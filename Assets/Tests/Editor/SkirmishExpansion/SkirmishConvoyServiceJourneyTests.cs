using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishConvoyServiceJourneyTests
    {
        public static void RunFocusedValidation(){var t=new SkirmishConvoyServiceJourneyTests();for(int i=0;i<23;i++)t.PublicJourneyPreservesNativeGates(i);t.HandbackKeepsTheEscortObjectiveForAria();t.TruckInsideTheDestinationIsNotSentToService();t.HoldThatNeverStopsTheTruckSkipsTheDetour();Debug.Log("[SkirmishConvoyServiceJourney] result=Passed controls=26 nativeMissionPending=1");}
        static UiSkirmishPublicObjectiveModel Model(int health=60,float x=0)
        {
            var node=new SkirmishPublicGoalNode{PublicId="original.truck",Kind=SkirmishPublicGoalNodeKind.OwnDesignatedSubject,Provenance=SkirmishPublicGoalProvenance.OwnRoster,
                OwnerKnown=true,OwnerFaction=1,HealthKnown=true,Health=health,MaximumHealth=100,HealthProvenance=SkirmishPublicHealthProvenance.OwnRoster,
                RevealedSubjectKind=SkirmishPublicSubjectKind.ObjectiveConvoyTruck,SubjectDisposition=SkirmishPublicSubjectDisposition.Live};
            Assert.That(SkirmishPublicGoalSnapshot.TryCreate("service-control",1,1,SkirmishObjectiveKind.ConvoyEscort,SkirmishPublicGoalPerspective.Escort,0,100,
                SkirmishOutcomeKind.None,new SkirmishPublicGoalScore{OriginalSubjects=3,Required=2,ProgressKnown=true},new[]{
                    new SkirmishPublicGoalNode{PublicId="destination",Kind=SkirmishPublicGoalNodeKind.DeliveryDestination,Provenance=SkirmishPublicGoalProvenance.PublicObjective},node},out var goal,out var error),Is.True,error);
            return new UiSkirmishPublicObjectiveModel{AttemptId="journey",Available=true,Goal=goal,GroundCellSize=1,
                OwnGroups=new[]{new UiSkirmishPublicGroupRow{AliveCount=1,OwnMemberPositions=new[]{new Vector2(x,100)},Policy=new SkirmishPublicPlayerGroup{
                    GroupId=7,Own=true,Living=true,CanMove=true,CanHold=true,WorldX=x,WorldZ=100,PositionKnown=true,DesignatedSubjects=new[]{"original.truck"}}}},
                Service=new UiPublicConvoyService{PublicId="service/pad",FacilityPublicId="own.staging",Available=true,World=new Vector3(100,2,100),EligibilityRadiusMetres=12,Materials=100,
                    Destinations=new[]{new UiPublicServiceDestination{World=new Vector3(100,2,100),GroundAvailable=true,PresentedGroundId=21},
                        new UiPublicServiceDestination{World=new Vector3(104,2,100),GroundAvailable=true,PresentedGroundId=22},
                        new UiPublicServiceDestination{World=new Vector3(96,2,100),GroundAvailable=true,PresentedGroundId=23}},
                    Trucks=new[]{new UiPublicConvoyRepairTruck{OriginalPublicId="original.truck",GroupId=7,Health=health,MaximumHealth=100,Materials=20,DurationTicks=200000000,Affordable=true}}}};
        }
        static UiVehicleRepairModel Selected()=>new UiVehicleRepairModel{SingleOwnVehicle=true,ActorPublicId="original.truck",AttemptId="journey",GroupId=7,Eligible=true,Health=60,MaximumHealth=100};
        static void AtPad(UiSkirmishPublicObjectiveModel model){model.OwnGroups[0].Policy.WorldX=100;model.OwnGroups[0].OwnMemberPositions=new[]{new Vector2(100,100)};model.OwnGroups[0].HoldActive=true;}
        static void BeginRepair(SkirmishConvoyServiceJourney journey,UiSkirmishPublicObjectiveModel model,UiVehicleRepairModel selected)
        {journey.Tick(model,null,false,false);AtPad(model);Assert.That(journey.Tick(model,selected,false,false),Is.EqualTo(ConvoyServiceStage.Hold));model.Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=true,GroupId=7,Action=AriaObjectiveAction.Hold,Revision=1};Assert.That(journey.Tick(model,selected,true,false),Is.EqualTo(ConvoyServiceStage.Repair));}
        // W37b (lead bound; W37 proof: 0 of 11 service Holds accepted, each detour 100-130 s): one Hold attempt, then the
        // detour is skipped (Handback, latched) instead of retrying the other service destinations.
        [Test]
        public void HoldThatNeverStopsTheTruckSkipsTheDetour()
        {
            var model=Model();var journey=new SkirmishConvoyServiceJourney();
            Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Travel));
            AtPad(model);model.SimulationSeconds=10;
            Assert.That(journey.Tick(model,Selected(),false,false),Is.EqualTo(ConvoyServiceStage.Hold));
            model.SimulationSeconds=29;Assert.That(journey.Tick(model,Selected(),false,false),Is.EqualTo(ConvoyServiceStage.Hold),"inside the 20 s Hold budget");
            model.SimulationSeconds=31;Assert.That(journey.Tick(model,Selected(),false,false),Is.EqualTo(ConvoyServiceStage.Handback),"not stopped within one attempt: no retry");
            Assert.That(journey.Failure,Does.Contain("could not stop"));Assert.That(journey.Failures,Is.EqualTo(1));
            model.SimulationSeconds=200;Assert.That(journey.Tick(model,Selected(),false,false),Is.EqualTo(ConvoyServiceStage.Handback),"latched");
            // An accepted Hold within the budget still starts the repair (unchanged).
            var ok=Model();var j2=new SkirmishConvoyServiceJourney();var shown=Selected();BeginRepair(j2,ok,shown);
            Assert.That(j2.Stage,Is.EqualTo(ConvoyServiceStage.Repair));
        }

        // W27 cause 3 (S068, S043/S045/S047): a handed-back service kept ARIA on a blank objective until the 20 s frame
        // Block. The handback stays latched (no retried service) while the escort objective stays presented.
        [Test]
        public void HandbackKeepsTheEscortObjectiveForAria()
        {
            var model=Model();var journey=new SkirmishConvoyServiceJourney();
            journey.Tick(model,null,false,false);model.Service.Available=false;
            Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Handback));
            Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Handback),"latched: the failed service is not retried");
            Assert.That(SkirmishConvoyServiceJourney.PresentsEscortObjective(ConvoyServiceStage.Handback),Is.True);
            Assert.That(SkirmishConvoyServiceJourney.PresentsEscortObjective(ConvoyServiceStage.Resume),Is.True);
            foreach(var stage in new[]{ConvoyServiceStage.None,ConvoyServiceStage.Travel,ConvoyServiceStage.Hold,ConvoyServiceStage.Repair,ConvoyServiceStage.EmergencyHold})
                Assert.That(SkirmishConvoyServiceJourney.PresentsEscortObjective(stage),Is.False,stage.ToString());
            string root=System.IO.Path.GetFullPath(System.IO.Path.Combine(ThisFile(),"..","..","..",".."));
            string hud=System.IO.File.ReadAllText(System.IO.Path.Combine(root,"Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.ConvoyService.cs"));
            Assert.That(hud,Does.Contain("if(SkirmishConvoyServiceJourney.PresentsEscortObjective(stage))view.PublicObjective=escort;"));
            Assert.That(hud,Does.Not.Contain("view.PublicObjective=new AriaObjectiveTouchObservation{Applicable=true,Intent=AriaSkirmishIntent.ServiceOriginalTruck};return;"));
        }
        // W37 (S046 130365, 231 s): the truck had reached the destination (Move done, no Hold yet) and was sent to the
        // service pad 2 s before its 5 s dwell delivered it.
        [Test]
        public void TruckInsideTheDestinationIsNotSentToService()
        {
            var model=Model();var journey=new SkirmishConvoyServiceJourney();
            Assert.That(journey.Tick(model,null,false,false,new System.Collections.Generic.HashSet<uint>{7}),Is.EqualTo(ConvoyServiceStage.None),"dwelling: no service");
            Assert.That(journey.Tick(model,null,false,false,new System.Collections.Generic.HashSet<uint>{8}),Is.EqualTo(ConvoyServiceStage.Travel),"another group inside: unchanged");
            Assert.That(new SkirmishConvoyServiceJourney().Tick(Model(),null,false,false),Is.EqualTo(ConvoyServiceStage.Travel),"no set: unchanged");
        }
        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path="")=>path;

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        [TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)]
        [TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)][TestCase(18)][TestCase(19)]
        [TestCase(20)][TestCase(21)][TestCase(22)]
        public void PublicJourneyPreservesNativeGates(int mutation)
        {
            var model=Model(mutation==2?30:60);var journey=new SkirmishConvoyServiceJourney();var selected=Selected();
            if(mutation>=14&&mutation<=18)
            {
                var group=model.OwnGroups[0];group.CurrentMoveGoals=new[]{new Vector2(mutation==15?150:100,100)};
                model.Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=true,Action=AriaObjectiveAction.Move,GroupId=7,WorldKnown=true,World=new Vector3(100,2,100),Frame=50,Screen=new Vector2(20,30)};
                model.CompletedContactTargetId=mutation==16?999:21;model.CompletedContactFrame=mutation==17?47:50;
                model.CompletedContactPosition=mutation==18?Vector2.zero:new Vector2(20,30);
                Assert.That(SkirmishPublicServiceAcceptance.Move(model,group,new[]{21,22,23}),Is.EqualTo(mutation==14));return;
            }
            if(mutation==19)
            {
                journey.Tick(model,null,false,false);uint epoch=journey.Epoch;journey.Reset();
                Assert.That(journey.Stage,Is.EqualTo(ConvoyServiceStage.None));Assert.That(journey.GroupId,Is.Zero);
                Assert.That(journey.OriginalId,Is.Null);Assert.That(journey.Attempt,Is.Null);Assert.That(journey.Epoch,Is.GreaterThan(epoch));return;
            }
            if(mutation==20)
            {
                model=Model(30);model.Service.Trucks[0].Affordable=false;
                Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.EmergencyHold));
                model.OwnGroups[0].HoldActive=true;model.Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=true,Action=AriaObjectiveAction.Hold,GroupId=7,Revision=1};
                Assert.That(journey.Tick(model,null,true,false),Is.EqualTo(ConvoyServiceStage.Handback));Assert.That(journey.Failure,Does.Contain("Materials"));return;
            }
            if(mutation==21)
            {
                model.Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=true,Action=AriaObjectiveAction.Hold,GroupId=7,Revision=4};
                journey.Tick(model,null,false,false);AtPad(model);journey.Tick(model,selected,false,false);
                Assert.That(journey.Tick(model,selected,true,false),Is.EqualTo(ConvoyServiceStage.Hold));
                model.Order.Revision=5;Assert.That(journey.Tick(model,selected,true,false),Is.EqualTo(ConvoyServiceStage.Repair));return;
            }
            if(mutation==22)
            {
                // W37: one attempt, then handback (was a Travel retry, three times).
                BeginRepair(journey,model,selected);model.SimulationSeconds=26;
                Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Handback));Assert.That(journey.Failure,Does.Contain("could not be verified"));
                var again=new SkirmishConvoyServiceJourney();var shown=Selected();var fresh=Model();BeginRepair(again,fresh,shown);fresh.SimulationSeconds=26;
                Assert.That(again.Tick(fresh,shown,false,false),Is.EqualTo(ConvoyServiceStage.Handback),"exact selection, repair never started");
                Assert.That(again.Failure,Does.Contain("could not start"));return;
            }
            if(mutation==0){Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Travel));Assert.That(journey.GroupId,Is.EqualTo(7));return;}
            if(mutation==1||mutation==2){model.OwnGroups[0].Policy.AcceptedOrderActive=true;Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(mutation==2?ConvoyServiceStage.Travel:ConvoyServiceStage.None));return;}
            if(mutation==3){model.Service.Trucks[0].Affordable=false;Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.None));return;}
            if(mutation==4){model.Service.Trucks[0].OriginalPublicId="foreign";Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.None));return;}
            if(mutation==5){journey.Tick(model,null,false,false);AtPad(model);Assert.That(journey.Tick(model,selected,false,false),Is.EqualTo(ConvoyServiceStage.Hold));return;}
            if(mutation==8||mutation==9)
            {
                journey.Tick(model,null,false,false);
                for(int i=1;i<=3;i++)
                {
                    model.Order=new UiSkirmishPublicOrderReadout{Present=true,Accepted=false,GroupId=7,RequestId=i,Action=AriaObjectiveAction.Move,Frame=i};
                    model.CompletedContactTargetId=mutation==9?999:model.Service.Destinations[journey.DestinationIndex].PresentedGroundId;model.CompletedContactFrame=i;
                    journey.Tick(model,null,false,false);
                }
                Assert.That(journey.Stage,Is.EqualTo(mutation==8?ConvoyServiceStage.Handback:ConvoyServiceStage.Travel));
                Assert.That(journey.Failures,Is.EqualTo(mutation==8?3:0));return;
            }
            if(mutation==11){journey.Tick(model,null,false,false);model.Service.Available=false;Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Handback));return;}
            if(mutation==12){model.Paused=true;model.SimulationSeconds=100;Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.None));return;}
            if(mutation==13){journey.Tick(model,null,false,false);model.OwnGroups=System.Array.Empty<UiSkirmishPublicGroupRow>();Assert.That(journey.Tick(model,null,false,false),Is.EqualTo(ConvoyServiceStage.Resume));return;}
            BeginRepair(journey,model,selected);selected.IssuedRequestId=1;selected.StateRequestId=1;selected.StateMatchesIssued=true;
            if(mutation==10){selected.Phase=UiVehicleRepairPhase.AccountingFailure;Assert.That(journey.Tick(model,selected,false,false),Is.EqualTo(ConvoyServiceStage.Handback));return;}
            selected.Phase=UiVehicleRepairPhase.Completed;selected.Health=85;
            Assert.That(journey.Tick(model,selected,false,mutation==7),Is.EqualTo(mutation==7?ConvoyServiceStage.Resume:ConvoyServiceStage.Repair));
        }
    }
}
