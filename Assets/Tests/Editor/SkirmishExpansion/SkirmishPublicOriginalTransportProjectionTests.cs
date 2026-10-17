using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishPublicOriginalTransportProjectionTests
    {
        [TestCase(0,true)]
        [TestCase(1,false)]
        [TestCase(2,false)]
        [TestCase(3,false)]
        [TestCase(4,false)]
        public void MountedOriginalRequiresPublicAndReciprocalOwnLineage(int mutation,bool expected)
        {
            using var world=new World("PublicTransportProjectionControl");var em=world.EntityManager;
            var attempt=new FixedString64Bytes("own-transport");
            var session=em.CreateEntity();em.AddComponentData(session,new SkirmishExpandedSessionComponent{
                SessionId=attempt,DefinitionId=new FixedString64Bytes("transport-control"),ContentVersion=1,
                ObjectiveKind=SkirmishObjectiveKind.Breakthrough,Phase=SkirmishSessionPhase.Playing});
            var carrier=em.CreateEntity();em.AddComponentData(carrier,new SkirmishAttemptOwnedComponent{
                SessionId=mutation==1?new FixedString64Bytes("foreign"):attempt,StableObjectId=new FixedString64Bytes("carrier"),FactionId=1});
            em.AddComponentData(carrier,new Faction{Id=1});em.AddComponentData(carrier,new UnitHealth{Current=100,Max=100});
            em.AddComponentData(carrier,new SkirmishArmyGroupMembershipComponent{GroupId=9});
            em.AddBuffer<SkirmishArmyGroupRecord>(session).Add(new SkirmishArmyGroupRecord{GroupId=9,FactionId=1,AliveCount=1});
            var passenger=em.CreateEntity();em.AddComponentData(passenger,new SkirmishAttemptOwnedComponent{
                SessionId=attempt,StableObjectId=new FixedString64Bytes("original"),FactionId=1});
            em.AddComponentData(passenger,new SkirmishArmyGroupMembershipComponent{GroupId=7});
            em.AddComponentData(passenger,new Faction{Id=1});em.AddComponentData(passenger,new UnitHealth{Current=100,Max=100});
            em.AddComponentData(passenger,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Rifle});
            em.AddComponentData(passenger,new UnitTransportPassenger{Transport=carrier});em.AddComponent<Disabled>(passenger);
            var passengers=em.AddBuffer<UnitTransportPassengerElement>(carrier);
            if(mutation!=2)passengers.Add(new UnitTransportPassengerElement{Passenger=passenger});
            if(mutation==3)passengers.Add(new UnitTransportPassengerElement{Passenger=passenger});
            var nodes=new[]{Marker("a",SkirmishPublicGoalNodeKind.ExitCorridor),Marker("b",SkirmishPublicGoalNodeKind.ExitCorridor),
                Marker("exit",SkirmishPublicGoalNodeKind.EvacuationExit),new SkirmishPublicGoalNode{
                    PublicId="original",Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                    Provenance=SkirmishPublicGoalProvenance.OwnRoster,OwnerKnown=true,OwnerFaction=1,
                    RevealedSubjectKind=SkirmishPublicSubjectKind.DesignatedRifle,SubjectDisposition=SkirmishPublicSubjectDisposition.Live,
                    Mounted=mutation!=4}};
            Assert.That(SkirmishPublicGoalSnapshot.TryCreate("transport-control",1,1,SkirmishObjectiveKind.Breakthrough,
                SkirmishPublicGoalPerspective.Evacuate,0,100,SkirmishOutcomeKind.None,
                new SkirmishPublicGoalScore{ProgressKnown=true,OriginalSubjects=12,Required=8,ExitStateKnown=true},nodes,out var goal,out var error),Is.True,error);
            Assert.That(SkirmishPublicOriginalTransportProjection.TryRead(em,session,goal,7,out var result),Is.EqualTo(expected));
            if(expected)
            {
                Assert.That(result.CarrierGroupId,Is.EqualTo(9));Assert.That(result.OriginalGroupId,Is.EqualTo(7));
                Assert.That(result.CarrierPublicId,Is.EqualTo("carrier"));Assert.That(result.CarrierOriginalIds,Is.EqualTo(new[]{"original"}));
                Assert.That(em.HasComponent<UnitTransportPassenger>(passenger),Is.True,"Projection must never unload.");
                Assert.That(em.HasComponent<Disabled>(passenger),Is.True,"Projection must never reveal or enable an actor.");
            }
        }
        static SkirmishPublicGoalNode Marker(string id,SkirmishPublicGoalNodeKind kind)=>new SkirmishPublicGoalNode{
            PublicId=id,Kind=kind,Provenance=SkirmishPublicGoalProvenance.PublicObjective};
    }
}
