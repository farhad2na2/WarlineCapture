using Unity.Burst;
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

            storage.StoredOilBarrels = result.StoredOilBarrels;
            storage.StoredFuelBarrels = result.StoredFuelBarrels;
            return new TickResult(result.OilExtractedBarrels, result.FuelProducedBarrels);
        }
    }
}
