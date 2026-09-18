using Game.Components;
using Game.Rendering.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // The authored canal planes are visual geometry, not road or navigation cells.
    // Reserve their actual transformed mesh footprint when constructing in skirmish.
    internal static class SkirmishWaterPlacement
    {
        internal static bool[] CreateMask(EntityManager em, GridConfig grid)
        {
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.IsEmptyIgnoreFilter) return null;
            var mask = new bool[grid.Width * grid.Height];
            var footprints = new NativeList<RenderSurfaceFootprint>(Allocator.Temp);
            try
            {
                RenderBoundsGateway.Provider?.CollectWaterFootprints(em, ref footprints);
                foreach (var footprint in footprints)
                {
                    var local = footprint.LocalBounds;
                    var bounds = footprint.WorldBounds;
                    int2 min = math.max(0, (int2)math.floor((((float3)bounds.min).xz - grid.Origin.xz) / grid.CellSize));
                    int2 max = math.min(new int2(grid.Width, grid.Height), (int2)math.ceil((((float3)bounds.max).xz - grid.Origin.xz) / grid.CellSize));
                    var inverse = (float4x4)footprint.WorldToLocal;
                    for (int y = min.y; y < max.y; y++)
                        for (int x = min.x; x < max.x; x++)
                        {
                            var point = math.transform(inverse, new float3(grid.Origin.x + (x + .5f) * grid.CellSize,
                                bounds.center.y, grid.Origin.z + (y + .5f) * grid.CellSize));
                            if (point.x >= local.min.x && point.x <= local.max.x && point.z >= local.min.z && point.z <= local.max.z)
                                mask[y * grid.Width + x] = true;
                        }
                }
            }
            finally { footprints.Dispose(); }
            return mask;
        }
    }
}
