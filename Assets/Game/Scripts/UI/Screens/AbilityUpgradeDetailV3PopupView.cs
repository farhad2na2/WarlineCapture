using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIPopupFrameView))]
    public sealed class AbilityUpgradeDetailV3PopupView : MonoBehaviour
    {
        [SerializeField] private Button viewSourceButton;
        [SerializeField] private Button unlockButton;

        private UIPopupFrameView _frame;
        private bool _listenersBound;

        public Button ViewSourceButton => viewSourceButton;
        public Button UnlockButton => unlockButton;
        public event Action ViewSourceRequested;
        public event Action UnlockRequested;

        private void Awake()
        {
            _frame = GetComponent<UIPopupFrameView>();
            BindListeners();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void SetUnlocked(bool unlocked)
        {
            if (unlockButton != null)
                unlockButton.interactable = unlocked;
        }

        private void ViewSource()
        {
            ViewSourceRequested?.Invoke();
            Close();
        }

        private void Unlock()
        {
            if (unlockButton == null || !unlockButton.interactable)
                return;
            UnlockRequested?.Invoke();
        }

        private void Close()
        {
            if (_frame == null)
                _frame = GetComponent<UIPopupFrameView>();
            _frame.Close();
        }

        private void BindListeners()
        {
            if (_listenersBound)
                return;
            if (viewSourceButton != null)
                viewSourceButton.onClick.AddListener(ViewSource);
            if (unlockButton != null)
                unlockButton.onClick.AddListener(Unlock);
            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound)
                return;
            if (viewSourceButton != null)
                viewSourceButton.onClick.RemoveListener(ViewSource);
            if (unlockButton != null)
                unlockButton.onClick.RemoveListener(Unlock);
            _listenersBound = false;
        }

#if UNITY_EDITOR
        public void Configure(Button configuredViewSource, Button configuredUnlock)
        {
            UnbindListeners();
            viewSourceButton = configuredViewSource;
            unlockButton = configuredUnlock;
            BindListeners();
        }
#endif
    }
}
