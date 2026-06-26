using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

internal sealed class MatchBootstrapCompositionSystemHelper
{
    private enum GameplayStartStep : byte
    {
        Idle = 0,
        InitializeManagedRuntime,
        ResetStats,
        ProjectStartupConfig,
        CustomGameStartup,
        AiStartup,
        BindMainMenu,
        InitializeGameplayFeatures,
        FinalizeRuntimeState,
        Complete
    }

    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem;
    private VisualQualitySettingsSystem _visualQualitySettingsSystem;
    private AIStartupSystem _aiStartupSystem;
    private readonly InitialFactionSpawnCellSystem _initialFactionSpawnCellSystem = new();
    private readonly List<InitialFactionSpawnCellFallbackEntry> _initialFactionSpawnCellFallbackEntries = new();
    private readonly GameplaySceneBindingSceneSystemHelper _gameplaySceneBindingSystem = new();
    private RuntimeRootSystem _runtimeRootSystem;
    private readonly GameplayFeatureStartupCompositionSystemHelper _gameplayFeatureStartupSystem = new();
    private RuntimeGridBootstrapSystem _runtimeGridBootstrapSystem;
    private MapSurfaceRuntimeBootstrapSceneSystemHelper _mapSurfaceRuntimeBootstrapSystem;
    private CustomGameStartupSystemHelper _customGameStartupSystem;
    private readonly PerformanceDiagnosticsReferenceDiagnosticsSystemHelper _performanceDiagnosticsReferenceSystem = new();
    private readonly MatchIntroEcsStateQuery matchIntroStateQuery = new();

    private readonly ManagedGameplayStartupSystem managedGameplayStartupSystem = new();
    private readonly GameplayRuntimeUpdateCompositionSystemHelper gameplayRuntimeUpdateSystem = new();
    private readonly PerformanceDiagnosticsSystem fallbackPerformanceDiagnosticsSystem = new();
    private bool fallbackPerformanceDiagnosticsInitialized;
    private MatchSceneView sceneView;

    public MatchSceneView SceneView => sceneView;
    public bool HasSceneView => sceneView != null;
    private MatchSceneView MatchScene => sceneView;

    public Camera WorldCamera => MatchScene != null ? MatchScene.WorldCamera : null;
    public Light DirectionalLight => MatchScene != null ? MatchScene.DirectionalLight : null;
    public Volume GlobalVolume => MatchScene != null ? MatchScene.GlobalVolume : null;
    public VisualQualityProfileAsset VisualQualityProfile => MatchScene != null ? MatchScene.VisualQualityProfile : null;
    public CombinedMeshBaker DecorationCombinedMeshBaker => MatchScene != null ? MatchScene.DecorationCombinedMeshBaker : null;
    public Transform DecorationRoot => MatchScene != null ? MatchScene.DecorationRoot : null;

    public RTSSelectionSystemConfig RtsSelectionConfig => MatchScene != null ? MatchScene.RtsSelectionConfig : null;
    public RoadBuildSystemConfig RoadBuildConfig => MatchScene != null ? MatchScene.RoadBuildConfig : null;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => MatchScene != null ? MatchScene.BuildingPlacementConfig : null;
    public MapBuildingPlacementConfig MapBuildingPlacementConfig => MatchScene != null ? MatchScene.MapBuildingPlacementConfig : null;
    public MapVehiclePlacementConfig MapVehiclePlacementConfig => MatchScene != null ? MatchScene.MapVehiclePlacementConfig : null;
    public Transform MapBuildingAuthoringRoot => MatchScene != null ? MatchScene.MapBuildingAuthoringRoot : null;
    public Transform MapVehicleAuthoringRoot => MatchScene != null ? MatchScene.MapVehicleAuthoringRoot : null;
    public UnitAttackTraceSystemConfig UnitAttackTraceConfig => MatchScene != null ? MatchScene.UnitAttackTraceConfig : null;
    public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => MatchScene != null ? MatchScene.RuntimeCitySpawnerConfig : null;
    public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => MatchScene != null ? MatchScene.RuntimeDecorationSpawnerConfig : null;
    public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => MatchScene != null ? MatchScene.RuntimeGridBlockerConfig : null;
    public DayNightSystemConfig DayNightConfig => MatchScene != null ? MatchScene.DayNightConfig : null;
    public GameStringsConfig GameStringsConfig => MatchScene != null ? MatchScene.GameStringsConfig : null;
    public AIPlanEntryStartupConfig AIPlanEntryConfig => MatchScene != null ? MatchScene.AIPlanEntryConfig : null;
    public IReadOnlyList<AIControllerConfig> AIControllerConfigs => MatchScene != null ? MatchScene.AIControllerConfigs : Array.Empty<AIControllerConfig>();
    private FactionVisualSettingsConfig FactionVisualConfig => MatchScene != null ? MatchScene.FactionVisualConfig : null;
    private PrefabPreviewCameraConfig PrefabPreviewCameraConfig => MatchScene != null ? MatchScene.PrefabPreviewCameraConfig : null;
    private GridAuthoringConfig RuntimeGridConfig => MatchScene != null ? MatchScene.RuntimeGridConfig : null;
    private MapSurfaceAuthoring MapSurfaceAuthoring => MatchScene != null ? MatchScene.MapSurfaceAuthoring : null;

    public RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers { get; private set; }
    public RuntimeDecorationSpawnerPresentationSystemHelper RuntimeDecorations { get; private set; }
    public RuntimeCityCompositionSystemHelper RuntimeCity { get; private set; }
    public RoadBuildReadModelCompositionSystemHelper RoadBuildReadModel { get; private set; }
    public BuildingSelectionClickUtilitySystemHelper BuildingSelectionClick { get; private set; }
    public BuildingUiCommandBoundary BuildingUiCommand { get; private set; }
    public BuildingUiQueryUiSystemHelper BuildingUiQuery { get; private set; }
    public IBuildingUiCommand BuildingUiCommandContract { get; private set; }
    public IBuildingUiQuery BuildingUiQueryContract { get; private set; }
    public BuildingRuntimeUpdateCompositionSystemHelper BuildingRuntimeUpdate { get; private set; }
    public SelectionUiCommandSystem SelectionUiCommand { get; private set; }
    public SelectionUiReadModelSystem SelectionUiReadModel { get; private set; }
    public SelectionUiCameraSystemHelper SelectionUiCamera { get; private set; }
    public SelectionBuildingInteractionSystem SelectionBuildingInteraction { get; private set; }
    public SelectionScreenMarkerUiSystemHelper SelectionScreenMarkers { get; private set; }
    public ISelectionRectangleView SelectionRectangle { get; private set; }
    public ISelectionDiagnosticsSink SelectionDiagnosticsSink { get; } = new SelectionDiagnosticsSinkAdapter();
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public IUnitAttackTraceRenderer UnitAttackTraces { get; private set; }
    public IUnitImpostorRenderer UnitImpostors { get; private set; }
    public bool GameplayInitialized { get; private set; }
    public BuildingSelectionClickUtilitySystemHelper.Context BuildingSelectionClickContext { get; private set; }
    public BuildingUiCommandBoundary.Context BuildingUiCommandContext => _buildingUiCommandContext;
    public BuildingUiQueryUiSystemHelper.Context BuildingUiQueryContext => _buildingUiQueryContext;
    private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper _buildingRuntimeCitySpawn;
    private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context _buildingRuntimeCitySpawnContext;
    private BuildingUiCommandBoundary.Context _buildingUiCommandContext;
    private BuildingUiQueryUiSystemHelper.Context _buildingUiQueryContext;
    private IMatchRuntimeState _matchRuntimeState;
    private IMatchHudCameraControl _matchHudCameraControl;
    private IMatchHudMinimapDataSource _matchHudMinimapDataSource;
    private ISelectionRectangleState _selectionRectangleState;
    private BuildingPlacementInteractionBoundaryCompositionSystemHelper _buildingPlacementInteraction;
    private BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context _buildingPlacementInteractionContext;
    private RoadRuntimeGenerationCompositionSystemHelper _roadRuntimeGeneration;
    private RoadRuntimeGenerationCompositionSystemHelper.Context _roadRuntimeGenerationContext;
    private Action _roadRuntimeUpdate;
    private Action _roadOnGui;
    private Action _disposeRoad;
    private Action<IMatchRuntimeUi> _bindRoadMainMenu;
    private Action<IMatchRuntimeUi, RuntimeGridBlockerPresentationSystemHelper> _bindRoadGameplayFeatures;
    private Action<IMatchRuntimeUi> _bindBuildingMainMenu;
    private Action<IMatchRuntimeUi, SelectionUiCameraSystemHelper, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventCompositionSystemHelper> _bindBuildingGameplayFeatures;
    private Action<IMatchRuntimeUi> _bindSelectionMainMenu;
    private Action<IMatchHudSelectionPanelView> _bindMatchHudSelectionPanel;
    private Action _selectionRuntimeUpdate;
    private Action _citizenPopulationRuntimeUpdate;
    private Action _disposeCitizenPopulation;
    private CitizenPopulationReadModelCompositionSystemHelper _citizenPopulationReadModel;
    private CitizenPopulationEventCompositionSystemHelper _citizenPopulationEventSystem;
    private Action _disposeSelection;
    private Action _disposeBuildingGameplay;
    private BuildingRuntimeUpdateCompositionSystemHelper.Context _buildingRuntimeUpdateContext;
    private Entity _buildingRuntimeBoundaryEntity;
    private PerformanceDiagnosticsSystem _performanceDiagnosticsSystem;
    private bool _mainMenuBaseBindingsApplied;
    private bool _mainMenuRoadBindingApplied;
    private bool _mainMenuBuildingBindingApplied;
    private bool _mainMenuSelectionBindingApplied;
    private RuntimeGridBlockerPresentationSystemHelper _mainMenuFeatureBoundGridBlockers;
    private RuntimeCityCompositionSystemHelper _mainMenuFeatureBoundRuntimeCity;
    private IMatchHudSelectionPanelView _pendingMatchHudSelectionPanelView;
    private IMatchHudSelectionPanelView _boundMatchHudSelectionPanelView;
    private IMatchHudSelectionPanelView _deferredMatchHudSelectionPanelView;
    private bool _gameplayStartPending;
    private bool _gameplayStartRequested;
    private bool _gameplayStartComplete;
    private bool _managedRuntimeInitialized;
    private bool _visualQualitySettingsInitialized;
    private GameplayStartStep _gameplayStartStep;
    private AISettingsSnapshot _pendingAiSettingsSnapshot;
    private AIStartupSystem.Result _pendingAiStartupResult;
    private float _gameplayStartProgress01;
    private string _gameplayStartStatus = "Waiting for match scene";
    private Transform _runtimeBlockerRoot;
    private Transform _runtimeCityRoot;
    private Transform _runtimeTransportsRoot;
    private Transform _runtimeUiRoot;

    public bool GameplayStartRequested => _gameplayStartRequested;
    public bool GameplayStartComplete => _gameplayStartComplete && !_gameplayStartPending;
    public float GameplayStartProgress01 => _gameplayStartComplete && _gameplayStartPending
        ? 0.98f
        : _gameplayStartProgress01;
    public string GameplayStartStatus => _gameplayStartComplete && _gameplayStartPending
        ? "Spawning world"
        : _gameplayStartStatus;

    public void Awake(MatchSceneView view, Transform ownerTransform, int ownerLayer)
    {
        Initialize(view);
        _performanceDiagnosticsSystem = ResolvePerformanceDiagnosticsSystem();
        _runtimeCameraReferenceSystem = ResolveRuntimeCameraReferenceSystem(World.DefaultGameObjectInjectionWorld);

        ResolveRuntimeRootSystem()?.Ensure(
            ownerTransform,
            ref _runtimeBlockerRoot,
            ref _runtimeCityRoot,
            ref _runtimeTransportsRoot,
            ref _runtimeUiRoot);
        _runtimeCameraReferenceSystem?.SetWorldCamera(WorldCamera);
    }

    public void BeginGameplay()
    {
        if (_gameplayStartComplete)
            return;
        if (_gameplayStartRequested)
            return;

        _gameplayStartRequested = true;
        _gameplayStartComplete = false;
        _gameplayStartStep = GameplayStartStep.InitializeManagedRuntime;
        _gameplayStartProgress01 = 0f;
        _gameplayStartStatus = "Preparing match";
    }

    public void Update()
    {
        AdvanceGameplayStartPipeline();
        UpdateRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _roadRuntimeUpdate,
            BuildingRuntimeUpdate,
            _buildingRuntimeUpdateContext,
            _selectionRuntimeUpdate,
            WorldCamera,
            RuntimeCity,
            RuntimeGridBlockers,
            RuntimeDecorations,
            DayNight,
            _citizenPopulationRuntimeUpdate,
            MainMenu,
            UnitImpostors,
            ref _gameplayStartPending);
        _visualQualitySettingsSystem?.Update();
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        OnApplicationFocus(_performanceDiagnosticsSystem, hasFocus);
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        OnApplicationPause(_performanceDiagnosticsSystem, pauseStatus);
    }

    public void OnApplicationFocus(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool hasFocus)
    {
        ForwardApplicationFocus(performanceDiagnosticsSystem, hasFocus);
    }

    public void OnApplicationPause(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool pauseStatus)
    {
        ForwardApplicationPause(performanceDiagnosticsSystem, pauseStatus);
    }

    public void LateUpdate()
    {
        LateUpdateRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            UnitAttackTraces,
            UnitImpostors);
    }

    public void OnGUI()
    {
        OnGuiRuntime(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _roadOnGui,
            SelectionRectangle);
    }

    public void OnDestroy()
    {
        ShutdownRuntime(
            MainMenu,
            _disposeSelection,
            _disposeBuildingGameplay,
            _disposeRoad,
            UnitAttackTraces,
            UnitImpostors,
            _disposeCitizenPopulation,
            DayNight,
            RuntimeDecorations,
            RuntimeGridBlockers,
            RuntimeCity,
            _mapSurfaceRuntimeBootstrapSystem,
            _runtimeCameraReferenceSystem,
            _performanceDiagnosticsSystem);
        gameplayRuntimeUpdateSystem.Dispose();
        _visualQualitySettingsSystem?.Dispose();
        matchIntroStateQuery.Reset();

        MainMenu = null;
        _mainMenuBaseBindingsApplied = false;
        _mainMenuRoadBindingApplied = false;
        _mainMenuBuildingBindingApplied = false;
        _mainMenuSelectionBindingApplied = false;
        _mainMenuFeatureBoundGridBlockers = null;
        _mainMenuFeatureBoundRuntimeCity = null;
        SelectionUiCommand = null;
        SelectionUiReadModel = null;
        SelectionUiCamera = null;
        SelectionBuildingInteraction = null;
        SelectionScreenMarkers = null;
        SelectionRectangle = null;
        _matchRuntimeState = null;
        _matchHudCameraControl = null;
        _matchHudMinimapDataSource = null;
        _selectionRectangleState = null;
        BuildingSelectionClick = null;
        BuildingSelectionClickContext = default;
        _buildingRuntimeCitySpawn = null;
        _buildingRuntimeCitySpawnContext = default;
        BuildingUiCommand = null;
        _buildingUiCommandContext = default;
        BuildingUiCommandContract = null;
        BuildingUiQuery = null;
        _buildingUiQueryContext = default;
        BuildingUiQueryContract = null;
        _buildingPlacementInteraction = null;
        _buildingPlacementInteractionContext = default;
        _roadRuntimeGenerationContext = default;
        _roadRuntimeUpdate = null;
        _roadOnGui = null;
        _disposeRoad = null;
        _bindRoadMainMenu = null;
        _bindRoadGameplayFeatures = null;
        _bindBuildingMainMenu = null;
        _bindBuildingGameplayFeatures = null;
        _bindSelectionMainMenu = null;
        _bindMatchHudSelectionPanel = null;
        _selectionRuntimeUpdate = null;
        _citizenPopulationRuntimeUpdate = null;
        _disposeCitizenPopulation = null;
        _citizenPopulationReadModel = null;
        _citizenPopulationEventSystem = null;
        _disposeSelection = null;
        _disposeBuildingGameplay = null;
        BuildingRuntimeUpdate = null;
        _buildingRuntimeUpdateContext = default;
        _roadRuntimeGeneration = null;
        FactionVisuals = null;
        UnitAttackTraces = null;
        UnitImpostors = null;
        DayNight = null;
        _pendingMatchHudSelectionPanelView = null;
        _boundMatchHudSelectionPanelView = null;
        _deferredMatchHudSelectionPanelView = null;
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCity = null;
        _runtimeGridBootstrapSystem = null;
        _mapSurfaceRuntimeBootstrapSystem = null;
        _initialFactionSpawnCellFallbackEntries.Clear();
        _customGameStartupSystem = null;
        _aiStartupSystem = default;
        _gameplayStartRequested = false;
        _gameplayStartComplete = false;
        _managedRuntimeInitialized = false;
        _visualQualitySettingsInitialized = false;
        _gameplayStartStep = GameplayStartStep.Idle;
        _gameplayStartProgress01 = 0f;
        _gameplayStartStatus = "Waiting for match scene";
    }


    public void Initialize(MatchSceneView view)
    {
        sceneView = view;
    }

    public void Shutdown()
    {
        matchIntroStateQuery.Reset();
        sceneView = null;
    }

    public PerformanceDiagnosticsSystem ResolvePerformanceDiagnosticsSystem()
    {
        if (_performanceDiagnosticsReferenceSystem.TryGet(out PerformanceDiagnosticsSystem persistentDiagnostics))
            return persistentDiagnostics;

        if (!fallbackPerformanceDiagnosticsInitialized)
        {
            Application.runInBackground = true;
            fallbackPerformanceDiagnosticsSystem.Initialize();
            fallbackPerformanceDiagnosticsInitialized = true;
        }

        return fallbackPerformanceDiagnosticsSystem;
    }

    private void ForwardApplicationFocus(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool hasFocus)
    {
        if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystem)
            fallbackPerformanceDiagnosticsSystem.OnApplicationFocus(hasFocus);
    }

    private void ForwardApplicationPause(PerformanceDiagnosticsSystem performanceDiagnosticsSystem, bool pauseStatus)
    {
        if (fallbackPerformanceDiagnosticsInitialized && performanceDiagnosticsSystem == fallbackPerformanceDiagnosticsSystem)
            fallbackPerformanceDiagnosticsSystem.OnApplicationPause(pauseStatus);
    }

    public ManagedGameplayStartupSystem.Result InitializeManagedRuntime(
        DayNightSystemConfig dayNightConfig,
        FactionVisualSettingsConfig factionVisualConfig,
        RoadBuildSystemConfig roadBuildConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        MapBuildingPlacementConfig mapBuildingPlacementConfig,
        MapVehiclePlacementConfig mapVehiclePlacementConfig,
        RTSSelectionSystemConfig rtsSelectionConfig,
        RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
        GameStringsConfig gameStringsConfig,
        Camera worldCamera,
        Light directionalLight,
        Volume globalVolume,
        Transform runtimeTransportsRoot,
        Transform runtimeUiRoot,
        Func<Transform, RTSSelectionSystemConfig, ISelectionRectangleView> createSelectionRectangleView,
        Transform mapBuildingAuthoringRoot,
        Transform mapVehicleAuthoringRoot,
        IMatchIntroStateQuery matchIntroStateQuery)
    {
        return managedGameplayStartupSystem.Initialize(
            dayNightConfig,
            factionVisualConfig,
            roadBuildConfig,
            buildingPlacementConfig,
            mapBuildingPlacementConfig,
            mapVehiclePlacementConfig,
            rtsSelectionConfig,
            runtimeCitySpawnerConfig,
            gameStringsConfig,
            worldCamera,
            directionalLight,
            globalVolume,
            runtimeTransportsRoot,
            runtimeUiRoot,
            createSelectionRectangleView,
            SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionPortraitSprite,
            SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionCardPortraitSprite,
            BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata,
            BuildingProductionUnitMetadataPrefabSystemHelper.PrepareTransportDropVisual,
            BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
            BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata,
            mapBuildingAuthoringRoot,
            mapVehicleAuthoringRoot,
            matchIntroStateQuery);
    }

    public MainMenuPlayUI EnsureMainMenuRuntimeDependencies(bool resetRuntimeState = false)
    {
        if (SelectionUiCommand == null)
            return MainMenu;

        if (MainMenu == null)
            MainMenu = new MainMenuPlayUI();

        EnsureUiRuntimeBoundaryAdapters();
        MainMenu.Init(SelectionUiCommand, _matchRuntimeState, _matchHudCameraControl, _matchHudMinimapDataSource, resetRuntimeState);
        ApplyMainMenuBaseBindings();
        ApplyMainMenuFeatureBindingsIfReady();
        return MainMenu;
    }

    public void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
    {
        _pendingMatchHudSelectionPanelView = view;
        if (_bindMatchHudSelectionPanel == null)
        {
            if (_deferredMatchHudSelectionPanelView == view)
                return;

            _deferredMatchHudSelectionPanelView = view;
            return;
        }

        if (_boundMatchHudSelectionPanelView == view)
            return;

        _bindMatchHudSelectionPanel.Invoke(view);
        _boundMatchHudSelectionPanelView = view;
        _deferredMatchHudSelectionPanelView = null;
    }

    private void ApplyMainMenuBaseBindings()
    {
        if (MainMenu == null)
            return;

        if (!_mainMenuRoadBindingApplied && _bindRoadMainMenu != null)
        {
            _bindRoadMainMenu.Invoke(MainMenu);
            _mainMenuRoadBindingApplied = true;
        }

        if (!_mainMenuBuildingBindingApplied && _bindBuildingMainMenu != null)
        {
            _bindBuildingMainMenu.Invoke(MainMenu);
            _mainMenuBuildingBindingApplied = true;
        }

        if (!_mainMenuSelectionBindingApplied && _bindSelectionMainMenu != null)
        {
            _bindSelectionMainMenu.Invoke(MainMenu);
            _mainMenuSelectionBindingApplied = true;
        }

        _mainMenuBaseBindingsApplied =
            _mainMenuRoadBindingApplied &&
            _mainMenuBuildingBindingApplied &&
            _mainMenuSelectionBindingApplied;
    }

    private void ApplyMainMenuFeatureBindingsIfReady()
    {
        if (!GameplayInitialized || MainMenu == null)
            return;
        if (_mainMenuFeatureBoundGridBlockers == RuntimeGridBlockers &&
            _mainMenuFeatureBoundRuntimeCity == RuntimeCity)
        {
            return;
        }

        _bindRoadGameplayFeatures?.Invoke(MainMenu, RuntimeGridBlockers);
        _bindBuildingGameplayFeatures?.Invoke(
            MainMenu,
            SelectionUiCamera,
            SelectionBuildingInteraction,
            RuntimeGridBlockers,
            RuntimeCity,
            _citizenPopulationEventSystem);
        _mainMenuFeatureBoundGridBlockers = RuntimeGridBlockers;
        _mainMenuFeatureBoundRuntimeCity = RuntimeCity;
    }

    public void UpdateRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        Action roadBuildRuntimeUpdate,
        BuildingRuntimeUpdateCompositionSystemHelper buildingRuntimeUpdate,
        BuildingRuntimeUpdateCompositionSystemHelper.Context buildingRuntimeUpdateContext,
        Action selectionRuntimeUpdate,
        Camera worldCamera,
        RuntimeCityCompositionSystemHelper runtimeCity,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
        RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations,
        DayNightSystem dayNight,
        Action citizenPopulationRuntimeUpdate,
        IMatchRuntimeUi mainMenu,
        IUnitImpostorRenderer unitImpostors,
        ref bool gameplayStartPending)
    {
        gameplayRuntimeUpdateSystem.Update(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            roadBuildRuntimeUpdate,
            buildingRuntimeUpdate,
            buildingRuntimeUpdateContext,
            selectionRuntimeUpdate,
            worldCamera,
            runtimeCity,
            runtimeGridBlockers,
            runtimeDecorations,
            dayNight,
            citizenPopulationRuntimeUpdate,
            mainMenu,
            unitImpostors,
            ref gameplayStartPending);
    }

    public void LateUpdateRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        IUnitAttackTraceRenderer unitAttackTraces,
        IUnitImpostorRenderer unitImpostors)
    {
        gameplayRuntimeUpdateSystem.LateUpdate(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            unitAttackTraces,
            unitImpostors);
    }

    public void OnGuiRuntime(
        bool gameplayInitialized,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem,
        Action roadBuildOnGui,
        ISelectionRectangleView selectionRectangleView)
    {
        gameplayRuntimeUpdateSystem.OnGui(
            gameplayInitialized,
            runtimeGameplayStateSystem,
            performanceDiagnosticsSystem,
            roadBuildOnGui,
            selectionRectangleView);
    }

    public void ShutdownRuntime(
        MainMenuPlayUI mainMenu,
        Action disposeSelection,
        Action disposeBuildingGameplay,
        Action disposeRoad,
        IUnitAttackTraceRenderer unitAttackTraces,
        IUnitImpostorRenderer unitImpostors,
        Action disposeCitizenPopulation,
        DayNightSystem dayNight,
        RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
        RuntimeCityCompositionSystemHelper runtimeCity,
        MapSurfaceRuntimeBootstrapSceneSystemHelper mapSurfaceRuntimeBootstrapSystem,
        RuntimeCameraReferenceSystem runtimeCameraReferenceSystem,
        PerformanceDiagnosticsSystem performanceDiagnosticsSystem)
    {
        Shutdown();
        mainMenu?.Dispose();
        disposeSelection?.Invoke();
        disposeBuildingGameplay?.Invoke();
        disposeRoad?.Invoke();
        unitAttackTraces?.Dispose();
        unitImpostors?.Dispose();
        disposeCitizenPopulation?.Invoke();
        dayNight?.Dispose();
        runtimeDecorations?.Dispose();
        runtimeGridBlockers?.Dispose();
        runtimeCity?.Dispose();
        mapSurfaceRuntimeBootstrapSystem?.DisposeRuntimeSurface();
        runtimeCameraReferenceSystem?.ClearWorldCamera();
        ReleasePerformanceDiagnostics(performanceDiagnosticsSystem);
        SharedPrefabPreviewCache.ReleaseAll();
    }

    private void ReleasePerformanceDiagnostics(PerformanceDiagnosticsSystem performanceDiagnosticsSystem)
    {
        if (!fallbackPerformanceDiagnosticsInitialized ||
            performanceDiagnosticsSystem != fallbackPerformanceDiagnosticsSystem)
        {
            return;
        }

        fallbackPerformanceDiagnosticsSystem.Dispose();
        fallbackPerformanceDiagnosticsInitialized = false;
    }

    private void AdvanceGameplayStartPipeline()
    {
        if (!_gameplayStartRequested || _gameplayStartComplete)
            return;

        switch (_gameplayStartStep)
        {
            case GameplayStartStep.InitializeManagedRuntime:
                SetGameplayStartProgress(0.02f, "Preparing gameplay runtime");
                InitializeManagedRuntimeIfNeeded();
                _gameplayStartStep = GameplayStartStep.ResetStats;
                break;

            case GameplayStartStep.ResetStats:
                SetGameplayStartProgress(0.10f, "Resetting match state");
                GameRuntimeStats.ConfigureUnitPrefabClassifier(GameRuntimeStatsUnitPrefabClassifierPrefabSystemHelper.ClassifyUnitPrefab);
                GameRuntimeStats.Reset();
                _pendingAiSettingsSnapshot = AISettingsRuntimeState.CurrentSnapshot;
                FactionVisualSystem.ProjectConfig(World.DefaultGameObjectInjectionWorld, FactionVisualConfig);
                _gameplayStartStep = GameplayStartStep.ProjectStartupConfig;
                break;

            case GameplayStartStep.ProjectStartupConfig:
                SetGameplayStartProgress(0.24f, "Preparing map data");
                MatchBootstrapStartupConfigProjection.ProjectRuntimeStartupConfig(
                    World.DefaultGameObjectInjectionWorld,
                    ResolveRuntimeGridBootstrapSystem(World.DefaultGameObjectInjectionWorld),
                    ResolveMapSurfaceRuntimeBootstrapSystem(World.DefaultGameObjectInjectionWorld),
                    RuntimeGridConfig,
                    MapSurfaceAuthoring,
                    BuildingPlacementConfig,
                    ResolveAIStartupSystem(World.DefaultGameObjectInjectionWorld),
                    AIControllerConfigs,
                    _pendingAiSettingsSnapshot,
                    _initialFactionSpawnCellFallbackEntries);
                _gameplayStartStep = GameplayStartStep.CustomGameStartup;
                break;

            case GameplayStartStep.CustomGameStartup:
                SetGameplayStartProgress(0.38f, "Preparing unit prefabs");
                CustomGameStartupSystemHelper customGameStartupSystemHelper = ResolveCustomGameStartupSystemHelper(World.DefaultGameObjectInjectionWorld);
                if (customGameStartupSystemHelper != null)
                {
                    customGameStartupSystemHelper.InitializeFromLegacyConfigs(
                        BuildingPlacementConfig != null ? BuildingPlacementConfig.InitialUnitsConfig : null,
                        BuildingPlacementConfig != null ? BuildingPlacementConfig.UnitPrefabRegistryConfig : null);
                }
                else
                {
                    Debug.LogWarning("[MatchBootstrap] missingCustomGameStartupSystemHelper");
                }

                _gameplayStartStep = GameplayStartStep.AiStartup;
                break;

            case GameplayStartStep.AiStartup:
                SetGameplayStartProgress(0.52f, "Preparing AI factions");
                _pendingAiStartupResult = MatchBootstrapStartupConfigProjection.InitializeAiStartupConfig(
                    ResolveAIStartupSystem(World.DefaultGameObjectInjectionWorld),
                    AIControllerConfigs,
                    AIPlanEntryConfig,
                    _pendingAiSettingsSnapshot,
                    ResolveInitialFactionSpawnCell);
                if (_pendingAiStartupResult.HasPlayerAutoMode)
                    _runtimeGameplayStateSystem.PlayerAutoModeEnabled = _pendingAiStartupResult.PlayerAutoModeEnabled;
                _gameplayStartStep = GameplayStartStep.BindMainMenu;
                break;

            case GameplayStartStep.BindMainMenu:
                SetGameplayStartProgress(0.66f, "Binding match HUD");
                EnsureMainMenuRuntimeDependencies(resetRuntimeState: true);
                _gameplayStartStep = GameplayStartStep.InitializeGameplayFeatures;
                break;

            case GameplayStartStep.InitializeGameplayFeatures:
                SetGameplayStartProgress(0.80f, "Starting gameplay systems");
                InitializeGameplaySystemsIfNeeded();
                _gameplayStartStep = GameplayStartStep.FinalizeRuntimeState;
                break;

            case GameplayStartStep.FinalizeRuntimeState:
                SetGameplayStartProgress(0.92f, "Focusing camera");
                _gameplayStartPending = true;
                ResolveRuntimeCameraReferenceSystem(World.DefaultGameObjectInjectionWorld)?.SetWorldCamera(WorldCamera);
                _runtimeGameplayStateSystem.ResetForGameplayStart();
                MatchBootstrapStartupConfigProjection.FocusInitialCameraOnConfiguredFactionBase(
                    World.DefaultGameObjectInjectionWorld,
                    SelectionUiCamera,
                    ResolveInitialFactionSpawnCell,
                    0);
                _gameplayStartStep = GameplayStartStep.Complete;
                break;

            case GameplayStartStep.Complete:
                SetGameplayStartProgress(0.98f, "Spawning world");
                _gameplayStartComplete = true;
                break;
        }
    }

    private void SetGameplayStartProgress(float progress01, string status)
    {
        _gameplayStartProgress01 = Mathf.Clamp01(progress01);
        _gameplayStartStatus = string.IsNullOrEmpty(status) ? "Starting match" : status;
    }

    private RuntimeGridBootstrapSystem ResolveRuntimeGridBootstrapSystem(World world)
    {
        if (world == null || !world.IsCreated)
            return null;

        _runtimeGridBootstrapSystem ??= new RuntimeGridBootstrapSystem();
        return _runtimeGridBootstrapSystem;
    }

    private MapSurfaceRuntimeBootstrapSceneSystemHelper ResolveMapSurfaceRuntimeBootstrapSystem(World world)
    {
        if (world == null || !world.IsCreated)
            return null;

        if (_mapSurfaceRuntimeBootstrapSystem == null)
            _mapSurfaceRuntimeBootstrapSystem = new MapSurfaceRuntimeBootstrapSceneSystemHelper(world);
        return _mapSurfaceRuntimeBootstrapSystem;
    }

    private bool ResolveInitialFactionSpawnCell(byte factionId, out int2 spawnCell)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            spawnCell = default;
            return false;
        }

        return _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell(
            world.EntityManager,
            _initialFactionSpawnCellFallbackEntries,
            factionId,
            out spawnCell);
    }

    private CustomGameStartupSystemHelper ResolveCustomGameStartupSystemHelper(World world)
    {
        if (world == null || !world.IsCreated)
            return null;

        _customGameStartupSystem ??= new CustomGameStartupSystemHelper(world.EntityManager);
        return _customGameStartupSystem;
    }

    private AIStartupSystem ResolveAIStartupSystem(World world)
    {
        _aiStartupSystem = new AIStartupSystem();
        return _aiStartupSystem;
    }

    private RuntimeCameraReferenceSystem ResolveRuntimeCameraReferenceSystem(World world)
    {
        if (world == null || !world.IsCreated)
            return null;

        _runtimeCameraReferenceSystem = world.GetOrCreateSystemManaged<RuntimeCameraReferenceSystem>();
        return _runtimeCameraReferenceSystem;
    }

    private VisualQualitySettingsSystem ResolveVisualQualitySettingsSystem(World world)
    {
        if (world == null || !world.IsCreated)
            return null;

        _visualQualitySettingsSystem = world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>();
        return _visualQualitySettingsSystem;
    }

    private RuntimeRootSystem ResolveRuntimeRootSystem()
    {
        _runtimeRootSystem ??= new RuntimeRootSystem();
        return _runtimeRootSystem;
    }

    private static UnitAttackTracePresentationSystemHelper ResolveUnitAttackTracePresentationSystemHelper()
    {
        return new UnitAttackTracePresentationSystemHelper();
    }

    private static UnitImpostorPresentationSystemHelper ResolveUnitImpostorPresentationSystemHelper()
    {
        return new UnitImpostorPresentationSystemHelper();
    }

    private static ISelectionRectangleView EnsureSelectionRectangleView(
        Transform runtimeUiRoot,
        RTSSelectionSystemConfig rtsSelectionConfig)
    {
        if (runtimeUiRoot == null)
            return null;

        SelectionRectangleView view = runtimeUiRoot.GetComponent<SelectionRectangleView>();
        if (view == null)
            view = runtimeUiRoot.gameObject.AddComponent<SelectionRectangleView>();

        if (rtsSelectionConfig != null)
            view.ApplyStyle(rtsSelectionConfig.SelectionFill, rtsSelectionConfig.SelectionBorder);
        return view;
    }

    private void InitializeManagedRuntimeIfNeeded()
    {
        if (_managedRuntimeInitialized)
            return;

        InitializeVisualQualitySettingsIfNeeded();
        int ownerLayer = MatchScene != null ? MatchScene.gameObject.layer : 0;

        ManagedGameplayStartupSystem.Result managedSystems = InitializeManagedRuntime(
            DayNightConfig,
            FactionVisualConfig,
            RoadBuildConfig,
            BuildingPlacementConfig,
            MapBuildingPlacementConfig,
            MapVehiclePlacementConfig,
            RtsSelectionConfig,
            RuntimeCitySpawnerConfig,
            GameStringsConfig,
            WorldCamera,
            DirectionalLight,
            GlobalVolume,
            _runtimeTransportsRoot,
            _runtimeUiRoot,
            EnsureSelectionRectangleView,
            MapBuildingAuthoringRoot,
            MapVehicleAuthoringRoot,
            matchIntroStateQuery);

        DayNight = managedSystems.DayNight;
        FactionVisuals = managedSystems.FactionVisuals;
        RoadBuildReadModel = managedSystems.RoadBuildReadModel;
        _roadRuntimeGeneration = managedSystems.RoadRuntimeGeneration;
        _roadRuntimeGenerationContext = managedSystems.RoadRuntimeGenerationContext;
        _roadRuntimeUpdate = managedSystems.RoadRuntimeUpdate;
        _roadOnGui = managedSystems.RoadOnGui;
        _disposeRoad = managedSystems.DisposeRoad;
        _bindRoadMainMenu = managedSystems.BindRoadMainMenu;
        _bindRoadGameplayFeatures = managedSystems.BindRoadGameplayFeatures;
        BuildingSelectionClick = managedSystems.BuildingSelectionClick;
        BuildingSelectionClickContext = managedSystems.BuildingSelectionClickContext;
        _buildingRuntimeCitySpawn = managedSystems.BuildingRuntimeCitySpawn;
        _buildingRuntimeCitySpawnContext = managedSystems.BuildingRuntimeCitySpawnContext;
        BuildingUiCommand = managedSystems.BuildingUiCommand;
        _buildingUiCommandContext = managedSystems.BuildingUiCommandContext;
        BuildingUiQuery = managedSystems.BuildingUiQuery;
        _buildingUiQueryContext = managedSystems.BuildingUiQueryContext;
        BuildingUiCommandContract = new BuildingUiCommandAdapter(BuildingUiCommand, _buildingUiCommandContext);
        BuildingUiQueryContract = new BuildingUiQueryAdapter(BuildingUiQuery, _buildingUiQueryContext);
        _buildingPlacementInteraction = managedSystems.BuildingPlacementInteraction;
        _buildingPlacementInteractionContext = managedSystems.BuildingPlacementInteractionContext;
        _bindBuildingMainMenu = managedSystems.BindBuildingMainMenu;
        _bindBuildingGameplayFeatures = managedSystems.BindBuildingGameplayFeatures;
        _bindSelectionMainMenu = managedSystems.BindSelectionMainMenu;
        _bindMatchHudSelectionPanel = managedSystems.BindMatchHudSelectionPanel;
        _mainMenuBaseBindingsApplied = false;
        _mainMenuRoadBindingApplied = false;
        _mainMenuBuildingBindingApplied = false;
        _mainMenuSelectionBindingApplied = false;
        _boundMatchHudSelectionPanelView = null;
        _selectionRuntimeUpdate = managedSystems.SelectionRuntimeUpdate;
        _disposeSelection = managedSystems.DisposeSelection;
        _disposeBuildingGameplay = managedSystems.DisposeBuildingGameplay;
        BuildingRuntimeUpdate = managedSystems.BuildingRuntimeUpdate;
        _buildingRuntimeUpdateContext = managedSystems.BuildingRuntimeUpdateContext;
        SelectionUiCommand = managedSystems.SelectionUiCommand;
        SelectionUiReadModel = managedSystems.SelectionUiReadModel;
        SelectionUiCamera = managedSystems.SelectionUiCamera;
        SelectionBuildingInteraction = managedSystems.SelectionBuildingInteraction;
        SelectionScreenMarkers = managedSystems.SelectionScreenMarkers;
        SelectionRectangle = managedSystems.SelectionRectangleView;
        EnsureUiRuntimeBoundaryAdapters();
        if (SelectionRectangle is SelectionRectangleView selectionRectangleView)
            selectionRectangleView.BindState(_selectionRectangleState);
        InitializeRenderingSystems(ownerLayer);
        _disposeCitizenPopulation = managedSystems.DisposeCitizenPopulation;
        _citizenPopulationRuntimeUpdate = managedSystems.CitizenPopulationComposition != null
            ? managedSystems.CitizenPopulationComposition.RuntimeUpdateSystem.Update
            : null;
        _citizenPopulationReadModel = managedSystems.CitizenPopulationComposition?.ReadModel;
        _citizenPopulationEventSystem = managedSystems.CitizenPopulationComposition?.EventSystem;
        _buildingRuntimeBoundaryEntity = MatchBuildingRuntimeBoundaryBootstrapStartupSystemHelper.Ensure(_buildingRuntimeBoundaryEntity);
        ResolveRuntimeCameraReferenceSystem(World.DefaultGameObjectInjectionWorld)?.SetWorldCamera(WorldCamera);
        _managedRuntimeInitialized = true;
        if (MainMenu != null)
        {
            MainMenu.Init(SelectionUiCommand, _matchRuntimeState, _matchHudCameraControl, _matchHudMinimapDataSource, resetRuntimeState: false);
            ApplyMainMenuBaseBindings();
        }

        if (_pendingMatchHudSelectionPanelView != null)
            BindMatchHudSelectionPanel(_pendingMatchHudSelectionPanelView);
    }

    private void InitializeRenderingSystems(int ownerLayer)
    {
        UnitAttackTracePresentationSystemHelper unitAttackTraces = ResolveUnitAttackTracePresentationSystemHelper();
        unitAttackTraces?.Init(UnitAttackTraceConfig, WorldCamera, ownerLayer);
        UnitAttackTraces = unitAttackTraces;

        UnitImpostorPresentationSystemHelper unitImpostors = ResolveUnitImpostorPresentationSystemHelper();
        unitImpostors?.Init(
            WorldCamera,
            ownerLayer,
            BuildingPlacementConfig != null ? BuildingPlacementConfig.UnitPrefabRegistryConfig : null,
            UnitRenderingMetadataAuthoringSystem.TryGetUnitRenderingMetadata);
        UnitImpostors = unitImpostors;

        SharedPrefabPreviewCache.ConfigureUnitRenderingMetadataResolver(UnitRenderingMetadataAuthoringSystem.TryGetUnitRenderingMetadata);
        SharedPrefabPreviewCache.Init(PrefabPreviewCameraConfig);
    }

    private void EnsureUiRuntimeBoundaryAdapters()
    {
        _matchRuntimeState ??= new MatchRuntimeStateAdapter(_runtimeGameplayStateSystem);
        _matchHudCameraControl = new MatchHudCameraControlAdapter(SelectionUiCamera);
        _matchHudMinimapDataSource ??= new MatchHudMinimapDataSourceAdapter();
        _selectionRectangleState ??= new SelectionRectangleStateAdapter(_matchRuntimeState);
    }

    private void InitializeVisualQualitySettingsIfNeeded()
    {
        if (_visualQualitySettingsInitialized)
            return;

        VisualQualitySettingsSystem visualQualitySettingsSystem = ResolveVisualQualitySettingsSystem(World.DefaultGameObjectInjectionWorld);
        if (visualQualitySettingsSystem == null)
            return;

        visualQualitySettingsSystem.Initialize(VisualQualityProfile, WorldCamera, DirectionalLight, GlobalVolume);
        _visualQualitySettingsInitialized = true;
    }

    private void InitializeGameplaySystemsIfNeeded()
    {
        if (GameplayInitialized)
            return;

        GameplayFeatureStartupCompositionSystemHelper.Result gameplaySystems = _gameplayFeatureStartupSystem.Initialize(
            RuntimeCitySpawnerConfig,
            RuntimeGridBlockerConfig,
            RuntimeDecorationSpawnerConfig,
            _roadRuntimeGeneration,
            _roadRuntimeGenerationContext,
            _bindRoadGameplayFeatures,
            _buildingRuntimeCitySpawn,
            _buildingRuntimeCitySpawnContext,
            _buildingPlacementInteraction,
            _buildingPlacementInteractionContext,
            _bindBuildingGameplayFeatures,
            MainMenu,
            SelectionUiCamera,
            SelectionBuildingInteraction,
            _citizenPopulationEventSystem,
            _runtimeCityRoot,
            _runtimeBlockerRoot,
            DecorationRoot,
            DecorationCombinedMeshBaker,
            MatchScene != null ? MatchScene.RuntimeGridDebugViews : null,
            _gameplaySceneBindingSystem);

        RuntimeCity = gameplaySystems.RuntimeCity;
        RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers;
        RuntimeDecorations = gameplaySystems.RuntimeDecorations;
        GameplayInitialized = true;
        ApplyMainMenuFeatureBindingsIfReady();
    }

}
