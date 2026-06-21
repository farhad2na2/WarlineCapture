using System;
using System.IO;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CanvasMenuFallbackValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
    private const string ScreenshotPath = "/private/tmp/warline-canvas-menu-fallback.png";

    private static int frameCount;
    private static int screenshotRequestedFrame;
    private static double startedAt;
    private static bool completed;
    private static bool screenshotRequested;

    public static void Run()
    {
        try
        {
            RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

            SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            if (File.Exists(ScreenshotPath))
                File.Delete(ScreenshotPath);

            frameCount = 0;
            screenshotRequestedFrame = 0;
            startedAt = EditorApplication.timeSinceStartup;
            completed = false;
            screenshotRequested = false;
            EditorApplication.update -= Continue;
            EditorApplication.update += Continue;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasMenuFallbackValidation] result=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void Continue()
    {
        if (completed)
            return;

        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 45d)
            {
                Complete(false, $"Timed out before Canvas menu deploy UI became visible. {DescribeRuntimeState()}");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            frameCount++;
            if (frameCount < 45)
                return;

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                Complete(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            if (bootstrap.IsUiToolkitMode)
            {
                Complete(false, "RuntimeUiConfig is not in Canvas mode.");
                return;
            }

            Canvas canvas = bootstrap.UiCanvas;
            if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
            {
                Complete(false, DescribeCanvasState(canvas, "Canvas is not active and enabled."));
                return;
            }

            Button deployButton = FindVisibleDeployButton();
            if (deployButton == null)
                return;

            if (!deployButton.IsInteractable())
            {
                Complete(false, "Deploy command view is mounted, but its Unity UI Button is not interactable.");
                return;
            }

            if (!screenshotRequested)
            {
                ScreenCapture.CaptureScreenshot(ScreenshotPath);
                screenshotRequested = true;
                screenshotRequestedFrame = frameCount;
                return;
            }

            if (frameCount - screenshotRequestedFrame < 12)
                return;

            if (!File.Exists(ScreenshotPath))
                return;

            Texture2D screenshot = new(2, 2, TextureFormat.RGBA32, false);
            if (!screenshot.LoadImage(File.ReadAllBytes(ScreenshotPath)))
            {
                UnityEngine.Object.DestroyImmediate(screenshot);
                Complete(false, $"Captured Canvas menu screenshot could not be read. path={ScreenshotPath}");
                return;
            }

            float luma = EstimateAverageLuma(screenshot);
            UnityEngine.Object.DestroyImmediate(screenshot);
            if (luma < 0.05f)
            {
                Complete(false, $"Captured Canvas menu screenshot is still black or near-black. luma={luma:0.000} path={ScreenshotPath}");
                return;
            }

            Complete(true, $"Canvas menu deploy UI is visible. luma={luma:0.000} path={ScreenshotPath}");
        }
        catch (Exception exception)
        {
            Complete(false, exception.ToString());
        }
    }

    private static Button FindVisibleDeployButton()
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.isActiveAndEnabled)
                continue;

            string objectName = button.gameObject.name;
            if (!string.Equals(objectName, "DeployCommandButton", StringComparison.Ordinal) &&
                !string.Equals(objectName, "DeployOperationButton", StringComparison.Ordinal))
            {
                continue;
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                continue;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            if (Vector3.Distance(corners[0], corners[2]) <= 1f)
                continue;

            return button;
        }

        return null;
    }

    private static void Complete(bool success, string message)
    {
        if (completed)
            return;

        completed = true;
        EditorApplication.update -= Continue;
        if (success)
            Debug.Log($"[CanvasMenuFallbackValidation] result=Passed {message}");
        else
            Debug.LogError($"[CanvasMenuFallbackValidation] result=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static string DescribeCanvasState(Canvas canvas, string prefix)
    {
        if (canvas == null)
            return $"{prefix} canvas=null";

        return $"{prefix} enabled={canvas.enabled} active={canvas.gameObject.activeInHierarchy} renderMode={canvas.renderMode} children={canvas.transform.childCount}";
    }

    private static string DescribeRuntimeState()
    {
        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
        Canvas canvas = bootstrap != null ? bootstrap.UiCanvas : null;
        Button[] activeButtonObjects = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        int activeButtons = activeButtonObjects.Length;
        int shellChildren = bootstrap != null && bootstrap.ShellView != null
            ? bootstrap.ShellView.transform.childCount
            : -1;
        int contentVersion = bootstrap != null && bootstrap.ContentSystem != null
            ? bootstrap.ContentSystem.ContentVersion
            : -1;

        string shellState = "shellState=unavailable";
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiShellPresentationCommandComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                Entity boundary = query.GetSingletonEntity();
                UiShellStateComponent state = entityManager.GetComponentData<UiShellStateComponent>(boundary);
                int pendingCommands = entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary).Length;
                shellState = $"mode={state.CurrentMode} route={state.ActiveRoute} phase={state.Phase} running={state.IsTransitionRunning} seq={state.TransitionSequenceId} pendingCommands={pendingCommands}";
            }
        }

        return $"{DescribeCanvasState(canvas, "canvas")} shellChildren={shellChildren} contentVersion={contentVersion} activeButtons={activeButtons} buttonNames=[{DescribeButtonNames(activeButtonObjects)}] regions=[{DescribeRegions(bootstrap)}] {shellState}";
    }

    private static string DescribeButtonNames(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0)
            return "";

        StringBuilder builder = new();
        int count = Mathf.Min(buttons.Length, 12);
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                builder.Append(',');

            Button button = buttons[i];
            builder.Append(button != null ? button.gameObject.name : "null");
        }

        if (buttons.Length > count)
            builder.Append(",...");

        return builder.ToString();
    }

    private static string DescribeRegions(MenuBootstrapView bootstrap)
    {
        if (bootstrap == null || bootstrap.ShellView == null || bootstrap.ShellView.Regions == null)
            return "";

        StringBuilder builder = new();
        for (int i = 0; i < bootstrap.ShellView.Regions.Count; i++)
        {
            UIShellRegionView region = bootstrap.ShellView.Regions[i];
            if (region == null)
                continue;

            if (builder.Length > 0)
                builder.Append(';');

            RectTransform contentRoot = region.ContentRoot;
            builder.Append(region.RegionId);
            builder.Append(":active=");
            builder.Append(region.gameObject.activeInHierarchy ? "1" : "0");
            builder.Append(",alpha=");
            builder.Append(region.CanvasGroup != null ? region.CanvasGroup.alpha.ToString("0.00") : "null");
            builder.Append(",scale=");
            builder.Append(region.RegionRoot != null ? region.RegionRoot.localScale.ToString("F2") : "null");
            builder.Append(",children=");
            builder.Append(contentRoot != null ? contentRoot.childCount : -1);
            if (contentRoot != null && contentRoot.childCount > 0)
            {
                builder.Append(",first=");
                Transform firstChild = contentRoot.GetChild(0);
                builder.Append(firstChild != null ? firstChild.name : "null");
                builder.Append(",firstActive=");
                builder.Append(firstChild != null && firstChild.gameObject.activeInHierarchy ? "1" : "0");
            }
        }

        return builder.ToString();
    }

    private static float EstimateAverageLuma(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
            return 0f;

        int step = Mathf.Max(1, pixels.Length / 4096);
        double total = 0d;
        int count = 0;
        for (int i = 0; i < pixels.Length; i += step)
        {
            Color32 pixel = pixels[i];
            total += (0.2126d * pixel.r + 0.7152d * pixel.g + 0.0722d * pixel.b) / 255d;
            count++;
        }

        return count > 0 ? (float)(total / count) : 0f;
    }

    private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
    {
        SerializedObject serializedObject = new(runtimeConfig);
        SerializedProperty modeProperty = serializedObject.FindProperty("mode");
        if (modeProperty == null)
            throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");

        modeProperty.enumValueIndex = (int)mode;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
