#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// Rebuilds POP-13's normal (non-tutorial) surface from the V3 target lock.
    /// Chrome and telemetry decoration are procedural; the portrait and icons
    /// come from the shared V3 atlases so no screen-specific duplicates exist.
    /// </summary>
    public static class AriaCommandAssistantV3PrefabBuilder
    {
        public const string PrefabPath = AriaTutorialBriefingPrefabBuilder.PrefabPath;
        public const string PortraitPath = AriaTutorialBriefingPrefabBuilder.PortraitPath;

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(7, 26, 34, 253);
        private static readonly Color DarkBottom = new Color32(0, 6, 10, 255);
        private static readonly Color RaisedTop = new Color32(20, 47, 58, 255);
        private static readonly Color RaisedBottom = new Color32(2, 14, 21, 255);
        private static readonly Color BlueTop = new Color32(8, 116, 174, 255);
        private static readonly Color BlueBottom = new Color32(2, 45, 79, 255);
        private static readonly Color Cyan = new Color32(0, 197, 239, 255);
        private static readonly Color CyanMuted = new Color32(0, 130, 174, 210);
        private static readonly Color Green = new Color32(91, 220, 41, 255);
        private static readonly Color Orange = new Color32(255, 76, 12, 255);
        private static readonly Color Text = new Color32(240, 243, 239, 255);
        private static readonly Color Muted = new Color32(195, 202, 199, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Build ARIA Command Assistant V3")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            boldFont = RequireAsset<TMP_FontAsset>(BoldFontPath);
            mediumFont = RequireAsset<TMP_FontAsset>(MediumFontPath);
            Sprite portrait = RequireAsset<Sprite>(PortraitPath);
            Sprite hostile = RequireAsset<Sprite>(V3UiFoundationBuilder.MatchHostileMarkerIconPath);
            Sprite integrity = RequireAsset<Sprite>(V3UiFoundationBuilder.MatchHoldIconPath);
            Sprite range = RequireAsset<Sprite>(V3UiFoundationBuilder.MatchScanIconPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new InvalidOperationException($"Missing POP-13 prefab at {PrefabPath}.");

            try
            {
                Transform existing = root.transform.Find("LandscapeLayout");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                RectTransform layout = CreateTopLeft(
                    "LandscapeLayout", root.transform, 0f, 0f, Reference.x, Reference.y);
                layout.gameObject.AddComponent<AriaTutorialHudVariantLayoutView>();

                RectTransform panel = CreatePanel(
                    "CommandAssistantPanel", layout, 1150f, 10f, 510f, 690f,
                    DarkTop, DarkBottom, Cyan, 3f, true);

                CreateText(
                    "CommandAssistantHeading", panel, 18f, 11f, 422f, 43f,
                    "ARIA COMMAND ASSISTANT", 28f, Cyan,
                    TextAlignmentOptions.MidlineLeft, true);
                Button headerClose = CreateButton(
                    "HeaderCloseButton", panel, 451f, 8f, 49f, 49f,
                    RaisedTop, RaisedBottom, Cyan, out _, string.Empty, 1f);
                CreateCloseGlyph(headerClose.transform as RectTransform);

                RectTransform portraitClip = CreateTopLeft("AriaPortraitClip", panel, 103f, 53f, 305f, 224f);
                portraitClip.gameObject.AddComponent<RectMask2D>();
                Image portraitImage = CreateImage(
                    "AriaPortraitV3", portraitClip, portrait, Color.white, false);
                Stretch(portraitImage.rectTransform);
                portraitImage.preserveAspect = false;
                AspectRatioFitter portraitFitter = portraitImage.gameObject.AddComponent<AspectRatioFitter>();
                portraitFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                portraitFitter.aspectRatio = portrait.rect.width / portrait.rect.height;
                V3GradientGraphic portraitFade = CreateGradient(
                    "AriaPortraitFade", portraitClip,
                    new Color(0f, .03f, .05f, 0f), new Color(0f, .025f, .04f, .86f),
                    Color.clear, 0f);
                Stretch(portraitFade.rectTransform);

                CreateTechDecoration(panel, 20f, 70f, false);
                CreateTechDecoration(panel, 421f, 72f, true);
                CreateReticle(panel, 454f, 187f);
                CreateSolid("PortraitDivider", panel, 10f, 279f, 490f, 2f,
                    new Color(Cyan.r, Cyan.g, Cyan.b, .65f));

                RectTransform reportPanel = CreatePanel(
                    "TacticalReportPanel", panel, 10f, 289f, 490f, 318f,
                    new Color32(2, 20, 27, 245), DarkBottom, CyanMuted, 3f);
                TMP_Text recommendationTitle = CreateText(
                    "RecommendationTitle", reportPanel, 14f, 7f, 460f, 35f,
                    "TACTICAL REPORTS", 22f, Cyan,
                    TextAlignmentOptions.MidlineLeft, true);
                TMP_Text recommendationReason = CreateText(
                    "RecommendationReason", reportPanel, 14f, 44f, 460f, 54f,
                    "Hostile infantry squad detected near market stalls.\n" +
                    "They are moving between cover positions.",
                    17f, Text, TextAlignmentOptions.TopLeft, false, false);
                recommendationReason.enableAutoSizing = true;
                recommendationReason.fontSizeMin = 14f;
                recommendationReason.fontSizeMax = 17f;

                RectTransform targetLock = CreatePanel(
                    "TargetLockPanel", reportPanel, 13f, 105f, 464f, 137f,
                    new Color32(0, 12, 17, 255), new Color32(0, 5, 8, 255),
                    CyanMuted, 3f);
                BuildTargetRow(targetLock, 0f, hostile, Orange, "TARGET", "TargetNameText",
                    "ENEMY INFANTRY SQUAD");
                BuildTargetRow(targetLock, 45f, integrity, Green, "INTEGRITY", "HealthText", "HIGH");
                BuildTargetRow(targetLock, 90f, range, Cyan, "RANGE", "DistanceText", "140m");

                Button showMe = CreateButton(
                    "ShowMeButton", reportPanel, 13f, 250f, 464f, 57f,
                    BlueTop, BlueBottom, Cyan, out TMP_Text showMeLabel, "SHOW ME", 24f);
                showMeLabel.gameObject.name = "ShowMeButtonLabel";

                RectTransform voice = CreatePanel(
                    "VoicePanel", panel, 10f, 615f, 490f, 65f,
                    new Color32(1, 23, 30, 252), DarkBottom, CyanMuted, 3f);
                RectTransform narrationChip = CreateTopLeft("NarrationStateChip", voice, 13f, 5f, 163f, 25f);
                CreateText("NarrationStateText", narrationChip, 0f, 0f, 163f, 25f,
                    "ARIA VOICE", 17f, Cyan, TextAlignmentOptions.MidlineLeft, true);
                CreateText("NarrationSubtitle", voice, 13f, 30f, 250f, 26f,
                    "MOVE ORDER CONFIRMED.", 15f, Text,
                    TextAlignmentOptions.MidlineLeft, false);
                RectTransform waveform = CreateTopLeft("NarrationWaveform", voice, 265f, 17f, 136f, 34f);
                BuildWaveform(waveform);
                BuildVoiceEnabledIndicator(voice);

                RectTransform takeover = BuildAssistantTakeoverSurface(layout, portrait);

                RectTransform compatibility = CreateTopLeft(
                    "CompatibilityBindings", layout, -2000f, -2000f, 10f, 10f);
                BuildCompatibilityBindings(compatibility);

                MainMenuV3SectionLayoutView responsive =
                    layout.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                responsive.Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    new[] { panel },
                    true,
                    new[] { takeover },
                    Array.Empty<RectTransform>());

                layout.gameObject.SetActive(true);
                takeover.gameObject.SetActive(false);
                layout.SetAsFirstSibling();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[AriaCommandAssistantV3PrefabBuilder] result=Passed layout=top-right " +
                      "portrait=aria-v3 gradients=procedural borders=3 input=panel-only");
        }

        public static void BuildAndValidate()
        {
            Build();
            Validate();
            Debug.Log("[AriaCommandAssistantV3Validation] result=Passed");
        }

        [MenuItem("Game/UI/V3/Capture ARIA Command Assistant V3 Prefab Review")]
        public static void CaptureReview()
        {
            MatchHudV3PrefabBuilder.CaptureAriaCommandAssistantReview();
        }

        public static void Validate()
        {
            GameObject prefab = RequireAsset<GameObject>(PrefabPath);
            AriaCommandAssistantPopupView view = prefab.GetComponent<AriaCommandAssistantPopupView>();
            if (view == null || !view.TryBindHierarchy())
                throw new InvalidOperationException("POP-13 V3 runtime hierarchy is incomplete.");
            if (view.CommandAssistantPanel == null ||
                view.CommandAssistantPanel.anchorMin != new Vector2(0f, 1f) ||
                view.CommandAssistantPanel.anchoredPosition.x < 1100f ||
                view.CommandAssistantPanel.rect.width < 500f)
            {
                throw new InvalidOperationException("POP-13 V3 must remain a readable top-right panel.");
            }
            if (view.LandscapeLayout.GetComponent<Graphic>() != null)
                throw new InvalidOperationException("POP-13 full-screen layout must not consume battlefield input.");
            MainMenuV3SectionLayoutView responsive =
                view.LandscapeLayout.GetComponent<MainMenuV3SectionLayoutView>();
            if (responsive == null || responsive.ReferenceResolution != Reference ||
                !responsive.ExpandToCanvasWidth || responsive.RightAnchoredTargets.Length != 1)
            {
                throw new InvalidOperationException("POP-13 V3 responsive layout is incomplete.");
            }
            if (view.LandscapeLayout.GetComponent<AriaTutorialHudVariantLayoutView>() == null)
                throw new InvalidOperationException("POP-13 V3 must apply its compact Match HUD header variant.");

            Image portrait = FindNamed(view.transform, "AriaPortraitV3")?.GetComponent<Image>();
            if (portrait == null || AssetDatabase.GetAssetPath(portrait.sprite) != PortraitPath ||
                portrait.GetComponent<AspectRatioFitter>() == null)
            {
                throw new InvalidOperationException("POP-13 V3 ARIA portrait must use a non-stretched aspect crop.");
            }

            V3GradientGraphic[] gradients =
                view.CommandAssistantPanel.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 7)
                throw new InvalidOperationException($"POP-13 V3 requires visible procedural gradients; found {gradients.Length}.");
            for (int i = 0; i < gradients.Length; i++)
            {
                SerializedObject serialized = new(gradients[i]);
                float borderWidth = serialized.FindProperty("borderWidth").floatValue;
                if (borderWidth != 0f && !Mathf.Approximately(borderWidth, 3f))
                {
                    throw new InvalidOperationException(
                        $"POP-13 V3 border {gradients[i].name} is {borderWidth}; expected 3.");
                }
            }

            Debug.Log($"[AriaCommandAssistantV3Validation] result=Passed gradients={gradients.Length} borders=3");
        }

        public static UiAssistantPanelModel CreateAssistantTakeoverPreviewModel()
        {
            return new UiAssistantPanelModel(
                3,
                true,
                48,
                new UiAssistantGoalRowModel(
                    true, 10, "Move to cover", string.Empty, 0, 3, true),
                new UiAssistantGoalRowModel(
                    true, 11, "Hold position", string.Empty, 0, 1, false),
                new UiAssistantGoalRowModel(
                    true, 12, "Scan patrol route", string.Empty, 0, 1, false),
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantTargetLockModel.Empty,
                UiAssistantNarrationModel.Empty,
                true,
                "MOVE RIFLE SQUAD TO COVER",
                "Move to cover",
                "HIGH",
                "STOP",
                false,
                false,
                true,
                false,
                "ARIA CONTROL",
                "ARIA is executing a bounded action. STOP returns control.");
        }

        private static void BuildTargetRow(
            Transform parent,
            float y,
            Sprite iconSprite,
            Color iconColor,
            string label,
            string valueName,
            string value)
        {
            RectTransform row = CreatePanel(
                label + "Row", parent, 0f, y, 464f, 47f,
                new Color32(3, 18, 23, 255), new Color32(0, 8, 12, 255),
                CyanMuted, 3f);
            Image icon = CreateImage(label + "IconV3", row, iconSprite, iconColor, false);
            SetTopLeft(icon.rectTransform, 5f, 3f, 41f, 41f);
            icon.preserveAspect = true;
            CreateText(label + "Label", row, 51f, 4f, 145f, 39f,
                label, 17f, Text, TextAlignmentOptions.MidlineLeft, false);
            TMP_Text valueText = CreateText(valueName, row, 211f, 4f, 241f, 39f,
                value, 17f, label == "INTEGRITY" ? Green : Text,
                TextAlignmentOptions.MidlineLeft, false);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 13f;
            valueText.fontSizeMax = 17f;
        }

        private static void BuildCompatibilityBindings(RectTransform parent)
        {
            CreateButton("CloseButton", parent, 0f, 0f, 1f, 1f,
                DarkTop, DarkBottom, Color.clear, out _, string.Empty, 1f);
            Button doIt = CreateButton("DoItButton", parent, 0f, 0f, 1f, 1f,
                DarkTop, DarkBottom, Color.clear, out TMP_Text doItLabel, string.Empty, 1f);
            doItLabel.gameObject.name = "DoItButtonLabel";
            doIt.gameObject.SetActive(false);
            RectTransform elapsed = CreateTopLeft("ElapsedChip", parent, 0f, 0f, 1f, 1f);
            CreateText("ElapsedText", elapsed, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);

            for (int i = 0; i < 3; i++)
                BuildMessageBinding(parent, "Alert", i);
            for (int i = 0; i < 2; i++)
                BuildMessageBinding(parent, "Report", i);

            CreateText("RecommendationPriorityText", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText("RecommendationTargetSummary", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText("TakeoverIntentDetail", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateTopLeft("RecommendationSignalLine", parent, 0f, 0f, 1f, 1f);
            CreateText("SourceNameText", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText("FactionRelationText", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText("ReadinessText", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText("TargetReasonText", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateTopLeft("TargetMarker0", parent, 0f, 0f, 1f, 1f);
            CreateTopLeft("TargetMarker1", parent, 0f, 0f, 1f, 1f);
            CreateTopLeft("TargetMarker2", parent, 0f, 0f, 1f, 1f);
            CreateText("NarrationFailureReason", parent, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            Image radar = CreateImage("RadarScanDisc", parent, null, Color.clear, false);
            SetTopLeft(radar.rectTransform, 0f, 0f, 1f, 1f);

            parent.gameObject.SetActive(false);
        }

        private static RectTransform BuildAssistantTakeoverSurface(Transform parent, Sprite portrait)
        {
            RectTransform surface = CreatePanel(
                "AssistantTakeoverSurface", parent, 443f, 225f, 785f, 470f,
                new Color32(13, 31, 37, 252), new Color32(2, 8, 11, 255),
                new Color32(87, 104, 108, 255), 3f, true);

            RectTransform portraitClip = CreateTopLeft(
                "TakeoverPortraitClip", surface, 20f, 17f, 228f, 305f);
            portraitClip.gameObject.AddComponent<RectMask2D>();
            Image portraitImage = CreateImage(
                "TakeoverAriaPortraitV3", portraitClip, portrait, Color.white, false);
            Stretch(portraitImage.rectTransform);
            portraitImage.preserveAspect = false;
            AspectRatioFitter fitter = portraitImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = portrait.rect.width / portrait.rect.height;
            V3GradientGraphic portraitFade = CreateGradient(
                "TakeoverPortraitFade", portraitClip,
                new Color(0f, .03f, .05f, 0f), new Color(0f, .025f, .04f, .92f),
                Color.clear, 0f);
            Stretch(portraitFade.rectTransform);

            TMP_Text state = CreateText(
                "ControlStateText", surface, 263f, 17f, 494f, 47f,
                "ARIA CONTROLLING", 30f, Cyan,
                TextAlignmentOptions.MidlineLeft, true);
            state.enableAutoSizing = true;
            state.fontSizeMin = 23f;
            state.fontSizeMax = 30f;
            CreateText(
                "TakeoverSubtitle", surface, 263f, 62f, 494f, 32f,
                "Temporary assistance in progress", 18f, Muted,
                TextAlignmentOptions.MidlineLeft, false);

            RectTransform intent = CreatePanel(
                "TakeoverIntentPanel", surface, 255f, 101f, 512f, 242f,
                new Color32(5, 25, 33, 250), new Color32(1, 11, 16, 255),
                CyanMuted, 3f);
            CreateText(
                "CurrentIntentLabel", intent, 15f, 8f, 470f, 28f,
                "CURRENT INTENT", 18f, Cyan,
                TextAlignmentOptions.MidlineLeft, true);
            TMP_Text intentTitle = CreateText(
                "TakeoverIntentTitle", intent, 15f, 40f, 475f, 43f,
                "MOVE RIFLE SQUAD TO COVER", 24f, Text,
                TextAlignmentOptions.MidlineLeft, true);
            intentTitle.enableAutoSizing = true;
            intentTitle.fontSizeMin = 18f;
            intentTitle.fontSizeMax = 24f;
            BuildTakeoverGoalRow(intent, 0, 88f, true, "Move to cover");
            BuildTakeoverGoalRow(intent, 1, 137f, false, "Hold position");
            BuildTakeoverGoalRow(intent, 2, 186f, false, "Scan patrol route");

            CreateText(
                "TakeoverResumeHint", surface, 265f, 350f, 492f, 32f,
                "You can resume command at any time.", 17f, Muted,
                TextAlignmentOptions.MidlineLeft, false);
            CreateButton(
                "ResumeCommandButton", surface, 17f, 391f, 378f, 61f,
                BlueTop, BlueBottom, Cyan, out _, "RESUME COMMAND", 23f);
            Button stop = CreateButton(
                "StopButton", surface, 407f, 391f, 360f, 61f,
                new Color32(174, 48, 37, 255), new Color32(74, 12, 12, 255),
                Orange, out TMP_Text stopLabel, "STOP ARIA", 23f);
            stopLabel.gameObject.name = "StopButtonLabel";
            return surface;
        }

        private static void BuildTakeoverGoalRow(
            Transform parent,
            int index,
            float y,
            bool primary,
            string title)
        {
            RectTransform row = CreatePanel(
                $"GoalRow{index}", parent, 14f, y, 484f, 43f,
                primary ? new Color32(8, 100, 139, 255) : new Color32(34, 38, 39, 255),
                primary ? new Color32(2, 46, 70, 255) : new Color32(12, 15, 16, 255),
                primary ? Cyan : new Color32(66, 72, 73, 255), 3f);
            CreateText(
                $"Goal{index}Icon", row, 12f, 2f, 30f, 37f,
                (index + 1).ToString(), 20f, Text,
                TextAlignmentOptions.Center, false);
            CreateTopLeft($"Goal{index}StateChip", row, 0f, 0f, 1f, 1f);
            CreateTopLeft($"Goal{index}PriorityRail", row, 0f, 0f, 1f, 1f);
            CreateText(
                $"Goal{index}Title", row, 47f, 2f, 264f, 37f,
                title, 17f, Text, TextAlignmentOptions.MidlineLeft, false);
            RectTransform hiddenBody = CreateTopLeft(
                $"Goal{index}BodyBinding", row, 0f, 0f, 1f, 1f);
            CreateText(
                $"Goal{index}Body", hiddenBody, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            hiddenBody.gameObject.SetActive(false);
            CreateText(
                $"Goal{index}StateText", row, 315f, 2f, 157f, 37f,
                primary ? "IN PROGRESS" : string.Empty, 14f,
                primary ? Cyan : Muted, TextAlignmentOptions.MidlineRight, false);
        }

        private static void BuildGoalBinding(Transform parent, int index)
        {
            RectTransform row = CreateTopLeft($"GoalRow{index}", parent, 0f, 0f, 1f, 1f);
            CreateTopLeft($"Goal{index}Icon", row, 0f, 0f, 1f, 1f);
            CreateTopLeft($"Goal{index}StateChip", row, 0f, 0f, 1f, 1f);
            CreateTopLeft($"Goal{index}PriorityRail", row, 0f, 0f, 1f, 1f);
            CreateText($"Goal{index}Title", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText($"Goal{index}Body", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText($"Goal{index}StateText", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
        }

        private static void BuildMessageBinding(Transform parent, string prefix, int index)
        {
            string rowPrefix = prefix + index;
            RectTransform row = CreateTopLeft($"{prefix}Row{index}", parent, 0f, 0f, 1f, 1f);
            CreateTopLeft(rowPrefix + "Icon", row, 0f, 0f, 1f, 1f);
            CreateTopLeft(rowPrefix + "PriorityChip", row, 0f, 0f, 1f, 1f);
            CreateTopLeft(rowPrefix + "PriorityRail", row, 0f, 0f, 1f, 1f);
            CreateText(rowPrefix + "Body", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText(rowPrefix + "Detail", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
            CreateText(rowPrefix + "PriorityText", row, 0f, 0f, 1f, 1f,
                string.Empty, 1f, Color.clear, TextAlignmentOptions.Center, false);
        }

        private static void BuildWaveform(Transform parent)
        {
            float[] heights = { 8f, 14f, 22f, 12f, 28f, 17f, 31f, 19f, 11f, 24f, 15f,
                9f, 20f, 13f, 26f, 16f, 10f, 22f, 12f, 18f, 8f, 14f, 7f };
            float spacing = 5.4f;
            for (int i = 0; i < heights.Length; i++)
            {
                float height = heights[i];
                CreateSolid($"Wave{i:00}", parent, 4f + i * spacing, (34f - height) * .5f,
                    2f, height, Cyan);
            }
        }

        private static void BuildVoiceEnabledIndicator(Transform parent)
        {
            RectTransform track = CreatePanel(
                "VoiceEnabledToggle", parent, 409f, 15f, 68f, 36f,
                new Color32(7, 156, 216, 255), new Color32(0, 83, 128, 255),
                Cyan, 3f, true);
            Toggle toggle = track.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track.GetComponent<V3GradientGraphic>();
            toggle.isOn = true;
            RectTransform knob = CreateTopLeft("EnabledKnob", track, 38f, 5f, 26f, 26f);
            V3DiscGraphic fill = knob.gameObject.AddComponent<V3DiscGraphic>();
            fill.Configure(new Color32(213, 226, 228, 255), 48);
        }

        private static void CreateTechDecoration(Transform parent, float x, float y, bool right)
        {
            float[] widths = { 62f, 45f, 79f, 57f, 83f, 50f, 68f, 39f, 76f };
            for (int i = 0; i < widths.Length; i++)
            {
                float width = widths[i];
                float lineX = right ? x + 82f - width : x;
                CreateSolid($"AssistantTech{(right ? "R" : "L")}{i:00}", parent,
                    lineX, y + i * 9f, width, i % 3 == 0 ? 2f : 1f,
                    new Color(CyanMuted.r, CyanMuted.g, CyanMuted.b, i % 2 == 0 ? .76f : .42f));
            }
            for (int i = 0; i < 10; i++)
            {
                float height = 8f + (i * 7 % 38);
                CreateSolid($"AssistantMeter{(right ? "R" : "L")}{i:00}", parent,
                    x + 4f + i * 6f, 168f + (48f - height), 2f, height,
                    new Color(Cyan.r, Cyan.g, Cyan.b, .82f));
            }
        }

        private static void CreateReticle(Transform parent, float centerX, float centerY)
        {
            RectTransform ring = CreateTopLeft(
                "AssistantReticle", parent, centerX - 24f, centerY - 24f, 48f, 48f);
            V3RingGraphic graphic = ring.gameObject.AddComponent<V3RingGraphic>();
            graphic.Configure(Cyan, 3f, 40);
            CreateSolid("AssistantReticleH", parent, centerX - 31f, centerY - 1f, 62f, 2f, Cyan);
            CreateSolid("AssistantReticleV", parent, centerX - 1f, centerY - 31f, 2f, 62f, Cyan);
            RectTransform dot = CreatePanel(
                "AssistantReticleDot", parent, centerX - 4f, centerY - 4f, 8f, 8f,
                Cyan, Cyan, Color.clear, 0f);
            dot.GetComponent<V3GradientGraphic>().raycastTarget = false;
        }

        private static void CreateCloseGlyph(RectTransform parent)
        {
            RectTransform first = CreateTopLeft("CloseStrokeA", parent, 11f, 23f, 27f, 3f);
            first.pivot = new Vector2(.5f, .5f);
            first.localRotation = Quaternion.Euler(0f, 0f, -45f);
            Image firstImage = first.gameObject.AddComponent<Image>();
            firstImage.color = Text;
            firstImage.raycastTarget = false;
            RectTransform second = CreateTopLeft("CloseStrokeB", parent, 11f, 23f, 27f, 3f);
            second.pivot = new Vector2(.5f, .5f);
            second.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image secondImage = second.gameObject.AddComponent<Image>();
            secondImage.color = Text;
            secondImage.raycastTarget = false;
        }

        private static RectTransform CreatePanel(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border, float borderWidth, bool raycast = false)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            graphic.raycastTarget = raycast;
            return rect;
        }

        private static V3GradientGraphic CreateGradient(
            string name, Transform parent, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            graphic.raycastTarget = false;
            return graphic;
        }

        private static Button CreateButton(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border, out TMP_Text label, string value, float fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height,
                top, bottom, border, 3f, true);
            V3GradientGraphic graphic = rect.GetComponent<V3GradientGraphic>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1f);
            colors.disabledColor = new Color(.42f, .42f, .42f, .72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            label = CreateText(name + "Text", rect, 4f, 3f, width - 8f, height - 6f,
                value, fontSize, Text, TextAlignmentOptions.Center, true);
            return button;
        }

        private static TMP_Text CreateText(
            string name, Transform parent, float x, float y, float width, float height,
            string value, float size, Color color, TextAlignmentOptions alignment, bool bold,
            bool noWrap = true)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = noWrap ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(
            string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void CreateSolid(
            string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject value = new(name, typeof(RectTransform)) { layer = 5 };
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopLeft(rect, x, y, width, height);
            return rect;
        }

        private static void SetTopLeft(
            RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing ARIA Command Assistant V3 asset at {path}.");
            return asset;
        }

        private static Transform FindNamed(Transform root, string objectName)
        {
            if (root == null)
                return null;
            if (root.name == objectName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), objectName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
#endif
