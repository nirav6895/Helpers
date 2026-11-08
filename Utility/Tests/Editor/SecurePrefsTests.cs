#region Using
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.Utility.Tests
{
    public class SecurePrefsTests
    {
        private const string Secret = "NG.Utility.Tests secret";
        private const string Key = "NG.Utility.Tests.SecurePrefs";
        private const string OtherKey = "NG.Utility.Tests.SecurePrefs.Other";
        private const string TamperWarning = @"^\[SecurePrefs\] The value of '.*' failed verification";

        private readonly List<string> tampered = new List<string>();

        [SetUp]
        public void SetUp()
        {
            SecurePrefs.Configure(Secret);
            tampered.Clear();
            SecurePrefs.TamperDetected += tampered.Add;
        }

        [TearDown]
        public void TearDown()
        {
            SecurePrefs.TamperDetected -= tampered.Add;
            SecurePrefs.Configure(Secret);
            SecurePrefs.DeleteKey(Key);
            SecurePrefs.DeleteKey(OtherKey);
            SecurePrefs.Configure(Secret, bindToDevice: true);
            SecurePrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(OtherKey);
        }

        [Test]
        public void AllTypes_RoundTrip()
        {
            SecurePrefs.SetBool(Key, true);
            Assert.IsTrue(SecurePrefs.GetBool(Key));

            SecurePrefs.SetInt(Key, -42);
            Assert.AreEqual(-42, SecurePrefs.GetInt(Key));

            SecurePrefs.SetFloat(Key, 0.1f);
            Assert.AreEqual(0.1f, SecurePrefs.GetFloat(Key));

            SecurePrefs.SetString(Key, "Héllo, wörld ✓");
            Assert.AreEqual("Héllo, wörld ✓", SecurePrefs.GetString(Key));

            SecurePrefs.SetString(Key, "");
            Assert.AreEqual("", SecurePrefs.GetString(Key, "default"));

            SecurePrefs.SetLong(Key, 9_000_000_000_000L);
            Assert.AreEqual(9_000_000_000_000L, SecurePrefs.GetLong(Key));

            SecurePrefs.SetDouble(Key, 0.1 + 0.2);
            Assert.AreEqual(0.1 + 0.2, SecurePrefs.GetDouble(Key));

            SecurePrefs.SetEnum(Key, Weapon.Staff);
            Assert.AreEqual(Weapon.Staff, SecurePrefs.GetEnum<Weapon>(Key));

            DateTime time = new DateTime(2026, 11, 8, 10, 30, 15, DateTimeKind.Utc);
            SecurePrefs.SetDateTime(Key, time);
            Assert.AreEqual(time, SecurePrefs.GetDateTime(Key));

            SecurePrefs.SetObject(Key, new PrefsData { name = "Nirav", level = 3 });
            PrefsData data = SecurePrefs.GetObject<PrefsData>(Key);
            Assert.AreEqual("Nirav", data.name);
            Assert.AreEqual(3, data.level);

            Assert.IsEmpty(tampered);
        }

        [Test]
        public void MissingKey_ReturnsTheDefault()
        {
            Assert.IsFalse(SecurePrefs.HasKey(Key));
            Assert.AreEqual(7, SecurePrefs.GetInt(Key, 7));
            Assert.AreEqual("default", SecurePrefs.GetString(Key, "default"));
            Assert.IsEmpty(tampered);
        }

        [Test]
        public void StoredValue_IsUnreadable_AndTheKeyNameIsHashed()
        {
            SecurePrefs.SetString(Key, "my secret value");
            string storageKey = SecurePrefs.GetStorageKey(Key);

            Assert.IsTrue(SecurePrefs.HasKey(Key));
            Assert.IsFalse(PlayerPrefs.HasKey(Key), "The plain key name isn't used.");
            StringAssert.StartsWith("ngsp.", storageKey);
            StringAssert.DoesNotContain(Key, storageKey);
            StringAssert.DoesNotContain("my secret value", PlayerPrefs.GetString(storageKey));
        }

        [Test]
        public void SameValue_EncryptsDifferentlyEachTime()
        {
            Assert.AreNotEqual(SecurePrefs.Encrypt(Key, "1500"), SecurePrefs.Encrypt(Key, "1500"));
        }

        [Test]
        public void EditedValue_IsRejected_AndRaisesTamperDetected()
        {
            SecurePrefs.SetLong(Key, 1500);
            string storageKey = SecurePrefs.GetStorageKey(Key);
            char[] stored = PlayerPrefs.GetString(storageKey).ToCharArray();
            stored[30] = stored[30] == 'A' ? 'B' : 'A';
            PlayerPrefs.SetString(storageKey, new string(stored));

            LogAssert.Expect(LogType.Warning, new Regex(TamperWarning));
            Assert.AreEqual(-1, SecurePrefs.GetLong(Key, -1));
            CollectionAssert.AreEqual(new[] { Key }, tampered);
        }

        [Test]
        public void ValueCopiedFromAnotherKey_IsRejected()
        {
            SecurePrefs.SetLong(Key, 5);
            SecurePrefs.SetLong(OtherKey, 999999);
            PlayerPrefs.SetString(SecurePrefs.GetStorageKey(Key), PlayerPrefs.GetString(SecurePrefs.GetStorageKey(OtherKey)));

            LogAssert.Expect(LogType.Warning, new Regex(TamperWarning));
            Assert.AreEqual(0, SecurePrefs.GetLong(Key));
        }

        [Test]
        public void PlainTextWrittenByAPlayer_IsRejected()
        {
            SecurePrefs.SetLong(Key, 5);
            PlayerPrefs.SetString(SecurePrefs.GetStorageKey(Key), "999999");

            LogAssert.Expect(LogType.Warning, new Regex(TamperWarning));
            Assert.AreEqual(0, SecurePrefs.GetLong(Key));
        }

        [Test]
        public void DifferentSecret_CantReadTheValues()
        {
            SecurePrefs.SetInt(Key, 5);
            string stored = PlayerPrefs.GetString(SecurePrefs.GetStorageKey(Key));

            SecurePrefs.Configure("another secret");
            Assert.IsFalse(SecurePrefs.TryDecrypt(Key, stored, out _));
        }

        [Test]
        public void DeviceBinding_ChangesTheKeys()
        {
            SecurePrefs.SetInt(Key, 5);
            string unbound = PlayerPrefs.GetString(SecurePrefs.GetStorageKey(Key));

            SecurePrefs.Configure(Secret, bindToDevice: true);
            Assume.That(SecurePrefs.IsDeviceBound, "Needs a device identifier.");

            Assert.IsFalse(SecurePrefs.TryDecrypt(Key, unbound, out _), "Values saved without binding aren't readable with it.");
            SecurePrefs.SetInt(Key, 6);
            Assert.AreEqual(6, SecurePrefs.GetInt(Key));
        }

        [Test]
        public void NotConfigured_Throws()
        {
            SecurePrefs.ResetConfiguration();

            Assert.IsFalse(SecurePrefs.IsConfigured);
            Assert.Throws<InvalidOperationException>(() => SecurePrefs.GetInt(Key));
            Assert.Throws<InvalidOperationException>(() => SecurePrefs.SetInt(Key, 1));
            Assert.Throws<ArgumentException>(() => SecurePrefs.Configure(""));
        }

        [Test]
        public void Migrate_MovesPlainValues_AndDeletesThem()
        {
            PlayerPrefs.SetInt(Key, 42);
            Assert.IsTrue(SecurePrefs.MigrateInt(Key));
            Assert.IsFalse(PlayerPrefs.HasKey(Key));
            Assert.AreEqual(42, SecurePrefs.GetInt(Key));

            Prefs.SetBool(OtherKey, true);
            Assert.IsTrue(SecurePrefs.MigrateInt(OtherKey));
            Assert.IsTrue(SecurePrefs.GetBool(OtherKey));
        }

        [Test]
        public void Migrate_ValuesSavedWithPrefs()
        {
            Prefs.SetLong(Key, 9_000_000_000_000L);
            Assert.IsTrue(SecurePrefs.MigrateString(Key));
            Assert.AreEqual(9_000_000_000_000L, SecurePrefs.GetLong(Key));

            Prefs.SetDateTime(Key, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            Assert.IsFalse(SecurePrefs.MigrateString(Key), "An existing SecurePrefs value isn't overwritten.");
            Assert.AreEqual(9_000_000_000_000L, SecurePrefs.GetLong(Key));
            Assert.IsFalse(PlayerPrefs.HasKey(Key), "The plain value is still deleted.");

            PlayerPrefs.SetFloat(OtherKey, 2.5f);
            Assert.IsTrue(SecurePrefs.MigrateFloat(OtherKey));
            Assert.AreEqual(2.5f, SecurePrefs.GetFloat(OtherKey));
        }

        [Test]
        public void Migrate_WithoutAPlainValue_DoesNothing()
        {
            Assert.IsFalse(SecurePrefs.MigrateString(Key));
            Assert.IsFalse(SecurePrefs.HasKey(Key));
        }
    }
}
