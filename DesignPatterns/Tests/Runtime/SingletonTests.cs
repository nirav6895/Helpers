#region Using
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#endregion

namespace NG.DesignPatterns.Tests
{
    public class SingletonTests
    {
        private readonly List<GameObject> created = new();

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
        public void FirstInstance_BecomesInstance_AndRunsOnAwake()
        {
            TestManager first = Create<TestManager>("First");

            Assert.AreSame(first, TestManager.Instance);
            Assert.AreEqual(1, first.AwakeCount);
        }

        [UnityTest]
        public IEnumerator Duplicate_IsDestroyed_AndSkipsOnAwake()
        {
            TestManager first = Create<TestManager>("First");
            TestManager duplicate = Create<TestManager>("Duplicate");
            GameObject duplicateObject = duplicate.gameObject;

            Assert.AreSame(first, TestManager.Instance);
            Assert.AreEqual(0, duplicate.AwakeCount, "setup code must not run on a duplicate");

            yield return null;   // Destroy happens at the end of the frame

            Assert.IsTrue(duplicateObject == null);
            Assert.AreSame(first, TestManager.Instance);
        }

        [UnityTest]
        public IEnumerator DestroyedInstance_ClearsInstance_AndANewOneTakesOver()
        {
            TestManager first = Create<TestManager>("First");
            Object.Destroy(first.gameObject);
            yield return null;

            Assert.IsTrue(ReferenceEquals(TestManager.Instance, null), "Instance must be really null, not a destroyed object");

            TestManager second = Create<TestManager>("Second");
            Assert.AreSame(second, TestManager.Instance);
        }

        [UnityTest]
        public IEnumerator PersistAcrossScenes_MovesTheObjectToDontDestroyOnLoad()
        {
            var parent = new GameObject("Parent");
            created.Add(parent);
            var child = new GameObject("Persistent");
            child.transform.SetParent(parent.transform);
            created.Add(child);

            TestPersistentManager manager = child.AddComponent<TestPersistentManager>();
            yield return null;

            Assert.AreSame(manager, TestPersistentManager.Instance);
            Assert.IsNull(manager.transform.parent);
            Assert.AreEqual("DontDestroyOnLoad", manager.gameObject.scene.name);
        }

        [Test]
        public void WrongTypeArgument_FailsLoudly()
        {
            LogAssert.Expect(LogType.Exception, new Regex("InvalidCastException"));
            Create<MismatchedSingleton>("Mismatched");

            Assert.IsTrue(ReferenceEquals(TestManager.Instance, null));
        }

        private T Create<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            created.Add(gameObject);
            return gameObject.AddComponent<T>();
        }
    }
}
