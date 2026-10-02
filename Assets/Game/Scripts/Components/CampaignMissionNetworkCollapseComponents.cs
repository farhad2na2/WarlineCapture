using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public enum NetworkCollapseFailure : byte { None, Integrity, EngineerLost, CarrierLost, CivicLost, AuditLost, StaffLost, EscortLost, FuelReserveLost, UnverifiedNodeDestroyed, Deadline }
    public enum NetworkCollapseMemberKind : byte { Escort, Engineer, Carrier, NodeOne, NodeTwo, NodeThree, GuardOne, GuardTwo, GuardThree, CivicStaff }
    public struct CampaignMissionNetworkCollapseState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity Engineer, Carrier, Audit, CivicOne, CivicTwo, Reserve;
        public float3 ReconOne, ReconTwo, ReconThree, ApproachOne, ApproachTwo, ApproachThree, AuditGate, Extraction;
        public int ElapsedMilliseconds, VerificationMilliseconds, RecoveryMilliseconds, ExtractionMilliseconds;
        public byte Initialized, Ready, NodeOneVerified, NodeTwoVerified, NodeThreeVerified, NodeOneDisabled, NodeTwoDisabled, NodeThreeDisabled;
        public byte AuditRecovered, EngineerAboard, Extracted, CommsReady, MilitaryCleared;
        public NetworkCollapseFailure Failure;
    }
    [InternalBufferCapacity(24)]
    public struct CampaignMissionNetworkCollapseMember : IBufferElementData
    { public Entity Entity; public NetworkCollapseMemberKind Kind; public byte HealthInitialized, Dead; }
    [InternalBufferCapacity(4)]
    public struct CampaignMissionNetworkBuildingRequest : IBufferElementData
    { public int RequestId; public Entity Entity; public byte Bound; }
}
