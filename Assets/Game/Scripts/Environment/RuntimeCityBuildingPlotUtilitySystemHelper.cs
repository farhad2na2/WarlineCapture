using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using PlotCandidate = RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate;

internal sealed class RuntimeCityBuildingPlotUtilitySystemHelper
{
    private readonly RuntimeCityBuildingPlotState _state = new();

    public RuntimeCityBuildingPlotState State => _state;

    public struct PlotCandidate
    {
        public Vector2Int PlotCell;
        public int DistanceFromCenter;
    }

    public List<PlotCandidate> CollectRoadsidePlots(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        int townRadius,
        int minDistance,
        int maxDistance)
    {
        return _state.CollectRoadsidePlots(roadCells, centerRoadCell, townRadius, minDistance, maxDistance);
    }

    public List<PlotCandidate> CollectEntryRoadsidePlots(CityLayoutData city, int townRadius)
    {
        return _state.CollectEntryRoadsidePlots(city, townRadius);
    }

    public List<PlotCandidate> BuildCorridorRoadsidePlots(
        Vector2Int connectorCell,
        Vector2Int direction,
        int corridorLength)
    {
        return _state.BuildCorridorRoadsidePlots(connectorCell, direction, corridorLength);
    }

    public List<Vector2Int> BuildAdjacentOrigins(RectInt anchorRect, Vector2Int footprint)
    {
        return _state.BuildAdjacentOrigins(anchorRect, footprint);
    }

    public Vector2Int GetRandomScatterPlotCell(
        Vector2Int centerRoadCell,
        int maxDistance,
        ref Unity.Mathematics.Random rng)
    {
        return _state.GetRandomScatterPlotCell(centerRoadCell, maxDistance, ref rng);
    }

    public bool HasPlotSpacing(Vector2Int candidate, List<Vector2Int> usedPlots, int minSpacing)
    {
        return _state.HasPlotSpacing(candidate, usedPlots, minSpacing);
    }

    public Vector2Int GetCenteredOriginForPlot(
        Vector2Int plotCell,
        Vector2Int footprint,
        int roadCellSizeInGridCells)
    {
        return _state.GetCenteredOriginForPlot(plotCell, footprint, roadCellSizeInGridCells);
    }
}

internal sealed class RuntimeCityBuildingPlotState
{
    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);
    private static readonly Vector2Int[] CardinalDirections = { North, East, South, West };

    public List<PlotCandidate> CollectRoadsidePlots(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        int townRadius,
        int minDistance,
        int maxDistance)
    {
        var results = new List<PlotCandidate>();
        var seenPlots = new HashSet<Vector2Int>();

        foreach (Vector2Int roadCell in roadCells)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int plotCell = roadCell + CardinalDirections[i];
                if (roadCells.Contains(plotCell) || !seenPlots.Add(plotCell))
                    continue;

                int distance = Mathf.Abs(plotCell.x - centerRoadCell.x) + Mathf.Abs(plotCell.y - centerRoadCell.y);
                if (distance < minDistance || distance > maxDistance)
                    continue;
                if (Mathf.Abs(plotCell.x - centerRoadCell.x) > townRadius + 3 || Mathf.Abs(plotCell.y - centerRoadCell.y) > townRadius + 3)
                    continue;

                results.Add(new PlotCandidate
                {
                    PlotCell = plotCell,
                    DistanceFromCenter = distance
                });
            }
        }

        results.Sort((a, b) => a.DistanceFromCenter.CompareTo(b.DistanceFromCenter));
        return results;
    }

    public List<PlotCandidate> CollectEntryRoadsidePlots(CityLayoutData city, int townRadius)
    {
        var results = new List<PlotCandidate>();
        if (!city.HasIncomingAnchor)
            return results;

        List<PlotCandidate> candidates = CollectRoadsidePlots(city.RoadCells, city.CenterRoadCell, townRadius, 0, townRadius + 1);
        Vector2Int inwardDirection = -city.IncomingOutwardDirection;
        Vector2Int inwardStart = city.IncomingAnchorCell + inwardDirection;
        for (int i = 0; i < candidates.Count; i++)
        {
            PlotCandidate candidate = candidates[i];
            Vector2Int relativeToEntry = candidate.PlotCell - inwardStart;
            int forwardDistance = relativeToEntry.x * inwardDirection.x + relativeToEntry.y * inwardDirection.y;
            if (forwardDistance < 0 || forwardDistance > 6)
                continue;

            int lateralDistance = Mathf.Abs(relativeToEntry.x * city.IncomingOutwardDirection.y - relativeToEntry.y * city.IncomingOutwardDirection.x);
            if (lateralDistance > 3)
                continue;

            results.Add(candidate);
        }

        return results;
    }

    public List<PlotCandidate> BuildCorridorRoadsidePlots(
        Vector2Int connectorCell,
        Vector2Int direction,
        int corridorLength)
    {
        var corridorPlots = new List<PlotCandidate>();
        if (corridorLength <= 0)
            return corridorPlots;

        Vector2Int left = new(-direction.y, direction.x);
        Vector2Int right = new(direction.y, -direction.x);
        var seen = new HashSet<Vector2Int>();

        for (int step = 1; step <= corridorLength; step++)
        {
            Vector2Int roadCell = connectorCell + direction * step;
            Vector2Int leftPlot = roadCell + left;
            Vector2Int rightPlot = roadCell + right;

            if (seen.Add(leftPlot))
            {
                corridorPlots.Add(new PlotCandidate
                {
                    PlotCell = leftPlot,
                    DistanceFromCenter = corridorLength - step
                });
            }

            if (seen.Add(rightPlot))
            {
                corridorPlots.Add(new PlotCandidate
                {
                    PlotCell = rightPlot,
                    DistanceFromCenter = corridorLength - step
                });
            }
        }

        return corridorPlots;
    }

    public List<Vector2Int> BuildAdjacentOrigins(RectInt anchorRect, Vector2Int footprint)
    {
        var origins = new List<Vector2Int>();

        int leftMinY = anchorRect.yMin - footprint.y + 1;
        int leftMaxY = anchorRect.yMax - 1;
        for (int y = leftMinY; y <= leftMaxY; y++)
        {
            origins.Add(new Vector2Int(anchorRect.xMin - footprint.x, y));
            origins.Add(new Vector2Int(anchorRect.xMax, y));
        }

        int bottomMinX = anchorRect.xMin - footprint.x + 1;
        int bottomMaxX = anchorRect.xMax - 1;
        for (int x = bottomMinX; x <= bottomMaxX; x++)
        {
            origins.Add(new Vector2Int(x, anchorRect.yMin - footprint.y));
            origins.Add(new Vector2Int(x, anchorRect.yMax));
        }

        return origins;
    }

    public Vector2Int GetRandomScatterPlotCell(
        Vector2Int centerRoadCell,
        int maxDistance,
        ref Unity.Mathematics.Random rng)
    {
        float angle = rng.NextFloat(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(rng.NextFloat()) * maxDistance;
        return new Vector2Int(
            centerRoadCell.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius),
            centerRoadCell.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius));
    }

    public bool HasPlotSpacing(Vector2Int candidate, List<Vector2Int> usedPlots, int minSpacing)
    {
        for (int i = 0; i < usedPlots.Count; i++)
        {
            Vector2Int used = usedPlots[i];
            if (Mathf.Abs(candidate.x - used.x) <= minSpacing && Mathf.Abs(candidate.y - used.y) <= minSpacing)
                return false;
        }

        return true;
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
}
