#if UNITY_EDITOR
using System;
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
    public static class MissionResultV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";

        private const string BackgroundPath =
            "Assets/Game/Art/UI/V3Shared/MissionBriefing/SCN06_ForwardPost_V3.png";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(17, 27, 31, 250);
        private static readonly Color DarkBottom = new Color32(2, 8, 10, 252);
        private static readonly Color RowTop = new Color32(17, 29, 33, 248);
        private static readonly Color RowBottom = new Color32(5, 13, 16, 250);
        private static readonly Color Line = new Color32(83, 99, 103, 255);
        private static readonly Color VictoryTop = new Color32(69, 143, 36, 255);
        private static readonly Color VictoryBottom = new Color32(17, 63, 19, 255);
        private static readonly Color LossTop = new Color32(190, 48, 27, 255);
        private static readonly Color LossBottom = new Color32(76, 13, 10, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;

        [MenuItem("Game/UI/V3/Rebuild Mission Result V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = new("MissionResultPopup", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);
                MissionResultPopupView view = root.AddComponent<MissionResultPopupView>();

                Texture2D backgroundTexture = RequireAsset<Texture2D>(BackgroundPath);
                RectTransform backdropRect = CreateRect("BattlefieldBackdrop", root.transform);
                Stretch(backdropRect);
                RawImage backdrop = backdropRect.gameObject.AddComponent<RawImage>();
                backdrop.texture = backgroundTexture;
                backdrop.color = Color.white;
                backdrop.raycastTarget = false;
                AspectRatioFitter fitter = backdropRect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = backgroundTexture.width / (float)backgroundTexture.height;

                Image backgroundTint = CreateImage("OutcomeBackdropTint", root.transform, null, Color.clear);
                Stretch(backgroundTint.rectTransform);

                RectTransform composition = CreateTopLeft("V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                layout.Configure(Reference, MainMenuV3SectionAlignment.Center);

                HeaderBindings header = BuildHeader(composition);
                ObjectiveBindings objectives = BuildObjectives(composition);
                PerformanceBindings performance = BuildPerformance(composition);
                RewardBindings rewards = BuildRewards(composition);
                FooterBindings footer = BuildFooter(composition);
                GameObject[] legacyRoots = BuildCompatibilityRoots(root.transform);

                TMP_Text hiddenStats = CreateText(
                    legacyRoots[1].transform, "AuthoritativeStatsText", 0f, 0f, 100f, 30f,
                    string.Empty, 14f, theme.TextPrimary, TextAlignmentOptions.Center, false);
                TMP_Text hiddenMissionName = legacyRoots[0].GetComponentInChildren<TMP_Text>(true);

                view.Configure(
                    header.Title,
                    hiddenMissionName,
                    footer.Summary,
                    header.Elapsed,
                    performance.SquadLosses,
                    performance.EnemiesDefeated,
                    rewards.RewardText,
                    hiddenStats,
                    header.Stars,
                    footer.ContinueButton,
                    footer.ContinueLabel,
                    footer.RetryButton,
                    footer.RetryLabel,
                    legacyRoots);
                view.ConfigureV3(
                    header.MissionIdentity,
                    header.MissionStatus,
                    header.StarCount,
                    performance.CiviliansLost,
                    objectives.PatrolStatus,
                    objectives.SquadStatus,
                    objectives.CivilianStatus,
                    new[]
                    {
                        objectives.Title,
                        objectives.PatrolStatus,
                        objectives.SquadStatus,
                        objectives.CivilianStatus
                    },
                    objectives.Icons,
                    new[] { performance.Title, rewards.Title },
                    new[] { performance.TitleIcon },
                    new[] { rewards.TitleStar },
                    header.Emblem,
                    header.TimerIcon,
                    footer.Accent,
                    backgroundTint,
                    rewards.IconRoot,
                    RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath),
                    RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath),
                    header.EmblemPanel,
                    footer.ContinueGradient,
                    footer.RetryGradient);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[MissionResultV3PrefabBuilder] result=Passed states=2 layout=1672x941 gradients=procedural borders=3 art=aspect-preserved runtime=bound");
        }

        [MenuItem("Game/UI/V3/Capture Mission Result V3 Review")]
        public static void CaptureReview()
        {
            Build();
            UiMissionResultPopupModel victory = new(
                41, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Victory,
                "VICTORY", "FIRST CONTACT • OLD MARKET",
                "The hostile patrol is neutralized. The corridor is secure.",
                3, "03:14", "0", "3 / 3", "1,200 CREDITS  ·  260 COMMANDER XP",
                "CONTINUE", true, false);
            UiMissionResultPopupModel loss = new(
                42, "saga.ch01.m01.first_contact", UiMissionResultOutcome.Loss,
                "MISSION FAILED", "FIRST CONTACT • OLD MARKET",
                "The command squad was lost. Regroup and redeploy.",
                0, "01:05", "1", "1 / 3", "NO REWARD", "RETRY", true, true);

            Capture("/private/tmp/warline-mission-result-v3-victory-16x9.png", 1920, 1080, victory);
            Capture("/private/tmp/warline-mission-result-v3-defeat-16x9.png", 1920, 1080, loss);
            Capture("/private/tmp/warline-mission-result-v3-victory-20x9.png", 4800, 2160, victory);
            Capture("/private/tmp/warline-mission-result-v3-defeat-20x9.png", 4800, 2160, loss);
        }

        [MenuItem("Game/UI/V3/Validate Mission Result V3 Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Mission Result prefab: {PrefabPath}");
            MissionResultPopupView view = prefab.GetComponent<MissionResultPopupView>();
            if (view == null)
                throw new MissingReferenceException("Mission Result V3 runtime view is missing.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference ||
                layout.Alignment != MainMenuV3SectionAlignment.Center)
                throw new InvalidOperationException("Mission Result V3 must use the centered 1672x941 composition.");
            RawImage background = FindDeepChild(prefab.transform, "BattlefieldBackdrop")?.GetComponent<RawImage>();
            AspectRatioFitter fitter = background != null ? background.GetComponent<AspectRatioFitter>() : null;
            if (background == null || background.texture == null || fitter == null ||
                fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("Mission Result background must fill without stretching.");
            if (FindDeepChild(prefab.transform, "ContinueButton")?.GetComponent<Button>() == null ||
                FindDeepChild(prefab.transform, "ReplayButton")?.GetComponent<Button>() == null)
                throw new MissingReferenceException("Mission Result V3 outcome actions are not bound.");
            int stars = 0;
            for (int index = 1; index <= 3; index++)
                if (FindDeepChild(prefab.transform, $"Star_{index}") != null) stars++;
            if (stars != 3)
                throw new InvalidOperationException("Mission Result V3 requires exactly three live star roots.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 14)
                throw new InvalidOperationException($"Mission Result V3 requires layered procedural gradients; found {gradients}.");
            Debug.Log($"[MissionResultV3PrefabBuilder] validation=Passed stars={stars} gradients={gradients} actions=one-per-state art=aspect-preserved");
        }

        private static HeaderBindings BuildHeader(RectTransform parent)
        {
            Color green = new Color32(112, 205, 48, 255);
            Color gold = new Color32(248, 177, 23, 255);

            RectTransform emblemPanel = CreatePanel("OutcomeEmblemPanel", parent, 15f, 16f, 165f, 171f, DarkTop, DarkBottom, green, 3f);
            Image emblem = CreateImage("OutcomeEmblem", emblemPanel, RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), green);
            SetTopLeft(emblem.rectTransform, 28f, 24f, 109f, 123f);

            RectTransform titlePanel = CreatePanel("OutcomeTitlePanel", parent, 180f, 16f, 485f, 171f, DarkTop, DarkBottom, Line, 3f);
            TMP_Text title = CreateText(titlePanel, "TitleText", 28f, 27f, 430f, 116f, "VICTORY", 68f, green, TextAlignmentOptions.MidlineLeft, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 40f;
            title.fontSizeMax = 68f;

            RectTransform starPanel = CreatePanel("StarPanel", parent, 665f, 16f, 333f, 171f, DarkTop, DarkBottom, Line, 3f);
            GameObject[] stars = new GameObject[3];
            for (int index = 0; index < stars.Length; index++)
            {
                RectTransform star = CreateTopLeft($"Star_{index + 1}", starPanel, 18f + index * 99f, 18f, 95f, 92f);
                Image filledStar = CreateImage(
                    "StarFilled", star,
                    RequireSprite(V3UiFoundationBuilder.MissionStarIconPath), Color.white);
                SetTopLeft(filledStar.rectTransform, 10f, 6f, 75f, 75f);
                RectTransform starOutline = CreateTopLeft("StarOutline", star, 10f, 6f, 75f, 75f);
                starOutline.gameObject.AddComponent<V3StarGraphic>().Configure(theme.OrangeRed, true, DarkTop);
                starOutline.gameObject.SetActive(false);
                stars[index] = star.gameObject;
            }
            TMP_Text starCount = CreateText(starPanel, "StarCountText", 0f, 112f, 333f, 45f, "3 / 3 STARS", 27f, gold, TextAlignmentOptions.Center, true);

            RectTransform identityPanel = CreatePanel("MissionIdentityPanel", parent, 998f, 16f, 456f, 171f, DarkTop, DarkBottom, Line, 3f);
            TMP_Text identity = CreateText(identityPanel, "V3MissionIdentityText", 28f, 24f, 400f, 91f, "M01 FIRST CONTACT\nOLD MARKET", 30f, theme.TextPrimary, TextAlignmentOptions.TopLeft, true, true);
            identity.lineSpacing = 7f;
            TMP_Text missionStatus = CreateText(identityPanel, "MissionStatusText", 28f, 115f, 400f, 41f, "MISSION COMPLETE", 26f, green, TextAlignmentOptions.MidlineLeft, true);

            RectTransform timePanel = CreatePanel("MissionTimerPanel", parent, 1464f, 16f, 193f, 171f, DarkTop, DarkBottom, Line, 3f);
            Image timerIcon = CreateImage("TimerIcon", timePanel, RequireSprite(V3UiFoundationBuilder.OperationsTimeIconPath), theme.Blue);
            SetTopLeft(timerIcon.rectTransform, 68f, 28f, 57f, 57f);
            TMP_Text elapsed = CreateText(timePanel, "MissionMetaText", 15f, 100f, 163f, 52f, "03:14", 30f, theme.Blue, TextAlignmentOptions.Center, true);

            return new HeaderBindings(
                title, identity, missionStatus, starCount, elapsed, stars, emblem,
                timerIcon, emblemPanel.GetComponent<V3GradientGraphic>());
        }

        private static ObjectiveBindings BuildObjectives(RectTransform parent)
        {
            Color green = new Color32(112, 190, 49, 255);
            RectTransform panel = CreatePanel("ObjectivesPanel", parent, 15f, 318f, 675f, 346f, DarkTop, DarkBottom, Line, 3f);
            Image titleIcon = CreateImage("ObjectivesIcon", panel, RequireSprite(V3UiFoundationBuilder.FirstLaunchTargetIconPath), green);
            SetTopLeft(titleIcon.rectTransform, 24f, 19f, 48f, 48f);
            TMP_Text title = CreateText(panel, "SectionTitleText", 89f, 16f, 300f, 54f, "OBJECTIVES", 29f, green, TextAlignmentOptions.MidlineLeft, true);

            ObjectiveRow patrol = BuildObjectiveRow(
                panel, "Objective_DestroyHostilePatrol", 82f,
                RequireSprite(V3UiFoundationBuilder.FirstLaunchTargetIconPath),
                "NEUTRALIZE HOSTILE PATROL", "COMPLETE", green);
            ObjectiveRow squad = BuildObjectiveRow(
                panel, "Objective_KeepCommandSquadAlive", 171f,
                RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath),
                "KEEP COMMAND SQUAD ALIVE", "COMPLETE", green);
            ObjectiveRow civilians = BuildObjectiveRow(
                panel, "Objective_CityConsequenceNeutral", 260f,
                RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath),
                "CIVILIAN CONSEQUENCE", "STABLE", green);
            return new ObjectiveBindings(
                title, patrol.Status, squad.Status, civilians.Status,
                new[] { titleIcon, patrol.Icon, squad.Icon, civilians.Icon });
        }

        private static ObjectiveRow BuildObjectiveRow(
            Transform parent, string name, float y, Sprite iconSprite, string label, string status, Color accent)
        {
            RectTransform row = CreatePanel(name, parent, 3f, y, 669f, 86f, RowTop, RowBottom, Color.clear, 0f);
            CreateSeparator(row, 0f);
            Image icon = CreateImage("Icon", row, iconSprite, accent);
            SetTopLeft(icon.rectTransform, 24f, 21f, 46f, 46f);
            CreateText(row, "Label", 92f, 11f, 405f, 64f, label, 23f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text state = CreateText(row, "Status", 494f, 11f, 150f, 64f, status, 22f, accent, TextAlignmentOptions.MidlineRight, true);
            return new ObjectiveRow(state, icon);
        }

        private static PerformanceBindings BuildPerformance(RectTransform parent)
        {
            RectTransform panel = CreatePanel("PerformancePanel", parent, 1150f, 313f, 475f, 220f, DarkTop, DarkBottom, Line, 3f);
            Image titleIcon = CreateImage("PerformanceIcon", panel, RequireSprite(V3UiFoundationBuilder.FirstLaunchTargetIconPath), theme.Blue);
            SetTopLeft(titleIcon.rectTransform, 23f, 15f, 45f, 45f);
            TMP_Text title = CreateText(panel, "SectionTitleText", 84f, 12f, 310f, 52f, "PERFORMANCE", 27f, theme.Blue, TextAlignmentOptions.MidlineLeft, true);

            TMP_Text losses = BuildPerformanceRow(panel, "UnitsLostCard", 65f, "SQUAD LOSSES", "0");
            TMP_Text defeated = BuildPerformanceRow(panel, "EnemiesDefeatedCard", 116f, "ENEMIES DEFEATED", "3 / 3");
            TMP_Text civilians = BuildPerformanceRow(panel, "CiviliansLostCard", 167f, "CIVILIANS LOST", "0");
            return new PerformanceBindings(title, titleIcon, losses, defeated, civilians);
        }

        private static TMP_Text BuildPerformanceRow(Transform parent, string name, float y, string label, string value)
        {
            RectTransform row = CreatePanel(name, parent, 3f, y, 469f, 50f, RowTop, RowBottom, Color.clear, 0f);
            CreateSeparator(row, 0f);
            CreateText(row, "Label", 29f, 0f, 300f, 50f, label, 21f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            return CreateText(row, "ValueText", 346f, 0f, 91f, 50f, value, 22f, theme.Blue, TextAlignmentOptions.MidlineRight, true);
        }

        private static RewardBindings BuildRewards(RectTransform parent)
        {
            RectTransform panel = CreatePanel("RewardsPanel", parent, 1150f, 539f, 475f, 175f, DarkTop, DarkBottom, Line, 3f);
            RectTransform titleIcon = CreateTopLeft("RewardsIcon", panel, 23f, 15f, 45f, 45f);
            V3StarGraphic titleStar = titleIcon.gameObject.AddComponent<V3StarGraphic>();
            titleStar.Configure(theme.Blue, false, DarkTop);
            TMP_Text title = CreateText(panel, "SectionTitleText", 84f, 12f, 310f, 52f, "REWARDS", 27f, theme.Blue, TextAlignmentOptions.MidlineLeft, true);
            CreateSeparator(panel, 65f);

            RectTransform iconRoot = CreateRect("VictoryRewardIcons", panel);
            Stretch(iconRoot);
            Image credits = CreateImage("CreditsIcon", iconRoot, catalog.CreditsIcon, Color.white);
            SetTopLeft(credits.rectTransform, 24f, 80f, 43f, 43f);
            Image xp = CreateImage("CommanderXpIcon", iconRoot, RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), new Color32(246, 174, 23, 255));
            SetTopLeft(xp.rectTransform, 24f, 126f, 43f, 43f);
            TMP_Text rewardText = CreateText(panel, "AuthoritativeRewardsText", 86f, 72f, 354f, 97f, "CREDITS     +1,200\nCOMMANDER XP     +260", 22f, new Color32(246, 174, 23, 255), TextAlignmentOptions.MidlineRight, true, true);
            rewardText.lineSpacing = 12f;
            return new RewardBindings(title, titleStar, rewardText, iconRoot.gameObject);
        }

        private static FooterBindings BuildFooter(RectTransform parent)
        {
            RectTransform summaryPanel = CreatePanel("ConsequenceRow", parent, 15f, 790f, 754f, 134f, DarkTop, DarkBottom, Line, 3f);
            Image accent = CreateImage("OutcomeAccent", summaryPanel, null, new Color32(86, 187, 49, 255));
            SetTopLeft(accent.rectTransform, 34f, 42f, 3f, 50f);
            TMP_Text summary = CreateText(summaryPanel, "ConsequenceText", 58f, 23f, 670f, 88f, "The hostile patrol is neutralized. The corridor is secure.", 24f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true, true);

            Button continueButton = CreateActionButton(
                "ContinueButton", parent, VictoryTop, VictoryBottom, theme.Green,
                RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), "CONTINUE",
                out TMP_Text continueLabel, out V3GradientGraphic continueGradient);
            Button retryButton = CreateActionButton(
                "ReplayButton", parent, LossTop, LossBottom, theme.OrangeRed,
                catalog.ResetIcon, "RETRY", out TMP_Text retryLabel, out V3GradientGraphic retryGradient);
            retryButton.gameObject.SetActive(false);
            return new FooterBindings(
                summary, accent, continueButton, continueLabel, continueGradient,
                retryButton, retryLabel, retryGradient);
        }

        private static Button CreateActionButton(
            string name, Transform parent, Color top, Color bottom, Color border,
            Sprite iconSprite, string label, out TMP_Text labelText, out V3GradientGraphic gradient)
        {
            RectTransform rect = CreatePanel(name, parent, 769f, 790f, 888f, 134f, top, bottom, border, 3f);
            gradient = rect.GetComponent<V3GradientGraphic>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = gradient;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color32(225, 248, 225, 255),
                pressedColor = new Color32(175, 210, 175, 255),
                selectedColor = Color.white,
                disabledColor = new Color32(90, 97, 91, 170),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            Image icon = CreateImage("Icon", rect, iconSprite, theme.TextPrimary);
            SetTopLeft(icon.rectTransform, 716f, 34f, 67f, 67f);
            labelText = CreateText(rect, "Label", 146f, 10f, 596f, 114f, label, 62f, theme.TextPrimary, TextAlignmentOptions.Center, true);
            return button;
        }

        private static GameObject[] BuildCompatibilityRoots(Transform parent)
        {
            RectTransform identity = CreateRect("MissionIdentityBlock", parent);
            Stretch(identity);
            CreateText(identity, "MissionNameText", 0f, 0f, 200f, 40f, string.Empty, 20f, theme.TextPrimary, TextAlignmentOptions.Center, true);
            identity.gameObject.SetActive(false);

            RectTransform canonical = CreateRect("LegacyCanonicalReferences", parent);
            Stretch(canonical);
            Image credits = CreateImage("CreditsReward", canonical, catalog.CreditsIcon, Color.white);
            Image materials = CreateImage("MaterialsReward", canonical, catalog.MaterialsIcon, Color.white);
            credits.rectTransform.sizeDelta = new Vector2(16f, 16f);
            materials.rectTransform.sizeDelta = new Vector2(16f, 16f);
            canonical.gameObject.SetActive(false);
            return new[] { identity.gameObject, canonical.gameObject };
        }

        private static void Capture(string outputPath, int width, int height, UiMissionResultPopupModel model)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Mission Result prefab for capture: {PrefabPath}");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("MissionResultV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new("MissionResultV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            Stretch(instance.transform as RectTransform);
            MissionResultPopupView view = instance.GetComponent<MissionResultPopupView>();
            view.Apply(in model);
            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
                layout.RefreshLayout();
            Canvas.ForceUpdateCanvases();

            RenderTexture previous = RenderTexture.active;
            Texture2D capture = new(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply(false);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
                Debug.Log($"[MissionResultV3PrefabBuilder] capture=Passed outcome={model.Outcome} size={width}x{height} path={outputPath} scene={scene.name}");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void LoadAssets()
        {
            boldFont = RequireAsset<TMP_FontAsset>(BoldFontPath);
            mediumFont = RequireAsset<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
        }

        private static RectTransform CreatePanel(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(
            Transform parent, string name, float x, float y, float width, float height,
            string value, float size, Color color, TextAlignmentOptions alignment,
            bool bold, bool wrap = false)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            Image image = V3UiPrefabFactory.CreateImage(name, parent, sprite, color, false);
            image.preserveAspect = sprite != null;
            return image;
        }

        private static void CreateSeparator(Transform parent, float y)
        {
            Image line = CreateImage("Separator", parent, null, Line);
            SetTopLeft(line.rectTransform, 0f, y, (parent as RectTransform).rect.width, 1f);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing Mission Result V3 asset: {path}");
            return asset;
        }

        private static Sprite RequireSprite(string path) => RequireAsset<Sprite>(path);

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDeepChild(root.GetChild(index), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent) =>
            V3UiPrefabFactory.CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = V3UiPrefabFactory.CreateRect(
                name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private readonly struct HeaderBindings
        {
            public readonly TMP_Text Title;
            public readonly TMP_Text MissionIdentity;
            public readonly TMP_Text MissionStatus;
            public readonly TMP_Text StarCount;
            public readonly TMP_Text Elapsed;
            public readonly GameObject[] Stars;
            public readonly Image Emblem;
            public readonly Image TimerIcon;
            public readonly V3GradientGraphic EmblemPanel;

            public HeaderBindings(
                TMP_Text title, TMP_Text missionIdentity, TMP_Text missionStatus, TMP_Text starCount,
                TMP_Text elapsed, GameObject[] stars, Image emblem, Image timerIcon, V3GradientGraphic emblemPanel)
            {
                Title = title;
                MissionIdentity = missionIdentity;
                MissionStatus = missionStatus;
                StarCount = starCount;
                Elapsed = elapsed;
                Stars = stars;
                Emblem = emblem;
                TimerIcon = timerIcon;
                EmblemPanel = emblemPanel;
            }
        }

        private readonly struct ObjectiveBindings
        {
            public readonly TMP_Text Title;
            public readonly TMP_Text PatrolStatus;
            public readonly TMP_Text SquadStatus;
            public readonly TMP_Text CivilianStatus;
            public readonly Image[] Icons;

            public ObjectiveBindings(TMP_Text title, TMP_Text patrol, TMP_Text squad, TMP_Text civilian, Image[] icons)
            {
                Title = title;
                PatrolStatus = patrol;
                SquadStatus = squad;
                CivilianStatus = civilian;
                Icons = icons;
            }
        }

        private readonly struct ObjectiveRow
        {
            public readonly TMP_Text Status;
            public readonly Image Icon;
            public ObjectiveRow(TMP_Text status, Image icon)
            {
                Status = status;
                Icon = icon;
            }
        }

        private readonly struct PerformanceBindings
        {
            public readonly TMP_Text Title;
            public readonly Image TitleIcon;
            public readonly TMP_Text SquadLosses;
            public readonly TMP_Text EnemiesDefeated;
            public readonly TMP_Text CiviliansLost;

            public PerformanceBindings(TMP_Text title, Image titleIcon, TMP_Text squadLosses, TMP_Text enemiesDefeated, TMP_Text civiliansLost)
            {
                Title = title;
                TitleIcon = titleIcon;
                SquadLosses = squadLosses;
                EnemiesDefeated = enemiesDefeated;
                CiviliansLost = civiliansLost;
            }
        }

        private readonly struct RewardBindings
        {
            public readonly TMP_Text Title;
            public readonly V3StarGraphic TitleStar;
            public readonly TMP_Text RewardText;
            public readonly GameObject IconRoot;
            public RewardBindings(TMP_Text title, V3StarGraphic titleStar, TMP_Text rewardText, GameObject iconRoot)
            {
                Title = title;
                TitleStar = titleStar;
                RewardText = rewardText;
                IconRoot = iconRoot;
            }
        }

        private readonly struct FooterBindings
        {
            public readonly TMP_Text Summary;
            public readonly Image Accent;
            public readonly Button ContinueButton;
            public readonly TMP_Text ContinueLabel;
            public readonly V3GradientGraphic ContinueGradient;
            public readonly Button RetryButton;
            public readonly TMP_Text RetryLabel;
            public readonly V3GradientGraphic RetryGradient;

            public FooterBindings(
                TMP_Text summary, Image accent,
                Button continueButton, TMP_Text continueLabel, V3GradientGraphic continueGradient,
                Button retryButton, TMP_Text retryLabel, V3GradientGraphic retryGradient)
            {
                Summary = summary;
                Accent = accent;
                ContinueButton = continueButton;
                ContinueLabel = continueLabel;
                ContinueGradient = continueGradient;
                RetryButton = retryButton;
                RetryLabel = retryLabel;
                RetryGradient = retryGradient;
            }
        }
    }
}
#endif
