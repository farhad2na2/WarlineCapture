using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSurfaceData", menuName = "WarlineCapture/Map Surface Data")]
public sealed class MapSurfaceDataAsset : ScriptableObject
{
    private const int CurrentPayloadVersion = 1;
    public const int GitFriendlyPayloadByteLimit = 25 * 1024 * 1024;

    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int dimensions = Vector2Int.one;
    [SerializeField] private bool generatedFlatEquivalent;
    [SerializeField] private int surfaceCount;
    [SerializeField] private int connectionCount;
    [SerializeField] private int payloadVersion = CurrentPayloadVersion;
    [SerializeField] private int uncompressedPayloadBytes;
    [SerializeField] private byte[] compressedSurfacePayload = Array.Empty<byte>();

    public Vector3 GridOrigin => gridOrigin;
    public float CellSize => cellSize;
    public Vector2Int Dimensions => dimensions;
    public bool GeneratedFlatEquivalent => generatedFlatEquivalent;
    public int SurfaceCount => surfaceCount;
    public int ConnectionCount => connectionCount;
    public int PayloadVersion => payloadVersion;
    public int UncompressedPayloadBytes => uncompressedPayloadBytes;
    public int CompressedPayloadBytes => compressedSurfacePayload?.Length ?? 0;
    public bool HasCompactPayload => compressedSurfacePayload != null && compressedSurfacePayload.Length > 0;

    public void ConfigureFlatEquivalent(Vector3 gridOrigin, float cellSize, Vector2Int dimensions)
    {
        this.gridOrigin = gridOrigin;
        this.cellSize = cellSize;
        this.dimensions = dimensions;
        generatedFlatEquivalent = true;
        surfaceCount = Mathf.Max(0, dimensions.x) * Mathf.Max(0, dimensions.y);
        connectionCount = 0;
        payloadVersion = CurrentPayloadVersion;
        uncompressedPayloadBytes = 0;
        compressedSurfacePayload = Array.Empty<byte>();
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
            payloadVersion = CurrentPayloadVersion;
            uncompressedPayloadBytes = 0;
            compressedSurfacePayload = Array.Empty<byte>();
            return;
        }

        ref MapSurfaceBlob blob = ref surfaceBlob.Value;
        surfaceCount = blob.Samples.Length;
        connectionCount = blob.Connections.Length;
        payloadVersion = CurrentPayloadVersion;
        compressedSurfacePayload = BuildCompressedPayload(ref blob, out uncompressedPayloadBytes);
    }

    private static byte[] BuildCompressedPayload(ref MapSurfaceBlob blob, out int uncompressedByteCount)
    {
        using var uncompressed = new MemoryStream();
        using (var writer = new BinaryWriter(uncompressed, Encoding.UTF8, true))
        {
            writer.Write(CurrentPayloadVersion);
            writer.Write(blob.Cells.Length);
            writer.Write(blob.Samples.Length);
            writer.Write(blob.Connections.Length);

            for (int i = 0; i < blob.Cells.Length; i++)
            {
                MapSurfaceCell cell = blob.Cells[i];
                writer.Write(cell.FirstSurfaceIndex);
                writer.Write(cell.SurfaceCount);
                writer.Write(cell.InlineSurfaceIndex);
            }

            for (int i = 0; i < blob.Samples.Length; i++)
            {
                MapSurfaceSample sample = blob.Samples[i];
                writer.Write(sample.Cell.x);
                writer.Write(sample.Cell.y);
                writer.Write(sample.SurfaceId);
                writer.Write(sample.LayerId);
                writer.Write(sample.Height);
                writer.Write(sample.Normal.x);
                writer.Write(sample.Normal.y);
                writer.Write(sample.Normal.z);
                writer.Write(sample.SlopeDegrees);
                writer.Write((int)sample.SurfaceType);
                writer.Write((int)sample.MovementMask);
                writer.Write((int)sample.Flags);
                writer.Write(sample.FirstConnectionIndex);
                writer.Write(sample.ConnectionCount);
            }

            for (int i = 0; i < blob.Connections.Length; i++)
            {
                MapSurfaceConnection connection = blob.Connections[i];
                writer.Write(connection.FromSurfaceId);
                writer.Write(connection.ToSurfaceId);
                writer.Write(connection.Direction.x);
                writer.Write(connection.Direction.y);
                writer.Write((int)connection.ConnectionType);
                writer.Write((int)connection.MovementMask);
            }
        }

        uncompressedByteCount = checked((int)uncompressed.Length);
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal, true))
        {
            uncompressed.Position = 0;
            uncompressed.CopyTo(gzip);
        }

        return compressed.ToArray();
    }
}
