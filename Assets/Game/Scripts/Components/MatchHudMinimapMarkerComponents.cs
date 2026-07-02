using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct MatchHudMinimapMarkerStateComponent : IComponentData
    {
    }

    public struct MatchHudMinimapMarkerElement : IBufferElementData
    {
        public float3 Position;
        public byte FactionId;
    }
}
