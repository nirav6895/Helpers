#region Using
using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class EventBusTests
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
            ClearEventBuses();
        }

        [TearDown]
        public void TearDown()
        {
            ClearEventBuses();
        }

        private static void ClearEventBuses()
        {
            EventBus.Clear();
            EventBus<GameEvent>.Clear();
        }
        #endregion


        #region EventBus
        [Test]
        public void Execute_CallsListener_WithPayload()
        {
            int received = 0;
            EventBus.Register<Ping>(ping => received = ping.Value);

            EventBus.Execute(new Ping { Value = 7 });

            Assert.AreEqual(7, received);
        }

        [Test]
        public void Unregister_StopsDelivery()
        {
            int calls = 0;
            void listener(Ping _) => calls++;
            EventBus.Register((Action<Ping>)listener);
            EventBus.Unregister((Action<Ping>)listener);

            EventBus.Execute(new Ping());

            Assert.AreEqual(0, calls);
            Assert.IsFalse(EventBus.HasListeners<Ping>());
        }

        [Test]
        public void DuplicateRegister_IsIgnored_WithAWarning()
        {
            int calls = 0;
            void listener(Ping _) => calls++;
            EventBus.Register((Action<Ping>)listener);
            LogAssert.Expect(LogType.Warning, new Regex("already registered for Ping"));
            EventBus.Register((Action<Ping>)listener);

            EventBus.Execute(new Ping());
            Assert.AreEqual(1, calls);

            EventBus.Unregister((Action<Ping>)listener);
            EventBus.Execute(new Ping());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void ThrowingListener_IsLogged_WithTheRealException_AndOthersStillRun()
        {
            int calls = 0;
            EventBus.Register<DamageEvent>(_ => throw new InvalidOperationException("boom"));
            EventBus.Register<DamageEvent>(_ => calls++);

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
            EventBus.Execute(new DamageEvent());

            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: boom"));
            IEvent untyped = new DamageEvent();
            EventBus.Execute(untyped);

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Dispatch_UsesTheEventsRuntimeType()
        {
            int critical = 0, plain = 0;
            EventBus.Register<CriticalDamageEvent>(_ => critical++);
            EventBus.Register<DamageEvent>(_ => plain++);

            DamageEvent typedAsBase = new CriticalDamageEvent();
            EventBus.Execute(typedAsBase);
            EventBus.Execute((IEvent)typedAsBase);

            Assert.AreEqual(2, critical);
            Assert.AreEqual(0, plain, "dispatch is by exact runtime type");
        }

        [Test]
        public void UnregisteringDuringDispatch_IsSafe()
        {
            int secondCalls = 0;
            void second(Ping _) => secondCalls++;
            EventBus.Register<Ping>(_ => EventBus.Unregister((Action<Ping>)second));
            EventBus.Register((Action<Ping>)second);

            Assert.DoesNotThrow(() => EventBus.Execute(new Ping()));
            EventBus.Execute(new Ping());

            Assert.AreEqual(1, secondCalls, "called from the snapshot of the first dispatch only");
        }

        [Test]
        public void RegisteringForAnAbstractType_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("abstract or an interface"));
            EventBus.Register<AbstractEvent>(_ => { });
        }
        #endregion


        #region EventBus<TEnum>
        [Test]
        public void Enum_DeliversWithAndWithoutPayload()
        {
            int pauses = 0, score = 0;
            EventBus<GameEvent>.Register(GameEvent.Paused, () => pauses++);
            EventBus<GameEvent>.Register<int>(GameEvent.ScoreChanged, value => score = value);

            EventBus<GameEvent>.Execute(GameEvent.Paused);
            EventBus<GameEvent>.Execute(GameEvent.ScoreChanged, 50);

            Assert.AreEqual(1, pauses);
            Assert.AreEqual(50, score);
        }

        [Test]
        public void Enum_MixedSignaturesOnOneKey_AreAllowed()
        {
            int plain = 0, withPayload = 0;
            EventBus<GameEvent>.Register(GameEvent.Toast, () => plain++);
            EventBus<GameEvent>.Register<int>(GameEvent.Toast, _ => withPayload++);

            EventBus<GameEvent>.Execute(GameEvent.Toast);
            EventBus<GameEvent>.Execute(GameEvent.Toast, 1);

            Assert.AreEqual(1, plain);
            Assert.AreEqual(1, withPayload);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Enum_PayloadTypeMismatch_Warns()
        {
            bool called = false;
            EventBus<GameEvent>.Register<float>(GameEvent.ScoreChanged, _ => called = true);

            LogAssert.Expect(LogType.Warning, new Regex("ScoreChanged was executed with a Int32 payload.*expect a Single payload"));
            EventBus<GameEvent>.Execute(GameEvent.ScoreChanged, 10);

            Assert.IsFalse(called);
        }

        [Test]
        public void Enum_AcceptsReferenceTypePayloads()
        {
            string message = null;
            EventBus<GameEvent>.Register<string>(GameEvent.Toast, text => message = text);

            EventBus<GameEvent>.Execute(GameEvent.Toast, "Saved");

            Assert.AreEqual("Saved", message);
        }

        [Test]
        public void Enum_DuplicateRegister_IsIgnored()
        {
            int calls = 0;
            void listener() => calls++;
            EventBus<GameEvent>.Register(GameEvent.Paused, listener);
            LogAssert.Expect(LogType.Warning, new Regex("already registered for Paused"));
            EventBus<GameEvent>.Register(GameEvent.Paused, listener);

            EventBus<GameEvent>.Execute(GameEvent.Paused);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Enum_ThrowingListener_IsLogged_AndOthersStillRun()
        {
            int calls = 0;
            EventBus<GameEvent>.Register(GameEvent.Paused, () => throw new InvalidOperationException("enum boom"));
            EventBus<GameEvent>.Register(GameEvent.Paused, () => calls++);

            LogAssert.Expect(LogType.Exception, new Regex("enum boom"));
            EventBus<GameEvent>.Execute(GameEvent.Paused);

            Assert.AreEqual(1, calls);
        }
        #endregion
    }
}
