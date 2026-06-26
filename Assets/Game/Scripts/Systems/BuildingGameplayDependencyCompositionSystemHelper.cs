using System;
using UnityEngine;

internal sealed class BuildingGameplayDependencyCompositionSystemHelper
{
    internal IMatchRuntimeUi MainMenuPlayUi { get; private set; }
    internal SelectionUiCameraSystemHelper SelectionUiCameraSystemHelper { get; private set; }
    internal SelectionBuildingInteractionSystem SelectionBuildingInteractionSystem { get; private set; }
    internal RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers { get; private set; }
    internal RuntimeCityCompositionSystemHelper RuntimeCitySystem { get; private set; }
    internal CitizenPopulationEventCompositionSystemHelper CitizenPopulationEventCompositionSystemHelper { get; private set; }
    internal FactionVisualSettings FactionVisualSettings { get; private set; }
    private Func<bool> ShouldBlockBuildingSelectionClick { get; set; }
    internal float BuildingFactionTintStrength => FactionVisualSettings != null ? FactionVisualSettings.BuildingFactionTintStrength : 0.45f;
    internal DayNightSystem DayNightSystem { get; private set; }

    internal void SetStartupDependencies(
        IMatchRuntimeUi mainMenuPlayUi,
        FactionVisualSettings factionVisualSettings,
        DayNightSystem dayNightSystem)
    {
        MainMenuPlayUi = mainMenuPlayUi;
        FactionVisualSettings = factionVisualSettings;
        DayNightSystem = dayNightSystem;
    }

    internal void BindRuntimeDependencies(
        IMatchRuntimeUi mainMenuPlayUi,
        DayNightSystem dayNightSystem = null,
        SelectionUiCameraSystemHelper selectionUiCameraSystem = null,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem = null,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers = null,
        RuntimeCityCompositionSystemHelper runtimeCitySystem = null,
        CitizenPopulationEventCompositionSystemHelper citizenPopulationEventSystem = null,
        Func<bool> shouldBlockBuildingSelectionClick = null)
    {
        MainMenuPlayUi = mainMenuPlayUi;
        if (dayNightSystem != null)
            DayNightSystem = dayNightSystem;
        if (selectionUiCameraSystem != null)
            SelectionUiCameraSystemHelper = selectionUiCameraSystem;
        if (selectionBuildingInteractionSystem != null)
            SelectionBuildingInteractionSystem = selectionBuildingInteractionSystem;
        if (runtimeGridBlockers != null)
            RuntimeGridBlockers = runtimeGridBlockers;
        if (runtimeCitySystem != null)
            RuntimeCitySystem = runtimeCitySystem;
        if (citizenPopulationEventSystem != null)
            CitizenPopulationEventCompositionSystemHelper = citizenPopulationEventSystem;
        if (shouldBlockBuildingSelectionClick != null)
            ShouldBlockBuildingSelectionClick = shouldBlockBuildingSelectionClick;
    }

    internal bool IsBuildingSelectionClickBlocked()
    {
        return ShouldBlockBuildingSelectionClick?.Invoke() == true;
    }

    internal bool IsRuntimeBlockerCell(int x, int y, int width, int height)
    {
        return RuntimeGridBlockers != null &&
               RuntimeGridBlockers.IsRuntimeBlockerCell(x, y, width, height);
    }

    internal void RemoveBlockersOverlappingFootprint(Vector2Int originCell, Vector2Int footprintCells)
    {
        RuntimeGridBlockers?.RemoveBlockersOverlappingFootprint(originCell, footprintCells);
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
        SelectionUiCameraSystemHelper?.SmoothMoveCameraGroundCenterTo(worldPosition);
    }

    internal void FollowCameraGroundCenterTo(Vector3 worldPosition)
    {
        SelectionUiCameraSystemHelper?.FollowCameraGroundCenterTo(worldPosition);
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

    internal bool TryRequestMoveOrderToBuilding(Vector2Int originCell, Vector2Int footprintCells)
    {
        return SelectionBuildingInteractionSystem != null &&
               SelectionBuildingInteractionSystem.TryRequestMoveOrderToBuilding(originCell, footprintCells);
    }

    internal void NotifyHomeBuildingDestroyed(int buildingId)
    {
        CitizenPopulationEventCompositionSystemHelper?.NotifyHomeBuildingDestroyed(buildingId);
    }
}
