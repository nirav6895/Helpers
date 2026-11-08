#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="Transform"/>.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Destroys every child of the transform.
        /// <para>
        /// In Play Mode it uses Object.Destroy, which destroys them at the end of the frame, so childCount doesn't
        /// change until then. In Edit Mode (editor scripts) it uses Object.DestroyImmediate.
        /// </para>
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }
    }
}
