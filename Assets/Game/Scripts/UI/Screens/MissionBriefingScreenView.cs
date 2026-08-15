using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MissionBriefingScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RectTransform missionOverview;
        [SerializeField] private RectTransform primaryObjectives;
        [SerializeField] private RectTransform tacticalConditions;
        [SerializeField] private RectTransform enemyIntel;
        [SerializeField] private RectTransform chapterProgress;
        [SerializeField] private RectTransform rewards;
        [SerializeField] private RectTransform[] progressNodes;
        [SerializeField] private RawImage missionArtImage;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text missionTitle;
        [SerializeField] private TMP_Text missionSummary;
        [SerializeField] private TMP_Text locationLabel;
        [SerializeField] private TMP_Text[] objectiveLabels;
        [SerializeField] private TMP_Text[] conditionLabels;
        [SerializeField] private TMP_Text enemyIntelLabel;
        [SerializeField] private RectTransform[] rewardRows;
        [SerializeField] private TMP_Text[] rewardLabels;
        [SerializeField] private TMP_Text[] rewardValues;
        [SerializeField] private Toggle replayTutorialToggle;
        [SerializeField] private TMP_Text replayTutorialLabel;
        [SerializeField] private Button deployOperationButton;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RectTransform MissionOverview => missionOverview;
        public RectTransform PrimaryObjectives => primaryObjectives;
        public RectTransform TacticalConditions => tacticalConditions;
        public RectTransform EnemyIntel => enemyIntel;
        public RectTransform ChapterProgress => chapterProgress;
        public RectTransform Rewards => rewards;
        public RectTransform[] ProgressNodes => progressNodes;
        public RawImage MissionArtImage => missionArtImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text MissionTitle => missionTitle;
        public Button DeployOperationButton => deployOperationButton;
        public Toggle ReplayTutorialToggle => replayTutorialToggle;

        public void BindGameTextResolver(IGameTextResolver gameTextResolver)
        {
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        public void Apply(in UiMissionBriefingModel model)
        {
            if (!model.IsValid)
            {
                ApplyUnavailable();
                return;
            }

            string title = _gameTextResolver.Get(model.DisplayNameKey, MissionTitleFromId(model.MissionId));
            Set(missionTitle, title.ToUpperInvariant());
            Set(missionSummary, $"BRIEFING: {_gameTextResolver.Get(model.DisplaySummaryKey, "Secure the Old Market corridor and protect the civilian route.")}");
            Set(locationLabel, $"LOCATION: {_gameTextResolver.Get(model.LocationNameKey, "Old Market, Sahrin")}");
            for (int index = 0; index < (objectiveLabels?.Length ?? 0); index++)
                Set(objectiveLabels[index], index < model.Objectives.Length
                    ? FormatObjective(in model.Objectives[index], _gameTextResolver)
                    : string.Empty);
            if (conditionLabels != null && conditionLabels.Length > 0)
                Set(conditionLabels[0], Restriction(model.BuildingDisabled || model.ProductionDisabled));
            if (conditionLabels != null && conditionLabels.Length > 1)
                Set(conditionLabels[1], Restriction(model.EconomyDisabled || model.TransportDisabled || model.AirDisabled));
            Set(enemyIntelLabel, $"{model.HostileUnitCount} CONFIRMED");
            for (int index = 0; index < (rewardLabels?.Length ?? 0); index++)
            {
                bool visible = index < model.Rewards.Length;
                if (rewardRows != null && index < rewardRows.Length && rewardRows[index] != null)
                    rewardRows[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    Set(rewardLabels[index], string.Empty);
                    if (rewardValues != null && index < rewardValues.Length)
                        Set(rewardValues[index], string.Empty);
                    continue;
                }
                UiMissionRewardModel reward = model.Rewards[index];
                Set(rewardLabels[index], RewardLabel(in reward, _gameTextResolver));
                if (rewardValues != null && index < rewardValues.Length)
                    Set(rewardValues[index], $"+{reward.Amount:N0}");
            }

            if (replayTutorialToggle != null)
            {
                replayTutorialToggle.gameObject.SetActive(model.ReplayTutorialToggleVisible);
                replayTutorialToggle.SetIsOnWithoutNotify(model.ReplayTutorialEnabled);
                replayTutorialToggle.interactable = model.ReplayTutorialToggleVisible && !model.DeployQueued;
            }
            if (replayTutorialLabel != null)
                replayTutorialLabel.gameObject.SetActive(model.ReplayTutorialToggleVisible);
            if (deployOperationButton != null)
                deployOperationButton.interactable = !model.DeployQueued;
        }

        public void ApplyUnavailable()
        {
            Set(missionTitle, "MISSION DATA UNAVAILABLE");
            Set(missionSummary, "Validated mission authority is not available.");
            if (deployOperationButton != null) deployOperationButton.interactable = false;
            if (replayTutorialToggle != null) replayTutorialToggle.gameObject.SetActive(false);
            if (replayTutorialLabel != null) replayTutorialLabel.gameObject.SetActive(false);
        }

        private static string MissionTitleFromId(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId)) return "MISSION";
            int separator = missionId.LastIndexOf('.');
            string token = separator >= 0 ? missionId[(separator + 1)..] : missionId;
            return "MISSION 01 - " + token.Replace('_', ' ').ToUpperInvariant();
        }

        private static string FormatObjective(
            in UiMissionObjectiveModel objective,
            IGameTextResolver gameTextResolver)
        {
            string fallback = objective.Rule switch
            {
                UiMissionObjectiveRuleKind.DestroyMissionRole => $"DESTROY THE HOSTILE PATROL ({objective.RequiredCount})",
                UiMissionObjectiveRuleKind.ProtectMissionRole => "KEEP THE COMMAND SQUAD ALIVE",
                _ => "MISSION OBJECTIVE"
            };
            return gameTextResolver.Get(objective.DisplayTextKey, fallback).ToUpperInvariant();
        }

        private static string RewardLabel(
            in UiMissionRewardModel reward,
            IGameTextResolver gameTextResolver)
        {
            string fallback = reward.Kind != UiMissionRewardKind.None
                ? reward.Kind.ToString().ToUpperInvariant()
                : "COMMANDER XP";
            return gameTextResolver.Get(reward.DisplayTextKey, fallback).ToUpperInvariant();
        }

        private static string Restriction(bool disabled) => disabled ? "DISABLED" : "ENABLED";
        private static void Set(TMP_Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }
}
