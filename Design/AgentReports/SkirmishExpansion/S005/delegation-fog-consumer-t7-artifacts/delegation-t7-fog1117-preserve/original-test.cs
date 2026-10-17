using System;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SkirmishFogVfxTests
    {
        private const string Attempt="fog.vfx.actual";
        public static void RunFocusedValidation()
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            var t=new SkirmishFogVfxTests();
            t.ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed(1,false);
            t.ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed(1,true);
            t.ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed(2,false);
            t.ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed(2,true);
            t.ActualUnitPlaybackBothEndpointsDestroyedUsesPresentedPoint(false);
            t.ActualUnitPlaybackBothEndpointsDestroyedUsesPresentedPoint(true);
            t.CampaignMissingEndpointsRetainActualPlaybackContract();
            t.UnitAttackWithoutCombatCannotInventObservationRadius();
            Debug.Log("[SkirmishFogVfxTests] result=Passed cases=8 scope=IsolatedActualRequestPlaybackNotNormalMission");
        }
        [TestCase((byte)1,false)] [TestCase((byte)1,true)] [TestCase((byte)2,false)] [TestCase((byte)2,true)]
        public void ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed(byte kind,bool visiblePoint)
        {
            using var w=new World(nameof(ActualMissileImpactRequestUsesPresentedPointAfterTargetDestroyed));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity source=Actor(em,"source",2,100,0),target=Actor(em,"target",1,90,0);
            Step(w);em.DestroyEntity(target);
            var prefab=new GameObject("fog-impact-request-fixture");
            try
            {
                float3 point=new float3(visiblePoint?4:50,0,0);Entity request=em.CreateEntity();
                if(kind==1)
                {
                    em.AddComponentData(source,new GroundMissileLauncherVfxReferenceComponent { ImpactExplosionPrefab=prefab });
                    em.AddComponentData(request,new GroundMissileImpactRequestComponent { Source=source,TargetEntity=target,Position=point,FactionId=2,Damage=0 });
                    w.GetOrCreateSystem<GroundMissileImpactSystem>().Update(w.Unmanaged);
                }
                else
                {
                    em.AddComponentData(source,new AirMissileLauncherVfxReferenceComponent { AirTargetImpactPrefab=prefab });
                    em.AddComponentData(request,new AirMissileImpactRequestComponent { Source=source,Target=target,Position=point,FactionId=2,TargetKind=(byte)AirMissileTargetKind.EnemyAirUnit,Damage=0 });
                    w.GetOrCreateSystem<AirMissileImpactSystem>().Update(w.Unmanaged);
                }
                em.CompleteAllTrackedJobs();using var produced=em.CreateEntityQuery(typeof(CombatGameObjectVfxRequest));
                Assert.AreEqual(visiblePoint?1:0,produced.CalculateEntityCount());Assert.IsFalse(em.Exists(request));
                if(visiblePoint)Assert.AreEqual(point,produced.GetSingleton<CombatGameObjectVfxRequest>().Position);
            }
            finally { UnityEngine.Object.DestroyImmediate(prefab); }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualUnitPlaybackBothEndpointsDestroyedUsesPresentedPoint(bool visiblePoint)
        {
            using var w=new World(nameof(ActualUnitPlaybackBothEndpointsDestroyedUsesPresentedPoint));var em=w.EntityManager;
            Session(em);Actor(em,"player",1,0,5);Entity source=Actor(em,"source",2,100,0),target=Actor(em,"target",1,90,0);
            Step(w);em.DestroyEntity(source);em.DestroyEntity(target);
            Playback(w,source,target,new float3(visiblePoint?4:50,0,0),visiblePoint);
        }
        [Test]
        public void CampaignMissingEndpointsRetainActualPlaybackContract()
        {
            using var w=new World(nameof(CampaignMissingEndpointsRetainActualPlaybackContract));
            w.SetTime(new TimeData(1,.1f));Playback(w,Entity.Null,Entity.Null,new float3(50,0,0),true);
        }
        [Test]
        public void UnitAttackWithoutCombatCannotInventObservationRadius()
        {
            using var w=new World(nameof(UnitAttackWithoutCombatCannotInventObservationRadius));var em=w.EntityManager;
            Session(em);Entity player=Actor(em,"player",1,0,0),source=Actor(em,"source",2,100,0);
            em.RemoveComponent<UnitCombat>(player);em.AddComponentData(player,new UnitAttack { Range=100 });
            Step(w);Assert.IsFalse(SkirmishFogObservedPointUtility.IsObserved(em,source,new float3(50,0,0),1));
        }
        private static void Playback(World w,Entity source,Entity target,float3 point,bool expected)
        {
            UnitAttackImpactVfxView.ReleaseAll();string name="fog-vfx-actual-"+Guid.NewGuid().ToString("N");var prefab=new GameObject(name);prefab.SetActive(false);
            try
            {
                var em=w.EntityManager;Entity request=em.CreateEntity(typeof(UnitAttackVfxRequest));
                em.SetComponentData(request,new UnitAttackVfxRequest { Kind=(byte)UnitAttackVfxRequestKind.Impact,Source=source,Target=target,Prefab=prefab,PlaybackPosition=point,PlaybackRotation=quaternion.identity });
                w.GetOrCreateSystemManaged<UnitAttackVfxRequestSystem>().Update();em.CompleteAllTrackedJobs();
                var shown=Resources.FindObjectsOfTypeAll<GameObject>().Where(go=>go.name==name+"(Clone)"&&go.activeSelf).ToArray();
                Assert.AreEqual(expected?1:0,shown.Length);if(expected)Assert.AreEqual((Vector3)point,shown[0].transform.position);
                Assert.IsFalse(em.Exists(request));
            }
            finally { UnitAttackImpactVfxView.ReleaseAll();UnityEngine.Object.DestroyImmediate(prefab); }
        }
        private static void Session(EntityManager em)
        {
            Entity grid=em.CreateEntity(typeof(GridConfig));em.SetComponentData(grid,new GridConfig { Width=200,Height=100,CellSize=1 });
            Entity session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishFogStateComponent),typeof(SkirmishObjectiveClockComponent));
            em.SetComponentData(session,new SkirmishExpandedSessionComponent { SessionId=new FixedString64Bytes(Attempt),Phase=SkirmishSessionPhase.Playing });
            em.SetComponentData(session,new SkirmishFogStateComponent { SharedFog=1,LastSeenExpireSeconds=20 });
        }
        private static Entity Actor(EntityManager em,string id,byte faction,float x,int radius)
        {
            Entity actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(Faction),typeof(UnitGrid),typeof(LocalTransform),typeof(UnitHealth),typeof(UnitCombat));
            em.SetComponentData(actor,new SkirmishAttemptOwnedComponent { SessionId=new FixedString64Bytes(Attempt),StableObjectId=new FixedString64Bytes(id),FactionId=faction });
            em.SetComponentData(actor,new Faction { Id=faction });em.SetComponentData(actor,new UnitHealth { Current=100,Max=100 });
            em.SetComponentData(actor,new UnitCombat { AggroRangeCells=radius,CanAttack=1 });em.SetComponentData(actor,LocalTransform.FromPosition(new float3(x,0,0)));em.SetComponentData(actor,new UnitGrid { Cell=new int2((int)x,0) });return actor;
        }
        private static void Step(World w) { w.SetTime(new TimeData(1,.1f));w.GetOrCreateSystem<SkirmishSharedFogUpdateSystem>().Update(w.Unmanaged);w.EntityManager.CompleteAllTrackedJobs(); }
    }
}
