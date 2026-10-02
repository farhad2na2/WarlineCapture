using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes CitywideReinforcementKey = "unit_chr_soldier_male_02_alt_04";
        private static void ProjectCitywideBuildings(EntityManager em,Entity root,ref OperationMapBlob map,ref CampaignMissionCitywideAlertState mission)
        {
            using var boundaryQuery=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
            if (boundaryQuery.CalculateEntityCount()!=1) return;
            var boundary=boundaryQuery.GetSingletonEntity();if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)) return;
            if (!em.HasComponent<BuildingStartingGrantOwner>(root)) em.AddComponent<BuildingStartingGrantOwner>(root);
            var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);var bindings=em.GetBuffer<CampaignMissionCitywideBuildingRequest>(root);
            for (int i=0;i<bindings.Length;i++)
            {
                var binding=bindings[i];
                if (binding.RequestId==0)
                {
                    string suffix=i switch { 0=>"clinic_service",1=>"utility_service",2=>"producer_clinic",3=>"producer_utility",4=>"reserve_clinic",_=>"reserve_utility" };
                    if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map,new FixedString64Bytes("anchor.ch05.m01."+suffix),out var anchor)) { MarkCitywideIntegrity(ref mission,$"binding={i} missing anchor={suffix}");continue; }
                    int id=1;foreach (var request in requests) id=math.max(id,request.RequestId+1);
                    requests.Add(new BuildingRuntimeSpawnRequest { RequestId=id,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding,HasOwnerFaction=1,FactionId=1,
                        AuthoredStartingGrant=1,PlanEntity=root,RequirePreferredOrigin=1,EntryIndex=i,
                        BuildingId=new FixedString128Bytes(i==0?"Building_Citywide_Clinic":i==1?"Building_Citywide_Utility":i<4?"Building_Barrack":"Building_Citywide_ReserveDepot"),
                        PreferredOrigin=CampaignMissionSpawnSystem.ToGridCell(anchor.Position,map.Grid) });
                    binding.RequestId=id;bindings[i]=binding;continue;
                }
                if (binding.Bound==0)
                {
                    foreach (var request in requests)
                    {
                        if (request.RequestId!=binding.RequestId) continue;
                        if (request.Status==BuildingRuntimeSpawnRequest.Failed) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} failed status={request.Status} origin={request.ActualOrigin} footprint={request.ActualFootprint}");break; }
                        if (request.Status!=BuildingRuntimeSpawnRequest.Succeeded) break;
                        using var buildings=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth));using var entities=buildings.ToEntityArray(Allocator.Temp);
                        foreach (var entity in entities)
                        {
                            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                            if (!IsCitywideRequestedBuilding(em,entity,in request)) continue;
                            float2 min=map.Grid.Origin.xz+(float2)request.ActualOrigin*map.Grid.CellSize;
                            float2 max=min+(float2)request.ActualFootprint*map.Grid.CellSize;
                            using var boundsQuery=em.CreateEntityQuery(typeof(OperationMapBoundsComponent));
                            if (boundsQuery.CalculateEntityCount()!=1) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} bounds count={boundsQuery.CalculateEntityCount()}");break; }
                            var bounds=em.GetComponentData<OperationMapBoundsComponent>(boundsQuery.GetSingletonEntity());
                            if (math.any(min<bounds.PlayableMin.xz)||math.any(max>bounds.PlayableMax.xz)) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} origin={request.ActualOrigin} footprint={request.ActualFootprint} worldMin={min} worldMax={max} playableMin={bounds.PlayableMin} playableMax={bounds.PlayableMax}");break; }
                            var health=em.GetComponentData<UnitHealth>(entity);
                            if (i<2 && health.Max!=(i==0?1000:700)) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} healthMax={health.Max} expected={(i==0?1000:700)} runtimeBuilding={info.RuntimeBuildingId}");break; }
                            if (i>=4)
                            {
                                if (!em.HasComponent<BuildingResourceStorageComponent>(entity)) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} missing reserve storage entity={entity}");break; }
                                var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                                if (storage.OwnerFactionId!=1 || storage.FuelStorageCapacity<100) { MarkCitywideIntegrity(ref mission,$"binding={i} request={binding.RequestId} reserve owner={storage.OwnerFactionId} capacity={storage.FuelStorageCapacity}");break; }
                                storage.StoredFuelBarrels=100;storage.CivilianFuelReserveBarrels=20;storage.Version++;em.SetComponentData(entity,storage);
                            }
                            binding.Entity=entity;binding.Bound=1;
                            if (i==0) mission.Clinic=entity;if (i==1) mission.Utility=entity;
                            if (i==2) { mission.ClinicProducer=entity;mission.ClinicProducerRuntimeId=info.RuntimeBuildingId; }
                            if (i==3) { mission.UtilityProducer=entity;mission.UtilityProducerRuntimeId=info.RuntimeBuildingId; }
                            if (i==4) mission.ClinicReserve=entity;if (i==5) mission.UtilityReserve=entity;
                            break;
                        }
                        break;
                    }
                    bindings[i]=binding;
                }
                else
                {
                    bool live=CitywideLive(em,binding.Entity)&&em.HasComponent<RuntimeBuildingCombatInfo>(binding.Entity)&&em.GetComponentData<RuntimeBuildingCombatInfo>(binding.Entity).OwnerFactionId==1;
                    if (!live && i==0) mission.Failure=CitywideAlertFailure.ClinicLost;
                    if (!live && i==1) mission.Failure=CitywideAlertFailure.UtilityLost;
                    if (i>=4)
                    {
                        if (!live || !em.HasComponent<BuildingResourceStorageComponent>(binding.Entity)) mission.Failure=CitywideAlertFailure.FuelReserveLost;
                        else { var storage=em.GetComponentData<BuildingResourceStorageComponent>(binding.Entity);if (storage.OwnerFactionId!=1 || storage.CivilianFuelReserveBarrels<20 || storage.StoredFuelBarrels+.001f<20) mission.Failure=CitywideAlertFailure.FuelReserveLost; }
                    }
                }
            }
            if (mission.ClinicProducer!=Entity.Null && mission.UtilityProducer!=Entity.Null && !CitywideLive(em,mission.ClinicProducer)&&!CitywideLive(em,mission.UtilityProducer)&&mission.ReinforcementProduced==0) mission.Failure=CitywideAlertFailure.ProducerLost;
        }
        internal static bool IsCitywideRequestedBuilding(EntityManager em,Entity entity,in BuildingRuntimeSpawnRequest request)
        {
            if(request.Status!=BuildingRuntimeSpawnRequest.Succeeded||!em.Exists(entity)||em.HasComponent<OperationMapBuildingComponent>(entity)||
                !em.HasComponent<RuntimeBuildingCombatInfo>(entity)||!em.HasComponent<UnitSourcePrefabKey>(entity))return false;
            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
            return info.RuntimeBuildingId==request.BuildingRuntimeId&&info.OwnerFactionId==1&&
                math.all(info.OriginCell==request.ActualOrigin)&&math.all(info.FootprintCells==request.ActualFootprint)&&
                em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()==request.BuildingId.ToString();
        }
        internal static bool IsCitywideProducedUnit(EntityManager em,in BuildingProducedUnitReadModel produced,in CampaignMissionCitywideAlertState mission)
        {
            if (produced.BuildingRuntimeId!=mission.ClinicProducerRuntimeId && produced.BuildingRuntimeId!=mission.UtilityProducerRuntimeId || produced.BuildingRuntimeId<=0 ||
                produced.HasOwnerFaction==0 || produced.OwnerFactionId!=1 || !produced.UnitSourceKey.Equals(CitywideReinforcementKey) || !CitywideLive(em,produced.Unit) ||
                em.HasComponent<Prefab>(produced.Unit) || !em.HasComponent<Faction>(produced.Unit) || em.GetComponentData<Faction>(produced.Unit).Id!=1 ||
                !em.HasComponent<UnitSourcePrefabKey>(produced.Unit) || !em.GetComponentData<UnitSourcePrefabKey>(produced.Unit).Value.Equals(CitywideReinforcementKey)) return false;
            return true;
        }
        private static void ProjectCitywideProduction(EntityManager em,ref CampaignMissionCitywideAlertState mission)
        {
            if (CitywideLive(em,mission.Reinforcement)) return;
            mission.Reinforcement=Entity.Null;mission.ReinforcementProduced=0;
            using var boundary=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));if (boundary.CalculateEntityCount()!=1) return;
            var entity=boundary.GetSingletonEntity();if (!em.HasBuffer<BuildingProducedUnitReadModel>(entity)) return;
            var produced=em.GetBuffer<BuildingProducedUnitReadModel>(entity,true);
            if (mission.ProducedBaseline<0 || mission.ProducedBaseline>produced.Length) { MarkCitywideIntegrity(ref mission,$"production baseline={mission.ProducedBaseline} bufferLength={produced.Length}");return; }
            for (int i=mission.ProducedBaseline;i<produced.Length;i++)
            {
                var unit=produced[i];if (!IsCitywideProducedUnit(em,in unit,in mission)) continue;
                mission.Reinforcement=unit.Unit;mission.ReinforcementProduced=1;break;
            }
        }
    }
}
