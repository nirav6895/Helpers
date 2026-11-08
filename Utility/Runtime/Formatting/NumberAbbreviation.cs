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
    /// </summary>
    public static class NumberAbbreviation
    {
        #region Variables
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        /// <summary>
        /// The most decimals <see cref="Math.Round(double, int, MidpointRounding)"/> supports.
        /// </summary>
        private const int MaxDecimalsLimit = 15;

        private static readonly string[] Formats = BuildFormats("0.", "");
        private static readonly string[] ScientificFormats = BuildFormats("0.", "E+0");
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

            maxDecimals = maxDecimals < 0 ? 0 : maxDecimals > MaxDecimalsLimit ? MaxDecimalsLimit : maxDecimals;
            string format = Formats[maxDecimals];

            double rounded = Round(value, maxDecimals);
            if (Math.Abs(rounded) < abbreviateFrom)
                return rounded.ToString(format, provider);

            int unit = 0;
            double scaled = value;
            while (Math.Abs(scaled) >= 1000 && unit < Suffixes.Length - 1)
            {
                scaled /= 1000;
                unit++;
            }

            // Rounding can carry into the next unit: 999999 is 999.999K, which would show as "1000K".
            rounded = Round(scaled, maxDecimals);
            if (Math.Abs(rounded) >= 1000 && unit < Suffixes.Length - 1)
            {
                unit++;
                rounded = Round(scaled / 1000, maxDecimals);
            }

            // Past the last unit.
            if (Math.Abs(rounded) >= 1000)
                return value.ToString(ScientificFormats[maxDecimals], provider);

            return rounded.ToString(format, provider) + Suffixes[unit];
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
        private static double Round(double value, int decimals)
        {
            double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

            // Avoid "-0" for small negative numbers.
            return rounded == 0 ? 0 : rounded;
        }

        private static string[] BuildFormats(string prefix, string suffix)
        {
            string[] formats = new string[MaxDecimalsLimit + 1];
            for (int i = 0; i < formats.Length; i++)
                formats[i] = (i == 0 ? "0" : prefix + new string('#', i)) + suffix;

            return formats;
        }
        #endregion
    }
}
