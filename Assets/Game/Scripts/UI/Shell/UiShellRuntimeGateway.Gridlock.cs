namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReplayGridlockChapter()=>current is Game.UI.Contracts.IUiGridlockStoryGateway gateway && gateway.TryReplayGridlockChapter();
        public static bool IsGridlockGuideContext()
        {
            if(TryReadMissionHudRestrictions(out var mission) && mission.MissionId=="saga.ch02.m01.gridlock")return true;
            return TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId=="saga.ch02.m01.gridlock";
        }
    }
}
