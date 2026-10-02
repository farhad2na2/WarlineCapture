using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public enum TrustUnderFireFailure : byte { None, Integrity, NorthConvoyLost, SouthConvoyLost, ShelterLost, StaffLost, EngineerLost, EscortLost, FuelReserveLost, BroadcastLost, Deadline }
    public enum TrustUnderFireMemberKind : byte { NorthEscort, SouthEscort, NorthConvoy, SouthConvoy, Engineer, NorthHostile, SouthHostile, RelayHostile, NorthStaff, SouthStaff }
    public struct CampaignMissionTrustUnderFireState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity NorthConvoy, SouthConvoy, Engineer, NorthShelter, SouthShelter, Broadcast, Reserve;
        public float3 NorthCrossing, SouthCrossing, NorthArrival, SouthArrival, RelayGate, RelayApproach;
        public int ElapsedMilliseconds, NorthHoldMilliseconds, SouthHoldMilliseconds, VerificationMilliseconds, StableMilliseconds;
        public byte Initialized, Ready, NorthCleared, SouthCleared, RelayCleared, NorthCrossed, SouthCrossed;
        public byte NorthPassagePhase, SouthPassagePhase;
        public byte NorthArrived, SouthArrived, RelayApproached, RelayVerified, CommsReady, MilitaryCleared;
        public TrustUnderFireFailure Failure;
    }
    [InternalBufferCapacity(32)]
    public struct CampaignMissionTrustUnderFireMember : IBufferElementData
    { public Entity Entity; public TrustUnderFireMemberKind Kind; public byte HealthInitialized, Dead; }
    [InternalBufferCapacity(4)]
    public struct CampaignMissionTrustBuildingRequest : IBufferElementData
    { public int RequestId; public Entity Entity; public byte Bound; }
}
