using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Unity.Entities;

internal static class FixtureProgram
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static int Main()
    {
        var world = new World();
        EntityManager manager = world.EntityManager;
        Entity boundary = manager.CreateEntity();
        manager.AddComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MainMenu,
            ActiveRoute = UIRoute.MainMenu,
            Phase = UiShellTransitionPhase.EnteringMenu,
            TransitionSequenceId = 7,
            IsTransitionRunning = 1
        });
        manager.AddComponentData(boundary, default(UiShellLoadingProgressComponent));
        manager.AddComponentData(boundary, default(MatchIntroTransitionComponent));
        manager.AddComponentData(boundary, default(UiShellActivePopupComponent));
        manager.GetBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        manager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        manager.GetBuffer<UiShellRouteHistoryComponent>(boundary);
        manager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        manager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
        DynamicBuffer<UiShellTransitionCompleteComponent> completions = manager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        SystemState state = new() { EntityManager = manager, World = world };
        var system = new UiShellFlowSystem();

        system.OnCreate(ref state);
        Require(UiShellEcsGateway.RegistrationBoundaryCalls == 1, "gateway boundary was not observed exactly once");

        system.OnUpdate(ref state);
        RequireStillWaiting(manager, boundary, "without completion");
        Require(completions.Length == 0, "host synthesized a completion");

        completions.Add(new UiShellTransitionCompleteComponent { Kind = UiShellCommandKind.EnterMenu, SequenceId = 8 });
        system.OnUpdate(ref state);
        RequireStillWaiting(manager, boundary, "after wrong-sequence fixture completion");
        Require(completions.Length == 0, "owner did not consume/clear the provided completion buffer");

        completions.Add(new UiShellTransitionCompleteComponent { Kind = UiShellCommandKind.EnterMenu, SequenceId = 7 });
        system.OnUpdate(ref state);
        UiShellStateComponent final = manager.GetComponentData<UiShellStateComponent>(boundary);
        Require(final.IsTransitionRunning == 0, "matching explicit completion did not finish transition");
        Require(final.Phase == UiShellTransitionPhase.MenuReady, "matching explicit completion mapped to unexpected phase");
        Require(completions.Length == 0, "owner did not clear explicit completion fixture input");
        Require(UiAudioEventBridgeSystem.CapturedRequests == 0, "unexpected audio side-effect boundary call");
        Require(Game.Runtime.AudioEventRequestSystem.CapturedMusicRequests == 0, "unexpected music enqueue boundary call");
        Console.WriteLine("SOURCE_HOST_FIXTURE=Passed");
        Console.WriteLine("completion_cases=no completion; wrong sequence; explicit matching sequence");
        Console.WriteLine("side_effects=gateway observed; audio/music boundary calls=0; no queue/playback claim");
        Console.WriteLine("scope=source-host fixture only; no mission, native Unity, player, or readiness claim");
        return 0;
    }

    private static void RequireStillWaiting(EntityManager manager, Entity boundary, string caseName)
    {
        UiShellStateComponent state = manager.GetComponentData<UiShellStateComponent>(boundary);
        Require(state.IsTransitionRunning == 1, $"transition advanced {caseName}");
        Require(state.Phase == UiShellTransitionPhase.EnteringMenu, $"phase changed {caseName}");
    }
}
