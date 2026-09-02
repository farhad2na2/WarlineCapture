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
    public static class EndOfDayReportV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab";
        private const string MapPath =
            "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(18, 29, 33, 248);
        private static readonly Color DarkBottom = new Color32(3, 9, 11, 252);
        private static readonly Color Line = new Color32(83, 99, 103, 255);
        private static readonly Color Amber = new Color32(246, 174, 23, 255);
        private static readonly Color Cyan = new Color32(18, 160, 224, 255);
        private static readonly Color Green = new Color32(111, 190, 49, 255);
        private static readonly Color Red = new Color32(235, 58, 31, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Texture2D mapTexture;

        [MenuItem("Game/UI/V3/Rebuild End Of Day Report V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = new("EndOfDayReportPopup", typeof(RectTransform));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                UIPopupFrameView popup = root.AddComponent<UIPopupFrameView>();
                EndOfDayReportPopupView reportView = root.AddComponent<EndOfDayReportPopupView>();
                RectTransform wideBackdropRect = CreateRect("WideMapBackdrop", root.transform);
                Stretch(wideBackdropRect);
                RawImage wideBackdrop = wideBackdropRect.gameObject.AddComponent<RawImage>();
                wideBackdrop.texture = mapTexture;
                wideBackdrop.color = new Color32(94, 77, 52, 255);
                wideBackdrop.raycastTarget = false;
                AspectRatioFitter wideFitter = wideBackdropRect.gameObject.AddComponent<AspectRatioFitter>();
                wideFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                wideFitter.aspectRatio = mapTexture.width / (float)mapTexture.height;
                Image wideShade = CreateImage("WideMapShade", root.transform, null, new Color(0f, 0f, 0f, .48f));
                Stretch(wideShade.rectTransform);
                RectTransform composition = CreateTopLeft("V3Composition", root.transform, 0f, 0f, Reference.x, Reference.y);
                MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                layout.Configure(Reference, MainMenuV3SectionAlignment.Center);

                HeaderBindings header = BuildHeader(composition);
                RectTransform mapViewport = BuildMap(composition);
                RectTransform body = BuildReportCards(composition);
                FooterBindings footer = BuildFooter(composition);
                popup.Configure(
                    null,
                    composition.gameObject,
                    header.Header.gameObject,
                    header.Title,
                    null,
                    body,
                    footer.ButtonRow);
                reportView.Configure(popup, footer.ViewOperations, footer.SaveContinue);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[EndOfDayReportV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 map=aspect-preserved actions=2");
        }

        [MenuItem("Game/UI/V3/Capture End Of Day Report V3 Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-end-of-day-v3-16x9.png", 1920, 1080);
            Capture("/private/tmp/warline-end-of-day-v3-20x9.png", 4800, 2160);
        }

        [MenuItem("Game/UI/V3/Validate End Of Day Report V3 Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EndOfDayReportPopupView reportView = prefab != null
                ? prefab.GetComponent<EndOfDayReportPopupView>()
                : null;
            if (prefab == null || prefab.GetComponent<UIPopupFrameView>() == null || reportView == null)
                throw new MissingReferenceException("End Of Day V3 popup or runtime frame binding is missing.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || layout.ReferenceResolution != Reference ||
                layout.Alignment != MainMenuV3SectionAlignment.Center)
                throw new InvalidOperationException("End Of Day V3 must use the centered 1672x941 composition.");
            RawImage map = Find(prefab.transform, "DistrictMapImage")?.GetComponent<RawImage>();
            AspectRatioFitter fitter = map != null ? map.GetComponent<AspectRatioFitter>() : null;
            if (map == null || map.texture == null || fitter == null ||
                fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("End Of Day V3 map must fill without stretching.");
            if (reportView.ViewOperationsButton == null || reportView.SaveContinueButton == null)
                throw new MissingReferenceException("End Of Day V3 requires both footer actions.");
            RequirePointerTarget(reportView.ViewOperationsButton, "View Operations");
            RequirePointerTarget(reportView.SaveContinueButton, "Save & Continue");
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                EndOfDayReportPopupView instanceView = instance.GetComponent<EndOfDayReportPopupView>();
                int viewOperationsCount = 0;
                int saveContinueCount = 0;
                instanceView.BindActions(() => viewOperationsCount++, () => saveContinueCount++);
                instanceView.ViewOperationsButton.onClick.Invoke();
                instanceView.SaveContinueButton.onClick.Invoke();
                if (viewOperationsCount != 1 || saveContinueCount != 1)
                    throw new InvalidOperationException("End Of Day footer actions must dispatch exactly once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 15)
                throw new InvalidOperationException($"End Of Day V3 requires layered procedural gradients; found {gradients}.");
            Debug.Log($"[EndOfDayReportV3PrefabBuilder] validation=Passed gradients={gradients} map=aspect-preserved actions=2 pointerTargets=Passed dispatch=one-each runtime=popup-frame");
        }

        private static void RequirePointerTarget(Button button, string label)
        {
            Graphic graphic = button != null ? button.targetGraphic : null;
            if (graphic == null || !graphic.raycastTarget || !button.interactable || !button.gameObject.activeSelf)
                throw new InvalidOperationException($"End Of Day {label} action is not a live pointer target.");
        }

        private static HeaderBindings BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateTopLeft("Header", parent, 0f, 0f, Reference.x, 110f);
            RectTransform logo = CreatePanel("HeaderLogoPanel", header, 10f, 10f, 361f, 98f, DarkTop, DarkBottom, Line, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            RectTransform titlePanel = CreatePanel("TitlePanel", header, 379f, 10f, 740f, 98f, DarkTop, DarkBottom, Line, 3f);
            TMP_Text title = CreateText(titlePanel, "TitleText", 42f, 8f, 560f, 78f, "END OF DAY REPORT", 43f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(titlePanel, "DayText", 607f, 13f, 116f, 70f, "DAY 17", 27f, Amber, TextAlignmentOptions.Center, true);

            BuildResourcePanel(header, "CreditsResource", 1128f, "CREDITS", "24,750", catalog.CreditsIcon, Amber);
            BuildResourcePanel(header, "CommandResource", 1391f, "COMMAND", "8,430", catalog.CommandIcon, Cyan);
            return new HeaderBindings(header, title);
        }

        private static void BuildResourcePanel(
            Transform parent, string name, float x, string label, string value, Sprite sprite, Color accent)
        {
            RectTransform panel = CreatePanel(name, parent, x, 10f, 254f, 98f, DarkTop, DarkBottom, Line, 3f);
            Image icon = CreateImage("IconImage", panel, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 19f, 17f, 61f, 61f);
            CreateText(panel, "LabelText", 96f, 13f, 142f, 29f, label, 20f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "ValueText", 96f, 42f, 142f, 45f, value, 31f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
        }

        private static RectTransform BuildMap(RectTransform parent)
        {
            RectTransform viewport = CreatePanel("MapViewport", parent, 10f, 117f, 1652f, 670f, Color.clear, Color.clear, Line, 3f);
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform mapRect = CreateRect("DistrictMapImage", viewport);
            Stretch(mapRect);
            RawImage map = mapRect.gameObject.AddComponent<RawImage>();
            map.texture = mapTexture;
            map.color = new Color32(151, 123, 78, 255);
            map.raycastTarget = false;
            AspectRatioFitter fitter = mapRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = mapTexture.width / (float)mapTexture.height;

            BuildZone(viewport, "NorthgateZone", new[]
            {
                new Vector2(5f, 65f), new Vector2(65f, 28f), new Vector2(174f, 42f),
                new Vector2(246f, 129f), new Vector2(212f, 238f), new Vector2(58f, 258f)
            }, new Color(0.04f, 0.38f, 0.67f, 0.18f), Cyan, 98f, 112f,
                RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath));
            BuildZone(viewport, "OldMarketZone", new[]
            {
                new Vector2(26f, 363f), new Vector2(96f, 313f), new Vector2(196f, 330f),
                new Vector2(262f, 408f), new Vector2(233f, 550f), new Vector2(82f, 574f)
            }, new Color(0.18f, 0.48f, 0.08f, 0.18f), Green, 113f, 444f,
                RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath));
            BuildZone(viewport, "ForwardPostZone", new[]
            {
                new Vector2(358f, 376f), new Vector2(469f, 307f), new Vector2(601f, 344f),
                new Vector2(648f, 491f), new Vector2(543f, 585f), new Vector2(398f, 544f)
            }, new Color(0.57f, 0.38f, 0.04f, 0.18f), Amber, 475f, 431f,
                RequireSprite(V3UiFoundationBuilder.MissionStarIconPath));
            BuildZone(viewport, "SouthQuarterZone", new[]
            {
                new Vector2(552f, 493f), new Vector2(665f, 438f), new Vector2(780f, 481f),
                new Vector2(817f, 633f), new Vector2(667f, 669f), new Vector2(571f, 602f)
            }, new Color(0.56f, 0.09f, 0.04f, 0.18f), Red, 650f, 555f,
                RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath));
            return viewport;
        }

        private static void BuildZone(
            Transform parent, string name, Vector2[] points, Color fill, Color accent,
            float iconX, float iconY, Sprite iconSprite)
        {
            RectTransform zone = CreateTopLeft(name, parent, 0f, 0f, 1652f, 670f);
            zone.gameObject.AddComponent<V3PolygonGraphic>().Configure(points, fill);
            for (int index = 0; index < points.Length; index++)
                CreateChartLine(
                    parent,
                    name + "Edge_" + index,
                    points[index],
                    points[(index + 1) % points.Length],
                    accent);
            RectTransform node = CreatePanel(name + "Node", parent, iconX, iconY, 53f, 53f, DarkTop, DarkBottom, accent, 3f);
            Image icon = CreateImage("Icon", node, iconSprite, accent);
            SetTopLeft(icon.rectTransform, 9f, 9f, 35f, 35f);
        }

        private static RectTransform BuildReportCards(RectTransform parent)
        {
            RectTransform body = CreateTopLeft("BodyRoot", parent, 0f, 0f, Reference.x, Reference.y);
            BuildStatCard(body, "RegionStabilityPanel", 168f, "REGION STABILITY", "+5", "73%", Cyan,
                RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath), 4, 6);
            BuildStatCard(body, "TrustStabilityPanel", 468f, "CIVILIAN TRUST", "+8", "70%", Green,
                RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath), 4, 6);
            BuildStatCard(body, "EnemyActivityPanel", 775f, "ENEMY ACTIVITY", "HIGH", "THREAT LEVEL", Red,
                RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), 7, 9);
            BuildPressureChart(body);
            BuildOperationSummary(body);
            BuildDistrictList(body);
            BuildCivilianCard(body);
            return body;
        }

        private static void BuildStatCard(
            Transform parent, string name, float x, string label, string delta, string metric,
            Color accent, Sprite iconSprite, int filled, int segments)
        {
            RectTransform card = CreatePanel(name, parent, x, 166f, name == "RegionStabilityPanel" ? 290f : 296f, 196f, DarkTop, DarkBottom, Line, 3f);
            RectTransform badge = CreatePanel("Badge", card, 20f, 24f, 61f, 83f, new Color(accent.r * .25f, accent.g * .25f, accent.b * .25f, 1f), DarkBottom, accent, 2f);
            Image icon = CreateImage("IconImage", badge, iconSprite, accent);
            SetTopLeft(icon.rectTransform, 8f, 17f, 45f, 45f);
            CreateText(card, "LabelText", 101f, 17f, 179f, 36f, label, 17f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(card, "DeltaText", 101f, 53f, 174f, 58f, delta, delta == "HIGH" ? 42f : 50f, accent, TextAlignmentOptions.MidlineLeft, true);
            CreateText(card, "ValueText", 22f, 129f, 111f, 45f, metric, metric == "THREAT LEVEL" ? 13f : 27f, accent, TextAlignmentOptions.MidlineLeft, true);
            float startX = 112f;
            float available = card.rect.width - startX - 18f;
            float gap = 4f;
            float width = (available - gap * (segments - 1)) / segments;
            for (int index = 0; index < segments; index++)
                CreateSolid($"Meter_{index + 1}", card, startX + index * (width + gap), 154f, width, 14f,
                    index < filled ? accent : new Color32(21, 29, 30, 255), index < filled ? accent : Line, 1f);
        }

        private static void BuildPressureChart(Transform parent)
        {
            RectTransform panel = CreatePanel("DailyPressurePanel", parent, 1082f, 166f, 540f, 244f, DarkTop, DarkBottom, Line, 3f);
            CreateText(panel, "TitleText", 24f, 13f, 270f, 37f, "DAILY PRESSURE", 25f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            RectTransform chart = CreateTopLeft("ThreatTrendChart", panel, 79f, 64f, 424f, 143f);
            for (int index = 0; index <= 4; index++)
                CreateSolid($"GridH_{index}", chart, 0f, index * 32f, 424f, 1f, new Color32(62, 72, 74, 130));
            for (int index = 0; index <= 6; index++)
                CreateSolid($"GridV_{index}", chart, index * 68f, 0f, 1f, 128f, new Color32(62, 72, 74, 100));
            float[] values = { 119f, 111f, 107f, 91f, 72f, 74f, 57f, 60f, 55f, 35f, 38f, 14f };
            float step = 424f / (values.Length - 1);
            for (int index = 0; index < values.Length - 1; index++)
                CreateChartLine(chart, $"Trend_{index}", new Vector2(index * step, values[index]), new Vector2((index + 1) * step, values[index + 1]), Red);
            for (int index = 0; index < values.Length; index++)
            {
                RectTransform point = CreateTopLeft($"Point_{index}", chart, index * step - 5f, values[index] - 5f, 10f, 10f);
                V3RingGraphic ring = point.gameObject.AddComponent<V3RingGraphic>();
                ring.Configure(Red, 4f, 20);
            }
            CreateText(panel, "High", 18f, 63f, 55f, 25f, "HIGH", 15f, Red, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "Medium", 18f, 112f, 62f, 25f, "MEDIUM", 13f, Amber, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "Low", 18f, 162f, 55f, 25f, "LOW", 15f, Green, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "Days", 79f, 207f, 424f, 25f, "DAY 11     DAY 12     DAY 13     DAY 14     DAY 15     DAY 16     DAY 17", 13f, theme.TextMuted, TextAlignmentOptions.Center, false);
        }

        private static void CreateChartLine(Transform parent, string name, Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = new(end.x - start.x, -(end.y - start.y));
            float length = delta.magnitude;
            Image line = CreateImage(name, parent, null, color);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(length, 3f);
            rect.anchoredPosition = new Vector2(start.x, -start.y);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void BuildOperationSummary(Transform parent)
        {
            RectTransform panel = CreatePanel("OperationSummaryPanel", parent, 714f, 445f, 290f, 298f, DarkTop, DarkBottom, Line, 3f);
            Image icon = CreateImage("Icon", panel, RequireSprite(V3UiFoundationBuilder.CampaignChaptersIconPath), Cyan);
            SetTopLeft(icon.rectTransform, 20f, 18f, 38f, 38f);
            CreateText(panel, "Title", 68f, 12f, 200f, 49f, "OPERATION SUMMARY", 16f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            BuildSummaryRow(panel, 77f, RequireSprite(V3UiFoundationBuilder.OperationsMapPinIconPath), "DISTRICTS SECURED", "2", Cyan);
            BuildSummaryRow(panel, 139f, RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath), "CIVILIANS PROTECTED", "156", Cyan);
            BuildSummaryRow(panel, 201f, RequireSprite(V3UiFoundationBuilder.OperationsWarningIconPath), "THREATS UNRESOLVED", "3", Red);
        }

        private static void BuildSummaryRow(Transform parent, float y, Sprite sprite, string label, string value, Color accent)
        {
            CreateSeparator(parent, y);
            Image icon = CreateImage("Icon", parent, sprite, accent);
            SetTopLeft(icon.rectTransform, 20f, y + 13f, 31f, 31f);
            CreateText(parent, "Label", 63f, y + 8f, 174f, 43f, label, 13f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(parent, "Value", 237f, y + 8f, 39f, 43f, value, 18f, accent, TextAlignmentOptions.MidlineRight, true);
        }

        private static void BuildDistrictList(Transform parent)
        {
            RectTransform panel = CreatePanel("DistrictsSecuredPanel", parent, 1010f, 445f, 300f, 298f, DarkTop, DarkBottom, Line, 3f);
            V3StarGraphic star = CreateTopLeft("Icon", panel, 19f, 16f, 36f, 36f).gameObject.AddComponent<V3StarGraphic>();
            star.Configure(Amber, false, DarkTop);
            CreateText(panel, "Title", 68f, 10f, 215f, 49f, "DISTRICTS SECURED", 17f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            string[] districts = { "NORTHGATE", "OLD MARKET", "FORWARD POST", "EASTRIDGE", "SOUTH QUARTER" };
            string[] states = { "SECURED", "SECURED", "SECURED", "CONTESTED", "HOSTILE" };
            for (int index = 0; index < districts.Length; index++)
            {
                float y = 67f + index * 45f;
                CreateSeparator(panel, y);
                CreateText(panel, "District", 21f, y + 4f, 168f, 37f, districts[index], 17f, Amber, TextAlignmentOptions.MidlineLeft, true);
                Color stateColor = index < 3 ? Green : index == 3 ? Amber : Red;
                CreateText(panel, "State", 185f, y + 4f, 98f, 37f, states[index], 13f, stateColor, TextAlignmentOptions.MidlineRight, true);
            }
        }

        private static void BuildCivilianCard(Transform parent)
        {
            RectTransform panel = CreatePanel("CiviliansProtectedPanel", parent, 1316f, 445f, 306f, 298f, DarkTop, DarkBottom, Line, 3f);
            Image icon = CreateImage("Icon", panel, RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath), Green);
            SetTopLeft(icon.rectTransform, 20f, 17f, 35f, 35f);
            CreateText(panel, "Title", 67f, 11f, 220f, 47f, "CIVILIANS PROTECTED", 16f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "ValueText", 0f, 74f, 306f, 86f, "156", 71f, Green, TextAlignmentOptions.Center, true);
            CreateText(panel, "LabelText", 0f, 158f, 306f, 33f, "CIVILIANS SAFE TODAY", 18f, theme.TextMuted, TextAlignmentOptions.Center, true);
            CreateSeparator(panel, 209f);
            Image totalIcon = CreateImage("TotalIcon", panel, RequireSprite(V3UiFoundationBuilder.MissionCivilianIconPath), Green);
            SetTopLeft(totalIcon.rectTransform, 44f, 229f, 53f, 53f);
            CreateText(panel, "TotalLabel", 118f, 219f, 145f, 31f, "TOTAL SAFE", 17f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "TotalValue", 118f, 248f, 145f, 39f, "2,874", 31f, Green, TextAlignmentOptions.MidlineLeft, true);
        }

        private static FooterBindings BuildFooter(RectTransform parent)
        {
            RectTransform row = CreateTopLeft("ButtonRow", parent, 0f, 0f, Reference.x, Reference.y);
            Button view = BuildAction(row, "ViewOperationsButton", 10f, 795f, 707f, 120f,
                new Color32(10, 72, 95, 255), new Color32(2, 25, 38, 255), Cyan,
                RequireSprite(V3UiFoundationBuilder.CampaignChaptersIconPath), "VIEW OPERATIONS", 39f);
            Button save = BuildAction(row, "SaveContinueButton", 727f, 795f, 935f, 120f,
                new Color32(70, 143, 36, 255), new Color32(17, 64, 18, 255), Green,
                RequireSprite(V3UiFoundationBuilder.CampaignLaunchIconPath), "SAVE & CONTINUE", 49f);
            return new FooterBindings(row, view, save);
        }

        private static Button BuildAction(
            Transform parent, string name, float x, float y, float width, float height,
            Color top, Color bottom, Color border, Sprite iconSprite, string label, float fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            V3GradientGraphic graphic = rect.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            Image icon = CreateImage("Icon", rect, iconSprite, theme.TextPrimary);
            SetTopLeft(icon.rectTransform, width == 707f ? 111f : 168f, 27f, 67f, 67f);
            CreateText(rect, "LabelText", width == 707f ? 202f : 274f, 13f, width == 707f ? 445f : 600f, 94f,
                label, fontSize, theme.TextPrimary, TextAlignmentOptions.Center, true);
            return button;
        }

        private static void Capture(string outputPath, int width, int height)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("EndOfDayV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new("EndOfDayV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
                Debug.Log($"[EndOfDayReportV3PrefabBuilder] capture=Passed size={width}x{height} path={outputPath} scene={scene.name}");
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
            mapTexture = RequireAsset<Texture2D>(MapPath);
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

        private static void CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color, Color? border = null, float borderWidth = 0f)
        {
            if (border.HasValue)
            {
                CreatePanel(name, parent, x, y, width, height, color, color, border.Value, borderWidth);
                return;
            }
            Image image = CreateImage(name, parent, null, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
        }

        private static void CreateSeparator(Transform parent, float y)
        {
            float width = (parent as RectTransform).rect.width;
            CreateSolid("Separator", parent, 12f, y, width - 24f, 1f, Line);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing End Of Day V3 asset: {path}");
            return asset;
        }

        private static Sprite RequireSprite(string path) => RequireAsset<Sprite>(path);

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = Find(root.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent) =>
            V3UiPrefabFactory.CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
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

        private readonly struct HeaderBindings
        {
            public readonly RectTransform Header;
            public readonly TMP_Text Title;
            public HeaderBindings(RectTransform header, TMP_Text title)
            {
                Header = header;
                Title = title;
            }
        }

        private readonly struct FooterBindings
        {
            public readonly RectTransform ButtonRow;
            public readonly Button ViewOperations;
            public readonly Button SaveContinue;
            public FooterBindings(RectTransform buttonRow, Button viewOperations, Button saveContinue)
            {
                ButtonRow = buttonRow;
                ViewOperations = viewOperations;
                SaveContinue = saveContinue;
            }
        }
    }
}
#endif
