using Unity.Entities;
using Unity.Mathematics;

public struct ScanIntelRevealedTag : IComponentData
{
}

public struct ScanIntelLastSeen : IComponentData
{
    public int2 Cell;
    public float3 Position;
    public int LastScanFrame;
    public byte FactionId;
}

public struct ScanIntelFeedQueueTag : IComponentData
{
}

public struct ScanIntelFeedEntry : IBufferElementData
{
    public int RequestId;
    public int Frame;
    public int2 CenterCell;
    public float3 CenterWorld;
    public int RadiusCells;
    public int RevealedCount;
}
