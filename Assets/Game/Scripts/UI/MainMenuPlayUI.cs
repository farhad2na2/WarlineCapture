using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal enum MatchHudLargeTacticalPopup : byte
    {
        Assistant = 0,
        BuildDrawer = 1,
        FullMap = 2,
        ResourceExchange = 3
    }

    public sealed class MainMenuPlayUI : IMatchRuntimeUi
    {
        private const float CompactMinimapUpdateIntervalSeconds = 0.1f;
        private const float AssistantPanelRefreshIntervalSeconds = 0.1f;
        private const float ZoomControlRefreshIntervalSeconds = 0.1f;
        private static readonly ProfilerMarker MinimapUpdateMarker = new("MainMenuPlayUI.MinimapUpdate");
        private static readonly ProfilerMarker FeedbackLifetimeMarker = new("MainMenuPlayUI.FeedbackLifetime");

        private readonly MatchHudMinimapInputUiSystemHelper _matchHudMinimapInputSystem = new();
        private readonly MatchHudMinimapInputUiSystemHelper _matchHudFullMapInputSystem = new();
        private readonly MatchHudAssistantUiSystemHelper _matchHudAssistantUiSystem = new();
        private readonly MatchHudResourceHeaderPresentation _matchHudResourceHeaderPresentation = new();
        private IMatchRuntimeState _runtimeGameplayStateSystem;
        private ISelectionUiCommand _selectionUiCommandSystem;
        private IMatchHudCameraControl _selectionUiCameraSystem;
        private IMatchHudMinimapDataSource _minimapDataSource;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private MatchOverlayCommandControlsView _matchHudCommandControlsView;
        private MatchHudRightQuickRailView _matchHudRightQuickRailView;
        private MatchHudMinimapView _matchHudMinimapView;
        private MatchHudFullMapPopupView _matchHudFullMapPopupView;
        private MatchHudSelectionPanelView _matchHudSelectionPanelView;
        private BattleHudRuntimeFeedbackView _matchHudRuntimeFeedbackView;
        private MatchHudSquadTrayView _matchHudSquadTrayView;
        private MatchHudHeaderReferenceUiSystemHelper _matchHudHeaderReferences;
        private GameObject _matchHudThreatJumpPanel;
        private TMP_Text _matchHudThreatTitle;
        private float _matchHudThreatVisibleUntil = float.NegativeInfinity;
        private float _nextCompactMinimapUpdateTime;
        private float _nextAssistantPanelRefreshTime;
        private float _nextZoomControlRefreshTime;
        private BuildDrawerView _buildDrawerView;
        private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
        private ResourceExchangePopupView _resourceExchangePopupView;
        private System.Action _closeBuildDrawerPopup;
        private System.Action _closeFullMapPopup;
        private System.Action _closeResourceExchangePopup;
        private System.Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
        private System.Action<IBattleHudRuntimeFeedbackSink> _bindMatchHudRuntimeFeedback;
        private System.Action<IMatchHudSquadTrayView> _bindMatchHudSquadTray;
        private EventSystem _raycastEventSystem;
        private PointerEventData _raycastPointerData;
        private readonly List<RaycastResult> _raycastResults = new(16);
        private readonly List<Button> _matchHudResourceExchangeButtons = new(4);
        private int _lastGameplayUiClickFrame = -1000;

        internal IGameTextResolver GameTextResolver => _gameTextResolver;

        public void Init(
            ISelectionUiCommand selectionUiCommandSystem,
            IMatchRuntimeState runtimeGameplayStateSystem,
            IMatchHudCameraControl selectionUiCameraSystem = null,
            IMatchHudMinimapDataSource minimapDataSource = null,
            IGameTextResolver gameTextResolver = null,
            bool resetRuntimeState = true)
        {
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeGameplayStateSystem = runtimeGameplayStateSystem;
            _selectionUiCameraSystem = selectionUiCameraSystem;
            _minimapDataSource = minimapDataSource;
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;

            if (!resetRuntimeState || _runtimeGameplayStateSystem == null)
                return;

            _runtimeGameplayStateSystem.PlayRequested = false;
            _runtimeGameplayStateSystem.SimulationActive = false;
            _runtimeGameplayStateSystem.SelectionModeActive = false;
            _runtimeGameplayStateSystem.BuildModeActive = false;
            _runtimeGameplayStateSystem.ZoomInHeld = false;
            _runtimeGameplayStateSystem.ZoomOutHeld = false;
            _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
        }

        public void Dispose()
        {
            _matchHudMinimapInputSystem.Dispose();
            _matchHudFullMapInputSystem.Dispose();
            _matchHudAssistantUiSystem.Unbind();
            if (_matchHudMinimapView != null)
                _matchHudMinimapView.FullMapOpenRequested -= RequestFullMapPopup;
            if (_matchHudFullMapPopupView != null)
                _matchHudFullMapPopupView.CloseRequested -= RequestFullMapClose;
            _matchHudRightQuickRailView?.UnbindZoomControls();
            _matchHudCommandControlsView = null;
            _matchHudRightQuickRailView = null;
            _matchHudMinimapView = null;
            _matchHudFullMapPopupView = null;
            _matchHudSelectionPanelView = null;
            _matchHudRuntimeFeedbackView = null;
            _matchHudSquadTrayView?.Unbind();
            _matchHudSquadTrayView = null;
            _matchHudHeaderReferences = null;
            UnbindMatchHudResourceExchangeButtons();
            BindMatchHudThreatJumpPanel(null);
            _buildDrawerView = null;
            _buildPlacementConfirmationBarView = null;
            _resourceExchangePopupView = null;
            _closeBuildDrawerPopup = null;
            _closeFullMapPopup = null;
            _closeResourceExchangePopup = null;
            _bindMatchHudSelectionPanel = null;
            _bindMatchHudRuntimeFeedback = null;
            _bindMatchHudSquadTray = null;
            _selectionUiCommandSystem = null;
            _runtimeGameplayStateSystem = null;
            _selectionUiCameraSystem = null;
            _minimapDataSource = null;
            _gameTextResolver = FallbackGameTextResolver.Instance;
            MatchHudAssistantPanelOpenChanged = null;
        }

        public void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true &&
                TryCloseMatchHudAssistantForBack())
            {
                return;
            }

            float now = Time.unscaledTime;
            using (MinimapUpdateMarker.Auto())
            {
                bool fullMapOpen = _matchHudFullMapPopupView != null && _matchHudFullMapPopupView.IsOpen;
                if (!fullMapOpen && now >= _nextCompactMinimapUpdateTime)
                {
                    _nextCompactMinimapUpdateTime = now + CompactMinimapUpdateIntervalSeconds;
                    _matchHudMinimapInputSystem.Update();
                }

                if (fullMapOpen)
                    _matchHudFullMapInputSystem.Update();
            }

            using (FeedbackLifetimeMarker.Auto())
            {
                BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(_matchHudRuntimeFeedbackView, Time.unscaledTime);
            }

            _selectionUiCameraSystem?.UpdateZoomTransition();
            RefreshZoomControlsIfDue(now);
            _matchHudResourceHeaderPresentation.RefreshIfDue(now);
            ApplyMatchHudAssistantPanelReadModelIfDue(now);
            TickMatchHudThreatWarning(now);
        }

        public void NotifyStaticMinimapChanged()
        {
            _nextCompactMinimapUpdateTime = 0f;
            _matchHudMinimapInputSystem.NotifyStaticMapChanged();
            _matchHudFullMapInputSystem.NotifyStaticMapChanged();
        }

        public void BindMatchHudMinimap(MatchHudMinimapView minimapView)
        {
            if (_matchHudMinimapView != null)
                _matchHudMinimapView.FullMapOpenRequested -= RequestFullMapPopup;

            _matchHudMinimapView = minimapView;
            _nextCompactMinimapUpdateTime = 0f;
            _matchHudMinimapInputSystem.Bind(
                minimapView,
                _runtimeGameplayStateSystem,
                _selectionUiCameraSystem,
                _minimapDataSource,
                useFullMapProjection: false,
                showViewport: false,
                allowViewportDrag: false,
                allowMapFocus: false,
                allowZoom: false,
                openFullMapOnClick: true,
                useStableFullMapProjection: false);

            if (_matchHudMinimapView != null)
                _matchHudMinimapView.FullMapOpenRequested += RequestFullMapPopup;
        }

        public void BindMatchHudFullMapPopup(MatchHudFullMapPopupView popupView)
        {
            if (_matchHudFullMapPopupView != null)
                _matchHudFullMapPopupView.CloseRequested -= RequestFullMapClose;

            _matchHudFullMapInputSystem.Unbind();
            _matchHudFullMapPopupView = popupView;

            if (_matchHudFullMapPopupView == null)
                return;

            _matchHudFullMapPopupView.CloseRequested += RequestFullMapClose;
            _matchHudFullMapPopupView.Show();
            _matchHudFullMapInputSystem.Bind(
                _matchHudFullMapPopupView.Minimap,
                _runtimeGameplayStateSystem,
                _selectionUiCameraSystem,
                _minimapDataSource,
                useFullMapProjection: true,
                showViewport: true,
                allowViewportDrag: true,
                allowMapFocus: true,
                allowZoom: true,
                openFullMapOnClick: false);
            _matchHudFullMapInputSystem.NotifyStaticMapChanged();
            _matchHudFullMapInputSystem.Update();
        }

        public void BindMatchHudCommandControls(MatchOverlayCommandControlsView commandControlsView)
        {
            _matchHudCommandControlsView = commandControlsView;
        }

        public void BindMatchHudRightQuickRail(MatchHudRightQuickRailView rightQuickRailView)
        {
            _matchHudRightQuickRailView?.UnbindZoomControls();
            _matchHudRightQuickRailView = rightQuickRailView;
            _nextZoomControlRefreshTime = 0f;
            _matchHudRightQuickRailView?.BindZoomControls(
                RequestMatchHudZoomIn,
                RequestMatchHudZoomOut,
                ReadMatchHudZoomControlState);
        }

        public void ConfigureMatchHudSelectionPanelBinding(System.Action<IMatchHudSelectionPanelView> bindMatchHudSelectionPanel)
        {
            _bindMatchHudSelectionPanel = bindMatchHudSelectionPanel;
            if (_matchHudSelectionPanelView != null)
                _bindMatchHudSelectionPanel?.Invoke(_matchHudSelectionPanelView);
        }

        public void BindMatchHudSelectionPanel(MatchHudSelectionPanelView selectionPanelView)
        {
            _matchHudSelectionPanelView = selectionPanelView;
            _matchHudSelectionPanelView?.HideSelection();
            _bindMatchHudSelectionPanel?.Invoke(_matchHudSelectionPanelView);
        }

        public void ConfigureMatchHudRuntimeFeedbackSinkBinding(System.Action<IBattleHudRuntimeFeedbackSink> bindMatchHudRuntimeFeedback)
        {
            _bindMatchHudRuntimeFeedback = bindMatchHudRuntimeFeedback;
            if (_matchHudRuntimeFeedbackView != null)
                _bindMatchHudRuntimeFeedback?.Invoke(new BattleHudRuntimeFeedbackSink(_matchHudRuntimeFeedbackView, _gameTextResolver));
        }

        public void BindMatchHudRuntimeFeedback(BattleHudRuntimeFeedbackView runtimeFeedbackView)
        {
            _matchHudRuntimeFeedbackView = runtimeFeedbackView;
            _bindMatchHudRuntimeFeedback?.Invoke(new BattleHudRuntimeFeedbackSink(_matchHudRuntimeFeedbackView, _gameTextResolver));
        }

        public void ApplyMatchHudCommandMode(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(_matchHudRuntimeFeedbackView, mode, _gameTextResolver);
        }

        public void ClearMatchHudCommandMode()
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(_matchHudRuntimeFeedbackView, _gameTextResolver);
        }

        public void ConfigureMatchHudSquadTrayBinding(System.Action<IMatchHudSquadTrayView> bindMatchHudSquadTray)
        {
            _bindMatchHudSquadTray = bindMatchHudSquadTray;
            if (_matchHudSquadTrayView != null)
                _bindMatchHudSquadTray?.Invoke(_matchHudSquadTrayView);
        }

        public void BindMatchHudSquadTray(MatchHudSquadTrayView squadTrayView)
        {
            _matchHudSquadTrayView?.Unbind();
            _matchHudSquadTrayView = squadTrayView;
            _bindMatchHudSquadTray?.Invoke(_matchHudSquadTrayView);
        }

        public void BindMatchHudThreatJumpPanel(GameObject headerContent)
        {
            SetMatchHudThreatWarningVisible(false);
            _matchHudThreatJumpPanel = null;
            _matchHudThreatTitle = null;
            _matchHudThreatVisibleUntil = float.NegativeInfinity;
            _matchHudResourceHeaderPresentation.Clear();
            UnbindMatchHudResourceExchangeButtons();
            _matchHudHeaderReferences = null;

            if (headerContent == null)
                return;

            _matchHudHeaderReferences = MatchHudHeaderReferenceUiSystemHelper.Create(headerContent.transform);
            BindMatchHudResourceSlots(_matchHudHeaderReferences);

            if (_matchHudHeaderReferences.ThreatJumpPanel == null)
                return;

            _matchHudThreatJumpPanel = _matchHudHeaderReferences.ThreatJumpPanel.gameObject;
            _matchHudThreatTitle = _matchHudHeaderReferences.ThreatTitle;
            SetMatchHudThreatWarningVisible(false);
        }

        public void BindMatchHudAssistant(
            GameObject headerContent,
            RectTransform popupLayer,
            GameObject popupPrefab)
        {
            _matchHudAssistantUiSystem.Bind(
                headerContent,
                popupLayer,
                popupPrefab,
                CaptureGameplayUiClickSequence,
                PrepareToOpenMatchHudAssistant,
                NotifyMatchHudAssistantPanelOpenChanged);
            _nextAssistantPanelRefreshTime = 0f;
        }

        public void ConfigureLargeTacticalPopupCloseActions(
            System.Action closeBuildDrawerPopup,
            System.Action closeFullMapPopup,
            System.Action closeResourceExchangePopup)
        {
            _closeBuildDrawerPopup = closeBuildDrawerPopup;
            _closeFullMapPopup = closeFullMapPopup;
            _closeResourceExchangePopup = closeResourceExchangePopup;
        }

        public event System.Action<bool> MatchHudAssistantPanelOpenChanged;

        public bool TryCloseMatchHudAssistantForBack()
        {
            return _matchHudAssistantUiSystem.TryClosePanel();
        }

        internal void PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup popup)
        {
            if (popup != MatchHudLargeTacticalPopup.Assistant)
                _matchHudAssistantUiSystem.ClosePanelWithoutInputCapture();

            if (popup != MatchHudLargeTacticalPopup.BuildDrawer &&
                _buildDrawerView != null &&
                _buildDrawerView.IsOpen)
            {
                _closeBuildDrawerPopup?.Invoke();
            }

            if (popup != MatchHudLargeTacticalPopup.FullMap &&
                _matchHudFullMapPopupView != null &&
                _matchHudFullMapPopupView.IsOpen)
            {
                _closeFullMapPopup?.Invoke();
            }

            if (popup != MatchHudLargeTacticalPopup.ResourceExchange &&
                _resourceExchangePopupView != null &&
                _resourceExchangePopupView.IsOpen)
            {
                _closeResourceExchangePopup?.Invoke();
            }
        }

        private void PrepareToOpenMatchHudAssistant()
        {
            PrepareToOpenLargeTacticalPopup(MatchHudLargeTacticalPopup.Assistant);
        }

        private void NotifyMatchHudAssistantPanelOpenChanged(bool open)
        {
            UiShellRuntimeGateway.TrySetAssistantPanelOpen(open);
            MatchHudAssistantPanelOpenChanged?.Invoke(open);
        }

        private void BindMatchHudResourceSlots(MatchHudHeaderReferenceUiSystemHelper references)
        {
            if (references.ResourceStrip == null ||
                references.MaterialsSlot == null ||
                references.OilSlot == null ||
                references.FuelSlot == null)
                return;

            BindMatchHudResourceExchangeButtons(references);
            _matchHudResourceHeaderPresentation.Bind(
                references.OilSlot.Root.gameObject,
                references.MaterialsSlot.Label,
                references.MaterialsSlot.Value,
                references.OilSlot.Label,
                references.OilSlot.Value,
                references.FuelSlot.Label,
                references.FuelSlot.Value,
                references.CivilianRiskSlot?.Label,
                references.CivilianRiskSlot?.Value,
                Time.unscaledTime);
        }

        private void BindMatchHudResourceExchangeButtons(MatchHudHeaderReferenceUiSystemHelper references)
        {
            UnbindMatchHudResourceExchangeButtons();
            BindMatchHudResourceExchangeButton(references.ResourceStrip);
        }

        private void BindMatchHudResourceExchangeButton(Transform slot)
        {
            if (slot == null)
                return;

            Button button = slot.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[MainMenuPlayUI] {slot.name} is missing its authored Resource Exchange button.");
                return;
            }

            button.onClick.RemoveListener(RequestResourceExchangePopup);
            button.onClick.AddListener(RequestResourceExchangePopup);
            _matchHudResourceExchangeButtons.Add(button);
        }

        private void UnbindMatchHudResourceExchangeButtons()
        {
            for (int i = 0; i < _matchHudResourceExchangeButtons.Count; i++)
            {
                Button button = _matchHudResourceExchangeButtons[i];
                if (button != null)
                    button.onClick.RemoveListener(RequestResourceExchangePopup);
            }

            _matchHudResourceExchangeButtons.Clear();
        }

        private void ApplyMatchHudAssistantPanelReadModelIfDue(float now)
        {
            if (now < _nextAssistantPanelRefreshTime)
                return;

            _nextAssistantPanelRefreshTime = now + AssistantPanelRefreshIntervalSeconds;
            ApplyMatchHudAssistantPanelReadModel();
        }

        private void ApplyMatchHudAssistantPanelReadModel()
        {
            if (UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel assistantPanel))
                _matchHudAssistantUiSystem.ApplyReadModel(assistantPanel);

            if (UiShellRuntimeGateway.TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel assistantHighlight))
                _matchHudAssistantUiSystem.ApplyHighlightReadModel(assistantHighlight);
            else
                _matchHudAssistantUiSystem.ApplyHighlightReadModel(UiAssistantHighlightModel.Empty);
        }

        public bool TryShowMatchHudThreatWarning(string title, float visibleUntilTime)
        {
            if (_matchHudThreatJumpPanel == null || _matchHudThreatTitle == null)
                return false;

            string resolvedTitle = string.IsNullOrWhiteSpace(title) ? "Threat detected" : title;
            if (_matchHudThreatTitle.text != resolvedTitle)
                _matchHudThreatTitle.text = resolvedTitle;
            _matchHudThreatVisibleUntil = visibleUntilTime;
            SetMatchHudThreatWarningVisible(true);
            return true;
        }

        public void TickMatchHudThreatWarning(float now)
        {
            if (_matchHudThreatJumpPanel == null || !_matchHudThreatJumpPanel.activeSelf)
                return;

            if (now >= _matchHudThreatVisibleUntil)
                SetMatchHudThreatWarningVisible(false);
        }

        public void BindBuildDrawer(BuildDrawerView buildDrawerView)
        {
            _buildDrawerView = buildDrawerView;
        }

        public void BindBuildPlacementConfirmationBar(BuildPlacementConfirmationBarView buildPlacementConfirmationBarView)
        {
            _buildPlacementConfirmationBarView = buildPlacementConfirmationBarView;
        }

        public void BindResourceExchangePopup(ResourceExchangePopupView resourceExchangePopupView)
        {
            _resourceExchangePopupView = resourceExchangePopupView;
        }

        public bool IsBuildDrawerOpen => _buildDrawerView != null && _buildDrawerView.IsOpen;

        public event System.Action FullMapPopupRequested;
        public event System.Action FullMapPopupCloseRequested;

        public bool IsPointerOverAnyGameplayUi(Vector2 screenPosition, out string source)
        {
            if (_matchHudFullMapPopupView != null && _matchHudFullMapPopupView.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudFullMapPopup";
                return true;
            }

            if (_buildDrawerView != null && _buildDrawerView.ContainsScreenPoint(screenPosition))
            {
                source = "BuildDrawer";
                return true;
            }

            if (_buildPlacementConfirmationBarView != null &&
                _buildPlacementConfirmationBarView.ContainsScreenPoint(screenPosition))
            {
                source = "BuildPlacementConfirmationBar";
                return true;
            }

            if (_resourceExchangePopupView != null && _resourceExchangePopupView.ContainsScreenPoint(screenPosition))
            {
                source = "ResourceExchangePopup";
                return true;
            }

            if (_matchHudCommandControlsView != null && _matchHudCommandControlsView.ContainsScreenPoint(screenPosition))
            {
                source = $"MatchHudCommandControls:{_matchHudCommandControlsView.DescribeScreenPointHit(screenPosition)}";
                return true;
            }

            if (_matchHudRuntimeFeedbackView != null &&
                _matchHudRuntimeFeedbackView.ContainsFeedbackActionScreenPoint(screenPosition))
            {
                source = "MatchHudFeedbackActions";
                return true;
            }

            if (_matchHudRightQuickRailView != null && _matchHudRightQuickRailView.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudRightQuickRail";
                return true;
            }

            if (_matchHudAssistantUiSystem.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudAssistant";
                return true;
            }

            if (_matchHudMinimapView != null && _matchHudMinimapView.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudMinimap";
                return true;
            }

            if (_matchHudSelectionPanelView != null && _matchHudSelectionPanelView.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudSelectionPanel";
                return true;
            }

            if (_matchHudSquadTrayView != null && _matchHudSquadTrayView.ContainsScreenPoint(screenPosition))
            {
                source = "MatchHudSquadTray";
                return true;
            }

            source = null;
            return false;
        }

        public bool IsPointerOverPlacementUi(Vector2 screenPosition)
        {
            return IsPointerOverAnyGameplayUi(screenPosition, out _);
        }

        public bool IsPointerOverRaycastableUi(Vector2 screenPosition, out string source)
        {
            source = null;
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (IsCurrentEventSystemPointerOverUi(eventSystem, out source))
                return true;

            if (_raycastPointerData == null || _raycastEventSystem != eventSystem)
            {
                _raycastEventSystem = eventSystem;
                _raycastPointerData = new PointerEventData(eventSystem);
            }

            _raycastPointerData.Reset();
            _raycastPointerData.position = screenPosition;
            _raycastResults.Clear();
            eventSystem.RaycastAll(_raycastPointerData, _raycastResults);

            for (int i = 0; i < _raycastResults.Count; i++)
            {
                RaycastResult result = _raycastResults[i];
                if (result.gameObject == null || !result.gameObject.activeInHierarchy)
                    continue;

                if (result.module is not UnityEngine.UI.GraphicRaycaster)
                    continue;

                source = result.gameObject.name;
                return true;
            }

            return false;
        }

        private static bool IsCurrentEventSystemPointerOverUi(EventSystem eventSystem, out string source)
        {
            source = null;
            if (eventSystem == null)
                return false;

            if (TryGetPrimaryTouchPointerId(out int touchPointerId) &&
                eventSystem.IsPointerOverGameObject(touchPointerId))
            {
                source = "EventSystemPrimaryPointer";
                return true;
            }

            if (eventSystem.IsPointerOverGameObject())
            {
                source = "EventSystemCurrentPointer";
                return true;
            }

            return false;
        }

        private static bool TryGetPrimaryTouchPointerId(out int pointerId)
        {
            pointerId = -1;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return false;

            var touch = touchscreen.primaryTouch;
            bool active = touch.press.isPressed ||
                          touch.press.wasPressedThisFrame ||
                          touch.press.wasReleasedThisFrame;
            if (!active)
                return false;

            pointerId = touch.touchId.ReadValue();
            return true;
        }

        public bool IsPointerOverSelectionCancelUi(Vector2 screenPosition)
        {
            return false;
        }

        public bool IsPointerOverBuildToolMenu(Vector2 screenPosition)
        {
            return false;
        }

        public bool IsPointerOverZoomControls(Vector2 screenPosition)
        {
            return _matchHudRightQuickRailView != null && _matchHudRightQuickRailView.ContainsZoomScreenPoint(screenPosition);
        }

        public bool IsPointerOverUnitCommandUi(Vector2 screenPosition, out string source)
        {
            source = null;
            return false;
        }

        public bool ShouldIgnoreBuildingSelectionThisFrame()
        {
            return Time.frameCount <= _lastGameplayUiClickFrame + 1;
        }

        public void CaptureGameplayUiClickSequence()
        {
            _lastGameplayUiClickFrame = Time.frameCount;
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            if (_runtimeGameplayStateSystem != null)
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        }

        public bool CanTriggerSelectionModeFromHold()
        {
            return false;
        }

        public void TriggerSelectionModeFromHold()
        {
            if (_runtimeGameplayStateSystem == null)
                return;

            _runtimeGameplayStateSystem.SelectionModeActive = true;
            _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        }

        public void TriggerSelectionCancel()
        {
            _selectionUiCommandSystem?.RequestDeselectAll();
        }

        private void OnToolbarUiPointerDown(object evt)
        {
        }

        private void OnToolbarUiMouseDown(object evt)
        {
        }

        private void RequestFullMapPopup()
        {
            if (_runtimeGameplayStateSystem != null)
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;

            FullMapPopupRequested?.Invoke();
        }

        private void RequestFullMapClose()
        {
            if (_runtimeGameplayStateSystem != null)
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;

            FullMapPopupCloseRequested?.Invoke();
        }

        private void RequestResourceExchangePopup()
        {
            CaptureGameplayUiClickSequence();
            UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.OpenResourceExchange, 0);
        }

        private void RequestMatchHudZoomIn()
        {
            CaptureZoomUiClick();
            _selectionUiCameraSystem?.RequestZoomInLevel();
            RefreshZoomControlsNow(Time.unscaledTime);
        }

        private void RequestMatchHudZoomOut()
        {
            CaptureZoomUiClick();
            _selectionUiCameraSystem?.RequestZoomOutLevel();
            RefreshZoomControlsNow(Time.unscaledTime);
        }

        private void RefreshZoomControlsIfDue(float now)
        {
            if (now < _nextZoomControlRefreshTime)
                return;

            RefreshZoomControlsNow(now);
        }

        private void RefreshZoomControlsNow(float now)
        {
            _nextZoomControlRefreshTime = now + ZoomControlRefreshIntervalSeconds;
            _matchHudRightQuickRailView?.RefreshZoomControls();
        }

        private MatchHudZoomControlState ReadMatchHudZoomControlState()
        {
            return _selectionUiCameraSystem != null
                ? _selectionUiCameraSystem.ReadZoomControlState()
                : MatchHudZoomControlState.Disabled;
        }

        private void CaptureZoomUiClick()
        {
            CaptureGameplayUiClickSequence();
        }

        private void SetMatchHudThreatWarningVisible(bool visible)
        {
            if (_matchHudThreatJumpPanel != null && _matchHudThreatJumpPanel.activeSelf != visible)
                _matchHudThreatJumpPanel.SetActive(visible);
        }

    }
}
