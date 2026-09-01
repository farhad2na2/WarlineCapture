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
    /// Rebuilds the persistent tutorial presentation inside POP-13 from the
    /// PREFAB-06 V3 lock. Panel and button chrome is procedural so it remains
    /// sharp at every supported aspect ratio without duplicating atlas art.
    /// </summary>
    public static class AriaTutorialBriefingPrefabBuilder
    {
        public const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab";
        public const string PortraitPath =
            "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_aria_v3.png";

        private static readonly Vector2 Reference = new(1672f, 941f);

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string PersianFontPath =
            "Assets/Game/Art/UI/Fonts/NotoSansArabic/NotoSansArabic-Narrative SDF.asset";

        private static readonly Color DarkTop = new Color32(8, 27, 35, 252);
        private static readonly Color DarkBottom = new Color32(0, 7, 11, 255);
        private static readonly Color RaisedTop = new Color32(25, 43, 49, 255);
        private static readonly Color RaisedBottom = new Color32(5, 15, 19, 255);
        private static readonly Color Cyan = new Color32(0, 209, 243, 255);
        private static readonly Color CyanMuted = new Color32(0, 145, 184, 210);
        private static readonly Color Green = new Color32(20, 229, 103, 255);
        private static readonly Color GreenTop = new Color32(7, 132, 63, 255);
        private static readonly Color GreenBottom = new Color32(2, 60, 31, 255);
        private static readonly Color Blue = new Color32(18, 169, 226, 255);
        private static readonly Color BlueTop = new Color32(12, 95, 145, 255);
        private static readonly Color BlueBottom = new Color32(2, 38, 69, 255);
        private static readonly Color Border = new Color32(65, 82, 87, 255);
        private static readonly Color Text = new Color32(241, 244, 238, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/Rebuild ARIA Tutorial Briefing")]
        [MenuItem("Game/UI/V3/Build Tutorial Presentation V3")]
        public static void Build()
        {
            boldFont = RequireAsset<TMP_FontAsset>(BoldFontPath);
            mediumFont = RequireAsset<TMP_FontAsset>(MediumFontPath);
            TMP_FontAsset persianFont = RequireAsset<TMP_FontAsset>(PersianFontPath);
            Sprite portrait = RequireAsset<Sprite>(PortraitPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new InvalidOperationException($"Missing ARIA popup prefab at {PrefabPath}.");

            try
            {
                Transform existing = root.transform.Find("TutorialBriefingSurface");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                RectTransform surface = CreateTopLeft(
                    "TutorialBriefingSurface", root.transform, 0f, 0f, Reference.x, Reference.y);
                AriaTutorialBriefingView view = surface.gameObject.AddComponent<AriaTutorialBriefingView>();
                surface.gameObject.AddComponent<AriaTutorialHudVariantLayoutView>();

                RectTransform panel = CreatePanel(
                    "BriefingPanel", surface, 1135f, 16f, 521f, 534f,
                    DarkTop, DarkBottom, Cyan, 3f, true);

                CreateText(
                    "AriaIdentity", panel, 23f, 17f, 116f, 42f, "ARIA", 31f,
                    Cyan, TextAlignmentOptions.MidlineLeft, true);
                TMP_Text progress = CreateText(
                    "TutorialProgress", panel, 337f, 16f, 160f, 32f, "TUTORIAL 1 / 5", 17f,
                    Cyan, TextAlignmentOptions.MidlineRight, false);

                RectTransform portraitClip = CreateTopLeft("PortraitClip", panel, 164f, 31f, 250f, 241f);
                portraitClip.gameObject.AddComponent<RectMask2D>();
                Image portraitImage = CreateImage("AriaPortrait", portraitClip, portrait, Color.white, false);
                Stretch(portraitImage.rectTransform);
                portraitImage.preserveAspect = false;
                AspectRatioFitter portraitFitter = portraitImage.gameObject.AddComponent<AspectRatioFitter>();
                portraitFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                portraitFitter.aspectRatio = portrait.rect.width / portrait.rect.height;
                V3GradientGraphic portraitFade = CreateGradient(
                    "PortraitFade", portraitClip,
                    new Color(0f, 0f, 0f, 0f), new Color(0f, 0.03f, 0.05f, .88f),
                    Color.clear, 0f);
                Stretch(portraitFade.rectTransform);

                CreateTechDecoration(panel, 24f, 80f, false);
                CreateTechDecoration(panel, 421f, 79f, true);
                CreateReticle(panel, 459f, 214f);
                CreateSolid("ContentDivider", panel, 22f, 275f, 477f, 2f,
                    new Color(Cyan.r, Cyan.g, Cyan.b, .35f));

                TMP_Text title = CreateText(
                    "TutorialTitle", panel, 23f, 289f, 476f, 47f,
                    "SELECT THE RIFLE SQUAD", 29f, Cyan,
                    TextAlignmentOptions.MidlineLeft, true);
                title.enableAutoSizing = true;
                title.fontSizeMin = 23f;
                title.fontSizeMax = 29f;

                TMP_Text body = CreateText(
                    "TutorialBody", panel, 23f, 337f, 476f, 93f,
                    "Tap the <color=#00D1F3>Rifle Squad</color> unit card to select.\n" +
                    "Then tap <color=#00D1F3>MOVE</color> to send them to the marker.",
                    21f, Text, TextAlignmentOptions.TopLeft, false, false);
                body.enableAutoSizing = true;
                body.fontSizeMin = 17f;
                body.fontSizeMax = 21f;
                body.richText = true;

                Button doIt = CreateButton(
                    "TutorialDoItButton", panel, 23f, 439f, 193f, 75f,
                    GreenTop, GreenBottom, Green, out TMP_Text doItLabel, "DO IT", 27f);
                Button showMe = CreateButton(
                    "TutorialShowMeButton", panel, 230f, 439f, 178f, 75f,
                    BlueTop, BlueBottom, Blue, out TMP_Text showMeLabel, "SHOW ME", 24f);
                Button close = CreateButton(
                    "TutorialCloseButton", panel, 422f, 439f, 78f, 75f,
                    RaisedTop, RaisedBottom, Border, out _, "SKIP", 20f);
                progress.rectTransform.SetAsLastSibling();

                RectTransform guide = CreateTopLeft("FirstStepGuide", surface, 0f, 0f, Reference.x, Reference.y);
                RectTransform panelStem = CreateGuideLine("PanelStem", guide, 1423f, 550f, 3f, 18f);
                RectTransform panelBridge = CreateGuideLine("PanelBridge", guide, 1135f, 567f, 291f, 3f);
                RectTransform panelDrop = CreateGuideLine("PanelDrop", guide, 1135f, 567f, 3f, 91f);
                RectTransform commandBridge = CreateGuideLine("CommandBridge", guide, 778f, 655f, 360f, 3f);
                CreateGuideLine("CommandDrop", guide, 778f, 655f, 3f, 115f);
                CreateGuideLine("UnitCardDrop", guide, 27f, 699f, 3f, 37f);
                CreateStepBadge(guide, "UnitCardStepBadge", 7f, 658f, "1");
                CreateStepBadge(guide, "MoveStepBadge", 706f, 670f, "2");

                MainMenuV3SectionLayoutView sectionLayout =
                    surface.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                sectionLayout.Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    new[] { panel, panelStem, panelBridge, panelDrop },
                    true,
                    Array.Empty<RectTransform>(),
                    new[] { commandBridge });

                SerializedObject serialized = new(view);
                SetObject(serialized, "briefingLayout", panel);
                SetObject(serialized, "portraitImage", portraitImage);
                SetObject(serialized, "titleText", title);
                SetObject(serialized, "bodyText", body);
                SetObject(serialized, "progressText", progress);
                SetObject(serialized, "closeButton", close);
                SetObject(serialized, "showMeButton", showMe);
                SetObject(serialized, "doItButton", doIt);
                SetObject(serialized, "showMeButtonLabel", showMeLabel);
                SetObject(serialized, "doItButtonLabel", doItLabel);
                SetObject(serialized, "firstStepGuideRoot", guide);
                SetObject(serialized, "persianFont", persianFont);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                surface.gameObject.SetActive(false);
                surface.SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[AriaTutorialBriefingPrefabBuilder] result=Passed layout=top-right " +
                      "portrait=aria-v3 gradients=procedural borders=3 actions=3 guide=nonblocking");
        }

        public static void BuildAndValidate()
        {
            Build();
            Validate();
            Debug.Log("[AriaTutorialBriefingV3Validation] result=Passed");
        }

        [MenuItem("Game/UI/Capture ARIA Tutorial Briefing")]
        public static void CapturePreview()
        {
            MatchHudV3PrefabBuilder.CaptureTutorialPresentationReview();
        }

        public static void Validate()
        {
            GameObject prefab = RequireAsset<GameObject>(PrefabPath);
            AriaTutorialBriefingView view = prefab.GetComponentInChildren<AriaTutorialBriefingView>(true);
            if (view == null || !view.TryBindHierarchy())
                throw new InvalidOperationException("ARIA tutorial briefing hierarchy is incomplete.");
            if (AssetDatabase.GetAssetPath(view.PortraitImage.sprite) != PortraitPath)
                throw new InvalidOperationException("ARIA tutorial briefing must use the V3 ARIA portrait.");
            if (view.PortraitImage.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("ARIA V3 portrait must preserve its aspect ratio inside a crop.");
            if (view.transform.Find("TutorialInputBlocker") != null)
                throw new InvalidOperationException("ARIA tutorial briefing must not cover the battlefield with an input overlay.");
            if (view.BriefingLayout.anchorMin != new Vector2(0f, 1f) ||
                view.BriefingLayout.anchorMax != new Vector2(0f, 1f) ||
                view.BriefingLayout.anchoredPosition.x < 1100f ||
                view.BriefingLayout.rect.width < 500f)
            {
                throw new InvalidOperationException("ARIA tutorial briefing must remain in the V3 top-right panel position.");
            }
            if (!Contains(view.BriefingLayout, view.ShowMeButton.transform as RectTransform) ||
                !Contains(view.BriefingLayout, view.DoItButton.transform as RectTransform) ||
                !Contains(view.BriefingLayout, view.CloseButton.transform as RectTransform))
            {
                throw new InvalidOperationException("ARIA tutorial actions must remain inside the briefing panel.");
            }
            if ((view.ShowMeButton.transform as RectTransform).rect.height < 72f ||
                (view.DoItButton.transform as RectTransform).rect.height < 72f ||
                (view.CloseButton.transform as RectTransform).rect.height < 72f)
            {
                throw new InvalidOperationException("ARIA tutorial actions must retain mobile touch targets.");
            }

            MainMenuV3SectionLayoutView layout = view.GetComponent<MainMenuV3SectionLayoutView>();
            if (layout == null || layout.ReferenceResolution != Reference ||
                !layout.ExpandToCanvasWidth || layout.RightAnchoredTargets.Length != 4)
            {
                throw new InvalidOperationException("ARIA tutorial V3 responsive layout is incomplete.");
            }
            if (view.GetComponent<AriaTutorialHudVariantLayoutView>() == null)
                throw new InvalidOperationException("ARIA tutorial V3 must apply and restore its compact Match HUD header variant.");
            V3GradientGraphic[] gradients = view.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 7)
                throw new InvalidOperationException($"ARIA tutorial V3 requires procedural gradients; found {gradients.Length}.");
            if (view.FirstStepGuideRoot == null ||
                view.FirstStepGuideRoot.GetComponentsInChildren<Graphic>(true).Length < 7)
            {
                throw new InvalidOperationException("ARIA tutorial V3 first-step guidance overlay is incomplete.");
            }

            Debug.Log($"[AriaTutorialBriefingV3Validation] result=Passed gradients={gradients.Length} borders=3 actions=3");
        }

        public static UiAssistantPanelModel CreateTargetLockPreviewModel()
        {
            return new UiAssistantPanelModel(
                1,
                false,
                0,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantTargetLockModel.Empty,
                UiAssistantNarrationModel.Empty,
                true,
                "Select the Rifle Squad",
                "Tap the <color=#00D1F3>Rifle Squad</color> unit card to select.\n" +
                "Then tap <color=#00D1F3>MOVE</color> to send them to the marker.",
                "HIGH",
                "DO IT",
                true,
                true,
                false,
                false,
                "PLAYER CONTROL",
                string.Empty,
                recommendationKind: 1,
                recommendationTargetKind: 6,
                tutorialStep: 1,
                tutorialStepCount: 5);
        }

        public static UiAssistantPanelModel CreateCommandAssistantPreviewModel()
        {
            return new UiAssistantPanelModel(
                2,
                false,
                0,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                new UiAssistantMessageRowModel(
                    true, 30, "Hostile infantry squad detected near market stalls.",
                    "They are moving between cover positions.", 3, 1, 1, true, false),
                UiAssistantMessageRowModel.Empty,
                new UiAssistantTargetLockModel(
                    true, 2, 1, "ENEMY INFANTRY SQUAD", "RIFLE SQUAD", "140m", "HIGH",
                    "HOSTILE", "READY", "Moving between cover positions."),
                new UiAssistantNarrationModel(
                    (byte)UiAssistantNarrationStateKind.Presented, 3, "ARIA VOICE",
                    "MOVE ORDER CONFIRMED.", string.Empty, true),
                true,
                "TACTICAL REPORTS",
                "Hostile infantry squad detected near market stalls.\nThey are moving between cover positions.",
                "HIGH",
                "SHOW ME",
                true,
                false,
                false,
                false,
                "PLAYER CONTROL",
                string.Empty,
                recommendationKind: 1,
                recommendationTargetKind: 1);
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
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f, true);
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
            label = CreateText("Label", rect, 5f, 4f, width - 10f, height - 8f, value,
                fontSize, Text, TextAlignmentOptions.Center, true);
            return button;
        }

        private static void CreateTechDecoration(Transform parent, float x, float y, bool rightAligned)
        {
            float[] widths = { 58f, 73f, 46f, 68f, 38f, 64f, 51f, 72f };
            for (int i = 0; i < widths.Length; i++)
            {
                float width = widths[i];
                float lineX = rightAligned ? x + 76f - width : x;
                CreateSolid($"TechLine{i:00}", parent, lineX, y + i * 9f, width,
                    i % 3 == 0 ? 2f : 1f,
                    new Color(CyanMuted.r, CyanMuted.g, CyanMuted.b, i % 2 == 0 ? .78f : .48f));
                if (i % 2 == 0)
                    CreateSolid($"TechTick{i:00}", parent,
                        rightAligned ? x + 80f : x - 4f, y + i * 9f, 2f, 5f, CyanMuted);
            }
        }

        private static void CreateReticle(Transform parent, float centerX, float centerY)
        {
            RectTransform ring = CreateTopLeft(
                "TutorialReticle", parent, centerX - 24f, centerY - 24f, 48f, 48f);
            V3RingGraphic ringGraphic = ring.gameObject.AddComponent<V3RingGraphic>();
            ringGraphic.Configure(CyanMuted, 3f, 40);
            ringGraphic.raycastTarget = false;
            CreateSolid("ReticleHorizontal", parent, centerX - 31f, centerY - 1f, 62f, 2f, CyanMuted);
            CreateSolid("ReticleVertical", parent, centerX - 1f, centerY - 31f, 2f, 62f, CyanMuted);
            RectTransform dot = CreatePanel(
                "ReticleDot", parent, centerX - 4f, centerY - 4f, 8f, 8f,
                Cyan, Cyan, Color.clear, 0f);
            dot.GetComponent<V3GradientGraphic>().raycastTarget = false;
        }

        private static RectTransform CreateGuideLine(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform line = CreateTopLeft(name, parent, x, y, width, height);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = Cyan;
            image.raycastTarget = false;
            return line;
        }

        private static void CreateStepBadge(Transform parent, string name, float x, float y, string value)
        {
            RectTransform badge = CreatePanel(
                name, parent, x, y, 40f, 43f,
                new Color32(24, 228, 249, 255), new Color32(0, 132, 170, 255), Cyan, 3f);
            CreateText("Label", badge, 2f, 0f, 36f, 41f, value, 28f, Text,
                TextAlignmentOptions.Center, true);
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

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(typeof(AriaTutorialBriefingView).Name, propertyName);
            property.objectReferenceValue = value;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing ARIA tutorial V3 asset at {path}.");
            return asset;
        }

        private static bool Contains(RectTransform parent, RectTransform child)
        {
            if (parent == null || child == null)
                return false;
            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect rect = parent.rect;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = parent.InverseTransformPoint(corners[i]);
                if (!rect.Contains(local))
                    return false;
            }
            return true;
        }
    }
}
#endif
