using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        [SerializeField] private Texture m03MissionArt;
        private void ApplyRadarWarning(in UiMissionBriefingModel model)
        {
            Set(missionNumber, "M03");
            Set(screenSubtitle, _gameTextResolver.Get("mission.chapter.first_response", "CHAPTER I · FIRST RESPONSE"));
            Set(operationCodename, _gameTextResolver.Get("mission.m03.name", "RADAR WARNING"));
            Set(missionTitle, _gameTextResolver.Get("mission.m03.name", "RADAR WARNING"));
            Set(missionSummary, _gameTextResolver.Get("mission.m03.summary", "Hold the forward post against two convoy elements."));
            Set(locationLabel, _gameTextResolver.Get("mission.m03.location", "Western approach · JRC forward post"));
            Set(enemyIntelLabel, _gameTextResolver.Get("mission.m03.enemy_intel", "Scout report · composition unconfirmed"));
            if (missionArtImage != null) missionArtImage.texture = m03MissionArt;
            string[] objectives = { "stop_convoy", "hold_post", "keep_core_clear" };
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], i < objectives.Length
                    ? _gameTextResolver.Get("mission.m03.objective." + objectives[i], objectives[i])
                    : _gameTextResolver.Get("mission.m03.goal.civilians", "Bonus star: protect all four civilians"));
            SetAt(conditionNameLabels, 0, _gameTextResolver.Get("mission.m02.resources.label", "STARTING RESOURCES"));
            SetAt(conditionLabels, 0, _gameTextResolver.Get("mission.m03.resources", "50,000 Credits · 100 Materials"));
            SetAt(conditionNameLabels, 1, _gameTextResolver.Get("mission.m03.label.forces", "STARTING FORCES"));
            SetAt(conditionLabels, 1, _gameTextResolver.Get("mission.m03.access", "Two rifle squads · Ground Radar Tank"));
            SetAt(conditionNameLabels, 2, _gameTextResolver.Get("mission.m03.label.options", "DEFENSE CHOICES"));
            SetAt(conditionLabels, 2, _gameTextResolver.Get("mission.m03.options", "Tower, Barrier or rifle reinforcements"));
            // The existing compact reward surface has three rows; show both real unlocks together.
            SetAt(rewardLabels, 2, _gameTextResolver.Get("mission.m03.reward.unlocks", "TOWER + RADAR PING"));
            SetAt(rewardValues, 2, _gameTextResolver.Get("mission.reward.unlock", "UNLOCK"));
            for (int i = 3; i < (rewardRows?.Length ?? 0); i++)
                if (rewardRows[i] != null) rewardRows[i].gameObject.SetActive(false);
        }
    }
}
