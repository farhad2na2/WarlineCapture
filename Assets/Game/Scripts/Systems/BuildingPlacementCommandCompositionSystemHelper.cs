using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

    internal sealed class BuildingPlacementCommandCompositionSystemHelper
    {
        internal delegate Vector2Int GetCenterScreenPlacementOriginDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2Int footprintCells);

        internal delegate bool TryResolveInitialPlacementOriginDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            BuildingDefinition definition,
            Vector2Int preferredOrigin,
            out Vector2Int resolvedOrigin);

        internal delegate void UpdatePlacementVisualDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            PlacementState placement,
            bool updateCellFromPointer,
            Vector2 screenPosition);

        internal delegate void FocusActivePlacementDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            PlacementState placement);

        internal delegate bool ValidateActivePlacementForConfirmDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            PlacementState placement);

        internal delegate BuildingPlacementCommitCompositionSystemHelper.CommitOutcome PlaceBuildingDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            PlacementState placement);

        internal delegate bool TryGetGridForPlacementInputDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            out GridConfig grid);

        internal delegate bool TryGetGridCellDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2 screenPosition,
            GridConfig grid,
            out Vector2Int cell);

        internal delegate void UpdatePlacementDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            Vector2 screenPosition);

        internal delegate bool TryAlignGateToNearbyWallDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2Int origin,
            BuildingDefinition definition,
            out bool gateVertical);

        public BuildingPlacementCommandRequestCompositionSystemHelper.Context CreateCommandContext(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            GetCenterScreenPlacementOriginDelegate getCenterScreenPlacementOrigin,
            TryResolveInitialPlacementOriginDelegate tryResolveInitialPlacementOrigin,
            UpdatePlacementVisualDelegate updatePlacementVisual,
            FocusActivePlacementDelegate focusActivePlacement,
            ValidateActivePlacementForConfirmDelegate validateActivePlacementForConfirm,
            PlaceBuildingDelegate placeBuilding,
            TryGetGridForPlacementInputDelegate tryGetGridForPlacementInput,
            TryGetGridCellDelegate tryGetGridCell,
            UpdatePlacementDelegate updatePlacement,
            TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return source.BuildingPlacementContextCompositionSystemHelper.CreateCommandContext(
                CreateContextSource(
                    source,
                    interactionContext,
                    markerPropertyBlock,
                    getCenterScreenPlacementOrigin,
                    tryResolveInitialPlacementOrigin,
                    updatePlacementVisual,
                    focusActivePlacement,
                    validateActivePlacementForConfirm,
                    placeBuilding,
                    tryGetGridForPlacementInput,
                    tryGetGridCell,
                    updatePlacement,
                    tryAlignGateToNearbyWall,
                    createBuildingRuntimeContextSource,
                    createBuildingSelectionContext),
                source.BuildingPlacementStartupSystemHelper,
                source.BuildingDefinitionPrefabSystemHelper,
                source.BuildingPlacementSessionCompositionSystemHelper,
                Debug.LogWarning,
                null,
                source.BuildingGameplayDependencyCompositionSystemHelper.NotifyStaticMinimapChanged,
                _ => source.BuildingSelectionRuntimeCompositionSystemHelper.ClearSelectedBuilding(createBuildingSelectionContext(source)),
                source.BuildingGameplayDependencyCompositionSystemHelper.ClearCommandMode);
        }

        public BuildingPlacementContextCompositionSystemHelper.Source CreateContextSource(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            GetCenterScreenPlacementOriginDelegate getCenterScreenPlacementOrigin,
            TryResolveInitialPlacementOriginDelegate tryResolveInitialPlacementOrigin,
            UpdatePlacementVisualDelegate updatePlacementVisual,
            FocusActivePlacementDelegate focusActivePlacement,
            ValidateActivePlacementForConfirmDelegate validateActivePlacementForConfirm,
            PlaceBuildingDelegate placeBuilding,
            TryGetGridForPlacementInputDelegate tryGetGridForPlacementInput,
            TryGetGridCellDelegate tryGetGridCell,
            UpdatePlacementDelegate updatePlacement,
            TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return new BuildingPlacementContextCompositionSystemHelper.Source(
                source.RuntimeGameplayStateSystem,
                source.BuildingPlacementLifecycleCompositionSystemHelper,
                source.BuildingPlacementInputUiSystemHelper,
                source.BuildingPlacementPreviewPresentationSystemHelper,
                source.BuildingPlacementValidationUtilitySystemHelper,
                source.RuntimeBuildingSystem,
                source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                source.BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance,
                source.BuildingPlacementVisualPresentationSystemHelper.ReleaseBuildingVisualInstance,
                footprint => getCenterScreenPlacementOrigin(source, footprint),
                (BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin) => tryResolveInitialPlacementOrigin(source, interactionContext, markerPropertyBlock, definition, preferredOrigin, out resolvedOrigin),
                (placement, updateCellFromPointer, screenPosition) => updatePlacementVisual(source, interactionContext, markerPropertyBlock, placement, updateCellFromPointer, screenPosition),
                placement => focusActivePlacement(source, interactionContext, markerPropertyBlock, placement),
                placement => validateActivePlacementForConfirm(source, interactionContext, markerPropertyBlock, placement),
                source.BuildingConstructionResourceTransactionSystemHelper.TryReserve,
                source.BuildingConstructionResourceTransactionSystemHelper.TryFinalize,
                source.BuildingConstructionResourceTransactionSystemHelper.TryRollback,
                placement => placeBuilding(source, interactionContext, markerPropertyBlock, placement),
                source.BuildingGameplayDependencyCompositionSystemHelper.ApplyBuildCommandMode,
                () => source.BuildingSelectionRuntimeCompositionSystemHelper.ClearSelectedBuilding(createBuildingSelectionContext(source)),
                (out GridConfig grid) => tryGetGridForPlacementInput(source, out grid),
                (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
                source.BuildingGameplayDependencyCompositionSystemHelper.IsPointerOverPlacementUi,
                screenPosition => updatePlacement(source, interactionContext, markerPropertyBlock, screenPosition),
                source.BuildingGameplayDependencyCompositionSystemHelper.IsRuntimeBlockerCell,
                (grid, origin, footprint) => source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasRoadInFootprint(source.BuildingPlacementStartupSystemHelper, grid, origin, footprint),
                source.BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance,
                (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualPresentationSystemHelper.PositionBuildingObject(
                    instance,
                    originCell,
                    definition,
                    grid,
                    rotateVertical,
                    source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint,
                    (origin, footprint, gridConfig) => source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
                    (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => tryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
                (definition, instance, originCell, removeOverlappingBlockers) =>
                {
                    BuildingRuntimeContextFactoryCompositionSystemHelper.Source runtimeContextSource =
                        createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock);
                    RuntimeBuildingEntity building = source.BuildingRuntimeCreationCompositionSystemHelper.RegisterRuntimeBuilding(
                        source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCreationContext(runtimeContextSource),
                        definition,
                        instance,
                        originCell,
                        removeOverlappingBlockers);
                    if (building != null)
                    {
                        source.BuildingRuntimeOwnershipCompositionSystemHelper.SetRuntimeBuildingOwnerFaction(
                            source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateOwnershipContext(runtimeContextSource),
                            building,
                            FactionIdentity.PlayerFactionId);
                    }

                    return building;
                },
                building => source.BuildingRuntimeCreationCompositionSystemHelper.RollbackRuntimeBuildingRegistration(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCreationContext(
                        createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                    building),
                BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint,
                source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint,
                source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
        }
    }
}
