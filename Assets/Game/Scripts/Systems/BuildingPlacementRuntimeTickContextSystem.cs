using UnityEngine;

internal sealed class BuildingPlacementRuntimeTickContextSystem
{
    public BuildingPlacementRuntimeTickSystem.Context Create(BuildingPlacementSystem placement)
    {
        return new BuildingPlacementRuntimeTickSystem.Context(
            placement.ProcessPendingProductions,
            placement.UpdateResourceProduction,
            placement.UpdateResourceHaulers,
            placement.UpdateBuildingResourceVisuals,
            placement.CleanupRecentSpawnReservations,
            placement.SyncDestroyedRuntimeBuildingCombatEntities,
            placement.UpdateDestroyedBuildings,
            placement.UpdateRoadBarrierDoors,
            placement.FlushPendingMarkerRefresh,
            placement.UpdateBuildingRuntimeBoundary,
            () => placement.WorldCamera,
            () => placement.ActivePlacement,
            placement.UpdateActivePlacementPointer,
            () => placement.PlayRequested,
            () => placement.BuildModeActive,
            placement.HidePlacementOutline,
            placement.ShouldIgnoreBuildingSelectionThisFrame,
            placement.IsPointerOverAnyGameplayUi,
            () => placement.HasActiveBuilding,
            placement.IsPointerOverUnitCommandUi,
            placement.SuppressNextWorldClick,
            placement.HandleBuildingSelectionClick,
            () => placement.RuntimeBuildingCount,
            placement.DiagnosticsEnabled,
            placement.DiagnosticsFreezeLogThresholdSeconds,
            Debug.Log);
    }
}
