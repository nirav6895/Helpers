#region Using
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Draws <see cref="EnumArray{TEnum, T}"/> as a foldout with one field per enum member, labelled with its name.
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumArray<,>), true)]
    internal class EnumArrayDrawer : PropertyDrawer
    {
        #region Variables
        private const string KeysField = "keys";
        private const string ValuesField = "values";

        private readonly GUIContent elementLabel = new GUIContent();
        #endregion


        #region Unity Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded || !TryGetArrays(property, out SerializedProperty keys, out SerializedProperty values))
                return height;

            int count = Mathf.Min(keys.arraySize, values.arraySize);
            for (int i = 0; i < count; i++)
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(values.GetArrayElementAtIndex(i), true);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            label = EditorGUI.BeginProperty(line, label, property);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            EditorGUI.EndProperty();

            if (!property.isExpanded || !TryGetArrays(property, out SerializedProperty keys, out SerializedProperty values))
                return;

            int count = Mathf.Min(keys.arraySize, values.arraySize);
            if (count == 0)
                return;

            // Every key is a member of the same enum, so read the names once.
            string[] names = keys.GetArrayElementAtIndex(0).enumDisplayNames;

            EditorGUI.indentLevel++;
            float y = line.yMax;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty key = keys.GetArrayElementAtIndex(i);
                SerializedProperty value = values.GetArrayElementAtIndex(i);

                int nameIndex = key.enumValueIndex;
                elementLabel.text = nameIndex >= 0 && nameIndex < names.Length ? names[nameIndex] : key.intValue.ToString();

                y += EditorGUIUtility.standardVerticalSpacing;
                float height = EditorGUI.GetPropertyHeight(value, elementLabel, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), value, elementLabel, true);
                y += height;
            }

            EditorGUI.indentLevel--;
        }
        #endregion


        #region Private Methods
        private static bool TryGetArrays(SerializedProperty property, out SerializedProperty keys, out SerializedProperty values)
        {
            keys = property.FindPropertyRelative(KeysField);
            values = property.FindPropertyRelative(ValuesField);
            return keys != null && values != null && keys.isArray && values.isArray;
        }
        #endregion
    }
}
