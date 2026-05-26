using System.Collections.Generic;
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
    private readonly RuntimeCityBuildingSpawnSystem _runtimeCityBuildingSpawnSystem = new();
    private readonly RuntimeCityVisualSystem _runtimeCityVisualSystem = new();
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

    internal void Configure(
        RuntimeCitySpawnerSystemConfig configAsset,
        RoadRuntimeGenerationSystem roadRuntimeGenerationSystem,
        RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext,
        BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawnSystem,
        BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
        Transform runtimeRoot,
        MainMenuPlayUI mainMenuPlayUi)
    {
        _config = configAsset;
        _runtimeCityRoadBuildBridgeSystem.Configure(roadRuntimeGenerationSystem, roadRuntimeGenerationContext);
        _runtimeCitySpawnBridgeSystem.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
        _runtimeCityVisualSystem.SetRuntimeRoot(runtimeRoot);
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
        _runtimeCityVisualSystem.Dispose();
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
        _runtimeCityBuildingSpawnSystem.Configure(
            cityConfig,
            _runtimeCityBuildingPlotSystem,
            _runtimeCityWalkabilitySystem,
            _runtimeCityPrefabSelectionSystem,
            _runtimeCityVisualSystem,
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
            Chapter01M01PlayableRuntime.IsActiveMission(),
            generateBuildings,
            _runtimeCityRoadBuildBridgeSystem.HasRoadRuntimeGenerationSystem,
            _runtimeCitySpawnBridgeSystem.HasSpawnSystem,
            hallPrefabs,
            shopPrefabs,
            housePrefabs,
            _runtimeCityReadinessQuerySystem.HasPendingInitialUnitsSpawn,
            _runtimeCityRoadBuildBridgeSystem.TryGetRoadCellSizeInGridCells,
            _runtimeCityReadinessQuerySystem.TryGetGridConfig,
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
            _runtimeCityBuildingSpawnSystem,
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
}
