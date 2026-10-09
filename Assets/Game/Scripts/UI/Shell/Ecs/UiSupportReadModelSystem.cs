using Game.Components;
using Game.Runtime;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityRequestSystem))]
    public partial struct UiSupportReadModelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)=>state.RequireForUpdate<SupportFuelAvailabilityComponent>();
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();
            // Each session displays its own faction's usable stock. Use the same
            // calculation as the transaction so authored scopes and reserves agree.
            foreach(var (session, available, root) in SystemAPI.Query<RefRO<SupportSessionComponent>,RefRW<SupportFuelAvailabilityComponent>>().WithEntityAccess())
            {
                float total=SupportFuelTransactionSystem.Available(state.EntityManager,root,session.ValueRO.FactionId);
                if(available.ValueRO.Total!=total){available.ValueRW.Total=total;available.ValueRW.Version++;}
            }
        }
    }
}
