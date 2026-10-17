using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishPublicUnloadNativeTests
    {
        public static void RunFocusedValidation()
        {
            var lineage=new SkirmishPublicOriginalTransportProjectionTests();
            for(int i=0;i<5;i++)lineage.MountedOriginalRequiresPublicAndReciprocalOwnLineage(i,i==0);
            var tests=new SkirmishPublicUnloadNativeTests();
            for(int i=0;i<6;i++)tests.CompleteDismountIsSeparateFromAcceptedOrPartialWork(i);
            UnityEngine.Debug.Log("[SkirmishPublicUnloadNative] result=Passed controls=11");
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void CompleteDismountIsSeparateFromAcceptedOrPartialWork(int mutation)
        {
            using var world=new World("PublicUnloadProgressControl");var em=world.EntityManager;
            var attempt=new FixedString64Bytes("unload-progress");var session=em.CreateEntity();
            em.AddComponentData(session,new SkirmishExpandedSessionComponent{SessionId=attempt,DefinitionId=new FixedString64Bytes("unload-control"),
                ContentVersion=1,ObjectiveKind=SkirmishObjectiveKind.Breakthrough,Phase=SkirmishSessionPhase.Playing});
            var carrier=em.CreateEntity();em.AddComponentData(carrier,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=1,StableObjectId=new FixedString64Bytes("carrier")});
            em.AddComponentData(carrier,new Faction{Id=1});em.AddComponentData(carrier,new UnitHealth{Current=100,Max=100});var buffer=em.AddBuffer<UnitTransportPassengerElement>(carrier);
            var actors=new Entity[2];var nodes=new List<SkirmishPublicGoalNode>{Marker("a",SkirmishPublicGoalNodeKind.ExitCorridor),
                Marker("b",SkirmishPublicGoalNodeKind.ExitCorridor),Marker("exit",SkirmishPublicGoalNodeKind.EvacuationExit)};
            for(int i=0;i<2;i++)
            {
                var actor=em.CreateEntity();actors[i]=actor;
                em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=1,StableObjectId=new FixedString64Bytes("original."+i)});
                em.AddComponentData(actor,new Faction{Id=1});em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
                em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=7});
                bool mounted=mutation==1||mutation==3||mutation==2&&i==1;
                if(mounted){em.AddComponentData(actor,new UnitTransportPassenger{Transport=carrier});em.GetBuffer<UnitTransportPassengerElement>(carrier).Add(new UnitTransportPassengerElement{Passenger=actor});em.AddComponent<Disabled>(actor);}
                nodes.Add(new SkirmishPublicGoalNode{PublicId="original."+i,Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                    Provenance=SkirmishPublicGoalProvenance.OwnRoster,OwnerKnown=true,OwnerFaction=1,RevealedSubjectKind=SkirmishPublicSubjectKind.DesignatedRifle,
                    SubjectDisposition=SkirmishPublicSubjectDisposition.Live,Mounted=mounted});
            }
            if(mutation==1||mutation==3){em.AddComponentData(carrier,new UnitTransportRopeDisembarkRequest{TotalDropCount=2});em.AddComponentData(carrier,new UnitAirComponent());em.AddComponentData(carrier,new UnitAirMovement());}
            if(mutation==3){var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier);owner.SessionId=new FixedString64Bytes("foreign");em.SetComponentData(carrier,owner);}
            if(mutation==4){var replacement=em.Instantiate(actors[0]);em.DestroyEntity(actors[0]);}
            if(mutation==5)em.SetComponentData(actors[0],new UnitHealth{Current=0,Max=100});
            Assert.That(SkirmishPublicGoalSnapshot.TryCreate("unload-control",1,1,SkirmishObjectiveKind.Breakthrough,
                SkirmishPublicGoalPerspective.Evacuate,0,100,SkirmishOutcomeKind.None,
                new SkirmishPublicGoalScore{ProgressKnown=true,OriginalSubjects=12,Required=8,ExitStateKnown=true,ExitOpen=true},nodes.ToArray(),out var goal,out var error),Is.True,error);
            var row=new SkirmishOriginalUnloadPresentation{Carrier=carrier,CarrierId="carrier",GroupId=7,Actors=actors,Ids=new[]{"original.0","original.1"},RequestAccepted=true};
            bool valid=SkirmishOriginalUnloadPresentation.TryReadProgressFromPublicGoal(em,session,row,goal,out int complete,out int pending,out bool partial);
            Assert.That(valid,Is.EqualTo(mutation==0||mutation==1));
            if(mutation==0){Assert.That(complete,Is.EqualTo(2));Assert.That(pending,Is.Zero);}
            if(mutation==1){Assert.That(complete,Is.Zero);Assert.That(pending,Is.EqualTo(2));Assert.That(row.RequestAccepted,Is.True);}
            if(mutation==2){Assert.That(partial,Is.True);Assert.That(complete,Is.EqualTo(1));Assert.That(pending,Is.EqualTo(1));}
            Assert.That(row.RequestAccepted,Is.True,"Read-only measurement cannot rewrite actual backend acceptance.");
        }
        static SkirmishPublicGoalNode Marker(string id,SkirmishPublicGoalNodeKind kind)=>new SkirmishPublicGoalNode{
            PublicId=id,Kind=kind,Provenance=SkirmishPublicGoalProvenance.PublicObjective};
    }
}
