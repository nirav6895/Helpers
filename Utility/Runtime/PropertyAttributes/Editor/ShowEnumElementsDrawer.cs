namespace NG.UtilityEditor.Attribute
{
    #region Using
    using NG.Utility.Attribute;
    using System;
    using UnityEditor;
    using UnityEngine;
    #endregion

    /// <Summary> 
    /// Enum Drawer, To show Enum Names in list of array instead of "element X" 
    /// in Insepctor window.
    [CustomPropertyDrawer(typeof(ShowEnumElementsAttribute))]
    public class ShowEnumElementsDrawer : PropertyDrawer
    {
        #region Methods
        /// <Summary> 
        /// Overridden to Properly configure height for expanded contents.
        /// </Summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
        }

        /// <Summary> 
        /// Overridden to change from "element X" to "[Enum Member Name]"
        /// </Summary>
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            try
            {
                string[] splitedPath = property.propertyPath.Split('[', ']');
                int indexOfArray = int.Parse(splitedPath[splitedPath.Length - 2]);
                Type enumType = (attribute as ShowEnumElementsAttribute).enumType;
                string elementName = enumType.GetEnumName(indexOfArray);
                if (string.IsNullOrEmpty(elementName))
                    EditorGUI.PropertyField(rect, property, label, property.isExpanded);
                else
                    EditorGUI.PropertyField(rect, property, new GUIContent(elementName), property.isExpanded);
            }
            catch
            {
                EditorGUI.PropertyField(rect, property, label, property.isExpanded);
            }
        }
        #endregion
    }
}