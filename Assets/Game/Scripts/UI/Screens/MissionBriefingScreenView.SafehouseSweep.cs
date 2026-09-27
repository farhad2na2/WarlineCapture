using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture safehouseSweepMissionArt;
        private void ApplySafehouseSweep(in UiMissionBriefingModel model)
        {
            Set(missionNumber,"CH03 · M02");Set(operationCodename,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.name"));
            Set(missionTitle,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.name"));Set(missionSummary,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.summary"));
            Set(locationLabel,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.location"));Set(enemyIntelLabel,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=safehouseSweepMissionArt;
            string[] names={"confirm","protect","ledger"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],UiShellRuntimeGateway.Localization.Get(i<3?"mission.safehouse_sweep.objective."+names[i]:"mission.safehouse_sweep.star.3"));
            SetAt(conditionLabels,0,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.resources"));SetAt(conditionLabels,1,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.forces"));SetAt(conditionLabels,2,UiShellRuntimeGateway.Localization.Get("mission.safehouse_sweep.deadline"));
        }
    }
}
