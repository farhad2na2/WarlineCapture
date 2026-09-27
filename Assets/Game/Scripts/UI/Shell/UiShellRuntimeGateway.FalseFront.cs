namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsFalseFrontGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.FalseFront ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.FalseFront;
    }
}
