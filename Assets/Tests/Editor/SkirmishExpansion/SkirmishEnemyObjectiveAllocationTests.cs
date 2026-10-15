using System;
using System.Linq;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using A=Game.Runtime.SkirmishEnemyObjectiveAllocationUtilitySystemHelper;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyObjectiveAllocationTests
    {
        private static SkirmishPublicGoalNode Public(string id,SkirmishPublicGoalNodeKind kind,float x)=>new SkirmishPublicGoalNode
        {PublicId=id,Kind=kind,Provenance=SkirmishPublicGoalProvenance.PublicObjective,PositionKnown=true,WorldX=x,WorldZ=5,LastObservedTicks=10};
        private static SkirmishPublicGoalSnapshot Goal(SkirmishObjectiveKind kind,SkirmishPublicGoalNode[] nodes=null,SkirmishOutcomeKind outcome=SkirmishOutcomeKind.None,int observer=2)
        {
            var perspective=kind==SkirmishObjectiveKind.FrontlineControl?SkirmishPublicGoalPerspective.Capture:
                kind==SkirmishObjectiveKind.Breakthrough?SkirmishPublicGoalPerspective.DefendExits:SkirmishPublicGoalPerspective.Interdict;
            if(nodes==null)nodes=kind==SkirmishObjectiveKind.FrontlineControl?new[]{Public("a",SkirmishPublicGoalNodeKind.CaptureZone,10),Public("b",SkirmishPublicGoalNodeKind.CaptureZone,30),Public("c",SkirmishPublicGoalNodeKind.CaptureZone,50)}:
                kind==SkirmishObjectiveKind.Breakthrough?new[]{Public("a",SkirmishPublicGoalNodeKind.ExitCorridor,10),Public("b",SkirmishPublicGoalNodeKind.ExitCorridor,30),Public("exit",SkirmishPublicGoalNodeKind.EvacuationExit,50)}:
                new[]{Public("destination",SkirmishPublicGoalNodeKind.DeliveryDestination,50)};
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("objective.fixture",1,observer,kind,perspective,10,1000,outcome,default,nodes,out var goal,out string error),error);
            return goal;
        }
        private static A.Group[] Groups(int count)=>Enumerable.Range(1,count).Select(i=>new A.Group{Id=(uint)i,Category=SkirmishPopulationCategory.Infantry,Members=4,Supply=4,X=0,Z=5,Speed=4,Healthy=true,Commandable=true}).ToArray();
        private static A.PublicPatrolPoint[] Patrols()=>new[]{new A.PublicPatrolPoint{PublicRouteId="route.a",X=10,Z=5},new A.PublicPatrolPoint{PublicRouteId="route.b",X=30,Z=5}};
        private static A.Assignment[] Allocate(SkirmishPublicGoalSnapshot goal,A.Group[] groups,A.PublicPatrolPoint[] patrols=null)
        {Assert.IsTrue(A.TryAllocate(goal,groups,patrols,0,0,out var result,out string error),error);return result;}

        [Test] public void FrontlineRetainsTwoOwnedZonesAndDoesNotPileOntoThird()
        {
            var nodes=new[]{Public("a",SkirmishPublicGoalNodeKind.CaptureZone,10),Public("b",SkirmishPublicGoalNodeKind.CaptureZone,30),Public("c",SkirmishPublicGoalNodeKind.CaptureZone,50)};
            for(int i=0;i<3;i++){nodes[i].OwnerKnown=true;nodes[i].OwnerFaction=i<2?2:1;nodes[i].CapturePhase=SkirmishPublicCapturePhase.Held;}
            var result=Allocate(Goal(SkirmishObjectiveKind.FrontlineControl,nodes),Groups(6));
            Assert.That(result.Count(r=>r.PublicGoalId=="a"),Is.EqualTo(3));Assert.That(result.Count(r=>r.PublicGoalId=="b"),Is.EqualTo(3));
            Assert.IsFalse(result.Any(r=>r.PublicGoalId=="c"));
        }
        [Test] public void FrontlineNeutralizationNeverPretendsZoneAlreadyOwned()
        {
            var nodes=new[]{Public("a",SkirmishPublicGoalNodeKind.CaptureZone,10),Public("b",SkirmishPublicGoalNodeKind.CaptureZone,30),Public("c",SkirmishPublicGoalNodeKind.CaptureZone,50)};
            nodes[0].OwnerKnown=true;nodes[0].OwnerFaction=2;nodes[0].CapturePhase=SkirmishPublicCapturePhase.Held;
            nodes[1].OwnerKnown=true;nodes[1].OwnerFaction=1;nodes[1].CapturingFaction=2;nodes[1].CaptureProgressKnown=true;nodes[1].CaptureFraction=.9f;nodes[1].CapturePhase=SkirmishPublicCapturePhase.Neutralizing;
            var result=Allocate(Goal(SkirmishObjectiveKind.FrontlineControl,nodes),Groups(2));
            Assert.That(result.Select(r=>r.PublicGoalId),Is.EquivalentTo(new[]{"a","b"}));
        }
        [Test] public void VehiclesScreenButCannotSubstituteForCaptureInfantry()
        {
            var groups=Groups(3);groups[2].Category=SkirmishPopulationCategory.Ground;
            var result=Allocate(Goal(SkirmishObjectiveKind.FrontlineControl),groups);
            Assert.AreEqual(2,result.Count(r=>r.Task==A.Duty.Capture));Assert.AreEqual(A.Duty.Screen,result.Single(r=>r.GroupId==3).Task);
        }
        [Test] public void BreakthroughWholeSquadFortyFortyTwentyCoverageUsesPublicCorridors()
        {
            var result=Allocate(Goal(SkirmishObjectiveKind.Breakthrough),Groups(10));
            Assert.AreEqual(4,result.Count(r=>r.PublicGoalId=="a"));Assert.AreEqual(4,result.Count(r=>r.PublicGoalId=="b"));
            Assert.AreEqual(2,result.Count(r=>r.Task==A.Duty.Reserve));Assert.IsFalse(result.Any(r=>r.PublicGoalId=="exit"));
        }
        [Test] public void UnobservedAndStalePositionsNeverBecomeInterceptDestinations()
        {
            var nodes=new[]{Public("a",SkirmishPublicGoalNodeKind.ExitCorridor,10),Public("b",SkirmishPublicGoalNodeKind.ExitCorridor,30),Public("exit",SkirmishPublicGoalNodeKind.EvacuationExit,50),
                new SkirmishPublicGoalNode{PublicId="seen",Kind=SkirmishPublicGoalNodeKind.ObservedOpposingSubject,Provenance=SkirmishPublicGoalProvenance.LegallyObservedContact,PositionKnown=true,WorldX=999,WorldZ=999,OwnerKnown=true,OwnerFaction=1,LastObservedTicks=9,SubjectDisposition=SkirmishPublicSubjectDisposition.Live}};
            var result=Allocate(Goal(SkirmishObjectiveKind.Breakthrough,nodes),Groups(10));Assert.IsFalse(result.Any(r=>r.Task==A.Duty.Intercept||r.X==999));
        }
        [Test] public void ConvoyPatrolRequiresTwoDistinctAuthoredRoutePoints()
        {
            var goal=Goal(SkirmishObjectiveKind.ConvoyEscort);
            Assert.IsFalse(A.TryAllocate(goal,Groups(3),null,0,0,out _,out _));
            var duplicate=Patrols();duplicate[1]=duplicate[0];Assert.IsFalse(A.TryAllocate(goal,Groups(3),duplicate,0,0,out _,out _));
            var result=Allocate(goal,Groups(10),Patrols());Assert.AreEqual(4,result.Count(r=>r.PublicGoalId=="route.a"));Assert.AreEqual(4,result.Count(r=>r.PublicGoalId=="route.b"));
        }
        [Test] public void VisibleUnrevealedContactCanBeInterceptedWithoutInventedDesignation()
        {
            var contact=new SkirmishPublicGoalNode{PublicId="seen",Kind=SkirmishPublicGoalNodeKind.ObservedOpposingSubject,Provenance=SkirmishPublicGoalProvenance.LegallyObservedContact,
                PositionKnown=true,CurrentlyVisible=true,WorldX=12,WorldZ=5,OwnerKnown=true,OwnerFaction=1,LastObservedTicks=10,SubjectDisposition=SkirmishPublicSubjectDisposition.Live};
            var goal=Goal(SkirmishObjectiveKind.ConvoyEscort,new[]{Public("destination",SkirmishPublicGoalNodeKind.DeliveryDestination,50),contact});
            var result=Allocate(goal,Groups(10),Patrols());var intercept=result.Where(r=>r.Task==A.Duty.Intercept).ToArray();
            Assert.AreEqual(2,intercept.Length);Assert.IsTrue(intercept.All(r=>r.PublicGoalId=="seen"&&r.Score<=40));
        }
        [Test] public void TerminalAndWrongPerspectiveObserversNeverAllocate()
        {
            Assert.IsFalse(A.TryAllocate(Goal(SkirmishObjectiveKind.ConvoyEscort,outcome:SkirmishOutcomeKind.Victory),Groups(3),Patrols(),0,0,out _,out _));
            Assert.IsFalse(A.TryAllocate(Goal(SkirmishObjectiveKind.ConvoyEscort,observer:1),Groups(3),Patrols(),0,0,out _,out _));
        }
        [Test] public void InvalidOwnFactsRejectWholeAllocationBeforeOutput()
        {
            var groups=Groups(3);groups[1].Id=groups[0].Id;Assert.IsFalse(A.TryAllocate(Goal(SkirmishObjectiveKind.Breakthrough),groups,null,0,0,out var result,out _));Assert.IsNull(result);
            groups=Groups(3);groups[0].X=float.NaN;Assert.IsFalse(A.TryAllocate(Goal(SkirmishObjectiveKind.Breakthrough),groups,null,0,0,out result,out _));Assert.IsNull(result);
            groups=Groups(3);groups[0].Supply=int.MaxValue;
            Assert.IsFalse(A.TryAllocate(Goal(SkirmishObjectiveKind.Breakthrough),groups,null,0,0,out result,out _));Assert.IsNull(result);
        }
        [Test] public void StableWholeGroupsAndUnhealthyRecoveryAreOrderIndependent()
        {
            var groups=Groups(5);groups[4].Healthy=false;
            var first=Allocate(Goal(SkirmishObjectiveKind.Breakthrough),groups);Array.Reverse(groups);
            var second=Allocate(Goal(SkirmishObjectiveKind.Breakthrough),groups);
            CollectionAssert.AreEqual(first.Select(r=>r.GroupId+":"+r.PublicGoalId).ToArray(),second.Select(r=>r.GroupId+":"+r.PublicGoalId).ToArray());
            Assert.AreEqual(A.Duty.Reserve,first.Single(r=>r.GroupId==5).Task);
        }
        [Test] public void VisibleInterceptUsesSharedHysteresisAndDropsIllegalOldContact()
        {
            var old=new SkirmishPublicGoalNode{PublicId="old",Kind=SkirmishPublicGoalNodeKind.ObservedOpposingSubject,Provenance=SkirmishPublicGoalProvenance.LegallyObservedContact,
                PositionKnown=true,CurrentlyVisible=true,WorldX=40,WorldZ=5,OwnerKnown=true,OwnerFaction=1,LastObservedTicks=10,SubjectDisposition=SkirmishPublicSubjectDisposition.Live};
            var alternative=old;alternative.PublicId="alternative";alternative.WorldX=8;
            var nodes=new[]{Public("destination",SkirmishPublicGoalNodeKind.DeliveryDestination,50),old,alternative};
            var previous=new[]{new A.Assignment{GroupId=3,Task=A.Duty.Intercept,PublicGoalId="old"}};
            Assert.IsTrue(A.TryAllocate(Goal(SkirmishObjectiveKind.ConvoyEscort,nodes),Groups(3),Patrols(),0,0,out var result,out _,previous));
            Assert.AreEqual("old",result.Single(r=>r.GroupId==3).PublicGoalId,"Eight score points cannot defeat the fifteen-point assignment margin.");
            nodes[1].CurrentlyVisible=false;nodes[1].LastObservedTicks=9;
            Assert.IsTrue(A.TryAllocate(Goal(SkirmishObjectiveKind.ConvoyEscort,nodes),Groups(3),Patrols(),0,0,out result,out _,previous));
            Assert.AreEqual("alternative",result.Single(r=>r.GroupId==3).PublicGoalId,"Stale contacts cannot retain current-target authority.");
        }
    }
}
