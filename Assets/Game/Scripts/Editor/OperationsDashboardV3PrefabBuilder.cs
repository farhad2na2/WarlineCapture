using System;
using System.IO;
using System.Linq;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class OperationsDashboardV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN11_OperationsDashboardContent.prefab";
        private const string MapPath = "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png";
        private const string AriaPath = "Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png";
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
        private static readonly Color Purple = new Color32(118, 79, 184, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Texture2D mapTexture;
        private static Sprite ariaSprite;

        [MenuItem("Game/UI/V3/Rebuild Operations Dashboard Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform root = CreateRect("SCN11_OperationsDashboardContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateGradientPanel(root, new Color32(11, 18, 21, 255), new Color32(1, 5, 7, 255), Color.clear, 0f);
            RectTransform composition = CreateTopLeft("OperationsDashboardComposition", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center);
            OperationsDashboardScreenView screen = composition.gameObject.AddComponent<OperationsDashboardScreenView>();

            BuildHeader(composition);
            RectTransform readinessRail = BuildReadinessRail(composition, out RectTransform[] readinessCards);
            RectTransform districtMap = BuildDistrictMap(composition, out RawImage districtMapImage, out Button[] districtButtons);
            RectTransform dailyBriefing = BuildAriaBriefing(composition);
            RectTransform activeWarnings = BuildWarnings(composition, out Button[] warningButtons);
            RectTransform commandBar = BuildFooter(composition, out Button intel, out Button patrol, out Button raid, out Button repair, out Button armory, out Button endDay);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "readinessRail", readinessRail);
            SetReference(serialized, "districtMap", districtMap);
            SetReference(serialized, "dailyBriefing", dailyBriefing);
            SetReference(serialized, "activeWarnings", activeWarnings);
            SetReference(serialized, "commandBar", commandBar);
            SetArray(serialized, "readinessCards", readinessCards);
            SetArray(serialized, "districtButtons", districtButtons);
            SetArray(serialized, "warningButtons", warningButtons);
            SetReference(serialized, "intelReportButton", intel);
            SetReference(serialized, "blackMarketButton", patrol);
            SetReference(serialized, "armoryButton", armory);
            SetReference(serialized, "commandLogButton", raid);
            SetReference(serialized, "endDayButton", endDay);
            SetReference(serialized, "districtMapImage", districtMapImage);
            SetReference(serialized, "screenTitle", FindText(composition, "ScreenTitle"));
            SetReference(serialized, "dayLabel", FindText(composition, "DayLabel"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Operations V3 prefab: {PrefabPath}");
            AssignToOpenMenuScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[OperationsDashboardV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 map=aspect-preserved aria=aspect-preserved atlas=operations-shared");
        }

        [MenuItem("Game/UI/V3/Validate Operations Dashboard Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Operations V3 prefab: {PrefabPath}");
            string[] required = { "ReadinessRail", "DistrictMap", "DailyBriefing", "ActiveWarnings", "CommandBar", "EndDayButton" };
            for (int i = 0; i < required.Length; i++)
                if (FindChild(prefab.transform, required[i]) == null)
                    throw new MissingReferenceException($"Operations V3 is missing {required[i]}.");
            if (prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length < 20)
                throw new InvalidOperationException("Operations V3 does not contain enough procedural gradient surfaces.");
            if (prefab.GetComponentsInChildren<V3PolygonGraphic>(true).Length != 5)
                throw new InvalidOperationException("Operations V3 requires exactly five district overlays.");
            if (prefab.GetComponentsInChildren<UIShellRouteButtonView>(true).Count(route => route.Route == UIRoute.DistrictDetail) != 5)
                throw new InvalidOperationException("Operations V3 requires five district-detail route hotspots.");
            Image aria = FindChild(prefab.transform, "AriaPortrait")?.GetComponent<Image>();
            RawImage map = FindChild(prefab.transform, "DistrictMapImage")?.GetComponent<RawImage>();
            if (aria == null || aria.GetComponent<AspectRatioFitter>() == null || map == null || map.GetComponent<AspectRatioFitter>() == null)
                throw new InvalidOperationException("Operations V3 must preserve both ARIA and district-map aspect ratios.");
            Debug.Log($"[OperationsDashboardV3PrefabBuilder] validation=Passed gradients={prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length} polygons=5 images={prefab.GetComponentsInChildren<Image>(true).Length}");
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
            if (boldFont == null || mediumFont == null || mapTexture == null)
                throw new MissingReferenceException("Operations V3 shared art or fonts are missing.");
        }

        private static void BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 5f, 6f, 391f, 102f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            RectTransform titlePanel = CreateTopLeft("ScreenTitlePanel", root, 401f, 6f, 560f, 102f);
            CreateGradientPanel(titlePanel, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("ScreenTitle", titlePanel, "OPERATIONS", 50f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            Stretch(title.rectTransform, 18f);

            BuildResourceChip(root, "Credits", 966f, 6f, 278f, 102f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "Command", 1249f, 6f, 285f, 102f, catalog.CommandIcon, "COMMAND", "8,430");
            Button settings = CreateGradientButton("SettingsButton", root, 1539f, 6f, 126f, 102f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetCentered(gear.rectTransform, 66f, 66f);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 18f, 20f, 65f, 65f);
            TMP_Text labelText = CreateText("Label", chip, label, 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 96f, 8f, width - 105f, 38f);
            TMP_Text valueText = CreateText("Value", chip, value, 34f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 96f, 40f, width - 105f, 52f);
        }

        private static RectTransform BuildReadinessRail(Transform root, out RectTransform[] cards)
        {
            RectTransform rail = CreateTopLeft("ReadinessRail", root, 5f, 115f, 350f, 680f);
            string[] labels = { "REGION STABILITY", "CIVILIAN TRUST", "THREAT LEVEL", "HEAT LEVEL", "FORCE READINESS" };
            string[] values = { "68%", "62%", "72%", "48%", "81%" };
            string[] icons =
            {
                V3UiFoundationBuilder.CampaignHoldIconPath,
                V3UiFoundationBuilder.MissionCivilianIconPath,
                V3UiFoundationBuilder.MissionEnemyIconPath,
                V3UiFoundationBuilder.OperationsHeatIconPath,
                V3UiFoundationBuilder.CommanderRankIconPath
            };
            Color[] colors = { Cyan, Lime, Red, Amber, Cyan };
            int[] filled = { 4, 4, 4, 3, 4 };
            cards = new RectTransform[5];
            for (int i = 0; i < 5; i++)
            {
                cards[i] = CreateTopLeft(labels[i].Replace(" ", string.Empty), rail, 0f, i * 137.5f, 350f, 130f);
                CreateGradientPanel(cards[i], DarkTop, DarkBottom, colors[i], 2f);
                CreateSolidTopLeft("IconDivider", cards[i], 98f, 2f, 2f, 126f, Border);
                Image icon = CreateImage("Icon", cards[i], RequireSprite(icons[i]), colors[i], false);
                SetTopLeft(icon.rectTransform, 18f, 25f, 64f, 64f);
                icon.preserveAspect = true;
                TMP_Text label = CreateText("Label", cards[i], labels[i], 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
                SetTopLeft(label.rectTransform, 114f, 12f, 220f, 34f);
                TMP_Text value = CreateText("Value", cards[i], values[i], 30f, boldFont, TextAlignmentOptions.MidlineLeft, colors[i]);
                SetTopLeft(value.rectTransform, 114f, 42f, 130f, 44f);
                for (int segment = 0; segment < 6; segment++)
                    CreateSolidTopLeft("Segment" + segment, cards[i], 114f + segment * 37f, 94f, 33f, 15f, segment < filled[i] ? colors[i] : new Color32(39, 45, 45, 255));
            }
            return rail;
        }

        private static RectTransform BuildDistrictMap(Transform root, out RawImage mapImage, out Button[] districtButtons)
        {
            RectTransform panel = CreateTopLeft("DistrictMap", root, 363f, 115f, 879f, 680f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            RectTransform clip = CreateTopLeft("MapClip", panel, 3f, 3f, 873f, 674f);
            clip.gameObject.AddComponent<RectMask2D>();
            RectTransform mapRect = CreateRect("DistrictMapImage", clip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapImage = mapRect.gameObject.AddComponent<RawImage>();
            mapImage.texture = mapTexture;
            mapImage.raycastTarget = false;
            AddCover(mapRect, mapTexture.width / (float)mapTexture.height);
            CreateSolidTopLeft("MapShade", clip, 0f, 0f, 873f, 674f, new Color(0.03f, 0.05f, 0.04f, 0.18f));

            BuildDistrictZone(clip, "Northgate", Cyan, new[] { new Vector2(5, 80), new Vector2(190, 12), new Vector2(380, 28), new Vector2(500, 180), new Vector2(350, 270), new Vector2(180, 290), new Vector2(20, 250) }, "NORTHGATE", 90f, 95f, V3UiFoundationBuilder.CampaignHoldIconPath);
            BuildDistrictZone(clip, "Eastridge", Amber, new[] { new Vector2(380, 28), new Vector2(870, 18), new Vector2(870, 225), new Vector2(710, 250), new Vector2(620, 210), new Vector2(500, 180) }, "EASTRIDGE", 570f, 90f, V3UiFoundationBuilder.OperationsWarningIconPath);
            BuildDistrictZone(clip, "OldMarket", Amber, new[] { new Vector2(180, 290), new Vector2(350, 270), new Vector2(500, 180), new Vector2(620, 210), new Vector2(710, 250), new Vector2(705, 400), new Vector2(610, 470), new Vector2(470, 530), new Vector2(350, 480), new Vector2(280, 390) }, "OLD MARKET", 335f, 290f, V3UiFoundationBuilder.MissionStarIconPath);
            BuildDistrictZone(clip, "ForwardPost", Lime, new[] { new Vector2(5, 360), new Vector2(180, 290), new Vector2(280, 390), new Vector2(350, 480), new Vector2(470, 530), new Vector2(400, 630), new Vector2(250, 670), new Vector2(5, 610) }, "FORWARD POST", 60f, 480f, V3UiFoundationBuilder.CampaignBarracksIconPath);
            BuildDistrictZone(clip, "SouthQuarter", Red, new[] { new Vector2(710, 250), new Vector2(870, 225), new Vector2(870, 670), new Vector2(515, 670), new Vector2(470, 530), new Vector2(610, 470), new Vector2(705, 400) }, "SOUTH QUARTER", 575f, 480f, V3UiFoundationBuilder.MissionEnemyIconPath);

            districtButtons = panel.GetComponentsInChildren<Button>(true);
            return panel;
        }

        private static void BuildDistrictZone(Transform parent, string name, Color color, Vector2[] points, string label, float x, float y, string iconPath)
        {
            RectTransform zone = CreateTopLeft(name + "Zone", parent, 0f, 0f, 873f, 674f);
            if (zone.GetComponent<CanvasRenderer>() == null)
                zone.gameObject.AddComponent<CanvasRenderer>();
            V3PolygonGraphic fill = zone.gameObject.AddComponent<V3PolygonGraphic>();
            fill.Configure(points, new Color(color.r, color.g, color.b, 0.20f));
            for (int i = 0; i < points.Length; i++)
                CreateLine(name + "Edge" + i, parent, points[i], points[(i + 1) % points.Length], 3f, new Color(color.r, color.g, color.b, 0.95f));
            RectTransform marker = CreateTopLeft(name + "Marker", parent, x, y, 280f, 78f);
            Image hitTarget = marker.gameObject.AddComponent<Image>();
            hitTarget.color = Color.clear;
            hitTarget.raycastTarget = true;
            Button button = marker.gameObject.AddComponent<Button>();
            button.targetGraphic = hitTarget;
            marker.gameObject.AddComponent<UIShellRouteButtonView>().Configure(
                UiShellRouteIntent.OpenMenuRoute,
                UIRoute.DistrictDetail,
                true);
            Image icon = CreateImage("Icon", marker, RequireSprite(iconPath), color, false);
            SetTopLeft(icon.rectTransform, 0f, 2f, 50f, 50f);
            TMP_Text text = CreateText("Label", marker, label, 22f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(text.rectTransform, 58f, 6f, 218f, 45f);
        }

        private static RectTransform BuildAriaBriefing(Transform root)
        {
            RectTransform panel = CreateTopLeft("DailyBriefing", root, 1250f, 115f, 415f, 362f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("Theater", panel, "SAHRIN THEATER", 29f, boldFont, TextAlignmentOptions.MidlineLeft, Cyan);
            SetTopLeft(title.rectTransform, 18f, 10f, 270f, 42f);
            TMP_Text day = CreateText("DayLabel", panel, "DAY 12", 24f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(day.rectTransform, 18f, 50f, 170f, 36f);
            TMP_Text time = CreateText("Time", panel, "09:00", 27f, boldFont, TextAlignmentOptions.MidlineRight, Cyan);
            SetTopLeft(time.rectTransform, 290f, 48f, 105f, 40f);
            CreateSolidTopLeft("Rule", panel, 18f, 92f, 379f, 2f, Border);

            RectTransform portraitClip = CreateTopLeft("AriaPortraitClip", panel, 25f, 101f, 210f, 258f);
            portraitClip.gameObject.AddComponent<RectMask2D>();
            Image portrait = CreateImage("AriaPortrait", portraitClip, ariaSprite, Color.white, false);
            Stretch(portrait.rectTransform);
            AddCover(portrait.rectTransform, ariaSprite.rect.width / ariaSprite.rect.height);
            TMP_Text briefing = CreateText("Briefing", panel, "Enemy pressure\nis increasing\nnear South\nQuarter and\nForward Post.\nMaintain control\nand protect\ncivilians.", 17f, mediumFont, TextAlignmentOptions.TopLeft, theme.TextPrimary);
            SetTopLeft(briefing.rectTransform, 247f, 111f, 150f, 235f);
            briefing.textWrappingMode = TextWrappingModes.Normal;
            briefing.overflowMode = TextOverflowModes.Truncate;
            return panel;
        }

        private static RectTransform BuildWarnings(Transform root, out Button[] warningButtons)
        {
            RectTransform panel = CreateTopLeft("ActiveWarnings", root, 1250f, 484f, 415f, 311f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            TMP_Text title = CreateText("Title", panel, "ACTIVE WARNINGS", 25f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 19f, 8f, 370f, 38f);
            string[] labels = { "HOSTILE CELL ACTIVE", "SUPPLY ROUTE BLOCKED", "CIVILIANS AT RISK" };
            string[] icons = { V3UiFoundationBuilder.MissionEnemyIconPath, V3UiFoundationBuilder.OperationsWarningIconPath, V3UiFoundationBuilder.MissionCivilianIconPath };
            Color[] colors = { Red, Amber, Amber };
            warningButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                warningButtons[i] = CreateGradientButton("WarningRow" + i, panel, 12f, 54f + i * 72f, 391f, 63f, DarkTop, DarkBottom, colors[i], 2f);
                warningButtons[i].interactable = false;
                Image icon = CreateImage("Icon", warningButtons[i].transform, RequireSprite(icons[i]), colors[i], false);
                SetTopLeft(icon.rectTransform, 14f, 10f, 44f, 44f);
                CreateSolidTopLeft("Divider", warningButtons[i].transform, 72f, 2f, 2f, 59f, colors[i]);
                TMP_Text label = CreateText("Label", warningButtons[i].transform, labels[i], 22f, boldFont, TextAlignmentOptions.MidlineLeft, colors[i]);
                SetTopLeft(label.rectTransform, 92f, 6f, 285f, 49f);
            }
            return panel;
        }

        private static RectTransform BuildFooter(Transform root, out Button intel, out Button patrol, out Button raid, out Button repair, out Button armory, out Button endDay)
        {
            RectTransform bar = CreateTopLeft("CommandBar", root, 5f, 805f, 1660f, 118f);
            intel = BuildFooterButton(bar, "IntelReport", 0f, 265f, "INTEL REPORT", V3UiFoundationBuilder.OperationsIntelIconPath, new Color32(6, 96, 142, 255), new Color32(1, 35, 55, 255), Cyan, Color.white, Color.white);
            patrol = BuildFooterButton(bar, "Patrol", 271f, 231f, "PATROL", V3UiFoundationBuilder.OperationsPatrolIconPath, new Color32(55, 112, 34, 255), new Color32(17, 48, 18, 255), Lime, Color.white, Lime);
            raid = BuildFooterButton(bar, "Raid", 508f, 244f, "RAID", V3UiFoundationBuilder.OperationsRaidIconPath, new Color32(143, 43, 15, 255), new Color32(66, 17, 7, 255), Orange, Color.white, Orange);
            repair = BuildFooterButton(bar, "Repair", 758f, 226f, "REPAIR", V3UiFoundationBuilder.OperationsRepairIconPath, new Color32(123, 84, 4, 255), new Color32(54, 36, 1, 255), Amber, Color.white, Amber);
            armory = BuildFooterButton(bar, "Armory", 990f, 245f, "ARMORY", V3UiFoundationBuilder.OperationsArmoryIconPath, new Color32(62, 45, 101, 255), new Color32(27, 20, 48, 255), Purple, Color.white, new Color32(235, 230, 255, 255));
            endDay = BuildFooterButton(bar, "EndDayButton", 1241f, 419f, "END DAY", V3UiFoundationBuilder.CampaignLaunchIconPath, new Color32(167, 112, 5, 255), new Color32(92, 57, 1, 255), Amber, new Color32(255, 238, 174, 255), Amber);
            return bar;
        }

        private static Button BuildFooterButton(Transform parent, string name, float x, float width, string label, string iconPath, Color top, Color bottom, Color border, Color textColor, Color iconColor)
        {
            Button button = CreateGradientButton(name, parent, x, 0f, width, 118f, top, bottom, border, 3f);
            Image icon = CreateImage("Icon", button.transform, RequireSprite(iconPath), iconColor, false);
            SetTopLeft(icon.rectTransform, 20f, 25f, 62f, 62f);
            icon.preserveAspect = true;
            float fontSize = name == "EndDayButton" ? 38f : name == "IntelReport" ? 25f : 30f;
            TMP_Text text = CreateText("Label", button.transform, label, fontSize, boldFont, TextAlignmentOptions.Center, textColor);
            SetTopLeft(text.rectTransform, 78f, 9f, width - 88f, 94f);
            return button;
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
                Debug.LogWarning("[OperationsDashboardV3PrefabBuilder] Menu scene is not open; prefab built but shell assignment was skipped.");
                return;
            }
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("operationsContentPrefab");
            if (property == null)
                throw new MissingFieldException(nameof(UIShellContentView), "operationsContentPrefab");
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
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void AddCover(RectTransform rect, float aspect)
        {
            AspectRatioFitter fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = aspect;
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

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(serialized.targetObject.GetType().Name, propertyName);
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void ConfigureTexture(string path, int maxSize, bool sprite)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Operations V3 texture: {path}");
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing Operations V3 sprite: {path}");
            return sprite;
        }

        private static Transform FindChild(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name)
                    return all[i];
            return null;
        }

        private static TMP_Text FindText(Transform root, string name) => FindChild(root, name)?.GetComponent<TMP_Text>();
    }
}
