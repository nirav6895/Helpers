#region Using
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#endregion

namespace NG.Utility.Samples
{
    /// <summary>
    /// Drives the UI Demo scene's texts. The components themselves (ScrollRectSnap, PageIndicator, ScrollSnapPageButton,
    /// HoldButton, SafeArea, AutoRotate, InternetMonitor) are set up in the Inspector; the hold button's fill bar is
    /// connected through On Hold Progress, without code.
    /// </summary>
    public class UIDemo : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField] private ScrollRectSnap carousel;
        [SerializeField] private HoldButton holdButton;
        [SerializeField] private Text pageText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text internetText;

        [Tooltip("Shown while the InternetMonitor is checking.")]
        [SerializeField] private GameObject checkingIndicator;

        [Tooltip("Seconds of the repeating reward countdown.")]
        [SerializeField, Min(1f)] private float rewardSeconds = 90f;
        #endregion


        #region Variables
        private CountdownTimer rewardTimer;
        private long coins = 1500;
        private int lastShownSeconds = -1;
        #endregion


        #region Unity Methods
        private void Awake()
        {
            rewardTimer = new CountdownTimer(rewardSeconds) { Repeat = true };
        }

        private void OnEnable()
        {
            carousel.PageChanged += OnPageChanged;
            holdButton.HoldCompleted += OnHoldCompleted;
            rewardTimer.Completed += OnRewardTimerCompleted;
            InternetMonitor.StatusChanged += OnInternetStatusChanged;
        }

        private void Start()
        {
            // The scene has no EventSystem, so that the right input module is used for the project's input handling.
            if (EventSystem.current == null)
                CreateEventSystem();

            rewardTimer.Start();
            OnPageChanged(carousel.CurrentPage);
            UpdateCoins();
            OnInternetStatusChanged(InternetMonitor.IsAvailable);
        }

        private void OnDisable()
        {
            carousel.PageChanged -= OnPageChanged;
            holdButton.HoldCompleted -= OnHoldCompleted;
            rewardTimer.Completed -= OnRewardTimerCompleted;
            InternetMonitor.StatusChanged -= OnInternetStatusChanged;
        }

        private void Update()
        {
            rewardTimer.Tick();

            // Only build a new string when the shown second changes, not every frame.
            int shownSeconds = Mathf.CeilToInt(rewardTimer.Remaining - 0.001f);
            if (shownSeconds != lastShownSeconds)
            {
                lastShownSeconds = shownSeconds;
                timerText.text = "Next reward in " + TimeFormatting.ToClock(rewardTimer.Remaining, roundUp: true);
            }

            bool checking = InternetMonitor.Instance != null && InternetMonitor.Instance.IsChecking;
            if (checkingIndicator != null && checkingIndicator.activeSelf != checking)
                checkingIndicator.SetActive(checking);
        }
        #endregion


        #region Private Methods
        private void OnPageChanged(int page)
        {
            pageText.text = $"Page {page + 1} of {carousel.PageCount}";
        }

        private void OnHoldCompleted()
        {
            coins *= 7;
            UpdateCoins();
        }

        private void OnRewardTimerCompleted()
        {
            coins += 1000;
            UpdateCoins();
        }

        private void UpdateCoins()
        {
            coinsText.text = "Coins: " + coins.ToAbbreviatedString();
        }

        private void OnInternetStatusChanged(bool available)
        {
            internetText.text = available ? "Internet: online" : "Internet: offline";
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
            // Found by name, so the sample doesn't need a reference to the Input System package.
            Type inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                eventSystem.AddComponent(inputSystemModule);
                return;
            }
#endif

            eventSystem.AddComponent<StandaloneInputModule>();
        }
        #endregion
    }
}
