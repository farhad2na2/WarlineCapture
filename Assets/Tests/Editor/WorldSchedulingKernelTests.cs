using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Combat;
using Unity.Mathematics;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class WorldSchedulingKernelTests
    {
        public static int RunChecks()
        {
            var suite=new WorldSchedulingKernelTests();suite.PinnedOriginalSources();suite.CadenceAndAdmissionParity();suite.NativeSubsetOrderAndDeferredPlayback();suite.UnengagedRecoveryMatchesOriginalManagedBurstAndNative();return 4;
        }
        [Test] public void PinnedOriginalSources()
        {
            string[] names={"AITargetingSystem","UnitAttackSystem","UnitGridMovementSystem","UnitEngagementSystem"};
            string[] hashes={"43fc33330dd7c831f8872390ef6b2edfb3547afb8374ff7cae23a8b92b46375b","865bd597a91bf2172ea6e55f68147c711a024fe551e2b9874544a6ab19b6e76d","dfe6a2203a8a089b3b049bdd1d3f53653d0b473b0370dab32471d420b79fcaca","265d75666daf50a05863f418f703105c736e70b28a811c1615ec02d3092785e7"};
            using var sha=SHA256.Create();
            for(int i=0;i<names.Length;i++)
            {
                string original=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/"+names[i]+"SchedulingOriginal.cs.txt")).Replace("\r\n","\n");
                Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(original))).Replace("-","").ToLowerInvariant());
            }
        }
        private struct Input { public double Now, NextUnit; public float NextAi, Delta; public byte Active,HasGameplay,HasSkirmish,Finished; }
        private struct Output { public float NextAi,NowAi;public double NextUnit;public byte Ai,Unit,Movement,Attack; }
        [BurstCompile(CompileSynchronously=true)] private struct CadenceJob:IJob
        {
            [ReadOnly] public NativeArray<Input> Inputs;
            public NativeArray<Output> Outputs;
            public NativeArray<int> Managed;
            public void Execute()
            {
                int managed=0;MarkManaged(ref managed);Managed[0]=managed;
                for(int i=0;i<Inputs.Length;i++)
                {
                    var input=Inputs[i];var output=new Output{NextAi=input.NextAi,NextUnit=input.NextUnit};
                    output.Ai=(byte)(WorldSchedulingKernel.TryAdvanceAiTargetRefresh(input.Active!=0,input.Now,ref output.NextAi,out output.NowAi)?1:0);
                    output.Unit=(byte)(WorldSchedulingKernel.TryAdvanceUnitAcquisition(input.Now,ref output.NextUnit)?1:0);
                    output.Movement=(byte)(WorldSchedulingKernel.ShouldRunGridMovement(input.Delta,input.HasGameplay!=0,input.Active)?1:0);
                    output.Attack=(byte)(WorldSchedulingKernel.ShouldRunAttack(input.HasSkirmish!=0,input.Finished!=0)?1:0);Outputs[i]=output;
                }
            }
            [BurstDiscard] private static void MarkManaged(ref int value){value=1;}
        }
        private static Output Original(Input i)
        {
            var o=new Output{NextAi=i.NextAi,NextUnit=i.NextUnit};
            if(i.Active!=0)
            {
                o.NowAi=i.Now>float.MaxValue?float.MaxValue:(float)i.Now;
                if(!(o.NowAi<o.NextAi)){o.NextAi=o.NowAi+0.5f;o.Ai=1;}
            }
            if(!(i.Now<o.NextUnit)){o.NextUnit=i.Now+0.12d;o.Unit=1;}
            o.Movement=(byte)(!(i.Delta<=0f||(i.HasGameplay!=0&&i.Active==0))?1:0);
            o.Attack=(byte)(!(i.HasSkirmish!=0&&i.Finished!=0)?1:0);return o;
        }
        private static void Equal(Output expected,Output actual,string context)
        {
            Assert.AreEqual(expected.Ai,actual.Ai,context);Assert.AreEqual(expected.Unit,actual.Unit,context);Assert.AreEqual(expected.Movement,actual.Movement,context);Assert.AreEqual(expected.Attack,actual.Attack,context);
            if(!float.IsNaN(expected.NextAi))Assert.AreEqual(BitConverter.SingleToInt32Bits(expected.NextAi),BitConverter.SingleToInt32Bits(actual.NextAi),context);else Assert.IsTrue(float.IsNaN(actual.NextAi));
            if(!float.IsNaN(expected.NowAi))Assert.AreEqual(BitConverter.SingleToInt32Bits(expected.NowAi),BitConverter.SingleToInt32Bits(actual.NowAi),context);else Assert.IsTrue(float.IsNaN(actual.NowAi));
            if(!double.IsNaN(expected.NextUnit))Assert.AreEqual(BitConverter.DoubleToInt64Bits(expected.NextUnit),BitConverter.DoubleToInt64Bits(actual.NextUnit),context);else Assert.IsTrue(double.IsNaN(actual.NextUnit));
        }
        [Test] public void CadenceAndAdmissionParity()
        {
            const int n=4096;using var inputsOwner=new NativeArray<Input>(n,Allocator.TempJob);using var outputs=new NativeArray<Output>(n,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);var inputs=inputsOwner;
            double[] boundary={0,.119999999,.12,.5,double.NaN,double.PositiveInfinity,double.NegativeInfinity,float.MaxValue,(double)float.MaxValue*2,-1};
            var random=new System.Random(38471);
            for(int i=0;i<n;i++)inputs[i]=new Input{Now=i<boundary.Length*32?boundary[i%boundary.Length]:random.NextDouble()*1000,NextAi=i%2==0?0:(float)random.NextDouble()*1000,NextUnit=i%3==0?0:random.NextDouble()*1000,Delta=i%4==0?0:i%4==1?-1:i%4==2?float.NaN:.01f,Active=(byte)(i%2),HasGameplay=(byte)((i/2)%2),HasSkirmish=(byte)((i/4)%2),Finished=(byte)((i/8)%2)};
            var job=new CadenceJob{Inputs=inputs,Outputs=outputs,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);
            for(int i=0;i<n;i++)Equal(Original(inputs[i]),outputs[i],"managed "+i);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Burst must execute");for(int i=0;i<n;i++)Equal(Original(inputs[i]),outputs[i],"Burst "+i);
            float next=0;Assert.IsFalse(WorldSchedulingKernel.TryAdvanceAiTargetRefresh(false,100,ref next,out _));Assert.AreEqual(0,next);Assert.IsTrue(WorldSchedulingKernel.TryAdvanceAiTargetRefresh(true,100,ref next,out _));Assert.AreEqual(100.5f,next);Assert.IsFalse(WorldSchedulingKernel.TryAdvanceAiTargetRefresh(true,100.49,ref next,out _));
            double acquisition=0;Assert.IsTrue(WorldSchedulingKernel.TryAdvanceUnitAcquisition(100,ref acquisition));Assert.AreEqual(100.12,acquisition);Assert.IsTrue(WorldSchedulingKernel.ShouldRunAttack(false,false));
        }
        private struct RecoveryInput { public StandardAttackState State;public float Delta; }
        private static StandardAttackState OriginalRecovery(in StandardAttackState original,float dt)
        {
            var next=original;
            if(dt>0f)
            {
                float cooldownRemaining=original.CooldownRemaining;
                if(cooldownRemaining>0f)next.CooldownRemaining=math.max(0f,cooldownRemaining-dt);
                float traceRemaining=original.TraceTimeRemaining;
                if(traceRemaining>0f)next.TraceTimeRemaining=math.max(0f,traceRemaining-dt);
            }
            return next;
        }
        [BurstCompile(CompileSynchronously=true)] private struct RecoveryJob:IJob
        {
            [ReadOnly] public NativeArray<RecoveryInput> Inputs;
            public NativeArray<StandardAttackState> OriginalResults,SharedResults;
            public NativeArray<int> Managed;
            public void Execute()
            {
                int managed=0;MarkManaged(ref managed);Managed[0]=managed;
                for(int i=0;i<Inputs.Length;i++){var input=Inputs[i];OriginalResults[i]=OriginalRecovery(input.State,input.Delta);SharedResults[i]=DirectDamageSequencer.RecoverUnengaged(input.State,input.Delta);}
            }
            [BurstDiscard] private static void MarkManaged(ref int value){value=1;}
        }
        private static void EqualRecovery(StandardAttackState a,StandardAttackState b,string context)
        {
            void EqualFloat(float expected,float actual)
            {if(float.IsNaN(expected))Assert.IsTrue(float.IsNaN(actual),context);else Assert.AreEqual(BitConverter.SingleToInt32Bits(expected),BitConverter.SingleToInt32Bits(actual),context);}
            EqualFloat(a.CooldownRemaining,b.CooldownRemaining);EqualFloat(a.TraceTimeRemaining,b.TraceTimeRemaining);EqualFloat(a.TracePhase,b.TracePhase);EqualFloat(a.AnimationTimeRemaining,b.AnimationTimeRemaining);Assert.AreEqual(a.ShotCounter,b.ShotCounter,context);
        }
        [Test] public void UnengagedRecoveryMatchesOriginalManagedBurstAndNative()
        {
            string snapshot=File.ReadAllText(Path.Combine(Application.dataPath,"Tests/Editor/UnitAttackSystemSchedulingOriginal.cs.txt"));
            StringAssert.Contains("if (dt > 0f)",snapshot);
            StringAssert.Contains("if (cooldownRemaining > 0f)\n                        attackState.ValueRW.CooldownRemaining = math.max(0f, cooldownRemaining - dt);",snapshot.Replace("\r\n","\n"));
            StringAssert.Contains("if (traceRemaining > 0f)\n                        traceState.ValueRW.TimeRemaining = math.max(0f, traceRemaining - dt);",snapshot.Replace("\r\n","\n"));
            float[] values={-1,-0f,0,.01f,.1f,1,float.Epsilon,float.MaxValue,float.PositiveInfinity,float.NegativeInfinity,float.NaN};
            int boundary=values.Length*values.Length*values.Length,n=boundary+4096;
            using var inputsOwner=new NativeArray<RecoveryInput>(n,Allocator.TempJob);using var expected=new NativeArray<StandardAttackState>(n,Allocator.TempJob);using var actual=new NativeArray<StandardAttackState>(n,Allocator.TempJob);using var managed=new NativeArray<int>(1,Allocator.TempJob);var inputs=inputsOwner;
            int index=0;foreach(float cooldown in values)foreach(float trace in values)foreach(float delta in values)
                inputs[index++]=new RecoveryInput{State=new StandardAttackState{CooldownRemaining=cooldown,TraceTimeRemaining=trace,TracePhase=.371f,AnimationTimeRemaining=2.75f,ShotCounter=int.MaxValue},Delta=delta};
            var random=new System.Random(728137);
            while(index<n){inputs[index]=new RecoveryInput{State=new StandardAttackState{CooldownRemaining=(float)(random.NextDouble()*5-1),TraceTimeRemaining=(float)(random.NextDouble()*3-1),TracePhase=(float)random.NextDouble(),AnimationTimeRemaining=(float)random.NextDouble(),ShotCounter=index},Delta=(float)(random.NextDouble()*3-1)};index++;}
            var job=new RecoveryJob{Inputs=inputs,OriginalResults=expected,SharedResults=actual,Managed=managed};job.Execute();Assert.AreEqual(1,managed[0]);for(int i=0;i<n;i++)EqualRecovery(expected[i],actual[i],"managed recovery "+i);
            job.Schedule().Complete();Assert.AreEqual(0,managed[0],"Actual Burst recovery required");for(int i=0;i<n;i++)EqualRecovery(expected[i],actual[i],"Burst recovery "+i);
            var native=new global::UnitCombatFocusedEditModeTests();native.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();native.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
            Debug.Log("[UnengagedRecoveryParity] result=Passed cases="+n+" managed=Passed burst=Passed nativeRegressions=2");
        }
        [Serializable] private class OrderReceipt { public string profile,scope;public bool full_world;public string[] systems; }
        private static string[] Names(World world,ComponentSystemGroup group)
        {
            using var handles=group.GetAllSystems();var names=new string[handles.Length];
            for(int i=0;i<handles.Length;i++)names[i]=TypeManager.GetSystemName(world.Unmanaged.GetSystemTypeIndex(handles[i])).ToString();return names;
        }
        [Test] public void NativeSubsetOrderAndDeferredPlayback()
        {
            using var world=new World("IsolatedStationaryCombatSchedule");var group=world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            Type[] types={typeof(BeginSimulationEntityCommandBufferSystem),typeof(DynamicOccupancyRebuildSystem),typeof(UnitEngagementSystem),typeof(UnitEngagedMovementSystem),typeof(UnitAttackSystem),typeof(SkirmishObjectiveFactProjectionSystem),typeof(SkirmishBaseAssaultObjectiveSystem),typeof(SkirmishOutcomeSystem),typeof(EndSimulationEntityCommandBufferSystem)};
            foreach(var type in types)group.AddSystemToUpdateList(world.GetOrCreateSystem(type));
            group.SortSystems();var names=Names(world,group);
            void Before(Type a,Type b){Assert.Less(Array.IndexOf(names,a.FullName),Array.IndexOf(names,b.FullName),a.Name+" before "+b.Name);}
            Before(typeof(DynamicOccupancyRebuildSystem),typeof(UnitEngagementSystem));Before(typeof(UnitEngagementSystem),typeof(UnitEngagedMovementSystem));Before(typeof(UnitEngagedMovementSystem),typeof(UnitAttackSystem));
            Before(typeof(SkirmishObjectiveFactProjectionSystem),typeof(SkirmishBaseAssaultObjectiveSystem));Before(typeof(SkirmishBaseAssaultObjectiveSystem),typeof(SkirmishOutcomeSystem));
            foreach(var type in types)if(type!=typeof(BeginSimulationEntityCommandBufferSystem)&&type!=typeof(EndSimulationEntityCommandBufferSystem)){Before(typeof(BeginSimulationEntityCommandBufferSystem),type);Before(type,typeof(EndSimulationEntityCommandBufferSystem));}
            Debug.Log("[WorldSchedulingNativeOrder] "+JsonUtility.ToJson(new OrderReceipt{profile="IsolatedStationaryCombatScheduleV1",scope="NativeSortedSubsetOnly",full_world=false,systems=names}));
            var end=world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();var em=world.EntityManager;var unit=em.CreateEntity();var target=em.CreateEntity();end.CreateCommandBuffer().AddComponent(unit,new EngageTarget{Target=target});
            Assert.IsFalse(em.HasComponent<EngageTarget>(unit),"Acquisition stays deferred before EndSimulation");end.Update();Assert.AreEqual(target,em.GetComponentData<EngageTarget>(unit).Target);
            var live=World.DefaultGameObjectInjectionWorld;
            if(live!=null&&live.IsCreated&&live!=world)
            {
                var actual=live.GetExistingSystemManaged<SimulationSystemGroup>();
                if(actual!=null)Debug.Log("[WorldSchedulingObservedDefaultWorld] "+JsonUtility.ToJson(new OrderReceipt{profile="ObservedDefaultWorld",scope="ReadOnlyExistingRootList_NotNestedGroupProof",full_world=false,systems=Names(live,actual)}));
            }
        }
    }
}
