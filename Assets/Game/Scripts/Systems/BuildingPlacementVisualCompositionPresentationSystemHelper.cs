using Unity.Entities;
using UnityEngine;
using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

internal sealed class BuildingPlacementVisualCompositionPresentationSystemHelper
{
    internal delegate bool TryGetGridCellDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell);

    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate bool IsActivePlacementValidDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData);

    internal delegate bool TryAlignGateToNearbyWallDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        Vector2Int origin,
        BuildingDefinition definition,
        out bool gateVertical);

    internal delegate BuildingPlacementContextCompositionSystemHelper.Source CreatePlacementContextSourceDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock);

    public void UpdatePlacement(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateCompositionSystemHelper == null)
            return;

        source.BuildingPlacementVisualUpdateCompositionSystemHelper.UpdatePlacement(
            CreateUpdateContext(
                source,
                interactionContext,
                markerPropertyBlock,
                tryGetGridCell,
                tryGetGridData,
                isActivePlacementValid,
                tryAlignGateToNearbyWall,
                createPlacementContextSource,
                createRuntimeContextSource,
                createBuildingSelectionContext),
            source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement,
            screenPosition);
    }

    public BuildingPlacementVisualUpdateCompositionSystemHelper.Context CreateUpdateContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return new BuildingPlacementVisualUpdateCompositionSystemHelper.Context(
            source.BuildingPlacementInputSystem,
            source.BuildingPlacementPreviewPresentationSystemHelper,
            source.BuildingPlacementValidationSystem,
            source.BuildingPlacementGridSystem,
            source.BuildingPlacementStartupSystemHelper,
            source.BuildingGameplayDependencyCompositionSystemHelper,
            source.BuildingPlacementContextCompositionSystemHelper,
            source.BuildingPlacementCommitSystem,
            source.BuildingPlacementLifecycleCompositionSystemHelper,
            source.BuildingBarrierUtilitySystemHelper,
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            (origin, footprint, grid, roads, blockerData) => isActivePlacementValid(source, origin, footprint, grid, roads, blockerData),
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
            source.BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance,
            (instance, originCell, definition, grid, rotateVertical) => source.BuildingPlacementVisualPresentationSystemHelper.PositionBuildingObject(
                instance,
                originCell,
                definition,
                grid,
                rotateVertical,
                source.BuildingPlacementGridSystem.GetPlacementFootprint,
                (origin, footprint, gridConfig) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, gridConfig, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => tryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            () => createPlacementContextSource(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(source)),
            building => source.BuildingSelectionSystem.SelectAndFocusBuilding(createBuildingSelectionContext(source), building));
    }

    public void FocusActivePlacement(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateCompositionSystemHelper == null)
            return;

        source.BuildingPlacementVisualUpdateCompositionSystemHelper.FocusActivePlacement(
            CreateUpdateContext(
                source,
                interactionContext,
                markerPropertyBlock,
                tryGetGridCell,
                tryGetGridData,
                isActivePlacementValid,
                tryAlignGateToNearbyWall,
                createPlacementContextSource,
                createRuntimeContextSource,
                createBuildingSelectionContext),
            placement);
    }

    public bool ValidateActivePlacementForConfirm(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateCompositionSystemHelper == null)
            return false;

        return source.BuildingPlacementVisualUpdateCompositionSystemHelper.ValidateActivePlacementForConfirm(
            CreateUpdateContext(
                source,
                interactionContext,
                markerPropertyBlock,
                tryGetGridCell,
                tryGetGridData,
                isActivePlacementValid,
                tryAlignGateToNearbyWall,
                createPlacementContextSource,
                createRuntimeContextSource,
                createBuildingSelectionContext),
            placement);
    }

    public void UpdatePlacementVisual(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        bool updateCellFromPointer,
        Vector2 screenPosition,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateCompositionSystemHelper == null)
            return;

        source.BuildingPlacementVisualUpdateCompositionSystemHelper.UpdatePlacementVisual(
            CreateUpdateContext(
                source,
                interactionContext,
                markerPropertyBlock,
                tryGetGridCell,
                tryGetGridData,
                isActivePlacementValid,
                tryAlignGateToNearbyWall,
                createPlacementContextSource,
                createRuntimeContextSource,
                createBuildingSelectionContext),
            placement,
            updateCellFromPointer,
            screenPosition);
    }

    public void PlaceBuilding(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateCompositionSystemHelper == null)
            return;

        source.BuildingPlacementVisualUpdateCompositionSystemHelper.PlaceBuilding(
            CreateUpdateContext(
                source,
                interactionContext,
                markerPropertyBlock,
                tryGetGridCell,
                tryGetGridData,
                isActivePlacementValid,
                tryAlignGateToNearbyWall,
                createPlacementContextSource,
                createRuntimeContextSource,
                createBuildingSelectionContext),
            placement);
    }
}
