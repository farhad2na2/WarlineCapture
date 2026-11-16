using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class PathRetryRestorationKernelTests
    {
        public static int RunChecks()
        {
            var t=new PathRetryRestorationKernelTests();t.OriginalSourcePinned();t.OriginalPolicyManagedAndBurst();t.NativeMembershipMatrix();t.NativeDeferredCooldownAndMembershipBoundaries();
            Debug.Log("[PathRetryRestorationParity] result=Passed checks=4 policyCases=7680 nativeCases=1536 burst=Passed missionReady=0");return 4;
        }
        [Test]public void OriginalSourcePinned()
        {
            using var sha=SHA256.Create();var text=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/PathRetryRestorationOriginalSource.cs.txt")).Replace("\r\n","\n");
            Assert.AreEqual("40b8ba7b49e56ef17adc6eabb535b6f4ec5e8b222cdd9280324ade0d65f43077",BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant());
        }
        // Independent transcription of the four saved job queries and their commands.
        // Query membership is evaluated before the common local ECB playback.
        private static PathRetryRestorationPlan Original(PathRetryRestorationFacts f,int frame)
        {
            var p=new PathRetryRestorationPlan();
            if(f.HasCooldown&&f.ResumeFrame<=frame)p.ClearCooldown=true;
            if(f.HasGroupMember&&!f.HasManual)p.RemoveStaleGroupMember=true;
            bool manualQuery=f.HasManual&&f.HasTarget&&!f.HasLongDistance&&!f.HasCooldown&&!f.HasRequest&&!f.HasFollow&&!f.HasEngage&&!f.HasAir;
            if(manualQuery){p.RestoreRequest=true;p.Goal=f.Target;}
            bool longQuery=f.HasLongDistance&&!f.HasCooldown&&!f.HasRequest&&!f.HasFollow&&!f.HasEngage&&!f.HasAir;
            if(longQuery)
            {
                p.TargetAction=f.HasTarget?RetryTargetAction.Set:RetryTargetAction.Add;
                if(f.LongDistanceManual!=0&&!f.HasManual)p.ManualAction=RetryManualAction.Add;
                else if(f.LongDistanceManual==0&&f.HasManual)p.ManualAction=RetryManualAction.Remove;
                p.RestoreRequest=true;p.Goal=f.FinalGoal;
            }
            return p;
        }
        private static PathRetryRestorationFacts Facts(int bits,byte manual,int resume)=>new PathRetryRestorationFacts
        {
            HasCooldown=(bits&1)!=0,HasManual=(bits&2)!=0,HasGroupMember=(bits&4)!=0,
            HasLongDistance=(bits&8)!=0,HasTarget=(bits&16)!=0,HasRequest=(bits&32)!=0,
            HasFollow=(bits&64)!=0,HasEngage=(bits&128)!=0,HasAir=(bits&256)!=0,
            LongDistanceManual=manual,ResumeFrame=resume,Target=new PlacementCell(-7,12),FinalGoal=new PlacementCell(91,-43)
        };
        private static void Equal(PathRetryRestorationPlan a,PathRetryRestorationPlan b)
        {
            Assert.AreEqual(a.ClearCooldown,b.ClearCooldown);Assert.AreEqual(a.RemoveStaleGroupMember,b.RemoveStaleGroupMember);Assert.AreEqual(a.RestoreRequest,b.RestoreRequest);
            Assert.AreEqual(a.TargetAction,b.TargetAction);Assert.AreEqual(a.ManualAction,b.ManualAction);Assert.AreEqual(a.Goal.x,b.Goal.x);Assert.AreEqual(a.Goal.y,b.Goal.y);
        }
        [BurstCompile(CompileSynchronously=true)]private struct PolicyJob:IJob
        {
            [ReadOnly]public NativeArray<PathRetryRestorationFacts> Inputs;
            public NativeArray<PathRetryRestorationPlan> OriginalResults,SharedResults;
            public NativeArray<int> Managed;
            [BurstDiscard]private static void MarkManaged(ref int value){value=1;}
            public void Execute()
            {
                int flag=0;MarkManaged(ref flag);Managed[0]=flag;
                for(int i=0;i<Inputs.Length;i++){var f=Inputs[i];OriginalResults[i]=Original(f,20);SharedResults[i]=PathRetryRestorationKernel.Plan(f,20);}
            }
        }
        [Test]public void OriginalPolicyManagedAndBurst()
        {
            const int count=512*3*5;using var owner=new NativeArray<PathRetryRestorationFacts>(count,Allocator.TempJob);var inputs=owner;
            using var expected=new NativeArray<PathRetryRestorationPlan>(count,Allocator.TempJob);using var actual=new NativeArray<PathRetryRestorationPlan>(count,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);
            int at=0;foreach(int resume in new[]{int.MinValue,19,20,21,int.MaxValue})foreach(byte manual in new byte[]{0,1,255})for(int bits=0;bits<512;bits++)inputs[at++]=Facts(bits,manual,resume);
            var job=new PolicyJob{Inputs=inputs,OriginalResults=expected,SharedResults=actual,Managed=managed};
            job.Execute();Assert.AreEqual(1,managed[0]);for(int i=0;i<count;i++)Equal(expected[i],actual[i]);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst required");for(int i=0;i<count;i++)Equal(expected[i],actual[i]);
        }
        private static Entity Create(EntityManager em,PathRetryRestorationFacts f)
        {
            var e=em.CreateEntity();
            if(f.HasCooldown)em.AddComponentData(e,new UnitPathRetryCooldown{ResumeFrame=f.ResumeFrame});
            if(f.HasManual)em.AddComponent<ManualMoveOrderTag>(e);
            if(f.HasGroupMember)em.AddComponent<ManualMoveGroupMemberTag>(e);
            if(f.HasLongDistance)em.AddComponentData(e,new UnitLongDistanceMove{FinalGoal=new int2(f.FinalGoal.x,f.FinalGoal.y),ManualMove=f.LongDistanceManual});
            if(f.HasTarget)em.AddComponentData(e,new UnitTarget{Cell=new int2(f.Target.x,f.Target.y)});
            if(f.HasRequest)em.AddComponentData(e,new UnitPathRequest{Goal=new int2(-101,-102)});
            if(f.HasFollow)em.AddComponent<UnitPathFollow>(e);
            if(f.HasEngage)em.AddComponent<EngageTarget>(e);
            if(f.HasAir)em.AddComponent<UnitAirMovement>(e);
            return e;
        }
        private static void VerifyEntity(EntityManager em,Entity e,PathRetryRestorationFacts f,PathRetryRestorationPlan plan)
        {
            Assert.IsFalse(em.HasComponent<UnitGrid>(e),"Retry jobs must not acquire an invented UnitGrid requirement");
            Assert.AreEqual(f.HasCooldown&&!plan.ClearCooldown,em.HasComponent<UnitPathRetryCooldown>(e));
            Assert.AreEqual(f.HasGroupMember&&!plan.RemoveStaleGroupMember,em.HasComponent<ManualMoveGroupMemberTag>(e));
            Assert.AreEqual(plan.ManualAction==RetryManualAction.Add||(f.HasManual&&plan.ManualAction!=RetryManualAction.Remove),em.HasComponent<ManualMoveOrderTag>(e));
            Assert.AreEqual(f.HasTarget||plan.TargetAction!=RetryTargetAction.None,em.HasComponent<UnitTarget>(e));
            if(em.HasComponent<UnitTarget>(e)){var expected=plan.TargetAction==RetryTargetAction.None?f.Target:plan.Goal;Assert.AreEqual(new int2(expected.x,expected.y),em.GetComponentData<UnitTarget>(e).Cell);}
            Assert.AreEqual(f.HasRequest||plan.RestoreRequest,em.HasComponent<UnitPathRequest>(e));
            if(em.HasComponent<UnitPathRequest>(e))Assert.AreEqual(plan.RestoreRequest?new int2(plan.Goal.x,plan.Goal.y):new int2(-101,-102),em.GetComponentData<UnitPathRequest>(e).Goal);
            Assert.AreEqual(f.HasLongDistance,em.HasComponent<UnitLongDistanceMove>(e));Assert.AreEqual(f.HasFollow,em.HasComponent<UnitPathFollow>(e));Assert.AreEqual(f.HasEngage,em.HasComponent<EngageTarget>(e));Assert.AreEqual(f.HasAir,em.HasComponent<UnitAirMovement>(e));
        }
        [Test]public void NativeMembershipMatrix()
        {
            using var world=new World("RetryNativeMembershipMatrix");var em=world.EntityManager;em.CreateEntity(typeof(GridConfig));
            var entities=new Entity[1536];var facts=new PathRetryRestorationFacts[entities.Length];int at=0;
            foreach(byte manual in new byte[]{0,1,255})for(int bits=0;bits<512;bits++){facts[at]=Facts(bits,manual,int.MinValue);entities[at]=Create(em,facts[at]);at++;}
            var system=world.CreateSystem<UnitManualMoveRetrySystem>();system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
            for(int i=0;i<entities.Length;i++)VerifyEntity(em,entities[i],facts[i],Original(facts[i],Time.frameCount));
        }
        [Test]public void NativeDeferredCooldownAndMembershipBoundaries()
        {
            using var world=new World("RetryDeferredCooldownBoundaries");var em=world.EntityManager;em.CreateEntity(typeof(GridConfig));
            var expired=Facts(1|2|16,0,int.MinValue);var a=Create(em,expired);
            var future=Facts(1|2|16,0,int.MaxValue);var b=Create(em,future);
            var stale=Facts(4|8,1,0);var c=Create(em,stale);
            var removeManual=Facts(2|4|8,0,0);var d=Create(em,removeManual);
            var system=world.CreateSystem<UnitManualMoveRetrySystem>();system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
            VerifyEntity(em,a,expired,Original(expired,Time.frameCount));Assert.IsFalse(em.HasComponent<UnitPathRequest>(a),"Same-update cooldown removal must not make restoration query eligible");
            VerifyEntity(em,b,future,Original(future,Time.frameCount));VerifyEntity(em,c,stale,Original(stale,Time.frameCount));VerifyEntity(em,d,removeManual,Original(removeManual,Time.frameCount));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(c));Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(c),"Stale cleanup sees old membership before long-distance adds manual");
            Assert.IsTrue(em.HasComponent<ManualMoveGroupMemberTag>(d),"Manual removal does not trigger stale cleanup in same update");
            system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(a));Assert.AreEqual(new int2(expired.Target.x,expired.Target.y),em.GetComponentData<UnitPathRequest>(a).Goal);
            Assert.IsTrue(em.HasComponent<UnitPathRetryCooldown>(b));Assert.IsFalse(em.HasComponent<UnitPathRequest>(b));Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(d));
        }
    }
}
