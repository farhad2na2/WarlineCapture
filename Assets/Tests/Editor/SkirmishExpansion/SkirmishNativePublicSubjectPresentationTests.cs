#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativePublicSubjectPresentationTests
    {
        static void With(SkirmishObjectiveKind kind,Action<EntityManager,Entity> assertion)
        {
            if(kind==SkirmishObjectiveKind.Breakthrough)SkirmishMissionLogicalBindingSubjectTestFixture.WithActiveBreakthrough(assertion);
            else SkirmishMissionLogicalBindingSubjectTestFixture.WithActiveConvoy(assertion);
        }
        static Entity FirstOriginal(EntityManager em,Entity session)
        {
            string id=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session).Collector.OriginalStableIds()[0];
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishObjectiveRoleComponent));
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
                if(em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId.ToString()==id)return actor;
            throw new InvalidOperationException("Fixture original actor missing");
        }
        static SkirmishPublicGoalSnapshot Read(EntityManager em,Entity session)
        {
            Assert.IsTrue(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out var view,out string error),error);
            return view;
        }
        static void MarkRemoved(EntityManager em,Entity session,SkirmishObjectiveKind kind,bool lost)
        {
            var record=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            string first=record.Collector.OriginalStableIds()[0];
            if(kind==SkirmishObjectiveKind.Breakthrough)
            {
                for(int i=0;i<record.Breakthrough.OriginalSubjects.Length;i++)
                    if(record.Breakthrough.OriginalSubjects[i].StableOriginalId==first)
                        record.Breakthrough.OriginalSubjects[i].Status=lost?SkirmishBreakthroughSubjectStatus.Dead:SkirmishBreakthroughSubjectStatus.Evacuated;
            }
            else
            {
                for(int i=0;i<record.Convoy.OriginalTrucks.Length;i++)
                    if(record.Convoy.OriginalTrucks[i].StableOriginalId==first)
                        record.Convoy.OriginalTrucks[i].Status=lost?SkirmishConvoySubjectStatus.Dead:SkirmishConvoySubjectStatus.Delivered;
            }
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void ActualSealedOwnOriginalsArePresentedWithFixedThresholds(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                var view=Read(em,session);int subjects=0;
                for(int i=0;i<view.NodeCount;i++)if(view.Node(i).Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject)
                {
                    subjects++;Assert.IsTrue(view.Node(i).HealthKnown);
                    Assert.AreEqual(SkirmishPublicHealthProvenance.OwnRoster,view.Node(i).HealthProvenance);
                }
                Assert.AreEqual(kind==SkirmishObjectiveKind.Breakthrough?12:3,subjects);
                Assert.AreEqual(subjects,view.Score.OriginalSubjects);
                Assert.AreEqual(kind==SkirmishObjectiveKind.Breakthrough?8:2,view.Score.Required);
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void ObjectiveRoleIdentitySubstitutionRejects(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);
                var role=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);role.StableObjectId="unsealed.replacement";em.SetComponentData(actor,role);
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void SameIdReplacementEntityCannotBecomeASealedOriginal(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var original=FirstOriginal(em,session);
                var replacement=em.Instantiate(original);em.DestroyEntity(original);
                Assert.IsFalse(em.GetComponentObject<SkirmishNativeObjectiveRecord>(session).Collector.IsOriginalEntity(replacement));
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void PopulationCategorySubstitutionRejects(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);role.Category=SkirmishPopulationCategory.Air;em.SetComponentData(actor,role);
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void ForeignParentCannotDiscloseItsTransformThroughOwnRoster(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);
                var foreign=em.CreateEntity(typeof(Faction),typeof(LocalTransform));
                em.SetComponentData(foreign,new Faction{Id=2});em.SetComponentData(foreign,LocalTransform.FromPosition(new float3(8765,4321,9876)));
                em.AddComponentData(actor,new Parent{Value=foreign});
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void OwnParentWorldPositionIsPresentedWithoutEnemyKnowledge(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);string id=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId.ToString();
                var parent=em.CreateEntity(typeof(Faction),typeof(LocalTransform),typeof(SkirmishAttemptOwnedComponent));
                var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                em.SetComponentData(parent,new Faction{Id=1});em.SetComponentData(parent,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=1,StableObjectId="own.parent"});
                em.SetComponentData(parent,LocalTransform.FromPosition(new float3(10,0,20)));
                em.SetComponentData(actor,LocalTransform.FromPosition(new float3(2,0,3)));em.AddComponentData(actor,new Parent{Value=parent});
                var view=Read(em,session);bool found=false;
                for(int i=0;i<view.NodeCount;i++)if(view.Node(i).PublicId==id){found=true;Assert.AreEqual(12,view.Node(i).WorldX);Assert.AreEqual(23,view.Node(i).WorldZ);}
                Assert.IsTrue(found);
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void LostOriginalRetainsPublicTombstoneAndRevalidatesIdentity(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);MarkRemoved(em,session,kind,true);
                var health=em.GetComponentData<UnitHealth>(actor);health.Current=0;em.SetComponentData(actor,health);
                Assert.AreEqual(1,Read(em,session).Score.SubjectsLost);
                var role=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);role.StableObjectId="wrong.dead.actor";em.SetComponentData(actor,role);
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
            });
        }
        [TestCase(SkirmishObjectiveKind.Breakthrough)][TestCase(SkirmishObjectiveKind.ConvoyEscort)]
        public void CompletedOriginalMustActuallyBeGone(SkirmishObjectiveKind kind)
        {
            With(kind,(em,session)=>
            {
                Read(em,session);var actor=FirstOriginal(em,session);MarkRemoved(em,session,kind,false);
                Assert.IsFalse(SkirmishNativePublicGoalPresentation.TryReadPlayer(em,session,out _,out _));
                em.DestroyEntity(actor);Assert.AreEqual(1,Read(em,session).Score.Completed);
            });
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishNativePublicSubjectPresentationTests();
            foreach(var kind in new[]{SkirmishObjectiveKind.Breakthrough,SkirmishObjectiveKind.ConvoyEscort})
            {
                tests.ActualSealedOwnOriginalsArePresentedWithFixedThresholds(kind);
                tests.ObjectiveRoleIdentitySubstitutionRejects(kind);
                tests.SameIdReplacementEntityCannotBecomeASealedOriginal(kind);
                tests.PopulationCategorySubstitutionRejects(kind);
                tests.ForeignParentCannotDiscloseItsTransformThroughOwnRoster(kind);
                tests.OwnParentWorldPositionIsPresentedWithoutEnemyKnowledge(kind);
                tests.LostOriginalRetainsPublicTombstoneAndRevalidatesIdentity(kind);
                tests.CompletedOriginalMustActuallyBeGone(kind);
            }
            Debug.Log("[SkirmishNativePublicSubjectPresentationTests] result=Passed focusedTests=16 normalMissionReadiness=Pending");
        }
    }
}
#endif
