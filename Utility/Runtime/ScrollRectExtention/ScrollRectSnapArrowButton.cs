#region Using
using UnityEngine;
using UnityEngine.UI;
#endregion

namespace NG.Utility.ScrollRectExtention
{
    [RequireComponent(typeof(Button))]
    public class ScrollRectSnapArrowButton : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] private Direction direction;
        [SerializeField] private ScrollRectSnap scrollRectSnap;
        [SerializeField] private bool disableButtonAtEndPage = false;
        #endregion


        #region Variables
        private Button button;
        #endregion


        #region Unity Methods
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClickArrowButton);
        }

        private void OnEnable()
        {
            if (scrollRectSnap != null)
                scrollRectSnap.OnUpdatePage += UpdateButton;
        }

        private void OnDisable()
        {
            if (scrollRectSnap != null)
                scrollRectSnap.OnUpdatePage -= UpdateButton;
        }
        #endregion


        #region Private Methods
        private void OnClickArrowButton()
        {
            if (scrollRectSnap == null)
                return;

            scrollRectSnap.UpdatePagePosition(direction == Direction.Left ?
                scrollRectSnap.curPage - 1 : scrollRectSnap.curPage + 1);
        }

        private void UpdateButton()
        {
            if (scrollRectSnap == null)
                return;

            if (!disableButtonAtEndPage)
                return;

            if (direction == Direction.Left)
            {
                button.image.enabled = button.interactable = scrollRectSnap.curPage != 0;
            }
            else
            {
                button.image.enabled = button.interactable = scrollRectSnap.curPage < scrollRectSnap.PageCount - 1;
            }
        }
        #endregion
    }

    public enum Direction
    {
        Left,
        Right
    }
}
