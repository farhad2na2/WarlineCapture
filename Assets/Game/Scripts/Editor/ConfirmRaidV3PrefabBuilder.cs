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
    public static class ConfirmRaidV3PrefabBuilder
    {
        internal const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab";
        private const string OperationsPrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN11_OperationsDashboardContent.prefab";
        private const string MapPath =
            "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(24, 33, 37, 253);
        private static readonly Color DarkBottom = new Color32(2, 7, 9, 255);
        private static readonly Color RaisedTop = new Color32(35, 45, 49, 253);
        private static readonly Color Line = new Color32(106, 121, 125, 255);
        private static readonly Color White = new Color32(242, 244, 240, 255);
        private static readonly Color Cyan = new Color32(30, 174, 230, 255);
        private static readonly Color Amber = new Color32(250, 171, 25, 255);
        private static readonly Color Orange = new Color32(255, 74, 42, 255);
        private static readonly Color RedTop = new Color32(216, 59, 31, 255);
        private static readonly Color RedBottom = new Color32(98, 19, 10, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static Texture2D mapTexture;

        [MenuItem("Game/UI/V3/Rebuild POP-02 Confirm Raid")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ClearChildren(root.transform);
                RectTransform rootRect = root.transform as RectTransform;
                Stretch(rootRect);
                rootRect.sizeDelta = Reference;

                RectTransform composition = CreateTopLeft(
                    "V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                Image scrim = CreateSolid("Scrim", composition, new Color(0f, 0f, 0f, .60f), true);
                SetTopLeft(scrim.rectTransform, 0f, 0f, Reference.x, Reference.y);

                FrameBindings bindings = BuildFrame(composition);
                composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    shouldExpandToCanvasWidth: true,
                    targetsAnchoredToCenter: new[] { bindings.Frame },
                    targetsExpandedAcrossWidth: new[] { scrim.rectTransform });

                UIPopupFrameView popup = root.GetComponent<UIPopupFrameView>() ??
                    root.AddComponent<UIPopupFrameView>();
                popup.Configure(
                    scrim.gameObject,
                    bindings.Frame.gameObject,
                    bindings.Header.gameObject,
                    bindings.Title,
                    bindings.CancelButton,
                    bindings.Body,
                    bindings.Footer);

                ConfirmRaidV3PopupView state = root.GetComponent<ConfirmRaidV3PopupView>() ??
                    root.AddComponent<ConfirmRaidV3PopupView>();
                state.Configure(bindings.CancelButton, bindings.ConfirmButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[ConfirmRaidV3PrefabBuilder] result=Passed layout=target gradients=procedural borders=3 map=reused-aspect-preserved icons=shared-v3");
        }

        [MenuItem("Game/UI/V3/Capture POP-02 Confirm Raid Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-confirm-raid-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-confirm-raid-v3-20x9.png", 4800, 2160);
        }

        [MenuItem("Game/UI/V3/Validate POP-02 Confirm Raid")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing POP-02 prefab: {PrefabPath}");
            if (prefab.GetComponent<UIPopupFrameView>() == null ||
                prefab.GetComponent<ConfirmRaidV3PopupView>() == null)
                throw new MissingReferenceException("POP-02 runtime popup bindings are incomplete.");

            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference || !layout.ExpandToCanvasWidth)
                throw new InvalidOperationException("POP-02 must use the responsive 1672x941 reference composition.");

            RawImage map = Find(prefab.transform, "TargetMapImage")?.GetComponent<RawImage>();
            AspectRatioFitter fitter = map != null ? map.GetComponent<AspectRatioFitter>() : null;
            if (map == null || map.texture == null || fitter == null ||
                fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("POP-02 district map must reuse aspect-preserved art.");

            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 28)
                throw new InvalidOperationException($"POP-02 requires layered gradients; found {gradients}.");
        }

        private static FrameBindings BuildFrame(Transform parent)
        {
            RectTransform frame = CreatePanel("Frame", parent, 332f, 104f, 1008f, 688f,
                DarkTop, DarkBottom, Line, 3f);
            RectTransform header = CreatePanel("Header", frame, 3f, 3f, 1002f, 88f,
                new Color32(27, 34, 38, 255), new Color32(5, 9, 11, 255), Line, 3f);
            Image titleIcon = CreateImage("TitleSkullIcon", header,
                RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), Orange);
            SetTopLeft(titleIcon.rectTransform, 255f, 13f, 62f, 62f);
            RectTransform titleIconFrame = CreatePanel("TitleIconFrame", header, 247f, 8f, 78f, 72f,
                new Color32(50, 21, 18, 170), new Color32(12, 7, 7, 210), Orange, 3f);
            titleIcon.transform.SetAsLastSibling();
            TMP_Text title = CreateText("TitleText", header, "CONFIRM RAID", 45f, boldFont,
                TextAlignmentOptions.Center, Orange);
            SetTopLeft(title.rectTransform, 335f, 5f, 440f, 78f);

            RectTransform body = CreateTopLeft("BodyRoot", frame, 0f, 91f, 1008f, 496f);
            BuildTargetCard(body);
            BuildMetricCard(body, "IntelConfidenceCard", 344f, 5f, 321f, 118f,
                "INTEL CONFIDENCE", "78%", Cyan, V3UiFoundationBuilder.MatchScanIconPath, 6, 8);
            BuildMetricCard(body, "ThreatLevelCard", 671f, 5f, 331f, 118f,
                "THREAT LEVEL", "HIGH", Orange, V3UiFoundationBuilder.MissionEnemyIconPath, 5, 8);
            BuildMetricCard(body, "CollateralRiskCard", 344f, 128f, 321f, 118f,
                "COLLATERAL RISK", "MEDIUM", Amber, V3UiFoundationBuilder.MatchInvalidIconPath, 3, 6);
            BuildMetricCard(body, "CivilianDensityCard", 671f, 128f, 331f, 118f,
                "CIVILIAN DENSITY", "ELEVATED", Orange, V3UiFoundationBuilder.MatchCiviliansIconPath, 6, 6);
            BuildCompositionCard(body);
            BuildReadinessCard(body);
            BuildWarningCard(body);

            RectTransform footer = CreateTopLeft("ButtonRow", frame, 0f, 587f, 1008f, 101f);
            Button cancel = BuildActionButton(footer, "CancelButton", 6f, 7f, 445f, 86f,
                RaisedTop, new Color32(24, 27, 29, 255), Line, "CANCEL", White, false);
            Button confirm = BuildActionButton(footer, "ConfirmButton", 460f, 7f, 542f, 86f,
                RedTop, RedBottom, Orange, "CONFIRM RAID", White, true);
            return new FrameBindings(frame, header, body, footer, title, cancel, confirm);
        }

        private static void BuildTargetCard(Transform parent)
        {
            RectTransform card = CreatePanel("TargetInfoCard", parent, 6f, 5f, 332f, 486f,
                DarkTop, DarkBottom, Line, 3f);
            CreateTextAt(card, "TargetLabelText", 18f, 9f, 290f, 28f, "TARGET", 18f, Cyan,
                TextAlignmentOptions.MidlineLeft, true);
            CreateTextAt(card, "TargetNameText", 18f, 36f, 296f, 43f, "North Bridge Cell", 29f, White,
                TextAlignmentOptions.MidlineLeft, true);

            RectTransform mapViewport = CreatePanel("TargetMapViewport", card, 8f, 83f, 316f, 333f,
                DarkTop, DarkBottom, Line, 3f);
            mapViewport.gameObject.AddComponent<RectMask2D>();
            RawImage map = CreateRawImage("TargetMapImage", mapViewport, mapTexture, new Color32(206, 192, 165, 255));
            Stretch(map.rectTransform, 3f, 3f);
            AspectRatioFitter fitter = map.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = mapTexture.width / (float)mapTexture.height;
            Image shade = CreateSolid("MapThreatShade", mapViewport, new Color(.18f, .055f, .025f, .18f), false);
            Stretch(shade.rectTransform, 3f, 3f);
            Image pin = CreateImage("MapPinIcon", mapViewport,
                RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath), Orange);
            SetTopLeft(pin.rectTransform, 122f, 105f, 72f, 104f);

            RectTransform info = CreatePanel("TargetFooter", card, 8f, 421f, 316f, 57f,
                new Color32(20, 29, 32, 255), DarkBottom, Line, 3f);
            Image location = CreateImage("LocationIcon", info,
                RequireSprite(V3UiFoundationBuilder.OperationsMapPinIconPath), Cyan);
            SetTopLeft(location.rectTransform, 11f, 8f, 31f, 41f);
            CreateTextAt(info, "DistrictText", 50f, 3f, 251f, 27f,
                "DISTRICT: NORTH BRIDGE", 13f, White, TextAlignmentOptions.MidlineLeft, false);
            TMP_Text threat = CreateTextAt(info, "ThreatText", 50f, 27f, 251f, 27f,
                "THREAT LEVEL: <color=#FF4A2A>HIGH</color>", 12f, White,
                TextAlignmentOptions.MidlineLeft, false);
            threat.richText = true;
        }

        private static void BuildMetricCard(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            string label,
            string value,
            Color accent,
            string iconPath,
            int activeSegments,
            int segmentCount)
        {
            RectTransform card = CreatePanel(name, parent, x, y, width, height,
                DarkTop, DarkBottom, Line, 3f);
            CreateTextAt(card, "LabelText", 20f, 9f, width - 40f, 27f, label, 17f, accent,
                TextAlignmentOptions.MidlineLeft, true);
            RectTransform iconFrame = CreatePanel("IconFrame", card, 18f, 39f, 61f, 60f,
                new Color32(28, 35, 38, 255), DarkBottom, accent, 3f);
            Image icon = CreateImage("IconImage", iconFrame, RequireSprite(iconPath), accent);
            SetTopLeft(icon.rectTransform, 9f, 8f, 43f, 43f);
            CreateTextAt(card, "ValueText", 91f, 37f, width - 108f, 45f, value, 31f, accent,
                TextAlignmentOptions.MidlineLeft, true);
            BuildMeter(card, 91f, 85f, width - 110f, 14f, activeSegments, segmentCount, accent);
        }

        private static void BuildCompositionCard(Transform parent)
        {
            RectTransform card = CreatePanel("EnemyCompositionCard", parent, 344f, 251f, 321f, 241f,
                DarkTop, DarkBottom, Line, 3f);
            CreateTextAt(card, "TitleText", 18f, 8f, 285f, 31f,
                "ESTIMATED ENEMY COMPOSITION", 18f, Orange, TextAlignmentOptions.MidlineLeft, true);
            BuildCompositionRow(card, 46f, V3UiFoundationBuilder.MissionEnemyIconPath, "HOSTILE CELLS", "18–24");
            BuildCompositionRow(card, 91f, V3UiFoundationBuilder.MissionVehicleIconPath, "LIGHT VEHICLES", "2–4");
            BuildCompositionRow(card, 136f, V3UiFoundationBuilder.OperationsWarningIconPath, "MORTAR TEAMS", "1–2");
            BuildCompositionRow(card, 181f, V3UiFoundationBuilder.MatchJumpIconPath, "SNIPERS", "2–3");
        }

        private static void BuildCompositionRow(
            Transform parent, float y, string iconPath, string label, string value)
        {
            Image icon = CreateImage("Icon_" + label.Replace(" ", string.Empty), parent,
                RequireSprite(iconPath), new Color32(222, 216, 195, 255));
            SetTopLeft(icon.rectTransform, 18f, y + 3f, 31f, 31f);
            CreateTextAt(parent, "Label_" + label.Replace(" ", string.Empty), 61f, y, 188f, 39f,
                label, 16f, White, TextAlignmentOptions.MidlineLeft, false);
            CreateTextAt(parent, "Value_" + label.Replace(" ", string.Empty), 252f, y, 49f, 39f,
                value, 16f, White, TextAlignmentOptions.MidlineRight, true);
        }

        private static void BuildReadinessCard(Transform parent)
        {
            RectTransform card = CreatePanel("SquadReadinessCard", parent, 671f, 251f, 331f, 151f,
                DarkTop, DarkBottom, Line, 3f);
            CreateTextAt(card, "TitleText", 18f, 8f, 295f, 29f,
                "SELECTED SQUAD READINESS", 17f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            RectTransform rankFrame = CreatePanel("RankFrame", card, 18f, 42f, 67f, 70f,
                new Color32(15, 46, 59, 255), DarkBottom, Cyan, 3f);
            Image rank = CreateImage("RankIcon", rankFrame,
                RequireSprite(V3UiFoundationBuilder.MatchRankBadgeIconPath), Cyan);
            SetTopLeft(rank.rectTransform, 8f, 8f, 51f, 53f);
            CreateTextAt(card, "SquadNameText", 98f, 43f, 210f, 28f,
                "SQUAD ALPHA", 20f, new Color32(172, 221, 239, 255), TextAlignmentOptions.MidlineLeft, true);
            CreateTextAt(card, "ReadinessValueText", 98f, 68f, 210f, 41f,
                "81%", 34f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            BuildMeter(card, 18f, 123f, 295f, 15f, 6, 7, Cyan);
        }

        private static void BuildWarningCard(Transform parent)
        {
            RectTransform card = CreatePanel("CivilianWarningCard", parent, 671f, 407f, 331f, 85f,
                new Color32(31, 26, 25, 255), DarkBottom, Orange, 3f);
            Image icon = CreateImage("WarningIcon", card,
                RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath), Orange);
            SetTopLeft(icon.rectTransform, 14f, 14f, 54f, 54f);
            TMP_Text warning = CreateTextAt(card, "WarningText", 79f, 6f, 237f, 51f,
                "This operation may cause significant civilian casualties and infrastructure damage.",
                12f, White, TextAlignmentOptions.TopLeft, false);
            warning.textWrappingMode = TextWrappingModes.Normal;
            warning.overflowMode = TextOverflowModes.Truncate;
            CreateTextAt(card, "CautionText", 79f, 56f, 237f, 23f,
                "Proceed with caution.", 12f, Orange, TextAlignmentOptions.MidlineLeft, true);
        }

        private static Button BuildActionButton(
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
            Color labelColor,
            bool danger)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            V3GradientGraphic graphic = rect.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;

            if (danger)
            {
                Image icon = CreateImage("Icon", rect,
                    RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), White);
                SetTopLeft(icon.rectTransform, 86f, 18f, 51f, 51f);
                CreateTextAt(rect, "LabelText", 141f, 5f, 365f, 75f,
                    label, 35f, labelColor, TextAlignmentOptions.Center, true);
            }
            else
            {
                RectTransform icon = CreateTopLeft("Icon", rect, 72f, 20f, 50f, 50f);
                CreateLine("SlashA", icon, new Vector2(10f, 9f), new Vector2(40f, 41f), 6f, White);
                CreateLine("SlashB", icon, new Vector2(40f, 9f), new Vector2(10f, 41f), 6f, White);
                CreateTextAt(rect, "LabelText", 126f, 5f, 275f, 75f,
                    label, 35f, labelColor, TextAlignmentOptions.Center, true);
            }
            return button;
        }

        private static void BuildMeter(
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            int active,
            int count,
            Color accent)
        {
            float gap = 4f;
            float segmentWidth = (width - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                RectTransform segment = CreatePanel("Segment_" + (i + 1), parent,
                    x + i * (segmentWidth + gap), y, segmentWidth, height,
                    i < active ? accent : new Color32(43, 49, 51, 255),
                    i < active ? accent * .62f : new Color32(14, 19, 21, 255),
                    Color.clear, 0f);
            }
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

        private static TMP_Text CreateTextAt(
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
            TMP_Text text = CreateText(name, parent, value, size, bold ? boldFont : mediumFont, alignment, color);
            SetTopLeft(text.rectTransform, x, y, width, height);
            return text;
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
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 30f);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.fontWeight = FontWeight.Regular;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(9f, size * .72f);
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

        private static RawImage CreateRawImage(string name, Transform parent, Texture texture, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void CreateLine(
            string name, Transform parent, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 delta = end - start;
            Image line = CreateSolid(name, parent, color, false);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = V3UiPrefabFactory.CreateRect(
                name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect, float insetX = 0f, float insetY = 0f)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(insetX, insetY);
            rect.offsetMax = new Vector2(-insetX, -insetY);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
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

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing POP-02 V3 sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
            if (boldFont == null || mediumFont == null || mapTexture == null)
                throw new FileNotFoundException("POP-02 V3 fonts or district map are missing.");
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject operationsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OperationsPrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("ConfirmRaidV3CaptureCamera", typeof(Camera));
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

            GameObject canvasObject = new("ConfirmRaidV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            foreach (MainMenuV3SectionLayoutView layout in canvasObject.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))
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
                Debug.Log($"[ConfirmRaidV3PrefabBuilder] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
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
                RectTransform footer,
                TMP_Text title,
                Button cancelButton,
                Button confirmButton)
            {
                Frame = frame;
                Header = header;
                Body = body;
                Footer = footer;
                Title = title;
                CancelButton = cancelButton;
                ConfirmButton = confirmButton;
            }

            public RectTransform Frame { get; }
            public RectTransform Header { get; }
            public RectTransform Body { get; }
            public RectTransform Footer { get; }
            public TMP_Text Title { get; }
            public Button CancelButton { get; }
            public Button ConfirmButton { get; }
        }
    }
}
#endif
