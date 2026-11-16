using System;
using Game.Simulation;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;

// Explicit synthetic facts only: no source mission, input, or acceptance is asserted.
internal static class S003OriginalAriaStepFixture
{
    static void Main()
    {
        var view = new AriaSkirmishObservation {
            SourceScenarioIndex = 5, Active = true, ExpandedSession = true,
            ExpandedBaseAssault = true, AirProfile = true, Frame = 11, Time = 20f,
            Infantry = 20, PresentedHomeThreat = true, PlacementOpen = true,
            PlacementCancel = new AriaTouchTarget { Id = 101, Available = true },
            CanAffordLogisticsTruck = true,
            RecruitLogisticsTruck = new AriaTouchTarget { Id = 102, Available = true }
        };
        if (AriaS002NativeAdapter.CanHandle(view)) throw new InvalidOperationException("Wrong planner route");
        var plan = default(AriaSkirmishPlanComponent);
        var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 7 };
        var output = default(AriaPlayObservationComponent);
        AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
        var facts = new AriaPreparationView { AirProfile = true, Infantry = 20, Time = 20f,
            CanAffordLogisticsTruck = true, LogisticsTargetAvailable = true };
        var partialState = default(AriaPreparationState);
        var partial = AriaExpandedPreparationPolicy.Supply(facts, ref partialState);
        if (output.TargetId != 101 || plan.Intent != AriaSkirmishIntent.DefendBase ||
            touch.Actions != 7 || partial != AriaPreparationAction.RecruitLogisticsTruck)
            throw new InvalidOperationException("Original home-defense priority or partial supply decision changed");
        Console.WriteLine("[S003OriginalAriaStepFixture] result=Passed original=CancelPlacement partial=RecruitLogisticsTruck fixtureOnly=1 nativeInput=0 readiness=0");
    }
}
