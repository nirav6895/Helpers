#region Using
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Certificate pinning for <see cref="UnityWebRequest"/>: the request only goes through if the server's certificate
    /// has one of your pinned public keys. This stops players (or anyone on their network) reading or changing your
    /// API traffic through a proxy with their own certificate, e.g. Charles or mitmproxy.
    /// <code>
    /// private static readonly string[] ApiPins = { "current key pin=", "backup key pin=" };
    ///
    /// using UnityWebRequest request = UnityWebRequest.Get(url);
    /// request.certificateHandler = new PinnedCertificateHandler(ApiPins);
    /// yield return request.SendWebRequest();   // a rejected certificate ends with Result.ConnectionError
    /// </code>
    /// <para>
    /// A pin is the base64 SHA-256 of the certificate's public key (its SubjectPublicKeyInfo), the same value
    /// SSL Labs shows as "Pin SHA256". Get it from Tools > NG Utility > Certificate Pin Finder, from SSL Labs, or with
    /// <c>openssl x509 -in cert.pem -noout -pubkey | openssl pkey -pubin -outform DER | openssl dgst -sha256 -binary | base64</c>.
    /// </para>
    /// <para>
    /// Only the server's own (leaf) certificate is checked. Renewing the certificate with a new key locks out every
    /// app version that doesn't pin that key, so pin a backup key too, ship it before you switch, and renew with the
    /// same key (e.g. certbot --reuse-key). On WebGL the browser handles HTTPS and Unity doesn't call certificate
    /// handlers, so pinning has no effect there. A rooted or jailbroken device can also get around it.
    /// </para>
    /// A handler is disposed with its request by default, so create one per request.
    /// </summary>
    public class PinnedCertificateHandler : CertificateHandler
    {
        #region Variables
        private const string PemBegin = "-----BEGIN CERTIFICATE-----";
        private const string PemEnd = "-----END CERTIFICATE-----";
        private const byte SequenceTag = 0x30;
        private const byte VersionTag = 0xA0;

        private readonly string[] pins;
        #endregion


        #region Constructors
        /// <param name="pins">Accepted pins: base64 SHA-256 of the certificate's SubjectPublicKeyInfo. Pass at least
        /// your current key's pin and a backup key's pin.</param>
        public PinnedCertificateHandler(params string[] pins)
        {
            if (pins == null || pins.Length == 0)
                throw new ArgumentException("Pass at least one pin.", nameof(pins));

            this.pins = new string[pins.Length];
            for (int i = 0; i < pins.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(pins[i]))
                    throw new ArgumentException("Pins can't be empty.", nameof(pins));

                this.pins[i] = pins[i].Trim();
            }
        }
        #endregion


        #region Properties
        /// <summary>
        /// The accepted pins.
        /// </summary>
        public IReadOnlyList<string> Pins => pins;

        /// <summary>
        /// Log a warning with the server's pin when a certificate is rejected, which helps when a certificate was
        /// renewed with a new key. On by default.
        /// </summary>
        public bool LogRejections { get; set; } = true;
        #endregion


        #region Public Methods
        /// <summary>
        /// The pin of a certificate: base64 SHA-256 of its SubjectPublicKeyInfo. Accepts DER or PEM data; for PEM with
        /// several certificates, the first one. Returns null if the data isn't a certificate.
        /// </summary>
        public static string GetPin(byte[] certificateData)
        {
            byte[] der = ToDer(certificateData);
            if (der == null || !TryFindSubjectPublicKeyInfo(der, out int start, out int length))
                return null;

            using (SHA256 sha256 = SHA256.Create())
                return Convert.ToBase64String(sha256.ComputeHash(der, start, length));
        }
        #endregion


        #region Overridden Methods
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Called from native code, possibly off the main thread: never let an exception escape.
            try
            {
                string pin = GetPin(certificateData);
                if (pin != null)
                {
                    for (int i = 0; i < pins.Length; i++)
                    {
                        if (string.Equals(pins[i], pin, StringComparison.Ordinal))
                            return true;
                    }
                }

                if (LogRejections)
                    Debug.LogWarning($"[PinnedCertificateHandler] Rejected the server's certificate: its pin {pin ?? "(unreadable)"} isn't one of the pinned keys.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return false;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// DER bytes of the (first) certificate, decoding PEM if needed, or null.
        /// </summary>
        private static byte[] ToDer(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            if (data[0] == SequenceTag)
                return data;

            string text = Encoding.ASCII.GetString(data);
            int begin = text.IndexOf(PemBegin, StringComparison.Ordinal);
            if (begin < 0)
                return null;

            begin += PemBegin.Length;
            int end = text.IndexOf(PemEnd, begin, StringComparison.Ordinal);
            if (end < 0)
                return null;

            try
            {
                return Convert.FromBase64String(text.Substring(begin, end - begin));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the subjectPublicKeyInfo element in an X.509 certificate:
        /// Certificate ::= SEQUENCE { tbsCertificate SEQUENCE { [0] version OPTIONAL, serialNumber, signature,
        /// issuer, validity, subject, subjectPublicKeyInfo, ... }, ... }
        /// </summary>
        private static bool TryFindSubjectPublicKeyInfo(byte[] der, out int start, out int length)
        {
            start = length = 0;
            int position = 0;

            if (!TryReadHeader(der, ref position, out byte tag, out _) || tag != SequenceTag)
                return false;
            if (!TryReadHeader(der, ref position, out tag, out int tbsLength) || tag != SequenceTag)
                return false;

            int tbsEnd = position + tbsLength;

            if (position < tbsEnd && der[position] == VersionTag && !TrySkipElement(der, ref position))
                return false;

            // serialNumber, signature, issuer, validity, subject
            for (int i = 0; i < 5; i++)
            {
                if (position >= tbsEnd || !TrySkipElement(der, ref position))
                    return false;
            }

            start = position;
            if (position >= tbsEnd || der[position] != SequenceTag || !TrySkipElement(der, ref position) || position > tbsEnd)
                return false;

            length = position - start;
            return true;
        }

        private static bool TrySkipElement(byte[] der, ref int position)
        {
            if (!TryReadHeader(der, ref position, out _, out int contentLength))
                return false;

            position += contentLength;
            return true;
        }

        /// <summary>
        /// Reads a DER tag and length, leaving <paramref name="position"/> at the start of the content.
        /// </summary>
        private static bool TryReadHeader(byte[] der, ref int position, out byte tag, out int contentLength)
        {
            tag = 0;
            contentLength = 0;
            if (position + 2 > der.Length)
                return false;

            tag = der[position++];
            int first = der[position++];

            if (first < 0x80)
            {
                contentLength = first;
            }
            else
            {
                int byteCount = first & 0x7F;
                if (byteCount == 0 || byteCount > 3 || position + byteCount > der.Length)
                    return false;

                for (int i = 0; i < byteCount; i++)
                    contentLength = (contentLength << 8) | der[position++];
            }

            return position + contentLength <= der.Length;
        }
        #endregion
    }
}
