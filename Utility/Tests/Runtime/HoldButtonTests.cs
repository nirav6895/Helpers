#region Using
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
#endregion

namespace NG.Utility.Tests
{
    public class HoldButtonTests
    {
        private GameObject gameObject;
        private HoldButton button;
        private int started;
        private int completed;
        private int canceled;
        private int clicked;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("HoldButton", typeof(RectTransform));
            button = gameObject.AddComponent<HoldButton>();
            button.HoldDuration = 0.1f;

            started = completed = canceled = clicked = 0;
            button.HoldStarted += () => started++;
            button.HoldCompleted += () => completed++;
            button.HoldCanceled += () => canceled++;
            button.onClick.AddListener(() => clicked++);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(gameObject);
        }

        private static PointerEventData Pointer(PointerEventData.InputButton inputButton = PointerEventData.InputButton.Left)
        {
            return new PointerEventData(EventSystem.current) { button = inputButton, pointerId = -1 };
        }

        [UnityTest]
        public IEnumerator Hold_CompletesAfterHoldDuration()
        {
            button.OnPointerDown(Pointer());
            Assert.AreEqual(1, started);
            Assert.IsTrue(button.IsHolding);

            yield return TestUtility.WaitUntil(() => completed > 0, 2f, "the hold completes");

            Assert.AreEqual(1, completed);
            Assert.AreEqual(1f, button.HoldProgress);

            button.OnPointerUp(Pointer());
            Assert.AreEqual(0, canceled);
            Assert.IsFalse(button.IsHolding);
            Assert.AreEqual(0f, button.HoldProgress);
        }

        [UnityTest]
        public IEnumerator ReleasingEarly_Cancels()
        {
            button.HoldDuration = 10f;
            button.OnPointerDown(Pointer());
            yield return null;

            button.OnPointerUp(Pointer());

            Assert.AreEqual(1, canceled);
            Assert.AreEqual(0, completed);
        }

        [UnityTest]
        public IEnumerator LeavingTheButton_Cancels()
        {
            button.HoldDuration = 10f;
            button.OnPointerDown(Pointer());
            yield return null;

            button.OnPointerExit(Pointer());

            Assert.AreEqual(1, canceled);
            Assert.IsFalse(button.IsHolding);
        }

        [Test]
        public void RightClick_DoesNotStartAHold()
        {
            button.OnPointerDown(Pointer(PointerEventData.InputButton.Right));

            Assert.AreEqual(0, started);
            Assert.IsFalse(button.IsHolding);
        }

        [Test]
        public void NonInteractable_DoesNotStartAHold()
        {
            button.interactable = false;
            button.OnPointerDown(Pointer());

            Assert.AreEqual(0, started);
        }

        [UnityTest]
        public IEnumerator Hold_CompletesWhileTheGameIsPaused()
        {
            Time.timeScale = 0f;
            button.OnPointerDown(Pointer());

            yield return TestUtility.WaitUntil(() => completed > 0, 2f, "the hold completes with timeScale 0");
        }

        [UnityTest]
        public IEnumerator Click_AfterACompletedHold_IsSuppressed()
        {
            button.OnPointerDown(Pointer());
            yield return TestUtility.WaitUntil(() => completed > 0, 2f, "the hold completes");

            button.OnPointerUp(Pointer());
            button.OnPointerClick(Pointer());
            Assert.AreEqual(0, clicked);

            // A normal tap still clicks.
            button.OnPointerDown(Pointer());
            button.OnPointerUp(Pointer());
            button.OnPointerClick(Pointer());
            Assert.AreEqual(1, clicked);
        }

        [UnityTest]
        public IEnumerator Click_AfterACompletedHold_IsKept_WhenNotSuppressed()
        {
            button.SuppressClickAfterHold = false;
            button.OnPointerDown(Pointer());
            yield return TestUtility.WaitUntil(() => completed > 0, 2f, "the hold completes");

            button.OnPointerUp(Pointer());
            button.OnPointerClick(Pointer());

            Assert.AreEqual(1, clicked);
        }

        [UnityTest]
        public IEnumerator Disabling_DuringAHold_Cancels()
        {
            button.HoldDuration = 10f;
            button.OnPointerDown(Pointer());
            yield return null;

            gameObject.SetActive(false);

            Assert.AreEqual(1, canceled);
            Assert.IsFalse(button.IsHolding);
        }

        [UnityTest]
        public IEnumerator RepeatWhileHeld_KeepsCompleting()
        {
            button.HoldDuration = 0.05f;
            button.RepeatWhileHeld = true;
            button.RepeatInterval = 0.02f;
            button.OnPointerDown(Pointer());

            yield return TestUtility.WaitUntil(() => completed >= 3, 2f, "the hold repeats");

            button.OnPointerUp(Pointer());
            int count = completed;
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.AreEqual(count, completed, "Repeats stop on release.");
        }

        [UnityTest]
        public IEnumerator Progress_IsReportedWhileHolding()
        {
            float lastProgress = -1f;
            button.HoldProgressChanged += progress => lastProgress = progress;
            button.HoldDuration = 0.2f;

            button.OnPointerDown(Pointer());
            Assert.AreEqual(0f, lastProgress);

            yield return TestUtility.WaitUntil(() => lastProgress > 0f && lastProgress < 1f, 2f, "progress is between 0 and 1");
            yield return TestUtility.WaitUntil(() => completed > 0, 2f, "the hold completes");
            Assert.AreEqual(1f, lastProgress);

            button.OnPointerUp(Pointer());
            Assert.AreEqual(0f, lastProgress);
        }
    }
}
