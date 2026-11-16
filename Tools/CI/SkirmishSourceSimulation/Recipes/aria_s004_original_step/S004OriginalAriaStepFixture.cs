using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Game.Simulation;
internal static class S004OriginalAriaStepFixture {
 static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
 public static void Main(){
  var view=new AriaSkirmishObservation{SourceScenarioIndex=6,ExpandedSession=true,ExpandedBaseAssault=true,AirProfile=true,Active=true,Frame=11,Time=20f};
  Require(!AriaS002NativeAdapter.CanHandle(view),"S004 scenario index must bypass S002 adapter");
  var other=view;other.AirProfile=false;Require(!AriaS002NativeAdapter.CanHandle(other),"Index 6 must bypass even without air profile");
  other=view;other.SourceScenarioIndex=4;Require(!AriaS002NativeAdapter.CanHandle(other),"Air profile must bypass index 4 adapter");
  foreach(var phase in new[]{AriaPlayPhase.Blocked,AriaPlayPhase.Starting}){
   var plan=default(AriaSkirmishPlanComponent);var touch=new AriaPlaySessionComponent{Phase=phase,Actions=7};var output=default(AriaPlayObservationComponent);
   AriaSkirmishPlanSystem.Step(view,ref plan,ref touch,ref output);
   Require(output.Kind==AriaPlayObservationKind.Waiting && touch.Phase==phase && touch.Actions==7,"Stopped phase must wait without accepted input");
  }
  var p=default(AriaSkirmishPlanComponent);var t=new AriaPlaySessionComponent{Phase=AriaPlayPhase.Observing,Actions=7};var o=default(AriaPlayObservationComponent);
  AriaSkirmishPlanSystem.Step(view,ref p,ref t,ref o);
  Require(t.Actions==7,"Planner must not fabricate accepted input");
  var facts=new AriaPreparationView{AirProfile=true,Infantry=9,Time=20f,CanAffordRifle=true};
  var state=default(AriaPreparationState);
  Require(AriaExpandedPreparationPolicy.AirOpening(facts,ref state)==AriaAirOpeningAction.RecruitRifle && state.OpeningUntil==95f && state.AssaultStarted==0,"Affordable air opening must request a rifle without any presented control");
  facts.RifleRecruitPending=true;state=default;
  Require(AriaExpandedPreparationPolicy.AirOpening(facts,ref state)==AriaAirOpeningAction.WaitForPending && state.AssaultStarted==0,"Pending production must wait");
  facts.RifleRecruitPending=false;facts.CanAffordRifle=false;state=default;
  Require(AriaExpandedPreparationPolicy.AirOpening(facts,ref state)==AriaAirOpeningAction.ContinueAssault && state.AssaultStarted==1,"Unavailable reinforcement must preserve original transition");
  facts.CanAffordRifle=true;state=new AriaPreparationState{OpeningUntil=20f};
  Require(AriaExpandedPreparationPolicy.AirOpening(facts,ref state)==AriaAirOpeningAction.ContinueAssault && state.AssaultStarted==1,"Deadline boundary must preserve original transition");
  facts.AirProfile=false;state=default;
  Require(AriaExpandedPreparationPolicy.AirOpening(facts,ref state)==AriaAirOpeningAction.NotApplicable && state.OpeningUntil==0,"Other profiles must remain unchanged");
  Console.WriteLine("[S004OriginalAriaStepFixture] result=Passed scenario=6 adapter=Bypassed fixtureOnly=1 actionAcceptance=Unclaimed observedKind="+o.Kind+" observedIntent="+p.Intent);
 }
}
