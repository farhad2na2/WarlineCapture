using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCityCompositionSystem
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
    private readonly RuntimeCityBuildingPlacementSystem _runtimeCityBuildingPlacementSystem = new();
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
    private readonly RuntimeCityBulkBuildingSpawnRoutineSystem _runtimeCityBulkBuildingSpawnRoutineSystem = new();
    private RuntimeCityCorridorBuildingSpawnSystem _runtimeCityCorridorBuildingSpawnSystem;
    private readonly RuntimeCityCorridorBuildingSpawnState _fallbackRuntimeCityCorridorBuildingSpawn = new();
    private readonly RuntimeCityYardWallPlanSystem _runtimeCityYardWallPlanSystem = new();
    private readonly RuntimeCityYardGateSystem _runtimeCityYardGateSystem = new();
    private readonly RuntimeCityYardWallVisualSystem _runtimeCityYardWallVisualSystem = new();
    private readonly RuntimeCityHouseYardWallSystem _runtimeCityHouseYardWallSystem = new();
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
    private readonly RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem = new();
    private readonly RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem = new();
    private RuntimeCityLifecycleSystem _runtimeCityLifecycleSystem;
    private readonly RuntimeCityLifecycleState _fallbackRuntimeCityLifecycle = new();
    private RuntimeCityStartupSystem _runtimeCityStartupSystem;
    private readonly RuntimeCityStartupState _fallbackRuntimeCityStartup = new();
    private RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem;
    private readonly RuntimeCityGenerationSystem _runtimeCityGenerationSystem = new();
    private readonly RuntimeCityChainSystem _runtimeCityChainSystem = new();
    private readonly RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem = new();
    private RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem;
    private readonly RuntimeCityIngressSystem _runtimeCityIngressSystem = new();
    private RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem;
    private RuntimeCityReadModelSystem _runtimeCityReadModelSystem;
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCityStartupSystem.TryGetPendingInitialUnitsDelegate _tryGetPendingInitialUnits;
    private readonly RuntimeCityStartupSystem.TryGetRoadCellSizeDelegate _tryGetRoadCellSize;
    private readonly RuntimeCityStartupSystem.TryGetGridDataDelegate _tryGetGridData;
    private RuntimeCityBuildingSpawnContextSystem.Context _runtimeCityBuildingSpawnContext;

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
        _tryGetRoadCellSize = _runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells;
        _tryGetGridData = TryGetGridConfig;
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
        _config = configAsset;
        _runtimeCityRoadBuildBridgeSystem.Configure(roadRuntimeGenerationSystem, roadRuntimeGenerationContext);
        _runtimeCitySpawnBridgeSystem.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
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
        RuntimeCityLifecycleState.CancelGeneration();
        RuntimeCityVisualSystem?.Dispose();
        _runtimeCitySpawnBridgeSystem.Clear();
        _runtimeCityRoadBuildBridgeSystem.Clear();
        RuntimeCityReadinessQuerySystem?.Clear();
        RuntimeCityMinimapEventSystem?.Clear();
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
                _runtimeCitySpawnBridgeSystem,
                RuntimeCityDiagnosticSystem)
            : global::RuntimeCityBuildingSpawnContextSystem.CreateFallback(
                cityConfig,
                RuntimeCityBuildingPlotState,
                RuntimeCityWalkabilityState,
                RuntimeCityPrefabSelectionState,
                RuntimeCityVisualSystem,
                _runtimeCitySpawnBridgeSystem,
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
        _runtimeCityGenerationSystem.TryBegin(CreateGenerationContext(grid, roadCellSizeInGridCells, frameCount));
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
            _runtimeCityRoadBuildBridgeSystem.HasRoadRuntimeGenerationSystem,
            _runtimeCitySpawnBridgeSystem.HasSpawnSystem,
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
            _runtimeCityBuildingPlacementSystem,
            RuntimeCityCorridorBuildingSpawnState,
            _runtimeCityRoadBuildBridgeSystem,
            _runtimeCitySpawnBridgeSystem,
            _runtimeCityChainSystem,
            CreateChainContext(),
            _runtimeCityRoadCommitSystem,
            CreateRoadCommitContext(),
            _runtimeCityIngressSystem,
            CreateIngressContext(),
            CollectInitialBaseExclusionRoadRects,
            ShouldYield,
            RuntimeCityMinimapEventSystem,
            RuntimeCityDiagnosticSystem);
    }

    private RuntimeCityBuildingSpawnContextSystem.Systems CreateBuildingSpawnSystems()
    {
        return new RuntimeCityBuildingSpawnContextSystem.Systems(
            _runtimeCityBuildingPlacementSystem,
            RuntimeCityLandmarkOffsetState,
            RuntimeCityHallSpawnState,
            RuntimeCityLandmarkSpawnState,
            RuntimeCityBulkPlotPlanState,
            RuntimeCityEntryBuildingSpawnState,
            RuntimeCityRoadsideBuildingSpawnState,
            RuntimeCityRuralBuildingSpawnState,
            _runtimeCityBulkBuildingSpawnRoutineSystem,
            RuntimeCityCorridorBuildingSpawnState,
            _runtimeCityYardWallPlanSystem,
            _runtimeCityYardGateSystem,
            _runtimeCityYardWallVisualSystem,
            _runtimeCityHouseYardWallSystem,
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
            _runtimeCityRoadCommitSystem,
            _runtimeCityIngressSystem,
            CreateIngressContext());
    }

    private RuntimeCityRoadCommitSystem.Context CreateRoadCommitContext()
    {
        return new RuntimeCityRoadCommitSystem.Context(
            _runtimeCityRoadBuildBridgeSystem,
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

    private RuntimeCityCorridorBuildingSpawnState RuntimeCityCorridorBuildingSpawnState =>
        RuntimeCityCorridorBuildingSpawnSystem?.State ?? _fallbackRuntimeCityCorridorBuildingSpawn;

    private RuntimeCityCorridorBuildingSpawnSystem RuntimeCityCorridorBuildingSpawnSystem =>
        _runtimeCityCorridorBuildingSpawnSystem ??= ResolveRuntimeCityCorridorBuildingSpawnSystem();

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

    private static RuntimeCityCorridorBuildingSpawnSystem ResolveRuntimeCityCorridorBuildingSpawnSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityCorridorBuildingSpawnSystem>()
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
}
