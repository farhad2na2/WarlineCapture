#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    private const string ScenePath = "Assets/Game/Scenes/GameUI.unity";
    private const string RootName = "GameUIRoot";
    private const string CameraName = "GameUICamera";
    private const string EventSystemName = "EventSystem";
    private const string CanvasName = "GameUICanvas";
    private const string ShellRootName = "WarlineCaptureRuntimeShell";
    private const string ContentRootName = "ContentRoot";

    private static readonly Rect StretchRegion = new(0f, 0f, 2400f, 1080f);

    private static readonly ShellRegionDefinition[] RegionDefinitions =
    {
        new(WarlineCaptureShellRegionId.LoadingLayer, "LoadingLayer", new Vector2(0f, -1f), StretchRegion),
        new(WarlineCaptureShellRegionId.HeaderRegion, "HeaderRegion", new Vector2(0f, 1f), new Rect(0f, 0f, 2400f, 140f)),
        new(WarlineCaptureShellRegionId.LeftRegion, "LeftRegion", new Vector2(-1f, 0f), new Rect(0f, 140f, 360f, 820f)),
        new(WarlineCaptureShellRegionId.MiddleRegion, "MiddleRegion", Vector2.zero, new Rect(360f, 140f, 1680f, 820f)),
        new(WarlineCaptureShellRegionId.RightRegion, "RightRegion", new Vector2(1f, 0f), new Rect(2040f, 140f, 360f, 820f)),
        new(WarlineCaptureShellRegionId.FooterRegion, "FooterRegion", new Vector2(0f, -1f), new Rect(0f, 960f, 2400f, 120f)),
        new(WarlineCaptureShellRegionId.PopupLayer, "PopupLayer", Vector2.zero, StretchRegion)
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
        if (scaler.referenceResolution != new Vector2(2400f, 1080f))
            throw new InvalidOperationException($"{CanvasName} must use the 2400x1080 shell reference resolution.");

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
            "GameSubScene"
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
        camera.orthographicSize = 540f;
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
        scaler.referenceResolution = new Vector2(2400f, 1080f);
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
}
#endif
