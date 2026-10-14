using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public enum SkirmishEnemyConstructionPhase : byte
    { Idle = 0, PaidPending = 1, AwaitingAdoption = 2, Delivered = 3, RefundPending = 4, Rejected = 5 }
    // Optional session-owned part. No world Entity handle or native request buffer crosses Continue.
    public struct SkirmishEnemyConstructionIntentV1Component : IComponentData
    {
        public byte Version, Phase, RejectionCount;
        public FixedString64Bytes SessionId, ActionKey, PadId, ResultStableObjectId, PreviousProducerStableId;
        public FixedString128Bytes BuildingId;
        public uint PlanRevision, FailedResearchId;
        public int RequestId, MaterialsPaid, DesiredCount, CandidateIndex;
        public int2 Origin, Footprint;
        public float RequestedAt, RetryAfter;
    }
}
