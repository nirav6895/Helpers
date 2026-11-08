#region Using
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
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
        public void OtherUnderlyingTypes_AndNegativeValues_Work()
        {
            EnumArray<SmallEnum, int> small = new EnumArray<SmallEnum, int>(e => (int)e + 1);
            Assert.AreEqual(3, small[SmallEnum.C]);
            Assert.IsFalse(small.TryGetValue((SmallEnum)200, out _));

            EnumArray<LongEnum, int> big = new EnumArray<LongEnum, int>(e => (int)e + 1);
            Assert.AreEqual(2, big[LongEnum.B]);
            Assert.IsFalse(big.TryGetValue((LongEnum)(-1), out _));

            EnumArray<SignedEnum, string> signed = new EnumArray<SignedEnum, string>();
            signed[SignedEnum.Minus] = "minus";
            signed[SignedEnum.One] = "one";
            Assert.AreEqual("minus", signed[SignedEnum.Minus]);
            Assert.AreEqual("one", signed[SignedEnum.One]);
            Assert.IsNull(signed[SignedEnum.Zero]);
            Assert.IsFalse(signed.TryGetValue((SignedEnum)5, out _));
        }

        [Test]
        public void Indexer_DoesNotAllocate()
        {
            EnumArray<Weapon, float> sequential = new EnumArray<Weapon, float>(1f);
            EnumArray<Rarity, float> nonSequential = new EnumArray<Rarity, float>(1f);
            float sum = sequential[Weapon.Bow] + nonSequential[Rarity.Rare];

            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    sequential[(Weapon)(i % 3)] = i;
                    sum += sequential[(Weapon)(i % 3)];
                    nonSequential[Rarity.Epic] = i;
                    sum += nonSequential[Rarity.Common];
                }
            }, Is.Not.AllocatingGCMemory());

            Assert.Greater(sum, 0f);
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
