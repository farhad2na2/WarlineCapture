#if UNITY_EDITOR
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativePublicGoalPresentationTests
    {
        [Test] public void ActualProviderBoundFrontlinePublishesThreePublicZones()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                Assert.IsTrue(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var view,out string error),error);
                Assert.AreEqual(SkirmishObjectiveKind.FrontlineControl,view.ObjectiveKind);
                Assert.AreEqual(SkirmishPublicGoalPerspective.Capture,view.Perspective);
                Assert.AreEqual(3,view.NodeCount);
                Assert.IsTrue(view.Score.TicketsKnown);
                for(int i=0;i<view.NodeCount;i++)
                {
                    Assert.AreEqual(SkirmishPublicGoalNodeKind.CaptureZone,view.Node(i).Kind);
                    Assert.AreEqual(SkirmishPublicGoalProvenance.PublicObjective,view.Node(i).Provenance);
                    Assert.IsFalse(view.Node(i).HealthKnown);
                }
            });
        }

        [Test] public void HiddenEnemyHealthAndPositionNeverEnterFrontlineView()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                Assert.IsTrue(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var before,out string error),error);
                using var actors=em.CreateEntityQuery(typeof(Faction),typeof(UnitHealth),typeof(LocalTransform));
                using var entities=actors.ToEntityArray(Unity.Collections.Allocator.Temp);
                bool changed=false;
                foreach(var entity in entities)
                {
                    if(em.GetComponentData<Faction>(entity).Id!=2)continue;
                    em.SetComponentData(entity,new UnitHealth{Current=7,Max=999});
                    em.SetComponentData(entity,LocalTransform.FromPosition(new Unity.Mathematics.float3(8765,4321,9876)));
                    changed=true;
                }
                Assert.IsTrue(changed,"Fixture must contain an actual opposing native actor.");
                Assert.IsTrue(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var after,out error),error);
                Assert.AreEqual(before.NodeCount,after.NodeCount);
                Assert.AreEqual(before.Score.OpposingTickets,after.Score.OpposingTickets);
                for(int i=0;i<before.NodeCount;i++)Assert.AreEqual(JsonUtility.ToJson(before.Node(i)),JsonUtility.ToJson(after.Node(i)));
            });
        }

        [Test] public void PublicTerminalTechnicalFailureIsPreserved()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                var state=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                state.Terminal=1;state.Outcome=SkirmishOutcomeKind.TechnicalFailure;em.SetComponentData(session,state);
                Assert.IsTrue(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var view,out string error),error);
                Assert.AreEqual(SkirmishOutcomeKind.TechnicalFailure,view.Outcome);
            });
        }

        [Test] public void MismatchedPublicObjectiveKindRejects()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                var state=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                state.Kind=SkirmishObjectiveKind.ConvoyEscort;em.SetComponentData(session,state);
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }

        [Test] public void MissingNativeAreasRejectInsteadOfFallingBackToBaseAssault()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                em.GetComponentObject<SkirmishNativeObjectiveRecord>(session).Areas=null;
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }

        [Test] public void MissingSessionRejectsWithoutInventingGoals()
        {
            using var world=new World("MissingPublicGoalSession");
            Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(world.EntityManager,Entity.Null,out var view,out _));
            Assert.IsNull(view);
        }
    }
}
#endif
