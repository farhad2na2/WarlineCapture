using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CampaignOperationsV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
        private const string CommanderScenePath = "Assets/Game/Art/UI/V3Shared/CommanderScenes/SCN02_FieldCommander_01_Scene_V3.png";
        private const string CampaignPlatePath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_CampaignScene_V3.png";
        private const string OperationsPlatePath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_OperationsScene_V3.png";
        private const string SkirmishPlatePath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_SkirmishScene_V3.png";
        private const string MissionMapPath = "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string MissionPreviewPath = "Assets/Game/Art/UI/Generated/SkirmishSetup/TargetLockV02/scn13_operation_preview_sahrin_v02.png";
        private const string AriaPortraitPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
        private const string DaliaPortraitPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Insurgent_Female_01_Rifle_Card_512.png";
        private const string SamiraPortraitPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Pilot_Female_01_CompactPistol_Card_512.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(69, 81, 85, 255);
        private static readonly Color DarkTop = new Color32(22, 32, 35, 250);
        private static readonly Color DarkBottom = new Color32(5, 12, 15, 252);
        private static readonly Color BlueTop = new Color32(16, 120, 198, 255);
        private static readonly Color BlueBottom = new Color32(4, 57, 101, 255);
        private static readonly Color GreenTop = new Color32(74, 151, 26, 255);
        private static readonly Color GreenBottom = new Color32(19, 72, 22, 255);
        private static readonly Color AmberTop = new Color32(153, 101, 3, 255);
        private static readonly Color AmberBottom = new Color32(58, 35, 2, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Sprite commanderScene;
        private static Sprite campaignPlate;
        private static Sprite operationsPlate;
        private static Sprite skirmishPlate;
        private static Sprite missionMap;
        private static Sprite missionPreview;
        private static Sprite ariaPortrait;
        private static Sprite daliaPortrait;
        private static Sprite samiraPortrait;

        [MenuItem("Game/UI/V3/Rebuild Campaign Operations Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform rootRect = CreateRect("SCN05_CampaignOperationsContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject root = rootRect.gameObject;
            Image backdrop = CreateImage("Backdrop", root.transform, commanderScene, new Color(0.34f, 0.34f, 0.32f, 1f), false);
            Stretch(backdrop.rectTransform);
            AddCover(backdrop, commanderScene);
            Image scrim = CreateImage("BackdropScrim", root.transform, null, new Color(0.005f, 0.008f, 0.008f, 0.60f), false);
            Stretch(scrim.rectTransform);

            RectTransform composition = CreateTopLeft("CampaignComposition", root.transform, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center);
            CampaignOperationsScreenView screen = composition.gameObject.AddComponent<CampaignOperationsScreenView>();

            BuildHeader(composition);
            RectTransform chapterState = CreateTopLeft("ChapterSelectState", composition, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            RectTransform missionState = CreateTopLeft("MissionSelectState", composition, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);

            BuildChapterState(
                chapterState,
                out Button openMissionSelect,
                out Button storyArchive,
                out UIShellRouteButtonView chapterBack);
            BuildMissionState(
                missionState,
                out RectTransform chapterRail,
                out RectTransform strategicMap,
                out RectTransform briefingPanel,
                out RectTransform[] chapterCards,
                out RectTransform[] missionNodes,
                out Button[] missionNodeButtons,
                out RectTransform[] progressNodes,
                out RawImage districtMap,
                out RawImage preview,
                out TMP_Text screenTitle,
                out TMP_Text missionNumber,
                out TMP_Text missionName,
                out TMP_Text briefingText,
                out TMP_Text objectiveText,
                out TMP_Text rewardText,
                out TMP_Text launchLabel,
                out Button showChapters,
                out Button launch,
                out UIShellRouteButtonView missionBack);

            chapterState.gameObject.SetActive(false);
            missionState.gameObject.SetActive(true);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", missionBack != null ? missionBack : chapterBack);
            SetReference(serialized, "chapterRail", chapterRail);
            SetReference(serialized, "strategicMap", strategicMap);
            SetReference(serialized, "missionBriefing", briefingPanel);
            SetArray(serialized, "chapterCards", chapterCards);
            SetArray(serialized, "missionNodes", missionNodes);
            SetArray(serialized, "missionNodeButtons", missionNodeButtons);
            SetArray(serialized, "progressNodes", progressNodes);
            SetReference(serialized, "districtMapImage", districtMap);
            SetReference(serialized, "missionPreviewImage", preview);
            SetReference(serialized, "m01MissionPreview", missionPreview.texture);
            SetReference(serialized, "m02MissionPreview", missionPreview.texture);
            SetReference(serialized, "screenTitle", screenTitle);
            SetReference(serialized, "missionNumber", missionNumber);
            SetReference(serialized, "missionName", missionName);
            SetReference(serialized, "missionBriefingText", briefingText);
            SetReference(serialized, "primaryObjectiveText", objectiveText);
            SetReference(serialized, "rewardSummaryText", rewardText);
            SetReference(serialized, "launchMissionLabel", launchLabel);
            SetReference(serialized, "storyArchiveButton", storyArchive);
            SetReference(serialized, "chapterIntelButton", showChapters);
            SetReference(serialized, "launchMissionButton", launch);
            SetReference(serialized, "chapterSelectRoot", chapterState);
            SetReference(serialized, "missionSelectRoot", missionState);
            SetReference(serialized, "showMissionSelectButton", openMissionSelect);
            SetReference(serialized, "showChapterSelectButton", showChapters);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CampaignMissionScreenBinder binder = composition.gameObject.AddComponent<CampaignMissionScreenBinder>();
            binder.Configure(screen, "saga.ch01.m02.establish_base");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[CampaignOperationsV3PrefabBuilder] result=Passed states=2 map=SCN05_SahrinMissionMap_V3 chrome=procedural-shared");
        }

        [MenuItem("Game/UI/V3/Validate Campaign Operations Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Campaign V3 prefab: {PrefabPath}");
            CampaignOperationsScreenView screen = prefab.GetComponentInChildren<CampaignOperationsScreenView>(true);
            if (screen == null || screen.ChapterSelectRoot == null || screen.MissionSelectRoot == null ||
                screen.ShowMissionSelectButton == null || screen.ShowChapterSelectButton == null)
                throw new MissingReferenceException("Campaign V3 state switching is incomplete.");
            if (screen.MissionNodeButtons == null || screen.MissionNodeButtons.Length != 5 ||
                screen.LaunchMissionButton == null || screen.DistrictMapImage == null)
                throw new MissingReferenceException("Campaign V3 mission bindings are incomplete.");
            if (AssetDatabase.GetAssetPath(screen.DistrictMapImage.texture) != MissionMapPath)
                throw new MissingReferenceException("Campaign V3 must use the canonical Sahrin mission-map plate.");
            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.sprite == null || AssetDatabase.GetAssetPath(image.sprite) != AriaPortraitPath)
                    continue;
                if (!image.preserveAspect && image.GetComponent<AspectRatioFitter>() == null)
                    throw new InvalidOperationException($"ARIA portrait must preserve its source aspect ratio: {image.transform.name}.");
            }
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 34)
                throw new InvalidOperationException($"Campaign V3 requires procedural gradients; found {gradients}.");
            Debug.Log($"[CampaignOperationsV3PrefabBuilder] validation=Passed gradients={gradients} images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        private static void LoadAssets()
        {
            ConfigureSprite(MissionMapPath, 4096);
            ConfigureSprite(CommanderScenePath, 4096);
            ConfigureSprite(CampaignPlatePath, 2048);
            ConfigureSprite(OperationsPlatePath, 2048);
            ConfigureSprite(SkirmishPlatePath, 2048);
            ConfigureSprite(MissionPreviewPath, 2048);
            ConfigureSprite(AriaPortraitPath, 1024);
            ConfigureSprite(DaliaPortraitPath, 1024);
            ConfigureSprite(SamiraPortraitPath, 1024);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            commanderScene = RequireSprite(CommanderScenePath);
            campaignPlate = RequireSprite(CampaignPlatePath);
            operationsPlate = RequireSprite(OperationsPlatePath);
            skirmishPlate = RequireSprite(SkirmishPlatePath);
            missionMap = RequireSprite(MissionMapPath);
            missionPreview = RequireSprite(MissionPreviewPath);
            ariaPortrait = RequireSprite(AriaPortraitPath);
            daliaPortrait = RequireSprite(DaliaPortraitPath);
            samiraPortrait = RequireSprite(SamiraPortraitPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("Campaign V3 fonts are missing.");
        }

        private static void BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 10f, 12f, 390f, 96f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            BuildResourceChip(root, "CreditsChip", 1008f, 14f, 260f, 94f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "CommandChip", 1278f, 14f, 260f, 94f, catalog.CommandIcon, "COMMAND", "8,430");
            Button settings = CreateGradientButton("SettingsButton", root, 1548f, 14f, 108f, 94f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetRect(gear.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 52f), Vector2.zero);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 15f, 18f, 55f, 55f);
            TMP_Text labelText = CreateText("Label", chip, label, 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 79f, 7f, width - 88f, 33f);
            TMP_Text valueText = CreateText("Value", chip, value, 33f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 79f, 36f, width - 88f, 48f);
        }

        private static void BuildChapterState(RectTransform root, out Button openMissionSelect, out Button storyArchive, out UIShellRouteButtonView backRoute)
        {
            RectTransform sceneClip = CreateTopLeft("ChapterBackdropClip", root, 0f, 111f, 1672f, 680f);
            sceneClip.gameObject.AddComponent<RectMask2D>();
            Image scene = CreateImage("ChapterBackdrop", sceneClip, commanderScene, new Color(0.62f, 0.60f, 0.54f, 1f), false);
            Stretch(scene.rectTransform);
            AddCover(scene, commanderScene);
            CreateSolidTopLeft("ChapterScrim", root, 0f, 111f, 1672f, 680f, new Color(0.005f, 0.008f, 0.008f, 0.45f));

            RectTransform emblem = CreateTopLeft("CampaignEmblem", root, 15f, 140f, 112f, 118f);
            CreateGradientPanel(emblem, new Color32(17, 79, 30, 255), DarkBottom, theme.Green, 3f);
            Image target = CreateImage("Icon", emblem, catalog.AttackIcon, theme.Green, false);
            SetTopLeft(target.rectTransform, 23f, 25f, 66f, 66f);
            TMP_Text campaign = CreateText("CampaignTitle", root, "CAMPAIGN", 54f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(campaign.rectTransform, 143f, 135f, 470f, 66f);
            TMP_Text chapters = CreateText("CampaignSubtitle", root, "CHAPTERS", 30f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(chapters.rectTransform, 143f, 198f, 320f, 45f);

            RectTransform hero = CreateTopLeft("ChapterOneHero", root, 15f, 273f, 713f, 496f);
            CreateGradientPanel(hero, DarkTop, DarkBottom, theme.Green, 3f);
            RectTransform heroClip = CreateTopLeft("ArtClip", hero, 5f, 5f, 703f, 486f);
            heroClip.gameObject.AddComponent<RectMask2D>();
            Image heroArt = CreateImage("Art", heroClip, commanderScene, Color.white, false);
            Stretch(heroArt.rectTransform);
            AddCover(heroArt, commanderScene);
            Image heroShade = CreateImage("Shade", heroClip, null, new Color(0f, 0f, 0f, 0.25f), false);
            Stretch(heroShade.rectTransform);
            BuildChapterHeroLabel(hero);
            Button m01 = CreateGradientButton("M01Completed", hero, 18f, 369f, 168f, 90f, DarkTop, DarkBottom, theme.Green, 3f);
            AddButtonCopy(m01, "M01", "COMPLETED", theme.Green);
            Image m01Icon = CreateImage("Icon", m01.transform, RequireSprite(V3UiFoundationBuilder.CommanderCheckIconPath), theme.Green, false);
            SetTopLeft(m01Icon.rectTransform, 111f, 20f, 48f, 48f);
            Button m02 = CreateGradientButton("M02Current", hero, 216f, 369f, 168f, 90f, DarkTop, DarkBottom, theme.Green, 3f);
            AddButtonCopy(m02, "M02", "CURRENT", theme.Green);
            Image m02Icon = CreateImage("Icon", m02.transform, catalog.AttackIcon, theme.Green, false);
            SetTopLeft(m02Icon.rectTransform, 111f, 20f, 48f, 48f);

            Sprite[] art = { operationsPlate, skirmishPlate, campaignPlate, commanderScene };
            string[] roman = { "II", "III", "IV", "V" };
            string[] names = { "BROKEN GRID", "HIDDEN NETWORK", "AIR AND ARMOR", "CITYWIDE COMMAND" };
            Color[] accents = { theme.Cyan, theme.Green, theme.OrangeRed, theme.OrangeRed };
            for (int i = 0; i < 4; i++)
                BuildChapterRow(root, 742f, 273f + i * 126f, 540f, 116f, roman[i], names[i], art[i], accents[i]);

            RectTransform aria = CreateTopLeft("AriaProtocol", root, 1296f, 128f, 360f, 392f);
            CreateGradientPanel(aria, DarkTop, DarkBottom, theme.Cyan, 3f);
            TMP_Text ariaTitle = CreateText("Title", aria, "ARIA PROTOCOL", 28f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Cyan);
            SetTopLeft(ariaTitle.rectTransform, 18f, 8f, 320f, 42f);
            Image ariaImage = CreateImage("Portrait", aria, ariaPortrait, Color.white, false);
            SetTopLeft(ariaImage.rectTransform, 78f, 52f, 204f, 224f);
            ariaImage.preserveAspect = true;
            TMP_Text fragment = CreateText("Fragment", aria, "PROTOCOL FRAGMENT  0/5", 20f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(fragment.rectTransform, 14f, 276f, 332f, 42f);
            storyArchive = CreateGradientButton("StoryArchiveButton", aria, 17f, 320f, 326f, 56f, BlueTop, BlueBottom, theme.Cyan, 2f);
            TMP_Text storyLabel = CreateText("Label", storyArchive.transform, "STORY ARCHIVE", 24f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(storyLabel.rectTransform, 62f, 4f, 248f, 48f);
            Image storyIcon = CreateImage("Icon", storyArchive.transform, RequireSprite(V3UiFoundationBuilder.CampaignChaptersIconPath), Color.white, false);
            SetTopLeft(storyIcon.rectTransform, 17f, 9f, 40f, 38f);

            RectTransform current = CreateTopLeft("CurrentMission", root, 1296f, 532f, 360f, 249f);
            CreateGradientPanel(current, DarkTop, DarkBottom, theme.Green, 3f);
            TMP_Text currentLabel = CreateText("Title", current, "CURRENT MISSION", 25f, boldFont, TextAlignmentOptions.Center, theme.Green);
            SetTopLeft(currentLabel.rectTransform, 12f, 8f, 336f, 41f);
            TMP_Text currentName = CreateText("Name", current, "ESTABLISH THE BASE", 25f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(currentName.rectTransform, 12f, 48f, 336f, 48f);
            BuildPortraitChip(current, 14f, 102f, ariaPortrait, "ARIA", theme.Cyan);
            BuildPortraitChip(current, 126f, 102f, daliaPortrait, "DALIA", theme.Amber);
            BuildPortraitChip(current, 238f, 102f, samiraPortrait, "SAMIRA", theme.Green);

            Button back = CreateGradientButton("ChapterBackButton", root, 15f, 801f, 345f, 125f, DarkTop, DarkBottom, Border, 3f);
            AddFooterIconLabel(back, V3UiFoundationBuilder.CommanderBackIconPath, "BACK", 31f);
            backRoute = back.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            openMissionSelect = CreateGradientButton("OpenMissionSelectButton", root, 370f, 801f, 850f, 125f, BlueTop, BlueBottom, theme.Cyan, 3f);
            TMP_Text continueLabel = CreateText("Label", openMissionSelect.transform, "CONTINUE CAMPAIGN", 38f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            Stretch(continueLabel.rectTransform);
            Button footerArchive = CreateGradientButton("FooterStoryArchiveButton", root, 1230f, 801f, 426f, 125f, new Color32(23, 53, 103, 255), new Color32(6, 21, 55, 255), theme.Cyan, 3f);
            AddFooterIconLabel(footerArchive, V3UiFoundationBuilder.CampaignChaptersIconPath, "STORY ARCHIVE", 31f);
        }

        private static void BuildMissionState(
            RectTransform root,
            out RectTransform chapterRail,
            out RectTransform strategicMap,
            out RectTransform briefingPanel,
            out RectTransform[] chapterCards,
            out RectTransform[] missionNodes,
            out Button[] missionNodeButtons,
            out RectTransform[] progressNodes,
            out RawImage districtMap,
            out RawImage preview,
            out TMP_Text screenTitle,
            out TMP_Text missionNumber,
            out TMP_Text missionName,
            out TMP_Text briefingText,
            out TMP_Text objectiveText,
            out TMP_Text rewardText,
            out TMP_Text launchLabel,
            out Button showChapters,
            out Button launch,
            out UIShellRouteButtonView backRoute)
        {
            screenTitle = CreateText("ScreenTitle", root, "CAMPAIGN", 43f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(screenTitle.rectTransform, 444f, 91f, 255f, 62f);
            TMP_Text titleDivider = CreateText("TitleDivider", root, "|", 43f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(titleDivider.rectTransform, 693f, 91f, 34f, 62f);
            TMP_Text titleSection = CreateText("TitleSection", root, "MISSION SELECT", 29f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(titleSection.rectTransform, 728f, 98f, 350f, 50f);

            chapterRail = CreateTopLeft("ChapterRail", root, 10f, 121f, 396f, 637f);
            chapterCards = new RectTransform[5];
            Sprite[] rowArt = { campaignPlate, operationsPlate, skirmishPlate, commanderScene, campaignPlate };
            string[] names = { "FIRST RESPONSE", "BROKEN GRID", "HIDDEN NETWORK", "AIR AND ARMOR", "CITYWIDE COMMAND" };
            for (int i = 0; i < 5; i++)
                chapterCards[i] = BuildMissionChapterRow(chapterRail, i, rowArt[i], names[i]);

            strategicMap = CreateTopLeft("StrategicMap", root, 406f, 151f, 794f, 638f);
            CreateGradientPanel(strategicMap, DarkTop, DarkBottom, Border, 3f);
            RectTransform mapClip = CreateTopLeft("MapClip", strategicMap, 4f, 4f, 786f, 630f);
            mapClip.gameObject.AddComponent<RectMask2D>();
            districtMap = CreateRawImage("DistrictMap", mapClip, missionMap.texture, Color.white);
            Stretch(districtMap.rectTransform);
            AspectRatioFitter mapFitter = districtMap.gameObject.AddComponent<AspectRatioFitter>();
            mapFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            mapFitter.aspectRatio = missionMap.rect.width / missionMap.rect.height;
            CreateSolidTopLeft("MapShade", mapClip, 0f, 0f, 786f, 630f, new Color(0.02f, 0.01f, 0f, 0.16f));

            Vector2[] centers =
            {
                new(94f, 287f), new(333f, 361f), new(470f, 206f), new(643f, 337f), new(505f, 493f)
            };
            int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 } };
            for (int i = 0; i < edges.GetLength(0); i++)
                BuildRouteLine(mapClip, centers[edges[i, 0]], centers[edges[i, 1]], i == 0 ? theme.Green : theme.TextMuted);
            missionNodes = new RectTransform[5];
            missionNodeButtons = new Button[5];
            string[] missionNames = { "FIRST CONTACT", "ESTABLISH\nTHE BASE", "RADAR WARNING", "AIRLIFT", "BREACH ASSAULT" };
            for (int i = 0; i < 5; i++)
            {
                bool selected = i == 1;
                bool completed = i == 0;
                Sprite nodeSprite = RequireSprite(selected
                    ? V3UiFoundationBuilder.CampaignNodeActivePath
                    : completed
                        ? V3UiFoundationBuilder.CampaignNodeClaimedPath
                        : V3UiFoundationBuilder.CampaignNodeLockedPath);
                Button node = CreateSpriteButton(
                    "MissionNode_" + (i + 1),
                    mapClip,
                    centers[i].x - (selected ? 43f : 33f),
                    centers[i].y - (selected ? 43f : 33f),
                    selected ? 86f : 66f,
                    selected ? 86f : 66f,
                    nodeSprite);
                if (selected)
                {
                    TMP_Text number = CreateText("Number", node.transform, "02", 23f, boldFont, TextAlignmentOptions.Center, theme.Amber);
                    Stretch(number.rectTransform);
                }
                else
                {
                    Sprite stateSprite = RequireSprite(completed
                        ? V3UiFoundationBuilder.CommanderCheckIconPath
                        : V3UiFoundationBuilder.CommanderLockIconPath);
                    Image stateIcon = CreateImage("StateIcon", node.transform, stateSprite, completed ? theme.TextPrimary : theme.TextMuted, false);
                    SetRect(
                        stateIcon.rectTransform,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        completed ? new Vector2(30f, 30f) : new Vector2(22f, 28f),
                        Vector2.zero);
                }
                RectTransform label = CreateTopLeft("MissionLabel", mapClip, centers[i].x - 54f, centers[i].y - 83f, 150f, 55f);
                CreateGradientPanel(label, DarkTop, DarkBottom, selected ? theme.Amber : Border, 2f);
                TMP_Text id = CreateText("Id", label, "M0" + (i + 1), 17f, boldFont, TextAlignmentOptions.MidlineLeft, selected ? theme.Amber : completed ? theme.Green : theme.TextMuted);
                SetTopLeft(id.rectTransform, 8f, 2f, 48f, 22f);
                TMP_Text nodeName = CreateText("Name", label, missionNames[i], 14f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(nodeName.rectTransform, 8f, 21f, 135f, 35f);
                nodeName.overflowMode = TextOverflowModes.Overflow;
                missionNodes[i] = node.GetComponent<RectTransform>();
                missionNodeButtons[i] = node;
            }

            briefingPanel = CreateTopLeft("MissionBriefing", root, 1200f, 102f, 462f, 687f);
            CreateGradientPanel(briefingPanel, DarkTop, DarkBottom, Border, 3f);
            missionNumber = CreateText("MissionNumber", briefingPanel, "M02", 27f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(missionNumber.rectTransform, 20f, 8f, 90f, 38f);
            missionName = CreateText("MissionName", briefingPanel, "ESTABLISH THE BASE", 34f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(missionName.rectTransform, 20f, 43f, 420f, 54f);
            RectTransform previewClip = CreateTopLeft("MissionPreviewClip", briefingPanel, 20f, 98f, 422f, 164f);
            previewClip.gameObject.AddComponent<RectMask2D>();
            preview = CreateRawImage("MissionPreviewImage", previewClip, missionPreview.texture, Color.white);
            Stretch(preview.rectTransform);
            AspectRatioFitter previewFitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            previewFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            previewFitter.aspectRatio = missionPreview.rect.width / missionPreview.rect.height;
            briefingText = CreateText("MissionBriefingText", briefingPanel, "Reopen an abandoned JRC forward post and establish our foothold in Sahrin.", 16f, mediumFont, TextAlignmentOptions.TopLeft, theme.TextPrimary);
            SetTopLeft(briefingText.rectTransform, 20f, 267f, 422f, 61f);
            briefingText.enableWordWrapping = true;
            TMP_Text objectivesTitle = CreateText("ObjectivesTitle", briefingPanel, "OBJECTIVES", 21f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(objectivesTitle.rectTransform, 20f, 329f, 200f, 30f);
            objectiveText = BuildObjectiveCard(briefingPanel, 20f, 360f, 128f, "BUILD\nBARRACK", V3UiFoundationBuilder.CampaignBarracksIconPath);
            BuildObjectiveCard(briefingPanel, 157f, 360f, 128f, "PRODUCE\nSQUAD", V3UiFoundationBuilder.CampaignSquadIconPath);
            BuildObjectiveCard(briefingPanel, 294f, 360f, 148f, "HOLD\nPERIMETER", V3UiFoundationBuilder.CampaignHoldIconPath);
            TMP_Text rewardsTitle = CreateText("RewardsTitle", briefingPanel, "REWARDS", 21f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(rewardsTitle.rectTransform, 20f, 456f, 180f, 30f);
            rewardText = CreateText("RewardSummaryText", briefingPanel, "XP 150  |  1,500 CREDITS  |  BARRACK UNLOCK", 17f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Cyan);
            SetTopLeft(rewardText.rectTransform, 20f, 487f, 422f, 52f);
            TMP_Text goalsTitle = CreateText("GoalsTitle", briefingPanel, "MISSION GOALS", 20f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(goalsTitle.rectTransform, 20f, 544f, 200f, 29f);
            for (int i = 0; i < 3; i++)
            {
                RectTransform goal = CreateTopLeft("Goal" + i, briefingPanel, 20f + i * 140f, 575f, 128f, 94f);
                CreateGradientPanel(goal, DarkTop, DarkBottom, Border, 2f);
                Image goalStar = CreateImage("Star", goal, RequireSprite(V3UiFoundationBuilder.CommanderHeaderStarIconPath), theme.Amber, false);
                SetTopLeft(goalStar.rectTransform, 43f, 9f, 42f, 42f);
                string copy = i == 0 ? "COMPLETE MISSION" : i == 1 ? "NO UNIT LOSSES" : "UNDER 15:00";
                TMP_Text goalCopy = CreateText("Copy", goal, copy, 12f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
                SetTopLeft(goalCopy.rectTransform, 6f, 50f, 116f, 38f);
            }

            Button back = CreateGradientButton("MissionBackButton", root, 10f, 803f, 294f, 123f, DarkTop, DarkBottom, Border, 3f);
            AddFooterIconLabel(back, V3UiFoundationBuilder.CommanderBackIconPath, "BACK", 31f);
            backRoute = back.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            showChapters = CreateGradientButton("ShowChapterSelectButton", root, 306f, 803f, 378f, 123f, BlueTop, BlueBottom, theme.Cyan, 3f);
            AddFooterIconLabel(showChapters, V3UiFoundationBuilder.CampaignChaptersIconPath, "CHAPTERS", 31f);
            launch = CreateGradientButton("LaunchMissionButton", root, 688f, 803f, 974f, 123f, GreenTop, GreenBottom, theme.Green, 3f);
            launchLabel = CreateText("Label", launch.transform, "START BRIEFING", 40f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(launchLabel.rectTransform, 280f, 8f, 630f, 107f);
            Image launchIcon = CreateImage("Icon", launch.transform, RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), Color.white, false);
            SetTopLeft(launchIcon.rectTransform, 210f, 33f, 64f, 58f);

            progressNodes = new RectTransform[5];
            for (int i = 0; i < progressNodes.Length; i++)
            {
                Image progress = CreateImage("ProgressNode" + i, strategicMap, RequireSprite(V3UiFoundationBuilder.CommanderHeaderStarIconPath), theme.Amber, false);
                SetTopLeft(progress.rectTransform, 16f + i * 28f, 600f, 20f, 20f);
                progressNodes[i] = progress.rectTransform;
            }
        }

        private static void BuildChapterHeroLabel(Transform hero)
        {
            RectTransform roman = CreateTopLeft("Roman", hero, 13f, 13f, 96f, 132f);
            CreateGradientPanel(roman, GreenTop, GreenBottom, theme.Green, 3f);
            TMP_Text one = CreateText("One", roman, "I", 72f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            Stretch(one.rectTransform);
            TMP_Text title = CreateText("Title", hero, "CHAPTER I", 42f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 120f, 16f, 310f, 55f);
            TMP_Text subtitle = CreateText("Subtitle", hero, "FIRST RESPONSE", 28f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Green);
            SetTopLeft(subtitle.rectTransform, 120f, 65f, 330f, 43f);
        }

        private static void AddButtonCopy(Button button, string title, string subtitle, Color accent)
        {
            TMP_Text titleText = CreateText("Title", button.transform, title, 28f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(titleText.rectTransform, 12f, 5f, 74f, 39f);
            TMP_Text subtitleText = CreateText("Subtitle", button.transform, subtitle, 15f, boldFont, TextAlignmentOptions.MidlineLeft, accent);
            SetTopLeft(subtitleText.rectTransform, 12f, 46f, 140f, 30f);
        }

        private static void BuildChapterRow(Transform root, float x, float y, float width, float height, string roman, string name, Sprite art, Color accent)
        {
            RectTransform row = CreateTopLeft("Chapter" + roman, root, x, y, width, height);
            CreateGradientPanel(row, DarkTop, DarkBottom, accent, 3f);
            RectTransform clip = CreateTopLeft("ArtClip", row, 91f, 4f, width - 95f, height - 8f);
            clip.gameObject.AddComponent<RectMask2D>();
            Image image = CreateImage("Art", clip, art, new Color(0.55f, 0.55f, 0.52f, 1f), false);
            Stretch(image.rectTransform);
            AddCover(image, art);
            CreateSolidTopLeft("Shade", clip, 0f, 0f, width - 95f, height - 8f, new Color(0f, 0f, 0f, 0.46f));
            RectTransform romanBlock = CreateTopLeft("RomanBlock", row, 4f, 4f, 87f, height - 8f);
            CreateGradientPanel(romanBlock, Color.Lerp(DarkTop, accent, 0.34f), DarkBottom, accent, 2f);
            TMP_Text romanText = CreateText("Roman", romanBlock, roman, 54f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            Stretch(romanText.rectTransform);
            TMP_Text title = CreateText("Title", row, "CHAPTER " + roman, 26f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 105f, 12f, 280f, 41f);
            TMP_Text subtitle = CreateText("Subtitle", row, name, 20f, boldFont, TextAlignmentOptions.MidlineLeft, accent);
            SetTopLeft(subtitle.rectTransform, 105f, 52f, 300f, 36f);
            Image lockIcon = CreateImage("Lock", row, RequireSprite(V3UiFoundationBuilder.CommanderLockIconPath), Color.white, false);
            SetTopLeft(lockIcon.rectTransform, width - 61f, 33f, 36f, 42f);
        }

        private static RectTransform BuildMissionChapterRow(Transform parent, int index, Sprite art, string name)
        {
            RectTransform row = CreateTopLeft("ChapterCard_" + (index + 1), parent, 0f, index * 126f, 396f, 116f);
            CreateGradientPanel(row, index == 0 ? AmberTop : DarkTop, index == 0 ? AmberBottom : DarkBottom, index == 0 ? theme.Amber : Border, 3f);
            RectTransform clip = CreateTopLeft("ArtClip", row, 4f, 4f, 392f, 108f);
            clip.gameObject.AddComponent<RectMask2D>();
            Image image = CreateImage("Art", clip, art, new Color(0.62f, 0.60f, 0.55f, 1f), false);
            Stretch(image.rectTransform);
            AddCover(image, art);
            CreateSolidTopLeft("Shade", clip, 0f, 0f, 392f, 108f, new Color(0f, 0f, 0f, index == 0 ? 0.30f : 0.68f));
            Image icon = CreateImage("Icon", row, index == 0 ? catalog.AttackIcon : RequireSprite(V3UiFoundationBuilder.CommanderLockIconPath), Color.white, false);
            SetTopLeft(icon.rectTransform, 16f, 22f, 58f, 58f);
            TMP_Text title = CreateText("Title", row, "CHAPTER " + ToRoman(index + 1), 26f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 90f, 11f, 245f, 41f);
            TMP_Text subtitle = CreateText("Subtitle", row, name, 19f, boldFont, TextAlignmentOptions.MidlineLeft, index == 0 ? theme.Amber : theme.TextMuted);
            SetTopLeft(subtitle.rectTransform, 90f, 52f, 270f, 37f);
            return row;
        }

        private static void BuildPortraitChip(Transform parent, float x, float y, Sprite portrait, string name, Color accent)
        {
            RectTransform card = CreateTopLeft(name, parent, x, y, 102f, 130f);
            CreateGradientPanel(card, DarkTop, DarkBottom, accent, 2f);
            RectTransform clip = CreateTopLeft("Clip", card, 3f, 3f, 96f, 92f);
            clip.gameObject.AddComponent<RectMask2D>();
            Image image = CreateImage("Portrait", clip, portrait, Color.white, false);
            Stretch(image.rectTransform);
            AddCover(image, portrait);
            TMP_Text label = CreateText("Label", card, name, 16f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(label.rectTransform, 3f, 96f, 96f, 30f);
        }

        private static TMP_Text BuildObjectiveCard(Transform root, float x, float y, float width, string copy, string iconPath)
        {
            RectTransform card = CreateTopLeft("Objective", root, x, y, width, 88f);
            CreateGradientPanel(card, GreenTop, GreenBottom, theme.Green, 2f);
            Image icon = CreateImage("Icon", card, RequireSprite(iconPath), theme.TextPrimary, false);
            SetTopLeft(icon.rectTransform, width * 0.5f - 20f, 5f, 40f, 40f);
            TMP_Text label = CreateText("Label", card, copy, 14f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(label.rectTransform, 5f, 44f, width - 10f, 40f);
            return label;
        }

        private static void BuildRouteLine(Transform root, Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            Image line = CreateImage("Route", root, null, color, false);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(length, 5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * 0.5f, -(start.y + end.y) * 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void AddFooterIconLabel(Button button, string iconPath, string label, float size)
        {
            Image icon = CreateImage("Icon", button.transform, RequireSprite(iconPath), Color.white, false);
            SetTopLeft(icon.rectTransform, 55f, 33f, 58f, 58f);
            TMP_Text text = CreateText("Label", button.transform, label, size, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(text.rectTransform, 112f, 8f, button.GetComponent<RectTransform>().sizeDelta.x - 130f, button.GetComponent<RectTransform>().sizeDelta.y - 16f);
        }

        private static string ToRoman(int value) => value switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", _ => "V" };

        private static Button CreateGradientButton(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradientPanel(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f),
                pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            return button;
        }

        private static Button CreateSpriteButton(string name, Transform parent, float x, float y, float width, float height, Sprite sprite)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static V3GradientGraphic CreateGradientPanel(RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            return graphic;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, position);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast) =>
            V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 100f), Vector2.zero);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolidTopLeft(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
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
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void AddCover(Image image, Sprite sprite)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Campaign V3 texture: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing Campaign V3 sprite: {path}");
            return sprite;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
