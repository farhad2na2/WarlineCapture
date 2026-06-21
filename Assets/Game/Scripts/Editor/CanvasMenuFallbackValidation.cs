using System;
using System.IO;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private static int deployValidationFrameCount;
    private static int deployValidationSubmitFrame;
    private static double deployValidationStartedAt;
    private static bool deployValidationCompleted;
    private static bool deployValidationSubmitted;

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

    public static void RunDeployClickValidation()
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

            deployValidationFrameCount = 0;
            deployValidationSubmitFrame = 0;
            deployValidationStartedAt = EditorApplication.timeSinceStartup;
            deployValidationCompleted = false;
            deployValidationSubmitted = false;
            EditorApplication.update -= ContinueDeployClickValidation;
            EditorApplication.update += ContinueDeployClickValidation;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasMenuDeployClickValidation] result=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void Continue()
    {
        if (completed)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
                return;

            frameCount++;
            if (frameCount == 1)
                startedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - startedAt > 60d)
            {
                string timeoutPrefix = screenshotRequested
                    ? "Timed out while waiting for Canvas menu render validation."
                    : "Timed out before Canvas menu deploy UI became visible.";
                Complete(false, $"{timeoutPrefix} {DescribeRuntimeState()}");
                return;
            }

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
                screenshotRequested = true;
                screenshotRequestedFrame = frameCount;
                return;
            }

            if (frameCount - screenshotRequestedFrame < 12)
                return;

            if (!TryRenderCameraLuma(bootstrap.UiCamera, ScreenshotPath, out float luma, out string renderError))
            {
                Complete(false, renderError);
                return;
            }

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

    private static void ContinueDeployClickValidation()
    {
        if (deployValidationCompleted)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
                return;

            deployValidationFrameCount++;
            if (deployValidationFrameCount == 1)
                deployValidationStartedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - deployValidationStartedAt > 120d)
            {
                CompleteDeployClickValidation(false, $"Timed out waiting for Canvas Deploy click to route to Match. {DescribeDeployRuntimeState()} {DescribeRuntimeState()}");
                return;
            }

            if (deployValidationFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                CompleteDeployClickValidation(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            if (bootstrap.IsUiToolkitMode)
            {
                CompleteDeployClickValidation(false, "RuntimeUiConfig is not in Canvas mode.");
                return;
            }

            if (!deployValidationSubmitted)
            {
                Button deployButton = FindVisibleDeployButton();
                if (deployButton == null)
                    return;

                deployButton.onClick.Invoke();
                deployValidationSubmitted = true;
                deployValidationSubmitFrame = deployValidationFrameCount;
                Debug.Log("[CanvasMenuDeployClickValidation] deployActionSubmitted=UnityUIButton target=DeployCommandButton");
                return;
            }

            if (UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState) &&
                shellState.ActiveRoute == UIRoute.Match &&
                (shellState.CurrentMode == UiShellMode.Loading || shellState.CurrentMode == UiShellMode.MatchHud))
            {
                CompleteDeployClickValidation(true, $"Canvas Deploy routed to Match. {DescribeDeployRuntimeState()}");
                return;
            }

            if (deployValidationFrameCount - deployValidationSubmitFrame < 12)
                return;

            if (TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState) &&
                lifecycleState.Status == SceneLifecycleStatusKind.Failed)
            {
                CompleteDeployClickValidation(false, $"Deploy Match scene load failed. {DescribeDeployRuntimeState()}");
            }
        }
        catch (Exception exception)
        {
            CompleteDeployClickValidation(false, exception.ToString());
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

            Rect localRect = rect.rect;
            if (localRect.width <= 1f || localRect.height <= 1f)
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

    private static void CompleteDeployClickValidation(bool success, string message)
    {
        if (deployValidationCompleted)
            return;

        deployValidationCompleted = true;
        EditorApplication.update -= ContinueDeployClickValidation;
        if (success)
            Debug.Log($"[CanvasMenuDeployClickValidation] result=Passed {message}");
        else
            Debug.LogError($"[CanvasMenuDeployClickValidation] result=Failed {message}");

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

        return $"{DescribeCanvasState(canvas, "canvas")} shellChildren={shellChildren} contentVersion={contentVersion} activeButtons={activeButtons} buttonNames=[{DescribeButtonNames(activeButtonObjects)}] deployCandidates=[{DescribeDeployCandidates(activeButtonObjects)}] regions=[{DescribeRegions(bootstrap)}] {shellState}";
    }

    private static string DescribeDeployRuntimeState()
    {
        string shell = UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState)
            ? $"shell(mode={shellState.CurrentMode},route={shellState.ActiveRoute},phase={shellState.Phase},transition={shellState.IsTransitionRunning},seq={shellState.TransitionSequenceId})"
            : "shell=unavailable";
        string loading = UiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loadingState)
            ? $"loading(progress={loadingState.Progress01:0.00},complete={loadingState.IsComplete},status={loadingState.Status})"
            : "loading=unavailable";
        string lifecycle = TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState)
            ? $"scene(status={lifecycleState.Status},busy={lifecycleState.IsBusy},loaded={lifecycleState.IsMatchLoaded},progress={lifecycleState.Progress01:0.00})"
            : "scene=unavailable";
        Scene matchScene = SceneManager.GetSceneByName(SceneLifecycleSystem.MatchSceneName);
        string unityScene = $"unityScene(valid={matchScene.IsValid()},loaded={matchScene.isLoaded})";
        return $"{shell}; {loading}; {lifecycle}; {unityScene}";
    }

    private static bool TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState)
    {
        lifecycleState = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SceneLifecycleBoundaryComponent>(),
            ComponentType.ReadOnly<SceneLifecycleStateComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        lifecycleState = entityManager.GetComponentData<SceneLifecycleStateComponent>(entity);
        return true;
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

    private static string DescribeDeployCandidates(Button[] buttons)
    {
        if (buttons == null || buttons.Length == 0)
            return "";

        StringBuilder builder = new();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            string objectName = button.gameObject.name;
            if (!string.Equals(objectName, "DeployCommandButton", StringComparison.Ordinal) &&
                !string.Equals(objectName, "DeployOperationButton", StringComparison.Ordinal))
            {
                continue;
            }

            if (builder.Length > 0)
                builder.Append(';');

            RectTransform rect = button.transform as RectTransform;
            Rect localRect = rect != null ? rect.rect : default;
            builder.Append(objectName);
            builder.Append(":enabled=");
            builder.Append(button.isActiveAndEnabled ? "1" : "0");
            builder.Append(",interactable=");
            builder.Append(button.IsInteractable() ? "1" : "0");
            builder.Append(",rect=");
            builder.Append(localRect.width.ToString("0.0"));
            builder.Append('x');
            builder.Append(localRect.height.ToString("0.0"));
            builder.Append(",sizeDelta=");
            builder.Append(rect != null ? rect.sizeDelta.ToString("F1") : "null");
            builder.Append(",lossyScale=");
            builder.Append(rect != null ? rect.lossyScale.ToString("F2") : "null");
        }

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

    private static bool TryRenderCameraLuma(Camera camera, string screenshotPath, out float luma, out string error)
    {
        luma = 0f;
        if (camera == null)
        {
            error = "Canvas menu validation could not render because the UI camera reference is missing.";
            return false;
        }

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply(false, false);
            File.WriteAllBytes(screenshotPath, texture.EncodeToPNG());
            luma = EstimateAverageLuma(texture);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = $"Canvas menu validation render failed. {exception}";
            return false;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
        }
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
