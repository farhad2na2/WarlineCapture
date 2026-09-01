using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Runtime contract for the POP-02 confirmation surface. Presentation stays
    /// in the prefab while callers may subscribe to the confirmed action.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConfirmRaidV3PopupView : MonoBehaviour
    {
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;

        public Button CancelButton => cancelButton;
        public Button ConfirmButton => confirmButton;
        public bool WasConfirmed { get; private set; }

        public event Action Confirmed;

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            WasConfirmed = false;
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void Cancel()
        {
            WasConfirmed = false;
            gameObject.SetActive(false);
        }

        public void Confirm()
        {
            WasConfirmed = true;
            Confirmed?.Invoke();
            gameObject.SetActive(false);
        }

        private void BindButtons()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);
        }

        private void UnbindButtons()
        {
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(Cancel);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(Confirm);
        }

#if UNITY_EDITOR
        public void Configure(Button configuredCancelButton, Button configuredConfirmButton)
        {
            UnbindButtons();
            cancelButton = configuredCancelButton;
            confirmButton = configuredConfirmButton;
            BindButtons();
        }
#endif
    }
}
