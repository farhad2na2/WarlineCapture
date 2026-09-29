namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsSteelPushGuideContext()=>TryReadMissionHudRestrictions(out var mission)&&mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SteelPush||TryReadCampaignOperations(out var campaign)&&campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.SteelPush;
        public static bool IsDefensePreparationGuideContext()=>IsAirCorridorGuideContext()||IsSteelPushGuideContext()||IsSplitFrontGuideContext();
    }
}
