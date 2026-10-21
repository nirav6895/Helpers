#region Using
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class ObjectPoolTests
    {
        #region Test Types
        private sealed class Bullet : IPoolable
        {
            public readonly List<string> Trace = new();
            public void OnSpawn() => Trace.Add("OnSpawn");
            public void OnDespawn() => Trace.Add("OnDespawn");
        }

        private sealed class AlwaysEqual
        {
            public override bool Equals(object other) => true;
            public override int GetHashCode() => 0;
        }
        #endregion


        #region Tests
        [Test]
        public void Get_CreatesWithTheFactory_AndReleasedItemsAreReused()
        {
            int created = 0;
            var pool = new ObjectPool<StringBuilder>(() => { created++; return new StringBuilder(); }, actionOnRelease: builder => builder.Clear());

            StringBuilder first = pool.Get();
            first.Append("used");
            pool.Release(first);
            StringBuilder second = pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(0, second.Length, "actionOnRelease reset it");
            Assert.AreEqual(1, created);
        }

        [Test]
        public void Callbacks_RunInOrder_AroundIPoolable()
        {
            Bullet bullet = null;
            var pool = new ObjectPool<Bullet>(
                () => bullet = new Bullet(),
                actionOnGet: item => item.Trace.Add("actionOnGet"),
                actionOnRelease: item => item.Trace.Add("actionOnRelease"));

            pool.Release(pool.Get());

            CollectionAssert.AreEqual(new[] { "actionOnGet", "OnSpawn", "OnDespawn", "actionOnRelease" }, bullet.Trace);
        }

        [Test]
        public void DoubleRelease_IsIgnored_SoOneItemIsNeverHandedOutTwice()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            Bullet bullet = pool.Get();
            pool.Release(bullet);

            LogAssert.Expect(LogType.Warning, new Regex("released twice"));
            pool.Release(bullet);

            Assert.AreEqual(1, pool.CountInactive);
            Assert.AreNotSame(pool.Get(), pool.Get());
        }

        [Test]
        public void MaxSize_SendsExtraReleasedItemsToActionOnDestroy()
        {
            var destroyed = new List<Bullet>();
            var pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnDestroy: destroyed.Add, maxSize: 1);
            Bullet first = pool.Get();
            Bullet second = pool.Get();

            pool.Release(first);
            pool.Release(second);

            Assert.AreEqual(1, pool.CountInactive);
            CollectionAssert.AreEqual(new[] { second }, destroyed);
        }

        [Test]
        public void Prewarm_CreatesUpToTheCount_ThroughActionOnRelease()
        {
            int released = 0;
            var pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnRelease: _ => released++);

            pool.Prewarm(3);
            pool.Prewarm(2);

            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(3, released);
        }

        [Test]
        public void Clear_PassesEveryInactiveItemToActionOnDestroy()
        {
            var destroyed = new List<Bullet>();
            var pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnDestroy: destroyed.Add);
            pool.Prewarm(2);

            pool.Clear();

            Assert.AreEqual(0, pool.CountInactive);
            Assert.AreEqual(2, destroyed.Count);
        }

        [Test]
        public void ReleaseList_ReleasesEveryItem_AndClearsTheList()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            var items = new List<Bullet> { pool.Get(), pool.Get() };

            pool.Release(items);

            Assert.AreEqual(0, items.Count);
            Assert.AreEqual(2, pool.CountInactive);
        }

        [Test]
        public void UnityObjectDestroyedWhileInThePool_IsSkipped()
        {
            var pool = new ObjectPool<Texture2D>(() => new Texture2D(1, 1), actionOnDestroy: Object.DestroyImmediate);
            Texture2D first = pool.Get();
            pool.Release(first);
            Object.DestroyImmediate(first);

            Texture2D next = pool.Get();

            Assert.IsTrue(next != null);
            Assert.AreNotSame(first, next);
            Object.DestroyImmediate(next);
        }

        [Test]
        public void InvalidFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Bullet>(null));
            Assert.Throws<InvalidOperationException>(() => new ObjectPool<Bullet>(() => null).Get());
        }

        [Test]
        public void ReleasingNull_IsIgnored()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            Assert.DoesNotThrow(() => pool.Release((Bullet)null));
            Assert.AreEqual(0, pool.CountInactive);
        }

        [Test]
        public void ActiveItems_HasItemsHandedOut_UntilTheyAreReleased()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet(), maxSize: 1);
            pool.Prewarm(1);
            Bullet first = pool.Get();
            Bullet second = pool.Get();
            Bullet third = pool.Get();

            pool.Release(first);
            CollectionAssert.AreEquivalent(new[] { second, third }, pool.ActiveItems);

            pool.Release(second);
            CollectionAssert.AreEquivalent(new[] { third }, pool.ActiveItems, "an item dropped by MaxSize is not active either");
        }

        [Test]
        public void ActiveItems_CantBeModified()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            pool.Get();

            Assert.IsNotInstanceOf<List<Bullet>>(pool.ActiveItems);
            Assert.Throws<NotSupportedException>(() => ((IList<Bullet>)pool.ActiveItems).Add(new Bullet()));
            Assert.Throws<NotSupportedException>(() => ((IList<Bullet>)pool.ActiveItems).Clear());
            Assert.AreEqual(1, pool.ActiveItems.Count);
        }

        [Test]
        public void ActiveItems_TracksItemsByReference_EvenIfEqualsIsOverridden()
        {
            var pool = new ObjectPool<AlwaysEqual>(() => new AlwaysEqual());
            AlwaysEqual first = pool.Get();
            AlwaysEqual second = pool.Get();

            // Removing by Equals would remove the first match, i.e. the wrong item
            pool.Release(second);

            Assert.AreEqual(1, pool.ActiveItems.Count);
            Assert.AreSame(first, pool.ActiveItems[0]);
        }

        [Test]
        public void ActiveItems_DropsAUnityObjectDestroyedWhileInUse_WhenItIsReleased()
        {
            var pool = new ObjectPool<Texture2D>(() => new Texture2D(1, 1));
            Texture2D texture = pool.Get();
            Object.DestroyImmediate(texture);

            pool.Release(texture);

            Assert.AreEqual(0, pool.ActiveItems.Count);
            Assert.AreEqual(0, pool.CountInactive);
        }

        [Test]
        public void ReleaseAll_ReleasesEveryActiveItem()
        {
            var pool = new ObjectPool<Bullet>(() => new Bullet());
            for (int i = 0; i < 6; i++)
                pool.Get();

            pool.ReleaseAll();

            Assert.AreEqual(0, pool.ActiveItems.Count);
            Assert.AreEqual(6, pool.CountInactive);
        }

        [Test]
        public void ReleaseAll_WhenACallbackReleasesAnotherItem_ReleasesEachOnce()
        {
            ObjectPool<Bullet> pool = null;
            Bullet first = null;
            Bullet second = null;
            pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnRelease: item =>
            {
                if (item == first)
                    pool.Release(second);
            });
            first = pool.Get();
            second = pool.Get();

            pool.ReleaseAll();

            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(0, pool.ActiveItems.Count);
            Assert.AreEqual(2, pool.CountInactive);
        }

        [Test]
        public void ReleaseAll_WhenACallbackGetsANewItem_KeepsItActive()
        {
            ObjectPool<Bullet> pool = null;
            Bullet extra = null;
            pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnRelease: _ => extra ??= pool.Get());
            Bullet first = pool.Get();

            pool.ReleaseAll();

            Assert.AreEqual(1, pool.ActiveItems.Count);
            Assert.AreSame(extra, pool.ActiveItems[0]);
            Assert.AreNotSame(first, extra);
        }

        [Test]
        public void ReleasingAgainFromAReleaseCallback_KeepsTheItemOnce()
        {
            ObjectPool<Bullet> pool = null;
            pool = new ObjectPool<Bullet>(() => new Bullet(), actionOnRelease: item => pool.Release(item));
            Bullet bullet = pool.Get();

            pool.Release(bullet);

            Assert.AreEqual(1, pool.CountInactive);
            CollectionAssert.AreEqual(new[] { "OnSpawn", "OnDespawn" }, bullet.Trace, "release callbacks ran once");
            Assert.AreNotSame(pool.Get(), pool.Get());
        }
        #endregion
    }
}
