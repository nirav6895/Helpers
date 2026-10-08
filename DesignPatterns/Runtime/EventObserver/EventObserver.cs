#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.EventObserver
{
    /// <summary>
    /// Event Observer to manage events using IEvent interface as a key.
    /// </summary>
    public static class EventObserver
    {
        #region Variables
        /// <summary>
        /// Contains all Events which use Type as a key.
        /// Used List<Delegate> to store multiple delegates with different method structure for the same key.
        /// </summary>
        private static readonly Dictionary<Type, List<Delegate>> eventDictionary = new();
        #endregion


        #region Public Methods
        /// <summary>
        /// Register a callback with IEvent Type argument. Type of IEvent is used as a key.
        /// </summary>
        /// <param name="callback">Callback with IEvent Type argument</param>
        public static void Register<T>(Action<T> callback) where T : IEvent
        {
            Type typeFromHandle = typeof(T);
            if (!eventDictionary.ContainsKey(typeFromHandle))
            {
                eventDictionary[typeFromHandle] = new List<Delegate>();
            }

            eventDictionary[typeFromHandle].Add(callback);
        }

        /// <summary>
        /// Unregister a callback with IEvent Type argument, if registered any before. Type of IEvent is used as a key.
        /// </summary>
        /// <param name="callback">Callback with IEvent Type argument</param>
        public static void Unregister<T>(Action<T> callback) where T : IEvent
        {
            Type typeFromHandle = typeof(T);
            if (eventDictionary.ContainsKey(typeFromHandle))
            {
                eventDictionary[typeFromHandle].RemoveAll(cb => cb.Equals(callback));
            }
        }

        /// <summary>
        /// Execute all callbacks for given IEvent Type.
        /// </summary>
        /// <param name="arg">Pass argument in callback</param>
        public static void Execute<T>(T arg) where T : IEvent
        {
            Type typeFromHandle = typeof(T);

            if (!eventDictionary.TryGetValue(typeFromHandle, out List<Delegate> allDelegates) || allDelegates == null)
                return;

            foreach (Delegate item in allDelegates.ToList())
            {
                try
                {
                    (item as Action<T>)?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventObserver] Exception while executing {typeFromHandle.Name} listener: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Executes all registered event listeners for the runtime type of the given IEvent instance.
        /// This overload is used when the concrete event type is not known at compile time,
        /// such as when triggering events via serialized references or dynamically.
        /// </summary>
        /// <param name="arg">The event instance to dispatch to all registered listeners.</param>
        public static void Execute(IEvent arg)
        {
            if (arg == null) return;

            Type eventType = arg.GetType();

            if (!eventDictionary.TryGetValue(eventType, out List<Delegate> allDelegates) || allDelegates == null)
                return;

            foreach (Delegate item in allDelegates.ToList())
            {
                try
                {
                    item.DynamicInvoke(arg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventObserver] Exception while executing {eventType.Name} listener: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear All Events
        /// </summary>
        public static void Dispose()
        {
            eventDictionary.Clear();
        }
        #endregion
    }
}