using Unity.Collections;
using Unity.Entities;

public enum MatchStartStatusKind : byte
{
    None = 0,
    Queued = 1,
    WaitingForMatchLoaded = 2,
    Started = 3,
    Failed = 4
}

public struct MatchStartBoundaryComponent : IComponentData
{
}

public struct MatchStartQueueComponent : IComponentData
{
    public int LastRequestId;
    public int ActiveRequestId;
    public byte IsStartPending;
    public byte HasStarted;
    public MatchStartStatusKind LastStatus;
}

public struct MatchStartRequestElement : IBufferElementData
{
    public int RequestId;
    public byte RequireMatchLoaded;
}

public struct MatchStartResultElement : IBufferElementData
{
    public int RequestId;
    public MatchStartStatusKind Status;
    public FixedString128Bytes Message;
}
