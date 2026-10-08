#region Using
using UnityEngine;
#endregion

namespace NG.Utility.Internet
{
    public class Internet : MonoBehaviour
    {
        #region Variables
        bool previousStatus = false;

        public const string INTERNET_STATUS_EVENT = "InternetStatus";
        #endregion

        #region Properties
#if UNITY_EDITOR
        /// <Summary> 
        /// Enable this to hack internet and disable internet
        /// </Summary>
        public static bool IsInterenetHacked { get; set; } = false;
#endif

        /// <Summary> 
        /// Flag to check whether Internet is available or not
        /// </Summary>
        public static bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR
                if (IsInterenetHacked)
                {
                    return false;
                }
#endif

                switch (Application.internetReachability)
                {
                    case NetworkReachability.ReachableViaCarrierDataNetwork:
                    case NetworkReachability.ReachableViaLocalAreaNetwork:
                        return true;
                    case NetworkReachability.NotReachable:
                    default:
                        return false;
                }
            }
        }
        #endregion


        #region Unity Methods
        void Start()
        {
            previousStatus = IsAvailable;
        }

        void Update()
        {
            if (previousStatus != IsAvailable)
            {
                // EventManager.Instance?.ExecuteEvent(Internet.INTERNET_STATUS_EVENT, IsAvailable);
                previousStatus = IsAvailable;
            }
        }
        #endregion
    }
}