using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CanvasMenuFallbackValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
    private const string ScreenshotPath = "/private/tmp/warline-canvas-menu-fallback.png";
    private const int DefaultScreenshotWidth = 1280;
    private const int DefaultScreenshotHeight = 720;

    private static int frameCount;
    private static int screenshotRequestedFrame;
    private static double startedAt;
    private static bool completed;
    private static bool screenshotRequested;
    private static string screenshotPath;
    private static int screenshotWidth;
    private static int screenshotHeight;
    private static int deployValidationFrameCount;
    private static int deployValidationSubmitFrame;
    private static double deployValidationStartedAt;
    private static bool deployValidationCompleted;
    private static bool deployValidationSubmitted;
    private static int routeCaptureFrameCount;
    private static int routeCaptureConfiguredFrame;
    private static double routeCaptureStartedAt;
    private static bool routeCaptureCompleted;
    private static bool routeCaptureConfigured;
    private static UIRoute routeCaptureRoute;
    private static UiShellPopupKind routeCapturePopup;
    private static bool routeCaptureShouldShowPopup;
    private static string routeCaptureOverlay;
    private static string routeCaptureModal;
    private static int performanceFrameCount;
    private static int performanceConfiguredFrame;
    private static int performanceWarmupFrames;
    private static int performanceSampleFrames;
    private static double performanceStartedAt;
    private static bool performanceCompleted;
    private static bool performanceConfigured;
    private static bool performanceCanvasActive;
    private static UIRoute performanceRoute;
    private static readonly List<float> performanceFrameTimes = new();
    private static int performanceCanvasRenderEvents;
    private static ProfilerRecorder performanceDrawCalls;
    private static ProfilerRecorder performanceBatches;
    private static ProfilerRecorder performanceSetPassCalls;
    private static ProfilerRecorder performanceTriangles;
    private static ProfilerRecorder performanceVertices;
    private static long performanceDrawCallsTotal;
    private static long performanceBatchesTotal;
    private static long performanceSetPassCallsTotal;
    private static long performanceTrianglesTotal;
    private static long performanceVerticesTotal;
    private static int performanceProfilerSamples;

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

            screenshotPath = ResolveScreenshotPath();
            screenshotWidth = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_WIDTH", DefaultScreenshotWidth);
            screenshotHeight = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_HEIGHT", DefaultScreenshotHeight);
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);

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

    public static void RunRouteCapture()
    {
        try
        {
            RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

            SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            screenshotPath = ResolveScreenshotPath();
            screenshotWidth = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_WIDTH", DefaultScreenshotWidth);
            screenshotHeight = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_HEIGHT", DefaultScreenshotHeight);
            routeCaptureRoute = ResolveRouteCaptureRoute();
            routeCaptureShouldShowPopup = ResolveRouteCapturePopup(out routeCapturePopup);
            routeCaptureOverlay = ResolveRouteCaptureOverlay();
            routeCaptureModal = ResolveRouteCaptureModal();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);

            routeCaptureFrameCount = 0;
            routeCaptureConfiguredFrame = 0;
            routeCaptureStartedAt = EditorApplication.timeSinceStartup;
            routeCaptureCompleted = false;
            routeCaptureConfigured = false;
            EditorApplication.update -= ContinueRouteCapture;
            EditorApplication.update += ContinueRouteCapture;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasRouteCaptureValidation] result=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    public static void RunCanvasPerformanceBaseline()
    {
        try
        {
            RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

            SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            performanceRoute = ResolvePerformanceRoute();
            performanceCanvasActive = ResolvePerformanceCanvasActive();
            performanceWarmupFrames = ResolvePositiveIntEnvironment("WARLINE_CANVAS_PERF_WARMUP_FRAMES", 90);
            performanceSampleFrames = ResolvePositiveIntEnvironment("WARLINE_CANVAS_PERF_SAMPLE_FRAMES", 240);
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            performanceFrameCount = 0;
            performanceConfiguredFrame = 0;
            performanceStartedAt = EditorApplication.timeSinceStartup;
            performanceCompleted = false;
            performanceConfigured = false;
            ResetPerformanceSamples();
            EditorApplication.update -= ContinueCanvasPerformanceBaseline;
            EditorApplication.update += ContinueCanvasPerformanceBaseline;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasPerformanceBaseline] result=Failed\n{exception}");
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

            if (!TryRenderCameraLuma(bootstrap.UiCamera, screenshotPath, screenshotWidth, screenshotHeight, out float luma, out string renderError))
            {
                Complete(false, renderError);
                return;
            }

            if (luma < 0.05f)
            {
                Complete(false, $"Captured Canvas menu screenshot is still black or near-black. luma={luma:0.000} path={screenshotPath}");
                return;
            }

            Complete(true, $"Canvas menu deploy UI is visible. luma={luma:0.000} size={screenshotWidth}x{screenshotHeight} path={screenshotPath}");
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

    private static void ContinueRouteCapture()
    {
        if (routeCaptureCompleted)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
                return;

            routeCaptureFrameCount++;
            if (routeCaptureFrameCount == 1)
                routeCaptureStartedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - routeCaptureStartedAt > 60d)
            {
                CompleteRouteCapture(false, $"Timed out waiting for Canvas route capture. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} {DescribeRuntimeState()}");
                return;
            }

            if (routeCaptureFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                CompleteRouteCapture(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            if (bootstrap.IsUiToolkitMode)
            {
                CompleteRouteCapture(false, "RuntimeUiConfig is not in Canvas mode.");
                return;
            }

            Canvas canvas = bootstrap.UiCanvas;
            if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
            {
                CompleteRouteCapture(false, DescribeCanvasState(canvas, "Canvas is not active and enabled."));
                return;
            }

            if (!routeCaptureConfigured)
            {
                if (!TryConfigureRouteCapture(bootstrap, out string configurationError))
                {
                    CompleteRouteCapture(false, configurationError);
                    return;
                }

                routeCaptureConfigured = true;
                routeCaptureConfiguredFrame = routeCaptureFrameCount;
                return;
            }

            if (routeCaptureFrameCount - routeCaptureConfiguredFrame < 12)
                return;

            if (!TryRenderCameraLuma(bootstrap.UiCamera, screenshotPath, screenshotWidth, screenshotHeight, out float luma, out string renderError))
            {
                CompleteRouteCapture(false, renderError);
                return;
            }

            float minimumLuma = ResolveRouteCaptureMinimumLuma();
            if (luma < minimumLuma)
            {
                CompleteRouteCapture(false, $"Captured Canvas route screenshot is still black or near-black. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} luma={luma:0.000} minimum={minimumLuma:0.000} path={screenshotPath}");
                return;
            }

            CompleteRouteCapture(true, $"Canvas route is visible. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} luma={luma:0.000} size={screenshotWidth}x{screenshotHeight} path={screenshotPath}");
        }
        catch (Exception exception)
        {
            CompleteRouteCapture(false, exception.ToString());
        }
    }

    private static void ContinueCanvasPerformanceBaseline()
    {
        if (performanceCompleted)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
                return;

            performanceFrameCount++;
            if (performanceFrameCount == 1)
                performanceStartedAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - performanceStartedAt > 120d)
            {
                CompleteCanvasPerformanceBaseline(false, $"Timed out waiting for Canvas performance baseline. route={performanceRoute} canvas={(performanceCanvasActive ? "Active" : "Disabled")} {DescribeRuntimeState()}");
                return;
            }

            if (performanceFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                CompleteCanvasPerformanceBaseline(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            if (bootstrap.IsUiToolkitMode)
            {
                CompleteCanvasPerformanceBaseline(false, "RuntimeUiConfig is not in Canvas mode.");
                return;
            }

            Canvas canvas = bootstrap.UiCanvas;
            if (canvas == null)
            {
                CompleteCanvasPerformanceBaseline(false, "Menu scene is missing the runtime Canvas reference.");
                return;
            }

            if (!performanceConfigured)
            {
                routeCaptureRoute = performanceRoute;
                routeCaptureShouldShowPopup = false;
                routeCaptureOverlay = string.Empty;
                routeCaptureModal = string.Empty;
                if (!TryConfigureRouteCapture(bootstrap, out string configurationError))
                {
                    CompleteCanvasPerformanceBaseline(false, configurationError);
                    return;
                }

                canvas.gameObject.SetActive(performanceCanvasActive);
                Canvas.willRenderCanvases -= CountPerformanceCanvasRender;
                Canvas.willRenderCanvases += CountPerformanceCanvasRender;
                StartPerformanceRecorders();
                performanceConfigured = true;
                performanceConfiguredFrame = performanceFrameCount;
                return;
            }

            int sampledFrame = performanceFrameCount - performanceConfiguredFrame - performanceWarmupFrames;
            if (sampledFrame < 0)
                return;

            float deltaSeconds = Time.unscaledDeltaTime;
            if (deltaSeconds > 0f)
                performanceFrameTimes.Add(deltaSeconds);

            SamplePerformanceRecorders();
            if (performanceFrameTimes.Count >= performanceSampleFrames)
                CompleteCanvasPerformanceBaseline(true, BuildPerformanceResultMessage());
        }
        catch (Exception exception)
        {
            CompleteCanvasPerformanceBaseline(false, exception.ToString());
        }
    }

    private static bool TryConfigureRouteCapture(MenuBootstrapView bootstrap, out string error)
    {
        error = null;
        UIShellContentView content = bootstrap != null ? bootstrap.ContentSystem : null;
        if (content == null)
        {
            error = "Menu scene is missing UIShellContentView for Canvas route capture.";
            return false;
        }

        switch (routeCaptureRoute)
        {
            case UIRoute.Splash:
                content.PrepareForCommandSequence(new[]
                {
                    new UiShellPresentationCommandModel(
                        UiShellCommandKind.ShowLoading,
                        UiShellRegionId.LoadingLayer,
                        UIRoute.Splash,
                        UiShellMode.Loading,
                        1)
                });
                break;
            case UIRoute.MainMenu:
            case UIRoute.Armory:
                content.InstallMenuRouteBody(routeCaptureRoute);
                break;
            case UIRoute.Match:
                content.PrepareForCommandSequence(new[]
                {
                    new UiShellPresentationCommandModel(
                        UiShellCommandKind.EnterMatchHud,
                        UiShellRegionId.None,
                        UIRoute.Match,
                        UiShellMode.MatchHud,
                        1)
                });
                break;
            default:
                error = $"Canvas route capture does not support route={routeCaptureRoute}. Supported routes: Splash, MainMenu, Armory, Match.";
                return false;
        }

        if (routeCaptureShouldShowPopup)
        {
            if (routeCapturePopup != UiShellPopupKind.BuildDrawer)
            {
                error = $"Canvas route capture does not support popup={routeCapturePopup}. Supported popup: BuildDrawer.";
                return false;
            }

            GameObject popup = content.InstallBuildDrawerPopup();
            if (popup == null)
            {
                error = "Canvas route capture could not install BuildDrawer popup.";
                return false;
            }
        }

        if (!TryConfigureRouteCaptureOverlay(out error))
            return false;

        if (!TryConfigureRouteCaptureModal(bootstrap, out error))
            return false;

        return true;
    }

    private static bool TryConfigureRouteCaptureOverlay(out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(routeCaptureOverlay))
            return true;

        if (!string.Equals(routeCaptureOverlay, "BuildPlacementBar", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Canvas route capture does not support overlay={routeCaptureOverlay}. Supported overlay: BuildPlacementBar.";
            return false;
        }

        if (routeCaptureRoute != UIRoute.Match)
        {
            error = "BuildPlacementBar overlay capture requires WARLINE_CANVAS_ROUTE=Match.";
            return false;
        }

        BuildPlacementConfirmationBarView placementBar =
            UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
        if (placementBar == null)
        {
            error = "Canvas route capture could not find BuildPlacementConfirmationBarView after Match HUD install.";
            return false;
        }

        placementBar.BindRuntimeCommands(new RouteCaptureBuildingUiCommand(), null);
        return true;
    }

    private static bool TryConfigureRouteCaptureModal(MenuBootstrapView bootstrap, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(routeCaptureModal))
            return true;

        if (!TryResolveRouteCaptureModalPath(routeCaptureModal, out string prefabPath))
        {
            error = $"Canvas route capture does not support modal={routeCaptureModal}. Supported modals: MissionResult, ConfirmRaid, EndOfDayReport, IntelReveal, AbilityUpgradeDetail, BuildPlacementPanel, PauseMenu, PopupFrame, RewardUnlock, ThreatAlert.";
            return false;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            error = $"Canvas route capture could not load modal prefab at {prefabPath}.";
            return false;
        }

        RectTransform parent = bootstrap != null && bootstrap.UiCanvas != null
            ? bootstrap.UiCanvas.transform as RectTransform
            : null;
        if (parent == null)
        {
            error = "Canvas route capture could not find a Canvas RectTransform for modal capture.";
            return false;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
        instance.name = prefab.name;
        instance.SetActive(true);

        RectTransform rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
            rect.SetAsLastSibling();
        }
        else
        {
            instance.transform.SetAsLastSibling();
        }

        return true;
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

    private static void CompleteRouteCapture(bool success, string message)
    {
        if (routeCaptureCompleted)
            return;

        routeCaptureCompleted = true;
        EditorApplication.update -= ContinueRouteCapture;
        if (success)
            Debug.Log($"[CanvasRouteCaptureValidation] result=Passed {message}");
        else
            Debug.LogError($"[CanvasRouteCaptureValidation] result=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static void CompleteCanvasPerformanceBaseline(bool success, string message)
    {
        if (performanceCompleted)
            return;

        performanceCompleted = true;
        EditorApplication.update -= ContinueCanvasPerformanceBaseline;
        Canvas.willRenderCanvases -= CountPerformanceCanvasRender;
        DisposePerformanceRecorders();
        if (success)
            Debug.Log($"[CanvasPerformanceBaseline] result=Passed {message}");
        else
            Debug.LogError($"[CanvasPerformanceBaseline] result=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static void CountPerformanceCanvasRender()
    {
        performanceCanvasRenderEvents++;
    }

    private static void ResetPerformanceSamples()
    {
        performanceFrameTimes.Clear();
        performanceCanvasRenderEvents = 0;
        performanceDrawCallsTotal = 0;
        performanceBatchesTotal = 0;
        performanceSetPassCallsTotal = 0;
        performanceTrianglesTotal = 0;
        performanceVerticesTotal = 0;
        performanceProfilerSamples = 0;
        DisposePerformanceRecorders();
    }

    private static void StartPerformanceRecorders()
    {
        DisposePerformanceRecorders();
        performanceDrawCalls = TryStartPerformanceRecorder("Draw Calls Count");
        performanceBatches = TryStartPerformanceRecorder("Batches Count");
        performanceSetPassCalls = TryStartPerformanceRecorder("SetPass Calls Count");
        performanceTriangles = TryStartPerformanceRecorder("Triangles Count");
        performanceVertices = TryStartPerformanceRecorder("Vertices Count");
    }

    private static ProfilerRecorder TryStartPerformanceRecorder(string statName)
    {
        try
        {
            return ProfilerRecorder.StartNew(ProfilerCategory.Render, statName);
        }
        catch
        {
            return default;
        }
    }

    private static void SamplePerformanceRecorders()
    {
        bool hasAnyRecorder = false;
        if (performanceDrawCalls.Valid)
        {
            performanceDrawCallsTotal += performanceDrawCalls.LastValue;
            hasAnyRecorder = true;
        }

        if (performanceBatches.Valid)
        {
            performanceBatchesTotal += performanceBatches.LastValue;
            hasAnyRecorder = true;
        }

        if (performanceSetPassCalls.Valid)
        {
            performanceSetPassCallsTotal += performanceSetPassCalls.LastValue;
            hasAnyRecorder = true;
        }

        if (performanceTriangles.Valid)
        {
            performanceTrianglesTotal += performanceTriangles.LastValue;
            hasAnyRecorder = true;
        }

        if (performanceVertices.Valid)
        {
            performanceVerticesTotal += performanceVertices.LastValue;
            hasAnyRecorder = true;
        }

        if (hasAnyRecorder)
            performanceProfilerSamples++;
    }

    private static void DisposePerformanceRecorders()
    {
        if (performanceDrawCalls.Valid)
            performanceDrawCalls.Dispose();
        if (performanceBatches.Valid)
            performanceBatches.Dispose();
        if (performanceSetPassCalls.Valid)
            performanceSetPassCalls.Dispose();
        if (performanceTriangles.Valid)
            performanceTriangles.Dispose();
        if (performanceVertices.Valid)
            performanceVertices.Dispose();

        performanceDrawCalls = default;
        performanceBatches = default;
        performanceSetPassCalls = default;
        performanceTriangles = default;
        performanceVertices = default;
    }

    private static string BuildPerformanceResultMessage()
    {
        float averageDelta = 0f;
        float minDelta = float.MaxValue;
        float maxDelta = 0f;
        for (int i = 0; i < performanceFrameTimes.Count; i++)
        {
            float delta = performanceFrameTimes[i];
            averageDelta += delta;
            if (delta < minDelta)
                minDelta = delta;
            if (delta > maxDelta)
                maxDelta = delta;
        }

        averageDelta /= performanceFrameTimes.Count;
        List<float> sortedFrameTimes = new(performanceFrameTimes);
        sortedFrameTimes.Sort();
        int p95Index = Mathf.Clamp(Mathf.CeilToInt(sortedFrameTimes.Count * 0.95f) - 1, 0, sortedFrameTimes.Count - 1);
        float p95Delta = sortedFrameTimes[p95Index];
        float fps = averageDelta > 0f ? 1f / averageDelta : 0f;
        return $"route={performanceRoute} canvas={(performanceCanvasActive ? "Active" : "Disabled")} samples={performanceFrameTimes.Count} warmupFrames={performanceWarmupFrames} avgMs={averageDelta * 1000f:0.000} fps={fps:0.0} minMs={minDelta * 1000f:0.000} p95Ms={p95Delta * 1000f:0.000} maxMs={maxDelta * 1000f:0.000} canvasRenderEvents={performanceCanvasRenderEvents} profilerSamples={performanceProfilerSamples} drawCallsAvg={FormatPerformanceAverage(performanceDrawCallsTotal)} batchesAvg={FormatPerformanceAverage(performanceBatchesTotal)} setPassAvg={FormatPerformanceAverage(performanceSetPassCallsTotal)} trianglesAvg={FormatPerformanceAverage(performanceTrianglesTotal)} verticesAvg={FormatPerformanceAverage(performanceVerticesTotal)}";
    }

    private static string FormatPerformanceAverage(long total)
    {
        if (performanceProfilerSamples <= 0)
            return "unavailable";

        return (total / (double)performanceProfilerSamples).ToString("0.0");
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

    private static bool TryRenderCameraLuma(Camera camera, string screenshotPath, int width, int height, out float luma, out string error)
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
            string directory = Path.GetDirectoryName(screenshotPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
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

    private static string ResolveScreenshotPath()
    {
        string configuredPath = Environment.GetEnvironmentVariable("WARLINE_CANVAS_SCREENSHOT_PATH");
        return string.IsNullOrWhiteSpace(configuredPath) ? ScreenshotPath : configuredPath;
    }

    private static int ResolveScreenshotDimension(string environmentVariableName, int fallback)
    {
        string configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return int.TryParse(configuredValue, out int value) && value > 0 ? value : fallback;
    }

    private static UIRoute ResolveRouteCaptureRoute()
    {
        string configuredRoute = Environment.GetEnvironmentVariable("WARLINE_CANVAS_ROUTE");
        return Enum.TryParse(configuredRoute, true, out UIRoute route) ? route : UIRoute.MainMenu;
    }

    private static bool ResolveRouteCapturePopup(out UiShellPopupKind popup)
    {
        string configuredPopup = Environment.GetEnvironmentVariable("WARLINE_CANVAS_POPUP");
        if (string.IsNullOrWhiteSpace(configuredPopup))
        {
            popup = default;
            return false;
        }

        if (Enum.TryParse(configuredPopup, true, out popup))
            return true;

        throw new InvalidOperationException($"Unsupported WARLINE_CANVAS_POPUP value: {configuredPopup}");
    }

    private static UIRoute ResolvePerformanceRoute()
    {
        string configuredRoute = Environment.GetEnvironmentVariable("WARLINE_CANVAS_PERF_ROUTE");
        if (string.IsNullOrWhiteSpace(configuredRoute))
            return UIRoute.MainMenu;

        if (Enum.TryParse(configuredRoute, true, out UIRoute route) &&
            (route == UIRoute.MainMenu || route == UIRoute.Match))
        {
            return route;
        }

        throw new InvalidOperationException($"Unsupported WARLINE_CANVAS_PERF_ROUTE value: {configuredRoute}. Supported routes: MainMenu, Match.");
    }

    private static bool ResolvePerformanceCanvasActive()
    {
        string configuredMode = Environment.GetEnvironmentVariable("WARLINE_CANVAS_PERF_CANVAS");
        return !string.Equals(configuredMode, "Disabled", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolvePositiveIntEnvironment(string environmentVariableName, int fallback)
    {
        string configuredValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return int.TryParse(configuredValue, out int value) && value > 0 ? value : fallback;
    }

    private static string DescribeRouteCapturePopup()
    {
        return routeCaptureShouldShowPopup ? routeCapturePopup.ToString() : "None";
    }

    private static string ResolveRouteCaptureOverlay()
    {
        string configuredOverlay = Environment.GetEnvironmentVariable("WARLINE_CANVAS_OVERLAY");
        return string.IsNullOrWhiteSpace(configuredOverlay) ? string.Empty : configuredOverlay.Trim();
    }

    private static string DescribeRouteCaptureOverlay()
    {
        return string.IsNullOrWhiteSpace(routeCaptureOverlay) ? "None" : routeCaptureOverlay;
    }

    private static string ResolveRouteCaptureModal()
    {
        string configuredModal = Environment.GetEnvironmentVariable("WARLINE_CANVAS_MODAL");
        return string.IsNullOrWhiteSpace(configuredModal) ? string.Empty : configuredModal.Trim();
    }

    private static string DescribeRouteCaptureModal()
    {
        return string.IsNullOrWhiteSpace(routeCaptureModal) ? "None" : routeCaptureModal;
    }

    private static bool TryResolveRouteCaptureModalPath(string modalName, out string prefabPath)
    {
        switch (modalName.Trim().ToLowerInvariant())
        {
            case "missionresult":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";
                return true;
            case "confirmraid":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/ConfirmRaidPopup.prefab";
                return true;
            case "endofdayreport":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab";
                return true;
            case "intelreveal":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/IntelRevealPopup.prefab";
                return true;
            case "abilityupgradedetail":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/AbilityUpgradeDetailPopup.prefab";
                return true;
            case "buildplacementpanel":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab";
                return true;
            case "pausemenu":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";
                return true;
            case "popupframe":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/PopupFrameView.prefab";
                return true;
            case "rewardunlock":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab";
                return true;
            case "threatalert":
                prefabPath = "Assets/Game/Prefabs/UI/Popups/ThreatAlertPopup.prefab";
                return true;
            default:
                prefabPath = null;
                return false;
        }
    }

    private static float ResolveRouteCaptureMinimumLuma()
    {
        if (routeCaptureRoute == UIRoute.Match &&
            !routeCaptureShouldShowPopup &&
            string.IsNullOrWhiteSpace(routeCaptureOverlay))
        {
            return 0.035f;
        }

        if (string.Equals(routeCaptureModal, "PopupFrame", StringComparison.OrdinalIgnoreCase))
            return 0.035f;

        return 0.05f;
    }

    private sealed class RouteCaptureBuildingUiCommand : IBuildingUiCommand
    {
        public int CurrentDollars => 12500;
        public bool HasPendingBuildingPlacement => true;
        public bool CanConfirmBuildingPlacement => true;
        public string PlacementStatusText => "Barracks: Valid placement";
        public int ActivePlacementCost => 650;
        public float ActivePlacementDurationSeconds => 45f;

        public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return default;
        }

        public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            return default;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex)
        {
            return false;
        }

        public bool ConfirmBuildingPlacement()
        {
            return true;
        }

        public void CancelBuildingPlacement()
        {
        }

        public bool RotateBuildingPlacement()
        {
            return true;
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
