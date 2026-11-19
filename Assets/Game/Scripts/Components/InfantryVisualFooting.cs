using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    // World-space height added to this character's render models and far billboard.
    public struct InfantryVisualFooting : IComponentData
    {
        public float Lift;
    }

    [InternalBufferCapacity(2)]
    public struct RunwayRenderTriangle : IBufferElementData
    {
        public float3 A, B, C;
    }
}
