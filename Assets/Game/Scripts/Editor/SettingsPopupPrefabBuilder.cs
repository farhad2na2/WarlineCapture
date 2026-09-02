using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class SettingsPopupPrefabBuilder
    {
        private const string PopupsRoot = "Assets/Game/Prefabs/UI/Shell/Popups/";
        private const string SharedPopupPath = PopupsRoot + "SCN_SettingsPopup.prefab";
        private const string LegacyMenuPopupPath = PopupsRoot + "SCN02_MenuSettingsPopup.prefab";
        private const string LegacyMatchPopupPath = PopupsRoot + "SCN08_MatchSettingsPopup.prefab";
        private const string MainMenuContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MatchHudContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string PauseMenuPopupPath = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const float PopupRuntimeScale = 2.1f;
        private const float ChromeStroke = 3f;
        private static readonly Vector2 MenuReferenceResolution = new(4800f, 2160f);
        private static readonly Color ChromeBorder = new Color(0.30f, 0.33f, 0.34f, 1f);
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string LightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static TMP_FontAsset lightFont;
        private static V3UiArtCatalog art;
        private static V3UiTheme theme;
        private static Sprite panelBacking;
        private static Sprite panelFrame;
        private static Sprite headerFrame;
        private static Sprite squareDefault;
        private static Sprite squareHover;
        private static Sprite squarePressed;
        private static Sprite squareSelected;
        private static Sprite squareDisabled;
        private static Sprite deployDefault;
        private static Sprite deployHover;
        private static Sprite deployPressed;
        private static Sprite deploySelected;
        private static Sprite deployDisabled;
        private static Sprite navDefault;
        private static Sprite navSelected;
        private static Sprite resourceChip;
        private static Sprite settingsIcon;
        private static Sprite settingsAudioIcon;
        private static Sprite settingsVideoIcon;
        private static Sprite settingsAccessibilityIcon;
        private static Sprite resetIcon;
        private static readonly string[] GraphicsQualityLabels = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
        private static readonly string[] FrameRateLabels = { "30 FPS", "60 FPS", "120 FPS" };
        private static readonly string[] AssistanceLevelLabels = { "FULL", "HINTS", "MINIMAL", "OFF" };
        private static readonly string[] NarrationModeLabels = { "OFF", "CRITICAL", "IMPORTANT", "ALL" };
        private static readonly string[] ColorblindModeLabels = { "OFF", "PRO", "DEU", "TRI" };
        private static readonly string[] LanguageLabels = { "EN", "DE", "FR", "ES" };

        [MenuItem("Game/UI/Rebuild Settings Popups")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            GameObject sharedPrefab = BuildPopup(SharedPopupPath, "SCN_SettingsPopup");
            DeleteLegacyPopup(LegacyMenuPopupPath);
            DeleteLegacyPopup(LegacyMatchPopupPath);
            WireSettingsButtons(MainMenuContentPath);
            WireSettingsButtons(MatchHudContentPath);
            WireMenuScene(sharedPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateV3Prefab();
            Debug.Log("[SettingsPopupPrefabBuilder] result=Passed v3=True shared settings popup rebuilt and shell bindings updated.");
        }

        [MenuItem("Game/UI/V3/Validate Settings Popup")]
        public static void ValidateV3Prefab()
        {
            V3UiFoundationBuilder.Validate();
            V3UiArtCatalog catalog = V3UiFoundationBuilder.RequireCatalog();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPopupPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing settings prefab: {SharedPopupPath}");

            V3SettingsTabView tabView = prefab.GetComponentInChildren<V3SettingsTabView>(true);
            if (tabView == null || tabView.TabButtons == null || tabView.TabButtons.Length != 4 ||
                tabView.Pages == null || tabView.Pages.Length != 4)
                throw new MissingComponentException("Settings V3 prefab must contain four vertical tabs and four content pages.");

            Transform tabRail = FindDeepChild(prefab.transform, "SettingsTabRail");
            Transform pageFrame = FindDeepChild(prefab.transform, "ActivePageFrame");
            if (tabRail is not RectTransform tabRailRect || pageFrame is not RectTransform pageFrameRect ||
                tabRailRect.rect.width < 380f || pageFrameRect.rect.width < 1000f ||
                tabRailRect.anchoredPosition.x >= pageFrameRect.anchoredPosition.x)
                throw new System.InvalidOperationException("Settings V3 layout must use a large left tab rail and a larger right content page.");

            string[] expectedTabs = { "AUDIOTab", "GAMEPLAYTab", "VIDEOTab", "ACCESSIBILITYTab" };
            string[] expectedPages = { "AudioPage", "GameplayPage", "VideoPage", "AccessibilityPage" };
            int activePages = 0;
            for (int i = 0; i < expectedTabs.Length; i++)
            {
                Transform tab = FindDeepChild(prefab.transform, expectedTabs[i]);
                Transform page = FindDeepChild(prefab.transform, expectedPages[i]);
                if (tab is not RectTransform tabRect || tabRect.rect.height < 130f || page == null)
                    throw new MissingReferenceException($"Settings V3 is missing target-sized tab/page pair {expectedTabs[i]} / {expectedPages[i]}.");
                if (page.gameObject.activeSelf)
                    activePages++;
            }

            if (activePages != 1 || !FindDeepChild(prefab.transform, "AudioPage").gameObject.activeSelf)
                throw new System.InvalidOperationException("Settings V3 prefab must author exactly one visible page with Audio selected by default.");

            SettingsPopupResponsiveScaleView responsiveScale = prefab.GetComponentInChildren<SettingsPopupResponsiveScaleView>(true);
            if (responsiveScale == null ||
                Mathf.Abs(responsiveScale.TargetCanvasHeight - 0.84f) > 0.001f ||
                Mathf.Abs(responsiveScale.MaximumCanvasWidth - 0.76f) > 0.001f)
                throw new MissingComponentException("Settings V3 must use the dual-aspect responsive popup scale contract.");

            if (FindDeepChild(prefab.transform, "AudioSection") != null ||
                FindDeepChild(prefab.transform, "ControlSection") != null ||
                FindDeepChild(prefab.transform, "DisplaySection") != null ||
                FindDeepChild(prefab.transform, "AccessibilitySection") != null)
                throw new System.InvalidOperationException("Legacy four-panel Settings structure is forbidden in V3.");

            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
            var uniqueSpritePaths = new HashSet<string>(System.StringComparer.Ordinal);
            bool hasPanel = false;
            bool hasButton = false;
            bool hasFocus = false;
            foreach (Image image in images)
            {
                if (image.sprite == null)
                    continue;

                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                uniqueSpritePaths.Add(spritePath);
                bool allowed = spritePath.StartsWith(V3UiFoundationBuilder.SharedRoot + "/", System.StringComparison.Ordinal) ||
                               string.Equals(spritePath, V3UiFoundationBuilder.SettingsIconPath, System.StringComparison.Ordinal);
                if (!allowed)
                    throw new System.InvalidOperationException($"Settings V3 prefab still references legacy shared art: {spritePath}");
                if (image.type == Image.Type.Sliced &&
                    !Mathf.Approximately(image.pixelsPerUnitMultiplier, 2f))
                {
                    throw new System.InvalidOperationException(
                        $"Settings V3 sliced image {image.name} must use the common PPU multiplier 2.");
                }

                hasPanel |= image.sprite == catalog.Panel;
                hasButton |= image.sprite == catalog.Button;
                hasFocus |= image.sprite == catalog.FocusOverlay;
            }

            if (!hasButton || !hasFocus)
                throw new MissingReferenceException("Settings V3 prefab must retain the shared button and focus sprite references.");

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 10)
                throw new MissingComponentException("Settings V3 must use shared procedural gradients for tabs, toggles, and footer actions.");
            if (FindDeepChild(prefab.transform, "Frame") != null ||
                FindDeepChild(prefab.transform, "TabRailFrame") != null ||
                FindDeepChild(prefab.transform, "PageFrame") != null ||
                FindDeepChild(prefab.transform, "ActionFrame") != null)
                throw new System.InvalidOperationException("Settings V3 contains a duplicate frame layer; each panel/control may render shared chrome only once.");
            ValidateFixedGradientBorder(prefab.transform, "UnifiedModalFrame", ChromeStroke);
            ValidateFixedGradientBorder(prefab.transform, "TabRailFill", ChromeStroke);
            ValidateFixedGradientBorder(prefab.transform, "PageFill", ChromeStroke);
            ValidateFixedDivider(prefab.transform, "HeaderDivider", ChromeStroke);
            ValidateFixedDivider(prefab.transform, "FooterDivider", ChromeStroke);

            if (FindDeepChild(prefab.transform, "MiddleFrame") != null ||
                FindDeepChild(prefab.transform, "MiddleDivider") != null)
                throw new System.InvalidOperationException("Settings V3 must not draw a wrapper border through the separate tab rail and content panel.");

            float tabRailRight = tabRailRect.anchoredPosition.x + tabRailRect.rect.width * 0.5f;
            float pageFrameLeft = pageFrameRect.anchoredPosition.x - pageFrameRect.rect.width * 0.5f;
            if (pageFrameLeft - tabRailRight < 8f)
                throw new System.InvalidOperationException("Settings V3 tab rail and content panel require a clean non-overlapping gap.");

            foreach (Button button in buttons)
            {
                if (button.transition != Selectable.Transition.ColorTint)
                    throw new System.InvalidOperationException($"{button.name} must use ColorTint with the single shared button sprite.");
                if (button.GetComponent<V3UiSelectableFocusView>() == null)
                    throw new MissingComponentException($"{button.name} is missing V3UiSelectableFocusView.");
                if (button.image != null && !Mathf.Approximately(button.image.pixelsPerUnitMultiplier, 2f))
                    throw new System.InvalidOperationException($"{button.name} must use the common V3 border scale.");
            }

            Debug.Log($"[SettingsPopupPrefabBuilder] validation=Passed layout=vertical-tabs activePages={activePages} gradients={gradients.Length} images={images.Length} buttons={buttons.Length} uniqueSprites={uniqueSpritePaths.Count}");
        }

        private static void ValidateFixedGradientBorder(Transform root, string name, float expectedWidth)
        {
            Transform transform = FindDeepChild(root, name);
            V3GradientGraphic graphic = transform != null ? transform.GetComponent<V3GradientGraphic>() : null;
            if (graphic == null)
                throw new MissingComponentException($"Settings V3 is missing the fixed {name} border.");

            SerializedProperty width = new SerializedObject(graphic).FindProperty("borderWidth");
            if (width == null || !Mathf.Approximately(width.floatValue, expectedWidth))
                throw new System.InvalidOperationException($"{name} must use the common {expectedWidth}px V3 stroke.");
        }

        private static void ValidateFixedDivider(Transform root, string name, float expectedWidth)
        {
            Transform transform = FindDeepChild(root, name);
            if (transform is not RectTransform rect || !Mathf.Approximately(rect.sizeDelta.y, expectedWidth))
                throw new System.InvalidOperationException($"{name} must use the common {expectedWidth}px V3 stroke.");
        }

        private static void ValidateFixedVerticalDivider(Transform root, string name, float expectedWidth)
        {
            Transform transform = FindDeepChild(root, name);
            if (transform is not RectTransform rect || !Mathf.Approximately(rect.sizeDelta.x, expectedWidth))
                throw new System.InvalidOperationException($"{name} must use the common {expectedWidth}px V3 stroke.");
        }

        [MenuItem("Game/UI/Repair Match Lifecycle Controls")]
        public static void RepairMatchLifecycleControls()
        {
            RepairMatchLifecycleControlsInternal();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MatchLifecycleControlsRepair] result=Passed");
        }

        [MenuItem("Game/UI/Capture Settings Popup QA")]
        public static void CaptureVisualQa()
        {
            CapturePopup(SharedPopupPath, SettingsPopupContext.Menu, "/private/tmp/warline-settings-popup-menu-16x9.png", 1920, 1080);
            CapturePopup(SharedPopupPath, SettingsPopupContext.Menu, "/private/tmp/warline-settings-popup-menu-20x9.png", 2400, 1080);
            CapturePopup(SharedPopupPath, SettingsPopupContext.Match, "/private/tmp/warline-settings-popup-match-16x9.png", 1920, 1080);
            CapturePopup(SharedPopupPath, SettingsPopupContext.Match, "/private/tmp/warline-settings-popup-match-20x9.png", 2400, 1080);
            Debug.Log("[SettingsPopupPrefabBuilder] Settings popup visual QA captures written to /private/tmp.");
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            lightFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LightFontPath);
            art = V3UiFoundationBuilder.RequireCatalog();
            theme = V3UiFoundationBuilder.RequireTheme();

            panelBacking = art.Panel;
            panelFrame = art.FocusOverlay;
            headerFrame = art.Panel;
            squareDefault = art.Button;
            squareHover = art.Button;
            squarePressed = art.Button;
            squareSelected = art.Button;
            squareDisabled = art.Button;
            deployDefault = art.Button;
            deployHover = art.Button;
            deployPressed = art.Button;
            deploySelected = art.Button;
            deployDisabled = art.Button;
            navDefault = art.Button;
            navSelected = art.Button;
            resourceChip = art.Panel;
            settingsIcon = art.SettingsIcon;
            settingsAudioIcon = art.SettingsAudioIcon;
            settingsVideoIcon = art.SettingsVideoIcon;
            settingsAccessibilityIcon = art.SettingsAccessibilityIcon;
            resetIcon = art.ResetIcon;
        }

        private static GameObject BuildPopup(string path, string prefabName)
        {
            GameObject root = CreateRect(prefabName, null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            root.AddComponent<CanvasGroup>();
            root.AddComponent<UIPopupMotionView>();

            Image dim = CreateImage("InputBlocker", root.transform, null, new Color(0f, 0f, 0f, 0.76f), true);
            Stretch(dim.rectTransform);

            RectTransform panel = CreateRect("SettingsRoot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1540f, 970f), Vector2.zero);
            panel.localScale = Vector3.one * PopupRuntimeScale;
            SettingsPopupResponsiveScaleView responsiveScale = panel.gameObject.AddComponent<SettingsPopupResponsiveScaleView>();
            responsiveScale.Configure(0.84f, 0.76f);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = null;
            panelImage.color = Color.clear;
            panelImage.raycastTarget = true;
            V3GradientGraphic panelFill = CreateGradient(
                "PanelFill",
                panel,
                new Color(0.04f, 0.05f, 0.055f, 1f),
                new Color(0.012f, 0.017f, 0.02f, 1f),
                Color.clear,
                0f);
            Stretch(panelFill.rectTransform);

            RectTransform header = CreateRect("Header", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 112f), new Vector2(0f, -56f));
            V3GradientGraphic headerImage = CreateGradient(
                "HeaderFill",
                header,
                new Color(0.055f, 0.066f, 0.074f, 1f),
                new Color(0.018f, 0.024f, 0.028f, 1f),
                Color.clear,
                0f);
            Stretch(headerImage.rectTransform);
            CreateEdge("HeaderDivider", header, ChromeBorder, ChromeStroke, Edge.Bottom);

            Image gear = CreateImage("SettingsIcon", header, settingsIcon, theme.TextPrimary, false);
            SetRect(gear.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 80f), new Vector2(70f, 0f));

            TMP_Text title = CreateText("TitleText", header, "COMMAND SETTINGS", 71f, boldFont, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-220f, 0f), new Vector2(12f, 0f));

            Button closeButton = CreateButton("CloseButton", header, squareDefault, squareHover, squarePressed, squareSelected, squareDisabled);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(74f, 74f), new Vector2(-70f, 0f));
            V3GradientGraphic closeFill = CreateGradient(
                "CloseFill",
                closeButton.transform,
                new Color(0.09f, 0.105f, 0.11f, 1f),
                new Color(0.018f, 0.022f, 0.024f, 1f),
                ChromeBorder,
                ChromeStroke);
            Stretch(closeFill.rectTransform);
            UseGradientButtonVisual(closeButton, closeFill);
            TMP_Text closeLabel = CreateText("CloseLabel", closeButton.transform, "X", 42f, boldFont, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform);

            SettingsPanelView panelView = panel.gameObject.AddComponent<SettingsPanelView>();
            RectTransform content = CreateRect("Content", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1450f, 700f), new Vector2(0f, -466f));

            RectTransform tabRail = CreateRect("SettingsTabRail", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(406f, 694f), new Vector2(-553f, 0f));
            V3GradientGraphic tabRailBacking = CreateGradient(
                "TabRailFill",
                tabRail,
                new Color(0.045f, 0.06f, 0.065f, 1f),
                new Color(0.014f, 0.025f, 0.028f, 1f),
                ChromeBorder,
                ChromeStroke);
            Stretch(tabRailBacking.rectTransform);

            RectTransform pageFrame = CreateRect("ActivePageFrame", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1086f, 694f), new Vector2(203f, 0f));
            V3GradientGraphic pageBacking = CreateGradient(
                "PageFill",
                pageFrame,
                new Color(0.055f, 0.075f, 0.085f, 1f),
                new Color(0.025f, 0.04f, 0.045f, 1f),
                ChromeBorder,
                ChromeStroke);
            Stretch(pageBacking.rectTransform);

            RectTransform pagesRoot = CreateRect("SettingsPages", pageFrame, Vector2.zero, Vector2.one, new Vector2(-34f, -28f), Vector2.zero);
            RectTransform audioPage = CreateSettingsPage("AudioPage", pagesRoot);
            RectTransform gameplayPage = CreateSettingsPage("GameplayPage", pagesRoot);
            RectTransform videoPage = CreateSettingsPage("VideoPage", pagesRoot);
            RectTransform accessibilityPage = CreateSettingsPage("AccessibilityPage", pagesRoot);
            gameplayPage.gameObject.SetActive(false);
            videoPage.gameObject.SetActive(false);
            accessibilityPage.gameObject.SetActive(false);

            Button[] tabButtons = new Button[4];
            V3GradientGraphic[] tabBackgrounds = new V3GradientGraphic[4];
            Image[] tabRails = new Image[4];
            TMP_Text[] tabLabels = new TMP_Text[4];
            Color[] tabAccents = { theme.Blue, theme.Green, theme.Cyan, theme.Violet };
            string[] tabNames = { "AUDIO", "GAMEPLAY", "VIDEO", "ACCESSIBILITY" };
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i] = CreateSettingsTab(
                    tabNames[i] + "Tab",
                    tabRail,
                    tabNames[i],
                    i,
                    tabAccents[i],
                    out tabBackgrounds[i],
                    out tabRails[i],
                    out tabLabels[i]);
                if (i == 0)
                    tabBackgrounds[i].ConfigureCorners(
                        new Color(0f, 0.24f, 0.42f, 1f),
                        new Color(0f, 0.23f, 0.40f, 1f),
                        new Color(0f, 0.11f, 0.18f, 1f),
                        new Color(0f, 0.12f, 0.20f, 1f),
                        new Color(0f, 0.63f, 0.96f, 1f),
                        ChromeStroke);
            }

            V3SettingsTabView tabView = content.gameObject.AddComponent<V3SettingsTabView>();
            SetObjectArray(tabView, "tabButtons", tabButtons);
            SetObjectArray(tabView, "pages", new GameObject[] { audioPage.gameObject, gameplayPage.gameObject, videoPage.gameObject, accessibilityPage.gameObject });
            SetObjectArray(tabView, "tabBackgrounds", tabBackgrounds);
            SetObjectArray(tabView, "selectionRails", tabRails);
            SetObjectArray(tabView, "tabLabels", tabLabels);
            SetColorArray(tabView, "accentColors", tabAccents);
            SetColor(tabView, "inactiveBackground", new Color(0.025f, 0.045f, 0.052f, 0.98f));
            SetColor(tabView, "inactiveText", theme.TextPrimary);
            SetColor(tabView, "inactiveBorder", ChromeBorder);
            SetInt(tabView, "defaultTab", 0);

            UISliderRowView master = CreateSliderRow("MasterVolumeRow", audioPage, "MASTER VOLUME", 28f, 82f);
            UISliderRowView music = CreateSliderRow("MusicVolumeRow", audioPage, "MUSIC VOLUME", 125f, 82f);
            UISliderRowView sfx = CreateSliderRow("SfxVolumeRow", audioPage, "SOUND VOLUME", 222f, 82f);
            UIToggleRowView musicToggle = CreateToggleRow("MusicEnabledRow", audioPage, "MUSIC", "Adjust in-game music volume.", 321f, 86f);
            UIToggleRowView soundToggle = CreateToggleRow("SoundEnabledRow", audioPage, "SOUND", "Adjust in-game sound effects volume.", 445f, 86f);
            UIToggleRowView voiceToggle = CreateToggleRow("VoiceEnabledRow", audioPage, "VOICE", "Adjust in-game voice volume.", 565f, 86f);

            UISliderRowView camera = CreateSliderRow("CameraSensitivityRow", gameplayPage, "CAMERA SENSITIVITY", 34f, 84f);
            UIToggleRowView threat = CreateToggleRow("ThreatWarningsRow", gameplayPage, "THREAT WARNINGS", "Show tactical warnings during missions.", 132f, 86f);
            UISegmentedControlView assistance = CreateSegmentRow("AssistanceLevelControl", gameplayPage, "ASSISTANT GUIDANCE", 232f, AssistanceLevelLabels, 94f);
            UISegmentedControlView narration = CreateSegmentRow("NarrationModeControl", gameplayPage, "NARRATION", 340f, NarrationModeLabels, 94f);
            UIToggleRowView takeover = CreateToggleRow("AssistantTakeoverRow", gameplayPage, "ASSISTANT TAKEOVER", "Allow assistant-guided bounded actions.", 466f, 86f);

            UISegmentedControlView quality = CreateSegmentRow("GraphicsQualityControl", videoPage, "GRAPHICS QUALITY", 86f, GraphicsQualityLabels, 118f);
            UISegmentedControlView frameRate = CreateSegmentRow("FrameRateControl", videoPage, "FRAME RATE", 246f, FrameRateLabels, 118f);

            UIToggleRowView contrast = CreateToggleRow("HighContrastRow", accessibilityPage, "HIGH CONTRAST UI", "Increase panel and text contrast.", 24f, 84f);
            UIToggleRowView largeText = CreateToggleRow("LargeTextRow", accessibilityPage, "LARGE TEXT", "Increase UI text scale for readability.", 116f, 84f);
            UIToggleRowView subtitles = CreateToggleRow("AssistantSubtitlesRow", accessibilityPage, "ASSISTANT SUBTITLES", "Show narration subtitles in the assistant panel.", 208f, 84f);
            UISegmentedControlView colorblind = CreateSegmentRow("ColorblindModeControl", accessibilityPage, "COLORBLIND MODE", 316f, ColorblindModeLabels, 96f);
            UISegmentedControlView language = CreateSegmentRow("LanguageControl", accessibilityPage, "LANGUAGE", 430f, LanguageLabels, 96f);

            SetObject(panelView, "masterVolumeRow", master);
            SetObject(panelView, "musicVolumeRow", music);
            SetObject(panelView, "sfxVolumeRow", sfx);
            SetObject(panelView, "musicEnabledRow", musicToggle);
            SetObject(panelView, "soundEnabledRow", soundToggle);
            SetObject(panelView, "voiceEnabledRow", voiceToggle);
            SetObject(panelView, "graphicsQualityControl", quality);
            SetObject(panelView, "frameRateControl", frameRate);
            SetObject(panelView, "cameraSensitivityRow", camera);
            SetObject(panelView, "threatWarningsRow", threat);
            SetObject(panelView, "highContrastRow", contrast);
            SetObject(panelView, "largeTextRow", largeText);
            SetObject(panelView, "assistanceLevelControl", assistance);
            SetObject(panelView, "narrationModeControl", narration);
            SetObject(panelView, "assistantTakeoverRow", takeover);
            SetObject(panelView, "assistantSubtitlesRow", subtitles);
            SetObject(panelView, "colorblindModeControl", colorblind);
            SetObject(panelView, "languageControl", language);

            RectTransform footer = CreateRect("Footer", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 152f), new Vector2(0f, 76f));
            V3GradientGraphic footerBacking = CreateGradient(
                "FooterFill",
                footer,
                new Color(0.055f, 0.06f, 0.06f, 1f),
                new Color(0.018f, 0.022f, 0.022f, 1f),
                Color.clear,
                0f);
            Stretch(footerBacking.rectTransform);
            CreateEdge("FooterDivider", footer, ChromeBorder, ChromeStroke, Edge.Top);
            Button reset = CreateButton("ResetButton", footer, deployDefault, deployHover, deployPressed, deploySelected, deployDisabled);
            SetRect(reset.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-34f, 120f), new Vector2(10f, 0f));
            SetButtonPalette(reset, theme.Amber, new Color(1f, 0.82f, 0.2f, 1f), new Color(0.9f, 0.58f, 0.02f, 1f));
            V3GradientGraphic resetFill = CreateGradient(
                "ActionFill",
                reset.transform,
                new Color(0.09f, 0.065f, 0.012f, 1f),
                new Color(0.014f, 0.015f, 0.012f, 1f),
                theme.Amber,
                ChromeStroke);
            Stretch(resetFill.rectTransform);
            resetFill.ConfigureCorners(
                new Color(0.12f, 0.085f, 0.012f, 1f),
                new Color(0.055f, 0.04f, 0.008f, 1f),
                new Color(0.006f, 0.008f, 0.006f, 1f),
                new Color(0.035f, 0.022f, 0.004f, 1f),
                theme.Amber,
                ChromeStroke);
            UseGradientButtonVisual(reset, resetFill);
            TMP_Text resetText = CreateText("Label", reset.transform, "RESET", 44f, boldFont, TextAlignmentOptions.Center);
            Stretch(resetText.rectTransform);
            resetText.rectTransform.sizeDelta = new Vector2(-110f, 0f);
            resetText.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            resetText.color = theme.Amber;
            Image resetGlyph = V3UiPrefabFactory.CreateImage("ResetGlyph", reset.transform, resetIcon, theme.Amber, false, false);
            SetRect(resetGlyph.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 70f), new Vector2(-140f, 0f));

            Button apply = CreateButton("ApplyButton", footer, deployDefault, deployHover, deployPressed, deploySelected, deployDisabled);
            SetRect(apply.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 120f), new Vector2(-10f, 0f));
            SetButtonPalette(apply, theme.Green, new Color(0.34f, 0.9f, 0.42f, 1f), new Color(0.12f, 0.56f, 0.22f, 1f));
            V3GradientGraphic applyFill = CreateGradient(
                "ActionFill",
                apply.transform,
                new Color(0.055f, 0.29f, 0.10f, 1f),
                new Color(0.012f, 0.14f, 0.04f, 1f),
                theme.Green,
                ChromeStroke);
            Stretch(applyFill.rectTransform);
            applyFill.ConfigureCorners(
                new Color(0.07f, 0.29f, 0.105f, 1f),
                new Color(0.07f, 0.29f, 0.105f, 1f),
                new Color(0.024f, 0.18f, 0.063f, 1f),
                new Color(0.024f, 0.18f, 0.063f, 1f),
                theme.Green,
                ChromeStroke);
            UseGradientButtonVisual(apply, applyFill);
            TMP_Text applyText = CreateText("Label", apply.transform, "APPLY", 44f, boldFont, TextAlignmentOptions.Center);
            Stretch(applyText.rectTransform);
            applyText.rectTransform.sizeDelta = new Vector2(-110f, 0f);
            applyText.rectTransform.anchoredPosition = new Vector2(42f, 0f);
            CreateApplyGlyph(apply.transform, theme.TextPrimary, new Vector2(-120f, 0f));

            V3GradientGraphic modalFrame = CreateGradient(
                "UnifiedModalFrame",
                panel,
                Color.clear,
                Color.clear,
                ChromeBorder,
                ChromeStroke);
            Stretch(modalFrame.rectTransform);
            modalFrame.transform.SetAsLastSibling();

            SettingsPopupView popupView = root.AddComponent<SettingsPopupView>();
            SetEnum(popupView, "context", (int)SettingsPopupContext.Menu);
            SetObject(popupView, "titleText", title);
            SetObject(popupView, "settingsPanel", panelView);
            SetObject(popupView, "closeButton", closeButton);
            SetObject(popupView, "resetButton", reset);
            SetObject(popupView, "applyButton", apply);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform CreateSettingsPage(string name, Transform parent)
        {
            RectTransform page = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(page);
            return page;
        }

        private static Button CreateSettingsTab(
            string name,
            Transform parent,
            string labelText,
            int index,
            Color accent,
            out V3GradientGraphic background,
            out Image selectionRail,
            out TMP_Text label)
        {
            Button button = CreateButton(name, parent, navDefault, navSelected, navSelected, navSelected, squareDisabled);
            SetRect(
                button.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(390f, 163f),
                new Vector2(0f, -86f - index * 172f));
            SetButtonPalette(button, theme.LinePrimary, theme.TextPrimary, theme.Cyan);

            background = CreateGradient(
                "TabFill",
                button.transform,
                Color.Lerp(theme.Surface, Color.white, 0.07f),
                Color.Lerp(theme.Surface, Color.black, 0.2f),
                ChromeBorder,
                ChromeStroke);
            Stretch(background.rectTransform);
            UseGradientButtonVisual(button, background);

            selectionRail = CreateSolid("SelectionRail", button.transform, accent, new Vector2(10f, 124f), new Vector2(-180f, 0f));
            selectionRail.gameObject.SetActive(index == 0);

            RectTransform iconRoot = CreateRect("CategoryIcon", button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 78f), new Vector2(80f, 0f));
            CreateTabIcon(iconRoot, index, accent);

            label = CreateText("Label", button.transform, labelText, 36f, boldFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(-150f, 0f), new Vector2(70f, 0f));
            label.color = theme.TextPrimary;

            Transform focus = button.transform.Find("FocusOverlay");
            focus?.SetAsLastSibling();
            return button;
        }

        private static void CreateApplyGlyph(Transform parent, Color color, Vector2 position)
        {
            RectTransform glyph = CreateRect("ApplyGlyph", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 62f), position);
            CreateSolid("CheckShort", glyph, color, new Vector2(32f, 9f), new Vector2(-12f, -10f), -45f);
            CreateSolid("CheckLong", glyph, color, new Vector2(52f, 9f), new Vector2(12f, -2f), 45f);
        }

        private static void CreateTabIcon(RectTransform parent, int index, Color accent)
        {
            Sprite sprite = index switch
            {
                0 => settingsAudioIcon,
                1 => art.AttackIcon,
                2 => settingsVideoIcon,
                _ => settingsAccessibilityIcon
            };
            Image icon = V3UiPrefabFactory.CreateImage("Icon", parent, sprite, accent, false, false);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(74f, 74f), Vector2.zero);
        }

        private static Image CreateSolid(
            string name,
            Transform parent,
            Color color,
            Vector2 size,
            Vector2 position,
            float rotation = 0f)
        {
            Image image = V3UiPrefabFactory.CreateImage(name, parent, null, color, false, false);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return image;
        }

        private enum Edge
        {
            Top,
            Bottom
        }

        private static V3GradientGraphic CreateGradient(
            string name,
            Transform parent,
            Color top,
            Color bottom,
            Color border,
            float borderWidth)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(top, bottom, border, borderWidth);
            return graphic;
        }

        private static void CreateEdge(string name, Transform parent, Color color, float width, Edge edge)
        {
            Image line = V3UiPrefabFactory.CreateImage(name, parent, null, color, false, false);
            line.rectTransform.anchorMin = edge == Edge.Top ? new Vector2(0f, 1f) : Vector2.zero;
            line.rectTransform.anchorMax = edge == Edge.Top ? Vector2.one : new Vector2(1f, 0f);
            line.rectTransform.pivot = edge == Edge.Top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            line.rectTransform.sizeDelta = new Vector2(0f, width);
            line.rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void StretchInset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void UseGradientButtonVisual(Button button, V3GradientGraphic visual)
        {
            if (button.image != null)
                button.image.enabled = false;
            button.targetGraphic = visual;
            visual.color = Color.white;
            visual.raycastTarget = true;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.9f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = theme.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void SetButtonPalette(Button button, Color normal, Color highlighted, Color pressed)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.disabledColor = theme.Disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (button.targetGraphic != null)
                button.targetGraphic.color = normal;
        }

        private static RectTransform CreateSection(string name, Transform parent, string title, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform section = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPosition);
            Image backing = section.gameObject.AddComponent<Image>();
            backing.sprite = panelBacking;
            ApplySliced(backing, 2f);
            backing.color = new Color(1f, 1f, 1f, 0.82f);
            backing.raycastTarget = false;

            Image frame = CreateImage("SectionFrame", section, panelFrame, new Color(theme.LinePrimary.r, theme.LinePrimary.g, theme.LinePrimary.b, 0.72f), false);
            Stretch(frame.rectTransform);
            ApplySliced(frame, 2f);

            Image rail = CreateImage("TitleRail", section, resourceChip, new Color(theme.Cyan.r, theme.Cyan.g, theme.Cyan.b, 0.58f), false);
            SetRect(rail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-40f, 30f), new Vector2(0f, -19f));
            ApplySliced(rail, 2f);

            TMP_Text sectionTitle = CreateText("SectionTitle", section, title, 22f, boldFont, TextAlignmentOptions.Left);
            SetRect(sectionTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-52f, 28f), new Vector2(28f, -20f));
            sectionTitle.color = theme.Amber;
            return section;
        }

        private static UISliderRowView CreateSliderRow(string name, Transform parent, string labelText, float topOffset, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UISliderRowView view = row.gameObject.AddComponent<UISliderRowView>();
            TMP_Text label = CreateText("Label", row, labelText, 30f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(270f, 42f), new Vector2(134f, 8f));
            TMP_Text value = CreateText("Value", row, "0%", 30f, lightFont, TextAlignmentOptions.Right);
            SetRect(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(104f, 42f), new Vector2(-62f, 8f));

            Slider slider = CreateSlider("Slider", row);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-394f, 26f), new Vector2(90f, -12f));
            SetObject(view, "labelText", label);
            SetObject(view, "valueText", value);
            SetObject(view, "slider", slider);
            return view;
        }

        private static UIToggleRowView CreateToggleRow(string name, Transform parent, string labelText, string descriptionText, float topOffset, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UIToggleRowView view = row.gameObject.AddComponent<UIToggleRowView>();
            Image divider = CreateSolid("Divider", row, new Color(theme.LinePrimary.r, theme.LinePrimary.g, theme.LinePrimary.b, 0.34f), new Vector2(1000f, 2f), new Vector2(0f, -rowHeight * 0.5f + 2f));
            divider.raycastTarget = false;
            TMP_Text label = CreateText("Label", row, labelText, 32f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(520f, 38f), new Vector2(260f, 16f));
            TMP_Text description = CreateText("Description", row, descriptionText, 22f, lightFont, TextAlignmentOptions.Left);
            SetRect(description.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(690f, 30f), new Vector2(345f, -20f));

            RectTransform toggleRect = CreateRect("Toggle", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(236f, 62f), new Vector2(-126f, 0f));
            V3GradientGraphic track = toggleRect.gameObject.AddComponent<V3GradientGraphic>();
            track.Configure(
                new Color(0.035f, 0.39f, 0.63f, 1f),
                new Color(0.012f, 0.22f, 0.42f, 1f),
                new Color(0f, 0.62f, 0.94f, 1f),
                4f);
            Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            TMP_Text state = CreateText("State", row, "OFF", 32f, boldFont, TextAlignmentOptions.Center);
            SetRect(state.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(132f, 52f), new Vector2(-164f, 0f));
            RectTransform handle = CreateRect("Handle", toggleRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 52f), new Vector2(5f, 0f));
            V3GradientGraphic handleImage = handle.gameObject.AddComponent<V3GradientGraphic>();
            handleImage.Configure(
                new Color(0.3f, 0.8f, 0.22f, 1f),
                new Color(0.08f, 0.42f, 0.08f, 1f),
                new Color(0.18f, 0.72f, 0.12f, 1f),
                4f);

            SetObject(view, "labelText", label);
            SetObject(view, "descriptionText", description);
            SetObject(view, "stateText", state);
            SetObject(view, "toggle", toggle);
            SetObject(view, "handle", handle);
            SetObject(view, "trackGradient", track);
            SetObject(view, "handleGradient", handleImage);
            SetColor(view, "onTrackColor", new Color(0.02f, 0.32f, 0.56f, 1f));
            SetColor(view, "offTrackColor", theme.SurfaceRaised);
            SetColor(view, "onHandleColor", theme.Green);
            SetColor(view, "offHandleColor", theme.Disabled);
            return view;
        }

        private static UISegmentedControlView CreateSegmentRow(string name, Transform parent, string labelText, float topOffset, string[] optionLabels, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UISegmentedControlView view = row.gameObject.AddComponent<UISegmentedControlView>();
            TMP_Text label = CreateText("Label", row, labelText, 27f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-40f, 34f), new Vector2(0f, -18f));

            RectTransform segmentRoot = CreateRect("Segments", row, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 46f), new Vector2(0f, 25f));
            Button[] buttons = new Button[optionLabels.Length];
            TMP_Text[] labels = new TMP_Text[optionLabels.Length];
            float gap = 8f;
            float segmentRootWidth = 980f;
            float width = (segmentRootWidth - (optionLabels.Length - 1) * gap) / optionLabels.Length;
            for (int i = 0; i < optionLabels.Length; i++)
            {
                Button button = CreateButton($"Segment{i}", segmentRoot, navDefault, navSelected, navSelected, navSelected, squareDisabled);
                ColorBlock segmentColors = button.colors;
                segmentColors.selectedColor = theme.Selected;
                segmentColors.disabledColor = theme.Selected;
                button.colors = segmentColors;
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(width, 46f), new Vector2(i * (width + gap) + width * 0.5f, 0f));
                TMP_Text segmentLabel = CreateText("Label", button.transform, optionLabels[i], 14f, boldFont, TextAlignmentOptions.Center);
                Stretch(segmentLabel.rectTransform);
                buttons[i] = button;
                labels[i] = segmentLabel;
            }

            SetObject(view, "segmentRoot", segmentRoot);
            SetObjectArray(view, "segmentButtons", buttons);
            SetObjectArray(view, "segmentLabels", labels);
            SetBool(view, "applyVisualSelection", true);
            SetObject(view, "normalSprite", navDefault);
            SetObject(view, "selectedSprite", navSelected);
            SetColor(view, "normalBackgroundColor", theme.Normal);
            SetColor(view, "selectedBackgroundColor", theme.Selected);
            return view;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            RectTransform root = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Slider slider = root.gameObject.AddComponent<Slider>();

            RectTransform background = CreateRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = null;
            backgroundImage.color = theme.SurfaceRaised;
            backgroundImage.raycastTarget = true;
            background.sizeDelta = new Vector2(0f, -12f);

            RectTransform fillArea = CreateRect("Fill Area", root, Vector2.zero, Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
            RectTransform fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = null;
            fillImage.color = theme.Blue;
            fillImage.raycastTarget = false;
            fillArea.sizeDelta = new Vector2(-28f, -12f);

            RectTransform handleArea = CreateRect("Handle Slide Area", root, Vector2.zero, Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
            RectTransform handle = CreateRect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f), Vector2.zero);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = null;
            handleImage.color = theme.TextPrimary;

            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 50f;
            return slider;
        }

        private static Button CreateButton(string name, Transform parent, Sprite normal, Sprite highlighted, Sprite pressed, Sprite selected, Sprite disabled)
        {
            Sprite sharedSprite = normal != null ? normal : art.Button;
            Button button = V3UiPrefabFactory.CreateButton(name, parent, sharedSprite, art.FocusOverlay, theme);
            if (button.image != null)
                button.image.pixelsPerUnitMultiplier = 2f;
            return button;
        }

        private static RectTransform CreateRowRoot(string name, Transform parent, float topOffset, float rowHeight)
        {
            RectTransform row = CreateRect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-52f, rowHeight),
                new Vector2(0f, -topOffset - rowHeight * 0.5f));
            return row;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float size, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 32f), Vector2.zero);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(10f, size - 4f);
            tmp.fontSizeMax = size;
            tmp.alignment = alignment;
            tmp.color = theme != null ? theme.TextPrimary : Color.white;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            Image image = V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycastTarget, sprite != null);
            if (sprite != null)
                ApplySliced(image, 2f);
            return image;
        }

        private static void ApplySliced(Image image, float pixelsPerUnitMultiplier)
        {
            if (image == null || image.sprite == null)
                return;

            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            return V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void WireSettingsButtons(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform settingsButtonTransform = FindDeepChild(root.transform, "SettingsButton");
                if (settingsButtonTransform == null)
                    return;

                Button button = settingsButtonTransform.GetComponent<Button>();
                if (button == null)
                    button = settingsButtonTransform.gameObject.AddComponent<Button>();

                UIShellRouteButtonView routeButton = settingsButtonTransform.GetComponent<UIShellRouteButtonView>();
                if (routeButton != null)
                    Object.DestroyImmediate(routeButton, true);

                UIShellActionButtonView actionButton = settingsButtonTransform.GetComponent<UIShellActionButtonView>();
                if (actionButton == null)
                    actionButton = settingsButtonTransform.gameObject.AddComponent<UIShellActionButtonView>();
                SetEnum(actionButton, "actionKind", (int)UiActionKind.OpenSettings);
                SetInt(actionButton, "payloadId", 0);
                SetObject(actionButton, "button", button);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RepairMatchLifecycleControlsInternal()
        {
            WireMatchHudLifecycleButtons();
            WirePauseMenuButtons();
            GameObject pauseMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPopupPath);
            if (pauseMenuPrefab == null)
                throw new FileNotFoundException($"Missing pause menu prefab: {PauseMenuPopupPath}");
            WirePauseMenuScene(pauseMenuPrefab);
        }

        private static void WireMatchHudLifecycleButtons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MatchHudContentPath);
            try
            {
                EnsureActionButton(root.transform, "PauseButton", UiActionKind.Pause, scopedPointerRoute: true);
                EnsureActionButton(root.transform, "SettingsButton", UiActionKind.OpenSettings, scopedPointerRoute: true);
                PrefabUtility.SaveAsPrefabAsset(root, MatchHudContentPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WirePauseMenuButtons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PauseMenuPopupPath);
            try
            {
                EnsureActionButton(root.transform, "ResumeButton", UiActionKind.ClosePause, scopedPointerRoute: false);
                EnsureActionButton(root.transform, "SettingsButton", UiActionKind.OpenSettings, scopedPointerRoute: false);
                EnsureActionButton(root.transform, "ExitButton", UiActionKind.MatchMenu, scopedPointerRoute: false);
                PrefabUtility.SaveAsPrefabAsset(root, PauseMenuPopupPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureActionButton(
            Transform root,
            string buttonName,
            UiActionKind actionKind,
            bool scopedPointerRoute)
        {
            Transform buttonTransform = FindDeepChild(root, buttonName);
            if (buttonTransform == null)
                throw new MissingReferenceException($"{root.name} is missing {buttonName}.");

            Button button = buttonTransform.GetComponent<Button>();
            if (button == null)
                throw new MissingComponentException($"{root.name}/{buttonName} is missing Button.");

            if (scopedPointerRoute)
            {
                Canvas canvas = buttonTransform.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = buttonTransform.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = false;
                if (buttonTransform.GetComponent<GraphicRaycaster>() == null)
                    buttonTransform.gameObject.AddComponent<GraphicRaycaster>();
            }

            UIShellRouteButtonView routeButton = buttonTransform.GetComponent<UIShellRouteButtonView>();
            if (routeButton != null)
                Object.DestroyImmediate(routeButton, true);

            UIShellActionButtonView actionButton = buttonTransform.GetComponent<UIShellActionButtonView>();
            if (actionButton == null)
                actionButton = buttonTransform.gameObject.AddComponent<UIShellActionButtonView>();
            SetEnum(actionButton, "actionKind", (int)actionKind);
            SetInt(actionButton, "payloadId", 0);
            SetObject(actionButton, "button", button);
        }

        private static void WirePauseMenuScene(GameObject pauseMenuPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = Object.FindAnyObjectByType<UIShellContentView>(FindObjectsInactive.Include);
            if (content == null)
                throw new MissingReferenceException($"{MenuScenePath} is missing UIShellContentView.");

            SetObject(content, "pauseMenuPopupPrefab", pauseMenuPrefab);
            EditorUtility.SetDirty(content);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WireMenuScene(GameObject sharedPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = Object.FindAnyObjectByType<UIShellContentView>(FindObjectsInactive.Include);
            if (content == null)
                return;

            SetObject(content, "settingsPopupPrefab", sharedPrefab);
            EditorUtility.SetDirty(content);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void DeleteLegacyPopup(string path)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
                return;

            AssetDatabase.DeleteAsset(path);
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = FindDeepChild(parent.GetChild(i), name);
                if (child != null)
                    return child;
            }

            return null;
        }

        private static void CapturePopup(string prefabPath, SettingsPopupContext context, string outputPath, int width, int height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SettingsPopupPrefabBuilder] Missing popup prefab for capture: {prefabPath}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("SettingsPopupCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.05f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            GameObject canvasObject = new("SettingsPopupCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.position = Vector3.zero;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = MenuReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            string backdropPath = context == SettingsPopupContext.Match ? MatchHudContentPath : MainMenuContentPath;
            GameObject backdropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(backdropPath);
            if (backdropPrefab != null)
            {
                GameObject backdrop = Object.Instantiate(backdropPrefab, canvasRect);
                backdrop.name = backdropPrefab.name;
                if (backdrop.transform is RectTransform backdropRect)
                    Stretch(backdropRect);
                backdrop.transform.SetAsFirstSibling();
            }

            GameObject instance = Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            RectTransform instanceRect = instance.GetComponent<RectTransform>();
            Stretch(instanceRect);
            SettingsPopupView popupView = instance.GetComponent<SettingsPopupView>();
            popupView?.ConfigureContext(context);
            popupView?.LoadSettings();
            foreach (UISliderRowView sliderRow in instance.GetComponentsInChildren<UISliderRowView>(true))
            {
                if (sliderRow.name == "MusicVolumeRow")
                {
                    sliderRow.Bind("MUSIC VOLUME", 60f, 0f, 100f);
                    break;
                }
            }

            Canvas.ForceUpdateCanvases();
            float expandScaleFactor = Mathf.Min(
                width / MenuReferenceResolution.x,
                height / MenuReferenceResolution.y);
            Vector2 simulatedCanvasSize = new(
                width / expandScaleFactor,
                height / expandScaleFactor);
            instance.GetComponentInChildren<SettingsPopupResponsiveScaleView>(true)
                ?.RefreshForCanvasSize(simulatedCanvasSize);
            Canvas.ForceUpdateCanvases();
            RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"[SettingsPopupPrefabBuilder] captured={outputPath} size={width}x{height} scene={scene.name}");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(renderTexture);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void SetObject(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray(Object target, string fieldName, Object[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetColorArray(Object target, string fieldName, Color[] values)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).colorValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string fieldName, bool value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetColor(Object target, string fieldName, Color value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(Object target, string fieldName, int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string fieldName, int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
