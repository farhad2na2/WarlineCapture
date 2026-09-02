using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace Game.Editor
{
    public static class MainMenuV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string CommanderScenePath = "Assets/Game/Art/UI/V3Shared/CommanderScenes/SCN02_FieldCommander_01_Scene_V3.png";
        private const string SceneAtlasPath = "Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_MainMenuScenes_01.spriteatlas";
        private const string AriaAtlasPath = "Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_Assistants_01.spriteatlas";
        private const string MainMenuIconAtlasPath = "Assets/Game/Art/UI/V3Shared/Atlases/UI_V3_MainMenuIcons_01.spriteatlas";
        private const string DefaultCommanderId = "field_commander_01";
        private const string CampaignArtPath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_CampaignScene_V3.png";
        private const string OperationsArtPath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_OperationsScene_V3.png";
        private const string SkirmishArtPath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_SkirmishScene_V3.png";
        private const string CampaignIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_CampaignTarget_V3.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string SkirmishIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_SkirmishBlades_V3.png";
        private const string StoreIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_StoreCart_V3.png";
        private const string ArmoryIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_ArmoryCrate_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(62, 76, 82, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(196, 202, 198, 255);
        private static readonly Color Amber = new Color32(255, 177, 0, 255);
        private static readonly Color Green = new Color32(25, 185, 93, 255);
        private static readonly Color Red = new Color32(241, 69, 20, 255);
        private static readonly Color Cyan = new Color32(0, 185, 236, 255);
        private static readonly Color GraphiteTop = new Color32(20, 31, 35, 250);
        private static readonly Color GraphiteBottom = new Color32(4, 10, 13, 253);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static Sprite commanderScene;
        private static Sprite campaignArt;
        private static Sprite operationsArt;
        private static Sprite skirmishArt;
        private static Sprite ariaPortrait;
        private static Sprite campaignIcon;
        private static Sprite operationsIcon;
        private static Sprite skirmishIcon;
        private static Sprite storeIcon;
        private static Sprite armoryIcon;
        private static Sprite creditsIcon;
        private static Sprite commandIcon;
        private static Sprite settingsIcon;

        [MenuItem("Game/UI/Rebuild Main Menu V3")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            ConfigureTexture(CommanderScenePath, false, 2048);
            ConfigureTexture(CampaignArtPath, false, 2048);
            ConfigureTexture(OperationsArtPath, false, 2048);
            ConfigureTexture(SkirmishArtPath, false, 2048);
            ConfigureTexture(V3UiFoundationBuilder.SharedAriaPortraitPath, true, 2048);
            ConfigureTexture(CampaignIconPath, true, 512);
            ConfigureTexture(OperationsIconPath, true, 512);
            ConfigureTexture(SkirmishIconPath, true, 512);
            ConfigureTexture(StoreIconPath, true, 512);
            ConfigureTexture(ArmoryIconPath, true, 512);
            BuildAtlas(SceneAtlasPath, "UI_V3_MainMenuScenes_01", CampaignArtPath, OperationsArtPath, SkirmishArtPath);
            BuildAtlas(AriaAtlasPath, "UI_V3_Assistants_01", V3UiFoundationBuilder.SharedAriaPortraitPath);
            BuildAtlas(MainMenuIconAtlasPath, "UI_V3_MainMenuIcons_01", CampaignIconPath, OperationsIconPath, SkirmishIconPath, StoreIconPath, ArmoryIconPath);
            LoadAssets();

            GameObject root = CreateRect("SCN02_MainMenuContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            UIShellContentSectionsView sectionsView = root.AddComponent<UIShellContentSectionsView>();
            var sections = new List<UIShellContentSectionsView.SectionReference>(6);
            RectTransform backgroundSection = CreateSection("MenuBackgroundContent", root.transform, UIShellContentSectionId.MenuBackground, sections);
            RectTransform headerSection = CreateSection("HeaderContent", root.transform, UIShellContentSectionId.Header, sections);
            RectTransform leftSection = CreateSection("LeftContent", root.transform, UIShellContentSectionId.Left, sections);
            RectTransform middleSection = CreateSection("MiddleContent", root.transform, UIShellContentSectionId.Middle, sections);
            RectTransform rightSection = CreateSection("RightContent", root.transform, UIShellContentSectionId.Right, sections);
            RectTransform footerSection = CreateSection("FooterContent", root.transform, UIShellContentSectionId.Footer, sections);
            sectionsView.ConfigureSections(sections.ToArray());

            BuildBackground(backgroundSection);
            BuildHeader(headerSection);
            BuildModeCards(leftSection);
            BuildMiddleHitTargets(middleSection);
            BuildRightRail(rightSection);
            BuildFooter(footerSection);
            ConfigureRuntimeLayouts(headerSection, leftSection, middleSection, rightSection, footerSection);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MainMenuV3PrefabBuilder] result=Passed v3=True cohesive baked commander scene selected by stable commander ID; live UI remains shared/procedural.");
        }

        [MenuItem("Game/UI/V3/Validate Main Menu")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Main Menu V3 prefab: {PrefabPath}");

            UIShellContentSectionsView sections = prefab.GetComponent<UIShellContentSectionsView>();
            if (sections == null || sections.Sections == null || sections.Sections.Count != 6)
                throw new MissingReferenceException("Main Menu V3 must expose all six shell sections.");

            Require(prefab.transform, "HeaderContent/HeaderResourceArea/CreditsPanel/Frame");
            Require(prefab.transform, "HeaderContent/HeaderResourceArea/CommandPanel/Frame");
            Require(prefab.transform, "LeftContent/Card_Campaign/Hotspot");
            Require(prefab.transform, "LeftContent/Card_Operations/Hotspot");
            Require(prefab.transform, "LeftContent/Card_Skirmish/Hotspot");
            Require(prefab.transform, "RightContent/CommanderPanel/CommanderPanelHotspot");
            Require(prefab.transform, "FooterContent/StoreButton");
            Require(prefab.transform, "FooterContent/OpenArmoryButton");

            Transform commanderTransform = Require(prefab.transform, "MenuBackgroundContent/CommanderSceneVariant");
            MainMenuCommanderVariantView commanderView = commanderTransform.GetComponent<MainMenuCommanderVariantView>();
            Image commanderImage = commanderTransform.GetComponent<Image>();
            AspectRatioFitter commanderFitter = commanderTransform.GetComponent<AspectRatioFitter>();
            if (commanderView == null || commanderImage == null || commanderView.Target != commanderImage ||
                commanderView.Variants == null || commanderView.Variants.Length < 1 ||
                !string.Equals(commanderView.DefaultCommanderId, DefaultCommanderId, StringComparison.Ordinal))
                throw new MissingReferenceException("Main Menu V3 must bind one cohesive baked commander scene by stable commander ID.");
            if (commanderFitter == null || commanderFitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new MissingComponentException("Main Menu V3 commander scene must use an aspect-fill crop instead of stretching.");

            MainMenuV3SectionLayoutView[] layouts = prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
            if (layouts.Length < 6)
                throw new MissingComponentException("Main Menu V3 must map every authored reference section into the live shell canvas.");

            ValidateAtlas(SceneAtlasPath, CampaignArtPath, OperationsArtPath, SkirmishArtPath);
            ValidateAtlas(AriaAtlasPath, V3UiFoundationBuilder.SharedAriaPortraitPath);
            ValidateAtlas(MainMenuIconAtlasPath, CampaignIconPath, OperationsIconPath, SkirmishIconPath, StoreIconPath, ArmoryIconPath);

            if (prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length < 18)
                throw new MissingComponentException("Main Menu V3 requires procedural gradients on its live chrome.");
            if (prefab.GetComponentsInChildren<V3RingGraphic>(true).Length < 8)
                throw new MissingComponentException("Main Menu V3 requires procedural rings for ARIA telemetry and Operations progress.");

            Transform settings = Require(prefab.transform, "HeaderContent/SettingsButton");
            UIShellActionButtonView settingsAction = settings.GetComponent<UIShellActionButtonView>();
            if (settingsAction == null || settingsAction.ActionKind != UiActionKind.OpenSettings || settings.GetComponent<UIShellRouteButtonView>() != null)
                throw new InvalidOperationException("Main Menu Settings must enqueue OpenSettings, not route to the legacy Settings screen.");

            ValidateRoute(prefab, "Card_Campaign", UIRoute.Campaign);
            ValidateRoute(prefab, "Card_Operations", UIRoute.Operations);
            ValidateRoute(prefab, "Card_Skirmish", UIRoute.QuickCustomSetup);
            ValidateRoute(prefab, "CommanderPanelHotspot", UIRoute.CommanderProfile);
            ValidateRoute(prefab, "StoreButton", UIRoute.CommandExchange);
            ValidateRoute(prefab, "OpenArmoryButton", UIRoute.Armory);

            HashSet<string> allowedRasterPaths = new(StringComparer.Ordinal)
            {
                CommanderScenePath,
                CampaignArtPath,
                OperationsArtPath,
                SkirmishArtPath,
                V3UiFoundationBuilder.SharedAriaPortraitPath,
                CampaignIconPath,
                OperationsIconPath,
                SkirmishIconPath,
                StoreIconPath,
                ArmoryIconPath,
                CanonicalUiResourceIconPaths.Credits,
                CanonicalUiResourceIconPaths.Command,
                V3UiFoundationBuilder.SettingsIconPath,
                V3UiFoundationBuilder.MainMenuLogoPath
            };
            foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(image.sprite);
                if (!allowedRasterPaths.Contains(path))
                    throw new InvalidOperationException($"Main Menu V3 references historical or duplicated raster chrome: {path}");
            }

            Debug.Log($"[MainMenuV3PrefabBuilder] validation=Passed gradients={prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length} images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        [MenuItem("Game/UI/Capture Main Menu V3 QA")]
        public static void CaptureQa()
        {
            Capture("/private/tmp/warline-main-menu-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-main-menu-v3-20x9.png", 2400, 1080);
            Debug.Log("[MainMenuV3PrefabBuilder] QA captures written to /private/tmp.");
        }

        [MenuItem("Game/UI/V3/Capture Running Main Menu")]
        private static void CaptureRunningMainMenu()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before capturing the running Main Menu.");

            string outputPath = $"/private/tmp/warline-main-menu-v3-runtime-{Screen.width}x{Screen.height}.png";
            ScreenCapture.CaptureScreenshot(outputPath);
            Debug.Log($"[MainMenuV3PrefabBuilder] runtimeCapture={outputPath}");
        }

        [MenuItem("Game/UI/V3/Open Settings In Running Menu")]
        private static void OpenSettingsInRunningMenu()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before opening Settings from the running Main Menu.");

            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (!string.Equals(button.name, "SettingsButton", StringComparison.Ordinal))
                    continue;

                button.onClick.Invoke();
                Debug.Log("[MainMenuV3PrefabBuilder] runtimeSettingsAction=Invoked");
                return;
            }

            throw new MissingReferenceException("The running Main Menu has no active SettingsButton.");
        }

        [MenuItem("Game/UI/V3/Set Game View 1920x1080")]
        private static void SetGameView16By9()
        {
            SetGameViewResolution(1920, 1080);
        }

        [MenuItem("Game/UI/V3/Set Game View 4800x2160")]
        private static void SetGameView20By9()
        {
            SetGameViewResolution(4800, 2160);
        }

        internal static void SetGameViewResolution(int width, int height)
        {
            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type singletonOpenType = editorAssembly.GetType("UnityEditor.ScriptableSingleton`1");
            if (gameViewType == null || sizesType == null || groupType == null || singletonOpenType == null)
                throw new MissingMemberException("Unity Game View resolution API is unavailable.");

            Type singletonType = singletonOpenType.MakeGenericType(sizesType);
            PropertyInfo instanceProperty = singletonType.GetProperty(
                "instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object sizes = instanceProperty?.GetValue(null);
            MethodInfo getGroup = sizesType.GetMethod(
                "GetGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object androidGroup = getGroup?.Invoke(sizes, new[] { Enum.Parse(groupType, "Android") });
            if (androidGroup == null)
                throw new MissingMemberException("Unity Android Game View size group is unavailable.");

            MethodInfo getTotalCount = androidGroup.GetType().GetMethod(
                "GetTotalCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getGameViewSize = androidGroup.GetType().GetMethod(
                "GetGameViewSize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = getTotalCount != null ? (int)getTotalCount.Invoke(androidGroup, null) : 0;
            int matchingIndex = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getGameViewSize?.Invoke(androidGroup, new object[] { i });
                if (size == null)
                    continue;

                PropertyInfo widthProperty = size.GetType().GetProperty(
                    "width",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                PropertyInfo heightProperty = size.GetType().GetProperty(
                    "height",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (widthProperty?.GetValue(size) is int candidateWidth &&
                    heightProperty?.GetValue(size) is int candidateHeight &&
                    candidateWidth == width && candidateHeight == height)
                {
                    // Custom fixed-resolution presets are listed after Unity's
                    // built-in aspect entries (for example "Landscape"). Keep
                    // the final exact match so runtime QA uses real pixels.
                    matchingIndex = i;
                }
            }

            if (matchingIndex < 0)
                throw new InvalidOperationException($"Game View preset {width}x{height} is missing from the Android size list.");

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selectedSize = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedSize == null)
                throw new MissingMemberException("Unity Game View selectedSizeIndex is unavailable.");

            selectedSize.SetValue(gameView, matchingIndex);
            FieldInfo zoomAreaField = gameViewType.GetField(
                "m_ZoomArea",
                BindingFlags.Instance | BindingFlags.NonPublic);
            object zoomArea = zoomAreaField?.GetValue(gameView);
            PropertyInfo scaleWithWindow = zoomArea?.GetType().GetProperty(
                "scaleWithWindow",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            scaleWithWindow?.SetValue(zoomArea, true);
            gameView.Repaint();
            Debug.Log($"[MainMenuV3PrefabBuilder] gameView={width}x{height} selectedIndex={matchingIndex}");
        }

        private static RectTransform CreateSection(
            string name,
            Transform root,
            UIShellContentSectionId id,
            ICollection<UIShellContentSectionsView.SectionReference> sections)
        {
            RectTransform section = CreateRect(name, root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            sections.Add(new UIShellContentSectionsView.SectionReference(id, section.gameObject));
            return section;
        }

        private static void BuildBackground(Transform root)
        {
            Image commanderSceneImage = CreateImage("CommanderSceneVariant", root, commanderScene, Color.white, false);
            Stretch(commanderSceneImage.rectTransform);
            AspectRatioFitter fitter = commanderSceneImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = commanderScene.rect.width / commanderScene.rect.height;
            MainMenuCommanderVariantView commanderView = commanderSceneImage.gameObject.AddComponent<MainMenuCommanderVariantView>();
            commanderView.Configure(
                commanderSceneImage,
                new[] { new MainMenuCommanderVariantView.CommanderVariant(DefaultCommanderId, commanderScene) },
                DefaultCommanderId);

            RectTransform shadeReference = CreateTopLeftRect("BackgroundChromeReference", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            ConfigureLayout(shadeReference, MainMenuV3SectionAlignment.TopLeft);
            V3GradientGraphic topShade = CreateGradient("HeaderReadability", shadeReference, new Color(0f, 0f, 0f, 0.55f), new Color(0f, 0f, 0f, 0f), Color.clear, 0f);
            SetTopLeft(topShade.rectTransform, 0f, 0f, 1672f, 205f);
        }

        private static void ConfigureRuntimeLayouts(
            RectTransform header,
            RectTransform left,
            RectTransform middle,
            RectTransform right,
            RectTransform footer)
        {
            ConfigureLayout(
                header,
                MainMenuV3SectionAlignment.TopLeft,
                header.Find("CreditsVisualPanel") as RectTransform,
                header.Find("CommandVisualPanel") as RectTransform,
                header.Find("SettingsButton") as RectTransform);
            ConfigureLayout(left, MainMenuV3SectionAlignment.TopLeft);
            ConfigureLayout(middle, MainMenuV3SectionAlignment.Center);
            ConfigureLayout(right, MainMenuV3SectionAlignment.TopRight);
            MainMenuV3SectionLayoutView footerLayout = footer.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            footerLayout.Configure(
                ReferenceResolution,
                MainMenuV3SectionAlignment.BottomCenter,
                shouldExpandToCanvasWidth: true);
        }

        private static void ConfigureLayout(
            RectTransform target,
            MainMenuV3SectionAlignment alignment,
            params RectTransform[] rightAnchoredTargets)
        {
            MainMenuV3SectionLayoutView layout = target.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(ReferenceResolution, alignment, rightAnchoredTargets);
        }

        private static void BuildHeader(Transform root)
        {
            BuildLogo(root);
            BuildVisibleResource(root, "CreditsVisualPanel", 963f, 14f, 281f, 107f, "CREDITS", "24,750", creditsIcon, Amber);
            BuildVisibleResource(root, "CommandVisualPanel", 1251f, 14f, 278f, 107f, "COMMAND", "8,430", commandIcon, Cyan);
            BuildSettingsButton(root);
            BuildResourceCompatibilityScaffold(root);
        }

        private static void BuildLogo(Transform root)
        {
            RectTransform plate = CreateTopLeftRect("HeaderLogoPanel", root, 14f, 13f, 513f, 137f);
            V3GradientGraphic fill = plate.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(20, 31, 35, 252), new Color32(11, 22, 26, 252), new Color32(3, 9, 12, 253), new Color32(6, 13, 16, 253), Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(plate, left: 18f, top: 10f, right: 18f, bottom: 10f);
        }

        private static void BuildVisibleResource(
            Transform root,
            string name,
            float x,
            float y,
            float width,
            float height,
            string label,
            string value,
            Sprite icon,
            Color accent)
        {
            RectTransform panel = CreateTopLeftRect(name, root, x, y, width, height);
            V3GradientGraphic fill = panel.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(19, 30, 34, 252), new Color32(11, 21, 25, 252), new Color32(4, 10, 13, 253), new Color32(7, 14, 17, 253), Border, 3f);
            if (string.Equals(name, "CreditsVisualPanel", StringComparison.Ordinal))
            {
                RectTransform iconRoot = CreateTopLeftRect("Icon", panel, 18f, 18f, 72f, 72f);
                CreateCreditsIcon(iconRoot, accent);
            }
            else
            {
                Image iconImage = CreateImage("Icon", panel, icon, Color.white, false);
                SetTopLeft(iconImage.rectTransform, 17f, 17f, 75f, 75f);
                iconImage.preserveAspect = true;
            }
            TMP_Text labelText = CreateText("Label", panel, label, 25f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(labelText.rectTransform, 101f, 12f, width - 108f, 38f);
            TMP_Text valueText = CreateText("Value", panel, value, 43f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(valueText.rectTransform, 101f, 45f, width - 108f, 55f);
            CreateSolidTopLeft("Accent", panel, 3f, height - 5f, width - 6f, 3f, new Color(accent.r, accent.g, accent.b, 0.55f));
        }

        private static void BuildSettingsButton(Transform root)
        {
            RectTransform rect = CreateTopLeftRect("SettingsButton", root, 1537f, 14f, 118f, 107f);
            V3GradientGraphic fill = rect.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(23, 35, 39, 255), new Color32(14, 26, 30, 255), new Color32(5, 12, 15, 255), new Color32(8, 17, 20, 255), Border, 3f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ButtonColors();
            Image icon = CreateImage("Icon", rect, settingsIcon, TextPrimary, false);
            SetTopLeft(icon.rectTransform, 28f, 23f, 62f, 62f);
            icon.preserveAspect = true;
            UIShellActionButtonView action = rect.gameObject.AddComponent<UIShellActionButtonView>();
            SerializedObject serialized = new(action);
            serialized.FindProperty("actionKind").enumValueIndex = (int)UiActionKind.OpenSettings;
            serialized.FindProperty("payloadId").intValue = 0;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildResourceCompatibilityScaffold(Transform root)
        {
            RectTransform area = CreateRect("HeaderResourceArea", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1380f, 160f), Vector2.zero);
            area.gameObject.SetActive(false);
            BuildResourceCompatibilityPanel(area, "CreditsPanel", -350f, "CREDITS", "24,750", creditsIcon);
            BuildResourceCompatibilityPanel(area, "CommandPanel", 350f, "COMMAND", "8,430", commandIcon);
        }

        private static void BuildResourceCompatibilityPanel(Transform area, string name, float x, string label, string value, Sprite icon)
        {
            RectTransform panel = CreateRect(name, area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(676f, 160f), new Vector2(x, 0f));
            RectTransform frame = CreateRect("Frame", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TMP_Text labelText = CreateText("Label", frame, label, 26f, boldFont, TextAlignmentOptions.BottomLeft, TextPrimary);
            labelText.rectTransform.anchoredPosition = new Vector2(150f, -20f);
            labelText.rectTransform.sizeDelta = new Vector2(330f, 34f);
            TMP_Text valueText = CreateText("Value", frame, value, 54f, boldFont, TextAlignmentOptions.TopLeft, TextPrimary);
            valueText.rectTransform.anchoredPosition = new Vector2(150f, -58f);
            valueText.rectTransform.sizeDelta = new Vector2(330f, 76f);
            Image iconImage = CreateImage("Icon", frame, icon, Color.white, false);
            iconImage.rectTransform.anchoredPosition = new Vector2(-235f, 0f);
            iconImage.rectTransform.sizeDelta = new Vector2(112f, 112f);
            iconImage.preserveAspect = true;
        }

        private static void BuildModeCards(Transform root)
        {
            BuildCampaignCard(root);
            BuildCompactModeCard(root, "Card_Operations", 14f, 504f, 680f, 128f, "OPERATIONS", operationsArt, Green, UIRoute.Operations, ModeIcon.Operations);
            BuildCompactModeCard(root, "Card_Skirmish", 14f, 644f, 680f, 139f, "SKIRMISH", skirmishArt, Red, UIRoute.QuickCustomSetup, ModeIcon.Skirmish);
        }

        private static void BuildCampaignCard(Transform root)
        {
            RectTransform card = CreateTopLeftRect("Card_Campaign", root, 14f, 156f, 680f, 337f);
            Image art = CreateImage("CampaignArt", card, campaignArt, Color.white, false);
            Stretch(art.rectTransform);
            V3GradientGraphic shade = CreateGradient("CampaignReadability", card, new Color(0.02f, 0.01f, 0f, 0.2f), new Color(0.02f, 0.01f, 0f, 0.65f), Color.clear, 0f);
            Stretch(shade.rectTransform);
            V3GradientGraphic frame = CreateGradient("Frame", card, Color.clear, Color.clear, Amber, 3f);
            Stretch(frame.rectTransform);
            RectTransform iconCell = CreateTopLeftRect("IconCell", card, 3f, 3f, 102f, 117f);
            V3GradientGraphic iconFill = iconCell.gameObject.AddComponent<V3GradientGraphic>();
            iconFill.Configure(new Color32(194, 127, 0, 245), new Color32(107, 63, 0, 248), Amber, 3f);
            Image campaignIconImage = CreateImage("CampaignTarget", iconCell, campaignIcon, Color.white, false);
            SetTopLeft(campaignIconImage.rectTransform, 8f, 12f, 86f, 86f);
            campaignIconImage.preserveAspect = true;
            TMP_Text title = CreateText("Title", card, "CAMPAIGN", 53f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 120f, 7f, 535f, 66f);
            TMP_Text subtitle = CreateText("Subtitle", card, "CONTINUE CAMPAIGN  ›", 27f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(subtitle.rectTransform, 122f, 67f, 500f, 43f);
            RectTransform quote = CreateTopLeftRect("StoryQuote", card, 24f, 135f, 255f, 78f);
            V3GradientGraphic quoteFill = quote.gameObject.AddComponent<V3GradientGraphic>();
            quoteFill.Configure(new Color32(255, 255, 252, 255), new Color32(226, 223, 211, 255), new Color32(54, 54, 49, 255), 2f);
            Image quoteTail = CreateSolid("Tail", quote, new Color32(238, 236, 225, 255), new Vector2(16f, 16f), new Vector2(124f, 0f));
            quoteTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            TMP_Text quoteText = CreateText("Text", quote, "PEOPLE ARE COUNTING\nON US. KEEP THEM SAFE.", 19f, boldFont, TextAlignmentOptions.Center, new Color32(22, 24, 23, 255));
            Stretch(quoteText.rectTransform);
            quoteText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            RectTransform warning = CreateTopLeftRect("EmergencyWarning", card, 22f, 263f, 475f, 59f);
            V3GradientGraphic warningFill = warning.gameObject.AddComponent<V3GradientGraphic>();
            warningFill.Configure(new Color32(30, 20, 14, 248), new Color32(8, 9, 9, 252), Red, 3f);
            CreateWarningIcon(CreateTopLeftRect("Icon", warning, 13f, 9f, 45f, 41f), Red);
            TMP_Text warningText = CreateText("Text", warning, "EMERGENCY: CIVILIANS AT RISK", 24f, boldFont, TextAlignmentOptions.MidlineLeft, Red);
            SetTopLeft(warningText.rectTransform, 70f, 5f, 392f, 49f);
            AddRouteHotspot(card, UIRoute.Campaign);
        }

        private static void BuildCompactModeCard(
            Transform root,
            string name,
            float x,
            float y,
            float width,
            float height,
            string title,
            Sprite artSprite,
            Color accent,
            UIRoute route,
            ModeIcon iconKind)
        {
            RectTransform card = CreateTopLeftRect(name, root, x, y, width, height);
            Image art = CreateImage("ThumbnailArt", card, artSprite, Color.white, false);
            Stretch(art.rectTransform);
            V3GradientGraphic tint = CreateGradient("Tint", card, new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.35f), new Color(0f, 0f, 0f, 0.48f), Color.clear, 0f);
            Stretch(tint.rectTransform);
            V3GradientGraphic frame = CreateGradient("Frame", card, Color.clear, Color.clear, accent, 3f);
            Stretch(frame.rectTransform);
            RectTransform iconCell = CreateTopLeftRect("IconCell", card, 3f, 3f, 102f, height - 6f);
            V3GradientGraphic iconFill = iconCell.gameObject.AddComponent<V3GradientGraphic>();
            iconFill.Configure(new Color(accent.r * 0.52f, accent.g * 0.52f, accent.b * 0.52f, 0.96f), new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.98f), accent, 3f);
            if (iconKind == ModeIcon.Operations)
            {
                Image operationsIconImage = CreateImage("OperationsCompass", iconCell, operationsIcon, Color.white, false);
                SetTopLeft(operationsIconImage.rectTransform, 4f, 7f, 94f, 94f);
                operationsIconImage.preserveAspect = true;
                BuildOperationsRoute(card);
            }
            else
            {
                Image skirmishIconImage = CreateImage("SkirmishBlades", iconCell, skirmishIcon, Color.white, false);
                SetTopLeft(skirmishIconImage.rectTransform, 8f, 14f, 86f, 90f);
                skirmishIconImage.preserveAspect = true;
            }
            TMP_Text label = CreateText("Title", card, title, 48f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(label.rectTransform, 120f, 0f, width - 195f, height);
            TMP_Text chevron = CreateText("Chevron", card, "›", 82f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(chevron.rectTransform, width - 66f, 2f, 54f, height - 4f);
            AddRouteHotspot(card, route);
        }

        private static void BuildMiddleHitTargets(Transform root)
        {
            RectTransform hidden = CreateTopLeftRect("DeployCommandButton", root, 824f, 704f, 2f, 2f);
            hidden.gameObject.SetActive(false);
            V3GradientGraphic graphic = hidden.gameObject.AddComponent<V3GradientGraphic>();
            Button button = hidden.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            UIShellRouteButtonView route = hidden.gameObject.AddComponent<UIShellRouteButtonView>();
            route.Configure(UiShellRouteIntent.EnterMatch, UIRoute.Match, false);
        }

        private static void BuildRightRail(Transform root)
        {
            BuildAriaPanel(root);
            BuildCommanderPanel(root);
        }

        private static void BuildAriaPanel(Transform root)
        {
            RectTransform panel = CreateTopLeftRect("AriaPanel", root, 1332f, 129f, 324f, 388f);
            V3GradientGraphic fill = panel.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(2, 18, 28, 252), new Color32(2, 24, 36, 252), new Color32(0, 7, 12, 254), new Color32(1, 12, 18, 254), Cyan, 3f);
            TMP_Text title = CreateText("Title", panel, "ARIA", 44f, boldFont, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(title.rectTransform, 20f, 3f, 160f, 62f);
            Image portrait = CreateImage("Portrait", panel, ariaPortrait, new Color32(112, 224, 255, 255), false);
            portrait.color = Color.white;
            SetTopLeft(portrait.rectTransform, 45f, 39f, 258f, 343f);
            portrait.preserveAspect = true;
            V3GradientGraphic scan = CreateGradient("PortraitScan", panel, new Color(0f, 0.65f, 1f, 0.025f), new Color(0f, 0.17f, 0.28f, 0.14f), Color.clear, 0f);
            SetTopLeft(scan.rectTransform, 42f, 37f, 264f, 347f);
            BuildAriaTelemetry(panel);
        }

        private static void BuildCommanderPanel(Transform root)
        {
            // Keep the panel's target-locked right edge while widening it to give the
            // commander copy and CTA a consistent inset from the right frame.
            RectTransform panel = CreateTopLeftRect("CommanderPanel", root, 1225f, 529f, 430f, 244f);
            V3GradientGraphic fill = panel.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(30, 45, 28, 252), new Color32(18, 30, 21, 252), new Color32(7, 14, 10, 253), new Color32(10, 21, 13, 253), Border, 3f);
            RectTransform emblem = CreateTopLeftRect("RankEmblem", panel, 20f, 22f, 64f, 92f);
            CreateChevron("Rank1", emblem, 0f, 23f, Amber, 31f);
            CreateChevron("Rank2", emblem, 0f, 1f, Amber, 31f);
            CreateChevron("Rank3", emblem, 0f, -21f, Amber, 31f);
            TMP_Text title = CreateText("Title", panel, "FIELD COMMANDER", 31f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 96f, 18f, 315f, 46f);
            TMP_Text subtitle = CreateText("Subtitle", panel, "SELECTED COMMANDER", 22f, boldFont, TextAlignmentOptions.MidlineLeft, Amber);
            SetTopLeft(subtitle.rectTransform, 96f, 61f, 313f, 38f);
            RectTransform change = CreateTopLeftRect("ChangeButton", panel, 24f, 122f, 382f, 95f);
            V3GradientGraphic changeFill = change.gameObject.AddComponent<V3GradientGraphic>();
            changeFill.ConfigureCorners(new Color32(74, 116, 58, 255), new Color32(48, 90, 43, 255), new Color32(27, 62, 27, 255), new Color32(35, 73, 31, 255), new Color32(111, 148, 84, 255), 3f);
            TMP_Text label = CreateText("Label", change, "CHANGE   ›", 44f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(label.rectTransform, 18f, 0f, 346f, 95f);
            AddRouteHotspot(panel, UIRoute.CommanderProfile, "CommanderPanelHotspot");
        }

        private static void BuildFooter(Transform root)
        {
            RectTransform store = CreateTopLeftRect("StoreButton", root, 14f, 795f, 753f, 146f);
            // The two footer actions form one uninterrupted, full-width strip. Their
            // half-width anchors preserve the target proportions while distributing
            // all additional ultra-wide width instead of leaving black side gutters.
            store.anchorMin = new Vector2(0f, 1f);
            store.anchorMax = new Vector2(0.5f, 1f);
            store.pivot = new Vector2(0f, 1f);
            store.anchoredPosition = new Vector2(14f, -795f);
            store.sizeDelta = new Vector2(-83f, 146f);
            V3GradientGraphic storeFill = store.gameObject.AddComponent<V3GradientGraphic>();
            storeFill.ConfigureCorners(new Color32(4, 144, 215, 255), new Color32(3, 112, 183, 255), new Color32(1, 77, 135, 255), new Color32(2, 92, 153, 255), new Color32(0, 138, 216, 255), 3f);
            Image cart = CreateImage("CartIcon", store, storeIcon, Color.white, false);
            SetTopLeft(cart.rectTransform, 109f, 21f, 128f, 105f);
            cart.preserveAspect = true;
            TMP_Text storeText = CreateText("Label", store, "STORE", 59f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(storeText.rectTransform, 264f, 0f, 340f, 146f);
            AddRouteHotspot(store, UIRoute.CommandExchange, "StoreButtonHotspot");

            RectTransform armory = CreateTopLeftRect("OpenArmoryButton", root, 767f, 795f, 890f, 146f);
            armory.anchorMin = new Vector2(0.5f, 1f);
            armory.anchorMax = new Vector2(1f, 1f);
            armory.pivot = new Vector2(0f, 1f);
            armory.anchoredPosition = new Vector2(-69f, -795f);
            armory.sizeDelta = new Vector2(54f, 146f);
            V3GradientGraphic armoryFill = armory.gameObject.AddComponent<V3GradientGraphic>();
            armoryFill.ConfigureCorners(new Color32(31, 65, 119, 255), new Color32(25, 52, 96, 255), new Color32(12, 29, 58, 255), new Color32(17, 38, 72, 255), new Color32(40, 69, 111, 255), 3f);
            Image crate = CreateImage("CrateIcon", armory, armoryIcon, Color.white, false);
            SetTopLeft(crate.rectTransform, 158f, 11f, 154f, 124f);
            crate.preserveAspect = true;
            TMP_Text armoryText = CreateText("Label", armory, "ARMORY", 59f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(armoryText.rectTransform, 327f, 0f, 400f, 146f);
            AddRouteHotspot(armory, UIRoute.Armory, "ArmoryButtonHotspot");
        }

        private static void AddRouteHotspot(RectTransform parent, UIRoute route, string name = "Hotspot")
        {
            RectTransform hotspot = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image hit = hotspot.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            Button button = hotspot.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ButtonColors();
            UIShellRouteButtonView routeButton = hotspot.gameObject.AddComponent<UIShellRouteButtonView>();
            routeButton.Configure(UiShellRouteIntent.OpenMenuRoute, route, true);
        }

        private static ColorBlock ButtonColors()
        {
            return new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f),
                pressedColor = new Color(0.82f, 0.88f, 0.9f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        private static void CreateTargetIcon(Transform root, Color color)
        {
            RectTransform holder = CreateTopLeftRect("Target", root, 20f, 22f, 62f, 62f);
            CreateRing("OuterRing", holder, new Vector2(56f, 56f), Vector2.zero, color, 5f);
            CreateSolid("CrossH", holder, color, new Vector2(62f, 4f), Vector2.zero);
            CreateSolid("CrossV", holder, color, new Vector2(4f, 62f), Vector2.zero);
            CreateRing("CoreRing", holder, new Vector2(20f, 20f), Vector2.zero, color, 4f);
        }

        private static void CreateCreditsIcon(Transform root, Color color)
        {
            CreateRing("OuterRing", root, new Vector2(66f, 66f), Vector2.zero, color, 4f);
            float[] heights = { 14f, 25f, 36f, 29f, 43f };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -20f + i * 10f;
                float y = -18f + heights[i] * 0.5f;
                CreateSolid("Bar" + i, root, color, new Vector2(6f, heights[i]), new Vector2(x, y));
            }
        }

        private static void CreateCompassIcon(Transform root, Color color)
        {
            RectTransform holder = CreateTopLeftRect("Compass", root, 20f, 23f, 62f, 62f);
            CreateRing("OuterRing", holder, new Vector2(57f, 57f), Vector2.zero, color, 4f);
            V3StarGraphic star = CreateRect("Star", holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(47f, 47f), Vector2.zero).gameObject.AddComponent<V3StarGraphic>();
            star.color = color;
            CreateRing("CoreRing", holder, new Vector2(24f, 24f), Vector2.zero, new Color32(26, 92, 55, 255), 7f);
        }

        private static void BuildOperationsRoute(Transform card)
        {
            Vector2[] points =
            {
                new(225f, 94f), new(285f, 84f), new(344f, 101f),
                new(405f, 79f), new(468f, 97f), new(525f, 79f)
            };
            for (int i = 0; i < points.Length - 1; i++)
                CreateLineBetween($"RouteSegment{i}", card, points[i], points[i + 1], Green, 3f);
            for (int i = 0; i < points.Length; i++)
                CreateRouteNode($"RouteNode{i}", card, points[i], i > 0 && i < points.Length - 1);
        }

        private static void CreateRouteNode(string name, Transform parent, Vector2 point, bool checkedNode)
        {
            RectTransform node = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(21f, 21f), new Vector2(point.x, -point.y));
            CreateRing("Ring", node, new Vector2(20f, 20f), Vector2.zero, Green, 4f);
            CreateSolid("Core", node, new Color32(11, 45, 28, 255), new Vector2(9f, 9f), Vector2.zero);
            if (!checkedNode)
                return;
            Image shortStroke = CreateSolid("CheckShort", node, TextPrimary, new Vector2(8f, 3f), new Vector2(-3f, -1f));
            shortStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -44f);
            Image longStroke = CreateSolid("CheckLong", node, TextPrimary, new Vector2(12f, 3f), new Vector2(3f, 1f));
            longStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 44f);
        }

        private static void BuildAriaTelemetry(Transform panel)
        {
            Color telemetry = new(0f, 0.72f, 0.95f, 0.78f);
            float[] leftY = { 73f, 80f, 91f, 106f, 114f, 122f, 143f, 151f, 166f, 181f, 189f, 209f };
            float[] leftWidth = { 31f, 8f, 45f, 37f, 19f, 12f, 42f, 25f, 9f, 34f, 17f, 39f };
            for (int i = 0; i < leftY.Length; i++)
                CreateSolidTopLeft("TelemetryLeft" + i, panel, 18f, leftY[i], leftWidth[i], 2f, telemetry);

            float[] rightWidth = { 9f, 30f, 23f, 31f, 19f, 28f, 30f, 17f, 27f, 12f };
            for (int i = 0; i < rightWidth.Length; i++)
                CreateSolidTopLeft("TelemetryRight" + i, panel, 268f, 57f + i * 9f, rightWidth[i], 2f, telemetry);

            CreateSolidTopLeft("TelemetryRightStem", panel, 303f, 55f, 2f, 101f, new Color(telemetry.r, telemetry.g, telemetry.b, 0.45f));
            for (int i = 0; i < 4; i++)
                CreateSolidTopLeft("TelemetryLowerLine" + i, panel, 18f, 298f + i * 13f, 57f - i * 8f, 2f, telemetry);

            float[] bars = { 18f, 42f, 27f, 55f, 33f, 48f };
            for (int i = 0; i < bars.Length; i++)
                CreateSolidTopLeft("ChartBar" + i, panel, 20f + i * 7f, 285f - bars[i], 4f, bars[i], telemetry);
            CreateSolidTopLeft("ChartBaseline", panel, 18f, 287f, 49f, 2f, telemetry);

            RectTransform reticle = CreateTopLeftRect("TelemetryReticle", panel, 251f, 220f, 58f, 58f);
            CreateRing("OuterRing", reticle, new Vector2(52f, 52f), Vector2.zero, telemetry, 3f);
            CreateRing("InnerRing", reticle, new Vector2(22f, 22f), Vector2.zero, telemetry, 3f);
            CreateSolid("Horizontal", reticle, telemetry, new Vector2(58f, 3f), Vector2.zero);
            CreateSolid("Vertical", reticle, telemetry, new Vector2(3f, 58f), Vector2.zero);
        }

        private static void CreateLineBetween(string name, Transform parent, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 delta = new(b.x - a.x, -(b.y - a.y));
            Vector2 center = (a + b) * 0.5f;
            Image line = CreateImage(name, parent, null, color, false);
            SetRect(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(delta.magnitude, thickness), new Vector2(center.x, -center.y));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static V3RingGraphic CreateRing(string name, Transform parent, Vector2 size, Vector2 position, Color color, float thickness)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            V3RingGraphic ring = rect.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, thickness);
            return ring;
        }

        private static void CreateCrossedBladesIcon(Transform root, Color color)
        {
            RectTransform holder = CreateTopLeftRect("Blades", root, 20f, 29f, 62f, 72f);
            Image left = CreateSolid("Left", holder, color, new Vector2(8f, 70f), Vector2.zero);
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -43f);
            Image right = CreateSolid("Right", holder, color, new Vector2(8f, 70f), Vector2.zero);
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 43f);
            CreateSolid("GuardLeft", holder, color, new Vector2(29f, 6f), new Vector2(-19f, -19f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -43f);
            CreateSolid("GuardRight", holder, color, new Vector2(29f, 6f), new Vector2(19f, -19f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 43f);
        }

        private static void CreateWarningIcon(Transform root, Color color)
        {
            CreateSolid("Stem", root, color, new Vector2(8f, 25f), new Vector2(0f, 5f));
            CreateSolid("Dot", root, color, new Vector2(8f, 8f), new Vector2(0f, -14f));
            Image left = CreateSolid("Left", root, color, new Vector2(5f, 39f), new Vector2(-11f, 0f));
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -29f);
            Image right = CreateSolid("Right", root, color, new Vector2(5f, 39f), new Vector2(11f, 0f));
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 29f);
            CreateSolid("Base", root, color, new Vector2(38f, 5f), new Vector2(0f, -20f));
        }

        private static void CreateCartIcon(Transform root, Color color)
        {
            Image basket = CreateSolid("Basket", root, color, new Vector2(67f, 39f), new Vector2(5f, 5f));
            basket.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            Image handle = CreateSolid("Handle", root, color, new Vector2(8f, 35f), new Vector2(-37f, 29f));
            handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            CreateSolid("WheelLeft", root, color, new Vector2(18f, 18f), new Vector2(-17f, -31f));
            CreateSolid("WheelRight", root, color, new Vector2(18f, 18f), new Vector2(31f, -31f));
        }

        private static void CreateChevron(string name, Transform parent, float centerX, float centerY, Color color, float length)
        {
            Image left = CreateSolid(name + "Left", parent, color, new Vector2(length, 7f), new Vector2(centerX - 10f, centerY));
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            Image right = CreateSolid(name + "Right", parent, color, new Vector2(length, 7f), new Vector2(centerX + 10f, centerY));
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        }

        private static void ConfigureTexture(string path, bool alpha, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Main Menu V3 texture: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static void BuildAtlas(string atlasPath, string atlasName, params string[] texturePaths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(atlasPath));
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            UnityEngine.Object[] existing = SpriteAtlasExtensions.GetPackables(atlas);
            if (existing.Length > 0)
                SpriteAtlasExtensions.Remove(atlas, existing);

            var textures = new List<UnityEngine.Object>(texturePaths.Length);
            for (int i = 0; i < texturePaths.Length; i++)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[i]);
                if (texture == null)
                    throw new FileNotFoundException($"Missing texture for atlas: {texturePaths[i]}");
                textures.Add(texture);
            }
            SpriteAtlasExtensions.Add(atlas, textures.ToArray());
            SpriteAtlasExtensions.SetPackingSettings(atlas, new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            });
            SpriteAtlasExtensions.SetTextureSettings(atlas, new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Bilinear,
                generateMipMaps = false,
                readable = false,
                sRGB = true
            });
            SetAtlasPlatform(atlas, "DefaultTexturePlatform", false, TextureImporterFormat.Automatic);
            SetAtlasPlatform(atlas, "Android", true, TextureImporterFormat.ASTC_6x6);
            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
            atlas.name = atlasName;
            EditorUtility.SetDirty(atlas);
        }

        private static void ValidateAtlas(string atlasPath, params string[] expectedPaths)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
                throw new FileNotFoundException($"Missing V3 atlas: {atlasPath}");

            UnityEngine.Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
            var actualPaths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < packables.Length; i++)
                actualPaths.Add(AssetDatabase.GetAssetPath(packables[i]));
            if (packables.Length != expectedPaths.Length || actualPaths.Count != expectedPaths.Length)
                throw new InvalidOperationException($"V3 atlas {atlasPath} contains duplicate or unexpected packables.");
            for (int i = 0; i < expectedPaths.Length; i++)
            {
                if (!actualPaths.Contains(expectedPaths[i]))
                    throw new InvalidOperationException($"V3 atlas {atlasPath} is missing canonical texture {expectedPaths[i]}.");
            }
        }

        private static void SetAtlasPlatform(SpriteAtlas atlas, string platformName, bool overridden, TextureImporterFormat format)
        {
            SpriteAtlasExtensions.SetPlatformSettings(atlas, new TextureImporterPlatformSettings
            {
                name = platformName,
                overridden = overridden,
                maxTextureSize = 2048,
                format = format,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100
            });
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            commanderScene = AssetDatabase.LoadAssetAtPath<Sprite>(CommanderScenePath);
            campaignArt = AssetDatabase.LoadAssetAtPath<Sprite>(CampaignArtPath);
            operationsArt = AssetDatabase.LoadAssetAtPath<Sprite>(OperationsArtPath);
            skirmishArt = AssetDatabase.LoadAssetAtPath<Sprite>(SkirmishArtPath);
            ariaPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.SharedAriaPortraitPath);
            campaignIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CampaignIconPath);
            operationsIcon = AssetDatabase.LoadAssetAtPath<Sprite>(OperationsIconPath);
            skirmishIcon = AssetDatabase.LoadAssetAtPath<Sprite>(SkirmishIconPath);
            storeIcon = AssetDatabase.LoadAssetAtPath<Sprite>(StoreIconPath);
            armoryIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ArmoryIconPath);
            creditsIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CanonicalUiResourceIconPaths.Credits);
            commandIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CanonicalUiResourceIconPaths.Command);
            settingsIcon = AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.SettingsIconPath);
            if (boldFont == null || mediumFont == null || commanderScene == null || campaignArt == null || operationsArt == null || skirmishArt == null || ariaPortrait == null || campaignIcon == null || operationsIcon == null || skirmishIcon == null || storeIcon == null || armoryIcon == null || creditsIcon == null || commandIcon == null || settingsIcon == null)
                throw new MissingReferenceException("Main Menu V3 is missing a required font or canonical content asset.");
        }

        private static void ValidateRoute(GameObject prefab, string objectName, UIRoute expectedRoute)
        {
            Transform target = FindDeepChild(prefab.transform, objectName);
            UIShellRouteButtonView route = target != null ? target.GetComponent<UIShellRouteButtonView>() : null;
            if (route == null)
                route = target != null ? target.GetComponentInChildren<UIShellRouteButtonView>(true) : null;
            if (route == null || route.Intent != UiShellRouteIntent.OpenMenuRoute || route.Route != expectedRoute || !route.PushHistory)
                throw new InvalidOperationException($"{objectName} has invalid route binding; expected {expectedRoute}.");
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Main Menu prefab for capture: {PrefabPath}");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("MainMenuV3CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(width, height, TextureFormat.RGBA32, false);
            // Screen-space camera canvases derive their dimensions from the camera's
            // active target. Bind the requested target before layout; otherwise QA
            // captures inherit the open Game view size and can crop an unrelated ratio.
            camera.targetTexture = renderTexture;

            GameObject canvasObject = new("MainMenuV3CaptureCanvas", typeof(RectTransform), typeof(Canvas));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localScale = Vector3.one;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            // World-space capture makes the requested RenderTexture dimensions the
            // authoritative canvas dimensions. Screen-space camera canvases inherit
            // the open Editor Game view and can silently capture the wrong ratio.
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            Stretch(instance.transform as RectTransform);
            Canvas.ForceUpdateCanvases();
            // The shell stretches content after component OnEnable. Mirror that runtime
            // ordering in QA captures so every section resolves against the final canvas
            // instead of retaining the pre-mount world position from instantiation.
            MainMenuV3SectionLayoutView[] layouts =
                instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
            for (int i = 0; i < layouts.Length; i++)
                layouts[i].RefreshLayout();
            Canvas.ForceUpdateCanvases();
            WriteLayoutDiagnostic(outputPath + ".layout.txt", canvas, canvasRect, instance, layouts);

            try
            {
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"[MainMenuV3PrefabBuilder] captured={outputPath} size={width}x{height} scene={scene.name}");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void WriteLayoutDiagnostic(
            string path,
            Canvas canvas,
            RectTransform canvasRect,
            GameObject instance,
            MainMenuV3SectionLayoutView[] layouts)
        {
            var report = new StringBuilder();
            report.AppendLine($"canvas scaleFactor={canvas.scaleFactor} rect={canvasRect.rect} size={canvasRect.rect.size}");
            AppendRect(report, "instance", instance.transform as RectTransform);
            for (int i = 0; i < layouts.Length; i++)
            {
                MainMenuV3SectionLayoutView layout = layouts[i];
                report.AppendLine($"layout name={layout.name} alignment={layout.Alignment} appliedScale={layout.LastAppliedScale} extraWidth={layout.LastAppliedExtraWidth}");
                AppendRect(report, "  section", layout.transform as RectTransform);
                if (layout.transform.childCount > 0)
                    AppendRect(report, "  firstChild", layout.transform.GetChild(0) as RectTransform);
            }
            File.WriteAllText(path, report.ToString());
        }

        private static void AppendRect(StringBuilder report, string label, RectTransform rect)
        {
            if (rect == null)
                return;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            report.AppendLine($"{label} rect={rect.rect} anchor=({rect.anchorMin},{rect.anchorMax}) pivot={rect.pivot} anchored={rect.anchoredPosition} local={rect.localPosition} world={rect.position} scale={rect.localScale} corners=({corners[0]}..{corners[2]})");
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            return V3UiPrefabFactory.CreateRect(name, parent, min, max, size, position);
        }

        private static RectTransform CreateTopLeftRect(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            return V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);
        }

        private static V3GradientGraphic CreateGradient(string name, Transform parent, Color top, Color bottom, Color border, float width)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), Vector2.zero);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, width);
            return gradient;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 60f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, Vector2 size, Vector2 position)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            return image;
        }

        private static Image CreateSolidTopLeft(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect != null)
                SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root != null ? root.Find(path) : null;
            if (result == null)
                throw new MissingReferenceException($"Main Menu V3 is missing '{path}'.");
            return result;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private enum ModeIcon
        {
            Operations,
            Skirmish
        }
    }
}
