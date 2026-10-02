using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsLastCorridorGuideContext()=>TryReadMissionHudRestrictions(out var m)&&m.MissionId==Game.Missions.Contracts.CampaignMissionSequence.LastCorridor||TryReadCampaignOperations(out var c)&&c.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.LastCorridor;
        public static bool TryReadLastCorridorResult(out UiLastCorridorResultModel model){model=default;return current is IUiLastCorridorResultGateway gateway&&gateway.TryReadLastCorridorResult(out model);}
        public static bool TryReadLastCorridor(out UiLastCorridorModel model){model=default;return current is IUiLastCorridorGateway gateway&&gateway.TryReadLastCorridor(out model);}
    }
}
