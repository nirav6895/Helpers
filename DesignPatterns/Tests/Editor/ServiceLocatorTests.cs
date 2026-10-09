#region Using
using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class ServiceLocatorTests
    {
        #region Test Types
        private interface IAudioService : IService { }

        private interface IVolumeService : IService { }

        private class AudioService : IAudioService { }

        private class Mixer : IAudioService, IVolumeService { }
        #endregion


        #region Setup
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }
        #endregion


        #region Tests
        [Test]
        public void AddByInterface_IsFoundByInterface()
        {
            var service = new AudioService();
            ServiceLocator.Add<IAudioService>(service);

            Assert.AreSame(service, ServiceLocator.Get<IAudioService>());
            Assert.IsTrue(ServiceLocator.Contains<IAudioService>());
        }

        [Test]
        public void AddByConcreteType_ThenGetByInterface_ExplainsTheMistake()
        {
            ServiceLocator.Add(new AudioService());

            LogAssert.Expect(LogType.Error, new Regex("IAudioService is not registered, but AudioService .*registered under its own type"));
            Assert.IsNull(ServiceLocator.Get<IAudioService>());
        }

        [Test]
        public void Get_ForAMissingService_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("IAudioService is not registered\\."));
            Assert.IsNull(ServiceLocator.Get<IAudioService>());
        }

        [Test]
        public void TryGet_ForAMissingService_ReturnsFalse_WithoutLogging()
        {
            Assert.IsFalse(ServiceLocator.TryGet(out IAudioService _));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RemoveInstance_LeavesAReplacementAlone()
        {
            var oldService = new AudioService();
            var newService = new AudioService();
            ServiceLocator.Add<IAudioService>(oldService);
            ServiceLocator.Add<IAudioService>(newService);   // e.g. registered by an additively loaded scene

            Assert.IsFalse(ServiceLocator.Remove(oldService));   // the old service's OnDestroy
            Assert.AreSame(newService, ServiceLocator.Get<IAudioService>());
        }

        [Test]
        public void RemoveInstance_RemovesEveryRegistrationOfThatInstance()
        {
            var mixer = new Mixer();
            ServiceLocator.Add<IAudioService>(mixer);
            ServiceLocator.Add<IVolumeService>(mixer);

            Assert.IsTrue(ServiceLocator.Remove(mixer));
            Assert.IsFalse(ServiceLocator.Contains<IAudioService>());
            Assert.IsFalse(ServiceLocator.Contains<IVolumeService>());
        }

        [Test]
        public void RemoveByType_RemovesWhateverIsRegistered()
        {
            ServiceLocator.Add<IAudioService>(new AudioService());

            Assert.IsTrue(ServiceLocator.Remove<IAudioService>());
            Assert.IsFalse(ServiceLocator.Contains<IAudioService>());
        }

        [Test]
        public void Add_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Add<IAudioService>(null));
        }
        #endregion
    }
}
