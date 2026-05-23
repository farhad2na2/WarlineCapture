#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn19ArmorySceneBuilder
{
    private const int CanvasWidth = 2400;
    private const int CanvasHeight = 1080;
    private const string SourceLayerRoot = "Design/VisualLockLayered/SCN-19_Armory/layers";
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Armory.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN19_Armory_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/SCN19_Armory_OneGo_2400x1080.png";
    private const string ButtonAnimatorControllerPath = "Assets/Game/Animations/UI/WarlineCaptureButtonStates.overrideController";

    private static readonly Color TextMain = new Color32(232, 226, 202, 255);
    private static readonly Color TextMuted = new Color32(188, 178, 136, 255);
    private static readonly Color Gold = new Color32(238, 172, 43, 255);
    private static readonly Color Olive = new Color32(160, 176, 72, 255);
    private static readonly Color Blue = new Color32(96, 172, 207, 255);
    private static readonly Color PanelWash = new Color(0f, 0f, 0f, 0.14f);

    private static readonly string[] LayerFiles =
    {
        "scn19_background_21x9_no_ui.png",
        "scn19_header_logo_panel_bg.png",
        "scn19_header_resource_panel_bg.png",
        "scn19_header_right_actions_bg.png",
        "scn19_title_back_panel_frame.png",
        "scn19_category_button_selected_frame.png",
        "scn19_category_button_default_frame.png",
        "scn19_dropdown_frame.png",
        "scn19_roster_card_selected_frame.png",
        "scn19_roster_card_default_frame.png",
        "scn19_roster_card_locked_frame.png",
        "scn19_inspection_panel_frame.png",
        "scn19_bottom_tab_selected_frame.png",
        "scn19_bottom_tab_default_frame.png",
        "scn19_cta_primary_gold_frame.png",
        "scn19_cta_secondary_dark_frame.png",
        "scn19_cta_disabled_frame.png",
        "scn19_progress_meter_empty_frame.png",
        "scn19_small_status_chip_frame.png",
        "scn19_small_counter_chip_frame.png",
        "scn19_route_breadcrumb_strip_frame.png",
        "scn19_comms_status_panel_frame.png",
        "scn19_icon_back_arrow.png",
        "scn19_icon_armory_crossed_weapons.png",
        "scn19_icon_units_group.png",
        "scn19_icon_vehicle_truck.png",
        "scn19_icon_aircraft_helicopter.png",
        "scn19_icon_buildings.png",
        "scn19_icon_support_plus.png",
        "scn19_icon_upgrades_chevrons.png",
        "scn19_resource_credits_coin.png",
        "scn19_resource_supplies_crate.png",
        "scn19_resource_command_shield.png",
        "scn19_icon_inbox_envelope.png",
        "scn19_icon_settings_gear.png",
        "scn19_icon_dropdown_chevron.png",
        "scn19_badge_owned_checkmark.png",
        "scn19_badge_locked_padlock.png",
        "scn19_badge_upgrade_ready_chevrons.png",
        "scn19_icon_health_cross.png",
        "scn19_icon_damage_burst.png",
        "scn19_icon_range_reticle.png",
        "scn19_icon_speed_boot.png",
        "scn19_icon_move_runner.png",
        "scn19_icon_attack_reticle.png",
        "scn19_icon_hold_shield.png",
        "scn19_icon_patrol_chevrons.png",
        "scn19_icon_blueprint_parts.png",
        "scn19_icon_source_building.png",
        "scn19_selected_glow_strip.png",
        "scn19_progress_fill_gold_segment.png",
        "scn19_progress_fill_olive_segment.png",
        "scn19_icon_disabled_slash.png",
        "scn19_icon_comms_signal.png",
        "scn19_art_rifleman_male_ii.png",
        "scn19_art_marksman_male_i.png",
        "scn19_art_assault_breacher_female_ii.png",
        "scn19_art_field_commander.png",
        "scn19_art_cargo_truck.png",
        "scn19_art_canopy_truck.png",
        "scn19_art_attack_helicopter.png",
        "scn19_art_transport_helicopter.png",
        "scn19_art_oil_pump.png",
        "scn19_art_oil_refinery.png",
        "scn19_art_guard_tower.png",
        "scn19_art_ammunition_depot.png"
    };

    private static readonly string[] SlicedLayers =
    {
        "scn19_header_logo_panel_bg.png",
        "scn19_header_resource_panel_bg.png",
        "scn19_header_right_actions_bg.png",
        "scn19_title_back_panel_frame.png",
        "scn19_category_button_selected_frame.png",
        "scn19_category_button_default_frame.png",
        "scn19_dropdown_frame.png",
        "scn19_roster_card_selected_frame.png",
        "scn19_roster_card_default_frame.png",
        "scn19_roster_card_locked_frame.png",
        "scn19_inspection_panel_frame.png",
        "scn19_bottom_tab_selected_frame.png",
        "scn19_bottom_tab_default_frame.png",
        "scn19_cta_primary_gold_frame.png",
        "scn19_cta_secondary_dark_frame.png",
        "scn19_cta_disabled_frame.png",
        "scn19_progress_meter_empty_frame.png",
        "scn19_small_status_chip_frame.png",
        "scn19_small_counter_chip_frame.png",
        "scn19_route_breadcrumb_strip_frame.png",
        "scn19_comms_status_panel_frame.png"
    };

    private readonly struct RosterItem
    {
        public RosterItem(string objectName, string title, string role, string art, string icon, bool selected, bool locked, bool upgradeReady, int level)
        {
            ObjectName = objectName;
            Title = title;
            Role = role;
            Art = art;
            Icon = icon;
            Selected = selected;
            Locked = locked;
            UpgradeReady = upgradeReady;
            Level = level;
        }

        public string ObjectName { get; }
        public string Title { get; }
        public string Role { get; }
        public string Art { get; }
        public string Icon { get; }
        public bool Selected { get; }
        public bool Locked { get; }
        public bool UpgradeReady { get; }
        public int Level { get; }
    }

    private static readonly RosterItem[] RosterItems =
    {
        new("RiflemanCard", "RIFLEMAN MALE II", "INFANTRY", "scn19_art_rifleman_male_ii.png", "scn19_icon_units_group.png", true, false, false, 12),
        new("MarksmanCard", "MARKSMAN MALE I", "INFANTRY", "scn19_art_marksman_male_i.png", "scn19_icon_units_group.png", false, false, false, 9),
        new("AssaultBreacherCard", "ASSAULT BREACHER", "INFANTRY", "scn19_art_assault_breacher_female_ii.png", "scn19_icon_units_group.png", false, false, true, 11),
        new("FieldCommanderCard", "FIELD COMMANDER", "COMMAND", "scn19_art_field_commander.png", "scn19_resource_command_shield.png", false, false, false, 10),
        new("CargoTruckCard", "CARGO TRUCK", "VEHICLE", "scn19_art_cargo_truck.png", "scn19_icon_vehicle_truck.png", false, false, false, 8),
        new("CanopyTruckCard", "CANOPY TRUCK", "VEHICLE", "scn19_art_canopy_truck.png", "scn19_icon_vehicle_truck.png", false, false, false, 7),
        new("AttackHelicopterCard", "ATTACK HELICOPTER", "AIRCRAFT", "scn19_art_attack_helicopter.png", "scn19_icon_aircraft_helicopter.png", false, false, false, 10),
        new("TransportHelicopterCard", "TRANSPORT HELICOPTER", "AIRCRAFT", "scn19_art_transport_helicopter.png", "scn19_icon_aircraft_helicopter.png", false, true, false, 9),
        new("OilPumpCard", "OIL PUMP", "BUILDING", "scn19_art_oil_pump.png", "scn19_icon_buildings.png", false, false, false, 6),
        new("OilRefineryCard", "OIL REFINERY", "BUILDING", "scn19_art_oil_refinery.png", "scn19_icon_buildings.png", false, false, true, 7),
        new("GuardTowerCard", "GUARD TOWER", "BUILDING", "scn19_art_guard_tower.png", "scn19_icon_buildings.png", false, false, false, 5),
        new("AmmunitionDepotCard", "AMMUNITION DEPOT", "BUILDING", "scn19_art_ammunition_depot.png", "scn19_icon_buildings.png", false, true, false, 6)
    };

    [MenuItem("WarlineCapture/Design/SCN-19 Build Armory One-Go")]
    public static void BuildScene()
    {
        BuildPrefabAndScene(saveScene: true);
    }

    public static void BuildPrefabOnly()
    {
        BuildPrefabAndScene(saveScene: false);
    }

    [MenuItem("WarlineCapture/Design/SCN-19 Capture Armory One-Go")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, 2400, 1080, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[SCN-19] Captured {CapturePath}");
    }

    private static void BuildPrefabAndScene(bool saveScene)
    {
        CopyLayerAssetsToUnity();
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);
        EnsureSlicedImports();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        if (saveScene)
        {
            GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN19_Armory_Canvas", null);
            RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
            sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            sceneCanvasRect.localPosition = Vector3.zero;
            sceneCanvasRect.localScale = Vector3.one;

            Canvas canvas = sceneCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            sceneCanvas.AddComponent<GraphicRaycaster>();

            GameObject instance = UnityEngine.Object.Instantiate(prefabRoot, sceneCanvas.transform);
            instance.name = "Screen_Armory";
            WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
            UnityEngine.Object.DestroyImmediate(prefabRoot);

            WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
            Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
            canvas.worldCamera = camera;

            WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-19] Built prefab={PrefabPath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_Armory", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        WarlineCaptureScreenController controller = root.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(WarlineCaptureRoute.Armory);

        GameObject visualRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN19_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(visualRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(visualRoot.transform);

        GameObject hitRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN19_HitZones", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(hitRoot.GetComponent<RectTransform>());
        AddHitZones(hitRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        AddCover(parent, "ShellFill", "scn19_background_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "ReadabilityWash", new RectInt(0, 90, CanvasWidth, CanvasHeight - 90), PanelWash);

        AddHeader(parent);
        AddTitle(parent);
        AddCategoryRail(parent);
        AddRoster(parent);
        AddInspection(parent);
        AddBottomTabs(parent);
        AddFooter(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Title", TitleRect()),
            new WarlineUiRect("CategoryRail", CategoryRailRect()),
            new WarlineUiRect("Roster", RosterRect()),
            new WarlineUiRect("Inspection", InspectionRect()),
            new WarlineUiRect("BottomTabs", BottomTabsRect()),
            new WarlineUiRect("Comms", CommsRect()));
    }

    private static void AddHeader(Transform parent)
    {
        RectInt logo = HeaderLogoRect();
        RectInt resources = HeaderResourceRect();
        RectInt actions = HeaderActionsRect();

        AddSliced(parent, "HeaderBar/LogoPanel", "scn19_header_logo_panel_bg.png", logo, new Vector4(64, 42, 20, 20), Color.white);
        AddSliced(parent, "HeaderBar/ResourcePanel", "scn19_header_resource_panel_bg.png", resources, new Vector4(54, 54, 20, 20), Color.white);
        AddSliced(parent, "HeaderBar/ActionsPanel", "scn19_header_right_actions_bg.png", actions, new Vector4(42, 42, 18, 18), Color.white);

        AddText(parent, "HeaderBar/BrandText", "WARLINE", new RectInt(92, 20, 260, 45), 39f, TextAlignmentOptions.Left, TextMain);
        AddText(parent, "HeaderBar/BrandSubText", "CAPTURE", new RectInt(94, 62, 220, 30), 24f, TextAlignmentOptions.Left, Gold);
        AddFitted(parent, "HeaderBar/BrandMark", "scn19_resource_command_shield.png", new RectInt(26, 18, 58, 70), 54, 62, TextMain);

        AddHeaderResource(parent, "CreditsCounter", "scn19_resource_credits_coin.png", "Credits", "187,540", new RectInt(resources.x + 28, resources.y + 18, 250, 72), Gold);
        AddHeaderResource(parent, "SuppliesCounter", "scn19_resource_supplies_crate.png", "Supplies", "92,860", new RectInt(resources.x + 310, resources.y + 18, 270, 72), new Color32(164, 169, 108, 255));
        AddHeaderResource(parent, "CommandCounter", "scn19_resource_command_shield.png", "Command", "2,715", new RectInt(resources.x + 625, resources.y + 18, 230, 72), Blue);

        AddHeaderAction(parent, "InboxButton", "scn19_icon_inbox_envelope.png", new RectInt(actions.x + 36, actions.y + 14, 72, 58));
        AddHeaderAction(parent, "SettingsButton", "scn19_icon_settings_gear.png", new RectInt(actions.x + 126, actions.y + 10, 72, 64));
    }

    private static void AddHeaderResource(Transform parent, string name, string icon, string label, string value, RectInt rect, Color valueColor)
    {
        GameObject counter = CreatePathObject(parent, $"HeaderBar/{name}");
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(counter.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(rect));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(counter.transform, LayerRoot, "IconImage", icon, new RectInt(0, 0, 72, rect.height), 58, 58, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(counter.transform, "LabelText", label, new RectInt(82, 5, rect.width - 92, 26), 18f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(counter.transform, "ValueText", value, new RectInt(82, 31, rect.width - 92, 36), 31f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderAction(Transform parent, string name, string icon, RectInt rect)
    {
        Button button = AddButton(parent, $"HeaderBar/{name}", "scn19_small_counter_chip_frame.png", rect, false, true);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "IconImage", icon, new RectInt(12, 8, rect.width - 24, rect.height - 16), 52, 52, TextMain);
    }

    private static void AddTitle(Transform parent)
    {
        RectInt rect = TitleRect();
        AddSliced(parent, "TitleBackPanel", "scn19_title_back_panel_frame.png", rect, new Vector4(42, 42, 18, 18), Color.white);
        Button back = AddButton(parent, "HeaderBar/BackButton", "scn19_small_counter_chip_frame.png", new RectInt(18, rect.y, 78, 78), false, true);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(back.transform, LayerRoot, "IconImage", "scn19_icon_back_arrow.png", new RectInt(10, 8, 52, 48), 42, 42, TextMain);
        ScreenRouteButton routeButton = back.gameObject.AddComponent<ScreenRouteButton>();
        SetSerializedBool(routeButton, "useBackNavigation", true);

        AddFitted(parent, "TitleIcon", "scn19_icon_armory_crossed_weapons.png", new RectInt(rect.x + 26, rect.y + 16, 70, 58), 58, 50, TextMuted);
        AddText(parent, "HeaderBar/TitleText", "ARMORY", new RectInt(rect.x + 108, rect.y + 13, 190, 40), 32f, TextAlignmentOptions.Left, TextMain);
        AddText(parent, "HeaderBar/SubtitleText", "Roster Inspection", new RectInt(rect.x + 110, rect.y + 53, 190, 26), 18f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddCategoryRail(Transform parent)
    {
        GameObject rail = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("CategoryRail", parent);
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(rail.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(CategoryRailRect()));
        AddCategory(rail.transform, "UnitsButton", "scn19_icon_units_group.png", "UNITS", "24 / 48", 0, true);
        AddCategory(rail.transform, "VehiclesButton", "scn19_icon_vehicle_truck.png", "VEHICLES", "9 / 24", 1, false);
        AddCategory(rail.transform, "AircraftButton", "scn19_icon_aircraft_helicopter.png", "AIRCRAFT", "6 / 16", 2, false);
        AddCategory(rail.transform, "BuildingsButton", "scn19_icon_buildings.png", "BUILDINGS", "12 / 32", 3, false);
        AddCategory(rail.transform, "SupportButton", "scn19_icon_support_plus.png", "SUPPORT", "8 / 16", 4, false);
        AddCategory(rail.transform, "UpgradesButton", "scn19_icon_upgrades_chevrons.png", "UPGRADES", "18 / 36", 5, false);
    }

    private static void AddCategory(Transform parent, string name, string icon, string label, string count, int index, bool selected)
    {
        RectInt rect = new(0, index * 111, 410, 96);
        Button button = AddButton(parent, name, selected ? "scn19_category_button_selected_frame.png" : "scn19_category_button_default_frame.png", rect, selected, true);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "IconImage", icon, new RectInt(31, 16, 76, 66), 62, 58, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "LabelText", label, new RectInt(132, 16, 230, 36), 27f, TextAlignmentOptions.Left, selected ? TextMain : TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "CountText", count, new RectInt(132, 56, 170, 26), 19f, TextAlignmentOptions.Left, selected ? Gold : Olive);
    }

    private static void AddRoster(Transform parent)
    {
        RectInt rect = RosterRect();
        GameObject roster = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("RosterPanel", parent);
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(roster.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(rect));

        AddDropdown(roster.transform, "FilterDropdown", "FILTER: ALL", new RectInt(0, 0, 315, 58));
        AddDropdown(roster.transform, "SortDropdown", "SORT: RARITY", new RectInt(rect.width - 315, 0, 315, 58));

        RectInt viewport = RosterViewportRect();
        GameObject scrollRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("RosterScrollRect", roster.transform);
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(scrollRoot.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(viewport));
        Image scrollRaycast = scrollRoot.AddComponent<Image>();
        scrollRaycast.color = new Color(1f, 1f, 1f, 0.001f);
        scrollRaycast.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 38f;

        GameObject viewportObject = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Viewport", scrollRoot.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(viewportObject.GetComponent<RectTransform>());
        Image viewportRaycast = viewportObject.AddComponent<Image>();
        viewportRaycast.color = new Color(1f, 1f, 1f, 0.001f);
        viewportRaycast.raycastTarget = true;
        viewportObject.AddComponent<RectMask2D>();

        GameObject content = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Content", viewportObject.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(contentRect, WarlineCaptureLayeredUiBuilderUtility.ToArray(new RectInt(0, 0, viewport.width, RosterContentHeight())));
        scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.verticalNormalizedPosition = 1f;

        for (int i = 0; i < RosterItems.Length; i++)
        {
            int col = i % 4;
            int row = i / 4;
            RectInt card = new(col * 292, row * 264, 270, 254);
            AddRosterCard(content.transform, RosterItems[i], card);
        }
    }

    private static void AddDropdown(Transform parent, string name, string label, RectInt rect)
    {
        AddSliced(parent, $"{name}/Frame", "scn19_dropdown_frame.png", rect, new Vector4(34, 34, 18, 18), Color.white);
        AddText(parent, $"{name}/LabelText", label, new RectInt(rect.x + 28, rect.y + 15, rect.width - 75, 28), 18f, TextAlignmentOptions.Left, TextMuted);
        AddFitted(parent, $"{name}/Chevron", "scn19_icon_dropdown_chevron.png", new RectInt(rect.x + rect.width - 55, rect.y + 13, 34, 30), 26, 22, TextMuted);
    }

    private static void AddRosterCard(Transform parent, RosterItem item, RectInt rect)
    {
        string frame = item.Locked ? "scn19_roster_card_locked_frame.png" : item.Selected ? "scn19_roster_card_selected_frame.png" : "scn19_roster_card_default_frame.png";
        Button button = AddButton(parent, item.ObjectName, frame, rect, item.Selected, !item.Locked);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "CardTypeIcon", item.Icon, new RectInt(12, 8, 32, 32), 25, 25, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "TitleText", item.Title, new RectInt(48, 8, 200, 28), item.Title.Length > 15 ? 15.5f : 17.5f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(button.transform, LayerRoot, "ArtImage", item.Art, new RectInt(14, 43, rect.width - 28, 132), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "RoleText", item.Role, new RectInt(16, 180, 124, 24), 15f, TextAlignmentOptions.Left, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "LevelText", $"LVL {item.Level}", new RectInt(rect.width - 82, 180, 66, 24), 15f, TextAlignmentOptions.Right, TextMuted);
        AddProgress(button.transform, "Progress", new RectInt(16, 209, 128, 13), item.Selected ? 0.68f : 0.48f, Gold);

        string stateText = item.Locked ? "LOCKED" : item.UpgradeReady ? "UPGRADE READY" : "OWNED";
        Color stateColor = item.Locked ? new Color32(132, 130, 112, 255) : item.UpgradeReady ? Gold : Olive;
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "StateText", stateText, new RectInt(123, 224, 114, 24), item.UpgradeReady ? 13.5f : 14.5f, TextAlignmentOptions.Right, stateColor);
        string badge = item.Locked ? "scn19_badge_locked_padlock.png" : item.UpgradeReady ? "scn19_badge_upgrade_ready_chevrons.png" : "scn19_badge_owned_checkmark.png";
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "StateIcon", badge, new RectInt(rect.width - 34, 225, 24, 24), 20, 20, Color.white);
    }

    private static void AddInspection(Transform parent)
    {
        RectInt rect = InspectionRect();
        AddSliced(parent, "InspectionPanel/Frame", "scn19_inspection_panel_frame.png", rect, new Vector4(60, 60, 34, 34), Color.white);
        AddSliced(parent, "InspectionPanel/HeaderBand", "scn19_small_status_chip_frame.png", new RectInt(rect.x + 20, rect.y + 24, rect.width - 40, 228), new Vector4(24, 24, 12, 12), new Color(1f, 1f, 1f, 0.14f));
        AddText(parent, "InspectionPanel/TitleText", "RIFLEMAN MALE II", new RectInt(rect.x + 30, rect.y + 36, 310, 42), 30f, TextAlignmentOptions.Left, TextMain);
        AddText(parent, "InspectionPanel/RoleText", "INFANTRY", new RectInt(rect.x + 30, rect.y + 84, 160, 30), 21f, TextAlignmentOptions.Left, Olive);
        AddText(parent, "InspectionPanel/DescriptionText", "Reliable frontline infantry equipped for patrols, defense, and direct engagements.", new RectInt(rect.x + 30, rect.y + 124, 280, 90), 17f, TextAlignmentOptions.Left, TextMuted, true);
        AddCover(parent, "InspectionPanel/SelectedArtImage", "scn19_art_rifleman_male_ii.png", new RectInt(rect.x + 285, rect.y + 18, 300, 328), new Color(1f, 1f, 1f, 0.8f));

        AddStatRow(parent, "HealthStatRow", "scn19_icon_health_cross.png", "HEALTH", "220", new RectInt(rect.x + 30, rect.y + 245, 330, 42));
        AddStatRow(parent, "DamageStatRow", "scn19_icon_damage_burst.png", "DAMAGE", "28", new RectInt(rect.x + 30, rect.y + 293, 330, 42));
        AddStatRow(parent, "RangeStatRow", "scn19_icon_range_reticle.png", "RANGE", "35 m", new RectInt(rect.x + 30, rect.y + 341, 330, 42));
        AddStatRow(parent, "SpeedStatRow", "scn19_icon_speed_boot.png", "SPEED", "4.6 m/s", new RectInt(rect.x + 30, rect.y + 389, 330, 42));

        AddText(parent, "InspectionPanel/AbilitiesTitle", "ABILITIES", new RectInt(rect.x + 30, rect.y + 452, 160, 26), 18f, TextAlignmentOptions.Left, TextMuted);
        AddAbility(parent, "MoveAbility", "scn19_icon_move_runner.png", "MOVE", new RectInt(rect.x + 30, rect.y + 488, 118, 76));
        AddAbility(parent, "AttackAbility", "scn19_icon_attack_reticle.png", "ATTACK", new RectInt(rect.x + 160, rect.y + 488, 118, 76));
        AddAbility(parent, "HoldAbility", "scn19_icon_hold_shield.png", "HOLD", new RectInt(rect.x + 290, rect.y + 488, 118, 76));
        AddAbility(parent, "PatrolAbility", "scn19_icon_patrol_chevrons.png", "PATROL", new RectInt(rect.x + 420, rect.y + 488, 118, 76));

        AddText(parent, "InspectionPanel/UpgradeTrackTitle", "UPGRADE TRACK", new RectInt(rect.x + 30, rect.y + 584, 190, 26), 18f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, "InspectionPanel/TierText", "TIER II", new RectInt(rect.x + rect.width - 135, rect.y + 584, 105, 26), 18f, TextAlignmentOptions.Right, TextMuted);
        AddPartsProgress(parent, new RectInt(rect.x + 30, rect.y + 623, rect.width - 60, 58));
        AddSourceRow(parent, new RectInt(rect.x + 30, rect.y + 695, rect.width - 60, 64));

        AddCta(parent, "InspectionPanel/UpgradeButton", "UPGRADE", new RectInt(rect.x + 30, rect.y + 775, 160, 62), "scn19_cta_primary_gold_frame.png", true, true);
        AddCta(parent, "InspectionPanel/InspectAbilitiesButton", "INSPECT ABILITIES", new RectInt(rect.x + 205, rect.y + 775, 210, 62), "scn19_cta_secondary_dark_frame.png", false, true);
        AddCta(parent, "InspectionPanel/EquipButton", "EQUIP", new RectInt(rect.x + 430, rect.y + 775, 130, 62), "scn19_cta_disabled_frame.png", false, false);
    }

    private static void AddStatRow(Transform parent, string name, string icon, string label, string value, RectInt rect)
    {
        AddSliced(parent, $"InspectionPanel/{name}/Frame", "scn19_small_status_chip_frame.png", new RectInt(rect.x - 8, rect.y - 2, rect.width + 16, rect.height), new Vector4(18, 18, 8, 8), new Color(1f, 1f, 1f, 0.46f));
        AddFitted(parent, $"InspectionPanel/{name}/IconImage", icon, new RectInt(rect.x, rect.y, 36, rect.height), 28, 28, TextMuted);
        AddText(parent, $"InspectionPanel/{name}/LabelText", label, new RectInt(rect.x + 52, rect.y + 7, 180, 26), 17f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, $"InspectionPanel/{name}/ValueText", value, new RectInt(rect.x + rect.width - 120, rect.y + 7, 115, 26), 17f, TextAlignmentOptions.Right, Gold);
    }

    private static void AddAbility(Transform parent, string name, string icon, string label, RectInt rect)
    {
        AddSliced(parent, $"InspectionPanel/{name}/Frame", "scn19_small_counter_chip_frame.png", rect, new Vector4(24, 24, 12, 12), new Color(1f, 1f, 1f, 0.92f));
        AddFitted(parent, $"InspectionPanel/{name}/IconImage", icon, new RectInt(rect.x + 34, rect.y + 8, 42, 34), 32, 30, TextMain);
        AddText(parent, $"InspectionPanel/{name}/LabelText", label, new RectInt(rect.x + 8, rect.y + 42, rect.width - 16, 24), 14f, TextAlignmentOptions.Center, TextMuted);
    }

    private static void AddPartsProgress(Transform parent, RectInt rect)
    {
        AddFitted(parent, "InspectionPanel/PartsProgress/PartsIcon", "scn19_icon_blueprint_parts.png", new RectInt(rect.x, rect.y, 44, rect.height), 36, 36, Gold);
        AddText(parent, "InspectionPanel/PartsProgress/LabelText", "BLUEPRINT PARTS", new RectInt(rect.x + 55, rect.y + 1, 200, 24), 16f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, "InspectionPanel/PartsProgress/ValueText", "38 / 60", new RectInt(rect.x + rect.width - 115, rect.y + 1, 110, 24), 16f, TextAlignmentOptions.Right, Gold);
        AddProgress(parent, "InspectionPanel/PartsProgress", new RectInt(rect.x + 56, rect.y + 31, rect.width - 66, 20), 0.63f, Gold);
    }

    private static void AddSourceRow(Transform parent, RectInt rect)
    {
        AddSliced(parent, "InspectionPanel/SourceRow/Frame", "scn19_small_status_chip_frame.png", rect, new Vector4(24, 24, 12, 12), Color.white);
        AddFitted(parent, "InspectionPanel/SourceRow/IconImage", "scn19_icon_source_building.png", new RectInt(rect.x + 12, rect.y + 12, 42, 40), 32, 32, TextMuted);
        AddText(parent, "InspectionPanel/SourceRow/LabelText", "SOURCE / UNLOCK", new RectInt(rect.x + 64, rect.y + 8, 250, 23), 15f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, "InspectionPanel/SourceRow/ValueText", "Barracks Level 4", new RectInt(rect.x + 64, rect.y + 31, 260, 25), 16f, TextAlignmentOptions.Left, Gold);
    }

    private static void AddCta(Transform parent, string name, string label, RectInt rect, string frame, bool primary, bool interactable)
    {
        Button button = AddButton(parent, name, frame, rect, primary, interactable);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "LabelText", label, new RectInt(8, 13, rect.width - 16, 36), label.Length > 12 ? 18f : 24f, TextAlignmentOptions.Center, primary ? Color.black : (interactable ? Gold : new Color32(120, 117, 100, 255)));
        if (!interactable)
            WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "DisabledIcon", "scn19_icon_disabled_slash.png", new RectInt(10, 14, 32, 32), 26, 26, new Color(1f, 1f, 1f, 0.7f));
    }

    private static void AddBottomTabs(Transform parent)
    {
        GameObject tabs = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("BottomTabBar", parent);
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(tabs.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(BottomTabsRect()));
        AddSliced(tabs.transform, "ContinuousBackFrame", "scn19_bottom_tab_default_frame.png", new RectInt(0, 0, BottomTabsRect().width, 64), new Vector4(28, 28, 12, 12), new Color(1f, 1f, 1f, 0.56f));
        AddBottomTab(tabs.transform, "OwnedTab", "scn19_icon_units_group.png", "OWNED", new RectInt(0, 0, 270, 64), true);
        AddBottomTab(tabs.transform, "UpgradeTracksTab", "scn19_icon_upgrades_chevrons.png", "UPGRADE TRACKS", new RectInt(270, 0, 342, 64), false);
        AddBottomTab(tabs.transform, "PartsTab", "scn19_icon_blueprint_parts.png", "PARTS", new RectInt(612, 0, 270, 64), false);
        AddBottomTab(tabs.transform, "GearModulesTab", "scn19_icon_settings_gear.png", "GEAR MODULES", new RectInt(882, 0, 366, 64), false);
    }

    private static void AddBottomTab(Transform parent, string name, string icon, string label, RectInt rect, bool selected)
    {
        Button button = AddButton(parent, name, selected ? "scn19_bottom_tab_selected_frame.png" : "scn19_bottom_tab_default_frame.png", rect, selected, true);
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(button.transform, LayerRoot, "IconImage", icon, new RectInt(32, 13, 42, 38), 30, 30, TextMuted);
        WarlineCaptureLayeredUiBuilderUtility.AddText(button.transform, "LabelText", label, new RectInt(80, 16, rect.width - 100, 30), label.Length > 8 ? 18f : 20f, TextAlignmentOptions.Center, selected ? TextMain : TextMuted);
    }

    private static void AddFooter(Transform parent)
    {
        RectInt comms = CommsRect();
        AddSliced(parent, "CommsStatusPanel/Frame", "scn19_comms_status_panel_frame.png", comms, new Vector4(42, 42, 18, 18), Color.white);
        AddFitted(parent, "CommsStatusPanel/IconImage", "scn19_icon_comms_signal.png", new RectInt(comms.x + 20, comms.y + 16, 50, 42), 42, 34, Olive);
        AddText(parent, "CommsStatusPanel/LabelText", "COMMS ONLINE", new RectInt(comms.x + 82, comms.y + 18, 170, 30), 18f, TextAlignmentOptions.Left, Olive);

        RectInt route = new(860, 1034, 620, 38);
        AddSliced(parent, "RouteBreadcrumbStrip/Frame", "scn19_route_breadcrumb_strip_frame.png", route, new Vector4(32, 32, 12, 12), new Color(1f, 1f, 1f, 0.9f));
        AddText(parent, "RouteBreadcrumbStrip/MainMenuText", "MAIN MENU", new RectInt(route.x + 45, route.y + 12, 140, 24), 17f, TextAlignmentOptions.Center, TextMuted);
        AddText(parent, "RouteBreadcrumbStrip/ProfileText", "COMMANDER PROFILE", new RectInt(route.x + 245, route.y + 12, 210, 24), 17f, TextAlignmentOptions.Center, TextMuted);
        AddText(parent, "RouteBreadcrumbStrip/ArmoryText", "ARMORY", new RectInt(route.x + 490, route.y + 12, 110, 24), 17f, TextAlignmentOptions.Center, Olive);

        RectInt disabled = new(InspectionRect().x + 30, InspectionRect().y + 775, InspectionRect().width - 60, 62);
        AddSliced(parent, "DisabledReasonPanel/Frame", "scn19_small_status_chip_frame.png", disabled, new Vector4(24, 24, 12, 12), new Color(1f, 1f, 1f, 0f));
        AddFitted(parent, "DisabledReasonPanel/WarningIcon", "scn19_icon_disabled_slash.png", new RectInt(disabled.x + 18, disabled.y + 13, 34, 32), 26, 26, new Color(1f, 1f, 1f, 0f));
        AddText(parent, "DisabledReasonPanel/ReasonText", "Equip is disabled until roster equipment persistence is connected.", new RectInt(disabled.x + 66, disabled.y + 15, disabled.width - 86, 28), 17f, TextAlignmentOptions.Left, new Color(Gold.r, Gold.g, Gold.b, 0f));
    }

    private static void AddProgress(Transform parent, string name, RectInt rect, float normalized, Color fillTint)
    {
        AddSliced(parent, $"{name}/ProgressFrame", "scn19_progress_meter_empty_frame.png", rect, new Vector4(14, 14, 6, 6), Color.white);
        RectInt fill = new(rect.x + 4, rect.y + 4, Mathf.RoundToInt((rect.width - 8) * Mathf.Clamp01(normalized)), rect.height - 8);
        AddSliced(parent, $"{name}/ProgressFill", "scn19_progress_fill_gold_segment.png", fill, new Vector4(8, 8, 3, 3), fillTint);
    }

    private static GameObject CreatePathObject(Transform parent, string path)
    {
        Transform resolvedParent = ResolvePathParent(parent, path, out string leafName);
        return WarlineCaptureLayeredUiBuilderUtility.CreateRectObject(leafName, resolvedParent);
    }

    private static Image AddSliced(Transform parent, string path, string spriteName, RectInt rect, Vector4 border, Color color)
    {
        Transform resolvedParent = ResolvePathParent(parent, path, out string leafName);
        return WarlineCaptureLayeredUiBuilderUtility.AddSlicedImage(resolvedParent, LayerRoot, leafName, spriteName, rect, border, color);
    }

    private static Image AddFitted(Transform parent, string path, string spriteName, RectInt rect, int maxWidth, int maxHeight, Color color)
    {
        Transform resolvedParent = ResolvePathParent(parent, path, out string leafName);
        return WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(resolvedParent, LayerRoot, leafName, spriteName, rect, maxWidth, maxHeight, color);
    }

    private static TMP_Text AddText(Transform parent, string path, string value, RectInt rect, float size, TextAlignmentOptions alignment, Color color, bool wordWrap = false)
    {
        Transform resolvedParent = ResolvePathParent(parent, path, out string leafName);
        return WarlineCaptureLayeredUiBuilderUtility.AddText(resolvedParent, leafName, value, rect, size, alignment, color, wordWrap);
    }

    private static void AddCover(Transform parent, string path, string spriteName, RectInt rect, Color color)
    {
        Transform resolvedParent = ResolvePathParent(parent, path, out string leafName);
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(resolvedParent, LayerRoot, leafName, spriteName, rect, color);
    }

    private static Transform ResolvePathParent(Transform parent, string path, out string leafName)
    {
        string[] parts = path.Split('/');
        leafName = parts[parts.Length - 1];
        Transform current = parent;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            Transform existing = current.Find(parts[i]);
            if (existing == null)
            {
                GameObject group = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject(parts[i], current);
                WarlineCaptureLayeredUiBuilderUtility.StretchToParent(group.GetComponent<RectTransform>());
                existing = group.transform;
            }

            current = existing;
        }

        return current;
    }

    private static Button AddButton(Transform parent, string name, string sprite, RectInt rect, bool selected, bool interactable)
    {
        GameObject buttonObject = CreatePathObject(parent, name);
        WarlineCaptureLayeredUiBuilderUtility.ApplyTopLeftRect(buttonObject.GetComponent<RectTransform>(), WarlineCaptureLayeredUiBuilderUtility.ToArray(rect));
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = WarlineCaptureLayeredUiBuilderUtility.LoadSprite(LayerRoot, sprite);
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = interactable;
        ConfigureAnimatedButton(button, selected);
        return button;
    }

    private static void ConfigureAnimatedButton(Button button, bool selectedOnEnable)
    {
        Animator animator = button.GetComponent<Animator>();
        if (animator == null)
            animator = button.gameObject.AddComponent<Animator>();

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ButtonAnimatorControllerPath);
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        button.transition = Selectable.Transition.Animation;
        button.animationTriggers = new AnimationTriggers
        {
            normalTrigger = "Normal",
            highlightedTrigger = "Highlighted",
            pressedTrigger = "Pressed",
            selectedTrigger = "Selected",
            disabledTrigger = "Disabled"
        };

        WarlineCaptureButtonAnimationState state = button.GetComponent<WarlineCaptureButtonAnimationState>();
        if (state == null)
            state = button.gameObject.AddComponent<WarlineCaptureButtonAnimationState>();

        SetSerializedString(state, "initialStateName", selectedOnEnable ? "Selected" : "Normal");
        SetSerializedBool(state, "selectWithEventSystem", selectedOnEnable);

        UiMotionFeedback feedback = button.GetComponent<UiMotionFeedback>();
        if (feedback == null)
            feedback = button.gameObject.AddComponent<UiMotionFeedback>();
        feedback.ConfigureButtonDefaults(selectedOnEnable);
    }

    private static void AddHitZones(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "BackHitZone", new RectInt(18, TitleRect().y, 78, 78));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "UnitsHitZone", new RectInt(CategoryRailRect().x, CategoryRailRect().y, 410, 96));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "RiflemanHitZone", new RectInt(RosterRect().x, RosterRect().y + 78, 270, 230));
        WarlineCaptureLayeredUiBuilderUtility.AddHitZone(parent, "UpgradeHitZone", new RectInt(InspectionRect().x + 30, InspectionRect().y + 790, 160, 62));
    }

    private static void CopyLayerAssetsToUnity()
    {
        if (!Directory.Exists(SourceLayerRoot))
            throw new DirectoryNotFoundException(SourceLayerRoot);

        Directory.CreateDirectory(LayerRoot);
        foreach (string layerFile in LayerFiles)
        {
            string source = Path.Combine(SourceLayerRoot, layerFile);
            if (!File.Exists(source))
                throw new FileNotFoundException($"Missing SCN-19 layer source: {source}");

            string destination = Path.Combine(LayerRoot, layerFile);
            File.Copy(source, destination, overwrite: true);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }

    private static void EnsureSlicedImports()
    {
        foreach (string layer in SlicedLayers)
            EnsureSpriteBorder($"{LayerRoot}/{layer}", new Vector4(24f, 24f, 14f, 14f));
    }

    private static void EnsureSpriteBorder(string assetPath, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static void SetSerializedString(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject serialized = new(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectInt HeaderLogoRect() => new(0, 0, 555, 106);
    private static RectInt HeaderResourceRect() => new(955, 0, 850, 106);
    private static RectInt HeaderActionsRect() => new(2148, 14, 230, 78);
    private static RectInt TitleRect() => new(112, 112, 318, 86);
    private static RectInt CategoryRailRect() => new(18, 210, 410, 662);
    private static RectInt RosterRect() => new(490, 132, 1146, 810);
    private static RectInt RosterViewportRect() => new(0, 70, RosterRect().width, RosterRect().height - 70);
    private static int RosterContentHeight() => Mathf.CeilToInt(RosterItems.Length / 4f) * 264 - 10;
    private static RectInt InspectionRect() => new(1738, 132, 595, 825);
    private static RectInt BottomTabsRect() => new(490, 966, 1248, 68);
    private static RectInt CommsRect() => new(18, 918, 280, 78);
}
#endif
