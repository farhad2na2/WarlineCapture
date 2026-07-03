using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class BuildingProductionRuntimeTickCompositionSystemHelper
    {
        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Dictionary<int, RuntimeBuildingEntity> RuntimeBuildingMap;
            public readonly DayNightSystem DayNightSystem;
            public readonly FactionResourceCompositionSystemHelper FactionResourceCompositionSystemHelper;
            public readonly BuildingProductionUpdateCompositionSystemHelper ProductionUpdateSystem;
            public readonly BuildingProductionUpdateCompositionSystemHelper.Context ProductionUpdateContext;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper ResourceHaulerBridgeSystem;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.Context ResourceHaulerBridgeContext;
            public readonly BuildingSpawnCompositionSystemHelper SpawnSystem;
            public readonly Func<uint> GetRandomState;
            public readonly Action<uint> SetRandomState;
            public readonly Action<float> RecordOilExtracted;
            public readonly Action<float> RecordFuelProduced;
            public readonly Action<RuntimeBuildingEntity> SyncResourceStorage;
            public readonly Func<bool> HasPendingPathJob;
            public readonly float OilBarrelsPerFuelBarrel;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                DayNightSystem dayNightSystem,
                FactionResourceCompositionSystemHelper factionResourceSystem,
                BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
                BuildingProductionUpdateCompositionSystemHelper.Context productionUpdateContext,
                BuildingResourceHaulerBridgeCompositionSystemHelper resourceHaulerBridgeSystem,
                BuildingResourceHaulerBridgeCompositionSystemHelper.Context resourceHaulerBridgeContext,
                BuildingSpawnCompositionSystemHelper spawnSystem,
                Func<uint> getRandomState,
                Action<uint> setRandomState,
                Action<float> recordOilExtracted,
                Action<float> recordFuelProduced,
                Func<bool> hasPendingPathJob,
                float oilBarrelsPerFuelBarrel,
                Action<RuntimeBuildingEntity> syncResourceStorage = null)
            {
                RuntimeBuildings = runtimeBuildings;
                RuntimeBuildingMap = runtimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
                DayNightSystem = dayNightSystem;
                FactionResourceCompositionSystemHelper = factionResourceSystem;
                ProductionUpdateSystem = productionUpdateSystem;
                ProductionUpdateContext = productionUpdateContext;
                ResourceHaulerBridgeSystem = resourceHaulerBridgeSystem;
                ResourceHaulerBridgeContext = resourceHaulerBridgeContext;
                SpawnSystem = spawnSystem;
                GetRandomState = getRandomState;
                SetRandomState = setRandomState;
                RecordOilExtracted = recordOilExtracted;
                RecordFuelProduced = recordFuelProduced;
                SyncResourceStorage = syncResourceStorage;
                HasPendingPathJob = hasPendingPathJob;
                OilBarrelsPerFuelBarrel = oilBarrelsPerFuelBarrel;
            }
        }

        public bool ProcessPendingProductions(Context context)
        {
            if (context.ProductionUpdateSystem == null)
                return false;

            uint randomState = context.GetRandomState != null ? context.GetRandomState() : 0u;
            bool hasActiveTransport = context.ProductionUpdateSystem.UpdatePendingProductions(
                context.ProductionUpdateContext,
                UnityEngine.Time.time,
                UnityEngine.Time.deltaTime,
                ref randomState);
            context.SetRandomState?.Invoke(randomState);
            return hasActiveTransport;
        }

        public bool UpdateActiveProductionTransports(Context context)
        {
            if (context.ProductionUpdateSystem == null)
                return false;

            uint randomState = context.GetRandomState != null ? context.GetRandomState() : 0u;
            bool hasActiveTransport = context.ProductionUpdateSystem.UpdateActiveProductionTransports(
                context.ProductionUpdateContext,
                UnityEngine.Time.time,
                UnityEngine.Time.deltaTime,
                ref randomState);
            context.SetRandomState?.Invoke(randomState);
            return hasActiveTransport;
        }

        public void UpdateResourceProduction(Context context)
        {
            if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.FactionResourceCompositionSystemHelper == null)
                return;

            float secondsPerDay = context.DayNightSystem != null
                ? Mathf.Max(1f, context.DayNightSystem.FullDayDurationMinutes * 60f)
                : 300f;

            FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = context.RuntimeBuildingMap != null
                ? context.FactionResourceCompositionSystemHelper.UpdateResourceProduction(
                    context.RuntimeBuildingMap,
                    secondsPerDay,
                    UnityEngine.Time.deltaTime,
                    context.OilBarrelsPerFuelBarrel)
                : context.FactionResourceCompositionSystemHelper.UpdateResourceProduction(
                    context.RuntimeBuildings,
                    secondsPerDay,
                    UnityEngine.Time.deltaTime,
                    context.OilBarrelsPerFuelBarrel);
            if (result.OilExtractedBarrels > 0f)
                context.RecordOilExtracted?.Invoke(result.OilExtractedBarrels);
            if (result.FuelProducedBarrels > 0f)
                context.RecordFuelProduced?.Invoke(result.FuelProducedBarrels);

            SyncResourceStorageMirrors(context);
        }

        private static void SyncResourceStorageMirrors(Context context)
        {
            if (context.SyncResourceStorage == null || context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0)
                return;

            if (context.RuntimeBuildingMap != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildingMap)
                    SyncResourceStorageMirror(context, pair.Value);
                return;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                SyncResourceStorageMirror(context, pair.Value);
        }

        private static void SyncResourceStorageMirror(Context context, RuntimeBuildingEntity building)
        {
            if (building == null || building.IsDestroyed)
                return;

            bool hasStorageOrProduction =
                building.OilStorageCapacity > 0 ||
                building.FuelStorageCapacity > 0 ||
                building.OilBarrelsPerDay > 0f ||
                building.FuelBarrelsPerDay > 0f;
            if (!hasStorageOrProduction)
                return;

            context.SyncResourceStorage(building);
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
}
