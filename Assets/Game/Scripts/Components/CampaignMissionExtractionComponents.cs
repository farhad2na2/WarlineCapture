using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct CampaignMissionExtractionDefinitionBlob
    {
        public byte Enabled, VehiclesSelfSupplied, AuthoredMapDefensesDormant;
        public FixedString64Bytes PassengerRoleId, CarrierRoleId, AircraftRoleId;
        public FixedString64Bytes RescueAnchorId, LandingAnchorId, DepartureAnchorId;
        public int RequiredPassengers, SecureHoldMilliseconds, DeadlineMilliseconds;
        public float LandingRadius, DepartureRadius;
        public MissionCameraTourBlob CameraTour;
    }

    public struct CampaignMissionExtractionState : IComponentData
    {
        public uint GuidanceCompletedMask;
        public byte UnloadedAtLanding;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public float3 LandingCenter, DepartureCenter;
        public Entity Carrier, Aircraft;
        public int SecureHoldMilliseconds;
        public byte Initialized, Ready, DepartureCleared, CarrierTransferComplete;
    }

    [InternalBufferCapacity(32)]
    public struct CampaignMissionExtractionMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 escort, 1 specialist, 2 APC, 3 helicopter, 4 hostile
        public byte HealthInitialized, Dead, RodeCarrier;
    }
}
