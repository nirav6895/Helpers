using System;
using UnityEngine;
using UnityEngine.UI;

namespace NG.Utility
{
    public class HoldButton : Button
    {
        #region Inspector Variables
        [SerializeField] private float requireHoldTime = 1f;
        #endregion


        #region Variables
        public event Action OnStartHold;
        public event Action OnCompleteHold;
        public event Action OnReleaseHold;

        private bool isHolding = false;
        #endregion


        #region Overridden Methods
        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (interactable)
            {
                isHolding = true;
                OnStartHold?.Invoke();
                Invoke(nameof(OnCompleteRequiredHoldTime), requireHoldTime);
            }
        }

        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (isHolding)
            {
                isHolding = false;
                OnReleaseHold?.Invoke();
                CancelInvoke(nameof(OnCompleteRequiredHoldTime));
            }
        }
        #endregion


        #region Private Methods
        private void OnCompleteRequiredHoldTime()
        {
            // If still holding, then invoke the event
            if (isHolding)
            {
                isHolding = false;
                OnCompleteHold?.Invoke();
            }
        }
        #endregion
    }
}