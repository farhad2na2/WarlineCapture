using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed class ResourceExchangeShellBinding
    {
        private GameObject _instance;
        private ResourceExchangePopupView _view;
        private Button _closeButton;
        private UnityAction _closeListener;

        public GameObject Install(
            UIShellContentView shell,
            MainMenuPlayUI mainMenuPlayUi,
            GameObject prefab,
            UnityAction closeRequested)
        {
            mainMenuPlayUi?.PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup.ResourceExchange);
            UnbindCloseButton();
            _instance = shell.InstallRoot(prefab, UIShellRegionId.PopupLayer);
            _view = _instance != null ? _instance.GetComponent<ResourceExchangePopupView>() : null;
            mainMenuPlayUi?.BindResourceExchangePopup(_view);
            BindCloseButton(_view, closeRequested);
            return _instance;
        }

        public void Close(UIShellContentView shell, MainMenuPlayUI mainMenuPlayUi, bool playPopupMotion)
        {
            UnbindCloseButton();
            mainMenuPlayUi?.BindResourceExchangePopup(null);
            GameObject popup = _instance;
            _instance = null;
            _view = null;

            if (popup == null)
                return;

            if (playPopupMotion && Application.isPlaying)
            {
                UIPopupMotionView motionView = popup.GetComponent<UIPopupMotionView>();
                if (motionView != null && motionView.PlayHide(() =>
                    {
                        UIShellContentView.DestroyRegionObject(popup);
                        shell.MarkContentChanged();
                    }))
                {
                    return;
                }
            }

            UIShellContentView.DestroyRegionObject(popup);
            shell.MarkContentChanged();
        }

        public void ResetForRegionClear(MainMenuPlayUI mainMenuPlayUi)
        {
            UnbindCloseButton();
            mainMenuPlayUi?.BindResourceExchangePopup(null);
            _instance = null;
            _view = null;
        }

        public void RebindMainMenuPlayUi(MainMenuPlayUI previous, MainMenuPlayUI current)
        {
            if (ReferenceEquals(previous, current))
                return;

            previous?.BindResourceExchangePopup(null);
            current?.BindResourceExchangePopup(_view);
        }

        private void BindCloseButton(ResourceExchangePopupView view, UnityAction closeRequested)
        {
            if (view == null || view.CloseButton == null || closeRequested == null)
                return;

            _closeButton = view.CloseButton;
            _closeListener = closeRequested;
            _closeButton.onClick.RemoveListener(_closeListener);
            _closeButton.onClick.AddListener(_closeListener);
        }

        private void UnbindCloseButton()
        {
            if (_closeButton != null && _closeListener != null)
                _closeButton.onClick.RemoveListener(_closeListener);

            _closeButton = null;
            _closeListener = null;
        }
    }
}
