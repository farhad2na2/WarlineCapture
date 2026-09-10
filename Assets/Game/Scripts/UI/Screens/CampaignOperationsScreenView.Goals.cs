using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        [SerializeField] private TMP_Text[] objectiveCards, starGoalLabels;
        private void ApplyMissionGoals(string missionId)
        {
            bool m3 = missionId == "saga.ch01.m03.radar_warning";
            bool m4 = missionId == "saga.ch01.m04.airlift";
            if(m3 || m4)
            {
                string prefix = m3 ? "mission.m03." : "mission.m04.";
                string[] objectives = m3 ? new[] { "stop_convoy", "protect_post", "prevent_breach" } : new[] { "extract", "transport", "landing" };
                string[] stars = m3 ? new[] { "complete", "civilians_safe", "post_undamaged" } : new[] { "1", "2", "3" };
                for(int i = 0; i < 3; i++)
                {
                    SetGoal(objectiveCards, i, UiShellRuntimeGateway.Localization.Get(prefix + "objective." + objectives[i]));
                    SetGoal(starGoalLabels, i, UiShellRuntimeGateway.Localization.Get(prefix + "star." + stars[i]));
                }
            }
            else
            {
                // Reset every card when switching back from a later mission.
                bool m2 = missionId == "saga.ch01.m02.establish_base";
                SetGoal(objectiveCards, 0, primaryObjectiveText != null ? primaryObjectiveText.text : "");
                SetGoal(objectiveCards, 1, UiShellRuntimeGateway.Localization.GetBySource(m2 ? "PRODUCE\nSQUAD" : "PROTECT CIVILIANS"));
                SetGoal(objectiveCards, 2, UiShellRuntimeGateway.Localization.GetBySource(m2 ? "HOLD\nPERIMETER" : "KEEP SQUAD SAFE"));
                SetGoal(starGoalLabels, 0, UiShellRuntimeGateway.Localization.GetBySource("COMPLETE MISSION"));
                SetGoal(starGoalLabels, 1, UiShellRuntimeGateway.Localization.GetBySource("NO UNIT LOSSES"));
                SetGoal(starGoalLabels, 2, UiShellRuntimeGateway.Localization.GetBySource("UNDER 15:00"));
            }
        }

        private static void SetGoal(TMP_Text[] labels, int index, string text)
        {
            if(labels == null || index >= labels.Length || labels[index] == null) return;
            labels[index].enableAutoSizing = true;
            labels[index].fontSizeMin = 10; labels[index].fontSizeMax = 14;
            labels[index].text = text;
        }
    }
}
