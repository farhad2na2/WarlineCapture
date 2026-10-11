using System;
using Game.Components;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishFogConsumerTests
    {
        private const string Attempt = "fog.consumer.actual";
        public static void RunFocusedValidation()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Consumer focused suite requires an idle Editor.");
            var t = new SkirmishFogConsumerTests();
            t.CombatPolicyUsesActualObserverThroughVisibilityLifecycle(1);
            t.CombatPolicyUsesActualObserverThroughVisibilityLifecycle(2);
            t.HiddenPrecisionRequestAndEngageAreRejectedBeforePursuit(1);
            t.HiddenPrecisionRequestAndEngageAreRejectedBeforePursuit(2);
            t.CampaignUnownedCombatAndMinimapRemainAvailable();
            t.MinimapFreezesPoseAndDropsPrecisionHandleBetweenRefreshes();
            t.DisabledLastSeenAndDestroyedContactPersistOnlyUntilExpiry();
            t.ObservedDeadContactDoesNotBecomeFrozenMarker();
            t.FinalMeshMaskPreservesOriginalFalseAndOtherOwners();
            t.VisibleIncomingFlightDoesNotRevealHiddenLauncher();
            t.HiddenTargetHomingRetainsAcceptedDestination();
            t.ActualPickingAndFocusedHudRejectUnknownAndLastSeen();
            t.EnemyApproachesPublicObjectiveThenAttacksObservedBase();
            Debug.Log("[SkirmishFogConsumerTests] result=Passed cases=13 scope=IsolatedActualSystemsNotNormalMission");
        }
        [TestCase((byte)1)] [TestCase((byte)2)]
        public void CombatPolicyUsesActualObserverThroughVisibilityLifecycle(byte faction)
        {
            using var w = new World(nameof(CombatPolicyUsesActualObserverThroughVisibilityLifecycle));
            var em = w.EntityManager; Session(em);
            Entity observer = Actor(em,"observer",faction,0,5);
            Entity target = Actor(em,"target",faction == 1 ? (byte)2 : (byte)1,100,0);
            em.AddComponentData(observer,new CombatTargetPolicy { AllowedTargets=CombatTargetDomain.All,Visible=1 });
            em.AddComponentData(target,new CombatTargetPolicy { Domain=CombatTargetDomain.Infantry,Visible=1 });
            Step(w,1); Assert.IsFalse(CombatTargetPolicyUtility.Allows(em,observer,target));
            Move(em,target,4); Step(w,2); Assert.IsTrue(CombatTargetPolicyUtility.Allows(em,observer,target));
            Move(em,target,100); Step(w,3); Assert.IsFalse(CombatTargetPolicyUtility.Allows(em,observer,target));
            Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em,target,faction,out var stale));
            Assert.AreEqual(SkirmishContactSight.LastSeen,stale.Sight);
            Step(w,22); Assert.IsFalse(CombatTargetPolicyUtility.Allows(em,observer,target));
            Assert.AreEqual(1,em.GetComponentData<CombatTargetPolicy>(target).Visible);
        }
        [TestCase((byte)1)] [TestCase((byte)2)]
        public void HiddenPrecisionRequestAndEngageAreRejectedBeforePursuit(byte faction)
        {
            using var w = new World(nameof(HiddenPrecisionRequestAndEngageAreRejectedBeforePursuit));
            var em=w.EntityManager;Session(em);
            Entity source=Actor(em,"source",faction,0,5), target=Actor(em,"hidden",faction==1?(byte)2:(byte)1,100,0);
            em.AddComponentData(source,new UnitMove { Speed=3 });
            em.AddComponentData(source,new UnitTarget { Cell=new int2(12,0) });
            em.AddComponentData(source,new UnitAttack { Range=5,Damage=10 });
            em.AddComponentData(source,new CombatTargetPolicy { AllowedTargets=CombatTargetDomain.All,Visible=1 });
            Step(w,1);
            Assert.IsFalse(UnitAttackOrderRequestSystem.EnqueueAndProcessDirectAttackTarget(em,source,target,new int2(100,0),new float3(100,0,0)));
            Assert.IsFalse(em.HasComponent<EngageTarget>(source));
            // First acquire through the real native request, then lose observation.
            Move(em,target,4);Step(w,2);
            Assert.IsTrue(UnitAttackOrderRequestSystem.EnqueueAndProcessDirectAttackTarget(em,source,target,new int2(4,0),new float3(4,0,0)));
            Assert.AreEqual(target,em.GetComponentData<EngageTarget>(source).Target);
            Move(em,target,100);Step(w,3);
            w.GetOrCreateSystem<EngageTargetValidateSystem>().Update(w.Unmanaged);em.CompleteAllTrackedJobs();
            Assert.IsFalse(em.HasComponent<EngageTarget>(source));
            Assert.IsFalse(em.HasComponent<UnitPathRequest>(source),"Lost precision target cannot create a path to its hidden pose.");
        }
        [Test]
        public void CampaignUnownedCombatAndMinimapRemainAvailable()
        {
            using var w=new World(nameof(CampaignUnownedCombatAndMinimapRemainAvailable));var em=w.EntityManager;
            Entity source=em.CreateEntity(typeof(LocalTransform),typeof(UnitHealth),typeof(Faction));
            Entity target=em.CreateEntity(typeof(LocalTransform),typeof(UnitHealth),typeof(Faction));
            em.SetComponentData(source,LocalTransform.FromPosition(float3.zero));em.SetComponentData(target,LocalTransform.FromPosition(new float3(30,0,0)));
            em.SetComponentData(source,new Faction { Id=1 });em.SetComponentData(target,new Faction { Id=2 });
            em.SetComponentData(source,new UnitHealth { Current=100,Max=100 });em.SetComponentData(target,new UnitHealth { Current=100,Max=100 });
            Assert.IsTrue(CombatTargetPolicyUtility.Allows(em,source,target));
            Minimap(w,1);Assert.AreEqual(2,Markers(em).Length);
            Assert.IsTrue(SkirmishFogRenderVisibility.PlayerVisible(em,target));
        }
        [Test]
        public void MinimapFreezesPoseAndDropsPrecisionHandleBetweenRefreshes()
        {
            using var w=new World(nameof(MinimapFreezesPoseAndDropsPrecisionHandleBetweenRefreshes));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity target=Actor(em,"target",2,4,0);
            Step(w,1);Minimap(w,1);Assert.AreEqual(target,Hostile(em).SourceEntity);
            Move(em,target,100);em.SetComponentData(target,new UnitHealth { Current=7,Max=100 });
            Step(w,1.1);Minimap(w,1.1);var marker=Hostile(em);
            Assert.AreEqual(Entity.Null,marker.SourceEntity);Assert.AreEqual(new float3(4,0,0),marker.Position);
            Assert.AreEqual(1,marker.IsLastSeen);Assert.IsTrue(SkirmishFogService.TryGetKnowledge(em,target,1,out var seen));
            Assert.AreEqual(100,seen.LastSeenHealth);Assert.AreEqual(100,seen.LastSeenMax);
            Minimap(w,1.4);Assert.AreEqual(new float3(4,0,0),Hostile(em).Position);
        }
        [Test]
        public void DisabledLastSeenAndDestroyedContactPersistOnlyUntilExpiry()
        {
            using var w=new World(nameof(DisabledLastSeenAndDestroyedContactPersistOnlyUntilExpiry));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity target=Actor(em,"target",2,4,0);
            Step(w,1);Minimap(w,1);Move(em,target,100);em.AddComponent<Disabled>(target);em.AddComponent<UnitTransportPassenger>(target);
            Step(w,2);Minimap(w,2);Assert.AreEqual(new float3(4,0,0),Hostile(em).Position);
            Assert.AreEqual(Entity.Null,Hostile(em).SourceEntity);
            em.DestroyEntity(target);Step(w,3);Minimap(w,3);
            Assert.AreEqual(new float3(4,0,0),Hostile(em).Position);Assert.AreEqual(Entity.Null,Hostile(em).SourceEntity);
            Step(w,21);Minimap(w,21);Assert.AreEqual(0,HostileCount(em));
        }
        [Test]
        public void ObservedDeadContactDoesNotBecomeFrozenMarker()
        {
            using var w=new World(nameof(ObservedDeadContactDoesNotBecomeFrozenMarker));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity target=Actor(em,"target",2,4,0);
            Step(w,1);Minimap(w,1);em.SetComponentData(target,new UnitHealth { Current=0,Max=100 });
            UnitDeathSystem.FinalizeDeath(em,Entity.Null,target,1,0);
            Assert.IsFalse(em.Exists(target));Step(w,2);Minimap(w,2);Step(w,3);Minimap(w,3);Assert.AreEqual(0,HostileCount(em));
        }
        [Test]
        public void FinalMeshMaskPreservesOriginalFalseAndOtherOwners()
        {
            using var w=new World(nameof(FinalMeshMaskPreservesOriginalFalseAndOtherOwners));var em=w.EntityManager;
            Session(em);Entity player=Actor(em,"player",1,0,5),target=Actor(em,"hidden",2,100,0);
            Entity originallyOff=em.CreateEntity(typeof(MaterialMeshInfo));em.SetComponentEnabled<MaterialMeshInfo>(originallyOff,false);
            Entity originallyOn=em.CreateEntity(typeof(MaterialMeshInfo),typeof(Disabled),typeof(DisableRendering));
            var linked=em.AddBuffer<LinkedEntityGroup>(target);linked.Add(new LinkedEntityGroup { Value=target });linked.Add(new LinkedEntityGroup { Value=originallyOff });linked.Add(new LinkedEntityGroup { Value=originallyOn });
            Entity passengerMesh=em.CreateEntity(typeof(MaterialMeshInfo),typeof(DisableRendering));
            em.AddComponent<UnitTransportPassenger>(player);em.AddComponent<Disabled>(player);em.AddComponentData(player,new UnitModelInstanceReference { Instance=passengerMesh });
            Step(w,1);var system=w.GetOrCreateSystemManaged<SkirmishFogRenderMaskSystem>();system.Update();
            Assert.IsFalse(em.IsComponentEnabled<MaterialMeshInfo>(originallyOn));
            em.RemoveComponent<Disabled>(player);em.RemoveComponent<UnitTransportPassenger>(player);Move(em,target,4);Step(w,2);system.Update();
            Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(originallyOn));Assert.IsFalse(em.IsComponentEnabled<MaterialMeshInfo>(originallyOff));
            Assert.IsTrue(em.HasComponent<Disabled>(originallyOn));Assert.IsTrue(em.HasComponent<DisableRendering>(originallyOn));
            Assert.IsTrue(em.HasComponent<DisableRendering>(passengerMesh));
        }
        [Test]
        public void VisibleIncomingFlightDoesNotRevealHiddenLauncher()
        {
            using var w=new World(nameof(VisibleIncomingFlightDoesNotRevealHiddenLauncher));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity launcher=Actor(em,"launcher",2,100,0);em.AddComponent<MaterialMeshInfo>(launcher);
            Entity shot=em.CreateEntity(typeof(AirMissileProjectileComponent),typeof(MaterialMeshInfo),typeof(LocalTransform));
            em.SetComponentData(shot,new AirMissileProjectileComponent { Source=launcher,FactionId=2,Target=Entity.Null });
            em.SetComponentData(shot,LocalTransform.FromPosition(new float3(4,0,0)));Step(w,1);
            var system=w.GetOrCreateSystemManaged<SkirmishFogRenderMaskSystem>();system.Update();
            Assert.IsTrue(em.IsComponentEnabled<MaterialMeshInfo>(shot));Assert.IsFalse(em.IsComponentEnabled<MaterialMeshInfo>(launcher));
            em.SetComponentData(shot,LocalTransform.FromPosition(new float3(50,0,0)));system.Update();
            Assert.IsFalse(em.IsComponentEnabled<MaterialMeshInfo>(shot));Assert.IsFalse(em.IsComponentEnabled<MaterialMeshInfo>(launcher));
        }
        [Test]
        public void HiddenTargetHomingRetainsAcceptedDestination()
        {
            using var w=new World(nameof(HiddenTargetHomingRetainsAcceptedDestination));var em=w.EntityManager;
            Entity session=Session(em);Entity source=Actor(em,"source",1,0,5),target=Actor(em,"target",2,4,0);
            Step(w,1);em.AddComponentData(source,new SkirmishMoveIntentComponent { Active=1,Order=SkirmishGroupOrderKind.Attack,AttackTarget=target,DestinationX=4 });
            Move(em,target,100);Step(w,2);SkirmishWorldMovementService.Step(em,session,.1f,false);
            var intent=em.GetComponentData<SkirmishMoveIntentComponent>(source);Assert.AreEqual(Entity.Null,intent.AttackTarget);Assert.AreEqual(4,intent.DestinationX);
        }
        [Test]
        public void ActualPickingAndFocusedHudRejectUnknownAndLastSeen()
        {
            using var w=new World(nameof(ActualPickingAndFocusedHudRejectUnknownAndLastSeen));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity target=Actor(em,"pick-target",2,100,0);
            em.AddComponentData(target,new UnitMove { Speed=3 });em.AddComponentData(target,new UnitFootprint { Size=new int2(1,1) });
            em.AddComponentData(target,new LocalToWorld { Value=float4x4.Translate(new float3(100,0,0)) });
            var go=new GameObject("fog-picking-actual-helper-camera");
            try
            {
                var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=10;camera.pixelRect=new Rect(0,0,800,600);
                go.transform.rotation=Quaternion.Euler(90,0,0);go.transform.position=new Vector3(100,20,0);
                var lookup=new FocusableUnitLookupCameraSystemHelper();Step(w,1);
                Vector3 center=camera.WorldToScreenPoint(new Vector3(100,0,0));
                Assert.IsFalse(lookup.TryGetClickedUnitEntityByScreenDistance(em,camera,new Vector2(center.x,center.y),1,out _));
                Move(em,target,4);em.SetComponentData(target,new LocalToWorld { Value=float4x4.Translate(new float3(4,0,0)) });
                go.transform.position=new Vector3(4,20,0);Step(w,2);center=camera.WorldToScreenPoint(new Vector3(4,0,0));
                Assert.IsTrue(lookup.TryGetClickedUnitEntityByScreenDistance(em,camera,new Vector2(center.x,center.y),1,out var picked));Assert.AreEqual(target,picked);
                var selection=new SelectionStateCompositionSystemHelper();selection.SetFocusedUnit(target);
                var focused=new FocusedUnitLifecycleCompositionSystemHelper();Assert.IsTrue(focused.TryGetFocusedUnitEntity(em,selection,out _));
                Move(em,target,100);Step(w,3);Assert.IsFalse(lookup.TryGetClickedUnitEntityByScreenDistance(em,camera,new Vector2(center.x,center.y),1,out _));
                int hudReads=0;focused.RefreshFocusedUnit(em,selection,(manager,entity)=>hudReads++);
                Assert.AreEqual(Entity.Null,selection.FocusedUnit);Assert.AreEqual(0,hudReads);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test]
        public void EnemyApproachesPublicObjectiveThenAttacksObservedBase()
        {
            using var w=new World(nameof(EnemyApproachesPublicObjectiveThenAttacksObservedBase));var em=w.EntityManager;
            Entity session=Session(em);Entity enemy=Actor(em,"enemy",2,100,5),playerBase=Actor(em,"base",1,4,0);
            em.AddComponentData(playerBase,new SkirmishObjectiveRoleComponent { Role=SkirmishObjectiveRoleKind.PlayerBase });
            var baseOwner=em.GetComponentData<SkirmishAttemptOwnedComponent>(playerBase);baseOwner.IsStructure=1;em.SetComponentData(playerBase,baseOwner);
            em.AddComponentData(enemy,new UnitMove { Speed=3 });em.AddComponentData(enemy,new SkirmishArmyGroupMembershipComponent { GroupId=7,Domain=SkirmishPopulationCategory.Infantry });
            em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord { GroupId=7,FactionId=2,Role=SkirmishRoleKind.Tank,Domain=SkirmishPopulationCategory.Ground,AliveCount=1,MemberCount=1 });
            em.AddComponentData(session,new SkirmishEnemyStockComponent { Materials=0 });
            em.AddComponentData(session,new SkirmishEnemyCapacityComponent { SupplyLive=4,SupplyCap=4 });
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord { Setup=new SkirmishResolvedSetup { MeasuredLayoutBound=true,PlayerBaseWorldX=4,PlayerBaseWorldZ=0,EnemyBaseWorldX=100,ArmyProfileId=SkirmishArmyProfileId.GroundManeuver,EnemyFaction=2 } });
            Step(w,1);using var owned=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            var army=SkirmishExpansionCatalogFactory.CreateInMemory().ArmyGround;
            var score=SkirmishEnemyStrategySystem.Evaluate(em,session,owned,army);Assert.AreEqual(SkirmishStrategyPriority.AttackBase,score.Priority);
            Assert.AreEqual(SkirmishGroupOrderKind.Move,em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
            Assert.AreEqual(new int2(4,0),em.GetComponentData<UnitPathRequest>(enemy).Goal);Assert.IsFalse(em.HasComponent<EngageTarget>(enemy));
            Assert.AreEqual(Entity.Null,em.GetComponentData<SkirmishMoveIntentComponent>(enemy).AttackTarget);
            Assert.IsFalse(SkirmishArmyCommandService.TryIssueGroupOrder(em,session,7,2,SkirmishGroupOrderKind.Attack,playerBase,out var denied));Assert.AreEqual(SkirmishReasonCode.HiddenContact,denied.Reason);
            Move(em,enemy,8);Step(w,2);SkirmishEnemyStrategySystem.Evaluate(em,session,owned,army);
            Assert.AreEqual(SkirmishGroupOrderKind.Attack,em.GetBuffer<SkirmishArmyGroupRecord>(session)[0].LastOrder);
            Assert.AreEqual(playerBase,em.GetComponentData<SkirmishMoveIntentComponent>(enemy).AttackTarget);
        }
        private static Entity Session(EntityManager em)
        {
            Entity grid=em.CreateEntity(typeof(GridConfig));em.SetComponentData(grid,new GridConfig { Width=200,Height=100,CellSize=1 });
            Entity session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishFogStateComponent),typeof(SkirmishObjectiveClockComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent { SessionId=new FixedString64Bytes(Attempt),Phase=SkirmishSessionPhase.Playing });
            em.SetComponentData(session,new SkirmishFogStateComponent { SharedFog=1,LastSeenExpireSeconds=20 });return session;
        }
        private static Entity Actor(EntityManager em,string id,byte faction,float x,int radius)
        {
            Entity actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitGrid),typeof(LocalTransform),typeof(UnitHealth),typeof(UnitCombat));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent { SessionId=new FixedString64Bytes(Attempt),StableObjectId=new FixedString64Bytes(id),FactionId=faction });
            em.SetComponentData(actor,new Faction { Id=faction });em.SetComponentData(actor,new UnitHealth { Current=100,Max=100 });
            em.SetComponentData(actor,new UnitCombat { AggroRangeCells=radius,CanAttack=1 });Move(em,actor,x);return actor;
        }
        private static void Move(EntityManager em,Entity actor,float x) { em.SetComponentData(actor,LocalTransform.FromPosition(new float3(x,0,0)));em.SetComponentData(actor,new UnitGrid { Cell=new int2((int)x,0) }); }
        private static void Step(World w,double time) { w.SetTime(new TimeData(time,.1f));w.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(w.Unmanaged);w.EntityManager.CompleteAllTrackedJobs(); }
        private static void Minimap(World w,double time) { w.SetTime(new TimeData(time,.1f));w.GetOrCreateSystem<MatchHudMinimapMarkerSystem>().Update(w.Unmanaged);w.EntityManager.CompleteAllTrackedJobs(); }
        private static DynamicBuffer<MatchHudMinimapMarkerElement> Markers(EntityManager em) { using var q=em.CreateEntityQuery(typeof(MatchHudMinimapMarkerStateComponent),typeof(MatchHudMinimapMarkerElement));return em.GetBuffer<MatchHudMinimapMarkerElement>(q.GetSingletonEntity()); }
        private static int HostileCount(EntityManager em) { int n=0;foreach(var marker in Markers(em))if(marker.FactionId==2)n++;return n; }
        private static MatchHudMinimapMarkerElement Hostile(EntityManager em) { foreach(var marker in Markers(em))if(marker.FactionId==2)return marker;Assert.Fail("Expected frozen or visible hostile marker.");return default; }
    }
}
