using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Core;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishMissionOrchestrationKernelTests
    {
        public static int RunChecks()
        {
            var t=new SkirmishMissionOrchestrationKernelTests();
            t.OriginalSourcesAndOraclePinned();t.BaseAssaultMatchesOriginal();t.CollectorClockMatchesOriginal();
            t.ClockAndFreezeMatchNativePhases();t.DispatchSurrenderAndStaging();
            var old=new SkirmishExpandedObjectiveTests();
            old.FinishedBaseFactsStayFrozenAfterDestroyedEntitiesAreRemoved();old.ExpandedResultCountsAttemptLossesOnceAndFreezesBeforeCleanup();
            old.ReplacementBarracksAndArmyWipeAreNonTerminal();old.PausedDeadlineDoesNotFreeze();old.BothDesignatedDeadIsDrawRegardlessOfReplacement();
            old.SurrenderRequiresPlaying();old.DesignatedFactsIgnoreReplacementBarracks();old.HiddenHealthProjectsVisibleLastSeenAndUnknown();
            Debug.Log("[SkirmishMissionOrchestrationParity] result=Passed parity=5 lifecycle=8 missionReady=0");return 13;
        }
        [Test] public void OriginalSourcesAndOraclePinned()
        {
            string[] names={"SkirmishObjectiveFactProjectionSystem","SkirmishNativeObjectiveTick","SkirmishBaseAssaultObjectiveSystem","SkirmishOutcomeSystem","SkirmishNativeObjectiveCollector"};
            string[] hashes={"a3fd6812e9a5bfa7f094928d79d0a4755eb5b2c90d81e25752234585fe23c362","ed6ed111d8c1765062a7b7d68a0dfeb3ae3e657030da5d2013e641a5c81b9e7a","4308e2f89879298d523bd0f86fb2a163bd3bd921c93da0d0bf4770c8d1ecc114","c7baa4f65e8c01dc784ff2dafdfef1c03ea8322074c61b7f9c9c3837bbceadec","ec89378f80c6a3391bc9ac897c8ccd45b304085e30bfe07ccd827f6db5e832d2"};
            string dir=Path.Combine(Application.dataPath,"Tests/Editor");var sources=new string[names.Length];
            using var sha=SHA256.Create();
            for(int i=0;i<names.Length;i++) {sources[i]=File.ReadAllText(Path.Combine(dir,names[i]+"OrchestrationOriginal.cs.txt")).Replace("\r\n","\n");Assert.AreEqual(hashes[i],BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sources[i]))).Replace("-","").ToLowerInvariant());}
            string oracle=File.ReadAllText(Path.Combine(dir,"SkirmishMissionOrchestrationOriginalOracle.cs")).Replace("\r\n","\n");
            int start=sources[2].IndexOf("        public static bool TryEvaluate(\n            in SkirmishBaseAssaultFacts",StringComparison.Ordinal);
            StringAssert.Contains(sources[2].Substring(start,sources[2].IndexOf("        /// <summary>",start,StringComparison.Ordinal)-start),oracle);
            start=sources[4].IndexOf("        public static bool TryProjectClock(",StringComparison.Ordinal);
            StringAssert.Contains(sources[4].Substring(start,sources[4].IndexOf("        public bool TryCommitCollectedTombstones",start,StringComparison.Ordinal)-start),oracle);
        }
        [Test] public void BaseAssaultMatchesOriginal()
        {
            float[] elapsed={-1,0,9.999f,10,11,float.NaN,float.PositiveInfinity};
            for(int flags=0;flags<128;flags++)foreach(float time in elapsed)foreach(int deadline in new[]{-1,0,10})
            {
                var f=new SkirmishBaseAssaultFacts{Playing=(flags&1)!=0,Paused=(flags&2)!=0,Surrender=(flags&4)!=0,PlayerDesignatedAlive=(flags&8)!=0,EnemyDesignatedAlive=(flags&16)!=0,ReplacementBarracksPresent=(flags&32)!=0,FieldArmyWiped=(flags&64)!=0,ElapsedSeconds=time,DeadlineSeconds=deadline};
                bool expected=SkirmishMissionOrchestrationOriginalOracle.TryEvaluate(f,out var eo,out var er);
                Assert.AreEqual(expected,SkirmishMissionOrchestrationKernel.TryEvaluateBaseAssault(f,out var ao,out var ar));Assert.AreEqual(eo,ao);Assert.AreEqual(er,ar);
                Assert.AreEqual(expected,SkirmishBaseAssaultObjectiveSystem.TryEvaluate(f,out ao,out ar));Assert.AreEqual(eo,ao);Assert.AreEqual(er,ar);
            }
        }
        [Test] public void CollectorClockMatchesOriginal()
        {
            var random=new System.Random(41731);
            for(int i=0;i<4096;i++)
            {
                double last=random.NextDouble()*1000, fraction=random.NextDouble();float elapsed=(float)(last+random.NextDouble());byte playing=(byte)(i%3),paused=(byte)((i/3)%3);
                if(i%11==0)last=-1;if(i%13==0)fraction=double.NaN;if(i%17==0)elapsed=float.PositiveInfinity;if(i%19==0)fraction=1;if(i%23==0)elapsed=(float)last-1;
                if(i%29==0){last=elapsed=10;fraction=.75;}
                var cursor=new SkirmishObjectiveCollectorClock{LastAuthoritativeSeconds=last,FractionalTicks=fraction};var clock=new SkirmishObjectiveClockComponent{ElapsedSeconds=elapsed,Playing=playing,Paused=paused};
                bool expected=SkirmishMissionOrchestrationOriginalOracle.TryProjectClock(cursor,clock,out var en,out var ed,out var ee);
                Assert.AreEqual(expected,SkirmishNativeObjectiveCollector.TryProjectClock(cursor,clock,out var an,out var ad,out var ae));
                Assert.AreEqual(ed,ad);Assert.AreEqual(ee,ae);Assert.AreEqual(BitConverter.DoubleToInt64Bits(en.LastAuthoritativeSeconds),BitConverter.DoubleToInt64Bits(an.LastAuthoritativeSeconds));Assert.AreEqual(BitConverter.DoubleToInt64Bits(en.FractionalTicks),BitConverter.DoubleToInt64Bits(an.FractionalTicks));
            }
            Assert.IsFalse(SkirmishMissionOrchestrationKernel.TryProjectCollectorClock(0,0,float.MaxValue,1,0,out _,out _,out _,out _));
        }
        [Test] public void ClockAndFreezeMatchNativePhases()
        {
            foreach(SkirmishSessionPhase phase in Enum.GetValues(typeof(SkirmishSessionPhase)))for(int legacy=0;legacy<2;legacy++)for(int pause=0;pause<2;pause++)
            {
                using var world=new World("OrchestrationClockParity");world.SetTime(new TimeData(11,.25f));var em=world.EntityManager;
                var session=em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),typeof(SkirmishResolvedSetupComponent));
                em.SetComponentData(session,new SkirmishExpandedSessionComponent{Phase=phase,IsLegacy=(byte)legacy});
                em.SetComponentData(session,new SkirmishObjectiveClockComponent{ElapsedSeconds=10,DeadlineSeconds=0,Playing=3,Paused=4});
                em.SetComponentData(session,new SkirmishResolvedSetupComponent{DeadlineSeconds=60});
                var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));em.SetComponentData(gameplay,new RuntimeGameplayStateComponent{SimulationActive=(byte)(pause==0?1:0)});
                world.GetOrCreateSystem<SkirmishObjectiveFactProjectionSystem>().Update(world.Unmanaged);
                var c=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                bool terminal=phase==SkirmishSessionPhase.Finished||phase==SkirmishSessionPhase.Failed||phase==SkirmishSessionPhase.Cleaning;
                Assert.AreEqual(legacy!=0?3:(phase==SkirmishSessionPhase.Playing?1:0),c.Playing);Assert.AreEqual(legacy!=0?4:(pause!=0||terminal?1:0),c.Paused);
                Assert.AreEqual(legacy==0&&phase==SkirmishSessionPhase.Playing&&pause==0?10.25f:10f,c.ElapsedSeconds);Assert.AreEqual(legacy==0?60:0,c.DeadlineSeconds);
                Assert.AreEqual(legacy==0&&!terminal,SkirmishMissionOrchestrationKernel.ShouldRefreshFacts(legacy!=0,phase));
                for(int term=0;term<2;term++)for(int frozen=0;frozen<2;frozen++)Assert.AreEqual(legacy!=0||term==0?SkirmishResultFreezeDecision.None:frozen!=0?SkirmishResultFreezeDecision.ProjectExisting:SkirmishResultFreezeDecision.CreateFrozen,SkirmishMissionOrchestrationKernel.DecideFreeze(legacy!=0,term!=0,frozen!=0));
            }
        }
        [Test] public void DispatchSurrenderAndStaging()
        {
            foreach(var kind in new[]{SkirmishObjectiveKind.FrontlineControl,SkirmishObjectiveKind.Breakthrough,SkirmishObjectiveKind.ConvoyEscort})
            {
                var f=default(SkirmishFrontlineState);var b=default(SkirmishBreakthroughState);var c=default(SkirmishConvoyState);
                Assert.IsTrue(SkirmishMissionOrchestrationKernel.TryDispatch(kind,true,ref f,ref b,ref c,default,default,default,out var removed,out var error));Assert.IsNull(error);Assert.IsEmpty(removed);
                Assert.IsTrue(SkirmishMissionOrchestrationKernel.TryGetOutcome(kind,f,b,c,out var outcome,out var reason));Assert.AreEqual(SkirmishOutcomeKind.Defeat,outcome);Assert.AreEqual(SkirmishEndReasonKind.Surrender,reason);
                Assert.IsTrue(SkirmishMissionOrchestrationKernel.TryDispatch(kind,false,ref f,ref b,ref c,default,default,default,out removed,out error));Assert.IsEmpty(removed);
            }
            var original=SkirmishFrontlineReducer.Create(SkirmishSizeId.Standard,"a","b","c");var staged=original;var bt=default(SkirmishBreakthroughState);var ce=default(SkirmishConvoyState);
            var facts=new SkirmishFrontlineTickFacts{Playing=true,OriginalBaseFactsPresent=true,PlayerOriginalBaseAlive=true,EnemyOriginalBaseAlive=true,SimulationDeltaTicks=TimeSpan.TicksPerSecond};
            Assert.IsTrue(SkirmishMissionOrchestrationKernel.TryDispatch(SkirmishObjectiveKind.FrontlineControl,false,ref staged,ref bt,ref ce,facts,default,default,out _,out _));Assert.AreEqual(0,original.ElapsedTicks);Assert.AreEqual(TimeSpan.TicksPerSecond,staged.ElapsedTicks);
            var invalid=original;facts.SimulationDeltaTicks=-1;Assert.Throws<ArgumentException>(()=>SkirmishMissionOrchestrationKernel.TryDispatch(SkirmishObjectiveKind.FrontlineControl,false,ref invalid,ref bt,ref ce,facts,default,default,out _,out _));Assert.AreEqual(0,invalid.ElapsedTicks);
            Assert.IsFalse(SkirmishMissionOrchestrationKernel.TryDispatch((SkirmishObjectiveKind)255,false,ref staged,ref bt,ref ce,default,default,default,out _,out var failure));Assert.AreEqual("Unsupported objective kind.",failure);
            Assert.IsFalse(SkirmishMissionOrchestrationKernel.TryGetOutcome((SkirmishObjectiveKind)255,staged,bt,ce,out _,out _));
        }
    }
}
