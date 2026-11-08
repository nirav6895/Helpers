#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="GameObject"/> and <see cref="Component"/>.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Returns the GameObject's <typeparamref name="T"/> component, adding one if it has none.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Returns the <typeparamref name="T"/> component on this component's GameObject, adding one if it has none.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }
    }
}
