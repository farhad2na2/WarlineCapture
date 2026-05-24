#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WarlineCaptureGameUiSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/GameUI.unity";
    private const string RootName = "GameUIRoot";
    private const string EventSystemName = "EventSystem";
    private const string CanvasName = "GameUICanvas";
    private const string ShellRootName = "WarlineCaptureRuntimeShell";

    [MenuItem("WarlineCapture/UI/Build GameUI Scene Step 1")]
    public static void BuildStep1()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new(RootName);

        CreateEventSystem(root.transform);
        GameObject canvasObject = CreateCanvas(root.transform);
        CreateShellRoot(canvasObject.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException($"Failed to save GameUI scene at {ScenePath}.");

        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateStep1();
        Debug.Log($"WARLINECAPTURE_GAMEUI_SCENE_STEP1_BUILT scene={ScenePath}");
    }

    [MenuItem("WarlineCapture/UI/Validate GameUI Scene Step 1")]
    public static void ValidateStep1()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Length != 1 || roots[0].name != RootName)
            throw new InvalidOperationException($"GameUI scene must contain exactly one root named {RootName}.");

        Transform root = roots[0].transform;
        Transform eventSystemTransform = RequireChild(root, EventSystemName);
        Transform canvasTransform = RequireChild(root, CanvasName);
        Transform shellTransform = RequireChild(canvasTransform, ShellRootName);

        EventSystem eventSystem = eventSystemTransform.GetComponent<EventSystem>();
        if (eventSystem == null)
            throw new InvalidOperationException($"{EventSystemName} must contain an EventSystem component.");

        BaseInputModule inputModule = eventSystemTransform.GetComponent<BaseInputModule>();
        if (inputModule == null)
            throw new InvalidOperationException($"{EventSystemName} must contain a UI input module.");

        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas == null)
            throw new InvalidOperationException($"{CanvasName} must contain a Canvas component.");
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            throw new InvalidOperationException($"{CanvasName} must use ScreenSpaceOverlay for the isolated UI shell scene.");

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

    private static void CreateEventSystem(Transform parent)
    {
        GameObject eventSystemObject = new(EventSystemName);
        eventSystemObject.transform.SetParent(parent, false);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        Stretch(rect);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        return canvasObject;
    }

    private static void CreateShellRoot(Transform parent)
    {
        GameObject shellRoot = new(ShellRootName, typeof(RectTransform));
        shellRoot.transform.SetParent(parent, false);
        Stretch(shellRoot.GetComponent<RectTransform>());
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

    private static void ValidateStretchRect(RectTransform rect, string name)
    {
        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
            throw new InvalidOperationException($"{name} must stretch to its parent.");
        if (rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            throw new InvalidOperationException($"{name} must have zero offsets.");
        if (rect.localScale != Vector3.one)
            throw new InvalidOperationException($"{name} must have unit scale.");
    }
}
#endif
