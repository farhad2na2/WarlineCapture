using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
namespace Game.Tests.Editor
{
    public sealed class AriaOffensiveAirTests
    {
        static AriaSkirmishObservation View()=>new AriaSkirmishObservation {Frame=50,Time=100,ExpandedSelectedGroups=1,ExpandedPageCount=1,
            PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Presented=true,Supported=true,PresentationEpoch=1,PublicKey=22,GroupId=11,
                Action=AriaObjectiveAction.Move,CameraSettled=true,GoalInView=true,GroundFitsPublicGoal=true,Ground=new AriaTouchTarget{Available=true,Id=77}},
            ObjectiveSupport=new AriaObjectiveSupportObservation{Presented=true,SimulationSeconds=50},AttackMode=true,
            OffensiveAir=new AriaOffensiveAirObservation{Presented=true,Eligible=true,Actor=new UiEntityHandle(5,3),World=8,SetupHash=123,GroupId=7,Role=AriaPublicAirRole.Strike}};
        static AriaSkirmishPlanComponent Plan()=>new AriaSkirmishPlanComponent {ObjectiveTouch=new AriaObjectiveTouchPlan{Stage=4},
            OffensiveAir=new AriaOffensiveAirPlan{Epoch=1}};
        static bool Step(ref AriaSkirmishObservation v,ref AriaSkirmishPlanComponent p,ref AriaPlaySessionComponent t,out AriaPlayObservationComponent result)
        {result=default;bool active=AriaSkirmishPlanSystem.StepOffensiveAir(v,ref p,ref t,ref result);v.Frame++;return active;}
        [Test]public void NormalAttackModeAndExactPublicGroundContactAreRequired()
        {
            var v=View();v.AttackMode=false;v.Attack=new AriaTouchTarget{Available=true,Id=66};var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};
            Step(ref v,ref p,ref t,out _);Step(ref v,ref p,ref t,out var mode);Assert.AreEqual(66,mode.TargetId);Assert.AreEqual(0,p.OffensiveAir.Stage);
            v.AttackMode=true;Step(ref v,ref p,ref t,out var target);Assert.AreEqual(77,target.TargetId);Assert.AreEqual(2,p.OffensiveAir.Stage);
            Assert.AreEqual(0,t.LastObjectiveProgressAt,"A requested ordinary touch is not native acceptance");
        }
        [Test]public void GestureCompletionAndStaleReceiptCannotCountAsAirPressure()
        {
            var v=View();var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};Step(ref v,ref p,ref t,out _);Step(ref v,ref p,ref t,out _);
            t.Actions=8;t.TargetId=77;v.OffensiveAir.AcceptedContact=true;v.OffensiveAir.GoalFits=true;v.OffensiveAir.ContactTarget=77;
            Step(ref v,ref p,ref t,out _);Assert.IsTrue(p.OffensiveAir.Active);Assert.AreEqual(0,t.LastObjectiveProgressAt);
        }
        [Test]public void NewExactActorOrderRequiresNativeGoalInsideDisplayedObjective()
        {
            var v=View();var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};Step(ref v,ref p,ref t,out _);Step(ref v,ref p,ref t,out _);
            v.OffensiveAir.Revision=1;v.OffensiveAir.RequestFrame=51;v.OffensiveAir.AcceptedContact=true;v.OffensiveAir.ContactTarget=77;
            Step(ref v,ref p,ref t,out _);Assert.IsTrue(p.OffensiveAir.Active);Assert.AreEqual(0,t.LastObjectiveProgressAt);
            v.OffensiveAir.GoalFits=true;Step(ref v,ref p,ref t,out _);Assert.IsFalse(p.OffensiveAir.Active);Assert.AreEqual(100,t.LastObjectiveProgressAt);
            Assert.AreEqual(4,p.ObjectiveTouch.Stage);
        }
        [Test]public void ReplacementOrReturningAircraftYieldsWithoutCancellingNativeOrders()
        {
            foreach(bool replacement in new[]{false,true})
            {
                var v=View();var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};Step(ref v,ref p,ref t,out _);
                if(replacement)v.OffensiveAir.Actor=new UiEntityHandle(5,4);else v.OffensiveAir.Returning=true;
                Assert.IsFalse(Step(ref v,ref p,ref t,out var command));Assert.AreEqual(0,command.TargetId);Assert.IsFalse(p.OffensiveAir.Active);
            }
        }
        [Test]public void ExistingCombatAssignmentAndPublicRiskPreemptNewPressure()
        {
            var v=View();v.OffensiveAir.Busy=true;v.SelectedSquadToToggle=new AriaTouchTarget{Available=true,Id=44};var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};
            Assert.IsTrue(Step(ref v,ref p,ref t,out var output));Assert.AreEqual(44,output.TargetId);Assert.AreEqual(0,p.OffensiveAir.Stage);
            v.ObjectiveSupport.CriticalPublicRisk=true;p=Plan();Assert.IsFalse(Step(ref v,ref p,ref t,out output));Assert.AreEqual(0,output.TargetId);
            v=View();v.OffensiveAir=default;v.AirRecruit=new AriaAirRecruitObservation{Presented=true};p=Plan();
            Assert.IsFalse(Step(ref v,ref p,ref t,out output),"No combat-air roster must not start an empty page sweep");Assert.AreEqual(0,output.TargetId);
        }
        [Test]public void StopClearsOnlyPolicyAndFreshAttemptCannotReuseOldContact()
        {
            var v=View();var p=Plan();var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};Step(ref v,ref p,ref t,out _);Step(ref v,ref p,ref t,out _);
            t.Phase=AriaPlayPhase.Manual;Assert.IsFalse(Step(ref v,ref p,ref t,out var output));Assert.AreEqual(0,output.TargetId);Assert.AreEqual(0,p.OffensiveAir.Stage);
            t.Phase=AriaPlayPhase.Observing;v.ObjectiveSupport.SimulationSeconds=200;Step(ref v,ref p,ref t,out _);
            v.ObjectiveSupport.SimulationSeconds=221;Step(ref v,ref p,ref t,out _);Assert.AreEqual(0,p.OffensiveAir.Stage);Assert.AreEqual(0,p.OffensiveAir.Target);
        }
        [Test]public void RecruitmentChoosesLegalAirCounterOrAvailableGroundPressureRole()
        {
            var v=View();v.VisibleHostileAir=1;v.AirRecruit=new AriaAirRecruitObservation{Presented=true,Fighter=new AriaAirRecruitChoice{CanQueue=true},Strike=new AriaAirRecruitChoice{CanQueue=true}};
            Assert.AreEqual(AriaPublicAirRole.Fighter,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
            v.AirRecruit.Fighter.CanQueue=false;Assert.AreEqual(AriaPublicAirRole.Strike,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
            v.AirRecruit.Strike.CanQueue=false;Assert.AreEqual(AriaPublicAirRole.None,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
        }
        [Test]public void PendingAndExistingPressureRespectCapsWithoutRoleSubstitution()
        {
            var v=View();v.AirRecruit=new AriaAirRecruitObservation{Presented=true,Strike=new AriaAirRecruitChoice{CanQueue=true,Pending=true},Heavy=new AriaAirRecruitChoice{CanQueue=true}};
            Assert.AreEqual(AriaPublicAirRole.None,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
            v.AirRecruit.Strike.Pending=false;v.AirRecruit.Strike.Live=1;v.AirRecruit.Light.Live=1;
            Assert.AreEqual(AriaPublicAirRole.None,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
            v.AirRecruit.Strike.Live=v.AirRecruit.Light.Live=0;v.AirRecruit.Fighter.Live=2;
            v.AirProfile=true;v.AirQueueOffered=true;v.ReadinessEligible=true;v.PadPresent=true;
            v.ObjectiveSupport.DismountedCombatGroups=2;v.LogisticsTruckCommitted=true;v.TankersCommitted=2;
            Assert.AreEqual(AriaObjectiveSupportAction.None,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v),"Old single-helicopter fallback cannot bypass the current role-specific fleet bound");
        }
        [Test]public void TransportRecruitmentIsUsefulOnlyForEvacuationAndReconRemainsNonCombat()
        {
            var v=View();v.AirRecruit=new AriaAirRecruitObservation{Presented=true,TransportHeli=new AriaAirRecruitChoice{CanQueue=true},Drone=new AriaAirRecruitChoice{CanQueue=true}};
            Assert.AreEqual(AriaPublicAirRole.Drone,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
            v.ObjectiveSupport.Evacuation=true;
            Assert.AreEqual(AriaPublicAirRole.TransportHeli,AriaSkirmishPlanSystem.ChoosePublicAirRecruit(v));
        }
        [Test]public void RecoveredNativeScanWithoutEphemeralReceiptIsPreservedWithoutInventedCompletion()
        {
            var v=View();v.PublicScan=new AriaPublicScanObservation{Presented=true,SelectedDrone=true,InProgress=true,Actor=new UiEntityHandle(5,3),World=8,GroupId=7,SetupHash=123};
            var p=Plan();p.PublicScan=new AriaPublicScanPlan{Epoch=1};var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};var output=default(AriaPlayObservationComponent);
            Assert.IsFalse(AriaSkirmishPlanSystem.StepPublicScan(v,ref p,ref t,ref output));
            Assert.IsFalse(p.PublicScan.Active);Assert.AreEqual(0,output.TargetId);Assert.AreEqual(0,t.LastObjectiveProgressAt);
            Assert.AreEqual(0,p.PublicScan.RequestId);Assert.AreEqual(0,p.PublicScan.Stage);
        }
        [Test]public void PeriodicReturnCannotInterruptAnExistingRecoveredNativeScan()
        {
            var v=View();v.AircraftService=new AriaAircraftServiceObservation{Presented=true,NativeTaskBusy=true,Actor=new UiEntityHandle(5,3),World=8,GroupId=7,SetupHash=123,HomeKnown=true,Airborne=true};
            v.SelectedAircraft=true;v.ReturnAircraft=new AriaTouchTarget{Available=true,Id=66};
            var p=Plan();p.ObjectiveTouch.GroupId=11;p.ObjectiveTouch.PublicKey=22;p.ObjectiveTouch.Action=AriaObjectiveAction.Move;
            p.ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,Action=AriaObjectiveSupportAction.ReturnAircraft,Deadline=200};
            var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};var output=default(AriaPlayObservationComponent);
            AriaSkirmishPlanSystem.StepPublicObjectiveSupport(v,ref p,ref t,ref output);
            Assert.AreEqual(AriaObjectiveSupportAction.None,p.ObjectiveSupport.Action);Assert.AreEqual(0,output.TargetId);
        }
        [Test]public void RequestedFighterCannotBeCertifiedByAnotherRolesPendingQueue()
        {
            var v=View();v.AirRecruit=new AriaAirRecruitObservation {Presented=true,Light=new AriaAirRecruitChoice{Pending=true},
                Fighter=new AriaAirRecruitChoice{Role=AriaPublicAirRole.Fighter,CanQueue=true,Target=new AriaTouchTarget{Available=true,Id=88}}};
            var p=Plan();p.ObjectiveTouch.GroupId=11;p.ObjectiveTouch.PublicKey=22;p.ObjectiveTouch.Action=AriaObjectiveAction.Move;
            p.ObjectiveSupport=new AriaObjectiveSupportPlan{Epoch=1,Action=AriaObjectiveSupportAction.AttackAircraft,RecruitAirRole=AriaPublicAirRole.Fighter,Deadline=200};
            var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing};var output=default(AriaPlayObservationComponent);
            AriaSkirmishPlanSystem.StepPublicObjectiveSupport(v,ref p,ref t,ref output);
            Assert.IsFalse(p.ObjectiveSupport.ObservedAcceptance);Assert.AreEqual(88,output.TargetId);
            v.Frame++;v.AirRecruit.Fighter.Pending=true;output=default;
            AriaSkirmishPlanSystem.StepPublicObjectiveSupport(v,ref p,ref t,ref output);
            Assert.IsTrue(p.ObjectiveSupport.ObservedAcceptance);Assert.AreEqual(AriaObjectiveSupportAction.None,p.ObjectiveSupport.Action);
        }
        [Test]public void ReadinessRequiresAUsefulLegallyQuotedFutureRoleAndNormalResearchEligibility()
        {
            var v=View();v.AirProfile=false;v.LogisticsTruckCommitted=true;v.TankersCommitted=2;v.ObjectiveSupport.DismountedCombatGroups=2;
            v.ObjectiveSupport.ResearchVisible=true;v.CanUpgradeReadiness=true;
            v.AirRecruit=new AriaAirRecruitObservation{Presented=true,Drone=new AriaAirRecruitChoice{CanQueueAtFullReadiness=true}};
            Assert.AreEqual(AriaObjectiveSupportAction.Readiness,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
            v.CanUpgradeReadiness=false;Assert.AreEqual(AriaObjectiveSupportAction.None,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
            v.CanUpgradeReadiness=true;v.AirRecruit.Drone.CanQueueAtFullReadiness=false;
            Assert.AreEqual(AriaObjectiveSupportAction.None,AriaSkirmishPlanSystem.ChoosePublicObjectiveSupport(v));
        }
    }
}
