#region Using
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
    }
}
