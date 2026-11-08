#region Using
using System;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Shows a serialized field in the Inspector but doesn't let it be edited, e.g. a value set by code that is
    /// useful to see while debugging: <c>[SerializeField, ReadOnlyField] private int score;</c>
    /// <para>
    /// On an array or list it applies to each element, so elements can't be edited but the size can still change.
    /// (It's not named ReadOnly, which would clash with Unity.Collections.ReadOnlyAttribute in job code.)
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ReadOnlyFieldAttribute : PropertyAttribute
    {
    }
}
