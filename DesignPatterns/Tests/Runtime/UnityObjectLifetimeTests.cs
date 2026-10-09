#region Using
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    /// <summary>
    /// Checks that the static registries notice Unity objects that were destroyed without unregistering.
    /// These run in Play Mode because Unity won't attach MonoBehaviours from an Editor-only test assembly.
    /// </summary>
    public class UnityObjectLifetimeTests
    {
        private struct Ping : IEvent { }

        private enum GameEvent { Paused }

        [SetUp]
        public void SetUp()
        {
            ClearRegistries();
        }

        [TearDown]
        public void TearDown()
        {
            ClearRegistries();
        }

        [Test]
        public void EventObserver_RemovesListenersOfDestroyedObjects_WithAWarning()
        {
            var gameObject = new GameObject("Listener");
            var listener = gameObject.AddComponent<TestListener>();
            EventObserver.Register<Ping>(listener.OnPing);
            Object.DestroyImmediate(gameObject);

            LogAssert.Expect(LogType.Warning, new Regex("TestListener.OnPing.*destroyed without unregistering"));
            EventObserver.Execute(new Ping());

            Assert.AreEqual(0, listener.Calls);
            Assert.IsFalse(EventObserver.HasListeners<Ping>());
        }

        [Test]
        public void EnumEventObserver_RemovesListenersOfDestroyedObjects_WithAWarning()
        {
            var gameObject = new GameObject("Listener");
            var listener = gameObject.AddComponent<TestListener>();
            EventObserver<GameEvent>.Register(GameEvent.Paused, listener.OnPaused);
            Object.DestroyImmediate(gameObject);

            LogAssert.Expect(LogType.Warning, new Regex("TestListener.OnPaused.*destroyed without unregistering"));
            EventObserver<GameEvent>.Execute(GameEvent.Paused);

            Assert.AreEqual(0, listener.Calls);
        }

        [Test]
        public void ServiceLocator_TreatsDestroyedServicesAsMissing_WithAWarning()
        {
            var gameObject = new GameObject("Service");
            ServiceLocator.Add<ITestMonoService>(gameObject.AddComponent<TestService>());
            Object.DestroyImmediate(gameObject);

            LogAssert.Expect(LogType.Warning, new Regex("destroyed without being removed"));
            Assert.IsFalse(ServiceLocator.TryGet(out ITestMonoService _));
            Assert.IsFalse(ServiceLocator.Contains<ITestMonoService>());
        }

        private static void ClearRegistries()
        {
            EventObserver.Clear();
            EventObserver<GameEvent>.Clear();
            ServiceLocator.Clear();
        }
    }
}
