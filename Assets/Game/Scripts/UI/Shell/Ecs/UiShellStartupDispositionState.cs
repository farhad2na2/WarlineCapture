using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public partial struct UiShellStateSystem
    {
        private static void AddStartupDisposition(ref SystemState state, Entity boundary)
        {
            state.EntityManager.AddComponentData(boundary, new UiShellStartupDispositionComponent
            {
                Value = UiShellStartupDisposition.Pending
            });
        }

        private static void EnsureStartupDispositionComponent(ref SystemState state, Entity boundary)
        {
            if (!state.EntityManager.HasComponent<UiShellStartupDispositionComponent>(boundary))
                AddStartupDisposition(ref state, boundary);
        }

        private static void EnsureShellStateComponent(ref SystemState state, Entity boundary)
        {
            if (state.EntityManager.HasComponent<UiShellStateComponent>(boundary))
                return;
            state.EntityManager.AddComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.None,
                ActiveRoute = UIRoute.Splash,
                Phase = UiShellTransitionPhase.Idle,
                TransitionSequenceId = 0,
                IsTransitionRunning = 0
            });
        }
    }
}
