namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsSafehouseSweepGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.SafehouseSweep ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.SafehouseSweep;
    }
}
