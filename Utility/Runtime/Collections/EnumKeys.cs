#region Using
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
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

        /// <summary>
        /// Size of the enum's underlying type in bytes.
        /// </summary>
        private static readonly int Size = UnsafeUtility.SizeOf<TEnum>();

        /// <summary>
        /// Whether the values are exactly 0, 1, 2... (most enums). Then a value's position is its number, so
        /// <see cref="IndexOf"/> skips the dictionary.
        /// </summary>
        private static readonly bool IsSequential;

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

            IsSequential = true;
            for (int i = 0; i < Values.Length; i++)
            {
                if (ToNumber(Values[i]) != i)
                {
                    IsSequential = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Position of <paramref name="value"/> in <see cref="Values"/>, or -1 if it isn't a member.
        /// </summary>
        public static int IndexOf(TEnum value)
        {
            if (IsSequential)
            {
                long number = ToNumber(value);
                return number >= 0 && number < Values.Length ? (int)number : -1;
            }

            return Indices.TryGetValue(value, out int index) ? index : -1;
        }

        /// <summary>
        /// The enum's number, read without boxing. Smaller types are read unsigned, so negative values become large
        /// numbers; that's fine here, because only 0 to Count - 1 are valid positions.
        /// </summary>
        private static long ToNumber(TEnum value)
        {
            switch (Size)
            {
                case 1: return UnsafeUtility.As<TEnum, byte>(ref value);
                case 2: return UnsafeUtility.As<TEnum, ushort>(ref value);
                case 4: return UnsafeUtility.As<TEnum, uint>(ref value);
                default: return UnsafeUtility.As<TEnum, long>(ref value);
            }
        }
    }
}
