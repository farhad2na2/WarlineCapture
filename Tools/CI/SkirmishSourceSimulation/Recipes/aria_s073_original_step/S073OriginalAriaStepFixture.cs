using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;

internal static class S073OriginalAriaStepFixture
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static AriaSkirmishObservation LegacyView()
    {
        return new AriaSkirmishObservation
        {
            SourceScenarioIndex = 3,
            Active = true,
            Frame = 11,
            Time = 240f,
            ExpandedSession = false,
            ExpandedBaseAssault = false,
            AvailableSquads = 1,
            Infantry = 24,
            AssaultAtBase = true,
            EnemyDesignatedAlive = true,
            Squad0 = new AriaTouchTarget { Available = true, Id = 410, Position = new UnityEngine.Vector2(0.4f, 0.6f) },
            EnemyBase = new AriaTouchTarget { Available = true, Id = 830, Position = new UnityEngine.Vector2(0.7f, 0.3f) },
            Attack = new AriaTouchTarget { Available = true, Id = 510, Position = new UnityEngine.Vector2(0.8f, 0.8f) }
        };
    }

    private static AriaSkirmishPlanComponent AssaultPlan() => new AriaSkirmishPlanComponent
    {
        AssaultStarted = 1,
        GroupStage = 4,
        Slot = 0
    };

    public static void Main()
    {
        var s073 = LegacyView();
        Require(!AriaS002NativeAdapter.CanHandle(s073),
            "Scenario-3 legacy S073 must not dispatch to the scenario-4 expanded adapter");

        var understrength = LegacyView();
        understrength.Infantry = 9;
        var plan = AssaultPlan();
        var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 6 };
        var output = default(AriaPlayObservationComponent);
        AriaSkirmishPlanSystem.Step(understrength, ref plan, ref touch, ref output);
        Require(plan.Intent == AriaSkirmishIntent.Recruit && output.Kind == AriaPlayObservationKind.Waiting &&
                touch.Phase == AriaPlayPhase.Observing && touch.Actions == 6,
            "Observed understrength legacy fixture should wait for the original recruit policy without inventing recruitment");

        plan = AssaultPlan();
        touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 6 };
        output = default(AriaPlayObservationComponent);
        AriaSkirmishPlanSystem.Step(s073, ref plan, ref touch, ref output);
        Require(plan.Intent == AriaSkirmishIntent.SelectSquad,
            "Original legacy planner did not choose its live Squad0 selector; intent=" + plan.Intent +
            " kind=" + output.Kind + " target=" + output.TargetId + " group=" + plan.GroupStage +
            " assault=" + plan.AssaultStarted + " slot=" + plan.Slot);
        Require(output.Kind == AriaPlayObservationKind.Control && output.TargetId == 410,
            "Original legacy planner did not emit the visible Squad0 control");
        Require(touch.Phase == AriaPlayPhase.Observing && touch.Actions == 6,
            "Planner fixture must preserve the actual observing phase and action count");

        // This is a separately observed selection/control fixture. It does not claim
        // the previous emitted selector was accepted by the player or touch driver.
        var selectedObservation = LegacyView();
        selectedObservation.SelectionVisible = true;
        selectedObservation.SelectedSlot = 0;
        selectedObservation.SelectedCount = 4;
        selectedObservation.AttackMode = true;
        plan = AssaultPlan();
        touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 6 };
        output = default;
        AriaSkirmishPlanSystem.Step(selectedObservation, ref plan, ref touch, ref output);
        Require(plan.Intent == AriaSkirmishIntent.TargetBase &&
                output.Kind == AriaPlayObservationKind.WorldTarget && output.TargetId == 830,
            "Observed selected legacy squad must target the currently visible designated base");
        Require(touch.Phase == AriaPlayPhase.Observing && touch.Actions == 6,
            "Target emission must not invent an accepted touch or action increment");

        var hiddenControl = LegacyView();
        hiddenControl.Squad0 = default;
        plan = AssaultPlan();
        touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing, Actions = 6 };
        output = default;
        AriaSkirmishPlanSystem.Step(hiddenControl, ref plan, ref touch, ref output);
        Require(plan.Intent == AriaSkirmishIntent.SelectSquad && output.Kind == AriaPlayObservationKind.Unavailable,
            "Hidden Squad0 control must remain unavailable rather than becoming a target");
        Require(touch.Phase == AriaPlayPhase.Observing && touch.Actions == 6,
            "Unavailable control must not fabricate touch progress");

        var wrongIndex = LegacyView();
        wrongIndex.SourceScenarioIndex = 3;
        wrongIndex.ExpandedSession = true;
        wrongIndex.ExpandedBaseAssault = true;
        Require(!AriaS002NativeAdapter.CanHandle(wrongIndex),
            "Scenario-4 expanded shape without S002's exact native gate inputs must remain out of adapter scope");

        var manual = LegacyView();
        plan = AssaultPlan();
        touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Manual, Actions = 6 };
        output = default;
        AriaSkirmishPlanSystem.Step(manual, ref plan, ref touch, ref output);
        Require(output.Kind == AriaPlayObservationKind.Waiting &&
                touch.Phase == AriaPlayPhase.Manual && touch.Actions == 6,
            "Manual phase must hold the original planner without requesting a touch");

        var touching = LegacyView();
        plan = AssaultPlan();
        touch = new AriaPlaySessionComponent
        {
            Phase = AriaPlayPhase.Touching, TargetId = 410, Actions = 6,
            GestureRequested = 1, Pressed = 1
        };
        output = default;
        AriaSkirmishPlanSystem.Step(touching, ref plan, ref touch, ref output);
        Require(output.Kind == AriaPlayObservationKind.Waiting &&
                touch.Phase == AriaPlayPhase.Touching && touch.TargetId == 410 &&
                touch.Actions == 6 && touch.GestureRequested == 1 && touch.Pressed == 1,
            "Touching lifecycle state must be preserved until the real touch owner reports completion");

        Console.WriteLine("[S073OriginalAriaStepFixture] result=Passed scenario=3 adapter=Bypassed squad0=VisibleControl base=VisibleWorldTarget hiddenSquad0=Unavailable manual=Waiting touching=Preserved actionAcceptance=Unclaimed");
    }
}
