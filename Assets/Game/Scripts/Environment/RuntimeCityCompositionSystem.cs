using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed partial class RuntimeCityCompositionSystem : SystemBase
{
    private RuntimeCitySpawnerSystemConfig _config;
    private readonly List<GameObject> _fallbackCityPrefabs = new();
    private RuntimeCityConfigSystem _runtimeCityConfigSystem;
    private RuntimeCityConfigSystem.Snapshot _fallbackCityConfig;
    private RuntimeCityLayoutSystem _runtimeCityLayoutSystem;
    private readonly RuntimeCityLayoutState _fallbackRuntimeCityLayout = new();
    private RuntimeCityRoadLayoutSystem _runtimeCityRoadLayoutSystem;
    private readonly RuntimeCityRoadLayoutState _fallbackRuntimeCityRoadLayout = new();
    private RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem;
    private readonly RuntimeCityBuildingPlotState _fallbackRuntimeCityBuildingPlot = new();
    private RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem;
    private readonly RuntimeCityWalkabilityState _fallbackRuntimeCityWalkability = new();
    private RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem;
    private readonly RuntimeCityPrefabSelectionState _fallbackRuntimeCityPrefabSelection = new();
    private RuntimeCityBuildingSpawnContextSystem _runtimeCityBuildingSpawnContextSystem;
    private RuntimeCityBuildingPlacementSystem _runtimeCityBuildingPlacementSystem;
    private readonly RuntimeCityBuildingPlacementState _fallbackRuntimeCityBuildingPlacement = new();
    private RuntimeCityLandmarkOffsetSystem _runtimeCityLandmarkOffsetSystem;
    private readonly RuntimeCityLandmarkOffsetState _fallbackRuntimeCityLandmarkOffset = new();
    private RuntimeCityHallSpawnSystem _runtimeCityHallSpawnSystem;
    private readonly RuntimeCityHallSpawnState _fallbackRuntimeCityHallSpawn = new();
    private RuntimeCityLandmarkSpawnSystem _runtimeCityLandmarkSpawnSystem;
    private readonly RuntimeCityLandmarkSpawnState _fallbackRuntimeCityLandmarkSpawn = new();
    private RuntimeCityBulkPlotPlanSystem _runtimeCityBulkPlotPlanSystem;
    private readonly RuntimeCityBulkPlotPlanState _fallbackRuntimeCityBulkPlotPlan = new();
    private RuntimeCityEntryBuildingSpawnSystem _runtimeCityEntryBuildingSpawnSystem;
    private readonly RuntimeCityEntryBuildingSpawnState _fallbackRuntimeCityEntryBuildingSpawn = new();
    private RuntimeCityRoadsideBuildingSpawnSystem _runtimeCityRoadsideBuildingSpawnSystem;
    private readonly RuntimeCityRoadsideBuildingSpawnState _fallbackRuntimeCityRoadsideBuildingSpawn = new();
    private RuntimeCityRuralBuildingSpawnSystem _runtimeCityRuralBuildingSpawnSystem;
    private readonly RuntimeCityRuralBuildingSpawnState _fallbackRuntimeCityRuralBuildingSpawn = new();
    private RuntimeCityBulkBuildingSpawnRoutineSystem _runtimeCityBulkBuildingSpawnRoutineSystem;
    private readonly RuntimeCityBulkBuildingSpawnRoutineState _fallbackRuntimeCityBulkBuildingSpawnRoutine = new();
    private RuntimeCityCorridorBuildingSpawnSystem _runtimeCityCorridorBuildingSpawnSystem;
    private readonly RuntimeCityCorridorBuildingSpawnState _fallbackRuntimeCityCorridorBuildingSpawn = new();
    private RuntimeCityYardWallPlanSystem _runtimeCityYardWallPlanSystem;
    private readonly RuntimeCityYardWallPlanState _fallbackRuntimeCityYardWallPlan = new();
    private RuntimeCityYardGateSystem _runtimeCityYardGateSystem;
    private readonly RuntimeCityYardGateState _fallbackRuntimeCityYardGate = new();
    private RuntimeCityYardWallVisualSystem _runtimeCityYardWallVisualSystem;
    private readonly RuntimeCityYardWallVisualState _fallbackRuntimeCityYardWallVisual = new();
    private RuntimeCityHouseYardWallSystem _runtimeCityHouseYardWallSystem;
    private readonly RuntimeCityHouseYardWallState _fallbackRuntimeCityHouseYardWall = new();
    private RuntimeCityDecorationPrefabGroupSystem _runtimeCityDecorationPrefabGroupSystem;
    private readonly RuntimeCityDecorationPrefabGroupState _fallbackRuntimeCityDecorationPrefabGroup = new();
    private RuntimeCityClothCoverSpawnSystem _runtimeCityClothCoverSpawnSystem;
    private readonly RuntimeCityClothCoverSpawnState _fallbackRuntimeCityClothCoverSpawn = new();
    private RuntimeCityArchwaySpawnSystem _runtimeCityArchwaySpawnSystem;
    private readonly RuntimeCityArchwaySpawnState _fallbackRuntimeCityArchwaySpawn = new();
    private RuntimeCityFreeScatterDecorationSystem _runtimeCityFreeScatterDecorationSystem;
    private readonly RuntimeCityFreeScatterDecorationState _fallbackRuntimeCityFreeScatterDecoration = new();
    private RuntimeCityDecorationBuildingSpawnSystem _runtimeCityDecorationBuildingSpawnSystem;
    private readonly RuntimeCityDecorationBuildingSpawnState _fallbackRuntimeCityDecorationBuildingSpawn = new();
    private RuntimeCityVisualSystem _runtimeCityVisualSystem;
    private RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem;
    private readonly RuntimeCitySpawnBridgeState _fallbackRuntimeCitySpawnBridge = new();
    private RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem;
    private readonly RuntimeCityRoadBuildBridgeState _fallbackRuntimeCityRoadBuildBridge = new();
    private RuntimeCityLifecycleSystem _runtimeCityLifecycleSystem;
    private readonly RuntimeCityLifecycleState _fallbackRuntimeCityLifecycle = new();
    private RuntimeCityStartupSystem _runtimeCityStartupSystem;
    private readonly RuntimeCityStartupState _fallbackRuntimeCityStartup = new();
    private RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem;
    private RuntimeCityGenerationSystem _runtimeCityGenerationSystem;
    private readonly RuntimeCityGenerationState _fallbackRuntimeCityGeneration = new();
    private RuntimeCityChainSystem _runtimeCityChainSystem;
    private readonly RuntimeCityChainState _fallbackRuntimeCityChain = new();
    private RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem;
    private readonly RuntimeCityRoadCommitState _fallbackRuntimeCityRoadCommit = new();
    private RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem;
    private RuntimeCityIngressSystem _runtimeCityIngressSystem;
    private readonly RuntimeCityIngressState _fallbackRuntimeCityIngress = new();
    private RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem;
    private RuntimeCityReadModelSystem _runtimeCityReadModelSystem;
    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCityStartupSystem.TryGetPendingInitialUnitsDelegate _tryGetPendingInitialUnits;
    private readonly RuntimeCityStartupSystem.TryGetRoadCellSizeDelegate _tryGetRoadCellSize;
    private readonly RuntimeCityStartupSystem.TryGetGridDataDelegate _tryGetGridData;
    private RuntimeCityBuildingSpawnContextSystem.Context _runtimeCityBuildingSpawnContext;
    private bool _configured;

    private RuntimeCityConfigSystem.Snapshot cityConfig => RuntimeCityConfigSystem?.Current ?? _fallbackCityConfig;
    private bool spawnOnStart => cityConfig.SpawnOnStart;
    private bool generateBuildings => cityConfig.GenerateBuildings;
    private int cityCount => cityConfig.CityCount;
    private int generationYieldInterval => cityConfig.GenerationYieldInterval;
    private List<GameObject> hallPrefabs => cityConfig.HallPrefabs;
    private List<GameObject> shopPrefabs => cityConfig.ShopPrefabs;
    private List<GameObject> housePrefabs => cityConfig.HousePrefabs;

    public bool SpawnOnStartEnabled => spawnOnStart;
    public bool HasSpawned => RuntimeCityLifecycleState.HasSpawned(cityCount);
    public bool IsGenerating => RuntimeCityLifecycleState.IsGenerating;
    public RuntimeCityReadModelSystem ReadModel => RuntimeCityReadModelSystem;

    public RuntimeCityCompositionSystem()
    {
        _fallbackCityConfig = global::RuntimeCityConfigSystem.Snapshot.Default(_fallbackCityPrefabs);
        _tryGetPendingInitialUnits = TryGetPendingInitialUnits;
        _tryGetRoadCellSize = TryGetRoadCellSizeInGridCells;
        _tryGetGridData = TryGetGridConfig;
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        Dispose();
    }

    public string DescribeStartupBlocker(int frameCount)
    {
        return global::RuntimeCityStartupSystem.DescribeStartupBlocker(CreateStartupContext(frameCount));
    }

    public void MarkSpawnedAfterLoadingGateTimeout()
    {
        RuntimeCityLifecycleState.MarkSpawned();
        PublishReadModel();
    }

    internal void Configure(
        RuntimeCitySpawnerSystemConfig configAsset,
        RoadRuntimeGenerationSystem roadRuntimeGenerationSystem,
        RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
        BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawnSystem,
        BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
        Transform runtimeRoot,
        IMatchRuntimeUi mainMenuPlayUi)
    {
        _configured = true;
        _config = configAsset;
        RuntimeCityRoadBuildBridgeState.Configure(roadRuntimeGenerationSystem, roadRuntimeGenerationContext);
        RuntimeCitySpawnBridgeState.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
        RuntimeCityVisualSystem?.SetRuntimeRoot(runtimeRoot);
        RuntimeCityMinimapEventSystem?.Configure(mainMenuPlayUi);
        ApplyConfigIfAvailable();
        PublishReadModel();
    }

    public void ConfigureForValidation(RuntimeCitySpawnerSystemConfig configAsset)
    {
        Configure(configAsset, null, default, null, default, null, null);
    }

    public void Update(int frameCount)
    {
        ApplyConfigIfAvailable();
        RuntimeCityLifecycleState.Tick(CreateLifecycleContext(frameCount));
        RuntimeCityMinimapEventSystem?.Flush();
        TryAutoSpawn(frameCount);
        PublishReadModel();
    }

    public void Dispose()
    {
        if (!_configured)
            return;

        (_runtimeCityLifecycleSystem?.State ?? _fallbackRuntimeCityLifecycle).CancelGeneration();
        _runtimeCityVisualSystem?.Dispose();
        (_runtimeCitySpawnBridgeSystem?.State ?? _fallbackRuntimeCitySpawnBridge).Clear();
        (_runtimeCityRoadBuildBridgeSystem?.State ?? _fallbackRuntimeCityRoadBuildBridge).Clear();
        _runtimeCityReadinessQuerySystem?.Clear();
        _runtimeCityMinimapEventSystem?.Clear();
        _configured = false;
    }

    public bool IsConfiguredHousePrefab(GameObject prefab)
    {
        return RuntimeCityPrefabSelectionState.IsConfiguredPrefab(prefab, housePrefabs);
    }

    public void GenerateCity(int frameCount)
    {
        RuntimeCityStartupSystem.Result result = EvaluateManualGeneration(CreateStartupContext(frameCount));
        if (result.Kind == RuntimeCityStartupSystem.ResultKind.Generate)
            GenerateCity(result.Grid, result.RoadCellSizeInGridCells, frameCount);
        PublishReadModel();
    }

    private void ApplyConfigIfAvailable()
    {
        RuntimeCityConfigSystem configSystem = RuntimeCityConfigSystem;
        if (configSystem != null)
            configSystem.Apply(_config);
        else
            _fallbackCityConfig = global::RuntimeCityConfigSystem.Snapshot.From(_config, _fallbackCityPrefabs);

        RuntimeCityBuildingSpawnContextSystem spawnContextSystem = RuntimeCityBuildingSpawnContextSystem;
        _runtimeCityBuildingSpawnContext = spawnContextSystem != null
            ? spawnContextSystem.Create(
                cityConfig,
                RuntimeCityBuildingPlotState,
                RuntimeCityWalkabilityState,
                RuntimeCityPrefabSelectionState,
                RuntimeCityVisualSystem,
                RuntimeCitySpawnBridgeState,
                RuntimeCityDiagnosticSystem)
            : global::RuntimeCityBuildingSpawnContextSystem.CreateFallback(
                cityConfig,
                RuntimeCityBuildingPlotState,
                RuntimeCityWalkabilityState,
                RuntimeCityPrefabSelectionState,
                RuntimeCityVisualSystem,
                RuntimeCitySpawnBridgeState,
                RuntimeCityDiagnosticSystem);
    }

    private void TryAutoSpawn(int frameCount)
    {
        RuntimeCityStartupSystem.Result result = EvaluateStartup(CreateStartupContext(frameCount));
        if (result.Kind == RuntimeCityStartupSystem.ResultKind.MarkSpawned)
            RuntimeCityLifecycleState.MarkSpawned();
        else if (result.Kind == RuntimeCityStartupSystem.ResultKind.Generate)
            GenerateCity(result.Grid, result.RoadCellSizeInGridCells, frameCount);
    }

    private RuntimeCityStartupSystem.Result EvaluateStartup(RuntimeCityStartupSystem.Context context)
    {
        RuntimeCityStartupSystem startupSystem = RuntimeCityStartupSystem;
        return startupSystem != null
            ? startupSystem.Evaluate(context)
            : _fallbackRuntimeCityStartup.Evaluate(context);
    }

    private RuntimeCityStartupSystem.Result EvaluateManualGeneration(RuntimeCityStartupSystem.Context context)
    {
        RuntimeCityStartupSystem startupSystem = RuntimeCityStartupSystem;
        return startupSystem != null
            ? startupSystem.EvaluateManualGeneration(context)
            : _fallbackRuntimeCityStartup.EvaluateManualGeneration(context);
    }

    private void PublishReadModel()
    {
        RuntimeCityReadModelSystem?.Publish(SpawnOnStartEnabled, HasSpawned, IsGenerating);
    }

    private void GenerateCity(GridConfig grid, int roadCellSizeInGridCells, int frameCount)
    {
        RuntimeCityGenerationState.TryBegin(CreateGenerationContext(grid, roadCellSizeInGridCells, frameCount));
    }

    private bool ShouldYield(int completedWorkItems)
    {
        return RuntimeCityLifecycleState.ShouldYield(completedWorkItems, generationYieldInterval);
    }

    private RuntimeCityLifecycleSystem.Context CreateLifecycleContext(int frameCount)
    {
        return new RuntimeCityLifecycleSystem.Context(
            frameCount,
            cityCount,
            generateBuildings,
            generationYieldInterval,
            RuntimeCityDiagnosticSystem);
    }

    private RuntimeCityStartupSystem.Context CreateStartupContext(int frameCount)
    {
        return new RuntimeCityStartupSystem.Context(
            frameCount,
            spawnOnStart,
            RuntimeCityLifecycleState.IsSpawned,
            cityCount,
            _runtimeGameplayStateSystem.PlayRequested,
            false,
            generateBuildings,
            RuntimeCityRoadBuildBridgeState.HasRoadRuntimeGenerationSystem,
            RuntimeCitySpawnBridgeState.HasSpawnSystem,
            hallPrefabs,
            shopPrefabs,
            housePrefabs,
            _tryGetPendingInitialUnits,
            _tryGetRoadCellSize,
            _tryGetGridData,
            RuntimeCityDiagnosticSystem);
    }

    private RuntimeCityGenerationSystem.Context CreateGenerationContext(GridConfig grid, int roadCellSizeInGridCells, int frameCount)
    {
        return new RuntimeCityGenerationSystem.Context(
            cityConfig,
            grid,
            roadCellSizeInGridCells,
            RuntimeCityLifecycleState,
            CreateLifecycleContext(frameCount),
            RuntimeCityLayoutState,
            RuntimeCityWalkabilityState,
            CreateBuildingSpawnSystems(),
            _runtimeCityBuildingSpawnContext,
            RuntimeCityBuildingPlacementState,
            RuntimeCityCorridorBuildingSpawnState,
            RuntimeCityRoadBuildBridgeState,
            RuntimeCitySpawnBridgeState,
            RuntimeCityChainState,
            CreateChainContext(),
            RuntimeCityRoadCommitState,
            CreateRoadCommitContext(),
            RuntimeCityIngressState,
            CreateIngressContext(),
            CollectInitialBaseExclusionRoadRects,
            ShouldYield,
            RuntimeCityMinimapEventSystem,
            RuntimeCityDiagnosticSystem);
    }

    private RuntimeCityBuildingSpawnContextSystem.Systems CreateBuildingSpawnSystems()
    {
        return new RuntimeCityBuildingSpawnContextSystem.Systems(
            RuntimeCityBuildingPlacementState,
            RuntimeCityLandmarkOffsetState,
            RuntimeCityHallSpawnState,
            RuntimeCityLandmarkSpawnState,
            RuntimeCityBulkPlotPlanState,
            RuntimeCityEntryBuildingSpawnState,
            RuntimeCityRoadsideBuildingSpawnState,
            RuntimeCityRuralBuildingSpawnState,
            RuntimeCityBulkBuildingSpawnRoutineState,
            RuntimeCityCorridorBuildingSpawnState,
            RuntimeCityYardWallPlanState,
            RuntimeCityYardGateState,
            RuntimeCityYardWallVisualState,
            RuntimeCityHouseYardWallState,
            RuntimeCityDecorationPrefabGroupState,
            RuntimeCityClothCoverSpawnState,
            RuntimeCityArchwaySpawnState,
            RuntimeCityFreeScatterDecorationState,
            RuntimeCityDecorationBuildingSpawnState);
    }

    private RuntimeCityChainSystem.Context CreateChainContext()
    {
        return new RuntimeCityChainSystem.Context(
            cityConfig,
            RuntimeCityLayoutState,
            RuntimeCityRoadLayoutState,
            RuntimeCityPrefabSelectionState,
            RuntimeCityRoadCommitState,
            RuntimeCityIngressState,
            CreateIngressContext());
    }

    private RuntimeCityRoadCommitSystem.Context CreateRoadCommitContext()
    {
        return new RuntimeCityRoadCommitSystem.Context(
            RuntimeCityRoadBuildBridgeState,
            RuntimeCityDiagnosticSystem);
    }

    private RuntimeCityIngressSystem.Context CreateIngressContext()
    {
        return new RuntimeCityIngressSystem.Context(
            cityConfig,
            RuntimeCityRoadLayoutState);
    }

    private RuntimeCityVisualSystem RuntimeCityVisualSystem =>
        _runtimeCityVisualSystem ??= ResolveRuntimeCityVisualSystem();

    private RuntimeCityMinimapEventSystem RuntimeCityMinimapEventSystem =>
        _runtimeCityMinimapEventSystem ??= ResolveRuntimeCityMinimapEventSystem();

    private RuntimeCityReadinessQuerySystem RuntimeCityReadinessQuerySystem =>
        _runtimeCityReadinessQuerySystem ??= ResolveRuntimeCityReadinessQuerySystem();

    private RuntimeCityReadModelSystem RuntimeCityReadModelSystem =>
        _runtimeCityReadModelSystem ??= ResolveRuntimeCityReadModelSystem();

    private RuntimeCityConfigSystem RuntimeCityConfigSystem =>
        _runtimeCityConfigSystem ??= ResolveRuntimeCityConfigSystem();

    private RuntimeCityDiagnosticSystem RuntimeCityDiagnosticSystem =>
        _runtimeCityDiagnosticSystem ??= ResolveRuntimeCityDiagnosticSystem();

    private RuntimeCityBuildingSpawnContextSystem RuntimeCityBuildingSpawnContextSystem =>
        _runtimeCityBuildingSpawnContextSystem ??= ResolveRuntimeCityBuildingSpawnContextSystem();

    private RuntimeCityStartupSystem RuntimeCityStartupSystem =>
        _runtimeCityStartupSystem ??= ResolveRuntimeCityStartupSystem();

    private RuntimeCityLifecycleState RuntimeCityLifecycleState =>
        RuntimeCityLifecycleSystem?.State ?? _fallbackRuntimeCityLifecycle;

    private RuntimeCityLifecycleSystem RuntimeCityLifecycleSystem =>
        _runtimeCityLifecycleSystem ??= ResolveRuntimeCityLifecycleSystem();

    private RuntimeCityLayoutState RuntimeCityLayoutState =>
        RuntimeCityLayoutSystem?.State ?? _fallbackRuntimeCityLayout;

    private RuntimeCityLayoutSystem RuntimeCityLayoutSystem =>
        _runtimeCityLayoutSystem ??= ResolveRuntimeCityLayoutSystem();

    private RuntimeCityRoadLayoutState RuntimeCityRoadLayoutState =>
        RuntimeCityRoadLayoutSystem?.State ?? _fallbackRuntimeCityRoadLayout;

    private RuntimeCityRoadLayoutSystem RuntimeCityRoadLayoutSystem =>
        _runtimeCityRoadLayoutSystem ??= ResolveRuntimeCityRoadLayoutSystem();

    private RuntimeCityWalkabilityState RuntimeCityWalkabilityState =>
        RuntimeCityWalkabilitySystem?.State ?? _fallbackRuntimeCityWalkability;

    private RuntimeCityWalkabilitySystem RuntimeCityWalkabilitySystem =>
        _runtimeCityWalkabilitySystem ??= ResolveRuntimeCityWalkabilitySystem();

    private RuntimeCityBuildingPlotState RuntimeCityBuildingPlotState =>
        RuntimeCityBuildingPlotSystem?.State ?? _fallbackRuntimeCityBuildingPlot;

    private RuntimeCityBuildingPlotSystem RuntimeCityBuildingPlotSystem =>
        _runtimeCityBuildingPlotSystem ??= ResolveRuntimeCityBuildingPlotSystem();

    private RuntimeCityBulkPlotPlanState RuntimeCityBulkPlotPlanState =>
        RuntimeCityBulkPlotPlanSystem?.State ?? _fallbackRuntimeCityBulkPlotPlan;

    private RuntimeCityBulkPlotPlanSystem RuntimeCityBulkPlotPlanSystem =>
        _runtimeCityBulkPlotPlanSystem ??= ResolveRuntimeCityBulkPlotPlanSystem();

    private RuntimeCityPrefabSelectionState RuntimeCityPrefabSelectionState =>
        RuntimeCityPrefabSelectionSystem?.State ?? _fallbackRuntimeCityPrefabSelection;

    private RuntimeCityPrefabSelectionSystem RuntimeCityPrefabSelectionSystem =>
        _runtimeCityPrefabSelectionSystem ??= ResolveRuntimeCityPrefabSelectionSystem();

    private RuntimeCityLandmarkOffsetState RuntimeCityLandmarkOffsetState =>
        RuntimeCityLandmarkOffsetSystem?.State ?? _fallbackRuntimeCityLandmarkOffset;

    private RuntimeCityLandmarkOffsetSystem RuntimeCityLandmarkOffsetSystem =>
        _runtimeCityLandmarkOffsetSystem ??= ResolveRuntimeCityLandmarkOffsetSystem();

    private RuntimeCityHallSpawnState RuntimeCityHallSpawnState =>
        RuntimeCityHallSpawnSystem?.State ?? _fallbackRuntimeCityHallSpawn;

    private RuntimeCityHallSpawnSystem RuntimeCityHallSpawnSystem =>
        _runtimeCityHallSpawnSystem ??= ResolveRuntimeCityHallSpawnSystem();

    private RuntimeCityLandmarkSpawnState RuntimeCityLandmarkSpawnState =>
        RuntimeCityLandmarkSpawnSystem?.State ?? _fallbackRuntimeCityLandmarkSpawn;

    private RuntimeCityLandmarkSpawnSystem RuntimeCityLandmarkSpawnSystem =>
        _runtimeCityLandmarkSpawnSystem ??= ResolveRuntimeCityLandmarkSpawnSystem();

    private RuntimeCityEntryBuildingSpawnState RuntimeCityEntryBuildingSpawnState =>
        RuntimeCityEntryBuildingSpawnSystem?.State ?? _fallbackRuntimeCityEntryBuildingSpawn;

    private RuntimeCityEntryBuildingSpawnSystem RuntimeCityEntryBuildingSpawnSystem =>
        _runtimeCityEntryBuildingSpawnSystem ??= ResolveRuntimeCityEntryBuildingSpawnSystem();

    private RuntimeCityRoadsideBuildingSpawnState RuntimeCityRoadsideBuildingSpawnState =>
        RuntimeCityRoadsideBuildingSpawnSystem?.State ?? _fallbackRuntimeCityRoadsideBuildingSpawn;

    private RuntimeCityRoadsideBuildingSpawnSystem RuntimeCityRoadsideBuildingSpawnSystem =>
        _runtimeCityRoadsideBuildingSpawnSystem ??= ResolveRuntimeCityRoadsideBuildingSpawnSystem();

    private RuntimeCityRuralBuildingSpawnState RuntimeCityRuralBuildingSpawnState =>
        RuntimeCityRuralBuildingSpawnSystem?.State ?? _fallbackRuntimeCityRuralBuildingSpawn;

    private RuntimeCityRuralBuildingSpawnSystem RuntimeCityRuralBuildingSpawnSystem =>
        _runtimeCityRuralBuildingSpawnSystem ??= ResolveRuntimeCityRuralBuildingSpawnSystem();

    private RuntimeCityBulkBuildingSpawnRoutineState RuntimeCityBulkBuildingSpawnRoutineState =>
        RuntimeCityBulkBuildingSpawnRoutineSystem?.State ?? _fallbackRuntimeCityBulkBuildingSpawnRoutine;

    private RuntimeCityBulkBuildingSpawnRoutineSystem RuntimeCityBulkBuildingSpawnRoutineSystem =>
        _runtimeCityBulkBuildingSpawnRoutineSystem ??= ResolveRuntimeCityBulkBuildingSpawnRoutineSystem();

    private RuntimeCityCorridorBuildingSpawnState RuntimeCityCorridorBuildingSpawnState =>
        RuntimeCityCorridorBuildingSpawnSystem?.State ?? _fallbackRuntimeCityCorridorBuildingSpawn;

    private RuntimeCityCorridorBuildingSpawnSystem RuntimeCityCorridorBuildingSpawnSystem =>
        _runtimeCityCorridorBuildingSpawnSystem ??= ResolveRuntimeCityCorridorBuildingSpawnSystem();

    private RuntimeCityYardWallPlanState RuntimeCityYardWallPlanState =>
        RuntimeCityYardWallPlanSystem?.State ?? _fallbackRuntimeCityYardWallPlan;

    private RuntimeCityYardWallPlanSystem RuntimeCityYardWallPlanSystem =>
        _runtimeCityYardWallPlanSystem ??= ResolveRuntimeCityYardWallPlanSystem();

    private RuntimeCityYardGateState RuntimeCityYardGateState =>
        RuntimeCityYardGateSystem?.State ?? _fallbackRuntimeCityYardGate;

    private RuntimeCityYardGateSystem RuntimeCityYardGateSystem =>
        _runtimeCityYardGateSystem ??= ResolveRuntimeCityYardGateSystem();

    private RuntimeCityYardWallVisualState RuntimeCityYardWallVisualState =>
        RuntimeCityYardWallVisualSystem?.State ?? _fallbackRuntimeCityYardWallVisual;

    private RuntimeCityYardWallVisualSystem RuntimeCityYardWallVisualSystem =>
        _runtimeCityYardWallVisualSystem ??= ResolveRuntimeCityYardWallVisualSystem();

    private RuntimeCityHouseYardWallState RuntimeCityHouseYardWallState =>
        RuntimeCityHouseYardWallSystem?.State ?? _fallbackRuntimeCityHouseYardWall;

    private RuntimeCityHouseYardWallSystem RuntimeCityHouseYardWallSystem =>
        _runtimeCityHouseYardWallSystem ??= ResolveRuntimeCityHouseYardWallSystem();

    private RuntimeCityDecorationPrefabGroupState RuntimeCityDecorationPrefabGroupState =>
        RuntimeCityDecorationPrefabGroupSystem?.State ?? _fallbackRuntimeCityDecorationPrefabGroup;

    private RuntimeCityDecorationPrefabGroupSystem RuntimeCityDecorationPrefabGroupSystem =>
        _runtimeCityDecorationPrefabGroupSystem ??= ResolveRuntimeCityDecorationPrefabGroupSystem();

    private RuntimeCityClothCoverSpawnState RuntimeCityClothCoverSpawnState =>
        RuntimeCityClothCoverSpawnSystem?.State ?? _fallbackRuntimeCityClothCoverSpawn;

    private RuntimeCityClothCoverSpawnSystem RuntimeCityClothCoverSpawnSystem =>
        _runtimeCityClothCoverSpawnSystem ??= ResolveRuntimeCityClothCoverSpawnSystem();

    private RuntimeCityArchwaySpawnState RuntimeCityArchwaySpawnState =>
        RuntimeCityArchwaySpawnSystem?.State ?? _fallbackRuntimeCityArchwaySpawn;

    private RuntimeCityArchwaySpawnSystem RuntimeCityArchwaySpawnSystem =>
        _runtimeCityArchwaySpawnSystem ??= ResolveRuntimeCityArchwaySpawnSystem();

    private RuntimeCityFreeScatterDecorationState RuntimeCityFreeScatterDecorationState =>
        RuntimeCityFreeScatterDecorationSystem?.State ?? _fallbackRuntimeCityFreeScatterDecoration;

    private RuntimeCityFreeScatterDecorationSystem RuntimeCityFreeScatterDecorationSystem =>
        _runtimeCityFreeScatterDecorationSystem ??= ResolveRuntimeCityFreeScatterDecorationSystem();

    private RuntimeCityDecorationBuildingSpawnState RuntimeCityDecorationBuildingSpawnState =>
        RuntimeCityDecorationBuildingSpawnSystem?.State ?? _fallbackRuntimeCityDecorationBuildingSpawn;

    private RuntimeCityDecorationBuildingSpawnSystem RuntimeCityDecorationBuildingSpawnSystem =>
        _runtimeCityDecorationBuildingSpawnSystem ??= ResolveRuntimeCityDecorationBuildingSpawnSystem();

    private RuntimeCityBuildingPlacementState RuntimeCityBuildingPlacementState =>
        RuntimeCityBuildingPlacementSystem?.State ?? _fallbackRuntimeCityBuildingPlacement;

    private RuntimeCityBuildingPlacementSystem RuntimeCityBuildingPlacementSystem =>
        _runtimeCityBuildingPlacementSystem ??= ResolveRuntimeCityBuildingPlacementSystem();

    private RuntimeCitySpawnBridgeState RuntimeCitySpawnBridgeState =>
        RuntimeCitySpawnBridgeSystem?.State ?? _fallbackRuntimeCitySpawnBridge;

    private RuntimeCitySpawnBridgeSystem RuntimeCitySpawnBridgeSystem =>
        _runtimeCitySpawnBridgeSystem ??= ResolveRuntimeCitySpawnBridgeSystem();

    private RuntimeCityRoadBuildBridgeState RuntimeCityRoadBuildBridgeState =>
        RuntimeCityRoadBuildBridgeSystem?.State ?? _fallbackRuntimeCityRoadBuildBridge;

    private RuntimeCityRoadBuildBridgeSystem RuntimeCityRoadBuildBridgeSystem =>
        _runtimeCityRoadBuildBridgeSystem ??= ResolveRuntimeCityRoadBuildBridgeSystem();

    private RuntimeCityGenerationState RuntimeCityGenerationState =>
        RuntimeCityGenerationSystem?.State ?? _fallbackRuntimeCityGeneration;

    private RuntimeCityGenerationSystem RuntimeCityGenerationSystem =>
        _runtimeCityGenerationSystem ??= ResolveRuntimeCityGenerationSystem();

    private RuntimeCityChainState RuntimeCityChainState =>
        RuntimeCityChainSystem?.State ?? _fallbackRuntimeCityChain;

    private RuntimeCityChainSystem RuntimeCityChainSystem =>
        _runtimeCityChainSystem ??= ResolveRuntimeCityChainSystem();

    private RuntimeCityRoadCommitState RuntimeCityRoadCommitState =>
        RuntimeCityRoadCommitSystem?.State ?? _fallbackRuntimeCityRoadCommit;

    private RuntimeCityRoadCommitSystem RuntimeCityRoadCommitSystem =>
        _runtimeCityRoadCommitSystem ??= ResolveRuntimeCityRoadCommitSystem();

    private RuntimeCityIngressState RuntimeCityIngressState =>
        RuntimeCityIngressSystem?.State ?? _fallbackRuntimeCityIngress;

    private RuntimeCityIngressSystem RuntimeCityIngressSystem =>
        _runtimeCityIngressSystem ??= ResolveRuntimeCityIngressSystem();

    private bool TryGetPendingInitialUnits(out int totalConfigs, out int initializedConfigs)
    {
        RuntimeCityReadinessQuerySystem readinessQuerySystem = RuntimeCityReadinessQuerySystem;
        if (readinessQuerySystem == null)
        {
            totalConfigs = 0;
            initializedConfigs = 0;
            return false;
        }

        return readinessQuerySystem.HasPendingInitialUnitsSpawn(out totalConfigs, out initializedConfigs);
    }

    private bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        return RuntimeCityRoadBuildBridgeState.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
    }

    private bool TryGetGridConfig(out GridConfig grid)
    {
        RuntimeCityReadinessQuerySystem readinessQuerySystem = RuntimeCityReadinessQuerySystem;
        if (readinessQuerySystem == null)
        {
            grid = default;
            return false;
        }

        return readinessQuerySystem.TryGetGridConfig(out grid);
    }

    private List<RectInt> CollectInitialBaseExclusionRoadRects(int roadCellSizeInGridCells)
    {
        return RuntimeCityReadinessQuerySystem?.CollectInitialBaseExclusionRoadRects(roadCellSizeInGridCells) ??
            new List<RectInt>();
    }

    private static RuntimeCityVisualSystem ResolveRuntimeCityVisualSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityVisualSystem>()
            : null;
    }

    private static RuntimeCityMinimapEventSystem ResolveRuntimeCityMinimapEventSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityMinimapEventSystem>()
            : null;
    }

    private static RuntimeCityReadinessQuerySystem ResolveRuntimeCityReadinessQuerySystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityReadinessQuerySystem>()
            : null;
    }

    private static RuntimeCityReadModelSystem ResolveRuntimeCityReadModelSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityReadModelSystem>()
            : null;
    }

    private static RuntimeCityConfigSystem ResolveRuntimeCityConfigSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityConfigSystem>()
            : null;
    }

    private static RuntimeCityDiagnosticSystem ResolveRuntimeCityDiagnosticSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityDiagnosticSystem>()
            : null;
    }

    private static RuntimeCityBuildingSpawnContextSystem ResolveRuntimeCityBuildingSpawnContextSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityBuildingSpawnContextSystem>()
            : null;
    }

    private static RuntimeCityStartupSystem ResolveRuntimeCityStartupSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityStartupSystem>()
            : null;
    }

    private static RuntimeCityLifecycleSystem ResolveRuntimeCityLifecycleSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityLifecycleSystem>()
            : null;
    }

    private static RuntimeCityLayoutSystem ResolveRuntimeCityLayoutSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityLayoutSystem>()
            : null;
    }

    private static RuntimeCityRoadLayoutSystem ResolveRuntimeCityRoadLayoutSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityRoadLayoutSystem>()
            : null;
    }

    private static RuntimeCityWalkabilitySystem ResolveRuntimeCityWalkabilitySystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityWalkabilitySystem>()
            : null;
    }

    private static RuntimeCityBuildingPlotSystem ResolveRuntimeCityBuildingPlotSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityBuildingPlotSystem>()
            : null;
    }

    private static RuntimeCityBulkPlotPlanSystem ResolveRuntimeCityBulkPlotPlanSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityBulkPlotPlanSystem>()
            : null;
    }

    private static RuntimeCityPrefabSelectionSystem ResolveRuntimeCityPrefabSelectionSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityPrefabSelectionSystem>()
            : null;
    }

    private static RuntimeCityLandmarkOffsetSystem ResolveRuntimeCityLandmarkOffsetSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityLandmarkOffsetSystem>()
            : null;
    }

    private static RuntimeCityHallSpawnSystem ResolveRuntimeCityHallSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityHallSpawnSystem>()
            : null;
    }

    private static RuntimeCityLandmarkSpawnSystem ResolveRuntimeCityLandmarkSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityLandmarkSpawnSystem>()
            : null;
    }

    private static RuntimeCityEntryBuildingSpawnSystem ResolveRuntimeCityEntryBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityEntryBuildingSpawnSystem>()
            : null;
    }

    private static RuntimeCityRoadsideBuildingSpawnSystem ResolveRuntimeCityRoadsideBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityRoadsideBuildingSpawnSystem>()
            : null;
    }

    private static RuntimeCityRuralBuildingSpawnSystem ResolveRuntimeCityRuralBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityRuralBuildingSpawnSystem>()
            : null;
    }

    private static RuntimeCityBulkBuildingSpawnRoutineSystem ResolveRuntimeCityBulkBuildingSpawnRoutineSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityBulkBuildingSpawnRoutineSystem>()
            : null;
    }

    private static RuntimeCityCorridorBuildingSpawnSystem ResolveRuntimeCityCorridorBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityCorridorBuildingSpawnSystem>()
            : null;
    }

    private static RuntimeCityYardWallPlanSystem ResolveRuntimeCityYardWallPlanSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityYardWallPlanSystem>()
            : null;
    }

    private static RuntimeCityYardGateSystem ResolveRuntimeCityYardGateSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityYardGateSystem>()
            : null;
    }

    private static RuntimeCityYardWallVisualSystem ResolveRuntimeCityYardWallVisualSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityYardWallVisualSystem>()
            : null;
    }

    private static RuntimeCityHouseYardWallSystem ResolveRuntimeCityHouseYardWallSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityHouseYardWallSystem>()
            : null;
    }

    private static RuntimeCityDecorationPrefabGroupSystem ResolveRuntimeCityDecorationPrefabGroupSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityDecorationPrefabGroupSystem>()
            : null;
    }

    private static RuntimeCityClothCoverSpawnSystem ResolveRuntimeCityClothCoverSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityClothCoverSpawnSystem>()
            : null;
    }

    private static RuntimeCityArchwaySpawnSystem ResolveRuntimeCityArchwaySpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityArchwaySpawnSystem>()
            : null;
    }

    private static RuntimeCityFreeScatterDecorationSystem ResolveRuntimeCityFreeScatterDecorationSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityFreeScatterDecorationSystem>()
            : null;
    }

    private static RuntimeCityDecorationBuildingSpawnSystem ResolveRuntimeCityDecorationBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityDecorationBuildingSpawnSystem>()
            : null;
    }

    private static RuntimeCitySpawnBridgeSystem ResolveRuntimeCitySpawnBridgeSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCitySpawnBridgeSystem>()
            : null;
    }

    private static RuntimeCityRoadBuildBridgeSystem ResolveRuntimeCityRoadBuildBridgeSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityRoadBuildBridgeSystem>()
            : null;
    }

    private static RuntimeCityBuildingPlacementSystem ResolveRuntimeCityBuildingPlacementSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityBuildingPlacementSystem>()
            : null;
    }

    private static RuntimeCityGenerationSystem ResolveRuntimeCityGenerationSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityGenerationSystem>()
            : null;
    }

    private static RuntimeCityChainSystem ResolveRuntimeCityChainSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityChainSystem>()
            : null;
    }

    private static RuntimeCityRoadCommitSystem ResolveRuntimeCityRoadCommitSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityRoadCommitSystem>()
            : null;
    }

    private static RuntimeCityIngressSystem ResolveRuntimeCityIngressSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityIngressSystem>()
            : null;
    }
}
