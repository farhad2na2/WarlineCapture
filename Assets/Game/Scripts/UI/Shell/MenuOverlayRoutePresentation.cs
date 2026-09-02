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

            if (route == UIRoute.LoadoutSquadPrep)
            {
                InstallLoadoutSquadPrepBody(contentView);
                return;
            }

            if (route == UIRoute.Operations)
            {
                InstallOperationsBody(contentView);
                return;
            }

            if (route == UIRoute.DistrictDetail)
            {
                InstallDistrictDetailBody(contentView);
                return;
            }

            if (route == UIRoute.CommandExchange)
            {
                InstallStoreBody(contentView);
                return;
            }

            if (route == UIRoute.Inbox)
            {
                InstallInboxBody(contentView);
                return;
            }

            if (route == UIRoute.Events)
            {
                InstallEventsBody(contentView);
                return;
            }

            if (route == UIRoute.Ranking)
            {
                InstallRankingBody(contentView);
                return;
            }

            if (route == UIRoute.CommandFeed)
            {
                InstallCommandFeedBody(contentView);
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
            GameObject body = InstallBody(contentView, contentView.CampaignContentPrefab);
            CampaignOperationsScreenView view = body != null
                ? body.GetComponentInChildren<CampaignOperationsScreenView>(true)
                : null;
            view?.BindGameTextResolver(contentView.GameTextResolver);
            body?.GetComponentInChildren<CampaignMissionScreenBinder>(true)?.Refresh();
        }

        internal static void InstallMissionBriefingBody(this UIShellContentView contentView)
        {
            GameObject body = InstallBody(contentView, contentView.MissionBriefingContentPrefab);
            MissionBriefingScreenView view = body != null
                ? body.GetComponentInChildren<MissionBriefingScreenView>(true)
                : null;
            view?.BindGameTextResolver(contentView.GameTextResolver);
            body?.GetComponentInChildren<CampaignMissionScreenBinder>(true)?.Refresh();
        }

        internal static void InstallLoadoutSquadPrepBody(this UIShellContentView contentView)
        {
            GameObject body = InstallBody(contentView, contentView.LoadoutSquadPrepContentPrefab);
            body?.GetComponent<LoadoutSquadPrepScreenView>()?.RefreshBindings();
        }

        internal static void InstallOperationsBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.OperationsContentPrefab);
        }

        internal static void InstallDistrictDetailBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.DistrictDetailContentPrefab);
        }

        internal static void InstallStoreBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.StoreContentPrefab);
        }

        internal static void InstallInboxBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.InboxContentPrefab);
        }

        internal static void InstallEventsBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.EventsContentPrefab);
        }

        internal static void InstallRankingBody(this UIShellContentView contentView)
        {
            InstallBody(contentView, contentView.RankingContentPrefab);
        }

        internal static void InstallCommandFeedBody(this UIShellContentView contentView)
        {
            GameObject body = InstallBody(contentView, contentView.CommandFeedContentPrefab);
            body?.GetComponent<CommandFeedScreenView>()?.Refresh();
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
