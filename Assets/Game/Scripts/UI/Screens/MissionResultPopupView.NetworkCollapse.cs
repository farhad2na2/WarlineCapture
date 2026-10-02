using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private void ApplyNetworkCollapseOutcome(in UiMissionResultPopupModel model)
        {
            if (model.MissionId != Game.Missions.Contracts.CampaignMissionSequence.NetworkCollapse) return;
            const string prefix = "mission.network_collapse.";
            var localization = UiShellRuntimeGateway.Localization;
            bool victory = model.Outcome == UiMissionResultOutcome.Victory;
            bool hasActualResult = UiShellRuntimeGateway.TryReadNetworkCollapseResult(out var actual);
            defenseLayoutActive = true;
            if (outcomeBackdrop != null)
                outcomeBackdrop.texture = Resources.Load<Texture2D>("FutureMissionComics/CH05M03_NetworkCollapse_Debrief");
            if (defenseGuideButton != null) defenseGuideButton.gameObject.SetActive(false);
            if (defenseLabels != null) foreach (var label in defenseLabels)
            {
                string key = label.Key switch
                {
                    "mission.m03.result.objectives" => prefix + "result.objectives",
                    "mission.m03.objective.stop_convoy" => prefix + "result.nodes",
                    "mission.m03.result.post" => prefix + "result.audit",
                    "mission.m03.result.civilian_goal" => prefix + "result.extraction",
                    "mission.m03.result.squad_losses" => prefix + "result.escort_losses",
                    "mission.m03.result.civilians" => prefix + "result.staff_losses",
                    _ => label.Key
                };
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18, 27);
                SetDefenseText(label.Text, localization.Get(key));
            }
            SetDefenseText(objectivePatrolStatusText, localization.Get(prefix + "result." + (hasActualResult && actual.NodesSecured ? "secured" : "incomplete")));
            SetDefenseText(objectiveSquadStatusText, localization.Get(prefix + "result." + (hasActualResult && actual.AuditRecovered ? "verified" : "incomplete")));
            SetDefenseText(objectiveCivilianStatusText, localization.Get(prefix + "result." + (hasActualResult && actual.Extracted ? "safe" : "losses")));
            SetDefenseText(civilianLostText, model.CivilianLossCount.ToString());
            SetDefenseText(enemiesDefeatedText, model.EnemiesDefeatedText);
            SetDefenseText(summaryText, victory ? localization.Get(prefix + "result.success_short") : model.SummaryBody);
            summaryText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18, 27);
            titleText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(24, 40);
            missionStatusText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(22, 32);
            LayoutDefenseResult();
        }
    }
}
