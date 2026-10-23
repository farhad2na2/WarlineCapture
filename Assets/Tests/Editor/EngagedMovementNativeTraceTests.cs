#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    // A composed native-system subset, not a normal-input mission or a result gate.
    public sealed class EngagedMovementNativeTraceTests
    {
        [Serializable] private sealed class Row
        {
            public int frame,actorHp,firstHp,secondHp,targetIndex,targetVersion;
            public float time;public float[] actorPosition,cachedTargetPosition,firstPosition,secondPosition;
            public int[] actorCell;
            public bool engaged,pathRequest,firstFrozen,secondFrozen,chainComplete;
        }
        [Serializable] private sealed class Trace
        {
            public string profile="IsolatedEngagementChaseDeathTraceV1",scope="NativeSortedSubset_NoPathOrObjectives";
            public bool full_world=false,mission_terminal=false;
            public string[] systems;public float delta=.25f,cell_size=1,attack_range=2,speed=3,cooldown=.5f;
            public int damage=3;public float[] actorStart={10.5f,0,10.5f},firstStart={17.5f,0,10.5f},secondStart={24.5f,0,10.5f};
            public float firstSpeed=1,firstAttackRange=.1f;public List<Row> rows=new List<Row>();
        }
        public static int RunChecks(){new EngagedMovementNativeTraceTests().ActualSortedSyncChaseAttackDeathAndReacquisition();return 1;}
        private static float[] Point(float3 p)=>new[]{p.x,p.y,p.z};
        private static Entity Unit(EntityManager em,float x,byte faction,bool attack)
        {
            var e=em.CreateEntity(typeof(LocalTransform),typeof(UnitGrid),typeof(Faction),typeof(UnitHealth),typeof(UnitCombat),typeof(UnitAttack),typeof(UnitMove),typeof(UnitFootprint),typeof(UnitMovementBehavior),typeof(UnitVehicleMovement),typeof(UnitVehicleKinematics),typeof(UnitAttackCooldownComponent),typeof(UnitAttackTraceComponent),typeof(UnitAttackAnimationComponent),typeof(UnitAnimationSettings));
            em.SetComponentData(e,LocalTransform.FromPosition(new float3(x,0,10.5f)));em.SetComponentData(e,new UnitGrid{Cell=new int2((int)x,10)});em.SetComponentData(e,new Faction{Id=faction});em.SetComponentData(e,new UnitHealth{Current=attack?20:3,Max=attack?20:3});
            em.SetComponentData(e,new UnitCombat{CanAttack=(byte)(attack?1:0),AutoEngage=1,AggroRangeCells=20,ChaseBreakDistance=30});em.SetComponentData(e,new UnitAttack{Range=2,CooldownSeconds=.5f,Damage=3});
            em.SetComponentData(e,new UnitMove{Speed=3,RoadSpeedMultiplier=1});em.SetComponentData(e,new UnitFootprint{Size=new int2(1,1)});em.SetComponentData(e,new UnitAnimationSettings{AttackAnimationSeconds=.25f,DeathAnimationSeconds=.1f});return e;
        }
        private static bool Frozen(EntityManager em,Entity e)=>em.HasComponent<UnitDeathAnimationComponent>(e)&&em.GetComponentData<UnitDeathAnimationComponent>(e).PoseFrozen!=0;
        [Test] public void ActualSortedSyncChaseAttackDeathAndReacquisition()
        {
            using var world=new World("EngagedComposedNativeSubset");var em=world.EntityManager;const int size=32*32;
            using var blocked=new NativeBitArray(size,Allocator.Persistent);using var friendly=new NativeArray<byte>(size,Allocator.Persistent);using var counts=new NativeArray<int>(size,Allocator.Persistent);
            var grid=em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent));em.SetComponentData(grid,new GridConfig{Width=32,Height=32,CellSize=1});em.SetComponentData(grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
            em.AddBuffer<GridWalkable>(grid);em.AddBuffer<GridRoad>(grid);em.AddBuffer<GridRoadSidewalk>(grid);em.AddBuffer<GridRoadDirt>(grid);
            for(int i=0;i<size;i++){em.GetBuffer<GridWalkable>(grid).Add(new GridWalkable{Value=1});em.GetBuffer<GridRoad>(grid).Add(default);em.GetBuffer<GridRoadSidewalk>(grid).Add(default);em.GetBuffer<GridRoadDirt>(grid).Add(default);}
            var actor=Unit(em,10.5f,1,true);var first=Unit(em,17.5f,2,false);var second=Unit(em,24.5f,2,false);
            em.SetComponentData(first,new UnitMove{Speed=1,RoadSpeedMultiplier=1});em.SetComponentData(first,new UnitAttack{Range=.1f,Damage=0,CooldownSeconds=.5f});
            em.AddComponentData(first,new EngageTarget{Target=actor,Cell=new int2(10,10),Position=new float3(10.5f,0,10.5f),IsCommanded=1});
            var group=world.GetOrCreateSystemManaged<SimulationSystemGroup>();Type[] types={typeof(BeginSimulationEntityCommandBufferSystem),typeof(EngageTargetValidateSystem),typeof(UnitEngagementSystem),typeof(EngageTargetSyncSystem),typeof(UnitEngagedMovementSystem),typeof(UnitAttackSystem),typeof(UnitDeathSystem),typeof(EndSimulationEntityCommandBufferSystem)};
            foreach(var type in types)group.AddSystemToUpdateList(world.GetOrCreateSystem(type));group.SortSystems();using var handles=group.GetAllSystems();var order=new string[handles.Length];for(int i=0;i<handles.Length;i++)order[i]=TypeManager.GetSystemName(world.Unmanaged.GetSystemTypeIndex(handles[i])).ToString();
            void Before(Type a,Type b)=>Assert.Less(Array.IndexOf(order,a.FullName),Array.IndexOf(order,b.FullName),a.Name+" before "+b.Name);
            Before(typeof(EngageTargetValidateSystem),typeof(UnitEngagementSystem));Before(typeof(UnitEngagementSystem),typeof(EngageTargetSyncSystem));Before(typeof(EngageTargetSyncSystem),typeof(UnitEngagedMovementSystem));Before(typeof(UnitEngagedMovementSystem),typeof(UnitAttackSystem));Before(typeof(UnitAttackSystem),typeof(UnitDeathSystem));Before(typeof(UnitDeathSystem),typeof(EndSimulationEntityCommandBufferSystem));
            var trace=new Trace{systems=order};bool sawSecond=false,sawChase=false,sawDeath=false,complete=false;
            for(int frame=0;frame<48;frame++)
            {
                world.SetTime(new TimeData((frame+1)*.25,.25f));group.Update();em.CompleteAllTrackedJobs();
                var transform=em.GetComponentData<LocalTransform>(actor);var cell=em.GetComponentData<UnitGrid>(actor).Cell;bool engaged=em.HasComponent<EngageTarget>(actor);var engage=engaged?em.GetComponentData<EngageTarget>(actor):default;
                var row=new Row{frame=frame,time=(frame+1)*.25f,actorPosition=Point(transform.Position),actorCell=new[]{cell.x,cell.y},actorHp=em.GetComponentData<UnitHealth>(actor).Current,firstHp=em.GetComponentData<UnitHealth>(first).Current,secondHp=em.GetComponentData<UnitHealth>(second).Current,
                    engaged=engaged,targetIndex=engage.Target.Index,targetVersion=engage.Target.Version,cachedTargetPosition=Point(engage.Position),firstPosition=Point(em.GetComponentData<LocalTransform>(first).Position),secondPosition=Point(em.GetComponentData<LocalTransform>(second).Position),pathRequest=em.HasComponent<UnitPathRequest>(actor),firstFrozen=Frozen(em,first),secondFrozen=Frozen(em,second)};
                row.chainComplete=row.firstFrozen&&row.secondFrozen&&!row.engaged;trace.rows.Add(row);
                if(frame==0){Assert.IsTrue(engaged,"acquisition visible after EndSimulation playback");Assert.AreEqual(first,engage.Target);Assert.AreEqual(10.5f,transform.Position.x,"new deferred engagement cannot move or attack until next frame");Assert.AreEqual(3,row.firstHp);}
                if(frame==1){Assert.AreEqual(trace.rows[0].firstPosition[0],engage.Position.x,"sync observes previous native target movement before current chase");Assert.Less(row.firstPosition[0],17.5f,"target moves through actual EngagedMovement");Assert.Greater(transform.Position.x,10.5f);}
                Assert.AreEqual(GridUtils.WorldToCell(em.GetComponentData<GridConfig>(grid),transform.Position),cell,"actual movement keeps grid synchronized");
                sawChase|=transform.Position.x>10.5f;sawSecond|=engaged&&engage.Target==second;sawDeath|=row.firstFrozen;
                if(row.chainComplete){complete=true;break;}
            }
            Debug.Log("[EngagedMovementNativeTrace] "+JsonUtility.ToJson(trace));Assert.IsTrue(sawChase);Assert.IsTrue(sawDeath);Assert.IsTrue(sawSecond,"invalidated dead target must permit second acquisition");Assert.IsTrue(complete,"bounded chain requires two native mobile deaths, frozen corpse pose and cleared engagement");
            Debug.Log("[EngagedMovementComposedParity] result=Passed profile=IsolatedEngagementChaseDeathTraceV1 path=NotIncluded objectives=NotIncluded missionReady=0");
        }
    }
}
#endif
