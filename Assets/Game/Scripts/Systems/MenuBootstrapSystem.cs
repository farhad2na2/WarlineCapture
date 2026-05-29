using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuBootstrapSystem
{
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();
    private readonly PerformanceDiagnosticsSystem performanceDiagnosticsSystem = new();
    private readonly PerformanceDiagnosticsReferenceSystem performanceDiagnosticsReferenceSystem = new();

    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;
    private bool diagnosticsInitialized;
    private bool initialized;
    private float loadingElapsedSeconds;
    private bool hasCapturedUiPresentation;
    private CameraClearFlags defaultUiCameraClearFlags;
    private Color defaultUiCameraBackgroundColor;
    private bool defaultUiCameraEnabled;
    private RenderMode defaultUiCanvasRenderMode;
    private Camera defaultUiCanvasWorldCamera;

    public PerformanceDiagnosticsSystem PerformanceDiagnostics => performanceDiagnosticsSystem;

    public void Initialize(MenuBootstrapView view)
    {
        if (view == null)
            return;

        bool wasInitialized = initialized;
        EnsurePersistentDiagnosticsInitialized();

        if (view.ShellEcsBridge != null)
            view.ShellEcsBridge.Configure(view.ShellView);
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
        if (!initialized)
            Initialize(view);
        if (view == null || !TryGetWorldEntityManager(out EntityManager entityManager))
            return;

        sceneLifecycleSystem.Update(entityManager);
        matchStartSystem.Update(entityManager);

        if (!TryGetBoundary(entityManager, out Entity boundary))
            return;

        UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        UiShellLoadingProgressComponent loading = entityManager.GetComponentData<UiShellLoadingProgressComponent>(boundary);
        ApplyUiPresentationMode(view.UiCamera, view.UiCanvas, shellState);
        if (shellState.CurrentMode != UiShellMode.Loading ||
            shellState.IsTransitionRunning != 0 ||
            loading.IsComplete != 0)
        {
            loadingElapsedSeconds = 0f;
            return;
        }

        loadingElapsedSeconds += Mathf.Max(0f, unscaledDeltaTime);
        float duration = Mathf.Max(0.01f, view.StartupLoadingDurationSeconds);
        float progress = Mathf.Clamp01(loadingElapsedSeconds / duration);
        if (shellState.ActiveRoute == WarlineCaptureRoute.Match && !IsMatchStartComplete(entityManager))
        {
            SetLoading(
                entityManager,
                boundary,
                Mathf.Min(progress, 0.95f),
                false,
                "Loading match");
            return;
        }

        SetLoading(entityManager, boundary, progress, progress >= 1f);
    }

    public void Shutdown(MenuBootstrapView view)
    {
        if (view != null)
            RestoreUiPresentationMode(view.UiCamera, view.UiCanvas);

        initialized = false;
        loadingElapsedSeconds = 0f;
        hasCapturedUiPresentation = false;
        performanceDiagnosticsReferenceSystem.Clear(performanceDiagnosticsSystem);
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

    private void ApplyUiPresentationMode(Camera uiCamera, Canvas uiCanvas, UiShellStateComponent shellState)
    {
        CaptureUiPresentationMode(uiCamera, uiCanvas);

        if (shellState.ActiveRoute == WarlineCaptureRoute.Match)
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
                if (uiCamera.clearFlags != CameraClearFlags.Depth)
                    uiCamera.clearFlags = CameraClearFlags.Depth;
                if (uiCamera.enabled)
                    uiCamera.enabled = false;
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
            ActiveRoute = WarlineCaptureRoute.Splash,
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
