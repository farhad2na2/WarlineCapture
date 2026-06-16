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
    public Entity SourceEntity;
    public int2 CenterCell;
    public float3 CenterWorld;
    public int RadiusCells;
    public int RevealedCount;
    public byte HasSourceEntity;
}

public struct ScanIntelCommandQueueComponent : IComponentData
{
    public int LastRequestId;
}

public struct ScanIntelCommandRequestElement : IBufferElementData
{
    public int RequestId;
    public int Frame;
    public Entity SourceEntity;
    public int2 CenterCell;
    public float3 CenterWorld;
    public int RadiusCells;
    public byte HasWorldPosition;
    public byte HasSourceEntity;
    public byte DeferRevealUntilSourceArrives;
}

public struct ScanIntelCommandResultElement : IBufferElementData
{
    public int RequestId;
    public int Frame;
    public Entity SourceEntity;
    public int2 CenterCell;
    public float3 CenterWorld;
    public int RadiusCells;
    public int RevealedCount;
    public int ReasonCode;
    public byte Accepted;
    public byte HasWorldPosition;
    public byte HasSourceEntity;
    public byte DeferredToSource;
}

public struct UnitScanOrder : IComponentData
{
    public int RequestId;
    public int StartedFrame;
    public Entity SourceEntity;
    public int2 CenterCell;
    public float3 CenterWorld;
    public int RadiusCells;
    public float StartedTimeSeconds;
    public float NextRevealTimeSeconds;
    public float NextPatrolMoveTimeSeconds;
    public float DurationSeconds;
    public int PatrolWaypointIndex;
    public byte EngageDetectedTargets;
    public byte ReturnHomeAfterCompletion;
    public byte HasStarted;
}
