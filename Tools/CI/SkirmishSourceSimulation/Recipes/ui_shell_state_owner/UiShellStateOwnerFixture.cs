using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Unity.Entities;

// Real EntityManager execution of the extracted production owner methods in a
// standalone source host. This does not create a gameplay world or UI events.
internal static class UiShellStateOwnerFixture
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("UiShellState owner fixture: " + message);
    }

    private static int Count<T>(EntityManager em) where T : struct, IComponentData
    {
        using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        return query.CalculateEntityCount();
    }

    public static void Main()
    {
        // This EntityManager is the pinned S073 source host's ECS store. The
        // production owner below performs every root/state creation operation.
        int nextEntityId = 1;
        var em = new EntityManager
        {
            LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = nextEntityId++, Version = 1 }
        };
        var state = new SystemState { EntityManager = em, Enabled = true };
        var system = new UiShellStateSystem();
        system.OnCreate(ref state);
        Need(Count<UiShellRootComponent>(em) == 1, "original owner did not create exactly one root");
        Need(!state.Enabled, "original owner did not disable its one-shot startup system");
        Need(Count<UiShellStateComponent>(em) == 1, "original owner did not initialize exactly one shell state");
        Need(Count<UiShellStartupDispositionComponent>(em) == 1, "original startup disposition missing");
        Need(Count<UiShellLoadingProgressComponent>(em) == 1, "original loading progress missing");
        using var roots = em.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
        Entity root = roots.GetSingletonEntity();
        UiShellStateComponent shell = em.GetComponentData<UiShellStateComponent>(root);
        Need(shell.CurrentMode == UiShellMode.None && shell.ActiveRoute == UIRoute.Splash &&
             shell.Phase == UiShellTransitionPhase.Idle && shell.TransitionSequenceId == 0 &&
             shell.IsTransitionRunning == 0, "production shell-state defaults changed");
        UiShellStartupDispositionComponent startup = em.GetComponentData<UiShellStartupDispositionComponent>(root);
        Need(startup.Value == UiShellStartupDisposition.Pending, "production startup default changed");
        UiShellLoadingProgressComponent loading = em.GetComponentData<UiShellLoadingProgressComponent>(root);
        Need(loading.Progress01 == 0f && loading.Status.ToString() == "Starting" && loading.IsComplete == 0,
             "production loading defaults changed");
        Need(Count<AriaPlaySessionComponent>(em) == 0 && Count<AriaPlayObservationComponent>(em) == 0,
             "shell initialization fabricated ARIA session or observation state");
        Need(em.HasBuffer<UiShellRouteRequestComponent>(root) && em.GetBuffer<UiShellRouteRequestComponent>(root).Length == 0 &&
             em.HasBuffer<UiShellPresentationCommandComponent>(root) && em.GetBuffer<UiShellPresentationCommandComponent>(root).Length == 0,
             "shell initialization fabricated a route request or presentation event");
        Console.WriteLine("[UiShellStateOwnerFixture] result=Passed source_owner=OriginalOnCreate+EnsureBoundary entity_manager=S073Host gateway_registration=Excluded aria_state=Absent ui_events=Absent gameplay=0 player=0 readiness=0");
    }
}
