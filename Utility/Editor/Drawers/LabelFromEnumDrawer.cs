#region Using
using System;
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Draws <see cref="LabelFromEnumAttribute"/> fields: element i is labelled with the enum's i-th member.
    /// </summary>
    [CustomPropertyDrawer(typeof(LabelFromEnumAttribute))]
    internal class LabelFromEnumDrawer : PropertyDrawer
    {
        #region Variables
        private readonly GUIContent content = new GUIContent();
        private string[] names;
        #endregion


        #region Unity Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string[] memberNames = GetNames();
            int index = DrawerUtility.GetArrayIndex(property.propertyPath);

            if (memberNames != null && index >= 0 && index < memberNames.Length)
            {
                content.text = memberNames[index];
                content.tooltip = label.tooltip;
                label = content;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }
        #endregion


        #region Private Methods
        private string[] GetNames()
        {
            if (names != null)
                return names;

            Type enumType = ((LabelFromEnumAttribute)attribute).EnumType;
            if (enumType == null || !enumType.IsEnum)
            {
                Debug.LogWarning($"[LabelFromEnum] The type given to [LabelFromEnum] on '{fieldInfo?.Name}' is not an enum, so the default labels are kept.");
                names = Array.Empty<string>();
                return names;
            }

            names = EnumKeys.GetNames(enumType);
            for (int i = 0; i < names.Length; i++)
                names[i] = ObjectNames.NicifyVariableName(names[i]);

            return names;
        }
        #endregion
    }
}
