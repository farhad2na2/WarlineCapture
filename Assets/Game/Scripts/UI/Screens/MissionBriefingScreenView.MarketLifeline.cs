using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture marketLifelineMissionArt;
        private void ApplyMarketLifeline(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH02 · M03");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.name"));Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=marketLifelineMissionArt;
            string[] names={"delivery","manifest","market"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.market_lifeline.objective."+names[i]:"mission.market_lifeline.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.forces"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.market_lifeline.deadline"));
        }
    }
}
