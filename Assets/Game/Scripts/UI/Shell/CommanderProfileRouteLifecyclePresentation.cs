using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal static class CommanderProfileRouteLifecyclePresentation
    {
        internal const string BackgroundScrimName = "CommanderBackgroundScrim";

        internal static void InstallMainMenu(UIShellContentView contentView)
        {
            contentView.UnbindMatchHudThreatWarningHeader();
            contentView.InstallSection(contentView.MainMenuContentPrefab, UIShellContentSectionId.MenuBackground, UIShellRegionId.MenuBackgroundRegion);
            contentView.InstallSection(contentView.MainMenuContentPrefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
            InstallMainMenuBody(contentView);
            contentView.ClearRegion(UIShellRegionId.PopupLayer);
        }

        internal static void InstallMenuRouteBody(UIShellContentView contentView, UIRoute route)
        {
            if (route == UIRoute.Armory)
            {
                contentView.InstallArmoryBody();
                return;
            }

            if (route == UIRoute.CommandFeed)
            {
                InstallCommanderProfileBody(contentView);
                return;
            }

            if (route == UIRoute.QuickCustomSetup)
            {
                contentView.InstallSkirmishSetupBody();
                return;
            }

            if (route == UIRoute.Campaign)
            {
                contentView.InstallCampaignBody();
                return;
            }

            if (route == UIRoute.MissionBriefing)
            {
                contentView.InstallMissionBriefingBody();
                return;
            }

            if (route == UIRoute.LoadoutSquadPrep)
            {
                contentView.InstallLoadoutSquadPrepBody();
                return;
            }

            InstallMainMenuBody(contentView);
        }

        internal static void ExitCommanderRoute(UIShellContentView contentView)
        {
            SetCommanderBackgroundScrim(contentView, false);
        }

        private static void InstallMainMenuBody(UIShellContentView contentView)
        {
            ExitCommanderRoute(contentView);
            GameObject prefab = contentView.MainMenuContentPrefab;
            GameObject left = contentView.InstallSection(prefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
            GameObject middle = contentView.InstallSection(prefab, UIShellContentSectionId.Middle, UIShellRegionId.MiddleRegion);
            GameObject right = contentView.InstallSection(prefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
            GameObject footer = contentView.InstallSection(prefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
            contentView.BindQuickCustomScreens(left, middle, right, footer);
            contentView.BindGameStartButtons(left, middle, right, footer);
            contentView.ClearRegion(UIShellRegionId.PopupLayer);
        }

        private static void InstallCommanderProfileBody(UIShellContentView contentView)
        {
            contentView.UnbindMatchHudThreatWarningHeader();
            SetCommanderBackgroundScrim(contentView, true);
            GameObject prefab = contentView.CommanderProfileContentPrefab;
            contentView.InstallSection(prefab, UIShellContentSectionId.Header, UIShellRegionId.HeaderRegion);
            contentView.InstallSection(prefab, UIShellContentSectionId.Left, UIShellRegionId.LeftRegion);
            GameObject middle = contentView.InstallSection(prefab, UIShellContentSectionId.Middle, UIShellRegionId.MiddleRegion);
            contentView.InstallSection(prefab, UIShellContentSectionId.Right, UIShellRegionId.RightRegion);
            contentView.InstallSection(prefab, UIShellContentSectionId.Footer, UIShellRegionId.FooterRegion);
            BindCommanderProfile(middle);
            contentView.ClearRegion(UIShellRegionId.PopupLayer);
        }

        private static void SetCommanderBackgroundScrim(UIShellContentView contentView, bool visible)
        {
            if (!contentView.TryGetRegionContentRoot(UIShellRegionId.MenuBackgroundRegion, out RectTransform contentRoot))
                return;

            GameObject backgroundScrim = contentView.CommanderBackgroundScrim;

            if (!visible)
            {
                if (backgroundScrim != null)
                {
                    UIShellContentView.DestroyRegionObject(backgroundScrim);
                    contentView.CommanderBackgroundScrim = null;
                    contentView.MarkContentChanged();
                }
                return;
            }

            if (backgroundScrim == null)
            {
                backgroundScrim = new GameObject(
                    BackgroundScrimName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                backgroundScrim.transform.SetParent(contentRoot, false);
                contentView.CommanderBackgroundScrim = backgroundScrim;
            }

            UIShellContentView.Stretch(backgroundScrim.GetComponent<RectTransform>());
            Image image = backgroundScrim.GetComponent<Image>();
            image.color = new Color(0.015f, 0.018f, 0.014f, 0.72f);
            image.raycastTarget = false;
            backgroundScrim.transform.SetAsLastSibling();
            contentView.MarkContentChanged();
        }

        private static void BindCommanderProfile(GameObject middle)
        {
            if (middle == null || !UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile))
                return;

            middle.GetComponent<CommanderProfileContentView>()?.Bind(profile);
        }
    }
}
