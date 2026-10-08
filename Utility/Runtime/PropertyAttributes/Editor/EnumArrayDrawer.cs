namespace NG.UtilityEditor.Attribute
{
    #region Using
    using NG.Utility.Attribute;
    using UnityEditor;
    using UnityEngine;
    #endregion

    /// <Summary> 
    /// Enum Array Drawer, To show Enum Variable Names in list of array instead of "element X" 
    /// in Insepctor window.
    [CustomPropertyDrawer(typeof(EnumArrayAttribute))]
    public class EnumArrayDrawer : PropertyDrawer
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
        /// Overridden to change from "element X" to "[Enum Variable]"
        /// </Summary>
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            try
            {
                string fullPathName = property.propertyPath + "." + (attribute as EnumArrayAttribute).variableName;
                SerializedProperty variableRef = property.serializedObject.FindProperty(fullPathName);
                if (variableRef.propertyType == SerializedPropertyType.Enum)
                {
                    string elementName = variableRef.enumNames[variableRef.enumValueIndex];
                    label = new GUIContent(elementName);
                }
                EditorGUI.PropertyField(rect, property, label, property.isExpanded);
            }
            catch
            {
                EditorGUI.PropertyField(rect, property, label, property.isExpanded);
            }
        }
        #endregion
    }
}