#region Using
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.ObjectPool
{
    /// <summary>
    /// Generic object pool for UI components
    /// </summary>
    /// <typeparam name="T">The type of UI component to pool.</typeparam>
    public class ObjectPool<T> : MonoBehaviour where T : Component
    {
        #region Inspector Variables
        [Header("Config")]
        [SerializeField] private T prefab;
        #endregion


        #region Variables
        private readonly Queue<T> pool = new();
        #endregion


        #region Methods
        /// <summary>
        /// Gets an item and sets its parent.
        /// </summary>
        public T Pop(Transform parent)
        {
            T item = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
            item.transform.SetParent(parent, false);
            item.gameObject.SetActive(true);
            return item;
        }

        /// <summary>
        /// Returns a single item to the pool.
        /// </summary>
        public void Push(T item, Transform parent = null)
        {
            if (parent == null)
                parent = transform;
            item.gameObject.SetActive(false);
            item.transform.SetParent(parent, false);
            pool.Enqueue(item);
        }

        /// <summary>
        /// Releases all items in the provided list.
        /// </summary>
        public void Push(List<T> list, Transform parent = null)
        {
            if (list != null)
            {
                foreach (T item in list)
                {
                    if (item != null)
                        Push(item, parent);
                }
                list.Clear();
            }
        }
        #endregion
    }
}