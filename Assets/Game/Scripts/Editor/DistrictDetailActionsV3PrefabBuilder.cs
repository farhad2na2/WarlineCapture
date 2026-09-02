using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class DistrictDetailActionsV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN12_DistrictDetailActionsContent.prefab";
        private const string MapPath = "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string AriaPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
        private const string ConfirmRaidPopupPath = "Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(49, 65, 70, 255);
        private static readonly Color DarkTop = new Color32(19, 29, 33, 255);
        private static readonly Color DarkBottom = new Color32(2, 8, 11, 255);
        private static readonly Color Cyan = new Color32(16, 183, 231, 255);
        private static readonly Color Lime = new Color32(132, 194, 48, 255);
        private static readonly Color Amber = new Color32(255, 180, 0, 255);
        private static readonly Color Orange = new Color32(239, 88, 32, 255);
        private static readonly Color Red = new Color32(241, 61, 33, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Texture2D mapTexture;
        private static Sprite ariaSprite;

        [MenuItem("Game/UI/V3/Rebuild District Detail Actions Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform root = CreateRect("SCN12_DistrictDetailActionsContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateGradientPanel(root, new Color32(11, 18, 21, 255), new Color32(1, 5, 7, 255), Color.clear, 0f);
            RectTransform composition = CreateTopLeft("DistrictDetailActionsComposition", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            MainMenuV3SectionLayoutView responsive = composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>();
            DistrictDetailActionsScreenView screen = composition.gameObject.AddComponent<DistrictDetailActionsScreenView>();

            UIShellRouteButtonView backRoute = BuildHeader(composition);
            BuildDistrictVisual(composition, out RawImage districtImage, out TMP_Text districtName, out TMP_Text threatLabel);
            BuildKeyStats(composition);
            Slider intelConfidence = BuildIntelAndThreats(composition);
            BuildAriaStatus(composition, out Image ariaPortrait);
            BuildRecentActivity(composition);
            Button[] actions = BuildActionBar(composition);
            ConfigureResponsiveLayout(composition, responsive);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", backRoute);
            SetReference(serialized, "districtImage", districtImage);
            SetReference(serialized, "ariaPortrait", ariaPortrait);
            SetReference(serialized, "districtName", districtName);
            SetReference(serialized, "threatLabel", threatLabel);
            SetReference(serialized, "intelConfidence", intelConfidence);
            SetArray(serialized, "actionButtons", actions);
            SetReference(serialized, "confirmRaidPopupPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmRaidPopupPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save District Detail V3 prefab: {PrefabPath}");
            AssignToOpenMenuScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[DistrictDetailActionsV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 map=aspect-preserved aria=aspect-preserved atlases=reused-shared");
        }

        [MenuItem("Game/UI/V3/Validate District Detail Actions Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing District Detail V3 prefab: {PrefabPath}");
            string[] required = { "DistrictVisual", "KeyStats", "IntelConfidence", "KnownThreats", "AriaStatus", "RecentActivity", "ActionBar" };
            for (int i = 0; i < required.Length; i++)
                if (FindChild(prefab.transform, required[i]) == null)
                    throw new MissingReferenceException($"District Detail V3 is missing {required[i]}.");
            if (prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length < 20)
                throw new InvalidOperationException("District Detail V3 does not contain enough procedural gradient surfaces.");
            if (prefab.GetComponentsInChildren<DistrictDetailActionsScreenView>(true).Length != 1)
                throw new InvalidOperationException("District Detail V3 requires one runtime screen view.");
            Image aria = FindChild(prefab.transform, "AriaPortrait")?.GetComponent<Image>();
            RawImage map = FindChild(prefab.transform, "DistrictImage")?.GetComponent<RawImage>();
            if (aria == null || aria.GetComponent<AspectRatioFitter>() == null || map == null || map.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("District Detail V3 must preserve both ARIA and district-map aspect ratios.");
            Button[] actions = FindChild(prefab.transform, "ActionBar")?.GetComponentsInChildren<Button>(true) ?? Array.Empty<Button>();
            if (actions.Length != 7 || actions[5].interactable || actions[6].interactable)
                throw new InvalidOperationException("District Detail V3 requires five active and two locked action cards.");
            DistrictDetailActionsScreenView screen = prefab.GetComponentInChildren<DistrictDetailActionsScreenView>(true);
            if (screen == null || screen.ConfirmRaidPopupPrefab == null)
                throw new MissingReferenceException("District Detail V3 requires its shared functional raid confirmation.");
            MainMenuV3SectionLayoutView responsive = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (responsive == null || !responsive.ExpandToCanvasWidth || responsive.RightAnchoredTargets.Length != 8)
                throw new InvalidOperationException("District Detail V3 must fill 16:9 and 20:9 canvases with its right-side panels anchored.");
            Debug.Log($"[DistrictDetailActionsV3PrefabBuilder] validation=Passed gradients={prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length} actions=7 images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        private static void LoadAssets()
        {
            ConfigureTexture(MapPath, 4096, false);
            ConfigureTexture(AriaPath, 1024, true);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
            ariaSprite = RequireSprite(AriaPath);
            if (boldFont == null || mediumFont == null || mapTexture == null || ariaSprite == null)
                throw new MissingReferenceException("District Detail V3 shared art or fonts are missing.");
        }

        private static void ConfigureResponsiveLayout(
            RectTransform composition,
            MainMenuV3SectionLayoutView responsive)
        {
            responsive.Configure(
                ReferenceResolution,
                MainMenuV3SectionAlignment.Center,
                new[]
                {
                    FindChild(composition, "Credits") as RectTransform,
                    FindChild(composition, "Command") as RectTransform,
                    FindChild(composition, "SettingsButton") as RectTransform,
                    FindChild(composition, "KeyStats") as RectTransform,
                    FindChild(composition, "IntelConfidence") as RectTransform,
                    FindChild(composition, "KnownThreats") as RectTransform,
                    FindChild(composition, "AriaStatus") as RectTransform,
                    FindChild(composition, "RecentActivity") as RectTransform
                },
                shouldExpandToCanvasWidth: true,
                targetsAnchoredToCenter: new[]
                {
                    FindChild(composition, "DistrictThreatMarker") as RectTransform
                },
                targetsExpandedAcrossWidth: new[]
                {
                    FindChild(composition, "ScreenTitlePanel") as RectTransform,
                    FindChild(composition, "DistrictVisual") as RectTransform,
                    FindChild(composition, "DistrictClip") as RectTransform,
                    FindChild(composition, "MapShade") as RectTransform,
                    FindChild(composition, "ActionBar") as RectTransform
                });
        }

        private static UIShellRouteButtonView BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 5f, 6f, 376f, 96f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            V3GradientGraphic backHit = logo.GetComponent<V3GradientGraphic>();
            backHit.raycastTarget = true;
            Button backButton = logo.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backHit;
            UIShellRouteButtonView backRoute = logo.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.Operations, false);

            RectTransform titlePanel = CreateTopLeft("ScreenTitlePanel", root, 387f, 6f, 631f, 96f);
            CreateGradientPanel(titlePanel, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("ScreenTitle", titlePanel, "DISTRICT DETAIL <color=#10B7E7>ACTIONS</color>", 37f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 30f, 7f, 585f, 80f);

            BuildResourceChip(root, "Credits", 1024f, 6f, 262f, 96f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "Command", 1292f, 6f, 270f, 96f, catalog.CommandIcon, "COMMAND", "8,430");
            Button settings = CreateGradientButton("SettingsButton", root, 1568f, 6f, 97f, 96f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetCentered(gear.rectTransform, 62f, 62f);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
            return backRoute;
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 16f, 17f, 61f, 61f);
            TMP_Text labelText = CreateText("Label", chip, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 88f, 7f, width - 96f, 34f);
            TMP_Text valueText = CreateText("Value", chip, value, 32f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 88f, 36f, width - 96f, 49f);
        }

        private static void BuildDistrictVisual(Transform root, out RawImage districtImage, out TMP_Text districtName, out TMP_Text threatLabel)
        {
            RectTransform panel = CreateTopLeft("DistrictVisual", root, 5f, 110f, 950f, 635f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            RectTransform clip = CreateTopLeft("DistrictClip", panel, 3f, 3f, 944f, 629f);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform mapRect = CreateRect("DistrictImage", clip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            districtImage = mapRect.gameObject.AddComponent<RawImage>();
            districtImage.texture = mapTexture;
            districtImage.raycastTarget = false;
            AddCover(mapRect, mapTexture.width / (float)mapTexture.height);
            CreateSolidTopLeft("MapShade", clip, 0f, 0f, 944f, 629f, new Color(0.02f, 0.02f, 0.015f, 0.25f));

            districtName = CreateText("DistrictName", panel, "OLD MARKET DISTRICT", 49f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(districtName.rectTransform, 20f, 8f, 700f, 63f);
            Image threatIcon = CreateImage("ThreatIcon", panel, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), Red, false);
            SetTopLeft(threatIcon.rectTransform, 21f, 70f, 38f, 38f);
            threatLabel = CreateText("ThreatLabel", panel, "HIGH THREAT", 24f, boldFont, TextAlignmentOptions.MidlineLeft, Red);
            SetTopLeft(threatLabel.rectTransform, 62f, 68f, 220f, 43f);

            RectTransform marker = CreateTopLeft("DistrictThreatMarker", panel, 422f, 154f, 136f, 166f);
            Image pinUnderlay = CreateImage("PinUnderlay", marker, RequireSprite(V3UiFoundationBuilder.OperationsMapPinUnderlayPath), new Color32(65, 8, 4, 255), false);
            Stretch(pinUnderlay.rectTransform);
            pinUnderlay.preserveAspect = true;
            Image pin = CreateImage("Pin", marker, RequireSprite(V3UiFoundationBuilder.OperationsMapPinIconPath), Red, false);
            SetTopLeft(pin.rectTransform, 7f, 5f, 122f, 150f);
            pin.preserveAspect = true;
            Image skull = CreateImage("Skull", marker, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), new Color32(56, 7, 4, 255), false);
            SetTopLeft(skull.rectTransform, 36f, 33f, 64f, 64f);

            BuildInsetMap(panel);
        }

        private static void BuildInsetMap(Transform parent)
        {
            RectTransform inset = CreateTopLeft("TacticalInset", parent, 11f, 355f, 300f, 268f);
            CreateGradientPanel(inset, DarkTop, DarkBottom, Border, 3f);
            RectTransform clip = CreateTopLeft("InsetClip", inset, 3f, 3f, 294f, 262f);
            clip.gameObject.AddComponent<RectMask2D>();
            RawImage map = CreateRect("InsetMap", clip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            map.texture = mapTexture;
            map.raycastTarget = false;
            map.color = new Color32(72, 88, 89, 255);
            AddCover(map.rectTransform, mapTexture.width / (float)mapTexture.height);
            BuildInsetZone(clip, Cyan, new[] { new Vector2(6, 34), new Vector2(135, 9), new Vector2(171, 68), new Vector2(94, 122), new Vector2(15, 104) });
            BuildInsetZone(clip, Amber, new[] { new Vector2(135, 9), new Vector2(292, 18), new Vector2(292, 95), new Vector2(171, 68) });
            BuildInsetZone(clip, Lime, new[] { new Vector2(6, 135), new Vector2(94, 122), new Vector2(164, 187), new Vector2(95, 257), new Vector2(5, 226) });
            BuildInsetZone(clip, Red, new[] { new Vector2(171, 68), new Vector2(292, 95), new Vector2(292, 258), new Vector2(164, 187) });
            Image pin = CreateImage("OldMarketPin", clip, RequireSprite(V3UiFoundationBuilder.OperationsMapPinIconPath), Red, false);
            SetTopLeft(pin.rectTransform, 190f, 145f, 48f, 60f);
            pin.preserveAspect = true;
            Image miniSkull = CreateImage("OldMarketSkull", clip, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), new Color32(58, 7, 4, 255), false);
            SetTopLeft(miniSkull.rectTransform, 203f, 158f, 22f, 22f);
        }

        private static void BuildInsetZone(Transform parent, Color color, Vector2[] points)
        {
            RectTransform zone = CreateTopLeft("Zone", parent, 0f, 0f, 294f, 262f);
            zone.gameObject.AddComponent<CanvasRenderer>();
            zone.gameObject.AddComponent<V3PolygonGraphic>().Configure(points, new Color(color.r, color.g, color.b, 0.20f));
            for (int i = 0; i < points.Length; i++)
                CreateLine("Edge" + i, parent, points[i], points[(i + 1) % points.Length], 2f, color);
        }

        private static void BuildKeyStats(Transform root)
        {
            RectTransform panel = CreateTopLeft("KeyStats", root, 962f, 110f, 298f, 385f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(panel, "KEY STATS", 16f, 11f, Cyan);
            string[] labels = { "Stability", "Civilian Trust", "Security", "Economic Output", "Population" };
            string[] values = { "58%", "64%", "32%", "72%", "25,430" };
            string[] icons =
            {
                V3UiFoundationBuilder.CampaignHoldIconPath,
                V3UiFoundationBuilder.MissionCivilianIconPath,
                V3UiFoundationBuilder.MissionEnemyIconPath,
                V3UiFoundationBuilder.OperationsIntelIconPath,
                V3UiFoundationBuilder.CampaignSquadIconPath
            };
            Color[] colors = { Cyan, Lime, Red, Amber, Cyan };
            float[] fills = { .58f, .64f, .32f, .72f, .58f };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 53f + i * 64f;
                Image icon = CreateImage("Icon" + i, panel, RequireSprite(icons[i]), colors[i], false);
                SetTopLeft(icon.rectTransform, 15f, y, 43f, 43f);
                TMP_Text label = CreateText("Label" + i, panel, labels[i], 17f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(label.rectTransform, 66f, y - 3f, 180f, 27f);
                CreateSolidTopLeft("Track" + i, panel, 66f, y + 29f, 151f, 12f, new Color32(5, 13, 16, 255));
                CreateSolidTopLeft("Fill" + i, panel, 67f, y + 30f, 149f * fills[i], 10f, colors[i]);
                TMP_Text value = CreateText("Value" + i, panel, values[i], 18f, mediumFont, TextAlignmentOptions.MidlineRight, theme.TextPrimary);
                SetTopLeft(value.rectTransform, 219f, y + 18f, 65f, 27f);
            }
        }

        private static Slider BuildIntelAndThreats(Transform root)
        {
            RectTransform intel = CreateTopLeft("IntelConfidence", root, 1267f, 110f, 398f, 105f);
            CreateGradientPanel(intel, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(intel, "INTEL CONFIDENCE", 15f, 9f, Cyan);
            RectTransform track = CreateTopLeft("Track", intel, 18f, 54f, 300f, 20f);
            Image trackImage = CreateSolidTopLeft("Background", track, 0f, 0f, 300f, 20f, new Color32(4, 13, 17, 255));
            RectTransform fill = CreateTopLeft("Fill", track, 3f, 3f, 229f, 14f);
            Image fillImage = CreateSolidTopLeft("FillGraphic", fill, 0f, 0f, 229f, 14f, Cyan);
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.targetGraphic = trackImage;
            slider.fillRect = fill;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = .78f;
            slider.interactable = false;
            TMP_Text value = CreateText("Value", intel, "78%", 26f, boldFont, TextAlignmentOptions.MidlineRight, Cyan);
            SetTopLeft(value.rectTransform, 319f, 42f, 64f, 42f);

            RectTransform threats = CreateTopLeft("KnownThreats", root, 1267f, 222f, 398f, 230f);
            CreateGradientPanel(threats, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(threats, "KNOWN THREATS", 15f, 9f, Cyan);
            string[] names = { "INFANTRY", "ARMORED", "AIR SUPPORT" };
            string[] ratings = { "HIGH", "HIGH", "MEDIUM" };
            string[] icons = { V3UiFoundationBuilder.CampaignSquadIconPath, V3UiFoundationBuilder.OperationsTankIconPath, V3UiFoundationBuilder.MissionAirIconPath };
            Color[] colors = { Red, Red, Amber };
            for (int i = 0; i < 3; i++)
            {
                RectTransform card = CreateTopLeft("ThreatCard" + i, threats, 10f + i * 128f, 51f, 120f, 169f);
                CreateGradientPanel(card, DarkTop, DarkBottom, Border, 2f);
                TMP_Text name = CreateText("Name", card, names[i], 15f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
                SetTopLeft(name.rectTransform, 5f, 8f, 110f, 27f);
                Image icon = CreateImage("Icon", card, RequireSprite(icons[i]), colors[i], false);
                SetTopLeft(icon.rectTransform, 20f, 39f, 80f, 80f);
                TMP_Text rating = CreateText("Rating", card, ratings[i], 20f, boldFont, TextAlignmentOptions.Center, colors[i]);
                SetTopLeft(rating.rectTransform, 5f, 130f, 110f, 31f);
            }
            return slider;
        }

        private static void BuildAriaStatus(Transform root, out Image portrait)
        {
            RectTransform panel = CreateTopLeft("AriaStatus", root, 762f, 502f, 498f, 243f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            RectTransform portraitClip = CreateTopLeft("AriaPortraitClip", panel, 8f, 8f, 236f, 232f);
            portraitClip.gameObject.AddComponent<RectMask2D>();
            portrait = CreateImage("AriaPortrait", portraitClip, ariaSprite, Color.white, false);
            Stretch(portrait.rectTransform);
            AddCover(portrait.rectTransform, ariaSprite.rect.width / ariaSprite.rect.height);
            TMP_Text title = CreateText("Title", panel, "ARIA STATUS", 27f, boldFont, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(title.rectTransform, 260f, 39f, 220f, 38f);
            CreateSolidTopLeft("Rule0", panel, 258f, 79f, 221f, 2f, Border);
            TMP_Text presence = CreateText("Presence", panel, "ENEMY PRESENCE  <color=#F13D21>HIGH</color>", 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(presence.rectTransform, 258f, 85f, 226f, 37f);
            CreateSolidTopLeft("Rule1", panel, 258f, 124f, 221f, 2f, Border);
            TMP_Text risk = CreateText("Risk", panel, "LOCAL CIVILIANS AT RISK", 18f, boldFont, TextAlignmentOptions.MidlineLeft, Amber);
            SetTopLeft(risk.rectTransform, 258f, 131f, 226f, 43f);
            CreateSolidTopLeft("Rule2", panel, 258f, 177f, 221f, 2f, Border);
        }

        private static void BuildRecentActivity(Transform root)
        {
            RectTransform panel = CreateTopLeft("RecentActivity", root, 1267f, 460f, 398f, 285f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            BuildPanelTitle(panel, "RECENT ACTIVITY", 15f, 8f, Cyan);
            string[] titles = { "Enemy Patrol Spotted", "Aid Convoy Arrived", "Industrial Output Increased" };
            string[] bodies = { "Multiple infantry units\nobserved moving through\nOld Market.", "Civilian supplies delivered\nto local safe zone.", "Workshops running at\nfull capacity." };
            string[] times = { "08:42", "06:15", "04:30" };
            string[] icons = { V3UiFoundationBuilder.MissionEnemyIconPath, V3UiFoundationBuilder.EquipmentHealthIconPath, V3UiFoundationBuilder.OperationsIntelIconPath };
            Color[] colors = { Red, Lime, Amber };
            for (int i = 0; i < 3; i++)
            {
                float y = 49f + i * 76f;
                Image icon = CreateImage("Icon" + i, panel, RequireSprite(icons[i]), colors[i], false);
                SetTopLeft(icon.rectTransform, 14f, y, 50f, 50f);
                TMP_Text title = CreateText("Title" + i, panel, titles[i], 16f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(title.rectTransform, 75f, y - 2f, 230f, 28f);
                TMP_Text time = CreateText("Time" + i, panel, times[i], 15f, mediumFont, TextAlignmentOptions.MidlineRight, colors[i]);
                SetTopLeft(time.rectTransform, 320f, y - 2f, 63f, 28f);
                TMP_Text body = CreateText("Body" + i, panel, bodies[i], 13f, mediumFont, TextAlignmentOptions.TopLeft, new Color32(188, 193, 191, 255));
                SetTopLeft(body.rectTransform, 75f, y + 24f, 285f, 52f);
                body.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static Button[] BuildActionBar(Transform root)
        {
            RectTransform bar = CreateTopLeft("ActionBar", root, 5f, 752f, 1660f, 181f);
            var actions = new Button[7];
            actions[0] = BuildActionCard(bar, "Patrol", 0f, 230f, "PATROL", "00:15:00", string.Empty, V3UiFoundationBuilder.CampaignSquadIconPath, new Color32(26, 100, 31, 255), new Color32(5, 45, 18, 255), Lime, false);
            actions[1] = BuildActionCard(bar, "DroneScan", 236f, 220f, "DRONE SCAN", "00:05:00", string.Empty, V3UiFoundationBuilder.OperationsDroneIconPath, new Color32(5, 91, 135, 255), new Color32(2, 35, 54, 255), Cyan, false);
            actions[2] = BuildActionCard(bar, "Aid", 462f, 220f, "AID", "00:20:00", "1,200", V3UiFoundationBuilder.OperationsAidIconPath, new Color32(48, 109, 35, 255), new Color32(12, 47, 18, 255), Lime, false);
            actions[3] = BuildActionCard(bar, "Raid", 688f, 220f, "RAID", "01:00:00", "2,500", V3UiFoundationBuilder.OperationsRaidIconPath, new Color32(139, 43, 15, 255), new Color32(58, 14, 6, 255), Orange, false);
            actions[4] = BuildActionCard(bar, "Repair", 914f, 230f, "REPAIR", "00:30:00", "1,800", V3UiFoundationBuilder.OperationsRepairIconPath, new Color32(130, 88, 4, 255), new Color32(54, 35, 1, 255), Amber, false);
            actions[5] = BuildActionCard(bar, "Evacuate", 1150f, 230f, "EVACUATE", "REQUIRES HQ LV. 16", string.Empty, V3UiFoundationBuilder.MissionCivilianIconPath, new Color32(25, 31, 33, 255), new Color32(8, 12, 13, 255), Border, true);
            actions[6] = BuildActionCard(bar, "BuildOutpost", 1386f, 274f, "BUILD OUTPOST", "REQUIRES HQ LV. 18", string.Empty, V3UiFoundationBuilder.CampaignBarracksIconPath, new Color32(25, 31, 33, 255), new Color32(8, 12, 13, 255), Border, true);

            HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return actions;
        }

        private static Button BuildActionCard(Transform parent, string name, float x, float width, string label, string timeOrRequirement, string cost, string iconPath, Color top, Color bottom, Color border, bool locked)
        {
            Button button = CreateGradientButton(name, parent, x, 0f, width, 181f, top, bottom, border, 3f);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = width;
            layout.minHeight = 181f;
            layout.preferredHeight = 181f;
            button.interactable = !locked;
            Color iconColor = locked ? new Color32(72, 78, 79, 255) : border;
            Image icon = CreateImage("Icon", button.transform, RequireSprite(iconPath), iconColor, false);
            SetTopLeft(icon.rectTransform, locked ? width * .5f - 55f : 22f, 23f, 76f, 76f);
            icon.preserveAspect = true;
            if (locked)
            {
                TMP_Text title = CreateText("Label", button.transform, label, 21f, boldFont, TextAlignmentOptions.Center, new Color32(92, 96, 96, 255));
                SetTopLeft(title.rectTransform, 8f, 93f, width - 16f, 34f);
                TMP_Text requirement = CreateText("Detail", button.transform, timeOrRequirement, 15f, mediumFont, TextAlignmentOptions.Center, new Color32(92, 96, 96, 255));
                SetTopLeft(requirement.rectTransform, 8f, 132f, width - 16f, 30f);
                Image lockIcon = CreateImage("Lock", button.transform, RequireSprite(V3UiFoundationBuilder.EquipmentLockIconPath), new Color32(150, 155, 154, 255), false);
                SetTopLeft(lockIcon.rectTransform, width * .5f + 4f, 42f, 48f, 48f);
            }
            else
            {
                float labelSize = label.Length > 9 ? 17f : 28f;
                TMP_Text title = CreateText("Label", button.transform, label, labelSize, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(title.rectTransform, 104f, 29f, width - 110f, 59f);
                Image timeIcon = CreateImage("TimeIcon", button.transform, RequireSprite(V3UiFoundationBuilder.OperationsTimeIconPath), theme.TextPrimary, false);
                SetTopLeft(timeIcon.rectTransform, cost.Length > 0 ? 13f : 35f, 135f, 21f, 21f);
                TMP_Text time = CreateText("Time", button.transform, timeOrRequirement, 14f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(time.rectTransform, cost.Length > 0 ? 38f : 62f, 127f, 87f, 37f);
                if (cost.Length > 0)
                {
                    Image costIcon = CreateImage("CostIcon", button.transform, catalog.CommandIcon, border, false);
                    SetTopLeft(costIcon.rectTransform, 127f, 135f, 22f, 22f);
                    TMP_Text costText = CreateText("Cost", button.transform, cost, 14f, mediumFont, TextAlignmentOptions.MidlineLeft, border);
                    SetTopLeft(costText.rectTransform, 151f, 127f, width - 155f, 37f);
                }
            }
            return button;
        }

        private static void BuildPanelTitle(Transform parent, string value, float x, float y, Color railColor)
        {
            CreateSolidTopLeft("TitleRail", parent, x, y + 4f, 4f, 22f, railColor);
            TMP_Text title = CreateText("Title", parent, value, 22f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, x + 12f, y, 270f, 36f);
        }

        private static void CreateLine(string name, Transform parent, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 delta = end - start;
            Image line = CreateSolidTopLeft(name, parent, 0f, 0f, delta.magnitude, thickness, color);
            RectTransform rect = line.rectTransform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void AssignToOpenMenuScene(GameObject prefab)
        {
            UIShellContentView content = UnityEngine.Object.FindAnyObjectByType<UIShellContentView>(FindObjectsInactive.Include);
            if (content == null || content.gameObject.scene.path != "Assets/Game/Scenes/Menu.unity")
            {
                Debug.LogWarning("[DistrictDetailActionsV3PrefabBuilder] Menu scene is not open; prefab built but shell assignment was skipped.");
                return;
            }
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("districtDetailContentPrefab");
            if (property == null)
                throw new MissingFieldException(nameof(UIShellContentView), "districtDetailContentPrefab");
            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
            EditorSceneManager.MarkSceneDirty(content.gameObject.scene);
            EditorSceneManager.SaveScene(content.gameObject.scene);
        }

        private static Button CreateGradientButton(string name, Transform parent, float x, float y, float width, float height, Color top, Color bottom, Color border, float borderWidth)
        {
            RectTransform rect = CreateTopLeft(name, parent, x, y, width, height);
            V3GradientGraphic graphic = CreateGradientPanel(rect, top, bottom, border, borderWidth);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static V3GradientGraphic CreateGradientPanel(RectTransform rect, Color top, Color bottom, Color border, float borderWidth)
        {
            V3GradientGraphic graphic = rect.gameObject.AddComponent<V3GradientGraphic>();
            graphic.Configure(top, bottom, border, borderWidth);
            return graphic;
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

        private static Image CreateSolidTopLeft(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            Image image = CreateImage(name, parent, null, color, false);
            SetTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(200f, 60f), Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void AddCover(RectTransform rect, float ratio)
        {
            AspectRatioFitter fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ratio;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new MissingReferenceException($"Missing District Detail sprite: {path}");
            return sprite;
        }

        private static void ConfigureTexture(string path, int maxSize, bool alpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing District Detail texture: {path}");
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite && alpha)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }
            if (alpha && !importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
            return null;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(nameof(DistrictDetailActionsScreenView), propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(nameof(DistrictDetailActionsScreenView), propertyName);
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
