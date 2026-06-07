using UnityEngine;

public sealed class MainMenuPlayUI
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly MatchHudMinimapInputSystem _matchHudMinimapInputSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;
    private SelectionUiCameraSystem _selectionUiCameraSystem;
    private MatchHudSquadTrayView _matchHudSquadTrayView;
    private System.Action<MatchHudSquadTrayView> _bindMatchHudSquadTray;

    public void Init(
        SelectionUiCommandSystem selectionUiCommandSystem,
        DayNightSystem dayNightSystem,
        SelectionUiCameraSystem selectionUiCameraSystem = null)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _selectionUiCameraSystem = selectionUiCameraSystem;
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
        _matchHudSquadTrayView?.Unbind();
        _matchHudSquadTrayView = null;
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
        _matchHudMinimapInputSystem.Bind(
            minimapView,
            _runtimeGameplayStateSystem,
            _selectionUiCameraSystem);
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

    public bool IsPointerOverAnyGameplayUi(Vector2 screenPosition, out string source)
    {
        source = null;
        return false;
    }

    public bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
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
