using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SupportAbilityRequestSystem))]
    public partial struct SupportAbilityStartupSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SupportSessionComponent>();
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;
            if(SystemAPI.TryGetSingleton(out SupportPayloadRegistryComponent payloads))
                foreach(var bindings in SystemAPI.Query<RefRW<SupportPayloadBindingsComponent>>())
                    bindings.ValueRW=new SupportPayloadBindingsComponent {InfantryPrefab=payloads.InfantryPrefab,ParachutePrefab=payloads.ParachutePrefab,CratePrefab=payloads.CratePrefab,InfantryCount=payloads.InfantryCount};
            foreach(var (sessionRef, catalogRef, entity) in SystemAPI.Query<RefRW<SupportSessionComponent>,RefRO<SupportCatalogComponent>>().WithEntityAccess())
            {
                var session=sessionRef.ValueRO;
                session.Active=1;
                // A test grant changes availability, never the real campaign attempt or phase.
                if(session.TestEncounter==0 || em.HasComponent<CampaignMissionRuntimeComponent>(entity))
                {
                    if(!em.HasComponent<CampaignMissionRuntimeComponent>(entity)) { session.Active=0; sessionRef.ValueRW=session; continue; }
                    var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(entity);
                    if(!session.SessionToken.Equals(runtime.SessionToken) || session.AttemptOrdinal!=runtime.AttemptOrdinal)
                    {
                        session.SessionToken=runtime.SessionToken; session.AttemptOrdinal=runtime.AttemptOrdinal;
                        session.MissionSourceVersion=runtime.SourceVersion; session.CatalogRevision=catalogRef.ValueRO.Revision;
                        session.SimulationSeconds=0; session.LastPlayerRequestId=0; session.LastAriaRequestId=0; session.NextRequestId=1;
                        var abilities=em.GetBuffer<SupportAbilityStateElement>(entity); abilities.Clear();
                        if(catalogRef.ValueRO.Blob.IsCreated)
                        {
                            ref var definitions=ref catalogRef.ValueRO.Blob.Value.Abilities;
                            var grant=em.GetComponentData<SupportMissionPolicyComponent>(entity).TestGrantMask;
                            for(int i=0;i<definitions.Length;i++) abilities.Add(new SupportAbilityStateElement
                            { Kind=definitions[i].Kind, ChargesRemaining=definitions[i].Charges, StateVersion=1,
                              Enabled=(byte)(definitions[i].ProductionReady!=0 || session.TestEncounter!=0 && (grant & SupportTargetValidationUtilitySystemHelper.Mask(definitions[i].Kind))!=0 ? 1 : 0) });
                        }
                        em.GetBuffer<SupportRequestElement>(entity).Clear(); em.GetBuffer<SupportReceiptElement>(entity).Clear();
                        if(em.HasBuffer<SupportCollectRequestElement>(entity))em.GetBuffer<SupportCollectRequestElement>(entity).Clear();
                        if(em.HasComponent<SupportSupplyReadModelComponent>(entity))em.SetComponentData(entity,default(SupportSupplyReadModelComponent));
                        if(em.HasComponent<SupportCollectionFeedbackComponent>(entity))em.SetComponentData(entity,default(SupportCollectionFeedbackComponent));
                        em.SetComponentData(entity,default(SupportPreviewComponent)); em.SetComponentData(entity,default(SupportProposalComponent));
                        var input=em.GetComponentData<SupportInputStateComponent>(entity);input.Phase=0;input.Version++;em.SetComponentData(entity,input);
                    }
                    var policy=em.GetComponentData<SupportMissionPolicyComponent>(entity);policy.AllowedMask=0;policy.LessonKind=SupportAbilityKind.None;
                    if(em.HasBuffer<SupportMissionPolicyElement>(entity))
                        foreach(var row in em.GetBuffer<SupportMissionPolicyElement>(entity))
                        {
                            bool match=runtime.MissionId.Length>=row.MissionPrefix.Length;
                            for(int i=0;match && i<row.MissionPrefix.Length;i++)match=runtime.MissionId[i]==row.MissionPrefix[i];
                            if(match){policy.AllowedMask=row.AllowedMask;policy.LessonKind=row.LessonKind;if(session.TestEncounter==0)policy.PopulationCeiling=row.PopulationCeiling;break;}
                        }
                    if(session.TestEncounter!=0)policy.AllowedMask|=policy.TestGrantMask;
                    em.SetComponentData(entity,policy);
                    session.Active=(byte)(runtime.SourceVersion==session.MissionSourceVersion && runtime.Phase==MissionPhaseKind.Engage && runtime.Outcome==MissionOutcomeKind.None ? 1 : 0);
                }
                if(!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.SimulationActive==0 || gameplay.PlayRequested==0) session.Active=0;
                if(session.Active!=0) session.SimulationSeconds+=SystemAPI.Time.DeltaTime;
                sessionRef.ValueRW=session;
            }
        }
    }
}
