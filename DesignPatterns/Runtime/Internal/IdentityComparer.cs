#region Using
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Compares by reference, ignoring any Equals override (including UnityEngine.Object's, which treats
    /// all destroyed objects as equal to null).
    /// </summary>
    internal sealed class IdentityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly IdentityComparer<T> Instance = new();

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
