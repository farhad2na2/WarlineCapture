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
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;
using Game.Composition;

namespace Game.Editor
{
    public static class CanvasMenuFallbackValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const string MainMenuContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        private const string ScreenshotPath = "/private/tmp/warline-canvas-menu-fallback.png";
        private const int DefaultScreenshotWidth = 1280;
        private const int DefaultScreenshotHeight = 720;
        private const float MinimumScreenshotDetail = 0.015f;
        private const double MinimumRouteCaptureSettleSeconds = 0.35d;
        private const string CommanderCapturePendingSessionKey = "Warline.CommanderCapture.Pending";
        private const string CommanderCapturePathSessionKey = "Warline.CommanderCapture.Path";
        private const string CommanderCaptureWidthSessionKey = "Warline.CommanderCapture.Width";
        private const string CommanderCaptureHeightSessionKey = "Warline.CommanderCapture.Height";
        private const string CommanderCapturePreviousFastPlayModeSessionKey = "Warline.CommanderCapture.PreviousFastPlayMode";
        private const string CommanderCapturePreviousPlayModeOptionsSessionKey = "Warline.CommanderCapture.PreviousPlayModeOptions";

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
        private static double routeCaptureConfiguredAt;
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

        [InitializeOnLoadMethod]
        private static void ResumeCommanderCaptureAfterDomainReload()
        {
            if (!SessionState.GetBool(CommanderCapturePendingSessionKey, false))
                return;

            screenshotPath = SessionState.GetString(CommanderCapturePathSessionKey, string.Empty);
            screenshotWidth = SessionState.GetInt(CommanderCaptureWidthSessionKey, 1920);
            screenshotHeight = SessionState.GetInt(CommanderCaptureHeightSessionKey, 1080);
            routeCaptureRoute = UIRoute.CommandFeed;
            routeCaptureShouldShowPopup = false;
            routeCapturePopup = default;
            routeCaptureOverlay = string.Empty;
            routeCaptureModal = string.Empty;
            routeCaptureStaticContentPrefabPath = string.Empty;
            routeCaptureStaticContentFullRoot = false;
            routeCaptureShouldSetArmoryCategory = false;
            routeCaptureArmoryCategory = default;
            routeCaptureSelectButtonName = string.Empty;
            routeCaptureSettleFrames = 90;
            routeCaptureFrameCount = 0;
            routeCaptureConfiguredFrame = 0;
            routeCaptureStartedAt = EditorApplication.timeSinceStartup;
            routeCaptureConfiguredAt = 0d;
            routeCaptureCompleted = false;
            routeCaptureConfigured = false;
            routeCaptureButtonSelectionApplied = false;
            EditorApplication.update -= ContinueRouteCapture;
            EditorApplication.update += ContinueRouteCapture;
        }

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
                if (Application.isBatchMode)
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
                if (Application.isBatchMode)
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
                routeCaptureConfiguredAt = 0d;
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
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        public static void RunCommanderProfileRouteCapture()
        {
            try
            {
                RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

                SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                string configuredCapturePath = Environment.GetEnvironmentVariable("WARLINE_COMMANDER_CAPTURE_PATH");
                screenshotPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredCapturePath)
                    ? "Design/AgentReports/Captures/commander_profile_route_capture.png"
                    : configuredCapturePath.Trim());
                Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? ".");
                screenshotWidth = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_WIDTH", 1920);
                screenshotHeight = ResolveScreenshotDimension("WARLINE_CANVAS_SCREENSHOT_HEIGHT", 1080);
                routeCaptureRoute = UIRoute.CommandFeed;
                routeCaptureShouldShowPopup = false;
                routeCapturePopup = default;
                routeCaptureOverlay = string.Empty;
                routeCaptureModal = string.Empty;
                routeCaptureStaticContentPrefabPath = string.Empty;
                routeCaptureStaticContentFullRoot = false;
                routeCaptureShouldSetArmoryCategory = false;
                routeCaptureArmoryCategory = default;
                routeCaptureSelectButtonName = string.Empty;
                routeCaptureSettleFrames = 90;
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
                SessionState.SetBool(CommanderCapturePendingSessionKey, true);
                SessionState.SetString(CommanderCapturePathSessionKey, screenshotPath);
                SessionState.SetInt(CommanderCaptureWidthSessionKey, screenshotWidth);
                SessionState.SetInt(CommanderCaptureHeightSessionKey, screenshotHeight);
                SessionState.SetBool(CommanderCapturePreviousFastPlayModeSessionKey, EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt(CommanderCapturePreviousPlayModeOptionsSessionKey, (int)EditorSettings.enterPlayModeOptions);
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                RestoreCommanderCapturePlayModeSettings();
                Debug.LogError($"[CanvasRouteCaptureValidation] result=Failed\n{exception}");
                if (Application.isBatchMode)
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
                if (Application.isBatchMode)
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

                RemoveDirectChild(root.transform, "HeaderContent");
                RemoveDirectChild(root.transform, "MenuBackgroundContent");
                SetRectFromTopLeft(left.gameObject, 0f, 0f, 760f, 1700f);
                SetRectFromTopLeft(middle.gameObject, 0f, 0f, 1760f, 1700f);
                SetRectFromTopLeft(right.gameObject, 0f, 0f, 1500f, 1700f);
                SetRectFromTopLeft(footer.gameObject, 0f, 0f, 3500f, 260f);

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
                SetRectFromTopLeft(backButton, 36f, 0f, 480f, 120f);
                EnsureButtonInteraction(backButton);
                ApplyNavButtonState(backButton, false);
                ConfigureRouteButton(backButton, UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, false);
                LayoutIconLabelButton(backButton, 38f, 28f, 64f, 60f, 122f, 0f, 350f, 120f, 40f);
                SetActive(FindChild(backButton != null ? backButton.transform : null, "Icon")?.gameObject, false);
                ConfigureSpriteChild(backButton, "NavIcon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png", 38f, 28f, 64f, 60f);
                HideDirectChildrenExcept(backButton != null ? backButton.transform : null, "NavIcon", "Label");

                string[] tabNames = { "OverviewTab", "StatsTab", "BadgesTab", "HistoryTab", "UpgradesTab" };
                string[] tabIconPaths =
                {
                    "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_command_shield_icon.png",
                    "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_resource_diamond_icon.png",
                    "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png",
                    "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png",
                    "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_nav_tech_tree_nodes_icon.png"
                };
                for (int i = 0; i < tabNames.Length; i++)
                {
                    GameObject tab = FindChild(root.transform, tabNames[i])?.gameObject;
                    MoveTo(tab, left);
                    SetRectFromTopLeft(tab, 36f, 165f + (i * 155f), 700f, 132f);
                    EnsureButtonInteraction(tab);
                    ApplyNavButtonState(tab, i == 0);
                    LayoutIconLabelButton(tab, 48f, 27f, 78f, 78f, 154f, 0f, 490f, 132f, 46f);
                    SetActive(FindChild(tab != null ? tab.transform : null, "Icon")?.gameObject, false);
                    ConfigureSpriteChild(tab, "NavIcon", tabIconPaths[i], 48f, 27f, 78f, 78f);
                    if (i > 0)
                    {
                        ConfigureSpriteChild(
                            tab,
                            "LockedState",
                            "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/pop12_icon_12_lock_badge.png",
                            620f,
                            39f,
                            38f,
                            52f);
                    }
                    else
                    {
                        SetActive(FindChild(tab != null ? tab.transform : null, "LockedState")?.gameObject, false);
                    }
                    HideDirectChildrenExcept(tab != null ? tab.transform : null, "NavIcon", "Label", "LockedState");
                    SetButtonAvailability(tab, false, i == 0 ? 1f : 0.62f);
                }
                HideDirectChildrenExcept(left, "BackButton", "OverviewTab", "StatsTab", "BadgesTab", "HistoryTab", "UpgradesTab");

                MoveTo(identityPanel, middle);
                SetRectFromTopLeft(identityPanel, 120f, -240f, 1650f, 720f);
                ApplyPanelFrame(identityPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 1f);
                LayoutCommanderIdentity(identityPanel);

                MoveTo(overviewPanel, middle);
                SetRectFromTopLeft(overviewPanel, 120f, 510f, 1650f, 360f);
                ApplyPanelFrame(overviewPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 1f);
                LayoutOverviewStats(overviewPanel);

                MoveTo(accountPanel, middle);
                SetRectFromTopLeft(accountPanel, 120f, 900f, 1650f, 700f);
                ApplyPanelFrame(accountPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 1f);
                LayoutAccountSnapshot(accountPanel);
                ConfigureCommanderProfileBinding(middle.gameObject, identityPanel);
                HideDirectChildrenExcept(middle, "CommanderIdentityPanel", "OverviewPanel", "AccountSnapshotPanel");

                MoveTo(rewardPanel, right);
                SetRectFromTopLeft(rewardPanel, -1500f, 20f, 1380f, 760f);
                ApplyPanelFrame(rewardPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 1f);
                LayoutRewardTrack(rewardPanel);

                MoveTo(historyPanel, right);
                SetRectFromTopLeft(historyPanel, -1500f, 810f, 1380f, 1020f);
                ApplyPanelFrame(historyPanel, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png", 1f);
                LayoutRecentHistory(historyPanel);
                HideDirectChildrenExcept(right, "RewardTrackPanel", "RecentHistoryPanel");

                SetActive(armoryPanel, false);
                SetActive(profileRewardsPanel, false);

                GameObject footerRail = ConfigureSolidImageChildAndReturn(
                    footer.gameObject,
                    "CommanderFooterRail",
                    720f,
                    -220f,
                    2760f,
                    230f,
                    new Color(0.015f, 0.018f, 0.015f, 0.88f));
                ApplyPanelFrame(footerRail, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png", 0.76f);
                PrepareFooterButton(openArmoryButton, footer, 800f, "OPEN ARMORY", FrameKind.Primary);
                PrepareFooterButton(detailButton, footer, 1700f, "DETAIL", FrameKind.Secondary);
                PrepareFooterButton(replayButton, footer, 2450f, "REPLAY", FrameKind.Primary);
                RemoveDuplicateDirectChildren(footer, "OpenArmoryButton", openArmoryButton);
                RemoveDuplicateDirectChildren(footer, "DetailButton", detailButton);
                RemoveDuplicateDirectChildren(footer, "ReplayButton", replayButton);
                ConfigureFooterActionIcon(openArmoryButton, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_04_supplies_crate.png");
                ConfigureFooterActionIcon(detailButton, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_05_command_shield.png");
                ConfigureFooterActionIcon(replayButton, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_20_claim_chevron.png");
                ConfigureRouteButton(openArmoryButton, UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory, true);
                SetButtonAvailability(openArmoryButton, true, 1f);
                SetButtonAvailability(detailButton, false, 0.62f);
                SetButtonAvailability(replayButton, false, 0.62f);
                HideFooterBreadcrumbs(footer, footerRail, openArmoryButton, detailButton, replayButton);
                HideDirectChildrenExcept(footer, "CommanderFooterRail", "OpenArmoryButton", "DetailButton", "ReplayButton");

                ConfigureCommanderProfileResponsiveLayout(
                    middle.gameObject,
                    right.gameObject,
                    footer.gameObject,
                    identityPanel,
                    overviewPanel,
                    accountPanel,
                    rewardPanel,
                    historyPanel,
                    footerRail,
                    openArmoryButton,
                    detailButton,
                    replayButton);
                ConfigureCommanderProfileContentSections(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[CanvasCommanderProfileTargetLockLayout] result=Passed prefab={PrefabPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CanvasCommanderProfileTargetLockLayout] result=Failed\n{exception}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }

            ApplyMainMenuCommanderRouteWiring();
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode)
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

                if (!TryRenderCameraMetrics(bootstrap.UiCamera, screenshotPath, screenshotWidth, screenshotHeight, out float luma, out float detail, out string renderError))
                {
                    Complete(false, renderError);
                    return;
                }

                if (luma < 0.05f)
                {
                    Complete(false, $"Captured Canvas menu screenshot is still black or near-black. luma={luma:0.000} path={screenshotPath}");
                    return;
                }

                if (detail < MinimumScreenshotDetail)
                {
                    Complete(false, $"Captured Canvas menu screenshot is visually flat. luma={luma:0.000} detail={detail:0.000} minimumDetail={MinimumScreenshotDetail:0.000} path={screenshotPath}");
                    return;
                }

                Complete(true, $"Canvas menu deploy UI is visible. luma={luma:0.000} detail={detail:0.000} size={screenshotWidth}x{screenshotHeight} path={screenshotPath}");
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
                    routeCaptureConfiguredAt = EditorApplication.timeSinceStartup;
                    DisableMenuDiagnosticsOverlay();
                    return;
                }

                if (routeCaptureFrameCount - routeCaptureConfiguredFrame < routeCaptureSettleFrames ||
                    EditorApplication.timeSinceStartup - routeCaptureConfiguredAt < MinimumRouteCaptureSettleSeconds)
                    return;

                DisableMenuDiagnosticsOverlay();
                if (!TryApplyRouteCaptureButtonSelection(out string selectionError, out bool waitForSelectionSettle))
                {
                    CompleteRouteCapture(false, selectionError);
                    return;
                }
                if (waitForSelectionSettle)
                    return;

                ApplyCommanderResponsiveCaptureLayout();
                if (!TryRenderCameraMetrics(bootstrap.UiCamera, screenshotPath, screenshotWidth, screenshotHeight, out float luma, out float detail, out string renderError))
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

                if (detail < MinimumScreenshotDetail)
                {
                    CompleteRouteCapture(false, $"Captured Canvas route screenshot is visually flat. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} armoryCategory={DescribeRouteCaptureArmoryCategory()} selectedButton={DescribeRouteCaptureSelectedButton()} luma={luma:0.000} detail={detail:0.000} minimumDetail={MinimumScreenshotDetail:0.000} path={screenshotPath}");
                    return;
                }

                CompleteRouteCapture(true, $"Canvas route is visible. route={routeCaptureRoute} popup={DescribeRouteCapturePopup()} overlay={DescribeRouteCaptureOverlay()} modal={DescribeRouteCaptureModal()} armoryCategory={DescribeRouteCaptureArmoryCategory()} selectedButton={DescribeRouteCaptureSelectedButton()} luma={luma:0.000} detail={detail:0.000} size={screenshotWidth}x{screenshotHeight} path={screenshotPath}");
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
                case UIRoute.CommandExchange:
                case UIRoute.Inbox:
                case UIRoute.Events:
                case UIRoute.Ranking:
                case UIRoute.LoadoutSquadPrep:
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
                case UIRoute.CommandFeed:
                    content.PrepareForCommandSequence(new[]
                    {
                        new UiShellPresentationCommandModel(
                            UiShellCommandKind.EnterMenu,
                            UiShellRegionId.None,
                            UIRoute.MainMenu,
                            UiShellMode.MainMenu,
                            1)
                    });
                    content.InstallMenuRouteBody(UIRoute.CommandFeed);
                    ResetRouteCaptureRegions(
                        bootstrap,
                        UIShellRegionId.MenuBackgroundRegion,
                        UIShellRegionId.HeaderRegion,
                        UIShellRegionId.LeftRegion,
                        UIShellRegionId.MiddleRegion,
                        UIShellRegionId.RightRegion,
                        UIShellRegionId.FooterRegion);
                    break;
                case UIRoute.QuickCustomSetup:
                    content.PrepareForCommandSequence(new[]
                    {
                        new UiShellPresentationCommandModel(
                            UiShellCommandKind.EnterMenu,
                            UiShellRegionId.None,
                            UIRoute.MainMenu,
                            UiShellMode.MainMenu,
                            1)
                    });
                    content.InstallMenuRouteBody(UIRoute.QuickCustomSetup);
                    ResetRouteCaptureRegions(
                        bootstrap,
                        UIShellRegionId.MenuBackgroundRegion,
                        UIShellRegionId.HeaderRegion,
                        UIShellRegionId.LeftRegion,
                        UIShellRegionId.MiddleRegion,
                        UIShellRegionId.RightRegion,
                        UIShellRegionId.FooterRegion,
                        UIShellRegionId.PopupLayer);
                    break;
                case UIRoute.Campaign:
                    content.PrepareForCommandSequence(new[]
                    {
                        new UiShellPresentationCommandModel(
                            UiShellCommandKind.EnterMenu,
                            UiShellRegionId.None,
                            UIRoute.MainMenu,
                            UiShellMode.MainMenu,
                            1)
                    });
                    content.InstallMenuRouteBody(UIRoute.Campaign);
                    ResetRouteCaptureRegions(
                        bootstrap,
                        UIShellRegionId.MenuBackgroundRegion,
                        UIShellRegionId.HeaderRegion,
                        UIShellRegionId.LeftRegion,
                        UIShellRegionId.MiddleRegion,
                        UIShellRegionId.RightRegion,
                        UIShellRegionId.FooterRegion,
                        UIShellRegionId.PopupLayer);
                    break;
                case UIRoute.MissionBriefing:
                    content.PrepareForCommandSequence(new[]
                    {
                        new UiShellPresentationCommandModel(
                            UiShellCommandKind.EnterMenu,
                            UiShellRegionId.None,
                            UIRoute.Campaign,
                            UiShellMode.MainMenu,
                            1)
                    });
                    content.InstallMenuRouteBody(UIRoute.MissionBriefing);
                    ResetRouteCaptureRegions(
                        bootstrap,
                        UIShellRegionId.MenuBackgroundRegion,
                        UIShellRegionId.HeaderRegion,
                        UIShellRegionId.LeftRegion,
                        UIShellRegionId.MiddleRegion,
                        UIShellRegionId.RightRegion,
                        UIShellRegionId.FooterRegion,
                        UIShellRegionId.PopupLayer);
                    break;
                case UIRoute.Operations:
                    content.PrepareForCommandSequence(new[]
                    {
                        new UiShellPresentationCommandModel(
                            UiShellCommandKind.EnterMenu,
                            UiShellRegionId.None,
                            UIRoute.MainMenu,
                            UiShellMode.MainMenu,
                            1)
                    });
                    content.InstallMenuRouteBody(UIRoute.Operations);
                    ResetRouteCaptureRegions(
                        bootstrap,
                        UIShellRegionId.MenuBackgroundRegion,
                        UIShellRegionId.HeaderRegion,
                        UIShellRegionId.LeftRegion,
                        UIShellRegionId.MiddleRegion,
                        UIShellRegionId.RightRegion,
                        UIShellRegionId.FooterRegion,
                        UIShellRegionId.PopupLayer);
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
                    error = $"Canvas route capture does not support route={routeCaptureRoute}.";
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
            if (Application.isBatchMode)
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
            if (Application.isBatchMode)
                EditorApplication.Exit(success ? 0 : 1);
        }

        private static void CompleteRouteCapture(bool success, string message)
        {
            if (routeCaptureCompleted)
                return;

            routeCaptureCompleted = true;
            EditorApplication.update -= ContinueRouteCapture;
            RestoreCommanderCapturePlayModeSettings();
            if (success)
                Debug.Log($"[CanvasRouteCaptureValidation] result=Passed {message}");
            else
                Debug.LogError($"[CanvasRouteCaptureValidation] result=Failed {message}");

            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            if (Application.isBatchMode)
                EditorApplication.Exit(success ? 0 : 1);
        }

        private static void RestoreCommanderCapturePlayModeSettings()
        {
            if (!SessionState.GetBool(CommanderCapturePendingSessionKey, false))
                return;

            bool previousFastPlayMode = SessionState.GetBool(CommanderCapturePreviousFastPlayModeSessionKey, false);
            int previousOptions = SessionState.GetInt(
                CommanderCapturePreviousPlayModeOptionsSessionKey,
                (int)EnterPlayModeOptions.None);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)previousOptions;
            EditorSettings.enterPlayModeOptionsEnabled = previousFastPlayMode;
            SessionState.SetBool(CommanderCapturePendingSessionKey, false);
        }

        private static void ApplyCommanderResponsiveCaptureLayout()
        {
            if (routeCaptureRoute != UIRoute.CommandFeed || screenshotWidth <= 0 || screenshotHeight <= 0)
                return;

            const float referenceWidth = 4800f;
            const float referenceHeight = 2160f;
            float scale = Mathf.Min(screenshotWidth / referenceWidth, screenshotHeight / referenceHeight);
            if (scale <= 0f)
                return;

            float logicalCanvasHeight = screenshotHeight / scale;
            CommanderProfileResponsiveLayoutView[] layouts = UnityEngine.Object.FindObjectsByType<CommanderProfileResponsiveLayoutView>(FindObjectsInactive.Include);
            for (int i = 0; i < layouts.Length; i++)
                layouts[i]?.ApplyLayout(logicalCanvasHeight);

            Canvas.ForceUpdateCanvases();
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
            if (Application.isBatchMode)
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
                    ComponentType.ReadOnly<UiShellRootComponent>(),
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
                ComponentType.ReadOnly<SceneLifecycleRootComponent>(),
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

        private static void EstimateImageMetrics(Texture2D texture, out float luma, out float detail)
        {
            luma = 0f;
            detail = 0f;
            Color32[] pixels = texture.GetPixels32();
            if (pixels == null || pixels.Length == 0)
                return;

            int step = Mathf.Max(1, pixels.Length / 4096);
            double total = 0d;
            float minLuma = 1f;
            float maxLuma = 0f;
            byte minR = byte.MaxValue;
            byte minG = byte.MaxValue;
            byte minB = byte.MaxValue;
            byte maxR = byte.MinValue;
            byte maxG = byte.MinValue;
            byte maxB = byte.MinValue;
            int count = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                float pixelLuma = (float)((0.2126d * pixel.r + 0.7152d * pixel.g + 0.0722d * pixel.b) / 255d);
                total += pixelLuma;
                minLuma = Mathf.Min(minLuma, pixelLuma);
                maxLuma = Mathf.Max(maxLuma, pixelLuma);
                minR = Math.Min(minR, pixel.r);
                minG = Math.Min(minG, pixel.g);
                minB = Math.Min(minB, pixel.b);
                maxR = Math.Max(maxR, pixel.r);
                maxG = Math.Max(maxG, pixel.g);
                maxB = Math.Max(maxB, pixel.b);
                count++;
            }

            if (count <= 0)
                return;

            luma = (float)(total / count);
            float lumaRange = maxLuma - minLuma;
            float colorRange =
                ((maxR - minR) + (maxG - minG) + (maxB - minB)) /
                (255f * 3f);
            detail = Mathf.Max(lumaRange, colorRange);
        }

        private static bool TryRenderCameraMetrics(Camera camera, string screenshotPath, int width, int height, out float luma, out float detail, out string error)
        {
            luma = 0f;
            detail = 0f;
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
                EstimateImageMetrics(texture, out luma, out detail);
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

        private static void RemoveDirectChild(Transform root, string childName)
        {
            Transform child = FindDirectChild(root, childName);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void MoveTo(GameObject target, Transform parent)
        {
            if (target == null || parent == null)
                return;

            target.transform.SetParent(parent, false);
            target.SetActive(true);
        }

        private static void ApplyMainMenuCommanderRouteWiring()
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(MainMenuContentPrefabPath);
                GameObject hotspot = FindChild(root.transform, "CommanderPanelHotspot")?.gameObject;
                if (hotspot == null)
                    throw new InvalidOperationException("SCN-02 main menu prefab is missing CommanderPanelHotspot.");

                Stretch(hotspot.transform as RectTransform);
                EnsureButtonInteraction(hotspot);
                ConfigureRouteButton(hotspot, UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, true);

                GameObject portraitButton = FindChild(root.transform, "CommanderPortraitButton")?.gameObject;
                if (portraitButton != null)
                    ConfigureRouteButton(portraitButton, UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, true);

                PrefabUtility.SaveAsPrefabAsset(root, MainMenuContentPrefabPath);
                Debug.Log($"[CanvasCommanderProfileTargetLockLayout] commanderRoute=Passed prefab={MainMenuContentPrefabPath}");
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Button EnsureButtonInteraction(GameObject target)
        {
            if (target == null)
                return null;

            Image image = GetOrAddImage(target);
            if (image != null)
                image.raycastTarget = true;

            Button button = target.GetComponent<Button>();
            if (button == null)
                button = target.AddComponent<Button>();

            if (button.targetGraphic == null)
                button.targetGraphic = image;

            button.interactable = true;
            return button;
        }

        private static UIShellRouteButtonView ConfigureRouteButton(
            GameObject target,
            UiShellRouteIntent intent,
            UIRoute route,
            bool pushHistory)
        {
            if (target == null)
                return null;

            EnsureButtonInteraction(target);
            UIShellRouteButtonView routeButton = target.GetComponent<UIShellRouteButtonView>();
            if (routeButton == null)
                routeButton = target.AddComponent<UIShellRouteButtonView>();

            routeButton.Configure(intent, route, pushHistory);
            return routeButton;
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

            ApplySlicedSprite(image, normalPath, 1f);
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
            ApplySlicedSprite(image, normalPath, 1f);
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

        private static void SetButtonAvailability(GameObject buttonObject, bool available, float visualStrength)
        {
            if (buttonObject == null)
                return;

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
                button.interactable = available;

            Image image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                float strength = Mathf.Clamp01(visualStrength);
                image.color = new Color(strength, strength, strength, 1f);
            }

            TMP_Text label = FindChild(buttonObject.transform, "Label")?.GetComponent<TMP_Text>();
            if (label != null)
                label.color = available || visualStrength >= 0.99f
                    ? new Color(0.95f, 0.88f, 0.66f, 1f)
                    : new Color(0.58f, 0.56f, 0.49f, 1f);
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

            child.SetActive(true);
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
            if (child != null)
                child.SetActive(true);
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

            child.SetActive(true);
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

        private static GameObject ConfigureSolidImageChildAndReturn(
            GameObject parent,
            string name,
            float left,
            float top,
            float width,
            float height,
            Color color)
        {
            ConfigureSolidImageChild(parent, name, left, top, width, height, color);
            return parent != null ? parent.transform.Find(name)?.gameObject : null;
        }

        private static GameObject ConfigureSpriteChild(
            GameObject parent,
            string name,
            string spritePath,
            float left,
            float top,
            float width,
            float height,
            bool preserveAspect = true)
        {
            if (parent == null)
                return null;

            Transform existing = parent.transform.Find(name);
            GameObject child = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            if (existing == null)
                child.transform.SetParent(parent.transform, false);

            SetRectFromTopLeft(child, left, top, width, height);
            Image image = GetOrAddImage(child);
            Sprite sprite = LoadSprite(spritePath);
            if (image == null || sprite == null)
                return child;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.raycastTarget = false;
            return child;
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
            SetRectFromTopLeft(portraitPanel, 40f, 38f, 560f, 642f);
            ApplyPanelFrame(portraitPanel, "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png", 1f);

            GameObject portrait = FindChild(panel.transform, "Portrait")?.gameObject;
            if (portraitPanel != null)
                MoveTo(portrait, portraitPanel.transform);
            SetRectFromTopLeft(portrait, 34f, 28f, 492f, 586f);
            Image portraitImage = GetOrAddImage(portrait);
            Sprite portraitSprite = LoadSprite("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_commander_portrait.png");
            if (portraitImage != null && portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = true;
                portraitImage.color = Color.white;
            }

            GameObject identityCard = FindChild(panel.transform, "IdentityCard")?.gameObject;
            SetRectFromTopLeft(identityCard, 620f, 42f, 970f, 630f);
            DisableImage(identityCard);
            ConfigureSolidImageChild(identityCard, "IdentityTextBacking", 0f, 0f, 970f, 630f, new Color(0.02f, 0.025f, 0.022f, 0.76f));
            SetActive(FindChild(identityCard != null ? identityCard.transform : null, "Badge")?.gameObject, false);
            ConfigureSpriteChild(
                identityCard,
                "RankEmblem",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_02_commander_rank_shield.png",
                26f,
                54f,
                166f,
                166f);
            ConfigureTextChild(identityCard, "RoleLabel", "FIELD COMMANDER", 200f, 34f, 740f, 60f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.83f, 0.22f));
            ConfigureTextChild(identityCard, "CommanderNameLabel", "COL. ALEX MORGAN", 200f, 98f, 740f, 112f, 76f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.9f, 0.76f));
            ConfigureTextChild(identityCard, "MottoLabel", "VICTORY IS PLANNED", 200f, 224f, 740f, 60f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.72f, 0.83f, 0.26f));
            ConfigureSolidImageChild(identityCard, "MottoRule", 200f, 294f, 710f, 3f, new Color(0.57f, 0.44f, 0.13f, 0.9f));
            ConfigureSpriteChild(
                identityCard,
                "LevelMedallion",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png",
                20f,
                344f,
                176f,
                176f);
            ConfigureTextChild(identityCard, "CommanderLevelLabel", "38", 28f, 354f, 160f, 112f, 88f, TextAlignmentOptions.Center, new Color(0.92f, 0.74f, 0.22f));
            SetActive(FindChild(identityCard != null ? identityCard.transform : null, "LevelCaption")?.gameObject, false);
            ConfigureTextChild(identityCard, "ProgressCaption", "COMMAND XP", 220f, 350f, 300f, 40f, 27f, TextAlignmentOptions.MidlineLeft, new Color(0.68f, 0.66f, 0.55f));
            ConfigureTextChild(identityCard, "LevelProgressLabel", "15,680 / 24,000 XP", 220f, 378f, 690f, 54f, 34f, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.84f, 0.65f));
            ConfigureSolidImageChild(identityCard, "LevelProgressFill", 220f, 458f, 448f, 40f, new Color(0.55f, 0.66f, 0.08f, 1f));
            ConfigureSolidImageChild(identityCard, "LevelProgressTrack", 220f, 458f, 690f, 40f, new Color(0.08f, 0.08f, 0.065f, 0.9f));
            ApplyPanelFrame(
                FindChild(identityCard != null ? identityCard.transform : null, "LevelProgressTrack")?.gameObject,
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_12_small_chip_frame.png",
                0.82f);
            ConfigureTextChild(identityCard, "ServiceRecordLabel", "ACTIVE COMMAND  /  246 OPERATIONS", 220f, 530f, 690f, 54f, 31f, TextAlignmentOptions.MidlineLeft, new Color(0.72f, 0.69f, 0.55f));
            FindChild(identityCard != null ? identityCard.transform : null, "CommanderLevelLabel")?.SetAsLastSibling();

            GameObject editButton = FindChild(panel.transform, "EditIdButton")?.gameObject;
            if (portraitPanel != null)
                MoveTo(editButton, portraitPanel.transform);
            SetRectFromTopLeft(editButton, 464f, 28f, 72f, 72f);
            ApplyButtonFrame(editButton, FrameKind.Secondary);
            SetChildSprite(editButton, "Icon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_09_edit_pencil.png", true);
            GameObject editIcon = FindChild(editButton != null ? editButton.transform : null, "Icon")?.gameObject;
            SetRectFromTopLeft(editIcon, 19f, 19f, 34f, 34f);
            HideDirectChildrenExcept(editButton != null ? editButton.transform : null, "Icon");
            HideDirectChildrenExcept(portraitPanel != null ? portraitPanel.transform : null, "Portrait", "EditIdButton");

            GameObject badgesButton = FindChild(panel.transform, "BadgesButton")?.gameObject;
            SetActive(badgesButton, false);
            HideDirectChildrenExcept(identityCard != null ? identityCard.transform : null,
                "IdentityTextBacking", "RankEmblem", "RoleLabel", "CommanderNameLabel", "MottoLabel", "MottoRule",
                "LevelMedallion", "ProgressCaption",
                "CommanderLevelLabel", "LevelProgressLabel", "LevelProgressTrack",
                "LevelProgressFill", "ServiceRecordLabel");
            HideDirectChildrenExcept(panel.transform, "PortraitPanel", "IdentityCard");
        }

        private static void LayoutOverviewStats(GameObject panel)
        {
            if (panel == null)
                return;

            SetTextValue(panel.transform, "Title", "SERVICE RECORD");
            SetTextBlock(panel.transform, "Title", 44f, 18f, 560f, 60f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
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
                SetRectFromTopLeft(card, 38f + (i * 398f), 88f, 370f, 242f);
                ApplyPanelFrame(card, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png", 0.74f);
                SetActive(FindChild(card != null ? card.transform : null, "Icon")?.gameObject, false);
                ConfigureSpriteChild(card, "StatIcon", iconPaths[i], 28f, 72f, 94f, 94f);
                SetTextValue(card != null ? card.transform : null, "Label", labels[i]);
                SetTextValue(card != null ? card.transform : null, "Value", values[i]);
                SetTextValue(card != null ? card.transform : null, "Suffix", suffixes[i]);
                SetTextBlock(card != null ? card.transform : null, "Label", 142f, 30f, 200f, 46f, 32f, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.88f, 0.68f));
                SetTextBlock(card != null ? card.transform : null, "Value", 140f, 76f, 202f, 78f, 66f, TextAlignmentOptions.MidlineLeft, new Color(0.92f, 0.74f, 0.22f));
                SetTextBlock(card != null ? card.transform : null, "Suffix", 142f, 166f, 200f, 44f, 27f, TextAlignmentOptions.MidlineLeft, new Color(0.64f, 0.78f, 0.32f));
                HideDirectChildrenExcept(card != null ? card.transform : null, "StatIcon", "Label", "Value", "Suffix");
            }

            HideDirectChildrenExcept(panel.transform, "Title", "VictoriesStatCard", "MissionsStatCard", "CiviliansStatCard", "LostStatCard");
        }

        private static void LayoutAccountSnapshot(GameObject panel)
        {
            if (panel == null)
                return;

            SetTextBlock(panel.transform, "Title", 44f, 18f, 660f, 60f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
            ConfigureTextChild(panel, "ModeHeader", "MODE", 164f, 82f, 300f, 42f, 29f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.61f, 0.52f));
            ConfigureTextChild(panel, "RatingHeader", "RATING", 570f, 82f, 220f, 42f, 29f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.61f, 0.52f));
            ConfigureTextChild(panel, "RankHeader", "RANK", 838f, 82f, 220f, 42f, 29f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.61f, 0.52f));
            ConfigureTextChild(panel, "WinRateHeader", "WIN RATE", 1098f, 82f, 220f, 42f, 29f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.61f, 0.52f));
            ConfigureTextChild(panel, "LastPlayedHeader", "LAST PLAYED", 1330f, 82f, 250f, 42f, 29f, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.61f, 0.52f));

            string[] names = { "CampaignSnapshot", "OperationsSnapshot", "SkirmishSnapshot" };
            string[] labels = { "CAMPAIGN", "OPERATIONS", "SKIRMISH" };
            string[] ratings = { "1,750", "1,620", "1,480" };
            string[] ranks = { "#2,134", "#3,987", "#6,412" };
            string[] winRates = { "76%", "64%", "58%" };
            string[] lastPlayed = { "1h ago", "3h ago", "5h ago" };
            string[] iconPaths =
            {
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_02_commander_rank_shield.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png"
            };

            for (int i = 0; i < names.Length; i++)
            {
                GameObject row = FindChild(panel.transform, names[i])?.gameObject;
                SetRectFromTopLeft(row, 38f, 130f + (i * 172f), 1570f, 144f);
                ApplyPanelFrame(row, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png", 1f);
                ConfigureSpriteChild(row, "Icon", iconPaths[i], 28f, 28f, 88f, 88f);
                ConfigureTextChild(row, "Mode", labels[i], 140f, 0f, 340f, 144f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.94f, 0.88f, 0.68f));
                ConfigureTextChild(row, "Rating", ratings[i], 530f, 0f, 220f, 144f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.63f, 0.79f, 0.14f));
                ConfigureTextChild(row, "Rank", ranks[i], 800f, 0f, 220f, 144f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.63f, 0.79f, 0.14f));
                ConfigureTextChild(row, "WinRate", winRates[i], 1060f, 0f, 220f, 144f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.63f, 0.79f, 0.14f));
                ConfigureTextChild(row, "LastPlayed", lastPlayed[i], 1298f, 0f, 230f, 144f, 38f, TextAlignmentOptions.MidlineLeft, new Color(0.73f, 0.7f, 0.59f));
                HideDirectChildrenExcept(row != null ? row.transform : null, "Icon", "Mode", "Rating", "Rank", "WinRate", "LastPlayed");
            }

            SetActive(FindChild(panel.transform, "ReadinessSnapshot")?.gameObject, false);
            HideDirectChildrenExcept(panel.transform, "Title", "ModeHeader", "RatingHeader", "RankHeader", "WinRateHeader", "LastPlayedHeader", "CampaignSnapshot", "OperationsSnapshot", "SkirmishSnapshot");
        }

        private static void LayoutRewardTrack(GameObject panel)
        {
            if (panel == null)
                return;

            SetTextBlock(panel.transform, "Title", 50f, 24f, 800f, 64f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
            ConfigureSolidImageChild(panel, "LevelBadge", 50f, 104f, 196f, 196f, new Color(0.035f, 0.04f, 0.035f, 0.95f));
            GameObject levelBadge = panel.transform.Find("LevelBadge")?.gameObject;
            ApplyPanelFrame(levelBadge, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_square_panel_frame.png", 0.8f);
            ConfigureTextChild(levelBadge, "LevelValue", "38", 0f, 14f, 196f, 112f, 80f, TextAlignmentOptions.Center, new Color(0.92f, 0.74f, 0.22f));
            ConfigureTextChild(levelBadge, "LevelCaption", "LEVEL", 0f, 124f, 196f, 46f, 28f, TextAlignmentOptions.Center, new Color(0.9f, 0.84f, 0.65f));
            HideDirectChildrenExcept(levelBadge != null ? levelBadge.transform : null, "LevelValue", "LevelCaption");

            GameObject progress = FindChild(panel.transform, "XpProgress")?.gameObject;
            SetActive(progress, false);
            GameObject rewardXpBar = ConfigureSolidImageChildAndReturn(
                panel,
                "RewardXpBar",
                270f,
                116f,
                770f,
                126f,
                new Color(0.02f, 0.024f, 0.02f, 0.92f));
            ApplyPanelFrame(
                rewardXpBar,
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png",
                0.72f);
            ConfigureTextChild(rewardXpBar, "Label", "15,680 / 24,000 XP", 26f, 14f, 710f, 44f, 32f, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.84f, 0.64f));
            ConfigureSolidImageChild(rewardXpBar, "Fill", 42f, 78f, 468f, 18f, new Color(0.58f, 0.68f, 0.08f, 1f));
            ConfigureSolidImageChild(rewardXpBar, "Track", 24f, 68f, 720f, 38f, new Color(0.055f, 0.06f, 0.045f, 0.96f));
            HideDirectChildrenExcept(rewardXpBar != null ? rewardXpBar.transform : null, "Label", "Track", "Fill");

            GameObject nextReward = ConfigureSolidImageChildAndReturn(panel, "NextReward", 1070f, 92f, 260f, 220f, new Color(0.03f, 0.035f, 0.03f, 0.92f));
            ApplyPanelFrame(nextReward, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png", 0.65f);
            ConfigureTextChild(nextReward, "Caption", "NEXT REWARD", 24f, 26f, 212f, 42f, 26f, TextAlignmentOptions.Center, new Color(0.82f, 0.77f, 0.62f));
            ConfigureSpriteChild(nextReward, "Icon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_04_supplies_crate.png", 66f, 72f, 128f, 128f);
            HideDirectChildrenExcept(nextReward != null ? nextReward.transform : null, "Caption", "Icon");

            ConfigureSolidImageChild(panel, "MilestoneTrack", 136f, 360f, 1110f, 6f, new Color(0.55f, 0.42f, 0.12f, 0.9f));

            SetActive(FindChild(panel.transform, "Node35")?.gameObject, false);
            string[] nodeNames = { "Node36", "Node37", "Node38", "Node39", "Node40" };
            string[] nodeValues = { "36", "37", "38", "39", "40" };
            string[] rewardIcons =
            {
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_15_reward_wreath.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_04_supplies_crate.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_05_command_shield.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_13_building.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png"
            };
            for (int i = 0; i < nodeNames.Length; i++)
            {
                GameObject node = FindChild(panel.transform, nodeNames[i])?.gameObject;
                SetRectFromTopLeft(node, 112f + (i * 252f), 302f, 120f, 120f);
                ApplySlicedSprite(GetOrAddImage(node), i == 2
                    ? "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_18_reward_node_active.png"
                    : i < 2
                        ? "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_17_reward_node_claimed.png"
                        : "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_19_reward_node_locked.png", 1f);
                SetTextValue(node != null ? node.transform : null, "Level", nodeValues[i]);
                SetTextBlock(node != null ? node.transform : null, "Level", 0f, 0f, 120f, 120f, 34f, TextAlignmentOptions.Center, new Color(0.92f, 0.82f, 0.45f));
                HideDirectChildrenExcept(node != null ? node.transform : null, "Level");

                GameObject rewardCard = ConfigureSolidImageChildAndReturn(panel, $"RewardCard{nodeValues[i]}", 72f + (i * 252f), 430f, 200f, 190f, new Color(0.025f, 0.03f, 0.025f, 0.94f));
                ApplyPanelFrame(rewardCard, "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_card_frame.png", i == 2 ? 0.76f : 0.56f);
                ConfigureSpriteChild(rewardCard, "Icon", rewardIcons[i], 52f, 30f, 96f, 96f);
                ConfigureTextChild(rewardCard, "State", i < 2 ? "CLAIMED" : i == 2 ? "ACTIVE" : "LOCKED", 22f, 126f, 156f, 36f, 27f, TextAlignmentOptions.Center, i <= 2 ? new Color(0.65f, 0.8f, 0.16f) : new Color(0.58f, 0.56f, 0.49f));
                HideDirectChildrenExcept(rewardCard != null ? rewardCard.transform : null, "Icon", "State");
            }

            SetActive(FindChild(panel.transform, "RewardText")?.gameObject, false);
            GameObject claimButton = FindChild(panel.transform, "ClaimButton")?.gameObject;
            SetRectFromTopLeft(claimButton, 1060f, 626f, 280f, 78f);
            ApplyButtonFrame(claimButton, FrameKind.Primary);
            SetButtonLabel(claimButton, "CLAIM");
            LayoutIconLabelButton(claimButton, 212f, 17f, 44f, 44f, 32f, 0f, 176f, 78f, 31f);
            SetChildSprite(claimButton, "Icon", "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_20_claim_chevron.png", true);
            HideDirectChildrenExcept(claimButton != null ? claimButton.transform : null, "Icon", "Label");
            HideDirectChildrenExcept(panel.transform, "Title", "LevelBadge", "RewardXpBar", "NextReward", "MilestoneTrack", "Node36", "Node37", "Node38", "Node39", "Node40", "RewardCard36", "RewardCard37", "RewardCard38", "RewardCard39", "RewardCard40", "ClaimButton");
        }

        private static void LayoutRecentHistory(GameObject panel)
        {
            if (panel == null)
                return;

            SetTextBlock(panel.transform, "Title", 50f, 22f, 700f, 68f, 44f, TextAlignmentOptions.MidlineLeft, new Color(0.96f, 0.88f, 0.62f));
            ConfigureSolidImageChild(panel, "ViewAllBacking", 1060f, 22f, 270f, 70f, new Color(0.03f, 0.035f, 0.03f, 0.92f));
            ConfigureTextChild(panel, "ViewAllLabel", "VIEW ALL", 1080f, 22f, 230f, 70f, 30f, TextAlignmentOptions.Center, new Color(0.9f, 0.82f, 0.58f));
            string[] rows = { "FirstContactRow", "OldMarketRow", "ConvoyEscortRow", "BridgeStrikeRow", "FrontlineClashRow" };
            string[] titles = { "HOSTILE PATROL", "SUPPLY RUN", "CONVOY ESCORT", "BRIDGE STRIKE", "FRONTLINE CLASH" };
            string[] modes = { "CAMPAIGN", "OPERATIONS", "OPERATIONS", "SKIRMISH", "SKIRMISH" };
            string[] results = { "VICTORY", "VICTORY", "DEFEAT", "VICTORY", "VICTORY" };
            string[] times = { "1h ago", "3h ago", "5h ago", "7h ago", "9h ago" };
            string[] iconPaths =
            {
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_15_reward_wreath.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_10_badge_shield.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png",
                "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_16_history_crossed_swords.png"
            };
            for (int i = 0; i < rows.Length; i++)
            {
                GameObject row = FindChild(panel.transform, rows[i])?.gameObject;
                if (row == null)
                    row = ConfigureSolidImageChildAndReturn(panel, rows[i], 42f, 108f + (i * 172f), 1296f, 152f, new Color(0.025f, 0.03f, 0.025f, 0.94f));

                SetRectFromTopLeft(row, 42f, 108f + (i * 172f), 1296f, 152f);
                ApplyPanelFrame(row, "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_chrome_20_route_strip_frame.png", 1f);
                ConfigureSpriteChild(row, "Icon", iconPaths[i], 26f, 32f, 88f, 88f);
                ConfigureTextChild(row, "Title", titles[i], 138f, 16f, 560f, 60f, 42f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.89f, 0.68f));
                ConfigureTextChild(row, "Subtitle", modes[i], 138f, 78f, 420f, 44f, 32f, TextAlignmentOptions.MidlineLeft, new Color(0.76f, 0.72f, 0.58f));
                ConfigureTextChild(row, "Result", results[i], 790f, 0f, 280f, 152f, 38f, TextAlignmentOptions.MidlineRight, i == 2 ? new Color(0.9f, 0.25f, 0.12f) : new Color(0.63f, 0.79f, 0.14f));
                ConfigureTextChild(row, "Time", times[i], 1100f, 0f, 150f, 152f, 32f, TextAlignmentOptions.MidlineRight, new Color(0.7f, 0.68f, 0.59f));
                HideDirectChildrenExcept(row != null ? row.transform : null, "Icon", "Title", "Subtitle", "Result", "Time");
            }

            HideDirectChildrenExcept(panel.transform, "Title", "ViewAllBacking", "ViewAllLabel", "FirstContactRow", "OldMarketRow", "ConvoyEscortRow", "BridgeStrikeRow", "FrontlineClashRow");
        }

        private static void PrepareFooterButton(
            GameObject button,
            Transform footer,
            float x,
            string labelText,
            FrameKind frameKind)
        {
            MoveTo(button, footer);
            EnsureButtonInteraction(button);
            float width = frameKind == FrameKind.Primary ? 820f : 680f;
            SetRectFromTopLeft(button, x, -150f, width, 170f);
            ApplyButtonFrame(button, frameKind);
            SetButtonLabel(button, labelText);
            LayoutIconLabelButton(button, 58f, 39f, 92f, 92f, 176f, 0f, width - 224f, 170f, 56f);
            HideDirectChildrenExcept(button != null ? button.transform : null, "Icon", "Label");
        }

        private static void ConfigureFooterActionIcon(GameObject button, string spritePath)
        {
            if (button == null)
                return;

            SetActive(FindChild(button.transform, "Icon")?.gameObject, false);
            ConfigureSpriteChild(button, "ActionIcon", spritePath, 58f, 39f, 92f, 92f);
            HideDirectChildrenExcept(button.transform, "ActionIcon", "Label");
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
            GameObject footerRail,
            GameObject openArmoryButton,
            GameObject detailButton,
            GameObject replayButton)
        {
            if (footer == null)
                return;

            for (int i = 0; i < footer.childCount; i++)
            {
                GameObject child = footer.GetChild(i).gameObject;
                if (child != footerRail && child != openArmoryButton && child != detailButton && child != replayButton)
                    child.SetActive(false);
            }
        }

        private static void RemoveDuplicateDirectChildren(Transform parent, string childName, GameObject keep)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child != keep && string.Equals(child.name, childName, StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child);
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

        private static void ConfigureCommanderProfileContentSections(GameObject root)
        {
            if (root == null)
                return;

            UIShellContentSectionsView sectionsView = root.GetComponent<UIShellContentSectionsView>();
            if (sectionsView == null)
                sectionsView = root.AddComponent<UIShellContentSectionsView>();

            var sections = new List<UIShellContentSectionsView.SectionReference>(4);
            AddContentSection(sections, root.transform, UIShellContentSectionId.Left, "LeftContent");
            AddContentSection(sections, root.transform, UIShellContentSectionId.Middle, "MiddleContent");
            AddContentSection(sections, root.transform, UIShellContentSectionId.Right, "RightContent");
            AddContentSection(sections, root.transform, UIShellContentSectionId.Footer, "FooterContent");
            sectionsView.ConfigureSections(sections.ToArray());
        }

        private static void ConfigureCommanderProfileBinding(GameObject middle, GameObject identityPanel)
        {
            if (middle == null || identityPanel == null)
                return;

            CommanderProfileContentView view = middle.GetComponent<CommanderProfileContentView>();
            if (view == null)
                view = middle.AddComponent<CommanderProfileContentView>();

            TMP_Text nameLabel = FindChild(identityPanel.transform, "CommanderNameLabel")?.GetComponent<TMP_Text>();
            TMP_Text subtitleLabel = FindChild(identityPanel.transform, "MottoLabel")?.GetComponent<TMP_Text>();
            view.Configure(nameLabel, subtitleLabel);
        }

        private static void ConfigureCommanderProfileResponsiveLayout(
            GameObject middle,
            GameObject right,
            GameObject footer,
            GameObject identityPanel,
            GameObject overviewPanel,
            GameObject accountPanel,
            GameObject rewardPanel,
            GameObject historyPanel,
            GameObject footerRail,
            GameObject openArmoryButton,
            GameObject detailButton,
            GameObject replayButton)
        {
            if (middle != null)
            {
                CommanderProfileResponsiveLayoutView middleLayout = middle.GetComponent<CommanderProfileResponsiveLayoutView>();
                if (middleLayout == null)
                    middleLayout = middle.AddComponent<CommanderProfileResponsiveLayoutView>();

                middleLayout.Configure(
                    CommanderProfileResponsiveSection.Middle,
                    new[]
                    {
                        identityPanel != null ? identityPanel.transform as RectTransform : null,
                        overviewPanel != null ? overviewPanel.transform as RectTransform : null,
                        accountPanel != null ? accountPanel.transform as RectTransform : null,
                        FindChild(accountPanel != null ? accountPanel.transform : null, "CampaignSnapshot") as RectTransform,
                        FindChild(accountPanel != null ? accountPanel.transform : null, "OperationsSnapshot") as RectTransform,
                        FindChild(accountPanel != null ? accountPanel.transform : null, "SkirmishSnapshot") as RectTransform
                    },
                    new[] { 0f, 750f, 1140f, 130f, 258f, 386f },
                    new[] { -240f, 510f, 900f, 130f, 302f, 474f },
                    new[] { 720f, 360f, 540f, 112f, 112f, 112f },
                    new[] { 720f, 360f, 700f, 144f, 144f, 144f });
            }

            if (right != null)
            {
                CommanderProfileResponsiveLayoutView rightLayout = right.GetComponent<CommanderProfileResponsiveLayoutView>();
                if (rightLayout == null)
                    rightLayout = right.AddComponent<CommanderProfileResponsiveLayoutView>();

                rightLayout.Configure(
                    CommanderProfileResponsiveSection.Right,
                    new[]
                    {
                        rewardPanel != null ? rewardPanel.transform as RectTransform : null,
                        historyPanel != null ? historyPanel.transform as RectTransform : null,
                        FindChild(historyPanel != null ? historyPanel.transform : null, "FirstContactRow") as RectTransform,
                        FindChild(historyPanel != null ? historyPanel.transform : null, "OldMarketRow") as RectTransform,
                        FindChild(historyPanel != null ? historyPanel.transform : null, "ConvoyEscortRow") as RectTransform,
                        FindChild(historyPanel != null ? historyPanel.transform : null, "BridgeStrikeRow") as RectTransform,
                        FindChild(historyPanel != null ? historyPanel.transform : null, "FrontlineClashRow") as RectTransform
                    },
                    new[] { 20f, 770f, 108f, 250f, 392f, 534f, 676f },
                    new[] { 20f, 810f, 108f, 280f, 452f, 624f, 796f },
                    new[] { 720f, 900f, 132f, 132f, 132f, 132f, 132f },
                    new[] { 760f, 1020f, 152f, 152f, 152f, 152f, 152f });
            }

            if (footer != null)
            {
                CommanderProfileResponsiveLayoutView footerLayout = footer.GetComponent<CommanderProfileResponsiveLayoutView>();
                if (footerLayout == null)
                    footerLayout = footer.AddComponent<CommanderProfileResponsiveLayoutView>();

                footerLayout.Configure(
                    CommanderProfileResponsiveSection.Footer,
                    new[]
                    {
                        footerRail != null ? footerRail.transform as RectTransform : null,
                        openArmoryButton != null ? openArmoryButton.transform as RectTransform : null,
                        detailButton != null ? detailButton.transform as RectTransform : null,
                        replayButton != null ? replayButton.transform as RectTransform : null
                    },
                    new[] { 30f, 100f, 100f, 100f },
                    new[] { -220f, -150f, -150f, -150f });
            }
        }

        private static void AddContentSection(
            List<UIShellContentSectionsView.SectionReference> sections,
            Transform root,
            UIShellContentSectionId sectionId,
            string childName)
        {
            Transform child = FindDirectChild(root, childName);
            if (child != null)
                sections.Add(new UIShellContentSectionsView.SectionReference(sectionId, child.gameObject));
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
                    return child;
            }

            return null;
        }

        private sealed class RouteCaptureBuildingUiCommand : IBuildingUiCommand
        {
            public int CurrentDollars => 12500;
            public bool HasPendingBuildingPlacement => true;
            public bool CanConfirmBuildingPlacement => true;
            public string PlacementStatusText => "Barracks: Valid placement";
            public int ActivePlacementCost => 650;
            public int ActivePlacementCreditsCost => 40000;
            public float ActivePlacementDurationSeconds => 45f;
            public int MaxQueuedUnitProductions => 25;

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
}
