#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn13SkirmishSetupSceneBuilder
{
    private const int CanvasWidth = 4800;
    private const int CanvasHeight = 2160;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/SkirmishSetup/LayeredOneGo";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_SCN13_SkirmishSetup_TargetLock.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN13_SkirmishSetup_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/SCN13_SkirmishSetup_TargetLock_2400x1080.png";

    private static Color TextMain => new Color32(232, 226, 202, 255);
    private static Color TextMuted => new Color32(192, 181, 143, 255);
    private static Color GoldText => new Color32(238, 172, 43, 255);
    private static Color BlueText => new Color32(96, 172, 207, 255);
    private static Color DisabledText => new Color32(128, 124, 105, 255);
    private static Color DropdownText => new Color32(220, 209, 172, 255);
    private static Color TextShadow => new Color32(8, 8, 6, 120);
    private static Color DropdownBacking => new Color32(18, 18, 13, 255);
    private static Color DropdownChrome => new Color(0.88f, 0.82f, 0.62f, 0.62f);
    private static Color SubtleChrome => new Color(0.86f, 0.80f, 0.62f, 0.72f);
    private static Color SelectedPresetTint => new Color(0.76f, 0.72f, 0.34f, 0.82f);

    [MenuItem("WarlineCapture/Design/SCN-13 Build Skirmish Setup Target Lock")]
    public static void BuildScene()
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN13_SkirmishSetup_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = "Screen_SCN13_SkirmishSetup_TargetLock";
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-13] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-13 Capture Skirmish Setup Target Lock")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, 2400, 1080, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[SCN-13] Captured {CapturePath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_SCN13_SkirmishSetup_TargetLock", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenController screenController = root.AddComponent<WarlineCaptureScreenController>();
        screenController.SetRouteForTests(WarlineCaptureRoute.QuickCustomSetup);

        GameObject artRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN13_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(artRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(artRoot.transform);

        GameObject hitRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN13_HitZones", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(hitRoot.GetComponent<RectTransform>());
        AddHitZones(hitRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Background_CommandTent", "scn13_background_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);

        AddHeader(parent);
        AddTitle(parent);
        AddPresetRail(parent);
        AddOperationPreview(parent);
        AddRulesPanel(parent);
        AddBottomActions(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Title", TitleRect()),
            new WarlineUiRect("PresetRail", PresetRailRect()),
            new WarlineUiRect("Preview", PreviewRect()),
            new WarlineUiRect("Rules", RulesRect()),
            new WarlineUiRect("Info", InfoRect()),
            new WarlineUiRect("Reset", ResetRect()),
            new WarlineUiRect("Randomize", RandomizeRect()),
            new WarlineUiRect("Launch", LaunchRect()));
    }

    private static void AddHeader(Transform parent)
    {
        RectInt logoPanel = HeaderLogoRect();
        RectInt resourcePanel = HeaderResourceRect();
        RectInt actionsPanel = HeaderActionsRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_LogoPanel", "scn13_header_logo_panel_bg.png", logoPanel, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_ResourcePanel", "scn13_header_resource_panel_bg.png", resourcePanel, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Header_ActionsPanel", "scn13_header_right_actions_bg.png", actionsPanel, false, Color.white);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Header_Logo", "scn13_brand_logo_lockup.png", WarlineCaptureLayeredUiBuilderUtility.Inset(logoPanel, 88, 48), 650, 135, Color.white);

        AddHeaderResource(parent, "Credits", "scn13_resource_credits_coin.png", "Credits", "187,540", new RectInt(resourcePanel.x + 52, resourcePanel.y + 22, 560, 190), GoldText);
        AddHeaderResource(parent, "Supplies", "scn13_resource_supplies_crate.png", "Supplies", "92,860", new RectInt(resourcePanel.x + 650, resourcePanel.y + 22, 570, 190), new Color32(161, 166, 105, 255));
        AddHeaderResource(parent, "Command", "scn13_resource_command_shield.png", "Command", "2,715", new RectInt(resourcePanel.x + 1256, resourcePanel.y + 22, 540, 190), BlueText);

        AddHeaderAction(parent, "Inbox", "scn13_icon_inbox_envelope.png", new RectInt(actionsPanel.x + 58, actionsPanel.y + 40, 205, 134), 150, 112);
        AddHeaderAction(parent, "Settings", "scn13_icon_settings_gear.png", new RectInt(actionsPanel.x + 318, actionsPanel.y + 36, 190, 142), 148, 148);
    }

    private static void AddHeaderResource(Transform parent, string name, string icon, string label, string value, RectInt slot, Color valueColor)
    {
        RectInt iconSlot = new(slot.x + 8, slot.y + 16, 150, 150);
        RectInt labelSlot = new(slot.x + 185, slot.y + 32, slot.width - 205, 46);
        RectInt valueSlot = new(slot.x + 185, slot.y + 82, slot.width - 205, 70);
        WarlineUiImagePlacement iconPlacement = WarlineCaptureLayeredUiBuilderUtility.VisibleFittedPlacement(LayerRoot, icon, iconSlot, 142, 142);
        WarlineCaptureLayeredUiBuilderUtility.ValidateSectionContent(
            $"Header_{name}",
            slot,
            new WarlineUiRect($"{name}_Icon", iconPlacement.VisibleRect),
            new WarlineUiRect($"{name}_Label", labelSlot),
            new WarlineUiRect($"{name}_Value", valueSlot));

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Header_{name}_Icon", icon, iconSlot, 142, 142, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Header_{name}_Label", label, labelSlot, 34f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Header_{name}_Value", value, valueSlot, 52f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderAction(Transform parent, string name, string icon, RectInt slot, int maxW, int maxH)
    {
        WarlineUiImagePlacement placement = WarlineCaptureLayeredUiBuilderUtility.VisibleFittedPlacement(LayerRoot, icon, slot, maxW, maxH);
        WarlineCaptureLayeredUiBuilderUtility.ValidateSectionContent($"HeaderAction_{name}", slot, new WarlineUiRect($"{name}_Icon", placement.VisibleRect));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"HeaderAction_{name}", icon, slot, maxW, maxH, TextMain);
    }

    private static void AddTitle(Transform parent)
    {
        RectInt rect = TitleRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Title_Frame", "scn13_title_back_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Title_Back", "scn13_icon_back_arrow.png", new RectInt(rect.x + 34, rect.y + 28, 118, 118), 86, 86, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Title_SkirmishIcon", "scn13_icon_skirmish_blades.png", new RectInt(rect.x + 190, rect.y + 24, 128, 122), 104, 104, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Title_Skirmish", "SKIRMISH", new RectInt(rect.x + 350, rect.y + 24, 470, 70), 62f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Title_Subtitle", "Configure Operation", new RectInt(rect.x + 350, rect.y + 94, 470, 44), 32f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddPresetRail(Transform parent)
    {
        RectInt rect = PresetRailRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "PresetRail_Frame", "scn13_preset_rail_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "PresetRail_Title", "OPERATION PRESETS", new RectInt(rect.x + 205, rect.y + 20, 530, 58), 38f, TextAlignmentOptions.Center, TextMuted);

        AddPresetRow(parent, 0, true, "scn13_icon_preset_target.png", "Tutorial Intercept", "Elimination", false);
        AddPresetRow(parent, 1, false, "scn13_icon_preset_convoy.png", "Convoy Pressure", "Supply Disruption", true);
        AddPresetRow(parent, 2, false, "scn13_icon_preset_airlift.png", "Airlift Extraction", "Extraction", true);
        AddPresetRow(parent, 3, false, "scn13_icon_preset_breach.png", "Breach Assault", "Base Assault", true);
        AddPresetRow(parent, 4, false, "scn13_icon_preset_hidden_cell.png", "Hidden Cell Raid", "Intel Sweep", true);

        RectInt manage = new(rect.x + 48, rect.y + rect.height - 130, 610, 82);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "PresetRail_ManageFrame", "scn13_secondary_action_button_frame.png", manage, new Vector4(58f, 58f, 25f, 25f), SubtleChrome);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "PresetRail_ManageIcon", "scn13_icon_manage_list.png", new RectInt(manage.x + 44, manage.y + 16, 70, 54), 56, 48, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "PresetRail_ManageText", "MANAGE PRESETS", new RectInt(manage.x + 142, manage.y + 14, 390, 54), 32f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddPresetRow(Transform parent, int index, bool selected, string icon, string title, string subtitle, bool locked)
    {
        RectInt row = PresetRowRect(index);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(
            parent,
            LayerRoot,
            $"Preset_{index}_Frame",
            selected ? "scn13_preset_row_selected_frame.png" : "scn13_preset_row_locked_frame.png",
            row,
            new Vector4(36f, 36f, 21f, 21f),
            selected ? SelectedPresetTint : SubtleChrome);
        RectInt safe = WarlineCaptureLayeredUiBuilderUtility.Inset(row, 36, 18);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Preset_{index}_Icon", icon, new RectInt(safe.x, safe.y, 110, safe.height), 92, 92, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Preset_{index}_Title", title, new RectInt(safe.x + 140, safe.y + 4, 390, 46), 37f, TextAlignmentOptions.Left, selected ? TextMain : TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Preset_{index}_Subtitle", subtitle, new RectInt(safe.x + 140, safe.y + 52, 390, 38), 27f, TextAlignmentOptions.Left, TextMuted);
        if (selected)
            WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Preset_{index}_SelectedDot", "scn13_selected_status_dot.png", new RectInt(row.x + row.width - 82, row.y + 38, 52, 52), 38, 38, Color.white);
        if (locked)
            WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Preset_{index}_Lock", "scn13_icon_lock.png", new RectInt(row.x + row.width - 95, row.y + 38, 58, 58), 46, 46, DisabledText);
    }

    private static void AddOperationPreview(Transform parent)
    {
        RectInt rect = PreviewRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Preview_Frame", "scn13_operation_preview_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Preview_Title", "OPERATION PREVIEW", new RectInt(rect.x + 82, rect.y + 26, 650, 54), 36f, TextAlignmentOptions.Left, TextMuted);

        RectInt art = new(rect.x + 34, rect.y + 92, rect.width - 68, rect.height - 210);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Preview_Art", "scn13_operation_preview_art_wide.png", art, Color.white);
        AddPreviewTacticalOverlays(parent, art);
        AddMapMarkers(parent, art);

        RectInt footer = new(rect.x + 38, rect.y + rect.height - 116, rect.width - 76, 86);
        AddPreviewStat(parent, "Map", "scn13_icon_map_pin.png", "MAP", "Desert Outpost", new RectInt(footer.x, footer.y, 435, footer.height));
        AddPreviewStat(parent, "Seed", "scn13_icon_seed_dice.png", "SEED", "104729", new RectInt(footer.x + 500, footer.y, 360, footer.height));
        AddPreviewStat(parent, "Intel", "scn13_icon_intel_eye.png", "INTEL REVEAL", "ON", new RectInt(footer.x + 905, footer.y, 390, footer.height));
        AddPreviewStat(parent, "Risk", "scn13_icon_civilian_group.png", "CIVILIAN RISK", "MED", new RectInt(footer.x + 1320, footer.y, 390, footer.height));
    }

    private static void AddMapMarkers(Transform parent, RectInt art)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Marker_Warning_A", "scn13_marker_hostile_intel_diamond.png", new RectInt(art.x + 240, art.y + 245, 70, 70), 52, 52, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Marker_Warning_B", "scn13_marker_warning_triangle.png", new RectInt(art.x + 624, art.y + 348, 72, 72), 56, 56, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Marker_Target", "scn13_marker_objective_target.png", new RectInt(art.x + 1158, art.y + 368, 94, 94), 82, 82, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Marker_Deployment", "scn13_marker_deployment_flag.png", new RectInt(art.x + 1388, art.y + 522, 132, 126), 102, 102, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Marker_Zone", "scn13_marker_deployment_zone_circle.png", new RectInt(art.x + 1258, art.y + 598, 222, 130), 198, 108, new Color(1f, 1f, 1f, 0.88f));
    }

    private static void AddPreviewTacticalOverlays(Transform parent, RectInt art)
    {
        Color route = new Color(0.95f, 0.74f, 0.20f, 0.26f);
        Color routeDim = new Color(0.78f, 0.70f, 0.28f, 0.20f);
        Color scan = new Color(0.86f, 0.78f, 0.34f, 0.26f);
        Color deploy = new Color(0.60f, 0.88f, 0.46f, 0.28f);

        AddPreviewOverlay(parent, "Preview_Path_A", "scn13_marker_path_segment.png", new RectInt(art.x + 260, art.y + 292, 270, 240), 232, 205, routeDim, -12f);
        AddPreviewOverlay(parent, "Preview_Path_B", "scn13_marker_path_segment.png", new RectInt(art.x + 742, art.y + 282, 300, 250), 248, 218, route, 13f);
        AddPreviewOverlay(parent, "Preview_Path_C", "scn13_marker_path_segment.png", new RectInt(art.x + 1120, art.y + 458, 270, 236), 232, 205, routeDim, 10f);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Scan_A", "scn13_marker_scan_ping.png", new RectInt(art.x + 430, art.y + 220, 190, 98), 168, 82, scan);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Scan_B", "scn13_marker_scan_ping.png", new RectInt(art.x + 940, art.y + 240, 190, 98), 168, 82, scan);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Scan_C", "scn13_marker_scan_ping.png", new RectInt(art.x + 1365, art.y + 490, 196, 104), 172, 88, deploy);

        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Patrol_A", "scn13_marker_patrol_route_ring.png", new RectInt(art.x + 340, art.y + 430, 218, 160), 184, 140, scan);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Patrol_B", "scn13_marker_patrol_route_ring.png", new RectInt(art.x + 1245, art.y + 545, 238, 178), 204, 160, deploy);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Preview_Camera_Brackets", "scn13_marker_camera_brackets.png", new RectInt(art.x + 1132, art.y + 326, 138, 126), 112, 100, new Color(0.95f, 0.78f, 0.30f, 0.34f));
    }

    private static void AddPreviewOverlay(Transform parent, string name, string spriteName, RectInt slot, int maxWidth, int maxHeight, Color color, float rotation)
    {
        Image image = WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, name, spriteName, slot, maxWidth, maxHeight, color);
        image.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private static void AddPreviewStat(Transform parent, string name, string icon, string label, string value, RectInt slot)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Preview_{name}_Icon", icon, new RectInt(slot.x, slot.y + 6, 82, 74), 62, 62, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Preview_{name}_Label", label, new RectInt(slot.x + 105, slot.y + 6, slot.width - 110, 32), 24f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Preview_{name}_Value", value, new RectInt(slot.x + 105, slot.y + 38, slot.width - 110, 40), 28f, TextAlignmentOptions.Left, name == "Risk" ? GoldText : new Color32(155, 168, 76, 255));
    }

    private static void AddRulesPanel(Transform parent)
    {
        RectInt rect = RulesRect();
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Rules_Frame", "scn13_rules_panel_frame.png", rect, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rules_Title", "OPERATION RULES", new RectInt(rect.x + 70, rect.y + 28, 500, 52), 38f, TextAlignmentOptions.Left, TextMuted);

        AddDropdownRule(parent, 0, "scn13_icon_enemy_type.png", "ENEMY TYPE", "Balanced");
        AddStepperRule(parent, 1, "scn13_icon_enemy_count.png", "ENEMY COUNT", "1");
        AddDropdownRule(parent, 2, "scn13_icon_difficulty_bars.png", "DIFFICULTY", "Normal");
        AddDropdownRule(parent, 3, "scn13_icon_starting_credits.png", "STARTING CREDITS", "Normal");
        AddDropdownRule(parent, 4, "scn13_icon_income_chart.png", "INCOME", "1.0x");
        AddDropdownRule(parent, 5, "scn13_icon_build_speed_gear.png", "BUILD SPEED", "Normal");
        AddDropdownRule(parent, 6, "scn13_icon_production_factory.png", "PRODUCTION SPEED", "Normal");
        AddDropdownRule(parent, 7, "scn13_icon_aggression_skull.png", "AGGRESSION", "Balanced");
        AddDropdownRule(parent, 8, "scn13_icon_expansion_arrows.png", "EXPANSION", "Normal");
        AddDropdownRule(parent, 9, "scn13_icon_win_condition_target.png", "WIN CONDITION", "Destroy All Enemies");
        AddLockedFogRule(parent);
    }

    private static void AddDropdownRule(Transform parent, int index, string icon, string label, string value)
    {
        RectInt row = RuleRowRect(index);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"Rule_{index}_Frame", "scn13_rule_row_frame.png", row, false, Color.white);
        RectInt safe = WarlineCaptureLayeredUiBuilderUtility.Inset(row, 22, 8);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Rule_{index}_Icon", icon, new RectInt(safe.x, safe.y, 68, safe.height), 50, 50, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rule_{index}_Label", label, new RectInt(safe.x + 82, safe.y + 8, 350, safe.height - 16), 27f, TextAlignmentOptions.Left, TextMuted);
        RectInt valueFrame = new(row.x + row.width - 610, row.y + 8, 555, row.height - 16);
        AddRuleControlBacking(parent, $"Rule_{index}_ValueBacking", row, valueFrame);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, $"Rule_{index}_ValueFrame", "scn13_stepper_value_frame.png", valueFrame, new Vector4(31f, 31f, 19f, 19f), DropdownChrome);
        AddDropdownValue(parent, index, value, valueFrame);
    }

    private static void AddDropdownValue(Transform parent, int index, string value, RectInt valueFrame)
    {
        float valueFontSize = value.Length > 15 ? 30f : 34f;
        RectInt textRect = new(valueFrame.x + 50, valueFrame.y + 6, valueFrame.width - 142, valueFrame.height - 12);
        RectInt shadowRect = new(textRect.x + 2, textRect.y + 2, textRect.width, textRect.height);
        TMP_Text shadow = WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rule_{index}_ValueShadow", value, shadowRect, valueFontSize, TextAlignmentOptions.Midline, TextShadow);
        shadow.fontStyle = FontStyles.Normal;

        TMP_Text text = WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rule_{index}_Value", value, textRect, valueFontSize, TextAlignmentOptions.Midline, DropdownText);
        text.fontStyle = FontStyles.Normal;
        text.characterSpacing = 0f;

        RectInt chevron = new(valueFrame.x + valueFrame.width - 82, valueFrame.y + 17, 58, valueFrame.height - 34);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Rule_{index}_Chevron", "scn13_dropdown_chevron.png", chevron, 38, 30, DropdownText);
    }

    private static void AddRuleControlBacking(Transform parent, string name, RectInt row, RectInt control)
    {
        RectInt scrim = new(control.x - 12, row.y + 12, control.width + 24, row.height - 24);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, name, scrim, DropdownBacking);
    }

    private static void AddStepperRule(Transform parent, int index, string icon, string label, string value)
    {
        RectInt row = RuleRowRect(index);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, $"Rule_{index}_Frame", "scn13_rule_row_frame.png", row, false, Color.white);
        RectInt safe = WarlineCaptureLayeredUiBuilderUtility.Inset(row, 22, 8);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Rule_{index}_Icon", icon, new RectInt(safe.x, safe.y, 68, safe.height), 50, 50, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Rule_{index}_Label", label, new RectInt(safe.x + 82, safe.y + 8, 350, safe.height - 16), 27f, TextAlignmentOptions.Left, TextMuted);
        RectInt minus = new(row.x + row.width - 470, row.y + 12, 88, row.height - 24);
        RectInt count = new(minus.xMax + 18, row.y + 12, 220, row.height - 24);
        RectInt plus = new(count.xMax + 18, row.y + 12, 88, row.height - 24);
        AddRuleControlBacking(parent, "EnemyCount_MinusBacking", row, minus);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "EnemyCount_MinusFrame", "scn13_stepper_minus_frame.png", minus, new Vector4(24f, 24f, 19f, 19f), DropdownChrome);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "EnemyCount_MinusIcon", "scn13_stepper_minus_icon.png", minus, 42, 20, TextMuted);
        AddRuleControlBacking(parent, "EnemyCount_ValueBacking", row, count);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "EnemyCount_ValueFrame", "scn13_stepper_value_frame.png", count, new Vector4(31f, 31f, 19f, 19f), DropdownChrome);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "EnemyCount_Value", value, count, 30f, TextAlignmentOptions.Center, TextMuted);
        AddRuleControlBacking(parent, "EnemyCount_PlusBacking", row, plus);
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "EnemyCount_PlusFrame", "scn13_stepper_plus_frame.png", plus, new Vector4(24f, 24f, 19f, 19f), DropdownChrome);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "EnemyCount_PlusIcon", "scn13_stepper_plus_icon.png", plus, 42, 42, TextMuted);
    }

    private static void AddLockedFogRule(Transform parent)
    {
        RectInt row = RuleRowRect(10);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Rule_Fog_Frame", "scn13_rule_row_frame.png", row, false, Color.white);
        RectInt safe = WarlineCaptureLayeredUiBuilderUtility.Inset(row, 22, 8);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rule_Fog_Icon", "scn13_icon_fog_hidden_eye.png", new RectInt(safe.x, safe.y, 68, safe.height), 50, 50, DisabledText);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rule_Fog_Label", "FOG OF WAR\nLocked", new RectInt(safe.x + 82, safe.y + 2, 280, safe.height - 4), 25f, TextAlignmentOptions.Left, DisabledText, true);
        RectInt chip = new(row.x + row.width - 435, row.y + 12, 380, row.height - 24);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Rule_Fog_ChipFrame", "scn13_locked_reason_chip_frame.png", chip, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Rule_Fog_Lock", "scn13_icon_lock.png", new RectInt(chip.x + 20, chip.y + 8, 44, chip.height - 16), 32, 32, DisabledText);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Rule_Fog_Reason", "Requires Fog Runtime", new RectInt(chip.x + 82, chip.y + 8, chip.width - 98, chip.height - 16), 22f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddBottomActions(Transform parent)
    {
        RectInt info = InfoRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "Info_Frame", "scn13_info_panel_frame.png", info, new Vector4(48f, 48f, 34f, 34f), SubtleChrome);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Info_Icon", "scn13_icon_info_circle.png", new RectInt(info.x + 44, info.y + 34, 88, 88), 72, 72, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Info_Text", "Tune the enemy force, economy, and mission\nrules before deployment.", new RectInt(info.x + 160, info.y + 34, 900, 96), 29f, TextAlignmentOptions.Left, TextMuted, true);

        RectInt reset = ResetRect();
        RectInt randomize = RandomizeRect();
        RectInt secondary = new(reset.x, reset.y, randomize.xMax - reset.x, reset.height);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "SecondaryActions_Frame", "scn13_secondary_action_button_frame.png", secondary, false, Color.white);
        AddSecondaryActionContent(parent, reset, "scn13_icon_reset_arrow.png", "RESET");
        AddSecondaryActionContent(parent, randomize, "scn13_icon_seed_dice.png", "RANDOMIZE SEED");

        RectInt launch = LaunchRect();
        WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(parent, LayerRoot, "Launch_Frame", "scn13_launch_cta_frame.png", launch, new Vector4(54f, 54f, 31f, 31f), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Launch_Text", "LAUNCH MISSION", new RectInt(launch.x + 125, launch.y + 46, launch.width - 330, launch.height - 92), 66f, TextAlignmentOptions.Center, Color.black);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Launch_Chevrons", "scn13_launch_chevrons.png", new RectInt(launch.x + launch.width - 245, launch.y + 48, 160, launch.height - 96), 138, 90, new Color32(93, 73, 22, 255));
    }

    private static void AddSecondaryActionContent(Transform parent, RectInt rect, string icon, string label)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"{label}_Icon", icon, new RectInt(rect.x + 88, rect.y + 32, 100, rect.height - 64), 76, 76, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{label}_Text", label, new RectInt(rect.x + 210, rect.y + 32, rect.width - 250, rect.height - 64), 38f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddHitZones(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "Title_Back", new RectInt(TitleRect().x, TitleRect().y, 170, TitleRect().height));
        for (int i = 0; i < 5; i++)
            WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, $"Preset_{i}", PresetRowRect(i));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "ManagePresets", new RectInt(PresetRailRect().x + 48, PresetRailRect().y + PresetRailRect().height - 130, 610, 82));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "Reset", ResetRect());
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "RandomizeSeed", RandomizeRect());
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "LaunchMission", LaunchRect());
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "Inbox", new RectInt(HeaderActionsRect().x + 58, HeaderActionsRect().y + 40, 205, 134));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "Settings", new RectInt(HeaderActionsRect().x + 318, HeaderActionsRect().y + 36, 190, 142));
    }

    private static RectInt HeaderLogoRect() => new(0, 0, 1060, 240);
    private static RectInt HeaderResourceRect() => new(2040, 0, 1970, 235);
    private static RectInt HeaderActionsRect() => new(4150, 0, 625, 230);
    private static RectInt TitleRect() => new(96, 260, 1100, 205);
    private static RectInt PresetRailRect() => new(96, 474, 1015, 1234);
    private static RectInt PreviewRect() => new(1145, 474, 2245, 1234);
    private static RectInt RulesRect() => new(3435, 474, 1230, 1234);
    private static RectInt InfoRect() => new(122, 1835, 1240, 210);
    private static RectInt ResetRect() => new(1560, 1858, 760, 190);
    private static RectInt RandomizeRect() => new(2388, 1858, 820, 190);
    private static RectInt LaunchRect() => new(3380, 1802, 1255, 260);

    private static RectInt PresetRowRect(int index)
    {
        RectInt rail = PresetRailRect();
        return new RectInt(rail.x + 42, rail.y + 120 + index * 198, 870, 158);
    }

    private static RectInt RuleRowRect(int index)
    {
        RectInt rules = RulesRect();
        return new RectInt(rules.x + 50, rules.y + 96 + index * 94, rules.width - 100, 90);
    }
}
#endif
