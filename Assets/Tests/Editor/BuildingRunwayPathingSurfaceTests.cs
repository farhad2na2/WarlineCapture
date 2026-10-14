using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class BuildingRunwayPathingSurfaceTests
{
    sealed class Fixture : IDisposable
    {
        internal readonly World World = new World("actual-runway-blocker-path");
        internal EntityManager Em => World.EntityManager;
        internal Entity Grid, Airport;
        internal NativeArray<int> Counts;
        internal NativeArray<byte> Pass;
        internal NativeBitArray Blocked, Occupied;
        internal NativeList<int2> Paths;
        internal readonly SystemHandle Update;
        internal readonly StaticGridBlockerSurfaceOpeningComponent Surface = new()
        { Center = new float2(36,32), Right = new float2(1,0), Forward = new float2(0,1), HalfExtents = new float2(6,28) };
        internal Fixture(bool opening = true)
        {
            const int n=64*64;
            Counts=new NativeArray<int>(n,Allocator.Persistent);Pass=new NativeArray<byte>(n,Allocator.Persistent);
            for(int i=0;i<n;i++)Pass[i]=byte.MaxValue;
            Blocked=new NativeBitArray(n,Allocator.Persistent);Occupied=new NativeBitArray(n,Allocator.Persistent);Paths=new NativeList<int2>(Allocator.Persistent);
            Grid=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
            Em.SetComponentData(Grid,new GridConfig{Width=64,Height=64,CellSize=1,Origin=float3.zero});
            Em.SetComponentData(Grid,new DynamicBlockerComponent{GridSize=n,Counts=Counts,Blocked=Blocked,FriendlyPassFactionIds=Pass});
            Em.SetComponentData(Grid,new DynamicOccupancyComponent{GridSize=n,Occupied=Occupied});Em.SetComponentData(Grid,new PathPoolComponent{Cells=Paths});
            Em.AddBuffer<GridWalkable>(Grid);Em.AddBuffer<GridRoad>(Grid);Em.AddBuffer<GridRoadSidewalk>(Grid);Em.AddBuffer<GridRoadDirt>(Grid);
            var walk=Em.GetBuffer<GridWalkable>(Grid);walk.ResizeUninitialized(n);for(int i=0;i<n;i++)walk[i]=new GridWalkable{Value=1};
            Em.GetBuffer<GridRoad>(Grid).Resize(n,NativeArrayOptions.ClearMemory);Em.GetBuffer<GridRoadSidewalk>(Grid).Resize(n,NativeArrayOptions.ClearMemory);Em.GetBuffer<GridRoadDirt>(Grid).Resize(n,NativeArrayOptions.ClearMemory);
            var runtime=Em.CreateEntity(typeof(RuntimeGameplayStateComponent));Em.SetComponentData(runtime,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=1});
            Airport=Block(new int2(8,4),new int2(40,56));if(opening)Em.AddComponentData(Airport,Surface);
            World.SetTime(new TimeData(1,1f/60));Update=World.CreateSystem<StaticGridBlockerUpdateSystem>();
        }
        internal Entity Block(int2 origin,int2 size)
        {
            var e=Em.CreateEntity(typeof(UnitGrid),typeof(GridBlockerSize),typeof(StaticGridBlocker));Em.SetComponentData(e,new UnitGrid{Cell=origin});Em.SetComponentData(e,new GridBlockerSize{Size=size});return e;
        }
        internal void Tick()=>Update.Update(World.Unmanaged);
        internal void Cell(int x,int z,int count){Assert.AreEqual(count,Counts[z*64+x]);Assert.AreEqual(count>0,Blocked.IsSet(z*64+x));}
        public void Dispose(){World.Dispose();Counts.Dispose();Pass.Dispose();Blocked.Dispose();Occupied.Dispose();Paths.Dispose();}
    }
    public static void RunFocusedValidation()
    {
        var t=new BuildingRunwayPathingSurfaceTests();
        t.RunwayLeavesAirportBodyAndOverlappingBlockerSolid();
        t.OpeningMovementAndRemovalSubtractExactlyOnce();
        t.DestroyedAirportRetainsForeignBlockerAndNoNegativeCounts();
        t.MalformedOpeningFailsClosed();
        t.QuarterTurnOpeningUsesFullCellGeometry();
        t.NativeBoardProducesFourDistinctRampGoalsAndNativeConnectedPaths();
        t.ActualAirportPrefabReconstructsSurfaceWithoutChangingPlacementFootprint();
        t.NativeBoardRejectsSealedAccessAndEnemyTransport();
        t.DiagonalEdgesDoNotOpenPartiallyCoveredCells();
        Debug.Log("[BuildingRunwayPathingSurfaceTests] result=Passed cases=9 scope=IsolatedActualSystemNotNormalMission");
    }
    [Test] public void RunwayLeavesAirportBodyAndOverlappingBlockerSolid()
    {
        using var f=new Fixture();var obstacle=f.Block(new int2(35,38),new int2(2,2));f.Tick();f.Cell(36,32,0);f.Cell(20,32,1);f.Cell(35,38,1);
        Assert.IsFalse(f.Em.HasComponent<FriendlyPassGridBlocker>(f.Airport));Assert.AreEqual(byte.MaxValue,f.Pass[32*64+36]);
        f.Em.DestroyEntity(obstacle);f.Tick();f.Cell(35,38,0);f.Cell(20,32,1);
    }
    [Test] public void OpeningMovementAndRemovalSubtractExactlyOnce()
    {
        using var f=new Fixture();f.Tick();var moved=f.Surface;moved.Center.x=18;f.Em.SetComponentData(f.Airport,moved);f.Tick();f.Cell(36,32,1);f.Cell(18,32,0);
        f.Em.RemoveComponent<StaticGridBlockerSurfaceOpeningComponent>(f.Airport);f.Tick();f.Cell(18,32,1);f.Cell(36,32,1);f.Tick();f.Cell(18,32,1);
    }
    [Test] public void DestroyedAirportRetainsForeignBlockerAndNoNegativeCounts()
    {
        using var f=new Fixture();var obstacle=f.Block(new int2(35,38),new int2(2,2));f.Tick();f.Em.DestroyEntity(f.Airport);f.Tick();f.Cell(20,32,0);f.Cell(35,38,1);f.Tick();f.Cell(35,38,1);
        Assert.IsFalse(f.Em.Exists(f.Airport));foreach(var n in f.Counts)Assert.GreaterOrEqual(n,0);
        f.Em.DestroyEntity(obstacle);f.Tick();f.Cell(35,38,0);
    }
    [Test] public void MalformedOpeningFailsClosed()
    {
        using var f=new Fixture();var bad=f.Surface;bad.Right=float2.zero;f.Em.SetComponentData(f.Airport,bad);f.Tick();f.Cell(36,32,1);
        bad=f.Surface;bad.Center.x=float.NaN;f.Em.SetComponentData(f.Airport,bad);f.Tick();f.Cell(36,32,1);f.Em.DestroyEntity(f.Airport);f.Tick();f.Cell(36,32,0);
    }
    [Test] public void QuarterTurnOpeningUsesFullCellGeometry()
    {
        using var f=new Fixture();var rotated=f.Surface;rotated.Right=new float2(0,1);rotated.Forward=new float2(-1,0);rotated.Center=new float2(28,32);f.Em.SetComponentData(f.Airport,rotated);f.Tick();f.Cell(20,32,0);f.Cell(28,16,1);f.Cell(20,4,1);
    }
    static Entity Passenger(Fixture f,int2 cell)
    {
        var e=f.Em.CreateEntity(typeof(Faction),typeof(UnitGrid),typeof(UnitFootprint),typeof(UnitMove),typeof(UnitMovementBehavior),typeof(UnitMoveVisualComponent),typeof(SelectedUnitTag),typeof(LocalTransform),typeof(UnitHealth));
        f.Em.SetComponentData(e,new Faction{Id=1});f.Em.SetComponentData(e,new UnitGrid{Cell=cell});f.Em.SetComponentData(e,new UnitFootprint{Size=new int2(1,1)});f.Em.SetComponentData(e,new UnitMove{Speed=4,WalkSpeed=1.5f,RoadSpeedMultiplier=1,ArriveDistance=.05f});f.Em.SetComponentData(e,new UnitMovementBehavior());f.Em.SetComponentData(e,LocalTransform.FromPosition(new float3(cell.x+.5f,0,cell.y+.5f)));f.Em.SetComponentData(e,new UnitHealth{Current=100,Max=100});return e;
    }
    static Entity Plane(Fixture f,byte faction=1)
    {
        var e=f.Em.CreateEntity(typeof(Faction),typeof(UnitGrid),typeof(UnitFootprint),typeof(UnitTransportCapacity),typeof(UnitSourcePrefabKey),typeof(LocalToWorld),typeof(LocalTransform),typeof(UnitAirMovement),typeof(UnitAirComponent),typeof(UnitHealth));
        var pos=new float3(36.5f,0,45.5f);f.Em.SetComponentData(e,new Faction{Id=faction});f.Em.SetComponentData(e,new UnitGrid{Cell=new int2(36,45)});f.Em.SetComponentData(e,new UnitFootprint{Size=new int2(1,1)});f.Em.SetComponentData(e,new UnitTransportCapacity{SoldierCapacity=24});f.Em.SetComponentData(e,new UnitSourcePrefabKey{Value="Unit_Veh_Plane_Transport"});f.Em.SetComponentData(e,LocalTransform.FromPosition(pos));f.Em.SetComponentData(e,new LocalToWorld{Value=float4x4.Translate(pos)});f.Em.SetComponentData(e,new UnitAirMovement{CruiseHeight=8,RunwayTaxiSpeed=5});f.Em.SetComponentData(e,new UnitAirComponent{HomeInitialized=1,HomePosition=pos,HomeCell=new int2(36,45)});f.Em.SetComponentData(e,new UnitHealth{Current=340,Max=340});f.Em.AddBuffer<UnitTransportPassengerElement>(e);f.Em.AddComponentData(e,new UnitTransportCargoCapacity{SoldierCapacity=24,VehicleCapacity=2});
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab");Assert.NotNull(prefab);Transform door=null;foreach(var child in prefab.GetComponentsInChildren<Transform>(true))if(child.name.StartsWith("Door_",StringComparison.Ordinal)){door=child;break;}Assert.NotNull(door);var root=prefab.transform.InverseTransformPoint(door.position);root.x=0;
        f.Em.AddComponentData(e,new UnitTransportPlaneDoorReference{DoorEntity=Entity.Null,DoorLocalPosition=root,ApproachLocalPosition=(float3)root+new float3(0,0,-6),InteriorLocalPosition=(float3)root+new float3(0,1.45f,9.5f),RolloutLocalPosition=(float3)root+new float3(0,0,-6),OpenSeconds=2.5f});return e;
    }
    [Test] public void NativeBoardProducesFourDistinctRampGoalsAndNativeConnectedPaths()
    {
        using var f=new Fixture();var plane=Plane(f);var passengers=new List<Entity>();for(int i=0;i<4;i++)passengers.Add(Passenger(f,new int2(51+i,2)));f.Tick();
        var result=new TransportBoardingCommandSystem().TryIssueBoardTransportOrderToTransport(f.Em,plane,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem(),null);Assert.IsTrue(result.Accepted);
        var goals=new HashSet<int2>();foreach(var p in passengers){Assert.IsTrue(f.Em.HasComponent<UnitTransportBoardingTarget>(p));var target=f.Em.GetComponentData<UnitTransportBoardingTarget>(p);Assert.AreEqual(plane,target.Transport);Assert.IsTrue(goals.Add(target.Goal));Assert.IsTrue(f.Surface.ContainsFullCell(f.Em.GetComponentData<GridConfig>(f.Grid),target.Goal.x,target.Goal.y));Assert.IsFalse(f.Blocked.IsSet(target.Goal.y*64+target.Goal.x));}
        var path=f.World.CreateSystem<UnitPathfindingSystem>();var due=Stopwatch.StartNew();
        bool complete=false;while(due.Elapsed.TotalSeconds<5){path.Update(f.World.Unmanaged);complete=true;foreach(var p in passengers)complete&=f.Em.HasComponent<UnitPathRange>(p)&&f.Em.GetComponentData<UnitPathRange>(p).Length>0;if(complete)break;System.Threading.Thread.Yield();}
        Assert.IsTrue(complete,"Native path scheduler must establish actual paths from outside the Airport to all four ramp goals.");
        foreach(var p in passengers){var range=f.Em.GetComponentData<UnitPathRange>(p);var goal=f.Em.GetComponentData<UnitTransportBoardingTarget>(p).Goal;Assert.AreEqual(goal,f.Paths[range.Start+range.Length-1]);for(int i=0;i<range.Length;i++){var cell=f.Paths[range.Start+i];Assert.IsFalse(f.Blocked.IsSet(cell.y*64+cell.x));}}
    }
    [Test] public void ActualAirportPrefabReconstructsSurfaceWithoutChangingPlacementFootprint()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Buildings/Building_Airport.prefab");Assert.NotNull(prefab);
        var instance=UnityEngine.Object.Instantiate(prefab);
        try
        {
            instance.transform.SetPositionAndRotation(new Vector3(317,0,409),Quaternion.Euler(0,90,0));instance.transform.localScale=Vector3.one*1.2f;
            Assert.IsTrue(BuildingRunwaySystem.TryResolvePrefabRunwayLocalData(prefab,out var local,out var rotation,out var extents));
            var definition=new BuildingDefinition{Prefab=prefab,DisplayName="Airport",HasRunway=true,RunwayLocalPosition=local,RunwayLocalRotation=rotation,RunwayHalfExtents=extents,FootprintCells=new Vector2Int(55,210)};
            using var f=new Fixture(false);var building=new RuntimeBuildingEntity{Definition=definition,Instance=instance,BlockerEntity=f.Airport,OriginCell=new Vector2Int(8,4)};
            BuildingRunwayPathingSurface.Attach(f.Em,building);Assert.IsTrue(f.Em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(f.Airport));var original=f.Em.GetComponentData<StaticGridBlockerSurfaceOpeningComponent>(f.Airport);
            Assert.AreEqual(new Vector2Int(55,210),definition.FootprintCells);Assert.AreEqual(new int2(40,56),f.Em.GetComponentData<GridBlockerSize>(f.Airport).Size);
            f.Em.RemoveComponent<StaticGridBlockerSurfaceOpeningComponent>(f.Airport);BuildingRunwayPathingSurface.Attach(f.Em,building);Assert.IsTrue(original.SameGeometry(f.Em.GetComponentData<StaticGridBlockerSurfaceOpeningComponent>(f.Airport)));
            definition.Prefab=null;Assert.IsFalse(BuildingRunwayPathingSurface.TryResolve(building,out _));
        }
        finally{UnityEngine.Object.DestroyImmediate(instance);}
    }
    [Test] public void NativeBoardRejectsSealedAccessAndEnemyTransport()
    {
        using var f=new Fixture(false);var plane=Plane(f);var p=Passenger(f,new int2(51,2));f.Tick();var commands=new TransportBoardingCommandSystem();var result=commands.TryIssueBoardTransportOrderToTransport(f.Em,plane,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem(),null);Assert.IsFalse(result.Accepted);Assert.IsFalse(f.Em.HasComponent<UnitTransportBoardingTarget>(p));
        f.Em.AddComponentData(f.Airport,f.Surface);f.Tick();f.Em.SetComponentData(plane,new Faction{Id=2});result=commands.TryIssueBoardTransportOrderToTransport(f.Em,plane,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem(),null);Assert.IsFalse(result.Accepted);Assert.IsFalse(f.Em.HasComponent<UnitTransportBoardingTarget>(p));
    }
    [Test] public void DiagonalEdgesDoNotOpenPartiallyCoveredCells()
    {
        using var f=new Fixture();var surface=f.Surface;float q=math.sqrt(.5f);surface.Center=new float2(32,32);surface.Right=new float2(q,q);surface.Forward=new float2(-q,q);surface.HalfExtents=new float2(2,10);f.Em.SetComponentData(f.Airport,surface);f.Tick();f.Cell(32,32,0);f.Cell(33,32,1);Assert.IsTrue(math.abs(math.dot(new float2(1.5f,.5f),surface.Right))<surface.HalfExtents.x);
    }
}
