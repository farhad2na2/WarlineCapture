#if UNITY_EDITOR
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishMissionLogicalBindingSubjectFixtureTests
    {
        [Test] public void BreakthroughUsesActualCompiledTwelveOriginalsAndRealProvider()
        {
            SkirmishMissionLogicalBindingSubjectTestFixture.WithActiveBreakthrough((em,session)=>
                Verify(em,session,SkirmishObjectiveKind.Breakthrough,12));
        }
        [Test] public void ConvoyUsesActualCompiledThreeSpecializedOriginalsAndRealProvider()
        {
            SkirmishMissionLogicalBindingSubjectTestFixture.WithActiveConvoy((em,session)=>
                Verify(em,session,SkirmishObjectiveKind.ConvoyEscort,3));
        }
        private static void Verify(EntityManager em,Entity session,SkirmishObjectiveKind kind,int count)
        {
            Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled));
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            Assert.AreEqual(kind,setup.ObjectiveKind);
            Assert.IsTrue(SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var plan,out string error),error);
            Assert.AreEqual(count,plan.Length);
            var owner=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            Assert.AreSame(compiled.Areas,owner.Areas);
            Assert.IsTrue(owner.Collector.TryExportCheckpointRows(em,session,out var rows,out error),error);
            Assert.AreEqual(count,rows.Length);
            foreach(var row in rows)
            {
                Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Live,row.Disposition);
                Assert.AreEqual(1,row.Faction);Assert.AreEqual(row.StableOriginalId,row.ObjectiveStableId);
                Assert.IsTrue(System.Array.Exists(plan,p=>p.StableActorId==row.StableOriginalId));
            }
            using var query=em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent),typeof(SkirmishUnitRoleComponent));
            using var actors=query.ToEntityArray(Allocator.Temp);Assert.AreEqual(count,actors.Length);
            if(kind==SkirmishObjectiveKind.ConvoyEscort)
            {
                // Real provider compilation must include player-only objective trucks.
                // Preserve every role/elevation/PNG requirement; never invent enemy CE actors.
                Assert.IsTrue(compiled.TryReadFraming(SkirmishRoleKind.ConvoyLogic,out var convoyFraming));
                Assert.IsTrue(convoyFraming.ObservePlayer);
                Assert.IsFalse(convoyFraming.ObserveEnemy);
                var framing=compiled.ReadFraming();
                var framingVariant=new SkirmishMissionLogicalVariant {Framing=framing,
                    OpeningFocusRole=compiled.OpeningFocusRole,
                    OpeningFocusStableActorId=compiled.OpeningFocusStableActorId,
                    OpeningFocusSmoothSeconds=compiled.OpeningFocusSmoothSeconds};
                Assert.IsTrue(SkirmishMissionLogicalBindingCompiler.ValidateFraming(framingVariant,setup,out error),error);
                int convoyIndex=System.Array.FindIndex(framing,row=>row.Role==SkirmishRoleKind.ConvoyLogic);
                framing[convoyIndex].ObserveEnemy=true;
                Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(framingVariant,setup,out error),
                    "Enemy objective-truck framing must remain invalid.");
                Assert.AreEqual(3,setup.ObjectiveSupportExtraPlayer);
                Assert.IsTrue(SkirmishConvoySpawnHooks.TryResolveDefinition(setup,out var definition,out error),error);
                foreach(var actor in actors)Assert.IsTrue(SkirmishConvoyTruckRuntime.ValidateSpecialized(em,actor,definition,out error),error);
            }
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishMissionLogicalBindingSubjectFixtureTests();
            tests.BreakthroughUsesActualCompiledTwelveOriginalsAndRealProvider();
            tests.ConvoyUsesActualCompiledThreeSpecializedOriginalsAndRealProvider();
            Debug.Log("[SkirmishMissionLogicalBindingSubjectFixtureTests] result=Passed tests=2 nativeMissionReadiness=Pending");
        }
    }
}
#endif
