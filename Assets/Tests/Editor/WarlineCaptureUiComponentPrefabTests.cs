using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiComponentPrefabTests
{
    private const string ComponentFolder = "Assets/Game/Prefabs/UI/Components";
    private const string PopupFolder = "Assets/Game/Prefabs/UI/Popups";
    private const string OxaniumFontFolder = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/";

    [Test]
    public void PhaseTwoComponentPrefabs_Exist()
    {
        AssertPrefabExists($"{ComponentFolder}/ModeCardView.prefab");
        AssertPrefabExists($"{ComponentFolder}/StatTileView.prefab");
        AssertPrefabExists($"{ComponentFolder}/ResourceCounterView.prefab");
        AssertPrefabExists($"{ComponentFolder}/ObjectiveRowView.prefab");
        AssertPrefabExists($"{ComponentFolder}/RewardItemView.prefab");
        AssertPrefabExists($"{ComponentFolder}/ActionButtonView.prefab");
        AssertPrefabExists($"{ComponentFolder}/SegmentedControlView.prefab");
        AssertPrefabExists($"{ComponentFolder}/ToggleRowView.prefab");
        AssertPrefabExists($"{ComponentFolder}/SliderRowView.prefab");
        AssertPrefabExists($"{PopupFolder}/PopupFrameView.prefab");
        AssertPrefabExists($"{PopupFolder}/PauseMenuPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/ThreatAlertPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/BuildPlacementPanel.prefab");
        AssertPrefabExists($"{PopupFolder}/ConfirmRaidPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/MissionResultPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/RewardUnlockPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/EndOfDayReportPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/IntelRevealPopup.prefab");
        AssertPrefabExists($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab");
    }

    [Test]
    public void PhaseTwoComponentPrefabs_HaveStableChildContracts()
    {
        AssertChildren($"{ComponentFolder}/ModeCardView.prefab", "Background", "ArtImage", "TitleText", "SubtitleText", "ProgressText", "LockRoot", "NotificationBadge", "Button");
        AssertChildren($"{ComponentFolder}/StatTileView.prefab", "Icon", "LabelText", "ValueText", "DeltaText");
        AssertChildren($"{ComponentFolder}/ResourceCounterView.prefab", "Icon", "ValueText", "PlusButton");
        AssertChildren($"{ComponentFolder}/ObjectiveRowView.prefab", "Icon", "LabelText", "ProgressText", "ProgressBar", "ProgressBar/Fill", "CompleteIcon");
        AssertChildren($"{ComponentFolder}/RewardItemView.prefab", "Icon", "QuantityText", "RarityFrame");
        AssertChildren($"{ComponentFolder}/ActionButtonView.prefab", "Icon", "LabelText", "CostText", "LockRoot", "Button");
        AssertChildren($"{ComponentFolder}/SegmentedControlView.prefab", "SegmentRoot");
        AssertChildren($"{ComponentFolder}/ToggleRowView.prefab", "LabelText", "DescriptionText", "Toggle");
        AssertChildren($"{ComponentFolder}/SliderRowView.prefab", "LabelText", "ValueText", "Slider");
        AssertChildren($"{PopupFolder}/PopupFrameView.prefab", "Scrim", "Frame", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/ButtonRow");
        AssertChildren($"{PopupFolder}/PauseMenuPopup.prefab", "Scrim", "Frame", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/CurrentTimeRow", "Frame/BodyRoot/MissionNameRow", "Frame/ButtonRow", "Frame/ButtonRow/ResumeButton", "Frame/ButtonRow/SettingsButton", "Frame/ButtonRow/RestartButton", "Frame/ButtonRow/HelpButton", "Frame/ButtonRow/ExitButton");
        AssertChildren($"{PopupFolder}/ThreatAlertPopup.prefab", "Scrim", "Frame", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/WarningIcon", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/HeadlineText", "Frame/BodyRoot/InfoColumn", "Frame/BodyRoot/InfoColumn/EtaRow", "Frame/BodyRoot/InfoColumn/RouteRow", "Frame/BodyRoot/InfoColumn/StrengthRow", "Frame/BodyRoot/StrengthMeter", "Frame/BodyRoot/ThreatImagePanel", "Frame/BodyRoot/ThreatImagePanel/ConvoyImage", "Frame/ButtonRow", "Frame/ButtonRow/JumpToThreatButton");
        AssertChildren($"{PopupFolder}/BuildPlacementPanel.prefab", "Scrim", "PlacementOverlay", "PlacementOverlay/GridOverlayImage", "PlacementOverlay/GhostBuildingImage", "ResourceBar", "BuildModeRail", "BuildChecklistPanel", "CoordinatePanel", "Frame", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/BuildingCard", "Frame/BodyRoot/BuildingCard/PreviewImage", "Frame/BodyRoot/InfoPanel", "Frame/BodyRoot/InfoPanel/FootprintRow", "Frame/BodyRoot/InfoPanel/StatusValueText", "Frame/BodyRoot/ControlPanel", "Frame/BodyRoot/ControlPanel/RotateButton", "Frame/BodyRoot/ControlPanel/CancelButton", "Frame/BodyRoot/ControlPanel/ConfirmButton");
        AssertChildren($"{PopupFolder}/ConfirmRaidPopup.prefab", "BackgroundOperationDashboard", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/TitleWarningIcon", "Frame/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/TargetPanel", "Frame/BodyRoot/TargetPanel/TargetNameText", "Frame/BodyRoot/TargetPanel/DistrictThumbnail", "Frame/BodyRoot/TargetPanel/DistrictThumbnail/ThumbnailImage", "Frame/BodyRoot/TargetPanel/DistrictThumbnail/FrameChrome", "Frame/BodyRoot/TargetPanel/TargetInfoCard", "Frame/BodyRoot/RiskPanel", "Frame/BodyRoot/RiskPanel/IntelConfidenceRow", "Frame/BodyRoot/RiskPanel/CollateralRiskRow", "Frame/BodyRoot/RiskPanel/CivilianDensityRow", "Frame/BodyRoot/RiskPanel/WarningTextPanel", "Frame/ButtonRow", "Frame/ButtonRow/CancelButton", "Frame/ButtonRow/ConfirmButton");
        AssertChildren($"{PopupFolder}/MissionResultPopup.prefab", "BackgroundTacticalArt", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/VictoryEmblem", "Frame/Header/Star_1", "Frame/Header/Star_2", "Frame/Header/Star_3", "Frame/Header/MapIdentityText", "Frame/BodyRoot", "Frame/BodyRoot/StatsPanel", "Frame/BodyRoot/StatsPanel/EnemiesDefeatedCard", "Frame/BodyRoot/RewardsPanel", "Frame/BodyRoot/RewardsPanel/CommanderXpReward", "Frame/BodyRoot/RewardsPanel/MaterialsReward", "Frame/BodyRoot/ObjectivesPanel", "Frame/BodyRoot/ObjectivesPanel/Objective_DestroyHostilePatrol", "Frame/ConsequenceRow", "Frame/ButtonRow", "Frame/ButtonRow/ReplayButton", "Frame/ButtonRow/ContinueButton");
        AssertChildren($"{PopupFolder}/RewardUnlockPopup.prefab", "BackgroundCommandCenter", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/HeaderEmblem", "Frame/Header/TitleText", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/UnlockImage", "Frame/BodyRoot/UnlockTitleText", "Frame/BodyRoot/UnlockSubtitleText", "Frame/BodyRoot/RewardIconGrid", "Frame/BodyRoot/RewardIconGrid/CommanderXpReward", "Frame/BodyRoot/RewardIconGrid/CreditsReward", "Frame/BodyRoot/RewardIconGrid/SupplyCrateReward", "Frame/BodyRoot/RewardIconGrid/GearPartsReward", "Frame/ButtonRow", "Frame/ButtonRow/ContinueButton");
        AssertChildren($"{PopupFolder}/EndOfDayReportPopup.prefab", "BackgroundOperationDashboard", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/DayTag", "Frame/BodyRoot", "Frame/BodyRoot/DeltaSummary", "Frame/BodyRoot/TrustStabilityPanel", "Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow", "Frame/BodyRoot/EnemyActivityPanel", "Frame/BodyRoot/ResourceRow", "Frame/BodyRoot/ResourceRow/FundsResource", "Frame/BodyRoot/SaveStatusRow", "Frame/ButtonRow", "Frame/ButtonRow/SaveContinueButton");
        AssertChildren($"{PopupFolder}/IntelRevealPopup.prefab", "BackgroundIntelArchive", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/HeaderIcon", "Frame/Header/TitleText", "Frame/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/SubheadingText", "Frame/BodyRoot/SupplyLedgerCard", "Frame/BodyRoot/SupplyLedgerCard/ThumbnailFrame", "Frame/BodyRoot/CargoManifestCard", "Frame/BodyRoot/RadioInterceptCard", "Frame/BodyRoot/RadioInterceptCard/ThumbnailFrame/OverlayIcon", "Frame/BodyRoot/NoticeBar", "Frame/ButtonRow", "Frame/ButtonRow/CloseButton", "Frame/ButtonRow/ViewIntelButton");
        AssertChildren($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab", "Scrim", "Frame", "Frame/FrameFill", "Frame/Header", "Frame/Header/TitleText", "Frame/Header/ConfigTargetChip", "Frame/Header/CloseButton", "Frame/BodyRoot", "Frame/BodyRoot/ContentPanel", "Frame/BodyRoot/ContentPanel/AbilityArtImage", "Frame/BodyRoot/DetailPanel", "Frame/BodyRoot/DetailPanel/AbilityTitleText", "Frame/BodyRoot/EffectsPanel", "Frame/BodyRoot/EffectsPanel/EffectRevealArea", "Frame/BodyRoot/UpgradeTargetRow", "Frame/BodyRoot/UpgradeTargetRow/PartsProgress", "Frame/ButtonRow", "Frame/ButtonRow/LockedButton", "Frame/ButtonRow/ViewSourceButton", "Frame/ButtonRow/ConfigNotice");
    }

    [Test]
    public void PhaseTwoComponentPrefabs_UseOxaniumFamilyForText()
    {
        foreach (string prefabPath in GetPhaseTwoPrefabPaths())
        {
            string prefabText = File.ReadAllText(prefabPath);
            MatchCollection fontMatches = Regex.Matches(prefabText, @"m_fontAsset: \{fileID: 11400000, guid: ([a-f0-9]+), type: 2\}");

            Assert.Greater(fontMatches.Count, 0, prefabPath);
            foreach (Match fontMatch in fontMatches)
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(fontMatch.Groups[1].Value);
                StringAssert.StartsWith(OxaniumFontFolder, fontPath, prefabPath);
                StringAssert.Contains("Oxanium", Path.GetFileNameWithoutExtension(fontPath), prefabPath);
            }
        }
    }

    [Test]
    public void PhaseTwoButtons_HaveTargetGraphicsAndTouchSize()
    {
        AssertButtonTargetAndHeight($"{ComponentFolder}/ModeCardView.prefab", "Button", 80f);
        AssertButtonTargetAndHeight($"{ComponentFolder}/ResourceCounterView.prefab", "PlusButton", 80f);
        AssertButtonTargetAndHeight($"{ComponentFolder}/ActionButtonView.prefab", null, 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/PopupFrameView.prefab", "Frame/Header/CloseButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/PauseMenuPopup.prefab", "Frame/Header/CloseButton", 64f);
        AssertButtonTargetAndHeight($"{PopupFolder}/PauseMenuPopup.prefab", "Frame/ButtonRow/ResumeButton", 76f);
        AssertButtonTargetAndHeight($"{PopupFolder}/ThreatAlertPopup.prefab", "Frame/Header/CloseButton", 60f);
        AssertButtonTargetAndHeight($"{PopupFolder}/ThreatAlertPopup.prefab", "Frame/ButtonRow/JumpToThreatButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/BuildPlacementPanel.prefab", "Frame/Header/CloseButton", 54f);
        AssertButtonTargetAndHeight($"{PopupFolder}/BuildPlacementPanel.prefab", "Frame/BodyRoot/ControlPanel/RotateButton", 68f);
        AssertButtonTargetAndHeight($"{PopupFolder}/BuildPlacementPanel.prefab", "Frame/BodyRoot/ControlPanel/CancelButton", 56f);
        AssertButtonTargetAndHeight($"{PopupFolder}/BuildPlacementPanel.prefab", "Frame/BodyRoot/ControlPanel/ConfirmButton", 60f);
        AssertButtonTargetAndHeight($"{PopupFolder}/ConfirmRaidPopup.prefab", "Frame/CloseButton", 58f);
        AssertButtonTargetAndHeight($"{PopupFolder}/ConfirmRaidPopup.prefab", "Frame/ButtonRow/CancelButton", 60f);
        AssertButtonTargetAndHeight($"{PopupFolder}/ConfirmRaidPopup.prefab", "Frame/ButtonRow/ConfirmButton", 76f);
        AssertButtonTargetAndHeight($"{PopupFolder}/MissionResultPopup.prefab", "Frame/ButtonRow/ReplayButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/MissionResultPopup.prefab", "Frame/ButtonRow/ContinueButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/RewardUnlockPopup.prefab", "Frame/Header/CloseButton", 60f);
        AssertButtonTargetAndHeight($"{PopupFolder}/RewardUnlockPopup.prefab", "Frame/ButtonRow/ContinueButton", 70f);
        AssertButtonTargetAndHeight($"{PopupFolder}/EndOfDayReportPopup.prefab", "Frame/ButtonRow/SaveContinueButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/IntelRevealPopup.prefab", "Frame/CloseButton", 76f);
        AssertButtonTargetAndHeight($"{PopupFolder}/IntelRevealPopup.prefab", "Frame/ButtonRow/ViewIntelButton", 80f);
        AssertButtonTargetAndHeight($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab", "Frame/Header/CloseButton", 62f);
        AssertButtonTargetAndHeight($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab", "Frame/ButtonRow/LockedButton", 70f);
    }

    [Test]
    public void VisualLockLayeredPopupPacks_ArePresentBeforePopupPrefabWork()
    {
        AssertPopupLayerPack(
            "POP-07_PauseOptions",
            9,
            "Assets/Game/Art/UI/Generated/Popups/Pause_Button_Selected_9Slice.png",
            "Assets/Game/Art/UI/Generated/Popups/Pause_Button_Normal_9Slice.png",
            "Assets/Game/Art/UI/Generated/Popups/Pause_Icon_Resume.png");

        AssertPopupLayerPack(
            "POP-01_ThreatAlert",
            12,
            "Assets/Game/Art/UI/Generated/Popups/Threat_HeaderBackplate_9Slice.png",
            "Assets/Game/Art/UI/Generated/Popups/Threat_JumpButton_9Slice.png",
            "Assets/Game/Art/UI/Generated/Popups/Threat_Icon_Warning.png");

        AssertPopupLayerPack(
            "POP-03_BuildPlacement",
            25,
            "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_PanelFrame_9Slice.png",
            "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_Button_Confirm_9Slice.png",
            "Assets/Game/Art/UI/Generated/MatchHUD/Frames/MatchHUD_ResourceBar_9Slice.png");

        AssertPopupLayerPack(
            "POP-02_ConfirmRaid",
            20,
            "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Frames/modal_frame.png",
            "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Frames/metric_row_blue_frame.png",
            "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Buttons/confirm_button_background.png",
            "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Icons/intel_icon.png");

        AssertPopupLayerPack(
            "POP-05_MissionResult",
            23,
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Frames/modal_frame.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Cards/stat_card_frame.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Rows/consequence_row_frame.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Buttons/continue_button_background.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_commander_xp.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_materials.png",
            "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_intel.png");

        AssertPopupLayerPack(
            "POP-04_RewardUnlock",
            18,
            "Assets/Game/Art/UI/Generated/RewardUnlock/LayeredOneGo/Frames/modal_frame.png",
            "Assets/Game/Art/UI/Generated/RewardUnlock/LayeredOneGo/Content/unlock_display_art.png",
            "Assets/Game/Art/UI/Generated/RewardUnlock/LayeredOneGo/Buttons/continue_button_background.png",
            "Assets/Game/Art/UI/Generated/RewardUnlock/LayeredOneGo/Icons/icon_commander_xp.png");

        AssertPopupLayerPack(
            "POP-06_EndOfDayReport",
            24,
            "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Frames/modal_frame.png",
            "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Frames/resource_row_frame.png",
            "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Buttons/save_button_background.png",
            "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Icons/icon_enemy_activity.png");

        AssertPopupLayerPack(
            "POP-08_IntelReveal",
            19,
            "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/modal_frame.png",
            "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/evidence_card_frame.png",
            "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/primary_button_background.png",
            "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/header_document_magnifier_icon.png");

        AssertPopupLayerPack(
            "POP-09_AbilityUpgradeDetail",
            26,
            "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/modal_frame.png",
            "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/content_art_frame.png",
            "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/disabled_primary_button_background.png",
            "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/icon_drone.png");
    }

    [Test]
    public void BuildPlacementPanel_ConsumesVisualLockLayerPackSprites()
    {
        WithPrefab($"{PopupFolder}/BuildPlacementPanel.prefab", root =>
        {
            AssertImageSpritePath(root, "ResourceBar/FillBackground", "Assets/Game/Art/UI/Generated/MatchHUD/Frames/MatchHUD_ResourceBar_Fill_9Slice.png");
            AssertImageSpritePath(root, "ResourceBar/FrameChrome", "Assets/Game/Art/UI/Generated/MatchHUD/Frames/MatchHUD_ResourceBar_9Slice.png");
            AssertImageSpritePath(root, "Frame", "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_PanelFrame_9Slice.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/BuildingCard", "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_CardFrame_9Slice.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/ControlPanel/RotateButton", "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_Button_Normal_9Slice.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/ControlPanel/ConfirmButton", "Assets/Game/Art/UI/Generated/Popups/BuildPlacement_Button_Confirm_9Slice.png");
        });
    }

    [Test]
    public void ConfirmRaidPopup_MatchesCurrentVisualTargetContentContract()
    {
        WithPrefab($"{PopupFolder}/ConfirmRaidPopup.prefab", root =>
        {
            AssertText(root, "Frame/Header/TitleText", "CONFIRM RAID");
            AssertText(root, "Frame/BodyRoot/TargetPanel/TargetNameText", "North Bridge Cell");
            AssertText(root, "Frame/BodyRoot/TargetPanel/TargetInfoCard/DistrictText", "District: North Bridge");
            AssertText(root, "Frame/BodyRoot/TargetPanel/TargetInfoCard/ThreatText", "Threat Level: High");
            AssertText(root, "Frame/BodyRoot/RiskPanel/IntelConfidenceRow/ValueText", "78%");
            AssertText(root, "Frame/BodyRoot/RiskPanel/CollateralRiskRow/ValueText", "Medium");
            AssertText(root, "Frame/BodyRoot/RiskPanel/CivilianDensityRow/ValueText", "Elevated");
            AssertText(root, "Frame/ButtonRow/CancelButton/LabelText", "CANCEL");
            AssertText(root, "Frame/ButtonRow/ConfirmButton/LabelText", "CONFIRM RAID");
            AssertImageSpritePath(root, "BackgroundOperationDashboard", "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Content/background_operation_dashboard.png");
            AssertImageSpritePath(root, "Frame", "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Frames/modal_frame.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/TargetPanel/DistrictThumbnail/ThumbnailImage", "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Content/district_thumbnail_north_bridge.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/RiskPanel/IntelConfidenceRow/IconImage", "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Icons/intel_icon.png");
            AssertImageSpritePath(root, "Frame/ButtonRow/ConfirmButton", "Assets/Game/Art/UI/Generated/ConfirmRaid/LayeredOneGo/Buttons/confirm_button_background.png");
        });
    }

    [Test]
    public void MissionResultPopup_MatchesCurrentVisualTargetContentContract()
    {
        WithPrefab($"{PopupFolder}/MissionResultPopup.prefab", root =>
        {
            AssertText(root, "Frame/Header/MissionNameText", "M01 First Contact");
            AssertText(root, "Frame/Header/MissionMetaText", "saga.ch01.m01.first_contact  |  scenario.ch01.m01.first_contact");
            AssertText(root, "Frame/Header/MapIdentityText", "level.ch01.district_edge_01  |  iso.ch01.district_edge_01");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CommanderXpReward/ValueText", "+250");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CreditsReward/ValueText", "+1,250");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/LabelText", "MATERIALS");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/IntelReward/LabelText", "INTEL");
            AssertText(root, "Frame/BodyRoot/ObjectivesPanel/Objective_DestroyHostilePatrol/LabelText", "Destroy hostile patrol");
            AssertText(root, "Frame/ConsequenceRow/ConsequenceText", "City Consequence     Civilian delta  0     Infrastructure delta  0");
            AssertImageSpritePath(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/IconImage", "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_materials.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/RewardsPanel/IntelReward/IconImage", "Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Icons/icon_intel.png");
            Assert.IsTrue(root.transform.Find("Frame/BodyRoot/RewardsPanel/MaterialsReward/LabelText").GetComponent<TMP_Text>().enableAutoSizing);
            Assert.IsTrue(root.transform.Find("Frame/ConsequenceRow").gameObject.activeSelf);
        });
    }

    [Test]
    public void EndOfDayReportPopup_MatchesCurrentVisualTargetContentContract()
    {
        WithPrefab($"{PopupFolder}/EndOfDayReportPopup.prefab", root =>
        {
            AssertText(root, "Frame/Header/TitleText", "END OF DAY REPORT");
            AssertText(root, "Frame/Header/DayTag/DayText", "Day 17");
            AssertText(root, "Frame/BodyRoot/DeltaSummary/LabelText", "DISTRICT CHANGES");
            AssertText(root, "Frame/BodyRoot/DeltaSummary/ValueText", "+2");
            AssertText(root, "Frame/BodyRoot/DeltaSummary/DeltaText", "Stability +2. Trust +4. Heat +0.");
            AssertText(root, "Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow/ValueText", "+8");
            AssertText(root, "Frame/BodyRoot/TrustStabilityPanel/CivilianTrustRow/DeltaText", "daily pressure");
            AssertText(root, "Frame/BodyRoot/TrustStabilityPanel/RegionStabilityRow/ValueText", "+5");
            AssertText(root, "Frame/BodyRoot/TrustStabilityPanel/RegionStabilityRow/DeltaText", "daily pressure");
            AssertText(root, "Frame/BodyRoot/EnemyActivityPanel/ThreatValueText", "HIGH");
            AssertText(root, "Frame/BodyRoot/SaveStatusRow/StatusText", "Operation progress ready to save");
            AssertText(root, "Frame/ButtonRow/SaveContinueButton/LabelText", "SAVE & CONTINUE");
            AssertImageSpritePath(root, "Frame/BodyRoot/ResourceRow/FundsResource/IconImage", "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Icons/icon_funds.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/EnemyActivityPanel/IconImage", "Assets/Game/Art/UI/Generated/EndOfDayReport/LayeredOneGo/Icons/icon_enemy_activity.png");
        });
    }

    [Test]
    public void IntelRevealPopup_MatchesCurrentVisualTargetContentContract()
    {
        WithPrefab($"{PopupFolder}/IntelRevealPopup.prefab", root =>
        {
            AssertText(root, "Frame/Header/TitleText", "INTEL REVEALED");
            AssertText(root, "Frame/BodyRoot/SubheadingText", "Evidence Collected");
            AssertText(root, "Frame/BodyRoot/SupplyLedgerCard/TitleText", "Supply Ledger");
            AssertText(root, "Frame/BodyRoot/CargoManifestCard/TitleText", "Cargo Manifest");
            AssertText(root, "Frame/BodyRoot/RadioInterceptCard/TitleText", "Radio Intercept");
            AssertText(root, "Frame/BodyRoot/SupplyLedgerCard/ConfidenceChip/ConfidenceText", "CONFIDENCE: HIGH");
            AssertText(root, "Frame/BodyRoot/CargoManifestCard/ConfidenceChip/ConfidenceText", "CONFIDENCE: MEDIUM");
            AssertText(root, "Frame/BodyRoot/NoticeBar/NoticeText", "New intel available in Intel Archive");
            AssertText(root, "Frame/ButtonRow/CloseButton/LabelText", "CLOSE");
            AssertText(root, "Frame/ButtonRow/ViewIntelButton/LabelText", "VIEW INTEL");
            AssertImageSpritePath(root, "BackgroundIntelArchive", "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/background_intel_archive.png");
            AssertImageSpritePath(root, "Frame", "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Frames/modal_frame.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/SupplyLedgerCard/ThumbnailFrame/ThumbnailImage", "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Content/thumbnail_supply_ledger.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/RadioInterceptCard/ThumbnailFrame/OverlayIcon", "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Icons/radio_play_icon.png");
            AssertImageSpritePath(root, "Frame/ButtonRow/ViewIntelButton", "Assets/Game/Art/UI/Generated/IntelReveal/LayeredOneGo/Buttons/primary_button_background.png");
        });
    }

    [Test]
    public void AbilityUpgradeDetailPopup_MatchesCurrentVisualTargetContentContract()
    {
        WithPrefab($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab", root =>
        {
            AssertText(root, "Frame/Header/TitleText", "ABILITY / UPGRADE DETAIL");
            AssertText(root, "Frame/Header/ConfigTargetChip/LabelText", "CONFIG TARGET");
            AssertText(root, "Frame/BodyRoot/DetailPanel/AbilityTitleText", "DRONE SCAN");
            AssertText(root, "Frame/BodyRoot/DetailPanel/TargetIdRow/ValueText", "ability.drone_scan");
            AssertText(root, "Frame/BodyRoot/DetailPanel/UnlockRow/ValueText", "Chapter 1 M03 Reward");
            AssertText(root, "Frame/BodyRoot/UpgradeTargetRow/UpgradeTitleText", "APC ARMOR UPGRADE");
            AssertText(root, "Frame/BodyRoot/UpgradeTargetRow/PartsProgress/ValueText", "18 / 40");
            AssertText(root, "Frame/ButtonRow/LockedButton/LabelText", "NOT UNLOCKED");
            AssertText(root, "Frame/ButtonRow/ViewSourceButton/LabelText", "VIEW SOURCE");
            AssertImageSpritePath(root, "Frame", "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/modal_frame.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/ContentPanel/AbilityArtImage", "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/art_drone_scan_city.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/DetailPanel/AbilityIconPlate/AbilityIcon", "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/icon_drone.png");
            AssertImageSpritePath(root, "Frame/BodyRoot/UpgradeTargetRow/UpgradeArtImage", "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/art_apc_armor_small.png");
            AssertImageSpritePath(root, "Frame/ButtonRow/LockedButton", "Assets/Game/Art/UI/Generated/AbilityUpgradeDetail/LayeredOneGo/disabled_primary_button_background.png");
        });
    }

    [Test]
    public void SliderRow_HandleUsesFixedCircularGeometry()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentFolder}/SliderRowView.prefab");
        Assert.NotNull(prefab);

        RectTransform handleArea = prefab.transform.Find("Slider/Handle Slide Area") as RectTransform;
        RectTransform handle = prefab.transform.Find("Slider/Handle Slide Area/Handle") as RectTransform;
        Assert.NotNull(handleArea);
        Assert.NotNull(handle);

        Assert.AreEqual(handleArea.anchorMin.y, handleArea.anchorMax.y, 0.0001f);
        Assert.AreEqual(0.5f, handleArea.anchorMin.y, 0.0001f);
        Assert.AreEqual(0f, handleArea.offsetMin.y, 0.0001f);
        Assert.AreEqual(0f, handleArea.offsetMax.y, 0.0001f);
        Assert.AreEqual(handle.sizeDelta.x, handle.sizeDelta.y, 0.0001f);
        Assert.LessOrEqual(handle.sizeDelta.y, 36f);
    }

    [Test]
    public void PhaseTwoComponentPrefabs_DisableDecorativeGraphicRaycasts()
    {
        foreach (string prefabPath in GetPhaseTwoPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.NotNull(prefab, prefabPath);

            foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
            {
                bool expectedRaycast = IsInteractiveRaycastGraphic(prefab, graphic);
                Assert.AreEqual(expectedRaycast, graphic.raycastTarget, $"{prefabPath}:{GetHierarchyPath(graphic.transform)} has an incorrect raycastTarget value.");
            }
        }
    }

    [Test]
    public void PhaseTwoComponentBindMethods_RunWithoutExceptions()
    {
        WithPrefab($"{ComponentFolder}/ModeCardView.prefab", root => root.GetComponent<WarlineCaptureModeCardView>().Bind("Saga", "Campaign", "2 / 8", true, true));
        WithPrefab($"{ComponentFolder}/StatTileView.prefab", root => root.GetComponent<WarlineCaptureStatTileView>().Bind("Trust", "72", "+4"));
        WithPrefab($"{ComponentFolder}/ResourceCounterView.prefab", root => root.GetComponent<WarlineCaptureResourceCounterView>().Bind("10,000"));
        WithPrefab($"{ComponentFolder}/ObjectiveRowView.prefab", root => root.GetComponent<WarlineCaptureObjectiveRowView>().Bind("Survive", "30 / 60", 0.5f, false));
        WithPrefab($"{ComponentFolder}/RewardItemView.prefab", root => root.GetComponent<WarlineCaptureRewardItemView>().Bind("x3", Color.yellow));
        WithPrefab($"{ComponentFolder}/ActionButtonView.prefab", root => root.GetComponent<WarlineCaptureActionButtonView>().Bind("Launch", "100", false));
        WithPrefab($"{ComponentFolder}/SegmentedControlView.prefab", root => root.GetComponent<WarlineCaptureSegmentedControlView>().Bind(new[] { "Easy", "Normal", "Hard" }, 1));
        WithPrefab($"{ComponentFolder}/ToggleRowView.prefab", root => root.GetComponent<WarlineCaptureToggleRowView>().Bind("Music", "Enable music.", true));
        WithPrefab($"{ComponentFolder}/SliderRowView.prefab", root => root.GetComponent<WarlineCaptureSliderRowView>().Bind("Camera", 50f, 0f, 100f));
        WithPrefab($"{PopupFolder}/PopupFrameView.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Warning");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/PauseMenuPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Paused");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/ThreatAlertPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Threat");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/BuildPlacementPanel.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Place Building");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/MissionResultPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Victory");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/RewardUnlockPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("New Asset Unlocked");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/EndOfDayReportPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("End Of Day Report");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/IntelRevealPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Intel Revealed");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
        WithPrefab($"{PopupFolder}/AbilityUpgradeDetailPopup.prefab", root =>
        {
            WarlineCapturePopupFrameView popup = root.GetComponent<WarlineCapturePopupFrameView>();
            popup.Show("Ability Detail");
            Assert.IsTrue(root.activeSelf);
            popup.Close();
            Assert.IsFalse(root.activeSelf);
        });
    }

    [Test]
    public void PhaseTwoComponentPrefabs_DoNotOwnShellOrGameplayDependencies()
    {
        foreach (string prefabPath in GetPhaseTwoPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.NotNull(prefab, prefabPath);
            Assert.IsNull(prefab.GetComponentInChildren<WarlineCaptureRouter>(true), prefabPath);
            Assert.IsNull(prefab.GetComponentInChildren<WarlineCaptureUiBootstrap>(true), prefabPath);
            foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                Assert.AreNotEqual("GameBootstrap", behaviour.GetType().Name, prefabPath);
        }
    }

    [Test]
    public void MissionResultPopupController_BindsRuntimeResultData()
    {
        WithPrefab($"{PopupFolder}/MissionResultPopup.prefab", root =>
        {
            MissionResultPopupController controller = root.GetComponent<MissionResultPopupController>();
            Assert.NotNull(controller);

            var result = new MissionResultData(
                "test.m01",
                "First Contact",
                true,
                2,
                12,
                1,
                3,
                80,
                new[]
                {
                    new ObjectiveRuntimeState("destroy", "Destroy hostiles", ObjectiveType.DestroyAllEnemies, 12, 12, true, true),
                    new ObjectiveRuntimeState("losses", "Keep losses below 2", ObjectiveType.KeepUnitLossesBelow, 1, 2, false, true)
                },
                new[]
                {
                    new RewardGrantResult("test.clear", RewardType.CommanderXp, string.Empty, 220, true, string.Empty),
                    new RewardGrantResult("test.clear", RewardType.Credits, string.Empty, 1200, true, string.Empty),
                    new RewardGrantResult("test.unlock", RewardType.BuildingUnlock, "Building_Barrack", 1, true, string.Empty),
                    new RewardGrantResult("test.skipped", RewardType.UnitUnlock, "Unit_Chr_Soldier_Male_02_Alt_04", 1, false, "Already owned.")
                });

            controller.Bind(result);

            AssertText(root, "Frame/Header/MissionNameText", "First Contact");
            AssertText(root, "Frame/BodyRoot/StatsPanel/EnemiesDefeatedCard/ValueText", "12");
            AssertText(root, "Frame/BodyRoot/StatsPanel/UnitsLostCard/ValueText", "1");
            AssertText(root, "Frame/BodyRoot/ObjectivesPanel/Objective_DestroyHostilePatrol/LabelText", "Destroy hostiles");
            AssertText(root, "Frame/BodyRoot/ObjectivesPanel/Objective_KeepCommandSquadAlive/LabelText", "Keep losses below 2");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CommanderXpReward/LabelText", "COMMAND XP");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CommanderXpReward/ValueText", "+220");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CreditsReward/LabelText", "CREDITS");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/CreditsReward/ValueText", "+1,200");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/LabelText", "BUILDING UNLOCK");
            AssertText(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/ValueText", "BARRACK");
            Assert.IsFalse(root.transform.Find("Frame/BodyRoot/RewardsPanel/IntelReward").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Frame/BodyRoot/ObjectivesPanel/Objective_CityConsequenceNeutral").gameObject.activeSelf);
            Assert.Greater(root.transform.Find("Frame/Header/Star_2").GetComponent<Image>().color.a, 0.9f);
            Assert.Less(root.transform.Find("Frame/Header/Star_3").GetComponent<Image>().color.a, 0.3f);
        });
    }

    [Test]
    public void MissionResultPopupController_PrioritizesOperationRewardsForOperationMission()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m05.breach_assault");
        WarlineCaptureMissionSession.BeginMissionForTests(mission, WarlineCaptureRoute.OperationDashboard);
        try
        {
            WithPrefab($"{PopupFolder}/MissionResultPopup.prefab", root =>
            {
                MissionResultPopupController controller = root.GetComponent<MissionResultPopupController>();
                Assert.NotNull(controller);

                var result = new MissionResultData(
                    "saga.ch01.m05.breach_assault",
                    "Breach Assault",
                    true,
                    2,
                    18,
                    1,
                    1,
                    240,
                    System.Array.Empty<ObjectiveRuntimeState>(),
                    new[]
                    {
                        new RewardGrantResult("ch01.m05.clear", RewardType.CommanderXp, string.Empty, 260, true, string.Empty),
                        new RewardGrantResult("ch01.m05.clear", RewardType.Credits, string.Empty, 1200, true, string.Empty),
                        new RewardGrantResult("ch01.m05.unlock", RewardType.UnitUnlock, "Unit_Chr_Ghillie_Male_01", 1, true, string.Empty),
                        new RewardGrantResult("ch01.m05.operation", RewardType.OperationSupply, string.Empty, 1, true, string.Empty),
                        new RewardGrantResult("ch01.m05.operation", RewardType.OperationSecurity, "port_breach", 4, true, string.Empty),
                        new RewardGrantResult("ch01.m05.operation", RewardType.OperationInfrastructure, "port_breach", 5, true, string.Empty)
                    });

                controller.Bind(result);

                AssertText(root, "Frame/BodyRoot/RewardsPanel/CommanderXpReward/LabelText", "OPERATION SUPPLY");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/CommanderXpReward/ValueText", "+1");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/CreditsReward/LabelText", "SECURITY");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/CreditsReward/ValueText", "+4 PORT BREACH");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/LabelText", "INFRASTRUCTURE");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/MaterialsReward/ValueText", "+5 PORT BREACH");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/IntelReward/LabelText", "COMMAND XP");
                AssertText(root, "Frame/BodyRoot/RewardsPanel/IntelReward/ValueText", "+260");
            });
        }
        finally
        {
            WarlineCaptureMissionSession.Clear();
        }
    }

    private static void AssertPrefabExists(string prefabPath)
    {
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), prefabPath);
    }

    private static void AssertChildren(string prefabPath, params string[] childPaths)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.NotNull(prefab, prefabPath);
        foreach (string childPath in childPaths)
            Assert.NotNull(prefab.transform.Find(childPath), $"{prefabPath} missing {childPath}");
    }

    private static void AssertButtonTargetAndHeight(string prefabPath, string buttonPath, float minHeight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.NotNull(prefab, prefabPath);
        Transform buttonTransform = string.IsNullOrEmpty(buttonPath) ? prefab.transform : prefab.transform.Find(buttonPath);
        Assert.NotNull(buttonTransform, $"{prefabPath} missing {buttonPath}");

        Button button = buttonTransform.GetComponent<Button>();
        Assert.NotNull(button, $"{prefabPath} {buttonPath}");
        Assert.NotNull(button.targetGraphic, $"{prefabPath} {buttonPath}");
        Assert.GreaterOrEqual(((RectTransform)buttonTransform).rect.height, minHeight, $"{prefabPath} {buttonPath}");
    }

    private static void AssertImageSpritePath(GameObject root, string imagePath, string expectedSpritePath)
    {
        Transform imageTransform = root.transform.Find(imagePath);
        Assert.NotNull(imageTransform, $"{root.name} missing {imagePath}");

        Image image = imageTransform.GetComponent<Image>();
        Assert.NotNull(image, $"{root.name}:{imagePath}");
        Assert.NotNull(image.sprite, $"{root.name}:{imagePath}");
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), $"{root.name}:{imagePath}");
    }

    private static void AssertText(GameObject root, string textPath, string expectedText)
    {
        Transform textTransform = root.transform.Find(textPath);
        Assert.NotNull(textTransform, $"{root.name} missing {textPath}");
        TMP_Text text = textTransform.GetComponent<TMP_Text>();
        Assert.NotNull(text, $"{root.name}:{textPath}");
        Assert.AreEqual(expectedText, text.text, $"{root.name}:{textPath}");
    }

    private static void AssertPopupLayerPack(string surfaceId, int minimumLayerCount, params string[] requiredUnityDestinations)
    {
        string root = ResolveProjectPath($"Design/VisualLockLayered/{surfaceId}");
        string referencePath = Path.Combine(root, "reference", $"{surfaceId}_Landscape_Target.png");
        string manifestPath = Path.Combine(root, "layer_manifest.json");
        string contactSheetPath = Path.Combine(root, "generated_one_go", "layers_contact_sheet.png");
        string readmePath = Path.Combine(root, "README.md");
        string layersPath = Path.Combine(root, "layers");

        Assert.IsTrue(File.Exists(referencePath), referencePath);
        Assert.IsTrue(File.Exists(manifestPath), manifestPath);
        Assert.IsTrue(File.Exists(contactSheetPath), contactSheetPath);
        Assert.IsTrue(File.Exists(readmePath), readmePath);
        Assert.IsTrue(Directory.Exists(layersPath), layersPath);

        string[] layers = Directory.GetFiles(layersPath, "*.png");
        Assert.GreaterOrEqual(layers.Length, minimumLayerCount, surfaceId);

        string manifestText = File.ReadAllText(manifestPath).Replace("\\\\", "/");
        Assert.GreaterOrEqual(Regex.Matches(manifestText, "\"file\"").Count, minimumLayerCount, surfaceId);
        Assert.IsTrue(
            manifestText.Contains("\"transparentCornersRequired\"") || manifestText.Contains("\"sprite\""),
            $"{surfaceId} must declare sprite slicing/corner handling metadata.");
        StringAssert.Contains("\"doNotBakeWithTextOrIcons\"", manifestText, surfaceId);

        foreach (string unityDestination in requiredUnityDestinations)
            StringAssert.Contains(unityDestination, manifestText, $"{surfaceId} missing mapping for {unityDestination}");
    }

    private static string ResolveProjectPath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrEmpty(projectRoot))
        {
            string localCandidate = Path.Combine(projectRoot, relativePath);
            if (File.Exists(localCandidate) || Directory.Exists(localCandidate))
                return localCandidate;

            string linkedWorkspaceCandidate = Path.GetFullPath(Path.Combine(projectRoot, "..", "WarlineCapture", relativePath));
            if (File.Exists(linkedWorkspaceCandidate) || Directory.Exists(linkedWorkspaceCandidate))
                return linkedWorkspaceCandidate;
        }

        return Path.GetFullPath(relativePath);
    }

    private static void WithPrefab(string prefabPath, System.Action<GameObject> action)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.NotNull(prefab, prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            action(instance);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static string[] GetPhaseTwoPrefabPaths()
    {
        return new[]
        {
            $"{ComponentFolder}/ModeCardView.prefab",
            $"{ComponentFolder}/StatTileView.prefab",
            $"{ComponentFolder}/ResourceCounterView.prefab",
            $"{ComponentFolder}/ObjectiveRowView.prefab",
            $"{ComponentFolder}/RewardItemView.prefab",
            $"{ComponentFolder}/ActionButtonView.prefab",
            $"{ComponentFolder}/SegmentedControlView.prefab",
            $"{ComponentFolder}/ToggleRowView.prefab",
            $"{ComponentFolder}/SliderRowView.prefab",
            $"{PopupFolder}/PopupFrameView.prefab",
            $"{PopupFolder}/PauseMenuPopup.prefab",
            $"{PopupFolder}/ThreatAlertPopup.prefab",
            $"{PopupFolder}/BuildPlacementPanel.prefab",
            $"{PopupFolder}/ConfirmRaidPopup.prefab",
            $"{PopupFolder}/MissionResultPopup.prefab",
            $"{PopupFolder}/RewardUnlockPopup.prefab",
            $"{PopupFolder}/EndOfDayReportPopup.prefab",
            $"{PopupFolder}/IntelRevealPopup.prefab",
            $"{PopupFolder}/AbilityUpgradeDetailPopup.prefab"
        };
    }

    private static bool IsInteractiveRaycastGraphic(GameObject root, Graphic graphic)
    {
        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            if (selectable.targetGraphic == graphic)
                return true;
        }

        foreach (ScrollRect scrollRect in root.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scrollRect.GetComponent<Graphic>() == graphic)
                return true;

            if (scrollRect.viewport != null && scrollRect.viewport.GetComponent<Graphic>() == graphic)
                return true;
        }

        return string.Equals(graphic.name, "Scrim", System.StringComparison.OrdinalIgnoreCase);
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
