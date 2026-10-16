using System.Reflection;
using Game.Components;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    public sealed class SkirmishPublicRepairHudTests
    {
        public static void RunFocusedValidation()
        {
            var test=new SkirmishPublicRepairHudTests();
            for(int i=0;i<4;i++)test.PriceAndActorMustMatchShownQuote(i);
            for(int i=0;i<4;i++)test.SelectedVehicleMustBeSingleOwnLivingUnparented(i);
            for(int i=0;i<4;i++)test.RepairPreservesAcceptedWorkAndOriginalPublicLineage(i);
            for(int i=0;i<3;i++)test.OnlyTheRepairTargetContactCanAttributeAcceptance(i);
            UnityEngine.Debug.Log("[SkirmishPublicRepairHud] result=Passed controls=15");
        }
        static UiVehicleRepairModel Quote()=>new UiVehicleRepairModel{AttemptId="repair-hud",ActorPublicId="truck",GroupId=7,
            Health=50,MaximumHealth=100,Materials=20,FractionNumerator=1,FractionDenominator=4,DurationTicks=200000000};
        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void OnlyTheRepairTargetContactCanAttributeAcceptance(int mutation)
        {
            var shown=Quote();shown.StateMatchesIssued=shown.IssuedScreenKnown=true;
            shown.CompletedContactTargetId=mutation==1?99:22;shown.IssuedFrame=10;shown.CompletedContactFrame=mutation==2?9:10;
            Assert.That(SkirmishPresentedRepairPolicy.HasAcceptedContact(shown,22),Is.EqualTo(mutation==0));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void PriceAndActorMustMatchShownQuote(int mutation)
        {
            var shown=Quote();var actual=Quote();
            if(mutation==1)actual.Materials++;
            if(mutation==2)actual.Health--;
            if(mutation==3)actual.ActorPublicId="another-truck";
            Assert.That(SkirmishPresentedRepairPolicy.SameQuote(shown,actual),Is.EqualTo(mutation==0));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void SelectedVehicleMustBeSingleOwnLivingUnparented(int mutation)
        {
            using var world=new World("RepairHudSelection");var em=world.EntityManager;var actor=em.CreateEntity();
            em.AddComponentData(actor,new SelectedUnitTag());em.AddComponentData(actor,new UnitHealth{Current=50,Max=100});
            em.AddComponentData(actor,new UnitMovementBehavior{UsesVehicleMotion=1});
            em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=new FixedString64Bytes(mutation==3?"other":"repair-hud"),FactionId=1,StableObjectId=new FixedString64Bytes("truck")});
            em.AddComponentData(actor,new Faction{Id=1});em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7});
            em.AddComponentData(actor,LocalTransform.Identity);
            if(mutation==1)em.Instantiate(actor);
            if(mutation==2)em.AddComponentData(actor,new Parent{Value=em.CreateEntity()});
            var method=typeof(UiShellEcsGateway).GetMethod("TrySingleRepairActor",BindingFlags.Static|BindingFlags.NonPublic);
            object[] args={em,new FixedString64Bytes("repair-hud"),Entity.Null,0,false};
            Assert.That((bool)method.Invoke(null,args),Is.EqualTo(mutation==0));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void RepairPreservesAcceptedWorkAndOriginalPublicLineage(int mutation)
        {
            var nodes=new[]{new SkirmishPublicGoalNode{PublicId="destination",Kind=SkirmishPublicGoalNodeKind.DeliveryDestination,Provenance=SkirmishPublicGoalProvenance.PublicObjective},
                new SkirmishPublicGoalNode{PublicId=mutation==2?"different-truck":"truck",Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                    Provenance=SkirmishPublicGoalProvenance.OwnRoster,OwnerKnown=true,OwnerFaction=1,RevealedSubjectKind=SkirmishPublicSubjectKind.ObjectiveConvoyTruck,
                    SubjectDisposition=SkirmishPublicSubjectDisposition.Live}};
            Assert.That(SkirmishPublicGoalSnapshot.TryCreate("repair-hud-control",1,1,SkirmishObjectiveKind.ConvoyEscort,SkirmishPublicGoalPerspective.Escort,
                0,100,SkirmishOutcomeKind.None,new SkirmishPublicGoalScore{ProgressKnown=true,OriginalSubjects=3,Required=2},nodes,out var goal,out var error),Is.True,error);
            var shown=Quote();shown.Visible=shown.SingleOwnVehicle=shown.Eligible=true;
            var group=new SkirmishPublicPlayerGroup{GroupId=7,Own=true,Living=true,AcceptedOrderActive=mutation==1};
            if(mutation==3){shown.Working=true;group.AcceptedOrderActive=true;}
            Assert.That(SkirmishPresentedRepairPolicy.ShouldObserve(goal,new[]{group},shown),Is.EqualTo(mutation==0||mutation==3));
        }
    }
}
