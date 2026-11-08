#region Using
using System;
using NUnit.Framework;
#endregion

namespace NG.Utility.Tests
{
    public class TimeFormattingTests
    {
        [TestCase(0, "00:00")]
        [TestCase(5, "00:05")]
        [TestCase(65, "01:05")]
        [TestCase(59.9, "00:59")]
        [TestCase(3599, "59:59")]
        [TestCase(3605, "01:00:05")]
        [TestCase(26 * 3600, "26:00:00")]
        [TestCase(-5, "00:00")]
        [TestCase(double.NaN, "00:00")]
        public void ToClock(double seconds, string expected)
        {
            Assert.AreEqual(expected, TimeFormatting.ToClock(seconds));
        }

        [Test]
        public void ToClock_ShowHours()
        {
            Assert.AreEqual("00:02:15", TimeFormatting.ToClock(135, showHours: true));
        }

        [Test]
        public void ToClock_RoundUp_ForCountdowns()
        {
            Assert.AreEqual("00:01", TimeFormatting.ToClock(0.4, roundUp: true));
            Assert.AreEqual("01:00", TimeFormatting.ToClock(59.2, roundUp: true));
            Assert.AreEqual("00:03", TimeFormatting.ToClock(3.0000001, roundUp: true), "Float error must not add a second.");
            Assert.AreEqual("00:00", TimeFormatting.ToClock(0, roundUp: true));
        }

        [Test]
        public void ToClock_TimeSpan()
        {
            Assert.AreEqual("01:30", TimeSpan.FromSeconds(90).ToClock());
        }

        [TestCase(0, "0s")]
        [TestCase(45, "45s")]
        [TestCase(125, "2m 5s")]
        [TestCase(3600, "1h 0m")]
        [TestCase(26 * 3600 + 60, "1d 2h")]
        public void ToCompact(double seconds, string expected)
        {
            Assert.AreEqual(expected, TimeFormatting.ToCompact(seconds));
        }

        [Test]
        public void ToCompact_MaxParts()
        {
            double seconds = 26 * 3600 + 61;
            Assert.AreEqual("1d", TimeFormatting.ToCompact(seconds, maxParts: 1));
            Assert.AreEqual("1d 2h 1m", TimeFormatting.ToCompact(seconds, maxParts: 3));
            Assert.AreEqual("1d 2h 1m 1s", TimeFormatting.ToCompact(seconds, maxParts: 10));
        }

        [Test]
        public void ToClock_MatchesStringFormat()
        {
            // The digits are written by hand for speed; they must match string.Format("{0:00}:{1:00}").
            System.Random random = new System.Random(4321);
            for (int i = 0; i < 20000; i++)
            {
                long total = i < 10000 ? i : (long)(random.NextDouble() * 400 * 3600);
                long hours = total / 3600;
                long minutes = total / 60 % 60;
                long seconds = total % 60;
                bool showHours = i % 3 == 0;

                string expected = hours > 0 || showHours
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", hours, minutes, seconds)
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, seconds);

                Assert.AreEqual(expected, TimeFormatting.ToClock(total, showHours));
            }
        }

        [Test]
        public void ToCompact_CustomLabels()
        {
            TimeUnitLabels french = new TimeUnitLabels("j", "h", "min", "s");
            Assert.AreEqual("1j 4h", TimeFormatting.ToCompact(28 * 3600, french));
            Assert.AreEqual("3min 20s", TimeFormatting.ToCompact(200, french));
        }
    }
}
