using System;

internal sealed class BuildingPlacementRuntimeTickContextSystem
{
    public readonly struct Source
    {
        public readonly BuildingProductionRuntimeTickSystem.Context ProductionContext;
        public readonly BuildingRuntimeBoundaryPublishSystem.Context BoundaryContext;
        public readonly Action UpdateBuildingResourceVisuals;
        public readonly Action SyncDestroyedRuntimeBuildingCombatEntities;
        public readonly Action UpdateDestroyedBuildings;
        public readonly Action UpdateRoadBarrierDoors;
        public readonly Action FlushPendingMarkerRefresh;
        public readonly Action EnqueueMapBuildingPlacements;
        public readonly Action EnqueueMapVehiclePlacements;
        public readonly Func<BuildingPlacementInputRuntimeTickSystem.Result> UpdateInput;
        public readonly BuildingPlacementRuntimeTickDiagnosticsSystem.Context DiagnosticsContext;

        public Source(
            BuildingProductionRuntimeTickSystem.Context productionContext,
            BuildingRuntimeBoundaryPublishSystem.Context boundaryContext,
            Action updateBuildingResourceVisuals,
            Action syncDestroyedRuntimeBuildingCombatEntities,
            Action updateDestroyedBuildings,
            Action updateRoadBarrierDoors,
            Action flushPendingMarkerRefresh,
            Action enqueueMapBuildingPlacements,
            Action enqueueMapVehiclePlacements,
            Func<BuildingPlacementInputRuntimeTickSystem.Result> updateInput,
            BuildingPlacementRuntimeTickDiagnosticsSystem.Context diagnosticsContext)
        {
            ProductionContext = productionContext;
            BoundaryContext = boundaryContext;
            UpdateBuildingResourceVisuals = updateBuildingResourceVisuals;
            SyncDestroyedRuntimeBuildingCombatEntities = syncDestroyedRuntimeBuildingCombatEntities;
            UpdateDestroyedBuildings = updateDestroyedBuildings;
            UpdateRoadBarrierDoors = updateRoadBarrierDoors;
            FlushPendingMarkerRefresh = flushPendingMarkerRefresh;
            EnqueueMapBuildingPlacements = enqueueMapBuildingPlacements;
            EnqueueMapVehiclePlacements = enqueueMapVehiclePlacements;
            UpdateInput = updateInput;
            DiagnosticsContext = diagnosticsContext;
        }
    }

    private readonly BuildingProductionRuntimeTickSystem _productionRuntimeTickSystem = new();
    private readonly BuildingRuntimeBoundaryPublishSystem _runtimeBoundaryPublishSystem = new();
    private readonly BuildingPlacementRuntimeTickDiagnosticsSystem _diagnosticsSystem = new();

    public BuildingPlacementRuntimeTickSystem.Context Create(Source source)
    {
        return new BuildingPlacementRuntimeTickSystem.Context(
            () => _productionRuntimeTickSystem.ProcessPendingProductions(source.ProductionContext),
            () => _productionRuntimeTickSystem.UpdateResourceProduction(source.ProductionContext),
            () => _productionRuntimeTickSystem.UpdateResourceHaulers(source.ProductionContext),
            source.UpdateBuildingResourceVisuals,
            () => _productionRuntimeTickSystem.CleanupRecentSpawnReservations(source.ProductionContext),
            source.SyncDestroyedRuntimeBuildingCombatEntities,
            source.UpdateDestroyedBuildings,
            source.UpdateRoadBarrierDoors,
            source.FlushPendingMarkerRefresh,
            source.EnqueueMapBuildingPlacements,
            source.EnqueueMapVehiclePlacements,
            () => _runtimeBoundaryPublishSystem.Update(source.BoundaryContext),
            source.UpdateInput,
            _diagnosticsSystem,
            source.DiagnosticsContext);
    }
}
