#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CommandFeedV3PrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN18_CommandFeedContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string CityArtPath = "Assets/Game/Art/UI/V3Shared/Inbox/SCN15_NorthBridgeIntel_V3.png";
        private const string MapArtPath = "Assets/Game/Art/UI/V3Shared/IntelReveal/POP08_EvidenceAtlas_V3.png";
        private const string SquadArtPath = "Assets/Game/Art/UI/V3Shared/RewardUnlock/POP04_RangerSquad_V3.png";
        private const string CivilianArtPath = "Assets/Game/Art/UI/V3Shared/Events/SCN16_ARIAFieldTrials_V3.png";
        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color Border = new Color32(63, 77, 82, 255);
        private static readonly Color DarkTop = new Color32(25, 37, 41, 255);
        private static readonly Color DarkBottom = new Color32(4, 11, 14, 255);
        private static readonly Color Cyan = new Color32(0, 184, 235, 255);
        private static readonly Color Green = new Color32(112, 205, 44, 255);
        private static readonly Color Amber = new Color32(250, 174, 0, 255);
        private static readonly Color Orange = new Color32(241, 91, 18, 255);
        private static readonly Color Purple = new Color32(158, 88, 232, 255);
        private static readonly Color Text = new Color32(244, 245, 242, 255);
        private static readonly Color Muted = new Color32(165, 177, 180, 255);
        private static TMP_FontAsset bold;
        private static TMP_FontAsset medium;
        private static V3UiArtCatalog catalog;
        private static Sprite operationsIcon;
        private static Sprite ariaPortrait;
        private static Sprite warningIcon;
        private static Sprite rewardIcon;
        private static Sprite upgradesIcon;
        private static Sprite intelIcon;
        private static Sprite civilianIcon;
        private static Sprite tankIcon;
        private static Sprite[] rowArt;

        [MenuItem("Game/UI/V3/Rebuild SCN-18 Command Feed")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();
            RectTransform root = Rect("SCN18_CommandFeedContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image black = Image("CanvasBlack", root, null, Color.black, false); Stretch(black.rectTransform);
            RectTransform composition = TopLeft("CommandFeedComposition", root, 0f, 0f, Reference.x, Reference.y);
            var rightTargets = new List<RectTransform>();
            var widthTargets = new List<RectTransform>();

            BuildHeader(composition, rightTargets, widthTargets, out TMP_Text credits, out TMP_Text command);
            BuildFilters(composition, out Button[] filters, out V3GradientGraphic[] filterGradients);
            BuildFeed(composition, rightTargets, widthTargets, out TMP_Text status, out Button pause, out TMP_Text pauseLabel,
                out Button search, out RectTransform[] rows, out CommandFeedCategory[] categories);
            BuildRightRail(composition, rightTargets);

            composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(
                Reference, MainMenuV3SectionAlignment.Center, rightTargets.ToArray(), true, null, widthTargets.ToArray());
            CommandFeedScreenView view = root.gameObject.AddComponent<CommandFeedScreenView>();
            view.Configure(credits, command, status, filters, filterGradients, rows, categories, pause, pauseLabel, search);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Command Feed V3 prefab: {PrefabPath}");
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[CommandFeedV3PrefabBuilder] result=Passed filters=5 rows=5 responsive=True sharedArt=True");
        }

        [MenuItem("Game/UI/V3/Validate SCN-18 Command Feed")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Command Feed V3 prefab: {PrefabPath}");
            CommandFeedScreenView view = prefab.GetComponent<CommandFeedScreenView>();
            if (view == null || view.FilterButtons?.Length != 5 || view.FeedRows?.Length != 5 ||
                view.PauseButton == null || view.SearchButton == null)
                throw new MissingReferenceException("Command Feed interaction bindings are incomplete.");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != Reference)
                throw new InvalidOperationException("Command Feed must expand across 16:9 and 20:9 canvases.");
            Require(prefab.transform, "CommandFeedComposition/Header/WarlineLogo");
            Require(prefab.transform, "CommandFeedComposition/FilterRail/AllFilter");
            Require(prefab.transform, "CommandFeedComposition/FeedRows/FeedRow_4");
            Require(prefab.transform, "CommandFeedComposition/RightRail/AriaPanel/AriaPortrait");
            RequireRoute(prefab.transform, "CommandFeedComposition/FilterRail/BackButton", UIRoute.MainMenu, UiShellRouteIntent.BackMenuRoute);
            RequireRoute(prefab.transform, "CommandFeedComposition/RightRail/ViewOperationButton", UIRoute.Operations, UiShellRouteIntent.OpenMenuRoute);
            RequireRoute(prefab.transform, "CommandFeedComposition/RightRail/OpenIntelButton", UIRoute.Inbox, UiShellRouteIntent.OpenMenuRoute);
            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 25)
                throw new InvalidOperationException($"Command Feed requires V3 gradient chrome; found {gradients.Length}.");
            foreach (V3GradientGraphic gradient in gradients)
            {
                SerializedObject serialized = new(gradient);
                Color border = serialized.FindProperty("borderColor").colorValue;
                float width = serialized.FindProperty("borderWidth").floatValue;
                if (border.a > .01f && Mathf.Abs(width - 3f) > .001f)
                    throw new InvalidOperationException($"Command Feed border is not 3 px: {gradient.name}");
            }
            Debug.Log($"[CommandFeedV3Validation] result=Passed gradients={gradients.Length} filters=5 rows=5");
        }

        private static void BuildHeader(Transform root, List<RectTransform> rightTargets, List<RectTransform> widthTargets,
            out TMP_Text credits, out TMP_Text command)
        {
            RectTransform header = TopLeft("Header", root, 11f, 9f, 1650f, 90f);
            RectTransform logo = TopLeft("WarlineLogo", header, 0f, 0f, 315f, 90f);
            Panel(logo, DarkTop, DarkBottom, Border);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);
            RectTransform title = TopLeft("TitlePanel", header, 330f, 0f, 750f, 90f);
            Panel(title, DarkTop, DarkBottom, Border);
            TMP_Text titleText = TextBlock("Title", title, "COMMAND FEED", 47f, bold, TextAlignmentOptions.MidlineLeft, Text);
            TopLeft(titleText.rectTransform, 24f, 6f, 700f, 78f);
            widthTargets.Add(title);
            Button creditChip = Chip(header, "CreditsChip", 1095f, 0f, 230f, catalog.CreditsIcon, "CREDITS", "24,750", out credits);
            Button commandChip = Chip(header, "CommandChip", 1335f, 0f, 315f, catalog.CommandIcon, "COMMAND", "8,430", out command);
            rightTargets.Add(creditChip.transform as RectTransform);
            rightTargets.Add(commandChip.transform as RectTransform);
        }

        private static void BuildFilters(Transform root, out Button[] filters, out V3GradientGraphic[] gradients)
        {
            RectTransform rail = TopLeft("FilterRail", root, 11f, 111f, 315f, 807f);
            string[] names = { "ALL", "OPERATIONS", "ARIA", "ALERTS", "REWARDS" };
            string[] counts = { "24", "8", "5", "4", "7" };
            Sprite[] icons = { catalog.AttackIcon, operationsIcon, ariaPortrait, warningIcon, rewardIcon };
            Color[] colors = { Cyan, Green, Cyan, Orange, Amber };
            filters = new Button[5];
            gradients = new V3GradientGraphic[5];
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform rect = TopLeft(names[i].Substring(0, 1) + names[i].Substring(1).ToLowerInvariant() + "Filter", rail, 0f, i * 103f, 315f, 91f);
                gradients[i] = Panel(rect, i == 0 ? new Color32(25, 135, 214, 255) : DarkTop,
                    i == 0 ? new Color32(4, 62, 111, 255) : DarkBottom, i == 0 ? Cyan : Border);
                filters[i] = rect.gameObject.AddComponent<Button>();
                filters[i].targetGraphic = gradients[i];
                Image icon = Image("Icon", rect, icons[i], Color.white, false);
                TopLeft(icon.rectTransform, 18f, 17f, 58f, 58f); icon.preserveAspect = true;
                TMP_Text label = TextBlock("Label", rect, names[i], 27f, bold, TextAlignmentOptions.MidlineLeft, Text);
                TopLeft(label.rectTransform, 92f, 10f, 165f, 70f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 20f;
                label.fontSizeMax = 27f;
                label.overflowMode = TextOverflowModes.Overflow;
                RectTransform count = TopLeft("Count", rect, 258f, 24f, 44f, 43f);
                Panel(count, DarkTop, DarkBottom, colors[i]);
                TMP_Text countText = TextBlock("Label", count, counts[i], 20f, bold, TextAlignmentOptions.Center, colors[i]); Stretch(countText.rectTransform);
            }
            Button back = GradientButton("BackButton", rail, 0f, 721f, 315f, 86f, DarkTop, DarkBottom, Border);
            Image backIcon = Image("Icon", back.transform, RequireSprite(V3UiFoundationBuilder.CommanderBackIconPath), Color.white, false);
            TopLeft(backIcon.rectTransform, 20f, 18f, 48f, 48f); backIcon.preserveAspect = true;
            TMP_Text backText = TextBlock("Label", back.transform, "BACK", 30f, bold, TextAlignmentOptions.Center, Text);
            TopLeft(backText.rectTransform, 65f, 5f, 225f, 76f);
            Route(back, UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
        }

        private static void BuildFeed(Transform root, List<RectTransform> rightTargets, List<RectTransform> widthTargets,
            out TMP_Text status, out Button pause, out TMP_Text pauseLabel, out Button search,
            out RectTransform[] rows, out CommandFeedCategory[] categories)
        {
            RectTransform header = TopLeft("FeedHeader", root, 342f, 111f, 891f, 55f);
            Panel(header, DarkTop, DarkBottom, Border);
            TMP_Text live = TextBlock("LiveFeed", header, "LIVE FEED", 26f, bold, TextAlignmentOptions.MidlineLeft, Text);
            TopLeft(live.rectTransform, 17f, 3f, 155f, 49f);
            status = TextBlock("LiveStatus", header, "●  UPDATING...", 18f, bold, TextAlignmentOptions.MidlineLeft, Green);
            TopLeft(status.rectTransform, 160f, 5f, 330f, 45f);
            pause = GradientButton("PauseButton", header, 781f, 7f, 48f, 41f, DarkTop, DarkBottom, Border);
            pauseLabel = TextBlock("Label", pause.transform, "||", 23f, bold, TextAlignmentOptions.Center, Text); Stretch(pauseLabel.rectTransform);
            search = GradientButton("SearchButton", header, 837f, 7f, 47f, 41f, DarkTop, DarkBottom, Border);
            BuildSearchGlyph(search.transform);
            rightTargets.Add(pause.transform as RectTransform); rightTargets.Add(search.transform as RectTransform);
            widthTargets.Add(header);

            RectTransform container = TopLeft("FeedRows", root, 342f, 176f, 891f, 725f);
            widthTargets.Add(container);
            string[] time = { "10:24", "10:18", "10:05", "09:52", "09:41" };
            string[] ago = { "2m ago", "8m ago", "21m ago", "34m ago", "45m ago" };
            string[] title = { "OPERATION COMPLETED", "ARIA REPORT", "RANGER SQUAD LEVEL UP", "CIVILIAN TRUST INCREASED", "INTEL REVEAL AVAILABLE" };
            string[] subtitle = { "OLD MARKET SECURED", "HOSTILE CONVOY DETECTED", "RANGER SQUAD REACHED LEVEL 12", "LOCAL POPULATION SUPPORT UP", "NEW INTEL CAN BE ACCESSED" };
            string[] body = {
                "Our forces have captured and secured Old Market. Area is now under control.",
                "ARIA sensors detected a hostile convoy moving through the Industrial District.",
                "Ranger Squad has gained experience and is now Level 12.",
                "Your actions are earning trust. Civilian cooperation has increased.",
                "New intel has been uncovered. Check the Intel Archive for details."
            };
            string[] tag = { "SUCCESS", "CONVOY", "LVL 12", "TRUST +15%", "NEW INTEL" };
            Color[] accents = { Green, Cyan, Purple, Amber, Orange };
            Sprite[] icons = { catalog.AttackIcon, ariaPortrait, upgradesIcon, civilianIcon, intelIcon };
            categories = new[] { CommandFeedCategory.Operations, CommandFeedCategory.Aria, CommandFeedCategory.Rewards, CommandFeedCategory.Operations, CommandFeedCategory.Alerts };
            rows = new RectTransform[5];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = TopLeft("FeedRow_" + i, container, 0f, i * 143f, 891f, 135f);
                Horizontal(row, 0f, 0f, i * 143f, 135f);
                Panel(row, DarkTop, DarkBottom, accents[i]);
                rows[i] = row;
                RectTransform timePanel = TopLeft("Time", row, 3f, 3f, 101f, 129f);
                Panel(timePanel, DarkTop, DarkBottom, Border);
                TMP_Text clock = TextBlock("Clock", timePanel, time[i], 28f, bold, TextAlignmentOptions.Center, Text); TopLeft(clock.rectTransform, 2f, 22f, 97f, 44f);
                TMP_Text agoText = TextBlock("Ago", timePanel, ago[i], 17f, medium, TextAlignmentOptions.Center, Muted); TopLeft(agoText.rectTransform, 2f, 67f, 97f, 35f);
                RectTransform iconPanel = TopLeft("IconPanel", row, 106f, 3f, 102f, 129f);
                Panel(iconPanel, DarkTop, DarkBottom, Border);
                Image icon = Image("Icon", iconPanel, icons[i], Color.white, false); TopLeft(icon.rectTransform, 22f, 24f, 58f, 58f); icon.preserveAspect = true;
                RectTransform copy = TopLeft("Copy", row, 211f, 3f, 346f, 129f);
                Horizontal(copy, 211f, 334f, 3f, 129f);
                TMP_Text heading = TextBlock("Title", copy, title[i], 22f, bold, TextAlignmentOptions.MidlineLeft, accents[i]); TopLeft(heading.rectTransform, 12f, 5f, 325f, 34f);
                TMP_Text sub = TextBlock("Subtitle", copy, subtitle[i], 16f, bold, TextAlignmentOptions.MidlineLeft, Text); TopLeft(sub.rectTransform, 12f, 36f, 325f, 27f);
                TMP_Text description = TextBlock("Body", copy, body[i], 15f, medium, TextAlignmentOptions.TopLeft, Muted); TopLeft(description.rectTransform, 12f, 65f, 325f, 57f); description.textWrappingMode = TextWrappingModes.Normal;
                RectTransform artClip = TopLeft("ArtClip", row, 560f, 3f, 328f, 129f); artClip.gameObject.AddComponent<RectMask2D>();
                TopRight(artClip, 3f, 3f, 328f, 129f);
                Image art = Image("Art", artClip, rowArt[i], Color.white, false); Stretch(art.rectTransform);
                AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio = rowArt[i].rect.width / rowArt[i].rect.height;
                RectTransform tagPanel = TopLeft("Tag", row, 764f, 91f, 115f, 34f); Panel(tagPanel, DarkTop, DarkBottom, accents[i]);
                TopRight(tagPanel, 9f, 91f, 115f, 34f);
                TMP_Text tagText = TextBlock("Label", tagPanel, tag[i], 16f, bold, TextAlignmentOptions.Center, accents[i]); Stretch(tagText.rectTransform);
                if (i == 0)
                {
                    TopLeft(tagText.rectTransform, 4f, 0f, 82f, 34f);
                    Image check = Image("Check", tagPanel, RequireSprite(V3UiFoundationBuilder.CommanderCheckIconPath), Green, false);
                    TopLeft(check.rectTransform, 87f, 5f, 24f, 24f);
                    check.preserveAspect = true;
                }
            }
        }

        private static void BuildRightRail(Transform root, List<RectTransform> rightTargets)
        {
            RectTransform rail = TopLeft("RightRail", root, 1248f, 111f, 413f, 807f);
            rightTargets.Add(rail);
            RectTransform aria = TopLeft("AriaPanel", rail, 0f, 0f, 413f, 307f); Panel(aria, DarkTop, DarkBottom, Cyan);
            TMP_Text ariaName = TextBlock("Name", aria, "ARIA", 36f, bold, TextAlignmentOptions.MidlineLeft, Cyan); TopLeft(ariaName.rectTransform, 17f, 4f, 128f, 51f);
            TMP_Text ariaRole = TextBlock("Role", aria, "TACTICAL ANALYST", 19f, bold, TextAlignmentOptions.MidlineLeft, Cyan); TopLeft(ariaRole.rectTransform, 17f, 49f, 168f, 35f);
            ariaRole.enableAutoSizing = true;
            ariaRole.fontSizeMin = 14f;
            ariaRole.fontSizeMax = 19f;
            ariaRole.overflowMode = TextOverflowModes.Overflow;
            RectTransform speech = TopLeft("Status", aria, 15f, 91f, 170f, 147f); Panel(speech, DarkTop, DarkBottom, Cyan);
            TMP_Text speechText = TextBlock("Text", speech, "Monitoring all command channels.\nProviding real-time analysis and tactical recommendations.", 14f, medium, TextAlignmentOptions.TopLeft, Text);
            TopLeft(speechText.rectTransform, 13f, 13f, 144f, 121f); speechText.textWrappingMode = TextWrappingModes.Normal;
            RectTransform portraitClip = TopLeft("AriaPortrait", aria, 188f, 7f, 215f, 293f); portraitClip.gameObject.AddComponent<RectMask2D>();
            Image portrait = Image("Portrait", portraitClip, ariaPortrait, Color.white, false); Stretch(portrait.rectTransform); portrait.preserveAspect = true;
            RectTransform situation = TopLeft("SituationPanel", rail, 0f, 318f, 413f, 300f); Panel(situation, DarkTop, DarkBottom, Border);
            TMP_Text situationTitle = TextBlock("Title", situation, "LIVE SITUATION", 27f, bold, TextAlignmentOptions.MidlineLeft, Text); TopLeft(situationTitle.rectTransform, 18f, 6f, 245f, 48f);
            TMP_Text asOf = TextBlock("AsOf", situation, "AS OF 10:24", 17f, bold, TextAlignmentOptions.MidlineRight, Muted); TopLeft(asOf.rectTransform, 270f, 8f, 125f, 44f);
            BuildSituationRow(situation, 62f, warningIcon, "THREAT LEVEL", "HIGH", Orange, .87f);
            BuildSituationRow(situation, 118f, operationsIcon, "REGION STABILITY", "68%", Green, .68f);
            BuildSituationRow(situation, 174f, civilianIcon, "CIVILIANS PROTECTED", "8,642", Cyan, 0f);
            BuildSituationRow(situation, 230f, tankIcon, "ACTIVE OPERATIONS", "3", Orange, 0f);
            Button operation = GradientButton("ViewOperationButton", rail, 0f, 628f, 413f, 86f, new Color32(25, 132, 208, 255), new Color32(4, 67, 119, 255), Cyan);
            AddActionContent(operation, catalog.AttackIcon, "VIEW OPERATION", Text);
            Route(operation, UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, true);
            Button intel = GradientButton("OpenIntelButton", rail, 0f, 723f, 413f, 84f, new Color32(214, 125, 6, 255), new Color32(116, 57, 0, 255), Amber);
            AddActionContent(intel, intelIcon, "OPEN INTEL", Text);
            Route(intel, UiShellRouteIntent.OpenMenuRoute, UIRoute.Inbox, true);
        }

        private static void BuildSituationRow(Transform root, float y, Sprite sprite, string label, string value, Color accent, float fill)
        {
            Image icon = Image("Icon", root, sprite, Color.white, false); TopLeft(icon.rectTransform, 18f, y + 4f, 42f, 42f); icon.preserveAspect = true;
            TMP_Text labelText = TextBlock("Label", root, label, 18f, bold, TextAlignmentOptions.MidlineLeft, Text); TopLeft(labelText.rectTransform, 72f, y, 225f, 33f);
            TMP_Text valueText = TextBlock("Value", root, value, 18f, bold, TextAlignmentOptions.MidlineRight, accent); TopLeft(valueText.rectTransform, 315f, y, 80f, 33f);
            if (fill <= 0f) return;
            Image track = Solid("Track", root, 72f, y + 37f, 270f, 9f, new Color32(31, 45, 49, 255));
            Solid("Fill", track.transform, 0f, 0f, 270f * fill, 9f, accent);
        }

        private static void AddActionContent(Button button, Sprite sprite, string label, Color color)
        {
            Image icon = Image("Icon", button.transform, sprite, Color.white, false); TopLeft(icon.rectTransform, 28f, 18f, 50f, 50f); icon.preserveAspect = true;
            TMP_Text text = TextBlock("Label", button.transform, label, 27f, bold, TextAlignmentOptions.Center, color); TopLeft(text.rectTransform, 81f, 7f, 292f, 72f);
        }

        private static void BuildSearchGlyph(Transform parent)
        {
            RectTransform ring = Rect("Ring", parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(21f, 21f), new Vector2(-3f, 3f));
            ring.gameObject.AddComponent<V3RingGraphic>().Configure(Text, 3f, 24);
            Image handle = Solid("Handle", parent, 27f, 27f, 14f, 3f, Text); handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        private static Button Chip(Transform root, string name, float x, float y, float w, Sprite iconSprite, string label, string value, out TMP_Text valueText)
        {
            Button button = GradientButton(name, root, x, y, w, 90f, DarkTop, DarkBottom, Border);
            Image icon = Image("Icon", button.transform, iconSprite, Color.white, false); TopLeft(icon.rectTransform, 14f, 17f, 56f, 56f); icon.preserveAspect = true;
            TMP_Text labelText = TextBlock("Label", button.transform, label, 18f, bold, TextAlignmentOptions.MidlineLeft, Text); TopLeft(labelText.rectTransform, 79f, 8f, w - 89f, 30f);
            valueText = TextBlock("Value", button.transform, value, 31f, bold, TextAlignmentOptions.MidlineLeft, Text); TopLeft(valueText.rectTransform, 79f, 36f, w - 89f, 45f);
            return button;
        }

        private static void LoadAssets()
        {
            foreach (string artPath in new[] { CityArtPath, MapArtPath, SquadArtPath, CivilianArtPath })
                ConfigureSprite(artPath, 2048);
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (bold == null || medium == null) throw new MissingReferenceException("Command Feed fonts are missing.");
            catalog = V3UiFoundationBuilder.RequireCatalog();
            operationsIcon = RequireSprite("Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png");
            ariaPortrait = RequireSprite(V3UiFoundationBuilder.SharedAriaPortraitPath);
            warningIcon = RequireSprite(V3UiFoundationBuilder.OperationsWarningIconPath);
            rewardIcon = RequireSprite(V3UiFoundationBuilder.CommanderCrateIconPath);
            upgradesIcon = RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath);
            intelIcon = RequireSprite(V3UiFoundationBuilder.OperationsIntelIconPath);
            civilianIcon = RequireSprite(V3UiFoundationBuilder.MatchCiviliansIconPath);
            tankIcon = RequireSprite(V3UiFoundationBuilder.OperationsTankIconPath);
            rowArt = new[] { RequireSprite(CityArtPath), RequireSprite(MapArtPath), RequireSprite(SquadArtPath), RequireSprite(CivilianArtPath), RequireSprite(MapArtPath) };
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Command Feed shared art: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static V3GradientGraphic Panel(RectTransform rect, Color top, Color bottom, Color border)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.ConfigureCorners(Color.Lerp(top, Color.white, .04f), top, Color.Lerp(bottom, Color.black, .1f), bottom, border, border.a > .01f ? 3f : 0f);
            return graphic;
        }

        private static Button GradientButton(string name, Transform parent, float x, float y, float w, float h, Color top, Color bottom, Color border)
        {
            RectTransform rect = TopLeft(name, parent, x, y, w, h); V3GradientGraphic graphic = Panel(rect, top, bottom, border);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; return button;
        }

        private static void Route(Button button, UiShellRouteIntent intent, UIRoute route, bool push) =>
            button.gameObject.AddComponent<UIShellRouteButtonView>().Configure(intent, route, push);
        private static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast) => V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);
        private static Image Solid(string name, Transform parent, float x, float y, float w, float h, Color color)
        { Image image = Image(name, parent, null, color, false); TopLeft(image.rectTransform, x, y, w, h); return image; }
        private static TMP_Text TextBlock(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        { RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200f, 60f), Vector2.zero); TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.alignment = alignment; text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text; }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position) => V3UiPrefabFactory.CreateRect(name, parent, min, max, size, position);
        private static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h) { RectTransform rect = Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(w, h), new Vector2(x, -y)); rect.pivot = new Vector2(0f, 1f); return rect; }
        private static void TopLeft(RectTransform rect, float x, float y, float w, float h) { rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, -y); }
        private static void TopRight(RectTransform rect, float right, float y, float w, float h) { rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(1f, 1f); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(-right, -y); }
        private static void Horizontal(RectTransform rect, float left, float right, float y, float h) { rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f); rect.pivot = new Vector2(0f, 1f); rect.sizeDelta = new Vector2(-(left + right), h); rect.anchoredPosition = new Vector2(left, -y); }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
        private static Sprite RequireSprite(string path) { Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (sprite == null) throw new FileNotFoundException($"Missing Command Feed shared art: {path}"); return sprite; }
        private static Transform Require(Transform root, string path) { Transform child = root.Find(path); if (child == null) throw new MissingReferenceException($"Command Feed is missing {path}."); return child; }
        private static void RequireRoute(Transform root, string path, UIRoute route, UiShellRouteIntent intent) { UIShellRouteButtonView view = Require(root, path).GetComponent<UIShellRouteButtonView>(); if (view == null || view.Route != route || view.Intent != intent) throw new InvalidOperationException($"Command Feed route mismatch: {path}"); }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                content = sceneRoot.GetComponentInChildren<UIShellContentView>(true);
                if (content != null) break;
            }
            if (content == null) throw new InvalidOperationException("Menu scene is missing UIShellContentView.");
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("commandFeedContentPrefab");
            if (property == null) throw new MissingFieldException(nameof(UIShellContentView), "commandFeedContentPrefab");
            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
