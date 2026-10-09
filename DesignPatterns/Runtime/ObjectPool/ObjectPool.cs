#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Pool of plain C# objects (or any reference type), created on demand by a factory function.
    /// Not a MonoBehaviour: create one in code, e.g.
    /// <c>new ObjectPool&lt;List&lt;int&gt;&gt;(() =&gt; new List&lt;int&gt;(), actionOnRelease: list =&gt; list.Clear())</c>.
    /// For prefab instances, use <see cref="ComponentPool{T}"/>.
    /// <para>
    /// If <typeparamref name="T"/> implements <see cref="IPoolable"/>, items are notified on every Get and Release
    /// (OnSpawn after <c>actionOnGet</c>, OnDespawn before <c>actionOnRelease</c>).
    /// Releasing an item that is already in the pool is ignored with a warning.
    /// </para>
    /// <para>
    /// UnityEngine.Pool also has an ObjectPool&lt;T&gt;. In a file that imports both namespaces,
    /// write NG.DesignPatterns.ObjectPool&lt;T&gt;.
    /// </para>
    /// Main thread only.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        #region Variables
        private readonly Func<T> createFunc;
        private readonly Action<T> actionOnGet;
        private readonly Action<T> actionOnRelease;
        private readonly Action<T> actionOnDestroy;
        private readonly PoolStore<T> store = new();
        private int maxSize;
        #endregion


        #region Properties
        /// <summary>
        /// Maximum number of inactive items kept. Extra released items go to <c>actionOnDestroy</c>. 0 = unlimited.
        /// </summary>
        public int MaxSize
        {
            get => maxSize;
            set => maxSize = Math.Max(0, value);
        }

        /// <summary>
        /// Number of inactive items ready to be reused.
        /// </summary>
        public int CountInactive => store.Count;
        #endregion


        #region Constructors
        /// <param name="createFunc">Creates a new item when the pool is empty. Must not return null.</param>
        /// <param name="actionOnGet">Called on every item handed out by Get, new or reused.</param>
        /// <param name="actionOnRelease">Called on every released (and prewarmed) item, e.g. to reset it.</param>
        /// <param name="actionOnDestroy">Called on items the pool drops: over <paramref name="maxSize"/>, or on Clear.</param>
        /// <param name="maxSize">Maximum number of inactive items kept; 0 = unlimited.</param>
        public ObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, int maxSize = 0)
        {
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.actionOnGet = actionOnGet;
            this.actionOnRelease = actionOnRelease;
            this.actionOnDestroy = actionOnDestroy;
            MaxSize = maxSize;
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Gets an item, reusing an inactive one when possible.
        /// </summary>
        public T Get()
        {
            if (!store.TryTake(out T item))
                item = Create();

            actionOnGet?.Invoke(item);
            if (item is IPoolable poolable)
                poolable.OnSpawn();

            return item;
        }

        /// <summary>
        /// Returns an item to the pool. Null, destroyed Unity objects, and items already in the pool are ignored.
        /// </summary>
        public void Release(T item)
        {
            if (item == null || PoolStore<T>.IsDestroyed(item))
                return;

            if (store.Contains(item))
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] {item} was released twice; the second release was ignored.", item as UnityEngine.Object);
                return;
            }

            if (item is IPoolable poolable)
                poolable.OnDespawn();

            actionOnRelease?.Invoke(item);

            if (!store.TryAdd(item, maxSize))
                actionOnDestroy?.Invoke(item);
        }

        /// <summary>
        /// Releases every item in the list, then clears the list.
        /// </summary>
        public void Release(List<T> items)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
                Release(items[i]);

            items.Clear();
        }

        /// <summary>
        /// Creates items until at least <paramref name="count"/> are inactive (capped by <see cref="MaxSize"/>).
        /// Prewarmed items go through <c>actionOnRelease</c>, so they are in the same state as released ones.
        /// </summary>
        public void Prewarm(int count)
        {
            int target = maxSize > 0 ? Math.Min(count, maxSize) : count;
            while (store.Count < target)
            {
                T item = Create();
                actionOnRelease?.Invoke(item);
                store.TryAdd(item, maxSize);
            }
        }

        /// <summary>
        /// Drops every inactive item, passing each to <c>actionOnDestroy</c>. Items in use are not affected.
        /// </summary>
        public void Clear()
        {
            store.Clear(actionOnDestroy);
        }
        #endregion


        #region Private Methods
        private T Create()
        {
            T item = createFunc();
            if (item == null)
                throw new InvalidOperationException($"[ObjectPool<{typeof(T).Name}>] createFunc returned null.");

            return item;
        }
        #endregion
    }
}
