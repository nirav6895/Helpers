#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Global registry of services, looked up by the type they were registered under.
    /// <para>
    /// Register a service under the interface callers will ask for:
    /// <c>ServiceLocator.Add&lt;IAudioService&gt;(this)</c>. Plain <c>Add(this)</c> registers the concrete class,
    /// so <c>Get&lt;IAudioService&gt;()</c> wouldn't find it.
    /// A MonoBehaviour service should call <c>ServiceLocator.Remove(this)</c> in OnDestroy; that only removes
    /// registrations that still point at it, never a replacement registered by a newer instance.
    /// </para>
    /// Main thread only.
    /// </summary>
    public static class ServiceLocator
    {
        #region Variables
        private static readonly Dictionary<Type, IService> services = new();
        #endregion


        #region Constructors
        static ServiceLocator()
        {
            StaticStateReset.Register(Clear);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Registers <paramref name="instance"/> under <typeparamref name="T"/>, replacing any previous registration.
        /// </summary>
        public static void Add<T>(T instance) where T : class, IService
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            services[typeof(T)] = instance;
        }

        /// <summary>
        /// Gets the service registered under <typeparamref name="T"/>. Logs an error and returns null if there isn't one.
        /// Use <see cref="TryGet{T}"/> for optional services.
        /// </summary>
        public static T Get<T>() where T : class, IService
        {
            if (TryGet(out T service))
                return service;

            Debug.LogError(BuildNotRegisteredMessage(typeof(T)));
            return null;
        }

        /// <summary>
        /// Gets the service registered under <typeparamref name="T"/>, if any.
        /// A registered Unity object that has been destroyed counts as missing and is removed.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class, IService
        {
            if (services.TryGetValue(typeof(T), out IService registered))
            {
                // Through an interface type even "== null" can't see that a Unity object was destroyed.
                if (registered is UnityEngine.Object unityObject && unityObject == null)
                {
                    services.Remove(typeof(T));
                    Debug.LogWarning($"[ServiceLocator] The {typeof(T).Name} service was destroyed without being removed. Call ServiceLocator.Remove(this) in its OnDestroy.");
                }
                else
                {
                    service = (T)registered;
                    return true;
                }
            }

            service = null;
            return false;
        }

        /// <summary>
        /// True if a live service is registered under <typeparamref name="T"/>.
        /// </summary>
        public static bool Contains<T>() where T : class, IService
        {
            return TryGet<T>(out _);
        }

        /// <summary>
        /// Removes every registration that points at <paramref name="instance"/>. Registrations that were
        /// replaced by another instance are left alone, so a service being destroyed can't unregister its successor.
        /// </summary>
        /// <returns>True if anything was removed.</returns>
        public static bool Remove(IService instance)
        {
            if (instance == null)
                return false;

            List<Type> keys = null;
            foreach (KeyValuePair<Type, IService> pair in services)
            {
                if (ReferenceEquals(pair.Value, instance))
                    (keys ??= new List<Type>()).Add(pair.Key);
            }

            if (keys == null)
                return false;

            foreach (Type key in keys)
                services.Remove(key);

            return true;
        }

        /// <summary>
        /// Removes whatever is registered under <typeparamref name="T"/>.
        /// To unregister a specific instance, use <see cref="Remove(IService)"/>.
        /// </summary>
        /// <returns>True if something was registered.</returns>
        public static bool Remove<T>() where T : class, IService
        {
            return services.Remove(typeof(T));
        }

        /// <summary>
        /// Removes every registration.
        /// </summary>
        public static void Clear()
        {
            services.Clear();
        }
        #endregion


        #region Private Methods
        private static string BuildNotRegisteredMessage(Type requested)
        {
            // Common mistake: Add(this) registers the concrete class, but callers ask for the interface.
            foreach (Type registered in services.Keys)
            {
                if (registered != requested && requested.IsAssignableFrom(registered))
                    return $"[ServiceLocator] {requested.Name} is not registered, but {registered.Name} (which implements it) is registered under its own type. Register it with ServiceLocator.Add<{requested.Name}>(...).";
            }

            return $"[ServiceLocator] {requested.Name} is not registered.";
        }
        #endregion
    }
}
