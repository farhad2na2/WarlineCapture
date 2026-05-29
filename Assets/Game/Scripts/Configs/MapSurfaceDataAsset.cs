using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSurfaceData", menuName = "WarlineCapture/Map Surface Data")]
public sealed class MapSurfaceDataAsset : ScriptableObject
{
    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int dimensions = Vector2Int.one;
    [SerializeField] private bool generatedFlatEquivalent;
    [SerializeField] private int surfaceCount;
    [SerializeField] private int connectionCount;
    [SerializeField] private SerializedMapSurfaceCell[] cells = Array.Empty<SerializedMapSurfaceCell>();
    [SerializeField] private SerializedMapSurfaceSample[] samples = Array.Empty<SerializedMapSurfaceSample>();
    [SerializeField] private SerializedMapSurfaceConnection[] connections = Array.Empty<SerializedMapSurfaceConnection>();

    public Vector3 GridOrigin => gridOrigin;
    public float CellSize => cellSize;
    public Vector2Int Dimensions => dimensions;
    public bool GeneratedFlatEquivalent => generatedFlatEquivalent;
    public int SurfaceCount => surfaceCount;
    public int ConnectionCount => connectionCount;
    public SerializedMapSurfaceCell[] Cells => cells;
    public SerializedMapSurfaceSample[] Samples => samples;
    public SerializedMapSurfaceConnection[] Connections => connections;

    public void ConfigureFlatEquivalent(Vector3 gridOrigin, float cellSize, Vector2Int dimensions)
    {
        this.gridOrigin = gridOrigin;
        this.cellSize = cellSize;
        this.dimensions = dimensions;
        generatedFlatEquivalent = true;
        surfaceCount = Mathf.Max(0, dimensions.x) * Mathf.Max(0, dimensions.y);
        connectionCount = 0;
        cells = new SerializedMapSurfaceCell[surfaceCount];
        samples = new SerializedMapSurfaceSample[surfaceCount];
        connections = Array.Empty<SerializedMapSurfaceConnection>();
        for (int y = 0; y < dimensions.y; y++)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                int index = x + y * dimensions.x;
                cells[index] = new SerializedMapSurfaceCell
                {
                    FirstSurfaceIndex = index,
                    SurfaceCount = 1,
                    InlineSurfaceIndex = index
                };
                samples[index] = new SerializedMapSurfaceSample
                {
                    Cell = new Vector2Int(x, y),
                    SurfaceId = index,
                    LayerId = 0,
                    Height = gridOrigin.y,
                    Normal = Vector3.up,
                    SlopeDegrees = 0f,
                    SurfaceType = MapSurfaceType.Terrain,
                    MovementMask = MapSurfaceMovementMask.AllGroundUnits |
                                   MapSurfaceMovementMask.AirGrounded |
                                   MapSurfaceMovementMask.BuildingPlacement,
                    Flags = MapSurfaceFlags.None,
                    FirstConnectionIndex = 0,
                    ConnectionCount = 0
                };
            }
        }
    }

    public void ConfigureBakedSurface(
        Vector3 gridOrigin,
        float cellSize,
        Vector2Int dimensions,
        BlobAssetReference<MapSurfaceBlob> surfaceBlob,
        bool generatedFlatEquivalent)
    {
        this.gridOrigin = gridOrigin;
        this.cellSize = cellSize;
        this.dimensions = dimensions;
        this.generatedFlatEquivalent = generatedFlatEquivalent;

        if (!surfaceBlob.IsCreated)
        {
            surfaceCount = 0;
            connectionCount = 0;
            cells = Array.Empty<SerializedMapSurfaceCell>();
            samples = Array.Empty<SerializedMapSurfaceSample>();
            connections = Array.Empty<SerializedMapSurfaceConnection>();
            return;
        }

        ref MapSurfaceBlob blob = ref surfaceBlob.Value;
        cells = new SerializedMapSurfaceCell[blob.Cells.Length];
        for (int i = 0; i < blob.Cells.Length; i++)
        {
            MapSurfaceCell cell = blob.Cells[i];
            cells[i] = new SerializedMapSurfaceCell
            {
                FirstSurfaceIndex = cell.FirstSurfaceIndex,
                SurfaceCount = cell.SurfaceCount,
                InlineSurfaceIndex = cell.InlineSurfaceIndex
            };
        }

        samples = new SerializedMapSurfaceSample[blob.Samples.Length];
        for (int i = 0; i < blob.Samples.Length; i++)
        {
            MapSurfaceSample sample = blob.Samples[i];
            samples[i] = new SerializedMapSurfaceSample
            {
                Cell = new Vector2Int(sample.Cell.x, sample.Cell.y),
                SurfaceId = sample.SurfaceId,
                LayerId = sample.LayerId,
                Height = sample.Height,
                Normal = new Vector3(sample.Normal.x, sample.Normal.y, sample.Normal.z),
                SlopeDegrees = sample.SlopeDegrees,
                SurfaceType = sample.SurfaceType,
                MovementMask = sample.MovementMask,
                Flags = sample.Flags,
                FirstConnectionIndex = sample.FirstConnectionIndex,
                ConnectionCount = sample.ConnectionCount
            };
        }

        connections = new SerializedMapSurfaceConnection[blob.Connections.Length];
        for (int i = 0; i < blob.Connections.Length; i++)
        {
            MapSurfaceConnection connection = blob.Connections[i];
            connections[i] = new SerializedMapSurfaceConnection
            {
                FromSurfaceId = connection.FromSurfaceId,
                ToSurfaceId = connection.ToSurfaceId,
                Direction = new Vector2Int(connection.Direction.x, connection.Direction.y),
                ConnectionType = connection.ConnectionType,
                MovementMask = connection.MovementMask
            };
        }

        surfaceCount = samples.Length;
        connectionCount = connections.Length;
    }
}

[Serializable]
public struct SerializedMapSurfaceCell
{
    public int FirstSurfaceIndex;
    public int SurfaceCount;
    public int InlineSurfaceIndex;
}

[Serializable]
public struct SerializedMapSurfaceSample
{
    public Vector2Int Cell;
    public int SurfaceId;
    public int LayerId;
    public float Height;
    public Vector3 Normal;
    public float SlopeDegrees;
    public MapSurfaceType SurfaceType;
    public MapSurfaceMovementMask MovementMask;
    public MapSurfaceFlags Flags;
    public int FirstConnectionIndex;
    public int ConnectionCount;
}

[Serializable]
public struct SerializedMapSurfaceConnection
{
    public int FromSurfaceId;
    public int ToSurfaceId;
    public Vector2Int Direction;
    public MapSurfaceConnectionType ConnectionType;
    public MapSurfaceMovementMask MovementMask;
}
