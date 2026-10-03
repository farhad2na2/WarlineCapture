using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public enum CommandNodeFailure : byte { None, Integrity, EngineerLost, SpecialistsLost, ServicesLost, StaffLost, EscortLost, FuelReserveLost, UnisolatedNodeDestroyed, Deadline }
    public enum CommandNodeMemberKind : byte { Escort, Engineer, Specialist, Exterior, PerimeterNode, CoreGuard, Qassem, CivicStaff }
    public struct CampaignMissionCommandNodeState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity Engineer, Node, Qassem, Clinic, Utility, Audit, Reserve;
        public float3 IsolationClinic, IsolationUtility, BreachGate, CoreAudit, AuditRelease, SafeReceiving;
        public int ElapsedMilliseconds, IsolationMilliseconds, BreachMilliseconds, AuditMilliseconds, ReleaseMilliseconds, ReceivingMilliseconds, ReleaseOrderBaseline;
        public byte Initialized, Ready, ExteriorCleared, ClinicIsolated, UtilityIsolated, NodeDisabled, CoverReady, Breached, QassemDefeated, MilitaryCleared, AuditPreserved, ReleaseOrdered, AuditReleased, SpecialistsSafe, CommsReady;
        public CommandNodeFailure Failure;
    }
    [InternalBufferCapacity(23)]
    public struct CampaignMissionCommandNodeMember : IBufferElementData { public Entity Entity; public CommandNodeMemberKind Kind; public byte HealthInitialized, Dead; }
    [InternalBufferCapacity(4)]
    public struct CampaignMissionCommandBuildingRequest : IBufferElementData { public int RequestId; public Entity Entity; public byte Bound; }
}
