#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MainMenuV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string BackgroundPath = "Assets/Game/Art/UI/Generated/MainMenu/V3/scn02_v3_commander_background.png";
        private const string CampaignPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_campaign_thumbnail_art.png";
        private const string OperationsPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_operations_thumbnail_art.png";
        private const string SkirmishPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_skirmish_thumbnail_art.png";
        private const string AriaPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_aria.png";
        private const string StoreIconPath = "Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas/Cleaned/ui_left_nav_icon_store.png";
        private const string ArmoryIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_armory_crossed_weapons.png";
        private const string SettingsIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_settings_gear.png";
        private const string CampaignIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_campaign_crosshair.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_operations_pin.png";
        private const string SkirmishIconPath = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_icon_skirmish_blades.png";
        private const string CreditsIconPath = "Assets/Game/Art/UI/Resources/resource_credits.png";
        private const string CommandIconPath = "Assets/Game/Art/UI/Resources/resource_command.png";
        private const string RankIconPath = "Assets/Game/Art/UI/Icons/scn08_icon_shield_rank_badge.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color Ink = new(0.012f, 0.024f, 0.028f, 0.985f);
        private static readonly Color Panel = new(0.018f, 0.038f, 0.043f, 0.965f);
        private static readonly Color PanelSoft = new(0.026f, 0.052f, 0.057f, 0.90f);
        private static readonly Color Border = new(0.20f, 0.27f, 0.28f, 1f);
        private static readonly Color White = new(0.95f, 0.96f, 0.94f, 1f);
        private static readonly Color Muted = new(0.68f, 0.72f, 0.71f, 1f);
        private static readonly Color Gold = new(1f, 0.65f, 0.015f, 1f);
        private static readonly Color Orange = new(0.96f, 0.20f, 0.035f, 1f);
        private static readonly Color Green = new(0.05f, 0.67f, 0.30f, 1f);
        private static readonly Color Cyan = new(0.02f, 0.69f, 0.94f, 1f);
        private static readonly Color Navy = new(0.035f, 0.15f, 0.28f, 1f);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/V3/Build SCN-02 Main Menu")]
        public static void Build()
        {
            LoadAssets();
            EnsureSpriteImport(BackgroundPath, 4096, false);
            EnsureSpriteImport(AriaPath, 1024, true);
            EnsureSpriteImport(StoreIconPath, 1024, true);
            EnsureSpriteImport(ArmoryIconPath, 512, true);
            EnsureSpriteImport(SettingsIconPath, 512, true);
            EnsureSpriteImport(CampaignIconPath, 512, true);
            EnsureSpriteImport(OperationsIconPath, 512, true);
            EnsureSpriteImport(SkirmishIconPath, 512, true);
            EnsureSpriteImport(CreditsIconPath, 512, true);
            EnsureSpriteImport(CommandIconPath, 512, true);
            EnsureSpriteImport(RankIconPath, 512, true);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ClearChildren(root.transform);
                RemoveComponent<MainMenuNavigationView>(root);
                Stretch(RequireRect(root));

                RectTransform background = CreateSection("MenuBackgroundContent", root.transform);
                RectTransform header = CreateSection("HeaderContent", root.transform);
                RectTransform left = CreateSection("LeftContent", root.transform);
                RectTransform middle = CreateSection("MiddleContent", root.transform);
                RectTransform right = CreateSection("RightContent", root.transform);
                RectTransform footer = CreateSection("FooterContent", root.transform);

                UIShellContentSectionsView sections = root.GetComponent<UIShellContentSectionsView>() ?? root.AddComponent<UIShellContentSectionsView>();
                sections.ConfigureSections(new[]
                {
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.MenuBackground, background.gameObject),
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Header, header.gameObject),
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Left, left.gameObject),
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Middle, middle.gameObject),
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Right, right.gameObject),
                    new UIShellContentSectionsView.SectionReference(UIShellContentSectionId.Footer, footer.gameObject)
                });

                BuildBackground(background);
                BuildHeader(header);
                BuildLeftCards(left);
                BuildRightRail(right);
                BuildFooter(footer);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MainMenuV3PrefabBuilder] result=Passed prefab={PrefabPath}");
        }

        [MenuItem("Game/UI/V3/Validate SCN-02 Main Menu")]
        public static void Validate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UIShellContentSectionsView sections = root.GetComponent<UIShellContentSectionsView>();
                if (sections == null || sections.Sections == null || sections.Sections.Count != 6)
                    throw new InvalidOperationException("SCN-02 V3 must expose exactly six shell content sections.");

                ValidateRoute(root, "Card_Campaign", UIRoute.Campaign, true);
                ValidateRoute(root, "Card_Operations", UIRoute.Operations, true);
                ValidateRoute(root, "Card_Skirmish", UIRoute.QuickCustomSetup, true);
                ValidateRoute(root, "CommanderPanelHotspot", UIRoute.CommandFeed, true);
                ValidateRoute(root, "StoreButton", UIRoute.MainMenu, false);
                ValidateRoute(root, "ArmoryButton", UIRoute.Armory, true);

                Transform settingsTransform = FindDescendant(root.transform, "SettingsButton");
                UIShellActionButtonView settings = settingsTransform != null
                    ? settingsTransform.GetComponent<UIShellActionButtonView>()
                    : null;
                if (settings == null || settings.ActionKind != UiActionKind.OpenSettings)
                    throw new InvalidOperationException("SCN-02 V3 SettingsButton is not wired to OpenSettings.");

                MainMenuV3ResponsiveLayoutView[] responsive = root.GetComponentsInChildren<MainMenuV3ResponsiveLayoutView>(true);
                if (responsive.Length != 4)
                    throw new InvalidOperationException($"SCN-02 V3 requires four responsive section hosts; found {responsive.Length}.");

                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                if (buttons.Length != 7)
                    throw new InvalidOperationException($"SCN-02 V3 expected seven interactive controls; found {buttons.Length}.");
                for (int i = 0; i < buttons.Length; i++)
                {
                    ColorBlock colors = buttons[i].colors;
                    if (!Mathf.Approximately(colors.disabledColor.a, colors.normalColor.a))
                        throw new InvalidOperationException($"{buttons[i].name} changes alpha when disabled.");
                }

                TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled)
                {
                    throw new InvalidOperationException("SCN-02 V3 background import contract failed.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("[MainMenuV3BindingValidation] result=Passed sections=6 routes=6 actions=1 buttons=7 responsiveHosts=4 disabledAlpha=Opaque");
        }

        private static void BuildBackground(Transform root)
        {
            Image background = CreateImage("V3_CommanderBackground", root, BackgroundPath, Color.white, false);
            Stretch(background.rectTransform);
            background.preserveAspect = true;
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;

            Image grade = CreateSolid("V3_ReadabilityGrade", root, new Color(0.005f, 0.012f, 0.014f, 0.12f));
            Stretch(grade.rectTransform);
        }

        private static void BuildHeader(Transform root)
        {
            RectTransform bar = CreateAnchored("V3_HeaderBar", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<MainMenuV3ResponsiveLayoutView>().Configure(MainMenuV3ResponsiveLayoutView.RegionLayoutKind.Header, bar);
            CreateSolid("Fill", bar, new Color(0.006f, 0.018f, 0.022f, 0.97f), true);
            SetStretchOffsets(bar.Find("Fill").GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            CreateSolid("BottomRail", bar, Gold, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 5f));

            RectTransform brand = CreatePanel("V3_Brand", bar, new Vector2(0.008f, 0.10f), new Vector2(0.315f, 0.94f), Ink, Border);
            CreateSolid("GoldRail", brand, Gold, new Vector2(0f, 0.18f), new Vector2(0f, 0.82f), new Vector2(28f, 0f), new Vector2(34f, 0f));
            CreateText("Warline", brand, "WARLINE", 152f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.09f, 0.36f), new Vector2(0.80f, 0.94f), boldFont, 2f);
            CreateText("Capture", brand, "CAPTURE", 70f, Gold, TextAlignmentOptions.Center,
                new Vector2(0.16f, 0.08f), new Vector2(0.72f, 0.38f), boldFont, 7f);
            CreateSolid("CaptureRailLeft", brand, Gold, new Vector2(0.07f, 0.17f), new Vector2(0.16f, 0.17f), new Vector2(0f, 8f), Vector2.zero);
            CreateSolid("CaptureRailRight", brand, Gold, new Vector2(0.72f, 0.17f), new Vector2(0.81f, 0.17f), new Vector2(0f, 8f), Vector2.zero);
            Image rank = CreateImage("Rank", brand, RankIconPath, Gold, false);
            SetAnchors(rank.rectTransform, new Vector2(0.82f, 0.16f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);
            rank.preserveAspect = true;

            RectTransform resourceArea = CreateAnchored("HeaderResourceArea", bar, new Vector2(0.57f, 0.17f), new Vector2(0.91f, 0.88f), Vector2.zero, Vector2.zero);
            BuildResourcePanel(resourceArea, "CreditsPanel", new Vector2(0f, 0f), new Vector2(0.49f, 1f), CreditsIconPath, "CREDITS", "24,750", Gold);
            BuildResourcePanel(resourceArea, "CommandPanel", new Vector2(0.51f, 0f), Vector2.one, CommandIconPath, "COMMAND", "8,430", Cyan);

            Button settings = CreateButton("SettingsButton", bar, new Vector2(0.925f, 0.16f), new Vector2(0.99f, 0.88f), Panel, Border);
            Image gear = CreateImage("Icon", settings.transform, SettingsIconPath, White, false);
            SetAnchors(gear.rectTransform, new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f), Vector2.zero, Vector2.zero);
            gear.preserveAspect = true;
            UIShellActionButtonView action = settings.gameObject.AddComponent<UIShellActionButtonView>();
            SerializedObject serialized = new(action);
            serialized.FindProperty("actionKind").enumValueIndex = (int)UiActionKind.OpenSettings;
            serialized.FindProperty("payloadId").intValue = 0;
            serialized.FindProperty("button").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildResourcePanel(Transform parent, string name, Vector2 min, Vector2 max, string iconPath, string label, string value, Color accent)
        {
            RectTransform panel = CreatePanel(name, parent, min, max, Ink, Border);
            RectTransform frame = CreateAnchored("Frame", panel, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            Image icon = CreateImage("Icon", frame, iconPath, Color.white, false);
            SetAnchors(icon.rectTransform, new Vector2(0.04f, 0.16f), new Vector2(0.28f, 0.84f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            CreateText("Label", frame, label, 43f, White, TextAlignmentOptions.BottomLeft,
                new Vector2(0.31f, 0.50f), new Vector2(0.94f, 0.88f), boldFont, 1f);
            CreateText("Value", frame, value, 71f, accent, TextAlignmentOptions.TopLeft,
                new Vector2(0.31f, 0.10f), new Vector2(0.94f, 0.56f), boldFont, 1f);
        }

        private static void BuildLeftCards(Transform root)
        {
            RectTransform zone = CreateAnchored("V3_LeftCards", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<MainMenuV3ResponsiveLayoutView>().Configure(MainMenuV3ResponsiveLayoutView.RegionLayoutKind.Left, zone);
            BuildCampaignCard(zone);
            BuildCompactCard(zone, "Card_Operations", new Vector2(0f, 0.205f), new Vector2(1f, 0.39f), OperationsPath,
                "OPERATIONS", "LIVE DISTRICT COMMAND", Green, OperationsIconPath, UIRoute.Operations);
            BuildCompactCard(zone, "Card_Skirmish", new Vector2(0f, 0f), new Vector2(1f, 0.185f), SkirmishPath,
                "SKIRMISH", "CUSTOM BATTLE", Orange, SkirmishIconPath, UIRoute.QuickCustomSetup);
        }

        private static void BuildCampaignCard(Transform parent)
        {
            RectTransform card = CreateAnchored("Card_Campaign", parent, new Vector2(0f, 0.415f), Vector2.one, Vector2.zero, Vector2.zero);
            Button button = CreateButton("Hotspot", card, Vector2.zero, Vector2.one, new Color(0.19f, 0.10f, 0.01f, 0.95f), Gold);
            AddRoute(button, UIRoute.Campaign, true);
            RawImage art = CreateCroppedImage("Art", button.transform, CampaignPath, new Color(1f, 0.76f, 0.36f, 0.84f));
            SetAnchors(art.rectTransform, new Vector2(0.005f, 0.005f), new Vector2(0.995f, 0.995f), Vector2.zero, Vector2.zero);
            CreateSolid("TopShade", button.transform, new Color(0.08f, 0.035f, 0f, 0.40f), new Vector2(0.005f, 0.62f), new Vector2(0.995f, 0.995f), Vector2.zero, Vector2.zero);
            CreateSolid("BottomShade", button.transform, new Color(0.035f, 0.012f, 0f, 0.80f), new Vector2(0.005f, 0.005f), new Vector2(0.995f, 0.24f), Vector2.zero, Vector2.zero);
            CreateSolid("IconTile", button.transform, new Color(0.04f, 0.05f, 0.04f, 0.82f), new Vector2(0.005f, 0.70f), new Vector2(0.15f, 0.995f), Vector2.zero, Vector2.zero);
            Image icon = CreateImage("Icon", button.transform, CampaignIconPath, Gold, false);
            SetAnchors(icon.rectTransform, new Vector2(0.02f, 0.73f), new Vector2(0.135f, 0.97f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            CreateText("Title", button.transform, "CAMPAIGN", 128f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.72f), new Vector2(0.89f, 0.98f), boldFont, 1f);
            CreateText("Subtitle", button.transform, "CONTINUE CAMPAIGN  ›", 62f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.62f), new Vector2(0.89f, 0.76f), boldFont, 1f);
            RectTransform quote = CreatePanel("CampaignQuote", button.transform, new Vector2(0.03f, 0.29f), new Vector2(0.52f, 0.51f), new Color(0.90f, 0.88f, 0.80f, 0.98f), new Color(0.82f, 0.77f, 0.65f, 1f));
            CreateText("Quote", quote, "PEOPLE ARE COUNTING ON US.\nKEEP THEM SAFE.", 42f, new Color(0.06f, 0.07f, 0.06f, 1f), TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), boldFont, 0f, true);
            RectTransform emergency = CreatePanel("Emergency", button.transform, new Vector2(0.03f, 0.035f), new Vector2(0.72f, 0.20f), new Color(0.04f, 0.025f, 0.018f, 0.98f), Orange);
            CreateText("Alert", emergency, "!", 62f, Orange, TextAlignmentOptions.Center,
                new Vector2(0.02f, 0.08f), new Vector2(0.14f, 0.92f), boldFont);
            CreateText("Label", emergency, "EMERGENCY: CIVILIANS AT RISK", 53f, Orange, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.15f, 0.08f), new Vector2(0.98f, 0.92f), boldFont, 1f);
        }

        private static void BuildCompactCard(Transform parent, string name, Vector2 min, Vector2 max, string texturePath,
            string title, string subtitle, Color accent, string iconPath, UIRoute route)
        {
            RectTransform card = CreateAnchored(name, parent, min, max, Vector2.zero, Vector2.zero);
            Button button = CreateButton("Hotspot", card, Vector2.zero, Vector2.one, Ink, accent);
            AddRoute(button, route, true);
            RawImage art = CreateCroppedImage("Art", button.transform, texturePath, new Color(1f, 1f, 1f, 0.80f));
            SetAnchors(art.rectTransform, new Vector2(0.005f, 0.02f), new Vector2(0.995f, 0.98f), Vector2.zero, Vector2.zero);
            CreateSolid("Readability", button.transform, new Color(0.005f, 0.014f, 0.016f, 0.43f), new Vector2(0.005f, 0.02f), new Vector2(0.995f, 0.98f), Vector2.zero, Vector2.zero);
            CreateSolid("IconTile", button.transform, new Color(0.01f, 0.03f, 0.025f, 0.86f), new Vector2(0.005f, 0.02f), new Vector2(0.15f, 0.98f), Vector2.zero, Vector2.zero);
            Image icon = CreateImage("Icon", button.transform, iconPath, accent, false);
            SetAnchors(icon.rectTransform, new Vector2(0.02f, 0.10f), new Vector2(0.135f, 0.90f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            CreateText("Title", button.transform, title, 72f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.42f), new Vector2(0.74f, 0.98f), boldFont, 1f);
            CreateText("Subtitle", button.transform, subtitle, 27f, Muted, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.18f), new Vector2(0.74f, 0.44f), mediumFont, 1f);
            CreateText("Chevron", button.transform, "›", 104f, White, TextAlignmentOptions.Center,
                new Vector2(0.88f, 0.08f), new Vector2(0.985f, 0.92f), boldFont);
        }

        private static void BuildRightRail(Transform root)
        {
            RectTransform zone = CreateAnchored("V3_RightRail", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<MainMenuV3ResponsiveLayoutView>().Configure(MainMenuV3ResponsiveLayoutView.RegionLayoutKind.Right, zone);
            RectTransform aria = CreatePanel("AriaPanel", zone, new Vector2(0f, 0.35f), Vector2.one, new Color(0.005f, 0.045f, 0.075f, 0.96f), Cyan);
            CreateText("Title", aria, "ARIA", 88f, Cyan, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.82f), new Vector2(0.42f, 0.98f), boldFont, 2f);
            CreateText("Status", aria, "COMMAND ASSISTANT  •  ONLINE", 28f, Cyan, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.75f), new Vector2(0.94f, 0.84f), mediumFont, 1f);
            Image portrait = CreateImage("Portrait", aria, AriaPath, new Color(0.75f, 0.96f, 1f, 1f), false);
            SetAnchors(portrait.rectTransform, new Vector2(0.11f, 0.02f), new Vector2(0.90f, 0.78f), Vector2.zero, Vector2.zero);
            portrait.preserveAspect = true;
            for (int i = 0; i < 6; i++)
            {
                float y = 0.10f + i * 0.065f;
                CreateSolid($"Telemetry{i + 1}", aria, new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f),
                    new Vector2(0.04f, y), new Vector2(0.075f + i * 0.012f, y + 0.018f), Vector2.zero, Vector2.zero);
            }

            RectTransform commander = CreatePanel("CommanderPanel", zone, new Vector2(0f, 0f), new Vector2(1f, 0.325f), new Color(0.035f, 0.075f, 0.035f, 0.97f), new Color(0.30f, 0.39f, 0.22f, 1f));
            Image rank = CreateImage("Rank", commander, RankIconPath, Gold, false);
            SetAnchors(rank.rectTransform, new Vector2(0.05f, 0.58f), new Vector2(0.22f, 0.91f), Vector2.zero, Vector2.zero);
            rank.preserveAspect = true;
            CreateText("Title", commander, "FIELD COMMANDER", 58f, White, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.25f, 0.70f), new Vector2(0.96f, 0.92f), boldFont, 1f);
            CreateText("Status", commander, "SELECTED COMMANDER", 42f, Gold, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.25f, 0.54f), new Vector2(0.96f, 0.73f), boldFont, 1f);
            Button change = CreateButton("CommanderPanelHotspot", commander, new Vector2(0.07f, 0.20f), new Vector2(0.93f, 0.50f), new Color(0.11f, 0.25f, 0.10f, 1f), new Color(0.38f, 0.52f, 0.28f, 1f));
            AddRoute(change, UIRoute.CommandFeed, true);
            CreateText("Label", change.transform, "CHANGE   ›", 56f, White, TextAlignmentOptions.Center,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), boldFont, 2f);
        }

        private static void BuildFooter(Transform root)
        {
            RectTransform bar = CreateAnchored("V3_FooterBar", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<MainMenuV3ResponsiveLayoutView>().Configure(MainMenuV3ResponsiveLayoutView.RegionLayoutKind.Footer, bar);
            Button store = CreateButton("StoreButton", bar, new Vector2(0.008f, 0.06f), new Vector2(0.455f, 0.94f), new Color(0.01f, 0.23f, 0.42f, 0.98f), new Color(0.02f, 0.49f, 0.78f, 1f));
            AddRoute(store, UIRoute.MainMenu, false);
            Image storeIcon = CreateImage("Icon", store.transform, StoreIconPath, White, false);
            SetAnchors(storeIcon.rectTransform, new Vector2(0.16f, 0.17f), new Vector2(0.31f, 0.83f), Vector2.zero, Vector2.zero);
            storeIcon.preserveAspect = true;
            CreateText("Label", store.transform, "STORE", 112f, White, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0.08f), new Vector2(0.86f, 0.92f), boldFont, 1f);

            Button armory = CreateButton("ArmoryButton", bar, new Vector2(0.46f, 0.06f), new Vector2(0.992f, 0.94f), Navy, new Color(0.13f, 0.28f, 0.52f, 1f));
            AddRoute(armory, UIRoute.Armory, true);
            Image armoryIcon = CreateImage("Icon", armory.transform, ArmoryIconPath, White, false);
            SetAnchors(armoryIcon.rectTransform, new Vector2(0.18f, 0.17f), new Vector2(0.31f, 0.83f), Vector2.zero, Vector2.zero);
            armoryIcon.preserveAspect = true;
            CreateText("Label", armory.transform, "ARMORY", 112f, White, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0.08f), new Vector2(0.84f, 0.92f), boldFont, 1f);
        }

        private static Button CreateButton(string name, Transform parent, Vector2 min, Vector2 max, Color fill, Color border)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, min, max, Vector2.zero, Vector2.zero);
            Image image = root.AddComponent<Image>();
            image.color = border;
            image.raycastTarget = true;
            Image inner = CreateSolid("Fill", root.transform, fill);
            SetStretchOffsets(inner.rectTransform, 8f, 8f, -8f, -8f);
            inner.transform.SetAsFirstSibling();

            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = colors.normalColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            return button;
        }

        private static void AddRoute(Button button, UIRoute route, bool pushHistory)
        {
            UIShellRouteButtonView view = button.gameObject.AddComponent<UIShellRouteButtonView>();
            view.Configure(UiShellRouteIntent.OpenMenuRoute, route, pushHistory);
        }

        private static void ValidateRoute(GameObject root, string objectName, UIRoute expectedRoute, bool expectedPushHistory)
        {
            Transform transform = FindDescendant(root.transform, objectName);
            UIShellRouteButtonView route = transform != null
                ? transform.GetComponent<UIShellRouteButtonView>() ?? transform.GetComponentInChildren<UIShellRouteButtonView>(true)
                : null;
            if (route == null || route.Intent != UiShellRouteIntent.OpenMenuRoute ||
                route.Route != expectedRoute || route.PushHistory != expectedPushHistory)
            {
                throw new InvalidOperationException(
                    $"SCN-02 V3 route mismatch object={objectName} expected={expectedRoute}/{expectedPushHistory}.");
            }
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
                return null;
            if (root.name == objectName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), objectName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color fill, Color border)
        {
            RectTransform outer = CreateAnchored(name, parent, min, max, Vector2.zero, Vector2.zero);
            Image borderImage = outer.gameObject.AddComponent<Image>();
            borderImage.color = border;
            borderImage.raycastTarget = false;
            Image inner = CreateSolid("Fill", outer, fill);
            SetStretchOffsets(inner.rectTransform, 8f, 8f, -8f, -8f);
            inner.transform.SetAsFirstSibling();
            return outer;
        }

        private static RawImage CreateCroppedImage(string name, Transform parent, string path, Color tint)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                throw new InvalidOperationException($"Missing UI texture: {path}");
            GameObject root = CreateRect(name, parent);
            RawImage image = root.AddComponent<RawImage>();
            image.texture = texture;
            image.color = tint;
            image.raycastTarget = false;
            image.uvRect = new Rect(0f, 0.12f, 1f, 0.76f);
            return image;
        }

        private static Image CreateImage(string name, Transform parent, string path, Color tint, bool raycast)
        {
            GameObject root = CreateRect(name, parent);
            Image image = root.AddComponent<Image>();
            image.sprite = RequireSprite(path);
            image.color = tint;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, bool raycast = false)
        {
            GameObject root = CreateRect(name, parent);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, Color color, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            Image image = CreateSolid(name, parent, color);
            SetAnchors(image.rectTransform, min, max, offsetMin, offsetMax);
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Color color,
            TextAlignmentOptions alignment, Vector2 min, Vector2 max, TMP_FontAsset font, float spacing = 0f, bool wrap = false)
        {
            GameObject root = CreateRect(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchors(rect, min, max, Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.characterSpacing = spacing;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateSection(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent).GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateAnchored(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = CreateRect(name, parent).GetComponent<RectTransform>();
            SetAnchors(rect, min, max, offsetMin, offsetMax);
            return rect;
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return root;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetStretchOffsets(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static void Stretch(RectTransform rect)
        {
            SetStretchOffsets(rect, 0f, 0f, 0f, 0f);
        }

        private static RectTransform RequireRect(GameObject root)
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                throw new InvalidOperationException($"Expected RectTransform on {root.name}.");
            return rect;
        }

        private static void ClearChildren(Transform root)
        {
            while (root.childCount > 0)
                UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
        }

        private static void RemoveComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
                UnityEngine.Object.DestroyImmediate(component, true);
        }

        private static void LoadAssets()
        {
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null || mediumFont == null)
                throw new InvalidOperationException("SCN-02 V3 requires the Oxanium Bold and Medium TMP font assets.");
        }

        private static void EnsureSpriteImport(string path, int maxSize, bool alpha)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing texture importer: {path}");
            bool dirty = importer.textureType != TextureImporterType.Sprite ||
                         importer.spriteImportMode != SpriteImportMode.Single ||
                         importer.mipmapEnabled ||
                         importer.maxTextureSize != maxSize ||
                         importer.alphaIsTransparency != alpha;
            if (!dirty)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing UI sprite: {path}");
            return sprite;
        }
    }
}
#endif
