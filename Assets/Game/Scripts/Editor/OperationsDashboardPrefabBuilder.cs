#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class OperationsDashboardPrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN11_OperationsDashboardContent.prefab";
        private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MapPath = "Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_sahrin_district_map_v01.png";
        private const string PanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_01_popup_outer_frame.png";
        private const string DetailPanelSpritePath = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_chrome_03_detail_panel_frame.png";
        private const string SelectedSpritePath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png";
        private const string DefaultCardSpritePath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_default_amber.png";
        private const string SecondarySpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_15_secondary_dark_cta_frame.png";
        private const string GoldSpritePath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_14_primary_gold_cta_frame.png";
        private const string BackIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png";
        private const string ShieldIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_hold_shield.png";
        private const string CivilianIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_civilian_group.png";
        private const string ThreatIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_attack_crosshair.png";
        private const string IntelIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_scan_radar.png";
        private const string WarningIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_warning_triangle.png";
        private const string StarIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_objective_star.png";
        private const string HistoryIconPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png";
        private const string ArmoryIconPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_armory_crossed_weapons.png";
        private const string OperationsIconPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_operations_star_icon.png";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";

        private static readonly Color PanelTint = new(0.018f, 0.024f, 0.022f, 0.995f);
        private static readonly Color RowTint = new(0.035f, 0.043f, 0.039f, 0.995f);
        private static readonly Color SelectedTint = new(0.29f, 0.33f, 0.045f, 0.98f);
        private static readonly Color Gold = new(0.94f, 0.68f, 0.16f, 1f);
        private static readonly Color Olive = new(0.67f, 0.76f, 0.18f, 1f);
        private static readonly Color Cyan = new(0.20f, 0.72f, 0.88f, 1f);
        private static readonly Color Danger = new(0.92f, 0.27f, 0.17f, 1f);
        private static readonly Color Text = new(0.93f, 0.90f, 0.80f, 1f);
        private static readonly Color Muted = new(0.62f, 0.61f, 0.53f, 1f);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;

        [MenuItem("Game/UI/Build SCN-11 Operations Dashboard")]
        public static void Build()
        {
            LoadStyleAssets();
            GameObject root = BuildPrefabRoot();
            EnsureFolder("Assets/Game/Prefabs/UI/Shell/Content");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Operations prefab at {PrefabPath}.");

            RouteMainMenuOperationsCard();
            AssignMenuScenePrefab(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[OperationsDashboardPrefabBuilder] result=Passed prefab={PrefabPath}");
        }

        [MenuItem("Game/UI/Capture SCN-11 Operations Dashboard")]
        public static void CaptureOperations()
        {
            string path = ResolveCapturePath();
            int width = ResolvePositiveEnvironmentInt("WARLINE_OPERATIONS_CAPTURE_WIDTH", 1920);
            int height = ResolvePositiveEnvironmentInt("WARLINE_OPERATIONS_CAPTURE_HEIGHT", 1080);
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = ResolveComponentInScene<MenuBootstrapView>(scene);
            if (bootstrap == null || bootstrap.ContentSystem == null || bootstrap.UiCamera == null || bootstrap.UiCanvas == null)
                throw new InvalidOperationException("Menu scene is missing its configured Canvas bootstrap references.");

            bootstrap.ApplyRuntimeUiMode();
            bootstrap.ContentSystem.PrepareForCommandSequence(new[]
            {
                new UiShellPresentationCommandModel(
                    UiShellCommandKind.EnterMenu,
                    default,
                    UIRoute.MainMenu,
                    UiShellMode.MainMenu,
                    1)
            });
            bootstrap.ContentSystem.InstallMenuRouteBody(UIRoute.Operations);
            GameObject fpsPanel = GameObject.Find("Panel_FPS");
            if (fpsPanel != null)
                fpsPanel.SetActive(false);
            Canvas.ForceUpdateCanvases();
            RenderCameraToPng(bootstrap.UiCamera, path, width, height);
            Debug.Log($"[OperationsDashboardCapture] result=Passed size={width}x{height} path={path}");
        }

        private static GameObject BuildPrefabRoot()
        {
            GameObject root = CreateRect("SCN11_OperationsDashboardContent", null, 0f, 0f, 4800f, 2160f);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            OperationsDashboardScreenView screen = root.AddComponent<OperationsDashboardScreenView>();

            Button backButton = CreateButton("BackButton", root.transform, 90f, 300f, 540f, 165f, "BACK", SecondarySpritePath, 70f, Text, out TMP_Text backLabel);
            UIShellRouteButtonView backRoute = backButton.gameObject.AddComponent<UIShellRouteButtonView>();
            backRoute.Configure(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
            CreateIcon("BackIcon", backButton.transform, BackIconPath, 40f, 40f, 82f, 82f);
            SetTextRect(backLabel.rectTransform, 135f, 0f, 350f, 165f);

            TMP_Text screenTitle = CreateText("ScreenTitle", root.transform, 690f, 292f, 2100f, 175f, "OPERATIONS DASHBOARD", 118f, Text, TextAlignmentOptions.MidlineLeft);
            TMP_Text dayLabel = CreateText("DayLabel", root.transform, 3260f, 326f, 1460f, 110f, "SAHRIN THEATER   |   DAY 12   /   09:00", 46f, Text, TextAlignmentOptions.MidlineRight);

            RectTransform readinessRail = BuildReadinessRail(root.transform, out RectTransform[] readinessCards);
            RectTransform districtMap = BuildDistrictMap(root.transform, out RawImage mapImage, out Button[] districtButtons);
            RectTransform dailyBriefing = BuildDailyBriefing(root.transform);
            RectTransform activeWarnings = BuildActiveWarnings(root.transform, out Button[] warningButtons);
            RectTransform commandBar = BuildCommandBar(
                root.transform,
                out Button intelReport,
                out Button blackMarket,
                out Button armory,
                out Button commandLog,
                out Button endDay);

            SerializedObject serialized = new(screen);
            SetReference(serialized, "backRouteButton", backRoute);
            SetReference(serialized, "readinessRail", readinessRail);
            SetReference(serialized, "districtMap", districtMap);
            SetReference(serialized, "dailyBriefing", dailyBriefing);
            SetReference(serialized, "activeWarnings", activeWarnings);
            SetReference(serialized, "commandBar", commandBar);
            SetArray(serialized, "readinessCards", readinessCards);
            SetArray(serialized, "districtButtons", districtButtons);
            SetArray(serialized, "warningButtons", warningButtons);
            SetReference(serialized, "intelReportButton", intelReport);
            SetReference(serialized, "blackMarketButton", blackMarket);
            SetReference(serialized, "armoryButton", armory);
            SetReference(serialized, "commandLogButton", commandLog);
            SetReference(serialized, "endDayButton", endDay);
            SetReference(serialized, "districtMapImage", mapImage);
            SetReference(serialized, "screenTitle", screenTitle);
            SetReference(serialized, "dayLabel", dayLabel);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static RectTransform BuildReadinessRail(Transform root, out RectTransform[] cards)
        {
            const float panelHeight = 1580f;
            Transform panel = CreatePanel("ReadinessRail", root, 80f, 500f, 1040f, panelHeight);
            CreateText("RailTitle", panel, 52f, 28f, 920f, 90f, "OPERATION READINESS", 58f, Text, TextAlignmentOptions.MidlineLeft);

            string[] labels = { "REGION STABILITY", "CIVILIAN TRUST", "THREAT LEVEL", "HEAT LEVEL", "FORCE READINESS" };
            string[] values = { "68%", "62%", "72%", "48%", "81%" };
            string[] states = { "STABLE", "STABLE", "HIGH", "ELEVATED", "READY" };
            string[] icons = { ShieldIconPath, CivilianIconPath, ThreatIconPath, WarningIconPath, OperationsIconPath };
            Color[] colors = { Cyan, Olive, Danger, Gold, Cyan };
            int[] segments = { 4, 4, 5, 3, 5 };
            cards = new RectTransform[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 126f + i * 274f;
                cards[i] = BuildReadinessRow(panel, y, labels[i], values[i], states[i], icons[i], colors[i], segments[i]);
            }
            SetVerticalStretch(panel as RectTransform, 500f, 80f);
            NormalizeVerticalChildren(panel, panelHeight, "PanelFill");
            return panel as RectTransform;
        }

        private static RectTransform BuildReadinessRow(
            Transform parent,
            float y,
            string label,
            string value,
            string state,
            string iconPath,
            Color color,
            int filledSegments)
        {
            GameObject row = CreateRect(label.Replace(" ", string.Empty), parent, 26f, y, 988f, 252f);
            Image backing = row.AddComponent<Image>();
            backing.color = RowTint;
            backing.raycastTarget = false;
            CreateIcon("Icon", row.transform, iconPath, 34f, 55f, 128f, 128f, color);
            CreateText("Label", row.transform, 190f, 32f, 650f, 60f, label, 38f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("Value", row.transform, 190f, 89f, 290f, 76f, value, 62f, color, TextAlignmentOptions.MidlineLeft);
            CreateText("State", row.transform, 680f, 99f, 240f, 54f, state, 32f, color, TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < 6; i++)
            {
                Color segmentColor = i < filledSegments ? color : new Color(0.17f, 0.18f, 0.16f, 0.94f);
                CreateSolid($"Segment{i + 1}", row.transform, 190f + i * 112f, 188f, 92f, 20f, segmentColor);
            }
            CreateSolid("Divider", row.transform, 0f, 250f, 988f, 2f, new Color(0.42f, 0.40f, 0.24f, 0.45f));
            return row.GetComponent<RectTransform>();
        }

        private static RectTransform BuildDistrictMap(Transform root, out RawImage mapImage, out Button[] districtButtons)
        {
            const float panelHeight = 1320f;
            const float mapWidth = 1936f;
            const float mapHeight = 1080f;
            Transform panel = CreatePanel("DistrictMap", root, 1160f, 500f, 2020f, panelHeight);
            CreateText("MapTitle", panel, 180f, 25f, 1660f, 92f, "SAHRIN DISTRICTS", 62f, Text, TextAlignmentOptions.Center);
            CreateIcon("TheaterBadge", panel, OperationsIconPath, 1840f, 26f, 86f, 86f, Gold);

            mapImage = CreateCroppedPreview("DistrictMapImage", panel, 42f, 126f, mapWidth, mapHeight, MapPath);
            Image mapTint = CreateSolid("MapTint", mapImage.transform, 0f, 0f, mapWidth, mapHeight, new Color(0.01f, 0.035f, 0.028f, 0.14f));
            SetFullStretchMargins(mapTint.rectTransform, 0f);

            Vector2[] northgatePoints =
            {
                new Vector2(90f, 80f), new Vector2(690f, 55f), new Vector2(875f, 245f),
                new Vector2(760f, 505f), new Vector2(230f, 490f)
            };
            Vector2[] eastridgePoints =
            {
                new Vector2(700f, 55f), new Vector2(1810f, 80f), new Vector2(1860f, 390f),
                new Vector2(1400f, 555f), new Vector2(865f, 250f)
            };
            Vector2[] downtownPoints =
            {
                new Vector2(650f, 280f), new Vector2(1110f, 250f), new Vector2(1450f, 540f),
                new Vector2(1240f, 885f), new Vector2(790f, 890f), new Vector2(510f, 600f)
            };
            Vector2[] riverfrontPoints =
            {
                new Vector2(90f, 440f), new Vector2(650f, 425f), new Vector2(820f, 880f),
                new Vector2(625f, 1050f), new Vector2(85f, 1020f)
            };
            Vector2[] southDocksPoints =
            {
                new Vector2(1390f, 540f), new Vector2(1860f, 390f), new Vector2(1880f, 1040f),
                new Vector2(1180f, 1050f), new Vector2(1000f, 875f)
            };

            CreateDistrictOutline(mapImage.transform, "NorthgateZone", Cyan, 10f, northgatePoints);
            CreateDistrictOutline(mapImage.transform, "EastridgeZone", Gold, 10f, eastridgePoints);
            CreateDistrictOutline(mapImage.transform, "DowntownZone", Gold, 15f, downtownPoints);
            CreateDistrictOutline(mapImage.transform, "RiverfrontZone", Olive, 10f, riverfrontPoints);
            CreateDistrictOutline(mapImage.transform, "SouthDocksZone", Danger, 10f, southDocksPoints);

            districtButtons = new[]
            {
                CreateDistrictMarker(mapImage.transform, "Northgate", 260f, 155f, "NORTHGATE", "78%", Cyan, ShieldIconPath),
                CreateDistrictMarker(mapImage.transform, "Eastridge", 1220f, 175f, "EASTRIDGE", "45%", Gold, WarningIconPath),
                CreateDistrictMarker(mapImage.transform, "Downtown", 765f, 445f, "DOWNTOWN", "62%", Gold, StarIconPath),
                CreateDistrictMarker(mapImage.transform, "Riverfront", 235f, 695f, "RIVERFRONT", "71%", Olive, ShieldIconPath),
                CreateDistrictMarker(mapImage.transform, "SouthDocks", 1220f, 735f, "SOUTH DOCKS", "39%", Danger, WarningIconPath)
            };

            GameObject legend = CreateRect("MapLegend", panel, 72f, 1222f, 1876f, 72f);
            CreateLegendItem(legend.transform, 0f, 410f, "STABLE", ShieldIconPath, Cyan);
            CreateLegendItem(legend.transform, 430f, 410f, "SECURE", ShieldIconPath, Olive);
            CreateLegendItem(legend.transform, 860f, 440f, "CONTESTED", StarIconPath, Gold);
            CreateLegendItem(legend.transform, 1320f, 500f, "CRITICAL", WarningIconPath, Danger);

            SetVerticalStretch(panel as RectTransform, 500f, 340f);
            NormalizeVerticalChildren(panel, panelHeight, "PanelFill");
            return panel as RectTransform;
        }

        private static Button CreateDistrictMarker(
            Transform parent,
            string name,
            float x,
            float y,
            string district,
            string value,
            Color color,
            string iconPath)
        {
            Image hitArea = CreateSolid(name, parent, x, y, 500f, 170f, Color.clear);
            hitArea.raycastTarget = true;
            Button button = hitArea.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.interactable = false;
            CreateIcon("Icon", button.transform, iconPath, 20f, 25f, 106f, 106f, color);
            CreateText("District", button.transform, 142f, 22f, 340f, 64f, district, 40f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("Value", button.transform, 142f, 87f, 340f, 54f, value, 40f, color, TextAlignmentOptions.MidlineLeft);
            return button;
        }

        private static RectTransform BuildDailyBriefing(Transform root)
        {
            Transform panel = CreateDetailPanel("DailyBriefing", root, 3220f, 500f, 1500f, 480f);
            CreateText("Title", panel, 62f, 30f, 1260f, 80f, "DAILY BRIEFING", 60f, Olive, TextAlignmentOptions.MidlineLeft);
            CreateSolid("Accent", panel, 32f, 37f, 8f, 54f, Gold);
            TMP_Text briefing = CreateText(
                "Briefing",
                panel,
                62f,
                126f,
                1120f,
                260f,
                "Enemy forces are increasing pressure in the South Docks. Secure critical zones and stabilize civilian support to maintain control.",
                45f,
                Text,
                TextAlignmentOptions.TopLeft);
            briefing.textWrappingMode = TextWrappingModes.Normal;
            briefing.overflowMode = TextOverflowModes.Truncate;
            CreateIcon("BriefingEmblem", panel, OperationsIconPath, 1245f, 120f, 180f, 180f, new Color(Text.r, Text.g, Text.b, 0.13f));
            return panel as RectTransform;
        }

        private static RectTransform BuildActiveWarnings(Transform root, out Button[] warningButtons)
        {
            const float panelHeight = 810f;
            Transform panel = CreatePanel("ActiveWarnings", root, 3220f, 1010f, 1500f, panelHeight);
            CreateText("Title", panel, 62f, 28f, 1300f, 82f, "ACTIVE WARNINGS", 60f, Text, TextAlignmentOptions.MidlineLeft);

            string[] titles = { "SOUTH DOCKS UNDER ATTACK", "SUPPLY LINE DISRUPTED", "CIVILIAN PROTESTS", "HIGH THREAT ACTIVITY" };
            string[] details = { "Enemy units detected", "Resource income reduced", "Trust penalty increasing", "Elite units reported" };
            string[] times = { "12:30", "08:45", "06:20", "03:15" };
            warningButtons = new Button[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                Color color = i == titles.Length - 1 ? Danger : Gold;
                warningButtons[i] = CreateWarningRow(panel, 116f + i * 160f, titles[i], details[i], times[i], color);
            }
            SetVerticalStretch(panel as RectTransform, 1010f, 340f);
            NormalizeVerticalChildren(panel, panelHeight, "PanelFill");
            return panel as RectTransform;
        }

        private static Button CreateWarningRow(Transform parent, float y, string title, string detail, string time, Color color)
        {
            Button button = CreateButton("WarningRow", parent, 38f, y, 1424f, 142f, string.Empty, DefaultCardSpritePath, 1f, Text, out TMP_Text unused);
            button.interactable = false;
            button.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.82f);
            CreateIcon("Icon", button.transform, WarningIconPath, 34f, 30f, 86f, 86f, color);
            CreateText("Title", button.transform, 152f, 20f, 1020f, 54f, title, 42f, color, TextAlignmentOptions.MidlineLeft);
            CreateText("Detail", button.transform, 152f, 76f, 1020f, 42f, detail, 31f, Text, TextAlignmentOptions.MidlineLeft);
            CreateText("Time", button.transform, 1200f, 34f, 170f, 70f, time, 42f, color, TextAlignmentOptions.MidlineRight);
            return button;
        }

        private static RectTransform BuildCommandBar(
            Transform root,
            out Button intelReport,
            out Button blackMarket,
            out Button armory,
            out Button commandLog,
            out Button endDay)
        {
            GameObject commandRoot = CreateRect("CommandBar", root, 1160f, 1875f, 3560f, 240f);
            RectTransform commandBar = commandRoot.GetComponent<RectTransform>();
            SetBottomAnchored(commandBar, 1160f, 45f, 3560f, 240f);

            intelReport = CreateCommandButton(commandRoot.transform, "IntelReport", 0f, 0f, 620f, "INTEL REPORT", IntelIconPath, false);
            blackMarket = CreateCommandButton(commandRoot.transform, "BlackMarket", 645f, 0f, 660f, "BLACK MARKET", StarIconPath, true);
            UIShellRouteButtonView marketRoute = blackMarket.gameObject.AddComponent<UIShellRouteButtonView>();
            marketRoute.Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandExchange, true);

            armory = CreateCommandButton(commandRoot.transform, "Armory", 1330f, 0f, 520f, "ARMORY", ArmoryIconPath, true);
            UIShellRouteButtonView armoryRoute = armory.gameObject.AddComponent<UIShellRouteButtonView>();
            armoryRoute.Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, true);

            commandLog = CreateCommandButton(commandRoot.transform, "CommandLog", 1875f, 0f, 600f, "COMMAND LOG", HistoryIconPath, false);
            endDay = CreateButton("EndDay", commandRoot.transform, 2500f, 0f, 1060f, 240f, "END DAY", GoldSpritePath, 82f, new Color(0.12f, 0.09f, 0.03f, 1f), out TMP_Text endDayLabel);
            endDay.interactable = false;
            SetTextRect(endDayLabel.rectTransform, 70f, 15f, 920f, 190f);
            return commandBar;
        }

        private static Button CreateCommandButton(Transform parent, string name, float x, float y, float width, string label, string iconPath, bool interactable)
        {
            Button button = CreateButton(name, parent, x, y, width, 240f, label, SecondarySpritePath, 48f, Text, out TMP_Text text);
            button.interactable = interactable;
            CreateIcon("Icon", button.transform, iconPath, 34f, 55f, 108f, 108f, interactable ? Gold : Muted);
            SetTextRect(text.rectTransform, 160f, 25f, width - 188f, 170f);
            return button;
        }

        private static void CreateDistrictOutline(Transform parent, string name, Color color, float thickness, Vector2[] points)
        {
            if (points == null || points.Length < 3)
                return;

            GameObject root = CreateRect(name, parent, 0f, 0f, 0f, 0f);
            Color lineColor = new(color.r, color.g, color.b, 0.92f);
            for (int i = 0; i < points.Length; i++)
                CreateLine($"Edge{i + 1}", root.transform, points[i], points[(i + 1) % points.Length], thickness, lineColor);
            root.transform.SetAsLastSibling();
        }

        private static void CreateLine(string name, Transform parent, Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 delta = end - start;
            Image line = CreateSolid(name, parent, 0f, 0f, delta.magnitude, thickness, color);
            RectTransform rect = line.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * 0.5f, -(start.y + end.y) * 0.5f);
            rect.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static void CreateLegendItem(Transform parent, float x, float width, string label, string iconPath, Color color)
        {
            CreateIcon($"{label}Icon", parent, iconPath, x, 8f, 52f, 52f, color);
            CreateText($"{label}Label", parent, x + 66f, 0f, width - 66f, 70f, label, 31f, Text, TextAlignmentOptions.MidlineLeft);
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            string label,
            string spritePath,
            float fontSize,
            Color labelColor,
            out TMP_Text labelText)
        {
            Image image = CreateFramed(name, parent, x, y, width, height, spritePath, Color.white);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.68f, 1f);
            colors.pressedColor = new Color(0.76f, 0.64f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.46f, 0.46f, 0.42f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            labelText = CreateText("Label", image.transform, 20f, 0f, width - 40f, height, label, fontSize, labelColor, TextAlignmentOptions.Center);
            return button;
        }

        private static Transform CreatePanel(string name, Transform parent, float x, float y, float width, float height)
        {
            Image frame = CreateFramed(name, parent, x, y, width, height, PanelSpritePath, Color.white);
            Image fill = CreateSolid("PanelFill", frame.transform, 22f, 22f, width - 44f, height - 44f, PanelTint);
            fill.transform.SetAsFirstSibling();
            SetFullStretchMargins(fill.rectTransform, 22f);
            return frame.transform;
        }

        private static Transform CreateDetailPanel(string name, Transform parent, float x, float y, float width, float height)
        {
            Image frame = CreateFramed(name, parent, x, y, width, height, DetailPanelSpritePath, Color.white);
            Image fill = CreateSolid("PanelFill", frame.transform, 12f, 12f, width - 24f, height - 24f, PanelTint);
            fill.transform.SetAsFirstSibling();
            SetFullStretchMargins(fill.rectTransform, 12f);
            return frame.transform;
        }

        private static Image CreateFramed(string name, Transform parent, float x, float y, float width, float height, string spritePath, Color tint, bool sliced = true)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image image = root.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSolid(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            Image image = root.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateCroppedPreview(string name, Transform parent, float x, float y, float width, float height, string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new InvalidOperationException($"Missing preview texture: {texturePath}");

            GameObject root = CreateRect(name, parent, x, y, width, height);
            RawImage image = root.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            float sourceAspect = texture.width / (float)texture.height;
            float targetAspect = width / height;
            if (sourceAspect > targetAspect)
            {
                float visibleWidth = Mathf.Clamp01(targetAspect / sourceAspect);
                image.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
            else
            {
                float visibleHeight = Mathf.Clamp01(sourceAspect / targetAspect);
                image.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, float x, float y, float width, float height, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject root = CreateRect(name, parent, x, y, width, height);
            TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = fontSize >= 36f ? boldFont : mediumFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.characterSpacing = 0f;
            return text;
        }

        private static Image CreateIcon(string name, Transform parent, string path, float x, float y, float width, float height, Color? tint = null)
        {
            Image image = CreateFramed(name, parent, x, y, width, height, path, tint ?? Color.white, false);
            image.preserveAspect = true;
            return image;
        }

        private static GameObject CreateRect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject root = new(name, typeof(RectTransform));
            if (parent != null)
                root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return root;
        }

        private static void SetTextRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetVerticalStretch(RectTransform rect, float top, float bottom)
        {
            float x = rect.anchoredPosition.x;
            float width = rect.sizeDelta.x;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(x, bottom);
            rect.offsetMax = new Vector2(x + width, -top);
        }

        private static void SetBottomAnchored(RectTransform rect, float x, float bottom, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void NormalizeVerticalChildren(Transform parent, float designHeight, string excludedChildName)
        {
            if (parent == null || designHeight <= 0f)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                RectTransform rect = parent.GetChild(i) as RectTransform;
                if (rect == null || rect.name == excludedChildName)
                    continue;

                float x = rect.anchoredPosition.x;
                float width = rect.sizeDelta.x;
                float top = -rect.anchoredPosition.y;
                float height = rect.sizeDelta.y;
                float anchorTop = 1f - top / designHeight;
                float anchorBottom = 1f - (top + height) / designHeight;
                rect.anchorMin = new Vector2(0f, anchorBottom);
                rect.anchorMax = new Vector2(0f, anchorTop);
                rect.pivot = new Vector2(0f, 1f);
                rect.offsetMin = new Vector2(x, 0f);
                rect.offsetMax = new Vector2(x + width, 0f);
            }
        }

        private static void SetFullStretchMargins(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void RouteMainMenuOperationsCard()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                Transform card = FindDescendant(root.transform, "Card_Operations");
                if (card == null)
                    throw new InvalidOperationException("Main Menu prefab is missing Card_Operations.");

                Transform hotspot = FindDescendant(card, "Hotspot");
                if (hotspot == null)
                    throw new InvalidOperationException("Main Menu Operations card is missing its Hotspot.");

                Button button = hotspot.GetComponent<Button>() ?? hotspot.gameObject.AddComponent<Button>();
                Image image = hotspot.GetComponent<Image>() ?? hotspot.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
                button.targetGraphic = image;
                UIShellRouteButtonView route = hotspot.GetComponent<UIShellRouteButtonView>() ?? hotspot.gameObject.AddComponent<UIShellRouteButtonView>();
                route.Configure(UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, true);
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignMenuScenePrefab(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
            if (content == null)
                throw new InvalidOperationException("Menu scene is missing UIShellContentView.");

            SerializedObject serialized = new(content);
            SetReference(serialized, "operationsContentPrefab", prefab);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T ResolveComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = ResolveComponentInHierarchy<T>(root.transform);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static T ResolveComponentInHierarchy<T>(Transform root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
                return component;
            for (int i = 0; i < root.childCount; i++)
            {
                component = ResolveComponentInHierarchy<T>(root.GetChild(i));
                if (component != null)
                    return component;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void RenderCameraToPng(Camera camera, string path, int width, int height)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D texture = null;
            try
            {
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                if (target != null)
                    UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string ResolveCapturePath()
        {
            string configured = Environment.GetEnvironmentVariable("WARLINE_OPERATIONS_CAPTURE_PATH");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                ? "/private/tmp/warline-scn11-operations.png"
                : configured.Trim());
        }

        private static int ResolvePositiveEnvironmentInt(string name, int fallback)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return int.TryParse(configured, out int value) && value > 0 ? value : fallback;
        }

        private static void LoadStyleAssets()
        {
            AssetDatabase.ImportAsset(MapPath, ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath) == null)
                throw new InvalidOperationException($"Missing Operations map art at {MapPath}.");
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            if (boldFont == null || mediumFont == null)
                throw new InvalidOperationException("Missing Operations display fonts.");
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Missing UI sprite: {path}");
            return sprite;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property {propertyName} on {serialized.targetObject.GetType().Name}.");
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized array {propertyName} on {serialized.targetObject.GetType().Name}.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
