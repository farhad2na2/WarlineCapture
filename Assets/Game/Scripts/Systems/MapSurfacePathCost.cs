using Unity.Mathematics;

public readonly struct MapSurfacePathCost
{
    public int GetSlopeTraversalCost(
        MapSurfaceComponent surface,
        byte hasSurfaceData,
        MapSurfacePathCostComponent pathCost,
        int2 cell)
    {
        if (hasSurfaceData == 0 || pathCost.EnableSlopeCost == 0)
            return 0;

        if (surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            (uint)cell.x >= (uint)surface.Dimensions.x ||
            (uint)cell.y >= (uint)surface.Dimensions.y)
        {
            return 0;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        int index = cell.x + cell.y * surface.Dimensions.x;
        if ((uint)index >= (uint)blob.Cells.Length)
            return 0;

        MapSurfaceCell surfaceCell = blob.Cells[index];
        if (surfaceCell.SurfaceCount == 0 || (uint)surfaceCell.FirstSurfaceIndex >= (uint)blob.Samples.Length)
            return 0;

        float slope = math.max(0f, blob.Samples[surfaceCell.FirstSurfaceIndex].SlopeDegrees);
        if (slope <= MapSurfaceSlopeClassifier.FlatSlopeDegrees)
            return 0;
        if (slope <= MapSurfaceSlopeClassifier.GentleSlopeDegrees)
            return math.max(0, pathCost.GentleSlopeTraversalCost);

        return math.max(0, pathCost.SteepSlopeTraversalCost);
    }

    public MapSurfacePathCostComponent CreateDisabledDefault()
    {
        return new MapSurfacePathCostComponent
        {
            EnableSlopeCost = 0,
            GentleSlopeTraversalCost = 0,
            SteepSlopeTraversalCost = 0
        };
    }
}
