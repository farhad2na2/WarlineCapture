#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class WarlineCaptureMainMenuAltSceneBuilder
{
    private const int CanvasWidth = 3840;
    private const int CanvasHeight = 2160;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/MainMenuAlt/LayeredOneGo";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_SyntyCommandTarget.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN02B_MainMenu_SyntyCommandTarget.unity";
    private const string CapturePath = "Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_v15_3840x2160.png";
    private const string CapturePath20x9 = "Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_v15_2400x1080.png";
    private const string DiagnosticOverlayPath = "Design/AgentReports/Captures/MainMenuAlt/SCN02B_MainMenu_SyntyCommandTarget_v15_diagnostics_3840x2160.png";
    private const int SectionPadding = 24;
    private const int HeaderCenterY = 128;
    private const int DefaultSelectedNavIndex = 0;
    private const float VisibleCenterTolerance = 2f;
    private static readonly List<DiagnosticRect> s_Diagnostics = new();
    private static readonly Dictionary<string, RectInt> s_VisibleBoundsCache = new();
    private static readonly NavItem[] s_NavItems =
    {
        new("Campaign", "icon_target_reticle.png"),
        new("Operations", "icon_target_reticle.png"),
        new("Skirmish", "icon_crossed_swords.png"),
        new("Store", "icon_store_cart.png"),
        new("Commander", "icon_commander_person.png"),
        new("Settings", "icon_settings_gear.png")
    };

    [MenuItem("WarlineCapture/Design/SCN-02B/Build Synty Command Main Menu Target Scene")]
    public static void BuildScene()
    {
        EnsureLayerSpriteImports();
        s_Diagnostics.Clear();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = BuildCanvasPrefabRoot();

        EnsureFolder("Assets/Game/Prefabs/UI", "Screens");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        GameObject sceneCanvas = CreateRectObject("SCN02B_MainMenuSceneCanvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = sceneCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject sceneRoot = UnityEngine.Object.Instantiate(root, sceneCanvas.transform);
        sceneRoot.name = "Screen_MainMenu_SyntyCommandTarget";
        StretchToParent(sceneRoot.GetComponent<RectTransform>());
        UnityEngine.Object.DestroyImmediate(root);

        AddEventSystem();
        AddSceneCamera();

        EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SCN-02B] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-02B/Capture Synty Command Main Menu Target Scene")]
    public static void CaptureScene()
    {
        BuildScene();
        CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, Color.black);
        WriteDiagnosticOverlay(CapturePath, DiagnosticOverlayPath, CanvasWidth, CanvasHeight);
        Debug.Log($"[SCN-02B] Captured {CapturePath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-02B/Capture Synty Command Main Menu Target 20x9")]
    public static void CaptureScene20x9()
    {
        BuildScene();
        CapturePrefab(PrefabPath, CapturePath20x9, 2400, 1080, Color.black);
        Debug.Log($"[SCN-02B] Captured {CapturePath20x9}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = CreateRectObject("Screen_MainMenu_SyntyCommandTarget", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenController screenController = root.AddComponent<WarlineCaptureScreenController>();
        screenController.SetRouteForTests(WarlineCaptureRoute.MainMenu);

        GameObject artRoot = CreateRectObject("MainMenuAltLayeredCanvas", root.transform);
        StretchToParent(artRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(artRoot.transform);

        GameObject hitRoot = CreateRectObject("MainMenuAltHitZones", root.transform);
        StretchToParent(hitRoot.GetComponent<RectTransform>());

        for (int i = 0; i < s_NavItems.Length; i++)
            AddHitZone(hitRoot.transform, $"Nav_{s_NavItems[i].Label}", ToArray(NavRect(i)));
        AddHitZone(hitRoot.transform, "Card_Campaign", ToArray(CardRect(0)));
        AddHitZone(hitRoot.transform, "Card_Operations", ToArray(CardRect(1)));
        AddHitZone(hitRoot.transform, "Card_Skirmish", ToArray(CardRect(2)));
        AddHitZone(hitRoot.transform, "Top_Inbox", ToArray(HeaderActionSlot(0)));
        AddHitZone(hitRoot.transform, "Top_Settings", ToArray(HeaderActionSlot(1)));
        AddHitZone(hitRoot.transform, "CommanderPanel", ToArray(CommanderRect()));
        AddHitZone(hitRoot.transform, "DeployOperation", ToArray(DeployRect()));

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        AddImage(parent, "Background_CommandTent", "background_command_tent.png", new[] { 0, 0, CanvasWidth, CanvasHeight }, false, Color.white);
        AddImage(parent, "Header_Frame", "header_bar_frame.png", new[] { 850, 42, 2870, 172 }, false, Color.white);
        AddImage(parent, "Logo_WarlineCapture", "logo_warline_capture.png", new[] { 78, 56, 610, 154 }, true, Color.white);

        AddHeaderResource(parent, "Credits", "icon_credits_coin.png", "Credits", "187,540", HeaderResourceSlot(0), new Color32(255, 190, 75, 255));
        AddHeaderResource(parent, "Supplies", "icon_supplies_crate.png", "Supplies", "92,860", HeaderResourceSlot(1), new Color32(168, 176, 117, 255));
        AddHeaderResource(parent, "Command", "icon_command_badge.png", "Command", "2,715", HeaderResourceSlot(2), new Color32(128, 185, 220, 255));

        AddHeaderAction(parent, "Top_InboxIcon", "icon_inbox_envelope.png", HeaderActionSlot(0), 64, 64);
        AddHeaderAction(parent, "Top_SettingsIcon", "icon_settings_gear.png", HeaderActionSlot(1), 66, 66);

        AddNavButtons(parent);

        AddCommsPanel(parent);

        AddModeCard(parent, "Campaign", "icon_logo_emblem_missing", "icon_map_book.png", "card_art_campaign_outpost.png", CardRect(0), new Color32(189, 151, 50, 255));
        AddModeCard(parent, "Operations", "icon_target_reticle.png", "icon_folder.png", "card_art_operations_hologram.png", CardRect(1), new Color32(98, 185, 190, 255));
        AddModeCard(parent, "Skirmish", "icon_crossed_swords.png", "icon_lightning.png", "card_art_skirmish_base.png", CardRect(2), new Color32(156, 165, 74, 255));

        AddCommanderPanel(parent);
        AddDeployButton(parent);
        ValidateMajorPanelGaps();
    }

    private static void AddHeaderResource(Transform parent, string name, string icon, string label, string value, RectInt slot, Color valueColor)
    {
        RectInt safeSlot = Inset(slot, 34, 10);
        int iconSize = 76;
        int gap = 40;
        int textWidth = Mathf.Min(250, safeSlot.width - iconSize - gap);
        int clusterWidth = iconSize + gap + textWidth;
        int clusterX = safeSlot.x + (safeSlot.width - clusterWidth) / 2;
        int clusterY = safeSlot.y + (safeSlot.height - 88) / 2;
        RectInt iconSlot = new(clusterX, safeSlot.y + (safeSlot.height - iconSize) / 2, iconSize, iconSize);
        RectInt labelSlot = new(clusterX + iconSize + gap, clusterY, textWidth, 36);
        RectInt valueSlot = new(clusterX + iconSize + gap, clusterY + 40, textWidth, 50);
        ImagePlacement iconPlacement = VisibleFittedPlacement(icon, iconSlot, iconSize, iconSize);

        ValidateSectionContent(
            $"Header_{name}",
            safeSlot,
            new LayoutRect($"{name}_Icon", iconPlacement.VisibleRect),
            new LayoutRect($"{name}_Label", labelSlot),
            new LayoutRect($"{name}_Value", valueSlot));

        AddFittedImage(parent, $"{name}_Icon", icon, iconSlot, iconSize, iconSize, Color.white);
        AddText(parent, $"{name}_Label", label, ToArray(labelSlot), 26f, TextAlignmentOptions.Left, new Color32(210, 203, 185, 255));
        AddText(parent, $"{name}_Value", value, ToArray(valueSlot), 37f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddHeaderAction(Transform parent, string name, string icon, RectInt slot, int width, int height)
    {
        RectInt safeSlot = Inset(slot, 12, 10);
        ImagePlacement iconPlacement = VisibleFittedPlacement(icon, safeSlot, width, height);
        ValidateSectionContent($"Header_{name}", safeSlot, new LayoutRect(name, iconPlacement.VisibleRect));
        ValidateCenterline($"Header_{name}", iconPlacement.VisibleRect, HeaderCenterY, 2);
        AddFittedImage(parent, name, icon, safeSlot, width, height, new Color32(224, 216, 190, 255));
    }

    private static void AddNavButtons(Transform parent)
    {
        ValidateSelectedNavModel();
        for (int i = 0; i < s_NavItems.Length; i++)
            AddNavButton(parent, i, s_NavItems[i], NavRect(i));
    }

    private static void AddNavButton(Transform parent, int index, NavItem item, RectInt rect)
    {
        bool selected = index == DefaultSelectedNavIndex;
        string label = item.Label;
        string icon = item.Icon;
        string background = selected ? "nav_button_selected_background.png" : "nav_button_normal_background.png";
        AddImage(parent, $"Nav_{label}_Background", background, ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 48, 34);
        RectInt iconSlot = new(safe.x + 20, safe.y, 118, safe.height);
        ImagePlacement iconPlacement = VisibleFittedPlacement(icon, iconSlot, 100, 100);
        RectInt textSlot = new(safe.x + 158, safe.y + (safe.height - 70) / 2, safe.width - 170, 70);

        ValidateSectionContent(
            $"Nav_{label}",
            safe,
            new LayoutRect($"{label}_Icon", iconPlacement.VisibleRect),
            new LayoutRect($"{label}_Text", textSlot));

        Color contentColor = selected ? new Color32(246, 239, 202, 255) : new Color32(229, 223, 205, 255);
        AddFittedImage(parent, $"Nav_{label}_Icon", icon, iconSlot, 100, 100, contentColor);
        AddText(parent, $"Nav_{label}_Text", label, ToArray(textSlot), 42f, TextAlignmentOptions.Left, contentColor);
    }

    private static void AddCommsPanel(Transform parent)
    {
        RectInt rect = CommsRect();
        AddImage(parent, "CommsOnline_Background", "comms_online_panel_background.png", ToArray(rect), false, Color.white);
        RectInt safe = Inset(rect, 34, 22);
        RectInt iconSlot = new(safe.x + 12, safe.y, 58, safe.height);
        RectInt textSlot = new(safe.x + 100, safe.y + (safe.height - 50) / 2, safe.width - 120, 50);
        ValidateSectionContent(
            "CommsOnline",
            safe,
            new LayoutRect("CommsIcon", VisibleFittedPlacement("icon_comms_radio.png", iconSlot, 54, 54).VisibleRect),
            new LayoutRect("CommsText", textSlot));
        AddFittedImage(parent, "CommsOnline_Icon", "icon_comms_radio.png", iconSlot, 54, 54, new Color32(150, 176, 54, 255));
        AddText(parent, "CommsOnline_Text", "COMMS ONLINE", ToArray(textSlot), 28f, TextAlignmentOptions.Left, new Color32(145, 185, 45, 255));
    }

    private static void AddModeCard(Transform parent, string title, string titleIcon, string footerIcon, string art, RectInt rect, Color progressColor)
    {
        AddImage(parent, $"Card_{title}_Frame", "center_card_frame_wide.png", ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 46, 48);
        RectInt titleBand = new(safe.x + 20, safe.y + 8, safe.width - 40, 94);
        RectInt titleIconSlot = new(titleBand.x, titleBand.y + 8, 80, 80);
        RectInt titleSlot = new(titleBand.x + 108, titleBand.y + 10, titleBand.width - 120, 72);
        RectInt artSlot = new(safe.x + 16, safe.y + 136, safe.width - 32, 390);
        RectInt footerBand = new(safe.x + 20, safe.y + safe.height - 140, safe.width - 40, 86);
        RectInt footerIconSlot = new(footerBand.x + 36, footerBand.y + 6, 74, 74);
        RectInt progressSlot = new(footerBand.x + 160, footerBand.y + 30, footerBand.width - 215, 28);

        ValidateSectionContent(
            $"Card_{title}",
            safe,
            new LayoutRect($"{title}_TitleIcon", VisibleFittedPlacement(titleIcon == "icon_logo_emblem_missing" ? "logo_emblem.png" : titleIcon, titleIconSlot, 70, 70).VisibleRect),
            new LayoutRect($"{title}_Title", titleSlot),
            new LayoutRect($"{title}_FooterIcon", VisibleFittedPlacement(footerIcon, footerIconSlot, 62, 62).VisibleRect),
            new LayoutRect($"{title}_Progress", progressSlot));

        string resolvedTitleIcon = titleIcon == "icon_logo_emblem_missing" ? "logo_emblem.png" : titleIcon;
        AddFittedImage(parent, $"Card_{title}_TitleIcon", resolvedTitleIcon, titleIconSlot, 70, 70, new Color32(198, 194, 160, 255));
        AddText(parent, $"Card_{title}_Title", title.ToUpperInvariant(), ToArray(titleSlot), 40f, TextAlignmentOptions.Left, new Color32(236, 229, 208, 255));
        AddImage(parent, $"Card_{title}_Art", art, ToArray(artSlot), false, Color.white);
        AddFittedImage(parent, $"Card_{title}_FooterIcon", footerIcon, footerIconSlot, 62, 62, new Color32(190, 184, 140, 255));
        AddProgress(parent, $"Card_{title}_Progress", progressSlot.x, progressSlot.y, progressSlot.width, progressSlot.height, progressColor);
    }

    private static void AddCommanderPanel(Transform parent)
    {
        RectInt rect = CommanderRect();
        AddImage(parent, "CommanderPanel_Frame", "commander_panel_frame.png", ToArray(rect), false, Color.white);

        RectInt safe = Inset(rect, 54, 52);
        RectInt titleSlot = new(safe.x, safe.y + 8, safe.width, 66);
        RectInt portraitSlot = new(safe.x + 58, safe.y + 134, safe.width - 116, 350);
        RectInt identitySlot = new(safe.x + 16, safe.y + 552, safe.width - 32, 128);
        RectInt readinessSlot = new(safe.x + 16, safe.y + 740, safe.width - 32, 128);
        RectInt rowOneSlot = new(safe.x + 16, safe.y + 928, safe.width - 32, 104);
        RectInt rowTwoSlot = new(safe.x + 16, safe.y + 1060, safe.width - 32, 104);

        ValidateSectionContent(
            "CommanderPanel",
            safe,
            new LayoutRect("CommanderTitle", titleSlot),
            new LayoutRect("CommanderPortrait", Inset(portraitSlot, 8, 10)),
            new LayoutRect("CommanderIdentity", identitySlot),
            new LayoutRect("CommanderReadiness", readinessSlot),
            new LayoutRect("SquadManagement", rowOneSlot),
            new LayoutRect("IntelReport", rowTwoSlot));

        AddText(parent, "CommanderPanel_Title", "COMMANDER", ToArray(titleSlot), 40f, TextAlignmentOptions.Center, new Color32(232, 225, 204, 255));
        AddImage(parent, "CommanderPanel_PortraitFrame", "profile_portrait_frame.png", ToArray(portraitSlot), false, Color.white);
        AddImage(parent, "CommanderPanel_Portrait", "profile_commander_silhouette.png", ToArray(Inset(portraitSlot, 28, 28)), false, Color.white);
        AddCommanderIdentity(parent, identitySlot);
        AddCommanderReadiness(parent, readinessSlot);
        AddLockedRow(parent, "SquadManagement", "SQUAD MANAGEMENT\nLOCKED", rowOneSlot);
        AddLockedRow(parent, "IntelReport", "INTEL REPORT\nLOCKED", rowTwoSlot);
    }

    private static void AddCommanderIdentity(Transform parent, RectInt slot)
    {
        RectInt badgeSlot = new(slot.x + 8, slot.y + 10, 116, slot.height - 20);
        RectInt nameSlot = new(slot.x + 146, slot.y + 26, slot.width - 168, 42);
        RectInt levelSlot = new(slot.x + 146, slot.y + 76, slot.width - 168, 44);
        AddFittedImage(parent, "CommanderPanel_Badge", "icon_command_badge.png", badgeSlot, 96, 96, Color.white);
        AddText(parent, "CommanderPanel_Name", "FIELD COMMANDER", ToArray(nameSlot), 26f, TextAlignmentOptions.Left, new Color32(210, 203, 185, 255));
        AddText(parent, "CommanderPanel_Level", "LEVEL 38", ToArray(levelSlot), 29f, TextAlignmentOptions.Left, new Color32(238, 174, 48, 255));
    }

    private static void AddCommanderReadiness(Transform parent, RectInt slot)
    {
        RectInt labelSlot = new(slot.x, slot.y + 8, slot.width, 46);
        RectInt pipsSlot = new(slot.x + 8, slot.y + 74, slot.width - 16, 48);
        AddText(parent, "CommanderPanel_ReadinessLabel", "READINESS", ToArray(labelSlot), 29f, TextAlignmentOptions.Left, new Color32(210, 203, 185, 255));
        AddFittedImage(parent, "CommanderPanel_ReadinessPips", "icon_readiness_pips.png", pipsSlot, 390, 42, new Color32(162, 168, 70, 255));
    }

    private static void AddLockedRow(Transform parent, string name, string text, RectInt slot)
    {
        AddImage(parent, $"{name}_Background", "locked_row_background.png", ToArray(slot), false, Color.white);
        AddFittedImage(parent, $"{name}_Icon", "icon_lock.png", new RectInt(slot.x + 30, slot.y + 20, 82, slot.height - 40), 52, 52, new Color32(120, 120, 105, 255));
        AddText(parent, $"{name}_Text", text, new[] { slot.x + 132, slot.y + 20, slot.width - 190, slot.height - 36 }, 24f, TextAlignmentOptions.Left, new Color32(120, 120, 105, 255));
    }

    private static void AddDeployButton(Transform parent)
    {
        RectInt rect = DeployRect();
        AddImage(parent, "DeployOperation_Background", "deploy_button_background.png", ToArray(rect), false, Color.white);
        RectInt safe = Inset(rect, 96, 40);
        RectInt textRect = new(safe.x + 18, safe.y + (safe.height - 86) / 2, safe.width - 190, 86);
        ValidateSectionContent("DeployOperation", safe, new LayoutRect("DeployOperationText", textRect));
        AddText(parent, "DeployOperation_Text", "DEPLOY OPERATION", ToArray(textRect), 56f, TextAlignmentOptions.Center, Color.black);
    }

    private static void AddProgress(Transform parent, string name, int x, int y, int width, int height, Color fillColor)
    {
        GameObject track = CreateRectObject($"{name}_Track", parent);
        ApplyTopLeftRect(track.GetComponent<RectTransform>(), new[] { x, y, width, height });
        Image trackImage = track.AddComponent<Image>();
        trackImage.color = new Color32(57, 59, 47, 210);
        trackImage.raycastTarget = false;

        GameObject fill = CreateRectObject($"{name}_Fill", parent);
        ApplyTopLeftRect(fill.GetComponent<RectTransform>(), new[] { x, y, Mathf.RoundToInt(width * 0.52f), height });
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;
    }

    private static void AddHitZone(Transform parent, string name, int[] rect)
    {
        GameObject zone = CreateRectObject(name, parent);
        ApplyTopLeftRect(zone.GetComponent<RectTransform>(), rect);
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
        return image;
    }

    private static Image AddCenteredImage(Transform parent, string name, string spriteName, RectInt slot, int width, int height, Color color)
    {
        RectInt rect = CenteredRect(slot, width, height);
        return AddImage(parent, name, spriteName, ToArray(rect), true, color);
    }

    private static Image AddFittedImage(Transform parent, string name, string spriteName, RectInt slot, int maxWidth, int maxHeight, Color color)
    {
        ImagePlacement placement = VisibleFittedPlacement(spriteName, slot, maxWidth, maxHeight);
        ValidateVisiblePlacement(name, slot, placement);
        RegisterDiagnostic($"{name}_Slot", slot, DiagnosticKind.Safe);
        RegisterDiagnostic($"{name}_Full", placement.FullRect, DiagnosticKind.Content);
        RegisterDiagnostic($"{name}_Visible", placement.VisibleRect, DiagnosticKind.Visible);
        RegisterCenterline($"{name}_CenterX", Mathf.RoundToInt(slot.center.x), true);
        RegisterCenterline($"{name}_CenterY", Mathf.RoundToInt(slot.center.y), false);
        return AddImage(parent, name, spriteName, ToArray(placement.FullRect), true, color);
    }

    private static RectInt FittedRect(string spriteName, RectInt slot, int maxWidth, int maxHeight)
    {
        return VisibleFittedPlacement(spriteName, slot, maxWidth, maxHeight).FullRect;
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
            int pixelY = startY + y;
            int row = pixelY * textureWidth;
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

    private static RectInt NavRect(int index)
    {
        RectInt zone = LeftRailZone();
        const int height = 210;
        const int gap = 14;
        return new RectInt(zone.x + 4, zone.y + index * (height + gap), zone.width - 8, height);
    }

    private static RectInt CardRect(int index)
    {
        RectInt zone = CenterCardsZone();
        const int gap = 58;
        int width = (zone.width - gap * 2) / 3;
        return new RectInt(zone.x + index * (width + gap), zone.y, width, zone.height);
    }

    private static RectInt CommanderRect()
    {
        return FitInsideZone(new RectInt(3130, 350, 650, 1360), RightRailZone());
    }

    private static RectInt DeployRect()
    {
        return FitInsideZone(new RectInt(2630, 1810, 1180, 245), BottomCtaZone());
    }

    private static RectInt HeaderResourceSlot(int index)
    {
        RectInt zone = HeaderResourceZone();
        const int gap = 34;
        int width = (zone.width - gap * 2) / 3;
        return new RectInt(zone.x + index * (width + gap), zone.y, width, zone.height);
    }

    private static RectInt HeaderActionSlot(int index)
    {
        RectInt zone = HeaderActionZone();
        const int gap = 26;
        int width = (zone.width - gap) / 2;
        return new RectInt(zone.x + index * (width + gap), zone.y, width, zone.height);
    }

    private static RectInt CommsRect()
    {
        return new RectInt(70, 1928, 520, 118);
    }

    private static RectInt LeftRailZone()
    {
        return new RectInt(16, 340, 575, 1330);
    }

    private static RectInt HeaderResourceZone()
    {
        return new RectInt(1000, 66, 2170, 112);
    }

    private static RectInt HeaderActionZone()
    {
        return new RectInt(3376, 72, 302, 112);
    }

    private static RectInt CenterCardsZone()
    {
        return new RectInt(715, 790, 2040, 840);
    }

    private static RectInt RightRailZone()
    {
        return new RectInt(3100, 335, 690, 1400);
    }

    private static RectInt BottomCtaZone()
    {
        return new RectInt(2585, 1785, 1235, 275);
    }

    private static RectInt FitInsideZone(RectInt rect, RectInt zone)
    {
        int width = Mathf.Min(rect.width, zone.width);
        int height = Mathf.Min(rect.height, zone.height);
        int x = Mathf.Clamp(rect.x, zone.xMin, zone.xMax - width);
        int y = Mathf.Clamp(rect.y, zone.yMin, zone.yMax - height);
        return new RectInt(x, y, width, height);
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
            throw new InvalidOperationException($"SCN-02B image placement invalid for {name}: {string.Join("; ", failures)}");
    }

    private static TMP_Text AddText(Transform parent, string name, string value, int[] rect, float size, TextAlignmentOptions alignment, Color color)
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
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RegisterDiagnostic($"{name}_TextRect", new RectInt(rect[0], rect[1], rect[2], rect[3]), DiagnosticKind.Text);
        return text;
    }

    private static Sprite LoadLayerSprite(string spriteName)
    {
        string assetPath = $"{LayerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new FileNotFoundException($"Missing SCN-02B layered sprite: {assetPath}");
        return sprite;
    }

    private static RectInt Inset(RectInt rect, int x, int y)
    {
        return new RectInt(rect.x + x, rect.y + y, rect.width - x * 2, rect.height - y * 2);
    }

    private static RectInt CenteredRect(RectInt slot, int width, int height)
    {
        int clampedWidth = Mathf.Min(width, slot.width);
        int clampedHeight = Mathf.Min(height, slot.height);
        return new RectInt(
            slot.x + (slot.width - clampedWidth) / 2,
            slot.y + (slot.height - clampedHeight) / 2,
            clampedWidth,
            clampedHeight);
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
            throw new InvalidOperationException($"SCN-02B layout invalid in {sectionName}: {string.Join("; ", failures)}");
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

    private static void ValidateCenterline(string sectionName, RectInt item, int centerY, int tolerance)
    {
        int itemCenterY = item.y + item.height / 2;
        if (Mathf.Abs(itemCenterY - centerY) > tolerance)
            throw new InvalidOperationException(
                $"SCN-02B layout invalid in {sectionName}: centerY={itemCenterY} expected={centerY} tolerance={tolerance} rect={item}");
    }

    private static void ValidateMajorPanelGaps()
    {
        ValidateSelectedNavModel();
        RegisterDiagnostic("Target_LeftRail", LeftRailZone(), DiagnosticKind.Target);
        RegisterDiagnostic("Target_HeaderResources", HeaderResourceZone(), DiagnosticKind.Target);
        RegisterDiagnostic("Target_HeaderActions", HeaderActionZone(), DiagnosticKind.Target);
        RegisterDiagnostic("Target_CenterCards", CenterCardsZone(), DiagnosticKind.Target);
        RegisterDiagnostic("Target_RightRail", RightRailZone(), DiagnosticKind.Target);
        RegisterDiagnostic("Target_BottomCTA", BottomCtaZone(), DiagnosticKind.Target);

        LayoutRect[] panels =
        {
            new("NavRail", LeftRailZone()),
            new("CardCampaign", CardRect(0)),
            new("CardOperations", CardRect(1)),
            new("CardSkirmish", CardRect(2)),
            new("CommanderPanel", CommanderRect()),
            new("DeployButton", DeployRect()),
            new("CommsOnline", CommsRect()),
            new("Header", new RectInt(850, 42, 2870, 172))
        };

        for (int i = 0; i < panels.Length; i++)
        {
            RegisterDiagnostic(panels[i].Name, panels[i].Rect, DiagnosticKind.Panel);
            RectInt canvasSafe = Inset(new RectInt(0, 0, CanvasWidth, CanvasHeight), 16, 16);
            if (!Contains(canvasSafe, panels[i].Rect))
                throw new InvalidOperationException($"SCN-02B major panel {panels[i].Name} violates canvas safe edge: {panels[i].Rect}");

            for (int j = i + 1; j < panels.Length; j++)
            {
                if (!IsAllowedPanelOverlap(panels[i].Name, panels[j].Name) && Intersects(panels[i].Rect, panels[j].Rect))
                    throw new InvalidOperationException($"SCN-02B major panel overlap: {panels[i].Name} {panels[i].Rect} overlaps {panels[j].Name} {panels[j].Rect}");
            }
        }
    }

    private static bool IsAllowedPanelOverlap(string left, string right)
    {
        return (left == "CommanderPanel" && right == "DeployButton")
            || (left == "DeployButton" && right == "CommanderPanel");
    }

    private static void ValidateSelectedNavModel()
    {
        if (s_NavItems.Length == 0)
            throw new InvalidOperationException("SCN-02B nav model is empty; one selected nav item is required.");
        if (DefaultSelectedNavIndex < 0 || DefaultSelectedNavIndex >= s_NavItems.Length)
            throw new InvalidOperationException($"SCN-02B selected nav index {DefaultSelectedNavIndex} is outside nav model length {s_NavItems.Length}.");
        if (!string.Equals(s_NavItems[DefaultSelectedNavIndex].Label, "Campaign", StringComparison.Ordinal))
            throw new InvalidOperationException($"SCN-02B default selected nav must be Campaign, got {s_NavItems[DefaultSelectedNavIndex].Label}.");
    }

    private static void RegisterDiagnostic(string name, RectInt rect, DiagnosticKind kind)
    {
        s_Diagnostics.Add(new DiagnosticRect(name, rect, kind));
    }

    private static void RegisterCenterline(string name, int coordinate, bool vertical)
    {
        RectInt rect = vertical
            ? new RectInt(coordinate, 0, 1, CanvasHeight)
            : new RectInt(0, coordinate, CanvasWidth, 1);
        RegisterDiagnostic(name, rect, DiagnosticKind.Centerline);
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
            DiagnosticKind.Centerline => new Color32(255, 0, 220, 190),
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
        Color32 blended = new(
            (byte)Mathf.RoundToInt(baseColor.r * (1f - alpha) + color.r * alpha),
            (byte)Mathf.RoundToInt(baseColor.g * (1f - alpha) + color.g * alpha),
            (byte)Mathf.RoundToInt(baseColor.b * (1f - alpha) + color.b * alpha),
            255);
        texture.SetPixel(x, y, blended);
    }

    private static int[] ToArray(RectInt rect)
    {
        return new[] { rect.x, rect.y, rect.width, rect.height };
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

    private enum DiagnosticKind
    {
        Panel,
        Safe,
        Content,
        Visible,
        Text,
        Target,
        Centerline
    }

    private static void EnsureLayerSpriteImports()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { LayerRoot });
        foreach (string guid in guids)
            EnsureSpriteImport(AssetDatabase.GUIDToAssetPath(guid));
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

    private static void CapturePrefab(string prefabPath, string outputPath, int width, int height, Color backgroundColor)
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
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            renderTexture.Create();

            cameraObject = new GameObject("SCN02BMainMenuCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = CanvasHeight * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.targetTexture = renderTexture;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            canvasObject = CreateRectObject("SCN02BMainMenuCaptureRoot", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localScale = Vector3.one;

            instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform);
            instance.name = prefab.name;
            RectTransform instanceTransform = (RectTransform)instance.transform;
            instanceTransform.anchorMin = new Vector2(0.5f, 0.5f);
            instanceTransform.anchorMax = new Vector2(0.5f, 0.5f);
            instanceTransform.pivot = new Vector2(0.5f, 0.5f);
            instanceTransform.anchoredPosition = Vector2.zero;
            instanceTransform.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
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
}
#endif
