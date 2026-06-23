using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingGameplayCompositionSystem
{
    private const float DestroyedBuildingLifetimeSeconds = 5f;
    private const float OilBarrelsPerFuelBarrel = 2f;
    private readonly BuildingGameplayChildSystem _childSystem = new();
    private readonly BuildingGameplayStartupCompositionSystemHelper _startupCompositionHelper = new();
    private readonly BuildingGameplayBindingCompositionSystemHelper _bindingCompositionHelper = new();
    private readonly BuildingCitizenPopulationCompositionSystem _citizenPopulationCompositionSystem = ResolveBuildingCitizenPopulationCompositionSystem();
    private readonly BuildingGameplayDisposalCompositionSystemHelper _disposalCompositionHelper = new();
    private readonly BuildingMarkerVisualPresentationSystemHelper _markerVisualPresentationHelper = new();
    private readonly BuildingRuntimeTickCompositionSystemHelper _runtimeTickCompositionHelper = new();
    private readonly BuildingPlacementInputTickCompositionSystemHelper _placementInputTickCompositionHelper = new();
    private readonly BuildingRuntimeBoundaryCompositionSystemHelper _runtimeBoundaryCompositionHelper = new();
    private readonly BuildingProductionTickCompositionSystemHelper _productionTickCompositionHelper = new();
    private readonly BuildingPlacementInteractionCompositionSystemHelper _placementInteractionCompositionHelper = new();
    private readonly BuildingPlacementRuntimeTickContextCompositionSystemHelper _runtimeTickContextCompositionHelper = new();
    private readonly BuildingGameplayCompositionResultSystem _resultSystem = new();

    public BuildingGameplayCompositionResultSystem.Result Initialize(
        BuildingPlacementSystemConfig buildingPlacementConfig,
        Camera worldCamera,
        Transform runtimeTransportsRoot,
        Transform runtimeUiRoot,
        RoadGridProjectionSystem.RoadFootprintState roadFootprintState,
        FactionVisualSettings factionVisuals,
        DayNightSystem dayNight,
        RTSSelectionSystemConfig rtsSelectionConfig = null,
        MapBuildingPlacementConfig mapBuildingPlacementConfig = null,
        MapVehiclePlacementConfig mapVehiclePlacementConfig = null,
        Transform mapBuildingAuthoringRoot = null,
        Transform mapVehicleAuthoringRoot = null,
        Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab = null,
        BuildingProductionSystem.TryGetUnitProductionMetadataDelegate tryGetUnitProductionMetadata = null,
        BuildingProductionTransportSystem.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null,
        Func<GameObject, string> resolveSpawnableLookupKey = null,
        BuildingDefinitionSystem.TryGetBuildingDefinitionMetadataDelegate tryGetBuildingDefinitionMetadata = null,
        BuildingDefinitionSystem.TryGetUnitDefinitionMetadataDelegate tryGetUnitDefinitionMetadata = null)
    {
        MaterialPropertyBlock markerPropertyBlock = BuildingMarkerVisualPresentationSystemHelper.GetMarkerPropertyBlock(_markerVisualPresentationHelper);
        BuildingGameplayCompositionSourceSystem childSystems = _childSystem.Create();
        childSystems.BuildingDefinitionSystem.ConfigureAuthoringMetadataResolvers(
            tryGetBuildingDefinitionMetadata,
            tryGetUnitDefinitionMetadata);
        childSystems.BuildingProductionSystem.ConfigureUnitProductionMetadataResolver(tryGetUnitProductionMetadata);
        childSystems.BuildingProductionTransportSystem.SetRuntimeRoot(runtimeTransportsRoot);
        childSystems.PrepareTransportDropVisual = prepareTransportDropVisual;
        _startupCompositionHelper.Initialize(
            childSystems,
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            roadFootprintState,
            factionVisuals,
            dayNight);
        BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource =
            BuildingRuntimeResourcePrefabCompositionSystemHelper.Create(
                childSystems.BuildingRuntimeResourcePrefabCompositionHelper,
                childSystems);
        bool tryGetEntityManager(out EntityManager entityManager)
        {
            return childSystems.BuildingEntityManagerAccessSystem.TryGetEntityManager(out entityManager);
        }
        BuildingGameplayGridDataSystem.TryGetEntityManagerDelegate tryGetGridEntityManager = tryGetEntityManager;

        bool tryGetGridData(
            BuildingGameplayCompositionSourceSystem source,
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData)
        {
            return source.BuildingGridCompositionSystem.TryGetGridData(
                source,
                tryGetGridEntityManager,
                out gridEntity,
                out grid,
                out roads,
                out blockerData);
        }

        bool tryGetGridForSelection(BuildingGameplayCompositionSourceSystem source, out GridConfig grid)
        {
            return source.BuildingGridCompositionSystem.TryGetGridForSelection(
                source,
                tryGetGridEntityManager,
                out grid);
        }

        bool tryGetGridForPlacementInput(BuildingGameplayCompositionSourceSystem source, out GridConfig grid)
        {
            return source.BuildingGridCompositionSystem.TryGetGridForPlacementInput(
                source,
                tryGetGridEntityManager,
                out grid);
        }

        bool tryGetGridCell(
            BuildingGameplayCompositionSourceSystem source,
            Vector2 screenPosition,
            GridConfig grid,
            out Vector2Int cell)
        {
            return source.BuildingGridCompositionSystem.TryGetGridCell(
                source,
                screenPosition,
                grid,
                out cell);
        }

        BuildingRuntimeCompositionSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect =
            (source, definition, originCell, grid, rotateVertical) => source.BuildingRuntimeCompositionQuerySystem.GetEffectivePlacementRect(
                source,
                definition,
                originCell,
                grid,
                rotateVertical);
        BuildingRuntimeCompositionSystem.IsHouseBuildingDelegate isHouseBuilding =
            (source, building) => source.BuildingRuntimeCompositionQuerySystem.IsHouseBuilding(source, building);
        BuildingRuntimeCompositionSystem.TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition =
            (BuildingGameplayCompositionSourceSystem source, RuntimeBuildingEntity building, out Vector3 worldPosition) =>
                source.BuildingRuntimeCompositionQuerySystem.TryResolveBuildingFocusWorldPosition(
                    source,
                    building,
                    tryGetEntityManager,
                    out worldPosition);
        BuildingRuntimeCompositionSystem.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding =
            (BuildingGameplayCompositionSourceSystem source, int id, out RuntimeBuildingEntity building) =>
                source.BuildingRuntimeCompositionQuerySystem.TryGetRuntimeBuilding(source, id, out building);
        BuildingRuntimeCompositionSystem.OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding =
            (source, candidateRect) => source.BuildingRuntimeCompositionQuerySystem.OverlapsAnyRuntimeBuilding(
                source,
                candidateRect,
                tryGetGridData,
                (querySource, definition, originCell, grid, rotateVertical) => getEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical));
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource =
            source => source.BuildingRuntimeCompositionSystem.CreateRuntimeContextSource(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect);
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext =
            source => source.BuildingRuntimeCompositionSystem.CreateBuildingRuntimeEntityContext(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect,
                DestroyedBuildingLifetimeSeconds);
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource =
            (source, placementInteractionContext, placementMarkerPropertyBlock) => source.BuildingRuntimeCompositionSystem.CreateBuildingRuntimeContextSource(
                source,
                placementInteractionContext,
                placementMarkerPropertyBlock,
                tryGetEntityManager,
                tryGetGridData,
                getEffectivePlacementRect,
                overlapsAnyRuntimeBuilding,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                source => source.BuildingRuntimeSideEffectCompositionSystemHelper.BeginDeferredRuntimeBuildingSideEffects(source, tryGetEntityManager),
                source => source.BuildingRuntimeSideEffectCompositionSystemHelper.EndDeferredRuntimeBuildingSideEffects(source, tryGetEntityManager),
                DestroyedBuildingLifetimeSeconds);
        BuildingPlacementAdapterSystem.CreateRuntimeContextSourceDelegate createRuntimeContextSourceForAdapter =
            source => createRuntimeContextSource(source);
        BuildingPlacementAdapterSystem.CreateBuildingRuntimeContextSourceDelegate createBuildingRuntimeContextSourceForAdapter =
            (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                createBuildingRuntimeContextSource(source, placementInteractionContext, placementMarkerPropertyBlock);
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createPlacementQueryContext =
            source => source.BuildingPlacementQueryCompositionSystem.Create(source);
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext =
            source => source.BuildingSelectionCompositionHelper.Create(
                source,
                tryGetGridForSelection,
                resolveSelectionPortraitSpriteFromPrefab,
                createRuntimeContextSource);
        BuildingPlacementAdapterSystem.IsPlacementValidDelegate isPlacementValid =
            (source, definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData) =>
                source.BuildingPlacementAdapterSystem.IsPlacementValid(
                    source,
                    definition,
                    originCell,
                    footprintCells,
                    rotateVertical,
                    grid,
                    roads,
                    blockerData,
                    (placementSource, definition, originCell, placementGrid, placementRotateVertical) =>
                        getEffectivePlacementRect(placementSource, definition, originCell, placementGrid, placementRotateVertical),
                    (placementSource, candidateRect) => overlapsAnyRuntimeBuilding(placementSource, candidateRect));
        BuildingPlacementCommandCompositionSystemHelper.GetCenterScreenPlacementOriginDelegate getCenterScreenPlacementOrigin =
            (source, footprintCells) => source.BuildingPlacementAdapterSystem.GetCenterScreenPlacementOrigin(
                source,
                footprintCells,
                tryGetGridData);
        BuildingPlacementCommandCompositionSystemHelper.TryResolveInitialPlacementOriginDelegate tryResolveInitialPlacementOrigin =
            (
                BuildingGameplayCompositionSourceSystem source,
                BuildingPlacementInteractionSystem.Context placementInteractionContext,
                MaterialPropertyBlock placementMarkerPropertyBlock,
                BuildingDefinition definition,
                Vector2Int preferredOrigin,
                out Vector2Int resolvedOrigin) => source.BuildingPlacementAdapterSystem.TryResolveInitialPlacementOrigin(
                source,
                placementInteractionContext,
                placementMarkerPropertyBlock,
                definition,
                preferredOrigin,
                createBuildingRuntimeContextSourceForAdapter,
                out resolvedOrigin);
        BuildingPlacementVisualCompositionSystem.IsActivePlacementValidDelegate isActivePlacementValid =
            (source, originCell, footprintCells, grid, roads, blockerData) => source.BuildingPlacementAdapterSystem.IsActivePlacementValid(
                source,
                originCell,
                footprintCells,
                grid,
                roads,
                blockerData,
                createRuntimeContextSourceForAdapter,
                isPlacementValid);
        BuildingPlacementCommandCompositionSystemHelper.TryAlignGateToNearbyWallDelegate tryAlignGateForCommand =
            (BuildingGameplayCompositionSourceSystem source, Vector2Int originCell, BuildingDefinition definition, out bool gateVertical) =>
                source.BuildingPlacementAdapterSystem.TryAlignGateToNearbyWall(
                    source,
                    originCell,
                    definition,
                    createRuntimeContextSourceForAdapter,
                    out gateVertical);
        BuildingPlacementVisualCompositionSystem.TryAlignGateToNearbyWallDelegate tryAlignGateForVisual =
            (BuildingGameplayCompositionSourceSystem source, Vector2Int originCell, BuildingDefinition definition, out bool gateVertical) =>
                source.BuildingPlacementAdapterSystem.TryAlignGateToNearbyWall(
                    source,
                    originCell,
                    definition,
                    createRuntimeContextSourceForAdapter,
                    out gateVertical);
        BuildingPlacementVisualCompositionSystem.CreatePlacementContextSourceDelegate createPlacementContextSource = null;
        BuildingPlacementCommandCompositionSystemHelper.UpdatePlacementVisualDelegate updatePlacementVisual =
            (source, placementInteractionContext, placementMarkerPropertyBlock, placement, updateCellFromPointer, screenPosition) =>
                source.BuildingPlacementVisualCompositionSystem?.UpdatePlacementVisual(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    placement,
                    updateCellFromPointer,
                    screenPosition,
                    tryGetGridCell,
                    tryGetGridData,
                    isActivePlacementValid,
                    tryAlignGateForVisual,
                    createPlacementContextSource,
                    createRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementCommandCompositionSystemHelper.FocusActivePlacementDelegate focusActivePlacement =
            (source, placementInteractionContext, placementMarkerPropertyBlock, placement) =>
                source.BuildingPlacementVisualCompositionSystem?.FocusActivePlacement(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    placement,
                    tryGetGridCell,
                    tryGetGridData,
                    isActivePlacementValid,
                    tryAlignGateForVisual,
                    createPlacementContextSource,
                    createRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementCommandCompositionSystemHelper.ValidateActivePlacementForConfirmDelegate validateActivePlacementForConfirm =
            (source, placementInteractionContext, placementMarkerPropertyBlock, placement) =>
                source.BuildingPlacementVisualCompositionSystem != null &&
                source.BuildingPlacementVisualCompositionSystem.ValidateActivePlacementForConfirm(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    placement,
                    tryGetGridCell,
                    tryGetGridData,
                    isActivePlacementValid,
                    tryAlignGateForVisual,
                    createPlacementContextSource,
                    createRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementCommandCompositionSystemHelper.PlaceBuildingDelegate placeBuilding =
            (source, placementInteractionContext, placementMarkerPropertyBlock, placement) =>
                source.BuildingPlacementVisualCompositionSystem?.PlaceBuilding(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    placement,
                    tryGetGridCell,
                    tryGetGridData,
                    isActivePlacementValid,
                    tryAlignGateForVisual,
                    createPlacementContextSource,
                    createRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementCommandCompositionSystemHelper.UpdatePlacementDelegate updatePlacement =
            (source, placementInteractionContext, placementMarkerPropertyBlock, screenPosition) =>
                source.BuildingPlacementVisualCompositionSystem?.UpdatePlacement(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    screenPosition,
                    tryGetGridCell,
                    tryGetGridData,
                    isActivePlacementValid,
                    tryAlignGateForVisual,
                    createPlacementContextSource,
                    createRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementInteractionCompositionSystemHelper.UpdatePlacementDelegate updatePlacementForInteraction =
            (source, placementInteractionContext, placementMarkerPropertyBlock, screenPosition) =>
                updatePlacement(source, placementInteractionContext, placementMarkerPropertyBlock, screenPosition);
        createPlacementContextSource = (source, placementInteractionContext, placementMarkerPropertyBlock) =>
            source.BuildingPlacementCommandCompositionSystemHelper.CreateContextSource(
                source,
                placementInteractionContext,
                placementMarkerPropertyBlock,
                getCenterScreenPlacementOrigin,
                tryResolveInitialPlacementOrigin,
                updatePlacementVisual,
                focusActivePlacement,
                validateActivePlacementForConfirm,
                placeBuilding,
                tryGetGridForPlacementInput,
                tryGetGridCell,
                updatePlacement,
                tryAlignGateForCommand,
                createBuildingRuntimeContextSource,
                createBuildingSelectionContext);
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext =
            (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                source.BuildingPlacementCommandCompositionSystemHelper.CreateCommandContext(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    getCenterScreenPlacementOrigin,
                    tryResolveInitialPlacementOrigin,
                    updatePlacementVisual,
                    focusActivePlacement,
                    validateActivePlacementForConfirm,
                    placeBuilding,
                    tryGetGridForPlacementInput,
                    tryGetGridCell,
                    updatePlacement,
                    tryAlignGateForCommand,
                    createBuildingRuntimeContextSource,
                    createBuildingSelectionContext);
        BuildingPlacementInteractionSystem.Context interactionContext = default;
        interactionContext = _placementInteractionCompositionHelper.CreateBuildingPlacementInteractionContext(
            childSystems,
            () => interactionContext,
            markerPropertyBlock,
            (source, placementInteractionContext, placementMarkerPropertyBlock) => source.BuildingUiCompositionSystem.CreateQueryContext(
                source,
                placementInteractionContext,
                placementMarkerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createPlacementQueryContext,
                createBuildingSelectionContext),
            createPlacementCommandContext,
            (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                source.BuildingProductionContextSystem.CreateProductionRequestContext(
                    source.BuildingProductionCompositionSystem.CreateRuntimeContextSource(
                        source,
                        createRuntimeContextSource,
                        createPlacementCommandContext,
                        placementInteractionContext,
                        placementMarkerPropertyBlock)),
            createBuildingSelectionContext,
            createBuildingRuntimeEntityContext,
            createRuntimeContextSource);
        BuildingRuntimeContextSystem.Source buildingRuntimeContextSource =
            createBuildingRuntimeContextSource(childSystems, interactionContext, markerPropertyBlock);
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary =
            BuildingCitizenPopulationCompositionSystem.CreateBoundary(_citizenPopulationCompositionSystem);
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition =
            BuildingCitizenPopulationCompositionSystem.Create(_citizenPopulationCompositionSystem);

        var runtimeUpdate = new BuildingRuntimeUpdateSystem();
        BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext =
            childSystems.BuildingRuntimeContextSystem.CreateSpawnCommandContext(
                buildingRuntimeContextSource,
                childSystems.BuildingRuntimeSpawnSystem);
        Func<BuildingSpawnSystem.Context> createSpawnContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(createRuntimeContextSource(childSystems));
        };
        Func<BuildingBarrierSystem.Context> createBarrierContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(childSystems));
        };
        Func<BuildingCombatSystem.Context<RuntimeBuildingEntity>> createCombatContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateCombatContext(createRuntimeContextSource(childSystems));
        };
        BuildingPlacementRuntimeTickSystem.Context runtimeTickContext = default;
        bool runtimeTickContextReady = false;
        bool EnsureRuntimeTickContext()
        {
            if (runtimeTickContextReady)
                return true;

            if (!tryGetEntityManager(out EntityManager em))
                return false;

            childSystems.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
            BuildingPlacementRuntimeTickContextCompositionSystemHelper.Source runtimeTickSource = _runtimeTickCompositionHelper.Create(
                childSystems,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                (source, placementInteractionContext, placementMarkerPropertyBlock) => _placementInputTickCompositionHelper.Create(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    rtsSelectionConfig != null ? rtsSelectionConfig.DragThresholdPixels : 8f,
                    createPlacementCommandContext,
                    (pointerSource, pointerInteractionContext, pointerMarkerPropertyBlock) =>
                        _placementInteractionCompositionHelper.CreateActivePlacementPointerContext(
                            pointerSource,
                            pointerInteractionContext,
                            pointerMarkerPropertyBlock,
                            tryGetGridForPlacementInput,
                            tryGetGridCell,
                            updatePlacementForInteraction),
                    source => source.BuildingSelectionClickCompositionHelper.Create(
                        source,
                        tryGetGridForSelection,
                        tryGetGridCell,
                        createBuildingSelectionContext)),
                source => _productionTickCompositionHelper.Create(
                    source,
                    productionSource => productionSource.BuildingProductionCompositionSystem.CreateRuntimeContextSource(
                        productionSource,
                        createRuntimeContextSource,
                        createPlacementCommandContext),
                    OilBarrelsPerFuelBarrel),
                (source, placementInteractionContext, placementMarkerPropertyBlock) => _runtimeBoundaryCompositionHelper.Create(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    createBuildingRuntimeContextSource,
                    boundarySource => boundarySource.BuildingProductionCompositionSystem.CreateRuntimeContextSource(
                        boundarySource,
                        createRuntimeContextSource,
                        createPlacementCommandContext),
                    createRuntimeContextSource),
                (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                {
                    BuildingRuntimeContextSystem.Source mapRuntimeContextSource =
                        createBuildingRuntimeContextSource(source, placementInteractionContext, placementMarkerPropertyBlock);
                    BuildingRuntimeSpawnSystem.Context mapSpawnContext =
                        source.BuildingRuntimeContextSystem.CreateSpawnContext(mapRuntimeContextSource);
                    bool TryGetMapGridData(
                        out Entity gridEntity,
                        out GridConfig grid,
                        out DynamicBuffer<GridRoad> roads,
                        out DynamicBlockerComponent blockerData)
                    {
                        return tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData);
                    }

                    MapBuildingPlacementSpawnSystem.Context mapSpawnPlacementContext =
                        new(
                            mapBuildingPlacementConfig,
                            mapBuildingAuthoringRoot,
                            source.BuildingRuntimeSpawnSystem,
                            mapSpawnContext,
                            TryGetMapGridData,
                            Debug.LogWarning);
                    return () => source.MapBuildingPlacementSpawnSystem.Update(mapSpawnPlacementContext);
                },
                (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                {
                    RuntimeUnitPrefabSystem.Context mapVehiclePrefabContext =
                        BuildingRuntimeResourcePrefabContextSystem.CreateRuntimeUnitPrefabContext(
                            source.BuildingRuntimeResourcePrefabContextSystem,
                            runtimeResourcePrefabSource);

                    bool TryGetMapGridData(
                        out Entity gridEntity,
                        out GridConfig grid,
                        out DynamicBuffer<GridRoad> roads,
                        out DynamicBlockerComponent blockerData)
                    {
                        return tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData);
                    }

                    bool TryGetMapRuntimeBoundary(EntityManager em, out Entity boundaryEntity)
                    {
                        source.BuildingGameplayEcsQuerySystem.EnsureEntityQueries(em);
                        EntityQuery boundaryQuery = source.BuildingGameplayEcsQuerySystem.BuildingRuntimeBoundaryQuery;
                        if (boundaryQuery.IsEmptyIgnoreFilter)
                        {
                            boundaryEntity = Entity.Null;
                            return false;
                        }

                        boundaryEntity = boundaryQuery.GetSingletonEntity();
                        return boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
                    }

                    MapVehiclePlacementSpawnSystem.Context mapVehiclePlacementContext =
                        new(
                            mapVehiclePlacementConfig,
                            mapVehicleAuthoringRoot,
                            source.RuntimeUnitPrefabSystem,
                            mapVehiclePrefabContext,
                            TryGetMapGridData,
                            TryGetMapRuntimeBoundary,
                            Debug.LogWarning);
                    return () => source.MapVehiclePlacementSpawnSystem.Update(mapVehiclePlacementContext);
                },
                DestroyedBuildingLifetimeSeconds);
            runtimeTickContext = _runtimeTickContextCompositionHelper.Create(runtimeTickSource);
            runtimeTickContextReady = true;
            return true;
        }
        void UpdateBuildingStartupTick()
        {
            if (EnsureRuntimeTickContext())
                childSystems.BuildingPlacementRuntimeTickSystem.UpdateStartup(runtimeTickContext);
        }
        void UpdateBuildingSimulationTick()
        {
            if (EnsureRuntimeTickContext())
                childSystems.BuildingPlacementRuntimeTickSystem.UpdateSimulation(runtimeTickContext);
        }
        return _resultSystem.Create(
            childSystems.BuildingSelectionClickSystem,
            childSystems.BuildingSelectionClickCompositionHelper.Create(
                childSystems,
                tryGetGridForSelection,
                tryGetGridCell,
                createBuildingSelectionContext),
            runtimeUpdate,
            new BuildingRuntimeUpdateSystem.Context(UpdateBuildingStartupTick, UpdateBuildingSimulationTick),
            childSystems.BuildingRuntimeCitySpawnSystem,
            childSystems.BuildingRuntimeContextSystem.CreateCitySpawnContext(
                buildingRuntimeContextSource,
                childSystems.BuildingRuntimeSpawnCommandBoundary,
                runtimeSpawnCommandContext,
                childSystems.BuildingRuntimeBoundarySystem),
            childSystems.BuildingRuntimeQuerySystem,
            childSystems.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(childSystems)),
            childSystems.BuildingRuntimeSpawnCommandBoundary,
            runtimeSpawnCommandContext,
            childSystems.BuildingSpawnSystem,
            createSpawnContext(),
            createSpawnContext,
            childSystems.BuildingBarrierSystem,
            createBarrierContext,
            childSystems.BuildingCombatSystem,
            createCombatContext,
            childSystems.BuildingUiCommandBoundary,
            childSystems.BuildingUiCompositionSystem.CreateCommandContext(
                childSystems,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createPlacementQueryContext,
                createBuildingSelectionContext),
            childSystems.BuildingUiQuerySystem,
            childSystems.BuildingUiCompositionSystem.CreateQueryContext(
                childSystems,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createPlacementQueryContext,
                createBuildingSelectionContext),
            childSystems.BuildingPlacementInteractionSystem,
            interactionContext,
            childSystems.BuildingGameplayDependencySystem,
            childSystems.BuildingRuntimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            _citizenPopulationCompositionSystem,
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            childSystems.RuntimeBuildingSystem.Buildings,
            screenRect => childSystems.BuildingSelectionSystem.SelectFirstBuildingInScreenRect(
                createBuildingSelectionContext(childSystems),
                screenRect),
            _bindingCompositionHelper.CreateMainMenuBinding(childSystems, dayNight),
            _bindingCompositionHelper.CreateGameplayFeatureBinding(childSystems, dayNight),
            _disposalCompositionHelper.CreateDisposeAction(
                childSystems,
                () => createPlacementCommandContext(childSystems, interactionContext, markerPropertyBlock)));
    }

    private static BuildingCitizenPopulationCompositionSystem ResolveBuildingCitizenPopulationCompositionSystem()
    {
        return new BuildingCitizenPopulationCompositionSystem();
    }
}
