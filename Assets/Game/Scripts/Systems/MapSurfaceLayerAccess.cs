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
        int index = cell.x + cell.y * surface.Dimensions.x;
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
        int surfaceIndex = range.FirstSurfaceIndex + surfaceOffset;
        if ((uint)surfaceIndex >= (uint)blob.Samples.Length)
            return false;

        sample = blob.Samples[surfaceIndex];
        return true;
    }

    public bool TryGetPrimarySurface(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
    {
        sample = default;
        return TryGetSurfaceRange(surface, cell, out MapSurfaceCellSurfaceRange range) &&
               TryGetSurface(surface, range, 0, out sample);
    }
}
