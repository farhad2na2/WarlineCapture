using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public sealed class WarlineCaptureUiQuickCustomTests
{
    private const string QuickCustomPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_QuickCustomSetup.prefab";
    private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab";
    private const string SettingsDropdownPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Dropdown_9Slice.png";
    private const string SettingsDropdownChevronPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Dropdown_Chevron.png";
    private const string QuickCustomMapPreviewPath = "Assets/Game/Art/UI/Generated/QuickCustom/Maps/QuickCustom_MapPreview_DesertOutpost.png";
    private const string QuickCustomInfoIconPath = "Assets/Game/Art/UI/Generated/QuickCustom/Icons/QuickCustom_Info.png";
    private const string QuickCustomResetIconPath = "Assets/Game/Art/UI/Generated/QuickCustom/Icons/QuickCustom_Reset.png";
    private const string QuickCustomMapBadgePath = "Assets/Game/Art/UI/Generated/QuickCustom/Icons/QuickCustom_MapBadge.png";
    private const string QuickCustomMoneyIconPath = "Assets/Game/Art/UI/Generated/QuickCustom/Icons/QuickCustom_Money.png";
    private const string QuickCustomPanelPath = "Assets/Game/Art/UI/Generated/QuickCustom/Frames/QuickCustom_Panel_9Slice.png";
    private const string QuickCustomCardPath = "Assets/Game/Art/UI/Generated/QuickCustom/Frames/QuickCustom_Card_9Slice.png";
    private const string QuickCustomButtonNormalPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_Button_Normal_9Slice.png";
    private const string QuickCustomButtonSelectedPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_Button_Selected_9Slice.png";
    private const string QuickCustomCheckboxBoxPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_Checkbox_Box.png";
    private const string QuickCustomCheckboxCheckPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_Checkbox_Check.png";
    private const string QuickCustomLaunchButtonPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_LaunchButton_9Slice.png";
    private const string QuickCustomLaunchArrowLeftPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_LaunchArrow_Left.png";
    private const string QuickCustomLaunchArrowRightPath = "Assets/Game/Art/UI/Generated/QuickCustom/Buttons/QuickCustom_LaunchArrow_Right.png";
    private const string QuickCustomMapsAtlasPath = "Assets/Game/Art/UI/Generated/QuickCustom/Atlases/QuickCustom_UI_Maps.spriteatlas";
    private const string QuickCustomIconsAtlasPath = "Assets/Game/Art/UI/Generated/QuickCustom/Atlases/QuickCustom_UI_Icons.spriteatlas";
    private const string QuickCustomFramesAtlasPath = "Assets/Game/Art/UI/Generated/QuickCustom/Atlases/QuickCustom_UI_Frames.spriteatlas";
    private const string QuickCustomButtonsAtlasPath = "Assets/Game/Art/UI/Generated/QuickCustom/Atlases/QuickCustom_UI_Buttons.spriteatlas";
    private const string QuickCustomMapsAtlasLabel = "Atlas_QuickCustom_Maps";
    private const string QuickCustomIconsAtlasLabel = "Atlas_QuickCustom_Icons";
    private const string QuickCustomFramesAtlasLabel = "Atlas_QuickCustom_Frames";
    private const string QuickCustomButtonsAtlasLabel = "Atlas_QuickCustom_Buttons";
    private const string OxaniumBoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
    private const string OxaniumLightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";
    private static readonly Vector2 QuickCustomReferenceSize = new(1672f, 941f);

    [Test]
    public void QuickCustomPrefab_HasPhaseFiveHierarchy()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertChildren(
            prefab,
            "HeaderBar",
            "HeaderBar/BackButton",
            "HeaderBar/TitleText",
            "BasicConfigurationPanel",
            "BasicConfigurationPanel/PresetDropdown",
            "BasicConfigurationPanel/PresetDropdown/Dropdown",
            "BasicConfigurationPanel/EnemyTypeDropdown",
            "BasicConfigurationPanel/EnemyCountStepper",
            "BasicConfigurationPanel/DifficultySegmented",
            "EconomyGameplayPanel",
            "EconomyGameplayPanel/StartingMoneySlider",
            "EconomyGameplayPanel/IncomeMultiplierSlider",
            "EconomyGameplayPanel/BuildSpeedSlider",
            "EconomyGameplayPanel/AggressionSlider",
            "RulesPanel",
            "RulesPanel/WinConditionDropdown",
            "RulesPanel/FogOfWarToggle",
            "RulesPanel/IntelRevealToggle",
            "RulesPanel/SuperWeaponsToggle",
            "RulesPanel/BaseRecoveryToggle",
            "RulesPanel/AlliancesToggle",
            "MapPreviewPanel",
            "MapPreviewPanel/PreviewImage",
            "MapPreviewPanel/MapNameCard",
            "MapPreviewPanel/MapNameCard/MapNameText",
            "MapPreviewPanel/MapNameCard/MapDifficultyText",
            "MapPreviewPanel/MapSizeStat/ValueText",
            "MapPreviewPanel/SeedInput",
            "BottomInfoPanel",
            "ResetButton",
            "LaunchButton");
    }

    [Test]
    public void QuickCustomPrefab_WiresControllerAndLaunchButton()
    {
        GameObject prefab = LoadQuickCustomPrefab();
        QuickCustomScreenController controller = prefab.GetComponent<QuickCustomScreenController>();
        Assert.NotNull(controller);

        var serializedObject = new SerializedObject(controller);
        AssertReference(serializedObject, "presetDropdown");
        AssertReference(serializedObject, "enemyTypeDropdown");
        AssertReference(serializedObject, "enemyCountStepper");
        AssertReference(serializedObject, "difficultySegmented");
        AssertReference(serializedObject, "startingMoneySlider");
        AssertReference(serializedObject, "incomeMultiplierSlider");
        AssertReference(serializedObject, "buildSpeedSlider");
        AssertReference(serializedObject, "aggressionSlider");
        AssertReference(serializedObject, "winConditionDropdown");
        AssertReference(serializedObject, "fogOfWarToggle");
        AssertReference(serializedObject, "intelRevealToggle");
        AssertReference(serializedObject, "seedInput");
        AssertReference(serializedObject, "mapNameText");
        AssertReference(serializedObject, "resetButton");
        AssertReference(serializedObject, "launchButton");

        Assert.NotNull(prefab.transform.Find("HeaderBar/BackButton").GetComponent<ScreenRouteButton>());
        Assert.NotNull(prefab.transform.Find("LaunchButton").GetComponent<Button>());
    }

    [Test]
    public void QuickCustomSliders_UseSettingsHandleGeometry()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertSliderHandleGeometry(prefab, "EconomyGameplayPanel/StartingMoneySlider/Slider");
        AssertSliderHandleGeometry(prefab, "EconomyGameplayPanel/IncomeMultiplierSlider/Slider");
        AssertSliderHandleGeometry(prefab, "EconomyGameplayPanel/BuildSpeedSlider/Slider");
        AssertSliderHandleGeometry(prefab, "EconomyGameplayPanel/AggressionSlider/Slider");
    }

    [Test]
    public void QuickCustomDropdowns_UseSettingsThinBorderArt()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertDropdownArt(prefab, "BasicConfigurationPanel/PresetDropdown/Dropdown");
        AssertDropdownArt(prefab, "BasicConfigurationPanel/EnemyTypeDropdown/Dropdown");
        AssertDropdownArt(prefab, "RulesPanel/WinConditionDropdown/Dropdown");
    }

    [Test]
    public void QuickCustomText_UsesLightFontExceptPageTitleAndDoesNotAutoWrap()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertScreenTypography(prefab, "HeaderBar/TitleText", "LaunchButton/LabelText");
    }

    [Test]
    public void QuickCustomStepper_ToggleAndLaunchGeometryMatchVisualLockRules()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        RectTransform plusButton = prefab.transform.Find("BasicConfigurationPanel/EnemyCountStepper/SegmentRoot/PlusButton") as RectTransform;
        Assert.NotNull(plusButton);
        Assert.LessOrEqual(plusButton.anchorMax.x, 0.96f);

        AssertImageSpritePath(prefab.transform, "RulesPanel/IntelRevealToggle/Toggle", QuickCustomCheckboxBoxPath);
        AssertImageSpritePath(prefab.transform, "RulesPanel/IntelRevealToggle/Toggle/Checkmark", QuickCustomCheckboxCheckPath);
        AssertImageSpritePath(prefab.transform, "LaunchButton/LeftChevronIcon", QuickCustomLaunchArrowLeftPath);
        AssertImageSpritePath(prefab.transform, "LaunchButton/RightChevronIcon", QuickCustomLaunchArrowRightPath);

        TMP_Text launchLabel = prefab.transform.Find("LaunchButton/LabelText").GetComponent<TMP_Text>();
        Assert.NotNull(launchLabel);
        Assert.LessOrEqual(launchLabel.fontSize, 36f);
        Assert.Greater(launchLabel.color.a, 0.95f, "Launch text must be real TMP text, not baked invisibly into the button background.");
        Assert.Greater(prefab.transform.Find("LaunchButton/LeftChevronIcon").GetComponent<Image>().color.a, 0.5f);
        Assert.Greater(prefab.transform.Find("LaunchButton/RightChevronIcon").GetComponent<Image>().color.a, 0.5f);
    }

    [Test]
    public void QuickCustomUsesTargetSpecificChromeInsteadOfSettingsSectionFrames()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertImageSpritePath(prefab.transform, "BasicConfigurationPanel", QuickCustomPanelPath);
        AssertImageSpritePath(prefab.transform, "EconomyGameplayPanel", QuickCustomPanelPath);
        AssertImageSpritePath(prefab.transform, "RulesPanel", QuickCustomPanelPath);
        AssertImageSpritePath(prefab.transform, "MapPreviewPanel", QuickCustomPanelPath);
        AssertImageSpritePath(prefab.transform, "BottomInfoPanel", QuickCustomPanelPath);
        AssertImageSpritePath(prefab.transform, "MapPreviewPanel/MapNameCard", QuickCustomCardPath);
        AssertImageSpritePath(prefab.transform, "MapPreviewPanel/MapSizeStat", QuickCustomCardPath);
        AssertImageSpritePath(prefab.transform, "MapPreviewPanel/VictoryPointsStat", QuickCustomCardPath);
        AssertImageSpritePath(prefab.transform, "MapPreviewPanel/PlayersStat", QuickCustomCardPath);
        AssertImageSpritePath(prefab.transform, "ResetButton", QuickCustomButtonNormalPath);
        AssertImageSpritePath(prefab.transform, "ResetButton/ResetIcon", QuickCustomResetIconPath);
        AssertImageSpritePath(prefab.transform, "LaunchButton", QuickCustomLaunchButtonPath);

        AssertImageSpritePath(prefab.transform, "BasicConfigurationPanel/DifficultySegmented/SegmentRoot/Segment_1", QuickCustomButtonNormalPath);
        AssertImageSpritePath(prefab.transform, "BasicConfigurationPanel/DifficultySegmented/SegmentRoot/Segment_3", QuickCustomButtonSelectedPath);
        AssertImageSpritePath(prefab.transform, "BasicConfigurationPanel/EnemyCountStepper/SegmentRoot/ValueButton", QuickCustomButtonSelectedPath);
    }

    [Test]
    public void QuickCustomGeneratedArtIsAtlasReadyAndDecorativeGraphicsDoNotRaycast()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertSpriteAtlas(QuickCustomMapsAtlasPath, "Assets/Game/Art/UI/Generated/QuickCustom/Maps");
        AssertSpriteAtlas(QuickCustomIconsAtlasPath, "Assets/Game/Art/UI/Generated/QuickCustom/Icons");
        AssertSpriteAtlas(QuickCustomFramesAtlasPath, "Assets/Game/Art/UI/Generated/QuickCustom/Frames");
        AssertSpriteAtlas(QuickCustomButtonsAtlasPath, "Assets/Game/Art/UI/Generated/QuickCustom/Buttons");

        AssertAtlasLabel(QuickCustomMapPreviewPath, QuickCustomMapsAtlasLabel);
        AssertAtlasLabel(QuickCustomInfoIconPath, QuickCustomIconsAtlasLabel);
        AssertAtlasLabel(QuickCustomMapBadgePath, QuickCustomIconsAtlasLabel);
        AssertAtlasLabel(QuickCustomMoneyIconPath, QuickCustomIconsAtlasLabel);
        AssertAtlasLabel(QuickCustomPanelPath, QuickCustomFramesAtlasLabel);
        AssertAtlasLabel(QuickCustomCardPath, QuickCustomFramesAtlasLabel);
        AssertAtlasLabel(QuickCustomButtonNormalPath, QuickCustomButtonsAtlasLabel);
        AssertAtlasLabel(QuickCustomButtonSelectedPath, QuickCustomButtonsAtlasLabel);
        AssertAtlasLabel(QuickCustomLaunchButtonPath, QuickCustomButtonsAtlasLabel);
        AssertAtlasLabel(QuickCustomCheckboxBoxPath, QuickCustomButtonsAtlasLabel);

        AssertUiSpriteImporter(QuickCustomMapPreviewPath);
        AssertUiSpriteImporter(QuickCustomPanelPath);
        AssertUiSpriteImporter(QuickCustomButtonNormalPath);
        AssertUiSpriteImporter(QuickCustomLaunchButtonPath);

        Assert.IsNull(prefab.GetComponent<Image>().sprite, "Quick Custom must be built from canvas pieces, not a flat visual-target screenshot.");

        foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
        {
            bool expectedRaycast = IsInteractiveRaycastGraphic(prefab, graphic);
            Assert.AreEqual(expectedRaycast, graphic.raycastTarget, $"{GetHierarchyPath(graphic.transform)} has an incorrect raycastTarget value.");
        }

        foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            Assert.IsFalse(image.sprite == null && Mathf.Approximately(image.color.a, 0f), $"{GetHierarchyPath(image.transform)} is a transparent placeholder Image and should be removed.");
    }

    [Test]
    public void QuickCustomSegmentedControlsRefreshSelectedArtWhenBound()
    {
        GameObject prefab = LoadQuickCustomPrefab();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            QuickCustomScreenController controller = instance.GetComponent<QuickCustomScreenController>();
            Assert.NotNull(controller);

            QuickGameConfig config = QuickGameConfig.Defaults;
            config.Difficulty = AIDifficultySetting.Easy;
            controller.Bind(config);

            AssertImageSpritePath(instance.transform, "BasicConfigurationPanel/DifficultySegmented/SegmentRoot/Segment_1", QuickCustomButtonSelectedPath);
            AssertImageSpritePath(instance.transform, "BasicConfigurationPanel/DifficultySegmented/SegmentRoot/Segment_3", QuickCustomButtonNormalPath);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void QuickCustomCriticalText_Has20x9SafeReadableBounds()
    {
        GameObject prefab = LoadQuickCustomPrefab();

        AssertReadableTextRect(prefab, "HeaderBar/TitleText", 1000f, 70f, true);
        AssertReadableTextRect(prefab, "BasicConfigurationPanel/SectionTitleText", 320f, 36f, true);
        AssertReadableTextRect(prefab, "EconomyGameplayPanel/BuildSpeedSlider/ValueText", 96f, 30f, true);
        AssertReadableTextRect(prefab, "MapPreviewPanel/MapNameCard/MapDifficultyText", 250f, 24f, true);
        AssertReadableTextRect(prefab, "MapPreviewPanel/MapSizeStat/ValueText", 70f, 22f, true);

        AssertRuleDescriptionReadable(prefab, "RulesPanel/FogOfWarToggle/DescriptionText", "Hide unexplored areas.");
        AssertRuleDescriptionReadable(prefab, "RulesPanel/IntelRevealToggle/DescriptionText", "Reveal enemy tech on scout.");
        AssertRuleDescriptionReadable(prefab, "RulesPanel/SuperWeaponsToggle/DescriptionText", "Enable super weapons.");
        AssertRuleDescriptionReadable(prefab, "RulesPanel/BaseRecoveryToggle/DescriptionText", "Allow structures to heal.");
        AssertRuleDescriptionReadable(prefab, "RulesPanel/AlliancesToggle/DescriptionText", "Enable player alliances.");
    }

    [Test]
    public void MainMenu_QuickCustomRoutesToSetupInsteadOfStartingLegacyGame()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        Assert.NotNull(prefab);

        Transform buttonTransform = prefab.transform.Find("ModeCardList/ModeCard_QuickCustom/Button");
        Assert.NotNull(buttonTransform);
        Assert.IsNull(buttonTransform.GetComponent<WarlineCaptureLegacyGameStartButton>());

        ScreenRouteButton routeButton = buttonTransform.GetComponent<ScreenRouteButton>();
        Assert.NotNull(routeButton);
        var serializedObject = new SerializedObject(routeButton);
        Assert.AreEqual((int)WarlineCaptureRoute.QuickCustomSetup, serializedObject.FindProperty("route").enumValueIndex);
    }

    [Test]
    public void MainMenuQuickCustomButtonClick_OpensQuickCustomSetupScreen()
    {
        GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        GameObject quickCustomPrefab = LoadQuickCustomPrefab();
        GameObject routerRoot = new("WarlineCaptureAppCanvas");
        GameObject mainMenu = null;
        GameObject quickCustom = null;

        try
        {
            WarlineCaptureRouter router = routerRoot.AddComponent<WarlineCaptureRouter>();
            mainMenu = (GameObject)PrefabUtility.InstantiatePrefab(mainMenuPrefab);
            quickCustom = (GameObject)PrefabUtility.InstantiatePrefab(quickCustomPrefab);
            mainMenu.transform.SetParent(routerRoot.transform, false);
            quickCustom.transform.SetParent(routerRoot.transform, false);

            router.ConfigureForTests(
                new[]
                {
                    mainMenu.GetComponent<WarlineCaptureScreenController>(),
                    quickCustom.GetComponent<WarlineCaptureScreenController>()
                },
                WarlineCaptureRoute.MainMenu);

            Button quickCustomButton = mainMenu.transform.Find("ModeCardList/ModeCard_QuickCustom/Button").GetComponent<Button>();
            ScreenRouteButton routeButton = quickCustomButton.GetComponent<ScreenRouteButton>();
            Assert.NotNull(routeButton);
            InvokeAwake(routeButton);

            quickCustomButton.onClick.Invoke();

            Assert.AreEqual(WarlineCaptureRoute.QuickCustomSetup, router.ActiveRoute);
            Assert.IsFalse(mainMenu.activeSelf);
            Assert.IsTrue(quickCustom.activeSelf);
        }
        finally
        {
            if (quickCustom != null)
                Object.DestroyImmediate(quickCustom);
            if (mainMenu != null)
                Object.DestroyImmediate(mainMenu);
            Object.DestroyImmediate(routerRoot);
        }
    }

    [Test]
    public void QuickCustomLaunchMission_StartsSkirmishWithoutMissionSession()
    {
        GameObject prefab = LoadQuickCustomPrefab();
        GameObject legacyCanvas = new("UI_Canvas");
        GameObject routerRoot = new("WarlineCaptureAppCanvas");
        GameObject instance = null;

        try
        {
            legacyCanvas.SetActive(false);
            routerRoot.AddComponent<WarlineCaptureRouter>();
            CreateRouterScreen(routerRoot.transform, WarlineCaptureRoute.Match);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(routerRoot.transform, false);
            QuickCustomScreenController controller = instance.GetComponent<QuickCustomScreenController>();
            Assert.NotNull(controller);

            controller.Bind(QuickGameConfig.Defaults);
            controller.LaunchMission();

            Assert.IsTrue(legacyCanvas.activeSelf);
            Assert.IsFalse(routerRoot.activeSelf);
            Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission);
        }
        finally
        {
            AISettingsRuntimeState.ResetDefaults();
            WarlineCaptureMissionSession.Clear();
            if (instance != null)
                Object.DestroyImmediate(instance);
            Object.DestroyImmediate(routerRoot);
            Object.DestroyImmediate(legacyCanvas);
        }
    }

    [Test]
    public void QuickCustomLaunchButtonClick_StartsSkirmishWithoutMissionSession()
    {
        GameObject prefab = LoadQuickCustomPrefab();
        GameObject legacyCanvas = new("UI_Canvas");
        GameObject routerRoot = new("WarlineCaptureAppCanvas");
        GameObject instance = null;

        try
        {
            legacyCanvas.SetActive(false);
            routerRoot.AddComponent<WarlineCaptureRouter>();
            CreateRouterScreen(routerRoot.transform, WarlineCaptureRoute.Match);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(routerRoot.transform, false);

            QuickCustomScreenController controller = instance.GetComponent<QuickCustomScreenController>();
            Assert.NotNull(controller);
            InvokeAwake(controller);

            Button launchButton = instance.transform.Find("LaunchButton").GetComponent<Button>();
            Assert.NotNull(launchButton);
            launchButton.onClick.Invoke();

            Assert.IsTrue(legacyCanvas.activeSelf);
            Assert.IsFalse(routerRoot.activeSelf);
            Assert.IsFalse(WarlineCaptureMissionSession.HasActiveMission);
        }
        finally
        {
            AISettingsRuntimeState.ResetDefaults();
            WarlineCaptureMissionSession.Clear();
            if (instance != null)
                Object.DestroyImmediate(instance);
            Object.DestroyImmediate(routerRoot);
            Object.DestroyImmediate(legacyCanvas);
        }
    }

    [Test]
    public void QuickGameConfig_AppliesToAISettingsRuntimeState()
    {
        AISettingsRuntimeState.ResetDefaults();
        QuickGameConfig config = QuickGameConfig.Defaults;
        config.EnemyCount = 3;
        config.Difficulty = AIDifficultySetting.Brutal;
        config.StartingMoney = AIStartingMoneySetting.High;
        config.IncomeMultiplier = 2.5f;
        config.BuildSpeed = AISpeedSetting.Fast;
        config.UnitProductionSpeed = AISpeedSetting.Fast;
        config.AttackGroupSize = AIAttackGroupSizeSetting.Large;
        config.AttackFrequency = AIAttackFrequencySetting.Frequent;
        config.Aggression = AIAggressionSetting.Aggressive;
        config.Expansion = AIExpansionSetting.Fast;
        config.TargetPriority = AITargetPriority.Production;
        config.PlayerAutoAIEnabled = true;

        config.ApplyToRuntimeState();

        Assert.AreEqual(3, AISettingsRuntimeState.EnemyAICount);
        Assert.AreEqual(AIDifficultySetting.Brutal, AISettingsRuntimeState.Difficulty);
        Assert.AreEqual(AIStartingMoneySetting.High, AISettingsRuntimeState.StartingMoney);
        Assert.AreEqual(2.5f, AISettingsRuntimeState.IncomeMultiplier);
        Assert.AreEqual(AISpeedSetting.Fast, AISettingsRuntimeState.BuildSpeed);
        Assert.AreEqual(AISpeedSetting.Fast, AISettingsRuntimeState.UnitProductionSpeed);
        Assert.AreEqual(AIAttackGroupSizeSetting.Large, AISettingsRuntimeState.AttackGroupSize);
        Assert.AreEqual(AIAttackFrequencySetting.Frequent, AISettingsRuntimeState.AttackFrequency);
        Assert.AreEqual(AIAggressionSetting.Aggressive, AISettingsRuntimeState.Aggression);
        Assert.AreEqual(AIExpansionSetting.Fast, AISettingsRuntimeState.Expansion);
        Assert.AreEqual(AITargetPriority.Production, AISettingsRuntimeState.TargetPriority);
        Assert.IsTrue(AISettingsRuntimeState.PlayerAutoAIEnabled);

        AISettingsRuntimeState.ResetDefaults();
    }

    [Test]
    public void QuickCustomController_BindsAndAppliesSelectedConfig()
    {
        GameObject prefab = LoadQuickCustomPrefab();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            QuickCustomScreenController controller = instance.GetComponent<QuickCustomScreenController>();
            Assert.NotNull(controller);

            QuickGameConfig config = QuickGameConfig.Defaults;
            config.EnemyType = QuickGameEnemyType.Air;
            config.EnemyCount = 2;
            config.Difficulty = AIDifficultySetting.Hard;
            config.StartingMoney = AIStartingMoneySetting.Low;
            config.IncomeMultiplier = 1.5f;
            config.BuildSpeed = AISpeedSetting.Slow;
            config.UnitProductionSpeed = AISpeedSetting.Fast;
            config.AttackGroupSize = AIAttackGroupSizeSetting.Small;
            config.AttackFrequency = AIAttackFrequencySetting.Rare;
            config.Aggression = AIAggressionSetting.Defensive;
            config.Expansion = AIExpansionSetting.Off;
            config.TargetPriority = AITargetPriority.Units;
            config.PlayerAutoAIEnabled = true;
            config.WinCondition = QuickGameWinCondition.SurviveDuration;
            config.FogOfWar = true;
            config.IntelReveal = false;
            config.StartingResources = QuickGameStartingResources.High;
            config.MapSeed = 777;

            controller.Bind(config);
            QuickGameConfig selected = controller.ReadConfigFromControls();

            Assert.AreEqual("Hide unexplored areas.", instance.transform.Find("RulesPanel/FogOfWarToggle/DescriptionText").GetComponent<TMP_Text>().text);
            Assert.AreEqual("Reveal enemy tech on scout.", instance.transform.Find("RulesPanel/IntelRevealToggle/DescriptionText").GetComponent<TMP_Text>().text);

            Assert.AreEqual(config.EnemyType, selected.EnemyType);
            Assert.AreEqual(config.EnemyCount, selected.EnemyCount);
            Assert.AreEqual(config.Difficulty, selected.Difficulty);
            Assert.AreEqual(config.StartingMoney, selected.StartingMoney);
            Assert.AreEqual(config.IncomeMultiplier, selected.IncomeMultiplier);
            Assert.AreEqual(config.BuildSpeed, selected.BuildSpeed);
            Assert.AreEqual(config.UnitProductionSpeed, selected.UnitProductionSpeed);
            Assert.AreEqual(config.AttackGroupSize, selected.AttackGroupSize);
            Assert.AreEqual(config.AttackFrequency, selected.AttackFrequency);
            Assert.AreEqual(config.Aggression, selected.Aggression);
            Assert.AreEqual(config.Expansion, selected.Expansion);
            Assert.AreEqual(config.TargetPriority, selected.TargetPriority);
            Assert.AreEqual(config.PlayerAutoAIEnabled, selected.PlayerAutoAIEnabled);
            Assert.AreEqual(config.WinCondition, selected.WinCondition);
            Assert.AreEqual(config.FogOfWar, selected.FogOfWar);
            Assert.AreEqual(config.IntelReveal, selected.IntelReveal);
            Assert.AreEqual(config.StartingResources, selected.StartingResources);
            Assert.AreEqual(config.MapSeed, selected.MapSeed);

            AISettingsRuntimeState.ResetDefaults();
            controller.ApplyCurrentConfigToRuntime();
            Assert.AreEqual(config.Difficulty, AISettingsRuntimeState.Difficulty);
            Assert.AreEqual(config.EnemyCount, AISettingsRuntimeState.EnemyAICount);
            Assert.AreEqual(config.PlayerAutoAIEnabled, AISettingsRuntimeState.PlayerAutoAIEnabled);
            AISettingsRuntimeState.ResetDefaults();
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject LoadQuickCustomPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuickCustomPrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static void AssertChildren(GameObject prefab, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(prefab.transform.Find(path), $"Missing {path}");
    }

    private static void AssertReference(SerializedObject serializedObject, string propertyName)
    {
        Assert.NotNull(serializedObject.FindProperty(propertyName).objectReferenceValue, propertyName);
    }

    private static void AssertDropdownArt(GameObject prefab, string dropdownPath)
    {
        AssertImageSpritePath(prefab.transform, dropdownPath, SettingsDropdownPath);
        AssertImageSpritePath(prefab.transform, $"{dropdownPath}/Arrow", SettingsDropdownChevronPath);
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

    private static void AssertScreenTypography(GameObject prefab, params string[] boldTextPaths)
    {
        var boldTargets = new HashSet<Transform>();
        foreach (string boldTextPath in boldTextPaths)
        {
            Transform boldTarget = prefab.transform.Find(boldTextPath);
            Assert.NotNull(boldTarget, boldTextPath);
            boldTargets.Add(boldTarget);
        }

        foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = GetHierarchyPath(text.transform);
            string expectedFontPath = boldTargets.Contains(text.transform) ? OxaniumBoldFontPath : OxaniumLightFontPath;
            Assert.NotNull(text.font, path);
            Assert.AreEqual(expectedFontPath, AssetDatabase.GetAssetPath(text.font), path);
            AssertSingleLineWrapping(text, path);
        }
    }

    private static void AssertSingleLineWrapping(TMP_Text text, string path)
    {
        bool isNoWrap =
            text.textWrappingMode == TextWrappingModes.NoWrap ||
            text.textWrappingMode == TextWrappingModes.PreserveWhitespaceNoWrap;
        Assert.IsTrue(isNoWrap, path);
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

    private static void AssertSliderHandleGeometry(GameObject prefab, string sliderPath)
    {
        RectTransform handleArea = prefab.transform.Find($"{sliderPath}/Handle Slide Area") as RectTransform;
        RectTransform handle = prefab.transform.Find($"{sliderPath}/Handle Slide Area/Handle") as RectTransform;
        Assert.NotNull(handleArea, sliderPath);
        Assert.NotNull(handle, sliderPath);

        Assert.AreEqual(handleArea.anchorMin.y, handleArea.anchorMax.y, 0.0001f, sliderPath);
        Assert.AreEqual(0.5f, handleArea.anchorMin.y, 0.0001f, sliderPath);
        Assert.AreEqual(0f, handleArea.offsetMin.y, 0.0001f, sliderPath);
        Assert.AreEqual(0f, handleArea.offsetMax.y, 0.0001f, sliderPath);
        Assert.AreEqual(handle.sizeDelta.x, handle.sizeDelta.y, 0.0001f, sliderPath);
        Assert.LessOrEqual(handle.sizeDelta.y, 36f, sliderPath);
        Assert.NotNull(handle.Find("HandleCore"), sliderPath);
    }

    private static void AssertSpriteAtlas(string atlasPath, params string[] expectedPackablePaths)
    {
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        Assert.NotNull(atlas, $"{atlasPath} is missing.");

        Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
        foreach (string expectedPackablePath in expectedPackablePaths)
        {
            bool found = false;
            for (int i = 0; i < packables.Length; i++)
            {
                if (AssetDatabase.GetAssetPath(packables[i]) == expectedPackablePath)
                    found = true;
            }

            Assert.IsTrue(found, $"{atlasPath} must include packable '{expectedPackablePath}'.");
        }
    }

    private static void AssertAtlasLabel(string assetPath, string expectedLabel)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        Assert.NotNull(asset, assetPath);
        CollectionAssert.Contains(AssetDatabase.GetLabels(asset), "WarlineCaptureUI", assetPath);
        CollectionAssert.Contains(AssetDatabase.GetLabels(asset), expectedLabel, assetPath);
    }

    private static void AssertUiSpriteImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.NotNull(importer, path);
        Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
        Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, path);
        Assert.IsFalse(importer.mipmapEnabled, path);
        Assert.IsTrue(importer.alphaIsTransparency, path);
        Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, path);
        Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, path);

        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        Assert.IsTrue(android.overridden, path);
        Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format, path);
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

    private static void AssertReadableTextRect(GameObject prefab, string textPath, float minWidth, float minHeight, bool requireAutoSizing)
    {
        RectTransform rect = prefab.transform.Find(textPath) as RectTransform;
        Assert.NotNull(rect, textPath);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        Assert.NotNull(text, textPath);
        Vector2 size = GetReferenceRectSize(rect);
        Assert.GreaterOrEqual(size.x, minWidth, textPath);
        Assert.GreaterOrEqual(size.y, minHeight, textPath);
        if (requireAutoSizing)
            Assert.IsTrue(text.enableAutoSizing, textPath);
    }

    private static void AssertRuleDescriptionReadable(GameObject prefab, string textPath, string expectedText)
    {
        RectTransform rect = prefab.transform.Find(textPath) as RectTransform;
        Assert.NotNull(rect, textPath);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        Assert.NotNull(text, textPath);
        Assert.AreEqual(expectedText, text.text, textPath);
        Assert.GreaterOrEqual(GetReferenceRectSize(rect).x, 180f, textPath);
        Assert.LessOrEqual(text.fontSizeMax, 13f, textPath);
        Assert.IsTrue(text.enableAutoSizing, textPath);
    }

    private static Vector2 GetReferenceRectSize(RectTransform rect)
    {
        if (rect.parent is not RectTransform parent)
            return QuickCustomReferenceSize;

        Vector2 parentSize = GetReferenceRectSize(parent);
        return new Vector2(
            (rect.anchorMax.x - rect.anchorMin.x) * parentSize.x + rect.offsetMax.x - rect.offsetMin.x,
            (rect.anchorMax.y - rect.anchorMin.y) * parentSize.y + rect.offsetMax.y - rect.offsetMin.y);
    }

    private static void InvokeAwake(MonoBehaviour behaviour)
    {
        MethodInfo awake = behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake, behaviour.GetType().Name);
        awake.Invoke(behaviour, null);
    }

    private static WarlineCaptureScreenController CreateRouterScreen(Transform parent, WarlineCaptureRoute route)
    {
        GameObject screen = new($"Screen_{route}", typeof(RectTransform));
        screen.transform.SetParent(parent, false);
        WarlineCaptureScreenController controller = screen.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(route);
        return controller;
    }
}
