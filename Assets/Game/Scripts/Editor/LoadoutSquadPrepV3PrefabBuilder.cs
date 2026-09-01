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
    public static class LoadoutSquadPrepV3PrefabBuilder
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN07_LoadoutSquadPrepContent.prefab";
        private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
        private const string MediumFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset";
        private const string RifleArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_02_Alt_02_Rifleman_Action_512.png";
        private const string ApcArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_APC_Heavy_Action_512.png";
        private const string TankArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Tank_USA_Action_512.png";
        private const string HelicopterArtPath = "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Helicopter_Attack_Action_512.png";

        private static readonly Vector2 ReferenceResolution = new(1672f, 941f);
        private static readonly Color Border = new Color32(49, 65, 70, 255);
        private static readonly Color DarkTop = new Color32(20, 31, 35, 255);
        private static readonly Color DarkBottom = new Color32(3, 9, 12, 255);
        private static readonly Color Cyan = new Color32(15, 183, 231, 255);
        private static readonly Color Lime = new Color32(126, 192, 49, 255);
        private static readonly Color Purple = new Color32(171, 70, 216, 255);
        private static readonly Color Amber = new Color32(255, 181, 0, 255);
        private static readonly Color Red = new Color32(240, 60, 31, 255);

        private static TMP_FontAsset boldFont;
        private static TMP_FontAsset mediumFont;
        private static V3UiTheme theme;
        private static V3UiArtCatalog catalog;
        private static Sprite rifleArt;
        private static Sprite apcArt;
        private static Sprite tankArt;
        private static Sprite helicopterArt;

        [MenuItem("Game/UI/V3/Rebuild Loadout Squad Prep Final")]
        public static void Build()
        {
            V3UiFoundationBuilder.EnsureBuilt();
            LoadAssets();

            RectTransform root = CreateRect("SCN07_LoadoutSquadPrepContent", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateGradientPanel(root, new Color32(19, 28, 31, 255), new Color32(2, 8, 10, 255), Color.clear, 0f);
            RectTransform composition = CreateTopLeft("LoadoutSquadPrepComposition", root, 0f, 0f, ReferenceResolution.x, ReferenceResolution.y);
            composition.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(ReferenceResolution, MainMenuV3SectionAlignment.Center);

            BuildHeader(composition);
            BuildSelectedUnits(composition);
            BuildSupportAndGear(composition);
            BuildMissionSummary(composition);
            BuildFooter(composition);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save Loadout V3 prefab: {PrefabPath}");
            AssignToOpenMenuScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[LoadoutSquadPrepV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 atlas=equipment-shared");
        }

        [MenuItem("Game/UI/V3/Validate Loadout Squad Prep Final")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new FileNotFoundException($"Missing Loadout V3 prefab: {PrefabPath}");
            string[] required = { "SelectedUnits", "SupportSlots", "RecommendedGear", "MissionSummary", "DeployButton" };
            for (int i = 0; i < required.Length; i++)
                if (FindChild(prefab.transform, required[i]) == null)
                    throw new MissingReferenceException($"Loadout V3 is missing {required[i]}.");
            Image[] art =
            {
                FindImage(prefab.transform, "RifleSquadArt"),
                FindImage(prefab.transform, "ApcArt"),
                FindImage(prefab.transform, "TankArt"),
                FindImage(prefab.transform, "HelicopterArt")
            };
            for (int i = 0; i < art.Length; i++)
                if (art[i] == null || art[i].GetComponent<AspectRatioFitter>() == null)
                    throw new InvalidOperationException("Every Loadout unit portrait must preserve aspect under a crop mask.");
            int gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true).Length;
            if (gradients < 18)
                throw new InvalidOperationException($"Loadout V3 requires procedural gradients; found {gradients}.");
            Debug.Log($"[LoadoutSquadPrepV3PrefabBuilder] validation=Passed gradients={gradients} images={prefab.GetComponentsInChildren<Image>(true).Length}");
        }

        private static void LoadAssets()
        {
            ConfigureSprite(RifleArtPath, 1024);
            ConfigureSprite(ApcArtPath, 1024);
            ConfigureSprite(TankArtPath, 1024);
            ConfigureSprite(HelicopterArtPath, 1024);
            boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            mediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MediumFontPath);
            theme = V3UiFoundationBuilder.RequireTheme();
            catalog = V3UiFoundationBuilder.RequireCatalog();
            rifleArt = RequireSprite(RifleArtPath);
            apcArt = RequireSprite(ApcArtPath);
            tankArt = RequireSprite(TankArtPath);
            helicopterArt = RequireSprite(HelicopterArtPath);
            if (boldFont == null || mediumFont == null)
                throw new MissingReferenceException("Loadout V3 fonts are missing.");
        }

        private static void BuildHeader(RectTransform root)
        {
            RectTransform logo = CreateTopLeft("WarlineLogo", root, 5f, 6f, 300f, 80f);
            CreateGradientPanel(logo, DarkTop, DarkBottom, Border, 3f);
            V3UiFoundationBuilder.AddMainMenuLogo(logo);

            RectTransform title = CreateTopLeft("ScreenTitlePanel", root, 307f, 6f, 708f, 80f);
            CreateGradientPanel(title, DarkTop, DarkBottom, Border, 3f);
            TMP_Text titleText = CreateText("ScreenTitle", title, "LOADOUT / SQUAD PREP", 43f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(titleText.rectTransform, 38f, 4f, 640f, 70f);
            BuildResourceChip(root, "Credits", 1017f, 6f, 234f, 80f, catalog.CreditsIcon, "CREDITS", "24,750");
            BuildResourceChip(root, "Command", 1253f, 6f, 320f, 80f, catalog.CommandIcon, "COMMAND", "8,430");
            Button settings = CreateGradientButton("SettingsButton", root, 1575f, 6f, 90f, 80f, DarkTop, DarkBottom, Border, 3f);
            Image gear = CreateImage("Icon", settings.transform, catalog.SettingsIcon, Color.white, false);
            SetRect(gear.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(48f, 48f), Vector2.zero);
            settings.gameObject.AddComponent<UIShellRouteButtonView>().Configure(UiShellRouteIntent.OpenSettings, UIRoute.Settings, false);
        }

        private static void BuildResourceChip(Transform root, string name, float x, float y, float width, float height, Sprite icon, string label, string value)
        {
            RectTransform chip = CreateTopLeft(name, root, x, y, width, height);
            CreateGradientPanel(chip, DarkTop, DarkBottom, Border, 3f);
            Image iconImage = CreateImage("Icon", chip, icon, Color.white, false);
            SetTopLeft(iconImage.rectTransform, 14f, 15f, 48f, 48f);
            TMP_Text labelText = CreateText("Label", chip, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(labelText.rectTransform, 76f, 5f, width - 84f, 31f);
            TMP_Text valueText = CreateText("Value", chip, value, 31f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(valueText.rectTransform, 76f, 31f, width - 84f, 43f);
        }

        private static void BuildSelectedUnits(RectTransform root)
        {
            RectTransform panel = CreateTopLeft("SelectedUnits", root, 5f, 96f, 615f, 674f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            BuildSectionTitle(panel, "SELECTED UNITS", Cyan, 15f, 8f);
            Sprite[] art = { rifleArt, apcArt, tankArt, helicopterArt };
            string[] artNames = { "RifleSquadArt", "ApcArt", "TankArt", "HelicopterArt" };
            string[] names = { "RIFLE SQUAD", "APC", "TANK", "HELICOPTER" };
            string[] levels = { "LVL 12", "LVL 11", "LVL 10", "LVL 9" };
            string[] hp = { "1,250", "1,800", "2,400", "1,700" };
            string[] power = { "4,800", "12,600", "22,500", "15,800" };
            Color[] accents = { Cyan, Lime, Purple, Amber };
            for (int i = 0; i < 4; i++)
                BuildUnitCard(panel, 11f, 49f + i * 155f, 594f, 146f, art[i], artNames[i], names[i], levels[i], hp[i], power[i], accents[i]);
        }

        private static void BuildUnitCard(Transform parent, float x, float y, float width, float height, Sprite art, string artName, string unitName, string level, string hp, string power, Color accent)
        {
            RectTransform card = CreateTopLeft(unitName.Replace(" ", "") + "Card", parent, x, y, width, height);
            CreateGradientPanel(card, new Color32(15, 29, 34, 255), new Color32(4, 13, 17, 255), accent, 2f);
            RectTransform artClip = CreateTopLeft("ArtClip", card, 2f, 2f, 290f, height - 4f);
            artClip.gameObject.AddComponent<RectMask2D>();
            Image image = CreateImage(artName, artClip, art, Color.white, false);
            Stretch(image.rectTransform);
            AddCover(image, art);
            TMP_Text name = CreateText("Name", card, unitName, 29f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(name.rectTransform, 354f, 8f, 222f, 43f);
            Image classIcon = CreateImage("ClassIcon", card, unitName == "RIFLE SQUAD" ? RequireSprite(V3UiFoundationBuilder.CampaignSquadIconPath) : unitName == "HELICOPTER" ? RequireSprite(V3UiFoundationBuilder.MissionAirIconPath) : RequireSprite(V3UiFoundationBuilder.MissionVehicleIconPath), accent, false);
            SetTopLeft(classIcon.rectTransform, 305f, 12f, 36f, 36f);
            CreateSolidTopLeft("Rule", card, 305f, 50f, 273f, 2f, Border);
            TMP_Text levelText = CreateText("Level", card, level, 17f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(levelText.rectTransform, 305f, 54f, 90f, 30f);
            for (int i = 0; i < 7; i++)
                CreateSolidTopLeft("Progress" + i, card, 305f + i * 26f, 87f, 22f, 15f, i < 6 ? Lime : new Color32(35, 63, 32, 255));
            TMP_Text hpText = CreateText("Health", card, "♥ " + hp, 17f, boldFont, TextAlignmentOptions.MidlineRight, Lime);
            SetTopLeft(hpText.rectTransform, 471f, 70f, 108f, 32f);
            Image shield = CreateImage("PowerIcon", card, RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath), accent, false);
            SetTopLeft(shield.rectTransform, 306f, 108f, 30f, 30f);
            TMP_Text powerText = CreateText("Power", card, power, 21f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(powerText.rectTransform, 344f, 104f, 140f, 36f);
        }

        private static void BuildSupportAndGear(RectTransform root)
        {
            RectTransform panel = CreateTopLeft("SupportAndGear", root, 627f, 96f, 563f, 674f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            RectTransform support = CreateTopLeft("SupportSlots", panel, 12f, 0f, 539f, 355f);
            BuildSectionTitle(support, "SUPPORT SLOTS", Cyan, 0f, 8f);
            string[] supportNames = { "AIRSTRIKE", "MEDIC DROP", "EMP BLAST", "LOCKED" };
            string[] supportLevels = { "LVL 3", "LVL 2", "LVL 2", "—" };
            Sprite[] supportIcons =
            {
                RequireSprite(V3UiFoundationBuilder.EquipmentAircraftIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentHealthIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentEmpIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentLockIconPath)
            };
            for (int i = 0; i < 4; i++)
                BuildSupportCard(support, i * 136f, 68f, 128f, 260f, supportNames[i], supportLevels[i], supportIcons[i], i == 3);

            RectTransform gear = CreateTopLeft("RecommendedGear", panel, 12f, 366f, 539f, 298f);
            BuildSectionTitle(gear, "RECOMMENDED GEAR", Cyan, 0f, 8f);
            string[] gearNames = { "ARMOR\nPLATE", "TARGETING\nMODULE", "AMMO\nCRATE", "REPAIR\nKIT" };
            string[] bonuses = { "+12%", "+15%", "+20%", "+10%" };
            Sprite[] gearIcons =
            {
                RequireSprite(V3UiFoundationBuilder.EquipmentArmorIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentTargetingIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentAmmoIconPath),
                RequireSprite(V3UiFoundationBuilder.EquipmentRepairIconPath)
            };
            Color[] bonusColors = { Cyan, Purple, Lime, Cyan };
            for (int i = 0; i < 4; i++)
                BuildGearCard(gear, i * 136f, 51f, 128f, 249f, gearNames[i], bonuses[i], gearIcons[i], bonusColors[i]);
        }

        private static void BuildSupportCard(Transform parent, float x, float y, float width, float height, string name, string level, Sprite iconSprite, bool locked)
        {
            RectTransform card = CreateTopLeft(name.Replace(" ", "") + "Support", parent, x, y, width, height);
            CreateGradientPanel(card, new Color32(31, 39, 41, 255), new Color32(8, 15, 18, 255), locked ? Border : new Color32(80, 91, 94, 255), 2f);
            TMP_Text title = CreateText("Title", card, name, 16f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 5f, 10f, width - 10f, 35f);
            Image icon = CreateImage("Icon", card, iconSprite, Color.white, false);
            SetTopLeft(icon.rectTransform, 4f, 45f, 120f, 145f);
            icon.preserveAspect = true;
            CreateSolidTopLeft("Rule", card, 10f, 199f, width - 20f, 2f, Border);
            TMP_Text levelText = CreateText("Level", card, level, 20f, boldFont, TextAlignmentOptions.Center, locked ? theme.TextPrimary : Cyan);
            SetTopLeft(levelText.rectTransform, 8f, 205f, width - 16f, 43f);
        }

        private static void BuildGearCard(Transform parent, float x, float y, float width, float height, string name, string bonus, Sprite iconSprite, Color accent)
        {
            RectTransform card = CreateTopLeft(name.Replace("\n", "") + "Gear", parent, x, y, width, height);
            CreateGradientPanel(card, new Color32(31, 39, 41, 255), new Color32(8, 15, 18, 255), new Color32(80, 91, 94, 255), 2f);
            TMP_Text title = CreateText("Title", card, name, 16f, boldFont, TextAlignmentOptions.Top, theme.TextPrimary);
            SetTopLeft(title.rectTransform, 6f, 10f, width - 12f, 52f);
            title.enableWordWrapping = true;
            Image icon = CreateImage("Icon", card, iconSprite, Color.white, false);
            SetTopLeft(icon.rectTransform, 4f, 48f, 120f, 142f);
            icon.preserveAspect = true;
            TMP_Text bonusText = CreateText("Bonus", card, bonus, 20f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(bonusText.rectTransform, 7f, 198f, width - 14f, 42f);
            bonusText.color = accent;
        }

        private static void BuildMissionSummary(RectTransform root)
        {
            RectTransform panel = CreateTopLeft("MissionSummary", root, 1197f, 96f, 468f, 674f);
            CreateGradientPanel(panel, DarkTop, DarkBottom, Border, 3f);
            BuildSectionTitle(panel, "MISSION SUMMARY", Cyan, 16f, 8f);

            BuildSubsectionTitle(panel, "OBJECTIVES", 18f, 65f, Cyan);
            string[] objectives = { "Capture the Forward HQ", "Protect Allied Convoys", "Clear All Enemy Zones" };
            string[] counts = { "0/1", "0/3", "0/4" };
            Sprite[] objectiveIcons =
            {
                catalog.AttackIcon,
                RequireSprite(V3UiFoundationBuilder.CampaignHoldIconPath),
                RequireSprite(V3UiFoundationBuilder.MissionIntelIconPath)
            };
            for (int i = 0; i < 3; i++)
                BuildObjectiveLine(panel, 18f, 105f + i * 46f, 430f, objectives[i], counts[i], objectiveIcons[i], Cyan);

            BuildSubsectionTitle(panel, "STAR GOALS", 18f, 267f, Cyan);
            string[] goals = { "Complete Mission", "Complete in 4:30", "Lose no more than 1 unit" };
            for (int i = 0; i < 3; i++)
                BuildObjectiveLine(panel, 18f, 307f + i * 46f, 430f, goals[i], "+1", RequireSprite(V3UiFoundationBuilder.MissionStarIconPath), Amber);

            BuildSubsectionTitle(panel, "ENEMY RATING", 18f, 465f, Cyan);
            RectTransform threat = CreateTopLeft("EnemyThreat", panel, 16f, 502f, 436f, 150f);
            CreateGradientPanel(threat, new Color32(20, 30, 33, 255), new Color32(4, 11, 14, 255), Border, 2f);
            Image skull = CreateImage("ThreatIcon", threat, RequireSprite(V3UiFoundationBuilder.MissionEnemyIconPath), Red, false);
            SetTopLeft(skull.rectTransform, 18f, 14f, 66f, 66f);
            TMP_Text high = CreateText("ThreatLabel", threat, "HIGH THREAT", 27f, boldFont, TextAlignmentOptions.MidlineLeft, Red);
            SetTopLeft(high.rectTransform, 96f, 15f, 220f, 48f);
            for (int i = 0; i < 8; i++)
                CreateSolidTopLeft("ThreatBar" + i, threat, 98f + i * 39f, 65f, 34f, 15f, i < 5 ? Red : new Color32(27, 33, 35, 255));
            TMP_Text powerLabel = CreateText("PowerLabel", threat, "Enemy Power", 20f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(powerLabel.rectTransform, 22f, 101f, 210f, 38f);
            TMP_Text power = CreateText("Power", threat, "58,200", 31f, boldFont, TextAlignmentOptions.MidlineRight, Red);
            SetTopLeft(power.rectTransform, 267f, 96f, 150f, 44f);
        }

        private static void BuildFooter(RectTransform root)
        {
            RectTransform footer = CreateTopLeft("Footer", root, 5f, 778f, 1660f, 135f);
            CreateGradientPanel(footer, DarkTop, DarkBottom, Border, 3f);
            RectTransform totals = CreateTopLeft("Totals", footer, 12f, 10f, 610f, 113f);
            CreateGradientPanel(totals, new Color32(18, 30, 35, 255), DarkBottom, Border, 2f);
            BuildFooterStat(totals, 5f, RequireSprite(V3UiFoundationBuilder.CampaignSquadIconPath), "TOTAL UNITS", "33/33", Cyan);
            BuildFooterStat(totals, 300f, RequireSprite(V3UiFoundationBuilder.CommanderRankIconPath), "SQUAD POWER", "55,700", Cyan);

            Button edit = CreateGradientButton("EditLoadoutButton", footer, 627f, 10f, 487f, 113f, DarkTop, DarkBottom, Border, 3f);
            TMP_Text editText = CreateText("Label", edit.transform, "EDIT LOADOUT", 31f, boldFont, TextAlignmentOptions.Center, theme.TextPrimary);
            SetTopLeft(editText.rectTransform, 35f, 19f, 350f, 73f);
            Image editIcon = CreateImage("Icon", edit.transform, RequireSprite(V3UiFoundationBuilder.CommanderEditIconPath), Cyan, false);
            SetTopLeft(editIcon.rectTransform, 385f, 31f, 51f, 51f);

            Button deploy = CreateGradientButton("DeployButton", footer, 1125f, 10f, 523f, 113f, new Color32(255, 197, 24, 255), new Color32(238, 151, 0, 255), Amber, 3f);
            TMP_Text deployText = CreateText("Label", deploy.transform, "DEPLOY 10", 45f, boldFont, TextAlignmentOptions.Center, Color.black);
            SetTopLeft(deployText.rectTransform, 35f, 10f, 390f, 90f);
            TMP_Text bolt = CreateText("CostIcon", deploy.transform, "ϟ", 58f, boldFont, TextAlignmentOptions.Center, Color.black);
            SetTopLeft(bolt.rectTransform, 402f, 9f, 80f, 90f);
        }

        private static void BuildFooterStat(Transform parent, float x, Sprite iconSprite, string label, string value, Color accent)
        {
            RectTransform stat = CreateTopLeft(label.Replace(" ", ""), parent, x, 0f, 300f, 113f);
            Image icon = CreateImage("Icon", stat, iconSprite, accent, false);
            SetTopLeft(icon.rectTransform, 12f, 20f, 68f, 68f);
            TMP_Text labelText = CreateText("Label", stat, label, 19f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextMuted);
            SetTopLeft(labelText.rectTransform, 94f, 10f, 196f, 35f);
            TMP_Text valueText = CreateText("Value", stat, value, 35f, boldFont, TextAlignmentOptions.MidlineLeft, accent);
            SetTopLeft(valueText.rectTransform, 94f, 42f, 196f, 54f);
        }

        private static void BuildSectionTitle(Transform parent, string label, Color accent, float x, float y)
        {
            CreateSolidTopLeft("Accent", parent, x, y + 6f, 5f, 25f, accent);
            TMP_Text title = CreateText("Title", parent, label, 25f, boldFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(title.rectTransform, x + 17f, y, 440f, 40f);
        }

        private static void BuildSubsectionTitle(Transform parent, string label, float x, float y, Color accent)
        {
            CreateSolidTopLeft(label + "Accent", parent, x, y + 5f, 5f, 22f, accent);
            TMP_Text title = CreateText(label + "Title", parent, label, 18f, boldFont, TextAlignmentOptions.MidlineLeft, accent);
            SetTopLeft(title.rectTransform, x + 16f, y, 260f, 34f);
        }

        private static void BuildObjectiveLine(Transform parent, float x, float y, float width, string label, string value, Sprite iconSprite, Color accent)
        {
            RectTransform row = CreateTopLeft(label.Replace(" ", ""), parent, x, y, width, 42f);
            CreateSolidTopLeft("Rule", row, 0f, 40f, width, 2f, new Color32(29, 43, 47, 255));
            Image icon = CreateImage("Icon", row, iconSprite, accent, false);
            SetTopLeft(icon.rectTransform, 8f, 4f, 32f, 32f);
            TMP_Text text = CreateText("Label", row, label, 18f, mediumFont, TextAlignmentOptions.MidlineLeft, theme.TextPrimary);
            SetTopLeft(text.rectTransform, 54f, 0f, width - 132f, 40f);
            TMP_Text valueText = CreateText("Value", row, value, 18f, boldFont, TextAlignmentOptions.MidlineRight, accent);
            SetTopLeft(valueText.rectTransform, width - 70f, 0f, 65f, 40f);
        }

        private static void AssignToOpenMenuScene(GameObject prefab)
        {
            UIShellContentView content = UnityEngine.Object.FindFirstObjectByType<UIShellContentView>(FindObjectsInactive.Include);
            if (content == null || content.gameObject.scene.path != "Assets/Game/Scenes/Menu.unity")
            {
                Debug.LogWarning("[LoadoutSquadPrepV3PrefabBuilder] Menu scene is not open; prefab built but shell assignment was skipped.");
                return;
            }
            SerializedObject serialized = new(content);
            SerializedProperty property = serialized.FindProperty("loadoutSquadPrepContentPrefab");
            if (property == null)
                throw new MissingFieldException(nameof(UIShellContentView), "loadoutSquadPrepContentPrefab");
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
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void AddCover(Image image, Sprite sprite)
        {
            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
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

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Missing Loadout V3 sprite: {path}");
            importer.textureType = TextureImporterType.Sprite;
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
                throw new FileNotFoundException($"Missing Loadout V3 sprite: {path}");
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

        private static Image FindImage(Transform root, string name) => FindChild(root, name)?.GetComponent<Image>();
    }
}
