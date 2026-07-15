using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal static class CommanderProfileRouteLifecyclePresentation
    {
        internal const string BackgroundScrimName = "CommanderBackgroundScrim";
        private static UIShellContentView s_backgroundScrimOwner;
        private static RectTransform s_backgroundScrimContentRoot;
        private static GameObject s_backgroundScrim;

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

            if (s_backgroundScrimOwner != contentView ||
                s_backgroundScrimContentRoot != contentRoot ||
                (s_backgroundScrim != null && s_backgroundScrim.transform.parent != contentRoot))
            {
                s_backgroundScrimOwner = contentView;
                s_backgroundScrimContentRoot = contentRoot;
                s_backgroundScrim = null;
            }

            if (!visible)
            {
                if (s_backgroundScrim != null)
                {
                    UIShellContentView.DestroyRegionObject(s_backgroundScrim);
                    s_backgroundScrim = null;
                    contentView.MarkContentChanged();
                }
                return;
            }

            if (s_backgroundScrim == null)
            {
                s_backgroundScrim = new GameObject(
                    BackgroundScrimName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                s_backgroundScrim.transform.SetParent(contentRoot, false);
            }

            UIShellContentView.Stretch(s_backgroundScrim.GetComponent<RectTransform>());
            Image image = s_backgroundScrim.GetComponent<Image>();
            image.color = new Color(0.015f, 0.018f, 0.014f, 0.34f);
            image.raycastTarget = false;
            s_backgroundScrim.transform.SetAsLastSibling();
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
