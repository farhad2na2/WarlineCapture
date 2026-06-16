using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed partial class RoadNetworkSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public enum RoadVisualType
    {
        None,
        End,
        Straight,
        Corner,
        TIntersection,
        Intersection,
        Autobahn,
        AutobahnConnect
    }

    public readonly struct TileConnectionMask : IEquatable<TileConnectionMask>
    {
        public readonly bool North;
        public readonly bool East;
        public readonly bool South;
        public readonly bool West;

        public TileConnectionMask(bool north, bool east, bool south, bool west)
        {
            North = north;
            East = east;
            South = south;
            West = west;
        }

        public int Count =>
            (North ? 1 : 0) +
            (East ? 1 : 0) +
            (South ? 1 : 0) +
            (West ? 1 : 0);

        public bool Equals(TileConnectionMask other) =>
            North == other.North &&
            East == other.East &&
            South == other.South &&
            West == other.West;

        public override bool Equals(object obj) => obj is TileConnectionMask other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(North, East, South, West);
    }

    public readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly Vector2Int A;
        public readonly Vector2Int B;

        public EdgeKey(Vector2Int first, Vector2Int second)
        {
            if (first.x < second.x || (first.x == second.x && first.y <= second.y))
            {
                A = first;
                B = second;
            }
            else
            {
                A = second;
                B = first;
            }
        }

        public bool Equals(EdgeKey other) => A == other.A && B == other.B;

        public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(A, B);
    }

    public sealed class StrokeData
    {
        public int Id;
        public List<Vector2Int> Cells = new();
        public bool IsAutobahn;
        public bool UseAutobahnConnectorAtStart;
        public bool UseAutobahnConnectorAtEnd;
    }

    public sealed class RoadTileData
    {
        public RoadVisualType Type;
        public TileConnectionMask Mask;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public sealed class Snapshot
    {
        public int NextStrokeId;
        public Dictionary<EdgeKey, int> EdgeCounts = new();
        public Dictionary<Vector2Int, List<int>> StrokeIdsByCell = new();
        public Dictionary<int, StrokeData> Strokes = new();
        public Dictionary<Vector2Int, RoadTileData> RoadTiles = new();
    }

    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);

    public Dictionary<EdgeKey, int> EdgeCounts { get; } = new();
    public Dictionary<Vector2Int, List<int>> StrokeIdsByCell { get; } = new();
    public Dictionary<int, StrokeData> Strokes { get; } = new();
    public Dictionary<Vector2Int, RoadTileData> RoadTiles { get; } = new();
    public HashSet<Vector2Int> AutobahnCells { get; } = new();
    public HashSet<Vector2Int> AutobahnConnectorCells { get; } = new();
    public int NextStrokeId { get; set; } = 1;

    public bool CreateStroke(
        List<Vector2Int> cells,
        bool isAutobahn,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd,
        out HashSet<Vector2Int> dirtyCells)
    {
        dirtyCells = new HashSet<Vector2Int>();
        if (cells == null || cells.Count < 2)
            return false;

        var stroke = new StrokeData
        {
            Id = NextStrokeId++,
            Cells = cells,
            IsAutobahn = isAutobahn,
            UseAutobahnConnectorAtStart = useAutobahnConnectorAtStart,
            UseAutobahnConnectorAtEnd = useAutobahnConnectorAtEnd
        };

        Strokes.Add(stroke.Id, stroke);

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];

            if (!StrokeIdsByCell.TryGetValue(cell, out var strokeIds))
            {
                strokeIds = new List<int>();
                StrokeIdsByCell.Add(cell, strokeIds);
            }

            strokeIds.Add(stroke.Id);
            dirtyCells.Add(cell);
            AddNeighborCells(cell, dirtyCells);

            if (i == 0)
                continue;

            AddEdge(cells[i - 1], cell);
        }

        AddEndpointConnections(cells, dirtyCells);
        RebuildSpecialRoadCellMetadata();
        return true;
    }

    public bool DeleteStroke(int strokeId, out HashSet<Vector2Int> dirtyCells)
    {
        dirtyCells = new HashSet<Vector2Int>();
        if (!Strokes.TryGetValue(strokeId, out var stroke))
            return false;

        for (int i = 0; i < stroke.Cells.Count; i++)
        {
            Vector2Int cell = stroke.Cells[i];

            if (StrokeIdsByCell.TryGetValue(cell, out var strokeIds))
            {
                strokeIds.Remove(strokeId);
                if (strokeIds.Count == 0)
                    StrokeIdsByCell.Remove(cell);
            }

            dirtyCells.Add(cell);
            AddNeighborCells(cell, dirtyCells);

            if (i == 0)
                continue;

            RemoveEdge(stroke.Cells[i - 1], cell);
        }

        Strokes.Remove(strokeId);
        RebuildSpecialRoadCellMetadata();
        return true;
    }

    public Snapshot CaptureSnapshot()
    {
        var snapshot = new Snapshot
        {
            NextStrokeId = NextStrokeId
        };

        foreach (var entry in EdgeCounts)
            snapshot.EdgeCounts.Add(entry.Key, entry.Value);

        foreach (var entry in StrokeIdsByCell)
            snapshot.StrokeIdsByCell.Add(entry.Key, new List<int>(entry.Value));

        foreach (var entry in Strokes)
            snapshot.Strokes.Add(entry.Key, CloneStroke(entry.Value));

        foreach (var entry in RoadTiles)
            snapshot.RoadTiles.Add(entry.Key, CloneRoadTile(entry.Value));

        return snapshot;
    }

    public void RestoreSnapshot(Snapshot snapshot)
    {
        if (snapshot == null)
            return;

        NextStrokeId = snapshot.NextStrokeId;

        EdgeCounts.Clear();
        foreach (var entry in snapshot.EdgeCounts)
            EdgeCounts.Add(entry.Key, entry.Value);

        StrokeIdsByCell.Clear();
        foreach (var entry in snapshot.StrokeIdsByCell)
            StrokeIdsByCell.Add(entry.Key, new List<int>(entry.Value));

        Strokes.Clear();
        foreach (var entry in snapshot.Strokes)
            Strokes.Add(entry.Key, CloneStroke(entry.Value));

        AutobahnCells.Clear();
        AutobahnConnectorCells.Clear();
        RebuildSpecialRoadCellMetadata();

        RoadTiles.Clear();
        foreach (var entry in snapshot.RoadTiles)
            RoadTiles.Add(entry.Key, CloneRoadTile(entry.Value));
    }

    public void RebuildSpecialRoadCellMetadata()
    {
        AutobahnCells.Clear();
        AutobahnConnectorCells.Clear();

        foreach (var stroke in Strokes.Values)
        {
            if (!stroke.IsAutobahn || stroke.Cells.Count == 0)
                continue;

            int startIndex = 0;
            int endIndex = stroke.Cells.Count - 1;

            if (stroke.UseAutobahnConnectorAtStart)
            {
                AutobahnConnectorCells.Add(stroke.Cells[startIndex]);
                startIndex++;
            }

            if (stroke.UseAutobahnConnectorAtEnd && endIndex >= 0)
            {
                AutobahnConnectorCells.Add(stroke.Cells[endIndex]);
                endIndex--;
            }

            for (int i = startIndex; i <= endIndex; i++)
                AutobahnCells.Add(stroke.Cells[i]);
        }

        AutobahnCells.ExceptWith(AutobahnConnectorCells);
    }

    public TileConnectionMask GetMask(Vector2Int cell)
    {
        return new TileConnectionMask(
            HasEdge(cell, cell + North),
            HasEdge(cell, cell + East),
            HasEdge(cell, cell + South),
            HasEdge(cell, cell + West));
    }

    public bool HasEdge(Vector2Int a, Vector2Int b) => EdgeCounts.ContainsKey(new EdgeKey(a, b));

    public IEnumerable<Vector2Int> GetAdjacentRoadCells(Vector2Int cell)
    {
        Vector2Int[] neighbors = { cell + North, cell + East, cell + South, cell + West };
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (StrokeIdsByCell.ContainsKey(neighbors[i]))
                yield return neighbors[i];
        }
    }

    private static StrokeData CloneStroke(StrokeData source)
    {
        return new StrokeData
        {
            Id = source.Id,
            Cells = new List<Vector2Int>(source.Cells),
            IsAutobahn = source.IsAutobahn,
            UseAutobahnConnectorAtStart = source.UseAutobahnConnectorAtStart,
            UseAutobahnConnectorAtEnd = source.UseAutobahnConnectorAtEnd
        };
    }

    private static RoadTileData CloneRoadTile(RoadTileData source)
    {
        return new RoadTileData
        {
            Type = source.Type,
            Mask = source.Mask,
            Rotation = source.Rotation,
            Scale = source.Scale
        };
    }

    private void AddEdge(Vector2Int a, Vector2Int b)
    {
        var key = new EdgeKey(a, b);
        EdgeCounts.TryGetValue(key, out int count);
        EdgeCounts[key] = count + 1;
    }

    private void RemoveEdge(Vector2Int a, Vector2Int b)
    {
        var key = new EdgeKey(a, b);
        if (!EdgeCounts.TryGetValue(key, out int count))
            return;

        if (count <= 1)
            EdgeCounts.Remove(key);
        else
            EdgeCounts[key] = count - 1;
    }

    private void AddEndpointConnections(List<Vector2Int> path, HashSet<Vector2Int> dirtyCells)
    {
        AddEndpointConnectionsForCell(path, 0, dirtyCells);
        AddEndpointConnectionsForCell(path, path.Count - 1, dirtyCells);
    }

    private void AddEndpointConnectionsForCell(List<Vector2Int> path, int index, HashSet<Vector2Int> dirtyCells)
    {
        if (path.Count < 2)
            return;

        Vector2Int endpoint = path[index];
        Vector2Int inwardNeighbor = index == 0 ? path[1] : path[path.Count - 2];

        foreach (Vector2Int neighbor in GetAdjacentRoadCells(endpoint))
        {
            if (neighbor == inwardNeighbor)
                continue;

            AddEdge(endpoint, neighbor);
            dirtyCells.Add(neighbor);
            AddNeighborCells(neighbor, dirtyCells);
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
