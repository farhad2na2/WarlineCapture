using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture routeReopenedMissionArt;
        private void ApplyRouteReopened(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH02 · M05");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.name"));Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=routeReopenedMissionArt;
            string[] names={"lifelines","link","hub","records"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<4?"mission.route_reopened.objective."+names[i]:"mission.route_reopened.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.forces"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.deadline"));
        }
    }
}
