namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsAirCorridorGuideContext()=>TryReadMissionHudRestrictions(out var mission)&&mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.AirCorridor||TryReadCampaignOperations(out var campaign)&&campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.AirCorridor;
    }
}
