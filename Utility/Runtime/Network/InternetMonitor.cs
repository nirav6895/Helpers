#region Using
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// Watches whether the device can reach the internet and raises <see cref="StatusChanged"/> when that changes.
    /// <para>
    /// <see cref="Application.internetReachability"/> only says whether a network interface is up: a Wi-Fi network
    /// without internet, or one that shows a login page first (a captive portal), still counts as reachable.
    /// So with Verify With Request on, the monitor also requests Check Url every Check Interval seconds and only
    /// counts the device as online when the expected response comes back.
    /// </para>
    /// <para>
    /// Add it to one GameObject in your first scene. <see cref="IsAvailable"/> and <see cref="StatusChanged"/> are
    /// static, so any script can use them. Without a monitor, IsAvailable falls back to Application.internetReachability.
    /// It checks again right away when the app comes back from the background. Main thread only.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("NG/Network/Internet Monitor")]
    public class InternetMonitor : MonoBehaviour
    {
        #region Inspector Variables
        [Tooltip("Seconds between checks.")]
        [SerializeField, Min(1f)] private float checkInterval = 5f;

        [Tooltip("Request Check Url to confirm the internet actually works. When off, only Application.internetReachability is used, which counts a Wi-Fi network without internet as online.")]
        [SerializeField] private bool verifyWithRequest = true;

        [Tooltip("URL requested to confirm the connection. Use one that answers quickly with little data. On WebGL it must allow cross-origin requests (CORS).")]
        [SerializeField] private string checkUrl = "https://www.google.com/generate_204";

        [Tooltip("HTTP status code that counts as online. 0 = any 2xx code. A captive portal answers with its login page (200), so 204 tells the two apart.")]
        [SerializeField, Min(0)] private int expectedResponseCode = 204;

        [Tooltip("Seconds to wait for the response before counting the device as offline.")]
        [SerializeField, Min(1)] private int timeout = 5;

        [Tooltip("Keep this object alive across scene loads (DontDestroyOnLoad). It is moved to the root of the hierarchy.")]
        [SerializeField] private bool persistAcrossScenes = true;
        #endregion


        #region Variables
        /// <summary>
        /// Raised when <see cref="IsAvailable"/> changes, with the new value.
        /// </summary>
        public static event Action<bool> StatusChanged;

        private static InternetMonitor instance;
        private static bool? lastResult;
        private static bool simulateOffline;

        private Coroutine checkLoop;
        private UnityWebRequest currentRequest;
        private bool isChecking;
        private bool checkRequested;
        private string invalidUrlWarned;
        #endregion


        #region Properties
        /// <summary>
        /// Whether the internet is reachable: the result of the last check, or Application.internetReachability
        /// before the first check and when no monitor is active. Always false while <see cref="SimulateOffline"/> is on.
        /// </summary>
        public static bool IsAvailable => !simulateOffline && (lastResult ?? IsReachable());

        /// <summary>
        /// Makes <see cref="IsAvailable"/> false, to test how the game handles going offline. Checks keep running,
        /// so turning it off restores the real status. It resets when Play Mode starts. In the Editor, it can also be
        /// switched from the monitor's Inspector in Play Mode.
        /// </summary>
        public static bool SimulateOffline
        {
            get => simulateOffline;
            set
            {
                if (simulateOffline == value)
                    return;

                bool wasAvailable = IsAvailable;
                simulateOffline = value;
                RaiseIfChanged(wasAvailable);
            }
        }

        /// <summary>
        /// The active monitor, or null if there is none.
        /// </summary>
        public static InternetMonitor Instance => instance;

        /// <summary>
        /// Whether a check is running right now.
        /// </summary>
        public bool IsChecking => isChecking;

        /// <summary>
        /// Seconds between checks. At least 1.
        /// </summary>
        public float CheckInterval
        {
            get => checkInterval;
            set => checkInterval = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Whether <see cref="CheckUrl"/> is requested to confirm the connection.
        /// </summary>
        public bool VerifyWithRequest
        {
            get => verifyWithRequest;
            set => verifyWithRequest = value;
        }

        /// <summary>
        /// URL requested to confirm the connection.
        /// </summary>
        public string CheckUrl
        {
            get => checkUrl;
            set => checkUrl = value;
        }

        /// <summary>
        /// HTTP status code that counts as online. 0 = any 2xx code.
        /// </summary>
        public int ExpectedResponseCode
        {
            get => expectedResponseCode;
            set => expectedResponseCode = Mathf.Max(0, value);
        }

        /// <summary>
        /// Seconds to wait for the response. At least 1.
        /// </summary>
        public int Timeout
        {
            get => timeout;
            set => timeout = Mathf.Max(1, value);
        }
        #endregion


        #region Unity Methods
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (!instance.persistAcrossScenes)
                    Debug.LogWarning($"[InternetMonitor] There is already an InternetMonitor on '{instance.name}'. The one on '{name}' was removed.", this);

                Destroy(this);
                return;
            }

            instance = this;

            if (persistAcrossScenes && Application.isPlaying)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            if (instance == this)
                checkLoop = StartCoroutine(CheckLoop());
        }

        private void OnDisable()
        {
            if (checkLoop != null)
            {
                StopCoroutine(checkLoop);
                checkLoop = null;
            }

            // A stopped coroutine doesn't run its using block, so dispose the request here.
            if (currentRequest != null)
            {
                currentRequest.Dispose();
                currentRequest = null;
            }

            isChecking = false;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            instance = null;

            // Fall back to Application.internetReachability.
            bool wasAvailable = IsAvailable;
            lastResult = null;
            RaiseIfChanged(wasAvailable);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                CheckNow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                CheckNow();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Checks again without waiting for the rest of Check Interval. If a check is running, the next one starts
        /// right after it. Does nothing while the monitor is disabled.
        /// </summary>
        public void CheckNow()
        {
            checkRequested = true;
        }
        #endregion


        #region Private Methods
        // One coroutine for checking and waiting: a nested coroutine might keep running after StopCoroutine.
        private IEnumerator CheckLoop()
        {
            while (true)
            {
                checkRequested = false;
                isChecking = true;
                bool online;

                if (!IsReachable())
                {
                    online = false;
                }
                else if (!verifyWithRequest || string.IsNullOrEmpty(checkUrl))
                {
                    online = true;
                }
                else
                {
                    UnityWebRequest request = CreateRequest();
                    if (request == null)
                    {
                        online = false;
                    }
                    else
                    {
                        using (request)
                        {
                            currentRequest = request;
                            yield return request.SendWebRequest();
                            currentRequest = null;

                            online = request.result == UnityWebRequest.Result.Success
                                && (expectedResponseCode == 0 || request.responseCode == expectedResponseCode);
                        }
                    }
                }

                isChecking = false;

                bool wasAvailable = IsAvailable;
                lastResult = online;
                RaiseIfChanged(wasAvailable);

                float wait = checkInterval;
                while (wait > 0f && !checkRequested)
                {
                    yield return null;
                    wait -= Time.unscaledDeltaTime;
                }
            }
        }

        private UnityWebRequest CreateRequest()
        {
            try
            {
                UnityWebRequest request = UnityWebRequest.Get(checkUrl);
                request.timeout = timeout;
                return request;
            }
            catch (Exception exception)
            {
                // Warn once per URL, not on every check.
                if (invalidUrlWarned != checkUrl)
                {
                    invalidUrlWarned = checkUrl;
                    Debug.LogWarning($"[InternetMonitor] Check Url '{checkUrl}' can't be requested, so the device counts as offline: {exception.Message}", this);
                }

                return null;
            }
        }

        private static bool IsReachable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        private static void RaiseIfChanged(bool wasAvailable)
        {
            bool isAvailable = IsAvailable;
            if (isAvailable != wasAvailable)
                StatusChanged?.Invoke(isAvailable);
        }

        /// <summary>
        /// Clears the static state when Play Mode starts, so it works with Domain Reload disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            StatusChanged = null;
            instance = null;
            lastResult = null;
            simulateOffline = false;
        }
        #endregion
    }
}
