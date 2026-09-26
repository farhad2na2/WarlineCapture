namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsPowerRelayGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId == "saga.ch02.m04.power_relay" ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId == "saga.ch02.m04.power_relay";
    }
}
