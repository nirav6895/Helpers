#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="Color"/> and <see cref="Color32"/>.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a copy with a different alpha, e.g. <c>image.color = image.color.WithAlpha(0.5f);</c>
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Returns a copy with a different alpha (0 to 255).
        /// </summary>
        public static Color32 WithAlpha(this Color32 color, byte alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
