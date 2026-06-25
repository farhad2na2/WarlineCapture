using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingProductionRuntimeTickCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Dictionary<int, RuntimeBuildingEntity> RuntimeBuildingMap;
        public readonly DayNightSystem DayNightSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingProductionUpdateCompositionSystemHelper ProductionUpdateSystem;
        public readonly BuildingProductionUpdateCompositionSystemHelper.Context ProductionUpdateContext;
        public readonly BuildingResourceHaulerBridgeCompositionSystemHelper ResourceHaulerBridgeSystem;
        public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.Context ResourceHaulerBridgeContext;
        public readonly BuildingSpawnSystem SpawnSystem;
        public readonly Func<uint> GetRandomState;
        public readonly Action<uint> SetRandomState;
        public readonly Action<float> RecordOilExtracted;
        public readonly Action<float> RecordFuelProduced;
        public readonly Func<bool> HasPendingPathJob;
        public readonly float OilBarrelsPerFuelBarrel;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            DayNightSystem dayNightSystem,
            FactionResourceSystem factionResourceSystem,
            BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
            BuildingProductionUpdateCompositionSystemHelper.Context productionUpdateContext,
            BuildingResourceHaulerBridgeCompositionSystemHelper resourceHaulerBridgeSystem,
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context resourceHaulerBridgeContext,
            BuildingSpawnSystem spawnSystem,
            Func<uint> getRandomState,
            Action<uint> setRandomState,
            Action<float> recordOilExtracted,
            Action<float> recordFuelProduced,
            Func<bool> hasPendingPathJob,
            float oilBarrelsPerFuelBarrel)
        {
            RuntimeBuildings = runtimeBuildings;
            RuntimeBuildingMap = runtimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
            DayNightSystem = dayNightSystem;
            FactionResourceSystem = factionResourceSystem;
            ProductionUpdateSystem = productionUpdateSystem;
            ProductionUpdateContext = productionUpdateContext;
            ResourceHaulerBridgeSystem = resourceHaulerBridgeSystem;
            ResourceHaulerBridgeContext = resourceHaulerBridgeContext;
            SpawnSystem = spawnSystem;
            GetRandomState = getRandomState;
            SetRandomState = setRandomState;
            RecordOilExtracted = recordOilExtracted;
            RecordFuelProduced = recordFuelProduced;
            HasPendingPathJob = hasPendingPathJob;
            OilBarrelsPerFuelBarrel = oilBarrelsPerFuelBarrel;
        }
    }

    public void ProcessPendingProductions(Context context)
    {
        if (context.ProductionUpdateSystem == null)
            return;

        uint randomState = context.GetRandomState != null ? context.GetRandomState() : 0u;
        context.ProductionUpdateSystem.UpdatePendingProductions(
            context.ProductionUpdateContext,
            UnityEngine.Time.time,
            UnityEngine.Time.deltaTime,
            ref randomState);
        context.SetRandomState?.Invoke(randomState);
    }

    public void UpdateActiveProductionTransports(Context context)
    {
        if (context.ProductionUpdateSystem == null)
            return;

        uint randomState = context.GetRandomState != null ? context.GetRandomState() : 0u;
        context.ProductionUpdateSystem.UpdateActiveProductionTransports(
            context.ProductionUpdateContext,
            UnityEngine.Time.time,
            UnityEngine.Time.deltaTime,
            ref randomState);
        context.SetRandomState?.Invoke(randomState);
    }

    public void UpdateResourceProduction(Context context)
    {
        if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.FactionResourceSystem == null)
            return;

        float secondsPerDay = context.DayNightSystem != null
            ? Mathf.Max(1f, context.DayNightSystem.FullDayDurationMinutes * 60f)
            : 300f;

        FactionResourceSystem.ResourceProductionTickResult result = context.RuntimeBuildingMap != null
            ? context.FactionResourceSystem.UpdateResourceProduction(
                context.RuntimeBuildingMap,
                secondsPerDay,
                UnityEngine.Time.deltaTime,
                context.OilBarrelsPerFuelBarrel)
            : context.FactionResourceSystem.UpdateResourceProduction(
                context.RuntimeBuildings,
                secondsPerDay,
                UnityEngine.Time.deltaTime,
                context.OilBarrelsPerFuelBarrel);
        if (result.OilExtractedBarrels > 0f)
            context.RecordOilExtracted?.Invoke(result.OilExtractedBarrels);
        if (result.FuelProducedBarrels > 0f)
            context.RecordFuelProduced?.Invoke(result.FuelProducedBarrels);
    }

    public void UpdateResourceHaulers(Context context)
    {
        context.ResourceHaulerBridgeSystem?.UpdateResourceHaulers(
            context.ResourceHaulerBridgeContext,
            context.HasPendingPathJob != null && context.HasPendingPathJob(),
            UnityEngine.Time.time);
    }

    public void CleanupRecentSpawnReservations(Context context)
    {
        context.SpawnSystem?.CleanupRecentSpawnReservations(UnityEngine.Time.time);
    }
}
