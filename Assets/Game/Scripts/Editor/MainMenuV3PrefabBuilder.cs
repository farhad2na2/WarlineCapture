using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Game.Configs;
using Game.Composition;
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
        public const string PanelArtDirectory = "Assets/Game/Art/UI/MainMenuPanels/V02/";
        public const string AriaBackdropPath = PanelArtDirectory + "aria-command-room-v02.png";
        public const string CompletionArtDirectory = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/CampaignCompletion/";
        private static readonly string[] CompletionIds = { "rebuilding", "supply", "watch" };
        public static string CommanderPanelPath(int portraitIndex) => PanelArtDirectory + "commander-" + portraitIndex + "-scene-v02.png";
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
        private const float RightColumnX = 1296f;
        private const float RightColumnWidth = 360f;
        private const float PanelGap = 12f;
        private const float SettingsWidth = 118f;
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
            ConfigureTexture(AriaBackdropPath, false, 2048);
            for (int i = 0; i < 6; i++) ConfigureTexture(CommanderPanelPath(i), false, 2048);
            foreach (string id in CompletionIds) ConfigureTexture(CompletionArtDirectory + "home-aftermath-" + id + "-v01.png", false, 2048);
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

            Image comicBackdrop = BuildBackground(backgroundSection);
            BuildHeader(headerSection);
            BuildModeCards(leftSection, comicBackdrop);
            BuildMiddleHitTargets(middleSection);
            BuildRightRail(rightSection);
            BuildFooter(footerSection);
            ConfigureRuntimeLayouts(headerSection, leftSection, middleSection, rightSection, footerSection);

            MissionUiSerializedBindingsAuthoring.Apply(root);
            MenuAccountHeaderAuthoring.Apply(root);
            MenuUiApprovedAuthoring.Apply(root, "main-menu");
            MilitaryUiMaterialAuthoring.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MainMenuV3PrefabBuilder] result=Passed v3=True Campaign-led home with native portraits, informational Credits, and independent mode routes.");
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

            Require(prefab.transform, "HeaderContent/CreditsVisualPanel/Value");
            Require(prefab.transform, "MenuBackgroundContent/ComicBackdrop");
            Require(prefab.transform, "MenuBackgroundContent/HeaderShade");
            if (prefab.transform.Find("HeaderContent/CommandVisualPanel") != null || prefab.transform.Find("HeaderContent/HeaderResourceArea") != null)
                throw new InvalidOperationException("Retired Command/header purchase routes must be absent.");
            Require(prefab.transform, "LeftContent/Card_Campaign/ContinueButton");
            var archive=Require(prefab.transform,"LeftContent/Card_Campaign").GetComponent<MainMenuStoryArchiveView>();
            if(archive==null)throw new MissingComponentException("Completed Campaign requires earned-story playback.");
            var archiveData=new SerializedObject(archive);
            var supplemental=archiveData.FindProperty("supplementalSequences");
            foreach(string stage in new[]{"brief","comms","debrief"})
            {
                bool found=false;
                for(int i=0;i<supplemental.arraySize;i++)
                    if(supplemental.GetArrayElementAtIndex(i).objectReferenceValue is NarrativeSequenceConfig sequence && sequence.SequenceId=="seq.ch01.m01."+stage)found=true;
                if(!found)throw new MissingReferenceException("Story Archive must include M01 "+stage);
            }
            Require(prefab.transform, "LeftContent/Card_Operations/Hotspot");
            Require(prefab.transform, "LeftContent/Card_Skirmish/Hotspot");
            Require(prefab.transform, "RightContent/CommanderPanel/ViewCommanderButton/CommanderPanelHotspot");
            Require(prefab.transform, "FooterContent/StoreButton");
            Require(prefab.transform, "FooterContent/OpenArmoryButton");

            Transform commanderTransform=Require(prefab.transform,"RightContent/CommanderPanel/CommanderSceneVariant");
            var commanderView=commanderTransform.GetComponent<MainMenuCommanderVariantView>();
            if(commanderView == null || commanderView.Variants.Length != 6) throw new MissingReferenceException("Commander must bind all six saved identity scenes.");
            for (int i = 0; i < 6; i++)
                if (commanderView.Variants[i].CommanderId != i.ToString() || AssetDatabase.GetAssetPath(commanderView.Variants[i].Sprite) != CommanderPanelPath(i))
                    throw new InvalidOperationException("Commander scene must match its canonical saved portrait index: " + i);
            var commanderRect = commanderTransform.GetComponent<RectTransform>();
            if (commanderRect.anchorMin != Vector2.zero || commanderRect.anchorMax != Vector2.one || commanderRect.sizeDelta != Vector2.zero)
                throw new InvalidOperationException("Commander illustration must cover the full panel.");
            var backdrop = Require(prefab.transform,"RightContent/AriaPanel/SceneBackground").GetComponent<Image>();
            if (AssetDatabase.GetAssetPath(backdrop.sprite) != AriaBackdropPath)
                throw new InvalidOperationException("ARIA must have the approved full-panel command room.");
            var aria=Require(prefab.transform,"RightContent/AriaPanel/Portrait").GetComponent<Image>();
            if(AssetDatabase.GetAssetPath(aria.sprite)!=V3UiFoundationBuilder.SharedAriaPortraitPath || !aria.preserveAspect)
                throw new InvalidOperationException("ARIA must retain her exact native portrait and proportions.");

            MainMenuV3SectionLayoutView[] layouts = prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
            if (layouts.Length < 6)
                throw new MissingComponentException("Main Menu V3 must map every authored reference section into the live shell canvas.");

            ValidateAtlas(SceneAtlasPath, CampaignArtPath, OperationsArtPath, SkirmishArtPath);
            ValidateAtlas(AriaAtlasPath, V3UiFoundationBuilder.SharedAriaPortraitPath);
            ValidateAtlas(MainMenuIconAtlasPath, CampaignIconPath, OperationsIconPath, SkirmishIconPath, StoreIconPath, ArmoryIconPath);

            Transform settings = Require(prefab.transform, "HeaderContent/SettingsButton");
            UIShellActionButtonView settingsAction = settings.GetComponent<UIShellActionButtonView>();
            if (settingsAction == null || settingsAction.ActionKind != UiActionKind.OpenSettings || settings.GetComponent<UIShellRouteButtonView>() != null)
                throw new InvalidOperationException("Main Menu Settings must enqueue OpenSettings, not route to the legacy Settings screen.");

            if (Require(prefab.transform,"LeftContent/Card_Campaign").GetComponent<MainMenuCampaignCardView>() == null)
                throw new MissingComponentException("Campaign must share its target with Continue.");
            ValidateRoute(prefab, "Card_Operations", UIRoute.Operations);
            ValidateRoute(prefab, "Card_Skirmish", UIRoute.QuickCustomSetup);
            ValidateRoute(prefab, "CommanderPanelHotspot", UIRoute.CommanderProfile);
            ValidateRoute(prefab, "StoreButton", UIRoute.CommandExchange);
            ValidateRoute(prefab, "OpenArmoryButton", UIRoute.Armory);

            HashSet<string> allowedRasterPaths = new(StringComparer.Ordinal)
            {
                FirstLaunchNarrativeDialogueAssetImporter.CommanderPortraitSheetPath,
                CampaignArtPath,
                OperationsArtPath,
                SkirmishArtPath,
                V3UiFoundationBuilder.SharedAriaPortraitPath,
                AriaBackdropPath,
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
            for (int i = 0; i < 6; i++) allowedRasterPaths.Add(CommanderPanelPath(i));
            foreach (string id in CompletionIds) allowedRasterPaths.Add(CompletionArtDirectory + "home-aftermath-" + id + "-v01.png");
            foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(image.sprite);
                if (!allowedRasterPaths.Contains(path) && !path.StartsWith("Assets/Game/Art/Narrative/",StringComparison.Ordinal))
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

        public static void CaptureComicBackdropQa()
        {
            Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>(CompletionArtDirectory + "home-aftermath-rebuilding-v01.png");
            if (plate == null) throw new FileNotFoundException("Missing Campaign completion comic for backdrop QA.");
            foreach (int width in new[] { 1920, 2400 })
            {
                CaptureConfigured($"/private/tmp/warline-main-menu-comic-backdrop-{width}.png", width, 1080, instance =>
                {
                    Image backdrop = Require(instance.transform, "MenuBackgroundContent/ComicBackdrop").GetComponent<Image>();
                    Image card = Require(instance.transform, "LeftContent/Card_Campaign/CampaignArt").GetComponent<Image>();
                    backdrop.sprite = plate;
                    backdrop.enabled = true;
                    card.enabled = false;
                });
            }
            Debug.Log("[MainMenuV3PrefabBuilder] comicBackdropQa=Passed widths=1920,2400");
        }

        public static void BuildAndCaptureComicBackdropQa()
        {
            Build();
            CaptureComicBackdropQa();
        }

        [MenuItem("Game/UI/V3/Capture Main Menu Persian QA")]
        public static void CapturePersianQa()
        {
            GameLocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(
                V3UiLocalizationCatalogBuilder.CatalogPath);
            if (catalog == null)
                throw new FileNotFoundException(
                    $"Missing localization catalog: {V3UiLocalizationCatalogBuilder.CatalogPath}");

            string previousLocale = GameLocalization.CurrentLocaleCode;
            try
            {
                GameLocalization.Initialize(
                    catalog,
                    GameLocalization.PersianLocaleCode,
                    persist: false);
                Capture("/private/tmp/warline-main-menu-v3-fa-16x9.png", 1920, 1080);
                Capture("/private/tmp/warline-main-menu-v3-fa-4800x2160.png", 4800, 2160);
            }
            finally
            {
                GameLocalization.Initialize(catalog, previousLocale, persist: false);
            }

            Debug.Log(
                "[MainMenuV3PrefabBuilder] persianQa=Passed " +
                "captures=/private/tmp/warline-main-menu-v3-fa-16x9.png," +
                "/private/tmp/warline-main-menu-v3-fa-4800x2160.png");
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
                         FindObjectsInactive.Exclude))
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
            object currentGroupType = sizesType.GetProperty("currentGroupType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(sizes);
            if (currentGroupType == null)
                throw new MissingMemberException("Unity active Game View size group is unavailable.");
            object activeGroup = getGroup?.Invoke(sizes, new[] { currentGroupType });
            if (activeGroup == null)
                throw new MissingMemberException("Unity active Game View size group is unavailable.");

            MethodInfo getTotalCount = activeGroup.GetType().GetMethod(
                "GetTotalCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getGameViewSize = activeGroup.GetType().GetMethod(
                "GetGameViewSize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int count = getTotalCount != null ? (int)getTotalCount.Invoke(activeGroup, null) : 0;
            int matchingIndex = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getGameViewSize?.Invoke(activeGroup, new object[] { i });
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
                    candidateWidth == width && candidateHeight == height &&
                    size.GetType().GetProperty("sizeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.GetValue(size)?.ToString() == "FixedResolution")
                {
                    // Custom fixed-resolution presets are listed after Unity's
                    // built-in aspect entries (for example "Landscape"). Keep
                    // the final exact match so runtime QA uses real pixels.
                    matchingIndex = i;
                }
            }

            if (matchingIndex < 0)
            {
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeKind = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                if (sizeType == null || sizeKind == null)
                    throw new MissingMemberException("Unity fixed Game View size API is unavailable.");
                object size = Activator.CreateInstance(sizeType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { Enum.Parse(sizeKind, "FixedResolution"), (object)width, height, $"Warline QA {width}x{height}" }, null);
                MethodInfo add = activeGroup.GetType().GetMethod("AddCustomSize",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (add == null) throw new MissingMemberException("Unity custom Game View sizes are unavailable.");
                add.Invoke(activeGroup, new[] { size });
                matchingIndex = count;
            }

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selectedSize = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedSize == null)
                throw new MissingMemberException("Unity Game View selectedSizeIndex is unavailable.");

            // Player reviews must exclude Editor-only outlines, which can remain
            // visible over Home after leaving the match shell.
            PropertyInfo drawGizmos = gameViewType.GetProperty("drawGizmos",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (drawGizmos == null)
                throw new MissingMemberException("Unity Game View drawGizmos is unavailable.");
            drawGizmos.SetValue(gameView, false);

            selectedSize.SetValue(gameView, matchingIndex);
            MethodInfo selectionCallback = gameViewType.GetMethod("SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(object) }, null);
            selectionCallback?.Invoke(gameView, new object[] { matchingIndex, null });
            gameView.Focus();
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

        private static Image BuildBackground(Transform root)
        {
            Image background = CreateImage("HomeBackground", root, null, new Color32(3, 10, 14, 255), false);
            Stretch(background.rectTransform);
            Image comic = CreateImage("ComicBackdrop", root, null, Color.white, false);
            Stretch(comic.rectTransform);
            comic.rectTransform.pivot = new Vector2(0.5f, 1f);
            var cover = comic.gameObject.AddComponent<AspectRatioFitter>();
            cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cover.aspectRatio = 16f / 9f;
            Image headerShade = CreateImage("HeaderShade", root, null, new Color(0.01f, 0.025f, 0.03f, 0.72f), false);
            headerShade.rectTransform.anchorMin = new Vector2(0f, 1f);
            headerShade.rectTransform.anchorMax = Vector2.one;
            headerShade.rectTransform.pivot = new Vector2(0.5f, 1f);
            headerShade.rectTransform.anchoredPosition = Vector2.zero;
            headerShade.rectTransform.sizeDelta = new Vector2(0f, 118f);
            RectTransform reference = CreateTopLeftRect("BackgroundChromeReference", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            ConfigureLayout(reference, MainMenuV3SectionAlignment.TopLeft);
            return comic;
        }


        private static void ConfigureRuntimeLayouts(RectTransform header, RectTransform left, RectTransform middle, RectTransform right, RectTransform footer)
        {
            ConfigureLayout(header, MainMenuV3SectionAlignment.TopLeft, header.Find("CreditsVisualPanel") as RectTransform, header.Find("SettingsButton") as RectTransform);
            var layout = left.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.TopLeft, horizontalTargets: new[]
            {
                new MainMenuV3HorizontalResponsiveTarget(left.Find("Card_Campaign") as RectTransform, 0f, 1f),
                new MainMenuV3HorizontalResponsiveTarget(left.Find("Card_Operations") as RectTransform, 0f, .5f),
                new MainMenuV3HorizontalResponsiveTarget(left.Find("Card_Skirmish") as RectTransform, .5f, .5f)
            });
            ConfigureLayout(middle, MainMenuV3SectionAlignment.Center);
            ConfigureLayout(right, MainMenuV3SectionAlignment.TopRight);
            ConfigureLayout(footer, MainMenuV3SectionAlignment.TopRight);
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
            BuildVisibleResource(root, "CreditsVisualPanel", RightColumnX, 10f, RightColumnWidth, 86f, "CREDITS", "—", creditsIcon, Amber);
            var value = root.Find("CreditsVisualPanel/Value").GetComponent<TMP_Text>();
            value.enableAutoSizing = true; value.fontSizeMin = 24; value.fontSizeMax = 40;
            root.gameObject.AddComponent<MainMenuAccountHeaderView>().Configure(value);
            BuildSettingsButton(root);
        }


        private static void BuildLogo(Transform root)
        {
            RectTransform plate = CreateTopLeftRect("HeaderLogoPanel", root, 14f, 10f, 410f, 86f);
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
                RectTransform iconRoot = CreateTopLeftRect("Icon", panel, 14f, 11f, 64f, 64f);
                CreateCreditsIcon(iconRoot, accent);
            }
            else
            {
                Image iconImage = CreateImage("Icon", panel, icon, Color.white, false);
                SetTopLeft(iconImage.rectTransform, 16f, 10f, 66f, 66f);
                iconImage.preserveAspect = true;
            }
            TMP_Text labelText = CreateText("Label", panel, label, 25f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(labelText.rectTransform, 90f, 4f, width - 102f, 30f);
            TMP_Text valueText = CreateText("Value", panel, value, 43f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(valueText.rectTransform, 90f, 31f, width - 102f, 51f);
            CreateSolidTopLeft("Accent", panel, 3f, height - 5f, width - 6f, 3f, new Color(accent.r, accent.g, accent.b, 0.55f));
        }

        private static void BuildSettingsButton(Transform root)
        {
            RectTransform rect = CreateTopLeftRect("SettingsButton", root, RightColumnX - PanelGap - SettingsWidth, 10f, SettingsWidth, 86f);
            V3GradientGraphic fill = rect.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(23, 35, 39, 255), new Color32(14, 26, 30, 255), new Color32(5, 12, 15, 255), new Color32(8, 17, 20, 255), Border, 3f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ButtonColors();
            Image icon = CreateImage("Icon", rect, settingsIcon, TextPrimary, false);
            SetTopLeft(icon.rectTransform, 28f, 12f, 62f, 62f);
            icon.preserveAspect = true;
            UIShellActionButtonView action = rect.gameObject.AddComponent<UIShellActionButtonView>();
            SerializedObject serialized = new(action);
            serialized.FindProperty("actionKind").enumValueIndex = (int)UiActionKind.OpenSettings;
            serialized.FindProperty("payloadId").intValue = 0;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildModeCards(Transform root, Image comicBackdrop)
        {
            BuildCampaignCard(root, comicBackdrop);
            BuildCompactModeCard(root, "Card_Operations", 14f, 705f, 628f, 216f, "OPERATIONS", operationsArt, Green, UIRoute.Operations, ModeIcon.Operations);
            BuildCompactModeCard(root, "Card_Skirmish", 654f, 705f, 628f, 216f, "SKIRMISH", skirmishArt, Red, UIRoute.QuickCustomSetup, ModeIcon.Skirmish);
        }


        private static void BuildCampaignCard(Transform root, Image comicBackdrop)
        {
            RectTransform card = CreateTopLeftRect("Card_Campaign", root, 14f, 108f, 1268f, 585f);
            Image art = CreateImage("CampaignArt", card, null, Color.white, false);
            Stretch(art.rectTransform);
            art.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Preserve scene proportions; crop within the hero, never stretch faces.
            var mask = card.gameObject.AddComponent<RectMask2D>();
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            V3GradientGraphic shade = CreateGradient("CampaignReadability", card, new Color(0,0,0,.05f), new Color(0,0,0,.95f), Border, 2f);
            Stretch(shade.rectTransform);
            TMP_Text label = CreateText("CampaignLabel", card, "CAMPAIGN", 30f, boldFont, TextAlignmentOptions.MidlineLeft, Amber);
            SetTopLeft(label.rectTransform, 32, 276, 1170, 42);
            ExpandTextWithParent(label.rectTransform, 1268);
            TMP_Text chapter = CreateText("Chapter", card, "", 26f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(chapter.rectTransform, 32, 319, 1170, 40);
            ExpandTextWithParent(chapter.rectTransform, 1268);
            TMP_Text title = CreateText("Title", card, "CAMPAIGN", 52f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 32, 357, 1170, 68);
            ExpandTextWithParent(title.rectTransform, 1268);
            title.enableAutoSizing=true; title.fontSizeMin=32; title.fontSizeMax=52;
            title.textWrappingMode=TextWrappingModes.Normal;
            TMP_Text purpose = CreateText("Purpose", card, "Review your Campaign missions.", 27f, mediumFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(purpose.rectTransform, 32, 426, 1170, 54);
            ExpandTextWithParent(purpose.rectTransform, 1268);
            purpose.textWrappingMode=TextWrappingModes.Normal; purpose.enableAutoSizing=true; purpose.fontSizeMin=20; purpose.fontSizeMax=27;
            RectTransform action = CreateTopLeftRect("ContinueButton", card, 32, 487, 650, 82);
            var fill=action.gameObject.AddComponent<V3GradientGraphic>();
            fill.Configure(new Color32(46,143,39,255), new Color32(9,65,34,255), Green, 3f);
            Button button=action.gameObject.AddComponent<Button>(); button.targetGraphic=fill; button.colors=ButtonColors();
            TMP_Text actionLabel=CreateText("Label", action, "CONTINUE CAMPAIGN   ›", 36, boldFont, TextAlignmentOptions.Center, TextPrimary);
            Stretch(actionLabel.rectTransform); actionLabel.enableAutoSizing=true; actionLabel.fontSizeMin=24; actionLabel.fontSizeMax=36;
            BindCampaignPlates(card.gameObject, art, comicBackdrop);
            card.GetComponent<MainMenuCampaignCardView>().Configure(title,chapter,purpose,actionLabel,button);
            RectTransform archive=CreateTopLeftRect("StoryArchiveButton",card,710,487,360,82);
            var archiveFill=archive.gameObject.AddComponent<V3GradientGraphic>();
            archiveFill.Configure(new Color32(6,105,172,255),new Color32(4,38,89,255),Cyan,2);
            var archiveButton=archive.gameObject.AddComponent<Button>(); archiveButton.targetGraphic=archiveFill; archiveButton.colors=ButtonColors();
            TMP_Text archiveLabel=CreateText("Label",archive,"STORY ARCHIVE",30,boldFont,TextAlignmentOptions.Center,TextPrimary); Stretch(archiveLabel.rectTransform);
            archiveLabel.enableAutoSizing=true; archiveLabel.fontSizeMin=22; archiveLabel.fontSizeMax=30;
            card.gameObject.AddComponent<MainMenuStoryArchiveView>().Configure(archiveButton,mediumFont,
                AssetDatabase.LoadAllAssetsAtPath(M01FirstContactNarrativeConfigBuilder.NarrativePath)
                    .OfType<NarrativeSequenceConfig>().ToArray());
            var data=new SerializedObject(card.GetComponent<MainMenuCampaignCardView>());
            data.FindProperty("archiveButton").objectReferenceValue=archiveButton;
            data.FindProperty("epilogue").objectReferenceValue=LoadPlate("Assets/Game/Resources/FutureMissionComics/Bookends/Campaign_Epilogue.png", null);
            var scenes=data.FindProperty("aftermathScenes"); scenes.arraySize=3;
            string[] captions={"Secured districts are rebuilding. Your command made the difference.","Supplies are reaching the people you protected.","Your team stands watch over the districts you secured."};
            for(int i=0;i<3;i++)
            {
                var scene=scenes.GetArrayElementAtIndex(i);
                scene.FindPropertyRelative("plate").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(CompletionArtDirectory+"home-aftermath-"+CompletionIds[i]+"-v01.png");
                scene.FindPropertyRelative("captionKey").stringValue="ui.home.aftermath."+CompletionIds[i];
                scene.FindPropertyRelative("captionFallback").stringValue=captions[i];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            archive.gameObject.SetActive(false);
        }


        private static void BindCampaignPlates(GameObject card, Image art, Image comicBackdrop)
        {
            MainMenuCampaignCardView view = card.GetComponent<MainMenuCampaignCardView>() ?? card.AddComponent<MainMenuCampaignCardView>();
            SerializedObject data = new(view);
            data.FindProperty("art").objectReferenceValue = art;
            data.FindProperty("comicBackdrop").objectReferenceValue = comicBackdrop;
            (string id, string path, string sprite)[] entries =
            {
                ("saga.ch01.m01.first_contact", "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P15.png", null),
                ("saga.ch01.m02.establish_base", "Assets/Game/Art/Narrative/M02EstablishBase/Final/M02-P01-Brief.png", null),
                ("saga.ch01.m03.radar_warning", "Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-B01.png", "M03-B01-16x9"),
                ("saga.ch01.m04.airlift", "Assets/Game/Art/Narrative/M04Airlift/Final/M04-B01.png", "M04-B01-16x9"),
                ("saga.ch01.m05.breach_assault", "Assets/Game/Art/Narrative/M05BreachAssault/Final/M05-B01.png", "M05-B01-16x9"),
                ("saga.ch02.m01.gridlock", "Assets/Game/Art/Narrative/CH02M01Gridlock/Final/CH02-B01.png", "CH02-B01-16x9"),
                ("saga.ch02.m02.supply_line", "Assets/Game/Art/Narrative/CH02M02SupplyLine/SupplyChain.png", "SupplyChain-16x9"),
                ("saga.ch02.m03.market_lifeline", "Assets/Game/Art/Narrative/CH02M03MarketLifeline/OldMarket.png", "OldMarket-16x9"),
                ("saga.ch02.m04.power_relay", "Assets/Game/Art/Narrative/CH02M04PowerRelay/BriefLinaShelter.png", "BriefLinaShelter-16x9"),
                ("saga.ch02.m05.route_reopened", "Assets/Game/Art/Narrative/CH02M05RouteReopened/BriefRoutingArchive.png", "BriefRoutingArchive-16x9"),
                ("saga.ch03.m01.signal_trace", "Assets/Game/Art/Narrative/CH03M01SignalTrace/BriefThreeSignals.png", "BriefThreeSignals-16x9"),
                ("saga.ch03.m02.safehouse_sweep", "Assets/Game/Art/Narrative/CH03M02SafehouseSweep/BriefVerifiedNode.png", "BriefVerifiedNode-16x9"),
                ("saga.ch03.m03.false_front", "Assets/Game/Art/Narrative/CH03M03FalseFront/BriefEvacuationReport.png", "BriefEvacuationReport-16x9"),
                ("saga.ch04.m02.steel_push", "Assets/Game/Resources/FutureMissionComics/CH04M02_SteelPush.png", "CH04M02_SteelPush-a-16x9")
            };
            SerializedProperty plates = data.FindProperty("plates");
            plates.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = plates.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("missionId").stringValue = entries[i].id;
                element.FindPropertyRelative("plate").objectReferenceValue = LoadPlate(entries[i].path, entries[i].sprite);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadPlate(string path, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            return null;
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

            }
            else
            {
                Image skirmishIconImage = CreateImage("SkirmishBlades", iconCell, skirmishIcon, Color.white, false);
                SetTopLeft(skirmishIconImage.rectTransform, 8f, 14f, 86f, 90f);
                skirmishIconImage.preserveAspect = true;
            }
            TMP_Text label = CreateText("Title", card, title, 48f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(label.rectTransform, 120f, 18f, width - 180f, 70f);
            ExpandTextWithParent(label.rectTransform,width);
            TMP_Text chevron = CreateText("Chevron", card, "›", 82f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(chevron.rectTransform, width - 66f, 2f, 54f, height - 4f);
            chevron.rectTransform.anchorMin=chevron.rectTransform.anchorMax=new Vector2(1,1);
            chevron.rectTransform.pivot=new Vector2(1,1); chevron.rectTransform.anchoredPosition=new Vector2(-12,-2);
            TMP_Text subtitle = CreateText("Subtitle", card, iconKind == ModeIcon.Operations ? "Multi-mission strategic operations" : "Standalone tactical battles", 25f, mediumFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(subtitle.rectTransform, 120, 95, width-195, 90);
            ExpandTextWithParent(subtitle.rectTransform,width);
            subtitle.textWrappingMode=TextWrappingModes.Normal; subtitle.enableAutoSizing=true; subtitle.fontSizeMin=20; subtitle.fontSizeMax=25;
            label.enableAutoSizing=true; label.fontSizeMin=32; label.fontSizeMax=48;
            AddRouteHotspot(card, route);
            card.gameObject.AddComponent<MainMenuDisclosureView>().Configure(0x1fu);
        }

        private static void BuildMiddleHitTargets(Transform root) { }


        private static void BuildRightRail(Transform root)
        {
            BuildAriaPanel(root);
            BuildCommanderPanel(root);
        }

        private static void BuildAriaPanel(Transform root)
        {
            RectTransform panel = CreateTopLeftRect("AriaPanel", root, RightColumnX, 408f, RightColumnWidth, 285f);
            V3GradientGraphic fill = panel.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(new Color32(2, 18, 28, 252), new Color32(2, 24, 36, 252), new Color32(0, 7, 12, 254), new Color32(1, 12, 18, 254), Cyan, 3f);
            Image backdrop = CreateImage("SceneBackground", panel, AssetDatabase.LoadAssetAtPath<Sprite>(AriaBackdropPath), Color.white, false);
            Stretch(backdrop.rectTransform);
            Image portrait = CreateImage("Portrait", panel, ariaPortrait, Color.white, false);
            SetTopLeft(portrait.rectTransform, 126f, 12f, 230f, 270f);
            portrait.preserveAspect = true;
            var shade = CreateTopLeftRect("CopyShade", panel, 0, 0, 180, 285).gameObject.AddComponent<V3GradientGraphic>();
            shade.ConfigureCorners(new Color(0, .035f, .055f, .95f), Color.clear, new Color(0, .035f, .055f, .95f), Color.clear, Color.clear, 0);
            TMP_Text title = CreateText("Title", panel, "ARIA", 44f, boldFont, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(title.rectTransform, 20f, 3f, 110f, 62f);
            TMP_Text description=CreateText("Description", panel, "Tactical assistant", 23, mediumFont, TextAlignmentOptions.TopLeft, Cyan);
            SetTopLeft(description.rectTransform, 20, 66, 98, 135); description.textWrappingMode=TextWrappingModes.Normal;
            Stretch(CreateGradient("Frame", panel, Color.clear, Color.clear, Cyan, 3).rectTransform);
        }

        private static void BuildCommanderPanel(Transform root)
        {
            RectTransform panel=CreateTopLeftRect("CommanderPanel", root, RightColumnX,108,RightColumnWidth,288);
            var fill=panel.gameObject.AddComponent<V3GradientGraphic>();
            fill.Configure(GraphiteTop,GraphiteBottom,Border,3);
            Image portrait=CreateImage("CommanderSceneVariant",panel,null,Color.white,false);
            Stretch(portrait.rectTransform);
            var variants=new List<MainMenuCommanderVariantView.CommanderVariant>();
            for(int i=0;i<6;i++) variants.Add(new MainMenuCommanderVariantView.CommanderVariant(i.ToString(),AssetDatabase.LoadAssetAtPath<Sprite>(CommanderPanelPath(i))));
            var shade=CreateTopLeftRect("IdentityShade",panel,0,0,184,288).gameObject.AddComponent<V3GradientGraphic>();
            shade.ConfigureCorners(new Color(0,0,0,.82f),Color.clear,new Color(0,0,0,.9f),Color.clear,Color.clear,0);
            Stretch(CreateGradient("HeadingShade",panel,new Color(0,0,0,.65f),Color.clear,Color.clear,0).rectTransform);
            TMP_Text heading=CreateText("Title",panel,"COMMANDER",32,boldFont,TextAlignmentOptions.MidlineLeft,TextPrimary);
            SetTopLeft(heading.rectTransform,18,8,324,48);
            TMP_Text name=CreateText("IdentityName",panel,"Commander",23,mediumFont,TextAlignmentOptions.MidlineLeft,TextPrimary);
            SetTopLeft(name.rectTransform,18,56,140,140); name.textWrappingMode=TextWrappingModes.Normal;
            name.enableAutoSizing=true; name.fontSizeMin=16; name.fontSizeMax=23;
            var view=portrait.gameObject.AddComponent<MainMenuCommanderVariantView>();
            view.Configure(portrait,variants.ToArray(),"0"); view.ConfigureIdentity(name);
            RectTransform action=CreateTopLeftRect("ViewCommanderButton",panel,12,206,336,80);
            var actionFill=action.gameObject.AddComponent<V3GradientGraphic>(); actionFill.Configure(new Color32(46,143,39,255),new Color32(9,65,34,255),Green,2);
            TMP_Text actionText=CreateText("Label",action,"VIEW COMMANDER   ›",27,boldFont,TextAlignmentOptions.Center,TextPrimary); Stretch(actionText.rectTransform);
            actionText.enableAutoSizing=true; actionText.fontSizeMin=20; actionText.fontSizeMax=27;
            AddRouteHotspot(action,UIRoute.CommanderProfile,"CommanderPanelHotspot");
            Stretch(CreateGradient("Frame",panel,Color.clear,Color.clear,Border,3).rectTransform);
        }


        private static void BuildFooter(Transform root)
        {
            BuildSecondaryButton(root,"StoreButton",705,"STORE",storeIcon,UIRoute.CommandExchange);
            BuildSecondaryButton(root,"OpenArmoryButton",821,"ARMORY",armoryIcon,UIRoute.Armory);
        }
        private static void BuildSecondaryButton(Transform root,string name,float y,string title,Sprite iconSprite,UIRoute route)
        {
            RectTransform rect=CreateTopLeftRect(name,root,RightColumnX,y,RightColumnWidth,100);
            var fill=rect.gameObject.AddComponent<V3GradientGraphic>(); fill.Configure(new Color32(6,105,172,255),new Color32(4,38,89,255),Cyan,2);
            RectTransform icon=CreateTopLeftRect("Icon",rect,14,10,82,80);
            if(route == UIRoute.Armory) BuildArmoryEmblem(icon); else BuildContentCollectionEmblem(icon);
            TMP_Text label=CreateText("Label",rect,title+"   ›",32,boldFont,TextAlignmentOptions.Center,TextPrimary); SetTopLeft(label.rectTransform,102,5,242,90);
            AddRouteHotspot(rect,route);
            rect.gameObject.AddComponent<MainMenuDisclosureView>().Configure(route == UIRoute.Armory ? 3u : 0x1fu);
        }

        private static void BuildContentCollectionEmblem(RectTransform root)
        {
            // Three illustrated collection volumes; content rather than consumable shopping.
            for(int i=0;i<3;i++)
            {
                float x=9+i*21, y=16-i*4;
                var volume=CreateTopLeftRect("CollectionVolume"+i,root,x,y,23,54);
                var face=volume.gameObject.AddComponent<V3GradientGraphic>();
                face.Configure(new Color32(255,200,80,255),new Color32(173,91,12,255),new Color32(255,224,135,255),1.5f);
                CreateSolidTopLeft("Spine",volume,3,3,3,48,new Color32(103,53,8,255));
                CreateSolidTopLeft("TopBand",volume,8,10,11,2,new Color32(255,234,167,255));
                CreateSolidTopLeft("BottomBand",volume,8,41,11,2,new Color32(255,234,167,255));
                var badge=CreateTopLeftRect("CollectionStar",volume,9,20,10,14).gameObject.AddComponent<V3StarGraphic>();
                badge.Configure(new Color32(255,235,172,255),false,Color.clear);
            }
        }

        private static void BuildArmoryEmblem(RectTransform root)
        {
            var shield=CreateTopLeftRect("Shield",root,8,4,66,72).gameObject.AddComponent<V3PolygonGraphic>();
            shield.ConfigureResponsive(new[]{new Vector2(2,4),new Vector2(33,0),new Vector2(64,4),new Vector2(59,47),new Vector2(33,70),new Vector2(7,47)},
                new Color32(15,28,37,255),new Color32(255,191,63,255),2.5f,new Vector2(66,72));
            for(int i=0;i<2;i++)
            {
                var rifle=CreateRect("Rifle"+i,root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(54,14),Vector2.zero);
                rifle.localRotation=Quaternion.Euler(0,0,i==0?42:-42);
                CreateSolid("Receiver",rifle,new Color32(230,235,232,255),new Vector2(27,7),new Vector2(-3,0));
                CreateSolid("Barrel",rifle,new Color32(255,193,61,255),new Vector2(20,3),new Vector2(18,2));
                CreateSolid("Stock",rifle,new Color32(255,193,61,255),new Vector2(9,10),new Vector2(-22,-1));
                CreateSolid("Magazine",rifle,new Color32(230,235,232,255),new Vector2(5,9),new Vector2(-3,-6)).rectTransform.localRotation=Quaternion.Euler(0,0,-18);
            }
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
            CreateRing("OuterRing", root, new Vector2(52f, 52f), Vector2.zero, color, 3f);
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
            => CaptureConfigured(outputPath,width,height,null);

        public static void CaptureConfigured(string outputPath,int width,int height,Action<GameObject> configure)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Main Menu prefab for capture: {PrefabPath}");

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject cameraObject = new("MainMenuV3CaptureCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
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
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
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
            // Editor capture instances do not consistently receive MonoBehaviour
            // OnEnable callbacks. Apply the active locale explicitly so the QA image
            // verifies the same localized text and font path used at runtime.
            V3LocalizedTextBindingView[] localizedTextBindings =
                instance.GetComponentsInChildren<V3LocalizedTextBindingView>(true);
            for (int i = 0; i < localizedTextBindings.Length; i++)
                localizedTextBindings[i].ApplyLocalization();
            foreach(var view in instance.GetComponentsInChildren<MainMenuCampaignCardView>(true)) view.Refresh();
            foreach(var view in instance.GetComponentsInChildren<MainMenuAccountHeaderView>(true)) view.Refresh();
            foreach(var view in instance.GetComponentsInChildren<MainMenuCommanderVariantView>(true)) view.RefreshIdentity();
            foreach(var view in instance.GetComponentsInChildren<MainMenuDisclosureView>(true)) view.Refresh();
            configure?.Invoke(instance);
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
                EditorSceneManager.ClosePreviewScene(scene);
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
            gradient.raycastTarget = false;
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
            text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void ExpandTextWithParent(RectTransform rect,float authoredParentWidth)
        {
            rect.anchorMax=new Vector2(1,1);
            rect.sizeDelta=new Vector2(rect.sizeDelta.x-authoredParentWidth,rect.sizeDelta.y);
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
