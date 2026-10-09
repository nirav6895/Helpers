#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Global event bus keyed by an enum value. Simple to use, but the key carries no payload type: prefer
    /// <see cref="EventObserver"/> with <see cref="IEvent"/> types for events with data, where a payload
    /// mismatch can't compile.
    /// <para>
    /// A key may have listeners with different signatures: Execute(key) reaches the Action listeners and
    /// Execute(key, arg) reaches the Action&lt;T&gt; listeners whose T matches exactly. If an Execute reaches none of
    /// a key's listeners, a warning lists what they expect, which catches mistakes such as sending an int to
    /// float listeners.
    /// </para>
    /// Main thread only.
    /// </summary>
    public static class EventObserver<TEnum> where TEnum : Enum
    {
        #region Variables
        /// <summary>
        /// Listeners per key. Stored as Delegate so one key can have listeners with different signatures.
        /// </summary>
        private static readonly Dictionary<TEnum, ListenerList<Delegate>> eventDictionary = new();
        #endregion


        #region Constructors
        static EventObserver()
        {
            StaticStateReset.Register(Clear);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Registers a callback without a payload. Registering the same callback twice is ignored.
        /// </summary>
        public static void Register(TEnum key, Action callback)
        {
            RegisterDelegate(key, callback);
        }

        /// <summary>
        /// Registers a callback that receives a payload of type <typeparamref name="T"/>.
        /// Registering the same callback twice is ignored.
        /// </summary>
        public static void Register<T>(TEnum key, Action<T> callback)
        {
            RegisterDelegate(key, callback);
        }

        /// <summary>
        /// Unregisters a callback. Does nothing if it isn't registered.
        /// </summary>
        public static void Unregister(TEnum key, Action callback)
        {
            UnregisterDelegate(key, callback);
        }

        /// <summary>
        /// Unregisters a callback. Does nothing if it isn't registered.
        /// </summary>
        public static void Unregister<T>(TEnum key, Action<T> callback)
        {
            UnregisterDelegate(key, callback);
        }

        /// <summary>
        /// Calls every payload-less callback registered for <paramref name="key"/>.
        /// </summary>
        public static void Execute(TEnum key)
        {
            if (!eventDictionary.TryGetValue(key, out ListenerList<Delegate> listeners))
                return;

            Delegate[] snapshot = listeners.Items;
            int alive = 0, delivered = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                Delegate listener = snapshot[i];
                if (RemoveIfDestroyed(key, listeners, listener))
                    continue;

                alive++;
                if (listener is Action action)
                {
                    delivered++;
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, listener.Target as UnityEngine.Object);
                    }
                }
            }

            if (alive > 0 && delivered == 0)
                WarnNoMatchingListener(key, "no payload", snapshot);
        }

        /// <summary>
        /// Calls every callback registered for <paramref name="key"/> with payload type exactly <typeparamref name="T"/>.
        /// </summary>
        public static void Execute<T>(TEnum key, T arg)
        {
            if (!eventDictionary.TryGetValue(key, out ListenerList<Delegate> listeners))
                return;

            Delegate[] snapshot = listeners.Items;
            int alive = 0, delivered = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                Delegate listener = snapshot[i];
                if (RemoveIfDestroyed(key, listeners, listener))
                    continue;

                alive++;
                if (listener is Action<T> action)
                {
                    delivered++;
                    try
                    {
                        action(arg);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, listener.Target as UnityEngine.Object);
                    }
                }
            }

            if (alive > 0 && delivered == 0)
                WarnNoMatchingListener(key, $"a {typeof(T).Name} payload", snapshot);
        }

        /// <summary>
        /// Unregisters every callback for every key of <typeparamref name="TEnum"/>.
        /// </summary>
        public static void Clear()
        {
            eventDictionary.Clear();
        }
        #endregion


        #region Private Methods
        private static void RegisterDelegate(TEnum key, Delegate callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!eventDictionary.TryGetValue(key, out ListenerList<Delegate> listeners))
            {
                listeners = new ListenerList<Delegate>();
                eventDictionary.Add(key, listeners);
            }

            if (!listeners.Add(callback))
                Debug.LogWarning($"[EventObserver<{typeof(TEnum).Name}>] {ListenerUtility.Describe(callback)} is already registered for {key}; the duplicate registration was ignored.");
        }

        private static void UnregisterDelegate(TEnum key, Delegate callback)
        {
            if (callback != null && eventDictionary.TryGetValue(key, out ListenerList<Delegate> listeners))
                listeners.Remove(callback);
        }

        private static bool RemoveIfDestroyed(TEnum key, ListenerList<Delegate> listeners, Delegate listener)
        {
            if (!ListenerUtility.IsDestroyedTarget(listener))
                return false;

            listeners.Remove(listener);
            Debug.LogWarning($"[EventObserver<{typeof(TEnum).Name}>] Removed {ListenerUtility.Describe(listener)} from {key}: its object was destroyed without unregistering.");
            return true;
        }

        private static void WarnNoMatchingListener(TEnum key, string sent, Delegate[] listeners)
        {
            var expected = new HashSet<string>();
            foreach (Delegate listener in listeners)
            {
                Type[] payload = listener.GetType().GetGenericArguments();
                expected.Add(payload.Length == 0 ? "no payload" : $"a {payload[0].Name} payload");
            }

            Debug.LogWarning($"[EventObserver<{typeof(TEnum).Name}>] {key} was executed with {sent}, but none of its listeners accept that (they expect {string.Join(" or ", expected)}). Check the payload type.");
        }
        #endregion
    }
}
