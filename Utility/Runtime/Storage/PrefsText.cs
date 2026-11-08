#region Using
using System;
using System.Globalization;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// The text forms <see cref="Prefs"/> and <see cref="SecurePrefs"/> store values in, in the invariant culture.
    /// Both use the same forms, so plain values can be moved into SecurePrefs as they are.
    /// </summary>
    internal static class PrefsText
    {
        #region Bool
        public static string FromBool(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>
        /// Reads "1"/"0", or any int (non-zero is true), like a bool stored with PlayerPrefs.SetInt.
        /// </summary>
        public static bool TryParseBool(string text, out bool value)
        {
            bool parsed = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number);
            value = parsed && number != 0;
            return parsed;
        }
        #endregion


        #region Numbers
        public static string FromInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseInt(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static string FromFloat(float value)
        {
            // "G9" always round-trips a float.
            return value.ToString("G9", CultureInfo.InvariantCulture);
        }

        public static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static string FromLong(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseLong(string text, out long value)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public static string FromDouble(double value)
        {
            // "G17" always round-trips; "R" doesn't on every .NET runtime.
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }

        public static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
        #endregion


        #region Enum and DateTime
        /// <summary>
        /// Enums are stored by name, so reordering or renumbering the members keeps the value.
        /// </summary>
        public static string FromEnum<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return value.ToString();
        }

        public static bool TryParseEnum<TEnum>(string text, out TEnum value) where TEnum : struct, Enum
        {
            return Enum.TryParse(text, out value);
        }

        /// <summary>
        /// DateTimes are stored with <see cref="DateTime.ToBinary"/>, which keeps their Kind.
        /// </summary>
        public static string FromDateTime(DateTime value)
        {
            return value.ToBinary().ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseDateTime(string text, out DateTime value)
        {
            value = default;
            if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long binary))
                return false;

            try
            {
                value = DateTime.FromBinary(binary);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        #endregion


        #region Object
        public static string FromObject<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        /// <summary>
        /// Reads JSON written by <see cref="FromObject{T}"/>. Invalid JSON logs a warning (prefixed with
        /// <paramref name="logPrefix"/>) and returns false. Empty text returns false without a warning.
        /// </summary>
        public static bool TryParseObject<T>(string text, string key, string logPrefix, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                value = JsonUtility.FromJson<T>(text);
                return true;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[{logPrefix}] '{key}' doesn't hold valid JSON for {typeof(T).Name}, so the default value was returned: {exception.Message}");
                return false;
            }
        }
        #endregion
    }
}
