using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingGameplayCompositionSystemHelper
{
    private const float DestroyedBuildingLifetimeSeconds = 5f;
    private const float OilBarrelsPerFuelBarrel = 2f;
    private readonly BuildingGameplayChildSystem _childSystem = new();
    private readonly BuildingGameplayStartupCompositionSystemHelper _startupCompositionHelper = new();
    private readonly BuildingGameplayBindingCompositionSystemHelper _bindingCompositionHelper = new();
    private readonly BuildingCitizenPopulationCompositionSystemHelper _citizenPopulationCompositionSystem = ResolveBuildingCitizenPopulationCompositionSystemHelper();
    private readonly BuildingGameplayDisposalCompositionSystemHelper _disposalCompositionHelper = new();
    private readonly BuildingMarkerVisualPresentationSystemHelper _markerVisualPresentationHelper = new();
    private readonly BuildingRuntimeTickCompositionSystemHelper _runtimeTickCompositionHelper = new();
    private readonly BuildingPlacementInputTickCompositionSystemHelper _placementInputTickCompositionHelper = new();
    private readonly BuildingRuntimeBoundaryCompositionSystemHelper _runtimeBoundaryCompositionHelper = new();
    private readonly BuildingProductionTickCompositionSystemHelper _productionTickCompositionHelper = new();
    private readonly BuildingPlacementInteractionCompositionSystemHelper _placementInteractionCompositionHelper = new();
    private readonly BuildingPlacementRuntimeTickContextCompositionSystemHelper _runtimeTickContextCompositionHelper = new();
    private readonly BuildingGameplayResultCompositionSystemHelper _resultSystem = new();

    public BuildingGameplayResultCompositionSystemHelper.Result Initialize(
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
        BuildingDefinitionPrefabSystemHelper.TryGetBuildingDefinitionMetadataDelegate tryGetBuildingDefinitionMetadata = null,
        BuildingDefinitionPrefabSystemHelper.TryGetUnitDefinitionMetadataDelegate tryGetUnitDefinitionMetadata = null)
    {
        MaterialPropertyBlock markerPropertyBlock = BuildingMarkerVisualPresentationSystemHelper.GetMarkerPropertyBlock(_markerVisualPresentationHelper);
        BuildingGameplaySourceCompositionSystemHelper childSystems = _childSystem.Create();
        childSystems.BuildingDefinitionPrefabSystemHelper.ConfigureAuthoringMetadataResolvers(
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
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource =
            BuildingRuntimeResourcePrefabCompositionSystemHelper.Create(
                childSystems.BuildingRuntimeResourcePrefabCompositionHelper,
                childSystems);
        bool tryGetEntityManager(out EntityManager entityManager)
        {
            return childSystems.BuildingEntityManagerAccessSystem.TryGetEntityManager(out entityManager);
        }
        BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetGridEntityManager = tryGetEntityManager;

        bool tryGetGridData(
            BuildingGameplaySourceCompositionSystemHelper source,
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

        bool tryGetGridForSelection(BuildingGameplaySourceCompositionSystemHelper source, out GridConfig grid)
        {
            return source.BuildingGridCompositionSystem.TryGetGridForSelection(
                source,
                tryGetGridEntityManager,
                out grid);
        }

        bool tryGetGridForPlacementInput(BuildingGameplaySourceCompositionSystemHelper source, out GridConfig grid)
        {
            return source.BuildingGridCompositionSystem.TryGetGridForPlacementInput(
                source,
                tryGetGridEntityManager,
                out grid);
        }

        bool tryGetGridCell(
            BuildingGameplaySourceCompositionSystemHelper source,
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

        BuildingRuntimeContextCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect =
            (source, definition, originCell, grid, rotateVertical) => source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(
                source,
                definition,
                originCell,
                grid,
                rotateVertical);
        BuildingRuntimeContextCompositionSystemHelper.IsHouseBuildingDelegate isHouseBuilding =
            (source, building) => source.BuildingRuntimeQueryCompositionSystemHelper.IsHouseBuilding(source, building);
        BuildingRuntimeContextCompositionSystemHelper.TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition =
            (BuildingGameplaySourceCompositionSystemHelper source, RuntimeBuildingEntity building, out Vector3 worldPosition) =>
                source.BuildingRuntimeQueryCompositionSystemHelper.TryResolveBuildingFocusWorldPosition(
                    source,
                    building,
                    tryGetEntityManager,
                    out worldPosition);
        BuildingRuntimeContextCompositionSystemHelper.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding =
            (BuildingGameplaySourceCompositionSystemHelper source, int id, out RuntimeBuildingEntity building) =>
                source.BuildingRuntimeQueryCompositionSystemHelper.TryGetRuntimeBuilding(source, id, out building);
        BuildingRuntimeContextCompositionSystemHelper.OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding =
            (source, candidateRect) => source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyRuntimeBuilding(
                source,
                candidateRect,
                tryGetGridData,
                (querySource, definition, originCell, grid, rotateVertical) => getEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical));
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource =
            source => source.BuildingRuntimeContextCompositionSystemHelper.CreateRuntimeContextSource(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect);
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeEntitySystem.Context> createBuildingRuntimeEntityContext =
            source => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeEntityContext(
                source,
                tryGetEntityManager,
                tryGetGridData,
                isHouseBuilding,
                tryResolveBuildingFocusWorldPosition,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect,
                DestroyedBuildingLifetimeSeconds);
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource =
            (source, placementInteractionContext, placementMarkerPropertyBlock) => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeContextSource(
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
        BuildingPlacementAdapterCompositionSystemHelper.CreateRuntimeContextSourceDelegate createRuntimeContextSourceForAdapter =
            source => createRuntimeContextSource(source);
        BuildingPlacementAdapterCompositionSystemHelper.CreateBuildingRuntimeContextSourceDelegate createBuildingRuntimeContextSourceForAdapter =
            (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                createBuildingRuntimeContextSource(source, placementInteractionContext, placementMarkerPropertyBlock);
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createPlacementQueryContext =
            source => source.BuildingPlacementQueryCompositionSystem.Create(source);
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext =
            source => source.BuildingSelectionCompositionHelper.Create(
                source,
                tryGetGridForSelection,
                resolveSelectionPortraitSpriteFromPrefab,
                createRuntimeContextSource);
        BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValidDelegate isPlacementValid =
            (source, definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData) =>
                source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(
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
            (source, footprintCells) => source.BuildingPlacementAdapterCompositionSystemHelper.GetCenterScreenPlacementOrigin(
                source,
                footprintCells,
                tryGetGridData);
        BuildingPlacementCommandCompositionSystemHelper.TryResolveInitialPlacementOriginDelegate tryResolveInitialPlacementOrigin =
            (
                BuildingGameplaySourceCompositionSystemHelper source,
                BuildingPlacementInteractionSystem.Context placementInteractionContext,
                MaterialPropertyBlock placementMarkerPropertyBlock,
                BuildingDefinition definition,
                Vector2Int preferredOrigin,
                out Vector2Int resolvedOrigin) => source.BuildingPlacementAdapterCompositionSystemHelper.TryResolveInitialPlacementOrigin(
                source,
                placementInteractionContext,
                placementMarkerPropertyBlock,
                definition,
                preferredOrigin,
                createBuildingRuntimeContextSourceForAdapter,
                out resolvedOrigin);
        BuildingPlacementVisualCompositionPresentationSystemHelper.IsActivePlacementValidDelegate isActivePlacementValid =
            (source, originCell, footprintCells, grid, roads, blockerData) => source.BuildingPlacementAdapterCompositionSystemHelper.IsActivePlacementValid(
                source,
                originCell,
                footprintCells,
                grid,
                roads,
                blockerData,
                createRuntimeContextSourceForAdapter,
                isPlacementValid);
        BuildingPlacementCommandCompositionSystemHelper.TryAlignGateToNearbyWallDelegate tryAlignGateForCommand =
            (BuildingGameplaySourceCompositionSystemHelper source, Vector2Int originCell, BuildingDefinition definition, out bool gateVertical) =>
                source.BuildingPlacementAdapterCompositionSystemHelper.TryAlignGateToNearbyWall(
                    source,
                    originCell,
                    definition,
                    createRuntimeContextSourceForAdapter,
                    out gateVertical);
        BuildingPlacementVisualCompositionPresentationSystemHelper.TryAlignGateToNearbyWallDelegate tryAlignGateForVisual =
            (BuildingGameplaySourceCompositionSystemHelper source, Vector2Int originCell, BuildingDefinition definition, out bool gateVertical) =>
                source.BuildingPlacementAdapterCompositionSystemHelper.TryAlignGateToNearbyWall(
                    source,
                    originCell,
                    definition,
                    createRuntimeContextSourceForAdapter,
                    out gateVertical);
        BuildingPlacementVisualCompositionPresentationSystemHelper.CreatePlacementContextSourceDelegate createPlacementContextSource = null;
        BuildingPlacementCommandCompositionSystemHelper.UpdatePlacementVisualDelegate updatePlacementVisual =
            (source, placementInteractionContext, placementMarkerPropertyBlock, placement, updateCellFromPointer, screenPosition) =>
                source.BuildingPlacementVisualCompositionPresentationSystemHelper?.UpdatePlacementVisual(
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
                source.BuildingPlacementVisualCompositionPresentationSystemHelper?.FocusActivePlacement(
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
                source.BuildingPlacementVisualCompositionPresentationSystemHelper != null &&
                source.BuildingPlacementVisualCompositionPresentationSystemHelper.ValidateActivePlacementForConfirm(
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
                source.BuildingPlacementVisualCompositionPresentationSystemHelper?.PlaceBuilding(
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
                source.BuildingPlacementVisualCompositionPresentationSystemHelper?.UpdatePlacement(
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
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext =
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
                source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(
                    source.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
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
            BuildingCitizenPopulationCompositionSystemHelper.CreateBoundary(_citizenPopulationCompositionSystem);
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition =
            BuildingCitizenPopulationCompositionSystemHelper.Create(_citizenPopulationCompositionSystem);

        var runtimeUpdate = new BuildingRuntimeUpdateSystem();
        BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext =
            childSystems.BuildingRuntimeContextSystem.CreateSpawnCommandContext(
                buildingRuntimeContextSource,
                childSystems.BuildingRuntimeSpawnSystem);
        Func<BuildingSpawnSystem.Context> createSpawnContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(createRuntimeContextSource(childSystems));
        };
        Func<BuildingBarrierUtilitySystemHelper.Context> createBarrierContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
            return childSystems.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(childSystems));
        };
        Func<BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>> createCombatContext = () =>
        {
            if (tryGetEntityManager(out EntityManager em))
                childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
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

            childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
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
                    productionSource => productionSource.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
                        productionSource,
                        createRuntimeContextSource,
                        createPlacementCommandContext),
                    OilBarrelsPerFuelBarrel),
                (source, placementInteractionContext, placementMarkerPropertyBlock) => _runtimeBoundaryCompositionHelper.Create(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    createBuildingRuntimeContextSource,
                    boundarySource => boundarySource.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
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
                        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateRuntimeUnitPrefabContext(
                            source.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
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
                        source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
                        EntityQuery boundaryQuery = source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeBoundaryQuery;
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
            new BuildingRuntimeUpdateSystem.Context(
                UpdateBuildingStartupTick,
                UpdateBuildingSimulationTick,
                childSystems.RuntimeBuildingEntityLinkRegistry),
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
            childSystems.BuildingBarrierUtilitySystemHelper,
            createBarrierContext,
            childSystems.BuildingCombatUtilitySystemHelper,
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
            childSystems.BuildingGameplayDependencyCompositionSystemHelper,
            childSystems.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
            runtimeResourcePrefabSource,
            childSystems.RuntimeBuildingEntityLinkRegistry,
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

    private static BuildingCitizenPopulationCompositionSystemHelper ResolveBuildingCitizenPopulationCompositionSystemHelper()
    {
        return new BuildingCitizenPopulationCompositionSystemHelper();
    }
}
