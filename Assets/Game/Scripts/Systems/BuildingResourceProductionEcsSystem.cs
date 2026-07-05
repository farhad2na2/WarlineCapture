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

            secondsPerDay = UnityEngine.Mathf.Max(1f, secondsPerDay);
            deltaTime = UnityEngine.Mathf.Max(0f, deltaTime);
            oilBarrelsPerFuelBarrel = UnityEngine.Mathf.Max(0.001f, oilBarrelsPerFuelBarrel);

            float oilExtracted = 0f;
            float fuelProduced = 0f;
            using NativeArray<Entity> entities = storageQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!entityManager.Exists(entity) ||
                    !entityManager.HasComponent<BuildingResourceStorageComponent>(entity))
                {
                    continue;
                }

                BuildingResourceStorageComponent storage =
                    entityManager.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (!HasProductionOrConversion(storage))
                    continue;

                uint previousVersion = storage.Version;
                TickResult result = ApplyTick(
                    ref storage,
                    secondsPerDay,
                    deltaTime,
                    oilBarrelsPerFuelBarrel);
                if (storage.Version != previousVersion)
                    entityManager.SetComponentData(entity, storage);

                oilExtracted += result.OilExtractedBarrels;
                fuelProduced += result.FuelProducedBarrels;
            }

            return new TickResult(oilExtracted, fuelProduced);
        }

        private static bool HasProductionOrConversion(in BuildingResourceStorageComponent storage)
        {
            return storage.OilBarrelsPerDay > 0f || storage.FuelBarrelsPerDay > 0f;
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
