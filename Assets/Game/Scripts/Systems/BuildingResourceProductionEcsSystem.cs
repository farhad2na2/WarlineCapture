using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BuildingResourceProductionEcsSystem : ISystem
    {
        public readonly struct TickResult
        {
            public readonly float OilExtractedBarrels;
            public readonly float FuelProducedBarrels;

            public TickResult(float oilExtractedBarrels, float fuelProducedBarrels)
            {
                OilExtractedBarrels = oilExtractedBarrels;
                FuelProducedBarrels = fuelProducedBarrels;
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }

        public static TickResult ApplyStorageQuery(
            EntityManager entityManager,
            EntityQuery storageQuery,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel)
        {
            if (storageQuery.IsEmptyIgnoreFilter)
                return new TickResult(0f, 0f);

#if UNITY_EDITOR
            S002PhysicalStartupObservation.ForwardProductionBoundary?.Invoke(entityManager, storageQuery, "before-production", secondsPerDay, deltaTime, oilBarrelsPerFuelBarrel);
#endif
            secondsPerDay = UnityEngine.Mathf.Max(1f, secondsPerDay);
            deltaTime = UnityEngine.Mathf.Max(0f, deltaTime);
            oilBarrelsPerFuelBarrel = UnityEngine.Mathf.Max(0.001f, oilBarrelsPerFuelBarrel);

            float oilExtracted = 0f;
            float fuelProduced = 0f;
            ComponentTypeHandle<BuildingResourceStorageComponent> storageType =
                entityManager.GetComponentTypeHandle<BuildingResourceStorageComponent>(false);
            ComponentTypeHandle<BuildingResourceProductionProfileComponent> profileType =
                entityManager.GetComponentTypeHandle<BuildingResourceProductionProfileComponent>(false);
            using NativeArray<ArchetypeChunk> chunks = storageQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<BuildingResourceStorageComponent> storages = chunks[chunkIndex].GetNativeArray(ref storageType);
                bool hasProfile = chunks[chunkIndex].Has(ref profileType);
                NativeArray<BuildingResourceProductionProfileComponent> profiles = hasProfile
                    ? chunks[chunkIndex].GetNativeArray(ref profileType) : default;
                for (int i = 0; i < storages.Length; i++)
                {
                    BuildingResourceStorageComponent storage = storages[i];
                    if (!hasProfile && !HasProductionOrConversion(storage))
                        continue;

                    uint previousVersion = storage.Version;
                    TickResult result;
                    if (hasProfile)
                    {
                        var profile = profiles[i];
                        result = ApplyProfileTick(ref storage, ref profile, deltaTime);
                        profiles[i] = profile;
                    }
                    else
                        result = ApplyTick(ref storage, secondsPerDay, deltaTime, oilBarrelsPerFuelBarrel);
                    if (hasProfile || storage.Version != previousVersion)
                        storages[i] = storage;

                    oilExtracted += result.OilExtractedBarrels;
                    fuelProduced += result.FuelProducedBarrels;
                }
            }

#if UNITY_EDITOR
            S002PhysicalStartupObservation.ForwardProductionBoundary?.Invoke(entityManager, storageQuery, "after-production", secondsPerDay, deltaTime, oilBarrelsPerFuelBarrel);
#endif
            return new TickResult(oilExtracted, fuelProduced);
        }

        private static bool HasProductionOrConversion(in BuildingResourceStorageComponent storage)
        {
            return storage.OilBarrelsPerDay > 0f || storage.FuelBarrelsPerDay > 0f;
        }

        public static TickResult ApplyProfileTick(
            ref BuildingResourceStorageComponent storage,
            ref BuildingResourceProductionProfileComponent profile,
            float deltaTime)
        {
            deltaTime = UnityEngine.Mathf.Max(0f, deltaTime);
            storage.OilBarrelsPerDay = profile.OilPerPeriod;
            storage.FuelBarrelsPerDay = profile.FuelPerPeriod;
            if (profile.OilPerPeriod > 0f)
                return ApplyTick(ref storage, profile.PeriodSeconds, deltaTime, 0.5f);
            if (profile.OilPerFuelBatch <= 0f || profile.FuelPerBatch <= 0f || profile.FuelBatchSeconds <= 0f)
                return new TickResult(0f, 0f);

            float availableOil = UnityEngine.Mathf.Max(0f, storage.StoredOilBarrels - storage.ReservedOilOutboundBarrels);
            float availableFuelRoom = UnityEngine.Mathf.Max(0f, storage.FuelStorageCapacity -
                storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels);
            // Wait before consuming any input or advancing a blocked batch.
            if (availableOil < profile.OilPerFuelBatch || availableFuelRoom < profile.FuelPerBatch)
                return new TickResult(0f, 0f);
            float progress = profile.FuelBatchProgressSeconds + deltaTime;
            int elapsed = UnityEngine.Mathf.FloorToInt(progress / profile.FuelBatchSeconds);
            int batches = UnityEngine.Mathf.Min(elapsed, UnityEngine.Mathf.Min(
                UnityEngine.Mathf.FloorToInt(availableOil / profile.OilPerFuelBatch),
                UnityEngine.Mathf.FloorToInt(availableFuelRoom / profile.FuelPerBatch)));
            profile.FuelBatchProgressSeconds = batches < elapsed
                ? 0f : progress - batches * profile.FuelBatchSeconds;
            if (batches == 0) return new TickResult(0f, 0f);
            float produced = batches * profile.FuelPerBatch;
            storage.StoredOilBarrels -= batches * profile.OilPerFuelBatch;
            storage.StoredFuelBarrels += produced;
            storage.Version = storage.Version == uint.MaxValue ? 1u : storage.Version + 1u;
            return new TickResult(0f, produced);
        }

        public static TickResult ApplyTick(
            ref BuildingResourceStorageComponent storage,
            float secondsPerDay,
            float deltaTime,
            float oilBarrelsPerFuelBarrel)
        {
            BuildingResourceProductionSystemHelper.Result result = BuildingResourceProductionSystemHelper.Tick(
                new BuildingResourceProductionSystemHelper.State(
                    storage.OilStorageCapacity,
                    storage.FuelStorageCapacity,
                    storage.OilBarrelsPerDay,
                    storage.FuelBarrelsPerDay,
                    storage.StoredOilBarrels,
                    storage.StoredFuelBarrels),
                secondsPerDay,
                deltaTime,
                oilBarrelsPerFuelBarrel);

            bool changed = storage.StoredOilBarrels != result.StoredOilBarrels ||
                           storage.StoredFuelBarrels != result.StoredFuelBarrels;
            storage.StoredOilBarrels = result.StoredOilBarrels;
            storage.StoredFuelBarrels = result.StoredFuelBarrels;
            if (changed)
                storage.Version = storage.Version == uint.MaxValue ? 1u : storage.Version + 1u;
            return new TickResult(result.OilExtractedBarrels, result.FuelProducedBarrels);
        }
    }
}
