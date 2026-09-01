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
    /// <summary>
    /// Rebuilds the placement confirmation bar from the SCN-08 V3 lock. Chrome is
    /// procedural so the shared V3 atlases contain icons and portraits only.
    /// </summary>
    public static class BuildPlacementConfirmationBarPrefabSetupEditor
    {
        public const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab";
        public const string StatusChipSpritePath = V3UiFoundationBuilder.PanelPath;
        public const string MaterialsIconSpritePath = V3UiFoundationBuilder.MatchMaterialsIconPath;
        public const string OilIconSpritePath = V3UiFoundationBuilder.MatchOilIconPath;
        public const string FuelIconSpritePath = V3UiFoundationBuilder.MatchFuelIconPath;
        public const string BuildingPortraitPath =
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Refinery_Big_Action_512.png";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color DarkTop = new Color32(20, 32, 37, 255);
        private static readonly Color DarkBottom = new Color32(3, 11, 14, 255);
        private static readonly Color RaisedTop = new Color32(43, 57, 62, 255);
        private static readonly Color RaisedBottom = new Color32(12, 24, 29, 255);
        private static readonly Color Line = new Color32(67, 82, 85, 255);
        private static readonly Color Green = new Color32(94, 212, 25, 255);
        private static readonly Color GreenTop = new Color32(16, 142, 41, 255);
        private static readonly Color GreenBottom = new Color32(3, 67, 24, 255);
        private static readonly Color Amber = new Color32(245, 188, 0, 255);
        private static readonly Color Text = new Color32(239, 241, 235, 255);
        private static readonly Color Muted = new Color32(181, 190, 188, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/Setup Build Placement Confirmation Bar Prefab")]
        [MenuItem("Game/UI/V3/Build Placement Confirmation Bar V3")]
        public static void Setup()
        {
            BuildPlacementPanelV3PrefabBuilder.Build();
            LoadAssets();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BuildPlacementConfirmationBarView view =
                    root.GetComponent<BuildPlacementConfirmationBarView>() ??
                    root.AddComponent<BuildPlacementConfirmationBarView>();
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1672f, 941f);

                CanvasGroup group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Image legacyImage = root.GetComponent<Image>();
                if (legacyImage != null)
                    UnityEngine.Object.DestroyImmediate(legacyImage);
                V3GradientGraphic oldGradient = root.GetComponent<V3GradientGraphic>();
                if (oldGradient != null)
                    UnityEngine.Object.DestroyImmediate(oldGradient);
                BuildPlacementConfirmationResponsiveLayoutView oldResponsive =
                    root.GetComponent<BuildPlacementConfirmationResponsiveLayoutView>();
                if (oldResponsive != null)
                    UnityEngine.Object.DestroyImmediate(oldResponsive);
                ClearChildren(root.transform);

                RectTransform barPanel = CreatePanel(
                    "PlacementBarPanel", rootRect, 4f, 617f, 1664f, 310f,
                    DarkTop, DarkBottom, Line, 3f);
                barPanel.GetComponent<V3GradientGraphic>().raycastTarget = true;

                Sprite materialIcon = RequireSprite(MaterialsIconSpritePath);
                Sprite oilIcon = RequireSprite(OilIconSpritePath);
                Sprite fuelIcon = RequireSprite(FuelIconSpritePath);
                Sprite buildingPortrait = RequireSprite(BuildingPortraitPath);
                Sprite rotateIcon = RequireSprite(V3UiFoundationBuilder.ResetIconPath);

                RectTransform portraitPanel = CreatePanel(
                    "BuildingPortraitPanel", barPanel, 20f, 21f, 332f, 264f,
                    RaisedTop, DarkBottom, Line, 3f);
                RectTransform portraitClip = CreateTopLeft("PortraitClip", portraitPanel, 5f, 5f, 322f, 254f);
                portraitClip.gameObject.AddComponent<RectMask2D>();
                Image portrait = CreateImage("BuildingPortrait", portraitClip, buildingPortrait, Color.white);
                Stretch(portrait.rectTransform);
                portrait.preserveAspect = false;
                AspectRatioFitter portraitFitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
                portraitFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                portraitFitter.aspectRatio = buildingPortrait.rect.width / buildingPortrait.rect.height;
                RectTransform portraitHighlight = CreatePanel(
                    "PortraitHighlight", portraitPanel, 2f, 2f, 328f, 260f,
                    Color.clear, Color.clear, Amber, 3f);
                portraitHighlight.SetAsLastSibling();

                RectTransform detail = CreatePanel(
                    "PlacementDetailPanel", barPanel, 352f, 21f, 474f, 264f,
                    DarkTop, DarkBottom, Line, 3f);
                TMP_Text title = CreateText(
                    "Title", detail, 31f, 15f, 414f, 52f, "BUILD POWER PLANT", 31f,
                    Text, TextAlignmentOptions.MidlineLeft, true);
                CreateSolid("DividerA", detail, 31f, 69f, 414f, 3f, new Color(1f, 1f, 1f, .20f));

                CreateImageAt("MaterialsIcon", detail, materialIcon, 31f, 80f, 36f, 36f, Text);
                TMP_Text materialCost = CreateText(
                    "MaterialsCost", detail, 75f, 75f, 84f, 47f, "1,500", 24f,
                    Text, TextAlignmentOptions.MidlineLeft, true);
                CreateImageAt("OilIcon", detail, oilIcon, 169f, 80f, 36f, 36f,
                    new Color32(203, 154, 69, 255));
                TMP_Text oilCost = CreateText(
                    "OilCost", detail, 213f, 75f, 69f, 47f, "250", 24f,
                    Text, TextAlignmentOptions.MidlineLeft, true);
                CreateImageAt("FuelIcon", detail, fuelIcon, 292f, 78f, 34f, 40f,
                    new Color32(242, 68, 20, 255));
                TMP_Text fuelCost = CreateText(
                    "FuelCost", detail, 334f, 75f, 81f, 47f, "150", 24f,
                    Text, TextAlignmentOptions.MidlineLeft, true);

                CreateSolid("DividerB", detail, 31f, 125f, 414f, 3f, new Color(1f, 1f, 1f, .20f));
                CreateText("FootprintLabel", detail, 31f, 132f, 150f, 28f, "FOOTPRINT", 17f,
                    Muted, TextAlignmentOptions.MidlineLeft, false);
                TMP_Text footprint = CreateText(
                    "Footprint", detail, 31f, 157f, 130f, 35f, "3x3", 23f,
                    Amber, TextAlignmentOptions.MidlineLeft, true);
                CreateSolid("DividerC", detail, 31f, 196f, 414f, 3f, new Color(1f, 1f, 1f, .20f));
                CreateText("StatusLabel", detail, 31f, 205f, 96f, 35f, "STATUS", 17f,
                    Muted, TextAlignmentOptions.MidlineLeft, false);
                RectTransform validStatus = CreateStatusCheck(detail, 152f, 211f, Green, "ValidStatusIndicator");
                RectTransform invalidStatus = CreateInvalidStatus(detail, 152f, 211f);
                invalidStatus.gameObject.SetActive(false);
                TMP_Text status = CreateText(
                    "Status", detail, 184f, 201f, 250f, 45f, "VALID PLACEMENT", 19f,
                    Green, TextAlignmentOptions.MidlineLeft, true);

                RectTransform rotateRegion = CreatePanel(
                    "RotateRegion", barPanel, 850f, 21f, 204f, 256f,
                    DarkTop, DarkBottom, Line, 3f);
                Button rotateButton = ConfigureButton(rotateRegion);
                CreateText("RotateLabel", rotateRegion, 12f, 21f, 180f, 43f, "ROTATE", 23f,
                    Text, TextAlignmentOptions.Center, false);
                CreateImageAt("Icon", rotateRegion, rotateIcon, 54f, 91f, 96f, 96f, Text);

                Button cancelButton = CreateButton(
                    "CancelButton", barPanel, 1076f, 21f, 198f, 256f,
                    new Color32(70, 80, 83, 255), new Color32(32, 41, 44, 255), Line);
                CreateText("Label", cancelButton.transform, 8f, 12f, 182f, 232f, "CANCEL", 30f,
                    Text, TextAlignmentOptions.Center, true);

                Button confirmButton = CreateButton(
                    "ConfirmButton", barPanel, 1297f, 21f, 347f, 256f,
                    GreenTop, GreenBottom, new Color32(48, 228, 73, 255));
                CreateText("Label", confirmButton.transform, 10f, 15f, 327f, 226f,
                    "PLACE\nBUILDING", 43f, Text, TextAlignmentOptions.Center, true, false);

                BuildPlacementConfirmationResponsiveLayoutView responsive =
                    barPanel.gameObject.AddComponent<BuildPlacementConfirmationResponsiveLayoutView>();
                responsive.Configure(
                    1664f,
                    new[] { rotateRegion, cancelButton.transform as RectTransform, confirmButton.transform as RectTransform },
                    new[] { detail });

                MainMenuV3SectionLayoutView sectionLayout =
                    root.GetComponent<MainMenuV3SectionLayoutView>() ??
                    root.AddComponent<MainMenuV3SectionLayoutView>();
                sectionLayout.Configure(
                    new Vector2(1672f, 941f),
                    MainMenuV3SectionAlignment.Center,
                    Array.Empty<RectTransform>(),
                    true,
                    Array.Empty<RectTransform>(),
                    new[] { barPanel });

                TMP_Text duration = CreateHiddenText("Duration", barPanel, "00:45");
                TMP_Text instruction = CreateHiddenText(
                    "Instruction", barPanel, "DRAG TO POSITION, CONFIRM TO BUILD");

                var serialized = new SerializedObject(view);
                SetObject(serialized, "root", barPanel);
                SetObject(serialized, "titleText", title);
                SetObject(serialized, "statusText", status);
                SetObject(serialized, "costText", materialCost);
                SetObject(serialized, "durationText", duration);
                SetObject(serialized, "instructionText", instruction);
                SetObject(serialized, "cancelButton", cancelButton);
                SetObject(serialized, "rotateButton", rotateButton);
                SetObject(serialized, "confirmButton", confirmButton);
                SetObject(serialized, "oilCostText", oilCost);
                SetObject(serialized, "fuelCostText", fuelCost);
                SetObject(serialized, "footprintText", footprint);
                SetObject(serialized, "buildingPortrait", portrait);
                SetObject(serialized, "validityPanelPrefab", Require<GameObject>(BuildPlacementPanelV3PrefabBuilder.PrefabPath));
                SetObject(serialized, "validStatusIndicator", validStatus.gameObject);
                SetObject(serialized, "invalidStatusIndicator", invalidStatus.gameObject);

                // Retain the old serialized compatibility slots, but point every
                // one at shared V3 art. None of these sprites draw the V3 chrome.
                SetObject(serialized, "panelFrameSprite", RequireSprite(V3UiFoundationBuilder.PanelPath));
                SetObject(serialized, "statusChipSprite", RequireSprite(V3UiFoundationBuilder.PanelPath));
                SetObject(serialized, "secondaryButtonSprite", RequireSprite(V3UiFoundationBuilder.ButtonPath));
                SetObject(serialized, "goldActionButtonSprite", RequireSprite(V3UiFoundationBuilder.ButtonPath));
                SetObject(serialized, "squareButtonSprite", RequireSprite(V3UiFoundationBuilder.ButtonPath));
                SetObject(serialized, "instructionStripSprite", RequireSprite(V3UiFoundationBuilder.PanelPath));
                SetObject(serialized, "materialsIconSprite", materialIcon);
                SetObject(serialized, "timeIconSprite", RequireSprite(V3UiFoundationBuilder.MatchSpeedIconPath));
                SetObject(serialized, "cancelIconSprite", RequireSprite(V3UiFoundationBuilder.MatchInvalidIconPath));
                SetObject(serialized, "rotateIconSprite", rotateIcon);
                SetObject(serialized, "confirmIconSprite", RequireSprite(V3UiFoundationBuilder.MatchSelectIconPath));
                SetObject(serialized, "infoIconSprite", RequireSprite(V3UiFoundationBuilder.MatchInfoIconPath));
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
            Debug.Log("[BuildPlacementConfirmationBarV3Builder] result=Passed layout=lower-right " +
                      "gradients=procedural borders=3 portrait=existing sharedIcons=match-v3 validity=linked");
        }

        [MenuItem("Game/UI/V3/Validate Placement Confirmation Bar V3")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing placement confirmation prefab: {PrefabPath}");
            BuildPlacementConfirmationBarView view = prefab.GetComponent<BuildPlacementConfirmationBarView>();
            if (view == null || view.Root == null || view.TitleText == null || view.StatusText == null ||
                view.MaterialsCostText == null || view.OilCostText == null || view.FuelCostText == null ||
                view.FootprintText == null || view.BuildingPortrait == null || view.CancelButton == null ||
                view.RotateButton == null || view.ConfirmButton == null || view.ValidityPanelPrefab == null)
            {
                throw new MissingReferenceException("Placement confirmation V3 runtime bindings are incomplete.");
            }

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 7)
                throw new InvalidOperationException($"Expected at least 7 procedural V3 gradients, found {gradients.Length}.");
            if (AssetDatabase.GetAssetPath(view.BuildingPortrait.sprite) != BuildingPortraitPath)
                throw new InvalidOperationException("Placement bar must reuse the existing building portrait.");
            if (AssetDatabase.GetAssetPath(view.MaterialsIconSprite) != MaterialsIconSpritePath)
                throw new InvalidOperationException("Placement bar materials icon must come from the shared V3 match icon atlas.");
            BuildPlacementConfirmationResponsiveLayoutView responsive =
                prefab.GetComponentInChildren<BuildPlacementConfirmationResponsiveLayoutView>(true);
            if (responsive == null || responsive.ReferenceWidth != 1664f || responsive.RightAnchoredTargets.Length != 3)
                throw new InvalidOperationException("Placement confirmation V3 ultrawide layout is incomplete.");
            MainMenuV3SectionLayoutView sectionLayout = prefab.GetComponent<MainMenuV3SectionLayoutView>();
            if (sectionLayout == null || !sectionLayout.ExpandToCanvasWidth ||
                sectionLayout.ReferenceResolution != new Vector2(1672f, 941f))
            {
                throw new InvalidOperationException("Placement confirmation V3 must use the shared Match HUD responsive section frame.");
            }

            Debug.Log($"[BuildPlacementConfirmationBarV3Validation] result=Passed gradients={gradients.Length} borders=3 actions=3");
        }

        private static void LoadAssets()
        {
            boldFont = Require<TMP_FontAsset>(BoldFontPath);
            mediumFont = Require<TMP_FontAsset>(MediumFontPath);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static RectTransform CreatePanel(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            graphic.raycastTarget = false;
            return rect;
        }

        private static Button CreateButton(
            string name, Transform parent, float x, float y, float width, float height,
            Color top, Color bottom, Color border)
        {
            RectTransform rect = CreatePanel(name, parent, x, y, width, height, top, bottom, border, 3f);
            return ConfigureButton(rect);
        }

        private static Button ConfigureButton(RectTransform rect)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<V3GradientGraphic>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1f);
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            return button;
        }

        private static RectTransform CreateStatusCheck(Transform parent, float x, float y, Color color, string name)
        {
            RectTransform ring = CreateTopLeft(name, parent, x, y, 24f, 24f);
            V3RingGraphic ringGraphic = ring.gameObject.AddComponent<V3RingGraphic>();
            ringGraphic.Configure(color, 3f, 32);
            ringGraphic.raycastTarget = false;
            CreateLine("CheckA", ring, 4f, 12f, 9f, 17f, 3f, color);
            CreateLine("CheckB", ring, 9f, 17f, 20f, 5f, 3f, color);
            return ring;
        }

        private static RectTransform CreateInvalidStatus(Transform parent, float x, float y)
        {
            RectTransform ring = CreateTopLeft("InvalidStatusIndicator", parent, x, y, 24f, 24f);
            V3RingGraphic ringGraphic = ring.gameObject.AddComponent<V3RingGraphic>();
            Color red = new Color32(255, 61, 43, 255);
            ringGraphic.Configure(red, 3f, 32);
            ringGraphic.raycastTarget = false;
            CreateSolid("Stem", ring, 10.5f, 4f, 3f, 10f, red);
            CreateSolid("Dot", ring, 10.5f, 17f, 3f, 3f, red);
            return ring;
        }

        private static TMP_Text CreateHiddenText(string name, Transform parent, string value)
        {
            TMP_Text text = CreateText(name, parent, 0f, 0f, 1f, 1f, value, 1f,
                Color.clear, TextAlignmentOptions.Center, false);
            text.gameObject.SetActive(false);
            return text;
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
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = noWrap ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
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

        private static void CreateSolid(
            string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            SetTopLeft(image.rectTransform, x, y, width, height);
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

        private static Sprite RequireSprite(string path) => Require<Sprite>(path);

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing placement confirmation V3 asset: {path}");
            return asset;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(typeof(BuildPlacementConfirmationBarView).Name, propertyName);
            property.objectReferenceValue = value;
        }
    }
}
#endif
