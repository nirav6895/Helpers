#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.EventObserver
{
    /// <summary>
    /// Event Observer to manage events using Enum as a key.
    /// </summary>
    public static class EventObserver<TEnum> where TEnum : Enum
    {
        #region Variables
        /// <summary>
        /// Contains all Events which use Enum as a key.
        /// Used List<Delegate> to store multiple delegates with different method structure for the same key.
        /// </summary>
        private static readonly Dictionary<TEnum, List<Delegate>> eventDictionary = new();
        #endregion


        #region Public Methods
        /// <summary>
        /// Register a callback for a specified event key.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Callback with no arguments</param>
        public static void Register(TEnum key, Action callback)
        {
            RegisterDelegate(key, callback);
        }

        /// <summary>
        /// Register a callback for a specified event key.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Callback with 1 argument of any value type data type like bool, int, float etc</param>
        public static void Register<T>(TEnum key, Action<T> callback) where T : struct
        {
            RegisterDelegate(key, callback);
        }

        /// <summary>
        /// Unregister a callback for a specified event key if registered any before.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Callback with no arguments</param>
        public static void Unregister(TEnum key, Action callback)
        {
            UnregisterDelegate(key, callback);
        }

        /// <summary>
        /// Unregister a callback for a specified event key if registered any before.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Callback with 1 argument of any value type data type like bool, int, float etc</param>
        public static void Unregister<T>(TEnum key, Action<T> callback) where T : struct
        {
            UnregisterDelegate(key, callback);
        }

        /// <summary>
        /// Execute all callbacks for a specified event key.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        public static void Execute(TEnum key)
        {
            if (!eventDictionary.TryGetValue(key, out List<Delegate> allDelegates) || allDelegates == null)
                return;

            // Make a snapshot to avoid invalid modification during iteration
            foreach (Delegate item in allDelegates.ToList())
            {
                try
                {
                    (item as Action)?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventObserver] Exception while executing listener for {key}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Execute all callbacks for a specified event key.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="arg">Pass argument in callback</param>
        public static void Execute<T>(TEnum key, T arg) where T : struct
        {
            if (!eventDictionary.TryGetValue(key, out List<Delegate> allDelegates) || allDelegates == null)
            {
                return;
            }

            foreach (Delegate item in allDelegates.ToList())
            {
                try
                {
                    (item as Action<T>)?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventObserver] Exception while executing listener for {key}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear All Events.
        /// </summary>
        public static void Dispose()
        {
            eventDictionary.Clear();
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Register a callback for a specified event key with any kind of delegate.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Any kind of Delegate</param>
        private static void RegisterDelegate(TEnum key, Delegate callback)
        {
            if (!eventDictionary.ContainsKey(key))
            {
                eventDictionary[key] = new List<Delegate>();
            }

            eventDictionary[key].Add(callback);
        }

        /// <summary>
        /// Unregister a callback for a specified event key with any kind of delegate.
        /// </summary>
        /// <param name="key">Key in Enum Format</param>
        /// <param name="callback">Any kind of Delegate</param>
        private static void UnregisterDelegate(TEnum key, Delegate callback)
        {
            if (eventDictionary.ContainsKey(key))
            {
                eventDictionary[key].RemoveAll(cb => cb.Equals(callback));
            }
        }
        #endregion
    }
}