using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeShellCompositionSystemHelper
    {
        private UiShellStartupDisposition startupDisposition = UiShellStartupDisposition.Pending;
        private bool handoffPending;
        private bool handoffPublished;

        internal UiShellStartupDisposition StartupDisposition => startupDisposition;
        internal bool IsHandoffPending => handoffPending;
        internal bool IsHandoffPublished => handoffPublished;

        public void SetStartupDisposition(FirstLaunchNarrativeStartupDisposition disposition)
        {
            startupDisposition = disposition == FirstLaunchNarrativeStartupDisposition.EnterMenu
                ? UiShellStartupDisposition.EnterMenu
                : UiShellStartupDisposition.FirstLaunch;
        }

        public void RequestHandoff()
        {
            handoffPending = true;
            handoffPublished = false;
        }

        public bool TryPublishHandoff()
        {
            if (!handoffPending || handoffPublished)
                return false;

            handoffPending = false;
            handoffPublished = true;
            startupDisposition = UiShellStartupDisposition.EnterMenu;
            return true;
        }

        public void Apply(EntityManager entityManager, Entity boundary)
        {
            if (entityManager.HasComponent<UiShellStartupDispositionComponent>(boundary))
            {
                UiShellStartupDispositionComponent current =
                    entityManager.GetComponentData<UiShellStartupDispositionComponent>(boundary);
                if (current.Value != startupDisposition)
                {
                    current.Value = startupDisposition;
                    entityManager.SetComponentData(boundary, current);
                }
            }
        }

        public void Reset()
        {
            startupDisposition = UiShellStartupDisposition.Pending;
            handoffPending = false;
            handoffPublished = false;
        }

        public static void ResetBoundary(EntityManager entityManager, Entity boundary)
        {
            UiShellStartupDispositionComponent state = new() { Value = UiShellStartupDisposition.Pending };
            if (entityManager.HasComponent<UiShellStartupDispositionComponent>(boundary))
                entityManager.SetComponentData(boundary, state);
            else
                entityManager.AddComponentData(boundary, state);
        }
    }
}
