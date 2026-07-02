using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MatchHudRightQuickRailView : MonoBehaviour
    {
        [SerializeField] private Button buildButton;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;

        private Action _buildCommandClicked;
        private Action _zoomInClicked;
        private Action _zoomOutClicked;
        private Func<MatchHudZoomControlState> _zoomStateProvider;
        private ISelectionUiCommand _selectionUiCommandSystem;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private bool _buildButtonListenerInstalled;
        private bool _zoomButtonListenersInstalled;
        private Canvas _cachedCanvas;

        public Button BuildButton => buildButton;
        public Button ZoomInButton
        {
            get
            {
                ResolveZoomButtonsFromChildren();
                return zoomInButton;
            }
        }

        public Button ZoomOutButton
        {
            get
            {
                ResolveZoomButtonsFromChildren();
                return zoomOutButton;
            }
        }

        private void OnEnable()
        {
            ResolveZoomButtonsFromChildren();
            InstallBuildButtonListener();
            InstallZoomButtonListeners();
            ClearButtonSelection(buildButton);
            ClearButtonSelection(zoomInButton);
            ClearButtonSelection(zoomOutButton);
            RefreshZoomControls();
        }

        private void OnDisable()
        {
            UninstallBuildButtonListener();
            UninstallZoomButtonListeners();
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
        }

        public void BindBuildCommand(
            Action buildCommandClicked,
            ISelectionUiCommand selectionUiCommandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null)
        {
            _buildCommandClicked = buildCommandClicked;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            InstallBuildButtonListener();
            ClearButtonSelection(buildButton);
        }

        public void UnbindBuildCommand()
        {
            _buildCommandClicked = null;
            _selectionUiCommandSystem = null;
            _runtimeFeedbackView = null;
        }

        public void BindZoomControls(
            Action zoomInClicked,
            Action zoomOutClicked,
            Func<MatchHudZoomControlState> zoomStateProvider)
        {
            _zoomInClicked = zoomInClicked;
            _zoomOutClicked = zoomOutClicked;
            _zoomStateProvider = zoomStateProvider;
            ResolveZoomButtonsFromChildren();
            InstallZoomButtonListeners();
            RefreshZoomControls();
        }

        public void UnbindZoomControls()
        {
            _zoomInClicked = null;
            _zoomOutClicked = null;
            _zoomStateProvider = null;
            UninstallZoomButtonListeners();
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            ResolveZoomButtonsFromChildren();
            return ContainsButton(buildButton, screenPosition, eventCamera) ||
                   ContainsButton(zoomInButton, screenPosition, eventCamera) ||
                   ContainsButton(zoomOutButton, screenPosition, eventCamera);
        }

        public bool ContainsZoomScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            ResolveZoomButtonsFromChildren();
            return ContainsButton(zoomInButton, screenPosition, eventCamera) ||
                   ContainsButton(zoomOutButton, screenPosition, eventCamera);
        }

        public void RefreshZoomControls()
        {
            ResolveZoomButtonsFromChildren();
            MatchHudZoomControlState state = _zoomStateProvider != null
                ? _zoomStateProvider.Invoke()
                : MatchHudZoomControlState.Disabled;

            if (zoomInButton != null)
                zoomInButton.interactable = state.ZoomInEnabled;
            if (zoomOutButton != null)
                zoomOutButton.interactable = state.ZoomOutEnabled;
        }

        private void OnBuildButtonClicked()
        {
            TriggerBuildCommand();
        }

        private void TriggerBuildCommand()
        {
            _selectionUiCommandSystem?.CaptureUiClickSequence();

            if (_buildCommandClicked != null)
            {
                _buildCommandClicked.Invoke();
                return;
            }

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Build drawer is not ready."));
        }

        private void OnZoomInButtonClicked()
        {
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            _zoomInClicked?.Invoke();
            RefreshZoomControls();
        }

        private void OnZoomOutButtonClicked()
        {
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            _zoomOutClicked?.Invoke();
            RefreshZoomControls();
        }

        private void InstallBuildButtonListener()
        {
            if (buildButton == null)
                return;

            buildButton.onClick.RemoveListener(OnBuildButtonClicked);
            buildButton.onClick.AddListener(OnBuildButtonClicked);
            _buildButtonListenerInstalled = true;
        }

        private void InstallZoomButtonListeners()
        {
            if (zoomInButton == null && zoomOutButton == null)
                return;

            if (zoomInButton != null)
            {
                zoomInButton.onClick.RemoveListener(OnZoomInButtonClicked);
                zoomInButton.onClick.AddListener(OnZoomInButtonClicked);
            }

            if (zoomOutButton != null)
            {
                zoomOutButton.onClick.RemoveListener(OnZoomOutButtonClicked);
                zoomOutButton.onClick.AddListener(OnZoomOutButtonClicked);
            }

            _zoomButtonListenersInstalled = true;
        }

        private void ClearButtonSelection(Button button)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || button == null)
                return;

            if (eventSystem.currentSelectedGameObject == button.gameObject)
                eventSystem.SetSelectedGameObject(null);
        }

        private void UninstallBuildButtonListener()
        {
            if (!_buildButtonListenerInstalled || buildButton == null)
                return;

            buildButton.onClick.RemoveListener(OnBuildButtonClicked);
            _buildButtonListenerInstalled = false;
        }

        private void UninstallZoomButtonListeners()
        {
            if (!_zoomButtonListenersInstalled)
                return;

            if (zoomInButton != null)
                zoomInButton.onClick.RemoveListener(OnZoomInButtonClicked);
            if (zoomOutButton != null)
                zoomOutButton.onClick.RemoveListener(OnZoomOutButtonClicked);
            _zoomButtonListenersInstalled = false;
        }

        private void ResolveZoomButtonsFromChildren()
        {
            if (zoomInButton != null && zoomOutButton != null)
                return;

            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                if (zoomInButton == null && button.name == "ZoomInButton")
                    zoomInButton = button;
                else if (zoomOutButton == null && button.name == "ZoomOutButton")
                    zoomOutButton = button;
            }
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas()
        {
            if (_cachedCanvas == null)
                _cachedCanvas = GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }

        private static bool ContainsButton(Button button, Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform rect = button != null && button.targetGraphic != null
                ? button.targetGraphic.rectTransform
                : button != null
                    ? button.transform as RectTransform
                    : null;

            return rect != null &&
                   button.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

    }
}
