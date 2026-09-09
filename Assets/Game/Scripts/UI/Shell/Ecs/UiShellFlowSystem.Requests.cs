using Unity.Entities;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    public partial struct UiShellFlowSystem
    {
        private static bool TryConsumeRouteRequest(
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests,
            out UiShellRouteRequestComponent request)
        {
            if (routeRequests.Length == 0)
            {
                request = default;
                return false;
            }

            request = routeRequests[0];
            routeRequests.Clear();
            return true;
        }

        private static bool TryConsumeLoadingProgressRequest(
            DynamicBuffer<UiShellLoadingProgressRequestComponent> loadingRequests,
            out UiShellLoadingProgressRequestComponent request)
        {
            if (loadingRequests.Length == 0)
            {
                request = default;
                return false;
            }

            request = loadingRequests[loadingRequests.Length - 1];
            loadingRequests.Clear();
            return true;
        }

        private static bool TryConsumePopupRequest(
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests,
            out UiShellPopupRequestComponent request)
        {
            if (popupRequests.Length == 0)
            {
                request = default;
                return false;
            }

            request = popupRequests[0];
            // A close followed by another popup can arrive during the same animation.
            // Preserve the later intent until this transition completes.
            popupRequests.RemoveAt(0);
            return true;
        }

    }
}
