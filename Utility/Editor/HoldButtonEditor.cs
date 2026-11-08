#region Using
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Inspector for <see cref="HoldButton"/>: Unity's Button inspector, followed by the hold settings and events.
    /// Derive from it for a HoldButton subclass that adds its own fields.
    /// </summary>
    [CustomEditor(typeof(HoldButton), true)]
    [CanEditMultipleObjects]
    public class HoldButtonEditor : ButtonEditor
    {
        #region Variables
        private SerializedProperty holdDuration;
        private SerializedProperty useUnscaledTime;
        private SerializedProperty suppressClickAfterHold;
        private SerializedProperty repeatWhileHeld;
        private SerializedProperty repeatInterval;
        private SerializedProperty onHoldStarted;
        private SerializedProperty onHoldCompleted;
        private SerializedProperty onHoldCanceled;
        private SerializedProperty onHoldProgress;
        #endregion


        #region Unity Methods
        protected override void OnEnable()
        {
            base.OnEnable();

            holdDuration = serializedObject.FindProperty("holdDuration");
            useUnscaledTime = serializedObject.FindProperty("useUnscaledTime");
            suppressClickAfterHold = serializedObject.FindProperty("suppressClickAfterHold");
            repeatWhileHeld = serializedObject.FindProperty("repeatWhileHeld");
            repeatInterval = serializedObject.FindProperty("repeatInterval");
            onHoldStarted = serializedObject.FindProperty("onHoldStarted");
            onHoldCompleted = serializedObject.FindProperty("onHoldCompleted");
            onHoldCanceled = serializedObject.FindProperty("onHoldCanceled");
            onHoldProgress = serializedObject.FindProperty("onHoldProgress");
        }

        public override void OnInspectorGUI()
        {
            // Button's inspector applies its own changes.
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hold", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(holdDuration);
            EditorGUILayout.PropertyField(useUnscaledTime);
            EditorGUILayout.PropertyField(suppressClickAfterHold);
            EditorGUILayout.PropertyField(repeatWhileHeld);

            if (repeatWhileHeld.boolValue || repeatWhileHeld.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(repeatInterval);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onHoldStarted);
            EditorGUILayout.PropertyField(onHoldCompleted);
            EditorGUILayout.PropertyField(onHoldCanceled);
            EditorGUILayout.PropertyField(onHoldProgress, new GUIContent("On Hold Progress (0 to 1)"));

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
