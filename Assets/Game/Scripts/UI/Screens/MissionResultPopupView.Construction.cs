using Game.UI.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        private TMP_Text[] constructionLabels;
        private string[] originalConstructionLabels;

        private void ApplyConstructionOutcome(in UiMissionResultPopupModel model)
        {
            if (constructionLabels == null)
            {
                constructionLabels = new[] { RowLabel(objectivePatrolStatusText),
                    RowLabel(objectiveSquadStatusText), RowLabel(enemiesDefeatedText) };
                originalConstructionLabels = new string[constructionLabels.Length];
                for (int i = 0; i < constructionLabels.Length; i++)
                    if (constructionLabels[i] != null)
                        originalConstructionLabels[i] = constructionLabels[i].GetComponent<V3LocalizedTextBindingView>()?.EnglishFallback
                            ?? constructionLabels[i].text;
            }
            // The same result view can be reused after another campaign entry.
            for (int i = 0; i < constructionLabels.Length; i++)
                SetText(constructionLabels[i], originalConstructionLabels[i]);
            if (!model.Construction.Applicable) return;
            SetText(missionStatusText, model.Outcome == UiMissionResultOutcome.Victory ? "MISSION COMPLETE" : "MISSION FAILED");
            missionIdentityText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(19,30);
            // Campaign reward order differs from the old fixed credit/XP artwork.
            if (victoryRewardIconsRoot != null) victoryRewardIconsRoot.SetActive(false);

            SetDefenseText(constructionLabels[0], UiShellRuntimeGateway.Localization.Get("mission.m02.objective.build_forward_barracks"));
            SetDefenseText(constructionLabels[1], UiShellRuntimeGateway.Localization.Get("mission.m02.objective.produce_rifle_squad"));
            SetDefenseText(constructionLabels[2], UiShellRuntimeGateway.Localization.Get("mission.m02.result.squads_recruited"));
            SetText(objectivePatrolStatusText, model.Construction.BuildingsCompleted > 0 ? "COMPLETE" : "FAILED");
            SetText(objectiveSquadStatusText, model.Construction.SquadsRecruited > 0 ? "COMPLETE" : "FAILED");
            SetText(enemiesDefeatedText, model.Construction.SquadsRecruited.ToString());
            SetText(civilianLostText, model.Construction.CivilianLosses.ToString());
            SetText(objectiveCivilianStatusText, model.Construction.CivilianLosses == 0 ? "STABLE" : "AT RISK");
            if (objectivePatrolStatusText != null) objectivePatrolStatusText.color = model.Construction.BuildingsCompleted > 0
                ? new Color32(102,190,45,255) : new Color32(232,58,31,255);
            if (objectiveSquadStatusText != null) objectiveSquadStatusText.color = model.Construction.SquadsRecruited > 0
                ? new Color32(102,190,45,255) : new Color32(232,58,31,255);
            if (objectiveCivilianStatusText != null) objectiveCivilianStatusText.color = model.Construction.CivilianLosses == 0
                ? new Color32(102,190,45,255) : new Color32(242,140,20,255);
        }

        private static TMP_Text RowLabel(TMP_Text value) =>
            value != null ? value.transform.parent.Find("Label")?.GetComponent<TMP_Text>() : null;
    }
}
