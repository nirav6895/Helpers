#region Using
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
#endregion

namespace NG.Utility.Tests
{
    public class SerializableDictionaryTests
    {
        [Test]
        public void WorksLikeADictionary()
        {
            SerializableDictionary<string, int> dictionary = new SerializableDictionary<string, int> { { "a", 1 } };
            dictionary["b"] = 2;

            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual(2, dictionary["b"]);
            Assert.IsTrue(dictionary.TryGetValue("a", out int a));
            Assert.AreEqual(1, a);
            Assert.IsTrue(dictionary.Remove("a"));
            Assert.IsFalse(dictionary.ContainsKey("a"));
            Assert.Throws<KeyNotFoundException>(() => _ = dictionary["missing"]);

            IReadOnlyDictionary<string, int> readOnly = dictionary;
            CollectionAssert.AreEqual(new[] { "b" }, readOnly.Keys);
        }

        [Test]
        public void Serialization_RoundTrips()
        {
            DictionaryHolder holder = new DictionaryHolder();
            holder.prices["sword"] = 100;
            holder.prices["bow"] = 80;

            DictionaryHolder copy = JsonUtility.FromJson<DictionaryHolder>(JsonUtility.ToJson(holder));

            Assert.AreEqual(2, copy.prices.Count);
            Assert.AreEqual(100, copy.prices["sword"]);
            Assert.AreEqual(80, copy.prices["bow"]);
        }

        [Test]
        public void Deserialization_KeepsFirstOfDuplicateKeys_AndKeepsTheRowsForEditing()
        {
            string json = "{\"prices\":{\"entries\":[{\"key\":\"sword\",\"value\":1},{\"key\":\"sword\",\"value\":2},{\"key\":\"bow\",\"value\":3}]}}";

            DictionaryHolder holder = JsonUtility.FromJson<DictionaryHolder>(json);

            Assert.AreEqual(2, holder.prices.Count);
            Assert.AreEqual(1, holder.prices["sword"]);

            // Untouched by code, so the duplicate row survives serialization (the user may be fixing it in the Inspector).
            StringAssert.Contains("\"value\":2", JsonUtility.ToJson(holder));
        }

        [Test]
        public void Serialization_AfterCodeChanges_RewritesRowsFromTheDictionary()
        {
            string json = "{\"prices\":{\"entries\":[{\"key\":\"sword\",\"value\":1},{\"key\":\"sword\",\"value\":2}]}}";
            DictionaryHolder holder = JsonUtility.FromJson<DictionaryHolder>(json);

            holder.prices["bow"] = 3;
            DictionaryHolder copy = JsonUtility.FromJson<DictionaryHolder>(JsonUtility.ToJson(holder));

            Assert.AreEqual(2, copy.prices.Count);
            Assert.AreEqual(1, copy.prices["sword"]);
            Assert.AreEqual(3, copy.prices["bow"]);
            StringAssert.DoesNotContain("\"value\":2", JsonUtility.ToJson(copy));
        }

        [Test]
        public void SerializedProperty_HasEntriesWithKeyAndValue_ForTheDrawer()
        {
            TestAsset asset = ScriptableObject.CreateInstance<TestAsset>();
            try
            {
                asset.prices["sword"] = 100;
                SerializedObject serializedObject = new SerializedObject(asset);
                SerializedProperty entries = serializedObject.FindProperty(nameof(TestAsset.prices)).FindPropertyRelative("entries");

                Assert.IsNotNull(entries, "SerializableDictionaryDrawer reads the field 'entries'.");
                Assert.AreEqual(1, entries.arraySize);

                SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                Assert.AreEqual("sword", entry.FindPropertyRelative("key").stringValue);
                Assert.AreEqual(100, entry.FindPropertyRelative("value").intValue);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
