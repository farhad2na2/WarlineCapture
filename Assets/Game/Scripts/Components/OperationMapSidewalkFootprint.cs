using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // Gameplay exclusion survives render-slot recycling and offscreen culling.
    [InternalBufferCapacity(0)]
    public struct OperationMapSidewalkFootprint : IBufferElementData
    {
        public float4x4 WorldToLocal;
        public float3 LocalMin, LocalMax, WorldMin, WorldMax;
    }
}
