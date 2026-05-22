using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingPlacementValidationSystem
{
    public static bool IsFootprintInsideGrid(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
    {
        return originCell.x >= 0 &&
            originCell.y >= 0 &&
            originCell.x + footprintCells.x <= grid.Width &&
            originCell.y + footprintCells.y <= grid.Height;
    }

    public static bool HasCachedInvalidCellInFootprint(
        int[] invalidPrefix,
        int prefixWidth,
        int prefixHeight,
        Vector2Int originCell,
        Vector2Int footprintCells)
    {
        if (invalidPrefix == null || invalidPrefix.Length == 0)
            return false;

        int xMin = originCell.x;
        int yMin = originCell.y;
        int xMax = originCell.x + footprintCells.x;
        int yMax = originCell.y + footprintCells.y;
        if (xMin < 0 || yMin < 0 || xMax >= prefixWidth || yMax >= prefixHeight)
            return true;

        int topRight = invalidPrefix[yMax * prefixWidth + xMax];
        int topLeft = invalidPrefix[yMax * prefixWidth + xMin];
        int bottomRight = invalidPrefix[yMin * prefixWidth + xMax];
        int bottomLeft = invalidPrefix[yMin * prefixWidth + xMin];
        int blockedCount = topRight - topLeft - bottomRight + bottomLeft;
        return blockedCount > 0;
    }

    public static void RebuildInvalidPrefix(
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        bool[] roadFootprintMask,
        Func<int, int, int, int, bool> isRuntimeBlockerCell,
        ref int[] invalidPrefix,
        out int prefixWidth,
        out int prefixHeight,
        out bool hasInvalidPrefix)
    {
        int width = grid.Width;
        int height = grid.Height;
        prefixWidth = width + 1;
        prefixHeight = height + 1;
        int totalLength = prefixWidth * prefixHeight;
        if (invalidPrefix == null || invalidPrefix.Length != totalLength)
            invalidPrefix = new int[totalLength];
        else
            Array.Clear(invalidPrefix, 0, invalidPrefix.Length);

        for (int y = 0; y < height; y++)
        {
            int rowPrefix = 0;
            int rowBase = (y + 1) * prefixWidth;
            int prevRowBase = y * prefixWidth;
            for (int x = 0; x < width; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), width);
                bool blockedByRoad = roads[index].Value != 0 ||
                    roadFootprintMask != null && roadFootprintMask[index];
                bool blockedByStaticBlocker =
                    blockerData.Blocked.IsCreated &&
                    blockerData.Blocked.IsSet(index) &&
                    !IsRuntimeBlockerCell(isRuntimeBlockerCell, x, y, width, height);
                if (blockedByRoad || blockedByStaticBlocker)
                    rowPrefix++;

                invalidPrefix[rowBase + x + 1] = invalidPrefix[prevRowBase + x + 1] + rowPrefix;
            }
        }

        hasInvalidPrefix = true;
    }

    public static bool IsPlacementRectValid(
        RectInt placementRect,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        bool hasInvalidPrefix,
        int[] invalidPrefix,
        int prefixWidth,
        int prefixHeight,
        Func<int, int, int, int, bool> isRuntimeBlockerCell,
        Func<GridConfig, Vector2Int, Vector2Int, bool> hasRoadInFootprint,
        Func<RectInt, bool> overlapsRuntimeBuilding)
    {
        if (!IsFootprintInsideGrid(placementRect.position, placementRect.size, grid))
            return false;

        if (overlapsRuntimeBuilding != null && overlapsRuntimeBuilding(placementRect))
            return false;

        if (hasInvalidPrefix)
            return !HasCachedInvalidCellInFootprint(invalidPrefix, prefixWidth, prefixHeight, placementRect.position, placementRect.size);

        if (HasBlockedCell(placementRect.position, placementRect.size, grid, roads, blockerData, isRuntimeBlockerCell))
            return false;

        if (hasRoadInFootprint != null && hasRoadInFootprint(grid, placementRect.position, placementRect.size))
            return false;

        return true;
    }

    public static bool IsWallFootprintValid(
        Vector2Int originCell,
        Vector2Int footprintCells,
        bool vertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        bool allowExistingWallOverlap,
        Func<int, int, int, int, bool> isRuntimeBlockerCell,
        Func<int, int, bool> isPerpendicularWallOverlapCell,
        Func<int, int, bool> isLinearWallOverlapCell,
        Func<GridConfig, Vector2Int, Vector2Int, bool> hasRoadInFootprint)
    {
        if (!IsFootprintInsideGrid(originCell, footprintCells, grid))
            return false;

        for (int y = originCell.y; y < originCell.y + footprintCells.y; y++)
        {
            for (int x = originCell.x; x < originCell.x + footprintCells.x; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                if (roads[index].Value != 0)
                    return false;

                bool blockedByStaticBlocker =
                    blockerData.Blocked.IsCreated &&
                    blockerData.Blocked.IsSet(index) &&
                    !IsRuntimeBlockerCell(isRuntimeBlockerCell, x, y, grid.Width, grid.Height);
                if (!blockedByStaticBlocker)
                    continue;

                bool perpendicularWallOverlap = isPerpendicularWallOverlapCell != null && isPerpendicularWallOverlapCell(x, y);
                bool allowedLinearWallOverlap = allowExistingWallOverlap &&
                    isLinearWallOverlapCell != null &&
                    isLinearWallOverlapCell(x, y);
                if (!perpendicularWallOverlap && !allowedLinearWallOverlap)
                    return false;
            }
        }

        if (hasRoadInFootprint != null && hasRoadInFootprint(grid, originCell, footprintCells))
            return false;

        return true;
    }

    public static bool DoWallSegmentsConflict(
        Vector2Int originA,
        Vector2Int footprintA,
        bool verticalA,
        Vector2Int originB,
        Vector2Int footprintB,
        bool verticalB)
    {
        bool overlaps =
            originA.x < originB.x + footprintB.x &&
            originA.x + footprintA.x > originB.x &&
            originA.y < originB.y + footprintB.y &&
            originA.y + footprintA.y > originB.y;

        if (!overlaps)
            return false;

        return verticalA == verticalB;
    }

    private static bool HasBlockedCell(
        Vector2Int originCell,
        Vector2Int footprintCells,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerData blockerData,
        Func<int, int, int, int, bool> isRuntimeBlockerCell)
    {
        for (int y = originCell.y; y < originCell.y + footprintCells.y; y++)
        {
            for (int x = originCell.x; x < originCell.x + footprintCells.x; x++)
            {
                int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                if (roads[index].Value != 0)
                    return true;
                if (blockerData.Blocked.IsCreated &&
                    blockerData.Blocked.IsSet(index) &&
                    !IsRuntimeBlockerCell(isRuntimeBlockerCell, x, y, grid.Width, grid.Height))
                    return true;
            }
        }

        return false;
    }

    private static bool IsRuntimeBlockerCell(
        Func<int, int, int, int, bool> isRuntimeBlockerCell,
        int x,
        int y,
        int width,
        int height)
    {
        return isRuntimeBlockerCell != null && isRuntimeBlockerCell(x, y, width, height);
    }
}
