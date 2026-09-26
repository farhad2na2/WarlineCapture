using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum MarketLifelineFailure : byte { None, SquadLost, ConvoyLost, WrongTransfer, Deadline, Integrity }
    public struct CampaignMissionMarketLifelineDefinitionBlob
    {
        public byte Enabled;
        public FixedString64Bytes DeliveryAnchorId,ManifestAnchorId,CorruptManifestAnchorId;
        public int RequiredDeliveries,ManifestHoldMilliseconds,VictoryHoldMilliseconds,DeadlineMilliseconds;
        public float DeliveryRadius,ManifestRadius;
        public MissionCameraTourBlob CameraTour;
    }
    public struct CampaignMissionMarketLifelineState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal,ElapsedMilliseconds,ManifestHoldMilliseconds,CorruptManifestHoldMilliseconds,VictoryHoldMilliseconds;
        public uint SourceVersion;
        public int2 DeliveryCell,ManifestCell,CorruptManifestCell;
        public byte Ready,DeliveredCount,LegitimateManifestInspected,CorruptManifestInspected,ManifestVerified,Complete;
        public MarketLifelineFailure Failure;
    }
    [InternalBufferCapacity(24)]
    public struct CampaignMissionMarketLifelineMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 rifle, 1 legitimate relief convoy, 2 hostile, 3 corrupt transfer truck
        public byte Initialized,Dead,Delivered;
    }
}
