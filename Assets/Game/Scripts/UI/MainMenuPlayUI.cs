using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class MainMenuPlayUI : IMatchRuntimeUi
    {
        private const float CompactMinimapUpdateIntervalSeconds = 0.1f;
        private const float HeaderResourceRefreshIntervalSeconds = 0.2f;
        private const float ZoomControlRefreshIntervalSeconds = 0.1f;
        private static readonly ProfilerMarker MinimapUpdateMarker = new("MainMenuPlayUI.MinimapUpdate");
        private static readonly ProfilerMarker FeedbackLifetimeMarker = new("MainMenuPlayUI.FeedbackLifetime");

        private readonly MatchHudMinimapInputUiSystemHelper _matchHudMinimapInputSystem = new();
        private readonly MatchHudMinimapInputUiSystemHelper _matchHudFullMapInputSystem = new();
        private readonly MatchHudAssistantUiSystemHelper _matchHudAssistantUiSystem = new();
        private IMatchRuntimeState _runtimeGameplayStateSystem;
        private ISelectionUiCommand _selectionUiCommandSystem;
        private IMatchHudCameraControl _selectionUiCameraSystem;
        private IMatchHudMinimapDataSource _minimapDataSource;
        private MatchOverlayCommandControlsView _matchHudCommandControlsView;
        private MatchHudRightQuickRailView _matchHudRightQuickRailView;
        private MatchHudMinimapView _matchHudMinimapView;
        private MatchHudFullMapPopupView _matchHudFullMapPopupView;
        private MatchHudSelectionPanelView _matchHudSelectionPanelView;
        private BattleHudRuntimeFeedbackView _matchHudRuntimeFeedbackView;
        private MatchHudSquadTrayView _matchHudSquadTrayView;
        private GameObject _matchHudThreatJumpPanel;
        private TMP_Text _matchHudThreatTitle;
        private float _matchHudThreatVisibleUntil = float.NegativeInfinity;
        private GameObject _matchHudOilSlotRoot;
        private TMP_Text _matchHudOilSlotLabel;
        private TMP_Text _matchHudOilSlotValue;
        private TMP_Text _matchHudFuelSlotLabel;
        private TMP_Text _matchHudFuelSlotValue;
        private string _lastMatchHudOilText;
        private string _lastMatchHudFuelText;
        private bool _lastMatchHudShowOil;
        private bool _matchHudOilVisibilityApplied;
        private bool _matchHudResourceLabelsApplied;
        private float _nextCompactMinimapUpdateTime;
        private float _nextHeaderResourceRefreshTime;
        private float _nextZoomControlRefreshTime;
        private BuildDrawerView _buildDrawerView;
        private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
        private System.Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
        private System.Action<IBattleHudRuntimeFeedbackSink> _bindMatchHudRuntimeFeedback;
        private System.Action<IMatchHudSquadTrayView> _bindMatchHudSquadTray;
        private EventSystem _raycastEventSystem;
        private PointerEventData _raycastPointerData;
        private readonly List<RaycastResult> _raycastResults = new(16);
        private int _lastGameplayUiClickFrame = -1000;

        public void Init(
            ISelectionUiCommand selectionUiCommandSystem,
            IMatchRuntimeState runtimeGameplayStateSystem,
            IMatchHudCameraControl selectionUiCameraSystem = null,
            IMatchHudMinimapDataSource minimapDataSource = null,
            bool resetRuntimeState = true)
        {
            _selectionUiCommandSystem = selectionUiCommandSystem;
            _runtimeGameplayStateSystem = runtimeGameplayStateSystem;
            _selectionUiCameraSystem = selectionUiCameraSystem;
            _minimapDataSource = minimapDataSource;

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
            BindMatchHudThreatJumpPanel(null);
            _buildDrawerView = null;
            _buildPlacementConfirmationBarView = null;
            _bindMatchHudSelectionPanel = null;
            _bindMatchHudRuntimeFeedback = null;
            _bindMatchHudSquadTray = null;
            _selectionUiCommandSystem = null;
            _runtimeGameplayStateSystem = null;
            _selectionUiCameraSystem = null;
            _minimapDataSource = null;
        }

        public void Update()
        {
            float now = Time.unscaledTime;
            using (MinimapUpdateMarker.Auto())
            {
                if (now >= _nextCompactMinimapUpdateTime)
                {
                    _nextCompactMinimapUpdateTime = now + CompactMinimapUpdateIntervalSeconds;
                    _matchHudMinimapInputSystem.Update();
                }

                if (_matchHudFullMapPopupView != null && _matchHudFullMapPopupView.IsOpen)
                    _matchHudFullMapInputSystem.Update();
            }

            using (FeedbackLifetimeMarker.Auto())
            {
                BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(_matchHudRuntimeFeedbackView, Time.unscaledTime);
            }

            _selectionUiCameraSystem?.UpdateZoomTransition();
            RefreshZoomControlsIfDue(now);
            ApplyMatchHudHeaderResourceStateIfDue(now);
            ApplyMatchHudAssistantPanelReadModel();
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
                _bindMatchHudRuntimeFeedback?.Invoke(new BattleHudRuntimeFeedbackSink(_matchHudRuntimeFeedbackView));
        }

        public void BindMatchHudRuntimeFeedback(BattleHudRuntimeFeedbackView runtimeFeedbackView)
        {
            _matchHudRuntimeFeedbackView = runtimeFeedbackView;
            _bindMatchHudRuntimeFeedback?.Invoke(new BattleHudRuntimeFeedbackSink(_matchHudRuntimeFeedbackView));
        }

        public void ApplyMatchHudCommandMode(TacticalCommandMode mode)
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(_matchHudRuntimeFeedbackView, mode);
        }

        public void ClearMatchHudCommandMode()
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(_matchHudRuntimeFeedbackView);
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
            _matchHudOilSlotRoot = null;
            _matchHudOilSlotLabel = null;
            _matchHudOilSlotValue = null;
            _matchHudFuelSlotLabel = null;
            _matchHudFuelSlotValue = null;
            _lastMatchHudOilText = null;
            _lastMatchHudFuelText = null;
            _lastMatchHudShowOil = false;
            _matchHudOilVisibilityApplied = false;
            _matchHudResourceLabelsApplied = false;
            _nextHeaderResourceRefreshTime = 0f;

            if (headerContent == null)
                return;

            BindMatchHudResourceSlots(headerContent);

            Transform panelTransform = headerContent.transform.Find("ThreatJumpPanel");
            panelTransform ??= headerContent.transform.Find("HeaderContent/ThreatJumpPanel");
            if (panelTransform == null)
                return;

            _matchHudThreatJumpPanel = panelTransform.gameObject;
            Transform titleTransform = panelTransform.Find("Title");
            _matchHudThreatTitle = titleTransform != null
                ? titleTransform.GetComponent<TMP_Text>()
                : panelTransform.GetComponentInChildren<TMP_Text>(true);
            SetMatchHudThreatWarningVisible(false);
        }

        public void BindMatchHudAssistant(GameObject headerContent, RectTransform overlayRoot)
        {
            _matchHudAssistantUiSystem.Bind(headerContent, overlayRoot, CaptureGameplayUiClickSequence);
        }

        private void BindMatchHudResourceSlots(GameObject headerContent)
        {
            Transform resourceStrip = headerContent.transform.Find("ResourceStrip");
            resourceStrip ??= headerContent.transform.Find("HeaderContent/ResourceStrip");
            if (resourceStrip == null)
                return;

            Transform fuelSlot = resourceStrip.Find("FuelSlot");
            if (fuelSlot == null)
                return;

            Transform oilSlot = resourceStrip.Find("OilSlot");
            if (oilSlot == null)
                oilSlot = CreateOilResourceSlot(resourceStrip, fuelSlot);

            ArrangeMatchHudResourceSlots(resourceStrip);
            _matchHudOilSlotRoot = oilSlot.gameObject;
            BindMatchHudResourceSlot(oilSlot, out _matchHudOilSlotLabel, out _matchHudOilSlotValue);
            BindMatchHudResourceSlot(fuelSlot, out _matchHudFuelSlotLabel, out _matchHudFuelSlotValue);
            ApplyMatchHudHeaderResourceState();
            _nextHeaderResourceRefreshTime = Time.unscaledTime + HeaderResourceRefreshIntervalSeconds;
        }

        private static Transform CreateOilResourceSlot(Transform resourceStrip, Transform fuelSlot)
        {
            GameObject oilSlot = UnityEngine.Object.Instantiate(fuelSlot.gameObject, resourceStrip);
            oilSlot.name = "OilSlot";
            oilSlot.transform.SetSiblingIndex(fuelSlot.GetSiblingIndex());
            return oilSlot.transform;
        }

        private static void ArrangeMatchHudResourceSlots(Transform resourceStrip)
        {
            SetResourceSlotLayout(resourceStrip.Find("CreditsSlot"), -640f);
            SetResourceSlotLayout(resourceStrip.Find("OilSlot"), -320f);
            SetResourceSlotLayout(resourceStrip.Find("FuelSlot"), 0f);
            SetResourceSlotLayout(resourceStrip.Find("SupplySlot"), 320f);
            SetResourceSlotLayout(resourceStrip.Find("CivilianRiskSlot"), 640f);
        }

        private static void SetResourceSlotLayout(Transform slot, float x)
        {
            if (slot == null || !slot.TryGetComponent(out RectTransform rectTransform))
                return;

            Vector2 anchoredPosition = new(x, rectTransform.anchoredPosition.y);
            if (!Mathf.Approximately(rectTransform.anchoredPosition.x, anchoredPosition.x) ||
                !Mathf.Approximately(rectTransform.anchoredPosition.y, anchoredPosition.y))
            {
                rectTransform.anchoredPosition = anchoredPosition;
            }

            Vector2 sizeDelta = new(300f, rectTransform.sizeDelta.y);
            if (!Mathf.Approximately(rectTransform.sizeDelta.x, sizeDelta.x) ||
                !Mathf.Approximately(rectTransform.sizeDelta.y, sizeDelta.y))
            {
                rectTransform.sizeDelta = sizeDelta;
            }
        }

        private static void BindMatchHudResourceSlot(Transform slot, out TMP_Text labelText, out TMP_Text valueText)
        {
            labelText = null;
            valueText = null;
            if (slot == null)
                return;

            Transform label = slot.Find("Label");
            Transform value = slot.Find("Value");
            labelText = label != null ? label.GetComponent<TMP_Text>() : null;
            valueText = value != null ? value.GetComponent<TMP_Text>() : null;
        }

        private void ApplyMatchHudHeaderResourceState()
        {
            if (_matchHudOilSlotValue == null && _matchHudFuelSlotValue == null)
                return;

            if (!UiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header))
                return;

            if (_matchHudOilSlotRoot != null &&
                (!_matchHudOilVisibilityApplied || _lastMatchHudShowOil != header.ShowOil))
            {
                _matchHudOilSlotRoot.SetActive(header.ShowOil);
                _lastMatchHudShowOil = header.ShowOil;
                _matchHudOilVisibilityApplied = true;
            }

            if (header.ShowOil)
            {
                if (!_matchHudResourceLabelsApplied && _matchHudOilSlotLabel != null && _matchHudOilSlotLabel.text != "Oil")
                    _matchHudOilSlotLabel.text = "Oil";
                string oilText = string.IsNullOrWhiteSpace(header.OilText) ? "0" : header.OilText;
                if (_matchHudOilSlotValue != null && _lastMatchHudOilText != oilText)
                {
                    _matchHudOilSlotValue.text = oilText;
                    _lastMatchHudOilText = oilText;
                }
            }

            if (!_matchHudResourceLabelsApplied && _matchHudFuelSlotLabel != null && _matchHudFuelSlotLabel.text != "Fuel")
                _matchHudFuelSlotLabel.text = "Fuel";
            string fuelText = string.IsNullOrWhiteSpace(header.FuelText) ? "0" : header.FuelText;
            if (_matchHudFuelSlotValue != null && _lastMatchHudFuelText != fuelText)
            {
                _matchHudFuelSlotValue.text = fuelText;
                _lastMatchHudFuelText = fuelText;
            }

            _matchHudResourceLabelsApplied = true;
        }

        private void ApplyMatchHudHeaderResourceStateIfDue(float now)
        {
            if (_matchHudResourceLabelsApplied && now < _nextHeaderResourceRefreshTime)
                return;

            _nextHeaderResourceRefreshTime = now + HeaderResourceRefreshIntervalSeconds;
            ApplyMatchHudHeaderResourceState();
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
