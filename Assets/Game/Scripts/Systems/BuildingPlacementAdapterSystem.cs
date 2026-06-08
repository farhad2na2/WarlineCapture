using Unity.Entities;
using UnityEngine;
using PlacementState = BuildingPlacementLifecycleSystem.PlacementState;

internal sealed class BuildingPlacementAdapterSystem
{
    internal delegate bool TryGetGridDataDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    internal delegate BuildingRuntimeContextSystem.Source CreateBuildingRuntimeContextSourceDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock);

    internal delegate BuildingRuntimeContextSystem.RuntimeSource CreateRuntimeContextSourceDelegate(
        BuildingGameplayCompositionSourceSystem source);

    internal delegate RectInt GetEffectivePlacementRectDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical);

    internal delegate bool OverlapsAnyRuntimeBuildingDelegate(
        BuildingGameplayCompositionSourceSystem source,
        RectInt candidateRect);

    internal delegate bool IsPlacementValidDelegate(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData);

    public bool TryResolveInitialPlacementOrigin(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        CreateBuildingRuntimeContextSourceDelegate createBuildingRuntimeContextSource,
        out Vector2Int resolvedOrigin)
    {
        BuildingRuntimeSpawnCommandSystem.Context context = source.BuildingRuntimeContextSystem.CreateSpawnCommandContext(
            createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock),
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

    public Vector2Int GetCenterScreenPlacementOrigin(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int footprintCells,
        TryGetGridDataDelegate tryGetGridData)
    {
        if (!tryGetGridData(source, out _, out GridConfig grid, out _, out _))
            return Vector2Int.zero;

        return source.BuildingPlacementGridSystem.GetCenterScreenPlacementOrigin(
            footprintCells,
            grid,
            source.BuildingPlacementStartupSystem.WorldCamera,
            source.BuildingPlacementStartupSystem.BuildPlaneY,
            new Vector2(Screen.width, Screen.height));
    }

    public bool IsActivePlacementValid(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        CreateRuntimeContextSourceDelegate createRuntimeContextSource,
        IsPlacementValidDelegate isPlacementValid)
    {
        PlacementState activePlacement = source.BuildingPlacementLifecycleSystem.ActivePlacement;
        bool rotateVertical = source.BuildingBarrierSystem.ResolvePlacementRotateVertical(
            source.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(source)),
            source.BuildingPlacementInputSystem,
            activePlacement);
        return isPlacementValid(source, activePlacement?.Definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData);
    }

    public bool TryAlignGateToNearbyWall(
        BuildingGameplayCompositionSourceSystem source,
        Vector2Int originCell,
        BuildingDefinition definition,
        CreateRuntimeContextSourceDelegate createRuntimeContextSource,
        out bool gateVertical)
    {
        return source.BuildingBarrierSystem.ShouldAlignGateToNearbyWall(
            source.BuildingRuntimeContextSystem.CreateBarrierContext(createRuntimeContextSource(source)),
            originCell,
            definition,
            out gateVertical);
    }

    public bool IsPlacementValid(
        BuildingGameplayCompositionSourceSystem source,
        BuildingDefinition definition,
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding)
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
                getEffectivePlacementRect(source, candidateDefinition, candidateOrigin, candidateGrid, candidateRotateVertical),
            candidateRect => overlapsAnyRuntimeBuilding(source, candidateRect));
    }
}
