using Game.Components;
using Unity.Burst;
using Unity.Entities;

namespace Game.Runtime
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BuildingResourceHaulerTransferEcsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }

        public static bool TryCompleteLoad(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            return BuildingResourceStorageTransferSystemHelper.TryCompleteLoad(
                ref source,
                resourceKind,
                loadAmount,
                ref hauler);
        }

        public static void RevertLoad(
            ref BuildingResourceStorageComponent source,
            byte resourceKind,
            float loadAmount,
            ref UnitResourceHauler hauler)
        {
            BuildingResourceStorageTransferSystemHelper.RevertLoad(
                ref source,
                resourceKind,
                loadAmount,
                ref hauler);
        }

        public static bool TryCompleteUnload(
            ref BuildingResourceStorageComponent destination,
            byte resourceKind,
            ref UnitResourceHauler hauler)
        {
            return BuildingResourceStorageTransferSystemHelper.TryCompleteUnload(
                ref destination,
                resourceKind,
                ref hauler);
        }
    }
}
