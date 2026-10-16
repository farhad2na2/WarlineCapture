using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class AriaObjectiveSupportTests
    {
        static AriaSkirmishObservation View()=>new AriaSkirmishObservation
        {
            Frame=10,Time=1000,Active=true,ExpandedSession=true,ExpandedRosterRevision=7,
            PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Presented=true,Supported=true,
                PresentationEpoch=1,GroupId=11,PublicKey=22,Action=AriaObjectiveAction.Move},
            ObjectiveSupport=new AriaObjectiveSupportObservation{Presented=true,CaptureMajority=true,
                Perspective=2,SimulationSeconds=10,DismountedCombatGroups=2},
            LogisticsTruckCommitted=true,TankersCommitted=2
        };
        static AriaSkirmishPlanComponent Plan()=>new AriaSkirmishPlanComponent
        {ObjectiveTouch=new AriaObjectiveTouchPlan{Stage=4,GroupId=11,PublicKey=22,Action=AriaObjectiveAction.Move,PresentationEpoch=1}};
        static AriaPlaySessionComponent Touch()=>new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};
        static AriaTouchTarget Target(int id)=>new AriaTouchTarget{Available=true,Id=id};
        static bool Step(in AriaSkirmishObservation view,ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch,out AriaPlayObservationComponent output)
        {output=default;return AriaSkirmishPlanSystem.StepPublicObjectiveSupport(view,ref plan,ref touch,ref output);}

        [Test]public void CaptureBuysSecondGroupWhileEscortDoesNotInventCaptureNeed()
        {
            var v=View();v.ObjectiveSupport.DismountedCombatGroups=1;v.CanAffordRifle=true;
            Assert.AreEqual(AriaObjectiveSupportAction.Rifle,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
            v.ObjectiveSupport.CaptureMajority=false;v.ObjectiveSupport.Perspective=5;
            Assert.AreEqual(AriaObjectiveSupportAction.None,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
        }
        [Test]public void RecruitsThroughPresentedControlWithoutChangingObjectiveOrder()
        {
            var v=View();v.ObjectiveSupport.DismountedCombatGroups=1;v.CanAffordRifle=true;v.Recruit=Target(91);
            var p=Plan();var t=Touch();Assert.IsTrue(Step(v,ref p,ref t,out var output));
            Assert.AreEqual(91,output.TargetId);Assert.AreEqual(11,p.ObjectiveTouch.GroupId);
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
        }
        [Test]public void CriticalRiskAndPendingObjectiveContactPreemptSupport()
        {
            for(int mode=0;mode<2;mode++)
            {
                var v=View();v.CanAffordAntiAir=true;v.VisibleHostileAir=1;v.RecruitAntiAir=Target(55);
                var p=Plan();var t=Touch();if(mode==0)v.ObjectiveSupport.CriticalPublicRisk=true;else p.ObjectiveTouch.CommandPending=true;
                Assert.IsFalse(Step(v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);
            }
        }
        [Test]public void CompletedGestureCannotCertifyPurchase()
        {
            var v=View();v.VisibleHostileAir=1;v.CanAffordAntiAir=true;v.RecruitAntiAir=Target(55);
            var p=Plan();var t=Touch();Assert.IsTrue(Step(v,ref p,ref t,out _));
            v.Frame++;v.ObjectiveSupport.SimulationSeconds++;t.Actions++;t.TargetId=55;
            Assert.IsTrue(Step(v,ref p,ref t,out _));Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
            Assert.AreEqual(0,t.LastObjectiveProgressAt);
        }
        [Test]public void RealCommittedRoleClosesDrawerAndResumesObjective()
        {
            var v=View();v.VisibleHostileAir=1;v.CanAffordAntiAir=true;v.RecruitAntiAir=Target(55);
            var p=Plan();var t=Touch();Step(v,ref p,ref t,out _);
            v.Frame++;v.AntiAirCommitted=true;v.DrawerOpen=true;v.CloseDrawer=Target(71);
            Assert.IsTrue(Step(v,ref p,ref t,out var output));Assert.AreEqual(71,output.TargetId);
            Assert.IsTrue(p.ObjectiveSupport.ObservedAcceptance);
            v.Frame++;v.DrawerOpen=false;Assert.IsFalse(Step(v,ref p,ref t,out _));
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [Test]public void ReadinessNeedsActualPublicQueueOrStageChange()
        {
            var v=View();v.AirProfile=true;v.CanUpgradeReadiness=true;v.UpgradeReadiness=Target(17);
            v.ObjectiveSupport.ResearchVisible=true;var p=Plan();var t=Touch();Step(v,ref p,ref t,out _);
            v.Frame++;v.CanUpgradeReadiness=false;Assert.IsTrue(Step(v,ref p,ref t,out _));
            Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
            v.Frame++;v.ObjectiveSupport.ResearchInProgress=true;v.ObjectiveSupport.ResearchId=9;
            Step(v,ref p,ref t,out _);Assert.IsTrue(p.ObjectiveSupport.ObservedAcceptance);
        }
        [Test]public void GroundProfileCannotSelectOffensiveAirOrResearchForIt()
        {
            var v=View();v.AirProfile=false;v.AirQueueOffered=true;v.CanUpgradeReadiness=true;
            v.ObjectiveSupport.ResearchVisible=true;
            Assert.AreEqual(AriaObjectiveSupportAction.None,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
        }
        [Test]public void PauseDoesNotExpireSupportFromWallClock()
        {
            var v=View();v.CanAffordAntiAir=true;v.VisibleHostileAir=1;v.RecruitAntiAir=Target(55);
            var p=Plan();var t=Touch();Step(v,ref p,ref t,out _);float deadline=p.ObjectiveSupport.Deadline;
            v.Frame++;v.Time+=10000;v.PublicObjective.Paused=true;
            Step(v,ref p,ref t,out var output);Assert.AreEqual(deadline,p.ObjectiveSupport.Deadline);Assert.AreEqual(0,output.TargetId);
        }
        [Test]public void StopManualDoesNotCancelAcceptedOrders()
        {
            var v=View();var p=Plan();p.ObjectiveSupport.Action=AriaObjectiveSupportAction.Helipad;
            var t=Touch();t.Phase=AriaPlayPhase.Manual;v.PlacementOpen=true;v.PlacementCancel=Target(83);
            Assert.IsFalse(Step(v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);Assert.AreEqual(AriaObjectiveSupportAction.None,p.ObjectiveSupport.Action);
        }
        [Test]public void ChangedGoalCancelsOnlyOwnedUnconfirmedPlacementNormally()
        {
            var v=View();var p=Plan();p.ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,Action=AriaObjectiveSupportAction.Helipad};
            v.PublicObjective.PublicKey=33;v.PlacementOpen=true;v.PlacementCancel=Target(83);var t=Touch();
            Assert.IsTrue(Step(v,ref p,ref t,out var output));Assert.AreEqual(83,output.TargetId);
            Assert.AreEqual(22,p.ObjectiveTouch.PublicKey);
        }
        [Test]public void RosterReplacementAbortsSingleAircraftServiceWithoutSuccess()
        {
            var v=View();v.OwnAttackAirLive=1;var p=Plan();var t=Touch();
            p.ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,Action=AriaObjectiveSupportAction.ReturnAircraft,
                BaselineRosterRevision=6,Deadline=200};
            Assert.IsFalse(Step(v,ref p,ref t,out _));Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
        }
        [Test]public void LandedCountAloneDoesNotProveReturnContact()
        {
            var v=View();v.OwnAttackAirLive=1;v.OwnAttackAirLanded=1;v.OwnAirFuel=50;
            var p=Plan();var t=Touch();p.ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,
                Action=AriaObjectiveSupportAction.ReturnAircraft,BaselineRosterRevision=7,Deadline=200};
            Step(v,ref p,ref t,out _);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
        }
        [Test]public void RealPlannerDispatchesSupportBeforePreservingAcceptedObjective()
        {
            var v=View();v.VisibleHostileAir=1;v.CanAffordAntiAir=true;v.RecruitAntiAir=Target(55);
            var p=Plan();var t=Touch();var output=new AriaPlayObservationComponent();
            AriaSkirmishPlanSystem.Step(v,ref p,ref t,ref output);
            Assert.AreEqual(55,output.TargetId);Assert.AreEqual(v.Frame,output.Frame);
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);Assert.AreEqual(11,p.ObjectiveTouch.GroupId);
            v.Frame++;v.ObjectiveSupport.CriticalPublicRisk=true;
            AriaSkirmishPlanSystem.Step(v,ref p,ref t,ref output);
            Assert.AreEqual(0,output.TargetId);Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [Test]public void ScrollProgressResetsRetriesButStuckScrollYieldsWithoutPurchase()
        {
            var v=View();v.VisibleHostileAir=1;v.CanAffordAntiAir=true;
            v.RecruitAntiAir=new AriaTouchTarget{Available=true,Id=55,Drag=true,ScrollTargetId=99,ScrollDistanceViewports=2};
            var p=Plan();var t=Touch();Step(v,ref p,ref t,out _);
            v.Frame++;t.Actions++;v.RecruitAntiAir.ScrollDistanceViewports=1.8f;
            Step(v,ref p,ref t,out _);Assert.AreEqual(0,p.ObjectiveSupport.RepeatedContacts);
            for(int i=0;i<3;i++){v.Frame++;t.Actions++;Step(v,ref p,ref t,out _);}
            Assert.AreEqual(3,p.ObjectiveSupport.RepeatedContacts);
            v.Frame++;Assert.IsFalse(Step(v,ref p,ref t,out _));
            Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [Test]public void LostAffordabilityCannotBecomeSuccessfulPurchase()
        {
            var v=View();v.VisibleHostileAir=1;v.CanAffordAntiAir=true;v.RecruitAntiAir=Target(55);
            var p=Plan();var t=Touch();Step(v,ref p,ref t,out _);
            v.Frame++;v.CanAffordAntiAir=false;v.RecruitAntiAir=default;
            Step(v,ref p,ref t,out var output);
            Assert.AreEqual(0,output.TargetId);Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);
            v.Frame++;v.ObjectiveSupport.SimulationSeconds=50;
            Assert.IsFalse(Step(v,ref p,ref t,out _));Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        public static void RunFocusedValidation()
        {
            var s=new AriaObjectiveSupportTests();s.CaptureBuysSecondGroupWhileEscortDoesNotInventCaptureNeed();
            s.RecruitsThroughPresentedControlWithoutChangingObjectiveOrder();s.CriticalRiskAndPendingObjectiveContactPreemptSupport();
            s.CompletedGestureCannotCertifyPurchase();s.RealCommittedRoleClosesDrawerAndResumesObjective();
            s.ReadinessNeedsActualPublicQueueOrStageChange();s.GroundProfileCannotSelectOffensiveAirOrResearchForIt();
            s.PauseDoesNotExpireSupportFromWallClock();s.StopManualDoesNotCancelAcceptedOrders();
            s.ChangedGoalCancelsOnlyOwnedUnconfirmedPlacementNormally();s.RosterReplacementAbortsSingleAircraftServiceWithoutSuccess();
            s.LandedCountAloneDoesNotProveReturnContact();
            s.RealPlannerDispatchesSupportBeforePreservingAcceptedObjective();
            s.ScrollProgressResetsRetriesButStuckScrollYieldsWithoutPurchase();s.LostAffordabilityCannotBecomeSuccessfulPurchase();
            UnityEngine.Debug.Log("[AriaObjectiveSupport] result=Passed controls=15 nativeMissionAcceptance=Pending");
        }
    }
}
