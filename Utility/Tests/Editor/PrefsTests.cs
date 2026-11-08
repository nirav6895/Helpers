#region Using
using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.Utility.Tests
{
    public class PrefsTests
    {
        private const string Key = "NG.Utility.Tests.PrefsTests";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Key);
        }

        [Test]
        public void MissingKey_ReturnsTheDefault()
        {
            PlayerPrefs.DeleteKey(Key);

            Assert.IsTrue(Prefs.GetBool(Key, true));
            Assert.AreEqual(5L, Prefs.GetLong(Key, 5));
            Assert.AreEqual(1.5, Prefs.GetDouble(Key, 1.5));
            Assert.AreEqual(Weapon.Bow, Prefs.GetEnum(Key, Weapon.Bow));
            Assert.AreEqual(DateTime.MinValue, Prefs.GetDateTime(Key));
            Assert.IsNull(Prefs.GetObject<PrefsData>(Key));
        }

        [Test]
        public void Bool_RoundTrips()
        {
            Prefs.SetBool(Key, true);
            Assert.IsTrue(Prefs.GetBool(Key));

            Prefs.SetBool(Key, false);
            Assert.IsFalse(Prefs.GetBool(Key, true));
        }

        [Test]
        public void Long_RoundTrips_BeyondIntRange()
        {
            Prefs.SetLong(Key, 9_000_000_000_000L);
            Assert.AreEqual(9_000_000_000_000L, Prefs.GetLong(Key));
        }

        [Test]
        public void Double_RoundTrips_Exactly()
        {
            Prefs.SetDouble(Key, 0.1 + 0.2);
            Assert.AreEqual(0.1 + 0.2, Prefs.GetDouble(Key));
        }

        [Test]
        public void Enum_IsStoredByName()
        {
            Prefs.SetEnum(Key, Weapon.Staff);

            Assert.AreEqual("Staff", PlayerPrefs.GetString(Key));
            Assert.AreEqual(Weapon.Staff, Prefs.GetEnum<Weapon>(Key));
        }

        [Test]
        public void Enum_UnknownName_ReturnsTheDefault()
        {
            PlayerPrefs.SetString(Key, "Axe");
            Assert.AreEqual(Weapon.Bow, Prefs.GetEnum(Key, Weapon.Bow));
        }

        [Test]
        public void DateTime_RoundTrips_WithItsKind()
        {
            DateTime now = new DateTime(2026, 11, 8, 10, 30, 15, DateTimeKind.Utc);
            Prefs.SetDateTime(Key, now);

            DateTime read = Prefs.GetDateTime(Key);
            Assert.AreEqual(now, read);
            Assert.AreEqual(DateTimeKind.Utc, read.Kind);
        }

        [Test]
        public void Object_RoundTrips_AsJson()
        {
            Prefs.SetObject(Key, new PrefsData { name = "Nirav", level = 3 });

            PrefsData data = Prefs.GetObject<PrefsData>(Key);
            Assert.AreEqual("Nirav", data.name);
            Assert.AreEqual(3, data.level);
        }

        [Test]
        public void Object_InvalidJson_WarnsAndReturnsTheDefault()
        {
            PlayerPrefs.SetString(Key, "{ not json");
            PrefsData fallback = new PrefsData();

            LogAssert.Expect(LogType.Warning, new Regex(@"^\[Prefs\]"));
            Assert.AreSame(fallback, Prefs.GetObject(Key, fallback));
        }

        [Test]
        public void WrongType_ReturnsTheDefault()
        {
            PlayerPrefs.SetString(Key, "not a number");

            Assert.AreEqual(7L, Prefs.GetLong(Key, 7));
            Assert.AreEqual(2.5, Prefs.GetDouble(Key, 2.5));
        }
    }
}
