using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public sealed class WarlineCaptureUiSettingsTests
{
    private const string SettingsPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Settings.prefab";
    private const string OxaniumFontFolder = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/";
    private const string SharedOuterFramePath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_OuterFrame_Overlay.png";
    private const string SettingsHeaderPanelPath = "Assets/Game/Art/UI/Generated/Settings/Frames/Settings_HeaderPanel_9Slice.png";
    private const string SettingsSectionPanelPath = "Assets/Game/Art/UI/Generated/Settings/Frames/Settings_SectionPanel_9Slice.png";
    private const string SettingsTabSelectedPath = "Assets/Game/Art/UI/Generated/Settings/Buttons/Settings_Tab_Selected_9Slice.png";
    private const string SettingsTabNormalPath = "Assets/Game/Art/UI/Generated/Settings/Buttons/Settings_Tab_Normal_9Slice.png";
    private const string SettingsButtonNormalPath = "Assets/Game/Art/UI/Generated/Settings/Buttons/Settings_Button_Normal_9Slice.png";
    private const string SettingsBackIconPath = "Assets/Game/Art/UI/Generated/Settings/Buttons/Settings_Back_Chevron.png";
    private const string SettingsDropdownPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Dropdown_9Slice.png";
    private const string SettingsDropdownChevronPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Dropdown_Chevron.png";
    private const string SettingsSliderTrackPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Slider_Track.png";
    private const string SettingsToggleTrackPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Toggle_Track.png";
    private const string SettingsToggleFillPath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Toggle_Fill.png";
    private const string SettingsToggleHandlePath = "Assets/Game/Art/UI/Generated/Settings/Controls/Settings_Toggle_Handle.png";
    private const string SettingsFramesAtlasPath = "Assets/Game/Art/UI/Generated/Settings/Atlases/Settings_UI_Frames.spriteatlas";
    private const string SettingsButtonsAtlasPath = "Assets/Game/Art/UI/Generated/Settings/Atlases/Settings_UI_Buttons.spriteatlas";
    private const string SettingsControlsAtlasPath = "Assets/Game/Art/UI/Generated/Settings/Atlases/Settings_UI_Controls.spriteatlas";
    private const string SplashFramesAtlasLabel = "Atlas_Splash_Frames";
    private const string SettingsFramesAtlasLabel = "Atlas_Settings_Frames";
    private const string SettingsButtonsAtlasLabel = "Atlas_Settings_Buttons";
    private const string SettingsControlsAtlasLabel = "Atlas_Settings_Controls";
    private const string OxaniumBoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
    private const string OxaniumLightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";
    private const float DropdownWidth = 328f;
    private const float DropdownHeight = 56f;
    private const float ToggleWidth = 126f;
    private const float ToggleHeight = 48f;

    [Test]
    public void SettingsPrefab_HasPhaseFourHierarchy()
    {
        GameObject prefab = LoadSettingsPrefab();

        AssertChildren(
            prefab,
            "HeaderBar",
            "HeaderBar/BackButton",
            "HeaderBar/TitleText",
            "TabStrip",
            "TabStrip/Tab_General",
            "TabStrip/Tab_Controls",
            "TabStrip/Tab_Notifications",
            "TabStrip/Tab_Accessibility",
            "SettingsScrollView",
            "SettingsScrollView/Viewport",
            "SettingsScrollView/Viewport/Content",
            "SettingsScrollView/Viewport/Content/AudioSection",
            "SettingsScrollView/Viewport/Content/GraphicsSection",
            "SettingsScrollView/Viewport/Content/ControlsSection",
            "SettingsScrollView/Viewport/Content/NotificationsSection",
            "SettingsScrollView/Viewport/Content/AccessibilitySection",
            "SettingsScrollView/Viewport/Content/LanguageSection",
            "FooterButtons",
            "FooterButtons/ResetButton",
            "FooterButtons/ApplyButton");
    }

    [Test]
    public void SettingsPrefab_WiresControllerControls()
    {
        GameObject prefab = LoadSettingsPrefab();
        SettingsScreenSystem controller = prefab.GetComponent<SettingsScreenSystem>();
        Assert.NotNull(controller);

        var serializedObject = new SerializedObject(controller);
        AssertReference(serializedObject, "masterVolumeRow");
        AssertReference(serializedObject, "musicVolumeRow");
        AssertReference(serializedObject, "sfxVolumeRow");
        AssertReference(serializedObject, "graphicsQualityControl");
        AssertReference(serializedObject, "frameRateControl");
        AssertReference(serializedObject, "cameraSensitivityRow");
        AssertReference(serializedObject, "threatWarningsRow");
        AssertReference(serializedObject, "highContrastRow");
        AssertReference(serializedObject, "largeTextRow");
        AssertReference(serializedObject, "colorblindModeDropdown");
        AssertReference(serializedObject, "languageDropdown");
        AssertReference(serializedObject, "resetButton");
        AssertReference(serializedObject, "applyButton");
        AssertReference(serializedObject, "accessibilityApplier");

        Assert.NotNull(prefab.transform.Find("HeaderBar/BackButton").GetComponent<ScreenRouteSystem>());
        Assert.NotNull(prefab.transform.Find("FooterButtons/ApplyButton").GetComponent<Button>());
        Assert.NotNull(prefab.transform.Find("FooterButtons/ResetButton").GetComponent<Button>());
    }

    [Test]
    public void SettingsPrefab_UsesLandscapeVisualLockArtAndFixedSections()
    {
        GameObject prefab = LoadSettingsPrefab();

        AssertImageSpritePath(prefab.transform, "OuterFrame", SharedOuterFramePath);
        AssertImageSpritePath(prefab.transform, "HeaderBar", SettingsHeaderPanelPath);
        AssertImageSpritePath(prefab.transform, "TabStrip/Tab_General", SettingsTabSelectedPath);
        AssertImageSpritePath(prefab.transform, "TabStrip/Tab_Controls", SettingsTabNormalPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AudioSection", SettingsSectionPanelPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/GraphicsSection", SettingsSectionPanelPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection", SettingsSectionPanelPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/LanguageSection", SettingsSectionPanelPath);

        AssertAtlasLabel(SharedOuterFramePath, SplashFramesAtlasLabel);
        AssertAtlasLabel(SettingsTabSelectedPath, SettingsButtonsAtlasLabel);
    }

    [Test]
    public void SettingsService_PersistsPlayerPrefsValues()
    {
        WarlineCaptureSettingsModel model = SettingsService.Defaults;
        model.Audio.MasterVolume = 33f;
        model.Audio.MusicVolume = 44f;
        model.Audio.SfxVolume = 55f;
        model.Graphics.Quality = WarlineCaptureGraphicsQuality.High;
        model.Graphics.FrameRateMode = WarlineCaptureFrameRateMode.Thirty;
        model.Controls.CameraSensitivity = 66f;
        model.Notifications.ThreatWarnings = false;
        model.Accessibility.HighContrastUi = true;
        model.Accessibility.LargeText = true;
        model.Accessibility.ColorblindMode = WarlineCaptureColorblindMode.Deuteranopia;
        model.Localization.Language = WarlineCaptureLanguage.German;

        SettingsService.Save(model);
        WarlineCaptureSettingsModel loaded = SettingsService.Load();

        Assert.AreEqual(33f, loaded.Audio.MasterVolume);
        Assert.AreEqual(44f, loaded.Audio.MusicVolume);
        Assert.AreEqual(55f, loaded.Audio.SfxVolume);
        Assert.AreEqual(WarlineCaptureGraphicsQuality.High, loaded.Graphics.Quality);
        Assert.AreEqual(WarlineCaptureFrameRateMode.Thirty, loaded.Graphics.FrameRateMode);
        Assert.AreEqual(66f, loaded.Controls.CameraSensitivity);
        Assert.IsFalse(loaded.Notifications.ThreatWarnings);
        Assert.IsTrue(loaded.Accessibility.HighContrastUi);
        Assert.IsTrue(loaded.Accessibility.LargeText);
        Assert.AreEqual(WarlineCaptureColorblindMode.Deuteranopia, loaded.Accessibility.ColorblindMode);
        Assert.AreEqual(WarlineCaptureLanguage.German, loaded.Localization.Language);

        SettingsService.ResetToDefaults();
    }

    [Test]
    public void SettingsPrefab_UsesConsistentDropdownAndToggleGeometry()
    {
        GameObject prefab = LoadSettingsPrefab();

        AssertFixedControlRect(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/ColorblindModeDropdownRow/Dropdown", DropdownWidth, DropdownHeight);
        AssertFixedControlRect(prefab.transform, "SettingsScrollView/Viewport/Content/LanguageSection/LanguageDropdownRow/Dropdown", DropdownWidth, DropdownHeight);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/ColorblindModeDropdownRow/Dropdown", SettingsDropdownPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/LanguageSection/LanguageDropdownRow/Dropdown", SettingsDropdownPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/ColorblindModeDropdownRow/Dropdown/Arrow", SettingsDropdownChevronPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/LanguageSection/LanguageDropdownRow/Dropdown/Arrow", SettingsDropdownChevronPath);

        AssertFixedControlRect(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/HighContrastRow/Toggle", ToggleWidth, ToggleHeight);
        AssertFixedControlRect(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/LargeTextRow/Toggle", ToggleWidth, ToggleHeight);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/HighContrastRow/Toggle", SettingsToggleTrackPath);
        AssertImageSpritePath(prefab.transform, "SettingsScrollView/Viewport/Content/AccessibilitySection/LargeTextRow/Toggle", SettingsToggleTrackPath);
    }

    [Test]
    public void SettingsPrefab_GeneratedArtIsAtlasReadyAndDecorativeGraphicsDoNotRaycast()
    {
        GameObject prefab = LoadSettingsPrefab();

        AssertSpriteAtlas(SettingsFramesAtlasPath, "Assets/Game/Art/UI/Generated/Settings/Frames");
        AssertSpriteAtlas(SettingsButtonsAtlasPath, "Assets/Game/Art/UI/Generated/Settings/Buttons");
        AssertSpriteAtlas(SettingsControlsAtlasPath, "Assets/Game/Art/UI/Generated/Settings/Controls");

        AssertAtlasLabel(SettingsHeaderPanelPath, SettingsFramesAtlasLabel);
        AssertAtlasLabel(SettingsSectionPanelPath, SettingsFramesAtlasLabel);
        AssertAtlasLabel(SettingsTabSelectedPath, SettingsButtonsAtlasLabel);
        AssertAtlasLabel(SettingsButtonNormalPath, SettingsButtonsAtlasLabel);
        AssertAtlasLabel(SettingsBackIconPath, SettingsButtonsAtlasLabel);
        AssertAtlasLabel(SettingsDropdownPath, SettingsControlsAtlasLabel);
        AssertAtlasLabel(SettingsDropdownChevronPath, SettingsControlsAtlasLabel);
        AssertAtlasLabel(SettingsSliderTrackPath, SettingsControlsAtlasLabel);
        AssertAtlasLabel(SettingsToggleTrackPath, SettingsControlsAtlasLabel);
        AssertAtlasLabel(SettingsToggleFillPath, SettingsControlsAtlasLabel);
        AssertAtlasLabel(SettingsToggleHandlePath, SettingsControlsAtlasLabel);

        AssertUiSpriteImporter(SettingsHeaderPanelPath);
        AssertUiSpriteImporter(SettingsDropdownPath);
        AssertUiSpriteImporter(SettingsDropdownChevronPath);
        AssertUiSpriteImporter(SettingsToggleTrackPath);

        foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
        {
            bool expectedRaycast = IsInteractiveRaycastGraphic(prefab, graphic);
            Assert.AreEqual(expectedRaycast, graphic.raycastTarget, $"{GetHierarchyPath(graphic.transform)} has an incorrect raycastTarget value.");
        }

        foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            Assert.IsFalse(image.sprite == null && Mathf.Approximately(image.color.a, 0f), $"{GetHierarchyPath(image.transform)} is a transparent placeholder Image and should be removed.");
    }

    [Test]
    public void SettingsAccessibilityApplier_UpdatesStandaloneScaleAndBackgroundContrast()
    {
        GameObject prefab = LoadSettingsPrefab();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            WarlineCaptureUiAccessibilityApplier accessibilityApplier = instance.GetComponent<WarlineCaptureUiAccessibilityApplier>();
            Assert.NotNull(accessibilityApplier);
            Transform content = instance.transform.Find("SettingsScrollView/Viewport/Content");
            Assert.NotNull(content);
            Image background = instance.GetComponent<Image>();
            Assert.NotNull(background);

            WarlineCaptureSettingsModel model = SettingsService.Defaults;
            model.Accessibility.LargeText = true;
            model.Accessibility.HighContrastUi = true;
            accessibilityApplier.Apply(model);

            Assert.AreEqual(1.08f, content.localScale.x, 0.001f);
            Assert.AreEqual(1.08f, content.localScale.y, 0.001f);
            Assert.AreEqual(Color.black, background.color);

            model.Accessibility.LargeText = false;
            model.Accessibility.HighContrastUi = false;
            accessibilityApplier.Apply(model);

            Assert.AreEqual(1f, content.localScale.x, 0.001f);
            Assert.AreEqual(1f, content.localScale.y, 0.001f);
            Assert.AreEqual(new Color(0.004f, 0.016f, 0.019f, 1f), background.color);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void SettingsPrefab_UsesOxaniumFamilyForText()
    {
        GameObject prefab = LoadSettingsPrefab();
        Transform boldTitle = prefab.transform.Find("HeaderBar/TitleText");
        Assert.NotNull(boldTitle);

        foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = GetHierarchyPath(text.transform);
            string expectedFontPath = text.transform == boldTitle ? OxaniumBoldFontPath : OxaniumLightFontPath;
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

    private static GameObject LoadSettingsPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
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

    private static void AssertImageSpritePath(Transform root, string path, string expectedSpritePath)
    {
        Transform target = string.IsNullOrEmpty(path) ? root : root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertFixedControlRect(Transform root, string path, float expectedWidth, float expectedHeight)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        RectTransform rectTransform = target as RectTransform;
        Assert.NotNull(rectTransform, path);
        Assert.AreEqual(rectTransform.anchorMin.x, rectTransform.anchorMax.x, 0.0001f, path);
        Assert.AreEqual(rectTransform.anchorMin.y, rectTransform.anchorMax.y, 0.0001f, path);
        Assert.AreEqual(expectedWidth, rectTransform.sizeDelta.x, 0.0001f, path);
        Assert.AreEqual(expectedHeight, rectTransform.sizeDelta.y, 0.0001f, path);
    }

    private static void AssertAtlasLabel(string assetPath, string expectedLabel)
    {
        Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        Assert.NotNull(asset, assetPath);
        CollectionAssert.Contains(AssetDatabase.GetLabels(asset), "WarlineCaptureUI", assetPath);
        CollectionAssert.Contains(AssetDatabase.GetLabels(asset), expectedLabel, assetPath);
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
