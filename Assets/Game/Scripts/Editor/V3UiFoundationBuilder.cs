using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class V3UiFoundationBuilder
    {
        internal const string SharedRoot = "Assets/Game/Art/UI/V3Shared";
        internal const string CatalogPath = SharedRoot + "/Config/V3UiArtCatalog.asset";
        internal const string ThemePath = SharedRoot + "/Config/V3UiTheme.asset";
        internal const string CoreAtlasPath = SharedRoot + "/Atlases/UI_V3_CoreChrome_01.spriteatlas";
        internal const string BrandAtlasPath = SharedRoot + "/Atlases/UI_V3_Brand_01.spriteatlas";
        internal const string BrandLogoPrefabPath = SharedRoot + "/Prefabs/UI_V3_MainMenuLogo.prefab";
        internal const string IconAtlasPath = SharedRoot + "/Atlases/UI_V3_CoreIcons_01.spriteatlas";
        internal const string CommanderIconAtlasPath = SharedRoot + "/Atlases/UI_V3_CommanderIcons_01.spriteatlas";
        internal const string CampaignIconAtlasPath = SharedRoot + "/Atlases/UI_V3_CampaignIcons_01.spriteatlas";
        internal const string EquipmentIconAtlasPath = SharedRoot + "/Atlases/UI_V3_EquipmentIcons_01.spriteatlas";
        internal const string OperationsIconAtlasPath = SharedRoot + "/Atlases/UI_V3_OperationsIcons_01.spriteatlas";
        internal const string FirstLaunchIconAtlasPath = SharedRoot + "/Atlases/UI_V3_FirstLaunchIcons_01.spriteatlas";
        internal const string MatchIconAtlasPath = SharedRoot + "/Atlases/UI_V3_MatchIcons_01.spriteatlas";
        private const string MatchV3IconRoot = "Assets/Game/Art/UI/Generated/V3Shared/Icons";
        private const string MatchAlignedCommandRoot =
            "Assets/Game/Art/UI/Generated/V3Shared/MatchCommandsAligned";

        internal const string PanelPath = SharedRoot + "/Sprites/Core/ui_core_panel_9s.png";
        internal const string ButtonPath = SharedRoot + "/Sprites/Core/ui_core_button_9s.png";
        internal const string FocusOverlayPath = SharedRoot + "/Sprites/Core/ui_core_focus_overlay_9s.png";
        internal const string MainMenuLogoPath = SharedRoot + "/Sprites/Brand/ui_v3_brand_logo_mainmenu.png";
        internal const string GreenGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_green.png";
        internal const string RedGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_red.png";
        internal const string AmberGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_amber.png";
        internal const string BlueGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_blue.png";
        internal const string CyanGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_cyan.png";
        internal const string GraphiteGradientPath = SharedRoot + "/Sprites/Core/Gradients/ui_v3_gradient_graphite.png";
        internal const string AttackIconPath = SharedRoot + "/Sprites/Icons/ui_icon_attack.png";
        internal const string SettingsIconPath = SharedRoot + "/Sprites/Icons/Settings/ui_icon_settings_gear.png";
        internal const string SettingsAudioIconPath = SharedRoot + "/Sprites/Icons/Settings/ui_icon_settings_audio.png";
        internal const string SettingsVideoIconPath = SharedRoot + "/Sprites/Icons/Settings/ui_icon_settings_video.png";
        internal const string SettingsAccessibilityIconPath = SharedRoot + "/Sprites/Icons/Settings/ui_icon_settings_accessibility.png";
        internal const string ResetIconPath = SharedRoot + "/Sprites/Icons/Settings/ui_icon_settings_reset.png";
        internal const string CommanderRankIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_02_commander_rank_shield.png";
        internal const string CommanderCrateIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_04_supplies_crate.png";
        internal const string CommanderBackIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png";
        internal const string CommanderEditIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_09_edit_pencil.png";
        internal const string CommanderBadgeIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png";
        internal const string CommanderRosterIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_11_roster_group.png";
        internal const string CommanderVehicleIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_12_vehicle.png";
        internal const string CommanderSupportIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_14_support_plus.png";
        internal const string CommanderRewardIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_15_reward_wreath.png";
        internal const string CommanderClaimIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_20_claim_chevron.png";
        internal const string CommanderHistoryIconPath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_icon_09_timer_clock.png";
        internal const string CommanderUpgradesIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_upgrades_chevrons.png";
        internal const string CommanderLockIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_lock.png";
        internal const string CommanderHeaderStarIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_objective_star.png";
        internal const string CommanderCheckIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_checkbox_checked.png";
        internal const string CampaignBarracksIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_barracks.png";
        internal const string CampaignSquadIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_squad_group.png";
        internal const string CampaignHoldIconPath = "Assets/Game/Art/UI/Icons/scn08_command_hold_shield.png";
        internal const string CampaignChaptersIconPath = SharedRoot + "/Sprites/Icons/ui_icon_chapters_book.png";
        internal const string CampaignLaunchIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_deploy_chevrons.png";
        internal const string CampaignNodeClaimedPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_17_reward_node_claimed.png";
        internal const string CampaignNodeActivePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png";
        internal const string CampaignNodeLockedPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_19_reward_node_locked.png";
        internal const string MissionCivilianIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_civilian_group.png";
        internal const string MissionIntelIconPath = "Assets/Game/Art/UI/Icons/scn08_command_scan_radar.png";
        internal const string MissionVisibilityIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_shield_rank_badge.png";
        internal const string MissionVehicleIconPath = "Assets/Game/Art/UI/Icons/scn08_command_board_vehicle.png";
        internal const string MissionStarIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_objective_star.png";
        internal const string MissionEnemyIconPath = MatchV3IconRoot + "/v3_icon_hostile_marker.png";
        internal const string MissionAirIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_aircraft_helicopter.png";
        internal const string MissionRadioIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_comms_signal.png";
        internal const string EquipmentAircraftIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_airstrike.png";
        internal const string EquipmentHealthIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_medic_drop.png";
        internal const string EquipmentEmpIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_emp_blast.png";
        internal const string EquipmentLockIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_lock.png";
        internal const string EquipmentArmorIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_armor_plate.png";
        internal const string EquipmentTargetingIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_targeting_module.png";
        internal const string EquipmentAmmoIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_ammo_crate.png";
        internal const string EquipmentRepairIconPath = SharedRoot + "/Sprites/Equipment/ui_equipment_repair_kit.png";
        internal const string OperationsHeatIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_damage_burst.png";
        internal const string OperationsIntelIconPath = MissionIntelIconPath;
        internal const string OperationsPatrolIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_patrol_chevrons.png";
        internal const string OperationsRepairIconPath = EquipmentRepairIconPath;
        internal const string OperationsArmoryIconPath = EquipmentAmmoIconPath;
        internal const string OperationsRaidIconPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_skirmish_crossed_weapons_icon.png";
        internal const string OperationsWarningIconPath = MatchV3IconRoot + "/v3_icon_warning.png";
        internal const string OperationsDroneIconPath = MatchAlignedCommandRoot + "/v3_match_command_support.png";
        internal const string OperationsMapPinIconPath = MatchV3IconRoot + "/v3_icon_locate.png";
        internal const string OperationsMapPinUnderlayPath = MatchV3IconRoot + "/v3_icon_friendly_marker.png";
        internal const string OperationsTankIconPath = MissionVehicleIconPath;
        internal const string OperationsAidIconPath = MatchV3IconRoot + "/v3_icon_medical.png";
        internal const string OperationsTimeIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_time_clock.png";
        internal const string FirstLaunchGlobeRingPath = MatchV3IconRoot + "/v3_icon_info.png";
        internal const string FirstLaunchMapIconPath = MatchV3IconRoot + "/v3_icon_locate.png";
        internal const string FirstLaunchPauseIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_pause.png";
        internal const string FirstLaunchTargetIconPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_campaign_target_icon.png";
        internal const string FirstLaunchMotionIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_move_runner.png";
        internal const string FirstLaunchAriaPortraitPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_aria_v3.png";
        internal const string SharedAriaPortraitPath = FirstLaunchAriaPortraitPath;
        // SCN-08 owns one new canonical command set. These target-aligned V3
        // sources are packed once into the dedicated Match atlas for every use.
        internal const string MatchSelectIconPath = MatchAlignedCommandRoot + "/v3_match_command_select.png";
        internal const string MatchMoveIconPath = MatchAlignedCommandRoot + "/v3_match_command_move.png";
        internal const string MatchAttackIconPath = MatchAlignedCommandRoot + "/v3_match_command_attack.png";
        internal const string MatchHoldIconPath = MatchAlignedCommandRoot + "/v3_match_command_hold.png";
        internal const string MatchStopIconPath = MatchAlignedCommandRoot + "/v3_match_command_stop.png";
        internal const string MatchScanIconPath = MatchAlignedCommandRoot + "/v3_match_command_scan.png";
        internal const string MatchSupportIconPath = MatchAlignedCommandRoot + "/v3_match_command_support.png";
        internal const string MatchBuildIconPath = MatchAlignedCommandRoot + "/v3_match_command_build.png";
        internal const string MatchExtractIconPath = MatchV3IconRoot + "/v3_icon_extract.png";
        internal const string MatchRopeDropIconPath = MatchV3IconRoot + "/v3_icon_rope_drop.png";
        internal const string MatchPatrolIconPath = MatchV3IconRoot + "/v3_icon_patrol.png";
        internal const string MatchReturnIconPath = MatchV3IconRoot + "/v3_icon_return.png";
        internal const string MatchDestroyIconPath = MatchV3IconRoot + "/v3_icon_destroy.png";
        internal const string MatchBoardIconPath = MatchV3IconRoot + "/v3_icon_board.png";
        internal const string MatchCameraIconPath = MatchV3IconRoot + "/v3_icon_camera.png";
        internal const string MatchInfoIconPath = MatchV3IconRoot + "/v3_icon_info.png";
        internal const string MatchRankBadgeIconPath = MatchV3IconRoot + "/v3_icon_rank_badge.png";
        internal const string MatchMaterialsIconPath = MatchV3IconRoot + "/v3_icon_materials.png";
        internal const string MatchOilIconPath = MatchV3IconRoot + "/v3_icon_oil.png";
        internal const string MatchFuelIconPath = MatchV3IconRoot + "/v3_icon_fuel.png";
        internal const string MatchCiviliansIconPath = MatchV3IconRoot + "/v3_icon_civilians.png";
        internal const string MatchSettingsIconPath = MatchV3IconRoot + "/v3_icon_settings.png";
        internal const string MatchPauseIconPath = MatchV3IconRoot + "/v3_icon_pause.png";
        internal const string MatchInvalidIconPath = MatchV3IconRoot + "/v3_icon_warning.png";
        internal const string MatchJumpIconPath = MatchV3IconRoot + "/v3_icon_locate.png";
        internal const string MatchPlayerIconPath = MatchV3IconRoot + "/v3_icon_player.png";
        internal const string MatchArmorIconPath = MatchV3IconRoot + "/v3_icon_armor_star.png";
        internal const string MatchSpeedIconPath = MatchV3IconRoot + "/v3_icon_speed.png";
        internal const string MatchAirTransportIconPath = MatchV3IconRoot + "/v3_icon_air_transport.png";
        internal const string MatchFriendlyMarkerIconPath = MatchV3IconRoot + "/v3_icon_friendly_marker.png";
        internal const string MatchHostileMarkerIconPath = MatchV3IconRoot + "/v3_icon_hostile_marker.png";
        internal const string MatchMedicalIconPath = MatchV3IconRoot + "/v3_icon_medical.png";
        internal const string PauseResumeIconPath = MatchV3IconRoot + "/v3_icon_resume.png";
        internal const string PauseHelpIconPath = MatchV3IconRoot + "/v3_icon_help.png";
        internal const string PauseExitIconPath = MatchV3IconRoot + "/v3_icon_exit.png";
        internal const string AbilitySourceIconPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Inventory/ICON_MilitaryCombat_Inventory_Notes_01_Clean.png";
        internal const string AbilityAvailabilityIconPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Map/ICON_MilitaryCombat_Map_Pin_01_Clean.png";

        private static readonly string[] CommanderIconPaths =
        {
            CommanderRankIconPath,
            CommanderCrateIconPath,
            CommanderBackIconPath,
            CommanderEditIconPath,
            CommanderBadgeIconPath,
            CommanderRosterIconPath,
            CommanderVehicleIconPath,
            CommanderSupportIconPath,
            CommanderRewardIconPath,
            CommanderClaimIconPath,
            CommanderHistoryIconPath,
            CommanderUpgradesIconPath,
            CommanderLockIconPath,
            CommanderHeaderStarIconPath,
            CommanderCheckIconPath
        };

        private static readonly string[] CampaignIconPaths =
        {
            CampaignBarracksIconPath,
            CampaignSquadIconPath,
            CampaignHoldIconPath,
            CampaignChaptersIconPath,
            CampaignLaunchIconPath,
            CampaignNodeClaimedPath,
            CampaignNodeActivePath,
            CampaignNodeLockedPath,
            MissionCivilianIconPath,
            MissionIntelIconPath,
            MissionVisibilityIconPath,
            MissionVehicleIconPath,
            MissionStarIconPath,
            MissionAirIconPath,
            MissionRadioIconPath
        };

        private static readonly string[] EquipmentIconPaths =
        {
            EquipmentAircraftIconPath,
            EquipmentHealthIconPath,
            EquipmentEmpIconPath,
            EquipmentLockIconPath,
            EquipmentArmorIconPath,
            EquipmentTargetingIconPath,
            EquipmentAmmoIconPath,
            EquipmentRepairIconPath
        };

        private static readonly string[] OperationsIconPaths =
        {
            OperationsHeatIconPath,
            OperationsPatrolIconPath,
            OperationsRaidIconPath,
            OperationsTimeIconPath
        };

        private static readonly string[] FirstLaunchIconPaths =
        {
            FirstLaunchTargetIconPath,
            FirstLaunchMotionIconPath
        };

        private static readonly string[] MatchIconPaths =
        {
            MatchSelectIconPath,
            MatchMoveIconPath,
            MatchAttackIconPath,
            MatchHoldIconPath,
            MatchStopIconPath,
            MatchScanIconPath,
            MatchBuildIconPath,
            MatchSupportIconPath,
            MatchExtractIconPath,
            MatchRopeDropIconPath,
            MatchPatrolIconPath,
            MatchReturnIconPath,
            MatchDestroyIconPath,
            MatchBoardIconPath,
            MatchCameraIconPath,
            MatchInfoIconPath,
            MatchRankBadgeIconPath,
            MatchMaterialsIconPath,
            MatchOilIconPath,
            MatchFuelIconPath,
            MatchCiviliansIconPath,
            MatchSettingsIconPath,
            MatchPauseIconPath,
            MatchInvalidIconPath,
            MatchJumpIconPath,
            MatchPlayerIconPath,
            MatchArmorIconPath,
            MatchSpeedIconPath,
            MatchAirTransportIconPath,
            MatchFriendlyMarkerIconPath,
            MatchHostileMarkerIconPath,
            MatchMedicalIconPath,
            PauseResumeIconPath,
            PauseHelpIconPath,
            PauseExitIconPath,
            AbilitySourceIconPath,
            AbilityAvailabilityIconPath
        };

        private static readonly string[] SharedGradientPaths =
        {
            GreenGradientPath,
            RedGradientPath,
            AmberGradientPath,
            BlueGradientPath,
            CyanGradientPath,
            GraphiteGradientPath
        };

        private static readonly Vector4 PanelBorder = new(24f, 24f, 24f, 24f);
        private static readonly Vector4 ButtonBorder = new(20f, 20f, 20f, 20f);
        private static readonly Vector4 FocusBorder = new(18f, 18f, 18f, 18f);
        private const string BatchBuildActiveKey = "Warline.V3UiFoundation.BatchBuildActive";
        private const string BatchFoundationBuiltKey = "Warline.V3UiFoundation.BatchFoundationBuilt";

        internal static void BeginBatchBuild()
        {
            SessionState.SetBool(BatchBuildActiveKey, true);
            SessionState.SetBool(
                BatchFoundationBuiltKey,
                AssetDatabase.LoadAssetAtPath<GameObject>(BrandLogoPrefabPath) != null);
        }

        internal static void EndBatchBuild()
        {
            SessionState.EraseBool(BatchBuildActiveKey);
            SessionState.EraseBool(BatchFoundationBuiltKey);
        }

        internal static void EnsureBuilt()
        {
            try
            {
                Validate();
            }
            catch (Exception)
            {
                Build();
            }
        }

        [MenuItem("Game/UI/V3/Rebuild Shared UI Foundation")]
        public static void Build()
        {
            if (SessionState.GetBool(BatchBuildActiveKey, false) &&
                SessionState.GetBool(BatchFoundationBuiltKey, false))
            {
                Validate();
                return;
            }

            Directory.CreateDirectory(SharedRoot + "/Config");
            Directory.CreateDirectory(SharedRoot + "/Atlases");
            Directory.CreateDirectory(SharedRoot + "/Prefabs");
            Directory.CreateDirectory(SharedRoot + "/Sprites/Brand");
            Directory.CreateDirectory(SharedRoot + "/Sprites/Core/Gradients");

            ConfigureSprite(PanelPath, PanelBorder);
            ConfigureSprite(ButtonPath, ButtonBorder);
            ConfigureSprite(FocusOverlayPath, FocusBorder);
            ConfigureSprite(MainMenuLogoPath, Vector4.zero, 1024);
            foreach (string gradientPath in SharedGradientPaths)
                ConfigureSprite(gradientPath, Vector4.zero, 256);
            ConfigureSprite(AttackIconPath, Vector4.zero, 256);
            ConfigureSprite(SettingsIconPath, Vector4.zero, 256);
            ConfigureSprite(SettingsAudioIconPath, Vector4.zero, 256);
            ConfigureSprite(SettingsVideoIconPath, Vector4.zero, 256);
            ConfigureSprite(SettingsAccessibilityIconPath, Vector4.zero, 256);
            ConfigureSprite(ResetIconPath, Vector4.zero, 256);
            foreach (string commanderIconPath in CommanderIconPaths)
                ConfigureSprite(commanderIconPath, Vector4.zero, 256);
            foreach (string campaignIconPath in CampaignIconPaths)
                ConfigureSprite(campaignIconPath, Vector4.zero, 256);
            foreach (string equipmentIconPath in EquipmentIconPaths)
                ConfigureSprite(equipmentIconPath, Vector4.zero, 256);
            foreach (string operationsIconPath in OperationsIconPaths)
                ConfigureSprite(operationsIconPath, Vector4.zero, 256);
            foreach (string firstLaunchIconPath in FirstLaunchIconPaths)
                ConfigureSprite(firstLaunchIconPath, Vector4.zero, 256);
            foreach (string matchIconPath in MatchIconPaths)
                ConfigureSprite(
                    matchIconPath,
                    Vector4.zero,
                    matchIconPath.StartsWith(MatchAlignedCommandRoot, StringComparison.Ordinal)
                        ? 512
                        : 256);
            ConfigureSprite(FirstLaunchAriaPortraitPath, Vector4.zero, 2048);

            Sprite panel = RequireSprite(PanelPath);
            Sprite button = RequireSprite(ButtonPath);
            Sprite focusOverlay = RequireSprite(FocusOverlayPath);
            Sprite attackIcon = RequireSprite(AttackIconPath);

            V3UiArtCatalog catalog = LoadOrCreate<V3UiArtCatalog>(CatalogPath);
            ConfigureCatalog(catalog, panel, button, focusOverlay, attackIcon);
            LoadOrCreate<V3UiTheme>(ThemePath);
            BuildMainMenuLogoPrefab();

            BuildAtlas(
                BrandAtlasPath,
                "UI_V3_Brand_01",
                new UnityEngine.Object[]
                {
                    RequireTexture(MainMenuLogoPath)
                });

            BuildAtlas(
                CoreAtlasPath,
                "UI_V3_CoreChrome_01",
                new UnityEngine.Object[]
                {
                    RequireTexture(PanelPath),
                    RequireTexture(ButtonPath),
                    RequireTexture(FocusOverlayPath),
                    RequireTexture(GreenGradientPath),
                    RequireTexture(RedGradientPath),
                    RequireTexture(AmberGradientPath),
                    RequireTexture(BlueGradientPath),
                    RequireTexture(CyanGradientPath),
                    RequireTexture(GraphiteGradientPath)
                });
            BuildAtlas(
                IconAtlasPath,
                "UI_V3_CoreIcons_01",
                new UnityEngine.Object[]
                {
                    RequireTexture(AttackIconPath),
                    RequireTexture(SettingsIconPath),
                    RequireTexture(SettingsAudioIconPath),
                    RequireTexture(SettingsVideoIconPath),
                    RequireTexture(SettingsAccessibilityIconPath),
                    RequireTexture(ResetIconPath),
                    RequireTexture(CanonicalUiResourceIconPaths.Credits),
                    RequireTexture(CanonicalUiResourceIconPaths.Command),
                    RequireTexture(CanonicalUiResourceIconPaths.Materials),
                    RequireTexture(CanonicalUiResourceIconPaths.Oil),
                    RequireTexture(CanonicalUiResourceIconPaths.Fuel),
                    RequireTexture(CanonicalUiResourceIconPaths.Rush)
                });
            var commanderPackables = new UnityEngine.Object[CommanderIconPaths.Length];
            for (int i = 0; i < CommanderIconPaths.Length; i++)
                commanderPackables[i] = RequireTexture(CommanderIconPaths[i]);
            BuildAtlas(CommanderIconAtlasPath, "UI_V3_CommanderIcons_01", commanderPackables);
            var campaignPackables = new UnityEngine.Object[CampaignIconPaths.Length];
            for (int i = 0; i < CampaignIconPaths.Length; i++)
                campaignPackables[i] = RequireTexture(CampaignIconPaths[i]);
            BuildAtlas(CampaignIconAtlasPath, "UI_V3_CampaignIcons_01", campaignPackables);
            var equipmentPackables = new UnityEngine.Object[EquipmentIconPaths.Length];
            for (int i = 0; i < EquipmentIconPaths.Length; i++)
                equipmentPackables[i] = RequireTexture(EquipmentIconPaths[i]);
            BuildAtlas(EquipmentIconAtlasPath, "UI_V3_EquipmentIcons_01", equipmentPackables);
            var operationsPackables = new UnityEngine.Object[OperationsIconPaths.Length];
            for (int i = 0; i < OperationsIconPaths.Length; i++)
                operationsPackables[i] = RequireTexture(OperationsIconPaths[i]);
            BuildAtlas(OperationsIconAtlasPath, "UI_V3_OperationsIcons_01", operationsPackables);
            var firstLaunchPackables = new UnityEngine.Object[FirstLaunchIconPaths.Length];
            for (int i = 0; i < FirstLaunchIconPaths.Length; i++)
                firstLaunchPackables[i] = RequireTexture(FirstLaunchIconPaths[i]);
            BuildAtlas(FirstLaunchIconAtlasPath, "UI_V3_FirstLaunchIcons_01", firstLaunchPackables);
            var matchPackables = new UnityEngine.Object[MatchIconPaths.Length];
            for (int i = 0; i < MatchIconPaths.Length; i++)
                matchPackables[i] = RequireTexture(MatchIconPaths[i]);
            BuildAtlas(MatchIconAtlasPath, "UI_V3_MatchIcons_01", matchPackables);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            if (SessionState.GetBool(BatchBuildActiveKey, false))
                SessionState.SetBool(BatchFoundationBuiltKey, true);
            Debug.Log($"[V3UiFoundationBuilder] result=Passed atlases=9 brand=single-shared-sprite gradients=6 catalog={CatalogPath} theme={ThemePath}");
        }

        public static void Validate()
        {
            ValidateSprite(PanelPath, PanelBorder);
            ValidateSprite(ButtonPath, ButtonBorder);
            ValidateSprite(FocusOverlayPath, FocusBorder);
            ValidateSprite(MainMenuLogoPath, Vector4.zero);
            foreach (string gradientPath in SharedGradientPaths)
                ValidateSprite(gradientPath, Vector4.zero);
            ValidateSprite(AttackIconPath, Vector4.zero);
            ValidateSprite(SettingsIconPath, Vector4.zero);
            ValidateSprite(SettingsAudioIconPath, Vector4.zero);
            ValidateSprite(SettingsVideoIconPath, Vector4.zero);
            ValidateSprite(SettingsAccessibilityIconPath, Vector4.zero);
            ValidateSprite(ResetIconPath, Vector4.zero);
            foreach (string commanderIconPath in CommanderIconPaths)
                ValidateSprite(commanderIconPath, Vector4.zero);
            foreach (string campaignIconPath in CampaignIconPaths)
                ValidateSprite(campaignIconPath, Vector4.zero);
            foreach (string equipmentIconPath in EquipmentIconPaths)
                ValidateSprite(equipmentIconPath, Vector4.zero);
            foreach (string operationsIconPath in OperationsIconPaths)
                ValidateSprite(operationsIconPath, Vector4.zero);
            foreach (string firstLaunchIconPath in FirstLaunchIconPaths)
                ValidateSprite(firstLaunchIconPath, Vector4.zero);
            foreach (string matchIconPath in MatchIconPaths)
                ValidateSprite(matchIconPath, Vector4.zero);

            V3UiArtCatalog catalog = RequireAsset<V3UiArtCatalog>(CatalogPath);
            RequireReference(catalog.Panel, nameof(catalog.Panel));
            RequireReference(catalog.Button, nameof(catalog.Button));
            RequireReference(catalog.FocusOverlay, nameof(catalog.FocusOverlay));
            RequireReference(catalog.AttackIcon, nameof(catalog.AttackIcon));
            RequireReference(catalog.SettingsIcon, nameof(catalog.SettingsIcon));
            RequireReference(catalog.SettingsAudioIcon, nameof(catalog.SettingsAudioIcon));
            RequireReference(catalog.SettingsVideoIcon, nameof(catalog.SettingsVideoIcon));
            RequireReference(catalog.SettingsAccessibilityIcon, nameof(catalog.SettingsAccessibilityIcon));
            RequireReference(catalog.ResetIcon, nameof(catalog.ResetIcon));
            RequireReference(catalog.CreditsIcon, nameof(catalog.CreditsIcon));
            RequireReference(catalog.CommandIcon, nameof(catalog.CommandIcon));
            RequireReference(catalog.MaterialsIcon, nameof(catalog.MaterialsIcon));
            RequireReference(catalog.OilIcon, nameof(catalog.OilIcon));
            RequireReference(catalog.FuelIcon, nameof(catalog.FuelIcon));
            RequireReference(catalog.RushIcon, nameof(catalog.RushIcon));

            RequireAsset<V3UiTheme>(ThemePath);
            ValidateMainMenuLogoPrefab();
            var noDuplicatePaths = new List<string>
            {
                PanelPath,
                ButtonPath,
                FocusOverlayPath,
                MainMenuLogoPath,
                AttackIconPath,
                SettingsIconPath,
                SettingsAudioIconPath,
                SettingsVideoIconPath,
                SettingsAccessibilityIconPath,
                ResetIconPath,
                CanonicalUiResourceIconPaths.Credits,
                CanonicalUiResourceIconPaths.Command,
                CanonicalUiResourceIconPaths.Materials,
                CanonicalUiResourceIconPaths.Oil,
                CanonicalUiResourceIconPaths.Fuel,
                CanonicalUiResourceIconPaths.Rush
            };
            noDuplicatePaths.AddRange(CommanderIconPaths);
            noDuplicatePaths.AddRange(CampaignIconPaths);
            noDuplicatePaths.AddRange(EquipmentIconPaths);
            noDuplicatePaths.AddRange(OperationsIconPaths);
            noDuplicatePaths.AddRange(FirstLaunchIconPaths);
            noDuplicatePaths.AddRange(MatchIconPaths);
            noDuplicatePaths.AddRange(SharedGradientPaths);
            ValidateNoDuplicateFiles(noDuplicatePaths);
            ValidateAtlas(BrandAtlasPath, new[] { MainMenuLogoPath });
            ValidateAtlas(
                CoreAtlasPath,
                new[]
                {
                    PanelPath,
                    ButtonPath,
                    FocusOverlayPath,
                    GreenGradientPath,
                    RedGradientPath,
                    AmberGradientPath,
                    BlueGradientPath,
                    CyanGradientPath,
                    GraphiteGradientPath
                });
            ValidateAtlas(
                IconAtlasPath,
                new[]
                {
                    AttackIconPath,
                    SettingsIconPath,
                    SettingsAudioIconPath,
                    SettingsVideoIconPath,
                    SettingsAccessibilityIconPath,
                    ResetIconPath,
                    CanonicalUiResourceIconPaths.Credits,
                    CanonicalUiResourceIconPaths.Command,
                    CanonicalUiResourceIconPaths.Materials,
                    CanonicalUiResourceIconPaths.Oil,
                    CanonicalUiResourceIconPaths.Fuel,
                    CanonicalUiResourceIconPaths.Rush
                });
            ValidateAtlas(CommanderIconAtlasPath, CommanderIconPaths);
            ValidateAtlas(CampaignIconAtlasPath, CampaignIconPaths);
            ValidateAtlas(EquipmentIconAtlasPath, EquipmentIconPaths);
            ValidateAtlas(OperationsIconAtlasPath, OperationsIconPaths);
            ValidateAtlas(FirstLaunchIconAtlasPath, FirstLaunchIconPaths);
            ValidateAtlas(MatchIconAtlasPath, MatchIconPaths);
        }

        internal static V3UiArtCatalog RequireCatalog()
        {
            return RequireAsset<V3UiArtCatalog>(CatalogPath);
        }

        internal static V3UiTheme RequireTheme()
        {
            return RequireAsset<V3UiTheme>(ThemePath);
        }

        /// <summary>
        /// Adds the exact approved Main Menu V3 WARLINE/CAPTURE lockup as a nested
        /// instance of one shared prefab. The lockup remains sharp at every size and
        /// no screen owns a duplicate logo bitmap or a divergent reconstruction.
        /// </summary>
        internal static RectTransform AddMainMenuLogo(
            Transform parent,
            string name = "SharedMainMenuLogo",
            float left = 8f,
            float top = 6f,
            float right = 8f,
            float bottom = 6f)
        {
            RectTransform parentRect = parent as RectTransform ??
                throw new InvalidOperationException("The shared V3 brand logo requires a RectTransform parent.");
            GameObject prefab = RequireAsset<GameObject>(BrandLogoPrefabPath);
            GameObject logoObject = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject ??
                throw new InvalidOperationException($"Unable to instantiate shared V3 brand logo: {BrandLogoPrefabPath}");
            logoObject.name = name;
            RectTransform rect = logoObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(513f, 137f);
            float availableWidth = Mathf.Max(1f, parentRect.rect.width - left - right);
            float availableHeight = Mathf.Max(1f, parentRect.rect.height - top - bottom);
            float scale = Mathf.Min(availableWidth / 513f, availableHeight / 137f);
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = new Vector2((left - right) * .5f, (bottom - top) * .5f);
            return rect;
        }

        private static void BuildMainMenuLogoPrefab()
        {
            GameObject root = new(
                "UI_V3_MainMenuLogo",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(513f, 137f);
            Image image = root.GetComponent<Image>();
            image.sprite = RequireSprite(MainMenuLogoPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(root, BrandLogoPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ValidateMainMenuLogoPrefab()
        {
            GameObject logo = RequireAsset<GameObject>(BrandLogoPrefabPath);
            Image image = logo.GetComponent<Image>();
            if (image == null || image.sprite == null ||
                !string.Equals(AssetDatabase.GetAssetPath(image.sprite), MainMenuLogoPath, StringComparison.Ordinal))
                throw new MissingReferenceException("Shared V3 brand prefab does not use the approved Main Menu V3 logo sprite.");
            if (!image.preserveAspect || image.raycastTarget)
                throw new InvalidOperationException("Shared V3 brand logo must preserve aspect and never intercept input.");
            if (logo.GetComponentsInChildren<TMP_Text>(true).Length != 0 ||
                logo.GetComponentsInChildren<V3PolygonGraphic>(true).Length != 0)
                throw new InvalidOperationException("Shared V3 brand logo cannot contain procedural text or polygon reconstructions.");
        }

        private static void ConfigureSprite(string path, Vector4 border, int maxTextureSize = 1024)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing V3 texture importer: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxTextureSize;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureCatalog(
            V3UiArtCatalog catalog,
            Sprite panel,
            Sprite button,
            Sprite focusOverlay,
            Sprite attackIcon)
        {
            SerializedObject serialized = new(catalog);
            SetReference(serialized, "panel", panel);
            SetReference(serialized, "button", button);
            SetReference(serialized, "focusOverlay", focusOverlay);
            SetReference(serialized, "attackIcon", attackIcon);
            SetReference(serialized, "settingsIcon", RequireSprite(SettingsIconPath));
            SetReference(serialized, "settingsAudioIcon", RequireSprite(SettingsAudioIconPath));
            SetReference(serialized, "settingsVideoIcon", RequireSprite(SettingsVideoIconPath));
            SetReference(serialized, "settingsAccessibilityIcon", RequireSprite(SettingsAccessibilityIconPath));
            SetReference(serialized, "resetIcon", RequireSprite(ResetIconPath));
            SetReference(serialized, "creditsIcon", RequireSprite(CanonicalUiResourceIconPaths.Credits));
            SetReference(serialized, "commandIcon", RequireSprite(CanonicalUiResourceIconPaths.Command));
            SetReference(serialized, "materialsIcon", RequireSprite(CanonicalUiResourceIconPaths.Materials));
            SetReference(serialized, "oilIcon", RequireSprite(CanonicalUiResourceIconPaths.Oil));
            SetReference(serialized, "fuelIcon", RequireSprite(CanonicalUiResourceIconPaths.Fuel));
            SetReference(serialized, "rushIcon", RequireSprite(CanonicalUiResourceIconPaths.Rush));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.objectReferenceValue = value;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void BuildAtlas(string path, string tag, UnityEngine.Object[] packables)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            UnityEngine.Object[] existing = SpriteAtlasExtensions.GetPackables(atlas);
            if (existing.Length > 0)
                SpriteAtlasExtensions.Remove(atlas, existing);
            SpriteAtlasExtensions.Add(atlas, packables);

            SpriteAtlasExtensions.SetPackingSettings(
                atlas,
                new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    enableRotation = false,
                    enableTightPacking = false,
                    padding = 8
                });
            SpriteAtlasExtensions.SetTextureSettings(
                atlas,
                new SpriteAtlasTextureSettings
                {
                    filterMode = FilterMode.Bilinear,
                    generateMipMaps = false,
                    readable = false,
                    sRGB = true
                });
            SetPlatformSettings(atlas, "DefaultTexturePlatform", false);
            SetPlatformSettings(atlas, "Android", true);
            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
            atlas.name = tag;
            EditorUtility.SetDirty(atlas);
        }

        private static void SetPlatformSettings(SpriteAtlas atlas, string platformName, bool overridden)
        {
            SpriteAtlasExtensions.SetPlatformSettings(
                atlas,
                new TextureImporterPlatformSettings
                {
                    name = platformName,
                    overridden = overridden,
                    maxTextureSize = 1024,
                    format = TextureImporterFormat.RGBA32,
                    compressionQuality = 100
                });
        }

        private static void ValidateSprite(string path, Vector4 expectedBorder)
        {
            Sprite sprite = RequireSprite(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                throw new InvalidOperationException($"{path} is not imported as a Sprite.");
            if (importer.mipmapEnabled || !importer.alphaIsTransparency)
                throw new InvalidOperationException($"{path} must use alpha transparency with mipmaps disabled.");
            if (!Approximately(importer.spriteBorder, expectedBorder))
                throw new InvalidOperationException($"{path} border is {importer.spriteBorder}; expected {expectedBorder}.");
            if (sprite == null)
                throw new MissingReferenceException($"Missing imported sprite: {path}");
        }

        private static void ValidateAtlas(string path, IReadOnlyCollection<string> expectedPaths)
        {
            SpriteAtlas atlas = RequireAsset<SpriteAtlas>(path);
            var actualPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (UnityEngine.Object packable in SpriteAtlasExtensions.GetPackables(atlas))
                actualPaths.Add(AssetDatabase.GetAssetPath(packable));

            foreach (string expectedPath in expectedPaths)
            {
                if (!actualPaths.Contains(expectedPath))
                    throw new MissingReferenceException($"{path} is missing packable {expectedPath}.");
            }

            if (actualPaths.Count != expectedPaths.Count)
                throw new InvalidOperationException($"{path} has {actualPaths.Count} packables; expected {expectedPaths.Count}.");
        }

        private static void ValidateNoDuplicateFiles(IEnumerable<string> paths)
        {
            var pathByHash = new Dictionary<string, string>(StringComparer.Ordinal);
            using SHA256 sha256 = SHA256.Create();
            foreach (string path in paths)
            {
                byte[] hash = sha256.ComputeHash(File.ReadAllBytes(path));
                string key = BitConverter.ToString(hash).Replace("-", string.Empty);
                if (pathByHash.TryGetValue(key, out string existingPath))
                    throw new InvalidOperationException($"Duplicate V3 UI art content: {existingPath} and {path}.");
                pathByHash.Add(key, path);
            }
        }

        private static bool Approximately(Vector4 left, Vector4 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                   Mathf.Approximately(left.y, right.y) &&
                   Mathf.Approximately(left.z, right.z) &&
                   Mathf.Approximately(left.w, right.w);
        }

        private static Sprite RequireSprite(string path)
        {
            return RequireAsset<Sprite>(path);
        }

        private static Texture2D RequireTexture(string path)
        {
            return RequireAsset<Texture2D>(path);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing required asset: {path}");
            return asset;
        }

        private static void RequireReference(UnityEngine.Object value, string name)
        {
            if (value == null)
                throw new MissingReferenceException($"V3 art catalog is missing {name}.");
        }
    }
}
