#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="CanvasGroup"/>.
    /// </summary>
    public static class CanvasGroupExtensions
    {
        /// <summary>
        /// Shows or hides the group without deactivating its GameObject: sets alpha to 1 or 0, and interactable
        /// and blocksRaycasts to <paramref name="visible"/>, so a hidden group can't be clicked.
        /// </summary>
        public static void SetVisible(this CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
