#region Using
using NUnit.Framework;
using UnityEngine;
#endregion

namespace NG.Utility.Tests
{
    public class InternetMonitorStaticTests
    {
        [TearDown]
        public void TearDown()
        {
            InternetMonitor.SimulateOffline = false;
        }

        [Test]
        public void WithoutAMonitor_FallsBackToReachability()
        {
            Assume.That(InternetMonitor.Instance, Is.Null);

            Assert.AreEqual(Application.internetReachability != NetworkReachability.NotReachable, InternetMonitor.IsAvailable);
        }

        [Test]
        public void SimulateOffline_MakesItUnavailable_AndRaisesStatusChanged()
        {
            bool wasAvailable = InternetMonitor.IsAvailable;
            bool? received = null;
            void OnStatusChanged(bool available) => received = available;

            InternetMonitor.StatusChanged += OnStatusChanged;
            try
            {
                InternetMonitor.SimulateOffline = true;

                Assert.IsFalse(InternetMonitor.IsAvailable);
                Assert.AreEqual(wasAvailable ? false : (bool?)null, received);

                received = null;
                InternetMonitor.SimulateOffline = false;

                Assert.AreEqual(wasAvailable, InternetMonitor.IsAvailable);
                Assert.AreEqual(wasAvailable ? true : (bool?)null, received);
            }
            finally
            {
                InternetMonitor.StatusChanged -= OnStatusChanged;
            }
        }
    }
}
