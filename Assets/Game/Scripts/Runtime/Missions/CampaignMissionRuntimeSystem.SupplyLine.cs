using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private bool TryAdvanceSupplyLine(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) || !CampaignMissionSpawnSystem.TryFindDefinition(in catalog,in runtime,out int index))return false;
            ref var rules=ref catalog.Blob.Value.Missions[index].SupplyLine;if(rules.Enabled==0)return false;
            var em=system.EntityManager;
            if(runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionSupplyLineState>(root))return true;
            var s=em.GetComponentData<CampaignMissionSupplyLineState>(root);
            if(!s.SessionToken.Equals(runtime.SessionToken) || s.AttemptOrdinal!=runtime.AttemptOrdinal || s.SourceVersion!=runtime.SourceVersion)return true;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            bool opening=em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage>=6;
            int delta=(int)math.round(math.max(0,SystemAPI.Time.DeltaTime)*1000);
            int rifles=0,haulers=0,hostiles=0,initialized=0,deadRifles=0,deadHostiles=0;
            var members=em.GetBuffer<CampaignMissionSupplyLineMember>(root);
            for(int i=0;i<members.Length;i++)
            {
                var m=members[i];
                if(m.Dead==0)
                {
                    if(!em.Exists(m.Entity) || !em.HasComponent<UnitHealth>(m.Entity))
                    {if(m.Initialized!=0)s.Failure=SupplyLineFailure.Integrity;continue;}
                    var hp=em.GetComponentData<UnitHealth>(m.Entity);
                    if(hp.Max>0){m.Initialized=1;if(hp.Current<=0)m.Dead=1;}
                }
                if(m.Kind==1 && m.Dead==0 && s.OilTransferred!=0 && em.Exists(m.Entity) && em.HasComponent<UnitGrid>(m.Entity))
                {
                    if(em.HasComponent<ManualMoveOrderTag>(m.Entity) && em.HasComponent<UnitTarget>(m.Entity) &&
                        CampaignMissionSupplyLineRuleUtility.IsAlternateLaneOrder(em.GetComponentData<UnitTarget>(m.Entity).Cell,s.AlternateLane))
                    {
                        // The mission asks the commander to choose the alternate
                        // lane. Credit that decision when the correct hauler
                        // accepts the order; path following can legitimately stop
                        // just outside the exact anchor on a crowded road.
                        s.RerouteOrdered=1;s.RouteRecovered=1;
                    }
                    if(s.RerouteOrdered!=0 && math.distancesq(em.GetComponentData<UnitGrid>(m.Entity).Cell,s.AlternateLane)<=36)
                        s.RouteRecovered=1;
                    else if(s.RouteRecovered==0 && !em.HasComponent<ManualMoveOrderTag>(m.Entity))s.RerouteOrdered=0;
                }
                initialized+=m.Initialized;
                if(m.Dead!=0){if(m.Kind==0)deadRifles++;else if(m.Kind==3)deadHostiles++;else s.Failure=SupplyLineFailure.HaulerLost;}
                else if(m.Initialized!=0){if(m.Kind==0)rifles++;else if(m.Kind==3)hostiles++;else haulers++;}
                members[i]=m;
            }
            if(facts.CommandSquadSpawned!=0 && SystemAPI.TryGetSingletonEntity<BuildingRuntimeStateTag>(out var boundary))
                AdvanceSupplyLineLinks(em,root,boundary,ref s);
            var links=em.GetBuffer<CampaignMissionSupplyLineLink>(root);
            bool linksReady=links.Length==3;
            for(int i=0;i<links.Length;i++)linksReady&=links[i].Initialized!=0;
            if(linksReady && initialized==members.Length && members.Length>0 && haulers==2)s.Ready=1;
            if(s.Ready==0)
            {s.PreparationMilliseconds=SaturatingAddMilliseconds(s.PreparationMilliseconds,SystemAPI.Time.DeltaTime);if(s.PreparationMilliseconds>60000)s.Failure=SupplyLineFailure.Integrity;}
            bool alive=linksReady;
            for(int i=0;i<links.Length;i++)
            {
                var link=links[i];if(link.Initialized==0)continue;
                if(!em.Exists(link.Entity) || !em.HasComponent<UnitHealth>(link.Entity) || em.GetComponentData<UnitHealth>(link.Entity).Current<=0)
                {alive=false;s.Failure=SupplyLineFailure.LinkLost;continue;}
                if(!em.HasComponent<BuildingResourceStorageComponent>(link.Entity)){s.Failure=SupplyLineFailure.Integrity;continue;}
                var storage=em.GetComponentData<BuildingResourceStorageComponent>(link.Entity);
                if(i==1)
                {
                    s.ObservedOil=math.max(s.ObservedOil,storage.StoredOilBarrels);
                    s.ObservedRefinedFuel=math.max(s.ObservedRefinedFuel,storage.StoredFuelBarrels);
                    if(s.ObservedOil>0 && s.ObservedRefinedFuel>0)s.OilTransferred=1;
                }
                if(i==2){s.StoredFuel=storage.StoredFuelBarrels;if(s.StoredFuel>10 && s.OilTransferred!=0)s.FuelTransferred=1;}
            }
            if(em.HasComponent<CampaignMissionSupplyLineAllocationRequest>(root))
            {
                var request=em.GetComponentData<CampaignMissionSupplyLineAllocationRequest>(root);
                if(request.Pending!=0)
                {
                    if(request.SessionToken.Equals(runtime.SessionToken) && request.AttemptOrdinal==runtime.AttemptOrdinal && request.SourceVersion==runtime.SourceVersion &&
                        s.Ready!=0 && s.Failure==SupplyLineFailure.None && runtime.Phase==MissionPhaseKind.Engage && s.StoredFuel>=rules.CivilianReserveBarrels && links.Length==3 && em.Exists(links[2].Entity))
                    {
                        var storage=em.GetComponentData<BuildingResourceStorageComponent>(links[2].Entity);
                        if(storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels>=rules.CivilianReserveBarrels)
                        {
                            storage.CivilianFuelReserveBarrels=rules.CivilianReserveBarrels;storage.Version++;
                            em.SetComponentData(links[2].Entity,storage);s.AllocatedCivilianBarrels=rules.CivilianReserveBarrels;
                        }
                    }
                    request.Pending=0;em.SetComponentData(root,request);
                }
            }
            bool active=s.Ready!=0 && opening && runtime.Phase==MissionPhaseKind.Engage;
            if(active)
            {
                s.ElapsedMilliseconds=SaturatingAddMilliseconds(s.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
                if(rifles==0)s.Failure=SupplyLineFailure.SquadLost;
                if(s.ElapsedMilliseconds>=rules.DeadlineMilliseconds)s.Failure=SupplyLineFailure.Deadline;
                s.HoldMilliseconds=CampaignMissionSupplyLineRuleUtility.AdvanceHold(s.HoldMilliseconds,delta,rules.HoldMilliseconds,true,alive,hostiles==0 && s.RouteRecovered!=0 && s.AllocatedCivilianBarrels>=rules.CivilianReserveBarrels,s.StoredFuel,rules.ReserveBarrels);
            }
            if(CampaignMissionSupplyLineRuleUtility.IsVictory(in s,in rules))s.Complete=1;
            facts.ElapsedMilliseconds=s.ElapsedMilliseconds;facts.CommandSquadAlive=rifles>0?(byte)1:(byte)0;
            facts.SquadLossCount=deadRifles;facts.HostileDefeatedCount=deadHostiles;
            facts.SupplyOilTransferred=s.OilTransferred;facts.SupplyFuelTransferred=s.FuelTransferred;
            facts.SupplyStoredFuel=(int)math.floor(s.StoredFuel);facts.SupplyReserveComplete=s.Complete;facts.SupplyFailure=s.Failure;
            em.SetComponentData(root,s);em.SetComponentData(root,facts);
            if(s.Failure==SupplyLineFailure.Integrity)return true;
            var phase=runtime.Phase;var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing && (runtime.ReadyReadiness&runtime.RequiredReadiness)==runtime.RequiredReadiness)phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted!=0)phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad && s.Ready!=0 && s.Failure!=SupplyLineFailure.None){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Defeat;}
            else if(phase==MissionPhaseKind.FindSquad && s.Ready!=0 && opening)phase=MissionPhaseKind.Engage;
            else if(phase==MissionPhaseKind.Engage && s.Complete!=0){phase=MissionPhaseKind.Result;outcome=MissionOutcomeKind.Victory;}
            if(phase!=runtime.Phase && TryTransition(runtime,phase,outcome,outcome==MissionOutcomeKind.None?MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out var next))em.SetComponentData(root,next);
            return true;
        }
        private void AdvanceSupplyLineLinks(EntityManager em,Entity root,Entity boundary,ref CampaignMissionSupplyLineState s)
        {
            if(!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary))return;
            for(int i=0;i<3;i++)
            {
                var link=em.GetBuffer<CampaignMissionSupplyLineLink>(root)[i];if(link.Initialized!=0)continue;
                var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                if(link.SpawnRequestId==0)
                {
                    int id=1;foreach(var r in requests)id=math.max(id,r.RequestId+1);
                    link.SpawnRequestId=id;
                    requests.Add(new BuildingRuntimeSpawnRequest {RequestId=id,HasOwnerFaction=1,FactionId=1,BuildingId=link.BuildingId,PreferredOrigin=link.Origin,RequirePreferredOrigin=1});
                }
                else foreach(var r in requests)
                {
                    if(r.RequestId!=link.SpawnRequestId)continue;
                    if(r.Status==BuildingRuntimeSpawnRequest.Failed){s.Failure=SupplyLineFailure.Integrity;break;}
                    if(r.Status!=BuildingRuntimeSpawnRequest.Succeeded)break;
                    using var entities=_gridlockBuildings.ToEntityArray(Allocator.Temp);
                    foreach(var entity in entities)
                    {
                        var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                        if(info.RuntimeBuildingId!=r.BuildingRuntimeId || !math.all(info.OriginCell==r.ActualOrigin) || info.OwnerFactionId!=1)continue;
                        if(!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Max<=0 || !em.HasComponent<BuildingResourceStorageComponent>(entity))break;
                        link.Entity=entity;link.RuntimeBuildingId=r.BuildingRuntimeId;link.Initialized=1;
                        if(i==2)
                        {
                            // Authored working-chain fallback: finite starting fuel for the two haulers.
                            // This is seeded exactly once, never on a later stock shortage.
                            var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                            storage.StoredFuelBarrels=10;storage.Version++;em.SetComponentData(entity,storage);
                        }
                        break;
                    }
                    break;
                }
                var updated=em.GetBuffer<CampaignMissionSupplyLineLink>(root);updated[i]=link;
            }
        }
    }
}
