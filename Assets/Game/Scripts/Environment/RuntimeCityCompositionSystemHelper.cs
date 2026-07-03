using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public sealed class RuntimeCityCompositionSystemHelper
    {
        private RuntimeCitySpawnerSystemConfig _config;
        private readonly List<GameObject> _fallbackCityPrefabs = new();
        private RuntimeCityConfigCompositionSystemHelper _runtimeCityConfigHelper;
        private RuntimeCityConfigCompositionSystemHelper.Snapshot _fallbackCityConfig;
        private RuntimeCityLayoutUtilitySystemHelper _runtimeCityLayoutHelper;
        private readonly RuntimeCityLayoutState _fallbackRuntimeCityLayout = new();
        private RuntimeCityRoadLayoutUtilitySystemHelper _runtimeCityRoadLayoutHelper;
        private readonly RuntimeCityRoadLayoutState _fallbackRuntimeCityRoadLayout = new();
        private RuntimeCityBuildingPlotUtilitySystemHelper _runtimeCityBuildingPlotHelper;
        private readonly RuntimeCityBuildingPlotState _fallbackRuntimeCityBuildingPlot = new();
        private RuntimeCityWalkabilityUtilitySystemHelper _runtimeCityWalkabilityHelper;
        private readonly RuntimeCityWalkabilityState _fallbackRuntimeCityWalkability = new();
        private RuntimeCityPrefabSelectionPrefabSystemHelper _runtimeCityPrefabSelectionHelper;
        private readonly RuntimeCityPrefabSelectionState _fallbackRuntimeCityPrefabSelection = new();
        private RuntimeCityBuildingSpawnContextCompositionSystemHelper _runtimeCityBuildingSpawnContextHelper;
        private RuntimeCityBuildingPlacementPrefabSystemHelper _runtimeCityBuildingPlacementHelper;
        private readonly RuntimeCityBuildingPlacementState _fallbackRuntimeCityBuildingPlacement = new();
        private RuntimeCityLandmarkOffsetUtilitySystemHelper _runtimeCityLandmarkOffsetHelper;
        private readonly RuntimeCityLandmarkOffsetState _fallbackRuntimeCityLandmarkOffset = new();
        private RuntimeCityHallSpawnPrefabSystemHelper _runtimeCityHallSpawnHelper;
        private readonly RuntimeCityHallSpawnState _fallbackRuntimeCityHallSpawn = new();
        private RuntimeCityLandmarkSpawnPrefabSystemHelper _runtimeCityLandmarkSpawnHelper;
        private readonly RuntimeCityLandmarkSpawnState _fallbackRuntimeCityLandmarkSpawn = new();
        private RuntimeCityBulkPlotPlanUtilitySystemHelper _runtimeCityBulkPlotPlanHelper;
        private readonly RuntimeCityBulkPlotPlanState _fallbackRuntimeCityBulkPlotPlan = new();
        private RuntimeCityEntryBuildingSpawnPrefabSystemHelper _runtimeCityEntryBuildingSpawnHelper;
        private readonly RuntimeCityEntryBuildingSpawnState _fallbackRuntimeCityEntryBuildingSpawn = new();
        private RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper _runtimeCityRoadsideBuildingSpawnHelper;
        private readonly RuntimeCityRoadsideBuildingSpawnState _fallbackRuntimeCityRoadsideBuildingSpawn = new();
        private RuntimeCityRuralBuildingSpawnPrefabSystemHelper _runtimeCityRuralBuildingSpawnHelper;
        private readonly RuntimeCityRuralBuildingSpawnState _fallbackRuntimeCityRuralBuildingSpawn = new();
        private RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper _runtimeCityBulkBuildingSpawnRoutineHelper;
        private readonly RuntimeCityBulkBuildingSpawnRoutineState _fallbackRuntimeCityBulkBuildingSpawnRoutine = new();
        private RuntimeCityCorridorBuildingSpawnPrefabSystemHelper _runtimeCityCorridorBuildingSpawnHelper;
        private readonly RuntimeCityCorridorBuildingSpawnState _fallbackRuntimeCityCorridorBuildingSpawn = new();
        private RuntimeCityYardWallPlanUtilitySystemHelper _runtimeCityYardWallPlanHelper;
        private readonly RuntimeCityYardWallPlanState _fallbackRuntimeCityYardWallPlan = new();
        private RuntimeCityYardGateUtilitySystemHelper _runtimeCityYardGateHelper;
        private readonly RuntimeCityYardGateState _fallbackRuntimeCityYardGate = new();
        private RuntimeCityYardWallVisualPresentationSystemHelper _runtimeCityYardWallVisualHelper;
        private readonly RuntimeCityYardWallVisualState _fallbackRuntimeCityYardWallVisual = new();
        private RuntimeCityHouseYardWallPrefabSystemHelper _runtimeCityHouseYardWallHelper;
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
        private RuntimeCitySpawnBridgePrefabSystemHelper _runtimeCitySpawnBridgeHelper;
        private readonly RuntimeCitySpawnBridgeState _fallbackRuntimeCitySpawnBridge = new();
        private RuntimeCityRoadBuildBridgeCompositionSystemHelper _runtimeCityRoadBuildBridgeHelper;
        private readonly RuntimeCityRoadBuildBridgeState _fallbackRuntimeCityRoadBuildBridge = new();
        private RuntimeCityLifecycleCompositionSystemHelper _runtimeCityLifecycleHelper;
        private readonly RuntimeCityLifecycleState _fallbackRuntimeCityLifecycle = new();
        private RuntimeCityStartupSystemHelper _runtimeCityStartupHelper;
        private readonly RuntimeCityStartupState _fallbackRuntimeCityStartup = new();
        private RuntimeCityReadinessQueryCompositionSystemHelper _runtimeCityReadinessQueryHelper;
        private RuntimeCityGenerationCompositionSystemHelper _runtimeCityGenerationHelper;
        private readonly RuntimeCityGenerationState _fallbackRuntimeCityGeneration = new();
        private RuntimeCityChainUtilitySystemHelper _runtimeCityChainHelper;
        private readonly RuntimeCityChainState _fallbackRuntimeCityChain = new();
        private RuntimeCityRoadCommitCompositionSystemHelper _runtimeCityRoadCommitHelper;
        private readonly RuntimeCityRoadCommitState _fallbackRuntimeCityRoadCommit = new();
        private RuntimeCityDiagnosticsSystemHelper _runtimeCityDiagnosticSystem;
        private RuntimeCityIngressUtilitySystemHelper _runtimeCityIngressHelper;
        private readonly RuntimeCityIngressState _fallbackRuntimeCityIngress = new();
        private RuntimeCityMinimapEventUiSystemHelper _runtimeCityMinimapEventHelper;
        private RuntimeCityReadModelCompositionSystemHelper _runtimeCityReadModelSystem;
        private RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
        private readonly RuntimeCityStartupSystemHelper.TryGetPendingInitialUnitsDelegate _tryGetPendingInitialUnits;
        private readonly RuntimeCityStartupSystemHelper.TryGetRoadCellSizeDelegate _tryGetRoadCellSize;
        private readonly RuntimeCityStartupSystemHelper.TryGetGridDataDelegate _tryGetGridData;
        private RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context _runtimeCityBuildingSpawnContext;
        private bool _configured;

        private RuntimeCityConfigCompositionSystemHelper.Snapshot cityConfig => RuntimeCityConfigCompositionSystemHelper?.Current ?? _fallbackCityConfig;
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
        public bool RequiresUpdate => _configured &&
                                      (!HasSpawned ||
                                       IsGenerating ||
                                       (_runtimeCityMinimapEventHelper?.HasPendingStaticMinimapChanged ?? false));
        public RuntimeCityReadModelCompositionSystemHelper ReadModel => RuntimeCityReadModelCompositionSystemHelper;

        public RuntimeCityCompositionSystemHelper()
        {
            _fallbackCityConfig = RuntimeCityConfigCompositionSystemHelper.Snapshot.Default(_fallbackCityPrefabs);
            _tryGetPendingInitialUnits = TryGetPendingInitialUnits;
            _tryGetRoadCellSize = TryGetRoadCellSizeInGridCells;
            _tryGetGridData = TryGetGridConfig;
        }

        public string DescribeStartupBlocker(int frameCount)
        {
            return RuntimeCityStartupSystemHelper.DescribeStartupBlocker(CreateStartupContext(frameCount));
        }

        public void MarkSpawnedAfterLoadingGateTimeout()
        {
            RuntimeCityLifecycleState.MarkSpawned();
            PublishReadModel();
        }

        internal void Configure(
            RuntimeCitySpawnerSystemConfig configAsset,
            RoadRuntimeGenerationCompositionSystemHelper roadRuntimeGenerationHelper,
            RoadRuntimeGenerationCompositionSystemHelper.Context roadRuntimeGenerationContext,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawnSystem,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext,
            Transform runtimeRoot,
            IMatchRuntimeUi mainMenuPlayUi)
        {
            _configured = true;
            _config = configAsset;
            RuntimeCityRoadBuildBridgeState.Configure(roadRuntimeGenerationHelper, roadRuntimeGenerationContext);
            RuntimeCitySpawnBridgeState.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
            RuntimeCityVisualPresentationSystemHelper?.SetRuntimeRoot(runtimeRoot);
            RuntimeCityMinimapEventUiSystemHelper?.Configure(mainMenuPlayUi);
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
            RuntimeCityMinimapEventUiSystemHelper?.Flush();
            TryAutoSpawn(frameCount);
            PublishReadModel();
        }

        public void Dispose()
        {
            if (!_configured)
                return;

            (_runtimeCityLifecycleHelper?.State ?? _fallbackRuntimeCityLifecycle).CancelGeneration();
            _runtimeCityVisualPresentationSystemHelper?.Dispose();
            (_runtimeCitySpawnBridgeHelper?.State ?? _fallbackRuntimeCitySpawnBridge).Clear();
            (_runtimeCityRoadBuildBridgeHelper?.State ?? _fallbackRuntimeCityRoadBuildBridge).Clear();
            _runtimeCityReadinessQueryHelper?.Clear();
            _runtimeCityMinimapEventHelper?.Clear();
            _configured = false;
        }

        public bool IsConfiguredHousePrefab(GameObject prefab)
        {
            return RuntimeCityPrefabSelectionState.IsConfiguredPrefab(prefab, housePrefabs);
        }

        public void GenerateCity(int frameCount)
        {
            RuntimeCityStartupSystemHelper.Result result = EvaluateManualGeneration(CreateStartupContext(frameCount));
            if (result.Kind == RuntimeCityStartupSystemHelper.ResultKind.Generate)
                GenerateCity(result.Grid, result.RoadCellSizeInGridCells, frameCount);
            PublishReadModel();
        }

        private void ApplyConfigIfAvailable()
        {
            RuntimeCityConfigCompositionSystemHelper configHelper = RuntimeCityConfigCompositionSystemHelper;
            if (configHelper != null)
                configHelper.Apply(_config);
            else
                _fallbackCityConfig = RuntimeCityConfigCompositionSystemHelper.Snapshot.From(_config, _fallbackCityPrefabs);

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
                : RuntimeCityBuildingSpawnContextCompositionSystemHelper.CreateFallback(
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
            RuntimeCityStartupSystemHelper.Result result = EvaluateStartup(CreateStartupContext(frameCount));
            if (result.Kind == RuntimeCityStartupSystemHelper.ResultKind.MarkSpawned)
                RuntimeCityLifecycleState.MarkSpawned();
            else if (result.Kind == RuntimeCityStartupSystemHelper.ResultKind.Generate)
                GenerateCity(result.Grid, result.RoadCellSizeInGridCells, frameCount);
        }

        private RuntimeCityStartupSystemHelper.Result EvaluateStartup(RuntimeCityStartupSystemHelper.Context context)
        {
            RuntimeCityStartupSystemHelper startupSystem = RuntimeCityStartupSystemHelper;
            return startupSystem != null
                ? startupSystem.Evaluate(context)
                : _fallbackRuntimeCityStartup.Evaluate(context);
        }

        private RuntimeCityStartupSystemHelper.Result EvaluateManualGeneration(RuntimeCityStartupSystemHelper.Context context)
        {
            RuntimeCityStartupSystemHelper startupSystem = RuntimeCityStartupSystemHelper;
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

        private RuntimeCityLifecycleCompositionSystemHelper.Context CreateLifecycleContext(int frameCount)
        {
            return new RuntimeCityLifecycleCompositionSystemHelper.Context(
                frameCount,
                cityCount,
                generateBuildings,
                generationYieldInterval,
                RuntimeCityDiagnosticsSystemHelper);
        }

        private RuntimeCityStartupSystemHelper.Context CreateStartupContext(int frameCount)
        {
            return new RuntimeCityStartupSystemHelper.Context(
                frameCount,
                spawnOnStart,
                RuntimeCityLifecycleState.IsSpawned,
                cityCount,
                _runtimeGameplayStateSystem.PlayRequested,
                false,
                generateBuildings,
                RuntimeCityRoadBuildBridgeState.HasRoadRuntimeGenerationCompositionSystemHelper,
                RuntimeCitySpawnBridgeState.HasSpawnSystem,
                hallPrefabs,
                shopPrefabs,
                housePrefabs,
                _tryGetPendingInitialUnits,
                _tryGetRoadCellSize,
                _tryGetGridData,
                RuntimeCityDiagnosticsSystemHelper);
        }

        private RuntimeCityGenerationCompositionSystemHelper.Context CreateGenerationContext(GridConfig grid, int roadCellSizeInGridCells, int frameCount)
        {
            return new RuntimeCityGenerationCompositionSystemHelper.Context(
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
                RuntimeCityMinimapEventUiSystemHelper,
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

        private RuntimeCityRoadCommitCompositionSystemHelper.Context CreateRoadCommitContext()
        {
            return new RuntimeCityRoadCommitCompositionSystemHelper.Context(
                RuntimeCityRoadBuildBridgeState,
                RuntimeCityDiagnosticsSystemHelper);
        }

        private RuntimeCityIngressUtilitySystemHelper.Context CreateIngressContext()
        {
            return new RuntimeCityIngressUtilitySystemHelper.Context(
                cityConfig,
                RuntimeCityRoadLayoutState);
        }

        private RuntimeCityVisualPresentationSystemHelper RuntimeCityVisualPresentationSystemHelper =>
            _runtimeCityVisualPresentationSystemHelper ??= ResolveRuntimeCityVisualPresentationSystemHelper();

        private RuntimeCityMinimapEventUiSystemHelper RuntimeCityMinimapEventUiSystemHelper =>
            _runtimeCityMinimapEventHelper ??= ResolveRuntimeCityMinimapEventUiSystemHelper();

        private RuntimeCityReadinessQueryCompositionSystemHelper RuntimeCityReadinessQueryCompositionSystemHelper =>
            _runtimeCityReadinessQueryHelper ??= new RuntimeCityReadinessQueryCompositionSystemHelper();

        private RuntimeCityReadModelCompositionSystemHelper RuntimeCityReadModelCompositionSystemHelper =>
            _runtimeCityReadModelSystem ??= new RuntimeCityReadModelCompositionSystemHelper();

        private RuntimeCityConfigCompositionSystemHelper RuntimeCityConfigCompositionSystemHelper =>
            _runtimeCityConfigHelper ??= ResolveRuntimeCityConfigCompositionSystemHelper();

        private RuntimeCityDiagnosticsSystemHelper RuntimeCityDiagnosticsSystemHelper =>
            _runtimeCityDiagnosticSystem ??= new RuntimeCityDiagnosticsSystemHelper();

        private RuntimeCityBuildingSpawnContextCompositionSystemHelper RuntimeCityBuildingSpawnContextHelper =>
            _runtimeCityBuildingSpawnContextHelper ??= new RuntimeCityBuildingSpawnContextCompositionSystemHelper();

        private RuntimeCityStartupSystemHelper RuntimeCityStartupSystemHelper =>
            _runtimeCityStartupHelper ??= ResolveRuntimeCityStartupSystemHelper();

        private RuntimeCityLifecycleState RuntimeCityLifecycleState =>
            RuntimeCityLifecycleCompositionSystemHelper?.State ?? _fallbackRuntimeCityLifecycle;

        private RuntimeCityLifecycleCompositionSystemHelper RuntimeCityLifecycleCompositionSystemHelper =>
            _runtimeCityLifecycleHelper ??= ResolveRuntimeCityLifecycleCompositionSystemHelper();

        private RuntimeCityLayoutState RuntimeCityLayoutState =>
            RuntimeCityLayoutUtilitySystemHelper?.State ?? _fallbackRuntimeCityLayout;

        private RuntimeCityLayoutUtilitySystemHelper RuntimeCityLayoutUtilitySystemHelper =>
            _runtimeCityLayoutHelper ??= ResolveRuntimeCityLayoutUtilitySystemHelper();

        private RuntimeCityRoadLayoutState RuntimeCityRoadLayoutState =>
            RuntimeCityRoadLayoutUtilitySystemHelper?.State ?? _fallbackRuntimeCityRoadLayout;

        private RuntimeCityRoadLayoutUtilitySystemHelper RuntimeCityRoadLayoutUtilitySystemHelper =>
            _runtimeCityRoadLayoutHelper ??= ResolveRuntimeCityRoadLayoutUtilitySystemHelper();

        private RuntimeCityWalkabilityState RuntimeCityWalkabilityState =>
            RuntimeCityWalkabilityUtilitySystemHelper?.State ?? _fallbackRuntimeCityWalkability;

        private RuntimeCityWalkabilityUtilitySystemHelper RuntimeCityWalkabilityUtilitySystemHelper =>
            _runtimeCityWalkabilityHelper ??= ResolveRuntimeCityWalkabilityUtilitySystemHelper();

        private RuntimeCityBuildingPlotState RuntimeCityBuildingPlotState =>
            RuntimeCityBuildingPlotHelper?.State ?? _fallbackRuntimeCityBuildingPlot;

        private RuntimeCityBuildingPlotUtilitySystemHelper RuntimeCityBuildingPlotHelper =>
            _runtimeCityBuildingPlotHelper ??= ResolveRuntimeCityBuildingPlotHelper();

        private RuntimeCityBulkPlotPlanState RuntimeCityBulkPlotPlanState =>
            RuntimeCityBulkPlotPlanHelper?.State ?? _fallbackRuntimeCityBulkPlotPlan;

        private RuntimeCityBulkPlotPlanUtilitySystemHelper RuntimeCityBulkPlotPlanHelper =>
            _runtimeCityBulkPlotPlanHelper ??= ResolveRuntimeCityBulkPlotPlanHelper();

        private RuntimeCityPrefabSelectionState RuntimeCityPrefabSelectionState =>
            RuntimeCityPrefabSelectionPrefabSystemHelper?.State ?? _fallbackRuntimeCityPrefabSelection;

        private RuntimeCityPrefabSelectionPrefabSystemHelper RuntimeCityPrefabSelectionPrefabSystemHelper =>
            _runtimeCityPrefabSelectionHelper ??= ResolveRuntimeCityPrefabSelectionPrefabSystemHelper();

        private RuntimeCityLandmarkOffsetState RuntimeCityLandmarkOffsetState =>
            RuntimeCityLandmarkOffsetUtilitySystemHelper?.State ?? _fallbackRuntimeCityLandmarkOffset;

        private RuntimeCityLandmarkOffsetUtilitySystemHelper RuntimeCityLandmarkOffsetUtilitySystemHelper =>
            _runtimeCityLandmarkOffsetHelper ??= ResolveRuntimeCityLandmarkOffsetUtilitySystemHelper();

        private RuntimeCityHallSpawnState RuntimeCityHallSpawnState =>
            RuntimeCityHallSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityHallSpawn;

        private RuntimeCityHallSpawnPrefabSystemHelper RuntimeCityHallSpawnPrefabSystemHelper =>
            _runtimeCityHallSpawnHelper ??= ResolveRuntimeCityHallSpawnPrefabSystemHelper();

        private RuntimeCityLandmarkSpawnState RuntimeCityLandmarkSpawnState =>
            RuntimeCityLandmarkSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityLandmarkSpawn;

        private RuntimeCityLandmarkSpawnPrefabSystemHelper RuntimeCityLandmarkSpawnPrefabSystemHelper =>
            _runtimeCityLandmarkSpawnHelper ??= ResolveRuntimeCityLandmarkSpawnPrefabSystemHelper();

        private RuntimeCityEntryBuildingSpawnState RuntimeCityEntryBuildingSpawnState =>
            RuntimeCityEntryBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityEntryBuildingSpawn;

        private RuntimeCityEntryBuildingSpawnPrefabSystemHelper RuntimeCityEntryBuildingSpawnPrefabSystemHelper =>
            _runtimeCityEntryBuildingSpawnHelper ??= ResolveRuntimeCityEntryBuildingSpawnPrefabSystemHelper();

        private RuntimeCityRoadsideBuildingSpawnState RuntimeCityRoadsideBuildingSpawnState =>
            RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityRoadsideBuildingSpawn;

        private RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper =>
            _runtimeCityRoadsideBuildingSpawnHelper ??= ResolveRuntimeCityRoadsideBuildingSpawnPrefabSystemHelper();

        private RuntimeCityRuralBuildingSpawnState RuntimeCityRuralBuildingSpawnState =>
            RuntimeCityRuralBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityRuralBuildingSpawn;

        private RuntimeCityRuralBuildingSpawnPrefabSystemHelper RuntimeCityRuralBuildingSpawnPrefabSystemHelper =>
            _runtimeCityRuralBuildingSpawnHelper ??= ResolveRuntimeCityRuralBuildingSpawnPrefabSystemHelper();

        private RuntimeCityBulkBuildingSpawnRoutineState RuntimeCityBulkBuildingSpawnRoutineState =>
            RuntimeCityBulkBuildingSpawnRoutineHelper?.State ?? _fallbackRuntimeCityBulkBuildingSpawnRoutine;

        private RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper RuntimeCityBulkBuildingSpawnRoutineHelper =>
            _runtimeCityBulkBuildingSpawnRoutineHelper ??= ResolveRuntimeCityBulkBuildingSpawnRoutineHelper();

        private RuntimeCityCorridorBuildingSpawnState RuntimeCityCorridorBuildingSpawnState =>
            RuntimeCityCorridorBuildingSpawnPrefabSystemHelper?.State ?? _fallbackRuntimeCityCorridorBuildingSpawn;

        private RuntimeCityCorridorBuildingSpawnPrefabSystemHelper RuntimeCityCorridorBuildingSpawnPrefabSystemHelper =>
            _runtimeCityCorridorBuildingSpawnHelper ??= ResolveRuntimeCityCorridorBuildingSpawnPrefabSystemHelper();

        private RuntimeCityYardWallPlanState RuntimeCityYardWallPlanState =>
            RuntimeCityYardWallPlanUtilitySystemHelper?.State ?? _fallbackRuntimeCityYardWallPlan;

        private RuntimeCityYardWallPlanUtilitySystemHelper RuntimeCityYardWallPlanUtilitySystemHelper =>
            _runtimeCityYardWallPlanHelper ??= ResolveRuntimeCityYardWallPlanUtilitySystemHelper();

        private RuntimeCityYardGateState RuntimeCityYardGateState =>
            RuntimeCityYardGateUtilitySystemHelper?.State ?? _fallbackRuntimeCityYardGate;

        private RuntimeCityYardGateUtilitySystemHelper RuntimeCityYardGateUtilitySystemHelper =>
            _runtimeCityYardGateHelper ??= ResolveRuntimeCityYardGateUtilitySystemHelper();

        private RuntimeCityYardWallVisualState RuntimeCityYardWallVisualState =>
            RuntimeCityYardWallVisualPresentationSystemHelper?.State ?? _fallbackRuntimeCityYardWallVisual;

        private RuntimeCityYardWallVisualPresentationSystemHelper RuntimeCityYardWallVisualPresentationSystemHelper =>
            _runtimeCityYardWallVisualHelper ??= ResolveRuntimeCityYardWallVisualPresentationSystemHelper();

        private RuntimeCityHouseYardWallState RuntimeCityHouseYardWallState =>
            RuntimeCityHouseYardWallPrefabSystemHelper?.State ?? _fallbackRuntimeCityHouseYardWall;

        private RuntimeCityHouseYardWallPrefabSystemHelper RuntimeCityHouseYardWallPrefabSystemHelper =>
            _runtimeCityHouseYardWallHelper ??= ResolveRuntimeCityHouseYardWallPrefabSystemHelper();

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
            RuntimeCitySpawnBridgePrefabSystemHelper?.State ?? _fallbackRuntimeCitySpawnBridge;

        private RuntimeCitySpawnBridgePrefabSystemHelper RuntimeCitySpawnBridgePrefabSystemHelper =>
            _runtimeCitySpawnBridgeHelper ??= ResolveRuntimeCitySpawnBridgePrefabSystemHelper();

        private RuntimeCityRoadBuildBridgeState RuntimeCityRoadBuildBridgeState =>
            RuntimeCityRoadBuildBridgeCompositionSystemHelper?.State ?? _fallbackRuntimeCityRoadBuildBridge;

        private RuntimeCityRoadBuildBridgeCompositionSystemHelper RuntimeCityRoadBuildBridgeCompositionSystemHelper =>
            _runtimeCityRoadBuildBridgeHelper ??= ResolveRuntimeCityRoadBuildBridgeCompositionSystemHelper();

        private RuntimeCityGenerationState RuntimeCityGenerationState =>
            RuntimeCityGenerationCompositionSystemHelper?.State ?? _fallbackRuntimeCityGeneration;

        private RuntimeCityGenerationCompositionSystemHelper RuntimeCityGenerationCompositionSystemHelper =>
            _runtimeCityGenerationHelper ??= ResolveRuntimeCityGenerationCompositionSystemHelper();

        private RuntimeCityChainState RuntimeCityChainState =>
            RuntimeCityChainHelper?.State ?? _fallbackRuntimeCityChain;

        private RuntimeCityChainUtilitySystemHelper RuntimeCityChainHelper =>
            _runtimeCityChainHelper ??= ResolveRuntimeCityChainHelper();

        private RuntimeCityRoadCommitState RuntimeCityRoadCommitState =>
            RuntimeCityRoadCommitCompositionSystemHelper?.State ?? _fallbackRuntimeCityRoadCommit;

        private RuntimeCityRoadCommitCompositionSystemHelper RuntimeCityRoadCommitCompositionSystemHelper =>
            _runtimeCityRoadCommitHelper ??= ResolveRuntimeCityRoadCommitCompositionSystemHelper();

        private RuntimeCityIngressState RuntimeCityIngressState =>
            RuntimeCityIngressUtilitySystemHelper?.State ?? _fallbackRuntimeCityIngress;

        private RuntimeCityIngressUtilitySystemHelper RuntimeCityIngressUtilitySystemHelper =>
            _runtimeCityIngressHelper ??= ResolveRuntimeCityIngressUtilitySystemHelper();

        private bool TryGetPendingInitialUnits(out int totalConfigs, out int initializedConfigs)
        {
            RuntimeCityReadinessQueryCompositionSystemHelper readinessQueryHelper = RuntimeCityReadinessQueryCompositionSystemHelper;
            if (readinessQueryHelper == null)
            {
                totalConfigs = 0;
                initializedConfigs = 0;
                return false;
            }

            return readinessQueryHelper.HasPendingInitialUnitsSpawn(out totalConfigs, out initializedConfigs);
        }

        private bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
        {
            return RuntimeCityRoadBuildBridgeState.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
        }

        private bool TryGetGridConfig(out GridConfig grid)
        {
            RuntimeCityReadinessQueryCompositionSystemHelper readinessQueryHelper = RuntimeCityReadinessQueryCompositionSystemHelper;
            if (readinessQueryHelper == null)
            {
                grid = default;
                return false;
            }

            return readinessQueryHelper.TryGetGridConfig(out grid);
        }

        private List<RectInt> CollectInitialBaseExclusionRoadRects(int roadCellSizeInGridCells)
        {
            return RuntimeCityReadinessQueryCompositionSystemHelper?.CollectInitialBaseExclusionRoadRects(roadCellSizeInGridCells) ??
                new List<RectInt>();
        }

        private static RuntimeCityVisualPresentationSystemHelper ResolveRuntimeCityVisualPresentationSystemHelper()
        {
            return new RuntimeCityVisualPresentationSystemHelper();
        }

        private static RuntimeCityMinimapEventUiSystemHelper ResolveRuntimeCityMinimapEventUiSystemHelper()
        {
            return new RuntimeCityMinimapEventUiSystemHelper();
        }

        private static RuntimeCityConfigCompositionSystemHelper ResolveRuntimeCityConfigCompositionSystemHelper()
        {
            return new RuntimeCityConfigCompositionSystemHelper();
        }

        private static RuntimeCityStartupSystemHelper ResolveRuntimeCityStartupSystemHelper()
        {
            return new RuntimeCityStartupSystemHelper();
        }

        private static RuntimeCityLifecycleCompositionSystemHelper ResolveRuntimeCityLifecycleCompositionSystemHelper()
        {
            return new RuntimeCityLifecycleCompositionSystemHelper();
        }

        private static RuntimeCityLayoutUtilitySystemHelper ResolveRuntimeCityLayoutUtilitySystemHelper()
        {
            return new RuntimeCityLayoutUtilitySystemHelper();
        }

        private static RuntimeCityRoadLayoutUtilitySystemHelper ResolveRuntimeCityRoadLayoutUtilitySystemHelper()
        {
            return new RuntimeCityRoadLayoutUtilitySystemHelper();
        }

        private static RuntimeCityWalkabilityUtilitySystemHelper ResolveRuntimeCityWalkabilityUtilitySystemHelper()
        {
            return new RuntimeCityWalkabilityUtilitySystemHelper();
        }

        private static RuntimeCityBuildingPlotUtilitySystemHelper ResolveRuntimeCityBuildingPlotHelper()
        {
            return new RuntimeCityBuildingPlotUtilitySystemHelper();
        }

        private static RuntimeCityBulkPlotPlanUtilitySystemHelper ResolveRuntimeCityBulkPlotPlanHelper()
        {
            return new RuntimeCityBulkPlotPlanUtilitySystemHelper();
        }

        private static RuntimeCityPrefabSelectionPrefabSystemHelper ResolveRuntimeCityPrefabSelectionPrefabSystemHelper()
        {
            return new RuntimeCityPrefabSelectionPrefabSystemHelper();
        }

        private static RuntimeCityLandmarkOffsetUtilitySystemHelper ResolveRuntimeCityLandmarkOffsetUtilitySystemHelper()
        {
            return new RuntimeCityLandmarkOffsetUtilitySystemHelper();
        }

        private static RuntimeCityHallSpawnPrefabSystemHelper ResolveRuntimeCityHallSpawnPrefabSystemHelper()
        {
            return new RuntimeCityHallSpawnPrefabSystemHelper();
        }

        private static RuntimeCityLandmarkSpawnPrefabSystemHelper ResolveRuntimeCityLandmarkSpawnPrefabSystemHelper()
        {
            return new RuntimeCityLandmarkSpawnPrefabSystemHelper();
        }

        private static RuntimeCityEntryBuildingSpawnPrefabSystemHelper ResolveRuntimeCityEntryBuildingSpawnPrefabSystemHelper()
        {
            return new RuntimeCityEntryBuildingSpawnPrefabSystemHelper();
        }

        private static RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper ResolveRuntimeCityRoadsideBuildingSpawnPrefabSystemHelper()
        {
            return new RuntimeCityRoadsideBuildingSpawnPrefabSystemHelper();
        }

        private static RuntimeCityRuralBuildingSpawnPrefabSystemHelper ResolveRuntimeCityRuralBuildingSpawnPrefabSystemHelper()
        {
            return new RuntimeCityRuralBuildingSpawnPrefabSystemHelper();
        }

        private static RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper ResolveRuntimeCityBulkBuildingSpawnRoutineHelper()
        {
            return new RuntimeCityBulkBuildingSpawnRoutinePrefabSystemHelper();
        }

        private static RuntimeCityCorridorBuildingSpawnPrefabSystemHelper ResolveRuntimeCityCorridorBuildingSpawnPrefabSystemHelper()
        {
            return new RuntimeCityCorridorBuildingSpawnPrefabSystemHelper();
        }

        private static RuntimeCityYardWallPlanUtilitySystemHelper ResolveRuntimeCityYardWallPlanUtilitySystemHelper()
        {
            return new RuntimeCityYardWallPlanUtilitySystemHelper();
        }

        private static RuntimeCityYardGateUtilitySystemHelper ResolveRuntimeCityYardGateUtilitySystemHelper()
        {
            return new RuntimeCityYardGateUtilitySystemHelper();
        }

        private static RuntimeCityYardWallVisualPresentationSystemHelper ResolveRuntimeCityYardWallVisualPresentationSystemHelper()
        {
            return new RuntimeCityYardWallVisualPresentationSystemHelper();
        }

        private static RuntimeCityHouseYardWallPrefabSystemHelper ResolveRuntimeCityHouseYardWallPrefabSystemHelper()
        {
            return new RuntimeCityHouseYardWallPrefabSystemHelper();
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

        private static RuntimeCitySpawnBridgePrefabSystemHelper ResolveRuntimeCitySpawnBridgePrefabSystemHelper()
        {
            return new RuntimeCitySpawnBridgePrefabSystemHelper();
        }

        private static RuntimeCityRoadBuildBridgeCompositionSystemHelper ResolveRuntimeCityRoadBuildBridgeCompositionSystemHelper()
        {
            return new RuntimeCityRoadBuildBridgeCompositionSystemHelper();
        }

        private static RuntimeCityBuildingPlacementPrefabSystemHelper ResolveRuntimeCityBuildingPlacementHelper()
        {
            return new RuntimeCityBuildingPlacementPrefabSystemHelper();
        }

        private static RuntimeCityGenerationCompositionSystemHelper ResolveRuntimeCityGenerationCompositionSystemHelper()
        {
            return new RuntimeCityGenerationCompositionSystemHelper();
        }

        private static RuntimeCityChainUtilitySystemHelper ResolveRuntimeCityChainHelper()
        {
            return new RuntimeCityChainUtilitySystemHelper();
        }

        private static RuntimeCityRoadCommitCompositionSystemHelper ResolveRuntimeCityRoadCommitCompositionSystemHelper()
        {
            return new RuntimeCityRoadCommitCompositionSystemHelper();
        }

        private static RuntimeCityIngressUtilitySystemHelper ResolveRuntimeCityIngressUtilitySystemHelper()
        {
            return new RuntimeCityIngressUtilitySystemHelper();
        }
    }
}
