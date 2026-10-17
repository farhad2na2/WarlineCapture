using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class AriaPublicScanTests
    {
        static AriaSkirmishObservation View()=>new AriaSkirmishObservation {Frame=20,Time=100,ExpandedSelectedGroups=1,
            ObjectiveSupport=new AriaObjectiveSupportObservation{Presented=true,SimulationSeconds=50},
            PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Presented=true,Supported=true,PresentationEpoch=1,GroupId=11,PublicKey=22,
                Action=AriaObjectiveAction.Inspect,CameraSettled=true,GoalInView=true,GroundFitsPublicGoal=true,Ground=new AriaTouchTarget{Id=55,Available=true}},
            PublicScan=new AriaPublicScanObservation{Presented=true,SelectedDrone=true,Actor=new UiEntityHandle(3,2),World=8,GroupId=7,SetupHash=99,ScanMode=true}};
        static AriaSkirmishPlanComponent Plan()=>new AriaSkirmishPlanComponent{PublicScan=new AriaPublicScanPlan{Epoch=1,Active=true,Bound=true,
            Actor=new UiEntityHandle(3,2),World=8,GroupId=7,SetupHash=99,GoalKey=22,Deadline=150}};
        static bool Step(ref AriaSkirmishObservation v,ref AriaSkirmishPlanComponent p,ref AriaPlaySessionComponent touch,out AriaPlayObservationComponent output)
        {output=default;bool result=AriaSkirmishPlanSystem.StepPublicScan(v,ref p,ref touch,ref output);v.Frame++;return result;}
        static AriaPlaySessionComponent Touch()=>new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};
        [Test]public void ScanUsesNormalPresentedWorldContactAndRequiresNativeReceipt()
        {
            var v=View();var p=Plan();var t=Touch();Assert.IsTrue(Step(ref v,ref p,ref t,out var output));
            Assert.AreEqual(AriaPlayObservationKind.WorldTarget,output.Kind);Assert.AreEqual(55,output.TargetId);
            t.Actions++;t.TargetId=55;Step(ref v,ref p,ref t,out _);
            Assert.AreEqual(2,p.PublicScan.Stage);Assert.AreEqual(0,t.LastObjectiveProgressAt);
        }
        [Test]public void ExactAcceptedScanWaitsForActualCompletionThenReturnsToObjective()
        {
            var v=View();var p=Plan();var t=Touch();Step(ref v,ref p,ref t,out _);
            v.PublicScan.Revision=1;v.PublicScan.RequestId=9;v.PublicScan.RequestFrame=20;v.PublicScan.ContactAccepted=true;
            v.PublicScan.ContactTarget=55;v.PublicScan.CenterInPublicGoal=true;Step(ref v,ref p,ref t,out _);
            Assert.AreEqual(3,p.PublicScan.Stage);Assert.IsTrue(p.PublicScan.Active);
            v.PublicScan.Completed=true;Assert.IsFalse(Step(ref v,ref p,ref t,out _));Assert.IsFalse(p.PublicScan.Active);
        }
        [Test]public void WrongGoalOrContactCannotCertifyAcceptedScan()
        {
            for(int mode=0;mode<2;mode++)
            {
                var v=View();var p=Plan();var t=Touch();Step(ref v,ref p,ref t,out _);
                v.PublicScan.Revision=1;v.PublicScan.RequestId=9;v.PublicScan.RequestFrame=20;v.PublicScan.ContactAccepted=true;
                v.PublicScan.ContactTarget=mode==0?56:55;v.PublicScan.CenterInPublicGoal=mode==0;
                v.PublicScan.Completed=true;Step(ref v,ref p,ref t,out _);Assert.AreEqual(2,p.PublicScan.Stage);
            }
        }
        [Test]public void ActorReplacementAndPublicGoalChangeAbortWithoutSuccess()
        {
            for(int mode=0;mode<2;mode++)
            {
                var v=View();var p=Plan();var t=Touch();
                if(mode==0)v.PublicScan.Actor=new UiEntityHandle(3,3);else v.PublicObjective.PublicKey=23;
                Assert.IsFalse(Step(ref v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);Assert.IsFalse(p.PublicScan.Active);
            }
        }
        [Test]public void InterruptedScanRequiresFreshTargetAndNewRequestOnReentry()
        {
            for(int mode=0;mode<2;mode++)
            {
                var v=View();var p=Plan();var t=Touch();Step(ref v,ref p,ref t,out _);
                v.PublicScan.Revision=1;v.PublicScan.RequestId=9;v.PublicScan.RequestFrame=20;
                v.PublicScan.ContactAccepted=true;v.PublicScan.ContactTarget=55;v.PublicScan.CenterInPublicGoal=true;
                if(mode==1){Step(ref v,ref p,ref t,out _);Assert.AreEqual(3,p.PublicScan.Stage);v.PublicObjective.PublicKey=23;}
                else v.ObjectiveSupport.SimulationSeconds=200;
                Step(ref v,ref p,ref t,out _);Assert.IsFalse(p.PublicScan.Active);Assert.AreEqual(0,p.PublicScan.Stage);Assert.AreEqual(0,p.PublicScan.RequestId);
                v.PublicObjective.PublicKey=22;v.ObjectiveSupport.SimulationSeconds=p.PublicScan.NextAt+1;v.PublicScan.Completed=true;
                Step(ref v,ref p,ref t,out _);Assert.IsTrue(p.PublicScan.Bound);Assert.AreEqual(0,p.PublicScan.Stage);
                Step(ref v,ref p,ref t,out var fresh);Assert.AreEqual(55,fresh.TargetId);Assert.AreEqual(2,p.PublicScan.Stage);
                Step(ref v,ref p,ref t,out _);Assert.AreEqual(2,p.PublicScan.Stage,"Old completed request cannot complete the new attempt");
            }
        }
        [Test]public void StopAndRiskPreserveAlreadyAcceptedNativeOrders()
        {
            var v=View();var p=Plan();var t=Touch();t.Phase=AriaPlayPhase.Manual;
            Assert.IsFalse(Step(ref v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);
            p=Plan();t=Touch();v.ObjectiveSupport.CriticalPublicRisk=true;
            Assert.IsFalse(Step(ref v,ref p,ref t,out output));Assert.AreEqual(0,output.TargetId);
        }
    }
}
