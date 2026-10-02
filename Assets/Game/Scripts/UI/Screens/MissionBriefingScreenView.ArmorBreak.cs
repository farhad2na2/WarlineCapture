using Game.UI.Contracts;
using TMPro;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private void ApplyArmorBreak(in UiMissionBriefingModel model)
        {
            var localization = UiShellRuntimeGateway.Localization;
            const string prefix = "mission.armor_break.";
            Set(screenSubtitle, localization.IsRightToLeft ? "فصل چهارم · هوا و زره" : "CHAPTER IV - AIR AND ARMOR");
            Set(missionNumber, "M05");
            Set(operationCodename, localization.Get(prefix + "name"));
            Set(missionTitle, localization.Get(prefix + "name"));
            Set(missionSummary, localization.Get(prefix + "summary"));
            Set(locationLabel, localization.Get(prefix + "location"));
            Set(enemyIntelLabel, localization.Get(prefix + "enemy_intel"));
            if (missionArtImage != null)
                missionArtImage.texture = Resources.Load<Texture2D>("FutureMissionComics/CH04M05_ArmorBreak");

            string[] objectives = { "command", "authority", "relief" };
            for (int i = 0; i < (objectiveLabels?.Length ?? 0); i++)
                Set(objectiveLabels[i], i < objectives.Length ? localization.Get(prefix + "objective." + objectives[i]) : string.Empty);
            SetAt(conditionLabels, 0, localization.Get(prefix + "resources"));
            SetAt(conditionLabels, 1, localization.Get(prefix + "forces"));
            SetAt(conditionLabels, 2, localization.Get(prefix + "deadline"));
            SetAt(conditionNameLabels, 0, localization.Get(prefix + "label.fuel"));
            SetAt(conditionNameLabels, 1, localization.Get("mission.m03.label.forces"));
            SetAt(conditionNameLabels, 2, localization.Get(prefix + "label.deadline"));
            SetAt(rewardLabels, 2, localization.IsRightToLeft ? "قطعهٔ چهارم پروتکل" : "PROTOCOL FRAGMENT 4");
            SetAt(rewardValues, 2, localization.Get("mission.reward.unlock", "UNLOCK"));

            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Label")?.GetComponent<TMP_Text>(), localization.Get(prefix + "intel.heavy"));
            var heavyPresence = enemyIntel?.Find("Row_LIGHT_VEHICLES/Value")?.GetComponent<TMP_Text>();
            heavyPresence?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(12, 20);
            Set(heavyPresence, localization.Get(prefix + "intel.present"));
            FitArmorBreakIntelValue(heavyPresence);
            Set(enemyIntel?.Find("Row_AIR_THREAT/Label")?.GetComponent<TMP_Text>(), localization.Get(prefix + "intel.air"));
            var airThreat = enemyIntel?.Find("Row_AIR_THREAT/Value")?.GetComponent<TMP_Text>();
            airThreat?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(12, 20);
            Set(airThreat, localization.Get(prefix + "intel.incoming"));
            FitArmorBreakIntelValue(airThreat);
            if (airThreat != null) airThreat.color = new Color32(255, 158, 45, 255);
            var goals = enemyIntel != null ? enemyIntel.parent.Find("StarGoals") : null;
            for (int i = 0; i < 3; i++)
                Set(goals?.Find("Goal" + i)?.GetComponent<TMP_Text>(), localization.Get(prefix + "star." + (i + 1)));
        }

        private static void FitArmorBreakIntelValue(TMP_Text value)
        {
            if (value == null) return;
            value.enableAutoSizing = true;
            value.fontSizeMin = 12;
            value.fontSizeMax = 20;
            value.textWrappingMode = TextWrappingModes.NoWrap;
        }

    }
}
