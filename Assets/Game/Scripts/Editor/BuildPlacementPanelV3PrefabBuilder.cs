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
    public static class BuildPlacementPanelV3PrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab";
        public const string MinimapPath =
            "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV01/scn08_minimap_content.png";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color DarkTop = new Color32(27, 38, 43, 252);
        private static readonly Color DarkBottom = new Color32(2, 8, 10, 254);
        private static readonly Color RaisedTop = new Color32(43, 56, 61, 252);
        private static readonly Color RaisedBottom = new Color32(10, 20, 24, 254);
        private static readonly Color Border = new Color32(112, 128, 132, 255);
        private static readonly Color Text = new Color32(239, 242, 237, 255);
        private static readonly Color Muted = new Color32(177, 187, 185, 255);
        private static readonly Color Green = new Color32(92, 224, 28, 255);
        private static readonly Color Red = new Color32(255, 62, 42, 255);
        private static readonly Color Cyan = new Color32(0, 199, 238, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Rebuild Build Placement Validity Panel")]
        public static void Build()
        {
            boldFont = Require<TMP_FontAsset>(BoldFontPath);
            mediumFont = Require<TMP_FontAsset>(MediumFontPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                RemoveLegacyComponents(root);
                ClearChildren(root.transform);

                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1672f, 941f);
                CanvasGroup group = root.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                BuildPlacementValidityPanelView view = root.AddComponent<BuildPlacementValidityPanelView>();

                RectTransform validity = CreatePanel(
                    "FootprintValidityPanel", rootRect, 1318f, 8f, 344f, 356f,
                    DarkTop, DarkBottom, Border, 3f);
                CreateText("Title", validity, 14f, 8f, 265f, 43f, "FOOTPRINT VALIDITY", 22f,
                    Text, TextAlignmentOptions.MidlineLeft, true);
                RectTransform help = CreatePanel(
                    "Help", validity, 293f, 9f, 41f, 39f,
                    RaisedTop, RaisedBottom, Border, 3f);
                CreateText("Label", help, 2f, 0f, 37f, 37f, "?", 24f,
                    Muted, TextAlignmentOptions.Center, true);
                CreateDivider(validity, 0f, 54f, 344f);

                CreateValidityRow(validity, "FootprintClear", 55f, 45f,
                    "Footprint clear", null, true);
                CreateValidityRow(validity, "RoadAccess", 100f, 60f,
                    "Road access", "Road connection available", true);
                CreateValidityRow(validity, "PowerConnection", 160f, 60f,
                    "Power connection", "Grid connection in range", true);

                RectTransform obstruction = CreateTopLeft("ObstructionDetected", validity, 0f, 220f, 344f, 83f);
                CreateDivider(obstruction, 0f, 0f, 344f);
                CreateImageAt("Warning", obstruction, Require<Sprite>(V3UiFoundationBuilder.MatchInvalidIconPath),
                    15f, 16f, 26f, 26f, Red);
                CreateText("Title", obstruction, 51f, 10f, 276f, 29f, "OBSTRUCTION DETECTED", 18f,
                    Red, TextAlignmentOptions.MidlineLeft, true);
                CreateText("Reason", obstruction, 51f, 37f, 276f, 22f, "Footprint overlaps blocked area", 14f,
                    Muted, TextAlignmentOptions.MidlineLeft, false);
                CreateText("Coordinates", obstruction, 51f, 57f, 276f, 22f, "Road edge at (X: 34, Y: 27)", 14f,
                    Muted, TextAlignmentOptions.MidlineLeft, false);

                CreateDivider(validity, 0f, 303f, 344f);
                CreateText("StatusLabel", validity, 14f, 310f, 100f, 37f, "STATUS", 17f,
                    Text, TextAlignmentOptions.MidlineLeft, false);
                TMP_Text status = CreateText("Status", validity, 127f, 307f, 203f, 40f,
                    "INVALID PLACEMENT", 17f, Red, TextAlignmentOptions.MidlineRight, true);

                RectTransform minimap = CreatePanel(
                    "PlacementMinimapPanel", rootRect, 1318f, 374f, 344f, 240f,
                    DarkTop, DarkBottom, Border, 3f);
                RectTransform clip = CreateTopLeft("MapClip", minimap, 7f, 7f, 330f, 226f);
                clip.gameObject.AddComponent<RectMask2D>();
                Image map = CreateImage("Map", clip, Require<Sprite>(MinimapPath), Color.white);
                Stretch(map.rectTransform);
                map.preserveAspect = false;
                AspectRatioFitter mapFitter = map.gameObject.AddComponent<AspectRatioFitter>();
                mapFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                mapFitter.aspectRatio = 1f;
                CreateMapMarker(clip, "EnemyNorthWest", 55f, 25f,
                    V3UiFoundationBuilder.MatchHostileMarkerIconPath, Red, 30f);
                CreateMapMarker(clip, "FriendlyNorth", 174f, 22f,
                    V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, 24f);
                CreateMapMarker(clip, "FriendlyEast", 229f, 41f,
                    V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, 24f);
                CreateMapMarker(clip, "EnemyCenter", 80f, 105f,
                    V3UiFoundationBuilder.MatchHostileMarkerIconPath, Red, 30f);
                CreateMapMarker(clip, "EnemyEast", 214f, 95f,
                    V3UiFoundationBuilder.MatchHostileMarkerIconPath, Red, 30f);
                CreateMapMarker(clip, "FriendlyFarEast", 278f, 86f,
                    V3UiFoundationBuilder.MatchFriendlyMarkerIconPath, Green, 24f);
                RectTransform cameraBounds = CreatePanel(
                    "CameraBounds", clip, 59f, 56f, 172f, 127f,
                    Color.clear, Color.clear, Cyan, 3f);
                cameraBounds.SetAsLastSibling();

                MainMenuV3SectionLayoutView layout = root.AddComponent<MainMenuV3SectionLayoutView>();
                layout.Configure(
                    new Vector2(1672f, 941f),
                    MainMenuV3SectionAlignment.Center,
                    new[] { validity, minimap },
                    true);

                SerializedObject serialized = new(view);
                SetObject(serialized, "canvasGroup", group);
                SetObject(serialized, "validitySurface", validity);
                SetObject(serialized, "minimapSurface", minimap);
                SetObject(serialized, "statusText", status);
                SetObject(serialized, "obstructionRow", obstruction.gameObject);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[BuildPlacementPanelV3Builder] result=Passed layout=top-right gradients=procedural borders=3 minimap=reused-shared state=invalid");
        }

        [MenuItem("Game/UI/V3/Validate Build Placement Validity Panel")]
        public static void Validate()
        {
            GameObject prefab = Require<GameObject>(PrefabPath);
            BuildPlacementValidityPanelView view = prefab.GetComponent<BuildPlacementValidityPanelView>();
            if (view == null || view.ValiditySurface == null || view.MinimapSurface == null ||
                view.StatusText == null || view.ObstructionRow == null)
            {
                throw new MissingReferenceException("Build Placement validity V3 bindings are incomplete.");
            }

            MainMenuV3SectionLayoutView layout = prefab.GetComponent<MainMenuV3SectionLayoutView>();
            if (layout == null || !layout.ExpandToCanvasWidth || layout.RightAnchoredTargets.Length != 2)
                throw new InvalidOperationException("Build Placement validity panel is not right-pinned for ultrawide layouts.");

            Image map = view.MinimapSurface.Find("MapClip/Map")?.GetComponent<Image>();
            if (map == null || AssetDatabase.GetAssetPath(map.sprite) != MinimapPath ||
                map.GetComponent<AspectRatioFitter>() == null)
            {
                throw new InvalidOperationException("Build Placement validity minimap must reuse the aspect-preserved V3 map.");
            }

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 4)
                throw new InvalidOperationException("Build Placement validity panel requires procedural gradient surfaces.");
            for (int i = 0; i < gradients.Length; i++)
            {
                SerializedObject serialized = new(gradients[i]);
                float width = serialized.FindProperty("borderWidth").floatValue;
                if (width > 0f && !Mathf.Approximately(width, 3f))
                    throw new InvalidOperationException($"Validity border {gradients[i].name} is {width}; expected 3.");
            }

            Debug.Log($"[BuildPlacementPanelV3Validation] result=Passed gradients={gradients.Length} borders=3 map=shared-aspect-preserved");
        }

        private static void CreateValidityRow(
            Transform parent, string name, float y, float height, string title, string detail, bool valid)
        {
            RectTransform row = CreateTopLeft(name, parent, 0f, y, 344f, height);
            CreateDivider(row, 0f, 0f, 344f);
            CreateText("Title", row, 14f, 6f, 265f, detail == null ? height - 10f : 27f,
                title, 16f, Text, TextAlignmentOptions.MidlineLeft, false);
            if (!string.IsNullOrWhiteSpace(detail))
            {
                CreateText("Detail", row, 14f, 29f, 273f, 24f,
                    detail, 14f, Muted, TextAlignmentOptions.MidlineLeft, false);
            }
            CreateStatusCheck(row, 305f, (height - 24f) * .5f, valid ? Green : Red);
        }

        private static void CreateStatusCheck(Transform parent, float x, float y, Color color)
        {
            RectTransform ring = CreateTopLeft("Valid", parent, x, y, 24f, 24f);
            V3RingGraphic ringGraphic = ring.gameObject.AddComponent<V3RingGraphic>();
            ringGraphic.Configure(color, 3f, 32);
            ringGraphic.raycastTarget = false;
            CreateLine("CheckA", ring, 4f, 12f, 9f, 17f, 3f, color);
            CreateLine("CheckB", ring, 9f, 17f, 20f, 5f, 3f, color);
        }

        private static void CreateMapMarker(
            Transform parent, string name, float x, float y, string path, Color color, float size)
        {
            CreateImageAt(name, parent, Require<Sprite>(path), x, y, size, size, color);
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

        private static void CreateDivider(Transform parent, float x, float y, float width)
        {
            RectTransform rect = CreateTopLeft("Divider", parent, x, y, width, 3f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Border;
            image.raycastTarget = false;
        }

        private static TMP_Text CreateText(
            string name, Transform parent, float x, float y, float width, float height,
            string value, float size, Color color, TextAlignmentOptions alignment, bool bold)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImageAt(
            string name, Transform parent, Sprite sprite, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, sprite, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 100f, 100f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateLine(
            string name, Transform parent, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            Vector2 start = new(x1, y1);
            Vector2 end = new(x2, y2);
            Vector2 delta = end - start;
            RectTransform line = CreateTopLeft(name, parent, 0f, 0f, delta.magnitude, thickness);
            line.pivot = new Vector2(.5f, .5f);
            Vector2 center = (start + end) * .5f;
            line.anchoredPosition = new Vector2(center.x, -center.y);
            line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetTopLeft(rect, x, y, width, height);
            return rect;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
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

        private static void RemoveLegacyComponents(GameObject root)
        {
            Component[] components = root.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component != null && component is not RectTransform)
                    UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing Build Placement V3 asset: {path}");
            return asset;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(typeof(BuildPlacementValidityPanelView).Name, propertyName);
            property.objectReferenceValue = value;
        }
    }
}
#endif
