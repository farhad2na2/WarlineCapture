#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class SkirmishSetupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab";
        private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string PreviewPath = "Assets/Game/Art/UI/Generated/SkirmishSetup/TargetLockV02/scn13_operation_preview_sahrin_v02.png";
        private const string PanelSpritePath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png";
        private const string SelectedSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_10_selected_small_button_frame.png";
        private const string SecondarySpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_15_secondary_dark_cta_frame.png";
        private const string GoldSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_14_primary_gold_cta_frame.png";
        private const string BackIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png";
        private const string LockIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_checkbox_empty.png";
        private const string ObjectiveIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_objective_star.png";
        private const string CivilianIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_civilian_group.png";
        private const string IntelIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_scan_radar.png";
        private const string TutorialArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_skirmish_thumbnail_art.png";
        private const string ConvoyArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_operations_thumbnail_art.png";
        private const string AirliftArtPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_art_attack_helicopter.png";
        private const string BreachArtPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_campaign_thumbnail_art.png";

        private static readonly Color PanelTint = new(0.035f, 0.045f, 0.04f, 0.98f);
        private static readonly Color RowTint = new(0.06f, 0.07f, 0.06f, 0.98f);
        private static readonly Color SelectedTint = new(0.40f, 0.43f, 0.08f, 0.96f);
        private static readonly Color Gold = new(0.94f, 0.68f, 0.16f, 1f);
        private static readonly Color Olive = new(0.68f, 0.76f, 0.18f, 1f);
        private static readonly Color Cyan = new(0.20f, 0.72f, 0.88f, 1f);
        private static readonly Color Text = new(0.93f, 0.90f, 0.80f, 1f);
        private static readonly Color Muted = new(0.66f, 0.65f, 0.56f, 1f);

        [MenuItem("WarlineCapture/UI/Build SCN-13 Skirmish Setup")]
        public static void Build()
        {
            EnsurePreviewIsSprite();
            GameObject root = BuildPrefabRoot();
            EnsureFolder("Assets/Game/Prefabs/UI/Shell/Content");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Skirmish setup prefab at {PrefabPath}.");

            RouteMainMenuSkirmishCard();
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SkirmishSetupPrefabBuilder] result=Passed prefab={PrefabPath}");
        }

        private static GameObject BuildPrefabRoot()
        {
            GameObject root = CreateRect("SCN13_SkirmishSetupContent", null, 0f, 0f, 4800f, 2160f);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            QuickCustomScreenView screen = root.AddComponent<QuickCustomScreenView>();
            screen.SetRouteForTests(UIRoute.QuickCustomSetup);

            Image bodyScrim = CreateSolid("BodyScrim", root.transform, 0f, 280f, 4800f, 1880f, new Color(0.01f, 0.014f, 0.011f, 0.76f));
            bodyScrim.raycastTarget = false;

            Button backButton = CreateButton("BackButton", root.transform, 90f, 315f, 520f, 150f, "BACK", SecondarySpritePath, 48f, Text);
            UIShellRouteButtonView backRoute = backButton.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            CreateIcon("BackIcon", backButton.transform, BackIconPath, 42f, 36f, 72f, 72f);
            SetTextRect(backButton.transform.Find("Label") as RectTransform, 132f, 0f, 330f, 150f);
            CreateText("ScreenTitle", root.transform, 660f, 315f, 1280f, 150f, "SKIRMISH SETUP", 72f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("ScreenSubtitle", root.transform, 1910f, 350f, 930f, 95f, "CONFIGURE OPERATION", 32f, Muted, TextAlignmentOptions.MidlineLeft);

            BuildPresetRail(root.transform);
            TMP_InputField seedInput;
            BuildPreview(root.transform, out seedInput);

            UISegmentedControlView enemyCount = null;
            UISegmentedControlView difficulty = null;
            UISegmentedControlView startingCredits = null;
            UISegmentedControlView startingResources = null;
            UISliderRowView income = null;
            UISegmentedControlView aggression = null;
            UISegmentedControlView winCondition = null;
            UIToggleRowView fog = null;
            UIToggleRowView intel = null;
            BuildRules(
                root.transform,
                out enemyCount,
                out difficulty,
                out startingCredits,
                out startingResources,
                out income,
                out aggression,
                out winCondition,
                out fog,
                out intel);

            Button resetButton;
            Button randomizeButton;
            Button launchButton;
            BuildFooter(root.transform, out resetButton, out randomizeButton, out launchButton);

            TMP_Text mapName = FindText(root.transform, "OperationName");
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
            SetReference(serializedScreen, "resetButton", resetButton);
            SetReference(serializedScreen, "randomizeSeedButton", randomizeButton);
            SetReference(serializedScreen, "launchButton", launchButton);
            serializedScreen.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void BuildPresetRail(Transform root)
        {
            Transform panel = CreatePanel("PresetRail", root, 80f, 500f, 1000f, 1420f);
            CreateText("Title", panel, 50f, 32f, 900f, 82f, "OPERATION PRESETS", 48f, Text, TextAlignmentOptions.Center);
            CreatePreset(panel, 0, "TUTORIAL INTERCEPT", "ELIMINATION", TutorialArtPath, true);
            CreatePreset(panel, 1, "CONVOY PRESSURE", "SUPPLY DISRUPTION", ConvoyArtPath, false);
            CreatePreset(panel, 2, "AIRLIFT EXTRACTION", "EXTRACTION", AirliftArtPath, false);
            CreatePreset(panel, 3, "BREACH ASSAULT", "BASE ASSAULT", BreachArtPath, false);
        }

        private static void CreatePreset(Transform panel, int index, string title, string subtitle, string artPath, bool selected)
        {
            float y = 135f + index * 305f;
            Image row = CreateFramed($"Preset_{index}", panel, 45f, y, 910f, 270f, selected ? SelectedSpritePath : SecondarySpritePath, Color.white);
            row.raycastTarget = false;
            Image fill = CreateSolid("Fill", row.transform, 12f, 12f, 886f, 246f, selected ? SelectedTint : RowTint);
            fill.transform.SetAsFirstSibling();
            RawImage thumbnail = CreateCroppedPreview("Thumbnail", row.transform, 30f, 30f, 210f, 210f, artPath);
            thumbnail.color = selected ? Color.white : new Color(0.66f, 0.66f, 0.60f, 0.82f);
            CreateText("Title", row.transform, 280f, 34f, 500f, 100f, title, 44f, selected ? Text : Muted, TextAlignmentOptions.MidlineLeft);
            CreateText("Subtitle", row.transform, 280f, 142f, 500f, 58f, subtitle, 30f, selected ? Olive : Muted, TextAlignmentOptions.MidlineLeft);
            if (!selected)
                CreateIcon("Lock", row.transform, LockIconPath, 810f, 96f, 58f, 58f, Muted);
            else
                CreateText("Selected", row.transform, 790f, 90f, 82f, 82f, "✓", 52f, Olive, TextAlignmentOptions.Center);
        }

        private static void BuildPreview(Transform root, out TMP_InputField seedInput)
        {
            Transform panel = CreatePanel("OperationPreview", root, 1130f, 500f, 2100f, 1420f);
            TMP_Text operationName = CreateText("OperationName", panel, 70f, 28f, 1960f, 92f, "SAHRIN OUTSKIRTS", 62f, Text, TextAlignmentOptions.Center);
            operationName.enableAutoSizing = true;
            operationName.fontSizeMin = 42f;
            CreateFramed("MapPreviewFrame", panel, 55f, 135f, 1990f, 770f, PanelSpritePath, Color.white);
            CreateCroppedPreview("MapPreview", panel, 70f, 150f, 1960f, 740f, PreviewPath);
            CreateIcon("ObjectiveIcon", panel, ObjectiveIconPath, 92f, 930f, 90f, 90f, Gold);
            CreateText("Objective", panel, 205f, 925f, 1710f, 100f, "DESTROY ALL ENEMIES", 54f, Text, TextAlignmentOptions.MidlineLeft);

            Transform risk = CreateMetricCard("CivilianRisk", panel, 70f, 1070f, 610f, "CIVILIAN RISK", "MED", CivilianIconPath, Gold);
            Transform intel = CreateMetricCard("IntelConfidence", panel, 705f, 1070f, 660f, "INTEL CONFIDENCE", "HIGH", IntelIconPath, Olive);
            Transform seed = CreatePanel("SeedMetric", panel, 1390f, 1070f, 640f, 200f);
            CreateIcon("Icon", seed, ObjectiveIconPath, 30f, 54f, 88f, 88f, Cyan);
            CreateText("Label", seed, 145f, 22f, 455f, 50f, "MAP SEED", 30f, Muted, TextAlignmentOptions.MidlineLeft);
            seedInput = CreateInput("MapSeedInput", seed, 135f, 78f, 455f, 92f, "104729");
            risk.gameObject.SetActive(true);
            intel.gameObject.SetActive(true);
            seed.gameObject.SetActive(true);
        }

        private static Transform CreateMetricCard(string name, Transform parent, float x, float y, float width, string label, string value, string iconPath, Color valueColor)
        {
            Transform card = CreatePanel(name, parent, x, y, width, 200f);
            CreateIcon("Icon", card, iconPath, 30f, 54f, 88f, 88f, valueColor);
            CreateText("Label", card, 145f, 28f, width - 180f, 52f, label, 30f, Muted, TextAlignmentOptions.MidlineLeft);
            CreateText("Value", card, 145f, 86f, width - 180f, 72f, value, 46f, valueColor, TextAlignmentOptions.MidlineLeft);
            return card;
        }

        private static void BuildRules(
            Transform root,
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
            Transform forcePanel = CreatePanel("OpposingForce", root, 3280f, 500f, 1440f, 510f);
            CreateText("Title", forcePanel, 52f, 24f, 1330f, 72f, "OPPOSING FORCE", 46f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("ProfileLabel", forcePanel, 55f, 110f, 430f, 46f, "ENEMY PROFILE", 34f, Muted, TextAlignmentOptions.MidlineLeft);
            CreateText("ProfileValue", forcePanel, 55f, 158f, 430f, 66f, "BALANCED", 40f, Olive, TextAlignmentOptions.MidlineLeft);
            CreateText("FactionLabel", forcePanel, 600f, 110f, 720f, 46f, "ENEMY FACTIONS", 34f, Muted, TextAlignmentOptions.Center);
            enemyCount = CreateSegmented("EnemyFactionStepper", forcePanel, 720f, 160f, 590f, 105f, new[] { "-", "1", "+" }, 36f);
            CreateText("DifficultyLabel", forcePanel, 55f, 285f, 1260f, 44f, "DIFFICULTY", 34f, Muted, TextAlignmentOptions.MidlineLeft);
            difficulty = CreateSegmented("Difficulty", forcePanel, 55f, 340f, 1330f, 115f, new[] { "EASY", "NORMAL", "HARD", "BRUTAL" }, 36f);

            Transform economyPanel = CreatePanel("MatchEconomy", root, 3280f, 1040f, 1440f, 880f);
            CreateText("Title", economyPanel, 52f, 24f, 1330f, 72f, "MATCH ECONOMY", 46f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("StartingCreditsLabel", economyPanel, 55f, 125f, 450f, 46f, "STARTING CREDITS", 32f, Muted, TextAlignmentOptions.MidlineLeft);
            startingCredits = CreateSegmented("StartingCredits", economyPanel, 520f, 105f, 850f, 100f, new[] { "LOW", "STANDARD", "HIGH" }, 34f);
            CreateText("StartingResourcesLabel", economyPanel, 55f, 240f, 450f, 46f, "STARTING RESOURCES", 32f, Muted, TextAlignmentOptions.MidlineLeft);
            startingResources = CreateSegmented("StartingResources", economyPanel, 520f, 220f, 850f, 100f, new[] { "STANDARD", "LOW", "HIGH" }, 32f);
            income = CreateSliderRow("Income", economyPanel, 55f, 345f, 1315f, 105f, "INCOME", "1.0x");
            CreateText("AggressionLabel", economyPanel, 55f, 490f, 430f, 46f, "AGGRESSION", 32f, Muted, TextAlignmentOptions.MidlineLeft);
            aggression = CreateSegmented("Aggression", economyPanel, 520f, 470f, 850f, 100f, new[] { "DEFENSIVE", "BALANCED", "AGGRESSIVE" }, 29f);
            CreateText("WinConditionLabel", economyPanel, 55f, 610f, 430f, 46f, "WIN CONDITION", 32f, Muted, TextAlignmentOptions.MidlineLeft);
            winCondition = CreateSegmented("WinCondition", economyPanel, 520f, 590f, 850f, 100f, new[] { "DESTROY", "SURVIVE", "SANDBOX" }, 31f);
            fog = CreateToggleRow("FogOfWar", economyPanel, 55f, 715f, 600f, 130f, "FOG OF WAR", "REQUIRES FOG RUNTIME", false, false);
            intel = CreateToggleRow("IntelReveal", economyPanel, 700f, 715f, 670f, 130f, "INTEL REVEAL", "REVEAL ENEMY TECH", true, true);
        }

        private static void BuildFooter(Transform root, out Button reset, out Button randomize, out Button launch)
        {
            reset = CreateButton("ResetButton", root, 450f, 1970f, 1000f, 175f, "RESET", SecondarySpritePath, 52f, Text);
            randomize = CreateButton("RandomizeSeedButton", root, 1570f, 1970f, 1300f, 175f, "RANDOMIZE SEED", SecondarySpritePath, 50f, Text);
            launch = CreateButton("LaunchMissionButton", root, 3020f, 1950f, 1500f, 195f, "LAUNCH MISSION", GoldSpritePath, 76f, new Color(0.12f, 0.09f, 0.02f, 1f));
            AddButtonBacking(reset, new Color(0.025f, 0.03f, 0.028f, 0.96f));
            AddButtonBacking(randomize, new Color(0.025f, 0.03f, 0.028f, 0.96f));
            AddButtonBacking(launch, new Color(0.83f, 0.52f, 0.035f, 0.96f));
        }

        private static UISegmentedControlView CreateSegmented(string name, Transform parent, float x, float y, float width, float height, string[] labels, float fontSize)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            UISegmentedControlView view = root.AddComponent<UISegmentedControlView>();
            Button[] buttons = new Button[labels.Length];
            TMP_Text[] texts = new TMP_Text[labels.Length];
            float gap = 8f;
            float segmentWidth = (width - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Button button = CreateButton($"Segment_{i}", root.transform, i * (segmentWidth + gap), 0f, segmentWidth, height, labels[i], SecondarySpritePath, fontSize, Text);
                buttons[i] = button;
                texts[i] = button.transform.Find("Label")?.GetComponent<TMP_Text>();
            }

            SerializedObject serialized = new(view);
            SetReference(serialized, "segmentRoot", root.transform);
            SetArray(serialized, "segmentButtons", buttons);
            SetArray(serialized, "segmentLabels", texts);
            serialized.FindProperty("applyVisualSelection").boolValue = true;
            serialized.FindProperty("normalSprite").objectReferenceValue = LoadSprite(SecondarySpritePath);
            serialized.FindProperty("selectedSprite").objectReferenceValue = LoadSprite(SelectedSpritePath);
            serialized.FindProperty("normalLabelColor").colorValue = Text;
            serialized.FindProperty("selectedLabelColor").colorValue = Olive;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static UISliderRowView CreateSliderRow(string name, Transform parent, float x, float y, float width, float height, string label, string value)
        {
            Transform root = CreatePanel(name, parent, x, y, width, height);
            TMP_Text labelText = CreateText("Label", root, 24f, 0f, 350f, height, label, 31f, Muted, TextAlignmentOptions.MidlineLeft);
            TMP_Text valueText = CreateText("Value", root, width - 170f, 0f, 140f, height, value, 34f, Cyan, TextAlignmentOptions.Center);
            GameObject sliderObject = CreateRect("Slider", root, 390f, 24f, width - 590f, height - 48f);
            Slider slider = sliderObject.AddComponent<Slider>();
            Image track = CreateSolid("Track", sliderObject.transform, 0f, (height - 56f) * 0.5f, width - 590f, 14f, new Color(0.12f, 0.15f, 0.14f, 1f));
            RectTransform fillArea = CreateRect("FillArea", sliderObject.transform, 0f, (height - 56f) * 0.5f, width - 590f, 14f).GetComponent<RectTransform>();
            Image fill = CreateSolid("Fill", fillArea, 0f, 0f, width - 590f, 14f, Olive);
            RectTransform handleArea = CreateRect("HandleArea", sliderObject.transform, 0f, 0f, width - 590f, height - 48f).GetComponent<RectTransform>();
            Image handle = CreateSolid("Handle", handleArea, 0f, 0f, 42f, 42f, Gold);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            track.raycastTarget = false;

            UISliderRowView view = root.gameObject.AddComponent<UISliderRowView>();
            SerializedObject serialized = new(view);
            SetReference(serialized, "labelText", labelText);
            SetReference(serialized, "valueText", valueText);
            SetReference(serialized, "slider", slider);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static UIToggleRowView CreateToggleRow(string name, Transform parent, float x, float y, float width, float height, string label, string description, bool value, bool interactable)
        {
            Transform root = CreatePanel(name, parent, x, y, width, height);
            TMP_Text labelText = CreateText("Label", root, 24f, 12f, width - 190f, 42f, label, 30f, Text, TextAlignmentOptions.MidlineLeft);
            TMP_Text descriptionText = CreateText("Description", root, 24f, 62f, width - 190f, 40f, description, 22f, interactable ? Muted : Gold, TextAlignmentOptions.MidlineLeft);
            GameObject toggleObject = CreateRect("Toggle", root, width - 150f, 35f, 120f, 60f);
            Image background = toggleObject.AddComponent<Image>();
            background.sprite = LoadSprite(SecondarySpritePath);
            background.type = Image.Type.Sliced;
            background.color = interactable ? new Color(0.35f, 0.38f, 0.08f, 1f) : new Color(0.14f, 0.14f, 0.12f, 1f);
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            Image handle = CreateSolid("Handle", toggleObject.transform, value ? 62f : 8f, 8f, 50f, 44f, value ? Gold : Muted);
            toggle.targetGraphic = background;
            toggle.graphic = handle;
            toggle.isOn = value;
            toggle.interactable = interactable;
            TMP_Text stateText = CreateText("State", toggleObject.transform, 0f, 0f, 120f, 60f, value ? "ON" : "OFF", 18f, Text, value ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);

            UIToggleRowView view = root.gameObject.AddComponent<UIToggleRowView>();
            SerializedObject serialized = new(view);
            SetReference(serialized, "labelText", labelText);
            SetReference(serialized, "descriptionText", descriptionText);
            SetReference(serialized, "stateText", stateText);
            SetReference(serialized, "toggle", toggle);
            SetReference(serialized, "handle", handle.rectTransform);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TMP_InputField CreateInput(string name, Transform parent, float x, float y, float width, float height, string value)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image background = root.AddComponent<Image>();
            background.sprite = LoadSprite(SecondarySpritePath);
            background.type = Image.Type.Sliced;
            background.color = RowTint;
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText("Text", root.transform, 28f, 0f, width - 56f, height, value, 36f, Cyan, TextAlignmentOptions.Center);
            input.textComponent = text;
            input.text = value;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.targetGraphic = background;
            return input;
        }

        private static Button CreateButton(string name, Transform parent, float x, float y, float width, float height, string label, string spritePath, float fontSize, Color labelColor)
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
            CreateText("Label", root.transform, 20f, 0f, width - 40f, height, label, fontSize, labelColor, TextAlignmentOptions.Center);
            return button;
        }

        private static Transform CreatePanel(string name, Transform parent, float x, float y, float width, float height)
        {
            Image frame = CreateFramed(name, parent, x, y, width, height, PanelSpritePath, Color.white);
            Image fill = CreateSolid("PanelFill", frame.transform, 12f, 12f, width - 24f, height - 24f, PanelTint);
            fill.transform.SetAsFirstSibling();
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
            if (button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            Image backing = CreateSolid("Backing", button.transform, 12f, 12f, rect.rect.width - 24f, rect.rect.height - 24f, color);
            backing.transform.SetAsFirstSibling();
        }

        private static TMP_Text CreateText(string name, Transform parent, float x, float y, float width, float height, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            return text;
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

        private static TMP_Text FindText(Transform root, string name)
        {
            Transform target = FindDescendant(root, name);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void RouteMainMenuSkirmishCard()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                Transform card = FindDescendant(root.transform, "Card_Skirmish");
                if (card == null)
                    throw new InvalidOperationException("Main Menu prefab is missing Card_Skirmish.");

                Transform hotspot = FindDescendant(card, "Hotspot");
                GameObject target = hotspot != null ? hotspot.gameObject : CreateRect("Hotspot", card, 0f, 0f, 860f, 1350f);
                RectTransform rect = target.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
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

        private static void EnsurePreviewIsSprite()
        {
            AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(PreviewPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing preview texture at {PreviewPath}.");
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
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
