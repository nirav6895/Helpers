#region Using
using UnityEngine;
using UnityEngine.UI;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// A previous or next page button for a <see cref="ScrollRectSnap"/>. Add it to a <see cref="Button"/>.
    /// When there is no page in its direction, the button is disabled or hidden, as set in At End.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("NG/UI/Scroll Snap Page Button")]
    public class ScrollSnapPageButton : MonoBehaviour
    {
        #region Enums
        /// <summary>
        /// Which page the button moves to.
        /// </summary>
        public enum PageDirection
        {
            Previous,
            Next
        }

        /// <summary>
        /// What happens to the button when there is no page in its direction.
        /// </summary>
        public enum EndBehavior
        {
            /// <summary>
            /// Nothing: the button stays as it is, and clicking it does nothing.
            /// </summary>
            None,

            /// <summary>
            /// The button is made non-interactable.
            /// </summary>
            Disable,

            /// <summary>
            /// The button is hidden with a CanvasGroup, which is added if missing.
            /// </summary>
            Hide
        }
        #endregion


        #region Inspector Variables
        [Tooltip("The ScrollRectSnap whose page this button changes.")]
        [SerializeField] private ScrollRectSnap scrollRectSnap;

        [Tooltip("Which page the button moves to.")]
        [SerializeField] private PageDirection direction = PageDirection.Next;

        [Tooltip("What happens to the button when there is no page in its direction. Hide uses a CanvasGroup, which is added if missing.")]
        [SerializeField] private EndBehavior atEnd = EndBehavior.Disable;
        #endregion


        #region Variables
        private Button button;
        private CanvasGroup canvasGroup;
        private bool disabledByThis;
        #endregion


        #region Properties
        /// <summary>
        /// The ScrollRectSnap whose page this button changes.
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
                    Subscribe();

                UpdateState();
            }
        }

        /// <summary>
        /// Which page the button moves to.
        /// </summary>
        public PageDirection Direction
        {
            get => direction;
            set
            {
                direction = value;
                UpdateState();
            }
        }

        /// <summary>
        /// What happens to the button when there is no page in its direction.
        /// </summary>
        public EndBehavior AtEnd
        {
            get => atEnd;
            set
            {
                atEnd = value;
                UpdateState();
            }
        }

        private Button Button
        {
            get
            {
                if (button == null)
                    button = GetComponent<Button>();

                return button;
            }
        }
        #endregion


        #region Unity Methods
        private void Awake()
        {
            Button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            Subscribe();
            UpdateState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        private void Reset()
        {
            scrollRectSnap = GetComponentInParent<ScrollRectSnap>();
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

        private void OnClick()
        {
            if (scrollRectSnap == null)
                return;

            if (direction == PageDirection.Previous)
                scrollRectSnap.Previous();
            else
                scrollRectSnap.Next();
        }

        private void OnPageChanged(int page)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            bool hasPage = HasPageInDirection();

            switch (atEnd)
            {
                case EndBehavior.Disable:
                    SetDisabled(!hasPage);
                    SetHidden(false);
                    break;

                case EndBehavior.Hide:
                    SetDisabled(false);
                    SetHidden(!hasPage);
                    break;

                default:
                    SetDisabled(false);
                    SetHidden(false);
                    break;
            }
        }

        /// <summary>
        /// Turns the button's interactable off, and back on only if this component turned it off,
        /// so a button disabled by other code stays disabled.
        /// </summary>
        private void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                if (Button.interactable)
                {
                    Button.interactable = false;
                    disabledByThis = true;
                }
            }
            else if (disabledByThis)
            {
                Button.interactable = true;
                disabledByThis = false;
            }
        }

        private bool HasPageInDirection()
        {
            if (scrollRectSnap == null)
                return false;

            return direction == PageDirection.Previous
                ? scrollRectSnap.CurrentPage > 0
                : scrollRectSnap.CurrentPage < scrollRectSnap.PageCount - 1;
        }

        private void SetHidden(bool hidden)
        {
            // Only add a CanvasGroup when something has to be hidden.
            if (!hidden && canvasGroup == null)
                return;

            if (canvasGroup == null)
                canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

            canvasGroup.SetVisible(!hidden);
        }
        #endregion
    }
}
