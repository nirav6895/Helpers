#region Using
using System;
using UnityEngine;
using UnityEngine.UI;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Page dots for a <see cref="ScrollRectSnap"/>: the current page's dot gets the active color (and sprite),
    /// the others the inactive one. Assign one Image per page, in page order. Dots beyond the page count are hidden.
    /// </summary>
    [AddComponentMenu("NG/UI/Page Indicator")]
    public class PageIndicator : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("The ScrollRectSnap whose current page is shown.")]
        [SerializeField] private ScrollRectSnap scrollRectSnap;

        [Tooltip("One dot per page, in page order. Dots beyond the page count are hidden.")]
        [SerializeField] private Image[] dots = Array.Empty<Image>();

        [Tooltip("Color of the current page's dot.")]
        [SerializeField] private Color activeColor = Color.white;

        [Tooltip("Color of the other dots.")]
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.4f);

        [Tooltip("Optional. Sprite of the current page's dot. Leave both sprites empty to keep each dot's own sprite.")]
        [SerializeField] private Sprite activeSprite;

        [Tooltip("Optional. Sprite of the other dots.")]
        [SerializeField] private Sprite inactiveSprite;
        #endregion


        #region Properties
        /// <summary>
        /// The ScrollRectSnap whose current page is shown.
        /// </summary>
        public ScrollRectSnap ScrollRectSnap
        {
            get => scrollRectSnap;
            set
            {
                if (scrollRectSnap == value)
                    return;

                Unsubscribe();
                scrollRectSnap = value;

                if (isActiveAndEnabled)
                {
                    Subscribe();
                    Refresh();
                }
            }
        }

        /// <summary>
        /// One dot per page, in page order. Call <see cref="Refresh"/> after changing the array's contents.
        /// </summary>
        public Image[] Dots
        {
            get => dots;
            set
            {
                dots = value ?? Array.Empty<Image>();
                Refresh();
            }
        }

        /// <summary>
        /// Color of the current page's dot.
        /// </summary>
        public Color ActiveColor
        {
            get => activeColor;
            set
            {
                activeColor = value;
                Refresh();
            }
        }

        /// <summary>
        /// Color of the other dots.
        /// </summary>
        public Color InactiveColor
        {
            get => inactiveColor;
            set
            {
                inactiveColor = value;
                Refresh();
            }
        }
        #endregion


        #region Unity Methods
        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Updates the dots to the current page and page count.
        /// </summary>
        public void Refresh()
        {
            if (dots == null)
                return;

            int pageCount = scrollRectSnap != null ? scrollRectSnap.PageCount : 0;
            int currentPage = scrollRectSnap != null ? scrollRectSnap.CurrentPage : -1;

            for (int i = 0; i < dots.Length; i++)
            {
                Image dot = dots[i];
                if (dot == null)
                    continue;

                bool visible = i < pageCount;
                if (dot.gameObject.activeSelf != visible)
                    dot.gameObject.SetActive(visible);

                bool active = i == currentPage;
                dot.color = active ? activeColor : inactiveColor;

                Sprite sprite = active ? activeSprite : inactiveSprite;
                if (sprite != null)
                    dot.sprite = sprite;
            }
        }
        #endregion


        #region Private Methods
        private void Subscribe()
        {
            if (scrollRectSnap != null)
                scrollRectSnap.PageChanged += OnPageChanged;
        }

        private void Unsubscribe()
        {
            if (scrollRectSnap != null)
                scrollRectSnap.PageChanged -= OnPageChanged;
        }

        private void OnPageChanged(int page)
        {
            Refresh();
        }
        #endregion
    }
}
