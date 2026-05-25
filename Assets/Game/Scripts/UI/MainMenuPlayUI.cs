using UnityEngine;

public sealed class MainMenuPlayUI
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private SelectionUiCommandSystem _selectionUiCommandSystem;

    public void Init(
        RoadBuildSystem roadBuildController,
        SelectionUiCommandSystem selectionUiCommandSystem,
        DayNightSystem dayNightSystem)
    {
        _selectionUiCommandSystem = selectionUiCommandSystem;
        _runtimeGameplayStateSystem.PlayRequested = false;
        _runtimeGameplayStateSystem.SelectionModeActive = false;
        _runtimeGameplayStateSystem.BuildModeActive = false;
        _runtimeGameplayStateSystem.ZoomInHeld = false;
        _runtimeGameplayStateSystem.ZoomOutHeld = false;
        _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
    }

    public void Dispose()
    {
        _selectionUiCommandSystem = null;
    }

    public void Update()
    {
    }

    public void NotifyStaticMinimapChanged()
    {
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
