using System.Reflection;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Shell.Ecs;
using Game.UI.Runtime;
using Game.UI.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Tests.Editor
{
    // Native World controls. Offline compilation does not execute these tests.
    public sealed class SkirmishPublicObjectiveHudTests
    {
        public static void RunFocusedValidation()
        {
            var test=new SkirmishPublicObjectiveHudTests();
            for(int i=0;i<=6;i++)test.MoveProofRequiresEveryActualMember(i,i==0?7u:0u);
            test.MoveFeedbackRequiresTheExactNormalHudResultPublication();
            test.ParentPositionRequiresSameAttemptAndFaction(false,false,true);
            test.ParentPositionRequiresSameAttemptAndFaction(true,false,false);
            test.ParentPositionRequiresSameAttemptAndFaction(false,true,false);
            test.MarkerInsideCannotCreditAFormationOutside(false);
            test.MarkerInsideCannotCreditAFormationOutside(true);
            UnityEngine.Debug.Log("[SkirmishPublicObjectiveHud] result=Passed controls=13");
        }
        sealed class Fixture : System.IDisposable
        {
            public readonly World World=new World("PublicObjectiveHudControl");
            public EntityManager Em=>World.EntityManager;
            public readonly FixedString64Bytes Attempt=new FixedString64Bytes("hud-control");
            public readonly Entity Session,A,B;
            public Fixture()
            {
                Session=Em.CreateEntity();Em.AddComponentData(Session,new SkirmishExpandedSessionComponent {
                    SessionId=Attempt,Phase=SkirmishSessionPhase.Playing,ObjectiveKind=SkirmishObjectiveKind.FrontlineControl});
                A=Actor();B=Actor();
            }
            public Entity Actor()
            {
                var actor=Em.CreateEntity();
                Em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=Attempt,FactionId=1});
                Em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7});
                Em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
                Em.AddComponentData(actor,new Faction{Id=1});
                Em.AddComponentData(actor,new SelectedUnitTag());
                Em.AddComponentData(actor,new ManualMoveOrderTag());
                Em.AddComponentData(actor,new UnitTarget{Cell=new int2(12,13)});
                Em.AddComponentData(actor,LocalTransform.FromPosition(new float3(2,0,3)));
                return actor;
            }
            public uint Proof(int mutation)
            {
                using var selected=new NativeArray<Entity>(new[]{A,B},Allocator.Temp);
                using var accepted=new NativeList<Entity>(Allocator.Temp);
                using var goals=new NativeList<int2>(Allocator.Temp);
                accepted.Add(A);goals.Add(new int2(12,13));
                if(mutation!=2){accepted.Add(mutation==4?A:B);goals.Add(new int2(12,13));}
                if(mutation==1)Em.RemoveComponent<SelectedUnitTag>(B);
                if(mutation==3)Em.SetComponentData(B,new UnitTarget{Cell=new int2(99,13)});
                if(mutation==5){var own=Em.GetComponentData<SkirmishAttemptOwnedComponent>(B);own.SessionId=new FixedString64Bytes("foreign");Em.SetComponentData(B,own);}
                if(mutation==6){var state=Em.GetComponentData<SkirmishExpandedSessionComponent>(Session);state.Phase=SkirmishSessionPhase.Finished;Em.SetComponentData(Session,state);}
                return SkirmishPublicOrderFeedback.VerifyCompleteMoveGroup(Em,selected,accepted,goals);
            }
            public void Dispose()=>World.Dispose();
        }
        [TestCase(0,7u)] // Full living group, exact per-member native destinations.
        [TestCase(1,0u)] // Partial displayed selection.
        [TestCase(2,0u)] // Aggregate accepted, but one member rejected.
        [TestCase(3,0u)] // One actual destination differs from its accepted goal.
        [TestCase(4,0u)] // Duplicate accepted actor cannot replace another member.
        [TestCase(5,0u)] // Foreign-attempt member.
        [TestCase(6,0u)] // Terminal attempt cannot publish a new receipt.
        public void MoveProofRequiresEveryActualMember(int mutation,uint expected)
        {using var fixture=new Fixture();Assert.That(fixture.Proof(mutation),Is.EqualTo(expected));}

        [Test]
        public void MoveFeedbackRequiresTheExactNormalHudResultPublication()
        {
            using var f=new Fixture();
            f.Em.AddComponentData(f.Session,new SkirmishPublicOrderFeedbackComponent{
                Kind=RtsSelectionCommandIntentKind.Move,RequestId=12,Frame=100,GroupId=7,Accepted=1});
            SkirmishPublicOrderFeedback.MarkPresented(f.Em,RtsSelectionCommandIntentKind.Move,13,100);
            Assert.That(f.Em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(f.Session).Presented,Is.Zero);
            SkirmishPublicOrderFeedback.MarkPresented(f.Em,RtsSelectionCommandIntentKind.Move,12,101);
            Assert.That(f.Em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(f.Session).Presented,Is.Zero);
            SkirmishPublicOrderFeedback.MarkPresented(f.Em,RtsSelectionCommandIntentKind.Move,12,100);
            Assert.That(f.Em.GetComponentData<SkirmishPublicOrderFeedbackComponent>(f.Session).Presented,Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MarkerInsideCannotCreditAFormationOutside(bool memberOutside)
        {
            var nodes=new[]{
                new SkirmishPublicGoalNode{PublicId="destination",Kind=SkirmishPublicGoalNodeKind.DeliveryDestination,
                    Provenance=SkirmishPublicGoalProvenance.PublicObjective,PositionKnown=true,WorldX=100,WorldZ=100},
                new SkirmishPublicGoalNode{PublicId="own-truck",Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                    Provenance=SkirmishPublicGoalProvenance.OwnRoster,PositionKnown=true,OwnerKnown=true,OwnerFaction=1,
                    RevealedSubjectKind=SkirmishPublicSubjectKind.ObjectiveConvoyTruck,SubjectDisposition=SkirmishPublicSubjectDisposition.Live}};
            Assert.That(SkirmishPublicGoalSnapshot.TryCreate("public-hud-control",1,1,SkirmishObjectiveKind.ConvoyEscort,
                SkirmishPublicGoalPerspective.Escort,100,200,SkirmishOutcomeKind.None,
                new SkirmishPublicGoalScore{ProgressKnown=true,OriginalSubjects=3,Required=2},nodes,out var goal,out var error),Is.True,error);
            var model=new UiSkirmishPublicObjectiveModel{Goal=goal,GroundCellSize=1,
                Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=true,GroupId=7,Action=AriaObjectiveAction.Move,WorldKnown=true,World=UnityEngine.Vector3.zero},
                OwnGroups=new[]{new UiSkirmishPublicGroupRow{Policy=new SkirmishPublicPlayerGroup{GroupId=7},
                    CurrentMoveGoals=new[]{UnityEngine.Vector2.zero,new UnityEngine.Vector2(memberOutside?4:0,0)}}}};
            var method=typeof(SkirmishMatchView).GetMethod("ResolvePublicOrderGoal",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            Assert.That((string)method.Invoke(null,new object[]{model}),Is.EqualTo(memberOutside?null:"own-truck"));
        }

        [TestCase(false,false,true)]
        [TestCase(true,false,false)]
        [TestCase(false,true,false)]
        public void ParentPositionRequiresSameAttemptAndFaction(bool foreignAttempt,bool foreignFaction,bool expected)
        {
            using var f=new Fixture();var parent=f.Actor();f.Em.RemoveComponent<SelectedUnitTag>(parent);
            f.Em.SetComponentData(parent,LocalTransform.FromPosition(new float3(10,0,20)));
            var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(parent);
            if(foreignAttempt)own.SessionId=new FixedString64Bytes("foreign");
            if(foreignFaction){own.FactionId=2;f.Em.SetComponentData(parent,new Faction{Id=2});}
            f.Em.SetComponentData(parent,own);f.Em.AddComponentData(f.A,new Parent{Value=parent});
            var method=typeof(UiShellEcsGateway).GetMethod("TryPublicOwnPosition",BindingFlags.Static|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            object[] args={f.Em,f.A,f.Attempt,default(float3)};
            Assert.That((bool)method.Invoke(null,args),Is.EqualTo(expected));
            if(expected)Assert.That(math.distance((float3)args[3],new float3(12,0,23)),Is.LessThan(.001f));
        }
    }
}
