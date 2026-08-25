#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class CampaignOperationsPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MapPath = "Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_sahrin_district_map_v01.png";
        private const string MissionPreviewPath = "Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_blackout_relay_preview_v01.png";
        private const string M02MissionPreviewPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Barrack_Action_512.png";
        private const string PanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_01_popup_outer_frame.png";
        private const string DetailPanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_03_detail_panel_frame.png";
        private const string SelectedSpritePath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png";
        private const string DefaultCardSpritePath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png";
        private const string SecondarySpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_15_secondary_dark_cta_frame.png";
        private const string GoldSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_14_primary_gold_cta_frame.png";
        private const string BackIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png";
        private const string LockIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_lock.png";
        private const string ObjectiveIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_attack_crosshair.png";
        private const string StarIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_objective_star.png";
        private const string CivilianIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_civilian_group.png";
        private const string IntelIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_scan_radar.png";
        private const string ArchiveIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png";
        private const string ActiveNodeSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png";
        private const string RouteSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png";
        private const string LeftChevronPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_chevron_left.png";
        private const string RightChevronPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_chevron_right.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string ChapterOneArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_campaign_thumbnail_art.png";
        private const string ChapterTwoArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_operations_thumbnail_art.png";
        private const string ChapterFourArtPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_art_attack_helicopter.png";

        private static readonly Color PanelTint = new(0.018f, 0.024f, 0.022f, 0.995f);
        private static readonly Color RowTint = new(0.035f, 0.043f, 0.039f, 0.995f);
        private static readonly Color SelectedTint = new(0.30f, 0.34f, 0.045f, 0.995f);
        private static readonly Color Gold = new(0.94f, 0.68f, 0.16f, 1f);
        private static readonly Color Olive = new(0.68f, 0.76f, 0.18f, 1f);
        private static readonly Color Cyan = new(0.20f, 0.72f, 0.88f, 1f);
        private static readonly Color Text = new(0.93f, 0.90f, 0.80f, 1f);
        private static readonly Color Muted = new(0.62f, 0.61f, 0.53f, 1f);
        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/Build SCN-05 Campaign Operations")]
        public static void Build()
        {
            GameObject prefab = BuildM01CampaignPrefab();
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CampaignOperationsPrefabBuilder] result=Passed prefab={PrefabPath} scene={MenuScenePath}");
        }

        public static void BuildM01CampaignPrefabOnly()
        {
            BuildM01CampaignPrefab();
        }

        private static GameObject BuildM01CampaignPrefab()
        {
            ImportProductionArt();
            LoadStyleAssets();
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null && existing.GetComponent<CampaignOperationsScreenView>() != null &&
                existing.GetComponent<CampaignMissionScreenBinder>() == null)
            {
                AddMissingCampaignBinder();
                existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            if (IsCurrentM01CampaignPrefab(existing))
            {
                Debug.Log($"[CampaignOperationsPrefabBuilder] result=Passed prefab={PrefabPath} scope=PrefabOnly reused=true");
                return existing;
            }

            GameObject root = BuildPrefabRoot();
            EnsureFolder("Assets/Game/Prefabs/UI/Shell/Content");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Campaign prefab at {PrefabPath}.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CampaignOperationsPrefabBuilder] result=Passed prefab={PrefabPath} scope=PrefabOnly");
            return prefab;
        }

        private static bool IsCurrentM01CampaignPrefab(GameObject prefab)
        {
            if (prefab == null) return false;
            CampaignOperationsScreenView view = prefab.GetComponent<CampaignOperationsScreenView>();
            CampaignMissionScreenBinder binder = prefab.GetComponent<CampaignMissionScreenBinder>();
            return view != null && binder != null && view.BackRouteButton != null && view.ChapterRail != null &&
                   view.StrategicMap != null && view.MissionBriefing != null &&
                   view.ChapterCards is { Length: > 0 } && view.MissionNodes is { Length: > 0 } &&
                   view.MissionNodeButtons is { Length: 5 } &&
                   view.ProgressNodes is { Length: 5 } && view.DistrictMapImage != null &&
                   view.DistrictMapImage.texture != null && view.MissionPreviewImage != null &&
                   view.MissionPreviewImage.texture != null && view.ScreenTitle != null &&
                   view.MissionName != null && view.StoryArchiveButton != null &&
                   view.MissionNumber != null && view.MissionBriefingText != null &&
                   view.PrimaryObjectiveText != null && view.RewardSummaryText != null &&
                   view.ChapterIntelButton != null && view.LaunchMissionButton != null;
        }

        private static void AddMissingCampaignBinder()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CampaignOperationsScreenView view = root.GetComponent<CampaignOperationsScreenView>();
                if (view == null)
                    throw new InvalidOperationException("Campaign prefab is missing its screen view.");
                CampaignMissionScreenBinder binder = root.AddComponent<CampaignMissionScreenBinder>();
                binder.Configure(view, "saga.ch01.m01.first_contact");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Game/UI/Capture SCN-05 Campaign Operations")]
        public static void CaptureCampaign()
        {
            string path = ResolveCapturePath();
            int width = ResolvePositiveEnvironmentInt("WARLINE_CAMPAIGN_CAPTURE_WIDTH", 1920);
            int height = ResolvePositiveEnvironmentInt("WARLINE_CAMPAIGN_CAPTURE_HEIGHT", 1080);
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = ResolveComponentInHierarchy<MenuBootstrapView>(root.transform);
                if (bootstrap != null)
                    break;
            }

            if (bootstrap == null || bootstrap.ContentSystem == null || bootstrap.UiCamera == null || bootstrap.UiCanvas == null)
                throw new InvalidOperationException("Menu scene is missing its configured Canvas bootstrap references.");

            bootstrap.ApplyRuntimeUiMode();
            bootstrap.ContentSystem.PrepareForCommandSequence(new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.EnterMenu,
                    default,
                    UIRoute.MainMenu,
                    UiShellMode.MainMenu,
                    1)
            });
            bootstrap.ContentSystem.InstallMenuRouteBody(UIRoute.Campaign);
            Canvas.ForceUpdateCanvases();
            RenderCameraToPng(bootstrap.UiCamera, path, width, height);
            Debug.Log($"[CampaignOperationsCapture] result=Passed size={width}x{height} path={path}");
        }

        private static GameObject BuildPrefabRoot()
        {
            GameObject root = CreateRect("SCN05_CampaignOperationsContent", null, 0f, 0f, 4800f, 2160f);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            CampaignOperationsScreenView screen = root.AddComponent<CampaignOperationsScreenView>();

            Image bodyScrim = CreateSolid("BodyScrim", root.transform, 0f, 280f, 4800f, 1880f, new Color(0.006f, 0.009f, 0.008f, 0.93f));
            SetVerticalStretch(bodyScrim.rectTransform, 280f, 0f);
            bodyScrim.raycastTarget = false;

            Button backButton = CreateButton("BackButton", root.transform, 90f, 300f, 540f, 165f, "BACK", SecondarySpritePath, 70f, Text, out TMP_Text backLabel);
            UIShellRouteButtonView backRoute = backButton.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            CreateIcon("BackIcon", backButton.transform, BackIconPath, 40f, 40f, 82f, 82f);
            SetTextRect(backLabel.rectTransform, 135f, 0f, 350f, 165f);

            TMP_Text screenTitle = CreateText("ScreenTitle", root.transform, 690f, 292f, 1880f, 175f, "CAMPAIGN OPERATIONS", 118f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("ScreenSubtitle", root.transform, 2730f, 335f, 820f, 100f, "FIRST RESPONSE", 40f, Muted, TextAlignmentOptions.MidlineLeft);

            RectTransform chapterRail = BuildChapterRail(root.transform, out RectTransform[] chapterCards);
            RectTransform strategicMap = BuildStrategicMap(
                root.transform, out RawImage districtMap, out RectTransform[] missionNodes,
                out Button[] missionNodeButtons, out RectTransform[] progressNodes);
            RectTransform missionBriefing = BuildMissionBriefing(
                root.transform, out RawImage missionPreview, out TMP_Text missionNumber,
                out TMP_Text missionName, out TMP_Text missionBriefingText,
                out TMP_Text primaryObjectiveText, out TMP_Text rewardSummaryText);
            BuildFooter(root.transform, out Button archive, out Button intel, out Button launch, out TMP_Text launchLabel);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", backRoute);
            SetReference(serialized, "chapterRail", chapterRail);
            SetReference(serialized, "strategicMap", strategicMap);
            SetReference(serialized, "missionBriefing", missionBriefing);
            SetArray(serialized, "chapterCards", chapterCards);
            SetArray(serialized, "missionNodes", missionNodes);
            SetArray(serialized, "missionNodeButtons", missionNodeButtons);
            SetArray(serialized, "progressNodes", progressNodes);
            SetReference(serialized, "districtMapImage", districtMap);
            SetReference(serialized, "missionPreviewImage", missionPreview);
            SetReference(serialized, "m01MissionPreview", AssetDatabase.LoadAssetAtPath<Texture2D>(MissionPreviewPath));
            SetReference(serialized, "m02MissionPreview", AssetDatabase.LoadAssetAtPath<Texture2D>(M02MissionPreviewPath));
            SetReference(serialized, "screenTitle", screenTitle);
            SetReference(serialized, "missionNumber", missionNumber);
            SetReference(serialized, "missionName", missionName);
            SetReference(serialized, "missionBriefingText", missionBriefingText);
            SetReference(serialized, "primaryObjectiveText", primaryObjectiveText);
            SetReference(serialized, "rewardSummaryText", rewardSummaryText);
            SetReference(serialized, "launchMissionLabel", launchLabel);
            SetReference(serialized, "storyArchiveButton", archive);
            SetReference(serialized, "chapterIntelButton", intel);
            SetReference(serialized, "launchMissionButton", launch);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CampaignMissionScreenBinder binder = root.AddComponent<CampaignMissionScreenBinder>();
            binder.Configure(screen, "saga.ch01.m01.first_contact");
            return root;
        }

        private static RectTransform BuildChapterRail(Transform root, out RectTransform[] chapterCards)
        {
            Transform panel = CreatePanel("ChapterRail", root, 80f, 500f, 1260f, 1420f);
            SetVerticalStretch(panel as RectTransform, 500f, 380f);
            chapterCards = new[]
            {
                CreateChapterCard(panel, 0, "CHAPTER I", "FIRST RESPONSE", "5 MISSIONS", ChapterOneArtPath, true),
                CreateChapterCard(panel, 1, "CHAPTER II", "BROKEN GRID", "0 / 5", ChapterTwoArtPath, false),
                CreateChapterCard(panel, 2, "CHAPTER III", "HIDDEN NETWORK", "0 / 5", MapPath, false),
                CreateChapterCard(panel, 3, "CHAPTER IV", "AIR AND ARMOR", "0 / 5", ChapterFourArtPath, false),
                CreateChapterCard(panel, 4, "CHAPTER V", "CITYWIDE COMMAND", "0 / 5", ChapterOneArtPath, false)
            };
            NormalizeVerticalChildren(panel, 1420f, "PanelFill");
            return panel as RectTransform;
        }

        private static RectTransform CreateChapterCard(Transform panel, int index, string chapter, string title, string progress, string artPath, bool selected)
        {
            float y = 24f + index * 275f;
            Image row = CreateFramed(
                $"Chapter_{index + 1:00}",
                panel,
                28f,
                y,
                1204f,
                250f,
                selected ? SelectedSpritePath : DefaultCardSpritePath,
                selected ? Color.white : new Color(0.58f, 0.56f, 0.48f, 1f));
            Image fill = CreateSolid("Fill", row.transform, 16f, 16f, 1172f, 218f, selected ? SelectedTint : RowTint);
            fill.transform.SetAsFirstSibling();
            SetFullStretchMargins(fill.rectTransform, 16f);
            RawImage thumbnail = CreateCroppedPreview("Thumbnail", row.transform, 28f, 28f, 218f, 194f, artPath);
            thumbnail.color = selected ? Color.white : new Color(0.62f, 0.62f, 0.57f, 0.74f);
            TMP_Text chapterLabel = CreateText("Chapter", row.transform, 280f, 16f, 700f, 62f, chapter, 64f, selected ? Text : Muted, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(chapterLabel, 42f, 64f);
            TMP_Text titleLabel = CreateText("Title", row.transform, 280f, 88f, 720f, 54f, title, 48f, selected ? Text : Muted, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(titleLabel, 32f, 48f);
            TMP_Text progressLabel = CreateText("Progress", row.transform, 280f, 160f, 560f, 42f, progress, 36f, selected ? Olive : Muted, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(progressLabel, 26f, 36f);
            if (selected)
            {
                CreateText("Selected", row.transform, 1080f, 68f, 82f, 104f, ">", 76f, Gold, TextAlignmentOptions.Center);
            }
            else
            {
                CreateIcon("Lock", row.transform, LockIconPath, 1090f, 88f, 70f, 70f, Muted);
            }
            NormalizeVerticalChildren(row.transform, 250f, "Fill");
            return row.rectTransform;
        }

        private static RectTransform BuildStrategicMap(
            Transform root,
            out RawImage districtMap,
            out RectTransform[] missionNodes,
            out Button[] missionNodeButtons,
            out RectTransform[] progressNodes)
        {
            Transform panel = CreatePanel("StrategicMap", root, 1380f, 500f, 1770f, 1420f);
            SetVerticalStretch(panel as RectTransform, 500f, 380f);
            CreateText("Title", panel, 55f, 22f, 1660f, 90f, "SAHRIN DISTRICT", 82f, Text, TextAlignmentOptions.Center);
            CreateIcon("ChapterBadge", panel, StarIconPath, 1600f, 24f, 88f, 88f, Gold);
            Image mapFrame = CreateFramed("MapFrame", panel, 38f, 120f, 1694f, 970f, DetailPanelSpritePath, Color.white);
            SetVerticalStretch(mapFrame.rectTransform, 120f, 335f);
            districtMap = CreateCroppedPreview("DistrictMap", panel, 55f, 140f, 1660f, 930f, MapPath);
            SetVerticalStretch(districtMap.rectTransform, 140f, 355f);

            RectTransform mapOverlay = CreateRect("MissionRouteOverlay", panel, 55f, 140f, 1660f, 930f).GetComponent<RectTransform>();
            SetVerticalStretch(mapOverlay, 140f, 355f);
            Image route = CreateFramed("MissionRoute", mapOverlay, 215f, 448f, 1230f, 34f, RouteSpritePath, new Color(1f, 0.82f, 0.30f, 0.98f), false);
            SetVerticalCenter(route.rectTransform, 0.50f);
            float[] nodeXs = { 215f, 522.5f, 830f, 1137.5f, 1445f };
            missionNodes = new RectTransform[nodeXs.Length];
            missionNodeButtons = new Button[nodeXs.Length];
            for (int i = 0; i < nodeXs.Length; i++)
            {
                missionNodes[i] = CreateMissionNode(
                    mapOverlay, i + 1, nodeXs[i], 465f, i == 0, out missionNodeButtons[i]);
                SetVerticalCenter(missionNodes[i], 0.50f);
            }

            Transform progress = CreateDetailPanel("ChapterProgress", panel, 38f, 1115f, 1694f, 285f);
            SetBottomAnchored(progress as RectTransform, 38f, 28f, 1694f, 285f);
            CreateText("Label", progress, 48f, 20f, 1120f, 68f, "CHAPTER PROGRESS", 44f, Muted, TextAlignmentOptions.MidlineLeft);
            CreateText("Value", progress, 1280f, 20f, 330f, 68f, "0 / 5", 50f, Cyan, TextAlignmentOptions.MidlineRight);
            CreateFramed("ProgressRoute", progress, 150f, 166f, 1394f, 30f, RouteSpritePath, new Color(0.55f, 0.52f, 0.42f, 0.92f), false);
            progressNodes = new RectTransform[nodeXs.Length];
            for (int i = 0; i < nodeXs.Length; i++)
                progressNodes[i] = CreateProgressNode(progress, i + 1, 150f + i * 348.5f, 181f, i == 0);
            return panel as RectTransform;
        }

        private static RectTransform CreateMissionNode(
            Transform parent, int mission, float centerX, float centerY, bool active, out Button button)
        {
            float size = active ? 224f : 190f;
            Image frame = CreateIcon(
                $"MissionNode_{mission:00}",
                parent,
                ActiveNodeSpritePath,
                centerX - size * 0.5f,
                centerY - size * 0.5f,
                size,
                size,
                active ? Color.white : new Color(0.34f, 0.35f, 0.32f, 1f));
            button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.interactable = active;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.56f, 1f);
            colors.pressedColor = new Color(0.78f, 0.66f, 0.32f, 1f);
            colors.disabledColor = new Color(0.46f, 0.46f, 0.42f, 0.72f);
            button.colors = colors;
            if (active)
            {
                Image glow = CreateIcon("ActiveGlow", frame.transform, ActiveNodeSpritePath, -24f, -24f, size + 48f, size + 48f, new Color(1f, 0.72f, 0.12f, 0.30f));
                glow.transform.SetAsFirstSibling();
            }
            CreateText("Number", frame.transform, 0f, 0f, size, size, mission.ToString("00"), active ? 72f : 60f, active ? new Color(1f, 0.82f, 0.24f, 1f) : Muted, TextAlignmentOptions.Center);
            if (!active)
                CreateIcon("Lock", frame.transform, LockIconPath, size * 0.5f - 27f, size - 66f, 54f, 54f, Muted);
            return frame.rectTransform;
        }

        private static RectTransform CreateProgressNode(Transform parent, int mission, float centerX, float centerY, bool active)
        {
            const float size = 110f;
            Image frame = CreateIcon(
                $"ProgressNode_{mission:00}",
                parent,
                ActiveNodeSpritePath,
                centerX - size * 0.5f,
                centerY - size * 0.5f,
                size,
                size,
                active ? Color.white : new Color(0.34f, 0.35f, 0.32f, 1f));
            CreateText("Number", frame.transform, 0f, 0f, size, size, mission.ToString("00"), 34f, active ? Gold : Muted, TextAlignmentOptions.Center);
            return frame.rectTransform;
        }

        private static RectTransform BuildMissionBriefing(
            Transform root, out RawImage missionPreview, out TMP_Text missionNumber,
            out TMP_Text missionName, out TMP_Text missionBriefingText,
            out TMP_Text primaryObjectiveText, out TMP_Text rewardSummaryText)
        {
            Transform panel = CreatePanel("MissionBriefing", root, 3190f, 500f, 1530f, 1420f);
            SetVerticalStretch(panel as RectTransform, 500f, 380f);
            missionNumber = CreateText("MissionNumber", panel, 55f, 28f, 680f, 68f, "MISSION 01", 56f, Olive, TextAlignmentOptions.MidlineLeft);
            Transform available = CreateDetailPanel("Availability", panel, 1135f, 26f, 330f, 78f);
            CreateText("Label", available, 10f, 0f, 310f, 78f, "AVAILABLE", 42f, Olive, TextAlignmentOptions.Center);
            NormalizeVerticalChildren(available, 78f, "PanelFill");
            missionName = CreateText("MissionName", panel, 55f, 104f, 1420f, 94f, "BLACKOUT AT SAHRIN", 84f, Text, TextAlignmentOptions.MidlineLeft);

            CreateFramed("PreviewFrame", panel, 45f, 205f, 1440f, 410f, DetailPanelSpritePath, Color.white);
            missionPreview = CreateCroppedPreview("MissionPreview", panel, 62f, 222f, 1406f, 376f, MissionPreviewPath);
            missionBriefingText = CreateText("Briefing", panel, 62f, 635f, 1406f, 130f,
                "Enemy jamming has cut communications across Sahrin District. Secure the eastern relay and bring the network back online.",
                38f, Text, TextAlignmentOptions.TopLeft);
            missionBriefingText.textWrappingMode = TextWrappingModes.Normal;
            missionBriefingText.overflowMode = TextOverflowModes.Ellipsis;

            Transform objective = CreateDetailPanel("PrimaryObjective", panel, 45f, 785f, 1440f, 125f);
            CreateIcon("Icon", objective, ObjectiveIconPath, 28f, 18f, 90f, 90f, Gold);
            primaryObjectiveText = CreateText("Label", objective, 145f, 0f, 1230f, 125f, "ELIMINATE THE HOSTILE PATROL", 68f, Text, TextAlignmentOptions.MidlineLeft);
            NormalizeVerticalChildren(objective, 125f, "PanelFill");

            CreateMetricCard("CivilianRisk", panel, 45f, 935f, 695f, "CIVILIAN RISK", "MED", CivilianIconPath, Gold);
            CreateMetricCard("IntelConfidence", panel, 790f, 935f, 695f, "INTEL CONFIDENCE", "HIGH", IntelIconPath, Olive);

            Transform rewards = CreateDetailPanel("Rewards", panel, 45f, 1150f, 1440f, 220f);
            CreateText("Title", rewards, 30f, 16f, 320f, 50f, "REWARDS", 38f, Muted, TextAlignmentOptions.MidlineLeft);
            rewardSummaryText = CreateText(
                "RewardSummary", rewards, 30f, 70f, 1360f, 104f,
                "260 XP  |  1,200 CREDITS", 50f, Cyan, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(rewardSummaryText, 34f, 50f);
            NormalizeVerticalChildren(rewards, 220f, "PanelFill");
            NormalizeVerticalChildren(panel, 1420f, "PanelFill");
            return panel as RectTransform;
        }

        private static void CreateMetricCard(string name, Transform parent, float x, float y, float width, string label, string value, string iconPath, Color valueColor)
        {
            Transform card = CreateDetailPanel(name, parent, x, y, width, 190f);
            CreateIcon("Icon", card, iconPath, 30f, 42f, 104f, 104f, valueColor);
            CreateText("Label", card, 165f, 22f, width - 195f, 58f, label, 40f, Muted, TextAlignmentOptions.MidlineLeft);
            CreateText("Value", card, 165f, 84f, width - 195f, 72f, value, 60f, valueColor, TextAlignmentOptions.MidlineLeft);
            NormalizeVerticalChildren(card, 190f, "PanelFill");
        }

        private static void CreateStarGoal(Transform parent, float x, string lineOne, string lineTwo)
        {
            CreateIcon("Star", parent, StarIconPath, x + 60f, 16f, 110f, 110f, Gold);
            CreateText("LineOne", parent, x, 122f, 230f, 42f, lineOne, 29f, Text, TextAlignmentOptions.Center);
            CreateText("LineTwo", parent, x, 164f, 230f, 38f, lineTwo, 25f, Muted, TextAlignmentOptions.Center);
        }

        private static void BuildFooter(
            Transform root, out Button archive, out Button intel, out Button launch,
            out TMP_Text launchLabel)
        {
            archive = CreateButton("StoryArchiveButton", root, 120f, 1890f, 1300f, 230f, "STORY ARCHIVE", SecondarySpritePath, 82f, Text, out TMP_Text archiveLabel);
            SetBottomAnchored(archive.GetComponent<RectTransform>(), 120f, 55f, 1300f, 230f);
            CreateIcon("Icon", archive.transform, ArchiveIconPath, 72f, 54f, 118f, 118f, Muted);
            SetTextRect(archiveLabel.rectTransform, 220f, 0f, 980f, 230f);

            intel = CreateButton("ChapterIntelButton", root, 1500f, 1890f, 1250f, 230f, "CHAPTER INTEL", SecondarySpritePath, 82f, Text, out TMP_Text intelLabel);
            SetBottomAnchored(intel.GetComponent<RectTransform>(), 1500f, 55f, 1250f, 230f);
            CreateIcon("Icon", intel.transform, IntelIconPath, 72f, 54f, 118f, 118f, Olive);
            SetTextRect(intelLabel.rectTransform, 220f, 0f, 930f, 230f);

            launch = CreateButton("LaunchMissionButton", root, 2810f, 1870f, 1910f, 250f, "START OPERATION", GoldSpritePath, 108f, Text, out launchLabel);
            SetBottomAnchored(launch.GetComponent<RectTransform>(), 2810f, 35f, 1910f, 250f);
            UIShellRouteButtonView briefingRoute = launch.gameObject.AddComponent<UIShellRouteButtonView>();
            briefingRoute.Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.MissionBriefing, true);
            CreateIcon("LeftChevron", launch.transform, LeftChevronPath, 95f, 70f, 150f, 105f, new Color(0.82f, 0.61f, 0.17f, 0.72f));
            CreateIcon("RightChevron", launch.transform, RightChevronPath, 1665f, 70f, 150f, 105f, new Color(0.82f, 0.61f, 0.17f, 0.72f));
            AddButtonBacking(archive, new Color(0.025f, 0.03f, 0.028f, 0.96f));
            AddButtonBacking(intel, new Color(0.025f, 0.03f, 0.028f, 0.96f));
            AddButtonBacking(launch, new Color(0.17f, 0.13f, 0.045f, 0.985f));
            SetUnavailable(archive);
            SetUnavailable(intel);
        }

        private static void SetUnavailable(Button button)
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.70f, 0.70f, 0.65f, 0.72f);
            button.colors = colors;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string label,
            string spritePath,
            float fontSize,
            Color labelColor,
            out TMP_Text labelText)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image image = root.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.68f, 1f);
            colors.pressedColor = new Color(0.76f, 0.64f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.42f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            labelText = CreateText("Label", root.transform, 20f, 0f, width - 40f, height, label, fontSize, labelColor, TextAlignmentOptions.Center);
            return button;
        }

        private static Transform CreatePanel(string name, Transform parent, float x, float y, float width, float height)
        {
            Image frame = CreateFramed(name, parent, x, y, width, height, PanelSpritePath, Color.white);
            Image fill = CreateSolid("PanelFill", frame.transform, 22f, 22f, width - 44f, height - 44f, PanelTint);
            fill.transform.SetAsFirstSibling();
            SetFullStretchMargins(fill.rectTransform, 22f);
            return frame.transform;
        }

        private static Transform CreateDetailPanel(string name, Transform parent, float x, float y, float width, float height)
        {
            Image frame = CreateFramed(name, parent, x, y, width, height, DetailPanelSpritePath, Color.white);
            Image fill = CreateSolid("PanelFill", frame.transform, 10f, 12f, width - 20f, height - 24f, new Color(0.022f, 0.029f, 0.027f, 0.985f));
            fill.transform.SetAsFirstSibling();
            SetFullStretchMargins(fill.rectTransform, 12f);
            return frame.transform;
        }

        private static Image CreateFramed(string name, Transform parent, float x, float y, float width, float height, string spritePath, Color tint, bool sliced = true)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image image = root.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateCroppedPreview(string name, Transform parent, float x, float y, float width, float height, string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new InvalidOperationException($"Missing preview texture: {texturePath}");

            GameObject root = CreateRect(name, parent, x, y, width, height);
            RawImage image = root.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;

            float sourceAspect = texture.width / (float)texture.height;
            float targetAspect = width / height;
            if (sourceAspect > targetAspect)
            {
                float visibleWidth = Mathf.Clamp01(targetAspect / sourceAspect);
                image.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
            else
            {
                float visibleHeight = Mathf.Clamp01(sourceAspect / targetAspect);
                image.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            return image;
        }

        private static void AddButtonBacking(Button button, Color color)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Image backing = CreateSolid("Backing", button.transform, 12f, 12f, rect.rect.width - 24f, rect.rect.height - 24f, color);
            backing.transform.SetAsFirstSibling();
        }

        private static TMP_Text CreateText(string name, Transform parent, float x, float y, float width, float height, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = fontSize >= 36f
                ? (boldFont != null ? boldFont : TMP_Settings.defaultFontAsset)
                : (mediumFont != null ? mediumFont : TMP_Settings.defaultFontAsset);
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            return text;
        }

        private static void ConfigureAutoSize(TMP_Text text, float minimumSize, float maximumSize)
        {
            if (text == null)
                return;

            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = maximumSize;
        }

        private static Image CreateIcon(string name, Transform parent, string path, float x, float y, float width, float height, Color? tint = null)
        {
            Image image = CreateFramed(name, parent, x, y, width, height, path, tint ?? Color.white, false);
            image.preserveAspect = true;
            return image;
        }

        private static GameObject CreateRect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject root = new(name, typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return root;
        }

        private static void SetTextRect(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetVerticalStretch(RectTransform rect, float top, float bottom)
        {
            if (rect == null)
                return;

            float x = rect.anchoredPosition.x;
            float width = rect.sizeDelta.x;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(x, bottom);
            rect.offsetMax = new Vector2(x + width, -top);
        }

        private static void SetBottomAnchored(RectTransform rect, float x, float bottom, float width, float height)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetVerticalCenter(RectTransform rect, float normalizedY)
        {
            if (rect == null)
                return;

            float x = rect.anchoredPosition.x;
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;
            float y = Mathf.Clamp01(normalizedY);
            rect.anchorMin = new Vector2(0f, y);
            rect.anchorMax = new Vector2(0f, y);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void NormalizeVerticalChildren(Transform parent, float designHeight, string excludedChildName)
        {
            if (parent == null || designHeight <= 0f)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform rect = parent.GetChild(i) as RectTransform;
                if (rect == null || rect.name == excludedChildName)
                    continue;

                float x = rect.anchoredPosition.x;
                float width = rect.sizeDelta.x;
                float top = -rect.anchoredPosition.y;
                float height = rect.sizeDelta.y;
                float anchorTop = 1f - top / designHeight;
                float anchorBottom = 1f - (top + height) / designHeight;
                rect.anchorMin = new Vector2(0f, anchorBottom);
                rect.anchorMax = new Vector2(0f, anchorTop);
                rect.pivot = new Vector2(0f, 1f);
                rect.offsetMin = new Vector2(x, 0f);
                rect.offsetMax = new Vector2(x + width, 0f);
            }
        }

        private static void SetFullStretchMargins(RectTransform rect, float margin)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                content = ResolveComponentInHierarchy<UIShellContentView>(root.transform);
                if (content != null)
                    break;
            }

            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");

            SerializedObject serialized = new(content);
            SerializedProperty campaignPrefab = serialized.FindProperty("campaignContentPrefab");
            if (campaignPrefab == null)
                throw new InvalidOperationException("UIShellContentView is missing campaignContentPrefab.");
            campaignPrefab.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T ResolveComponentInHierarchy<T>(Transform root) where T : Component
        {
            if (root == null)
                return null;

            T component = root.GetComponent<T>();
            if (component != null)
                return component;

            for (int i = 0; i < root.childCount; i++)
            {
                component = ResolveComponentInHierarchy<T>(root.GetChild(i));
                if (component != null)
                    return component;
            }

            return null;
        }

        private static void RenderCameraToPng(Camera camera, string path, int width, int height)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D texture = null;
            try
            {
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                if (target != null)
                    UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string ResolveCapturePath()
        {
            string configured = Environment.GetEnvironmentVariable("WARLINE_CAMPAIGN_CAPTURE_PATH");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                ? "/private/tmp/warline-scn05-campaign.png"
                : configured.Trim());
        }

        private static int ResolvePositiveEnvironmentInt(string name, int fallback)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return int.TryParse(configured, out int value) && value > 0 ? value : fallback;
        }

        private static void ImportProductionArt()
        {
            AssetDatabase.ImportAsset(MapPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(MissionPreviewPath, ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath) == null)
                throw new InvalidOperationException($"Missing Campaign map art at {MapPath}.");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(MissionPreviewPath) == null)
                throw new InvalidOperationException($"Missing Campaign preview art at {MissionPreviewPath}.");
        }

        private static void LoadStyleAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null)
                throw new InvalidOperationException($"Missing Campaign display font at {BoldFontPath}.");
            if (mediumFont == null)
                throw new InvalidOperationException($"Missing Campaign body font at {MediumFontPath}.");
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing UI sprite: {path}");
            return sprite;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property {propertyName} on {serialized.targetObject.GetType().Name}.");
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized array {propertyName} on {serialized.targetObject.GetType().Name}.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
