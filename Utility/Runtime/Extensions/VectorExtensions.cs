#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Extension methods for <see cref="Vector2"/> and <see cref="Vector3"/>.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Returns a copy with some components replaced, e.g. <c>transform.position = transform.position.With(y: 0f);</c>
        /// </summary>
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        /// <summary>
        /// Returns a copy with some components replaced, e.g. <c>size = size.With(x: 100f);</c>
        /// </summary>
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        }
    }
}
