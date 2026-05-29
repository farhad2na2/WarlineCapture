using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuBootstrapSystem
{
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();
    private readonly PerformanceDiagnosticsSystem performanceDiagnosticsSystem = new();

    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;
    private bool diagnosticsInitialized;
    private bool initialized;
    private float loadingElapsedSeconds;

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
        SetLoading(entityManager, boundary, progress, progress >= 1f);
    }

    public void Shutdown()
    {
        initialized = false;
        loadingElapsedSeconds = 0f;
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

    private static void SetLoading(EntityManager entityManager, Entity boundary, float progress01, bool complete)
    {
        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = new FixedString64Bytes(complete ? "Command shell ready" : "Loading command shell"),
            IsComplete = complete ? (byte)1 : (byte)0
        });
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
