#if UNITY_EDITOR
using System;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class SplashLoadingV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab";
        private const string BackgroundPath = "Assets/Game/Art/UI/Generated/SplashLoading/V3/scn01_v3_command_post_background.png";
        private const string RankIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_shield_rank_badge.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color Panel = new(0.018f, 0.041f, 0.047f, 0.965f);
        private static readonly Color PanelOpaque = new(0.014f, 0.031f, 0.035f, 0.995f);
        private static readonly Color Border = new(0.22f, 0.29f, 0.31f, 1f);
        private static readonly Color White = new(0.94f, 0.95f, 0.93f, 1f);
        private static readonly Color Muted = new(0.68f, 0.72f, 0.72f, 1f);
        private static readonly Color Gold = new(1f, 0.67f, 0.015f, 1f);
        private static readonly Color Cyan = new(0.035f, 0.82f, 0.88f, 1f);
        private static readonly Color Green = new(0.49f, 0.78f, 0.08f, 1f);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Build SCN-01 Splash Loading")]
        public static void Build()
        {
            LoadStyleAssets();
            EnsureSpriteImport(BackgroundPath);
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ClearChildren(root.transform);
                RectTransform rootRect = RequireRect(root);
                Stretch(rootRect);

                BuildBackground(root.transform);
                BuildBrand(root.transform);
                BuildStatus(root.transform);
                UIShellLoadingProgressView progressView = BuildLoadingPanel(root.transform);
                if (progressView == null)
                    throw new InvalidOperationException("SCN-01 V3 loading progress view was not created.");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SplashLoadingV3PrefabBuilder] result=Passed prefab={PrefabPath}");
        }

        private static void BuildBackground(Transform root)
        {
            GameObject background = CreateRect("V3_Background", root);
            Stretch(background.GetComponent<RectTransform>());
            Image image = background.AddComponent<Image>();
            image.sprite = RequireSprite(BackgroundPath);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            AspectRatioFitter fitter = background.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;

            Image grade = CreateSolid("V3_ReadabilityGrade", root, new Color(0.005f, 0.012f, 0.014f, 0.12f));
            Stretch(grade.rectTransform);
        }

        private static void BuildBrand(Transform root)
        {
            RectTransform brand = CreateBorderedPanel("V3_Brand", root, new Vector2(54f, -54f), new Vector2(1720f, 410f), PanelOpaque);
            CreateSolid("GoldRail", brand, Gold, new Vector2(48f, -64f), new Vector2(28f, 220f));
            CreateText("Warline", brand, "WARLINE", 218f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(112f, -26f), new Vector2(1300f, 245f), boldFont, 2.5f);
            CreateSolid("WordDivider", brand, Gold, new Vector2(112f, -286f), new Vector2(260f, 12f));
            CreateSolid("WordDividerRight", brand, Gold, new Vector2(1060f, -286f), new Vector2(190f, 12f));
            CreateText("Capture", brand, "CAPTURE", 92f, Gold, TextAlignmentOptions.Center,
                new Vector2(392f, -250f), new Vector2(640f, 100f), boldFont, 4f);
            CreateIcon("Rank", brand, RankIconPath, new Vector2(1392f, -66f), new Vector2(245f, 245f), Gold);

            RectTransform command = CreateBorderedPanel("V3_CommandSystem", root, new Vector2(54f, -492f), new Vector2(1180f, 172f), PanelOpaque);
            for (int i = 0; i < 4; i++)
            {
                float height = 44f + i * 20f;
                CreateSolid($"Signal{i + 1}", command, Gold, new Vector2(54f + i * 36f, -(118f - height)), new Vector2(24f, height));
            }
            CreateSolid("Divider", command, Border, new Vector2(218f, -34f), new Vector2(4f, 104f));
            CreateText("Label", command, "COMMAND SYSTEM", 66f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(264f, -20f), new Vector2(860f, 126f), boldFont, 1f);
        }

        private static void BuildStatus(Transform root)
        {
            RectTransform rail = CreateRect("V3_StatusRail", root).GetComponent<RectTransform>();
            rail.anchorMin = new Vector2(1f, 1f);
            rail.anchorMax = new Vector2(1f, 1f);
            rail.pivot = new Vector2(1f, 1f);
            rail.anchoredPosition = new Vector2(-56f, -52f);
            rail.sizeDelta = new Vector2(1960f, 174f);

            RectTransform android = CreateBorderedPanel("AndroidBuild", rail, new Vector2(0f, 0f), new Vector2(800f, 174f), PanelOpaque);
            CreateText("Icon", android, "A", 72f, Green, TextAlignmentOptions.Center,
                new Vector2(46f, -22f), new Vector2(116f, 118f), boldFont);
            CreateText("Label", android, "ANDROID BUILD", 60f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(190f, -18f), new Vector2(580f, 126f), boldFont, 0f);

            RectTransform secure = CreateBorderedPanel("SecureLink", rail, new Vector2(832f, 0f), new Vector2(770f, 174f), PanelOpaque);
            CreateText("Icon", secure, "LOCK", 34f, Green, TextAlignmentOptions.Center,
                new Vector2(42f, -42f), new Vector2(146f, 78f), boldFont, 1f);
            CreateText("Label", secure, "SECURE LINK", 67f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(206f, -18f), new Vector2(510f, 126f), boldFont, 1f);

            RectTransform signal = CreateBorderedPanel("Signal", rail, new Vector2(1634f, 0f), new Vector2(326f, 174f), PanelOpaque);
            for (int i = 0; i < 5; i++)
            {
                float height = 34f + i * 19f;
                CreateSolid($"Bar{i + 1}", signal, Green, new Vector2(56f + i * 45f, -(132f - height)), new Vector2(29f, height));
            }
        }

        private static UIShellLoadingProgressView BuildLoadingPanel(Transform root)
        {
            RectTransform panel = CreateBorderedPanel("V3_LoadingPanel", root, Vector2.zero, Vector2.zero, PanelOpaque);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(0f, 560f);
            RectTransform panelFill = panel.Find("Fill")?.GetComponent<RectTransform>();
            if (panelFill == null)
                throw new InvalidOperationException("SCN-01 V3 loading panel is missing its fill.");
            panelFill.anchorMin = Vector2.zero;
            panelFill.anchorMax = Vector2.one;
            panelFill.offsetMin = new Vector2(6f, 6f);
            panelFill.offsetMax = new Vector2(-6f, -6f);

            CreateText("Title", panel, "LOADING OPERATION MAP", 116f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(92f, -40f), new Vector2(3300f, 150f), boldFont, 1f);
            TMP_Text percent = CreateText("Percent", panel, "68%", 146f, Cyan, TextAlignmentOptions.MidlineRight,
                new Vector2(3800f, -28f), new Vector2(910f, 170f), boldFont, 1f);

            RectTransform track = CreateBorderedPanel("ProgressTrack", panel, new Vector2(96f, -208f), new Vector2(4608f, 84f), new Color(0.012f, 0.025f, 0.028f, 1f));
            Image fill = CreateSolid("ProgressFill", track, Cyan, new Vector2(8f, -8f), new Vector2(0f, 68f));
            fill.rectTransform.anchorMin = new Vector2(0f, 1f);
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 1f);
            for (int i = 1; i < 6; i++)
                CreateSolid($"SegmentDivider{i}", track, new Color(0.02f, 0.12f, 0.14f, 1f), new Vector2(i * 765f, -8f), new Vector2(6f, 68f));

            BuildSpinner(panel, new Vector2(142f, -396f));
            TMP_Text status = CreateText("Status", panel, "LOADING REQUIRED DATA", 56f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(222f, -334f), new Vector2(1900f, 120f), boldFont, 1.5f);
            CreateText("TipPrefix", panel, "Tip:", 54f, Cyan, TextAlignmentOptions.MidlineRight,
                new Vector2(2980f, -334f), new Vector2(220f, 120f), mediumFont);
            CreateText("Tip", panel, "Scout streets before committing armor.", 54f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(3226f, -334f), new Vector2(1460f, 120f), mediumFont);

            UIShellLoadingProgressView view = panel.gameObject.AddComponent<UIShellLoadingProgressView>();
            view.Configure(fill.rectTransform, percent, status, 4592f);
            return view;
        }

        private static void BuildSpinner(Transform parent, Vector2 center)
        {
            RectTransform root = CreateRect("Spinner", parent).GetComponent<RectTransform>();
            root.anchoredPosition = new Vector2(center.x - 48f, center.y + 48f);
            root.sizeDelta = new Vector2(96f, 96f);
            root.pivot = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 offset = new(Mathf.Sin(radians) * 35f, Mathf.Cos(radians) * 35f);
                Image segment = CreateSolid($"Segment{i + 1}", root, new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f + i * 0.09f));
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = offset;
                rect.sizeDelta = new Vector2(13f, 28f);
                rect.localEulerAngles = new Vector3(0f, 0f, -angle);
            }
        }

        private static RectTransform CreateBorderedPanel(string name, Transform parent, Vector2 position, Vector2 size, Color fill)
        {
            GameObject outer = CreateRect(name, parent);
            RectTransform rect = outer.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image border = outer.AddComponent<Image>();
            border.color = Border;
            border.raycastTarget = false;
            Image inner = CreateSolid("Fill", rect, fill, new Vector2(6f, -6f), new Vector2(Mathf.Max(0f, size.x - 12f), Mathf.Max(0f, size.y - 12f)));
            inner.transform.SetAsFirstSibling();
            return rect;
        }

        private static Image CreateSolid(string name, Transform parent, Color color)
        {
            GameObject root = CreateRect(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, Vector2 position, Vector2 size)
        {
            Image image = CreateSolid(name, parent, color);
            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private static Image CreateIcon(string name, Transform parent, string path, Vector2 position, Vector2 size, Color tint)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = root.AddComponent<Image>();
            image.sprite = RequireSprite(path);
            image.color = tint;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            TMP_FontAsset font,
            float characterSpacing = 0f)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.characterSpacing = characterSpacing;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return root;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform RequireRect(GameObject root)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                throw new InvalidOperationException($"Expected RectTransform on {root.name}.");
            return rect;
        }

        private static void ClearChildren(Transform root)
        {
            while (root.childCount > 0)
                UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
        }

        private static void LoadStyleAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null || mediumFont == null)
                throw new InvalidOperationException("SCN-01 V3 requires the Oxanium Bold and Medium TMP font assets.");
        }

        private static void EnsureSpriteImport(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing texture importer: {path}");
            bool dirty = importer.textureType != TextureImporterType.Sprite ||
                         importer.spriteImportMode != SpriteImportMode.Single ||
                         importer.mipmapEnabled ||
                         importer.maxTextureSize != 2048;
            if (!dirty)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing UI sprite: {path}");
            return sprite;
        }
    }
}
#endif
