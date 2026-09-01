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
    public static class EventsV3PrefabBuilder
    {
        public const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN16_EventsContent.prefab";
        private const string ScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string OldMarketPath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_CampaignScene_V3.png";
        private const string ConvoyPath = "Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_SkirmishScene_V3.png";
        private const string AriaPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
        private const string OperationsPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 Reference = new(1672f, 941f);
        private static readonly Color Border = new Color32(57, 70, 74, 255);
        private static readonly Color DarkTop = new Color32(27, 38, 42, 255);
        private static readonly Color DarkBottom = new Color32(4, 10, 13, 255);
        private static readonly Color RaisedTop = new Color32(44, 54, 57, 255);
        private static readonly Color RaisedBottom = new Color32(12, 18, 20, 255);
        private static readonly Color TextPrimary = new Color32(244, 245, 242, 255);
        private static readonly Color TextMuted = new Color32(185, 193, 193, 255);
        private static readonly Color Amber = new Color32(250, 177, 0, 255);
        private static readonly Color Red = new Color32(239, 62, 20, 255);
        private static readonly Color Cyan = new Color32(0, 176, 223, 255);
        private static readonly Color Green = new Color32(95, 190, 73, 255);

        private static TMP_FontAsset bold;
        private static TMP_FontAsset medium;
        private static V3UiArtCatalog catalog;
        private static Sprite logo;
        private static Sprite oldMarket;
        private static Sprite convoy;
        private static Sprite aria;
        private static Sprite operations;

        [MenuItem("Game/UI/V3/Rebuild SCN-16 Events")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            Load();
            RectTransform rootRect = Rect("SCN16_EventsContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject root = rootRect.gameObject;
            EventsV3View view = root.AddComponent<EventsV3View>();
            Image black = Image("CanvasBlack", root.transform, null, Color.black);
            Stretch(black.rectTransform);
            RectTransform composition = TopLeft("EventsComposition", root.transform, 0, 0, Reference.x, Reference.y);
            var right = new List<RectTransform>();
            var widths = new List<RectTransform>();
            BuildHeader(composition, right, widths, out TMP_Text credits, out TMP_Text command);
            BuildRail(composition, out Button[] tabs, out V3GradientGraphic[] tabChrome);
            BuildCards(composition, widths, out Button[] eventButtons, out V3GradientGraphic[] eventChrome,
                out TMP_Text[] titles, out TMP_Text[] timers, out TMP_Text[] descriptions,
                out TMP_Text[] progressTexts, out RectTransform[] progressFills);
            BuildDetail(composition, right, out TMP_Text detailTitle, out TMP_Text detailTimer,
                out TMP_Text detailDescription, out TMP_Text[] objectives, out TMP_Text[] states,
                out TMP_Text[] modifiers, out TMP_Text[] rewards);
            MainMenuV3SectionLayoutView layout = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            layout.Configure(Reference, MainMenuV3SectionAlignment.Center, right.ToArray(), true, null, widths.ToArray());
            view.Configure(credits, command, tabs, tabChrome, eventButtons, eventChrome, titles, timers,
                descriptions, progressTexts, progressFills, detailTitle, detailTimer, detailDescription,
                objectives, states, modifiers, rewards);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null) throw new InvalidOperationException(PrefabPath);
            Assign(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[EventsV3PrefabBuilder] result=Passed tabs=4 cards=3 gradients=directional borders=3");
        }

        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new FileNotFoundException(PrefabPath);
            EventsV3View view = prefab.GetComponent<EventsV3View>();
            if (view == null || view.TabButtons?.Length != 4 || view.EventButtons?.Length != 3)
                throw new MissingReferenceException("Events bindings are incomplete.");
            Require(prefab.transform, "EventsComposition/EventCards/EventCard_0");
            Require(prefab.transform, "EventsComposition/DetailPanel/EnterOperationButton");
            RawImage[] art = prefab.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < art.Length; i++)
                if (art[i].GetComponent<AspectRatioFitter>()?.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent)
                    throw new InvalidOperationException($"Event art stretches: {art[i].name}");
            MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout == null || !layout.ExpandToCanvasWidth || layout.ReferenceResolution != Reference)
                throw new InvalidOperationException("Events responsive layout is missing.");
            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 45) throw new InvalidOperationException($"Events gradients={gradients.Length}");
            for (int i = 0; i < gradients.Length; i++)
            {
                SerializedObject data = new(gradients[i]);
                if (data.FindProperty("borderColor").colorValue.a > .01f && Mathf.Abs(data.FindProperty("borderWidth").floatValue - 3f) > .001f)
                    throw new InvalidOperationException($"Events border mismatch: {gradients[i].name}");
            }
            Debug.Log($"[EventsV3Validation] result=Passed gradients={gradients.Length}");
        }

        private static void Load()
        {
            string[] paths = { OldMarketPath, ConvoyPath, AriaPath, OperationsPath };
            int[] sizes = { 2048, 2048, 2048, 512 };
            for (int i = 0; i < paths.Length; i++) ConfigureSprite(paths[i], sizes[i]);
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            catalog = V3UiFoundationBuilder.RequireCatalog();
            logo = Sprite(V3UiFoundationBuilder.MainMenuLogoPath); oldMarket = Sprite(OldMarketPath); convoy = Sprite(ConvoyPath); aria = Sprite(AriaPath); operations = Sprite(OperationsPath);
            if (bold == null || medium == null) throw new MissingReferenceException("Events fonts missing.");
        }

        private static void BuildHeader(RectTransform root, ICollection<RectTransform> right, ICollection<RectTransform> widths,
            out TMP_Text credits, out TMP_Text command)
        {
            RectTransform header = TopLeft("Header", root, 15, 16, 1642, 96);
            RectTransform logoPanel = TopLeft("LogoPanel", header, 0, 0, 355, 96);
            V3GradientGraphic logoChrome = Gradient(logoPanel, DarkTop, DarkBottom, Border);
            Button logoButton = logoPanel.gameObject.AddComponent<Button>();
            logoButton.targetGraphic = logoChrome;
            logoButton.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            Image logoImage = Image("Logo", logoPanel, logo, Color.white); TopLeft(logoImage.rectTransform, 10, 6, 335, 83); logoImage.preserveAspect = true;
            RectTransform titlePanel = TopLeft("TitlePanel", header, 370, 0, 647, 96); Gradient(titlePanel, DarkTop, DarkBottom, Border);
            TMP_Text title = Text("Title", titlePanel, "EVENTS", 57, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); Horizontal(title.rectTransform, 26, 18, 0, 94);
            widths.Add(titlePanel);
            RectTransform creditPanel = Resource(header, "Credits", 1018, 0, 240, catalog.CreditsIcon, "CREDITS", "24,750", Amber, out credits);
            RectTransform commandPanel = Resource(header, "Command", 1265, 0, 265, catalog.CommandIcon, "COMMAND", "8,430", Cyan, out command);
            Button settings = Button("SettingsButton", header, 1537, 0, 105, 96, DarkTop, DarkBottom, Border);
            Image settingsIcon = Image("Icon", settings.transform, catalog.SettingsIcon, TextPrimary); Center(settingsIcon.rectTransform, 60, 60);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            right.Add(creditPanel); right.Add(commandPanel); right.Add(settings.GetComponent<RectTransform>());
        }

        private static RectTransform Resource(Transform parent, string name, float x, float y, float width, Sprite icon,
            string label, string value, Color accent, out TMP_Text valueText)
        {
            RectTransform panel = TopLeft(name, parent, x, y, width, 96); Gradient(panel, DarkTop, DarkBottom, Border);
            Image iconImage = Image("Icon", panel, icon, accent); TopLeft(iconImage.rectTransform, 14, 14, 62, 62);
            TMP_Text labelText = Text("Label", panel, label, 18, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(labelText.rectTransform, 86, 7, width - 94, 31);
            valueText = Text("Value", panel, value, 32, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(valueText.rectTransform, 86, 37, width - 94, 47);
            return panel;
        }

        private static void BuildRail(RectTransform root, out Button[] tabs, out V3GradientGraphic[] chrome)
        {
            RectTransform rail = TopLeft("CategoryRail", root, 15, 123, 208, 779);
            string[] labels = { "ACTIVE", "UPCOMING", "CHALLENGES", "REWARDS" };
            tabs = new Button[4]; chrome = new V3GradientGraphic[4];
            for (int i = 0; i < 4; i++)
            {
                Button tab = Button($"Tab_{i}", rail, 0, i * 100, 208, 89, i == 0 ? new Color32(55, 143, 55, 255) : DarkTop,
                    i == 0 ? new Color32(9, 62, 25, 255) : DarkBottom, i == 0 ? Green : Border);
                tabs[i] = tab; chrome[i] = tab.targetGraphic as V3GradientGraphic;
                RectTransform iconRoot = TopLeft("Icon", tab.transform, 18, 20, 47, 47);
                BuildTabIcon(iconRoot, i, i == 0 ? Green : TextPrimary);
                TMP_Text label = Text("Label", tab.transform, labels[i], i == 2 ? 20 : 23, bold, TextAlignmentOptions.MidlineLeft, TextPrimary);
                TopLeft(label.rectTransform, 78, 6, 120, 76);
            }
        }

        private static void BuildCards(RectTransform root, ICollection<RectTransform> widths, out Button[] eventButtons,
            out V3GradientGraphic[] cardChrome, out TMP_Text[] titles, out TMP_Text[] timers, out TMP_Text[] descriptions,
            out TMP_Text[] progressTexts, out RectTransform[] progressFills)
        {
            RectTransform area = TopLeft("EventCards", root, 235, 126, 966, 776); widths.Add(area);
            Sprite[] art = { oldMarket, convoy, aria };
            string[] names = { "HOLD THE\nOLD MARKET", "CONVOY\nBREAKER", "ARIA\nFIELD TRIALS" };
            string[] timerValues = { "02D 14H", "05D 08H", "07D" };
            string[] descriptionsValue = { "Hold the market district\nand repel enemy waves.", "Destroy supply convoys\nand deny their advance.", "Complete ARIA's field trials\nand gather operation data." };
            string[] progress = { "7/10", "4/6", "2/5" };
            Color[] accents = { Amber, Red, Cyan };
            eventButtons = new Button[3]; cardChrome = new V3GradientGraphic[3]; titles = new TMP_Text[3]; timers = new TMP_Text[3];
            descriptions = new TMP_Text[3]; progressTexts = new TMP_Text[3]; progressFills = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform card = Anchored($"EventCard_{i}", area, i / 3f, (i + 1) / 3f, 0, i == 2 ? 0 : 8, 0, 776);
                cardChrome[i] = Gradient(card, DarkTop, DarkBottom, accents[i]);
                RectTransform artClip = Horizontal("ArtClip", card, 4, 4, 4, 292); artClip.gameObject.AddComponent<RectMask2D>();
                RawImage artImage = Raw("Art", artClip, art[i].texture); Cover(artImage, art[i].texture); Overlay("Shade", artClip);
                RectTransform live = TopLeft("Live", card, 8, 8, 67, 41); Gradient(live, Color.Lerp(accents[i], Color.white, .12f), Color.Lerp(accents[i], Color.black, .4f), accents[i]);
                TMP_Text liveText = Text("Label", live, "LIVE", 19, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(liveText.rectTransform);
                timers[i] = Text("Timer", card, timerValues[i], 21, bold, TextAlignmentOptions.MidlineRight, accents[i]); TopRight(timers[i].rectTransform, 11, 9, 94, 38);
                titles[i] = Text("Title", card, names[i], 35, bold, TextAlignmentOptions.TopLeft, TextPrimary); Horizontal(titles[i].rectTransform, 17, 12, 54, 117); titles[i].textWrappingMode = TextWrappingModes.Normal;
                TMP_Text objectiveHeader = Text("ObjectivesHeader", card, "OBJECTIVES", 19, bold, TextAlignmentOptions.MidlineLeft, accents[i]); TopLeft(objectiveHeader.rectTransform, 15, 302, 170, 34);
                descriptions[i] = Text("Description", card, descriptionsValue[i], 16, medium, TextAlignmentOptions.TopLeft, TextPrimary); Horizontal(descriptions[i].rectTransform, 15, 78, 337, 59); descriptions[i].textWrappingMode = TextWrappingModes.Normal;
                progressTexts[i] = Text("Progress", card, progress[i], 28, bold, TextAlignmentOptions.MidlineRight, accents[i]); TopRight(progressTexts[i].rectTransform, 14, 340, 67, 48);
                RectTransform track = Horizontal("ProgressTrack", card, 15, 15, 402, 10); Gradient(track, RaisedTop, RaisedBottom, Color.clear);
                RectTransform fill = TopLeft("ProgressFill", track, 0, 0, i == 0 ? 193 : i == 1 ? 184 : 110, 10); Image fillImage = Image("Fill", fill, null, accents[i]); Stretch(fillImage.rectTransform); progressFills[i] = fill;
                BuildCardSection(card, 427, "MODIFIERS", accents[i], new[] { i == 0 ? "NO AIR SUPPORT" : i == 1 ? "ARMORED UNITS" : "ARIA ASSIST", i == 0 ? "FOG OF WAR" : i == 1 ? "LIMITED REPAIRS" : "ENHANCED ENEMIES" });
                BuildRewards(card, 542, accents[i], i == 0 ? "2,500" : i == 1 ? "4,000" : "3,000", i == 0 ? "250" : i == 1 ? "400" : "300", i == 0 ? "10" : i == 1 ? "20" : "15");
                Button select = AnchoredButton("ViewEventButton", card, 13, 13, 690, 69, Color.Lerp(accents[i], Color.white, .08f), Color.Lerp(accents[i], Color.black, .24f), accents[i]);
                TMP_Text selectLabel = Text("Label", select.transform, "VIEW EVENT", 28, bold, TextAlignmentOptions.Center, DarkBottom); Stretch(selectLabel.rectTransform);
                eventButtons[i] = select;
            }
        }

        private static void BuildCardSection(RectTransform card, float y, string header, Color accent, string[] values)
        {
            TMP_Text heading = Text(header, card, header, 18, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(heading.rectTransform, 15, y, 180, 31);
            CreateSolid("Rule", card, 15, y + 86, 283, 3, Border);
            for (int i = 0; i < 2; i++)
            {
                RectTransform icon = TopLeft($"ModifierIcon_{i}", card, 16 + i * 145, y + 34, 37, 37); Gradient(icon, DarkTop, DarkBottom, i == 0 ? accent : Border);
                CreateModifierIcon(icon, i, i == 0 ? accent : TextMuted);
                TMP_Text label = Text($"Modifier_{i}", card, values[i], 13, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(label.rectTransform, 59 + i * 145, y + 32, 99, 42);
            }
        }

        private static void BuildRewards(RectTransform card, float y, Color accent, string a, string b, string c)
        {
            TMP_Text heading = Text("RewardsHeader", card, "REWARDS", 18, bold, TextAlignmentOptions.MidlineLeft, accent); TopLeft(heading.rectTransform, 15, y, 180, 31);
            string[] values = { a, b, c }; string[] labels = { "CREDITS", "COMMANDER XP", "PARTS" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform cell = TopLeft($"Reward_{i}", card, 13 + i * 99, y + 35, 93, 103); Gradient(cell, DarkTop, DarkBottom, Border);
                Image icon = Image("Icon", cell, i == 0 ? catalog.CreditsIcon : i == 1 ? catalog.CommandIcon : operations, i == 0 ? Amber : i == 1 ? Cyan : Green); TopLeft(icon.rectTransform, 7, 13, 30, 30);
                TMP_Text value = Text("Value", cell, values[i], 17, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(value.rectTransform, 40, 10, 49, 35);
                TMP_Text label = Text("Label", cell, labels[i], i == 1 ? 10 : 12, medium, TextAlignmentOptions.Center, TextPrimary); TopLeft(label.rectTransform, 4, 54, 85, 40); label.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static void BuildDetail(RectTransform root, ICollection<RectTransform> right, out TMP_Text title, out TMP_Text timer,
            out TMP_Text description, out TMP_Text[] objectives, out TMP_Text[] states, out TMP_Text[] modifiers, out TMP_Text[] rewards)
        {
            RectTransform panel = TopLeft("DetailPanel", root, 1208, 126, 449, 776); Gradient(panel, DarkTop, DarkBottom, Amber); right.Add(panel);
            Image icon = Image("TargetIcon", panel, operations, Amber); TopLeft(icon.rectTransform, 17, 19, 42, 42);
            title = Text("DetailTitle", panel, "HOLD THE OLD MARKET", 31, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); Horizontal(title.rectTransform, 69, 58, 8, 54);
            RectTransform live = TopLeft("Live", panel, 384, 17, 51, 37); Gradient(live, Red, Color.Lerp(Red, Color.black, .36f), Red); TMP_Text liveText = Text("Label", live, "LIVE", 16, bold, TextAlignmentOptions.Center, TextPrimary); Stretch(liveText.rectTransform);
            timer = Text("DetailTimer", panel, "02D 14H REMAINING", 21, bold, TextAlignmentOptions.MidlineLeft, Amber); TopLeft(timer.rectTransform, 23, 65, 370, 35);
            description = Text("DetailDescription", panel, "Enemy forces are pushing into the old market district. Hold key positions, protect civilians, and survive the counterattacks.", 17, medium, TextAlignmentOptions.TopLeft, TextPrimary); Horizontal(description.rectTransform, 23, 22, 112, 82); description.textWrappingMode = TextWrappingModes.Normal;
            CreateSolid("Rule0", panel, 22, 208, 405, 3, Border);
            TMP_Text objectiveHeading = Text("ObjectivesHeading", panel, "OBJECTIVES", 20, bold, TextAlignmentOptions.MidlineLeft, Amber); TopLeft(objectiveHeading.rectTransform, 23, 214, 250, 34);
            objectives = new TMP_Text[3]; states = new TMP_Text[3];
            string[] objectiveValues = { "Establish perimeter", "Secure central plaza", "Hold 10 waves" }; string[] stateValues = { "COMPLETED", "COMPLETED", "7/10" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform row = TopLeft($"Objective_{i}", panel, 22, 251 + i * 53, 405, 50); Gradient(row, DarkTop, DarkBottom, Border);
                CreateCheckOrTarget(TopLeft("Icon", row, 10, 10, 30, 30), i < 2, i < 2 ? Green : Amber);
                objectives[i] = Text("Label", row, objectiveValues[i], 15, medium, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(objectives[i].rectTransform, 51, 3, 225, 43);
                states[i] = Text("State", row, stateValues[i], 14, bold, TextAlignmentOptions.MidlineRight, i < 2 ? Green : Amber); TopRight(states[i].rectTransform, 9, 3, 118, 43);
            }
            CreateSolid("Rule1", panel, 22, 414, 405, 3, Border);
            TMP_Text modifierHeading = Text("ModifiersHeading", panel, "MODIFIERS", 20, bold, TextAlignmentOptions.MidlineLeft, Amber); TopLeft(modifierHeading.rectTransform, 23, 420, 230, 34);
            modifiers = new TMP_Text[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform mod = TopLeft($"Modifier_{i}", panel, 23 + i * 204, 459, 193, 54); Gradient(mod, DarkTop, DarkBottom, Border);
                CreateModifierIcon(TopLeft("Icon", mod, 8, 8, 37, 37), i, i == 0 ? Red : TextMuted);
                modifiers[i] = Text("Label", mod, i == 0 ? "NO AIR SUPPORT" : "FOG OF WAR", 14, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(modifiers[i].rectTransform, 52, 4, 133, 45);
            }
            CreateSolid("Rule2", panel, 22, 528, 405, 3, Border);
            TMP_Text rewardHeading = Text("RewardsHeading", panel, "REWARDS", 20, bold, TextAlignmentOptions.MidlineLeft, Amber); TopLeft(rewardHeading.rectTransform, 23, 535, 220, 34);
            rewards = new TMP_Text[3];
            string[] rewardValues = { "2,500", "250", "10" }; string[] rewardLabels = { "CREDITS", "COMMANDER XP", "RIFLE PARTS" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform reward = TopLeft($"Reward_{i}", panel, 23 + i * 137, 575, 126, 98); Gradient(reward, DarkTop, DarkBottom, Border);
                Image rewardIcon = Image("Icon", reward, i == 0 ? catalog.CreditsIcon : i == 1 ? catalog.CommandIcon : operations, i == 0 ? Amber : i == 1 ? Cyan : Green); TopLeft(rewardIcon.rectTransform, 10, 12, 35, 35);
                rewards[i] = Text("Value", reward, rewardValues[i], 19, bold, TextAlignmentOptions.MidlineLeft, TextPrimary); TopLeft(rewards[i].rectTransform, 50, 10, 67, 38);
                TMP_Text label = Text("Label", reward, rewardLabels[i], 12, medium, TextAlignmentOptions.Center, TextPrimary); TopLeft(label.rectTransform, 5, 56, 116, 34);
            }
            Button enter = Button("EnterOperationButton", panel, 22, 693, 405, 67, Color.Lerp(Amber, Color.white, .1f), Color.Lerp(Amber, Color.black, .2f), Amber);
            TMP_Text enterLabel = Text("Label", enter.transform, "ENTER OPERATION", 29, bold, TextAlignmentOptions.Center, DarkBottom); Stretch(enterLabel.rectTransform);
            enter.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, true);
        }

        private static void BuildTabIcon(RectTransform root, int index, Color color)
        {
            if (index == 0) { Image icon = Image("Target", root, operations, color); Stretch(icon.rectTransform); }
            else if (index == 1) { Gradient(root, DarkTop, DarkBottom, color); CreateSolid("Top", root, 7, 12, 33, 5, color); CreateSolid("Line0", root, 9, 23, 29, 4, color); CreateSolid("Line1", root, 9, 33, 29, 4, color); }
            else if (index == 2) { RectTransform cup = TopLeft("Cup", root, 9, 5, 29, 27); V3RingGraphic ring = cup.gameObject.AddComponent<V3RingGraphic>(); ring.Configure(color, 5, 32); CreateSolid("Stem", root, 21, 29, 6, 12, color); CreateSolid("Base", root, 13, 40, 22, 5, color); }
            else { CreateSolid("Box", root, 7, 21, 34, 24, color); CreateSolid("RibbonV", root, 21, 7, 7, 38, DarkBottom); CreateSolid("Lid", root, 4, 17, 40, 7, color); }
        }

        private static void CreateModifierIcon(RectTransform root, int index, Color color)
        {
            if (index == 0) { Image a = CreateSolid("A", root, 5, 16, 29, 5, color); Image b = CreateSolid("B", root, 5, 16, 29, 5, color); a.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); b.rectTransform.localRotation = Quaternion.Euler(0, 0, -45); }
            else { RectTransform ringRoot = TopLeft("Ring", root, 4, 6, 28, 28); V3RingGraphic ring = ringRoot.gameObject.AddComponent<V3RingGraphic>(); ring.Configure(color, 4, 32); Image slash = CreateSolid("Slash", root, 1, 17, 36, 5, color); slash.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); }
        }

        private static void CreateCheckOrTarget(RectTransform root, bool check, Color color)
        {
            if (check) { Image a = CreateSolid("A", root, 3, 16, 13, 5, color); Image b = CreateSolid("B", root, 11, 12, 20, 5, color); a.rectTransform.localRotation = Quaternion.Euler(0, 0, -43); b.rectTransform.localRotation = Quaternion.Euler(0, 0, 47); }
            else { V3RingGraphic ring = root.gameObject.AddComponent<V3RingGraphic>(); ring.Configure(color, 4, 32); CreateSolid("H", root, 0, 13, 30, 4, color); CreateSolid("V", root, 13, 0, 4, 30, color); }
        }

        private static Button Button(string name, Transform parent, float x, float y, float w, float h, Color top, Color bottom, Color border)
        { RectTransform rect = TopLeft(name, parent, x, y, w, h); V3GradientGraphic g = Gradient(rect, top, bottom, border); Button b = rect.gameObject.AddComponent<Button>(); b.targetGraphic = g; return b; }
        private static Button AnchoredButton(string name, Transform parent, float left, float right, float y, float h, Color top, Color bottom, Color border)
        { RectTransform rect = Horizontal(name, parent, left, right, y, h); V3GradientGraphic g = Gradient(rect, top, bottom, border); Button b = rect.gameObject.AddComponent<Button>(); b.targetGraphic = g; return b; }
        private static V3GradientGraphic Gradient(RectTransform rect, Color top, Color bottom, Color border)
        { V3GradientGraphic g = rect.gameObject.AddComponent<V3GradientGraphic>(); g.ConfigureCorners(Color.Lerp(top, Color.white, .04f), top, Color.Lerp(bottom, Color.black, .12f), bottom, border, border.a > .01f ? 3 : 0); return g; }
        private static void Overlay(string name, Transform parent)
        { RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); V3GradientGraphic g = rect.gameObject.AddComponent<V3GradientGraphic>(); g.Configure(new Color(0, 0, 0, .02f), new Color(0, 0, 0, .65f), Color.clear, 0); g.raycastTarget = false; }
        private static RawImage Raw(string name, Transform parent, Texture texture)
        { RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); RawImage image = rect.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; return image; }
        private static void Cover(RawImage image, Texture texture)
        { AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fitter.aspectRatio = texture.width / (float)texture.height; }
        private static Image Image(string name, Transform parent, Sprite sprite, Color color)
        { return V3UiPrefabFactory.CreateImage(name, parent, sprite, color, false, false); }
        private static Image CreateSolid(string name, Transform parent, float x, float y, float w, float h, Color color)
        { Image image = Image(name, parent, null, color); TopLeft(image.rectTransform, x, y, w, h); return image; }
        private static TMP_Text Text(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions align, Color color)
        { RectTransform rect = Rect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200, 60), Vector2.zero); TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.font = font; text.fontSize = size; text.alignment = align; text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text; }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 pos) => V3UiPrefabFactory.CreateRect(name, parent, min, max, size, pos);
        private static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h) { RectTransform r = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(w, h), new Vector2(x, -y)); r.pivot = new Vector2(0, 1); return r; }
        private static void TopLeft(RectTransform r, float x, float y, float w, float h) { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, -y); }
        private static RectTransform Horizontal(string name, Transform parent, float left, float right, float y, float h) { RectTransform r = Rect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-(left + right), h), new Vector2(left, -y)); r.pivot = new Vector2(0, 1); return r; }
        private static void Horizontal(RectTransform r, float left, float right, float y, float h) { r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = new Vector2(-(left + right), h); r.anchoredPosition = new Vector2(left, -y); }
        private static RectTransform Anchored(string name, Transform parent, float minX, float maxX, float left, float right, float y, float h) { RectTransform r = Rect(name, parent, new Vector2(minX, 1), new Vector2(maxX, 1), new Vector2(-(left + right), h), new Vector2(left, -y)); r.pivot = new Vector2(0, 1); return r; }
        private static void TopRight(RectTransform r, float right, float y, float w, float h) { r.anchorMin = r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 1); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(-right, -y); }
        private static void Center(RectTransform r, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = Vector2.zero; }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f); r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }

        private static Transform Require(Transform root, string path) { Transform found = root.Find(path); if (found == null) throw new MissingReferenceException(path); return found; }
        private static void Assign(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); UIShellContentView content = null;
            foreach (GameObject root in scene.GetRootGameObjects()) { content = root.GetComponentInChildren<UIShellContentView>(true); if (content != null) break; }
            if (content == null) throw new InvalidOperationException("Menu scene missing UIShellContentView.");
            SerializedObject data = new(content); SerializedProperty property = data.FindProperty("eventsContentPrefab");
            if (property == null) throw new MissingFieldException(nameof(UIShellContentView), "eventsContentPrefab");
            property.objectReferenceValue = prefab; data.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        private static void ConfigureSprite(string path, int max)
        { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter; if (importer == null) throw new FileNotFoundException(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = max; importer.SaveAndReimport(); }
        private static Sprite Sprite(string path) { Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (sprite == null) throw new FileNotFoundException(path); return sprite; }
    }
}
#endif
