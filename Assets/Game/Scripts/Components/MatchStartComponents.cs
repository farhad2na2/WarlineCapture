using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum MatchStartStatusKind : byte
    {
        None = 0,
        Queued = 1,
        WaitingForMatchLoaded = 2,
        WaitingForRuntimeContent = 3,
        Starting = 4,
        Started = 5,
        Failed = 6
    }

    public struct MatchStartStateComponent : IComponentData
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

    public struct MatchStartProgressComponent : IComponentData
    {
        public float Progress01;
        public FixedString64Bytes Status;
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
}
