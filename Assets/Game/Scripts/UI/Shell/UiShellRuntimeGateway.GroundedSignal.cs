using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsGroundedSignalGuideContext()=>TryReadMissionHudRestrictions(out var mission) && mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.GroundedSignal ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.GroundedSignal;
        public static bool TryReadGroundedSignal(out UiGroundedSignalModel model)
        {model=default;return current is IUiGroundedSignalGateway gateway && gateway.TryReadGroundedSignal(out model);}
    }
}
