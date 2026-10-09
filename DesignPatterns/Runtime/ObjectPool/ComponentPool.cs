#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Pool of prefab instances for any Component type. Unity can't add a generic component to a GameObject,
    /// so create a subclass per type, e.g. <c>public class BulletPool : ComponentPool&lt;Bullet&gt; { }</c>,
    /// add it to a GameObject and assign the prefab.
    /// <para>
    /// Released items are deactivated and kept under the pool's transform. If <typeparamref name="T"/> implements
    /// <see cref="IPoolable"/>, it is notified on every Get and Release so it can reset its state;
    /// subclasses can also override <see cref="OnGet"/> and <see cref="OnRelease"/>.
    /// </para>
    /// Main thread only.
    /// </summary>
    /// <typeparam name="T">Component on the prefab's root GameObject.</typeparam>
    public abstract class ComponentPool<T> : MonoBehaviour where T : Component
    {
        #region Inspector Variables
        [Header("Config")]
        [SerializeField] private T prefab;

        [Tooltip("Inactive instances created in Awake, so the first Get calls don't have to instantiate.")]
        [SerializeField, Min(0)] private int prewarmCount = 0;

        [Tooltip("Maximum number of inactive instances kept. Extra released items are destroyed. 0 = unlimited.")]
        [SerializeField, Min(0)] private int maxSize = 0;
        #endregion


        #region Variables
        private readonly Stack<T> available = new();
        private readonly HashSet<T> availableSet = new(IdentityComparer<T>.Instance);
        #endregion


        #region Properties
        /// <summary>
        /// Prefab that new instances are created from.
        /// </summary>
        public T Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        /// <summary>
        /// Maximum number of inactive instances kept; extra released items are destroyed. 0 = unlimited.
        /// </summary>
        public int MaxSize
        {
            get => maxSize;
            set => maxSize = Mathf.Max(0, value);
        }

        /// <summary>
        /// Number of inactive instances ready to be reused.
        /// </summary>
        public int CountInactive => available.Count;
        #endregion


        #region Unity Methods
        protected virtual void Awake()
        {
            if (prewarmCount > 0 && prefab != null)
                Prewarm(prewarmCount);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Gets an active instance, reusing an inactive one when possible.
        /// </summary>
        /// <param name="parent">New parent; null puts the item at the scene root. Local position, rotation and scale are kept.</param>
        public T Get(Transform parent = null)
        {
            T item = TakeAvailable();
            if (item == null)
            {
                EnsurePrefab();
                item = Instantiate(prefab, parent, false);
            }
            else
            {
                item.transform.SetParent(parent, false);
            }

            Activate(item);
            return item;
        }

        /// <summary>
        /// Gets an active instance at a world position and rotation, reusing an inactive one when possible.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T item = TakeAvailable();
            if (item == null)
            {
                EnsurePrefab();
                item = Instantiate(prefab, position, rotation, parent);
            }
            else
            {
                item.transform.SetParent(parent, false);
                item.transform.SetPositionAndRotation(position, rotation);
            }

            Activate(item);
            return item;
        }

        /// <summary>
        /// Deactivates an item and keeps it for reuse. Releasing an item that is already in the pool is
        /// ignored with a warning; destroyed items are ignored.
        /// </summary>
        public void Release(T item)
        {
            if (item == null)
                return;

            if (this == null)
            {
                // The pool itself is gone (e.g. its scene was unloaded), so there is nowhere to keep the item.
                DestroyObject(item.gameObject);
                return;
            }

            if (availableSet.Contains(item))
            {
                Debug.LogWarning($"[{GetType().Name}] '{item.name}' was released twice; the second release was ignored.", item);
                return;
            }

            if (item is IPoolable poolable)
                poolable.OnDespawn();

            OnRelease(item);
            item.gameObject.SetActive(false);

            if (maxSize > 0 && available.Count >= maxSize)
            {
                DestroyObject(item.gameObject);
                return;
            }

            item.transform.SetParent(transform, false);
            available.Push(item);
            availableSet.Add(item);
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
        /// Creates inactive instances until at least <paramref name="count"/> are available (capped by <see cref="MaxSize"/>).
        /// </summary>
        public void Prewarm(int count)
        {
            EnsurePrefab();

            int target = maxSize > 0 ? Mathf.Min(count, maxSize) : count;
            while (available.Count < target)
            {
                T item = Instantiate(prefab, transform, false);
                item.gameObject.SetActive(false);
                available.Push(item);
                availableSet.Add(item);
            }
        }

        /// <summary>
        /// Destroys every inactive instance. Items that are currently in use are not affected.
        /// </summary>
        public void Clear()
        {
            while (available.Count > 0)
            {
                T item = available.Pop();
                if (item != null)
                    DestroyObject(item.gameObject);
            }

            availableSet.Clear();
        }
        #endregion


        #region Virtual Methods
        /// <summary>
        /// Called after an item has been activated by Get.
        /// </summary>
        protected virtual void OnGet(T item) { }

        /// <summary>
        /// Called by Release before the item is deactivated.
        /// </summary>
        protected virtual void OnRelease(T item) { }
        #endregion


        #region Private Methods
        /// <summary>
        /// Takes the next inactive instance, skipping any that were destroyed while in the pool.
        /// </summary>
        private T TakeAvailable()
        {
            while (available.Count > 0)
            {
                T item = available.Pop();
                availableSet.Remove(item);
                if (item != null)
                    return item;
            }

            return null;
        }

        private void Activate(T item)
        {
            item.gameObject.SetActive(true);
            OnGet(item);

            if (item is IPoolable poolable)
                poolable.OnSpawn();
        }

        private void EnsurePrefab()
        {
            if (prefab == null)
                throw new InvalidOperationException($"{GetType().Name} on '{name}' has no prefab assigned.");
        }

        private static void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
        #endregion
    }
}
