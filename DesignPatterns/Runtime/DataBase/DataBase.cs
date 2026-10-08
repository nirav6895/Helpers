#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns.DataBase
{
    /// <summary>
    /// A globally accessible, type-safe session state manager for storing and retrieving ISavable objects during runtime.
    /// Objects are managed using their type as the unique key.
    /// </summary>
    public static class DataBase
    {
        /// <summary>
        /// Internal dictionary storing session objects using their type as the key.
        /// </summary>
        private static readonly Dictionary<Type, IData> sessionData = new();

        /// <summary>
        /// Saves an ISavable object using its static type as the key.
        /// If the type already exists in the session, the value will be overwritten.
        /// </summary>
        /// <typeparam name="T">The type of the ISavable object.</typeparam>
        /// <param name="data">The object to be saved.</param>
        public static void Save<T>(T data) where T : IData
        {
            sessionData[typeof(T)] = data;
        }

        /// <summary>
        /// Saves an ISavable object using its runtime type as the key.
        /// Useful when the static type is not known, such as when calling from an interface or base class.
        /// </summary>
        /// <param name="data">The ISavable object to save.</param>
        public static void Save(IData data)
        {
            sessionData[data.GetType()] = data;
        }

        /// <summary>
        /// Loads an ISavable object of the given type from the session.
        /// If no object is found, a new instance is created, stored, and returned.
        /// </summary>
        /// <typeparam name="T">The type of the ISavable object to load.</typeparam>
        /// <returns>An instance of the requested type, either loaded or newly created.</returns>
        public static T Load<T>() where T : IData, new()
        {
            var type = typeof(T);

            if (sessionData.TryGetValue(type, out var data))
                return (T)data;

            var newInstance = new T();
            sessionData[type] = newInstance;
            return newInstance;
        }

        /// <summary>
        /// Removes a saved ISavable object of the specified type from the session.
        /// </summary>
        /// <typeparam name="T">The type of the object to remove.</typeparam>
        public static void Remove<T>() where T : IData
        {
            sessionData.Remove(typeof(T));
        }

        /// <summary>
        /// Clears all objects from the session state.
        /// </summary>
        public static void Dispose()
        {
            sessionData.Clear();
        }
    }
}
