#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ThreatAlertV3PrefabBuilder
    {
        internal const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/ThreatAlertPopup.prefab";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string VehiclePreviewPath =
            "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_VehicleSquad_512.png";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(24, 34, 38, 252);
        private static readonly Color DarkBottom = new Color32(2, 7, 9, 255);
        private static readonly Color RaisedTop = new Color32(35, 46, 50, 252);
        private static readonly Color Line = new Color32(126, 143, 148, 255);
        private static readonly Color RedTop = new Color32(205, 58, 26, 255);
        private static readonly Color RedBottom = new Color32(78, 15, 8, 255);
        private static readonly Color Orange = new Color32(255, 66, 24, 255);
        private static readonly Color Amber = new Color32(255, 188, 50, 255);
        private static readonly Color Cyan = new Color32(0, 198, 235, 255);
        private static readonly Color Green = new Color32(70, 205, 54, 255);
        private static readonly Color White = new Color32(244, 246, 242, 255);
        private static readonly Color Muted = new Color32(175, 186, 187, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Rebuild POP-01 Threat Alert")]
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

                RectTransform composition = CreateRect(
                    "V3Composition",
                    root.transform,
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f),
                    Reference,
                    Vector2.zero);

                Image scrim = CreateSolid("Scrim", composition, new Color(0f, 0f, 0f, .44f), true);
                SetTopLeft(scrim.rectTransform, 0f, 0f, Reference.x, Reference.y);

                RectTransform routeWorldOverlay = BuildRouteWorldOverlay(composition);
                RectTransform alertSurface = BuildAlertSurface(composition, out Button alertClose, out Button jumpButton,
                    out RectTransform body, out RectTransform buttonRow, out TMP_Text title);
                RectTransform routeSurface = BuildRoutePreviewSurface(composition, out Button routeClose);
                RectTransform routeStrip = BuildRoutePreviewStrip(composition);

                routeWorldOverlay.gameObject.SetActive(false);
                routeSurface.gameObject.SetActive(false);
                routeStrip.gameObject.SetActive(false);

                MainMenuV3SectionLayoutView responsive = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
                responsive.Configure(
                    Reference,
                    MainMenuV3SectionAlignment.Center,
                    shouldExpandToCanvasWidth: true,
                    targetsAnchoredToCenter: new[] { alertSurface, routeSurface, routeStrip, routeWorldOverlay },
                    targetsExpandedAcrossWidth: new[] { scrim.rectTransform });

                UIPopupFrameView popup = root.GetComponent<UIPopupFrameView>() ?? root.AddComponent<UIPopupFrameView>();
                popup.Configure(
                    scrim.gameObject,
                    alertSurface.gameObject,
                    alertSurface.Find("Header")?.gameObject,
                    title,
                    alertClose,
                    body,
                    buttonRow);

                ThreatAlertV3PopupView state = root.GetComponent<ThreatAlertV3PopupView>() ??
                    root.AddComponent<ThreatAlertV3PopupView>();
                state.Configure(
                    scrim.gameObject,
                    alertSurface.gameObject,
                    routeSurface.gameObject,
                    routeStrip.gameObject,
                    routeWorldOverlay.gameObject,
                    jumpButton,
                    alertClose,
                    routeClose);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[ThreatAlertV3PrefabBuilder] result=Passed states=2 gradients=procedural borders=3 icons=shared-v3 vehicle-art=reused");
        }

        [MenuItem("Game/UI/V3/Validate POP-01 Threat Alert")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing POP-01 prefab: {PrefabPath}");

            ThreatAlertV3PopupView state = prefab.GetComponent<ThreatAlertV3PopupView>();
            UIPopupFrameView frame = prefab.GetComponent<UIPopupFrameView>();
            if (state == null || frame == null || state.AlertSurface == null ||
                state.RoutePreviewSurface == null || state.RoutePreviewStrip == null ||
                state.RouteWorldOverlay == null || state.JumpToThreatButton == null)
            {
                throw new MissingReferenceException("POP-01 V3 runtime state bindings are incomplete.");
            }

            if (prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length < 18)
                throw new InvalidOperationException("POP-01 V3 must retain visible procedural gradients.");
            if (prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).Length != 1)
                throw new InvalidOperationException("POP-01 V3 must serialize one responsive reference composition.");

            Image preview = FindDeepChild(prefab.transform, "VehiclePreview")?.GetComponent<Image>();
            AspectRatioFitter fitter = preview != null ? preview.GetComponent<AspectRatioFitter>() : null;
            if (preview == null || preview.sprite == null || fitter == null ||
                fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
            {
                throw new InvalidOperationException("POP-01 vehicle preview must reuse an aspect-preserved art plate.");
            }
        }

        private static RectTransform BuildAlertSurface(
            Transform parent,
            out Button closeButton,
            out Button jumpButton,
            out RectTransform body,
            out RectTransform buttonRow,
            out TMP_Text title)
        {
            RectTransform surface = CreateTopLeft("AlertSurface", parent, 466f, 133f, 740f, 610f);
            AddGradient(surface, DarkTop, DarkBottom, Line, 3f);

            RectTransform header = CreateTopLeft("Header", surface, 0f, 0f, 740f, 97f);
            AddGradient(header, new Color32(21, 29, 32, 255), new Color32(5, 10, 12, 255), Line, 3f);
            Image warning = CreateImage("WarningIcon", header,
                RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath), Orange);
            SetTopLeft(warning.rectTransform, 39f, 19f, 59f, 59f);
            title = CreateText("TitleText", header, "INCOMING THREAT DETECTED", 33f, boldFont,
                TextAlignmentOptions.MidlineLeft, Orange);
            SetTopLeft(title.rectTransform, 121f, 10f, 510f, 77f);
            closeButton = CreateCloseButton("CloseButton", header, 666f, 17f, 58f, 64f);

            body = CreateTopLeft("BodyRoot", surface, 0f, 97f, 740f, 443f);
            TMP_Text headline = CreateText("HeadlineText", body, "Enemy Convoy Approaching", 35f, boldFont,
                TextAlignmentOptions.Center, White);
            SetTopLeft(headline.rectTransform, 24f, 19f, 692f, 62f);

            RectTransform info = CreateTopLeft("InfoColumn", body, 18f, 99f, 296f, 222f);
            BuildInfoRow(info, "EtaRow", 0f, "ETA", "02:15", InfoIcon.Clock, Orange, Orange);
            BuildInfoRow(info, "RouteRow", 74f, "Route:", "North Bridge", InfoIcon.Route, Orange, White);
            BuildInfoRow(info, "StrengthRow", 148f, "Est. Strength", "High", InfoIcon.Strength, Orange, Orange);

            RectTransform previewFrame = CreateTopLeft("ThreatImagePanel", body, 332f, 99f, 390f, 222f);
            AddGradient(previewFrame, RaisedTop, DarkBottom, Line, 3f);
            previewFrame.gameObject.AddComponent<RectMask2D>();
            Image preview = CreateImage("VehiclePreview", previewFrame, RequireSprite(VehiclePreviewPath), Color.white);
            Stretch(preview.rectTransform, 4f, 4f);
            preview.preserveAspect = true;
            AspectRatioFitter fitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = preview.sprite.rect.width / preview.sprite.rect.height;
            Image veil = CreateSolid("ThreatTint", previewFrame, new Color(0.01f, 0.015f, 0.018f, .30f), false);
            Stretch(veil.rectTransform, 4f, 4f);

            BuildStrengthMeter(body, 18f, 336f, 704f);

            buttonRow = CreateTopLeft("ButtonRow", surface, 18f, 507f, 704f, 84f);
            jumpButton = CreateGradientButton(
                "JumpToThreatButton", buttonRow, 0f, 0f, 704f, 84f,
                RedTop, RedBottom, Orange, 3f);
            Image jumpIcon = CreateImage("Icon", jumpButton.transform,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), Amber);
            SetTopLeft(jumpIcon.rectTransform, 120f, 14f, 55f, 55f);
            TMP_Text jumpLabel = CreateText("LabelText", jumpButton.transform, "JUMP TO THREAT", 34f, boldFont,
                TextAlignmentOptions.Center, Amber);
            SetTopLeft(jumpLabel.rectTransform, 172f, 5f, 420f, 72f);
            return surface;
        }

        private static RectTransform BuildRoutePreviewSurface(Transform parent, out Button closeButton)
        {
            RectTransform surface = CreateTopLeft("RoutePreviewSurface", parent, 409f, 87f, 520f, 335f);
            AddGradient(surface, DarkTop, DarkBottom, Line, 3f);

            RectTransform header = CreateTopLeft("Header", surface, 0f, 0f, 520f, 66f);
            AddGradient(header, new Color32(21, 29, 32, 255), new Color32(5, 10, 12, 255), Line, 3f);
            Image warning = CreateImage("WarningIcon", header,
                RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath), Orange);
            SetTopLeft(warning.rectTransform, 17f, 11f, 44f, 44f);
            TMP_Text title = CreateText("Title", header, "INCOMING THREAT DETECTED", 22f, boldFont,
                TextAlignmentOptions.MidlineLeft, Orange);
            SetTopLeft(title.rectTransform, 71f, 7f, 386f, 52f);
            closeButton = CreateCloseButton("CloseButton", header, 466f, 9f, 44f, 48f);

            TMP_Text headline = CreateText("Headline", surface, "Enemy Convoy Approaching", 27f, boldFont,
                TextAlignmentOptions.Center, White);
            SetTopLeft(headline.rectTransform, 16f, 73f, 488f, 47f);

            RectTransform info = CreateTopLeft("InfoColumn", surface, 16f, 121f, 316f, 143f);
            BuildCompactInfoRow(info, "EtaRow", 0f, "ETA", "02:15", InfoIcon.Clock);
            BuildCompactInfoRow(info, "RouteRow", 48f, "Route:", "North Bridge", InfoIcon.Route);
            BuildCompactInfoRow(info, "StrengthRow", 96f, "Est. Strength", "High", InfoIcon.Strength);
            BuildStrengthMeter(surface, 16f, 269f, 488f);
            return surface;
        }

        private static RectTransform BuildRoutePreviewStrip(Transform parent)
        {
            RectTransform strip = CreateTopLeft("RoutePreviewStrip", parent, 409f, 430f, 368f, 120f);
            AddGradient(strip, new Color32(24, 34, 38, 252), DarkBottom, Line, 3f);
            TMP_Text title = CreateText("Title", strip, "ROUTE PREVIEW", 17f, boldFont,
                TextAlignmentOptions.MidlineLeft, White);
            SetTopLeft(title.rectTransform, 13f, 7f, 210f, 27f);
            TMP_Text detail = CreateText("Detail", strip,
                "route.enemy_convoy_01  |  ETA 02:15  |  Strength: High", 11f, mediumFont,
                TextAlignmentOptions.MidlineLeft, Muted);
            SetTopLeft(detail.rectTransform, 13f, 31f, 342f, 25f);

            for (int i = 0; i < 3; i++)
            {
                Image marker = CreateImage("HostileMarker" + i, strip,
                    RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath), Orange);
                SetTopLeft(marker.rectTransform, 17f + i * 76f, 63f, 40f, 40f);
                if (i < 2)
                {
                    CreateLine("Connector" + i, strip,
                        new Vector2(56f + i * 76f, 83f), new Vector2(87f + i * 76f, 83f), 3f, Orange);
                    BuildChevron(strip, 76f + i * 76f, 71f, 22f, Orange);
                }
            }
            for (int i = 0; i < 5; i++)
                CreateSolidTopLeft("RouteDash" + i, strip, 247f + i * 15f, 81f, 9f, 3f, Orange);
            Image destination = CreateImage("Destination", strip,
                RequireSprite(V3UiFoundationBuilder.MatchFriendlyMarkerIconPath), Green);
            SetTopLeft(destination.rectTransform, 327f, 61f, 30f, 43f);
            return strip;
        }

        private static RectTransform BuildRouteWorldOverlay(Transform parent)
        {
            RectTransform overlay = CreateTopLeft("RouteWorldOverlay", parent, 765f, 102f, 470f, 570f);
            Vector2[] route =
            {
                new(52f, 485f), new(120f, 399f), new(198f, 338f),
                new(257f, 246f), new(326f, 169f), new(401f, 88f)
            };
            for (int i = 0; i < route.Length - 1; i++)
            {
                CreateLine("RouteLine" + i, overlay, route[i], route[i + 1], 8f,
                    new Color(1f, .22f, .04f, .72f));
            }

            for (int i = 0; i < 4; i++)
            {
                Image hostile = CreateImage("WorldHostile" + i, overlay,
                    RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath), Orange);
                Vector2 p = route[i + 1];
                SetTopLeft(hostile.rectTransform, p.x - 24f, p.y - 30f, 48f, 60f);
            }

            Image friendly = CreateImage("WorldFriendly", overlay,
                RequireSprite(V3UiFoundationBuilder.MatchFriendlyMarkerIconPath), Green);
            SetTopLeft(friendly.rectTransform, route[0].x - 24f, route[0].y - 30f, 48f, 60f);
            Image destination = CreateImage("WorldDestination", overlay,
                RequireSprite(V3UiFoundationBuilder.MatchJumpIconPath), Amber);
            SetTopLeft(destination.rectTransform, route[0].x + 75f, route[0].y - 15f, 52f, 52f);
            return overlay;
        }

        private static void BuildInfoRow(
            Transform parent,
            string name,
            float y,
            string label,
            string value,
            InfoIcon iconKind,
            Color iconColor,
            Color valueColor)
        {
            RectTransform row = CreateTopLeft(name, parent, 0f, y, 296f, 74f);
            AddGradient(row, RaisedTop, DarkBottom, Line, 3f);
            BuildInfoIcon(row, iconKind, 16f, 16f, 42f, iconColor);
            TMP_Text labelText = CreateText("LabelText", row, label, 21f, mediumFont,
                TextAlignmentOptions.MidlineLeft, new Color32(170, 221, 237, 255));
            SetTopLeft(labelText.rectTransform, 74f, 7f, 112f, 60f);
            float valueFontSize = iconKind == InfoIcon.Route ? 18f : 22f;
            TMP_Text valueText = CreateText("ValueText", row, value, valueFontSize, boldFont,
                TextAlignmentOptions.MidlineRight, valueColor);
            SetTopLeft(valueText.rectTransform, 178f, 7f, 102f, 60f);
        }

        private static void BuildCompactInfoRow(
            Transform parent,
            string name,
            float y,
            string label,
            string value,
            InfoIcon iconKind)
        {
            RectTransform row = CreateTopLeft(name, parent, 0f, y, 316f, 48f);
            AddGradient(row, RaisedTop, DarkBottom, Line, 3f);
            BuildInfoIcon(row, iconKind, 10f, 8f, 32f, Orange);
            TMP_Text labelText = CreateText("LabelText", row, label, 15f, mediumFont,
                TextAlignmentOptions.MidlineLeft, new Color32(170, 221, 237, 255));
            SetTopLeft(labelText.rectTransform, 52f, 4f, 128f, 40f);
            TMP_Text valueText = CreateText("ValueText", row, value, 17f, boldFont,
                TextAlignmentOptions.MidlineRight, value == "High" || value == "02:15" ? Orange : White);
            SetTopLeft(valueText.rectTransform, 174f, 4f, 129f, 40f);
        }

        private static void BuildInfoIcon(
            Transform parent,
            InfoIcon iconKind,
            float x,
            float y,
            float size,
            Color color)
        {
            if (iconKind == InfoIcon.Route)
            {
                Image icon = CreateImage("Icon", parent,
                    RequireSprite(V3UiFoundationBuilder.MatchHostileMarkerIconPath), color);
                SetTopLeft(icon.rectTransform, x, y, size, size);
                return;
            }
            if (iconKind == InfoIcon.Strength)
            {
                Image icon = CreateImage("Icon", parent,
                    RequireSprite(V3UiFoundationBuilder.MatchArmorIconPath), color);
                SetTopLeft(icon.rectTransform, x, y, size, size);
                return;
            }

            RectTransform clock = CreateTopLeft("Icon", parent, x, y, size, size);
            V3RingGraphic ring = clock.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 3f, 48);
            CreateLine("Minute", clock, new Vector2(size * .5f, size * .5f),
                new Vector2(size * .5f, size * .20f), 3f, color);
            CreateLine("Hour", clock, new Vector2(size * .5f, size * .5f),
                new Vector2(size * .72f, size * .63f), 3f, color);
        }

        private static void BuildStrengthMeter(Transform parent, float x, float y, float width)
        {
            RectTransform meter = CreateTopLeft("StrengthMeter", parent, x, y, width, 64f);
            AddGradient(meter, new Color32(11, 18, 20, 254), DarkBottom, Line, 3f);
            TMP_Text label = CreateText("Label", meter, "ESTIMATED STRENGTH", 15f, boldFont,
                TextAlignmentOptions.MidlineLeft, Orange);
            SetTopLeft(label.rectTransform, 14f, 3f, width - 66f, 22f);
            Image armor = CreateImage("ArmorIcon", meter,
                RequireSprite(V3UiFoundationBuilder.MatchArmorIconPath), Orange);
            SetTopLeft(armor.rectTransform, width - 49f, 11f, 36f, 43f);

            float segmentWidth = (width - 94f) / 10f;
            for (int i = 0; i < 10; i++)
            {
                RectTransform segment = CreateTopLeft("Segment_" + (i + 1), meter,
                    14f + i * segmentWidth, 31f, segmentWidth - 5f, 20f);
                AddGradient(
                    segment,
                    i < 7 ? new Color32(255, 80, 54, 255) : new Color32(35, 44, 47, 255),
                    i < 7 ? new Color32(185, 34, 19, 255) : new Color32(11, 18, 20, 255),
                    Color.clear,
                    0f);
            }
        }

        private static Button CreateCloseButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height)
        {
            Button button = CreateGradientButton(name, parent, x, y, width, height,
                RaisedTop, DarkBottom, Line, 3f);
            RectTransform icon = CreateTopLeft("Icon", button.transform, 0f, 0f, width, height);
            CreateLine("SlashA", icon, new Vector2(width * .30f, height * .28f),
                new Vector2(width * .70f, height * .72f), 5f, White);
            CreateLine("SlashB", icon, new Vector2(width * .70f, height * .28f),
                new Vector2(width * .30f, height * .72f), 5f, White);
            return button;
        }

        private static Button CreateGradientButton(
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
            V3GradientGraphic graphic = AddGradient(rect, top, bottom, border, borderWidth);
            graphic.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static V3GradientGraphic AddGradient(
            RectTransform rect,
            Color top,
            Color bottom,
            Color border,
            float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            graphic.raycastTarget = false;
            return graphic;
        }

        private static void BuildChevron(Transform parent, float x, float y, float size, Color color)
        {
            CreateLine("ChevronTop", parent, new Vector2(x, y), new Vector2(x + size * .45f, y + size * .5f), 3f, color);
            CreateLine("ChevronBottom", parent, new Vector2(x + size * .45f, y + size * .5f), new Vector2(x, y + size), 3f, color);
        }

        private static void CreateLine(
            string name,
            Transform parent,
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color)
        {
            Vector2 delta = end - start;
            Image line = CreateSolidTopLeft(name, parent, 0f, 0f, delta.magnitude, thickness, color);
            RectTransform rect = line.rectTransform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Image CreateSolidTopLeft(
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

        private static Image CreateSolid(string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 100f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 100f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
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
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 30f), Vector2.zero);
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

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Vector2 size,
            Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, min, max, size, position);

        private static RectTransform CreateTopLeft(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
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
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing POP-01 V3 sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null || mediumFont == null)
                throw new FileNotFoundException("POP-01 V3 fonts are missing.");
        }

        private enum InfoIcon : byte
        {
            Clock,
            Route,
            Strength
        }
    }
}
#endif
