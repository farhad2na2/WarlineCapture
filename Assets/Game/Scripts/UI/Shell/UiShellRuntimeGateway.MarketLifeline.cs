namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsMarketLifelineGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == "saga.ch02.m03.market_lifeline" ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == "saga.ch02.m03.market_lifeline";
    }
}
