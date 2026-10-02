using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum CitywideAlertFailure : byte { None, Integrity, ClinicLost, UtilityLost, StaffLost, EngineerLost, ResponseLost, FuelReserveLost, ProducerLost, ServiceOutage, Deadline }
    public enum CitywideAlertMemberKind : byte { ClinicResponse, UtilityResponse, ClinicEngineer, UtilityEngineer, AirDefense, Radar, ClinicHostile, UtilityHostile, HostileAir, ClinicStaff, UtilityStaff }
    public struct CampaignMissionCitywideAlertState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity Clinic, Utility, ClinicProducer, UtilityProducer, ClinicReserve, UtilityReserve;
        public Entity ClinicEngineer, UtilityEngineer, AirDefense, Radar, Reinforcement;
        public float3 ClinicCenter, UtilityCenter, ClinicDefense, UtilityDefense, CoverageCenter, PerimeterCenter, ClinicRecoveryCenter, UtilityRecoveryCenter;
        public float ServiceRadius, CoverageRadius, PerimeterRadius;
        public int ElapsedMilliseconds, ClinicRecoveryMilliseconds, UtilityRecoveryMilliseconds;
        public int ClinicObstructionMilliseconds, UtilityObstructionMilliseconds, StableMilliseconds;
        public int ProducedBaseline, ClinicProducerRuntimeId, UtilityProducerRuntimeId;
        public byte Initialized, Ready, CoverageReady, ClinicThreatsCleared, UtilityThreatsCleared, AirCleared;
        public byte ClinicRecovered, UtilityRecovered, ReinforcementProduced, ReinforcementReady, CommsReady;
        public byte ClinicObstructed, UtilityObstructed, MilitaryCleared;
        public CitywideAlertFailure Failure;
    }
    [InternalBufferCapacity(32)]
    public struct CampaignMissionCitywideAlertMember : IBufferElementData
    {
        public Entity Entity;
        public CitywideAlertMemberKind Kind;
        public byte HealthInitialized, Dead;
    }
    [InternalBufferCapacity(6)]
    public struct CampaignMissionCitywideBuildingRequest : IBufferElementData
    {
        public byte Kind, Bound;
        public int RequestId;
        public Entity Entity;
    }
}
