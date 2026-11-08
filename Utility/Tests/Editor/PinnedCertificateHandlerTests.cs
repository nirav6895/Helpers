#region Using
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
#endregion

namespace NG.Utility.Tests
{
    public class PinnedCertificateHandlerTests
    {
        // Self-signed test certificates. The expected pins were computed independently with OpenSSL:
        // openssl x509 -noout -pubkey | openssl pkey -pubin -outform DER | openssl dgst -sha256 -binary | base64
        private const string RsaCertificateDer =
            "MIIDHTCCAgWgAwIBAgIUEtJ9kUcf/uX4uOE6f7YqFb4vjLwwDQYJKoZIhvcNAQELBQAwHjEcMBoGA1UEAwwTbmctdXRpbGl0eS10ZXN0LXJzYTAe" +
            "Fw0yNjExMDgxMDE2NTBaFw0zNjExMDUxMDE2NTBaMB4xHDAaBgNVBAMME25nLXV0aWxpdHktdGVzdC1yc2EwggEiMA0GCSqGSIb3DQEBAQUAA4IB" +
            "DwAwggEKAoIBAQDKmBk7gE/jMC+W60yQLLoVtoNFs9X8sLo96u+OCSlB7G4WMwXmmmg0dQ5kWLupARhy+/bdYbVDkvV2FYxUlpYT7IvejTzKhqeo" +
            "MPHuU206xAU55J64j1yuaB9VDAOTdQhMLezH5gfIC0XvCjSb1hPnmdMU3TpR3dJqDjOgoPapqJhKQpXgnpGeH0UNZZXec0TEMfoQ2uC4XLlOELsw" +
            "gUL5VRiEWJccHUY6ZQ6Oo6caUkSlb6Or003bBqKuEsiIkhE9woc/NUCHBWFSLY6Uwa0afBv3KPTqSF/Mi7zsVdlYTEzKE/DEiNZbfK0+nGWqe9cj" +
            "REk4PAKgHm38ZFmdeaIzAgMBAAGjUzBRMB0GA1UdDgQWBBThmjHhNdGo6ZNW+NnO1CxptosR/TAfBgNVHSMEGDAWgBThmjHhNdGo6ZNW+NnO1Cxp" +
            "tosR/TAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQAPQJGn1iEieEuA6PYistRsRjQWRWuPLDOzFcTCTSCPX16UoSB6or7bjZQa" +
            "bCiGvtU91M5+YldIFWp/krZErg61lCeu/+28cTXP/DyuQgeXV4QK+tK6aAMcLz1ZPqbwoD6Vs4AkgnSXjTkWeT8Q3EQqn13HyFzUsZrdqWShMaec" +
            "+/boGtqmAv0uY62COyxlAF+bjtDKoried92QMqxURuhwQnAH4e4/Xa7R+mGBceHSF5bUzyLyEcOncD3HQMhgmSoRvfFzV2ZkTF91lp0xNC7+p+xT" +
            "+zVmOYGBZN/LnZ2MR252ay9+cbQP7PODMZPIGe1AeVoSTZJ+MsrFwMUMdt+c";

        private const string RsaPin = "Y1mxwYFLdHwgHmb6u0OozlKZ6KSh9E7s4eLsqW/xkp8=";

        private const string EcCertificatePem =
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIBjjCCATWgAwIBAgIUdH2FhBCSBQjSOzZDLtzB41e8YL0wCgYIKoZIzj0EAwIw\n" +
            "HTEbMBkGA1UEAwwSbmctdXRpbGl0eS10ZXN0LWVjMB4XDTI2MTEwODEwMTY1MFoX\n" +
            "DTM2MTEwNTEwMTY1MFowHTEbMBkGA1UEAwwSbmctdXRpbGl0eS10ZXN0LWVjMFkw\n" +
            "EwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENsFxUKRz+wgYm2uAliF8hrmQVrVX+4Oh\n" +
            "CGpGAuSK9qwlrpUftwDHRMG5+aUY639bO2034QbNM7kTqJApi1DdHaNTMFEwHQYD\n" +
            "VR0OBBYEFOn/pMiTdJGbeXXLVhkU6rLijemFMB8GA1UdIwQYMBaAFOn/pMiTdJGb\n" +
            "eXXLVhkU6rLijemFMA8GA1UdEwEB/wQFMAMBAf8wCgYIKoZIzj0EAwIDRwAwRAIg\n" +
            "IW+GcvkwFn2TzqM3o77/6310dGJ2nGWqpM2RFwk5lF4CIDbXPM3Vbdb+HsXwXtuO\n" +
            "OvwkBz0/3DxiNZImVfZ6alAY\n" +
            "-----END CERTIFICATE-----\n";

        private const string EcPin = "ovCRkXEtApoyrbhl8DcatPRRcSFKwiXOLLDSIxqF1Wc=";

        /// <summary>
        /// Exposes ValidateCertificate, which UnityWebRequest calls during the TLS handshake.
        /// </summary>
        private class TestHandler : PinnedCertificateHandler
        {
            public TestHandler(params string[] pins) : base(pins) { }

            public bool Validate(byte[] certificateData) => ValidateCertificate(certificateData);
        }

        /// <summary>
        /// Accepts any certificate and remembers its pin.
        /// </summary>
        private class PinRecorder : CertificateHandler
        {
            public string Pin;

            protected override bool ValidateCertificate(byte[] certificateData)
            {
                Pin = PinnedCertificateHandler.GetPin(certificateData);
                return true;
            }
        }

        [Test]
        public void GetPin_RsaDer_MatchesOpenSsl()
        {
            Assert.AreEqual(RsaPin, PinnedCertificateHandler.GetPin(Convert.FromBase64String(RsaCertificateDer)));
        }

        [Test]
        public void GetPin_EcPem_MatchesOpenSsl()
        {
            Assert.AreEqual(EcPin, PinnedCertificateHandler.GetPin(Encoding.ASCII.GetBytes(EcCertificatePem)));
        }

        [Test]
        public void GetPin_PemWithSeveralCertificates_UsesTheFirst()
        {
            string chain = EcCertificatePem + "-----BEGIN CERTIFICATE-----\n" + RsaCertificateDer + "\n-----END CERTIFICATE-----\n";
            Assert.AreEqual(EcPin, PinnedCertificateHandler.GetPin(Encoding.ASCII.GetBytes(chain)));
        }

        [Test]
        public void GetPin_NotACertificate_ReturnsNull()
        {
            Assert.IsNull(PinnedCertificateHandler.GetPin(null));
            Assert.IsNull(PinnedCertificateHandler.GetPin(new byte[0]));
            Assert.IsNull(PinnedCertificateHandler.GetPin(Encoding.ASCII.GetBytes("hello")));
            Assert.IsNull(PinnedCertificateHandler.GetPin(new byte[] { 0x30, 0x82, 0xFF, 0xFF, 0x01 }));

            byte[] truncated = Convert.FromBase64String(RsaCertificateDer);
            Array.Resize(ref truncated, 200);
            Assert.IsNull(PinnedCertificateHandler.GetPin(truncated));
        }

        [Test]
        public void Validate_AcceptsAPinnedKey_AndRejectsOthers()
        {
            using (TestHandler handler = new TestHandler(EcPin, RsaPin))
                Assert.IsTrue(handler.Validate(Convert.FromBase64String(RsaCertificateDer)));

            using (TestHandler handler = new TestHandler(EcPin))
            {
                LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(RsaPin)));
                Assert.IsFalse(handler.Validate(Convert.FromBase64String(RsaCertificateDer)));

                handler.LogRejections = false;
                Assert.IsFalse(handler.Validate(Encoding.ASCII.GetBytes("not a certificate")));
            }
        }

        [Test]
        public void Constructor_RequiresPins()
        {
            Assert.Throws<ArgumentException>(() => new PinnedCertificateHandler());
            Assert.Throws<ArgumentException>(() => new PinnedCertificateHandler(" "));
        }

        [UnityTest]
        public IEnumerator RealRequest_SucceedsWithTheServersPin_AndFailsWithAnother()
        {
            const string Url = "https://www.google.com/generate_204";

            // Read the server's pin first. Skipped when there's no internet.
            string serverPin;
            using (UnityWebRequest probe = UnityWebRequest.Head(Url))
            {
                PinRecorder recorder = new PinRecorder();
                probe.certificateHandler = recorder;
                probe.timeout = 15;
                UnityWebRequestAsyncOperation operation = probe.SendWebRequest();
                while (!operation.isDone)
                    yield return null;

                Assume.That(probe.result, Is.Not.EqualTo(UnityWebRequest.Result.ConnectionError), "Needs an internet connection.");
                serverPin = recorder.Pin;
                Assert.IsNotNull(serverPin, "ValidateCertificate got a certificate it could read.");
            }

            using (UnityWebRequest pinned = UnityWebRequest.Head(Url))
            {
                pinned.certificateHandler = new PinnedCertificateHandler(serverPin);
                pinned.timeout = 15;
                UnityWebRequestAsyncOperation operation = pinned.SendWebRequest();
                while (!operation.isDone)
                    yield return null;

                Assert.AreNotEqual(UnityWebRequest.Result.ConnectionError, pinned.result, pinned.error);
            }

            using (UnityWebRequest wrong = UnityWebRequest.Head(Url))
            {
                wrong.certificateHandler = new PinnedCertificateHandler(EcPin);
                wrong.timeout = 15;
                LogAssert.Expect(LogType.Warning, new Regex(@"^\[PinnedCertificateHandler\] Rejected"));
                UnityWebRequestAsyncOperation operation = wrong.SendWebRequest();
                while (!operation.isDone)
                    yield return null;

                Assert.AreEqual(UnityWebRequest.Result.ConnectionError, wrong.result, "A certificate that isn't pinned is rejected.");
            }
        }
    }
}
