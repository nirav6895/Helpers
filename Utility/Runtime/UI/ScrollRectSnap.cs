#region Using
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Snaps a <see cref="ScrollRect"/> to pages. After a drag, the content moves to the nearest page, or to the next
    /// one if it was dragged past <see cref="DragThreshold"/> of a page or flicked.
    /// <para>
    /// Add it to the same GameObject as the ScrollRect. It scrolls horizontally if the ScrollRect's Horizontal is on,
    /// otherwise vertically. By default every active child of the content is a page and is centered in the viewport
    /// (clamped at the ends), so spacing and padding of a layout group are respected. Set Page Count to split the
    /// content into that many equal pages instead. Call <see cref="Refresh"/> after adding or removing pages.
    /// </para>
    /// For page dots, add a <see cref="PageIndicator"/>; for previous/next buttons, a <see cref="ScrollSnapPageButton"/>.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    [DisallowMultipleComponent]
    [AddComponentMenu("NG/UI/Scroll Rect Snap")]
    public class ScrollRectSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        #region Inspector Variables
        [Tooltip("Number of equal pages the content is split into. 0 = one page per active child of the content.")]
        [SerializeField, Min(0)] private int pageCount = 0;

        [Tooltip("Page shown when the scene starts.")]
        [SerializeField, Min(0)] private int startPage = 0;

        [Tooltip("How fast the content moves to the page. Higher is faster.")]
        [SerializeField, Min(0.1f)] private float snapSpeed = 10f;

        [Tooltip("Fraction of a page the content has to be dragged to move to the next page.")]
        [SerializeField, Range(0.05f, 0.5f)] private float dragThreshold = 0.2f;

        [Tooltip("Release speed, in pages per second, above which a short swipe still moves to the next page. 0 = off.")]
        [SerializeField, Min(0f)] private float flickSpeed = 2f;

        [Tooltip("Move with unscaled time, so snapping works while the game is paused (Time.timeScale = 0).")]
        [SerializeField] private bool useUnscaledTime = true;

        [SerializeField] private UnityEvent<int> onPageChanged = new UnityEvent<int>();
        #endregion


        #region Variables
        /// <summary>
        /// Raised with the new page index when <see cref="CurrentPage"/> changes. <see cref="Refresh"/> always raises it,
        /// because the page count may have changed.
        /// </summary>
        public event Action<int> PageChanged;

        /// <summary>
        /// Smallest move per frame, in content units. ScrollRect ignores moves under 0.01.
        /// </summary>
        private const float MinStep = 0.02f;

        private ScrollRect scrollRect;
        private readonly List<RectTransform> pages = new List<RectTransform>();
        private readonly Vector3[] corners = new Vector3[4];
        private float[] pagePositions = Array.Empty<float>();
        private int currentPage;
        private bool initialized;
        private bool startPageApplied;
        private bool isDragging;
        private bool isSnapping;
        private bool resnapPending;
        private float dragPage;
        private float dragVelocity;
        #endregion


        #region Properties
        /// <summary>
        /// The ScrollRect on this GameObject.
        /// </summary>
        public ScrollRect ScrollRect
        {
            get
            {
                if (scrollRect == null)
                    scrollRect = GetComponent<ScrollRect>();

                return scrollRect;
            }
        }

        /// <summary>
        /// Index of the current page, or of the page the content is moving to.
        /// </summary>
        public int CurrentPage => currentPage;

        /// <summary>
        /// Number of pages. 0 until the component has started (or <see cref="Refresh"/> was called).
        /// </summary>
        public int PageCount => pagePositions.Length;

        /// <summary>
        /// Number of equal pages the content is split into. 0 = one page per active child of the content.
        /// Setting it refreshes the pages.
        /// </summary>
        public int FixedPageCount
        {
            get => pageCount;
            set
            {
                pageCount = Mathf.Max(0, value);
                if (initialized)
                    Refresh();
            }
        }

        /// <summary>
        /// Page shown when the component starts, or when pages first appear after a <see cref="Refresh"/>.
        /// Set it before then; afterwards use <see cref="GoToPage"/>.
        /// </summary>
        public int StartPage
        {
            get => startPage;
            set => startPage = Mathf.Max(0, value);
        }

        /// <summary>
        /// Whether pages are laid out horizontally: true if the ScrollRect's Horizontal is on, or Vertical is off.
        /// </summary>
        public bool IsHorizontal => ScrollRect.horizontal || !ScrollRect.vertical;

        /// <summary>
        /// How fast the content moves to the page. Higher is faster.
        /// </summary>
        public float SnapSpeed
        {
            get => snapSpeed;
            set => snapSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Fraction of a page (0.05 to 0.5) the content has to be dragged to move to the next page.
        /// </summary>
        public float DragThreshold
        {
            get => dragThreshold;
            set => dragThreshold = Mathf.Clamp(value, 0.05f, 0.5f);
        }

        /// <summary>
        /// Release speed, in pages per second, above which a short swipe still moves to the next page. 0 = off.
        /// </summary>
        public float FlickSpeed
        {
            get => flickSpeed;
            set => flickSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Move with unscaled time, so snapping works while the game is paused.
        /// </summary>
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }
        #endregion


        #region Unity Methods
        private void Start()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            // Finish a snap that was interrupted by disabling.
            if (initialized)
                isSnapping = true;
        }

        private void OnDisable()
        {
            isDragging = false;
            isSnapping = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            // e.g. the screen rotated: page positions change. Layout may be rebuilding now, so wait for Update.
            if (initialized)
                resnapPending = true;
        }

        private void Update()
        {
            if (resnapPending && !isDragging)
            {
                resnapPending = false;
                Rebuild();
                if (PageCount > 0)
                {
                    SetNormalizedPosition(pagePositions[currentPage]);
                    isSnapping = false;
                }
            }

            if (isDragging)
            {
                TrackDragVelocity();
                return;
            }

            if (isSnapping)
                MoveTowardsCurrentPage();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Moves to a page. Out-of-range pages are clamped.
        /// </summary>
        /// <param name="page">Page index.</param>
        /// <param name="animate">Move smoothly; otherwise jump there at once.</param>
        public void GoToPage(int page, bool animate = true)
        {
            EnsureInitialized();
            if (PageCount == 0)
                return;

            page = Mathf.Clamp(page, 0, PageCount - 1);
            bool changed = page != currentPage;
            currentPage = page;

            ScrollRect.StopMovement();

            if (animate)
            {
                isSnapping = true;
            }
            else
            {
                SetNormalizedPosition(pagePositions[page]);
                isSnapping = false;
            }

            if (changed)
                RaisePageChanged();
        }

        /// <summary>
        /// Moves to the next page, if there is one.
        /// </summary>
        public void Next()
        {
            GoToPage(currentPage + 1);
        }

        /// <summary>
        /// Moves to the previous page, if there is one.
        /// </summary>
        public void Previous()
        {
            GoToPage(currentPage - 1);
        }

        /// <summary>
        /// Reads the pages again, e.g. after adding or removing children of the content, and moves back onto the
        /// current page (clamped to the new page count). Raises <see cref="PageChanged"/>.
        /// </summary>
        public void Refresh()
        {
            if (!initialized)
            {
                EnsureInitialized();
                return;
            }

            Rebuild();
            if (!ApplyStartPage())
                isSnapping = !isDragging && PageCount > 0;

            RaisePageChanged();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            EnsureInitialized();
            isDragging = true;
            isSnapping = false;
            dragVelocity = 0f;
            dragPage = PositionToPage(GetNormalizedPosition());
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !isDragging)
                return;

            isDragging = false;

            // ScrollRect's inertia would keep moving the content and fight the snap.
            ScrollRect.StopMovement();

            if (PageCount == 0)
                return;

            float page = PositionToPage(GetNormalizedPosition());
            int target;

            if (flickSpeed > 0f && Mathf.Abs(dragVelocity) >= flickSpeed)
            {
                target = dragVelocity > 0f ? Mathf.FloorToInt(page) + 1 : Mathf.CeilToInt(page) - 1;
            }
            else
            {
                float offset = page - currentPage;
                if (offset >= dragThreshold)
                    target = Mathf.Max(currentPage + 1, Mathf.RoundToInt(page));
                else if (offset <= -dragThreshold)
                    target = Mathf.Min(currentPage - 1, Mathf.RoundToInt(page));
                else
                    target = currentPage;
            }

            // Always animate back, even when the page doesn't change.
            target = Mathf.Clamp(target, 0, PageCount - 1);
            bool changed = target != currentPage;
            currentPage = target;
            isSnapping = true;

            if (changed)
                RaisePageChanged();
        }
        #endregion


        #region Private Methods
        private void EnsureInitialized()
        {
            if (initialized)
                return;

            Rebuild();
            if (ApplyStartPage())
                RaisePageChanged();
        }

        /// <summary>
        /// Jumps to Start Page the first time there are pages, which may be after a <see cref="Refresh"/>
        /// if the content is filled by script.
        /// </summary>
        private bool ApplyStartPage()
        {
            if (startPageApplied || PageCount == 0)
                return false;

            startPageApplied = true;
            currentPage = Mathf.Clamp(startPage, 0, PageCount - 1);
            SetNormalizedPosition(pagePositions[currentPage]);
            isSnapping = false;
            return true;
        }

        /// <summary>
        /// Reads the pages and their normalized scroll positions, and clamps the current page.
        /// </summary>
        private void Rebuild()
        {
            initialized = true;

            ScrollRect scroll = ScrollRect;
            RectTransform content = scroll.content;
            bool horizontal = IsHorizontal;

            pages.Clear();
            int count = pageCount;
            if (count == 0 && content != null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    if (content.GetChild(i) is RectTransform child && child.gameObject.activeSelf)
                        pages.Add(child);
                }

                count = pages.Count;
            }

            if (pagePositions.Length != count)
                pagePositions = new float[count];

            if (count == 0)
            {
                currentPage = 0;
                return;
            }

            if (pageCount > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = count == 1 ? 0f : (float)i / (count - 1);

                    // Vertical normalized position is 1 at the top, where the first page is.
                    pagePositions[i] = horizontal ? t : 1f - t;
                }
            }
            else
            {
                // Children positions come from the layout group, which may not have run yet.
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                for (int i = 0; i < count; i++)
                    pagePositions[i] = GetCenteredPosition(pages[i], horizontal);
            }

            currentPage = Mathf.Clamp(currentPage, 0, count - 1);
        }

        /// <summary>
        /// Normalized scroll position that centers <paramref name="page"/> in the viewport, clamped to 0..1.
        /// It is measured relative to the content, so it doesn't depend on the current scroll position.
        /// </summary>
        private float GetCenteredPosition(RectTransform page, bool horizontal)
        {
            RectTransform viewport = GetViewport();
            Rect view = viewport.rect;
            Bounds contentBounds = GetBoundsInViewport(ScrollRect.content, viewport);
            Bounds pageBounds = GetBoundsInViewport(page, viewport);

            int axis = horizontal ? 0 : 1;
            float hiddenLength = contentBounds.size[axis] - view.size[axis];
            if (hiddenLength <= 0f)
                return horizontal ? 0f : 1f;

            // Same formula as ScrollRect's normalized position, after moving the content to center the page.
            float offset = view.center[axis] - pageBounds.center[axis];
            float contentMin = contentBounds.min[axis] + offset;
            return Mathf.Clamp01((view.min[axis] - contentMin) / hiddenLength);
        }

        /// <summary>
        /// Converts a normalized scroll position to a fractional page index, e.g. 1.5 is halfway between pages 1 and 2.
        /// </summary>
        private float PositionToPage(float position)
        {
            int last = pagePositions.Length - 1;
            if (last <= 0)
                return 0f;

            // Work with increasing positions: vertical positions decrease from the first page to the last.
            float sign = pagePositions[last] >= pagePositions[0] ? 1f : -1f;
            float p = position * sign;
            if (p <= pagePositions[0] * sign)
                return 0f;

            for (int i = 0; i < last; i++)
            {
                float a = pagePositions[i] * sign;
                float b = pagePositions[i + 1] * sign;
                if (p <= b)
                    return b - a > 0.0001f ? i + (p - a) / (b - a) : i + 1;
            }

            return last;
        }

        private void TrackDragVelocity()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float page = PositionToPage(GetNormalizedPosition());
            if (deltaTime > 0f)
                dragVelocity = Mathf.Lerp(dragVelocity, (page - dragPage) / deltaTime, Mathf.Clamp01(deltaTime * 10f));

            dragPage = page;
        }

        private void MoveTowardsCurrentPage()
        {
            GetScrollState(out float position, out float hiddenLength);
            if (PageCount == 0 || hiddenLength <= 0f)
            {
                isSnapping = false;
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            float target = pagePositions[currentPage];

            // Frame-rate independent smoothing.
            float fraction = 1f - Mathf.Exp(-snapSpeed * deltaTime);
            float remaining = Mathf.Abs(target - position) * hiddenLength;

            // Finish within half a unit, or when this frame's step is too small for ScrollRect to apply: it ignores moves
            // under 0.01 units, which at very high frame rates would leave the content stuck just short of the page.
            float next;
            if (remaining < 0.5f || remaining * fraction < MinStep)
            {
                next = target;
                isSnapping = false;
            }
            else
            {
                next = Mathf.Lerp(position, target, fraction);
            }

            ApplyNormalizedPosition(next);
        }

        private float GetNormalizedPosition()
        {
            GetScrollState(out float position, out _);
            return position;
        }

        private void SetNormalizedPosition(float position)
        {
            // When the content fits in the viewport, ScrollRect would move it away from its resting place.
            GetScrollState(out _, out float hiddenLength);
            if (hiddenLength > 0f)
                ApplyNormalizedPosition(position);
        }

        private void ApplyNormalizedPosition(float position)
        {
            if (IsHorizontal)
                ScrollRect.horizontalNormalizedPosition = position;
            else
                ScrollRect.verticalNormalizedPosition = position;
        }

        /// <summary>
        /// The normalized scroll position and how much of the content is outside the viewport along the scroll axis,
        /// from one measurement of the content (the ScrollRect's own getter measures it again on every call).
        /// Same formula as ScrollRect's normalized position; 0 or 1 when the content fits.
        /// </summary>
        private void GetScrollState(out float position, out float hiddenLength)
        {
            bool horizontal = IsHorizontal;
            RectTransform content = ScrollRect.content;
            if (content == null)
            {
                position = horizontal ? 0f : 1f;
                hiddenLength = 0f;
                return;
            }

            RectTransform viewport = GetViewport();
            Rect view = viewport.rect;
            Bounds contentBounds = GetBoundsInViewport(content, viewport);

            int axis = horizontal ? 0 : 1;
            hiddenLength = contentBounds.size[axis] - view.size[axis];
            position = hiddenLength > 0f
                ? (view.min[axis] - contentBounds.min[axis]) / hiddenLength
                : horizontal ? 0f : 1f;
        }

        private RectTransform GetViewport()
        {
            return ScrollRect.viewport != null ? ScrollRect.viewport : (RectTransform)ScrollRect.transform;
        }

        private Bounds GetBoundsInViewport(RectTransform target, RectTransform viewport)
        {
            target.GetWorldCorners(corners);
            Matrix4x4 toViewport = viewport.worldToLocalMatrix;

            Bounds bounds = new Bounds(toViewport.MultiplyPoint3x4(corners[0]), Vector3.zero);
            for (int i = 1; i < 4; i++)
                bounds.Encapsulate(toViewport.MultiplyPoint3x4(corners[i]));

            return bounds;
        }

        private void RaisePageChanged()
        {
            PageChanged?.Invoke(currentPage);
            onPageChanged.Invoke(currentPage);
        }
        #endregion
    }
}
