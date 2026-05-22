#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn08RtsBattleHudSceneBuilder
{
    private const int CanvasWidth = 2400;
    private const int CanvasHeight = 1080;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_SCN08_RTSBattleHUD_TargetLock.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN08_RTSBattleHUD_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/SCN08_RTSBattleHUD_TargetLock_V07_2400x1080.png";

    private static Color TextMain => new Color32(224, 214, 184, 255);
    private static Color TextMuted => new Color32(172, 160, 128, 255);
    private static Color Gold => new Color32(232, 165, 38, 255);
    private static Color Green => new Color32(159, 184, 70, 255);
    private static Color Red => new Color32(210, 82, 54, 255);
    private static Color DarkFill => new Color32(13, 15, 12, 218);

    [MenuItem("WarlineCapture/Design/SCN-08 Build RTS Battle HUD Target Lock")]
    public static void BuildScene()
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN08_RTSBattleHUD_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = "Screen_SCN08_RTSBattleHUD_TargetLock";
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-08] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-08 Capture RTS Battle HUD Target Lock")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[SCN-08] Captured {CapturePath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_SCN08_RTSBattleHUD_TargetLock", null);
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

        GameObject visualRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN08_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(visualRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(visualRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Battlefield_NoUi", "scn08_battlefield_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        AddWorldMarkers(parent);
        AddTopHud(parent);
        AddObjectivePanel(parent);
        AddSelectedEntityPanel(parent);
        AddSquadTray(parent);
        AddCommandBar(parent);
        AddRightQuickPanel(parent);
        AddMiniMap(parent);
        AddInvalidToast(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Objectives", ObjectiveRect()),
            new WarlineUiRect("Selected", SelectedRect()),
            new WarlineUiRect("SquadTray", SquadTrayRect()),
            new WarlineUiRect("CommandBar", CommandBarRect()),
            new WarlineUiRect("MiniMap", MiniMapRect()),
            new WarlineUiRect("RightQuick", RightQuickRect()));
    }

    private static void AddTopHud(Transform parent)
    {
        RectInt command = new(1012, 14, 350, 82);
        AddFrame(parent, "CommandMode_Frame", "scn08_command_mode_banner_frame.png", command);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "CommandMode_Chevrons", "scn08_current_order_chevrons.png", new RectInt(command.x + 28, command.y + 16, 70, 50), 62, 38, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "CommandMode_Title", "MOVE ORDER", new RectInt(command.x + 110, command.y + 18, 205, 36), 26f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "CommandMode_Subtitle", "Rifle Squad", new RectInt(command.x + 114, command.y + 50, 170, 24), 16f, TextAlignmentOptions.Left, TextMuted);

        RectInt resource = new(1394, 14, 860, 80);
        AddFrame(parent, "ResourceStrip_Frame", "scn08_resource_strip_frame.png", resource);
        AddResource(parent, "Credits", "scn08_resource_credits_coin.png", "Credits", "187,540", new RectInt(resource.x + 25, resource.y + 11, 195, 58), Gold);
        AddResource(parent, "Fuel", "scn08_resource_fuel_can.png", "Fuel", "2,860", new RectInt(resource.x + 250, resource.y + 11, 165, 58), Green);
        AddResource(parent, "Supply", "scn08_resource_supply_crate.png", "Supply", "92 / 120", new RectInt(resource.x + 438, resource.y + 11, 195, 58), TextMain);
        AddResource(parent, "CivilianRisk", "scn08_icon_civilian_group.png", "Civilian Risk", "MED", new RectInt(resource.x + 650, resource.y + 11, 198, 58), Gold);

        RectInt menu = new(2268, 16, 82, 76);
        AddFrame(parent, "MenuButton_Frame", "scn08_top_icon_button_frame_a.png", menu);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MenuButton_Icon", "scn08_icon_menu_list.png", new RectInt(menu.x + 16, menu.y + 15, 50, 48), 42, 42, TextMain);

        RectInt threat = new(1818, 112, 552, 74);
        AddFrame(parent, "ThreatJump_Frame", "scn08_jump_button_frame.png", threat);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "ThreatJump_Icon", "scn08_icon_threat_warning.png", new RectInt(threat.x + 36, threat.y + 14, 54, 46), 38, 38, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ThreatJump_Text", "Hostile cell spotted", new RectInt(threat.x + 104, threat.y + 20, 282, 34), 21f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "ThreatJump_Action", "JUMP", new RectInt(threat.x + 420, threat.y + 17, 98, 38), 24f, TextAlignmentOptions.Center, TextMain);
    }

    private static void AddResource(Transform parent, string name, string icon, string label, string value, RectInt rect, Color valueColor)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"{name}_Icon", icon, new RectInt(rect.x, rect.y, 62, rect.height), 52, 52, Color.white);
        float labelSize = name == "CivilianRisk" ? 14f : 16f;
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{name}_Label", label, new RectInt(rect.x + 72, rect.y + 1, rect.width - 74, 22), labelSize, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{name}_Value", value, new RectInt(rect.x + 72, rect.y + 24, rect.width - 74, 34), 25f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddObjectivePanel(Transform parent)
    {
        RectInt rect = ObjectiveRect();
        AddFrame(parent, "Objective_Frame", "scn08_objective_panel_frame.png", rect);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Objective_Title", "OBJECTIVES", new RectInt(rect.x + 34, rect.y + 25, 300, 34), 26f, TextAlignmentOptions.Left, TextMain);
        AddObjectiveRow(parent, rect, 0, "scn08_icon_checkbox_empty.png", "Neutralize hostile patrol");
        AddObjectiveRow(parent, rect, 1, "scn08_icon_checkbox_checked.png", "Protect civilians");
        AddObjectiveRow(parent, rect, 2, "scn08_icon_objective_star.png", "Keep losses low");
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Objective_Elapsed", "Elapsed: 07:42", new RectInt(rect.x + 34, rect.y + 222, 210, 28), 20f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddObjectiveRow(Transform parent, RectInt panel, int index, string icon, string text)
    {
        int y = panel.y + 80 + index * 50;
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Objective_{index}_Icon", icon, new RectInt(panel.x + 32, y - 6, 34, 36), 28, 28, index == 0 ? TextMuted : Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Objective_{index}_Text", text, new RectInt(panel.x + 88, y - 3, 285, 30), 20f, TextAlignmentOptions.Left, TextMain);
    }

    private static void AddSelectedEntityPanel(Transform parent)
    {
        RectInt rect = SelectedRect();
        AddFrame(parent, "Selected_Frame", "scn08_selected_entity_panel_frame.png", rect);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Selected_Badge", "scn08_icon_shield_rank_badge.png", new RectInt(rect.x + 28, rect.y + 36, 58, 76), 48, 64, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Selected_Title", "RIFLE SQUAD", new RectInt(rect.x + 98, rect.y + 35, 240, 38), 28f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Selected_Role", "Squad  -  Anti-Infantry", new RectInt(rect.x + 98, rect.y + 75, 230, 28), 18f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Selected_Portrait", "scn08_portrait_rifle_squad.png", new RectInt(rect.x + 40, rect.y + 118, 294, 160), new Color(0.80f, 0.78f, 0.70f, 0.94f));
        AddHealth(parent, "Selected_Health", new RectInt(rect.x + 42, rect.y + 304, 288, 34), "120/120");
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Selected_OrderLabel", "ORDER", new RectInt(rect.x + 42, rect.y + 340, 120, 24), 16f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Selected_OrderValue", "Moving", new RectInt(rect.x + 42, rect.y + 362, 170, 28), 20f, TextAlignmentOptions.Left, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Selected_OrderChevron", "scn08_current_order_chevrons.png", new RectInt(rect.x + 300, rect.y + 353, 42, 42), 32, 26, Green);
        AddAbility(parent, rect.x + 45, rect.y + 416, "scn08_command_scan_radar.png", "SCAN");
        AddAbility(parent, rect.x + 146, rect.y + 416, "scn08_command_hold_shield.png", "HOLD");
        AddAbility(parent, rect.x + 247, rect.y + 416, "scn08_command_board_vehicle.png", "BOARD");
    }

    private static void AddSquadTray(Transform parent)
    {
        RectInt tray = SquadTrayRect();
        AddFrame(parent, "SquadTray_Frame", "scn08_squad_tray_frame.png", tray);
        AddSquadCard(parent, new RectInt(tray.x + 12, tray.y + 12, 196, 222), "1", "Rifle Squad", "scn08_portrait_rifle_squad.png", "120/120", true);
        AddSquadCard(parent, new RectInt(tray.x + 220, tray.y + 16, 196, 218), "2", "Fast APC", "scn08_portrait_fast_apc.png", "240/240", false);
        AddSquadCard(parent, new RectInt(tray.x + 428, tray.y + 16, 196, 218), "3", "Recon Drone", "scn08_portrait_recon_drone.png", "80/80", false);
        AddSquadCard(parent, new RectInt(tray.x + 636, tray.y + 16, 196, 218), "4", "Bomb Suit", "scn08_portrait_bomb_suit.png", "100/100", false);
    }

    private static void AddSquadCard(Transform parent, RectInt rect, string number, string title, string portrait, string hp, bool selected)
    {
        AddFrameTinted(parent, $"Squad_{number}_Frame", selected ? "scn08_squad_card_selected_frame.png" : "scn08_squad_card_normal_frame.png", rect, selected ? new Color(0.92f, 0.86f, 0.48f, 0.88f) : Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Squad_{number}_Number", number, new RectInt(rect.x + 22, rect.y + 18, 28, 28), 16f, TextAlignmentOptions.Center, selected ? TextMain : Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Squad_{number}_Title", title, new RectInt(rect.x + 60, rect.y + 18, 125, 28), 16f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, $"Squad_{number}_Portrait", portrait, new RectInt(rect.x + 26, rect.y + 60, 146, 78), new Color(0.84f, 0.82f, 0.74f, 0.94f));
        AddHealth(parent, $"Squad_{number}_Health", new RectInt(rect.x + 25, rect.y + 152, 150, 28), hp);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Squad_{number}_Segments", "scn08_status_segment_strip.png", new RectInt(rect.x + 24, rect.y + 185, 150, 22), 134, 15, selected ? Green : TextMuted);
    }

    private static void AddCommandBar(Transform parent)
    {
        RectInt rect = CommandBarRect();
        AddFrame(parent, "CommandRail_Frame", "scn08_command_bar_rail_frame.png", rect);
        int x = rect.x + 40;
        const int step = 104;
        AddCommandButton(parent, x, rect.y + 29, "scn08_command_select_cursor.png", "SELECT", false);
        AddCommandButton(parent, x + step, rect.y + 29, "scn08_command_move_chevrons.png", "MOVE", true);
        AddCommandButton(parent, x + step * 2, rect.y + 29, "scn08_command_attack_crosshair.png", "ATTACK", false);
        AddCommandButton(parent, x + step * 3, rect.y + 29, "scn08_command_hold_shield.png", "HOLD", false);
        AddCommandButton(parent, x + step * 4, rect.y + 29, "scn08_command_stop_hand.png", "STOP", false);
        AddCommandButton(parent, x + step * 5, rect.y + 29, "scn08_icon_build_tools.png", "BUILD", false);
        AddCommandButton(parent, x + step * 6, rect.y + 29, "scn08_command_scan_radar.png", "SCAN", false);
        AddCommandButton(parent, x + step * 7, rect.y + 29, "scn08_icon_support_parachute.png", "SUPPORT", false);
    }

    private static void AddCommandButton(Transform parent, int x, int y, string icon, string label, bool selected)
    {
        RectInt rect = new(x, y, 92, 132);
        AddFrame(parent, $"Command_{label}_Frame", selected ? "scn08_command_button_selected_frame.png" : "scn08_command_button_normal_frame.png", rect);
        Color commandColor = selected ? Gold : TextMain;
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Command_{label}_Icon", icon, new RectInt(rect.x + 18, rect.y + 16, 56, 62), 52, 52, commandColor);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Command_{label}_Text", label, new RectInt(rect.x + 5, rect.y + 92, 82, 28), 17f, TextAlignmentOptions.Center, selected ? Gold : TextMuted);
    }

    private static void AddRightQuickPanel(Transform parent)
    {
        RectInt rect = RightQuickRect();
        AddFrame(parent, "RightQuick_Frame", "scn08_right_quick_panel_frame.png", rect);
        AddQuickButton(parent, rect.x + 17, rect.y + 34, "scn08_icon_pause.png", string.Empty);
        AddQuickButton(parent, rect.x + 17, rect.y + 126, "scn08_icon_settings_gear.png", string.Empty);
        AddQuickButton(parent, rect.x + 17, rect.y + 218, "scn08_icon_build_tools.png", "BUILD");
        AddQuickButton(parent, rect.x + 17, rect.y + 316, "scn08_icon_support_parachute.png", "SUPPORT");
        AddZoomButton(parent, rect.x + 24, 812, "scn08_minimap_zoom_plus_icon.png");
        AddZoomButton(parent, rect.x + 24, 895, "scn08_minimap_zoom_minus_icon.png");
        AddZoomButton(parent, rect.x + 24, 978, "scn08_minimap_focus_target_icon.png");
    }

    private static void AddQuickButton(Transform parent, int x, int y, string icon, string label)
    {
        AddFrame(parent, $"Quick_{icon}_Frame", "scn08_side_quick_button_frame.png", new RectInt(x, y, 76, 76));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Quick_{icon}_Icon", icon, new RectInt(x + 13, y + 8, 50, 44), 40, 40, TextMain);
        if (!string.IsNullOrEmpty(label))
            WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Quick_{label}_Text", label, new RectInt(x - 2, y + 51, 80, 22), 12f, TextAlignmentOptions.Center, TextMuted);
    }

    private static void AddMiniMap(Transform parent)
    {
        RectInt rect = MiniMapRect();
        AddFrame(parent, "MiniMap_Frame", "scn08_minimap_panel_frame.png", rect);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "MiniMap_Content", "scn08_minimap_content.png", new RectInt(rect.x + 32, rect.y + 42, rect.width - 66, rect.height - 72), new Color(0.88f, 0.84f, 0.76f, 0.88f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MiniMap_Viewport", "scn08_marker_minimap_viewport_rect.png", new RectInt(rect.x + 174, rect.y + 184, 138, 84), 124, 58, new Color(1f, 1f, 1f, 0.76f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MiniMap_North", "scn08_minimap_north_arrow.png", new RectInt(rect.x + 222, rect.y + 18, 52, 52), 40, 40, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MiniMap_Obj", "scn08_marker_objective_star_pin.png", new RectInt(rect.x + 385, rect.y + 215, 46, 62), 28, 50, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MiniMap_EnemyA", "scn08_marker_hostile_diamond.png", new RectInt(rect.x + 225, rect.y + 94, 46, 56), 30, 40, Red);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "MiniMap_EnemyB", "scn08_icon_hostile_diamond.png", new RectInt(rect.x + 331, rect.y + 101, 40, 40), 28, 28, Red);
    }

    private static void AddInvalidToast(Transform parent)
    {
        RectInt rect = new(910, 756, 430, 72);
        AddFrame(parent, "InvalidToast_Frame", "scn08_invalid_command_toast_frame.png", rect);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "InvalidToast_Icon", "scn08_icon_invalid_warning.png", new RectInt(rect.x + 40, rect.y + 13, 52, 48), 40, 40, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "InvalidToast_Text", "Blocked: civilian zone", new RectInt(rect.x + 112, rect.y + 21, 245, 32), 20f, TextAlignmentOptions.Left, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "InvalidToast_Chevrons", "scn08_current_order_chevrons.png", new RectInt(rect.x + 330, rect.y + 22, 66, 30), 55, 24, Gold);
    }

    private static void AddWorldMarkers(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_Path", "scn08_marker_path_line.png", new RectInt(1118, 408, 420, 208), 388, 84, new Color(0.65f, 0.85f, 0.34f, 0.68f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_SelectionRing", "scn08_marker_selection_ring.png", new RectInt(990, 540, 318, 128), 296, 88, new Color(0.65f, 0.85f, 0.34f, 0.72f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_MoveDestination", "scn08_marker_move_destination.png", new RectInt(1288, 405, 120, 88), 92, 62, new Color(0.65f, 0.85f, 0.34f, 0.82f));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_Objective", "scn08_marker_objective_star_pin.png", new RectInt(650, 235, 90, 150), 76, 126, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_EnemyA", "scn08_marker_hostile_diamond.png", new RectInt(805, 242, 82, 110), 64, 86, Red);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_EnemyB", "scn08_marker_hostile_diamond.png", new RectInt(1535, 225, 82, 110), 64, 86, Red);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_CivilianRisk", "scn08_marker_civilian_risk_zone.png", new RectInt(1760, 476, 150, 128), 132, 100, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "World_Focus", "scn08_marker_command_focus_brackets.png", new RectInt(1004, 546, 272, 110), 232, 72, new Color(0.65f, 0.85f, 0.48f, 0.50f));
    }

    private static void AddAbility(Transform parent, int x, int y, string icon, string label)
    {
        AddFrame(parent, $"Ability_{label}_Frame", "scn08_ability_chip_frame.png", new RectInt(x, y, 82, 82));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Ability_{label}_Icon", icon, new RectInt(x + 16, y + 8, 50, 42), 40, 38, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"Ability_{label}_Text", label, new RectInt(x + 4, y + 52, 74, 21), 13f, TextAlignmentOptions.Center, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Ability_{label}_Segments", "scn08_status_segment_strip.png", new RectInt(x + 15, y + 70, 52, 10), 48, 8, Green);
    }

    private static void AddHealth(Transform parent, string name, RectInt rect, string value)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"{name}_Bar", "scn08_health_bar_frame.png", new RectInt(rect.x, rect.y, rect.width - 68, rect.height), rect.width - 76, 20, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, $"{name}_Text", value, new RectInt(rect.x + rect.width - 70, rect.y + 2, 70, 25), 17f, TextAlignmentOptions.Right, TextMain);
    }

    private static void AddZoomButton(Transform parent, int x, int y, string icon)
    {
        AddFrame(parent, $"Zoom_{icon}_Frame", "scn08_minimap_zoom_button_frame.png", new RectInt(x, y, 72, 72));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, $"Zoom_{icon}_Icon", icon, new RectInt(x + 15, y + 15, 42, 42), 36, 36, TextMain);
    }

    private static void AddFrame(Transform parent, string name, string spriteName, RectInt rect)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 10, 10), DarkFill);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, name, spriteName, rect, false, Color.white);
    }

    private static void AddFrameTinted(Transform parent, string name, string spriteName, RectInt rect, Color tint)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, 10, 10), DarkFill);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, name, spriteName, rect, false, tint);
    }

    private static RectInt ObjectiveRect() => new(9, 14, 428, 260);
    private static RectInt SelectedRect() => new(10, 292, 382, 516);
    private static RectInt SquadTrayRect() => new(8, 828, 850, 248);
    private static RectInt CommandBarRect() => new(862, 875, 900, 190);
    private static RectInt MiniMapRect() => new(1788, 628, 490, 382);
    private static RectInt RightQuickRect() => new(2270, 190, 118, 420);
}
#endif
