#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureGameUiSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RootName = "GameUIRoot";
    private const string CameraName = "GameUICamera";
    private const string EventSystemName = "EventSystem";
    private const string CanvasName = "GameUICanvas";
    private const string ShellRootName = "WarlineCaptureRuntimeShell";
    private const string ContentRootName = "ContentRoot";
    private const string ContentFolder = "Assets/Game/Prefabs/UI/Shell/Content";
    private const string PopupFolder = "Assets/Game/Prefabs/UI/Shell/Popups";
    private const string LoadingPrefabPath = ContentFolder + "/SCN01_LoadingContent.prefab";
    private const string MainMenuPrefabPath = ContentFolder + "/SCN02_MainMenuContent.prefab";
    private const string CommanderProfilePrefabPath = ContentFolder + "/SCN03_CommanderProfileContent.prefab";
    private const string ArmoryPrefabPath = ContentFolder + "/SCN19_ArmoryContent.prefab";
    private const string MatchHudPrefabPath = ContentFolder + "/SCN08_MatchHudContent.prefab";
    private const string BuildDrawerPopupPrefabPath = PopupFolder + "/SCN09_BuildDrawerPopup.prefab";
    private const string ResultPopupPrefabPath = PopupFolder + "/POP05_MissionResultPopup.prefab";
    private const string CaptureFolder = "Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock";
    private const string MainMenuCaptureFolder = CaptureFolder + "/Responsive";
    private const string CommanderProfileCaptureFolder = "Design/AgentReports/Captures/GameUI/CommanderProfile/CleanTargetLock";
    private const string CommanderProfileResponsiveCaptureFolder = CommanderProfileCaptureFolder + "/Responsive";
    private const string ArmoryCaptureFolder = "Design/AgentReports/Captures/GameUI/Armory/CleanTargetLock";
    private const string ArmoryResponsiveCaptureFolder = ArmoryCaptureFolder + "/Responsive";
    private const string MatchHudCaptureFolder = "Design/AgentReports/Captures/GameUI/MatchHud/CleanTargetLock";
    private const string MatchHudResponsiveCaptureFolder = MatchHudCaptureFolder + "/Responsive";
    private const string BuildDrawerCaptureFolder = "Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock";
    private const string BuildDrawerResponsiveCaptureFolder = BuildDrawerCaptureFolder + "/Responsive";
    private const string MissionResultCaptureFolder = "Design/AgentReports/Captures/GameUI/MissionResult/CleanTargetLock";
    private const string MissionResultResponsiveCaptureFolder = MissionResultCaptureFolder + "/Responsive";
    private const int ShellWidth = 4800;
    private const int ShellHeight = 2160;
    private const int CaptureWidth = ShellWidth;
    private const int CaptureHeight = ShellHeight;

    private static readonly Rect StretchRegion = new(0f, 0f, ShellWidth, ShellHeight);

    private static readonly CaptureResolution[] MainMenuCaptureResolutions =
    {
        new(1920, 1080, "1920x1080"),
        new(2400, 1080, "2400x1080"),
        new(3840, 2160, "3840x2160"),
        new(4800, 2160, "4800x2160")
    };

    private static readonly ShellRegionDefinition[] RegionDefinitions =
    {
        new(WarlineCaptureShellRegionId.MenuBackgroundRegion, "MenuBackgroundRegion", Vector2.zero, StretchRegion),
        new(WarlineCaptureShellRegionId.HeaderRegion, "HeaderRegion", new Vector2(0f, 1f), new Rect(0f, 0f, 4800f, 280f)),
        new(WarlineCaptureShellRegionId.LeftRegion, "LeftRegion", new Vector2(-1f, 0f), new Rect(0f, 280f, 720f, 1640f)),
        new(WarlineCaptureShellRegionId.MiddleRegion, "MiddleRegion", Vector2.zero, new Rect(720f, 280f, 3360f, 1640f)),
        new(WarlineCaptureShellRegionId.RightRegion, "RightRegion", new Vector2(1f, 0f), new Rect(4080f, 280f, 720f, 1640f)),
        new(WarlineCaptureShellRegionId.FooterRegion, "FooterRegion", new Vector2(0f, -1f), new Rect(0f, 1920f, 4800f, 240f)),
        new(WarlineCaptureShellRegionId.PopupLayer, "PopupLayer", Vector2.zero, StretchRegion),
        new(WarlineCaptureShellRegionId.LoadingLayer, "LoadingLayer", new Vector2(0f, -1f), StretchRegion)
    };

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 1")]
    public static void BuildStep1()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        Camera uiCamera = CreateUiCamera(root.transform);
        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform, uiCamera);
        CreateShellRoot(canvasObject.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep1();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP1_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 2")]
    public static void BuildStep2()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        Camera uiCamera = CreateUiCamera(root.transform);
        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform, uiCamera);
        GameObject shellRoot = CreateShellRoot(canvasObject.transform);
        CreateShellRegions(shellRoot.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep2();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP2_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 3")]
    public static void BuildStep3()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        Camera uiCamera = CreateUiCamera(root.transform);
        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform, uiCamera);
        GameObject shellRoot = CreateShellRoot(canvasObject.transform);
        CreateShellRegions(shellRoot.transform);
        AddMotionHost(shellRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep3();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP3_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 5")]
    public static void BuildStep5()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        Camera uiCamera = CreateUiCamera(root.transform);
        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform, uiCamera);
        GameObject shellRoot = CreateShellRoot(canvasObject.transform);
        CreateShellRegions(shellRoot.transform);
        AddMotionHost(shellRoot);
        AddShellViewAndBridge(shellRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep5();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP5_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 7")]
    public static void BuildStep7()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        Camera uiCamera = CreateUiCamera(root.transform);
        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform, uiCamera);
        GameObject shellRoot = CreateShellRoot(canvasObject.transform);
        CreateShellRegions(shellRoot.transform);
        AddMotionHost(shellRoot);
        AddShellViewAndBridge(shellRoot);
        AddContentPresenterAndSmoke(shellRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep7();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP7_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 8")]
    public static void BuildStep8()
    {
        BuildStep7();
        ValidateStep8();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP8_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Capture GameUI Scene Step 9")]
    public static void CaptureStep9()
    {
        ValidateStep8();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform root = scene.GetRootGameObjects()[0].transform;
        Camera camera = root.Find(CameraName)?.GetComponent<Camera>();
        Transform canvasTransform = root.Find(CanvasName);
        Transform shellTransform = canvasTransform?.Find(ShellRootName);
        WarlineCaptureShellView shellView = shellTransform?.GetComponent<WarlineCaptureShellView>();
        WarlineCaptureShellContentPresenterView contentPresenter = shellTransform?.GetComponent<WarlineCaptureShellContentPresenterView>();

        if (camera == null || shellView == null || contentPresenter == null)
            throw new InvalidOperationException("GameUI Step 9 capture requires camera, shell view, and content presenter.");

        Directory.CreateDirectory(CaptureFolder);

        PrepareLoadingStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CaptureFolder}/GameUI_Loading_Stable.png");

        PrepareMainMenuStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CaptureFolder}/GameUI_MainMenu_Stable.png");

        PrepareCommanderProfileStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CommanderProfileCaptureFolder}/GameUI_CommanderProfile_Stable.png");

        PrepareArmoryStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{ArmoryCaptureFolder}/GameUI_Armory_Stable.png");

        PrepareMatchHudStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CaptureFolder}/GameUI_MatchHud_Stable.png");
        CaptureCamera(camera, $"{MatchHudCaptureFolder}/GameUI_MatchHud_Stable.png");

        PrepareBuildDrawerStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{BuildDrawerCaptureFolder}/GameUI_BuildDrawer_Stable.png");

        PrepareResultPopupStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CaptureFolder}/GameUI_ResultPopup_Stable.png");
        CaptureCamera(camera, $"{MissionResultCaptureFolder}/GameUI_MissionResult_Stable.png");

        PrepareMainMenuStable(shellView, contentPresenter);
        CaptureCamera(camera, $"{CaptureFolder}/GameUI_ReturnedMainMenu_Stable.png");
        CaptureMainMenuAspectSamples(camera, shellView, contentPresenter);
        CaptureCommanderProfileAspectSamples(camera, shellView, contentPresenter);
        CaptureArmoryAspectSamples(camera, shellView, contentPresenter);
        CaptureMatchHudAspectSamples(camera, shellView, contentPresenter);
        CaptureBuildDrawerAspectSamples(camera, shellView, contentPresenter);
        CaptureMissionResultAspectSamples(camera, shellView, contentPresenter);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateStep9Captures();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP9_CAPTURED folder={CaptureFolder}");
    }

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 9")]
    public static void BuildStep9()
    {
        BuildStep8();
        CaptureStep9();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP9_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 1")]
    public static void ValidateStep1()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Length != 1 || roots[0].name != RootName)
            throw new InvalidOperationException($"GameUI scene must contain exactly one root named {RootName}.");

        Transform root = roots[0].transform;
        Transform cameraTransform = RequireChild(root, CameraName);
        Transform eventSystemTransform = RequireChild(root, EventSystemName);
        Transform canvasTransform = RequireChild(root, CanvasName);
        Transform shellTransform = RequireChild(canvasTransform, ShellRootName);

        EventSystem eventSystem = eventSystemTransform.GetComponent<EventSystem>();
        if (eventSystem == null)
            throw new InvalidOperationException($"{EventSystemName} must contain an EventSystem component.");

        Camera uiCamera = cameraTransform.GetComponent<Camera>();
        if (uiCamera == null)
            throw new InvalidOperationException($"{CameraName} must contain a Camera component.");
        if (!uiCamera.orthographic)
            throw new InvalidOperationException($"{CameraName} must be orthographic.");

        InputSystemUIInputModule inputModule = eventSystemTransform.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            throw new InvalidOperationException($"{EventSystemName} must contain an InputSystemUIInputModule.");
        if (eventSystemTransform.GetComponent<StandaloneInputModule>() != null)
            throw new InvalidOperationException($"{EventSystemName} must not contain StandaloneInputModule because Player Settings use Input System input handling.");

        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas == null)
            throw new InvalidOperationException($"{CanvasName} must contain a Canvas component.");
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
            throw new InvalidOperationException($"{CanvasName} must use ScreenSpaceCamera for the isolated UI shell scene.");
        if (canvas.worldCamera != uiCamera)
            throw new InvalidOperationException($"{CanvasName} must render through {CameraName}.");

        CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
        if (scaler == null)
            throw new InvalidOperationException($"{CanvasName} must contain a CanvasScaler component.");
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            throw new InvalidOperationException($"{CanvasName} must scale with screen size.");
        if (scaler.referenceResolution != new Vector2(ShellWidth, ShellHeight))
            throw new InvalidOperationException($"{CanvasName} must use the {ShellWidth}x{ShellHeight} shell reference resolution.");

        if (canvasTransform.GetComponent<GraphicRaycaster>() == null)
            throw new InvalidOperationException($"{CanvasName} must contain a GraphicRaycaster component.");

        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        RectTransform shellRect = shellTransform.GetComponent<RectTransform>();
        if (canvasRect == null || shellRect == null)
            throw new InvalidOperationException("GameUI Canvas and shell root must be RectTransform UI objects.");
        ValidateStretchRect(shellRect, ShellRootName);

        if (roots[0].GetComponentsInChildren<Canvas>(true).Length != 1)
            throw new InvalidOperationException("GameUI scene must contain exactly one Canvas in Step 1.");
        if (roots[0].GetComponentsInChildren<EventSystem>(true).Length != 1)
            throw new InvalidOperationException("GameUI scene must contain exactly one EventSystem in Step 1.");
        if (roots[0].GetComponentsInChildren<Camera>(true).Length != 1)
            throw new InvalidOperationException("GameUI scene must contain exactly one Camera in Step 1.");

        string[] forbiddenRoots =
        {
            "GameBootstrap",
            "Bootstrap",
            "UI_Canvas",
            "WarlineCaptureUIBootstrap",
            "Main Camera",
            "Directional Light",
            "MatchSubScene"
        };
        foreach (string forbiddenRoot in forbiddenRoots)
        {
            if (roots.Any(rootObject => string.Equals(rootObject.name, forbiddenRoot, StringComparison.Ordinal)))
                throw new InvalidOperationException($"GameUI Step 1 must not include legacy/gameplay root {forbiddenRoot}.");
        }

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP1_VALIDATED scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 2")]
    public static void ValidateStep2()
    {
        ValidateStep1();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform shellTransform = scene.GetRootGameObjects()[0].transform.Find($"{CanvasName}/{ShellRootName}");
        if (shellTransform == null)
            throw new InvalidOperationException($"{ShellRootName} is missing.");

        if (shellTransform.childCount != RegionDefinitions.Length)
            throw new InvalidOperationException($"{ShellRootName} must contain exactly {RegionDefinitions.Length} shell regions.");

        HashSet<WarlineCaptureShellRegionId> seenRegionIds = new();
        for (int index = 0; index < RegionDefinitions.Length; index++)
        {
            ShellRegionDefinition definition = RegionDefinitions[index];
            Transform regionTransform = shellTransform.Find(definition.Name);
            if (regionTransform == null)
                throw new InvalidOperationException($"{ShellRootName} is missing region {definition.Name}.");
            if (regionTransform.GetSiblingIndex() != index)
                throw new InvalidOperationException($"{definition.Name} must keep sibling index {index} for deterministic draw order.");

            RectTransform regionRect = regionTransform.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = regionTransform.GetComponent<CanvasGroup>();
            WarlineCaptureShellRegionView regionView = regionTransform.GetComponent<WarlineCaptureShellRegionView>();
            if (regionRect == null || canvasGroup == null || regionView == null)
                throw new InvalidOperationException($"{definition.Name} must contain RectTransform, CanvasGroup, and WarlineCaptureShellRegionView.");

            if (regionView.RegionId != definition.Id)
                throw new InvalidOperationException($"{definition.Name} has region id {regionView.RegionId} instead of {definition.Id}.");
            if (regionView.RegionRoot != regionRect)
                throw new InvalidOperationException($"{definition.Name} region root reference is not self.");
            if (regionView.CanvasGroup != canvasGroup)
                throw new InvalidOperationException($"{definition.Name} CanvasGroup reference is not bound.");
            if (regionView.OffScreenDirection != definition.OffScreenDirection)
                throw new InvalidOperationException($"{definition.Name} offscreen direction is not configured.");
            if (!seenRegionIds.Add(regionView.RegionId))
                throw new InvalidOperationException($"Duplicate shell region id {regionView.RegionId}.");
            if (definition.Id == WarlineCaptureShellRegionId.RightRegion &&
                (regionRect.anchorMin != new Vector2(1f, 1f) ||
                 regionRect.anchorMax != new Vector2(1f, 1f) ||
                 regionRect.pivot != new Vector2(1f, 1f)))
            {
                throw new InvalidOperationException("RightRegion must be top-right anchored so it remains visible on 16:9 and wider aspect ratios.");
            }

            Transform contentTransform = regionTransform.Find(ContentRootName);
            if (contentTransform == null)
                throw new InvalidOperationException($"{definition.Name} is missing {ContentRootName}.");
            RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
            if (regionView.ContentRoot != contentRect)
                throw new InvalidOperationException($"{definition.Name} content root reference is not bound.");
            ValidateStretchRect(contentRect, $"{definition.Name}/{ContentRootName}");
        }

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP2_VALIDATED scene={ScenePath} regions={RegionDefinitions.Length}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 3")]
    public static void ValidateStep3()
    {
        ValidateStep2();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform shellTransform = scene.GetRootGameObjects()[0].transform.Find($"{CanvasName}/{ShellRootName}");
        if (shellTransform == null)
            throw new InvalidOperationException($"{ShellRootName} is missing.");

        WarlineCaptureUiMotionHostView motionHost = shellTransform.GetComponent<WarlineCaptureUiMotionHostView>();
        if (motionHost == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureUiMotionHostView in Step 3.");

        if (shellTransform.GetComponents<WarlineCaptureUiMotionHostView>().Length != 1)
            throw new InvalidOperationException($"{ShellRootName} must contain exactly one WarlineCaptureUiMotionHostView.");

        if (motionHost.DefaultDurationSeconds <= 0f)
            throw new InvalidOperationException("Motion host default duration must be positive.");
        if (motionHost.DefaultEnterEase != WarlineCaptureUiEase.EaseOutCubic)
            throw new InvalidOperationException("Motion host default enter ease must be EaseOutCubic.");
        if (motionHost.DefaultExitEase != WarlineCaptureUiEase.EaseInCubic)
            throw new InvalidOperationException("Motion host default exit ease must be EaseInCubic.");
        if (motionHost.DefaultSwapEase != WarlineCaptureUiEase.EaseInOutCubic)
            throw new InvalidOperationException("Motion host default swap ease must be EaseInOutCubic.");

        ValidateEase(WarlineCaptureUiEase.Linear);
        ValidateEase(WarlineCaptureUiEase.EaseInCubic);
        ValidateEase(WarlineCaptureUiEase.EaseOutCubic);
        ValidateEase(WarlineCaptureUiEase.EaseInOutCubic);
        ValidateEase(WarlineCaptureUiEase.EaseOutBackSubtle);

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP3_VALIDATED scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 5")]
    public static void ValidateStep5()
    {
        ValidateStep3();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform shellTransform = scene.GetRootGameObjects()[0].transform.Find($"{CanvasName}/{ShellRootName}");
        if (shellTransform == null)
            throw new InvalidOperationException($"{ShellRootName} is missing.");

        WarlineCaptureShellView shellView = shellTransform.GetComponent<WarlineCaptureShellView>();
        if (shellView == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureShellView in Step 5.");
        if (shellTransform.GetComponents<WarlineCaptureShellView>().Length != 1)
            throw new InvalidOperationException($"{ShellRootName} must contain exactly one WarlineCaptureShellView.");
        if (shellView.MotionHost == null)
            throw new InvalidOperationException($"{ShellRootName} ShellView must reference the motion host.");
        if (shellView.Regions == null || shellView.Regions.Count != RegionDefinitions.Length)
            throw new InvalidOperationException($"{ShellRootName} ShellView must reference all {RegionDefinitions.Length} regions.");

        for (int i = 0; i < RegionDefinitions.Length; i++)
        {
            if (shellView.Regions[i] == null || shellView.Regions[i].RegionId != RegionDefinitions[i].Id)
                throw new InvalidOperationException($"{ShellRootName} ShellView region index {i} is not bound to {RegionDefinitions[i].Id}.");
        }

        WarlineCaptureShellEcsBridgeView bridge = shellTransform.GetComponent<WarlineCaptureShellEcsBridgeView>();
        if (bridge == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureShellEcsBridgeView in Step 5.");
        if (shellTransform.GetComponents<WarlineCaptureShellEcsBridgeView>().Length != 1)
            throw new InvalidOperationException($"{ShellRootName} must contain exactly one WarlineCaptureShellEcsBridgeView.");

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP5_VALIDATED scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 7")]
    public static void ValidateStep7()
    {
        ValidateStep5();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform shellTransform = scene.GetRootGameObjects()[0].transform.Find($"{CanvasName}/{ShellRootName}");
        if (shellTransform == null)
            throw new InvalidOperationException($"{ShellRootName} is missing.");

        WarlineCaptureShellView shellView = shellTransform.GetComponent<WarlineCaptureShellView>();
        WarlineCaptureShellContentPresenterView contentPresenter = shellTransform.GetComponent<WarlineCaptureShellContentPresenterView>();
        if (contentPresenter == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureShellContentPresenterView in Step 7.");
        if (shellTransform.GetComponents<WarlineCaptureShellContentPresenterView>().Length != 1)
            throw new InvalidOperationException($"{ShellRootName} must contain exactly one WarlineCaptureShellContentPresenterView.");
        if (contentPresenter.ShellView != shellView)
            throw new InvalidOperationException("Content presenter must reference the shell view.");
        if (shellView.ContentPresenter != contentPresenter)
            throw new InvalidOperationException("Shell view must reference the content presenter.");

        ValidatePresenterPrefab(contentPresenter.LoadingContentPrefab, LoadingPrefabPath, "SCN01_LoadingContent");
        ValidatePresenterPrefab(contentPresenter.MainMenuContentPrefab, MainMenuPrefabPath, "SCN02_MainMenuContent");
        ValidatePresenterPrefab(contentPresenter.CommanderProfileContentPrefab, CommanderProfilePrefabPath, "SCN03_CommanderProfileContent");
        ValidatePresenterPrefab(contentPresenter.ArmoryContentPrefab, ArmoryPrefabPath, "SCN19_ArmoryContent");
        ValidatePresenterPrefab(contentPresenter.MatchHudContentPrefab, MatchHudPrefabPath, "SCN08_MatchHudContent");
        ValidatePresenterPrefab(contentPresenter.BuildDrawerPopupPrefab, BuildDrawerPopupPrefabPath, "SCN09_BuildDrawerPopup");
        ValidatePresenterPrefab(contentPresenter.ResultPopupPrefab, ResultPopupPrefabPath, "POP05_MissionResultPopup");

        WarlineCaptureGameUiSmokeDriverView smokeDriver = shellTransform.GetComponent<WarlineCaptureGameUiSmokeDriverView>();
        if (smokeDriver == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureGameUiSmokeDriverView in Step 7.");
        if (!smokeDriver.PlayOnStart)
            throw new InvalidOperationException("GameUI smoke driver must autoplay in the isolated Step 7 scene.");

        ValidateContentPresenterInstalls(shellView, contentPresenter);

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP7_VALIDATED scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 8")]
    public static void ValidateStep8()
    {
        ValidateStep7();

        Scene scene = EditorSceneManager.GetActiveScene();
        Transform root = scene.GetRootGameObjects()[0].transform;
        Transform canvasTransform = root.Find(CanvasName);
        Transform shellTransform = canvasTransform.Find(ShellRootName);
        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        WarlineCaptureShellView shellView = shellTransform.GetComponent<WarlineCaptureShellView>();
        WarlineCaptureShellContentPresenterView contentPresenter = shellTransform.GetComponent<WarlineCaptureShellContentPresenterView>();

        ValidateShellRegionLayout(canvasRect, shellView);
        ValidateMenuContentLayout(canvasRect, shellView, contentPresenter);
        ValidateMatchHudContentLayout(canvasRect, shellView, contentPresenter);
        ValidatePopupLayout(canvasRect, shellView, contentPresenter);

        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP8_VALIDATED scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Captures Step 9")]
    public static void ValidateStep9Captures()
    {
        ValidateCaptureFile("GameUI_Loading_Stable.png");
        ValidateCaptureFile("GameUI_MainMenu_Stable.png");
        ValidateCaptureFile($"{CommanderProfileCaptureFolder}/GameUI_CommanderProfile_Stable.png");
        ValidateCaptureFile("GameUI_MatchHud_Stable.png");
        ValidateCaptureFile("GameUI_ResultPopup_Stable.png");
        ValidateCaptureFile("GameUI_ReturnedMainMenu_Stable.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{MainMenuCaptureFolder}/GameUI_MainMenu_{MainMenuCaptureResolutions[i].Name}.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{CommanderProfileResponsiveCaptureFolder}/GameUI_CommanderProfile_{MainMenuCaptureResolutions[i].Name}.png");
        ValidateCaptureFile($"{ArmoryCaptureFolder}/GameUI_Armory_Stable.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{ArmoryResponsiveCaptureFolder}/GameUI_Armory_{MainMenuCaptureResolutions[i].Name}.png");
        ValidateCaptureFile($"{MatchHudCaptureFolder}/GameUI_MatchHud_Stable.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{MatchHudResponsiveCaptureFolder}/GameUI_MatchHud_{MainMenuCaptureResolutions[i].Name}.png");
        ValidateCaptureFile($"{BuildDrawerCaptureFolder}/GameUI_BuildDrawer_Stable.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{BuildDrawerResponsiveCaptureFolder}/GameUI_BuildDrawer_{MainMenuCaptureResolutions[i].Name}.png");
        ValidateCaptureFile($"{MissionResultCaptureFolder}/GameUI_MissionResult_Stable.png");
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
            ValidateCaptureFile($"{MissionResultResponsiveCaptureFolder}/GameUI_MissionResult_{MainMenuCaptureResolutions[i].Name}.png");
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP9_VALIDATED captures={5 + MainMenuCaptureResolutions.Length} folder={CaptureFolder}");
    }

    private static void CreateEventSystem(Transform parent)
    {
        GameObject eventSystemObject = new(EventSystemName);
        eventSystemObject.transform.SetParent(parent, false);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static Camera CreateUiCamera(Transform parent)
    {
        GameObject cameraObject = new(CameraName, typeof(Camera));
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
        cameraObject.transform.localRotation = Quaternion.identity;
        cameraObject.transform.localScale = Vector3.one;

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = ShellHeight * 0.5f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.depth = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        return camera;
    }

    private static GameObject CreateCanvas(Transform parent, Camera uiCamera)
    {
        GameObject canvasObject = new(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        Stretch(rect);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ShellWidth, ShellHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        return canvasObject;
    }

    private static GameObject CreateShellRoot(Transform parent)
    {
        GameObject shellRoot = new(ShellRootName, typeof(RectTransform));
        shellRoot.transform.SetParent(parent, false);
        Stretch(shellRoot.GetComponent<RectTransform>());
        return shellRoot;
    }

    private static void CreateShellRegions(Transform shellRoot)
    {
        foreach (ShellRegionDefinition definition in RegionDefinitions)
        {
            GameObject regionObject = new(definition.Name, typeof(RectTransform), typeof(CanvasGroup), typeof(WarlineCaptureShellRegionView));
            regionObject.transform.SetParent(shellRoot, false);

            RectTransform regionRect = regionObject.GetComponent<RectTransform>();
            if (definition.IsStretch)
                Stretch(regionRect);
            else if (definition.Id == WarlineCaptureShellRegionId.HeaderRegion)
                ApplyTopHorizontalStretchRect(regionRect, definition.Rect);
            else if (definition.Id == WarlineCaptureShellRegionId.RightRegion)
                ApplyTopRightRect(regionRect, definition.Rect);
            else
                ApplyTopLeftRect(regionRect, definition.Rect);

            CanvasGroup canvasGroup = regionObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            GameObject contentObject = new(ContentRootName, typeof(RectTransform));
            contentObject.transform.SetParent(regionObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            Stretch(contentRect);

            WarlineCaptureShellRegionView view = regionObject.GetComponent<WarlineCaptureShellRegionView>();
            view.Configure(definition.Id, regionRect, contentRect, canvasGroup, definition.OffScreenDirection);
            EditorUtility.SetDirty(view);
        }
    }

    private static void AddMotionHost(GameObject shellRoot)
    {
        shellRoot.AddComponent<WarlineCaptureUiMotionHostView>();
    }

    private static void AddShellViewAndBridge(GameObject shellRoot)
    {
        WarlineCaptureUiMotionHostView motionHost = shellRoot.GetComponent<WarlineCaptureUiMotionHostView>();
        WarlineCaptureShellRegionView[] regionViews = new WarlineCaptureShellRegionView[RegionDefinitions.Length];
        for (int i = 0; i < RegionDefinitions.Length; i++)
        {
            Transform regionTransform = shellRoot.transform.Find(RegionDefinitions[i].Name);
            if (regionTransform == null)
                throw new InvalidOperationException($"{ShellRootName} is missing region {RegionDefinitions[i].Name}.");
            regionViews[i] = regionTransform.GetComponent<WarlineCaptureShellRegionView>();
        }

        WarlineCaptureShellView shellView = shellRoot.AddComponent<WarlineCaptureShellView>();
        shellView.Configure(motionHost, regionViews);

        WarlineCaptureShellEcsBridgeView bridge = shellRoot.AddComponent<WarlineCaptureShellEcsBridgeView>();
        bridge.Configure(shellView);
    }

    private static void AddContentPresenterAndSmoke(GameObject shellRoot)
    {
        WarlineCaptureShellView shellView = shellRoot.GetComponent<WarlineCaptureShellView>();
        if (shellView == null)
            throw new InvalidOperationException($"{ShellRootName} must contain WarlineCaptureShellView before Step 7 content wiring.");

        WarlineCaptureShellContentPresenterView presenter = shellRoot.AddComponent<WarlineCaptureShellContentPresenterView>();
        presenter.Configure(
            shellView,
            RequirePrefab(LoadingPrefabPath),
            RequirePrefab(MainMenuPrefabPath),
            RequirePrefab(CommanderProfilePrefabPath),
            RequirePrefab(ArmoryPrefabPath),
            RequirePrefab(MatchHudPrefabPath),
            RequirePrefab(BuildDrawerPopupPrefabPath),
            RequirePrefab(ResultPopupPrefabPath));
        shellView.SetContentPresenter(presenter);

        WarlineCaptureGameUiSmokeDriverView smokeDriver = shellRoot.AddComponent<WarlineCaptureGameUiSmokeDriverView>();
        smokeDriver.Configure(true, 2f, 0.25f);
    }

    private static Transform RequireChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            throw new InvalidOperationException($"{parent.name} is missing child {childName}.");
        return child;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ApplyTopLeftRect(RectTransform rect, Rect topLeftRect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(topLeftRect.x, -topLeftRect.y);
        rect.sizeDelta = new Vector2(topLeftRect.width, topLeftRect.height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ApplyTopHorizontalStretchRect(RectTransform rect, Rect topLeftRect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topLeftRect.y);
        rect.sizeDelta = new Vector2(0f, topLeftRect.height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ApplyTopRightRect(RectTransform rect, Rect topLeftRect)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(topLeftRect.xMax - ShellWidth, -topLeftRect.y);
        rect.sizeDelta = new Vector2(topLeftRect.width, topLeftRect.height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ValidateStretchRect(RectTransform rect, string name)
    {
        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
            throw new InvalidOperationException($"{name} must stretch to its parent.");
        if (rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            throw new InvalidOperationException($"{name} must have zero offsets.");
        if (rect.localScale != Vector3.one)
            throw new InvalidOperationException($"{name} must have unit scale.");
    }

    private static void ValidateEase(WarlineCaptureUiEase ease)
    {
        float start = WarlineCaptureUiMotionHostView.EvaluateEase(ease, 0f);
        float end = WarlineCaptureUiMotionHostView.EvaluateEase(ease, 1f);
        if (Mathf.Abs(start) > 0.001f)
            throw new InvalidOperationException($"{ease} must evaluate 0 at progress 0.");
        if (Mathf.Abs(end - 1f) > 0.001f)
            throw new InvalidOperationException($"{ease} must evaluate 1 at progress 1.");
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException($"Missing GameUI prefab at {path}.");

        return prefab;
    }

    private static void ValidatePresenterPrefab(GameObject prefab, string path, string expectedName)
    {
        if (prefab == null)
            throw new InvalidOperationException($"Content presenter is missing prefab reference {path}.");
        if (prefab != AssetDatabase.LoadAssetAtPath<GameObject>(path))
            throw new InvalidOperationException($"Content presenter prefab reference must point to {path}.");
        if (prefab.name != expectedName)
            throw new InvalidOperationException($"{path} must reference prefab root {expectedName}.");
    }

    private static void ValidateContentPresenterInstalls(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.ShowLoading }
        });
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.LoadingLayer, "SCN01_LoadingContent");

        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.MiddleRegion, "MiddleContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.RightRegion, "RightContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.FooterRegion, "FooterContent");
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.FooterRegion, "FooterContent/DeployCommandButton", UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.RightRegion, "RightContent/CommanderPortraitButton", UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.CommanderProfile);

        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.CommanderProfile);
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion, "MenuBackgroundContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.MiddleRegion, "MiddleContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.RightRegion, "RightContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.FooterRegion, "FooterContent");
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent/BackButton/Hotspot", UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);

        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.Armory);
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion, "MenuBackgroundContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.MiddleRegion, "MiddleContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.RightRegion, "RightContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.FooterRegion, "FooterContent");
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent/ArmoryTitleBlock/BackHotspot", UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.CommanderProfile);

        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.RightRegion, "RightContent");
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.FooterRegion, "FooterContent");
        RequireRegionEmpty(shellView, WarlineCaptureShellRegionId.MiddleRegion);

        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.ShowPopup }
        });
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.PopupLayer, "POP05_MissionResultPopup");

        contentPresenter.InstallBuildDrawerPopup();
        RequireRegionChild(shellView, WarlineCaptureShellRegionId.PopupLayer, "SCN09_BuildDrawerPopup");
    }

    private static void RequireRegionChild(WarlineCaptureShellView shellView, WarlineCaptureShellRegionId regionId, string childName)
    {
        if (!shellView.TryGetRegion(regionId, out WarlineCaptureShellRegionView region) || region.ContentRoot == null)
            throw new InvalidOperationException($"Missing shell region {regionId}.");
        if (region.ContentRoot.childCount != 1)
            throw new InvalidOperationException($"{regionId} must contain exactly one content child after presenter install.");
        if (region.ContentRoot.GetChild(0).name != childName)
            throw new InvalidOperationException($"{regionId} must contain {childName} after presenter install.");
    }

    private static void RequireRegionEmpty(WarlineCaptureShellView shellView, WarlineCaptureShellRegionId regionId)
    {
        if (!shellView.TryGetRegion(regionId, out WarlineCaptureShellRegionView region) || region.ContentRoot == null)
            throw new InvalidOperationException($"Missing shell region {regionId}.");
        if (region.ContentRoot.childCount != 0)
            throw new InvalidOperationException($"{regionId} must be empty after presenter install.");
    }

    private static void RequireRouteButton(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellRegionId regionId,
        string path,
        UiShellRouteIntent intent,
        WarlineCaptureRoute route)
    {
        WarlineCaptureShellRegionView region = RequireRegion(shellView, regionId);
        Transform buttonTransform = region.ContentRoot.Find(path);
        if (buttonTransform == null)
            throw new InvalidOperationException($"{regionId} must contain route button {path}.");

        if (buttonTransform.GetComponent<Button>() == null)
            throw new InvalidOperationException($"{path} must contain a Unity Button.");

        WarlineCaptureShellRouteButtonView routeButton = buttonTransform.GetComponent<WarlineCaptureShellRouteButtonView>();
        if (routeButton == null)
            throw new InvalidOperationException($"{path} must contain WarlineCaptureShellRouteButtonView.");
        if (routeButton.Intent != intent || routeButton.Route != route)
            throw new InvalidOperationException($"{path} must route to {intent}/{route}.");
    }

    private static void ValidateShellRegionLayout(RectTransform canvasRect, WarlineCaptureShellView shellView)
    {
        Rect canvasBounds = DesignCanvasBounds();
        List<(WarlineCaptureShellRegionId Id, Rect Rect)> majorRegions = new();

        foreach (ShellRegionDefinition definition in RegionDefinitions)
        {
            if (!shellView.TryGetRegion(definition.Id, out WarlineCaptureShellRegionView region) || region.RegionRoot == null)
                throw new InvalidOperationException($"Missing shell region {definition.Id} for Step 8 layout validation.");

            Rect actual = GetReferenceTopLeftRect(region.RegionRoot);
            Rect expected = definition.IsStretch ? canvasBounds : definition.Rect;
            ValidateRectNear(actual, expected, $"{definition.Name} rect");
            ValidateRectInside(actual, canvasBounds, $"{definition.Name} rect", 0.5f);

            if (!definition.IsStretch)
                majorRegions.Add((definition.Id, actual));
        }

        for (int i = 0; i < majorRegions.Count; i++)
        {
            for (int j = i + 1; j < majorRegions.Count; j++)
            {
                Rect overlap = RectIntersection(majorRegions[i].Rect, majorRegions[j].Rect);
                if (overlap.width > 0.5f && overlap.height > 0.5f)
                    throw new InvalidOperationException($"{majorRegions[i].Id} overlaps {majorRegions[j].Id} by {overlap.width:0.##}x{overlap.height:0.##}.");
            }
        }
    }

    private static void ValidateMenuContentLayout(
        RectTransform canvasRect,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });

        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.HeaderRegion, canvasRect, "HeaderContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion, canvasRect, "MenuBackgroundContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.LeftRegion, canvasRect, "LeftContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MiddleRegion, canvasRect, "MiddleContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.RightRegion, canvasRect, "RightContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.FooterRegion, canvasRect, "FooterContent");

        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.CommanderProfile);
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion, canvasRect, "MenuBackgroundContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.HeaderRegion, canvasRect, "HeaderContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.LeftRegion, canvasRect, "LeftContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MiddleRegion, canvasRect, "MiddleContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.RightRegion, canvasRect, "RightContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.FooterRegion, canvasRect, "FooterContent");
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.HeaderRegion, "HeaderContent/BackButton/Hotspot", UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.MainMenu);

        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.Armory);
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion, canvasRect, "MenuBackgroundContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.HeaderRegion, canvasRect, "HeaderContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.LeftRegion, canvasRect, "LeftContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.MiddleRegion, canvasRect, "MiddleContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.RightRegion, canvasRect, "RightContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.FooterRegion, canvasRect, "FooterContent");
        RequireRouteButton(shellView, WarlineCaptureShellRegionId.LeftRegion, "LeftContent/ArmoryTitleBlock/BackHotspot", UiShellRouteIntent.OpenMenuRoute, WarlineCaptureRoute.CommanderProfile);
    }

    private static void ValidateMatchHudContentLayout(
        RectTransform canvasRect,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.HeaderRegion, canvasRect, "HeaderContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.LeftRegion, canvasRect, "LeftContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.RightRegion, canvasRect, "RightContent");
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.FooterRegion, canvasRect, "FooterContent");
        RequireRegionEmpty(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        RequireRegionEmpty(shellView, WarlineCaptureShellRegionId.MiddleRegion);
    }

    private static void ValidatePopupLayout(
        RectTransform canvasRect,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.ShowPopup }
        });

        WarlineCaptureShellRegionView popupRegion = RequireRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
        ValidateRegionContent(shellView, WarlineCaptureShellRegionId.PopupLayer, canvasRect, "POP05_MissionResultPopup");

        RectTransform popupRoot = popupRegion.ContentRoot.GetChild(0).GetComponent<RectTransform>();
        RectTransform popupFrame = popupRoot.Find("PopupFrame") as RectTransform;
        if (popupFrame == null)
            throw new InvalidOperationException("Mission result popup must contain PopupFrame.");
        if (popupFrame.anchorMin != new Vector2(0.5f, 0.5f) || popupFrame.anchorMax != new Vector2(0.5f, 0.5f))
            throw new InvalidOperationException("PopupFrame must be center-anchored.");
        if (popupFrame.pivot != new Vector2(0.5f, 0.5f))
            throw new InvalidOperationException("PopupFrame pivot must be centered for scale-in popup motion.");
        if (popupFrame.anchoredPosition.sqrMagnitude > 0.25f)
            throw new InvalidOperationException("PopupFrame must be centered in PopupLayer.");

        Transform continueButton = popupFrame.Find("Actions/ContinueButton");
        if (continueButton == null)
            throw new InvalidOperationException("Mission result popup must contain Actions/ContinueButton.");
        if (continueButton.GetComponent<Button>() == null)
            throw new InvalidOperationException("Mission result ContinueButton must contain a Unity Button.");
        if (continueButton.GetComponent<WarlineCaptureShellResultConfirmButtonView>() == null)
            throw new InvalidOperationException("Mission result ContinueButton must contain WarlineCaptureShellResultConfirmButtonView.");
    }

    private static void PrepareLoadingStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        ClearAllRegionContent(shellView);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.ShowLoading }
        });
        ShowRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        HideRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareMainMenuStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        ClearAllRegionContent(shellView);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });
        HideRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareCommanderProfileStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        ClearAllRegionContent(shellView);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });
        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.CommanderProfile);
        HideRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareArmoryStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        ClearAllRegionContent(shellView);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });
        contentPresenter.InstallMenuRouteBody(WarlineCaptureRoute.Armory);
        HideRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareMatchHudStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        ClearAllRegionContent(shellView);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });
        HideRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        HideRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareResultPopupStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareMatchHudStable(shellView, contentPresenter);
        contentPresenter.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.ShowPopup }
        });
        HideRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void PrepareBuildDrawerStable(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareMatchHudStable(shellView, contentPresenter);
        contentPresenter.InstallBuildDrawerPopup();
        HideRegion(shellView, WarlineCaptureShellRegionId.LoadingLayer);
        HideRegion(shellView, WarlineCaptureShellRegionId.MenuBackgroundRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.HeaderRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.LeftRegion);
        HideRegion(shellView, WarlineCaptureShellRegionId.MiddleRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.RightRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.FooterRegion);
        ShowRegion(shellView, WarlineCaptureShellRegionId.PopupLayer);
    }

    private static void ClearAllRegionContent(WarlineCaptureShellView shellView)
    {
        foreach (ShellRegionDefinition definition in RegionDefinitions)
        {
            if (!shellView.TryGetRegion(definition.Id, out WarlineCaptureShellRegionView region) || region.ContentRoot == null)
                continue;

            ClearChildren(region.ContentRoot);
        }
    }

    private static void ShowRegion(WarlineCaptureShellView shellView, WarlineCaptureShellRegionId regionId)
    {
        WarlineCaptureShellRegionView region = RequireRegion(shellView, regionId);
        region.ResetVisualState();
        region.CanvasGroup.alpha = 1f;
        region.RegionRoot.localScale = Vector3.one;
    }

    private static void HideRegion(WarlineCaptureShellView shellView, WarlineCaptureShellRegionId regionId)
    {
        WarlineCaptureShellRegionView region = RequireRegion(shellView, regionId);
        region.ResetVisualState();
        region.CanvasGroup.alpha = 0f;
        region.CanvasGroup.interactable = false;
        region.CanvasGroup.blocksRaycasts = false;
    }

    private static void CaptureCamera(Camera camera, string relativePath)
    {
        CaptureCamera(camera, relativePath, CaptureWidth, CaptureHeight);
    }

    private static void CaptureCamera(Camera camera, string relativePath, int width, int height)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        RenderTexture renderTexture = null;
        Texture2D texture = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;

        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            renderTexture.Create();

            camera.targetTexture = renderTexture;
            camera.aspect = width / (float)height;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, camera.backgroundColor);
            Canvas.ForceUpdateCanvases();
            camera.Render();

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Debug.Log($"WARLINECAPTURE_GAMEUI_STEP9_CAPTURE path={relativePath}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static void CaptureMainMenuAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareMainMenuStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{MainMenuCaptureFolder}/GameUI_MainMenu_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void CaptureCommanderProfileAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareCommanderProfileStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{CommanderProfileResponsiveCaptureFolder}/GameUI_CommanderProfile_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void CaptureArmoryAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareArmoryStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{ArmoryResponsiveCaptureFolder}/GameUI_Armory_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void CaptureMatchHudAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareMatchHudStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{MatchHudResponsiveCaptureFolder}/GameUI_MatchHud_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void CaptureBuildDrawerAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareBuildDrawerStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{BuildDrawerResponsiveCaptureFolder}/GameUI_BuildDrawer_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void CaptureMissionResultAspectSamples(
        Camera camera,
        WarlineCaptureShellView shellView,
        WarlineCaptureShellContentPresenterView contentPresenter)
    {
        PrepareResultPopupStable(shellView, contentPresenter);
        for (int i = 0; i < MainMenuCaptureResolutions.Length; i++)
        {
            CaptureResolution resolution = MainMenuCaptureResolutions[i];
            CaptureCamera(camera, $"{MissionResultResponsiveCaptureFolder}/GameUI_MissionResult_{resolution.Name}.png", resolution.Width, resolution.Height);
        }
    }

    private static void ValidateCaptureFile(string fileName)
    {
        string relativePath = fileName.StartsWith("Design/", StringComparison.Ordinal) ? fileName : $"{CaptureFolder}/{fileName}";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (!File.Exists(fullPath))
            throw new InvalidOperationException($"Missing GameUI Step 9 capture {relativePath}.");

        FileInfo info = new(fullPath);
        if (info.Length <= 1024)
            throw new InvalidOperationException($"GameUI Step 9 capture is unexpectedly small: {relativePath} bytes={info.Length}.");
    }

    private static void ValidateRegionContent(
        WarlineCaptureShellView shellView,
        WarlineCaptureShellRegionId regionId,
        RectTransform canvasRect,
        string expectedChildName)
    {
        WarlineCaptureShellRegionView region = RequireRegion(shellView, regionId);
        RequireRegionChild(shellView, regionId, expectedChildName);

        Rect regionRect = GetReferenceTopLeftRect(region.RegionRoot);
        Rect contentRect = GetReferenceTopLeftRect(region.ContentRoot.GetChild(0).GetComponent<RectTransform>());
        Rect canvasBounds = DesignCanvasBounds();

        ValidateRectInside(regionRect, canvasBounds, $"{regionId} region", 0.5f);
        ValidateRectInside(contentRect, regionRect, $"{regionId}/{expectedChildName}", 1f);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static WarlineCaptureShellRegionView RequireRegion(WarlineCaptureShellView shellView, WarlineCaptureShellRegionId regionId)
    {
        if (!shellView.TryGetRegion(regionId, out WarlineCaptureShellRegionView region) || region == null)
            throw new InvalidOperationException($"Missing shell region {regionId}.");
        return region;
    }

    private static Rect GetReferenceTopLeftRect(RectTransform rect)
    {
        if (rect == null)
            return Rect.zero;
        if (rect.name == CanvasName)
            return DesignCanvasBounds();

        RectTransform parent = rect.parent as RectTransform;
        Rect parentRect = parent == null ? DesignCanvasBounds() : GetReferenceTopLeftRect(parent);

        float width;
        float height;
        float x;
        float y;

        if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one)
        {
            x = parentRect.x + rect.offsetMin.x;
            y = parentRect.y - rect.offsetMax.y;
            width = parentRect.width - rect.offsetMin.x + rect.offsetMax.x;
            height = parentRect.height - rect.offsetMin.y + rect.offsetMax.y;
            return new Rect(x, y, width, height);
        }

        if (rect.anchorMin == new Vector2(0f, 1f) && rect.anchorMax == new Vector2(1f, 1f))
        {
            width = parentRect.width + rect.sizeDelta.x;
            height = rect.sizeDelta.y;
            x = parentRect.x + rect.anchoredPosition.x - rect.pivot.x * rect.sizeDelta.x;
            y = parentRect.y - rect.anchoredPosition.y - (1f - rect.pivot.y) * height;
            return new Rect(x, y, width, height);
        }

        width = rect.rect.width;
        height = rect.rect.height;
        float anchorX = parentRect.x + parentRect.width * rect.anchorMin.x;
        float anchorY = parentRect.y + parentRect.height * (1f - rect.anchorMax.y);
        x = anchorX + rect.anchoredPosition.x - rect.pivot.x * width;
        y = anchorY - rect.anchoredPosition.y - (1f - rect.pivot.y) * height;
        return new Rect(x, y, width, height);
    }

    private static Rect DesignCanvasBounds() => new(0f, 0f, ShellWidth, ShellHeight);

    private static void ValidateRectInside(Rect inner, Rect outer, string name, float tolerance)
    {
        if (inner.xMin < outer.xMin - tolerance ||
            inner.yMin < outer.yMin - tolerance ||
            inner.xMax > outer.xMax + tolerance ||
            inner.yMax > outer.yMax + tolerance)
        {
            throw new InvalidOperationException($"{name} must stay inside bounds. inner={inner} outer={outer}");
        }
    }

    private static void ValidateRectNear(Rect actual, Rect expected, string name)
    {
        const float tolerance = 0.5f;
        if (Mathf.Abs(actual.x - expected.x) > tolerance ||
            Mathf.Abs(actual.y - expected.y) > tolerance ||
            Mathf.Abs(actual.width - expected.width) > tolerance ||
            Mathf.Abs(actual.height - expected.height) > tolerance)
        {
            throw new InvalidOperationException($"{name} changed unexpectedly. actual={actual} expected={expected}");
        }
    }

    private static Rect RectIntersection(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin)
            return Rect.zero;
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private readonly struct ShellRegionDefinition
    {
        public ShellRegionDefinition(WarlineCaptureShellRegionId id, string name, Vector2 offScreenDirection, Rect rect)
        {
            Id = id;
            Name = name;
            OffScreenDirection = offScreenDirection;
            Rect = rect;
            IsStretch = rect == StretchRegion;
        }

        public WarlineCaptureShellRegionId Id { get; }
        public string Name { get; }
        public Vector2 OffScreenDirection { get; }
        public Rect Rect { get; }
        public bool IsStretch { get; }
    }

    private readonly struct CaptureResolution
    {
        public CaptureResolution(int width, int height, string name)
        {
            Width = width;
            Height = height;
            Name = name;
        }

        public int Width { get; }
        public int Height { get; }
        public string Name { get; }
    }
}
#endif
