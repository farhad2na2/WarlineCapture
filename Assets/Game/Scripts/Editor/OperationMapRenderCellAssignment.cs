using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Mathematics;

namespace Game.Editor
{
    internal readonly struct OperationMapRenderCellRange
    {
        internal OperationMapRenderCellRange(int first, int count)
        {
            First = first;
            Count = count;
        }

        internal int First { get; }
        internal int Count { get; }
    }

    internal static class OperationMapRenderCellAssignment
    {
        internal static bool TryAssign(
            in OperationMapRenderBoundsBlob worldBounds,
            float cellSize,
            float3 gridOrigin,
            int2 gridDimensions,
            out int[] cellIndices,
            out string error)
        {
            cellIndices = Array.Empty<int>();
            if (!IsFinite(worldBounds.Center) ||
                !IsFinite(worldBounds.Extents) ||
                math.any(worldBounds.Extents < 0f))
            {
                error = "Render bounds must be finite with nonnegative extents.";
                return false;
            }

            if (!math.isfinite(cellSize) ||
                cellSize <= 0f ||
                !IsFinite(gridOrigin) ||
                math.any(gridDimensions <= 0))
            {
                error = "Cell grid requires finite origin, positive cell size, and positive dimensions.";
                return false;
            }

            float3 minimum = worldBounds.Center - worldBounds.Extents;
            float3 maximum = worldBounds.Center + worldBounds.Extents;
            if (!TryResolveAxis(
                    minimum.x,
                    maximum.x,
                    gridOrigin.x,
                    cellSize,
                    gridDimensions.x,
                    out int minX,
                    out int maxX) ||
                !TryResolveAxis(
                    minimum.z,
                    maximum.z,
                    gridOrigin.z,
                    cellSize,
                    gridDimensions.y,
                    out int minZ,
                    out int maxZ))
            {
                error = "Render bounds do not intersect the operation-map cell grid.";
                return false;
            }

            int width = maxX - minX + 1;
            int height = maxZ - minZ + 1;
            cellIndices = new int[checked(width * height)];
            int writeIndex = 0;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                    cellIndices[writeIndex++] = checked(z * gridDimensions.x + x);
            }

            error = null;
            return true;
        }

        internal static bool TryGatherUnique(
            IReadOnlyList<int> cellPlacementIndices,
            IReadOnlyList<OperationMapRenderCellRange> selectedCellRanges,
            int placementCount,
            out int[] uniquePlacementIndices,
            out string error)
        {
            uniquePlacementIndices = Array.Empty<int>();
            if (cellPlacementIndices == null || selectedCellRanges == null || placementCount < 0)
            {
                error = "Cell-placement input and ranges are required with a nonnegative placement count.";
                return false;
            }

            bool[] seen = new bool[placementCount];
            List<int> unique = new();
            for (int rangeIndex = 0; rangeIndex < selectedCellRanges.Count; rangeIndex++)
            {
                OperationMapRenderCellRange range = selectedCellRanges[rangeIndex];
                if (range.First < 0 ||
                    range.Count < 0 ||
                    range.First > cellPlacementIndices.Count ||
                    range.Count > cellPlacementIndices.Count - range.First)
                {
                    error = $"Selected cell range {rangeIndex} is outside the placement-index array.";
                    return false;
                }

                int end = range.First + range.Count;
                for (int index = range.First; index < end; index++)
                {
                    int placementIndex = cellPlacementIndices[index];
                    if (placementIndex < 0 || placementIndex >= placementCount)
                    {
                        error =
                            $"Cell placement index {placementIndex} is outside [0,{placementCount}).";
                        return false;
                    }

                    if (seen[placementIndex])
                        continue;

                    seen[placementIndex] = true;
                    unique.Add(placementIndex);
                }
            }

            unique.Sort();
            uniquePlacementIndices = unique.ToArray();
            error = null;
            return true;
        }

        private static bool TryResolveAxis(
            float minimum,
            float maximum,
            float origin,
            float cellSize,
            int dimension,
            out int minimumCell,
            out int maximumCell)
        {
            minimumCell = 0;
            maximumCell = -1;
            float gridMaximum = origin + dimension * cellSize;
            bool isPoint = minimum == maximum;
            if (isPoint)
            {
                if (minimum < origin || minimum >= gridMaximum)
                    return false;

                minimumCell = maximumCell =
                    math.clamp((int)math.floor((minimum - origin) / cellSize), 0, dimension - 1);
                return true;
            }

            if (maximum <= origin || minimum >= gridMaximum)
                return false;

            float clampedMinimum = math.max(minimum, origin);
            float clampedMaximum = math.min(maximum, gridMaximum);
            minimumCell =
                math.clamp((int)math.floor((clampedMinimum - origin) / cellSize), 0, dimension - 1);
            maximumCell =
                math.clamp((int)math.ceil((clampedMaximum - origin) / cellSize) - 1, 0, dimension - 1);
            return minimumCell <= maximumCell;
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }
    }
}
