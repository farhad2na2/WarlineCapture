using System;
using UnityEngine;

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
        public readonly Func<BuildingPlacementInputRuntimeTickSystem.Result> UpdateInput;
        public readonly Func<int> GetRuntimeBuildingCount;
        public readonly bool DiagnosticsEnabled;
        public readonly double DiagnosticsFreezeLogThresholdSeconds;

        public Source(
            BuildingProductionRuntimeTickSystem.Context productionContext,
            BuildingRuntimeBoundaryPublishSystem.Context boundaryContext,
            Action updateBuildingResourceVisuals,
            Action syncDestroyedRuntimeBuildingCombatEntities,
            Action updateDestroyedBuildings,
            Action updateRoadBarrierDoors,
            Action flushPendingMarkerRefresh,
            Func<BuildingPlacementInputRuntimeTickSystem.Result> updateInput,
            Func<int> getRuntimeBuildingCount,
            bool diagnosticsEnabled,
            double diagnosticsFreezeLogThresholdSeconds)
        {
            ProductionContext = productionContext;
            BoundaryContext = boundaryContext;
            UpdateBuildingResourceVisuals = updateBuildingResourceVisuals;
            SyncDestroyedRuntimeBuildingCombatEntities = syncDestroyedRuntimeBuildingCombatEntities;
            UpdateDestroyedBuildings = updateDestroyedBuildings;
            UpdateRoadBarrierDoors = updateRoadBarrierDoors;
            FlushPendingMarkerRefresh = flushPendingMarkerRefresh;
            UpdateInput = updateInput;
            GetRuntimeBuildingCount = getRuntimeBuildingCount;
            DiagnosticsEnabled = diagnosticsEnabled;
            DiagnosticsFreezeLogThresholdSeconds = diagnosticsFreezeLogThresholdSeconds;
        }
    }

    private readonly BuildingProductionRuntimeTickSystem _productionRuntimeTickSystem = new();
    private readonly BuildingRuntimeBoundaryPublishSystem _runtimeBoundaryPublishSystem = new();

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
            () => _runtimeBoundaryPublishSystem.Update(source.BoundaryContext),
            source.UpdateInput,
            source.GetRuntimeBuildingCount,
            source.DiagnosticsEnabled,
            source.DiagnosticsFreezeLogThresholdSeconds,
            Debug.Log);
    }
}
