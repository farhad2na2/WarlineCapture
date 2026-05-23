#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn03CommanderProfileSceneBuilder
{
    private const int CanvasWidth = 2400;
    private const int CanvasHeight = 1080;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V04_2400x1080.png";

    private static Color TextMain => new Color32(230, 223, 199, 255);
    private static Color TextMuted => new Color32(180, 169, 126, 255);
    private static Color Gold => new Color32(235, 174, 42, 255);
    private static Color Green => new Color32(157, 184, 70, 255);
    private static Color Blue => new Color32(94, 169, 207, 255);
    private static Color Red => new Color32(211, 87, 55, 255);
    private static Color PanelFill => new Color32(8, 11, 9, 220);
    private static Color PanelFillStrong => new Color32(8, 10, 8, 240);
    private static Color Stroke => new Color(0.72f, 0.62f, 0.36f, 0.58f);

    [MenuItem("WarlineCapture/Design/SCN-03 Build Commander Profile Target Lock")]
    public static void BuildScene()
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN03_CommanderProfile_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = "Screen_SCN03_CommanderProfile_TargetLock";
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-03] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-03 Capture Commander Profile Target Lock")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[SCN-03] Captured {CapturePath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_SCN03_CommanderProfile_TargetLock", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenController controller = root.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(WarlineCaptureRoute.CommanderProfile);

        GameObject visualRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN03_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(visualRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(visualRoot.transform);

        GameObject hitRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN03_HitZones", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(hitRoot.GetComponent<RectTransform>());
        AddHitZones(hitRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Background_CommandTent", "scn03_background_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Background_ReadabilityWash", new RectInt(0, 114, CanvasWidth, CanvasHeight - 114), new Color(0f, 0f, 0f, 0.16f));

        AddHeader(parent);
        AddIdentity(parent);
        AddOverview(parent);
        AddRewardTrack(parent);
        AddRecentHistory(parent);
        AddArmory(parent);
        AddProfileRewards(parent);
        AddAccountSnapshot(parent);
        AddRouteStrip(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Header", HeaderRect()),
            new WarlineUiRect("Identity", IdentityRect()),
            new WarlineUiRect("Overview", OverviewRect()),
            new WarlineUiRect("RewardTrack", RewardTrackRect()),
            new WarlineUiRect("RecentHistory", RecentHistoryRect()),
            new WarlineUiRect("Armory", ArmoryRect()),
            new WarlineUiRect("Rewards", ProfileRewardsRect()),
            new WarlineUiRect("Snapshot", AccountSnapshotRect()),
            new WarlineUiRect("RouteStrip", RouteStripRect()));
    }

    private static void AddHeader(Transform parent)
    {
        RectInt header = HeaderRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_Frame", "scn03_chrome_01_top_header_bar_frame.png", header, false, Color.white);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_Brand", "shared_brand_logo_lockup.png", new RectInt(30, 5, 446, 110), 360, 106, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Title", "COMMANDER PROFILE", new RectInt(540, 17, 540, 54), 35f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Subtitle", "Identity, progression, rewards, history, and roster access", new RectInt(542, 70, 600, 28), 16.5f, TextAlignmentOptions.Left, TextMuted);

        AddHeaderResource(parent, "Credits", "scn03_icon_03_credits_coin.png", "Credits", "187,540", new RectInt(1110, 15, 315, 92), Gold);
        AddHeaderResource(parent, "Supplies", "scn03_icon_04_supplies_crate.png", "Supplies", "92,860", new RectInt(1452, 15, 315, 92), new Color32(164, 169, 108, 255));
        AddHeaderResource(parent, "Command", "scn03_icon_05_command_shield.png", "Command", "2,715", new RectInt(1782, 15, 290, 92), Blue);

        AddHeaderAction(parent, "Inbox", "scn03_icon_06_inbox_envelope.png", new RectInt(1980, 18, 88, 76), 58, 44);
        AddTextBadge(parent, new RectInt(2058, 26, 25, 25), "1", 16f, Gold);
        AddHeaderAction(parent, "Settings", "scn03_icon_07_settings_gear.png", new RectInt(2096, 14, 92, 84), 58, 58);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_BackIcon", "scn03_icon_08_back_arrow.png", new RectInt(2200, 26, 54, 50), 34, 34, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Back", "BACK", new RectInt(2255, 24, 115, 46), 28f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddHeaderResource(Transform parent, string name, string icon, string label, string value, RectInt slot, Color valueColor)
    {
        RectInt iconSlot = new(slot.x, slot.y + 6, 78, 78);
        RectInt labelSlot = new(slot.x + 86, slot.y + 8, slot.width - 90, 26);
        RectInt valueSlot = new(slot.x + 86, slot.y + 38, slot.width - 90, 46);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Header_{name}_Icon", icon, iconSlot, 64, 64, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Header_{name}_Label", label, labelSlot, 18f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Header_{name}_Value", value, valueSlot, 31f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderAction(Transform parent, string name, string icon, RectInt slot, int maxW, int maxH)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Header_{name}", icon, slot, maxW, maxH, TextMuted);
    }

    private static void AddIdentity(Transform parent)
    {
        RectInt rect = IdentityRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Identity_Frame", "scn03_chrome_02_commander_identity_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Identity_TitleIcon", "scn03_icon_02_commander_rank_shield.png", new RectInt(rect.x + 31, rect.y + 17, 58, 58), 44, 52, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Identity_Title", "COMMANDER IDENTITY", new RectInt(rect.x + 96, rect.y + 23, 365, 38), 24f, TextAlignmentOptions.Left, TextMain);

        RectInt portraitFrame = new(rect.x + 48, rect.y + 72, rect.width - 96, 418);
        AddRectStroke(parent, "Identity_PortraitStroke", portraitFrame, new Color(0.88f, 0.67f, 0.12f, 0.82f), 2);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Identity_Portrait", "scn03_portrait_01_commander_portrait_shadowed.png", WarlineCaptureLayeredUiBuilderUtility.Inset(portraitFrame, 9, 9), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Identity_EditIcon", "scn03_icon_09_edit_pencil.png", new RectInt(portraitFrame.x + portraitFrame.width - 62, portraitFrame.y + 22, 45, 45), 34, 34, TextMuted);

        RectInt info = new(rect.x + 40, rect.y + 506, rect.width - 80, 172);
        AddRectStroke(parent, "Identity_InfoStroke", info, Stroke, 2);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Identity_Name", "FIELD COMMANDER", new RectInt(info.x + 18, info.y + 19, 280, 36), 25f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Identity_Level", "LEVEL 38", new RectInt(info.x + 18, info.y + 56, 220, 30), 22f, TextAlignmentOptions.Left, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Identity_Profile", "PROFILE", new RectInt(info.x + info.width - 104, info.y + 25, 85, 28), 17f, TextAlignmentOptions.Right, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Identity_XpLabel", "2,740 / 3,200 XP TO LEVEL 39", new RectInt(info.x + 18, info.y + 102, 330, 24), 17f, TextAlignmentOptions.Left, TextMuted);
        AddProgress(parent, "Identity_Xp", new RectInt(info.x + 18, info.y + 130, info.width - 36, 26), 0.72f, Gold);

        AddSmallButton(parent, "Identity_Edit", new RectInt(rect.x + 40, rect.y + 708, 203, 62), "scn03_icon_09_edit_pencil.png", "EDIT ID");
        AddSmallButton(parent, "Identity_Badges", new RectInt(rect.x + rect.width - 243, rect.y + 708, 203, 62), "scn03_icon_10_badge_shield.png", "BADGES");
    }

    private static void AddOverview(Transform parent)
    {
        RectInt rect = OverviewRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Overview_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 20, 18), new Color32(7, 10, 8, 214));
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Overview_Frame", "scn03_chrome_03_overview_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Overview_Title", "OVERVIEW", new RectInt(rect.x + 34, rect.y + 21, 260, 36), 29f, TextAlignmentOptions.Left, TextMain);

        string[] tabs = { "OVERVIEW", "UPGRADES", "HISTORY", "BADGES", "STATS" };
        for (int i = 0; i < tabs.Length; i++)
        {
            RectInt tab = new(rect.x + 34 + i * 158, rect.y + 52, 130, 48);
            if (i == 0)
                WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Overview_Tab_{i}_SelectedFill", WarlineCaptureLayeredUiBuilderUtility.Inset(tab, 5, 6), new Color(0.58f, 0.60f, 0.18f, 0.62f));
            WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Overview_Tab_{i}_Text", tabs[i], WarlineCaptureLayeredUiBuilderUtility.Inset(tab, 8, 11), 16.5f, TextAlignmentOptions.Center, i == 0 ? TextMain : TextMuted);
        }

        AddStatCell(parent, "Missions", "MISSIONS", "42", "completed", new RectInt(rect.x + 34, rect.y + 135, 265, 126), Gold);
        AddStatCell(parent, "Victories", "VICTORIES", "36", "86% success", new RectInt(rect.x + 326, rect.y + 135, 265, 126), Gold);
        AddStatCell(parent, "Civilians", "CIVILIANS PROTECTED", "91%", "protected", new RectInt(rect.x + 618, rect.y + 135, 265, 126), Gold);
        AddStatCell(parent, "Unlocks", "UNLOCKS", "44", "owned items", new RectInt(rect.x + 34, rect.y + 280, 265, 126), Gold);
        AddStatCell(parent, "Lost", "UNITS LOST", "18", "lifetime", new RectInt(rect.x + 326, rect.y + 280, 265, 126), Red);
        AddStatCell(parent, "Best", "BEST STREAK", "7", "operations", new RectInt(rect.x + 618, rect.y + 280, 265, 126), Gold);
    }

    private static void AddStatCell(Transform parent, string name, string label, string value, string sub, RectInt rect, Color valueColor)
    {
        AddRectStroke(parent, $"Stat_{name}_Stroke", rect, Stroke, 2);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Stat_{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 3, 3), PanelFill);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stat_{name}_Label", label, new RectInt(rect.x + 25, rect.y + 22, rect.width - 50, 30), 16.5f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stat_{name}_Value", value, new RectInt(rect.x + 25, rect.y + 54, rect.width - 50, 42), 34f, TextAlignmentOptions.Left, valueColor);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stat_{name}_Sub", sub, new RectInt(rect.x + 25, rect.y + 93, rect.width - 50, 24), 16f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddRewardTrack(Transform parent)
    {
        RectInt rect = RewardTrackRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "RewardTrack_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 18, 14), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "RewardTrack_Frame", "scn03_chrome_05_reward_track_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "RewardTrack_Title", "COMMANDER REWARD TRACK", new RectInt(rect.x + 35, rect.y + 23, 420, 35), 22f, TextAlignmentOptions.Left, TextMain);

        int startX = rect.x + 50;
        for (int i = 0; i < 6; i++)
        {
            int x = startX + i * 110;
            string node = i < 3 ? "scn03_chrome_17_reward_node_claimed.png" : i == 3 ? "scn03_chrome_18_reward_node_active.png" : "scn03_chrome_19_reward_node_locked.png";
            WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"RewardTrack_Node_{i}", node, new RectInt(x, rect.y + 66, 62, 62), true, Color.white);
            WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"RewardTrack_Node_{i}_Level", (35 + i).ToString(), new RectInt(x + 3, rect.y + 80, 56, 30), 20f, TextAlignmentOptions.Center, TextMain);
            WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"RewardTrack_Node_{i}_State", i < 3 ? "CLAIMED" : i == 3 ? "READY" : "LOCKED", new RectInt(x - 15, rect.y + 132, 90, 25), 15f, TextAlignmentOptions.Center, i == 3 ? Gold : i < 3 ? Green : TextMuted);
            if (i < 5)
                WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"RewardTrack_Link_{i}", new RectInt(x + 64, rect.y + 96, 50, 3), Stroke);
        }

        RectInt claim = new(rect.x + rect.width - 200, rect.y + 70, 160, 65);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "RewardTrack_ClaimFrame", "scn03_chrome_14_primary_gold_cta_frame.png", claim, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "RewardTrack_ClaimText", "CLAIM", new RectInt(claim.x + 18, claim.y + 16, 92, 32), 24f, TextAlignmentOptions.Center, Color.black);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "RewardTrack_ClaimChevron", "scn03_icon_20_claim_chevron.png", new RectInt(claim.x + 110, claim.y + 13, 38, 40), 28, 30, Color.black);
    }

    private static void AddRecentHistory(Transform parent)
    {
        RectInt rect = RecentHistoryRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "RecentHistory_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 18, 14), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "RecentHistory_Frame", "scn03_chrome_06_recent_history_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "RecentHistory_Title", "RECENT HISTORY", new RectInt(rect.x + 35, rect.y + 24, 290, 33), 25f, TextAlignmentOptions.Left, TextMain);
        AddHistoryRow(parent, 0, "scn03_icon_02_commander_rank_shield.png", "M01 First Contact", "Victory  + 3 stars  •  civilians protected", "14m ago");
        AddHistoryRow(parent, 1, "scn03_icon_16_history_crossed_swords.png", "Skirmish: Old Market", "Custom win  •  armor squad survived", "2h ago");
    }

    private static void AddHistoryRow(Transform parent, int index, string icon, string title, string sub, string time)
    {
        RectInt row = new(RecentHistoryRect().x + 36, RecentHistoryRect().y + 70 + index * 63, RecentHistoryRect().width - 72, 51);
        AddRectStroke(parent, $"History_{index}_Stroke", row, Stroke, 1);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"History_{index}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(row, 2, 2), PanelFill);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"History_{index}_Icon", icon, new RectInt(row.x + 16, row.y + 7, 42, 38), 32, 32, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"History_{index}_Title", title, new RectInt(row.x + 76, row.y + 5, 330, 23), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"History_{index}_Sub", sub, new RectInt(row.x + 76, row.y + 28, 410, 20), 13.5f, TextAlignmentOptions.Left, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"History_{index}_Time", time, new RectInt(row.x + 500, row.y + 14, 90, 22), 16f, TextAlignmentOptions.Center, TextMuted);
        AddMiniAction(parent, $"History_{index}_Replay", new RectInt(row.x + row.width - 220, row.y + 9, 105, 33), "REPLAY");
        AddMiniAction(parent, $"History_{index}_Detail", new RectInt(row.x + row.width - 100, row.y + 9, 82, 33), "DETAIL");
    }

    private static void AddArmory(Transform parent)
    {
        RectInt rect = ArmoryRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Armory_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 18, 18), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Armory_Frame", "scn03_chrome_04_armory_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Armory_TitleIcon", "scn03_icon_11_roster_group.png", new RectInt(rect.x + 46, rect.y + 30, 72, 58), 58, 48, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Armory_Title", "ARMORY / SQUADS", new RectInt(rect.x + 135, rect.y + 29, 330, 38), 28f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Armory_Subtitle", "Access and manage your full roster", new RectInt(rect.x + 135, rect.y + 68, 405, 27), 18f, TextAlignmentOptions.Left, TextMuted);

        AddRosterCell(parent, "Units", "scn03_icon_11_roster_group.png", "UNITS", "24", new RectInt(rect.x + 36, rect.y + 126, 306, 64));
        AddRosterCell(parent, "Vehicles", "scn03_icon_12_vehicle.png", "VEHICLES", "9", new RectInt(rect.x + 370, rect.y + 126, 306, 64));
        AddRosterCell(parent, "Buildings", "scn03_icon_13_building.png", "BUILDINGS", "12", new RectInt(rect.x + 36, rect.y + 207, 306, 64));
        AddRosterCell(parent, "Support", "scn03_icon_14_support_plus.png", "SUPPORT", "8", new RectInt(rect.x + 370, rect.y + 207, 306, 64), "Routes to SCN-19");

        RectInt cta = new(rect.x + 40, rect.y + 293, rect.width - 80, 76);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Armory_CtaFrame", "scn03_chrome_14_primary_gold_cta_frame.png", cta, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Armory_CtaText", "OPEN ARMORY", new RectInt(cta.x + 110, cta.y + 17, cta.width - 210, 42), 34f, TextAlignmentOptions.Center, Color.black);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Armory_CtaChevron", "scn03_icon_20_claim_chevron.png", new RectInt(cta.x + cta.width - 92, cta.y + 17, 46, 42), 36, 36, Color.black);
    }

    private static void AddRosterCell(Transform parent, string name, string icon, string label, string value, RectInt rect, string sub = null)
    {
        AddRectStroke(parent, $"Roster_{name}_Stroke", rect, Stroke, 1);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Roster_{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 2, 2), PanelFill);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Roster_{name}_Icon", icon, new RectInt(rect.x + 20, rect.y + 11, 54, 42), 42, 34, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Roster_{name}_Label", label, new RectInt(rect.x + 84, rect.y + 12, 135, 28), 20f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Roster_{name}_Value", value, new RectInt(rect.x + rect.width - 75, rect.y + 12, 55, 30), 26f, TextAlignmentOptions.Right, Gold);
        if (!string.IsNullOrEmpty(sub))
            WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Roster_{name}_Sub", sub, new RectInt(rect.x + 84, rect.y + 39, 170, 22), 14f, TextAlignmentOptions.Left, Green);
    }

    private static void AddProfileRewards(Transform parent)
    {
        RectInt rect = ProfileRewardsRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "ProfileRewards_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 18, 16), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "ProfileRewards_Frame", "scn03_chrome_07_profile_rewards_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ProfileRewards_Title", "PROFILE REWARDS", new RectInt(rect.x + 28, rect.y + 22, 290, 34), 27f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ProfileRewards_Subtitle", "Next milestone unlocks at Level 39", new RectInt(rect.x + 28, rect.y + 58, 390, 24), 17f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ProfileRewards_XpLabel", "XP PROGRESS", new RectInt(rect.x + 28, rect.y + 88, 160, 22), 13.5f, TextAlignmentOptions.Left, TextMuted);
        AddProgress(parent, "ProfileRewards_Xp", new RectInt(rect.x + 28, rect.y + 114, 365, 25), 0.68f, Gold);

        RectInt reward = new(rect.x + 415, rect.y + 86, 278, 72);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "ProfileRewards_RewardFrame", "scn03_chrome_12_small_chip_frame.png", reward, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "ProfileRewards_RewardIcon", "scn03_icon_15_reward_wreath.png", new RectInt(reward.x + 18, reward.y + 10, 54, 52), 44, 44, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ProfileRewards_RewardTitle", "LEVEL 39 REWARD", new RectInt(reward.x + 88, reward.y + 13, 170, 24), 16f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ProfileRewards_RewardSub", "Command Authority + cosmetic frame", new RectInt(reward.x + 88, reward.y + 39, 170, 20), 11.5f, TextAlignmentOptions.Left, Gold);
    }

    private static void AddAccountSnapshot(Transform parent)
    {
        RectInt rect = AccountSnapshotRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "AccountSnapshot_Back", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 18, 16), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "AccountSnapshot_Frame", "scn03_chrome_08_account_snapshot_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "AccountSnapshot_Title", "ACCOUNT SNAPSHOT", new RectInt(rect.x + 28, rect.y + 22, 330, 34), 24f, TextAlignmentOptions.Left, TextMain);
        AddSnapshotCell(parent, "Campaign", "CAMPAIGN", "35%", new RectInt(rect.x + 28, rect.y + 76, 310, 64));
        AddSnapshotCell(parent, "Operations", "OPERATIONS", "6/18", new RectInt(rect.x + 365, rect.y + 76, 310, 64));
        AddSnapshotCell(parent, "Skirmish", "SKIRMISH", "12 WINS", new RectInt(rect.x + 28, rect.y + 155, 310, 64));
        AddSnapshotCell(parent, "Readiness", "READINESS", "HIGH", new RectInt(rect.x + 365, rect.y + 155, 310, 64));
    }

    private static void AddSnapshotCell(Transform parent, string name, string label, string value, RectInt rect)
    {
        AddRectStroke(parent, $"Snapshot_{name}_Stroke", rect, Stroke, 1);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Snapshot_{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 2, 2), PanelFill);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Snapshot_{name}_Label", label, new RectInt(rect.x + 20, rect.y + 12, rect.width - 40, 23), 16f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Snapshot_{name}_Value", value, new RectInt(rect.x + 20, rect.y + 34, rect.width - 40, 29), 22f, TextAlignmentOptions.Left, Gold);
    }

    private static void AddRouteStrip(Transform parent)
    {
        RectInt rect = RouteStripRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "RouteStrip_Frame", "scn03_chrome_20_route_strip_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Route_MainMenu", "MAIN MENU", new RectInt(rect.x + 155, rect.y + 20, 180, 30), 19f, TextAlignmentOptions.Center, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Route_ArrowA", ">", new RectInt(rect.x + 370, rect.y + 18, 40, 30), 22f, TextAlignmentOptions.Center, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Route_Profile", "COMMANDER PROFILE", new RectInt(rect.x + 440, rect.y + 20, 260, 30), 19f, TextAlignmentOptions.Center, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Route_ArrowB", ">", new RectInt(rect.x + 725, rect.y + 18, 40, 30), 22f, TextAlignmentOptions.Center, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Route_Armory", "ARMORY", new RectInt(rect.x + 785, rect.y + 20, 155, 30), 19f, TextAlignmentOptions.Center, TextMuted);
    }

    private static void AddSmallButton(Transform parent, string name, RectInt rect, string icon, string text)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"{name}_Frame", "scn03_chrome_15_secondary_dark_cta_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"{name}_Icon", icon, new RectInt(rect.x + 18, rect.y + 14, 42, 35), 30, 30, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{name}_Text", text, new RectInt(rect.x + 66, rect.y + 15, rect.width - 78, 34), 21f, TextAlignmentOptions.Center, TextMuted);
    }

    private static void AddMiniAction(Transform parent, string name, RectInt rect, string text)
    {
        AddRectStroke(parent, $"{name}_Stroke", rect, Stroke, 1);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 2, 2), PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{name}_Text", text, WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 6, 6), 15f, TextAlignmentOptions.Center, Gold);
    }

    private static void AddProgress(Transform parent, string name, RectInt rect, float fill, Color fillColor)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"{name}_Frame", "scn03_chrome_13_thin_progress_bar_frame.png", rect, false, Color.white);
        int inset = 5;
        int fillW = Mathf.RoundToInt((rect.width - inset * 2) * Mathf.Clamp01(fill));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", new RectInt(rect.x + inset, rect.y + inset, fillW, rect.height - inset * 2), fillColor);
        for (int i = 1; i < 6; i++)
        {
            int x = rect.x + inset + Mathf.RoundToInt((rect.width - inset * 2) * (i / 6f));
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Tick_{i}", new RectInt(x, rect.y + inset, 2, rect.height - inset * 2), new Color(0f, 0f, 0f, 0.55f));
        }
    }

    private static void AddRectStroke(Transform parent, string name, RectInt rect, Color color, int width)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Top", new RectInt(rect.x, rect.y, rect.width, width), color);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Bottom", new RectInt(rect.x, rect.yMax - width, rect.width, width), color);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Left", new RectInt(rect.x, rect.y, width, rect.height), color);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Right", new RectInt(rect.xMax - width, rect.y, width, rect.height), color);
    }

    private static void AddTextBadge(Transform parent, RectInt rect, string value, float size, Color color)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Header_InboxBadge_Back", rect, new Color(0.85f, 0.54f, 0.12f, 0.85f));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_InboxBadge_Text", value, rect, size, TextAlignmentOptions.Center, Color.white);
    }

    private static void AddHitZones(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "BackHitZone", new RectInt(2188, 10, 190, 100));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "OpenArmoryHitZone", new RectInt(ArmoryRect().x + 40, ArmoryRect().y + 293, ArmoryRect().width - 80, 76));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "EditIdHitZone", new RectInt(IdentityRect().x + 40, IdentityRect().y + 708, 203, 62));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "BadgesHitZone", new RectInt(IdentityRect().x + IdentityRect().width - 243, IdentityRect().y + 708, 203, 62));
    }

    private static RectInt HeaderRect() => new(0, 0, CanvasWidth, 126);
    private static RectInt IdentityRect() => new(74, 140, 512, 826);
    private static RectInt OverviewRect() => new(628, 140, 910, 414);
    private static RectInt RewardTrackRect() => new(628, 560, 910, 176);
    private static RectInt RecentHistoryRect() => new(628, 756, 910, 200);
    private static RectInt ArmoryRect() => new(1590, 140, 728, 380);
    private static RectInt ProfileRewardsRect() => new(1590, 540, 728, 205);
    private static RectInt AccountSnapshotRect() => new(1590, 760, 728, 205);
    private static RectInt RouteStripRect() => new(732, 992, 936, 56);
}
#endif
