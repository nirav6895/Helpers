#region Using
using System;
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Draws <see cref="SerializableDictionary{TKey, TValue}"/> as its list of entries, under the dictionary's own label.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    internal class SerializableDictionaryDrawer : PropertyDrawer
    {
        internal const string EntriesField = "entries";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty entries = property.FindPropertyRelative(EntriesField);
            return EditorGUI.GetPropertyHeight(entries ?? property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty entries = property.FindPropertyRelative(EntriesField);
            EditorGUI.PropertyField(position, entries ?? property, label, true);
        }
    }

    /// <summary>
    /// Draws one dictionary entry as a key | value row. A key that is a duplicate of an earlier row, or an empty
    /// object reference, is highlighted: that row is ignored at runtime.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>.Entry), true)]
    internal class SerializableDictionaryEntryDrawer : PropertyDrawer
    {
        #region Variables
        private const string KeyField = "key";
        private const string ValueField = "value";
        private const float KeyWidthFraction = 0.4f;
        private const float Gap = 4f;

        private static readonly Color InvalidKeyColor = new Color(1f, 0.25f, 0.25f, 0.35f);
        private static readonly GUIContent DuplicateKeyTooltip = new GUIContent(string.Empty, "Duplicate key: this row is ignored. The first row with this key is used.");
        private static readonly GUIContent EmptyKeyTooltip = new GUIContent(string.Empty, "Empty key: this row is ignored.");
        #endregion


        #region Unity Methods
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty key = property.FindPropertyRelative(KeyField);
            SerializedProperty value = property.FindPropertyRelative(ValueField);
            if (key == null || value == null)
                return EditorGUI.GetPropertyHeight(property, label, true);

            return Mathf.Max(EditorGUI.GetPropertyHeight(key, GUIContent.none, true), EditorGUI.GetPropertyHeight(value, GUIContent.none, true));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty key = property.FindPropertyRelative(KeyField);
            SerializedProperty value = property.FindPropertyRelative(ValueField);
            if (key == null || value == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float keyWidth = position.width * KeyWidthFraction;
            Rect keyRect = new Rect(position.x, position.y, keyWidth - Gap * 0.5f, EditorGUI.GetPropertyHeight(key, GUIContent.none, true));
            Rect valueRect = new Rect(position.x + keyWidth + Gap * 0.5f, position.y, position.width - keyWidth - Gap * 0.5f, EditorGUI.GetPropertyHeight(value, GUIContent.none, true));

            int indent = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            GUIContent problem = GetKeyProblem(property, key);
            if (problem != null)
            {
                EditorGUI.DrawRect(keyRect, InvalidKeyColor);
                GUI.Label(keyRect, problem);
            }

            EditorGUI.PropertyField(keyRect, key, GUIContent.none, true);

            // Nested fields of a class value get labels sized to the value column.
            EditorGUIUtility.labelWidth = Mathf.Max(60f, valueRect.width * 0.4f);
            EditorGUI.PropertyField(valueRect, value, GUIContent.none, true);

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Tooltip explaining why this row's key is ignored, or null if it is used.
        /// </summary>
        private static GUIContent GetKeyProblem(SerializedProperty entry, SerializedProperty key)
        {
            if (key.propertyType == SerializedPropertyType.ObjectReference && key.objectReferenceValue == null)
                return EmptyKeyTooltip;

            // "dictionary.entries.Array.data[3]" -> the list "dictionary.entries" and index 3.
            string path = entry.propertyPath;
            int arrayStart = path.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            int index = DrawerUtility.GetArrayIndex(path);
            if (arrayStart < 0 || index <= 0)
                return null;

            SerializedProperty list = entry.serializedObject.FindProperty(path.Substring(0, arrayStart));
            if (list == null || !list.isArray)
                return null;

            // Only earlier rows count: the first row with a key wins.
            for (int i = 0; i < index && i < list.arraySize; i++)
            {
                SerializedProperty otherKey = list.GetArrayElementAtIndex(i).FindPropertyRelative(KeyField);
                if (otherKey != null && SerializedProperty.DataEquals(otherKey, key))
                    return DuplicateKeyTooltip;
            }

            return null;
        }
        #endregion
    }
}
