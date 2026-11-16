using System;
using System.Collections.Generic;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Explicit synthetic ECS fixtures for fail-closed source-host diagnostic gates.
// They never create a gameplay world or claim readiness. One complete synthetic tuple invokes the original planner on copies only.
internal static class S073OriginalAriaStepPositiveControls
{
    private const string OptIn = "WARLINE_S073_ORIGINAL_ARIA_STEP_PROBE";

    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("S073 original ARIA source-host controls: " + message);
    }

    // Compare instance fields only: Unity presentation structs expose computed
    // properties that are not safe to serialize outside Unity. Unknown non-null
    // reference objects are rejected rather than treated as immutable snapshots.
    private static bool FieldsEqual(object left, object right)
    {
        if (left == null || right == null) return left == null && right == null;
        Type type = left.GetType();
        if (type != right.GetType()) return false;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return left.Equals(right);
        if (!type.IsValueType) throw new NotSupportedException("Mutable reference in synthetic ECS snapshot: " + type.FullName);
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            if (!FieldsEqual(field.GetValue(left), field.GetValue(right))) return false;
        return true;
    }

    private static int Count<T>(EntityManager em) where T : struct, IComponentData
    {
        using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    private static string Snapshot(EntityManager em)
    {
        int rootCount = Count<UiShellRootComponent>(em);
        int ownerCount = Count<AriaSkirmishObservationOwner>(em);
        var rows = new List<string>
        {
            // Fixture rows are boundaries or storage-owner entities only.
            "entities=" + (rootCount + ownerCount),
            "roots=" + rootCount,
            "states=" + Count<UiShellStateComponent>(em),
            "owners=" + ownerCount,
            "views=" + Count<AriaSkirmishObservationComponent>(em),
            "plans=" + Count<AriaSkirmishPlanComponent>(em),
            "sessions=" + Count<AriaPlaySessionComponent>(em),
            "outputs=" + Count<AriaPlayObservationComponent>(em)
        };
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>()))
        using (var entities = query.ToEntityArray(Allocator.Temp))
            foreach (Entity entity in entities)
            {
                UiShellStateComponent state = em.GetComponentData<UiShellStateComponent>(entity);
                rows.Add("root=" + entity + ":" + state.ActiveRoute + ":" + state.TransitionSequenceId);
                if (em.HasComponent<AriaPlaySessionComponent>(entity))
                {
                    AriaPlaySessionComponent session = em.GetComponentData<AriaPlaySessionComponent>(entity);
                    rows.Add("session=" + session.Phase + ":" + session.Actions + ":" + session.TargetId + ":" + session.GestureRequested);
                }
                if (em.HasComponent<AriaPlayObservationComponent>(entity))
                {
                    AriaPlayObservationComponent observation = em.GetComponentData<AriaPlayObservationComponent>(entity);
                    rows.Add("play-observation=" + observation.Kind + ":" + observation.TargetId + ":" + observation.Frame);
                }
            }
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationOwner>()))
        using (var entities = query.ToEntityArray(Allocator.Temp))
            foreach (Entity entity in entities)
            {
                AriaSkirmishObservationOwner owner = em.GetComponentData<AriaSkirmishObservationOwner>(entity);
                rows.Add("owner=" + entity + ":" + owner.Boundary + ":" + owner.TransitionSequence);
            }
        return string.Join("|", rows);
    }

    private static JObject Capture(EntityManager em)
    {
        Environment.SetEnvironmentVariable(OptIn, "1");
        var match = new SkirmishMatchState
        {
            ScenarioIndex = 3,
            Phase = SkirmishPhase.Playing,
            Outcome = SkirmishOutcome.None
        };
        return SourceS073OriginalAriaStepDiagnostic.Capture(em, match, OptIn);
    }

    private static void AssertUnavailableWithoutMutation(EntityManager em, string reason)
    {
        string before = Snapshot(em);
        JObject result = Capture(em);
        string after = Snapshot(em);
        Need((string)result["status"] == "Unavailable", "status did not fail closed for " + reason);
        Need((string)result["unavailable_reason"] == reason, "unexpected fail-closed reason for " + reason);
        Need((bool?)result["step_invoked"] == false, "Step ran for unavailable tuple " + reason);
        Need((bool?)result["gameplay_state_changed"] == false &&
             (bool?)result["actions_synthesized"] == false &&
             (bool?)result["touch_or_input_owner_invoked"] == false &&
             (bool?)result["planner_suggestion_accepted"] == false,
            "diagnostic reported an action or mutation for " + reason);
        Need((bool?)result["native_readiness_established"] == false &&
             (bool?)result["mission_ready"] == false,
            "readiness was claimed for " + reason);
        Need((string)result["native_control_availability"] == "NotObserved",
            "controls were reported available without a complete original UI tuple");
        Need(before == after, "EntityManager state changed for " + reason);
    }

    private static Entity CreateBoundary(EntityManager em, int sequence)
    {
        Entity boundary = em.CreateEntity(typeof(UiShellRootComponent), typeof(UiShellStateComponent),
            typeof(AriaPlaySessionComponent), typeof(AriaPlayObservationComponent));
        em.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            TransitionSequenceId = sequence
        });
        em.SetComponentData(boundary, new AriaPlaySessionComponent
        {
            Phase = AriaPlayPhase.Manual,
            Actions = 17,
            TargetId = 234
        });
        em.SetComponentData(boundary, new AriaPlayObservationComponent
        {
            Kind = AriaPlayObservationKind.Waiting,
            TargetId = 345,
            Frame = 12
        });
        return boundary;
    }

    private static Entity CreateOwner(EntityManager em, Entity boundary, int sequence)
    {
        Entity storage = em.CreateEntity(typeof(AriaSkirmishObservationOwner));
        em.SetComponentData(storage, new AriaSkirmishObservationOwner
        {
            Boundary = boundary,
            TransitionSequence = sequence
        });
        return storage;
    }

    private static EntityManager NewManager()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        return em;
    }

    public static void Main()
    {
        Environment.SetEnvironmentVariable(OptIn, "1");
        var absent = NewManager();
        AssertUnavailableWithoutMutation(absent, "OriginalUiRootAbsentInSourceWorld");

        var partial = NewManager();
        Entity partialBoundary = CreateBoundary(partial, 8);
        CreateOwner(partial, partialBoundary, 8); // no source observation or plan
        AssertUnavailableWithoutMutation(partial, "OriginalAriaLifecycleTupleIncomplete");

        var ambiguous = NewManager();
        Entity ambiguousBoundary = CreateBoundary(ambiguous, 9);
        CreateOwner(ambiguous, ambiguousBoundary, 9);
        CreateOwner(ambiguous, ambiguousBoundary, 9);
        AssertUnavailableWithoutMutation(ambiguous, "AmbiguousOriginalAriaObservationOwners");

        var stale = NewManager();
        Entity staleBoundary = CreateBoundary(stale, 10);
        CreateOwner(stale, staleBoundary, 9);
        AssertUnavailableWithoutMutation(stale, "StaleOriginalAriaObservationOwner");

        var complete = NewManager();
        Entity completeBoundary = CreateBoundary(complete, 11);
        Entity completeOwner = CreateOwner(complete, completeBoundary, 11);
        complete.AddComponentData(completeOwner, new AriaSkirmishObservationComponent {
            Value = new AriaSkirmishObservation { Active = true, SourceScenarioIndex = 3, Infantry = 9, Frame = 1, Time = 1f }
        });
        complete.AddComponentData(completeOwner, new AriaSkirmishPlanComponent { Intent = AriaSkirmishIntent.Recruit });
        string beforeComplete = Snapshot(complete);
        var beforeSession = complete.GetComponentData<AriaPlaySessionComponent>(completeBoundary);
        var beforeOutput = complete.GetComponentData<AriaPlayObservationComponent>(completeBoundary);
        var beforeView = complete.GetComponentData<AriaSkirmishObservationComponent>(completeOwner);
        var beforePlan = complete.GetComponentData<AriaSkirmishPlanComponent>(completeOwner);
        Need(FieldsEqual(beforeView, complete.GetComponentData<AriaSkirmishObservationComponent>(completeOwner)), "view differs before planner");
        var changedView=beforeView;changedView.Value.Infantry++;
        var changedPlan=beforePlan;changedPlan.Slot++;
        var changedSession=beforeSession;changedSession.Actions++;
        var changedOutput=beforeOutput;changedOutput.Frame++;
        Need(!FieldsEqual(beforeView,changedView) && !FieldsEqual(beforePlan,changedPlan) &&
             !FieldsEqual(beforeSession,changedSession) && !FieldsEqual(beforeOutput,changedOutput),
             "field-only snapshot comparison missed a synthetic mutation");
        JObject observed = Capture(complete);
        Need((string)observed["status"] == "OriginalStepEvaluatedFromCopiedSourceState" && (bool?)observed["step_invoked"] == true,
            "complete synthetic tuple did not invoke the original planner");
        Need((bool?)observed["planner_suggestion_accepted"] == false && (bool?)observed["touch_or_input_owner_invoked"] == false &&
             (bool?)observed["gameplay_state_changed"] == false && (bool?)observed["mission_ready"] == false,
            "copy-only fixture claimed an action or readiness");
        Need(beforeComplete == Snapshot(complete), "planner diagnostic changed ECS snapshot");
        Need(FieldsEqual(beforeSession, complete.GetComponentData<AriaPlaySessionComponent>(completeBoundary)), "planner diagnostic changed session");
        Need(FieldsEqual(beforeOutput, complete.GetComponentData<AriaPlayObservationComponent>(completeBoundary)), "planner diagnostic changed output");
        Need(FieldsEqual(beforeView, complete.GetComponentData<AriaSkirmishObservationComponent>(completeOwner)), "planner diagnostic changed view");
        Need(FieldsEqual(beforePlan, complete.GetComponentData<AriaSkirmishPlanComponent>(completeOwner)), "planner diagnostic changed plan");

        Environment.SetEnvironmentVariable(OptIn, null);
        Console.WriteLine("[S073OriginalAriaStepPositiveControls] result=Passed cases=5 stepInvoked=1 mutation=0 readiness=0 gameplay=0 unity=0 syntheticFixtures=1");
    }
}
