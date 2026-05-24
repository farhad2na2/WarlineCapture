using System;
using UnityEngine;

internal sealed class BuildingPlacementRuntimeTickSystem
{
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
            UpdateBuildingRuntimeBoundary = updateBuildingRuntimeBoundary;
            UpdateInput = updateInput;
            DiagnosticsSystem = diagnosticsSystem;
            DiagnosticsContext = diagnosticsContext;
        }
    }

    public void Update(Context context)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
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
            context.ProcessPendingProductions?.Invoke();
            afterProductions = Time.realtimeSinceStartupAsDouble;
            context.UpdateResourceProduction?.Invoke();
            afterResources = Time.realtimeSinceStartupAsDouble;
            context.UpdateResourceHaulers?.Invoke();
            afterHaulers = Time.realtimeSinceStartupAsDouble;
            context.UpdateBuildingResourceVisuals?.Invoke();
            afterResourceVisuals = Time.realtimeSinceStartupAsDouble;
            context.CleanupRecentSpawnReservations?.Invoke();
            afterReservations = Time.realtimeSinceStartupAsDouble;
            context.SyncDestroyedRuntimeBuildingCombatEntities?.Invoke();
            context.UpdateDestroyedBuildings?.Invoke();
            afterDestroyed = Time.realtimeSinceStartupAsDouble;
            context.UpdateRoadBarrierDoors?.Invoke();
            afterDoors = Time.realtimeSinceStartupAsDouble;
            context.FlushPendingMarkerRefresh?.Invoke();
            afterMarkers = Time.realtimeSinceStartupAsDouble;
            context.UpdateBuildingRuntimeBoundary?.Invoke();

            BuildingPlacementInputRuntimeTickSystem.Result input = context.UpdateInput != null
                ? context.UpdateInput()
                : default;
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
