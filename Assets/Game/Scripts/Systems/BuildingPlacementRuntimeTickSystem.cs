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
        public readonly Func<int> GetRuntimeBuildingCount;
        public readonly bool DiagnosticsEnabled;
        public readonly double FreezeLogThresholdSeconds;
        public readonly Action<string> Log;

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
            Func<int> getRuntimeBuildingCount,
            bool diagnosticsEnabled,
            double freezeLogThresholdSeconds,
            Action<string> log)
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
            GetRuntimeBuildingCount = getRuntimeBuildingCount;
            DiagnosticsEnabled = diagnosticsEnabled;
            FreezeLogThresholdSeconds = freezeLogThresholdSeconds;
            Log = log;
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
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (context.DiagnosticsEnabled && elapsed >= context.FreezeLogThresholdSeconds)
            {
                if (afterProductions < startTime) afterProductions = startTime;
                if (afterResources < afterProductions) afterResources = afterProductions;
                if (afterHaulers < afterResources) afterHaulers = afterResources;
                if (afterResourceVisuals < afterHaulers) afterResourceVisuals = afterHaulers;
                if (afterReservations < afterResourceVisuals) afterReservations = afterResourceVisuals;
                if (afterDestroyed < afterReservations) afterDestroyed = afterReservations;
                if (afterDoors < afterDestroyed) afterDoors = afterDestroyed;
                if (afterMarkers < afterDoors) afterMarkers = afterDoors;
                if (afterInputOutline < afterMarkers) afterInputOutline = afterMarkers;
                if (afterInputMouse < afterInputOutline) afterInputMouse = afterInputOutline;
                if (afterInputUi < afterInputMouse) afterInputUi = afterInputMouse;
                if (afterInputBuildingClick < afterInputUi) afterInputBuildingClick = afterInputUi;
                if (afterInput < afterInputBuildingClick) afterInput = afterInputBuildingClick;

                context.Log?.Invoke(
                    $"[BuildingPlacementDiag] frame={Time.frameCount} total={elapsed * 1000d:F1}ms " +
                    $"productions={(afterProductions - startTime) * 1000d:F1}ms " +
                    $"resources={(afterResources - afterProductions) * 1000d:F1}ms " +
                    $"haulers={(afterHaulers - afterResources) * 1000d:F1}ms " +
                    $"resourceVisuals={(afterResourceVisuals - afterHaulers) * 1000d:F1}ms " +
                    $"reservations={(afterReservations - afterResourceVisuals) * 1000d:F1}ms " +
                    $"destroyed={(afterDestroyed - afterReservations) * 1000d:F1}ms " +
                    $"doors={(afterDoors - afterDestroyed) * 1000d:F1}ms " +
                    $"markers={(afterMarkers - afterDoors) * 1000d:F1}ms " +
                    $"input={(afterInput - afterMarkers) * 1000d:F1}ms " +
                    $"inputOutline={(afterInputOutline - afterMarkers) * 1000d:F1}ms " +
                    $"inputMouse={(afterInputMouse - afterInputOutline) * 1000d:F1}ms " +
                    $"inputUi={(afterInputUi - afterInputMouse) * 1000d:F1}ms " +
                    $"inputBuilding={(afterInputBuildingClick - afterInputUi) * 1000d:F1}ms " +
                    $"buildings={context.GetRuntimeBuildingCount?.Invoke() ?? 0}");
            }
        }
    }
}
