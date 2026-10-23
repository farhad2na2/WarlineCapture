#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Targeting;
using Game.Skirmish.Contracts;
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
    [DisableAutoCreation] public partial class TargetAcquisitionLookupHarness:SystemBase
    {
        protected override void OnUpdate(){}
        internal UnitEngagementSystem.AcquireTargetsJob Build(GridConfig grid,NativeParallelMultiHashMap<int,Entity> map,NativeParallelHashMap<Entity,int> counts,NativeArray<Entity> buildings,EntityCommandBuffer.ParallelWriter ecb)
            => new UnitEngagementSystem.AcquireTargetsJob{Grid=grid,SpatialMap=map,AttackerCounts=counts,BuildingTargets=buildings,Ecb=ecb,
                FactionLookup=GetComponentLookup<Faction>(true),
                TransformLookup=GetComponentLookup<LocalTransform>(true),
                EngageLookup=GetComponentLookup<EngageTarget>(true),
                HealthLookup=GetComponentLookup<UnitHealth>(true),
                RecentAttackerLookup=GetComponentLookup<RecentAttacker>(true),
                ManualMoveLookup=GetComponentLookup<ManualMoveOrderTag>(true),
                AttackMoveLookup=GetComponentLookup<AttackMoveOrder>(true),
                PathFollowLookup=GetComponentLookup<UnitPathFollow>(true),
                PathRequestLookup=GetComponentLookup<UnitPathRequest>(true),
                HoldPositionLookup=GetComponentLookup<HoldPositionOrderTag>(true),
                ScanOrderLookup=GetComponentLookup<UnitScanOrder>(true),
                TargetPolicyLookup=GetComponentLookup<CombatTargetPolicy>(true),
                FogOwnedLookup=GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                FogSightLookup=GetComponentLookup<SkirmishContactSightComponent>(true),
                FogKnowledgeLookup=GetBufferLookup<SkirmishContactKnowledgeElement>(true),
                TargetBuildingLookup=GetComponentLookup<RuntimeBuildingCombatInfo>(true),
                ProtectedRecordsLookup=GetComponentLookup<CampaignMissionProtectedRecordsTag>(true),
                TargetBlockerLookup=GetComponentLookup<StaticGridBlocker>(true),
                TargetAirLookup=GetComponentLookup<UnitAirMovement>(true),
                TargetMovementLookup=GetComponentLookup<UnitMovementBehavior>(true),
            };
    }
    public sealed class TargetAcquisitionKernelTests
    {
        public static int RunChecks()
        {
            var tests=new TargetAcquisitionKernelTests();tests.OriginalSnapshotAndOracleArePinned();tests.NativeNamedAdmissionRetaliationAndOrderedTies();tests.NativePolicyFogAndIdentityCases();tests.SeededNativeManagedAndBurstParity();tests.ExistingAutomaticAcquisitionRegressions();
            Debug.Log("[TargetAcquisitionParity] result=Passed tests=5 nativeNamed=64 nativeSeeded=192 managed=Passed burst=Passed orderedEcb=Passed missionReady=0");return 5;
        }
        public static void RunCombinedValidation()
        {
            try
            {
                Assert.AreEqual(5,GridPathRoutingKernelTests.RunChecks());Assert.AreEqual(13,SkirmishMissionOrchestrationKernelTests.RunChecks());
                GridPathSearchKernelTests.RunChecks();GridOccupancyKernelTests.RunChecks();Assert.AreEqual(3,PathPlacementKernelTests.RunChecks());GridPathMovementKernelTests.RunChecks();DirectDamageSequencerTests.RunChecks();
                var attack=new StandardAttackPlannerTests();attack.OriginalOracleIsPinnedVerbatimProduction();attack.InstalledMathNaNEqualAndClampSemanticsMatchKernel();attack.ManagedRandomAndBoundaryPlansMatchEveryField();attack.BurstScheduledPlansMatchOriginalAndManagedResults();attack.ActualPrivateRuntimeAdapterPreservesEveryPlanField();
                var combat=new global::UnitCombatFocusedEditModeTests();combat.StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();combat.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();combat.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();combat.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();combat.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();combat.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
                Assert.AreEqual(4,WorldSchedulingKernelTests.RunChecks());Assert.AreEqual(7,SkirmishSquadTargetingKernelTests.RunChecks());Assert.AreEqual(5,RunChecks());
                Debug.Log("[SharedWorldTargetingParity] result=Passed prior=59 scheduling=4 squad=7 acquisition=5 total=75 missionReady=0");ValidationExit.Passed();
            }
            catch(Exception e){Debug.LogException(e);Debug.LogError("[SharedWorldTargetingParity] result=Failed missionReady=0");ValidationExit.Failed();}
        }
        [Test] public void OriginalSnapshotAndOracleArePinned()
        {
            foreach(bool isStatic in new[]{false,true})foreach(bool hasHealth in new[]{false,true})foreach(int health in new[]{-1,0,1,int.MinValue,int.MaxValue})
                Assert.AreEqual(isStatic||(hasHealth&&health<=0),TargetAcquisitionKernel.ShouldClearUnavailableTarget(isStatic,hasHealth,health));
            string original=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/TargetAcquisitionOriginalSource.cs.txt")).Replace("\r\n","\n");
            using var sha=SHA256.Create();Assert.AreEqual("265d75666daf50a05863f418f703105c736e70b28a811c1615ec02d3092785e7",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            string expected="#if UNITY_EDITOR\n"+original.Replace("UnitEngagementSystem","TargetAcquisitionOriginalSystem").Replace("    [BurstCompile]\n    [UpdateAfter","    [DisableAutoCreation]\n    [BurstCompile]\n    [UpdateAfter").Replace("private partial struct AcquireTargetsJob","internal partial struct AcquireTargetsJob")+"\n#endif\n";
            Assert.AreEqual(expected,File.ReadAllText(Path.Combine(Application.dataPath,"Game/Scripts/Systems/TargetAcquisitionOriginalSystem.cs")).Replace("\r\n","\n"));
        }
        // Store only initialized native inputs in the scheduled wrapper. The actual
        // IJobEntity structs are constructed locally so their unused generated query
        // type handles are not scheduled as uninitialized native containers.
        [BurstCompile(CompileSynchronously=true)] private struct PairJob:IJob
        {
            [ReadOnly] public GridConfig Grid;
            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
            [ReadOnly] public NativeParallelHashMap<Entity, int> AttackerCounts;
            [ReadOnly] public NativeArray<Entity> BuildingTargets;
            [ReadOnly] public ComponentLookup<Faction> FactionLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
            [ReadOnly] public ComponentLookup<RecentAttacker> RecentAttackerLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;
            [ReadOnly] public ComponentLookup<AttackMoveOrder> AttackMoveLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> PathFollowLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
            [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
            [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;
            [ReadOnly] public ComponentLookup<CombatTargetPolicy> TargetPolicyLookup;
            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwnedLookup;
            [ReadOnly] public ComponentLookup<SkirmishContactSightComponent> FogSightLookup;
            [ReadOnly] public BufferLookup<SkirmishContactKnowledgeElement> FogKnowledgeLookup;
            [ReadOnly] public ComponentLookup<RuntimeBuildingCombatInfo> TargetBuildingLookup;
            [ReadOnly] public ComponentLookup<CampaignMissionProtectedRecordsTag> ProtectedRecordsLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> TargetBlockerLookup;
            [ReadOnly] public ComponentLookup<UnitAirMovement> TargetAirLookup;
            [ReadOnly] public ComponentLookup<UnitMovementBehavior> TargetMovementLookup;
            public EntityCommandBuffer.ParallelWriter ActualEcb,OriginalEcb;
            public Entity Source;public UnitGrid SelfGrid;public Faction SelfFaction;public UnitCombat Combat;public UnitAttack Attack;public LocalTransform Transform;public NativeArray<int> Compiled;
            [BurstDiscard] private static void Managed(ref bool value){value=false;}
            public void Execute()
            {
                bool compiled=true;Managed(ref compiled);Compiled[0]=compiled?1:0;
                var original=new TargetAcquisitionOriginalSystem.AcquireTargetsJob{
                    Grid=Grid,
                    SpatialMap=SpatialMap,
                    AttackerCounts=AttackerCounts,
                    BuildingTargets=BuildingTargets,
                    FactionLookup=FactionLookup,
                    TransformLookup=TransformLookup,
                    EngageLookup=EngageLookup,
                    HealthLookup=HealthLookup,
                    RecentAttackerLookup=RecentAttackerLookup,
                    ManualMoveLookup=ManualMoveLookup,
                    AttackMoveLookup=AttackMoveLookup,
                    PathFollowLookup=PathFollowLookup,
                    PathRequestLookup=PathRequestLookup,
                    HoldPositionLookup=HoldPositionLookup,
                    ScanOrderLookup=ScanOrderLookup,
                    TargetPolicyLookup=TargetPolicyLookup,
                    FogOwnedLookup=FogOwnedLookup,
                    FogSightLookup=FogSightLookup,
                    FogKnowledgeLookup=FogKnowledgeLookup,
                    TargetBuildingLookup=TargetBuildingLookup,
                    ProtectedRecordsLookup=ProtectedRecordsLookup,
                    TargetBlockerLookup=TargetBlockerLookup,
                    TargetAirLookup=TargetAirLookup,
                    TargetMovementLookup=TargetMovementLookup,
                    Ecb=OriginalEcb};
                var actual=new UnitEngagementSystem.AcquireTargetsJob{
                    Grid=Grid,
                    SpatialMap=SpatialMap,
                    AttackerCounts=AttackerCounts,
                    BuildingTargets=BuildingTargets,
                    FactionLookup=FactionLookup,
                    TransformLookup=TransformLookup,
                    EngageLookup=EngageLookup,
                    HealthLookup=HealthLookup,
                    RecentAttackerLookup=RecentAttackerLookup,
                    ManualMoveLookup=ManualMoveLookup,
                    AttackMoveLookup=AttackMoveLookup,
                    PathFollowLookup=PathFollowLookup,
                    PathRequestLookup=PathRequestLookup,
                    HoldPositionLookup=HoldPositionLookup,
                    ScanOrderLookup=ScanOrderLookup,
                    TargetPolicyLookup=TargetPolicyLookup,
                    FogOwnedLookup=FogOwnedLookup,
                    FogSightLookup=FogSightLookup,
                    FogKnowledgeLookup=FogKnowledgeLookup,
                    TargetBuildingLookup=TargetBuildingLookup,
                    ProtectedRecordsLookup=ProtectedRecordsLookup,
                    TargetBlockerLookup=TargetBlockerLookup,
                    TargetAirLookup=TargetAirLookup,
                    TargetMovementLookup=TargetMovementLookup,
                    Ecb=ActualEcb};
                original.Execute(17,Source,SelfGrid,SelfFaction,Combat,Attack,Transform);actual.Execute(17,Source,SelfGrid,SelfFaction,Combat,Attack,Transform);
            }
        }
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("AcquisitionExactCapturedMap");public EntityManager Em=>World.EntityManager;
            public GridConfig Grid=new GridConfig{Width=32,Height=32,CellSize=1};
            public UnitCombat Combat=new UnitCombat{CanAttack=1,AutoEngage=1,AggroRangeCells=8};public UnitAttack Attack=new UnitAttack{Range=3};
            public Entity Source,Enemy,Other,Ally,Building;public NativeParallelMultiHashMap<int,Entity> Map;public NativeParallelHashMap<Entity,int> Counts;public NativeArray<Entity> Buildings;
            public Fixture()
            {
                Map=new NativeParallelMultiHashMap<int,Entity>(64,Allocator.TempJob);Counts=new NativeParallelHashMap<Entity,int>(32,Allocator.TempJob);Buildings=new NativeArray<Entity>(1,Allocator.TempJob);
                Source=Unit(1,new float3(10,9,10));Enemy=Unit(2,new float3(12,3,10));Other=Unit(2,new float3(13,0,10));Ally=Unit(1,new float3(11,0,10));Building=Unit(2,new float3(11,0,11));
                Em.AddComponent<StaticGridBlocker>(Building);Buildings[0]=Building;Map.Add(10+10*32,Source);Map.Add(12+10*32,Enemy);Map.Add(13+10*32,Other);
                RestorePaths();
            }
            public Entity Unit(byte faction,float3 position)
            {
                var e=Em.CreateEntity(typeof(Faction),typeof(LocalTransform),typeof(UnitHealth),typeof(UnitGrid));Em.SetComponentData(e,new Faction{Id=faction});Em.SetComponentData(e,LocalTransform.FromPosition(position));Em.SetComponentData(e,new UnitHealth{Current=50,Max=100});Em.SetComponentData(e,new UnitGrid{Cell=GridUtils.WorldToCell(Grid,position)});return e;
            }
            public void Pos(Entity e,float3 p)=>Em.SetComponentData(e,LocalTransform.FromPosition(p));
            public void Policy(Entity e,CombatTargetDomain allowed,CombatTargetDomain domain,byte visible=1)=>Em.AddComponentData(e,new CombatTargetPolicy{AllowedTargets=allowed,Domain=domain,Visible=visible});
            public void Recent(Entity e)=>Em.AddComponentData(Source,new RecentAttacker{Attacker=e});
            public void Scan(int x=12,int z=10,int radius=2)=>Em.AddComponentData(Source,new UnitScanOrder{CenterCell=new int2(x,z),RadiusCells=radius,HasStarted=1,EngageDetectedTargets=1});
            public void Owner(Entity e,string session,byte faction)=>Em.AddComponentData(e,new SkirmishAttemptOwnedComponent{SessionId=new FixedString64Bytes(session),FactionId=faction});
            public void Knowledge(Entity e,byte observer,SkirmishContactSight sight)=>Em.AddBuffer<SkirmishContactKnowledgeElement>(e).Add(new SkirmishContactKnowledgeElement{ObserverFactionId=observer,Sight=sight});
            public void RestorePaths(){Em.AddComponent<UnitPathFollow>(Source);Em.AddComponent<UnitPathRange>(Source);Em.AddComponent<UnitPathRequest>(Source);Em.AddComponent<AutoWanderMoveTag>(Source);}
            public void Dispose(){World.Dispose();Map.Dispose();Counts.Dispose();Buildings.Dispose();}
        }
        private struct State
        {
            public bool Engage,Recent,Follow,Range,Request,Wander,Manual,AttackMove;public EngageTarget Target;public RecentAttacker RecentValue;
        }
        private static State Read(Fixture f)
        {
            var em=f.Em;var e=f.Source;var s=new State{Engage=em.HasComponent<EngageTarget>(e),Recent=em.HasComponent<RecentAttacker>(e),Follow=em.HasComponent<UnitPathFollow>(e),Range=em.HasComponent<UnitPathRange>(e),Request=em.HasComponent<UnitPathRequest>(e),Wander=em.HasComponent<AutoWanderMoveTag>(e),Manual=em.HasComponent<ManualMoveOrderTag>(e),AttackMove=em.HasComponent<AttackMoveOrder>(e)};
            if(s.Engage)s.Target=em.GetComponentData<EngageTarget>(e);if(s.Recent)s.RecentValue=em.GetComponentData<RecentAttacker>(e);return s;
        }
        private static void Same(State a,State b,string context)
        {
            Assert.AreEqual(a.Engage,b.Engage,context);Assert.AreEqual(a.Recent,b.Recent,context);Assert.AreEqual(a.Follow,b.Follow,context);Assert.AreEqual(a.Range,b.Range,context);Assert.AreEqual(a.Request,b.Request,context);Assert.AreEqual(a.Wander,b.Wander,context);Assert.AreEqual(a.Manual,b.Manual,context);Assert.AreEqual(a.AttackMove,b.AttackMove,context);
            if(a.Recent)Assert.AreEqual(a.RecentValue.Attacker,b.RecentValue.Attacker,context);
            if(a.Engage){Assert.AreEqual(a.Target.Target,b.Target.Target,context);Assert.AreEqual(a.Target.Cell,b.Target.Cell,context);Assert.AreEqual(a.Target.IsCommanded,b.Target.IsCommanded,context);Assert.AreEqual(math.asint(a.Target.Position),math.asint(b.Target.Position),context);}
        }
        private static string[] Commands(EntityCommandBuffer ecb)
        {
            var type=typeof(EntityCommandBuffer).GetNestedType("EntityCommandBufferDebugView",BindingFlags.NonPublic);Assert.NotNull(type,"installed package debug view");
            var view=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{ecb},null);var commands=(Array)type.GetProperty("Commands").GetValue(view);var result=new string[commands.Length];
            for(int i=0;i<commands.Length;i++){object c=commands.GetValue(i);var t=c.GetType();result[i]=t.GetField("CommandType").GetValue(c)+"|"+t.GetField("SortKey").GetValue(c)+"|"+t.GetField("Entity").GetValue(c)+"|"+t.GetField("ComponentTypeIndex").GetValue(c);}
            return result;
        }
        private static State Compare(Fixture f,bool burst,string label)
        {
            using var expectedEcb=new EntityCommandBuffer(Allocator.TempJob);using var actualEcb=new EntityCommandBuffer(Allocator.TempJob);using var compiled=new NativeArray<int>(1,Allocator.TempJob);
            var harness=f.World.GetOrCreateSystemManaged<TargetAcquisitionLookupHarness>();var actual=harness.Build(f.Grid,f.Map,f.Counts,f.Buildings,actualEcb.AsParallelWriter());
            var job=new PairJob{Grid=actual.Grid,SpatialMap=actual.SpatialMap,AttackerCounts=actual.AttackerCounts,BuildingTargets=actual.BuildingTargets,FactionLookup=actual.FactionLookup,TransformLookup=actual.TransformLookup,EngageLookup=actual.EngageLookup,HealthLookup=actual.HealthLookup,RecentAttackerLookup=actual.RecentAttackerLookup,ManualMoveLookup=actual.ManualMoveLookup,AttackMoveLookup=actual.AttackMoveLookup,PathFollowLookup=actual.PathFollowLookup,PathRequestLookup=actual.PathRequestLookup,HoldPositionLookup=actual.HoldPositionLookup,ScanOrderLookup=actual.ScanOrderLookup,TargetPolicyLookup=actual.TargetPolicyLookup,FogOwnedLookup=actual.FogOwnedLookup,FogSightLookup=actual.FogSightLookup,FogKnowledgeLookup=actual.FogKnowledgeLookup,TargetBuildingLookup=actual.TargetBuildingLookup,ProtectedRecordsLookup=actual.ProtectedRecordsLookup,TargetBlockerLookup=actual.TargetBlockerLookup,TargetAirLookup=actual.TargetAirLookup,TargetMovementLookup=actual.TargetMovementLookup,ActualEcb=actualEcb.AsParallelWriter(),OriginalEcb=expectedEcb.AsParallelWriter(),Source=f.Source,SelfGrid=f.Em.GetComponentData<UnitGrid>(f.Source),SelfFaction=f.Em.GetComponentData<Faction>(f.Source),Combat=f.Combat,Attack=f.Attack,Transform=f.Em.GetComponentData<LocalTransform>(f.Source),Compiled=compiled};
            var before=Read(f);int countBefore=f.Counts.Count();if(burst)job.Schedule().Complete();else job.Execute();Assert.AreEqual(burst?1:0,compiled[0],label+" compiled sentinel");Same(before,Read(f),label+" stays deferred");
            Assert.AreEqual(countBefore,f.Counts.Count(),"attacker snapshot immutable");CollectionAssert.AreEqual(Commands(expectedEcb),Commands(actualEcb),label+" exact ECB sequence");
            expectedEcb.Playback(f.Em);var expected=Read(f);f.Em.RemoveComponent<EngageTarget>(f.Source);f.RestorePaths();if(before.Recent)f.Em.AddComponentData(f.Source,before.RecentValue);else f.Em.RemoveComponent<RecentAttacker>(f.Source);
            actualEcb.Playback(f.Em);var output=Read(f);Same(expected,output,label+" exact native output");return output;
        }

        [Test] public void NativeNamedAdmissionRetaliationAndOrderedTies()
        {
            foreach(bool burst in new[]{false,true})for(int scenario=0;scenario<16;scenario++)
            {
                using var f=new Fixture();Entity expected=f.Enemy;bool retainedRecent=false;
                switch(scenario)
                {
                    case 0:f.Combat.CanAttack=0;f.Recent(f.Enemy);expected=Entity.Null;retainedRecent=true;break;
                    case 1:f.Combat.AutoEngage=0;f.Recent(f.Enemy);expected=Entity.Null;retainedRecent=true;break;
                    case 2:f.Em.SetComponentData(f.Source,new Faction{Id=0});f.Recent(f.Enemy);expected=Entity.Null;retainedRecent=true;break;
                    case 3:f.Combat.AggroRangeCells=0;f.Attack.Range=0;f.Recent(f.Enemy);expected=Entity.Null;retainedRecent=true;break;
                    case 4:f.Em.AddComponent<ManualMoveOrderTag>(f.Source);f.Recent(f.Enemy);expected=Entity.Null;break;
                    case 5:f.Em.AddComponent<ManualMoveOrderTag>(f.Source);f.Em.RemoveComponent<UnitPathFollow>(f.Source);f.Em.RemoveComponent<UnitPathRequest>(f.Source);break;
                    case 6:f.Em.AddComponent<ManualMoveOrderTag>(f.Source);f.Em.AddComponent<AttackMoveOrder>(f.Source);f.Combat.AutoEngage=0;break;
                    case 7:f.Em.AddComponent<HoldPositionOrderTag>(f.Source);f.Attack.Range=1;expected=Entity.Null;break;
                    case 8:f.Pos(f.Other,new float3(80,14,80));f.Recent(f.Other);expected=f.Other;break;
                    case 9:f.Pos(f.Other,new float3(80,14,80));f.Recent(f.Other);f.Em.AddComponent<HoldPositionOrderTag>(f.Source);f.Attack.Range=1;expected=Entity.Null;break;
                    case 10:f.Em.AddComponent<CampaignMissionProtectedRecordsTag>(f.Enemy);expected=f.Other;break;
                    case 11:f.Em.AddComponent<CampaignMissionProtectedRecordsTag>(f.Enemy);f.Recent(f.Enemy);break;
                    case 12:
                        f.Map.Clear();f.Pos(f.Other,new float3(12,0,10));f.Map.Add(332,f.Enemy);f.Map.Add(332,f.Other);f.Map.TryGetFirstValue(332,out expected,out _);break;
                    case 13:f.Map.Clear();f.Pos(f.Enemy,new float3(8,0,10));f.Pos(f.Other,new float3(10,0,8));f.Map.Add(328,f.Enemy);f.Map.Add(266,f.Other);expected=f.Other;break;
                    case 14:f.Counts.Add(f.Enemy,1);expected=f.Other;break;
                    case 15:f.Map.Clear();f.Map.Add(331,f.Ally);f.Em.AddComponentData(f.Ally,new EngageTarget{Target=f.Other});expected=f.Other;break;
                }
                var result=Compare(f,burst,"admission "+scenario);Assert.AreEqual(expected!=Entity.Null,result.Engage,"admission "+scenario);if(result.Engage)Assert.AreEqual(expected,result.Target.Target,"admission target "+scenario);Assert.AreEqual(retainedRecent,result.Recent,"recent state "+scenario);
                if(scenario==8)Assert.AreEqual(new int2(80,80),result.Target.Cell,"retaliation intentionally keeps out-of-bounds target cell");
                if(scenario==14)Assert.AreEqual(1,f.Counts[f.Enemy],"snapshot value unchanged");
            }
        }
        [Test] public void NativePolicyFogAndIdentityCases()
        {
            foreach(bool burst in new[]{false,true})for(int scenario=0;scenario<16;scenario++)
            {
                using var f=new Fixture();Entity expected=f.Enemy;
                switch(scenario)
                {
                    case 0:f.Map.Clear();f.Em.AddComponent<AttackMoveOrder>(f.Source);f.Policy(f.Source,CombatTargetDomain.Structure,CombatTargetDomain.Infantry);expected=f.Building;break;
                    case 1:f.Em.AddComponent<UnitAirMovement>(f.Enemy);f.Policy(f.Source,CombatTargetDomain.Infantry,CombatTargetDomain.Infantry);expected=f.Other;break;
                    case 2:f.Em.AddComponentData(f.Enemy,new UnitMovementBehavior{UsesVehicleMotion=1});f.Policy(f.Source,CombatTargetDomain.Ground,CombatTargetDomain.Infantry);break;
                    case 3:f.Policy(f.Source,CombatTargetDomain.Ground,CombatTargetDomain.Infantry);f.Policy(f.Enemy,CombatTargetDomain.All,CombatTargetDomain.Air);expected=Entity.Null;break;
                    case 4:f.Policy(f.Enemy,CombatTargetDomain.All,CombatTargetDomain.Infantry,0);expected=f.Other;break;
                    case 5:f.Policy(f.Enemy,CombatTargetDomain.All,CombatTargetDomain.Infantry,0);f.Owner(f.Source,"same",3);f.Owner(f.Enemy,"same",2);f.Knowledge(f.Enemy,3,SkirmishContactSight.Visible);break;
                    case 6:f.Owner(f.Source,"a",3);f.Owner(f.Enemy,"b",2);f.Knowledge(f.Enemy,3,SkirmishContactSight.Visible);expected=f.Other;break;
                    case 7:f.Owner(f.Source,"same",3);f.Owner(f.Enemy,"same",2);expected=f.Other;break;
                    case 8:f.Owner(f.Source,"same",3);f.Owner(f.Enemy,"same",2);f.Knowledge(f.Enemy,1,SkirmishContactSight.Visible);expected=f.Other;break;
                    case 9:f.Owner(f.Source,"same",1);f.Owner(f.Enemy,"same",2);f.Em.AddComponentData(f.Enemy,new SkirmishContactSightComponent{Sight=SkirmishContactSight.Visible});break;
                    case 10:f.Owner(f.Enemy,"unpaired",2);f.Policy(f.Enemy,CombatTargetDomain.All,CombatTargetDomain.Infantry,0);expected=f.Other;break;
                    case 11:f.Recent(new Entity{Index=f.Enemy.Index,Version=f.Enemy.Version+1});break;
                    case 12:f.Em.RemoveComponent<UnitHealth>(f.Enemy);break;
                    case 13:f.Em.SetComponentData(f.Enemy,new UnitHealth{Current=0});expected=f.Other;break;
                    case 14:f.Em.SetComponentData(f.Enemy,new Faction{Id=0});expected=f.Other;break;
                    case 15:f.Em.AddComponent<ManualMoveOrderTag>(f.Source);f.Scan(13,10,1);break;
                }
                var result=Compare(f,burst,"policy "+scenario);Assert.AreEqual(expected!=Entity.Null,result.Engage,"policy "+scenario);if(result.Engage)Assert.AreEqual(expected,result.Target.Target,"policy target "+scenario);
            }
        }
        [Test] public void SeededNativeManagedAndBurstParity()
        {
            foreach(bool burst in new[]{false,true})for(int seed=0;seed<96;seed++)
            {
                using var f=new Fixture();var r=new System.Random(seed*971+3827);f.Map.Clear();f.Combat.AggroRangeCells=r.Next(0,10);f.Attack.Range=(float)(r.NextDouble()*7);f.Grid.CellSize=new[]{.5f,1f,2.75f}[seed%3];
                f.Pos(f.Source,new float3(10*f.Grid.CellSize,12,10*f.Grid.CellSize));f.Em.SetComponentData(f.Source,new UnitGrid{Cell=new int2(10,10)});
                Entity[] targets={f.Enemy,f.Other,f.Ally};foreach(var e in targets)
                {
                    var pos=new float3((float)(r.NextDouble()*15+3)*f.Grid.CellSize,(float)r.NextDouble()*10,(float)(r.NextDouble()*15+3)*f.Grid.CellSize);f.Pos(e,pos);var cell=GridUtils.WorldToCell(f.Grid,pos);f.Map.Add(cell.x+cell.y*f.Grid.Width,e);
                    f.Em.SetComponentData(e,new Faction{Id=(byte)r.Next(0,4)});f.Em.SetComponentData(e,new UnitHealth{Current=r.Next(-1,3)});f.Counts.TryAdd(e,r.Next(0,4));
                    if(r.Next(3)==0)f.Policy(e,CombatTargetDomain.All,(CombatTargetDomain)(1<<r.Next(4)),(byte)r.Next(0,2));if(r.Next(6)==0)f.Em.AddComponent<CampaignMissionProtectedRecordsTag>(e);
                }
                if(seed%3==0)f.Recent(seed%2==0?f.Enemy:f.Other);if(seed%4==0)f.Em.AddComponent<HoldPositionOrderTag>(f.Source);if(seed%5==0)f.Em.AddComponent<ManualMoveOrderTag>(f.Source);if(seed%7==0)f.Em.AddComponent<AttackMoveOrder>(f.Source);if(seed%8==0)f.Scan(r.Next(8,15),r.Next(8,15),r.Next(1,5));if(seed%6==0)f.Policy(f.Source,(CombatTargetDomain)r.Next(0,16),CombatTargetDomain.Infantry);
                Compare(f,burst,"seed="+seed+" burst="+burst);
            }
        }
        [Test] public void ExistingAutomaticAcquisitionRegressions()
        {
            var tests=new global::AutomaticCombatFactionTargetingTests();tests.AcquisitionAndRetaliationRespectAuthoredDomainsAndVisibility();tests.AttackMove_AcquiresHostileBuildingButIgnoresNeutralDeadAndSuppressedBuildings();tests.AttackMove_InterruptsCommandedPathForHostileButPlainMoveDoesNot();tests.UnitEngagementSystem_IgnoresNeutralCitizenTargets();tests.UnitEngagementSystem_HoldAcquiresHostileAndHonorsSuppressionWithRealEcbSingleton();
        }
    }
}
#endif
