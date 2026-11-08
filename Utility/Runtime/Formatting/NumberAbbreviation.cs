#region Using
using System;
using System.Globalization;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Formats numbers in a short form for UI: 1500 → "1.5K", 2500000 → "2.5M", 999999 → "1M".
    /// <para>
    /// Units are K (thousand), M (million), B (billion), T (trillion), then Qa, Qi, Sx, Sp, Oc, No and Dc for idle games.
    /// Larger numbers are shown in scientific notation, e.g. "1.5E+36".
    /// </para>
    /// <para>
    /// Numbers are rounded (half away from zero) to <c>maxDecimals</c> decimals and trailing zeros are dropped: "1.5K", not "1.50K".
    /// Numbers below <c>abbreviateFrom</c> are shown in full. The invariant culture is used by default, so the decimal
    /// separator is always "."; pass e.g. <see cref="CultureInfo.CurrentCulture"/> to localize it.
    /// </para>
    /// The only allocation is the returned string.
    /// </summary>
    public static class NumberAbbreviation
    {
        #region Variables
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        /// <summary>
        /// The most decimals <see cref="Math.Round(double, int, MidpointRounding)"/> supports.
        /// </summary>
        private const int MaxDecimalsLimit = 15;

        /// <summary>
        /// Scaled values from this size on are formatted with double.ToString instead, so they never overflow a long.
        /// </summary>
        private const double MaxScaled = 1e18;

        private static readonly long[] PowersOfTen = BuildPowersOfTen();
        private static readonly string[] FullFormats = BuildFormats(string.Empty);
        private static readonly string[] ScientificFormats = BuildFormats("E+0");

        /// <summary>
        /// Reused buffer, one per thread: sign, up to 19 digits, separator, 15 decimals and a suffix.
        /// </summary>
        [ThreadStatic] private static char[] buffer;
        #endregion


        #region Public Methods
        /// <summary>
        /// Formats <paramref name="value"/> in a short form, e.g. 1500 → "1.5K".
        /// </summary>
        /// <param name="value">The number to format.</param>
        /// <param name="maxDecimals">Most decimals shown, 0 to 15. Trailing zeros are dropped.</param>
        /// <param name="abbreviateFrom">Numbers whose absolute value is below this are shown in full.
        /// E.g. 1000000 shows 999999 in full and abbreviates from "1M".</param>
        /// <param name="provider">Culture used for the decimal separator. Defaults to the invariant culture.</param>
        public static string Abbreviate(double value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(provider);

            int decimals = maxDecimals < 0 ? 0 : maxDecimals > MaxDecimalsLimit ? MaxDecimalsLimit : maxDecimals;
            long power = PowersOfTen[decimals];
            double magnitude = Math.Abs(value);

            // Shown in full: the number rounded to the decimals is below the threshold.
            double fullScaled = Math.Round(magnitude * power, MidpointRounding.AwayFromZero);
            if (fullScaled / power < abbreviateFrom)
            {
                return fullScaled < MaxScaled
                    ? Format(value < 0, (long)fullScaled, decimals, string.Empty, provider)
                    : Math.Round(value, decimals, MidpointRounding.AwayFromZero).ToString(FullFormats[decimals], provider);
            }

            int unit = 0;
            double scaled = magnitude;
            while (scaled >= 1000 && unit < Suffixes.Length - 1)
            {
                scaled /= 1000;
                unit++;
            }

            // Rounding can carry into the next unit: 999999 is 999.999K, which would show as "1000K".
            double rounded = Math.Round(scaled * power, MidpointRounding.AwayFromZero);
            if (rounded >= 1000.0 * power && unit < Suffixes.Length - 1)
            {
                unit++;
                rounded = Math.Round(scaled / 1000 * power, MidpointRounding.AwayFromZero);
            }

            // Past the last unit.
            if (rounded >= 1000.0 * power)
                return value.ToString(ScientificFormats[decimals], provider);

            return Format(value < 0, (long)rounded, decimals, Suffixes[unit], provider);
        }

        /// <inheritdoc cref="Abbreviate(double, int, double, IFormatProvider)"/>
        public static string Abbreviate(long value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            // Below the threshold, format the long itself: converting to double loses digits above 2^53.
            if (Math.Abs((double)value) < abbreviateFrom)
                return value.ToString(provider ?? CultureInfo.InvariantCulture);

            return Abbreviate((double)value, maxDecimals, abbreviateFrom, provider);
        }

        /// <inheritdoc cref="Abbreviate(double, int, double, IFormatProvider)"/>
        public static string ToAbbreviatedString(this int value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            return Abbreviate((long)value, maxDecimals, abbreviateFrom, provider);
        }

        /// <inheritdoc cref="Abbreviate(double, int, double, IFormatProvider)"/>
        public static string ToAbbreviatedString(this long value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            return Abbreviate(value, maxDecimals, abbreviateFrom, provider);
        }

        /// <inheritdoc cref="Abbreviate(double, int, double, IFormatProvider)"/>
        public static string ToAbbreviatedString(this float value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            return Abbreviate((double)value, maxDecimals, abbreviateFrom, provider);
        }

        /// <inheritdoc cref="Abbreviate(double, int, double, IFormatProvider)"/>
        public static string ToAbbreviatedString(this double value, int maxDecimals = 2, double abbreviateFrom = 1000, IFormatProvider provider = null)
        {
            return Abbreviate(value, maxDecimals, abbreviateFrom, provider);
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Writes <paramref name="scaled"/> / 10^<paramref name="decimals"/> without trailing zeros, then the suffix,
        /// e.g. 150 with 2 decimals and "K" is "1.5K". Same output as ToString("0.##") with the culture's separators.
        /// </summary>
        private static string Format(bool negative, long scaled, int decimals, string suffix, IFormatProvider provider)
        {
            // Drop trailing zeros.
            while (decimals > 0 && scaled % 10 == 0)
            {
                scaled /= 10;
                decimals--;
            }

            NumberFormatInfo format = NumberFormatInfo.GetInstance(provider);
            char[] chars = buffer ??= new char[64];
            int length = 0;

            // No "-0" for numbers that round to zero.
            if (negative && scaled != 0)
                length = Append(chars, length, format.NegativeSign);

            long power = PowersOfTen[decimals];
            length = WriteDigits(chars, length, scaled / power, 1);
            if (decimals > 0)
            {
                length = Append(chars, length, format.NumberDecimalSeparator);
                length = WriteDigits(chars, length, scaled % power, decimals);
            }

            length = Append(chars, length, suffix);
            return new string(chars, 0, length);
        }

        private static int WriteDigits(char[] chars, int index, long value, int minDigits)
        {
            int count = 1;
            for (long rest = value; rest >= 10; rest /= 10)
                count++;

            if (count < minDigits)
                count = minDigits;

            for (int i = index + count - 1; i >= index; i--)
            {
                chars[i] = (char)('0' + value % 10);
                value /= 10;
            }

            return index + count;
        }

        private static int Append(char[] chars, int index, string text)
        {
            text.CopyTo(0, chars, index, text.Length);
            return index + text.Length;
        }

        private static long[] BuildPowersOfTen()
        {
            long[] powers = new long[MaxDecimalsLimit + 1];
            powers[0] = 1;
            for (int i = 1; i < powers.Length; i++)
                powers[i] = powers[i - 1] * 10;

            return powers;
        }

        /// <summary>
        /// "0", "0.#", "0.##"... with <paramref name="suffix"/>, for the rare cases formatted with double.ToString.
        /// </summary>
        private static string[] BuildFormats(string suffix)
        {
            string[] formats = new string[MaxDecimalsLimit + 1];
            for (int i = 0; i < formats.Length; i++)
                formats[i] = (i == 0 ? "0" : "0." + new string('#', i)) + suffix;

            return formats;
        }
        #endregion
    }
}
