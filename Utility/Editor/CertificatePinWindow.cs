#region Using
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Tools > NG Utility > Certificate Pin Finder: connects to a URL and shows the pin of the server's certificate,
    /// for <see cref="PinnedCertificateHandler"/>.
    /// </summary>
    internal class CertificatePinWindow : EditorWindow
    {
        #region Variables
        private string url = "https://";
        private string pin;
        private string message;
        private MessageType messageType;
        private UnityWebRequest request;
        #endregion


        #region Unity Methods
        [MenuItem("Tools/NG Utility/Certificate Pin Finder")]
        private static void Open()
        {
            GetWindow<CertificatePinWindow>("Certificate Pin").minSize = new Vector2(380f, 200f);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Gets the pin of a server's certificate, to pass to PinnedCertificateHandler. Do this on a network you " +
                "trust: on an intercepted network you would get the interceptor's pin.",
                MessageType.Info);

            url = EditorGUILayout.TextField("URL", url);

            using (new EditorGUI.DisabledScope(request != null))
            {
                if (GUILayout.Button(request != null ? "Connecting..." : "Get Pin"))
                    Send();
            }

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, messageType);

            if (!string.IsNullOrEmpty(pin))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Pin", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(pin, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button("Copy"))
                    EditorGUIUtility.systemCopyBuffer = pin;
            }
        }

        private void OnDisable()
        {
            request?.Dispose();
            request = null;
        }
        #endregion


        #region Private Methods
        private void Send()
        {
            pin = null;
            message = null;

            PinRecorder recorder = new PinRecorder();
            try
            {
                request = UnityWebRequest.Head(url.Trim());
            }
            catch (System.Exception exception)
            {
                recorder.Dispose();
                ShowMessage($"Invalid URL: {exception.Message}", MessageType.Error);
                return;
            }

            request.certificateHandler = recorder;
            request.timeout = 15;

            UnityWebRequest sent = request;
            sent.SendWebRequest().completed += _ =>
            {
                if (request != sent)
                    return;

                // The pin is read during the TLS handshake, so it's there even if the server answers with an HTTP error.
                pin = recorder.Pin;
                if (pin != null)
                    ShowMessage("Add this pin, and a backup key's pin, to PinnedCertificateHandler.", MessageType.None);
                else if (sent.result == UnityWebRequest.Result.ConnectionError)
                    ShowMessage($"Couldn't connect: {sent.error}", MessageType.Error);
                else
                    ShowMessage("The server didn't send a certificate. Use an https:// URL.", MessageType.Warning);

                sent.Dispose();
                request = null;
                Repaint();
            };
        }

        private void ShowMessage(string text, MessageType type)
        {
            message = text;
            messageType = type;
        }
        #endregion


        #region Pin Recorder
        /// <summary>
        /// Accepts any certificate and remembers its pin. Only used to read the pin, never to send data.
        /// </summary>
        private class PinRecorder : CertificateHandler
        {
            public string Pin { get; private set; }

            protected override bool ValidateCertificate(byte[] certificateData)
            {
                Pin = PinnedCertificateHandler.GetPin(certificateData);
                return true;
            }
        }
        #endregion
    }
}
