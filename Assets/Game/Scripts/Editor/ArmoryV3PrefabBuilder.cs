#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ArmoryV3PrefabBuilder
    {
        internal const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";
        internal const string BackgroundPath =
            "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png";
        private const string UnitConfigPath =
            "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        private const string BuildingConfigPath =
            "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset";
        private const string BoldFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath =
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color DarkTop = new Color32(28, 37, 42, 253);
        private static readonly Color DarkBottom = new Color32(3, 9, 12, 255);
        private static readonly Color RaisedTop = new Color32(42, 52, 57, 255);
        private static readonly Color RaisedBottom = new Color32(10, 17, 20, 255);
        private static readonly Color Line = new Color32(88, 103, 108, 255);
        private static readonly Color White = new Color32(239, 242, 238, 255);
        private static readonly Color Muted = new Color32(174, 181, 180, 255);
        private static readonly Color Cyan = new Color32(26, 191, 239, 255);
        private static readonly Color Lime = new Color32(145, 205, 50, 255);
        private static readonly Color BlueTop = new Color32(18, 122, 190, 255);
        private static readonly Color BlueBottom = new Color32(3, 67, 123, 255);
        private static readonly Color GreenTop = new Color32(86, 141, 43, 255);
        private static readonly Color GreenBottom = new Color32(30, 75, 27, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static Sprite background;
        private static Sprite compatibilityFrame;
        private static ScriptableObject unitConfig;
        private static ScriptableObject buildingConfig;

        [MenuItem("Game/UI/V3/Rebuild SCN-19 Armory")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = CreateRect(
                "SCN19_ArmoryContent", null, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero).gameObject;
            try
            {
                UIShellContentSectionsView sectionsView =
                    root.AddComponent<UIShellContentSectionsView>();
                var sections = new List<UIShellContentSectionsView.SectionReference>(6);
                RectTransform backgroundSection = CreateSection(
                    "MenuBackgroundContent", root.transform,
                    UIShellContentSectionId.MenuBackground, sections);
                RectTransform headerSection = CreateSection(
                    "HeaderContent", root.transform,
                    UIShellContentSectionId.Header, sections);
                RectTransform leftSection = CreateSection(
                    "LeftContent", root.transform,
                    UIShellContentSectionId.Left, sections);
                RectTransform middleSection = CreateSection(
                    "MiddleContent", root.transform,
                    UIShellContentSectionId.Middle, sections);
                RectTransform rightSection = CreateSection(
                    "RightContent", root.transform,
                    UIShellContentSectionId.Right, sections);
                RectTransform footerSection = CreateSection(
                    "FooterContent", root.transform,
                    UIShellContentSectionId.Footer, sections);
                sectionsView.ConfigureSections(sections.ToArray());

                BuildBackground(backgroundSection);
                HeaderBindings header = BuildHeader(headerSection);
                BuildLeftNavigation(leftSection);
                MiddleBindings middle = BuildCatalog(middleSection);
                RightBindings right = BuildInspection(rightSection);
                FooterBindings footer = BuildFooter(footerSection);
                ConfigureCatalog(middle, right);
                ConfigureLayouts(
                    headerSection, header,
                    leftSection,
                    middleSection, middle,
                    rightSection, right,
                    footerSection, footer);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log(
                "[ArmoryV3PrefabBuilder] result=Passed target=SCN-19 " +
                "sections=6 gradients=procedural borders=3 portraits=runtime-reused " +
                "layout=responsive");
        }

        [MenuItem("Game/UI/V3/Validate SCN-19 Armory")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Armory prefab: {PrefabPath}");

            UIShellContentSectionsView sections =
                prefab.GetComponent<UIShellContentSectionsView>();
            if (sections == null || sections.Sections == null || sections.Sections.Count != 6)
                throw new MissingReferenceException("SCN-19 must expose all six shell sections.");

            ArmoryCategoryNavigationView nav =
                prefab.GetComponentInChildren<ArmoryCategoryNavigationView>(true);
            ArmoryContentListView list =
                prefab.GetComponentInChildren<ArmoryContentListView>(true);
            ArmoryRightContentView right =
                prefab.GetComponentInChildren<ArmoryRightContentView>(true);
            if (nav == null || list == null || right == null || right.InspectionPanel == null)
                throw new MissingReferenceException("SCN-19 runtime catalog bindings are incomplete.");

            SerializedProperty tabs = new SerializedObject(nav).FindProperty("tabs");
            if (tabs == null || tabs.arraySize != 5)
                throw new InvalidOperationException("SCN-19 requires five large V3 category tabs.");

            SerializedObject listObject = new(list);
            if (listObject.FindProperty("unitPrefabRegistryConfig").objectReferenceValue == null ||
                listObject.FindProperty("buildingPlacementConfig").objectReferenceValue == null ||
                listObject.FindProperty("contentRoot").objectReferenceValue == null ||
                listObject.FindProperty("itemTemplate").objectReferenceValue == null)
            {
                throw new MissingReferenceException("SCN-19 catalog data references are incomplete.");
            }

            MainMenuV3SectionLayoutView[] layouts =
                prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
            if (layouts.Length < 5)
                throw new InvalidOperationException("SCN-19 sections must use responsive V3 layout.");
            foreach (MainMenuV3SectionLayoutView layout in layouts)
            {
                if (layout.ReferenceResolution != Reference)
                    throw new InvalidOperationException("SCN-19 layout reference must be 1672x941.");
            }

            AspectRatioFitter fitter =
                prefab.transform.Find("MenuBackgroundContent/ArmoryBackground")
                    ?.GetComponent<AspectRatioFitter>();
            if (fitter == null || fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                throw new InvalidOperationException("SCN-19 background must crop without stretching.");

            int borderedGradients = 0;
            foreach (V3GradientGraphic gradient in
                     prefab.GetComponentsInChildren<V3GradientGraphic>(true))
            {
                SerializedObject serialized = new(gradient);
                float width = serialized.FindProperty("borderWidth").floatValue;
                Color color = serialized.FindProperty("borderColor").colorValue;
                if (color.a <= .01f)
                    continue;
                borderedGradients++;
                if (!Mathf.Approximately(width, 3f))
                {
                    throw new InvalidOperationException(
                        $"SCN-19 border on {gradient.name} is {width}px; visible borders must be 3px.");
                }
            }

            if (borderedGradients < 25)
                throw new InvalidOperationException("SCN-19 is missing layered V3 gradient chrome.");

            Debug.Log(
                $"[ArmoryV3PrefabBuilder] validation=Passed sections=6 " +
                $"tabs=5 borderedGradients={borderedGradients} borders=3");
        }

        private static void BuildBackground(Transform root)
        {
            Image art = CreateImage("ArmoryBackground", root, background, Color.white);
            Stretch(art.rectTransform);
            art.preserveAspect = false;
            AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.rect.width / background.rect.height;
            V3GradientGraphic shade = CreatePanel(
                "BackdropShade", root, 0f, 0f, Reference.x, Reference.y,
                new Color(0f, .025f, .035f, .28f),
                new Color(0f, .01f, .015f, .78f), Color.clear, 0f);
            Stretch(shade.rectTransform);
        }

        private static HeaderBindings BuildHeader(Transform root)
        {
            V3GradientGraphic bar = CreatePanel(
                "HeaderBar", root, 6f, 8f, 1655f, 87f,
                new Color32(27, 37, 42, 248), new Color32(4, 11, 14, 252), Line, 3f);
            RectTransform logo = CreateTopLeft("Brand", root, 8f, 9f, 350f, 84f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            RectTransform credits = BuildResource(
                root, "CreditsPanel", 1060f, "CREDITS", "24,750", Cyan, true);
            RectTransform command = BuildResource(
                root, "CommandPanel", 1312f, "COMMAND", "8,430", Cyan, false);
            RectTransform settings = BuildSettingsButton(root);
            return new HeaderBindings(bar.rectTransform, credits, command, settings);
        }

        private static RectTransform BuildResource(
            Transform root, string name, float x,
            string label, string value, Color accent, bool credits)
        {
            RectTransform panel = CreatePanel(
                name, root, x, 10f, 244f, 82f,
                RaisedTop, DarkBottom, Line, 3f).rectTransform;
            RectTransform iconRoot = CreateTopLeft("Icon", panel, 15f, 12f, 58f, 58f);
            if (credits)
                BuildBarCoin(iconRoot, accent);
            else
                BuildCommandHex(iconRoot, accent);
            CreateText(panel, "Label", 81f, 7f, 147f, 31f,
                label, 19f, White, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "Value", 81f, 34f, 147f, 40f,
                value, 29f, White, TextAlignmentOptions.MidlineLeft, true);
            return panel;
        }

        private static RectTransform BuildSettingsButton(Transform root)
        {
            RectTransform rect = CreatePanel(
                "SettingsButton", root, 1569f, 10f, 91f, 82f,
                RaisedTop, DarkBottom, Line, 3f).rectTransform;
            Button button = AddButton(rect.gameObject, rect.GetComponent<V3GradientGraphic>());
            Image icon = CreateImage(
                "Icon", rect, RequireSprite(V3UiFoundationBuilder.SettingsIconPath), White);
            SetTopLeft(icon.rectTransform, 21f, 18f, 49f, 49f);
            UIShellActionButtonView action = rect.gameObject.AddComponent<UIShellActionButtonView>();
            SerializedObject serialized = new(action);
            serialized.FindProperty("actionKind").enumValueIndex = (int)UiActionKind.OpenSettings;
            serialized.FindProperty("payloadId").intValue = 0;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rect;
        }

        private static void BuildLeftNavigation(Transform root)
        {
            RectTransform rail = CreateTopLeft("CategoryRail", root, 6f, 147f, 265f, 628f);
            ArmoryCategoryNavigationView navigation =
                root.gameObject.AddComponent<ArmoryCategoryNavigationView>();
            var bindings = new List<NavigationBinding>(5);
            BuildCategoryTab(rail, bindings, "UnitsTab", 0f,
                ArmoryCatalogCategory.Characters, "UNITS",
                V3UiFoundationBuilder.CommanderRosterIconPath);
            BuildCategoryTab(rail, bindings, "VehiclesTab", 132f,
                ArmoryCatalogCategory.Vehicles, "VEHICLES",
                V3UiFoundationBuilder.MatchArmorIconPath);
            BuildCategoryTab(rail, bindings, "AircraftTab", 264f,
                ArmoryCatalogCategory.Aircrafts, "AIRCRAFT",
                V3UiFoundationBuilder.MissionAirIconPath);
            BuildCategoryTab(rail, bindings, "BuildingsTab", 396f,
                ArmoryCatalogCategory.Buildings, "BUILDINGS",
                V3UiFoundationBuilder.CampaignBarracksIconPath);
            BuildCategoryTab(rail, bindings, "UpgradesTab", 528f,
                ArmoryCatalogCategory.Support, "UPGRADES",
                V3UiFoundationBuilder.CommanderUpgradesIconPath);

            SerializedObject serialized = new(navigation);
            SerializedProperty tabs = serialized.FindProperty("tabs");
            tabs.arraySize = bindings.Count;
            for (int i = 0; i < bindings.Count; i++)
            {
                SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("category").enumValueIndex = (int)bindings[i].Category;
                tab.FindPropertyRelative("button").objectReferenceValue = bindings[i].Button;
                tab.FindPropertyRelative("frame").objectReferenceValue = bindings[i].Frame;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCategoryTab(
            Transform parent,
            ICollection<NavigationBinding> bindings,
            string name,
            float y,
            ArmoryCatalogCategory category,
            string label,
            string iconPath)
        {
            RectTransform rect = CreateTopLeft(name, parent, 0f, y, 265f, 119f);
            V3GradientGraphic panel = rect.gameObject.AddComponent<V3GradientGraphic>();
            panel.Configure(RaisedTop, DarkBottom, Line, 3f);
            Button button = AddButton(rect.gameObject, panel);
            Image icon = CreateImage("Icon", rect, RequireSprite(iconPath), White);
            SetTopLeft(icon.rectTransform, 23f, 29f, 60f, 60f);
            icon.preserveAspect = true;
            TMP_Text text = CreateText(rect, "Label", 103f, 18f, 146f, 82f,
                label, 28f, White, TextAlignmentOptions.MidlineLeft, true);
            text.enableAutoSizing = true;
            text.fontSizeMin = 19f;
            text.fontSizeMax = 28f;
            text.overflowMode = TextOverflowModes.Overflow;
            Image frameReference = CreateImage(
                "FrameReference", rect, compatibilityFrame, Color.clear);
            SetTopLeft(frameReference.rectTransform, 0f, 0f, 1f, 1f);
            frameReference.raycastTarget = false;
            ArmoryV3CategoryTabVisual visual =
                rect.gameObject.AddComponent<ArmoryV3CategoryTabVisual>();
            visual.Configure(panel, text, icon);
            visual.SetSelected(category == ArmoryCatalogCategory.Characters);
            bindings.Add(new NavigationBinding(category, button, frameReference));
        }

        private static MiddleBindings BuildCatalog(Transform root)
        {
            RectTransform panel = CreatePanel(
                "CatalogPanel", root, 288f, 110f, 946f, 699f,
                DarkTop, DarkBottom, Line, 3f).rectTransform;
            CreateText(panel, "Title", 24f, 4f, 385f, 58f,
                "ARMORY", 38f, White, TextAlignmentOptions.MidlineLeft, true);
            RectTransform filter = BuildDropdown(panel, "FilterButton", 447f, "FILTER: ALL");
            RectTransform sort = BuildDropdown(panel, "SortButton", 702f, "SORT: RARITY");
            CreateSolid("HeaderDivider", panel, 3f, 67f, 940f, 3f, Line);

            RectTransform viewport = CreateTopLeft(
                "CatalogViewport", panel, 13f, 72f, 920f, 614f);
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0f, 0f, 0f, .01f);
            viewportHit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = CreateRect(
                "CatalogGrid", viewport,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 614f), Vector2.zero);
            content.pivot = new Vector2(0f, 1f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.cellSize = new Vector2(221f, 296f);
            grid.spacing = new Vector2(12f, 11f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            content.gameObject.AddComponent<ArmoryV3ResponsiveCatalogGrid>()
                .Configure(4, 296f);
            ContentSizeFitter size = content.gameObject.AddComponent<ContentSizeFitter>();
            size.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            ArmoryCatalogItemView template = BuildCatalogItem(content);
            ArmoryContentListView list = root.gameObject.AddComponent<ArmoryContentListView>();
            return new MiddleBindings(
                panel, filter, sort, viewport, content, list, template);
        }

        private static RectTransform BuildDropdown(
            Transform parent, string name, float x, string label)
        {
            RectTransform rect = CreatePanel(
                name, parent, x, 14f, 236f, 43f,
                RaisedTop, DarkBottom, Line, 3f).rectTransform;
            AddButton(rect.gameObject, rect.GetComponent<V3GradientGraphic>());
            CreateText(rect, "Label", 14f, 2f, 174f, 39f,
                label, 19f, White, TextAlignmentOptions.MidlineLeft, true);
            CreateTriangle(rect, 207f, 14f, Cyan);
            return rect;
        }

        private static ArmoryCatalogItemView BuildCatalogItem(Transform parent)
        {
            RectTransform card = CreateTopLeft("ItemView", parent, 0f, 0f, 221f, 296f);
            V3GradientGraphic frame = card.gameObject.AddComponent<V3GradientGraphic>();
            frame.Configure(DarkTop, DarkBottom, Line, 3f);
            Button selection = AddButton(card.gameObject, frame);
            ArmoryV3CatalogItemVisual visual =
                card.gameObject.AddComponent<ArmoryV3CatalogItemVisual>();
            visual.Configure(frame);

            TMP_Text title = CreateText(card, "TitleText", 8f, 5f, 205f, 39f,
                "RIFLE SQUAD", 19f, White, TextAlignmentOptions.Center, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 11f;
            title.fontSizeMax = 19f;
            title.overflowMode = TextOverflowModes.Overflow;
            StretchHorizontal(title.rectTransform, 8f, 8f, 5f, 39f);
            Image titleDivider = CreateSolid("TitleDivider", card, 3f, 45f, 215f, 3f, Line);
            StretchHorizontal(titleDivider.rectTransform, 3f, 3f, 45f, 3f);
            List<CategoryVisual> categoryVisuals = BuildCategoryArtSets(
                card, 3f, 48f, 215f, 153f, "CardArt");
            foreach (CategoryVisual categoryVisual in categoryVisuals)
            {
                StretchHorizontal((RectTransform)categoryVisual.Root.transform, 3f, 3f, 48f, 153f);
            }
            V3GradientGraphic ownedBand = CreatePanel("OwnedBand", card, 3f, 201f, 215f, 33f,
                new Color32(18, 47, 22, 255),
                new Color32(6, 24, 12, 255), Line, 3f);
            StretchHorizontal(ownedBand.rectTransform, 3f, 3f, 201f, 33f);
            TMP_Text owned = CreateText(card, "OwnedText", 10f, 201f, 155f, 33f,
                "OWNED", 18f, Lime, TextAlignmentOptions.Center, true);
            StretchHorizontal(owned.rectTransform, 10f, 50f, 201f, 33f);
            RectTransform[] ownedChevron = CreateChevronMark(card, 184f, 207f, Lime);
            SetTopRight(ownedChevron[0], 23f, 207f, 18f, 4f, 38f);
            SetTopRight(ownedChevron[1], 0f, 207f, 18f, 4f, -38f);
            TMP_Text type = CreateText(card, "TypeText", 10f, 239f, 125f, 30f,
                "INFANTRY", 16f, White, TextAlignmentOptions.MidlineLeft, true);
            TMP_Text level = CreateText(card, "LevelText", 138f, 239f, 73f, 30f,
                "LVL 12", 16f, White, TextAlignmentOptions.MidlineRight, true);
            SetTopRight(level.rectTransform, 10f, 239f, 73f, 30f, 0f);
            V3GradientGraphic progressTrack = CreatePanel("ProgressTrack", card, 10f, 273f, 201f, 13f,
                new Color32(24, 33, 36, 255),
                new Color32(8, 14, 16, 255), Line, 3f);
            StretchHorizontal(progressTrack.rectTransform, 10f, 10f, 273f, 13f);
            V3GradientGraphic progressFill = CreatePanel("ProgressFill", card, 13f, 276f, 118f, 7f,
                new Color32(174, 224, 64, 255),
                new Color32(103, 167, 36, 255), Color.clear, 0f);
            StretchHorizontalFraction(progressFill.rectTransform, 13f, .59f, 276f, 7f);

            Image frameReference = CreateImage(
                "FrameReference", card, compatibilityFrame, Color.clear);
            SetTopLeft(frameReference.rectTransform, 0f, 0f, 1f, 1f);
            frameReference.raycastTarget = false;

            ArmoryCatalogItemView item = card.gameObject.AddComponent<ArmoryCatalogItemView>();
            SerializedObject serialized = new(item);
            serialized.FindProperty("selectionButton").objectReferenceValue = selection;
            serialized.FindProperty("frameImage").objectReferenceValue = frameReference;
            serialized.FindProperty("defaultFrameSprite").objectReferenceValue = compatibilityFrame;
            serialized.FindProperty("selectedFrameSprite").objectReferenceValue = compatibilityFrame;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("typeText").objectReferenceValue = type;
            ConfigureCategoryVisuals(
                serialized.FindProperty("categoryVisuals"), categoryVisuals);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static RightBindings BuildInspection(Transform root)
        {
            RectTransform panel = CreatePanel(
                "InspectionPanel", root, 1244f, 110f, 417f, 699f,
                DarkTop, DarkBottom, Line, 3f).rectTransform;
            ArmoryInspectionPanelView inspection =
                panel.gameObject.AddComponent<ArmoryInspectionPanelView>();
            TMP_Text title = CreateText(panel, "TitleText", 18f, 7f, 381f, 47f,
                "RIFLE SQUAD", 30f, White, TextAlignmentOptions.MidlineLeft, true);
            title.enableAutoSizing = true;
            title.fontSizeMin = 15f;
            title.fontSizeMax = 30f;
            title.overflowMode = TextOverflowModes.Overflow;
            TMP_Text type = CreateText(panel, "TypeText", 18f, 48f, 381f, 29f,
                "INFANTRY", 19f, Lime, TextAlignmentOptions.MidlineLeft, true);
            List<CategoryVisual> categoryVisuals = BuildCategoryArtSets(
                panel, 16f, 82f, 385f, 184f, "InspectionArt");
            CreateText(panel, "LevelLabel", 17f, 276f, 208f, 32f,
                "LEVEL 12 / 20", 20f, White, TextAlignmentOptions.MidlineLeft, true);
            CreateText(panel, "LevelValue", 240f, 276f, 159f, 32f,
                "2,750 / 4,000", 17f, White, TextAlignmentOptions.MidlineRight, true);
            CreatePanel("LevelTrack", panel, 17f, 311f, 382f, 16f,
                new Color32(30, 40, 44, 255),
                new Color32(8, 14, 17, 255), Line, 3f);
            CreatePanel("LevelFill", panel, 20f, 314f, 220f, 10f,
                new Color32(174, 224, 64, 255),
                new Color32(103, 167, 36, 255), Color.clear, 0f);

            TMP_Text health = BuildStatRow(panel, "Health", 337f,
                "HEALTH", V3UiFoundationBuilder.MatchMedicalIconPath, "1,250");
            TMP_Text damage = BuildStatRow(panel, "Damage", 381f,
                "DAMAGE", V3UiFoundationBuilder.AttackIconPath, "85");
            TMP_Text range = BuildStatRow(panel, "Range", 425f,
                "RANGE", V3UiFoundationBuilder.MatchJumpIconPath, "25");
            TMP_Text speed = BuildStatRow(panel, "Speed", 469f,
                "SPEED", V3UiFoundationBuilder.MatchSpeedIconPath, "4.5");

            RectTransform upgrade = BuildActionButton(
                panel, "UpgradeButton", 16f, 613f, 187f, 69f,
                GreenTop, GreenBottom, Lime,
                "UPGRADE", V3UiFoundationBuilder.CommanderUpgradesIconPath);
            RectTransform equip = BuildActionButton(
                panel, "EquipButton", 214f, 613f, 187f, 69f,
                BlueTop, BlueBottom, Cyan,
                "EQUIP", V3UiFoundationBuilder.MatchSelectIconPath);
            AddRoute(upgrade, UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, false);
            AddRoute(equip, UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, false);

            RectTransform compatibility =
                CreateTopLeft("RuntimeCompatibility", panel, 0f, 0f, 1f, 1f);
            compatibility.gameObject.SetActive(false);
            TMP_Text description = CreateText(
                compatibility, "DescriptionText", 0f, 0f, 1f, 1f,
                string.Empty, 10f, White, TextAlignmentOptions.TopLeft, false);
            TMP_Text move = CreateText(
                compatibility, "MoveCapabilityText", 0f, 0f, 1f, 1f,
                string.Empty, 10f, White, TextAlignmentOptions.TopLeft, false);
            TMP_Text patrol = CreateText(
                compatibility, "PatrolCapabilityText", 0f, 0f, 1f, 1f,
                string.Empty, 10f, White, TextAlignmentOptions.TopLeft, false);
            TMP_Text attack = CreateText(
                compatibility, "AttackCapabilityText", 0f, 0f, 1f, 1f,
                string.Empty, 10f, White, TextAlignmentOptions.TopLeft, false);
            TMP_Text hold = CreateText(
                compatibility, "HoldCapabilityText", 0f, 0f, 1f, 1f,
                string.Empty, 10f, White, TextAlignmentOptions.TopLeft, false);

            SerializedObject inspectionObject = new(inspection);
            inspectionObject.FindProperty("titleText").objectReferenceValue = title;
            inspectionObject.FindProperty("typeText").objectReferenceValue = type;
            inspectionObject.FindProperty("descriptionText").objectReferenceValue = description;
            inspectionObject.FindProperty("healthValueText").objectReferenceValue = health;
            inspectionObject.FindProperty("damageValueText").objectReferenceValue = damage;
            inspectionObject.FindProperty("rangeValueText").objectReferenceValue = range;
            inspectionObject.FindProperty("speedValueText").objectReferenceValue = speed;
            inspectionObject.FindProperty("moveCapabilityText").objectReferenceValue = move;
            inspectionObject.FindProperty("patrolCapabilityText").objectReferenceValue = patrol;
            inspectionObject.FindProperty("attackCapabilityText").objectReferenceValue = attack;
            inspectionObject.FindProperty("holdCapabilityText").objectReferenceValue = hold;
            ConfigureCategoryVisuals(
                inspectionObject.FindProperty("categoryVisuals"), categoryVisuals);
            inspectionObject.ApplyModifiedPropertiesWithoutUndo();

            ArmoryRightContentView right = root.gameObject.AddComponent<ArmoryRightContentView>();
            SerializedObject rightObject = new(right);
            rightObject.FindProperty("inspectionPanel").objectReferenceValue = inspection;
            rightObject.ApplyModifiedPropertiesWithoutUndo();
            return new RightBindings(panel, inspection);
        }

        private static TMP_Text BuildStatRow(
            Transform parent, string name, float y,
            string label, string iconPath, string value)
        {
            RectTransform row = CreatePanel(
                name + "Row", parent, 16f, y, 385f, 42f,
                new Color32(21, 29, 33, 255),
                new Color32(7, 13, 16, 255), Line, 3f).rectTransform;
            Image icon = CreateImage("Icon", row, RequireSprite(iconPath), White);
            SetTopLeft(icon.rectTransform, 11f, 8f, 27f, 27f);
            CreateText(row, "Label", 47f, 3f, 210f, 36f,
                label, 17f, Muted, TextAlignmentOptions.MidlineLeft, true);
            return CreateText(row, "Value", 271f, 3f, 99f, 36f,
                value, 20f, Lime, TextAlignmentOptions.MidlineRight, true);
        }

        private static RectTransform BuildActionButton(
            Transform parent, string name,
            float x, float y, float width, float height,
            Color top, Color bottom, Color border,
            string label, string iconPath)
        {
            RectTransform rect = CreatePanel(
                name, parent, x, y, width, height,
                top, bottom, border, 3f).rectTransform;
            AddButton(rect.gameObject, rect.GetComponent<V3GradientGraphic>());
            Image icon = CreateImage("Icon", rect, RequireSprite(iconPath), White);
            SetTopLeft(icon.rectTransform, 13f, 15f, 39f, 39f);
            CreateText(rect, "Label", 58f, 4f, width - 69f, height - 8f,
                label, 20f, White, TextAlignmentOptions.Center, true);
            return rect;
        }

        private static FooterBindings BuildFooter(Transform root)
        {
            RectTransform bar = CreatePanel(
                "FooterBar", root, 6f, 824f, 1655f, 99f,
                DarkTop, DarkBottom, Line, 3f).rectTransform;
            RectTransform back = BuildFooterButton(
                bar, "BackButton", 0f, 0f, 303f, 99f,
                "BACK", V3UiFoundationBuilder.CommanderBackIconPath,
                RaisedTop, DarkBottom, Line);
            RectTransform profile = BuildFooterButton(
                bar, "CommanderProfileButton", 521f, 0f, 703f, 99f,
                "COMMANDER PROFILE", V3UiFoundationBuilder.MatchPlayerIconPath,
                BlueTop, BlueBottom, Cyan);
            AddRoute(back, UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            AddRoute(profile, UiShellRouteIntent.OpenMenuRoute, UIRoute.CommanderProfile, true);
            return new FooterBindings(bar, profile);
        }

        private static RectTransform BuildFooterButton(
            Transform parent, string name,
            float x, float y, float width, float height,
            string label, string iconPath,
            Color top, Color bottom, Color border)
        {
            RectTransform rect = CreatePanel(
                name, parent, x, y, width, height,
                top, bottom, border, 3f).rectTransform;
            AddButton(rect.gameObject, rect.GetComponent<V3GradientGraphic>());
            Image icon = CreateImage("Icon", rect, RequireSprite(iconPath), White);
            SetTopLeft(icon.rectTransform, width * .24f, 21f, 56f, 56f);
            CreateText(rect, "Label", width * .24f + 67f, 4f,
                width * .65f, height - 8f,
                label, 28f, White, TextAlignmentOptions.MidlineLeft, true);
            return rect;
        }

        private static void ConfigureCatalog(MiddleBindings middle, RightBindings right)
        {
            SerializedObject serialized = new(middle.List);
            serialized.FindProperty("unitPrefabRegistryConfig").objectReferenceValue = unitConfig;
            serialized.FindProperty("buildingPlacementConfig").objectReferenceValue = buildingConfig;
            serialized.FindProperty("contentRoot").objectReferenceValue = middle.Content;
            serialized.FindProperty("itemTemplate").objectReferenceValue = middle.Template;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            middle.List.SetInspectionPanel(right.Inspection);
        }

        private static void ConfigureLayouts(
            RectTransform headerSection,
            HeaderBindings header,
            RectTransform leftSection,
            RectTransform middleSection,
            MiddleBindings middle,
            RectTransform rightSection,
            RightBindings right,
            RectTransform footerSection,
            FooterBindings footer)
        {
            headerSection.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference,
                MainMenuV3SectionAlignment.TopLeft,
                new[] { header.Credits, header.Command, header.Settings },
                true,
                null,
                new[] { header.Bar });
            leftSection.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference, MainMenuV3SectionAlignment.TopLeft);
            middleSection.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference,
                MainMenuV3SectionAlignment.Center,
                new[] { middle.Filter, middle.Sort },
                true,
                null,
                new[] { middle.Panel, middle.Viewport });
            rightSection.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference,
                MainMenuV3SectionAlignment.TopRight);
            footerSection.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference,
                MainMenuV3SectionAlignment.BottomCenter,
                null,
                true,
                new[] { footer.Profile },
                new[] { footer.Bar });
        }

        private static List<CategoryVisual> BuildCategoryArtSets(
            Transform parent,
            float x, float y, float width, float height,
            string prefix)
        {
            var result = new List<CategoryVisual>(5);
            foreach (ArmoryCatalogCategory category in Enum.GetValues(typeof(ArmoryCatalogCategory)))
            {
                RectTransform root = CreatePanel(
                    $"{prefix}_{category}", parent, x, y, width, height,
                    new Color32(22, 31, 35, 255),
                    new Color32(6, 13, 16, 255), Line, 3f).rectTransform;
                root.gameObject.AddComponent<RectMask2D>();
                Image art = CreateImage("ArtImage", root, null, Color.white);
                Stretch(art.rectTransform);
                art.preserveAspect = false;
                AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = width / height;
                art.enabled = false;
                root.gameObject.SetActive(category == ArmoryCatalogCategory.Characters);
                result.Add(new CategoryVisual(category, root.gameObject, art));
            }
            return result;
        }

        private static void ConfigureCategoryVisuals(
            SerializedProperty property,
            IReadOnlyList<CategoryVisual> visuals)
        {
            property.arraySize = visuals.Count;
            for (int i = 0; i < visuals.Count; i++)
            {
                SerializedProperty entry = property.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("category").enumValueIndex = (int)visuals[i].Category;
                entry.FindPropertyRelative("backgroundRoot").objectReferenceValue = visuals[i].Root;
                entry.FindPropertyRelative("artImage").objectReferenceValue = visuals[i].Art;
            }
        }

        private static void AddRoute(
            RectTransform target,
            UiShellRouteIntent intent,
            UIRoute route,
            bool pushHistory)
        {
            UIShellRouteButtonView routeButton =
                target.gameObject.AddComponent<UIShellRouteButtonView>();
            routeButton.Configure(intent, route, pushHistory);
        }

        private static Button AddButton(GameObject target, Graphic graphic)
        {
            Button button = target.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.07f, 1.07f, 1.07f, 1f),
                pressedColor = new Color(.77f, .84f, .87f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(.35f, .35f, .35f, .6f),
                colorMultiplier = 1f,
                fadeDuration = .08f
            };
            return button;
        }

        private static void CreateRankMark(Transform parent)
        {
            RectTransform holder = CreateTopLeft("RankMark", parent, 293f, 7f, 49f, 68f);
            V3StarGraphic star = CreateRect(
                "Star", holder, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(38f, 38f), new Vector2(0f, 17f))
                .gameObject.AddComponent<V3StarGraphic>();
            star.color = new Color32(242, 172, 8, 255);
            CreateChevronMark(holder, 7f, 39f, new Color32(242, 172, 8, 255));
            CreateChevronMark(holder, 7f, 51f, new Color32(242, 172, 8, 255));
        }

        private static void BuildBarCoin(Transform parent, Color color)
        {
            V3RingGraphic ring = CreateRect(
                "Ring", parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(52f, 52f), Vector2.zero)
                .gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 4f);
            float[] heights = { 16f, 28f, 38f, 24f };
            for (int i = 0; i < heights.Length; i++)
            {
                RectTransform bar = CreateRect(
                    "Bar" + i, parent,
                    new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(5f, heights[i]),
                    new Vector2(-15f + i * 10f, -8f + heights[i] * .5f));
                bar.gameObject.AddComponent<Image>().color = color;
            }
        }

        private static void BuildCommandHex(Transform parent, Color color)
        {
            Image icon = CreateImage(
                "Command", parent,
                RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath), color);
            SetTopLeft(icon.rectTransform, 3f, 3f, 52f, 52f);
        }

        private static void CreateTriangle(Transform parent, float x, float y, Color color)
        {
            RectTransform left = CreateTopLeft(
                "ChevronLeft", parent, x, y, 12f, 3f);
            left.gameObject.AddComponent<Image>().color = color;
            left.localRotation = Quaternion.Euler(0f, 0f, -36f);
            RectTransform right = CreateTopLeft(
                "ChevronRight", parent, x + 8f, y, 12f, 3f);
            right.gameObject.AddComponent<Image>().color = color;
            right.localRotation = Quaternion.Euler(0f, 0f, 36f);
        }

        private static RectTransform[] CreateChevronMark(Transform parent, float x, float y, Color color)
        {
            RectTransform left = CreateTopLeft("ChevronLeft", parent, x, y, 18f, 4f);
            left.gameObject.AddComponent<Image>().color = color;
            left.localRotation = Quaternion.Euler(0f, 0f, 38f);
            RectTransform right = CreateTopLeft("ChevronRight", parent, x + 13f, y, 18f, 4f);
            right.gameObject.AddComponent<Image>().color = color;
            right.localRotation = Quaternion.Euler(0f, 0f, -38f);
            return new[] { left, right };
        }

        private static RectTransform CreateSection(
            string name,
            Transform root,
            UIShellContentSectionId id,
            ICollection<UIShellContentSectionsView.SectionReference> sections)
        {
            RectTransform section = CreateRect(
                name, root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            sections.Add(new UIShellContentSectionsView.SectionReference(id, section.gameObject));
            return section;
        }

        private static V3GradientGraphic CreatePanel(
            string name,
            Transform parent,
            float x, float y, float width, float height,
            Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            return graphic;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float x, float y, float width, float height,
            string value,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = bold ? boldFont : mediumFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.color = color;
            text.text = value;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(
            string name, Transform parent, Sprite sprite, Color color)
        {
            Image image = V3UiPrefabFactory.CreateImage(
                name, parent, sprite, color, false);
            image.preserveAspect = true;
            return image;
        }

        private static Image CreateSolid(
            string name, Transform parent,
            float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateTopLeft(
            string name, Transform parent,
            float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(
                name, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static RectTransform CreateRect(
            string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            return V3UiPrefabFactory.CreateRect(
                name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition);
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

        private static void SetTopRight(
            RectTransform rect, float right, float y, float width, float height, float rotation)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void StretchHorizontal(
            RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -y);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.localScale = Vector3.one;
        }

        private static void StretchHorizontalFraction(
            RectTransform rect, float left, float maxFraction, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(maxFraction, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -y);
            rect.sizeDelta = new Vector2(-left, height);
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
            rect.localRotation = Quaternion.identity;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing SCN-19 sprite: {path}");
            return sprite;
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            background = RequireSprite(BackgroundPath);
            compatibilityFrame = RequireSprite(V3UiFoundationBuilder.PanelPath);
            unitConfig = AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnitConfigPath);
            buildingConfig = AssetDatabase.LoadAssetAtPath<ScriptableObject>(BuildingConfigPath);
            if (boldFont == null || mediumFont == null ||
                unitConfig == null || buildingConfig == null)
            {
                throw new FileNotFoundException("SCN-19 fonts or catalog configs are missing.");
            }
        }

        private readonly struct NavigationBinding
        {
            public NavigationBinding(
                ArmoryCatalogCategory category, Button button, Image frame)
            {
                Category = category;
                Button = button;
                Frame = frame;
            }

            public ArmoryCatalogCategory Category { get; }
            public Button Button { get; }
            public Image Frame { get; }
        }

        private readonly struct CategoryVisual
        {
            public CategoryVisual(
                ArmoryCatalogCategory category, GameObject root, Image art)
            {
                Category = category;
                Root = root;
                Art = art;
            }

            public ArmoryCatalogCategory Category { get; }
            public GameObject Root { get; }
            public Image Art { get; }
        }

        private readonly struct HeaderBindings
        {
            public HeaderBindings(
                RectTransform bar,
                RectTransform credits,
                RectTransform command,
                RectTransform settings)
            {
                Bar = bar;
                Credits = credits;
                Command = command;
                Settings = settings;
            }

            public RectTransform Bar { get; }
            public RectTransform Credits { get; }
            public RectTransform Command { get; }
            public RectTransform Settings { get; }
        }

        private readonly struct MiddleBindings
        {
            public MiddleBindings(
                RectTransform panel,
                RectTransform filter,
                RectTransform sort,
                RectTransform viewport,
                RectTransform content,
                ArmoryContentListView list,
                ArmoryCatalogItemView template)
            {
                Panel = panel;
                Filter = filter;
                Sort = sort;
                Viewport = viewport;
                Content = content;
                List = list;
                Template = template;
            }

            public RectTransform Panel { get; }
            public RectTransform Filter { get; }
            public RectTransform Sort { get; }
            public RectTransform Viewport { get; }
            public RectTransform Content { get; }
            public ArmoryContentListView List { get; }
            public ArmoryCatalogItemView Template { get; }
        }

        private readonly struct RightBindings
        {
            public RightBindings(
                RectTransform panel, ArmoryInspectionPanelView inspection)
            {
                Panel = panel;
                Inspection = inspection;
            }

            public RectTransform Panel { get; }
            public ArmoryInspectionPanelView Inspection { get; }
        }

        private readonly struct FooterBindings
        {
            public FooterBindings(RectTransform bar, RectTransform profile)
            {
                Bar = bar;
                Profile = profile;
            }

            public RectTransform Bar { get; }
            public RectTransform Profile { get; }
        }
    }
}
#endif
