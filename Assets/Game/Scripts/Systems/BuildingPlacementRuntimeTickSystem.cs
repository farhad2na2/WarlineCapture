using System;
using Unity.Profiling;
using UnityEngine;

internal sealed class BuildingPlacementRuntimeTickSystem
{
    private const double ProductionIntervalSeconds = 0.1d;
    private const double ResourceProductionIntervalSeconds = 1d;
    private const double ResourceHaulerIntervalSeconds = 0.25d;
    private const double ResourceVisualIntervalSeconds = 0.25d;
    private const double ReservationCleanupIntervalSeconds = 0.5d;
    private const double DestroyedCleanupIntervalSeconds = 0.5d;
    private static readonly ProfilerMarker ProcessPendingProductionsMarker = new("BuildingPlacementRuntimeTick.ProcessPendingProductions");
    private static readonly ProfilerMarker UpdateActiveProductionTransportsMarker = new("BuildingPlacementRuntimeTick.UpdateActiveProductionTransports");
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
    private double _nextProductionAt;
    private double _nextResourceProductionAt;
    private double _nextResourceHaulerAt;
    private double _nextResourceVisualAt;
    private double _nextReservationCleanupAt;
    private double _nextDestroyedCleanupAt;

    public readonly struct Context
    {
        public readonly Action ProcessPendingProductions;
        public readonly Action UpdateActiveProductionTransports;
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
            Action updateActiveProductionTransports,
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
            UpdateActiveProductionTransports = updateActiveProductionTransports;
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

    public void UpdateStartup(Context context)
    {
        double startTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double afterMapPlacements = startTime;
        double afterBoundary = startTime;
        try
        {
            using (UpdateBuildingRuntimeBoundaryMarker.Auto())
            {
                context.UpdateBuildingRuntimeBoundary?.Invoke();
            }

            afterBoundary = UnityEngine.Time.realtimeSinceStartupAsDouble;

            using (EnqueueMapBuildingPlacementsMarker.Auto())
            {
                context.EnqueueMapBuildingPlacements?.Invoke();
            }

            using (EnqueueMapVehiclePlacementsMarker.Auto())
            {
                context.EnqueueMapVehiclePlacements?.Invoke();
            }

            afterMapPlacements = UnityEngine.Time.realtimeSinceStartupAsDouble;

            using (UpdateBuildingRuntimeBoundaryMarker.Auto())
            {
                context.UpdateBuildingRuntimeBoundary?.Invoke();
            }

            afterBoundary = UnityEngine.Time.realtimeSinceStartupAsDouble;
        }
        finally
        {
            context.DiagnosticsSystem?.LogIfSlow(
                context.DiagnosticsContext,
                new BuildingPlacementRuntimeTickDiagnosticsSystem.Timing(
                    startTime,
                    afterMapPlacements,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary,
                    afterBoundary));
        }
    }

    public void Update(Context context)
    {
        UpdateSimulation(context);
    }

    public void UpdateSimulation(Context context)
    {
        double startTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double afterMapPlacements = startTime;
        double afterBoundary = startTime;
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
            using (UpdateBuildingRuntimeBoundaryMarker.Auto())
            {
                context.UpdateBuildingRuntimeBoundary?.Invoke();
            }

            afterBoundary = UnityEngine.Time.realtimeSinceStartupAsDouble;
            double now = afterBoundary;
            if (now >= _nextProductionAt)
            {
                _nextProductionAt = now + ProductionIntervalSeconds;
                using (ProcessPendingProductionsMarker.Auto())
                {
                    context.ProcessPendingProductions?.Invoke();
                }
            }
            afterProductions = UnityEngine.Time.realtimeSinceStartupAsDouble;

            using (UpdateActiveProductionTransportsMarker.Auto())
            {
                context.UpdateActiveProductionTransports?.Invoke();
            }
            afterProductions = UnityEngine.Time.realtimeSinceStartupAsDouble;

            now = afterProductions;
            if (now >= _nextResourceProductionAt)
            {
                _nextResourceProductionAt = now + ResourceProductionIntervalSeconds;
                using (UpdateResourceProductionMarker.Auto())
                {
                    context.UpdateResourceProduction?.Invoke();
                }
            }
            afterResources = UnityEngine.Time.realtimeSinceStartupAsDouble;

            now = afterResources;
            if (now >= _nextResourceHaulerAt)
            {
                _nextResourceHaulerAt = now + ResourceHaulerIntervalSeconds;
                using (UpdateResourceHaulersMarker.Auto())
                {
                    context.UpdateResourceHaulers?.Invoke();
                }
            }
            afterHaulers = UnityEngine.Time.realtimeSinceStartupAsDouble;

            now = afterHaulers;
            if (now >= _nextResourceVisualAt)
            {
                _nextResourceVisualAt = now + ResourceVisualIntervalSeconds;
                using (UpdateBuildingResourceVisualsMarker.Auto())
                {
                    context.UpdateBuildingResourceVisuals?.Invoke();
                }
            }
            afterResourceVisuals = UnityEngine.Time.realtimeSinceStartupAsDouble;

            now = afterResourceVisuals;
            if (now >= _nextReservationCleanupAt)
            {
                _nextReservationCleanupAt = now + ReservationCleanupIntervalSeconds;
                using (CleanupRecentSpawnReservationsMarker.Auto())
                {
                    context.CleanupRecentSpawnReservations?.Invoke();
                }
            }
            afterReservations = UnityEngine.Time.realtimeSinceStartupAsDouble;

            now = afterReservations;
            if (now >= _nextDestroyedCleanupAt)
            {
                _nextDestroyedCleanupAt = now + DestroyedCleanupIntervalSeconds;
                using (SyncDestroyedRuntimeBuildingCombatEntitiesMarker.Auto())
                {
                    context.SyncDestroyedRuntimeBuildingCombatEntities?.Invoke();
                }

                using (UpdateDestroyedBuildingsMarker.Auto())
                {
                    context.UpdateDestroyedBuildings?.Invoke();
                }
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
                    afterMapPlacements,
                    afterBoundary,
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
