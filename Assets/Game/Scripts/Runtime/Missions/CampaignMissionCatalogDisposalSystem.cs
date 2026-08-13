using Game.Components;
using Unity.Burst;
using Unity.Entities;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct CampaignMissionCatalogDisposalSystem : ISystem
    {
        private EntityQuery _missionUnits;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _missionUnits = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (RefRW<CampaignMissionCatalogComponent> catalog
                     in SystemAPI.Query<RefRW<CampaignMissionCatalogComponent>>())
            {
                if (catalog.ValueRO.OwnsBlob == 0 || catalog.ValueRO.Blob.IsCreated)
                    continue;
                CampaignMissionCatalogComponent cleared = catalog.ValueRO;
                cleared.OwnsBlob = 0;
                catalog.ValueRW = cleared;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            foreach (RefRW<CampaignMissionCatalogComponent> catalog
                     in SystemAPI.Query<RefRW<CampaignMissionCatalogComponent>>())
            {
                CampaignMissionCatalogComponent owned = catalog.ValueRO;
                DisposeOwned(ref owned);
                catalog.ValueRW = owned;
            }
            if (!_missionUnits.IsEmptyIgnoreFilter)
                state.EntityManager.DestroyEntity(_missionUnits);
        }

        public static void DisposeOwned(ref CampaignMissionCatalogComponent catalog)
        {
            if (catalog.OwnsBlob != 0 && catalog.Blob.IsCreated)
                catalog.Blob.Dispose();
            catalog.Blob = default;
            catalog.OwnsBlob = 0;
        }
    }
}
