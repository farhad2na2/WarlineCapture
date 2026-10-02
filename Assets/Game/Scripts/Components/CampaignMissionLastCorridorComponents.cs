using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public enum LastCorridorFailure : byte { None, Integrity, EngineerLost, KeysLost, MedicineLost, FuelCargoLost, ReinforcementsLost, CivicLost, StaffLost, EscortLost, FuelReserveLost, ReceiverLost, Deadline }
    public enum LastCorridorMemberKind : byte { Escort, Engineer, KeyCarrier, MedicineCarrier, FuelCarrier, Reinforcement, Blockade, RouteGuard, CivicStaff, BrokenLink }
    public enum CorridorCargoKind : byte { None, Medicine, Fuel, Keys }
    public struct CampaignMissionLastCorridorState : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint SourceVersion;
        public Entity Engineer, KeyCarrier, MedicineCarrier, FuelCarrier, Civic, Reserve, Receiver, BrokenLink;
        public float3 RepairGate, MedicineGate, FuelGate, ReinforcementGate, EngineerPickup, KeyReceiver;
        public float3 MainEntry, MainMiddle, MainExit, AlternateEntry, AlternateMiddle, AlternateExit;
        public int ElapsedMilliseconds, RepairMilliseconds, MedicineMilliseconds, FuelMilliseconds, ReinforcementMilliseconds, KeyMilliseconds, DeliveredFuelBarrels;
        public byte Initialized, Ready, LinkRecovered, MedicineDelivered, FuelDelivered, ReinforcementsDelivered, EngineerReady, EngineerAboard, EngineerDelivered, KeysDelivered, MilitaryCleared, CommsReady;
        public byte MedicineRouteStep, FuelRouteStep, KeyRouteStep, FuelRoute, KeyRoute, CargoLoaded;
        public LastCorridorFailure Failure;
    }
    public struct CampaignMissionLastCorridorMember : IBufferElementData { public Entity Entity; public LastCorridorMemberKind Kind; public byte HealthInitialized, Dead; }
    public struct CampaignMissionCorridorBuildingRequest : IBufferElementData { public int RequestId; public Entity Entity; public byte Bound; }
    public struct CampaignMissionCorridorCargo : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint SourceVersion;
        public CorridorCargoKind Kind; public int Quantity; public byte Delivered;
    }
}
