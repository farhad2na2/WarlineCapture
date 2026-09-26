using Game.Components;
using Game.Configs;
using Unity.Collections;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionMarketLifelineDefinitionBlob ProjectMarketLifeline(MissionMarketLifelineDefinitionConfig value)=>!value.Enabled?default:new CampaignMissionMarketLifelineDefinitionBlob
        {
            Enabled=1,DeliveryAnchorId=new FixedString64Bytes(value.DeliveryAnchorId),ManifestAnchorId=new FixedString64Bytes(value.ManifestAnchorId),
            RequiredDeliveries=value.RequiredDeliveries,ManifestHoldMilliseconds=value.ManifestHoldMilliseconds,VictoryHoldMilliseconds=value.VictoryHoldMilliseconds,
            DeadlineMilliseconds=value.DeadlineMilliseconds,DeliveryRadius=value.DeliveryRadius,ManifestRadius=value.ManifestRadius,CameraTour=ProjectCameraTour(value.CameraTour)
        };
        private static bool MatchesMarketLifeline(CampaignMissionMarketLifelineDefinitionBlob projected,MissionMarketLifelineDefinitionConfig source)=>
            projected.Enabled==Flag(source.Enabled) && (!source.Enabled || projected.DeliveryAnchorId.Equals(new FixedString64Bytes(source.DeliveryAnchorId)) &&
            projected.ManifestAnchorId.Equals(new FixedString64Bytes(source.ManifestAnchorId)) && projected.RequiredDeliveries==source.RequiredDeliveries &&
            projected.ManifestHoldMilliseconds==source.ManifestHoldMilliseconds && projected.VictoryHoldMilliseconds==source.VictoryHoldMilliseconds &&
            projected.DeadlineMilliseconds==source.DeadlineMilliseconds && projected.DeliveryRadius==source.DeliveryRadius && projected.ManifestRadius==source.ManifestRadius &&
            MatchesCameraTour(projected.CameraTour,source.CameraTour));
    }
}
