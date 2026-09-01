using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class CommanderProfileV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab";
        private const string CommanderScenePath = "Assets/Game/Art/UI/V3Shared/CommanderScenes/SCN02_FieldCommander_01_Scene_V3.png";
        private const string CampaignIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_CampaignTarget_V3.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_OperationsCompass_V3.png";
        private const string SkirmishIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_SkirmishBlades_V3.png";
        private const string ArmoryIconPath = "Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/SCN02_Icon_ArmoryCrate_V3.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(70, 82, 86, 255);
        private static readonly Color DarkTop = new Color32(20, 31, 35, 250);
        private static readonly Color DarkBottom = new Color32(6, 13, 16, 252);
        private static readonly Color BlueTop = new Color32(16, 121, 196, 255);
        private static readonly Color BlueBottom = new Color32(4, 62, 105, 255);
        private static readonly Color GreenTop = new Color32(76, 154, 24, 255);
        private static readonly Color GreenBottom = new Color32(22, 82, 22, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Sprite commanderScene;
        private static Sprite campaignIcon;
        private static Sprite operationsIcon;
        private static Sprite skirmishIcon;
        private static Sprite armoryIcon;
        private static Sprite commanderRankIcon;
        private static Sprite commanderCrateIcon;
        private static Sprite commanderBackIcon;
        private static Sprite commanderEditIcon;
        private static Sprite commanderBadgeIcon;
        private static Sprite commanderRosterIcon;
        private static Sprite commanderVehicleIcon;
        private static Sprite commanderRewardIcon;
        private static Sprite commanderClaimIcon;
        private static Sprite commanderHistoryIcon;
        private static Sprite commanderUpgradesIcon;
        private static Sprite commanderLockIcon;
        private static Sprite commanderHeaderStarIcon;
        private static Sprite commanderCheckIcon;

        [MenuItem("Game/UI/V3/Rebuild Commander Profile Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            GameObject root = CreateRect("SCN03_CommanderProfileContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            RectTransform header = CreateSection("HeaderContent", root.transform, MainMenuV3SectionAlignment.TopLeft);
            RectTransform left = CreateSection("LeftContent", root.transform, MainMenuV3SectionAlignment.TopLeft);
            RectTransform middle = CreateSection("MiddleContent", root.transform, MainMenuV3SectionAlignment.Center);
            RectTransform right = CreateSection("RightContent", root.transform, MainMenuV3SectionAlignment.TopRight);
            RectTransform footer = CreateSection("FooterContent", root.transform, MainMenuV3SectionAlignment.BottomCenter);

            BuildHeader(header);
            BuildLeftNavigation(left);
            BuildMiddle(middle);
            BuildRight(right);
            BuildFooter(footer);

            UIShellContentSectionsView sections = root.AddComponent<UIShellContentSectionsView>();
            sections.ConfigureSections(new[]
            {
                new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Header, header.gameObject),
                new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Left, left.gameObject),
                new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Middle, middle.gameObject),
                new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Right, right.gameObject),
                new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Footer, footer.gameObject)
            });

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[CommanderProfileV3PrefabBuilder] result=Passed prefab rebuilt from shared commander art and procedural V3 chrome.");
        }

        [MenuItem("Game/UI/V3/Validate Commander Profile Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Commander Profile V3 prefab: {PrefabPath}");

            UIShellContentSectionsView sections = prefab.GetComponent<UIShellContentSectionsView>();
            if (sections == null)
                throw new MissingComponentException("Commander Profile V3 is missing UIShellContentSectionsView.");

            foreach (UIShellContentSectionId section in new[]
                     {
                         UIShellContentSectionId.Header,
                         UIShellContentSectionId.Left,
                         UIShellContentSectionId.Middle,
                         UIShellContentSectionId.Right,
                         UIShellContentSectionId.Footer
                     })
            {
                if (!sections.TryGetSection(section, out GameObject sectionRoot) || sectionRoot == null)
                    throw new MissingReferenceException($"Commander Profile V3 is missing section {section}.");
                if (sectionRoot.GetComponent<MainMenuV3SectionLayoutView>() == null)
                    throw new MissingComponentException($"Commander Profile V3 section {section} is not responsive.");
            }

            CommanderProfileContentView contentView = prefab.GetComponentInChildren<CommanderProfileContentView>(true);
            if (contentView == null || contentView.CommanderNameLabel == null || contentView.CommanderSubtitleLabel == null)
                throw new MissingReferenceException("Commander Profile runtime text bindings are incomplete.");

            Image portrait = FindDeepChild(prefab.transform, "CommanderScene")?.GetComponent<Image>();
            if (portrait == null || AssetDatabase.GetAssetPath(portrait.sprite) != CommanderScenePath)
                throw new MissingReferenceException("Commander Profile must reuse the canonical baked commander scene.");

            V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
            if (gradients.Length < 28)
                throw new InvalidOperationException($"Commander Profile requires procedural V3 gradients; found {gradients.Length}.");

            Debug.Log($"[CommanderProfileV3PrefabBuilder] validation=Passed gradients={gradients.Length} images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            commanderScene = RequireSprite(CommanderScenePath);
            campaignIcon = RequireSprite(CampaignIconPath);
            operationsIcon = RequireSprite(OperationsIconPath);
            skirmishIcon = RequireSprite(SkirmishIconPath);
            armoryIcon = RequireSprite(ArmoryIconPath);
            commanderRankIcon = RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath);
            commanderCrateIcon = RequireSprite(V3UiFoundationBuilder.CommanderCrateIconPath);
            commanderBackIcon = RequireSprite(V3UiFoundationBuilder.CommanderBackIconPath);
            commanderEditIcon = RequireSprite(V3UiFoundationBuilder.CommanderEditIconPath);
            commanderBadgeIcon = RequireSprite(V3UiFoundationBuilder.CommanderBadgeIconPath);
            commanderRosterIcon = RequireSprite(V3UiFoundationBuilder.CommanderRosterIconPath);
            commanderVehicleIcon = RequireSprite(V3UiFoundationBuilder.CommanderVehicleIconPath);
            commanderRewardIcon = RequireSprite(V3UiFoundationBuilder.CommanderRewardIconPath);
            commanderClaimIcon = RequireSprite(V3UiFoundationBuilder.CommanderClaimIconPath);
            commanderHistoryIcon = RequireSprite(V3UiFoundationBuilder.CommanderHistoryIconPath);
            commanderUpgradesIcon = RequireSprite(V3UiFoundationBuilder.CommanderUpgradesIconPath);
            commanderLockIcon = RequireSprite(V3UiFoundationBuilder.CommanderLockIconPath);
            commanderHeaderStarIcon = RequireSprite(V3UiFoundationBuilder.CommanderHeaderStarIconPath);
            commanderCheckIcon = RequireSprite(V3UiFoundationBuilder.CommanderCheckIconPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("Commander Profile V3 fonts are missing.");
        }

        private static RectTransform CreateSection(string name, Transform parent, MainMenuV3SectionAlignment alignment)
        {
            RectTransform section = CreateTopLeft(name, parent, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            section.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(ReferenceResolution, alignment);
            return section;
        }

        private static void BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 10f, 12f, 390f, 96f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            BuildResourceChip(root, "CreditsChip", 1038f, 14f, 265f, 90f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "CommandChip", 1310f, 14f, 245f, 90f, catalog.CommandIcon, "COMMAND", "8,430");

            Button settings = CreateGradientButton("SettingsButton", root, 1562f, 14f, 100f, 90f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetRect(gear.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50f, 50f), Vector2.zero);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 15f, 17f, 56f, 56f);
            TMP_Text labelText = CreateText("Label", chip, label, 20f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 82f, 9f, width - 92f, 33f);
            TMP_Text valueText = CreateText("Value", chip, value, 34f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 82f, 38f, width - 92f, 46f);
        }

        private static void BuildLeftNavigation(RectTransform root)
        {
            string[] labels = { "OVERVIEW", "STATS", "BADGES", "HISTORY", "UPGRADES" };
            Sprite[] icons = { catalog.AttackIcon, null, commanderBadgeIcon, commanderHistoryIcon, commanderUpgradesIcon };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 121f + i * 125f;
                bool selected = i == 0;
                Button button = CreateGradientButton(
                    labels[i] + "Tab",
                    root,
                    10f,
                    y,
                    245f,
                    118f,
                    selected ? BlueTop : DarkTop,
                    selected ? BlueBottom : DarkBottom,
                    selected ? theme.Cyan : Border,
                    3f);
                if (icons[i] != null)
                {
                    Image icon = CreateImage("Icon", button.transform, icons[i], Color.white, false);
                    SetTopLeft(icon.rectTransform, 21f, 28f, 59f, 59f);
                }
                else
                {
                    BuildBarsIcon(CreateTopLeft("StatsIcon", button.transform, 21f, 28f, 59f, 59f), theme.TextPrimary);
                }
                TMP_Text label = CreateText("Label", button.transform, labels[i], 24f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(label.rectTransform, 92f, 14f, 147f, 90f);
            }
        }

        private static void BuildMiddle(RectTransform root)
        {
            RectTransform artPanel = CreateTopLeft("CommanderArtPanel", root, 262f, 121f, 430f, 520f);
            CreateGradientPanel(artPanel, DarkTop, DarkBottom, Border, 3f);
            RectTransform artClip = CreateTopLeft("ArtClip", artPanel, 5f, 5f, 420f, 510f);
            artClip.gameObject.AddComponent<RectMask2D>();
            Image scene = CreateImage("CommanderScene", artClip, commanderScene, Color.white, false);
            Stretch(scene.rectTransform);
            AspectRatioFitter fitter = scene.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = commanderScene.rect.width / commanderScene.rect.height;
            // The profile lock uses a tighter, character-first crop than the main-menu scene.
            // Keep the same baked commander/environment source, but enlarge and offset the
            // presentation inside its mask so the commander reads at the locked scale.
            scene.rectTransform.localScale = Vector3.one * 1.28f;
            scene.rectTransform.anchoredPosition = new Vector2(-48f, -8f);

            RectTransform identity = CreateTopLeft("CommanderIdentityPanel", root, 692f, 121f, 430f, 520f);
            CreateGradientPanel(identity, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("Title", identity, "COMMANDER PROFILE", 31f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 28f, 11f, 360f, 56f);
            CreateSolidTopLeft("TitleRule", identity, 28f, 67f, 374f, 2f, Border);
            Image rankBadge = CreateImage("RankBadge", identity, commanderRankIcon, Color.white, false);
            SetTopLeft(rankBadge.rectTransform, 27f, 78f, 49f, 58f);
            TMP_Text rank = CreateText("Rank", identity, "FIELD COMMANDER", 22f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Cyan);
            SetTopLeft(rank.rectTransform, 82f, 81f, 260f, 40f);
            Button edit = CreateGradientButton("EditCommanderButton", identity, 360f, 78f, 45f, 45f, BlueTop, BlueBottom, theme.Cyan, 2f);
            Image editIcon = CreateImage("Icon", edit.transform, commanderEditIcon, Color.white, false);
            SetRect(editIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(29f, 29f), Vector2.zero);
            TMP_Text name = CreateText("CommanderName", identity, "COL. ALEX MORGAN", 37f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(name.rectTransform, 28f, 122f, 374f, 58f);
            TMP_Text subtitle = CreateText("CommanderSubtitle", identity, "SELECTED COMMANDER", 22f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Green);
            SetTopLeft(subtitle.rectTransform, 28f, 180f, 350f, 40f);
            TMP_Text levelLabel = CreateText("LevelLabel", identity, "LEVEL", 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextMuted);
            SetTopLeft(levelLabel.rectTransform, 62f, 244f, 115f, 32f);
            TMP_Text level = CreateText("Level", identity, "38", 54f, boldFont, TextAlignmentOptions.MidlineLeft, theme.Amber);
            SetTopLeft(level.rectTransform, 62f, 270f, 120f, 68f);
            TMP_Text xpLabel = CreateText("XpLabel", identity, "COMMANDER XP", 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextMuted);
            SetTopLeft(xpLabel.rectTransform, 62f, 348f, 200f, 30f);
            TMP_Text xp = CreateText("Xp", identity, "15,680 / 24,000 XP", 21f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(xp.rectTransform, 62f, 378f, 300f, 34f);
            RectTransform xpTrack = CreateTopLeft("XpTrack", identity, 62f, 421f, 310f, 20f);
            CreateGradientPanel(xpTrack, new Color32(30, 44, 48, 255), new Color32(12, 21, 24, 255), Border, 1f);
            RectTransform xpFill = CreateTopLeft("XpFill", xpTrack, 2f, 2f, 202f, 16f);
            CreateGradientPanel(xpFill, new Color32(37, 175, 238, 255), new Color32(5, 112, 184, 255), Color.clear, 0f);
            TMP_Text milestone = CreateText("Milestone", identity, "NEXT MILESTONE:  LEVEL 39", 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(milestone.rectTransform, 62f, 454f, 330f, 37f);
            identity.gameObject.AddComponent<CommanderProfileContentView>().Configure(name, subtitle);

            RectTransform stats = CreateTopLeft("CommanderStatsPanel", root, 262f, 645f, 860f, 145f);
            CreateGradientPanel(stats, DarkTop, DarkBottom, Border, 3f);
            string[] labels = { "VICTORIES", "MISSIONS", "CIVILIANS", "UNITS LOST", "WIN RATE" };
            string[] values = { "128", "246", "8,642", "312", "76%" };
            Color[] colors = { theme.Green, theme.Cyan, theme.Amber, theme.OrangeRed, new Color32(155, 100, 235, 255) };
            Sprite[] icons = { commanderRewardIcon, catalog.AttackIcon, commanderRosterIcon, commanderVehicleIcon, null };
            for (int i = 0; i < labels.Length; i++)
                BuildStat(stats, i * 172f, 172f, labels[i], values[i], colors[i], icons[i]);
        }

        private static void BuildStat(Transform root, float x, float width, string label, string value, Color color, Sprite icon)
        {
            if (x > 0f)
                CreateSolidTopLeft("Divider", root, x, 21f, 2f, 103f, Border);
            if (icon != null)
            {
                Image image = CreateImage("Icon", root, icon, Color.white, false);
                SetTopLeft(image.rectTransform, x + 16f, 35f, 42f, 42f);
            }
            else
            {
                BuildBarsIcon(CreateTopLeft("Icon", root, x + 16f, 35f, 42f, 42f), color);
            }
            TMP_Text labelText = CreateText("Label", root, label, 15f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, x + 64f, 23f, width - 70f, 37f);
            TMP_Text valueText = CreateText("Value", root, value, 33f, boldFont, TextAlignmentOptions.MidlineLeft, color);
            SetTopLeft(valueText.rectTransform, x + 64f, 58f, width - 70f, 58f);
        }

        private static void BuildRight(RectTransform root)
        {
            RectTransform rewards = CreateTopLeft("CommanderRewardTrack", root, 1131f, 121f, 531f, 290f);
            CreateGradientPanel(rewards, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("Title", rewards, "COMMANDER REWARD TRACK", 25f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 24f, 12f, 483f, 46f);
            int[] levels = { 36, 37, 38, 39, 40 };
            for (int i = 0; i < levels.Length; i++)
            {
                float x = 29f + i * 100f;
                bool active = levels[i] == 38;
                RectTransform node = CreateTopLeft("Level" + levels[i], rewards, x, 80f, 54f, 54f);
                CreateGradientPanel(node, active ? new Color32(76, 64, 4, 255) : DarkTop, active ? new Color32(28, 25, 4, 255) : DarkBottom, active ? theme.Amber : (levels[i] < 38 ? theme.Cyan : Border), 3f);
                TMP_Text level = CreateText("Label", node, levels[i].ToString(), 23f, boldFont, TextAlignmentOptions.Center, active ? theme.Amber : theme.TextPrimary);
                Stretch(level.rectTransform);
                if (i < levels.Length - 1)
                    CreateSolidTopLeft("Route", rewards, x + 54f, 105f, 46f, 3f, levels[i] < 38 ? theme.Cyan : Border);
            }

            for (int i = 0; i < 5; i++)
            {
                float x = 18f + i * 102f;
                RectTransform reward = CreateTopLeft("Reward" + i, rewards, x, 158f, 88f, 101f);
                CreateGradientPanel(reward, DarkTop, DarkBottom, i == 2 ? theme.Amber : Border, 3f);
                if (i == 1 || i == 2)
                {
                    Image icon = CreateImage("Icon", reward, armoryIcon, Color.white, false);
                    SetTopLeft(icon.rectTransform, 19f, 16f, 50f, 50f);
                }
                else if (i == 0)
                {
                    Image claim = CreateImage("Claimed", reward, commanderCheckIcon, theme.Green, false);
                    SetTopLeft(claim.rectTransform, 20f, 17f, 48f, 48f);
                }
                else if (i == 3)
                {
                    Image badge = CreateImage("Badge", reward, commanderRewardIcon, Color.white, false);
                    SetTopLeft(badge.rectTransform, 20f, 17f, 48f, 48f);
                }
                else
                {
                    Image lockIcon = CreateImage("Lock", reward, commanderLockIcon, Color.white, false);
                    SetTopLeft(lockIcon.rectTransform, 23f, 18f, 42f, 45f);
                }

                if (i == 1 || i == 2)
                {
                    Image icon = reward.Find("Icon")?.GetComponent<Image>();
                    if (icon != null)
                        icon.sprite = commanderCrateIcon;
                    if (i == 1)
                    {
                        Image checkedBadge = CreateImage("ClaimCheck", reward, commanderCheckIcon, theme.Green, false);
                        SetTopLeft(checkedBadge.rectTransform, 52f, 47f, 28f, 28f);
                    }
                }
            }

            RectTransform history = CreateTopLeft("RecentHistory", root, 1131f, 418f, 531f, 324f);
            CreateGradientPanel(history, DarkTop, DarkBottom, Border, 3f);
            TMP_Text historyTitle = CreateText("Title", history, "RECENT HISTORY", 24f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(historyTitle.rectTransform, 24f, 9f, 310f, 46f);
            Button viewAll = CreateGradientButton("ViewAll", history, 386f, 11f, 125f, 43f, BlueTop, BlueBottom, theme.Cyan, 2f);
            TMP_Text viewLabel = CreateText("Label", viewAll.transform, "VIEW ALL", 17f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            Stretch(viewLabel.rectTransform);
            BuildHistoryRow(history, 67f, catalog.AttackIcon, "HOSTILE PATROL", "CAMPAIGN", "VICTORY", "1h ago", theme.Green);
            BuildHistoryRow(history, 147f, commanderBadgeIcon, "SUPPLY RUN", "OPERATIONS", "VICTORY", "3h ago", theme.Green);
            BuildHistoryRow(history, 227f, commanderBadgeIcon, "CONVOY ESCORT", "OPERATIONS", "DEFEAT", "5h ago", theme.OrangeRed);
        }

        private static void BuildHistoryRow(Transform root, float y, Sprite icon, string title, string mode, string outcome, string time, Color outcomeColor)
        {
            CreateSolidTopLeft("Rule", root, 18f, y - 7f, 495f, 2f, Border);
            Image image = CreateImage("Icon", root, icon, Color.white, false);
            SetTopLeft(image.rectTransform, 24f, y + 10f, 45f, 45f);
            TMP_Text titleText = CreateText("Title", root, title, 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(titleText.rectTransform, 83f, y, 220f, 32f);
            TMP_Text modeText = CreateText("Mode", root, mode, 14f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextMuted);
            SetTopLeft(modeText.rectTransform, 83f, y + 31f, 180f, 24f);
            TMP_Text outcomeText = CreateText("Outcome", root, outcome, 18f, boldFont, TextAlignmentOptions.MidlineRight, outcomeColor);
            SetTopLeft(outcomeText.rectTransform, 300f, y + 8f, 130f, 40f);
            TMP_Text timeText = CreateText("Time", root, time, 15f, mediumFont, TextAlignmentOptions.MidlineRight, theme.TextMuted);
            SetTopLeft(timeText.rectTransform, 430f, y + 8f, 70f, 40f);
        }

        private static void BuildFooter(RectTransform root)
        {
            Button back = CreateGradientButton("BackButton", root, 10f, 804f, 335f, 123f, DarkTop, DarkBottom, Border, 3f);
            Image backIcon = CreateImage("Icon", back.transform, commanderBackIcon, Color.white, false);
            SetTopLeft(backIcon.rectTransform, 74f, 32f, 57f, 57f);
            TMP_Text backLabel = CreateText("Label", back.transform, "BACK", 31f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(backLabel.rectTransform, 123f, 8f, 176f, 106f);
            back.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);

            Button armory = CreateGradientButton("OpenArmoryButton", root, 355f, 804f, 610f, 123f, BlueTop, BlueBottom, theme.Cyan, 3f);
            Image armoryImage = CreateImage("Icon", armory.transform, commanderCrateIcon, Color.white, false);
            SetTopLeft(armoryImage.rectTransform, 88f, 28f, 67f, 67f);
            TMP_Text armoryLabel = CreateText("Label", armory.transform, "OPEN ARMORY", 34f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(armoryLabel.rectTransform, 148f, 10f, 410f, 103f);
            armory.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, true);

            Button change = CreateGradientButton("ChangeCommanderButton", root, 975f, 804f, 687f, 123f, GreenTop, GreenBottom, theme.Green, 3f);
            Image changeIcon = CreateImage("Icon", change.transform, commanderUpgradesIcon, Color.white, false);
            SetTopLeft(changeIcon.rectTransform, 113f, 30f, 62f, 62f);
            TMP_Text changeLabel = CreateText("Label", change.transform, "CHANGE COMMANDER", 33f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(changeLabel.rectTransform, 176f, 8f, 455f, 106f);
            change.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
        }

        private static Button CreateGradientButton(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradientPanel(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f),
                pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            return button;
        }

        private static V3GradientGraphic CreateGradientPanel(RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            return graphic;
        }

        private static void BuildRankMark(RectTransform root)
        {
            Image star = CreateImage("Star", root, commanderHeaderStarIcon, theme.Amber, false);
            SetRect(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f), new Vector2(0f, 18f));
            for (int i = 0; i < 2; i++)
            {
                float y = -17f - i * 15f;
                Image left = CreateSolid("ChevronLeft" + i, root, theme.Amber, new Vector2(25f, 5f), new Vector2(-9f, y));
                left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
                Image right = CreateSolid("ChevronRight" + i, root, theme.Amber, new Vector2(25f, 5f), new Vector2(9f, y));
                right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            }
        }

        private static void BuildBarsIcon(RectTransform root, Color color)
        {
            float scale = Mathf.Max(0.5f, root.sizeDelta.x / 59f);
            float[] heights = { 21f * scale, 39f * scale, 55f * scale };
            for (int i = 0; i < heights.Length; i++)
            {
                Image bar = CreateSolidTopLeft(
                    "Bar" + i,
                    root,
                    (6f + i * 17f) * scale,
                    root.sizeDelta.y - heights[i],
                    11f * scale,
                    heights[i],
                    color);
                bar.raycastTarget = false;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position) =>
            V3UiPrefabFactory.CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, position);

        private static RectTransform CreateTopLeft(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(width, height), new Vector2(x, -y));
            rect.pivot = new Vector2(0f, 1f);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycast) =>
            V3UiPrefabFactory.CreateImage(name, parent, sprite, color, raycast, false);

        private static Image CreateSolid(string name, Transform parent, Color color, Vector2 size, Vector2 position)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            return image;
        }

        private static Image CreateSolidTopLeft(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string textValue, float fontSize, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 60f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect) => SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new MissingReferenceException($"Missing Commander Profile V3 sprite: {path}");
            return sprite;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            if (parent.name == name)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = FindDeepChild(parent.GetChild(i), name);
                if (child != null)
                    return child;
            }
            return null;
        }
    }
}
