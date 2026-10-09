#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// In-memory store for session data: at most one object per type, shared across scenes for the lifetime
    /// of the app. Nothing is written to disk, so the data is gone after a restart.
    /// <para>
    /// Objects are keyed by their concrete runtime type. An AudioSettings stored through a BaseSettings variable
    /// is found with Get&lt;AudioSettings&gt;(), not Get&lt;BaseSettings&gt;().
    /// </para>
    /// Main thread only.
    /// </summary>
    public static class SessionStore
    {
        #region Variables
        private static readonly Dictionary<Type, ISessionData> entries = new();
        #endregion


        #region Constructors
        static SessionStore()
        {
            StaticStateReset.Register(Clear);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Stores <paramref name="data"/> under its concrete type, replacing any previous object of that type.
        /// </summary>
        public static void Set(ISessionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            entries[data.GetType()] = data;
        }

        /// <summary>
        /// Gets the stored object of type <typeparamref name="T"/>, or null if there isn't one.
        /// </summary>
        public static T Get<T>() where T : class, ISessionData
        {
            return TryGet(out T data) ? data : null;
        }

        /// <summary>
        /// Gets the stored object of type <typeparamref name="T"/>, if any.
        /// </summary>
        public static bool TryGet<T>(out T data) where T : class, ISessionData
        {
            if (entries.TryGetValue(typeof(T), out ISessionData stored))
            {
                data = (T)stored;
                return true;
            }

            data = null;
            return false;
        }

        /// <summary>
        /// Gets the stored object of type <typeparamref name="T"/>. If there isn't one, creates it, stores it and returns it.
        /// </summary>
        public static T GetOrCreate<T>() where T : class, ISessionData, new()
        {
            if (TryGet(out T data))
                return data;

            data = new T();
            entries[typeof(T)] = data;
            return data;
        }

        /// <summary>
        /// True if an object of type <typeparamref name="T"/> is stored.
        /// </summary>
        public static bool Contains<T>() where T : class, ISessionData
        {
            return entries.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Removes the stored object of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>True if there was one.</returns>
        public static bool Remove<T>() where T : class, ISessionData
        {
            return entries.Remove(typeof(T));
        }

        /// <summary>
        /// Removes every stored object.
        /// </summary>
        public static void Clear()
        {
            entries.Clear();
        }
        #endregion
    }
}
