#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Threading;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class MultiUnitPathNativeTraceTests
    {
        [Serializable] public sealed class Identity {public int index,version;public Identity(Entity e){index=e.Index;version=e.Version;}}
        [Serializable] public sealed class UnitRow
        {
            public string id;public int index,version,hp,cellX,cellZ,goalX,goalZ,requestX,requestZ,pathIndex,pathStart,pathLength,resumeFrame,targetIndex,targetVersion;
            public float x,y,z,speed,stall;public bool request,follow,range,engage,cooldown,manual,longDistance,deathFrozen;
            public int[] pathCells;
        }
        [Serializable] public sealed class AdmissionRow {public int engineFrame;public string decision;public bool consumedCompleted;}
        [Serializable] public sealed class BudgetRow
        {
            public PathRequestBudgetState before,after;public int pendingFrames,requestCount,manualCount,vehicleCount,pendingBudget;
            public double wallSeconds;public long wallBits;
        }
        [Serializable] public sealed class Frame
        {
            public int tick,engineFrame;public double elapsed,realtime;public bool simulationActive,playRequested=true;
            public NativePathTraceSnapshot pathBefore,pathAfter;
            public Identity[] acquisitionOrder,movementOrder,attackOrder,requestQueryOrderBeforePathfinding;
            public List<AdmissionRow> admission=new List<AdmissionRow>();public List<BudgetRow> budget=new List<BudgetRow>();
            public List<UnitRow> units=new List<UnitRow>();public UnitRow hostile;
        }
        [Serializable] public sealed class Trace
        {
            public string profile="IsolatedMultiUnitCombatTraceV1",scope="NativeSortedSubset_ExplicitHandleDispatch";
            public bool full_world=false,mission_terminal=false,nativePendingCompletionForced=false;
            public int width=32,height=16,unitCount=12,requestedX=26,requestedZ=7,wallX=14,wallZMin=0,wallZMax=11,workerYieldMilliseconds=1;
            public float cellSize=1,delta=.125f,moveSpeed=4,range=2;public int damage=100;public float cooldown=.1f;
            public string initialStage="PlayRequested1_SimulationActive0_until_actual_initial_paths_applied";
            public string requestOrderProvenance="Pre-system query can precede structural Apply. pathAfter.orderedRequests is the actual admitted readonly prefix; no full inside-Collect order is claimed.";
            public string acquisitionOrderProof="All active acquirers share faction1; exactly one eligible hostile identity, so friendly hash order cannot change selection.";
            public string[] systems;public List<Frame> frames=new List<Frame>();
            public bool sawMultiRequestBatch,sawDistinctAssignedGoals,sawCorridorDetour,sawCombat,sawDeath,sawRestore,sawResume,chainComplete;
        }
        [Serializable] private sealed class OrderReceipt {public string profile,scope="NativeSortedSubsetOnly";public bool full_world=false;public string[] systems;}
        private static Type[] MovingTypes(bool multi)
        {
            var t=new List<Type>{typeof(BeginSimulationEntityCommandBufferSystem),typeof(SkirmishObjectiveFactProjectionSystem),typeof(SkirmishBaseAssaultObjectiveSystem),typeof(DynamicOccupancyRebuildSystem),typeof(EngageTargetValidateSystem),typeof(UnitEngagementSystem),typeof(EngageTargetSyncSystem),typeof(UnitPathfindingSystem),typeof(UnitEngagedMovementSystem),typeof(UnitGridMovementSystem),typeof(UnitAttackSystem),typeof(UnitDeathSystem),typeof(SkirmishOutcomeSystem),typeof(EndSimulationEntityCommandBufferSystem)};
            if(multi)t.Add(typeof(UnitManualMoveRetrySystem));return t.ToArray();
        }
        private static string[] Names(World world,ComponentSystemGroup group)
        {using var handles=group.GetAllSystems();var names=new string[handles.Length];for(int i=0;i<handles.Length;i++)names[i]=TypeManager.GetSystemName(world.Unmanaged.GetSystemTypeIndex(handles[i])).ToString();return names;}
        private static ComponentSystemGroup Group(World world,bool multi)
        {var group=world.GetOrCreateSystemManaged<SimulationSystemGroup>();foreach(var t in MovingTypes(multi))group.AddSystemToUpdateList(world.GetOrCreateSystem(t));group.SortSystems();return group;}
        public static void RunCombinedValidation()
        {
            int failures=0,passed=0;
            void Check(string name,int expected,Func<int> run)
            {
                try{Assert.AreEqual(expected,run(),name+" count");passed+=expected;}
                catch(Exception e){failures++;Debug.LogError("[SharedMultiUnitPathSuite] result=Failed suite="+name);Debug.LogException(e);}
            }
            try
            {
                Check("routing",5,GridPathRoutingKernelTests.RunChecks);Check("mission",13,SkirmishMissionOrchestrationKernelTests.RunChecks);Check("search",4,GridPathSearchKernelTests.RunChecks);
                Check("occupancy",9,()=>{GridOccupancyKernelTests.RunChecks();return 9;});Check("placement",3,PathPlacementKernelTests.RunChecks);
                Check("pathMovement",9,()=>{GridPathMovementKernelTests.RunChecks();return 9;});Check("directDamage",5,()=>{DirectDamageSequencerTests.RunChecks();return 5;});
                Check("standardAttack",5,()=>{var t=new StandardAttackPlannerTests();t.OriginalOracleIsPinnedVerbatimProduction();t.InstalledMathNaNEqualAndClampSemanticsMatchKernel();t.ManagedRandomAndBoundaryPlansMatchEveryField();t.BurstScheduledPlansMatchOriginalAndManagedResults();t.ActualPrivateRuntimeAdapterPreservesEveryPlanField();return 5;});
                Check("nativeCombat",6,()=>{var t=new global::UnitCombatFocusedEditModeTests();t.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();t.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();t.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();t.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();t.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();t.AttackVfxRequestResolution_PopulatesImpactPlaybackData();return 6;});
                Check("scheduling",4,WorldSchedulingKernelTests.RunChecks);Check("squad",7,SkirmishSquadTargetingKernelTests.RunChecks);Check("acquisition",5,TargetAcquisitionKernelTests.RunChecks);
                Check("coordinates",3,GridCoordinateKernelTests.RunChecks);Check("pathScheduling",5,PathJobSchedulingKernelTests.RunChecks);Check("death",8,UnitDeathLifecycleKernelTests.RunChecks);Check("targetLifecycle",4,EngageTargetLifecycleKernelTests.RunChecks);
                Check("engaged",5,EngagedMovementKernelTests.RunChecks);Check("nativeChaseTrace",1,EngagedMovementNativeTraceTests.RunChecks);Check("infantryPath",6,InfantryPathLifecycleKernelTests.RunChecks);
                Check("multiUnitGoals",5,MultiUnitGoalAssignmentKernelTests.RunChecks);Check("requestBudgetAdmission",4,PathRequestAdmissionBudgetKernelTests.RunChecks);Check("retryRestoration",4,PathRetryRestorationKernelTests.RunChecks);Check("multiUnitNativeTraceAndProfiles",2,RunChecks);
                Assert.AreEqual(0,failures,"Every independent suite and capture must pass");Assert.AreEqual(122,passed);
                Debug.Log("[SharedMultiUnitPathParity] result=Passed prior=107 goals=5 requestBudget=4 retry=4 nativeTraceAndProfiles=2 total=122 missionReady=0");ValidationExit.Passed();
            }
            catch(Exception e){Debug.LogException(e);Debug.LogError("[SharedMultiUnitPathParity] result=Failed passed="+passed+" failedSuites="+failures+" missionReady=0");ValidationExit.Failed();}
        }
        public static int RunChecks(){CaptureProfiles();new MultiUnitPathNativeTraceTests().ActualMultiUnitRoutingCombatAndResume();return 2;}
        private static void CaptureProfiles()
        {
            foreach(bool multi in new[]{false,true})
            {
                using var world=new World("MultiUnitNativeOrderCapture");var group=Group(world,multi);var names=Names(world,group);Assert.AreEqual(multi?15:14,names.Length);
                if(multi)Assert.Less(Array.IndexOf(names,typeof(UnitManualMoveRetrySystem).FullName),Array.IndexOf(names,typeof(UnitPathfindingSystem).FullName));
                Debug.Log((multi?"[MultiUnitWorldSchedulingNativeOrder] ":"[MovingWorldSchedulingNativeOrder] ")+JsonUtility.ToJson(new OrderReceipt{profile=multi?"IsolatedMultiUnitCombatScheduleV1":"IsolatedMovingCombatScheduleV1",systems=names}));
            }
        }
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("MultiUnitNativeRoutingTrace");public EntityManager Em=>World.EntityManager;
            public NativeArray<int> Counts;public NativeArray<byte> Friendly;public NativeBitArray Blocked,Occupied;public NativeList<int2> Pool;
            public Entity Grid,Gate,Hostile;public readonly List<Entity> Units=new List<Entity>();public ComponentSystemGroup Systems;public SystemHandle Pathfinder;public string[] Order;
            public EntityQuery Acquisition,Movement,Attacks,Requests;
            public Fixture()
            {
                const int n=32*16;Counts=new NativeArray<int>(n,Allocator.Persistent);Friendly=new NativeArray<byte>(n,Allocator.Persistent);Blocked=new NativeBitArray(n,Allocator.Persistent);Occupied=new NativeBitArray(n,Allocator.Persistent);Pool=new NativeList<int2>(1024,Allocator.Persistent);
                Grid=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));Em.SetComponentData(Grid,new GridConfig{Width=32,Height=16,CellSize=1});
                Em.SetComponentData(Grid,new DynamicBlockerComponent{GridSize=n,Counts=Counts,Blocked=Blocked,FriendlyPassFactionIds=Friendly});Em.SetComponentData(Grid,new DynamicOccupancyComponent{GridSize=n,Occupied=Occupied});Em.SetComponentData(Grid,new PathPoolComponent{Cells=Pool});
                Em.AddBuffer<GridWalkable>(Grid);Em.AddBuffer<GridRoad>(Grid);Em.AddBuffer<GridRoadSidewalk>(Grid);Em.AddBuffer<GridRoadDirt>(Grid);
                for(int z=0;z<16;z++)for(int x=0;x<32;x++)
                {
                    int i=x+z*32;bool wall=x==14&&z<=11;Counts[i]=wall?1:0;Blocked.Set(i,wall);Friendly[i]=byte.MaxValue;
                    Em.GetBuffer<GridWalkable>(Grid).Add(new GridWalkable{Value=(byte)(wall?0:1)});Em.GetBuffer<GridRoad>(Grid).Add(default);Em.GetBuffer<GridRoadSidewalk>(Grid).Add(default);Em.GetBuffer<GridRoadDirt>(Grid).Add(default);
                }
                Gate=Em.CreateEntity(typeof(RuntimeGameplayStateComponent));SetActive(false);
                for(int i=0;i<12;i++)
                {
                    var e=CreateUnit(1,new float3(1.5f,0,i+1.5f),true);Units.Add(e);Em.AddComponentData(e,new UnitTarget{Cell=new int2(26,7)});Em.AddComponentData(e,new UnitPathRequest{Goal=new int2(26,7)});
                    Em.AddComponentData(e,new AttackMoveOrder{Destination=new int2(26,7),Position=new float3(26.5f,0,7.5f)});
                }
                Hostile=CreateUnit(2,new float3(25.5f,0,7.5f),false);
                Systems=Group(World,true);Order=Names(World,Systems);Pathfinder=World.GetExistingSystem<UnitPathfindingSystem>();
                Acquisition=Em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<Faction>(),ComponentType.ReadOnly<UnitCombat>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<LocalTransform>()},None=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<EngageTarget>(),ComponentType.ReadOnly<BaseBreachOrder>(),ComponentType.ReadOnly<GroundMissileInFlightComponent>(),ComponentType.ReadOnly<GroundMissileLauncherComponent>(),ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>()}});
                Movement=Em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadWrite<LocalTransform>(),ComponentType.ReadWrite<UnitGrid>(),ComponentType.ReadWrite<UnitPathFollow>(),ComponentType.ReadWrite<UnitVehicleKinematics>(),ComponentType.ReadOnly<UnitMove>(),ComponentType.ReadOnly<UnitPathRange>(),ComponentType.ReadOnly<UnitFootprint>(),ComponentType.ReadOnly<UnitMovementBehavior>(),ComponentType.ReadOnly<UnitVehicleMovement>(),ComponentType.ReadOnly<Faction>()},None=new[]{ComponentType.ReadOnly<EngageTarget>(),ComponentType.ReadOnly<UnitDeathAnimationComponent>(),ComponentType.ReadOnly<UnitAirMovement>(),ComponentType.ReadOnly<CampaignMissionStationaryUnitTag>()}});
                Attacks=Em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadWrite<EngageTarget>(),ComponentType.ReadWrite<UnitAttackCooldownComponent>(),ComponentType.ReadWrite<UnitAttackTraceComponent>(),ComponentType.ReadWrite<UnitAttackAnimationComponent>(),ComponentType.ReadOnly<LocalTransform>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<UnitHealth>()},None=new[]{ComponentType.ReadOnly<StaticGridBlocker>(),ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>(),ComponentType.ReadOnly<SupportPassengerTransitTag>(),ComponentType.ReadOnly<UnitDeathAnimationComponent>()}});
                Requests=Em.CreateEntityQuery(ComponentType.ReadOnly<UnitPathRequest>(),ComponentType.ReadOnly<UnitGrid>(),ComponentType.ReadOnly<UnitFootprint>(),ComponentType.ReadOnly<UnitMovementBehavior>(),ComponentType.ReadOnly<Faction>());
            }
            private Entity CreateUnit(byte faction,float3 position,bool armed)
            {
                var e=Em.CreateEntity(typeof(LocalTransform),typeof(UnitGrid),typeof(Faction),typeof(UnitHealth),typeof(UnitCombat),typeof(UnitAttack),typeof(UnitMove),typeof(UnitFootprint),typeof(UnitMovementBehavior),typeof(UnitVehicleMovement),typeof(UnitVehicleKinematics),typeof(UnitAttackCooldownComponent),typeof(UnitAttackTraceComponent),typeof(UnitAttackAnimationComponent),typeof(UnitAnimationSettings));
                Em.SetComponentData(e,LocalTransform.FromPosition(position));Em.SetComponentData(e,new UnitGrid{Cell=new int2((int)position.x,(int)position.z)});Em.SetComponentData(e,new Faction{Id=faction});Em.SetComponentData(e,new UnitHealth{Current=100,Max=100});
                Em.SetComponentData(e,new UnitCombat{CanAttack=(byte)(armed?1:0),AutoEngage=(byte)(armed?1:0),AggroRangeCells=3,ChaseBreakDistance=30});Em.SetComponentData(e,new UnitAttack{Range=2,Damage=100,CooldownSeconds=.1f});Em.SetComponentData(e,new UnitMove{Speed=4,RoadSpeedMultiplier=1,ArriveDistance=.05f});Em.SetComponentData(e,new UnitFootprint{Size=new int2(1,1)});Em.SetComponentData(e,new UnitAnimationSettings{AttackAnimationSeconds=.25f,DeathAnimationSeconds=.4f});return e;
            }
            public void SetActive(bool active)=>Em.SetComponentData(Gate,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=(byte)(active?1:0)});
            public bool InitialPathsReady(){foreach(var e in Units)if(Em.HasComponent<UnitPathRequest>(e)||!Em.HasComponent<UnitPathRange>(e)||!Em.HasComponent<UnitPathFollow>(e))return false;return !NativePathTraceProbe.Capture(World,Pathfinder).hasPending;}
            public void Dispose(){World.Dispose();Pool.Dispose();Friendly.Dispose();Occupied.Dispose();Blocked.Dispose();Counts.Dispose();}
        }
        private static Identity[] Ids(EntityQuery q){using var a=q.ToEntityArray(Allocator.Temp);var r=new Identity[a.Length];for(int i=0;i<a.Length;i++)r[i]=new Identity(a[i]);return r;}
        private static UnitRow Read(Fixture f,Entity e,string id)
        {
            var em=f.Em;var p=em.GetComponentData<LocalTransform>(e).Position;var c=em.GetComponentData<UnitGrid>(e).Cell;var k=em.GetComponentData<UnitVehicleKinematics>(e);var r=new UnitRow{id=id,index=e.Index,version=e.Version,x=p.x,y=p.y,z=p.z,cellX=c.x,cellZ=c.y,hp=em.GetComponentData<UnitHealth>(e).Current,speed=k.CurrentSpeed,stall=k.StallSeconds,request=em.HasComponent<UnitPathRequest>(e),follow=em.HasComponent<UnitPathFollow>(e),range=em.HasComponent<UnitPathRange>(e),engage=em.HasComponent<EngageTarget>(e),cooldown=em.HasComponent<UnitPathRetryCooldown>(e),manual=em.HasComponent<ManualMoveOrderTag>(e),longDistance=em.HasComponent<UnitLongDistanceMove>(e),deathFrozen=em.HasComponent<UnitDeathAnimationComponent>(e)&&em.GetComponentData<UnitDeathAnimationComponent>(e).PoseFrozen!=0};
            if(em.HasComponent<UnitTarget>(e)){var g=em.GetComponentData<UnitTarget>(e).Cell;r.goalX=g.x;r.goalZ=g.y;}if(r.request){var g=em.GetComponentData<UnitPathRequest>(e).Goal;r.requestX=g.x;r.requestZ=g.y;}if(r.follow)r.pathIndex=em.GetComponentData<UnitPathFollow>(e).PathIndex;
            if(r.range){var range=em.GetComponentData<UnitPathRange>(e);r.pathStart=range.Start;r.pathLength=range.Length;Assert.GreaterOrEqual(range.Start,0);Assert.LessOrEqual(range.Start+range.Length,f.Pool.Length);r.pathCells=new int[range.Length*2];for(int i=0;i<range.Length;i++){var v=f.Pool[range.Start+i];r.pathCells[i*2]=v.x;r.pathCells[i*2+1]=v.y;}}
            if(r.cooldown)r.resumeFrame=em.GetComponentData<UnitPathRetryCooldown>(e).ResumeFrame;if(r.engage){var target=em.GetComponentData<EngageTarget>(e).Target;r.targetIndex=target.Index;r.targetVersion=target.Version;}return r;
        }
        [Test] public void ActualMultiUnitRoutingCombatAndResume()
        {
            using var f=new Fixture();var trace=new Trace{systems=f.Order};Frame current=null;var oldAdmission=UnitPathfindingSystem.AdmissionObserved;var oldBudget=UnitPathfindingBudget.CompletionObserved;
            var engagedEver=new HashSet<int>();var restoredAt=new Dictionary<int,float>();var assignments=new HashSet<string>();bool active=false;
            UnitPathfindingSystem.AdmissionObserved=(engine,decision)=>{oldAdmission?.Invoke(engine,decision);current?.admission.Add(new AdmissionRow{engineFrame=engine,decision=decision.ToString(),consumedCompleted=decision==PathJobAdmission.ApplyThenSchedule});};
            UnitPathfindingBudget.CompletionObserved=o=>{oldBudget?.Invoke(o);current?.budget.Add(new BudgetRow{before=o.Before,after=o.After,pendingFrames=o.PendingFrames,requestCount=o.RequestCount,manualCount=o.ManualRequestCount,vehicleCount=o.VehicleLikeCount,pendingBudget=o.PendingRequestBudget,wallSeconds=o.PendingWallTime,wallBits=BitConverter.DoubleToInt64Bits(o.PendingWallTime)});};
            try
            {
                using var handles=f.Systems.GetAllSystems();
                for(int tick=0;tick<320;tick++)
                {
                    if(!active&&f.InitialPathsReady()){active=true;f.SetActive(true);}
                    current=new Frame{tick=tick,engineFrame=Time.frameCount,realtime=Time.realtimeSinceStartupAsDouble,elapsed=(tick+1)*.125,simulationActive=active};trace.frames.Add(current);f.World.SetTime(new TimeData(current.elapsed,.125f));
                    foreach(var handle in handles)
                    {
                        string name=TypeManager.GetSystemName(f.World.Unmanaged.GetSystemTypeIndex(handle)).ToString();
                        if(name==typeof(UnitEngagementSystem).FullName)current.acquisitionOrder=Ids(f.Acquisition);
                        if(name==typeof(UnitPathfindingSystem).FullName){current.requestQueryOrderBeforePathfinding=Ids(f.Requests);current.pathBefore=NativePathTraceProbe.Capture(f.World,f.Pathfinder);}
                        if(name==typeof(UnitGridMovementSystem).FullName)current.movementOrder=Ids(f.Movement);
                        if(name==typeof(UnitAttackSystem).FullName)current.attackOrder=Ids(f.Attacks);
                        if(name==typeof(BeginSimulationEntityCommandBufferSystem).FullName)f.World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>().Update();
                        else if(name==typeof(EndSimulationEntityCommandBufferSystem).FullName)f.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>().Update();
                        else handle.Update(f.World.Unmanaged);
                        if(name==typeof(UnitPathfindingSystem).FullName)current.pathAfter=NativePathTraceProbe.Capture(f.World,f.Pathfinder);
                    }
                    f.Em.CompleteAllTrackedJobs();current.hostile=Read(f,f.Hostile,"hostile");trace.sawDeath|=current.hostile.deathFrozen;
                    foreach(var e in f.Units)
                    {
                        var r=Read(f,e,"u"+f.Units.IndexOf(e).ToString("00"));current.units.Add(r);if(r.range)assignments.Add(r.goalX+":"+r.goalZ);if(r.x>14&&r.z>=12)trace.sawCorridorDetour=true;
                        if(r.engage){engagedEver.Add(e.Index);trace.sawCombat=true;}
                        if(current.hostile.hp<=0&&engagedEver.Contains(e.Index)&&!r.engage&&(r.request||r.follow))
                        {trace.sawRestore=true;if(!restoredAt.ContainsKey(e.Index))restoredAt[e.Index]=r.x;}
                        if(restoredAt.TryGetValue(e.Index,out float restoredX)&&r.x>restoredX+.1f)trace.sawResume=true;
                        Assert.IsFalse(r.cellX==14&&r.cellZ<=11,"real movement may not cross blocked wall");Assert.IsTrue(math.isfinite(r.x)&&math.isfinite(r.z));
                        if(!active){Assert.AreEqual(1.5f,r.x,"planner stage must respect native movement pause");}
                    }
                    trace.sawDistinctAssignedGoals=assignments.Count>1;trace.sawMultiRequestBatch|=current.pathAfter!=null&&current.pathAfter.pendingCount>1;
                    trace.chainComplete=trace.sawMultiRequestBatch&&trace.sawDistinctAssignedGoals&&trace.sawCorridorDetour&&trace.sawCombat&&trace.sawDeath&&trace.sawRestore&&trace.sawResume;
                    if(trace.chainComplete)break;
                    Thread.Sleep(1); // Worker yield only; production alone observes/completes its pending job.
                }
                Assert.IsTrue(trace.sawMultiRequestBatch,"native adaptive budget must actually admit competing requests in one batch");Assert.IsTrue(trace.sawDistinctAssignedGoals);Assert.IsTrue(trace.sawCorridorDetour);Assert.IsTrue(trace.sawCombat);Assert.IsTrue(trace.sawDeath);Assert.IsTrue(trace.sawRestore);Assert.IsTrue(trace.sawResume);Assert.IsTrue(trace.chainComplete);
                Debug.Log("[MultiUnitNativeTraceParity] result=Passed profile=IsolatedMultiUnitCombatTraceV1 pendingCompletionForced=0 missionReady=0");
            }
            finally
            {
                UnitPathfindingSystem.AdmissionObserved=oldAdmission;UnitPathfindingBudget.CompletionObserved=oldBudget;
                Debug.Log("[MultiUnitNativeTrace] "+JsonUtility.ToJson(trace));
            }
        }
    }
}
#endif
