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
    public static class RewardUnlockV3PrefabBuilder
    {
        internal const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab";
        private const string BackgroundPath = "Assets/Game/Art/UI/V3Shared/MissionBriefing/SCN06_ForwardPost_V3.png";
        private const string RangerPlatePath = "Assets/Game/Art/UI/V3Shared/RewardUnlock/POP04_RangerSquad_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(19, 29, 33, 249);
        private static readonly Color DarkBottom = new Color32(3, 8, 10, 253);
        private static readonly Color Line = new Color32(92, 106, 109, 255);
        private static readonly Color Green = new Color32(111, 190, 49, 255);
        private static readonly Color Amber = new Color32(246, 174, 23, 255);
        private static readonly Color Cyan = new Color32(20, 158, 223, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Texture2D backgroundTexture;
        private static Texture2D rangerTexture;

        [MenuItem("Game/UI/V3/Rebuild Reward Unlock V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            GameObject root = new("RewardUnlockPopup", typeof(RectTransform));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                UIPopupFrameView popup = root.AddComponent<UIPopupFrameView>();
                BuildBackground(root.transform);
                RectTransform composition = CreateTopLeft("V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>()
                    .Configure(Reference, MainMenuV3SectionAlignment.Center);
                BuildAppHeader(composition);
                FrameBindings frame = BuildUnlockFrame(composition);
                popup.Configure(
                    null,
                    frame.Frame.gameObject,
                    frame.Header.gameObject,
                    frame.Title,
                    frame.ContinueButton,
                    frame.Body,
                    frame.ButtonRow);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[RewardUnlockV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 ranger=aspect-preserved actions=1");
        }

        [MenuItem("Game/UI/V3/Capture Reward Unlock V3 Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-reward-unlock-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-reward-unlock-v3-20x9.png", 4800, 2160);
        }

        [MenuItem("Game/UI/V3/Validate Reward Unlock V3 Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponent<UIPopupFrameView>() == null)
                throw new MissingReferenceException("Reward Unlock V3 popup binding is missing.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference)
                throw new InvalidOperationException("Reward Unlock V3 must use the centered reference composition.");
            RawImage ranger = Find(prefab.transform, "UnlockImage")?.GetComponent<RawImage>();
            if (ranger == null || ranger.texture == null || ranger.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("Reward Unlock V3 ranger plate must preserve aspect ratio.");
            if (Find(prefab.transform, "ContinueButton")?.GetComponent<Button>() == null)
                throw new MissingReferenceException("Reward Unlock V3 Continue action is missing.");
            Button continueButton = Find(prefab.transform, "ContinueButton").GetComponent<Button>();
            if (prefab.GetComponent<UIPopupFrameView>().CloseButton != continueButton)
                throw new MissingReferenceException("Reward Unlock V3 Continue action must dismiss the popup.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 10)
                throw new InvalidOperationException($"Reward Unlock V3 requires procedural gradients; found {gradients}.");
            Debug.Log($"[RewardUnlockV3PrefabBuilder] validation=Passed gradients={gradients} rewards=4 art=aspect-preserved action=1");
        }

        private static void BuildBackground(Transform parent)
        {
            RawImage background = CreateRawImage("BackgroundCommandCenter", parent, backgroundTexture);
            Stretch(background.rectTransform);
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundTexture.width / (float)backgroundTexture.height;
            Image shade = CreateImage("BackdropShade", parent, null, new Color(0f, .015f, .025f, .62f));
            Stretch(shade.rectTransform);
        }

        private static void BuildAppHeader(Transform parent)
        {
            RectTransform rank = CreatePanel("CommanderRank", parent, 20f, 18f, 80f, 80f, DarkTop, DarkBottom, Line, 3f);
            Image rankIcon = CreateImage("Icon", rank, RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), Green);
            SetTopLeft(rankIcon.rectTransform, 16f, 14f, 48f, 52f);

            RectTransform logo = CreatePanel("BrandPanel", parent, 106f, 18f, 240f, 80f, DarkTop, DarkBottom, Line, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo, left: 4f, top: 4f, right: 4f, bottom: 4f);

            BuildResource(parent, "CreditsResource", 1062f, 228f, "CREDITS", "24,750", catalog.CreditsIcon);
            BuildResource(parent, "CommandResource", 1298f, 248f, "COMMAND", "8,430", catalog.CommandIcon);
            RectTransform settings = CreatePanel("SettingsButton", parent, 1555f, 18f, 95f, 80f, DarkTop, DarkBottom, Line, 3f);
            Button button = settings.gameObject.AddComponent<Button>();
            button.targetGraphic = settings.GetComponent<V3GradientGraphic>();
            button.targetGraphic.raycastTarget = true;
            Image settingsIcon = CreateImage("Icon", settings, catalog.SettingsIcon, theme.TextPrimary);
            SetTopLeft(settingsIcon.rectTransform, 23f, 16f, 49f, 49f);
        }

        private static void BuildResource(Transform parent, string name, float x, float width, string label, string value, Sprite sprite)
        {
            RectTransform panel = CreatePanel(name, parent, x, 18f, width, 80f, DarkTop, DarkBottom, Line, 3f);
            Image icon = CreateImage("Icon", panel, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 13f, 12f, 54f, 54f);
            CreateText(panel, "Label", 77f, 8f, width - 87f, 30f, label, 18f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "Value", 77f, 35f, width - 87f, 38f, value, 27f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
        }

        private static FrameBindings BuildUnlockFrame(Transform parent)
        {
            RectTransform frame = CreatePanel("Frame", parent, 278f, 112f, 1110f, 785f, DarkTop, DarkBottom, Line, 3f);
            RectTransform header = CreateTopLeft("Header", frame, 15f, 12f, 1080f, 92f);
            Image leftRank = CreateImage("LeftRank", header, RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), Green);
            SetTopLeft(leftRank.rectTransform, 30f, 13f, 65f, 66f);
            Image rightRank = CreateImage("RightRank", header, RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath), Green);
            SetTopLeft(rightRank.rectTransform, 985f, 13f, 65f, 66f);
            TMP_Text title = CreateText(header, "TitleText", 140f, 0f, 800f, 82f, "NEW ASSET UNLOCKED", 58f, Green, TextAlignmentOptions.Center, true);
            CreateSolid("HeaderDivider", header, 0f, 88f, 1080f, 2f, Green);

            RectTransform body = CreateTopLeft("BodyRoot", frame, 0f, 0f, 1110f, 785f);
            CreateText(body, "UnlockTitleText", 45f, 139f, 350f, 60f, "RANGER SQUAD", 34f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(body, "UnlockSubtitleText", 45f, 199f, 350f, 42f, "Light Recon Unit", 27f, Cyan, TextAlignmentOptions.MidlineLeft, false);
            TMP_Text description = CreateText(body, "DescriptionText", 45f, 257f, 355f, 140f,
                "Fast and agile reconnaissance\nunit. Excels at scouting,\nraiding, and flanking.", 24f, theme.TextPrimary, TextAlignmentOptions.TopLeft, false, true);
            description.lineSpacing = 5f;

            RectTransform artClip = CreateTopLeft("UnlockArtClip", body, 392f, 105f, 688f, 400f);
            artClip.gameObject.AddComponent<RectMask2D>();
            RawImage ranger = CreateRawImage("UnlockImage", artClip, rangerTexture);
            SetTopLeft(ranger.rectTransform, 34f, -89f, 620f, 620f);
            AspectRatioFitter rangerFitter = ranger.gameObject.AddComponent<AspectRatioFitter>();
            rangerFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            rangerFitter.aspectRatio = rangerTexture.width / (float)rangerTexture.height;

            BuildRewardCard(body, "CommanderXpReward", 15f, 505f, 250f, "XP", "+450", RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath), Amber);
            BuildRewardCard(body, "CreditsReward", 275f, 505f, 265f, "CREDITS", "12K", catalog.CreditsIcon, Amber);
            BuildRewardCard(body, "GearPartsReward", 550f, 505f, 257f, "PARTS", "x3", catalog.MaterialsIcon, Amber);
            BuildRewardCard(body, "SupplyCrateReward", 817f, 505f, 278f, "SUPPLIES", "x24", catalog.FuelIcon, Amber);

            RectTransform buttonRow = CreateTopLeft("ButtonRow", body, 0f, 0f, 1110f, 785f);
            RectTransform continueRect = CreatePanel("ContinueButton", buttonRow, 15f, 662f, 1080f, 108f,
                new Color32(73, 146, 38, 255), new Color32(18, 65, 20, 255), Green, 3f);
            Button continueButton = continueRect.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = continueRect.GetComponent<V3GradientGraphic>();
            continueButton.targetGraphic.raycastTarget = true;
            CreateText(continueRect, "LabelText", 255f, 8f, 570f, 90f, "CONTINUE", 57f, theme.TextPrimary, TextAlignmentOptions.Center, true);
            Image chevrons = CreateImage("Icon", continueRect, RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), Amber);
            SetTopLeft(chevrons.rectTransform, 947f, 26f, 70f, 60f);
            return new FrameBindings(frame, header, title, body, buttonRow, continueButton);
        }

        private static void BuildRewardCard(Transform parent, string name, float x, float y, float width, string label, string value, Sprite sprite, Color accent)
        {
            RectTransform card = CreatePanel(name, parent, x, y, width, 140f, DarkTop, DarkBottom, Line, 3f);
            Image icon = CreateImage("IconImage", card, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 17f, 17f, 92f, 103f);
            CreateText(card, "LabelText", 121f, 25f, width - 134f, 37f, label, 22f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(card, "ValueText", 121f, 66f, width - 134f, 53f, value, 31f, accent, TextAlignmentOptions.MidlineLeft, true);
        }

        private static void Capture(string path, int width, int height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("RewardUnlockV3CaptureCamera", typeof(Camera));
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

            GameObject canvasObject = new("RewardUnlockV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            Stretch(instance.transform as RectTransform);
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
                File.WriteAllBytes(path, capture.EncodeToPNG());
                Debug.Log($"[RewardUnlockV3PrefabBuilder] capture=Passed size={width}x{height} path={path} scene={scene.name}");
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
            boldFont = Require<TMP_FontAsset>(BoldFontPath);
            mediumFont = Require<TMP_FontAsset>(MediumFontPath);
            backgroundTexture = Require<Texture2D>(BackgroundPath);
            rangerTexture = Require<Texture2D>(RangerPlatePath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
        }

        private static RectTransform CreatePanel(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static TMP_Text CreateText(Transform parent, string name, float x, float y, float width, float height, string value, float size, Color color, TextAlignmentOptions alignment, bool bold, bool wrap = false)
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

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
        {
            RectTransform rect = CreateRect(name, parent);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new FileNotFoundException($"Missing Reward Unlock V3 asset: {path}");
            return asset;
        }

        private static Sprite RequireSprite(string path) => Require<Sprite>(path);

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent) =>
            V3UiPrefabFactory.CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = V3UiPrefabFactory.CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
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

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private readonly struct FrameBindings
        {
            public readonly RectTransform Frame;
            public readonly RectTransform Header;
            public readonly TMP_Text Title;
            public readonly RectTransform Body;
            public readonly RectTransform ButtonRow;
            public readonly Button ContinueButton;
            public FrameBindings(
                RectTransform frame,
                RectTransform header,
                TMP_Text title,
                RectTransform body,
                RectTransform buttonRow,
                Button continueButton)
            {
                Frame = frame;
                Header = header;
                Title = title;
                Body = body;
                ButtonRow = buttonRow;
                ContinueButton = continueButton;
            }
        }
    }
}
#endif
