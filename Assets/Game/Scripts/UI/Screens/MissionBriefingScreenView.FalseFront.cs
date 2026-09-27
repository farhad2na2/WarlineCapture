using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture falseFrontMissionArt;

        private void ApplyFalseFront(in UiMissionBriefingModel model)
        {
            Set(missionNumber, "CH03 · M03");
            Set(operationCodename, UiShellRuntimeGateway.Localization.Get("mission.false_front.name"));
            Set(missionTitle, UiShellRuntimeGateway.Localization.Get("mission.false_front.name"));
            Set(missionSummary, UiShellRuntimeGateway.Localization.Get("mission.false_front.summary"));
            Set(locationLabel, UiShellRuntimeGateway.Localization.Get("mission.false_front.location"));
            Set(enemyIntelLabel, UiShellRuntimeGateway.Localization.Get("mission.false_front.enemy_intel"));
            if (missionArtImage != null) missionArtImage.texture = falseFrontMissionArt;
            string[] names = {"verify", "evacuate", "ambush"};
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], UiShellRuntimeGateway.Localization.Get(i < 3 ? "mission.false_front.objective." + names[i] : "mission.false_front.star.3"));
            SetAt(conditionLabels, 0, UiShellRuntimeGateway.Localization.Get("mission.false_front.resources"));
            SetAt(conditionLabels, 1, UiShellRuntimeGateway.Localization.Get("mission.false_front.forces"));
            SetAt(conditionLabels, 2, UiShellRuntimeGateway.Localization.Get("mission.false_front.deadline"));
        }
    }
}
