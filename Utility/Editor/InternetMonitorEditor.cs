#region Using
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Inspector for <see cref="InternetMonitor"/>. In Play Mode it shows the status, a Simulate Offline toggle
    /// and a Check Now button.
    /// </summary>
    [CustomEditor(typeof(InternetMonitor))]
    internal class InternetMonitorEditor : UnityEditor.Editor
    {
        #region Variables
        private static readonly GUIContent SimulateOfflineLabel = new GUIContent(
            "Simulate Offline",
            "Makes InternetMonitor.IsAvailable false, to test how the game handles going offline. Resets when Play Mode starts.");
        #endregion


        #region Unity Methods
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            InternetMonitor monitor = (InternetMonitor)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play Mode", EditorStyles.boldLabel);

            string status = InternetMonitor.IsAvailable ? "Online" : "Offline";
            if (monitor.IsChecking)
                status += " (checking...)";

            EditorGUILayout.LabelField("Status", status);
            InternetMonitor.SimulateOffline = EditorGUILayout.Toggle(SimulateOfflineLabel, InternetMonitor.SimulateOffline);

            using (new EditorGUI.DisabledScope(!monitor.isActiveAndEnabled))
            {
                if (GUILayout.Button("Check Now"))
                    monitor.CheckNow();
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
        #endregion
    }
}
