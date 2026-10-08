#region Using
using System;
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns.ServiceLocator
{
    public static class ServiceLocator
    {
        #region Variables
        private static readonly Dictionary<Type, object> serviceMapping = new();
        private static readonly object lockObject = new();
        #endregion


        #region Methods
        public static void Add<T>(T instance) where T : class, IService
        {
            lock (lockObject)
            {
                if (!serviceMapping.ContainsKey(typeof(T)))
                    serviceMapping.Add(typeof(T), instance);
                else
                    serviceMapping[typeof(T)] = instance;
            }
        }

        public static T Get<T>() where T : class, IService
        {
            lock (lockObject)
            {
                if (serviceMapping.TryGetValue(typeof(T), out var service))
                    return service as T;

                // Try to auto-locate MonoBehaviour-based services
                // if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                // {
                //     if (UnityEngine.Object.FindAnyObjectByType<T>() is T foundService)
                //     {
                //         serviceMapping[typeof(T)] = foundService;
                //         return foundService;
                //     }

                //     Debug.LogWarning($"[ServiceLocator] MonoBehaviour Service of type {typeof(T).Name} not found in scene.");
                // }
                // else
                // {
                //     Debug.LogWarning($"[ServiceLocator] Non-MonoBehaviour service of type {typeof(T).Name} is not registered.");
                // }

                return null;
            }
        }

        public static bool Contains<T>() where T : class, IService
        {
            lock (lockObject)
            {
                return serviceMapping.ContainsKey(typeof(T));
            }
        }

        public static void Remove<T>() where T : class, IService
        {
            lock (lockObject)
            {
                serviceMapping.Remove(typeof(T));
            }
        }

        public static void Dispose()
        {
            lock (lockObject)
            {
                serviceMapping.Clear();
            }
        }
        #endregion
    }
}