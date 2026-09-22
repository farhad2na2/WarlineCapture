using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    public struct CampaignMissionSupplyLineAllocationRequest : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public byte Pending;
    }
    public enum SupplyLineFailure : byte { None, SquadLost, HaulerLost, LinkLost, Deadline, Integrity }
    public struct CampaignMissionSupplyLineDefinitionBlob
    {
        public byte Enabled;
        public FixedString64Bytes OilAnchorId, RefineryAnchorId, StorageAnchorId, AlternateLaneAnchorId;
        public FixedString128Bytes OilBuildingId, RefineryBuildingId, StorageBuildingId;
        public int ReserveBarrels, CivilianReserveBarrels, HoldMilliseconds, DeadlineMilliseconds;
    }
    public struct CampaignMissionSupplyLineState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public int PreparationMilliseconds, ElapsedMilliseconds, HoldMilliseconds;
        public float StoredFuel, ObservedOil, ObservedRefinedFuel;
        public byte Ready, OilTransferred, FuelTransferred, Complete;
        public int AllocatedCivilianBarrels;
        public int2 AlternateLane;
        public byte RerouteOrdered, RouteRecovered;
        public SupplyLineFailure Failure;
    }
    [InternalBufferCapacity(3)]
    public struct CampaignMissionSupplyLineLink : IBufferElementData
    {
        public Entity Entity;
        public int2 Origin;
        public FixedString128Bytes BuildingId;
        public int SpawnRequestId, RuntimeBuildingId;
        public byte Initialized;
    }
    [InternalBufferCapacity(24)]
    public struct CampaignMissionSupplyLineMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 rifles, 1 Oil hauler, 2 Fuel hauler, 3 hostile
        public byte Initialized, Dead;
    }
}
