using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private void ApplyArmorBreakOutcome(in UiMissionResultPopupModel model)
        {
            if (model.MissionId != Game.Missions.Contracts.CampaignMissionSequence.ArmorBreak) return;
            const string prefix = "mission.armor_break.";
            var localization = UiShellRuntimeGateway.Localization;
            bool victory = model.Outcome == UiMissionResultOutcome.Victory;
            defenseLayoutActive = true;
            if (outcomeBackdrop != null)
                outcomeBackdrop.texture = Resources.Load<Texture2D>("FutureMissionComics/CH04M05_ArmorBreak_Debrief");
            if (defenseGuideButton != null) defenseGuideButton.gameObject.SetActive(false);
            if (defenseLabels != null) foreach (var label in defenseLabels)
            {
                string key = label.Key switch
                {
                    "mission.m03.result.objectives" => "mission.m04.result.star_objectives",
                    "mission.m03.objective.stop_convoy" => prefix + "result.star.authority",
                    "mission.m03.result.post" => prefix + "star.2",
                    "mission.m03.result.civilian_goal" => prefix + "star.3",
                    "mission.m03.result.squad_losses" => prefix + "result.combat_losses",
                    "mission.m03.result.civilians" => prefix + "result.relief_losses",
                    _ => label.Key
                };
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18, 27);
                SetDefenseText(label.Text, localization.Get(key));
            }
            SetDefenseText(objectivePatrolStatusText, StarStatus(victory));
            SetDefenseText(objectiveSquadStatusText, StarStatus(victory && model.SquadLossText == "0"));
            SetDefenseText(objectiveCivilianStatusText, StarStatus(victory && model.CivilianLossCount == 0));
            SetDefenseText(civilianLostText, model.CivilianLossCount.ToString());
            SetDefenseText(enemiesDefeatedText, model.EnemiesDefeatedText);
            SetDefenseText(summaryText, victory ? localization.Get(prefix + "result.success_short") : model.SummaryBody);
            summaryText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18, 27);
            LayoutDefenseResult();
        }
    }
}
