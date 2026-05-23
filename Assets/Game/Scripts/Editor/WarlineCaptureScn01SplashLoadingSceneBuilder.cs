#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureScn01SplashLoadingSceneBuilder
{
    private const int CanvasWidth = 2400;
    private const int CanvasHeight = 1080;
    private const string LayerRoot = "Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV01";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_SCN01_SplashLoading_TargetLock.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/SCN01_SplashLoading_TargetLock.unity";
    private const string CapturePath = "Design/AgentReports/Captures/SCN01_SplashLoading_TargetLock_V03_2400x1080.png";

    private static Color TextMain => new Color32(226, 219, 197, 255);
    private static Color TextMuted => new Color32(178, 171, 148, 255);
    private static Color Gold => new Color32(229, 174, 39, 255);
    private static Color Green => new Color32(160, 187, 75, 255);
    private static Color DarkPanel => new Color32(9, 11, 9, 232);

    [MenuItem("WarlineCapture/Design/SCN-01 Build Splash Loading Target Lock")]
    public static void BuildScene()
    {
        WarlineCaptureLayeredUiBuilderUtility.EnsureLayerSpriteImports(LayerRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject prefabRoot = BuildCanvasPrefabRoot();

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);

        GameObject sceneCanvas = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN01_SplashLoading_Canvas", null);
        RectTransform sceneCanvasRect = sceneCanvas.GetComponent<RectTransform>();
        sceneCanvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        sceneCanvasRect.localPosition = Vector3.zero;
        sceneCanvasRect.localScale = Vector3.one;

        Canvas canvas = sceneCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        sceneCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        sceneCanvas.AddComponent<GraphicRaycaster>();

        GameObject instance = Object.Instantiate(prefabRoot, sceneCanvas.transform);
        instance.name = "Screen_SCN01_SplashLoading_TargetLock";
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(instance.GetComponent<RectTransform>());
        Object.DestroyImmediate(prefabRoot);

        WarlineCaptureLayeredUiBuilderUtility.AddEventSystem();
        Camera camera = WarlineCaptureLayeredUiBuilderUtility.AddSceneCamera(CanvasHeight);
        canvas.worldCamera = camera;

        WarlineCaptureLayeredUiBuilderUtility.EnsureParentFolder(ScenePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-01] Built scene={ScenePath} prefab={PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-01 Capture Splash Loading Target Lock")]
    public static void CaptureScene()
    {
        BuildScene();
        WarlineCaptureLayeredUiBuilderUtility.CapturePrefab(PrefabPath, CapturePath, CanvasWidth, CanvasHeight, CanvasWidth, CanvasHeight, Color.black);
        Debug.Log($"[SCN-01] Captured {CapturePath}");
    }

    private static GameObject BuildCanvasPrefabRoot()
    {
        GameObject root = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("Screen_SCN01_SplashLoading_TargetLock", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

        Image baseImage = root.AddComponent<Image>();
        baseImage.color = Color.black;
        baseImage.raycastTarget = false;

        WarlineCaptureScreenController controller = root.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(WarlineCaptureRoute.Splash);

        GameObject visualRoot = WarlineCaptureLayeredUiBuilderUtility.CreateRectObject("SCN01_LayeredCanvas", root.transform);
        WarlineCaptureLayeredUiBuilderUtility.StretchToParent(visualRoot.GetComponent<RectTransform>());
        BuildLayeredVisual(visualRoot.transform);

        return root;
    }

    private static void BuildLayeredVisual(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddCoverImage(parent, LayerRoot, "Background_NoUi", "scn01_background_21x9_no_ui.png", new RectInt(0, 0, CanvasWidth, CanvasHeight), Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Screen_Vignette", new RectInt(0, 0, CanvasWidth, CanvasHeight), new Color(0f, 0f, 0f, 0.14f));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "Logo_ReadabilityBand", new RectInt(384, 200, 1632, 252), new Color(0f, 0f, 0f, 0.16f));

        AddOuterFrame(parent);
        AddBrand(parent);
        AddLoadingPanel(parent);
        AddBottomStatus(parent);

        WarlineCaptureLayeredUiBuilderUtility.ValidateMajorPanels(
            new WarlineUiRect("Brand", new RectInt(650, 204, 1100, 250)),
            new WarlineUiRect("LoadingPanel", LoadingPanelRect()),
            new WarlineUiRect("BottomStatus", BottomStatusRect()));
    }

    private static void AddOuterFrame(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "OuterTopLine", new RectInt(16, 12, CanvasWidth - 32, 4), new Color(0.8f, 0.65f, 0.32f, 0.38f));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "OuterBottomLine", new RectInt(16, CanvasHeight - 18, CanvasWidth - 32, 4), new Color(0.8f, 0.65f, 0.32f, 0.38f));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "OuterLeftLine", new RectInt(10, 18, 4, CanvasHeight - 36), new Color(0.8f, 0.65f, 0.32f, 0.26f));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "OuterRightLine", new RectInt(CanvasWidth - 14, 18, 4, CanvasHeight - 36), new Color(0.8f, 0.65f, 0.32f, 0.26f));

        AddCorner(parent, "OuterTopLeftTrim", 26, 26, true, true);
        AddCorner(parent, "OuterTopRightTrim", 2118, 26, false, true);
        AddCorner(parent, "OuterBottomLeftTrim", 26, 1000, true, false);
        AddCorner(parent, "OuterBottomRightTrim", 2118, 1000, false, false);
    }

    private static void AddCorner(Transform parent, string name, int x, int y, bool left, bool top)
    {
        Color color = new(0.92f, 0.70f, 0.18f, 0.82f);
        int horizontalX = left ? x : x + 52;
        int verticalX = left ? x : x + 202;
        int horizontalY = top ? y : y + 74;
        int verticalY = top ? y : y - 1;
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Horizontal", new RectInt(horizontalX, horizontalY, 158, 4), color);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Vertical", new RectInt(verticalX, verticalY, 4, 78), color);
    }

    private static void AddBrand(Transform parent)
    {
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "Brand_CommandShield", "scn01_icon_02_command_shield.png", new RectInt(726, 198, 206, 220), 174, 202, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Brand_WarlineShadow", "WARLINE", new RectInt(964, 210, 780, 112), 98f, TextAlignmentOptions.Left, new Color(0f, 0f, 0f, 0.72f));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Brand_Warline", "WARLINE", new RectInt(954, 198, 780, 112), 98f, TextAlignmentOptions.Left, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Brand_CaptureShadow", "C A P T U R E", new RectInt(990, 326, 620, 76), 58f, TextAlignmentOptions.Left, new Color(0f, 0f, 0f, 0.72f));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "Brand_Capture", "C A P T U R E", new RectInt(980, 316, 620, 76), 58f, TextAlignmentOptions.Left, Gold);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Brand_GoldChevron", "scn01_chrome_16_gold_chevron_trio.png", new RectInt(1538, 332, 134, 58), true, Gold);

        RectInt system = new(994, 440, 414, 48);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "CommandSystem_Back", system, DarkPanel);
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "CommandSystem_TopLine", new RectInt(system.x, system.y, system.width, 2), new Color(0.82f, 0.66f, 0.24f, 0.65f));
        WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, "CommandSystem_BottomLine", new RectInt(system.x, system.yMax - 2, system.width, 2), new Color(0.82f, 0.66f, 0.24f, 0.65f));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "CommandSystem_Text", "COMMAND SYSTEM", new RectInt(system.x + 18, system.y + 6, system.width - 36, 36), 24f, TextAlignmentOptions.Center, TextMain);
    }

    private static void AddLoadingPanel(Transform parent)
    {
        RectInt panel = LoadingPanelRect();
        AddFrame(parent, "LoadingPanel_Frame", "scn01_chrome_03_loading_panel_frame.png", panel, 16, new Color32(7, 9, 8, 232));
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "LoadingPanel_Status", "INITIALIZING COMMAND NET...", new RectInt(panel.x + 44, panel.y + 35, 770, 48), 33f, TextAlignmentOptions.Left, TextMain);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "LoadingPanel_Percent", "68%", new RectInt(panel.x + panel.width - 170, panel.y + 31, 120, 50), 37f, TextAlignmentOptions.Right, Gold);

        RectInt progressFrame = new(panel.x + 44, panel.y + 100, panel.width - 88, 30);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Progress_Frame", "scn01_chrome_04_progress_bar_frame.png", progressFrame, false, Color.white);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, "Progress_Fill", "scn01_chrome_05_progress_fill_gold.png", new RectInt(progressFrame.x + 7, progressFrame.y + 7, Mathf.RoundToInt((progressFrame.width - 14) * 0.68f), 16), false, Gold);
        AddProgressTicks(parent, progressFrame);

        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "LoadingPanel_Tip", "Tip: Scout streets before committing armor; civilian risk changes mission rewards.", new RectInt(panel.x + 44, panel.y + 136, panel.width - 88, 28), 20f, TextAlignmentOptions.Left, TextMuted);
    }

    private static void AddProgressTicks(Transform parent, RectInt frame)
    {
        int usable = frame.width - 14;
        for (int i = 1; i < 10; i++)
        {
            int x = frame.x + 7 + Mathf.RoundToInt(usable * (i / 10f));
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"Progress_Tick_{i}", new RectInt(x, frame.y + 7, 2, 16), new Color(0f, 0f, 0f, 0.62f));
        }
    }

    private static void AddBottomStatus(Transform parent)
    {
        RectInt rect = BottomStatusRect();
        AddFrame(parent, "BottomStatus_Chip", "scn01_chrome_07_small_system_chip_frame.png", rect, 7, new Color32(9, 12, 8, 228));
        WarlineCaptureLayeredUiBuilderUtility.AddFittedImage(parent, LayerRoot, "BottomStatus_Spinner", "scn01_icon_03_loading_spinner_ring.png", new RectInt(rect.x + 28, rect.y + 10, 46, 42), 32, 32, Green);
        WarlineCaptureLayeredUiBuilderUtility.AddText(parent, "BottomStatus_Text", "LOADING REQUIRED DATA", new RectInt(rect.x + 78, rect.y + 12, rect.width - 100, 30), 18f, TextAlignmentOptions.Center, Green);
    }

    private static void AddFrame(Transform parent, string name, string sprite, RectInt rect, int fillInset, Color fillColor)
    {
        if (fillInset > 0)
            WarlineCaptureLayeredUiBuilderUtility.AddSolidImage(parent, $"{name}_Fill", WarlineCaptureLayeredUiBuilderUtility.Inset(rect, fillInset, fillInset), fillColor);
        WarlineCaptureLayeredUiBuilderUtility.AddImage(parent, LayerRoot, name, sprite, rect, false, Color.white);
    }

    private static RectInt LoadingPanelRect() => new(560, 764, 1280, 178);
    private static RectInt BottomStatusRect() => new(990, 1014, 420, 44);
}
#endif
