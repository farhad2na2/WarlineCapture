using System;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Optional source-host bridge from the already initialized S073 shell owner
// profile to the original flow system. It invokes OnCreate and one no-input
// OnUpdate; it does not synthesize presentation/input/completion.
internal static class SourceS073UiShellFlowBootstrap
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("S073 UI shell flow boundary: " + message);
    }

    internal static JObject Initialize(EntityManager em, AutonomousWorld host, JObject ownerInitialization)
    {
        Need(em != null && host != null && ReferenceEquals(host.Entities, em),
             "the retained S073 host EntityManager must be the flow target");
        Need(ownerInitialization != null &&
             (string)ownerInitialization["kind"] == "S073RetainedSourceUiShellOwnerInitialization" &&
             (bool?)ownerInitialization["same_retained_entity_manager"] == true &&
             (bool?)ownerInitialization["owner_disabled_after_create"] == true,
             "successful original S073 shell owner initialization is required");
        Need((string)ownerInitialization["route"] == UIRoute.Splash.ToString() &&
             (string)ownerInitialization["mode"] == UiShellMode.None.ToString() &&
             (int?)ownerInitialization["route_requests"] == 0 &&
             (int?)ownerInitialization["presentation_commands"] == 0 &&
             (int?)ownerInitialization["transition_completions"] == 0,
             "owner must retain its default route/mode and empty request/presentation buffers");

        using var boundary = em.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadOnly<UiShellStateComponent>(),
            ComponentType.ReadOnly<UiShellLoadingProgressComponent>(),
            ComponentType.ReadOnly<UiShellLoadingProgressRequestComponent>(),
            ComponentType.ReadOnly<MatchIntroTransitionComponent>(),
            ComponentType.ReadOnly<UiShellRouteRequestComponent>(),
            ComponentType.ReadOnly<UiShellRouteHistoryComponent>(),
            ComponentType.ReadOnly<UiShellActivePopupComponent>(),
            ComponentType.ReadOnly<UiShellPopupRequestComponent>(),
            ComponentType.ReadOnly<UiShellPresentationCommandComponent>(),
            ComponentType.ReadOnly<UiShellTransitionCompleteComponent>());
        int boundaryCount = boundary.CalculateEntityCount();
        Need(boundaryCount == 1,
             "the original shell owner must have created exactly one complete flow presentation boundary");

        Entity root = boundary.GetSingletonEntity();
        Need(em.HasComponent<UiShellStartupDispositionComponent>(root) &&
             em.GetComponentData<UiShellStartupDispositionComponent>(root).Value == UiShellStartupDisposition.Pending,
             "the original owner must have retained Pending startup disposition");
        UiShellStateComponent beforeFlow = em.GetComponentData<UiShellStateComponent>(root);
        Need(beforeFlow.CurrentMode == UiShellMode.None && beforeFlow.ActiveRoute == UIRoute.Splash &&
             beforeFlow.Phase == UiShellTransitionPhase.Idle && beforeFlow.IsTransitionRunning == 0,
             "flow admission requires the untouched original Splash/None idle state");
        int routeRequests = em.GetBuffer<UiShellRouteRequestComponent>(root).Length;
        int routeHistory = em.GetBuffer<UiShellRouteHistoryComponent>(root).Length;
        int loadingRequests = em.GetBuffer<UiShellLoadingProgressRequestComponent>(root).Length;
        int popupRequests = em.GetBuffer<UiShellPopupRequestComponent>(root).Length;
        int presentationCommands = em.GetBuffer<UiShellPresentationCommandComponent>(root).Length;
        int completions = em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length;
        Need(routeRequests == 0 && routeHistory == 0 && loadingRequests == 0 && popupRequests == 0 &&
             presentationCommands == 0 && completions == 0,
             "source-host admission requires empty route, loading, popup, presentation, and completion buffers");

        int audioBefore = SourceS073UiShellFlowBoundary.AudioCalls;
        int musicBefore = SourceS073UiShellFlowBoundary.MusicCalls;
        int ensureAudioBefore = SourceS073UiShellFlowBoundary.EnsureAudioCalls;
        int gatewayBefore = SourceS073UiShellFlowBoundary.GatewayCalls;
        var state = new SystemState { EntityManager = em, Enabled = true };
        var flow = new UiShellFlowSystem();
        flow.OnCreate(ref state);
        Need(SourceS073UiShellFlowBoundary.GatewayCalls == gatewayBefore + 1,
             "original flow gateway registration call must reach only the explicit source-host boundary");
        flow.OnUpdate(ref state);

        UiShellStateComponent afterFlow = em.GetComponentData<UiShellStateComponent>(root);
        UiShellStartupDispositionComponent dispositionAfterFlow = em.GetComponentData<UiShellStartupDispositionComponent>(root);
        Need(afterFlow.CurrentMode == UiShellMode.None && afterFlow.ActiveRoute == UIRoute.Splash &&
             afterFlow.Phase == UiShellTransitionPhase.Idle && afterFlow.IsTransitionRunning == 0 &&
             dispositionAfterFlow.Value == UiShellStartupDisposition.Pending,
             "one no-input original flow update must preserve Splash/Pending without starting a transition");
        Need(routeRequests == em.GetBuffer<UiShellRouteRequestComponent>(root).Length &&
             routeHistory == em.GetBuffer<UiShellRouteHistoryComponent>(root).Length &&
             loadingRequests == em.GetBuffer<UiShellLoadingProgressRequestComponent>(root).Length &&
             popupRequests == em.GetBuffer<UiShellPopupRequestComponent>(root).Length &&
             presentationCommands == em.GetBuffer<UiShellPresentationCommandComponent>(root).Length &&
             completions == em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length,
             "flow OnCreate/OnUpdate must not mutate shell request or presentation buffers");
        int audioAfter = SourceS073UiShellFlowBoundary.AudioCalls;
        int musicAfter = SourceS073UiShellFlowBoundary.MusicCalls;
        int ensureAudioAfter = SourceS073UiShellFlowBoundary.EnsureAudioCalls;
        Need(audioAfter == audioBefore && musicAfter == musicBefore && ensureAudioAfter == ensureAudioBefore,
             "no UI audio bridge, music enqueue, or audio-entity ensure boundary may be reached");
        return new JObject
        {
            ["kind"] = "S073RetainedSourceUiShellFlowNoInputUpdate",
            ["scope"] = "NonNativeS073SourceHost_OriginalFlowOneNoInputUpdate_NoReadinessClaim",
            ["owner"] = "UiShellFlowSystem.OnCreate+OnUpdate",
            ["flow_update_invoked"] = true,
            ["flow_update_count"] = 1,
            ["same_retained_entity_manager"] = ReferenceEquals(host.Entities, em),
            ["complete_presentation_boundary_count"] = boundaryCount,
            ["gateway_registration_boundary_calls"] = SourceS073UiShellFlowBoundary.GatewayCalls - gatewayBefore,
            ["mode_after_flow"] = afterFlow.CurrentMode.ToString(),
            ["route_after_flow"] = afterFlow.ActiveRoute.ToString(),
            ["startup_disposition_after_flow"] = dispositionAfterFlow.Value.ToString(),
            ["route_requests"] = routeRequests,
            ["route_history"] = routeHistory,
            ["loading_requests"] = loadingRequests,
            ["popup_requests"] = popupRequests,
            ["presentation_commands"] = presentationCommands,
            ["transition_completions"] = completions,
            ["ui_audio_bridge_calls"] = audioAfter - audioBefore,
            ["music_enqueue_calls"] = musicAfter - musicBefore,
            ["audio_entity_ensure_calls"] = ensureAudioAfter - ensureAudioBefore,
            ["input_injected"] = false,
            ["route_requested"] = false,
            ["player_or_ui_readiness"] = false,
            ["unity_native_runtime"] = false
        };
    }
}
