using System;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
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
        private readonly BuildingRuntimeCompositionSystemHelper _runtimeBoundaryCompositionHelper = new();
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
            BuildingProductionQueueCompositionSystemHelper.TryGetUnitProductionMetadataDelegate tryGetUnitProductionMetadata = null,
            BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null,
            Func<GameObject, string> resolveSpawnableLookupKey = null,
            BuildingDefinitionPrefabSystemHelper.TryGetBuildingDefinitionMetadataDelegate tryGetBuildingDefinitionMetadata = null,
            BuildingDefinitionPrefabSystemHelper.TryGetUnitDefinitionMetadataDelegate tryGetUnitDefinitionMetadata = null,
            bool requirePackedVehiclePresentationContract = false)
        {
            MaterialPropertyBlock markerPropertyBlock = BuildingMarkerVisualPresentationSystemHelper.GetMarkerPropertyBlock(_markerVisualPresentationHelper);
            BuildingGameplaySourceCompositionSystemHelper childSystems = _childSystem.Create();
            childSystems.BuildingDefinitionPrefabSystemHelper.ConfigureAuthoringMetadataResolvers(
                tryGetBuildingDefinitionMetadata,
                tryGetUnitDefinitionMetadata);
            childSystems.BuildingProductionQueueCompositionSystemHelper.ConfigureUnitProductionMetadataResolver(tryGetUnitProductionMetadata);
            childSystems.BuildingProductionTransportPresentationSystemHelper.SetRuntimeRoot(runtimeTransportsRoot);
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
            BuildingRuntimeContextCompositionSystemHelper.OverlapsAnyPlacementOccupantDelegate overlapsAnyPlacementOccupant =
                (source, candidateRect) =>
                    source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyRuntimeBuilding(
                        source,
                        candidateRect,
                        tryGetGridData,
                        (querySource, definition, originCell, grid, rotateVertical) => getEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical)) ||
                    source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
                        source,
                        candidateRect,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager));
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource =
                source => source.BuildingRuntimeContextCompositionSystemHelper.CreateRuntimeContextSource(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect);
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeEntityCompositionSystemHelper.Context> createBuildingRuntimeEntityContext =
                source => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeEntityContext(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect,
                    DestroyedBuildingLifetimeSeconds);
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource =
                (source, placementInteractionContext, placementMarkerPropertyBlock) => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeContextSource(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    tryGetEntityManager,
                    tryGetGridData,
                    getEffectivePlacementRect,
                    overlapsAnyPlacementOccupant,
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
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext =
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
                        (placementSource, candidateRect) => overlapsAnyPlacementOccupant(placementSource, candidateRect));
            BuildingPlacementCommandCompositionSystemHelper.GetCenterScreenPlacementOriginDelegate getCenterScreenPlacementOrigin =
                (source, footprintCells) => source.BuildingPlacementAdapterCompositionSystemHelper.GetCenterScreenPlacementOrigin(
                    source,
                    footprintCells,
                    tryGetGridData);
            BuildingPlacementCommandCompositionSystemHelper.TryResolveInitialPlacementOriginDelegate tryResolveInitialPlacementOrigin =
                (
                    BuildingGameplaySourceCompositionSystemHelper source,
                    BuildingPlacementInteractionCompositionSystemHelper.Context placementInteractionContext,
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
                    source.BuildingPlacementVisualCompositionPresentationSystemHelper != null
                        ? source.BuildingPlacementVisualCompositionPresentationSystemHelper.PlaceBuilding(
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
                            createBuildingSelectionContext)
                        : default;
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
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext =
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
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext = default;
            void BeginSoldierBasePlacement()
            {
                BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext =
                    createPlacementCommandContext(childSystems, interactionContext, markerPropertyBlock);
                if (tryGetEntityManager(out EntityManager em))
                {
                    childSystems.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessBeginSoldierBasePlacement(em, commandContext);
                    return;
                }

                if (childSystems.BuildingPlacementStartupSystemHelper.SoldierBaseDefinition != null)
                    commandContext.SessionSystem?.BeginPlacement(commandContext.SessionContext, childSystems.BuildingPlacementStartupSystemHelper.SoldierBaseDefinition);
            }

            bool ConfirmBuildingPlacement()
            {
                BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext =
                    createPlacementCommandContext(childSystems, interactionContext, markerPropertyBlock);
                return tryGetEntityManager(out EntityManager em)
                    ? childSystems.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessConfirmBuildingPlacement(em, commandContext)
                    : commandContext.SessionSystem != null && commandContext.SessionSystem.ConfirmBuildingPlacement(commandContext.SessionContext);
            }

            void CancelBuildingPlacement()
            {
                BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext =
                    createPlacementCommandContext(childSystems, interactionContext, markerPropertyBlock);
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessCancelBuildingPlacement(em, commandContext);
                else
                    commandContext.SessionSystem?.CancelBuildingPlacement(commandContext.SessionContext);
            }

            void CreateUnitFromSelectedBuilding()
            {
                if (!tryGetEntityManager(out EntityManager em))
                    return;

                BuildingProductionContextCompositionSystemHelper.Source productionSource =
                    childSystems.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
                        childSystems,
                        createRuntimeContextSource,
                        createPlacementCommandContext,
                        interactionContext,
                        markerPropertyBlock);
                BuildingProductionRequestSystemHelper.Context productionContext =
                    childSystems.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(productionSource);
                childSystems.BuildingProductionRequestSystemHelper.EnqueueAndProcessCreateUnitFromSelectedBuilding(
                    em,
                    productionContext,
                    childSystems.RuntimeBuildingSystem.CurrentActiveBuildingId,
                    productionIndex: 0,
                    frameCount: Time.frameCount);
            }

            void DeleteSelectedBuilding()
            {
                BuildingSelectionRuntimeCompositionSystemHelper.Context selectionContext =
                    createBuildingSelectionContext(childSystems);
                bool DeleteBuildingById(int buildingId)
                {
                    return childSystems.BuildingRuntimeEntityCompositionSystemHelper.DeleteBuildingById(
                        createBuildingRuntimeEntityContext(childSystems),
                        buildingId);
                }

                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingSelectionRuntimeCompositionSystemHelper.EnqueueAndProcessDeleteSelectedBuilding(em, selectionContext, DeleteBuildingById);
                else
                    childSystems.BuildingSelectionRuntimeCompositionSystemHelper.DeleteSelectedBuilding(selectionContext, DeleteBuildingById);
            }

            void ClearSelectedBuilding(string reason)
            {
                BuildingSelectionRuntimeCompositionSystemHelper.Context selectionContext =
                    createBuildingSelectionContext(childSystems);
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingSelectionRuntimeCompositionSystemHelper.EnqueueAndProcessClearSelectedBuilding(em, selectionContext);
                else
                    childSystems.BuildingSelectionRuntimeCompositionSystemHelper.ClearSelectedBuilding(selectionContext);
            }

            void ExitBuildMode()
            {
                BuildingPlacementCommandRequestCompositionSystemHelper.Context commandContext =
                    createPlacementCommandContext(childSystems, interactionContext, markerPropertyBlock);
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessExitBuildMode(em, commandContext);
                else
                    commandContext.SessionSystem?.ExitBuildMode(commandContext.SessionContext);
            }

            bool TryResolveSelectedBuildingFollowTarget(out Vector3 worldPosition, out float boundsRadius)
            {
                worldPosition = Vector3.zero;
                boundsRadius = 0f;
                int? buildingId = childSystems.RuntimeBuildingSystem.CurrentActiveBuildingId;
                if (!buildingId.HasValue)
                    return false;

                BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource runtimeSource =
                    createRuntimeContextSource(childSystems);
                if (runtimeSource.TryGetRuntimeBuilding == null ||
                    !runtimeSource.TryGetRuntimeBuilding(buildingId.Value, out RuntimeBuildingEntity building) ||
                    building == null)
                {
                    return false;
                }

                worldPosition = BuildingRuntimeFocusPositionPresentationSystemHelper.Resolve(runtimeSource, building);
                if (building.Definition != null)
                    boundsRadius = Mathf.Max(1f, Mathf.Max(building.Definition.FootprintCells.x, building.Definition.FootprintCells.y) * 0.5f);
                else if (building.Instance != null)
                    boundsRadius = Mathf.Max(1f, building.Instance.transform.localScale.magnitude);
                else
                    boundsRadius = 1f;
                return true;
            }

            interactionContext = childSystems.BuildingPlacementInteractionContextCompositionSystemHelper.CreateContext(
                childSystems.BuildingPlacementInteractionContextCompositionSystemHelper.CreateSource(
                    () => childSystems.BuildingPlacementLifecycleCompositionSystemHelper.HasPendingBuildingPlacement,
                    () => childSystems.BuildingPlacementLifecycleCompositionSystemHelper.CanConfirmBuildingPlacement,
                    childSystems.RuntimeBuildingSystem.HasSelectedBuilding,
                    () => childSystems.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
                    () => childSystems.BuildingPlacementInputUiSystemHelper.IsDraggingPlacement,
                    () => childSystems.BuildingPlacementQueryUiSystemHelper.GetPlacementStatusText(
                        childSystems.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement),
                    () => childSystems.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingLabel(
                        createPlacementQueryContext(childSystems)),
                    BeginSoldierBasePlacement,
                    ConfirmBuildingPlacement,
                    CancelBuildingPlacement,
                    CreateUnitFromSelectedBuilding,
                    DeleteSelectedBuilding,
                    ClearSelectedBuilding,
                    ExitBuildMode,
                    (buildingId, blockerEntity, buildingObject) => childSystems.BuildingRuntimeEntityCompositionSystemHelper.HandleRuntimeBuildingEntityDestroyed(
                        createBuildingRuntimeEntityContext(childSystems),
                        buildingId,
                        blockerEntity,
                        buildingObject),
                    (byte attackerFactionId,
                        Entity finalTarget,
                        Unity.Mathematics.int2 finalTargetCell,
                        Unity.Mathematics.int2 attackerCell,
                        out Entity breachTarget,
                        out Unity.Mathematics.int2 breachCell,
                        out Unity.Mathematics.float3 breachPosition,
                        out string reason) => childSystems.BuildingRuntimeReadModelCompositionSystemHelper.TryResolveBaseBreachTarget(
                        childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(childSystems)),
                        attackerFactionId,
                        finalTarget,
                        finalTargetCell,
                        attackerCell,
                        out breachTarget,
                        out breachCell,
                        out breachPosition,
                        out reason),
                    TryResolveSelectedBuildingFollowTarget,
                    (out int oilCurrent, out int oilCapacity, out int fuelCurrent, out int fuelCapacity) =>
                        childSystems.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingResourceStorage(
                            createPlacementQueryContext(childSystems),
                            out oilCurrent,
                            out oilCapacity,
                            out fuelCurrent,
                            out fuelCapacity),
                    (out SelectedBuildingResourceStorageSnapshot snapshot) =>
                        childSystems.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingResourceStorageSnapshot(
                            createPlacementQueryContext(childSystems),
                            out snapshot)));
            BuildingRuntimeContextFactoryCompositionSystemHelper.Source buildingRuntimeContextSource =
                createBuildingRuntimeContextSource(childSystems, interactionContext, markerPropertyBlock);
            CitizenPopulationCompositionSystemHelper citizenPopulationCompositionBoundary =
                BuildingCitizenPopulationCompositionSystemHelper.CreateBoundary(_citizenPopulationCompositionSystem);
            CitizenPopulationCompositionSystemHelper.Result citizenPopulationComposition =
                BuildingCitizenPopulationCompositionSystemHelper.Create(_citizenPopulationCompositionSystem);

            var runtimeUpdate = new BuildingRuntimeUpdateCompositionSystemHelper();
            BuildingRuntimeSpawnCommandSystemHelper.Context runtimeSpawnCommandContext =
                childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnCommandContext(
                    buildingRuntimeContextSource,
                    childSystems.BuildingRuntimeSpawnCompositionSystemHelper);
            Func<BuildingSpawnCompositionSystemHelper.Context> createSpawnContext = () =>
            {
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
                return childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBuildingSpawnContext(createRuntimeContextSource(childSystems));
            };
            Func<BuildingBarrierUtilitySystemHelper.Context> createBarrierContext = () =>
            {
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
                return childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(createRuntimeContextSource(childSystems));
            };
            Func<BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>> createCombatContext = () =>
            {
                if (tryGetEntityManager(out EntityManager em))
                    childSystems.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
                return childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCombatContext(createRuntimeContextSource(childSystems));
            };
            BuildingPlacementRuntimeTickCompositionSystemHelper.Context runtimeTickContext = default;
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
                            pointerSource.BuildingPlacementContextCompositionSystemHelper.CreateActivePlacementPointerContext(
                                createPlacementContextSource(pointerSource, pointerInteractionContext, pointerMarkerPropertyBlock)),
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
                        BuildingRuntimeContextFactoryCompositionSystemHelper.Source mapRuntimeContextSource =
                            createBuildingRuntimeContextSource(source, placementInteractionContext, placementMarkerPropertyBlock);
                        BuildingRuntimeSpawnCompositionSystemHelper.Context mapSpawnContext =
                            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(mapRuntimeContextSource);
                        bool TryGetMapGridData(
                            out Entity gridEntity,
                            out GridConfig grid,
                            out DynamicBuffer<GridRoad> roads,
                            out DynamicBlockerComponent blockerData)
                        {
                            return tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData);
                        }

                        MapBuildingPlacementSpawnPrefabSystemHelper.Context mapSpawnPlacementContext =
                            new(
                                mapBuildingPlacementConfig,
                                mapBuildingAuthoringRoot,
                                source.BuildingRuntimeSpawnCompositionSystemHelper,
                                mapSpawnContext,
                                TryGetMapGridData,
                                Debug.LogWarning);
                        return () => source.MapBuildingPlacementSpawnPrefabSystemHelper.Update(mapSpawnPlacementContext);
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
                            EntityQuery boundaryQuery = source.BuildingGameplayEcsQueryCompositionSystemHelper.BuildingRuntimeStateQuery;
                            if (boundaryQuery.IsEmptyIgnoreFilter)
                            {
                                boundaryEntity = Entity.Null;
                                return false;
                            }

                            boundaryEntity = boundaryQuery.GetSingletonEntity();
                            return boundaryEntity != Entity.Null && em.Exists(boundaryEntity);
                        }

                        MapVehiclePlacementSpawnPrefabSystemHelper.Context mapVehiclePlacementContext =
                            new(
                                mapVehiclePlacementConfig,
                                mapVehicleAuthoringRoot,
                                source.RuntimeUnitPrefabSystem,
                                mapVehiclePrefabContext,
                                TryGetMapGridData,
                                TryGetMapRuntimeBoundary,
                                Debug.LogWarning,
                                requirePackedVehiclePresentationContract);
                        return () => source.MapVehiclePlacementSpawnPrefabSystemHelper.Update(mapVehiclePlacementContext);
                    },
                    DestroyedBuildingLifetimeSeconds);
                runtimeTickContext = _runtimeTickContextCompositionHelper.Create(runtimeTickSource);
                runtimeTickContextReady = true;
                return true;
            }
            void UpdateBuildingStartupTick()
            {
                if (EnsureRuntimeTickContext())
                    childSystems.BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateStartup(runtimeTickContext);
            }
            void UpdateBuildingSimulationTick()
            {
                if (EnsureRuntimeTickContext())
                    childSystems.BuildingPlacementRuntimeTickCompositionSystemHelper.UpdateSimulation(runtimeTickContext);
            }
            bool IsBuildingStartupComplete()
            {
                bool placementsComplete =
                    childSystems.MapBuildingPlacementSpawnPrefabSystemHelper.IsCompleteFor(
                        mapBuildingPlacementConfig,
                        mapBuildingAuthoringRoot) &&
                    childSystems.MapVehiclePlacementSpawnPrefabSystemHelper.IsCompleteFor(
                        mapVehiclePlacementConfig,
                        mapVehicleAuthoringRoot);
                if (!placementsComplete)
                    return false;
                if (!requirePackedVehiclePresentationContract)
                    return true;
                return tryGetEntityManager(out EntityManager em) &&
                       MapVehiclePlacementSpawnPrefabSystemHelper.IsAuthoredVehicleOwnershipReady(
                           em,
                           mapVehiclePlacementConfig,
                           requireReadinessContract: true);
            }
            return _resultSystem.Create(
                childSystems.BuildingSelectionClickUtilitySystemHelper,
                childSystems.BuildingSelectionClickCompositionHelper.Create(
                childSystems,
                tryGetGridForSelection,
                tryGetGridCell,
                createBuildingSelectionContext),
                runtimeUpdate,
                new BuildingRuntimeUpdateCompositionSystemHelper.Context(
                    UpdateBuildingStartupTick,
                    UpdateBuildingSimulationTick,
                    childSystems.RuntimeBuildingEntityLinkRegistry,
                    IsBuildingStartupComplete),
                childSystems.BuildingRuntimeCitySpawnBridgeCompositionSystemHelper,
                childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCitySpawnContext(
                    buildingRuntimeContextSource,
                    childSystems.BuildingRuntimeSpawnCommandSystemHelper,
                    runtimeSpawnCommandContext,
                    childSystems.BuildingRuntimeProcessingCompositionSystemHelper),
                childSystems.BuildingRuntimeReadModelCompositionSystemHelper,
                childSystems.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(createRuntimeContextSource(childSystems)),
                childSystems.BuildingRuntimeSpawnCommandSystemHelper,
                runtimeSpawnCommandContext,
                childSystems.BuildingSpawnCompositionSystemHelper,
                createSpawnContext(),
                createSpawnContext,
                childSystems.BuildingBarrierUtilitySystemHelper,
                createBarrierContext,
                childSystems.BuildingCombatUtilitySystemHelper,
                createCombatContext,
                childSystems.BuildingUiCommandSystemHelper,
                childSystems.BuildingUiCompositionSystemHelper.CreateCommandContext(
                    childSystems,
                    interactionContext,
                    markerPropertyBlock,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    createPlacementQueryContext,
                    createBuildingSelectionContext),
                childSystems.BuildingUiQueryUiSystemHelper,
                childSystems.BuildingUiCompositionSystemHelper.CreateQueryContext(
                    childSystems,
                    interactionContext,
                    markerPropertyBlock,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    createPlacementQueryContext,
                    createBuildingSelectionContext),
                childSystems.BuildingPlacementInteractionCompositionSystemHelper,
                interactionContext,
                childSystems.BuildingGameplayDependencyCompositionSystemHelper,
                childSystems.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
                runtimeResourcePrefabSource,
                childSystems.RuntimeBuildingEntityLinkRegistry,
                _citizenPopulationCompositionSystem,
                citizenPopulationCompositionBoundary,
                citizenPopulationComposition,
                childSystems.RuntimeBuildingSystem.Buildings,
                screenRect => childSystems.BuildingSelectionRuntimeCompositionSystemHelper.SelectFirstBuildingInScreenRect(
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
}
