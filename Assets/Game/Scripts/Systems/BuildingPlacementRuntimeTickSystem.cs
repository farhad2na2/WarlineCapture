using System;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

internal sealed partial class BuildingPlacementRuntimeTickSystem : SystemBase
{
    private static readonly ProfilerMarker ProcessPendingProductionsMarker = new("BuildingPlacementRuntimeTick.ProcessPendingProductions");
    private static readonly ProfilerMarker UpdateResourceProductionMarker = new("BuildingPlacementRuntimeTick.UpdateResourceProduction");
    private static readonly ProfilerMarker UpdateResourceHaulersMarker = new("BuildingPlacementRuntimeTick.UpdateResourceHaulers");
    private static readonly ProfilerMarker UpdateBuildingResourceVisualsMarker = new("BuildingPlacementRuntimeTick.UpdateBuildingResourceVisuals");
    private static readonly ProfilerMarker CleanupRecentSpawnReservationsMarker = new("BuildingPlacementRuntimeTick.CleanupRecentSpawnReservations");
    private static readonly ProfilerMarker SyncDestroyedRuntimeBuildingCombatEntitiesMarker = new("BuildingPlacementRuntimeTick.SyncDestroyedRuntimeBuildingCombatEntities");
    private static readonly ProfilerMarker UpdateDestroyedBuildingsMarker = new("BuildingPlacementRuntimeTick.UpdateDestroyedBuildings");
    private static readonly ProfilerMarker UpdateRoadBarrierDoorsMarker = new("BuildingPlacementRuntimeTick.UpdateRoadBarrierDoors");
    private static readonly ProfilerMarker FlushPendingMarkerRefreshMarker = new("BuildingPlacementRuntimeTick.FlushPendingMarkerRefresh");
    private static readonly ProfilerMarker EnqueueMapBuildingPlacementsMarker = new("BuildingPlacementRuntimeTick.EnqueueMapBuildingPlacements");
    private static readonly ProfilerMarker EnqueueMapVehiclePlacementsMarker = new("BuildingPlacementRuntimeTick.EnqueueMapVehiclePlacements");
    private static readonly ProfilerMarker UpdateBuildingRuntimeBoundaryMarker = new("BuildingPlacementRuntimeTick.UpdateBuildingRuntimeBoundary");
    private static readonly ProfilerMarker UpdateInputMarker = new("BuildingPlacementRuntimeTick.UpdateInput");

    public readonly struct Context
    {
        public readonly Action ProcessPendingProductions;
        public readonly Action UpdateResourceProduction;
        public readonly Action UpdateResourceHaulers;
        public readonly Action UpdateBuildingResourceVisuals;
        public readonly Action CleanupRecentSpawnReservations;
        public readonly Action SyncDestroyedRuntimeBuildingCombatEntities;
        public readonly Action UpdateDestroyedBuildings;
        public readonly Action UpdateRoadBarrierDoors;
        public readonly Action FlushPendingMarkerRefresh;
        public readonly Action EnqueueMapBuildingPlacements;
        public readonly Action EnqueueMapVehiclePlacements;
        public readonly Action UpdateBuildingRuntimeBoundary;
        public readonly Func<BuildingPlacementInputRuntimeTickSystem.Result> UpdateInput;
        public readonly BuildingPlacementRuntimeTickDiagnosticsSystem DiagnosticsSystem;
        public readonly BuildingPlacementRuntimeTickDiagnosticsSystem.Context DiagnosticsContext;

        public Context(
            Action processPendingProductions,
            Action updateResourceProduction,
            Action updateResourceHaulers,
            Action updateBuildingResourceVisuals,
            Action cleanupRecentSpawnReservations,
            Action syncDestroyedRuntimeBuildingCombatEntities,
            Action updateDestroyedBuildings,
            Action updateRoadBarrierDoors,
            Action flushPendingMarkerRefresh,
            Action enqueueMapBuildingPlacements,
            Action enqueueMapVehiclePlacements,
            Action updateBuildingRuntimeBoundary,
            Func<BuildingPlacementInputRuntimeTickSystem.Result> updateInput,
            BuildingPlacementRuntimeTickDiagnosticsSystem diagnosticsSystem,
            BuildingPlacementRuntimeTickDiagnosticsSystem.Context diagnosticsContext)
        {
            ProcessPendingProductions = processPendingProductions;
            UpdateResourceProduction = updateResourceProduction;
            UpdateResourceHaulers = updateResourceHaulers;
            UpdateBuildingResourceVisuals = updateBuildingResourceVisuals;
            CleanupRecentSpawnReservations = cleanupRecentSpawnReservations;
            SyncDestroyedRuntimeBuildingCombatEntities = syncDestroyedRuntimeBuildingCombatEntities;
            UpdateDestroyedBuildings = updateDestroyedBuildings;
            UpdateRoadBarrierDoors = updateRoadBarrierDoors;
            FlushPendingMarkerRefresh = flushPendingMarkerRefresh;
            EnqueueMapBuildingPlacements = enqueueMapBuildingPlacements;
            EnqueueMapVehiclePlacements = enqueueMapVehiclePlacements;
            UpdateBuildingRuntimeBoundary = updateBuildingRuntimeBoundary;
            UpdateInput = updateInput;
            DiagnosticsSystem = diagnosticsSystem;
            DiagnosticsContext = diagnosticsContext;
        }
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void Update(Context context)
    {
        double startTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double afterProductions = startTime;
        double afterResources = startTime;
        double afterHaulers = startTime;
        double afterResourceVisuals = startTime;
        double afterReservations = startTime;
        double afterDestroyed = startTime;
        double afterDoors = startTime;
        double afterMarkers = startTime;
        double afterInputOutline = startTime;
        double afterInputMouse = startTime;
        double afterInputUi = startTime;
        double afterInputBuildingClick = startTime;
        double afterInput = startTime;
        try
        {
            using (ProcessPendingProductionsMarker.Auto())
            {
                context.ProcessPendingProductions?.Invoke();
            }
            afterProductions = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (UpdateResourceProductionMarker.Auto())
            {
                context.UpdateResourceProduction?.Invoke();
            }
            afterResources = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (UpdateResourceHaulersMarker.Auto())
            {
                context.UpdateResourceHaulers?.Invoke();
            }
            afterHaulers = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (UpdateBuildingResourceVisualsMarker.Auto())
            {
                context.UpdateBuildingResourceVisuals?.Invoke();
            }
            afterResourceVisuals = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (CleanupRecentSpawnReservationsMarker.Auto())
            {
                context.CleanupRecentSpawnReservations?.Invoke();
            }
            afterReservations = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (SyncDestroyedRuntimeBuildingCombatEntitiesMarker.Auto())
            {
                context.SyncDestroyedRuntimeBuildingCombatEntities?.Invoke();
            }
            using (UpdateDestroyedBuildingsMarker.Auto())
            {
                context.UpdateDestroyedBuildings?.Invoke();
            }
            afterDestroyed = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (UpdateRoadBarrierDoorsMarker.Auto())
            {
                context.UpdateRoadBarrierDoors?.Invoke();
            }
            afterDoors = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (FlushPendingMarkerRefreshMarker.Auto())
            {
                context.FlushPendingMarkerRefresh?.Invoke();
            }
            afterMarkers = UnityEngine.Time.realtimeSinceStartupAsDouble;
            using (EnqueueMapBuildingPlacementsMarker.Auto())
            {
                context.EnqueueMapBuildingPlacements?.Invoke();
            }
            using (EnqueueMapVehiclePlacementsMarker.Auto())
            {
                context.EnqueueMapVehiclePlacements?.Invoke();
            }
            using (UpdateBuildingRuntimeBoundaryMarker.Auto())
            {
                context.UpdateBuildingRuntimeBoundary?.Invoke();
            }

            BuildingPlacementInputRuntimeTickSystem.Result input;
            using (UpdateInputMarker.Auto())
            {
                input = context.UpdateInput != null
                    ? context.UpdateInput()
                    : default;
            }
            afterInputOutline = input.AfterOutline;
            afterInputMouse = input.AfterMouse;
            afterInputUi = input.AfterUi;
            afterInputBuildingClick = input.AfterBuildingClick;
            afterInput = input.AfterInput;
        }
        finally
        {
            context.DiagnosticsSystem?.LogIfSlow(
                context.DiagnosticsContext,
                new BuildingPlacementRuntimeTickDiagnosticsSystem.Timing(
                    startTime,
                    afterProductions,
                    afterResources,
                    afterHaulers,
                    afterResourceVisuals,
                    afterReservations,
                    afterDestroyed,
                    afterDoors,
                    afterMarkers,
                    afterInputOutline,
                    afterInputMouse,
                    afterInputUi,
                    afterInputBuildingClick,
                    afterInput));
        }
    }
}
