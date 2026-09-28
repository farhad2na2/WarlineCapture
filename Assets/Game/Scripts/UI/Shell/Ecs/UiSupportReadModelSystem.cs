using Game.Components;
using Game.Runtime;
using Unity.Entities;
using Unity.Burst;
namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityRequestSystem))]
    public partial struct UiSupportReadModelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)=>state.RequireForUpdate<SupportFuelAvailabilityComponent>();
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!SystemAPI.TryGetSingleton(out SupportSessionComponent session))return;
            float total=0;
            bool scoped=SystemAPI.TryGetSingleton(out SupportFuelScopeComponent scope) && scope.Required!=0;
            foreach(var (storage,entity) in SystemAPI.Query<RefRO<BuildingResourceStorageComponent>>().WithEntityAccess())
                if(!scoped || entity==scope.Storage)total+=SupportFuelTransactionSystem.Usable(storage.ValueRO,session.FactionId);
            foreach(var available in SystemAPI.Query<RefRW<SupportFuelAvailabilityComponent>>())
                if(available.ValueRO.Total!=total){available.ValueRW.Total=total;available.ValueRW.Version++;}
        }
    }
}
