using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitTransportAirdropSystem))]
    public partial struct SupportPassengerLifecycleSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;var ecb=new EntityCommandBuffer(Allocator.Temp);
            foreach(var (cleanup,entity) in SystemAPI.Query<RefRO<SupportPassengerCleanupComponent>>().WithNone<SupportPassengerOwnerComponent>().WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
            {
                if(em.Exists(cleanup.ValueRO.Canopy))ecb.DestroyEntity(cleanup.ValueRO.Canopy);
                ecb.RemoveComponent<SupportPassengerCleanupComponent>(entity);
            }
            foreach(var (reference,entity) in SystemAPI.Query<RefRW<SupportPassengerOwnerComponent>>().WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
            {
                var owner=reference.ValueRO;
                bool valid=em.Exists(owner.Root)&&em.HasComponent<SupportSessionComponent>(owner.Root);
                if(valid){var session=em.GetComponentData<SupportSessionComponent>(owner.Root);valid=session.SessionToken.Equals(owner.SessionToken)&&session.AttemptOrdinal==owner.AttemptOrdinal;}
                if(!valid){if(em.HasComponent<SupportPassengerCleanupComponent>(entity)){var canopy=em.GetComponentData<SupportPassengerCleanupComponent>(entity).Canopy;if(em.Exists(canopy))ecb.DestroyEntity(canopy);ecb.RemoveComponent<SupportPassengerCleanupComponent>(entity);}ecb.DestroyEntity(entity);continue;}
                if(owner.Released==0||owner.Landed!=0||em.HasComponent<UnitTransportParachuteDropComponent>(entity)||em.HasComponent<UnitTransportCargoDropComponent>(entity)||em.HasComponent<UnitTransportAirdropSettleComponent>(entity))continue;
                owner.Landed=1;reference.ValueRW=owner;ecb.RemoveComponent<SupportPassengerTransitTag>(entity);
            }
            ecb.Playback(em);ecb.Dispose();
        }
    }
}
