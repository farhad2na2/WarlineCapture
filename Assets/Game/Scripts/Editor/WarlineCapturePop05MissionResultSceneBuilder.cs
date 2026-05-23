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
    private const string LegacyPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_TargetLock.prefab";
    private const string LegacyScenePath = "Assets/Game/Scenes/DesignTargets/POP05_MissionResult_TargetLock.unity";
    private const string LegacyCapturePath = "Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V08_2400x1080.png";

    private static Color TextMain => new Color32(225, 216, 188, 255);
    private static Color TextMuted => new Color32(169, 154, 112, 255);
    private static Color Gold => new Color32(231, 169, 39, 255);
    private static Color Green => new Color32(157, 184, 68, 255);
    private static Color Red => new Color32(206, 84, 52, 255);
    private static Color Blue => new Color32(112, 182, 194, 255);
    private static Color PanelFill => new Color32(12, 14, 11, 222);
    private static Color PanelFillStrong => new Color32(8, 10, 8, 238);

    private enum ResultVariant
    {
        Victory,
        Partial,
        Defeat,
        Withdrawn,
        Resolved
    }

    private readonly struct ObjectiveRow
    {
        public ObjectiveRow(string label, string status, string icon, Color color)
        {
            Label = label;
            Status = status;
            Icon = icon;
            Color = color;
        }

        public string Label { get; }
        public string Status { get; }
        public string Icon { get; }
        public Color Color { get; }
    }

    private readonly struct RewardRow
    {
        public RewardRow(string icon, string label, string value, Color color, bool disabled = false)
        {
            Icon = icon;
            Label = label;
            Value = value;
            Color = color;
            Disabled = disabled;
        }

        public string Icon { get; }
        public string Label { get; }
        public string Value { get; }
        public Color Color { get; }
        public bool Disabled { get; }
    }

    private readonly struct ConsequenceRow
    {
        public ConsequenceRow(string icon, string label, string value, Color color)
        {
            Icon = icon;
            Label = label;
            Value = value;
            Color = color;
        }

        public string Icon { get; }
        public string Label { get; }
        public string Value { get; }
        public Color Color { get; }
    }

    private readonly struct StatRow
    {
        public StatRow(string name, string icon, string label, string value, Color color)
        {
            Name = name;
            Icon = icon;
            Label = label;
            Value = value;
            Color = color;
        }

        public string Name { get; }
        public string Icon { get; }
        public string Label { get; }
        public string Value { get; }
        public Color Color { get; }
    }

    private readonly struct VariantConfig
    {
        public VariantConfig(
            ResultVariant variant,
            string id,
            string title,
            string subtitle,
            string metadata,
            string background,
            string snapshot,
            string headerAccent,
            string summaryTitle,
            string summaryBody,
            string ratingLabel,
            int[] starStates,
            ObjectiveRow[] objectives,
            StatRow[] stats,
            RewardRow[] rewards,
            ConsequenceRow[] consequences,
            string routeIcon,
            string routeText,
            string ctaLabel,
            string ctaIcon)
        {
            Variant = variant;
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Metadata = metadata;
            Background = background;
            Snapshot = snapshot;
            HeaderAccent = headerAccent;
            SummaryTitle = summaryTitle;
            SummaryBody = summaryBody;
            RatingLabel = ratingLabel;
            StarStates = starStates;
            Objectives = objectives;
            Stats = stats;
            Rewards = rewards;
            Consequences = consequences;
            RouteIcon = routeIcon;
            RouteText = routeText;
            CtaLabel = ctaLabel;
            CtaIcon = ctaIcon;
        }

        public ResultVariant Variant { get; }
        public string Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Metadata { get; }
        public string Background { get; }
        public string Snapshot { get; }
        public string HeaderAccent { get; }
        public string SummaryTitle { get; }
        public string SummaryBody { get; }
        public string RatingLabel { get; }
        public int[] StarStates { get; }
        public ObjectiveRow[] Objectives { get; }
        public StatRow[] Stats { get; }
        public RewardRow[] Rewards { get; }
        public ConsequenceRow[] Consequences { get; }
        public string RouteIcon { get; }
        public string RouteText { get; }
        public string CtaLabel { get; }
        public string CtaIcon { get; }
    }

    [MenuItem("WarlineCapture/Design/POP-05 Build Mission Result Target Lock")]
    public static void BuildScene()
    {
        BuildVariantScene(CreateVariant(ResultVariant.Victory), LegacyPrefabPath, LegacyScenePath, "Screen_POP05_MissionResult_TargetLock");
    }

    [MenuItem("WarlineCapture/Design/POP-05 Capture Mission Result Target Lock")]
    public static void CaptureScene()
    {
        VariantConfig config = CreateVariant(ResultVariant.Victory);
        BuildVariantScene(config, LegacyPrefabPath, LegacyScenePath, "Screen_POP05_MissionResult_TargetLock");
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(LegacyPrefabPath, LegacyCapturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[POP-05] Captured {LegacyCapturePath}");
    }

    [MenuItem("WarlineCapture/Design/POP-05 Capture All Mission Result Variants")]
    public static void CaptureAllVariants()
    {
        CaptureVariant(CreateVariant(ResultVariant.Victory));
        CaptureVariant(CreateVariant(ResultVariant.Partial));
        CaptureVariant(CreateVariant(ResultVariant.Defeat));
        CaptureVariant(CreateVariant(ResultVariant.Withdrawn));
        CaptureVariant(CreateVariant(ResultVariant.Resolved));
    }

    private static void CaptureVariant(VariantConfig config)
    {
        string prefabPath = VariantPrefabPath(config);
        string scenePath = VariantScenePath(config);
        string capturePath = VariantCapturePath(config);
        BuildVariantScene(config, prefabPath, scenePath, $"Screen_POP05_MissionResult_{config.Id}_TargetLock");
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(prefabPath, capturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[POP-05] Captured {config.Id} {capturePath}");
    }

    private static void BuildVariantScene(VariantConfig config, string prefabPath, string scenePath, string rootName)
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot(config, rootName);

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject($"{config.Id}_MissionResult_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = rootName;
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(scenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[POP-05] Built {config.Id} scene={scenePath} prefab={prefabPath}");
    }

    private static GameObject BuildCanvasPrefabRoot(VariantConfig config, string rootName)
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject(rootName, null);
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
        BuildLayeredVisual(visualRoot.transform, config);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent, VariantConfig config)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Background_NoUi", config.Background, new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Background_BottomShade", new RectInt(0, 760, CanvasWidth, 320), new Color(0f, 0f, 0f, 0.22f));

        AddHeader(parent, config);
        AddMissionSummary(parent, config);
        AddRatingAndObjectives(parent, config);
        AddPerformanceStats(parent, config);
        AddRewards(parent, config);
        AddConsequences(parent, config);
        AddBottomActions(parent, config);

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

    private static void AddHeader(Transform parent, VariantConfig config)
    {
        RectInt rect = HeaderRect();
        AddFrame(parent, "Header_Frame", "pop05_result_header_frame.png", rect, 18);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_CommanderLogo", "pop05_commander_logo.png", new RectInt(rect.x + 44, rect.y + 31, 118, 110), 104, 100, TextMain);
        AddHeaderAccent(parent, config, rect);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Title", config.Title, new RectInt(rect.x + 575, rect.y + 25, 666, 72), HeaderTitleSize(config), TextAlignmentOptions.Center, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_Subtitle", config.Subtitle, new RectInt(rect.x + 625, rect.y + 94, 566, 34), 25f, TextAlignmentOptions.Center, TextMain);

        RectInt xp = new(rect.x + 1400, rect.y + 40, 330, 86);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_XpShield", "pop05_reward_commander_xp_shield.png", new RectInt(xp.x, xp.y + 2, 78, 78), 66, 66, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_XpLabel", "COMMANDER XP", new RectInt(xp.x + 88, xp.y + 1, 230, 30), 18f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_XpFrame", "pop05_xp_bar_frame.png", new RectInt(xp.x + 88, xp.y + 36, 220, 24), false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_XpFill", "pop05_progress_gold_fill_segment.png", new RectInt(xp.x + 96, xp.y + 42, HeaderXpFillWidth(config), 12), false, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Header_XpValue", HeaderXpValue(config), new RectInt(xp.x + 88, xp.y + 61, 220, 25), 18f, TextAlignmentOptions.Left, HeaderTitleColor(config));

        RectInt meta = new(rect.x + 485, rect.y + 132, 845, 38);
        AddFrame(parent, "Metadata_Frame", "pop05_mission_metadata_strip_frame.png", meta, 5, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Metadata_Text", config.Metadata, new RectInt(meta.x + 38, meta.y + 8, meta.width - 76, 23), 14.5f, TextAlignmentOptions.Center, TextMain);
    }

    private static void AddHeaderAccent(Transform parent, VariantConfig config, RectInt rect)
    {
        if (config.Variant == ResultVariant.Victory)
        {
            WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_VictoryWingLeft", "pop05_victory_wing_left.png", new RectInt(rect.x + 500, rect.y + 35, 180, 72), 170, 58, Gold);
            WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_VictoryWingRight", "pop05_victory_wing_right.png", new RectInt(rect.x + 1135, rect.y + 35, 180, 72), 170, 58, Gold);
            return;
        }

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_StateAccentLeft", config.HeaderAccent, new RectInt(rect.x + 508, rect.y + 42, 126, 58), 108, 50, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_StateAccentRight", config.HeaderAccent, new RectInt(rect.x + 1182, rect.y + 42, 126, 58), 108, 50, HeaderTitleColor(config));
    }

    private static void AddMissionSummary(Transform parent, VariantConfig config)
    {
        RectInt rect = SummaryRect();
        AddFrame(parent, "Summary_Frame", "pop05_mission_summary_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Summary_TitleIcon", "pop05_mission_summary_star_outline.png", new RectInt(rect.x + 32, rect.y + 26, 58, 58), 46, 46, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_Title", "MISSION SUMMARY", new RectInt(rect.x + 102, rect.y + 28, rect.width - 140, 34), 27f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_Subtitle", "First Response", new RectInt(rect.x + 103, rect.y + 64, rect.width - 142, 26), 18f, TextAlignmentOptions.Left, TextMuted);

        RectInt snapshotFrame = new(rect.x + 36, rect.y + 112, rect.width - 72, 240);
        AddFrame(parent, "Summary_SnapshotFrame", "pop05_mission_snapshot_frame.png", snapshotFrame, 8, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Summary_SnapshotArt", config.Snapshot, WarlineCaptureLayeredUiBuilderUtility.Inset(snapshotFrame, 12, 12), Color.white);

        RectInt desc = new(rect.x + 34, rect.y + 385, rect.width - 68, 218);
        AddFrame(parent, "Summary_DescriptionFrame", "pop05_mission_description_panel_frame.png", desc, 10, new Color32(10, 12, 10, 212));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_DescriptionTitle", config.SummaryTitle, new RectInt(desc.x + 24, desc.y + 24, desc.width - 48, 30), config.SummaryTitle.Length > 22 ? 18f : 22f, TextAlignmentOptions.Left, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Summary_DescriptionBody", config.SummaryBody, new RectInt(desc.x + 24, desc.y + 64, desc.width - 48, 116), 19f, TextAlignmentOptions.TopLeft, TextMain, true);
    }

    private static void AddRatingAndObjectives(Transform parent, VariantConfig config)
    {
        RectInt rect = RatingRect();
        AddFrame(parent, "Rating_Frame", "pop05_rating_objectives_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rating_TitleIcon", "pop05_rewards_blades_icon.png", new RectInt(rect.x + 34, rect.y + 26, 58, 56), 46, 44, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rating_Title", "MISSION RATING", new RectInt(rect.x + 102, rect.y + 29, 270, 35), 28f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rating_Result", config.RatingLabel, new RectInt(rect.x + rect.width - 290, rect.y + 31, 255, 32), 24f, TextAlignmentOptions.Right, HeaderTitleColor(config));

        AddStar(parent, "Primary", rect.x + 122, rect.y + 104, "OBJECTIVE COMPLETE", config.StarStates[0], config);
        AddStar(parent, "Secondary", rect.x + 323, rect.y + 104, "CIVILIANS PROTECTED", config.StarStates[1], config);
        AddStar(parent, "Tertiary", rect.x + 524, rect.y + 104, "LOSSES LOW", config.StarStates[2], config);

        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Objectives_Title", "OBJECTIVES", new RectInt(rect.x + 44, rect.y + 290, 220, 30), 22f, TextAlignmentOptions.Left, TextMuted);
        for (int i = 0; i < config.Objectives.Length; i++)
            AddObjective(parent, rect, i, config.Objectives[i]);
    }

    private static void AddStar(Transform parent, string name, int x, int y, string label, int state, VariantConfig config)
    {
        string sprite = state switch
        {
            2 => "pop05_star_full_gold.png",
            1 => "pop05_variant_star_partial_gold_large.png",
            _ => "pop05_variant_star_dim_large.png"
        };
        Color color = state == 0 ? new Color(1f, 1f, 1f, 0.72f) : Color.white;
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Rating_{name}_Star", sprite, new RectInt(x, y, 132, 122), 116, 106, color);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rating_{name}_Label", label, new RectInt(x - 24, y + 128, 180, 42), 14.5f, TextAlignmentOptions.Center, state == 0 ? TextMuted : TextMain, true);
    }

    private static void AddObjective(Transform parent, RectInt panel, int index, ObjectiveRow rowData)
    {
        RectInt row = new(panel.x + 44, panel.y + 330 + index * 53, panel.width - 88, 43);
        AddFrame(parent, $"Objective_{index}_Frame", "pop05_objective_row_frame.png", row, 4, new Color32(9, 11, 9, 210));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Objective_{index}_Icon", rowData.Icon, new RectInt(row.x + 15, row.y + 7, 34, 30), 27, 27, rowData.Color);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Objective_{index}_Label", rowData.Label, new RectInt(row.x + 60, row.y + 9, row.width - 250, 25), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Objective_{index}_Status", rowData.Status, new RectInt(row.x + row.width - 178, row.y + 9, 148, 24), 16f, TextAlignmentOptions.Right, rowData.Color);
    }

    private static void AddPerformanceStats(Transform parent, VariantConfig config)
    {
        RectInt rect = StatsRect();
        AddFrame(parent, "Stats_Frame", "pop05_performance_stats_panel_frame.png", rect, 14);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Stats_Title", "PERFORMANCE", new RectInt(rect.x + 42, rect.y + 24, 250, 32), 24f, TextAlignmentOptions.Left, TextMain);

        int tileW = 168;
        int gap = 18;
        int x = rect.x + 42;
        for (int i = 0; i < config.Stats.Length; i++)
        {
            RectInt tile = new(x + (tileW + gap) * i, rect.y + 72, tileW, 92);
            AddStatTile(parent, config.Stats[i], $"pop05_stat_tile_frame_{i + 1}.png", tile);
        }
    }

    private static void AddStatTile(Transform parent, StatRow stat, string frame, RectInt rect)
    {
        AddFrame(parent, $"Stats_{stat.Name}_Frame", frame, rect, 5, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Stats_{stat.Name}_Icon", stat.Icon, new RectInt(rect.x + 14, rect.y + 20, 44, 48), 36, 36, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stats_{stat.Name}_Label", stat.Label, new RectInt(rect.x + 66, rect.y + 16, rect.width - 78, 22), 14f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Stats_{stat.Name}_Value", stat.Value, new RectInt(rect.x + 66, rect.y + 40, rect.width - 78, 34), 28f, TextAlignmentOptions.Left, stat.Color);
    }

    private static void AddRewards(Transform parent, VariantConfig config)
    {
        RectInt rect = RewardsRect();
        AddFrame(parent, "Rewards_Frame", "pop05_rewards_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rewards_TitleIcon", "pop05_rewards_blades_icon.png", new RectInt(rect.x + 32, rect.y + 24, 58, 55), 44, 42, HeaderTitleColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rewards_Title", "REWARDS", new RectInt(rect.x + 96, rect.y + 28, 210, 35), 28f, TextAlignmentOptions.Left, TextMain);
        for (int i = 0; i < config.Rewards.Length; i++)
            AddRewardRow(parent, rect, i, config.Rewards[i]);
    }

    private static void AddRewardRow(Transform parent, RectInt panel, int index, RewardRow rowData)
    {
        RectInt row = new(panel.x + 36, panel.y + 86 + index * 50, panel.width - 72, 42);
        Color rowColor = rowData.Disabled ? new Color(1f, 1f, 1f, 0.46f) : Color.white;
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Reward_{index}_Divider", new RectInt(row.x, row.y + row.height - 2, row.width, 1), new Color(0.78f, 0.64f, 0.28f, 0.18f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Reward_{index}_Icon", rowData.Icon, new RectInt(row.x + 4, row.y + 4, 38, 34), 32, 32, rowColor);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Reward_{index}_Label", rowData.Label, new RectInt(row.x + 58, row.y + 8, 245, 25), 18f, TextAlignmentOptions.Left, rowData.Disabled ? TextMuted : TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Reward_{index}_Value", rowData.Value, new RectInt(row.x + row.width - 155, row.y + 8, 136, 25), 18f, TextAlignmentOptions.Right, rowData.Color);
        if (rowData.Disabled)
            WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"Reward_{index}_DisabledOverlay", "pop05_variant_disabled_reward_overlay.png", new RectInt(row.x + row.width - 72, row.y + 3, 58, 32), true, new Color(1f, 1f, 1f, 0.62f));
    }

    private static void AddConsequences(Transform parent, VariantConfig config)
    {
        RectInt rect = ConsequencesRect();
        AddFrame(parent, "Consequences_Frame", "pop05_consequences_panel_frame.png", rect, 16);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Consequences_TitleIcon", "pop05_consequences_compass_icon.png", new RectInt(rect.x + 34, rect.y + 27, 54, 50), 38, 38, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Consequences_Title", "CONSEQUENCES", new RectInt(rect.x + 98, rect.y + 29, 260, 34), 26f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Consequences_InteriorCleanPlate", new RectInt(rect.x + 20, rect.y + 84, rect.width - 40, 230), new Color32(10, 12, 10, 232));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Consequences_TitleDivider", new RectInt(rect.x + 48, rect.y + 88, rect.width - 96, 1), new Color(0.78f, 0.64f, 0.28f, 0.18f));
        for (int i = 0; i < config.Consequences.Length; i++)
            AddConsequenceRow(parent, rect, i, config.Consequences[i]);
    }

    private static void AddConsequenceRow(Transform parent, RectInt panel, int index, ConsequenceRow rowData)
    {
        RectInt row = new(panel.x + 52, panel.y + 96 + index * 50, panel.width - 104, 42);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Consequence_{index}_Icon", rowData.Icon, new RectInt(row.x + 2, row.y + 5, 44, 34), 34, 32, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Consequence_{index}_Label", rowData.Label, new RectInt(row.x + 62, row.y + 7, 250, 27), 18f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Consequence_{index}_Value", rowData.Value, new RectInt(row.x + row.width - 160, row.y + 7, 148, 27), rowData.Value.Length > 10 ? 16f : 18f, TextAlignmentOptions.Right, rowData.Color);

        if (index < 3)
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Consequence_{index}_Divider", new RectInt(row.x + 60, row.y + row.height + 3, row.width - 72, 1), new Color(0.78f, 0.64f, 0.28f, 0.14f));
    }

    private static void AddBottomActions(Transform parent, VariantConfig config)
    {
        RectInt rail = BottomRailRect();
        AddFrame(parent, "BottomRail_Frame", "pop05_bottom_action_bar_rail_frame.png", rail, 10, new Color32(8, 9, 8, 185));

        RectInt replay = ReplayRect();
        AddFrame(parent, "Replay_Frame", "pop05_replay_button_frame.png", replay, 10, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Replay_Icon", ReplayIcon(config), new RectInt(replay.x + 42, replay.y + 21, 66, 58), 50, 48, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Replay_Label", ReplayLabel(config), new RectInt(replay.x + 116, replay.y + 30, 286, 42), config.Variant == ResultVariant.Defeat ? 23f : 27f, TextAlignmentOptions.Left, TextMain);

        RectInt route = RouteChipRect();
        AddFrame(parent, "RouteChip_Frame", "pop05_route_note_chip_frame.png", route, 8, PanelFillStrong);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "RouteChip_Icon", config.RouteIcon, new RectInt(route.x + 28, route.y + 14, 48, 42), 36, 34, RouteIconColor(config));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "RouteChip_Text", config.RouteText, new RectInt(route.x + 84, route.y + 17, route.width - 112, 34), 22f, TextAlignmentOptions.Left, TextMain);

        RectInt cta = ContinueRect();
        AddFrame(parent, "Continue_Frame", "pop05_continue_button_frame.png", cta, 10, new Color32(55, 36, 7, 220));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Continue_Label", config.CtaLabel, new RectInt(cta.x + 30, cta.y + 23, 348, 50), config.CtaLabel.Length > 12 ? 27f : 36f, TextAlignmentOptions.Center, new Color32(32, 24, 10, 255));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Continue_Chevrons", config.CtaIcon, new RectInt(cta.x + cta.width - 88, cta.y + 23, 56, 50), 48, 38, new Color32(89, 64, 24, 255));
    }

    private static VariantConfig CreateVariant(ResultVariant variant)
    {
        return variant switch
        {
            ResultVariant.Partial => CreatePartial(),
            ResultVariant.Defeat => CreateDefeat(),
            ResultVariant.Withdrawn => CreateWithdrawn(),
            ResultVariant.Resolved => CreateResolved(),
            _ => CreateVictory()
        };
    }

    private static VariantConfig CreateVictory()
    {
        return new VariantConfig(
            ResultVariant.Victory,
            "Victory",
            "OPERATION COMPLETE",
            "FIRST CONTACT COMPLETE",
            "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  07:42",
            "pop05_background_21x9_no_ui.png",
            "pop05_mission_snapshot_art.png",
            string.Empty,
            "DISTRICT STABILIZED",
            "The first hostile cell was intercepted before it could disrupt relief lanes. Civilian response remains steady and command authority has expanded.",
            "3 / 3 STARS",
            new[] { 2, 2, 2 },
            new[]
            {
                new ObjectiveRow("Neutralize hostile patrol", "COMPLETE", "pop05_checkbox_checked.png", Green),
                new ObjectiveRow("Protect convoy route", "COMPLETE", "pop05_checkbox_checked.png", Green),
                new ObjectiveRow("Keep civilian losses at zero", "COMPLETE", "pop05_checkbox_checked.png", Green)
            },
            new[]
            {
                new StatRow("Enemies", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "34", Gold),
                new StatRow("Units", "pop05_stat_units_lost_shield.png", "UNITS LOST", "0", Gold),
                new StatRow("Civilians", "pop05_consequence_civilian_group.png", "CIVILIANS", "18", Green),
                new StatRow("Time", "pop05_stat_timer_clock.png", "TIME", "07:42", Gold)
            },
            new[]
            {
                new RewardRow("pop05_reward_commander_xp_shield.png", "Commander XP", "+1,250", Gold),
                new RewardRow("pop05_reward_credits_coin.png", "Credits", "+2,400", Gold),
                new RewardRow("pop05_reward_supplies_crate.png", "Supplies", "+860", Green),
                new RewardRow("pop05_reward_intel_document.png", "Intel", "+1 Report", TextMain)
            },
            new[]
            {
                new ConsequenceRow("pop05_consequence_civilian_group.png", "Civilian Safety", "+8", Green),
                new ConsequenceRow("pop05_consequence_district_trust_shield.png", "District Trust", "+6", Green),
                new ConsequenceRow("pop05_consequence_hostile_influence.png", "Hostile Influence", "-4", Red),
                new ConsequenceRow("pop05_consequence_infrastructure.png", "Infrastructure", "Stable", Gold)
            },
            "pop05_route_path_icon.png",
            "Continue to Campaign Map",
            "CONTINUE",
            "pop05_continue_chevrons_icon.png");
    }

    private static VariantConfig CreatePartial()
    {
        return new VariantConfig(
            ResultVariant.Partial,
            "Partial",
            "OBJECTIVE SECURED",
            "PARTIAL SUCCESS",
            "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  11:18",
            "pop05_partial_background_21x9_no_ui.png",
            "pop05_partial_mission_snapshot_art.png",
            "pop05_variant_partial_header_accent.png",
            "DISTRICT PARTIALLY SECURED",
            "Primary command objectives were secured, but civilian pressure and lingering hostile signals require follow-up operations.",
            "2 / 3 STARS",
            new[] { 2, 1, 0 },
            new[]
            {
                new ObjectiveRow("Neutralize hostile patrol", "COMPLETE", "pop05_checkbox_checked.png", Green),
                new ObjectiveRow("Protect convoy route", "PARTIAL", "pop05_variant_icon_warning_triangle.png", Gold),
                new ObjectiveRow("Keep civilian losses at zero", "UNRESOLVED", "pop05_variant_icon_unknown_question.png", TextMuted)
            },
            new[]
            {
                new StatRow("Enemies", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "28", Gold),
                new StatRow("Units", "pop05_stat_units_lost_shield.png", "UNITS LOST", "1", Gold),
                new StatRow("Civilians", "pop05_consequence_civilian_group.png", "CIVILIANS", "11", Gold),
                new StatRow("Time", "pop05_stat_timer_clock.png", "TIME", "11:18", Gold)
            },
            new[]
            {
                new RewardRow("pop05_reward_commander_xp_shield.png", "Commander XP", "+780", Gold),
                new RewardRow("pop05_reward_credits_coin.png", "Credits", "+1,120", Gold),
                new RewardRow("pop05_reward_supplies_crate.png", "Supplies", "+420", Green),
                new RewardRow("pop05_reward_intel_document.png", "Intel", "Partial", TextMuted)
            },
            new[]
            {
                new ConsequenceRow("pop05_consequence_civilian_group.png", "Civilian Safety", "+2", Gold),
                new ConsequenceRow("pop05_consequence_district_trust_shield.png", "District Trust", "+1", Gold),
                new ConsequenceRow("pop05_consequence_hostile_influence.png", "Hostile Influence", "+3", Red),
                new ConsequenceRow("pop05_consequence_infrastructure.png", "Infrastructure", "Damaged", Gold)
            },
            "pop05_variant_marker_civilian_unresolved.png",
            "Review unresolved objectives",
            "CONTINUE",
            "pop05_continue_chevrons_icon.png");
    }

    private static VariantConfig CreateDefeat()
    {
        return new VariantConfig(
            ResultVariant.Defeat,
            "Defeat",
            "OPERATION FAILED",
            "MISSION LOST",
            "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  05:36",
            "pop05_defeat_background_21x9_no_ui.png",
            "pop05_defeat_mission_snapshot_art.png",
            "pop05_variant_failure_header_warning_accent.png",
            "COMMAND CELL OVERRUN",
            "The hostile cell held the district and forced command units to break contact. Refit the squad before attempting another push.",
            "0 / 3 STARS",
            new[] { 0, 0, 0 },
            new[]
            {
                new ObjectiveRow("Neutralize hostile patrol", "FAILED", "pop05_variant_icon_failed_x.png", Red),
                new ObjectiveRow("Protect convoy route", "FAILED", "pop05_variant_icon_failed_x.png", Red),
                new ObjectiveRow("Keep civilian losses at zero", "FAILED", "pop05_variant_icon_failed_x.png", Red)
            },
            new[]
            {
                new StatRow("Enemies", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "9", Red),
                new StatRow("Units", "pop05_stat_units_lost_shield.png", "UNITS LOST", "4", Red),
                new StatRow("Civilians", "pop05_consequence_civilian_group.png", "CIVILIANS", "3", Red),
                new StatRow("Time", "pop05_stat_timer_clock.png", "TIME", "05:36", Gold)
            },
            new[]
            {
                new RewardRow("pop05_reward_commander_xp_shield.png", "Commander XP", "+120", TextMuted),
                new RewardRow("pop05_reward_credits_coin.png", "Credits", "0", TextMuted, true),
                new RewardRow("pop05_reward_supplies_crate.png", "Supplies", "-240", Red),
                new RewardRow("pop05_reward_intel_document.png", "Intel", "Lost", Red, true)
            },
            new[]
            {
                new ConsequenceRow("pop05_consequence_civilian_group.png", "Civilian Safety", "-7", Red),
                new ConsequenceRow("pop05_consequence_district_trust_shield.png", "District Trust", "-5", Red),
                new ConsequenceRow("pop05_consequence_hostile_influence.png", "Hostile Influence", "+9", Red),
                new ConsequenceRow("pop05_consequence_infrastructure.png", "Infrastructure", "Compromised", Red)
            },
            "pop05_variant_adjust_loadout_helmet.png",
            "Adjust loadout before retry",
            "RETRY OPERATION",
            "pop05_variant_retry_chevrons.png");
    }

    private static VariantConfig CreateWithdrawn()
    {
        return new VariantConfig(
            ResultVariant.Withdrawn,
            "Withdrawn",
            "FORCE WITHDRAWN",
            "UNITS EXTRACTED",
            "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  09:04",
            "pop05_withdrawn_background_21x9_no_ui.png",
            "pop05_withdrawn_mission_snapshot_art.png",
            "pop05_variant_withdraw_header_accent.png",
            "TACTICAL WITHDRAWAL",
            "Command units extracted before losses escalated. The district remains contested and needs a follow-up mission.",
            "1 / 3 STARS",
            new[] { 1, 0, 0 },
            new[]
            {
                new ObjectiveRow("Extract command squad", "EXTRACTED", "pop05_variant_icon_extracted_arrow.png", Blue),
                new ObjectiveRow("Neutralize hostile patrol", "ABANDONED", "pop05_variant_icon_abandoned_square.png", Gold),
                new ObjectiveRow("Secure district route", "UNRESOLVED", "pop05_variant_icon_unknown_question.png", TextMuted)
            },
            new[]
            {
                new StatRow("Enemies", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "16", Gold),
                new StatRow("Units", "pop05_stat_units_lost_shield.png", "UNITS LOST", "1", Gold),
                new StatRow("Civilians", "pop05_consequence_civilian_group.png", "CIVILIANS", "7", Gold),
                new StatRow("Time", "pop05_stat_timer_clock.png", "TIME", "09:04", Gold)
            },
            new[]
            {
                new RewardRow("pop05_reward_commander_xp_shield.png", "Commander XP", "+420", Gold),
                new RewardRow("pop05_reward_credits_coin.png", "Credits", "+300", Gold),
                new RewardRow("pop05_reward_supplies_crate.png", "Supplies", "Recovered", Blue),
                new RewardRow("pop05_reward_intel_document.png", "Intel", "Unknown", TextMuted, true)
            },
            new[]
            {
                new ConsequenceRow("pop05_consequence_civilian_group.png", "Civilian Safety", "-1", Gold),
                new ConsequenceRow("pop05_consequence_district_trust_shield.png", "District Trust", "-2", Red),
                new ConsequenceRow("pop05_consequence_hostile_influence.png", "Hostile Influence", "+5", Red),
                new ConsequenceRow("pop05_consequence_infrastructure.png", "Infrastructure", "Contested", Gold)
            },
            "pop05_variant_return_map_icon.png",
            "Return to district map",
            "RETURN TO MAP",
            "pop05_variant_main_menu_arrow.png");
    }

    private static VariantConfig CreateResolved()
    {
        return new VariantConfig(
            ResultVariant.Resolved,
            "Resolved",
            "OPERATION RESOLVED",
            "SIMULATION RESOLVED",
            "CAMPAIGN  Saga Campaign        CHAPTER  01        MISSION  First Response        DURATION  AUTO",
            "pop05_partial_background_21x9_no_ui.png",
            "pop05_partial_mission_snapshot_art.png",
            "pop05_variant_partial_header_accent.png",
            "DISTRICT STATE UPDATED",
            "The operation was resolved by command simulation. Review the district state and choose the next tactical assignment.",
            "1 / 3 STARS",
            new[] { 1, 0, 0 },
            new[]
            {
                new ObjectiveRow("Resolve operation outcome", "RESOLVED", "pop05_checkbox_checked.png", Green),
                new ObjectiveRow("Protect convoy route", "ESTIMATED", "pop05_variant_icon_unknown_question.png", TextMuted),
                new ObjectiveRow("Verify civilian impact", "PENDING", "pop05_variant_icon_warning_triangle.png", Gold)
            },
            new[]
            {
                new StatRow("Enemies", "pop05_stat_enemies_defeated_crosshair.png", "ENEMIES", "Auto", Gold),
                new StatRow("Units", "pop05_stat_units_lost_shield.png", "UNITS LOST", "Est.", Gold),
                new StatRow("Civilians", "pop05_consequence_civilian_group.png", "CIVILIANS", "Est.", Gold),
                new StatRow("Time", "pop05_stat_timer_clock.png", "TIME", "AUTO", Gold)
            },
            new[]
            {
                new RewardRow("pop05_reward_commander_xp_shield.png", "Commander XP", "+520", Gold),
                new RewardRow("pop05_reward_credits_coin.png", "Credits", "+740", Gold),
                new RewardRow("pop05_reward_supplies_crate.png", "Supplies", "+260", Green),
                new RewardRow("pop05_reward_intel_document.png", "Intel", "District", TextMain)
            },
            new[]
            {
                new ConsequenceRow("pop05_consequence_civilian_group.png", "Civilian Safety", "Estimated", Gold),
                new ConsequenceRow("pop05_consequence_district_trust_shield.png", "District Trust", "+1", Green),
                new ConsequenceRow("pop05_consequence_hostile_influence.png", "Hostile Influence", "+2", Red),
                new ConsequenceRow("pop05_consequence_infrastructure.png", "Infrastructure", "Updated", Gold)
            },
            "pop05_route_path_icon.png",
            "Open updated district state",
            "VIEW DISTRICT",
            "pop05_continue_chevrons_icon.png");
    }

    private static Color HeaderTitleColor(VariantConfig config)
    {
        return config.Variant switch
        {
            ResultVariant.Defeat => Red,
            ResultVariant.Withdrawn => Blue,
            ResultVariant.Resolved => Gold,
            ResultVariant.Partial => Gold,
            _ => Gold
        };
    }

    private static Color RouteIconColor(VariantConfig config)
    {
        return config.Variant switch
        {
            ResultVariant.Defeat => Red,
            ResultVariant.Withdrawn => Blue,
            ResultVariant.Resolved => Green,
            _ => Green
        };
    }

    private static float HeaderTitleSize(VariantConfig config)
    {
        return config.Title.Length > 16 ? 48f : 56f;
    }

    private static int HeaderXpFillWidth(VariantConfig config)
    {
        return config.Variant switch
        {
            ResultVariant.Defeat => 44,
            ResultVariant.Withdrawn => 92,
            ResultVariant.Resolved => 104,
            ResultVariant.Partial => 118,
            _ => 150
        };
    }

    private static string HeaderXpValue(VariantConfig config)
    {
        return config.Variant switch
        {
            ResultVariant.Defeat => "+120 XP",
            ResultVariant.Withdrawn => "+420 XP",
            ResultVariant.Resolved => "+520 XP",
            ResultVariant.Partial => "+780 XP",
            _ => "+1,250 XP"
        };
    }

    private static string ReplayIcon(VariantConfig config)
    {
        return config.Variant == ResultVariant.Defeat ? "pop05_variant_adjust_loadout_helmet.png" : "pop05_replay_arrow_icon.png";
    }

    private static string ReplayLabel(VariantConfig config)
    {
        return config.Variant == ResultVariant.Defeat ? "ADJUST LOADOUT" : "REPLAY MISSION";
    }

    private static string VariantPrefabPath(VariantConfig config) => $"Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_{config.Id}_TargetLock.prefab";
    private static string VariantScenePath(VariantConfig config) => $"Assets/Game/Scenes/DesignTargets/POP05_MissionResult_{config.Id}_TargetLock.unity";
    private static string VariantCapturePath(VariantConfig config) => $"Design/AgentReports/Captures/POP05_MissionResult_{config.Id}_TargetLock_V01_2400x1080.png";

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
