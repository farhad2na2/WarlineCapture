#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Movement;
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
    [DisableAutoCreation] public partial class EngagedMovementLookupHarness:SystemBase
    {
        protected override void OnUpdate(){}
        internal UnitEngagedMovementSystem.EngagedMoveJob Build(FixtureInputs f)=>new UnitEngagedMovementSystem.EngagedMoveJob{
                Grid=f.Grid,DeltaTime=f.DeltaTime,Walkable=f.Walkable,DynamicBlocked=f.Blocked,FriendlyPassFactionIds=f.Friendly,Roads=f.Roads,Sidewalks=f.Sidewalks,DirtRoads=f.Dirt,
                FootprintLookup=GetComponentLookup<UnitFootprint>(true),
                HealthLookup=GetComponentLookup<UnitHealth>(true),
                StaticBlockerLookup=GetComponentLookup<StaticGridBlocker>(true),
                HoldPositionLookup=GetComponentLookup<HoldPositionOrderTag>(true),
                DebugFireTargetLookup=GetComponentLookup<DebugFireTargetTag>(true),
                ScanOrderLookup=GetComponentLookup<UnitScanOrder>(true),
            };
    }
    public struct FixtureInputs
    {
        public GridConfig Grid;public float DeltaTime;
        public NativeArray<GridWalkable> Walkable;public NativeBitArray Blocked;public NativeArray<byte> Friendly;
        public NativeArray<GridRoad> Roads;public NativeArray<GridRoadSidewalk> Sidewalks;public NativeArray<GridRoadDirt> Dirt;
    }
    public sealed class EngagedMovementKernelTests
    {
        public static int RunChecks()
        {
            var t=new EngagedMovementKernelTests();t.OriginalSnapshotAndOraclePinned();t.NativeNamedGatesChaseCollisionAndVehicleDelegation();t.SeededManagedAndActualBurstMatchOriginal();t.SharedSegmentSamplingFactsPreserveOrder();t.ExistingPausedNativeMovementRegression();
            Debug.Log("[EngagedMovementParity] result=Passed checks=5 nativeNamed=128 nativeSeeded=512 managed=Passed burst=Passed delegatedVehicle=NativeOnly missionReady=0");return 5;
        }
        public static void RunCombinedValidation()
        {
            try
            {
                Assert.AreEqual(5,GridPathRoutingKernelTests.RunChecks());Assert.AreEqual(13,SkirmishMissionOrchestrationKernelTests.RunChecks());
                GridPathSearchKernelTests.RunChecks();GridOccupancyKernelTests.RunChecks();Assert.AreEqual(3,PathPlacementKernelTests.RunChecks());GridPathMovementKernelTests.RunChecks();DirectDamageSequencerTests.RunChecks();
                var attack=new StandardAttackPlannerTests();attack.OriginalOracleIsPinnedVerbatimProduction();attack.InstalledMathNaNEqualAndClampSemanticsMatchKernel();attack.ManagedRandomAndBoundaryPlansMatchEveryField();attack.BurstScheduledPlansMatchOriginalAndManagedResults();attack.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                var combat=new global::UnitCombatFocusedEditModeTests();combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                Assert.AreEqual(4,WorldSchedulingKernelTests.RunChecks());Assert.AreEqual(7,SkirmishSquadTargetingKernelTests.RunChecks());Assert.AreEqual(5,TargetAcquisitionKernelTests.RunChecks());

                int failures=0;
                void Check(string name,int expected,Func<int> run)
                {
                    try{Assert.AreEqual(expected,run(),name+" check count");}
                    catch(Exception e){failures++;Debug.LogError("[SharedMovingLifecycleSuite] result=Failed suite="+name);Debug.LogException(e);}
                }
                Check("coordinates",3,GridCoordinateKernelTests.RunChecks);Check("pathScheduling",5,PathJobSchedulingKernelTests.RunChecks);
                Check("death",8,UnitDeathLifecycleKernelTests.RunChecks);Check("targetLifecycle",4,EngageTargetLifecycleKernelTests.RunChecks);
                Check("engaged",5,RunChecks);Check("composedTrace",1,EngagedMovementNativeTraceTests.RunChecks);Check("infantryPath",6,InfantryPathLifecycleKernelTests.RunChecks);
                try{CaptureMovingOrder();}catch(Exception e){failures++;Debug.LogException(e);}
                if(failures!=0)throw new InvalidOperationException("Combined moving lifecycle failed suites="+failures+"; no pass marker authorized.");
                Debug.Log("[SharedMovingLifecycleParity] result=Passed prior=75 coordinates=3 pathScheduling=5 death=8 targetLifecycle=4 infantryPath=6 engaged=5 composedTrace=1 total=107 missionReady=0");ValidationExit.Passed();
            }
            catch(Exception e){Debug.LogException(e);Debug.LogError("[SharedMovingLifecycleParity] result=Failed missionReady=0");ValidationExit.Failed();}
        }
        [Serializable] private sealed class OrderReceipt {public string profile="IsolatedMovingCombatScheduleV1",scope="NativeSortedSubsetOnly";public bool full_world=false;public string[] systems;}
        private static void CaptureMovingOrder()
        {
            using var world=new World("IsolatedMovingCombatSchedule");var group=world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            Type[] types={typeof(BeginSimulationEntityCommandBufferSystem),typeof(SkirmishObjectiveFactProjectionSystem),typeof(SkirmishBaseAssaultObjectiveSystem),typeof(DynamicOccupancyRebuildSystem),typeof(EngageTargetValidateSystem),typeof(UnitEngagementSystem),typeof(EngageTargetSyncSystem),typeof(UnitEngagedMovementSystem),typeof(UnitGridMovementSystem),typeof(UnitPathfindingSystem),typeof(UnitAttackSystem),typeof(UnitDeathSystem),typeof(SkirmishOutcomeSystem),typeof(EndSimulationEntityCommandBufferSystem)};
            foreach(var type in types)group.AddSystemToUpdateList(world.GetOrCreateSystem(type));group.SortSystems();using var handles=group.GetAllSystems();var names=new string[handles.Length];
            for(int i=0;i<handles.Length;i++)names[i]=TypeManager.GetSystemName(world.Unmanaged.GetSystemTypeIndex(handles[i])).ToString();
            void Before(Type a,Type b)=>Assert.Less(Array.IndexOf(names,a.FullName),Array.IndexOf(names,b.FullName),a.Name+" before "+b.Name);
            Before(typeof(EngageTargetValidateSystem),typeof(UnitEngagementSystem));Before(typeof(UnitEngagementSystem),typeof(EngageTargetSyncSystem));Before(typeof(EngageTargetSyncSystem),typeof(UnitEngagedMovementSystem));Before(typeof(UnitEngagedMovementSystem),typeof(UnitGridMovementSystem));Before(typeof(UnitAttackSystem),typeof(UnitDeathSystem));
            Debug.Log("[MovingWorldSchedulingNativeOrder] "+JsonUtility.ToJson(new OrderReceipt{systems=names}));
        }
        [Test] public void OriginalSnapshotAndOraclePinned()
        {
            string original=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/EngagedMovementOriginalSource.cs.txt")).Replace("\r\n","\n");using var sha=SHA256.Create();
            Assert.AreEqual("78cef37055c2f435a7bbbbd249127423302fecdee0e1183c37bfd3049fe2c218",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            string expected="#if UNITY_EDITOR\n"+original.Replace("UnitEngagedMovementSystem","EngagedMovementOriginalSystem").Replace("    [BurstCompile]\n    [UpdateAfter","    [DisableAutoCreation]\n    [BurstCompile]\n    [UpdateAfter").Replace("private partial struct EngagedMoveJob","internal partial struct EngagedMoveJob")+"\n#endif\n";
            Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Game/Scripts/Systems/EngagedMovementOriginalSystem.cs")).Replace("\r\n","\n"));
        }
        private struct Outcome {public LocalTransform Transform;public UnitGrid Grid;public EngageTarget Engage;public UnitVehicleKinematics Kinematics;}
        [BurstCompile(CompileSynchronously=true)] private struct PairJob:IJob
        {
            [ReadOnly] public GridConfig Grid;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public Unity.Collections.NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray DynamicBlocked;
            [ReadOnly] public Unity.Collections.NativeArray<byte> FriendlyPassFactionIds;
            [ReadOnly] public Unity.Collections.NativeArray<GridRoad> Roads;
            [ReadOnly] public Unity.Collections.NativeArray<GridRoadSidewalk> Sidewalks;
            [ReadOnly] public Unity.Collections.NativeArray<GridRoadDirt> DirtRoads;
            [ReadOnly] public ComponentLookup<UnitFootprint> FootprintLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticBlockerLookup;
            [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
            [ReadOnly] public ComponentLookup<DebugFireTargetTag> DebugFireTargetLookup;
            [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;
            public Entity Source;public Outcome Initial;
            public UnitCombat Combat;public UnitAttack Attack;public UnitMove Move;public UnitFootprint Footprint;public UnitMovementBehavior Behavior;public UnitVehicleMovement Vehicle;public Faction Faction;
            public NativeArray<Outcome> Outputs;public NativeArray<int> Compiled;
            [BurstDiscard] private static void Managed(ref bool value){value=false;}
            public void Execute()
            {
                bool compiled=true;Managed(ref compiled);Compiled[0]=compiled?1:0;
                var original=new EngagedMovementOriginalSystem.EngagedMoveJob{
                    Grid=Grid,
                    DeltaTime=DeltaTime,
                    Walkable=Walkable,
                    DynamicBlocked=DynamicBlocked,
                    FriendlyPassFactionIds=FriendlyPassFactionIds,
                    Roads=Roads,
                    Sidewalks=Sidewalks,
                    DirtRoads=DirtRoads,
                    FootprintLookup=FootprintLookup,
                    HealthLookup=HealthLookup,
                    StaticBlockerLookup=StaticBlockerLookup,
                    HoldPositionLookup=HoldPositionLookup,
                    DebugFireTargetLookup=DebugFireTargetLookup,
                    ScanOrderLookup=ScanOrderLookup,
                };
                var actual=new UnitEngagedMovementSystem.EngagedMoveJob{
                    Grid=Grid,
                    DeltaTime=DeltaTime,
                    Walkable=Walkable,
                    DynamicBlocked=DynamicBlocked,
                    FriendlyPassFactionIds=FriendlyPassFactionIds,
                    Roads=Roads,
                    Sidewalks=Sidewalks,
                    DirtRoads=DirtRoads,
                    FootprintLookup=FootprintLookup,
                    HealthLookup=HealthLookup,
                    StaticBlockerLookup=StaticBlockerLookup,
                    HoldPositionLookup=HoldPositionLookup,
                    DebugFireTargetLookup=DebugFireTargetLookup,
                    ScanOrderLookup=ScanOrderLookup,
                };
                var a=Initial;var b=Initial;
                original.Execute(Source,ref a.Transform,ref a.Grid,ref a.Engage,ref a.Kinematics,Combat,Attack,Move,Footprint,Behavior,Vehicle,Faction);
                actual.Execute(Source,ref b.Transform,ref b.Grid,ref b.Engage,ref b.Kinematics,Combat,Attack,Move,Footprint,Behavior,Vehicle,Faction);
                Outputs[0]=a;Outputs[1]=b;
            }
        }
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("EngagedMovementOriginalParity");public EntityManager Em=>World.EntityManager;
            public FixtureInputs Inputs;public Entity Source,Target;public LocalTransform Transform=LocalTransform.FromPositionRotation(new float3(10.5f,2,10.5f),quaternion.RotateY(math.PI*.5f));
            public UnitGrid SelfGrid=new UnitGrid{Cell=new int2(10,10)};public EngageTarget Engage;public UnitVehicleKinematics Kinematics=new UnitVehicleKinematics{CurrentSpeed=7,StallSeconds=.37f};
            public UnitCombat Combat=new UnitCombat{AggroRangeCells=20,ChaseBreakDistance=30};public UnitAttack Attack=new UnitAttack{Range=2};
            public UnitMove Move=new UnitMove{Speed=3,RoadSpeedMultiplier=1.5f};public UnitFootprint Footprint=new UnitFootprint{Size=new int2(1,1)};
            public UnitMovementBehavior Behavior;public UnitVehicleMovement Vehicle=new UnitVehicleMovement{TurnSpeedDegrees=180,Acceleration=20,Braking=20};
            public Fixture(bool vehicle=false)
            {
                Inputs=new FixtureInputs{Grid=new GridConfig{Width=32,Height=32,CellSize=1},DeltaTime=.25f,Walkable=new NativeArray<GridWalkable>(1024,Allocator.TempJob),Blocked=new NativeBitArray(1024,Allocator.TempJob),Friendly=new NativeArray<byte>(1024,Allocator.TempJob),Roads=new NativeArray<GridRoad>(1024,Allocator.TempJob),Sidewalks=new NativeArray<GridRoadSidewalk>(1024,Allocator.TempJob),Dirt=new NativeArray<GridRoadDirt>(1024,Allocator.TempJob)};
                for(int i=0;i<1024;i++){Inputs.Walkable[i]=new GridWalkable{Value=1};Inputs.Friendly[i]=byte.MaxValue;}
                Source=Em.CreateEntity();Target=Em.CreateEntity(typeof(UnitHealth),typeof(UnitFootprint));Em.SetComponentData(Target,new UnitHealth{Current=100,Max=100});Em.SetComponentData(Target,new UnitFootprint{Size=new int2(1,1)});
                Engage=new EngageTarget{Target=Target,Cell=new int2(17,10),Position=new float3(17.5f,8,10.5f),IsCommanded=0};Behavior.UsesVehicleMotion=(byte)(vehicle?1:0);
            }
            public void Scan(int radius=1,byte started=1)=>Em.AddComponentData(Source,new UnitScanOrder{CenterCell=new int2(10,10),RadiusCells=radius,HasStarted=started,EngageDetectedTargets=0});
            public void Dispose(){World.Dispose();Inputs.Walkable.Dispose();Inputs.Blocked.Dispose();Inputs.Friendly.Dispose();Inputs.Roads.Dispose();Inputs.Sidewalks.Dispose();Inputs.Dirt.Dispose();}
        }
        private static void EqualFloat(float a,float b,string label)
        {if(float.IsNaN(a))Assert.IsTrue(float.IsNaN(b),label);else Assert.AreEqual(math.asint(a),math.asint(b),label);}
        private static void Same(Outcome a,Outcome b,string label)
        {
            for(int i=0;i<3;i++){EqualFloat(a.Transform.Position[i],b.Transform.Position[i],label+" position"+i);EqualFloat(a.Engage.Position[i],b.Engage.Position[i],label+" cached target"+i);}
            for(int i=0;i<4;i++)EqualFloat(a.Transform.Rotation.value[i],b.Transform.Rotation.value[i],label+" rotation"+i);
            EqualFloat(a.Transform.Scale,b.Transform.Scale,label+" scale");EqualFloat(a.Kinematics.CurrentSpeed,b.Kinematics.CurrentSpeed,label+" speed");EqualFloat(a.Kinematics.StallSeconds,b.Kinematics.StallSeconds,label+" stall");
            Assert.AreEqual(a.Grid.Cell,b.Grid.Cell,label+" grid");Assert.AreEqual(a.Engage.Target,b.Engage.Target,label+" identity/version");Assert.AreEqual(a.Engage.Cell,b.Engage.Cell,label+" target cell");Assert.AreEqual(a.Engage.IsCommanded,b.Engage.IsCommanded,label+" commanded");
        }
        private static Outcome Compare(Fixture f,bool burst,string label)
        {
            using var outputs=new NativeArray<Outcome>(2,Allocator.TempJob);using var compiled=new NativeArray<int>(1,Allocator.TempJob);var harness=f.World.GetOrCreateSystemManaged<EngagedMovementLookupHarness>();var job=harness.Build(f.Inputs);
            var pair=new PairJob{Grid=job.Grid,DeltaTime=job.DeltaTime,Walkable=job.Walkable,DynamicBlocked=job.DynamicBlocked,FriendlyPassFactionIds=job.FriendlyPassFactionIds,Roads=job.Roads,Sidewalks=job.Sidewalks,DirtRoads=job.DirtRoads,FootprintLookup=job.FootprintLookup,HealthLookup=job.HealthLookup,StaticBlockerLookup=job.StaticBlockerLookup,HoldPositionLookup=job.HoldPositionLookup,DebugFireTargetLookup=job.DebugFireTargetLookup,ScanOrderLookup=job.ScanOrderLookup,Source=f.Source,Initial=new Outcome{Transform=f.Transform,Grid=f.SelfGrid,Engage=f.Engage,Kinematics=f.Kinematics},Combat=f.Combat,Attack=f.Attack,Move=f.Move,Footprint=f.Footprint,Behavior=f.Behavior,Vehicle=f.Vehicle,Faction=new Faction{Id=1},Outputs=outputs,Compiled=compiled};
            if(burst)pair.Schedule().Complete();else pair.Execute();Assert.AreEqual(burst?1:0,compiled[0],label+" actual Burst sentinel");Same(outputs[0],outputs[1],label);return outputs[1];
        }

        [Test] public void NativeNamedGatesChaseCollisionAndVehicleDelegation()
        {
            foreach(bool burst in new[]{false,true})foreach(bool vehicle in new[]{false,true})for(int scenario=0;scenario<32;scenario++)
            {
                using var f=new Fixture(vehicle);var original=f.Transform.Position;
                switch(scenario)
                {
                    case 0:f.Attack.Range=0;break;
                    case 1:f.Engage.Target=Entity.Null;break;
                    case 2:f.Em.AddComponentData(f.Target,new DebugFireTargetTag{Source=f.Source});break;
                    case 3:f.Em.AddComponent<StaticGridBlocker>(f.Target);break;
                    case 4:f.Em.SetComponentData(f.Target,new UnitHealth{Current=0});break;
                    case 5:f.Em.RemoveComponent<UnitHealth>(f.Target);break;
                    case 6:f.Em.AddComponent<HoldPositionOrderTag>(f.Source);break;
                    case 7:f.Scan();break;
                    case 8:f.Combat.ChaseBreakDistance=1;f.Combat.AggroRangeCells=1;break;
                    case 9:f.Combat.ChaseBreakDistance=1;f.Combat.AggroRangeCells=1;f.Engage.IsCommanded=1;break;
                    case 10:f.Attack.Range=8;break;
                    case 11:f.Inputs.Blocked.Set(11+10*32,true);f.Inputs.DeltaTime=1;break;
                    case 12:f.Inputs.Blocked.Set(11+10*32,true);f.Inputs.Friendly[11+10*32]=1;f.Inputs.DeltaTime=1;break;
                    case 13:f.Inputs.Walkable[11+10*32]=new GridWalkable{Value=0};f.Inputs.DeltaTime=1;break;
                    case 14:f.Inputs.Sidewalks[330]=new GridRoadSidewalk{Value=1};f.Inputs.Dirt[330]=new GridRoadDirt{Value=1};break;
                    case 15:f.Inputs.Grid.Origin=new float3(2,99,-3);break;
                    case 16:f.SelfGrid.Cell=new int2(0,0);break;
                    case 17:f.Transform.Position=new float3(-1,2,10.5f);break;
                    case 18:f.Em.SetComponentData(f.Target,new UnitFootprint{Size=new int2(12,12)});break;
                    case 19:f.Footprint.Size=new int2(0,-1);break;
                    case 20:f.Move.Speed=-2;break;
                    case 21:f.Inputs.DeltaTime=0;break;
                    case 22:f.Inputs.DeltaTime=-.25f;break;
                    case 23:f.Engage.Position=f.Transform.Position+new float3(.00001f,80,0);break;
                    case 24:f.Engage.Target=new Entity{Index=f.Target.Index,Version=f.Target.Version+1};break;
                    case 25:f.Em.RemoveComponent<UnitFootprint>(f.Target);break;
                    case 26:f.Scan(1,0);break;
                    case 27:f.Scan(0);break;
                    case 28:f.Em.AddComponentData(f.Target,LocalTransform.FromPosition(new float3(-500,0,-500)));break;
                    case 29:f.Transform.Position=new float3(10.5f,-0f,10.5f);f.Move.Speed=-0f;break;
                    case 30:f.Inputs.DeltaTime=10;f.Inputs.Blocked.Set(13+10*32,true);break;
                    case 31:f.Inputs.Blocked.Set(10+10*32,true);break;
                }
                var result=Compare(f,burst,"named="+scenario+" vehicle="+vehicle+" burst="+burst);
                if(scenario==3||scenario==4||scenario==6||scenario==7||scenario==8)Assert.AreEqual(Entity.Null,result.Engage.Target);
                if(scenario==8)Assert.AreEqual(7f,result.Kinematics.CurrentSpeed,"leash clear intentionally retains speed");
                if(scenario==2||scenario==3||scenario==4||scenario==6||scenario==7||scenario==10)Assert.AreEqual(0f,result.Kinematics.CurrentSpeed);
                if(scenario==0||scenario==1||scenario==2||scenario==3||scenario==4||scenario==6||scenario==7||scenario==8||scenario==10)Assert.AreEqual(original,result.Transform.Position);
                if(scenario==16)Assert.AreNotEqual(new int2(0,0),result.Grid.Cell);
                if(scenario==28){Assert.AreEqual(f.Engage.Position,result.Engage.Position);Assert.Greater(result.Transform.Position.x,original.x,"chase cached position, not target transform");}
            }
        }
        [Test] public void SeededManagedAndActualBurstMatchOriginal()
        {
            foreach(bool burst in new[]{false,true})for(int seed=0;seed<256;seed++)
            {
                using var f=new Fixture(seed%4==0);var r=new System.Random(71821+seed*97);f.Inputs.Grid.CellSize=new[]{.25f,1f,3.25f}[seed%3];f.Inputs.Grid.Origin=new float3((float)r.NextDouble()*5,0,(float)r.NextDouble()*5);
                float c=f.Inputs.Grid.CellSize;f.Transform=LocalTransform.FromPositionRotation(new float3((float)(r.NextDouble()*8+8)*c,(float)r.NextDouble()*10,(float)(r.NextDouble()*8+8)*c),quaternion.RotateY((float)r.NextDouble()*math.PI*2));
                f.SelfGrid.Cell=GridUtils.WorldToCell(f.Inputs.Grid,f.Transform.Position);f.Engage.Position=new float3((float)(r.NextDouble()*12+6)*c,(float)r.NextDouble()*10,(float)(r.NextDouble()*12+6)*c);f.Engage.Cell=GridUtils.WorldToCell(f.Inputs.Grid,f.Engage.Position);f.Engage.IsCommanded=(byte)(seed%2);
                f.Inputs.DeltaTime=(float)r.NextDouble()*2;f.Attack.Range=(float)r.NextDouble()*4*c;f.Move.Speed=(float)(r.NextDouble()*8-1);f.Move.RoadSpeedMultiplier=(float)r.NextDouble()*2;
                f.Combat.AggroRangeCells=r.Next(0,15);f.Combat.ChaseBreakDistance=(float)r.NextDouble()*20;f.Footprint.Size=new int2(r.Next(0,4),r.Next(0,4));f.Em.SetComponentData(f.Target,new UnitFootprint{Size=new int2(r.Next(0,5),r.Next(0,5))});f.Kinematics.CurrentSpeed=(float)r.NextDouble()*9;
                for(int i=0;i<1024;i++)
                {
                    if(r.Next(20)==0)f.Inputs.Blocked.Set(i,true);if(r.Next(7)==0)f.Inputs.Friendly[i]=1;
                    if(r.Next(16)==0)f.Inputs.Walkable[i]=new GridWalkable{Value=0};if(r.Next(4)==0)f.Inputs.Sidewalks[i]=new GridRoadSidewalk{Value=1};if(r.Next(4)==0)f.Inputs.Dirt[i]=new GridRoadDirt{Value=1};
                }
                if(seed%9==0)f.Em.AddComponent<HoldPositionOrderTag>(f.Source);if(seed%11==0)f.Scan(r.Next(1,8));if(seed%13==0)f.Em.SetComponentData(f.Target,new UnitHealth{Current=0});
                Compare(f,burst,"seed="+seed+" burst="+burst);
            }
        }
        private struct ProbeContext:IEngagedMovementContext
        {
            public NativeArray<int> Cells;public int Count,BlockX;
            public bool DebugTargetForSource()=>false;public bool TargetIsStatic()=>false;public bool TargetHasHealth()=>false;public int TargetHealth()=>0;public bool HoldingPosition()=>false;
            public bool TryReadScan(out EngagedScanOrder scan){scan=default;return false;}public bool TryTargetFootprint(out EngagedCell footprint){footprint=default;return false;}
            public EngagedCell WorldToCell(EngagedPosition p)=>new EngagedCell((int)math.floor(p.X),(int)math.floor(p.Z));
            public bool PreferredSurface(bool vehicle,int index)=>false;
            public bool CanMoveIntoCell(EngagedCell target,EngagedCell footprint,EngagedCell current,byte faction){Cells[Count++]=target.X+target.Z*32;return target.X!=BlockX;}
        }
        [BurstCompile(CompileSynchronously=true)] private struct ProbeJob:IJob
        {
            public ProbeContext Context;public NativeArray<int> Results;
            [BurstDiscard] private static void Managed(ref bool value){value=false;}
            public void Execute()
            {
                bool compiled=true;Managed(ref compiled);var input=new EngagedMovementInput{GridWidth=32,GridHeight=32,CellSize=1};
                bool legal=EngagedMovementKernel.CanMoveAlongSegment(ref Context,input,new EngagedPosition(.5f,0,.5f),new EngagedPosition(4.5f,0,.5f),new EngagedCell(1,1),new EngagedCell(0,0),1);
                Results[0]=legal?1:0;Results[1]=Context.Count;Results[2]=compiled?1:0;
            }
        }
        [Test] public void SharedSegmentSamplingFactsPreserveOrder()
        {
            using var cells=new NativeArray<int>(16,Allocator.TempJob);using var results=new NativeArray<int>(3,Allocator.TempJob);
            new ProbeJob{Context=new ProbeContext{Cells=cells,BlockX=3},Results=results}.Schedule().Complete();Assert.AreEqual(1,results[2]);Assert.AreEqual(0,results[0]);Assert.Greater(results[1],2);Assert.AreEqual(1,cells[0]);Assert.AreEqual(3,cells[results[1]-1]);
            for(int i=1;i<results[1];i++)Assert.GreaterOrEqual(cells[i],cells[i-1],"ordered samples, stop at first blocked fact");
        }
        [Test] public void ExistingPausedNativeMovementRegression()=>new UnitGroundMovementPauseTests().NativeMovementConservesPausedStateAndRetainsPositiveTimeBehavior();
    }
}
#endif
