#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
#endregion

namespace NG.Utility.Tests
{
    public class ExtensionsTests
    {
        [Test]
        public void Vector_With_ReplacesOnlyGivenComponents()
        {
            Assert.AreEqual(new Vector3(1f, 0f, 3f), new Vector3(1f, 2f, 3f).With(y: 0f));
            Assert.AreEqual(new Vector2(5f, 2f), new Vector2(1f, 2f).With(x: 5f));
        }

        [Test]
        public void Color_WithAlpha()
        {
            Assert.AreEqual(new Color(1f, 0.5f, 0f, 0.25f), new Color(1f, 0.5f, 0f, 1f).WithAlpha(0.25f));
            Assert.AreEqual(new Color32(10, 20, 30, 40), new Color32(10, 20, 30, 255).WithAlpha(40));
        }

        [Test]
        public void Shuffle_KeepsTheElements_AndIsRepeatableWithASeed()
        {
            List<int> a = Enumerable.Range(0, 50).ToList();
            List<int> b = Enumerable.Range(0, 50).ToList();

            a.Shuffle(new Random(7));
            b.Shuffle(new Random(7));

            CollectionAssert.AreEquivalent(Enumerable.Range(0, 50), a);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(Enumerable.Range(0, 50), a);
        }

        [Test]
        public void Shuffle_WorksOnArrays()
        {
            int[] array = { 1, 2, 3, 4 };
            array.Shuffle();

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, array);
        }

        [Test]
        public void GetRandom_ReturnsAnElement()
        {
            int[] array = { 4, 5, 6 };

            CollectionAssert.Contains(array, array.GetRandom());
            CollectionAssert.Contains(array, array.GetRandom(new Random(1)));
        }

        [Test]
        public void GetRandom_EmptyList_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new List<int>().GetRandom());
        }

        [Test]
        public void GetOrAddComponent_ReturnsTheExistingOne()
        {
            GameObject gameObject = new GameObject("Test");
            try
            {
                CanvasGroup added = gameObject.GetOrAddComponent<CanvasGroup>();

                Assert.IsNotNull(added);
                Assert.AreSame(added, gameObject.GetOrAddComponent<CanvasGroup>());
                Assert.AreSame(added, gameObject.transform.GetOrAddComponent<CanvasGroup>());
                Assert.AreEqual(1, gameObject.GetComponents<CanvasGroup>().Length);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CanvasGroup_SetVisible()
        {
            GameObject gameObject = new GameObject("Test");
            try
            {
                CanvasGroup group = gameObject.AddComponent<CanvasGroup>();

                group.SetVisible(false);
                Assert.AreEqual(0f, group.alpha);
                Assert.IsFalse(group.interactable);
                Assert.IsFalse(group.blocksRaycasts);

                group.SetVisible(true);
                Assert.AreEqual(1f, group.alpha);
                Assert.IsTrue(group.interactable);
                Assert.IsTrue(group.blocksRaycasts);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DestroyChildren_InEditMode_DestroysImmediately()
        {
            GameObject parent = new GameObject("Parent");
            try
            {
                new GameObject("A").transform.SetParent(parent.transform);
                new GameObject("B").transform.SetParent(parent.transform);

                parent.transform.DestroyChildren();

                Assert.AreEqual(0, parent.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
