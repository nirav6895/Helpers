#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Fits its RectTransform to the screen's safe area (<see cref="Screen.safeArea"/>), so content stays clear of
    /// notches, rounded corners and the home indicator. It updates when the safe area or the screen size changes,
    /// e.g. on rotation.
    /// <para>
    /// Add it to a full-screen child of a Screen Space canvas and put the content that must stay visible under it;
    /// backgrounds that should fill the whole screen go outside it. It sets this RectTransform's anchors and offsets
    /// in Play Mode. Use the Device Simulator to preview phones with a notch.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("NG/UI/Safe Area")]
    public class SafeArea : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("Keep out of the unsafe area on the left edge.")]
        [SerializeField] private bool left = true;

        [Tooltip("Keep out of the unsafe area on the right edge.")]
        [SerializeField] private bool right = true;

        [Tooltip("Keep out of the unsafe area on the top edge.")]
        [SerializeField] private bool top = true;

        [Tooltip("Keep out of the unsafe area on the bottom edge.")]
        [SerializeField] private bool bottom = true;
        #endregion


        #region Variables
        private RectTransform rectTransform;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;

        // The screen's safe area and size, read once per frame and shared by every SafeArea (e.g. one per screen prefab).
        private static int screenReadFrame = -1;
        private static Rect screenSafeArea;
        private static Vector2Int screenSize;
        #endregion


        #region Properties
        /// <summary>
        /// Keep out of the unsafe area on the left edge.
        /// </summary>
        public bool Left
        {
            get => left;
            set
            {
                left = value;
                Apply();
            }
        }

        /// <summary>
        /// Keep out of the unsafe area on the right edge.
        /// </summary>
        public bool Right
        {
            get => right;
            set
            {
                right = value;
                Apply();
            }
        }

        /// <summary>
        /// Keep out of the unsafe area on the top edge.
        /// </summary>
        public bool Top
        {
            get => top;
            set
            {
                top = value;
                Apply();
            }
        }

        /// <summary>
        /// Keep out of the unsafe area on the bottom edge.
        /// </summary>
        public bool Bottom
        {
            get => bottom;
            set
            {
                bottom = value;
                Apply();
            }
        }
        #endregion


        #region Unity Methods
        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            ReadScreen();
            if (screenSafeArea != appliedSafeArea || screenSize != appliedScreenSize)
                Apply();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Fits the RectTransform to the current safe area. It's called automatically when the safe area changes.
        /// </summary>
        public void Apply()
        {
            if (rectTransform == null)
                rectTransform = (RectTransform)transform;

            Rect safeArea = Screen.safeArea;
            Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
            appliedSafeArea = safeArea;
            appliedScreenSize = screen;

            if (screen.x <= 0 || screen.y <= 0)
                return;

            Vector2 min = safeArea.min;
            Vector2 max = safeArea.max;

            if (!left)
                min.x = 0f;
            if (!bottom)
                min.y = 0f;
            if (!right)
                max.x = screen.x;
            if (!top)
                max.y = screen.y;

            rectTransform.anchorMin = new Vector2(min.x / screen.x, min.y / screen.y);
            rectTransform.anchorMax = new Vector2(max.x / screen.x, max.y / screen.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        #endregion


        #region Private Methods
        private static void ReadScreen()
        {
            int frame = Time.frameCount;
            if (frame == screenReadFrame)
                return;

            screenReadFrame = frame;
            screenSafeArea = Screen.safeArea;
            screenSize = new Vector2Int(Screen.width, Screen.height);
        }

        /// <summary>
        /// Clears the shared screen values when Play Mode starts, so it works with Domain Reload disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            screenReadFrame = -1;
        }
        #endregion
    }
}
