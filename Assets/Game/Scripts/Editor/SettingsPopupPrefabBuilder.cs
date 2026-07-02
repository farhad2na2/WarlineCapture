using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class SettingsPopupPrefabBuilder
    {
        private const string SpriteRoot = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/";
        private const string PopupsRoot = "Assets/Game/Prefabs/UI/Shell/Popups/";
        private const string SharedPopupPath = PopupsRoot + "SCN_SettingsPopup.prefab";
        private const string LegacyMenuPopupPath = PopupsRoot + "SCN02_MenuSettingsPopup.prefab";
        private const string LegacyMatchPopupPath = PopupsRoot + "SCN08_MatchSettingsPopup.prefab";
        private const string MainMenuContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MatchHudContentPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const float PopupRuntimeScale = 2.1f;
        private static readonly Vector2 MenuReferenceResolution = new(4800f, 2160f);
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string LightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static TMP_FontAsset lightFont;
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
        private static readonly string[] GraphicsQualityLabels = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
        private static readonly string[] FrameRateLabels = { "30 FPS", "60 FPS", "120 FPS" };
        private static readonly string[] AssistanceLevelLabels = { "FULL", "HINTS", "MINIMAL", "OFF" };
        private static readonly string[] ColorblindModeLabels = { "OFF", "PRO", "DEU", "TRI" };
        private static readonly string[] LanguageLabels = { "EN", "DE", "FR", "ES" };

        [MenuItem("Tools/Game/UI/Rebuild Settings Popups")]
        public static void Build()
        {
            LoadAssets();
            GameObject sharedPrefab = BuildPopup(SharedPopupPath, "SCN_SettingsPopup");
            DeleteLegacyPopup(LegacyMenuPopupPath);
            DeleteLegacyPopup(LegacyMatchPopupPath);
            WireSettingsButtons(MainMenuContentPath);
            WireSettingsButtons(MatchHudContentPath);
            WireMenuScene(sharedPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SettingsPopupPrefabBuilder] Shared settings popup prefab rebuilt and shell bindings updated.");
        }

        [MenuItem("Tools/Game/UI/Capture Settings Popup QA")]
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
            panelBacking = LoadSprite("scn02c_mode_card_backing_blue.png");
            panelFrame = LoadSprite("scn02c_mode_card_frame_default_blue.png");
            headerFrame = LoadSprite("scn02c_header_bar_frame.png");
            squareDefault = LoadSprite("scn02c_header_square_button_frame_default.png");
            squareHover = LoadSprite("scn02c_header_square_button_frame_hover.png");
            squarePressed = LoadSprite("scn02c_header_square_button_frame_pressed.png");
            squareSelected = LoadSprite("scn02c_header_square_button_frame_selected.png");
            squareDisabled = LoadSprite("scn02c_header_square_button_frame_disabled.png");
            deployDefault = LoadSprite("scn02c_deploy_button_frame.png");
            deployHover = LoadSprite("scn02c_deploy_button_frame_hover.png");
            deployPressed = LoadSprite("scn02c_deploy_button_frame_pressed.png");
            deploySelected = LoadSprite("scn02c_deploy_button_frame_selected.png");
            deployDisabled = LoadSprite("scn02c_deploy_button_frame_disabled.png");
            navDefault = LoadSprite("scn02c_nav_button_frame_default.png");
            navSelected = LoadSprite("scn02c_nav_button_frame_selected.png");
            resourceChip = LoadSprite("scn02c_resource_chip_frame.png");
            settingsIcon = LoadSprite("scn02c_settings_gear_icon.png");
        }

        private static GameObject BuildPopup(string path, string prefabName)
        {
            GameObject root = CreateRect(prefabName, null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            root.AddComponent<CanvasGroup>();
            root.AddComponent<UIPopupMotionView>();

            Image dim = CreateImage("InputBlocker", root.transform, null, new Color(0f, 0f, 0f, 0.48f), true);
            Stretch(dim.rectTransform);

            RectTransform panel = CreateRect("SettingsRoot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1660f, 940f), Vector2.zero);
            panel.localScale = Vector3.one * PopupRuntimeScale;
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = panelBacking;
            ApplySliced(panelImage, 2f);
            panelImage.raycastTarget = true;

            Image frame = CreateImage("Frame", panel, panelFrame, Color.white, false);
            Stretch(frame.rectTransform);
            ApplySliced(frame, 2f);

            RectTransform header = CreateRect("Header", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 112f), new Vector2(0f, -56f));
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.sprite = headerFrame;
            ApplySliced(headerImage, 2f);
            headerImage.raycastTarget = false;

            Image gear = CreateImage("SettingsIcon", header, settingsIcon, Color.white, false);
            SetRect(gear.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(68f, 68f), new Vector2(70f, 0f));

            TMP_Text title = CreateText("TitleText", header, "COMMAND SETTINGS", 42f, boldFont, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-260f, 0f), new Vector2(166f, 0f));

            Button closeButton = CreateButton("CloseButton", header, squareDefault, squareHover, squarePressed, squareSelected, squareDisabled);
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(74f, 74f), new Vector2(-70f, 0f));
            TMP_Text closeLabel = CreateText("CloseLabel", closeButton.transform, "X", 30f, boldFont, TextAlignmentOptions.Center);
            Stretch(closeLabel.rectTransform);

            SettingsPanelView panelView = panel.gameObject.AddComponent<SettingsPanelView>();
            RectTransform content = CreateRect("Content", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1540f, 720f), new Vector2(0f, -470f));
            RectTransform audioSection = CreateSection("AudioSection", content, "AUDIO", new Vector2(-395f, 190f), new Vector2(750f, 300f));
            RectTransform controlSection = CreateSection("ControlSection", content, "GAMEPLAY", new Vector2(-395f, -170f), new Vector2(750f, 330f));
            RectTransform displaySection = CreateSection("DisplaySection", content, "VIDEO", new Vector2(395f, 190f), new Vector2(750f, 280f));
            RectTransform accessSection = CreateSection("AccessibilitySection", content, "ACCESSIBILITY", new Vector2(395f, -150f), new Vector2(750f, 380f));

            UISliderRowView master = CreateSliderRow("MasterVolumeRow", audioSection, "MASTER VOLUME", 52f, 72f);
            UISliderRowView music = CreateSliderRow("MusicVolumeRow", audioSection, "MUSIC", 130f, 72f);
            UISliderRowView sfx = CreateSliderRow("SfxVolumeRow", audioSection, "SFX", 208f, 72f);

            UISliderRowView camera = CreateSliderRow("CameraSensitivityRow", controlSection, "CAMERA SENSITIVITY", 52f, 72f);
            UIToggleRowView threat = CreateToggleRow("ThreatWarningsRow", controlSection, "THREAT WARNINGS", "Show tactical warnings during missions.", 130f, 70f);
            UISegmentedControlView assistance = CreateSegmentRow("AssistanceLevelControl", controlSection, "ASSISTANT GUIDANCE", 208f, AssistanceLevelLabels, 84f);

            UISegmentedControlView quality = CreateSegmentRow("GraphicsQualityControl", displaySection, "GRAPHICS QUALITY", 52f, GraphicsQualityLabels, 84f);
            UISegmentedControlView frameRate = CreateSegmentRow("FrameRateControl", displaySection, "FRAME RATE", 144f, FrameRateLabels, 84f);

            UIToggleRowView contrast = CreateToggleRow("HighContrastRow", accessSection, "HIGH CONTRAST UI", "Increase panel and text contrast.", 52f, 70f);
            UIToggleRowView largeText = CreateToggleRow("LargeTextRow", accessSection, "LARGE TEXT", "Increase UI text scale for readability.", 124f, 70f);
            UISegmentedControlView colorblind = CreateSegmentRow("ColorblindModeControl", accessSection, "COLORBLIND MODE", 200f, ColorblindModeLabels, 84f);
            UISegmentedControlView language = CreateSegmentRow("LanguageControl", accessSection, "LANGUAGE", 282f, LanguageLabels, 84f);

            SetObject(panelView, "masterVolumeRow", master);
            SetObject(panelView, "musicVolumeRow", music);
            SetObject(panelView, "sfxVolumeRow", sfx);
            SetObject(panelView, "graphicsQualityControl", quality);
            SetObject(panelView, "frameRateControl", frameRate);
            SetObject(panelView, "cameraSensitivityRow", camera);
            SetObject(panelView, "threatWarningsRow", threat);
            SetObject(panelView, "highContrastRow", contrast);
            SetObject(panelView, "largeTextRow", largeText);
            SetObject(panelView, "assistanceLevelControl", assistance);
            SetObject(panelView, "colorblindModeControl", colorblind);
            SetObject(panelView, "languageControl", language);

            RectTransform footer = CreateRect("Footer", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 100f), new Vector2(0f, 50f));
            Button reset = CreateButton("ResetButton", footer, deployDefault, deployHover, deployPressed, deploySelected, deployDisabled);
            SetRect(reset.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(260f, 66f), new Vector2(-405f, 0f));
            TMP_Text resetText = CreateText("Label", reset.transform, "RESET", 27f, boldFont, TextAlignmentOptions.Center);
            Stretch(resetText.rectTransform);

            Button apply = CreateButton("ApplyButton", footer, deployDefault, deployHover, deployPressed, deploySelected, deployDisabled);
            SetRect(apply.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(300f, 66f), new Vector2(-120f, 0f));
            TMP_Text applyText = CreateText("Label", apply.transform, "APPLY", 27f, boldFont, TextAlignmentOptions.Center);
            Stretch(applyText.rectTransform);

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

        private static RectTransform CreateSection(string name, Transform parent, string title, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform section = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPosition);
            Image backing = section.gameObject.AddComponent<Image>();
            backing.sprite = panelBacking;
            ApplySliced(backing, 2f);
            backing.color = new Color(0.66f, 0.84f, 0.92f, 0.22f);
            backing.raycastTarget = false;

            Image frame = CreateImage("SectionFrame", section, resourceChip, new Color(1f, 1f, 1f, 0.72f), false);
            Stretch(frame.rectTransform);
            ApplySliced(frame, 2f);

            Image rail = CreateImage("TitleRail", section, resourceChip, new Color(0.84f, 0.95f, 1f, 0.62f), false);
            SetRect(rail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-40f, 30f), new Vector2(0f, -19f));
            ApplySliced(rail, 2f);

            TMP_Text sectionTitle = CreateText("SectionTitle", section, title, 22f, boldFont, TextAlignmentOptions.Left);
            SetRect(sectionTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-52f, 28f), new Vector2(28f, -20f));
            sectionTitle.color = new Color(0.97f, 0.9f, 0.66f, 1f);
            return section;
        }

        private static UISliderRowView CreateSliderRow(string name, Transform parent, string labelText, float topOffset, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UISliderRowView view = row.gameObject.AddComponent<UISliderRowView>();
            TMP_Text label = CreateText("Label", row, labelText, 21f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-116f, 28f), new Vector2(-52f, -18f));
            TMP_Text value = CreateText("Value", row, "0%", 21f, lightFont, TextAlignmentOptions.Right);
            SetRect(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(86f, 28f), new Vector2(-49f, -18f));

            Slider slider = CreateSlider("Slider", row);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-52f, 28f), new Vector2(0f, 18f));
            SetObject(view, "labelText", label);
            SetObject(view, "valueText", value);
            SetObject(view, "slider", slider);
            return view;
        }

        private static UIToggleRowView CreateToggleRow(string name, Transform parent, string labelText, string descriptionText, float topOffset, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UIToggleRowView view = row.gameObject.AddComponent<UIToggleRowView>();
            TMP_Text label = CreateText("Label", row, labelText, 21f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-176f, 28f), new Vector2(-82f, -18f));
            TMP_Text description = CreateText("Description", row, descriptionText, 15f, lightFont, TextAlignmentOptions.Left);
            SetRect(description.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-176f, 24f), new Vector2(-82f, 19f));
            TMP_Text state = CreateText("State", row, "OFF", 20f, boldFont, TextAlignmentOptions.Right);
            SetRect(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(56f, 26f), new Vector2(-84f, -18f));

            RectTransform toggleRect = CreateRect("Toggle", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(94f, 30f), new Vector2(-48f, -6f));
            Image track = toggleRect.gameObject.AddComponent<Image>();
            track.sprite = resourceChip;
            ApplySliced(track, 2f);
            Toggle toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            RectTransform handle = CreateRect("Handle", toggleRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 30f), new Vector2(5f, 0f));
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = squareDefault;
            ApplySliced(handleImage, 2f);

            SetObject(view, "labelText", label);
            SetObject(view, "descriptionText", description);
            SetObject(view, "stateText", state);
            SetObject(view, "toggle", toggle);
            SetObject(view, "handle", handle);
            return view;
        }

        private static UISegmentedControlView CreateSegmentRow(string name, Transform parent, string labelText, float topOffset, string[] optionLabels, float rowHeight)
        {
            RectTransform row = CreateRowRoot(name, parent, topOffset, rowHeight);
            UISegmentedControlView view = row.gameObject.AddComponent<UISegmentedControlView>();
            TMP_Text label = CreateText("Label", row, labelText, 21f, mediumFont, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-52f, 30f), new Vector2(0f, -18f));

            RectTransform segmentRoot = CreateRect("Segments", row, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 40f), new Vector2(0f, 22f));
            Button[] buttons = new Button[optionLabels.Length];
            TMP_Text[] labels = new TMP_Text[optionLabels.Length];
            float gap = 8f;
            float segmentRootWidth = 640f;
            float width = (segmentRootWidth - (optionLabels.Length - 1) * gap) / optionLabels.Length;
            for (int i = 0; i < optionLabels.Length; i++)
            {
                Button button = CreateButton($"Segment{i}", segmentRoot, navDefault, navSelected, navSelected, navSelected, squareDisabled);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(width, 40f), new Vector2(i * (width + gap) + width * 0.5f, 0f));
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
            return view;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            RectTransform root = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Slider slider = root.gameObject.AddComponent<Slider>();

            RectTransform background = CreateRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = resourceChip;
            ApplySliced(backgroundImage, 2f);
            backgroundImage.raycastTarget = true;

            RectTransform fillArea = CreateRect("Fill Area", root, Vector2.zero, Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
            RectTransform fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = resourceChip;
            ApplySliced(fillImage, 2f);
            fillImage.color = new Color(0.43f, 0.91f, 0.82f, 0.92f);
            fillImage.raycastTarget = false;

            RectTransform handleArea = CreateRect("Handle Slide Area", root, Vector2.zero, Vector2.one, new Vector2(-28f, 0f), Vector2.zero);
            RectTransform handle = CreateRect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f), Vector2.zero);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = squareDefault;
            ApplySliced(handleImage, 2f);

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
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 44f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = normal;
            ApplySliced(image, 2f);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = highlighted,
                pressedSprite = pressed,
                selectedSprite = selected,
                disabledSprite = disabled
            };
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
            tmp.color = new Color(0.88f, 0.96f, 0.98f, 1f);
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
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
            GameObject go = new(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, sizeDelta, anchoredPosition);
            return rect;
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

        private static Sprite LoadSprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + fileName);
            if (sprite == null)
                Debug.LogError($"[SettingsPopupPrefabBuilder] Missing sprite {SpriteRoot}{fileName}");
            return sprite;
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject instance = Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            RectTransform instanceRect = instance.GetComponent<RectTransform>();
            Stretch(instanceRect);
            SettingsPopupView popupView = instance.GetComponent<SettingsPopupView>();
            popupView?.ConfigureContext(context);
            popupView?.LoadSettings();

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

        private static void SetBool(Object target, string fieldName, bool value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(fieldName).boolValue = value;
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
