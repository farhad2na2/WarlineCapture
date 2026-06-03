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
    private const string ArmoryPrefabPath = ContentFolder + "/SCN19_ArmoryContent.prefab";
    private const string MatchHudPrefabPath = ContentFolder + "/SCN08_MatchHudContent.prefab";
    private const string BuildDrawerPopupPrefabPath = PopupFolder + "/SCN09_BuildDrawerPopup.prefab";
    private const string ResultPopupPrefabPath = PopupFolder + "/POP05_MissionResultPopup.prefab";
    private const string MainMenuVisualLockRoot = "Design/VisualLockLayered/SCN-02_MainMenu";
    private const string MainMenuVisualLockReferencePath = MainMenuVisualLockRoot + "/reference/SCN-02_MainMenu_Landscape_Target.png";
    private const string MainMenuVisualLockLayersRoot = MainMenuVisualLockRoot + "/layers";
    private const string MainMenuVisualLockManifestPath = MainMenuVisualLockRoot + "/layer_manifest.json";
    private const string RejectedMainMenuV15BRequestPath = MainMenuVisualLockRoot + "/layer_requests/SCN-02_MainMenu_Layer_Regeneration_Request_V15B.md";
    private const string MainMenuLayerRoot = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo";
    private const string CommanderProfileLayerRoot = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01";
    private const string ArmoryLayerRoot = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo";
    private const string MatchHudLayerRoot = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01";
    private const string BuildDrawerLayerRoot = "Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo";
    private const string MissionResultLayerRoot = "Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01";

    private static readonly Vector2 MainMenuHeaderSize = new(4800f, 280f);
    private static readonly Vector2 MainMenuBackgroundSize = new(4800f, 2160f);
    private static readonly Vector2 MainMenuBackgroundCoverSize = new(5038f, 2160f);
    private static readonly Vector2 MainMenuSideSize = new(720f, 1640f);
    private static readonly Vector2 MainMenuMiddleSize = new(3360f, 1640f);
    private static readonly Vector2 MatchHudHeaderSize = new(4800f, 280f);
    private static readonly Vector2 MatchHudSideSize = new(720f, 1640f);
    private static readonly Vector2 MatchHudFooterSize = new(4800f, 240f);

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
        SavePrefab(BuildArmoryContent(), ArmoryPrefabPath);
        SavePrefab(BuildMatchHudContent(), MatchHudPrefabPath);
        SavePrefab(BuildBuildDrawerPopup(), BuildDrawerPopupPrefabPath);
        SavePrefab(BuildResultPopup(), ResultPopupPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateStep6();
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_BUILT generated=5 protected=SCN01_LoadingContent.prefab");
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

    [MenuItem("WarlineCapture/UI/Build Match HUD Content Prefab Only")]
    public static void BuildMatchHudOnly()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildMatchHudContent(), MatchHudPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePrefab(MatchHudPrefabPath, "SCN08_MatchHudContent", "HeaderContent", "LeftContent", "RightContent", "FooterContent");
        Debug.Log("WARLINECAPTURE_GAMEUI_MATCH_HUD_CONTENT_BUILT prefab=SCN08_MatchHudContent.prefab");
    }

    [MenuItem("WarlineCapture/UI/Build Commander Profile Content Prefab Only")]
    public static void BuildCommanderProfileOnly()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildCommanderProfileContent(), CommanderProfilePrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePrefab(CommanderProfilePrefabPath, "SCN03_CommanderProfileContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent", "FooterContent");
        Debug.Log("WARLINECAPTURE_GAMEUI_COMMANDER_PROFILE_CONTENT_BUILT prefab=SCN03_CommanderProfileContent.prefab");
    }

    [MenuItem("WarlineCapture/UI/Build Mission Result Popup Prefab Only")]
    public static void BuildMissionResultPopupOnly()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildResultPopup(), ResultPopupPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePrefab(ResultPopupPrefabPath, "POP05_MissionResultPopup", "PopupFrame", "PopupFrame/Actions");
        Debug.Log("WARLINECAPTURE_GAMEUI_MISSION_RESULT_POPUP_BUILT prefab=POP05_MissionResultPopup.prefab");
    }

    [MenuItem("WarlineCapture/UI/Build Build Drawer Popup Prefab Only")]
    public static void BuildBuildDrawerPopupOnly()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildBuildDrawerPopup(), BuildDrawerPopupPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePrefab(BuildDrawerPopupPrefabPath, "SCN09_BuildDrawerPopup", "BuildDrawerRoot", "BuildDrawerRoot/DrawerFrame", "BuildDrawerRoot/DrawerFrame/CardGrid", "BuildDrawerRoot/DrawerFrame/DetailPanel");
        Debug.Log("WARLINECAPTURE_GAMEUI_BUILD_DRAWER_POPUP_BUILT prefab=SCN09_BuildDrawerPopup.prefab");
    }

    [MenuItem("WarlineCapture/UI/Build Armory Content Prefab Only")]
    public static void BuildArmoryOnly()
    {
        EnsureFolders();
        ValidateProtectedLoadingContentPrefab();
        SavePrefab(BuildArmoryContent(), ArmoryPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidatePrefab(ArmoryPrefabPath, "SCN19_ArmoryContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent", "FooterContent");
        Debug.Log("WARLINECAPTURE_GAMEUI_ARMORY_CONTENT_BUILT prefab=SCN19_ArmoryContent.prefab");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Content Prefabs Step 6")]
    public static void ValidateStep6()
    {
        ValidateProtectedLoadingContentPrefab();
        ValidateMainMenuContentPrefab();
        ValidatePrefab(CommanderProfilePrefabPath, "SCN03_CommanderProfileContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent", "FooterContent");
        ValidatePrefab(ArmoryPrefabPath, "SCN19_ArmoryContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent", "FooterContent");
        ValidatePrefab(MatchHudPrefabPath, "SCN08_MatchHudContent", "HeaderContent", "LeftContent", "RightContent", "FooterContent");
        ValidatePrefab(BuildDrawerPopupPrefabPath, "SCN09_BuildDrawerPopup", "BuildDrawerRoot", "BuildDrawerRoot/DrawerFrame", "BuildDrawerRoot/DrawerFrame/CardGrid", "BuildDrawerRoot/DrawerFrame/DetailPanel");
        ValidatePrefab(ResultPopupPrefabPath, "POP05_MissionResultPopup", "PopupFrame", "PopupFrame/Actions");
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_VALIDATED prefabs=6 protected=SCN01_LoadingContent.prefab");
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

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 1920f, MatchHudFooterSize.x, MatchHudFooterSize.y));
        BuildMainMenuFooter(footer.transform);

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

        GameObject background = CreateGroup("MenuBackgroundContent", root.transform, new Rect(0f, 0f, MainMenuBackgroundSize.x, MainMenuBackgroundSize.y));
        BuildCommanderProfileBackground(background.transform);

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, MainMenuHeaderSize.x, MainMenuHeaderSize.y));
        BuildCommanderProfileHeader(header.transform);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildCommanderProfileLeft(left.transform);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(MainMenuSideSize.x, MainMenuHeaderSize.y, MainMenuMiddleSize.x, MainMenuMiddleSize.y));
        BuildCommanderProfileMiddle(middle.transform);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(4080f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildCommanderProfileRight(right.transform);

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 1920f, MatchHudFooterSize.x, MatchHudFooterSize.y));
        BuildCommanderProfileFooter(footer.transform);

        return root;
    }

    private static void BuildCommanderProfileBackground(Transform parent)
    {
        GameObject viewport = CreateCenteredRect("BackgroundViewport", parent, Vector2.zero, MainMenuBackgroundSize);
        viewport.AddComponent<RectMask2D>();
        AddCommanderProfileSpriteCentered(viewport.transform, "BackgroundArt", "scn03_background_21x9_no_ui.png", Vector2.zero, MainMenuBackgroundCoverSize, false);
    }

    private static void BuildCommanderProfileHeader(Transform parent)
    {
        AddCommanderProfileSpriteCentered(parent, "HeaderFrame", "scn03_chrome_01_top_header_bar_frame.png", Vector2.zero, MainMenuHeaderSize, false);

        GameObject logo = CreateTopLeftMainMenuRect("LogoPanel", parent, new Rect(18f, 14f, 960f, 240f));
        AddCommanderProfileSpriteCentered(logo.transform, "Logo", "shared_brand_logo_lockup.png", new Vector2(-120f, 2f), new Vector2(680f, 150f), true);

        AddTextCentered(parent, "ScreenTitle", "COMMANDER PROFILE", new Vector2(-760f, 44f), new Vector2(920f, 76f), 58f, TextAlignmentOptions.Left, Text);
        AddTextCentered(parent, "ScreenSubtitle", "Identity, progression, rewards, history, and roster access", new Vector2(-760f, -34f), new Vector2(920f, 46f), 30f, TextAlignmentOptions.Left, MutedText);

        AddCommanderHeaderResource(parent, "Credits", "scn03_icon_03_credits_coin.png", "Credits", "187,540", 670f, Accent);
        AddCommanderHeaderResource(parent, "Supplies", "scn03_icon_04_supplies_crate.png", "Supplies", "92,860", 1190f, new Color32(174, 181, 113, 255));
        AddCommanderHeaderResource(parent, "Command", "scn03_icon_05_command_shield.png", "Command", "2,715", 1710f, Blue);

        GameObject inbox = CreateTopRightMainMenuRect("InboxButton", parent, 430f, 34f, new Vector2(170f, 150f));
        AddCommanderProfileSpriteCentered(inbox.transform, "Frame", "scn03_chrome_09_secondary_small_button_frame.png", Vector2.zero, new Vector2(170f, 130f), false);
        AddCommanderProfileSpriteCentered(inbox.transform, "Icon", "scn03_icon_06_inbox_envelope.png", Vector2.zero, new Vector2(70f, 70f), true);
        AddRouteButtonHotspot(inbox.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Inbox);

        GameObject settings = CreateTopRightMainMenuRect("SettingsButton", parent, 242f, 34f, new Vector2(170f, 150f));
        AddCommanderProfileSpriteCentered(settings.transform, "Frame", "scn03_chrome_09_secondary_small_button_frame.png", Vector2.zero, new Vector2(170f, 130f), false);
        AddCommanderProfileSpriteCentered(settings.transform, "Icon", "scn03_icon_07_settings_gear.png", Vector2.zero, new Vector2(70f, 70f), true);
        AddRouteButtonHotspot(settings.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Settings);

        GameObject back = CreateTopRightMainMenuRect("BackButton", parent, 24f, 34f, new Vector2(200f, 150f));
        AddCommanderProfileSpriteCentered(back.transform, "Frame", "scn03_chrome_15_secondary_dark_cta_frame.png", Vector2.zero, new Vector2(200f, 130f), false);
        AddCommanderProfileSpriteCentered(back.transform, "BackIcon", "scn03_icon_08_back_arrow.png", new Vector2(-56f, 0f), new Vector2(46f, 46f), true);
        AddTextCentered(back.transform, "Label", "BACK", new Vector2(34f, 0f), new Vector2(118f, 46f), 32f, TextAlignmentOptions.Center, Text);
        AddRouteButtonHotspot(back.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);
    }

    private static void AddCommanderHeaderResource(Transform parent, string name, string icon, string label, string value, float x, Color valueColor)
    {
        GameObject slot = CreateCenteredRect($"{name}Resource", parent, new Vector2(x, 4f), new Vector2(460f, 150f));
        AddCommanderProfileSpriteCentered(slot.transform, "Frame", "scn03_chrome_12_small_chip_frame.png", Vector2.zero, new Vector2(460f, 126f), false);
        AddCommanderProfileSpriteCentered(slot.transform, "Icon", icon, new Vector2(-162f, 0f), new Vector2(92f, 92f), true);
        AddTextCentered(slot.transform, "Label", label, new Vector2(36f, 28f), new Vector2(260f, 42f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(slot.transform, "Value", value, new Vector2(36f, -30f), new Vector2(260f, 58f), 42f, TextAlignmentOptions.Left, valueColor);
    }

    private static void BuildCommanderProfileLeft(Transform parent)
    {
        GameObject panel = CreateTopLeftMainMenuRect("CommanderIdentityPanel", parent, new Rect(70f, 20f, 1000f, 1568f));
        AddCommanderProfileSpriteCentered(panel.transform, "Frame", "scn03_chrome_02_commander_identity_panel_frame.png", Vector2.zero, new Vector2(1000f, 1568f), false);
        AddCommanderProfileSpriteCentered(panel.transform, "RankIcon", "scn03_icon_02_commander_rank_shield.png", new Vector2(-410f, 664f), new Vector2(90f, 110f), true);
        AddTextCentered(panel.transform, "Title", "COMMANDER ID", new Vector2(-120f, 672f), new Vector2(520f, 62f), 36f, TextAlignmentOptions.Left, Text);

        GameObject portrait = CreateCenteredRect("PortraitPanel", panel.transform, new Vector2(0f, 282f), new Vector2(820f, 680f));
        AddSolidCentered(portrait.transform, "PortraitBack", Vector2.zero, new Vector2(760f, 620f), new Color(0.02f, 0.024f, 0.02f, 0.92f));
        AddCommanderProfileSpriteCentered(portrait.transform, "Portrait", "scn03_portrait_01_commander_portrait_shadowed.png", Vector2.zero, new Vector2(760f, 620f), true);
        AddCommanderProfileSpriteCentered(portrait.transform, "EditIcon", "scn03_icon_09_edit_pencil.png", new Vector2(342f, 238f), new Vector2(54f, 54f), true);

        GameObject identity = CreateCenteredRect("IdentityCard", panel.transform, new Vector2(0f, -238f), new Vector2(820f, 300f));
        AddCommanderProfilePlate(identity.transform, "Plate", Vector2.zero, new Vector2(820f, 300f), new Color(0.018f, 0.024f, 0.021f, 0.86f));
        AddTextCentered(identity.transform, "Name", "FIELD COMMANDER", new Vector2(-120f, 80f), new Vector2(560f, 60f), 42f, TextAlignmentOptions.Left, Text);
        AddTextCentered(identity.transform, "ProfileTag", "PROFILE", new Vector2(268f, 74f), new Vector2(150f, 42f), 24f, TextAlignmentOptions.Right, MutedText);
        AddTextCentered(identity.transform, "Level", "LEVEL 38", new Vector2(-276f, 20f), new Vector2(230f, 46f), 32f, TextAlignmentOptions.Left, Accent);
        AddTextCentered(identity.transform, "XpLabel", "2,740 / 3,200 XP TO LEVEL 39", new Vector2(-146f, -58f), new Vector2(560f, 40f), 26f, TextAlignmentOptions.Left, MutedText);
        AddCommanderProgressBar(identity.transform, "XpProgress", new Vector2(0f, -108f), new Vector2(700f, 46f), 520f);

        AddCommanderProfileButton(panel.transform, "EditIdButton", "scn03_icon_09_edit_pencil.png", "EDIT ID", new Vector2(-230f, -644f), new Vector2(360f, 126f), WarlineCaptureRoute.CommanderProfile);
        AddCommanderProfileButton(panel.transform, "BadgesButton", "scn03_icon_10_badge_shield.png", "BADGES", new Vector2(230f, -644f), new Vector2(360f, 126f), WarlineCaptureRoute.CommanderProfile);
    }

    private static void BuildCommanderProfileMiddle(Transform parent)
    {
        GameObject overview = CreateTopLeftMainMenuRect("OverviewPanel", parent, new Rect(540f, 20f, 1900f, 760f));
        AddCommanderProfileSpriteCentered(overview.transform, "Frame", "scn03_chrome_03_overview_panel_frame.png", Vector2.zero, new Vector2(1900f, 760f), false);
        AddTextCentered(overview.transform, "Title", "OVERVIEW", new Vector2(-790f, 306f), new Vector2(300f, 54f), 38f, TextAlignmentOptions.Left, Text);
        AddCommanderTab(overview.transform, "Overview", "OVERVIEW", -690f, true);
        AddCommanderTab(overview.transform, "Upgrades", "UPGRADES", -350f, false);
        AddCommanderTab(overview.transform, "History", "HISTORY", -10f, false);
        AddCommanderTab(overview.transform, "Badges", "BADGES", 330f, false);
        AddCommanderTab(overview.transform, "Stats", "STATS", 670f, false);

        AddCommanderStatCard(overview.transform, "Missions", "MISSIONS", "42", "completed", -620f, 104f, Accent);
        AddCommanderStatCard(overview.transform, "Victories", "VICTORIES", "36", "86% success", 0f, 104f, Accent);
        AddCommanderStatCard(overview.transform, "Civilians", "CIVILIANS", "91%", "protected", 620f, 104f, Accent);
        AddCommanderStatCard(overview.transform, "Unlocks", "UNLOCKS", "44", "owned items", -620f, -172f, Accent);
        AddCommanderStatCard(overview.transform, "Lost", "UNITS LOST", "18", "lifetime", 0f, -172f, new Color32(210, 96, 66, 255));
        AddCommanderStatCard(overview.transform, "Streak", "BEST STREAK", "7", "operations", 620f, -172f, Accent);

        GameObject track = CreateTopLeftMainMenuRect("RewardTrackPanel", parent, new Rect(540f, 850f, 1900f, 300f));
        AddCommanderProfileSpriteCentered(track.transform, "Frame", "scn03_chrome_05_reward_track_panel_frame.png", Vector2.zero, new Vector2(1900f, 300f), false);
        AddTextCentered(track.transform, "Title", "COMMANDER REWARD TRACK", new Vector2(-628f, 88f), new Vector2(760f, 52f), 34f, TextAlignmentOptions.Left, Text);
        AddCommanderRewardNode(track.transform, "Node35", "35", "CLAIMED", -600f, "scn03_chrome_17_reward_node_claimed.png");
        AddCommanderRewardNode(track.transform, "Node36", "36", "CLAIMED", -360f, "scn03_chrome_17_reward_node_claimed.png");
        AddCommanderRewardNode(track.transform, "Node37", "37", "CLAIMED", -120f, "scn03_chrome_17_reward_node_claimed.png");
        AddCommanderRewardNode(track.transform, "Node38", "38", "READY", 120f, "scn03_chrome_18_reward_node_active.png");
        AddCommanderRewardNode(track.transform, "Node39", "39", "LOCKED", 360f, "scn03_chrome_19_reward_node_locked.png");
        AddCommanderRewardNode(track.transform, "Node40", "40", "LOCKED", 600f, "scn03_chrome_19_reward_node_locked.png");
        AddCommanderCta(track.transform, "ClaimButton", "CLAIM", new Vector2(770f, -12f), new Vector2(320f, 118f), WarlineCaptureRoute.CommanderProfile);

        GameObject history = CreateTopLeftMainMenuRect("RecentHistoryPanel", parent, new Rect(540f, 1205f, 1900f, 360f));
        AddCommanderProfileSpriteCentered(history.transform, "Frame", "scn03_chrome_06_recent_history_panel_frame.png", Vector2.zero, new Vector2(1900f, 360f), false);
        AddTextCentered(history.transform, "Title", "RECENT HISTORY", new Vector2(-690f, 118f), new Vector2(620f, 50f), 34f, TextAlignmentOptions.Left, Text);
        AddCommanderHistoryRow(history.transform, "FirstContact", "scn03_icon_15_reward_wreath.png", "M01 First Contact", "Victory  |  3 stars  |  civilians protected", "14m ago", 18f);
        AddCommanderHistoryRow(history.transform, "OldMarket", "scn03_icon_16_history_crossed_swords.png", "Skirmish: Old Market", "Custom win  |  armor squad survived", "2h ago", -102f);
    }

    private static void BuildCommanderProfileRight(Transform parent)
    {
        GameObject armory = CreateTopLeftMainMenuRect("ArmorySquadsPanel", parent, new Rect(-900f, 20f, 1440f, 620f));
        AddCommanderProfileSpriteCentered(armory.transform, "Frame", "scn03_chrome_04_armory_panel_frame.png", Vector2.zero, new Vector2(1440f, 620f), false);
        AddCommanderProfileSpriteCentered(armory.transform, "Icon", "scn03_icon_11_roster_group.png", new Vector2(-560f, 212f), new Vector2(110f, 110f), true);
        AddTextCentered(armory.transform, "Title", "ARMORY / SQUADS", new Vector2(-170f, 238f), new Vector2(560f, 58f), 36f, TextAlignmentOptions.Left, Text);
        AddTextCentered(armory.transform, "Subtitle", "Access and manage your full roster", new Vector2(-126f, 188f), new Vector2(640f, 42f), 24f, TextAlignmentOptions.Left, MutedText);
        AddArmoryMetric(armory.transform, "Units", "scn03_icon_11_roster_group.png", "UNITS", "24", -360f, 66f);
        AddArmoryMetric(armory.transform, "Vehicles", "scn03_icon_12_vehicle.png", "VEHICLES", "9", 360f, 66f);
        AddArmoryMetric(armory.transform, "Buildings", "scn03_icon_13_building.png", "BUILDINGS", "12", -360f, -84f);
        AddArmoryMetric(armory.transform, "Support", "scn03_icon_14_support_plus.png", "SUPPORT", "8", 360f, -84f);
        AddCommanderCta(armory.transform, "OpenArmoryButton", "OPEN ARMORY", new Vector2(0f, -226f), new Vector2(1280f, 128f), WarlineCaptureRoute.Armory, true);

        GameObject rewards = CreateTopLeftMainMenuRect("ProfileRewardsPanel", parent, new Rect(-900f, 700f, 1440f, 380f));
        AddCommanderProfileSpriteCentered(rewards.transform, "Frame", "scn03_chrome_07_profile_rewards_panel_frame.png", Vector2.zero, new Vector2(1440f, 380f), false);
        AddTextCentered(rewards.transform, "Title", "PROFILE REWARDS", new Vector2(-470f, 122f), new Vector2(640f, 48f), 34f, TextAlignmentOptions.Left, Text);
        AddTextCentered(rewards.transform, "Subtitle", "Next milestone unlocks at Level 39", new Vector2(-398f, 70f), new Vector2(780f, 42f), 24f, TextAlignmentOptions.Left, Text);
        AddTextCentered(rewards.transform, "XpLabel", "XP PROGRESS", new Vector2(-520f, 24f), new Vector2(300f, 36f), 22f, TextAlignmentOptions.Left, MutedText);
        AddCommanderProgressBar(rewards.transform, "XpProgress", new Vector2(-300f, -24f), new Vector2(740f, 42f), 520f);
        AddCommanderProfileSpriteCentered(rewards.transform, "RewardIcon", "scn03_icon_02_commander_rank_shield.png", new Vector2(300f, -118f), new Vector2(70f, 86f), true);
        AddTextCentered(rewards.transform, "RewardText", "LEVEL 39 REWARD\nCommand Authority + cosmetic frame", new Vector2(500f, -118f), new Vector2(470f, 82f), 22f, TextAlignmentOptions.Left, Text);

        GameObject account = CreateTopLeftMainMenuRect("AccountSnapshotPanel", parent, new Rect(-900f, 1140f, 1440f, 430f));
        AddCommanderProfilePlate(account.transform, "Frame", Vector2.zero, new Vector2(1440f, 430f), new Color(0.018f, 0.024f, 0.021f, 0.72f));
        AddTextCentered(account.transform, "Title", "ACCOUNT SNAPSHOT", new Vector2(-430f, 126f), new Vector2(640f, 52f), 34f, TextAlignmentOptions.Left, Text);
        AddSnapshotMetric(account.transform, "Campaign", "CAMPAIGN", "35%", -360f, 46f);
        AddSnapshotMetric(account.transform, "Operations", "OPERATIONS", "6/18", 360f, 46f);
        AddSnapshotMetric(account.transform, "Skirmish", "SKIRMISH", "12 WINS", -360f, -112f);
        AddSnapshotMetric(account.transform, "Readiness", "READINESS", "HIGH", 360f, -112f);
    }

    private static void BuildCommanderProfileFooter(Transform parent)
    {
        GameObject route = CreateCenteredRect("RouteStrip", parent, Vector2.zero, new Vector2(1900f, 132f));
        AddCommanderProfileSpriteCentered(route.transform, "Frame", "scn03_chrome_20_route_strip_frame.png", Vector2.zero, new Vector2(1900f, 132f), false);
        AddTextCentered(route.transform, "MainMenu", "MAIN MENU", new Vector2(-560f, 0f), new Vector2(360f, 48f), 30f, TextAlignmentOptions.Center, Text);
        AddTextCentered(route.transform, "ArrowA", ">", new Vector2(-258f, 0f), new Vector2(80f, 48f), 34f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(route.transform, "Commander", "COMMANDER PROFILE", new Vector2(80f, 0f), new Vector2(460f, 48f), 30f, TextAlignmentOptions.Center, new Color32(169, 198, 90, 255));
        AddTextCentered(route.transform, "ArrowB", ">", new Vector2(418f, 0f), new Vector2(80f, 48f), 34f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(route.transform, "Armory", "ARMORY", new Vector2(650f, 0f), new Vector2(300f, 48f), 30f, TextAlignmentOptions.Center, Text);
        AddRouteButtonHotspot(route.transform, "MainMenuHotspot", new Rect(160f, 20f, 430f, 92f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);
        AddRouteButtonHotspot(route.transform, "ArmoryHotspot", new Rect(1300f, 20f, 360f, 92f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Armory, true);
    }

    private static void AddCommanderProfileButton(Transform parent, string name, string icon, string label, Vector2 position, Vector2 size, WarlineCaptureRoute route)
    {
        GameObject button = CreateCenteredRect(name, parent, position, size);
        AddCommanderProfileSpriteCentered(button.transform, "Frame", "scn03_chrome_09_secondary_small_button_frame.png", Vector2.zero, size, false);
        AddCommanderProfileSpriteCentered(button.transform, "Icon", icon, new Vector2(-70f, 0f), new Vector2(54f, 54f), true);
        AddTextCentered(button.transform, "Label", label, new Vector2(48f, 0f), new Vector2(134f, 44f), 30f, TextAlignmentOptions.Center, Text);
        AddRouteButtonHotspot(button.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, route);
    }

    private static void AddCommanderTab(Transform parent, string name, string label, float x, bool selected)
    {
        GameObject tab = CreateCenteredRect($"{name}Tab", parent, new Vector2(x, 256f), new Vector2(300f, 90f));
        AddCommanderProfileSpriteCentered(tab.transform, "Frame", selected ? "scn03_chrome_10_selected_small_button_frame.png" : "scn03_chrome_09_secondary_small_button_frame.png", Vector2.zero, new Vector2(300f, 78f), false);
        AddTextCentered(tab.transform, "Label", label, Vector2.zero, new Vector2(250f, 42f), 28f, TextAlignmentOptions.Center, selected ? Text : MutedText);
    }

    private static void AddCommanderStatCard(Transform parent, string name, string label, string value, string suffix, float x, float y, Color valueColor)
    {
        GameObject card = CreateCenteredRect($"{name}StatCard", parent, new Vector2(x, y), new Vector2(500f, 190f));
        AddCommanderProfilePlate(card.transform, "Plate", Vector2.zero, new Vector2(500f, 190f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddTextCentered(card.transform, "Label", label, new Vector2(-62f, 48f), new Vector2(360f, 40f), 25f, TextAlignmentOptions.Left, Text);
        AddTextCentered(card.transform, "Value", value, new Vector2(-130f, -8f), new Vector2(220f, 62f), 44f, TextAlignmentOptions.Left, valueColor);
        AddTextCentered(card.transform, "Suffix", suffix, new Vector2(-54f, -64f), new Vector2(340f, 38f), 23f, TextAlignmentOptions.Left, MutedText);
    }

    private static void AddCommanderRewardNode(Transform parent, string name, string level, string state, float x, string sprite)
    {
        GameObject node = CreateCenteredRect(name, parent, new Vector2(x, -26f), new Vector2(170f, 160f));
        AddCommanderProfileSpriteCentered(node.transform, "Node", sprite, new Vector2(0f, 26f), new Vector2(110f, 110f), true);
        AddTextCentered(node.transform, "Level", level, new Vector2(0f, 26f), new Vector2(86f, 54f), 28f, TextAlignmentOptions.Center, Text);
        AddTextCentered(node.transform, "State", state, new Vector2(0f, -66f), new Vector2(160f, 36f), 23f, TextAlignmentOptions.Center, state == "READY" ? Accent : MutedText);
    }

    private static void AddCommanderCta(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        WarlineCaptureRoute route,
        bool pushHistory = false)
    {
        GameObject button = CreateCenteredRect(name, parent, position, size);
        AddCommanderProfileSpriteCentered(button.transform, "Frame", "scn03_chrome_14_primary_gold_cta_frame.png", Vector2.zero, size, false);
        AddTextCentered(button.transform, "Label", label, new Vector2(-34f, 0f), new Vector2(size.x - 160f, 56f), 40f, TextAlignmentOptions.Center, Color.black);
        AddCommanderProfileSpriteCentered(button.transform, "Chevron", "scn03_icon_20_claim_chevron.png", new Vector2(size.x * 0.5f - 76f, 0f), new Vector2(54f, 54f), true);
        AddRouteButtonHotspot(button.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, route, pushHistory);
    }

    private static void AddCommanderHistoryRow(Transform parent, string name, string icon, string title, string subtitle, string time, float y)
    {
        GameObject row = CreateCenteredRect($"{name}Row", parent, new Vector2(0f, y), new Vector2(1700f, 92f));
        AddCommanderProfilePlate(row.transform, "Plate", Vector2.zero, new Vector2(1700f, 82f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddCommanderProfileSpriteCentered(row.transform, "Icon", icon, new Vector2(-760f, 0f), new Vector2(58f, 58f), true);
        AddTextCentered(row.transform, "Title", title, new Vector2(-430f, 14f), new Vector2(660f, 38f), 27f, TextAlignmentOptions.Left, Text);
        AddTextCentered(row.transform, "Subtitle", subtitle, new Vector2(-388f, -24f), new Vector2(720f, 32f), 21f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));
        AddTextCentered(row.transform, "Time", time, new Vector2(345f, 0f), new Vector2(130f, 38f), 23f, TextAlignmentOptions.Center, MutedText);
        AddSmallCommanderAction(row.transform, "ReplayButton", "REPLAY", 590f);
        AddSmallCommanderAction(row.transform, "DetailButton", "DETAIL", 770f);
    }

    private static void AddSmallCommanderAction(Transform parent, string name, string label, float x)
    {
        GameObject button = CreateCenteredRect(name, parent, new Vector2(x, 0f), new Vector2(190f, 62f));
        AddCommanderProfileSpriteCentered(button.transform, "Frame", "scn03_chrome_09_secondary_small_button_frame.png", Vector2.zero, new Vector2(190f, 62f), false);
        AddTextCentered(button.transform, "Label", label, Vector2.zero, new Vector2(160f, 34f), 22f, TextAlignmentOptions.Center, Accent);
    }

    private static void AddArmoryMetric(Transform parent, string name, string icon, string label, string value, float x, float y)
    {
        GameObject metric = CreateCenteredRect($"{name}Metric", parent, new Vector2(x, y), new Vector2(520f, 90f));
        AddCommanderProfilePlate(metric.transform, "Plate", Vector2.zero, new Vector2(520f, 90f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddCommanderProfileSpriteCentered(metric.transform, "Icon", icon, new Vector2(-202f, 0f), new Vector2(50f, 50f), true);
        AddTextCentered(metric.transform, "Label", label, new Vector2(-50f, 0f), new Vector2(230f, 34f), 24f, TextAlignmentOptions.Left, Text);
        AddTextCentered(metric.transform, "Value", value, new Vector2(190f, 0f), new Vector2(90f, 40f), 30f, TextAlignmentOptions.Right, Accent);
    }

    private static void AddSnapshotMetric(Transform parent, string name, string label, string value, float x, float y)
    {
        GameObject metric = CreateCenteredRect($"{name}Snapshot", parent, new Vector2(x, y), new Vector2(520f, 118f));
        AddCommanderProfilePlate(metric.transform, "Plate", Vector2.zero, new Vector2(520f, 118f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddTextCentered(metric.transform, "Label", label, new Vector2(-120f, 24f), new Vector2(260f, 34f), 22f, TextAlignmentOptions.Left, MutedText);
        AddTextCentered(metric.transform, "Value", value, new Vector2(-112f, -22f), new Vector2(280f, 44f), 30f, TextAlignmentOptions.Left, Accent);
    }

    private static GameObject BuildArmoryContent()
    {
        GameObject root = CreateRoot("SCN19_ArmoryContent");

        GameObject background = CreateGroup("MenuBackgroundContent", root.transform, new Rect(0f, 0f, MainMenuBackgroundSize.x, MainMenuBackgroundSize.y));
        BuildArmoryBackground(background.transform);

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, MainMenuHeaderSize.x, MainMenuHeaderSize.y));
        BuildArmoryHeader(header.transform);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildArmoryLeft(left.transform);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(MainMenuSideSize.x, MainMenuHeaderSize.y, MainMenuMiddleSize.x, MainMenuMiddleSize.y));
        BuildArmoryMiddle(middle.transform);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(4080f, MainMenuHeaderSize.y, MainMenuSideSize.x, MainMenuSideSize.y));
        BuildArmoryRight(right.transform);

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 1920f, MatchHudFooterSize.x, MatchHudFooterSize.y));
        BuildArmoryFooter(footer.transform);
        return root;
    }

    private static void BuildArmoryBackground(Transform parent)
    {
        GameObject viewport = CreateCenteredRect("BackgroundViewport", parent, Vector2.zero, MainMenuBackgroundSize);
        viewport.AddComponent<RectMask2D>();
        AddArmorySpriteCentered(viewport.transform, "BackgroundArt", "scn19_background_21x9_no_ui.png", Vector2.zero, MainMenuBackgroundCoverSize, false);
    }

    private static void BuildArmoryHeader(Transform parent)
    {
        AddStretchSolid(parent, "HeaderBackPlate", new Color(0.018f, 0.022f, 0.018f, 0.9f));
        GameObject logo = CreateTopLeftMainMenuRect("HeaderLogoPanel", parent, new Rect(0f, 12f, 820f, 236f));
        AddArmorySpriteCentered(logo.transform, "Frame", "scn19_header_logo_panel_bg.png", Vector2.zero, new Vector2(820f, 170f), false);
        AddTextCentered(logo.transform, "Brand", "WARLINE\nCAPTURE", new Vector2(-160f, 8f), new Vector2(380f, 126f), 42f, TextAlignmentOptions.Left, Text);

        AddArmoryHeaderResource(parent, "Credits", "scn19_resource_credits_coin.png", "Credits", "187,540", -1340f, Accent);
        AddArmoryHeaderResource(parent, "Supplies", "scn19_resource_supplies_crate.png", "Supplies", "92,860", -740f, new Color32(174, 181, 113, 255));
        AddArmoryHeaderResource(parent, "Command", "scn19_resource_command_shield.png", "Command", "2,715", -140f, Blue);

        GameObject actions = CreateTopRightMainMenuRect("HeaderActionsPanel", parent, 24f, 28f, new Vector2(460f, 164f));
        AddArmorySpriteCentered(actions.transform, "Frame", "scn19_header_right_actions_bg.png", Vector2.zero, new Vector2(460f, 150f), false);
        AddArmorySpriteCentered(actions.transform, "InboxIcon", "scn19_icon_inbox_envelope.png", new Vector2(-118f, 0f), new Vector2(70f, 70f), true);
        AddArmorySpriteCentered(actions.transform, "SettingsIcon", "scn19_icon_settings_gear.png", new Vector2(122f, 0f), new Vector2(76f, 76f), true);
        AddRouteButtonHotspot(actions.transform, "InboxHotspot", new Rect(60f, 26f, 150f, 112f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Inbox);
        AddRouteButtonHotspot(actions.transform, "SettingsHotspot", new Rect(254f, 26f, 150f, 112f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Settings);
    }

    private static void BuildArmoryLeft(Transform parent)
    {
        GameObject title = CreateTopLeftMainMenuRect("ArmoryTitleBlock", parent, new Rect(32f, 28f, 700f, 170f));
        AddArmorySpriteCentered(title.transform, "BackFrame", "scn19_title_back_panel_frame.png", new Vector2(52f, 0f), new Vector2(600f, 150f), false);
        AddArmorySpriteCentered(title.transform, "BackIcon", "scn19_icon_back_arrow.png", new Vector2(-260f, 0f), new Vector2(86f, 86f), true);
        AddArmorySpriteCentered(title.transform, "ArmoryIcon", "scn19_icon_armory_crossed_weapons.png", new Vector2(-82f, 16f), new Vector2(88f, 88f), true);
        AddTextCentered(title.transform, "Title", "ARMORY", new Vector2(156f, 28f), new Vector2(310f, 60f), 48f, TextAlignmentOptions.Left, Text);
        AddTextCentered(title.transform, "Subtitle", "Roster Inspection", new Vector2(170f, -34f), new Vector2(350f, 42f), 28f, TextAlignmentOptions.Left, MutedText);
        AddRouteButtonHotspot(title.transform, "BackHotspot", new Rect(0f, 0f, 144f, 150f), UiShellRouteIntent.BackMenuRoute, WarlineCaptureRoute.MainMenu);

        AddArmoryCategory(parent, "Units", "scn19_icon_units_group.png", "UNITS", "24 / 48", 22f, 240f, true);
        AddArmoryCategory(parent, "Vehicles", "scn19_icon_vehicle_truck.png", "VEHICLES", "9 / 24", 22f, 442f, false);
        AddArmoryCategory(parent, "Aircraft", "scn19_icon_aircraft_helicopter.png", "AIRCRAFT", "6 / 16", 22f, 644f, false);
        AddArmoryCategory(parent, "Buildings", "scn19_icon_buildings.png", "BUILDINGS", "12 / 32", 22f, 846f, false);
        AddArmoryCategory(parent, "Support", "scn19_icon_support_plus.png", "SUPPORT", "8 / 16", 22f, 1048f, false);
        AddArmoryCategory(parent, "Upgrades", "scn19_icon_upgrades_chevrons.png", "UPGRADES", "18 / 36", 22f, 1250f, false);

        GameObject comms = CreateTopLeftMainMenuRect("CommsStatusPanel", parent, new Rect(22f, 1470f, 640f, 150f));
        AddArmorySpriteCentered(comms.transform, "Frame", "scn19_comms_status_panel_frame.png", Vector2.zero, new Vector2(640f, 150f), false);
        AddArmorySpriteCentered(comms.transform, "Icon", "scn19_icon_comms_signal.png", new Vector2(-248f, 0f), new Vector2(68f, 68f), true);
        AddTextCentered(comms.transform, "Label", "COMMS ONLINE", new Vector2(-48f, 4f), new Vector2(350f, 50f), 30f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));
    }

    private static void BuildArmoryMiddle(Transform parent)
    {
        AddArmoryDropdown(parent, "FilterDropdown", "FILTER: ALL", new Rect(40f, 28f, 620f, 110f));
        AddArmoryDropdown(parent, "SortDropdown", "SORT: RARITY", new Rect(2360f, 28f, 620f, 110f));

        AddArmoryRosterCard(parent, "RiflemanMaleII", "scn19_art_rifleman_male_ii.png", "RIFLEMAN MALE II", "INFANTRY", "LVL 12", "OWNED", 40f, 180f, true, false);
        AddArmoryRosterCard(parent, "MarksmanMaleI", "scn19_art_marksman_male_i.png", "MARKSMAN MALE I", "INFANTRY", "LVL 9", "OWNED", 660f, 180f, false, false);
        AddArmoryRosterCard(parent, "AssaultBreacher", "scn19_art_assault_breacher_female_ii.png", "ASSAULT BREACHER FEMALE II", "INFANTRY", "LVL 11", "UPGRADE READY", 1280f, 180f, false, false);
        AddArmoryRosterCard(parent, "FieldCommander", "scn19_art_field_commander.png", "FIELD COMMANDER", "COMMAND", "LVL 10", "OWNED", 1900f, 180f, false, false);
        AddArmoryRosterCard(parent, "CargoTruck", "scn19_art_cargo_truck.png", "CARGO TRUCK", "VEHICLE", "LVL 8", "OWNED", 40f, 630f, false, false);
        AddArmoryRosterCard(parent, "CanopyTruck", "scn19_art_canopy_truck.png", "CANOPY TRUCK", "VEHICLE", "LVL 7", "OWNED", 660f, 630f, false, false);
        AddArmoryRosterCard(parent, "AttackHelicopter", "scn19_art_attack_helicopter.png", "ATTACK HELICOPTER", "AIRCRAFT", "LVL 10", "OWNED", 1280f, 630f, false, false);
        AddArmoryRosterCard(parent, "TransportHelicopter", "scn19_art_transport_helicopter.png", "TRANSPORT HELICOPTER", "AIRCRAFT", "LVL 9", "LOCKED", 1900f, 630f, false, true);
        AddArmoryRosterCard(parent, "OilPump", "scn19_art_oil_pump.png", "OIL PUMP", "BUILDING", "LVL 6", "OWNED", 40f, 1080f, false, false);
        AddArmoryRosterCard(parent, "OilRefinery", "scn19_art_oil_refinery.png", "OIL REFINERY", "BUILDING", "LVL 7", "UPGRADE READY", 660f, 1080f, false, false);
        AddArmoryRosterCard(parent, "GuardTower", "scn19_art_guard_tower.png", "GUARD TOWER", "BUILDING", "LVL 5", "OWNED", 1280f, 1080f, false, false);
        AddArmoryRosterCard(parent, "AmmoDepot", "scn19_art_ammunition_depot.png", "AMMUNITION DEPOT", "BUILDING", "LVL 6", "LOCKED", 1900f, 1080f, false, true);
    }

    private static void BuildArmoryRight(Transform parent)
    {
        GameObject panel = CreateTopLeftMainMenuRect("InspectionPanel", parent, new Rect(-520f, 48f, 1200f, 1470f));
        AddSolidCentered(panel.transform, "BackPlate", Vector2.zero, new Vector2(1140f, 1390f), new Color(0.018f, 0.024f, 0.021f, 0.84f));
        AddArmorySpriteCentered(panel.transform, "Frame", "scn19_inspection_panel_frame.png", Vector2.zero, new Vector2(1200f, 1470f), false);
        AddTextCentered(panel.transform, "Name", "RIFLEMAN MALE II", new Vector2(-300f, 610f), new Vector2(520f, 72f), 44f, TextAlignmentOptions.Left, Text);
        AddTextCentered(panel.transform, "Type", "INFANTRY", new Vector2(-348f, 532f), new Vector2(320f, 50f), 32f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));
        AddTextCentered(panel.transform, "Description", "Reliable frontline infantry equipped for a variety of combat situations.", new Vector2(-260f, 410f), new Vector2(520f, 126f), 28f, TextAlignmentOptions.TopLeft, Text);
        AddArmorySpriteCentered(panel.transform, "HeroArt", "scn19_art_rifleman_male_ii.png", new Vector2(455f, 390f), new Vector2(330f, 500f), true);

        AddArmoryStatRow(panel.transform, "Health", "scn19_icon_health_cross.png", "HEALTH", "220", -20f, 198f);
        AddArmoryStatRow(panel.transform, "Damage", "scn19_icon_damage_burst.png", "DAMAGE", "28", -20f, 120f);
        AddArmoryStatRow(panel.transform, "Range", "scn19_icon_range_reticle.png", "RANGE", "35 m", -20f, 42f);
        AddArmoryStatRow(panel.transform, "Speed", "scn19_icon_speed_boot.png", "SPEED", "4.6 m/s", -20f, -36f);

        AddTextCentered(panel.transform, "AbilitiesTitle", "ABILITIES", new Vector2(-382f, -142f), new Vector2(360f, 48f), 28f, TextAlignmentOptions.Left, MutedText);
        AddArmoryAbility(panel.transform, "Move", "scn19_icon_move_runner.png", "MOVE", -390f);
        AddArmoryAbility(panel.transform, "Attack", "scn19_icon_attack_reticle.png", "ATTACK", -130f);
        AddArmoryAbility(panel.transform, "Hold", "scn19_icon_hold_shield.png", "HOLD", 130f);
        AddArmoryAbility(panel.transform, "Patrol", "scn19_icon_patrol_chevrons.png", "PATROL", 390f);

        AddTextCentered(panel.transform, "TrackTitle", "UPGRADE TRACK", new Vector2(-330f, -420f), new Vector2(420f, 48f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(panel.transform, "Tier", "TIER II >>>", new Vector2(350f, -420f), new Vector2(300f, 48f), 28f, TextAlignmentOptions.Right, Accent);
        AddArmorySpriteCentered(panel.transform, "PartsIcon", "scn19_icon_blueprint_parts.png", new Vector2(-410f, -512f), new Vector2(64f, 64f), true);
        AddTextCentered(panel.transform, "PartsLabel", "BLUEPRINT PARTS", new Vector2(-170f, -512f), new Vector2(400f, 48f), 28f, TextAlignmentOptions.Left, Text);
        AddTextCentered(panel.transform, "PartsValue", "38 / 60", new Vector2(378f, -512f), new Vector2(180f, 48f), 30f, TextAlignmentOptions.Right, Accent);
        AddArmoryProgressBar(panel.transform, "BlueprintProgress", new Vector2(60f, -570f), new Vector2(760f, 44f), 520f);
        AddTextCentered(panel.transform, "Source", "SOURCE / UNLOCK\nBarracks Level 4", new Vector2(-170f, -694f), new Vector2(620f, 92f), 28f, TextAlignmentOptions.Left, Text);

        AddArmoryCta(panel.transform, "UpgradeButton", "UPGRADE", new Vector2(-330f, -880f), new Vector2(300f, 128f), true, WarlineCaptureRoute.Armory);
        AddArmoryCta(panel.transform, "InspectButton", "INSPECT ABILITIES", new Vector2(50f, -880f), new Vector2(400f, 128f), false, WarlineCaptureRoute.Armory);
        AddArmoryCta(panel.transform, "EquipButton", "EQUIP", new Vector2(410f, -880f), new Vector2(260f, 128f), false, WarlineCaptureRoute.Armory);
    }

    private static void BuildArmoryFooter(Transform parent)
    {
        AddArmoryFooterTab(parent, "OwnedTab", "scn19_icon_units_group.png", "OWNED", -900f, true);
        AddArmoryFooterTab(parent, "UpgradeTracksTab", "scn19_icon_upgrades_chevrons.png", "UPGRADE TRACKS", -300f, false);
        AddArmoryFooterTab(parent, "PartsTab", "scn19_icon_blueprint_parts.png", "PARTS", 300f, false);
        AddArmoryFooterTab(parent, "GearModulesTab", "scn19_icon_settings_gear.png", "GEAR MODULES", 900f, false);

        GameObject route = CreateCenteredRect("RouteStrip", parent, new Vector2(0f, -70f), new Vector2(1900f, 110f));
        AddArmorySpriteCentered(route.transform, "Frame", "scn19_route_breadcrumb_strip_frame.png", Vector2.zero, new Vector2(1900f, 110f), false);
        AddTextCentered(route.transform, "MainMenu", "MAIN MENU", new Vector2(-560f, 0f), new Vector2(360f, 42f), 28f, TextAlignmentOptions.Center, Text);
        AddTextCentered(route.transform, "ArrowA", ">", new Vector2(-258f, 0f), new Vector2(80f, 42f), 30f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(route.transform, "Commander", "COMMANDER PROFILE", new Vector2(90f, 0f), new Vector2(460f, 42f), 28f, TextAlignmentOptions.Center, Text);
        AddTextCentered(route.transform, "ArrowB", ">", new Vector2(428f, 0f), new Vector2(80f, 42f), 30f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(route.transform, "Armory", "ARMORY", new Vector2(650f, 0f), new Vector2(300f, 42f), 28f, TextAlignmentOptions.Center, new Color32(169, 198, 90, 255));
        AddRouteButtonHotspot(route.transform, "MainMenuHotspot", new Rect(160f, 12f, 430f, 86f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);
        AddRouteButtonHotspot(route.transform, "CommanderHotspot", new Rect(720f, 12f, 560f, 86f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.CommanderProfile);
    }

    private static void AddArmoryHeaderResource(Transform parent, string name, string icon, string label, string value, float x, Color valueColor)
    {
        GameObject slot = CreateCenteredRect($"{name}Resource", parent, new Vector2(x, 8f), new Vector2(520f, 150f));
        AddArmorySpriteCentered(slot.transform, "Frame", "scn19_header_resource_panel_bg.png", Vector2.zero, new Vector2(520f, 132f), false);
        AddArmorySpriteCentered(slot.transform, "Icon", icon, new Vector2(-170f, 0f), new Vector2(94f, 94f), true);
        AddTextCentered(slot.transform, "Label", label, new Vector2(48f, 30f), new Vector2(250f, 40f), 28f, TextAlignmentOptions.Left, Text);
        AddTextCentered(slot.transform, "Value", value, new Vector2(48f, -28f), new Vector2(250f, 56f), 40f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddArmoryCategory(Transform parent, string name, string icon, string label, string value, float x, float y, bool selected)
    {
        GameObject button = CreateTopLeftMainMenuRect($"{name}CategoryButton", parent, new Rect(x, y, 680f, 178f));
        AddArmorySpriteCentered(button.transform, "Frame", selected ? "scn19_category_button_selected_frame.png" : "scn19_category_button_default_frame.png", Vector2.zero, new Vector2(680f, 160f), false);
        AddArmorySpriteCentered(button.transform, "Icon", icon, new Vector2(-234f, 0f), new Vector2(78f, 78f), true);
        AddTextCentered(button.transform, "Label", label, new Vector2(-8f, 28f), new Vector2(360f, 56f), 40f, TextAlignmentOptions.Left, Text);
        AddTextCentered(button.transform, "Value", value, new Vector2(-12f, -42f), new Vector2(350f, 46f), 32f, TextAlignmentOptions.Left, Accent);
    }

    private static void AddArmoryDropdown(Transform parent, string name, string label, Rect rect)
    {
        GameObject dropdown = CreateTopLeftMainMenuRect(name, parent, rect);
        AddArmorySpriteCentered(dropdown.transform, "Frame", "scn19_dropdown_frame.png", Vector2.zero, new Vector2(rect.width, rect.height), false);
        AddTextCentered(dropdown.transform, "Label", label, new Vector2(-86f, 0f), new Vector2(rect.width - 150f, 48f), 30f, TextAlignmentOptions.Left, Accent);
        AddArmorySpriteCentered(dropdown.transform, "Chevron", "scn19_icon_dropdown_chevron.png", new Vector2(rect.width * 0.5f - 78f, 0f), new Vector2(42f, 42f), true);
    }

    private static void AddArmoryRosterCard(
        Transform parent,
        string name,
        string art,
        string title,
        string type,
        string level,
        string state,
        float x,
        float y,
        bool selected,
        bool locked)
    {
        GameObject card = CreateTopLeftMainMenuRect($"{name}RosterCard", parent, new Rect(x, y, 560f, 420f));
        AddSolidCentered(card.transform, "BackPlate", Vector2.zero, new Vector2(530f, 390f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddArmorySpriteCentered(card.transform, "Frame", selected ? "scn19_roster_card_selected_frame.png" : locked ? "scn19_roster_card_locked_frame.png" : "scn19_roster_card_default_frame.png", Vector2.zero, new Vector2(560f, 420f), false);
        AddTextCentered(card.transform, "Title", title, new Vector2(20f, 166f), new Vector2(420f, 46f), 28f, TextAlignmentOptions.Left, Text);
        AddArmorySpriteCentered(card.transform, "Art", art, new Vector2(0f, 42f), new Vector2(500f, 220f), false);
        AddTextCentered(card.transform, "Type", type, new Vector2(-154f, -112f), new Vector2(220f, 38f), 24f, TextAlignmentOptions.Left, Text);
        AddTextCentered(card.transform, "Level", level, new Vector2(186f, -112f), new Vector2(140f, 38f), 24f, TextAlignmentOptions.Right, Text);
        AddArmoryProgressBar(card.transform, "Progress", new Vector2(-110f, -156f), new Vector2(260f, 26f), locked ? 70f : selected ? 190f : 150f);
        AddTextCentered(card.transform, "State", state, new Vector2(154f, -184f), new Vector2(260f, 34f), 24f, TextAlignmentOptions.Right, state == "LOCKED" ? MutedText : new Color32(169, 198, 90, 255));
        AddArmorySpriteCentered(card.transform, "Badge", state == "LOCKED" ? "scn19_badge_locked_padlock.png" : state == "UPGRADE READY" ? "scn19_badge_upgrade_ready_chevrons.png" : "scn19_badge_owned_checkmark.png", new Vector2(244f, -184f), new Vector2(42f, 42f), true);
    }

    private static void AddArmoryStatRow(Transform parent, string name, string icon, string label, string value, float x, float y)
    {
        GameObject row = CreateCenteredRect($"{name}StatRow", parent, new Vector2(x, y), new Vector2(860f, 70f));
        AddSolidCentered(row.transform, "Rule", new Vector2(0f, -34f), new Vector2(860f, 2f), new Color(0.73f, 0.59f, 0.25f, 0.35f));
        AddArmorySpriteCentered(row.transform, "Icon", icon, new Vector2(-376f, 0f), new Vector2(46f, 46f), true);
        AddTextCentered(row.transform, "Label", label, new Vector2(-210f, 0f), new Vector2(300f, 42f), 28f, TextAlignmentOptions.Left, Text);
        AddTextCentered(row.transform, "Value", value, new Vector2(336f, 0f), new Vector2(210f, 42f), 30f, TextAlignmentOptions.Right, Accent);
    }

    private static void AddArmoryAbility(Transform parent, string name, string icon, string label, float x)
    {
        GameObject ability = CreateCenteredRect($"{name}Ability", parent, new Vector2(x, -264f), new Vector2(230f, 150f));
        AddCommanderProfilePlate(ability.transform, "Plate", Vector2.zero, new Vector2(230f, 150f), new Color(0.018f, 0.024f, 0.021f, 0.82f));
        AddArmorySpriteCentered(ability.transform, "Icon", icon, new Vector2(0f, 28f), new Vector2(60f, 60f), true);
        AddTextCentered(ability.transform, "Label", label, new Vector2(0f, -46f), new Vector2(180f, 40f), 24f, TextAlignmentOptions.Center, Text);
    }

    private static void AddArmoryCta(Transform parent, string name, string label, Vector2 position, Vector2 size, bool primary, WarlineCaptureRoute route)
    {
        GameObject button = CreateCenteredRect(name, parent, position, size);
        AddArmorySpriteCentered(button.transform, "Frame", primary ? "scn19_cta_primary_gold_frame.png" : "scn19_cta_secondary_dark_frame.png", Vector2.zero, size, false);
        AddTextCentered(button.transform, "Label", label, Vector2.zero, new Vector2(size.x - 50f, 48f), 30f, TextAlignmentOptions.Center, primary ? Color.black : Text);
        AddRouteButtonHotspot(button.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, route);
    }

    private static void AddArmoryFooterTab(Transform parent, string name, string icon, string label, float x, bool selected)
    {
        GameObject tab = CreateCenteredRect(name, parent, new Vector2(x, 34f), new Vector2(560f, 108f));
        AddArmorySpriteCentered(tab.transform, "Frame", selected ? "scn19_bottom_tab_selected_frame.png" : "scn19_bottom_tab_default_frame.png", Vector2.zero, new Vector2(560f, 94f), false);
        AddArmorySpriteCentered(tab.transform, "Icon", icon, new Vector2(-142f, 0f), new Vector2(56f, 56f), true);
        AddTextCentered(tab.transform, "Label", label, new Vector2(78f, 0f), new Vector2(320f, 44f), 28f, TextAlignmentOptions.Center, selected ? Text : MutedText);
    }

    private static void AddArmoryProgressBar(Transform parent, string name, Vector2 localPosition, Vector2 size, float fillWidth)
    {
        GameObject bar = CreateCenteredRect(name, parent, localPosition, size);
        AddSolidCentered(bar.transform, "Track", Vector2.zero, size, new Color(0.012f, 0.016f, 0.014f, 0.9f));
        float clampedFill = Mathf.Clamp(fillWidth, 0f, size.x - 8f);
        float fillX = -size.x * 0.5f + clampedFill * 0.5f + 4f;
        AddSolidCentered(bar.transform, "Fill", new Vector2(fillX, 0f), new Vector2(clampedFill, size.y - 8f), Accent);
        Color stroke = new(0.73f, 0.59f, 0.25f, 0.7f);
        AddSolidCentered(bar.transform, "TopStroke", new Vector2(0f, size.y * 0.5f - 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "BottomStroke", new Vector2(0f, -size.y * 0.5f + 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "LeftStroke", new Vector2(-size.x * 0.5f + 2f, 0f), new Vector2(3f, size.y), stroke);
        AddSolidCentered(bar.transform, "RightStroke", new Vector2(size.x * 0.5f - 2f, 0f), new Vector2(3f, size.y), stroke);
    }

    private static GameObject BuildMatchHudContent()
    {
        GameObject root = CreateRoot("SCN08_MatchHudContent");

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, MatchHudHeaderSize.x, MatchHudHeaderSize.y));
        BuildMatchHudHeader(header.transform);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, MatchHudHeaderSize.y, MatchHudSideSize.x, MatchHudSideSize.y));
        BuildMatchHudLeft(left.transform);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(4080f, MatchHudHeaderSize.y, MatchHudSideSize.x, MatchHudSideSize.y));
        BuildMatchHudRight(right.transform);

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 1920f, MatchHudFooterSize.x, MatchHudFooterSize.y));
        BuildMatchHudFooter(footer.transform);

        return root;
    }

    private static void BuildMatchHudHeader(Transform parent)
    {
        GameObject battlefield = CreateTopLeftMainMenuRect("BattlefieldLayer", parent, new Rect(0f, 0f, 4800f, 2700f));
        AddMatchHudSpriteCentered(battlefield.transform, "BattlefieldArt", "scn08_battlefield_21x9_no_ui.png", new Vector2(0f, 270f), new Vector2(6225f, 2700f), false);
        BuildMatchHudBattlefieldMarkers(battlefield.transform);

        AddStretchSolid(parent, "HeaderBackPlate", new Color(0.018f, 0.022f, 0.018f, 0.78f));

        GameObject banner = CreateTopLeftMainMenuRect("CurrentOrderBanner", parent, new Rect(1865f, 18f, 700f, 150f));
        AddMatchHudSpriteCentered(banner.transform, "Frame", "scn08_command_mode_banner_frame.png", Vector2.zero, new Vector2(700f, 150f), false);
        AddMatchHudSpriteCentered(banner.transform, "Chevrons", "scn08_current_order_chevrons.png", new Vector2(-242f, 0f), new Vector2(110f, 60f), true);
        AddTextCentered(banner.transform, "OrderText", "MOVE ORDER", new Vector2(40f, 24f), new Vector2(410f, 54f), 42f, TextAlignmentOptions.Left, Text);
        AddTextCentered(banner.transform, "SquadText", "Rifle Squad", new Vector2(40f, -34f), new Vector2(410f, 42f), 28f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));

        GameObject resources = CreateTopLeftMainMenuRect("ResourceStrip", parent, new Rect(2640f, 18f, 1608f, 150f));
        AddMatchHudSpriteCentered(resources.transform, "Frame", "scn08_resource_strip_frame.png", Vector2.zero, new Vector2(1608f, 150f), false);
        AddMatchHudResource(resources.transform, "Credits", "scn08_resource_credits_coin.png", "Credits", "187,540", -600f, Accent);
        AddMatchHudResource(resources.transform, "Fuel", "scn08_resource_fuel_can.png", "Fuel", "2,860", -210f, new Color32(178, 178, 118, 255));
        AddMatchHudResource(resources.transform, "Supply", "scn08_resource_supply_crate.png", "Supply", "92/120", 210f, new Color32(174, 181, 113, 255));
        AddMatchHudResource(resources.transform, "CivilianRisk", "scn08_icon_civilian_group.png", "Civilian Risk", "MED", 615f, Accent);

        GameObject menu = CreateTopRightMainMenuRect("MenuButton", parent, 24f, 18f, new Vector2(150f, 150f));
        AddMatchHudSpriteCentered(menu.transform, "Frame", "scn08_top_icon_button_frame_a.png", Vector2.zero, new Vector2(150f, 135f), false);
        AddMatchHudSpriteCentered(menu.transform, "Icon", "scn08_icon_menu_list.png", Vector2.zero, new Vector2(86f, 86f), true);

        GameObject pause = CreateTopRightMainMenuRect("PauseButton", parent, 186f, 18f, new Vector2(150f, 150f));
        AddMatchHudSpriteCentered(pause.transform, "Frame", "scn08_top_icon_button_frame_b.png", Vector2.zero, new Vector2(150f, 135f), false);
        AddMatchHudSpriteCentered(pause.transform, "Icon", "scn08_icon_pause.png", Vector2.zero, new Vector2(82f, 82f), true);

        GameObject settings = CreateTopRightMainMenuRect("SettingsButton", parent, 348f, 18f, new Vector2(150f, 150f));
        AddMatchHudSpriteCentered(settings.transform, "Frame", "scn08_top_icon_button_frame_b.png", Vector2.zero, new Vector2(150f, 135f), false);
        AddMatchHudSpriteCentered(settings.transform, "Icon", "scn08_icon_settings_gear.png", Vector2.zero, new Vector2(82f, 82f), true);
    }

    private static void AddMatchHudResource(Transform parent, string name, string icon, string label, string value, float x, Color valueColor)
    {
        GameObject slot = CreateCenteredRect($"{name}Slot", parent, new Vector2(x, 0f), new Vector2(350f, 130f));
        AddMatchHudSpriteCentered(slot.transform, "Icon", icon, new Vector2(-112f, 0f), new Vector2(92f, 92f), true);
        AddTextCentered(slot.transform, "Label", label, new Vector2(60f, 28f), new Vector2(210f, 44f), 28f, TextAlignmentOptions.Left, Text);
        AddTextCentered(slot.transform, "Value", value, new Vector2(60f, -28f), new Vector2(210f, 54f), 42f, TextAlignmentOptions.Left, valueColor);
    }

    private static void BuildMatchHudLeft(Transform parent)
    {
        GameObject objectives = CreateTopLeftMainMenuRect("ObjectivesPanel", parent, new Rect(16f, 18f, 670f, 520f));
        AddMatchHudSpriteCentered(objectives.transform, "Frame", "scn08_objective_panel_frame.png", Vector2.zero, new Vector2(670f, 520f), false);
        AddTextCentered(objectives.transform, "Title", "OBJECTIVES", new Vector2(-80f, 204f), new Vector2(450f, 56f), 38f, TextAlignmentOptions.Left, Text);
        AddObjectiveRow(objectives.transform, "NeutralizeHostiles", "Neutralize hostile patrol", "scn08_icon_checkbox_empty.png", 96f, MutedText);
        AddObjectiveRow(objectives.transform, "ProtectCivilians", "Protect civilians", "scn08_icon_checkbox_checked.png", -6f, Text);
        AddObjectiveRow(objectives.transform, "KeepLossesLow", "Keep losses low", "scn08_icon_objective_star.png", -108f, Text);
        AddTextCentered(objectives.transform, "Elapsed", "Elapsed: 07:42", new Vector2(-80f, -206f), new Vector2(460f, 50f), 28f, TextAlignmentOptions.Left, MutedText);

        GameObject squad = CreateTopLeftMainMenuRect("SelectedSquadPanel", parent, new Rect(16f, 570f, 690f, 1000f));
        AddMatchHudSpriteCentered(squad.transform, "Frame", "scn08_selected_entity_panel_frame.png", Vector2.zero, new Vector2(690f, 1000f), false);
        AddMatchHudSpriteCentered(squad.transform, "Badge", "scn08_icon_shield_rank_badge.png", new Vector2(-270f, 396f), new Vector2(100f, 132f), true);
        AddTextCentered(squad.transform, "Title", "RIFLE SQUAD", new Vector2(60f, 414f), new Vector2(430f, 60f), 42f, TextAlignmentOptions.Left, Text);
        AddTextCentered(squad.transform, "Subtitle", "Squad  |  Anti-Infantry", new Vector2(60f, 356f), new Vector2(430f, 44f), 26f, TextAlignmentOptions.Left, MutedText);
        AddMatchHudSpriteCentered(squad.transform, "PortraitFrame", "scn08_selected_entity_portrait_frame.png", new Vector2(0f, 144f), new Vector2(600f, 360f), false);
        AddMatchHudSpriteCentered(squad.transform, "Portrait", "scn08_portrait_rifle_squad.png", new Vector2(0f, 144f), new Vector2(560f, 300f), true);
        AddSolidCentered(squad.transform, "HealthFill", new Vector2(-64f, -84f), new Vector2(400f, 30f), new Color32(154, 190, 82, 255));
        AddMatchHudSpriteCentered(squad.transform, "HealthFrame", "scn08_health_bar_frame.png", new Vector2(-64f, -84f), new Vector2(430f, 52f), false);
        AddTextCentered(squad.transform, "HealthText", "120 / 120", new Vector2(214f, -84f), new Vector2(160f, 42f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(squad.transform, "OrderLabel", "ORDER", new Vector2(-250f, -178f), new Vector2(180f, 40f), 28f, TextAlignmentOptions.Left, MutedText);
        AddTextCentered(squad.transform, "OrderValue", "Moving", new Vector2(-214f, -228f), new Vector2(260f, 46f), 32f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));
        AddAbilityChip(squad.transform, "Scan", "scn08_command_scan_radar.png", "SCAN", -212f);
        AddAbilityChip(squad.transform, "Hold", "scn08_command_hold_shield.png", "HOLD", 0f);
        AddAbilityChip(squad.transform, "Board", "scn08_command_board_vehicle.png", "BOARD", 212f);
    }

    private static void AddObjectiveRow(Transform parent, string name, string text, string icon, float y, Color color)
    {
        GameObject row = CreateCenteredRect(name, parent, new Vector2(0f, y), new Vector2(600f, 82f));
        AddMatchHudSpriteCentered(row.transform, "Icon", icon, new Vector2(-258f, 0f), new Vector2(54f, 54f), true);
        AddTextCentered(row.transform, "Text", text, new Vector2(40f, 0f), new Vector2(480f, 52f), 29f, TextAlignmentOptions.Left, color);
    }

    private static void AddAbilityChip(Transform parent, string name, string icon, string label, float x)
    {
        GameObject chip = CreateCenteredRect($"{name}Chip", parent, new Vector2(x, -374f), new Vector2(154f, 190f));
        AddMatchHudSpriteCentered(chip.transform, "Frame", "scn08_ability_chip_frame.png", Vector2.zero, new Vector2(154f, 150f), false);
        AddMatchHudSpriteCentered(chip.transform, "Icon", icon, new Vector2(0f, 18f), new Vector2(82f, 82f), true);
        AddTextCentered(chip.transform, "Label", label, new Vector2(0f, -62f), new Vector2(130f, 34f), 25f, TextAlignmentOptions.Center, Text);
        AddMatchHudSpriteCentered(chip.transform, "Status", "scn08_status_segment_strip.png", new Vector2(0f, -104f), new Vector2(112f, 26f), false);
    }

    private static void BuildMatchHudRight(Transform parent)
    {
        GameObject threat = CreateTopLeftMainMenuRect("ThreatJumpPanel", parent, new Rect(-825f, 42f, 800f, 165f));
        AddMatchHudSpriteCentered(threat.transform, "Frame", "scn08_rule_toast_chip_frame.png", Vector2.zero, new Vector2(800f, 165f), false);
        AddMatchHudSpriteCentered(threat.transform, "WarningIcon", "scn08_icon_threat_warning.png", new Vector2(-316f, 0f), new Vector2(82f, 82f), true);
        AddTextCentered(threat.transform, "Title", "HOSTILE CELL SPOTTED", new Vector2(-48f, 28f), new Vector2(430f, 44f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(threat.transform, "Subtitle", "Market quarter, 140m", new Vector2(-48f, -26f), new Vector2(430f, 38f), 25f, TextAlignmentOptions.Left, MutedText);
        AddMatchHudSpriteCentered(threat.transform, "JumpFrame", "scn08_jump_button_frame.png", new Vector2(285f, 0f), new Vector2(150f, 94f), false);
        AddMatchHudSpriteCentered(threat.transform, "JumpIcon", "scn08_icon_jump_arrow.png", new Vector2(285f, 0f), new Vector2(68f, 68f), true);

        GameObject quick = CreateTopLeftMainMenuRect("RightQuickRail", parent, new Rect(514f, 205f, 170f, 770f));
        AddMatchHudSpriteCentered(quick.transform, "Frame", "scn08_right_quick_panel_frame.png", Vector2.zero, new Vector2(170f, 770f), false);
        AddQuickAction(quick.transform, "Build", "scn08_icon_build_tools.png", 246f);
        AddQuickAction(quick.transform, "Scan", "scn08_command_scan_radar.png", 82f);
        AddQuickAction(quick.transform, "Support", "scn08_icon_support_parachute.png", -82f);
        AddQuickAction(quick.transform, "Settings", "scn08_icon_settings_gear.png", -246f);

        GameObject minimap = CreateTopLeftMainMenuRect("MinimapPanel", parent, new Rect(-218f, 1010f, 900f, 610f));
        AddMatchHudSpriteCentered(minimap.transform, "Frame", "scn08_minimap_panel_frame.png", Vector2.zero, new Vector2(900f, 610f), false);
        AddMatchHudSpriteCentered(minimap.transform, "Map", "scn08_minimap_content.png", new Vector2(0f, 18f), new Vector2(760f, 440f), false);
        AddMatchHudSpriteCentered(minimap.transform, "Viewport", "scn08_marker_minimap_viewport_rect.png", new Vector2(52f, 20f), new Vector2(250f, 154f), false);
        AddMatchHudSpriteCentered(minimap.transform, "North", "scn08_minimap_north_arrow.png", new Vector2(-356f, 246f), new Vector2(64f, 64f), true);
        AddMatchHudSpriteCentered(minimap.transform, "FriendlyA", "scn08_marker_friendly_minimap_dot.png", new Vector2(-120f, 34f), new Vector2(34f, 34f), true);
        AddMatchHudSpriteCentered(minimap.transform, "FriendlyB", "scn08_marker_friendly_minimap_dot.png", new Vector2(38f, -80f), new Vector2(34f, 34f), true);
        AddMatchHudSpriteCentered(minimap.transform, "HostileA", "scn08_marker_hostile_minimap_dot.png", new Vector2(184f, 114f), new Vector2(38f, 38f), true);
        AddMatchHudSpriteCentered(minimap.transform, "Civilian", "scn08_marker_civilian_minimap_dot.png", new Vector2(-230f, -110f), new Vector2(32f, 32f), true);
        AddMinimapButton(minimap.transform, "ZoomIn", "scn08_minimap_zoom_plus_icon.png", 300f, -222f);
        AddMinimapButton(minimap.transform, "ZoomOut", "scn08_minimap_zoom_minus_icon.png", 388f, -222f);
        AddMatchHudSpriteCentered(minimap.transform, "Focus", "scn08_minimap_focus_target_icon.png", new Vector2(-350f, -226f), new Vector2(64f, 64f), true);
    }

    private static void BuildMatchHudFooter(Transform parent)
    {
        GameObject tray = CreateTopLeftMainMenuRect("SquadTray", parent, new Rect(24f, -58f, 1480f, 276f));
        AddMatchHudSpriteCentered(tray.transform, "Frame", "scn08_squad_tray_frame.png", Vector2.zero, new Vector2(1480f, 276f), false);
        AddSquadCard(tray.transform, "RifleSquad", "scn08_portrait_rifle_squad.png", "1", true, -520f);
        AddSquadCard(tray.transform, "BombSuit", "scn08_portrait_bomb_suit.png", "2", false, -172f);
        AddSquadCard(tray.transform, "FastApc", "scn08_portrait_fast_apc.png", "3", false, 176f);
        AddSquadCard(tray.transform, "ReconDrone", "scn08_portrait_recon_drone.png", "4", false, 524f);

        GameObject rail = CreateTopLeftMainMenuRect("CommandRail", parent, new Rect(1670f, -34f, 1820f, 244f));
        AddMatchHudSpriteCentered(rail.transform, "Frame", "scn08_command_bar_rail_frame.png", Vector2.zero, new Vector2(1820f, 244f), false);
        AddCommandButton(rail.transform, "Select", "scn08_command_select_cursor.png", "SELECT", -700f, false);
        AddCommandButton(rail.transform, "Move", "scn08_command_move_chevrons.png", "MOVE", -500f, true);
        AddCommandButton(rail.transform, "Attack", "scn08_command_attack_crosshair.png", "ATTACK", -300f, false);
        AddCommandButton(rail.transform, "Hold", "scn08_command_hold_shield.png", "HOLD", -100f, false);
        AddCommandButton(rail.transform, "Stop", "scn08_command_stop_hand.png", "STOP", 100f, false);
        AddCommandButton(rail.transform, "Build", "scn08_icon_build_tools.png", "BUILD", 300f, false);
        AddCommandButton(rail.transform, "Scan", "scn08_command_scan_radar.png", "SCAN", 500f, false);
        AddCommandButton(rail.transform, "Support", "scn08_icon_support_parachute.png", "SUPPORT", 700f, false);
    }

    private static void BuildMatchHudBattlefieldMarkers(Transform parent)
    {
        AddMatchHudSpriteCentered(parent, "SelectedSquadRing", "scn08_marker_selection_ring.png", new Vector2(-210f, -500f), new Vector2(540f, 170f), true);
        AddMatchHudSpriteCentered(parent, "MoveDestination", "scn08_marker_move_destination.png", new Vector2(640f, -315f), new Vector2(180f, 180f), true);
        AddMatchHudSpriteCentered(parent, "MovePath", "scn08_marker_path_line.png", new Vector2(250f, -408f), new Vector2(900f, 62f), false);
        AddMatchHudSpriteCentered(parent, "ObjectivePin", "scn08_marker_objective_star_pin.png", new Vector2(-920f, 330f), new Vector2(150f, 178f), true);
        AddMatchHudSpriteCentered(parent, "HostileMarkerA", "scn08_marker_hostile_diamond.png", new Vector2(860f, 168f), new Vector2(118f, 118f), true);
        AddMatchHudSpriteCentered(parent, "HostileMarkerB", "scn08_marker_hostile_diamond.png", new Vector2(1120f, -36f), new Vector2(104f, 104f), true);
        AddMatchHudSpriteCentered(parent, "ThreatPin", "scn08_marker_threat_warning_pin.png", new Vector2(1320f, 304f), new Vector2(136f, 162f), true);
        AddMatchHudSpriteCentered(parent, "CivilianRiskZone", "scn08_marker_civilian_risk_zone.png", new Vector2(-1180f, -190f), new Vector2(560f, 360f), false);
        AddMatchHudSpriteCentered(parent, "CommandFocus", "scn08_marker_command_focus_brackets.png", new Vector2(-210f, -500f), new Vector2(720f, 260f), true);
    }

    private static void AddQuickAction(Transform parent, string name, string icon, float y)
    {
        GameObject button = CreateCenteredRect($"{name}Button", parent, new Vector2(0f, y), new Vector2(120f, 120f));
        AddMatchHudSpriteCentered(button.transform, "Frame", "scn08_side_quick_button_frame.png", Vector2.zero, new Vector2(120f, 120f), false);
        AddMatchHudSpriteCentered(button.transform, "Icon", icon, Vector2.zero, new Vector2(70f, 70f), true);
    }

    private static void AddMinimapButton(Transform parent, string name, string icon, float x, float y)
    {
        GameObject button = CreateCenteredRect(name, parent, new Vector2(x, y), new Vector2(72f, 72f));
        AddMatchHudSpriteCentered(button.transform, "Frame", "scn08_minimap_zoom_button_frame.png", Vector2.zero, new Vector2(72f, 72f), false);
        AddMatchHudSpriteCentered(button.transform, "Icon", icon, Vector2.zero, new Vector2(42f, 42f), true);
    }

    private static void AddSquadCard(Transform parent, string name, string portrait, string number, bool selected, float x)
    {
        GameObject card = CreateCenteredRect($"{name}Card", parent, new Vector2(x, 4f), new Vector2(300f, 252f));
        AddMatchHudSpriteCentered(card.transform, "Frame", selected ? "scn08_squad_card_selected_frame.png" : "scn08_squad_card_normal_frame.png", Vector2.zero, new Vector2(220f, 252f), false);
        AddMatchHudSpriteCentered(card.transform, "Portrait", portrait, new Vector2(0f, 18f), new Vector2(178f, 156f), true);
        AddMatchHudSpriteCentered(card.transform, "NumberBadge", "scn08_squad_number_badge_frame.png", new Vector2(-82f, 92f), new Vector2(62f, 62f), false);
        AddTextCentered(card.transform, "Number", number, new Vector2(-82f, 92f), new Vector2(54f, 46f), 28f, TextAlignmentOptions.Center, Text);
        AddSolidCentered(card.transform, "HealthFill", new Vector2(0f, -92f), new Vector2(150f, 18f), selected ? new Color32(154, 190, 82, 255) : new Color32(116, 132, 78, 255));
        AddMatchHudSpriteCentered(card.transform, "HealthFrame", "scn08_health_bar_small_frame.png", new Vector2(0f, -92f), new Vector2(168f, 32f), false);
    }

    private static void AddCommandButton(Transform parent, string name, string icon, string label, float x, bool selected)
    {
        GameObject button = CreateCenteredRect($"{name}Command", parent, new Vector2(x, 4f), new Vector2(160f, 206f));
        AddMatchHudSpriteCentered(button.transform, "Frame", selected ? "scn08_command_button_selected_frame.png" : "scn08_command_button_normal_frame.png", Vector2.zero, new Vector2(150f, 182f), false);
        AddMatchHudSpriteCentered(button.transform, "Icon", icon, new Vector2(0f, 28f), new Vector2(82f, 82f), true);
        AddTextCentered(button.transform, "Label", label, new Vector2(0f, -66f), new Vector2(140f, 40f), 24f, TextAlignmentOptions.Center, Text);
    }

    private static GameObject BuildBuildDrawerPopup()
    {
        GameObject root = CreateRoot("SCN09_BuildDrawerPopup");
        GameObject overlay = CreateCenteredRect("BuildDrawerRoot", root.transform, Vector2.zero, MainMenuBackgroundSize);
        AddStretchSolid(overlay.transform, "MatchHudFocusShade", new Color(0f, 0f, 0f, 0.08f));

        GameObject drawer = CreateTopLeftMainMenuRect("DrawerFrame", overlay.transform, new Rect(1940f, 250f, 2460f, 1510f));
        AddBuildDrawerSpriteCentered(drawer.transform, "OuterFrame", "chrome_01_drawer_outer_frame.png", Vector2.zero, new Vector2(2460f, 1510f), false);
        AddTextCentered(drawer.transform, "Title", "BUILD", new Vector2(-1020f, 636f), new Vector2(360f, 66f), 54f, TextAlignmentOptions.Left, Text);
        AddTextCentered(drawer.transform, "Subtitle", "Production and field placement", new Vector2(-790f, 582f), new Vector2(720f, 44f), 29f, TextAlignmentOptions.Left, MutedText);
        AddBuildDrawerSpriteCentered(drawer.transform, "BuildIcon", "icon_06_icon_build_tools.png", new Vector2(-1138f, 608f), new Vector2(90f, 90f), true);
        AddBuildDrawerCloseButton(drawer.transform);

        AddBuildDrawerTab(drawer.transform, "BuildingsTab", "BUILDINGS", "icon_06_icon_build_tools.png", -820f, 482f, true);
        AddBuildDrawerTab(drawer.transform, "VehiclesTab", "VEHICLES", "icon_12_icon_vehicle_depot.png", -250f, 482f, false);
        AddBuildDrawerTab(drawer.transform, "SoldiersTab", "SOLDIERS", "icon_11_icon_squad_group.png", 320f, 482f, false);

        GameObject cardGrid = CreateCenteredRect("CardGrid", drawer.transform, new Vector2(-510f, -120f), new Vector2(1480f, 980f));
        AddBuildDrawerCard(cardGrid.transform, "BarracksCard", "thumb_01_building_barracks_thumb.png", "BARRACKS", "Infantry production", "450", "120", "00:45", -555f, 250f, true, false);
        AddBuildDrawerCard(cardGrid.transform, "VehicleDepotCard", "thumb_02_building_vehicle_depot_thumb.png", "VEHICLE DEPOT", "Light armor bay", "820", "260", "01:30", -185f, 250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "WatchtowerCard", "thumb_03_building_watchtower_thumb.png", "WATCHTOWER", "Vision and defense", "320", "90", "00:35", 185f, 250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "SupplyDepotCard", "thumb_04_building_supply_depot_thumb.png", "SUPPLY DEPOT", "Storage capacity", "380", "220", "00:50", 555f, 250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "OilPumpCard", "thumb_05_building_oil_pump_thumb.png", "OIL PUMP", "Extracts oil", "520", "160", "01:05", -555f, -250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "OilRefineryCard", "thumb_06_building_oil_refinery_thumb.png", "OIL REFINERY", "Turns oil to fuel", "2,000", "0", "00:30", -185f, -250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "FuelBladderCard", "thumb_07_building_fuel_bladder_thumb.png", "FUEL BLADDER", "Fuel storage", "1,000", "0", "00:15", 185f, -250f, false, false);
        AddBuildDrawerCard(cardGrid.transform, "RadarCard", "thumb_08_building_advanced_radar_thumb.png", "ADV. RADAR", "Needs command lvl 12", "960", "340", "02:00", 555f, -250f, false, true);

        GameObject detail = CreateCenteredRect("DetailPanel", drawer.transform, new Vector2(870f, -80f), new Vector2(660f, 1120f));
        AddBuildDrawerSpriteCentered(detail.transform, "Frame", "chrome_02_detail_panel_frame.png", Vector2.zero, new Vector2(660f, 1120f), false);
        AddBuildDrawerSpriteCentered(detail.transform, "Preview", "thumb_06_building_oil_refinery_thumb.png", new Vector2(0f, 322f), new Vector2(520f, 270f), false);
        AddTextCentered(detail.transform, "Name", "OIL REFINERY", new Vector2(-16f, 114f), new Vector2(520f, 58f), 40f, TextAlignmentOptions.Left, Text);
        AddTextCentered(detail.transform, "Role", "RESOURCE STRUCTURE", new Vector2(-16f, 58f), new Vector2(520f, 40f), 26f, TextAlignmentOptions.Left, Accent);
        AddTextCentered(detail.transform, "Description", "Turns Oil into Fuel and feeds vehicle production during extended field operations.", new Vector2(0f, -42f), new Vector2(520f, 118f), 26f, TextAlignmentOptions.TopLeft, Text);
        AddBuildDrawerCostRow(detail.transform, "CreditsCost", "icon_01_icon_credits.png", "Credits", "2,000", -190f);
        AddBuildDrawerCostRow(detail.transform, "SuppliesCost", "icon_02_icon_supplies.png", "Supplies", "0", -290f);
        AddBuildDrawerCostRow(detail.transform, "TimeCost", "icon_05_icon_time.png", "Build Time", "00:30", -390f);
        AddBuildDrawerSpriteCentered(detail.transform, "Divider", "chrome_11_thin_divider.png", new Vector2(0f, -468f), new Vector2(560f, 22f), false);
        AddBuildDrawerSpriteCentered(detail.transform, "PlacementIcon", "icon_17_icon_placement_corners.png", new Vector2(-230f, -540f), new Vector2(58f, 58f), true);
        AddTextCentered(detail.transform, "Placement", "PLACE ON VALID GROUND", new Vector2(42f, -540f), new Vector2(430f, 42f), 25f, TextAlignmentOptions.Left, Blue);
        AddBuildDrawerButton(detail.transform, "BuildButton", "BUILD", new Vector2(-150f, -650f), new Vector2(300f, 104f), true);
        AddBuildDrawerButton(detail.transform, "QueueButton", "QUEUE", new Vector2(190f, -650f), new Vector2(260f, 104f), false);

        GameObject strip = CreateTopLeftMainMenuRect("InstructionStrip", overlay.transform, new Rect(2120f, 1808f, 2120f, 142f));
        AddBuildDrawerSpriteCentered(strip.transform, "Frame", "chrome_14_bottom_instruction_strip.png", Vector2.zero, new Vector2(2120f, 142f), false);
        AddBuildDrawerSpriteCentered(strip.transform, "CursorIcon", "icon_18_icon_placement_cursor.png", new Vector2(-920f, 0f), new Vector2(60f, 60f), true);
        AddTextCentered(strip.transform, "Instruction", "Select a building, choose a valid footprint, then confirm placement.", new Vector2(-130f, 0f), new Vector2(1580f, 52f), 30f, TextAlignmentOptions.Left, Text);
        AddBuildDrawerSpriteCentered(strip.transform, "ConfirmIcon", "icon_08_icon_confirm.png", new Vector2(910f, 0f), new Vector2(58f, 58f), true);

        return root;
    }

    private static void AddBuildDrawerTab(Transform parent, string name, string label, string icon, float x, float y, bool selected)
    {
        GameObject tab = CreateCenteredRect(name, parent, new Vector2(x, y), new Vector2(520f, 112f));
        AddBuildDrawerSpriteCentered(tab.transform, "Frame", selected ? "chrome_04_tab_selected_bg.png" : "chrome_05_tab_idle_bg.png", Vector2.zero, new Vector2(520f, 112f), false);
        AddBuildDrawerSpriteCentered(tab.transform, "Icon", icon, new Vector2(-174f, 0f), new Vector2(56f, 56f), true);
        AddTextCentered(tab.transform, "Label", label, new Vector2(62f, 0f), new Vector2(300f, 44f), 30f, TextAlignmentOptions.Center, selected ? Text : MutedText);
    }

    private static void AddBuildDrawerCard(Transform parent, string name, string thumb, string title, string role, string credits, string supplies, string time, float x, float y, bool selected, bool locked)
    {
        GameObject card = CreateCenteredRect(name, parent, new Vector2(x, y), new Vector2(330f, 430f));
        AddSolidCentered(card.transform, "BackPlate", Vector2.zero, new Vector2(304f, 398f), new Color(0.018f, 0.024f, 0.021f, 0.86f));
        AddBuildDrawerSpriteCentered(card.transform, "Frame", locked ? "chrome_09_card_frame_tall.png" : "chrome_08_card_frame_standard.png", Vector2.zero, new Vector2(330f, 430f), false);
        if (selected)
            AddBuildDrawerSpriteCentered(card.transform, "SelectedHighlight", "chrome_07_selected_card_highlight_frame.png", Vector2.zero, new Vector2(340f, 440f), false);
        AddBuildDrawerSpriteCentered(card.transform, "Thumb", thumb, new Vector2(0f, 76f), new Vector2(266f, 168f), false);
        AddTextCentered(card.transform, "Title", title, new Vector2(0f, 178f), new Vector2(280f, 38f), 22f, TextAlignmentOptions.Center, Text);
        AddTextCentered(card.transform, "Role", role, new Vector2(-2f, -40f), new Vector2(260f, 34f), 18f, TextAlignmentOptions.Left, MutedText);
        AddBuildDrawerTinyCost(card.transform, "Credits", "icon_01_icon_credits.png", credits, -78f, -102f);
        AddBuildDrawerTinyCost(card.transform, "Supplies", "icon_02_icon_supplies.png", supplies, 68f, -102f);
        AddBuildDrawerTinyCost(card.transform, "Time", "icon_05_icon_time.png", time, -70f, -158f);
        if (locked)
        {
            AddBuildDrawerSpriteCentered(card.transform, "LockedOverlay", "chrome_10_selected_card_body_overlay.png", Vector2.zero, new Vector2(306f, 400f), false);
            AddBuildDrawerSpriteCentered(card.transform, "Lock", "icon_09_icon_lock.png", new Vector2(114f, -158f), new Vector2(42f, 42f), true);
        }
        else
        {
            AddBuildDrawerSpriteCentered(card.transform, "Add", "icon_15_icon_plus.png", new Vector2(114f, -158f), new Vector2(42f, 42f), true);
        }
    }

    private static void AddBuildDrawerTinyCost(Transform parent, string name, string icon, string value, float x, float y)
    {
        GameObject row = CreateCenteredRect($"{name}TinyCost", parent, new Vector2(x, y), new Vector2(150f, 42f));
        AddBuildDrawerSpriteCentered(row.transform, "Icon", icon, new Vector2(-54f, 0f), new Vector2(34f, 34f), true);
        AddTextCentered(row.transform, "Value", value, new Vector2(28f, 0f), new Vector2(86f, 32f), 21f, TextAlignmentOptions.Left, Accent);
    }

    private static void AddBuildDrawerCostRow(Transform parent, string name, string icon, string label, string value, float y)
    {
        GameObject row = CreateCenteredRect(name, parent, new Vector2(0f, y), new Vector2(560f, 78f));
        AddBuildDrawerSpriteCentered(row.transform, "Frame", "chrome_13_small_status_chip_bg.png", Vector2.zero, new Vector2(560f, 70f), false);
        AddBuildDrawerSpriteCentered(row.transform, "Icon", icon, new Vector2(-222f, 0f), new Vector2(48f, 48f), true);
        AddTextCentered(row.transform, "Label", label, new Vector2(-44f, 0f), new Vector2(260f, 36f), 25f, TextAlignmentOptions.Left, Text);
        AddTextCentered(row.transform, "Value", value, new Vector2(210f, 0f), new Vector2(160f, 36f), 27f, TextAlignmentOptions.Right, Accent);
    }

    private static void AddBuildDrawerButton(Transform parent, string name, string label, Vector2 position, Vector2 size, bool primary)
    {
        GameObject button = CreateCenteredRect(name, parent, position, size);
        AddBuildDrawerSpriteCentered(button.transform, "Frame", primary ? "chrome_06_gold_action_button_bg.png" : "chrome_12_secondary_button_bg.png", Vector2.zero, size, false);
        AddTextCentered(button.transform, "Label", label, Vector2.zero, new Vector2(size.x - 42f, 44f), 30f, TextAlignmentOptions.Center, primary ? Color.black : Text);
        AddRouteButtonHotspot(button.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Match);
    }

    private static void AddBuildDrawerQueueRow(Transform parent, string name, string label, string time, float y)
    {
        GameObject row = CreateCenteredRect(name, parent, new Vector2(0f, y), new Vector2(300f, 104f));
        AddBuildDrawerSpriteCentered(row.transform, "Frame", "chrome_13_small_status_chip_bg.png", Vector2.zero, new Vector2(300f, 88f), false);
        AddTextCentered(row.transform, "Label", label, new Vector2(-20f, 16f), new Vector2(210f, 30f), 22f, TextAlignmentOptions.Left, Text);
        AddTextCentered(row.transform, "Time", time, new Vector2(26f, -22f), new Vector2(190f, 28f), 21f, TextAlignmentOptions.Left, Accent);
    }

    private static void AddBuildDrawerCloseButton(Transform parent)
    {
        GameObject close = CreateCenteredRect("CloseButton", parent, new Vector2(1128f, 628f), new Vector2(104f, 104f));
        AddBuildDrawerSpriteCentered(close.transform, "Frame", "chrome_12_secondary_button_bg.png", Vector2.zero, new Vector2(104f, 104f), false);
        AddBuildDrawerSpriteCentered(close.transform, "Icon", "icon_20_icon_close.png", Vector2.zero, new Vector2(56f, 56f), true);
        AddRouteButtonHotspot(close.transform, "Hotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.Match);
    }

    private static GameObject BuildResultPopup()
    {
        GameObject root = CreateRoot("POP05_MissionResultPopup");
        AddMissionResultBackdrop(root.transform);

        GameObject frame = CreateCenteredRect("PopupFrame", root.transform, Vector2.zero, new Vector2(3600f, 1960f));
        BuildMissionResultHeader(frame.transform);
        BuildMissionResultSummary(frame.transform);
        BuildMissionResultRating(frame.transform);
        BuildMissionResultStats(frame.transform);
        BuildMissionResultRewards(frame.transform);
        BuildMissionResultConsequences(frame.transform);
        BuildMissionResultActions(frame.transform);
        return root;
    }

    private static void AddMissionResultBackdrop(Transform parent)
    {
        AddStretchSolid(parent, "Dimmer", new Color(0f, 0f, 0f, 0.28f));
        GameObject viewport = CreateCenteredRect("MissionResultBackgroundViewport", parent, Vector2.zero, MainMenuBackgroundSize);
        viewport.AddComponent<RectMask2D>();
        AddMissionResultSpriteCentered(viewport.transform, "BackgroundArt", "pop05_background_21x9_no_ui.png", Vector2.zero, MainMenuBackgroundCoverSize, false);
    }

    private static void BuildMissionResultHeader(Transform parent)
    {
        GameObject header = CreateTopLeftMainMenuRect("ResultHeader", parent, new Rect(0f, 0f, 3600f, 330f));
        AddMissionResultSpriteCentered(header.transform, "Frame", "pop05_result_header_frame.png", Vector2.zero, new Vector2(3600f, 310f), false);
        AddMissionResultSpriteCentered(header.transform, "Logo", "pop05_commander_logo.png", new Vector2(-1560f, 28f), new Vector2(150f, 170f), true);
        AddMissionResultSpriteCentered(header.transform, "WingLeft", "pop05_victory_wing_left.png", new Vector2(-330f, 70f), new Vector2(210f, 124f), true);
        AddMissionResultSpriteCentered(header.transform, "WingRight", "pop05_victory_wing_right.png", new Vector2(330f, 70f), new Vector2(210f, 124f), true);
        AddTextCentered(header.transform, "OutcomeText", "VICTORY", new Vector2(0f, 78f), new Vector2(560f, 106f), 92f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(header.transform, "MissionTitleText", "FIRST CONTACT COMPLETE", new Vector2(0f, -16f), new Vector2(820f, 56f), 38f, TextAlignmentOptions.Center, Text);

        GameObject xp = CreateCenteredRect("CommanderXp", header.transform, new Vector2(1220f, 52f), new Vector2(620f, 150f));
        AddMissionResultSpriteCentered(xp.transform, "Icon", "pop05_reward_commander_xp_shield.png", new Vector2(-248f, 4f), new Vector2(96f, 124f), true);
        AddTextCentered(xp.transform, "Level", "Commander Level 38", new Vector2(72f, 42f), new Vector2(430f, 42f), 28f, TextAlignmentOptions.Left, Text);
        AddMissionResultProgressBar(xp.transform, "Progress", new Vector2(90f, 0f), new Vector2(430f, 30f), 280f);
        AddTextCentered(xp.transform, "Xp", "12,450 / 18,000 XP", new Vector2(122f, -44f), new Vector2(420f, 40f), 28f, TextAlignmentOptions.Right, Accent);

        GameObject metadata = CreateCenteredRect("MissionMetadataStrip", header.transform, new Vector2(0f, -128f), new Vector2(1480f, 70f));
        AddMissionResultSpriteCentered(metadata.transform, "Frame", "pop05_mission_metadata_strip_frame.png", Vector2.zero, new Vector2(1480f, 70f), false);
        AddTextCentered(metadata.transform, "Campaign", "Campaign", new Vector2(-430f, 0f), new Vector2(190f, 42f), 28f, TextAlignmentOptions.Center, MutedText);
        AddTextCentered(metadata.transform, "SlashA", "/", new Vector2(-250f, 0f), new Vector2(50f, 42f), 34f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(metadata.transform, "Chapter", "Chapter 01", new Vector2(-80f, 0f), new Vector2(220f, 42f), 28f, TextAlignmentOptions.Center, Text);
        AddTextCentered(metadata.transform, "SlashB", "/", new Vector2(110f, 0f), new Vector2(50f, 42f), 34f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(metadata.transform, "Response", "First Response", new Vector2(330f, 0f), new Vector2(300f, 42f), 28f, TextAlignmentOptions.Center, Text);
        AddTextCentered(metadata.transform, "SlashC", "/", new Vector2(580f, 0f), new Vector2(50f, 42f), 34f, TextAlignmentOptions.Center, Accent);
        AddTextCentered(metadata.transform, "Duration", "Duration  07:42", new Vector2(770f, 0f), new Vector2(260f, 42f), 28f, TextAlignmentOptions.Center, Text);
    }

    private static void BuildMissionResultSummary(Transform parent)
    {
        GameObject summary = CreateTopLeftMainMenuRect("MissionSummaryPanel", parent, new Rect(0f, 360f, 760f, 1180f));
        AddMissionResultSpriteCentered(summary.transform, "Frame", "pop05_mission_summary_panel_frame.png", Vector2.zero, new Vector2(760f, 1180f), false);
        AddMissionResultSpriteCentered(summary.transform, "Icon", "pop05_mission_summary_star_outline.png", new Vector2(-314f, 506f), new Vector2(54f, 54f), true);
        AddTextCentered(summary.transform, "Title", "MISSION SUMMARY", new Vector2(18f, 506f), new Vector2(460f, 56f), 38f, TextAlignmentOptions.Left, Accent);
        AddTextCentered(summary.transform, "MissionName", "FIRST RESPONSE", new Vector2(-132f, 396f), new Vector2(500f, 58f), 34f, TextAlignmentOptions.Left, Text);

        GameObject snapshot = CreateCenteredRect("MissionSnapshot", summary.transform, new Vector2(0f, 78f), new Vector2(640f, 560f));
        AddSolidCentered(snapshot.transform, "Back", Vector2.zero, new Vector2(604f, 500f), new Color(0.02f, 0.024f, 0.021f, 0.92f));
        AddMissionResultSpriteCentered(snapshot.transform, "Frame", "pop05_mission_snapshot_frame.png", Vector2.zero, new Vector2(640f, 560f), false);
        AddMissionResultSpriteCentered(snapshot.transform, "Art", "pop05_mission_snapshot_art.png", Vector2.zero, new Vector2(560f, 448f), true);

        AddTextCentered(summary.transform, "Description", "Our first contact has been successful. The hostile patrol was neutralized and civilians were evacuated to safety.", new Vector2(0f, -372f), new Vector2(620f, 210f), 30f, TextAlignmentOptions.TopLeft, Text);
    }

    private static void BuildMissionResultRating(Transform parent)
    {
        GameObject rating = CreateTopLeftMainMenuRect("MissionRatingPanel", parent, new Rect(800f, 360f, 1280f, 840f));
        AddMissionResultSpriteCentered(rating.transform, "Frame", "pop05_rating_objectives_panel_frame.png", Vector2.zero, new Vector2(1280f, 840f), false);
        AddTextCentered(rating.transform, "Title", "MISSION RATING", new Vector2(-300f, 344f), new Vector2(520f, 58f), 38f, TextAlignmentOptions.Left, Accent);

        AddMissionResultStar(rating.transform, "ObjectiveStar", "OBJECTIVE\nCOMPLETE", -390f);
        AddMissionResultStar(rating.transform, "CiviliansStar", "CIVILIANS\nPROTECTED", 0f);
        AddMissionResultStar(rating.transform, "LossesStar", "LOSSES\nLOW", 390f);

        AddTextCentered(rating.transform, "ObjectivesTitle", "OBJECTIVES", new Vector2(-496f, -76f), new Vector2(420f, 54f), 34f, TextAlignmentOptions.Left, Accent);
        AddMissionResultObjectiveRow(rating.transform, "ObjectiveNeutralize", "Neutralize hostile patrol", "COMPLETE", 30f);
        AddMissionResultObjectiveRow(rating.transform, "ObjectiveProtect", "Protect civilians", "COMPLETE", -54f);
        AddMissionResultObjectiveRow(rating.transform, "ObjectiveLosses", "Keep losses low", "COMPLETE", -138f);
    }

    private static void BuildMissionResultStats(Transform parent)
    {
        GameObject stats = CreateTopLeftMainMenuRect("PerformanceStatsPanel", parent, new Rect(800f, 1218f, 1280f, 322f));
        AddMissionResultSpriteCentered(stats.transform, "Frame", "pop05_performance_stats_panel_frame.png", Vector2.zero, new Vector2(1280f, 322f), false);
        AddTextCentered(stats.transform, "Title", "PERFORMANCE STATS", new Vector2(-430f, 106f), new Vector2(600f, 50f), 32f, TextAlignmentOptions.Left, Accent);
        AddMissionResultStat(stats.transform, "EnemiesDefeated", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES\nDEFEATED", "12", -450f);
        AddMissionResultStat(stats.transform, "UnitsLost", "pop05_stat_units_lost_shield.png", "UNITS\nLOST", "0", -150f);
        AddMissionResultStat(stats.transform, "CiviliansSaved", "pop05_consequence_civilian_group.png", "CIVILIANS\nSAVED", "18", 150f);
        AddMissionResultStat(stats.transform, "SuppliesSpent", "pop05_reward_supplies_crate.png", "SUPPLIES\nSPENT", "420", 450f);
    }

    private static void BuildMissionResultRewards(Transform parent)
    {
        GameObject rewards = CreateTopLeftMainMenuRect("RewardsPanel", parent, new Rect(2120f, 360f, 960f, 560f));
        AddMissionResultSpriteCentered(rewards.transform, "Frame", "pop05_rewards_panel_frame.png", Vector2.zero, new Vector2(960f, 560f), false);
        AddMissionResultSpriteCentered(rewards.transform, "Icon", "pop05_rewards_blades_icon.png", new Vector2(-384f, 212f), new Vector2(64f, 64f), true);
        AddTextCentered(rewards.transform, "Title", "REWARDS", new Vector2(-118f, 214f), new Vector2(360f, 56f), 38f, TextAlignmentOptions.Left, Accent);
        AddMissionResultValueRow(rewards.transform, "CommanderXpReward", "pop05_reward_commander_xp_shield.png", "Commander XP", "+250", 112f, new Color32(169, 198, 90, 255));
        AddMissionResultValueRow(rewards.transform, "CreditsReward", "pop05_reward_credits_coin.png", "Credits", "+1,200", 26f, new Color32(169, 198, 90, 255));
        AddMissionResultValueRow(rewards.transform, "SuppliesReward", "pop05_reward_supplies_crate.png", "Supplies", "+350", -60f, new Color32(169, 198, 90, 255));
        AddMissionResultValueRow(rewards.transform, "IntelReward", "pop05_reward_intel_document.png", "Intel", "+2", -146f, new Color32(169, 198, 90, 255));
    }

    private static void BuildMissionResultConsequences(Transform parent)
    {
        GameObject consequences = CreateTopLeftMainMenuRect("ConsequencesPanel", parent, new Rect(2120f, 960f, 960f, 580f));
        AddMissionResultSpriteCentered(consequences.transform, "Frame", "pop05_consequences_panel_frame.png", Vector2.zero, new Vector2(960f, 580f), false);
        AddMissionResultSpriteCentered(consequences.transform, "Icon", "pop05_consequences_compass_icon.png", new Vector2(-384f, 214f), new Vector2(64f, 64f), true);
        AddTextCentered(consequences.transform, "Title", "CONSEQUENCES", new Vector2(-92f, 216f), new Vector2(440f, 56f), 38f, TextAlignmentOptions.Left, Accent);
        AddMissionResultValueRow(consequences.transform, "CivilianSafety", "pop05_consequence_civilian_group.png", "Civilian Safety", "+8", 112f, new Color32(169, 198, 90, 255));
        AddMissionResultValueRow(consequences.transform, "DistrictTrust", "pop05_consequence_district_trust_shield.png", "District Trust", "+6", 26f, new Color32(169, 198, 90, 255));
        AddMissionResultValueRow(consequences.transform, "HostileInfluence", "pop05_consequence_hostile_influence.png", "Hostile Influence", "-4", -60f, new Color32(210, 96, 66, 255));
        AddMissionResultValueRow(consequences.transform, "Infrastructure", "pop05_consequence_infrastructure.png", "Infrastructure", "Stable", -146f, Accent);
    }

    private static void BuildMissionResultActions(Transform parent)
    {
        GameObject actions = CreateRect("Actions", parent, new Rect(0f, 1620f, 3600f, 220f));
        AddMissionResultSpriteCentered(actions.transform, "Rail", "pop05_bottom_action_bar_rail_frame.png", Vector2.zero, new Vector2(2440f, 118f), false);

        GameObject replay = CreateCenteredRect("ReplayButton", actions.transform, new Vector2(-1180f, 0f), new Vector2(700f, 150f));
        AddMissionResultSpriteCentered(replay.transform, "Frame", "pop05_replay_button_frame.png", Vector2.zero, new Vector2(700f, 150f), false);
        AddMissionResultSpriteCentered(replay.transform, "Icon", "pop05_replay_arrow_icon.png", new Vector2(-210f, 0f), new Vector2(92f, 92f), true);
        AddTextCentered(replay.transform, "Label", "REPLAY", new Vector2(82f, 0f), new Vector2(360f, 70f), 54f, TextAlignmentOptions.Center, Text);

        GameObject route = CreateCenteredRect("RouteNote", actions.transform, new Vector2(0f, 0f), new Vector2(980f, 118f));
        AddMissionResultSpriteCentered(route.transform, "Frame", "pop05_route_note_chip_frame.png", Vector2.zero, new Vector2(980f, 92f), false);
        AddMissionResultSpriteCentered(route.transform, "Icon", "pop05_route_path_icon.png", new Vector2(-264f, 0f), new Vector2(132f, 94f), true);
        AddTextCentered(route.transform, "Label", "Continue to Campaign Map", new Vector2(100f, 0f), new Vector2(600f, 58f), 38f, TextAlignmentOptions.Center, new Color32(169, 198, 90, 255));

        GameObject continueButton = CreateCenteredRect("ContinueButton", actions.transform, new Vector2(1160f, 0f), new Vector2(780f, 150f));
        AddMissionResultSpriteCentered(continueButton.transform, "Frame", "pop05_continue_button_frame.png", Vector2.zero, new Vector2(780f, 150f), false);
        AddMissionResultSpriteCentered(continueButton.transform, "Chevrons", "pop05_continue_chevrons_icon.png", new Vector2(-240f, 0f), new Vector2(92f, 92f), true);
        AddTextCentered(continueButton.transform, "Label", "CONTINUE", new Vector2(90f, 0f), new Vector2(460f, 76f), 58f, TextAlignmentOptions.Center, Color.black);
        AddMissionResultConfirmButton(continueButton);
    }

    private static void AddMissionResultStar(Transform parent, string name, string label, float x)
    {
        GameObject star = CreateCenteredRect(name, parent, new Vector2(x, 178f), new Vector2(270f, 310f));
        AddMissionResultSpriteCentered(star.transform, "Star", "pop05_star_full_gold.png", new Vector2(0f, 54f), new Vector2(230f, 220f), true);
        AddTextCentered(star.transform, "Label", label, new Vector2(0f, -104f), new Vector2(240f, 86f), 30f, TextAlignmentOptions.Center, Text);
    }

    private static void AddMissionResultObjectiveRow(Transform parent, string name, string label, string state, float y)
    {
        GameObject row = CreateCenteredRect(name, parent, new Vector2(0f, y - 246f), new Vector2(1160f, 72f));
        AddMissionResultSpriteCentered(row.transform, "Frame", "pop05_objective_row_frame.png", Vector2.zero, new Vector2(1160f, 72f), false);
        AddMissionResultSpriteCentered(row.transform, "Checkbox", "pop05_checkbox_checked.png", new Vector2(-514f, 0f), new Vector2(48f, 48f), true);
        AddTextCentered(row.transform, "Label", label, new Vector2(-164f, 0f), new Vector2(700f, 44f), 30f, TextAlignmentOptions.Left, new Color32(169, 198, 90, 255));
        AddTextCentered(row.transform, "State", state, new Vector2(450f, 0f), new Vector2(220f, 44f), 30f, TextAlignmentOptions.Right, new Color32(169, 198, 90, 255));
    }

    private static void AddMissionResultStat(Transform parent, string name, string icon, string label, string value, float x)
    {
        GameObject stat = CreateCenteredRect(name, parent, new Vector2(x, -34f), new Vector2(260f, 210f));
        AddMissionResultSpriteCentered(stat.transform, "Frame", "pop05_stat_tile_frame_1.png", Vector2.zero, new Vector2(260f, 210f), false);
        AddMissionResultSpriteCentered(stat.transform, "Icon", icon, new Vector2(-72f, 34f), new Vector2(78f, 78f), true);
        AddTextCentered(stat.transform, "Label", label, new Vector2(58f, 34f), new Vector2(128f, 76f), 21f, TextAlignmentOptions.Center, Text);
        AddTextCentered(stat.transform, "Value", value, new Vector2(0f, -68f), new Vector2(180f, 70f), 54f, TextAlignmentOptions.Center, Accent);
    }

    private static void AddMissionResultValueRow(Transform parent, string name, string icon, string label, string value, float y, Color valueColor)
    {
        GameObject row = CreateCenteredRect(name, parent, new Vector2(0f, y), new Vector2(830f, 76f));
        AddSolidCentered(row.transform, "Rule", new Vector2(0f, -36f), new Vector2(830f, 2f), new Color(0.73f, 0.59f, 0.25f, 0.35f));
        AddMissionResultSpriteCentered(row.transform, "Icon", icon, new Vector2(-356f, 0f), new Vector2(58f, 58f), true);
        AddTextCentered(row.transform, "Label", label, new Vector2(-86f, 0f), new Vector2(500f, 46f), 30f, TextAlignmentOptions.Left, Text);
        AddTextCentered(row.transform, "Value", value, new Vector2(330f, 0f), new Vector2(190f, 46f), 34f, TextAlignmentOptions.Right, valueColor);
    }

    private static void AddMissionResultProgressBar(Transform parent, string name, Vector2 localPosition, Vector2 size, float fillWidth)
    {
        GameObject bar = CreateCenteredRect(name, parent, localPosition, size);
        AddSolidCentered(bar.transform, "Track", Vector2.zero, size, new Color(0.012f, 0.016f, 0.014f, 0.9f));
        float clampedFill = Mathf.Clamp(fillWidth, 0f, size.x - 8f);
        float fillX = -size.x * 0.5f + clampedFill * 0.5f + 4f;
        AddSolidCentered(bar.transform, "Fill", new Vector2(fillX, 0f), new Vector2(clampedFill, size.y - 8f), Accent);
        Color stroke = new(0.73f, 0.59f, 0.25f, 0.72f);
        AddSolidCentered(bar.transform, "TopStroke", new Vector2(0f, size.y * 0.5f - 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "BottomStroke", new Vector2(0f, -size.y * 0.5f + 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "LeftStroke", new Vector2(-size.x * 0.5f + 2f, 0f), new Vector2(3f, size.y), stroke);
        AddSolidCentered(bar.transform, "RightStroke", new Vector2(size.x * 0.5f - 2f, 0f), new Vector2(3f, size.y), stroke);
    }

    private static void AddMissionResultConfirmButton(GameObject buttonObject)
    {
        Image hitTarget = buttonObject.AddComponent<Image>();
        hitTarget.color = Clear;
        hitTarget.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.42f, 1f);
        colors.pressedColor = new Color(0.88f, 0.56f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        buttonObject.AddComponent<WarlineCaptureShellResultConfirmButtonView>();
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

        AddRouteButtonHotspot(panel.transform, "CommanderPanelHotspot", StretchRect(), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.CommanderProfile);
        AddRouteHotspot(parent, "CommanderPortraitButton", new Rect(83f, 246f, 528f, 507f), WarlineCaptureRoute.CommanderProfile);
    }

    private static void BuildMainMenuFooter(Transform parent)
    {
        GameObject deployVisual = CreateMainMenuRect("DeployOperationButton", parent, new Rect(3659f, -132f, 1141f, 264f), MatchHudFooterSize);
        AddMainMenuSpriteCentered(deployVisual.transform, "Frame", "scn02_deploy_cta_frame.png", Vector2.zero, new Vector2(1141f, 264f), false);
        AddTextCentered(deployVisual.transform, "Text", "DEPLOY OPERATION", new Vector2(-70f, 0f), new Vector2(760f, 112f), 64f, TextAlignmentOptions.Center, Color.black);
        AddMainMenuSpriteCentered(deployVisual.transform, "Chevrons", "scn02_deploy_chevrons.png", new Vector2(448f, 0f), new Vector2(150f, 108f), true);
        AddRouteButtonHotspot(deployVisual.transform, "Hotspot", StretchRect(), UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);

        AddRouteButtonHotspot(parent, "DeployCommandButton", new Rect(3659f, -132f, 1141f, 264f), UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);
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

    private static void AddCommanderProfilePlate(Transform parent, string name, Vector2 localPosition, Vector2 size, Color fill)
    {
        GameObject plate = CreateCenteredRect(name, parent, localPosition, size);
        AddSolidCentered(plate.transform, "Fill", Vector2.zero, size, fill);

        Color stroke = new(0.73f, 0.59f, 0.25f, 0.55f);
        AddSolidCentered(plate.transform, "TopStroke", new Vector2(0f, size.y * 0.5f - 3f), new Vector2(size.x, 4f), stroke);
        AddSolidCentered(plate.transform, "BottomStroke", new Vector2(0f, -size.y * 0.5f + 3f), new Vector2(size.x, 4f), stroke);
        AddSolidCentered(plate.transform, "LeftStroke", new Vector2(-size.x * 0.5f + 3f, 0f), new Vector2(4f, size.y), stroke);
        AddSolidCentered(plate.transform, "RightStroke", new Vector2(size.x * 0.5f - 3f, 0f), new Vector2(4f, size.y), stroke);
    }

    private static void AddCommanderProgressBar(Transform parent, string name, Vector2 localPosition, Vector2 size, float fillWidth)
    {
        GameObject bar = CreateCenteredRect(name, parent, localPosition, size);
        AddSolidCentered(bar.transform, "Track", Vector2.zero, size, new Color(0.012f, 0.016f, 0.014f, 0.92f));

        float clampedFill = Mathf.Clamp(fillWidth, 0f, size.x - 8f);
        float fillX = -size.x * 0.5f + clampedFill * 0.5f + 4f;
        AddSolidCentered(bar.transform, "Fill", new Vector2(fillX, 0f), new Vector2(clampedFill, size.y - 14f), Accent);

        Color stroke = new(0.73f, 0.59f, 0.25f, 0.7f);
        AddSolidCentered(bar.transform, "TopStroke", new Vector2(0f, size.y * 0.5f - 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "BottomStroke", new Vector2(0f, -size.y * 0.5f + 2f), new Vector2(size.x, 3f), stroke);
        AddSolidCentered(bar.transform, "LeftStroke", new Vector2(-size.x * 0.5f + 2f, 0f), new Vector2(3f, size.y), stroke);
        AddSolidCentered(bar.transform, "RightStroke", new Vector2(size.x * 0.5f - 2f, 0f), new Vector2(3f, size.y), stroke);
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
        WarlineCaptureRoute route,
        bool pushHistory = false)
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
        routeButton.Configure(intent, route, pushHistory);
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

    private static Image AddCommanderProfileSpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureCommanderProfileImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureCommanderProfileImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadCommanderProfileSprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadCommanderProfileSprite(string spriteName)
    {
        string path = $"{CommanderProfileLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = spriteName != "scn03_background_21x9_no_ui.png";
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        if (sprite == null)
            throw new InvalidOperationException($"Missing Commander Profile sprite at {path}.");
        return sprite;
    }

    private static Image AddArmorySpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureArmoryImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureArmoryImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadArmorySprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadArmorySprite(string spriteName)
    {
        string path = $"{ArmoryLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = !spriteName.Contains("background_21x9_no_ui", StringComparison.OrdinalIgnoreCase);
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        if (sprite == null)
            throw new InvalidOperationException($"Missing Armory sprite at {path}.");
        return sprite;
    }

    private static Image AddMissionResultSpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureMissionResultImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureMissionResultImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadMissionResultSprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadMissionResultSprite(string spriteName)
    {
        string path = $"{MissionResultLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = !spriteName.Contains("background_21x9_no_ui", StringComparison.OrdinalIgnoreCase);
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        if (sprite == null)
            throw new InvalidOperationException($"Missing Mission Result sprite at {path}.");
        return sprite;
    }

    private static Image AddBuildDrawerSpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureBuildDrawerImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureBuildDrawerImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadBuildDrawerSprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadBuildDrawerSprite(string spriteName)
    {
        string path = $"{BuildDrawerLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        if (sprite == null)
            throw new InvalidOperationException($"Missing Build Drawer sprite at {path}.");
        return sprite;
    }

    private static Image AddMatchHudSprite(Transform parent, string name, string spriteName, Rect rect, Vector2 parentSize, bool preserveAspect)
    {
        GameObject obj = CreateMainMenuRect(name, parent, rect, parentSize);
        return ConfigureMatchHudImage(obj, spriteName, preserveAspect);
    }

    private static Image AddMatchHudSpriteCentered(
        Transform parent,
        string name,
        string spriteName,
        Vector2 localPosition,
        Vector2 size,
        bool preserveAspect)
    {
        GameObject obj = CreateCenteredRect(name, parent, localPosition, size);
        return ConfigureMatchHudImage(obj, spriteName, preserveAspect);
    }

    private static Image ConfigureMatchHudImage(GameObject obj, string spriteName, bool preserveAspect)
    {
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadMatchHudSprite(spriteName);
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite LoadMatchHudSprite(string spriteName)
    {
        string path = $"{MatchHudLayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null && AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        if (sprite == null)
            throw new InvalidOperationException($"Missing Match HUD sprite at {path}.");
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

        ValidatePrefab(MainMenuPrefabPath, "SCN02_MainMenuContent", "MenuBackgroundContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent", "FooterContent");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        ValidateDirectChildren(
            prefab.transform,
            MainMenuPrefabPath,
            "MenuBackgroundContent",
            "HeaderContent",
            "LeftContent",
            "MiddleContent",
            "RightContent",
            "FooterContent");
        ValidateRectSize(prefab.transform.Find("MenuBackgroundContent") as RectTransform, MainMenuBackgroundSize, "SCN02 MenuBackgroundContent");
        ValidateRectSize(prefab.transform.Find("HeaderContent") as RectTransform, MainMenuHeaderSize, "SCN02 HeaderContent");
        ValidateRectSize(prefab.transform.Find("LeftContent") as RectTransform, MainMenuSideSize, "SCN02 LeftContent");
        ValidateRectSize(prefab.transform.Find("MiddleContent") as RectTransform, MainMenuMiddleSize, "SCN02 MiddleContent");
        ValidateRectSize(prefab.transform.Find("RightContent") as RectTransform, MainMenuSideSize, "SCN02 RightContent");
        ValidateRectSize(prefab.transform.Find("FooterContent") as RectTransform, MatchHudFooterSize, "SCN02 FooterContent");

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
        RequireMainMenuChild(prefab, "RightContent/CommanderPortraitButton");
        RequireMainMenuChild(prefab, "FooterContent/DeployOperationButton/Hotspot");
        RequireMainMenuChild(prefab, "FooterContent/DeployCommandButton");

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
