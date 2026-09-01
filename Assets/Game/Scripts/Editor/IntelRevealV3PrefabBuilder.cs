#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class IntelRevealV3PrefabBuilder
    {
        internal const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab";
        private const string OperationsPrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN11_OperationsDashboardContent.prefab";
        private const string EvidenceAtlasPath =
            "Assets/Game/Art/UI/V3Shared/IntelReveal/POP08_EvidenceAtlas_V3.png";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color FrameTop = new Color32(27, 36, 41, 252);
        private static readonly Color FrameBottom = new Color32(2, 8, 11, 255);
        private static readonly Color CardTop = new Color32(23, 31, 35, 255);
        private static readonly Color CardBottom = new Color32(2, 7, 9, 255);
        private static readonly Color RaisedTop = new Color32(56, 63, 66, 255);
        private static readonly Color RaisedBottom = new Color32(20, 24, 26, 255);
        private static readonly Color Line = new Color32(116, 130, 133, 255);
        private static readonly Color White = new Color32(239, 242, 238, 255);
        private static readonly Color Cyan = new Color32(21, 184, 236, 255);
        private static readonly Color CyanDark = new Color32(0, 73, 119, 255);
        private static readonly Color Lime = new Color32(138, 207, 45, 255);
        private static readonly Color Orange = new Color32(255, 126, 10, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static Texture2D evidenceAtlas;

        [MenuItem("Game/UI/V3/Rebuild POP-08 Intel Reveal")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            AssetDatabase.Refresh();
            LoadAssets();

            GameObject root = new("IntelRevealPopup", typeof(RectTransform));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                UIPopupFrameView popup = root.AddComponent<UIPopupFrameView>();
                IntelRevealV3PopupView state = root.AddComponent<IntelRevealV3PopupView>();

                Image scrim = CreateSolid("Scrim", root.transform, new Color(0f, .015f, .025f, .66f), true);
                Stretch(scrim.rectTransform);

                RectTransform composition = CreateTopLeft(
                    "V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                RectTransform shadow = CreatePanel(
                    "FrameShadow", composition, 278f, 107f, 1116f, 770f,
                    new Color(0f, 0f, 0f, .6f), new Color(0f, 0f, 0f, .86f), Color.clear, 0f);
                shadow.gameObject.GetComponent<V3GradientGraphic>().raycastTarget = false;

                FrameBindings bindings = BuildFrame(composition);
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    shouldExpandToCanvasWidth: true,
                    targetsAnchoredToCenter: new[] { shadow, bindings.Frame },
                    targetsExpandedAcrossWidth: new[] { scrim.rectTransform });

                popup.Configure(
                    scrim.gameObject,
                    bindings.Frame.gameObject,
                    bindings.Header.gameObject,
                    bindings.Title,
                    bindings.HeaderClose,
                    bindings.Body,
                    bindings.ButtonRow);
                state.Configure(bindings.FooterClose, bindings.ViewIntel, bindings.EvidenceButtons);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[IntelRevealV3PrefabBuilder] result=Passed layout=target gradients=procedural borders=3 evidence=single-atlas aspect-preserved actions=close-view-inspect");
        }

        [MenuItem("Game/UI/V3/Capture POP-08 Intel Reveal Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-intel-reveal-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-intel-reveal-v3-20x9.png", 4800, 2160);
        }

        [MenuItem("Game/UI/V3/Validate POP-08 Intel Reveal")]
        public static void Validate()
        {
            if (evidenceAtlas == null)
                LoadAssets();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<UIPopupFrameView>() == null ||
                prefab.GetComponent<IntelRevealV3PopupView>() == null)
                throw new MissingReferenceException("POP-08 runtime popup bindings are incomplete.");

            MainMenuV3SectionLayoutView layout =
                prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference || !layout.ExpandToCanvasWidth)
                throw new InvalidOperationException("POP-08 must use the responsive 1672x941 composition.");

            RawImage[] evidence = Find(prefab.transform, "EvidenceCards")?
                .GetComponentsInChildren<RawImage>(true) ?? Array.Empty<RawImage>();
            if (evidence.Length != 3)
                throw new InvalidOperationException($"POP-08 requires exactly three evidence atlas views; found {evidence.Length}.");
            for (int index = 0; index < evidence.Length; index++)
            {
                if (evidence[index].texture != evidenceAtlas ||
                    evidence[index].GetComponent<AspectRatioFitter>() == null)
                    throw new InvalidOperationException("POP-08 evidence must reuse one aspect-preserved atlas texture.");
            }

            if (Find(prefab.transform, "HeaderCloseButton")?.GetComponent<Button>() == null ||
                Find(prefab.transform, "FooterCloseButton")?.GetComponent<Button>() == null ||
                Find(prefab.transform, "ViewIntelButton")?.GetComponent<Button>() == null)
                throw new MissingReferenceException("POP-08 close/view actions are missing.");

            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 18)
                throw new InvalidOperationException($"POP-08 requires layered procedural gradients; found {gradients}.");

            foreach (V3GradientGraphic gradient in prefab.GetComponentsInChildren<V3GradientGraphic>(true))
            {
                SerializedObject serialized = new(gradient);
                float width = serialized.FindProperty("borderWidth").floatValue;
                if (width > 0f && !Mathf.Approximately(width, 3f))
                    throw new InvalidOperationException($"POP-08 border on {gradient.name} is {width}px; every visible border must be 3px.");
            }

            Debug.Log($"[IntelRevealV3PrefabBuilder] validation=Passed gradients={gradients} evidence=3 atlas=1 borders=3 runtime=bound");
        }

        private static FrameBindings BuildFrame(Transform parent)
        {
            RectTransform frame = CreatePanel(
                "Frame", parent, 286f, 114f, 1100f, 756f,
                FrameTop, FrameBottom, Line, 3f);
            RectTransform header = CreateTopLeft("Header", frame, 3f, 3f, 1094f, 109f);
            CreatePanel("HeaderFill", header, 0f, 0f, 1094f, 106f,
                new Color32(34, 43, 48, 255), new Color32(7, 13, 16, 255), Color.clear, 0f);

            Image document = CreateImage(
                "IntelDocumentIcon", header,
                RequireSprite(V3UiFoundationBuilder.OperationsIntelIconPath), Cyan);
            SetTopLeft(document.rectTransform, 191f, 23f, 58f, 58f);
            Image inspect = CreateImage(
                "IntelInspectIcon", header,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), Cyan);
            SetTopLeft(inspect.rectTransform, 222f, 52f, 37f, 37f);

            TMP_Text title = CreateText(
                header, "TitleText", 279f, 8f, 620f, 89f,
                "INTEL REVEALED", 52f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            Button headerClose = BuildTextButton(
                header, "HeaderCloseButton", 991f, 23f, 68f, 68f,
                RaisedTop, RaisedBottom, Line, "X", 48f, White);
            CreateSolid("HeaderDivider", header, 22f, 106f, 1050f, 3f, Line);

            RectTransform body = CreateTopLeft("BodyRoot", frame, 0f, 0f, 1100f, 756f);
            CreateSolid("SubheadingLeftRule", body, 327f, 136f, 62f, 3f, Line);
            CreateText(body, "SubheadingText", 410f, 118f, 280f, 43f,
                "EVIDENCE COLLECTED", 26f, White, TextAlignmentOptions.Center, true);
            CreateSolid("SubheadingRightRule", body, 711f, 136f, 62f, 3f, Line);

            RectTransform evidenceRoot = CreateTopLeft("EvidenceCards", body, 32f, 163f, 1036f, 388f);
            Button[] evidenceButtons =
            {
                BuildEvidenceCard(
                    evidenceRoot, "SupplyLedgerCard", 0f, "SUPPLY LEDGER",
                    new Rect(16f / 1024f, 8f / 288f, 310f / 1024f, 272f / 288f),
                    310f / 272f, "CONFIDENCE: HIGH", Lime),
                BuildEvidenceCard(
                    evidenceRoot, "CargoManifestCard", 355f, "CARGO MANIFEST",
                    new Rect(342f / 1024f, 8f / 288f, 326f / 1024f, 272f / 288f),
                    326f / 272f, "CONFIDENCE: MEDIUM", Orange),
                BuildEvidenceCard(
                    evidenceRoot, "RadioInterceptCard", 710f, "RADIO INTERCEPT",
                    new Rect(684f / 1024f, 8f / 288f, 315f / 1024f, 272f / 288f),
                    315f / 272f, "CONFIDENCE: HIGH", Lime)
            };

            BuildEvidenceProgress(body);
            RectTransform buttonRow = CreateTopLeft("ButtonRow", frame, 32f, 641f, 1036f, 91f);
            Button footerClose = BuildTextButton(
                buttonRow, "FooterCloseButton", 0f, 0f, 457f, 89f,
                RaisedTop, RaisedBottom, Line, "CLOSE", 33f, White);
            Button viewIntel = BuildTextButtonCorners(
                buttonRow, "ViewIntelButton", 497f, 0f, 539f, 89f,
                new Color32(29, 188, 239, 255), new Color32(12, 145, 218, 255),
                new Color32(5, 116, 182, 255), new Color32(0, 77, 128, 255),
                Cyan, "VIEW INTEL", 34f, new Color32(2, 8, 11, 255));

            return new FrameBindings(
                frame, header, body, buttonRow, title,
                headerClose, footerClose, viewIntel, evidenceButtons);
        }

        private static Button BuildEvidenceCard(
            Transform parent,
            string name,
            float x,
            string title,
            Rect uv,
            float aspect,
            string confidence,
            Color accent)
        {
            RectTransform card = CreatePanel(
                name, parent, x, 0f, 326f, 388f,
                CardTop, CardBottom, Line, 3f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<V3GradientGraphic>();
            card.GetComponent<V3GradientGraphic>().raycastTarget = true;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.86f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.85f, 0.94f, 1f);
            button.colors = colors;

            CreateText(card, "TitleText", 3f, 3f, 320f, 48f,
                title, 24f, White, TextAlignmentOptions.Center, true);
            CreateSolid("TitleDivider", card, 3f, 50f, 320f, 3f, Line);

            RectTransform artClip = CreateTopLeft("EvidenceViewport", card, 3f, 53f, 320f, 276f);
            artClip.gameObject.AddComponent<RectMask2D>();
            RawImage art = CreateRawImage("EvidenceImage", artClip, evidenceAtlas, Color.white);
            Stretch(art.rectTransform);
            art.uvRect = uv;
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = aspect;

            CreateSolid("ConfidenceDivider", card, 3f, 329f, 320f, 3f, Line);
            Image reticle = CreateImage(
                "ConfidenceIcon", card,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), accent);
            SetTopLeft(reticle.rectTransform, 13f, 341f, 39f, 39f);
            CreateText(card, "ConfidenceText", 59f, 337f, 254f, 46f,
                confidence, 19f, accent, TextAlignmentOptions.MidlineLeft, true);
            return button;
        }

        private static void BuildEvidenceProgress(Transform parent)
        {
            RectTransform progress = CreatePanel(
                "EvidenceProgress", parent, 32f, 560f, 1036f, 63f,
                new Color32(10, 27, 34, 255), new Color32(2, 10, 14, 255), Line, 3f);
            Image radio = CreateImage(
                "EvidenceRadioIcon", progress,
                RequireSprite(V3UiFoundationBuilder.MatchScanIconPath), Cyan);
            SetTopLeft(radio.rectTransform, 16f, 10f, 43f, 43f);
            CreateText(progress, "ProgressLabel", 76f, 6f, 248f, 51f,
                "EVIDENCE COLLECTED", 22f, White, TextAlignmentOptions.MidlineLeft, true);

            const int count = 8;
            const float gap = 6f;
            const float segmentWidth = 67f;
            for (int index = 0; index < count; index++)
            {
                CreatePanel(
                    $"Segment_{index + 1}", progress,
                    330f + index * (segmentWidth + gap), 22f, segmentWidth, 18f,
                    new Color32(21, 184, 236, 255), new Color32(0, 105, 170, 255),
                    Color.clear, 0f);
            }
            CreateText(progress, "ProgressValue", 926f, 5f, 85f, 52f,
                "3 / 3", 24f, Cyan, TextAlignmentOptions.Center, true);
        }

        private static Button BuildTextButton(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color top,
            Color bottom,
            Color border,
            string label,
            float fontSize,
            Color textColor)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            return FinishButton(rect, label, fontSize, textColor);
        }

        private static Button BuildTextButtonCorners(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color topLeft,
            Color topRight,
            Color bottomLeft,
            Color bottomRight,
            Color border,
            string label,
            float fontSize,
            Color textColor)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.ConfigureCorners(topLeft, topRight, bottomLeft, bottomRight, border, 3f);
            return FinishButton(rect, label, fontSize, textColor);
        }

        private static Button FinishButton(
            RectTransform rect, string label, float fontSize, Color textColor)
        {
            V3GradientGraphic gradient = rect.GetComponent<V3GradientGraphic>();
            gradient.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = gradient;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.86f, .98f, 1f, 1f);
            colors.pressedColor = new Color(.62f, .82f, .92f, 1f);
            button.colors = colors;
            CreateText(rect, "LabelText", 12f, 3f, rect.rect.width - 24f, rect.rect.height - 6f,
                label, fontSize, textColor, TextAlignmentOptions.Center, true);
            return button;
        }

        private static RectTransform CreatePanel(
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
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontWeight = bold ? FontWeight.Bold : FontWeight.Regular;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, size * .72f);
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(
            string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(
            string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateSolid(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            Image image = CreateSolid(name, parent, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = V3UiPrefabFactory.CreateRect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(width, height),
                new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static void SetTopLeft(
            RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = Find(root.GetChild(index), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing POP-08 V3 sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            evidenceAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(EvidenceAtlasPath);
            if (boldFont == null || mediumFont == null || evidenceAtlas == null)
                throw new FileNotFoundException("POP-08 V3 fonts or evidence atlas are missing.");
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject operationsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OperationsPrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("IntelRevealV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new(
                "IntelRevealV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            scaler.matchWidthOrHeight = .5f;

            if (operationsPrefab != null)
            {
                GameObject background = UnityEngine.Object.Instantiate(operationsPrefab, canvasRect);
                Stretch(background.transform as RectTransform);
            }
            GameObject popup = UnityEngine.Object.Instantiate(popupPrefab, canvasRect);
            Stretch(popup.transform as RectTransform);
            Canvas.ForceUpdateCanvases();
            foreach (MainMenuV3SectionLayoutView layout in
                     canvasObject.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
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
                Debug.Log($"[IntelRevealV3PrefabBuilder] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
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

        private readonly struct FrameBindings
        {
            public FrameBindings(
                RectTransform frame,
                RectTransform header,
                RectTransform body,
                RectTransform buttonRow,
                TMP_Text title,
                Button headerClose,
                Button footerClose,
                Button viewIntel,
                Button[] evidenceButtons)
            {
                Frame = frame;
                Header = header;
                Body = body;
                ButtonRow = buttonRow;
                Title = title;
                HeaderClose = headerClose;
                FooterClose = footerClose;
                ViewIntel = viewIntel;
                EvidenceButtons = evidenceButtons;
            }

            public RectTransform Frame { get; }
            public RectTransform Header { get; }
            public RectTransform Body { get; }
            public RectTransform ButtonRow { get; }
            public TMP_Text Title { get; }
            public Button HeaderClose { get; }
            public Button FooterClose { get; }
            public Button ViewIntel { get; }
            public Button[] EvidenceButtons { get; }
        }
    }
}
#endif
