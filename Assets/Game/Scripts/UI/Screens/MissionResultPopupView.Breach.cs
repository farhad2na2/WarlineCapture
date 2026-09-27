using Game.UI.Contracts;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        [SerializeField]private Texture m05ResultBackdrop;
        [SerializeField]private Texture networkBreakResultBackdrop;
        private void ApplyBreachOutcome(in UiMissionResultPopupModel model)
        {
            if(!model.Breach.Applicable)return;
            bool networkBreak=model.MissionId==Game.Missions.Contracts.CampaignMissionSequence.NetworkBreak;
            string prefix=networkBreak?"mission.network_break":"mission.m05";
            defenseLayoutActive=true;if(defenseGuideButton!=null)defenseGuideButton.gameObject.SetActive(true);
            if(outcomeBackdrop!=null){Texture backdrop=networkBreak?networkBreakResultBackdrop:m05ResultBackdrop;if(backdrop!=null)outcomeBackdrop.texture=backdrop;}
            titleText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(24,48);
            missionNameText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(20,36);
            rewardsText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(17,22);
            if(defenseLabels!=null)foreach(var label in defenseLabels)
            {
                string key=label.Key switch {"mission.m03.result.objectives"=>"mission.m04.result.star_objectives","mission.m03.objective.stop_convoy"=>prefix+".star.1","mission.m03.result.post"=>prefix+".star.2",
                    "mission.m03.result.civilian_goal"=>prefix+".star.3","mission.m03.result.enemies"=>"mission.m03.result.enemies","mission.m03.result.squad_losses"=>prefix+".result.unit_losses","mission.m03.result.civilians"=>prefix+".result.support",_=>label.Key};
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);SetDefenseText(label.Text,UiShellRuntimeGateway.Localization.Get(key));
            }
            SetDefenseText(missionStatusText,model.Outcome==UiMissionResultOutcome.Victory?UiShellRuntimeGateway.Localization.Get(prefix+".result.chapter"):model.Title);SetDefenseText(titleText,model.Title);SetDefenseText(missionNameText,model.Subtitle);
            SetDefenseText(missionIdentityText,BuildMissionIdentity(in model));SetDefenseText(summaryText,model.SummaryBody);
            SetDefenseText(starCountText,UiShellRuntimeGateway.Localization.Format("mission.m03.result.stars","",model.Stars));SetDefenseText(civilianLostText,UiShellRuntimeGateway.Localization.Get(prefix+(model.Breach.SupportLost?".result.lost":".result.survived")));
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            SetDefenseText(objectivePatrolStatusText,StarStatus(victory));
            SetDefenseText(objectiveSquadStatusText,StarStatus(victory && !model.Breach.SupportLost));
            SetDefenseText(objectiveCivilianStatusText,StarStatus(victory && model.Breach.ElapsedMilliseconds<540000));
            foreach(var status in new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText})
                status?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,22);
            SetDefenseText(elapsedText,model.ElapsedText);SetDefenseText(squadLossText,model.SquadLossText);SetDefenseText(enemiesDefeatedText,model.EnemiesDefeatedText);
            SetDefenseText(primaryButtonLabel,model.PrimaryActionLabel);SetDefenseText(retryButtonLabel,model.PrimaryActionLabel);
            if(victoryRewardIconsRoot!=null)victoryRewardIconsRoot.SetActive(false);SetDefenseText(rewardsText,BuildRewardDisplay(model.RewardsText));LayoutDefenseResult();
        }
    }
}
