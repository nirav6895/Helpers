#region Using
using UnityEngine;
#endregion

namespace NG.DesignPatterns.Singleton
{
    /// <summary>
    /// Singleton Class
    /// </summary>
    public abstract class Singleton<TSingleton> : MonoBehaviour where TSingleton : MonoBehaviour
    {
        #region Variables
        /// <summary>
        /// Single Instance
        /// </summary>
        public static TSingleton Instance { get; protected set; }
        #endregion


        #region Unity Methods
        /// <summary>
        /// Set Instance
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this as TSingleton;
            }
        }
        #endregion
    }
}