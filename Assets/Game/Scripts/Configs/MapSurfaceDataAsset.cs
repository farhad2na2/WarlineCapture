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
    private const int CurrentPayloadVersion = 2;
    private const byte FullPayloadEncoding = 0;
    private const byte SingleLayerGridPayloadEncoding = 1;
    private const float PreferredHeightQuantizationStep = 0.01f;
    public const int GitFriendlyPayloadByteLimit = 25 * 1024 * 1024;

    [SerializeField] private Vector3 gridOrigin;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int dimensions = Vector2Int.one;
    [SerializeField] private bool generatedFlatEquivalent;
    [SerializeField] private int surfaceCount;
    [SerializeField] private int connectionCount;
    [SerializeField] private int payloadVersion = CurrentPayloadVersion;
    [SerializeField] private byte payloadEncoding;
    [SerializeField] private int uncompressedPayloadBytes;
    [SerializeField] private byte[] compressedSurfacePayload = Array.Empty<byte>();

    public Vector3 GridOrigin => gridOrigin;
    public float CellSize => cellSize;
    public Vector2Int Dimensions => dimensions;
    public bool GeneratedFlatEquivalent => generatedFlatEquivalent;
    public int SurfaceCount => surfaceCount;
    public int ConnectionCount => connectionCount;
    public int PayloadVersion => payloadVersion;
    public byte PayloadEncoding => payloadEncoding;
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
        payloadEncoding = SingleLayerGridPayloadEncoding;
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
            payloadEncoding = FullPayloadEncoding;
            uncompressedPayloadBytes = 0;
            compressedSurfacePayload = Array.Empty<byte>();
            return;
        }

        ref MapSurfaceBlob blob = ref surfaceBlob.Value;
        surfaceCount = blob.Samples.Length;
        connectionCount = blob.Connections.Length;
        payloadVersion = CurrentPayloadVersion;
        compressedSurfacePayload = BuildCompressedPayload(ref blob, out uncompressedPayloadBytes, out payloadEncoding);
    }

    private static byte[] BuildCompressedPayload(
        ref MapSurfaceBlob blob,
        out int uncompressedByteCount,
        out byte encoding)
    {
        using var uncompressed = new MemoryStream();
        using (var writer = new BinaryWriter(uncompressed, Encoding.UTF8, true))
        {
            if (TryWriteSingleLayerGridPayload(ref blob, writer))
                encoding = SingleLayerGridPayloadEncoding;
            else
                encoding = WriteFullPayload(ref blob, writer);
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

    private static bool TryWriteSingleLayerGridPayload(ref MapSurfaceBlob blob, BinaryWriter writer)
    {
        if (!CanUseSingleLayerGridPayload(ref blob, out float minHeight, out float maxHeight))
            return false;

        float heightStep = Mathf.Max(PreferredHeightQuantizationStep, (maxHeight - minHeight) / ushort.MaxValue);

        writer.Write(CurrentPayloadVersion);
        writer.Write(SingleLayerGridPayloadEncoding);
        writer.Write(blob.Cells.Length);
        writer.Write(blob.Samples.Length);
        writer.Write(blob.Connections.Length);
        writer.Write(blob.Dimensions.x);
        writer.Write(blob.Dimensions.y);
        writer.Write(minHeight);
        writer.Write(heightStep);

        for (int i = 0; i < blob.Samples.Length; i++)
        {
            MapSurfaceSample sample = blob.Samples[i];
            writer.Write(PackHeight(sample.Height, minHeight, heightStep));
            writer.Write(PackNormalComponent(sample.Normal.x));
            writer.Write(PackNormalComponent(sample.Normal.y));
            writer.Write(PackNormalComponent(sample.Normal.z));
            writer.Write((short)Mathf.Clamp(sample.LayerId, short.MinValue, short.MaxValue));
            writer.Write((byte)sample.SurfaceType);
            writer.Write((ushort)sample.MovementMask);
            writer.Write((ushort)sample.Flags);
        }

        return true;
    }

    private static bool CanUseSingleLayerGridPayload(ref MapSurfaceBlob blob, out float minHeight, out float maxHeight)
    {
        minHeight = float.MaxValue;
        maxHeight = float.MinValue;

        int cellCount = blob.Dimensions.x * blob.Dimensions.y;
        if (cellCount <= 0 ||
            blob.Cells.Length != cellCount ||
            blob.Samples.Length != cellCount ||
            blob.Connections.Length != 0)
            return false;

        for (int i = 0; i < cellCount; i++)
        {
            MapSurfaceCell cell = blob.Cells[i];
            MapSurfaceSample sample = blob.Samples[i];
            int x = i % blob.Dimensions.x;
            int y = i / blob.Dimensions.x;

            if (cell.FirstSurfaceIndex != i ||
                cell.SurfaceCount != 1 ||
                sample.Cell.x != x ||
                sample.Cell.y != y ||
                sample.SurfaceId != i ||
                sample.FirstConnectionIndex != 0 ||
                sample.ConnectionCount != 0)
                return false;

            minHeight = Mathf.Min(minHeight, sample.Height);
            maxHeight = Mathf.Max(maxHeight, sample.Height);
        }

        return true;
    }

    private static byte WriteFullPayload(ref MapSurfaceBlob blob, BinaryWriter writer)
    {
        writer.Write(CurrentPayloadVersion);
        writer.Write(FullPayloadEncoding);
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

        return FullPayloadEncoding;
    }

    private static ushort PackHeight(float height, float minHeight, float heightStep)
    {
        if (heightStep <= 0f)
            return 0;

        float normalized = (height - minHeight) / heightStep;
        return (ushort)Mathf.Clamp(Mathf.RoundToInt(normalized), 0, ushort.MaxValue);
    }

    private static short PackNormalComponent(float value)
    {
        return (short)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * short.MaxValue), short.MinValue, short.MaxValue);
    }
}
