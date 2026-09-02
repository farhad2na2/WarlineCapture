#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Rebuilds SCN-13 from the locked V3 composition. All chrome is procedural;
    /// the only raster inputs are shared operation/unit art and shared V3 icons.
    /// </summary>
    public static class SkirmishSetupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab";
        private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string PreviewPath = "Assets/Game/Art/UI/Generated/SkirmishSetup/TargetLockV02/scn13_operation_preview_sahrin_v02.png";
        private const string TutorialArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_skirmish_thumbnail_art.png";
        private const string ConvoyArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_operations_thumbnail_art.png";
        private const string AirliftArtPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_art_attack_helicopter.png";
        private const string BreachArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_campaign_thumbnail_art.png";
        private const string HiddenCellArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Insurgent_Female_01_Rifle_Card_512.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(67, 78, 82, 255);
        private static readonly Color ThinRule = new Color32(48, 59, 62, 255);
        private static readonly Color DarkTop = new Color32(24, 33, 36, 255);
        private static readonly Color DarkBottom = new Color32(4, 10, 12, 255);
        private static readonly Color RaisedTop = new Color32(39, 47, 50, 255);
        private static readonly Color RaisedBottom = new Color32(10, 16, 18, 255);
        private static readonly Color SelectedTop = new Color32(105, 119, 30, 255);
        private static readonly Color SelectedBottom = new Color32(31, 44, 12, 255);
        private static readonly Color LaunchTop = new Color32(56, 145, 39, 255);
        private static readonly Color LaunchBottom = new Color32(13, 75, 30, 255);
        private static readonly Color Amber = new Color32(249, 176, 0, 255);
        private static readonly Color Lime = new Color32(137, 195, 42, 255);
        private static readonly Color Cyan = new Color32(0, 185, 232, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(175, 181, 178, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiArtCatalog catalog;
        private static Sprite preview;
        private static Sprite tutorialArt;
        private static Sprite convoyArt;
        private static Sprite airliftArt;
        private static Sprite breachArt;
        private static Sprite hiddenCellArt;

        [MenuItem("Game/UI/V3/Rebuild SCN-13 Skirmish Setup")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform rootRect = CreateRect(
                "SCN13_SkirmishSetupContent",
                null,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            GameObject root = rootRect.gameObject;
            QuickCustomScreenView screen = root.AddComponent<QuickCustomScreenView>();
            screen.SetRouteForTests(UIRoute.QuickCustomSetup);

            Image canvasBlack = CreateImage("CanvasBlack", root.transform, null, Color.black, false);
            Stretch(canvasBlack.rectTransform);
            RectTransform composition = CreateTopLeft("SkirmishSetupComposition", root.transform, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);

            var rightTargets = new List<RectTransform>();
            var widthTargets = new List<RectTransform>();
            BuildHeader(composition, rightTargets, widthTargets, out _);
            BuildPresetRail(composition);
            BuildOperationPreview(composition, widthTargets, out TMP_InputField seedInput, out TMP_Text mapName);
            BuildRules(
                composition,
                rightTargets,
                out UISegmentedControlView enemyCount,
                out UISegmentedControlView difficulty,
                out UISegmentedControlView startingCredits,
                out UISegmentedControlView startingResources,
                out UISliderRowView income,
                out UISegmentedControlView aggression,
                out UISegmentedControlView winCondition,
                out UIToggleRowView fog,
                out UIToggleRowView intel);
            BuildFooter(composition, rightTargets, widthTargets, out Button reset, out Button randomize, out Button launch);

            MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(
                ReferenceResolution,
                MainMenuV3SectionAlignment.Center,
                rightTargets.ToArray(),
                true,
                null,
                widthTargets.ToArray());

            SerializedObject serializedScreen = new(screen);
            SetReference(serializedScreen, "enemyCountStepper", enemyCount);
            SetReference(serializedScreen, "difficultySegmented", difficulty);
            SetReference(serializedScreen, "startingMoneySegmented", startingCredits);
            SetReference(serializedScreen, "incomeMultiplierSlider", income);
            SetReference(serializedScreen, "aggressionSegmented", aggression);
            SetReference(serializedScreen, "winConditionSegmented", winCondition);
            SetReference(serializedScreen, "startingResourcesSegmented", startingResources);
            SetReference(serializedScreen, "fogOfWarToggle", fog);
            SetReference(serializedScreen, "intelRevealToggle", intel);
            SetReference(serializedScreen, "seedInput", seedInput);
            SetReference(serializedScreen, "mapNameText", mapName);
            SetReference(serializedScreen, "resetButton", reset);
            SetReference(serializedScreen, "randomizeSeedButton", randomize);
            SetReference(serializedScreen, "launchButton", launch);
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Game/Prefabs/UI/Shell/Content");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Skirmish setup prefab: {PrefabPath}");

            RouteMainMenuSkirmishCard();
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[SkirmishSetupPrefabBuilder] result=Passed v3=True reference=1672x941 responsive=True");
        }

        [MenuItem("Game/UI/V3/Validate SCN-13 Skirmish Setup")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing SCN-13 prefab: {PrefabPath}");
            if (prefab.GetComponent<QuickCustomScreenView>() == null)
                throw new MissingComponentException("SCN-13 root must own QuickCustomScreenView.");
            if (Find(prefab.transform, "HeaderContent") != null)
                throw new InvalidOperationException("SCN-13 must preserve the shell header instead of installing a HeaderContent section.");
            Transform composition = Require(prefab.transform, "SkirmishSetupComposition");
            Transform presetRail = Require(composition, "PresetRail");
            for (int i = 0; i < 5; i++)
                Require(presetRail, $"Preset_{i}");
            Require(composition, "OperationPreview/MapPreviewClip/MapPreview");
            Require(composition, "OpposingForce/EnemyFactionStepper");
            Require(composition, "OpposingForce/Difficulty");
            Require(composition, "MatchEconomy/CompatibilityControls/StartingCredits");
            Require(composition, "MatchEconomy/CompatibilityControls/StartingResources");
            Require(composition, "MatchEconomy/CompatibilityControls/Income");
            Require(composition, "MatchEconomy/CompatibilityControls/Aggression");
            Require(composition, "MatchEconomy/CompatibilityControls/WinCondition");
            Require(composition, "MatchEconomy/IntelReveal");
            Toggle fog = Require(composition, "MatchEconomy/FogOfWar").GetComponentInChildren<Toggle>(true);
            if (fog == null || fog.interactable)
                throw new InvalidOperationException("SCN-13 Fog of War must stay visibly locked until its runtime exists.");

            RawImage map = Require(composition, "OperationPreview/MapPreviewClip/MapPreview").GetComponent<RawImage>();
            AspectRatioFitter fitter = map != null ? map.GetComponent<AspectRatioFitter>() : null;
            if (map == null || map.texture == null || fitter == null || fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("SCN-13 map preview must use an aspect-fill crop and never stretch.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != ReferenceResolution)
                throw new InvalidOperationException("SCN-13 must expand cleanly across 16:9 and 20:9 canvases.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 42)
                throw new InvalidOperationException($"SCN-13 requires procedural V3 gradients; found {gradients}.");
            Debug.Log($"[SkirmishSetupV3Validation] result=Passed gradients={gradients} presets=5");
        }

        private static void LoadAssets()
        {
            ConfigureSprite(PreviewPath, 4096);
            ConfigureSprite(TutorialArtPath, 2048);
            ConfigureSprite(ConvoyArtPath, 2048);
            ConfigureSprite(AirliftArtPath, 1024);
            ConfigureSprite(BreachArtPath, 2048);
            ConfigureSprite(HiddenCellArtPath, 1024);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            preview = RequireSprite(PreviewPath);
            tutorialArt = RequireSprite(TutorialArtPath);
            convoyArt = RequireSprite(ConvoyArtPath);
            airliftArt = RequireSprite(AirliftArtPath);
            breachArt = RequireSprite(BreachArtPath);
            hiddenCellArt = RequireSprite(HiddenCellArtPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("SCN-13 V3 fonts are missing.");
        }

        private static void BuildHeader(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            ICollection<RectTransform> widthTargets,
            out Button backButton)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 14f, 13f, 381f, 100f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);
            Image invisibleHit = CreateImage("BackButton", logo, null, Color.clear, true);
            Stretch(invisibleHit.rectTransform);
            backButton = invisibleHit.gameObject.AddComponent<Button>();
            backButton.targetGraphic = invisibleHit;
            backButton.transition = Selectable.Transition.None;
            backButton.gameObject.AddComponent<UIShellRouteButtonView>()
                .Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            RectTransform title = CreateTopLeft("ScreenTitlePanel", root, 396f, 13f, 574f, 100f);
            CreateGradientPanel(title, DarkTop, DarkBottom, Border, 3f);
            widthTargets.Add(title);
            TMP_Text titleText = CreateText("ScreenTitle", title, "SKIRMISH SETUP", 49f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(titleText.rectTransform, 39f, 18f, 6f, 76f);

            RectTransform credits = BuildResourceChip(root, "CreditsChip", 970f, 13f, 273f, 100f, catalog.CreditsIcon, "CREDITS", "24,750", Amber);
            RectTransform command = BuildResourceChip(root, "CommandChip", 1251f, 13f, 288f, 100f, catalog.CommandIcon, "COMMAND", "8,430", Cyan);
            Button settings = CreateGradientButton("SettingsButton", root, 1547f, 13f, 111f, 100f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, TextPrimary, false);
            SetCentered(gear.rectTransform, 58f, 58f);
            settings.gameObject.AddComponent<UIShellRouteButtonView>()
                .Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            rightTargets.Add(credits);
            rightTargets.Add(command);
            rightTargets.Add(settings.GetComponent<RectTransform>());
        }

        private static RectTransform BuildResourceChip(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Sprite icon,
            string label,
            string value,
            Color accent)
        {
            RectTransform chip = CreateTopLeft(name, parent, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, accent, false);
            SetTopLeft(iconImage.rectTransform, 17f, 18f, 61f, 61f);
            TMP_Text labelText = CreateText("Label", chip, label, 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(labelText.rectTransform, 94f, 7f, width - 102f, 34f);
            TMP_Text valueText = CreateText("Value", chip, value, 35f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(valueText.rectTransform, 94f, 37f, width - 102f, 50f);
            return chip;
        }

        private static void BuildPresetRail(RectTransform root)
        {
            RectTransform rail = CreateTopLeft("PresetRail", root, 14f, 125f, 381f, 671f);
            BuildPreset(rail, 0, 0f, 132f, "TUTORIAL\nINTERCEPT", tutorialArt, V3UiFoundationBuilder.MatchAttackIconPath, true);
            BuildPreset(rail, 1, 143f, 125f, "CONVOY\nPRESSURE", convoyArt, V3UiFoundationBuilder.MissionVehicleIconPath, false);
            BuildPreset(rail, 2, 282f, 125f, "AIRLIFT\nEXTRACTION", airliftArt, V3UiFoundationBuilder.MissionAirIconPath, false);
            BuildPreset(rail, 3, 416f, 125f, "BREACH\nASSAULT", breachArt, V3UiFoundationBuilder.OperationsRaidIconPath, false);
            BuildPreset(rail, 4, 551f, 120f, "HIDDEN CELL\nRAID", hiddenCellArt, V3UiFoundationBuilder.MissionEnemyIconPath, false);
        }

        private static void BuildPreset(
            Transform parent,
            int index,
            float y,
            float height,
            string label,
            Sprite art,
            string iconPath,
            bool selected)
        {
            RectTransform card = CreateTopLeft($"Preset_{index}", parent, 0f, y, 381f, height);
            CreateGradientPanel(card, selected ? SelectedTop : DarkTop, selected ? SelectedBottom : DarkBottom, selected ? Amber : Border, 3f);

            RectTransform iconCell = CreateTopLeft("IconCell", card, 3f, 3f, 88f, height - 6f);
            CreateGradientPanel(iconCell, selected ? new Color32(122, 83, 2, 255) : RaisedTop, DarkBottom, selected ? Amber : Border, 3f);
            Image icon = CreateImage("Icon", iconCell, RequireSprite(iconPath), selected ? Amber : TextMuted, false);
            SetCentered(icon.rectTransform, 55f, 55f);

            RectTransform artClip = CreateTopLeft("ArtClip", card, 92f, 3f, 228f, height - 6f);
            artClip.gameObject.AddComponent<RectMask2D>();
            RawImage image = CreateRawImage("Art", artClip, art.texture, selected ? Color.white : new Color(0.58f, 0.60f, 0.58f, 0.88f));
            AddCover(image, art.texture);
            CreateGradientOverlay("ArtShade", artClip, new Color(0f, 0f, 0f, 0.05f), new Color(0f, 0f, 0f, 0.72f));
            TMP_Text title = CreateText("Title", artClip, label, 25f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 19f, 8f, 203f, height - 16f);

            RectTransform stateCell = CreateTopLeft("StateCell", card, 320f, 3f, 58f, height - 6f);
            CreateGradientPanel(stateCell, RaisedTop, DarkBottom, selected ? Amber : Border, 3f);
            if (selected)
                CreateCheckMark(stateCell, Amber);
            else
                CreateLockIcon(stateCell, TextMuted);
        }

        private static void BuildOperationPreview(
            RectTransform root,
            ICollection<RectTransform> widthTargets,
            out TMP_InputField seedInput,
            out TMP_Text mapName)
        {
            RectTransform panel = CreateTopLeft("OperationPreview", root, 408f, 125f, 700f, 671f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            widthTargets.Add(panel);

            mapName = CreateText("OperationName", panel, "SAHRIN OUTSKIRTS", 39f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetHorizontalStretch(mapName.rectTransform, 5f, 5f, 0f, 71f);
            CreateSolidStretch("TitleRule", panel, 3f, 3f, 70f, 3f, Border);

            RectTransform previewClip = CreateHorizontalStretch("MapPreviewClip", panel, 4f, 4f, 73f, 365f);
            previewClip.gameObject.AddComponent<RectMask2D>();
            RawImage map = CreateRawImage("MapPreview", previewClip, preview.texture, Color.white);
            AddCover(map, preview.texture);
            CreateGradientOverlay("MapReadability", previewClip, new Color(0f, 0f, 0f, 0.02f), new Color(0f, 0f, 0f, 0.20f));

            RectTransform objective = CreateHorizontalStretch("ObjectiveRow", panel, 4f, 4f, 438f, 68f);
            CreateGradientPanel(objective, DarkTop, DarkBottom, Border, 3f);
            RectTransform targetIcon = CreateTopLeft("ObjectiveIcon", objective, 18f, 10f, 50f, 50f);
            CreateTargetIcon(targetIcon, Amber);
            TMP_Text objectiveText = CreateText("Objective", objective, "DESTROY ALL ENEMIES", 30f, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetHorizontalStretch(objectiveText.rectTransform, 94f, 16f, 4f, 59f);

            RectTransform metrics = CreateHorizontalStretch("Metrics", panel, 14f, 14f, 510f, 81f);
            RectTransform risk = CreateAnchoredHorizontal("CivilianRisk", metrics, 0f, .5f, 0f, 0f, 0f, 81f);
            CreateGradientPanel(risk, DarkTop, DarkBottom, Border, 3f);
            Image riskIcon = CreateImage("Icon", risk, RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath), Amber, false);
            SetTopLeft(riskIcon.rectTransform, 20f, 21f, 45f, 45f);
            CreateMetricText(risk, "CIVILIAN RISK", "MED", Amber);
            RectTransform intel = CreateAnchoredHorizontal("IntelConfidence", metrics, .5f, 1f, 0f, 0f, 0f, 81f);
            CreateGradientPanel(intel, DarkTop, DarkBottom, Border, 3f);
            Image intelIcon = CreateImage("Icon", intel, RequireSprite(V3UiFoundationBuilder.MissionIntelIconPath), Lime, false);
            SetTopLeft(intelIcon.rectTransform, 21f, 20f, 45f, 45f);
            CreateMetricText(intel, "INTEL CONFIDENCE", "HIGH", Lime);

            RectTransform seed = CreateHorizontalStretch("SeedMetric", panel, 14f, 14f, 594f, 65f);
            CreateGradientPanel(seed, DarkTop, DarkBottom, Border, 3f);
            RectTransform dice = CreateTopLeft("DiceIcon", seed, 23f, 12f, 43f, 43f);
            CreateDiceIcon(dice, TextMuted);
            TMP_Text seedLabel = CreateText("Label", seed, "MAP SEED", 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(seedLabel.rectTransform, 94f, 6f, 180f, 54f);
            seedInput = CreateInput("MapSeedInput", seed, .5f, 1f, 0f, 0f, 0f, 65f, "104729");
        }

        private static void CreateMetricText(Transform parent, string label, string value, Color accent)
        {
            TMP_Text labelText = CreateText("Label", parent, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(labelText.rectTransform, 91f, 5f, 240f, 33f);
            TMP_Text valueText = CreateText("Value", parent, value, 26f, boldFont, TextAlignmentOptions.MidlineLeft, accent);
            SetTopLeft(valueText.rectTransform, 91f, 34f, 240f, 40f);
        }

        private static void BuildRules(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            out UISegmentedControlView enemyCount,
            out UISegmentedControlView difficulty,
            out UISegmentedControlView startingCredits,
            out UISegmentedControlView startingResources,
            out UISliderRowView income,
            out UISegmentedControlView aggression,
            out UISegmentedControlView winCondition,
            out UIToggleRowView fog,
            out UIToggleRowView intel)
        {
            RectTransform force = CreateTopLeft("OpposingForce", root, 1120f, 125f, 538f, 269f);
            CreateGradientPanel(force, DarkTop, DarkBottom, Border, 3f);
            rightTargets.Add(force);
            BuildSectionTitle(force, "OPPOSING FORCE", 23f, 0f, 56f);
            CreateSolid("ForceRule", force, 13f, 55f, 512f, 2f, ThinRule);
            TMP_Text profileLabel = CreateText("EnemyProfileLabel", force, "ENEMY PROFILE", 19f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(profileLabel.rectTransform, 28f, 62f, 220f, 35f);
            TMP_Text profile = CreateText("EnemyProfile", force, "BALANCED", 28f, boldFont, TextAlignmentOptions.MidlineLeft, Lime);
            SetTopLeft(profile.rectTransform, 28f, 94f, 220f, 47f);
            TMP_Text countLabel = CreateText("EnemyCountLabel", force, "ENEMY COUNT", 19f, boldFont, TextAlignmentOptions.Center, TextMuted);
            SetTopLeft(countLabel.rectTransform, 293f, 62f, 220f, 35f);
            enemyCount = CreateVisibleSegmented("EnemyFactionStepper", force, 300f, 100f, 212f, 54f, new[] { "−", "1", "+" }, 27f, 1);
            CreateSolid("DifficultyRule", force, 13f, 160f, 512f, 2f, ThinRule);
            TMP_Text difficultyLabel = CreateText("DifficultyLabel", force, "DIFFICULTY", 19f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(difficultyLabel.rectTransform, 28f, 166f, 200f, 33f);
            difficulty = CreateVisibleSegmented("Difficulty", force, 28f, 203f, 484f, 52f, new[] { "EASY", "NORMAL", "HARD", "BRUTAL" }, 19f, -1);

            RectTransform rules = CreateTopLeft("MatchEconomy", root, 1120f, 407f, 538f, 389f);
            CreateGradientPanel(rules, DarkTop, DarkBottom, Border, 3f);
            rightTargets.Add(rules);
            BuildSectionTitle(rules, "MATCH RULES", 23f, 0f, 56f);

            RectTransform compatibility = CreateTopLeft("CompatibilityControls", rules, 0f, 0f, 1f, 1f);
            startingCredits = CreateHiddenSegmented("StartingCredits", compatibility, new[] { "LOW", "NORMAL", "HIGH" }, 1);
            startingResources = CreateHiddenSegmented("StartingResources", compatibility, new[] { "STANDARD", "LOW", "HIGH" }, 0);
            aggression = CreateHiddenSegmented("Aggression", compatibility, new[] { "DEFENSIVE", "BALANCED", "AGGRESSIVE" }, 1);
            winCondition = CreateHiddenSegmented("WinCondition", compatibility, new[] { "DESTROY", "SURVIVE", "SANDBOX" }, 0);
            income = CreateHiddenSlider("Income", compatibility);

            BuildCycleRuleRow(rules, "StartingMaterialsRow", 57f, "STARTING MATERIALS", "STANDARD", startingResources, null,
                new[] { "STANDARD", "LOW", "HIGH" }, new[] { "STANDARD", "LOW", "HIGH" });
            BuildCycleRuleRow(rules, "IncomeRow", 104f, "INCOME", "1.0x", null, income, null, null);
            BuildCycleRuleRow(rules, "AggressionRow", 151f, "AGGRESSION", "BALANCED", aggression, null,
                new[] { "DEFENSIVE", "BALANCED", "AGGRESSIVE" }, new[] { "DEFENSIVE", "BALANCED", "AGGRESSIVE" });
            BuildCycleRuleRow(rules, "WinConditionRow", 198f, "WIN CONDITION", "DESTROY ALL ENEMIES", winCondition, null,
                new[] { "DESTROY", "SURVIVE", "SANDBOX" }, new[] { "DESTROY ALL ENEMIES", "SURVIVE", "SANDBOX" });

            intel = CreateToggleRow("IntelReveal", rules, 13f, 267f, 248f, 106f, "INTEL REVEAL", true, true);
            fog = CreateToggleRow("FogOfWar", rules, 273f, 267f, 252f, 106f, "FOG OF WAR", false, false);
            TMP_Text locked = CreateText("Locked", fog.transform, "LOCKED", 26f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(locked.rectTransform, 20f, 47f, 142f, 43f);
            RectTransform lockRoot = CreateTopLeft("LockIcon", fog.transform, 181f, 41f, 46f, 48f);
            CreateLockIcon(lockRoot, TextMuted);
        }

        private static void BuildCycleRuleRow(
            Transform parent,
            string name,
            float y,
            string label,
            string initialValue,
            UISegmentedControlView segment,
            UISliderRowView slider,
            string[] segmentValues,
            string[] displayValues)
        {
            Button row = CreateGradientButton(name, parent, 13f, y, 512f, 47f, DarkTop, DarkBottom, ThinRule, 2f);
            TMP_Text labelText = CreateText("Label", row.transform, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(labelText.rectTransform, 15f, 1f, 255f, 43f);
            TMP_Text valueText = CreateText("Value", row.transform, initialValue, 20f, boldFont, TextAlignmentOptions.MidlineRight, Cyan);
            SetTopLeft(valueText.rectTransform, 254f, 1f, 242f, 43f);
            SkirmishSetupV3CycleControl cycle = row.gameObject.AddComponent<SkirmishSetupV3CycleControl>();
            if (segment != null)
                cycle.ConfigureSegment(row, segment, valueText, segmentValues, displayValues);
            else
                cycle.ConfigureSlider(row, slider, valueText, .5f, 3f, .5f, "0.0x");
        }

        private static UISegmentedControlView CreateVisibleSegmented(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string[] labels,
            float fontSize,
            int fixedVisualIndex)
        {
            RectTransform root = CreateTopLeft(name, parent, x, y, width, height);
            UISegmentedControlView view = root.gameObject.AddComponent<UISegmentedControlView>();
            Button[] buttons = new Button[labels.Length];
            TMP_Text[] texts = new TMP_Text[labels.Length];
            V3GradientGraphic[] gradients = new V3GradientGraphic[labels.Length];
            float gap = 4f;
            float segmentWidth = (width - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                bool selected = fixedVisualIndex >= 0 ? i == fixedVisualIndex : i == 1;
                Button button = CreateGradientButton(
                    $"Segment_{i}", root, i * (segmentWidth + gap), 0f, segmentWidth, height,
                    selected ? SelectedTop : RaisedTop,
                    selected ? SelectedBottom : RaisedBottom,
                    selected ? Lime : Border,
                    3f);
                TMP_Text text = CreateText("Label", button.transform, labels[i], fontSize, boldFont, TextAlignmentOptions.Center, selected ? TextPrimary : TextMuted);
                Stretch(text.rectTransform);
                buttons[i] = button;
                texts[i] = text;
                gradients[i] = button.targetGraphic as V3GradientGraphic;
            }
            SerializedObject serialized = new(view);
            SetReference(serialized, "segmentRoot", root);
            SetArray(serialized, "segmentButtons", buttons);
            SetArray(serialized, "segmentLabels", texts);
            serialized.FindProperty("applyVisualSelection").boolValue = true;
            serialized.FindProperty("normalLabelColor").colorValue = TextMuted;
            serialized.FindProperty("selectedLabelColor").colorValue = TextPrimary;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SkirmishSetupV3SegmentVisual visual = root.gameObject.AddComponent<SkirmishSetupV3SegmentVisual>();
            visual.Configure(buttons, gradients, fixedVisualIndex, RaisedTop, RaisedBottom, Border, SelectedTop, SelectedBottom, Lime, 3f);
            return view;
        }

        private static UISegmentedControlView CreateHiddenSegmented(string name, Transform parent, string[] labels, int selectedIndex)
        {
            RectTransform root = CreateTopLeft(name, parent, -20f, -20f, 1f, 1f);
            UISegmentedControlView view = root.gameObject.AddComponent<UISegmentedControlView>();
            Button[] buttons = new Button[labels.Length];
            TMP_Text[] texts = new TMP_Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Image image = CreateImage($"Segment_{i}", root, null, Color.clear, true);
                SetTopLeft(image.rectTransform, 0f, 0f, 1f, 1f);
                Button button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                TMP_Text text = CreateText("Label", button.transform, labels[i], 1f, mediumFont, TextAlignmentOptions.Center, Color.clear);
                Stretch(text.rectTransform);
                buttons[i] = button;
                texts[i] = text;
            }
            SerializedObject serialized = new(view);
            SetReference(serialized, "segmentRoot", root);
            SetArray(serialized, "segmentButtons", buttons);
            SetArray(serialized, "segmentLabels", texts);
            serialized.FindProperty("applyVisualSelection").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.Bind(labels, selectedIndex);
            return view;
        }

        private static UISliderRowView CreateHiddenSlider(string name, Transform parent)
        {
            RectTransform root = CreateTopLeft(name, parent, -20f, -20f, 1f, 1f);
            Slider slider = root.gameObject.AddComponent<Slider>();
            RectTransform fill = CreateTopLeft("Fill", root, 0f, 0f, 1f, 1f);
            fill.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform handle = CreateTopLeft("Handle", root, 0f, 0f, 1f, 1f);
            handle.gameObject.AddComponent<Image>().color = Color.clear;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            TMP_Text label = CreateText("Label", root, "INCOME", 1f, mediumFont, TextAlignmentOptions.Center, Color.clear);
            TMP_Text value = CreateText("Value", root, "1.0", 1f, mediumFont, TextAlignmentOptions.Center, Color.clear);
            UISliderRowView view = root.gameObject.AddComponent<UISliderRowView>();
            SerializedObject serialized = new(view);
            SetReference(serialized, "labelText", label);
            SetReference(serialized, "valueText", value);
            SetReference(serialized, "slider", slider);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static UIToggleRowView CreateToggleRow(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string label,
            bool value,
            bool interactable)
        {
            RectTransform root = CreateTopLeft(name, parent, x, y, width, height);
            CreateGradientPanel(root, DarkTop, DarkBottom, Border, 3f);
            TMP_Text labelText = CreateText("Label", root, label, 20f, boldFont, TextAlignmentOptions.MidlineLeft, TextMuted);
            SetTopLeft(labelText.rectTransform, 16f, 7f, width - 32f, 34f);
            TMP_Text description = CreateText("Description", root, string.Empty, 1f, mediumFont, TextAlignmentOptions.Left, Color.clear);
            SetTopLeft(description.rectTransform, 0f, 0f, 1f, 1f);

            RectTransform track = CreateTopLeft("Toggle", root, 79f, 51f, 88f, 38f);
            V3GradientGraphic trackGradient = CreateGradientPanel(
                track,
                value ? new Color32(80, 169, 44, 255) : RaisedTop,
                value ? new Color32(24, 92, 30, 255) : RaisedBottom,
                value ? Lime : Border,
                3f);
            Toggle toggle = track.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = trackGradient;
            toggle.isOn = value;
            toggle.interactable = interactable;
            RectTransform handle = CreateTopLeft("Handle", track, value ? 52f : 4f, 4f, 32f, 30f);
            V3GradientGraphic handleGradient = CreateGradientPanel(
                handle,
                new Color32(255, 247, 207, 255),
                new Color32(191, 176, 124, 255),
                new Color32(242, 229, 178, 255),
                2f);
            toggle.graphic = handleGradient;
            TMP_Text stateText = CreateText("State", root, value ? "ON" : "OFF", 1f, mediumFont, TextAlignmentOptions.Center, Color.clear);
            SetTopLeft(stateText.rectTransform, 0f, 0f, 1f, 1f);

            UIToggleRowView view = root.gameObject.AddComponent<UIToggleRowView>();
            SerializedObject serialized = new(view);
            SetReference(serialized, "labelText", labelText);
            SetReference(serialized, "descriptionText", description);
            SetReference(serialized, "stateText", stateText);
            SetReference(serialized, "toggle", toggle);
            SetReference(serialized, "handle", handle);
            SetReference(serialized, "trackGradient", trackGradient);
            SetReference(serialized, "handleGradient", handleGradient);
            serialized.FindProperty("onTrackColor").colorValue = new Color32(53, 143, 38, 255);
            serialized.FindProperty("offTrackColor").colorValue = new Color32(30, 38, 40, 255);
            serialized.FindProperty("onHandleColor").colorValue = new Color32(234, 220, 170, 255);
            serialized.FindProperty("offHandleColor").colorValue = TextMuted;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void BuildFooter(
            RectTransform root,
            ICollection<RectTransform> rightTargets,
            ICollection<RectTransform> widthTargets,
            out Button reset,
            out Button randomize,
            out Button launch)
        {
            reset = CreateGradientButton("ResetButton", root, 14f, 812f, 436f, 114f, RaisedTop, RaisedBottom, Border, 3f);
            RectTransform resetIcon = CreateTopLeft("Icon", reset.transform, 91f, 29f, 55f, 55f);
            Image resetImage = CreateImage("Sprite", resetIcon, catalog.ResetIcon, TextPrimary, false);
            Stretch(resetImage.rectTransform);
            TMP_Text resetLabel = CreateText("Label", reset.transform, "RESET", 36f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(resetLabel.rectTransform, 154f, 12f, 246f, 88f);

            randomize = CreateGradientButton("RandomizeSeedButton", root, 463f, 812f, 503f, 114f, RaisedTop, RaisedBottom, Border, 3f);
            widthTargets.Add(randomize.GetComponent<RectTransform>());
            RectTransform dice = CreateTopLeft("Icon", randomize.transform, 66f, 30f, 55f, 55f);
            CreateDiceIcon(dice, TextPrimary);
            TMP_Text randomLabel = CreateText("Label", randomize.transform, "RANDOMIZE SEED", 33f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetHorizontalStretch(randomLabel.rectTransform, 129f, 25f, 12f, 88f);

            launch = CreateGradientButton("LaunchMissionButton", root, 979f, 812f, 679f, 114f, LaunchTop, LaunchBottom, Lime, 3f);
            rightTargets.Add(launch.GetComponent<RectTransform>());
            TMP_Text launchLabel = CreateText("Label", launch.transform, "LAUNCH MISSION", 45f, boldFont, TextAlignmentOptions.Center, TextPrimary);
            SetTopLeft(launchLabel.rectTransform, 118f, 10f, 443f, 92f);
            CreateChevronGroup(launch.transform, 46f, 57f, TextPrimary, true);
            CreateChevronGroup(launch.transform, 632f, 57f, TextPrimary, true);
        }

        private static Button CreateGradientButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradientPanel(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.58f, .58f, .58f, .72f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            return button;
        }

        private static TMP_InputField CreateInput(
            string name,
            Transform parent,
            float anchorMinX,
            float anchorMaxX,
            float left,
            float right,
            float y,
            float height,
            string value)
        {
            RectTransform rect = CreateAnchoredHorizontal(name, parent, anchorMinX, anchorMaxX, left, right, y, height);
            V3GradientGraphic background = CreateGradientPanel(rect, DarkTop, DarkBottom, Color.clear, 0f);
            TMP_Text text = CreateText("Text", rect, value, 28f, boldFont, TextAlignmentOptions.MidlineRight, Cyan);
            Stretch(text.rectTransform);
            text.margin = new Vector4(10f, 0f, 16f, 0f);
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            input.textComponent = text;
            input.text = value;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.targetGraphic = background;
            return input;
        }

        private static void BuildSectionTitle(Transform parent, string label, float size, float y, float height)
        {
            TMP_Text title = CreateText("Title", parent, label, size, boldFont, TextAlignmentOptions.MidlineLeft, TextPrimary);
            SetTopLeft(title.rectTransform, 20f, y, 490f, height);
        }

        private static void CreateTargetIcon(RectTransform root, Color color)
        {
            V3RingGraphic outer = root.gameObject.AddComponent<V3RingGraphic>();
            outer.Configure(color, 5f, 64);
            CreateSolid("CrossH", root, 2f, 23f, 46f, 4f, color);
            CreateSolid("CrossV", root, 23f, 2f, 4f, 46f, color);
            RectTransform center = CreateTopLeft("Center", root, 20f, 20f, 10f, 10f);
            V3DiscGraphic disc = center.gameObject.AddComponent<V3DiscGraphic>();
            disc.Configure(color);
        }

        private static void CreateLockIcon(RectTransform root, Color color)
        {
            RectTransform shackle = CreateTopLeft("Shackle", root, 10f, 3f, root.rect.width - 20f, root.rect.height * .56f);
            V3RingGraphic ring = shackle.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 4f, 40);
            RectTransform mask = CreateTopLeft("ShackleMask", shackle, -2f, shackle.rect.height * .52f, shackle.rect.width + 4f, shackle.rect.height * .56f);
            mask.gameObject.AddComponent<Image>().color = new Color(DarkBottom.r, DarkBottom.g, DarkBottom.b, 1f);
            RectTransform body = CreateTopLeft("Body", root, 5f, root.rect.height * .43f, root.rect.width - 10f, root.rect.height * .48f);
            CreateGradientPanel(body, Color.Lerp(color, Color.white, .12f), Color.Lerp(color, Color.black, .28f), color, 2f);
            CreateSolid("Key", body, body.rect.width * .46f, body.rect.height * .28f, body.rect.width * .08f, body.rect.height * .42f, DarkBottom);
        }

        private static void CreateCheckMark(RectTransform root, Color color)
        {
            float centerY = root.rect.height * .52f;
            Vector2 joint = new(25f, centerY + 12f);
            CreateStroke("CheckLeft", root, new Vector2(12f, centerY), joint, 6f, color);
            CreateStroke("CheckRight", root, joint, new Vector2(49f, centerY - 20f), 6f, color);
        }

        private static void CreateDiceIcon(RectTransform root, Color color)
        {
            V3GradientGraphic die = root.gameObject.AddComponent<V3GradientGraphic>();
            die.Configure(new Color32(218, 219, 211, 255), new Color32(105, 111, 109, 255), color, 2f);
            float size = Mathf.Min(root.rect.width, root.rect.height) * .12f;
            Vector2[] dots =
            {
                new(.27f, .27f), new(.73f, .27f), new(.5f, .5f), new(.27f, .73f), new(.73f, .73f)
            };
            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = CreateRect($"Dot_{i}", root, dots[i], dots[i], new Vector2(size, size), Vector2.zero);
                V3DiscGraphic disc = dot.gameObject.AddComponent<V3DiscGraphic>();
                disc.Configure(DarkBottom);
            }
        }

        private static void CreateChevronGroup(Transform parent, float centerX, float centerY, Color color, bool pointRight)
        {
            for (int i = 0; i < 3; i++)
            {
                float offset = (i - 1f) * 14f;
                float x = centerX + offset;
                float direction = pointRight ? 1f : -1f;
                Vector2 tip = new(x + 8f * direction, centerY);
                CreateStroke(
                    $"Chevron_{(pointRight ? "R" : "L")}_{i}_A",
                    parent,
                    new Vector2(x - 9f * direction, centerY - 16f),
                    tip,
                    5f,
                    color);
                CreateStroke(
                    $"Chevron_{(pointRight ? "R" : "L")}_{i}_B",
                    parent,
                    tip,
                    new Vector2(x - 9f * direction, centerY + 16f),
                    5f,
                    color);
            }
        }

        private static Image CreateStroke(
            string name,
            Transform parent,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 screenDelta = end - start;
            Vector2 localDelta = new(screenDelta.x, -screenDelta.y);
            float length = localDelta.magnitude;
            Image stroke = CreateImage(name, parent, null, color, false);
            RectTransform rect = stroke.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(localDelta.y, localDelta.x) * Mathf.Rad2Deg);
            return stroke;
        }

        private static V3GradientGraphic CreateGradientPanel(RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.ConfigureCorners(
                Color.Lerp(top, Color.white, .035f),
                top,
                Color.Lerp(bottom, Color.black, .12f),
                bottom,
                border,
                borderWidth);
            return graphic;
        }

        private static void CreateGradientOverlay(string name, Transform parent, Color top, Color bottom)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, Color.clear, 0f);
            gradient.raycastTarget = false;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static Image CreateSolidStretch(string name, Transform parent, float left, float right, float y, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetHorizontalStretch(image.rectTransform, left, right, y, height);
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddCover(RawImage image, Texture texture)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = texture.width / (float)texture.height;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            TMP_FontAsset font,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200f, 60f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, position);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateHorizontalStretch(string name, Transform parent, float left, float right, float y, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-(left + right), height), new Vector2(left, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateAnchoredHorizontal(
            string name,
            Transform parent,
            float anchorMinX,
            float anchorMaxX,
            float left,
            float right,
            float y,
            float height)
        {
            RectTransform rect = CreateRect(
                name,
                parent,
                new Vector2(anchorMinX, 1f),
                new Vector2(anchorMaxX, 1f),
                new Vector2(-(left + right), height),
                new Vector2(left, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast) =>
            V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetHorizontalStretch(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.anchoredPosition = new Vector2(left, -y);
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void RouteMainMenuSkirmishCard()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                Transform card = Find(root.transform, "Card_Skirmish");
                if (card == null)
                    throw new InvalidOperationException("Main Menu prefab is missing Card_Skirmish.");
                Transform hotspot = Find(card, "Hotspot");
                GameObject target = hotspot != null
                    ? hotspot.gameObject
                    : CreateRect("Hotspot", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
                Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
                Button button = target.GetComponent<Button>() ?? target.AddComponent<Button>();
                button.targetGraphic = image;
                UIShellRouteButtonView route = target.GetComponent<UIShellRouteButtonView>() ?? target.AddComponent<UIShellRouteButtonView>();
                route.Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, true);
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                content = root.GetComponentInChildren<UIShellContentView>(true);
                if (content != null)
                    break;
            }
            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");
            SerializedObject serialized = new(content);
            SetReference(serialized, "skirmishSetupContentPrefab", prefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root.Find(path);
            if (result == null)
                throw new MissingReferenceException($"SCN-13 is missing {path}.");
            return result;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing SCN-13 art: {path}");
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.mipmapEnabled ||
                           importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           importer.maxTextureSize != maxSize;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            if (changed)
                importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing SCN-13 sprite: {path}");
            return sprite;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
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
