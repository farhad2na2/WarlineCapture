using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCityCompositionSystem
{
    private RuntimeCitySpawnerSystemConfig _config;
    private readonly List<GameObject> _fallbackCityPrefabs = new();
    private RuntimeCityConfigSystem _runtimeCityConfigSystem;
    private RuntimeCityConfigSystem.Snapshot _fallbackCityConfig;
    private readonly RuntimeCityLayoutSystem _runtimeCityLayoutSystem = new();
    private readonly RuntimeCityRoadLayoutSystem _runtimeCityRoadLayoutSystem = new();
    private readonly RuntimeCityBuildingPlotSystem _runtimeCityBuildingPlotSystem = new();
    private readonly RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem = new();
    private readonly RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem = new();
    private readonly RuntimeCityBuildingSpawnContextSystem _runtimeCityBuildingSpawnContextSystem = new();
    private readonly RuntimeCityBuildingPlacementSystem _runtimeCityBuildingPlacementSystem = new();
    private readonly RuntimeCityLandmarkOffsetSystem _runtimeCityLandmarkOffsetSystem = new();
    private readonly RuntimeCityHallSpawnSystem _runtimeCityHallSpawnSystem = new();
    private readonly RuntimeCityLandmarkSpawnSystem _runtimeCityLandmarkSpawnSystem = new();
    private readonly RuntimeCityBulkPlotPlanSystem _runtimeCityBulkPlotPlanSystem = new();
    private readonly RuntimeCityEntryBuildingSpawnSystem _runtimeCityEntryBuildingSpawnSystem = new();
    private readonly RuntimeCityRoadsideBuildingSpawnSystem _runtimeCityRoadsideBuildingSpawnSystem = new();
    private readonly RuntimeCityRuralBuildingSpawnSystem _runtimeCityRuralBuildingSpawnSystem = new();
    private readonly RuntimeCityBulkBuildingSpawnRoutineSystem _runtimeCityBulkBuildingSpawnRoutineSystem = new();
    private readonly RuntimeCityCorridorBuildingSpawnSystem _runtimeCityCorridorBuildingSpawnSystem = new();
    private readonly RuntimeCityYardWallPlanSystem _runtimeCityYardWallPlanSystem = new();
    private readonly RuntimeCityYardGateSystem _runtimeCityYardGateSystem = new();
    private readonly RuntimeCityYardWallVisualSystem _runtimeCityYardWallVisualSystem = new();
    private readonly RuntimeCityHouseYardWallSystem _runtimeCityHouseYardWallSystem = new();
    private readonly RuntimeCityDecorationPrefabGroupSystem _runtimeCityDecorationPrefabGroupSystem = new();
    private readonly RuntimeCityClothCoverSpawnSystem _runtimeCityClothCoverSpawnSystem = new();
    private readonly RuntimeCityArchwaySpawnSystem _runtimeCityArchwaySpawnSystem = new();
    private readonly RuntimeCityFreeScatterDecorationSystem _runtimeCityFreeScatterDecorationSystem = new();
    private readonly RuntimeCityDecorationBuildingSpawnSystem _runtimeCityDecorationBuildingSpawnSystem = new();
    private RuntimeCityVisualSystem _runtimeCityVisualSystem;
    private readonly RuntimeCitySpawnBridgeSystem _runtimeCitySpawnBridgeSystem = new();
    private readonly RuntimeCityRoadBuildBridgeSystem _runtimeCityRoadBuildBridgeSystem = new();
    private readonly RuntimeCityLifecycleSystem _runtimeCityLifecycleSystem = new();
    private readonly RuntimeCityStartupSystem _runtimeCityStartupSystem = new();
    private RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem;
    private readonly RuntimeCityGenerationSystem _runtimeCityGenerationSystem = new();
    private readonly RuntimeCityChainSystem _runtimeCityChainSystem = new();
    private readonly RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem = new();
    private readonly RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem = new();
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
    public bool HasSpawned => _runtimeCityLifecycleSystem.HasSpawned(cityCount);
    public bool IsGenerating => _runtimeCityLifecycleSystem.IsGenerating;
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
        return RuntimeCityStartupSystem.DescribeStartupBlocker(CreateStartupContext(frameCount));
    }

    public void MarkSpawnedAfterLoadingGateTimeout()
    {
        _runtimeCityLifecycleSystem.MarkSpawned();
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
        _runtimeCityLifecycleSystem.Tick(CreateLifecycleContext(frameCount));
        RuntimeCityMinimapEventSystem?.Flush();
        TryAutoSpawn(frameCount);
        PublishReadModel();
    }

    public void Dispose()
    {
        _runtimeCityLifecycleSystem.CancelGeneration();
        RuntimeCityVisualSystem?.Dispose();
        _runtimeCitySpawnBridgeSystem.Clear();
        _runtimeCityRoadBuildBridgeSystem.Clear();
        RuntimeCityReadinessQuerySystem?.Clear();
        RuntimeCityMinimapEventSystem?.Clear();
    }

    public bool IsConfiguredHousePrefab(GameObject prefab)
    {
        return _runtimeCityPrefabSelectionSystem.IsConfiguredPrefab(prefab, housePrefabs);
    }

    public void GenerateCity(int frameCount)
    {
        RuntimeCityStartupSystem.Result result = _runtimeCityStartupSystem.EvaluateManualGeneration(CreateStartupContext(frameCount));
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

        _runtimeCityBuildingSpawnContext = _runtimeCityBuildingSpawnContextSystem.Create(
            cityConfig,
            _runtimeCityBuildingPlotSystem,
            _runtimeCityWalkabilitySystem,
            _runtimeCityPrefabSelectionSystem,
            RuntimeCityVisualSystem,
            _runtimeCitySpawnBridgeSystem,
            _runtimeCityDiagnosticSystem);
    }

    private void TryAutoSpawn(int frameCount)
    {
        RuntimeCityStartupSystem.Result result = _runtimeCityStartupSystem.Evaluate(CreateStartupContext(frameCount));
        if (result.Kind == RuntimeCityStartupSystem.ResultKind.MarkSpawned)
            _runtimeCityLifecycleSystem.MarkSpawned();
        else if (result.Kind == RuntimeCityStartupSystem.ResultKind.Generate)
            GenerateCity(result.Grid, result.RoadCellSizeInGridCells, frameCount);
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
        return _runtimeCityLifecycleSystem.ShouldYield(completedWorkItems, generationYieldInterval);
    }

    private RuntimeCityLifecycleSystem.Context CreateLifecycleContext(int frameCount)
    {
        return new RuntimeCityLifecycleSystem.Context(
            frameCount,
            cityCount,
            generateBuildings,
            generationYieldInterval,
            _runtimeCityDiagnosticSystem);
    }

    private RuntimeCityStartupSystem.Context CreateStartupContext(int frameCount)
    {
        return new RuntimeCityStartupSystem.Context(
            frameCount,
            spawnOnStart,
            _runtimeCityLifecycleSystem.IsSpawned,
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
            _runtimeCityDiagnosticSystem);
    }

    private RuntimeCityGenerationSystem.Context CreateGenerationContext(GridConfig grid, int roadCellSizeInGridCells, int frameCount)
    {
        return new RuntimeCityGenerationSystem.Context(
            cityConfig,
            grid,
            roadCellSizeInGridCells,
            _runtimeCityLifecycleSystem,
            CreateLifecycleContext(frameCount),
            _runtimeCityLayoutSystem,
            _runtimeCityWalkabilitySystem,
            CreateBuildingSpawnSystems(),
            _runtimeCityBuildingSpawnContext,
            _runtimeCityBuildingPlacementSystem,
            _runtimeCityCorridorBuildingSpawnSystem,
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
            _runtimeCityDiagnosticSystem);
    }

    private RuntimeCityBuildingSpawnContextSystem.Systems CreateBuildingSpawnSystems()
    {
        return new RuntimeCityBuildingSpawnContextSystem.Systems(
            _runtimeCityBuildingPlacementSystem,
            _runtimeCityLandmarkOffsetSystem,
            _runtimeCityHallSpawnSystem,
            _runtimeCityLandmarkSpawnSystem,
            _runtimeCityBulkPlotPlanSystem,
            _runtimeCityEntryBuildingSpawnSystem,
            _runtimeCityRoadsideBuildingSpawnSystem,
            _runtimeCityRuralBuildingSpawnSystem,
            _runtimeCityBulkBuildingSpawnRoutineSystem,
            _runtimeCityCorridorBuildingSpawnSystem,
            _runtimeCityYardWallPlanSystem,
            _runtimeCityYardGateSystem,
            _runtimeCityYardWallVisualSystem,
            _runtimeCityHouseYardWallSystem,
            _runtimeCityDecorationPrefabGroupSystem,
            _runtimeCityClothCoverSpawnSystem,
            _runtimeCityArchwaySpawnSystem,
            _runtimeCityFreeScatterDecorationSystem,
            _runtimeCityDecorationBuildingSpawnSystem);
    }

    private RuntimeCityChainSystem.Context CreateChainContext()
    {
        return new RuntimeCityChainSystem.Context(
            cityConfig,
            _runtimeCityLayoutSystem,
            _runtimeCityRoadLayoutSystem,
            _runtimeCityPrefabSelectionSystem,
            _runtimeCityRoadCommitSystem,
            _runtimeCityIngressSystem,
            CreateIngressContext());
    }

    private RuntimeCityRoadCommitSystem.Context CreateRoadCommitContext()
    {
        return new RuntimeCityRoadCommitSystem.Context(
            _runtimeCityRoadBuildBridgeSystem,
            _runtimeCityDiagnosticSystem);
    }

    private RuntimeCityIngressSystem.Context CreateIngressContext()
    {
        return new RuntimeCityIngressSystem.Context(
            cityConfig,
            _runtimeCityRoadLayoutSystem);
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
}
