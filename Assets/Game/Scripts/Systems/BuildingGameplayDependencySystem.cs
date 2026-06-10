using UnityEngine;

internal sealed class BuildingGameplayDependencySystem
{
    internal MainMenuPlayUI MainMenuPlayUi { get; private set; }
    internal SelectionUiCameraSystem SelectionUiCameraSystem { get; private set; }
    internal SelectionBuildingInteractionSystem SelectionBuildingInteractionSystem { get; private set; }
    internal RuntimeGridBlockerSystem RuntimeGridBlockerSystem { get; private set; }
    internal RuntimeCityCompositionSystem RuntimeCitySystem { get; private set; }
    internal CitizenPopulationEventSystem CitizenPopulationEventSystem { get; private set; }
    internal FactionVisualSettings FactionVisualSettings { get; private set; }
    internal float BuildingFactionTintStrength => FactionVisualSettings != null ? FactionVisualSettings.BuildingFactionTintStrength : 0.45f;
    internal DayNightSystem DayNightSystem { get; private set; }

    internal void SetStartupDependencies(
        MainMenuPlayUI mainMenuPlayUi,
        FactionVisualSettings factionVisualSettings,
        DayNightSystem dayNightSystem)
    {
        MainMenuPlayUi = mainMenuPlayUi;
        FactionVisualSettings = factionVisualSettings;
        DayNightSystem = dayNightSystem;
    }

    internal void BindRuntimeDependencies(
        MainMenuPlayUI mainMenuPlayUi,
        DayNightSystem dayNightSystem = null,
        SelectionUiCameraSystem selectionUiCameraSystem = null,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem = null,
        RuntimeGridBlockerSystem runtimeGridBlockerSystem = null,
        RuntimeCityCompositionSystem runtimeCitySystem = null,
        CitizenPopulationEventSystem citizenPopulationEventSystem = null)
    {
        MainMenuPlayUi = mainMenuPlayUi;
        if (dayNightSystem != null)
            DayNightSystem = dayNightSystem;
        if (selectionUiCameraSystem != null)
            SelectionUiCameraSystem = selectionUiCameraSystem;
        if (selectionBuildingInteractionSystem != null)
            SelectionBuildingInteractionSystem = selectionBuildingInteractionSystem;
        if (runtimeGridBlockerSystem != null)
            RuntimeGridBlockerSystem = runtimeGridBlockerSystem;
        if (runtimeCitySystem != null)
            RuntimeCitySystem = runtimeCitySystem;
        if (citizenPopulationEventSystem != null)
            CitizenPopulationEventSystem = citizenPopulationEventSystem;
    }

    internal bool IsRuntimeBlockerCell(int x, int y, int width, int height)
    {
        return RuntimeGridBlockerSystem != null &&
               RuntimeGridBlockerSystem.IsRuntimeBlockerCell(x, y, width, height);
    }

    internal void RemoveBlockersOverlappingFootprint(Vector2Int originCell, Vector2Int footprintCells)
    {
        RuntimeGridBlockerSystem?.RemoveBlockersOverlappingFootprint(originCell, footprintCells);
    }

    internal bool IsConfiguredHousePrefab(GameObject prefab)
    {
        return RuntimeCitySystem != null &&
               prefab != null &&
               RuntimeCitySystem.IsConfiguredHousePrefab(prefab);
    }

    internal void NotifyStaticMinimapChanged()
    {
        MainMenuPlayUi?.NotifyStaticMinimapChanged();
    }

    internal void ApplyBuildCommandMode()
    {
        MainMenuPlayUi?.ApplyMatchHudCommandMode(TacticalCommandMode.Build);
    }

    internal void ClearCommandMode()
    {
        MainMenuPlayUi?.ClearMatchHudCommandMode();
    }

    internal bool IsPointerOverPlacementUi(Vector2 screenPosition)
    {
        return MainMenuPlayUi != null &&
               MainMenuPlayUi.IsPointerOverPlacementUi(screenPosition);
    }

    internal bool IsBuildDrawerOpen()
    {
        return MainMenuPlayUi != null && MainMenuPlayUi.IsBuildDrawerOpen;
    }

    internal void SmoothMoveCameraGroundCenterTo(Vector3 worldPosition)
    {
        SelectionUiCameraSystem?.SmoothMoveCameraGroundCenterTo(worldPosition);
    }

    internal void FollowCameraGroundCenterTo(Vector3 worldPosition)
    {
        SelectionUiCameraSystem?.FollowCameraGroundCenterTo(worldPosition);
    }

    internal void ClearFocusedUnit()
    {
        SelectionBuildingInteractionSystem?.ClearFocusedUnit();
    }

    internal void ShowHudSelection(Sprite portraitSprite)
    {
        SelectionBuildingInteractionSystem?.ApplyBuildingSelectionHudFeedback(portraitSprite);
    }

    internal bool IsBoardablePlayerTransportClick(Vector2 screenPosition)
    {
        return SelectionBuildingInteractionSystem != null &&
               SelectionBuildingInteractionSystem.IsBoardablePlayerTransportClick(screenPosition);
    }

    internal bool TryIssueMoveOrderToBuilding(Vector2Int originCell, Vector2Int footprintCells)
    {
        return SelectionBuildingInteractionSystem != null &&
               SelectionBuildingInteractionSystem.TryIssueMoveOrderToBuilding(originCell, footprintCells);
    }

    internal void NotifyHomeBuildingDestroyed(int buildingId)
    {
        CitizenPopulationEventSystem?.NotifyHomeBuildingDestroyed(buildingId);
    }
}
