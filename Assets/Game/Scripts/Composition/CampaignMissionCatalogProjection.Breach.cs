using Game.Components;
using Game.Configs;
using Unity.Collections;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionBreachDefinitionBlob ProjectBreach(MissionBreachDefinitionConfig source)
        {
            if(!source.Enabled) return default;
            return new CampaignMissionBreachDefinitionBlob {
                Enabled=1, GateBuildingId=new FixedString128Bytes(source.GateBuildingId), CoreBuildingId=new FixedString128Bytes(source.CoreBuildingId),
                ApproachAnchorId=new FixedString64Bytes(source.ApproachAnchorId), GateAnchorId=new FixedString64Bytes(source.GateAnchorId),
                CoreAnchorId=new FixedString64Bytes(source.CoreAnchorId), ArchiveAnchorId=new FixedString64Bytes(source.ArchiveAnchorId),
                SupportRoleId=new FixedString64Bytes(source.SupportRoleId), CounterattackRoleId=new FixedString64Bytes(source.CounterattackRoleId),
                GateHealth=source.GateHealth, CoreHealth=source.CoreHealth, SecureHoldMilliseconds=source.SecureHoldMilliseconds,
                DeadlineMilliseconds=source.DeadlineMilliseconds, CounterattackWarningMilliseconds=source.CounterattackWarningMilliseconds,
                ArchiveRadius=source.ArchiveRadius, CameraTour=ProjectCameraTour(source.CameraTour) };
        }
        private static bool MatchesBreach(CampaignMissionBreachDefinitionBlob value,MissionBreachDefinitionConfig source)
        {
            if(value.Enabled!=Flag(source.Enabled)) return false;
            if(!source.Enabled) return true;
            return value.GateBuildingId.Equals(new FixedString128Bytes(source.GateBuildingId)) && value.CoreBuildingId.Equals(new FixedString128Bytes(source.CoreBuildingId)) &&
                Matches(value.ApproachAnchorId,source.ApproachAnchorId) && Matches(value.GateAnchorId,source.GateAnchorId) &&
                Matches(value.CoreAnchorId,source.CoreAnchorId) && Matches(value.ArchiveAnchorId,source.ArchiveAnchorId) &&
                Matches(value.SupportRoleId,source.SupportRoleId) && Matches(value.CounterattackRoleId,source.CounterattackRoleId) &&
                value.GateHealth==source.GateHealth && value.CoreHealth==source.CoreHealth && value.SecureHoldMilliseconds==source.SecureHoldMilliseconds &&
                value.DeadlineMilliseconds==source.DeadlineMilliseconds && value.CounterattackWarningMilliseconds==source.CounterattackWarningMilliseconds &&
                value.ArchiveRadius==source.ArchiveRadius && MatchesCameraTour(value.CameraTour,source.CameraTour);
        }
    }
}
