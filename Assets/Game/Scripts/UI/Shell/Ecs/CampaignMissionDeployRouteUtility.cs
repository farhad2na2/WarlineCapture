using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public static class CampaignMissionDeployRouteUtility
    {
        public static bool TryPublish(EntityManager entityManager, Entity uiRoot)
        {
            if (!entityManager.Exists(uiRoot) ||
                !entityManager.HasBuffer<UiShellRouteRequestComponent>(uiRoot))
                return false;

            DynamicBuffer<UiShellRouteRequestComponent> routes =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(uiRoot);
            for (int index = 0; index < routes.Length; index++)
                if (routes[index].Intent == UiShellRouteIntent.EnterMatch &&
                    routes[index].Route == UIRoute.Match)
                    return true;

            routes.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            return true;
        }
    }
}
