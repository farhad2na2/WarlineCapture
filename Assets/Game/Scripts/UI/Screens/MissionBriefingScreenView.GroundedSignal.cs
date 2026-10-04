using Game.UI.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private AuthoredText[] _groundedObjectiveRows;
        private AuthoredText[] _groundedObjectiveText;
        private bool _groundedBriefingLayoutApplied;

        private void ApplyGroundedSignal(in UiMissionBriefingModel model)
        {
            var localization = UiShellRuntimeGateway.Localization;
            const string prefix = "mission.grounded_signal.";
            Set(screenSubtitle, localization.IsRightToLeft ? "فصل چهارم · هوا و زره" : "CHAPTER IV - AIR AND ARMOR");
            Set(missionNumber, "M04");
            Set(operationCodename, localization.Get(prefix + "name"));
            Set(missionTitle, localization.Get(prefix + "name"));
            Set(missionSummary, localization.Get(prefix + "summary"));
            Set(locationLabel, localization.Get(prefix + "location"));
            Set(enemyIntelLabel, localization.Get(prefix + "enemy_intel"));
            if (missionArtImage != null)
                missionArtImage.texture = Resources.Load<Texture2D>("FutureMissionComics/CH04M04_GroundedSignal");

            string[] objectives = { "hardware", "extract", "terminal" };
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], i < objectives.Length ? localization.Get(prefix + "objective." + objectives[i]) : string.Empty);
            SetAt(conditionLabels, 0, localization.Get(prefix + "resources"));
            SetAt(conditionLabels, 1, localization.Get(prefix + "forces"));
            SetAt(conditionLabels, 2, localization.Get(prefix + "deadline"));
            SetAt(conditionNameLabels, 0, localization.Get(prefix + "label.transport"));
            SetAt(conditionNameLabels, 1, localization.Get("mission.m03.label.forces"));
            SetAt(conditionNameLabels, 2, localization.Get(prefix + "label.deadline"));
            SetAt(rewardLabels, 2, localization.Get("mission.reward.paratroopers"));
            SetAt(rewardValues, 2, localization.Get("mission.reward.unlock", "UNLOCK"));

            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Label")?.GetComponent<TMP_Text>(), localization.Get(prefix + "intel.relay"));
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Value")?.GetComponent<TMP_Text>(), localization.Get(prefix + "intel.present"));
            Set(enemyIntel?.Find("Row_AIR_THREAT/Label")?.GetComponent<TMP_Text>(), localization.Get(prefix + "intel.air"));
            var airThreat = enemyIntel?.Find("Row_AIR_THREAT/Value")?.GetComponent<TMP_Text>();
            Set(airThreat, localization.Get(prefix + "intel.none"));
            if (airThreat != null) airThreat.color = new Color32(160, 195, 65, 255);
            var goals = enemyIntel != null ? enemyIntel.parent.Find("StarGoals") : null;
            for (int i = 0; i < 3; i++)
                Set(goals?.Find("Goal" + i)?.GetComponent<TMP_Text>(), localization.Get(prefix + "star." + (i + 1)));
        }

        private void FitGroundedBriefingObjectives()
        {
            if (approvedInnerLayout) return;
            if (objectiveLabels == null) return;
            _groundedObjectiveRows ??= new AuthoredText[objectiveLabels.Length];
            _groundedObjectiveText ??= new AuthoredText[objectiveLabels.Length];
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            for (int i = 0; i < objectiveLabels.Length; i++)
            {
                var label = objectiveLabels[i];
                if (label == null) continue;
                var row = label.transform.parent as RectTransform;
                CaptureRow(ref _groundedObjectiveRows[i], ref _groundedObjectiveText[i], label);
                if (row == null) continue;
                row.gameObject.SetActive(i < 3);
                if (i >= 3) continue;
                // Three real objectives use the space previously occupied by four single-line rows.
                Place(_groundedObjectiveRows[i], row, 14f, 64f + i * 64f, PanelWidth(primaryObjectives, 375f) - 30f, 62f, 0f, false, TextAlignmentOptions.MidlineLeft);
                Place(_groundedObjectiveText[i], label, 48f, 0f, row.sizeDelta.x - 60f, 62f, rtl ? 14f : 18f, true,
                    rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            }
            _groundedBriefingLayoutApplied = true;
        }

        private void RestoreGroundedBriefingLayout()
        {
            if (!_groundedBriefingLayoutApplied || objectiveLabels == null) return;
            for (int i = 0; i < objectiveLabels.Length && i < _groundedObjectiveRows.Length; i++)
            {
                var label = objectiveLabels[i];
                if (label == null) continue;
                Restore(_groundedObjectiveRows[i], label.transform.parent as RectTransform);
                Restore(_groundedObjectiveText[i], label);
            }
            _groundedBriefingLayoutApplied = false;
        }
    }
}
