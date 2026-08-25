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
    public static class MissionBriefingPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MissionArtPath = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P01.png";
        private const string M02MissionArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Barrack_Action_512.png";
        private const string PanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_01_popup_outer_frame.png";
        private const string DetailPanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_03_detail_panel_frame.png";
        private const string SecondarySpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_15_secondary_dark_cta_frame.png";
        private const string GoldSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_14_primary_gold_cta_frame.png";
        private const string BackIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png";
        private const string ObjectiveIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_attack_crosshair.png";
        private const string CivilianIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_civilian_group.png";
        private const string ShieldIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_hold_shield.png";
        private const string StarIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_objective_star.png";
        private const string IntelIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_scan_radar.png";
        private const string VehicleIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_12_vehicle.png";
        private const string AirIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_aircraft_helicopter.png";
        private const string CreditsIconPath = CanonicalUiResourceIconPaths.Credits;
        private const string RankIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_02_commander_rank_shield.png";
        private const string ActiveNodeSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png";
        private const string RouteSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png";
        private const string LeftChevronPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_chevron_left.png";
        private const string RightChevronPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_deploy_chevron_right.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color PanelTint = new(0.014f, 0.019f, 0.018f, 0.995f);
        private static readonly Color DetailTint = new(0.022f, 0.028f, 0.026f, 0.985f);
        private static readonly Color Gold = new(0.94f, 0.68f, 0.16f, 1f);
        private static readonly Color Olive = new(0.68f, 0.76f, 0.18f, 1f);
        private static readonly Color Cyan = new(0.20f, 0.72f, 0.88f, 1f);
        private static readonly Color Text = new(0.93f, 0.90f, 0.80f, 1f);
        private static readonly Color Muted = new(0.62f, 0.61f, 0.53f, 1f);
        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/Build SCN-06 Mission Briefing")]
        public static void Build()
        {
            GameObject prefab = BuildM01BriefingPrefab();
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MissionBriefingPrefabBuilder] result=Passed prefab={PrefabPath} scene={MenuScenePath}");
        }

        public static void BuildM01BriefingPrefabOnly()
        {
            BuildM01BriefingPrefab();
        }

        private static GameObject BuildM01BriefingPrefab()
        {
            ImportProductionArt();
            LoadStyleAssets();
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (IsCurrentM01BriefingPrefab(existing))
            {
                Debug.Log($"[MissionBriefingPrefabBuilder] result=Passed prefab={PrefabPath} scope=PrefabOnly reused=true");
                return existing;
            }

            GameObject root = BuildPrefabRoot();
            EnsureFolder("Assets/Game/Prefabs/UI/Shell/Content");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Mission Briefing prefab at {PrefabPath}.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MissionBriefingPrefabBuilder] result=Passed prefab={PrefabPath} scope=PrefabOnly");
            return prefab;
        }

        private static bool IsCurrentM01BriefingPrefab(GameObject prefab)
        {
            if (prefab == null) return false;
            MissionBriefingScreenView view = prefab.GetComponent<MissionBriefingScreenView>();
            if (view == null || prefab.GetComponent<CampaignMissionScreenBinder>() == null ||
                view.MissionArtImage == null || view.MissionArtImage.texture == null ||
                view.MissionTitle == null || view.DeployOperationButton == null ||
                view.MissionNumber == null || view.OperationCodename == null ||
                view.ConditionNameLabels is not { Length: 2 } ||
                view.RewardLabels is not { Length: 3 } ||
                view.ReplayTutorialToggle == null ||
                AssetDatabase.GetAssetPath(view.MissionArtImage.texture) != MissionArtPath)
                return false;

            TMP_Text[] text = prefab.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < text.Length; index++)
            {
                string value = text[index].text;
                if (!string.IsNullOrEmpty(value) &&
                    (value.Contains("BLACKOUT AT SAHRIN", StringComparison.Ordinal) ||
                     value.Contains("RESTORE THE RELAY", StringComparison.Ordinal) ||
                     value.Contains("+1,200", StringComparison.Ordinal)))
                    return false;
            }

            return true;
        }

        [MenuItem("Game/UI/Capture SCN-06 Mission Briefing")]
        public static void CaptureMissionBriefing()
        {
            string path = ResolveCapturePath();
            int width = ResolvePositiveEnvironmentInt("WARLINE_MISSION_BRIEFING_CAPTURE_WIDTH", 1920);
            int height = ResolvePositiveEnvironmentInt("WARLINE_MISSION_BRIEFING_CAPTURE_HEIGHT", 1080);
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = ResolveComponentInScene<MenuBootstrapView>(scene);
            if (bootstrap == null || bootstrap.ContentSystem == null || bootstrap.UiCamera == null || bootstrap.UiCanvas == null)
                throw new InvalidOperationException("Menu scene is missing its configured Canvas bootstrap references.");

            bootstrap.ApplyRuntimeUiMode();
            bootstrap.ContentSystem.PrepareForCommandSequence(new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.EnterMenu,
                    default,
                    UIRoute.Campaign,
                    UiShellMode.MainMenu,
                    1)
            });
            bootstrap.ContentSystem.InstallMenuRouteBody(UIRoute.MissionBriefing);
            Canvas.ForceUpdateCanvases();
            RenderCameraToPng(bootstrap.UiCamera, path, width, height);
            Debug.Log($"[MissionBriefingCapture] result=Passed size={width}x{height} path={path}");
        }

        private static GameObject BuildPrefabRoot()
        {
            GameObject root = CreateRect("SCN06_MissionBriefingContent", null, 0f, 0f, 4800f, 2160f);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            MissionBriefingScreenView screen = root.AddComponent<MissionBriefingScreenView>();

            Image bodyScrim = CreateSolid("BodyScrim", root.transform, 0f, 280f, 4800f, 1880f, new Color(0.004f, 0.007f, 0.006f, 0.95f));
            SetVerticalStretch(bodyScrim.rectTransform, 280f, 0f);

            Button backButton = CreateButton("BackButton", root.transform, 90f, 300f, 540f, 165f, "BACK", SecondarySpritePath, 70f, Text, out TMP_Text backLabel);
            UIShellRouteButtonView backRoute = backButton.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.Campaign, false);
            CreateIcon("BackIcon", backButton.transform, BackIconPath, 40f, 40f, 82f, 82f);
            SetTextRect(backLabel.rectTransform, 135f, 0f, 350f, 165f);

            TMP_Text screenTitle = CreateText("ScreenTitle", root.transform, 690f, 292f, 2080f, 175f, "MISSION BRIEFING", 118f, Text, TextAlignmentOptions.MidlineLeft);
            TMP_Text screenSubtitle = CreateText("ScreenSubtitle", root.transform, 2970f, 335f, 1720f, 100f, "FIRST RESPONSE  /  MISSION 01", 40f, Muted, TextAlignmentOptions.MidlineRight);

            RectTransform overview = BuildMissionOverview(
                root.transform, out RawImage missionArt, out TMP_Text missionNumber,
                out TMP_Text missionTitle, out TMP_Text operationCodename,
                out TMP_Text missionSummary, out TMP_Text locationLabel);
            RectTransform objectives = BuildObjectives(root.transform, out TMP_Text[] objectiveLabels);
            RectTransform conditions = BuildConditions(
                root.transform, out TMP_Text[] conditionNameLabels, out TMP_Text[] conditionLabels);
            RectTransform intel = BuildEnemyIntel(root.transform, out TMP_Text enemyIntelLabel);
            RectTransform progress = BuildChapterProgress(root.transform, out RectTransform[] progressNodes);
            BuildFooter(
                root.transform, out RectTransform rewards, out Button deploy,
                out RectTransform[] rewardRows, out TMP_Text[] rewardLabels,
                out TMP_Text[] rewardValues, out Toggle replayTutorialToggle,
                out TMP_Text replayTutorialLabel);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", backRoute);
            SetReference(serialized, "missionOverview", overview);
            SetReference(serialized, "primaryObjectives", objectives);
            SetReference(serialized, "tacticalConditions", conditions);
            SetReference(serialized, "enemyIntel", intel);
            SetReference(serialized, "chapterProgress", progress);
            SetReference(serialized, "rewards", rewards);
            SetArray(serialized, "progressNodes", progressNodes);
            SetReference(serialized, "missionArtImage", missionArt);
            SetReference(serialized, "m01MissionArt", AssetDatabase.LoadAssetAtPath<Texture2D>(MissionArtPath));
            SetReference(serialized, "m02MissionArt", AssetDatabase.LoadAssetAtPath<Texture2D>(M02MissionArtPath));
            SetReference(serialized, "screenTitle", screenTitle);
            SetReference(serialized, "screenSubtitle", screenSubtitle);
            SetReference(serialized, "missionNumber", missionNumber);
            SetReference(serialized, "missionTitle", missionTitle);
            SetReference(serialized, "operationCodename", operationCodename);
            SetReference(serialized, "missionSummary", missionSummary);
            SetReference(serialized, "locationLabel", locationLabel);
            SetArray(serialized, "objectiveLabels", objectiveLabels);
            SetArray(serialized, "conditionLabels", conditionLabels);
            SetArray(serialized, "conditionNameLabels", conditionNameLabels);
            SetReference(serialized, "enemyIntelLabel", enemyIntelLabel);
            SetArray(serialized, "rewardRows", rewardRows);
            SetArray(serialized, "rewardLabels", rewardLabels);
            SetArray(serialized, "rewardValues", rewardValues);
            SetReference(serialized, "replayTutorialToggle", replayTutorialToggle);
            SetReference(serialized, "replayTutorialLabel", replayTutorialLabel);
            SetReference(serialized, "deployOperationButton", deploy);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CampaignMissionScreenBinder binder = root.AddComponent<CampaignMissionScreenBinder>();
            binder.Configure(screen, "saga.ch01.m01.first_contact");
            return root;
        }

        private static RectTransform BuildMissionOverview(
            Transform root, out RawImage missionArt, out TMP_Text missionNumber,
            out TMP_Text missionTitle, out TMP_Text operationCodename,
            out TMP_Text missionSummary, out TMP_Text locationLabel)
        {
            Transform panel = CreatePanel("MissionOverview", root, 80f, 500f, 2920f, 1060f);
            CreateFramed("MissionArtFrame", panel, 30f, 30f, 2860f, 1000f, DetailPanelSpritePath, Color.white);
            missionArt = CreateCroppedPreview("MissionArt", panel, 48f, 48f, 2824f, 964f, MissionArtPath);

            CreateSolid("IdentityScrim", panel, 54f, 52f, 1370f, 620f, new Color(0.006f, 0.009f, 0.008f, 0.82f));
            missionNumber = CreateText("MissionNumber", panel, 92f, 84f, 1000f, 64f, "MISSION 01", 54f, Gold, TextAlignmentOptions.MidlineLeft);
            missionTitle = CreateText("MissionTitle", panel, 92f, 164f, 1260f, 112f, string.Empty, 88f, Text, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(missionTitle, 58f, 88f);
            CreateIcon("OperationIcon", panel, ObjectiveIconPath, 92f, 310f, 104f, 104f, Gold);
            operationCodename = CreateText("OperationCodename", panel, 224f, 310f, 1050f, 104f, "FIRST CONTACT", 56f, Olive, TextAlignmentOptions.MidlineLeft);
            CreateDivider(panel, 92f, 440f, 1050f, new Color(0.66f, 0.55f, 0.29f, 0.65f));
            CreateIcon("LocationIcon", panel, IntelIconPath, 92f, 480f, 92f, 92f, Muted);
            locationLabel = CreateText("Location", panel, 220f, 476f, 1050f, 100f, string.Empty, 48f, Text, TextAlignmentOptions.MidlineLeft);

            CreateSolid("SituationScrim", panel, 54f, 720f, 1450f, 270f, new Color(0.006f, 0.009f, 0.008f, 0.86f));
            CreateText("SituationLabel", panel, 92f, 752f, 1260f, 54f, "SITUATION BRIEFING", 40f, Olive, TextAlignmentOptions.MidlineLeft);
            missionSummary = CreateText(
                "SituationBody",
                panel,
                92f,
                824f,
                1300f,
                130f,
                string.Empty,
                35f,
                Text,
                TextAlignmentOptions.TopLeft);
            ConfigureWrappedText(missionSummary, 30f, 35f);
            return panel as RectTransform;
        }

        private static RectTransform BuildObjectives(Transform root, out TMP_Text[] labels)
        {
            Transform panel = CreateDetailPanel("PrimaryObjectives", root, 3030f, 500f, 1690f, 470f);
            CreateSectionHeader(panel, "PRIMARY OBJECTIVES", ObjectiveIconPath, Olive);
            labels = new[]
            {
                CreateObjectiveRow(panel, "Objective_01", ObjectiveIconPath, "OBJECTIVE 01", 146f, Gold),
                CreateObjectiveRow(panel, "Objective_02", ShieldIconPath, "OBJECTIVE 02", 244f, Text),
                CreateObjectiveRow(panel, "Objective_03", CivilianIconPath, string.Empty, 342f, Text)
            };
            return panel as RectTransform;
        }

        private static RectTransform BuildConditions(
            Transform root, out TMP_Text[] nameLabels, out TMP_Text[] labels)
        {
            Transform panel = CreateDetailPanel("TacticalConditions", root, 3030f, 1000f, 1690f, 330f);
            CreateSectionHeader(panel, "TACTICAL CONDITIONS", ShieldIconPath, Olive);
            nameLabels = new TMP_Text[2];
            labels = new TMP_Text[2];
            labels[0] = CreateMetricRow(
                panel, "CommandRestrictions", CivilianIconPath, "BUILDING / PRODUCTION",
                "PENDING", 148f, Gold, out nameLabels[0]);
            labels[1] = CreateMetricRow(
                panel, "SupportRestrictions", IntelIconPath, "ECONOMY / TRANSPORT / AIR",
                "PENDING", 238f, Gold, out nameLabels[1]);
            return panel as RectTransform;
        }

        private static RectTransform BuildEnemyIntel(Transform root, out TMP_Text enemyIntelLabel)
        {
            Transform panel = CreateDetailPanel("EnemyIntel", root, 3030f, 1360f, 1690f, 420f);
            CreateSectionHeader(panel, "ENEMY INTEL", IntelIconPath, Olive);
            enemyIntelLabel = CreateMetricRow(panel, "HostileForce", ObjectiveIconPath, "HOSTILE FORCE", "PENDING", 148f, Gold);
            CreateMetricRow(panel, "MissionRoles", VehicleIconPath, "SOURCE", "MISSION CATALOG", 238f, Gold);
            CreateMetricRow(panel, "AirThreat", AirIconPath, "AIR SUPPORT", "DISABLED", 328f, Olive);
            return panel as RectTransform;
        }

        private static RectTransform BuildChapterProgress(Transform root, out RectTransform[] progressNodes)
        {
            Transform panel = CreateDetailPanel("ChapterProgress", root, 80f, 1590f, 2920f, 190f);
            CreateText("Label", panel, 48f, 44f, 700f, 92f, "CHAPTER PROGRESS", 44f, Olive, TextAlignmentOptions.MidlineLeft);
            CreateFramed("ProgressRoute", panel, 900f, 92f, 1100f, 26f, RouteSpritePath, new Color(0.74f, 0.58f, 0.20f, 0.88f), false);
            progressNodes = new RectTransform[5];
            for (int i = 0; i < progressNodes.Length; i++)
                progressNodes[i] = CreateProgressNode(panel, i + 1, 900f + i * 275f, 105f, i == 0);
            return panel as RectTransform;
        }

        private static void BuildFooter(
            Transform root, out RectTransform rewards, out Button deploy,
            out RectTransform[] rewardRows, out TMP_Text[] rewardLabels, out TMP_Text[] rewardValues,
            out Toggle replayTutorialToggle, out TMP_Text replayTutorialLabel)
        {
            Transform rewardPanel = CreatePanel("Rewards", root, 80f, 1810f, 2920f, 305f);
            rewards = rewardPanel as RectTransform;
            SetBottomAnchored(rewards, 80f, 45f, 2920f, 305f);
            CreateText("Title", rewardPanel, 54f, 32f, 420f, 90f, "REWARDS", 56f, Olive, TextAlignmentOptions.MidlineLeft);
            rewardRows = new RectTransform[3];
            rewardLabels = new TMP_Text[3];
            rewardValues = new TMP_Text[3];
            rewardRows[0] = CreateRewardMetric(rewardPanel, "Reward01", RankIconPath, "REWARD 01", "PENDING", 470f, Gold, out rewardLabels[0], out rewardValues[0]);
            CreateDivider(rewardPanel, 1120f, 40f, 1f, new Color(0.58f, 0.49f, 0.28f, 0.62f), 220f);
            rewardRows[1] = CreateRewardMetric(rewardPanel, "Reward02", CreditsIconPath, "REWARD 02", "PENDING", 1180f, Gold, out rewardLabels[1], out rewardValues[1]);
            CreateDivider(rewardPanel, 1830f, 40f, 1f, new Color(0.58f, 0.49f, 0.28f, 0.62f), 220f);
            rewardRows[2] = CreateRewardMetric(rewardPanel, "Reward03", ObjectiveIconPath, "REWARD 03", "PENDING", 1890f, Gold, out rewardLabels[2], out rewardValues[2]);
            RectTransform tutorialRoot = CreateRect("ReplayTutorial", rewardPanel, 2200f, 38f, 660f, 225f).GetComponent<RectTransform>();
            Image toggleBackground = CreateSolid("ToggleBackground", tutorialRoot, 10f, 76f, 90f, 90f, new Color(0.12f, 0.14f, 0.11f, 1f));
            Image toggleCheck = CreateSolid("ToggleCheck", toggleBackground.transform, 18f, 18f, 54f, 54f, Gold);
            replayTutorialToggle = tutorialRoot.gameObject.AddComponent<Toggle>();
            replayTutorialToggle.targetGraphic = toggleBackground;
            replayTutorialToggle.graphic = toggleCheck;
            replayTutorialToggle.isOn = false;
            replayTutorialLabel = CreateText("Label", tutorialRoot, 130f, 62f, 500f, 120f, "REPLAY TUTORIAL", 38f, Text, TextAlignmentOptions.MidlineLeft);

            deploy = CreateButton("DeployOperationButton", root, 3030f, 1810f, 1690f, 305f, "DEPLOY OPERATION", GoldSpritePath, 98f, Muted, out _);
            SetBottomAnchored(deploy.GetComponent<RectTransform>(), 3030f, 45f, 1690f, 305f);
            CreateIcon("LeftChevron", deploy.transform, LeftChevronPath, 72f, 92f, 150f, 105f, new Color(0.82f, 0.61f, 0.17f, 0.62f));
            CreateIcon("RightChevron", deploy.transform, RightChevronPath, 1468f, 92f, 150f, 105f, new Color(0.82f, 0.61f, 0.17f, 0.62f));
            AddButtonBacking(deploy, new Color(0.17f, 0.13f, 0.045f, 0.985f));
            deploy.interactable = true;
        }

        private static void CreateSectionHeader(Transform panel, string label, string iconPath, Color iconTint)
        {
            CreateIcon("HeaderIcon", panel, iconPath, 52f, 34f, 86f, 86f, iconTint);
            CreateText("Header", panel, 166f, 34f, 1400f, 86f, label, 48f, Olive, TextAlignmentOptions.MidlineLeft);
            CreateDivider(panel, 52f, 126f, 1580f, new Color(0.56f, 0.48f, 0.29f, 0.62f));
        }

        private static TMP_Text CreateObjectiveRow(Transform panel, string name, string iconPath, string label, float y, Color color)
        {
            Transform row = CreateRect(name, panel, 52f, y, 1580f, 86f).transform;
            CreateIcon("Icon", row, iconPath, 10f, 3f, 78f, 78f, color);
            TMP_Text text = CreateText("Label", row, 116f, 0f, 1430f, 86f, label, 41f, Text, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(text, 32f, 41f);
            return text;
        }

        private static TMP_Text CreateMetricRow(Transform panel, string name, string iconPath, string label, string value, float y, Color valueColor)
        {
            return CreateMetricRow(panel, name, iconPath, label, value, y, valueColor, out _);
        }

        private static TMP_Text CreateMetricRow(
            Transform panel, string name, string iconPath, string label, string value,
            float y, Color valueColor, out TMP_Text labelText)
        {
            Transform row = CreateRect(name, panel, 52f, y, 1580f, 80f).transform;
            CreateIcon("Icon", row, iconPath, 10f, 1f, 76f, 76f, Muted);
            labelText = CreateText("Label", row, 116f, 0f, 760f, 80f, label, 39f, Text, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(labelText, 30f, 39f);
            TMP_Text valueText = CreateText("Value", row, 900f, 0f, 640f, 80f, value, 38f, valueColor, TextAlignmentOptions.MidlineRight);
            ConfigureAutoSize(valueText, 24f, 38f);
            return valueText;
        }

        private static RectTransform CreateRewardMetric(
            Transform panel, string name, string iconPath, string label, string value,
            float x, Color valueColor, out TMP_Text labelText, out TMP_Text valueText)
        {
            Transform metric = CreateRect(name, panel, x, 38f, 620f, 225f).transform;
            CreateIcon("Icon", metric, iconPath, 12f, 34f, 150f, 150f, valueColor);
            labelText = CreateText("Label", metric, 172f, 34f, 430f, 70f, label, 36f, Text, TextAlignmentOptions.MidlineLeft);
            valueText = CreateText("Value", metric, 172f, 108f, 430f, 78f, value, 54f, valueColor, TextAlignmentOptions.MidlineLeft);
            ConfigureAutoSize(labelText, 24f, 36f);
            ConfigureAutoSize(valueText, 34f, 54f);
            return metric as RectTransform;
        }

        private static RectTransform CreateProgressNode(Transform parent, int mission, float centerX, float centerY, bool active)
        {
            float size = active ? 118f : 104f;
            Image frame = CreateIcon(
                $"ProgressNode_{mission:00}",
                parent,
                ActiveNodeSpritePath,
                centerX - size * 0.5f,
                centerY - size * 0.5f,
                size,
                size,
                active ? Color.white : new Color(0.34f, 0.35f, 0.32f, 1f));
            CreateText("Number", frame.transform, 0f, 0f, size, size, mission.ToString("00"), active ? 38f : 34f, active ? Gold : Muted, TextAlignmentOptions.Center);
            return frame.rectTransform;
        }

        private static Button CreateButton(string name, Transform parent, float x, float y, float width, float height, string label, string spritePath, float fontSize, Color labelColor, out TMP_Text labelText)
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
            Image fill = CreateSolid("PanelFill", frame.transform, 12f, 12f, width - 24f, height - 24f, DetailTint);
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

        private static void CreateDivider(Transform parent, float x, float y, float width, Color color, float height = 3f)
        {
            CreateSolid("Divider", parent, x, y, width, height, color);
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
            Image backing = CreateSolid("Backing", button.transform, 12f, 12f, rect.sizeDelta.x - 24f, rect.sizeDelta.y - 24f, color);
            backing.transform.SetAsFirstSibling();
        }

        private static void SetUnavailable(Button button)
        {
            button.interactable = false;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.70f, 0.70f, 0.65f, 0.72f);
            button.colors = colors;
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
            text.enableAutoSizing = true;
            text.fontSizeMin = minimumSize;
            text.fontSizeMax = maximumSize;
        }

        private static void ConfigureWrappedText(TMP_Text text, float minimumSize, float maximumSize)
        {
            ConfigureAutoSize(text, minimumSize, maximumSize);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
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
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetVerticalStretch(RectTransform rect, float top, float bottom)
        {
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetFullStretchMargins(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");

            SerializedObject serialized = new(content);
            SerializedProperty briefingPrefab = serialized.FindProperty("missionBriefingContentPrefab");
            if (briefingPrefab == null)
                throw new InvalidOperationException("UIShellContentView is missing missionBriefingContentPrefab.");
            briefingPrefab.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T ResolveComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = ResolveComponentInHierarchy<T>(root.transform);
                if (result != null)
                    return result;
            }
            return null;
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
            string configured = Environment.GetEnvironmentVariable("WARLINE_MISSION_BRIEFING_CAPTURE_PATH");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                ? "/private/tmp/warline-scn06-mission-briefing.png"
                : configured.Trim());
        }

        private static int ResolvePositiveEnvironmentInt(string name, int fallback)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return int.TryParse(configured, out int value) && value > 0 ? value : fallback;
        }

        private static void ImportProductionArt()
        {
            AssetDatabase.ImportAsset(MissionArtPath, ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(MissionArtPath) == null)
                throw new InvalidOperationException($"Missing Mission Briefing art at {MissionArtPath}.");
        }

        private static void LoadStyleAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null)
                throw new InvalidOperationException($"Missing Mission Briefing display font at {BoldFontPath}.");
            if (mediumFont == null)
                throw new InvalidOperationException($"Missing Mission Briefing body font at {MediumFontPath}.");
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
