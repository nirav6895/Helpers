#region Using
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// A <see cref="Button"/> that also reacts to being held down. A tap still invokes On Click; holding it for
    /// <see cref="HoldDuration"/> seconds invokes <see cref="HoldCompleted"/>.
    /// <para>
    /// The hold is canceled when the pointer is released early, leaves the button, or the button becomes
    /// non-interactable or disabled. Only the left mouse button and touches start a hold.
    /// <see cref="HoldProgress"/> goes from 0 to 1 while holding; connect On Hold Progress to an Image's fillAmount
    /// to show it without code.
    /// </para>
    /// <para>
    /// For +/- buttons, turn on Repeat While Held and connect On Click and On Hold Completed to the same method:
    /// a tap changes the value once, and holding keeps changing it every Repeat Interval.
    /// </para>
    /// </summary>
    [AddComponentMenu("NG/UI/Hold Button")]
    public class HoldButton : Button
    {
        #region Inspector Variables
        [Tooltip("Seconds the button has to be held down.")]
        [SerializeField, Min(0f), FormerlySerializedAs("requireHoldTime")] private float holdDuration = 1f;

        [Tooltip("Count time with Time.unscaledDeltaTime, so holding works while the game is paused (Time.timeScale = 0).")]
        [SerializeField] private bool useUnscaledTime = true;

        [Tooltip("Don't invoke On Click when the button is released after a completed hold.")]
        [SerializeField] private bool suppressClickAfterHold = true;

        [Tooltip("Keep invoking On Hold Completed every Repeat Interval while the button stays held, e.g. for +/- buttons.")]
        [SerializeField] private bool repeatWhileHeld = false;

        [Tooltip("Seconds between repeats while the button stays held.")]
        [SerializeField, Min(0.01f)] private float repeatInterval = 0.1f;

        [SerializeField] private UnityEvent onHoldStarted = new UnityEvent();
        [SerializeField] private UnityEvent onHoldCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onHoldCanceled = new UnityEvent();
        [SerializeField] private UnityEvent<float> onHoldProgress = new UnityEvent<float>();
        #endregion


        #region Variables
        /// <summary>
        /// Raised when a hold starts (the button is pressed).
        /// </summary>
        public event Action HoldStarted;

        /// <summary>
        /// Raised when the button has been held for <see cref="HoldDuration"/> seconds, and again every
        /// <see cref="RepeatInterval"/> while <see cref="RepeatWhileHeld"/> is on.
        /// </summary>
        public event Action HoldCompleted;

        /// <summary>
        /// Raised when a hold ends before it completed.
        /// </summary>
        public event Action HoldCanceled;

        /// <summary>
        /// Raised every frame while holding with <see cref="HoldProgress"/>, and with 0 when the hold ends.
        /// </summary>
        public event Action<float> HoldProgressChanged;

        private bool isHolding;
        private bool holdCompleted;
        private bool suppressNextClick;
        private int holdPointerId;
        private float heldTime;
        private float nextRepeatTime;
        private Coroutine holdRoutine;
        #endregion


        #region Properties
        /// <summary>
        /// Seconds the button has to be held down. 0 completes on the first frame of the hold.
        /// </summary>
        public float HoldDuration
        {
            get => holdDuration;
            set => holdDuration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Count time with <see cref="Time.unscaledDeltaTime"/>, so holding works while the game is paused.
        /// </summary>
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }

        /// <summary>
        /// Don't invoke On Click when the button is released after a completed hold.
        /// </summary>
        public bool SuppressClickAfterHold
        {
            get => suppressClickAfterHold;
            set => suppressClickAfterHold = value;
        }

        /// <summary>
        /// Keep raising <see cref="HoldCompleted"/> every <see cref="RepeatInterval"/> while the button stays held.
        /// </summary>
        public bool RepeatWhileHeld
        {
            get => repeatWhileHeld;
            set => repeatWhileHeld = value;
        }

        /// <summary>
        /// Seconds between repeats. At least 0.01.
        /// </summary>
        public float RepeatInterval
        {
            get => repeatInterval;
            set => repeatInterval = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Whether the button is being held right now.
        /// </summary>
        public bool IsHolding => isHolding;

        /// <summary>
        /// 0 to 1 while holding (1 once the hold has completed), 0 when not holding.
        /// </summary>
        public float HoldProgress
        {
            get
            {
                if (!isHolding)
                    return 0f;

                return holdDuration <= 0f ? 1f : Mathf.Clamp01(heldTime / holdDuration);
            }
        }
        #endregion


        #region Overridden Methods
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            suppressNextClick = false;

            if (isHolding || !IsActive() || !IsInteractable())
                return;

            isHolding = true;
            holdCompleted = false;
            heldTime = 0f;
            holdPointerId = eventData.pointerId;

            // Start before the events, so a listener that disables the button stops it.
            holdRoutine = StartCoroutine(HoldRoutine());

            HoldStarted?.Invoke();
            onHoldStarted.Invoke();
            RaiseProgress(0f);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (isHolding && eventData.button == PointerEventData.InputButton.Left && eventData.pointerId == holdPointerId)
                EndHold();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            if (isHolding && eventData.pointerId == holdPointerId)
                EndHold();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            base.OnPointerClick(eventData);
        }

        protected override void OnDisable()
        {
            if (isHolding)
                EndHold();

            suppressNextClick = false;
            base.OnDisable();
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Counts the hold time. It only runs while the button is held, so idle buttons cost nothing per frame.
        /// </summary>
        private IEnumerator HoldRoutine()
        {
            while (isHolding)
            {
                yield return null;

                if (!isHolding)
                    yield break;

                if (!IsInteractable())
                {
                    EndHold();
                    yield break;
                }

                heldTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                if (!holdCompleted)
                {
                    if (heldTime < holdDuration)
                    {
                        RaiseProgress(HoldProgress);
                        continue;
                    }

                    holdCompleted = true;
                    nextRepeatTime = heldTime + repeatInterval;
                    RaiseProgress(1f);
                    RaiseCompleted();
                }
                else if (repeatWhileHeld && heldTime >= nextRepeatTime)
                {
                    // One repeat per frame: after a long frame, the missed repeats follow on the next frames.
                    nextRepeatTime += repeatInterval;
                    RaiseCompleted();
                }
            }
        }

        private void EndHold()
        {
            isHolding = false;
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;
            }

            suppressNextClick = holdCompleted && suppressClickAfterHold;
            RaiseProgress(0f);

            if (!holdCompleted)
            {
                HoldCanceled?.Invoke();
                onHoldCanceled.Invoke();
            }
        }

        private void RaiseCompleted()
        {
            HoldCompleted?.Invoke();
            onHoldCompleted.Invoke();
        }

        private void RaiseProgress(float progress)
        {
            HoldProgressChanged?.Invoke(progress);
            onHoldProgress.Invoke(progress);
        }
        #endregion
    }
}
