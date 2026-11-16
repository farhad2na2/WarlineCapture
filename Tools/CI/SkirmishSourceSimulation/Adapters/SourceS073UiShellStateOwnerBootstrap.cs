using System;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Source-host glue only. All shell/root/component creation is performed by the
// exact extracted UiShellStateSystem.OnCreate and EnsureBoundary methods.
internal static class SourceS073UiShellStateOwnerBootstrap
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("S073 UI shell owner boundary: " + message);
    }

    private static int Count<T>(EntityManager em) where T : struct, IComponentData
    {
        using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    internal static JObject Initialize(EntityManager em, AutonomousWorld host)
    {
        Need(em != null && host != null && ReferenceEquals(host.Entities, em),
             "the retained S073 host EntityManager must be the owner target");
        int rootsBefore = Count<UiShellRootComponent>(em);
        int statesBefore = Count<UiShellStateComponent>(em);
        int sessionsBefore = Count<SkirmishMatchState>(em);
        int mapsBefore = Count<ActiveOperationMapComponent>(em);
        int ariaSessionsBefore = Count<AriaPlaySessionComponent>(em);
        int ariaObservationsBefore = Count<AriaPlayObservationComponent>(em);
        Need(rootsBefore == 0 && statesBefore == 0,
             "fresh retained S073 owner boundary required before legacy startup");
        Need(sessionsBefore == 0 && mapsBefore == 0,
             "call site must precede legacy match/session and operation-map startup");
        Need(ariaSessionsBefore == 0 && ariaObservationsBefore == 0,
             "UI ARIA state must be absent before shell initialization");

        // OnCreate owns the root and all default data. The S073 profile supplies
        // an entity identity adapter already bound to this same retained host.
        var state = new SystemState { EntityManager = em, Enabled = true };
        var owner = new UiShellStateSystem();
        state.Begin();
        try { owner.OnCreate(ref state); }
        finally { state.End(); }

        Need(!state.Enabled, "the original one-shot owner must disable itself after OnCreate");
        Need(Count<UiShellRootComponent>(em) == 1 && Count<UiShellStateComponent>(em) == 1,
             "original OnCreate must create one shell root/state in the retained host");
        Entity root;
        using (var roots = em.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>()))
            root = roots.GetSingletonEntity();
        Need(em.GetName(root) == "UiShellState", "original shell root identity/name changed");
        UiShellStateComponent shell = em.GetComponentData<UiShellStateComponent>(root);
        Need(shell.CurrentMode == UiShellMode.None && shell.ActiveRoute == UIRoute.Splash &&
             shell.Phase == UiShellTransitionPhase.Idle && shell.TransitionSequenceId == 0 &&
             shell.IsTransitionRunning == 0,
             "original UiShellState defaults changed");
        UiShellStartupDispositionComponent disposition = em.GetComponentData<UiShellStartupDispositionComponent>(root);
        Need(disposition.Value == UiShellStartupDisposition.Pending,
             "original startup disposition default changed");
        UiShellLoadingProgressComponent loading = em.GetComponentData<UiShellLoadingProgressComponent>(root);
        Need(loading.Progress01 == 0f && loading.Status.ToString() == "Starting" && loading.IsComplete == 0,
             "original loading defaults changed");

        Need(em.HasBuffer<UiShellRouteRequestComponent>(root) && em.GetBuffer<UiShellRouteRequestComponent>(root).Length == 0 &&
             em.HasBuffer<UiShellRouteHistoryComponent>(root) && em.GetBuffer<UiShellRouteHistoryComponent>(root).Length == 0 &&
             em.HasBuffer<UiShellPresentationCommandComponent>(root) && em.GetBuffer<UiShellPresentationCommandComponent>(root).Length == 0 &&
             em.HasBuffer<UiShellTransitionCompleteComponent>(root) && em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length == 0 &&
             em.HasBuffer<UiShellLoadingProgressRequestComponent>(root) && em.GetBuffer<UiShellLoadingProgressRequestComponent>(root).Length == 0,
             "shell initialization created route, presentation, completion, or loading requests");
        Need(Count<AriaPlaySessionComponent>(em) == ariaSessionsBefore &&
             Count<AriaPlayObservationComponent>(em) == ariaObservationsBefore &&
             !em.HasComponent<AriaPlaySessionComponent>(root) && !em.HasComponent<AriaPlayObservationComponent>(root),
             "shell owner created or changed UI ARIA session/observation state");
        Need(Count<SkirmishMatchState>(em) == sessionsBefore && Count<ActiveOperationMapComponent>(em) == mapsBefore,
             "shell owner changed mission/session bootstrap state");

        return new JObject
        {
            ["kind"] = "S073RetainedSourceUiShellOwnerInitialization",
            ["scope"] = "NonNativeS073SourceHost_OriginalOnCreateOnly_NoFlowOrUIReadinessClaim",
            ["owner"] = "UiShellStateSystem.OnCreate/EnsureBoundary",
            ["same_retained_entity_manager"] = ReferenceEquals(host.Entities, em),
            ["owner_disabled_after_create"] = !state.Enabled,
            ["pre_legacy_startup"] = sessionsBefore == 0 && mapsBefore == 0,
            ["root_entity"] = root.ToString(),
            ["root_entity_index"] = root.Index,
            ["root_count_before"] = rootsBefore,
            ["root_count_after"] = Count<UiShellRootComponent>(em),
            ["mode"] = shell.CurrentMode.ToString(),
            ["route"] = shell.ActiveRoute.ToString(),
            ["phase"] = shell.Phase.ToString(),
            ["transition_sequence"] = shell.TransitionSequenceId,
            ["transition_running"] = shell.IsTransitionRunning != 0,
            ["startup_disposition"] = disposition.Value.ToString(),
            ["loading_status"] = loading.Status.ToString(),
            ["route_requests"] = 0,
            ["presentation_commands"] = 0,
            ["transition_completions"] = 0,
            ["aria_session_count_before"] = ariaSessionsBefore,
            ["aria_session_count_after"] = Count<AriaPlaySessionComponent>(em),
            ["aria_observation_count_before"] = ariaObservationsBefore,
            ["aria_observation_count_after"] = Count<AriaPlayObservationComponent>(em),
            ["gateway_registration_invoked"] = false,
            ["flow_or_owner_update_invoked"] = false,
            ["unity_native_runtime"] = false,
            ["player_or_ui_readiness"] = false,
            ["early_entity_id_shift_expected"] = true
        };
    }
}
