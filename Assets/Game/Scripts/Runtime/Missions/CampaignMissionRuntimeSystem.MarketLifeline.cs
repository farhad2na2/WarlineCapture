using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceMarketLifeline(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) || !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index))return false;
            ref var rules=ref catalog.Blob.Value.Missions[index].MarketLifeline;if(rules.Enabled==0)return false;
            var em=system.EntityManager;if(runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionMarketLifelineState>(root))return true;
            var state=em.GetComponentData<CampaignMissionMarketLifelineState>(root);
            if(!state.SessionToken.Equals(runtime.SessionToken)||state.AttemptOrdinal!=runtime.AttemptOrdinal||state.SourceVersion!=runtime.SourceVersion)return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)&&em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            int delta=(int)math.round(math.max(0,SystemAPI.Time.DeltaTime)*1000);int rifles=0,hostiles=0,deadRifles=0,deadHostiles=0,initialized=0,delivered=0;bool rifleAtManifest=false,rifleAtCorruptManifest=false;
            var members=em.GetBuffer<CampaignMissionMarketLifelineMember>(root);
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];
                if(member.Dead==0)
                {
                    if(!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity)){if(member.Initialized!=0)state.Failure=MarketLifelineFailure.Integrity;continue;}
                    var health=em.GetComponentData<UnitHealth>(member.Entity);if(health.Max>0){member.Initialized=1;if(health.Current<=0)member.Dead=1;}
                }
                initialized+=member.Initialized;
                if(member.Dead!=0){if(member.Kind==0)deadRifles++;else if(member.Kind==1)state.Failure=MarketLifelineFailure.ConvoyLost;else if(member.Kind==2)deadHostiles++;members[i]=member;continue;}
                if(member.Initialized!=0)
                {
                    if(member.Kind==0)
                    {
                        rifles++;if(em.HasComponent<UnitGrid>(member.Entity)&&!em.HasComponent<UnitPathRequest>(member.Entity)&&!em.HasComponent<UnitPathFollow>(member.Entity))
                        {
                            int2 cell=em.GetComponentData<UnitGrid>(member.Entity).Cell;
                            rifleAtManifest|=math.distancesq(cell,state.ManifestCell)<=rules.ManifestRadius*rules.ManifestRadius;
                            rifleAtCorruptManifest|=math.distancesq(cell,state.CorruptManifestCell)<=rules.ManifestRadius*rules.ManifestRadius;
                        }
                    }
                    else if(member.Kind==1)
                    {
                        if(member.Delivered==0&&em.HasComponent<UnitGrid>(member.Entity)&&math.distancesq(em.GetComponentData<UnitGrid>(member.Entity).Cell,state.DeliveryCell)<=rules.DeliveryRadius*rules.DeliveryRadius)member.Delivered=1;
                        delivered+=member.Delivered;
                    }
                    else if(member.Kind==2)hostiles++;
                    else if(member.Kind==3&&em.HasComponent<UnitGrid>(member.Entity)&&math.distancesq(em.GetComponentData<UnitGrid>(member.Entity).Cell,state.DeliveryCell)<=rules.DeliveryRadius*rules.DeliveryRadius)state.Failure=MarketLifelineFailure.WrongTransfer;
                }
                members[i]=member;
            }
            state.DeliveredCount=(byte)math.min(255,delivered);if(initialized==members.Length&&members.Length>0)state.Ready=1;
            bool active=state.Ready!=0&&opening&&runtime.Phase==MissionPhaseKind.Engage;
            if(active)
            {
                state.ElapsedMilliseconds=SaturatingAddMilliseconds(state.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(rifles==0)state.Failure=MarketLifelineFailure.SquadLost;
                if(state.ElapsedMilliseconds>=rules.DeadlineMilliseconds)state.Failure=MarketLifelineFailure.Deadline;
                state.ManifestHoldMilliseconds=CampaignMissionMarketLifelineRuleUtility.AdvanceHold(state.ManifestHoldMilliseconds,delta,rules.ManifestHoldMilliseconds,true,rifleAtManifest);
                state.CorruptManifestHoldMilliseconds=CampaignMissionMarketLifelineRuleUtility.AdvanceHold(state.CorruptManifestHoldMilliseconds,delta,rules.ManifestHoldMilliseconds,true,rifleAtCorruptManifest);
                if(state.ManifestHoldMilliseconds>=rules.ManifestHoldMilliseconds)state.LegitimateManifestInspected=1;
                if(state.CorruptManifestHoldMilliseconds>=rules.ManifestHoldMilliseconds)state.CorruptManifestInspected=1;
                if(state.LegitimateManifestInspected!=0&&state.CorruptManifestInspected!=0)state.ManifestVerified=1;
                state.VictoryHoldMilliseconds=CampaignMissionMarketLifelineRuleUtility.AdvanceHold(state.VictoryHoldMilliseconds,delta,rules.VictoryHoldMilliseconds,true,state.DeliveredCount>=rules.RequiredDeliveries&&state.ManifestVerified!=0&&hostiles==0);
            }
            if(CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules))state.Complete=1;
            facts.ElapsedMilliseconds=state.ElapsedMilliseconds;facts.CommandSquadAlive=rifles>0?(byte)1:(byte)0;facts.SquadLossCount=deadRifles;facts.HostileDefeatedCount=deadHostiles;
            facts.MarketReliefDelivered=state.DeliveredCount>=rules.RequiredDeliveries?(byte)1:(byte)0;facts.MarketManifestVerified=state.ManifestVerified;facts.MarketOpen=state.Complete;facts.MarketFailure=state.Failure;
            em.SetComponentData(root,state);em.SetComponentData(root,facts);
            if(state.Failure==MarketLifelineFailure.Integrity)return true;
            var phase=runtime.Phase;var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing&&(runtime.ReadyReadiness&runtime.RequiredReadiness)==runtime.RequiredReadiness)phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief&&facts.InteractiveBriefCompleted!=0)phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad&&state.Ready!=0&&state.Failure!=MarketLifelineFailure.None){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Defeat;}
            else if(phase==MissionPhaseKind.FindSquad&&state.Ready!=0&&opening)phase=MissionPhaseKind.Engage;
            else if(phase==MissionPhaseKind.Engage&&state.Complete!=0){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Victory;}
            if(phase!=runtime.Phase&&TryTransition(runtime,phase,outcome,outcome==MissionOutcomeKind.None?MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out var next))em.SetComponentData(root,next);
            return true;
        }
    }
}
