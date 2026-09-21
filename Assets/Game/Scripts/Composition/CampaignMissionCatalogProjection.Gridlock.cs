using Game.Components;
using Game.Configs;
using Unity.Collections;
namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionGridlockDefinitionBlob ProjectGridlock(MissionGridlockDefinitionConfig source)
        {
            if (!source.Enabled) return default;
            return new CampaignMissionGridlockDefinitionBlob {
                Enabled=1,
                ObstructionAAnchorId=new FixedString64Bytes(source.ObstructionAAnchorId),
                ObstructionBAnchorId=new FixedString64Bytes(source.ObstructionBAnchorId),
                SiteAAnchorId=new FixedString64Bytes(source.SiteAAnchorId),
                SiteBAnchorId=new FixedString64Bytes(source.SiteBAnchorId),
                HospitalAnchorId=new FixedString64Bytes(source.HospitalAnchorId),
                FadiRoleId=new FixedString64Bytes(source.FadiRoleId),
                WorkerRoleId=new FixedString64Bytes(source.WorkerRoleId),
                VehicleRoleId=new FixedString64Bytes(source.VehicleRoleId),
                CounterattackRoleId=new FixedString64Bytes(source.CounterattackRoleId),
                ObstructionBuildingId=new FixedString128Bytes(source.ObstructionBuildingId),
                WorkMilliseconds=source.WorkMilliseconds,
                HoldMilliseconds=source.HoldMilliseconds,
                DeadlineMilliseconds=source.DeadlineMilliseconds,
                WarningMilliseconds=source.WarningMilliseconds,
                WorkRadius=source.WorkRadius,
                ThreatRadius=source.ThreatRadius,
                HospitalRadius=source.HospitalRadius };
        }
        private static bool MatchesGridlock(CampaignMissionGridlockDefinitionBlob value, MissionGridlockDefinitionConfig source)
        {
            if(value.Enabled!=Flag(source.Enabled)) return false;
            if(!source.Enabled) return true;
            return Matches(value.ObstructionAAnchorId,source.ObstructionAAnchorId) && Matches(value.ObstructionBAnchorId,source.ObstructionBAnchorId) && value.SiteAAnchorId.Equals(new FixedString64Bytes(source.SiteAAnchorId)) &&
                value.SiteBAnchorId.Equals(new FixedString64Bytes(source.SiteBAnchorId)) &&
                value.HospitalAnchorId.Equals(new FixedString64Bytes(source.HospitalAnchorId)) &&
                value.FadiRoleId.Equals(new FixedString64Bytes(source.FadiRoleId)) &&
                value.WorkerRoleId.Equals(new FixedString64Bytes(source.WorkerRoleId)) &&
                value.VehicleRoleId.Equals(new FixedString64Bytes(source.VehicleRoleId)) &&
                value.CounterattackRoleId.Equals(new FixedString64Bytes(source.CounterattackRoleId)) &&
                value.ObstructionBuildingId.Equals(new FixedString128Bytes(source.ObstructionBuildingId)) &&
                value.WorkMilliseconds==source.WorkMilliseconds &&
                value.HoldMilliseconds==source.HoldMilliseconds &&
                value.DeadlineMilliseconds==source.DeadlineMilliseconds &&
                value.WarningMilliseconds==source.WarningMilliseconds &&
                value.WorkRadius==source.WorkRadius &&
                value.ThreatRadius==source.ThreatRadius &&
                value.HospitalRadius==source.HospitalRadius;
        }
    }
}
