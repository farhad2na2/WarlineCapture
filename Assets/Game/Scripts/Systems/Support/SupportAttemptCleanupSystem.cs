using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SupportAbilityStartupSystem))]
    [UpdateBefore(typeof(SupportAbilityRequestSystem))]
    public partial struct SupportAttemptCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SupportSessionComponent>();
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;
            if(!SystemAPI.TryGetSingletonEntity<SupportSessionComponent>(out var entity))return;
            {
                var session=em.GetComponentData<SupportSessionComponent>(entity);
                bool terminal=session.SessionToken.IsEmpty || em.HasComponent<CampaignMissionRuntimeComponent>(entity) &&
                    em.GetComponentData<CampaignMissionRuntimeComponent>(entity).Outcome!=MissionOutcomeKind.None;
                if(terminal || session.Active==0)
                {
                    em.GetBuffer<SupportRequestElement>(entity).Clear();
                    if(em.HasBuffer<SupportCollectRequestElement>(entity))em.GetBuffer<SupportCollectRequestElement>(entity).Clear();
                    em.SetComponentData(entity,default(SupportPreviewComponent)); em.SetComponentData(entity,default(SupportProposalComponent));
                    var input=em.GetComponentData<SupportInputStateComponent>(entity);
                    if(input.Phase!=0){input.Phase=0;input.Version++;em.SetComponentData(entity,input);}
                    if(!terminal)return; // Pause cancels consent and targeting while preserving live effects.
                    using var ecb=new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
                    foreach(var (zone,effect) in SystemAPI.Query<RefRO<SupportSmokeZoneComponent>>().WithEntityAccess())
                        if(zone.ValueRO.SessionToken.Equals(session.SessionToken)) ecb.DestroyEntity(effect);
                    ecb.Playback(em);
                }
                else if(em.HasComponent<SupportProposalComponent>(entity))
                {
                    var proposal=em.GetComponentData<SupportProposalComponent>(entity);
                    if(proposal.ProposalId!=0 && session.SimulationSeconds>=proposal.ExpiresAt && proposal.Consumed==0)
                    { proposal.Consumed=1; em.SetComponentData(entity,proposal); }
                }
            }
        }
    }
}
