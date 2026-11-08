#region Using
using System;
using System.Globalization;
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
            long minutes = total / SecondsPerMinute % 60;
            long secs = total % SecondsPerMinute;

            return hours > 0 || showHours
                ? string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", hours, minutes, secs)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, secs);
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

            StringBuilder builder = new StringBuilder(16);
            for (int unit = first; unit <= last; unit++)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                switch (unit)
                {
                    case 0: builder.Append(days.ToString(CultureInfo.InvariantCulture)).Append(labels.Days); break;
                    case 1: builder.Append(hours.ToString(CultureInfo.InvariantCulture)).Append(labels.Hours); break;
                    case 2: builder.Append(minutes.ToString(CultureInfo.InvariantCulture)).Append(labels.Minutes); break;
                    default: builder.Append(secs.ToString(CultureInfo.InvariantCulture)).Append(labels.Seconds); break;
                }
            }

            return builder.ToString();
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
