using Game.UI.Contracts;
namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool IsCommandNodeGuideContext()=>TryReadMissionHudRestrictions(out var m)&&m.MissionId==Game.Missions.Contracts.CampaignMissionSequence.CommandNode||TryReadCampaignOperations(out var c)&&c.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.CommandNode;
        public static bool TryReadCommandNodeResult(out UiCommandNodeResultModel model){model=default;return current is IUiCommandNodeResultGateway gateway&&gateway.TryReadCommandNodeResult(out model);}
        public static bool TryReadCommandNode(out UiCommandNodeModel model){model=default;return current is IUiCommandNodeGateway gateway&&gateway.TryReadCommandNode(out model);}
    }
}
