using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed partial class MenuBootstrapCompositionSystemHelper
    {
        private const int DeferredMatchLoadVisibleFrames = 2;
        private const float MinimumLoadingVisibleSeconds = 2f;
        private const float MatchReadyHoldSeconds = 0.75f;
        private const string AutoStartMatchEnvironmentVariable = "WARLINE_AUTO_START_MATCH";
        private const string AutoStartMatchCommandLineArg = "-warlineAutoStartMatch";

        private static World startupRuntimeSettingsWorld;

        private readonly SceneLifecycleSceneSystemHelper sceneLifecycleSceneSystemHelper = new();
        private readonly MatchStartSceneSystemHelper matchStartSystem = new();
        private readonly PerformanceDiagnosticsSystemHelper performanceDiagnosticsSystem = new();
        private readonly MatchSceneReferenceSceneSystemHelper matchSceneReferenceSystem = new();
        private readonly QuickCustomGameConfigStore quickCustomGameConfigStore = new();
        private readonly MatchLaunchCommand matchLaunchCommand = new();
        private readonly IGameTextResolver gameTextResolver = new GameTextResolverAdapter();
        private readonly StaticMapPresentationStreamer staticMapPresentationStreamer;
        private readonly FirstLaunchNarrativeCompositionSystemHelper firstLaunchNarrative = new();

        private EntityQuery boundaryQuery;
        private Entity cachedBoundaryEntity;
        private World cachedWorld;
        private bool hasBoundaryQuery;
        private EntityQuery sceneLifecycleQuery;
        private World sceneLifecycleQueryWorld;
        private bool hasSceneLifecycleQuery;
        private EntityQuery matchStartBoundaryQuery;
        private World matchStartBoundaryQueryWorld;
        private bool hasMatchStartBoundaryQuery;
        private EntityQuery matchStartProgressQuery;
        private World matchStartProgressQueryWorld;
        private bool hasMatchStartProgressQuery;
        private bool diagnosticsInitialized;
        private bool initialized;
        private bool hasCapturedUiPresentation;
        private CameraClearFlags defaultUiCameraClearFlags;
        private Color defaultUiCameraBackgroundColor;
        private bool defaultUiCameraEnabled;
        private bool defaultUiAudioListenerEnabled;
        private RenderMode defaultUiCanvasRenderMode;
        private Camera defaultUiCanvasWorldCamera;
        private int deferredMatchLoadFrame = -1;
        private int activeLoadingSequenceId = -1;
        private float activeLoadingStartedAt;
        private UIRoute activeLoadingRoute;
        private int activeMatchReadySequenceId = -1;
        private float activeMatchReadyStartedAt;
        private bool matchLoadQueuedForCurrentRoute;
        private MatchSceneView boundMatchRuntimeView;
        private MatchSceneView streamedMatchView;
        private SelectionUiCommandUiSystemHelper boundSelectionUiCommand;
        private SelectionUiReadModelUiSystemHelper boundSelectionUiReadModel;
        private MainMenuPlayUI boundMainMenu;
        private int boundContentVersion = -1;
        private bool autoStartMatchRequested;
        private bool autoStartMatchSubmitted;

        public PerformanceDiagnosticsSystemHelper PerformanceDiagnostics => performanceDiagnosticsSystem;
        public bool IsPerformanceDiagnosticsInitialized => diagnosticsInitialized;

        public MenuBootstrapCompositionSystemHelper() : this(new StaticMapPresentationStreamer())
        {
        }

        internal MenuBootstrapCompositionSystemHelper(StaticMapPresentationStreamer presentationStreamer)
        {
            staticMapPresentationStreamer = presentationStreamer;
        }

        public void Initialize(MenuBootstrapView view)
        {
            if (view == null)
                return;

            bool wasInitialized = initialized;
            EnsurePersistentDiagnosticsInitialized();
            autoStartMatchRequested = ShouldAutoStartMatch();
            view.ApplyRuntimeUiMode();

            if (view.ShellEcsPresentation != null)
                view.ShellEcsPresentation.Configure(view.ShellView);
            if (view.ContentSystem != null)
            {
                view.ContentSystem.BindGameTextResolver(gameTextResolver);
                view.ContentSystem.ConfigureCatalogMetadataResolvers(
                    UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
                    UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
                view.ContentSystem.BindQuickCustomRuntimeDependencies(quickCustomGameConfigStore, matchLaunchCommand);
            }
            if (view.Router != null)
                view.Router.Initialize();

            if (!wasInitialized)
            {
                ResetShellForFreshMenuScene();
                bool reviewerMode = FirstLaunchNarrativeReviewUtilitySystemHelper.ConsumeRequest();
                firstLaunchNarrative.InitializeShell(
                    view,
                    gameTextResolver,
                    autoStartMatchRequested && !reviewerMode,
                    reviewerMode);
            }

            initialized = true;
            TryApplyStartupRuntimeSettings();
        }

        public void Update(MenuBootstrapView view, float unscaledDeltaTime)
        {
            if (!initialized)
                Initialize(view);
            if (view == null)
                return;
            firstLaunchNarrative.Tick(unscaledDeltaTime);
            TryApplyStartupRuntimeSettings();
            view.ApplyRuntimeUiMode();

            if (!TryGetWorldEntityManager(out EntityManager entityManager))
                return;

            sceneLifecycleSceneSystemHelper.Update(entityManager);

            if (!TryGetBoundary(entityManager, out Entity boundary))
                return;

            firstLaunchNarrative.ApplyShellState(entityManager, boundary);

            UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            UpdateStaticMapPresentation(shellState);
            if (CanAdvanceMatchStart(shellState))
                matchStartSystem.Update(entityManager);
            if (shellState.CurrentMode == UiShellMode.MatchHud)
                MarkMatchHudReady();
            QueueAutoStartMatchIfRequested(entityManager, boundary, shellState);
            ApplyUiPresentationMode(view.UiCamera, view.UiCanvas, shellState, entityManager);
            QueueDeferredMatchLoadAfterLoadingFeedback(entityManager, shellState);
            UpdateActualLoadingProgress(entityManager, boundary, shellState);
            BindMatchRuntimeUi(view, shellState);
        }

        public void Shutdown(MenuBootstrapView view)
        {
            if (view != null && view.UiCanvas != null && view.UiCanvas.transform.localScale != Vector3.one)
                view.UiCanvas.transform.localScale = Vector3.one;

            if (view != null)
                RestoreUiPresentationMode(view.UiCamera, view.UiCanvas, isMatchSceneLoaded: false);

            initialized = false;
            hasCapturedUiPresentation = false;
            deferredMatchLoadFrame = -1;
            ResetLoadingMinimumWindow();
            ResetMatchReadyHoldWindow();
            matchLoadQueuedForCurrentRoute = false;
            staticMapPresentationStreamer.Unbind();
            streamedMatchView = null;
            if (view != null && view.ContentSystem != null)
                view.ContentSystem.BindGameplayRuntimeDependencies(null);
            ClearBoundMatchRuntimeUi();
            autoStartMatchSubmitted = false;
            firstLaunchNarrative.Shutdown();
            if (!diagnosticsInitialized)
                return;

            performanceDiagnosticsSystem.Dispose();
            diagnosticsInitialized = false;
        }

        private void EnsurePersistentDiagnosticsInitialized()
        {
            if (diagnosticsInitialized)
                return;

            Application.runInBackground = true;
            performanceDiagnosticsSystem.Initialize();
            diagnosticsInitialized = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStartupRuntimeSettingsApplication()
        {
            startupRuntimeSettingsWorld = null;
        }

        internal static void ResetStartupRuntimeSettingsApplicationForTests()
        {
            ResetStartupRuntimeSettingsApplication();
        }

        private static bool TryApplyStartupRuntimeSettings()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;
            if (ReferenceEquals(startupRuntimeSettingsWorld, world))
                return true;
            if (world.Unmanaged.GetExistingUnmanagedSystem<UiAudioSettingsProjectionSystem>() == SystemHandle.Null ||
                world.Unmanaged.GetExistingUnmanagedSystem<AssistantSettingsPersistenceSystem>() == SystemHandle.Null)
            {
                return false;
            }

            ApplyStartupRuntimeSettings();
            startupRuntimeSettingsWorld = world;
            return true;
        }

        private void QueueAutoStartMatchIfRequested(EntityManager entityManager, Entity boundary, UiShellStateComponent shellState)
        {
            if (!autoStartMatchRequested || autoStartMatchSubmitted)
                return;

            if (shellState.ActiveRoute == UIRoute.Match)
            {
                autoStartMatchSubmitted = true;
                return;
            }

            if (shellState.CurrentMode == UiShellMode.None || shellState.IsTransitionRunning != 0)
                return;

            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            routeRequests.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            autoStartMatchSubmitted = true;
            Debug.Log("[UiShellRoute] submitted validation Match auto-start request.");
        }

        private static bool ShouldAutoStartMatch()
        {
            try
            {
                if (IsTruthy(Environment.GetEnvironmentVariable(AutoStartMatchEnvironmentVariable)))
                    return true;

                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], AutoStartMatchCommandLineArg, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // Diagnostic auto-start must never make normal menu startup fail.
            }

            return false;
        }

        private static bool IsTruthy(string value)
        {
            return
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateActualLoadingProgress(EntityManager entityManager, Entity boundary, UiShellStateComponent shellState)
        {
            if (shellState.CurrentMode != UiShellMode.Loading)
            {
                ResetLoadingMinimumWindow();
                return;
            }

            TrackLoadingMinimumWindow(shellState);

            if (shellState.IsTransitionRunning != 0)
                return;

            UiShellLoadingProgressComponent loading = entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
            if (loading.IsComplete != 0)
                return;

            if (shellState.ActiveRoute == UIRoute.Match)
            {
                UpdateMatchLoadingProgress(entityManager, boundary);
                return;
            }

            UpdateMenuLoadingProgress(entityManager, boundary);
        }

        private void UpdateMatchLoadingProgress(EntityManager entityManager, Entity boundary)
        {
            if (!matchLoadQueuedForCurrentRoute)
            {
                SetLoading(entityManager, boundary, 0f, false, "Preparing match load");
                return;
            }

            if (!TryGetSceneLifecycleState(entityManager, out SceneLifecycleStateComponent sceneState))
            {
                SetLoading(entityManager, boundary, 0f, false, "Loading match");
                return;
            }

            if (sceneState.IsMatchLoaded == 0)
            {
                SetLoading(entityManager, boundary, Mathf.Min(sceneState.Progress01, 0.9f), false, "Loading match");
                return;
            }

            if (!IsStaticMapPresentationPreloadReady())
            {
                float presentationProgress = 0.90f + (Mathf.Clamp01(staticMapPresentationStreamer.Progress01) * 0.04f);
                string presentationStatus = staticMapPresentationStreamer.Failed
                    ? staticMapPresentationStreamer.Status
                    : "Loading map presentation";
                SetLoading(entityManager, boundary, presentationProgress, false, presentationStatus);
                return;
            }

            if (!IsMatchStartComplete(entityManager))
            {
                if (TryGetMatchStartProgress(entityManager, out MatchStartProgressComponent progress))
                {
                    float startupProgress = 0.94f + (Mathf.Clamp01(progress.Progress01) * 0.05f);
                    string status = progress.Status.Length == 0 ? "Starting match" : progress.Status.ToString();
                    SetLoading(entityManager, boundary, startupProgress, false, status);
                    return;
                }

                SetLoading(entityManager, boundary, 0.96f, false, "Starting match");
                return;
            }

            TrackMatchReadyHoldWindow();
            bool readyToExitLoading = IsMinimumLoadingWindowElapsed() && IsMatchReadyHoldWindowElapsed();
            SetLoading(entityManager, boundary, 1f, readyToExitLoading, "Match ready");
        }

        private void UpdateMenuLoadingProgress(EntityManager entityManager, Entity boundary)
        {
            if (TryGetSceneLifecycleState(entityManager, out SceneLifecycleStateComponent sceneState) &&
                (sceneState.IsBusy != 0 || sceneState.IsMatchLoaded != 0))
            {
                if (sceneState.IsBusy == 0 && sceneState.IsMatchLoaded != 0)
                {
                    if (!staticMapPresentationStreamer.DrainComplete)
                    {
                        SetLoading(
                            entityManager,
                            boundary,
                            Mathf.Clamp01(staticMapPresentationStreamer.Progress01),
                            false,
                            staticMapPresentationStreamer.Failed
                                ? staticMapPresentationStreamer.Status
                                : "Unloading map presentation");
                        return;
                    }

                    if (streamedMatchView != null &&
                        !streamedMatchView.OperationMapContentUnloadComplete)
                    {
                        if (!string.IsNullOrEmpty(streamedMatchView.OperationMapContentFailure))
                        {
                            SetLoading(
                                entityManager,
                                boundary,
                                0f,
                                false,
                                $"Map unload failed: {streamedMatchView.OperationMapContentFailure}");
                            return;
                        }

                        if (!streamedMatchView.OperationMapContentUnloading &&
                            !streamedMatchView.TryBeginOperationMapContentUnload(out string unloadError))
                        {
                            SetLoading(
                                entityManager,
                                boundary,
                                0f,
                                false,
                                $"Map unload failed: {unloadError}");
                            return;
                        }

                        SetLoading(
                            entityManager,
                            boundary,
                            Mathf.Clamp01(streamedMatchView.OperationMapContentProgress01),
                            false,
                            "Unloading operation map");
                        return;
                    }

                    sceneLifecycleSceneSystemHelper.QueueUnloadMatch(entityManager);
                }

                float progress = sceneState.Status == SceneLifecycleStatusKind.Unloading ? sceneState.Progress01 : 0f;
                SetLoading(entityManager, boundary, progress, false, "Unloading match");
                return;
            }

            SetLoading(entityManager, boundary, 1f, IsMinimumLoadingWindowElapsed(), "Command shell ready");
        }

        private void UpdateStaticMapPresentation(UiShellStateComponent shellState)
        {
            if (!matchSceneReferenceSystem.TryGetLoadedMatchSceneView(out MatchSceneView matchScene))
            {
                if (streamedMatchView != null)
                {
                    staticMapPresentationStreamer.Unbind();
                    streamedMatchView = null;
                }
                staticMapPresentationStreamer.Update();
                return;
            }

            UpdateStaticMapPresentationForLoadedMatch(
                shellState.ActiveRoute == UIRoute.Match,
                matchScene);
        }

        internal void UpdateStaticMapPresentationForLoadedMatch(
            bool isMatchRoute,
            MatchSceneView matchScene)
        {
            if (isMatchRoute && !matchScene.OperationMapContentReady)
            {
                staticMapPresentationStreamer.Update();
                return;
            }

            if (isMatchRoute && streamedMatchView != matchScene)
            {
                if (!staticMapPresentationStreamer.Bind(
                        matchScene.StaticMapPresentationManifest,
                        matchScene.WorldCamera) &&
                    staticMapPresentationStreamer.HasDetachedOperation)
                    return;
                streamedMatchView = matchScene;
            }

            if (!isMatchRoute)
            {
                staticMapPresentationStreamer.BeginDrain();
            }
            else if (staticMapPresentationStreamer.IsDraining)
            {
                staticMapPresentationStreamer.Update();
                if (staticMapPresentationStreamer.DrainComplete)
                {
                    staticMapPresentationStreamer.Bind(
                        matchScene.StaticMapPresentationManifest,
                        matchScene.WorldCamera);
                }
                return;
            }

            staticMapPresentationStreamer.Update();
            if (matchScene.OperationMapReadinessPublicationAvailable &&
                !matchScene.TryPublishOperationMapReadiness(
                    staticMapPresentationStreamer.PreloadComplete,
                    staticMapPresentationStreamer.Failed,
                    out string readinessError))
            {
                Debug.LogError($"[OperationMapReadiness] {readinessError}");
            }
        }

        private bool CanAdvanceMatchStart(UiShellStateComponent shellState)
        {
            return shellState.ActiveRoute == UIRoute.Match && IsStaticMapPresentationPreloadReady();
        }

        private bool IsStaticMapPresentationPreloadReady()
        {
            return streamedMatchView != null &&
                !staticMapPresentationStreamer.Failed &&
                !staticMapPresentationStreamer.IsDraining &&
                staticMapPresentationStreamer.PreloadComplete;
        }

        private void TrackLoadingMinimumWindow(UiShellStateComponent shellState)
        {
            if (activeLoadingSequenceId == shellState.TransitionSequenceId &&
                activeLoadingRoute == shellState.ActiveRoute)
            {
                return;
            }

            activeLoadingSequenceId = shellState.TransitionSequenceId;
            activeLoadingRoute = shellState.ActiveRoute;
            activeLoadingStartedAt = Time.unscaledTime;
        }

        private bool IsMinimumLoadingWindowElapsed()
        {
            return activeLoadingSequenceId >= 0 &&
                Time.unscaledTime - activeLoadingStartedAt >= MinimumLoadingVisibleSeconds;
        }

        private void ResetLoadingMinimumWindow()
        {
            activeLoadingSequenceId = -1;
            activeLoadingStartedAt = 0f;
            activeLoadingRoute = UIRoute.Splash;
            ResetMatchReadyHoldWindow();
        }

        private void TrackMatchReadyHoldWindow()
        {
            if (activeMatchReadySequenceId == activeLoadingSequenceId)
                return;

            activeMatchReadySequenceId = activeLoadingSequenceId;
            activeMatchReadyStartedAt = Time.unscaledTime;
        }

        private bool IsMatchReadyHoldWindowElapsed()
        {
            return activeMatchReadySequenceId == activeLoadingSequenceId &&
                Time.unscaledTime - activeMatchReadyStartedAt >= MatchReadyHoldSeconds;
        }

        private void ResetMatchReadyHoldWindow()
        {
            activeMatchReadySequenceId = -1;
            activeMatchReadyStartedAt = 0f;
        }

        private void QueueDeferredMatchLoadAfterLoadingFeedback(EntityManager entityManager, UiShellStateComponent shellState)
        {
            if (shellState.ActiveRoute != UIRoute.Match)
            {
                deferredMatchLoadFrame = -1;
                matchLoadQueuedForCurrentRoute = false;
                return;
            }

            if (matchLoadQueuedForCurrentRoute)
                return;

            if (shellState.CurrentMode != UiShellMode.Loading || shellState.IsTransitionRunning != 0)
            {
                deferredMatchLoadFrame = -1;
                return;
            }

            if (deferredMatchLoadFrame < 0)
            {
                deferredMatchLoadFrame = Time.frameCount;
                return;
            }

            if (Time.frameCount - deferredMatchLoadFrame < DeferredMatchLoadVisibleFrames)
                return;

            if (!sceneLifecycleSceneSystemHelper.QueueLoadMatch(entityManager))
            {
                Debug.LogError("[UiShellRoute] failed to submit deferred Match scene load request.");
                return;
            }

            matchLoadQueuedForCurrentRoute = true;
            deferredMatchLoadFrame = -1;
            Debug.Log("[UiShellRoute] submitted deferred Match scene load request after loading feedback.");

            if (matchStartSystem.QueueStartAfterMatchLoaded(entityManager))
                Debug.Log("[UiShellRoute] submitted deferred Match gameplay start request.");
            else
                Debug.LogError("[UiShellRoute] failed to submit deferred Match gameplay start request.");
        }

        private void BindMatchRuntimeUi(MenuBootstrapView view, UiShellStateComponent shellState)
        {
            if (shellState.ActiveRoute != UIRoute.Match)
            {
                ClearBoundMatchRuntimeUi();
                return;
            }

            if (view == null || view.ContentSystem == null)
                return;

            if (!matchSceneReferenceSystem.TryGetLoadedMatchSceneView(out MatchSceneView matchScene))
            {
                return;
            }

            MatchBootstrapCompositionSystemHelper matchBootstrap = matchScene.MatchBootstrap;
            SelectionUiCommandUiSystemHelper selectionUiCommand = matchBootstrap.SelectionUiCommand;
            if (selectionUiCommand == null)
                return;
            SelectionUiReadModelUiSystemHelper selectionUiReadModel = matchBootstrap.SelectionUiReadModel;

            int contentVersion = view.ContentSystem.ContentVersion;
            MainMenuPlayUI currentMainMenu = matchBootstrap.MainMenu;
            if (boundMatchRuntimeView == matchScene &&
                boundSelectionUiCommand == selectionUiCommand &&
                boundSelectionUiReadModel == selectionUiReadModel &&
                boundMainMenu == currentMainMenu &&
                boundContentVersion == contentVersion &&
                matchBootstrap.AreMainMenuRuntimeDependenciesCurrent())
            {
                return;
            }

            MainMenuPlayUI mainMenu = matchBootstrap.EnsureMainMenuRuntimeDependencies();
            if (view.ContentSystem.TryGetMatchHudSelectionPanelView(out MatchHudSelectionPanelView selectionPanelView))
                matchBootstrap.BindMatchHudSelectionPanel(selectionPanelView);

            view.ContentSystem.BindGameplayRuntimeDependencies(
                selectionUiCommand,
                mainMenu,
                matchBootstrap.BindMatchHudSelectionPanel,
                matchBootstrap.BuildingUiCommandContract,
                matchBootstrap.SelectionDiagnosticsSink,
                selectionUiReadModel);
            view.ContentSystem.BindBuildDrawerRuntimeQueries(matchBootstrap.BuildingUiQueryContract);
            view.ContentSystem.BindQuickCustomRuntimeDependencies(quickCustomGameConfigStore, matchLaunchCommand);
            boundMatchRuntimeView = matchScene;
            boundSelectionUiCommand = selectionUiCommand;
            boundSelectionUiReadModel = selectionUiReadModel;
            boundMainMenu = mainMenu;
            boundContentVersion = contentVersion;
        }

        private void ClearBoundMatchRuntimeUi()
        {
            boundMatchRuntimeView = null;
            boundSelectionUiCommand = null;
            boundSelectionUiReadModel = null;
            boundMainMenu = null;
            boundContentVersion = -1;
        }

        private void ApplyUiPresentationMode(Camera uiCamera, Canvas uiCanvas, UiShellStateComponent shellState, EntityManager entityManager)
        {
            CaptureUiPresentationMode(uiCamera, uiCanvas);

            if (shellState.ActiveRoute == UIRoute.Match)
            {
                if (uiCanvas != null)
                {
                    if (uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    if (uiCanvas.worldCamera != null)
                        uiCanvas.worldCamera = null;
                }

                if (uiCamera != null)
                {
                    bool isMatchSceneLoaded = IsMatchSceneLoaded(entityManager);
                    ApplyUiAudioListenerMatchMode(uiCamera, isMatchSceneLoaded);
                    if (isMatchSceneLoaded)
                    {
                        if (uiCamera.clearFlags != CameraClearFlags.Depth)
                            uiCamera.clearFlags = CameraClearFlags.Depth;
                        if (uiCamera.enabled)
                            uiCamera.enabled = false;
                    }
                    else
                    {
                        if (uiCamera.clearFlags != CameraClearFlags.SolidColor)
                            uiCamera.clearFlags = CameraClearFlags.SolidColor;
                        if (!uiCamera.enabled)
                            uiCamera.enabled = true;
                    }
                }

                return;
            }

            RestoreUiPresentationMode(uiCamera, uiCanvas, IsMatchSceneLoaded(entityManager));
        }

        private void CaptureUiPresentationMode(Camera uiCamera, Canvas uiCanvas)
        {
            if (hasCapturedUiPresentation)
                return;

            if (uiCamera != null)
            {
                defaultUiCameraClearFlags = uiCamera.clearFlags;
                defaultUiCameraBackgroundColor = uiCamera.backgroundColor;
                defaultUiCameraEnabled = uiCamera.enabled;
                AudioListener audioListener = uiCamera.GetComponent<AudioListener>();
                defaultUiAudioListenerEnabled = audioListener != null && audioListener.enabled;
            }

            if (uiCanvas != null)
            {
                defaultUiCanvasRenderMode = uiCanvas.renderMode;
                defaultUiCanvasWorldCamera = uiCanvas.worldCamera;
            }

            hasCapturedUiPresentation = true;
        }

        private void RestoreUiPresentationMode(Camera uiCamera, Canvas uiCanvas, bool isMatchSceneLoaded)
        {
            if (!hasCapturedUiPresentation)
                return;

            if (uiCamera != null)
            {
                RestoreUiAudioListener(uiCamera, isMatchSceneLoaded);
                if (uiCamera.enabled != defaultUiCameraEnabled)
                    uiCamera.enabled = defaultUiCameraEnabled;
                if (uiCamera.clearFlags != defaultUiCameraClearFlags)
                    uiCamera.clearFlags = defaultUiCameraClearFlags;
                if (uiCamera.backgroundColor != defaultUiCameraBackgroundColor)
                    uiCamera.backgroundColor = defaultUiCameraBackgroundColor;
            }

            if (uiCanvas != null)
            {
                if (uiCanvas.renderMode != defaultUiCanvasRenderMode)
                    uiCanvas.renderMode = defaultUiCanvasRenderMode;
                Camera targetWorldCamera = defaultUiCanvasWorldCamera != null ? defaultUiCanvasWorldCamera : uiCamera;
                if (uiCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCanvas.worldCamera != targetWorldCamera)
                    uiCanvas.worldCamera = targetWorldCamera;
            }
        }

        private static void ApplyUiAudioListenerMatchMode(Camera uiCamera, bool isMatchSceneLoaded)
        {
            AudioListener audioListener = uiCamera != null ? uiCamera.GetComponent<AudioListener>() : null;
            if (audioListener == null || !isMatchSceneLoaded)
                return;

            audioListener.enabled = false;
        }

        private void RestoreUiAudioListener(Camera uiCamera, bool isMatchSceneLoaded)
        {
            ApplyUiAudioListenerMenuMode(uiCamera, isMatchSceneLoaded, defaultUiAudioListenerEnabled);
        }

        private static void ApplyUiAudioListenerMenuMode(
            Camera uiCamera,
            bool isMatchSceneLoaded,
            bool defaultListenerEnabled)
        {
            AudioListener audioListener = uiCamera != null ? uiCamera.GetComponent<AudioListener>() : null;
            if (audioListener == null || isMatchSceneLoaded)
                return;

            audioListener.enabled = defaultListenerEnabled;
        }

        private bool IsMatchStartComplete(EntityManager entityManager)
        {
            EntityQuery query = GetMatchStartBoundaryQuery(entityManager);
            if (query.IsEmptyIgnoreFilter)
                return false;

            Entity entity = query.GetSingletonEntity();
            if (!entityManager.HasComponent<MatchStartQueueComponent>(entity))
                return false;

            MatchStartQueueComponent queue = entityManager.GetComponentData<MatchStartQueueComponent>(entity);
            return queue.HasStarted != 0 && queue.IsStartPending == 0;
        }

        private bool TryGetMatchStartProgress(EntityManager entityManager, out MatchStartProgressComponent progress)
        {
            progress = default;
            EntityQuery query = GetMatchStartProgressQuery(entityManager);
            if (query.IsEmptyIgnoreFilter)
                return false;

            Entity entity = query.GetSingletonEntity();
            progress = entityManager.GetComponentData<MatchStartProgressComponent>(entity);
            return true;
        }

        private bool IsMatchSceneLoaded(EntityManager entityManager)
        {
            return TryGetSceneLifecycleState(entityManager, out SceneLifecycleStateComponent state) && state.IsMatchLoaded != 0;
        }

        private bool TryGetSceneLifecycleState(EntityManager entityManager, out SceneLifecycleStateComponent state)
        {
            state = default;
            EntityQuery query = GetSceneLifecycleQuery(entityManager);
            if (query.IsEmptyIgnoreFilter)
                return false;

            Entity entity = query.GetSingletonEntity();
            if (!entityManager.HasComponent<SceneLifecycleStateComponent>(entity))
                return false;

            state = entityManager.GetComponentData<SceneLifecycleStateComponent>(entity);
            return true;
        }

        private EntityQuery GetSceneLifecycleQuery(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (sceneLifecycleQueryWorld != world || !hasSceneLifecycleQuery)
            {
                sceneLifecycleQueryWorld = world;
                sceneLifecycleQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneLifecycleRootComponent>());
                hasSceneLifecycleQuery = true;
            }

            return sceneLifecycleQuery;
        }

        private EntityQuery GetMatchStartBoundaryQuery(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (matchStartBoundaryQueryWorld != world || !hasMatchStartBoundaryQuery)
            {
                matchStartBoundaryQueryWorld = world;
                matchStartBoundaryQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MatchStartStateComponent>());
                hasMatchStartBoundaryQuery = true;
            }

            return matchStartBoundaryQuery;
        }

        private EntityQuery GetMatchStartProgressQuery(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (matchStartProgressQueryWorld != world || !hasMatchStartProgressQuery)
            {
                matchStartProgressQueryWorld = world;
                matchStartProgressQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<MatchStartStateComponent>(),
                    ComponentType.ReadOnly<MatchStartProgressComponent>());
                hasMatchStartProgressQuery = true;
            }

            return matchStartProgressQuery;
        }

        private static void ResetShellForFreshMenuScene()
        {
            if (!TryGetWorldEntityManager(out EntityManager entityManager))
                return;

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
                ComponentType.ReadWrite<MatchIntroTransitionComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>(),
                ComponentType.ReadWrite<UiShellPopupRequestComponent>(),
                ComponentType.ReadWrite<UiShellPresentationCommandComponent>(),
                ComponentType.ReadWrite<UiShellTransitionCompleteComponent>());
            if (query.IsEmptyIgnoreFilter)
                return;

            Entity boundary = query.GetSingletonEntity();
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Clear();
            entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary).Clear();
            entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary).Clear();
            entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary).Clear();
            entityManager.SetComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.None,
                ActiveRoute = UIRoute.MainMenu,
                Phase = UiShellTransitionPhase.Idle,
                TransitionSequenceId = 0,
                IsTransitionRunning = 0
            });
            FirstLaunchNarrativeCompositionSystemHelper.ResetShellState(entityManager, boundary);
            entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
            {
                Progress01 = 0f,
                Status = new FixedString64Bytes("Starting"),
                IsComplete = 0
            });
            entityManager.SetComponentData(boundary, new MatchIntroTransitionComponent
            {
                State = MatchIntroTransitionStateKind.Inactive,
                Progress01 = 0f,
                InputLocked = 0,
                SequenceId = 0,
                Status = new FixedString64Bytes("Inactive")
            });

            if (entityManager.HasComponent<UiShellCommanderProfileComponent>(boundary))
            {
                entityManager.SetComponentData(boundary, new UiShellCommanderProfileComponent
                {
                    Name = new FixedString64Bytes("COL. ALEX MORGAN"),
                    Subtitle = new FixedString64Bytes("VICTORY IS PLANNED"),
                    PortraitClass = new FixedString64Bytes("commander-portrait-default")
                });
            }

            if (entityManager.HasComponent<UiShellMainMenuResourcesComponent>(boundary))
            {
                entityManager.SetComponentData(boundary, new UiShellMainMenuResourcesComponent
                {
                    CreditsText = new FixedString32Bytes("12,450"),
                    CommandText = new FixedString32Bytes("78/100")
                });
            }
        }

        private static bool TryGetWorldEntityManager(out EntityManager entityManager)
        {
            entityManager = default;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            return true;
        }

        private bool TryGetBoundary(EntityManager entityManager, out Entity boundary)
        {
            boundary = Entity.Null;

            World world = entityManager.World;
            if (cachedWorld != world || !hasBoundaryQuery)
            {
                cachedWorld = world;
                cachedBoundaryEntity = Entity.Null;
                boundaryQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
                hasBoundaryQuery = true;
            }

            if (cachedBoundaryEntity != Entity.Null &&
                entityManager.Exists(cachedBoundaryEntity) &&
                entityManager.HasComponent<UiShellRootComponent>(cachedBoundaryEntity))
            {
                boundary = cachedBoundaryEntity;
                return true;
            }

            if (boundaryQuery.IsEmptyIgnoreFilter)
                return false;
            boundary = boundaryQuery.GetSingletonEntity();
            cachedBoundaryEntity = boundary;
            return true;
        }
    }
}
