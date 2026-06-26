using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private static int routeCaptureSettleFrames;
    private static double routeCaptureStartedAt;
    private static bool routeCaptureCompleted;
    private static bool routeCaptureConfigured;
    private static UIRoute routeCaptureRoute;
    private static UiShellPopupKind routeCapturePopup;
    private static bool routeCaptureShouldShowPopup;
    private static string routeCaptureOverlay;
    private static string routeCaptureModal;
    private static string routeCaptureStaticContentPrefabPath;
    private static bool routeCaptureStaticContentFullRoot;
    private static bool routeCaptureShouldSetArmoryCategory;
    private static ArmoryCatalogCategory routeCaptureArmoryCategory;
    private static string routeCaptureSelectButtonName;
    private static bool routeCaptureButtonSelectionApplied;
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
            routeCaptureStaticContentPrefabPath = ResolveRouteCaptureStaticContentPrefabPath();
            routeCaptureStaticContentFullRoot = ResolveRouteCaptureStaticContentFullRoot();
            routeCaptureShouldSetArmoryCategory = ResolveRouteCaptureArmoryCategory(out routeCaptureArmoryCategory);
            routeCaptureSelectButtonName = ResolveRouteCaptureSelectButtonName();
            routeCaptureSettleFrames = ResolvePositiveIntEnvironment("WARLINE_CANVAS_ROUTE_CAPTURE_SETTLE_FRAMES", 12);
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);

            routeCaptureFrameCount = 0;
            routeCaptureConfiguredFrame = 0;
            routeCaptureStartedAt = EditorApplication.timeSinceStartup;
            routeCaptureCompleted = false;
            routeCaptureConfigured = false;
            routeCaptureButtonSelectionApplied = false;
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

    public static void ApplyCommanderProfileTargetLockLayout()
    {
        const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab";

        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(PrefabPath);
            Transform left = FindChild(root.transform, "LeftContent");
            Transform middle = FindChild(root.transform, "MiddleContent");
            Transform right = FindChild(root.transform, "RightContent");
            Transform footer = FindChild(root.transform, "FooterContent");
            if (left == null || middle == null || right == null || footer == null)
                throw new InvalidOperationException("SCN-03 prefab is missing one or more shell content roots.");

            SetActive(FindChild(root.transform, "HeaderContent")?.gameObject, false);
            HideAllChildren(FindChild(root.transform, "MenuBackgroundContent"));
            SetRectFromTopLeft(left.gameObject, 52f, 310f, 800f, 1500f);
            SetRectFromTopLeft(middle.gameObject, 980f, 300f, 1880f, 1700f);
            SetRectFromTopLeft(right.gameObject, 2980f, 300f, 1740f, 1700f);
            SetRectFromTopLeft(footer.gameObject, 980f, 1905f, 2860f, 210f);

            GameObject backButton = FindChild(root.transform, "BackButton")?.gameObject;
            GameObject identityPanel = FindChild(root.transform, "CommanderIdentityPanel")?.gameObject;
            GameObject overviewPanel = FindChild(root.transform, "OverviewPanel")?.gameObject;
            GameObject accountPanel = FindChild(root.transform, "AccountSnapshotPanel")?.gameObject;
            GameObject rewardPanel = FindChild(root.transform, "RewardTrackPanel")?.gameObject;
            GameObject historyPanel = FindChild(root.transform, "RecentHistoryPanel")?.gameObject;
            GameObject armoryPanel = FindChild(root.transform, "ArmorySquadsPanel")?.gameObject;
            GameObject profileRewardsPanel = FindChild(root.transform, "ProfileRewardsPanel")?.gameObject;
            GameObject openArmoryButton = FindChild(root.transform, "OpenArmoryButton")?.gameObject;
            GameObject replayButton = FindChild(root.transform, "ReplayButton")?.gameObject;
            GameObject detailButton = FindChild(root.transform, "DetailButton")?.gameObject;

            MoveTo(backButton, left);
            SetRectFromTopLeft(backButton, 0f, 0f, 500f, 135f);
            ApplyButtonFrame(backButton, FrameKind.Secondary);
            LayoutIconLabelButton(backButton, 64f, 42f, 68f, 52f, 156f, 0f, 310f, 135f, 46f);

            string[] tabNames = { "OverviewTab", "StatsTab", "BadgesTab", "HistoryTab", "UpgradesTab" };
            string[] tabIconPaths =
            {
                "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_hold_shield.png",
                "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_units_group.png",
                "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_badge_owned_checkmark.png",
                "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_attack_reticle.png",
                "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_upgrades_chevrons.png"
            };
            for (int i = 0; i < tabNames.Length; i++)
            {
                GameObject tab = FindChild(root.transform, tabNames[i])?.gameObject;
                MoveTo(tab, left);
                SetRectFromTopLeft(tab, 0f, 215f + (i * 235f), 800f, 205f);
                ApplyNavButtonState(tab, i == 0);
                SetChildSprite(tab, "Icon", tabIconPaths[i], true);
                LayoutIconLabelButton(tab, 54f, 52f, 124f, 98f, 220f, 0f, 520f, 205f, 56f);
            }
            HideDirectChildrenExcept(left, "BackButton", "OverviewTab", "StatsTab", "BadgesTab", "HistoryTab", "UpgradesTab");

            MoveTo(identityPanel, middle);
            SetRectFromTopLeft(identityPanel, 0f, 0f, 1880f, 760f);
            ApplyPanelFrame(identityPanel, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_02_commander_identity_panel_frame.png", 4.2f);
            LayoutCommanderIdentity(identityPanel);

            MoveTo(overviewPanel, middle);
            SetRectFromTopLeft(overviewPanel, 0f, 800f, 1880f, 350f);
            ApplyPanelFrame(overviewPanel, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_03_overview_panel_frame.png", 4.4f);
            LayoutOverviewStats(overviewPanel);

            MoveTo(accountPanel, middle);
            SetRectFromTopLeft(accountPanel, 0f, 1220f, 1880f, 430f);
            ApplyPanelFrame(accountPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 4.2f);
            LayoutAccountSnapshot(accountPanel);
            HideDirectChildrenExcept(middle, "CommanderIdentityPanel", "OverviewPanel", "AccountSnapshotPanel");

            MoveTo(rewardPanel, right);
            SetRectFromTopLeft(rewardPanel, 0f, 0f, 1740f, 760f);
            ApplyPanelFrame(rewardPanel, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_05_reward_track_panel_frame.png", 4.2f);
            LayoutRewardTrack(rewardPanel);

            MoveTo(historyPanel, right);
            SetRectFromTopLeft(historyPanel, 0f, 810f, 1740f, 890f);
            ApplyPanelFrame(historyPanel, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_06_recent_history_panel_frame.png", 4.3f);
            LayoutRecentHistory(historyPanel);
            HideDirectChildrenExcept(right, "RewardTrackPanel", "RecentHistoryPanel");

            SetActive(armoryPanel, false);
            SetActive(profileRewardsPanel, false);

            PrepareFooterButton(openArmoryButton, footer, 0f, "OPEN ARMORY", FrameKind.Primary);
            PrepareFooterButton(detailButton, footer, 960f, "DETAIL", FrameKind.Secondary);
            PrepareFooterButton(replayButton, footer, 1780f, "REPLAY", FrameKind.Primary);
            HideFooterBreadcrumbs(footer, openArmoryButton, detailButton, replayButton);
            HideDirectChildrenExcept(footer, "OpenArmoryButton", "DetailButton", "ReplayButton");

            ApplyPanelBackground(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[CanvasCommanderProfileTargetLockLayout] result=Passed prefab={PrefabPath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasCommanderProfileTargetLockLayout] result=Failed\n{exception}");
            EditorApplication.Exit(1);
            return;
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
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
            DisableMenuDiagnosticsOverlay();
            if (bootstrap.UiMode != RuntimeUiMode.Canvas)
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
            DisableMenuDiagnosticsOverlay();
            if (bootstrap.UiMode != RuntimeUiMode.Canvas)
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
                CompleteRouteCapture(false, $"Timed out waiting for Canvas route capture. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} armoryCategory={DescribeRouteCaptureArmoryCategory()} {DescribeRuntimeState()}");
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
            if (bootstrap.UiMode != RuntimeUiMode.Canvas)
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
                DisableMenuDiagnosticsOverlay();
                return;
            }

            if (routeCaptureFrameCount - routeCaptureConfiguredFrame < routeCaptureSettleFrames)
                return;

            DisableMenuDiagnosticsOverlay();
            if (!TryApplyRouteCaptureButtonSelection(out string selectionError, out bool waitForSelectionSettle))
            {
                CompleteRouteCapture(false, selectionError);
                return;
            }
            if (waitForSelectionSettle)
                return;

            if (!TryRenderCameraLuma(bootstrap.UiCamera, screenshotPath, screenshotWidth, screenshotHeight, out float luma, out string renderError))
            {
                CompleteRouteCapture(false, renderError);
                return;
            }

            float minimumLuma = ResolveRouteCaptureMinimumLuma();
            if (luma < minimumLuma)
            {
                CompleteRouteCapture(false, $"Captured Canvas route screenshot is still black or near-black. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} armoryCategory={DescribeRouteCaptureArmoryCategory()} selectedButton={DescribeRouteCaptureSelectedButton()} luma={luma:0.000} minimum={minimumLuma:0.000} path={screenshotPath}");
                return;
            }

            CompleteRouteCapture(true, $"Canvas route is visible. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} armoryCategory={DescribeRouteCaptureArmoryCategory()} selectedButton={DescribeRouteCaptureSelectedButton()} luma={luma:0.000} size={screenshotWidth}x{screenshotHeight} path={screenshotPath}");
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
            if (bootstrap.UiMode != RuntimeUiMode.Canvas)
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
                routeCaptureShouldSetArmoryCategory = false;
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

        if (!string.IsNullOrWhiteSpace(routeCaptureStaticContentPrefabPath))
        {
            content.InstallMenuRouteBody(UIRoute.MainMenu);
            if (!TryMountStaticMenuContent(bootstrap, routeCaptureStaticContentPrefabPath, out error))
                return false;

            ResetRouteCaptureRegions(
                bootstrap,
                UIShellRegionId.MenuBackgroundRegion,
                UIShellRegionId.HeaderRegion,
                UIShellRegionId.LeftRegion,
                UIShellRegionId.MiddleRegion,
                UIShellRegionId.RightRegion,
                UIShellRegionId.FooterRegion);
        }
        else
        {
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
                ResetRouteCaptureRegions(
                    bootstrap,
                    UIShellRegionId.MenuBackgroundRegion,
                    UIShellRegionId.HeaderRegion,
                    UIShellRegionId.LeftRegion,
                    UIShellRegionId.MiddleRegion,
                    UIShellRegionId.RightRegion,
                    UIShellRegionId.FooterRegion);
                if (!TryConfigureRouteCaptureArmoryCategory(out error))
                    return false;
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

    private static bool TryMountStaticMenuContent(MenuBootstrapView bootstrap, string prefabPath, out string error)
    {
        error = null;
        if (bootstrap == null || bootstrap.ShellView == null)
        {
            error = "Canvas route capture could not find UIShellView for static content mounting.";
            return false;
        }

        if (routeCaptureStaticContentFullRoot)
            return TryMountStaticMenuContentRoot(bootstrap, prefabPath, out error);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            error = $"Canvas route capture could not load static content prefab at {prefabPath}.";
            return false;
        }

        UIShellContentSectionId[] sectionIds =
        {
            UIShellContentSectionId.MenuBackground,
            UIShellContentSectionId.Left,
            UIShellContentSectionId.Middle,
            UIShellContentSectionId.Right,
            UIShellContentSectionId.Footer
        };

        bool mountedAny = false;
        for (int i = 0; i < sectionIds.Length; i++)
        {
            UIShellContentSectionId sectionId = sectionIds[i];
            if (!TryResolveStaticContentSection(prefab, sectionId, out GameObject source) || source == null)
                continue;

            if (!TryMountStaticSection(bootstrap.ShellView, source, ToRegionId(sectionId), out error))
                return false;

            mountedAny = true;
        }

        if (!mountedAny)
        {
            error = $"Static content prefab has no mountable menu sections: {prefabPath}.";
            return false;
        }

        if (bootstrap.ShellView.TryGetRegion(UIShellRegionId.PopupLayer, out UIShellRegionView popup) && popup != null)
            popup.ClearContent();

        Debug.Log($"[CanvasRouteCaptureValidation] staticContentPrefab={prefabPath}");
        return true;
    }

    private static bool TryMountStaticMenuContentRoot(MenuBootstrapView bootstrap, string prefabPath, out string error)
    {
        error = null;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            error = $"Canvas route capture could not load static content prefab at {prefabPath}.";
            return false;
        }

        if (!bootstrap.ShellView.TryGetRegion(UIShellRegionId.MenuBackgroundRegion, out UIShellRegionView background) ||
            background == null ||
            background.ContentRoot == null)
        {
            error = "Canvas route capture could not find MenuBackgroundRegion for full-root static content mounting.";
            return false;
        }

        ClearStaticRegion(bootstrap.ShellView, UIShellRegionId.LeftRegion);
        ClearStaticRegion(bootstrap.ShellView, UIShellRegionId.MiddleRegion);
        ClearStaticRegion(bootstrap.ShellView, UIShellRegionId.RightRegion);
        ClearStaticRegion(bootstrap.ShellView, UIShellRegionId.FooterRegion);
        ClearStaticRegion(bootstrap.ShellView, UIShellRegionId.PopupLayer);
        background.ClearContent();

        GameObject instance = UnityEngine.Object.Instantiate(prefab, background.ContentRoot, false);
        instance.name = prefab.name;
        Stretch(instance.GetComponent<RectTransform>());

        Transform header = FindChild(instance.transform, "HeaderContent");
        if (header != null)
            header.gameObject.SetActive(false);

        Debug.Log($"[CanvasRouteCaptureValidation] staticContentPrefab={prefabPath} mount=FullRoot");
        return true;
    }

    private static void ClearStaticRegion(UIShellView shellView, UIShellRegionId regionId)
    {
        if (shellView != null && shellView.TryGetRegion(regionId, out UIShellRegionView region) && region != null)
            region.ClearContent();
    }

    private static bool TryResolveStaticContentSection(
        GameObject prefab,
        UIShellContentSectionId sectionId,
        out GameObject source)
    {
        source = null;
        if (prefab == null)
            return false;

        UIShellContentSectionsView sections = prefab.GetComponent<UIShellContentSectionsView>();
        if (sections != null && sections.TryGetSection(sectionId, out source) && source != null)
            return true;

        string fallbackName = ToStaticContentRootName(sectionId);
        if (string.IsNullOrWhiteSpace(fallbackName))
            return false;

        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && string.Equals(transform.name, fallbackName, StringComparison.Ordinal))
            {
                source = transform.gameObject;
                return true;
            }
        }

        return false;
    }

    private static string ToStaticContentRootName(UIShellContentSectionId sectionId)
    {
        switch (sectionId)
        {
            case UIShellContentSectionId.MenuBackground:
                return "MenuBackgroundContent";
            case UIShellContentSectionId.Left:
                return "LeftContent";
            case UIShellContentSectionId.Middle:
                return "MiddleContent";
            case UIShellContentSectionId.Right:
                return "RightContent";
            case UIShellContentSectionId.Footer:
                return "FooterContent";
            default:
                return string.Empty;
        }
    }

    private static bool TryMountStaticSection(
        UIShellView shellView,
        GameObject source,
        UIShellRegionId regionId,
        out string error)
    {
        error = null;
        if (!shellView.TryGetRegion(regionId, out UIShellRegionView region) || region == null || region.ContentRoot == null)
        {
            error = $"Canvas route capture could not find target region={regionId} for static section={source.name}.";
            return false;
        }

        region.ClearContent();
        GameObject instance = UnityEngine.Object.Instantiate(source, region.ContentRoot, false);
        instance.name = source.name;
        Stretch(instance.GetComponent<RectTransform>());
        return true;
    }

    private static UIShellRegionId ToRegionId(UIShellContentSectionId sectionId)
    {
        switch (sectionId)
        {
            case UIShellContentSectionId.MenuBackground:
                return UIShellRegionId.MenuBackgroundRegion;
            case UIShellContentSectionId.Left:
                return UIShellRegionId.LeftRegion;
            case UIShellContentSectionId.Middle:
                return UIShellRegionId.MiddleRegion;
            case UIShellContentSectionId.Right:
                return UIShellRegionId.RightRegion;
            case UIShellContentSectionId.Footer:
                return UIShellRegionId.FooterRegion;
            default:
                return UIShellRegionId.MiddleRegion;
        }
    }

    private static void ResetRouteCaptureRegions(MenuBootstrapView bootstrap, params UIShellRegionId[] regionIds)
    {
        if (bootstrap == null || bootstrap.ShellView == null || regionIds == null)
            return;

        for (int i = 0; i < regionIds.Length; i++)
        {
            if (bootstrap.ShellView.TryGetRegion(regionIds[i], out UIShellRegionView region) && region != null)
                region.ResetVisualState();
        }
    }

    private static void DisableMenuDiagnosticsOverlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null ||
                !candidate.scene.IsValid() ||
                candidate.scene != activeScene)
            {
                continue;
            }

            string name = candidate.name;
            if (string.Equals(name, "MenuDiagnosticsPanel", StringComparison.Ordinal) ||
                string.Equals(name, "Panel_FPS", StringComparison.Ordinal) ||
                string.Equals(name, "Label_FPS", StringComparison.Ordinal))
            {
                candidate.SetActive(false);
            }
        }
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

    private static bool TryConfigureRouteCaptureArmoryCategory(out string error)
    {
        error = null;
        if (!routeCaptureShouldSetArmoryCategory)
            return true;

        if (routeCaptureRoute != UIRoute.Armory)
        {
            error = "WARLINE_CANVAS_ARMORY_CATEGORY requires WARLINE_CANVAS_ROUTE=Armory.";
            return false;
        }

        if (!UiShellRuntimeGateway.TryEnqueueArmoryCategory(routeCaptureArmoryCategory))
        {
            error = $"Canvas route capture could not enqueue Armory category={routeCaptureArmoryCategory}.";
            return false;
        }

        return true;
    }

    private static bool TryApplyRouteCaptureButtonSelection(out string error, out bool waitForSelectionSettle)
    {
        error = null;
        waitForSelectionSettle = false;
        if (string.IsNullOrWhiteSpace(routeCaptureSelectButtonName) || routeCaptureButtonSelectionApplied)
            return true;

        Button button = FindActiveButtonByName(routeCaptureSelectButtonName);
        if (button == null)
        {
            error = $"WARLINE_CANVAS_SELECT_BUTTON could not find active Button or child Button named '{routeCaptureSelectButtonName}'.";
            return false;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(button.gameObject);

        button.Select();
        routeCaptureButtonSelectionApplied = true;
        routeCaptureConfiguredFrame = routeCaptureFrameCount;
        waitForSelectionSettle = true;
        Debug.Log($"[CanvasRouteCaptureValidation] selectedButton={routeCaptureSelectButtonName}");
        return true;
    }

    private static Button FindActiveButtonByName(string buttonName)
    {
        if (string.IsNullOrWhiteSpace(buttonName))
            return null;

        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform == null || transform.name != buttonName)
                continue;

            Button directButton = transform.GetComponent<Button>();
            if (directButton != null && directButton.isActiveAndEnabled)
                return directButton;

            Button childButton = transform.GetComponentInChildren<Button>(false);
            if (childButton != null && childButton.isActiveAndEnabled)
                return childButton;
        }

        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.isActiveAndEnabled && button.gameObject.name == buttonName)
                return button;
        }

        string normalizedTarget = NormalizeLabel(buttonName);
        TMP_Text[] labels = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || NormalizeLabel(label.text) != normalizedTarget)
                continue;

            Button labelButton = label.GetComponentInParent<Button>();
            if (labelButton != null && labelButton.isActiveAndEnabled)
                return labelButton;
        }

        return null;
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        StringBuilder builder = new();
        for (int i = 0; i < label.Length; i++)
        {
            char c = label[i];
            if (char.IsWhiteSpace(c) || c == '_' || c == '-')
                continue;

            builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
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
        Scene matchScene = SceneManager.GetSceneByName(SceneLifecycleSceneSystemHelper.MatchSceneName);
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

    private static bool ResolveRouteCaptureArmoryCategory(out ArmoryCatalogCategory category)
    {
        string configuredCategory = Environment.GetEnvironmentVariable("WARLINE_CANVAS_ARMORY_CATEGORY");
        if (string.IsNullOrWhiteSpace(configuredCategory))
        {
            category = default;
            return false;
        }

        if (Enum.TryParse(configuredCategory, true, out category))
            return true;

        throw new InvalidOperationException($"Unsupported WARLINE_CANVAS_ARMORY_CATEGORY value: {configuredCategory}. Supported categories: Characters, Vehicles, Aircrafts, Buildings, Support.");
    }

    private static string ResolveRouteCaptureSelectButtonName()
    {
        string configuredName = Environment.GetEnvironmentVariable("WARLINE_CANVAS_SELECT_BUTTON");
        return string.IsNullOrWhiteSpace(configuredName) ? string.Empty : configuredName.Trim();
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

    private static string ResolveRouteCaptureStaticContentPrefabPath()
    {
        string configuredPath = Environment.GetEnvironmentVariable("WARLINE_CANVAS_STATIC_CONTENT_PREFAB");
        return string.IsNullOrWhiteSpace(configuredPath) ? string.Empty : configuredPath.Trim();
    }

    private static bool ResolveRouteCaptureStaticContentFullRoot()
    {
        string configuredValue = Environment.GetEnvironmentVariable("WARLINE_CANVAS_STATIC_CONTENT_FULL_ROOT");
        return string.Equals(configuredValue, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(configuredValue, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(configuredValue, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeRouteCaptureModal()
    {
        return string.IsNullOrWhiteSpace(routeCaptureModal) ? "None" : routeCaptureModal;
    }

    private static string DescribeRouteCaptureArmoryCategory()
    {
        return routeCaptureShouldSetArmoryCategory ? routeCaptureArmoryCategory.ToString() : "Default";
    }

    private static string DescribeRouteCaptureSelectedButton()
    {
        return string.IsNullOrWhiteSpace(routeCaptureSelectButtonName) ? "None" : routeCaptureSelectButtonName;
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

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private enum FrameKind
    {
        Secondary,
        Primary
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (string.Equals(root.name, childName, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChild(root.GetChild(i), childName);
            if (child != null)
                return child;
        }

        return null;
    }

    private static void MoveTo(GameObject target, Transform parent)
    {
        if (target == null || parent == null)
            return;

        target.transform.SetParent(parent, false);
        target.SetActive(true);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private static void SetRectTopLeft(GameObject target, float x, float y, float width, float height)
    {
        RectTransform rect = target != null ? target.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetRectFromTopLeft(GameObject target, float left, float top, float width, float height)
    {
        RectTransform rect = target != null ? target.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetRectCenter(GameObject target, float x, float y, float width, float height)
    {
        RectTransform rect = target != null ? target.transform as RectTransform : null;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void ApplyPanelFrame(GameObject panel, string spritePath, float pixelsPerUnitMultiplier)
    {
        if (panel == null)
            return;

        Image image = GetOrAddImage(panel);
        ApplySlicedSprite(image, spritePath, pixelsPerUnitMultiplier);
        image.raycastTarget = false;
        DisableDecorativeChrome(panel.transform);
    }

    private static void ApplyButtonFrame(GameObject buttonObject, FrameKind frameKind)
    {
        if (buttonObject == null)
            return;

        Image image = GetOrAddImage(buttonObject);
        string normalPath = frameKind == FrameKind.Primary
            ? "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_14_primary_gold_cta_frame.png"
            : "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_15_secondary_dark_cta_frame.png";
        string selectedPath = frameKind == FrameKind.Primary
            ? "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_cta_primary_gold_frame.png"
            : "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_10_selected_small_button_frame.png";

        ApplySlicedSprite(image, normalPath, frameKind == FrameKind.Primary ? 2.8f : 3.2f);
        DisableDecorativeChrome(buttonObject.transform);
        ConfigureButtonState(buttonObject.GetComponent<Button>(), image, normalPath, selectedPath);
    }

    private static void ApplyNavButtonState(GameObject buttonObject, bool selected)
    {
        if (buttonObject == null)
            return;

        Image image = GetOrAddImage(buttonObject);
        string normalPath = selected
            ? "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png"
            : "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_default.png";
        string selectedPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_button_frame_selected.png";
        ApplySlicedSprite(image, normalPath, 4.4f);
        DisableDecorativeChrome(buttonObject.transform);
        ConfigureButtonState(buttonObject.GetComponent<Button>(), image, normalPath, selectedPath);
    }

    private static void ConfigureButtonState(Button button, Image target, string normalPath, string selectedPath)
    {
        if (button == null || target == null)
            return;

        Sprite normal = LoadSprite(normalPath);
        Sprite selected = LoadSprite(selectedPath);
        button.transition = Selectable.Transition.SpriteSwap;
        button.targetGraphic = target;
        target.sprite = normal;

        SpriteState state = button.spriteState;
        state.highlightedSprite = selected;
        state.pressedSprite = selected;
        state.selectedSprite = selected;
        state.disabledSprite = normal;
        button.spriteState = state;
    }

    private static void LayoutIconLabelButton(
        GameObject button,
        float iconLeft,
        float iconTop,
        float iconWidth,
        float iconHeight,
        float labelLeft,
        float labelTop,
        float labelWidth,
        float labelHeight,
        float labelFontSize)
    {
        if (button == null)
            return;

        GameObject icon = FindChild(button.transform, "Icon")?.gameObject;
        SetRectFromTopLeft(icon, iconLeft, iconTop, iconWidth, iconHeight);

        GameObject label = FindChild(button.transform, "Label")?.gameObject;
        SetRectFromTopLeft(label, labelLeft, labelTop, labelWidth, labelHeight);
        TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
        if (text != null)
        {
            text.enableAutoSizing = false;
            text.fontSize = labelFontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(0.95f, 0.88f, 0.66f, 1f);
            text.raycastTarget = false;
        }
    }

    private static void SetChildSprite(GameObject root, string childName, string spritePath, bool preserveAspect)
    {
        GameObject child = FindChild(root != null ? root.transform : null, childName)?.gameObject;
        if (child == null)
            return;

        Image image = GetOrAddImage(child);
        Sprite sprite = LoadSprite(spritePath);
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void SetTextBlock(
        Transform root,
        string childName,
        float left,
        float top,
        float width,
        float height,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject child = FindChild(root, childName)?.gameObject;
        SetRectFromTopLeft(child, left, top, width, height);
        TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;
        if (text == null)
            return;

        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(color.r, color.g, color.b, 1f);
        text.raycastTarget = false;
    }

    private static void SetTextValue(Transform root, string childName, string value)
    {
        TMP_Text text = FindChild(root, childName)?.GetComponent<TMP_Text>();
        if (text != null)
            text.text = value;
    }

    private static void ConfigureTextChild(
        GameObject parent,
        string name,
        string value,
        float left,
        float top,
        float width,
        float height,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        if (parent == null)
            return;

        Transform existing = parent.transform.Find(name);
        GameObject child = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (existing == null)
            child.transform.SetParent(parent.transform, false);

        SetRectFromTopLeft(child, left, top, width, height);
        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = child.AddComponent<TextMeshProUGUI>();

        text.text = value;
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(color.r, color.g, color.b, 1f);
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private static void ConfigureSolidImageChild(
        GameObject parent,
        string name,
        float left,
        float top,
        float width,
        float height,
        Color color)
    {
        if (parent == null)
            return;

        Transform existing = parent.transform.Find(name);
        GameObject child = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (existing == null)
            child.transform.SetParent(parent.transform, false);

        SetRectFromTopLeft(child, left, top, width, height);
        child.transform.SetAsFirstSibling();
        Image image = GetOrAddImage(child);
        if (image == null)
            return;

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void ApplySlicedSprite(Image image, string spritePath, float pixelsPerUnitMultiplier)
    {
        if (image == null)
            return;

        Sprite sprite = LoadSprite(spritePath);
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.color = Color.white;
    }

    private static Image GetOrAddImage(GameObject target)
    {
        if (target == null)
            return null;

        Image image = target.GetComponent<Image>();
        if (image == null)
            image = target.AddComponent<Image>();

        return image;
    }

    private static void DisableImage(GameObject target)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image != null)
            image.enabled = false;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void HideAllChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);
    }

    private static void HideDirectChildrenExcept(Transform parent, params string[] keptNames)
    {
        if (parent == null)
            return;

        HashSet<string> kept = new(keptNames, StringComparer.Ordinal);
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(kept.Contains(child.name));
        }
    }

    private static void DisableDecorativeChrome(Transform root)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (IsDecorativeChromeName(child.name))
                child.gameObject.SetActive(false);
            else
                DisableDecorativeChrome(child);
        }
    }

    private static bool IsDecorativeChromeName(string name)
    {
        return string.Equals(name, "Frame", StringComparison.Ordinal)
            || string.Equals(name, "Plate", StringComparison.Ordinal)
            || string.Equals(name, "HeaderFrame", StringComparison.Ordinal)
            || name.Contains("Stroke", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetAllTextSize(GameObject root, float size)
    {
        if (root == null)
            return;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null)
                continue;

            label.enableAutoSizing = false;
            label.fontSize = size;
            label.color = new Color(0.93f, 0.87f, 0.67f, 1f);
            label.raycastTarget = false;
        }
    }

    private static void SetStatTextSize(GameObject panel)
    {
        if (panel == null)
            return;

        TMP_Text[] labels = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null)
                continue;

            string text = label.text ?? string.Empty;
            if (text.IndexOf("OVERVIEW", StringComparison.OrdinalIgnoreCase) >= 0)
                label.fontSize = 42f;
            else if (ContainsDigit(text))
                label.fontSize = 58f;
            else
                label.fontSize = 30f;
        }
    }

    private static bool ContainsDigit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
                return true;
        }

        return false;
    }

    private static void LayoutCommanderIdentity(GameObject panel)
    {
        if (panel == null)
            return;

        GameObject portraitPanel = FindChild(panel.transform, "PortraitPanel")?.gameObject;
        SetRectFromTopLeft(portraitPanel, 80f, 90f, 620f, 600f);
        ApplyPanelFrame(portraitPanel, "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png", 4.4f);

        GameObject portrait = FindChild(panel.transform, "Portrait")?.gameObject;
        if (portraitPanel != null)
            MoveTo(portrait, portraitPanel.transform);
        SetRectFromTopLeft(portrait, 58f, 54f, 504f, 492f);
        Image portraitImage = GetOrAddImage(portrait);
        Sprite portraitSprite = LoadSprite("Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_portrait_01_commander_portrait_shadowed.png");
        if (portraitImage != null && portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitImage.type = Image.Type.Simple;
            portraitImage.preserveAspect = true;
            portraitImage.color = Color.white;
        }

        GameObject identityCard = FindChild(panel.transform, "IdentityCard")?.gameObject;
        SetRectFromTopLeft(identityCard, 760f, 115f, 1040f, 535f);
        DisableImage(identityCard);
        ConfigureSolidImageChild(identityCard, "IdentityTextBacking", 190f, 30f, 825f, 420f, new Color(0.02f, 0.025f, 0.022f, 0.58f));
        SetChildSprite(identityCard, "Badge", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_02_commander_rank_shield.png", true);
        ConfigureTextChild(identityCard, "RoleLabel", "FIELD COMMANDER", 235f, 45f, 760f, 64f, 38f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.83f, 0.22f));
        ConfigureTextChild(identityCard, "CommanderNameLabel", "COL. ALEX MORGAN", 235f, 132f, 790f, 118f, 62f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.9f, 0.76f));
        ConfigureTextChild(identityCard, "MottoLabel", "VICTORY IS PLANNED", 235f, 275f, 790f, 58f, 34f, TextAlignmentOptions.MidlineLeft, new Color(0.84f, 0.78f, 0.55f));
        ConfigureTextChild(identityCard, "CommanderLevelLabel", "LEVEL 38", 235f, 370f, 520f, 68f, 40f, TextAlignmentOptions.MidlineLeft, new Color(0.92f, 0.74f, 0.22f));
        GameObject rankBadge = FindChild(identityCard.transform, "Badge")?.gameObject;
        SetRectFromTopLeft(rankBadge, 44f, 112f, 160f, 160f);

        GameObject editButton = FindChild(panel.transform, "EditIdButton")?.gameObject;
        if (portraitPanel != null)
            MoveTo(editButton, portraitPanel.transform);
        SetRectFromTopLeft(editButton, 480f, 42f, 105f, 105f);
        ApplyButtonFrame(editButton, FrameKind.Secondary);
        SetChildSprite(editButton, "Icon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_09_edit_pencil.png", true);
        GameObject editIcon = FindChild(editButton != null ? editButton.transform : null, "Icon")?.gameObject;
        SetRectFromTopLeft(editIcon, 28f, 28f, 49f, 49f);
        HideDirectChildrenExcept(editButton != null ? editButton.transform : null, "Icon");
        HideDirectChildrenExcept(portraitPanel != null ? portraitPanel.transform : null, "Portrait", "EditIdButton");

        GameObject badgesButton = FindChild(panel.transform, "BadgesButton")?.gameObject;
        SetActive(badgesButton, false);
        HideDirectChildrenExcept(identityCard != null ? identityCard.transform : null, "IdentityTextBacking", "Badge", "RoleLabel", "CommanderNameLabel", "MottoLabel", "CommanderLevelLabel");
        HideDirectChildrenExcept(panel.transform, "PortraitPanel", "IdentityCard");
    }

    private static void LayoutOverviewStats(GameObject panel)
    {
        if (panel == null)
            return;

        SetTextBlock(panel.transform, "Title", 70f, 28f, 600f, 70f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
        string[] cards = { "VictoriesStatCard", "MissionsStatCard", "CiviliansStatCard", "LostStatCard" };
        string[] labels = { "VICTORIES", "MISSIONS", "CIVILIANS", "UNITS LOST" };
        string[] values = { "128", "246", "8,642", "312" };
        string[] suffixes = { "86% success", "completed", "protected", "lifetime" };
        string[] iconPaths =
        {
            "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_15_reward_wreath.png",
            "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_armory_crossed_weapons.png",
            "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_11_roster_group.png",
            "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_12_vehicle.png"
        };

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject card = FindChild(panel.transform, cards[i])?.gameObject;
            SetRectFromTopLeft(card, 60f + (i * 455f), 115f, 420f, 190f);
            ApplyPanelFrame(card, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_12_small_chip_frame.png", 5.2f);
            SetChildSprite(card, "Icon", iconPaths[i], true);
            GameObject icon = FindChild(card != null ? card.transform : null, "Icon")?.gameObject;
            SetRectFromTopLeft(icon, 40f, 58f, 76f, 76f);
            SetTextValue(card != null ? card.transform : null, "Label", labels[i]);
            SetTextValue(card != null ? card.transform : null, "Value", values[i]);
            SetTextValue(card != null ? card.transform : null, "Suffix", suffixes[i]);
            SetTextBlock(card != null ? card.transform : null, "Label", 142f, 25f, 230f, 48f, 30f, TextAlignmentOptions.Midline, new Color(0.94f, 0.88f, 0.68f));
            SetTextBlock(card != null ? card.transform : null, "Value", 140f, 74f, 230f, 72f, 54f, TextAlignmentOptions.Midline, new Color(0.92f, 0.74f, 0.22f));
            SetTextBlock(card != null ? card.transform : null, "Suffix", 140f, 140f, 230f, 38f, 24f, TextAlignmentOptions.Midline, new Color(0.64f, 0.78f, 0.32f));
            HideDirectChildrenExcept(card != null ? card.transform : null, "Icon", "Label", "Value", "Suffix");
        }

        HideDirectChildrenExcept(panel.transform, "Title", "VictoriesStatCard", "MissionsStatCard", "CiviliansStatCard", "LostStatCard");
    }

    private static void LayoutAccountSnapshot(GameObject panel)
    {
        if (panel == null)
            return;

        SetTextBlock(panel.transform, "Title", 70f, 34f, 720f, 70f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
        string[] names = { "CampaignSnapshot", "OperationsSnapshot", "SkirmishSnapshot", "ReadinessSnapshot" };
        string[] labels = { "CAMPAIGN", "OPERATIONS", "SKIRMISH", "READINESS" };
        string[] values = { "1,750", "1,620", "1,480", "HIGH" };
        Vector2[] positions =
        {
            new(70f, 145f),
            new(980f, 145f),
            new(70f, 315f),
            new(980f, 315f)
        };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject row = FindChild(panel.transform, names[i])?.gameObject;
            SetRectFromTopLeft(row, positions[i].x, positions[i].y, 830f, 130f);
            ApplyPanelFrame(row, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_12_small_chip_frame.png", 5.2f);
            SetTextValue(row != null ? row.transform : null, "Label", labels[i]);
            SetTextValue(row != null ? row.transform : null, "Value", values[i]);
            SetTextBlock(row != null ? row.transform : null, "Label", 58f, 16f, 420f, 42f, 32f, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.88f, 0.68f));
            SetTextBlock(row != null ? row.transform : null, "Value", 58f, 64f, 420f, 52f, 40f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.9f, 0.76f));
            HideDirectChildrenExcept(row != null ? row.transform : null, "Icon", "Label", "Value");
        }

        HideDirectChildrenExcept(panel.transform, "Title", "CampaignSnapshot", "OperationsSnapshot", "SkirmishSnapshot", "ReadinessSnapshot");
    }

    private static void LayoutRewardTrack(GameObject panel)
    {
        if (panel == null)
            return;

        SetTextBlock(panel.transform, "Title", 80f, 35f, 920f, 70f, 46f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
        GameObject progress = FindChild(panel.transform, "XpProgress")?.gameObject;
        SetRectFromTopLeft(progress, 100f, 145f, 1180f, 160f);
        GameObject track = FindChild(progress != null ? progress.transform : null, "Track")?.gameObject;
        SetRectFromTopLeft(track, 0f, 36f, 980f, 62f);
        ApplyPanelFrame(track, "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_progress_meter_empty_frame.png", 4.2f);
        GameObject fill = FindChild(track != null ? track.transform : null, "Fill")?.gameObject;
        SetRectFromTopLeft(fill, 22f, 18f, 680f, 28f);
        SetChildSprite(track, "Fill", "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_progress_fill_olive_segment.png", false);
        SetTextBlock(progress != null ? progress.transform : null, "XpLabel", 0f, 105f, 980f, 48f, 30f, TextAlignmentOptions.Midline, new Color(0.9f, 0.84f, 0.64f));

        string[] nodeNames = { "Node35", "Node36", "Node37", "Node38", "Node39", "Node40" };
        for (int i = 0; i < nodeNames.Length; i++)
        {
            GameObject node = FindChild(panel.transform, nodeNames[i])?.gameObject;
            SetRectFromTopLeft(node, 115f + (i * 238f), 350f, 145f, 145f);
            ApplySlicedSprite(GetOrAddImage(node), i == 3
                ? "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png"
                : "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_19_reward_node_locked.png", 4.6f);
            SetTextBlock(node != null ? node.transform : null, "Level", 0f, 0f, 145f, 145f, 38f, TextAlignmentOptions.Center, new Color(0.92f, 0.82f, 0.45f));
            HideDirectChildrenExcept(node != null ? node.transform : null, "Level");
        }

        SetTextBlock(panel.transform, "RewardText", 105f, 540f, 920f, 55f, 30f, TextAlignmentOptions.MidlineLeft, new Color(0.92f, 0.84f, 0.58f));
        GameObject claimButton = FindChild(panel.transform, "ClaimButton")?.gameObject;
        SetRectFromTopLeft(claimButton, 1170f, 560f, 430f, 140f);
        ApplyButtonFrame(claimButton, FrameKind.Primary);
        LayoutIconLabelButton(claimButton, 312f, 42f, 58f, 56f, 44f, 0f, 260f, 140f, 42f);
        SetChildSprite(claimButton, "Icon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_20_claim_chevron.png", true);
        HideDirectChildrenExcept(claimButton != null ? claimButton.transform : null, "Icon", "Label");
        HideDirectChildrenExcept(panel.transform, "Title", "XpProgress", "Node35", "Node36", "Node37", "Node38", "Node39", "Node40", "RewardText", "ClaimButton");
    }

    private static void LayoutRecentHistory(GameObject panel)
    {
        if (panel == null)
            return;

        SetTextBlock(panel.transform, "Title", 80f, 34f, 720f, 70f, 46f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
        string[] rows = { "FirstContactRow", "OldMarketRow" };
        for (int i = 0; i < rows.Length; i++)
        {
            GameObject row = FindChild(panel.transform, rows[i])?.gameObject;
            SetRectFromTopLeft(row, 75f, 150f + (i * 165f), 1590f, 135f);
            ApplyPanelFrame(row, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_12_small_chip_frame.png", 5.2f);
            SetTextValue(row != null ? row.transform : null, "Title", i == 0 ? "HOSTILE PATROL" : "SUPPLY RUN");
            SetTextValue(row != null ? row.transform : null, "Subtitle", i == 0 ? "CAMPAIGN  |  VICTORY" : "OPERATIONS  |  VICTORY");
            SetTextValue(row != null ? row.transform : null, "Time", i == 0 ? "1h ago" : "3h ago");
            SetTextBlock(row != null ? row.transform : null, "Title", 72f, 18f, 900f, 46f, 36f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.89f, 0.68f));
            SetTextBlock(row != null ? row.transform : null, "Subtitle", 72f, 72f, 1040f, 38f, 26f, TextAlignmentOptions.MidlineLeft, new Color(0.76f, 0.72f, 0.58f));
            SetTextBlock(row != null ? row.transform : null, "Time", 1250f, 22f, 250f, 42f, 28f, TextAlignmentOptions.MidlineRight, new Color(0.64f, 0.78f, 0.32f));
            HideDirectChildrenExcept(row != null ? row.transform : null, "Icon", "Title", "Subtitle", "Time");
        }

        HideDirectChildrenExcept(panel.transform, "Title", "FirstContactRow", "OldMarketRow");
    }

    private static void PrepareFooterButton(
        GameObject button,
        Transform footer,
        float x,
        string labelText,
        FrameKind frameKind)
    {
        MoveTo(button, footer);
        float width = frameKind == FrameKind.Primary ? 820f : 680f;
        SetRectFromTopLeft(button, x, 22f, width, 170f);
        ApplyButtonFrame(button, frameKind);
        SetButtonLabel(button, labelText);
        LayoutIconLabelButton(button, 65f, 46f, 78f, 72f, 170f, 0f, width - 215f, 170f, 56f);
        HideDirectChildrenExcept(button != null ? button.transform : null, "Icon", "Label");
    }

    private static void SetButtonLabel(GameObject button, string text)
    {
        Transform label = button != null ? FindChild(button.transform, "Label") : null;
        TMP_Text labelText = label != null ? label.GetComponent<TMP_Text>() : null;
        if (labelText != null)
            labelText.text = text;
    }

    private static void HideFooterBreadcrumbs(
        Transform footer,
        GameObject openArmoryButton,
        GameObject detailButton,
        GameObject replayButton)
    {
        if (footer == null)
            return;

        for (int i = 0; i < footer.childCount; i++)
        {
            GameObject child = footer.GetChild(i).gameObject;
            if (child != openArmoryButton && child != detailButton && child != replayButton)
                child.SetActive(false);
        }
    }

    private static void ApplyPanelBackground(GameObject root)
    {
        Transform background = FindChild(root.transform, "MenuBackgroundContent");
        if (background == null)
            return;

        Image image = GetOrAddImage(background.gameObject);
        Sprite sprite = LoadSprite("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_background_command_table_no_ui.png");
        if (sprite == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = Color.white;
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
