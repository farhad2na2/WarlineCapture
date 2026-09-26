using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes PowerEngineerRoleId="role.power.engineer";
        private static readonly FixedString64Bytes PowerFamilyConvoyRoleId="role.power.family_convoy";
        private static readonly FixedString64Bytes PowerFuelServiceRoleId="role.power.fuel_service";
        private bool TryAdvancePowerRelay(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) || !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index))return false;
            ref var rules=ref catalog.Blob.Value.Missions[index].PowerRelay;if(rules.Enabled==0)return false;
            var em=system.EntityManager;if(runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionPowerRelayState>(root))return true;
            var state=em.GetComponentData<CampaignMissionPowerRelayState>(root);
            if(!state.SessionToken.Equals(runtime.SessionToken)||state.AttemptOrdinal!=runtime.AttemptOrdinal||state.SourceVersion!=runtime.SourceVersion)return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)&&em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            int delta=(int)math.round(math.max(0,SystemAPI.Time.DeltaTime)*1000);int rifles=0,engineers=0,hostiles=0,deadRifles=0,deadCivilians=0,deadHostiles=0,initialized=0;
            bool engineersAtRepair=false,fuelAtRepair=false,familiesAtShort=false,familiesAtSafe=false,familiesAtShelter=false,hostileNearRepair=false;
            var members=em.GetBuffer<CampaignMissionPowerRelayMember>(root);
            if(runtime.Phase<MissionPhaseKind.Engage)
                RefreshPowerRelayRosterBeforeEngage(em,in runtime,ref members);
            int engineersOnSite=0;
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];
                if(member.Dead==0)
                {
                    if(!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity)){if(member.Initialized!=0)state.Failure=PowerRelayFailure.Integrity;continue;}
                    var health=em.GetComponentData<UnitHealth>(member.Entity);if(health.Max>0){member.Initialized=1;if(health.Current<=0)member.Dead=1;}
                }
                initialized+=member.Initialized;
                if(member.Dead!=0)
                {
                    if(member.Kind==0)deadRifles++;else if(member.Kind==1){deadCivilians++;state.Failure=PowerRelayFailure.EngineerLost;}else if(member.Kind==2){deadCivilians++;state.Failure=PowerRelayFailure.FamilyConvoyLost;}else if(member.Kind==3)state.Failure=PowerRelayFailure.FuelServiceLost;else deadHostiles++;
                    members[i]=member;continue;
                }
                if(member.Initialized!=0)
                {
                    bool hasCell=em.HasComponent<UnitGrid>(member.Entity);int2 cell=hasCell?em.GetComponentData<UnitGrid>(member.Entity).Cell:default;
                    bool stationary=!em.HasComponent<UnitPathRequest>(member.Entity)&&!em.HasComponent<UnitPathFollow>(member.Entity);
                    if(member.Kind==0)rifles++;
                    else if(member.Kind==1)
                    {
                        engineers++;if(hasCell&&stationary&&math.distancesq(cell,state.RepairCell)<=rules.RepairRadius*rules.RepairRadius)engineersOnSite++;
                    }
                    else if(member.Kind==2&&hasCell)
                    {
                        familiesAtShort|=math.distancesq(cell,state.ShortRouteCell)<=rules.RouteRadius*rules.RouteRadius;
                        familiesAtSafe|=math.distancesq(cell,state.SafeRouteCell)<=rules.RouteRadius*rules.RouteRadius;
                        familiesAtShelter|=math.distancesq(cell,state.ShelterCell)<=rules.ShelterRadius*rules.ShelterRadius;
                    }
                    else if(member.Kind==3&&hasCell)fuelAtRepair|=math.distancesq(cell,state.RepairCell)<=rules.RepairRadius*rules.RepairRadius;
                    else if(member.Kind==4){hostiles++;if(hasCell&&math.distancesq(cell,state.RepairCell)<=rules.RepairRadius*rules.RepairRadius*4f)hostileNearRepair=true;}
                }
                members[i]=member;
            }
            engineersAtRepair=engineersOnSite>=2;state.Ready=(byte)(initialized==members.Length&&members.Length>0?1:0);
            bool active=state.Ready!=0&&opening&&runtime.Phase==MissionPhaseKind.Engage;
            if(active)
            {
                state.ElapsedMilliseconds=SaturatingAddMilliseconds(state.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(rifles==0)state.Failure=PowerRelayFailure.SquadLost;
                if(engineers<2)state.Failure=PowerRelayFailure.EngineerLost;
                if(state.ElapsedMilliseconds>=rules.DeadlineMilliseconds)state.Failure=PowerRelayFailure.Deadline;
                if(familiesAtShort)state.ExposedRouteUsed=1;
                if(familiesAtSafe)state.SafeRouteConfirmed=1;
                if(state.SafeRouteConfirmed!=0&&familiesAtShelter)state.FamiliesSheltered=1;
                if(fuelAtRepair)state.FuelDelivered=1;
                state.RepairHoldMilliseconds=CampaignMissionPowerRelayRuleUtility.AdvanceHold(state.RepairHoldMilliseconds,delta,rules.RepairHoldMilliseconds,true,state.FuelDelivered!=0&&engineersAtRepair&&!hostileNearRepair);
                if(state.RepairHoldMilliseconds>=rules.RepairHoldMilliseconds)state.PowerRestored=1;
                state.VictoryHoldMilliseconds=CampaignMissionPowerRelayRuleUtility.AdvanceHold(state.VictoryHoldMilliseconds,delta,rules.VictoryHoldMilliseconds,true,state.FamiliesSheltered!=0&&state.PowerRestored!=0&&hostiles==0);
            }
            if(CampaignMissionPowerRelayRuleUtility.IsVictory(in state,in rules))state.Complete=1;
            facts.ElapsedMilliseconds=state.ElapsedMilliseconds;facts.CommandSquadAlive=rifles>0?(byte)1:(byte)0;facts.SquadLossCount=deadRifles;facts.CivilianLossCount=deadCivilians;
            // Power Relay's registered mission roster is the authoritative combat set.
            // Project both sides of the result fact from it so settlement cannot compare
            // live defeated attackers against a stale authored group count.
            facts.HostileTotalCount=hostiles+deadHostiles;facts.HostileDefeatedCount=deadHostiles;
            facts.PowerSafeRouteConfirmed=state.SafeRouteConfirmed;facts.PowerFamiliesSheltered=state.FamiliesSheltered;facts.PowerRestored=state.PowerRestored;facts.PowerRelaySecured=state.Complete;facts.PowerExposedRouteUsed=state.ExposedRouteUsed;facts.PowerRelayFailure=state.Failure;
            em.SetComponentData(root,state);em.SetComponentData(root,facts);
            if(state.Failure==PowerRelayFailure.Integrity)return true;
            var phase=runtime.Phase;var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing&&(runtime.ReadyReadiness&runtime.RequiredReadiness)==runtime.RequiredReadiness)phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief&&facts.InteractiveBriefCompleted!=0)phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad&&state.Ready!=0&&state.Failure!=PowerRelayFailure.None){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Defeat;}
            else if(phase==MissionPhaseKind.FindSquad&&state.Ready!=0&&opening)
            {
                using var releaseCombat=new NativeList<Entity>(members.Length,Allocator.Temp);
                for(int i=0;i<members.Length;i++)
                    if(em.Exists(members[i].Entity)&&em.HasComponent<CampaignMissionCombatSuppressedTag>(members[i].Entity))
                        releaseCombat.Add(members[i].Entity);
                for(int i=0;i<releaseCombat.Length;i++)
                    em.RemoveComponent<CampaignMissionCombatSuppressedTag>(releaseCombat[i]);
                phase=MissionPhaseKind.Engage;
            }
            else if(phase==MissionPhaseKind.Engage&&state.Complete!=0){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Victory;}
            if(phase!=runtime.Phase&&TryTransition(runtime,phase,outcome,outcome==MissionOutcomeKind.None?MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out var next))em.SetComponentData(root,next);
            return true;
        }

        private static void RefreshPowerRelayRosterBeforeEngage(
            EntityManager em,in CampaignMissionRuntimeComponent runtime,
            ref DynamicBuffer<CampaignMissionPowerRelayMember> members)
        {
            bool stale=false;
            for(int i=0;i<members.Length&&!stale;i++)
                stale=!em.Exists(members[i].Entity)||!em.HasComponent<UnitHealth>(members[i].Entity);
            if(!stale)return;

            using EntityQuery query=new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CampaignMissionUnitRoleComponent,Faction,UnitHealth>().Build(em);
            using NativeArray<Entity> entities=query.ToEntityArray(Allocator.Temp);
            var replacement=new NativeList<CampaignMissionPowerRelayMember>(entities.Length,Allocator.Temp);
            for(int i=0;i<entities.Length;i++)
            {
                Entity entity=entities[i];
                var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if(!role.SessionToken.Equals(runtime.SessionToken))continue;
                byte faction=em.GetComponentData<Faction>(entity).Id;
                byte kind=faction==2?(byte)4:role.MissionRoleId.Equals(PowerEngineerRoleId)?(byte)1:
                    role.MissionRoleId.Equals(PowerFamilyConvoyRoleId)?(byte)2:
                    role.MissionRoleId.Equals(PowerFuelServiceRoleId)?(byte)3:(byte)0;
                replacement.Add(new CampaignMissionPowerRelayMember {Entity=entity,Kind=kind});
            }
            if(replacement.Length==0)return;
            members.Clear();
            for(int i=0;i<replacement.Length;i++)members.Add(replacement[i]);
        }
    }
}
