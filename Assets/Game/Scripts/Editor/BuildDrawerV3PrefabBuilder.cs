#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class BuildDrawerV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";

        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(28, 38, 43, 252);
        private static readonly Color DarkBottom = new Color32(3, 8, 10, 254);
        private static readonly Color RaisedTop = new Color32(53, 65, 70, 252);
        private static readonly Color Line = new Color32(112, 127, 131, 255);
        private static readonly Color Amber = new Color32(255, 194, 17, 255);
        private static readonly Color Orange = new Color32(240, 72, 29, 255);
        private static readonly Color Cyan = new Color32(54, 174, 215, 255);
        private static readonly Color Green = new Color32(79, 199, 73, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Sprite timeIcon;
        private static Sprite lockIcon;
        private static Sprite footprintIcon;

        [MenuItem("Game/UI/V3/Rebuild Build Drawer V3 Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            PresenterAssets presenterAssets = ReadPresenterAssets();

            GameObject root = new("SCN09_BuildDrawerPopup", typeof(RectTransform));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                BuildDrawerView view = root.AddComponent<BuildDrawerView>();
                BuildDrawerCatalogRuntimeView presenter = root.AddComponent<BuildDrawerCatalogRuntimeView>();
                root.AddComponent<BuildDrawerHudOcclusionView>();
                UIPopupCloseView closeView = root.AddComponent<UIPopupCloseView>();
                UIPopupCloseButtonView closeButtonView = root.AddComponent<UIPopupCloseButtonView>();
                UIPopupMotionView.Ensure(root);

                RectTransform drawerRoot = CreateTopLeft("BuildDrawerRoot", root.transform, 0f, 0f, Reference.x, Reference.y);
                Stretch(drawerRoot);
                Image scrim = drawerRoot.gameObject.AddComponent<Image>();
                scrim.color = new Color(0f, .012f, .018f, .58f);
                scrim.raycastTarget = true;

                RectTransform frame = CreateTopLeft("DrawerFrame", drawerRoot, 0f, 0f, Reference.x, Reference.y);
                frame.gameObject.AddComponent<MainMenuV3SectionLayoutView>()
                    .Configure(Reference, MainMenuV3SectionAlignment.Center);

                CreatePanel("OuterFrame", frame, 112f, 16f, 1448f, 909f, DarkTop, DarkBottom, Line, 3f);
                BuildHeader(frame, out Button closeButton);
                TabBindings tabs = BuildTabs(frame);
                CatalogBindings catalogBindings = BuildCatalog(frame);
                DetailBindings detail = BuildDetail(frame);
                QueueBindings queue = BuildQueue(frame);
                FooterBindings footer = BuildFooter(frame);

                BindView(view, drawerRoot, closeButton, tabs, catalogBindings, detail, queue, footer);
                BindPresenter(presenter, view, presenterAssets);
                BindClose(root, closeView, closeButtonView, closeButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[BuildDrawerV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 art=catalog-bound atlases=shared");
        }

        [MenuItem("Game/UI/V3/Capture Build Drawer V3 Review")]
        public static void CaptureReview()
        {
            Build();
            Capture("/private/tmp/warline-build-drawer-v3-16x9.png", 1920, 1080, false);
            Capture("/private/tmp/warline-build-drawer-v3-20x9.png", 4800, 2160, false);
            Capture("/private/tmp/warline-build-drawer-disabled-v3-16x9.png", 1920, 1080, true);
            Capture("/private/tmp/warline-build-drawer-disabled-v3-20x9.png", 4800, 2160, true);
        }

        [MenuItem("Game/UI/V3/Validate Build Drawer V3 Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Build Drawer prefab: {PrefabPath}");

            BuildDrawerView view = prefab.GetComponent<BuildDrawerView>();
            BuildDrawerCatalogRuntimeView presenter = prefab.GetComponent<BuildDrawerCatalogRuntimeView>();
            BuildDrawerHudOcclusionView occlusion = prefab.GetComponent<BuildDrawerHudOcclusionView>();
            UIPopupCloseView close = prefab.GetComponent<UIPopupCloseView>();
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (view == null || presenter == null || occlusion == null || close == null)
                throw new MissingReferenceException("Build Drawer runtime bindings are incomplete.");
            if (layout == null || layout.ReferenceResolution != Reference)
                throw new InvalidOperationException("Build Drawer must use the centered 1672x941 composition.");
            if (prefab.transform.Find("BuildDrawerRoot/DrawerFrame/CloseButton")?.GetComponent<Button>() == null)
                throw new MissingReferenceException("Build Drawer close button path changed.");
            if (view.Tabs == null || view.Tabs.Length != 4 || view.ItemTemplate == null ||
                view.ItemContentRoot == null || view.PrimaryActionButton == null)
                throw new MissingReferenceException("Build Drawer tabs, catalog, or primary action binding is missing.");
            if (view.ItemTemplate.ThumbnailImage == null ||
                view.ItemTemplate.ThumbnailImage.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("Catalog portraits must use aspect-fill without stretching.");
            if (view.SelectedItemFrameSprite != catalog.FocusOverlay)
                throw new InvalidOperationException("Build Drawer selection must reuse the shared V3 focus asset.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 18)
                throw new InvalidOperationException($"Build Drawer requires procedural directional gradients; found {gradients}.");
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(V3UiFoundationBuilder.MatchIconAtlasPath) == null)
                throw new FileNotFoundException("Missing shared Match V3 icon atlas.");
            Debug.Log($"[BuildDrawerV3PrefabBuilder] validation=Passed tabs=4 gradients={gradients} borders=3 images=runtime-catalog aspect=preserved");
        }

        [MenuItem("Game/UI/V3/Inspect Build Drawer V3 Hierarchy")]
        public static void InspectHierarchy()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var report = new StringBuilder(65536);
                report.AppendLine("[BuildDrawerV3PrefabBuilder] hierarchy-begin");
                AppendHierarchy(report, root.transform, root.transform, 0);
                report.AppendLine("[BuildDrawerV3PrefabBuilder] hierarchy-end");
                Debug.Log(report.ToString());
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void LoadAssets()
        {
            boldFont = Require<TMP_FontAsset>(BoldFontPath);
            mediumFont = Require<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            timeIcon = Require<Sprite>(V3UiFoundationBuilder.OperationsTimeIconPath);
            lockIcon = Require<Sprite>(V3UiFoundationBuilder.CommanderLockIconPath);
            footprintIcon = Require<Sprite>(V3UiFoundationBuilder.FirstLaunchTargetIconPath);
        }

        private static PresenterAssets ReadPresenterAssets()
        {
            GameObject current = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            BuildDrawerCatalogRuntimeView presenter = current != null
                ? current.GetComponent<BuildDrawerCatalogRuntimeView>()
                : null;
            if (presenter == null)
                return default;
            var serialized = new SerializedObject(presenter);
            return new PresenterAssets(
                serialized.FindProperty("unitPrefabRegistryConfig")?.objectReferenceValue as ScriptableObject,
                serialized.FindProperty("buildingPlacementConfig")?.objectReferenceValue as ScriptableObject);
        }

        private static void BuildHeader(RectTransform frame, out Button closeButton)
        {
            RectTransform header = CreatePanel("Header", frame, 122f, 26f, 1428f, 84f, DarkTop, DarkBottom, Line, 3f);
            RectTransform buildIcon = CreateTopLeft("BuildIcon", header, 18f, 12f, 61f, 59f);
            BuildCraneMark(buildIcon, Amber);
            CreateText(header, "Title", 91f, 5f, 370f, 72f, "BUILD", 54f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            BuildResource(header, "Materials", 494f, "MATERIALS", "12,450", catalog.MaterialsIcon, theme.TextPrimary);
            BuildResource(header, "Oil", 698f, "OIL", "3,280", catalog.OilIcon, theme.Amber);
            BuildResource(header, "Fuel", 902f, "FUEL", "6,750", catalog.FuelIcon, theme.OrangeRed);

            RectTransform closeRect = CreatePanel("CloseButton", frame, 1464f, 30f, 72f, 72f, RaisedTop, DarkBottom, Line, 3f);
            closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeRect.GetComponent<V3GradientGraphic>();
            closeButton.targetGraphic.raycastTarget = true;
            BuildX(closeRect, theme.TextPrimary);
        }

        private static void BuildResource(RectTransform parent, string name, float x, string label, string value, Sprite sprite, Color accent)
        {
            RectTransform slot = CreateTopLeft(name + "Resource", parent, x, 4f, 196f, 76f);
            CreateSolid("Divider", slot, 0f, 5f, 2f, 66f, Line);
            Image icon = CreateImage("Icon", slot, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 14f, 12f, 50f, 50f);
            CreateText(slot, "Label", 72f, 4f, 116f, 28f, label, 17f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, false);
            CreateText(slot, "Value", 72f, 31f, 116f, 38f, value, 26f, accent, TextAlignmentOptions.MidlineLeft, true);
        }

        private static TabBindings BuildTabs(RectTransform frame)
        {
            RectTransform left = CreateTopLeft("LeftPanel", frame, 122f, 120f, 216f, 690f);
            RectTransform tabsRoot = CreateTopLeft("Tabs", left, 0f, 0f, 216f, 690f);
            string[] names = { "BuildingsTab", "VehiclesTab", "AircraftsTab", "SoldiersTab" };
            string[] labels = { "BUILDINGS", "VEHICLES", "AIRCRAFTS", "SOLDIERS" };
            Sprite[] icons =
            {
                Require<Sprite>(V3UiFoundationBuilder.CampaignBarracksIconPath),
                Require<Sprite>(V3UiFoundationBuilder.CommanderVehicleIconPath),
                Require<Sprite>(V3UiFoundationBuilder.MissionAirIconPath),
                Require<Sprite>(V3UiFoundationBuilder.CommanderRosterIconPath)
            };
            var buttons = new Button[4];
            var frames = new Image[4];
            var labelTexts = new TMP_Text[4];
            var disabled = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                bool selected = i == 0;
                RectTransform tab = CreatePanel(
                    names[i], tabsRoot, 0f, i * 173f, 216f, 164f,
                    selected ? new Color32(98, 72, 12, 255) : DarkTop,
                    selected ? new Color32(28, 19, 3, 255) : DarkBottom,
                    selected ? Amber : Line,
                    3f);
                buttons[i] = tab.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = tab.GetComponent<V3GradientGraphic>();
                buttons[i].targetGraphic.raycastTarget = true;
                Image icon = CreateImage("Icon", tab, icons[i], selected ? Amber : theme.TextMuted);
                SetTopLeft(icon.rectTransform, 18f, 42f, 58f, 70f);
                labelTexts[i] = CreateText(tab, "Label", 86f, 34f, 122f, 86f, labels[i], 25f,
                    selected ? theme.TextPrimary : theme.TextMuted, TextAlignmentOptions.MidlineLeft, true);
                frames[i] = CreateImage("Frame", tab, selected ? catalog.FocusOverlay : catalog.Button, Color.clear);
                Stretch(frames[i].rectTransform);
                frames[i].raycastTarget = false;
                disabled[i] = CreateSolid("DisabledOverlay", tab, 3f, 3f, 210f, 158f, new Color(0f, 0f, 0f, .52f)).gameObject;
                disabled[i].SetActive(false);
            }

            return new TabBindings(buttons, frames, labelTexts, disabled);
        }

        private static CatalogBindings BuildCatalog(RectTransform frame)
        {
            RectTransform panel = CreateTopLeft("CatalogPanel", frame, 348f, 120f, 824f, 690f);
            RectTransform scroll = CreateTopLeft("Scroll View", panel, 0f, 0f, 824f, 690f);
            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            RectTransform viewport = CreateTopLeft("Viewport", scroll, 0f, 0f, 824f, 690f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = CreateTopLeft("Content", viewport, 0f, 0f, 824f, 690f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(407f, 340f);
            grid.spacing = new Vector2(10f, 10f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            Sprite[] cards =
            {
                LoadConfigSprite("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset", "portraitCardSprite"),
                LoadConfigSprite("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GuardTower_Config.asset", "portraitCardSprite"),
                LoadConfigSprite("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Ammunition_Depot_Config.asset", "portraitCardSprite"),
                LoadConfigSprite("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_OilRefinery_Config.asset", "portraitCardSprite")
            };
            string[] names = { "BARRACKS", "GUARD TOWER", "FABRICATION DEPOT", "OIL REFINERY" };
            string[] materialCosts = { "900", "420", "600", "2,000" };
            string[] fuelCosts = { "200", "90", "120", "—" };
            string[] times = { "00:30", "00:18", "00:25", "00:30" };

            CardBindings template = BuildCard(content, "ItemView", names[0], cards[0], materialCosts[0], fuelCosts[0], times[0], true, true);
            for (int i = 1; i < cards.Length; i++)
                BuildCard(content, "PreviewCard" + i, names[i], cards[i], materialCosts[i], fuelCosts[i], times[i], false, false);

            return new CatalogBindings(content, template);
        }

        private static CardBindings BuildCard(
            RectTransform parent,
            string name,
            string title,
            Sprite cardSprite,
            string materialCost,
            string fuelCost,
            string time,
            bool selected,
            bool runtimeTemplate)
        {
            RectTransform card = CreatePanel(
                name, parent, 0f, 0f, 407f, 340f,
                selected ? new Color32(70, 56, 17, 255) : RaisedTop,
                selected ? new Color32(19, 16, 5, 255) : DarkBottom,
                selected ? Amber : Line,
                3f);
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 407f;
            layout.preferredHeight = 340f;
            Button button = runtimeTemplate ? card.gameObject.AddComponent<Button>() : null;
            if (button != null)
            {
                button.targetGraphic = card.GetComponent<V3GradientGraphic>();
                button.targetGraphic.raycastTarget = true;
            }
            CreateText(card, "Title", 12f, 4f, 383f, 39f, title, 27f, theme.TextPrimary, TextAlignmentOptions.Center, true);

            RectTransform artClip = CreateTopLeft("ArtClip", card, 3f, 45f, 401f, 244f);
            artClip.gameObject.AddComponent<RectMask2D>();
            Image art = CreateImage("Thumb", artClip, cardSprite, Color.white);
            Stretch(art.rectTransform);
            art.preserveAspect = false;
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (cardSprite != null)
                fitter.aspectRatio = cardSprite.rect.width / Mathf.Max(1f, cardSprite.rect.height);

            RectTransform cost = CreateTopLeft("CostPanel", card, 3f, 289f, 401f, 48f);
            CreateSolid("CostShade", cost, 0f, 0f, 401f, 48f, new Color(0f, .02f, .025f, .9f));
            TMP_Text materials = BuildTinyCost(cost, "MaterialsTinyCost", 8f, catalog.MaterialsIcon, materialCost, theme.TextPrimary);
            TMP_Text fuel = BuildTinyCost(cost, "FuelTinyCost", 139f, catalog.FuelIcon, fuelCost, theme.OrangeRed);
            TMP_Text timeTextValue = BuildTinyCost(cost, "TimeTinyCost", 270f, timeIcon, time, theme.TextPrimary);

            TMP_Text role = CreateText(card, "Role", 12f, 45f, 160f, 24f, "MILITARY STRUCTURE", 14f, Amber, TextAlignmentOptions.MidlineLeft, true);
            role.gameObject.SetActive(false);
            TMP_Text description = CreateText(card, "Description", 12f, 73f, 360f, 40f, string.Empty, 14f, theme.TextMuted, TextAlignmentOptions.TopLeft, false);
            description.gameObject.SetActive(false);
            TMP_Text requirements = CreateText(card, "Requirements", 12f, 118f, 360f, 28f, string.Empty, 13f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            requirements.gameObject.SetActive(false);

            // The serialized template must start from the shared normal sprite so
            // BuildDrawerItemView can restore it after another card is selected.
            // The visible initial selection is drawn by the procedural gradient.
            Image frame = CreateImage("Frame", card, catalog.Button, Color.clear);
            Stretch(frame.rectTransform);
            frame.raycastTarget = false;

            RectTransform disabledOverlay = CreateTopLeft("DisabledOverlay", card, 3f, 45f, 401f, 292f);
            Image disabledShade = disabledOverlay.gameObject.AddComponent<Image>();
            disabledShade.color = new Color(.025f, .04f, .045f, .78f);
            disabledShade.raycastTarget = false;
            Image lockMark = CreateImage("LockIcon", disabledOverlay, lockIcon, theme.TextMuted);
            SetTopLeft(lockMark.rectTransform, 122f, 99f, 42f, 42f);
            CreateText(disabledOverlay, "Reason", 169f, 93f, 210f, 54f, "LOCKED UNTIL\nFORWARD HQ", 18f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            disabledOverlay.gameObject.SetActive(false);

            BuildDrawerItemView view = null;
            if (runtimeTemplate)
            {
                view = card.gameObject.AddComponent<BuildDrawerItemView>();
                var serialized = new SerializedObject(view);
                Set(serialized, "selectionButton", button);
                Set(serialized, "frameImage", frame);
                Set(serialized, "thumbnailImage", art);
                Set(serialized, "nameText", FindText(card, "Title"));
                Set(serialized, "roleText", role);
                Set(serialized, "descriptionText", description);
                Set(serialized, "materialsCostText", materials);
                Set(serialized, "fuelCostText", fuel);
                Set(serialized, "timeText", timeTextValue);
                Set(serialized, "requirementsText", requirements);
                Set(serialized, "disabledOverlay", disabledOverlay.gameObject);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return new CardBindings(view, art, frame);
        }

        private static TMP_Text BuildTinyCost(RectTransform parent, string name, float x, Sprite sprite, string value, Color color)
        {
            RectTransform group = CreateTopLeft(name, parent, x, 4f, 126f, 40f);
            Image icon = CreateImage("Icon", group, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 0f, 4f, 32f, 32f);
            return CreateText(group, "Value", 39f, 0f, 84f, 40f, value, 19f, color, TextAlignmentOptions.MidlineLeft, true);
        }

        private static DetailBindings BuildDetail(RectTransform frame)
        {
            RectTransform detail = CreatePanel("DetailPanel", frame, 1182f, 120f, 368f, 455f, DarkTop, DarkBottom, Line, 3f);
            TMP_Text name = CreateText(detail, "Name", 14f, 7f, 340f, 39f, "BARRACKS", 29f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text role = CreateText(detail, "Role", 14f, 43f, 340f, 28f, "MILITARY STRUCTURE", 17f, Amber, TextAlignmentOptions.MidlineLeft, true);
            RectTransform previewClip = CreateTopLeft("PreviewClip", detail, 12f, 74f, 344f, 168f);
            previewClip.gameObject.AddComponent<RectMask2D>();
            Sprite previewSprite = LoadConfigSprite("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset", "portraitActionSprite");
            Image preview = CreateImage("Preview", previewClip, previewSprite, Color.white);
            Stretch(preview.rectTransform);
            AspectRatioFitter fitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (previewSprite != null)
                fitter.aspectRatio = previewSprite.rect.width / Mathf.Max(1f, previewSprite.rect.height);

            RectTransform stats = CreateTopLeft("Stats", detail, 12f, 250f, 344f, 156f);
            TMP_Text placement = BuildDetailRow(stats, "Footprint", 0f, "FOOTPRINT", "3x3", footprintIcon, Amber);
            TMP_Text materials = BuildDetailRow(stats, "MaterialsCost", 39f, "MATERIALS", "900", catalog.MaterialsIcon, theme.TextPrimary);
            TMP_Text fuel = BuildDetailRow(stats, "FuelCost", 78f, "FUEL", "200", catalog.FuelIcon, theme.OrangeRed);
            TMP_Text production = BuildDetailRow(stats, "BuildTime", 117f, "BUILD TIME", "00:30", timeIcon, Amber);
            TMP_Text requirements = CreateText(detail, "Requirements", 16f, 411f, 336f, 31f, "REQUIRES  •  COMMAND CENTER LEVEL 1", 13f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            TMP_Text description = CreateText(detail, "Description", 14f, 76f, 334f, 40f, string.Empty, 14f, theme.TextMuted, TextAlignmentOptions.TopLeft, false);
            description.gameObject.SetActive(false);
            return new DetailBindings(preview, name, role, description, materials, fuel, production, placement, requirements);
        }

        private static TMP_Text BuildDetailRow(RectTransform parent, string name, float y, string label, string value, Sprite sprite, Color accent)
        {
            RectTransform row = CreateTopLeft(name, parent, 0f, y, 344f, 38f);
            if (y > 0f)
                CreateSolid("Divider", row, 0f, 0f, 344f, 1f, new Color(Line.r, Line.g, Line.b, .48f));
            Image icon = CreateImage("Icon", row, sprite, Color.white);
            SetTopLeft(icon.rectTransform, 4f, 6f, 26f, 26f);
            CreateText(row, "Label", 39f, 3f, 175f, 32f, label, 15f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, true);
            return CreateText(row, "Value", 220f, 3f, 115f, 32f, value, 18f, accent, TextAlignmentOptions.MidlineRight, true);
        }

        private static QueueBindings BuildQueue(RectTransform frame)
        {
            RectTransform production = CreatePanel("ProductionPanel", frame, 1182f, 585f, 368f, 225f, DarkTop, DarkBottom, Line, 3f);
            RectTransform activePanel = CreateTopLeft("ProductionPanelActive", production, 0f, 0f, 368f, 225f);
            CreateText(activePanel, "Name", 12f, 4f, 230f, 36f, "PRODUCTION QUEUE", 20f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text numbers = CreateText(activePanel, "Numbers", 279f, 4f, 72f, 36f, "2 / 3", 18f, Green, TextAlignmentOptions.MidlineRight, true);
            RectTransform scroll = CreateTopLeft("Scroll View", activePanel, 10f, 43f, 348f, 172f);
            RectTransform viewport = CreateTopLeft("Viewport", scroll, 0f, 0f, 348f, 172f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = CreateTopLeft("Content", viewport, 0f, 0f, 348f, 172f);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            QueueItemBindings active = BuildQueueItem(content, "ProductionActiveItemView", "1", "GUARD TOWER", "00:18", true);
            QueueItemBindings queued = BuildQueueItem(content, "ProductionItemView", "2", "FABRICATION DEPOT", "00:25", false);
            BuildQueueLockedRow(content);
            TMP_Text noProduction = CreateText(production, "NoProduction", 18f, 88f, 332f, 55f, "NO PRODUCTION QUEUED", 20f, theme.TextMuted, TextAlignmentOptions.Center, true);
            noProduction.gameObject.SetActive(false);

            Button rush = CreateHiddenUtilityButton(activePanel, "RushButton");
            Button clear = CreateHiddenUtilityButton(activePanel, "ClearButton");
            return new QueueBindings(
                production, activePanel, noProduction.gameObject, noProduction, content,
                queued.View, active.View, active.Progress, active.Percentage, active.Time,
                numbers, active.Cancel, rush, clear);
        }

        private static QueueItemBindings BuildQueueItem(RectTransform parent, string name, string number, string label, string time, bool active)
        {
            RectTransform row = CreatePanel(name, parent, 0f, 0f, 348f, 50f, RaisedTop, DarkBottom, Line, 3f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            TMP_Text numberText = CreateText(row, "Number", 7f, 3f, 35f, 44f, number, 21f, theme.TextPrimary, TextAlignmentOptions.Center, true);
            TMP_Text nameText = CreateText(row, "Name", 48f, 4f, 145f, 42f, label, 15f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text producer = CreateText(row, "Producer", 48f, 24f, 145f, 20f, string.Empty, 11f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            producer.gameObject.SetActive(false);
            TMP_Text timeText = CreateText(row, "TimeText", 265f, 4f, 73f, 42f, time, 15f, theme.TextPrimary, TextAlignmentOptions.MidlineRight, true);
            Image thumbnail = CreateImage("Image", row, null, Color.white);
            SetTopLeft(thumbnail.rectTransform, 45f, 7f, 36f, 36f);
            thumbnail.gameObject.SetActive(false);

            Slider slider = CreateSlider(row, "Slider", 48f, 42f, 192f, 5f, active ? .42f : 0f);
            slider.gameObject.SetActive(active);
            TMP_Text percentage = CreateText(row, "Percentage", 197f, 4f, 56f, 42f, active ? "42%" : string.Empty, 11f, Cyan, TextAlignmentOptions.MidlineRight, true);
            percentage.gameObject.SetActive(active);
            Button cancel = CreateHiddenUtilityButton(row, "CancelButton");
            SetTopLeft(cancel.GetComponent<RectTransform>(), 265f, 4f, 73f, 42f);
            timeText.raycastTarget = false;

            BuildDrawerQueueItemView view = row.gameObject.AddComponent<BuildDrawerQueueItemView>();
            var serialized = new SerializedObject(view);
            Set(serialized, "cancelButton", cancel);
            Set(serialized, "thumbnailImage", thumbnail);
            Set(serialized, "progressSlider", slider);
            Set(serialized, "nameText", nameText);
            Set(serialized, "producerText", producer);
            Set(serialized, "timeText", timeText);
            Set(serialized, "percentageText", percentage);
            Set(serialized, "numberText", numberText);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return new QueueItemBindings(view, slider, percentage, timeText, cancel);
        }

        private static void BuildQueueLockedRow(RectTransform parent)
        {
            RectTransform row = CreatePanel("QueueSlotLocked", parent, 0f, 0f, 348f, 58f,
                new Color32(30, 35, 37, 255), new Color32(10, 13, 15, 255), new Color32(67, 75, 78, 255), 3f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            CreateText(row, "Number", 7f, 4f, 35f, 48f, "3", 20f, theme.TextMuted, TextAlignmentOptions.Center, true);
            CreateText(row, "Label", 49f, 4f, 244f, 24f, "QUEUE SLOT LOCKED", 14f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, true);
            CreateText(row, "Requirement", 49f, 27f, 244f, 24f, "Upgrade Command Center", 11f, theme.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            Image icon = CreateImage("Lock", row, lockIcon, theme.TextMuted);
            SetTopLeft(icon.rectTransform, 309f, 15f, 26f, 28f);
        }

        private static FooterBindings BuildFooter(RectTransform frame)
        {
            RectTransform instruction = CreatePanel("InstructionStrip", frame, 122f, 820f, 946f, 95f,
                new Color32(53, 18, 10, 255), new Color32(12, 5, 3, 255), Orange, 3f);
            Image icon = CreateImage("Icon", instruction, Require<Sprite>(V3UiFoundationBuilder.MatchInvalidIconPath), Orange);
            SetTopLeft(icon.rectTransform, 82f, 19f, 54f, 54f);
            TMP_Text instructionText = CreateText(instruction, "Instruction", 155f, 7f, 760f, 80f,
                "SELECT A VALID BUILD AREA", 34f, Orange, TextAlignmentOptions.Center, true);

            RectTransform unavailable = CreatePanel("UnavailablePanel", frame, 122f, 820f, 946f, 95f,
                new Color32(16, 44, 54, 255), new Color32(3, 13, 18, 255), Cyan, 3f);
            Image unavailableIcon = CreateImage("Icon", unavailable, lockIcon, Cyan);
            SetTopLeft(unavailableIcon.rectTransform, 62f, 18f, 56f, 58f);
            TMP_Text unavailableTitle = CreateText(unavailable, "Title", 142f, 7f, 330f, 42f,
                "BUILD UNAVAILABLE", 28f, Cyan, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text unavailableDescription = CreateText(unavailable, "Description", 142f, 47f, 740f, 35f,
                "Mission does not allow construction.", 17f, theme.TextPrimary, TextAlignmentOptions.MidlineLeft, false);
            unavailable.gameObject.SetActive(false);

            RectTransform placeRect = CreatePanel("BuildButton", frame, 1078f, 820f, 472f, 95f,
                new Color32(68, 181, 69, 255), new Color32(8, 76, 29, 255), Green, 3f);
            Button place = placeRect.gameObject.AddComponent<Button>();
            place.targetGraphic = placeRect.GetComponent<V3GradientGraphic>();
            place.targetGraphic.raycastTarget = true;
            TMP_Text label = CreateText(placeRect, "Label", 30f, 6f, 412f, 82f, "PLACE", 48f, theme.TextPrimary, TextAlignmentOptions.Center, true);

            // BuildButton is the one authored CTA for both placement and production. The
            // legacy duplicate OrderButton was permanently hidden, had no raycast graphic,
            // and made the serialized screen fail functional pointer auditing.
            return new FooterBindings(instructionText, icon, place, null, label, unavailable.gameObject, unavailableTitle, unavailableDescription);
        }

        private static void BindView(
            BuildDrawerView view,
            RectTransform drawerRoot,
            Button closeButton,
            TabBindings tabs,
            CatalogBindings itemCatalog,
            DetailBindings detail,
            QueueBindings queue,
            FooterBindings footer)
        {
            var serialized = new SerializedObject(view);
            Set(serialized, "drawerRoot", drawerRoot.gameObject);
            Set(serialized, "closeButton", closeButton);
            SerializedProperty tabArray = serialized.FindProperty("tabs");
            tabArray.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                SerializedProperty tab = tabArray.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("category").enumValueIndex = i;
                tab.FindPropertyRelative("button").objectReferenceValue = tabs.Buttons[i];
                tab.FindPropertyRelative("frame").objectReferenceValue = tabs.Frames[i];
                tab.FindPropertyRelative("labelText").objectReferenceValue = tabs.Labels[i];
                tab.FindPropertyRelative("countText").objectReferenceValue = null;
                tab.FindPropertyRelative("disabledOverlay").objectReferenceValue = tabs.DisabledOverlays[i];
            }
            Set(serialized, "selectedTabFrameSprite", catalog.FocusOverlay);
            Set(serialized, "normalTabFrameSprite", catalog.Button);
            Set(serialized, "itemContentRoot", itemCatalog.Content);
            Set(serialized, "itemTemplate", itemCatalog.Template.View);
            Set(serialized, "selectedItemFrameSprite", catalog.FocusOverlay);
            Set(serialized, "previewImage", detail.Preview);
            Set(serialized, "thumbnailImage", null);
            Set(serialized, "nameText", detail.Name);
            Set(serialized, "roleText", detail.Role);
            Set(serialized, "descriptionText", detail.Description);
            Set(serialized, "materialsCostText", detail.MaterialsCost);
            Set(serialized, "fuelCostText", detail.FuelCost);
            Set(serialized, "productionTimeText", detail.Time);
            Set(serialized, "placementText", detail.Placement);
            Set(serialized, "requirementsText", detail.Requirements);
            Set(serialized, "buildButton", footer.BuildButton);
            Set(serialized, "orderButton", footer.OrderButton);
            Set(serialized, "primaryActionLabelText", footer.ActionLabel);
            Set(serialized, "unavailablePanel", footer.UnavailablePanel);
            Set(serialized, "unavailableTitleText", footer.UnavailableTitle);
            Set(serialized, "unavailableDescriptionText", footer.UnavailableDescription);
            Set(serialized, "instructionText", footer.InstructionText);
            Set(serialized, "instructionIcon", footer.InstructionIcon);
            Set(serialized, "instructionInfoIcon", Require<Sprite>(V3UiFoundationBuilder.OperationsIntelIconPath));
            Set(serialized, "instructionReadyIcon", Require<Sprite>(V3UiFoundationBuilder.FirstLaunchTargetIconPath));
            Set(serialized, "instructionWarningIcon", Require<Sprite>(V3UiFoundationBuilder.MatchInvalidIconPath));
            Set(serialized, "instructionErrorIcon", Require<Sprite>(V3UiFoundationBuilder.MatchInvalidIconPath));
            Set(serialized, "productionPanel", queue.Panel.gameObject);
            Set(serialized, "productionPanelActive", queue.ActivePanel.gameObject);
            Set(serialized, "noProductionView", queue.NoProductionView);
            Set(serialized, "noProductionText", queue.NoProductionText);
            Set(serialized, "queueContentRoot", queue.Content);
            Set(serialized, "queuedItemTemplate", queue.QueuedTemplate);
            Set(serialized, "activeItemView", queue.ActiveItem);
            Set(serialized, "queueProgressSlider", queue.Progress);
            Set(serialized, "queuePercentageText", queue.Percentage);
            Set(serialized, "queueTimeText", queue.Time);
            Set(serialized, "queueNumbersText", queue.Numbers);
            Set(serialized, "cancelButton", queue.Cancel);
            Set(serialized, "rushButton", queue.Rush);
            Set(serialized, "clearButton", queue.Clear);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindPresenter(BuildDrawerCatalogRuntimeView presenter, BuildDrawerView view, PresenterAssets assets)
        {
            var serialized = new SerializedObject(presenter);
            Set(serialized, "view", view);
            Set(serialized, "unitPrefabRegistryConfig", assets.UnitRegistry);
            Set(serialized, "buildingPlacementConfig", assets.BuildingPlacement);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindClose(GameObject root, UIPopupCloseView closeView, UIPopupCloseButtonView closeButtonView, Button closeButton)
        {
            var close = new SerializedObject(closeView);
            Set(close, "closeButton", closeButton);
            Set(close, "popupRoot", root);
            close.FindProperty("commandModeToClear").enumValueIndex = (int)TacticalCommandMode.Build;
            close.ApplyModifiedPropertiesWithoutUndo();
            var button = new SerializedObject(closeButtonView);
            Set(button, "closeView", closeView);
            button.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Capture(string path, int width, int height, bool disabled)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new("BuildDrawerV3CaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .045f, .05f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = height * .5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;

            GameObject canvasObject = new("BuildDrawerV3CaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            Stretch(instance.transform as RectTransform);
            BuildDrawerView view = instance.GetComponent<BuildDrawerView>();
            if (disabled)
            {
                view.ApplyTabVisuals(BuildDrawerCategory.Buildings, new int[4], new bool[4]);
                view.ApplyAvailability(false);
            }
            else
            {
                view.ApplyTabVisuals(BuildDrawerCategory.Buildings, new[] { 4, 3, 2, 8 }, new[] { true, true, true, true });
                view.ApplyAvailability(true);
                view.ItemTemplate.SetSelected(true, view.SelectedItemFrameSprite);
            }

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
                Debug.Log($"[BuildDrawerV3PrefabBuilder] capture=Passed state={(disabled ? "disabled" : "ready")} size={width}x{height} path={path} scene={scene.name}");
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

        private static Slider CreateSlider(RectTransform parent, string name, float x, float y, float width, float height, float value)
        {
            RectTransform root = CreateTopLeft(name, parent, x, y, width, height);
            Slider slider = root.gameObject.AddComponent<Slider>();
            Image background = CreateSolid("Background", root, 0f, 0f, width, height, new Color32(29, 45, 51, 255));
            RectTransform fillArea = CreateTopLeft("Fill Area", root, 0f, 0f, width, height);
            Image fill = CreateSolid("Fill", fillArea, 0f, 0f, width, height, Cyan);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.interactable = false;
            return slider;
        }

        private static Button CreateHiddenUtilityButton(RectTransform parent, string name)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 1f, 1f);
            Image hitTarget = rect.gameObject.AddComponent<Image>();
            hitTarget.color = Color.clear;
            hitTarget.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hitTarget;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static void BuildX(RectTransform parent, Color color)
        {
            RectTransform first = CreateSolid("StrokeA", parent, 0f, 0f, 7f, 42f, color).rectTransform;
            Center(first);
            first.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RectTransform second = CreateSolid("StrokeB", parent, 0f, 0f, 7f, 42f, color).rectTransform;
            Center(second);
            second.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        private static void BuildCraneMark(RectTransform parent, Color color)
        {
            CreateSolid("Mast", parent, 13f, 10f, 6f, 43f, color);
            CreateSolid("TopBeam", parent, 4f, 9f, 51f, 6f, color);
            CreateSolid("Base", parent, 5f, 51f, 27f, 6f, color);
            RectTransform braceA = CreateSolid("BraceA", parent, 0f, 0f, 5f, 38f, color).rectTransform;
            Center(braceA, new Vector2(-9f, 3f));
            braceA.localRotation = Quaternion.Euler(0f, 0f, -27f);
            RectTransform braceB = CreateSolid("BraceB", parent, 0f, 0f, 5f, 28f, color).rectTransform;
            Center(braceB, new Vector2(-9f, 11f));
            braceB.localRotation = Quaternion.Euler(0f, 0f, 27f);
            CreateSolid("Cable", parent, 49f, 14f, 3f, 27f, color);
            CreateSolid("HookBar", parent, 44f, 40f, 9f, 4f, color);
        }

        private static void Center(RectTransform rect, Vector2 offset = default)
        {
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = offset;
        }

        private static Sprite LoadConfigSprite(string assetPath, string propertyName)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return null;
            return new SerializedObject(asset).FindProperty(propertyName)?.objectReferenceValue as Sprite;
        }

        private static RectTransform CreatePanel(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic gradient = rect.gameObject.AddComponent<V3GradientGraphic>();
            gradient.Configure(top, bottom, border, borderWidth);
            gradient.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, 0f, 1f, 1f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, float x, float y, float width, float height, string value, float size, Color color, TextAlignmentOptions alignment, bool bold)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontSizeMin = Mathf.Max(10f, size * .58f);
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text FindText(Transform parent, string name) => parent.Find(name)?.GetComponent<TMP_Text>();

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void Set(SerializedObject serialized, string name, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, name);
            property.objectReferenceValue = value;
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing required asset: {path}");
            return asset;
        }

        private static void AppendHierarchy(StringBuilder report, Transform root, Transform node, int depth)
        {
            string path = node == root ? root.name : AnimationUtility.CalculateTransformPath(node, root);
            RectTransform rect = node as RectTransform;
            report.Append(' ', depth * 2).Append(path);
            if (rect != null)
                report.Append(" anchor=").Append(rect.anchorMin).Append("..").Append(rect.anchorMax).Append(" pos=").Append(rect.anchoredPosition).Append(" size=").Append(rect.sizeDelta);
            report.AppendLine();
            for (int i = 0; i < node.childCount; i++)
                AppendHierarchy(report, root, node.GetChild(i), depth + 1);
        }

        private readonly struct PresenterAssets
        {
            public readonly ScriptableObject UnitRegistry;
            public readonly ScriptableObject BuildingPlacement;
            public PresenterAssets(ScriptableObject unitRegistry, ScriptableObject buildingPlacement) { UnitRegistry = unitRegistry; BuildingPlacement = buildingPlacement; }
        }

        private readonly struct TabBindings
        {
            public readonly Button[] Buttons;
            public readonly Image[] Frames;
            public readonly TMP_Text[] Labels;
            public readonly GameObject[] DisabledOverlays;
            public TabBindings(Button[] buttons, Image[] frames, TMP_Text[] labels, GameObject[] disabledOverlays) { Buttons = buttons; Frames = frames; Labels = labels; DisabledOverlays = disabledOverlays; }
        }

        private readonly struct CardBindings
        {
            public readonly BuildDrawerItemView View;
            public readonly Image Thumbnail;
            public readonly Image Frame;
            public CardBindings(BuildDrawerItemView view, Image thumbnail, Image frame) { View = view; Thumbnail = thumbnail; Frame = frame; }
        }

        private readonly struct CatalogBindings
        {
            public readonly RectTransform Content;
            public readonly CardBindings Template;
            public CatalogBindings(RectTransform content, CardBindings template) { Content = content; Template = template; }
        }

        private readonly struct DetailBindings
        {
            public readonly Image Preview;
            public readonly TMP_Text Name;
            public readonly TMP_Text Role;
            public readonly TMP_Text Description;
            public readonly TMP_Text MaterialsCost;
            public readonly TMP_Text FuelCost;
            public readonly TMP_Text Time;
            public readonly TMP_Text Placement;
            public readonly TMP_Text Requirements;
            public DetailBindings(Image preview, TMP_Text name, TMP_Text role, TMP_Text description, TMP_Text materialsCost, TMP_Text fuelCost, TMP_Text time, TMP_Text placement, TMP_Text requirements) { Preview = preview; Name = name; Role = role; Description = description; MaterialsCost = materialsCost; FuelCost = fuelCost; Time = time; Placement = placement; Requirements = requirements; }
        }

        private readonly struct QueueItemBindings
        {
            public readonly BuildDrawerQueueItemView View;
            public readonly Slider Progress;
            public readonly TMP_Text Percentage;
            public readonly TMP_Text Time;
            public readonly Button Cancel;
            public QueueItemBindings(BuildDrawerQueueItemView view, Slider progress, TMP_Text percentage, TMP_Text time, Button cancel) { View = view; Progress = progress; Percentage = percentage; Time = time; Cancel = cancel; }
        }

        private readonly struct QueueBindings
        {
            public readonly RectTransform Panel;
            public readonly RectTransform ActivePanel;
            public readonly GameObject NoProductionView;
            public readonly TMP_Text NoProductionText;
            public readonly RectTransform Content;
            public readonly BuildDrawerQueueItemView QueuedTemplate;
            public readonly BuildDrawerQueueItemView ActiveItem;
            public readonly Slider Progress;
            public readonly TMP_Text Percentage;
            public readonly TMP_Text Time;
            public readonly TMP_Text Numbers;
            public readonly Button Cancel;
            public readonly Button Rush;
            public readonly Button Clear;
            public QueueBindings(RectTransform panel, RectTransform activePanel, GameObject noProductionView, TMP_Text noProductionText, RectTransform content, BuildDrawerQueueItemView queuedTemplate, BuildDrawerQueueItemView activeItem, Slider progress, TMP_Text percentage, TMP_Text time, TMP_Text numbers, Button cancel, Button rush, Button clear) { Panel = panel; ActivePanel = activePanel; NoProductionView = noProductionView; NoProductionText = noProductionText; Content = content; QueuedTemplate = queuedTemplate; ActiveItem = activeItem; Progress = progress; Percentage = percentage; Time = time; Numbers = numbers; Cancel = cancel; Rush = rush; Clear = clear; }
        }

        private readonly struct FooterBindings
        {
            public readonly TMP_Text InstructionText;
            public readonly Image InstructionIcon;
            public readonly Button BuildButton;
            public readonly Button OrderButton;
            public readonly TMP_Text ActionLabel;
            public readonly GameObject UnavailablePanel;
            public readonly TMP_Text UnavailableTitle;
            public readonly TMP_Text UnavailableDescription;
            public FooterBindings(TMP_Text instructionText, Image instructionIcon, Button buildButton, Button orderButton, TMP_Text actionLabel, GameObject unavailablePanel, TMP_Text unavailableTitle, TMP_Text unavailableDescription) { InstructionText = instructionText; InstructionIcon = instructionIcon; BuildButton = buildButton; OrderButton = orderButton; ActionLabel = actionLabel; UnavailablePanel = unavailablePanel; UnavailableTitle = unavailableTitle; UnavailableDescription = unavailableDescription; }
        }
    }
}
#endif
