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
        public readonly Func<Camera> GetWorldCamera;
        public readonly Func<BuildingPlacementLifecycleSystem.PlacementState> GetActivePlacement;
        public readonly Action<BuildingPlacementLifecycleSystem.PlacementState, GamePointerState> UpdateActivePlacementPointer;
        public readonly Func<bool> IsPlayRequested;
        public readonly Func<bool> IsBuildModeActive;
        public readonly Action HidePlacementOutline;
        public readonly Func<bool> ShouldIgnoreBuildingSelectionThisFrame;
        public readonly Func<Vector2, bool> IsPointerOverAnyGameplayUi;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<Vector2, bool> IsPointerOverUnitCommandUi;
        public readonly Action SuppressNextWorldClick;
        public readonly Action<Vector2> HandleBuildingSelectionClick;
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
            Func<Camera> getWorldCamera,
            Func<BuildingPlacementLifecycleSystem.PlacementState> getActivePlacement,
            Action<BuildingPlacementLifecycleSystem.PlacementState, GamePointerState> updateActivePlacementPointer,
            Func<bool> isPlayRequested,
            Func<bool> isBuildModeActive,
            Action hidePlacementOutline,
            Func<bool> shouldIgnoreBuildingSelectionThisFrame,
            Func<Vector2, bool> isPointerOverAnyGameplayUi,
            Func<bool> hasActiveBuilding,
            Func<Vector2, bool> isPointerOverUnitCommandUi,
            Action suppressNextWorldClick,
            Action<Vector2> handleBuildingSelectionClick,
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
            GetWorldCamera = getWorldCamera;
            GetActivePlacement = getActivePlacement;
            UpdateActivePlacementPointer = updateActivePlacementPointer;
            IsPlayRequested = isPlayRequested;
            IsBuildModeActive = isBuildModeActive;
            HidePlacementOutline = hidePlacementOutline;
            ShouldIgnoreBuildingSelectionThisFrame = shouldIgnoreBuildingSelectionThisFrame;
            IsPointerOverAnyGameplayUi = isPointerOverAnyGameplayUi;
            HasActiveBuilding = hasActiveBuilding;
            IsPointerOverUnitCommandUi = isPointerOverUnitCommandUi;
            SuppressNextWorldClick = suppressNextWorldClick;
            HandleBuildingSelectionClick = handleBuildingSelectionClick;
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
            source.GetWorldCamera,
            source.GetActivePlacement,
            source.UpdateActivePlacementPointer,
            source.IsPlayRequested,
            source.IsBuildModeActive,
            source.HidePlacementOutline,
            source.ShouldIgnoreBuildingSelectionThisFrame,
            source.IsPointerOverAnyGameplayUi,
            source.HasActiveBuilding,
            source.IsPointerOverUnitCommandUi,
            source.SuppressNextWorldClick,
            source.HandleBuildingSelectionClick,
            source.GetRuntimeBuildingCount,
            source.DiagnosticsEnabled,
            source.DiagnosticsFreezeLogThresholdSeconds,
            Debug.Log);
    }
}
