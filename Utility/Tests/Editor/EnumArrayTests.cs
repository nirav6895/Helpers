#region Using
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#endregion

namespace NG.Utility.Tests
{
    public class EnumArrayTests
    {
        [Test]
        public void HasOneValuePerMember()
        {
            EnumArray<Weapon, int> array = new EnumArray<Weapon, int>();

            Assert.AreEqual(3, array.Count);
            CollectionAssert.AreEqual(new[] { Weapon.Sword, Weapon.Bow, Weapon.Staff }, EnumArray<Weapon, int>.Keys);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, array.Values);
        }

        [Test]
        public void Indexer_GetsAndSets()
        {
            EnumArray<Weapon, int> array = new EnumArray<Weapon, int>();
            array[Weapon.Bow] = 7;

            Assert.AreEqual(7, array[Weapon.Bow]);
            Assert.AreEqual(0, array[Weapon.Sword]);
        }

        [Test]
        public void Indexer_UnknownKey_Throws()
        {
            EnumArray<Weapon, int> array = new EnumArray<Weapon, int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = array[(Weapon)99]);
            Assert.IsFalse(array.TryGetValue((Weapon)99, out _));
        }

        [Test]
        public void Constructors_FillValues()
        {
            CollectionAssert.AreEqual(new[] { 5, 5, 5 }, new EnumArray<Weapon, int>(5).Values);
            CollectionAssert.AreEqual(new[] { 0, 10, 20 }, new EnumArray<Weapon, int>(weapon => (int)weapon * 10).Values);
        }

        [Test]
        public void NonSequentialEnum_UsesValueOrder_AndAliasesCountOnce()
        {
            EnumArray<Rarity, string> array = new EnumArray<Rarity, string>();

            CollectionAssert.AreEqual(new[] { Rarity.Epic, Rarity.Common, Rarity.Rare }, EnumArray<Rarity, string>.Keys);

            array[Rarity.Legendary] = "gold";
            Assert.AreEqual("gold", array[Rarity.Rare], "Legendary is an alias of Rare.");
        }

        [Test]
        public void Enumerates_PairsInValueOrder()
        {
            EnumArray<Weapon, int> array = new EnumArray<Weapon, int>(weapon => (int)weapon + 1);
            List<KeyValuePair<Weapon, int>> pairs = new List<KeyValuePair<Weapon, int>>();

            foreach (KeyValuePair<Weapon, int> pair in array)
                pairs.Add(pair);

            CollectionAssert.AreEqual(new[]
            {
                new KeyValuePair<Weapon, int>(Weapon.Sword, 1),
                new KeyValuePair<Weapon, int>(Weapon.Bow, 2),
                new KeyValuePair<Weapon, int>(Weapon.Staff, 3)
            }, pairs);
        }

        [Test]
        public void Serialization_RoundTrips()
        {
            EnumArrayHolder holder = new EnumArrayHolder();
            holder.damage[Weapon.Staff] = 42;

            EnumArrayHolder copy = JsonUtility.FromJson<EnumArrayHolder>(JsonUtility.ToJson(holder));

            Assert.AreEqual(42, copy.damage[Weapon.Staff]);
        }

        [Test]
        public void Deserialization_MapsValuesByKey_WhenMembersWereReorderedOrRemoved()
        {
            // Saved data where members were in a different order, Sword was missing and 99 no longer exists.
            string json = "{\"damage\":{\"keys\":[2,99,1],\"values\":[30,50,20]}}";

            EnumArrayHolder holder = JsonUtility.FromJson<EnumArrayHolder>(json);

            Assert.AreEqual(3, holder.damage.Count);
            Assert.AreEqual(0, holder.damage[Weapon.Sword]);
            Assert.AreEqual(20, holder.damage[Weapon.Bow]);
            Assert.AreEqual(30, holder.damage[Weapon.Staff]);
        }

        [Test]
        public void SerializedProperty_HasOneElementPerMember_ForTheDrawer()
        {
            TestAsset asset = ScriptableObject.CreateInstance<TestAsset>();
            try
            {
                asset.damage[Weapon.Bow] = 3;
                SerializedObject serializedObject = new SerializedObject(asset);
                SerializedProperty damage = serializedObject.FindProperty(nameof(TestAsset.damage));
                SerializedProperty keys = damage.FindPropertyRelative("keys");
                SerializedProperty values = damage.FindPropertyRelative("values");

                Assert.IsNotNull(keys, "EnumArrayDrawer reads the field 'keys'.");
                Assert.IsNotNull(values, "EnumArrayDrawer reads the field 'values'.");
                Assert.AreEqual(3, keys.arraySize);
                Assert.AreEqual(3, values.arraySize);
                Assert.AreEqual("Bow", keys.GetArrayElementAtIndex(1).enumDisplayNames[keys.GetArrayElementAtIndex(1).enumValueIndex]);
                Assert.AreEqual(3, values.GetArrayElementAtIndex(1).intValue);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
