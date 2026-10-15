using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class AriaPublicBoardingTests
    {
        static AriaSkirmishObservation View()=>new AriaSkirmishObservation{Frame=20,Time=100,ExpandedSelectedGroups=1,
            ObjectiveSupport=new AriaObjectiveSupportObservation{Presented=true,SimulationSeconds=50},
            PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Presented=true,Supported=true,PresentationEpoch=1,GroupId=11,PublicKey=22,CameraSettled=true,
                ExclusiveGroupSelected=true,Action=AriaObjectiveAction.Move,GoalInView=true,GroundFitsPublicGoal=true,MoveMode=true,Ground=new AriaTouchTarget{Available=true,Id=88}},
            PublicBoarding=new AriaPublicBoardingObservation{Presented=true,Eligible=true,ExitOpen=true,GroupId=11,CarrierGroupId=7,Carrier=new UiEntityHandle(5,3),World=8,SetupHash=99,
                CarrierInView=true,BoardMode=true,CarrierTarget=new AriaTouchTarget{Available=true,Id=66},SelectCarrier=new AriaTouchTarget{Available=true,Id=77}}};
        static AriaPlaySessionComponent Touch()=>new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};
        static bool Step(ref AriaSkirmishObservation v,ref AriaSkirmishPlanComponent p,ref AriaPlaySessionComponent t,out AriaPlayObservationComponent output)
        {output=default;bool result=AriaSkirmishPlanSystem.StepPublicBoarding(v,ref p,ref t,ref output);v.Frame++;return result;}
        [Test]public void BoardUsesNormalCarrierTouchAndCannotCompleteFromGesture()
        {
            var v=View();var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out var output);
            Assert.AreEqual(66,output.TargetId);Assert.AreEqual(AriaPlayObservationKind.WorldTarget,output.Kind);
            t.Actions++;t.TargetId=66;Step(ref v,ref p,ref t,out _);Assert.AreEqual(1,p.PublicBoarding.Stage);Assert.AreEqual(0,t.LastObjectiveProgressAt);
        }
        [Test]public void FerryRequiresFullMountAndAcceptedNormalMoveBeforeExitUnload()
        {
            var v=View();var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out _);
            v.PublicBoarding.Accepted=true;v.PublicBoarding.Frame=20;v.PublicBoarding.ContactTarget=66;v.PublicBoarding.RequestId=9;
            Step(ref v,ref p,ref t,out _);Assert.AreEqual(2,p.PublicBoarding.Stage);
            v.PublicBoarding.CarrierSelected=true;v.PublicBoarding.AllMounted=true;
            Step(ref v,ref p,ref t,out var move);Assert.AreEqual(88,move.TargetId);Assert.AreEqual(3,p.PublicBoarding.Stage);
            v.PublicBoarding.MoveAccepted=true;v.PublicBoarding.MoveRevision=1;v.PublicBoarding.MoveFrame=22;v.PublicBoarding.ContactTarget=88;
            Step(ref v,ref p,ref t,out _);Assert.AreEqual(4,p.PublicBoarding.Stage);
            v.PublicBoarding.AtExit=true;Assert.IsFalse(Step(ref v,ref p,ref t,out _));Assert.IsFalse(p.PublicBoarding.Active);
        }
        [Test]public void PartialBoardingCannotDispatchCarrierMove()
        {
            var v=View();var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out _);
            v.PublicBoarding.Accepted=true;v.PublicBoarding.Frame=20;v.PublicBoarding.ContactTarget=66;v.PublicBoarding.RequestId=9;Step(ref v,ref p,ref t,out _);
            v.PublicBoarding.CarrierSelected=true;v.PublicBoarding.AllMounted=false;Step(ref v,ref p,ref t,out var output);
            Assert.AreEqual(0,output.TargetId);Assert.AreEqual(2,p.PublicBoarding.Stage);
        }
        [Test]public void CarrierReplacementBlocksWithoutRebindingOrUnload()
        {
            var v=View();var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out _);
            v.PublicBoarding.Carrier=new UiEntityHandle(5,4);Step(ref v,ref p,ref t,out var output);
            Assert.AreEqual(AriaPlayPhase.Blocked,t.Phase);Assert.AreEqual(0,output.TargetId);
        }
        [Test]public void FreshMountedRosterPreservesExistingCarrierMoveWithoutOldReceipt()
        {
            var v=View();v.PublicObjective.Action=AriaObjectiveAction.Unload;v.PublicBoarding.Eligible=false;
            v.PublicBoarding.AlreadyMounted=true;v.PublicBoarding.AllMounted=true;v.PublicBoarding.CarrierSelected=true;v.PublicBoarding.ExistingMoveToExit=true;
            var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out var output);
            Assert.AreEqual(4,p.PublicBoarding.Stage);Assert.AreEqual(0,p.PublicBoarding.RequestId);Assert.AreEqual(0,output.TargetId);
            v.PublicBoarding.AtExit=true;Assert.IsFalse(Step(ref v,ref p,ref t,out _));
        }
        [Test]public void PublicBoardContactRejectsStaleScreenAndUnpresentedNativeResult()
        {
            var m=new UiPublicBoardingModel{Accepted=true,Presented=true,RequestId=9,Frame=20,CompletedFrame=21,CompletedTarget=66};
            Assert.IsTrue(SkirmishPresentedBoardingPolicy.AcceptedContact(m));
            m.Presented=false;Assert.IsFalse(SkirmishPresentedBoardingPolicy.AcceptedContact(m));
            m.Presented=true;m.CompletedFrame=23;Assert.IsFalse(SkirmishPresentedBoardingPolicy.AcceptedContact(m));
            m.CompletedFrame=21;m.CompletedScreen=new UnityEngine.Vector2(3,0);Assert.IsFalse(SkirmishPresentedBoardingPolicy.AcceptedContact(m));
        }
        [Test]public void StopPreservesAcceptedOrdersAndTimeoutCannotUnloadOutsideExit()
        {
            var v=View();var p=new AriaSkirmishPlanComponent();var t=Touch();Step(ref v,ref p,ref t,out _);
            t.Phase=AriaPlayPhase.Manual;Assert.IsFalse(Step(ref v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);
            p=new AriaSkirmishPlanComponent();t=Touch();Step(ref v,ref p,ref t,out _);v.ObjectiveSupport.SimulationSeconds=500;
            Step(ref v,ref p,ref t,out output);Assert.AreEqual(AriaPlayPhase.Blocked,t.Phase);Assert.AreEqual(0,output.TargetId);
        }
    }
}
