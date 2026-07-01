using Unity.Mathematics;

public readonly struct MapSurfaceLayerAccess
{
    public bool TryGetSurfaceRange(MapSurfaceComponent surface, int2 cell, out MapSurfaceCellSurfaceRange range)
    {
        range = default;

        if (surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            (uint)cell.x >= (uint)surface.Dimensions.x ||
            (uint)cell.y >= (uint)surface.Dimensions.y)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        return MapSurfaceBlobAccess.TryGetSurfaceRange(ref blob, cell, out range);
    }

    public bool TryGetSurface(MapSurfaceComponent surface, MapSurfaceCellSurfaceRange range, int surfaceOffset, out MapSurfaceSample sample)
    {
        sample = default;

        if (surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            surfaceOffset < 0 ||
            surfaceOffset >= range.SurfaceCount)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        return MapSurfaceBlobAccess.TryGetSurface(ref blob, range, surfaceOffset, out sample);
    }

    public bool TryGetPrimarySurface(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
    {
        sample = default;
        return TryGetSurfaceRange(surface, cell, out MapSurfaceCellSurfaceRange range) &&
               TryGetSurface(surface, range, 0, out sample);
    }
}
