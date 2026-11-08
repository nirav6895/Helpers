#region Using
using System;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Labels each element of an array or list in the Inspector with the value of one of its fields,
    /// instead of "Element 0", "Element 1"...
    /// <code>
    /// [Serializable] public class Reward { public RewardType type; public int amount; }
    ///
    /// [LabelFromField(nameof(Reward.type))] public List&lt;Reward&gt; rewards;
    /// </code>
    /// The field can be an enum, string, number, bool or object reference. When it's empty, the default label is kept.
    /// (Unity already does this by itself when the element's first field is a string.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LabelFromFieldAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the element's field whose value is used as the label.
        /// </summary>
        public string FieldName { get; }

        /// <param name="fieldName">Name of the element's field whose value is used as the label. Use nameof() so renames are caught.</param>
        public LabelFromFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
