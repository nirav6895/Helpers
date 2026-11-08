#region Using
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
#endregion

namespace NG.Utility.Tests
{
    public class AutoRotateTests
    {
        [UnityTest]
        public IEnumerator Rotates_ByDegreesPerSecond()
        {
            GameObject gameObject = new GameObject("Rotator");
            try
            {
                AutoRotate rotate = gameObject.AddComponent<AutoRotate>();
                rotate.Axis = Vector3.forward;
                rotate.Speed = 30f;

                float expected = 0f;
                for (int i = 0; i < 10; i++)
                {
                    yield return null;
                    expected += Time.deltaTime * 30f;
                }

                Assert.AreEqual(expected % 360f, gameObject.transform.eulerAngles.z, 0.5f);
            }
            finally
            {
                Object.Destroy(gameObject);
            }
        }
    }

    public class SafeAreaTests
    {
        [UnityTest]
        public IEnumerator FitsTheRectTransformToTheSafeArea()
        {
            Assume.That(Screen.width > 0 && Screen.height > 0, "Needs a screen size.");

            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                GameObject panel = new GameObject("Safe Area", typeof(RectTransform));
                panel.transform.SetParent(root.transform, false);
                SafeArea safeArea = panel.AddComponent<SafeArea>();
                yield return null;

                RectTransform rect = (RectTransform)panel.transform;
                Rect area = Screen.safeArea;
                Assert.AreEqual(area.xMin / Screen.width, rect.anchorMin.x, 0.0001f);
                Assert.AreEqual(area.yMin / Screen.height, rect.anchorMin.y, 0.0001f);
                Assert.AreEqual(area.xMax / Screen.width, rect.anchorMax.x, 0.0001f);
                Assert.AreEqual(area.yMax / Screen.height, rect.anchorMax.y, 0.0001f);
                Assert.AreEqual(Vector2.zero, rect.offsetMin);
                Assert.AreEqual(Vector2.zero, rect.offsetMax);

                safeArea.Left = false;
                safeArea.Bottom = false;
                Assert.AreEqual(Vector2.zero, rect.anchorMin);
            }
            finally
            {
                Object.Destroy(root);
            }
        }
    }

    public class AnimatorExtensionsTests
    {
        [UnityTest]
        public IEnumerator NullAnimator_WarnsAndEnds()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[AnimatorExtensions\].*null or destroyed"));
            yield return TestUtility.RunWithTimeout(((Animator)null).PlayAndWait("Open"), 1f, "PlayAndWait");
        }

        [UnityTest]
        public IEnumerator MissingState_WarnsAndEnds()
        {
            // The old version waited forever for a state that doesn't exist.
            GameObject gameObject = new GameObject("Animator");
            try
            {
                Animator animator = gameObject.AddComponent<Animator>();

                LogAssert.Expect(LogType.Warning, new Regex(@"^\[AnimatorExtensions\].*has no state 'Open'"));
                yield return TestUtility.RunWithTimeout(animator.PlayAndWait("Open"), 1f, "PlayAndWait");
            }
            finally
            {
                Object.Destroy(gameObject);
            }
        }
    }

    public class PlayableDirectorExtensionsTests
    {
        private GameObject gameObject;
        private TestPlayableAsset asset;
        private PlayableDirector director;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Director");
            director = gameObject.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            asset = ScriptableObject.CreateInstance<TestPlayableAsset>();
            asset.length = 0.2;
            director.playableAsset = asset;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(gameObject);
            Object.Destroy(asset);
        }

        [UnityTest]
        public IEnumerator WrapModeNone_EndsWhenTheDirectorStops()
        {
            director.extrapolationMode = DirectorWrapMode.None;
            float start = Time.realtimeSinceStartup;

            yield return TestUtility.RunWithTimeout(director.PlayAndWait(), 3f, "PlayAndWait");

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - start, 0.15f);
            Assert.AreNotEqual(PlayState.Playing, director.state);
        }

        [UnityTest]
        public IEnumerator WrapModeHold_EndsAtTheEnd()
        {
            // The old version waited forever: a holding director keeps playing.
            director.extrapolationMode = DirectorWrapMode.Hold;
            float start = Time.realtimeSinceStartup;

            yield return TestUtility.RunWithTimeout(director.PlayAndWait(), 3f, "PlayAndWait");

            Assert.GreaterOrEqual(Time.realtimeSinceStartup - start, 0.15f);
        }

        [UnityTest]
        public IEnumerator MissingAsset_WarnsAndEnds()
        {
            director.playableAsset = null;

            LogAssert.Expect(LogType.Warning, new Regex(@"^\[PlayableDirectorExtensions\].*no Playable asset"));
            yield return TestUtility.RunWithTimeout(director.PlayAndWait(), 1f, "PlayAndWait");
        }
    }

    public class InternetMonitorTests
    {
        private GameObject gameObject;
        private GameObject otherGameObject;

        [TearDown]
        public void TearDown()
        {
            InternetMonitor.SimulateOffline = false;

            if (gameObject != null)
                Object.Destroy(gameObject);
            if (otherGameObject != null)
                Object.Destroy(otherGameObject);
        }

        private InternetMonitor CreateMonitor()
        {
            // Configure it before it wakes up, so the first check doesn't send a request.
            gameObject = new GameObject("Internet Monitor");
            gameObject.SetActive(false);
            InternetMonitor monitor = gameObject.AddComponent<InternetMonitor>();
            monitor.VerifyWithRequest = false;
            gameObject.SetActive(true);
            return monitor;
        }

        [UnityTest]
        public IEnumerator WithoutRequests_ReportsReachability()
        {
            InternetMonitor monitor = CreateMonitor();
            yield return null;

            Assert.AreSame(monitor, InternetMonitor.Instance);
            Assert.AreEqual(Application.internetReachability != NetworkReachability.NotReachable, InternetMonitor.IsAvailable);
        }

        [UnityTest]
        public IEnumerator SecondMonitor_IsRemoved()
        {
            InternetMonitor first = CreateMonitor();
            otherGameObject = new GameObject("Second Internet Monitor");
            InternetMonitor second = otherGameObject.AddComponent<InternetMonitor>();
            yield return null;

            Assert.AreSame(first, InternetMonitor.Instance);
            Assert.IsTrue(second == null, "The duplicate component is destroyed.");
        }

        [UnityTest]
        public IEnumerator Destroying_ClearsTheInstance()
        {
            CreateMonitor();
            yield return null;

            Object.Destroy(gameObject);
            yield return null;

            Assert.IsNull(InternetMonitor.Instance);
        }
    }

    public class TransformExtensionsTests
    {
        [UnityTest]
        public IEnumerator DestroyChildren_InPlayMode_DestroysThemByTheEndOfTheFrame()
        {
            GameObject parent = new GameObject("Parent");
            try
            {
                new GameObject("A").transform.SetParent(parent.transform);
                new GameObject("B").transform.SetParent(parent.transform);

                parent.transform.DestroyChildren();
                yield return null;

                Assert.AreEqual(0, parent.transform.childCount);
            }
            finally
            {
                Object.Destroy(parent);
            }
        }
    }
}
