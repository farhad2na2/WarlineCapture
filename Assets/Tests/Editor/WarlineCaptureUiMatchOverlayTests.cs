using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiMatchOverlayTests
{
    private const string MatchOverlayPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab";
    private const string AssistantPanelPrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab";
    private const string MatchHudLayerPackManifestPath = "Design/VisualLockLayered/SCN-08_RTSBattleHUD/layer_manifest.json";
    private const string MatchHudM01LayerPackManifestPath = "Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback/layer_manifest.json";
    private const string MatchHudResourceBarPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/resource_bar_frame.png";
    private const string MatchHudResourceBarFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/resource_bar_fill.png";
    private const string MatchHudMoneyIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/resource_money_icon.png";
    private const string MatchHudMaterialsIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/resource_crate_icon.png";
    private const string MatchHudPopulationIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/resource_population_icon.png";
    private const string MatchHudTimeIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/time_clock_icon.png";
    private const string MatchHudLegacyTimeIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/Icons/MatchHUD_Resource_Time.png";
    private const string MatchHudObjectivePanelPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/objective_panel_frame.png";
    private const string MatchHudObjectivePanelFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/objective_panel_fill.png";
    private const string MatchHudThreatPanelPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/threat_feed_panel_frame.png";
    private const string MatchHudThreatPanelFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/threat_feed_panel_fill.png";
    private const string MatchHudMiniMapPanelPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/minimap_frame.png";
    private const string MatchHudMiniMapPanelFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/minimap_fill.png";
    private const string MatchHudSquadTrayPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/squad_tray_frame.png";
    private const string MatchHudSquadTrayFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/squad_tray_fill.png";
    private const string MatchHudCommandRailPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_rail_frame.png";
    private const string MatchHudCommandRailFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_rail_fill.png";
    private const string MatchHudButtonNormalPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/command_button_normal_background.png";
    private const string MatchHudButtonSelectedPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/command_button_selected_background.png";
    private const string MatchHudButtonBuildPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/build_button_selected_background.png";
    private const string MatchHudTopButtonPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/top_icon_button_background.png";
    private const string MatchHudSquadCardNormalPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Cards/squad_card_normal_background.png";
    private const string MatchHudSquadCardSelectedPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Cards/squad_card_selected_background.png";
    private const string MatchHudSquadCardFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/MatchHUD_SquadCard_Fill_9Slice.png";
    private const string MatchHudIsoForwardCommandHqPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/IsoThumb_GA-04_ForwardCommandHQ.png";
    private const string MatchHudIsoRifleSquadPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/IsoThumb_GA-08_RifleSquad.png";
    private const string MatchHudIsoApcPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/IsoThumb_GA-09_APC.png";
    private const string MatchHudIsoTankPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/IsoThumb_GA-10_Tank.png";
    private const string MatchHudSquadPortraitRiflePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Content/squad_portrait_rifle.png";
    private const string MatchHudSquadPortraitApcPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Content/squad_portrait_apc.png";
    private const string MatchHudSquadPortraitTankPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Content/squad_portrait_tank.png";
    private const string MatchHudHelicopterPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Content/squad_portrait_helicopter.png";
    private const string MatchHudDesignedUnavailableContentPath = "Assets/Game/Art/UI/Generated/MatchHUD/Cards/MatchHUD_DesignedUnavailableContent.png";
    private const string MatchHudIsoMiniMapContentPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Content/minimap_content.png";
    private const string MatchHudIconPausePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/pause_icon.png";
    private const string MatchHudIconSettingsPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/settings_gear_icon.png";
    private const string MatchHudIconStopPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_stop_icon.png";
    private const string MatchHudIconHoldPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_hold_icon.png";
    private const string MatchHudIconSelectPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_select_icon.png";
    private const string MatchHudIconMovePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_move_icon.png";
    private const string MatchHudIconAttackPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_attack_icon.png";
    private const string MatchHudIconSpecialPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_special_icon.png";
    private const string MatchHudIconBuildPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_build_icon.png";
    private const string MatchHudIconRankChevronPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/squad_rank_triple_chevron.png";
    private const string MatchHudObjectiveCheckboxPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/objective_empty_square.png";
    private const string MatchHudObjectiveStarPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/objective_star_filled.png";
    private const string MatchHudM01CommandModeFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Frames/command_mode_banner_frame.png";
    private const string MatchHudM01SelectedEntityFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Frames/selected_entity_panel_frame.png";
    private const string MatchHudM01InvalidToastFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Frames/invalid_command_toast_frame.png";
    private const string MatchHudM01SelectionRingPath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png";
    private const string MatchHudM01MoveDestinationRingPath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/move_destination_ring.png";
    private const string MatchHudM01AttackTargetRingPath = "Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/attack_target_ring.png";
    private const string MatchHudM01MinimapViewportRectPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/minimap_viewport_rect.png";
    private const string CommandWheelLayerPackManifestPath = "Design/VisualLockLayered/SCN-10_UnitCommandWheel/layer_manifest.json";
    private const string CommandWheelContextHintFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_context_hint_frame.png";
    private const string CommandWheelContextHintFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_context_hint_fill.png";
    private const string CommandWheelEntityFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_entity_frame.png";
    private const string CommandWheelEntityFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_entity_fill.png";
    private const string CommandWheelCenterFramePath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_center_frame.png";
    private const string CommandWheelCenterFillPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_center_fill.png";
    private const string CommandWheelTargetingRingPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/command_wheel_targeting_ring.png";
    private const string CommandWheelSegmentNormalPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/command_wheel_segment_normal.png";
    private const string CommandWheelSegmentSelectedPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/command_wheel_segment_selected.png";
    private const string CommandWheelCloseButtonPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Buttons/command_wheel_close_button.png";
    private const string CommandWheelExtractIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_extract_icon.png";
    private const string CommandWheelRopeDropIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_rope_drop_icon.png";
    private const string CommandWheelPatrolIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_patrol_icon.png";
    private const string CommandWheelHintInfoIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_hint_info_icon.png";
    private const string CommandWheelCloseIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_wheel_close_icon.png";
    private const string CommandWheelTargetBracketsIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_target_brackets.png";
    private const string CommandWheelBlackhawkIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Icons/command_blackhawk_icon.png";
    private const string CommandWheelBlackhawkArtPath = "Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Cards/command_wheel_blackhawk_unit_art.png";
    private const string UiButtonAnimatorControllerPath = "Assets/Game/Animations/UI/WarlineCaptureButtonStates.overrideController";
    private static readonly Vector2 ReferenceSize = new(1672f, 941f);

    [SetUp]
    public void SetUp()
    {
        WarlineCaptureMissionSession.Clear();
        GameRuntimeStats.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        WarlineCaptureMissionSession.Clear();
        GameRuntimeStats.Reset();
    }

    [Test]
    public void MatchOverlay_HasTacticalHudHierarchy()
    {
        GameObject prefab = LoadPrefab();
        WarlineCaptureScreenController controller = prefab.GetComponent<WarlineCaptureScreenController>();
        Assert.NotNull(controller);
        Assert.AreEqual(WarlineCaptureRoute.Match, controller.Route);
        Assert.NotNull(prefab.GetComponent<BattleHudGameplayBridge>(), "Match overlay must expose a gameplay-to-HUD bridge for selection and command feedback.");
        MatchOverlayCommandControlsController commandControls = prefab.GetComponent<MatchOverlayCommandControlsController>();
        Assert.NotNull(commandControls, "Match overlay must expose Hold/Stop command button wiring.");
        Assert.NotNull(commandControls.HoldButton, "Hold command button must be wired.");
        Assert.NotNull(commandControls.StopButton, "Stop command button must be wired.");
        Assert.NotNull(commandControls.CommandWheelStopButton, "Command wheel Stop segment must be wired.");
        Assert.IsNull(prefab.GetComponent<Image>(), "The tactical HUD overlay must not bake the battlefield or visual target into the screen root.");

        AssertChildren(
            prefab,
            "ObjectivePanel",
            "ObjectivePanel/FillBackground",
            "ObjectivePanel/AccentBar",
            "ObjectivePanel/FrameChrome",
            "ObjectivePanel/Objective_1/LabelText",
            "ObjectivePanel/StarGoal_1/LabelText",
            "ThreatFeedPanel",
            "ThreatFeedPanel/FillBackground",
            "ThreatFeedPanel/FrameChrome",
            "ResourceBar",
            "ResourceBar/FillBackground",
            "ResourceBar/FrameChrome",
            "ResourceBar/Divider_MoneyMaterials",
            "ResourceBar/Divider_MaterialsPopulation",
            "ResourceBar/Divider_PopulationTime",
            "ResourceBar/MoneyCounter/IconImage",
            "ResourceBar/MoneyCounter/ValueText",
            "ResourceBar/MaterialsCounter/IconImage",
            "ResourceBar/PopulationCounter/IconImage",
            "ResourceBar/TimeCounter/IconImage",
            "WorldCommandMarkerLayer",
            "WorldCommandMarkerLayer/SelectionRing",
            "WorldCommandMarkerLayer/MoveDestinationMarker/RingImage",
            "WorldCommandMarkerLayer/MoveDestinationMarker/Icon",
            "WorldCommandMarkerLayer/AttackTargetMarker",
            "PauseButton/IconImage",
            "SettingsButton/GearIcon",
            "AssistantLayer",
            "AssistantLayer/AssistantEntryButton",
            "AssistantLayer/AssistantEntryButton/StateBackground",
            "AssistantLayer/AssistantEntryButton/WaveformIcon",
            "AssistantLayer/AssistantEntryButton/LabelText",
            "AssistantLayer/AssistantEntryButton/StateText",
            "AssistantLayer/AssistantEntryButton/CueText",
            "AssistantLayer/AssistantPanelDock",
            "SquadTray",
            "SquadTray/FillBackground",
            "SquadTray/FrameChrome",
            "SquadTray/Squad_Rifle/FrameChrome",
            "SquadTray/Squad_Rifle/PortraitPlate",
            "SquadTray/Squad_Rifle/HealthFill",
            "SquadTray/Squad_APC/FrameChrome",
            "SquadTray/Squad_APC/RankIcon",
            "CommandBar",
            "CommandBar/CommandRailArt/FillBackground",
            "CommandBar/CommandRailArt/FrameChrome",
            "CommandBar/SelectButton",
            "CommandBar/SelectButton/IconText",
            "CommandBar/StopButton",
            "CommandBar/StopButton/IconText",
            "CommandBar/HoldButton",
            "CommandBar/HoldButton/IconText",
            "CommandBar/MoveButton",
            "CommandBar/MoveButton/IconText",
            "BuildButton",
            "BuildButton/IconText",
            "SelectedEntityPanel",
            "SelectedEntityPanel/Frame",
            "SelectedEntityPanel/NameText",
            "SelectedEntityPanel/StatusText",
            "CommandModeBanner",
            "CommandModeBanner/Frame",
            "CommandModeBanner/ModeText",
            "InvalidCommandToast",
            "InvalidCommandToast/Frame",
            "InvalidCommandToast/MessageText",
            "MiniMapPanel/MinimapCameraBridge/ViewportRect",
            "CommandWheelCanvas",
            "CommandWheelCanvas/Scrim",
            "CommandWheelCanvas/CommandHint/FillBackground",
            "CommandWheelCanvas/CommandHint/FrameChrome",
            "CommandWheelCanvas/CommandHint/InfoIcon",
            "CommandWheelCanvas/SelectedEntityCard/FillBackground",
            "CommandWheelCanvas/SelectedEntityCard/FrameChrome",
            "CommandWheelCanvas/SelectedEntityCard/UnitArt",
            "CommandWheelCanvas/RadialCommandRoot/MoveSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/AttackSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/ExtractSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/RopeDropSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/PatrolSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/StopSegment/IconImage",
            "CommandWheelCanvas/RadialCommandRoot/CenterHub/FillBackground",
            "CommandWheelCanvas/RadialCommandRoot/CenterHub/FrameChrome",
            "CommandWheelCanvas/CloseButton/IconImage",
            "MiniMapPanel",
            "MiniMapPanel/FillBackground",
            "MiniMapPanel/FrameChrome",
            "MiniMapPanel/MapImage",
            "MiniMapPanel/ZoomInButton",
            "MiniMapPanel/ZoomOutButton");
    }

    [Test]
    public void MatchOverlay_HasBuildDrawerOverlayHierarchy()
    {
        GameObject prefab = LoadPrefab();
        Transform drawer = prefab.transform.Find("BuildDrawerCanvas");
        Assert.NotNull(drawer);
        Assert.IsFalse(drawer.gameObject.activeSelf, "Build drawer should be hidden until the HUD Build button is pressed.");
        Assert.NotNull(prefab.GetComponent<BuildDrawerPanelController>());
        Assert.Greater(
            drawer.GetSiblingIndex(),
            prefab.transform.Find("MiniMapPanel").GetSiblingIndex(),
            "Build drawer must render above the base HUD, including the minimap.");

        AssertChildren(
            prefab,
            "BuildDrawerCanvas/Scrim",
            "BuildDrawerCanvas/FillBackground",
            "BuildDrawerCanvas/FrameChrome",
            "BuildDrawerCanvas/HeaderBar/TitleText",
            "BuildDrawerCanvas/HeaderBar/CloseButton/IconText",
            "BuildDrawerCanvas/Tab_INFANTRY/LabelText",
            "BuildDrawerCanvas/Tab_VEHICLES/LabelText",
            "BuildDrawerCanvas/BuildListPanel/FillBackground",
            "BuildDrawerCanvas/BuildListPanel/FrameChrome",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/PreviewImage",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/TitleText",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/PopulationMetric/IconImage",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/MaterialMetric/IconImage",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/TimeMetric/IconImage",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/BuildButton/LabelText",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Grenadier/BuildButton",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Medic/BuildButton",
            "BuildDrawerCanvas/BuildListPanel/BuildItem_Barracks/BuildButton",
            "BuildDrawerCanvas/ProductionQueuePanel/FillBackground",
            "BuildDrawerCanvas/ProductionQueuePanel/FrameChrome",
            "BuildDrawerCanvas/ProductionQueuePanel/Queue_Rifle/PreviewImage",
            "BuildDrawerCanvas/ProductionQueuePanel/Queue_Rifle/ProgressBar/Fill",
            "BuildDrawerCanvas/ProductionQueuePanel/Queue_Rifle/CancelButton/IconText",
            "BuildDrawerCanvas/ProductionQueuePanel/LockedQueueSlot/LabelText",
            "BuildDrawerCanvas/BuildCapacityPanel/CapacityBar/Fill",
            "BuildDrawerCanvas/RushAllButton/LabelText");
    }

    [Test]
    public void MatchOverlay_BuildDrawerUsesLayeredChromeAndAnimatedControls()
    {
        GameObject prefab = LoadPrefab();

        AssertNoImage(prefab.transform, "BuildDrawerCanvas");
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/FillBackground", MatchHudCommandRailFillPath);
        AssertImageType(prefab.transform, "BuildDrawerCanvas/FillBackground", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/FrameChrome", MatchHudCommandRailPath);
        AssertImageType(prefab.transform, "BuildDrawerCanvas/FrameChrome", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/FillBackground", MatchHudObjectivePanelFillPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/FrameChrome", MatchHudObjectivePanelPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/ProductionQueuePanel/FillBackground", MatchHudObjectivePanelFillPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/ProductionQueuePanel/FrameChrome", MatchHudObjectivePanelPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/PreviewImage", MatchHudIsoRifleSquadPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Grenadier/PreviewImage", MatchHudIsoTankPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Medic/PreviewImage", MatchHudDesignedUnavailableContentPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Barracks/PreviewImage", MatchHudIsoForwardCommandHqPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/PopulationMetric/IconImage", MatchHudPopulationIconPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/MaterialMetric/IconImage", MatchHudMaterialsIconPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/TimeMetric/IconImage", MatchHudLegacyTimeIconPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/ProductionQueuePanel/Queue_Rifle/PreviewImage", MatchHudIsoRifleSquadPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/ProductionQueuePanel/Queue_Barracks/PreviewImage", MatchHudIsoForwardCommandHqPath);
        AssertImageSpritePath(prefab.transform, "BuildDrawerCanvas/ProductionQueuePanel/Queue_Medic/PreviewImage", MatchHudDesignedUnavailableContentPath);

        AssertAnimatedTabButton(prefab, "BuildDrawerCanvas/Tab_INFANTRY", "Selected", true);
        AssertAnimatedTabButton(prefab, "BuildDrawerCanvas/Tab_VEHICLES", "Normal", false);
        AssertAnimatedTabButton(prefab, "BuildDrawerCanvas/BuildListPanel/BuildItem_Rifle/BuildButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "BuildDrawerCanvas/ProductionQueuePanel/Queue_Rifle/CancelButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "BuildDrawerCanvas/RushAllButton", "Normal", false);
        Assert.IsFalse(
            prefab.transform.Find("BuildDrawerCanvas/BuildListPanel/BuildItem_Medic/BuildButton").GetComponent<Button>().interactable,
            "BuildItem_Medic should be disabled until accepted 2D isometric unit art and gameplay config exist.");

        Image scrim = prefab.transform.Find("BuildDrawerCanvas/Scrim").GetComponent<Image>();
        Assert.IsTrue(scrim.raycastTarget, "Build drawer must block world input while open.");
    }

    [Test]
    public void MatchOverlay_BuildButtonTogglesBuildDrawerOverlay()
    {
        GameObject prefab = LoadPrefab();
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            BuildDrawerPanelController controller = instance.GetComponent<BuildDrawerPanelController>();
            Assert.NotNull(controller);
            var awake = typeof(BuildDrawerPanelController).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(controller, null);

            Button buildButton = instance.transform.Find("BuildButton").GetComponent<Button>();
            Button closeButton = instance.transform.Find("BuildDrawerCanvas/HeaderBar/CloseButton").GetComponent<Button>();

            Assert.IsFalse(controller.IsOpen);
            buildButton.onClick.Invoke();
            Assert.IsTrue(controller.IsOpen);
            closeButton.onClick.Invoke();
            Assert.IsFalse(controller.IsOpen);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_M01ScopeHidesBuildFromNoSelectionCommandSet()
    {
        WarlineCaptureMissionSession.BeginMission(ChapterOneMissionCatalog.FirstContactMissionId, WarlineCaptureRoute.SagaMap);
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            M01InfantryOnlyHudScopeController scope = instance.GetComponent<M01InfantryOnlyHudScopeController>();
            Assert.NotNull(scope);
            scope.Refresh();

            GameObject buildButtonRoot = instance.transform.Find("BuildButton").gameObject;
            Button buildButton = buildButtonRoot.GetComponent<Button>();
            Assert.IsFalse(buildButtonRoot.activeSelf, "M01-01 no-selection command set excludes Build.");
            Assert.IsFalse(buildButton.interactable, "M01-01 Build must be disabled instead of available.");
            Assert.IsFalse(scope.IsM01BuildButtonDisabled(), "M01 scope hides Build rather than exposing it in the no-selection command row.");
            Assert.IsTrue(scope.AreM01SuppressedRootsHidden(), "M01 scope should suppress non-infantry affordances.");
            AssertText(instance, "ObjectivePanel/Objective_1/LabelText", "Destroy hostile patrol");
            Assert.IsFalse(instance.transform.Find("ObjectivePanel/Objective_2").gameObject.activeSelf, "M01 no-selection evidence must not show non-M01 objective rows.");
            Assert.IsFalse(instance.transform.Find("ObjectivePanel/Objective_3").gameObject.activeSelf, "M01 no-selection evidence must not show non-M01 objective rows.");
            Assert.IsFalse(instance.transform.Find("AssistantLayer/AssistantEntryButton").gameObject.activeSelf, "M01-01 must keep ARIA closed.");
            Assert.IsFalse(instance.transform.Find("AssistantLayer/AssistantPanelDock").gameObject.activeSelf, "M01-01 must not show the ARIA panel in no-selection evidence.");
            Assert.IsFalse(instance.transform.Find("WorldCommandMarkerLayer").gameObject.activeSelf, "M01-01 no-selection must not show selected rings or command target markers.");
            Assert.IsFalse(instance.transform.Find("SelectedEntityPanel").gameObject.activeSelf, "M01-01 no-selection must not show selected squad status.");
            Assert.IsFalse(instance.transform.Find("CommandModeBanner").gameObject.activeSelf, "M01-01 no-selection must not show an active command mode.");
            Assert.IsFalse(instance.transform.Find("InvalidCommandToast").gameObject.activeSelf, "M01-01 no-selection must not show stale command feedback.");
            Assert.IsFalse(instance.transform.Find("BuildDrawerCanvas").gameObject.activeSelf, "M01-01 no-selection must not show the Build drawer.");
            Assert.IsFalse(instance.transform.Find("CommandWheelCanvas").gameObject.activeSelf, "M01-01 no-selection must not show the command wheel.");
            Assert.IsTrue(instance.transform.Find("SquadTray/Squad_Rifle").gameObject.activeSelf, "M01 keeps the selected-readability squad strip visible.");
            Assert.IsTrue(instance.transform.Find("SquadTray/Squad_APC").gameObject.activeSelf, "M01 keeps the SCN-08 squad card layout instead of leaving empty tray columns.");
            Assert.IsFalse(instance.transform.Find("SquadTray/Squad_APC").GetComponent<Button>().interactable, "M01 non-rifle squad cards are visible but disabled.");
            Assert.IsTrue(instance.transform.Find("CommandBar/SelectButton").gameObject.activeSelf, "M01 uses SELECT as the first command slot.");
            Assert.IsTrue(instance.transform.Find("CommandBar/MoveButton").gameObject.activeSelf, "M01 keeps command button chrome visible.");
            Assert.IsTrue(instance.transform.Find("CommandBar/AttackButton").gameObject.activeSelf, "M01 keeps command button chrome visible.");
            Assert.IsTrue(instance.transform.Find("CommandBar/StopButton").gameObject.activeSelf, "M01 keeps command button chrome visible.");
            Assert.IsTrue(instance.transform.Find("CommandBar/HoldButton").gameObject.activeSelf, "M01 keeps command button chrome visible.");
            Assert.IsFalse(instance.transform.Find("CommandBar/SpecialButton").gameObject.activeSelf, "M01 must not use SPECIAL.");
            AssertImageSpritePath(instance.transform, "CommandBar/SelectButton/IconText", MatchHudIconSelectPath);
            AssertText(instance, "CommandBar/SelectButton/LabelText", "SELECT");
            AssertPixelRect(instance, "CommandBar/SelectButton", 12f, 24f, 98f, 124f, 704f, 164f);
            AssertPixelRect(instance, "CommandBar/MoveButton", 115f, 24f, 98f, 124f, 704f, 164f);
            AssertPixelRect(instance, "CommandBar/AttackButton", 218f, 24f, 98f, 124f, 704f, 164f);
            AssertPixelRect(instance, "CommandBar/StopButton", 320f, 24f, 98f, 124f, 704f, 164f);
            AssertPixelRect(instance, "CommandBar/HoldButton", 422f, 24f, 98f, 124f, 704f, 164f);
            AssertNoOverlap(instance, "CommandBar/AttackButton", "MiniMapPanel");
            AssertNoOverlap(instance, "CommandBar/HoldButton", "MiniMapPanel");
            Assert.IsFalse(instance.transform.Find("CommandBar/MoveButton").GetComponent<Button>().interactable, "M01 no-selection MOVE must be neutral/disabled.");
            Assert.IsFalse(instance.transform.Find("CommandBar/AttackButton").GetComponent<Button>().interactable, "M01 no-selection ATTACK must be neutral/disabled.");
            Assert.IsFalse(instance.transform.Find("CommandBar/SelectButton").GetComponent<Button>().interactable, "M01 no-selection SELECT must be neutral/disabled.");
            Assert.AreEqual(
                instance.transform.Find("CommandBar/StopButton").GetComponent<Image>().sprite,
                instance.transform.Find("CommandBar/MoveButton").GetComponent<Image>().sprite,
                "M01 no-selection MOVE must use neutral command chrome, not the selected/active command art.");
            Assert.Less(
                instance.transform.Find("CommandBar/CommandRailArt").GetSiblingIndex(),
                instance.transform.Find("CommandBar/StopButton").GetSiblingIndex(),
                "M01 disabled command buttons must render above the command rail chrome.");
            Assert.Less(instance.transform.Find("CommandBar/SelectButton").GetSiblingIndex(), instance.transform.Find("CommandBar/MoveButton").GetSiblingIndex());
            Assert.Less(instance.transform.Find("CommandBar/MoveButton").GetSiblingIndex(), instance.transform.Find("CommandBar/AttackButton").GetSiblingIndex());
            Assert.Less(instance.transform.Find("CommandBar/AttackButton").GetSiblingIndex(), instance.transform.Find("CommandBar/StopButton").GetSiblingIndex());
            Assert.Less(instance.transform.Find("CommandBar/StopButton").GetSiblingIndex(), instance.transform.Find("CommandBar/HoldButton").GetSiblingIndex());
            Assert.IsNull(instance.transform.Find("CommandBar/M01CommandLabel_MOVE"), "M01 command labels must come from button LabelText, not separate fallback overlays.");
            Assert.IsNull(instance.transform.Find("M01RootCommandLabel_MOVE"), "M01 must not create root-level command fallback labels over the battle scene.");
            Assert.AreEqual("Building unlocks in the next mission.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.MissionDoesNotAllowBuild));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_NonM01ScopeRestoresBuildAsAvailable()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            M01InfantryOnlyHudScopeController scope = instance.GetComponent<M01InfantryOnlyHudScopeController>();
            Assert.NotNull(scope);
            scope.Refresh();

            GameObject buildButtonRoot = instance.transform.Find("BuildButton").gameObject;
            Button buildButton = buildButtonRoot.GetComponent<Button>();
            Assert.IsTrue(buildButtonRoot.activeSelf);
            Assert.IsTrue(buildButton.interactable, "Non-M01 match HUD should keep Build available.");
            Assert.IsFalse(scope.IsM01BuildButtonDisabled());
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_HasCommandWheelOverlayHierarchy()
    {
        GameObject prefab = LoadPrefab();
        Transform wheel = prefab.transform.Find("CommandWheelCanvas");
        Assert.NotNull(wheel);
        Assert.IsFalse(wheel.gameObject.activeSelf, "Command wheel should be hidden until a command opens it.");
        Assert.NotNull(prefab.GetComponent<CommandWheelPanelController>());
        Assert.Greater(
            wheel.GetSiblingIndex(),
            prefab.transform.Find("MiniMapPanel").GetSiblingIndex(),
            "Command wheel must render above the base tactical HUD.");
        Assert.Less(
            wheel.GetSiblingIndex(),
            prefab.transform.Find("BuildDrawerCanvas").GetSiblingIndex(),
            "Build drawer should remain the highest blocking overlay when both overlays are open.");

        AssertNoImage(prefab.transform, "CommandWheelCanvas");
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/CommandHint/FillBackground", CommandWheelContextHintFillPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/CommandHint/FrameChrome", CommandWheelContextHintFramePath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/CommandHint/InfoIcon", CommandWheelHintInfoIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/SelectedEntityCard/FillBackground", CommandWheelEntityFillPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/SelectedEntityCard/FrameChrome", CommandWheelEntityFramePath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/SelectedEntityCard/UnitArt", CommandWheelBlackhawkArtPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/CenterHub/FillBackground", CommandWheelCenterFillPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/CenterHub/FrameChrome", CommandWheelCenterFramePath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/CenterHub/UnitIcon", CommandWheelBlackhawkIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/WorldTargetingRing", CommandWheelTargetingRingPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/WorldFocusBrackets", CommandWheelTargetBracketsIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/MoveSegment", CommandWheelSegmentSelectedPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/AttackSegment", CommandWheelSegmentNormalPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/ExtractSegment/IconImage", CommandWheelExtractIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/RopeDropSegment/IconImage", CommandWheelRopeDropIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/RadialCommandRoot/PatrolSegment/IconImage", CommandWheelPatrolIconPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/CloseButton", CommandWheelCloseButtonPath);
        AssertImageSpritePath(prefab.transform, "CommandWheelCanvas/CloseButton/IconImage", CommandWheelCloseIconPath);

        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/MoveSegment", "Selected", true);
        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/AttackSegment", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/ExtractSegment", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/RopeDropSegment", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/PatrolSegment", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandWheelCanvas/RadialCommandRoot/StopSegment", "Normal", false);
    }

    [Test]
    public void MatchOverlay_AssistantEntryMountsPanelControllerWithoutShowingPanel()
    {
        GameObject prefab = LoadPrefab();
        AssistantPanelController controller = prefab.GetComponent<AssistantPanelController>();
        Assert.NotNull(controller);
        Assert.NotNull(controller.PanelPrefab);
        Assert.AreEqual(AssistantPanelPrefabPath, AssetDatabase.GetAssetPath(controller.PanelPrefab));
        Assert.NotNull(controller.PanelRoot);
        Assert.AreEqual("AssistantPanelDock", controller.PanelRoot.name);
        Assert.NotNull(controller.OpenButton);
        Assert.AreEqual("AssistantEntryButton", controller.OpenButton.name);
        Assert.IsFalse(controller.IsOpen);

        AssertAnimatedTabButton(prefab, "AssistantLayer/AssistantEntryButton", "Normal", false);
        AssertText(prefab, "AssistantLayer/AssistantEntryButton/LabelText", "ARIA");
        AssertText(prefab, "AssistantLayer/AssistantEntryButton/StateText", "IDLE");
        Assert.NotNull(prefab.transform.Find("AssistantLayer/AssistantEntryButton").GetComponent<AssistantButtonView>());
        AssertFixedLeftProportionalY(prefab, "AssistantLayer/AssistantEntryButton", 14f, 316f, 190f, 96f);
        AssertFixedLeftProportionalY(prefab, "AssistantLayer/AssistantPanelDock", 674f, 90f, 660f, 620f);
    }

    [Test]
    public void MatchOverlay_AssistantEntryButtonTogglesPlaceholderPanel()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            AssistantPanelController controller = instance.GetComponent<AssistantPanelController>();
            Assert.NotNull(controller);
            var awake = typeof(AssistantPanelController).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(controller, null);

            Assert.IsFalse(controller.IsOpen);
            Button entryButton = instance.transform.Find("AssistantLayer/AssistantEntryButton").GetComponent<Button>();
            entryButton.onClick.Invoke();

            Assert.IsTrue(controller.IsOpen);
            Assert.AreEqual("placeholder.ui.assistant_panel.presentation_shell", controller.ActiveRecommendationId);
            AssistantPanelView view = controller.PanelView;
            Assert.NotNull(view);
            Assert.AreEqual("Read the objective", view.RecommendationTitleText.text);
            Assert.AreEqual("Destroy the hostile patrol and keep the command squad alive.", view.RecommendationBodyText.text);
            Assert.AreEqual("Check objective tracker", view.ChipLabels[0].text);
            Assert.IsTrue(view.ShowMeButton.interactable);
            Assert.IsFalse(view.DoItButton.interactable);
            Assert.IsFalse(view.StopButton.interactable);

            entryButton.onClick.Invoke();
            Assert.IsFalse(controller.IsOpen);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_AssistantMountDoesNotOccludeCoreHudAtTargetAspects()
    {
        GameObject prefab = LoadPrefab();

        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "ObjectivePanel");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "ThreatFeedPanel");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "SquadTray");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "CommandBar");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "ResourceBar");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantEntryButton", "MiniMapPanel");

        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "ObjectivePanel");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "ThreatFeedPanel");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "SquadTray");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "CommandBar");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "ResourceBar");
        AssertNoOverlap(prefab, "AssistantLayer/AssistantPanelDock", "MiniMapPanel");
    }

    [Test]
    public void MatchOverlay_SpecialButtonTogglesCommandWheelOverlay()
    {
        GameObject prefab = LoadPrefab();
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            CommandWheelPanelController controller = instance.GetComponent<CommandWheelPanelController>();
            Assert.NotNull(controller);
            var awake = typeof(CommandWheelPanelController).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(controller, null);

            Button specialButton = instance.transform.Find("CommandBar/SpecialButton").GetComponent<Button>();
            Button closeButton = instance.transform.Find("CommandWheelCanvas/CloseButton").GetComponent<Button>();

            Assert.IsFalse(controller.IsOpen);
            specialButton.onClick.Invoke();
            Assert.IsTrue(controller.IsOpen);
            closeButton.onClick.Invoke();
            Assert.IsFalse(controller.IsOpen);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_TacticalFeedbackClearsInvalidToastWhenCommandModeStarts()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            BattleHudTacticalFeedbackController controller = instance.GetComponent<BattleHudTacticalFeedbackController>();
            Assert.NotNull(controller);
            var awake = typeof(BattleHudTacticalFeedbackController).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(controller, null);

            Transform invalidToast = instance.transform.Find("InvalidCommandToast");
            Transform commandModeBanner = instance.transform.Find("CommandModeBanner");
            Assert.NotNull(invalidToast);
            Assert.NotNull(commandModeBanner);

            controller.ShowInvalidCommand("INVALID: Blocked route");
            Assert.IsTrue(invalidToast.gameObject.activeSelf);
            AssertText(instance, "InvalidCommandToast/MessageText", "INVALID: Blocked route");

            controller.ShowCommandMode("MOVE ORDER");
            Assert.IsFalse(
                invalidToast.gameObject.activeSelf,
                "Starting a valid command mode should clear stale invalid-command feedback.");
            Assert.IsTrue(commandModeBanner.gameObject.activeSelf);
            AssertText(instance, "CommandModeBanner/ModeText", "MOVE ORDER");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_GameplayBridgeMapsCommandResultsToTacticalFeedback()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            BattleHudGameplayBridge bridge = instance.GetComponent<BattleHudGameplayBridge>();
            Assert.NotNull(bridge);
            var awake = typeof(BattleHudGameplayBridge).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(awake);
            awake.Invoke(bridge, null);

            bridge.ApplySelection("Rifle Squad", "READY");
            Assert.IsTrue(instance.transform.Find("SelectedEntityPanel").gameObject.activeSelf);
            AssertText(instance, "SelectedEntityPanel/NameText", "Rifle Squad");
            AssertText(instance, "SelectedEntityPanel/StatusText", "READY");

            bridge.ApplyCommandMode(TacticalCommandMode.Attack);
            Assert.IsTrue(instance.transform.Find("CommandModeBanner").gameObject.activeSelf);
            AssertText(instance, "CommandModeBanner/ModeText", "ATTACK ORDER");

            bridge.ApplyCommandResult(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetBlocked));
            Assert.IsTrue(instance.transform.Find("InvalidCommandToast").gameObject.activeSelf);
            AssertText(instance, "InvalidCommandToast/MessageText", "Route is blocked.");

            bridge.ApplyCommandResult(TacticalCommandResult.Success());
            Assert.IsFalse(instance.transform.Find("InvalidCommandToast").gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_HasLayerPackManifestForProductionConversion()
    {
        string absolutePath = ResolveRepoFilePath(MatchHudLayerPackManifestPath);
        Assert.IsTrue(File.Exists(absolutePath), "MatchOverlay must keep a layer-pack manifest next to the visual target.");

        string manifest = File.ReadAllText(absolutePath);
        StringAssert.Contains("\"schema\": \"warlinecapture.ui.oneGoLayerPack.v1\"", manifest);
        StringAssert.Contains("\"screen\": \"Screen_MatchOverlay\"", manifest);
        StringAssert.Contains("\"source\"", manifest);
        StringAssert.Contains("\"reviewSheet\": \"Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png\"", manifest);
        StringAssert.Contains("\"unityDestination\": \"Assets/Game/Art/UI/Generated/MatchHUD/LayeredOneGo/Frames/resource_bar_frame.png\"", manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/ResourceBar/FrameChrome\"", manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/CommandBar/SelectButton/IconText\"", manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/CommandBar/MoveButton\"", manifest);
        StringAssert.Contains("\"m01CommandRule\"", manifest);
        StringAssert.Contains("\"SELECT\"", manifest);
        StringAssert.Contains("\"notUsedForM01\"", manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/SquadTray/Squad_Rifle/FrameChrome\"", manifest);
        StringAssert.Contains("\"role\": \"buttonState\"", manifest);
        StringAssert.Contains("\"alphaRule\": \"transparent outside button silhouette; no icon/text\"", manifest);

        string commandWheelManifestPath = ResolveRepoFilePath(CommandWheelLayerPackManifestPath);
        Assert.IsTrue(File.Exists(commandWheelManifestPath), "SCN-10 command wheel must keep a layer-pack manifest.");
        string commandWheelManifest = File.ReadAllText(commandWheelManifestPath);
        StringAssert.Contains("\"surface\": \"SCN-10_UnitCommandWheel\"", commandWheelManifest);
        StringAssert.Contains("\"screen\": \"Screen_MatchOverlay\"", commandWheelManifest);
        StringAssert.Contains("Command wheel is a hidden overlay child", commandWheelManifest);

        string m01ManifestPath = ResolveRepoFilePath(MatchHudM01LayerPackManifestPath);
        Assert.IsTrue(File.Exists(m01ManifestPath), "M01 tactical feedback state must keep a layer-pack manifest.");
        string m01Manifest = File.ReadAllText(m01ManifestPath);
        StringAssert.Contains("\"surface\": \"SCN-08_RTSBattleHUD_M01_TacticalFeedback\"", m01Manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/SelectedEntityPanel/Frame\"", m01Manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/CommandModeBanner/Frame\"", m01Manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/WorldCommandMarkerLayer/SelectionRing\"", m01Manifest);
        StringAssert.Contains("\"objectPath\": \"Screen_MatchOverlay/MiniMapPanel/MinimapCameraBridge/ViewportRect\"", m01Manifest);
    }

    [Test]
    public void MatchOverlay_UsesReusableChromeAndFixedEdgeAnchors()
    {
        GameObject prefab = LoadPrefab();

        AssertNoImage(prefab.transform, "ObjectivePanel");
        AssertImageSpritePath(prefab.transform, "ObjectivePanel/FillBackground", MatchHudObjectivePanelFillPath);
        AssertImageType(prefab.transform, "ObjectivePanel/FillBackground", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "ObjectivePanel/FrameChrome", MatchHudObjectivePanelPath);
        AssertImageType(prefab.transform, "ObjectivePanel/FrameChrome", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "ObjectivePanel/Objective_1/Checkbox", MatchHudObjectiveCheckboxPath);
        AssertImageSpritePath(prefab.transform, "ObjectivePanel/StarGoal_1/MarkerImage", MatchHudObjectiveStarPath);
        AssertNoImage(prefab.transform, "ThreatFeedPanel");
        AssertImageSpritePath(prefab.transform, "ThreatFeedPanel/FillBackground", MatchHudThreatPanelFillPath);
        AssertImageSpritePath(prefab.transform, "ThreatFeedPanel/FrameChrome", MatchHudThreatPanelPath);
        AssertNoImage(prefab.transform, "ResourceBar");
        AssertImageSpritePath(prefab.transform, "ResourceBar/FillBackground", MatchHudResourceBarFillPath);
        AssertImageType(prefab.transform, "ResourceBar/FillBackground", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "ResourceBar/FrameChrome", MatchHudResourceBarPath);
        AssertImageType(prefab.transform, "ResourceBar/FrameChrome", Image.Type.Sliced);
        AssertNoImage(prefab.transform, "MiniMapPanel");
        AssertImageSpritePath(prefab.transform, "MiniMapPanel/FillBackground", MatchHudMiniMapPanelFillPath);
        AssertImageSpritePath(prefab.transform, "MiniMapPanel/FrameChrome", MatchHudMiniMapPanelPath);
        AssertImageSpritePath(prefab.transform, "MiniMapPanel/MapImage", MatchHudIsoMiniMapContentPath);
        AssertImageSpritePath(prefab.transform, "MiniMapPanel/MinimapCameraBridge/ViewportRect", MatchHudM01MinimapViewportRectPath);
        AssertImageSpritePath(prefab.transform, "ResourceBar/MoneyCounter/IconImage", MatchHudMoneyIconPath);
        AssertImageSpritePath(prefab.transform, "ResourceBar/MaterialsCounter/IconImage", MatchHudMaterialsIconPath);
        AssertImageSpritePath(prefab.transform, "ResourceBar/PopulationCounter/IconImage", MatchHudPopulationIconPath);
        AssertImageSpritePath(prefab.transform, "ResourceBar/TimeCounter/IconImage", MatchHudTimeIconPath);
        Assert.NotNull(prefab.transform.Find("ResourceBar/Divider_MoneyMaterials_Shadow"), "ResourceBar dividers must be separate layered objects, not baked into the background.");
        Assert.NotNull(prefab.transform.Find("ResourceBar/Divider_MaterialsPopulation_Shadow"), "ResourceBar dividers must be separate layered objects, not baked into the background.");
        Assert.NotNull(prefab.transform.Find("ResourceBar/Divider_PopulationTime_Shadow"), "ResourceBar dividers must be separate layered objects, not baked into the background.");
        AssertImageSpritePath(prefab.transform, "PauseButton", MatchHudTopButtonPath);
        AssertImageType(prefab.transform, "PauseButton", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "PauseButton/IconImage", MatchHudIconPausePath);
        AssertImageSpritePath(prefab.transform, "SettingsButton", MatchHudTopButtonPath);
        AssertImageSpritePath(prefab.transform, "SettingsButton/GearIcon", MatchHudIconSettingsPath);
        AssertImageSpritePath(prefab.transform, "WorldCommandMarkerLayer/SelectionRing", MatchHudM01SelectionRingPath);
        AssertImageSpritePath(prefab.transform, "WorldCommandMarkerLayer/MoveDestinationMarker/RingImage", MatchHudM01MoveDestinationRingPath);
        AssertImageSpritePath(prefab.transform, "WorldCommandMarkerLayer/AttackTargetMarker", MatchHudM01AttackTargetRingPath);
        AssertImageSpritePath(prefab.transform, "SelectedEntityPanel/Frame", MatchHudM01SelectedEntityFramePath);
        AssertImageType(prefab.transform, "SelectedEntityPanel/Frame", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "CommandModeBanner/Frame", MatchHudM01CommandModeFramePath);
        AssertImageType(prefab.transform, "CommandModeBanner/Frame", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "InvalidCommandToast/Frame", MatchHudM01InvalidToastFramePath);
        AssertImageType(prefab.transform, "InvalidCommandToast/Frame", Image.Type.Sliced);
        AssertNoImage(prefab.transform, "SquadTray");
        AssertImageSpritePath(prefab.transform, "SquadTray/FillBackground", MatchHudSquadTrayFillPath);
        AssertImageType(prefab.transform, "SquadTray/FillBackground", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "SquadTray/FrameChrome", MatchHudSquadTrayPath);
        AssertImageType(prefab.transform, "SquadTray/FrameChrome", Image.Type.Sliced);
        AssertNoImage(prefab.transform, "CommandBar/CommandRailArt");
        AssertImageSpritePath(prefab.transform, "CommandBar/CommandRailArt/FillBackground", MatchHudCommandRailFillPath);
        AssertImageType(prefab.transform, "CommandBar/CommandRailArt/FillBackground", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "CommandBar/CommandRailArt/FrameChrome", MatchHudCommandRailPath);
        AssertImageType(prefab.transform, "CommandBar/CommandRailArt/FrameChrome", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "CommandBar/SelectButton/IconText", MatchHudIconSelectPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/StopButton/IconText", MatchHudIconStopPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/HoldButton/IconText", MatchHudIconHoldPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/MoveButton", MatchHudButtonSelectedPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/MoveButton/IconText", MatchHudIconMovePath);
        AssertImageSpritePath(prefab.transform, "CommandBar/AttackButton", MatchHudButtonNormalPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/AttackButton/IconText", MatchHudIconAttackPath);
        AssertImageSpritePath(prefab.transform, "CommandBar/SpecialButton/IconText", MatchHudIconSpecialPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Rifle", MatchHudSquadCardFillPath);
        AssertImageType(prefab.transform, "SquadTray/Squad_Rifle", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Rifle/FrameChrome", MatchHudSquadCardSelectedPath);
        AssertImageType(prefab.transform, "SquadTray/Squad_Rifle/FrameChrome", Image.Type.Sliced);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_APC", MatchHudSquadCardFillPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_APC/FrameChrome", MatchHudSquadCardNormalPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Tank", MatchHudSquadCardFillPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Tank/FrameChrome", MatchHudSquadCardNormalPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Helicopter", MatchHudSquadCardFillPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Helicopter/FrameChrome", MatchHudSquadCardNormalPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Rifle/PortraitPlate", MatchHudSquadPortraitRiflePath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_APC/PortraitPlate", MatchHudSquadPortraitApcPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Tank/PortraitPlate", MatchHudSquadPortraitTankPath);
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_Helicopter/PortraitPlate", MatchHudHelicopterPath);
        Assert.IsNull(prefab.transform.Find("SquadTray/Squad_Helicopter/StatusText"), "Accepted Air Support portrait removes the old pending-art fallback label.");
        Assert.IsFalse(
            prefab.transform.Find("SquadTray/Squad_Helicopter").GetComponent<Button>().interactable,
            "Squad_Helicopter remains disabled for M01 rules even though accepted portrait art now exists.");
        AssertImageSpritePath(prefab.transform, "SquadTray/Squad_APC/RankIcon", MatchHudIconRankChevronPath);
        AssertImageSpritePath(prefab.transform, "BuildButton", MatchHudButtonBuildPath);
        AssertImageSpritePath(prefab.transform, "BuildButton/IconText", MatchHudIconBuildPath);
        AssertNoBakedTargetCrops(prefab);

        AssertFixedLeftProportionalY(prefab, "ObjectivePanel", 8f, 7f, 358f, 282f);
        AssertFixedLeftProportionalY(prefab, "ThreatFeedPanel", 12f, 444f, 292f, 216f);
        AssertFixedLeftProportionalY(prefab, "SquadTray", 12f, 684f, 654f, 218f);
        AssertFixedLeftProportionalY(prefab, "CommandBar", 676f, 744f, 704f, 164f);
        AssertFixedRightProportionalY(prefab, "ResourceBar", 218f, 10f, 636f, 62f);
        AssertFixedRightProportionalY(prefab, "PauseButton", 110f, 12f, 88f, 66f);
        AssertFixedRightProportionalY(prefab, "SettingsButton", 10f, 12f, 88f, 66f);
        AssertFixedRightProportionalY(prefab, "MiniMapPanel", 10f, 592f, 310f, 304f);
        AssertFixedRightProportionalY(prefab, "BuildButton", 333f, 762f, 122f, 138f);
        AssertPixelRect(prefab, "MiniMapPanel/MapImage", 12f, 28f, 280f, 262f, 310f, 304f);
        AssertPixelRect(prefab, "SquadTray/Squad_Rifle", 10f, 6f, 176f, 218f, 654f, 218f);
        AssertPixelRect(prefab, "SquadTray/Squad_APC", 188f, 10f, 158f, 214f, 654f, 218f);
        AssertPixelRect(prefab, "SquadTray/Squad_Tank", 348f, 10f, 156f, 214f, 654f, 218f);
        AssertPixelRect(prefab, "SquadTray/Squad_Helicopter", 506f, 10f, 158f, 214f, 654f, 218f);
        AssertPixelRect(prefab, "CommandBar/SelectButton", 12f, 24f, 98f, 124f, 704f, 164f);
        AssertPixelRect(prefab, "CommandBar/StopButton", 12f, 24f, 98f, 124f, 704f, 164f);
        AssertPixelRect(prefab, "CommandBar/HoldButton", 115f, 24f, 98f, 124f, 704f, 164f);
        AssertPixelRect(prefab, "CommandBar/MoveButton", 218f, 24f, 98f, 124f, 704f, 164f);
        AssertPixelRect(prefab, "CommandBar/AttackButton", 320f, 24f, 98f, 124f, 704f, 164f);
        AssertPixelRect(prefab, "CommandBar/SpecialButton", 422f, 24f, 98f, 124f, 704f, 164f);
        AssertNoOverlap(prefab, "CommandBar/AttackButton", "MiniMapPanel");
        AssertNoOverlap(prefab, "CommandBar/SpecialButton", "MiniMapPanel");
        Assert.Less(
            prefab.transform.Find("MiniMapPanel/FrameChrome").GetSiblingIndex(),
            prefab.transform.Find("MiniMapPanel/MapImage").GetSiblingIndex(),
            "MiniMapPanel FrameChrome is a clean solid backplate and must render below separate map content.");
        Assert.Less(
            prefab.transform.Find("ObjectivePanel/FrameChrome").GetSiblingIndex(),
            prefab.transform.Find("ObjectivePanel/SectionTitleText").GetSiblingIndex(),
            "ObjectivePanel FrameChrome is a clean solid backplate and must render below text/content.");
        Assert.Less(
            prefab.transform.Find("ThreatFeedPanel/FrameChrome").GetSiblingIndex(),
            prefab.transform.Find("ThreatFeedPanel/SectionTitleText").GetSiblingIndex(),
            "ThreatFeedPanel FrameChrome is a clean solid backplate and must render below text/content.");
        AssertPixelRect(prefab, "CommandBar/StopButton/IconText", 22f, 18f, 54f, 54f, 98f, 124f);
        AssertPixelRect(prefab, "CommandBar/StopButton/LabelText", 4f, 85f, 90f, 28f, 98f, 124f);
        AssertPixelRect(prefab, "CommandBar/MoveButton/IconText", 22f, 18f, 54f, 54f, 98f, 124f);
        AssertPixelRect(prefab, "CommandBar/MoveButton/LabelText", 4f, 85f, 90f, 28f, 98f, 124f);
        AssertPixelRect(prefab, "PauseButton/IconImage", 22f, 12f, 44f, 42f, 88f, 66f);
        AssertPixelRect(prefab, "SettingsButton/GearIcon", 16f, 5f, 56f, 56f, 88f, 66f);
    }

    [Test]
    public void MatchOverlay_UsesTransparentLayeredChromeAndAnimatedCommandTabs()
    {
        GameObject prefab = LoadPrefab();

        AssertAnimatedTabButton(prefab, "CommandBar/StopButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandBar/HoldButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandBar/MoveButton", "Selected", true);
        AssertAnimatedTabButton(prefab, "CommandBar/AttackButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "CommandBar/SpecialButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "SquadTray/Squad_Rifle", "Selected", true);
        AssertAnimatedTabButton(prefab, "SquadTray/Squad_APC", "Normal", false);
        AssertAnimatedTabButton(prefab, "SquadTray/Squad_Tank", "Normal", false);
        AssertAnimatedTabButton(prefab, "SquadTray/Squad_Helicopter", "Normal", false);

        foreach (string path in new[]
        {
            MatchHudResourceBarPath,
            MatchHudResourceBarFillPath,
            MatchHudObjectivePanelPath,
            MatchHudObjectivePanelFillPath,
            MatchHudThreatPanelPath,
            MatchHudThreatPanelFillPath,
            MatchHudMiniMapPanelPath,
            MatchHudMiniMapPanelFillPath,
            MatchHudCommandRailPath,
            MatchHudCommandRailFillPath,
            MatchHudSquadTrayPath,
            MatchHudSquadTrayFillPath,
            MatchHudSquadCardNormalPath,
            MatchHudSquadCardSelectedPath,
            MatchHudButtonNormalPath,
            MatchHudButtonSelectedPath,
            MatchHudButtonBuildPath,
            MatchHudTopButtonPath,
            CommandWheelContextHintFramePath,
            CommandWheelContextHintFillPath,
            CommandWheelEntityFramePath,
            CommandWheelEntityFillPath,
            CommandWheelSegmentNormalPath,
            CommandWheelSegmentSelectedPath,
            CommandWheelCloseButtonPath
        })
        {
            AssertTextureCornersTransparent(path, $"{path} must have transparent outside corners.");
        }

        AssertTextureCenterAlpha(MatchHudResourceBarPath, 0f, "Resource bar frame center must be transparent because FillBackground is a separate layer.");
        AssertTextureCenterAlphaGreater(MatchHudResourceBarFillPath, 0.90f, "Resource bar fill center must be the fill layer, not the frame layer.");
        AssertTextureCenterAlpha(MatchHudObjectivePanelPath, 0f, "ObjectivePanel frame center must be transparent because FillBackground is separate.");
        AssertTextureCenterAlphaGreater(MatchHudObjectivePanelFillPath, 0.90f, "ObjectivePanel fill center must be opaque enough to read text.");
        AssertTextureCenterAlpha(MatchHudThreatPanelPath, 0f, "ThreatFeedPanel frame center must be transparent because FillBackground is separate.");
        AssertTextureCenterAlphaGreater(MatchHudThreatPanelFillPath, 0.90f, "ThreatFeedPanel fill center must be opaque enough to read text.");
        AssertTextureCenterAlpha(MatchHudMiniMapPanelPath, 0f, "MiniMapPanel frame center must be transparent because map content is separate.");
        AssertTextureCenterAlphaGreater(MatchHudMiniMapPanelFillPath, 0.90f, "MiniMapPanel fill center must be opaque enough below map content.");
        AssertTextureCenterAlpha(MatchHudCommandRailPath, 0f, "Command rail frame center must be transparent because FillBackground is separate.");
        AssertTextureCenterAlphaGreater(MatchHudCommandRailFillPath, 0.90f, "Command rail fill center must be the fill layer, not the frame layer.");
        AssertTextureCenterAlphaGreater(MatchHudSquadTrayFillPath, 0.90f, "SquadTray fill center must be visible and separate from cards.");
        AssertTextureCenterAlphaGreater(MatchHudSquadCardNormalPath, 0.10f, "Accepted squad card chrome carries target-quality body/well detail while keeping outside corners transparent.");
        AssertTextureCenterAlphaGreater(MatchHudSquadCardSelectedPath, 0.10f, "Accepted selected squad card chrome carries target-quality body/well detail while keeping outside corners transparent.");
        AssertTextureCenterAlphaGreater(MatchHudSquadCardFillPath, 0.90f, "Squad card fallback fill center must be visible but icon-free.");
        AssertTextureCenterAlphaGreater(MatchHudButtonNormalPath, 0.90f, "Normal command button body must be visible with icon/text separate.");
        AssertTextureCenterAlphaGreater(MatchHudButtonSelectedPath, 0.90f, "Selected command button body must be visible with icon/text separate.");

        foreach (string path in new[]
        {
            MatchHudIconStopPath,
            MatchHudIconHoldPath,
            MatchHudIconSelectPath,
            MatchHudIconMovePath,
            MatchHudIconAttackPath,
            MatchHudIconSpecialPath,
            MatchHudIconBuildPath,
            MatchHudIconRankChevronPath,
            MatchHudMoneyIconPath,
            MatchHudMaterialsIconPath,
            MatchHudPopulationIconPath,
            MatchHudTimeIconPath,
            CommandWheelExtractIconPath,
            CommandWheelRopeDropIconPath,
            CommandWheelPatrolIconPath,
            CommandWheelHintInfoIconPath,
            CommandWheelCloseIconPath,
            CommandWheelTargetBracketsIconPath,
            CommandWheelBlackhawkIconPath
        })
        {
            AssertTextureCornersTransparent(path, $"{path} must be an icon-only transparent sprite.");
        }
    }

    [Test]
    public void MatchOverlay_ResourceBarMatchesTargetSpacingAndReadableText()
    {
        GameObject prefab = LoadPrefab();

        AssertPixelRect(prefab, "ResourceBar/MoneyCounter", 22f, 10f, 126f, 42f, 636f, 62f);
        AssertPixelRect(prefab, "ResourceBar/MaterialsCounter", 178f, 10f, 130f, 42f, 636f, 62f);
        AssertPixelRect(prefab, "ResourceBar/PopulationCounter", 338f, 10f, 138f, 42f, 636f, 62f);
        AssertPixelRect(prefab, "ResourceBar/TimeCounter", 498f, 10f, 126f, 42f, 636f, 62f);

        foreach (string path in new[] { "MoneyCounter", "MaterialsCounter", "PopulationCounter", "TimeCounter" })
        {
            TMP_Text text = prefab.transform.Find($"ResourceBar/{path}/ValueText").GetComponent<TMP_Text>();
            Assert.AreEqual(TextAlignmentOptions.Left, text.alignment, $"ResourceBar/{path}/ValueText");
            Assert.LessOrEqual(text.fontSizeMax, 21f, $"ResourceBar/{path}/ValueText");
            Assert.GreaterOrEqual(text.fontSizeMin, 18f, $"ResourceBar/{path}/ValueText");
        }
    }

    [Test]
    public void MatchOverlay_PreviewTextMatchesTacticalHudTarget()
    {
        GameObject prefab = LoadPrefab();

        AssertText(prefab, "ObjectivePanel/SectionTitleText", "OBJECTIVES");
        AssertText(prefab, "ObjectivePanel/Objective_1/LabelText", "Capture the Forward HQ");
        AssertText(prefab, "ThreatFeedPanel/Threat_1/LabelText", "Enemy Air Detected");
        AssertText(prefab, "ResourceBar/MoneyCounter/ValueText", "2,450");
        AssertText(prefab, "SquadTray/Squad_Rifle/TitleText", "RIFLE SQUAD");
        AssertText(prefab, "CommandBar/MoveButton/LabelText", "MOVE");
        AssertText(prefab, "BuildButton/LabelText", "BUILD");

        foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            bool noWrap = text.textWrappingMode == TextWrappingModes.NoWrap ||
                text.textWrappingMode == TextWrappingModes.PreserveWhitespaceNoWrap;
            Assert.IsTrue(noWrap, GetHierarchyPath(text.transform));
        }
    }

    [Test]
    public void MatchOverlay_ObjectivePanelBindsActiveMissionRuntimeState()
    {
        WarlineCaptureMissionSession.BeginMission("saga.ch01.m02.establish_base", WarlineCaptureRoute.SagaMap);
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            MatchObjectivePanelController controller = instance.GetComponent<MatchObjectivePanelController>();
            Assert.NotNull(controller);

            controller.RefreshForTests();
            TMP_Text objective1 = instance.transform.Find("ObjectivePanel/Objective_1/LabelText").GetComponent<TMP_Text>();
            TMP_Text objective2 = instance.transform.Find("ObjectivePanel/Objective_2/LabelText").GetComponent<TMP_Text>();
            TMP_Text objective3 = instance.transform.Find("ObjectivePanel/Objective_3/LabelText").GetComponent<TMP_Text>();
            TMP_Text starGoal1 = instance.transform.Find("ObjectivePanel/StarGoal_1/LabelText").GetComponent<TMP_Text>();

            StringAssert.Contains("Build the first operations outpost", objective1.text);
            StringAssert.Contains("0 / 1", objective1.text);
            StringAssert.Contains("Defeat the first attack group", objective2.text);
            StringAssert.Contains("0 / 8", objective2.text);
            Assert.IsFalse(objective3.transform.parent.gameObject.activeSelf);
            StringAssert.Contains("Build two support structures", starGoal1.text);

            GameRuntimeStats.RecordBuildingBuilt();
            for (int i = 0; i < 8; i++)
                GameRuntimeStats.RecordMilitaryDeath(1);

            controller.RefreshForTests();
            StringAssert.Contains("1 / 1", objective1.text);
            StringAssert.Contains("8 / 8", objective2.text);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MatchOverlay_ObjectivePanelKeepsFallbackTextWithoutActiveMission()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            MatchObjectivePanelController controller = instance.GetComponent<MatchObjectivePanelController>();
            Assert.NotNull(controller);
            controller.RefreshForTests();

            AssertText(instance, "ObjectivePanel/Objective_1/LabelText", "Capture the Forward HQ");
            AssertText(instance, "ObjectivePanel/StarGoal_1/LabelText", "Complete Mission");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatchOverlayPrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        string[] candidates =
        {
            Path.GetFullPath(relativePath),
            string.IsNullOrEmpty(projectRoot) ? relativePath : Path.Combine(projectRoot, relativePath),
            string.IsNullOrEmpty(projectRoot) ? relativePath : Path.GetFullPath(Path.Combine(projectRoot, "..", "WarlineCapture", relativePath)),
            Path.GetFullPath(Path.Combine("..", "WarlineCapture", relativePath))
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return candidates[0];
    }

    private static void AssertChildren(GameObject prefab, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(prefab.transform.Find(path), $"Missing {path}");
    }

    private static void AssertImageSpritePath(Transform root, string path, string expectedSpritePath)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertImageType(Transform root, string path, Image.Type expectedType)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.AreEqual(expectedType, image.type, path);
    }

    private static void AssertNoImage(Transform root, string path)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Assert.IsNull(target.GetComponent<Image>(), $"{path} must not merge fill/content and frame/chrome into one root Image.");
    }

    private static void AssertAnimatedTabButton(GameObject prefab, string path, string expectedInitialState, bool shouldSelectWithEventSystem)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);

        Button button = target.GetComponent<Button>();
        Assert.NotNull(button, path);
        Assert.AreEqual(Selectable.Transition.Animation, button.transition, $"{path} must use Animation transition instead of Color Tint.");
        Assert.AreEqual("Normal", button.animationTriggers.normalTrigger, path);
        Assert.AreEqual("Highlighted", button.animationTriggers.highlightedTrigger, path);
        Assert.AreEqual("Pressed", button.animationTriggers.pressedTrigger, path);
        Assert.AreEqual("Selected", button.animationTriggers.selectedTrigger, path);
        Assert.AreEqual("Disabled", button.animationTriggers.disabledTrigger, path);

        Animator animator = target.GetComponent<Animator>();
        Assert.NotNull(animator, $"{path} must have an Animator.");
        Assert.NotNull(animator.runtimeAnimatorController, $"{path} must use the shared button animator controller.");
        Assert.AreEqual(UiButtonAnimatorControllerPath, AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), path);

        WarlineCaptureButtonAnimationState initialState = target.GetComponent<WarlineCaptureButtonAnimationState>();
        Assert.NotNull(initialState, $"{path} must keep its authored initial animation state.");
        var serializedState = new SerializedObject(initialState);
        Assert.AreEqual(expectedInitialState, serializedState.FindProperty("initialStateName").stringValue, path);
        Assert.AreEqual(shouldSelectWithEventSystem, serializedState.FindProperty("selectWithEventSystem").boolValue, path);
    }

    private static void AssertTextureAlpha(string assetPath, int x, int y, float expectedAlpha, string message)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Assert.IsTrue(File.Exists(absolutePath), assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath)), assetPath);
            Assert.AreEqual(expectedAlpha, texture.GetPixel(x, y).a, 0.01f, message);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void AssertTextureAlphaGreater(string assetPath, int x, int y, float minimumAlpha, string message)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Assert.IsTrue(File.Exists(absolutePath), assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath)), assetPath);
            Assert.GreaterOrEqual(texture.GetPixel(x, y).a, minimumAlpha, message);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void AssertTextureCornersTransparent(string assetPath, string message)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Assert.IsTrue(File.Exists(absolutePath), assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath)), assetPath);
            Assert.AreEqual(0f, texture.GetPixel(0, 0).a, 0.01f, message);
            Assert.AreEqual(0f, texture.GetPixel(texture.width - 1, 0).a, 0.01f, message);
            Assert.AreEqual(0f, texture.GetPixel(0, texture.height - 1).a, 0.01f, message);
            Assert.AreEqual(0f, texture.GetPixel(texture.width - 1, texture.height - 1).a, 0.01f, message);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void AssertTextureCenterAlpha(string assetPath, float expectedAlpha, string message)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Assert.IsTrue(File.Exists(absolutePath), assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath)), assetPath);
            Assert.AreEqual(expectedAlpha, texture.GetPixel(texture.width / 2, texture.height / 2).a, 0.01f, message);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void AssertTextureCenterAlphaGreater(string assetPath, float minimumAlpha, string message)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Assert.IsTrue(File.Exists(absolutePath), assetPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(absolutePath)), assetPath);
            Assert.GreaterOrEqual(texture.GetPixel(texture.width / 2, texture.height / 2).a, minimumAlpha, message);
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void AssertNoBakedTargetCrops(GameObject prefab)
    {
        foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null)
                continue;

            string path = AssetDatabase.GetAssetPath(image.sprite);
            Assert.IsFalse(path.Contains("_Target"), $"{GetHierarchyPath(image.transform)} uses baked target crop {path}");
            Assert.IsFalse(
                path.Contains("Assets/Game/Art/UI/Generated/MatchHUD/Cards/MatchHUD_Portrait_"),
                $"{GetHierarchyPath(image.transform)} uses old HUD target-cropped gameplay art instead of 2D isometric content: {path}");
            Assert.AreNotEqual(
                "Assets/Game/Art/UI/Generated/MatchHUD/Frames/MatchHUD_MiniMap_Content.png",
                path,
                $"{GetHierarchyPath(image.transform)} uses the old tactical HUD minimap crop instead of 2D isometric minimap content.");
        }
    }

    private static void AssertText(GameObject prefab, string path, string expected)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expected, text.text, path);
    }

    private static void AssertFixedLeftProportionalY(GameObject prefab, string path, float left, float top, float width, float height)
    {
        RectTransform rect = prefab.transform.Find(path) as RectTransform;
        Assert.NotNull(rect, path);
        Assert.AreEqual(0f, rect.anchorMin.x, 0.0001f, path);
        Assert.AreEqual(0f, rect.anchorMax.x, 0.0001f, path);
        Assert.AreEqual(left, rect.offsetMin.x, 0.0001f, path);
        Assert.AreEqual(left + width, rect.offsetMax.x, 0.0001f, path);
        Assert.AreEqual(1f - (top + height) / ReferenceSize.y, rect.anchorMin.y, 0.0001f, path);
        Assert.AreEqual(1f - top / ReferenceSize.y, rect.anchorMax.y, 0.0001f, path);
    }

    private static void AssertFixedRightProportionalY(GameObject prefab, string path, float right, float top, float width, float height)
    {
        RectTransform rect = prefab.transform.Find(path) as RectTransform;
        Assert.NotNull(rect, path);
        Assert.AreEqual(1f, rect.anchorMin.x, 0.0001f, path);
        Assert.AreEqual(1f, rect.anchorMax.x, 0.0001f, path);
        Assert.AreEqual(-(right + width), rect.offsetMin.x, 0.0001f, path);
        Assert.AreEqual(-right, rect.offsetMax.x, 0.0001f, path);
        Assert.AreEqual(1f - (top + height) / ReferenceSize.y, rect.anchorMin.y, 0.0001f, path);
        Assert.AreEqual(1f - top / ReferenceSize.y, rect.anchorMax.y, 0.0001f, path);
    }

    private static void AssertPixelRect(GameObject prefab, string path, float x, float yFromTop, float width, float height, float referenceWidth, float referenceHeight)
    {
        RectTransform rect = prefab.transform.Find(path) as RectTransform;
        Assert.NotNull(rect, path);
        Assert.AreEqual(x / referenceWidth, rect.anchorMin.x, 0.0001f, path);
        Assert.AreEqual((x + width) / referenceWidth, rect.anchorMax.x, 0.0001f, path);
        Assert.AreEqual(1f - (yFromTop + height) / referenceHeight, rect.anchorMin.y, 0.0001f, path);
        Assert.AreEqual(1f - yFromTop / referenceHeight, rect.anchorMax.y, 0.0001f, path);
        Assert.AreEqual(Vector2.zero, rect.offsetMin, path);
        Assert.AreEqual(Vector2.zero, rect.offsetMax, path);
    }

    private static void AssertNoOverlap(GameObject prefab, string firstPath, string secondPath)
    {
        Rect first = GetReferenceRect(prefab, firstPath);
        Rect second = GetReferenceRect(prefab, secondPath);
        Assert.IsFalse(first.Overlaps(second), $"{firstPath} overlaps {secondPath}: {first} vs {second}");
    }

    private static Rect GetReferenceRect(GameObject prefab, string path)
    {
        RectTransform rect = prefab.transform.Find(path) as RectTransform;
        Assert.NotNull(rect, path);
        return GetReferenceRect(rect);
    }

    private static Rect GetReferenceRect(RectTransform rect)
    {
        Rect parentRect = new(0f, 0f, ReferenceSize.x, ReferenceSize.y);
        if (rect.parent is RectTransform parent && parent.parent != null)
            parentRect = GetReferenceRect(parent);

        float left = parentRect.xMin + rect.anchorMin.x * parentRect.width + rect.offsetMin.x;
        float right = parentRect.xMin + rect.anchorMax.x * parentRect.width + rect.offsetMax.x;
        float bottom = parentRect.yMin + rect.anchorMin.y * parentRect.height + rect.offsetMin.y;
        float top = parentRect.yMin + rect.anchorMax.y * parentRect.height + rect.offsetMax.y;
        return Rect.MinMaxRect(left, bottom, right, top);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }
}
