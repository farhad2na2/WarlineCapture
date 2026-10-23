using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
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
    public sealed class EngageTargetLifecycleKernelTests
    {
        public static int RunChecks(){var t=new EngageTargetLifecycleKernelTests();t.OriginalSourcesPinned();t.ManagedAndBurstDecisionParity();t.NativeValidationIsImmediateAndRestoresExactOrders();t.NativeSyncUpdatesCacheAndDefersRemoval();Debug.Log("[EngageTargetLifecycleParity] result=Passed checks=4 missionReady=0");return 4;}
        [Test]public void OriginalSourcesPinned()
        {
            string[] names={"EngageTargetValidateSystem","EngageTargetSyncSystem"};string[] hashes={"aa7312147affe4557bf2c4e37160cb343c108bf6c214e9654f2a0f4e8617107f","0b7eaa9e9d093601860672c9295ec24c50d65f454fc4615b411f7bd6fe2c5c65"};using var sha=SHA256.Create();
            for(int i=0;i<names.Length;i++){string source=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+names[i]+"LifecycleOriginal.cs.txt")).Replace("\r\n","\n");Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source))).Replace("-","").ToLowerInvariant());}
        }
        private struct Input {public EngageValidationFacts Facts;public EngagementTargetState State;public bool Air,Path,Target,Transform;public float X,Y,Z;public int CellX,CellZ;}
        private struct Output {public bool Invalid;public EngageInvalidationEffects Effects;public EngagementSyncResult Sync;}
        private static Output Original(Input i)
        {
            bool invalid=false;if(i.Facts.TargetNull)invalid=true;else if(!i.Facts.TargetHasTransform)invalid=true;else if(i.Facts.TargetHasHealth&&i.Facts.TargetHealth<=0)invalid=true;
            if(!invalid&&i.Facts.PairedFog)invalid=!i.Facts.SameFogSession||!i.Facts.FogVisible;
            var o=new Output{Invalid=invalid,Sync=new EngagementSyncResult{State=i.State}};
            if(i.Air)o.Effects.ReturningHome=true;else if(!i.Path&&i.Target)o.Effects.RestorePathRequest=true;
            if((i.State.TargetIndex==0&&i.State.TargetVersion==0)||!i.Transform){o.Sync.State.TargetIndex=0;o.Sync.State.TargetVersion=0;o.Sync.State.CellX=0;o.Sync.State.CellZ=0;o.Sync.State.X=0;o.Sync.State.Y=0;o.Sync.State.Z=0;o.Sync.RemoveAtEndSimulation=true;}
            else{o.Sync.State.X=i.X;o.Sync.State.Y=i.Y;o.Sync.State.Z=i.Z;o.Sync.State.CellX=i.CellX;o.Sync.State.CellZ=i.CellZ;}return o;
        }
        [BurstCompile(CompileSynchronously=true)]private struct Job:IJob
        {
            [ReadOnly]public NativeArray<Input> Inputs;public NativeArray<Output> Expected,Actual;public NativeArray<int> Managed;
            public void Execute(){int managed=0;MarkManaged(ref managed);Managed[0]=managed;for(int j=0;j<Inputs.Length;j++){var i=Inputs[j];Expected[j]=Original(i);Actual[j]=new Output{Invalid=EngageTargetLifecycleKernel.IsInvalid(i.Facts),Effects=EngageTargetLifecycleKernel.PlanInvalidation(i.Air,i.Path,i.Target),Sync=EngageTargetLifecycleKernel.Sync(i.State,i.Transform,i.X,i.Y,i.Z,i.CellX,i.CellZ)};}}
            [BurstDiscard]private static void MarkManaged(ref int n){n=1;}
        }
        private static void Equal(Output a,Output b)
        {
            Assert.AreEqual(a.Invalid,b.Invalid);Assert.AreEqual(a.Effects.ReturningHome,b.Effects.ReturningHome);Assert.AreEqual(a.Effects.RestorePathRequest,b.Effects.RestorePathRequest);Assert.AreEqual(a.Sync.RemoveAtEndSimulation,b.Sync.RemoveAtEndSimulation);Assert.AreEqual(a.Sync.State.TargetIndex,b.Sync.State.TargetIndex);Assert.AreEqual(a.Sync.State.TargetVersion,b.Sync.State.TargetVersion);Assert.AreEqual(a.Sync.State.CellX,b.Sync.State.CellX);Assert.AreEqual(a.Sync.State.CellZ,b.Sync.State.CellZ);Assert.AreEqual(a.Sync.State.IsCommanded,b.Sync.State.IsCommanded);Assert.AreEqual(BitConverter.SingleToInt32Bits(a.Sync.State.X),BitConverter.SingleToInt32Bits(b.Sync.State.X));Assert.AreEqual(BitConverter.SingleToInt32Bits(a.Sync.State.Y),BitConverter.SingleToInt32Bits(b.Sync.State.Y));Assert.AreEqual(BitConverter.SingleToInt32Bits(a.Sync.State.Z),BitConverter.SingleToInt32Bits(b.Sync.State.Z));
        }
        [Test]public void ManagedAndBurstDecisionParity()
        {
            const int n=4096;using var owner=new NativeArray<Input>(n,Allocator.TempJob);using var expected=new NativeArray<Output>(n,Allocator.TempJob);using var actual=new NativeArray<Output>(n,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);var input=owner;var random=new System.Random(14373);
            for(int j=0;j<n;j++)input[j]=new Input{Facts=new EngageValidationFacts{TargetNull=(j&1)!=0,TargetHasTransform=(j&2)!=0,TargetHasHealth=(j&4)!=0,TargetHealth=j%3-1,PairedFog=(j&8)!=0,SameFogSession=(j&16)!=0,FogVisible=(j&32)!=0},Air=(j&64)!=0,Path=(j&128)!=0,Target=(j&256)!=0,Transform=(j&512)!=0,State=new EngagementTargetState{TargetIndex=j%5==0?0:j,TargetVersion=j%5==0?0:j%3+1,CellX=51,CellZ=-31,X=-0f,Y=17,Z=-19,IsCommanded=(byte)(j%256)},X=(float)random.NextDouble()*100,Y=(float)random.NextDouble()*10,Z=(float)random.NextDouble()*-100,CellX=j%31,CellZ=j%17};
            var job=new Job{Inputs=input,Expected=expected,Actual=actual,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);for(int j=0;j<n;j++)Equal(expected[j],actual[j]);job.Schedule().Complete();Assert.AreEqual(0,managed[0]);for(int j=0;j<n;j++)Equal(expected[j],actual[j]);
        }
        [Test]public void NativeValidationIsImmediateAndRestoresExactOrders()
        {
            using var world=new World("ImmediateEngageValidation");var em=world.EntityManager;var dead=em.CreateEntity(typeof(LocalTransform),typeof(UnitHealth));em.SetComponentData(dead,new UnitHealth{Current=0,Max=1});
            Entity Source(bool air,bool request){var e=em.CreateEntity(typeof(EngageTarget),typeof(UnitTarget));em.SetComponentData(e,new EngageTarget{Target=dead,IsCommanded=1});em.SetComponentData(e,new UnitTarget{Cell=new int2(5,7)});if(air)em.AddComponentData(e,new UnitAirComponent{ReturningHome=0});if(request)em.AddComponentData(e,new UnitPathRequest{Goal=new int2(9,11)});return e;}
            var ground=Source(false,false);var existing=Source(false,true);var flying=Source(true,false);
            world.GetOrCreateSystem<EngageTargetValidateSystem>().Update(world.Unmanaged);
            Assert.IsFalse(em.HasComponent<EngageTarget>(ground),"Local ECB must remove before acquisition without EndSimulation playback");Assert.AreEqual(new int2(5,7),em.GetComponentData<UnitPathRequest>(ground).Goal);Assert.AreEqual(new int2(9,11),em.GetComponentData<UnitPathRequest>(existing).Goal);Assert.AreEqual(1,em.GetComponentData<UnitAirComponent>(flying).ReturningHome);Assert.IsFalse(em.HasComponent<UnitPathRequest>(flying));
            var noHealth=em.CreateEntity(typeof(LocalTransform));var valid=em.CreateEntity(typeof(EngageTarget));em.SetComponentData(valid,new EngageTarget{Target=noHealth,IsCommanded=7});world.GetOrCreateSystem<EngageTargetValidateSystem>().Update(world.Unmanaged);Assert.IsTrue(em.HasComponent<EngageTarget>(valid),"Missing target health is allowed");
            em.AddComponentData(valid,new SkirmishAttemptOwnedComponent{SessionId="observer-session",FactionId=1});em.AddComponentData(noHealth,new SkirmishAttemptOwnedComponent{SessionId="different-session",FactionId=2});world.GetOrCreateSystem<EngageTargetValidateSystem>().Update(world.Unmanaged);Assert.IsFalse(em.HasComponent<EngageTarget>(valid),"Different fog sessions invalidate immediately");
        }
        [Test]public void NativeSyncUpdatesCacheAndDefersRemoval()
        {
            using var world=new World("DeferredEngageSync");var em=world.EntityManager;var grid=new GridConfig{Width=16,Height=16,CellSize=2,Origin=new float3(10,0,20)};var ge=em.CreateEntity(typeof(GridConfig));em.SetComponentData(ge,grid);var end=world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var target=em.CreateEntity(typeof(LocalTransform));var position=new float3(17,3,29);em.SetComponentData(target,LocalTransform.FromPosition(position));var unit=em.CreateEntity(typeof(LocalTransform),typeof(EngageTarget));em.SetComponentData(unit,new EngageTarget{Target=target,Cell=new int2(99),Position=new float3(-100),IsCommanded=9});
            var system=world.GetOrCreateSystem<EngageTargetSyncSystem>();system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();var engage=em.GetComponentData<EngageTarget>(unit);Assert.AreEqual(target,engage.Target);Assert.AreEqual(position,engage.Position);Assert.AreEqual(GridUtils.WorldToCell(grid,position),engage.Cell);Assert.AreEqual(9,engage.IsCommanded);
            em.DestroyEntity(target);system.Update(world.Unmanaged);em.CompleteAllTrackedJobs();Assert.IsTrue(em.HasComponent<EngageTarget>(unit),"Sync removal must remain deferred until EndSimulation");engage=em.GetComponentData<EngageTarget>(unit);Assert.AreEqual(Entity.Null,engage.Target);Assert.AreEqual(default(int2),engage.Cell);Assert.AreEqual(default(float3),engage.Position);Assert.AreEqual(9,engage.IsCommanded);end.Update();Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        }
    }
}
