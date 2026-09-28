using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct CampaignMissionSteelPushState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal, FuelSpawnRequestId;
        public Entity FuelReserve;
        public float StartingUsableFuel, LowestUsableFuel;
        public byte Initialized, FuelSpent;
    }
}
