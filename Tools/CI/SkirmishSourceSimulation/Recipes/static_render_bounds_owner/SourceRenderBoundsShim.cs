using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    // Narrow source-host representation of the exact fields consumed by the owner.
    public struct AABB
    {
        public float3 Center;
        public float3 Extents;
    }

    public struct WorldRenderBounds : IComponentData
    {
        public AABB Value;
    }
}
