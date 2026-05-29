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
    private bool hasCapturedUiCameraClearMode;
    private CameraClearFlags defaultUiCameraClearFlags;
    private Color defaultUiCameraBackgroundColor;

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
        ApplyUiCameraClearMode(view.UiCamera, shellState);
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

    public void Shutdown()
    {
        initialized = false;
        loadingElapsedSeconds = 0f;
        hasCapturedUiCameraClearMode = false;
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

    private void ApplyUiCameraClearMode(Camera uiCamera, UiShellStateComponent shellState)
    {
        if (uiCamera == null)
            return;

        if (!hasCapturedUiCameraClearMode)
        {
            defaultUiCameraClearFlags = uiCamera.clearFlags;
            defaultUiCameraBackgroundColor = uiCamera.backgroundColor;
            hasCapturedUiCameraClearMode = true;
        }

        bool overlaysMatchWorld =
            shellState.ActiveRoute == WarlineCaptureRoute.Match &&
            (shellState.CurrentMode == UiShellMode.Loading ||
             shellState.CurrentMode == UiShellMode.MatchHud ||
             shellState.CurrentMode == UiShellMode.PopupOnly);

        CameraClearFlags targetClearFlags = overlaysMatchWorld
            ? CameraClearFlags.Depth
            : defaultUiCameraClearFlags;
        if (uiCamera.clearFlags != targetClearFlags)
            uiCamera.clearFlags = targetClearFlags;

        if (!overlaysMatchWorld && uiCamera.backgroundColor != defaultUiCameraBackgroundColor)
            uiCamera.backgroundColor = defaultUiCameraBackgroundColor;
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
