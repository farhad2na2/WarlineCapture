#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Movement;
using Game.Runtime.Pathfinding;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    [DisableAutoCreation] public partial class InfantryPathLookupHarness : SystemBase
    {
        protected override void OnUpdate() { }
        public UnitGridMoveJob Build() => new UnitGridMoveJob { AutoWanderLookup=GetComponentLookup<AutoWanderMoveTag>(true),
                UnitTargetLookup=GetComponentLookup<UnitTarget>(true),
                LongDistanceMoveLookup=GetComponentLookup<UnitLongDistanceMove>(true),
                ManualMoveGroupLookup=GetComponentLookup<ManualMoveGroupMemberTag>(true),
                ManualMoveLookup=GetComponentLookup<ManualMoveOrderTag>(true),
                CampaignGuidedMoveLookup=GetComponentLookup<CampaignMissionGuidedMoveInProgressTag>(true),
                BoardingTargetLookup=GetComponentLookup<UnitTransportBoardingTarget>(true) };
        public ComponentLookup<UnitGrid> Grids() => GetComponentLookup<UnitGrid>(true);
    }
    public sealed class InfantryPathLifecycleKernelTests
    {
        public static int RunChecks()
        {
            var t=new InfantryPathLifecycleKernelTests(); t.PinnedOriginals();t.NativeManagedAndBurstMoveParity();
            t.NativeCompletionAndDeferredParity();t.ResultAndRetryIntents();t.NativePathResultApplyParity();t.NativeGoalIdentityAndSegmentPolicyParity();
            Debug.Log("[InfantryPathLifecycleParity] result=Passed tests=6 moveCases=224 completionCases=128 orderedEcb=Passed burst=Passed missionReady=0");return 6;
        }
        [Test] public void PinnedOriginals()
        {
            string[] names={"UnitGridMovementSystem","UnitGridMovementPolicy","UnitPathResultApply","UnitPathRetry"};
            string[] hashes={"4b8da9c4558a2e076c4c5c3b2045f5b0815c6a845b3616b62669bb0d50f41c22","fe4cfe5941cd9f0526e0f0e5f7d2a1e48ac047be4e44baabd425e0e59edecb2f","a55b63fb6524feadeace413a5d66a81f7cec5478e317485c404bb9b4c01153ae","7bde00d3418eee6117eb16679264f6fdf1d18d45c8dfbdb1118392ddfaec5531"};
            for(int i=0;i<names.Length;i++)
            {
                string source=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/InfantryPathLifecycle"+names[i]+"Original.cs.txt")).Replace("\r\n","\n");
                using var sha=SHA256.Create();Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source))).Replace("-","").ToLowerInvariant());
                if(i<2)
                {
                    if(i==0)source=source.Substring(0,source.IndexOf("    [BurstCompile]\n    [UpdateInGroup",StringComparison.Ordinal))+"}\n";
                    source=source.Replace("UnitGridMoveJob","InfantryPathOriginalMoveJob").Replace("UnitPathFollowCleanupJob","InfantryPathOriginalCleanupJob");
                    string expected="#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+source+"#endif\n";
                    Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/InfantryPathOriginal"+(i==0?"System":"Policy")+".cs")).Replace("\r\n","\n"));
                }
                else
                {
                    string expected="#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+System.Text.RegularExpressions.Regex.Replace(source.Replace("UnitPathResultApply","InfantryPathOriginalResultApply"),@"\bUnitPathRetry\b","InfantryPathOriginalRetry")+"#endif\n";
                    Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/InfantryPathOriginal"+(i==2?"ResultApply":"Retry")+".cs")).Replace("\r\n","\n"));
                }
            }
        }
        private struct GoalProbe : IPathGoalIdentityContext
        {
            public int Width => 16; public int Height => 16;
            public bool Allowed; public int Calls, Reservations;
            public bool CanUseGoal(PlacementCell cell) { Calls++; return Allowed; }
            public void ReserveGoal(PlacementCell cell) { Reservations++; }
        }
        [BurstCompile(CompileSynchronously=true)] private struct SegmentParityJob : IJob
        {
            public NativeArray<int> Output;
            [BurstDiscard] private static void ManagedOnly(ref bool value) { value=false; }
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Output[0]=burst?1:0;
                int failures=0;
                for(int flags=0;flags<4;flags++)for(int i=0;i<512;i++)
                {
                    var start=new int2(i%17-8,i%29-14);var goal=new int2((i*37)%2501-1250,(i*61)%2501-1250);
                    bool manual=(flags&1)!=0, vehicle=(flags&2)!=0;
                    var original=new InfantryPathOriginalSegmentation();var actual=new UnitPathSegmentation();
                    float max=original.GetMaxSegmentCells(manual,vehicle);
                    if(max!=actual.GetMaxSegmentCells(manual,vehicle)||original.ExceedsMaxSegment(start,goal,manual,vehicle)!=actual.ExceedsMaxSegment(start,goal,manual,vehicle)||!original.GetSegmentGoal(start,goal,max).Equals(actual.GetSegmentGoal(start,goal,max)))failures++;
                }
                Output[1]=failures;
            }
        }
        [Test] public void NativeGoalIdentityAndSegmentPolicyParity()
        {
            string[] names={"UnitPathGoalAssignment","UnitPathfindingScheduler","UnitPathSegmentation"};
            string[] hashes={"854705ab41478868f3e7fc53ed6356a2dadd69b43440b3c0b2f0bd3b5fcf6b1c","932997ff6ec6dea9eb76d462abfa51d84962aa281b961ad54cced9add076c762","fc33da3e64204578dbb702283cb67a9b587151b5e64a6dba51c7efbb8e3cbed4"};
            for(int i=0;i<names.Length;i++)
            {
                string source=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/InfantryPathLifecycle"+names[i]+"Original.cs.txt")).Replace("\r\n","\n");
                using var sha=SHA256.Create();Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source))).Replace("-","").ToLowerInvariant());
                if(i!=1)
                {
                    string replacement=i==0?"InfantryPathOriginalGoalAssignment":"InfantryPathOriginalSegmentation";
                    string expected="#if UNITY_EDITOR && UNITY_INCLUDE_TESTS\n"+source.Replace(names[i],replacement)+"#endif\n";
                    Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+replacement+".cs")).Replace("\r\n","\n"));
                }
            }
            var probe=new GoalProbe { Allowed=false };
            Assert.IsTrue(PathGoalPolicyKernel.TryUseDesiredGoal(ref probe,new PlacementCell(3,4),67,out var direct));
            Assert.AreEqual(new PlacementCell(3,4),direct);Assert.AreEqual(0,probe.Calls);Assert.AreEqual(0,probe.Reservations);
            Assert.IsFalse(PathGoalPolicyKernel.TryUseDesiredGoal(ref probe,new PlacementCell(-1,4),67,out _));Assert.AreEqual(0,probe.Calls);
            Assert.IsFalse(PathGoalPolicyKernel.TryUseDesiredGoal(ref probe,new PlacementCell(4,4),67,out _));Assert.AreEqual(1,probe.Calls);
            probe.Allowed=true;Assert.IsTrue(PathGoalPolicyKernel.TryUseDesiredGoal(ref probe,new PlacementCell(4,4),67,out _));Assert.AreEqual(1,probe.Reservations);
            for(int flags=0;flags<16;flags++)
            {
                bool manual=(flags&1)!=0,vehicle=(flags&2)!=0,skirmish=(flags&4)!=0,hauler=(flags&8)!=0;
                Assert.AreEqual((byte)(hauler?3:(skirmish||hauler)&&vehicle?2:manual&&!vehicle?1:0),PathGoalPolicyKernel.GetRoutingMode(manual,vehicle,skirmish,hauler));
                Assert.AreEqual(hauler||manual&&(!vehicle||skirmish),PathGoalPolicyKernel.SkipAlternateSearch(manual,vehicle,skirmish,hauler));
            }
            for(int seed=0;seed<128;seed++)
            {
                using var f=new Fixture();using var ra=new NativeArray<int>(256,Allocator.TempJob);using var rb=new NativeArray<int>(256,Allocator.TempJob);
                var desired=new int2(seed%21-2,(seed*7)%21-2);var random=new System.Random(seed+471);
                for(int i=0;i<256;i++){if(random.Next(5)==0)f.Blocked.Set(i,true);if(random.Next(7)==0){Set(ra,i,1);Set(rb,i,1);}if(random.Next(5)==0)f.Occupied.Set(i,true);}
                if(seed==0)desired=f.Input.UnitGrid.Cell;
                var actual=new UnitPathGoalAssignment().FindNearestFreeGoal(f.Grid,f.Walkable,f.Blocked,f.Friendly,f.Occupied,f.Entities,f.Grids,f.Sizes,ra,1,f.Actor,Entity.Null,default,default,default,desired,f.Input.UnitGrid.Cell,new int2(1,1),1,67);
                var original=new InfantryPathOriginalGoalAssignment().FindNearestFreeGoal(f.Grid,f.Walkable,f.Blocked,f.Friendly,f.Occupied,f.Entities,f.Grids,f.Sizes,rb,1,f.Actor,Entity.Null,default,default,default,desired,f.Input.UnitGrid.Cell,new int2(1,1),1,67);
                Assert.AreEqual(original,actual,"native goal "+seed);CollectionAssert.AreEqual(rb.ToArray(),ra.ToArray(),"native reservations "+seed);
            }
            using var output=new NativeArray<int>(2,Allocator.TempJob);
            var job=new SegmentParityJob{Output=output};job.Execute();Assert.AreEqual(0,output[0]);Assert.AreEqual(0,output[1]);job.Schedule().Complete();Assert.AreEqual(1,output[0]);Assert.AreEqual(0,output[1]);
        }
        private struct Snapshot
        {
            public LocalTransform Transform;
            public UnitGrid UnitGrid;
            public UnitPathFollow Follow;
            public UnitVehicleKinematics Kinematics;
        }
        [BurstCompile(CompileSynchronously=true)] private struct PairJob : IJob
        {
            public float DeltaTime;
            public GridConfig Grid;
            [ReadOnly] public NativeArray<int2> Pool;
            [ReadOnly] public NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray DynamicBlocked;
            [ReadOnly] public NativeArray<byte> FriendlyPassFactionIds;
            [ReadOnly] public NativeBitArray Occupied;
            [ReadOnly] public NativeArray<GridRoad> Roads;
            [ReadOnly] public NativeArray<GridRoadSidewalk> Sidewalks;
            [ReadOnly] public NativeArray<GridRoadDirt> DirtRoads;
            [ReadOnly] public NativeArray<Entity> LiveUnitEntities;
            [ReadOnly] public NativeArray<UnitGrid> LiveUnitGrids;
            [ReadOnly] public NativeArray<UnitFootprint> LiveUnitFootprints;
            [ReadOnly] public ComponentLookup<AutoWanderMoveTag> AutoWanderLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
            [ReadOnly] public ComponentLookup<UnitLongDistanceMove> LongDistanceMoveLookup;
            [ReadOnly] public ComponentLookup<ManualMoveGroupMemberTag> ManualMoveGroupLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;
            [ReadOnly] public ComponentLookup<CampaignMissionGuidedMoveInProgressTag> CampaignGuidedMoveLookup;
            [ReadOnly] public ComponentLookup<UnitTransportBoardingTarget> BoardingTargetLookup;
            [ReadOnly] public ComponentLookup<UnitGrid> GridLookup;
            public EntityCommandBuffer.ParallelWriter ActualEcb, OriginalEcb;
            public Entity Entity;
            public Snapshot Input;
            public UnitPathRange Range;
            public UnitMove Move;
            public UnitFootprint Footprint;
            public UnitMovementBehavior Behavior;
            public UnitVehicleMovement Vehicle;
            public Faction Faction;
            public byte CleanupOnly;
            public NativeArray<Snapshot> Output;
            public NativeArray<byte> Compiled;
            [BurstDiscard] private static void ManagedOnly(ref bool value) { value=false; }
            public void Execute()
            {
                bool burst=true;ManagedOnly(ref burst);Compiled[0]=burst?(byte)1:(byte)0;
                var actual=new UnitGridMoveJob { DeltaTime=DeltaTime,
                Grid=Grid,
                Pool=Pool,
                Walkable=Walkable,
                DynamicBlocked=DynamicBlocked,
                FriendlyPassFactionIds=FriendlyPassFactionIds,
                Occupied=Occupied,
                Roads=Roads,
                Sidewalks=Sidewalks,
                DirtRoads=DirtRoads,
                LiveUnitEntities=LiveUnitEntities,
                LiveUnitGrids=LiveUnitGrids,
                LiveUnitFootprints=LiveUnitFootprints,
                AutoWanderLookup=AutoWanderLookup,
                UnitTargetLookup=UnitTargetLookup,
                LongDistanceMoveLookup=LongDistanceMoveLookup,
                ManualMoveGroupLookup=ManualMoveGroupLookup,
                ManualMoveLookup=ManualMoveLookup,
                CampaignGuidedMoveLookup=CampaignGuidedMoveLookup,
                BoardingTargetLookup=BoardingTargetLookup, Ecb=ActualEcb };
                var original=new InfantryPathOriginalMoveJob { DeltaTime=DeltaTime,
                Grid=Grid,
                Pool=Pool,
                Walkable=Walkable,
                DynamicBlocked=DynamicBlocked,
                FriendlyPassFactionIds=FriendlyPassFactionIds,
                Occupied=Occupied,
                Roads=Roads,
                Sidewalks=Sidewalks,
                DirtRoads=DirtRoads,
                LiveUnitEntities=LiveUnitEntities,
                LiveUnitGrids=LiveUnitGrids,
                LiveUnitFootprints=LiveUnitFootprints,
                AutoWanderLookup=AutoWanderLookup,
                UnitTargetLookup=UnitTargetLookup,
                LongDistanceMoveLookup=LongDistanceMoveLookup,
                ManualMoveGroupLookup=ManualMoveGroupLookup,
                ManualMoveLookup=ManualMoveLookup,
                CampaignGuidedMoveLookup=CampaignGuidedMoveLookup,
                BoardingTargetLookup=BoardingTargetLookup, Ecb=OriginalEcb };
                var a=Input;var b=Input;
                if(CleanupOnly==0)
                {
                    actual.Execute(0,Entity,ref a.Transform,ref a.UnitGrid,ref a.Follow,ref a.Kinematics,Move,Range,Footprint,Behavior,Vehicle,Faction);
                    original.Execute(0,Entity,ref b.Transform,ref b.UnitGrid,ref b.Follow,ref b.Kinematics,Move,Range,Footprint,Behavior,Vehicle,Faction);
                }
                else
                {
                    new UnitPathFollowCleanupJob { Ecb=ActualEcb,UnitGridLookup=GridLookup,LongDistanceMoveLookup=LongDistanceMoveLookup,ManualMoveLookup=ManualMoveLookup,CampaignGuidedMoveLookup=CampaignGuidedMoveLookup }.Execute(0,Entity,ref a.Kinematics,a.Follow,Range);
                    new InfantryPathOriginalCleanupJob { Ecb=OriginalEcb,UnitGridLookup=GridLookup,LongDistanceMoveLookup=LongDistanceMoveLookup,ManualMoveLookup=ManualMoveLookup,CampaignGuidedMoveLookup=CampaignGuidedMoveLookup }.Execute(0,Entity,ref b.Kinematics,b.Follow,Range);
                }
                Output[0]=a;Output[1]=b;
            }
        }
        private sealed class Fixture : IDisposable
        {
            public readonly World World=new World("Infantry path parity");
            public EntityManager Em=>World.EntityManager;
            public Entity Actor, Other;
            public NativeArray<int2> Pool=new NativeArray<int2>(12,Allocator.TempJob);
            public NativeArray<GridWalkable> Walkable=new NativeArray<GridWalkable>(256,Allocator.TempJob);
            public NativeBitArray Blocked=new NativeBitArray(256,Allocator.TempJob), Occupied=new NativeBitArray(256,Allocator.TempJob);
            public NativeArray<byte> Friendly=new NativeArray<byte>(256,Allocator.TempJob);
            public NativeArray<GridRoad> Roads=new NativeArray<GridRoad>(256,Allocator.TempJob);
            public NativeArray<GridRoadSidewalk> Sidewalks=new NativeArray<GridRoadSidewalk>(256,Allocator.TempJob);
            public NativeArray<GridRoadDirt> Dirt=new NativeArray<GridRoadDirt>(256,Allocator.TempJob);
            public NativeArray<Entity> Entities=new NativeArray<Entity>(2,Allocator.TempJob);
            public NativeArray<UnitGrid> Grids=new NativeArray<UnitGrid>(2,Allocator.TempJob);
            public NativeArray<UnitFootprint> Sizes=new NativeArray<UnitFootprint>(2,Allocator.TempJob);
            public Snapshot Input;
            public UnitPathRange Range=new UnitPathRange{Start=0,Length=8};
            public UnitMove Move=new UnitMove{Speed=4,WalkSpeed=1,RoadSpeedMultiplier=1.75f,ArriveDistance=.05f};
            public float Dt=.1f;
            public GridConfig Grid=new GridConfig{Width=16,Height=16,CellSize=1};
            public Fixture()
            {
                Actor=Em.CreateEntity(typeof(UnitGrid),typeof(UnitPathFollow),typeof(UnitPathRange),typeof(UnitTarget));Other=Em.CreateEntity();
                Em.SetComponentData(Actor,new UnitTarget{Cell=new int2(11,4)});
                Input=new Snapshot{Transform=LocalTransform.FromPositionRotationScale(new float3(3.5f,2,4.5f),quaternion.RotateY(.37f),1.3f),UnitGrid=new UnitGrid{Cell=new int2(3,4)},Follow=new UnitPathFollow{PathIndex=0},Kinematics=new UnitVehicleKinematics{CurrentSpeed=3,StallSeconds=.2f}};
                for(int i=0;i<256;i++)Walkable[i]=new GridWalkable{Value=1};
                for(int i=0;i<12;i++)Pool[i]=new int2(4+i,4);
                Entities[0]=Actor;Entities[1]=Other;Grids[0]=Input.UnitGrid;Grids[1]=new UnitGrid{Cell=new int2(15,15)};
                Sizes[0]=Sizes[1]=new UnitFootprint{Size=new int2(1,1)};
                Em.SetComponentData(Actor,Input.UnitGrid);
            }
            public UnitGridMoveJob Build()
            {
                var j=World.GetOrCreateSystemManaged<InfantryPathLookupHarness>().Build();j.DeltaTime=Dt;j.Grid=Grid;j.Pool=Pool;j.Walkable=Walkable;j.DynamicBlocked=Blocked;j.FriendlyPassFactionIds=Friendly;j.Occupied=Occupied;j.Roads=Roads;j.Sidewalks=Sidewalks;j.DirtRoads=Dirt;j.LiveUnitEntities=Entities;j.LiveUnitGrids=Grids;j.LiveUnitFootprints=Sizes;return j;
            }
            public void Dispose(){World.Dispose();Pool.Dispose();Walkable.Dispose();Blocked.Dispose();Occupied.Dispose();Friendly.Dispose();Roads.Dispose();Sidewalks.Dispose();Dirt.Dispose();Entities.Dispose();Grids.Dispose();Sizes.Dispose();}
        }
        private struct Effects
        {
            public bool Follow,Range,Wander,Guided,Long,Manual,Target,Request;
            public UnitLongDistanceMove LongValue;public UnitTarget TargetValue;public UnitPathRequest RequestValue;
        }
        private static Effects Read(Fixture f)
        {
            var em=f.Em;var e=f.Actor;var v=new Effects{Follow=em.HasComponent<UnitPathFollow>(e),Range=em.HasComponent<UnitPathRange>(e),Wander=em.HasComponent<AutoWanderMoveTag>(e),Guided=em.HasComponent<CampaignMissionGuidedMoveInProgressTag>(e),Long=em.HasComponent<UnitLongDistanceMove>(e),Manual=em.HasComponent<ManualMoveOrderTag>(e),Target=em.HasComponent<UnitTarget>(e),Request=em.HasComponent<UnitPathRequest>(e)};
            if(v.Long)v.LongValue=em.GetComponentData<UnitLongDistanceMove>(e);if(v.Target)v.TargetValue=em.GetComponentData<UnitTarget>(e);if(v.Request)v.RequestValue=em.GetComponentData<UnitPathRequest>(e);return v;
        }
        private static void Restore(Fixture f,Effects s)
        {
            void Tag<T>(bool value) where T:unmanaged,IComponentData { if(value)f.Em.AddComponent<T>(f.Actor);else f.Em.RemoveComponent<T>(f.Actor); }
            Tag<UnitPathFollow>(s.Follow);Tag<UnitPathRange>(s.Range);Tag<AutoWanderMoveTag>(s.Wander);Tag<CampaignMissionGuidedMoveInProgressTag>(s.Guided);Tag<UnitLongDistanceMove>(s.Long);Tag<ManualMoveOrderTag>(s.Manual);Tag<UnitTarget>(s.Target);Tag<UnitPathRequest>(s.Request);
            if(s.Long)f.Em.SetComponentData(f.Actor,s.LongValue);if(s.Target)f.Em.SetComponentData(f.Actor,s.TargetValue);if(s.Request)f.Em.SetComponentData(f.Actor,s.RequestValue);
        }
        private static string[] Commands(EntityCommandBuffer ecb)
        {
            var type=typeof(EntityCommandBuffer).GetNestedType("EntityCommandBufferDebugView",BindingFlags.NonPublic);Assert.NotNull(type);
            var view=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{ecb},null);var commands=(Array)type.GetProperty("Commands").GetValue(view);var result=new string[commands.Length];
            for(int i=0;i<commands.Length;i++){object c=commands.GetValue(i);var t=c.GetType();result[i]=t.GetField("CommandType").GetValue(c)+"|"+t.GetField("SortKey").GetValue(c)+"|"+t.GetField("Entity").GetValue(c)+"|"+t.GetField("ComponentTypeIndex").GetValue(c);}return result;
        }
        private static void Compare(Fixture f,bool burst,bool cleanup,string label)
        {
            using var a=new EntityCommandBuffer(Allocator.TempJob);using var b=new EntityCommandBuffer(Allocator.TempJob);
            using var output=new NativeArray<Snapshot>(2,Allocator.TempJob);using var compiled=new NativeArray<byte>(1,Allocator.TempJob);
            var actual=f.Build();var job=new PairJob { DeltaTime=actual.DeltaTime,
                Grid=actual.Grid,
                Pool=actual.Pool,
                Walkable=actual.Walkable,
                DynamicBlocked=actual.DynamicBlocked,
                FriendlyPassFactionIds=actual.FriendlyPassFactionIds,
                Occupied=actual.Occupied,
                Roads=actual.Roads,
                Sidewalks=actual.Sidewalks,
                DirtRoads=actual.DirtRoads,
                LiveUnitEntities=actual.LiveUnitEntities,
                LiveUnitGrids=actual.LiveUnitGrids,
                LiveUnitFootprints=actual.LiveUnitFootprints,
                AutoWanderLookup=actual.AutoWanderLookup,
                UnitTargetLookup=actual.UnitTargetLookup,
                LongDistanceMoveLookup=actual.LongDistanceMoveLookup,
                ManualMoveGroupLookup=actual.ManualMoveGroupLookup,
                ManualMoveLookup=actual.ManualMoveLookup,
                CampaignGuidedMoveLookup=actual.CampaignGuidedMoveLookup,
                BoardingTargetLookup=actual.BoardingTargetLookup,
                ActualEcb=a.AsParallelWriter(),OriginalEcb=b.AsParallelWriter(),GridLookup=f.World.GetOrCreateSystemManaged<InfantryPathLookupHarness>().Grids(),
                Entity=f.Actor,Input=f.Input,Range=f.Range,Move=f.Move,Footprint=new UnitFootprint{Size=new int2(1,1)},Faction=new Faction{Id=1},Output=output,Compiled=compiled,CleanupOnly=cleanup?(byte)1:(byte)0};
            var before=Read(f);if(burst)job.Schedule().Complete();else job.Execute();Assert.AreEqual(burst?1:0,compiled[0],"Burst sentinel "+label);
            Assert.AreEqual(before,Read(f),"effects must remain deferred "+label);CollectionAssert.AreEqual(Commands(b),Commands(a),"ECB order "+label);
            var x=output[0];var y=output[1];Assert.AreEqual(math.asint(y.Transform.Position),math.asint(x.Transform.Position),"position "+label);Assert.AreEqual(math.asint(y.Transform.Rotation.value),math.asint(x.Transform.Rotation.value),"facing "+label);Assert.AreEqual(y.Transform.Scale,x.Transform.Scale,label);Assert.AreEqual(y.UnitGrid.Cell,x.UnitGrid.Cell,"cell "+label);Assert.AreEqual(y.Follow.PathIndex,x.Follow.PathIndex,"path "+label);Assert.AreEqual(math.asint(y.Kinematics.CurrentSpeed),math.asint(x.Kinematics.CurrentSpeed),"speed "+label);Assert.AreEqual(math.asint(y.Kinematics.StallSeconds),math.asint(x.Kinematics.StallSeconds),"stall "+label);
            b.Playback(f.Em);var expected=Read(f);Restore(f,before);a.Playback(f.Em);Assert.AreEqual(expected,Read(f),"ECB payload/effects "+label);
        }
        [Test] public void NativeManagedAndBurstMoveParity()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled);
            foreach(bool burst in new[]{false,true})for(int scenario=0;scenario<112;scenario++)
            {
                using var f=new Fixture();
                if(scenario<16)switch(scenario)
                {
                    case 0:f.Range.Length=0;break;
                    case 1:f.Range.Start=-1;break;
                    case 2:f.Walkable[68]=default;break;
                    case 3:f.Blocked.Set(68,true);break;
                    case 4:f.Blocked.Set(68,true);f.Friendly[68]=1;break;
                    case 5:f.Occupied.Set(68,true);f.Grids[0]=new UnitGrid{Cell=new int2(4,4)};break;
                    case 6:f.Occupied.Set(68,true);f.Grids[1]=new UnitGrid{Cell=new int2(4,4)};break;
                    case 7:f.Occupied.Set(68,true);f.Grids[1]=new UnitGrid{Cell=new int2(4,4)};f.Dt=.2f;break;
                    case 8:f.Em.AddComponent<CampaignMissionGuidedMoveInProgressTag>(f.Actor);f.Blocked.Set(68,true);f.Occupied.Set(68,true);break;
                    case 9:f.Em.AddComponent<ManualMoveGroupMemberTag>(f.Actor);f.Em.AddComponent<ManualMoveGroupMemberTag>(f.Other);f.Grids[1]=new UnitGrid{Cell=new int2(4,4)};f.Occupied.Set(68,true);break;
                    case 10:f.Em.AddComponentData(f.Actor,new UnitTransportBoardingTarget{Transport=f.Other});f.Grids[1]=new UnitGrid{Cell=new int2(4,4)};f.Occupied.Set(68,true);break;
                    case 11:f.Sidewalks[67]=new GridRoadSidewalk{Value=1};f.Em.AddComponent<AutoWanderMoveTag>(f.Actor);break;
                    case 12:f.Move.Speed=100;f.Dt=1;f.Walkable[69]=default;break;
                    case 13:f.Input.UnitGrid.Cell=new int2(0,0);break;
                    case 14:f.Range.Length=1;f.Em.AddComponent<ManualMoveGroupMemberTag>(f.Actor);break;
                    case 15:f.Range.Start=-1;f.Em.RemoveComponent<UnitTarget>(f.Actor);break;
                }
                else
                {
                    var random=new System.Random(scenario*127+31);f.Dt=(float)random.NextDouble()*1.5f;f.Move.Speed=(float)random.NextDouble()*15;f.Move.ArriveDistance=(float)random.NextDouble()*.5f;
                    f.Input.Transform.Position=new float3(2.5f+(float)random.NextDouble()*8,3,3.5f+(float)random.NextDouble()*2);f.Input.Follow.PathIndex=random.Next(0,8);f.Range.Length=random.Next(1,12);f.Range.Start=random.Next(0,3);f.Input.Kinematics.StallSeconds=(float)random.NextDouble()*.4f;
                    if(scenario%3==0)f.Em.AddComponent<ManualMoveGroupMemberTag>(f.Actor);if(scenario%5==0)f.Em.AddComponent<AutoWanderMoveTag>(f.Actor);if(scenario%7==0)f.Em.AddComponentData(f.Actor,new UnitLongDistanceMove{FinalGoal=new int2(14,12),ManualMove=1});
                    for(int i=0;i<256;i++){f.Sidewalks[i]=new GridRoadSidewalk{Value=(byte)random.Next(0,2)};if(random.Next(50)==0)f.Blocked.Set(i,true);}
                }
                Compare(f,burst,false,"scenario="+scenario+" burst="+burst);
            }
        }
        [Test] public void NativeCompletionAndDeferredParity()
        {
            foreach(bool burst in new[]{false,true})for(int flags=0;flags<64;flags++)
            {
                using var f=new Fixture();f.Input.Follow.PathIndex=(flags&1)!=0?8:0;
                if((flags&2)!=0)f.Em.AddComponent<CampaignMissionGuidedMoveInProgressTag>(f.Actor);
                if((flags&4)!=0)f.Em.AddComponentData(f.Actor,new UnitLongDistanceMove{FinalGoal=(flags&8)!=0?f.Input.UnitGrid.Cell:new int2(12,12),ManualMove=(byte)((flags&16)!=0?1:0)});
                if((flags&32)!=0)f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);f.Em.AddComponent<AutoWanderMoveTag>(f.Actor);
                Compare(f,burst,true,"cleanup flags="+flags);
            }
        }
        private static void Set<T>(NativeArray<T> array, int index, T value) where T : struct { array[index]=value; }
        [Test] public void NativePathResultApplyParity()
        {
            for(int flags=0;flags<64;flags++)
            {
                using var world=new World("Path result apply parity");var em=world.EntityManager;
                var harness=world.GetOrCreateSystemManaged<InfantryPathLookupHarness>();
                Entity Make()
                {
                    var e=em.CreateEntity(typeof(UnitPathRequest),typeof(UnitPathFollow),typeof(UnitPathRange),typeof(UnitTarget),typeof(AutoWanderMoveTag));
                    em.SetComponentData(e,new UnitPathRequest{Goal=new int2((flags&1)!=0?9:8,9)});
                    em.SetComponentData(e,new UnitTarget{Cell=new int2(4,4)});
                    if((flags&8)!=0)em.AddComponent<ManualMoveOrderTag>(e);
                    if((flags&16)!=0)em.AddComponentData(e,new UnitLongDistanceMove{FinalGoal=new int2(12,12),ManualMove=1});
                    em.AddComponentData(e,new UnitPathRetryCooldown{ResumeFrame=27});return e;
                }
                var a=Make();var b=Make();
                using var entities=new NativeArray<Entity>(1,Allocator.TempJob);using var requests=new NativeArray<UnitPathRequest>(1,Allocator.TempJob);Set(requests,0,new UnitPathRequest{Goal=new int2(8,9)});
                using var goals=new NativeArray<int2>(1,Allocator.TempJob);Set(goals,0,new int2(6,7));
                using var segmented=new NativeArray<byte>(1,Allocator.TempJob);Set(segmented,0,(byte)((flags&4)!=0?1:0));
                using var manual=new NativeArray<byte>(1,Allocator.TempJob);Set(manual,0,(byte)((flags&8)!=0?1:0));
                using var status=new NativeArray<byte>(1,Allocator.TempJob);Set(status,0,(byte)((flags&2)!=0?1:0));
                using var failure=new NativeArray<int>(1,Allocator.TempJob);using var expansion=new NativeArray<int>(1,Allocator.TempJob);
                using var stream=new NativeStream(1,Allocator.TempJob);var writer=stream.AsWriter();writer.BeginForEachIndex(0);if((flags&32)!=0){writer.Write(new int2(5,6));writer.Write(new int2(6,7));}writer.EndForEachIndex();
                var poolA=new PathPoolComponent{Cells=new NativeList<int2>(Allocator.TempJob)};var poolB=new PathPoolComponent{Cells=new NativeList<int2>(Allocator.TempJob)};
                try
                {
                    Set(entities,0,a);new UnitPathResultApply().Apply(ref harness.CheckedStateRef,Entity.Null,ref poolA,entities,requests,goals,segmented,manual,default,stream,status,failure,expansion,
                        out int ca,out int csa,out int cma,out int ra,out int rsa,out int rma,out int aa);
                    Set(entities,0,b);new InfantryPathOriginalResultApply().Apply(ref harness.CheckedStateRef,Entity.Null,ref poolB,entities,requests,goals,segmented,manual,default,stream,status,failure,expansion,
                        out int cb,out int csb,out int cmb,out int rb,out int rsb,out int rmb,out int ab);
                    CollectionAssert.AreEqual(new[]{cb,csb,cmb,rb,rsb,rmb,ab},new[]{ca,csa,cma,ra,rsa,rma,aa},"result counters "+flags);
                    CollectionAssert.AreEqual(poolB.Cells.AsArray().ToArray(),poolA.Cells.AsArray().ToArray(),"pool append "+flags);
                    string State(Entity e)
                    {
                        string C<T>() where T:unmanaged,IComponentData => em.HasComponent<T>(e)?JsonUtility.ToJson(em.GetComponentData<T>(e)):"absent";
                        // Zero-sized tags have presence only; EntityManager rejects data reads for them.
                        string output=C<UnitPathRequest>()+C<UnitPathFollow>()+C<UnitPathRange>()+C<UnitTarget>()+C<UnitLongDistanceMove>()+C<UnitPathRetryCooldown>()
                            +"|manual="+em.HasComponent<ManualMoveOrderTag>(e)+"|wander="+em.HasComponent<AutoWanderMoveTag>(e);
                        if(em.HasBuffer<UnitPathSurfaceNode>(e)){var buffer=em.GetBuffer<UnitPathSurfaceNode>(e);output+="surface:"+buffer.Length;for(int i=0;i<buffer.Length;i++)output+=JsonUtility.ToJson(buffer[i]);}return output;
                    }
                    Assert.AreEqual(State(b),State(a),"immediate result application "+flags);
                }
                finally { poolA.Cells.Dispose();poolB.Cells.Dispose(); }
            }
        }
        [Test] public void ResultAndRetryIntents()
        {
            foreach(bool matching in new[]{false,true})foreach(byte status in new byte[]{0,1,2})foreach(int count in new[]{0,1,8})foreach(byte segmented in new byte[]{0,1})foreach(bool manual in new[]{false,true})foreach(bool longMove in new[]{false,true})
            {
                var goal=new PlacementCell(8,9);var assigned=new PlacementCell(6,7);
                var result=InfantryPathLifecycleKernel.ApplyResult(matching,status,count,17,goal,assigned,segmented,1,manual,longMove,100);
                var expected=!matching?PathResultDisposition.Ignore:status==1&&count>0?PathResultDisposition.Success:manual||longMove||segmented!=0?PathResultDisposition.Retry:PathResultDisposition.Abandon;
                Assert.AreEqual(expected,result.Disposition);Assert.AreEqual(matching,result.AppendPool);
                if(expected==PathResultDisposition.Success){Assert.AreEqual(17,result.PathStart);Assert.AreEqual(count,result.PathLength);Assert.AreEqual(0,result.PathIndex);Assert.AreEqual(assigned,result.Target);Assert.AreEqual(segmented!=0,result.SetLongDistance);}
                if(expected==PathResultDisposition.Retry){Assert.AreEqual(108,result.ResumeFrame);Assert.AreEqual(goal,result.FinalGoal);}
            }
            Assert.AreEqual(unchecked(int.MaxValue+8),InfantryPathLifecycleKernel.RetryResumeFrame(int.MaxValue));
            Assert.IsFalse(InfantryPathLifecycleKernel.MatchingRequest(true,true,new PlacementCell(1,2),new PlacementCell(2,2)));
            Assert.IsTrue(InfantryPathLifecycleKernel.MatchingRequest(true,true,new PlacementCell(1,2),new PlacementCell(1,2)));
        }
    }
}
#endif
