#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Inactive items of a pool, shared by <see cref="ObjectPool{T}"/> and <see cref="ComponentPool{T}"/>.
    /// Tracks membership by reference so a double release can be detected, and skips Unity objects that were
    /// destroyed while stored. Main thread only.
    /// </summary>
    internal sealed class PoolStore<T> where T : class
    {
        #region Variables
        private readonly Stack<T> items = new();
        private readonly HashSet<T> lookup = new(IdentityComparer<T>.Instance);
        #endregion


        #region Properties
        public int Count => items.Count;
        #endregion


        #region Methods
        /// <summary>
        /// True if <paramref name="item"/> is already stored, i.e. releasing it again would be a double release.
        /// </summary>
        public bool Contains(T item)
        {
            return lookup.Contains(item);
        }

        /// <summary>
        /// Takes the most recently stored item, skipping any that were destroyed while stored.
        /// </summary>
        public bool TryTake(out T item)
        {
            while (items.Count > 0)
            {
                item = items.Pop();
                lookup.Remove(item);
                if (!IsDestroyed(item))
                    return true;
            }

            item = null;
            return false;
        }

        /// <summary>
        /// Stores <paramref name="item"/> unless the store is full. Callers check <see cref="Contains"/> first.
        /// </summary>
        /// <param name="maxSize">Maximum number of stored items; 0 = unlimited.</param>
        /// <returns>False if the store is full and the item was not stored.</returns>
        public bool TryAdd(T item, int maxSize)
        {
            if (maxSize > 0 && items.Count >= maxSize)
                return false;

            items.Push(item);
            lookup.Add(item);
            return true;
        }

        /// <summary>
        /// Empties the store, passing every item that still exists to <paramref name="onRemoved"/>.
        /// </summary>
        public void Clear(Action<T> onRemoved)
        {
            while (items.Count > 0)
            {
                T item = items.Pop();
                if (!IsDestroyed(item))
                    onRemoved?.Invoke(item);
            }

            lookup.Clear();
        }

        /// <summary>
        /// True for a UnityEngine.Object that has been destroyed. Plain C# objects are never "destroyed".
        /// </summary>
        public static bool IsDestroyed(T item)
        {
            return item is UnityEngine.Object unityObject && unityObject == null;
        }
        #endregion
    }
}
