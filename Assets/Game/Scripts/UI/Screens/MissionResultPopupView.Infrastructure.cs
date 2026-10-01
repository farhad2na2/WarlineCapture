using Game.UI.Contracts;
using Game.Missions.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private void ApplyInfrastructureOutcome(in UiMissionResultPopupModel model)
        {
            bool supply=model.MissionId==CampaignMissionSequence.SupplyLine;
            if(!supply && model.MissionId!=CampaignMissionSequence.PowerRelay)return;
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string prefix=supply?"mission.supply_line.objective.":"mission.power_relay.objective.";
            var objectives=supply?new[]{"oil","fuel","reserve"}:new[]{"shelter","restore","secure"};
            var rows=new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText};
            for(int i=0;i<rows.Length;i++)
            {
                var label=RowLabel(rows[i]);
                label?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(16,27);
                SetDefenseText(label,UiShellRuntimeGateway.Localization.Get(prefix+objectives[i]));
                rows[i]?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(16,20);
                SetDefenseText(rows[i],StarStatus(victory));
                if(rows[i]!=null)rows[i].color=victory?new Color32(102,190,45,255):new Color32(232,58,31,255);
            }
            titleText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(20,36);
            missionStatusText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,28);
            SetDefenseText(titleText,model.Title);
            SetDefenseText(missionStatusText,model.Title);
        }
    }
}
