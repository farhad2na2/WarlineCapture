#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WarlineCaptureGameUiContentPrefabBuilder
{
    private const string ContentFolder = "Assets/Game/Prefabs/UI/Shell/Content";
    private const string PopupFolder = "Assets/Game/Prefabs/UI/Shell/Popups";
    private const string LoadingPrefabPath = ContentFolder + "/SCN01_LoadingContent.prefab";
    private const string MainMenuPrefabPath = ContentFolder + "/SCN02_MainMenuContent.prefab";
    private const string CommanderProfilePrefabPath = ContentFolder + "/SCN03_CommanderProfileContent.prefab";
    private const string MatchHudPrefabPath = ContentFolder + "/SCN08_MatchHudContent.prefab";
    private const string ResultPopupPrefabPath = PopupFolder + "/POP05_MissionResultPopup.prefab";
    private const string MainMenuVisualLockRoot = "Design/VisualLockLayered/SCN-02_MainMenu";
    private const string MainMenuVisualLockReferencePath = MainMenuVisualLockRoot + "/reference/SCN-02_MainMenu_Landscape_Target.png";
    private const string MainMenuVisualLockLayersRoot = MainMenuVisualLockRoot + "/layers";
    private const string MainMenuVisualLockManifestPath = MainMenuVisualLockRoot + "/layer_manifest.json";
    private const string RejectedMainMenuV15BRequestPath = MainMenuVisualLockRoot + "/layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15B.md";
    private const string MainMenuLayerRoot = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo";

    private static readonly Vector2 MainMenuHeaderSize = new(4800f, 280f);
    private static readonly Vector2 MainMenuBackgroundSize = new(4800f, 2160f);
    private static readonly Vector2 MainMenuBackgroundCoverSize = new(5038f, 2160f);
    private static readonly Vector2 MainMenuSideSize = new(720f, 1640f);
    private static readonly Vector2 MainMenuMiddleSize = new(3360f, 1640f);

    private static readonly Color Clear = new(0f, 0f, 0f, 0f);
    private static readonly Color Panel = new(0.025f, 0.031f, 0.027f, 0.92f);
    private static readonly Color PanelMuted = new(0.055f, 0.062f, 0.046f, 0.88f);
    private static readonly Color Stroke = new(0.73f, 0.59f, 0.25f, 0.9f);
    private static readonly Color Text = new(0.86f, 0.84f, 0.74f, 1f);
    private static readonly Color MutedText = new(0.62f, 0.61f, 0.54f, 1f);
    private static readonly Color Accent = new(0.96f, 0.66f, 0.16f, 1f);
    private static readonly Color Blue = new(0.38f, 0.75f, 0.85f, 1f);

    private static readonly string[] RequiredMainMenuLayerSprites =
    {
        "scn02_background_art.png",
        "scn02_brand_logo_lockup.png",
        "scn02_campaign_thumbnail_art.png",
        "scn02_commander_panel_frame.png",
        "scn02_commander_portrait_art.png",
        "scn02_commander_portrait_frame.png",
        "scn02_comms_status_panel_frame.png",
        "scn02_deploy_chevrons.png",
        "scn02_deploy_cta_frame.png",
        "scn02_header_command_panel_bg.png",
        "scn02_header_logo_panel_bg.png",
        "scn02_header_resource_panel_bg.png",
        "scn02_header_right_actions_bg.png",
        "scn02_icon_campaign_crosshair.png",
        "scn02_icon_commander_bust.png",
        "scn02_icon_inbox_envelope.png",
        "scn02_icon_lightning_small.png",
        "scn02_icon_lock.png",
        "scn02_icon_operations_pin.png",
        "scn02_icon_settings_gear.png",
        "scn02_icon_skirmish_blades.png",
        "scn02_icon_store_cart.png",
        "scn02_locked_row_frame.png",
        "scn02_mode_card_frame.png",
        "scn02_mode_card_thumbnail_mask_frame.png",
        "scn02_mode_progress_meter_frame.png",
        "scn02_nav_button_inactive_frame.png",
        "scn02_nav_button_selected_frame.png",
        "scn02_operations_thumbnail_art.png",
        "scn02_readiness_segments.png",
        "scn02_resource_coin_badge.png",
        "scn02_resource_command_shield.png",
        "scn02_resource_supplies_crate.png",
        "scn02_skirmish_thumbnail_art.png"
    };

    [MenuItem("WarlineCapture/UI/Build GameUI Content Prefabs Step 6")]
    public static void BuildStep6()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildMainMenuContent(), MainMenuPrefabPath);
        SavePrefab(BuildCommanderProfileContent(), CommanderProfilePrefabPath);
        SavePrefab(BuildMatchHudContent(), MatchHudPrefabPath);
        SavePrefab(BuildResultPopup(), ResultPopupPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateStep6();
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_BUILT generated=4 protected=SCN01_LoadingContent.prefab");
    }

    [MenuItem("WarlineCapture/UI/Build Main Menu Content Prefab Only")]
    public static void BuildMainMenuOnly()
    {
        EnsureFolders();
        SavePrefab(BuildMainMenuContent(), MainMenuPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateMainMenuContentPrefab();
        Debug.Log("WARLINECAPTURE_GAMEUI_MAIN_MENU_CONTENT_BUILT prefab=SCN02_MainMenuContent.prefab");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Content Prefabs Step 6")]
    public static void ValidateStep6()
    {
        ValidateProtectedLoadingContentPrefab();
        ValidateMainMenuContentPrefab();
        ValidatePrefab(CommanderProfilePrefabPath, "SCN03_CommanderProfileContent", "LeftContent", "MiddleContent", "RightContent");
        ValidatePrefab(MatchHudPrefabPath, "SCN08_MatchHudContent", "HeaderContent", "LeftContent", "RightContent", "FooterContent");
        ValidatePrefab(ResultPopupPrefabPath, "POP05_MissionResultPopup", "PopupFrame", "PopupFrame/Actions");
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_VALIDATED prefabs=5 protected=SCN01_LoadingContent.prefab");
    }

    private static GameObject BuildMainMenuContent()
    {
        ValidateMainMenuSourceContract();

        GameObject root = CreateRoot("SCN02_MainMenuContent");

        GameObject background = CreateGroup("MenuBackgroundContent", root.transform, new Rect(0f, 0f, MainMenuBackgroundSize.x, MainMenuBackgroundSize.y));
        BuildMainMenuBackground(background.transform);

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, MainMenuHeaderSize.x, MainMenuHeaderSize.y));
        BuildDesignedMainMenuHeader(header.transform);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildMainMenuLeftNav(left.transform);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(MainMenuSideSize.x, MainMenuHeaderSize.y, MainMenuMiddleSize.x, MainMenuMiddleSize.y));
        BuildMainMenuCenter(middle.transform);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(4080f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildMainMenuCommanderPanel(right.transform);

        return root;
    }

    private static void BuildMainMenuBackground(Transform parent)
    {
        GameObject viewport = CreateCenteredRect("BackgroundViewport", parent, Vector2.zero, MainMenuBackgroundSize);
        viewport.AddComponent<RectMask2D>();
        AddMainMenuSpriteCentered(viewport.transform, "BackgroundArt", "scn02_background_art.png", Vector2.zero, MainMenuBackgroundCoverSize, false);
    }

    private static GameObject BuildCommanderProfileContent()
    {
        GameObject root = CreateRoot("SCN03_CommanderProfileContent");

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, 140f, 360f, 820f));
        AddRouteButton(left.transform, "BackButton", "<  BACK", new Rect(16f, 18f, 328f, 76f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);
        AddPanel(left.transform, "CommanderNavPanel", new Rect(16f, 118f, 328f, 630f));
        AddText(left.transform, "CommanderNavTitle", "COMMANDER", new Rect(44f, 148f, 272f, 38f), 28f, TextAlignmentOptions.Left, Text);
        AddProfileTab(left.transform, 212f, "Overview", true);
        AddProfileTab(left.transform, 300f, "Progression", false);
        AddProfileTab(left.transform, 388f, "Service Record", false);
        AddProfileTab(left.transform, 476f, "Loadout", false);
        AddProfileTab(left.transform, 564f, "Cosmetics", false);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(360f, 140f, 1680f, 820f));
        AddPanel(middle.transform, "ProfileMainPanel", new Rect(70f, 42f, 1540f, 700f));
        AddText(middle.transform, "ProfileTitle", "COMMANDER PROFILE", new Rect(118f, 82f, 700f, 54f), 40f, TextAlignmentOptions.Left, Text);
        AddText(middle.transform, "ProfileSubtitle", "Field Commander  |  Level 38  |  First Contact Ready", new Rect(118f, 142f, 900f, 36f), 23f, TextAlignmentOptions.Left, Accent);
        AddSolid(middle.transform, "PortraitLarge", new Rect(118f, 220f, 370f, 420f), new Color(0.018f, 0.022f, 0.018f, 1f));
        AddText(middle.transform, "PortraitSilhouette", "COMMANDER\nSILHOUETTE", new Rect(156f, 370f, 294f, 88f), 24f, TextAlignmentOptions.Center, MutedText);
        AddText(middle.transform, "ProfileBio", "Decorated operations leader assigned to rapid district stabilization.\n\nCurrent doctrine favors mobile armor, district control, and precision extraction under pressure.", new Rect(560f, 222f, 920f, 180f), 28f, TextAlignmentOptions.Left, Text);
        AddMetricCard(middle.transform, 560f, 456f, "MISSIONS", "42");
        AddMetricCard(middle.transform, 820f, 456f, "VICTORIES", "31");
        AddMetricCard(middle.transform, 1080f, 456f, "AUTHORITY", "2,715");
        AddMetricCard(middle.transform, 1340f, 456f, "RANK", "III");

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(2040f, 140f, 360f, 820f));
        AddPanel(right.transform, "ProfileStatsPanel", new Rect(18f, 20f, 324f, 720f));
        AddText(right.transform, "StatsTitle", "READINESS", new Rect(48f, 54f, 264f, 36f), 28f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "StatsBars", "TACTICS     ||||||||||--\nARMOR       |||||||||---\nLOGISTICS   ||||||||----\nINTEL       |||||||-----", new Rect(48f, 124f, 264f, 180f), 22f, TextAlignmentOptions.Left, MutedText);
        AddText(right.transform, "UnlockTitle", "ACTIVE PERKS", new Rect(48f, 342f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "UnlockList", "Rapid Deployment\nSupply Efficiency\nDistrict Resolve", new Rect(48f, 394f, 264f, 150f), 22f, TextAlignmentOptions.Left, Accent);
        AddText(right.transform, "ProfileHint", "Header remains active.\nBody regions are swapped by shell route.", new Rect(48f, 604f, 264f, 86f), 18f, TextAlignmentOptions.Center, MutedText);

        return root;
    }

    private static GameObject BuildMatchHudContent()
    {
        GameObject root = CreateRoot("SCN08_MatchHudContent");

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, 2400f, 118f));
        AddPanel(header.transform, "HudHeader", StretchRect());
        AddText(header.transform, "HudTitle", "PORT BREACH  |  LIVE OPERATION", new Rect(48f, 32f, 700f, 46f), 30f, TextAlignmentOptions.Left, Text);
        AddText(header.transform, "HudResources", "CRED 187,540     SUP 92,860     CMD 2,715", new Rect(1040f, 32f, 980f, 46f), 28f, TextAlignmentOptions.Right, Text);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, 140f, 360f, 820f));
        AddPanel(left.transform, "ObjectivesPanel", new Rect(20f, 20f, 320f, 370f));
        AddText(left.transform, "ObjectiveTitle", "OBJECTIVES", new Rect(48f, 50f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(left.transform, "ObjectiveList", "Secure depot\nHold main road\nExtract civilians", new Rect(48f, 108f, 264f, 160f), 22f, TextAlignmentOptions.Left, MutedText);
        AddPanel(left.transform, "SquadPanel", new Rect(20f, 430f, 320f, 260f));
        AddText(left.transform, "SquadText", "SQUADS\nRifle 01 Ready\nArmor 02 Moving", new Rect(48f, 462f, 264f, 160f), 22f, TextAlignmentOptions.Left, Text);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(2040f, 140f, 360f, 820f));
        AddPanel(right.transform, "CommandPanel", new Rect(20f, 20f, 320f, 520f));
        AddText(right.transform, "CommandTitle", "COMMANDS", new Rect(48f, 50f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "CommandList", "Move\nAttack\nHold\nExtract", new Rect(48f, 116f, 264f, 240f), 28f, TextAlignmentOptions.Left, MutedText);

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 960f, 2400f, 120f));
        AddPanel(footer.transform, "FooterRail", StretchRect());
        AddText(footer.transform, "FooterText", "TACTICAL LINK ONLINE     |     UNIT ORDERS QUEUED     |     CAMERA FOLLOW READY", new Rect(80f, 32f, 2240f, 48f), 28f, TextAlignmentOptions.Center, Text);

        return root;
    }

    private static GameObject BuildResultPopup()
    {
        GameObject root = CreateRoot("POP05_MissionResultPopup");
        GameObject frame = CreateRect("PopupFrame", root.transform, new Rect(0f, 0f, 920f, 560f));
        AddPanel(frame.transform, "Frame", StretchRect());
        AddText(frame.transform, "TitleText", "MISSION RESULT", new Rect(64f, 48f, 792f, 58f), 42f, TextAlignmentOptions.Center, Text);
        AddText(frame.transform, "OutcomeText", "VICTORY COMPLETE", new Rect(64f, 136f, 792f, 52f), 34f, TextAlignmentOptions.Center, Accent);
        AddText(frame.transform, "SummaryText", "Primary objectives secured.\nDistrict pressure reduced.\nCommander authority increased.", new Rect(96f, 230f, 728f, 150f), 25f, TextAlignmentOptions.Center, MutedText);
        GameObject actions = CreateRect("Actions", frame.transform, new Rect(160f, 430f, 600f, 72f));
        AddResultConfirmButton(actions.transform, "ContinueButton", "CONTINUE", StretchRect());
        return root;
    }

    private static void BuildDesignedMainMenuHeader(Transform parent)
    {
        AddStretchSolid(parent, "HeaderBackPlate", new Color(0.018f, 0.022f, 0.018f, 0.88f));

        GameObject logoPanel = CreateTopLeftMainMenuRect("HeaderLogoPanel", parent, new Rect(0f, 14f, 950f, 243f));
        AddMainMenuSpriteCentered(logoPanel.transform, "Frame", "scn02_header_logo_panel_bg.png", Vector2.zero, logoPanel.GetComponent<RectTransform>().sizeDelta, false);
        AddMainMenuSprite(logoPanel.transform, "Logo", "scn02_brand_logo_lockup.png", new Rect(108f, 56f, 690f, 132f), logoPanel.GetComponent<RectTransform>().sizeDelta, true);

        GameObject resourceArea = CreateTopLeftMainMenuRect("HeaderResourceArea", parent, new Rect(950f, 14f, 2260f, 243f));
        AddHeaderResource(resourceArea.transform, "Credits", "scn02_header_resource_panel_bg.png", "scn02_resource_coin_badge.png", "Credits", "187,540", 0f, 735f, 52f, 49f, 148f, 148f, 214f, new Color32(235, 179, 65, 255));
        AddHeaderResource(resourceArea.transform, "Supplies", "scn02_header_resource_panel_bg.png", "scn02_resource_supplies_crate.png", "Supplies", "92,860", 760f, 735f, 54f, 57f, 166f, 140f, 238f, new Color32(161, 166, 105, 255));
        AddHeaderResource(resourceArea.transform, "Command", "scn02_header_command_panel_bg.png", "scn02_resource_command_shield.png", "Command", "2,715", 1520f, 740f, 60f, 44f, 150f, 172f, 250f, new Color32(119, 180, 215, 255));

        GameObject actionsPanel = CreateTopRightMainMenuRect("HeaderActionsPanel", parent, 0f, 14f, new Vector2(608f, 243f));
        Vector2 actionsSize = actionsPanel.GetComponent<RectTransform>().sizeDelta;
        AddMainMenuSpriteCentered(actionsPanel.transform, "Frame", "scn02_header_right_actions_bg.png", Vector2.zero, actionsSize, false);
        AddHeaderActionButton(actionsPanel.transform, "InboxButton", "scn02_icon_inbox_envelope.png", new Rect(84f, 58f, 160f, 124f), actionsSize, WarlineCaptureRoute.Inbox);
        AddHeaderActionButton(actionsPanel.transform, "SettingsButton", "scn02_icon_settings_gear.png", new Rect(364f, 46f, 164f, 152f), actionsSize, WarlineCaptureRoute.Settings);
    }

    private static Rect HeaderRect(float x, float y, float width, float height)
    {
        const float HeaderSourceHeight = 250f;
        float scaleY = MainMenuHeaderSize.y / HeaderSourceHeight;
        return new Rect(x, y * scaleY, width, height * scaleY);
    }

    private static void AddHeaderResource(
        Transform parent,
        string name,
        string frame,
        string icon,
        string label,
        string value,
        float panelX,
        float panelWidth,
        float iconX,
        float iconY,
        float iconWidth,
        float iconHeight,
        float textX,
        Color valueColor)
    {
        Vector2 parentSize = (parent as RectTransform)?.sizeDelta ?? MainMenuHeaderSize;
        Vector2 panelSize = new(panelWidth, 243f);
        GameObject panel = CreateMainMenuRect($"{name}Panel", parent, new Rect(panelX, 0f, panelSize.x, panelSize.y), parentSize);
        AddMainMenuSpriteCentered(panel.transform, "Frame", frame, Vector2.zero, panelSize, false);
        AddMainMenuSprite(panel.transform, "Icon", icon, new Rect(iconX, iconY, iconWidth, iconHeight), panelSize, true);
        AddText(panel.transform, "Label", label, new Rect(textX, 52f, panelWidth - textX - 42f, 50f), 36f, TextAlignmentOptions.Left, Text);
        AddText(panel.transform, "Value", value, new Rect(textX, 106f, panelWidth - textX - 42f, 84f), 58f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderActionButton(
        Transform parent,
        string name,
        string icon,
        Rect rect,
        Vector2 parentSize,
        WarlineCaptureRoute route)
    {
        GameObject buttonObject = CreateMainMenuRect(name, parent, rect, parentSize);
        Image image = buttonObject.AddComponent<Image>();
        image.color = Clear;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.42f, 0.18f);
        colors.pressedColor = new Color(1f, 0.72f, 0.18f, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = buttonObject.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(UiShellRouteIntent.OpenMenuRoute, route, false);
        AddMainMenuSpriteCentered(buttonObject.transform, "Icon", icon, Vector2.zero, rect.size, true);
    }

    private static void BuildMainMenuLeftNav(Transform parent)
    {
        GameObject navPanel = CreateMainMenuRect("LeftNavPanel", parent, new Rect(41f, 145f, 574f, 1392f), MainMenuSideSize);
        Vector2 navPanelSize = navPanel.GetComponent<RectTransform>().sizeDelta;
        AddMainMenuNavRow(navPanel.transform, "Campaign", "Campaign", "scn02_icon_campaign_crosshair.png", 0f, true, WarlineCaptureRoute.MainMenu, navPanelSize);
        AddMainMenuNavRow(navPanel.transform, "Operations", "Operations", "scn02_icon_operations_pin.png", 238f, false, WarlineCaptureRoute.OperationDashboard, navPanelSize);
        AddMainMenuNavRow(navPanel.transform, "Skirmish", "Skirmish", "scn02_icon_skirmish_blades.png", 477f, false, WarlineCaptureRoute.QuickCustomSetup, navPanelSize);
        AddMainMenuNavRow(navPanel.transform, "Store", "Store", "scn02_icon_store_cart.png", 718f, false, WarlineCaptureRoute.CommandExchange, navPanelSize);
        AddMainMenuNavRow(navPanel.transform, "Commander", "Commander", "scn02_icon_commander_bust.png", 959f, false, WarlineCaptureRoute.CommanderProfile, navPanelSize);
        AddMainMenuNavRow(navPanel.transform, "Settings", "Settings", "scn02_icon_settings_gear.png", 1198f, false, WarlineCaptureRoute.Settings, navPanelSize);

        GameObject commsPanel = CreateMainMenuRect("CommsStatusPanel", parent, new Rect(41f, 1485f, 574f, 280f), MainMenuSideSize);
        AddMainMenuSpriteCentered(commsPanel.transform, "Frame", "scn02_comms_status_panel_frame.png", Vector2.zero, new Vector2(574f, 280f), false);
        AddMainMenuSpriteCentered(commsPanel.transform, "StatusIcon", "scn02_icon_lightning_small.png", new Vector2(-198f, 16f), new Vector2(72f, 96f), true);
        AddTextCentered(commsPanel.transform, "Label", "COMMS ONLINE", new Vector2(66f, 24f), new Vector2(330f, 64f), 34f, TextAlignmentOptions.Left, Accent);
        AddTextCentered(commsPanel.transform, "Detail", "Secure Channel 7", new Vector2(66f, -36f), new Vector2(330f, 48f), 24f, TextAlignmentOptions.Left, MutedText);
    }

    private static void AddMainMenuNavRow(
        Transform parent,
        string name,
        string label,
        string icon,
        float y,
        bool selected,
        WarlineCaptureRoute route,
        Vector2 parentSize)
    {
        GameObject row = CreateMainMenuRect($"Nav_{name}", parent, new Rect(0f, y, 574f, 207f), parentSize);
        AddMainMenuSpriteCentered(row.transform, "Frame", selected ? "scn02_nav_button_selected_frame.png" : "scn02_nav_button_inactive_frame.png", Vector2.zero, new Vector2(574f, 207f), false);
        AddMainMenuSpriteCentered(row.transform, "Icon", icon, new Vector2(-220f, 0f), new Vector2(116f, 116f), true);
        AddTextCentered(row.transform, "Text", label, new Vector2(86f, -2f), new Vector2(330f, 80f), 46f, TextAlignmentOptions.Left, selected ? Accent : Text);
        AddRouteButtonHotspot(row.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, route);
    }

    private static void BuildMainMenuCenter(Transform parent)
    {
        GameObject container = CreateMainMenuRect("ModeCardsContainer", parent, new Rect(591f, 410f, 2178f, 985f), MainMenuMiddleSize);
        Vector2 containerSize = container.GetComponent<RectTransform>().sizeDelta;
        AddMainMenuModeCard(container.transform, "Campaign", "CAMPAIGN", "Advance through the campaign and liberate the city districts.", "PROGRESS", "35%", "scn02_campaign_thumbnail_art.png", "scn02_icon_campaign_crosshair.png", 0f, 0.35f, Accent, WarlineCaptureRoute.MainMenu, containerSize);
        AddMainMenuModeCard(container.transform, "Operations", "OPERATIONS", "Stabilize districts, gather intel and expand our influence.", "DISTRICTS CONTROLLED", "6/18", "scn02_operations_thumbnail_art.png", "scn02_icon_operations_pin.png", 740f, 0.33f, Blue, WarlineCaptureRoute.OperationDashboard, containerSize);
        AddMainMenuModeCard(container.transform, "Skirmish", "SKIRMISH", "Custom battles. Configure rules and test your tactics.", "CUSTOM SETUPS", "3", "scn02_skirmish_thumbnail_art.png", "scn02_icon_skirmish_blades.png", 1480f, 0.25f, new Color32(159, 174, 62, 255), WarlineCaptureRoute.QuickCustomSetup, containerSize);
    }

    private static void AddMainMenuModeCard(
        Transform parent,
        string name,
        string title,
        string subtitle,
        string progressLabel,
        string progressValue,
        string art,
        string emblem,
        float x,
        float progress,
        Color progressColor,
        WarlineCaptureRoute route,
        Vector2 parentSize)
    {
        GameObject card = CreateMainMenuRect($"Card_{name}", parent, new Rect(x, 0f, 698f, 985f), parentSize);
        AddMainMenuSpriteCentered(card.transform, "Frame", "scn02_mode_card_frame.png", Vector2.zero, new Vector2(698f, 985f), false);
        AddMainMenuSpriteCentered(card.transform, "TitleIcon", emblem, new Vector2(-238f, 388f), new Vector2(96f, 96f), true);
        AddTextCentered(card.transform, "Title", title, new Vector2(62f, 392f), new Vector2(430f, 92f), 58f, TextAlignmentOptions.Left, Text);

        GameObject viewport = CreateCenteredRect("ThumbnailViewport", card.transform, new Vector2(0f, 162f), new Vector2(652f, 427f));
        viewport.AddComponent<RectMask2D>();
        AddMainMenuSpriteCentered(viewport.transform, "ThumbnailArt", art, Vector2.zero, new Vector2(996f, 427f), true);
        AddMainMenuSpriteCentered(card.transform, "ThumbnailMaskFrame", "scn02_mode_card_thumbnail_mask_frame.png", new Vector2(0f, 162f), new Vector2(652f, 427f), false);

        AddTextCentered(card.transform, "Description", subtitle, new Vector2(0f, -168f), new Vector2(590f, 150f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(card.transform, "ProgressLabel", progressLabel, new Vector2(-168f, -310f), new Vector2(310f, 54f), 28f, TextAlignmentOptions.Left, MutedText);
        AddTextCentered(card.transform, "ProgressValue", progressValue, new Vector2(238f, -310f), new Vector2(160f, 54f), 34f, TextAlignmentOptions.Right, Accent);
        AddMainMenuSpriteCentered(card.transform, "ProgressMeter", "scn02_mode_progress_meter_frame.png", new Vector2(0f, -388f), new Vector2(590f, 52f), false);
        AddMainMenuProgressSegments(card.transform, "ProgressSegments", new Vector2(0f, -388f), 500f, 18f, 12, progress, progressColor);
        AddRouteButtonHotspot(card.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, route);
    }

    private static void AddMainMenuProgressSegments(
        Transform parent,
        string name,
        Vector2 localPosition,
        float width,
        float height,
        int count,
        float progress,
        Color fillColor)
    {
        GameObject group = CreateCenteredRect(name, parent, localPosition, new Vector2(width, height));
        int filledCount = Mathf.Clamp(Mathf.RoundToInt(count * progress), 0, count);
        float gap = 8f;
        float segmentWidth = (width - gap * (count - 1)) / count;
        Color emptyColor = new(0.18f, 0.19f, 0.16f, 0.92f);

        for (int index = 0; index < count; index++)
        {
            float x = -width * 0.5f + segmentWidth * 0.5f + index * (segmentWidth + gap);
            AddSolidCentered(group.transform, $"Segment_{index:00}", new Vector2(x, 0f), new Vector2(segmentWidth, height), index < filledCount ? fillColor : emptyColor);
        }
    }

    private static void BuildMainMenuCommanderPanel(Transform parent)
    {
        GameObject panel = CreateMainMenuRect("CommanderPanel", parent, new Rect(0f, 83f, 719f, 1483f), MainMenuSideSize);
        AddMainMenuSpriteCentered(panel.transform, "Frame", "scn02_commander_panel_frame.png", Vector2.zero, new Vector2(719f, 1483f), false);
        AddMainMenuSpriteCentered(panel.transform, "TitleIcon", "scn02_resource_command_shield.png", new Vector2(-232f, 614f), new Vector2(78f, 98f), true);
        AddTextCentered(panel.transform, "Title", "COMMANDER", new Vector2(64f, 616f), new Vector2(420f, 82f), 52f, TextAlignmentOptions.Left, Text);

        GameObject portrait = CreateMainMenuRect("PortraitPanel", panel.transform, new Rect(83f, 163f, 528f, 507f), new Vector2(719f, 1483f));
        AddMainMenuSpriteCentered(portrait.transform, "Frame", "scn02_commander_portrait_frame.png", Vector2.zero, new Vector2(528f, 507f), false);
        AddMainMenuSpriteCentered(portrait.transform, "Portrait", "scn02_commander_portrait_art.png", new Vector2(0f, -12f), new Vector2(460f, 474f), true);

        GameObject identity = CreateMainMenuRect("IdentityPanel", panel.transform, new Rect(78f, 704f, 560f, 142f), new Vector2(719f, 1483f));
        AddMainMenuSpriteCentered(identity.transform, "Badge", "scn02_resource_command_shield.png", new Vector2(-222f, 0f), new Vector2(92f, 116f), true);
        AddTextCentered(identity.transform, "Name", "FIELD COMMANDER", new Vector2(82f, 16f), new Vector2(390f, 54f), 40f, TextAlignmentOptions.Left, Text);
        AddTextCentered(identity.transform, "Level", "LEVEL 38", new Vector2(82f, -42f), new Vector2(390f, 50f), 34f, TextAlignmentOptions.Left, Accent);

        GameObject readiness = CreateMainMenuRect("ReadinessPanel", panel.transform, new Rect(78f, 904f, 592f, 131f), new Vector2(719f, 1483f));
        AddText(readiness.transform, "Label", "READINESS", new Rect(0f, 0f, 260f, 58f), 36f, TextAlignmentOptions.Left, Text);
        AddMainMenuSprite(readiness.transform, "Segments", "scn02_readiness_segments.png", new Rect(0f, 70f, 530f, 48f), readiness.GetComponent<RectTransform>().sizeDelta, false);

        GameObject lockedRows = CreateMainMenuRect("LockedRowsContainer", panel.transform, new Rect(47f, 1084f, 624f, 348f), new Vector2(719f, 1483f));
        AddLockedCommanderRow(lockedRows.transform, "SquadManagementRow", "SQUAD MANAGEMENT\nLOCKED", 0f);
        AddLockedCommanderRow(lockedRows.transform, "IntelReportRow", "INTEL REPORT\nLOCKED", 190f);

        GameObject deployVisual = CreateMainMenuRect("DeployOperationButton", parent, new Rect(-421f, 1508f, 1141f, 264f), MainMenuSideSize);
        AddMainMenuSpriteCentered(deployVisual.transform, "Frame", "scn02_deploy_cta_frame.png", Vector2.zero, new Vector2(1141f, 264f), false);
        AddTextCentered(deployVisual.transform, "Text", "DEPLOY OPERATION", new Vector2(-70f, 0f), new Vector2(760f, 112f), 64f, TextAlignmentOptions.Center, Color.black);
        AddMainMenuSpriteCentered(deployVisual.transform, "Chevrons", "scn02_deploy_chevrons.png", new Vector2(448f, 0f), new Vector2(150f, 108f), true);
        AddRouteButtonHotspot(deployVisual.transform, "Hotspot", StretchRect(), UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);

        AddRouteHotspot(parent, "CommanderPortraitButton", new Rect(83f, 246f, 528f, 507f), WarlineCaptureRoute.CommanderProfile);
        AddRouteButtonHotspot(parent, "DeployCommandButton", new Rect(-421f, 1508f, 1141f, 264f), UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);
    }

    private static void AddLockedCommanderRow(Transform parent, string name, string label, float y)
    {
        GameObject row = CreateMainMenuRect(name, parent, new Rect(0f, y, 624f, 158f), new Vector2(624f, 348f));
        AddMainMenuSpriteCentered(row.transform, "Frame", "scn02_locked_row_frame.png", Vector2.zero, new Vector2(624f, 158f), false);
        AddMainMenuSpriteCentered(row.transform, "LockIcon", "scn02_icon_lock.png", new Vector2(-238f, 0f), new Vector2(72f, 88f), true);
        AddTextCentered(row.transform, "Label", label, new Vector2(74f, 0f), new Vector2(410f, 94f), 34f, TextAlignmentOptions.Left, MutedText);
    }

    private static void AddHeaderStat(Transform parent, float x, string label, string value, Color valueColor)
    {
        AddSolid(parent, $"{label}IconSlot", new Rect(x, 34f, 70f, 70f), new Color(0.11f, 0.11f, 0.09f, 1f));
        AddText(parent, $"{label}Label", label, new Rect(x + 92f, 28f, 220f, 30f), 20f, TextAlignmentOptions.Left, MutedText);
        AddText(parent, $"{label}Value", value, new Rect(x + 92f, 60f, 230f, 46f), 32f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddNavButton(Transform parent, float y, string label, bool selected)
    {
        Color fill = selected ? new Color(0.28f, 0.28f, 0.12f, 0.95f) : Panel;
        AddSolid(parent, $"{label}_Button", new Rect(16f, y + 18f, 328f, 82f), fill);
        AddText(parent, $"{label}_Label", label, new Rect(96f, y + 38f, 220f, 42f), 26f, TextAlignmentOptions.Left, Text);
        AddSolid(parent, $"{label}_Icon", new Rect(42f, y + 42f, 34f, 34f), selected ? Accent : MutedText);
    }

    private static void AddModeCard(Transform parent, float x, string title, string subtitle)
    {
        AddPanel(parent, $"{title}_Card", new Rect(x, 150f, 460f, 520f));
        AddText(parent, $"{title}_Title", title, new Rect(x + 48f, 188f, 364f, 46f), 32f, TextAlignmentOptions.Left, Text);
        AddSolid(parent, $"{title}_Art", new Rect(x + 42f, 260f, 376f, 245f), new Color(0.08f, 0.10f, 0.085f, 1f));
        AddText(parent, $"{title}_Subtitle", subtitle, new Rect(x + 48f, 538f, 364f, 76f), 22f, TextAlignmentOptions.Left, MutedText);
        AddSolid(parent, $"{title}_Progress", new Rect(x + 48f, 628f, 260f, 18f), Accent);
    }

    private static void AddProfileTab(Transform parent, float y, string label, bool selected)
    {
        AddSolid(parent, $"{label}_Tab", new Rect(42f, y, 276f, 58f), selected ? new Color(0.30f, 0.29f, 0.12f, 0.95f) : PanelMuted);
        AddText(parent, $"{label}_Label", label, new Rect(64f, y + 12f, 232f, 32f), 20f, TextAlignmentOptions.Left, selected ? Accent : Text);
    }

    private static void AddMetricCard(Transform parent, float x, float y, string label, string value)
    {
        AddPanel(parent, $"{label}_Metric", new Rect(x, y, 210f, 118f));
        AddText(parent, $"{label}_MetricLabel", label, new Rect(x + 22f, y + 22f, 166f, 28f), 18f, TextAlignmentOptions.Center, MutedText);
        AddText(parent, $"{label}_MetricValue", value, new Rect(x + 22f, y + 56f, 166f, 42f), 28f, TextAlignmentOptions.Center, Accent);
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect);
        root.GetComponent<CanvasGroup>().alpha = 1f;
        return root;
    }

    private static GameObject CreateGroup(string name, Transform parent, Rect rect)
    {
        GameObject group = CreateRect(name, parent, rect);
        return group;
    }

    private static GameObject CreateRect(string name, Transform parent, Rect rect)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        ApplyTopLeftRect(rectTransform, rect);
        return obj;
    }

    private static void AddPanel(Transform parent, string name, Rect rect)
    {
        AddSolid(parent, name, rect, Panel);
        AddSolid(parent, $"{name}_TopStroke", new Rect(rect.x, rect.y, rect.width, 2f), Stroke);
        AddSolid(parent, $"{name}_BottomStroke", new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), Stroke);
    }

    private static Image AddSolid(Transform parent, string name, Rect rect, Color color)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image AddStretchSolid(Transform parent, string name, Color color)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Stretch(obj.GetComponent<RectTransform>());
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image AddSolidCentered(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void AddRouteButton(
        Transform parent,
        string name,
        string label,
        Rect rect,
        UiShellRouteIntent intent,
        WarlineCaptureRoute route)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.69f, 0.45f, 0.08f, 0.96f);
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.42f, 1f);
        colors.pressedColor = new Color(0.88f, 0.56f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = obj.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(intent, route, false);

        AddText(obj.transform, "Label", label, StretchRect(), 27f, TextAlignmentOptions.Center, Color.black);
    }

    private static void AddRouteHotspot(Transform parent, string name, Rect rect, WarlineCaptureRoute route)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = Clear;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.pressedColor = new Color(1f, 0.82f, 0.3f, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = obj.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(UiShellRouteIntent.OpenMenuRoute, route, false);
    }

    private static void AddRouteButtonHotspot(
        Transform parent,
        string name,
        Rect rect,
        UiShellRouteIntent intent,
        WarlineCaptureRoute route)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = Clear;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.42f, 0.18f);
        colors.pressedColor = new Color(1f, 0.72f, 0.18f, 0.28f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = obj.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(intent, route, false);
    }

    private static void AddResultConfirmButton(Transform parent, string name, string label, Rect rect)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = PanelMuted;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.75f, 0.32f, 1f);
        colors.pressedColor = new Color(0.74f, 0.50f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        obj.AddComponent<WarlineCaptureShellResultConfirmButtonView>();
        AddText(obj.transform, "Label", label, StretchRect(), 30f, TextAlignmentOptions.Center, Text);
    }

    private static TMP_Text AddText(Transform parent, string name, string value, Rect rect, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = CreateRect(name, parent, rect);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static Image AddMainMenuSprite(Transform parent, string name, string spriteName, Rect rect, Vector2 parentSize, bool preserveAspect)
    {
        GameObject obj = CreateMainMenuRect(name, parent, rect, parentSize);
        return ConfigureMainMenuImage(obj, spriteName, preserveAspect);
    }

    private static Image AddMainMenuSpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureMainMenuImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureMainMenuImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadMainMenuSprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadMainMenuSprite(string spriteName)
    {
        string path = $"{MainMenuLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new InvalidOperationException($"Missing Main Menu sprite at {path}.");
        return sprite;
    }

    private static TMP_Text AddTextCentered(
        Transform parent,
        string name,
        string value,
        Vector2 localPosition,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateMainMenuRect(string name, Transform parent, Rect topLeftRect, Vector2 parentSize)
    {
        Vector2 center = new(
            topLeftRect.x + topLeftRect.width * 0.5f - parentSize.x * 0.5f,
            parentSize.y * 0.5f - topLeftRect.y - topLeftRect.height * 0.5f);
        return CreateCenteredRect(name, parent, center, new Vector2(topLeftRect.width, topLeftRect.height));
    }

    private static GameObject CreateTopLeftMainMenuRect(string name, Transform parent, Rect topLeftRect)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(topLeftRect.x, -topLeftRect.y);
        rectTransform.sizeDelta = new Vector2(topLeftRect.width, topLeftRect.height);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        return obj;
    }

    private static GameObject CreateTopRightMainMenuRect(string name, Transform parent, float right, float top, Vector2 size)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-right, -top);
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        return obj;
    }

    private static GameObject CreateCenteredRect(string name, Transform parent, Vector2 localPosition, Vector2 size)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = localPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        return obj;
    }

    private static Rect StretchRect() => new(0f, 0f, 0f, 0f);

    private static void ApplyTopLeftRect(RectTransform rectTransform, Rect rect)
    {
        if (rect.width <= 0f && rect.height <= 0f)
        {
            Stretch(rectTransform);
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
        rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ValidatePrefab(string path, string expectedRootName, params string[] requiredChildren)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException($"Missing GameUI content prefab at {path}.");
        if (prefab.name != expectedRootName)
            throw new InvalidOperationException($"{path} root must be named {expectedRootName}.");
        if (prefab.GetComponent<RectTransform>() == null)
            throw new InvalidOperationException($"{path} root must be a RectTransform.");
        if (prefab.GetComponent<CanvasGroup>() == null)
            throw new InvalidOperationException($"{path} root must contain a CanvasGroup.");
        if (prefab.GetComponentInChildren<Canvas>(true) != null)
            throw new InvalidOperationException($"{path} must not contain a nested Canvas.");

        foreach (string childName in requiredChildren)
        {
            if (prefab.transform.Find(childName) == null)
                throw new InvalidOperationException($"{path} is missing required child {childName}.");
        }
    }

    private static void ValidateMainMenuContentPrefab()
    {
        ValidateMainMenuSourceContract();

        ValidatePrefab(MainMenuPrefabPath, "SCN02_MainMenuContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        ValidateDirectChildren(
            prefab.transform,
            MainMenuPrefabPath,
            "MenuBackgroundContent",
            "HeaderContent",
            "LeftContent",
            "MiddleContent",
            "RightContent");
        ValidateRectSize(prefab.transform.Find("MenuBackgroundContent") as RectTransform, MainMenuBackgroundSize, "SCN02 MenuBackgroundContent");
        ValidateRectSize(prefab.transform.Find("HeaderContent") as RectTransform, MainMenuHeaderSize, "SCN02 HeaderContent");
        ValidateRectSize(prefab.transform.Find("LeftContent") as RectTransform, MainMenuSideSize, "SCN02 LeftContent");
        ValidateRectSize(prefab.transform.Find("MiddleContent") as RectTransform, MainMenuMiddleSize, "SCN02 MiddleContent");
        ValidateRectSize(prefab.transform.Find("RightContent") as RectTransform, MainMenuSideSize, "SCN02 RightContent");

        RequireMainMenuChild(prefab, "MenuBackgroundContent/BackgroundViewport/BackgroundArt");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderLogoPanel/Frame");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderLogoPanel/Logo");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderResourceArea/CreditsPanel/Icon");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderResourceArea/SuppliesPanel/Icon");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderResourceArea/CommandPanel/Icon");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderActionsPanel/InboxButton/Icon");
        RequireMainMenuChild(prefab, "HeaderContent/HeaderActionsPanel/SettingsButton/Icon");
        RequireMainMenuChild(prefab, "LeftContent/LeftNavPanel/Nav_Campaign/Icon");
        RequireMainMenuChild(prefab, "LeftContent/LeftNavPanel/Nav_Campaign/Hotspot");
        RequireMainMenuChild(prefab, "LeftContent/LeftNavPanel/Nav_Commander/Hotspot");
        RequireMainMenuChild(prefab, "LeftContent/CommsStatusPanel/Frame");
        RequireMainMenuChild(prefab, "LeftContent/CommsStatusPanel/Label");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Campaign/ThumbnailViewport/ThumbnailArt");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Campaign/Hotspot");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Operations/ThumbnailViewport/ThumbnailArt");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Operations/Hotspot");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Skirmish/ThumbnailViewport/ThumbnailArt");
        RequireMainMenuChild(prefab, "MiddleContent/ModeCardsContainer/Card_Skirmish/Hotspot");
        RequireMainMenuChild(prefab, "RightContent/CommanderPanel/PortraitPanel/Portrait");
        RequireMainMenuChild(prefab, "RightContent/CommanderPanel/LockedRowsContainer/SquadManagementRow/LockIcon");
        RequireMainMenuChild(prefab, "RightContent/CommanderPanel/LockedRowsContainer/IntelReportRow/LockIcon");
        RequireMainMenuChild(prefab, "RightContent/DeployOperationButton/Hotspot");
        RequireMainMenuChild(prefab, "RightContent/CommanderPortraitButton");
        RequireMainMenuChild(prefab, "RightContent/DeployCommandButton");

        ValidateCenteredRect(prefab, "HeaderContent/HeaderLogoPanel/Frame");
        ValidateCenteredRect(prefab, "HeaderContent/HeaderLogoPanel/Logo");
        ValidateCenteredRect(prefab, "LeftContent/LeftNavPanel/Nav_Campaign/Frame");
        ValidateCenteredRect(prefab, "MiddleContent/ModeCardsContainer/Card_Campaign/Frame");
        ValidateCenteredRect(prefab, "MiddleContent/ModeCardsContainer/Card_Campaign/ThumbnailViewport/ThumbnailArt");
        ValidateCenteredRect(prefab, "RightContent/CommanderPanel/Frame");
        ValidateCenteredRect(prefab, "RightContent/CommanderPanel/PortraitPanel/Portrait");
        ValidateMainMenuSpriteSources(prefab);
    }

    private static void RequireMainMenuChild(GameObject prefab, string path)
    {
        if (prefab.transform.Find(path) == null)
            throw new InvalidOperationException($"{MainMenuPrefabPath} is missing required main menu child {path}.");
    }

    private static void ValidateDirectChildren(Transform parent, string owner, params string[] expectedNames)
    {
        if (parent.childCount != expectedNames.Length)
            throw new InvalidOperationException($"{owner} must have exactly {expectedNames.Length} direct children, but has {parent.childCount}.");

        for (int i = 0; i < expectedNames.Length; i++)
        {
            Transform child = parent.Find(expectedNames[i]);
            if (child == null || child.parent != parent)
                throw new InvalidOperationException($"{owner} must have direct child {expectedNames[i]}.");
        }
    }

    private static void ValidateCenteredRect(GameObject prefab, string path)
    {
        RectTransform rect = prefab.transform.Find(path) as RectTransform;
        if (rect == null)
            throw new InvalidOperationException($"{MainMenuPrefabPath} is missing centered rect {path}.");

        Vector2 center = new(0.5f, 0.5f);
        if (Vector2.Distance(rect.anchorMin, center) > 0.001f ||
            Vector2.Distance(rect.anchorMax, center) > 0.001f ||
            Vector2.Distance(rect.pivot, center) > 0.001f)
        {
            throw new InvalidOperationException($"{path} must use centered anchors and pivot.");
        }
    }

    private static void ValidateMainMenuSpriteSources(GameObject prefab)
    {
        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = images[i].sprite;
            if (sprite == null)
                continue;

            string path = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
            if (string.Equals(path, MainMenuVisualLockReferencePath, StringComparison.Ordinal) ||
                path.StartsWith("Assets/Game/Art/UI/Generated/MainMenu/", StringComparison.Ordinal) ||
                path.StartsWith("Assets/Game/Art/UI/Generated/MainMenuAlt/", StringComparison.Ordinal) ||
                path.IndexOf("V15B", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException($"{images[i].name} uses forbidden SCN02 main menu sprite source: {path}");
            }
        }
    }

    private static void ValidateMainMenuSourceContract()
    {
        RequireProjectFile(MainMenuVisualLockReferencePath);
        RequireProjectDirectory(MainMenuVisualLockLayersRoot);
        RequireProjectFile(MainMenuVisualLockManifestPath);
        RequireProjectDirectory(MainMenuLayerRoot);

        string normalizedLayerRoot = MainMenuLayerRoot.Replace('\\', '/');
        if (normalizedLayerRoot.StartsWith("Assets/Game/Art/UI/Generated/MainMenu/", StringComparison.Ordinal) ||
            normalizedLayerRoot.StartsWith("Assets/Game/Art/UI/Generated/MainMenuAlt/", StringComparison.Ordinal) ||
            normalizedLayerRoot.IndexOf("V15B", StringComparison.OrdinalIgnoreCase) >= 0 ||
            string.Equals(normalizedLayerRoot, RejectedMainMenuV15BRequestPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"SCN02 main menu source root is not approved: {MainMenuLayerRoot}. " +
                $"Use the active target contract at {MainMenuVisualLockRoot} and mirrored Unity sprites from the approved V15C layer set only.");
        }

        for (int i = 0; i < RequiredMainMenuLayerSprites.Length; i++)
            RequireProjectFile($"{MainMenuLayerRoot}/{RequiredMainMenuLayerSprites[i]}");
    }

    private static void RequireProjectFile(string projectRelativePath)
    {
        string absolutePath = ToProjectAbsolutePath(projectRelativePath);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"Required SCN02 main menu target source file is missing: {projectRelativePath}", absolutePath);
    }

    private static void RequireProjectDirectory(string projectRelativePath)
    {
        string absolutePath = ToProjectAbsolutePath(projectRelativePath);
        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException($"Required SCN02 main menu target source directory is missing: {projectRelativePath}");
    }

    private static string ToProjectAbsolutePath(string projectRelativePath)
    {
        string localPath = projectRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Directory.GetCurrentDirectory(), localPath);
    }

    private static void ValidateRectSize(RectTransform rect, Vector2 expected, string name)
    {
        if (rect == null)
            throw new InvalidOperationException($"{name} is missing.");
        if (Vector2.Distance(rect.sizeDelta, expected) > 0.5f)
            throw new InvalidOperationException($"{name} must be {expected} but was {rect.sizeDelta}.");
    }

    private static void ValidateProtectedLoadingContentPrefab()
    {
        ValidatePrefab(LoadingPrefabPath, "SCN01_LoadingContent", "LoadingBody");
        ValidateLoadingProgressBinding();
    }

    private static void ValidateLoadingProgressBinding()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPrefabPath);
        Transform body = prefab.transform.Find("LoadingBody");
        WarlineCaptureShellLoadingProgressView progressView = body?.GetComponent<WarlineCaptureShellLoadingProgressView>();
        if (progressView == null)
            throw new InvalidOperationException("SCN01 loading content must contain WarlineCaptureShellLoadingProgressView on LoadingBody.");

        SerializedObject serializedProgress = new(progressView);
        if (serializedProgress.FindProperty("progressFill")?.objectReferenceValue == null ||
            serializedProgress.FindProperty("percentText")?.objectReferenceValue == null ||
            serializedProgress.FindProperty("statusText")?.objectReferenceValue == null)
        {
            throw new InvalidOperationException("SCN01 loading progress view must keep its fill, percent, and status references assigned.");
        }

        if (serializedProgress.FindProperty("fillWidth")?.floatValue <= 0f)
            throw new InvalidOperationException("SCN01 loading progress view must keep a positive fill width.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Game/Prefabs/UI/Shell", "Content");
        EnsureFolder("Assets/Game/Prefabs/UI/Shell", "Popups");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string fullPath = $"{parent}/{name}";
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

        string guid = AssetDatabase.CreateFolder(parent, name);
        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException($"Failed to create folder {fullPath}.");
    }
}
#endif
