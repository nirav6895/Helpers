# if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

namespace NG.Utility.Editor
{
    [CustomEditor(typeof(HoldButton))]
    public class HoldButtonEditor : ButtonEditor
    {
        private SerializedProperty requireHoldTimeProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            requireHoldTimeProp = serializedObject.FindProperty("requireHoldTime");
        }

        public override void OnInspectorGUI()
        {
            // Draw the default Button inspector
            base.OnInspectorGUI();

            // Draw your custom field
            serializedObject.Update();
            EditorGUILayout.PropertyField(requireHoldTimeProp);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif