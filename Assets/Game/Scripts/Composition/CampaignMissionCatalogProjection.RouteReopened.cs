using Game.Components;
using Game.Configs;
using Unity.Collections;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionRouteReopenedDefinitionBlob ProjectRouteReopened(MissionRouteReopenedDefinitionConfig value)=>!value.Enabled?default:new CampaignMissionRouteReopenedDefinitionBlob
        {
            Enabled=1,ReliefGoalAnchorId=new FixedString64Bytes(value.ReliefGoalAnchorId),FuelGoalAnchorId=new FixedString64Bytes(value.FuelGoalAnchorId),
            DisruptedLinkAnchorId=new FixedString64Bytes(value.DisruptedLinkAnchorId),HubGateAnchorId=new FixedString64Bytes(value.HubGateAnchorId),RecordsAnchorId=new FixedString64Bytes(value.RecordsAnchorId),
            LinkRepairHoldMilliseconds=value.LinkRepairHoldMilliseconds,RecordsHoldMilliseconds=value.RecordsHoldMilliseconds,DeadlineMilliseconds=value.DeadlineMilliseconds,
            DeliveryRadius=value.DeliveryRadius,RepairRadius=value.RepairRadius,HubRadius=value.HubRadius,RecordsRadius=value.RecordsRadius,CameraTour=ProjectCameraTour(value.CameraTour)
        };
        private static bool MatchesRouteReopened(CampaignMissionRouteReopenedDefinitionBlob projected,MissionRouteReopenedDefinitionConfig source)=>
            projected.Enabled==Flag(source.Enabled) && (!source.Enabled ||
            projected.ReliefGoalAnchorId.Equals(new FixedString64Bytes(source.ReliefGoalAnchorId)) && projected.FuelGoalAnchorId.Equals(new FixedString64Bytes(source.FuelGoalAnchorId)) &&
            projected.DisruptedLinkAnchorId.Equals(new FixedString64Bytes(source.DisruptedLinkAnchorId)) && projected.HubGateAnchorId.Equals(new FixedString64Bytes(source.HubGateAnchorId)) &&
            projected.RecordsAnchorId.Equals(new FixedString64Bytes(source.RecordsAnchorId)) && projected.LinkRepairHoldMilliseconds==source.LinkRepairHoldMilliseconds &&
            projected.RecordsHoldMilliseconds==source.RecordsHoldMilliseconds && projected.DeadlineMilliseconds==source.DeadlineMilliseconds &&
            projected.DeliveryRadius==source.DeliveryRadius && projected.RepairRadius==source.RepairRadius && projected.HubRadius==source.HubRadius &&
            projected.RecordsRadius==source.RecordsRadius && MatchesCameraTour(projected.CameraTour,source.CameraTour));
    }
}
