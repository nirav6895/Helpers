#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// The distinct values of an enum, in value order (the order of Enum.GetValues). Members that share a value
    /// (aliases) count once. Shared by <see cref="EnumArray{TEnum, T}"/> and the LabelFromEnum drawer, so they agree.
    /// </summary>
    internal static class EnumKeys
    {
        /// <summary>
        /// Name of each distinct value of <paramref name="enumType"/>, in value order.
        /// </summary>
        public static string[] GetNames(Type enumType)
        {
            Array values = Enum.GetValues(enumType);
            HashSet<object> seen = new HashSet<object>();
            List<string> names = new List<string>(values.Length);

            foreach (object value in values)
            {
                if (seen.Add(value))
                    names.Add(Enum.GetName(enumType, value));
            }

            return names.ToArray();
        }
    }

    /// <inheritdoc cref="EnumKeys"/>
    internal static class EnumKeys<TEnum> where TEnum : struct, Enum
    {
        /// <summary>
        /// Distinct values, in value order. Don't modify.
        /// </summary>
        public static readonly TEnum[] Values;

        private static readonly Dictionary<TEnum, int> Indices;

        static EnumKeys()
        {
            Array all = Enum.GetValues(typeof(TEnum));
            List<TEnum> values = new List<TEnum>(all.Length);
            Indices = new Dictionary<TEnum, int>(all.Length);

            foreach (TEnum value in all)
            {
                if (Indices.ContainsKey(value))
                    continue;

                Indices.Add(value, values.Count);
                values.Add(value);
            }

            Values = values.ToArray();
        }

        /// <summary>
        /// Position of <paramref name="value"/> in <see cref="Values"/>, or -1 if it isn't a member.
        /// </summary>
        public static int IndexOf(TEnum value)
        {
            return Indices.TryGetValue(value, out int index) ? index : -1;
        }
    }
}
