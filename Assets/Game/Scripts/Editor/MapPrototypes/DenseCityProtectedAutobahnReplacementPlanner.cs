using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityProtectedAutobahnLaneRange
    {
        internal DenseCityProtectedAutobahnLaneRange(int row, int minimumColumn, int maximumColumn)
        {
            Row = row;
            MinimumColumn = minimumColumn;
            MaximumColumn = maximumColumn;
        }

        internal int Row { get; }
        internal int MinimumColumn { get; }
        internal int MaximumColumn { get; }
    }

    internal sealed class DenseCityProtectedAutobahnRouteDescriptor
    {
        internal DenseCityProtectedAutobahnRouteDescriptor(
            Vector2 gridOrigin,
            IReadOnlyList<string> sourceGlobalObjectIds,
            IReadOnlyList<DenseCityProtectedAutobahnLaneRange> laneRanges,
            IReadOnlyList<Vector2Int> cells)
        {
            GridOrigin = gridOrigin;
            SourceGlobalObjectIds = Copy(sourceGlobalObjectIds);
            LaneRanges = Copy(laneRanges);
            Cells = Copy(cells);
        }

        internal Vector2 GridOrigin { get; }
        internal IReadOnlyList<string> SourceGlobalObjectIds { get; }
        internal IReadOnlyList<DenseCityProtectedAutobahnLaneRange> LaneRanges { get; }
        internal IReadOnlyList<Vector2Int> Cells { get; }

        internal Vector2 GetWorldPlacement(Vector2Int cell) =>
            GridOrigin + new Vector2(
                cell.x * DenseCityProtectedAutobahnReplacementPlanner.CellSize,
                cell.y * DenseCityProtectedAutobahnReplacementPlanner.CellSize);

        private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null)
                return Array.AsReadOnly(Array.Empty<T>());

            var copy = new T[values.Count];
            for (int index = 0; index < values.Count; index++)
                copy[index] = values[index];
            return Array.AsReadOnly(copy);
        }
    }

    internal static class DenseCityProtectedAutobahnReplacementPlanner
    {
        internal const float CellSize = 10f;
        internal const float LegacyMinimumWorldX = -1700f;
        internal const float LegacyMaximumWorldX = 3100f;
        internal const float LegacyMinimumWorldZ = 412.0962f;
        internal const float LegacyMaximumWorldZ = 437.4662f;
        internal const string AcceptedWestSourceGlobalObjectId =
            "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1327110974329158-1224302320877551806";
        internal const string AcceptedEastSourceGlobalObjectId =
            "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1327110974329158-1314812446050087256";

        internal static bool TryCreate(
            IReadOnlyList<string> sourceGlobalObjectIds,
            Vector2 gridOrigin,
            out DenseCityProtectedAutobahnRouteDescriptor descriptor,
            out string error)
        {
            descriptor = null;
            if (!TryValidateSourceIds(sourceGlobalObjectIds, out error))
                return false;
            if (!float.IsFinite(gridOrigin.x) || !float.IsFinite(gridOrigin.y))
            {
                error = "Dense-city road grid origin must be finite.";
                return false;
            }

            int minimumColumn = QuantizeBoundary(LegacyMinimumWorldX, gridOrigin.x);
            int maximumColumn = QuantizeBoundary(LegacyMaximumWorldX, gridOrigin.x);
            if (maximumColumn < minimumColumn)
            {
                error = "Protected Autobahn longitudinal range is empty after grid quantization.";
                return false;
            }

            int nearestRow = QuantizePlacement(
                (LegacyMinimumWorldZ + LegacyMaximumWorldZ) * 0.5f,
                gridOrigin.y);
            float nearestPlacement = WorldPlacement(nearestRow, gridOrigin.y);
            int adjacentRow = nearestPlacement <=
                              (LegacyMinimumWorldZ + LegacyMaximumWorldZ) * 0.5f
                ? nearestRow + 1
                : nearestRow - 1;
            int firstRow = Math.Min(nearestRow, adjacentRow);
            int secondRow = Math.Max(nearestRow, adjacentRow);

            var sourceIds = new[]
            {
                AcceptedWestSourceGlobalObjectId,
                AcceptedEastSourceGlobalObjectId
            };
            var ranges = new[]
            {
                new DenseCityProtectedAutobahnLaneRange(firstRow, minimumColumn, maximumColumn),
                new DenseCityProtectedAutobahnLaneRange(secondRow, minimumColumn, maximumColumn)
            };
            int laneLength = maximumColumn - minimumColumn + 1;
            var cells = new Vector2Int[laneLength * ranges.Length];
            int cellIndex = 0;
            for (int rangeIndex = 0; rangeIndex < ranges.Length; rangeIndex++)
            {
                DenseCityProtectedAutobahnLaneRange range = ranges[rangeIndex];
                for (int column = range.MinimumColumn; column <= range.MaximumColumn; column++)
                    cells[cellIndex++] = new Vector2Int(column, range.Row);
            }

            var candidate = new DenseCityProtectedAutobahnRouteDescriptor(
                gridOrigin,
                sourceIds,
                ranges,
                cells);
            if (!TryValidate(candidate, out error))
                return false;

            descriptor = candidate;
            return true;
        }

        internal static bool TryValidate(
            DenseCityProtectedAutobahnRouteDescriptor descriptor,
            out string error)
        {
            if (descriptor == null)
            {
                error = "Protected Autobahn route descriptor is required.";
                return false;
            }
            if (!TryValidateSourceIds(descriptor.SourceGlobalObjectIds, out error))
                return false;
            if (!float.IsFinite(descriptor.GridOrigin.x) || !float.IsFinite(descriptor.GridOrigin.y))
            {
                error = "Dense-city road grid origin must be finite.";
                return false;
            }
            if (descriptor.LaneRanges.Count != 2)
            {
                error = "Protected Autobahn replacement requires exactly two lane ranges.";
                return false;
            }

            DenseCityProtectedAutobahnLaneRange first = descriptor.LaneRanges[0];
            DenseCityProtectedAutobahnLaneRange second = descriptor.LaneRanges[1];
            if (second.Row - first.Row != 1)
            {
                error = "Protected Autobahn lane rows must be sorted and adjacent.";
                return false;
            }
            if (first.MinimumColumn > first.MaximumColumn ||
                second.MinimumColumn > second.MaximumColumn)
            {
                error = "Protected Autobahn lane ranges must be non-empty.";
                return false;
            }
            if (first.MinimumColumn != second.MinimumColumn ||
                first.MaximumColumn != second.MaximumColumn)
            {
                error = "Protected Autobahn lane ranges must share one continuous longitudinal span.";
                return false;
            }

            int expectedMinimumColumn = QuantizeBoundary(
                LegacyMinimumWorldX,
                descriptor.GridOrigin.x);
            int expectedMaximumColumn = QuantizeBoundary(
                LegacyMaximumWorldX,
                descriptor.GridOrigin.x);
            if (first.MinimumColumn != expectedMinimumColumn ||
                first.MaximumColumn != expectedMaximumColumn)
            {
                error = "Protected Autobahn lane ranges do not match the quantized legacy X bounds.";
                return false;
            }

            int laneLength = first.MaximumColumn - first.MinimumColumn + 1;
            if (descriptor.Cells.Count != laneLength * 2)
            {
                error = "Protected Autobahn cell count does not cover both complete lane ranges.";
                return false;
            }

            int cellIndex = 0;
            for (int rangeIndex = 0; rangeIndex < descriptor.LaneRanges.Count; rangeIndex++)
            {
                DenseCityProtectedAutobahnLaneRange range = descriptor.LaneRanges[rangeIndex];
                for (int column = range.MinimumColumn; column <= range.MaximumColumn; column++)
                {
                    Vector2Int expected = new(column, range.Row);
                    if (descriptor.Cells[cellIndex] != expected)
                    {
                        error =
                            "Protected Autobahn cells must be sorted by lane row and form continuous ranges.";
                        return false;
                    }
                    cellIndex++;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool TryValidateSourceIds(
            IReadOnlyList<string> sourceGlobalObjectIds,
            out string error)
        {
            if (sourceGlobalObjectIds == null || sourceGlobalObjectIds.Count != 2)
            {
                error = "Protected Autobahn replacement requires exactly two accepted source GlobalObjectIds.";
                return false;
            }

            bool westFound = false;
            bool eastFound = false;
            for (int index = 0; index < sourceGlobalObjectIds.Count; index++)
            {
                string sourceId = sourceGlobalObjectIds[index];
                if (string.Equals(
                        sourceId,
                        AcceptedWestSourceGlobalObjectId,
                        StringComparison.Ordinal))
                {
                    if (westFound)
                    {
                        error = "Protected Autobahn source GlobalObjectIds must not contain duplicates.";
                        return false;
                    }
                    westFound = true;
                }
                else if (string.Equals(
                             sourceId,
                             AcceptedEastSourceGlobalObjectId,
                             StringComparison.Ordinal))
                {
                    if (eastFound)
                    {
                        error = "Protected Autobahn source GlobalObjectIds must not contain duplicates.";
                        return false;
                    }
                    eastFound = true;
                }
                else
                {
                    error = $"Unexpected protected Autobahn source GlobalObjectId '{sourceId ?? "<null>"}'.";
                    return false;
                }
            }

            if (!westFound || !eastFound)
            {
                error = "Both accepted protected Autobahn source GlobalObjectIds are required.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static int QuantizeBoundary(float worldCoordinate, float gridOrigin) =>
            Mathf.RoundToInt((worldCoordinate - gridOrigin) / CellSize);

        private static int QuantizePlacement(float worldCoordinate, float gridOrigin) =>
            Mathf.RoundToInt((worldCoordinate - gridOrigin) / CellSize);

        private static float WorldPlacement(int coordinate, float gridOrigin) =>
            gridOrigin + coordinate * CellSize;
    }
}
