#region Using
using System;
using System.Globalization;
using NUnit.Framework;
#endregion

namespace NG.Utility.Tests
{
    public class NumberAbbreviationTests
    {
        [TestCase(0, "0")]
        [TestCase(999, "999")]
        [TestCase(1000, "1K")]
        [TestCase(1500, "1.5K")]
        [TestCase(1234, "1.23K")]
        [TestCase(999499, "999.5K")]
        [TestCase(999999, "1M")]
        [TestCase(1500000, "1.5M")]
        [TestCase(2500000000, "2.5B")]
        [TestCase(1000000000000, "1T")]
        [TestCase(-1500, "-1.5K")]
        [TestCase(-999, "-999")]
        [TestCase(long.MaxValue, "9.22Qi")]
        public void Abbreviate_Long(long value, string expected)
        {
            Assert.AreEqual(expected, NumberAbbreviation.Abbreviate(value));
        }

        [Test]
        public void Abbreviate_LargerUnitsThanBillion()
        {
            // The old version stopped at B and printed "2500M" for 2.5 billion.
            Assert.AreEqual("2.5B", 2500000000L.ToAbbreviatedString());
            Assert.AreEqual("1.5Qa", 1.5e15.ToAbbreviatedString());
            Assert.AreEqual("1Dc", 1e33.ToAbbreviatedString());
        }

        [Test]
        public void Abbreviate_PastLastUnit_UsesScientificNotation()
        {
            Assert.AreEqual("1.5E+36", 1.5e36.ToAbbreviatedString());
        }

        [Test]
        public void Abbreviate_AbbreviateFrom_ShowsSmallerNumbersInFull()
        {
            Assert.AreEqual("999999", NumberAbbreviation.Abbreviate(999999L, abbreviateFrom: 1000000));
            Assert.AreEqual("1M", NumberAbbreviation.Abbreviate(1000000L, abbreviateFrom: 1000000));
            Assert.AreEqual("9999", 9999.ToAbbreviatedString(abbreviateFrom: 10000));
            Assert.AreEqual("10K", 10000.ToAbbreviatedString(abbreviateFrom: 10000));
        }

        [Test]
        public void Abbreviate_MaxDecimals()
        {
            Assert.AreEqual("2K", 1500.ToAbbreviatedString(maxDecimals: 0));
            Assert.AreEqual("1.235M", 1234567.ToAbbreviatedString(maxDecimals: 3));
            Assert.AreEqual("1.5K", 1500.ToAbbreviatedString(maxDecimals: 3));
            Assert.AreEqual("2K", 1500.ToAbbreviatedString(maxDecimals: -4));
        }

        [Test]
        public void Abbreviate_Doubles()
        {
            Assert.AreEqual("12.35", 12.345678.ToAbbreviatedString());
            Assert.AreEqual("1K", 999.996.ToAbbreviatedString());
            Assert.AreEqual("0", (-0.001).ToAbbreviatedString());
            Assert.AreEqual("1.5K", 1500f.ToAbbreviatedString());
        }

        [Test]
        public void Abbreviate_NaNAndInfinity()
        {
            Assert.AreEqual(double.NaN.ToString(CultureInfo.InvariantCulture), double.NaN.ToAbbreviatedString());
            Assert.AreEqual(double.PositiveInfinity.ToString(CultureInfo.InvariantCulture), double.PositiveInfinity.ToAbbreviatedString());
        }

        [Test]
        public void Abbreviate_Culture()
        {
            Assert.AreEqual("1.5K", 1500.ToAbbreviatedString());
            Assert.AreEqual("1,5K", 1500.ToAbbreviatedString(provider: CultureInfo.GetCultureInfo("de-DE")));
        }

        [Test]
        public void Abbreviate_MatchesTheFormatStringImplementation()
        {
            // The digits are written by hand for speed; they must match what ToString("0.##") gave before.
            System.Random random = new System.Random(12345);
            IFormatProvider[] cultures = { CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("de-DE") };

            for (int i = 0; i < 20000; i++)
            {
                double magnitude = Math.Pow(10, random.NextDouble() * 20 - 3);
                double value = (random.Next(2) == 0 ? -1 : 1) * magnitude;
                if (i % 7 == 0)
                    value = Math.Round(value, 3);   // values near rounding boundaries

                int decimals = random.Next(0, 5);
                double abbreviateFrom = i % 5 == 0 ? 1000000 : 1000;
                IFormatProvider culture = cultures[i % 2];

                Assert.AreEqual(Reference(value, decimals, abbreviateFrom, culture), NumberAbbreviation.Abbreviate(value, decimals, abbreviateFrom, culture),
                    $"value {value:R}, {decimals} decimals, from {abbreviateFrom}");
            }
        }

        /// <summary>
        /// The previous implementation, which formatted with ToString("0.##").
        /// </summary>
        private static string Reference(double value, int maxDecimals, double abbreviateFrom, IFormatProvider provider)
        {
            string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
            string format = maxDecimals == 0 ? "0" : "0." + new string('#', maxDecimals);

            double rounded = Round(value, maxDecimals);
            if (Math.Abs(rounded) < abbreviateFrom)
                return rounded.ToString(format, provider);

            int unit = 0;
            double scaled = value;
            while (Math.Abs(scaled) >= 1000 && unit < suffixes.Length - 1)
            {
                scaled /= 1000;
                unit++;
            }

            rounded = Round(scaled, maxDecimals);
            if (Math.Abs(rounded) >= 1000 && unit < suffixes.Length - 1)
            {
                unit++;
                rounded = Round(scaled / 1000, maxDecimals);
            }

            if (Math.Abs(rounded) >= 1000)
                return value.ToString(format + "E+0", provider);

            return rounded.ToString(format, provider) + suffixes[unit];
        }

        private static double Round(double value, int decimals)
        {
            double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            return rounded == 0 ? 0 : rounded;
        }
    }
}
