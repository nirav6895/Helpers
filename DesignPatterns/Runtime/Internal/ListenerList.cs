#region Using
using System;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Copy-on-write list of delegates. Add and Remove build a new array; <see cref="Items"/> never allocates,
    /// so dispatch can iterate a snapshot safely even if listeners register or unregister while it runs.
    /// Main thread only.
    /// </summary>
    internal sealed class ListenerList<TDelegate> where TDelegate : Delegate
    {
        #region Variables
        private TDelegate[] items = Array.Empty<TDelegate>();
        #endregion


        #region Properties
        /// <summary>
        /// Current snapshot. Never modified in place.
        /// </summary>
        public TDelegate[] Items => items;

        public int Count => items.Length;
        #endregion


        #region Methods
        /// <summary>
        /// Adds the listener unless an equal delegate (same target and method) is already present.
        /// </summary>
        /// <returns>False if it was a duplicate.</returns>
        public bool Add(TDelegate listener)
        {
            if (Array.IndexOf(items, listener) >= 0)
                return false;

            var next = new TDelegate[items.Length + 1];
            Array.Copy(items, next, items.Length);
            next[items.Length] = listener;
            items = next;
            return true;
        }

        /// <returns>False if the listener wasn't registered.</returns>
        public bool Remove(TDelegate listener)
        {
            int index = Array.IndexOf(items, listener);
            if (index < 0)
                return false;

            if (items.Length == 1)
            {
                items = Array.Empty<TDelegate>();
                return true;
            }

            var next = new TDelegate[items.Length - 1];
            Array.Copy(items, 0, next, 0, index);
            Array.Copy(items, index + 1, next, index, items.Length - index - 1);
            items = next;
            return true;
        }

        public void Clear()
        {
            items = Array.Empty<TDelegate>();
        }
        #endregion
    }

    /// <summary>
    /// Helpers shared by the event observers.
    /// </summary>
    internal static class ListenerUtility
    {
        /// <summary>
        /// True when the listener is a method of a UnityEngine.Object that has been destroyed,
        /// i.e. its owner forgot to unregister.
        /// </summary>
        public static bool IsDestroyedTarget(Delegate listener)
        {
            return listener.Target is UnityEngine.Object target && target == null;
        }

        /// <summary>
        /// "ClassName.MethodName" for log messages.
        /// </summary>
        public static string Describe(Delegate listener)
        {
            Type declaringType = listener.Method.DeclaringType;
            return declaringType != null ? $"{declaringType.Name}.{listener.Method.Name}" : listener.Method.Name;
        }
    }
}
