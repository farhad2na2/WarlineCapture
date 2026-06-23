using Unity.Entities;
using UnityEngine;

internal sealed class BuildingPlacementInvalidCellSystem
{
    internal delegate RectInt GetEffectivePlacementRectDelegate(
        BuildingDefinition definition,
        Vector2Int originCell,
        GridConfig grid,
        bool rotateVertical);

    private int[] _placementInvalidPrefix;
    private bool _hasPlacementInvalidPrefix;
    private int _placementInvalidPrefixWidth;
    private int _placementInvalidPrefixHeight;

    internal void Clear()
    {
        _hasPlacementInvalidPrefix = false;
    }

    internal void RebuildPlacementInvalidPrefix(
        BuildingGameplayGridDataSystem gridDataSystem,
        BuildingGameplayEcsQuerySystem ecsQuerySystem,
        BuildingGameplayGridDataSystem.TryGetEntityManagerDelegate tryGetEntityManager,
        BuildingPlacementStartupSystem startupSystem,
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem)
    {
        _hasPlacementInvalidPrefix = false;
        if (!gridDataSystem.TryGetGridData(ecsQuerySystem, tryGetEntityManager, out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            return;

        bool[] roadFootprintMask = new bool[grid.Width * grid.Height];
        startupSystem.FillRoadFootprintMask(grid, roadFootprintMask);

        BuildingPlacementValidationSystem.RebuildInvalidPrefix(
            grid,
            roads,
            blockerData,
            roadFootprintMask,
            (x, y, width, height) => IsRuntimeBlockerCell(dependencySystem, x, y, width, height),
            ref _placementInvalidPrefix,
            out _placementInvalidPrefixWidth,
            out _placementInvalidPrefixHeight,
            out _hasPlacementInvalidPrefix);
    }

    internal bool IsPlacementValid(
        BuildingDefinition definition,
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        BuildingPlacementStartupSystem startupSystem,
        GetEffectivePlacementRectDelegate getEffectivePlacementRect,
        System.Func<RectInt, bool> overlapsRuntimeBuilding)
    {
        RectInt placementRect = definition != null && getEffectivePlacementRect != null
            ? getEffectivePlacementRect(definition, originCell, grid, rotateVertical)
            : new RectInt(originCell, footprintCells);

        return BuildingPlacementValidationSystem.IsPlacementRectValid(
            placementRect,
            grid,
            roads,
            blockerData,
            _hasPlacementInvalidPrefix,
            _placementInvalidPrefix,
            _placementInvalidPrefixWidth,
            _placementInvalidPrefixHeight,
            (x, y, width, height) => IsRuntimeBlockerCell(dependencySystem, x, y, width, height),
            (queryGrid, queryOrigin, queryFootprint) => HasRoadInFootprint(startupSystem, queryGrid, queryOrigin, queryFootprint),
            overlapsRuntimeBuilding);
    }

    internal bool HasCachedInvalidCellInFootprint(Vector2Int originCell, Vector2Int footprintCells)
    {
        if (!_hasPlacementInvalidPrefix)
            return false;

        return BuildingPlacementValidationSystem.HasCachedInvalidCellInFootprint(
            _placementInvalidPrefix,
            _placementInvalidPrefixWidth,
            _placementInvalidPrefixHeight,
            originCell,
            footprintCells);
    }

    internal bool HasRoadInFootprint(
        BuildingPlacementStartupSystem startupSystem,
        GridConfig grid,
        Vector2Int originCell,
        Vector2Int footprintCells)
    {
        return startupSystem.HasRoadInFootprint(grid, originCell, footprintCells);
    }

    internal bool IsRuntimeBlockerCell(
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        int x,
        int y,
        int width,
        int height)
    {
        return dependencySystem.IsRuntimeBlockerCell(x, y, width, height);
    }
}
