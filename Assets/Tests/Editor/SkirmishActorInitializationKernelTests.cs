#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishActorInitializationKernelTests
    {
        [Serializable] public sealed class ActorCase
        {
            public string id, kind="actor_initialization";
            public SkirmishActorInitializationState state;
            public SkirmishActorOverlayValues overlay;
            public float shot_interval;
        }
        [Serializable] public sealed class ActorResult
        { public string id, kind="actor_initialization"; public SkirmishActorInitializationState state; }
        [Serializable] public sealed class ActorTrace
        { public int schema_version=1; public string scope="Native EntityManager fixture actors; not authored baked prefab or live mission capture"; public ActorCase[] cases; public ActorResult[] results; }

        public static int RunChecks()
        {
            var suite=new SkirmishActorInitializationKernelTests();
            suite.OriginalSourcePinned();suite.NativeOriginalAndSharedComponentsAgree();
            suite.HealthAndRosterApplyOnce();suite.BurstMatchesNativeOverlayProjection();
            Debug.Log("[SkirmishActorInitializationParity] result=Passed tests=4 native=Passed original=Passed burst=Passed missionReady=0");
            return 4;
        }
        [Test] public void OriginalSourcePinned()
        {
            using var sha=SHA256.Create();
            string text=File.ReadAllText("Assets/Tests/Editor/SkirmishSharedCombatBindingOriginal.cs.txt").Replace("\r\n","\n");
            Assert.AreEqual("f43c767909f8d120aa49e9963faad78a5f159892ad8b2fd7c4fa832b8701cb9a",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant());
            Assert.AreEqual((byte)CombatTargetDomain.Structure,4);Assert.AreEqual((byte)CombatTargetDomain.Air,8);
            Assert.AreEqual((byte)CombatTargetDomain.Infantry,1);Assert.AreEqual((byte)CombatTargetDomain.Ground,2);
            Assert.AreEqual((byte)SkirmishPopulationCategory.Infantry,1);Assert.AreEqual((byte)SkirmishPopulationCategory.Air,3);
        }
        static Entity Actor(EntityManager em,int flags,byte category,float minimum)
        {
            var e=em.CreateEntity();
            if(category!=0)em.AddComponentData(e,new SkirmishUnitRoleComponent {Category=(SkirmishPopulationCategory)category,Role=SkirmishRoleKind.Rifle});
            if((flags&32)!=0)em.AddComponent<SkirmishSharedActorTag>(e);
            if((flags&64)!=0)em.AddComponent<SkirmishStructureIdentityComponent>(e);
            if((flags&128)!=0)em.AddComponentData(e,new UnitHealth {Current=17,Max=99});
            em.AddComponent<CampaignMissionCombatSuppressedTag>(e);
            if((flags&1)!=0)em.AddComponentData(e,new UnitAttack {Damage=99,Range=3,CooldownSeconds=.25f,TraceWidth=77,TracerEveryNthShot=11});
            if((flags&2)!=0)em.AddComponentData(e,new BuildingDefenseWeapon {Damage=98,Range=4,CooldownSeconds=.5f,MaxConcurrentAttacks=3,TraceWidth=78});
            if((flags&4)!=0)em.AddComponentData(e,new UnitCombat {CanAttack=3,AutoEngage=4,AggroRangeCells=29,ChaseBreakDistance=37.5f});
            if((flags&8)!=0)em.AddComponentData(e,new AirMissileLauncherComponent {AirTargetDamage=97,BaseDetectionRange=100,MaxDetectionRange=120,MinRange=minimum,MaxSupportRangeBonus=3,IncomingMissileDamage=7,MissileSpeed=83});
            if((flags&16)!=0)em.AddComponentData(e,new GroundMissileLauncherComponent {Damage=96,MaxRange=160,MinRange=minimum,DamageRadius=5,PrepareSeconds=.3f});
            return e;
        }
        static void Equal(SkirmishActorInitializationState a,SkirmishActorInitializationState b)
        {
            foreach(var f in typeof(SkirmishActorInitializationState).GetFields(BindingFlags.Instance|BindingFlags.Public))
            {
                if(f.FieldType==typeof(float))Assert.AreEqual(math.asint((float)f.GetValue(a)),math.asint((float)f.GetValue(b)),f.Name);
                else Assert.AreEqual(f.GetValue(a),f.GetValue(b),f.Name);
            }
        }
        static void Preserved(EntityManager em,Entity e)
        {
            if(em.HasComponent<UnitAttack>(e)){var v=em.GetComponentData<UnitAttack>(e);Assert.AreEqual(77,v.TraceWidth);Assert.AreEqual(11,v.TracerEveryNthShot);}
            if(em.HasComponent<BuildingDefenseWeapon>(e)){var v=em.GetComponentData<BuildingDefenseWeapon>(e);Assert.AreEqual(78,v.TraceWidth);Assert.AreEqual(3,v.MaxConcurrentAttacks);}
            if(em.HasComponent<UnitCombat>(e)){var v=em.GetComponentData<UnitCombat>(e);Assert.AreEqual(29,v.AggroRangeCells);Assert.AreEqual(37.5f,v.ChaseBreakDistance);}
            if(em.HasComponent<AirMissileLauncherComponent>(e)){var v=em.GetComponentData<AirMissileLauncherComponent>(e);Assert.AreEqual(7,v.IncomingMissileDamage);Assert.AreEqual(83,v.MissileSpeed);}
            if(em.HasComponent<GroundMissileLauncherComponent>(e)){var v=em.GetComponentData<GroundMissileLauncherComponent>(e);Assert.AreEqual(5,v.DamageRadius);Assert.AreEqual(.3f,v.PrepareSeconds);}
        }
        [Test] public void NativeOriginalAndSharedComponentsAgree()
        {
            using var world=new World("actor overlay original parity");var em=world.EntityManager;
            float[] ranges={-4f,-0f,0f,.1f,7f,1000f,float.PositiveInfinity,float.NaN};
            for(int flags=0;flags<256;flags++)for(byte category=0;category<8;category++)
            {
                float range=ranges[(flags+category)%ranges.Length];float minimum=ranges[(flags*3+category+2)%ranges.Length];
                var overlay=new SkirmishRoleOverlay {MaxHealth=13,Damage=(flags%3)-1,RangeWorld=range,TargetDomains=(SkirmishTargetDomain)(flags%16)};
                var current=Actor(em,flags,category,minimum);var original=Actor(em,flags,category,minimum);
                var pure=SkirmishSharedCombatBinding.CaptureState(em,current);
                SkirmishActorInitializationKernel.BindWeapons(ref pure,SkirmishSharedCombatBinding.OverlayValues(overlay),SkirmishExpandedEngagementService.ShotIntervalSeconds);
                SkirmishSharedCombatBindingOriginal.Bind(em,original,overlay);
                SkirmishSharedCombatBinding.Bind(em,current,overlay);
                Equal(SkirmishSharedCombatBinding.CaptureState(em,original),SkirmishSharedCombatBinding.CaptureState(em,current));
                Equal(pure,SkirmishSharedCombatBinding.CaptureState(em,current));Preserved(em,current);Preserved(em,original);
                em.DestroyEntity(current);em.DestroyEntity(original);
            }
        }
        [Test] public void HealthAndRosterApplyOnce()
        {
            using var world=new World("actor roster once");var em=world.EntityManager;
            var actor=Actor(em,255,1,7);var session=new FixedString64Bytes("fixture.session");
            em.AddComponentData(actor,new SkirmishAttemptOwnedComponent {SessionId=session,StableObjectId=new FixedString64Bytes("fixture.actor")});
            var overlay=new SkirmishRoleOverlay {RoleKind=SkirmishRoleKind.Rifle,MaxHealth=73,Damage=9,RangeWorld=13,TargetDomains=SkirmishTargetDomain.Ground};
            var setup=new SkirmishResolvedSetup {RoleOverlays=new[]{overlay}};
            using var owned=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            Assert.AreEqual(1,SkirmishRosterProjectionSystem.Apply(em,owned,session,setup));
            Assert.AreEqual(73,em.GetComponentData<UnitHealth>(actor).Current);
            Assert.AreEqual(1,em.GetComponentData<SkirmishRoleOverlayComponent>(actor).Applied);
            Assert.IsFalse(em.HasComponent<CampaignMissionCombatSuppressedTag>(actor));
            var hp=em.GetComponentData<UnitHealth>(actor);hp.Current=5;em.SetComponentData(actor,hp);
            Assert.AreEqual(0,SkirmishRosterProjectionSystem.Apply(em,owned,session,setup));
            Assert.AreEqual(5,em.GetComponentData<UnitHealth>(actor).Current);
            var absent=Actor(em,0,0,1);SkirmishRosterProjectionSystem.WriteOverlay(em,absent,overlay);
            Assert.AreEqual(73,em.GetComponentData<UnitHealth>(absent).Max);Assert.IsFalse(em.HasComponent<UnitAttack>(absent));
            Assert.IsTrue(em.HasComponent<CampaignMissionCombatSuppressedTag>(absent));
        }
        struct Pair {public SkirmishActorInitializationState State;public SkirmishActorOverlayValues Overlay;}
        [BurstCompile(CompileSynchronously=true)] struct ProjectionJob:IJob
        {
            [ReadOnly] public NativeArray<Pair> Inputs;public NativeArray<SkirmishActorInitializationState> Outputs;public NativeArray<byte> Managed;
            [BurstDiscard] static void MarkManaged(ref byte value){value=1;}
            public void Execute(){byte managed=0;MarkManaged(ref managed);Managed[0]=managed;for(int i=0;i<Inputs.Length;i++){var p=Inputs[i];var s=p.State;SkirmishActorInitializationKernel.ApplyHealth(ref s,p.Overlay);SkirmishActorInitializationKernel.BindWeapons(ref s,p.Overlay,1f);Outputs[i]=s;}}
        }
        [Test] public void BurstMatchesNativeOverlayProjection()
        {
            using var world=new World("actor Burst fixture");var em=world.EntityManager;
            using var inputsOwner=new NativeArray<Pair>(256,Allocator.TempJob);var inputs=inputsOwner;
            using var expectedOwner=new NativeArray<SkirmishActorInitializationState>(256,Allocator.TempJob);var expected=expectedOwner;
            using var outputs=new NativeArray<SkirmishActorInitializationState>(256,Allocator.TempJob);using var managed=new NativeArray<byte>(1,Allocator.TempJob);
            for(int i=0;i<256;i++)
            {
                var actor=Actor(em,i,(byte)(i%8),(i%9)*.25f);
                var overlay=new SkirmishRoleOverlay {MaxHealth=i-1,Damage=i%17-1,RangeWorld=(i%11)*.1f,TargetDomains=(SkirmishTargetDomain)(i%16)};
                inputs[i]=new Pair {State=SkirmishSharedCombatBinding.CaptureState(em,actor),Overlay=SkirmishSharedCombatBinding.OverlayValues(overlay)};
                SkirmishRosterProjectionSystem.WriteOverlay(em,actor,overlay);expected[i]=SkirmishSharedCombatBinding.CaptureState(em,actor);em.DestroyEntity(actor);
            }
            var job=new ProjectionJob {Inputs=inputs,Outputs=outputs,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);
            for(int i=0;i<256;i++)Equal(expected[i],outputs[i]);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst execution required");
            for(int i=0;i<256;i++)Equal(expected[i],outputs[i]);
        }
        public static void EmitNativeTrace()
        {
            using var world=new World("actor initialization trace fixture");var em=world.EntityManager;
            var trace=new ActorTrace {cases=new ActorCase[128],results=new ActorResult[128]};
            for(int i=0;i<128;i++)
            {
                var actor=Actor(em,i*2+1,(byte)(i%8),(i%9)*.25f);
                var overlay=new SkirmishRoleOverlay {MaxHealth=100+i,Damage=i%17,RangeWorld=2+(i%11)*.1f,TargetDomains=(SkirmishTargetDomain)(i%16)};
                string id="actor-"+i;
                trace.cases[i]=new ActorCase {id=id,state=SkirmishSharedCombatBinding.CaptureState(em,actor),overlay=SkirmishSharedCombatBinding.OverlayValues(overlay),shot_interval=SkirmishExpandedEngagementService.ShotIntervalSeconds};
                SkirmishRosterProjectionSystem.WriteOverlay(em,actor,overlay);
                trace.results[i]=new ActorResult {id=id,state=SkirmishSharedCombatBinding.CaptureState(em,actor)};em.DestroyEntity(actor);
            }
            Debug.Log("[SpawnInitializationNativeTrace] "+JsonUtility.ToJson(trace));
        }
    }
}
#endif
