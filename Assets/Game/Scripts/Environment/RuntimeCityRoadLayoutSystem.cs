using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using CityChainAxis = RuntimeCityLayoutSystem.CityChainAxis;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed partial class RuntimeCityRoadLayoutSystem : SystemBase
{
    private readonly RuntimeCityRoadLayoutState _state = new();

    public RuntimeCityRoadLayoutState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public List<List<Vector2Int>> BuildTownRoadStrokes(
        Vector2Int center,
        int townRadius,
        int plazaRadius,
        ref Unity.Mathematics.Random rng)
    {
        return _state.BuildTownRoadStrokes(center, townRadius, plazaRadius, ref rng);
    }

    public List<Vector2Int> BuildStraightRoadPath(Vector2Int start, Vector2Int end)
    {
        return _state.BuildStraightRoadPath(start, end);
    }

    public List<Vector2Int> BuildCityToCityAutobahnPath(
        CityLayoutData fromCity,
        CityLayoutData toCity,
        CityChainAxis chainAxis)
    {
        return _state.BuildCityToCityAutobahnPath(fromCity, toCity, chainAxis);
    }

    public List<Vector2Int> BuildAutobahnPath(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int autobahnEdgeMarginRoadCells,
        int autobahnMinLengthRoadCells)
    {
        return _state.BuildAutobahnPath(
            roadCells,
            centerRoadCell,
            grid,
            roadCellSizeInGridCells,
            autobahnEdgeMarginRoadCells,
            autobahnMinLengthRoadCells);
    }

    public void AddStroke(List<List<Vector2Int>> strokes, Vector2Int start, Vector2Int end)
    {
        _state.AddStroke(strokes, start, end);
    }
}

internal sealed class RuntimeCityRoadLayoutState
{
    public struct AutobahnAnchorCandidate
    {
        public Vector2Int AnchorCell;
        public Vector2Int OutwardDirection;
        public int Score;
    }

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);
    private static readonly Vector2Int[] CardinalDirections = { North, East, South, West };

    public List<List<Vector2Int>> BuildTownRoadStrokes(
        Vector2Int center,
        int townRadius,
        int plazaRadius,
        ref Unity.Mathematics.Random rng)
    {
        var strokes = new List<List<Vector2Int>>();
        int ringRadius = plazaRadius + 1;

        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y - ringRadius), new Vector2Int(center.x + ringRadius, center.y - ringRadius));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y - ringRadius), new Vector2Int(center.x + ringRadius, center.y + ringRadius));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y + ringRadius), new Vector2Int(center.x - ringRadius, center.y + ringRadius));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y + ringRadius), new Vector2Int(center.x - ringRadius, center.y - ringRadius));

        int northLength = townRadius + rng.NextInt(0, 2);
        int southLength = townRadius - 1 + rng.NextInt(0, 3);
        int eastLength = townRadius + rng.NextInt(1, 3);
        int westLength = townRadius - 1 + rng.NextInt(0, 2);

        AddStroke(strokes, new Vector2Int(center.x, center.y + ringRadius), new Vector2Int(center.x, center.y + northLength));
        AddStroke(strokes, new Vector2Int(center.x, center.y - ringRadius), new Vector2Int(center.x, center.y - southLength));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius, center.y), new Vector2Int(center.x + eastLength, center.y));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius, center.y), new Vector2Int(center.x - westLength, center.y));

        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 1, center.y + ringRadius + 2), new Vector2Int(center.x + ringRadius + 2, center.y + ringRadius + 2));
        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 2, center.y - ringRadius - 2), new Vector2Int(center.x + ringRadius + 1, center.y - ringRadius - 2));

        AddStroke(strokes, new Vector2Int(center.x - ringRadius - 2, center.y - ringRadius), new Vector2Int(center.x - ringRadius - 2, center.y + ringRadius + 1));
        AddStroke(strokes, new Vector2Int(center.x + ringRadius + 2, center.y - ringRadius - 1), new Vector2Int(center.x + ringRadius + 2, center.y + ringRadius + 2));

        AddStroke(strokes, new Vector2Int(center.x, center.y + northLength - 1), new Vector2Int(center.x + 2 + rng.NextInt(2, 5), center.y + northLength - 1));
        AddStroke(strokes, new Vector2Int(center.x, center.y - southLength + 1), new Vector2Int(center.x - 1 - rng.NextInt(2, 5), center.y - southLength + 1));
        AddStroke(strokes, new Vector2Int(center.x + eastLength - 1, center.y), new Vector2Int(center.x + eastLength - 1, center.y + 1 + rng.NextInt(2, 5)));
        AddStroke(strokes, new Vector2Int(center.x - westLength + 1, center.y), new Vector2Int(center.x - westLength + 1, center.y - 2 - rng.NextInt(2, 5)));

        return strokes;
    }

    public List<Vector2Int> BuildStraightRoadPath(Vector2Int start, Vector2Int end)
    {
        if (start.x != end.x && start.y != end.y)
            return new List<Vector2Int>();

        var path = new List<Vector2Int> { start };
        AppendStraightSegment(path, start, end);
        return path;
    }

    public List<Vector2Int> BuildCityToCityAutobahnPath(
        CityLayoutData fromCity,
        CityLayoutData toCity,
        CityChainAxis chainAxis)
    {
        Vector2Int forwardDirection = chainAxis == CityChainAxis.Horizontal
            ? (toCity.CenterRoadCell.x >= fromCity.CenterRoadCell.x ? East : West)
            : (toCity.CenterRoadCell.y >= fromCity.CenterRoadCell.y ? North : South);
        Vector2Int backwardDirection = new(-forwardDirection.x, -forwardDirection.y);

        if (!TrySelectDirectionalAutobahnAnchor(fromCity.RoadCells, fromCity.CenterRoadCell, forwardDirection, chainAxis, out AutobahnAnchorCandidate fromAnchor))
            return new List<Vector2Int>();
        if (!TrySelectDirectionalAutobahnAnchor(toCity.RoadCells, toCity.CenterRoadCell, backwardDirection, chainAxis, out AutobahnAnchorCandidate toAnchor))
            return new List<Vector2Int>();

        if (chainAxis == CityChainAxis.Horizontal && fromAnchor.AnchorCell.y != toAnchor.AnchorCell.y)
            return new List<Vector2Int>();
        if (chainAxis == CityChainAxis.Vertical && fromAnchor.AnchorCell.x != toAnchor.AnchorCell.x)
            return new List<Vector2Int>();

        var path = new List<Vector2Int> { fromAnchor.AnchorCell };
        AppendStraightSegment(path, path[path.Count - 1], toAnchor.AnchorCell);
        return path;
    }

    public List<Vector2Int> BuildAutobahnPath(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int autobahnEdgeMarginRoadCells,
        int autobahnMinLengthRoadCells)
    {
        List<AutobahnAnchorCandidate> candidates = CollectAutobahnAnchorCandidates(roadCells, centerRoadCell);
        if (candidates.Count == 0)
            return new List<Vector2Int>();

        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            AutobahnAnchorCandidate candidate = candidates[candidateIndex];
            int maxStepsToEdge = CalculateStepsToEdge(candidate.AnchorCell, candidate.OutwardDirection, roadGridWidth, roadGridHeight, autobahnEdgeMarginRoadCells);
            if (maxStepsToEdge < autobahnMinLengthRoadCells)
                continue;

            var path = new List<Vector2Int> { candidate.AnchorCell };
            Vector2Int current = candidate.AnchorCell;
            for (int step = 0; step < maxStepsToEdge; step++)
            {
                current += candidate.OutwardDirection;
                if (!IsWithinRoadGridBounds(current, roadGridWidth, roadGridHeight, autobahnEdgeMarginRoadCells))
                    break;

                if (roadCells.Contains(current))
                    break;

                path.Add(current);
            }

            if (path.Count >= 3)
                return path;
        }

        return new List<Vector2Int>();
    }

    public void AddStroke(List<List<Vector2Int>> strokes, Vector2Int start, Vector2Int end)
    {
        var cells = new List<Vector2Int>();
        cells.Add(start);
        if (start.x == end.x || start.y == end.y)
        {
            AppendStraightSegment(cells, start, end);
            strokes.Add(cells);
            return;
        }

        Vector2Int corner = new(end.x, start.y);
        AppendStraightSegment(cells, start, corner);
        AppendStraightSegment(cells, corner, end);
        strokes.Add(cells);
    }

    private static bool TrySelectDirectionalAutobahnAnchor(
        HashSet<Vector2Int> roadCells,
        Vector2Int cityCenterRoadCell,
        Vector2Int desiredDirection,
        CityChainAxis chainAxis,
        out AutobahnAnchorCandidate selectedAnchor)
    {
        List<AutobahnAnchorCandidate> candidates = CollectAutobahnAnchorCandidates(roadCells, cityCenterRoadCell);
        int bestScore = int.MinValue;
        bool found = false;
        selectedAnchor = default;

        for (int i = 0; i < candidates.Count; i++)
        {
            AutobahnAnchorCandidate candidate = candidates[i];
            if (candidate.OutwardDirection != desiredDirection)
                continue;

            int perpendicularOffset = chainAxis == CityChainAxis.Horizontal
                ? Mathf.Abs(candidate.AnchorCell.y - cityCenterRoadCell.y)
                : Mathf.Abs(candidate.AnchorCell.x - cityCenterRoadCell.x);
            int score = candidate.Score * 4 - perpendicularOffset * 1000;
            if (score <= bestScore)
                continue;

            bestScore = score;
            selectedAnchor = candidate;
            found = true;
        }

        return found;
    }

    private static List<AutobahnAnchorCandidate> CollectAutobahnAnchorCandidates(
        HashSet<Vector2Int> roadCells,
        Vector2Int centerRoadCell)
    {
        var candidates = new List<AutobahnAnchorCandidate>();

        foreach (Vector2Int cell in roadCells)
        {
            int neighborCount = 0;
            Vector2Int onlyNeighbor = default;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = cell + CardinalDirections[i];
                if (!roadCells.Contains(neighbor))
                    continue;

                neighborCount++;
                onlyNeighbor = neighbor;
            }

            if (neighborCount != 1)
                continue;

            Vector2Int direction = cell - onlyNeighbor;
            Vector2Int fromCenter = cell - centerRoadCell;
            int alignment = direction.x * fromCenter.x + direction.y * fromCenter.y;
            if (alignment <= 0)
                continue;

            int score = fromCenter.sqrMagnitude * 4 + alignment;
            candidates.Add(new AutobahnAnchorCandidate
            {
                AnchorCell = cell,
                OutwardDirection = direction,
                Score = score
            });
        }

        candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
        return candidates;
    }

    private static int CalculateStepsToEdge(
        Vector2Int anchorCell,
        Vector2Int direction,
        int roadGridWidth,
        int roadGridHeight,
        int edgeMargin)
    {
        int minX = Mathf.Max(0, edgeMargin);
        int minY = Mathf.Max(0, edgeMargin);
        int maxX = Mathf.Max(minX, roadGridWidth - 1 - edgeMargin);
        int maxY = Mathf.Max(minY, roadGridHeight - 1 - edgeMargin);

        if (direction == East)
            return Mathf.Max(0, maxX - anchorCell.x);
        if (direction == West)
            return Mathf.Max(0, anchorCell.x - minX);
        if (direction == North)
            return Mathf.Max(0, maxY - anchorCell.y);
        if (direction == South)
            return Mathf.Max(0, anchorCell.y - minY);

        return 0;
    }

    private static bool IsWithinRoadGridBounds(Vector2Int cell, int roadGridWidth, int roadGridHeight, int edgeMargin)
    {
        int minX = Mathf.Max(0, edgeMargin);
        int minY = Mathf.Max(0, edgeMargin);
        int maxX = Mathf.Max(minX, roadGridWidth - 1 - edgeMargin);
        int maxY = Mathf.Max(minY, roadGridHeight - 1 - edgeMargin);
        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
    }

    private static void AppendStraightSegment(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = new(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        Vector2Int current = cells[cells.Count - 1];
        while (current != to)
        {
            current += direction;
            if (cells[cells.Count - 1] != current)
                cells.Add(current);
        }
    }
}
