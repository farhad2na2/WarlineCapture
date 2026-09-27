using Game.Configs;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture networkBreakMissionArt;

        private void ApplyNetworkBreak(in UiMissionBriefingModel model)
        {
            Set(screenSubtitle, GameLocalization.CurrentLocaleCode == "fa-IR" ? "فصل سوم · شبکهٔ پنهان" : "CHAPTER III - HIDDEN NETWORK");
            Set(missionNumber, "M05");
            Set(operationCodename, UiShellRuntimeGateway.Localization.Get("mission.network_break.name"));
            Set(missionTitle, UiShellRuntimeGateway.Localization.Get("mission.network_break.name"));
            Set(missionSummary, UiShellRuntimeGateway.Localization.Get("mission.network_break.summary"));
            Set(locationLabel, UiShellRuntimeGateway.Localization.Get("mission.network_break.location"));
            Set(enemyIntelLabel, UiShellRuntimeGateway.Localization.Get("mission.network_break.enemy_intel"));
            if (missionArtImage != null) missionArtImage.texture = networkBreakMissionArt;
            string[] objectives = {"breach", "disable", "archive"};
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], UiShellRuntimeGateway.Localization.Get(
                    i < 3 ? "mission.network_break.objective." + objectives[i] : "mission.network_break.star.3"));
            SetAt(conditionLabels, 0, UiShellRuntimeGateway.Localization.Get("mission.network_break.resources"));
            SetAt(conditionLabels, 1, UiShellRuntimeGateway.Localization.Get("mission.network_break.forces"));
            SetAt(conditionLabels, 2, UiShellRuntimeGateway.Localization.Get("mission.network_break.deadline"));
            Transform starGoals = enemyIntel != null ? enemyIntel.parent.Find("StarGoals") : null;
            for (int i = 0; i < 3; i++)
                Set(starGoals?.Find("Goal" + i)?.GetComponent<TMPro.TMP_Text>(),
                    UiShellRuntimeGateway.Localization.Get("mission.network_break.star." + (i + 1)));
        }
    }
}
