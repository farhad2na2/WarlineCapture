using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private void ApplyRouteReopenedOutcome(in UiMissionResultPopupModel model)
        {
            if(model.MissionId!=Game.Missions.Contracts.CampaignMissionSequence.RouteReopened)return;
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            titleText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(20,36);
            missionStatusText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,28);
            SetDefenseText(titleText,model.Title);
            SetDefenseText(missionStatusText,model.Title);
            var rows=new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText};
            for(int i=0;i<rows.Length;i++)
            {
                var label=RowLabel(rows[i]);
                label?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);
                SetDefenseText(label,UiShellRuntimeGateway.Localization.Get("mission.route_reopened.star."+(i+1)));
                rows[i]?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(16,20);
                SetDefenseText(rows[i],StarStatus(victory));
                if(rows[i]!=null)rows[i].color=victory?new Color32(102,190,45,255):new Color32(232,58,31,255);
            }
            SetDefenseText(starCountText,UiShellRuntimeGateway.Localization.Format("mission.m03.result.stars","",model.Stars));
            if(victoryRewardIconsRoot!=null)victoryRewardIconsRoot.SetActive(false);
        }
    }
}
