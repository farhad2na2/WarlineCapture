using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public struct SupportSmokeVisualConfigComponent : IComponentData { public UnityObjectRef<UnityEngine.GameObject> Prefab; }
    public struct SupportSmokeZoneComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal;
        public float3 Center; public float RadiusSquared; public double ExpiresAt; public ushort DirectDamagePermille;
    }
    public struct SupportRangedCoverComponent : IComponentData { public ushort DirectDamagePermille; }
    public struct SupportTargetEligibilityComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal;
        public byte CurrentlyVisible, HostileConfirmed, Protected, IsMilitaryTarget;
        public uint SourceVersion;
    }
    // Explicit mission-authored route. No default route is inferred from a target.
    public struct SupportAirRouteComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint Version;
        public float3 Entry, Release, Exit; public float Clearance; public byte Authored;
    }
    public struct SupportFlightComponent : IComponentData
    {
        public Entity Root,Payload; public SupportRequestElement Request;
        public SupportExecutionPhase Phase; public double StartedAt, ReleasedAt;
        public float ApproachSeconds, ExitSeconds; public float3 Entry, Release, Exit;
        public uint RouteVersion; public int Damage; public byte FactionId;
    }
    public struct SupportFlightCleanupComponent : ICleanupComponentData
    {public Entity Root,Payload;public SupportRequestElement Request;public byte Released;}
    public struct SupportPayloadRegistryComponent : IComponentData
    { public Entity InfantryPrefab,ParachutePrefab,CratePrefab;public int InfantryCount; }
    public struct SupportPayloadBindingsComponent : IComponentData
    { public Entity InfantryPrefab,ParachutePrefab,CratePrefab;public int InfantryCount; }
    public struct SupportPassengerTransitTag : IComponentData {}
    public struct SupportPassengerCleanupComponent : ICleanupComponentData {public Entity Canopy;}
    public struct SupportCollectionFeedbackComponent : IComponentData {public SupportRejectionReason Reason;}
    public struct SupportPassengerOwnerComponent : IComponentData
    {
        public Entity Root,Flight,Canopy;public FixedString64Bytes SessionToken;public int AttemptOrdinal;
        public int2 LandingCell;public float3 LandingPosition;public byte Released,Landed;
    }
    public struct SupportPassengerReservationElement : ICleanupBufferElementData
    { public Entity Passenger;public byte Released; }
    public struct SupportSupplyCrateComponent : IComponentData
    {
        public Entity Root,Flight,Canopy,Claimant;public FixedString64Bytes SessionToken;public int AttemptOrdinal,RemainingMaterials;
        public int2 LandingCell;public float3 LandingPosition;public byte FactionId,Released,Landed;
    }
    public struct SupportCollectOrderComponent : IComponentData {public Entity Crate;public int2 Goal;public double ExpiresAt;}
    public struct SupportCollectRequestElement : IBufferElementData {public Entity Collector,Crate;public FixedString64Bytes SessionToken;public int AttemptOrdinal;}
    public struct SupportSupplyReadModelComponent : IComponentData {public Entity Crate;public int Remaining;public byte Claimed,Full,Ready;}
}
