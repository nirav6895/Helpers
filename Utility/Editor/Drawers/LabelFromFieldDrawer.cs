#region Using
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Draws <see cref="LabelFromFieldAttribute"/> fields: the element's label is the value of the named field.
    /// </summary>
    [CustomPropertyDrawer(typeof(LabelFromFieldAttribute))]
    internal class LabelFromFieldDrawer : PropertyDrawer
    {
        #region Variables
        private readonly GUIContent content = new GUIContent();

        /// <summary>
        /// Display names of the labelling field's enum, if it is one. The drawer belongs to one field, so it's always the same enum.
        /// </summary>
        private string[] enumNames;
        #endregion


        #region Unity Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string fieldName = ((LabelFromFieldAttribute)attribute).FieldName;
            SerializedProperty source = string.IsNullOrEmpty(fieldName) ? null : property.FindPropertyRelative(fieldName);
            string text = null;
            if (source != null)
            {
                // [SerializeReference] elements can be different classes, whose fields can be different enums: don't cache.
                if (property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    string[] uncached = null;
                    text = DrawerUtility.GetValueLabel(source, ref uncached);
                }
                else
                {
                    text = DrawerUtility.GetValueLabel(source, ref enumNames);
                }
            }

            if (text != null)
            {
                content.text = text;
                content.tooltip = label.tooltip;
                label = content;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }
        #endregion
    }
}
