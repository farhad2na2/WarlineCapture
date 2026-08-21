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

        private Button _supportButton;
        private bool _missionRestrictionVisibilityApplied;
        private bool _lastBuildDisabled;
        private bool _lastSupportDisabled;

        private Action _buildCommandClicked;
        private Action _zoomInClicked;
        private Action _zoomOutClicked;
        private Func<MatchHudZoomControlState> _zoomStateProvider;
        private ISelectionUiCommand _selectionUiCommandSystem;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private bool _buildButtonListenerInstalled;
        private bool _zoomButtonListenersInstalled;
        private Canvas _cachedCanvas;
        private bool _hasLastZoomControlState;
        private MatchHudZoomControlState _lastZoomControlState;

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
            RefreshMissionRestrictions();
            InstallBuildButtonListener();
            InstallZoomButtonListeners();
            ClearButtonSelection(buildButton);
            ClearButtonSelection(zoomInButton);
            ClearButtonSelection(zoomOutButton);
            _hasLastZoomControlState = false;
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
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
            IGameTextResolver gameTextResolver = null)
        {
            _buildCommandClicked = buildCommandClicked;
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
            InstallBuildButtonListener();
            ClearButtonSelection(buildButton);
        }

        public void UnbindBuildCommand()
        {
            _buildCommandClicked = null;
            _selectionUiCommandSystem = null;
            _runtimeFeedbackView = null;
            _gameTextResolver = FallbackGameTextResolver.Instance;
        }

        public void BindZoomControls(
            Action zoomInClicked,
            Action zoomOutClicked,
            Func<MatchHudZoomControlState> zoomStateProvider)
        {
            _zoomInClicked = zoomInClicked;
            _zoomOutClicked = zoomOutClicked;
            _zoomStateProvider = zoomStateProvider;
            _hasLastZoomControlState = false;
            ResolveZoomButtonsFromChildren();
            InstallZoomButtonListeners();
            RefreshZoomControls();
        }

        public void UnbindZoomControls()
        {
            _zoomInClicked = null;
            _zoomOutClicked = null;
            _zoomStateProvider = null;
            _hasLastZoomControlState = false;
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
            RefreshMissionRestrictions();
            MatchHudZoomControlState state = _zoomStateProvider != null
                ? _zoomStateProvider.Invoke()
                : MatchHudZoomControlState.Disabled;

            if (_hasLastZoomControlState &&
                _lastZoomControlState.ZoomInEnabled == state.ZoomInEnabled &&
                _lastZoomControlState.ZoomOutEnabled == state.ZoomOutEnabled)
            {
                return;
            }

            _hasLastZoomControlState = true;
            _lastZoomControlState = state;

            if (zoomInButton != null && zoomInButton.interactable != state.ZoomInEnabled)
                zoomInButton.interactable = state.ZoomInEnabled;
            if (zoomOutButton != null && zoomOutButton.interactable != state.ZoomOutEnabled)
                zoomOutButton.interactable = state.ZoomOutEnabled;
        }

        private void OnBuildButtonClicked()
        {
            TriggerBuildCommand();
        }

        private void TriggerBuildCommand()
        {
            EmitButtonClickAudio();
            _selectionUiCommandSystem?.CaptureUiClickSequence();

            if (_buildCommandClicked != null)
            {
                _buildCommandClicked.Invoke();
                return;
            }

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                _gameTextResolver.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")), _gameTextResolver);
        }

        private void OnZoomInButtonClicked()
        {
            EmitButtonClickAudio();
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            _zoomInClicked?.Invoke();
            RefreshZoomControls();
        }

        private void OnZoomOutButtonClicked()
        {
            EmitButtonClickAudio();
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            _zoomOutClicked?.Invoke();
            RefreshZoomControls();
        }

        private static void EmitButtonClickAudio()
        {
            UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);
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
            if (zoomInButton != null && zoomOutButton != null && _supportButton != null)
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
                else if (_supportButton == null && button.name == "SupportCommand")
                    _supportButton = button;
            }
        }

        internal void RefreshMissionRestrictions()
        {
            bool buildDisabled = false;
            bool supportDisabled = false;
            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(
                    out UiMissionHudRestrictionsModel restrictions))
            {
                buildDisabled = restrictions.BuildingDisabled || restrictions.ProductionDisabled;
                supportDisabled = restrictions.AirDisabled || restrictions.TransportDisabled;
            }

            ApplyMissionRestrictionVisibility(buildDisabled, supportDisabled);
        }

        internal void ApplyMissionRestrictionVisibility(bool buildDisabled, bool supportDisabled)
        {
            ResolveZoomButtonsFromChildren();
            if (_missionRestrictionVisibilityApplied &&
                _lastBuildDisabled == buildDisabled && _lastSupportDisabled == supportDisabled)
                return;

            SetButtonDisabled(buildButton, buildDisabled);
            SetButtonDisabled(_supportButton, supportDisabled);
            _lastBuildDisabled = buildDisabled;
            _lastSupportDisabled = supportDisabled;
            _missionRestrictionVisibilityApplied = true;
        }

        private static void SetButtonDisabled(Button button, bool disabled)
        {
            if (button == null)
                return;

            if (!button.gameObject.activeSelf)
                button.gameObject.SetActive(true);
            UiDisabledMaterialUtility.SetSelectableDisabled(
                button,
                UiDisabledVisualReason.MissionRestriction,
                disabled);
            UiDisabledMaterialUtility.SetDisabled(
                button.gameObject,
                UiDisabledVisualReason.MissionRestriction,
                disabled);
            button.interactable = !disabled;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = !disabled;
            group.blocksRaycasts = !disabled;
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
