#region Using
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#endregion

namespace NG.Utility.Tests
{
    public class ScrollRectSnapTests
    {
        private const float PageSize = 100f;

        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in created)
            {
                if (gameObject != null)
                    Object.Destroy(gameObject);
            }

            created.Clear();
        }

        /// <summary>
        /// A 100x100 ScrollRect (its own rect is the viewport) with <paramref name="pages"/> 100x100 pages side by side.
        /// </summary>
        private ScrollRectSnap CreateCarousel(int pages, bool vertical = false)
        {
            GameObject root = new GameObject("Scroll", typeof(RectTransform));
            created.Add(root);
            root.SetActive(false);

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(PageSize, PageSize);

            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(rootRect, false);

            if (vertical)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.sizeDelta = new Vector2(0f, PageSize * pages);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
                content.sizeDelta = new Vector2(PageSize * pages, 0f);
            }

            content.anchoredPosition = Vector2.zero;

            for (int i = 0; i < pages; i++)
                AddPage(content, i, vertical);

            ScrollRect scroll = root.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;

            ScrollRectSnap snap = root.AddComponent<ScrollRectSnap>();
            snap.SnapSpeed = 50f;
            return snap;
        }

        private static RectTransform AddPage(RectTransform content, int index, bool vertical)
        {
            RectTransform page = new GameObject("Page " + index, typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(content, false);

            if (vertical)
            {
                page.anchorMin = page.anchorMax = new Vector2(0.5f, 1f);
                page.pivot = new Vector2(0.5f, 1f);
                page.anchoredPosition = new Vector2(0f, -PageSize * index);
            }
            else
            {
                page.anchorMin = page.anchorMax = new Vector2(0f, 0.5f);
                page.pivot = new Vector2(0f, 0.5f);
                page.anchoredPosition = new Vector2(PageSize * index, 0f);
            }

            page.sizeDelta = new Vector2(PageSize, PageSize);
            return page;
        }

        private static PointerEventData Pointer()
        {
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        }

        [UnityTest]
        public IEnumerator Start_JumpsToStartPage()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.StartPage = 2;
            snap.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(3, snap.PageCount);
            Assert.AreEqual(2, snap.CurrentPage);
            Assert.AreEqual(1f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator GoToPage_WithoutAnimation_SetsThePosition()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            snap.GoToPage(1, animate: false);
            Assert.AreEqual(0.5f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);

            snap.GoToPage(2, animate: false);
            Assert.AreEqual(1f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator GoToPage_Animated_ReachesThePage()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            snap.GoToPage(2);

            yield return TestUtility.WaitUntil(() => Mathf.Abs(snap.ScrollRect.horizontalNormalizedPosition - 1f) < 0.001f, 3f, "the content reaches page 2");
        }

        [UnityTest]
        public IEnumerator Snapping_WorksWhileTheGameIsPaused()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            Time.timeScale = 0f;
            try
            {
                snap.GoToPage(1);
                yield return TestUtility.WaitUntil(() => Mathf.Abs(snap.ScrollRect.horizontalNormalizedPosition - 0.5f) < 0.001f, 3f, "the content reaches page 1 with timeScale 0");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator Vertical_FirstPageIsAtTheTop()
        {
            ScrollRectSnap snap = CreateCarousel(3, vertical: true);
            snap.gameObject.SetActive(true);
            yield return null;

            Assert.IsFalse(snap.IsHorizontal);
            Assert.AreEqual(1f, snap.ScrollRect.verticalNormalizedPosition, 0.001f);

            snap.GoToPage(2, animate: false);
            Assert.AreEqual(0f, snap.ScrollRect.verticalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator SinglePage_DoesNotBreakThePosition()
        {
            // The old version divided 0 by 0 here and set the position to NaN.
            ScrollRectSnap snap = CreateCarousel(1);
            snap.gameObject.SetActive(true);
            yield return null;

            snap.Next();
            yield return null;

            Assert.AreEqual(1, snap.PageCount);
            Assert.AreEqual(0, snap.CurrentPage);
            Assert.IsFalse(float.IsNaN(snap.ScrollRect.content.anchoredPosition.x));
        }

        [UnityTest]
        public IEnumerator NextAndPrevious_AreClamped_AndRaisePageChanged()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            List<int> changes = new List<int>();
            snap.PageChanged += changes.Add;

            snap.Previous();
            snap.Next();
            snap.Next();
            snap.Next();

            Assert.AreEqual(2, snap.CurrentPage);
            CollectionAssert.AreEqual(new[] { 1, 2 }, changes);
        }

        [Test]
        public void GoToPage_BeforeStart_Works()
        {
            // The old version threw a NullReferenceException when used before it was initialized.
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);

            snap.GoToPage(2, animate: false);

            Assert.AreEqual(2, snap.CurrentPage);
            Assert.AreEqual(1f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator Drag_PastTheThreshold_MovesToTheNextPage()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            // Pages are 0.5 apart: 0.15 is 30% of a page, past the default 20% threshold.
            snap.OnBeginDrag(Pointer());
            snap.ScrollRect.horizontalNormalizedPosition = 0.15f;
            snap.OnEndDrag(Pointer());

            Assert.AreEqual(1, snap.CurrentPage);
            yield return TestUtility.WaitUntil(() => Mathf.Abs(snap.ScrollRect.horizontalNormalizedPosition - 0.5f) < 0.001f, 3f, "the content snaps to page 1");
        }

        [UnityTest]
        public IEnumerator Drag_UnderTheThreshold_SnapsBack()
        {
            ScrollRectSnap snap = CreateCarousel(3);
            snap.gameObject.SetActive(true);
            yield return null;

            snap.OnBeginDrag(Pointer());
            snap.ScrollRect.horizontalNormalizedPosition = 0.05f;
            snap.OnEndDrag(Pointer());

            Assert.AreEqual(0, snap.CurrentPage);
            yield return TestUtility.WaitUntil(() => snap.ScrollRect.horizontalNormalizedPosition < 0.001f, 3f, "the content snaps back to page 0");
        }

        [UnityTest]
        public IEnumerator Drag_OverSeveralPages_MovesToTheNearest()
        {
            ScrollRectSnap snap = CreateCarousel(5);
            snap.gameObject.SetActive(true);
            yield return null;

            // Pages are 0.25 apart: 0.55 is page 2.2.
            snap.OnBeginDrag(Pointer());
            snap.ScrollRect.horizontalNormalizedPosition = 0.55f;
            snap.OnEndDrag(Pointer());

            Assert.AreEqual(2, snap.CurrentPage);
        }

        [UnityTest]
        public IEnumerator FixedPageCount_SplitsTheContentEvenly()
        {
            ScrollRectSnap snap = CreateCarousel(1);
            snap.ScrollRect.content.sizeDelta = new Vector2(PageSize * 4, 0f);
            snap.FixedPageCount = 4;
            snap.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(4, snap.PageCount);
            snap.GoToPage(1, animate: false);
            Assert.AreEqual(1f / 3f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator Refresh_PicksUpNewPages()
        {
            ScrollRectSnap snap = CreateCarousel(2);
            snap.gameObject.SetActive(true);
            yield return null;

            RectTransform content = snap.ScrollRect.content;
            content.sizeDelta = new Vector2(PageSize * 3, 0f);
            AddPage(content, 2, vertical: false);
            snap.Refresh();

            Assert.AreEqual(3, snap.PageCount);
            snap.GoToPage(2, animate: false);
            Assert.AreEqual(1f, snap.ScrollRect.horizontalNormalizedPosition, 0.001f);
        }

        [UnityTest]
        public IEnumerator PageIndicator_HighlightsTheCurrentPage_AndHidesExtraDots()
        {
            ScrollRectSnap snap = CreateCarousel(3);

            Image[] dots = new Image[4];
            for (int i = 0; i < dots.Length; i++)
            {
                GameObject dot = new GameObject("Dot " + i, typeof(RectTransform));
                created.Add(dot);
                dots[i] = dot.AddComponent<Image>();
            }

            GameObject indicatorObject = new GameObject("Indicator");
            created.Add(indicatorObject);
            PageIndicator indicator = indicatorObject.AddComponent<PageIndicator>();
            indicator.ScrollRectSnap = snap;
            indicator.Dots = dots;

            snap.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(indicator.ActiveColor, dots[0].color);
            Assert.AreEqual(indicator.InactiveColor, dots[1].color);
            Assert.IsFalse(dots[3].gameObject.activeSelf, "There are only 3 pages.");

            snap.GoToPage(2);
            Assert.AreEqual(indicator.InactiveColor, dots[0].color);
            Assert.AreEqual(indicator.ActiveColor, dots[2].color);
        }

        [UnityTest]
        public IEnumerator PageButtons_AreDisabledAtTheEnds_AndChangeThePage()
        {
            ScrollRectSnap snap = CreateCarousel(2);

            GameObject previousObject = new GameObject("Previous", typeof(RectTransform));
            created.Add(previousObject);
            Button previousButton = previousObject.AddComponent<Button>();
            ScrollSnapPageButton previous = previousObject.AddComponent<ScrollSnapPageButton>();
            previous.Direction = ScrollSnapPageButton.PageDirection.Previous;
            previous.ScrollRectSnap = snap;

            GameObject nextObject = new GameObject("Next", typeof(RectTransform));
            created.Add(nextObject);
            Button nextButton = nextObject.AddComponent<Button>();
            ScrollSnapPageButton next = nextObject.AddComponent<ScrollSnapPageButton>();
            next.AtEnd = ScrollSnapPageButton.EndBehavior.Hide;
            next.ScrollRectSnap = snap;

            snap.gameObject.SetActive(true);
            yield return null;

            Assert.IsFalse(previousButton.interactable);
            Assert.IsTrue(nextButton.interactable);

            nextButton.onClick.Invoke();

            Assert.AreEqual(1, snap.CurrentPage);
            Assert.IsTrue(previousButton.interactable);
            CanvasGroup nextGroup = nextObject.GetComponent<CanvasGroup>();
            Assert.IsNotNull(nextGroup, "Hide adds a CanvasGroup.");
            Assert.AreEqual(0f, nextGroup.alpha);
            Assert.IsFalse(nextGroup.blocksRaycasts);

            previousButton.onClick.Invoke();

            Assert.AreEqual(0, snap.CurrentPage);
            Assert.AreEqual(1f, nextGroup.alpha);
        }
    }
}
