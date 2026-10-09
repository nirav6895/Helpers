#region Using
using System;
using NUnit.Framework;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class SessionStoreTests
    {
        #region Test Types
        private class BaseSettings : ISessionData { }

        private class AudioSettings : BaseSettings
        {
            public float Volume;
        }
        #endregion


        #region Setup
        [SetUp]
        public void SetUp()
        {
            SessionStore.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SessionStore.Clear();
        }
        #endregion


        #region Tests
        [Test]
        public void Set_ThroughABaseTypedVariable_IsFoundByItsConcreteType()
        {
            BaseSettings settings = new AudioSettings { Volume = 0.3f };
            SessionStore.Set(settings);

            AudioSettings loaded = SessionStore.Get<AudioSettings>();

            Assert.AreSame(settings, loaded);
            Assert.AreEqual(0.3f, loaded.Volume);
        }

        [Test]
        public void GetOrCreate_CreatesOnce_ThenReturnsTheSameObject()
        {
            AudioSettings first = SessionStore.GetOrCreate<AudioSettings>();
            first.Volume = 0.8f;

            Assert.AreSame(first, SessionStore.GetOrCreate<AudioSettings>());
            Assert.AreEqual(0.8f, SessionStore.Get<AudioSettings>().Volume);
        }

        [Test]
        public void Get_ForMissingData_ReturnsNull_WithoutCreatingIt()
        {
            Assert.IsNull(SessionStore.Get<AudioSettings>());
            Assert.IsFalse(SessionStore.TryGet(out AudioSettings _));
            Assert.IsFalse(SessionStore.Contains<AudioSettings>());
        }

        [Test]
        public void Remove_And_Clear()
        {
            SessionStore.Set(new AudioSettings());
            Assert.IsTrue(SessionStore.Remove<AudioSettings>());
            Assert.IsFalse(SessionStore.Remove<AudioSettings>());

            SessionStore.Set(new AudioSettings());
            SessionStore.Clear();
            Assert.IsFalse(SessionStore.Contains<AudioSettings>());
        }

        [Test]
        public void Set_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SessionStore.Set(null));
        }
        #endregion
    }
}
