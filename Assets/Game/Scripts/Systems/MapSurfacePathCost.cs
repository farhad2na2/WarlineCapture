using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
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
            if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out MapSurfaceSample sample))
                return 0;

            float slope = math.max(0f, sample.SlopeDegrees);
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
}
