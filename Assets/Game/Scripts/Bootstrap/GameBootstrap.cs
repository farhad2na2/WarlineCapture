using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Game.Scripts.UI;

[DisallowMultipleComponent]
public sealed class GameBootstrap : MonoBehaviour
{
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCameraReferenceSystem _runtimeCameraReferenceSystem = new();
    private readonly AIStartupSystem _aiStartupSystem = new();
    private readonly MissionStartupSystem _missionStartupSystem = new();
    private readonly PerformanceDiagnosticsSystem _performanceDiagnosticsSystem = new();
    private readonly InitialFactionSpawnCellSystem _initialFactionSpawnCellSystem = new();
    private readonly GameplaySceneBindingSystem _gameplaySceneBindingSystem = new();
    private readonly RuntimeRootSystem _runtimeRootSystem = new();
    private readonly ManagedGameplayStartupSystem _managedGameplayStartupSystem = new();
    private readonly MenuStartupSystem _menuStartupSystem = new();
    private readonly GameplayFeatureStartupSystem _gameplayFeatureStartupSystem = new();
    private readonly GameplayRuntimeUpdateSystem _gameplayRuntimeUpdateSystem = new();
    private readonly RuntimeGridBootstrapSystem _runtimeGridBootstrapSystem = new();
    private readonly SkirmishRuntimeConfigBootstrapSystem _skirmishRuntimeConfigBootstrapSystem = new();

    [Header("Scene Refs")]
    [SerializeField] private MenuView menuView;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
    [SerializeField] private Transform decorationRoot;
    [SerializeField] private GameObject[] legacyVisualRootsDisabledForM01 = Array.Empty<GameObject>();

    [Header("Configs")]
    [SerializeField] private RTSSelectionSystemConfig rtsSelectionConfig;
    [SerializeField] private RoadBuildSystemConfig roadBuildConfig;
    [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;
    [SerializeField] private UnitAttackTraceSystemConfig unitAttackTraceConfig;
    [SerializeField] private RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig;
    [SerializeField] private RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig;
    [SerializeField] private RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig;
    [SerializeField] private DayNightSystemConfig dayNightConfig;
    [SerializeField] private FactionVisualSettingsConfig factionVisualConfig;
    [SerializeField] private GameStringsConfig gameStringsConfig;
    [SerializeField] private PrefabPreviewCameraConfig prefabPreviewCameraConfig;
    [SerializeField] private AIPlanEntryStartupConfig aiPlanEntryConfig;
    [SerializeField] private List<AIControllerConfig> aiControllerConfigs = new();

    [Header("Runtime Grid")]
    [SerializeField] private int runtimeGridWidth = 2048;
    [SerializeField] private int runtimeGridHeight = 2048;
    [SerializeField] private float runtimeGridCellSize = 1f;
    [SerializeField] private Vector3 runtimeGridOrigin = Vector3.zero;

    public Camera WorldCamera => worldCamera;
    public Light DirectionalLight => directionalLight;
    public Volume GlobalVolume => globalVolume;
    public CombinedMeshBaker DecorationCombinedMeshBaker => decorationCombinedMeshBaker;
    public Transform DecorationRoot => decorationRoot != null ? decorationRoot : (decorationCombinedMeshBaker != null ? decorationCombinedMeshBaker.transform : null);

    public RTSSelectionSystemConfig RtsSelectionConfig => rtsSelectionConfig;
    public RoadBuildSystemConfig RoadBuildConfig => roadBuildConfig;
    public BuildingPlacementSystemConfig BuildingPlacementConfig => buildingPlacementConfig;
    public UnitAttackTraceSystemConfig UnitAttackTraceConfig => unitAttackTraceConfig;
    public RuntimeCitySpawnerSystemConfig RuntimeCitySpawnerConfig => runtimeCitySpawnerConfig;
    public RuntimeDecorationSpawnerSystemConfig RuntimeDecorationSpawnerConfig => runtimeDecorationSpawnerConfig;
    public RuntimeGridBlockerSystemConfig RuntimeGridBlockerConfig => runtimeGridBlockerConfig;
    public DayNightSystemConfig DayNightConfig => dayNightConfig;
    public GameStringsConfig GameStringsConfig => gameStringsConfig;
    public AIPlanEntryStartupConfig AIPlanEntryConfig => aiPlanEntryConfig;
    public IReadOnlyList<AIControllerConfig> AIControllerConfigs => aiControllerConfigs;

    public RuntimeGridBlockerSystem RuntimeGridBlockers { get; private set; }
    public RuntimeDecorationSpawnerSystem RuntimeDecorations { get; private set; }
    public RuntimeCityCompositionSystem RuntimeCity { get; private set; }
    public RoadBuildReadModelSystem RoadBuildReadModel { get; private set; }
    public BuildingSelectionClickSystem BuildingSelectionClick { get; private set; }
    public BuildingUiCommandSystem BuildingUiCommand { get; private set; }
    public BuildingUiQuerySystem BuildingUiQuery { get; private set; }
    public BuildingRuntimeUpdateSystem BuildingRuntimeUpdate { get; private set; }
    public SelectionUiCommandSystem SelectionUiCommand { get; private set; }
    public SelectionUiReadModelSystem SelectionUiReadModel { get; private set; }
    public SelectionUiCameraSystem SelectionUiCamera { get; private set; }
    public SelectionBuildingInteractionSystem SelectionBuildingInteraction { get; private set; }
    public SelectionScreenMarkerSystem SelectionScreenMarkers { get; private set; }
    public SelectionRectangleView SelectionRectangle { get; private set; }
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public UnitAttackTraceSystem UnitAttackTraces { get; private set; }
    public UnitImpostorRenderSystem UnitImpostors { get; private set; }
    public bool GameplayInitialized { get; private set; }
    public BuildingSelectionClickSystem.Context BuildingSelectionClickContext { get; private set; }
    private BuildingRuntimeCitySpawnSystem _buildingRuntimeCitySpawn;
    private BuildingRuntimeCitySpawnSystem.Context _buildingRuntimeCitySpawnContext;
    private BuildingUiCommandSystem.Context _buildingUiCommandContext;
    private BuildingUiQuerySystem.Context _buildingUiQueryContext;
    private BuildingPlacementInteractionSystem _buildingPlacementInteraction;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private RoadRuntimeGenerationSystem _roadRuntimeGeneration;
    private RoadRuntimeGenerationSystem.Context _roadRuntimeGenerationContext;
    private Action _roadRuntimeUpdate;
    private Action _roadOnGui;
    private Action _disposeRoad;
    private Action<MainMenuPlayUI> _bindRoadMainMenu;
    private Action<MainMenuPlayUI, RuntimeGridBlockerSystem> _bindRoadGameplayFeatures;
    private Action<MainMenuPlayUI> _bindBuildingMainMenu;
    private Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> _bindBuildingGameplayFeatures;
    private Action<MainMenuPlayUI> _bindSelectionMainMenu;
    private Action _selectionRuntimeUpdate;
    private Action _citizenPopulationRuntimeUpdate;
    private Action _disposeCitizenPopulation;
    private CitizenPopulationReadModelSystem _citizenPopulationReadModel;
    private CitizenPopulationEventSystem _citizenPopulationEventSystem;
    private Action _disposeSelection;
    private Action _disposeBuildingGameplay;
    private BuildingRuntimeUpdateSystem.Context _buildingRuntimeUpdateContext;
    private Entity _buildingRuntimeBoundaryEntity;
    private bool _gameplayStartPending;
    private Transform _runtimeBlockerRoot;
    private Transform _runtimeCityRoot;
    private Transform _runtimeUiRoot;

    private void Awake()
    {
        Application.runInBackground = true;
        _performanceDiagnosticsSystem.Initialize();

        _runtimeRootSystem.Ensure(transform, ref _runtimeBlockerRoot, ref _runtimeCityRoot, ref _runtimeUiRoot);

        ManagedGameplayStartupSystem.Result managedSystems = _managedGameplayStartupSystem.Initialize(
            dayNightConfig,
            factionVisualConfig,
            roadBuildConfig,
            buildingPlacementConfig,
            rtsSelectionConfig,
            unitAttackTraceConfig,
            gameStringsConfig,
            prefabPreviewCameraConfig,
            worldCamera,
            directionalLight,
            globalVolume,
            _runtimeUiRoot,
            gameObject.layer);

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
        _buildingPlacementInteraction = managedSystems.BuildingPlacementInteraction;
        _buildingPlacementInteractionContext = managedSystems.BuildingPlacementInteractionContext;
        _bindBuildingMainMenu = managedSystems.BindBuildingMainMenu;
        _bindBuildingGameplayFeatures = managedSystems.BindBuildingGameplayFeatures;
        _bindSelectionMainMenu = managedSystems.BindSelectionMainMenu;
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
        UnitAttackTraces = managedSystems.UnitAttackTraces;
        UnitImpostors = managedSystems.UnitImpostors;
        _disposeCitizenPopulation = managedSystems.DisposeCitizenPopulation;
        _citizenPopulationRuntimeUpdate = managedSystems.CitizenPopulationComposition != null
            ? managedSystems.CitizenPopulationComposition.RuntimeUpdateSystem.Update
            : null;
        _citizenPopulationReadModel = managedSystems.CitizenPopulationComposition?.ReadModel;
        _citizenPopulationEventSystem = managedSystems.CitizenPopulationComposition?.EventSystem;
        EnsureBuildingRuntimeBoundaryEntity();
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
    }

    private void Start()
    {
        MainMenu = _menuStartupSystem.Initialize(
            menuView,
            BeginGameplay,
            _bindRoadMainMenu,
            BuildingUiCommand,
            _buildingUiCommandContext,
            BuildingUiQuery,
            _buildingUiQueryContext,
            _buildingPlacementInteraction,
            _buildingPlacementInteractionContext,
            _bindBuildingMainMenu,
            _bindSelectionMainMenu,
            SelectionUiCommand,
            SelectionUiReadModel,
            SelectionUiCamera,
            SelectionScreenMarkers,
            DayNight,
            _citizenPopulationReadModel,
            worldCamera,
            _gameplaySceneBindingSystem,
            World.DefaultGameObjectInjectionWorld,
            Debug.LogException);
    }

    public void BeginGameplay()
    {
        GameRuntimeStats.Reset();
        _runtimeGridBootstrapSystem.Ensure(
            World.DefaultGameObjectInjectionWorld,
            runtimeGridWidth,
            runtimeGridHeight,
            runtimeGridCellSize,
            runtimeGridOrigin);
        _initialFactionSpawnCellSystem.Configure(
            World.DefaultGameObjectInjectionWorld,
            buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null);
        _aiStartupSystem.LogConfigValidation(aiControllerConfigs);
        if (WarlineCaptureMissionSession.HasActiveMission)
        {
            LogRuntimeEcsBootstrapState("beforeMissionInit");
            _missionStartupSystem.Initialize(
                World.DefaultGameObjectInjectionWorld,
                worldCamera,
                DayNight,
                legacyVisualRootsDisabledForM01);
            LogRuntimeEcsBootstrapState("afterMissionInit");
        }
        else
        {
            _missionStartupSystem.ApplySkirmishSceneDefaults(DayNight, legacyVisualRootsDisabledForM01);
            _skirmishRuntimeConfigBootstrapSystem.EnsureRuntimeConfigs(
                World.DefaultGameObjectInjectionWorld,
                buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null,
                buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);
            Debug.Log("[SkirmishStart] Mission startup skipped because no active mission session is set.");
            LogRuntimeEcsBootstrapState("skirmishMissionSkipped");
        }
        AIStartupSystem.Result aiStartupResult = _aiStartupSystem.Initialize(
            World.DefaultGameObjectInjectionWorld,
            aiControllerConfigs,
            aiPlanEntryConfig,
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell);
        if (aiStartupResult.HasPlayerAutoMode)
            _runtimeGameplayStateSystem.PlayerAutoModeEnabled = aiStartupResult.PlayerAutoModeEnabled;
        InitializeGameplaySystemsIfNeeded();
        _gameplayStartPending = true;
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
        _runtimeGameplayStateSystem.ResetForGameplayStart();
        _missionStartupSystem.FocusInitialCamera(
            World.DefaultGameObjectInjectionWorld,
            SelectionUiCamera,
            worldCamera,
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell,
            0);
    }

    private void Update()
    {
        _gameplayRuntimeUpdateSystem.Update(
            menuView,
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _missionStartupSystem,
            _roadRuntimeUpdate,
            BuildingRuntimeUpdate,
            _buildingRuntimeUpdateContext,
            _selectionRuntimeUpdate,
            worldCamera,
            RuntimeCity,
            RuntimeGridBlockers,
            RuntimeDecorations,
            DayNight,
            _citizenPopulationRuntimeUpdate,
            MainMenu,
            UnitImpostors,
            ref _gameplayStartPending);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _performanceDiagnosticsSystem.OnApplicationFocus(hasFocus);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _performanceDiagnosticsSystem.OnApplicationPause(pauseStatus);
    }

    private void LateUpdate()
    {
        _gameplayRuntimeUpdateSystem.LateUpdate(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            UnitAttackTraces,
            UnitImpostors);
    }

    private void OnGUI()
    {
        _gameplayRuntimeUpdateSystem.OnGui(
            GameplayInitialized,
            _runtimeGameplayStateSystem,
            _performanceDiagnosticsSystem,
            _roadOnGui,
            SelectionRectangle);
    }

    private void OnDestroy()
    {
        _menuStartupSystem.Shutdown(menuView, BeginGameplay);

        MainMenu?.Dispose();
        _disposeSelection?.Invoke();
        _disposeBuildingGameplay?.Invoke();
        _disposeRoad?.Invoke();
        UnitAttackTraces?.Dispose();
        UnitImpostors?.Dispose();
        _disposeCitizenPopulation?.Invoke();
        DayNight?.Dispose();
        RuntimeDecorations?.Dispose();
        RuntimeGridBlockers?.Dispose();
        RuntimeCity?.Dispose();
        MainMenu = null;
        SelectionUiCommand = null;
        SelectionUiReadModel = null;
        SelectionUiCamera = null;
        SelectionBuildingInteraction = null;
        SelectionScreenMarkers = null;
        SelectionRectangle = null;
        BuildingSelectionClick = null;
        BuildingSelectionClickContext = default;
        _buildingRuntimeCitySpawn = null;
        _buildingRuntimeCitySpawnContext = default;
        BuildingUiCommand = null;
        _buildingUiCommandContext = default;
        BuildingUiQuery = null;
        _buildingUiQueryContext = default;
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
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCity = null;
        _runtimeCameraReferenceSystem.ClearWorldCamera();
        _performanceDiagnosticsSystem.Dispose();
        SharedPrefabPreviewCache.ReleaseAll();
    }

    private void EnsureBuildingRuntimeBoundaryEntity()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        if (_buildingRuntimeBoundaryEntity == Entity.Null || !em.Exists(_buildingRuntimeBoundaryEntity))
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _buildingRuntimeBoundaryEntity = query.GetSingletonEntity();
            }
            else
            {
                _buildingRuntimeBoundaryEntity = em.CreateEntity();
                em.SetName(_buildingRuntimeBoundaryEntity, "BuildingRuntimeBoundaryEntity");
            }
        }

        EnsureBuildingRuntimeBoundaryBuffers(em, _buildingRuntimeBoundaryEntity);
    }

    private static void EnsureBuildingRuntimeBoundaryBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<BuildingRuntimeBoundaryTag>(entity))
            em.AddComponent<BuildingRuntimeBoundaryTag>(entity);
        EnsureBuffer<BuildingConfiguredSpawnableReadModel>(em, entity);
        EnsureBuffer<BuildingConfiguredUnitReadModel>(em, entity);
        EnsureBuffer<BuildingRuntimeFactionSummary>(em, entity);
        EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>(em, entity);
        EnsureBuffer<BuildingRuntimeUnitProductionSummary>(em, entity);
        EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>(em, entity);
        EnsureBuffer<BuildingFactionUnitProductionRequest>(em, entity);
        EnsureBuffer<BuildingFactionResourceSellRequest>(em, entity);
        EnsureBuffer<BuildingRuntimeSpawnRequest>(em, entity);
    }

    private static void EnsureBuffer<T>(EntityManager em, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(entity))
            em.AddBuffer<T>(entity);
    }

    private static void LogRuntimeEcsBootstrapState(string phase)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogWarning($"[AndroidVisualDiag] phase={phase} world=missing");
            return;
        }

        EntityManager em = world.EntityManager;
        using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery initialSpawnQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        using EntityQuery registryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitPrefabRegistryTag>());
        using EntityQuery prefabCandidateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
        using EntityQuery unitQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>());
        using EntityQuery modelQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitModelInstanceReference>());
        using EntityQuery sourceKeyQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using EntityQuery missionFallbackVisualQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<MissionRuntimeEntityId>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<Unity.Transforms.LocalTransform>(),
            ComponentType.Exclude<UnitModelInstanceReference>());
        string activeMissionId = WarlineCaptureMissionSession.ActiveMissionId;
        int hasActiveMission = WarlineCaptureMissionSession.HasActiveMission ? 1 : 0;
        int isFirstContactMission = activeMissionId == ChapterOneMissionCatalog.FirstContactMissionId ? 1 : 0;

        Debug.Log(
            $"[AndroidVisualDiag] phase={phase} " +
            $"activeMission={hasActiveMission} mission={activeMissionId} isM01={isFirstContactMission} " +
            $"gridConfigs={gridQuery.CalculateEntityCount()} initialSpawnConfigs={initialSpawnQuery.CalculateEntityCount()} " +
            $"unitRegistries={registryQuery.CalculateEntityCount()} prefabCandidates={prefabCandidateQuery.CalculateEntityCount()} " +
            $"units={unitQuery.CalculateEntityCount()} " +
            $"sourceKeys={sourceKeyQuery.CalculateEntityCount()} models={modelQuery.CalculateEntityCount()} " +
            $"missionFallbackVisuals={missionFallbackVisualQuery.CalculateEntityCount()}");
    }

    private void InitializeGameplaySystemsIfNeeded()
    {
        if (GameplayInitialized)
            return;

        GameplayFeatureStartupSystem.Result gameplaySystems = _gameplayFeatureStartupSystem.Initialize(
            runtimeCitySpawnerConfig,
            runtimeGridBlockerConfig,
            runtimeDecorationSpawnerConfig,
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
            decorationCombinedMeshBaker,
            _gameplaySceneBindingSystem);

        RuntimeCity = gameplaySystems.RuntimeCity;
        RuntimeGridBlockers = gameplaySystems.RuntimeGridBlockers;
        RuntimeDecorations = gameplaySystems.RuntimeDecorations;
        GameplayInitialized = true;
    }

}
