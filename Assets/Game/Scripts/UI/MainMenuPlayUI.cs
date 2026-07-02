using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class MainMenuPlayUI : IMatchRuntimeUi
    {
        private static readonly ProfilerMarker MinimapUpdateMarker = new("MainMenuPlayUI.MinimapUpdate");
        private static readonly ProfilerMarker FeedbackLifetimeMarker = new("MainMenuPlayUI.FeedbackLifetime");

        private readonly MatchHudMinimapInputUiSystemHelper _matchHudMinimapInputSystem = new();
        private readonly MatchHudMinimapInputUiSystemHelper _matchHudFullMapInputSystem = new();
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
        private TMP_Text _matchHudOilSlotLabel;
        private TMP_Text _matchHudOilSlotValue;
        private TMP_Text _matchHudFuelSlotLabel;
        private TMP_Text _matchHudFuelSlotValue;
        private BuildDrawerView _buildDrawerView;
        private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
        private System.Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
        private System.Action<IBattleHudRuntimeFeedbackSink> _bindMatchHudRuntimeFeedback;
        private System.Action<IMatchHudSquadTrayView> _bindMatchHudSquadTray;

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
            using (MinimapUpdateMarker.Auto())
            {
                _matchHudMinimapInputSystem.Update();
                if (_matchHudFullMapPopupView != null && _matchHudFullMapPopupView.IsOpen)
                    _matchHudFullMapInputSystem.Update();
            }

            using (FeedbackLifetimeMarker.Auto())
            {
                BattleHudRuntimeFeedbackUiSystemHelper.TickFeedbackLifetime(_matchHudRuntimeFeedbackView, Time.unscaledTime);
            }

            _selectionUiCameraSystem?.UpdateZoomTransition();
            _matchHudRightQuickRailView?.RefreshZoomControls();
            ApplyMatchHudHeaderResourceState();
            TickMatchHudThreatWarning(Time.unscaledTime);
        }

        public void NotifyStaticMinimapChanged()
        {
            _matchHudMinimapInputSystem.NotifyStaticMapChanged();
            _matchHudFullMapInputSystem.NotifyStaticMapChanged();
        }

        public void BindMatchHudMinimap(MatchHudMinimapView minimapView)
        {
            if (_matchHudMinimapView != null)
                _matchHudMinimapView.FullMapOpenRequested -= RequestFullMapPopup;

            _matchHudMinimapView = minimapView;
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
                useStableFullMapProjection: true);

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
            _matchHudOilSlotLabel = null;
            _matchHudOilSlotValue = null;
            _matchHudFuelSlotLabel = null;
            _matchHudFuelSlotValue = null;

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
            BindMatchHudResourceSlot(oilSlot, out _matchHudOilSlotLabel, out _matchHudOilSlotValue);
            BindMatchHudResourceSlot(fuelSlot, out _matchHudFuelSlotLabel, out _matchHudFuelSlotValue);
            ApplyMatchHudHeaderResourceState();
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

            rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
            rectTransform.sizeDelta = new Vector2(300f, rectTransform.sizeDelta.y);
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

            if (_matchHudOilSlotLabel != null)
                _matchHudOilSlotLabel.text = "Oil";
            if (_matchHudOilSlotValue != null)
                _matchHudOilSlotValue.text = string.IsNullOrWhiteSpace(header.OilText)
                    ? "0"
                    : header.OilText;

            if (_matchHudFuelSlotLabel != null)
                _matchHudFuelSlotLabel.text = "Fuel";
            if (_matchHudFuelSlotValue != null)
                _matchHudFuelSlotValue.text = string.IsNullOrWhiteSpace(header.FuelText)
                    ? "0"
                    : header.FuelText;
        }

        public bool TryShowMatchHudThreatWarning(string title, float visibleUntilTime)
        {
            if (_matchHudThreatJumpPanel == null || _matchHudThreatTitle == null)
                return false;

            _matchHudThreatTitle.text = string.IsNullOrWhiteSpace(title) ? "Threat detected" : title;
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

            var pointerData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult result = results[i];
                if (result.gameObject == null || !result.gameObject.activeInHierarchy)
                    continue;

                if (result.module is not UnityEngine.UI.GraphicRaycaster)
                    continue;

                source = result.gameObject.name;
                return true;
            }

            return false;
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
            return false;
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
            _matchHudRightQuickRailView?.RefreshZoomControls();
        }

        private void RequestMatchHudZoomOut()
        {
            CaptureZoomUiClick();
            _selectionUiCameraSystem?.RequestZoomOutLevel();
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
            _selectionUiCommandSystem?.CaptureUiClickSequence();
            if (_runtimeGameplayStateSystem != null)
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        }

        private void SetMatchHudThreatWarningVisible(bool visible)
        {
            if (_matchHudThreatJumpPanel != null && _matchHudThreatJumpPanel.activeSelf != visible)
                _matchHudThreatJumpPanel.SetActive(visible);
        }
    }
}
