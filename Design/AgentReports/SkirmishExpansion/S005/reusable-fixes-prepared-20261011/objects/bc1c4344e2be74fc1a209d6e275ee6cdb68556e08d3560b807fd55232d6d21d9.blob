using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    // Versioned separately: never alter the legacy SkirmishEnemyStrategyComponent layout.
    public struct SkirmishEnemyPlannerV1Component : IComponentData
    {
        public int PolicyVersion;
        public uint PolicyHash, RandomState, PlanRevision, DecisionCount, NextActionKey;
        public FixedString128Bytes PolicyFingerprint;
        public byte Personality, Initialized, ResponsePending;
        public SkirmishEnemyPlanKind CurrentPlan;
        public float NextDecisionAt, NextObservationAt, ResponseDueAt, NextScoutAt, OpeningUntil;
        public float LastDecisionAt, PlanStartedAt, LastHomeHealth, HomeThreatClearedAt;
        public uint LastObservationHash;
        public int CurrentScore;
    }

    public struct SkirmishEnemyGroupAssignmentV1 : IBufferElementData
    {
        public uint GroupId, Revision, ActionKey;
        public SkirmishEnemyDuty Duty;
        public SkirmishGroupOrderKind Order;
        public FixedString64Bytes RouteId, TargetStableId;
        public float DestinationX, DestinationZ, AssignedAt, LastProgressAt, RetryAfter;
        public float LastProgressX, LastProgressZ, LastIssuedAt;
        public int RouteWaypointIndex;
        public byte OrderAccepted, Retries;
    }

    public struct SkirmishEnemyProductionIntentV1Component : IComponentData
    {
        public byte Version;
        public SkirmishEnemyProductionIntentState State;
        public FixedString64Bytes ActionKey;
        public SkirmishRoleKind Role;
        public int RequestId, NextRequestId;
        public uint ReservationId;
        public int ExpectedMembers;
        public uint PlanRevision;
        public byte IdenticalRejectCount, RejectDomain;
        public ushort LastRejectCode;
        public FixedString64Bytes LastRejectField;
        public float RetryNotBeforeElapsed, LastRequestElapsed;
        public byte LastObservedReservationPhase;
        public FixedString64Bytes ProducerStableObjectId;
    }
}
