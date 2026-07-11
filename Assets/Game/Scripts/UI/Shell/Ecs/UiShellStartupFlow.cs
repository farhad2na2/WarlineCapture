using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public partial struct UiShellFlowSystem
    {
        private bool TryHandleStartup(
            ref SystemState state,
            Entity boundary,
            ref UiShellStateComponent shellState,
            ref MatchIntroTransitionComponent matchIntro,
            ref UiShellLoadingProgressComponent loading,
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests,
            DynamicBuffer<UiShellPresentationCommandComponent> commands,
            DynamicBuffer<UiShellRouteHistoryComponent> routeHistory)
        {
            if (routeRequests.Length > 0 &&
                routeRequests[0].Intent == UiShellRouteIntent.EnterMatch &&
                TryConsumeRouteRequest(routeRequests, out UiShellRouteRequestComponent request))
            {
                ProcessRouteRequest(ref shellState, ref matchIntro, commands, routeHistory, request);
                EmitRouteMusic(state.World, request);
                EmitRouteAudio(state.World, request);
                ResetLoading(ref loading, "Loading operation interface");
                state.EntityManager.SetComponentData(boundary, loading);
                state.EntityManager.SetComponentData(boundary, shellState);
                state.EntityManager.SetComponentData(boundary, matchIntro);
                return true;
            }
            UiShellStartupDisposition disposition = state.EntityManager.HasComponent<UiShellStartupDispositionComponent>(boundary)
                ? state.EntityManager.GetComponentData<UiShellStartupDispositionComponent>(boundary).Value
                : UiShellStartupDisposition.EnterMenu;
            if (disposition == UiShellStartupDisposition.EnterMenu)
                return false;
            state.EntityManager.SetComponentData(boundary, shellState);
            state.EntityManager.SetComponentData(boundary, matchIntro);
            return true;
        }

        private static void ResetLoading(ref UiShellLoadingProgressComponent loading, string status)
        {
            loading.Progress01 = 0f;
            loading.Status = new Unity.Collections.FixedString64Bytes(status);
            loading.IsComplete = 0;
        }
    }
}
