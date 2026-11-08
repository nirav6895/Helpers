#region Using
using System;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Labels the elements of an array or list in the Inspector with the members of an enum: element 0 gets the first
    /// member's name, element 1 the second, and so on, instead of "Element 0", "Element 1"...
    /// <code>
    /// [LabelFromEnum(typeof(WeaponType))] public float[] damagePerWeapon;
    /// </code>
    /// <para>
    /// Members are taken in value order (the order of Enum.GetValues), which matches <c>(int)member</c> for enums
    /// numbered 0, 1, 2... Members that share a value count once. Elements past the last member keep the default label.
    /// </para>
    /// The array's size isn't changed. For a collection that always has exactly one value per member, and keeps
    /// the values when members are added or reordered, use <see cref="EnumArray{TEnum, T}"/> instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LabelFromEnumAttribute : PropertyAttribute
    {
        /// <summary>
        /// The enum whose member names are used as labels.
        /// </summary>
        public Type EnumType { get; }

        /// <param name="enumType">The enum whose member names are used as labels, e.g. typeof(WeaponType).</param>
        public LabelFromEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}
