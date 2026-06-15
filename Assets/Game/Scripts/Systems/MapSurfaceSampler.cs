using Unity.Entities;
using Unity.Mathematics;

public sealed class MapSurfaceSampler
{
    private readonly MapSurfaceLayerAccess _layeredCellSystem = new();

    public readonly struct Context
    {
        public readonly MapSurfaceComponent Surface;

        public Context(MapSurfaceComponent surface)
        {
            Surface = surface;
        }

        public bool IsCreated => Surface.HasSurfaceData != 0 && Surface.SurfaceBlob.IsCreated;
    }

    public bool TryCreateContext(EntityManager entityManager, EntityQuery surfaceQuery, out Context context)
    {
        context = default;

        if (surfaceQuery.IsEmptyIgnoreFilter)
            return false;

        MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return false;

        context = new Context(surface);
        return true;
    }

    public bool TryGetCellIndex(Context context, int2 cell, out int index)
    {
        index = -1;

        if (!context.IsCreated ||
            (uint)cell.x >= (uint)context.Surface.Dimensions.x ||
            (uint)cell.y >= (uint)context.Surface.Dimensions.y)
        {
            return false;
        }

        index = cell.x + cell.y * context.Surface.Dimensions.x;
        return true;
    }

    public bool TryGetPrimarySurface(Context context, int2 cell, out MapSurfaceSample sample)
    {
        return _layeredCellSystem.TryGetPrimarySurface(context.Surface, cell, out sample);
    }

    public bool TryGetSurfaceRange(Context context, int2 cell, out MapSurfaceCellSurfaceRange range)
    {
        return _layeredCellSystem.TryGetSurfaceRange(context.Surface, cell, out range);
    }

    public bool TryGetSurfaceInRange(Context context, MapSurfaceCellSurfaceRange range, int surfaceOffset, out MapSurfaceSample sample)
    {
        return _layeredCellSystem.TryGetSurface(context.Surface, range, surfaceOffset, out sample);
    }

    public bool TryGetSurfaceById(Context context, int surfaceId, out MapSurfaceSample sample)
    {
        sample = default;

        if (!context.IsCreated)
            return false;

        ref MapSurfaceBlob blob = ref context.Surface.SurfaceBlob.Value;
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

    public bool TryGetNearestValidSurface(Context context, int2 cell, out MapSurfaceSample sample)
    {
        if (TryGetPrimarySurface(context, cell, out sample))
            return true;

        int2 dimensions = context.Surface.Dimensions;
        int2 clamped = math.clamp(cell, int2.zero, dimensions - new int2(1, 1));
        return TryGetPrimarySurface(context, clamped, out sample);
    }

    public bool TrySampleHeight(Context context, int2 cell, out float height)
    {
        height = 0f;

        if (!TryGetPrimarySurface(context, cell, out MapSurfaceSample sample))
            return false;

        height = sample.Height;
        return true;
    }

    public bool TrySampleNormal(Context context, int2 cell, out float3 normal)
    {
        normal = new float3(0f, 1f, 0f);

        if (!TryGetPrimarySurface(context, cell, out MapSurfaceSample sample))
            return false;

        normal = math.normalizesafe(sample.Normal, new float3(0f, 1f, 0f));
        return true;
    }

    public bool TrySampleSlope(Context context, int2 cell, out float slopeDegrees)
    {
        slopeDegrees = 0f;

        if (!TryGetPrimarySurface(context, cell, out MapSurfaceSample sample))
            return false;

        slopeDegrees = sample.SlopeDegrees;
        return true;
    }

    public bool TrySampleBilinearHeight(Context context, float3 worldPosition, out float height)
    {
        height = 0f;

        if (!context.IsCreated || context.Surface.CellSize <= 0f)
            return false;

        float2 local = new float2(
            (worldPosition.x - context.Surface.GridOrigin.x) / context.Surface.CellSize - 0.5f,
            (worldPosition.z - context.Surface.GridOrigin.z) / context.Surface.CellSize - 0.5f);
        int2 minCell = (int2)math.floor(local);
        float2 t = math.saturate(local - minCell);

        if (!TrySampleHeight(context, minCell, out float h00))
            return false;

        if (!TrySampleHeight(context, minCell + new int2(1, 0), out float h10))
            h10 = h00;
        if (!TrySampleHeight(context, minCell + new int2(0, 1), out float h01))
            h01 = h00;
        if (!TrySampleHeight(context, minCell + new int2(1, 1), out float h11))
            h11 = h00;

        float hx0 = math.lerp(h00, h10, t.x);
        float hx1 = math.lerp(h01, h11, t.x);
        height = math.lerp(hx0, hx1, t.y);
        return true;
    }

    public bool TrySampleBilinearNormal(Context context, float3 worldPosition, out float3 normal)
    {
        normal = new float3(0f, 1f, 0f);

        if (!context.IsCreated || context.Surface.CellSize <= 0f)
            return false;

        float2 local = new float2(
            (worldPosition.x - context.Surface.GridOrigin.x) / context.Surface.CellSize - 0.5f,
            (worldPosition.z - context.Surface.GridOrigin.z) / context.Surface.CellSize - 0.5f);
        int2 minCell = (int2)math.floor(local);
        float2 t = math.saturate(local - minCell);

        if (!TrySampleNormal(context, minCell, out float3 n00))
            return false;

        if (!TrySampleNormal(context, minCell + new int2(1, 0), out float3 n10))
            n10 = n00;
        if (!TrySampleNormal(context, minCell + new int2(0, 1), out float3 n01))
            n01 = n00;
        if (!TrySampleNormal(context, minCell + new int2(1, 1), out float3 n11))
            n11 = n00;

        float3 nx0 = math.lerp(n00, n10, t.x);
        float3 nx1 = math.lerp(n01, n11, t.x);
        normal = math.normalizesafe(math.lerp(nx0, nx1, t.y), new float3(0f, 1f, 0f));
        return true;
    }
}
