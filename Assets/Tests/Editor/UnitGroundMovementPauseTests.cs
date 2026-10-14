#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Owned small ECS Worlds only. No Editor Play, live actors, assets, or mission state touched.
// Real shared systems; paused state conservation and positive-time compatibility.
namespace Game.Tests.Editor
{
public sealed class UnitGroundMovementPauseTests
{
    [Test]
    public void NativeMovementConservesPausedStateAndRetainsPositiveTimeBehavior()
    {
        Assert.That(RunFocusedValidation().GetAwaiter().GetResult(), Is.EqualTo(0));
    }

    [Serializable] struct Row
    {
        public string scenario,system,mode;public float delta,positionDelta,rotationDelta;
        public bool simulationActive,missingGameplayState,transformChanged,gridChanged,kinematicsChanged,pathChanged,targetChanged,positiveControl;
        public int originalPathIndex,currentPathIndex;
    }
    sealed class Fixture:IDisposable
    {
        public readonly World World=new World("GroundPause-native-owned");
        public EntityManager Em=>World.EntityManager;
        public Entity Actor,Target,Gate,GridEntity;
        public NativeArray<int> Counts;public NativeArray<byte> Friendly;
        public NativeBitArray Blocked,Occupied;public NativeList<int2> Pool;
        public SystemHandle System;public EndSimulationEntityCommandBufferSystem End;
        public Fixture(bool engaged,bool vehicle,bool near,bool inRange)
        {
            const int width=12,height=8,size=width*height;
            Counts=new NativeArray<int>(size,Allocator.Persistent);
            Friendly=new NativeArray<byte>(size,Allocator.Persistent);
            Blocked=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            Occupied=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            Pool=new NativeList<int2>(4,Allocator.Persistent);
            for(int i=0;i<size;i++)Friendly[i]=byte.MaxValue;
            GridEntity=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
            Em.SetComponentData(GridEntity,new GridConfig{Width=width,Height=height,CellSize=1,Origin=float3.zero});
            Em.SetComponentData(GridEntity,new DynamicBlockerComponent{GridSize=size,Counts=Counts,Blocked=Blocked,FriendlyPassFactionIds=Friendly});
            Em.SetComponentData(GridEntity,new DynamicOccupancyComponent{Occupied=Occupied});
            Pool.Add(new int2(6,2));Em.SetComponentData(GridEntity,new PathPoolComponent{Cells=Pool});
            Em.AddBuffer<GridWalkable>(GridEntity);Em.AddBuffer<GridRoad>(GridEntity);Em.AddBuffer<GridRoadSidewalk>(GridEntity);Em.AddBuffer<GridRoadDirt>(GridEntity);
            var w=Em.GetBuffer<GridWalkable>(GridEntity);var r=Em.GetBuffer<GridRoad>(GridEntity);var s=Em.GetBuffer<GridRoadSidewalk>(GridEntity);var d=Em.GetBuffer<GridRoadDirt>(GridEntity);
            for(int i=0;i<size;i++){w.Add(new GridWalkable{Value=1});r.Add(default);s.Add(default);d.Add(default);}
            Gate=Em.CreateEntity(typeof(RuntimeGameplayStateComponent));Em.SetComponentData(Gate,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=0});
            Actor=Em.CreateEntity(typeof(Faction),typeof(UnitGrid),typeof(UnitFootprint),typeof(UnitMove),typeof(UnitMovementBehavior),typeof(UnitVehicleMovement),typeof(UnitVehicleKinematics),typeof(UnitHealth),typeof(LocalTransform));
            Em.SetComponentData(Actor,new Faction{Id=1});Em.SetComponentData(Actor,new UnitHealth{Current=100,Max=100});
            var pos=near?new float3(6.2f,0,2.5f):new float3(1.5f,0,2.5f);
            Em.SetComponentData(Actor,new UnitGrid{Cell=GridUtils.WorldToCell(Em.GetComponentData<GridConfig>(GridEntity),pos)});
            Em.SetComponentData(Actor,new UnitFootprint{Size=new int2(1,1)});
            Em.SetComponentData(Actor,new UnitMove{Speed=2,WalkSpeed=2,RoadSpeedMultiplier=1,ArriveDistance=.05f});
            Em.SetComponentData(Actor,new UnitMovementBehavior{UsesVehicleMotion=(byte)(vehicle?1:0),AllowIdleWander=0});
            Em.SetComponentData(Actor,new UnitVehicleMovement{TurnSpeedDegrees=180,Acceleration=20,Braking=20});
            Em.SetComponentData(Actor,new UnitVehicleKinematics{CurrentSpeed=3,StallSeconds=.1f});
            Em.SetComponentData(Actor,LocalTransform.FromPositionRotation(pos,vehicle?quaternion.RotateY(math.PI/2):quaternion.identity));
            if(engaged)
            {
                Target=Em.CreateEntity(typeof(UnitHealth),typeof(UnitFootprint));Em.SetComponentData(Target,new UnitHealth{Current=100,Max=100});Em.SetComponentData(Target,new UnitFootprint{Size=new int2(1,1)});
                Em.AddComponentData(Actor,new UnitCombat{CanAttack=1,AutoEngage=1,AggroRangeCells=10,ChaseBreakDistance=30});
                Em.AddComponentData(Actor,new UnitAttack{Range=inRange?6:.1f,CooldownSeconds=1,Damage=1});
                Em.AddComponentData(Actor,new EngageTarget{Target=Target,Cell=new int2(6,2),Position=new float3(6.5f,0,2.5f),IsCommanded=1});
                System=World.CreateSystem<UnitEngagedMovementSystem>();
            }
            else
            {
                Em.AddComponentData(Actor,new UnitPathFollow{PathIndex=0});Em.AddComponentData(Actor,new UnitPathRange{Start=0,Length=1});Em.AddComponentData(Actor,new UnitTarget{Cell=new int2(6,2)});
                End=World.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();System=World.CreateSystem<UnitGridMovementSystem>();
            }
        }
        public void Step(bool active,float delta,bool missingGate){if(missingGate)Em.DestroyEntity(Gate);else Em.SetComponentData(Gate,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=(byte)(active?1:0)});World.SetTime(new TimeData(100d+delta,delta));System.Update(World.Unmanaged);Em.CompleteAllTrackedJobs();End?.Update();Em.CompleteAllTrackedJobs();}
        public void Dispose(){Em.CompleteAllTrackedJobs();World.Dispose();Pool.Dispose();Occupied.Dispose();Blocked.Dispose();Friendly.Dispose();Counts.Dispose();}
    }
    public static Task<int> RunFocusedValidation()
    {
        Case("EngagedInfantryInRange",true,false,false,true,false,0,false);
        Case("EngagedInfantryChase",true,false,false,false,false,0,false);
        Case("GridInfantryZeroDelta",false,false,false,false,false,0,false);
        Case("GridVehicleZeroDelta",false,true,false,false,false,0,false);
        Case("GridVehicleNearArrivalCleanup",false,true,true,false,false,0,false);
        Case("EngagedActiveZeroDelta",true,false,false,false,true,0,false);
        Case("EngagedChasePositiveDelta",true,false,false,false,true,.25f,true);
        Case("GridInfantryPositiveDelta",false,false,false,false,true,.25f,true);
        Case("GridVehiclePositiveDelta",false,true,false,false,true,.25f,true);
        Case("EngagedInRangePositiveDeltaFacing",true,false,false,true,true,.25f,true);
        Case("EngagedInactivePositiveDelta",true,false,false,false,false,.25f,false);
        Case("GridInfantryInactivePositiveDelta",false,false,false,false,false,.25f,false);
        Case("GridVehicleNearArrivalInactivePositiveDelta",false,true,true,false,false,.25f,false);
        Case("EngagedMissingGameplayPositiveDelta",true,false,false,false,true,.25f,true,true);
        Case("GridInfantryMissingGameplayPositiveDelta",false,false,false,false,true,.25f,true,true);
        Case("GridVehicleMissingGameplayPositiveDelta",false,true,false,false,true,.25f,true,true);
        Case("EngagedMissingGameplayZeroDelta",true,false,false,false,true,0,false,true);
        Case("GridNearArrivalMissingGameplayZeroDelta",false,true,true,false,true,0,false,true);
        Debug.Log("[UnitGroundMovementPauseTests] result=Passed actualSystems=2 cases=18 conservationCases=11 positiveControls=7 inactivePositiveCases=3 missingGameplayCases=5 missionFailureCauseCertified=0");
        return Task.FromResult(0);
    }
    static bool Case(string name,bool engaged,bool vehicle,bool near,bool inRange,bool active,float delta,bool positive,bool missingGate=false)
    {
        using var f=new Fixture(engaged,vehicle,near,inRange);var em=f.Em;
        var before=em.GetComponentData<LocalTransform>(f.Actor);var grid=em.GetComponentData<UnitGrid>(f.Actor);var motion=em.GetComponentData<UnitVehicleKinematics>(f.Actor);
        bool path=em.HasComponent<UnitPathFollow>(f.Actor),target=em.HasComponent<UnitTarget>(f.Actor);int index=path?em.GetComponentData<UnitPathFollow>(f.Actor).PathIndex:-1;
        f.Step(active,delta,missingGate);
        var after=em.GetComponentData<LocalTransform>(f.Actor);bool hasPath=em.HasComponent<UnitPathFollow>(f.Actor);int nowIndex=hasPath?em.GetComponentData<UnitPathFollow>(f.Actor).PathIndex:-1;
        var row=new Row{scenario=name,system=engaged?"UnitEngagedMovementSystem":"UnitGridMovementSystem",mode="Conservation",delta=delta,simulationActive=active,missingGameplayState=missingGate,positionDelta=math.distance(before.Position,after.Position),rotationDelta=math.distance(before.Rotation.value,after.Rotation.value),transformChanged=!before.Equals(after),gridChanged=!grid.Equals(em.GetComponentData<UnitGrid>(f.Actor)),kinematicsChanged=!motion.Equals(em.GetComponentData<UnitVehicleKinematics>(f.Actor)),pathChanged=path!=hasPath||index!=nowIndex,targetChanged=target!=em.HasComponent<UnitTarget>(f.Actor),originalPathIndex=index,currentPathIndex=nowIndex,positiveControl=positive};
        Debug.Log("[UnitGroundMovementPauseTestsCase] "+JsonUtility.ToJson(row));
        if(positive){if(!row.transformChanged||(inRange?(row.positionDelta!=0||row.rotationDelta<=.01f):row.positionDelta<=.01f))throw new InvalidOperationException("Positive native control did not perform intended facing/movement:"+name);return false;}
        if(row.transformChanged||row.gridChanged||row.kinematicsChanged||row.pathChanged||row.targetChanged)
            throw new InvalidOperationException("Paused native movement state changed:"+name);
        return row.transformChanged;
    }
}
}

#endif
