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

            Transform existing = contentRoot.Find(BackgroundScrimName);
            if (!visible)
            {
                if (existing != null)
                {
                    UIShellContentView.DestroyRegionObject(existing.gameObject);
                    contentView.MarkContentChanged();
                }
                return;
            }

            GameObject scrim = existing != null
                ? existing.gameObject
                : new GameObject(BackgroundScrimName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null)
                scrim.transform.SetParent(contentRoot, false);

            UIShellContentView.Stretch(scrim.GetComponent<RectTransform>());
            Image image = scrim.GetComponent<Image>();
            image.color = new Color(0.015f, 0.018f, 0.014f, 0.34f);
            image.raycastTarget = false;
            scrim.transform.SetAsLastSibling();
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
