#region Using
using System.Globalization;
using UnityEditor;
#endregion

namespace NG.Utility.Editor
{
    /// <summary>
    /// Helpers shared by the property drawers.
    /// </summary>
    internal static class DrawerUtility
    {
        /// <summary>
        /// Index of an array element from its property path, e.g. 3 for "items.Array.data[3]", or -1 if the path
        /// doesn't end with an element index.
        /// </summary>
        public static int GetArrayIndex(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath) || propertyPath[propertyPath.Length - 1] != ']')
                return -1;

            int index = 0;
            int multiplier = 1;
            for (int i = propertyPath.Length - 2; i >= 0; i--)
            {
                char c = propertyPath[i];
                if (c == '[')
                    return multiplier > 1 ? index : -1;

                if (c < '0' || c > '9')
                    return -1;

                index += (c - '0') * multiplier;
                multiplier *= 10;
            }

            return -1;
        }

        /// <summary>
        /// A short text for a property's value, used as a label, or null if it has none (empty string,
        /// missing object reference or an unsupported type).
        /// </summary>
        public static string GetValueLabel(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Enum:
                    string[] names = property.enumDisplayNames;
                    int index = property.enumValueIndex;
                    return index >= 0 && index < names.Length ? names[index] : property.intValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(property.stringValue) ? null : property.stringValue;

                case SerializedPropertyType.Integer:
                    return property.longValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString(CultureInfo.InvariantCulture);

                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "True" : "False";

                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : null;

                default:
                    return null;
            }
        }
    }
}
