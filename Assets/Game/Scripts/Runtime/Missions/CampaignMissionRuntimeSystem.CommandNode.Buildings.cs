using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static void ProjectCommandBuildings(EntityManager em,Entity root,ref OperationMapBlob map,ref CampaignMissionCommandNodeState mission)
        {
            using var boundaryQuery=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
            if (boundaryQuery.CalculateEntityCount()!=1) return;
            var boundary=boundaryQuery.GetSingletonEntity();if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)) return;
            if (!em.HasComponent<BuildingStartingGrantOwner>(root)) em.AddComponent<BuildingStartingGrantOwner>(root);
            var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);var bindings=em.GetBuffer<CampaignMissionCommandBuildingRequest>(root);
            for (int i=0;i<bindings.Length;i++)
            {
                var binding=bindings[i];
                if (binding.RequestId==0)
                {
                    string suffix=i switch { 0=>"clinic_origin",1=>"utility_origin",2=>"audit_origin",_=>"fuel_reserve" };
                    if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map,new FixedString64Bytes("anchor.ch05.m05."+suffix),out var anchor)) { MarkCommandIntegrity(ref mission,$"binding={i} missing anchor={suffix}");continue; }
                    int id=1;foreach (var request in requests) id=math.max(id,request.RequestId+1);
                    requests.Add(new BuildingRuntimeSpawnRequest { RequestId=id,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding,HasOwnerFaction=1,FactionId=(byte)(i<3?0:1),
                        AuthoredStartingGrant=1,PlanEntity=root,RequirePreferredOrigin=1,EntryIndex=i,
                        BuildingId=new FixedString128Bytes(i==0?"Building_Command_Clinic":i==1?"Building_Command_Utility":i==2?"Building_Command_Audit":"Building_Command_ReserveDepot"),
                        PreferredOrigin=CampaignMissionSpawnSystem.ToGridCell(anchor.Position,map.Grid) });
                    binding.RequestId=id;bindings[i]=binding;continue;
                }
                if (binding.Bound==0)
                {
                    foreach (var request in requests)
                    {
                        if (request.RequestId!=binding.RequestId) continue;
                        if (request.Status==BuildingRuntimeSpawnRequest.Failed) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} failed status={request.Status} origin={request.ActualOrigin} footprint={request.ActualFootprint}");break; }
                        if (request.Status!=BuildingRuntimeSpawnRequest.Succeeded) break;
                        using var buildings=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth));using var entities=buildings.ToEntityArray(Allocator.Temp);
                        foreach (var entity in entities)
                        {
                            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                            if (!IsCommandRequestedBuilding(em,entity,in request)) continue;
                            float2 min=map.Grid.Origin.xz+(float2)request.ActualOrigin*map.Grid.CellSize;
                            float2 max=min+(float2)request.ActualFootprint*map.Grid.CellSize;
                            using var boundsQuery=em.CreateEntityQuery(typeof(OperationMapBoundsComponent));
                            if (boundsQuery.CalculateEntityCount()!=1) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} bounds count={boundsQuery.CalculateEntityCount()}");break; }
                            var bounds=em.GetComponentData<OperationMapBoundsComponent>(boundsQuery.GetSingletonEntity());
                            if (math.any(min<bounds.PlayableMin.xz)||math.any(max>bounds.PlayableMax.xz)) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} origin={request.ActualOrigin} footprint={request.ActualFootprint} worldMin={min} worldMax={max} playableMin={bounds.PlayableMin} playableMax={bounds.PlayableMax}");break; }
                            var health=em.GetComponentData<UnitHealth>(entity);
                            if (i<3 && health.Max!=1000) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} healthMax={health.Max} expected=1000 runtimeBuilding={info.RuntimeBuildingId}");break; }
                            if (i==3)
                            {
                                if (!em.HasComponent<BuildingResourceStorageComponent>(entity)) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} missing reserve storage entity={entity}");break; }
                                var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                                if (storage.OwnerFactionId!=1 || storage.FuelStorageCapacity<240) { MarkCommandIntegrity(ref mission,$"binding={i} request={binding.RequestId} reserve owner={storage.OwnerFactionId} capacity={storage.FuelStorageCapacity}");break; }
                                storage.StoredFuelBarrels=160;storage.CivilianFuelReserveBarrels=20;storage.Version++;em.SetComponentData(entity,storage);
                            }
                            binding.Entity=entity;binding.Bound=1;
                            if (i==0) mission.Clinic=entity; if (i==1) mission.Utility=entity;
                            if (i==2) mission.Audit=entity; if (i==3) mission.Reserve=entity;
                            break;
                        }
                        break;
                    }
                    bindings[i]=binding;
                }
                else
                {
                    bool live=NetworkLive(em,binding.Entity)&&em.HasComponent<RuntimeBuildingCombatInfo>(binding.Entity)&&em.GetComponentData<RuntimeBuildingCombatInfo>(binding.Entity).OwnerFactionId==(i<3?0:1);
                    if (!live && i<2) mission.Failure=CommandNodeFailure.ServicesLost;
                    if (!live && i==2) mission.Failure=CommandNodeFailure.ServicesLost;
                    if (i==3)
                    {
                        if (!live || !em.HasComponent<BuildingResourceStorageComponent>(binding.Entity)) mission.Failure=CommandNodeFailure.FuelReserveLost;
                        else { var storage=em.GetComponentData<BuildingResourceStorageComponent>(binding.Entity);if (storage.OwnerFactionId!=1 || storage.CivilianFuelReserveBarrels<20 || storage.StoredFuelBarrels+.001f<20) mission.Failure=CommandNodeFailure.FuelReserveLost; }
                    }
                }
            }
        }
        internal static bool IsCommandRequestedBuilding(EntityManager em,Entity entity,in BuildingRuntimeSpawnRequest request)
        {
            if(request.Status!=BuildingRuntimeSpawnRequest.Succeeded||!em.Exists(entity)||em.HasComponent<OperationMapBuildingComponent>(entity)||
                !em.HasComponent<RuntimeBuildingCombatInfo>(entity)||!em.HasComponent<UnitSourcePrefabKey>(entity))return false;
            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
            return request.HasOwnerFaction!=0&&info.RuntimeBuildingId==request.BuildingRuntimeId&&info.OwnerFactionId==request.FactionId&&
                math.all(info.OriginCell==request.ActualOrigin)&&math.all(info.FootprintCells==request.ActualFootprint)&&
                em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()==request.BuildingId.ToString();
        }
    }
}
