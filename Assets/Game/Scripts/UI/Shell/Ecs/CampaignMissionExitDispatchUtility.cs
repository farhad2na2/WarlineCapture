using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public static class CampaignMissionExitDispatchUtility
    {
        public static bool TryHandle(
            EntityManager entityManager,
            Entity uiBoundary,
            int payloadId)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
                ComponentType.ReadWrite<CampaignMissionActionRequestElement>(),
                ComponentType.ReadWrite<CampaignMissionActionResultElement>());
            if (query.CalculateEntityCount() != 1)
                return false;

            Entity root = query.GetSingletonEntity();
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            DynamicBuffer<CampaignMissionActionResultElement> results =
                entityManager.GetBuffer<CampaignMissionActionResultElement>(root);
            if (HasAcceptedExit(results))
            {
                CompleteRoute(entityManager, uiBoundary, payloadId);
                return true;
            }
            if (runtime.Phase < MissionPhaseKind.Preparing || runtime.Phase > MissionPhaseKind.SecureCorridor)
                return false;

            DynamicBuffer<CampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionActionRequestElement>(root);
            if (requests.Length != 0)
                return true;
            requests.Add(new CampaignMissionActionRequestElement
            {
                Action = MissionActionKind.Exit,
                TransitionToken = runtime.TransitionToken,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal
            });

            bool consumed = false;
            CampaignMissionRuntimeSystem.TryConsumeActionManaged(entityManager, root, ref consumed);
            if (!consumed)
                return true;
            results = entityManager.GetBuffer<CampaignMissionActionResultElement>(root);
            if (!HasAcceptedExit(results))
                return true;

            CompleteRoute(entityManager, uiBoundary, payloadId);
            return true;
        }

        private static void CompleteRoute(EntityManager entityManager, Entity uiBoundary, int payloadId)
        {
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
                entityManager.GetBuffer<UiShellPopupRequestComponent>(uiBoundary);
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(uiBoundary);
            if (!HasPauseHide(popupRequests, payloadId))
            {
                popupRequests.Add(new UiShellPopupRequestComponent
                {
                    PopupKind = UiShellPopupKind.Pause,
                    Intent = UiShellPopupIntent.Hide,
                    PayloadId = payloadId
                });
            }
            if (!HasMainMenuRoute(routeRequests))
            {
                routeRequests.Add(new UiShellRouteRequestComponent
                {
                    Route = UIRoute.MainMenu,
                    Intent = UiShellRouteIntent.ReturnToMainMenu,
                    PushHistory = 0
                });
            }
        }

        private static bool HasPauseHide(
            DynamicBuffer<UiShellPopupRequestComponent> requests,
            int payloadId)
        {
            for (int index = 0; index < requests.Length; index++)
            {
                if (requests[index].PopupKind == UiShellPopupKind.Pause &&
                    requests[index].Intent == UiShellPopupIntent.Hide &&
                    requests[index].PayloadId == payloadId)
                    return true;
            }
            return false;
        }

        private static bool HasAcceptedExit(DynamicBuffer<CampaignMissionActionResultElement> results)
        {
            for (int index = 0; index < results.Length; index++)
            {
                if (results[index].Action == MissionActionKind.Exit && results[index].Accepted != 0)
                    return true;
            }
            return false;
        }

        private static bool HasMainMenuRoute(DynamicBuffer<UiShellRouteRequestComponent> routes)
        {
            for (int index = 0; index < routes.Length; index++)
            {
                if (routes[index].Route == UIRoute.MainMenu &&
                    routes[index].Intent == UiShellRouteIntent.ReturnToMainMenu)
                    return true;
            }
            return false;
        }
    }
}
