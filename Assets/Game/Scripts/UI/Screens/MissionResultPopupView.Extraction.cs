using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        [SerializeField]private Texture m04ResultBackdrop;
        private void ApplyExtractionOutcome(in UiMissionResultPopupModel model)
        {
            if(!model.Extraction.Applicable)return;
            defenseLayoutActive=true;if(defenseGuideButton!=null)defenseGuideButton.gameObject.SetActive(true);
            if(outcomeBackdrop!=null&&m04ResultBackdrop!=null)outcomeBackdrop.texture=m04ResultBackdrop;
            titleText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(24,48);
            missionNameText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(20,36);
            rewardsText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(17,22);
            if(defenseLabels!=null)foreach(var label in defenseLabels)
            {
                string key=label.Key switch {"mission.m03.result.objectives"=>"mission.m04.result.objectives","mission.m03.objective.stop_convoy"=>"mission.m04.result.extract","mission.m03.result.post"=>"mission.m04.result.transport",
                    "mission.m03.result.civilian_goal"=>"mission.m04.result.landing",_=>label.Key};
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);SetDefenseText(label.Text,UiShellRuntimeGateway.Localization.Get(key));
            }
            SetDefenseText(missionStatusText,model.Title);SetDefenseText(titleText,model.Title);SetDefenseText(missionNameText,model.Subtitle);
            SetDefenseText(missionIdentityText,BuildMissionIdentity(in model));SetDefenseText(summaryText,model.SummaryBody);
            SetDefenseText(starCountText,UiShellRuntimeGateway.Localization.Format("mission.m03.result.stars","",model.Stars));SetDefenseText(civilianLostText,model.Extraction.Losses.ToString());
            SetDefenseText(objectivePatrolStatusText,model.Extraction.Delivered+" / 4");
            SetDefenseText(objectiveSquadStatusText,UiShellRuntimeGateway.Localization.Get(model.Extraction.CarrierLost||model.Extraction.AircraftLost?"mission.m03.result.losses":"mission.m03.result.safe"));
            SetDefenseText(objectiveCivilianStatusText,UiShellRuntimeGateway.Localization.Get(model.Outcome==UiMissionResultOutcome.Victory?"mission.m03.result.safe":"mission.m03.result.incomplete"));
            foreach(var status in new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText})
                status?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,22);
            SetDefenseText(elapsedText,model.ElapsedText);SetDefenseText(squadLossText,model.SquadLossText);SetDefenseText(enemiesDefeatedText,model.EnemiesDefeatedText);
            SetDefenseText(primaryButtonLabel,model.PrimaryActionLabel);SetDefenseText(retryButtonLabel,model.PrimaryActionLabel);
            if(victoryRewardIconsRoot!=null)victoryRewardIconsRoot.SetActive(false);SetDefenseText(rewardsText,BuildRewardDisplay(model.RewardsText));LayoutDefenseResult();
        }
    }
}
