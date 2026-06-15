using Unity.Entities;
using UnityEngine;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed partial class BuildingPlacementVisualCompositionSystem : SystemBase
{
    internal delegate bool TryGetGridCellDelegate(
        BuildingGameplayCompositionSourceSystem source,
        Vector2 screenPosition,
        GridConfig grid,
        out Vector2Int cell);

    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate bool IsActivePlacementValidDelegate(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData);

    internal delegate bool TryAlignGateToNearbyWallDelegate(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int origin,
        BuildingDefinition definition,
        out bool gateVertical);

    internal delegate BuildingPlacementContextSystem.Source CreatePlacementContextSourceDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock);

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void UpdatePlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Vector2 screenPosition,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateSystem == null)
            return;

        source.BuildingPlacementVisualUpdateSystem.UpdatePlacement(
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
            source.BuildingPlacementLifecycleSystem.ActivePlacement,
            screenPosition);
    }

    public BuildingPlacementVisualUpdateSystem.Context CreateUpdateContext(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
            (Vector2 screenPosition, GridConfig grid, out Vector2Int cell) => tryGetGridCell(source, screenPosition, grid, out cell),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                tryGetGridData(source, out gridEntity, out grid, out roads, out blockerData),
            source.BuildingPlacementGridSystem.GetPlacementFootprint,
            (origin, footprint, grid, roads, blockerData) => isActivePlacementValid(source, origin, footprint, grid, roads, blockerData),
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
                (Vector2Int origin, BuildingDefinition definition, out bool gateVertical) => tryAlignGateToNearbyWall(source, origin, definition, out gateVertical)),
            () => createPlacementContextSource(source, interactionContext, markerPropertyBlock),
            () => source.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(source)),
            building => source.BuildingSelectionSystem.SelectAndFocusBuilding(createBuildingSelectionContext(source), building));
    }

    public void FocusActivePlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateSystem == null)
            return;

        source.BuildingPlacementVisualUpdateSystem.FocusActivePlacement(
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
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateSystem == null)
            return false;

        return source.BuildingPlacementVisualUpdateSystem.ValidateActivePlacementForConfirm(
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
        BuildingGameplayCompositionSourceSystem source,
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
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateSystem == null)
            return;

        source.BuildingPlacementVisualUpdateSystem.UpdatePlacementVisual(
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
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        PlacementState placement,
        TryGetGridCellDelegate tryGetGridCell,
        TryGetGridDataDelegate tryGetGridData,
        IsActivePlacementValidDelegate isActivePlacementValid,
        TryAlignGateToNearbyWallDelegate tryAlignGateToNearbyWall,
        CreatePlacementContextSourceDelegate createPlacementContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        if (source?.BuildingPlacementVisualUpdateSystem == null)
            return;

        source.BuildingPlacementVisualUpdateSystem.PlaceBuilding(
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
