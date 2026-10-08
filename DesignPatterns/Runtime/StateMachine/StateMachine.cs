#region Using
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns.StateMachine
{
    /// <Summary> 
    /// State Machine is consist of predefined States and Transition between those states.
    /// We need to defines those states and transition in <see cref= Initialize>Initialize</see> Method.
    /// State Machine will switch it's state on run time, based on received events. 
    /// Those events will be also defined initially.
    /// </Summary>
    public class StateMachine
    {
        #region Inspector Variables
        [SerializeField]
        bool isDebugMode = false;
        #endregion

        #region Private Vairables
        /// <Summary> 
        /// Current State of State Machine
        /// </Summary>
        State curState = null;

        /// <Summary> 
        /// End State of State Machine
        /// </Summary>
        State endState = null;

        /// <Summary> 
        /// List of All States
        /// </Summary>
        List<State> allStates = null;

        /// <Summary> 
        /// Queue of Events Names which is executed by user but current state doesn't have transition for that event
        /// so added in queue so that can be used in next states.
        /// </Summary>
        Queue<TransitionEvent> queuedEvents = null;

        /// <Summary> 
        /// Dicationary of Master Transitions : Event Name and ToState
        /// Master Transitions are those which can transit from any state
        /// </Summary>
        Dictionary<string, State> masterTransitions = null;
        #endregion


        #region Constant Variables
        /// <Summary> 
        /// Auto Event is used for Auto Transition from one State to another.
        /// </Summary>
        public const string AUTO_EVENT = "Auto";
        #endregion


        #region Properties
        /// <Summary> 
        /// Public Access of curState
        /// </Summary>
        public State CurrentState
        {
            get
            {
                return curState;
            }
        }

        #endregion


        #region Unity Methods
        /// <Summary> 
        /// Keep Updating State
        /// </Summary>
        void Update()
        {
            if (curState != null)
            {
                curState.Update();
            }
        }
        #endregion


        #region Virtual Methods
        /// <Summary> 
        /// Defines All States and Transition between those State
        /// </Summary>
        public virtual void Initialize()
        {
            allStates = new List<State>();
            queuedEvents = new Queue<TransitionEvent>();
            masterTransitions = new Dictionary<string, State>();
        }

        /// <Summary> 
        /// Starts State Machine and Enters into first added State
        /// </Summary>
        public virtual void StartStateMachine()
        {
            if (allStates == null)
            {
                DebugLogWarning("Initialize State Machine before Start the State Machine");
                return;
            }

            if (allStates.Count == 0)
            {
                DebugLogWarning("Add minium 1 State to start the State Machine");
                return;
            }

            curState = allStates[0];
            curState.Enter();

            // Execute Auto Event if there is any
            ExecuteAutoEvent();
        }


        /// <Summary> 
        /// Restart State Machine from first added state
        /// </Summary>
        public virtual void RestartStateMachine(State startState = null)
        {
            // Reset Variables
            queuedEvents.Clear();

            // Set Current State
            curState = startState == null ? allStates[0] : startState;
            curState.Enter();

            // Execute Auto Event if there is any
            ExecuteAutoEvent();
        }

        /// <Summary> 
        /// Add State in State Machine
        /// </Summary>
        public virtual void AddState(string stateName, State state)
        {
            if (allStates.Contains(state))
            {
                DebugLogWarning("Already Conatins this state : " + state);
                return;
            }

            allStates.Add(state);
            state.Initialize(stateName, this);
        }

        /// <Summary> 
        /// Set End State
        /// </Summary>
        public virtual void SetEndState(State state)
        {
            endState = state;
        }

        /// <Summary> 
        /// Add Transition in State Machine
        /// </Summary>
        public virtual void AddTransition(State fromState, State toState, string eventName)
        {
            fromState.AddTransition(toState, eventName);
        }

        /// <Summary> 
        /// Add Auto Transition in State Machine
        /// </Summary>
        public virtual void AddAutoTransition(State fromState, State toState)
        {
            fromState.AddTransition(toState, AUTO_EVENT);
        }

        /// <Summary> 
        /// This Transition can be entered from any State. That's why its called Master Transition.
        /// </Summary>
        public virtual void AddMasterTransition(State toState, string eventName)
        {
            if (masterTransitions.ContainsKey(eventName))
            {
                DebugLogWarning("Already have Master transition with event Name : " + eventName);
                return;
            }
            masterTransitions.Add(eventName, toState);
        }

        /// <Summary> 
        /// Execute Event which will switch States internally
        /// </Summary>
        public virtual void ExecuteEvent(string eventName, object data = null, int counter = -1)
        {
            if (curState == endState)
                return;

            // If event is for Master Transition then execute it first
            if (masterTransitions.ContainsKey(eventName))
            {
                // Exit Current State
                curState.Exit();

                DebugLog("On Master Transition event : " + eventName + ", Switching State from " + curState.Name + " to " + masterTransitions[eventName].Name);

                // Clear All Queued Events
                ClearAllQueuedEvents();

                // Get Next State from Master transitions List and set as Current State
                curState = masterTransitions[eventName];
                curState.Enter(data);
            }
            // if Current State has transition for given eventName then Switch State
            else if (curState.transitions.ContainsKey(eventName))
            {
                // Exit Current State
                curState.Exit();

                if (counter != -1)
                    DebugLog("On event : " + eventName + " and Counter : " + counter + ", Switching State from " + curState.Name + " to " + curState.transitions[eventName].Name);
                else
                    DebugLog("On event : " + eventName + ", Switching State from " + curState.Name + " to " + curState.transitions[eventName].Name);

                // Get Next State from transitions List and set as Current State
                curState = curState.transitions[eventName];
                curState.Enter(data);

                // Remove Old Queued Events
                if (counter > 0)
                    RemoveOldQueuedEvents(counter);

                // Execute Queued Event if there is any
                ExecuteQueuedEvent();

                // Execute Auto Event if there is any
                ExecuteAutoEvent();
            }
            // Else Add Event in Queue
            else
            {
                AddEventInQueue(new TransitionEvent(eventName, data, counter));
            }
        }
        #endregion



        #region Queue Related Virtual Methods
        public virtual void AddEventInQueue(TransitionEvent transitionEvent)
        {
            DebugLog("Event : " + transitionEvent.eventName + " added in queue");
            queuedEvents.Enqueue(transitionEvent);
        }

        public virtual TransitionEvent PeekEventFromQueue()
        {
            if (queuedEvents.Count > 0)
                return queuedEvents.Peek();
            else
                return null;
        }

        public virtual void RemoveEventFromQueue(TransitionEvent transitionEvent)
        {
            DebugLog("Executing Queued Event : " + transitionEvent.eventName);
            queuedEvents.Dequeue();
        }

        /// <Summary> 
        /// Kept Empty Intentionally to let override this method in child class.
        /// </Summary>
        public virtual void RemoveOldQueuedEvents(int counter)
        {

        }

        public virtual void ClearAllQueuedEvents()
        {
            queuedEvents.Clear();
        }
        #endregion


        #region Private Methods
        /// <Summary> 
        /// Executes Queued Event if there is any
        /// </Summary>
        void ExecuteQueuedEvent()
        {
            TransitionEvent transitionEvent = PeekEventFromQueue();
            if (transitionEvent != null)
            {
                if (curState.transitions.ContainsKey(transitionEvent.eventName))
                {
                    RemoveEventFromQueue(transitionEvent);
                    ExecuteEvent(transitionEvent.eventName, transitionEvent.data);
                }
            }
        }

        /// <Summary> 
        /// Executes Auto Event if there is any
        /// </Summary>
        void ExecuteAutoEvent()
        {
            if (curState.transitions.ContainsKey(AUTO_EVENT))
            {
                DebugLog("Executing Auto Event");
                ExecuteEvent(AUTO_EVENT);
            }
        }
        #endregion


        #region Debug Methods
        public void DebugLog(string msg)
        {
            if (isDebugMode)
            {
                Debug.LogFormat("[State Machine] {0} at time : {1}", msg, Time.time);
            }
        }

        public void DebugLogWarning(string msg)
        {
            if (isDebugMode)
            {
                Debug.LogWarningFormat("[State Machine] {0} at time : {1}", msg, Time.time);
            }
        }
        #endregion
    }

    /// <Summary> 
    /// Transition Event Data. This will help us in storing received event and data attached to that event, 
    /// for later use as this state Machine used Queue Events mechanism
    /// </Summary>
    public class TransitionEvent
    {
        public string eventName;
        public object data;
        public int counter;

        public TransitionEvent(string eventName, object data, int counter)
        {
            this.eventName = eventName;
            this.data = data;
            this.counter = counter;
        }
    }
}