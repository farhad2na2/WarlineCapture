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
    public static class RankingV3PrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN17_RankingContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RankingIconPath = "Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/Cleaned/ui_left_nav_icon_ranking.png";
        private const string RegionIconPath = V3UiFoundationBuilder.OperationsMapPinIconPath;
        private const string FriendsIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_civilian_group.png";
        private const string SeasonIconPath = "Assets/Game/Art/UI/Icons/scn09_icon_time_clock.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string DaliaPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_dalia.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly string[] PortraitPaths =
        {
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_Alt_01_Marksman_Action_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Female_02_Alt_01_Rifle_Action_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Contractor_Male_02_Rifle_Action_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Pilot_Female_01_CompactPistol_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Insurgent_Male_02_MachineGun_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_02_Alt_02_Rifleman_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Ghillie_Male_01_RocketLauncher_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Insurgent_Female_01_Rifle_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Card_512.png",
            "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Leader_Male_01_FieldCommander_Card_512.png"
        };

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(65, 78, 83, 255);
        private static readonly Color DarkTop = new Color32(26, 37, 41, 255);
        private static readonly Color DarkBottom = new Color32(4, 11, 14, 255);
        private static readonly Color RaisedTop = new Color32(45, 54, 57, 255);
        private static readonly Color RaisedBottom = new Color32(12, 20, 23, 255);
        private static readonly Color Cyan = new Color32(0, 184, 235, 255);
        private static readonly Color BlueTop = new Color32(24, 129, 205, 255);
        private static readonly Color BlueBottom = new Color32(2, 64, 116, 255);
        private static readonly Color Amber = new Color32(250, 174, 0, 255);
        private static readonly Color Green = new Color32(111, 185, 52, 255);
        private static readonly Color Red = new Color32(239, 72, 29, 255);
        private static readonly Color Bronze = new Color32(190, 91, 37, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(173, 181, 181, 255);

        private static TMP_FontAsset bold;
        private static TMP_FontAsset medium;
        private static V3UiArtCatalog catalog;
        private static Sprite rankingIcon;
        private static Sprite regionIcon;
        private static Sprite friendsIcon;
        private static Sprite seasonIcon;
        private static Sprite districtIcon;
        private static Sprite operationsIcon;
        private static Sprite dalia;
        private static Sprite[] portraits;

        [MenuItem("Game/UI/V3/Rebuild SCN-17 Ranking")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform root = Rect("SCN17_RankingContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RankingV3View view = root.gameObject.AddComponent<RankingV3View>();
            Image black = Image("CanvasBlack", root, null, Color.black); Stretch(black.rectTransform);
            RectTransform composition = TopLeft("RankingComposition", root, 0, 0, ReferenceResolution.x, ReferenceResolution.y);

            var rightTargets = new List<RectTransform>();
            var widthTargets = new List<RectTransform>();
            BuildHeader(composition, rightTargets, widthTargets, out TMP_Text credits, out TMP_Text command);
            BuildTabs(composition, out Button[] categoryButtons, out V3GradientGraphic[] categoryGradients);

            GameObject[] bodies = new GameObject[4];
            bodies[0] = BuildGlobalBody(composition, rightTargets, widthTargets, out Button viewRewards);
            bodies[1] = BuildAlternateBody(composition, "REGION RANKING", "EASTERN SAHRIN", "Your region is ranked 8 of 42 active command zones.", Cyan, widthTargets);
            bodies[2] = BuildAlternateBody(composition, "FRIENDS RANKING", "COMMAND NETWORK", "Connect commanders to compare operation score and civilian protection.", Green, widthTargets);
            bodies[3] = BuildAlternateBody(composition, "SEASON REWARDS", "STEEL RESOLVE", "Reach the next division to unlock the highlighted commander reward tier.", Amber, widthTargets);
            for (int i = 1; i < bodies.Length; i++) bodies[i].SetActive(false);

            MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center, rightTargets.ToArray(), true, null, widthTargets.ToArray());
            view.Configure(credits, command, categoryButtons, categoryGradients, bodies, viewRewards);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null) throw new InvalidOperationException($"Failed to save Ranking V3 prefab: {PrefabPath}");
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[RankingV3PrefabBuilder] result=Passed tabs=4 portraits=11 wide=True");
        }

        [MenuItem("Game/UI/V3/Validate SCN-17 Ranking")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException($"Missing Ranking V3 prefab: {PrefabPath}");
            RankingV3View view = prefab.GetComponent<RankingV3View>();
            if (view == null || view.CategoryButtons?.Length != 4 || view.CategoryBodies?.Length != 4 || view.ViewRewardsButton == null)
                throw new MissingReferenceException("Ranking V3 interaction bindings are incomplete.");
            Require(prefab.transform, "RankingComposition/Header/TitlePanel");
            Require(prefab.transform, "RankingComposition/CategoryRail/Category_0");
            Require(prefab.transform, "RankingComposition/GlobalBody/Podium/Rank_1");
            Require(prefab.transform, "RankingComposition/GlobalBody/Leaderboard/Row_6");
            Require(prefab.transform, "RankingComposition/GlobalBody/CurrentCommander");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != ReferenceResolution)
                throw new InvalidOperationException("Ranking V3 must fill 16:9 and 20:9 canvases.");
            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 38) throw new InvalidOperationException($"Ranking requires layered gradient chrome; found {gradients.Length}.");
            foreach (V3GradientGraphic gradient in gradients)
            {
                SerializedObject serialized = new(gradient);
                if (serialized.FindProperty("borderColor").colorValue.a > .01f &&
                    Mathf.Abs(serialized.FindProperty("borderWidth").floatValue - 3f) > .001f)
                    throw new InvalidOperationException($"Ranking border is not 3 px: {gradient.name}");
            }
            foreach (AspectRatioFitter fitter in prefab.GetComponentsInChildren<AspectRatioFitter>(true))
                if (fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                    throw new InvalidOperationException($"Ranking art must crop without stretching: {fitter.name}");
            Debug.Log($"[RankingV3Validation] result=Passed gradients={gradients.Length} tabs=4 wide=True");
        }

        private static void LoadAssets()
        {
            string[] paths = { RankingIconPath, RegionIconPath, FriendsIconPath, SeasonIconPath, DaliaPath };
            int[] sizes = { 512, 512, 512, 512, 2048 };
            for (int i = 0; i < paths.Length; i++) ConfigureSprite(paths[i], sizes[i]);
            foreach (string path in PortraitPaths) ConfigureSprite(path, 1024);
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            rankingIcon = RequireSprite(RankingIconPath); regionIcon = RequireSprite(RegionIconPath);
            friendsIcon = RequireSprite(FriendsIconPath); seasonIcon = RequireSprite(SeasonIconPath); dalia = RequireSprite(DaliaPath);
            districtIcon = RequireSprite(V3UiFoundationBuilder.CommanderBadgeIconPath);
            operationsIcon = RequireSprite(OperationsIconPath);
            portraits = new Sprite[PortraitPaths.Length];
            for (int i = 0; i < PortraitPaths.Length; i++) portraits[i] = RequireSprite(PortraitPaths[i]);
            if (bold == null || medium == null) throw new MissingReferenceException("Ranking fonts are missing.");
        }

        private static void BuildHeader(RectTransform root, ICollection<RectTransform> right, ICollection<RectTransform> widths,
            out TMP_Text creditsValue, out TMP_Text commandValue)
        {
            RectTransform header = TopLeft("Header", root, 9, 7, 1655, 85);
            RectTransform logoPanel = TopLeft("LogoPanel", header, 0, 0, 262, 85);
            V3GradientGraphic logoChrome = Gradient(logoPanel, DarkTop, DarkBottom, Border);
            Button logoButton = logoPanel.gameObject.AddComponent<Button>();
            logoButton.targetGraphic = logoChrome;
            logoButton.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            V3UiFoundationBuilder.AddMainMenuLogo(logoPanel, left: 9f, top: 8f, right: 9f, bottom: 9f);
            RectTransform titlePanel = TopLeft("TitlePanel", header, 270, 0, 710, 85); Gradient(titlePanel, DarkTop, DarkBottom, Border); widths.Add(titlePanel);
            TMP_Text title = Text("Title", titlePanel, "COMMANDER RANKING", 46, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); Horizontal(title.rectTransform, 27, 18, 0, 85);
            RectTransform credits = Resource(header, "Credits", 988, 0, 254, catalog.CreditsIcon, "CREDITS", "24,750", Amber, out creditsValue);
            RectTransform command = Resource(header, "Command", 1250, 0, 270, catalog.CommandIcon, "COMMAND", "8,430", Cyan, out commandValue);
            Button settings = Button("SettingsButton", header, 1528, 0, 127, 85, DarkTop, DarkBottom, Border);
            Image settingsIcon = Image("Icon", settings.transform, catalog.SettingsIcon, TextPrimary); Center(settingsIcon.rectTransform, 57, 57);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            right.Add(credits); right.Add(command); right.Add(settings.GetComponent<RectTransform>());
        }

        private static RectTransform Resource(Transform parent, string name, float x, float y, float width, Sprite icon,
            string label, string value, Color accent, out TMP_Text valueText)
        {
            RectTransform panel = TopLeft(name, parent, x, y, width, 85); Gradient(panel, DarkTop, DarkBottom, Border);
            Image iconImage = Image("Icon", panel, icon, accent); TopLeft(iconImage.rectTransform, 15, 12, 60, 60);
            TMP_Text labelText = Text("Label", panel, label, 18, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(labelText.rectTransform, 92, 4, width - 102, 32);
            valueText = Text("Value", panel, value, 32, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(valueText.rectTransform, 92, 33, width - 102, 43);
            return panel;
        }

        private static void BuildTabs(RectTransform root, out Button[] buttons, out V3GradientGraphic[] chrome)
        {
            RectTransform rail = TopLeft("CategoryRail", root, 10, 96, 260, 455);
            string[] labels = { "GLOBAL", "REGION", "FRIENDS", "SEASON" };
            Sprite[] icons = { rankingIcon, regionIcon, friendsIcon, seasonIcon };
            buttons = new Button[4]; chrome = new V3GradientGraphic[4];
            for (int i = 0; i < 4; i++)
            {
                Button tab = Button($"Category_{i}", rail, 0, i * 116, 260, 108,
                    i == 0 ? BlueTop : DarkTop, i == 0 ? BlueBottom : DarkBottom, i == 0 ? Cyan : Border);
                buttons[i] = tab; chrome[i] = tab.targetGraphic as V3GradientGraphic;
                RectTransform iconRoot = TopLeft("Icon", tab.transform, 21, 22, 63, 63);
                if (i == 0)
                    CreateGlobeIcon(iconRoot, i == 0 ? TextPrimary : TextMuted);
                else if (i == 3)
                    CreateCalendarIcon(iconRoot, TextMuted);
                else
                {
                    Image icon = Image("Sprite", iconRoot, icons[i], TextMuted);
                    Stretch(icon.rectTransform);
                    icon.preserveAspect = true;
                }
                TMP_Text label = Text("Label", tab.transform, labels[i], 29, bold, TextAlignmentOptions.MidlineLeft, i == 0 ? TextPrimary : TextMuted); TopLeft(label.rectTransform, 98, 8, 150, 90);
            }
        }

        private static GameObject BuildGlobalBody(RectTransform root, ICollection<RectTransform> right, ICollection<RectTransform> widths, out Button viewRewards)
        {
            RectTransform body = TopLeft("GlobalBody", root, 0, 0, ReferenceResolution.x, ReferenceResolution.y);
            BuildSeason(body, widths, out viewRewards);
            BuildPodium(body, widths);
            BuildLeaderboard(body, widths);
            BuildCurrentCommander(body, widths);
            RectTransform stats = BuildStats(body); right.Add(stats);
            return body.gameObject;
        }

        private static void BuildSeason(RectTransform body, ICollection<RectTransform> widths, out Button viewRewards)
        {
            RectTransform panel = TopLeft("SeasonHeader", body, 278, 100, 1029, 108); Gradient(panel, DarkTop, DarkBottom, Border); widths.Add(panel);
            TMP_Text title = Text("Title", panel, "SEASON 15: STEEL RESOLVE", 28, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(title.rectTransform, 24, 8, 470, 38);
            TMP_Text progress = Text("ProgressLabel", panel, "SEASON PROGRESS", 19, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(progress.rectTransform, 24, 43, 250, 28);
            RectTransform track = TopLeft("ProgressTrack", panel, 24, 77, 525, 16); Gradient(track, new Color32(27, 53, 15, 255), new Color32(10, 27, 10, 255), Border);
            RectTransform fill = TopLeft("ProgressFill", track, 0, 0, 447, 16); Gradient(fill, new Color32(126, 190, 72, 255), new Color32(73, 138, 36, 255), Color.clear);
            TMP_Text time = Text("Time", panel, "28D 12H REMAINING", 20, bold, TextAlignmentOptions.MidlineRight, Cyan); TopRight(time.rectTransform, 314, 46, 240, 30);
            TMP_Text xp = Text("XP", panel, "18,560 / 24,000 XP", 18, bold, TextAlignmentOptions.MidlineRight, TextMuted); TopRight(xp.rectTransform, 314, 75, 240, 27);
            viewRewards = Button("ViewRewardsButton", panel, 835, 35, 179, 47, BlueTop, BlueBottom, Cyan);
            TopRight(viewRewards.GetComponent<RectTransform>(), 15, 35, 179, 47);
            TMP_Text label = Text("Label", viewRewards.transform, "VIEW REWARDS", 20, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(label.rectTransform);
        }

        private static void BuildPodium(RectTransform body, ICollection<RectTransform> widths)
        {
            RectTransform podium = TopLeft("Podium", body, 278, 216, 1029, 190); widths.Add(podium);
            string[] names = { "IRONWOLF", "SHADOWFOX", "NIGHTHAWK" };
            string[] roles = { "BATTLEMASTER", "WARLORD", "STRATEGOS" };
            string[] division = { "FIELD I", "COMMAND I", "VANGUARD II" };
            string[] score = { "124,580", "156,780", "98,320" };
            string[] movement = { "▲ 2", "▲ 1", "▼ 1" };
            int[] rank = { 2, 1, 3 }; Color[] accents = { Cyan, Amber, Bronze };
            for (int slot = 0; slot < 3; slot++)
            {
                float w = 336;
                float min = slot / 3f;
                float max = (slot + 1) / 3f;
                float left = slot == 0 ? 0 : 5;
                float right = slot == 2 ? 0 : 5;
                RectTransform card = Anchored($"Rank_{rank[slot]}", podium, min, max, left, right, 0, 190); Gradient(card, DarkTop, DarkBottom, accents[slot]);
                RectTransform clip = TopLeft("PortraitClip", card, 43, 4, 150, 182); clip.gameObject.AddComponent<RectMask2D>();
                RawImage portrait = Raw("Portrait", clip, portraits[slot].texture); Cover(portrait, portraits[slot].texture);
                TMP_Text rankText = Text("Rank", card, rank[slot].ToString(), 48, bold, TextAlignmentOptions.Center, TextPrimary); TopLeft(rankText.rectTransform, 8, 7, 46, 66);
                TMP_Text name = Text("Name", card, names[slot], 28, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(name.rectTransform, 202, 22, w - 208, 43);
                TMP_Text role = Text("Role", card, roles[slot], 19, bold, TextAlignmentOptions.MidlineLeft, accents[slot]); TopLeft(role.rectTransform, 202, 63, w - 208, 31);
                name.enableAutoSizing = true; name.fontSizeMin = 17; name.fontSizeMax = 28;
                role.enableAutoSizing = true; role.fontSizeMin = 12; role.fontSizeMax = 19;
                TMP_Text div = Text("Division", card, division[slot], 18, medium, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(div.rectTransform, 202, 99, w - 208, 29);
                TMP_Text points = Text("Score", card, score[slot], 32, bold, TextAlignmentOptions.MidlineLeft, accents[slot]); TopLeft(points.rectTransform, 202, 132, w - 208, 48);
                TMP_Text move = Text("Movement", card, movement[slot], 22, bold, TextAlignmentOptions.MidlineLeft, movement[slot][0] == '▲' ? Green : Red); TopLeft(move.rectTransform, 12, 139, 55, 40);
            }
        }

        private static void BuildLeaderboard(RectTransform body, ICollection<RectTransform> widths)
        {
            RectTransform panel = TopLeft("Leaderboard", body, 278, 414, 1029, 343); Gradient(panel, DarkTop, DarkBottom, Border); widths.Add(panel);
            string[] headers = { "RANK", "COMMANDER", "DIVISION", "SCORE", "MOVEMENT" };
            float[] headerMin = { 0f, .12f, .44f, .68f, .86f };
            float[] headerMax = { .12f, .44f, .68f, .86f, 1f };
            for (int i = 0; i < headers.Length; i++)
            {
                TMP_Text h = Text($"Header_{i}", panel, headers[i], 17, bold, TextAlignmentOptions.MidlineLeft, TextMuted);
                HorizontalRange(h.rectTransform, headerMin[i], headerMax[i], 10, 8, 0, 38);
            }
            string[] names = { "VALKYRIE", "CRIMSON", "GHOSTRIDER", "RAZORBACK", "WOLFPACK", "TITAN-07", "PUNISHER" };
            string[] divisions = { "COMMAND I", "VANGUARD I", "VANGUARD I", "FIELD II", "FIELD II", "FIELD I", "FIELD I" };
            string[] scores = { "86,410", "78,950", "72,430", "68,210", "63,890", "59,120", "55,780" };
            string[] moves = { "▲ 3", "▲ 2", "▼ 1", "▲ 4", "▼ 2", "▲ 1", "▼ 1" };
            for (int i = 0; i < 7; i++)
            {
                RectTransform row = Horizontal($"Row_{i}", panel, 3, 3, 39 + i * 43, 42); Gradient(row, i % 2 == 0 ? DarkTop : RaisedTop, DarkBottom, Border);
                TMP_Text rank = Text("Rank", row, (i + 4).ToString(), 23, bold, TextAlignmentOptions.Center, TextMuted); HorizontalRange(rank.rectTransform, 0f, .07f, 5, 5, 0, 42);
                RectTransform clip = TopFraction("PortraitClip", row, .08f, 2, 48, 38); clip.gameObject.AddComponent<RectMask2D>(); RawImage portrait = Raw("Portrait", clip, portraits[i + 3].texture); Cover(portrait, portraits[i + 3].texture);
                TMP_Text name = Text("Commander", row, names[i], 23, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); HorizontalRange(name.rectTransform, .15f, .44f, 0, 8, 0, 42);
                TMP_Text division = Text("Division", row, divisions[i], 20, medium, TextAlignmentOptions.MidlineLeft, TextMuted); HorizontalRange(division.rectTransform, .44f, .68f, 0, 8, 0, 42);
                TMP_Text score = Text("Score", row, scores[i], 23, bold, TextAlignmentOptions.MidlineLeft, i < 3 ? Cyan : i < 5 ? Amber : TextMuted); HorizontalRange(score.rectTransform, .68f, .86f, 0, 8, 0, 42);
                TMP_Text movement = Text("Movement", row, moves[i], 21, bold, TextAlignmentOptions.MidlineLeft, moves[i][0] == '▲' ? Green : Red); HorizontalRange(movement.rectTransform, .86f, 1f, 0, 8, 0, 42);
            }
        }

        private static void BuildCurrentCommander(RectTransform body, ICollection<RectTransform> widths)
        {
            RectTransform panel = TopLeft("CurrentCommander", body, 278, 766, 1029, 141); Gradient(panel, new Color32(35, 91, 25, 255), new Color32(8, 48, 13, 255), Green); widths.Add(panel);
            TMP_Text rank = Text("Rank", panel, "27", 45, bold, TextAlignmentOptions.Center, TextPrimary); TopLeft(rank.rectTransform, 12, 29, 66, 75);
            RectTransform clip = TopLeft("PortraitClip", panel, 80, 4, 145, 133); clip.gameObject.AddComponent<RectMask2D>(); RawImage portrait = Raw("Dalia", clip, dalia.texture); Cover(portrait, dalia.texture);
            TMP_Text name = Text("Name", panel, "DALIA RAHIM", 36, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(name.rectTransform, 211, 25, 265, 50);
            TMP_Text role = Text("Role", panel, "FIELD COMMANDER", 25, bold, TextAlignmentOptions.MidlineLeft, Green); TopLeft(role.rectTransform, 211, 73, 265, 39);
            CreateSolid("Rule0", panel, 461, 28, 3, 83, new Color(1, 1, 1, .14f));
            TMP_Text division = Text("Division", panel, "FIELD II", 28, bold, TextAlignmentOptions.Center, Green); TopLeft(division.rectTransform, 477, 30, 158, 75);
            CreateSolid("Rule1", panel, 642, 28, 3, 83, new Color(1, 1, 1, .14f));
            TMP_Text score = Text("Score", panel, "36,480", 35, bold, TextAlignmentOptions.Center, Green); TopLeft(score.rectTransform, 658, 30, 160, 75);
            Image rule2 = CreateSolid("Rule2", panel, 826, 28, 3, 83, new Color(1, 1, 1, .14f)); TopRight(rule2.rectTransform, 218, 28, 3, 83);
            TMP_Text progress = Text("Progress", panel, "PROMOTION PROGRESS", 15, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopRight(progress.rectTransform, 18, 22, 190, 28);
            progress.enableAutoSizing = true; progress.fontSizeMin = 11; progress.fontSizeMax = 15; progress.overflowMode = TextOverflowModes.Overflow;
            RectTransform track = TopFraction("ProgressTrack", panel, 1f, 58, 190, 16); TopRight(track, 18, 58, 190, 16); Gradient(track, new Color32(24, 66, 17, 255), new Color32(13, 43, 11, 255), Border);
            RectTransform fill = TopLeft("ProgressFill", track, 0, 0, 105, 16); Gradient(fill, new Color32(134, 204, 74, 255), new Color32(73, 148, 34, 255), Color.clear);
            TMP_Text value = Text("ProgressValue", panel, "6,520 / 10,000", 20, bold, TextAlignmentOptions.MidlineLeft, Green); TopRight(value.rectTransform, 18, 78, 190, 38);
        }

        private static RectTransform BuildStats(RectTransform body)
        {
            RectTransform panel = TopLeft("Stats", body, 1316, 100, 348, 807); Gradient(panel, DarkTop, DarkBottom, Border);
            BuildStat(panel, 0, "DISTRICT SCORE", "12,450", "PTS", "TOP 12%", districtIcon, Cyan);
            BuildStat(panel, 201, "OPERATIONS WON", "128", "", "TOP 7%", operationsIcon, Green);
            BuildStat(panel, 402, "CIVILIANS PROTECTED", "8,642", "", "TOP 15%", friendsIcon, Amber);
            BuildStat(panel, 603, "SEASON ENDS IN", "12D 08H", "", "", seasonIcon, Cyan);
            return panel;
        }

        private static void BuildStat(RectTransform panel, float y, string heading, string value, string suffix, string rank, Sprite icon, Color accent)
        {
            RectTransform section = TopLeft("Stat_" + heading.Replace(" ", ""), panel, 4, y + 4, 340, 195); Gradient(section, DarkTop, DarkBottom, Border);
            TMP_Text headingText = Text("Heading", section, heading, 24, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(headingText.rectTransform, 30, 18, 290, 39);
            Image image = Image("Icon", section, icon, accent); TopLeft(image.rectTransform, 28, 83, 78, 78); image.preserveAspect = true;
            TMP_Text valueText = Text("Value", section, value, 45, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(valueText.rectTransform, 126, 75, 185, 67);
            if (!string.IsNullOrEmpty(suffix)) { TMP_Text suffixText = Text("Suffix", section, suffix, 18, bold, TextAlignmentOptions.MidlineLeft, TextMuted); TopLeft(suffixText.rectTransform, 289, 96, 47, 38); }
            if (!string.IsNullOrEmpty(rank)) { TMP_Text rankText = Text("Rank", section, rank, 22, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(rankText.rectTransform, 126, 139, 190, 38); }
        }

        private static GameObject BuildAlternateBody(RectTransform root, string title, string subtitle, string description, Color accent, ICollection<RectTransform> widths)
        {
            RectTransform body = TopLeft(title.Replace(" ", "") + "Body", root, 278, 100, 1029, 807); Gradient(body, DarkTop, DarkBottom, Border); widths.Add(body);
            TMP_Text heading = Text("Heading", body, title, 43, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(heading.rectTransform, 34, 22, 700, 63);
            TMP_Text sub = Text("Subtitle", body, subtitle, 26, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(sub.rectTransform, 34, 89, 700, 43);
            CreateSolid("Rule", body, 34, 143, 961, 3, Border);
            Image icon = Image("Icon", body, title.StartsWith("REGION") ? regionIcon : title.StartsWith("FRIENDS") ? friendsIcon : seasonIcon, accent); TopLeft(icon.rectTransform, 80, 216, 218, 218); icon.preserveAspect = true;
            TMP_Text copy = Text("Description", body, description, 29, medium, TextAlignmentOptions.TopLeft, TextPrimary); TopLeft(copy.rectTransform, 352, 226, 588, 150); copy.textWrappingMode = TextWrappingModes.Normal;
            RectTransform panel = TopLeft("StatusPanel", body, 352, 405, 588, 166); Gradient(panel, RaisedTop, RaisedBottom, accent);
            TMP_Text status = Text("Status", panel, "LIVE DATA READY", 31, bold, TextAlignmentOptions.Center, accent); Stretch(status.rectTransform);
            return body.gameObject;
        }

        private static Button Button(string name, Transform parent, float x, float y, float w, float h, Color top, Color bottom, Color border)
        { RectTransform rect = TopLeft(name, parent, x, y, w, h); V3GradientGraphic graphic = Gradient(rect, top, bottom, border); Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; return button; }
        private static V3GradientGraphic Gradient(RectTransform rect, Color top, Color bottom, Color border)
        { V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>(); graphic.ConfigureCorners(Color.Lerp(top, Color.white, .055f), top, Color.Lerp(bottom, Color.black, .12f), bottom, border, border.a > .01f ? 3f : 0f); return graphic; }
        private static void CreateGlobeIcon(RectTransform root, Color color)
        {
            V3RingGraphic ring = root.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(color, 4f, 48);
            CreateSolid("Equator", root, 5, 29, 53, 4, color);
            CreateSolid("Axis", root, 29, 5, 4, 53, color);
            RectTransform meridian = TopLeft("Meridian", root, 14, 5, 35, 53);
            V3RingGraphic inner = meridian.gameObject.AddComponent<V3RingGraphic>();
            inner.Configure(color, 3f, 40);
        }
        private static void CreateCalendarIcon(RectTransform root, Color color)
        {
            Gradient(root, DarkTop, DarkBottom, color);
            CreateSolid("TopRule", root, 5, 15, 53, 5, color);
            CreateSolid("BindingL", root, 14, 0, 5, 17, color);
            CreateSolid("BindingR", root, 44, 0, 5, 17, color);
            TMP_Text day = Text("Day", root, "31", 24, bold, TextAlignmentOptions.Center, color);
            TopLeft(day.rectTransform, 7, 20, 49, 38);
        }
        private static Image Image(string name, Transform parent, Sprite sprite, Color color)
        { return V3UiPrefabFactory.CreateImage(name, parent, sprite, color, false, false); }
        private static Image CreateSolid(string name, Transform parent, float x, float y, float w, float h, Color color)
        { Image image = Image(name, parent, null, color); TopLeft(image.rectTransform, x, y, w, h); return image; }
        private static RawImage Raw(string name, Transform parent, Texture texture)
        { RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); RawImage image = rect.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; return image; }
        private static void Cover(RawImage image, Texture texture)
        { AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio = texture.width / (float)texture.height; }
        private static TMP_Text Text(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions align, Color color)
        { RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200, 60), Vector2.zero); TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.alignment = align; text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text; }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 pos) => V3UiPrefabFactory.CreateRect(name, parent, min, max, size, pos);
        private static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h) { RectTransform rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(w, h), new Vector2(x, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static RectTransform TopFraction(string name, Transform parent, float x, float y, float w, float h) { RectTransform rect = Rect(name, parent, new Vector2(x, 1), new Vector2(x, 1), new Vector2(w, h), new Vector2(0, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static RectTransform Horizontal(string name, Transform parent, float left, float right, float y, float h) { RectTransform rect = Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-(left + right), h), new Vector2(left, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static RectTransform Anchored(string name, Transform parent, float minX, float maxX, float left, float right, float y, float h) { RectTransform rect = Rect(name, parent, new Vector2(minX, 1), new Vector2(maxX, 1), new Vector2(-(left + right), h), new Vector2(left, -y)); rect.pivot = new Vector2(0, 1); return rect; }
        private static void TopLeft(RectTransform rect, float x, float y, float w, float h) { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, -y); }
        private static void TopRight(RectTransform rect, float x, float y, float w, float h) { rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(-x, -y); }
        private static void Horizontal(RectTransform rect, float left, float right, float y, float h) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(-(left + right), h); rect.anchoredPosition = new Vector2(left, -y); }
        private static void HorizontalRange(RectTransform rect, float minX, float maxX, float left, float right, float y, float h) { rect.anchorMin = new Vector2(minX, 1); rect.anchorMax = new Vector2(maxX, 1); rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(-(left + right), h); rect.anchoredPosition = new Vector2(left, -y); }
        private static void Center(RectTransform rect, float w, float h) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = Vector2.zero; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }

        private static Transform Require(Transform root, string path)
        { Transform result = root.Find(path); if (result == null) throw new MissingReferenceException($"Ranking V3 is missing {path}."); return result; }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects()) { content = sceneRoot.GetComponentInChildren<UIShellContentView>(true); if (content != null) break; }
            if (content == null) throw new InvalidOperationException("Menu scene is missing UIShellContentView.");
            SerializedObject serialized = new(content); SerializedProperty property = serialized.FindProperty("rankingContentPrefab");
            if (property == null) throw new MissingFieldException(nameof(UIShellContentView), "rankingContentPrefab");
            property.objectReferenceValue = prefab; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException($"Missing Ranking V3 art: {path}");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = maxSize; importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        { Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (sprite == null) throw new FileNotFoundException($"Missing Ranking V3 sprite: {path}"); return sprite; }
    }
}
#endif
