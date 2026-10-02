using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsNetworkCollapseGuideContext()=>TryReadMissionHudRestrictions(out var m)&&m.MissionId==Game.Missions.Contracts.CampaignMissionSequence.NetworkCollapse||TryReadCampaignOperations(out var c)&&c.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.NetworkCollapse;
        public static bool TryReadNetworkCollapseResult(out UiNetworkCollapseResultModel model){model=default;return current is IUiNetworkCollapseResultGateway gateway&&gateway.TryReadNetworkCollapseResult(out model);}
        public static bool TryReadNetworkCollapse(out UiNetworkCollapseModel model){model=default;return current is IUiNetworkCollapseGateway gateway&&gateway.TryReadNetworkCollapse(out model);}
    }
}
