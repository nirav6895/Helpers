#region Using
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class ComponentPoolTests
    {
        private TestPool pool;
        private TestPooledItem template;
        private readonly List<GameObject> created = new();

        [SetUp]
        public void SetUp()
        {
            template = new GameObject("Template").AddComponent<TestPooledItem>();
            template.gameObject.SetActive(false);
            pool = new GameObject("Pool").AddComponent<TestPool>();
            pool.Prefab = template;
            created.Add(template.gameObject);
            created.Add(pool.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in created)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }

            created.Clear();
        }

        [Test]
        public void Get_CreatesAnActiveInstance_UnderTheParent()
        {
            Transform parent = Track(new GameObject("Parent")).transform;

            TestPooledItem item = Track(pool.Get(parent));

            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.AreSame(parent, item.transform.parent);
            Assert.AreEqual(1, item.Spawns);
        }

        [Test]
        public void Release_DeactivatesAndKeepsTheItem_ThenGetReusesIt()
        {
            TestPooledItem item = Track(pool.Get());

            pool.Release(item);

            Assert.IsFalse(item.gameObject.activeSelf);
            Assert.AreSame(pool.transform, item.transform.parent);
            Assert.AreEqual(1, item.Despawns);
            Assert.AreEqual(1, pool.CountInactive);

            Assert.AreSame(item, pool.Get());
            Assert.AreEqual(2, item.Spawns);
            Assert.AreEqual(0, pool.CountInactive);
        }

        [Test]
        public void DoubleRelease_IsIgnored_SoOneItemIsNeverHandedOutTwice()
        {
            TestPooledItem item = Track(pool.Get());
            pool.Release(item);

            LogAssert.Expect(LogType.Warning, new Regex("released twice"));
            pool.Release(item);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreNotSame(Track(pool.Get()), Track(pool.Get()));
        }

        [UnityTest]
        public IEnumerator ItemDestroyedWhileInThePool_IsSkipped()
        {
            TestPooledItem item = pool.Get();
            pool.Release(item);
            Object.Destroy(item.gameObject);
            yield return null;

            TestPooledItem next = Track(pool.Get());

            Assert.IsTrue(next != null);
            Assert.AreNotSame(item, next);
        }

        [UnityTest]
        public IEnumerator MaxSize_DestroysExtraReleasedItems()
        {
            pool.MaxSize = 1;
            TestPooledItem first = Track(pool.Get());
            TestPooledItem second = Track(pool.Get());

            pool.Release(first);
            pool.Release(second);
            yield return null;

            Assert.AreEqual(1, pool.CountInactive);
            Assert.IsTrue(second == null);
            Assert.AreEqual(0, pool.ActiveItems.Count, "an item destroyed by MaxSize is not active either");
        }

        [Test]
        public void Prewarm_CreatesInactiveInstances_UpToTheCount()
        {
            pool.Prewarm(3);
            Assert.AreEqual(3, pool.CountInactive);

            pool.Prewarm(2);
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void Get_WithPositionAndRotation_PlacesTheItem()
        {
            var position = new Vector3(1, 2, 3);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);

            TestPooledItem first = Track(pool.Get(position, rotation));
            Assert.AreEqual(position, first.transform.position);

            pool.Release(first);
            TestPooledItem reused = pool.Get(position * 2, rotation);

            Assert.AreSame(first, reused);
            Assert.AreEqual(position * 2, reused.transform.position);
            Assert.That(Quaternion.Angle(rotation, reused.transform.rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void ReleaseList_ReleasesEveryItem_AndClearsTheList()
        {
            var items = new List<TestPooledItem> { Track(pool.Get()), Track(pool.Get()) };

            pool.Release(items);

            Assert.AreEqual(0, items.Count);
            Assert.AreEqual(2, pool.CountInactive);
        }

        [Test]
        public void Get_WithoutAPrefab_Throws()
        {
            pool.Prefab = null;
            Assert.Throws<InvalidOperationException>(() => pool.Get());
        }

        [UnityTest]
        public IEnumerator Release_AfterThePoolWasDestroyed_DestroysTheItem()
        {
            TestPooledItem item = pool.Get();
            Object.Destroy(pool.gameObject);
            yield return null;

            pool.Release(item);
            yield return null;

            Assert.IsTrue(item == null);
            Assert.AreEqual(0, pool.ActiveItems.Count);
        }

        [Test]
        public void ActiveItems_HasItemsHandedOut_UntilTheyAreReleased()
        {
            pool.Prewarm(1);
            TestPooledItem first = Track(pool.Get());
            TestPooledItem second = Track(pool.Get());

            pool.Release(first);

            CollectionAssert.AreEquivalent(new[] { second }, pool.ActiveItems);
            Assert.IsNotInstanceOf<List<TestPooledItem>>(pool.ActiveItems);
        }

        [Test]
        public void ReleasingAnItemDestroyedWhileInUse_RemovesItFromActiveItems()
        {
            TestPooledItem item = pool.Get();
            Object.DestroyImmediate(item.gameObject);

            pool.Release(item);

            Assert.AreEqual(0, pool.ActiveItems.Count);
            Assert.AreEqual(0, pool.CountInactive);
        }

        [Test]
        public void ReleaseAll_ReleasesEveryActiveItem()
        {
            var items = new List<TestPooledItem> { Track(pool.Get()), Track(pool.Get()), Track(pool.Get()) };

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.ActiveItems.Count);
            Assert.AreEqual(3, pool.CountInactive);
            foreach (TestPooledItem item in items)
                Assert.IsFalse(item.gameObject.activeSelf);
        }

        [Test]
        public void ReleasingAgainFromOnDisable_KeepsTheItemOnce()
        {
            TestPooledItem item = Track(pool.Get());
            item.ReleaseOnDisable = pool;

            pool.Release(item);
            item.ReleaseOnDisable = null;

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreEqual(1, item.Despawns, "release callbacks ran once");
            Assert.AreSame(pool.transform, item.transform.parent);
            Assert.AreNotSame(Track(pool.Get()), Track(pool.Get()));
        }

        private T Track<T>(T component) where T : Component
        {
            created.Add(component.gameObject);
            return component;
        }

        private GameObject Track(GameObject gameObject)
        {
            created.Add(gameObject);
            return gameObject;
        }
    }
}
