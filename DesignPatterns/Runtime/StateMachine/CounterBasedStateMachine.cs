#region Using
using System;
using System.Collections;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.StateMachine
{
    /// <Summary> 
    /// This State Machine is specially designed to executes events based on counter attached with that event.
    /// In this state machine, each event has "counter" as id for sorting list.
    /// </Summary>
    public class CounterBasedStateMachine : StateMachine
    {
        #region Variables
        /// <Summary> 
        /// Sorted Events based on counter. This list only contains those event which comes with "Turn Counter" and
        /// this types of events only comes from server.
        /// </Summary>
        SortedList sortedEvents = null;

        /// <Summary> 
        /// Last Counter to check whether game is synced or not with "Turn Counter" coming from server
        /// </Summary>
        int lastCounter = 0;

        /// <Summary> 
        /// On Sync Counter when "Turn Counter" coming from server is not synced with local counter.
        /// </Summary>
        public Action onSyncCounter = null;

        /// <Summary> 
        /// Tells whether SyncCounter is running or not
        /// </Summary>
        int lastRunningSyncCounterFrame = -1;
        #endregion


        #region Overridden Methods
        public override void Initialize()
        {
            base.Initialize();

            sortedEvents = new SortedList();
        }

        public override void ExecuteEvent(string eventName, object data = null, int counter = -1)
        {
            UpdateLastCounter(counter);
            base.ExecuteEvent(eventName, data, counter);
        }
        #endregion


        #region Queue Related Overridden Methods
        public override void AddEventInQueue(TransitionEvent transitionEvent)
        {
            if (transitionEvent.counter >= 0)
            {
                DebugLog("Event : " + transitionEvent.eventName + " with Counter : " + transitionEvent.counter + " added in Queued events");
                sortedEvents.Add(transitionEvent.counter, transitionEvent);

                // If more than 3 events are in queue, then Do Game Sync
                if (sortedEvents.Count >= 3)
                {
                    // On Pause and Resume game, all the recent events comes from server in 1 time frame so
                    // to avoid multiple calls of OnSyncCounter, added 1 frame delay.
                    if (lastRunningSyncCounterFrame == Time.frameCount)
                    {
                        DebugLogWarning("Already Running Sync Counter in this frame, so skipping this call");
                    }
                    else
                    {
                        lastRunningSyncCounterFrame = Time.frameCount;
                        DebugLogWarning("Running Sync Counter as more than 3 events are in queue");
                        onSyncCounter?.Invoke();
                    }
                }
            }
        }

        public override TransitionEvent PeekEventFromQueue()
        {
            TransitionEvent transitionEvent = null;
            if (sortedEvents.Count > 0)
            {
                transitionEvent = sortedEvents.GetByIndex(0) as TransitionEvent;
            }
            return transitionEvent;
        }

        public override void RemoveEventFromQueue(TransitionEvent transitionEvent)
        {
            DebugLog("Executing Queued Event : " + transitionEvent.eventName + " with Counter : " + transitionEvent.counter + ", which was added in queue");
            sortedEvents.RemoveAt(0);
        }

        public override void RemoveOldQueuedEvents(int counter)
        {
            base.RemoveOldQueuedEvents(counter);
            TransitionEvent transitionEvent = PeekEventFromQueue();
            while (transitionEvent != null && transitionEvent.counter < counter)
            {
                DebugLog("Removed Old Queued Event : " + transitionEvent.eventName + " with Counter : " + transitionEvent.counter + " as current counter is " + counter);
                sortedEvents.RemoveAt(0);
                transitionEvent = PeekEventFromQueue();
            }
            DebugLog("Finished Removing Old Queued Event.");
        }

        public override void ClearAllQueuedEvents()
        {
            if (sortedEvents.Count > 0)
                DebugLog("Forcefully Cleared all Queued events");
            sortedEvents.Clear();
        }
        #endregion


        #region Private Methods
        private void UpdateLastCounter(int counter)
        {
            if (counter != -1)
            {
                if (counter != lastCounter + 1)
                {
                    DebugLog("Counter is switched from " + lastCounter + " to " + counter + " directly.");
                    lastCounter = counter;
                }
                else
                    lastCounter++;
            }
        }
        #endregion
    }
}