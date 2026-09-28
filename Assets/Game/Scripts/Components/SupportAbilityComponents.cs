using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum SupportAbilityKind : byte { None, Smoke, Strike, Paratroopers, Supply }
    public enum SupportRequestSource : byte { Player, Aria }
    public enum SupportTargetKind : byte { Ground, Entity, LandingZone }
    public enum SupportExecutionPhase : byte { Reserved, Approaching, Released, Resolved, Aborted, PartiallyResolved }
    public enum SupportRejectionReason : ushort
    {
        None, WrongAttempt, NotActive, NotUnlocked, MissionRestricted, NotReady, NoCharges, Cooldown,
        InvalidGround, NotVisible, NotConfirmed, ProtectedTarget, InvalidTargetType, TargetGone,
        NoSafeAirRoute, LandingBlocked, PopulationFull, InsufficientFuel, StalePreview, ConsentRequired,
        ConsentExpired, AlreadyProcessed
    }
    public struct SupportFuelAvailabilityComponent : IComponentData { public float Total; public uint Version; }
    // Optional authored encounter scope. Required with a missing storage fails closed.
    public struct SupportFuelScopeComponent : IComponentData { public Entity Storage; public byte Required; }
    public struct SupportInputStateComponent : IComponentData { public byte Phase; public SupportAbilityKind Selected; public uint Version; public Entity Collector; }
    public struct SupportCatalogComponent : IComponentData
    { public BlobAssetReference<SupportCatalogBlob> Blob; public uint Revision; public byte OwnsBlob; }
    public struct SupportCatalogBlob { public BlobArray<SupportAbilityDefinition> Abilities; }
    public struct SupportAbilityDefinition
    {
        public SupportAbilityKind Kind; public FixedString64Bytes Id;
        public int Charges, FuelCost, Damage, Materials;
        public float CooldownSeconds, Radius, DurationSeconds, ApproachSeconds;
        public ushort DirectDamagePermille; public byte ProductionReady;
        public Entity VisualPrefab;
        public FixedString64Bytes AircraftSourceKey;
    }
    public struct SupportSessionComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint MissionSourceVersion;
        public byte FactionId, Active, TestEncounter; public uint CatalogRevision, NextRequestId;
        public uint LastPlayerRequestId, LastAriaRequestId; public double SimulationSeconds;
    }
    public struct SupportMissionPolicyComponent : IComponentData
    {
        public byte AllowedMask, OwnedMask, TestGrantMask; public SupportAbilityKind LessonKind; public int PopulationCeiling; public float2 GroundMin, GroundMax;
    }
    [InternalBufferCapacity(25)]
    public struct SupportMissionPolicyElement : IBufferElementData { public FixedString64Bytes MissionPrefix; public byte AllowedMask; public SupportAbilityKind LessonKind; public int PopulationCeiling; }
    [InternalBufferCapacity(8)]
    public struct SupportGroundRegionElement : IBufferElementData
    { public float2 Min, Max; public byte Visible, Protected; public uint Version; }
    [InternalBufferCapacity(4)]
    public struct SupportAbilityStateElement : IBufferElementData
    { public SupportAbilityKind Kind; public int ChargesRemaining; public double CooldownUntil; public uint StateVersion; public byte Enabled; }
    [InternalBufferCapacity(4)]
    public struct SupportRequestElement : IBufferElementData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint RequestId;
        public SupportAbilityKind Kind; public SupportRequestSource Source; public SupportTargetKind TargetKind;
        public Entity Target; public float3 Position; public int2 Cell;
        public uint ExpectedAbilityVersion, PreviewId, ProposalId, ConsentVersion;
        public uint TargetKnowledgeVersion;
    }
    public struct SupportPreviewComponent : IComponentData
    {
        public uint PreviewId; public SupportRequestElement Request; public byte Valid;
        public SupportRejectionReason Reason; public int FuelCost; public float Radius;
    }
    [InternalBufferCapacity(8)]
    public struct SupportReceiptElement : IBufferElementData
    {
        public uint RequestId; public SupportRequestSource Source; public SupportAbilityKind Kind;
        public SupportExecutionPhase Phase; public SupportRejectionReason Reason;
        public int ReservedFuel, SpentFuel; public double PreviousCooldown;
        public Entity Effect; public byte EffectApplied, ReservationReleased; public double UpdatedAt;
    }
    [InternalBufferCapacity(4)]
    public struct SupportFuelReservationElement : ICleanupBufferElementData
    { public Entity SourceStorage; public float Amount; public uint RequestId; public byte Status; }
    public struct SupportProposalComponent : IComponentData
    {
        public uint ProposalId, ConsentVersion, CatalogRevision; public SupportRequestElement Request;
        public int FuelCost; public double ExpiresAt; public byte Approved, Consumed, Declined;
    }
}
