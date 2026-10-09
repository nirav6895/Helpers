#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.Samples
{
    /// <summary>
    /// State machine for server-driven, turn-based games in which every server event carries an increasing counter.
    /// Send server events through <see cref="ExecuteCountedEvent"/>; local events still use ExecuteEvent.
    /// <list type="bullet">
    /// <item>Duplicate or stale events (counter already processed or already buffered) are ignored.</item>
    /// <item>An event the current state can handle runs immediately. Buffered events with a lower counter are then discarded as stale.</item>
    /// <item>Any other event is buffered in counter order. After each state change, the lowest buffered event runs if the new state can handle it.</item>
    /// <item>Once <see cref="SyncThreshold"/> events are buffered, <see cref="SyncRequested"/> is raised (at most once per frame) so the game can resync with the server.</item>
    /// </list>
    /// Master transitions, restart and stop clear the buffer. Restart also resets <see cref="LastProcessedCounter"/>;
    /// set it afterwards if the server's counter carries on from where it was.
    /// </summary>
    public class CounterBasedStateMachine<TEvent> : StateMachine<TEvent>
    {
        #region Variables
        private readonly SortedList<int, BufferedEvent> bufferedEvents = new();
        private int lastSyncFrame = -1;
        #endregion


        #region Properties
        /// <summary>
        /// Counter of the last server event that was executed. Events with a counter at or below it are ignored.
        /// </summary>
        public int LastProcessedCounter { get; set; } = -1;

        /// <summary>
        /// Number of buffered events that triggers <see cref="SyncRequested"/>.
        /// </summary>
        public int SyncThreshold { get; set; } = 3;

        /// <summary>
        /// Number of server events waiting for a state that can handle them.
        /// </summary>
        public int BufferedEventCount => bufferedEvents.Count;
        #endregion


        #region Events
        /// <summary>
        /// Raised when too many server events are waiting, meaning the client is out of sync.
        /// </summary>
        public event Action SyncRequested;
        #endregion


        #region Constructors
        public CounterBasedStateMachine() { }

        public CounterBasedStateMachine(string name, IEqualityComparer<TEvent> eventComparer = null) : base(name, eventComparer) { }
        #endregion


        #region Public Methods
        /// <summary>
        /// Executes a server event in counter order.
        /// </summary>
        /// <param name="counter">The event's turn counter from the server.</param>
        public void ExecuteCountedEvent(TEvent eventKey, int counter, object data = null)
        {
            if (counter <= LastProcessedCounter || bufferedEvents.ContainsKey(counter))
            {
                if (DebugMode)
                    LogDebug($"'{eventKey}' #{counter} ignored: already received (last processed #{LastProcessedCounter})");
                return;
            }

            if (!IsProcessing && CanHandle(eventKey))
            {
                MarkProcessed(counter);
                ExecuteEvent(eventKey, data);
                return;
            }

            bufferedEvents.Add(counter, new BufferedEvent(eventKey, data));
            if (DebugMode)
                LogDebug($"'{eventKey}' #{counter} buffered ({bufferedEvents.Count} waiting)");

            // Received from inside a state callback: check the buffer again once the machine is idle.
            if (IsProcessing)
                ScheduleSettle();

            // After pause/resume the server sends many events in one frame; request one sync per frame at most.
            if (bufferedEvents.Count >= SyncThreshold && lastSyncFrame != Time.frameCount)
            {
                lastSyncFrame = Time.frameCount;
                if (DebugMode)
                    LogDebug($"{bufferedEvents.Count} events waiting; requesting a sync");

                SyncRequested?.Invoke();
            }
        }

        public override void ClearDeferredEvents()
        {
            base.ClearDeferredEvents();

            if (DebugMode && bufferedEvents.Count > 0)
                LogDebug($"Cleared {bufferedEvents.Count} buffered server event(s)");

            bufferedEvents.Clear();
        }
        #endregion


        #region Overridden Methods
        protected override void OnStateSettled()
        {
            base.OnStateSettled();

            // Wait for anything the base class queued; this runs again after the next state change.
            if (PendingEventCount > 0 || bufferedEvents.Count == 0)
                return;

            int counter = bufferedEvents.Keys[0];
            BufferedEvent next = bufferedEvents.Values[0];
            if (!CanHandle(next.EventKey))
                return;

            bufferedEvents.RemoveAt(0);
            MarkProcessed(counter);
            if (DebugMode)
                LogDebug($"Running buffered '{next.EventKey}' #{counter}");

            // We are inside the machine's processing loop, so this is queued and runs next.
            ExecuteEvent(next.EventKey, next.Data);
        }

        protected override void OnRestarting()
        {
            base.OnRestarting();
            LastProcessedCounter = -1;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Records <paramref name="counter"/> as processed and drops older buffered events, which are now stale.
        /// </summary>
        private void MarkProcessed(int counter)
        {
            LastProcessedCounter = counter;

            while (bufferedEvents.Count > 0 && bufferedEvents.Keys[0] < counter)
            {
                if (DebugMode)
                    LogDebug($"Dropped stale '{bufferedEvents.Values[0].EventKey}' #{bufferedEvents.Keys[0]} (now at #{counter})");

                bufferedEvents.RemoveAt(0);
            }
        }
        #endregion


        #region Nested Types
        private readonly struct BufferedEvent
        {
            public readonly TEvent EventKey;
            public readonly object Data;

            public BufferedEvent(TEvent eventKey, object data)
            {
                EventKey = eventKey;
                Data = data;
            }
        }
        #endregion
    }
}
