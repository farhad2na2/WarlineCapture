using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum SkirmishEnemyCapabilityKind : byte { Recon, Radar, CombatAir, Transport }
    public enum SkirmishEnemyCapabilityPhase : byte { Ready, Investigating, Scanning, Engaging, Boarding, Transit, Unloading, Returning, Cooldown, Aborting, Failed }
    // Separate versioned schema. All timers use the saved objective elapsed clock,
    // not Unity/world time. Existing native cargo/air/production remain authoritative.
    public struct SkirmishEnemyCapabilityStateV1Component : IComponentData
    {
        public byte Version;
        public uint NextOperationId;
    }
    public struct SkirmishEnemyCapabilityOperationV1 : IBufferElementData
    {
        public uint OperationId, GroupId, ReservationId;
        public SkirmishRoleKind Role;
        public SkirmishEnemyCapabilityKind Kind;
        public SkirmishEnemyCapabilityPhase Phase;
        public FixedString64Bytes SourceStableId, ProducerStableId, TargetStableId, PublicAnchorId;
        public float DestinationX, DestinationZ, StartedAt, LastIssuedAt, RetryAt, NextSurveyAt;
        public int ScanRequestId;
        public ushort LastReason;
        public byte Retries, CommandAccepted, LastObservedAirborne;
    }
    // Planned whole-group cargo; never a replacement native seat/weight ledger.
    public struct SkirmishEnemyTransportPlanV1 : IBufferElementData
    {
        public uint OperationId, PassengerGroupId;
        public FixedString64Bytes PassengerStableId;
    }
}
