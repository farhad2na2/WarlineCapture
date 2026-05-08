using UnityEngine;

public sealed class MainMenuPlayUI
{
    public static MainMenuPlayUI Instance { get; private set; }

    private RTSSelectionSystem _selectionController;

    public void Init(
        RoadBuildSystem roadBuildController,
        BuildingPlacementSystem buildingPlacementController,
        RTSSelectionSystem selectionController,
        DayNightSystem dayNightSystem)
    {
        if (Instance != null && Instance != this)
            Instance.Dispose();

        Instance = this;
        _selectionController = selectionController;
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
    }

    public void Dispose()
    {
        if (Instance == this)
            Instance = null;

        _selectionController = null;
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
        InitialUnitsRuntimeState.SelectionModeActive = true;
        InitialUnitsRuntimeState.SuppressNextWorldClick = true;
    }

    public void TriggerSelectionCancel()
    {
        _selectionController?.DeselectAllUnits("MainMenuPlayUI.TriggerSelectionCancel");
    }

    private void OnToolbarUiPointerDown(object evt)
    {
    }

    private void OnToolbarUiMouseDown(object evt)
    {
    }
}
