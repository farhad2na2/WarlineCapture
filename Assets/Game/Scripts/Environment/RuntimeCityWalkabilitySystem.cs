using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityWalkabilitySystem
{
    public struct ReservedFootprint
    {
        public RectInt Rect;
        public int ClearanceCells;
    }

    public void ReserveFootprint(
        List<ReservedFootprint> reservedFootprints,
        Vector2Int originCell,
        Vector2Int footprint,
        int clearanceCells)
    {
        reservedFootprints.Add(new ReservedFootprint
        {
            Rect = new RectInt(originCell, footprint),
            ClearanceCells = Mathf.Max(0, clearanceCells)
        });
    }

    public void ReserveStandaloneEntranceCorridor(
        CityLayoutData city,
        Vector2Int startRoadCell,
        Vector2Int direction,
        int roadSegmentCount,
        int roadCellSizeInGridCells)
    {
        Vector2Int roadFootprint = new(
            Mathf.Max(1, roadCellSizeInGridCells),
            Mathf.Max(1, roadCellSizeInGridCells));

        for (int step = 0; step <= roadSegmentCount; step++)
        {
            Vector2Int roadCell = startRoadCell + direction * step;
            Vector2Int originCell = GetCenteredOriginForPlot(roadCell, roadFootprint, roadCellSizeInGridCells);
            ReserveFootprint(city.ReservedFootprints, originCell, roadFootprint, 0);
        }
    }

    public bool WouldBeTooCloseToReserved(
        Vector2Int originCell,
        Vector2Int footprint,
        List<ReservedFootprint> reservedFootprints,
        int additionalClearanceCells)
    {
        RectInt candidateRect = new(originCell, footprint);
        for (int i = 0; i < reservedFootprints.Count; i++)
        {
            ReservedFootprint reserved = reservedFootprints[i];
            RectInt expandedReserved = ExpandRect(reserved.Rect, reserved.ClearanceCells + Mathf.Max(0, additionalClearanceCells));
            if (expandedReserved.Overlaps(candidateRect))
                return true;
        }

        return false;
    }

    public bool CanPlaceHouseYardRect(
        RectInt yardRect,
        RectInt houseRect,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        List<ReservedFootprint> reservedFootprints,
        GridConfig grid)
    {
        if (yardRect.xMin < 0 || yardRect.yMin < 0 || yardRect.xMax > grid.Width || yardRect.yMax > grid.Height)
            return false;
        if (DoesRectOverlapRoadCells(yardRect, roadCellSizeInGridCells, roadCells))
            return false;

        for (int i = 0; i < reservedFootprints.Count; i++)
        {
            RectInt reserved = reservedFootprints[i].Rect;
            if (RectsEqual(reserved, houseRect))
                continue;
            if (yardRect.Overlaps(reserved))
                return false;
        }

        return true;
    }

    public RectInt ExpandRect(RectInt rect, int padding)
    {
        if (padding <= 0)
            return rect;

        return new RectInt(
            rect.xMin - padding,
            rect.yMin - padding,
            rect.width + padding * 2,
            rect.height + padding * 2);
    }

    public bool DoesRectOverlapRoadCells(RectInt rect, int roadCellSizeInGridCells, HashSet<Vector2Int> roadCells)
    {
        foreach (Vector2Int roadCell in roadCells)
        {
            RectInt roadRect = new(
                roadCell.x * roadCellSizeInGridCells,
                roadCell.y * roadCellSizeInGridCells,
                roadCellSizeInGridCells,
                roadCellSizeInGridCells);
            if (rect.Overlaps(roadRect))
                return true;
        }

        return false;
    }

    public bool TouchesRect(RectInt rectA, RectInt rectB)
    {
        bool horizontalTouch =
            (rectA.xMax == rectB.xMin || rectA.xMin == rectB.xMax) &&
            rectA.yMin < rectB.yMax &&
            rectA.yMax > rectB.yMin;
        bool verticalTouch =
            (rectA.yMax == rectB.yMin || rectA.yMin == rectB.yMax) &&
            rectA.xMin < rectB.xMax &&
            rectA.xMax > rectB.xMin;
        return horizontalTouch || verticalTouch;
    }

    public Vector2Int GetCenteredOriginForPlot(
        Vector2Int plotCell,
        Vector2Int footprint,
        int roadCellSizeInGridCells)
    {
        return new Vector2Int(
            plotCell.x * roadCellSizeInGridCells + Mathf.FloorToInt((roadCellSizeInGridCells - footprint.x) * 0.5f),
            plotCell.y * roadCellSizeInGridCells + Mathf.FloorToInt((roadCellSizeInGridCells - footprint.y) * 0.5f));
    }

    private static bool RectsEqual(RectInt a, RectInt b)
    {
        return a.xMin == b.xMin && a.yMin == b.yMin && a.width == b.width && a.height == b.height;
    }
}
