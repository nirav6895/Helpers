#region Using
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Keeps rotating its object at a constant speed, e.g. a loading spinner or a collectible.
    /// The speed is in degrees per second, so it is the same at any frame rate.
    /// </summary>
    [AddComponentMenu("NG/Animation/Auto Rotate")]
    public class AutoRotate : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("Axis to rotate around. Z (0, 0, 1) spins 2D and UI objects in the screen plane.")]
        [SerializeField] private Vector3 axis = Vector3.forward;

        [Tooltip("Degrees per second. Negative values rotate the other way. Around Z, positive values turn counterclockwise on screen.")]
        [SerializeField] private float speed = 180f;

        [Tooltip("Rotate around the object's own axes (Self) or the world axes (World).")]
        [SerializeField] private Space space = Space.Self;

        [Tooltip("Keep rotating while the game is paused (Time.timeScale = 0), e.g. for loading spinners.")]
        [SerializeField] private bool useUnscaledTime = false;
        #endregion


        #region Properties
        /// <summary>
        /// Axis to rotate around, in the object's own space or world space depending on <see cref="Space"/>.
        /// </summary>
        public Vector3 Axis
        {
            get => axis;
            set => axis = value;
        }

        /// <summary>
        /// Degrees per second. Negative values rotate the other way.
        /// </summary>
        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        /// <summary>
        /// Whether <see cref="Axis"/> is in the object's own space or world space.
        /// </summary>
        public Space Space
        {
            get => space;
            set => space = value;
        }

        /// <summary>
        /// Rotate with <see cref="Time.unscaledDeltaTime"/>, so it keeps rotating while the game is paused.
        /// </summary>
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }
        #endregion


        #region Unity Methods
        private void Update()
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(axis, speed * deltaTime, space);
        }
        #endregion
    }
}
