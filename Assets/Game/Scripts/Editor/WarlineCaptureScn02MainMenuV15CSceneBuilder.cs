#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn02MainMenuV15CSceneBuilder
{
    private const int CanvasWidth = 3840;
    private const int CanvasHeight = 2160;
    private const int CanvasWidth20x9 = 4800;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_V22OneGo.prefab";
    private const string PrefabPath20x9 = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_V22OneGo_20x9.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN02_MainMenu_V22OneGo.unity";
    private const string ScenePath20x9 = "Assets/Game/Scenes/DesignTargets/SCN02_MainMenu_V22OneGo_20x9.unity";
    private const string CapturePath = "Design/AgentReports/Captures/MainMenuV15C/SCN02_MainMenu_V22OneGo_3840x2160.png";
    private const string CapturePath20x9 = "Design/AgentReports/Captures/MainMenuV15C/SCN02_MainMenu_V22OneGo_2400x1080.png";
    private const string DiagnosticPath = "Design/AgentReports/Captures/MainMenuV15C/SCN02_MainMenu_V22OneGo_diagnostics_3840x2160.png";
    private const int DefaultSelectedNavIndex = 0;
    private const float VisibleCenterTolerance = 2f;
    private static int s_LayoutWidth = CanvasWidth;

    private static readonly Dictionary<string, RectInt> s_VisibleBoundsCache = new();
    private static readonly List<DiagnosticRect> s_Diagnostics = new();

    private static readonly NavItem[] s_NavItems =
    {
        new("Campaign", "scn02_icon_campaign_crosshair.png"),
        new("Operations", "scn02_icon_operations_pin.png"),
        new("Skirmish", "scn02_icon_skirmish_blades.png"),
        new("Store", "scn02_icon_store_cart.png"),
        new("Commander", "scn02_icon_commander_bust.png"),
        new("Settings", "scn02_icon_settings_gear.png")
    };

    [MenuItem("WarlineCapture/Design/SCN-02/V15C Build One-Go Main Menu Scene")]
    public static void BuildScene()
    {
        BuildSceneForLayout(CanvasWidth, PrefabPath, ScenePath, "Screen_MainMenu_V22OneGo");
    }

    private static void BuildSceneForLayout(int layoutWidth, string prefabPath, string scenePath, string sceneRootName)
    {
        s_LayoutWidth = layoutWidth;
        EnsureLayerSpriteImports();
        s_Diagnostics.Clear();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot(sceneRootName);

        EnsureParentFolder(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

        GameObject sceneCanvas = CreateRectObject("SCN02_MainMenu_V15COneGo_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(s_LayoutWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = UnityEngine.Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = sceneRootName;
        StretchToParent(instance.GetComponent<RectTransform>());
        UnityEngine.Object.DestroyImmediate(prefabRoot);

        AddEventSystem();
        AddSceneCamera();

        EnsureParentFolder(scenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-02 V15C] Built scene={scenePath} prefab={prefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-02/V15C Capture One-Go 16x9")]
    public static void CaptureScene()
    {
        BuildScene();
        CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, Color.black, CanvasWidth);
        WriteDiagnosticOverlay(CapturePath, DiagnosticPath, CanvasWidth, CanvasHeight);
        Debug.Log($"[SCN-02 V15C] Captured {CapturePath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-02/V15C Capture One-Go 20x9")]
    public static void CaptureScene20x9()
    {
        BuildSceneForLayout(CanvasWidth20x9, PrefabPath20x9, ScenePath20x9, "Screen_MainMenu_V22OneGo_20x9");
        CapturePrefab(PrefabPath20x9, CapturePath20x9, 2400, 1080, Color.black, CanvasWidth20x9);
        Debug.Log($"[SCN-02 V15C] Captured {CapturePath20x9}");
    }

    private static GameObject BuildCanvasPrefabRoot(string sceneRootName)
    {
        GameObject root = CreateRectObject(sceneRootName, null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(s_LayoutWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenSystem screenController = root.AddComponent<WarlineCaptureScreenSystem>();
        screenController.SetRouteForTests(WarlineCaptureRoute.MainMenu);

        GameObject artRoot = CreateRectObject("MainMenuV15C_OneGo_LayeredCanvas", root.transform);
        StretchToParent(artRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(artRoot.transform);

        GameObject hitRoot = CreateRectObject("MainMenuV15C_HitZones", root.transform);
        StretchToParent(hitRoot.GetComponent<RectTransform>());
        AddHitZones(hitRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        AddCoverImage(parent, "Background_CommandTentMap", "scn02_background_art.png", new RectInt(0, 0, s_LayoutWidth, CanvasHeight), Color.white);

        AddHeader(parent);
        AddNavRail(parent);
        AddCommsPanel(parent);
        AddModeCard(
            parent,
            "Campaign",
            "scn02_icon_campaign_crosshair.png",
            "scn02_icon_operations_pin.png",
            "scn02_campaign_thumbnail_art.png",
            "Advance the campaign and secure key objectives.",
            CardRect(0),
            new Color32(176, 137, 37, 255));
        AddModeCard(
            parent,
            "Operations",
            "scn02_icon_campaign_crosshair.png",
            "scn02_icon_operations_pin.png",
            "scn02_operations_thumbnail_art.png",
            "Manage live operations and district pressure.",
            CardRect(1),
            new Color32(89, 162, 164, 255));
        AddModeCard(
            parent,
            "Skirmish",
            "scn02_icon_skirmish_blades.png",
            "scn02_icon_lightning_small.png",
            "scn02_skirmish_thumbnail_art.png",
            "Set up a custom combat scenario.",
            CardRect(2),
            new Color32(146, 154, 68, 255));
        AddCommanderPanel(parent);
        AddDeployButton(parent);
        ValidateMajorPanels();
    }

    private static void AddHeader(Transform parent)
    {
        AddImage(parent, "Header_LogoPanel", "scn02_header_logo_panel_bg.png", ToArray(HeaderLogoPanelRect()), false, Color.white);
        AddImage(parent, "Header_CreditsPanel", "scn02_header_resource_panel_bg.png", ToArray(HeaderResourcePanelRect(0)), false, Color.white);
        AddImage(parent, "Header_SuppliesPanel", "scn02_header_resource_panel_bg.png", ToArray(HeaderResourcePanelRect(1)), false, Color.white);
        AddImage(parent, "Header_CommandPanel", "scn02_header_logo_panel_bg.png", ToArray(HeaderCommandPanelRect()), false, Color.white);
        AddImage(parent, "Header_RightActionsPanel", "scn02_header_right_actions_bg.png", ToArray(HeaderActionPanelRect()), false, Color.white);

        AddFittedImage(parent, "Header_Logo", "scn02_brand_logo_lockup.png", Inset(HeaderLogoPanelRect(), 54, 34), 650, 142, Color.white);
        AddHeaderResource(parent, "Credits", "scn02_resource_coin_badge.png", "Credits", "187,540", HeaderResourceContentRect(0), new Color32(235, 179, 65, 255), 126, 126);
        AddHeaderResource(parent, "Supplies", "scn02_resource_supplies_crate.png", "Supplies", "92,860", HeaderResourceContentRect(1), new Color32(161, 166, 105, 255), 152, 126);
        AddHeaderResource(parent, "Command", "scn02_resource_command_shield.png", "Command", "2,715", HeaderResourceContentRect(2), new Color32(119, 180, 215, 255), 142, 166);
        AddHeaderAction(parent, "Inbox", "scn02_icon_inbox_envelope.png", HeaderActionSlot(0), 150, 118);
        AddHeaderAction(parent, "Settings", "scn02_icon_settings_gear.png", HeaderActionSlot(1), 148, 148);
    }

    private static void AddHeaderResource(Transform parent, string name, string icon, string label, string value, RectInt slot, Color valueColor, int iconW, int iconH)
    {
        RectInt safe = Inset(slot, 48, 28);
        bool command = string.Equals(name, "Command", StringComparison.Ordinal);
        RectInt iconSlot = command
            ? new RectInt(safe.x + 20, safe.y + 24, 170, 150)
            : new RectInt(safe.x + 22, safe.y + 28, 142, 132);
        int textX = command ? safe.x + 244 : safe.x + 194;
        RectInt labelSlot = new(textX, safe.y + 40, safe.xMax - textX - 28, 38);
        RectInt valueSlot = new(textX, safe.y + 84, safe.xMax - textX - 28, 60);
        ImagePlacement iconPlacement = VisibleFittedPlacement(icon, iconSlot, iconW, iconH);

        ValidateSectionContent(
            $"Header_{name}",
            safe,
            new LayoutRect($"{name}_Icon", iconPlacement.VisibleRect),
            new LayoutRect($"{name}_Label", labelSlot),
            new LayoutRect($"{name}_Value", valueSlot));

        AddFittedImage(parent, $"Header_{name}_Icon", icon, iconSlot, iconW, iconH, Color.white);
        AddText(parent, $"Header_{name}_Label", label, ToArray(labelSlot), 28f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, $"Header_{name}_Value", value, ToArray(valueSlot), 44f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderAction(Transform parent, string name, string icon, RectInt slot, int iconW, int iconH)
    {
        RectInt safe = Inset(slot, 0, 0);
        ImagePlacement placement = VisibleFittedPlacement(icon, safe, iconW, iconH);
        ValidateSectionContent($"HeaderAction_{name}", safe, new LayoutRect($"{name}_Icon", placement.VisibleRect));
        AddFittedImage(parent, $"HeaderAction_{name}", icon, safe, iconW, iconH, new Color32(221, 216, 194, 255));
    }

    private static void AddNavRail(Transform parent)
    {
        ValidateSelectedNavModel();
        for (int i = 0; i < s_NavItems.Length; i++)
            AddNavButton(parent, i, s_NavItems[i], NavRect(i));
    }

    private static void AddNavButton(Transform parent, int index, NavItem item, RectInt rect)
    {
        bool selected = index == DefaultSelectedNavIndex;
        AddImage(parent, $"Nav_{item.Label}_Frame", selected ? "scn02_nav_button_selected_frame.png" : "scn02_nav_button_inactive_frame.png", ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 44, 22);
        RectInt iconSlot = new(safe.x + 4, safe.y, 114, safe.height);
        RectInt textSlot = new(safe.x + 154, safe.y + 16, safe.width - 178, safe.height - 32);
        ImagePlacement iconPlacement = VisibleFittedPlacement(item.Icon, iconSlot, 104, 104);
        ValidateSectionContent(
            $"Nav_{item.Label}",
            safe,
            new LayoutRect($"{item.Label}_Icon", iconPlacement.VisibleRect),
            new LayoutRect($"{item.Label}_Text", textSlot));

        Color contentColor = selected ? new Color32(246, 239, 204, 255) : new Color32(221, 216, 196, 255);
        AddFittedImage(parent, $"Nav_{item.Label}_Icon", item.Icon, iconSlot, 104, 104, contentColor);
        AddText(parent, $"Nav_{item.Label}_Text", item.Label, ToArray(textSlot), 42f, TextAlignmentOptions.Left, contentColor);
    }

    private static void AddCommsPanel(Transform parent)
    {
        RectInt rect = CommsRect();
        AddImage(parent, "CommsOnline_Frame", "scn02_comms_status_panel_frame.png", ToArray(rect), false, Color.white);
        RectInt safe = Inset(rect, 54, 30);
        RectInt iconSlot = new(safe.x + 4, safe.y, 70, safe.height);
        RectInt textSlot = new(safe.x + 96, safe.y + 8, safe.width - 124, safe.height - 16);
        ImagePlacement iconPlacement = VisibleFittedPlacement("scn02_icon_lock.png", iconSlot, 40, 48);
        ValidateSectionContent(
            "CommsOnline",
            safe,
            new LayoutRect("CommsIcon", iconPlacement.VisibleRect),
            new LayoutRect("CommsText", textSlot));
        AddFittedImage(parent, "CommsOnline_Icon", "scn02_icon_lock.png", iconSlot, 40, 48, new Color32(143, 172, 47, 255));
        AddText(parent, "CommsOnline_Text", "COMMS ONLINE", ToArray(textSlot), 27f, TextAlignmentOptions.Left, new Color32(143, 180, 43, 255));
    }

    private static void AddModeCard(Transform parent, string title, string titleIcon, string footerIcon, string art, string description, RectInt rect, Color progressColor)
    {
        AddImage(parent, $"Card_{title}_Frame", "scn02_mode_card_frame.png", ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 44, 44);
        RectInt titleBand = new(safe.x + 42, safe.y + 26, safe.width - 84, 100);
        RectInt titleIconSlot = new(titleBand.x, titleBand.y + 4, 108, 92);
        RectInt titleSlot = new(titleBand.x + 144, titleBand.y, titleBand.width - 156, 100);
        RectInt artSlot = new(safe.x + 46, safe.y + 184, safe.width - 92, 555);
        RectInt bodySlot = new(safe.x + 68, artSlot.yMax + 36, safe.width - 136, 150);
        RectInt footerBand = new(safe.x + 56, safe.y + safe.height - 144, safe.width - 112, 104);
        RectInt footerIconSlot = new(footerBand.x, footerBand.y + 6, 110, 92);
        RectInt progressSlot = new(footerBand.x + 154, footerBand.y + 28, footerBand.width - 174, 48);

        ImagePlacement titleIconPlacement = VisibleFittedPlacement(titleIcon, titleIconSlot, 92, 92);
        ImagePlacement footerIconPlacement = VisibleFittedPlacement(footerIcon, footerIconSlot, 92, 92);
        ValidateSectionContent(
            $"Card_{title}",
            safe,
            new LayoutRect($"{title}_TitleIcon", titleIconPlacement.VisibleRect),
            new LayoutRect($"{title}_Title", titleSlot),
            new LayoutRect($"{title}_Art", artSlot),
            new LayoutRect($"{title}_Body", bodySlot),
            new LayoutRect($"{title}_FooterIcon", footerIconPlacement.VisibleRect),
            new LayoutRect($"{title}_Progress", progressSlot));

        AddFittedImage(parent, $"Card_{title}_TitleIcon", titleIcon, titleIconSlot, 92, 92, TextMuted);
        AddText(parent, $"Card_{title}_Title", title.ToUpperInvariant(), ToArray(titleSlot), 54f, TextAlignmentOptions.Left, TextMain);
        AddCoverImage(parent, $"Card_{title}_Art", art, artSlot, Color.white);
        AddText(parent, $"Card_{title}_Description", description, ToArray(bodySlot), 38f, TextAlignmentOptions.TopLeft, TextMuted, true);
        AddFittedImage(parent, $"Card_{title}_FooterIcon", footerIcon, footerIconSlot, 92, 92, TextMuted);
        AddProgress(parent, $"Card_{title}_Progress", progressSlot, progressColor);
    }

    private static void AddCommanderPanel(Transform parent)
    {
        RectInt rect = CommanderRect();
        AddImage(parent, "CommanderPanel_Frame", "scn02_commander_panel_frame.png", ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 38, 34);
        RectInt titleSlot = SourceToCanvasRect(rect, 365, 674, new RectInt(48, 24, 268, 38));
        RectInt portraitWell = SourceToCanvasRect(rect, 365, 674, new RectInt(42, 80, 281, 269));
        RectInt portraitFrameSlot = Inset(portraitWell, 46, 42);
        RectInt portraitArtSlot = Inset(portraitFrameSlot, 26, 28);
        RectInt identitySlot = SourceToCanvasRect(rect, 365, 674, new RectInt(42, 360, 281, 43));
        RectInt readinessSlot = SourceToCanvasRect(rect, 365, 674, new RectInt(42, 418, 281, 43));
        RectInt rowOne = SourceToCanvasRect(rect, 365, 674, new RectInt(42, 476, 281, 74));
        RectInt rowTwo = SourceToCanvasRect(rect, 365, 674, new RectInt(42, 568, 281, 74));

        ValidateSectionContent(
            "CommanderPanel",
            safe,
            new LayoutRect("CommanderTitle", titleSlot),
            new LayoutRect("CommanderPortrait", portraitWell),
            new LayoutRect("CommanderIdentity", identitySlot),
            new LayoutRect("CommanderReadiness", readinessSlot),
            new LayoutRect("SquadManagement", rowOne),
            new LayoutRect("IntelReport", rowTwo));

        AddText(parent, "CommanderPanel_Title", "COMMANDER", ToArray(titleSlot), 34f, TextAlignmentOptions.Center, TextMain);
        AddImage(parent, "CommanderPanel_PortraitFrame", "scn02_commander_portrait_frame.png", ToArray(portraitFrameSlot), false, Color.white);
        AddCoverImage(parent, "CommanderPanel_PortraitArt", "scn02_commander_portrait_art.png", portraitArtSlot, Color.white);
        AddCommanderIdentity(parent, identitySlot);
        AddCommanderReadiness(parent, readinessSlot);
        AddLockedRow(parent, "SquadManagement", "SQUAD MANAGEMENT\nLOCKED", rowOne);
        AddLockedRow(parent, "IntelReport", "INTEL REPORT\nLOCKED", rowTwo);
    }

    private static void AddCommanderIdentity(Transform parent, RectInt slot)
    {
        RectInt safe = Inset(slot, 22, 12);
        RectInt badgeSlot = new(safe.x, safe.y, 92, safe.height);
        RectInt nameSlot = new(safe.x + 124, safe.y + 4, safe.width - 134, 24);
        RectInt levelSlot = new(safe.x + 124, safe.y + 34, safe.width - 134, 28);
        ValidateSectionContent(
            "CommanderIdentity",
            safe,
            new LayoutRect("CommanderBadge", VisibleFittedPlacement("scn02_resource_command_shield.png", badgeSlot, 66, 82).VisibleRect),
            new LayoutRect("CommanderName", nameSlot),
            new LayoutRect("CommanderLevel", levelSlot));
        AddFittedImage(parent, "CommanderPanel_Badge", "scn02_resource_command_shield.png", badgeSlot, 66, 82, Color.white);
        AddText(parent, "CommanderPanel_Name", "FIELD COMMANDER", ToArray(nameSlot), 18f, TextAlignmentOptions.Left, TextMuted);
        AddText(parent, "CommanderPanel_Level", "LEVEL 38", ToArray(levelSlot), 22f, TextAlignmentOptions.Left, GoldText);
    }

    private static void AddCommanderReadiness(Transform parent, RectInt slot)
    {
        RectInt safe = Inset(slot, 22, 14);
        RectInt labelSlot = new(safe.x, safe.y + 6, Mathf.RoundToInt(safe.width * 0.34f), safe.height - 12);
        RectInt pipsSlot = new(labelSlot.xMax + 24, safe.y + 8, safe.width - labelSlot.width - 34, safe.height - 16);
        ValidateSectionContent(
            "CommanderReadiness",
            safe,
            new LayoutRect("ReadinessLabel", labelSlot),
            new LayoutRect("ReadinessPips", pipsSlot));
        AddText(parent, "CommanderPanel_ReadinessLabel", "READINESS", ToArray(labelSlot), 22f, TextAlignmentOptions.Left, TextMuted);
        AddFittedImage(parent, "CommanderPanel_ReadinessSegments", "scn02_readiness_segments.png", pipsSlot, pipsSlot.width, 40, Color.white);
    }

    private static void AddLockedRow(Transform parent, string name, string text, RectInt slot)
    {
        AddImage(parent, $"{name}_Frame", "scn02_locked_row_frame.png", ToArray(slot), false, Color.white);
        RectInt safe = Inset(slot, 42, 28);
        RectInt iconSlot = new(safe.x + 4, safe.y, 76, safe.height);
        RectInt textSlot = new(safe.x + 112, safe.y + 8, safe.width - 124, safe.height - 16);
        ValidateSectionContent(
            name,
            safe,
            new LayoutRect($"{name}_Icon", VisibleFittedPlacement("scn02_icon_lock.png", iconSlot, 44, 52).VisibleRect),
            new LayoutRect($"{name}_Text", textSlot));
        AddFittedImage(parent, $"{name}_LockIcon", "scn02_icon_lock.png", iconSlot, 42, 50, DisabledText);
        AddText(parent, $"{name}_Text", text, ToArray(textSlot), 23f, TextAlignmentOptions.Left, DisabledText);
    }

    private static void AddDeployButton(Transform parent)
    {
        RectInt rect = DeployRect();
        AddImage(parent, "DeployOperation_Frame", "scn02_deploy_cta_frame.png", ToArray(rect), false, Color.white);
        RectInt safe = Inset(rect, 128, 48);
        int groupWidth = 850;
        int groupX = safe.x + (safe.width - groupWidth) / 2;
        RectInt textSlot = new(groupX, safe.y + 12, groupWidth - 158, safe.height - 24);
        RectInt chevrons = new(groupX + groupWidth - 138, safe.y + 22, 130, safe.height - 44);
        ValidateSectionContent(
            "DeployOperation",
            safe,
            new LayoutRect("DeployText", textSlot),
            new LayoutRect("DeployChevrons", VisibleFittedPlacement("scn02_deploy_chevrons.png", chevrons, 136, 86).VisibleRect));
        AddText(parent, "DeployOperation_Text", "DEPLOY OPERATION", ToArray(textSlot), 58f, TextAlignmentOptions.Center, Color.black);
        AddFittedImage(parent, "DeployOperation_Chevrons", "scn02_deploy_chevrons.png", chevrons, 118, 76, new Color32(75, 62, 28, 255));
    }

    private static void AddProgress(Transform parent, string name, RectInt slot, Color fillColor)
    {
        AddImage(parent, $"{name}_Frame", "scn02_mode_progress_meter_frame.png", ToArray(slot), false, Color.white);
        RectInt safe = Inset(slot, 18, 13);
        int segments = 8;
        int gap = 8;
        int segmentWidth = (safe.width - gap * (segments - 1)) / segments;
        int filledSegments = 4;
        for (int i = 0; i < segments; i++)
        {
            GameObject segment = CreateRectObject($"{name}_Segment_{i + 1:00}", parent);
            RectInt rect = new(safe.x + i * (segmentWidth + gap), safe.y + 4, segmentWidth, safe.height - 8);
            ApplyTopLeftRect(segment.GetComponent<RectTransform>(), ToArray(rect));
            Image image = segment.AddComponent<Image>();
            image.color = i < filledSegments ? fillColor : new Color32(48, 52, 42, 200);
            image.raycastTarget = false;
        }
    }

    private static void AddHitZones(Transform parent)
    {
        for (int i = 0; i < s_NavItems.Length; i++)
            AddHitZone(parent, $"Nav_{s_NavItems[i].Label}", NavRect(i));
        AddHitZone(parent, "Card_Campaign", CardRect(0));
        AddHitZone(parent, "Card_Operations", CardRect(1));
        AddHitZone(parent, "Card_Skirmish", CardRect(2));
        AddHitZone(parent, "Top_Inbox", HeaderActionSlot(0));
        AddHitZone(parent, "Top_Settings", HeaderActionSlot(1));
        AddHitZone(parent, "CommanderPanel", CommanderRect());
        AddHitZone(parent, "DeployOperation", DeployRect());
    }

    private static void AddHitZone(Transform parent, string name, RectInt rect)
    {
        GameObject zone = CreateRectObject(name, parent);
        ApplyTopLeftRect(zone.GetComponent<RectTransform>(), ToArray(rect));
        Image image = zone.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        Button button = zone.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
        colors.highlightedColor = new Color(1f, 0.78f, 0.25f, 0.12f);
        colors.pressedColor = new Color(1f, 0.62f, 0.12f, 0.20f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private static RectInt HeaderLogoPanelRect() => new(0, 0, 930, 250);
    private static RectInt HeaderResourcePanelRect(int index)
    {
        int suppliesX = HeaderCommandPanelRect().x - 780;
        int creditsX = suppliesX - 780;
        return new RectInt(index == 0 ? creditsX : suppliesX, 0, 780, 250);
    }

    private static RectInt HeaderCommandPanelRect() => new(HeaderActionPanelRect().x - 690, 0, 690, 250);
    private static RectInt HeaderActionPanelRect() => new(s_LayoutWidth - 660, 0, 660, 250);
    private static RectInt HeaderResourceContentRect(int index) => index < 2 ? HeaderResourcePanelRect(index) : HeaderCommandPanelRect();

    private static RectInt HeaderActionSlot(int index)
    {
        RectInt panel = HeaderActionPanelRect();
        RectInt sourceSlot = index == 0
            ? new RectInt(42, 30, 154, 100)
            : new RectInt(221, 30, 150, 100);
        return SourceToCanvasRect(panel, 405, 162, sourceSlot);
    }

    private static RectInt NavRect(int index)
    {
        const int x = 18;
        const int y = 350;
        const int width = 552;
        const int height = 146;
        const int gap = 0;
        return new RectInt(x, y + index * (height + gap), width, height);
    }

    private static RectInt CardRect(int index)
    {
        const int x = 600;
        const int y = 560;
        const int width = 805;
        const int height = 1260;
        const int gap = 24;
        int layoutOffset = Mathf.Max(0, s_LayoutWidth - CanvasWidth) / 2;
        return new RectInt(x + layoutOffset + index * (width + gap), y, width, height);
    }

    private static RectInt CommanderRect() => new(s_LayoutWidth - 720, 335, 675, 1396);
    private static RectInt CommsRect() => new(65, 1930, 540, 134);
    private static RectInt DeployRect() => new(s_LayoutWidth - 1235, 1858, 1188, 250);

    private static Color TextMain => new Color32(235, 229, 207, 255);
    private static Color TextMuted => new Color32(210, 204, 184, 255);
    private static Color DisabledText => new Color32(118, 118, 102, 255);
    private static Color GoldText => new Color32(238, 172, 43, 255);

    private static Image AddImage(Transform parent, string name, string spriteName, int[] rect, bool preserveAspect, Color color)
    {
        Sprite sprite = LoadLayerSprite(spriteName);
        GameObject gameObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(gameObject.GetComponent<RectTransform>(), rect);
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        RegisterDiagnostic(name, new RectInt(rect[0], rect[1], rect[2], rect[3]), DiagnosticKind.Panel);
        return image;
    }

    private static Image AddFittedImage(Transform parent, string name, string spriteName, RectInt slot, int maxWidth, int maxHeight, Color color)
    {
        ImagePlacement placement = VisibleFittedPlacement(spriteName, slot, maxWidth, maxHeight);
        ValidateVisiblePlacement(name, slot, placement);
        RegisterDiagnostic($"{name}_Slot", slot, DiagnosticKind.Safe);
        RegisterDiagnostic($"{name}_Full", placement.FullRect, DiagnosticKind.Content);
        RegisterDiagnostic($"{name}_Visible", placement.VisibleRect, DiagnosticKind.Visible);
        return AddImage(parent, name, spriteName, ToArray(placement.FullRect), true, color);
    }

    private static void AddCoverImage(Transform parent, string name, string spriteName, RectInt slot, Color color)
    {
        Sprite sprite = LoadLayerSprite(spriteName);
        GameObject maskObject = CreateRectObject($"{name}_Viewport", parent);
        ApplyTopLeftRect(maskObject.GetComponent<RectTransform>(), ToArray(slot));
        maskObject.AddComponent<RectMask2D>();
        RegisterDiagnostic($"{name}_Viewport", slot, DiagnosticKind.Safe);

        float sourceW = Mathf.Max(1f, sprite.rect.width);
        float sourceH = Mathf.Max(1f, sprite.rect.height);
        float scale = Mathf.Max(slot.width / sourceW, slot.height / sourceH);
        int width = Mathf.CeilToInt(sourceW * scale);
        int height = Mathf.CeilToInt(sourceH * scale);

        GameObject imageObject = CreateRectObject(name, maskObject.transform);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static TMP_Text AddText(Transform parent, string name, string value, int[] rect, float size, TextAlignmentOptions alignment, Color color, bool wordWrap = false)
    {
        GameObject gameObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(gameObject.GetComponent<RectTransform>(), rect);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = wordWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RegisterDiagnostic($"{name}_TextRect", new RectInt(rect[0], rect[1], rect[2], rect[3]), DiagnosticKind.Text);
        return text;
    }

    private static ImagePlacement VisibleFittedPlacement(string spriteName, RectInt slot, int maxWidth, int maxHeight)
    {
        Sprite sprite = LoadLayerSprite(spriteName);
        float sourceWidth = Mathf.Max(1f, sprite.rect.width);
        float sourceHeight = Mathf.Max(1f, sprite.rect.height);
        RectInt visibleSource = GetVisibleAlphaBounds(spriteName, sprite);
        float scale = Mathf.Min(
            Mathf.Min(maxWidth, slot.width) / sourceWidth,
            Mathf.Min(maxHeight, slot.height) / sourceHeight);

        int fittedWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
        int fittedHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

        float sourceCenterX = sourceWidth * 0.5f;
        float sourceCenterY = sourceHeight * 0.5f;
        float visibleCenterX = visibleSource.x + visibleSource.width * 0.5f;
        float visibleCenterY = visibleSource.y + visibleSource.height * 0.5f;
        float visibleOffsetX = (visibleCenterX - sourceCenterX) * scale;
        float visibleOffsetY = (visibleCenterY - sourceCenterY) * scale;

        float fullCenterX = slot.center.x - visibleOffsetX;
        float fullCenterY = slot.center.y + visibleOffsetY;
        RectInt fullRect = new(
            Mathf.RoundToInt(fullCenterX - fittedWidth * 0.5f),
            Mathf.RoundToInt(fullCenterY - fittedHeight * 0.5f),
            fittedWidth,
            fittedHeight);
        RectInt visibleRect = new(
            fullRect.x + Mathf.RoundToInt(visibleSource.x * scale),
            fullRect.y + fittedHeight - Mathf.RoundToInt((visibleSource.y + visibleSource.height) * scale),
            Mathf.Max(1, Mathf.RoundToInt(visibleSource.width * scale)),
            Mathf.Max(1, Mathf.RoundToInt(visibleSource.height * scale)));
        return new ImagePlacement(fullRect, visibleRect);
    }

    private static RectInt GetVisibleAlphaBounds(string spriteName, Sprite sprite)
    {
        if (s_VisibleBoundsCache.TryGetValue(spriteName, out RectInt cached))
            return cached;

        Texture2D texture = sprite.texture;
        Rect source = sprite.rect;
        int startX = Mathf.RoundToInt(source.x);
        int startY = Mathf.RoundToInt(source.y);
        int width = Mathf.RoundToInt(source.width);
        int height = Mathf.RoundToInt(source.height);
        Color32[] pixels = texture.GetPixels32();
        int textureWidth = texture.width;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            int row = (startY + y) * textureWidth;
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[row + startX + x];
                if (pixel.a <= 8)
                    continue;

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        RectInt bounds = maxX < minX || maxY < minY
            ? new RectInt(0, 0, width, height)
            : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        s_VisibleBoundsCache[spriteName] = bounds;
        return bounds;
    }

    private static Sprite LoadLayerSprite(string spriteName)
    {
        string assetPath = $"{LayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new FileNotFoundException($"Missing SCN-02 V15C layered sprite: {assetPath}");
        return sprite;
    }

    private static void ValidateVisiblePlacement(string name, RectInt slot, ImagePlacement placement)
    {
        List<string> failures = new();
        if (!Contains(slot, placement.VisibleRect))
            failures.Add($"visible={placement.VisibleRect} is outside slot={slot}");

        Vector2 slotCenter = slot.center;
        Vector2 visibleCenter = placement.VisibleRect.center;
        if (Mathf.Abs(slotCenter.x - visibleCenter.x) > VisibleCenterTolerance)
            failures.Add($"visible center x={visibleCenter.x:0.0} expected={slotCenter.x:0.0}");
        if (Mathf.Abs(slotCenter.y - visibleCenter.y) > VisibleCenterTolerance)
            failures.Add($"visible center y={visibleCenter.y:0.0} expected={slotCenter.y:0.0}");

        if (failures.Count > 0)
            throw new InvalidOperationException($"SCN-02 V15C image placement invalid for {name}: {string.Join("; ", failures)}");
    }

    private static void ValidateSectionContent(string sectionName, RectInt safeRect, params LayoutRect[] items)
    {
        List<string> failures = new();
        foreach (LayoutRect item in items)
        {
            if (!Contains(safeRect, item.Rect))
                failures.Add($"{item.Name} rect={item.Rect} is outside safe={safeRect}");
        }

        for (int i = 0; i < items.Length; i++)
        {
            for (int j = i + 1; j < items.Length; j++)
            {
                if (Intersects(items[i].Rect, items[j].Rect))
                    failures.Add($"{items[i].Name} rect={items[i].Rect} overlaps {items[j].Name} rect={items[j].Rect}");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException($"SCN-02 V15C layout invalid in {sectionName}: {string.Join("; ", failures)}");
    }

    private static void ValidateMajorPanels()
    {
        ValidateSelectedNavModel();
        RectInt canvasSafe = new(0, 0, s_LayoutWidth, CanvasHeight);
        LayoutRect[] panels =
        {
            new("HeaderLogo", HeaderLogoPanelRect()),
            new("HeaderCredits", HeaderResourcePanelRect(0)),
            new("HeaderSupplies", HeaderResourcePanelRect(1)),
            new("HeaderCommand", HeaderCommandPanelRect()),
            new("HeaderActions", HeaderActionPanelRect()),
            new("NavCampaign", NavRect(0)),
            new("NavOperations", NavRect(1)),
            new("NavSkirmish", NavRect(2)),
            new("NavStore", NavRect(3)),
            new("NavCommander", NavRect(4)),
            new("NavSettings", NavRect(5)),
            new("CardCampaign", CardRect(0)),
            new("CardOperations", CardRect(1)),
            new("CardSkirmish", CardRect(2)),
            new("CommanderPanel", CommanderRect()),
            new("CommsOnline", CommsRect()),
            new("DeployOperation", DeployRect())
        };

        for (int i = 0; i < panels.Length; i++)
        {
            RegisterDiagnostic(panels[i].Name, panels[i].Rect, DiagnosticKind.Target);
            if (!Contains(canvasSafe, panels[i].Rect))
                throw new InvalidOperationException($"SCN-02 V15C major panel {panels[i].Name} violates canvas safe edge: {panels[i].Rect}");

            for (int j = i + 1; j < panels.Length; j++)
            {
                if (!IsAllowedPanelTouch(panels[i].Name, panels[j].Name) && Intersects(panels[i].Rect, panels[j].Rect))
                    throw new InvalidOperationException($"SCN-02 V15C major panel overlap: {panels[i].Name} {panels[i].Rect} overlaps {panels[j].Name} {panels[j].Rect}");
            }
        }
    }

    private static bool IsAllowedPanelTouch(string left, string right)
    {
        return left.StartsWith("Header", StringComparison.Ordinal) && right.StartsWith("Header", StringComparison.Ordinal);
    }

    private static void ValidateSelectedNavModel()
    {
        if (s_NavItems.Length == 0)
            throw new InvalidOperationException("SCN-02 V15C nav model is empty.");
        if (DefaultSelectedNavIndex < 0 || DefaultSelectedNavIndex >= s_NavItems.Length)
            throw new InvalidOperationException("SCN-02 V15C selected nav index is outside the nav model.");
        if (!string.Equals(s_NavItems[DefaultSelectedNavIndex].Label, "Campaign", StringComparison.Ordinal))
            throw new InvalidOperationException($"SCN-02 V15C default selected nav must be Campaign, got {s_NavItems[DefaultSelectedNavIndex].Label}.");
    }

    private static RectInt Inset(RectInt rect, int x, int y) => new(rect.x + x, rect.y + y, rect.width - x * 2, rect.height - y * 2);
    private static int[] ToArray(RectInt rect) => new[] { rect.x, rect.y, rect.width, rect.height };

    private static RectInt SourceToCanvasRect(RectInt canvasRect, int sourceWidth, int sourceHeight, RectInt sourceRect)
    {
        float scaleX = canvasRect.width / (float)sourceWidth;
        float scaleY = canvasRect.height / (float)sourceHeight;
        return new RectInt(
            canvasRect.x + Mathf.RoundToInt(sourceRect.x * scaleX),
            canvasRect.y + Mathf.RoundToInt(sourceRect.y * scaleY),
            Mathf.RoundToInt(sourceRect.width * scaleX),
            Mathf.RoundToInt(sourceRect.height * scaleY));
    }

    private static bool Contains(RectInt outer, RectInt inner)
    {
        return inner.xMin >= outer.xMin
            && inner.yMin >= outer.yMin
            && inner.xMax <= outer.xMax
            && inner.yMax <= outer.yMax;
    }

    private static bool Intersects(RectInt left, RectInt right)
    {
        return left.xMin < right.xMax
            && left.xMax > right.xMin
            && left.yMin < right.yMax
            && left.yMax > right.yMin;
    }

    private static void RegisterDiagnostic(string name, RectInt rect, DiagnosticKind kind)
    {
        s_Diagnostics.Add(new DiagnosticRect(name, rect, kind));
    }

    private static void WriteDiagnosticOverlay(string sourcePath, string outputPath, int width, int height)
    {
        if (!File.Exists(sourcePath))
            return;

        byte[] bytes = File.ReadAllBytes(sourcePath);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
            return;

        foreach (DiagnosticRect diagnostic in s_Diagnostics)
            DrawRect(texture, diagnostic.Rect, DiagnosticColor(diagnostic.Kind));

        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        EnsureParentFolder(outputPath);
        File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
    }

    private static Color32 DiagnosticColor(DiagnosticKind kind)
    {
        return kind switch
        {
            DiagnosticKind.Panel => new Color32(255, 196, 0, 255),
            DiagnosticKind.Safe => new Color32(0, 220, 255, 255),
            DiagnosticKind.Content => new Color32(255, 255, 255, 255),
            DiagnosticKind.Visible => new Color32(0, 255, 80, 255),
            DiagnosticKind.Text => new Color32(255, 120, 0, 255),
            DiagnosticKind.Target => new Color32(120, 140, 255, 160),
            _ => new Color32(255, 255, 255, 255)
        };
    }

    private static void DrawRect(Texture2D texture, RectInt rect, Color32 color)
    {
        int xMin = Mathf.Clamp(rect.xMin, 0, texture.width - 1);
        int xMax = Mathf.Clamp(rect.xMax - 1, 0, texture.width - 1);
        int yMin = Mathf.Clamp(rect.yMin, 0, texture.height - 1);
        int yMax = Mathf.Clamp(rect.yMax - 1, 0, texture.height - 1);

        for (int x = xMin; x <= xMax; x++)
        {
            BlendTopLeftPixel(texture, x, yMin, color);
            BlendTopLeftPixel(texture, x, yMax, color);
        }

        for (int y = yMin; y <= yMax; y++)
        {
            BlendTopLeftPixel(texture, xMin, y, color);
            BlendTopLeftPixel(texture, xMax, y, color);
        }
    }

    private static void BlendTopLeftPixel(Texture2D texture, int x, int topLeftY, Color32 color)
    {
        int y = texture.height - 1 - topLeftY;
        Color32 baseColor = texture.GetPixel(x, y);
        float alpha = color.a / 255f;
        texture.SetPixel(
            x,
            y,
            new Color32(
                (byte)Mathf.RoundToInt(baseColor.r * (1f - alpha) + color.r * alpha),
                (byte)Mathf.RoundToInt(baseColor.g * (1f - alpha) + color.g * alpha),
                (byte)Mathf.RoundToInt(baseColor.b * (1f - alpha) + color.b * alpha),
                255));
    }

    private static void EnsureLayerSpriteImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { LayerRoot });
        if (guids.Length == 0)
            throw new FileNotFoundException($"No V15C layer textures found under {LayerRoot}");

        foreach (string guid in guids)
            EnsureSpriteImport(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void EnsureSpriteImport(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    private static void AddEventSystem()
    {
        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void AddSceneCamera()
    {
        GameObject cameraObject = new("UICamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = CanvasHeight * 0.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
        }
    }

    private static void CapturePrefab(string prefabPath, string outputPath, int width, int height, Color backgroundColor, int layoutWidth)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            throw new InvalidOperationException($"Cannot capture {prefabPath} while Unity is running with NullGfxDevice.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"UI prefab not found at {prefabPath}");

        EnsureParentFolder(outputPath);

        RenderTexture renderTexture = null;
        Texture2D screenshot = null;
        GameObject cameraObject = null;
        GameObject canvasObject = null;
        GameObject instance = null;
        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            renderTexture.Create();

            cameraObject = new GameObject("SCN02V15CMainMenuCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = CanvasHeight * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.targetTexture = renderTexture;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            canvasObject = CreateRectObject("SCN02V15CMainMenuCaptureRoot", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.sizeDelta = new Vector2(layoutWidth, CanvasHeight);
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localScale = Vector3.one;

            instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform);
            instance.name = prefab.name;
            RectTransform instanceTransform = (RectTransform)instance.transform;
            instanceTransform.anchorMin = new Vector2(0.5f, 0.5f);
            instanceTransform.anchorMax = new Vector2(0.5f, 0.5f);
            instanceTransform.pivot = new Vector2(0.5f, 0.5f);
            instanceTransform.anchoredPosition = Vector2.zero;
            instanceTransform.sizeDelta = new Vector2(layoutWidth, CanvasHeight);
            instanceTransform.localScale = Vector3.one;

            Canvas.ForceUpdateCanvases();
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, backgroundColor);
            camera.Render();

            screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
            Camera camera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
            if (camera != null)
                camera.targetTexture = null;
            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            if (canvasObject != null)
                UnityEngine.Object.DestroyImmediate(canvasObject);
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplyTopLeftRect(RectTransform rect, int[] topLeftRect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(topLeftRect[0], -topLeftRect[1]);
        rect.sizeDelta = new Vector2(topLeftRect[2], topLeftRect[3]);
    }

    private static void EnsureParentFolder(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(folder))
            return;

        if (!folder.StartsWith("Assets/", StringComparison.Ordinal) && !string.Equals(folder, "Assets", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(folder);
            return;
        }

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            EnsureFolder(current, parts[i]);
            current += "/" + parts[i];
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private readonly struct NavItem
    {
        public NavItem(string label, string icon)
        {
            Label = label;
            Icon = icon;
        }

        public string Label { get; }
        public string Icon { get; }
    }

    private readonly struct LayoutRect
    {
        public LayoutRect(string name, RectInt rect)
        {
            Name = name;
            Rect = rect;
        }

        public string Name { get; }
        public RectInt Rect { get; }
    }

    private readonly struct ImagePlacement
    {
        public ImagePlacement(RectInt fullRect, RectInt visibleRect)
        {
            FullRect = fullRect;
            VisibleRect = visibleRect;
        }

        public RectInt FullRect { get; }
        public RectInt VisibleRect { get; }
    }

    private readonly struct DiagnosticRect
    {
        public DiagnosticRect(string name, RectInt rect, DiagnosticKind kind)
        {
            Name = name;
            Rect = rect;
            Kind = kind;
        }

        public string Name { get; }
        public RectInt Rect { get; }
        public DiagnosticKind Kind { get; }
    }

    private enum DiagnosticKind
    {
        Panel,
        Safe,
        Content,
        Visible,
        Text,
        Target
    }
}
#endif
