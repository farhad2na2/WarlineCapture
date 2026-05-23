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

    [Header("Scene Refs")]
    [SerializeField] private MenuView menuView;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private CombinedMeshBaker decorationCombinedMeshBaker;
    [SerializeField] private Transform decorationRoot;
    [SerializeField] private Chapter01MissionTacticalRuntimeBinder chapter01TacticalBinder;
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

    public Camera WorldCamera => worldCamera;
    public Light DirectionalLight => directionalLight;
    public Volume GlobalVolume => globalVolume;
    public CombinedMeshBaker DecorationCombinedMeshBaker => decorationCombinedMeshBaker;
    public Transform DecorationRoot => decorationRoot != null ? decorationRoot : (decorationCombinedMeshBaker != null ? decorationCombinedMeshBaker.transform : null);
    public Chapter01MissionTacticalRuntimeBinder Chapter01TacticalBinder => chapter01TacticalBinder;

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
    public RuntimeCitySpawnerSystem RuntimeCitySpawner { get; private set; }
    public RoadBuildSystem RoadBuild { get; private set; }
    public BuildingPlacementSystem BuildingPlacement { get; private set; }
    public RTSSelectionSystem Selection { get; private set; }
    public MainMenuPlayUI MainMenu { get; private set; }
    public DayNightSystem DayNight { get; private set; }
    public FactionVisualSettings FactionVisuals { get; private set; }
    public UnitAttackTraceSystem UnitAttackTraces { get; private set; }
    public UnitImpostorRenderSystem UnitImpostors { get; private set; }
    public CitizenPopulationSystem CitizenPopulation { get; private set; }
    public bool GameplayInitialized { get; private set; }
    private Entity _buildingPlacementRuntimeEntity;
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
        RoadBuild = managedSystems.RoadBuild;
        BuildingPlacement = managedSystems.BuildingPlacement;
        Selection = managedSystems.Selection;
        UnitAttackTraces = managedSystems.UnitAttackTraces;
        UnitImpostors = managedSystems.UnitImpostors;
        CitizenPopulation = managedSystems.CitizenPopulation;
        EnsureBuildingPlacementRuntimeComponent();
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
    }

    private void Start()
    {
        if (menuView != null)
            menuView.GameRequested += BeginGameplay;

        if (menuView != null)
        {
            menuView.Init(Selection, BuildingPlacement, worldCamera, DayNight, CitizenPopulation);
            menuView.NotifyBootstrapReady();
        }

        try
        {
            MainMenu = new MainMenuPlayUI();
            MainMenu.Init(RoadBuild, BuildingPlacement, Selection, DayNight);
            RoadBuild?.BindDependencies(BuildingPlacement, MainMenu);
            BuildingPlacement?.BindDependencies(RoadBuild, MainMenu, DayNight, Selection);
            Selection?.BindDependencies(MainMenu, RoadBuild, BuildingPlacement);
            _gameplaySceneBindingSystem.BindGameplayUiRuntimeDependencies(
                chapter01TacticalBinder,
                World.DefaultGameObjectInjectionWorld,
                Selection);
        }
        catch (Exception exception)
        {
            MainMenu = null;
            Debug.LogException(exception);
            RoadBuild?.BindDependencies(BuildingPlacement, null);
            BuildingPlacement?.BindDependencies(RoadBuild, null, DayNight, Selection);
            Selection?.BindDependencies(null, RoadBuild, BuildingPlacement);
            _gameplaySceneBindingSystem.BindGameplayUiRuntimeDependencies(
                chapter01TacticalBinder,
                World.DefaultGameObjectInjectionWorld,
                Selection);
        }
    }

    public void BeginGameplay()
    {
        GameRuntimeStats.Reset();
        _initialFactionSpawnCellSystem.Configure(
            World.DefaultGameObjectInjectionWorld,
            buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null);
        _aiStartupSystem.LogConfigValidation(aiControllerConfigs);
        _missionStartupSystem.Initialize(
            World.DefaultGameObjectInjectionWorld,
            chapter01TacticalBinder,
            worldCamera,
            DayNight,
            legacyVisualRootsDisabledForM01);
        AIStartupSystem.Result aiStartupResult = _aiStartupSystem.Initialize(
            World.DefaultGameObjectInjectionWorld,
            aiControllerConfigs,
            aiPlanEntryConfig,
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell);
        if (aiStartupResult.HasPlayerAutoMode)
            _runtimeGameplayStateSystem.PlayerAutoModeEnabled = aiStartupResult.PlayerAutoModeEnabled;
        EnsureGameplaySystemsInitialized();
        _gameplayStartPending = true;
        _runtimeCameraReferenceSystem.SetWorldCamera(worldCamera);
        _runtimeGameplayStateSystem.ResetForGameplayStart();
        _missionStartupSystem.FocusInitialCamera(
            World.DefaultGameObjectInjectionWorld,
            Selection,
            worldCamera,
            GetMapLoader(),
            _initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell,
            0);
    }

    private TacticalMapRuntimeLoader GetMapLoader()
    {
        return chapter01TacticalBinder != null ? chapter01TacticalBinder.TacticalMapLoader : null;
    }

    private void Update()
    {
        bool gameplayActive = GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested;
        _performanceDiagnosticsSystem.BeginUpdate(gameplayActive);
        bool hadSlowStep = false;

        double stepStart = _performanceDiagnosticsSystem.BeginStep();
        menuView?.SyncInputState();
        hadSlowStep |= _performanceDiagnosticsSystem.EndStep("MenuCanvasInput", stepStart);
        if (gameplayActive)
        {
            GameRuntimeStats.RecordMissionElapsed(Time.deltaTime);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            _missionStartupSystem.UpdateActiveMission(World.DefaultGameObjectInjectionWorld, GetMapLoader());
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("MissionRuntime", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            RoadBuild?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("RoadBuild", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            BuildingPlacement?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("BuildingPlacement", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            Selection?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("Selection", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            _missionStartupSystem.ApplyM01ProductionCameraPoseIfActive(worldCamera, GetMapLoader());
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("MissionCamera", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            RuntimeCitySpawner?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("RuntimeCitySpawner", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            RuntimeGridBlockers?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("RuntimeGridBlockers", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            RuntimeDecorations?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("RuntimeDecorations", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            DayNight?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("DayNight", stepStart);

            stepStart = _performanceDiagnosticsSystem.BeginStep();
            CitizenPopulation?.Update();
            hadSlowStep |= _performanceDiagnosticsSystem.EndStep("CitizenPopulation", stepStart);
        }

        stepStart = _performanceDiagnosticsSystem.BeginStep();
        menuView?.SyncRuntimeState();
        hadSlowStep |= _performanceDiagnosticsSystem.EndStep("MenuCanvas", stepStart);

        stepStart = _performanceDiagnosticsSystem.BeginStep();
        MainMenu?.Update();
        hadSlowStep |= _performanceDiagnosticsSystem.EndStep("MainMenu", stepStart);

        if (_gameplayStartPending && IsGameplayStartComplete())
        {
            _gameplayStartPending = false;
            menuView?.NotifyGameplayReady();
        }

        if (gameplayActive)
            WarlineCaptureMatchResultFlow.TryCompleteActiveMissionFromLoadedScene();

        _performanceDiagnosticsSystem.EndUpdate(
            gameplayActive,
            hadSlowStep,
            menuView,
            UnitImpostors?.LastDrawnCount ?? 0,
            GameplayInitialized,
            _runtimeGameplayStateSystem.PlayRequested);
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
        if (!(GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = _performanceDiagnosticsSystem.BeginTimedSection();
        UnitAttackTraces?.LateUpdate();
        UnitImpostors?.LateUpdate();
        _performanceDiagnosticsSystem.EndLateUpdate(start, UnitImpostors?.LastDrawnCount ?? 0);
    }

    private void OnGUI()
    {
        if (!(GameplayInitialized && _runtimeGameplayStateSystem.PlayRequested))
            return;

        double start = _performanceDiagnosticsSystem.BeginTimedSection();
        RoadBuild?.OnGui();
        Selection?.OnGui();
        _performanceDiagnosticsSystem.EndOnGui(start);
    }

    private void OnDestroy()
    {
        if (menuView != null)
            menuView.GameRequested -= BeginGameplay;

        ClearBuildingPlacementRuntimeComponent();
        MainMenu?.Dispose();
        Selection?.Dispose();
        BuildingPlacement?.Dispose();
        RoadBuild?.Dispose();
        UnitAttackTraces?.Dispose();
        UnitImpostors?.Dispose();
        CitizenPopulation?.Dispose();
        DayNight?.Dispose();
        RuntimeDecorations?.Dispose();
        RuntimeGridBlockers?.Dispose();
        RuntimeCitySpawner?.Dispose();
        MainMenu = null;
        Selection = null;
        BuildingPlacement = null;
        RoadBuild = null;
        FactionVisuals = null;
        UnitAttackTraces = null;
        UnitImpostors = null;
        CitizenPopulation = null;
        DayNight = null;
        RuntimeDecorations = null;
        RuntimeGridBlockers = null;
        RuntimeCitySpawner = null;
        _runtimeCameraReferenceSystem.ClearWorldCamera();
        _performanceDiagnosticsSystem.Dispose();
        SharedPrefabPreviewCache.ReleaseAll();
    }

    private void EnsureBuildingPlacementRuntimeComponent()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || BuildingPlacement == null)
            return;

        EntityManager em = world.EntityManager;
        if (_buildingPlacementRuntimeEntity == Entity.Null || !em.Exists(_buildingPlacementRuntimeEntity))
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _buildingPlacementRuntimeEntity = query.GetSingletonEntity();
            }
            else
            {
                _buildingPlacementRuntimeEntity = em.CreateEntity();
                em.SetName(_buildingPlacementRuntimeEntity, "BuildingPlacementRuntimeEntity");
                em.AddComponentObject(_buildingPlacementRuntimeEntity, new BuildingPlacementRuntimeComponent());
            }
        }

        BuildingPlacementRuntimeComponent component = em.GetComponentObject<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity);
        component.BuildingPlacement = BuildingPlacement;
    }

    private void ClearBuildingPlacementRuntimeComponent()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || _buildingPlacementRuntimeEntity == Entity.Null)
            return;

        EntityManager em = world.EntityManager;
        if (!em.Exists(_buildingPlacementRuntimeEntity) || !em.HasComponent<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity))
            return;

        BuildingPlacementRuntimeComponent component = em.GetComponentObject<BuildingPlacementRuntimeComponent>(_buildingPlacementRuntimeEntity);
        component.BuildingPlacement = null;
    }

    private void EnsureGameplaySystemsInitialized()
    {
        if (GameplayInitialized)
            return;

        RuntimeCitySpawner = new RuntimeCitySpawnerSystem();
        RuntimeCitySpawner.Init(runtimeCitySpawnerConfig, RoadBuild, BuildingPlacement, _runtimeCityRoot, MainMenu);

        RuntimeGridBlockers = new RuntimeGridBlockerSystem();
        RuntimeGridBlockers.Init(runtimeGridBlockerConfig, _runtimeBlockerRoot, RuntimeCitySpawner);
        RoadBuild?.BindDependencies(BuildingPlacement, MainMenu, RuntimeGridBlockers);
        _gameplaySceneBindingSystem.BindRuntimeGridBlockerDebugViews(RuntimeGridBlockers);
        BuildingPlacement?.BindDependencies(
            RoadBuild,
            MainMenu,
            DayNight,
            Selection,
            RuntimeGridBlockers,
            RuntimeCitySpawner,
            CitizenPopulation);

        RuntimeDecorations = new RuntimeDecorationSpawnerSystem();
        RuntimeDecorations.Init(runtimeDecorationSpawnerConfig, DecorationRoot, decorationCombinedMeshBaker, RuntimeCitySpawner, RuntimeGridBlockers);

        GameplayInitialized = true;
    }

    private bool IsGameplayStartComplete()
    {
        if (!GameplayInitialized || !_runtimeGameplayStateSystem.PlayRequested)
            return false;
        if (RuntimeCitySpawner != null && !RuntimeCitySpawner.HasSpawned)
            return false;
        if (RuntimeGridBlockers != null && !RuntimeGridBlockers.HasSpawned)
            return false;
        if (RuntimeDecorations != null && !RuntimeDecorations.HasSpawned)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        EntityQuery allSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        EntityQuery initializedSpawnConfigs = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

        int totalConfigCount = allSpawnConfigs.CalculateEntityCount();
        int initializedConfigCount = initializedSpawnConfigs.CalculateEntityCount();
        allSpawnConfigs.Dispose();
        initializedSpawnConfigs.Dispose();

        return totalConfigCount == 0 || initializedConfigCount >= totalConfigCount;
    }

}
