using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        [SerializeField]private Texture m04ResultBackdrop;
        private static string StarStatus(bool earned)=>UiShellRuntimeGateway.Localization.Get(earned?"mission.m04.result.earned":"mission.m04.result.missed");
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
                string key=label.Key switch {"mission.m03.result.objectives"=>"mission.m04.result.star_objectives","mission.m03.objective.stop_convoy"=>"mission.m04.result.star.complete","mission.m03.result.post"=>"mission.m04.result.star.escort",
                    "mission.m03.result.civilian_goal"=>"mission.m04.result.star.time","mission.m03.result.enemies"=>"mission.m04.result.rescued_stat",_=>label.Key};
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);SetDefenseText(label.Text,UiShellRuntimeGateway.Localization.Get(key));
            }
            SetDefenseText(missionStatusText,model.Title);SetDefenseText(titleText,model.Title);SetDefenseText(missionNameText,model.Subtitle);
            SetDefenseText(missionIdentityText,BuildMissionIdentity(in model));SetDefenseText(summaryText,model.SummaryBody);
            SetDefenseText(starCountText,UiShellRuntimeGateway.Localization.Format("mission.m03.result.stars","",model.Stars));SetDefenseText(civilianLostText,model.Extraction.Losses.ToString());
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            SetDefenseText(objectivePatrolStatusText,StarStatus(victory));
            SetDefenseText(objectiveSquadStatusText,StarStatus(victory && model.Extraction.EscortLosses==0));
            SetDefenseText(objectiveCivilianStatusText,StarStatus(victory && model.Extraction.ElapsedMilliseconds<=420000));
            foreach(var status in new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText})
                status?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,22);
            SetDefenseText(elapsedText,model.ElapsedText);SetDefenseText(squadLossText,model.SquadLossText);SetDefenseText(enemiesDefeatedText,model.Extraction.Delivered+" / 4");
            SetDefenseText(primaryButtonLabel,model.PrimaryActionLabel);SetDefenseText(retryButtonLabel,model.PrimaryActionLabel);
            if(victoryRewardIconsRoot!=null)victoryRewardIconsRoot.SetActive(false);SetDefenseText(rewardsText,BuildRewardDisplay(model.RewardsText));LayoutDefenseResult();
        }
    }
}
