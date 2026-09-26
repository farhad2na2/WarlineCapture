namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsSignalTraceGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == "saga.ch03.m01.signal_trace" ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == "saga.ch03.m01.signal_trace";
    }
}
