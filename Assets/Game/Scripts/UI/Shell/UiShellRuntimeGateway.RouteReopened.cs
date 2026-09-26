namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsRouteReopenedGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == "saga.ch02.m05.route_reopened" ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == "saga.ch02.m05.route_reopened";
    }
}
