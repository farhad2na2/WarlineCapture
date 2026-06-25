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
    private RuntimeCityBuildingPlotUtilitySystemHelper _runtimeCityBuildingPlotHelper;
    private readonly RuntimeCityBuildingPlotState _fallbackRuntimeCityBuildingPlot = new();
    private RuntimeCityWalkabilitySystem _runtimeCityWalkabilitySystem;
    private readonly RuntimeCityWalkabilityState _fallbackRuntimeCityWalkability = new();
    private RuntimeCityPrefabSelectionSystem _runtimeCityPrefabSelectionSystem;
    private readonly RuntimeCityPrefabSelectionState _fallbackRuntimeCityPrefabSelection = new();
    private RuntimeCityBuildingSpawnContextCompositionSystemHelper _runtimeCityBuildingSpawnContextHelper;
    private RuntimeCityBuildingPlacementPrefabSystemHelper _runtimeCityBuildingPlacementHelper;
    private readonly RuntimeCityBuildingPlacementState _fallbackRuntimeCityBuildingPlacement = new();
    private RuntimeCityLandmarkOffsetSystem _runtimeCityLandmarkOffsetSystem;
    private readonly RuntimeCityLandmarkOffsetState _fallbackRuntimeCityLandmarkOffset = new();
    private RuntimeCityHallSpawnSystem _runtimeCityHallSpawnSystem;
    private readonly RuntimeCityHallSpawnState _fallbackRuntimeCityHallSpawn = new();
    private RuntimeCityLandmarkSpawnSystem _runtimeCityLandmarkSpawnSystem;
    private readonly RuntimeCityLandmarkSpawnState _fallbackRuntimeCityLandmarkSpawn = new();
    private RuntimeCityBulkPlotPlanUtilitySystemHelper _runtimeCityBulkPlotPlanHelper;
    private readonly RuntimeCityBulkPlotPlanState _fallbackRuntimeCityBulkPlotPlan = new();
    private RuntimeCityEntryBuildingSpawnPrefabSystemHelper _runtimeCityEntryBuildingSpawnHelper;
    private readonly RuntimeCityEntryBuildingSpawnState _fallbackRuntimeCityEntryBuildingSpawn = new();
    private RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper _runtimeCityRoadsideBuildingSpawnHelper;
    private readonly RuntimeCityRoadsideBuildingSpawnState _fallbackRuntimeCityRoadsideBuildingSpawn = new();
    private RuntimeCityRuralBuildingSpawnSystem _runtimeCityRuralBuildingSpawnSystem;
    private readonly RuntimeCityRuralBuildingSpawnState _fallbackRuntimeCityRuralBuildingSpawn = new();
    private RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper _runtimeCityBulkBuildingSpawnRoutineHelper;
    private readonly RuntimeCityBulkBuildingSpawnRoutineState _fallbackRuntimeCityBulkBuildingSpawnRoutine = new();
    private RuntimeCityCorridorBuildingSpawnPrefabSystemHelper _runtimeCityCorridorBuildingSpawnHelper;
    private readonly RuntimeCityCorridorBuildingSpawnState _fallbackRuntimeCityCorridorBuildingSpawn = new();
    private RuntimeCityYardWallPlanSystem _runtimeCityYardWallPlanSystem;
    private readonly RuntimeCityYardWallPlanState _fallbackRuntimeCityYardWallPlan = new();
    private RuntimeCityYardGateSystem _runtimeCityYardGateSystem;
    private readonly RuntimeCityYardGateState _fallbackRuntimeCityYardGate = new();
    private RuntimeCityYardWallVisualSystem _runtimeCityYardWallVisualSystem;
    private readonly RuntimeCityYardWallVisualState _fallbackRuntimeCityYardWallVisual = new();
    private RuntimeCityHouseYardWallSystem _runtimeCityHouseYardWallSystem;
    private readonly RuntimeCityHouseYardWallState _fallbackRuntimeCityHouseYardWall = new();
    private RuntimeCityDecorationGroupPrefabSystemHelper _runtimeCityDecorationGroupHelper;
    private readonly RuntimeCityDecorationPrefabGroupState _fallbackRuntimeCityDecorationPrefabGroup = new();
    private RuntimeCityClothCoverSpawnPrefabSystemHelper _runtimeCityClothCoverSpawnHelper;
    private readonly RuntimeCityClothCoverSpawnState _fallbackRuntimeCityClothCoverSpawn = new();
    private RuntimeCityArchwaySpawnPrefabSystemHelper _runtimeCityArchwaySpawnHelper;
    private readonly RuntimeCityArchwaySpawnState _fallbackRuntimeCityArchwaySpawn = new();
    private RuntimeCityFreeScatterDecorationPrefabSystemHelper _runtimeCityFreeScatterDecorationHelper;
    private readonly RuntimeCityFreeScatterDecorationState _fallbackRuntimeCityFreeScatterDecoration = new();
    private RuntimeCityDecorationBuildingSpawnPrefabSystemHelper _runtimeCityDecorationBuildingSpawnHelper;
    private readonly RuntimeCityDecorationBuildingSpawnState _fallbackRuntimeCityDecorationBuildingSpawn = new();
    private RuntimeCityVisualPresentationSystemHelper _runtimeCityVisualPresentationSystemHelper;
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
    private RuntimeCityChainUtilitySystemHelper _runtimeCityChainHelper;
    private readonly RuntimeCityChainState _fallbackRuntimeCityChain = new();
    private RuntimeCityRoadCommitSystem _runtimeCityRoadCommitSystem;
    private readonly RuntimeCityRoadCommitState _fallbackRuntimeCityRoadCommit = new();
    private RuntimeCityDiagnosticsSystemHelper _runtimeCityDiagnosticSystem;
    private RuntimeCityIngressSystem _runtimeCityIngressSystem;
    private readonly RuntimeCityIngressState _fallbackRuntimeCityIngress = new();
    private RuntimeCityMinimapEventSystem _runtimeCityMinimapEventSystem;
    private RuntimeCityReadModelCompositionSystemHelper _runtimeCityReadModelSystem;
    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
    private readonly RuntimeCityStartupSystem.TryGetPendingInitialUnitsDelegate _tryGetPendingInitialUnits;
    private readonly RuntimeCityStartupSystem.TryGetRoadCellSizeDelegate _tryGetRoadCellSize;
    private readonly RuntimeCityStartupSystem.TryGetGridDataDelegate _tryGetGridData;
    private RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context _runtimeCityBuildingSpawnContext;
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
    public RuntimeCityReadModelCompositionSystemHelper ReadModel => RuntimeCityReadModelCompositionSystemHelper;

    public RuntimeCityCompositionSystem()
    {
        _fallbackCityConfig = global::RuntimeCityConfigSystem.Snapshot.Default(_fallbackCityPrefabs);
        _tryGetPendingInitialUnits = TryGetPendingInitialUnits;
        _tryGetRoadCellSize = TryGetRoadCellSizeInGridCells;
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
        _configured = true;
        _config = configAsset;
        RuntimeCityRoadBuildBridgeState.Configure(roadRuntimeGenerationSystem, roadRuntimeGenerationContext);
        RuntimeCitySpawnBridgeState.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
        RuntimeCityVisualPresentationSystemHelper?.SetRuntimeRoot(runtimeRoot);
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
        _runtimeCityVisualPresentationSystemHelper?.Dispose();
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

        RuntimeCityBuildingSpawnContextCompositionSystemHelper spawnContextHelper = RuntimeCityBuildingSpawnContextHelper;
        _runtimeCityBuildingSpawnContext = spawnContextHelper != null
            ? spawnContextHelper.Create(
                cityConfig,
                RuntimeCityBuildingPlotState,
                RuntimeCityWalkabilityState,
                RuntimeCityPrefabSelectionState,
                RuntimeCityVisualPresentationSystemHelper,
                RuntimeCitySpawnBridgeState,
                RuntimeCityDiagnosticsSystemHelper)
            : global::RuntimeCityBuildingSpawnContextCompositionSystemHelper.CreateFallback(
                cityConfig,
                RuntimeCityBuildingPlotState,
                RuntimeCityWalkabilityState,
                RuntimeCityPrefabSelectionState,
                RuntimeCityVisualPresentationSystemHelper,
                RuntimeCitySpawnBridgeState,
                RuntimeCityDiagnosticsSystemHelper);
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
        RuntimeCityReadModelCompositionSystemHelper?.Publish(SpawnOnStartEnabled, HasSpawned, IsGenerating);
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
            RuntimeCityDiagnosticsSystemHelper);
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
            RuntimeCityDiagnosticsSystemHelper);
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
            RuntimeCityDiagnosticsSystemHelper);
    }

    private RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems CreateBuildingSpawnSystems()
    {
        return new RuntimeCityBuildingSpawnContextCompositionSystemHelper.Systems(
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

    private RuntimeCityChainUtilitySystemHelper.Context CreateChainContext()
    {
        return new RuntimeCityChainUtilitySystemHelper.Context(
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
            RuntimeCityDiagnosticsSystemHelper);
    }

    private RuntimeCityIngressSystem.Context CreateIngressContext()
    {
        return new RuntimeCityIngressSystem.Context(
            cityConfig,
            RuntimeCityRoadLayoutState);
    }

    private RuntimeCityVisualPresentationSystemHelper RuntimeCityVisualPresentationSystemHelper =>
        _runtimeCityVisualPresentationSystemHelper ??= ResolveRuntimeCityVisualPresentationSystemHelper();

    private RuntimeCityMinimapEventSystem RuntimeCityMinimapEventSystem =>
        _runtimeCityMinimapEventSystem ??= ResolveRuntimeCityMinimapEventSystem();

    private RuntimeCityReadinessQuerySystem RuntimeCityReadinessQuerySystem =>
        _runtimeCityReadinessQuerySystem ??= new RuntimeCityReadinessQuerySystem();

    private RuntimeCityReadModelCompositionSystemHelper RuntimeCityReadModelCompositionSystemHelper =>
        _runtimeCityReadModelSystem ??= new RuntimeCityReadModelCompositionSystemHelper();

    private RuntimeCityConfigSystem RuntimeCityConfigSystem =>
        _runtimeCityConfigSystem ??= ResolveRuntimeCityConfigSystem();

    private RuntimeCityDiagnosticsSystemHelper RuntimeCityDiagnosticsSystemHelper =>
        _runtimeCityDiagnosticSystem ??= new RuntimeCityDiagnosticsSystemHelper();

    private RuntimeCityBuildingSpawnContextCompositionSystemHelper RuntimeCityBuildingSpawnContextHelper =>
        _runtimeCityBuildingSpawnContextHelper ??= new RuntimeCityBuildingSpawnContextCompositionSystemHelper();

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
        RuntimeCityBuildingPlotHelper?.State ?? _fallbackRuntimeCityBuildingPlot;

    private RuntimeCityBuildingPlotUtilitySystemHelper RuntimeCityBuildingPlotHelper =>
        _runtimeCityBuildingPlotHelper ??= ResolveRuntimeCityBuildingPlotHelper();

    private RuntimeCityBulkPlotPlanState RuntimeCityBulkPlotPlanState =>
        RuntimeCityBulkPlotPlanHelper?.State ?? _fallbackRuntimeCityBulkPlotPlan;

    private RuntimeCityBulkPlotPlanUtilitySystemHelper RuntimeCityBulkPlotPlanHelper =>
        _runtimeCityBulkPlotPlanHelper ??= ResolveRuntimeCityBulkPlotPlanHelper();

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
        RuntimeCityEntryBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityEntryBuildingSpawn;

    private RuntimeCityEntryBuildingSpawnPrefabSystemHelper RuntimeCityEntryBuildingSpawnPrefabSystemHelper =>
        _runtimeCityEntryBuildingSpawnHelper ??= ResolveRuntimeCityEntryBuildingSpawnPrefabSystemHelper();

    private RuntimeCityRoadsideBuildingSpawnState RuntimeCityRoadsideBuildingSpawnState =>
        RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityRoadsideBuildingSpawn;

    private RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper =>
        _runtimeCityRoadsideBuildingSpawnHelper ??= ResolveRuntimeCityRoadsideBuildingSpawnPrefabSystemHelper();

    private RuntimeCityRuralBuildingSpawnState RuntimeCityRuralBuildingSpawnState =>
        RuntimeCityRuralBuildingSpawnSystem?.State ?? _fallbackRuntimeCityRuralBuildingSpawn;

    private RuntimeCityRuralBuildingSpawnSystem RuntimeCityRuralBuildingSpawnSystem =>
        _runtimeCityRuralBuildingSpawnSystem ??= ResolveRuntimeCityRuralBuildingSpawnSystem();

    private RuntimeCityBulkBuildingSpawnRoutineState RuntimeCityBulkBuildingSpawnRoutineState =>
        RuntimeCityBulkBuildingSpawnRoutineHelper?.State ?? _fallbackRuntimeCityBulkBuildingSpawnRoutine;

    private RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper RuntimeCityBulkBuildingSpawnRoutineHelper =>
        _runtimeCityBulkBuildingSpawnRoutineHelper ??= ResolveRuntimeCityBulkBuildingSpawnRoutineHelper();

    private RuntimeCityCorridorBuildingSpawnState RuntimeCityCorridorBuildingSpawnState =>
        RuntimeCityCorridorBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityCorridorBuildingSpawn;

    private RuntimeCityCorridorBuildingSpawnPrefabSystemHelper RuntimeCityCorridorBuildingSpawnPrefabSystemHelper =>
        _runtimeCityCorridorBuildingSpawnHelper ??= ResolveRuntimeCityCorridorBuildingSpawnPrefabSystemHelper();

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
        RuntimeCityDecorationGroupHelper?.State ?? _fallbackRuntimeCityDecorationPrefabGroup;

    private RuntimeCityDecorationGroupPrefabSystemHelper RuntimeCityDecorationGroupHelper =>
        _runtimeCityDecorationGroupHelper ??= ResolveRuntimeCityDecorationGroupHelper();

    private RuntimeCityClothCoverSpawnState RuntimeCityClothCoverSpawnState =>
        RuntimeCityClothCoverSpawnHelper?.State ?? _fallbackRuntimeCityClothCoverSpawn;

    private RuntimeCityClothCoverSpawnPrefabSystemHelper RuntimeCityClothCoverSpawnHelper =>
        _runtimeCityClothCoverSpawnHelper ??= ResolveRuntimeCityClothCoverSpawnHelper();

    private RuntimeCityArchwaySpawnState RuntimeCityArchwaySpawnState =>
        RuntimeCityArchwaySpawnHelper?.State ?? _fallbackRuntimeCityArchwaySpawn;

    private RuntimeCityArchwaySpawnPrefabSystemHelper RuntimeCityArchwaySpawnHelper =>
        _runtimeCityArchwaySpawnHelper ??= ResolveRuntimeCityArchwaySpawnHelper();

    private RuntimeCityFreeScatterDecorationState RuntimeCityFreeScatterDecorationState =>
        RuntimeCityFreeScatterDecorationHelper?.State ?? _fallbackRuntimeCityFreeScatterDecoration;

    private RuntimeCityFreeScatterDecorationPrefabSystemHelper RuntimeCityFreeScatterDecorationHelper =>
        _runtimeCityFreeScatterDecorationHelper ??= ResolveRuntimeCityFreeScatterDecorationHelper();

    private RuntimeCityDecorationBuildingSpawnState RuntimeCityDecorationBuildingSpawnState =>
        RuntimeCityDecorationBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityDecorationBuildingSpawn;

    private RuntimeCityDecorationBuildingSpawnPrefabSystemHelper RuntimeCityDecorationBuildingSpawnPrefabSystemHelper =>
        _runtimeCityDecorationBuildingSpawnHelper ??= ResolveRuntimeCityDecorationBuildingSpawnPrefabSystemHelper();

    private RuntimeCityBuildingPlacementState RuntimeCityBuildingPlacementState =>
        RuntimeCityBuildingPlacementHelper?.State ?? _fallbackRuntimeCityBuildingPlacement;

    private RuntimeCityBuildingPlacementPrefabSystemHelper RuntimeCityBuildingPlacementHelper =>
        _runtimeCityBuildingPlacementHelper ??= ResolveRuntimeCityBuildingPlacementHelper();

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
        RuntimeCityChainHelper?.State ?? _fallbackRuntimeCityChain;

    private RuntimeCityChainUtilitySystemHelper RuntimeCityChainHelper =>
        _runtimeCityChainHelper ??= ResolveRuntimeCityChainHelper();

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

    private static RuntimeCityVisualPresentationSystemHelper ResolveRuntimeCityVisualPresentationSystemHelper()
    {
        return new RuntimeCityVisualPresentationSystemHelper();
    }

    private static RuntimeCityMinimapEventSystem ResolveRuntimeCityMinimapEventSystem()
    {
        return new RuntimeCityMinimapEventSystem();
    }

    private static RuntimeCityConfigSystem ResolveRuntimeCityConfigSystem()
    {
        return new RuntimeCityConfigSystem();
    }

    private static RuntimeCityStartupSystem ResolveRuntimeCityStartupSystem()
    {
        return new RuntimeCityStartupSystem();
    }

    private static RuntimeCityLifecycleSystem ResolveRuntimeCityLifecycleSystem()
    {
        return new RuntimeCityLifecycleSystem();
    }

    private static RuntimeCityLayoutSystem ResolveRuntimeCityLayoutSystem()
    {
        return new RuntimeCityLayoutSystem();
    }

    private static RuntimeCityRoadLayoutSystem ResolveRuntimeCityRoadLayoutSystem()
    {
        return new RuntimeCityRoadLayoutSystem();
    }

    private static RuntimeCityWalkabilitySystem ResolveRuntimeCityWalkabilitySystem()
    {
        return new RuntimeCityWalkabilitySystem();
    }

    private static RuntimeCityBuildingPlotUtilitySystemHelper ResolveRuntimeCityBuildingPlotHelper()
    {
        return new RuntimeCityBuildingPlotUtilitySystemHelper();
    }

    private static RuntimeCityBulkPlotPlanUtilitySystemHelper ResolveRuntimeCityBulkPlotPlanHelper()
    {
        return new RuntimeCityBulkPlotPlanUtilitySystemHelper();
    }

    private static RuntimeCityPrefabSelectionSystem ResolveRuntimeCityPrefabSelectionSystem()
    {
        return new RuntimeCityPrefabSelectionSystem();
    }

    private static RuntimeCityLandmarkOffsetSystem ResolveRuntimeCityLandmarkOffsetSystem()
    {
        return new RuntimeCityLandmarkOffsetSystem();
    }

    private static RuntimeCityHallSpawnSystem ResolveRuntimeCityHallSpawnSystem()
    {
        return new RuntimeCityHallSpawnSystem();
    }

    private static RuntimeCityLandmarkSpawnSystem ResolveRuntimeCityLandmarkSpawnSystem()
    {
        return new RuntimeCityLandmarkSpawnSystem();
    }

    private static RuntimeCityEntryBuildingSpawnPrefabSystemHelper ResolveRuntimeCityEntryBuildingSpawnPrefabSystemHelper()
    {
        return new RuntimeCityEntryBuildingSpawnPrefabSystemHelper();
    }

    private static RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper ResolveRuntimeCityRoadsideBuildingSpawnPrefabSystemHelper()
    {
        return new RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper();
    }

    private static RuntimeCityRuralBuildingSpawnSystem ResolveRuntimeCityRuralBuildingSpawnSystem()
    {
        return new RuntimeCityRuralBuildingSpawnSystem();
    }

    private static RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper ResolveRuntimeCityBulkBuildingSpawnRoutineHelper()
    {
        return new RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper();
    }

    private static RuntimeCityCorridorBuildingSpawnPrefabSystemHelper ResolveRuntimeCityCorridorBuildingSpawnPrefabSystemHelper()
    {
        return new RuntimeCityCorridorBuildingSpawnPrefabSystemHelper();
    }

    private static RuntimeCityYardWallPlanSystem ResolveRuntimeCityYardWallPlanSystem()
    {
        return new RuntimeCityYardWallPlanSystem();
    }

    private static RuntimeCityYardGateSystem ResolveRuntimeCityYardGateSystem()
    {
        return new RuntimeCityYardGateSystem();
    }

    private static RuntimeCityYardWallVisualSystem ResolveRuntimeCityYardWallVisualSystem()
    {
        return new RuntimeCityYardWallVisualSystem();
    }

    private static RuntimeCityHouseYardWallSystem ResolveRuntimeCityHouseYardWallSystem()
    {
        return new RuntimeCityHouseYardWallSystem();
    }

    private static RuntimeCityDecorationGroupPrefabSystemHelper ResolveRuntimeCityDecorationGroupHelper()
    {
        return new RuntimeCityDecorationGroupPrefabSystemHelper();
    }

    private static RuntimeCityClothCoverSpawnPrefabSystemHelper ResolveRuntimeCityClothCoverSpawnHelper()
    {
        return new RuntimeCityClothCoverSpawnPrefabSystemHelper();
    }

    private static RuntimeCityArchwaySpawnPrefabSystemHelper ResolveRuntimeCityArchwaySpawnHelper()
    {
        return new RuntimeCityArchwaySpawnPrefabSystemHelper();
    }

    private static RuntimeCityFreeScatterDecorationPrefabSystemHelper ResolveRuntimeCityFreeScatterDecorationHelper()
    {
        return new RuntimeCityFreeScatterDecorationPrefabSystemHelper();
    }

    private static RuntimeCityDecorationBuildingSpawnPrefabSystemHelper ResolveRuntimeCityDecorationBuildingSpawnPrefabSystemHelper()
    {
        return new RuntimeCityDecorationBuildingSpawnPrefabSystemHelper();
    }

    private static RuntimeCitySpawnBridgeSystem ResolveRuntimeCitySpawnBridgeSystem()
    {
        return new RuntimeCitySpawnBridgeSystem();
    }

    private static RuntimeCityRoadBuildBridgeSystem ResolveRuntimeCityRoadBuildBridgeSystem()
    {
        return new RuntimeCityRoadBuildBridgeSystem();
    }

    private static RuntimeCityBuildingPlacementPrefabSystemHelper ResolveRuntimeCityBuildingPlacementHelper()
    {
        return new RuntimeCityBuildingPlacementPrefabSystemHelper();
    }

    private static RuntimeCityGenerationSystem ResolveRuntimeCityGenerationSystem()
    {
        return new RuntimeCityGenerationSystem();
    }

    private static RuntimeCityChainUtilitySystemHelper ResolveRuntimeCityChainHelper()
    {
        return new RuntimeCityChainUtilitySystemHelper();
    }

    private static RuntimeCityRoadCommitSystem ResolveRuntimeCityRoadCommitSystem()
    {
        return new RuntimeCityRoadCommitSystem();
    }

    private static RuntimeCityIngressSystem ResolveRuntimeCityIngressSystem()
    {
        return new RuntimeCityIngressSystem();
    }
}
