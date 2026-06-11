using Unity.Entities;
using Unity.Mathematics;

public struct MatchHudMinimapMarkerBoundary : IComponentData
{
}

public struct MatchHudMinimapMarkerElement : IBufferElementData
{
    public float3 Position;
    public byte FactionId;
}
