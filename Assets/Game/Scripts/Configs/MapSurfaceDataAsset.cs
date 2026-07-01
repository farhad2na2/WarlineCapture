using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSurfaceData", menuName = "Game/Map Surface Data")]
public sealed class MapSurfaceDataAsset : ScriptableObject
{
    private const int CurrentPayloadVersion = 3;
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

    public Unity.Entities.Hash128 ComputeRuntimeBlobHash()
    {
        unchecked
        {
            uint h1 = 2166136261u;
            uint h2 = 2166136261u;
            uint h3 = 2166136261u;
            uint h4 = 2166136261u;

            Mix(ref h1, payloadVersion);
            Mix(ref h1, payloadEncoding);
            Mix(ref h1, uncompressedPayloadBytes);
            Mix(ref h1, dimensions.x);
            Mix(ref h1, dimensions.y);
            Mix(ref h2, cellSize.GetHashCode());
            Mix(ref h2, gridOrigin.x.GetHashCode());
            Mix(ref h2, gridOrigin.y.GetHashCode());
            Mix(ref h2, gridOrigin.z.GetHashCode());
            Mix(ref h3, generatedFlatEquivalent ? 1 : 0);
            Mix(ref h3, surfaceCount);
            Mix(ref h3, connectionCount);

            byte[] payload = compressedSurfacePayload;
            if (payload != null)
            {
                Mix(ref h4, payload.Length);
                for (int i = 0; i < payload.Length; i++)
                {
                    uint value = payload[i];
                    switch (i & 3)
                    {
                        case 0:
                            Mix(ref h1, value);
                            break;
                        case 1:
                            Mix(ref h2, value);
                            break;
                        case 2:
                            Mix(ref h3, value);
                            break;
                        default:
                            Mix(ref h4, value);
                            break;
                    }
                }
            }

            return new Unity.Entities.Hash128(h1, h2, h3, h4);
        }
    }

    public bool TryCreateRuntimeBlobAsset(
        Allocator allocator,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
    {
        surfaceBlob = default;

        if (!HasCompactPayload || cellSize <= 0f || dimensions.x <= 0 || dimensions.y <= 0)
            return false;

        try
        {
            using var compressed = new MemoryStream(compressedSurfacePayload);
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var reader = new BinaryReader(gzip, Encoding.UTF8, true);
            int version = reader.ReadInt32();
            if (version == 1)
                return TryReadFullPayload(reader, allocator, out surfaceBlob);

            if (version != 2 && version != CurrentPayloadVersion)
                return false;

            byte encoding = reader.ReadByte();
            return encoding switch
            {
                SingleLayerGridPayloadEncoding => TryReadSingleLayerGridPayload(reader, version, allocator, out surfaceBlob),
                FullPayloadEncoding => TryReadFullPayload(reader, allocator, out surfaceBlob),
                _ => false
            };
        }
        catch (Exception exception)
        {
            if (surfaceBlob.IsCreated)
                surfaceBlob.Dispose();

            surfaceBlob = default;
            Debug.LogError($"Failed to load baked map surface data '{name}': {exception.Message}", this);
            return false;
        }
    }

    private static void Mix(ref uint hash, int value)
    {
        unchecked
        {
            Mix(ref hash, (uint)value);
        }
    }

    private static void Mix(ref uint hash, uint value)
    {
        unchecked
        {
            hash ^= value;
            hash *= 16777619u;
            hash ^= value >> 16;
            hash *= 16777619u;
        }
    }

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
        surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
        connectionCount = MapSurfaceBlobAccess.ConnectionCount(ref blob);
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
        if (MapSurfaceBlobAccess.IsCompactSingleLayer(ref blob))
        {
            writer.Write(CurrentPayloadVersion);
            writer.Write(SingleLayerGridPayloadEncoding);
            writer.Write(blob.CompactSamples.Length);
            writer.Write(blob.CompactSamples.Length);
            writer.Write(blob.Connections.Length);
            writer.Write(blob.Dimensions.x);
            writer.Write(blob.Dimensions.y);
            writer.Write(blob.CompactMinHeight);
            writer.Write(blob.CompactHeightStep);

            for (int i = 0; i < blob.CompactSamples.Length; i++)
            {
                MapSurfaceCompactSample sample = blob.CompactSamples[i];
                writer.Write(sample.PackedHeight);
                writer.Write(sample.NormalX);
                writer.Write(sample.NormalY);
                writer.Write(sample.NormalZ);
                writer.Write(sample.LayerId);
                writer.Write((byte)sample.SurfaceType);
                writer.Write((ushort)sample.MovementMask);
                writer.Write((ushort)sample.Flags);
            }

            return true;
        }

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
            writer.Write(PackNormalByte(sample.Normal.x));
            writer.Write(PackNormalByte(sample.Normal.y));
            writer.Write(PackNormalByte(sample.Normal.z));
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

    private static sbyte PackNormalByte(float value)
    {
        return (sbyte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * sbyte.MaxValue), sbyte.MinValue, sbyte.MaxValue);
    }

    private bool TryReadSingleLayerGridPayload(
        BinaryReader reader,
        int payloadVersion,
        Allocator allocator,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
    {
        surfaceBlob = default;

        int cellCount = reader.ReadInt32();
        int sampleCount = reader.ReadInt32();
        int serializedConnectionCount = reader.ReadInt32();
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        float minHeight = reader.ReadSingle();
        float heightStep = reader.ReadSingle();

        if (width != dimensions.x ||
            height != dimensions.y ||
            cellCount != width * height ||
            sampleCount != cellCount ||
            serializedConnectionCount != 0)
            return false;

        using var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = ToFloat3(gridOrigin);
        root.CellSize = cellSize;
        root.Dimensions = new int2(width, height);
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
        root.CompactMinHeight = minHeight;
        root.CompactHeightStep = heightStep;

        builder.Allocate(ref root.Cells, 0);
        builder.Allocate(ref root.Samples, 0);
        builder.Allocate(ref root.Connections, 0);
        BlobBuilderArray<MapSurfaceCompactSample> samples = builder.Allocate(ref root.CompactSamples, sampleCount);

        for (int i = 0; i < sampleCount; i++)
        {
            ushort packedHeight = reader.ReadUInt16();
            sbyte normalX;
            sbyte normalY;
            sbyte normalZ;
            if (payloadVersion >= 3)
            {
                normalX = reader.ReadSByte();
                normalY = reader.ReadSByte();
                normalZ = reader.ReadSByte();
            }
            else
            {
                normalX = PackNormalByte(UnpackNormalComponent(reader.ReadInt16()));
                normalY = PackNormalByte(UnpackNormalComponent(reader.ReadInt16()));
                normalZ = PackNormalByte(UnpackNormalComponent(reader.ReadInt16()));
            }

            samples[i] = new MapSurfaceCompactSample
            {
                LayerId = reader.ReadInt16(),
                PackedHeight = packedHeight,
                NormalX = normalX,
                NormalY = normalY,
                NormalZ = normalZ,
                SurfaceType = (MapSurfaceType)reader.ReadByte(),
                MovementMask = (MapSurfaceMovementMask)reader.ReadUInt16(),
                Flags = (MapSurfaceFlags)reader.ReadUInt16()
            };
        }

        surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(allocator);
        return surfaceBlob.IsCreated;
    }

    private bool TryReadFullPayload(
        BinaryReader reader,
        Allocator allocator,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
    {
        surfaceBlob = default;

        int cellCount = reader.ReadInt32();
        int sampleCount = reader.ReadInt32();
        int serializedConnectionCount = reader.ReadInt32();
        if (cellCount <= 0 || sampleCount <= 0 || serializedConnectionCount < 0)
            return false;

        using var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = ToFloat3(gridOrigin);
        root.CellSize = cellSize;
        root.Dimensions = new int2(dimensions.x, dimensions.y);
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, sampleCount);
        BlobBuilderArray<MapSurfaceConnection> connections = builder.Allocate(ref root.Connections, serializedConnectionCount);
        builder.Allocate(ref root.CompactSamples, 0);

        for (int i = 0; i < cellCount; i++)
        {
            cells[i] = new MapSurfaceCell
            {
                FirstSurfaceIndex = reader.ReadInt32(),
                SurfaceCount = reader.ReadUInt16(),
                InlineSurfaceIndex = reader.ReadUInt16()
            };
        }

        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = new MapSurfaceSample
            {
                Cell = new int2(reader.ReadInt32(), reader.ReadInt32()),
                SurfaceId = reader.ReadInt32(),
                LayerId = reader.ReadInt32(),
                Height = reader.ReadSingle(),
                Normal = new float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                SlopeDegrees = reader.ReadSingle(),
                SurfaceType = (MapSurfaceType)reader.ReadInt32(),
                MovementMask = (MapSurfaceMovementMask)reader.ReadInt32(),
                Flags = (MapSurfaceFlags)reader.ReadInt32(),
                FirstConnectionIndex = reader.ReadInt32(),
                ConnectionCount = reader.ReadUInt16()
            };
        }

        for (int i = 0; i < serializedConnectionCount; i++)
        {
            connections[i] = new MapSurfaceConnection
            {
                FromSurfaceId = reader.ReadInt32(),
                ToSurfaceId = reader.ReadInt32(),
                Direction = new int2(reader.ReadInt32(), reader.ReadInt32()),
                ConnectionType = (MapSurfaceConnectionType)reader.ReadInt32(),
                MovementMask = (MapSurfaceMovementMask)reader.ReadInt32()
            };
        }

        surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(allocator);
        return surfaceBlob.IsCreated;
    }

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
    }

    private static float UnpackNormalComponent(short value)
    {
        return Mathf.Clamp(value / (float)short.MaxValue, -1f, 1f);
    }

    private static float UnpackNormalByte(sbyte value)
    {
        return Mathf.Clamp(value / (float)sbyte.MaxValue, -1f, 1f);
    }

    private static float CalculateSlopeDegrees(float3 normal)
    {
        float y = math.clamp(math.normalizesafe(normal, new float3(0f, 1f, 0f)).y, -1f, 1f);
        return math.degrees(math.acos(y));
    }
}
