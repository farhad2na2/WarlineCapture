using UnityEngine;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class BuildingPlacementCommandCompositionSystemHelper
{
    internal delegate Vector2Int GetCenterScreenPlacementOriginDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int footprintCells);

    internal delegate bool TryResolveInitialPlacementOriginDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out Vector2Int resolvedOrigin);

    internal delegate void UpdatePlacementVisualDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition);

    internal delegate void FocusActivePlacementDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement);

    internal delegate bool ValidateActivePlacementForConfirmDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement);

    internal delegate void PlaceBuildingDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
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
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition);

    internal delegate bool TryAlignGateToNearbyWallDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int origin,
        BuildingDefinition definition,
        out bool gateVertical);

    public BuildingPlacementCommandSystem.Context CreateCommandContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
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
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingPlacementContextSystem.CreateCommandContext(
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
            source.BuildingPlacementStartupSystem,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementSessionSystem,
            Debug.LogWarning,
            GameRuntimeStats.RecordBuildingBuilt,
            source.BuildingGameplayDependencySystem.NotifyStaticMinimapChanged,
            _ => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
            source.BuildingGameplayDependencySystem.ClearCommandMode);
    }

    public BuildingPlacementContextSystem.Source CreateContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
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
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingRuntimeContextSystem.Source> createBuildingRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
            preview => source.RuntimeObjectPresentationHelper.DestroyRuntimeObject(preview),
            footprint => getCenterScreenPlacementOrigin(source, footprint),
            (BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin) => tryResolveInitialPlacementOrigin(source, interactionContext, markerPropertyBlock, definition, preferredOrigin, out resolvedOrigin),
            (placement, updateCellFromPointer, screenPosition) => updatePlacementVisual(source, interactionContext, markerPropertyBlock, placement, updateCellFromPointer, screenPosition),
            placement => focusActivePlacement(source, interactionContext, markerPropertyBlock, placement),
            placement => validateActivePlacementForConfirm(source, interactionContext, markerPropertyBlock, placement),
            source.RuntimeResourceSystem.TrySpendDollars,
            placement => placeBuilding(source, interactionContext, markerPropertyBlock, placement),
            source.BuildingGameplayDependencySystem.ApplyBuildCommandMode,
            () => source.BuildingSelectionSystem.ClearSelectedBuilding(createBuildingSelectionContext(source)),
            (out GridConfig grid) => tryGetGridForPlacementInput(source, out grid),
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            source.BuildingGameplayDependencySystem.IsPointerOverPlacementUi,
            screenPosition => updatePlacement(source, interactionContext, markerPropertyBlock, screenPosition),
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
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => tryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            (definition, instance, originCell, removeOverlappingBlockers) => source.BuildingRuntimeCreationSystem.RegisterRuntimeBuilding(
                source.BuildingRuntimeContextSystem.CreateCreationContext(createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock)),
                definition,
                instance,
                originCell,
                removeOverlappingBlockers),
            BuildingRuntimeSpawnSystem.CloneDefinitionWithFootprint,
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            source.RuntimeObjectPresentationHelper.DestroyRuntimeObject);
    }
}
