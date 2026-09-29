using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private void ApplySplitFrontOutcome(in UiMissionResultPopupModel model)
        {
            if(model.MissionId!=Game.Missions.Contracts.CampaignMissionSequence.SplitFront)return;
            if(outcomeBackdrop!=null)outcomeBackdrop.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M03_SplitFront_Debrief");
            if(defenseLabels!=null)foreach(var label in defenseLabels)
            {
                string key=label.Key switch{"mission.m03.objective.stop_convoy"=>"mission.split_front.objective.battery","mission.m03.result.post"=>"mission.split_front.objective.base","mission.m03.result.civilian_goal"=>"mission.split_front.objective.civilians","mission.m03.result.civilians"=>"mission.split_front.result.base","mission.m03.result.squad_losses"=>"mission.split_front.result.losses",_=>label.Key};
                SetDefenseText(label.Text,UiShellRuntimeGateway.Localization.Get(key));
            }
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            SetDefenseText(objectivePatrolStatusText,StarStatus(victory));SetDefenseText(objectiveSquadStatusText,StarStatus(victory&&!model.Defense.PostDestroyed));SetDefenseText(objectiveCivilianStatusText,StarStatus(victory&&!model.Defense.CoreBreached));
            civilianLostText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(16,27);
            if(civilianLostText!=null){civilianLostText.enableAutoSizing=true;civilianLostText.fontSizeMin=16;civilianLostText.fontSizeMax=27;}
            SetDefenseText(civilianLostText,UiShellRuntimeGateway.Localization.Get("mission.split_front.result."+(model.Defense.PostDestroyed?"destroyed":model.Defense.PostDamaged?"damaged":"intact")));
            summaryText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);
            if(summaryText!=null){summaryText.enableAutoSizing=true;summaryText.fontSizeMin=18;summaryText.fontSizeMax=27;}
            SetDefenseText(summaryText,model.SummaryBody);
        }
    }
}
