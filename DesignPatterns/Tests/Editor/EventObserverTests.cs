#region Using
using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class EventObserverTests
    {
        #region Test Types
        private struct Ping : IEvent
        {
            public int Value;
        }

        private class DamageEvent : IEvent
        {
            public int Amount;
        }

        private class CriticalDamageEvent : DamageEvent { }

        private abstract class AbstractEvent : IEvent { }

        private enum GameEvent { ScoreChanged, Toast, Paused }
        #endregion


        #region Setup
        [SetUp]
        public void SetUp()
        {
            ClearObservers();
        }

        [TearDown]
        public void TearDown()
        {
            ClearObservers();
        }

        private static void ClearObservers()
        {
            EventObserver.Clear();
            EventObserver<GameEvent>.Clear();
        }
        #endregion


        #region EventObserver
        [Test]
        public void Execute_CallsListener_WithPayload()
        {
            int received = 0;
            EventObserver.Register<Ping>(ping => received = ping.Value);

            EventObserver.Execute(new Ping { Value = 7 });

            Assert.AreEqual(7, received);
        }

        [Test]
        public void Unregister_StopsDelivery()
        {
            int calls = 0;
            Action<Ping> listener = _ => calls++;
            EventObserver.Register(listener);
            EventObserver.Unregister(listener);

            EventObserver.Execute(new Ping());

            Assert.AreEqual(0, calls);
            Assert.IsFalse(EventObserver.HasListeners<Ping>());
        }

        [Test]
        public void DuplicateRegister_IsIgnored_WithAWarning()
        {
            int calls = 0;
            Action<Ping> listener = _ => calls++;
            EventObserver.Register(listener);
            LogAssert.Expect(LogType.Warning, new Regex("already registered for Ping"));
            EventObserver.Register(listener);

            EventObserver.Execute(new Ping());
            Assert.AreEqual(1, calls);

            EventObserver.Unregister(listener);
            EventObserver.Execute(new Ping());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void ThrowingListener_IsLogged_WithTheRealException_AndOthersStillRun()
        {
            int calls = 0;
            EventObserver.Register<DamageEvent>(_ => throw new InvalidOperationException("boom"));
            EventObserver.Register<DamageEvent>(_ => calls++);

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
            EventObserver.Execute(new DamageEvent());

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
            IEvent untyped = new DamageEvent();
            EventObserver.Execute(untyped);

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Dispatch_UsesTheEventsRuntimeType()
        {
            int critical = 0, plain = 0;
            EventObserver.Register<CriticalDamageEvent>(_ => critical++);
            EventObserver.Register<DamageEvent>(_ => plain++);

            DamageEvent typedAsBase = new CriticalDamageEvent();
            EventObserver.Execute(typedAsBase);
            EventObserver.Execute((IEvent)typedAsBase);

            Assert.AreEqual(2, critical);
            Assert.AreEqual(0, plain, "dispatch is by exact runtime type");
        }

        [Test]
        public void UnregisteringDuringDispatch_IsSafe()
        {
            int secondCalls = 0;
            Action<Ping> second = _ => secondCalls++;
            EventObserver.Register<Ping>(_ => EventObserver.Unregister(second));
            EventObserver.Register(second);

            Assert.DoesNotThrow(() => EventObserver.Execute(new Ping()));
            EventObserver.Execute(new Ping());

            Assert.AreEqual(1, secondCalls, "called from the snapshot of the first dispatch only");
        }

        [Test]
        public void RegisteringForAnAbstractType_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("abstract or an interface"));
            EventObserver.Register<AbstractEvent>(_ => { });
        }
        #endregion


        #region EventObserver<TEnum>
        [Test]
        public void Enum_DeliversWithAndWithoutPayload()
        {
            int pauses = 0, score = 0;
            EventObserver<GameEvent>.Register(GameEvent.Paused, () => pauses++);
            EventObserver<GameEvent>.Register<int>(GameEvent.ScoreChanged, value => score = value);

            EventObserver<GameEvent>.Execute(GameEvent.Paused);
            EventObserver<GameEvent>.Execute(GameEvent.ScoreChanged, 50);

            Assert.AreEqual(1, pauses);
            Assert.AreEqual(50, score);
        }

        [Test]
        public void Enum_MixedSignaturesOnOneKey_AreAllowed()
        {
            int plain = 0, withPayload = 0;
            EventObserver<GameEvent>.Register(GameEvent.Toast, () => plain++);
            EventObserver<GameEvent>.Register<int>(GameEvent.Toast, _ => withPayload++);

            EventObserver<GameEvent>.Execute(GameEvent.Toast);
            EventObserver<GameEvent>.Execute(GameEvent.Toast, 1);

            Assert.AreEqual(1, plain);
            Assert.AreEqual(1, withPayload);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Enum_PayloadTypeMismatch_Warns()
        {
            bool called = false;
            EventObserver<GameEvent>.Register<float>(GameEvent.ScoreChanged, _ => called = true);

            LogAssert.Expect(LogType.Warning, new Regex("ScoreChanged was executed with a Int32 payload.*expect a Single payload"));
            EventObserver<GameEvent>.Execute(GameEvent.ScoreChanged, 10);

            Assert.IsFalse(called);
        }

        [Test]
        public void Enum_AcceptsReferenceTypePayloads()
        {
            string message = null;
            EventObserver<GameEvent>.Register<string>(GameEvent.Toast, text => message = text);

            EventObserver<GameEvent>.Execute(GameEvent.Toast, "Saved");

            Assert.AreEqual("Saved", message);
        }

        [Test]
        public void Enum_DuplicateRegister_IsIgnored()
        {
            int calls = 0;
            Action listener = () => calls++;
            EventObserver<GameEvent>.Register(GameEvent.Paused, listener);
            LogAssert.Expect(LogType.Warning, new Regex("already registered for Paused"));
            EventObserver<GameEvent>.Register(GameEvent.Paused, listener);

            EventObserver<GameEvent>.Execute(GameEvent.Paused);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Enum_ThrowingListener_IsLogged_AndOthersStillRun()
        {
            int calls = 0;
            EventObserver<GameEvent>.Register(GameEvent.Paused, () => throw new InvalidOperationException("enum boom"));
            EventObserver<GameEvent>.Register(GameEvent.Paused, () => calls++);

            LogAssert.Expect(LogType.Exception, new Regex("enum boom"));
            EventObserver<GameEvent>.Execute(GameEvent.Paused);

            Assert.AreEqual(1, calls);
        }
        #endregion
    }
}
