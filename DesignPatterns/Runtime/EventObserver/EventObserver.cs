#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Global event bus keyed by event type. Define an event as a class or struct implementing <see cref="IEvent"/>,
    /// register callbacks for it and execute it with an instance.
    /// <para>
    /// Listeners receive events whose runtime type is exactly the type they registered for
    /// (no dispatch to base classes or interfaces).
    /// A listener that throws is logged and doesn't stop the others. Unregister listeners when their owner is
    /// disabled or destroyed; methods of destroyed Unity objects are removed automatically, with a warning.
    /// </para>
    /// Main thread only.
    /// </summary>
    public static class EventObserver
    {
        #region Variables
        /// <summary>
        /// One channel per event type.
        /// </summary>
        private static readonly Dictionary<Type, IChannel> channels = new();
        #endregion


        #region Constructors
        static EventObserver()
        {
            StaticStateReset.Register(Clear);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Registers a callback for event type <typeparamref name="T"/>. Registering the same callback twice is ignored.
        /// </summary>
        public static void Register<T>(Action<T> callback) where T : IEvent
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (typeof(T).IsInterface || typeof(T).IsAbstract)
                Debug.LogWarning($"[EventObserver] {ListenerUtility.Describe(callback)} registered for {typeof(T).Name}, which is abstract or an interface. Events are dispatched by their exact runtime type, so this listener will never be called. Register for the concrete event type.");

            if (!GetOrCreateChannel<T>().Listeners.Add(callback))
                Debug.LogWarning($"[EventObserver] {ListenerUtility.Describe(callback)} is already registered for {typeof(T).Name}; the duplicate registration was ignored.");
        }

        /// <summary>
        /// Unregisters a callback. Does nothing if it isn't registered.
        /// </summary>
        public static void Unregister<T>(Action<T> callback) where T : IEvent
        {
            if (callback != null && channels.TryGetValue(typeof(T), out IChannel channel))
                ((Channel<T>)channel).Listeners.Remove(callback);
        }

        /// <summary>
        /// Calls every callback registered for the runtime type of <paramref name="arg"/>.
        /// </summary>
        public static void Execute<T>(T arg) where T : IEvent
        {
            if (arg == null)
                return;

            // A struct's static type is its runtime type; skipping GetType() avoids boxing it.
            Type eventType = typeof(T).IsValueType ? typeof(T) : arg.GetType();
            if (!channels.TryGetValue(eventType, out IChannel channel))
                return;

            if (channel is Channel<T> exactChannel)
                exactChannel.Execute(arg);
            else
                channel.Execute(arg);   // T is a base class or interface of the event's type
        }

        /// <summary>
        /// Calls every callback registered for the runtime type of <paramref name="arg"/>.
        /// Use this when the event's type isn't known at compile time, e.g. an event held in an IEvent field.
        /// </summary>
        public static void Execute(IEvent arg)
        {
            if (arg != null && channels.TryGetValue(arg.GetType(), out IChannel channel))
                channel.Execute(arg);
        }

        /// <summary>
        /// True if at least one callback is registered for <typeparamref name="T"/>.
        /// </summary>
        public static bool HasListeners<T>() where T : IEvent
        {
            return channels.TryGetValue(typeof(T), out IChannel channel) && channel.ListenerCount > 0;
        }

        /// <summary>
        /// Unregisters every callback for every event type.
        /// </summary>
        public static void Clear()
        {
            channels.Clear();
        }
        #endregion


        #region Private Methods
        private static Channel<T> GetOrCreateChannel<T>() where T : IEvent
        {
            if (channels.TryGetValue(typeof(T), out IChannel channel))
                return (Channel<T>)channel;

            var created = new Channel<T>();
            channels.Add(typeof(T), created);
            return created;
        }
        #endregion


        #region Nested Types
        private interface IChannel
        {
            int ListenerCount { get; }
            void Execute(IEvent arg);
        }

        /// <summary>
        /// Listeners of one event type. Calling them through the typed channel avoids reflection.
        /// </summary>
        private sealed class Channel<T> : IChannel where T : IEvent
        {
            public readonly ListenerList<Action<T>> Listeners = new();

            public int ListenerCount => Listeners.Count;

            public void Execute(T arg)
            {
                Action<T>[] snapshot = Listeners.Items;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    Action<T> listener = snapshot[i];
                    if (ListenerUtility.IsDestroyedTarget(listener))
                    {
                        Listeners.Remove(listener);
                        Debug.LogWarning($"[EventObserver] Removed {ListenerUtility.Describe(listener)} from {typeof(T).Name}: its object was destroyed without unregistering.");
                        continue;
                    }

                    try
                    {
                        listener(arg);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, listener.Target as UnityEngine.Object);
                    }
                }
            }

            void IChannel.Execute(IEvent arg)
            {
                Execute((T)arg);
            }
        }
        #endregion
    }
}
