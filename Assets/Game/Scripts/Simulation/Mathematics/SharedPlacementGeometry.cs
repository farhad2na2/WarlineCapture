using System;
using Unity.Mathematics;

namespace Game.Simulation
{
    public static class SharedPlacementGeometry
    {
        public readonly struct Bounds
        {
            public readonly float3 Center;
            public readonly float3 Extents;

            public Bounds(float3 center, float3 extents)
            {
                Center = center;
                Extents = extents;
            }
        }

        public static Bounds TransformBounds(float4x4 matrix, float3 localCenter, float3 localExtents)
        {
            return new Bounds(
                math.transform(matrix, localCenter),
                math.abs(matrix.c0.xyz) * localExtents.x +
                math.abs(matrix.c1.xyz) * localExtents.y +
                math.abs(matrix.c2.xyz) * localExtents.z);
        }

        public static bool ShouldReservePackedBounds(bool intactBuilding, bool solidScenery, float verticalExtent)
        {
            return intactBuilding || solidScenery && verticalExtent >= .5f;
        }

        public static void Reserve(bool[] mask, int width, int height, float cellSize,
            float3 gridOrigin, float3 center, float3 extents)
        {
            if (mask == null || width < 1 || height < 1 || (long)width * height != mask.Length ||
                !(cellSize > 0f) || !math.isfinite(cellSize))
                throw new ArgumentException("Complete finite placement grid required");

            // Preserve the existing one metre gap and conservative rotated bounds.
            int2 min = math.max(0, (int2)math.floor((center.xz - extents.xz - 1f - gridOrigin.xz) / cellSize));
            int2 max = math.min(new int2(width, height),
                (int2)math.ceil((center.xz + extents.xz + 1f - gridOrigin.xz) / cellSize));
            for (int y = min.y; y < max.y; y++)
                for (int x = min.x; x < max.x; x++) mask[y * width + x] = true;
        }
    }
}
