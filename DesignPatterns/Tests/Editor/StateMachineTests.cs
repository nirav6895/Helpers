#region Using
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class StateMachineTests
    {
        #region Test Types
        private enum Trigger { Go, Click, Reset, Unknown }

        private sealed class TraceState : State<Trigger>
        {
            private readonly List<string> trace;
            public Action OnEnterHook;
            public Action OnTickHook;
            public object LastData;
            public int Ticks;

            public TraceState(List<string> trace)
            {
                this.trace = trace;
            }

            public override void Enter(object data)
            {
                LastData = data;
                trace.Add("Enter " + Name);
                OnEnterHook?.Invoke();
                trace.Add("Entered " + Name);
            }

            public override void Tick()
            {
                Ticks++;
                trace.Add("Tick " + Name);
                OnTickHook?.Invoke();
                trace.Add("Ticked " + Name);
            }

            public override void Exit()
            {
                trace.Add("Exit " + Name);
            }
        }

        /// <summary>
        /// A state that implements IState directly instead of deriving from State.
        /// </summary>
        private sealed class PlainState : IState
        {
            public int Entered;
            public void Enter(object data) => Entered++;
            public void Tick() { }
            public void Exit() { }
        }
        #endregion


        #region Setup
        private List<string> trace;
        private StateMachine<Trigger> machine;
        private TraceState a, b, c, reset, end;

        // A -Go-> B -Go-> C -Click-> A, C -Go-> End (end state), Reset -auto-> A, master Reset -> Reset.
        [SetUp]
        public void SetUp()
        {
            trace = new List<string>();
            machine = new StateMachine<Trigger>("Test");
            a = new TraceState(trace);
            b = new TraceState(trace);
            c = new TraceState(trace);
            reset = new TraceState(trace);
            end = new TraceState(trace);

            machine.AddState("A", a);
            machine.AddState("B", b);
            machine.AddState("C", c);
            machine.AddState("Reset", reset);
            machine.AddState("End", end);
            machine.AddTransition(a, b, Trigger.Go);
            machine.AddTransition(b, c, Trigger.Go);
            machine.AddTransition(c, a, Trigger.Click);
            machine.AddTransition(c, end, Trigger.Go);
            machine.AddAutoTransition(reset, a);
            machine.AddMasterTransition(reset, Trigger.Reset);
            machine.SetEndState(end);
        }
        #endregion


        #region Basics
        [Test]
        public void Start_EntersFirstState_WithData()
        {
            IState from = b, to = null;
            machine.StateChanged += (previous, next) => { from = previous; to = next; };

            machine.StartStateMachine(null, "payload");

            Assert.AreSame(a, machine.CurrentState);
            Assert.AreEqual("A", machine.CurrentStateName);
            Assert.AreEqual("payload", a.LastData);
            Assert.IsNull(from);
            Assert.AreSame(a, to);
        }

        [Test]
        public void ExecuteEvent_FollowsTransition_AndPassesData()
        {
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go, 42);

            Assert.AreSame(b, machine.CurrentState);
            Assert.AreEqual(42, b.LastData);
            CollectionAssert.AreEqual(new[] { "Enter A", "Entered A", "Exit A", "Enter B", "Entered B" }, trace);
        }

        [Test]
        public void Tick_CallsCurrentStateTick()
        {
            machine.StartStateMachine();
            machine.Tick();
            machine.Tick();

            Assert.AreEqual(2, a.Ticks);
        }

        [Test]
        public void PlainIStateImplementation_Works()
        {
            var plainMachine = new StateMachine<string>();
            var first = new PlainState();
            var second = new PlainState();
            plainMachine.AddState(first);
            plainMachine.AddState(second);
            plainMachine.AddTransition(first, second, "next");

            plainMachine.StartStateMachine();
            plainMachine.ExecuteEvent("next");

            Assert.AreSame(second, plainMachine.CurrentState);
            Assert.AreEqual("PlainState", plainMachine.GetStateName(second));
        }

        [Test]
        public void StringEvents_UseTheGivenComparer()
        {
            var stringMachine = new StateMachine<string>("Strings", StringComparer.OrdinalIgnoreCase);
            var idle = new State<string>();
            var running = new State<string>();
            stringMachine.AddState("Idle", idle);
            stringMachine.AddState("Running", running);
            stringMachine.AddTransition(idle, running, "Run");

            stringMachine.StartStateMachine();
            stringMachine.ExecuteEvent("RUN");

            Assert.AreSame(running, stringMachine.CurrentState);
            Assert.AreSame(stringMachine, running.Machine);
            Assert.AreEqual("Running", running.Name);
        }
        #endregion


        #region Unhandled And Deferred Events
        [Test]
        public void UnhandledEvent_IsIgnoredByDefault()
        {
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Click);   // A can't handle it
            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Go);

            Assert.AreSame(c, machine.CurrentState, "an old click must not fire later");
            Assert.AreEqual(0, machine.DeferredEventCount);
        }

        [Test]
        public void UnhandledEvent_IsReplayed_WhenDeferralIsOn()
        {
            machine.DeferUnhandledEvents = true;
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Click);
            Assert.AreEqual(1, machine.DeferredEventCount);

            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Go);       // enters C, which handles the deferred click

            Assert.AreSame(a, machine.CurrentState);
            Assert.AreEqual(0, machine.DeferredEventCount);
        }

        [Test]
        public void DeferredEvent_ThatNoStateHandles_DoesNotBlockLaterOnes()
        {
            machine.DeferUnhandledEvents = true;
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Unknown);
            machine.ExecuteEvent(Trigger.Click);
            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Go);

            Assert.AreSame(a, machine.CurrentState, "the click should replay even though Unknown is ahead of it");
            Assert.AreEqual(1, machine.DeferredEventCount);
        }

        [Test]
        public void MasterTransition_ClearsDeferredEvents()
        {
            machine.DeferUnhandledEvents = true;
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Click);
            machine.ExecuteEvent(Trigger.Reset);

            Assert.AreEqual(0, machine.DeferredEventCount);
        }
        #endregion


        #region Run To Completion
        [Test]
        public void EventExecutedInsideEnter_RunsAfterEnterReturns()
        {
            b.OnEnterHook = () => machine.ExecuteEvent(Trigger.Go);
            machine.StartStateMachine();
            trace.Clear();

            machine.ExecuteEvent(Trigger.Go);

            Assert.AreSame(c, machine.CurrentState);
            CollectionAssert.AreEqual(new[] { "Exit A", "Enter B", "Entered B", "Exit B", "Enter C", "Entered C" }, trace);
        }

        [Test]
        public void EventExecutedInsideTick_RunsAfterTickReturns()
        {
            a.OnTickHook = () => machine.ExecuteEvent(Trigger.Go);
            machine.StartStateMachine();
            trace.Clear();

            machine.Tick();

            Assert.AreSame(b, machine.CurrentState);
            CollectionAssert.AreEqual(new[] { "Tick A", "Ticked A", "Exit A", "Enter B", "Entered B" }, trace);
        }

        [Test]
        public void AutoTransitionLoop_IsStopped_WithAnError()
        {
            var loopMachine = new StateMachine<Trigger>("Loop") { MaxStepsPerCall = 50 };
            var x = new State<Trigger>();
            var y = new State<Trigger>();
            loopMachine.AddState("X", x);
            loopMachine.AddState("Y", y);
            loopMachine.AddAutoTransition(x, y);
            loopMachine.AddAutoTransition(y, x);

            LogAssert.Expect(LogType.Error, new Regex("Stopped after 50 steps"));
            loopMachine.StartStateMachine();
        }
        #endregion


        #region Master Transitions, Restart, Stop, End State
        [Test]
        public void MasterTransition_FollowsTheTargetsAutoTransition()
        {
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go);
            trace.Clear();

            machine.ExecuteEvent(Trigger.Reset);

            Assert.AreSame(a, machine.CurrentState);
            CollectionAssert.AreEqual(new[] { "Exit B", "Enter Reset", "Entered Reset", "Exit Reset", "Enter A", "Entered A" }, trace);
        }

        [Test]
        public void Restart_ExitsCurrentState_AndClearsDeferredEvents()
        {
            machine.DeferUnhandledEvents = true;
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Click);   // deferred in B
            trace.Clear();

            machine.RestartStateMachine();

            Assert.AreSame(a, machine.CurrentState);
            Assert.AreEqual(0, machine.DeferredEventCount);
            CollectionAssert.AreEqual(new[] { "Exit B", "Enter A", "Entered A" }, trace);
        }

        [Test]
        public void Restart_CalledInsideEnter_HappensAfterEnterReturns()
        {
            c.OnEnterHook = () => machine.RestartStateMachine();
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go);
            trace.Clear();

            machine.ExecuteEvent(Trigger.Go);

            Assert.AreSame(a, machine.CurrentState);
            CollectionAssert.AreEqual(new[] { "Exit B", "Enter C", "Entered C", "Exit C", "Enter A", "Entered A" }, trace);
        }

        [Test]
        public void EndState_IgnoresEvents_UntilRestarted()
        {
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Go);
            machine.ExecuteEvent(Trigger.Go);
            Assert.IsTrue(machine.IsFinished);

            machine.ExecuteEvent(Trigger.Reset);
            Assert.AreSame(end, machine.CurrentState);

            machine.RestartStateMachine();
            Assert.AreSame(a, machine.CurrentState);
            Assert.IsFalse(machine.IsFinished);
        }

        [Test]
        public void Stop_ExitsCurrentState()
        {
            IState from = null, to = a;
            machine.StartStateMachine();
            machine.ExecuteEvent(Trigger.Go);
            machine.StateChanged += (previous, next) => { from = previous; to = next; };

            machine.StopStateMachine();

            Assert.IsFalse(machine.IsRunning);
            Assert.IsNull(machine.CurrentState);
            Assert.AreEqual("Exit B", trace[trace.Count - 1]);
            Assert.AreSame(b, from);
            Assert.IsNull(to);
        }
        #endregion


        #region Misconfiguration
        [Test]
        public void Misconfiguration_IsReported_WithoutDebugMode()
        {
            LogAssert.Expect(LogType.Warning, new Regex("already added"));
            machine.AddState("A again", a);

            LogAssert.Expect(LogType.Warning, new Regex("already has a transition for 'Go'"));
            machine.AddTransition(a, c, Trigger.Go);

            LogAssert.Expect(LogType.Warning, new Regex("already has an auto transition"));
            machine.AddAutoTransition(reset, b);

            LogAssert.Expect(LogType.Warning, new Regex("already a master transition for 'Reset'"));
            machine.AddMasterTransition(a, Trigger.Reset);

            LogAssert.Expect(LogType.Warning, new Regex("before StartStateMachine"));
            machine.ExecuteEvent(Trigger.Go);

            LogAssert.Expect(LogType.Warning, new Regex("has no states"));
            new StateMachine<Trigger>().StartStateMachine();

            machine.StartStateMachine();
            LogAssert.Expect(LogType.Warning, new Regex("already running"));
            machine.StartStateMachine();
        }

        [Test]
        public void AddTransition_WithAStateThatWasNotAdded_Throws()
        {
            var stranger = new TraceState(trace);
            Assert.Throws<ArgumentException>(() => machine.AddTransition(a, stranger, Trigger.Click));
            Assert.Throws<ArgumentException>(() => machine.StartStateMachine(stranger));
        }

        [Test]
        public void State_CannotBelongToTwoMachines()
        {
            var other = new StateMachine<Trigger>();
            Assert.Throws<InvalidOperationException>(() => other.AddState(a));
        }
        #endregion
    }
}
