using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Opt-in diagnostic projection only. It never creates or writes UI/ARIA state,
// invokes input owners, or treats a planner suggestion as an accepted action.
internal static class SourceS073OriginalAriaStepDiagnostic
{
    internal static JObject Capture(EntityManager em, SkirmishMatchState match, string optInName)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(optInName), "1", StringComparison.Ordinal))
            return null;

        var result = new JObject
        {
            ["kind"] = "S073OriginalAriaStepSameWorldDiagnostic",
            ["schema_version"] = 1,
            ["scope"] = "OptInReadOnlyProjection_OriginalUiOwnedStateOnly",
            ["status"] = "Unavailable",
            ["step_invoked"] = false,
            ["native_control_availability"] = "NotObserved",
            ["native_readiness_established"] = false,
            ["mission_ready"] = false,
            ["actions_synthesized"] = false,
            ["gameplay_state_changed"] = false,
            ["touch_or_input_owner_invoked"] = false,
            ["planner_suggestion_accepted"] = false
        };

        if (em == null || em.World == null || !em.World.IsCreated)
        {
            result["unavailable_reason"] = "EntityManagerUnavailable";
            return result;
        }

        using var roots = em.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
        using var rootEntities = roots.ToEntityArray(Allocator.Temp);
        result["ui_root_count"] = rootEntities.Length;
        if (rootEntities.Length != 1)
        {
            result["unavailable_reason"] = rootEntities.Length == 0
                ? "OriginalUiRootAbsentInSourceWorld" : "AmbiguousOriginalUiRoots";
            return result;
        }

        Entity boundary = rootEntities[0];
        bool hasShellState = em.HasComponent<UiShellStateComponent>(boundary);
        result["ui_shell_state_present"] = hasShellState;
        if (!hasShellState)
        {
            result["unavailable_reason"] = "UiShellStateAbsent";
            return result;
        }
        UiShellStateComponent shell = em.GetComponentData<UiShellStateComponent>(boundary);
        bool matchRoute = shell.ActiveRoute == UIRoute.Match;
        result["match_route_observed"] = matchRoute;
        result["transition_sequence"] = shell.TransitionSequenceId;
        if (!matchRoute)
        {
            result["unavailable_reason"] = "UiRootNotOnMatchRoute";
            return result;
        }

        Entity storage = Entity.Null;
        int linkedOwnerCount = 0;
        using (var owners = em.CreateEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationOwner>()))
        using (var ownerEntities = owners.ToEntityArray(Allocator.Temp))
        {
            foreach (Entity candidate in ownerEntities)
            {
                AriaSkirmishObservationOwner owner = em.GetComponentData<AriaSkirmishObservationOwner>(candidate);
                if (owner.Boundary != boundary) continue;
                linkedOwnerCount++;
                storage = candidate;
                if (owner.TransitionSequence != shell.TransitionSequenceId)
                {
                    result["owner_transition_matches"] = false;
                }
            }
        }
        result["linked_observation_owner_count"] = linkedOwnerCount;
        if (linkedOwnerCount != 1)
        {
            result["unavailable_reason"] = linkedOwnerCount == 0
                ? "OriginalAriaObservationOwnerAbsent" : "AmbiguousOriginalAriaObservationOwners";
            return result;
        }
        AriaSkirmishObservationOwner linkedOwner = em.GetComponentData<AriaSkirmishObservationOwner>(storage);
        bool ownerSequenceMatches = linkedOwner.TransitionSequence == shell.TransitionSequenceId;
        result["owner_transition_matches"] = ownerSequenceMatches;
        if (!ownerSequenceMatches)
        {
            result["unavailable_reason"] = "StaleOriginalAriaObservationOwner";
            return result;
        }

        bool storageHasView = em.HasComponent<AriaSkirmishObservationComponent>(storage);
        bool storageHasPlan = em.HasComponent<AriaSkirmishPlanComponent>(storage);
        bool boundaryHasSession = em.HasComponent<AriaPlaySessionComponent>(boundary);
        bool boundaryHasOutput = em.HasComponent<AriaPlayObservationComponent>(boundary);
        result["storage_observation_present"] = storageHasView;
        result["storage_plan_present"] = storageHasPlan;
        result["boundary_session_present"] = boundaryHasSession;
        result["boundary_play_observation_present"] = boundaryHasOutput;
        if (!storageHasView || !storageHasPlan || !boundaryHasSession || !boundaryHasOutput)
        {
            result["unavailable_reason"] = "OriginalAriaLifecycleTupleIncomplete";
            return result;
        }

        AriaSkirmishObservation view = em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value;
        AriaSkirmishPlanComponent plan = em.GetComponentData<AriaSkirmishPlanComponent>(storage);
        AriaPlaySessionComponent session = em.GetComponentData<AriaPlaySessionComponent>(boundary);
        AriaPlayObservationComponent priorOutput = em.GetComponentData<AriaPlayObservationComponent>(boundary);
        result["view_active"] = view.Active;
        result["view_scenario_index"] = view.SourceScenarioIndex;
        result["match_phase"] = match.Phase.ToString();
        if (!view.Active || view.SourceScenarioIndex != 3 || match.ScenarioIndex != 3 || match.Phase != SkirmishPhase.Playing ||
            match.Outcome != SkirmishOutcome.None || AriaS002NativeAdapter.CanHandle(view))
        {
            result["unavailable_reason"] = "OriginalS073LegacyStepPreconditionsNotMet";
            return result;
        }

        // These availability bits come directly from the actual published view.
        // With no original UI lifecycle tuple, no control is described as available.
        result["native_control_availability"] = "SourceUiOwnedObservation_NotNative";
        result["observed_controls"] = new JObject
        {
            ["squad0"] = view.Squad0.Available,
            ["squad1"] = view.Squad1.Available,
            ["squad2"] = view.Squad2.Available,
            ["squad3"] = view.Squad3.Available,
            ["squad4"] = view.Squad4.Available,
            ["attack"] = view.Attack.Available,
            ["enemy_base"] = view.EnemyBase.Available,
            ["controls_are_ui_observations"] = true
        };
        result["observed_source_state"] = new JObject
        {
            ["plan_intent"] = plan.Intent.ToString(),
            ["plan_slot"] = plan.Slot,
            ["plan_group_stage"] = plan.GroupStage,
            ["plan_assault_started"] = plan.AssaultStarted,
            ["session_phase"] = session.Phase.ToString(),
            ["session_actions"] = session.Actions,
            ["session_target_id"] = session.TargetId,
            ["prior_output_kind"] = priorOutput.Kind.ToString(),
            ["prior_output_target_id"] = priorOutput.TargetId
        };

        AriaSkirmishPlanComponent projectedPlan = plan;
        AriaPlaySessionComponent projectedSession = session;
        AriaPlayObservationComponent projectedOutput = priorOutput;
        AriaSkirmishPlanSystem.Step(view, ref projectedPlan, ref projectedSession, ref projectedOutput);
        result["status"] = "OriginalStepEvaluatedFromCopiedSourceState";
        result["step_invoked"] = true;
        result["planner_suggestion_accepted"] = false;
        result["projected_step"] = new JObject
        {
            ["intent"] = projectedPlan.Intent.ToString(),
            ["slot"] = projectedPlan.Slot,
            ["group_stage"] = projectedPlan.GroupStage,
            ["assault_started"] = projectedPlan.AssaultStarted,
            ["session_phase"] = projectedSession.Phase.ToString(),
            ["session_target_id"] = projectedSession.TargetId,
            ["session_actions"] = projectedSession.Actions,
            ["gesture_requested"] = projectedSession.GestureRequested != 0,
            ["output_kind"] = projectedOutput.Kind.ToString(),
            ["output_target_id"] = projectedOutput.TargetId,
            ["output_position"] = new JArray(projectedOutput.Position.x, projectedOutput.Position.y)
        };
        return result;
    }
}
