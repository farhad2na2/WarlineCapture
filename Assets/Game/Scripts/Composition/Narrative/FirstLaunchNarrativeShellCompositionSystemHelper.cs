using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeShellCompositionSystemHelper
    {
        private static readonly FixedString64Bytes BriefingStatus = "Preparing opening briefing";

        private UiShellStartupDisposition startupDisposition = UiShellStartupDisposition.Pending;
        private bool handoffPending;
        private bool handoffPublished;

        internal UiShellStartupDisposition StartupDisposition => startupDisposition;
        internal bool IsHandoffPending => handoffPending;
        internal bool IsHandoffPublished => handoffPublished;

        public void SetStartupDisposition(FirstLaunchNarrativeStartupDisposition disposition)
        {
            startupDisposition = disposition == FirstLaunchNarrativeStartupDisposition.EnterMenu
                ? UiShellStartupDisposition.EnterMenu : UiShellStartupDisposition.FirstLaunch;
        }

        public void RequestHandoff()
        {
            handoffPending = true;
            handoffPublished = false;
            startupDisposition = UiShellStartupDisposition.EnterMission;
        }

        public void Apply(EntityManager manager, Entity shell)
        {
            if (manager.HasComponent<UiShellStartupDispositionComponent>(shell))
            {
                var current = manager.GetComponentData<UiShellStartupDispositionComponent>(shell);
                if (current.Value != startupDisposition)
                {
                    current.Value = startupDisposition;
                    manager.SetComponentData(shell, current);
                }
            }

            if ((startupDisposition == UiShellStartupDisposition.FirstLaunch ||
                 startupDisposition == UiShellStartupDisposition.EnterMission) &&
                manager.HasComponent<UiShellLoadingProgressComponent>(shell))
            {
                var loading = manager.GetComponentData<UiShellLoadingProgressComponent>(shell);
                if (loading.Status != BriefingStatus || loading.IsComplete != 0)
                {
                    loading.Progress01 = 0f;
                    loading.Status = BriefingStatus;
                    loading.IsComplete = 0;
                    manager.SetComponentData(shell, loading);
                }
            }

            if (!handoffPending || handoffPublished ||
                !manager.HasBuffer<UiShellRouteRequestComponent>(shell))
                return;

            manager.GetBuffer<UiShellRouteRequestComponent>(shell).Add(
                new() { Intent = UiShellRouteIntent.EnterMatch, Route = UIRoute.Match });
            handoffPublished = true;
        }

        public void Reset()
        {
            startupDisposition = UiShellStartupDisposition.Pending;
            handoffPending = false;
            handoffPublished = false;
        }

        public static void ResetBoundary(EntityManager manager, Entity shell)
        {
            UiShellStartupDispositionComponent state = new() { Value = UiShellStartupDisposition.Pending };
            if (manager.HasComponent<UiShellStartupDispositionComponent>(shell))
                manager.SetComponentData(shell, state);
            else
                manager.AddComponentData(shell, state);
        }
    }
}
