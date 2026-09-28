using Game.Components;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitTransportAirdropSystem))]
    [UpdateAfter(typeof(SupportFlightSystem))]
    public partial struct SupportAirdropPauseSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;float delta=SystemAPI.Time.DeltaTime;
            foreach(var (flight,entity) in SystemAPI.Query<RefRO<SupportFlightComponent>>().WithEntityAccess())
                if(em.Exists(flight.ValueRO.Root)&&em.GetComponentData<SupportSessionComponent>(flight.ValueRO.Root).Active==0&&em.HasComponent<UnitTransportAirdropRequest>(entity))
                {var request=em.GetComponentData<UnitTransportAirdropRequest>(entity);request.NextDropAt+=delta;em.SetComponentData(entity,request);}
            foreach(var (ownerRef,entity) in SystemAPI.Query<RefRW<SupportPassengerOwnerComponent>>().WithEntityAccess())
            {
                var owner=ownerRef.ValueRO;if(owner.Released==0||!em.Exists(owner.Root)||!em.HasComponent<SupportSessionComponent>(owner.Root))continue;
                bool paused=em.GetComponentData<SupportSessionComponent>(owner.Root).Active==0;
                if(em.HasComponent<UnitTransportParachuteDropComponent>(entity))
                {var drop=em.GetComponentData<UnitTransportParachuteDropComponent>(entity);owner.Canopy=drop.VisualEntity;if(paused){drop.StartedAt+=delta;em.SetComponentData(entity,drop);}}
                if(em.HasComponent<UnitTransportAirdropSettleComponent>(entity)&&paused)
                {var settle=em.GetComponentData<UnitTransportAirdropSettleComponent>(entity);settle.StartedAt+=delta;em.SetComponentData(entity,settle);}
                if(paused&&em.Exists(owner.Canopy)&&em.HasComponent<UnitTransportAirdropVisualCleanup>(owner.Canopy))
                {var cleanup=em.GetComponentData<UnitTransportAirdropVisualCleanup>(owner.Canopy);if(cleanup.DestroyAt>0)cleanup.DestroyAt+=delta;em.SetComponentData(owner.Canopy,cleanup);}
                ownerRef.ValueRW=owner;
            }
            foreach(var (crateRef,entity) in SystemAPI.Query<RefRO<SupportSupplyCrateComponent>>().WithEntityAccess())
            {
                var crate=crateRef.ValueRO;if(crate.Released==0||!em.Exists(crate.Root)||!em.HasComponent<SupportSessionComponent>(crate.Root)||em.GetComponentData<SupportSessionComponent>(crate.Root).Active!=0)continue;
                if(em.HasComponent<UnitTransportCargoDropComponent>(entity)){var drop=em.GetComponentData<UnitTransportCargoDropComponent>(entity);drop.StartedAt+=delta;em.SetComponentData(entity,drop);}
                if(em.Exists(crate.Canopy)&&em.HasComponent<UnitTransportAirdropVisualCleanup>(crate.Canopy)){var cleanup=em.GetComponentData<UnitTransportAirdropVisualCleanup>(crate.Canopy);if(cleanup.DestroyAt>0)cleanup.DestroyAt+=delta;em.SetComponentData(crate.Canopy,cleanup);}
            }
        }
    }
}
