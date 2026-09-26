using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum PowerRelayFailure : byte { None, SquadLost, EngineerLost, FamilyConvoyLost, FuelServiceLost, Deadline, Integrity }
    public struct CampaignMissionPowerRelayDefinitionBlob
    {
        public byte Enabled;
        public FixedString64Bytes ShortRouteAnchorId,SafeRouteAnchorId,ShelterAnchorId,RepairAnchorId;
        public int RepairHoldMilliseconds,VictoryHoldMilliseconds,DeadlineMilliseconds;
        public float RouteRadius,ShelterRadius,RepairRadius;
        public MissionCameraTourBlob CameraTour;
    }
    public struct CampaignMissionPowerRelayState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal,ElapsedMilliseconds,RepairHoldMilliseconds,VictoryHoldMilliseconds;
        public uint SourceVersion;
        public int2 ShortRouteCell,SafeRouteCell,ShelterCell,RepairCell;
        public byte Ready,SafeRouteConfirmed,FamiliesSheltered,FuelDelivered,PowerRestored,Complete,ExposedRouteUsed;
        public PowerRelayFailure Failure;
    }
    [InternalBufferCapacity(24)]
    public struct CampaignMissionPowerRelayMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 rifle, 1 engineer, 2 family convoy, 3 Fuel service, 4 hostile
        public byte Initialized,Dead;
    }
}
