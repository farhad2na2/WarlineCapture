using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class BuildingPlacementValidationUtilitySystemHelper
    {
        public static bool IsPlacementRectValid(
            RectInt placementRect,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            bool hasInvalidPrefix,
            int[] invalidPrefix,
            int prefixWidth,
            int prefixHeight,
            Func<int, int, int, int, bool> isRuntimeBlockerCell,
            Func<GridConfig, Vector2Int, Vector2Int, bool> hasRoadInFootprint,
            Func<RectInt, bool> overlapsRuntimeBuilding,
            bool allowRoadOverlap = false)
        {
            var origin = placementRect.position;
            var size = placementRect.size;
            if (!IsFootprintInsideGrid(origin, size, grid))
                return false;

            // Check terrain first.
            if (hasInvalidPrefix && !allowRoadOverlap)
            {
                if (HasCachedInvalidCellInFootprint(invalidPrefix, prefixWidth, prefixHeight, origin, size))
                    return false;
            }
            else if (HasBlockedCell(origin, size, grid, roads, blockerData, isRuntimeBlockerCell, allowRoadOverlap) ||
                     !allowRoadOverlap && hasRoadInFootprint != null && hasRoadInFootprint(grid, origin, size))
                return false;

            return overlapsRuntimeBuilding == null || !overlapsRuntimeBuilding(placementRect);
        }

        private static bool HasBlockedCell(
            Vector2Int originCell,
            Vector2Int footprintCells,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            Func<int, int, int, int, bool> isRuntimeBlockerCell, bool allowRoadOverlap)
        {
            for (int y = originCell.y; y < originCell.y + footprintCells.y; y++)
            {
                for (int x = originCell.x; x < originCell.x + footprintCells.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    if (!allowRoadOverlap && roads[index].Value != 0)
                        return true;
                    if (blockerData.Blocked.IsCreated &&
                        blockerData.Blocked.IsSet(index) &&
                        !IsRuntimeBlockerCell(isRuntimeBlockerCell, x, y, grid.Width, grid.Height))
                        return true;
                }
            }

            return false;
        }
    }
}
