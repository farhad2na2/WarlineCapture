using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuBootstrapSystem
{
    private const int DeferredMatchLoadVisibleFrames = 2;
    private const float MinimumLoadingVisibleSeconds = 2f;

    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();
    private readonly PerformanceDiagnosticsSystem performanceDiagnosticsSystem = new();
    private readonly PerformanceDiagnosticsReferenceSystem performanceDiagnosticsReferenceSystem = new();

    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;
    private bool diagnosticsInitialized;
    private bool initialized;
    private bool hasCapturedUiPresentation;
    private CameraClearFlags defaultUiCameraClearFlags;
    private Color defaultUiCameraBackgroundColor;
    private bool defaultUiCameraEnabled;
    private RenderMode defaultUiCanvasRenderMode;
    private Camera defaultUiCanvasWorldCamera;
    private int deferredMatchLoadFrame = -1;
    private int activeLoadingSequenceId = -1;
    private float activeLoadingStartedAt;
    private UIRoute activeLoadingRoute;
    private bool matchLoadQueuedForCurrentRoute;

    public PerformanceDiagnosticsSystem PerformanceDiagnostics => performanceDiagnosticsSystem;

    public void Initialize(MenuBootstrapView view)
    {
        if (view == null)
            return;

        bool wasInitialized = initialized;
        EnsurePersistentDiagnosticsInitialized();

        if (view.ShellEcsPresentation != null)
            view.ShellEcsPresentation.Configure(view.ShellView);
        if (view.Router != null)
            view.Router.Initialize();

        if (!wasInitialized)
            ResetShellForFreshMenuScene();

        initialized = true;
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        if (diagnosticsInitialized)
            performanceDiagnosticsSystem.OnApplicationFocus(hasFocus);
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        if (diagnosticsInitialized)
            performanceDiagnosticsSystem.OnApplicationPause(pauseStatus);
    }

    public void Update(MenuBootstrapView view, float unscaledDeltaTime)
    {
        _ = unscaledDeltaTime;
        if (!initialized)
            Initialize(view);
        if (view == null || !TryGetWorldEntityManager(out EntityManager entityManager))
            return;

        sceneLifecycleSystem.Update(entityManager);
        matchStartSystem.Update(entityManager);

        if (!TryGetBoundary(entityManager, out Entity boundary))
            return;

        UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        ApplyUiPresentationMode(view.UiCamera, view.UiCanvas, shellState, entityManager);
        QueueDeferredMatchLoadAfterLoadingFeedback(entityManager, shellState);
        UpdateActualLoadingProgress(entityManager, boundary, shellState);
    }

    public void Shutdown(MenuBootstrapView view)
    {
        if (view != null)
            RestoreUiPresentationMode(view.UiCamera, view.UiCanvas);

        initialized = false;
        hasCapturedUiPresentation = false;
        performanceDiagnosticsReferenceSystem.Clear(performanceDiagnosticsSystem);
        deferredMatchLoadFrame = -1;
        ResetLoadingMinimumWindow();
        matchLoadQueuedForCurrentRoute = false;
        if (!diagnosticsInitialized)
            return;

        performanceDiagnosticsSystem.Dispose();
        diagnosticsInitialized = false;
    }

    private void EnsurePersistentDiagnosticsInitialized()
    {
        if (diagnosticsInitialized)
        {
            performanceDiagnosticsReferenceSystem.Register(performanceDiagnosticsSystem);
            return;
        }

        Application.runInBackground = true;
        performanceDiagnosticsSystem.Initialize();
        diagnosticsInitialized = true;
        performanceDiagnosticsReferenceSystem.Register(performanceDiagnosticsSystem);
    }

    private static void SetLoading(EntityManager entityManager, Entity boundary, float progress01, bool complete)
    {
        SetLoading(
            entityManager,
            boundary,
            progress01,
            complete,
            complete ? "Command shell ready" : "Loading command shell");
    }

    private static void SetLoading(EntityManager entityManager, Entity boundary, float progress01, bool complete, string status)
    {
        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = new FixedString64Bytes(status),
            IsComplete = complete ? (byte)1 : (byte)0
        });
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

        if (!IsMatchStartComplete(entityManager))
        {
            SetLoading(entityManager, boundary, 0.95f, false, "Starting match");
            return;
        }

        SetLoading(entityManager, boundary, 1f, IsMinimumLoadingWindowElapsed(), "Match ready");
    }

    private void UpdateMenuLoadingProgress(EntityManager entityManager, Entity boundary)
    {
        if (TryGetSceneLifecycleState(entityManager, out SceneLifecycleStateComponent sceneState) &&
            (sceneState.IsBusy != 0 || sceneState.IsMatchLoaded != 0))
        {
            if (sceneState.IsBusy == 0 && sceneState.IsMatchLoaded != 0)
                sceneLifecycleSystem.QueueUnloadMatch(entityManager);

            float progress = sceneState.Status == SceneLifecycleStatusKind.Unloading ? sceneState.Progress01 : 0f;
            SetLoading(entityManager, boundary, progress, false, "Unloading match");
            return;
        }

        SetLoading(entityManager, boundary, 1f, IsMinimumLoadingWindowElapsed(), "Command shell ready");
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

        if (!sceneLifecycleSystem.QueueLoadMatch(entityManager))
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
                if (IsMatchSceneLoaded(entityManager))
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

        RestoreUiPresentationMode(uiCamera, uiCanvas);
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
        }

        if (uiCanvas != null)
        {
            defaultUiCanvasRenderMode = uiCanvas.renderMode;
            defaultUiCanvasWorldCamera = uiCanvas.worldCamera;
        }

        hasCapturedUiPresentation = true;
    }

    private void RestoreUiPresentationMode(Camera uiCamera, Canvas uiCanvas)
    {
        if (!hasCapturedUiPresentation)
            return;

        if (uiCamera != null)
        {
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

    private static bool IsMatchStartComplete(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MatchStartBoundaryComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        if (!entityManager.HasComponent<MatchStartQueueComponent>(entity))
            return false;

        MatchStartQueueComponent queue = entityManager.GetComponentData<MatchStartQueueComponent>(entity);
        return queue.HasStarted != 0 && queue.IsStartPending == 0;
    }

    private static bool IsMatchSceneLoaded(EntityManager entityManager)
    {
        return TryGetSceneLifecycleState(entityManager, out SceneLifecycleStateComponent state) && state.IsMatchLoaded != 0;
    }

    private static bool TryGetSceneLifecycleState(EntityManager entityManager, out SceneLifecycleStateComponent state)
    {
        state = default;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneLifecycleBoundaryComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        if (!entityManager.HasComponent<SceneLifecycleStateComponent>(entity))
            return false;

        state = entityManager.GetComponentData<SceneLifecycleStateComponent>(entity);
        return true;
    }

    private static void ResetShellForFreshMenuScene()
    {
        if (!TryGetWorldEntityManager(out EntityManager entityManager))
            return;

        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
            ComponentType.ReadWrite<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
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
            ActiveRoute = UIRoute.Splash,
            Phase = UiShellTransitionPhase.Idle,
            TransitionSequenceId = 0,
            IsTransitionRunning = 0
        });
        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = 0f,
            Status = new FixedString64Bytes("Starting"),
            IsComplete = 0
        });
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
            boundaryQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
    }
}
