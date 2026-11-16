using System;
using System.Collections.Generic;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class SourceS073UiShellFlowIntegrationFixture
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("S073 shell flow integration fixture: " + message);
    }

    public static int Main()
    {
        int nextEntityId = 1;
        var em = new EntityManager
        {
            LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = nextEntityId++, Version = 1 }
        };
        using var host = new AutonomousWorld(em, new Dictionary<int, RuntimeBuildingEntity>());
        JObject owner = SourceS073UiShellStateOwnerBootstrap.Initialize(em, host);
        JObject flow = SourceS073UiShellFlowBootstrap.Initialize(em, host, owner);

        Need((string)flow["kind"] == "S073RetainedSourceUiShellFlowNoInputUpdate", "adapter receipt kind missing");
        Need((int?)flow["flow_update_count"] == 1 && (bool?)flow["flow_update_invoked"] == true,
             "fixture must invoke one original no-input flow update");
        Need((string)flow["route_after_flow"] == "Splash" && (string)flow["mode_after_flow"] == "None" &&
             (string)flow["startup_disposition_after_flow"] == "Pending",
             "original flow update must preserve the retained S073 Splash/Pending shell state");
        Need((int?)flow["route_requests"] == 0 && (int?)flow["loading_requests"] == 0 &&
             (int?)flow["popup_requests"] == 0 && (int?)flow["presentation_commands"] == 0 &&
             (int?)flow["transition_completions"] == 0,
             "fixture must not fabricate routes, progress, popup requests, commands, or completions");
        Need((int?)flow["ui_audio_bridge_calls"] == 0 && (int?)flow["music_enqueue_calls"] == 0 &&
             (int?)flow["audio_entity_ensure_calls"] == 0,
             "captured audio/music side-effect boundaries must remain untouched");
        Need((int?)flow["gateway_registration_boundary_calls"] == 1,
             "the original flow gateway call must reach only its captured boundary");
        Need((bool?)flow["player_or_ui_readiness"] == false && (bool?)flow["unity_native_runtime"] == false,
             "fixture must not claim native runtime or readiness");
        Console.WriteLine("[S073UiShellFlowIntegrationFixture] result=Passed owner=OriginalOnCreate flow=OriginalOnCreate+OneNoInputOnUpdate state=Splash/Pending requests=0 audio=0 music=0 gateway=captured native=0 readiness=0");
        return 0;
    }
}
