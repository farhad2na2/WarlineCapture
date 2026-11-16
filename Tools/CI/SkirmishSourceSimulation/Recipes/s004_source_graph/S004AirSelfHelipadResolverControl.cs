using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

internal static class S004AirSelfHelipadResolverControl
{
    static void Need(bool value,string reason) { if(!value)throw new InvalidOperationException(reason); }
    static void Main()
    {
        var em=new EntityManager {LogicalAllocatorAdmitted=true};int next=1;
        em.AllocateLogicalIdentity=()=>new Entity {Index=next++,Version=1};
        var gridEntity=em.CreateEntity();var grid=new GridConfig {Width=16,Height=16,CellSize=1,Origin=float3.zero};
        em.AddComponentData(gridEntity,grid);var walkable=em.AddBuffer<GridWalkable>(gridEntity);
        for(int i=0;i<256;i++)walkable.Add(new GridWalkable {Value=1});
        var blocked=new NativeBitArray(256,Allocator.Persistent,NativeArrayOptions.ClearMemory);
        var occupied=new NativeBitArray(256,Allocator.Persistent,NativeArrayOptions.ClearMemory);
        try {
            em.AddComponentData(gridEntity,new DynamicBlockerComponent {GridSize=256,Blocked=blocked});
            em.AddComponentData(gridEntity,new DynamicOccupancyComponent {GridSize=256,Occupied=occupied});
            var boundary=em.CreateEntity();var points=em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
            var producer=new RuntimeBuildingEntity {Id=7,HasOwnerFaction=true,OwnerFactionId=1,
                ProducedUnitSlots=new Entity[2],PendingProductions=new List<SourcePendingProduction>()};
            var buildings=new Dictionary<int,RuntimeBuildingEntity> {{7,producer}};
            points.Add(new BuildingFactionProductionSpawnPointReadModel {FactionId=2,BuildingId=new FixedString128Bytes("building_helipad"),BuildingRuntimeId=7,SlotIndex=0,Cell=new int2(1,1),WorldPosition=new float3(1.5f,0,1.5f)});
            points.Add(new BuildingFactionProductionSpawnPointReadModel {FactionId=1,BuildingId=new FixedString128Bytes("building_helipad"),BuildingRuntimeId=7,SlotIndex=0,Cell=new int2(3,3),WorldPosition=new float3(3.5f,0,3.5f)});
            points.Add(new BuildingFactionProductionSpawnPointReadModel {FactionId=1,BuildingId=new FixedString128Bytes("building_helipad"),BuildingRuntimeId=7,SlotIndex=1,Cell=new int2(8,8),WorldPosition=new float3(8.5f,0,8.5f)});
            var world=new SourceProductionGroundWorld(em,gridEntity,boundary,buildings,new Dictionary<int,Game.Runtime.TransportPose.PoseGraph>());
            uint random=11;
            bool found=world.TryResolveAvailableFactionHelipadSpawn(world.SpawnContext,1,null,em,gridEntity,grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),new int2(1,1),ref random,out var cell,out var position);
            Need(found && cell.Equals(new int2(3,3)) && position.Equals(new float3(3.5f,0,3.5f)) && random==12,"First own helipad resolution differs");
#if WARLINE_S004_AIRSELF_SLOT_FIXTURE
            Need(world.TryResolveProductionSlotAtCell(world.SpawnContext,em,grid,new int2(3,3),new int2(1,1),out var slotBuilding,out int slotIndex) &&
                ReferenceEquals(slotBuilding,producer) && slotIndex==0,"Original override cell did not bind the helipad slot");
#endif
            var unit=em.CreateEntity();em.AddComponentData(unit,new UnitGrid {Cell=new int2(3,3)});em.AddComponentData(unit,new UnitFootprint {Size=new int2(1,1)});
            random=11;
            found=world.TryResolveAvailableFactionHelipadSpawn(world.SpawnContext,1,null,em,gridEntity,grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),new int2(1,1),ref random,out cell,out position);
            Need(found && cell.Equals(new int2(8,8)) && random==12,"Occupied helipad did not choose next actual slot");
#if WARLINE_S004_AIRSELF_SLOT_FIXTURE
            // Geometric occupancy is checked by the prior helipad resolver. Slot
            // identity lookup binds only the actual registered production slot.
            Need(world.TryResolveProductionSlotAtCell(world.SpawnContext,em,grid,new int2(8,8),new int2(1,1),out slotBuilding,out slotIndex) &&
                ReferenceEquals(slotBuilding,producer) && slotIndex==1,"Second override cell did not bind the actual slot");
            Console.WriteLine("[S004OriginalAircraftSlotControl] result=Passed cases=2 fixtureOnly=1");
#endif
#if WARLINE_S004_AIRSELF_MATCHER_FIXTURE
            var match=typeof(SourceProductionGroundWorld).GetMethod("SourceOriginalBuildingMatchesId",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Need(match!=null,"Original matcher projection missing");
            bool Matches(RuntimeBuildingEntity b,string id)=>(bool)match.Invoke(null,new object[]{b,id});
            var prefabEntity=em.CreateEntity();
            producer.Definition=new RuntimeDefinition { DisplayName="Other",Prefab=new SourceUnitPrefab("Building_Helipad",prefabEntity) };
            Need(Matches(producer,"building_helipad"),"Original prefab identity not matched");
            Need(!Matches(producer,"building_barrack"),"Foreign building identity matched");
            Need(!Matches(null,"building_helipad"),"Missing building identity matched");
            producer.Definition=new RuntimeDefinition { DisplayName="Building_Helipad" };
            Need(Matches(producer,"building_helipad"),"Original display identity not matched");
            Console.WriteLine("[S004OriginalBuildingMatcherControl] result=Passed cases=4 fixtureOnly=1");
#endif
#if WARLINE_S004_AIRSELF_ALIGNMENT_FIXTURE
            var produced=em.AddBuffer<BuildingProducedUnitReadModel>(boundary);
            var aircraft=em.CreateEntity();em.AddComponentData(aircraft,LocalTransform.FromPosition(float3.zero));
            produced.Add(new BuildingProducedUnitReadModel { BuildingRuntimeId=producer.Id,Unit=aircraft });
            world.SourceAlignNewestProducedUnitRotation(world.SpawnContext,producer,new Vector3(1,0,0));
            var aligned=em.GetComponentData<LocalTransform>(aircraft);
            var forward=math.mul(aligned.Rotation,new float3(0,0,1));
            Need(math.distancesq(forward,new float3(1,0,0))<0.0001f,"Original alignment did not use newest produced read-model actor");
            world.SourceAlignNewestProducedUnitRotation(world.SpawnContext,producer,new Vector3(0,0,0));
            Need(em.GetComponentData<LocalTransform>(aircraft).Rotation.Equals(aligned.Rotation),"Zero direction must retain actor rotation");
            Console.WriteLine("[S004OriginalAircraftAlignmentControl] result=Passed cases=2 fixtureOnly=1");
#endif
            Console.WriteLine("[S004AirSelfHelipadResolverControl] result=Passed cases=2 fixtureOnly=1 queue=0 delivery=0 readiness=0");
        } finally { blocked.Dispose();occupied.Dispose(); }
    }
}
