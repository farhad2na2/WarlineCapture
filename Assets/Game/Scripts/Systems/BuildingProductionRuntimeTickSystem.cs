using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingProductionRuntimeTickSystem
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly DayNightSystem DayNightSystem;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly BuildingProductionUpdateSystem ProductionUpdateSystem;
        public readonly BuildingProductionUpdateSystem.Context ProductionUpdateContext;
        public readonly BuildingResourceHaulerBridgeSystem ResourceHaulerBridgeSystem;
        public readonly BuildingResourceHaulerBridgeSystem.Context ResourceHaulerBridgeContext;
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
            BuildingProductionUpdateSystem productionUpdateSystem,
            BuildingProductionUpdateSystem.Context productionUpdateContext,
            BuildingResourceHaulerBridgeSystem resourceHaulerBridgeSystem,
            BuildingResourceHaulerBridgeSystem.Context resourceHaulerBridgeContext,
            BuildingSpawnSystem spawnSystem,
            Func<uint> getRandomState,
            Action<uint> setRandomState,
            Action<float> recordOilExtracted,
            Action<float> recordFuelProduced,
            Func<bool> hasPendingPathJob,
            float oilBarrelsPerFuelBarrel)
        {
            RuntimeBuildings = runtimeBuildings;
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
            Time.time,
            Time.deltaTime,
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

        FactionResourceSystem.ResourceProductionTickResult result = context.FactionResourceSystem.UpdateResourceProduction(
            context.RuntimeBuildings,
            secondsPerDay,
            Time.deltaTime,
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
            Time.time);
    }

    public void CleanupRecentSpawnReservations(Context context)
    {
        context.SpawnSystem?.CleanupRecentSpawnReservations(Time.time);
    }
}
