#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
#endregion

namespace NG.DesignPatterns
{
    /// <summary>
    /// Finite state machine driven by events of type <typeparamref name="TEvent"/>, such as an enum or a string.
    /// <para>
    /// Setup: add states with <see cref="AddState(IState)"/>, connect them with <see cref="AddTransition"/>,
    /// <see cref="AddAutoTransition"/> and <see cref="AddMasterTransition"/>, optionally call <see cref="SetEndState"/>,
    /// then call <see cref="StartStateMachine"/>. Call <see cref="Tick"/> every frame from the owning MonoBehaviour
    /// and drive the machine with <see cref="ExecuteEvent"/>.
    /// </para>
    /// <para>
    /// Events run to completion: an event executed from inside a state callback (Enter, Tick, Exit) or a
    /// <see cref="StateChanged"/> listener is queued and handled once the current transition has finished,
    /// so a state is never exited before its Enter has returned.
    /// </para>
    /// <para>
    /// After a state is entered, follow-up work happens in this order: events queued during the transition,
    /// then <see cref="OnStateSettled"/> (which replays deferred events), then the state's auto transition.
    /// </para>
    /// Not thread-safe: use it from the main thread only.
    /// </summary>
    /// <typeparam name="TEvent">Event key type. Enums give compile-time safety; strings work too.</typeparam>
    public class StateMachine<TEvent>
    {
        #region Variables
        private readonly Dictionary<IState, StateNode> nodes = new(IdentityComparer<IState>.Instance);
        private readonly List<StateNode> nodesInOrder = new();
        private readonly IEqualityComparer<TEvent> eventComparer;
        private readonly Dictionary<TEvent, StateNode> masterTransitions;
        private readonly Queue<QueuedEvent> pendingEvents = new();
        private readonly List<QueuedEvent> deferredEvents = new();

        private StateNode currentNode;
        private StateNode endNode;
        private bool isProcessing;
        private bool settlePending;
        private bool autoTransitionPending;
        private Command pendingCommand;
        private StateNode commandTarget;
        private object commandData;
        #endregion


        #region Properties
        /// <summary>
        /// Name used in log messages.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Logs every transition and every ignored or deferred event.
        /// Misconfiguration warnings are always logged, whatever this is set to.
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// When false (the default), an event the current state has no transition for is ignored.
        /// When true, it is kept and replayed after a later state change, as soon as the current state can handle it.
        /// Deferred events are cleared by master transitions, <see cref="RestartStateMachine"/> and <see cref="StopStateMachine"/>.
        /// </summary>
        public bool DeferUnhandledEvents { get; set; }

        /// <summary>
        /// Maximum number of processing steps in one call. Stops auto transitions or events that loop forever.
        /// </summary>
        public int MaxStepsPerCall { get; set; } = 1000;

        /// <summary>
        /// The active state, or null when the machine isn't running.
        /// </summary>
        public IState CurrentState => currentNode?.State;

        /// <summary>
        /// Name of the active state, or null when the machine isn't running.
        /// </summary>
        public string CurrentStateName => currentNode?.Name;

        /// <summary>
        /// The state set with <see cref="SetEndState"/>, or null.
        /// </summary>
        public IState EndState => endNode?.State;

        /// <summary>
        /// True between <see cref="StartStateMachine"/> and <see cref="StopStateMachine"/>.
        /// </summary>
        public bool IsRunning => currentNode != null;

        /// <summary>
        /// True while the machine is in its end state. Events are ignored until it is restarted.
        /// </summary>
        public bool IsFinished => currentNode != null && currentNode == endNode;

        /// <summary>
        /// Number of events waiting to be replayed (see <see cref="DeferUnhandledEvents"/>).
        /// </summary>
        public int DeferredEventCount => deferredEvents.Count;

        /// <summary>
        /// True while the machine is handling an event or running a state callback.
        /// Events executed now are queued until the current step has finished.
        /// </summary>
        protected bool IsProcessing => isProcessing;

        /// <summary>
        /// Number of events queued for the current processing run.
        /// </summary>
        protected int PendingEventCount => pendingEvents.Count;
        #endregion


        #region Events
        /// <summary>
        /// Raised after a state has been entered, with (previous state, new state).
        /// The previous state is null when the machine starts; the new state is null when it stops.
        /// </summary>
        public event Action<IState, IState> StateChanged;
        #endregion


        #region Constructors
        public StateMachine() : this(null) { }

        /// <param name="name">Name used in log messages. Defaults to the class name.</param>
        /// <param name="eventComparer">Optional comparer for event keys, e.g. StringComparer.OrdinalIgnoreCase.</param>
        public StateMachine(string name, IEqualityComparer<TEvent> eventComparer = null)
        {
            Type type = GetType();
            Name = !string.IsNullOrEmpty(name) ? name
                : type.IsGenericType ? $"{type.Name.Split('`')[0]}<{typeof(TEvent).Name}>"
                : type.Name;

            this.eventComparer = eventComparer ?? EqualityComparer<TEvent>.Default;
            masterTransitions = new Dictionary<TEvent, StateNode>(this.eventComparer);
        }
        #endregion


        #region Setup Methods
        /// <summary>
        /// Adds a state named after its type. The first state added is the default start state.
        /// </summary>
        public void AddState(IState state)
        {
            AddState(null, state);
        }

        /// <summary>
        /// Adds a state. The first state added is the default start state.
        /// </summary>
        /// <param name="stateName">Name used in log messages. Doesn't have to be unique.</param>
        public void AddState(string stateName, IState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (nodes.ContainsKey(state))
            {
                LogWarning($"{GetStateName(state)} was already added; the second AddState was ignored.");
                return;
            }

            if (state is State<TEvent> { Machine: { } owner } && owner != this)
                throw new InvalidOperationException($"State '{state}' already belongs to state machine '{owner.Name}'. Create a separate state instance for each machine.");

            var node = new StateNode(state, string.IsNullOrEmpty(stateName) ? state.GetType().Name : stateName, eventComparer);
            nodes.Add(state, node);
            nodesInOrder.Add(node);

            if (state is State<TEvent> ownedState)
                ownedState.Attach(this, node.Name);
        }

        /// <summary>
        /// Once the machine enters this state, it ignores all events until it is restarted.
        /// Pass null to clear it.
        /// </summary>
        public void SetEndState(IState state)
        {
            endNode = state == null ? null : GetNode(state, nameof(state));
        }

        /// <summary>
        /// Executing <paramref name="eventKey"/> while in <paramref name="fromState"/> switches to <paramref name="toState"/>.
        /// </summary>
        public void AddTransition(IState fromState, IState toState, TEvent eventKey)
        {
            StateNode from = GetNode(fromState, nameof(fromState));
            StateNode to = GetNode(toState, nameof(toState));
            ThrowIfNull(eventKey);

            if (from.Transitions.TryGetValue(eventKey, out StateNode existing))
            {
                LogWarning($"{from.Name} already has a transition for '{eventKey}' (to {existing.Name}); the transition to {to.Name} was ignored.");
                return;
            }

            from.Transitions.Add(eventKey, to);
        }

        /// <summary>
        /// Switches from <paramref name="fromState"/> to <paramref name="toState"/> right after
        /// <paramref name="fromState"/> has been entered, unless another event moves the machine first.
        /// </summary>
        public void AddAutoTransition(IState fromState, IState toState)
        {
            StateNode from = GetNode(fromState, nameof(fromState));
            StateNode to = GetNode(toState, nameof(toState));

            if (from.AutoTransition != null)
            {
                LogWarning($"{from.Name} already has an auto transition (to {from.AutoTransition.Name}); the auto transition to {to.Name} was ignored.");
                return;
            }

            from.AutoTransition = to;
        }

        /// <summary>
        /// Executing <paramref name="eventKey"/> switches to <paramref name="toState"/> from any state
        /// (except the end state). Takes priority over the current state's own transitions and clears deferred events.
        /// </summary>
        public void AddMasterTransition(IState toState, TEvent eventKey)
        {
            StateNode to = GetNode(toState, nameof(toState));
            ThrowIfNull(eventKey);

            if (masterTransitions.TryGetValue(eventKey, out StateNode existing))
            {
                LogWarning($"There is already a master transition for '{eventKey}' (to {existing.Name}); the one to {to.Name} was ignored.");
                return;
            }

            masterTransitions.Add(eventKey, to);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Enters <paramref name="startState"/>, or the first added state when it is null.
        /// </summary>
        /// <param name="data">Passed to the start state's Enter.</param>
        public void StartStateMachine(IState startState = null, object data = null)
        {
            if (IsRunning)
            {
                LogWarning($"StartStateMachine was called while already running (in {currentNode.Name}); ignored. Use RestartStateMachine to start over.");
                return;
            }

            StateNode start = ResolveStartNode(startState);
            if (start == null)
                return;

            if (isProcessing)
            {
                // Called from a callback while the machine is stopping: start once the current step is done.
                ScheduleCommand(Command.Restart, start, data);
                return;
            }

            if (DebugMode)
                LogDebug($"Starting in {start.Name}");

            Run(start, data);
        }

        /// <summary>
        /// Exits the current state, clears queued and deferred events, then enters <paramref name="startState"/>
        /// (or the first added state). Safe to call from inside a state callback: the restart happens once
        /// that callback has returned.
        /// </summary>
        public void RestartStateMachine(IState startState = null, object data = null)
        {
            if (!IsRunning && !isProcessing)
            {
                StartStateMachine(startState, data);
                return;
            }

            StateNode start = ResolveStartNode(startState);
            if (start == null)
                return;

            ScheduleCommand(Command.Restart, start, data);
            if (!isProcessing)
                Run(null, null);
        }

        /// <summary>
        /// Exits the current state and stops the machine. Call it when the owner is destroyed so the
        /// current state's Exit runs. Safe to call from inside a state callback.
        /// </summary>
        public void StopStateMachine()
        {
            if (!IsRunning)
                return;

            ScheduleCommand(Command.Stop, null, null);
            if (!isProcessing)
                Run(null, null);
        }

        /// <summary>
        /// Handles an event: takes a master transition or the current state's transition for it. An event
        /// with no transition is ignored, or deferred when <see cref="DeferUnhandledEvents"/> is on.
        /// Executed from inside a state callback, the event is queued until that callback has returned.
        /// </summary>
        /// <param name="data">Passed to the next state's Enter.</param>
        public void ExecuteEvent(TEvent eventKey, object data = null)
        {
            ThrowIfNull(eventKey);

            if (!IsRunning && !isProcessing)
            {
                LogWarning($"'{eventKey}' was executed before StartStateMachine; ignored.");
                return;
            }

            pendingEvents.Enqueue(new QueuedEvent(eventKey, data));
            if (!isProcessing)
                Run(null, null);
        }

        /// <summary>
        /// Calls the current state's Tick. Call this every frame from the owning MonoBehaviour's Update.
        /// </summary>
        public void Tick()
        {
            if (currentNode == null || isProcessing)
                return;

            isProcessing = true;
            try
            {
                currentNode.State.Tick();
            }
            catch
            {
                DiscardQueuedWork();
                throw;
            }
            finally
            {
                isProcessing = false;
            }

            if (pendingEvents.Count > 0 || pendingCommand != Command.None || settlePending)
                Run(null, null);
        }

        /// <summary>
        /// True if executing <paramref name="eventKey"/> now would cause a transition.
        /// </summary>
        public bool CanHandle(TEvent eventKey)
        {
            return currentNode != null && !IsFinished && eventKey != null &&
                   (masterTransitions.ContainsKey(eventKey) || currentNode.Transitions.ContainsKey(eventKey));
        }

        /// <summary>
        /// The name a state was added with, or null if it isn't part of this machine.
        /// </summary>
        public string GetStateName(IState state)
        {
            return state != null && nodes.TryGetValue(state, out StateNode node) ? node.Name : null;
        }

        /// <summary>
        /// Drops every deferred event.
        /// </summary>
        public virtual void ClearDeferredEvents()
        {
            if (DebugMode && deferredEvents.Count > 0)
                LogDebug($"Cleared {deferredEvents.Count} deferred event(s)");

            deferredEvents.Clear();
        }
        #endregion


        #region Virtual Methods
        /// <summary>
        /// Called when the current state has no transition for an event. By default the event is ignored,
        /// or deferred when <see cref="DeferUnhandledEvents"/> is on.
        /// </summary>
        protected virtual void OnUnhandledEvent(TEvent eventKey, object data)
        {
            if (DeferUnhandledEvents)
            {
                deferredEvents.Add(new QueuedEvent(eventKey, data));
                if (DebugMode)
                    LogDebug($"'{eventKey}' deferred: {currentNode.Name} has no transition for it");
            }
            else if (DebugMode)
            {
                LogDebug($"'{eventKey}' ignored: {currentNode.Name} has no transition for it");
            }
        }

        /// <summary>
        /// Called once after each state change, when the events queued during that transition have been handled
        /// and before the state's auto transition. By default it replays the oldest deferred event the new state
        /// can handle. Call <see cref="ExecuteEvent"/> here to queue an event for the settled state.
        /// </summary>
        protected virtual void OnStateSettled()
        {
            for (int i = 0; i < deferredEvents.Count; i++)
            {
                QueuedEvent deferred = deferredEvents[i];
                if (!CanHandle(deferred.Event))
                    continue;

                deferredEvents.RemoveAt(i);
                if (DebugMode)
                    LogDebug($"Replaying deferred '{deferred.Event}'");

                pendingEvents.Enqueue(deferred);
                return;
            }
        }

        /// <summary>
        /// Called during a restart, after the old state's Exit and before the start state's Enter.
        /// </summary>
        protected virtual void OnRestarting() { }

        /// <summary>
        /// Makes the machine call <see cref="OnStateSettled"/> again once the current processing step has finished,
        /// e.g. after a subclass buffered an event while <see cref="IsProcessing"/> was true.
        /// </summary>
        protected void ScheduleSettle()
        {
            settlePending = true;
        }
        #endregion


        #region Logging
        /// <summary>
        /// Logs a debug message with the machine's name. Check <see cref="DebugMode"/> before calling,
        /// so the message isn't built when logging is off.
        /// </summary>
        protected void LogDebug(string message)
        {
            Debug.Log($"[StateMachine:{Name}] {message} (frame {Time.frameCount})");
        }

        /// <summary>
        /// Logs a warning with the machine's name. Always shown.
        /// </summary>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[StateMachine:{Name}] {message}");
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Processes queued work until the machine is idle: commands, queued events, the settle hook and
        /// auto transitions. Every state callback runs inside this loop, which is what makes events
        /// executed from those callbacks run to completion.
        /// </summary>
        private void Run(StateNode startNode, object startData)
        {
            isProcessing = true;
            try
            {
                if (startNode != null)
                    EnterState(startNode, startData, null);

                for (int step = 1; ; step++)
                {
                    if (step > MaxStepsPerCall)
                    {
                        Debug.LogError($"[StateMachine:{Name}] Stopped after {MaxStepsPerCall} steps in one call. Check for auto transitions that loop, or states that keep executing events. Queued events were dropped.");
                        DiscardQueuedWork();
                        break;
                    }

                    if (pendingCommand != Command.None)
                    {
                        RunCommand();
                    }
                    else if (currentNode == null)
                    {
                        pendingEvents.Clear();
                        break;
                    }
                    else if (pendingEvents.Count > 0)
                    {
                        QueuedEvent queued = pendingEvents.Dequeue();
                        Process(queued.Event, queued.Data);
                    }
                    else if (settlePending)
                    {
                        settlePending = false;
                        OnStateSettled();
                    }
                    else if (autoTransitionPending)
                    {
                        autoTransitionPending = false;
                        StateNode target = currentNode.AutoTransition;
                        if (target != null && !IsFinished)
                        {
                            if (DebugMode)
                                LogDebug($"Auto transition: {currentNode.Name} -> {target.Name}");

                            TransitionTo(target, null);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                DiscardQueuedWork();
                throw;
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void Process(TEvent eventKey, object data)
        {
            if (IsFinished)
            {
                if (DebugMode)
                    LogDebug($"'{eventKey}' ignored: the machine is in its end state ({currentNode.Name})");
                return;
            }

            if (masterTransitions.TryGetValue(eventKey, out StateNode masterTarget))
            {
                if (DebugMode)
                    LogDebug($"Master event '{eventKey}': {currentNode.Name} -> {masterTarget.Name}");

                ClearDeferredEvents();
                TransitionTo(masterTarget, data);
                return;
            }

            if (currentNode.Transitions.TryGetValue(eventKey, out StateNode target))
            {
                if (DebugMode)
                    LogDebug($"Event '{eventKey}': {currentNode.Name} -> {target.Name}");

                TransitionTo(target, data);
                return;
            }

            OnUnhandledEvent(eventKey, data);
        }

        private void TransitionTo(StateNode next, object data)
        {
            StateNode previous = currentNode;
            previous.State.Exit();
            EnterState(next, data, previous);
        }

        private void EnterState(StateNode next, object data, StateNode previous)
        {
            currentNode = next;
            settlePending = true;
            autoTransitionPending = true;
            next.State.Enter(data);
            StateChanged?.Invoke(previous?.State, next.State);
        }

        private void ScheduleCommand(Command command, StateNode target, object data)
        {
            pendingCommand = command;
            commandTarget = target;
            commandData = data;
        }

        private void RunCommand()
        {
            Command command = pendingCommand;
            StateNode target = commandTarget;
            object data = commandData;
            ScheduleCommand(Command.None, null, null);

            StateNode previous = currentNode;
            if (previous != null)
            {
                if (DebugMode)
                    LogDebug($"{(command == Command.Restart ? "Restarting" : "Stopping")}: exiting {previous.Name}");

                previous.State.Exit();
            }

            // Anything queued before or during Exit belongs to the old run.
            pendingEvents.Clear();
            ClearDeferredEvents();

            if (command == Command.Restart)
            {
                OnRestarting();
                EnterState(target, data, previous);
            }
            else
            {
                currentNode = null;
                settlePending = false;
                autoTransitionPending = false;
                StateChanged?.Invoke(previous?.State, null);
            }
        }

        private void DiscardQueuedWork()
        {
            pendingEvents.Clear();
            ScheduleCommand(Command.None, null, null);
            settlePending = false;
            autoTransitionPending = false;
        }

        private StateNode ResolveStartNode(IState startState)
        {
            if (startState != null)
                return GetNode(startState, nameof(startState));

            if (nodesInOrder.Count == 0)
            {
                LogWarning("has no states. Add at least one state before starting it.");
                return null;
            }

            return nodesInOrder[0];
        }

        private StateNode GetNode(IState state, string paramName)
        {
            if (state == null)
                throw new ArgumentNullException(paramName);

            if (nodes.TryGetValue(state, out StateNode node))
                return node;

            throw new ArgumentException($"{state} has not been added to state machine '{Name}'. Call AddState first.", paramName);
        }

        private static void ThrowIfNull(TEvent eventKey)
        {
            if (eventKey == null)
                throw new ArgumentNullException(nameof(eventKey));
        }
        #endregion


        #region Nested Types
        private enum Command { None, Restart, Stop }

        private readonly struct QueuedEvent
        {
            public readonly TEvent Event;
            public readonly object Data;

            public QueuedEvent(TEvent eventKey, object data)
            {
                Event = eventKey;
                Data = data;
            }
        }

        /// <summary>
        /// A state plus its outgoing transitions. Transitions live in the machine, not in the state,
        /// so any <see cref="IState"/> (including a MonoBehaviour) can be used.
        /// </summary>
        private sealed class StateNode
        {
            public readonly IState State;
            public readonly string Name;
            public readonly Dictionary<TEvent, StateNode> Transitions;
            public StateNode AutoTransition;

            public StateNode(IState state, string name, IEqualityComparer<TEvent> eventComparer)
            {
                State = state;
                Name = name;
                Transitions = new Dictionary<TEvent, StateNode>(eventComparer);
            }
        }
        #endregion
    }
}
