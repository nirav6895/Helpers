#region Using
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Encrypted <see cref="PlayerPrefs"/>, with the same types as <see cref="Prefs"/> plus int, float and string.
    /// Players can't read the values, and edited values are detected and ignored.
    /// <code>
    /// // Once, before the first scene loads:
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// static void ConfigurePrefs() => SecurePrefs.Configure("a long random string only your game uses");
    ///
    /// SecurePrefs.SetLong("coins", 1500);
    /// long coins = SecurePrefs.GetLong("coins");
    /// </code>
    /// <para>
    /// Each value is encrypted with AES-256 (a new random IV every time) and signed with HMAC-SHA256 together with
    /// its key name. A value that was edited, copied from another key, or saved with a different secret fails the
    /// check: Get returns the default value, logs a warning and raises <see cref="TamperDetected"/>.
    /// Key names are stored hashed, so players can't tell which entry is which.
    /// </para>
    /// <para>
    /// The secret ships inside your game, so this stops players editing their saves with a text or registry editor,
    /// not someone who reverse-engineers the build. It also can't stop a player restoring an older copy of their
    /// own saves. Keep purchases and anything valuable on a server.
    /// </para>
    /// Main thread only, like PlayerPrefs. Cache values you read often instead of reading them every frame.
    /// </summary>
    public static class SecurePrefs
    {
        #region Variables
        /// <summary>
        /// Raised with the key name when a stored value fails verification: it was edited, copied from another key or
        /// device, or saved with a different secret. The value is left as it is; Get returns the default value.
        /// </summary>
        public static event Action<string> TamperDetected;

        private const string LogPrefix = "SecurePrefs";
        private const string StorageKeyPrefix = "ngsp.";
        private const string Salt = "NG.Utility.SecurePrefs";
        private const byte FormatVersion = 1;
        private const int BlockSize = 16;
        private const int MacSize = 32;
        private const int HeaderSize = 1 + BlockSize;

        private static readonly RandomNumberGenerator RandomBytes = RandomNumberGenerator.Create();

        private static byte[] encryptionKey;
        private static byte[] authenticationKey;
        private static byte[] keyNameKey;
        private static bool isDeviceBound;
        #endregion


        #region Properties
        /// <summary>
        /// Whether <see cref="Configure"/> has been called.
        /// </summary>
        public static bool IsConfigured => encryptionKey != null;

        /// <summary>
        /// Whether values are bound to this device. False if device binding wasn't asked for, or isn't supported here.
        /// </summary>
        public static bool IsDeviceBound => isDeviceBound;
        #endregion


        #region Configuration
        /// <summary>
        /// Sets the secret the keys are made from. Call it once at startup, before any other SecurePrefs method.
        /// <para>
        /// Use a long random string only your game uses, and never change it: values saved with another secret
        /// can't be read.
        /// </para>
        /// </summary>
        /// <param name="secret">Your game's secret.</param>
        /// <param name="bindToDevice">Mix <see cref="SystemInfo.deviceUniqueIdentifier"/> into the keys, so values
        /// copied to another device can't be read. Values restored from a backup onto a new phone are lost too, and
        /// if the identifier changes (e.g. the app is reinstalled on iOS) existing values are lost. Ignored, with a
        /// warning, where the identifier isn't available, e.g. on WebGL.</param>
        public static void Configure(string secret, bool bindToDevice = false)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("The secret can't be empty.", nameof(secret));

            string salt = Salt;
            isDeviceBound = false;

            if (bindToDevice)
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
                {
                    Debug.LogWarning($"[{LogPrefix}] This platform has no device identifier, so values aren't bound to the device.");
                }
                else
                {
                    salt += "|" + deviceId;
                    isDeviceBound = true;
                }
            }

            // HKDF-style: one master key from the secret, then a separate key for each use.
            byte[] masterKey = Hmac(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(salt));
            encryptionKey = Hmac(masterKey, Encoding.UTF8.GetBytes("encryption"));
            authenticationKey = Hmac(masterKey, Encoding.UTF8.GetBytes("authentication"));
            keyNameKey = Hmac(masterKey, Encoding.UTF8.GetBytes("key names"));
        }
        #endregion


        #region Bool, Int, Float, String
        public static bool GetBool(string key, bool defaultValue = false)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseBool(text, out bool value) ? value : defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
            SetText(key, PrefsText.FromBool(value));
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseInt(text, out int value) ? value : defaultValue;
        }

        public static void SetInt(string key, int value)
        {
            SetText(key, PrefsText.FromInt(value));
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseFloat(text, out float value) ? value : defaultValue;
        }

        public static void SetFloat(string key, float value)
        {
            SetText(key, PrefsText.FromFloat(value));
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return TryGetText(key, out string text) ? text : defaultValue;
        }

        public static void SetString(string key, string value)
        {
            SetText(key, value ?? string.Empty);
        }
        #endregion


        #region Long, Double, Enum, DateTime, Object
        /// <inheritdoc cref="Prefs.GetLong"/>
        public static long GetLong(string key, long defaultValue = 0)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseLong(text, out long value) ? value : defaultValue;
        }

        public static void SetLong(string key, long value)
        {
            SetText(key, PrefsText.FromLong(value));
        }

        /// <inheritdoc cref="Prefs.GetDouble"/>
        public static double GetDouble(string key, double defaultValue = 0)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseDouble(text, out double value) ? value : defaultValue;
        }

        public static void SetDouble(string key, double value)
        {
            SetText(key, PrefsText.FromDouble(value));
        }

        /// <inheritdoc cref="Prefs.GetEnum{TEnum}"/>
        public static TEnum GetEnum<TEnum>(string key, TEnum defaultValue = default) where TEnum : struct, Enum
        {
            return TryGetText(key, out string text) && PrefsText.TryParseEnum(text, out TEnum value) ? value : defaultValue;
        }

        public static void SetEnum<TEnum>(string key, TEnum value) where TEnum : struct, Enum
        {
            SetText(key, PrefsText.FromEnum(value));
        }

        /// <inheritdoc cref="Prefs.GetDateTime"/>
        public static DateTime GetDateTime(string key, DateTime defaultValue = default)
        {
            return TryGetText(key, out string text) && PrefsText.TryParseDateTime(text, out DateTime value) ? value : defaultValue;
        }

        public static void SetDateTime(string key, DateTime value)
        {
            SetText(key, PrefsText.FromDateTime(value));
        }

        /// <inheritdoc cref="Prefs.GetObject{T}"/>
        public static T GetObject<T>(string key, T defaultValue = default)
        {
            return TryGetText(key, out string json) && PrefsText.TryParseObject(json, key, LogPrefix, out T value) ? value : defaultValue;
        }

        public static void SetObject<T>(string key, T value)
        {
            SetText(key, PrefsText.FromObject(value));
        }
        #endregion


        #region Keys
        /// <summary>
        /// Whether a value is stored for <paramref name="key"/>, even one that fails verification.
        /// </summary>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(GetStorageKey(key));
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(GetStorageKey(key));
        }

        /// <inheritdoc cref="PlayerPrefs.Save"/>
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        #endregion


        #region Migration
        /// <summary>
        /// Moves a value saved with PlayerPrefs.SetInt (or <see cref="Prefs.SetBool"/>) into SecurePrefs and deletes
        /// the plain value. Read it afterwards with <see cref="GetInt"/> or <see cref="GetBool"/>.
        /// </summary>
        /// <inheritdoc cref="MigrateString"/>
        public static bool MigrateInt(string key)
        {
            return Migrate(key, () => PrefsText.FromInt(PlayerPrefs.GetInt(key)));
        }

        /// <summary>
        /// Moves a value saved with PlayerPrefs.SetFloat into SecurePrefs and deletes the plain value.
        /// Read it afterwards with <see cref="GetFloat"/>.
        /// </summary>
        /// <inheritdoc cref="MigrateString"/>
        public static bool MigrateFloat(string key)
        {
            return Migrate(key, () => PrefsText.FromFloat(PlayerPrefs.GetFloat(key)));
        }

        /// <summary>
        /// Moves a value saved with PlayerPrefs.SetString, or with Prefs.SetLong, SetDouble, SetEnum, SetDateTime or
        /// SetObject, into SecurePrefs and deletes the plain value. Read it afterwards with the matching SecurePrefs Get.
        /// <para>
        /// Call the Migrate methods at startup in the update that switches to SecurePrefs. An existing SecurePrefs value
        /// is never overwritten. Plain values are trusted as they are, so remove the Migrate calls in a later update:
        /// until then, a player can still plant a plain value for a key that has no SecurePrefs value yet.
        /// </para>
        /// </summary>
        /// <returns>True if a plain value was moved.</returns>
        public static bool MigrateString(string key)
        {
            return Migrate(key, () => PlayerPrefs.GetString(key));
        }
        #endregion


        #region Internal Methods
        /// <summary>
        /// The PlayerPrefs key a value is stored under: a hash of its name, so the names aren't readable.
        /// </summary>
        internal static string GetStorageKey(string key)
        {
            EnsureConfigured();

            byte[] hash = Hmac(keyNameKey, Encoding.UTF8.GetBytes(key ?? string.Empty));
            StringBuilder builder = new StringBuilder(StorageKeyPrefix.Length + 32);
            builder.Append(StorageKeyPrefix);
            for (int i = 0; i < 16; i++)
                builder.Append(hash[i].ToString("x2"));

            return builder.ToString();
        }

        /// <summary>
        /// Forgets the configuration, for tests.
        /// </summary>
        internal static void ResetConfiguration()
        {
            encryptionKey = null;
            authenticationKey = null;
            keyNameKey = null;
            isDeviceBound = false;
        }

        internal static string Encrypt(string key, string text)
        {
            EnsureConfigured();

            byte[] iv = new byte[BlockSize];
            RandomBytes.GetBytes(iv);

            byte[] plain = Encoding.UTF8.GetBytes(text);
            byte[] cipher;
            using (Aes aes = CreateAes())
            using (ICryptoTransform encryptor = aes.CreateEncryptor(encryptionKey, iv))
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            // Format: version | IV | ciphertext | HMAC(key name, version | IV | ciphertext)
            byte[] output = new byte[HeaderSize + cipher.Length + MacSize];
            output[0] = FormatVersion;
            Buffer.BlockCopy(iv, 0, output, 1, BlockSize);
            Buffer.BlockCopy(cipher, 0, output, HeaderSize, cipher.Length);

            byte[] mac = ComputeMac(key, output, HeaderSize + cipher.Length);
            Buffer.BlockCopy(mac, 0, output, HeaderSize + cipher.Length, MacSize);

            return Convert.ToBase64String(output);
        }

        internal static bool TryDecrypt(string key, string stored, out string text)
        {
            EnsureConfigured();
            text = null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(stored ?? string.Empty);
            }
            catch (FormatException)
            {
                return false;
            }

            int cipherLength = bytes.Length - HeaderSize - MacSize;
            if (cipherLength < BlockSize || cipherLength % BlockSize != 0 || bytes[0] != FormatVersion)
                return false;

            // Check the signature before decrypting anything.
            byte[] expectedMac = ComputeMac(key, bytes, HeaderSize + cipherLength);
            if (!FixedTimeEquals(expectedMac, bytes, HeaderSize + cipherLength))
                return false;

            byte[] iv = new byte[BlockSize];
            Buffer.BlockCopy(bytes, 1, iv, 0, BlockSize);

            try
            {
                using (Aes aes = CreateAes())
                using (ICryptoTransform decryptor = aes.CreateDecryptor(encryptionKey, iv))
                {
                    byte[] plain = decryptor.TransformFinalBlock(bytes, HeaderSize, cipherLength);
                    text = Encoding.UTF8.GetString(plain);
                    return true;
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
        #endregion


        #region Private Methods
        private static void SetText(string key, string text)
        {
            PlayerPrefs.SetString(GetStorageKey(key), Encrypt(key, text));
        }

        private static bool TryGetText(string key, out string text)
        {
            string storageKey = GetStorageKey(key);
            if (!PlayerPrefs.HasKey(storageKey))
            {
                text = null;
                return false;
            }

            if (TryDecrypt(key, PlayerPrefs.GetString(storageKey), out text))
                return true;

            Debug.LogWarning($"[{LogPrefix}] The value of '{key}' failed verification, so the default value was returned. It was edited, copied from another key or device, or saved with a different secret.");
            TamperDetected?.Invoke(key);
            return false;
        }

        private static bool Migrate(string key, Func<string> readPlainValue)
        {
            EnsureConfigured();

            if (!PlayerPrefs.HasKey(key))
                return false;

            bool moved = false;
            if (!HasKey(key))
            {
                SetText(key, readPlainValue());
                moved = true;
            }

            PlayerPrefs.DeleteKey(key);
            return moved;
        }

        private static void EnsureConfigured()
        {
            if (!IsConfigured)
                throw new InvalidOperationException($"[{LogPrefix}] Call SecurePrefs.Configure(secret) once at startup, before reading or writing values.");
        }

        private static Aes CreateAes()
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        /// <summary>
        /// HMAC of the key name (with its length, so names and data can't be shifted between each other) and the first
        /// <paramref name="count"/> bytes of <paramref name="data"/>.
        /// </summary>
        private static byte[] ComputeMac(string key, byte[] data, int count)
        {
            byte[] name = Encoding.UTF8.GetBytes(key ?? string.Empty);
            byte[] nameLength =
            {
                (byte)(name.Length >> 24), (byte)(name.Length >> 16), (byte)(name.Length >> 8), (byte)name.Length
            };

            using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
            {
                hmac.TransformBlock(nameLength, 0, nameLength.Length, null, 0);
                hmac.TransformBlock(name, 0, name.Length, null, 0);
                hmac.TransformFinalBlock(data, 0, count);
                return hmac.Hash;
            }
        }

        private static byte[] Hmac(byte[] key, byte[] data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
                return hmac.ComputeHash(data);
        }

        /// <summary>
        /// Compares <paramref name="expected"/> with the bytes of <paramref name="actual"/> from <paramref name="offset"/>,
        /// taking the same time whether they match or not.
        /// </summary>
        private static bool FixedTimeEquals(byte[] expected, byte[] actual, int offset)
        {
            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
                difference |= expected[i] ^ actual[offset + i];

            return difference == 0;
        }

        /// <summary>
        /// Clears the event when Play Mode starts, so it works with Domain Reload disabled. The configuration is kept:
        /// it doesn't refer to any scene object.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            TamperDetected = null;
        }
        #endregion
    }
}
