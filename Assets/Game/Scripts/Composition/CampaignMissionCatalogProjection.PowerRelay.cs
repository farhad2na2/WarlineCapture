using Game.Components;
using Game.Configs;
using Unity.Collections;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionPowerRelayDefinitionBlob ProjectPowerRelay(MissionPowerRelayDefinitionConfig value)=>!value.Enabled?default:new CampaignMissionPowerRelayDefinitionBlob
        {
            Enabled=1,ShortRouteAnchorId=new FixedString64Bytes(value.ShortRouteAnchorId),SafeRouteAnchorId=new FixedString64Bytes(value.SafeRouteAnchorId),
            ShelterAnchorId=new FixedString64Bytes(value.ShelterAnchorId),RepairAnchorId=new FixedString64Bytes(value.RepairAnchorId),
            RepairHoldMilliseconds=value.RepairHoldMilliseconds,VictoryHoldMilliseconds=value.VictoryHoldMilliseconds,DeadlineMilliseconds=value.DeadlineMilliseconds,
            RouteRadius=value.RouteRadius,ShelterRadius=value.ShelterRadius,RepairRadius=value.RepairRadius,CameraTour=ProjectCameraTour(value.CameraTour)
        };
        private static bool MatchesPowerRelay(CampaignMissionPowerRelayDefinitionBlob projected,MissionPowerRelayDefinitionConfig source)=>
            projected.Enabled==Flag(source.Enabled) && (!source.Enabled ||
            projected.ShortRouteAnchorId.Equals(new FixedString64Bytes(source.ShortRouteAnchorId)) &&
            projected.SafeRouteAnchorId.Equals(new FixedString64Bytes(source.SafeRouteAnchorId)) &&
            projected.ShelterAnchorId.Equals(new FixedString64Bytes(source.ShelterAnchorId)) &&
            projected.RepairAnchorId.Equals(new FixedString64Bytes(source.RepairAnchorId)) &&
            projected.RepairHoldMilliseconds==source.RepairHoldMilliseconds && projected.VictoryHoldMilliseconds==source.VictoryHoldMilliseconds &&
            projected.DeadlineMilliseconds==source.DeadlineMilliseconds && projected.RouteRadius==source.RouteRadius &&
            projected.ShelterRadius==source.ShelterRadius && projected.RepairRadius==source.RepairRadius && MatchesCameraTour(projected.CameraTour,source.CameraTour));
    }
}
