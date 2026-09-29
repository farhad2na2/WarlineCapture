namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSplitFrontLauncher(out Game.UI.Contracts.UiSplitFrontLauncherModel model)
        {model=default;return current is Game.UI.Contracts.IUiSplitFrontGateway gateway&&gateway.TryReadSplitFrontLauncher(out model);}
        public static bool IsSplitFrontGuideContext()=>TryReadMissionHudRestrictions(out var mission)&&mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SplitFront||TryReadCampaignOperations(out var campaign)&&campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SplitFront;
    }
}
