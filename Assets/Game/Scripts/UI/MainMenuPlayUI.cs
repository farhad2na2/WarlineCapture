using UnityEngine;

public sealed class MainMenuPlayUI
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly MatchHudMinimapInputSystem _matchHudMinimapInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private SelectionUiCameraSystem _selectionUiCameraSystem;
    private MatchOverlayCommandControlsView _matchHudCommandControlsView;
    private MatchHudRightQuickRailView _matchHudRightQuickRailView;
    private MatchHudMinimapView _matchHudMinimapView;
    private MatchHudSelectionPanelView _matchHudSelectionPanelView;
    private BattleHudRuntimeFeedbackView _matchHudRuntimeFeedbackView;
    private MatchHudSquadTrayView _matchHudSquadTrayView;
    private BuildDrawerView _buildDrawerView;
    private BuildPlacementConfirmationBarView _buildPlacementConfirmationBarView;
    private System.Action<MatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
    private System.Action<BattleHudRuntimeFeedbackView> _bindMatchHudRuntimeFeedback;
    private System.Action<MatchHudSquadTrayView> _bindMatchHudSquadTray;

    public void Init(
        SelectionUiCommandSystem selectionUiCommandSystem,
        DayNightSystem dayNightSystem,
        SelectionUiCameraSystem selectionUiCameraSystem = null,
        bool resetRuntimeState = true)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _selectionUiCameraSystem = selectionUiCameraSystem;

        if (!resetRuntimeState)
            return;

        _runtimeGameplayStateSystem.PlayRequested = false;
        _runtimeGameplayStateSystem.SelectionModeActive = false;
        _runtimeGameplayStateSystem.BuildModeActive = false;
        _runtimeGameplayStateSystem.ZoomInHeld = false;
        _runtimeGameplayStateSystem.ZoomOutHeld = false;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
    }

    public void Dispose()
    {
        _matchHudMinimapInputSystem.Dispose();
        _matchHudCommandControlsView = null;
        _matchHudRightQuickRailView = null;
        _matchHudMinimapView = null;
        _matchHudSelectionPanelView = null;
        _matchHudRuntimeFeedbackView = null;
        _matchHudSquadTrayView?.Unbind();
        _matchHudSquadTrayView = null;
        _buildDrawerView = null;
        _buildPlacementConfirmationBarView = null;
        _bindMatchHudSelectionPanel = null;
        _bindMatchHudRuntimeFeedback = null;
        _bindMatchHudSquadTray = null;
        _selectionUiCommandSystem = null;
        _selectionUiCameraSystem = null;
    }

    public void Update()
    {
        _matchHudMinimapInputSystem.Update();
    }

    public void NotifyStaticMinimapChanged()
    {
        _matchHudMinimapInputSystem.NotifyStaticMapChanged();
    }

    public void BindMatchHudMinimap(MatchHudMinimapView minimapView)
    {
        _matchHudMinimapView = minimapView;
        _matchHudMinimapInputSystem.Bind(
            minimapView,
            _runtimeGameplayStateSystem,
            _selectionUiCameraSystem);
    }

    public void BindMatchHudCommandControls(MatchOverlayCommandControlsView commandControlsView)
    {
        _matchHudCommandControlsView = commandControlsView;
    }

    public void BindMatchHudRightQuickRail(MatchHudRightQuickRailView rightQuickRailView)
    {
        _matchHudRightQuickRailView = rightQuickRailView;
    }

    public void ConfigureMatchHudSelectionPanelBinding(System.Action<MatchHudSelectionPanelView> bindMatchHudSelectionPanel)
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

    public void ConfigureMatchHudRuntimeFeedbackBinding(System.Action<BattleHudRuntimeFeedbackView> bindMatchHudRuntimeFeedback)
    {
        _bindMatchHudRuntimeFeedback = bindMatchHudRuntimeFeedback;
        if (_matchHudRuntimeFeedbackView != null)
            _bindMatchHudRuntimeFeedback?.Invoke(_matchHudRuntimeFeedbackView);
    }

    public void BindMatchHudRuntimeFeedback(BattleHudRuntimeFeedbackView runtimeFeedbackView)
    {
        _matchHudRuntimeFeedbackView = runtimeFeedbackView;
        _bindMatchHudRuntimeFeedback?.Invoke(_matchHudRuntimeFeedbackView);
    }

    public void ApplyMatchHudCommandMode(TacticalCommandMode mode)
    {
        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(_matchHudRuntimeFeedbackView, mode);
    }

    public void ClearMatchHudCommandMode()
    {
        BattleHudRuntimeFeedbackSystem.ClearCommandMode(_matchHudRuntimeFeedbackView);
    }

    public void ConfigureMatchHudSquadTrayBinding(System.Action<MatchHudSquadTrayView> bindMatchHudSquadTray)
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

    public void BindBuildDrawer(BuildDrawerView buildDrawerView)
    {
        _buildDrawerView = buildDrawerView;
    }

    public void BindBuildPlacementConfirmationBar(BuildPlacementConfirmationBarView buildPlacementConfirmationBarView)
    {
        _buildPlacementConfirmationBarView = buildPlacementConfirmationBarView;
    }

    public bool IsBuildDrawerOpen => _buildDrawerView != null && _buildDrawerView.IsOpen;

    public bool IsPointerOverAnyGameplayUi(Vector2 screenPosition, out string source)
    {
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
            source = "MatchHudCommandControls";
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
        return false;
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
}
