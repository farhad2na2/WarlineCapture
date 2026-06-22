using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class MenuBootstrapSystem
{
    private const int DeferredMatchLoadVisibleFrames = 2;
    private const float MinimumLoadingVisibleSeconds = 2f;
    private const float MatchReadyHoldSeconds = 0.75f;

    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartSystem matchStartSystem = new();
    private readonly PerformanceDiagnosticsSystem performanceDiagnosticsSystem = new();
    private readonly MatchSceneReferenceSystem matchSceneReferenceSystem = new();
    private readonly QuickCustomGameConfigStore quickCustomGameConfigStore = new();
    private readonly MatchLaunchCommand matchLaunchCommand = new();

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
    private SelectionUiCommandSystem boundSelectionUiCommand;
    private SelectionUiReadModelSystem boundSelectionUiReadModel;
    private MainMenuPlayUI boundMainMenu;
    private IBuildingUiCommand boundUiToolkitBuildingUiCommand;
    private IBuildingUiQuery boundUiToolkitBuildingUiQuery;
    private ICatalogPrefabSource boundUiToolkitUnitPrefabSource;
    private ICatalogPrefabSource boundUiToolkitBuildingPrefabSource;
    private UiToolkitMatchHudMinimapSurface boundUiToolkitMinimapSurface;
    private int boundContentVersion = -1;

    public PerformanceDiagnosticsSystem PerformanceDiagnostics => performanceDiagnosticsSystem;
    public bool IsPerformanceDiagnosticsInitialized => diagnosticsInitialized;

    public void Initialize(MenuBootstrapView view)
    {
        if (view == null)
            return;

        bool wasInitialized = initialized;
        EnsurePersistentDiagnosticsInitialized();
        view.ApplyRuntimeUiMode();
        if (view.IsUiToolkitMode)
        {
            if (!wasInitialized)
                ResetShellForFreshMenuScene();

            initialized = true;
            return;
        }

        if (view.ShellEcsPresentation != null)
            view.ShellEcsPresentation.Configure(view.ShellView);
        if (view.ContentSystem != null)
        {
            view.ContentSystem.ConfigureCatalogMetadataResolvers(
                UiCatalogAuthoringMetadataSystem.TryGetBuildingMetadata,
                UiCatalogAuthoringMetadataSystem.TryGetUnitMetadata);
            view.ContentSystem.BindQuickCustomRuntimeDependencies(quickCustomGameConfigStore, matchLaunchCommand);
        }
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
        if (view == null)
            return;
        view.ApplyRuntimeUiMode();
        bool useUiToolkit = view.IsUiToolkitMode;

        if (!TryGetWorldEntityManager(out EntityManager entityManager))
            return;

        sceneLifecycleSystem.Update(entityManager);
        matchStartSystem.Update(entityManager);

        if (!TryGetBoundary(entityManager, out Entity boundary))
            return;

        UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        if (!useUiToolkit)
            ApplyUiPresentationMode(view.UiCamera, view.UiCanvas, shellState, entityManager);
        QueueDeferredMatchLoadAfterLoadingFeedback(entityManager, shellState);
        UpdateActualLoadingProgress(entityManager, boundary, shellState);
        if (useUiToolkit)
        {
            ApplyUiToolkitPresentationMode(view.UiCamera, view.UiCanvas, shellState, entityManager);
            BindUiToolkitMatchReadModels(view, shellState);
            ClearBoundMatchRuntimeUi();
            return;
        }

        ClearUiToolkitMatchReadModels();
        BindMatchRuntimeUi(view, shellState);
    }

    public void Shutdown(MenuBootstrapView view)
    {
        if (view != null && view.UiCanvas != null && view.UiCanvas.transform.localScale != Vector3.one)
            view.UiCanvas.transform.localScale = Vector3.one;

        if (view != null)
            RestoreUiPresentationMode(view.UiCamera, view.UiCanvas);

        initialized = false;
        hasCapturedUiPresentation = false;
        deferredMatchLoadFrame = -1;
        ResetLoadingMinimumWindow();
        ResetMatchReadyHoldWindow();
        matchLoadQueuedForCurrentRoute = false;
        ClearBoundMatchRuntimeUi();
        ClearUiToolkitMatchReadModels();
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
            Status = new FixedString64Bytes(ToFixed64Status(status)),
            IsComplete = complete ? (byte)1 : (byte)0
        });
    }

    private static string ToFixed64Status(string status)
    {
        const int MaxAsciiChars = 60;
        if (string.IsNullOrEmpty(status))
            return "Loading";
        return status.Length <= MaxAsciiChars ? status : status.Substring(0, MaxAsciiChars);
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
            if (TryGetMatchStartProgress(entityManager, out MatchStartProgressComponent progress))
            {
                float startupProgress = 0.90f + (Mathf.Clamp01(progress.Progress01) * 0.09f);
                string status = progress.Status.Length == 0 ? "Starting match" : progress.Status.ToString();
                SetLoading(entityManager, boundary, startupProgress, false, status);
                return;
            }

            SetLoading(entityManager, boundary, 0.95f, false, "Starting match");
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

        MatchBootstrapSystem matchBootstrap = matchScene.MatchBootstrap;
        MainMenuPlayUI mainMenu = matchBootstrap.EnsureMainMenuRuntimeDependencies();
        if (view.ContentSystem.TryGetMatchHudSelectionPanelView(out MatchHudSelectionPanelView selectionPanelView))
            matchBootstrap.BindMatchHudSelectionPanel(selectionPanelView);

        SelectionUiCommandSystem selectionUiCommand = matchBootstrap.SelectionUiCommand;
        if (selectionUiCommand == null)
            return;
        SelectionUiReadModelSystem selectionUiReadModel = matchBootstrap.SelectionUiReadModel;

        int contentVersion = view.ContentSystem.ContentVersion;
        if (boundMatchRuntimeView == matchScene &&
            boundSelectionUiCommand == selectionUiCommand &&
            boundSelectionUiReadModel == selectionUiReadModel &&
            boundMainMenu == mainMenu &&
            boundContentVersion == contentVersion)
        {
            return;
        }

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

    private void BindUiToolkitMatchReadModels(MenuBootstrapView view, UiShellStateComponent shellState)
    {
        if (shellState.ActiveRoute != UIRoute.Match)
        {
            ClearUiToolkitMatchReadModels();
            return;
        }

        if (!matchSceneReferenceSystem.TryGetLoadedMatchSceneView(out MatchSceneView matchScene))
        {
            ClearUiToolkitMatchReadModels();
            return;
        }

        MatchBootstrapSystem matchBootstrap = matchScene.MatchBootstrap;
        MainMenuPlayUI mainMenu = matchBootstrap != null
            ? matchBootstrap.EnsureMainMenuRuntimeDependencies()
            : null;
        BindUiToolkitMinimapSurface(view, mainMenu);

        IBuildingUiCommand command = matchBootstrap != null
            ? matchBootstrap.BuildingUiCommandContract
            : null;
        IBuildingUiQuery buildQuery = matchBootstrap != null
            ? matchBootstrap.BuildingUiQueryContract
            : null;
        BuildingPlacementSystemConfig buildingConfig = matchBootstrap != null
            ? matchBootstrap.BuildingPlacementConfig
            : null;
        ICatalogPrefabSource unitPrefabSource = buildingConfig != null && buildingConfig.UnitPrefabRegistryConfig != null
            ? buildingConfig.UnitPrefabRegistryConfig
            : buildingConfig;
        ICatalogPrefabSource buildingPrefabSource = buildingConfig;
        if (ReferenceEquals(boundUiToolkitBuildingUiCommand, command) &&
            ReferenceEquals(boundUiToolkitBuildingUiQuery, buildQuery) &&
            ReferenceEquals(boundUiToolkitUnitPrefabSource, unitPrefabSource) &&
            ReferenceEquals(boundUiToolkitBuildingPrefabSource, buildingPrefabSource))
        {
            return;
        }

        UiBuildPlacementReadModelSource.Configure(command);
        UiBuildDrawerReadModelSource.Configure(
            unitPrefabSource,
            buildingPrefabSource,
            command,
            buildQuery,
            UiCatalogAuthoringMetadataSystem.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataSystem.TryGetUnitMetadata);
        boundUiToolkitBuildingUiCommand = command;
        boundUiToolkitBuildingUiQuery = buildQuery;
        boundUiToolkitUnitPrefabSource = unitPrefabSource;
        boundUiToolkitBuildingPrefabSource = buildingPrefabSource;
    }

    private void ClearUiToolkitMatchReadModels()
    {
        ClearUiToolkitMinimapSurface();
        if (boundUiToolkitBuildingUiCommand == null &&
            boundUiToolkitBuildingUiQuery == null &&
            boundUiToolkitUnitPrefabSource == null &&
            boundUiToolkitBuildingPrefabSource == null &&
            !UiBuildPlacementReadModelSource.HasBuildingUiCommand &&
            !UiBuildDrawerReadModelSource.HasCatalogSources)
        {
            return;
        }

        UiBuildPlacementReadModelSource.Clear();
        UiBuildDrawerReadModelSource.Clear();
        boundUiToolkitBuildingUiCommand = null;
        boundUiToolkitBuildingUiQuery = null;
        boundUiToolkitUnitPrefabSource = null;
        boundUiToolkitBuildingPrefabSource = null;
    }

    private void BindUiToolkitMinimapSurface(MenuBootstrapView view, MainMenuPlayUI mainMenu)
    {
        if (view == null || view.UiToolkitShellRoot == null || view.UiToolkitShellView == null || mainMenu == null)
        {
            ClearUiToolkitMinimapSurface();
            return;
        }

        UiToolkitMatchHudMinimapSurface surface =
            UiToolkitMatchHudMinimapSurface.Ensure(view.UiToolkitShellRoot);
        if (surface == null)
        {
            ClearUiToolkitMinimapSurface();
            return;
        }

        surface.Configure(view.UiToolkitShellView, mainMenu);
        boundUiToolkitMinimapSurface = surface;
    }

    private void ClearUiToolkitMinimapSurface()
    {
        if (boundUiToolkitMinimapSurface == null)
            return;

        boundUiToolkitMinimapSurface.Clear();
        boundUiToolkitMinimapSurface = null;
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

    private void ApplyUiToolkitPresentationMode(Camera uiCamera, Canvas uiCanvas, UiShellStateComponent shellState, EntityManager entityManager)
    {
        CaptureUiPresentationMode(uiCamera, uiCanvas);

        if (uiCanvas != null && uiCanvas.enabled)
            uiCanvas.enabled = false;

        if (uiCamera == null)
            return;

        if (shellState.ActiveRoute == UIRoute.Match && IsMatchSceneLoaded(entityManager))
        {
            if (uiCamera.clearFlags != CameraClearFlags.Depth)
                uiCamera.clearFlags = CameraClearFlags.Depth;
            if (uiCamera.enabled)
                uiCamera.enabled = false;
            return;
        }

        if (uiCamera.clearFlags != CameraClearFlags.SolidColor)
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
        if (!uiCamera.enabled)
            uiCamera.enabled = true;
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
            sceneLifecycleQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneLifecycleBoundaryComponent>());
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
            matchStartBoundaryQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MatchStartBoundaryComponent>());
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
                ComponentType.ReadOnly<MatchStartBoundaryComponent>(),
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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
                SuppliesText = new FixedString32Bytes("1,280"),
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
            boundaryQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (cachedBoundaryEntity != Entity.Null &&
            entityManager.Exists(cachedBoundaryEntity) &&
            entityManager.HasComponent<UiShellBoundaryComponent>(cachedBoundaryEntity))
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
