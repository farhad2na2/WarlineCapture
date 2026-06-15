using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeCityCompositionSystem
{
    private RuntimeCitySpawnerSystemConfig _config;
    private readonly RuntimeCityConfigSystem _runtimeCityConfigSystem = new();
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
    private readonly RuntimeCityReadinessQuerySystem _runtimeCityReadinessQuerySystem = new();
    private readonly RuntimeCityGenerationSystem _runtimeCityGenerationSystem = new();
    private readonly RuntimeCityChainSystem _runtimeCityChainSystem = new();
    private readonly RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem = new();
    private readonly RuntimeCityDiagnosticSystem _runtimeCityDiagnosticSystem = new();
    private readonly RuntimeCityIngressSystem _runtimeCityIngressSystem = new();
    private readonly RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem = new();
    private readonly RuntimeCityReadModelSystem _runtimeCityReadModelSystem = new();
    private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCityStartupSystem.TryGetPendingInitialUnitsDelegate _tryGetPendingInitialUnits;
    private readonly RuntimeCityStartupSystem.TryGetRoadCellSizeDelegate _tryGetRoadCellSize;
    private readonly RuntimeCityStartupSystem.TryGetGridDataDelegate _tryGetGridData;
    private RuntimeCityBuildingSpawnContextSystem.Context _runtimeCityBuildingSpawnContext;

    private RuntimeCityConfigSystem.Snapshot cityConfig => _runtimeCityConfigSystem.Current;
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
    public RuntimeCityReadModelSystem ReadModel => _runtimeCityReadModelSystem;

    public RuntimeCityCompositionSystem()
    {
        _tryGetPendingInitialUnits = _runtimeCityReadinessQuerySystem.HasPendingInitialUnitsSpawn;
        _tryGetRoadCellSize = _runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells;
        _tryGetGridData = _runtimeCityReadinessQuerySystem.TryGetGridConfig;
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
        _runtimeCityMinimapEventSystem.Configure(mainMenuPlayUi);
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
        _runtimeCityMinimapEventSystem.Flush();
        TryAutoSpawn(frameCount);
        PublishReadModel();
    }

    public void Dispose()
    {
        _runtimeCityLifecycleSystem.CancelGeneration();
        RuntimeCityVisualSystem?.Dispose();
        _runtimeCitySpawnBridgeSystem.Clear();
        _runtimeCityRoadBuildBridgeSystem.Clear();
        _runtimeCityReadinessQuerySystem.Clear();
        _runtimeCityMinimapEventSystem.Clear();
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
        _runtimeCityConfigSystem.Apply(_config);
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
        _runtimeCityReadModelSystem.Publish(SpawnOnStartEnabled, HasSpawned, IsGenerating);
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
            _runtimeCityReadinessQuerySystem.CollectInitialBaseExclusionRoadRects,
            ShouldYield,
            _runtimeCityMinimapEventSystem,
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

    private static RuntimeCityVisualSystem ResolveRuntimeCityVisualSystem()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RuntimeCityVisualSystem>()
            : null;
    }
}
