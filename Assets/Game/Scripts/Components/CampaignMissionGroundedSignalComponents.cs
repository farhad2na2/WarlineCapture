using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum GroundedSignalFailure : byte { None, Integrity, SpecialistLost, CarrierLost, AircraftLost, TerminalLost, Deadline }

    public struct CampaignMissionGroundedSignalState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity Relay, ProtectedTerminal;
        public float3 ApronCenter, HardwareCenter;
        public float ApronRadius, HardwareRadius;
        public int RecoveryHoldMilliseconds, ElapsedMilliseconds;
        public byte Initialized, Inserted, RelayDisabled, HardwareRecovered;
        public byte SpecialistsAboardCarrier;
        public byte RelayHealthInitialized, TerminalHealthInitialized;
        public GroundedSignalFailure Failure;
    }

    [InternalBufferCapacity(4)]
    public struct CampaignMissionGroundedSignalProtectedMember : IBufferElementData
    {
        public Entity Entity;
        public byte HealthInitialized;
    }

    [InternalBufferCapacity(2)]
    public struct CampaignMissionGroundedSignalSpecialist : IBufferElementData
    {
        public Entity Entity;
        public byte SawInsertionPlane, UnloadedAtApron;
    }
}
