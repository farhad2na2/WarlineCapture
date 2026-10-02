using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsTrustUnderFireGuideContext() =>
            TryReadMissionHudRestrictions(out var mission) && mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.TrustUnderFire ||
            TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.TrustUnderFire;
        public static bool TryReadTrustUnderFireResult(out UiTrustUnderFireResultModel model)
        { model=default; return current is IUiTrustUnderFireResultGateway gateway && gateway.TryReadTrustUnderFireResult(out model); }
        public static bool TryReadTrustUnderFire(out UiTrustUnderFireModel model)
        { model=default; return current is IUiTrustUnderFireGateway gateway && gateway.TryReadTrustUnderFire(out model); }
    }
}
