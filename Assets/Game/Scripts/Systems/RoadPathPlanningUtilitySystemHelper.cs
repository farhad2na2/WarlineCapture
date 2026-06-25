using System;
using System.Collections.Generic;
using UnityEngine;
using EdgeKey = RoadNetworkCompositionSystemHelper.EdgeKey;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;

public sealed class RoadPathPlanningUtilitySystemHelper
{
    public enum DragFirstAxis
    {
        None,
        Horizontal,
        Vertical
    }

    public sealed class PreviewPlan
    {
        public readonly List<Vector2Int> Path = new();
        public readonly HashSet<EdgeKey> ProposedEdges = new();
        public readonly HashSet<Vector2Int> DirtyCells = new();
    }

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);

    public DragFirstAxis ResolveDragFirstAxis(Vector2Int startCell, Vector2Int hoveredCell, DragFirstAxis currentAxis)
    {
        if (currentAxis != DragFirstAxis.None)
            return currentAxis;

        Vector2Int delta = hoveredCell - startCell;
        if (delta.x == 0 && delta.y == 0)
            return currentAxis;

        return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? DragFirstAxis.Horizontal
            : DragFirstAxis.Vertical;
    }

    public List<Vector2Int> BuildPath(Vector2Int startCell, Vector2Int endCell, DragFirstAxis dragFirstAxis)
    {
        var cells = new List<Vector2Int>();
        cells.Add(startCell);
        if (startCell == endCell)
            return cells;

        if (startCell.x == endCell.x || startCell.y == endCell.y)
        {
            AppendStraightSegment(cells, startCell, endCell);
            return cells;
        }

        Vector2Int corner = dragFirstAxis == DragFirstAxis.Vertical
            ? new Vector2Int(startCell.x, endCell.y)
            : new Vector2Int(endCell.x, startCell.y);

        AppendStraightSegment(cells, startCell, corner);
        AppendStraightSegment(cells, corner, endCell);
        return cells;
    }

    public PreviewPlan BuildPreviewPlan(
        Vector2Int startCell,
        Vector2Int endCell,
        DragFirstAxis dragFirstAxis,
        RoadNetworkCompositionSystemHelper roadNetworkSystem)
    {
        var plan = new PreviewPlan();
        plan.Path.AddRange(BuildPath(startCell, endCell, dragFirstAxis));
        if (plan.Path.Count <= 1)
            return plan;

        for (int i = 0; i < plan.Path.Count; i++)
        {
            plan.DirtyCells.Add(plan.Path[i]);
            AddNeighborCells(plan.Path[i], plan.DirtyCells);

            if (i > 0)
                plan.ProposedEdges.Add(new EdgeKey(plan.Path[i - 1], plan.Path[i]));
        }

        AddEndpointPreviewConnections(plan.Path, plan.ProposedEdges, plan.DirtyCells, roadNetworkSystem);
        return plan;
    }

    public TileConnectionMask GetPreviewMask(
        Vector2Int cell,
        HashSet<EdgeKey> proposedEdges,
        RoadNetworkCompositionSystemHelper roadNetworkSystem)
    {
        return new TileConnectionMask(
            HasPreviewEdge(cell, cell + North, proposedEdges, roadNetworkSystem),
            HasPreviewEdge(cell, cell + East, proposedEdges, roadNetworkSystem),
            HasPreviewEdge(cell, cell + South, proposedEdges, roadNetworkSystem),
            HasPreviewEdge(cell, cell + West, proposedEdges, roadNetworkSystem));
    }

    private static bool HasPreviewEdge(
        Vector2Int a,
        Vector2Int b,
        HashSet<EdgeKey> proposedEdges,
        RoadNetworkCompositionSystemHelper roadNetworkSystem)
    {
        var key = new EdgeKey(a, b);
        return roadNetworkSystem.EdgeCounts.ContainsKey(key) || proposedEdges.Contains(key);
    }

    private static void AddEndpointPreviewConnections(
        List<Vector2Int> path,
        HashSet<EdgeKey> proposedEdges,
        HashSet<Vector2Int> dirtyCells,
        RoadNetworkCompositionSystemHelper roadNetworkSystem)
    {
        AddEndpointPreviewConnectionsForCell(path, 0, proposedEdges, dirtyCells, roadNetworkSystem);
        AddEndpointPreviewConnectionsForCell(path, path.Count - 1, proposedEdges, dirtyCells, roadNetworkSystem);
    }

    private static void AddEndpointPreviewConnectionsForCell(
        List<Vector2Int> path,
        int index,
        HashSet<EdgeKey> proposedEdges,
        HashSet<Vector2Int> dirtyCells,
        RoadNetworkCompositionSystemHelper roadNetworkSystem)
    {
        if (path.Count < 2)
            return;

        Vector2Int endpoint = path[index];
        Vector2Int inwardNeighbor = index == 0 ? path[1] : path[path.Count - 2];

        foreach (Vector2Int neighbor in roadNetworkSystem.GetAdjacentRoadCells(endpoint))
        {
            if (neighbor == inwardNeighbor)
                continue;

            proposedEdges.Add(new EdgeKey(endpoint, neighbor));
            dirtyCells.Add(neighbor);
            AddNeighborCells(neighbor, dirtyCells);
        }
    }

    private static void AppendStraightSegment(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = new(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        Vector2Int current = cells[cells.Count - 1];
        while (current != to)
        {
            current += direction;
            if (cells.Count == 0 || cells[cells.Count - 1] != current)
                cells.Add(current);
        }
    }

    private static void AddNeighborCells(Vector2Int cell, HashSet<Vector2Int> cells)
    {
        cells.Add(cell + North);
        cells.Add(cell + East);
        cells.Add(cell + South);
        cells.Add(cell + West);
    }
}
