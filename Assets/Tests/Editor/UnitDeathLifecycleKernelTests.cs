using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class UnitDeathLifecycleKernelTests
    {
        public static int RunChecks()
        {
            var t=new UnitDeathLifecycleKernelTests();t.OriginalSourcesPinned();t.ManagedAndBurstMatchOriginalArithmetic();t.NativeStripEffectsAreExact();t.NativeSameFrameTickFreezeAndDelayedRemoval();
            // Direct executeMethod calls do not run NUnit's TearDown lifecycle.
            void RunLegacy(Action<global::CombatDeathValidationTests> check)
            {
                var old=new global::CombatDeathValidationTests();
                try { check(old); }
                finally { old.TearDown(); }
            }
            RunLegacy(old=>old.SoldierAttack_KillsTargetRetainsVisibleCorpseThenCullsOffscreenWithoutRespawn());
            RunLegacy(old=>old.DeathPlayback_ResetsClockAndStopsAtFirstFinalFrame());
            RunLegacy(old=>old.AirVehicleDeath_WithDestroyedVisual_HidesAliveVisualAndSpawnsDestroyedVisualWithoutGrid());
            RunLegacy(old=>old.VehicleWreckCleanup_FinalizesExpiredWreckAndDescendants());
            Debug.Log("[UnitDeathLifecycleParity] result=Passed parity=4 regressions=4 missionReady=0");return 8;
        }
        [Test] public void OriginalSourcesPinned()
        {
            string[] names={"UnitDeathSystem","UnitDeathAnimationPlaybackUtility"};string[] hashes={"97e4817e1605f0fd14ecf6104d58ab17943d21a9452abf4c560b4c9fddf305cb","1d354824e6b7934f2671ebc875ea537e20c1921ed26fed97b564bedcc4a5dbd5"};
            using var sha=SHA256.Create();for(int i=0;i<names.Length;i++){string text=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+names[i]+"LifecycleOriginal.cs.txt")).Replace("\r\n","\n");Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant());}
        }
        private struct Input {public int Health,Frames;public float Remaining,Delta,Configured;public byte Frozen,Fps,Speed;}
        private struct Output {public InfantryDeathState State;public bool Ready,Begin;public float Duration,Clip;}
        private static Output Original(Input input)
        {
            var o=new Output{State=new InfantryDeathState{TimeRemaining=input.Remaining,PoseFrozen=input.Frozen}};
            if(input.Health<=0){o.Begin=true;o.Duration=math.max(.01f,input.Configured);if(input.Frozen!=0)o.Ready=true;else{o.State.TimeRemaining=math.max(0f,input.Remaining-input.Delta);o.Ready=o.State.TimeRemaining<=0f;}}
            float fallback=math.max(.01f,input.Configured);o.Clip=fallback;
            if(input.Frames>1&&input.Fps!=0&&input.Speed!=0){float rate=input.Fps*input.Speed;float final=(input.Frames-1)/rate;o.Clip=math.isfinite(final)&&final>0?math.max(.01f,final):fallback;}return o;
        }
        [BurstCompile(CompileSynchronously=true)] private struct Job:IJob
        {
            [ReadOnly]public NativeArray<Input> Inputs;public NativeArray<Output> Expected,Actual;public NativeArray<int> Managed;
            public void Execute(){int managed=0;MarkManaged(ref managed);Managed[0]=managed;for(int i=0;i<Inputs.Length;i++){var x=Inputs[i];Expected[i]=Original(x);var o=new Output{State=new InfantryDeathState{TimeRemaining=x.Remaining,PoseFrozen=x.Frozen}};o.Begin=UnitDeathLifecycleKernel.TryBegin(x.Health,x.Configured,out o.Duration);o.Ready=UnitDeathLifecycleKernel.Tick(x.Health,x.Delta,ref o.State);o.Clip=UnitDeathLifecycleKernel.ResolveSinglePlaybackDuration(x.Frames,x.Fps,x.Speed,x.Configured);Actual[i]=o;}}
            [BurstDiscard]private static void MarkManaged(ref int n){n=1;}
        }
        private static void Equal(Output a,Output b)
        {
            void F(float x,float y){if(float.IsNaN(x))Assert.IsTrue(float.IsNaN(y));else Assert.AreEqual(BitConverter.SingleToInt32Bits(x),BitConverter.SingleToInt32Bits(y));}
            Assert.AreEqual(a.Ready,b.Ready);Assert.AreEqual(a.Begin,b.Begin);Assert.AreEqual(a.State.PoseFrozen,b.State.PoseFrozen);F(a.State.TimeRemaining,b.State.TimeRemaining);F(a.Duration,b.Duration);F(a.Clip,b.Clip);
        }
        [Test]public void ManagedAndBurstMatchOriginalArithmetic()
        {
            const int count=4096;using var owner=new NativeArray<Input>(count,Allocator.TempJob);using var expected=new NativeArray<Output>(count,Allocator.TempJob);using var actual=new NativeArray<Output>(count,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);var input=owner;
            float[] edges={-1,-0f,0,.01f,.1f,1,float.NaN,float.PositiveInfinity,float.NegativeInfinity};var random=new System.Random(5721);
            for(int i=0;i<count;i++)input[i]=new Input{Health=i%3-1,Frames=i%17==0?int.MaxValue:i%101,Frozen=(byte)(i%3),Remaining=i<729?edges[i%9]:(float)random.NextDouble()*2,Delta=i<729?edges[(i/9)%9]:(float)random.NextDouble(),Configured=i<729?edges[(i/81)%9]:(float)random.NextDouble(),Fps=(byte)(i%256),Speed=(byte)((i/11)%256)};
            var job=new Job{Inputs=input,Expected=expected,Actual=actual,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);for(int i=0;i<count;i++)Equal(expected[i],actual[i]);job.Schedule().Complete();Assert.AreEqual(0,managed[0]);for(int i=0;i<count;i++)Equal(expected[i],actual[i]);
            for(int frozen=0;frozen<3;frozen++)for(int camera=0;camera<2;camera++)for(int visible=0;visible<2;visible++)Assert.AreEqual(frozen==0?DeathFinalizationAction.Freeze:camera==0||visible!=0?DeathFinalizationAction.Retain:DeathFinalizationAction.Remove,UnitDeathLifecycleKernel.DecideFinalize((byte)frozen,camera!=0,visible!=0));
            var state=new InfantryDeathState{TimeRemaining=7,PoseFrozen=0};UnitDeathLifecycleKernel.Freeze(ref state);Assert.AreEqual(0,state.TimeRemaining);Assert.AreEqual(1,state.PoseFrozen);
        }
        [Test]public void NativeStripEffectsAreExact()
        {
            using var world=new World("DeathStripNativeParity");var em=world.EntityManager;var entity=em.CreateEntity(typeof(UnitHealth),typeof(UnitGrid),typeof(UnitAttack),typeof(UnitCombat),typeof(HoldPositionOrderTag),typeof(AttackMoveOrder),typeof(SelectedUnitTag),typeof(ManualMoveOrderTag),typeof(AutoWanderMoveTag),typeof(EngageTarget),typeof(RecentAttacker),typeof(RecentDamageHealthBarVisibility),typeof(UnitPathFollow),typeof(UnitPathRange),typeof(UnitPathRequest),typeof(UnitAttackAnimationComponent),typeof(UnitMoveVisualComponent));
            em.AddBuffer<UnitAttachedLightSetupElement>(entity);em.SetComponentData(entity,new UnitAttackAnimationComponent{TimeRemaining=9});em.SetComponentData(entity,new UnitMoveVisualComponent{IsMoving=1,StillSeconds=9});
            UnitDeathSystem.StripActiveUnitState(em,entity);
            Type[] removed={typeof(SelectedUnitTag),typeof(ManualMoveOrderTag),typeof(AutoWanderMoveTag),typeof(EngageTarget),typeof(RecentAttacker),typeof(RecentDamageHealthBarVisibility),typeof(UnitPathFollow),typeof(UnitPathRange),typeof(UnitPathRequest)};
            foreach(var type in removed)Assert.IsFalse(em.HasComponent(entity,ComponentType.ReadOnly(type)),type.Name);
            Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(entity));Assert.IsTrue(em.HasComponent<AttackMoveOrder>(entity));Assert.IsTrue(em.HasComponent<UnitCombat>(entity));Assert.IsTrue(em.HasComponent<UnitAttack>(entity));Assert.IsTrue(em.HasComponent<UnitGrid>(entity));Assert.IsTrue(em.HasComponent<UnitHealth>(entity));Assert.IsTrue(em.HasComponent<UnitAttachedLightCleanupRequest>(entity));
            Assert.AreEqual(0,em.GetComponentData<UnitAttackAnimationComponent>(entity).TimeRemaining);Assert.AreEqual(0,em.GetComponentData<UnitMoveVisualComponent>(entity).IsMoving);Assert.AreEqual(0,em.GetComponentData<UnitMoveVisualComponent>(entity).StillSeconds);
            UnitDeathSystem.StripActiveUnitState(em,entity);Assert.IsTrue(em.HasComponent<UnitAttachedLightCleanupRequest>(entity));
            Assert.AreEqual(DeathStripFlags.RequestLightCleanup,UnitDeathLifecycleKernel.PlanStrip(DeathStripFlags.None,true,false));Assert.AreEqual(DeathStripFlags.None,UnitDeathLifecycleKernel.PlanStrip(DeathStripFlags.None,true,true));
        }
        [Test]public void NativeSameFrameTickFreezeAndDelayedRemoval()
        {
            using var world=new World("DeathSameFrameNativeParity");var em=world.EntityManager;var entity=em.CreateEntity(typeof(UnitHealth),typeof(UnitAnimationSettings));em.SetComponentData(entity,new UnitHealth{Current=0,Max=10});em.SetComponentData(entity,new UnitAnimationSettings{DeathAnimationSeconds=.2f});var system=world.GetOrCreateSystem<UnitDeathSystem>();
            world.SetTime(new TimeData(1,.1f));system.Update(world.Unmanaged);Assert.IsTrue(em.HasComponent<UnitDeathAnimationComponent>(entity));Assert.AreEqual(.1f,em.GetComponentData<UnitDeathAnimationComponent>(entity).TimeRemaining);Assert.AreEqual(0,em.GetComponentData<UnitDeathAnimationComponent>(entity).PoseFrozen);
            world.SetTime(new TimeData(2,.1f));system.Update(world.Unmanaged);Assert.IsTrue(em.Exists(entity));Assert.AreEqual(1,em.GetComponentData<UnitDeathAnimationComponent>(entity).PoseFrozen);
            world.SetTime(new TimeData(3,0));system.Update(world.Unmanaged);Assert.IsTrue(em.Exists(entity),"No valid camera retains frozen corpse even zero delta");
            var camera=em.CreateEntity(typeof(RuntimeCameraSnapshotComponent));em.SetComponentData(camera,new RuntimeCameraSnapshotComponent{IsValid=1});world.SetTime(new TimeData(4,0));system.Update(world.Unmanaged);Assert.IsFalse(em.Exists(entity),"Frozen offscreen corpse finalizes on later update, including zero delta");
            using var queues=em.CreateEntityQuery(typeof(RespawnQueueTag));Assert.AreEqual(1,queues.CalculateEntityCount());Assert.AreEqual(0,em.GetBuffer<RespawnRequest>(queues.GetSingletonEntity()).Length,"Death finalization does not enqueue respawn");
        }
    }
}
