#region Using
using NUnit.Framework;
#endregion

namespace NG.Utility.Tests
{
    public class CountdownTimerTests
    {
        [Test]
        public void CompletesOnce_WhenItReachesZero()
        {
            CountdownTimer timer = new CountdownTimer(1f);
            int completed = 0;
            timer.Completed += () => completed++;

            timer.Start();
            timer.Tick(0.6f);
            Assert.AreEqual(0, completed);
            Assert.AreEqual(0.4f, timer.Remaining, 0.0001f);
            Assert.AreEqual(0.6f, timer.Progress, 0.0001f);

            timer.Tick(0.6f);
            timer.Tick(0.6f);
            Assert.AreEqual(1, completed);
            Assert.IsTrue(timer.IsCompleted);
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0f, timer.Remaining);
        }

        [Test]
        public void DoesNotCount_UntilStarted()
        {
            CountdownTimer timer = new CountdownTimer(1f);
            timer.Tick(5f);

            Assert.AreEqual(1f, timer.Remaining);
            Assert.IsFalse(timer.IsCompleted);
        }

        [Test]
        public void PauseAndResume()
        {
            CountdownTimer timer = new CountdownTimer(1f);
            timer.Start();
            timer.Tick(0.25f);
            timer.Pause();
            timer.Tick(5f);

            Assert.AreEqual(0.75f, timer.Remaining, 0.0001f);

            timer.Resume();
            timer.Tick(0.25f);
            Assert.AreEqual(0.5f, timer.Remaining, 0.0001f);
        }

        [Test]
        public void Stop_ResetsTheRemainingTime()
        {
            CountdownTimer timer = new CountdownTimer(2f);
            timer.Start();
            timer.Tick(1f);
            timer.Stop();

            Assert.AreEqual(2f, timer.Remaining);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void Repeat_KeepsTheOvershoot()
        {
            CountdownTimer timer = new CountdownTimer(1f) { Repeat = true };
            int completed = 0;
            timer.Completed += () => completed++;

            timer.Start();
            timer.Tick(1.25f);

            Assert.AreEqual(1, completed);
            Assert.IsTrue(timer.IsRunning);
            Assert.AreEqual(0.75f, timer.Remaining, 0.0001f);

            timer.Tick(0.75f);
            Assert.AreEqual(2, completed);
        }

        [Test]
        public void Completed_CanRestartTheTimer()
        {
            CountdownTimer timer = new CountdownTimer(1f);
            timer.Completed += () => timer.Start(2f);

            timer.Start();
            timer.Tick(1f);

            Assert.IsTrue(timer.IsRunning);
            Assert.AreEqual(2f, timer.Remaining);
        }

        [Test]
        public void ZeroDuration_CompletesOnFirstTick()
        {
            CountdownTimer timer = new CountdownTimer(0f);
            int completed = 0;
            timer.Completed += () => completed++;

            timer.Start();
            timer.Tick(0f);

            Assert.AreEqual(1, completed);
        }
    }
}
