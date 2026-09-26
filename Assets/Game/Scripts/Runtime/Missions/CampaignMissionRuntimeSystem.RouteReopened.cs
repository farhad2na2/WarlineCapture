using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes RouteEngineerRoleId="role.route.engineer";
        private static readonly FixedString64Bytes RouteReliefConvoyRoleId="role.route.relief_convoy";
        private static readonly FixedString64Bytes RouteFuelConvoyRoleId="role.route.fuel_convoy";
        private bool TryAdvanceRouteReopened(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) || !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index))return false;
            ref var rules=ref catalog.Blob.Value.Missions[index].RouteReopened;if(rules.Enabled==0)return false;
            var em=system.EntityManager;if(runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionRouteReopenedState>(root))return true;
            var state=em.GetComponentData<CampaignMissionRouteReopenedState>(root);
            if(!state.SessionToken.Equals(runtime.SessionToken)||state.AttemptOrdinal!=runtime.AttemptOrdinal||state.SourceVersion!=runtime.SourceVersion)return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)&&em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            int delta=(int)math.round(math.max(0,SystemAPI.Time.DeltaTime)*1000);int rifles=0,engineers=0,hostiles=0,hostileRoster=0,deadRifles=0,deadCivilians=0,deadHostiles=0,initialized=0;
            bool reliefAtGoal=false,fuelAtGoal=false,riflesAtGate=false,riflesAtRecords=false;int engineersAtLink=0;
            var members=em.GetBuffer<CampaignMissionRouteReopenedMember>(root);
            if(runtime.Phase<MissionPhaseKind.Engage)RefreshRouteReopenedRosterBeforeEngage(em,in runtime,ref members);
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];
                if(member.Kind==4)hostileRoster++;
                if(member.Dead==0)
                {
                    if(!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity)){if(member.Initialized!=0)state.Failure=RouteReopenedFailure.Integrity;continue;}
                    var health=em.GetComponentData<UnitHealth>(member.Entity);if(health.Max>0){member.Initialized=1;if(health.Current<=0)member.Dead=1;}
                }
                initialized+=member.Initialized;
                if(member.Dead!=0)
                {
                    if(member.Kind==0)deadRifles++;else if(member.Kind==1){deadCivilians++;state.Failure=RouteReopenedFailure.EngineerLost;}else if(member.Kind==2){deadCivilians++;state.Failure=RouteReopenedFailure.ReliefConvoyLost;}else if(member.Kind==3){deadCivilians++;state.Failure=RouteReopenedFailure.FuelConvoyLost;}else deadHostiles++;
                    members[i]=member;continue;
                }
                if(member.Initialized!=0)
                {
                    bool hasCell=em.HasComponent<UnitGrid>(member.Entity);int2 cell=hasCell?em.GetComponentData<UnitGrid>(member.Entity).Cell:default;bool stationary=!em.HasComponent<UnitPathRequest>(member.Entity)&&!em.HasComponent<UnitPathFollow>(member.Entity);
                    if(member.Kind==0){rifles++;if(hasCell){riflesAtGate|=math.distancesq(cell,state.HubGateCell)<=rules.HubRadius*rules.HubRadius;riflesAtRecords|=math.distancesq(cell,state.RecordsCell)<=rules.RecordsRadius*rules.RecordsRadius;}}
                    else if(member.Kind==1){engineers++;if(hasCell&&stationary&&math.distancesq(cell,state.DisruptedLinkCell)<=rules.RepairRadius*rules.RepairRadius)engineersAtLink++;}
                    else if(member.Kind==2&&hasCell)reliefAtGoal|=math.distancesq(cell,state.ReliefGoalCell)<=rules.DeliveryRadius*rules.DeliveryRadius;
                    else if(member.Kind==3&&hasCell)fuelAtGoal|=math.distancesq(cell,state.FuelGoalCell)<=rules.DeliveryRadius*rules.DeliveryRadius;
                    else if(member.Kind==4)hostiles++;
                }
                members[i]=member;
            }
            state.Ready=(byte)(initialized==members.Length&&members.Length>0?1:0);bool active=state.Ready!=0&&opening&&runtime.Phase==MissionPhaseKind.Engage;
            if(active)
            {
                state.ElapsedMilliseconds=SaturatingAddMilliseconds(state.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(rifles==0)state.Failure=RouteReopenedFailure.SquadLost;if(engineers<2)state.Failure=RouteReopenedFailure.EngineerLost;if(state.ElapsedMilliseconds>=rules.DeadlineMilliseconds)state.Failure=RouteReopenedFailure.Deadline;
                if(reliefAtGoal)state.ReliefDelivered=1;if(fuelAtGoal)state.FuelDelivered=1;if(state.ReliefDelivered!=0||state.FuelDelivered!=0)state.RelayNodeActivated=1;
                state.LinkRepairHoldMilliseconds=CampaignMissionRouteReopenedRuleUtility.AdvanceHold(state.LinkRepairHoldMilliseconds,delta,rules.LinkRepairHoldMilliseconds,true,engineersAtLink>=2);if(state.LinkRepairHoldMilliseconds>=rules.LinkRepairHoldMilliseconds)state.LinkRestored=1;
                if(riflesAtGate||riflesAtRecords)state.HubEntered=1;
                // Never latch a clear during spawn/roster replacement. A controlled capture
                // requires entering the hub and accounting for every authored defender.
                if(state.HubEntered!=0&&hostileRoster>0&&deadHostiles==hostileRoster)state.GarrisonCleared=1;
                state.RecordsHoldMilliseconds=CampaignMissionRouteReopenedRuleUtility.AdvanceHold(state.RecordsHoldMilliseconds,delta,rules.RecordsHoldMilliseconds,true,state.ReliefDelivered!=0&&state.FuelDelivered!=0&&state.LinkRestored!=0&&state.HubEntered!=0&&state.GarrisonCleared!=0&&riflesAtRecords);
                if(state.RecordsHoldMilliseconds>=rules.RecordsHoldMilliseconds)state.RecordsPreserved=1;
            }
            if(CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules))state.Complete=1;
            facts.ElapsedMilliseconds=state.ElapsedMilliseconds;facts.CommandSquadAlive=rifles>0?(byte)1:(byte)0;facts.SquadLossCount=deadRifles;facts.CivilianLossCount=deadCivilians;facts.HostileTotalCount=hostiles+deadHostiles;facts.HostileDefeatedCount=deadHostiles;
            facts.RouteReliefDelivered=state.ReliefDelivered;facts.RouteFuelDelivered=state.FuelDelivered;facts.RouteLinkRestored=state.LinkRestored;facts.RouteHubEntered=state.HubEntered;facts.RouteGarrisonCleared=state.GarrisonCleared;facts.RouteRecordsPreserved=state.RecordsPreserved;facts.RouteRelayNodeActivated=state.RelayNodeActivated;facts.RouteReopenedFailure=state.Failure;
            em.SetComponentData(root,state);em.SetComponentData(root,facts);if(state.Failure==RouteReopenedFailure.Integrity)return true;
            var phase=runtime.Phase;var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing&&(runtime.ReadyReadiness&runtime.RequiredReadiness)==runtime.RequiredReadiness)phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief&&facts.InteractiveBriefCompleted!=0)phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad&&state.Ready!=0&&state.Failure!=RouteReopenedFailure.None){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Defeat;}
            else if(phase==MissionPhaseKind.FindSquad&&state.Ready!=0&&opening)
            {
                using var releaseCombat=new NativeList<Entity>(members.Length,Allocator.Temp);for(int i=0;i<members.Length;i++)if(em.Exists(members[i].Entity)&&em.HasComponent<CampaignMissionCombatSuppressedTag>(members[i].Entity))releaseCombat.Add(members[i].Entity);for(int i=0;i<releaseCombat.Length;i++)em.RemoveComponent<CampaignMissionCombatSuppressedTag>(releaseCombat[i]);phase=MissionPhaseKind.Engage;
            }
            else if(phase==MissionPhaseKind.Engage&&state.Complete!=0){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Victory;}
            if(phase!=runtime.Phase&&TryTransition(runtime,phase,outcome,outcome==MissionOutcomeKind.None?MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out var next))em.SetComponentData(root,next);return true;
        }

        private static void RefreshRouteReopenedRosterBeforeEngage(EntityManager em,in CampaignMissionRuntimeComponent runtime,ref DynamicBuffer<CampaignMissionRouteReopenedMember> members)
        {
            bool stale=false;for(int i=0;i<members.Length&&!stale;i++)stale=!em.Exists(members[i].Entity)||!em.HasComponent<UnitHealth>(members[i].Entity);if(!stale)return;
            using EntityQuery query=new EntityQueryBuilder(Allocator.Temp).WithAll<CampaignMissionUnitRoleComponent,Faction,UnitHealth>().Build(em);using NativeArray<Entity> entities=query.ToEntityArray(Allocator.Temp);var replacement=new NativeList<CampaignMissionRouteReopenedMember>(entities.Length,Allocator.Temp);
            for(int i=0;i<entities.Length;i++){Entity entity=entities[i];var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);if(!role.SessionToken.Equals(runtime.SessionToken))continue;byte faction=em.GetComponentData<Faction>(entity).Id;byte kind=faction==2?(byte)4:role.MissionRoleId.Equals(RouteEngineerRoleId)?(byte)1:role.MissionRoleId.Equals(RouteReliefConvoyRoleId)?(byte)2:role.MissionRoleId.Equals(RouteFuelConvoyRoleId)?(byte)3:(byte)0;replacement.Add(new CampaignMissionRouteReopenedMember{Entity=entity,Kind=kind});}
            if(replacement.Length==0)return;members.Clear();for(int i=0;i<replacement.Length;i++)members.Add(replacement[i]);
        }
    }
}
