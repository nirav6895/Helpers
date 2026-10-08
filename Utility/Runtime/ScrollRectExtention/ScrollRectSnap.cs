#region Using
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
#endregion

namespace NG.Utility.ScrollRectExtention
{
    public class ScrollRectSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        #region Inspector Variables
        public ScrollRect scrollRect;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private Image[] pageDots;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private int pageCount = 3;
        [SerializeField] private float snapSpeed = 10f;
        [SerializeField, Range(0.01f, 0.5f)] private float snapThreshold = 0.1f; // Sensitivity for page switch
        #endregion


        #region Variables
        private float[] pagePositions;
        private float targetPos;
        private bool isLerping;
        private float startDragPos;
        internal int curPage = 0;
        public event Action OnUpdatePage;
        #endregion


        #region Properties
        public int PageCount
        {
            get => pageCount;
            set
            {
                pageCount = value;
                Initialize();
            }
        }
        #endregion


        #region Unity Methods
        private void Start()
        {
            if (initializeOnStart)
                Initialize();
        }

        private void Update()
        {
            if (isLerping)
            {
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetPos, snapSpeed * Time.deltaTime);
                if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetPos) < 0.001f)
                {
                    scrollRect.horizontalNormalizedPosition = targetPos;
                    isLerping = false;
                }
            }
        }
        #endregion


        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            startDragPos = scrollRect.horizontalNormalizedPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float endDragPos = scrollRect.horizontalNormalizedPosition;
            float dragDelta = endDragPos - startDragPos;
            int targetPage = curPage;

            if (Mathf.Abs(dragDelta) > snapThreshold)
            {
                if (dragDelta > 0 && curPage < pageCount - 1)
                    targetPage = curPage + 1;
                else if (dragDelta < 0 && curPage > 0)
                    targetPage = curPage - 1;
            }

            UpdatePagePosition(targetPage);
        }

        public void UpdatePagePosition(int curPage)
        {
            if (curPage < 0 || curPage >= pageCount)
                return;

            this.curPage = curPage;
            targetPos = pagePositions[curPage];
            isLerping = true;
            UpdatePageDots();
            OnUpdatePage?.Invoke();
        }
        #endregion


        #region Private Methods
        private void Initialize()
        {
            pagePositions = new float[pageCount];
            for (int i = 0; i < pageCount; i++)
                pagePositions[i] = (float)i / (pageCount - 1);

            UpdatePagePosition(curPage);
        }

        private void UpdatePageDots()
        {
            if (pageDots != null)
            {
                for (int i = 0; i < pageDots.Length; i++)
                    pageDots[i].color = (i == curPage) ? activeColor : inactiveColor;
            }
        }

        private int GetClosestPage(float position)
        {
            float closest = Mathf.Abs(position - pagePositions[0]);
            int closestPage = 0;

            for (int i = 1; i < pagePositions.Length; i++)
            {
                float distance = Mathf.Abs(position - pagePositions[i]);
                if (distance < closest)
                {
                    closest = distance;
                    closestPage = i;
                }
            }

            return closestPage;
        }
        #endregion
    }
}