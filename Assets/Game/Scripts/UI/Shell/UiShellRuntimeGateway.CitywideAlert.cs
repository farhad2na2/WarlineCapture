using Game.UI.Contracts;
namespace Game.UI.Runtime
{
 public static partial class UiShellRuntimeGateway
 {
  public static bool IsCitywideAlertGuideContext()=>TryReadMissionHudRestrictions(out var mission) && mission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.CitywideAlert || TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==Game.Missions.Contracts.CampaignMissionSequence.CitywideAlert;
  public static bool TryReadCitywideAlert(out UiCitywideAlertModel model){model=default;return current is IUiCitywideAlertGateway gateway && gateway.TryReadCitywideAlert(out model);}
 }
}
