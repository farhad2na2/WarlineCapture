using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes SteelPushMissionId=CampaignMissionSequence.SteelPush;
        private static readonly FixedString64Bytes SteelFuelAnchor="anchor.ch04.m02.fuel_reserve";
        private static readonly FixedString128Bytes SteelFuelBuilding="Building_SteelPush_ReserveDepot";
        private const float SteelStartingFuel=160f,SteelCivilianFloor=40f;

        private static void ProjectSteelPushProtection(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,ref CampaignMissionAttemptFactsComponent facts)
        {
            var reserve=em.HasComponent<CampaignMissionSteelPushState>(root)?em.GetComponentData<CampaignMissionSteelPushState>(root):default;
            if(!reserve.SessionToken.Equals(runtime.SessionToken)||reserve.AttemptOrdinal!=runtime.AttemptOrdinal)
                reserve=new CampaignMissionSteelPushState{SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal};
            if(reserve.Initialized==0)
            {
                using var boundaryQuery=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
                using var mapQuery=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if(boundaryQuery.CalculateEntityCount()!=1||mapQuery.CalculateEntityCount()!=1)return;
                var boundary=boundaryQuery.GetSingletonEntity();var metadata=mapQuery.GetSingleton<OperationMapMetadataComponent>();
                if(!metadata.Blob.IsCreated||!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)||
                    !CampaignMissionSpawnSystem.TryFindAnchor(ref metadata.Blob.Value,SteelFuelAnchor,out var anchor))return;
                var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                if(reserve.FuelSpawnRequestId==0)
                {
                    int requestId=1;foreach(var request in requests)requestId=math.max(requestId,request.RequestId+1);
                    reserve.FuelSpawnRequestId=requestId;
                    requests.Add(new BuildingRuntimeSpawnRequest{RequestId=requestId,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding,
                        HasOwnerFaction=1,FactionId=1,BuildingId=SteelFuelBuilding,
                        // Use the normal placement validator to resolve a clear off-road
                        // footprint near the authored yard; never bypass roads or blockers.
                        PreferredOrigin=CampaignMissionSpawnSystem.ToGridCell(anchor.Position,metadata.Blob.Value.Grid)});
                }
                else
                {
                    foreach(var request in requests)
                    {
                        if(request.RequestId!=reserve.FuelSpawnRequestId)continue;
                        if(request.Status==BuildingRuntimeSpawnRequest.Failed){facts.HostileRosterIntegrityFault=1;break;}
                        if(request.Status!=BuildingRuntimeSpawnRequest.Succeeded)break;
                        using var buildings=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth),typeof(BuildingResourceStorageComponent));
                        using var entities=buildings.ToEntityArray(Allocator.Temp);
                        foreach(var entity in entities)
                        {
                            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(entity);if(info.RuntimeBuildingId!=request.BuildingRuntimeId||info.OwnerFactionId!=1)continue;
                            if(request.ActualOrigin.x<590||request.ActualOrigin.y<435||
                                request.ActualOrigin.x+request.ActualFootprint.x>845||request.ActualOrigin.y+request.ActualFootprint.y>490)
                            {facts.HostileRosterIntegrityFault=1;break;}
                            var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);
                            if(storage.FuelStorageCapacity<SteelStartingFuel){facts.HostileRosterIntegrityFault=1;break;}
                            // Authored finite start, once per attempt. Normal shared-storage consumers
                            // spend only the military allocation and cannot drain this civilian floor.
                            storage.StoredFuelBarrels=SteelStartingFuel;storage.CivilianFuelReserveBarrels=SteelCivilianFloor;storage.Version++;
                            em.SetComponentData(entity,storage);reserve.FuelReserve=entity;reserve.Initialized=1;
                            reserve.StartingUsableFuel=reserve.LowestUsableFuel=SteelStartingFuel-SteelCivilianFloor;
                            UnityEngine.Debug.Log($"[SteelPushReserve] initialized origin={request.ActualOrigin} footprint={request.ActualFootprint} stored=160 civilian=40 usable=120");
                            break;
                        }
                        break;
                    }
                }
            }
            if(reserve.Initialized!=0)
            {
                bool valid=em.Exists(reserve.FuelReserve)&&em.HasComponent<UnitHealth>(reserve.FuelReserve)&&em.HasComponent<BuildingResourceStorageComponent>(reserve.FuelReserve);
                if(!valid){facts.ForwardPostDestroyed=1;facts.CoreBreached=1;}
                else
                {
                    var health=em.GetComponentData<UnitHealth>(reserve.FuelReserve);var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);
                    if(health.Max>0)facts.ForwardPostBound=1;
                    if(health.Current<health.Max)facts.ForwardPostDamaged=1;
                    if(health.Current<=0||storage.StoredFuelBarrels+0.001f<SteelCivilianFloor||storage.CivilianFuelReserveBarrels<SteelCivilianFloor)
                    {facts.ForwardPostDestroyed=1;facts.CoreBreached=1;}
                    float usable=math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);
                    reserve.LowestUsableFuel=math.min(reserve.LowestUsableFuel,usable);
                    if(usable<reserve.StartingUsableFuel-.01f)reserve.FuelSpent=1;
                }
            }
            if(facts.ElapsedMilliseconds>=240000)facts.CoreBreached=1;
            if(em.HasComponent<CampaignMissionSteelPushState>(root))em.SetComponentData(root,reserve);else em.AddComponentData(root,reserve);
        }
    }
}
