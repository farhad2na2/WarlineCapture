using UnityEngine;
using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

internal sealed class BuildingPlacementCommandCompositionSystemHelper
{
    internal delegate Vector2Int GetCenterScreenPlacementOriginDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int footprintCells);

    internal delegate bool TryResolveInitialPlacementOriginDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out Vector2Int resolvedOrigin);

    internal delegate void UpdatePlacementVisualDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition);

    internal delegate void FocusActivePlacementDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement);

    internal delegate bool ValidateActivePlacementForConfirmDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement);

    internal delegate void PlaceBuildingDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
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
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition);

    internal delegate bool TryAlignGateToNearbyWallDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int origin,
        BuildingDefinition definition,
        out bool gateVertical);

    public BuildingPlacementCommandRequestCompositionSystemHelper.Context CreateCommandContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
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
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
            GameRuntimeStats.RecordBuildingBuilt,
            source.BuildingGameplayDependencyCompositionSystemHelper.NotifyStaticMinimapChanged,
            _ => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
            source.BuildingGameplayDependencyCompositionSystemHelper.ClearCommandMode);
    }

    public BuildingPlacementContextCompositionSystemHelper.Source CreateContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
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
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
            preview => source.RuntimeObjectPresentationHelper.DestroyRuntimeObject(preview),
            footprint => getCenterScreenPlacementOrigin(source, footprint),
            (BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin) => tryResolveInitialPlacementOrigin(source, interactionContext, markerPropertyBlock, definition, preferredOrigin, out resolvedOrigin),
            (placement, updateCellFromPointer, screenPosition) => updatePlacementVisual(source, interactionContext, markerPropertyBlock, placement, updateCellFromPointer, screenPosition),
            placement => focusActivePlacement(source, interactionContext, markerPropertyBlock, placement),
            placement => validateActivePlacementForConfirm(source, interactionContext, markerPropertyBlock, placement),
            source.RuntimeResourceSystem.TrySpendDollars,
            placement => placeBuilding(source, interactionContext, markerPropertyBlock, placement),
            source.BuildingGameplayDependencyCompositionSystemHelper.ApplyBuildCommandMode,
            () => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
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
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationCompositionSystemHelper.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateCreationContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                definition,
                instance,
                originCell,
                removeOverlappingBlockers),
            BuildingRuntimeSpawnSystem.CloneDefinitionWithFootprint,
            source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint,
            source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
    }
}
