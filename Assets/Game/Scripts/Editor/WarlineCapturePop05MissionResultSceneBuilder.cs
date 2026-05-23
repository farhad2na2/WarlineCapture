#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCapturePop05MissionResultSceneBuilder
{
    private const int CanvasWidth = 2400;
    private const int CanvasHeight = 1080;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_TargetLock.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/POP05_MissionResult_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V07_2400x1080.png";

    private static Color TextMain => new Color32(225, 216, 188, 255);
    private static Color TextMuted => new Color32(169, 154, 112, 255);
    private static Color Gold => new Color32(231, 169, 39, 255);
    private static Color Green => new Color32(157, 184, 68, 255);
    private static Color Red => new Color32(206, 84, 52, 255);
    private static Color PanelFill => new Color32(12, 14, 11, 222);
    private static Color PanelFillStrong => new Color32(8, 10, 8, 238);

    [MenuItem("WarlineCapture/Design/POP-05 Build Mission Result Target Lock")]
    public static void BuildScene()
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("POP05_MissionResult_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = "Screen_POP05_MissionResult_TargetLock";
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[POP-05] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/POP-05 Capture Mission Result Target Lock")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[POP-05] Captured {CapturePath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_POP05_MissionResult_TargetLock", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenController controller = root.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(WarlineCaptureRoute.Match);

        GameObject visualRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("POP05_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(visualRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(visualRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Background_NoUi", "pop05_background_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Background_BottomShade", new RectInt(0, 760, CanvasWidth, 320), new Color(0f, 0f, 0f, 0.26f));

        AddHeader(parent);
        AddMissionSummary(parent);
        AddRatingAndObjectives(parent);
        AddPerformanceStats(parent);
        AddRewards(parent);
        AddConsequences(parent);
        AddBottomActions(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Header", HeaderRect()),
            new WarlineUiRect("MissionSummary", SummaryRect()),
            new WarlineUiRect("RatingObjectives", RatingRect()),
            new WarlineUiRect("PerformanceStats", StatsRect()),
            new WarlineUiRect("Rewards", RewardsRect()),
            new WarlineUiRect("Consequences", ConsequencesRect()),
            new WarlineUiRect("ReplayButton", ReplayRect()),
            new WarlineUiRect("RouteNote", RouteChipRect()),
            new WarlineUiRect("ContinueButton", ContinueRect()));
    }

    private static void AddHeader(Transform parent)
    {
        RectInt rect = HeaderRect();
        AddFrame(parent, "Header_Frame", "pop05_result_header_frame.png", rect, 18);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_CommanderLogo", "pop05_commander_logo.png", new RectInt(rect.x + 44, rect.y + 31, 118, 110), 104, 100, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_VictoryWingLeft", "pop05_victory_wing_left.png", new RectInt(rect.x + 555, rect.y + 35, 180, 72), 170, 58, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_VictoryWingRight", "pop05_victory_wing_right.png", new RectInt(rect.x + 1080, rect.y + 35, 180, 72), 170, 58, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Title", "VICTORY", new RectInt(rect.x + 720, rect.y + 26, 380, 72), 64f, TextAlignmentOptions.Center, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Subtitle", "FIRST CONTACT COMPLETE", new RectInt(rect.x + 650, rect.y + 93, 520, 34), 25f, TextAlignmentOptions.Center, TextMain);

        RectInt xp = new(rect.x + 1400, rect.y + 40, 330, 86);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_XpShield", "pop05_reward_commander_xp_shield.png", new RectInt(xp.x, xp.y + 2, 78, 78), 66, 66, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_XpLabel", "COMMANDER XP", new RectInt(xp.x + 88, xp.y + 1, 230, 30), 18f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_XpFrame", "pop05_xp_bar_frame.png", new RectInt(xp.x + 88, xp.y + 36, 220, 24), false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_XpFill", "pop05_progress_gold_fill_segment.png", new RectInt(xp.x + 96, xp.y + 42, 150, 12), false, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_XpValue", "+1,250 XP", new RectInt(xp.x + 88, xp.y + 61, 220, 25), 18f, TextAlignmentOptions.Left, Gold);

        RectInt meta = new(rect.x + 485, rect.y + 132, 845, 38);
        AddFrame(parent, "Metadata_Frame", "pop05_mission_metadata_strip_frame.png", meta, 5, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Metadata_Text", "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  07:42", new RectInt(meta.x + 38, meta.y + 8, meta.width - 76, 23), 14.5f, TextAlignmentOptions.Center, TextMain);
    }

    private static void AddMissionSummary(Transform parent)
    {
        RectInt rect = SummaryRect();
        AddFrame(parent, "Summary_Frame", "pop05_mission_summary_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Summary_TitleIcon", "pop05_mission_summary_star_outline.png", new RectInt(rect.x + 32, rect.y + 26, 58, 58), 46, 46, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_Title", "MISSION SUMMARY", new RectInt(rect.x + 102, rect.y + 28, rect.width - 140, 34), 27f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_Subtitle", "First Response", new RectInt(rect.x + 103, rect.y + 64, rect.width - 142, 26), 18f, TextAlignmentOptions.Left, TextMuted);

        RectInt snapshotFrame = new(rect.x + 36, rect.y + 112, rect.width - 72, 240);
        AddFrame(parent, "Summary_SnapshotFrame", "pop05_mission_snapshot_frame.png", snapshotFrame, 8, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Summary_SnapshotArt", "pop05_mission_snapshot_art.png", WarlineCaptureLayeredUiBuilderUtility.Inset(snapshotFrame, 12, 12), new Color(0.92f, 0.88f, 0.78f, 1f));

        RectInt desc = new(rect.x + 34, rect.y + 385, rect.width - 68, 218);
        AddFrame(parent, "Summary_DescriptionFrame", "pop05_mission_description_panel_frame.png", desc, 10, new Color32(10, 12, 10, 212));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_DescriptionTitle", "DISTRICT STABILIZED", new RectInt(desc.x + 24, desc.y + 24, desc.width - 48, 30), 22f, TextAlignmentOptions.Left, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_DescriptionBody", "The first hostile cell was intercepted before it could disrupt relief lanes. Civilian response remains steady and command authority has expanded.", new RectInt(desc.x + 24, desc.y + 64, desc.width - 48, 116), 19f, TextAlignmentOptions.TopLeft, TextMain, true);
    }

    private static void AddRatingAndObjectives(Transform parent)
    {
        RectInt rect = RatingRect();
        AddFrame(parent, "Rating_Frame", "pop05_rating_objectives_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rating_TitleIcon", "pop05_rewards_blades_icon.png", new RectInt(rect.x + 34, rect.y + 26, 58, 56), 46, 44, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rating_Title", "MISSION RATING", new RectInt(rect.x + 102, rect.y + 29, 270, 35), 28f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rating_Result", "3 / 3 STARS", new RectInt(rect.x + rect.width - 265, rect.y + 31, 230, 32), 24f, TextAlignmentOptions.Right, Gold);

        AddStar(parent, "ObjectiveComplete", rect.x + 122, rect.y + 104, "OBJECTIVE COMPLETE");
        AddStar(parent, "CiviliansProtected", rect.x + 323, rect.y + 104, "CIVILIANS PROTECTED");
        AddStar(parent, "LossesLow", rect.x + 524, rect.y + 104, "LOSSES LOW");

        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Objectives_Title", "OBJECTIVES", new RectInt(rect.x + 44, rect.y + 290, 220, 30), 22f, TextAlignmentOptions.Left, TextMuted);
        AddObjective(parent, rect, 0, "Neutralize hostile patrol", "COMPLETE");
        AddObjective(parent, rect, 1, "Protect convoy route", "COMPLETE");
        AddObjective(parent, rect, 2, "Keep civilian losses at zero", "COMPLETE");
    }

    private static void AddStar(Transform parent, string name, int x, int y, string label)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Rating_{name}_Star", "pop05_star_full_gold.png", new RectInt(x, y, 132, 122), 116, 106, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rating_{name}_Label", label, new RectInt(x - 24, y + 128, 180, 42), 14.5f, TextAlignmentOptions.Center, TextMain, true);
    }

    private static void AddObjective(Transform parent, RectInt panel, int index, string label, string status)
    {
        RectInt row = new(panel.x + 44, panel.y + 330 + index * 53, panel.width - 88, 43);
        AddFrame(parent, $"Objective_{index}_Frame", "pop05_objective_row_frame.png", row, 4, new Color32(9, 11, 9, 210));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Objective_{index}_Check", "pop05_checkbox_checked.png", new RectInt(row.x + 17, row.y + 8, 30, 28), 24, 24, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Objective_{index}_Label", label, new RectInt(row.x + 60, row.y + 9, row.width - 230, 25), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Objective_{index}_Status", status, new RectInt(row.x + row.width - 150, row.y + 9, 120, 24), 17f, TextAlignmentOptions.Right, Green);
    }

    private static void AddPerformanceStats(Transform parent)
    {
        RectInt rect = StatsRect();
        AddFrame(parent, "Stats_Frame", "pop05_performance_stats_panel_frame.png", rect, 14);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Stats_Title", "PERFORMANCE", new RectInt(rect.x + 42, rect.y + 24, 250, 32), 24f, TextAlignmentOptions.Left, TextMain);

        int tileW = 168;
        int gap = 18;
        int x = rect.x + 42;
        AddStatTile(parent, "Enemies", "pop05_stat_tile_frame_1.png", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "34", new RectInt(x, rect.y + 72, tileW, 92));
        AddStatTile(parent, "Units", "pop05_stat_tile_frame_2.png", "pop05_stat_units_lost_shield.png", "UNITS LOST", "0", new RectInt(x + (tileW + gap), rect.y + 72, tileW, 92));
        AddStatTile(parent, "Civilians", "pop05_stat_tile_frame_3.png", "pop05_consequence_civilian_group.png", "CIVILIANS", "18", new RectInt(x + (tileW + gap) * 2, rect.y + 72, tileW, 92));
        AddStatTile(parent, "Time", "pop05_stat_tile_frame_4.png", "pop05_stat_timer_clock.png", "TIME", "07:42", new RectInt(x + (tileW + gap) * 3, rect.y + 72, tileW, 92));
    }

    private static void AddStatTile(Transform parent, string name, string frame, string icon, string label, string value, RectInt rect)
    {
        AddFrame(parent, $"Stats_{name}_Frame", frame, rect, 5, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Stats_{name}_Icon", icon, new RectInt(rect.x + 14, rect.y + 20, 44, 48), 36, 36, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stats_{name}_Label", label, new RectInt(rect.x + 66, rect.y + 16, rect.width - 78, 22), 14f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stats_{name}_Value", value, new RectInt(rect.x + 66, rect.y + 40, rect.width - 78, 34), 28f, TextAlignmentOptions.Left, Gold);
    }

    private static void AddRewards(Transform parent)
    {
        RectInt rect = RewardsRect();
        AddFrame(parent, "Rewards_Frame", "pop05_rewards_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rewards_TitleIcon", "pop05_rewards_blades_icon.png", new RectInt(rect.x + 32, rect.y + 24, 58, 55), 44, 42, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rewards_Title", "REWARDS", new RectInt(rect.x + 96, rect.y + 28, 210, 35), 28f, TextAlignmentOptions.Left, TextMain);
        AddRewardRow(parent, rect, 0, "pop05_reward_commander_xp_shield.png", "Commander XP", "+1,250", Gold);
        AddRewardRow(parent, rect, 1, "pop05_reward_credits_coin.png", "Credits", "+2,400", Gold);
        AddRewardRow(parent, rect, 2, "pop05_reward_supplies_crate.png", "Supplies", "+860", Green);
        AddRewardRow(parent, rect, 3, "pop05_reward_intel_document.png", "Intel", "+1 Report", TextMain);
    }

    private static void AddRewardRow(Transform parent, RectInt panel, int index, string icon, string label, string value, Color valueColor)
    {
        RectInt row = new(panel.x + 36, panel.y + 86 + index * 50, panel.width - 72, 42);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Reward_{index}_Divider", new RectInt(row.x, row.y + row.height - 2, row.width, 1), new Color(0.78f, 0.64f, 0.28f, 0.18f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Reward_{index}_Icon", icon, new RectInt(row.x + 4, row.y + 4, 38, 34), 32, 32, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Reward_{index}_Label", label, new RectInt(row.x + 58, row.y + 8, 245, 25), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Reward_{index}_Value", value, new RectInt(row.x + row.width - 155, row.y + 8, 136, 25), 18f, TextAlignmentOptions.Right, valueColor);
    }

    private static void AddConsequences(Transform parent)
    {
        RectInt rect = ConsequencesRect();
        AddFrame(parent, "Consequences_Frame", "pop05_consequences_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Consequences_TitleIcon", "pop05_consequences_compass_icon.png", new RectInt(rect.x + 34, rect.y + 27, 54, 50), 38, 38, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Consequences_Title", "CONSEQUENCES", new RectInt(rect.x + 98, rect.y + 29, 260, 34), 26f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Consequences_InteriorCleanPlate", new RectInt(rect.x + 20, rect.y + 84, rect.width - 40, 230), new Color32(10, 12, 10, 232));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Consequences_TitleDivider", new RectInt(rect.x + 48, rect.y + 88, rect.width - 96, 1), new Color(0.78f, 0.64f, 0.28f, 0.18f));
        AddConsequenceRow(parent, rect, 0, "pop05_consequence_civilian_group.png", "Civilian Safety", "+8", Green);
        AddConsequenceRow(parent, rect, 1, "pop05_consequence_district_trust_shield.png", "District Trust", "+6", Green);
        AddConsequenceRow(parent, rect, 2, "pop05_consequence_hostile_influence.png", "Hostile Influence", "-4", Red);
        AddConsequenceRow(parent, rect, 3, "pop05_consequence_infrastructure.png", "Infrastructure", "Stable", Gold);
    }

    private static void AddConsequenceRow(Transform parent, RectInt panel, int index, string icon, string label, string value, Color valueColor)
    {
        RectInt row = new(panel.x + 52, panel.y + 96 + index * 50, panel.width - 104, 42);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Consequence_{index}_Icon", icon, new RectInt(row.x + 2, row.y + 5, 44, 34), 34, 32, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Consequence_{index}_Label", label, new RectInt(row.x + 62, row.y + 7, 280, 27), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Consequence_{index}_Value", value, new RectInt(row.x + row.width - 112, row.y + 7, 100, 27), 18f, TextAlignmentOptions.Right, valueColor);

        if (index < 3)
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Consequence_{index}_Divider", new RectInt(row.x + 60, row.y + row.height + 3, row.width - 72, 1), new Color(0.78f, 0.64f, 0.28f, 0.14f));
    }

    private static void AddBottomActions(Transform parent)
    {
        RectInt rail = BottomRailRect();
        AddFrame(parent, "BottomRail_Frame", "pop05_bottom_action_bar_rail_frame.png", rail, 10, new Color32(8, 9, 8, 185));

        RectInt replay = ReplayRect();
        AddFrame(parent, "Replay_Frame", "pop05_replay_button_frame.png", replay, 10, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Replay_Icon", "pop05_replay_arrow_icon.png", new RectInt(replay.x + 42, replay.y + 21, 66, 58), 50, 48, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Replay_Label", "REPLAY MISSION", new RectInt(replay.x + 118, replay.y + 30, 260, 42), 27f, TextAlignmentOptions.Left, TextMain);

        RectInt route = RouteChipRect();
        AddFrame(parent, "RouteChip_Frame", "pop05_route_note_chip_frame.png", route, 8, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "RouteChip_Icon", "pop05_route_path_icon.png", new RectInt(route.x + 28, route.y + 14, 48, 42), 36, 34, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "RouteChip_Text", "Continue to Campaign Map", new RectInt(route.x + 84, route.y + 17, route.width - 112, 34), 22f, TextAlignmentOptions.Left, TextMain);

        RectInt cta = ContinueRect();
        AddFrame(parent, "Continue_Frame", "pop05_continue_button_frame.png", cta, 10, new Color32(55, 36, 7, 220));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Continue_Label", "CONTINUE", new RectInt(cta.x + 52, cta.y + 23, 280, 50), 36f, TextAlignmentOptions.Center, new Color32(32, 24, 10, 255));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Continue_Chevrons", "pop05_continue_chevrons_icon.png", new RectInt(cta.x + cta.width - 114, cta.y + 24, 74, 48), 64, 40, new Color32(89, 64, 24, 255));
    }

    private static void AddFrame(Transform parent, string name, string sprite, RectInt rect, int fillInset)
    {
        AddFrame(parent, name, sprite, rect, fillInset, PanelFill);
    }

    private static void AddFrame(Transform parent, string name, string sprite, RectInt rect, int fillInset, Color fillColor)
    {
        if (fillInset > 0)
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, fillInset, fillInset), fillColor);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, name, sprite, rect, false, Color.white);
    }

    private static RectInt HeaderRect() => new(292, 18, 1816, 172);
    private static RectInt SummaryRect() => new(296, 214, 450, 680);
    private static RectInt RatingRect() => new(758, 214, 780, 486);
    private static RectInt StatsRect() => new(758, 710, 780, 184);
    private static RectInt RewardsRect() => new(1550, 214, 560, 322);
    private static RectInt ConsequencesRect() => new(1550, 558, 560, 336);
    private static RectInt BottomRailRect() => new(296, 916, 1814, 124);
    private static RectInt ReplayRect() => new(304, 924, 436, 100);
    private static RectInt RouteChipRect() => new(872, 940, 610, 70);
    private static RectInt ContinueRect() => new(1628, 928, 468, 96);
}
#endif
