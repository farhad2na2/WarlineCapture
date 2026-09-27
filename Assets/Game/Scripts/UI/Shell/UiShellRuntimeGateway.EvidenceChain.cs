namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsEvidenceChainGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) &&
            mission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.EvidenceChain ||
            TryReadCampaignOperations(out var campaign) &&
            campaign.SelectedMission.MissionId == Game.Missions.Contracts.CampaignMissionSequence.EvidenceChain;
    }
}
