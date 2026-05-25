#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WarlineCaptureGameUiContentPrefabBuilder
{
    private const string ContentFolder = "Assets/Game/Prefabs/UI/Shell/Content";
    private const string PopupFolder = "Assets/Game/Prefabs/UI/Shell/Popups";
    private const string LoadingPrefabPath = ContentFolder + "/SCN01_LoadingContent.prefab";
    private const string MainMenuPrefabPath = ContentFolder + "/SCN02_MainMenuContent.prefab";
    private const string CommanderProfilePrefabPath = ContentFolder + "/SCN03_CommanderProfileContent.prefab";
    private const string MatchHudPrefabPath = ContentFolder + "/SCN08_MatchHudContent.prefab";
    private const string ResultPopupPrefabPath = PopupFolder + "/POP05_MissionResultPopup.prefab";

    private static readonly Color Clear = new(0f, 0f, 0f, 0f);
    private static readonly Color Panel = new(0.025f, 0.031f, 0.027f, 0.92f);
    private static readonly Color PanelMuted = new(0.055f, 0.062f, 0.046f, 0.88f);
    private static readonly Color Stroke = new(0.73f, 0.59f, 0.25f, 0.9f);
    private static readonly Color Text = new(0.86f, 0.84f, 0.74f, 1f);
    private static readonly Color MutedText = new(0.62f, 0.61f, 0.54f, 1f);
    private static readonly Color Accent = new(0.96f, 0.66f, 0.16f, 1f);
    private static readonly Color Blue = new(0.38f, 0.75f, 0.85f, 1f);

    [MenuItem("WarlineCapture/UI/Build GameUI Content Prefabs Step 6")]
    public static void BuildStep6()
    {
        EnsureFolders();
        SavePrefab(BuildLoadingContent(), LoadingPrefabPath);
        SavePrefab(BuildMainMenuContent(), MainMenuPrefabPath);
        SavePrefab(BuildCommanderProfileContent(), CommanderProfilePrefabPath);
        SavePrefab(BuildMatchHudContent(), MatchHudPrefabPath);
        SavePrefab(BuildResultPopup(), ResultPopupPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateStep6();
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_BUILT prefabs=5");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Content Prefabs Step 6")]
    public static void ValidateStep6()
    {
        ValidatePrefab(LoadingPrefabPath, "SCN01_LoadingContent", "LoadingBody");
        ValidateLoadingBackdropStretch();
        ValidateLoadingProgressBinding();
        ValidatePrefab(MainMenuPrefabPath, "SCN02_MainMenuContent", "HeaderContent", "LeftContent", "MiddleContent", "RightContent");
        ValidatePrefab(CommanderProfilePrefabPath, "SCN03_CommanderProfileContent", "LeftContent", "MiddleContent", "RightContent");
        ValidatePrefab(MatchHudPrefabPath, "SCN08_MatchHudContent", "HeaderContent", "LeftContent", "RightContent", "FooterContent");
        ValidatePrefab(ResultPopupPrefabPath, "POP05_MissionResultPopup", "PopupFrame", "PopupFrame/Actions");
        Debug.Log("WARLINECAPTURE_GAMEUI_CONTENT_STEP6_VALIDATED prefabs=5");
    }

    private static GameObject BuildLoadingContent()
    {
        GameObject root = CreateRoot("SCN01_LoadingContent");
        AddSolid(root.transform, "LoadingBackdrop", StretchRect(), new Color(0.015f, 0.018f, 0.016f, 0.98f));
        GameObject body = CreateRect("LoadingBody", root.transform, new Rect(780f, 365f, 840f, 350f));
        AddPanel(body.transform, "Frame", StretchRect());
        AddText(body.transform, "TitleText", "WARLINE CAPTURE", new Rect(46f, 42f, 748f, 58f), 48f, TextAlignmentOptions.Center, Text);
        TMP_Text statusText = AddText(body.transform, "StatusText", "Preparing command interface", new Rect(46f, 120f, 748f, 40f), 24f, TextAlignmentOptions.Center, MutedText);
        AddSolid(body.transform, "ProgressTrack", new Rect(96f, 206f, 648f, 18f), new Color(0.16f, 0.16f, 0.13f, 1f));
        Image progressFill = AddSolid(body.transform, "ProgressFill", new Rect(96f, 206f, 0f, 18f), Accent);
        TMP_Text percentText = AddText(body.transform, "PercentText", "0%", new Rect(96f, 238f, 648f, 34f), 24f, TextAlignmentOptions.Center, Accent);
        WarlineCaptureShellLoadingProgressView loadingProgress = body.AddComponent<WarlineCaptureShellLoadingProgressView>();
        loadingProgress.Configure(progressFill.rectTransform, percentText, statusText, 648f);
        return root;
    }

    private static GameObject BuildMainMenuContent()
    {
        GameObject root = CreateRoot("SCN02_MainMenuContent");

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, 2400f, 140f));
        AddPanel(header.transform, "HeaderPanel", StretchRect());
        AddText(header.transform, "LogoText", "WARLINE\nCAPTURE", new Rect(48f, 26f, 290f, 88f), 34f, TextAlignmentOptions.Left, Text);
        AddHeaderStat(header.transform, 560f, "Credits", "187,540", Accent);
        AddHeaderStat(header.transform, 940f, "Supplies", "92,860", new Color(0.64f, 0.74f, 0.46f, 1f));
        AddHeaderStat(header.transform, 1320f, "Command", "2,715", Blue);
        AddText(header.transform, "MailText", "MAIL", new Rect(2020f, 44f, 130f, 44f), 28f, TextAlignmentOptions.Center, Text);
        AddText(header.transform, "GearText", "GEAR", new Rect(2190f, 44f, 130f, 44f), 28f, TextAlignmentOptions.Center, Text);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, 140f, 360f, 820f));
        AddNavButton(left.transform, 0f, "Campaign", true);
        AddNavButton(left.transform, 120f, "Operations", false);
        AddNavButton(left.transform, 240f, "Skirmish", false);
        AddNavButton(left.transform, 360f, "Store", false);
        AddNavButton(left.transform, 480f, "Commander", false);
        AddNavButton(left.transform, 600f, "Settings", false);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(360f, 140f, 1680f, 820f));
        AddModeCard(middle.transform, 80f, "CAMPAIGN", "Story arc readiness");
        AddModeCard(middle.transform, 600f, "OPERATIONS", "Persistent control");
        AddModeCard(middle.transform, 1120f, "SKIRMISH", "Custom engagement");

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(2040f, 140f, 360f, 820f));
        AddPanel(right.transform, "CommanderPanel", new Rect(18f, 20f, 324f, 680f));
        AddText(right.transform, "CommanderTitle", "COMMANDER", new Rect(44f, 52f, 272f, 36f), 28f, TextAlignmentOptions.Left, Text);
        AddSolid(right.transform, "PortraitPlaceholder", new Rect(56f, 112f, 248f, 250f), new Color(0.02f, 0.024f, 0.02f, 1f));
        AddRouteHotspot(right.transform, "CommanderPortraitButton", new Rect(56f, 112f, 248f, 250f), WarlineCaptureRoute.CommanderProfile);
        AddText(right.transform, "CommanderName", "FIELD COMMANDER\nLEVEL 38", new Rect(56f, 390f, 248f, 76f), 24f, TextAlignmentOptions.Center, Accent);
        AddText(right.transform, "Readiness", "READINESS\n||||||||||---", new Rect(56f, 514f, 248f, 82f), 21f, TextAlignmentOptions.Center, Text);
        AddRouteButton(
            right.transform,
            "DeployCommandButton",
            "DEPLOY COMMAND",
            new Rect(18f, 722f, 324f, 74f),
            UiShellRouteIntent.EnterMatch,
            WarlineCaptureRoute.Match);

        return root;
    }

    private static GameObject BuildCommanderProfileContent()
    {
        GameObject root = CreateRoot("SCN03_CommanderProfileContent");

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, 140f, 360f, 820f));
        AddRouteButton(left.transform, "BackButton", "<  BACK", new Rect(16f, 18f, 328f, 76f), UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);
        AddPanel(left.transform, "CommanderNavPanel", new Rect(16f, 118f, 328f, 630f));
        AddText(left.transform, "CommanderNavTitle", "COMMANDER", new Rect(44f, 148f, 272f, 38f), 28f, TextAlignmentOptions.Left, Text);
        AddProfileTab(left.transform, 212f, "Overview", true);
        AddProfileTab(left.transform, 300f, "Progression", false);
        AddProfileTab(left.transform, 388f, "Service Record", false);
        AddProfileTab(left.transform, 476f, "Loadout", false);
        AddProfileTab(left.transform, 564f, "Cosmetics", false);

        GameObject middle = CreateGroup("MiddleContent", root.transform, new Rect(360f, 140f, 1680f, 820f));
        AddPanel(middle.transform, "ProfileMainPanel", new Rect(70f, 42f, 1540f, 700f));
        AddText(middle.transform, "ProfileTitle", "COMMANDER PROFILE", new Rect(118f, 82f, 700f, 54f), 40f, TextAlignmentOptions.Left, Text);
        AddText(middle.transform, "ProfileSubtitle", "Field Commander  |  Level 38  |  First Contact Ready", new Rect(118f, 142f, 900f, 36f), 23f, TextAlignmentOptions.Left, Accent);
        AddSolid(middle.transform, "PortraitLarge", new Rect(118f, 220f, 370f, 420f), new Color(0.018f, 0.022f, 0.018f, 1f));
        AddText(middle.transform, "PortraitSilhouette", "COMMANDER\nSILHOUETTE", new Rect(156f, 370f, 294f, 88f), 24f, TextAlignmentOptions.Center, MutedText);
        AddText(middle.transform, "ProfileBio", "Decorated operations leader assigned to rapid district stabilization.\n\nCurrent doctrine favors mobile armor, district control, and precision extraction under pressure.", new Rect(560f, 222f, 920f, 180f), 28f, TextAlignmentOptions.Left, Text);
        AddMetricCard(middle.transform, 560f, 456f, "MISSIONS", "42");
        AddMetricCard(middle.transform, 820f, 456f, "VICTORIES", "31");
        AddMetricCard(middle.transform, 1080f, 456f, "AUTHORITY", "2,715");
        AddMetricCard(middle.transform, 1340f, 456f, "RANK", "III");

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(2040f, 140f, 360f, 820f));
        AddPanel(right.transform, "ProfileStatsPanel", new Rect(18f, 20f, 324f, 720f));
        AddText(right.transform, "StatsTitle", "READINESS", new Rect(48f, 54f, 264f, 36f), 28f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "StatsBars", "TACTICS     ||||||||||--\nARMOR       |||||||||---\nLOGISTICS   ||||||||----\nINTEL       |||||||-----", new Rect(48f, 124f, 264f, 180f), 22f, TextAlignmentOptions.Left, MutedText);
        AddText(right.transform, "UnlockTitle", "ACTIVE PERKS", new Rect(48f, 342f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "UnlockList", "Rapid Deployment\nSupply Efficiency\nDistrict Resolve", new Rect(48f, 394f, 264f, 150f), 22f, TextAlignmentOptions.Left, Accent);
        AddText(right.transform, "ProfileHint", "Header remains active.\nBody regions are swapped by shell route.", new Rect(48f, 604f, 264f, 86f), 18f, TextAlignmentOptions.Center, MutedText);

        return root;
    }

    private static GameObject BuildMatchHudContent()
    {
        GameObject root = CreateRoot("SCN08_MatchHudContent");

        GameObject header = CreateGroup("HeaderContent", root.transform, new Rect(0f, 0f, 2400f, 118f));
        AddPanel(header.transform, "HudHeader", StretchRect());
        AddText(header.transform, "HudTitle", "PORT BREACH  |  LIVE OPERATION", new Rect(48f, 32f, 700f, 46f), 30f, TextAlignmentOptions.Left, Text);
        AddText(header.transform, "HudResources", "CRED 187,540     SUP 92,860     CMD 2,715", new Rect(1040f, 32f, 980f, 46f), 28f, TextAlignmentOptions.Right, Text);

        GameObject left = CreateGroup("LeftContent", root.transform, new Rect(0f, 140f, 360f, 820f));
        AddPanel(left.transform, "ObjectivesPanel", new Rect(20f, 20f, 320f, 370f));
        AddText(left.transform, "ObjectiveTitle", "OBJECTIVES", new Rect(48f, 50f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(left.transform, "ObjectiveList", "Secure depot\nHold main road\nExtract civilians", new Rect(48f, 108f, 264f, 160f), 22f, TextAlignmentOptions.Left, MutedText);
        AddPanel(left.transform, "SquadPanel", new Rect(20f, 430f, 320f, 260f));
        AddText(left.transform, "SquadText", "SQUADS\nRifle 01 Ready\nArmor 02 Moving", new Rect(48f, 462f, 264f, 160f), 22f, TextAlignmentOptions.Left, Text);

        GameObject right = CreateGroup("RightContent", root.transform, new Rect(2040f, 140f, 360f, 820f));
        AddPanel(right.transform, "CommandPanel", new Rect(20f, 20f, 320f, 520f));
        AddText(right.transform, "CommandTitle", "COMMANDS", new Rect(48f, 50f, 264f, 34f), 25f, TextAlignmentOptions.Left, Text);
        AddText(right.transform, "CommandList", "Move\nAttack\nHold\nExtract", new Rect(48f, 116f, 264f, 240f), 28f, TextAlignmentOptions.Left, MutedText);

        GameObject footer = CreateGroup("FooterContent", root.transform, new Rect(0f, 960f, 2400f, 120f));
        AddPanel(footer.transform, "FooterRail", StretchRect());
        AddText(footer.transform, "FooterText", "TACTICAL LINK ONLINE     |     UNIT ORDERS QUEUED     |     CAMERA FOLLOW READY", new Rect(80f, 32f, 2240f, 48f), 28f, TextAlignmentOptions.Center, Text);

        return root;
    }

    private static GameObject BuildResultPopup()
    {
        GameObject root = CreateRoot("POP05_MissionResultPopup");
        GameObject frame = CreateRect("PopupFrame", root.transform, new Rect(0f, 0f, 920f, 560f));
        AddPanel(frame.transform, "Frame", StretchRect());
        AddText(frame.transform, "TitleText", "MISSION RESULT", new Rect(64f, 48f, 792f, 58f), 42f, TextAlignmentOptions.Center, Text);
        AddText(frame.transform, "OutcomeText", "VICTORY COMPLETE", new Rect(64f, 136f, 792f, 52f), 34f, TextAlignmentOptions.Center, Accent);
        AddText(frame.transform, "SummaryText", "Primary objectives secured.\nDistrict pressure reduced.\nCommander authority increased.", new Rect(96f, 230f, 728f, 150f), 25f, TextAlignmentOptions.Center, MutedText);
        GameObject actions = CreateRect("Actions", frame.transform, new Rect(160f, 430f, 600f, 72f));
        AddResultConfirmButton(actions.transform, "ContinueButton", "CONTINUE", StretchRect());
        return root;
    }

    private static void AddHeaderStat(Transform parent, float x, string label, string value, Color valueColor)
    {
        AddSolid(parent, $"{label}IconSlot", new Rect(x, 34f, 70f, 70f), new Color(0.11f, 0.11f, 0.09f, 1f));
        AddText(parent, $"{label}Label", label, new Rect(x + 92f, 28f, 220f, 30f), 20f, TextAlignmentOptions.Left, MutedText);
        AddText(parent, $"{label}Value", value, new Rect(x + 92f, 60f, 230f, 46f), 32f, TextAlignmentOptions.Left, valueColor);
    }

    private static void AddNavButton(Transform parent, float y, string label, bool selected)
    {
        Color fill = selected ? new Color(0.28f, 0.28f, 0.12f, 0.95f) : Panel;
        AddSolid(parent, $"{label}_Button", new Rect(16f, y + 18f, 328f, 82f), fill);
        AddText(parent, $"{label}_Label", label, new Rect(96f, y + 38f, 220f, 42f), 26f, TextAlignmentOptions.Left, Text);
        AddSolid(parent, $"{label}_Icon", new Rect(42f, y + 42f, 34f, 34f), selected ? Accent : MutedText);
    }

    private static void AddModeCard(Transform parent, float x, string title, string subtitle)
    {
        AddPanel(parent, $"{title}_Card", new Rect(x, 150f, 460f, 520f));
        AddText(parent, $"{title}_Title", title, new Rect(x + 48f, 188f, 364f, 46f), 32f, TextAlignmentOptions.Left, Text);
        AddSolid(parent, $"{title}_Art", new Rect(x + 42f, 260f, 376f, 245f), new Color(0.08f, 0.10f, 0.085f, 1f));
        AddText(parent, $"{title}_Subtitle", subtitle, new Rect(x + 48f, 538f, 364f, 76f), 22f, TextAlignmentOptions.Left, MutedText);
        AddSolid(parent, $"{title}_Progress", new Rect(x + 48f, 628f, 260f, 18f), Accent);
    }

    private static void AddProfileTab(Transform parent, float y, string label, bool selected)
    {
        AddSolid(parent, $"{label}_Tab", new Rect(42f, y, 276f, 58f), selected ? new Color(0.30f, 0.29f, 0.12f, 0.95f) : PanelMuted);
        AddText(parent, $"{label}_Label", label, new Rect(64f, y + 12f, 232f, 32f), 20f, TextAlignmentOptions.Left, selected ? Accent : Text);
    }

    private static void AddMetricCard(Transform parent, float x, float y, string label, string value)
    {
        AddPanel(parent, $"{label}_Metric", new Rect(x, y, 210f, 118f));
        AddText(parent, $"{label}_MetricLabel", label, new Rect(x + 22f, y + 22f, 166f, 28f), 18f, TextAlignmentOptions.Center, MutedText);
        AddText(parent, $"{label}_MetricValue", value, new Rect(x + 22f, y + 56f, 166f, 42f), 28f, TextAlignmentOptions.Center, Accent);
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        Stretch(rect);
        root.GetComponent<CanvasGroup>().alpha = 1f;
        return root;
    }

    private static GameObject CreateGroup(string name, Transform parent, Rect rect)
    {
        GameObject group = CreateRect(name, parent, rect);
        return group;
    }

    private static GameObject CreateRect(string name, Transform parent, Rect rect)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        ApplyTopLeftRect(rectTransform, rect);
        return obj;
    }

    private static void AddPanel(Transform parent, string name, Rect rect)
    {
        AddSolid(parent, name, rect, Panel);
        AddSolid(parent, $"{name}_TopStroke", new Rect(rect.x, rect.y, rect.width, 2f), Stroke);
        AddSolid(parent, $"{name}_BottomStroke", new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), Stroke);
    }

    private static Image AddSolid(Transform parent, string name, Rect rect, Color color)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void AddRouteButton(
        Transform parent,
        string name,
        string label,
        Rect rect,
        UiShellRouteIntent intent,
        WarlineCaptureRoute route)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.69f, 0.45f, 0.08f, 0.96f);
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.42f, 1f);
        colors.pressedColor = new Color(0.88f, 0.56f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = obj.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(intent, route, false);

        AddText(obj.transform, "Label", label, StretchRect(), 27f, TextAlignmentOptions.Center, Color.black);
    }

    private static void AddRouteHotspot(Transform parent, string name, Rect rect, WarlineCaptureRoute route)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = Clear;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.pressedColor = new Color(1f, 0.82f, 0.3f, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        WarlineCaptureShellRouteButtonView routeButton = obj.AddComponent<WarlineCaptureShellRouteButtonView>();
        routeButton.Configure(UiShellRouteIntent.OpenMenuRoute, route, false);
    }

    private static void AddResultConfirmButton(Transform parent, string name, string label, Rect rect)
    {
        GameObject obj = CreateRect(name, parent, rect);
        Image image = obj.AddComponent<Image>();
        image.color = PanelMuted;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.75f, 0.32f, 1f);
        colors.pressedColor = new Color(0.74f, 0.50f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        obj.AddComponent<WarlineCaptureShellResultConfirmButtonView>();
        AddText(obj.transform, "Label", label, StretchRect(), 30f, TextAlignmentOptions.Center, Text);
    }

    private static TMP_Text AddText(Transform parent, string name, string value, Rect rect, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = CreateRect(name, parent, rect);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static Rect StretchRect() => new(0f, 0f, 0f, 0f);

    private static void ApplyTopLeftRect(RectTransform rectTransform, Rect rect)
    {
        if (rect.width <= 0f && rect.height <= 0f)
        {
            Stretch(rectTransform);
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
        rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ValidatePrefab(string path, string expectedRootName, params string[] requiredChildren)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException($"Missing GameUI content prefab at {path}.");
        if (prefab.name != expectedRootName)
            throw new InvalidOperationException($"{path} root must be named {expectedRootName}.");
        if (prefab.GetComponent<RectTransform>() == null)
            throw new InvalidOperationException($"{path} root must be a RectTransform.");
        if (prefab.GetComponent<CanvasGroup>() == null)
            throw new InvalidOperationException($"{path} root must contain a CanvasGroup.");
        if (prefab.GetComponentInChildren<Canvas>(true) != null)
            throw new InvalidOperationException($"{path} must not contain a nested Canvas.");

        foreach (string childName in requiredChildren)
        {
            if (prefab.transform.Find(childName) == null)
                throw new InvalidOperationException($"{path} is missing required child {childName}.");
        }
    }

    private static void ValidateLoadingBackdropStretch()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPrefabPath);
        RectTransform backdrop = prefab.transform.Find("LoadingBackdrop") as RectTransform;
        if (backdrop == null)
            throw new InvalidOperationException("SCN01 loading content must contain LoadingBackdrop.");

        if (backdrop.anchorMin != Vector2.zero || backdrop.anchorMax != Vector2.one)
            throw new InvalidOperationException("SCN01 LoadingBackdrop must be stretched to the full loading layer.");
        if (backdrop.offsetMin != Vector2.zero || backdrop.offsetMax != Vector2.zero)
            throw new InvalidOperationException("SCN01 LoadingBackdrop must use zero stretch offsets.");
        if (backdrop.localScale != Vector3.one)
            throw new InvalidOperationException("SCN01 LoadingBackdrop scale must remain 1.");
    }

    private static void ValidateLoadingProgressBinding()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPrefabPath);
        Transform body = prefab.transform.Find("LoadingBody");
        WarlineCaptureShellLoadingProgressView progressView = body?.GetComponent<WarlineCaptureShellLoadingProgressView>();
        if (progressView == null)
            throw new InvalidOperationException("SCN01 loading content must contain WarlineCaptureShellLoadingProgressView on LoadingBody.");

        RectTransform fill = body.Find("ProgressFill") as RectTransform;
        TMP_Text percent = body.Find("PercentText")?.GetComponent<TMP_Text>();
        TMP_Text status = body.Find("StatusText")?.GetComponent<TMP_Text>();
        if (fill == null || percent == null || status == null)
            throw new InvalidOperationException("SCN01 loading content must contain ProgressFill, PercentText, and StatusText.");
        if (fill.rect.width > 0.5f)
            throw new InvalidOperationException("SCN01 loading ProgressFill must start at zero width.");
        if (percent.text != "0%")
            throw new InvalidOperationException("SCN01 loading PercentText must start at 0%.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Game/Prefabs/UI/Shell", "Content");
        EnsureFolder("Assets/Game/Prefabs/UI/Shell", "Popups");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string fullPath = $"{parent}/{name}";
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

        string guid = AssetDatabase.CreateFolder(parent, name);
        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException($"Failed to create folder {fullPath}.");
    }
}
#endif
