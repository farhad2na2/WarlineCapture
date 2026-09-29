using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes SplitFuelMissionId=CampaignMissionSequence.SplitFront;
        private static readonly FixedString64Bytes SplitFuelAnchor="anchor.ch04.m03.fuel_reserve";
        private static readonly FixedString128Bytes SplitFuelBuilding="Building_SplitFront_SupportDepot";
        private const float SplitStartingFuel=44f,SplitCivilianFloor=40f;

        private static void ProjectSplitFrontFuel(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,ref CampaignMissionAttemptFactsComponent facts)
        {
            var reserve=em.HasComponent<CampaignMissionSplitFrontFuelState>(root)?em.GetComponentData<CampaignMissionSplitFrontFuelState>(root):default;
            if(!reserve.SessionToken.Equals(runtime.SessionToken)||reserve.AttemptOrdinal!=runtime.AttemptOrdinal)
                reserve=new CampaignMissionSplitFrontFuelState{SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal};
            if(reserve.Initialized==0)
            {
                using var boundaryQuery=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
                using var mapQuery=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if(boundaryQuery.CalculateEntityCount()!=1||mapQuery.CalculateEntityCount()!=1)return;
                var boundary=boundaryQuery.GetSingletonEntity();var metadata=mapQuery.GetSingleton<OperationMapMetadataComponent>();
                if(!metadata.Blob.IsCreated||!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)||
                    !CampaignMissionSpawnSystem.TryFindAnchor(ref metadata.Blob.Value,SplitFuelAnchor,out var anchor))return;
                var requests=em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                if(reserve.FuelSpawnRequestId==0)
                {
                    int requestId=1;foreach(var request in requests)requestId=math.max(requestId,request.RequestId+1);
                    reserve.FuelSpawnRequestId=requestId;
                    requests.Add(new BuildingRuntimeSpawnRequest{RequestId=requestId,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding,
                        HasOwnerFaction=1,FactionId=1,BuildingId=SplitFuelBuilding,
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
                            if(storage.FuelStorageCapacity<SplitStartingFuel){facts.HostileRosterIntegrityFault=1;break;}
                            // Authored finite start, once per attempt. Normal shared-storage consumers
                            // spend only the military allocation and cannot drain this civilian floor.
                            storage.StoredFuelBarrels=SplitStartingFuel;storage.CivilianFuelReserveBarrels=SplitCivilianFloor;storage.Version++;
                            em.SetComponentData(entity,storage);reserve.FuelReserve=entity;reserve.Initialized=1;
                            reserve.StartingUsableFuel=reserve.LowestUsableFuel=SplitStartingFuel-SplitCivilianFloor;
                            UnityEngine.Debug.Log($"[SplitFrontSupportReserve] initialized origin={request.ActualOrigin} footprint={request.ActualFootprint} stored=44 civilian=40 usable=4");
                            break;
                        }
                        break;
                    }
                }
            }
            if(reserve.Initialized!=0)
            {
                bool valid=em.Exists(reserve.FuelReserve)&&em.HasComponent<UnitHealth>(reserve.FuelReserve)&&em.HasComponent<BuildingResourceStorageComponent>(reserve.FuelReserve);
                if(valid)
                {
                    var health=em.GetComponentData<UnitHealth>(reserve.FuelReserve);var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);
                    float usable=health.Current<=0?0:math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);
                    reserve.LowestUsableFuel=math.min(reserve.LowestUsableFuel,usable);
                    if(usable<reserve.StartingUsableFuel-.01f)reserve.FuelSpent=1;
                }
            }
            var scope=new SupportFuelScopeComponent{Required=1,Storage=reserve.FuelReserve};
            if(em.HasComponent<SupportFuelScopeComponent>(root))em.SetComponentData(root,scope);else em.AddComponentData(root,scope);
            if(em.HasComponent<CampaignMissionSplitFrontFuelState>(root))em.SetComponentData(root,reserve);else em.AddComponentData(root,reserve);
        }
    }
}
