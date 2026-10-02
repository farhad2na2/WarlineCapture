using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsArmorBreakGuideContext()=>TryReadMissionHudRestrictions(out var mission) && mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.ArmorBreak ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.ArmorBreak;
        public static bool TryReadArmorBreak(out UiArmorBreakModel model)
        {model=default;return current is IUiArmorBreakGateway gateway && gateway.TryReadArmorBreak(out model);}
    }
}
