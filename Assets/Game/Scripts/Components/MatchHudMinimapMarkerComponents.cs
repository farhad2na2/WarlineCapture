using Unity.Entities;
using Unity.Mathematics;

public struct MatchHudMinimapMarkerStateComponent : IComponentData
{
}

public struct MatchHudMinimapMarkerElement : IBufferElementData
{
    public float3 Position;
    public byte FactionId;
}
