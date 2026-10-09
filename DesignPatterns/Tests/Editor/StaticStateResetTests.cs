#region Using
using NUnit.Framework;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class StaticStateResetTests
    {
        private struct ResetEvent : IEvent { }

        private enum ResetKey { A }

        private interface IResetService : IService { }

        private class ResetService : IResetService { }

        private class ResetData : ISessionData { }

        /// <summary>
        /// ResetAll is what Unity calls when Play Mode starts, so this is what keeps a Play session without
        /// Domain Reload from inheriting the previous session's registrations.
        /// </summary>
        [Test]
        public void ResetAll_ClearsEveryRegistry()
        {
            int enumCalls = 0;
            ServiceLocator.Add<IResetService>(new ResetService());
            SessionStore.Set(new ResetData());
            EventObserver.Register<ResetEvent>(_ => { });
            EventObserver<ResetKey>.Register(ResetKey.A, () => enumCalls++);

            StaticStateReset.ResetAll();

            Assert.IsFalse(ServiceLocator.Contains<IResetService>());
            Assert.IsFalse(SessionStore.Contains<ResetData>());
            Assert.IsFalse(EventObserver.HasListeners<ResetEvent>());
            EventObserver<ResetKey>.Execute(ResetKey.A);
            Assert.AreEqual(0, enumCalls);
        }
    }
}
