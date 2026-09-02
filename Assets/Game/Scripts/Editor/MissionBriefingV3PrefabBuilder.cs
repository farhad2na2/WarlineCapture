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
    public static class MissionBriefingV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab";
        private const string MissionArtPath = "Assets/Game/Art/UI/V3Shared/MissionBriefing/SCN06_ForwardPost_V3.png";
        private const string EnemyPortraitPath = "Assets/Game/Art/UI/V3Shared/Portraits/SCN06_EnemyOfficer_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(55, 68, 72, 255);
        private static readonly Color BorderLight = new Color32(113, 125, 127, 255);
        private static readonly Color DarkTop = new Color32(24, 32, 35, 252);
        private static readonly Color DarkBottom = new Color32(3, 8, 10, 254);
        private static readonly Color BlueTop = new Color32(23, 111, 190, 255);
        private static readonly Color BlueBottom = new Color32(3, 52, 101, 255);
        private static readonly Color GreenTop = new Color32(80, 145, 45, 255);
        private static readonly Color GreenBottom = new Color32(22, 73, 24, 255);
        private static readonly Color GrayTop = new Color32(57, 64, 66, 255);
        private static readonly Color GrayBottom = new Color32(17, 23, 25, 255);
        private static readonly Color Olive = new Color32(150, 188, 48, 255);
        private static readonly Color Red = new Color32(235, 63, 28, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Texture2D missionArt;
        private static Sprite enemyPortrait;

        [MenuItem("Game/UI/V3/Rebuild Mission Briefing Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform root = CreateRect("SCN06_MissionBriefingContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateGradientPanel(root, new Color32(24, 30, 32, 255), new Color32(2, 7, 9, 255), Color.clear, 0f);
            RectTransform composition = CreateTopLeft("MissionBriefingComposition", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            MainMenuV3SectionLayoutView responsiveLayout =
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            MissionBriefingScreenView screen = composition.gameObject.AddComponent<MissionBriefingScreenView>();

            BuildHeader(composition);
            BuildMissionOverview(
                composition,
                out RectTransform missionOverview,
                out RawImage missionArtImage,
                out TMP_Text screenTitle,
                out TMP_Text missionNumber,
                out TMP_Text missionTitle,
                out TMP_Text chapterLabel,
                out TMP_Text missionSummary);
            BuildIntelColumns(
                composition,
                out RectTransform objectivesPanel,
                out RectTransform conditionsPanel,
                out RectTransform enemyIntelPanel,
                out RectTransform rewardsPanel,
                out TMP_Text[] objectiveLabels,
                out TMP_Text[] conditionNames,
                out TMP_Text[] conditionValues,
                out TMP_Text enemyIntelLabel,
                out RectTransform[] rewardRows,
                out TMP_Text[] rewardLabels,
                out TMP_Text[] rewardValues);
            BuildFooter(
                composition,
                out UIShellRouteButtonView backRoute,
                out Button deploy,
                out Toggle replayToggle,
                out TMP_Text replayLabel);

            RectTransform hiddenProgress = CreateTopLeft("ChapterProgress_RuntimeOnly", composition, 0f, 0f, 1f, 1f);
            hiddenProgress.gameObject.SetActive(false);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", backRoute);
            SetReference(serialized, "missionOverview", missionOverview);
            SetReference(serialized, "primaryObjectives", objectivesPanel);
            SetReference(serialized, "tacticalConditions", conditionsPanel);
            SetReference(serialized, "enemyIntel", enemyIntelPanel);
            SetReference(serialized, "chapterProgress", hiddenProgress);
            SetReference(serialized, "rewards", rewardsPanel);
            SetArray(serialized, "progressNodes", Array.Empty<RectTransform>());
            SetReference(serialized, "missionArtImage", missionArtImage);
            SetReference(serialized, "m01MissionArt", missionArt);
            SetReference(serialized, "m02MissionArt", missionArt);
            SetReference(serialized, "screenTitle", screenTitle);
            SetReference(serialized, "screenSubtitle", chapterLabel);
            SetReference(serialized, "missionNumber", missionNumber);
            SetReference(serialized, "missionTitle", missionTitle);
            SetReference(serialized, "operationCodename", null);
            SetReference(serialized, "missionSummary", missionSummary);
            SetReference(serialized, "locationLabel", null);
            SetArray(serialized, "objectiveLabels", objectiveLabels);
            SetArray(serialized, "conditionLabels", conditionValues);
            SetArray(serialized, "conditionNameLabels", conditionNames);
            SetReference(serialized, "enemyIntelLabel", enemyIntelLabel);
            SetArray(serialized, "rewardRows", rewardRows);
            SetArray(serialized, "rewardLabels", rewardLabels);
            SetArray(serialized, "rewardValues", rewardValues);
            SetReference(serialized, "replayTutorialToggle", replayToggle);
            SetReference(serialized, "replayTutorialLabel", replayLabel);
            SetReference(serialized, "deployOperationButton", deploy);
            SetBool(serialized, "v3TargetLayout", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CampaignMissionScreenBinder binder = composition.gameObject.AddComponent<CampaignMissionScreenBinder>();
            binder.Configure(screen, "saga.ch01.m02.establish_base");

            ConfigureResponsiveLayout(
                responsiveLayout,
                composition,
                missionOverview,
                objectivesPanel,
                conditionsPanel,
                enemyIntelPanel,
                rewardsPanel,
                deploy);

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MissionBriefingV3PrefabBuilder] result=Passed layout=1672x941 borders=3 gradients=procedural art=aspect-preserved");
        }

        private static void ConfigureResponsiveLayout(
            MainMenuV3SectionLayoutView layout,
            RectTransform composition,
            RectTransform missionOverview,
            RectTransform objectivesPanel,
            RectTransform conditionsPanel,
            RectTransform enemyIntelPanel,
            RectTransform rewardsPanel,
            Button deploy)
        {
            RectTransform deployRect = deploy.GetComponent<RectTransform>();
            layout.Configure(
                ReferenceResolution,
                MainMenuV3SectionAlignment.Center,
                new[]
                {
                    composition.Find("CreditsChip") as RectTransform,
                    composition.Find("CommandChip") as RectTransform,
                    composition.Find("SettingsButton") as RectTransform,
                    objectivesPanel,
                    conditionsPanel,
                    enemyIntelPanel,
                    composition.Find("EnemyCommander") as RectTransform,
                    rewardsPanel,
                    composition.Find("StarGoals") as RectTransform,
                    deploy.transform.Find("RightChevrons") as RectTransform
                },
                true,
                new[]
                {
                    deploy.transform.Find("Label") as RectTransform
                },
                new[]
                {
                    missionOverview,
                    missionOverview.Find("MissionArtClip") as RectTransform,
                    missionOverview.Find("MissionCopyOverlay") as RectTransform,
                    missionOverview.Find("TitleBand") as RectTransform,
                    missionOverview.Find("TitleRule") as RectTransform,
                    deployRect
                });
        }

        [MenuItem("Game/UI/V3/Validate Mission Briefing Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Mission Briefing V3 prefab: {PrefabPath}");
            MissionBriefingScreenView screen = prefab.GetComponentInChildren<MissionBriefingScreenView>(true);
            if (screen == null || !screen.V3TargetLayout || screen.MissionArtImage == null || screen.DeployOperationButton == null)
                throw new MissingReferenceException("Mission Briefing V3 runtime binding is incomplete.");
            if (AssetDatabase.GetAssetPath(screen.MissionArtImage.texture) != MissionArtPath)
                throw new MissingReferenceException("Mission Briefing V3 must use the canonical forward-post plate.");
            AspectRatioFitter artFitter = screen.MissionArtImage.GetComponent<AspectRatioFitter>();
            if (artFitter == null || artFitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("Mission Briefing art must use an aspect-preserving cover fit.");
            Image portrait = FindImage(prefab.transform, "EnemyCommanderPortrait");
            if (portrait == null || portrait.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("Enemy commander portrait must preserve aspect under its crop mask.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 12)
                throw new InvalidOperationException($"Mission Briefing V3 requires procedural gradients; found {gradients}.");
            Debug.Log($"[MissionBriefingV3PrefabBuilder] validation=Passed gradients={gradients} images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        private static void LoadAssets()
        {
            ConfigureTexture(MissionArtPath, 4096);
            ConfigureSprite(EnemyPortraitPath, 2048);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            missionArt = AssetDatabase.LoadAssetAtPath<Texture2D>(MissionArtPath);
            enemyPortrait = RequireSprite(EnemyPortraitPath);
            if (boldFont == null || mediumFont == null || missionArt == null)
                throw new MissingReferenceException("Mission Briefing V3 fonts or art are missing.");
        }

        private static void BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 10f, 9f, 374f, 93f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            BuildResourceChip(root, "CreditsChip", 909f, 9f, 302f, 93f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "CommandChip", 1219f, 9f, 318f, 93f, catalog.CommandIcon, "COMMAND", "8,430");
            Button settings = CreateGradientButton("SettingsButton", root, 1545f, 9f, 113f, 93f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetRect(gear.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), Vector2.zero);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 15f, 16f, 58f, 58f);
            TMP_Text labelText = CreateText("Label", chip, label, 21f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 87f, 9f, width - 96f, 32f);
            TMP_Text valueText = CreateText("Value", chip, value, 35f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 87f, 38f, width - 96f, 46f);
        }

        private static void BuildMissionOverview(
            RectTransform root,
            out RectTransform missionOverview,
            out RawImage missionArtImage,
            out TMP_Text screenTitle,
            out TMP_Text missionNumber,
            out TMP_Text missionTitle,
            out TMP_Text chapterLabel,
            out TMP_Text missionSummary)
        {
            missionOverview = CreateTopLeft("MissionOverview", root, 12f, 112f, 911f, 675f);
            CreateGradientPanel(missionOverview, DarkTop, DarkBottom, theme.Amber, 3f);
            RectTransform clip = CreateTopLeft("MissionArtClip", missionOverview, 3f, 3f, 905f, 669f);
            clip.gameObject.AddComponent<RectMask2D>();
            missionArtImage = CreateRawImage("MissionArt", clip, missionArt, Color.white);
            Stretch(missionArtImage.rectTransform);
            AddCover(missionArtImage, missionArt);

            RectTransform overlay = CreateTopLeft("MissionCopyOverlay", missionOverview, 3f, 3f, 905f, 667f);
            V3GradientGraphic overlayGradient = CreateGradientPanel(
                overlay,
                new Color(0.005f, 0.008f, 0.008f, 0.86f),
                new Color(0.005f, 0.008f, 0.008f, 0.08f),
                Color.clear,
                0f);
            overlayGradient.raycastTarget = false;

            RectTransform titleBand = CreateTopLeft("TitleBand", missionOverview, 3f, 3f, 905f, 65f);
            CreateGradientPanel(titleBand, new Color(0.04f, 0.055f, 0.06f, 0.96f), new Color(0.012f, 0.018f, 0.02f, 0.90f), Color.clear, 0f);
            CreateSolidTopLeft("TitleRule", missionOverview, 3f, 67f, 905f, 3f, BorderLight);
            Image titleIcon = CreateImage("TitleIcon", titleBand, catalog.AttackIcon, theme.Amber, false);
            SetTopLeft(titleIcon.rectTransform, 16f, 13f, 42f, 42f);
            screenTitle = CreateText("ScreenTitle", titleBand, "MISSION BRIEFING", 38f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(screenTitle.rectTransform, 75f, 2f, 600f, 60f);

            missionNumber = CreateText("MissionNumber", missionOverview, "M02", 57f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(missionNumber.rectTransform, 25f, 82f, 190f, 66f);
            missionTitle = CreateText("MissionTitle", missionOverview, "ESTABLISH THE BASE", 55f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(missionTitle.rectTransform, 25f, 139f, 820f, 69f);
            chapterLabel = CreateText("ChapterLabel", missionOverview, "CHAPTER I - FIRST RESPONSE", 26f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(chapterLabel.rectTransform, 26f, 205f, 500f, 43f);
            TMP_Text briefingLabel = CreateText("BriefingLabel", missionOverview, "SITUATION BRIEFING", 22f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(briefingLabel.rectTransform, 26f, 258f, 380f, 32f);
            missionSummary = CreateText(
                "MissionSummary",
                missionOverview,
                "Reopen the abandoned JRC forward\npost before the Ash Line reaches it.\nEstablish a foothold and prepare\nfor incoming threats.",
                20f,
                mediumFont,
                TextAlignmentOptions.TopLeft,
                theme.TextPrimary);
            SetTopLeft(missionSummary.rectTransform, 26f, 294f, 320f, 130f);
            missionSummary.enableWordWrapping = true;
            missionSummary.overflowMode = TextOverflowModes.Overflow;
        }

        private static void BuildIntelColumns(
            RectTransform root,
            out RectTransform objectivesPanel,
            out RectTransform conditionsPanel,
            out RectTransform enemyIntelPanel,
            out RectTransform rewardsPanel,
            out TMP_Text[] objectiveLabels,
            out TMP_Text[] conditionNames,
            out TMP_Text[] conditionValues,
            out TMP_Text enemyIntelLabel,
            out RectTransform[] rewardRows,
            out TMP_Text[] rewardLabels,
            out TMP_Text[] rewardValues)
        {
            objectivesPanel = CreateTopLeft("PrimaryObjectives", root, 935f, 118f, 375f, 262f);
            CreateGradientPanel(objectivesPanel, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(objectivesPanel, catalog.AttackIcon, "PRIMARY OBJECTIVES", Olive);
            objectiveLabels = new TMP_Text[4];
            string[] objectiveCopy = { "RESTORE COMMAND POST", "BUILD BARRACK", "PRODUCE RIFLE SQUAD", "HOLD PERIMETER" };
            string[] objectiveIcons =
            {
                V3UiFoundationBuilder.MissionRadioIconPath,
                V3UiFoundationBuilder.CampaignBarracksIconPath,
                V3UiFoundationBuilder.CampaignSquadIconPath,
                V3UiFoundationBuilder.CampaignHoldIconPath
            };
            for (int i = 0; i < 4; i++)
                objectiveLabels[i] = BuildIconRow(objectivesPanel, 14f, 64f + i * 48f, 345f, 42f, RequireSprite(objectiveIcons[i]), objectiveCopy[i], 20f, theme.TextPrimary, null);

            conditionsPanel = CreateTopLeft("TacticalConditions", root, 1319f, 118f, 333f, 262f);
            CreateGradientPanel(conditionsPanel, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(conditionsPanel, RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath), "TACTICAL CONDITIONS", Olive);
            conditionNames = new TMP_Text[3];
            conditionValues = new TMP_Text[3];
            string[] conditionCopy = { "CIVILIAN RISK", "INTEL CONFIDENCE", "VISIBILITY" };
            string[] conditionValueCopy = { "MED", "HIGH", "CLEAR" };
            string[] conditionIcons =
            {
                V3UiFoundationBuilder.MissionCivilianIconPath,
                V3UiFoundationBuilder.MissionIntelIconPath,
                V3UiFoundationBuilder.MissionVisibilityIconPath
            };
            for (int i = 0; i < 3; i++)
            {
                float y = 68f + i * 58f;
                Image icon = CreateImage("Icon" + i, conditionsPanel, RequireSprite(conditionIcons[i]), new Color32(225, 204, 159, 255), false);
                SetTopLeft(icon.rectTransform, 15f, y, 38f, 38f);
                conditionNames[i] = CreateText("ConditionName" + i, conditionsPanel, conditionCopy[i], 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(conditionNames[i].rectTransform, 65f, y - 2f, 185f, 42f);
                conditionValues[i] = CreateText("ConditionValue" + i, conditionsPanel, conditionValueCopy[i], 19f, boldFont, TextAlignmentOptions.MidlineRight, i == 0 ? theme.Amber : Olive);
                SetTopLeft(conditionValues[i].rectTransform, 246f, y - 2f, 72f, 42f);
            }

            enemyIntelPanel = CreateTopLeft("EnemyIntel", root, 935f, 391f, 375f, 225f);
            CreateGradientPanel(enemyIntelPanel, DarkTop, DarkBottom, Red, 3f);
            BuildPanelTitle(enemyIntelPanel, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), "ENEMY INTEL", Red);
            enemyIntelLabel = BuildIconRow(enemyIntelPanel, 14f, 67f, 345f, 42f, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), "TUTORIAL CELL", 19f, theme.TextPrimary, null);
            BuildIconRow(enemyIntelPanel, 14f, 117f, 345f, 42f, RequireSprite(V3UiFoundationBuilder.MissionVehicleIconPath), "LIGHT VEHICLES", 19f, theme.TextPrimary, "LOW");
            BuildIconRow(enemyIntelPanel, 14f, 166f, 345f, 42f, RequireSprite(V3UiFoundationBuilder.MissionAirIconPath), "AIR THREAT", 19f, theme.TextPrimary, "NONE");

            RectTransform portraitPanel = CreateTopLeft("EnemyCommander", root, 1319f, 391f, 333f, 225f);
            CreateGradientPanel(portraitPanel, DarkTop, DarkBottom, Border, 3f);
            RectTransform portraitClip = CreateTopLeft("PortraitClip", portraitPanel, 3f, 3f, 327f, 219f);
            portraitClip.gameObject.AddComponent<RectMask2D>();
            Image portrait = CreateImage("EnemyCommanderPortrait", portraitClip, enemyPortrait, Color.white, false);
            Stretch(portrait.rectTransform);
            AddCover(portrait, enemyPortrait);

            rewardsPanel = CreateTopLeft("Rewards", root, 935f, 627f, 437f, 154f);
            CreateGradientPanel(rewardsPanel, new Color32(7, 37, 51, 255), DarkBottom, theme.Blue, 3f);
            BuildPanelTitle(rewardsPanel, RequireSprite(V3UiFoundationBuilder.MissionStarIconPath), "REWARDS", theme.Blue);
            rewardRows = new RectTransform[3];
            rewardLabels = new TMP_Text[3];
            rewardValues = new TMP_Text[3];
            Sprite[] rewardIcons = { RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath), catalog.CreditsIcon, RequireSprite(V3UiFoundationBuilder.CampaignBarracksIconPath) };
            string[] labels = { "COMMANDER XP", "CREDITS", "BARRACK" };
            string[] values = { "+260", "+1,500", "UNLOCK" };
            for (int i = 0; i < 3; i++)
            {
                float x = 14f + i * 139f;
                rewardRows[i] = CreateTopLeft("RewardRow" + i, rewardsPanel, x, 57f, 130f, 88f);
                if (i > 0) CreateSolidTopLeft("Divider", rewardRows[i], 0f, 2f, 2f, 78f, Border);
                Image icon = CreateImage("Icon", rewardRows[i], rewardIcons[i], i == 2 ? Color.white : theme.Amber, false);
                SetTopLeft(icon.rectTransform, 4f, 15f, 47f, 47f);
                rewardLabels[i] = CreateText("Label", rewardRows[i], labels[i], 9.5f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(rewardLabels[i].rectTransform, 54f, 8f, 76f, 30f);
                rewardValues[i] = CreateText("Value", rewardRows[i], values[i], i == 2 ? 16f : 20f, boldFont, TextAlignmentOptions.MidlineLeft, Olive);
                SetTopLeft(rewardValues[i].rectTransform, 54f, 38f, 76f, 36f);
            }

            RectTransform starPanel = CreateTopLeft("StarGoals", root, 1380f, 627f, 272f, 154f);
            CreateGradientPanel(starPanel, new Color32(5, 31, 45, 255), DarkBottom, theme.Blue, 3f);
            BuildPanelTitle(starPanel, RequireSprite(V3UiFoundationBuilder.MissionStarIconPath), "STAR GOALS", theme.Blue);
            string[] goals = { "COMPLETE MISSION", "BUILD UNDER 5:00", "NO BASE BREACH" };
            for (int i = 0; i < 3; i++)
            {
                Image star = CreateImage("Star" + i, starPanel, RequireSprite(V3UiFoundationBuilder.MissionStarIconPath), theme.Blue, false);
                SetTopLeft(star.rectTransform, 13f, 61f + i * 29f, 24f, 24f);
                TMP_Text goal = CreateText("Goal" + i, starPanel, goals[i], 16f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(goal.rectTransform, 47f, 56f + i * 29f, 210f, 32f);
            }
        }

        private static TMP_Text BuildIconRow(Transform parent, float x, float y, float width, float height, Sprite iconSprite, string label, float size, Color color, string value)
        {
            RectTransform row = CreateTopLeft("Row_" + label.Replace(' ', '_'), parent, x, y, width, height);
            Image icon = CreateImage("Icon", row, iconSprite, new Color32(225, 204, 159, 255), false);
            SetTopLeft(icon.rectTransform, 0f, 2f, 38f, 38f);
            TMP_Text text = CreateText("Label", row, label, size, boldFont, TextAlignmentOptions.MidlineLeft, color);
            SetTopLeft(text.rectTransform, 52f, 0f, value == null ? width - 52f : width - 120f, height);
            if (!string.IsNullOrEmpty(value))
            {
                TMP_Text valueText = CreateText("Value", row, value, size, boldFont, TextAlignmentOptions.MidlineRight, value == "LOW" ? theme.Amber : Olive);
                SetTopLeft(valueText.rectTransform, width - 72f, 0f, 72f, height);
            }
            return text;
        }

        private static void BuildPanelTitle(RectTransform panel, Sprite iconSprite, string title, Color color)
        {
            Image icon = CreateImage("HeaderIcon", panel, iconSprite, color, false);
            SetTopLeft(icon.rectTransform, 14f, 13f, 38f, 38f);
            TMP_Text label = CreateText("HeaderLabel", panel, title, 21f, boldFont, TextAlignmentOptions.MidlineLeft, color);
            SetTopLeft(label.rectTransform, 64f, 8f, panel.sizeDelta.x - 78f, 48f);
            CreateSolidTopLeft("HeaderRule", panel, 13f, 54f, panel.sizeDelta.x - 26f, 2f, Border);
        }

        private static void BuildFooter(RectTransform root, out UIShellRouteButtonView backRoute, out Button deploy, out Toggle replayToggle, out TMP_Text replayLabel)
        {
            Button back = CreateGradientButton("BackButton", root, 12f, 796f, 294f, 127f, GrayTop, GrayBottom, BorderLight, 3f);
            Image backIcon = CreateImage("Icon", back.transform, RequireSprite(V3UiFoundationBuilder.CommanderBackIconPath), Color.white, false);
            SetTopLeft(backIcon.rectTransform, 26f, 35f, 55f, 55f);
            TMP_Text backText = CreateText("Label", back.transform, "BACK", 42f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(backText.rectTransform, 80f, 15f, 195f, 96f);
            backRoute = back.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.Campaign, false);

            Button loadout = CreateGradientButton("LoadoutButton", root, 318f, 796f, 336f, 127f, BlueTop, BlueBottom, theme.Blue, 3f);
            Image loadoutIcon = CreateImage("Icon", loadout.transform, RequireSprite(V3UiFoundationBuilder.CampaignSquadIconPath), Color.white, false);
            SetTopLeft(loadoutIcon.rectTransform, 36f, 31f, 66f, 66f);
            TMP_Text loadoutText = CreateText("Label", loadout.transform, "LOADOUT", 42f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(loadoutText.rectTransform, 100f, 15f, 220f, 96f);
            loadout.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.LoadoutSquadPrep, true);

            deploy = CreateGradientButton("DeployOperationButton", root, 666f, 796f, 986f, 127f, GreenTop, GreenBottom, theme.Green, 3f);
            RectTransform leftHolder = CreateTopLeft("LeftChevrons", deploy.transform, 38f, 35f, 74f, 55f);
            Image left = CreateImage("Icon", leftHolder, RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), Olive, false);
            Stretch(left.rectTransform);
            left.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            Image right = CreateImage("RightChevrons", deploy.transform, RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), Olive, false);
            SetTopLeft(right.rectTransform, 868f, 35f, 74f, 55f);
            TMP_Text deployText = CreateText("Label", deploy.transform, "DEPLOY OPERATION", 49f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(deployText.rectTransform, 135f, 12f, 716f, 100f);

            RectTransform replayRoot = CreateTopLeft("ReplayTutorial_RuntimeOnly", root, 0f, 0f, 1f, 1f);
            Image replayGraphic = replayRoot.gameObject.AddComponent<Image>();
            replayToggle = replayRoot.gameObject.AddComponent<Toggle>();
            replayToggle.targetGraphic = replayGraphic;
            replayToggle.graphic = replayGraphic;
            replayLabel = CreateText("ReplayTutorialLabel", replayRoot, string.Empty, 1f, mediumFont, TextAlignmentOptions.Center, Color.clear);
            replayRoot.gameObject.SetActive(false);
        }

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
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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

        private static void AddCover(RawImage image, Texture texture)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
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

        private static void ConfigureTexture(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Mission Briefing V3 texture: {path}");
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Mission Briefing V3 sprite: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing Mission Briefing V3 sprite: {path}");
            return sprite;
        }

        private static Image FindImage(Transform root, string name)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
                if (images[i].name == name)
                    return images[i];
            return null;
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

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName) ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.boolValue = value;
        }
    }
}
