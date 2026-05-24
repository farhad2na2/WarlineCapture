using UnityEngine;

internal sealed class BuildingPlacementRuntimeTickContextSystem
{
    private readonly BuildingProductionRuntimeTickSystem _productionRuntimeTickSystem = new();
    private readonly BuildingRuntimeBoundaryPublishSystem _runtimeBoundaryPublishSystem = new();

    public BuildingPlacementRuntimeTickSystem.Context Create(BuildingPlacementSystem placement)
    {
        BuildingProductionRuntimeTickSystem.Context productionContext = CreateProductionContext(placement);
        BuildingRuntimeBoundaryPublishSystem.Context boundaryContext = CreateBoundaryContext(placement);
        return new BuildingPlacementRuntimeTickSystem.Context(
            () => _productionRuntimeTickSystem.ProcessPendingProductions(productionContext),
            () => _productionRuntimeTickSystem.UpdateResourceProduction(productionContext),
            () => _productionRuntimeTickSystem.UpdateResourceHaulers(productionContext),
            placement.UpdateBuildingResourceVisuals,
            () => _productionRuntimeTickSystem.CleanupRecentSpawnReservations(productionContext),
            placement.SyncDestroyedRuntimeBuildingCombatEntities,
            placement.UpdateDestroyedBuildings,
            placement.UpdateRoadBarrierDoors,
            placement.FlushPendingMarkerRefresh,
            () => _runtimeBoundaryPublishSystem.Update(boundaryContext),
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

    private static BuildingProductionRuntimeTickSystem.Context CreateProductionContext(BuildingPlacementSystem placement)
    {
        return new BuildingProductionRuntimeTickSystem.Context(
            placement.RuntimeBuildings,
            placement.DayNightSystem,
            placement.FactionResourceSystem,
            placement.ProductionUpdateSystem,
            placement.CreateBuildingProductionUpdateContext(),
            placement.ResourceHaulerBridgeSystem,
            placement.CreateBuildingResourceHaulerBridgeContext(),
            placement.BuildingSpawnSystem,
            () => placement.BuildingSpawnRandomState,
            value => placement.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            placement.OilBarrelsPerFuelBarrelRatio);
    }

    private static BuildingRuntimeBoundaryPublishSystem.Context CreateBoundaryContext(BuildingPlacementSystem placement)
    {
        return new BuildingRuntimeBoundaryPublishSystem.Context(
            placement.TryGetEntityManagerForRuntimeTick,
            placement.EnsureEntityQueries,
            placement.RuntimeBoundarySystem,
            placement.DefinitionSystem,
            placement.RuntimeSpawnSystem,
            placement.CreateBuildingRuntimeSpawnContext(),
            placement.ProductionRequestSystem,
            placement.CreateBuildingProductionRequestContext(),
            placement.RuntimeQuerySystem,
            placement.CreateBuildingRuntimeQueryContext(),
            placement.FactionResourceSystem,
            () => placement.RuntimeBoundaryQuery,
            placement.RuntimeBuildings);
    }
}
