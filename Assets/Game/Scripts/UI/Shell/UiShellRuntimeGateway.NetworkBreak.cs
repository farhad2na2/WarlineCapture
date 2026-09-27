namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsNetworkBreakGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) &&
                mission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.NetworkBreak ||
            TryReadCampaignOperations(out var campaign) &&
                campaign.SelectedMission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.NetworkBreak;
    }
}
