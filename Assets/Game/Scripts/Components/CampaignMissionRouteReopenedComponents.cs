using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum RouteReopenedFailure : byte { None, SquadLost, EngineerLost, ReliefConvoyLost, FuelConvoyLost, RecordsLost, Deadline, Integrity }
    public struct CampaignMissionRouteReopenedDefinitionBlob
    {
        public byte Enabled;
        public FixedString64Bytes ReliefGoalAnchorId,FuelGoalAnchorId,DisruptedLinkAnchorId,HubGateAnchorId,RecordsAnchorId;
        public int LinkRepairHoldMilliseconds,RecordsHoldMilliseconds,DeadlineMilliseconds;
        public float DeliveryRadius,RepairRadius,HubRadius,RecordsRadius;
        public MissionCameraTourBlob CameraTour;
    }
    public struct CampaignMissionRouteReopenedState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal,ElapsedMilliseconds,LinkRepairHoldMilliseconds,RecordsHoldMilliseconds;
        public uint SourceVersion;
        public int2 ReliefGoalCell,FuelGoalCell,DisruptedLinkCell,HubGateCell,RecordsCell;
        public byte Ready,ReliefDelivered,FuelDelivered,LinkRestored,HubEntered,GarrisonCleared,RecordsPreserved,Complete,RelayNodeActivated;
        public RouteReopenedFailure Failure;
    }
    [InternalBufferCapacity(24)]
    public struct CampaignMissionRouteReopenedMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 rifle, 1 road engineer, 2 relief convoy, 3 Fuel convoy, 4 hostile
        public byte Initialized,Dead;
    }
}
