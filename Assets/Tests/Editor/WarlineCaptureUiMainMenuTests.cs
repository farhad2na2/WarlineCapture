using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public sealed class WarlineCaptureUiMainMenuTests
{
    private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab";
    private const string BackgroundTacticalMapPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Backgrounds/main_menu_background_tactical_map.png";
    private const string BrandLogoPanelPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/brand_logo_panel_frame.png";
    private const string BrandEmblemPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/brand_emblem.png";
    private const string TopResourceBarFramePath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/top_resource_bar_frame_full.png";
    private const string ResourceCounterSlotPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/resource_counter_slot_frame.png";
    private const string SettingsButtonFramePath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/settings_button_frame.png";
    private const string CommanderProfilePanelPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/commander_profile_panel_frame.png";
    private const string LeftNavRowPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/left_nav_row_frame.png";
    private const string CardFrameMaskPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/mode_card_frame_large.png";
    private const string CardContentMaskPath = "Assets/Game/Art/UI/Generated/MainMenu/Frames/MainMenu_Card_ContentMask.png";
    private const string DesignedUnavailableBadgePath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/designed_unavailable_badge.png";
    private const string DeployCommandButtonFramePath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons/deploy_command_button_frame.png";
    private const string CommandFeedPanelFramePath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames/command_feed_panel_frame.png";
    private const string CommanderProfilePortraitPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/commander_profile_portrait.png";
    private const string CreditsIconPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_credits.png";
    private const string MaterialsIconPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_materials.png";
    private const string CommandAuthorityIconPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons/icon_command_authority.png";
    private const string SagaArtPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_saga.png";
    private const string OperationArtPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_operation.png";
    private const string QuickCustomArtPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/mode_card_art_quick_custom.png";
    private const string OperationPressureMeterPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/operation_pressure_meter_segments.png";
    private const string OperationRiskMeterPath = "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/operation_risk_meter_segments.png";
    private const string UiButtonAnimatorControllerPath = "Assets/Game/Animations/UI/WarlineCaptureButtonStates.overrideController";
    private const string UiButtonAnimatorBaseControllerPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Animation/AC_Button_MilitaryCombat_Basic_01.controller";
    private const string UiButtonNormalClipPath = "Assets/Game/Animations/UI/WarlineCapture_Button_Normal.anim";
    private const string UiButtonHighlightedClipPath = "Assets/Game/Animations/UI/WarlineCapture_Button_Highlighted.anim";
    private const string UiButtonPressedClipPath = "Assets/Game/Animations/UI/WarlineCapture_Button_Pressed.anim";
    private const string UiButtonSelectedClipPath = "Assets/Game/Animations/UI/WarlineCapture_Button_Selected.anim";
    private const string UiButtonDisabledClipPath = "Assets/Game/Animations/UI/WarlineCapture_Button_Disabled.anim";
    private const string IconsButtonsAtlasPath = "Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_IconsButtons.spriteatlas";
    private const string FramesChromeAtlasPath = "Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_FramesChrome.spriteatlas";
    private const string CardArtAtlasPath = "Assets/Game/Art/UI/Generated/MainMenu/Atlases/MainMenu_UI_CardArt.spriteatlas";
    private const string IconsButtonsAtlasLabel = "Atlas_MainMenu_IconsButtons";
    private const string FramesChromeAtlasLabel = "Atlas_MainMenu_FramesChrome";
    private const string CardArtAtlasLabel = "Atlas_MainMenu_CardArt";
    private const string OxaniumFontFolder = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/";

    [Test]
    public void MainMenu_HasPhaseThreeHierarchy()
    {
        GameObject prefab = LoadMainMenuPrefab();

        AssertChildren(
            prefab,
            "TopProfileBar",
            "TopProfileBar/LogoPanelFrame",
            "TopProfileBar/BrandEmblem",
            "TopProfileBar/MastheadText",
            "TopProfileBar/CommandDeckText",
            "TopProfileBar/TopResourceBarFrame",
            "TopProfileBar/ResourceCounterList",
            "TopProfileBar/ResourceCounterList/Resource_Money",
            "TopProfileBar/ResourceCounterList/Resource_Trust",
            "TopProfileBar/ResourceCounterList/Resource_Intel",
            "TopProfileBar/SettingsButton",
            "TopProfileBar/SettingsButton/SettingsGearIcon",
            "LeftNav",
            "LeftNav/CommanderProfilePanel",
            "LeftNav/CommanderProfilePanel/CommanderProfileTitleText",
            "LeftNav/CommanderProfilePanel/ProfileAvatar",
            "LeftNav/CommanderProfilePanel/ProfileNameText",
            "LeftNav/InboxButton",
            "LeftNav/InboxButton/Icon",
            "LeftNav/StoreButton",
            "LeftNav/StoreButton/Icon",
            "LeftNav/EventsButton",
            "LeftNav/EventsButton/Icon",
            "LeftNav/RankingButton",
            "LeftNav/RankingButton/Icon",
            "LeftNav/CommandFeedRouteButton",
            "LeftNav/CommandFeedRouteButton/Icon",
            "ModeCardList",
            "ModeCardList/ModeCard_Saga",
            "ModeCardList/ModeCard_Saga/ArtClip",
            "ModeCardList/ModeCard_Saga/BodyText",
            "ModeCardList/ModeCard_Operation",
            "ModeCardList/ModeCard_Operation/OperationWarningIcon",
            "ModeCardList/ModeCard_Operation/DistrictPressureRow",
            "ModeCardList/ModeCard_Operation/CityRiskRow",
            "ModeCardList/ModeCard_QuickCustom",
            "ModeCardList/ModeCard_QuickCustom/ArtClip",
            "ModeCardList/ModeCard_QuickCustom/BodyText",
            "DeployCommandButton",
            "DeployCommandButton/LabelText",
            "DeployCommandButton/DeployCommandChevrons",
            "WideAspectOnlyRoot/CommandFeedPanelFrame",
            "WideAspectOnlyRoot/CommandFeedIcon");
    }

    [Test]
    public void MainMenu_UsesProjectLogoAndExpectedPreviewData()
    {
        GameObject prefab = LoadMainMenuPrefab();

        var backgroundImage = prefab.GetComponent<Image>();
        Assert.NotNull(backgroundImage);
        Assert.IsNull(backgroundImage.sprite, "Main Menu must not use the visual target as a flat full-screen background.");

        AssertStretchedSprite(prefab, "MainMenuBackgroundTacticalMap", BackgroundTacticalMapPath);
        var logoImage = prefab.transform.Find("TopProfileBar/LogoPanelFrame").GetComponent<Image>();
        Assert.NotNull(logoImage.sprite);
        Assert.AreEqual(BrandLogoPanelPath, AssetDatabase.GetAssetPath(logoImage.sprite));
        AssertSprite(prefab, "TopProfileBar/BrandEmblem", BrandEmblemPath);

        AssertModeCardArt(prefab, "ModeCardList/ModeCard_Saga", SagaArtPath);
        AssertModeCardArt(prefab, "ModeCardList/ModeCard_Operation", OperationArtPath);
        AssertModeCardArt(prefab, "ModeCardList/ModeCard_QuickCustom", QuickCustomArtPath);

        AssertStretchedSprite(prefab, "LeftNav/CommanderProfilePanel/ProfileAvatar", CommanderProfilePortraitPath);
        AssertText(prefab, "LeftNav/CommanderProfilePanel/ProfileNameText", "Profile data pending");
        AssertText(prefab, "TopProfileBar/MastheadText", "WARLINE");
        AssertText(prefab, "TopProfileBar/CommandDeckText", "CAPTURE");
        AssertText(prefab, "TopProfileBar/ResourceCounterList/Resource_Money/LabelText", "Credits");
        AssertText(prefab, "TopProfileBar/ResourceCounterList/Resource_Trust/LabelText", "Materials");
        AssertText(prefab, "TopProfileBar/ResourceCounterList/Resource_Intel/LabelText", "Command Authority");
        AssertResourceCounter(prefab, "TopProfileBar/ResourceCounterList/Resource_Money", "187,540");
        AssertResourceCounter(prefab, "TopProfileBar/ResourceCounterList/Resource_Trust", "92,860");
        AssertResourceCounter(prefab, "TopProfileBar/ResourceCounterList/Resource_Intel", "2,715");
        AssertText(prefab, "ModeCardList/ModeCard_Saga/SubtitleText", "Chapter 1: First Contact");
        AssertText(prefab, "ModeCardList/ModeCard_Operation/SubtitleText", "District pressure rising");
        AssertText(prefab, "ModeCardList/ModeCard_QuickCustom/SubtitleText", "Skirmish setup route available.");
        AssertText(prefab, "DeployCommandButton/LabelText", "DEPLOY COMMAND");
        AssertText(prefab, "WideAspectOnlyRoot/CommandFeedTitleText", "Command Feed");
    }

    [Test]
    public void MainMenu_UsesMockupSectionPlatesInsteadOfSyntheticCorners()
    {
        GameObject prefab = LoadMainMenuPrefab();

        Assert.IsNull(prefab.GetComponent<AspectRatioFitter>(), "Main menu must fill wide devices instead of letterboxing to one aspect ratio.");

        AssertScalableSprite(prefab, "TopProfileBar/LogoPanelFrame", BrandLogoPanelPath);
        AssertScalableSprite(prefab, "TopProfileBar/TopResourceBarFrame", TopResourceBarFramePath);
        AssertScalableSprite(prefab, "TopProfileBar/ResourceCounterList/Resource_Money", ResourceCounterSlotPath);
        AssertScalableSprite(prefab, "TopProfileBar/ResourceCounterList/Resource_Trust", ResourceCounterSlotPath);
        AssertScalableSprite(prefab, "TopProfileBar/ResourceCounterList/Resource_Intel", ResourceCounterSlotPath);
        AssertScalableSprite(prefab, "TopProfileBar/SettingsButton", SettingsButtonFramePath);
        AssertScalableSprite(prefab, "LeftNav/CommanderProfilePanel", CommanderProfilePanelPath);
        AssertScalableSprite(prefab, "ModeCardList/ModeCard_Saga/FrameOverlay", CardFrameMaskPath);
        AssertScalableSprite(prefab, "ModeCardList/ModeCard_Operation/FrameOverlay", CardFrameMaskPath);
        AssertScalableSprite(prefab, "ModeCardList/ModeCard_QuickCustom/FrameOverlay", CardFrameMaskPath);
        AssertModeCardMask(prefab, "ModeCardList/ModeCard_Saga");
        AssertModeCardMask(prefab, "ModeCardList/ModeCard_Operation");
        AssertModeCardMask(prefab, "ModeCardList/ModeCard_QuickCustom");
        AssertReplaceableVisual(prefab, "TopProfileBar/ResourceCounterList/Resource_Money/Icon");
        AssertReplaceableVisual(prefab, "TopProfileBar/ResourceCounterList/Resource_Trust/Icon");
        AssertReplaceableVisual(prefab, "TopProfileBar/ResourceCounterList/Resource_Intel/Icon");
        AssertReplaceableVisual(prefab, "TopProfileBar/SettingsButton/SettingsGearIcon");
        AssertScalableSprite(prefab, "DeployCommandButton", DeployCommandButtonFramePath);
        AssertScalableSprite(prefab, "LeftNav/InboxButton", LeftNavRowPath);
        AssertScalableSprite(prefab, "LeftNav/StoreButton", LeftNavRowPath);
        AssertScalableSprite(prefab, "LeftNav/EventsButton", LeftNavRowPath);
        AssertScalableSprite(prefab, "LeftNav/RankingButton", LeftNavRowPath);
        AssertScalableSprite(prefab, "LeftNav/CommandFeedRouteButton", LeftNavRowPath);
        AssertScalableSprite(prefab, "LeftNav/InboxButton/DesignedUnavailableBadge", DesignedUnavailableBadgePath);
        AssertScalableSprite(prefab, "LeftNav/StoreButton/DesignedUnavailableBadge", DesignedUnavailableBadgePath);
        AssertScalableSprite(prefab, "LeftNav/EventsButton/DesignedUnavailableBadge", DesignedUnavailableBadgePath);
        AssertScalableSprite(prefab, "LeftNav/RankingButton/DesignedUnavailableBadge", DesignedUnavailableBadgePath);
        AssertScalableSprite(prefab, "LeftNav/CommandFeedRouteButton/DesignedUnavailableBadge", DesignedUnavailableBadgePath);
        AssertScalableSprite(prefab, "WideAspectOnlyRoot/CommandFeedPanelFrame", CommandFeedPanelFramePath);
        AssertButtonAnimatorControllerAsset();
        AssertAnimatedTabButton(prefab, "LeftNav/InboxButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "LeftNav/StoreButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "LeftNav/EventsButton", "Normal", false);
        AssertAnimatedTabButton(prefab, "LeftNav/RankingButton", "Normal", false);

        AssertPixelRect(prefab, "TopProfileBar", 0f, 0f, 1672f, 120f, 1672f, 941f);
        AssertPixelRect(prefab, "LeftNav", 7f, 126f, 350f, 667f, 1672f, 941f);
        AssertPixelRect(prefab, "ModeCardList", 398f, 148f, 1236f, 620f, 1672f, 941f);
        AssertPixelRect(prefab, "DeployCommandButton", 1132f, 780f, 506f, 102f, 1672f, 941f);
        AssertPixelRect(prefab, "ModeCardList/ModeCard_Saga", 0f, 0f, 374f, 588f, 1248f, 620f);
        AssertPixelRect(prefab, "ModeCardList/ModeCard_Operation", 396f, 0f, 386f, 588f, 1248f, 620f);
        AssertPixelRect(prefab, "ModeCardList/ModeCard_QuickCustom", 804f, 0f, 432f, 588f, 1248f, 620f);

        foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
        {
            Assert.AreNotEqual("Corner_TL", transform.name, "Use sliced panels or mockup frame sprites, not generated corner bars.");
            Assert.AreNotEqual("Corner_BR", transform.name, "Use sliced panels or mockup frame sprites, not generated corner bars.");
            Assert.AreNotEqual("Frame_Top", transform.name, "Use sliced panels or mockup frame sprites, not generated border bars.");
            Assert.AreNotEqual("Frame_Bottom", transform.name, "Use sliced panels or mockup frame sprites, not generated border bars.");
            Assert.AreNotEqual("Frame_Left", transform.name, "Use sliced panels or mockup frame sprites, not generated border bars.");
            Assert.AreNotEqual("Frame_Right", transform.name, "Use sliced panels or mockup frame sprites, not generated border bars.");
        }
    }

    [Test]
    public void MainMenu_RoutesOnlyImplementedDestinations()
    {
        GameObject prefab = LoadMainMenuPrefab();

        AssertRoute(prefab, "TopProfileBar/SettingsButton", WarlineCaptureRoute.Settings);
        AssertRoute(prefab, "LeftNav/InboxButton", WarlineCaptureRoute.Inbox);
        AssertRoute(prefab, "LeftNav/StoreButton", WarlineCaptureRoute.CommandExchange);
        AssertRoute(prefab, "LeftNav/EventsButton", WarlineCaptureRoute.Events);
        AssertRoute(prefab, "LeftNav/RankingButton", WarlineCaptureRoute.Ranking);
        AssertRoute(prefab, "LeftNav/CommandFeedRouteButton", WarlineCaptureRoute.CommandFeed);
        AssertRoute(prefab, "ModeCardList/ModeCard_Saga/Button", WarlineCaptureRoute.SagaMap);
        AssertRoute(prefab, "ModeCardList/ModeCard_QuickCustom/Button", WarlineCaptureRoute.QuickCustomSetup);
        AssertRoute(prefab, "ModeCardList/ModeCard_Operation/Button", WarlineCaptureRoute.OperationDashboard);
        AssertRoute(prefab, "DeployCommandButton", WarlineCaptureRoute.SagaMap);

        AssertNoActivePlaceholderModalButtons(prefab);
    }

    [Test]
    public void MainMenu_ModeCardsUseChildButtonAsOnlyClickTarget()
    {
        GameObject prefab = LoadMainMenuPrefab();

        AssertModeCardClickTarget(prefab, "ModeCardList/ModeCard_Saga");
        AssertModeCardClickTarget(prefab, "ModeCardList/ModeCard_Operation");
        AssertModeCardClickTarget(prefab, "ModeCardList/ModeCard_QuickCustom");
    }

    [Test]
    public void MainMenu_GeneratedArtIsAtlasReadyAndDecorativeGraphicsDoNotRaycast()
    {
        GameObject prefab = LoadMainMenuPrefab();

        AssertSpriteAtlas(IconsButtonsAtlasPath, "Assets/Game/Art/UI/Generated/MainMenu/Buttons", "Assets/Game/Art/UI/Generated/MainMenu/Icons", "Assets/Game/Art/UI/Generated/MainMenu/Portraits", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Icons", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Buttons");
        AssertSpriteAtlas(FramesChromeAtlasPath, "Assets/Game/Art/UI/Generated/MainMenu/Frames", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Frames", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Overlays", "Assets/Game/Art/UI/Generated/MainMenu/ImageGenFlat/FramesTrimmed");
        AssertSpriteAtlas(CardArtAtlasPath, "Assets/Game/Art/UI/Generated/MainMenu/Cards", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content", "Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Backgrounds");

        AssertAtlasLabel(CreditsIconPath, IconsButtonsAtlasLabel);
        AssertAtlasLabel(MaterialsIconPath, IconsButtonsAtlasLabel);
        AssertAtlasLabel(CommandAuthorityIconPath, IconsButtonsAtlasLabel);
        AssertAtlasLabel(CommanderProfilePortraitPath, IconsButtonsAtlasLabel);
        AssertAtlasLabel(SagaArtPath, CardArtAtlasLabel);
        AssertAtlasLabel(OperationArtPath, CardArtAtlasLabel);
        AssertAtlasLabel(QuickCustomArtPath, CardArtAtlasLabel);
        AssertAtlasLabel(TopResourceBarFramePath, FramesChromeAtlasLabel);
        AssertAtlasLabel(LeftNavRowPath, IconsButtonsAtlasLabel);
        AssertAtlasLabel(CardFrameMaskPath, FramesChromeAtlasLabel);
        AssertAtlasLabel(BackgroundTacticalMapPath, CardArtAtlasLabel);
        AssertAtlasLabel(OperationPressureMeterPath, CardArtAtlasLabel);
        AssertAtlasLabel(OperationRiskMeterPath, CardArtAtlasLabel);
        AssertNoAtlasLabel("Assets/Game/Art/UI/Generated/MainMenu/MainMenu_Landscape_Visual_Target.png");

        AssertUiSpriteImporter(LeftNavRowPath, true);
        AssertUiSpriteImporter(TopResourceBarFramePath, true);
        AssertUiSpriteImporter(CommanderProfilePortraitPath, true);
        AssertUiSpriteImporter(BackgroundTacticalMapPath, true);
        AssertUiSpriteImporter(SagaArtPath, true);
        AssertUiSpriteImporter(OperationArtPath, true);
        AssertUiSpriteImporter(QuickCustomArtPath, true);
        AssertTextureDimensions(CommanderProfilePortraitPath, 180, 150);
        AssertTextureDimensions(SagaArtPath, 334, 414);
        AssertTextureDimensions(OperationArtPath, 346, 286);
        AssertTextureDimensions(QuickCustomArtPath, 392, 414);

        foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
        {
            bool expectedRaycast = IsInteractiveRaycastGraphic(prefab, graphic);
            Assert.AreEqual(expectedRaycast, graphic.raycastTarget, $"{GetHierarchyPath(graphic.transform)} has an incorrect raycastTarget value.");
        }
    }

    [Test]
    public void MainMenu_UsesOxaniumFamilyForText()
    {
        string prefabText = File.ReadAllText(MainMenuPrefabPath);
        MatchCollection fontMatches = Regex.Matches(prefabText, @"m_fontAsset: \{fileID: 11400000, guid: ([a-f0-9]+), type: 2\}");

        Assert.Greater(fontMatches.Count, 0);
        foreach (Match fontMatch in fontMatches)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(fontMatch.Groups[1].Value);
            StringAssert.StartsWith(OxaniumFontFolder, fontPath);
            StringAssert.Contains("Oxanium", Path.GetFileNameWithoutExtension(fontPath));
        }
    }

    private static GameObject LoadMainMenuPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static void AssertChildren(GameObject prefab, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(prefab.transform.Find(path), $"Missing {path}");
    }

    private static void AssertText(GameObject prefab, string path, string expectedText)
    {
        TMP_Text text = prefab.transform.Find(path).GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expectedText, text.text, path);
    }

    private static void AssertResourceCounter(GameObject prefab, string path, string expectedValue)
    {
        Transform counterTransform = prefab.transform.Find(path);
        Assert.NotNull(counterTransform, path);
        Assert.NotNull(counterTransform.GetComponent<WarlineCaptureResourceCounterView>(), path);
        Assert.NotNull(counterTransform.Find("PlusButton").GetComponent<WarlineCapturePlaceholderModalSystem>(), path);
        AssertText(prefab, $"{path}/ValueText", expectedValue);
    }

    private static void AssertButtonAnimatorControllerAsset()
    {
        AnimatorOverrideController overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(UiButtonAnimatorControllerPath);
        Assert.NotNull(overrideController, "Reusable WarlineCapture button override controller is missing.");
        Assert.NotNull(overrideController.runtimeAnimatorController, "WarlineCapture button override controller must keep the Synty base controller graph.");
        Assert.AreEqual(UiButtonAnimatorBaseControllerPath, AssetDatabase.GetAssetPath(overrideController.runtimeAnimatorController));

        AnimatorController controller = overrideController.runtimeAnimatorController as AnimatorController;
        Assert.NotNull(controller, "Synty button base controller must be an AnimatorController.");

        string[] expectedStates = { "Normal", "Highlighted", "Pressed", "Selected", "Disabled" };
        string[] expectedClipPaths =
        {
            UiButtonNormalClipPath,
            UiButtonHighlightedClipPath,
            UiButtonPressedClipPath,
            UiButtonSelectedClipPath,
            UiButtonDisabledClipPath
        };

        for (int i = 0; i < expectedStates.Length; i++)
        {
            string expectedState = expectedStates[i];
            Assert.IsTrue(HasTriggerParameter(controller, expectedState), $"{UiButtonAnimatorControllerPath} must expose trigger '{expectedState}' for Unity Button animation transitions.");
            AnimatorState state = FindAnimatorState(controller, expectedState);
            Assert.NotNull(state, $"{UiButtonAnimatorControllerPath} must contain state '{expectedState}'.");
            Assert.AreEqual(expectedClipPaths[i], GetOverrideClipPath(overrideController, state.motion as AnimationClip), $"{expectedState} must use the WarlineCapture button animation clip.");
        }
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

    private static void AssertNoAtlasLabel(string assetPath)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        Assert.NotNull(asset, assetPath);
        string[] labels = AssetDatabase.GetLabels(asset);
        CollectionAssert.DoesNotContain(labels, IconsButtonsAtlasLabel, assetPath);
        CollectionAssert.DoesNotContain(labels, FramesChromeAtlasLabel, assetPath);
        CollectionAssert.DoesNotContain(labels, CardArtAtlasLabel, assetPath);
    }

    private static void AssertUiSpriteImporter(string path, bool androidOverridden)
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
        Assert.AreEqual(androidOverridden, android.overridden, path);
        Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format, path);
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

        if (expectedInitialState == "Selected")
            Assert.Greater(target.localScale.x, 1.03f, $"{path} should preview the selected tab state in the prefab.");
        else
            Assert.That(target.localScale.x, Is.EqualTo(1f).Within(0.001f), $"{path} should preview the normal tab state in the prefab.");
    }

    private static void AssertModeCardClickTarget(GameObject prefab, string cardPath)
    {
        Transform card = prefab.transform.Find(cardPath);
        Assert.NotNull(card, cardPath);
        Assert.IsNull(card.GetComponent<Button>(), $"{cardPath} root must stay decorative so it cannot intercept the route button.");

        Transform buttonTransform = card.Find("Button");
        Assert.NotNull(buttonTransform, $"{cardPath}/Button");
        Button button = buttonTransform.GetComponent<Button>();
        Image buttonImage = buttonTransform.GetComponent<Image>();
        Assert.NotNull(button, $"{cardPath}/Button");
        Assert.NotNull(buttonImage, $"{cardPath}/Button");
        Assert.AreSame(buttonImage, button.targetGraphic, $"{cardPath}/Button must use its own transparent Image as the raycast target.");
        Assert.IsTrue(buttonImage.raycastTarget, $"{cardPath}/Button raycast image must stay enabled after UI optimization.");

        RectTransform buttonRect = buttonTransform as RectTransform;
        Assert.NotNull(buttonRect, $"{cardPath}/Button");
        Assert.AreEqual(Vector2.zero, buttonRect.anchorMin, $"{cardPath}/Button must cover the full card.");
        Assert.AreEqual(Vector2.one, buttonRect.anchorMax, $"{cardPath}/Button must cover the full card.");
        Assert.AreEqual(Vector2.zero, buttonRect.offsetMin, $"{cardPath}/Button must cover the full card.");
        Assert.AreEqual(Vector2.zero, buttonRect.offsetMax, $"{cardPath}/Button must cover the full card.");
    }

    private static bool HasTriggerParameter(AnimatorController controller, string name)
    {
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name && parameters[i].type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private static AnimatorState FindAnimatorState(AnimatorController controller, string name)
    {
        if (controller.layers.Length == 0)
            return null;

        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == name)
                return states[i].state;
        }

        return null;
    }

    private static string GetOverrideClipPath(AnimatorOverrideController overrideController, AnimationClip sourceClip)
    {
        Assert.NotNull(sourceClip);

        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key == sourceClip)
                return AssetDatabase.GetAssetPath(overrides[i].Value);
        }

        return null;
    }

    private static void AssertModeCardArt(GameObject prefab, string path, string expectedSpritePath)
    {
        Transform artClip = prefab.transform.Find($"{path}/ArtClip");
        Assert.NotNull(artClip, path);
        Assert.IsTrue(artClip.gameObject.activeSelf, $"{path} must display the accepted Art/Atlas target slice.");
        Assert.NotNull(artClip.GetComponent<RectMask2D>(), $"{path} artwork must be clipped instead of stretched.");

        Image artImage = artClip.Find("ArtImage").GetComponent<Image>();
        Assert.NotNull(artImage, path);
        Assert.NotNull(artImage.sprite, $"{path} must serialize the accepted Art/Atlas mode-card art.");
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(artImage.sprite), path);
        Assert.IsFalse(artImage.preserveAspect, $"{path} art must be controlled by the parent crop fitter, not Image preserveAspect.");
        AspectRatioFitter artFitter = artImage.GetComponent<AspectRatioFitter>();
        Assert.NotNull(artFitter, $"{path} art must cover the crop mask without stretching.");
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, artFitter.aspectMode, $"{path} art must bleed past the mask like cover art.");
    }

    private static void AssertModeCardMask(GameObject prefab, string path)
    {
        RectMask2D mask = prefab.transform.Find(path).GetComponent<RectMask2D>();
        Assert.NotNull(mask, $"{path} must clip child layers to the card silhouette.");
    }

    private static void AssertModeCardContentMask(GameObject prefab, string path)
    {
        Transform contentClip = prefab.transform.Find(path);
        Assert.NotNull(contentClip, path);
        Image maskImage = contentClip.GetComponent<Image>();
        Assert.NotNull(maskImage, $"{path} must use the inner content mask sprite.");
        Assert.NotNull(maskImage.sprite, $"{path} must use the inner content mask sprite.");
        Assert.AreEqual(CardContentMaskPath, AssetDatabase.GetAssetPath(maskImage.sprite), path);
        Assert.AreEqual(Image.Type.Sliced, maskImage.type, $"{path} must scale as a sliced mask.");
        Mask mask = contentClip.GetComponent<Mask>();
        Assert.NotNull(mask, $"{path} must clip fill layers inside the frame line.");
        Assert.IsFalse(mask.showMaskGraphic, $"{path} must not render an extra visible panel.");
    }

    private static void AssertTintMaskClipsCardCorners(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.Less(texture.GetPixel(4, 4).a, 0.02f, $"{path} must be transparent outside the lower-left card chamfer.");
        Assert.Less(texture.GetPixel(4, texture.height - 5).a, 0.02f, $"{path} must be transparent outside the upper-left card chamfer.");
        Assert.Greater(texture.GetPixel(4, texture.height / 2).a, 0.5f, $"{path} must still tint the left-center interior of the card.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertCardContentMaskKeepsFillsInsideFrame(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.Less(texture.GetPixel(6, texture.height / 2).a, 0.02f, $"{path} must hide fill layers outside the left frame line.");
        Assert.Less(texture.GetPixel(texture.width - 7, texture.height / 2).a, 0.02f, $"{path} must hide fill layers outside the right frame line.");
        Assert.Less(texture.GetPixel(20, 20).a, 0.02f, $"{path} must hide fill layers outside the lower-left chamfer.");
        Assert.Less(texture.GetPixel(texture.width - 21, 20).a, 0.02f, $"{path} must hide fill layers outside the lower-right chamfer.");
        Assert.Greater(texture.GetPixel(30, texture.height / 2).a, 0.8f, $"{path} must keep the left interior fill visible.");
        Assert.Greater(texture.GetPixel(texture.width - 31, texture.height / 2).a, 0.8f, $"{path} must keep the right interior fill visible.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertCardFrameMaskUsesSinglePerimeter(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.Greater(MaxAlpha(texture, 50, 4, 180, 11), 0.28f, $"{path} must keep the mockup-derived top perimeter.");
        Assert.Greater(MaxAlpha(texture, 50, texture.height - 12, 180, texture.height - 5), 0.28f, $"{path} must keep the mockup-derived bottom perimeter.");
        Assert.Less(MaxAlpha(texture, 70, 18, 220, 34), 0.08f, $"{path} must not draw a second lower inner border.");
        Assert.Less(MaxAlpha(texture, 70, texture.height - 35, 220, texture.height - 19), 0.08f, $"{path} must not draw a second upper inner border.");
        Assert.Greater(MaxAlpha(texture, 4, 4, 64, 64), 0.28f, $"{path} must keep the mockup-derived lower corner cap.");
        Assert.Greater(MaxAlpha(texture, 4, texture.height - 65, 64, texture.height - 5), 0.28f, $"{path} must keep the mockup-derived upper corner cap.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static float MaxAlpha(Texture2D texture, int minX, int minY, int maxXExclusive, int maxYExclusive)
    {
        float maxAlpha = 0f;
        for (int y = Mathf.Clamp(minY, 0, texture.height); y < Mathf.Clamp(maxYExclusive, 0, texture.height); y++)
        {
            for (int x = Mathf.Clamp(minX, 0, texture.width); x < Mathf.Clamp(maxXExclusive, 0, texture.width); x++)
                maxAlpha = Mathf.Max(maxAlpha, texture.GetPixel(x, y).a);
        }

        return maxAlpha;
    }

    private static void AssertSliderTextureDimensions(string path, int expectedWidth, int expectedHeight)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.AreEqual(expectedWidth, texture.width, $"{path} width");
        Assert.AreEqual(expectedHeight, texture.height, $"{path} height");
        Assert.Greater(texture.GetPixel(texture.width / 2, texture.height / 2).a, 0.95f, $"{path} must have an opaque rounded interior.");
        Assert.Less(texture.GetPixel(0, 0).a, 0.1f, $"{path} must have transparent rounded corners.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertTextureDimensions(string path, int expectedWidth, int expectedHeight)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.AreEqual(expectedWidth, texture.width, $"{path} width");
        Assert.AreEqual(expectedHeight, texture.height, $"{path} height");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertFadedArtTexture(string path, int expectedWidth, int expectedHeight, int cleanArtOffset, int cleanArtTop, int cleanArtHeight)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        Assert.AreEqual(expectedWidth, texture.width, $"{path} width");
        Assert.AreEqual(expectedHeight, texture.height, $"{path} height");
        Assert.Less(texture.GetPixel(0, texture.height / 2).a, 0.05f, $"{path} must fade out at the left edge.");
        int cleanArtMiddleY = texture.height - cleanArtTop - cleanArtHeight / 2;
        Assert.Greater(texture.GetPixel(Mathf.Min(cleanArtOffset + 16, texture.width - 1), cleanArtMiddleY).a, 0.95f, $"{path} must become fully visible where the clean art crop starts.");
        Assert.Greater(texture.GetPixel(texture.width - 8, cleanArtMiddleY).a, 0.95f, $"{path} must remain visible at the right edge.");
        Assert.Greater(texture.GetPixel(Mathf.Min(cleanArtOffset + 40, texture.width - 1), texture.height - 8).a, 0.95f, $"{path} must cover the top of the card instead of leaving a black gap.");
        Assert.Greater(texture.GetPixel(Mathf.Min(cleanArtOffset + 40, texture.width - 1), 8).a, 0.95f, $"{path} must cover the bottom of the card instead of leaving a black gap.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertArtTextureDoesNotBakeRightCornerFrame(string path, Color accentColor)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
        float accentScore = MaxAccentScore(texture, accentColor, texture.width - 105, 0, texture.width, 36);
        Assert.Less(accentScore, 0.1f, $"{path} must not bake the mode-card accent frame into the bottom-right art corner; BorderOverlay draws the only border.");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static float MaxAccentScore(Texture2D texture, Color accentColor, int minX, int minY, int maxXExclusive, int maxYExclusive)
    {
        float maxScore = 0f;
        for (int y = Mathf.Clamp(minY, 0, texture.height); y < Mathf.Clamp(maxYExclusive, 0, texture.height); y++)
        {
            for (int x = Mathf.Clamp(minX, 0, texture.width); x < Mathf.Clamp(maxXExclusive, 0, texture.width); x++)
            {
                Color pixel = texture.GetPixel(x, y);
                float distance = Mathf.Abs(pixel.r - accentColor.r) + Mathf.Abs(pixel.g - accentColor.g) + Mathf.Abs(pixel.b - accentColor.b);
                maxScore = Mathf.Max(maxScore, pixel.a * Mathf.Clamp01(1f - distance / 0.55f));
            }
        }

        return maxScore;
    }

    private static void AssertTopBarBackgroundDoesNotBakeSeparators(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);

        AssertSeparatorColumnIsClean(texture, 748, path);
        AssertSeparatorColumnIsClean(texture, 1012, path);
        AssertSeparatorColumnIsClean(texture, 1252, path);
        AssertSeparatorColumnIsClean(texture, 1538, path);

        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void AssertSeparatorColumnIsClean(Texture2D texture, int centerX, string path)
    {
        float bakedSeparatorLuma = MaxLuma(texture, centerX - 3, 50, centerX + 5, 102);
        Assert.Less(bakedSeparatorLuma, 0.24f, $"{path} must not bake the header separator at x={centerX}; separators are runtime child objects.");
    }

    private static float MaxLuma(Texture2D texture, int minX, int minY, int maxXExclusive, int maxYExclusive)
    {
        float maxLuma = 0f;
        for (int y = Mathf.Clamp(minY, 0, texture.height); y < Mathf.Clamp(maxYExclusive, 0, texture.height); y++)
        {
            for (int x = Mathf.Clamp(minX, 0, texture.width); x < Mathf.Clamp(maxXExclusive, 0, texture.width); x++)
            {
                Color pixel = texture.GetPixel(x, y);
                maxLuma = Mathf.Max(maxLuma, pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f);
            }
        }

        return maxLuma;
    }

    private static void AssertSprite(GameObject prefab, string path, string expectedSpritePath)
    {
        Image image = prefab.transform.Find(path).GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.IsTrue(image.preserveAspect, $"{path} must preserve sprite aspect and must not stretch.");
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertStretchedSprite(GameObject prefab, string path, string expectedSpritePath)
    {
        Image image = prefab.transform.Find(path).GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.IsFalse(image.preserveAspect, $"{path} must fill the authored reference canvas.");
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertNullSprite(GameObject prefab, string path)
    {
        Image image = prefab.transform.Find(path).GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.IsNull(image.sprite, $"{path} must not serialize placeholder or fallback art while Art/Atlas owns the missing target slice.");
    }

    private static void AssertTintedSprite(GameObject prefab, string path, string expectedSpritePath, Color expectedColor, bool sliced)
    {
        Image image = prefab.transform.Find(path).GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        if (sliced)
            Assert.AreEqual(Image.Type.Sliced, image.type, $"{path} must use a sliced grayscale mask.");
        else
            Assert.AreEqual(Image.Type.Simple, image.type, $"{path} must use a simple grayscale mask.");
        AssertColor(expectedColor, image.color, path);
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertScalableSprite(GameObject prefab, string path, string expectedSpritePath)
    {
        Image image = prefab.transform.Find(path).GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual(Image.Type.Sliced, image.type, $"{path} must scale with sliced borders instead of stretching the full image.");
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertFixedRightProportionalY(GameObject prefab, string path, float right, float top, float width, float height, float referenceHeight)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(1f, 1f - (top + height) / referenceHeight), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2(1f, 1f - top / referenceHeight), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(new Vector2(-(right + width), 0f), rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(new Vector2(-right, 0f), rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertFixedLeftProportionalY(GameObject prefab, string path, float left, float top, float width, float height, float referenceHeight)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(0f, 1f - (top + height) / referenceHeight), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2(0f, 1f - top / referenceHeight), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(new Vector2(left, 0f), rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(new Vector2(left + width, 0f), rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertStretchWidthProportionalY(GameObject prefab, string path, float left, float right, float top, float height, float referenceHeight)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(0f, 1f - (top + height) / referenceHeight), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2(1f, 1f - top / referenceHeight), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(new Vector2(left, 0f), rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(new Vector2(-right, 0f), rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertPixelRect(GameObject prefab, string path, float x, float yFromTop, float width, float height, float referenceWidth, float referenceHeight)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(x / referenceWidth, 1f - (yFromTop + height) / referenceHeight), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2((x + width) / referenceWidth, 1f - yFromTop / referenceHeight), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(Vector2.zero, rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(Vector2.zero, rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertColor(Color expected, Color actual, string message)
    {
        Assert.AreEqual(expected.r, actual.r, 0.001f, $"{message} color.r");
        Assert.AreEqual(expected.g, actual.g, 0.001f, $"{message} color.g");
        Assert.AreEqual(expected.b, actual.b, 0.001f, $"{message} color.b");
        Assert.AreEqual(expected.a, actual.a, 0.001f, $"{message} color.a");
    }

    private static void AssertFixedWidthVerticalBand(GameObject prefab, string path, float left, float width, float anchorMinY, float anchorMaxY)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(0f, anchorMinY), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2(0f, anchorMaxY), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(new Vector2(left, 0f), rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(new Vector2(left + width, 0f), rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertStretchHorizontalVerticalBand(GameObject prefab, string path, float left, float right, float anchorMinY, float anchorMaxY)
    {
        RectTransform rect = prefab.transform.Find(path).GetComponent<RectTransform>();
        Assert.NotNull(rect, path);
        Assert.AreEqual(new Vector2(0f, anchorMinY), rect.anchorMin, $"{path} anchorMin");
        Assert.AreEqual(new Vector2(1f, anchorMaxY), rect.anchorMax, $"{path} anchorMax");
        Assert.AreEqual(new Vector2(left, 0f), rect.offsetMin, $"{path} offsetMin");
        Assert.AreEqual(new Vector2(-right, 0f), rect.offsetMax, $"{path} offsetMax");
    }

    private static void AssertReplaceableVisual(GameObject prefab, string path)
    {
        Transform transform = prefab.transform.Find(path);
        Assert.NotNull(transform, path);
        Assert.IsTrue(transform.gameObject.activeSelf, path);
        Graphic graphic = transform.GetComponent<Graphic>();
        Assert.NotNull(graphic, path);
        Assert.Greater(graphic.color.a, 0.5f, $"{path} must be a visible separate UI element, not baked into a parent background.");
        if (graphic is Image image && image.sprite != null)
            Assert.IsTrue(image.preserveAspect, $"{path} must preserve sprite aspect and must not stretch.");
    }

    private static void AssertTransparentButtonTarget(GameObject prefab, string path)
    {
        Transform transform = prefab.transform.Find(path);
        Assert.NotNull(transform, path);
        Button button = transform.GetComponent<Button>();
        Image image = transform.GetComponent<Image>();
        Assert.NotNull(button, path);
        Assert.NotNull(image, path);
        Assert.AreSame(image, button.targetGraphic, path);
        Assert.IsNull(image.sprite, $"{path} must not serialize old-shell button art.");
        Assert.That(image.color.a, Is.EqualTo(0f).Within(0.001f), $"{path} must use a transparent raycast target until Art/Atlas provides approved button chrome.");
    }

    private static void AssertRoute(GameObject prefab, string path, WarlineCaptureRoute expectedRoute)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Assert.NotNull(target.GetComponent<Button>(), path);
        ScreenRouteSystem routeButton = target.GetComponent<ScreenRouteSystem>();
        Assert.NotNull(routeButton, path);

        var serializedObject = new SerializedObject(routeButton);
        Assert.AreEqual((int)expectedRoute, serializedObject.FindProperty("route").enumValueIndex, path);
    }

    private static void AssertPlaceholder(GameObject prefab, string path)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Assert.NotNull(target.GetComponent<Button>(), path);
        Assert.NotNull(target.GetComponent<WarlineCapturePlaceholderModalSystem>(), path);
    }

    private static void AssertNoActivePlaceholderModalButtons(GameObject prefab)
    {
        foreach (WarlineCapturePlaceholderModalSystem placeholder in prefab.GetComponentsInChildren<WarlineCapturePlaceholderModalSystem>(true))
        {
            if (placeholder.gameObject.activeSelf)
                Assert.Fail($"{GetHierarchyPath(placeholder.transform)} is an active placeholder modal trigger. Visible Main Menu buttons must route to designed screens.");
        }
    }

    private static void AssertLegacyGameStart(GameObject prefab, string path)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Assert.NotNull(target.GetComponent<Button>(), path);
        Assert.NotNull(target.GetComponent<WarlineCaptureLegacyGameStartSystem>(), path);
        Assert.IsNull(target.GetComponent<ScreenRouteSystem>(), path);
    }
}
