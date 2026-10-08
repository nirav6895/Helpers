namespace NG.Utility.Animation
{
    #region Using
    using UnityEngine;
    #endregion

    /// <Summary> 
    /// Rotate Object with give speed
    /// </Summary>
    public class RotateObject : MonoBehaviour
    {
        #region Variables
        /// <Summary> 
        /// Rotate Speed
        /// </Summary>
        [SerializeField]
        float rotateSpeed = 0;
        #endregion


        #region Unity Methods
        /// <Summary> 
        /// Keep Rotating the object
        /// </Summary>
        void Update()
        {
            transform.Rotate(Vector3.forward, rotateSpeed);
        }
        #endregion
    }
}