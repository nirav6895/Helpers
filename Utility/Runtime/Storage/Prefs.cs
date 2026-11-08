#region Using
using System;
using System.Globalization;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// <see cref="PlayerPrefs"/> for more types: bool, long, double, enums, DateTime and [Serializable] objects (as JSON).
    /// <code>
    /// Prefs.SetBool("music", true);
    /// Prefs.SetDateTime("lastDailyReward", DateTime.UtcNow);
    /// Settings settings = Prefs.GetObject("settings", new Settings());
    /// </code>
    /// <para>
    /// Values other than bool are stored as strings in the invariant culture, so they read back the same on every device.
    /// A missing key, or a value that can't be read as the requested type, returns the default value.
    /// Like PlayerPrefs, values are written to disk on <see cref="Save"/> or when the app quits, and it's main thread only.
    /// </para>
    /// PlayerPrefs isn't encrypted: don't use it for purchases or anything players shouldn't be able to edit.
    /// </summary>
    public static class Prefs
    {
        #region Bool
        /// <summary>
        /// Reads a bool, stored as an int (1 or 0).
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        /// <summary>
        /// Stores a bool as an int (1 or 0).
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }
        #endregion


        #region Long
        /// <summary>
        /// Reads a long, e.g. a currency amount larger than an int can hold.
        /// </summary>
        public static long GetLong(string key, long defaultValue = 0)
        {
            return TryGetString(key, out string text) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Stores a long as a string.
        /// </summary>
        public static void SetLong(string key, long value)
        {
            PlayerPrefs.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }
        #endregion


        #region Double
        /// <summary>
        /// Reads a double, with full precision (PlayerPrefs.GetFloat only has float precision).
        /// </summary>
        public static double GetDouble(string key, double defaultValue = 0)
        {
            return TryGetString(key, out string text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Stores a double as a string that reads back exactly.
        /// </summary>
        public static void SetDouble(string key, double value)
        {
            // "G17" always round-trips; "R" doesn't on every .NET runtime.
            PlayerPrefs.SetString(key, value.ToString("G17", CultureInfo.InvariantCulture));
        }
        #endregion


        #region Enum
        /// <summary>
        /// Reads an enum stored by name. A name that is no longer in the enum returns the default value.
        /// </summary>
        public static TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default) where TEnum : struct, Enum
        {
            return TryGetString(key, out string text) && Enum.TryParse(text, out TEnum value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Stores an enum by name, so reordering or renumbering the enum's members keeps the value.
        /// </summary>
        public static void SetEnum<TEnum>(string key, TEnum value) where TEnum : struct, Enum
        {
            PlayerPrefs.SetString(key, value.ToString());
        }
        #endregion


        #region DateTime
        /// <summary>
        /// Reads a DateTime, with its Kind (UTC or local). Prefer UTC times, e.g. <see cref="DateTime.UtcNow"/>:
        /// local times are converted to the device's current time zone when read.
        /// </summary>
        public static DateTime GetDateTime(string key, DateTime defaultValue = default)
        {
            if (!TryGetString(key, out string text) || !long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long binary))
                return defaultValue;

            try
            {
                return DateTime.FromBinary(binary);
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Stores a DateTime, with its Kind (UTC or local).
        /// </summary>
        public static void SetDateTime(string key, DateTime value)
        {
            PlayerPrefs.SetString(key, value.ToBinary().ToString(CultureInfo.InvariantCulture));
        }
        #endregion


        #region Object
        /// <summary>
        /// Reads an object stored as JSON with <see cref="JsonUtility"/>. <typeparamref name="T"/> must be a
        /// [Serializable] class or struct (not a MonoBehaviour or ScriptableObject). Invalid JSON logs a warning
        /// and returns the default value.
        /// </summary>
        public static T GetObject<T>(string key, T defaultValue = default)
        {
            if (!TryGetString(key, out string json) || string.IsNullOrEmpty(json))
                return defaultValue;

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[Prefs] '{key}' doesn't hold valid JSON for {typeof(T).Name}, so the default value was returned: {exception.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Stores an object as JSON with <see cref="JsonUtility"/>, so only fields Unity can serialize are saved.
        /// </summary>
        public static void SetObject<T>(string key, T value)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        }
        #endregion


        #region Keys
        /// <inheritdoc cref="PlayerPrefs.HasKey"/>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <inheritdoc cref="PlayerPrefs.DeleteKey"/>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        /// <inheritdoc cref="PlayerPrefs.Save"/>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        #endregion


        #region Private Methods
        private static bool TryGetString(string key, out string text)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                text = null;
                return false;
            }

            text = PlayerPrefs.GetString(key);
            return true;
        }
        #endregion
    }
}
