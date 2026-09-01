using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIPopupFrameView))]
    public sealed class IntelRevealV3PopupView : MonoBehaviour
    {
        [SerializeField] private Button footerCloseButton;
        [SerializeField] private Button viewIntelButton;
        [SerializeField] private Button[] inspectButtons = Array.Empty<Button>();

        private UIPopupFrameView _frame;
        private bool _listenersBound;
        private UnityAction[] _inspectActions = Array.Empty<UnityAction>();

        public Button FooterCloseButton => footerCloseButton;
        public Button ViewIntelButton => viewIntelButton;
        public Button[] InspectButtons => inspectButtons;

        public event Action ViewIntelRequested;
        public event Action<int> InspectRequested;

        private void Awake()
        {
            _frame = GetComponent<UIPopupFrameView>();
            BindListeners();
        }

        private void BindListeners()
        {
            if (_listenersBound)
                return;
            if (footerCloseButton != null)
                footerCloseButton.onClick.AddListener(Close);
            if (viewIntelButton != null)
                viewIntelButton.onClick.AddListener(ViewIntel);
            _inspectActions = new UnityAction[inspectButtons.Length];
            for (int index = 0; index < inspectButtons.Length; index++)
            {
                int capturedIndex = index;
                _inspectActions[index] = () => Inspect(capturedIndex);
                if (inspectButtons[index] != null)
                    inspectButtons[index].onClick.AddListener(_inspectActions[index]);
            }
            _listenersBound = true;
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void UnbindListeners()
        {
            if (!_listenersBound)
                return;
            if (footerCloseButton != null)
                footerCloseButton.onClick.RemoveListener(Close);
            if (viewIntelButton != null)
                viewIntelButton.onClick.RemoveListener(ViewIntel);
            int count = Mathf.Min(inspectButtons.Length, _inspectActions.Length);
            for (int index = 0; index < count; index++)
            {
                if (inspectButtons[index] != null && _inspectActions[index] != null)
                    inspectButtons[index].onClick.RemoveListener(_inspectActions[index]);
            }
            _inspectActions = Array.Empty<UnityAction>();
            _listenersBound = false;
        }

        public void Close()
        {
            if (_frame == null)
                _frame = GetComponent<UIPopupFrameView>();
            _frame.Close();
        }

        public void ViewIntel()
        {
            ViewIntelRequested?.Invoke();
            Close();
        }

        private void Inspect(int index)
        {
            InspectRequested?.Invoke(index);
        }

#if UNITY_EDITOR
        public void Configure(
            Button configuredFooterClose,
            Button configuredViewIntel,
            Button[] configuredInspectButtons)
        {
            UnbindListeners();
            footerCloseButton = configuredFooterClose;
            viewIntelButton = configuredViewIntel;
            inspectButtons = configuredInspectButtons ?? Array.Empty<Button>();
            BindListeners();
        }
#endif
    }
}
