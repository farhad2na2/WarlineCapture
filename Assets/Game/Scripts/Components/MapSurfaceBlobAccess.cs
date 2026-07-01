using Unity.Mathematics;

public static class MapSurfaceBlobAccess
{
    public static bool IsCompactSingleLayer(ref MapSurfaceBlob blob)
    {
        return blob.RuntimeEncoding == MapSurfaceRuntimeEncoding.SingleLayerCompact &&
               blob.CompactSamples.Length > 0 &&
               blob.Dimensions.x > 0 &&
               blob.Dimensions.y > 0 &&
               blob.CompactSamples.Length == blob.Dimensions.x * blob.Dimensions.y &&
               blob.Connections.Length == 0;
    }

    public static int CellCount(ref MapSurfaceBlob blob)
    {
        return IsCompactSingleLayer(ref blob)
            ? blob.Dimensions.x * blob.Dimensions.y
            : blob.Cells.Length;
    }

    public static int SurfaceCount(ref MapSurfaceBlob blob)
    {
        return IsCompactSingleLayer(ref blob)
            ? blob.CompactSamples.Length
            : blob.Samples.Length;
    }

    public static int ConnectionCount(ref MapSurfaceBlob blob)
    {
        return blob.Connections.Length;
    }

    public static bool TryGetSurfaceRange(ref MapSurfaceBlob blob, int2 cell, out MapSurfaceCellSurfaceRange range)
    {
        range = default;
        if ((uint)cell.x >= (uint)blob.Dimensions.x ||
            (uint)cell.y >= (uint)blob.Dimensions.y)
        {
            return false;
        }

        int index = cell.x + cell.y * blob.Dimensions.x;
        if (IsCompactSingleLayer(ref blob))
        {
            if ((uint)index >= (uint)blob.CompactSamples.Length)
                return false;

            range = new MapSurfaceCellSurfaceRange
            {
                FirstSurfaceIndex = index,
                SurfaceCount = 1,
                InlineSurfaceIndex = 0,
                IsLayered = 0
            };
            return true;
        }

        if ((uint)index >= (uint)blob.Cells.Length)
            return false;

        MapSurfaceCell cellData = blob.Cells[index];
        if (cellData.SurfaceCount == 0)
            return false;

        range = new MapSurfaceCellSurfaceRange
        {
            FirstSurfaceIndex = cellData.FirstSurfaceIndex,
            SurfaceCount = cellData.SurfaceCount,
            InlineSurfaceIndex = cellData.InlineSurfaceIndex,
            IsLayered = (byte)(cellData.SurfaceCount > 1 ? 1 : 0)
        };
        return true;
    }

    public static bool TryGetSurface(ref MapSurfaceBlob blob, MapSurfaceCellSurfaceRange range, int surfaceOffset, out MapSurfaceSample sample)
    {
        sample = default;
        if (surfaceOffset < 0 || surfaceOffset >= range.SurfaceCount)
            return false;

        int surfaceIndex = range.FirstSurfaceIndex + surfaceOffset;
        if (IsCompactSingleLayer(ref blob))
            return TryGetCompactSurface(ref blob, surfaceIndex, out sample);

        if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[surfaceIndex];
        return true;
    }

    public static bool TryGetSurfaceByIndex(ref MapSurfaceBlob blob, int surfaceIndex, out MapSurfaceSample sample)
    {
        sample = default;
        if (IsCompactSingleLayer(ref blob))
            return TryGetCompactSurface(ref blob, surfaceIndex, out sample);

        if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[surfaceIndex];
        return true;
    }

    public static bool TryGetPrimarySurface(ref MapSurfaceBlob blob, int2 cell, out MapSurfaceSample sample)
    {
        sample = default;
        return TryGetSurfaceRange(ref blob, cell, out MapSurfaceCellSurfaceRange range) &&
               TryGetSurface(ref blob, range, 0, out sample);
    }

    public static bool TryGetSurfaceById(ref MapSurfaceBlob blob, int surfaceId, out MapSurfaceSample sample)
    {
        sample = default;
        if (surfaceId < 0)
            return false;

        if (IsCompactSingleLayer(ref blob))
            return TryGetCompactSurface(ref blob, surfaceId, out sample);

        for (int i = 0; i < blob.Samples.Length; i++)
        {
            MapSurfaceSample candidate = blob.Samples[i];
            if (candidate.SurfaceId != surfaceId)
                continue;

            sample = candidate;
            return true;
        }

        return false;
    }

    public static bool TryGetConnection(ref MapSurfaceBlob blob, int connectionIndex, out MapSurfaceConnection connection)
    {
        connection = default;
        if ((uint)connectionIndex >= (uint)blob.Connections.Length)
            return false;

        connection = blob.Connections[connectionIndex];
        return true;
    }

    public static bool IsLayered(ref MapSurfaceBlob blob)
    {
        return CountLayeredCells(ref blob) > 0;
    }

    public static int CountLayeredCells(ref MapSurfaceBlob blob)
    {
        if (IsCompactSingleLayer(ref blob))
            return 0;

        int count = 0;
        for (int i = 0; i < blob.Cells.Length; i++)
        {
            if (blob.Cells[i].SurfaceCount > 1)
                count++;
        }

        return count;
    }

    private static bool TryGetCompactSurface(ref MapSurfaceBlob blob, int surfaceIndex, out MapSurfaceSample sample)
    {
        sample = default;
        if ((uint)surfaceIndex >= (uint)blob.CompactSamples.Length ||
            blob.Dimensions.x <= 0)
        {
            return false;
        }

        MapSurfaceCompactSample compact = blob.CompactSamples[surfaceIndex];
        float3 normal = new(
            UnpackNormalByte(compact.NormalX),
            UnpackNormalByte(compact.NormalY),
            UnpackNormalByte(compact.NormalZ));
        normal = math.normalizesafe(normal, new float3(0f, 1f, 0f));

        sample = new MapSurfaceSample
        {
            Cell = new int2(surfaceIndex % blob.Dimensions.x, surfaceIndex / blob.Dimensions.x),
            SurfaceId = surfaceIndex,
            LayerId = compact.LayerId,
            Height = blob.CompactMinHeight + compact.PackedHeight * blob.CompactHeightStep,
            Normal = normal,
            SlopeDegrees = CalculateSlopeDegrees(normal),
            SurfaceType = compact.SurfaceType,
            MovementMask = compact.MovementMask,
            Flags = compact.Flags,
            FirstConnectionIndex = 0,
            ConnectionCount = 0
        };
        return true;
    }

    private static float UnpackNormalByte(sbyte value)
    {
        return math.clamp(value / (float)sbyte.MaxValue, -1f, 1f);
    }

    private static float CalculateSlopeDegrees(float3 normal)
    {
        float y = math.clamp(math.normalizesafe(normal, new float3(0f, 1f, 0f)).y, -1f, 1f);
        return math.degrees(math.acos(y));
    }
}
