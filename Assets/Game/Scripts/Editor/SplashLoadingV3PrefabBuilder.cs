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
    public static class SplashLoadingV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab";
        private const string BackgroundPath = "Assets/Game/Art/UI/V3Shared/Backgrounds/SCN01_LoadingEnvironment_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string LightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";
        private static readonly Vector2 ReferenceResolution = new Vector2(1672f, 941f);
        private static readonly Color ChromeBorder = new Color32(70, 82, 86, 255);
        private static readonly Color GraphiteTop = new Color32(19, 31, 35, 248);
        private static readonly Color GraphiteBottom = new Color32(6, 13, 16, 252);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static TMP_FontAsset lightFont;
        private static V3UiTheme theme;
        private static Sprite background;

        [MenuItem("Game/UI/Rebuild Splash Loading V3")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            ConfigureBackgroundImport();
            LoadAssets();

            GameObject root = CreateRect("SCN01_LoadingContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            UIShellLoadingProgressView progressView = root.AddComponent<UIShellLoadingProgressView>();
            BuildScreen(root.transform, progressView);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[SplashLoadingV3PrefabBuilder] result=Passed v3=True prefab rebuilt with one unique background and shared procedural chrome.");
        }

        [MenuItem("Game/UI/V3/Validate Splash Loading")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Splash V3 prefab: {PrefabPath}");

            UIShellLoadingProgressView progressView = prefab.GetComponent<UIShellLoadingProgressView>();
            if (progressView == null)
                throw new MissingComponentException("Splash V3 is missing UIShellLoadingProgressView.");

            SerializedObject serializedView = new SerializedObject(progressView);
            if (serializedView.FindProperty("progressFill")?.objectReferenceValue == null ||
                serializedView.FindProperty("percentText")?.objectReferenceValue == null ||
                serializedView.FindProperty("statusText")?.objectReferenceValue == null)
                throw new MissingReferenceException("Splash V3 loading progress bindings are incomplete.");

            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            int rasterSpriteCount = 0;
            foreach (Image image in images)
            {
                if (image.sprite == null)
                    continue;

                rasterSpriteCount++;
                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                if (!string.Equals(spritePath, BackgroundPath, StringComparison.Ordinal) &&
                    !string.Equals(spritePath, V3UiFoundationBuilder.MainMenuLogoPath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Splash V3 references duplicated or historical raster UI art: {spritePath}");
            }

            if (rasterSpriteCount != 2)
                throw new InvalidOperationException(
                    $"Splash V3 must use one unique background plus the canonical shared brand logo; found {rasterSpriteCount} raster sprites.");
            if (prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length < 8)
                throw new MissingComponentException("Splash V3 must use procedural gradients for its reusable chrome and progress treatment.");
            if (FindDeepChild(prefab.transform, "IntegratedLoadingFooter") == null ||
                FindDeepChild(prefab.transform, "BrandLogoPlate") == null ||
                FindDeepChild(prefab.transform, "AndroidBuildChip") == null ||
                FindDeepChild(prefab.transform, "SecureLinkChip") == null)
                throw new MissingReferenceException("Splash V3 is missing target-lock structural regions.");
            if (FindDeepChild(prefab.transform, "LoadingPanel_Frame") != null ||
                FindDeepChild(prefab.transform, "CornerTL") != null)
                throw new InvalidOperationException("Historical ornate Splash chrome is forbidden in V3.");

            Image backgroundImage = FindDeepChild(prefab.transform, "LoadingEnvironment")?.GetComponent<Image>();
            AspectRatioFitter backgroundFitter = backgroundImage != null
                ? backgroundImage.GetComponent<AspectRatioFitter>()
                : null;
            if (backgroundFitter == null || backgroundFitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new MissingComponentException("Splash V3 background must cover-crop without non-uniform stretching.");
            if (FindDeepChild(prefab.transform, "SplashChromeReference")?.GetComponent<MainMenuV3SectionLayoutView>() == null)
                throw new MissingComponentException("Splash V3 chrome must map its authored reference frame into the live shell canvas.");

            Debug.Log($"[SplashLoadingV3PrefabBuilder] validation=Passed rasterSprites={rasterSpriteCount} gradients={prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length} images={images.Length}");
        }

        [MenuItem("Game/UI/Capture Splash Loading V3 QA")]
        public static void CaptureQa()
        {
            Capture("/private/tmp/warline-splash-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-splash-v3-20x9.png", 2400, 1080);
            Debug.Log("[SplashLoadingV3PrefabBuilder] QA captures written to /private/tmp.");
        }

        private static void BuildScreen(Transform root, UIShellLoadingProgressView progressView)
        {
            Image backgroundImage = CreateImage("LoadingEnvironment", root, background, Color.white);
            Stretch(backgroundImage.rectTransform);
            AspectRatioFitter backgroundFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = background.rect.width / background.rect.height;

            RectTransform chromeReference = CreateTopLeftRect(
                "SplashChromeReference",
                root,
                0f,
                0f,
                ReferenceResolution.x,
                ReferenceResolution.y);
            MainMenuV3SectionLayoutView chromeLayout = chromeReference.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            chromeLayout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center);

            V3GradientGraphic topReadability = CreateGradient(
                "TopReadability",
                chromeReference,
                new Color(0f, 0f, 0f, 0.34f),
                new Color(0f, 0f, 0f, 0f),
                Color.clear,
                0f);
            SetRect(topReadability.rectTransform, new Vector2(0f, 0.72f), Vector2.one, Vector2.zero, Vector2.zero);

            BuildLogoPlate(chromeReference);
            BuildStatusChip("CommandSystemChip", chromeReference, 18f, 209f, 361f, 70f, "COMMAND SYSTEM", theme.Amber, CreateSignalIcon);
            BuildStatusChip("AndroidBuildChip", chromeReference, 960f, 21f, 283f, 70f, "ANDROID BUILD", new Color32(132, 202, 38, 255), CreateAndroidIcon);
            BuildStatusChip("SecureLinkChip", chromeReference, 1254f, 21f, 282f, 70f, "SECURE LINK", new Color32(132, 202, 38, 255), CreateLockIcon);
            BuildSignalOnlyChip(chromeReference);
            BuildFooter(chromeReference, progressView);
        }

        private static void BuildLogoPlate(Transform root)
        {
            RectTransform plate = CreateTopLeftRect("BrandLogoPlate", root, 19f, 25f, 598f, 168f);
            V3GradientGraphic fill = plate.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(
                new Color32(22, 34, 38, 252),
                new Color32(13, 25, 29, 252),
                new Color32(5, 12, 15, 252),
                new Color32(8, 16, 19, 252),
                ChromeBorder,
                3f);

            V3UiFoundationBuilder.AddMainMenuLogo(plate, left: 16f, top: 10f, right: 16f, bottom: 10f);
        }

        private static void BuildStatusChip(
            string name,
            Transform root,
            float x,
            float y,
            float width,
            float height,
            string label,
            Color accent,
            Action<RectTransform, Color> iconBuilder)
        {
            RectTransform chip = CreateTopLeftRect(name, root, x, y, width, height);
            V3GradientGraphic fill = chip.gameObject.AddComponent<V3GradientGraphic>();
            fill.ConfigureCorners(GraphiteTop, new Color32(15, 27, 31, 248), GraphiteBottom, new Color32(8, 17, 20, 252), ChromeBorder, 3f);
            RectTransform icon = CreateTopLeftRect("Icon", chip, 17f, 11f, 52f, 48f);
            iconBuilder(icon, accent);
            TMP_Text text = CreateText("Label", chip, label, 27f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(text.rectTransform, 75f, 8f, width - 82f, 54f);
        }

        private static void BuildSignalOnlyChip(Transform root)
        {
            RectTransform chip = CreateTopLeftRect("SignalStrengthChip", root, 1548f, 21f, 94f, 70f);
            V3GradientGraphic fill = chip.gameObject.AddComponent<V3GradientGraphic>();
            fill.Configure(GraphiteTop, GraphiteBottom, ChromeBorder, 3f);
            CreateSignalIcon(CreateTopLeftRect("Icon", chip, 17f, 10f, 60f, 50f), new Color32(132, 202, 38, 255));
        }

        private static void BuildFooter(Transform root, UIShellLoadingProgressView progressView)
        {
            RectTransform footer = CreateRect(
                "IntegratedLoadingFooter",
                root,
                Vector2.zero,
                new Vector2(1f, 0.228f),
                Vector2.zero,
                Vector2.zero);
            V3GradientGraphic footerFill = footer.gameObject.AddComponent<V3GradientGraphic>();
            footerFill.ConfigureCorners(
                new Color32(21, 35, 39, 252),
                new Color32(15, 29, 33, 252),
                new Color32(4, 10, 12, 255),
                new Color32(7, 14, 16, 255),
                ChromeBorder,
                3f);

            TMP_Text title = CreateText("LoadingTitle", footer, "LOADING OPERATION MAP", 50f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 36f, 13f, 1110f, 75f);
            TMP_Text percent = CreateText("LoadingPercent", footer, "68%", 67f, boldFont, TextAlignmentOptions.MidlineRight, theme.Cyan);
            SetTopLeft(percent.rectTransform, 1435f, 8f, 200f, 80f);

            RectTransform track = CreateTopLeftRect("ProgressTrack", footer, 38f, 96f, 1597f, 35f);
            V3GradientGraphic trackGraphic = track.gameObject.AddComponent<V3GradientGraphic>();
            trackGraphic.Configure(new Color32(12, 22, 25, 255), new Color32(4, 10, 12, 255), ChromeBorder, 2f);

            RectTransform progressFill = CreateRect(
                "ProgressFill",
                track,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(1085f, 27f),
                new Vector2(4f, 0f));
            progressFill.pivot = new Vector2(0f, 0.5f);
            V3GradientGraphic progressGradient = progressFill.gameObject.AddComponent<V3GradientGraphic>();
            progressGradient.ConfigureCorners(
                new Color32(43, 227, 230, 255),
                new Color32(25, 211, 218, 255),
                new Color32(0, 144, 168, 255),
                new Color32(0, 165, 181, 255),
                new Color32(67, 238, 239, 255),
                1f);

            for (int i = 1; i < 6; i++)
            {
                float segmentX = 1597f * i / 6f;
                CreateSolidTopLeft("SegmentDivider" + i, track, segmentX, 2f, 3f, 31f, new Color32(4, 16, 20, 220));
            }

            RectTransform spinner = CreateTopLeftRect("LoadingSpinner", footer, 38f, 155f, 39f, 39f);
            CreateSpinner(spinner, theme.Cyan);
            TMP_Text status = CreateText("LoadingStatus", footer, "LOADING REQUIRED DATA", 23f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(status.rectTransform, 94f, 151f, 640f, 48f);
            status.rectTransform.localScale = new Vector3(0.74f, 1f, 1f);
            TMP_Text tipLabel = CreateText("TipLabel", footer, "Tip:", 25f, mediumFont, TextAlignmentOptions.MidlineRight, theme.Cyan);
            SetTopLeft(tipLabel.rectTransform, 1120f, 151f, 96f, 48f);
            TMP_Text tip = CreateText("TipText", footer, "Scout streets before committing armor.", 22f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(tip.rectTransform, 1224f, 151f, 410f, 48f);

            SetObject(progressView, "progressFill", progressFill);
            SetObject(progressView, "percentText", percent);
            SetObject(progressView, "statusText", status);
            SetFloat(progressView, "fillWidth", 1591f);
        }

        private static void CreateAndroidIcon(RectTransform root, Color color)
        {
            CreateSolid("Body", root, color, new Vector2(30f, 27f), new Vector2(0f, -3f));
            CreateSolid("Head", root, color, new Vector2(30f, 15f), new Vector2(0f, 14f));
            CreateSolid("ArmLeft", root, color, new Vector2(5f, 25f), new Vector2(-19f, -3f));
            CreateSolid("ArmRight", root, color, new Vector2(5f, 25f), new Vector2(19f, -3f));
            CreateSolid("LegLeft", root, color, new Vector2(6f, 14f), new Vector2(-7f, -22f));
            CreateSolid("LegRight", root, color, new Vector2(6f, 14f), new Vector2(7f, -22f));
            Image antennaLeft = CreateSolid("AntennaLeft", root, color, new Vector2(3f, 11f), new Vector2(-7f, 23f));
            antennaLeft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            Image antennaRight = CreateSolid("AntennaRight", root, color, new Vector2(3f, 11f), new Vector2(7f, 23f));
            antennaRight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            CreateSolid("EyeLeft", root, GraphiteBottom, new Vector2(3f, 3f), new Vector2(-6f, 14f));
            CreateSolid("EyeRight", root, GraphiteBottom, new Vector2(3f, 3f), new Vector2(6f, 14f));
        }

        private static void CreateLockIcon(RectTransform root, Color color)
        {
            CreateSolid("Body", root, color, new Vector2(31f, 28f), new Vector2(0f, -7f));
            CreateSolid("ShackleTop", root, color, new Vector2(24f, 5f), new Vector2(0f, 17f));
            CreateSolid("ShackleLeft", root, color, new Vector2(5f, 17f), new Vector2(-10f, 10f));
            CreateSolid("ShackleRight", root, color, new Vector2(5f, 17f), new Vector2(10f, 10f));
            CreateSolid("KeySlot", root, GraphiteBottom, new Vector2(5f, 13f), new Vector2(0f, -7f));
        }

        private static void CreateSignalIcon(RectTransform root, Color color)
        {
            for (int i = 0; i < 5; i++)
            {
                float height = 12f + i * 8f;
                CreateSolid("SignalBar" + i, root, color, new Vector2(7f, height), new Vector2(-20f + i * 10f, -16f + height * 0.5f));
            }
        }

        private static void CreateSpinner(RectTransform root, Color color)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 36f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Sin(radians) * 14f, Mathf.Cos(radians) * 14f);
                Color segmentColor = new Color(color.r, color.g, color.b, 0.3f + i * 0.07f);
                Image segment = CreateSolid("SpinnerSegment" + i, root, segmentColor, new Vector2(5f, 10f), position);
                segment.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }

        private static void CreateChevron(string name, Transform parent, float centerX, float centerY, Color color)
        {
            Image left = CreateSolid(name + "Left", parent, color, new Vector2(38f, 7f), new Vector2(centerX - 13f, centerY));
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            Image right = CreateSolid(name + "Right", parent, color, new Vector2(38f, 7f), new Vector2(centerX + 13f, centerY));
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        }

        private static void ConfigureBackgroundImport()
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Splash background did not import as a texture: {BackgroundPath}");

            bool dirty = importer.textureType != TextureImporterType.Sprite ||
                         importer.mipmapEnabled ||
                         importer.wrapMode != TextureWrapMode.Clamp ||
                         importer.maxTextureSize < 2048;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.maxTextureSize = 4096;
            if (dirty)
                importer.SaveAndReimport();
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            lightFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LightFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (boldFont == null || mediumFont == null || lightFont == null || background == null)
                throw new MissingReferenceException("Splash V3 is missing a required font or its unique background sprite.");
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Splash prefab for capture: {PrefabPath}");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("SplashV3CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            GameObject canvasObject = new GameObject("SplashV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            instance.name = prefab.name;
            if (instance.transform is RectTransform instanceRect)
                Stretch(instanceRect);

            Canvas.ForceUpdateCanvases();
            instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
            Canvas.ForceUpdateCanvases();
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"[SplashLoadingV3PrefabBuilder] captured={outputPath} size={width}x{height} scene={scene.name}");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position)
        {
            return V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, position);
        }

        private static RectTransform CreateTopLeftRect(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            return V3UiPrefabFactory.CreateImage(name, parent, sprite, color, false, false);
        }

        private static Image CreateSolid(string name, Transform parent, Color color, Vector2 size, Vector2 position)
        {
            Image image = CreateImage(name, parent, null, color);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            return image;
        }

        private static Image CreateSolidTopLeft(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static V3GradientGraphic CreateGradient(string name, Transform parent, Color top, Color bottom, Color border, float width)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), Vector2.zero);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, width);
            return graphic;
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
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            if (parent.name == name)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = FindDeepChild(parent.GetChild(i), name);
                if (child != null)
                    return child;
            }
            return null;
        }

        private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(UnityEngine.Object target, string fieldName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
