#region Using
using System;
using System.Text;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Formats durations for UI: as a clock ("02:15", "01:02:15") or compact ("1d 4h", "3m 20s").
    /// <para>
    /// Negative and NaN durations are shown as zero. Seconds are rounded down by default; pass <c>roundUp: true</c>
    /// for countdowns, so 0.4 seconds left shows "00:01" instead of "00:00".
    /// </para>
    /// The only allocation is the returned string, so it's fine to call every frame. To also skip that, only update
    /// the text when the whole number of seconds changes.
    /// </summary>
    public static class TimeFormatting
    {
        #region Variables
        private const long SecondsPerMinute = 60;
        private const long SecondsPerHour = 3600;
        private const long SecondsPerDay = 86400;

        /// <summary>
        /// Durations are clamped to this many seconds (about 31 million years), so they always fit in a long.
        /// </summary>
        private const double MaxSeconds = 1e15;

        /// <summary>
        /// Reused buffers, one per thread. The longest clock is 12 hour digits + ":mm:ss".
        /// </summary>
        [ThreadStatic] private static char[] digits;
        [ThreadStatic] private static StringBuilder builder;
        #endregion


        #region Public Methods
        /// <summary>
        /// Formats a duration as "mm:ss", or "hh:mm:ss" from one hour on (or always, with <paramref name="showHours"/>).
        /// Hours keep counting past 24: 26 hours is "26:00:00".
        /// </summary>
        /// <param name="seconds">Duration in seconds.</param>
        /// <param name="showHours">Show hours even when they are zero: "00:02:15".</param>
        /// <param name="roundUp">Round partial seconds up, for countdowns.</param>
        public static string ToClock(double seconds, bool showHours = false, bool roundUp = false)
        {
            long total = ToWholeSeconds(seconds, roundUp);
            long hours = total / SecondsPerHour;

            char[] chars = digits ??= new char[32];
            int length = 0;
            if (hours > 0 || showHours)
            {
                length = WriteNumber(chars, length, hours, 2);
                chars[length++] = ':';
            }

            length = WriteNumber(chars, length, total / SecondsPerMinute % 60, 2);
            chars[length++] = ':';
            length = WriteNumber(chars, length, total % SecondsPerMinute, 2);

            return new string(chars, 0, length);
        }

        /// <inheritdoc cref="ToClock(double, bool, bool)"/>
        public static string ToClock(this TimeSpan duration, bool showHours = false, bool roundUp = false)
        {
            return ToClock(duration.TotalSeconds, showHours, roundUp);
        }

        /// <summary>
        /// Formats a duration with units, starting from the largest non-zero unit: "45s", "3m 20s", "1d 4h".
        /// Zero is "0s".
        /// </summary>
        /// <param name="seconds">Duration in seconds.</param>
        /// <param name="maxParts">How many units to show, 1 to 4. With 2, 1 day 4 hours 30 minutes is "1d 4h".</param>
        /// <param name="roundUp">Round partial seconds up, for countdowns.</param>
        public static string ToCompact(double seconds, int maxParts = 2, bool roundUp = false)
        {
            return ToCompact(seconds, TimeUnitLabels.Default, maxParts, roundUp);
        }

        /// <summary>
        /// Formats a duration with your own unit labels, e.g. for localization:
        /// <c>ToCompact(seconds, new TimeUnitLabels("j", "h", "min", "s"))</c> gives "1j 4h".
        /// </summary>
        /// <inheritdoc cref="ToCompact(double, int, bool)"/>
        public static string ToCompact(double seconds, TimeUnitLabels labels, int maxParts = 2, bool roundUp = false)
        {
            long total = ToWholeSeconds(seconds, roundUp);
            maxParts = maxParts < 1 ? 1 : maxParts > 4 ? 4 : maxParts;

            long days = total / SecondsPerDay;
            long hours = total / SecondsPerHour % 24;
            long minutes = total / SecondsPerMinute % 60;
            long secs = total % SecondsPerMinute;

            int first = days > 0 ? 0 : hours > 0 ? 1 : minutes > 0 ? 2 : 3;
            int last = Math.Min(first + maxParts - 1, 3);

            char[] chars = digits ??= new char[32];
            StringBuilder text = builder ??= new StringBuilder(32);
            text.Length = 0;

            for (int unit = first; unit <= last; unit++)
            {
                if (unit > first)
                    text.Append(' ');

                long value = unit == 0 ? days : unit == 1 ? hours : unit == 2 ? minutes : secs;
                text.Append(chars, 0, WriteNumber(chars, 0, value, 1));
                text.Append(unit == 0 ? labels.Days : unit == 1 ? labels.Hours : unit == 2 ? labels.Minutes : labels.Seconds);
            }

            return text.ToString();
        }

        /// <inheritdoc cref="ToCompact(double, int, bool)"/>
        public static string ToCompact(this TimeSpan duration, int maxParts = 2, bool roundUp = false)
        {
            return ToCompact(duration.TotalSeconds, TimeUnitLabels.Default, maxParts, roundUp);
        }
        #endregion


        #region Private Methods
        private static long ToWholeSeconds(double seconds, bool roundUp)
        {
            if (double.IsNaN(seconds) || seconds <= 0)
                return 0;

            if (seconds > MaxSeconds)
                seconds = MaxSeconds;

            // The small margin keeps float error (e.g. 3.0000001 left) from showing an extra second when rounding up.
            return roundUp ? (long)Math.Ceiling(seconds - 0.001) : (long)Math.Floor(seconds);
        }

        /// <summary>
        /// Writes a non-negative number into <paramref name="chars"/> at <paramref name="index"/>, padded with zeros to
        /// <paramref name="minDigits"/>, without allocating. Returns the index after the last digit.
        /// </summary>
        private static int WriteNumber(char[] chars, int index, long value, int minDigits)
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
        #endregion
    }

    /// <summary>
    /// Unit labels for <see cref="TimeFormatting.ToCompact(double, TimeUnitLabels, int, bool)"/>.
    /// Each label is written right after its number: "4" + "h" = "4h".
    /// </summary>
    public readonly struct TimeUnitLabels
    {
        /// <summary>
        /// "d", "h", "m" and "s".
        /// </summary>
        public static TimeUnitLabels Default => new TimeUnitLabels("d", "h", "m", "s");

        public string Days { get; }
        public string Hours { get; }
        public string Minutes { get; }
        public string Seconds { get; }

        public TimeUnitLabels(string days, string hours, string minutes, string seconds)
        {
            Days = days;
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }
    }
}
