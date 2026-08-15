using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal static class MenuOverlayRoutePresentation
    {
        internal static void Install(UIShellContentView contentView, UIRoute route)
        {
            if (route == UIRoute.QuickCustomSetup)
            {
                InstallSkirmishSetupBody(contentView);
                return;
            }

            if (route == UIRoute.Campaign)
            {
                InstallCampaignBody(contentView);
                return;
            }

            if (route == UIRoute.MissionBriefing)
            {
                InstallMissionBriefingBody(contentView);
                return;
            }

            if (route == UIRoute.Operations)
            {
                InstallOperationsBody(contentView);
                return;
            }

            CommanderProfileRouteLifecyclePresentation.InstallMenuRouteBody(contentView, route);
        }

        internal static void InstallSkirmishSetupBody(this UIShellContentView contentView)
        {
            GameObject setup = InstallBody(contentView, contentView.SkirmishSetupContentPrefab);
            contentView.BindQuickCustomScreens(setup);
        }

        internal static void InstallCampaignBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.CampaignContentPrefab);
        }

        internal static void InstallMissionBriefingBody(this UIShellContentView contentView)
        {
            GameObject body = InstallBody(contentView, contentView.MissionBriefingContentPrefab);
            MissionBriefingScreenView view = body != null
                ? body.GetComponentInChildren<MissionBriefingScreenView>(true)
                : null;
            view?.BindGameTextResolver(contentView.GameTextResolver);
        }

        internal static void InstallOperationsBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.OperationsContentPrefab);
        }

        private static GameObject InstallBody(UIShellContentView contentView, GameObject prefab)
        {
            contentView.UnbindMatchHudThreatWarningHeader();
            CommanderProfileRouteLifecyclePresentation.ExitCommanderRoute(contentView);
            contentView.ClearRegion(UIShellRegionId.LeftRegion);
            contentView.ClearRegion(UIShellRegionId.MiddleRegion);
            contentView.ClearRegion(UIShellRegionId.RightRegion);
            contentView.ClearRegion(UIShellRegionId.FooterRegion);
            return contentView.InstallRoot(prefab, UIShellRegionId.PopupLayer);
        }
    }
}
