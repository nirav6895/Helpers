#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Items of a pool, shared by <see cref="ObjectPool{T}"/> and <see cref="ComponentPool{T}"/>: the inactive items
    /// ready to be reused, and the active items handed out and not released yet.
    /// Tracks membership by reference so a double release can be detected, and skips Unity objects that were
    /// destroyed while stored. Main thread only.
    /// </summary>
    internal sealed class PoolStore<T> where T : class
    {
        #region Variables
        private readonly Stack<T> items = new();
        private readonly HashSet<T> lookup = new(IdentityComparer<T>.Instance);

        /// <summary>
        /// Active items, in no particular order.
        /// </summary>
        private readonly List<T> active = new();

        /// <summary>
        /// Index of every active item in <see cref="active"/>, so it can be removed without searching the list.
        /// </summary>
        private readonly Dictionary<T, int> activeIndex = new(IdentityComparer<T>.Instance);
        #endregion


        #region Properties
        /// <summary>
        /// Number of inactive items.
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Read-only view of the active items, in no particular order. It can't be cast back to a list to modify it.
        /// </summary>
        public IReadOnlyList<T> Active { get; }
        #endregion


        #region Constructors
        public PoolStore()
        {
            Active = active.AsReadOnly();
        }
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
        /// Marks <paramref name="item"/> as active, i.e. handed out by Get.
        /// </summary>
        public void AddActive(T item)
        {
            if (activeIndex.ContainsKey(item))
                return;

            activeIndex.Add(item, active.Count);
            active.Add(item);
        }

        /// <summary>
        /// Marks <paramref name="item"/> as not active anymore. Items that are not active are ignored.
        /// </summary>
        public void RemoveActive(T item)
        {
            if (!activeIndex.Remove(item, out int index))
                return;

            // Move the last item into the removed slot, so nothing else has to shift
            int lastIndex = active.Count - 1;
            if (index != lastIndex)
            {
                T lastItem = active[lastIndex];
                active[index] = lastItem;
                activeIndex[lastItem] = index;
            }

            active.RemoveAt(lastIndex);
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
