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
    public static class AriaTutorialBriefingPrefabBuilder
    {
        public const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab";
        public const string PortraitPath =
            "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_aria.png";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string PanelPath = "Assets/Game/Art/UI/Panels/scn09_panel_detail_tall_frame.png";
        private const string SecondaryButtonPath = "Assets/Game/Art/UI/Panels/scn09_panel_secondary_button_bg.png";
        private const string PrimaryButtonPath = "Assets/Game/Art/UI/Panels/scn09_panel_gold_action_button_bg.png";
        private const string CloseButtonPath = "Assets/Game/Art/UI/Panels/scn09_panel_close_button_bg.png";
        private const string CloseIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_close_x.png";
        private const string FocusIconPath = "Assets/Game/Art/UI/Icons/scn08_minimap_focus_target_icon.png";
        private const string ConfirmIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_check_confirm.png";

        private static readonly Color Cyan = new(0.19f, 0.91f, 0.96f, 1f);
        private static readonly Color Gold = new(0.91f, 0.72f, 0.31f, 1f);
        private static readonly Color Pale = new(0.95f, 0.92f, 0.82f, 1f);
        private static readonly Color Muted = new(0.64f, 0.68f, 0.66f, 1f);

        [MenuItem("Game/UI/Rebuild ARIA Tutorial Briefing")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new InvalidOperationException($"Missing ARIA popup prefab at {PrefabPath}.");

            try
            {
                Transform existing = root.transform.Find("TutorialBriefingSurface");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                TMP_FontAsset bold = RequireAsset<TMP_FontAsset>(BoldFontPath);
                TMP_FontAsset medium = RequireAsset<TMP_FontAsset>(MediumFontPath);
                Sprite portrait = RequireAsset<Sprite>(PortraitPath);
                Sprite panel = RequireAsset<Sprite>(PanelPath);
                Sprite secondaryButton = RequireAsset<Sprite>(SecondaryButtonPath);
                Sprite primaryButton = RequireAsset<Sprite>(PrimaryButtonPath);
                Sprite closeButtonFrame = RequireAsset<Sprite>(CloseButtonPath);
                Sprite closeIcon = RequireAsset<Sprite>(CloseIconPath);
                Sprite focusIcon = RequireAsset<Sprite>(FocusIconPath);
                Sprite confirmIcon = RequireAsset<Sprite>(ConfirmIconPath);

                RectTransform surface = CreateRect("TutorialBriefingSurface", root.transform);
                Stretch(surface);
                AriaTutorialBriefingView view = surface.gameObject.AddComponent<AriaTutorialBriefingView>();

                Image portraitImage = CreateImage("AriaPortrait", surface, portrait, Color.white, false);
                SetRect(
                    portraitImage.rectTransform,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(960f, 960f),
                    new Vector2(540f, 1100f));
                portraitImage.preserveAspect = true;

                Image backing = CreateImage("BriefingPanel", surface, panel, Color.white, false);
                SetRect(
                    backing.rectTransform,
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(1800f, 760f),
                    new Vector2(1650f, 1000f));
                backing.type = Image.Type.Sliced;

                TMP_Text identity = CreateText(
                    "AriaIdentity",
                    backing.transform,
                    "ARIA",
                    bold,
                    40f,
                    TextAlignmentOptions.Left,
                    Cyan);
                SetAnchored(identity.rectTransform, new Vector2(0.065f, 0.845f), new Vector2(0.28f, 0.94f));

                TMP_Text role = CreateText(
                    "AriaRole",
                    backing.transform,
                    "TACTICAL ADVISOR",
                    medium,
                    28f,
                    TextAlignmentOptions.Left,
                    Muted);
                SetAnchored(role.rectTransform, new Vector2(0.245f, 0.845f), new Vector2(0.62f, 0.94f));

                TMP_Text title = CreateText(
                    "TutorialTitle",
                    backing.transform,
                    "FIND YOUR SQUAD",
                    bold,
                    58f,
                    TextAlignmentOptions.Left,
                    Pale);
                SetAnchored(title.rectTransform, new Vector2(0.065f, 0.64f), new Vector2(0.92f, 0.84f));
                title.enableAutoSizing = true;
                title.fontSizeMin = 42f;
                title.fontSizeMax = 58f;
                title.textWrappingMode = TextWrappingModes.NoWrap;

                Image divider = CreateImage("TutorialDivider", backing.transform, null, new Color(Cyan.r, Cyan.g, Cyan.b, 0.68f), false);
                SetAnchored(divider.rectTransform, new Vector2(0.065f, 0.615f), new Vector2(0.935f, 0.621f));

                TMP_Text body = CreateText(
                    "TutorialBody",
                    backing.transform,
                    "Select the command squad to begin.",
                    medium,
                    38f,
                    TextAlignmentOptions.TopLeft,
                    Pale);
                SetAnchored(body.rectTransform, new Vector2(0.065f, 0.285f), new Vector2(0.935f, 0.58f));
                body.enableAutoSizing = true;
                body.fontSizeMin = 30f;
                body.fontSizeMax = 38f;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Ellipsis;

                TMP_Text progress = CreateText(
                    "TutorialProgress",
                    backing.transform,
                    "TRAINING 1 / 5",
                    bold,
                    30f,
                    TextAlignmentOptions.Left,
                    Gold);
                SetAnchored(progress.rectTransform, new Vector2(0.065f, 0.075f), new Vector2(0.39f, 0.24f));

                Button showMe = CreateActionButton(
                    "TutorialShowMeButton",
                    backing.transform,
                    secondaryButton,
                    focusIcon,
                    bold,
                    "SHOW ME",
                    new Vector2(0.505f, 0.065f),
                    new Vector2(0.715f, 0.245f),
                    out TMP_Text showMeLabel);
                Button doIt = CreateActionButton(
                    "TutorialDoItButton",
                    backing.transform,
                    primaryButton,
                    confirmIcon,
                    bold,
                    "DO IT",
                    new Vector2(0.735f, 0.065f),
                    new Vector2(0.945f, 0.245f),
                    out TMP_Text doItLabel);

                Button close = CreateIconButton(
                    "TutorialCloseButton",
                    backing.transform,
                    closeButtonFrame,
                    closeIcon,
                    new Vector2(0.945f, 0.84f),
                    new Vector2(0.988f, 0.955f));

                SetObject(view, "briefingLayout", backing.rectTransform);
                SetObject(view, "portraitImage", portraitImage);
                SetObject(view, "titleText", title);
                SetObject(view, "bodyText", body);
                SetObject(view, "progressText", progress);
                SetObject(view, "closeButton", close);
                SetObject(view, "showMeButton", showMe);
                SetObject(view, "doItButton", doIt);
                SetObject(view, "showMeButtonLabel", showMeLabel);
                SetObject(view, "doItButtonLabel", doItLabel);

                surface.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AriaTutorialBriefingPrefabBuilder] Built {PrefabPath}");
        }

        public static void BuildAndValidate()
        {
            Build();
            Validate();
            Debug.Log("[AriaTutorialBriefingPrefabBuilder] result=Passed");
        }

        [MenuItem("Game/UI/Capture ARIA Tutorial Briefing")]
        public static void CapturePreview()
        {
            const int width = 2400;
            const int height = 1080;
            const string outputPath = "/private/tmp/warline-aria-tutorial-briefing.png";
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            RenderTexture target = null;
            Texture2D readback = null;
            try
            {
                cameraObject = new GameObject("AriaTutorialCaptureCamera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.19f, 0.22f, 0.21f, 1f);
                camera.orthographic = true;

                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                target.Create();
                camera.targetTexture = target;

                canvasObject = new GameObject(
                    "AriaTutorialCaptureCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(4800f, 2160f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Image backdrop = CreateImage(
                    "MapBackdrop",
                    canvasObject.transform,
                    null,
                    new Color(0.49f, 0.45f, 0.34f, 1f),
                    false);
                Stretch(backdrop.rectTransform);

                GameObject prefab = RequireAsset<GameObject>(PrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Failed to instantiate ARIA tutorial preview.");
                instance.SetActive(true);
                AriaCommandAssistantPopupView popup = instance.GetComponent<AriaCommandAssistantPopupView>();
                if (popup == null || !popup.TryBindHierarchy())
                    throw new InvalidOperationException("ARIA tutorial preview hierarchy did not bind.");

                popup.ApplyRecommendation(CreatePreviewModel());
                popup.ApplyAccessibility(false, false);
                popup.Show();
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readback.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readback.Apply(false, false);
                File.WriteAllBytes(outputPath, readback.EncodeToPNG());
                RenderTexture.active = previous;
                Debug.Log($"[AriaTutorialBriefingPrefabBuilder] capture={outputPath}");
            }
            finally
            {
                if (readback != null)
                    Object.DestroyImmediate(readback);
                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }
                if (canvasObject != null)
                    Object.DestroyImmediate(canvasObject);
                if (cameraObject != null)
                    Object.DestroyImmediate(cameraObject);
            }
        }

        public static void Validate()
        {
            GameObject prefab = RequireAsset<GameObject>(PrefabPath);
            AriaTutorialBriefingView view = prefab.GetComponentInChildren<AriaTutorialBriefingView>(true);
            if (view == null || !view.TryBindHierarchy())
                throw new InvalidOperationException("ARIA tutorial briefing hierarchy is incomplete.");
            if (AssetDatabase.GetAssetPath(view.PortraitImage.sprite) != PortraitPath)
                throw new InvalidOperationException("ARIA tutorial briefing must use the canonical ARIA portrait.");
            if (view.transform.Find("TutorialInputBlocker") != null)
                throw new InvalidOperationException("ARIA tutorial briefing must not cover the battlefield with an input overlay.");
            if (view.BriefingLayout.anchorMin != Vector2.zero || view.BriefingLayout.anchorMax != Vector2.zero ||
                view.BriefingLayout.anchoredPosition.y - view.BriefingLayout.rect.height * 0.5f < 620f)
                throw new InvalidOperationException("ARIA tutorial briefing must remain above the lower-left squad controls.");
            if (!Contains(view.BriefingLayout, view.ShowMeButton.transform as RectTransform) ||
                !Contains(view.BriefingLayout, view.DoItButton.transform as RectTransform))
                throw new InvalidOperationException("ARIA tutorial actions must remain inside the briefing panel.");
            if ((view.ShowMeButton.transform as RectTransform).rect.height < 110f ||
                (view.DoItButton.transform as RectTransform).rect.height < 110f)
                throw new InvalidOperationException("ARIA tutorial actions must retain mobile touch targets.");
        }

        private static UiAssistantPanelModel CreatePreviewModel()
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
                "Find your squad",
                "Select the command squad to begin. I will mark the exact unit if you need guidance.",
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

        private static Button CreateActionButton(
            string name,
            Transform parent,
            Sprite frame,
            Sprite iconSprite,
            TMP_FontAsset font,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out TMP_Text labelText)
        {
            Image image = CreateImage(name, parent, frame, Color.white, true);
            SetAnchored(image.rectTransform, anchorMin, anchorMax);
            image.type = Image.Type.Sliced;
            Button button = image.gameObject.AddComponent<Button>();

            Image icon = CreateImage("Icon", image.transform, iconSprite, Color.white, false);
            SetAnchored(icon.rectTransform, new Vector2(0.09f, 0.2f), new Vector2(0.29f, 0.8f));
            icon.preserveAspect = true;

            labelText = CreateText("Label", image.transform, label, font, 32f, TextAlignmentOptions.Center, Pale);
            SetAnchored(labelText.rectTransform, new Vector2(0.25f, 0.08f), new Vector2(0.96f, 0.92f));
            return button;
        }

        private static Button CreateIconButton(
            string name,
            Transform parent,
            Sprite frame,
            Sprite iconSprite,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Image image = CreateImage(name, parent, frame, Color.white, true);
            SetAnchored(image.rectTransform, anchorMin, anchorMax);
            image.type = Image.Type.Sliced;
            Button button = image.gameObject.AddComponent<Button>();
            Image icon = CreateImage("Icon", image.transform, iconSprite, Color.white, false);
            SetAnchored(icon.rectTransform, new Vector2(0.24f, 0.24f), new Vector2(0.76f, 0.76f));
            icon.preserveAspect = true;
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject value = new(name, typeof(RectTransform)) { layer = 5 };
            RectTransform rect = value.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float size,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property {target.GetType().Name}.{propertyName}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing required asset at {path}.");
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
