using System;
using Unity.Entities;

internal sealed class BuildingPlacementRuntimeTickContextCompositionSystemHelper
{
    public readonly struct Source
    {
        public readonly BuildingProductionRuntimeTickCompositionSystemHelper.Context ProductionContext;
        public readonly BuildingRuntimePublishCompositionSystemHelper.Context BoundaryContext;
        public readonly Action UpdateBuildingResourceVisuals;
        public readonly Action SyncDestroyedRuntimeBuildingCombatEntities;
        public readonly Action UpdateDestroyedBuildings;
        public readonly Action UpdateRoadBarrierDoors;
        public readonly Action FlushPendingMarkerRefresh;
        public readonly Action EnqueueMapBuildingPlacements;
        public readonly Action EnqueueMapVehiclePlacements;
        public readonly Func<BuildingPlacementInputRuntimeTickUiSystemHelper.Result> UpdateInput;
        public readonly BuildingPlacementRuntimeTickDiagnosticsSystemHelper.Context DiagnosticsContext;

        public Source(
            BuildingProductionRuntimeTickCompositionSystemHelper.Context productionContext,
            BuildingRuntimePublishCompositionSystemHelper.Context boundaryContext,
            Action updateBuildingResourceVisuals,
            Action syncDestroyedRuntimeBuildingCombatEntities,
            Action updateDestroyedBuildings,
            Action updateRoadBarrierDoors,
            Action flushPendingMarkerRefresh,
            Action enqueueMapBuildingPlacements,
            Action enqueueMapVehiclePlacements,
            Func<BuildingPlacementInputRuntimeTickUiSystemHelper.Result> updateInput,
            BuildingPlacementRuntimeTickDiagnosticsSystemHelper.Context diagnosticsContext)
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

    private readonly BuildingProductionRuntimeTickCompositionSystemHelper _productionRuntimeTickSystem = new();
    private readonly BuildingRuntimePublishCompositionSystemHelper _runtimeBoundaryPublishSystem = new();
    private readonly BuildingPlacementRuntimeTickDiagnosticsSystemHelper _diagnosticsSystem = new();

    public BuildingPlacementRuntimeTickCompositionSystemHelper.Context Create(Source source)
    {
        return new BuildingPlacementRuntimeTickCompositionSystemHelper.Context(
            () => _productionRuntimeTickSystem.ProcessPendingProductions(source.ProductionContext),
            () => _productionRuntimeTickSystem.UpdateActiveProductionTransports(source.ProductionContext),
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
