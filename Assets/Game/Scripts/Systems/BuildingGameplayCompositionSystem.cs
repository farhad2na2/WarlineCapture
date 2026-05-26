using System;
using Game.Scripts.UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class BuildingGameplayCompositionSystem
{
    private const float DestroyedBuildingLifetimeSeconds = 5f;
    private const float OilBarrelsPerFuelBarrel = 2f;
    private readonly BuildingPlacementRuntimeTickContextSystem _runtimeTickContextSystem = new();
    private MaterialPropertyBlock _markerPropertyBlock;

    public readonly struct Result
    {
        public readonly BuildingSelectionClickSystem SelectionClick;
        public readonly BuildingSelectionClickSystem.Context SelectionClickContext;
        public readonly BuildingRuntimeUpdateSystem RuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context RuntimeUpdateContext;
        public readonly BuildingRuntimeCitySpawnSystem RuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context RuntimeCitySpawnContext;
        public readonly BuildingRuntimeQuerySystem RuntimeQuery;
        public readonly BuildingRuntimeQuerySystem.Context RuntimeQueryContext;
        public readonly BuildingRuntimeSpawnCommandSystem RuntimeSpawnCommand;
        public readonly BuildingRuntimeSpawnCommandSystem.Context RuntimeSpawnCommandContext;
        public readonly BuildingSpawnSystem Spawn;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly Func<BuildingSpawnSystem.Context> CreateSpawnContext;
        public readonly BuildingBarrierSystem Barrier;
        public readonly Func<BuildingBarrierSystem.Context> CreateBarrierContext;
        public readonly BuildingCombatSystem Combat;
        public readonly Func<BuildingCombatSystem.Context<RuntimeBuildingData>> CreateCombatContext;
        public readonly BuildingUiCommandSystem UiCommand;
        public readonly BuildingUiCommandSystem.Context UiCommandContext;
        public readonly BuildingUiQuerySystem UiQuery;
        public readonly BuildingUiQuerySystem.Context UiQueryContext;
        public readonly BuildingPlacementInteractionSystem Interaction;
        public readonly BuildingPlacementInteractionSystem.Context InteractionContext;
        private readonly BuildingGameplayDependencySystem DependencySystem;
        private readonly BuildingRuntimeResourcePrefabContextSystem RuntimeResourcePrefabContextSystem;
        private readonly BuildingRuntimeResourcePrefabContextSystem.Source RuntimeResourcePrefabSource;
        private readonly CitizenPopulationCompositionSystem CitizenPopulationCompositionBoundary;
        public readonly CitizenPopulationCompositionSystem.Result CitizenPopulationComposition;
        public readonly System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly Action<MainMenuPlayUI> BindMainMenu;
        public readonly Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> BindGameplayFeatures;
        public readonly Action Dispose;

        public Result(
            BuildingSelectionClickSystem selectionClick,
            BuildingSelectionClickSystem.Context selectionClickContext,
            BuildingRuntimeUpdateSystem runtimeUpdate,
            BuildingRuntimeUpdateSystem.Context runtimeUpdateContext,
            BuildingRuntimeCitySpawnSystem runtimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context runtimeCitySpawnContext,
            BuildingRuntimeQuerySystem runtimeQuery,
            BuildingRuntimeQuerySystem.Context runtimeQueryContext,
            BuildingRuntimeSpawnCommandSystem runtimeSpawnCommand,
            BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext,
            BuildingSpawnSystem spawn,
            BuildingSpawnSystem.Context spawnContext,
            Func<BuildingSpawnSystem.Context> createSpawnContext,
            BuildingBarrierSystem barrier,
            Func<BuildingBarrierSystem.Context> createBarrierContext,
            BuildingCombatSystem combat,
            Func<BuildingCombatSystem.Context<RuntimeBuildingData>> createCombatContext,
            BuildingUiCommandSystem uiCommand,
            BuildingUiCommandSystem.Context uiCommandContext,
            BuildingUiQuerySystem uiQuery,
            BuildingUiQuerySystem.Context uiQueryContext,
            BuildingPlacementInteractionSystem interaction,
            BuildingPlacementInteractionSystem.Context interactionContext,
            BuildingGameplayDependencySystem dependencySystem,
            BuildingRuntimeResourcePrefabContextSystem runtimeResourcePrefabContextSystem,
            BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource,
            CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
            CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
            System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            Action<MainMenuPlayUI> bindMainMenu,
            Action<MainMenuPlayUI, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindGameplayFeatures,
            Action dispose)
        {
            SelectionClick = selectionClick;
            SelectionClickContext = selectionClickContext;
            RuntimeUpdate = runtimeUpdate;
            RuntimeUpdateContext = runtimeUpdateContext;
            RuntimeCitySpawn = runtimeCitySpawn;
            RuntimeCitySpawnContext = runtimeCitySpawnContext;
            RuntimeQuery = runtimeQuery;
            RuntimeQueryContext = runtimeQueryContext;
            RuntimeSpawnCommand = runtimeSpawnCommand;
            RuntimeSpawnCommandContext = runtimeSpawnCommandContext;
            Spawn = spawn;
            SpawnContext = spawnContext;
            CreateSpawnContext = createSpawnContext;
            Barrier = barrier;
            CreateBarrierContext = createBarrierContext;
            Combat = combat;
            CreateCombatContext = createCombatContext;
            UiCommand = uiCommand;
            UiCommandContext = uiCommandContext;
            UiQuery = uiQuery;
            UiQueryContext = uiQueryContext;
            Interaction = interaction;
            InteractionContext = interactionContext;
            DependencySystem = dependencySystem;
            RuntimeResourcePrefabContextSystem = runtimeResourcePrefabContextSystem;
            RuntimeResourcePrefabSource = runtimeResourcePrefabSource;
            CitizenPopulationCompositionBoundary = citizenPopulationCompositionBoundary;
            CitizenPopulationComposition = citizenPopulationComposition;
            RuntimeBuildings = runtimeBuildings;
            BindMainMenu = bindMainMenu;
            BindGameplayFeatures = bindGameplayFeatures;
            Dispose = dispose;
        }

        public void BindSelection(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)
        {
            DependencySystem?.BindRuntimeDependencies(
                null,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem);
        }

        public void InitializeCitizenPopulation(DayNightSystem dayNight, Camera worldCamera)
        {
            CitizenResourceSystem.Context resourceContext = RuntimeResourcePrefabContextSystem.CreateCitizenResourceContext(RuntimeResourcePrefabSource);
            CitizenPrefabSystem.Context prefabContext = RuntimeResourcePrefabContextSystem.CreateCitizenPrefabContext(RuntimeResourcePrefabSource);
            CitizenPopulationCompositionBoundary.Init(
                CitizenPopulationComposition,
                RuntimeQuery,
                RuntimeQueryContext,
                dayNight,
                worldCamera,
                resourceContext,
                prefabContext);
        }

        public void DisposeCitizenPopulation()
        {
            CitizenPopulationCompositionBoundary.Dispose(CitizenPopulationComposition);
        }

        public void BindCitizenPopulation(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
            CitizenPopulationEventSystem citizenPopulationEventSystem)
        {
            DependencySystem?.BindRuntimeDependencies(
                null,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                citizenPopulationEventSystem: citizenPopulationEventSystem);
        }
    }

    public Result Initialize(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadFootprintQuerySystem roadFootprintQuerySystem,
        RoadFootprintQuerySystem.Context roadFootprintQueryContext,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight)
    {
        MaterialPropertyBlock markerPropertyBlock = GetMarkerPropertyBlock();
        BuildingGameplayCompositionSourceSystem childSystems = CreateChildSystems();
        childSystems.RuntimeResourceSystem.SetInitialDollars(ResolveInitialDollars(buildingPlacementConfig));
        childSystems.BuildingGameplayDependencySystem.SetStartupDependencies(
            null,
            factionVisuals,
            dayNight);
        childSystems.BuildingPlacementStartupSystem.ConfigureRoadFootprintQuery(
            roadFootprintQuerySystem,
            roadFootprintQueryContext);
        childSystems.BuildingPlacementStartupSystem.Init(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            childSystems.BuildingDefinitionSystem,
            childSystems.BuildingRunwaySystem,
            childSystems.BuildingPlacementPreviewSystem,
            childSystems.BuildingRuntimeObjectSystem.DestroyRuntimeObject);
        BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource =
            CreateRuntimeResourcePrefabSource(childSystems);
        BuildingPlacementInteractionSystem.Context interactionContext = default;
        interactionContext = CreateBuildingPlacementInteractionContext(
            childSystems,
            () => interactionContext,
            markerPropertyBlock);
        BuildingRuntimeContextSystem.Source buildingRuntimeContextSource =
            CreateBuildingRuntimeContextSource(childSystems, interactionContext, markerPropertyBlock);

        var runtimeUpdate = new BuildingRuntimeUpdateSystem();
        BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext =
            childSystems.BuildingRuntimeContextSystem.CreateSpawnCommandContext(
                buildingRuntimeContextSource,
                childSystems.BuildingRuntimeSpawnSystem,
                childSystems.BuildingPlacementStartupSystem.SoldierBaseDefinition,
                childSystems.BuildingPlacementStartupSystem.SoldierTentDefinition,
                childSystems.BuildingPlacementStartupSystem.FactoryDefinition);
        Func<BuildingSpawnSystem.Context> createSpawnContext = () =>
        {
            if (TryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(CreateRuntimeContextSource(childSystems));
        };
        Func<BuildingBarrierSystem.Context> createBarrierContext = () =>
        {
            if (TryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(childSystems));
        };
        Func<BuildingCombatSystem.Context<RuntimeBuildingData>> createCombatContext = () =>
        {
            if (TryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateCombatContext(CreateRuntimeContextSource(childSystems));
        };
        return new Result(
            childSystems.BuildingSelectionClickSystem,
            CreateBuildingSelectionClickContext(childSystems),
            runtimeUpdate,
            new BuildingRuntimeUpdateSystem.Context(
                () => childSystems.BuildingPlacementRuntimeTickSystem.Update(_runtimeTickContextSystem.Create(CreateRuntimeTickSource(childSystems, interactionContext, markerPropertyBlock)))),
            childSystems.BuildingRuntimeCitySpawnSystem,
            childSystems.BuildingRuntimeContextSystem.CreateCitySpawnContext(
                buildingRuntimeContextSource,
                childSystems.BuildingRuntimeSpawnCommandSystem,
                runtimeSpawnCommandContext),
            childSystems.BuildingRuntimeQuerySystem,
            childSystems.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(childSystems)),
            childSystems.BuildingRuntimeSpawnCommandSystem,
            runtimeSpawnCommandContext,
            childSystems.BuildingSpawnSystem,
            createSpawnContext(),
            createSpawnContext,
            childSystems.BuildingBarrierSystem,
            createBarrierContext,
            childSystems.BuildingCombatSystem,
            createCombatContext,
            childSystems.BuildingUiCommandSystem,
            childSystems.BuildingUiContextSystem.CreateCommandContext(CreateBuildingUiContextSource(childSystems, interactionContext, markerPropertyBlock)),
            childSystems.BuildingUiQuerySystem,
            childSystems.BuildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource(childSystems, interactionContext, markerPropertyBlock)),
            childSystems.BuildingPlacementInteractionSystem,
            interactionContext,
            childSystems.BuildingGameplayDependencySystem,
            childSystems.BuildingRuntimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            new CitizenPopulationCompositionSystem(),
            CitizenPopulationCompositionSystem.Create(),
            childSystems.RuntimeBuildingSystem.Buildings,
            mainMenu => childSystems.BuildingGameplayDependencySystem.BindRuntimeDependencies(mainMenu, dayNight),
            (mainMenu, selectionUiCameraSystem, selectionBuildingInteractionSystem, runtimeGridBlockers, runtimeCity, citizenPopulationEventSystem) =>
                childSystems.BuildingGameplayDependencySystem.BindRuntimeDependencies(
                    mainMenu,
                    dayNight,
                    selectionUiCameraSystem,
                    selectionBuildingInteractionSystem,
                    runtimeGridBlockers,
                    runtimeCity,
                    citizenPopulationEventSystem),
            () => childSystems.BuildingGameplayDisposalSystem.Dispose(CreateDisposalSource(childSystems, interactionContext, markerPropertyBlock)));
    }

    internal static int ResolveInitialDollars(BuildingPlacementSystemConfig buildingPlacementConfig)
    {
        return buildingPlacementConfig != null && buildingPlacementConfig.InitialUnitsConfig != null
            ? buildingPlacementConfig.InitialUnitsConfig.InitialDollars
            : 0;
    }

    private MaterialPropertyBlock GetMarkerPropertyBlock()
    {
        _markerPropertyBlock ??= new MaterialPropertyBlock();
        return _markerPropertyBlock;
    }

    private static BuildingRuntimeResourcePrefabContextSystem.Source CreateRuntimeResourcePrefabSource(
        BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingRuntimeResourcePrefabContextSystem.CreateSource(
            source.RuntimeResourceSystem,
            source.RuntimeUnitPrefabSystem,
            source.BuildingDefinitionSystem,
            source.RuntimeBuildingSystem,
            source.BuildingSpawnPrefabSystem,
            TryGetEntityManager,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            source.BuildingGameplayEcsQuerySystem.UnitPrefabRegistryQuery,
            source.BuildingGameplayEcsQuerySystem.SpawnPrefabCandidatesQuery,
            source.BuildingGameplayEcsQuerySystem.LivePlayerUnitsQuery,
            () => CreateRuntimeResourcePrefabSource(source));
    }

    internal static BuildingGameplayCompositionSourceSystem CreateChildSystems()
    {
        return new BuildingGameplayCompositionSourceSystem();
    }

    private static BuildingGameplayDisposalSystem.Source CreateDisposalSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingGameplayDisposalSystem.Source(
            source.RuntimeBuildingSystem,
            source.BuildingPlacementStartupSystem,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementPreviewSystem,
            source.BuildingRuntimeObjectSystem,
            () => source.BuildingPlacementCommandSystem.ExitBuildMode(CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock)));
    }

    internal static BuildingPlacementRuntimeTickContextSystem.Source CreateRuntimeTickSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = CreateRuntimeContextSource(source);
        BuildingRuntimeVisualSystem.Context runtimeVisualContext = source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(runtimeSource);
        BuildingCombatSystem.Context<RuntimeBuildingData> combatContext = source.BuildingRuntimeContextSystem.CreateCombatContext(runtimeSource);
        BuildingBarrierSystem.Context barrierContext = source.BuildingRuntimeContextSystem.CreateBarrierContext(runtimeSource);
        BuildingPlacementInputRuntimeTickSystem.Context inputContext = CreateInputRuntimeTickContext(source, interactionContext, markerPropertyBlock);
        return new BuildingPlacementRuntimeTickContextSystem.Source(
            CreateProductionRuntimeTickContext(source),
            CreateRuntimeBoundaryPublishContext(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeVisualSystem.UpdateBuildingResourceVisuals(runtimeVisualContext, Time.time),
            () => source.BuildingCombatSystem.SyncDestroyedRuntimeBuildingCombatEntities(
                combatContext,
                Time.time,
                DestroyedBuildingLifetimeSeconds),
            () => source.BuildingCombatSystem.UpdateDestroyedBuildings(combatContext, Time.time),
            () => source.BuildingBarrierSystem.UpdateRoadBarrierDoors(barrierContext, Time.deltaTime),
            () => source.BuildingPlacementRedirectSystem.FlushPendingMarkerRefresh(
                () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(runtimeVisualContext)),
            () => source.BuildingPlacementInputRuntimeTickSystem.Update(inputContext),
            CreateRuntimeTickDiagnosticsContext(source));
    }

    private static BuildingPlacementInputRuntimeTickSystem.Context CreateInputRuntimeTickContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingPlacementInputRuntimeTickSystem.Context(
            () => source.BuildingPlacementStartupSystem.WorldCamera,
            () => source.BuildingPlacementLifecycleSystem.ActivePlacement,
            source.BuildingPlacementInputSystem,
            CreateActivePlacementPointerContext(source, interactionContext, markerPropertyBlock),
            () => source.RuntimeGameplayStateSystem.PlayRequested,
            () => source.RuntimeGameplayStateSystem.BuildModeActive,
            source.BuildingPlacementPreviewSystem,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            source.RuntimeGameplayStateSystem,
            () => source.BuildingGameplayDependencySystem.MainMenuPlayUi,
            source.BuildingSelectionClickSystem,
            CreateBuildingSelectionClickContext(source));
    }

    private static BuildingProductionRuntimeTickSystem.Context CreateProductionRuntimeTickContext(
        BuildingGameplayCompositionSourceSystem source)
    {
        BuildingProductionContextSystem.Source productionSource = CreateProductionRuntimeContextSource(source);
        return new BuildingProductionRuntimeTickSystem.Context(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingGameplayDependencySystem.DayNightSystem,
            source.FactionResourceSystem,
            source.BuildingProductionUpdateSystem,
            source.BuildingProductionContextSystem.CreateProductionUpdateContext(productionSource),
            source.BuildingResourceHaulerBridgeSystem,
            source.BuildingProductionContextSystem.CreateResourceHaulerBridgeContext(productionSource),
            source.BuildingSpawnSystem,
            () => source.BuildingSpawnSystem.BuildingSpawnRandomState,
            value => source.BuildingSpawnSystem.BuildingSpawnRandomState = value,
            GameRuntimeStats.RecordOilExtracted,
            GameRuntimeStats.RecordFuelProduced,
            OilBarrelsPerFuelBarrel);
    }

    private static BuildingProductionContextSystem.Source CreateProductionRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext = default,
        MaterialPropertyBlock markerPropertyBlock = null)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = CreateRuntimeContextSource(source);
        BuildingRuntimeQuerySystem.Context runtimeQueryContext = source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(runtimeSource);
        BuildingSpawnSystem.Context spawnContext = source.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(runtimeSource);
        BuildingProductionContextSystem.Source productionSource = default;
        productionSource = source.BuildingProductionContextSystem.CreateSource(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingPlacementStartupSystem.WorldCamera,
            source.BuildingDefinitionSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionUpdateSystem,
            source.BuildingProductionTransportSystem,
            source.BuildingProductionTransportBridgeSystem,
            source.BuildingProductionSlotSystem,
            source.BuildingRunwaySystem,
            source.BuildingVisualSystem,
            source.BuildingSpawnSystem,
            spawnContext,
            source.RuntimeResourceSystem.CurrentDollars,
            prefab => source.BuildingPlacementCommandSystem.BeginPlacementForConfiguredSpawnable(
                CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock),
                prefab),
            source.RuntimeResourceSystem.TrySpendDollars,
            source.RuntimeResourceSystem.AddDollars,
            cost => source.BuildingPlacementCommandSystem.SetActivePlacementCost(
                CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock),
                cost),
            (building, productionIndex, spawnUnitPrefab) =>
                TryQueuePlayerUnitProduction(source, productionSource, building, productionIndex, spawnUnitPrefab),
            buildingId => source.RuntimeBuildingSystem.SelectBuilding(buildingId),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => runtimeSource.RefreshBuildingMarkerVisibility?.Invoke(),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            building => ResolveBuildingFocusWorldPosition(runtimeSource, building),
            GameRuntimeStats.RecordUnitOrdered,
            Debug.LogWarning,
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountPendingProductionsForFaction(runtimeQueryContext, factionId, unitId),
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, factionId, unitId),
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            (out EntityManager entityManager) => runtimeSource.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                runtimeSource.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => runtimeSource.EnsureEntityQueries?.Invoke(entityManager),
            () => source.BuildingGameplayEcsQuerySystem.HaulerUnitsQuery,
            () => source.BuildingGameplayEcsQuerySystem.SelectedUnitsQuery,
            runtimeSource.TryGetRuntimeBuilding,
            runtimeSource.GetEffectivePlacementRect);
        return productionSource;
    }

    private static bool TryQueuePlayerUnitProduction(
        BuildingGameplayCompositionSourceSystem source,
        BuildingProductionContextSystem.Source productionSource,
        RuntimeBuildingData building,
        int productionIndex,
        GameObject spawnUnitPrefab)
    {
        if (!TryGetEntityManager(out EntityManager em))
            return false;

        return source.BuildingProductionSystem.TryQueuePlayerUnitFromBuilding(
            source.BuildingProductionContextSystem.CreateProductionQueueContext(productionSource),
            building,
            productionIndex,
            spawnUnitPrefab,
            em,
            Time.time);
    }

    private static Vector3 ResolveBuildingFocusWorldPosition(
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource,
        RuntimeBuildingData building)
    {
        if (runtimeSource.TryResolveBuildingFocusWorldPosition != null &&
            runtimeSource.TryResolveBuildingFocusWorldPosition(building, out Vector3 worldPosition))
            return worldPosition;

        return building != null && building.Instance != null
            ? building.Instance.transform.position
            : Vector3.zero;
    }

    private static BuildingPlacementRuntimeTickDiagnosticsSystem.Context CreateRuntimeTickDiagnosticsContext(BuildingGameplayCompositionSourceSystem source)
    {
        return new BuildingPlacementRuntimeTickDiagnosticsSystem.Context(
            () => source.RuntimeBuildingSystem.Count,
            Debug.Log);
    }

    private static BuildingPlacementInputSystem.ActivePlacementPointerContext CreateActivePlacementPointerContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingPlacementInputSystem.ActivePlacementPointerContext(
            (out GridConfig grid) => TryGetGridForPlacementInput(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => TryGetGridCell(source, screenPosition, grid, out cell),
            BuildingPlacementGridSystem.CenterCellToOrigin,
            BuildingPlacementCommitSystem.GetWallSegmentFootprint,
            source.BuildingGameplayDependencySystem.IsPointerOverPlacementUi,
            BuildingBarrierSystem.IsLinearWallDefinition,
            screenPosition => UpdatePlacement(source, interactionContext, markerPropertyBlock, screenPosition));
    }

    private static BuildingPlacementInteractionSystem.Context CreateBuildingPlacementInteractionContext(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingPlacementInteractionSystem.Context> getInteractionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return source.BuildingPlacementInteractionContextSystem.CreateContext(
            source.BuildingPlacementInteractionContextSystem.CreateSource(
                () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement,
                () => source.BuildingPlacementLifecycleSystem.CanConfirmBuildingPlacement,
                () => source.BuildingUiQuerySystem.HasSelectedBuilding(
                    source.BuildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource(source, getInteractionContext(), markerPropertyBlock))),
                () => source.BuildingUiQuerySystem.HasActiveBuilding(
                    source.BuildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource(source, getInteractionContext(), markerPropertyBlock))),
                () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement &&
                      source.BuildingPlacementInputSystem.IsDraggingPlacement,
                () => source.BuildingUiQuerySystem.PlacementStatusText(
                    source.BuildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource(source, getInteractionContext(), markerPropertyBlock))),
                () => source.BuildingUiQuerySystem.SelectedBuildingLabel(
                    source.BuildingUiContextSystem.CreateQueryContext(CreateBuildingUiContextSource(source, getInteractionContext(), markerPropertyBlock))),
                () => source.BuildingPlacementCommandSystem.BeginSoldierBasePlacement(CreatePlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingPlacementCommandSystem.ConfirmBuildingPlacement(CreatePlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingPlacementCommandSystem.CancelBuildingPlacement(CreatePlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                () => source.BuildingUiCommandSystem.CreateUnitFromSelectedBuilding(
                    source.BuildingUiContextSystem.CreateCommandContext(CreateBuildingUiContextSource(source, getInteractionContext(), markerPropertyBlock))),
                () => source.BuildingSelectionSystem.DeleteSelectedBuilding(
                    CreateBuildingSelectionContext(source),
                    buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(CreateBuildingRuntimeEntityContext(source), buildingId)),
                _ => source.BuildingSelectionSystem.ClearSelectedBuilding(CreateBuildingSelectionContext(source)),
                () => source.BuildingPlacementCommandSystem.ExitBuildMode(CreatePlacementCommandContext(source, getInteractionContext(), markerPropertyBlock)),
                (buildingId, blockerEntity, buildingObject) => source.BuildingRuntimeEntitySystem.HandleRuntimeBuildingEntityDestroyed(
                    CreateBuildingRuntimeEntityContext(source),
                    buildingId,
                    blockerEntity,
                    buildingObject),
                (
                    byte attackerFactionId,
                    Entity finalTarget,
                    int2 finalTargetCell,
                    int2 attackerCell,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition,
                    out string reason) => source.BuildingRuntimeQuerySystem.TryResolveBaseBreachTarget(
                    source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(source)),
                    attackerFactionId,
                    finalTarget,
                    finalTargetCell,
                    attackerCell,
                    out breachTarget,
                    out breachCell,
                    out breachPosition,
                    out reason)));
    }

    private static BuildingUiContextSystem.Source CreateBuildingUiContextSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return source.BuildingUiContextSystem.CreateSource(
            source.RuntimeResourceSystem,
            source.BuildingDefinitionSystem,
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionRequestSystem,
            () => source.BuildingProductionContextSystem.CreateProductionRequestContext(
                CreateProductionRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            () => Time.frameCount,
            TryGetEntityManager,
            () => Time.time,
            source.RuntimeBuildingSystem.HasSelectedBuilding,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            () => source.BuildingPlacementQuerySystem.GetPlacementStatusText(source.BuildingPlacementLifecycleSystem.ActivePlacement),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingLabel(CreateBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDisplayName(CreateBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDescription(CreateBuildingPlacementQueryContext(source)),
            (out int current, out int max) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingHealth(
                CreateBuildingPlacementQueryContext(source),
                out current,
                out max),
            (out GameObject prefab) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab(
                CreateBuildingPlacementQueryContext(source),
                out prefab),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingWall(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(source)),
                buildingId),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(source)),
                buildingId),
            (int buildingId, out byte factionId) => source.BuildingRuntimeQuerySystem.TryGetRuntimeBuildingOwnerFaction(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(source)),
                buildingId,
                out factionId),
            camera => source.BuildingSelectionSystem.HasVisibleSelectableBuilding(
                CreateBuildingSelectionContext(source),
                camera != null ? camera : source.BuildingPlacementStartupSystem.WorldCamera,
                Screen.width,
                Screen.height),
            (Entity unitEntity, out GameObject prefab) => source.RuntimeUnitPrefabSystem.TryResolveLiveUnitPreviewPrefab(
                source.BuildingRuntimeResourcePrefabContextSystem.CreateRuntimeUnitPrefabContext(CreateRuntimeResourcePrefabSource(source)),
                unitEntity,
                out prefab),
            () => source.BuildingSelectionSystem.DeleteSelectedBuilding(
                CreateBuildingSelectionContext(source),
                buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(CreateBuildingRuntimeEntityContext(source), buildingId)),
            () => source.BuildingPlacementCommandSystem.ConfirmBuildingPlacement(CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => source.BuildingPlacementCommandSystem.CancelBuildingPlacement(CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            _ => source.BuildingSelectionSystem.ClearSelectedBuilding(CreateBuildingSelectionContext(source)),
            () => source.BuildingPlacementCommandSystem.ExitBuildMode(CreatePlacementCommandContext(source, interactionContext, markerPropertyBlock)));
    }

    private static BuildingPlacementQuerySystem.Context CreateBuildingPlacementQueryContext(
        BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingPlacementQuerySystem.CreateContext(new BuildingPlacementQuerySystem.Source(
            source.RuntimeBuildingSystem.Buildings,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            BuildingDefinitionSystem.GetProductionCount,
            BuildingDefinitionSystem.GetProductionPrefab,
            TryGetEntityManager));
    }

    private static BuildingPlacementCommandSystem.Context CreatePlacementCommandContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return source.BuildingPlacementContextSystem.CreateCommandContext(
            CreatePlacementContextSource(source, interactionContext, markerPropertyBlock),
            source.BuildingPlacementStartupSystem,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementSessionSystem,
            Debug.LogWarning,
            GameRuntimeStats.RecordBuildingBuilt,
            source.BuildingGameplayDependencySystem.NotifyStaticMinimapChanged,
            _ => source.BuildingSelectionSystem.ClearSelectedBuilding(CreateBuildingSelectionContext(source)),
            () => BattleHudGameplayBridge.ResolveActive()?.ClearCommandMode());
    }

    private static void UpdatePlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition)
    {
        source.BuildingPlacementVisualUpdateSystem.UpdatePlacement(
            CreatePlacementVisualUpdateContext(source, interactionContext, markerPropertyBlock),
            source.BuildingPlacementLifecycleSystem.ActivePlacement,
            screenPosition);
    }

    private static BuildingPlacementVisualUpdateSystem.Context CreatePlacementVisualUpdateContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingPlacementVisualUpdateSystem.Context(
            source.BuildingPlacementInputSystem,
            source.BuildingPlacementPreviewSystem,
            source.BuildingPlacementValidationSystem,
            source.BuildingPlacementGridSystem,
            source.BuildingPlacementStartupSystem,
            source.BuildingGameplayDependencySystem,
            source.BuildingPlacementContextSystem,
            source.BuildingPlacementCommitSystem,
            source.BuildingPlacementLifecycleSystem,
            source.BuildingBarrierSystem,
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => TryGetGridCell(source, screenPosition, grid, out cell),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                TryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            (origin, footprint, grid, roads, blockerData) => IsActivePlacementValid(source, origin, footprint, grid, roads, blockerData),
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            source.BuildingPlacementVisualSystem.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualSystem.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridSystem.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystem.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => TryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            () => CreatePlacementContextSource(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(source)),
            building => source.BuildingSelectionSystem.SelectAndFocusBuilding(CreateBuildingSelectionContext(source), building));
    }

    private static BuildingPlacementContextSystem.Source CreatePlacementContextSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingPlacementContextSystem.Source(
            source.RuntimeGameplayStateSystem,
            source.BuildingPlacementLifecycleSystem,
            source.BuildingPlacementInputSystem,
            source.BuildingPlacementPreviewSystem,
            source.BuildingPlacementValidationSystem,
            source.RuntimeBuildingSystem,
            source.BuildingPlacementStartupSystem.BuildingRoot,
            source.BuildingPlacementVisualSystem.CreateBuildingVisualInstance,
            preview => source.BuildingRuntimeObjectSystem.DestroyRuntimeObject(preview),
            footprint => GetCenterScreenPlacementOrigin(source, footprint),
            (BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin) => TryResolveInitialPlacementOrigin(source, interactionContext, markerPropertyBlock, definition, preferredOrigin, out resolvedOrigin),
            (placement, updateCellFromPointer, screenPosition) => UpdatePlacementVisual(source, interactionContext, markerPropertyBlock, placement, updateCellFromPointer, screenPosition),
            placement => FocusActivePlacement(source, interactionContext, markerPropertyBlock, placement),
            placement => ValidateActivePlacementForConfirm(source, interactionContext, markerPropertyBlock, placement),
            source.RuntimeResourceSystem.TrySpendDollars,
            placement => PlaceBuilding(source, interactionContext, markerPropertyBlock, placement),
            () => BattleHudGameplayBridge.ResolveActive()?.ApplyCommandMode(TacticalCommandMode.Build),
            () => source.BuildingSelectionSystem.ClearSelectedBuilding(CreateBuildingSelectionContext(source)),
            (out GridConfig grid) => TryGetGridForPlacementInput(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => TryGetGridCell(source, screenPosition, grid, out cell),
            source.BuildingGameplayDependencySystem.IsPointerOverPlacementUi,
            screenPosition => UpdatePlacement(source, interactionContext, markerPropertyBlock, screenPosition),
            source.BuildingGameplayDependencySystem.IsRuntimeBlockerCell,
            (grid, origin, footprint) => source.BuildingPlacementInvalidCellSystem.HasRoadInFootprint(source.BuildingPlacementStartupSystem, grid, origin, footprint),
            source.BuildingPlacementVisualSystem.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualSystem.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridSystem.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystem.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => TryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationSystem.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextSystem.CreateCreationContext(CreateBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                definition,
                instance,
                originCell,
                removeOverlappingBlockers),
            BuildingRuntimeSpawnSystem.CloneDefinitionWithFootprint,
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            source.BuildingRuntimeObjectSystem.DestroyRuntimeObject);
    }

    private static void FocusActivePlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement)
    {
        source.BuildingPlacementVisualUpdateSystem.FocusActivePlacement(
            CreatePlacementVisualUpdateContext(source, interactionContext, markerPropertyBlock),
            placement);
    }

    private static bool ValidateActivePlacementForConfirm(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement)
    {
        return source.BuildingPlacementVisualUpdateSystem.ValidateActivePlacementForConfirm(
            CreatePlacementVisualUpdateContext(source, interactionContext, markerPropertyBlock),
            placement);
    }

    private static void UpdatePlacementVisual(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition)
    {
        source.BuildingPlacementVisualUpdateSystem.UpdatePlacementVisual(
            CreatePlacementVisualUpdateContext(source, interactionContext, markerPropertyBlock),
            placement,
            updateCellFromPointer,
            screenPosition);
    }

    private static void PlaceBuilding(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement)
    {
        source.BuildingPlacementVisualUpdateSystem.PlaceBuilding(
            CreatePlacementVisualUpdateContext(source, interactionContext, markerPropertyBlock),
            placement);
    }

    private static bool TryResolveInitialPlacementOrigin(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out Vector2Int resolvedOrigin)
    {
        BuildingRuntimeSpawnCommandSystem.Context context = source.BuildingRuntimeContextSystem.CreateSpawnCommandContext(
            CreateBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock),
            source.BuildingRuntimeSpawnSystem,
            source.BuildingPlacementStartupSystem.SoldierBaseDefinition,
            source.BuildingPlacementStartupSystem.SoldierTentDefinition,
            source.BuildingPlacementStartupSystem.FactoryDefinition);
        return source.BuildingRuntimeSpawnCommandSystem.TryResolveInitialPlacementOrigin(
            context,
            definition,
            preferredOrigin,
            out resolvedOrigin);
    }

    private static Vector2Int GetCenterScreenPlacementOrigin(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int footprintCells)
    {
        if (!TryGetGridData(source, out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        return source.BuildingPlacementGridSystem.GetCenterScreenPlacementOrigin(
            footprintCells,
            grid,
            source.BuildingPlacementStartupSystem.WorldCamera,
            source.BuildingPlacementStartupSystem.BuildPlaneY,
            new Vector2(Screen.width, Screen.height));
    }

    private static bool IsActivePlacementValid(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData)
    {
        PlacementState activePlacement = source.BuildingPlacementLifecycleSystem.ActivePlacement;
        bool rotateVertical = source.BuildingBarrierSystem.ResolvePlacementRotateVertical(
            source.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(source)),
            source.BuildingPlacementInputSystem,
            activePlacement);
        return IsPlacementValid(source, activePlacement?.Definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData);
    }

    private static BuildingSelectionClickSystem.Context CreateBuildingSelectionClickContext(
        BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingSelectionClickSystem.CreateContext(new BuildingSelectionClickSystem.Source(
            () => UnitPathfindingSystem.HasPendingPathJob,
            (out GridConfig grid) => TryGetGridForSelection(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => TryGetGridCell(source, screenPosition, grid, out cell),
            (screenPosition, cell) => source.BuildingSelectionSystem.HandleBuildingSelectionClick(
                CreateBuildingSelectionContext(source),
                screenPosition,
                cell)));
    }

    private static BuildingSelectionSystem.Context CreateBuildingSelectionContext(
        BuildingGameplayCompositionSourceSystem source)
    {
        return source.BuildingSelectionSystem.CreateContext(new BuildingSelectionSystem.Source(
            source.RuntimeBuildingSystem,
            source.RuntimeBuildingSystem.Buildings,
            (out GridConfig grid) => TryGetGridForSelection(source, out grid),
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSource(source))),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            source.BuildingGameplayDependencySystem.IsBoardablePlayerTransportClick,
            clickedBuildingId => source.BuildingRuntimeContextSystem.TryAssignSelectedHaulerOrders(
                CreateRuntimeContextSource(source),
                clickedBuildingId),
            source.BuildingGameplayDependencySystem.TryIssueMoveOrderToBuilding,
            BuildingBarrierSystem.ShouldUseExpandedSelectionArea));
    }

    private static BuildingRuntimeBoundaryPublishSystem.Context CreateRuntimeBoundaryPublishContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingRuntimeBoundaryPublishSystem.Context(
            TryGetEntityManager,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            source.BuildingRuntimeBoundarySystem,
            source.BuildingDefinitionSystem,
            source.BuildingRuntimeSpawnSystem,
            source.BuildingRuntimeContextSystem.CreateSpawnContext(CreateBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
            source.BuildingProductionRequestSystem,
            source.BuildingProductionContextSystem.CreateProductionRequestContext(CreateProductionRuntimeContextSource(source)),
            source.BuildingRuntimeQuerySystem,
            source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(CreateRuntimeContextSource(source)),
            source.FactionResourceSystem,
            () => source.BuildingGameplayEcsQuerySystem.BuildingRuntimeBoundaryQuery,
            source.RuntimeBuildingSystem.Buildings);
    }

    private static BuildingRuntimeContextSystem.Source CreateBuildingRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock)
    {
        return new BuildingRuntimeContextSystem.Source(
            source.BuildingPlacementStartupSystem.BuildingRoot,
            source.BuildingDefinitionSystem,
            source.BuildingRunwaySystem,
            source.BuildingPlacementValidationSystem,
            new BuildingPlacementValidationSystem.WallValidationContext(
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingGameplayDependencySystem.IsRuntimeBlockerCell,
                (grid, origin, footprint) => source.BuildingPlacementInvalidCellSystem.HasRoadInFootprint(source.BuildingPlacementStartupSystem, grid, origin, footprint)),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                TryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            (definition, origin, grid, rotateVertical) => GetEffectivePlacementRect(source, definition, origin, grid, rotateVertical),
            (definition, origin, footprint, rotateVertical, grid, roads, blockerData) => IsPlacementValid(source, definition, origin, footprint, rotateVertical, grid, roads, blockerData),
            source.BuildingPlacementInvalidCellSystem.HasCachedInvalidCellInFootprint,
            source.BuildingPlacementVisualSystem.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualSystem.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridSystem.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystem.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => TryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationSystem.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextSystem.CreateCreationContext(CreateBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                definition,
                instance,
                originCell,
                removeOverlappingBlockers),
            (building, ownerFactionId) => source.BuildingRuntimeOwnershipSystem.SetRuntimeBuildingOwnerFaction(
                source.BuildingRuntimeContextSystem.CreateOwnershipContext(CreateBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                building,
                ownerFactionId),
            source.RuntimeBuildingSystem,
            source.BuildingPlacementInteractionSystem,
            interactionContext,
            () => source.BuildingPlacementRedirectSystem.IsDeferringSideEffects,
            (out GridConfig grid) => TryGetGridData(source, out _, out grid, out _, out _),
            (definition, origin, grid) => GetEffectivePlacementRect(source, definition, origin, grid),
            source.BuildingGameplayDependencySystem.RemoveBlockersOverlappingFootprint,
            source.BuildingRuntimeEntitySystem,
            CreateBuildingRuntimeEntityContext(source),
            source.BuildingPlacementRedirectSystem,
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            () => source.BuildingGameplayEcsQuerySystem.RedirectUnitsQuery,
            building => source.BuildingRuntimeVisualSystem.InitializeBuildingVisuals(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSource(source)),
                building),
            () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSource(source))),
            TryGetEntityManager,
            source.BuildingVisualSystem,
            source.BuildingGameplayDependencySystem.FactionVisualSettings,
            markerPropertyBlock,
            buildingId => source.BuildingRuntimeEntitySystem.DeleteBuildingById(CreateBuildingRuntimeEntityContext(source), buildingId),
            () => BeginDeferredRuntimeBuildingSideEffects(source),
            () => EndDeferredRuntimeBuildingSideEffects(source));
    }

    private static BuildingRuntimeEntitySystem.Context CreateBuildingRuntimeEntityContext(
        BuildingGameplayCompositionSourceSystem source)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = CreateRuntimeContextSource(source);
        BuildingCombatSystem.Context<RuntimeBuildingData> combatContext =
            source.BuildingRuntimeContextSystem.CreateCombatContext(runtimeSource);
        return source.BuildingRuntimeContextSystem.CreateRuntimeEntityContext(
            runtimeSource,
            source.BuildingCombatSystem,
            combatContext,
            () => Time.time,
            DestroyedBuildingLifetimeSeconds);
    }

    private static bool TryAlignGateToNearbyWall(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int originCell,
        BuildingDefinition definition,
        out bool gateVertical)
    {
        return source.BuildingBarrierSystem.ShouldAlignGateToNearbyWall(
            source.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(source)),
            originCell,
            definition,
            out gateVertical);
    }

    private static bool IsPlacementValid(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData)
    {
        return source.BuildingPlacementInvalidCellSystem.IsPlacementValid(
            definition,
            originCell,
            footprintCells,
            rotateVertical,
            grid,
            roads,
            blockerData,
            source.BuildingGameplayDependencySystem,
            source.BuildingPlacementStartupSystem,
            (candidateDefinition, candidateOrigin, candidateGrid, candidateRotateVertical) =>
                GetEffectivePlacementRect(source, candidateDefinition, candidateOrigin, candidateGrid, candidateRotateVertical),
            candidateRect => OverlapsAnyRuntimeBuilding(source, candidateRect));
    }

    private static bool OverlapsAnyRuntimeBuilding(
        BuildingGameplayCompositionSourceSystem source,
        RectInt candidateRect)
    {
        if (source.RuntimeBuildingSystem.Buildings == null || source.RuntimeBuildingSystem.Buildings.Count == 0)
            return false;
        if (!TryGetGridData(source, out _, out GridConfig grid, out _, out _))
            return false;

        foreach (var entry in source.RuntimeBuildingSystem.Buildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;

            RectInt existingRect = GetEffectivePlacementRect(source, building.Definition, building.OriginCell, grid);
            if (candidateRect.Overlaps(existingRect))
                return true;
        }

        return false;
    }

    private static void BeginDeferredRuntimeBuildingSideEffects(BuildingGameplayCompositionSourceSystem source)
    {
        source.BuildingPlacementRedirectSystem.BeginDeferredRuntimeBuildingSideEffects(
            () => source.BuildingPlacementInvalidCellSystem.RebuildPlacementInvalidPrefix(
                source.BuildingGameplayGridDataSystem,
                source.BuildingGameplayEcsQuerySystem,
                TryGetEntityManager,
                source.BuildingPlacementStartupSystem,
                source.BuildingGameplayDependencySystem));
    }

    private static void EndDeferredRuntimeBuildingSideEffects(BuildingGameplayCompositionSourceSystem source)
    {
        source.BuildingPlacementRedirectSystem.EndDeferredRuntimeBuildingSideEffects(
            source.BuildingRuntimeContextSystem.CreateRedirectContext(CreateRuntimeContextSource(source)),
            () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSource(source))),
            source.BuildingPlacementInvalidCellSystem.Clear);
    }

    private static BuildingRuntimeContextSystem.RuntimeSource CreateRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source)
    {
        return new BuildingRuntimeContextSystem.RuntimeSource(
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionSlotSystem,
            source.BuildingSpawnPrefabSystem,
            source.BuildingRuntimeResourcePrefabContextSystem.CreateBuildingSpawnPrefabContext(CreateRuntimeResourcePrefabSource(source)),
            source.BuildingVisualSystem,
            source.BuildingRuntimeVisualSystem,
            source.BuildingBarrierSystem,
            source.BuildingResourceHaulerBridgeSystem,
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            source.BuildingProductionContextSystem,
            source.BuildingGameplayDependencySystem.FactionVisualSettings,
            null,
            source.BuildingGameplayEcsQuerySystem.LiveUnitFootprintQuery,
            source.BuildingGameplayEcsQuerySystem.RedirectUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.HaulerUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.SelectedUnitsQuery,
            source.BuildingGameplayEcsQuerySystem.LiveFactionUnitsQuery,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            TryGetEntityManager,
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                source.BuildingGameplayGridDataSystem.TryGetGridData(
                    source.BuildingGameplayEcsQuerySystem,
                    TryGetEntityManager,
                    out gridEntity,
                    out grid,
                    out roads,
                    out blockerData),
            source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries,
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            building => IsHouseBuilding(source, building),
            (RuntimeBuildingData building, out Vector3 worldPosition) => TryResolveBuildingFocusWorldPosition(source, building, out worldPosition),
            (int id, out RuntimeBuildingData building) => TryGetRuntimeBuilding(source, id, out building),
            (building, grid) => GetEffectivePlacementRect(source, building.Definition, building.OriginCell, grid),
            building => source.BuildingBarrierSystem.RememberOpenBaseBreach(
                source.BuildingRuntimeContextSystem.CreateBarrierContext(CreateRuntimeContextSource(source)),
                building),
            source.BuildingGameplayDependencySystem.NotifyHomeBuildingDestroyed,
            source.BuildingRuntimeObjectSystem.DestroyRuntimeObject,
            () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(CreateRuntimeContextSource(source))),
            source.BuildingGameplayDependencySystem.NotifyStaticMinimapChanged,
            message => Debug.Log(message),
            false);
    }

    private static bool TryGetGridData(
        BuildingGameplayCompositionSourceSystem source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerData blockerData)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridData(
            source.BuildingGameplayEcsQuerySystem,
            TryGetEntityManager,
            out gridEntity,
            out grid,
            out roads,
            out blockerData);
    }

    private static bool TryGetGridForSelection(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForSelection(
            source.BuildingGameplayEcsQuerySystem,
            TryGetEntityManager,
            out grid);
    }

    private static bool TryGetGridForPlacementInput(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridForPlacementInput(
            source.BuildingGameplayEcsQuerySystem,
            TryGetEntityManager,
            out grid);
    }

    private static bool TryGetGridCell(
        BuildingGameplayCompositionSourceSystem source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell)
    {
        return source.BuildingGameplayGridDataSystem.TryGetGridCell(
            source.BuildingPlacementGridSystem,
            source.BuildingPlacementStartupSystem,
            screenPosition,
            grid,
            out cell);
    }

    private static bool IsHouseBuilding(BuildingGameplayCompositionSourceSystem source, RuntimeBuildingData building)
    {
        if (building?.Definition == null)
            return false;

        if (building.Definition.Role == BuildingRole.House)
            return true;

        if (building.Definition.Role != BuildingRole.None)
            return false;

        GameObject prefab = building.Definition.Prefab;
        string prefabName = prefab != null ? prefab.name : string.Empty;
        if (source.BuildingGameplayDependencySystem.IsConfiguredHousePrefab(prefab))
            return true;

        return prefabName.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0 &&
               !building.Definition.IsWall;
    }

    private static bool TryResolveBuildingFocusWorldPosition(
        BuildingGameplayCompositionSourceSystem source,
        RuntimeBuildingData building,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (building == null)
            return false;

        if (building.Instance != null &&
            building.Definition != null &&
            source.BuildingGameplayGridDataSystem.TryGetGridForSelection(source.BuildingGameplayEcsQuerySystem, TryGetEntityManager, out GridConfig grid))
        {
            worldPosition = source.BuildingPlacementGridSystem.GetFootprintCenter(
                building.OriginCell,
                building.Definition.FootprintCells,
                grid,
                source.BuildingPlacementStartupSystem.BuildPlaneY);
            return true;
        }

        if (building.Instance == null)
            return false;

        worldPosition = building.Instance.transform.position;
        worldPosition.y = 0f;
        return true;
    }

    private static bool TryGetRuntimeBuilding(
        BuildingGameplayCompositionSourceSystem source,
        int id,
        out RuntimeBuildingData building)
    {
        if (source.RuntimeBuildingSystem.TryGetBuilding(id, out building) && building != null && !building.IsDestroyed)
            return true;

        building = null;
        return false;
    }

    private static RectInt GetEffectivePlacementRect(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical = false)
    {
        return source.BuildingRunwaySystem.GetEffectivePlacementRect(
            definition,
            originCell,
            grid,
            rotateVertical,
            source.BuildingPlacementStartupSystem.BuildPlaneY,
            source.BuildingPlacementGridSystem.GetPlacementFootprint);
    }

    private static bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        entityManager = world.EntityManager;
        return true;
    }

    public void BindSelection(
        Result building,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem)
    {
        building.BindSelection(dayNight, selectionUiCameraSystem, selectionBuildingInteractionSystem);
    }

    public void InitializeCitizenPopulation(Result building, DayNightSystem dayNight, Camera worldCamera)
    {
        building.InitializeCitizenPopulation(dayNight, worldCamera);
    }

    public void BindCitizenPopulation(
        Result building,
        DayNightSystem dayNight,
        SelectionUiCameraSystem selectionUiCameraSystem,
        SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
        CitizenPopulationEventSystem citizenPopulationEventSystem)
    {
        building.BindCitizenPopulation(
            dayNight,
            selectionUiCameraSystem,
            selectionBuildingInteractionSystem,
            citizenPopulationEventSystem);
    }
}
