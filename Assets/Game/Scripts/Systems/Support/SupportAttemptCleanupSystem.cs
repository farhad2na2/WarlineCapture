using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityStartupSystem))]
    [UpdateBefore(typeof(SupportAbilityRequestSystem))]
    public partial struct SupportAttemptCleanupSystem : ISystem
    {
        private EntityQuery sessionRoots;
        public void OnCreate(ref SystemState state)
        {
            sessionRoots=state.GetEntityQuery(typeof(SupportSessionComponent),typeof(SupportRequestElement),typeof(SupportInputStateComponent),typeof(SupportPreviewComponent),typeof(SupportProposalComponent));
            state.RequireForUpdate(sessionRoots);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();var em=state.EntityManager;
            using var roots=sessionRoots.ToEntityArray(Allocator.Temp);
            using var ecb=new EntityCommandBuffer(Allocator.Temp);
            foreach(var entity in roots)
            {
                var session=em.GetComponentData<SupportSessionComponent>(entity);
                bool terminal=SupportSessionUtility.IsTerminal(em,entity,session);
                if(terminal || session.Active==0)
                {
                    em.GetBuffer<SupportRequestElement>(entity).Clear();
                    if(em.HasBuffer<SupportCollectRequestElement>(entity))em.GetBuffer<SupportCollectRequestElement>(entity).Clear();
                    em.SetComponentData(entity,default(SupportPreviewComponent));em.SetComponentData(entity,default(SupportProposalComponent));
                    var input=em.GetComponentData<SupportInputStateComponent>(entity);
                    if(input.Phase!=0){input.Phase=0;input.Version++;em.SetComponentData(entity,input);}
                    if(!terminal)continue; // Pause clears consent, preserving effects and other sessions.
                    foreach(var (zone,effect) in SystemAPI.Query<RefRO<SupportSmokeZoneComponent>>().WithEntityAccess())
                        if(zone.ValueRO.Root==entity || zone.ValueRO.Root==Entity.Null && SupportSessionUtility.TrySmokeOwner(em,roots,zone.ValueRO,out var owner) && owner==entity)
                            ecb.DestroyEntity(effect);
                }
                else
                {
                    var proposal=em.GetComponentData<SupportProposalComponent>(entity);
                    if(proposal.ProposalId!=0 && session.SimulationSeconds>=proposal.ExpiresAt && proposal.Consumed==0)
                    {proposal.Consumed=1;em.SetComponentData(entity,proposal);}
                }
            }
            ecb.Playback(em);
        }
    }
}
