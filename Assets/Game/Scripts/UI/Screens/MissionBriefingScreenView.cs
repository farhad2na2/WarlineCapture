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
        [SerializeField] private Texture m01MissionArt;
        [SerializeField] private Texture m02MissionArt;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text screenSubtitle;
        [SerializeField] private TMP_Text missionNumber;
        [SerializeField] private TMP_Text missionTitle;
        [SerializeField] private TMP_Text operationCodename;
        [SerializeField] private TMP_Text missionSummary;
        [SerializeField] private TMP_Text locationLabel;
        [SerializeField] private TMP_Text[] objectiveLabels;
        [SerializeField] private TMP_Text[] conditionLabels;
        [SerializeField] private TMP_Text[] conditionNameLabels;
        [SerializeField] private TMP_Text enemyIntelLabel;
        [SerializeField] private RectTransform[] rewardRows;
        [SerializeField] private TMP_Text[] rewardLabels;
        [SerializeField] private TMP_Text[] rewardValues;
        [SerializeField] private Toggle replayTutorialToggle;
        [SerializeField] private TMP_Text replayTutorialLabel;
        [SerializeField] private Button deployOperationButton;
        [SerializeField] private bool v3TargetLayout;
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
        public TMP_Text MissionNumber => missionNumber;
        public TMP_Text OperationCodename => operationCodename;
        public TMP_Text MissionSummary => missionSummary;
        public TMP_Text LocationLabel => locationLabel;
        public TMP_Text[] ObjectiveLabels => objectiveLabels;
        public TMP_Text[] ConditionLabels => conditionLabels;
        public TMP_Text[] ConditionNameLabels => conditionNameLabels;
        public TMP_Text EnemyIntelLabel => enemyIntelLabel;
        public TMP_Text[] RewardLabels => rewardLabels;
        public TMP_Text[] RewardValues => rewardValues;
        public Button DeployOperationButton => deployOperationButton;
        public Toggle ReplayTutorialToggle => replayTutorialToggle;
        public bool V3TargetLayout => v3TargetLayout;

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

            bool m02 = model.MissionId == UiCampaignMissionProjectionIds.M02;
            Set(screenTitle, _gameTextResolver.Get("mission.briefing.title", "MISSION BRIEFING"));
            Set(screenSubtitle, v3TargetLayout
                ? m02 ? "CHAPTER I - FIRST RESPONSE" : "CHAPTER I - FIRST RESPONSE"
                : m02 ? "FIRST RESPONSE  /  MISSION 02" : "FIRST RESPONSE  /  MISSION 01");
            Set(missionNumber, v3TargetLayout ? m02 ? "M02" : "M01" : m02 ? "MISSION 02" : "MISSION 01");
            Set(operationCodename, m02 ? "ESTABLISH THE BASE" : "FIRST CONTACT");
            if (missionArtImage != null)
                missionArtImage.texture = m02 ? m02MissionArt : m01MissionArt;
            string title = _gameTextResolver.Get(model.DisplayNameKey, MissionTitleFromId(model.MissionId));
            Set(missionTitle, title.ToUpperInvariant());
            string summaryFallback = v3TargetLayout && m02
                ? "Reopen the abandoned JRC forward post before the Ash Line reaches it. Establish a foothold and prepare for incoming threats."
                : SummaryFallback(model.MissionId);
            string summary = _gameTextResolver.Get(model.DisplaySummaryKey, summaryFallback);
            Set(missionSummary, v3TargetLayout ? summary : $"BRIEFING: {summary}");
            Set(locationLabel, $"LOCATION: {_gameTextResolver.Get(model.LocationNameKey, LocationFallback(model.MissionId))}");
            for (int index = 0; index < (objectiveLabels?.Length ?? 0); index++)
                Set(objectiveLabels[index], v3TargetLayout && m02
                    ? V3Objective(index, true, _gameTextResolver)
                    : index < model.Objectives.Length
                        ? FormatObjective(in model.Objectives[index], _gameTextResolver)
                        : string.Empty);
            ApplyConditions(in model, m02);
            string enemyIntelFallback = m02
                ? model.HostileUnitCount > 0
                    ? $"{model.HostileUnitCount} HOSTILES | DELAYED PATROL"
                    : "SECURED BUILD ZONE | NO HOSTILES"
                : $"{model.HostileUnitCount} CONFIRMED";
            Set(enemyIntelLabel, _gameTextResolver.Get(
                m02 ? "mission.m02.enemy_intel" : "mission.m01.enemy_intel",
                v3TargetLayout && m02 ? "TUTORIAL CELL" : enemyIntelFallback));
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
                if (v3TargetLayout)
                {
                    UiMissionRewardModel reward = model.Rewards[index];
                    string rewardKey = m02 ? index switch
                    {
                        0 => "mission.reward.commander_xp",
                        1 => "mission.reward.credits",
                        _ => "mission.m02.reward.barracks_unlock"
                    } : reward.DisplayTextKey;
                    string rewardFallback = m02 ? index switch
                    {
                        0 => "COMMANDER XP",
                        1 => "CREDITS",
                        _ => "BARRACK"
                    } : RewardLabel(in reward, _gameTextResolver);
                    Set(rewardLabels[index], _gameTextResolver.Get(rewardKey, rewardFallback).ToUpperInvariant());
                    if (rewardValues != null && index < rewardValues.Length)
                        Set(rewardValues[index], m02 && index == 2
                            ? _gameTextResolver.Get("mission.reward.unlock", "UNLOCK").ToUpperInvariant()
                            : $"+{reward.Amount:N0}");
                }
                else
                {
                    UiMissionRewardModel reward = model.Rewards[index];
                    Set(rewardLabels[index], RewardLabel(in reward, _gameTextResolver));
                    if (rewardValues != null && index < rewardValues.Length)
                        Set(rewardValues[index], $"+{reward.Amount:N0}");
                }
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
            if (missionId == UiCampaignMissionProjectionIds.M01) return "FIRST CONTACT";
            if (missionId == UiCampaignMissionProjectionIds.M02) return "ESTABLISH THE BASE";
            if (string.IsNullOrWhiteSpace(missionId)) return "MISSION";
            int separator = missionId.LastIndexOf('.');
            string token = separator >= 0 ? missionId[(separator + 1)..] : missionId;
            return token.Replace('_', ' ').ToUpperInvariant();
        }

        private static string FormatObjective(
            in UiMissionObjectiveModel objective,
            IGameTextResolver gameTextResolver)
        {
            string fallback = objective.Rule switch
            {
                UiMissionObjectiveRuleKind.DestroyMissionRole => $"DESTROY THE HOSTILE PATROL ({objective.RequiredCount})",
                UiMissionObjectiveRuleKind.ProtectMissionRole => "KEEP THE COMMAND SQUAD ALIVE",
                UiMissionObjectiveRuleKind.BuildStructure => "BUILD THE FORWARD BARRACKS",
                UiMissionObjectiveRuleKind.ProduceUnit => "PRODUCE ONE RIFLE SQUAD",
                UiMissionObjectiveRuleKind.DefendMissionRole => "DEFEND THE FORWARD POST",
                _ => "MISSION OBJECTIVE"
            };
            return gameTextResolver.Get(objective.DisplayTextKey, fallback).ToUpperInvariant();
        }

        private static string V3Objective(
            int index,
            bool m02,
            IGameTextResolver gameTextResolver)
        {
            if (!m02)
            {
                (string key, string fallback) = index switch
                {
                    0 => ("mission.m01.objective.secure_old_market", "SECURE OLD MARKET"),
                    1 => ("mission.m01.objective.protect_civilian_route", "PROTECT CIVILIAN ROUTE"),
                    2 => ("mission.m01.objective.defeat_hostile_patrol", "DEFEAT HOSTILE PATROL"),
                    _ => ("mission.m01.objective.keep_command_squad_alive", "KEEP COMMAND SQUAD ALIVE")
                };
                return gameTextResolver.Get(key, fallback).ToUpperInvariant();
            }

            (string m02Key, string m02Fallback) = index switch
            {
                0 => ("mission.m02.objective.restore_command_post", "RESTORE COMMAND POST"),
                1 => ("mission.m02.objective.build_forward_barracks", "BUILD BARRACK"),
                2 => ("mission.m02.objective.produce_rifle_squad", "PRODUCE RIFLE SQUAD"),
                _ => ("mission.m02.objective.defend_forward_post", "HOLD PERIMETER")
            };
            return gameTextResolver.Get(m02Key, m02Fallback).ToUpperInvariant();
        }

        private static string RewardLabel(
            in UiMissionRewardModel reward,
            IGameTextResolver gameTextResolver)
        {
            string fallback = reward.Kind != UiMissionRewardKind.None
                ? reward.Kind.ToString().ToUpperInvariant()
                : reward.RewardConfigId.Contains("production_unlock", StringComparison.Ordinal)
                    ? "BARRACKS UNLOCK"
                    : "COMMANDER XP";
            return gameTextResolver.Get(reward.DisplayTextKey, fallback).ToUpperInvariant();
        }

        private void ApplyConditions(in UiMissionBriefingModel model, bool m02)
        {
            if (v3TargetLayout)
            {
                SetAt(conditionNameLabels, 0, _gameTextResolver.Get(
                    "mission.condition.civilian_risk", "CIVILIAN RISK").ToUpperInvariant());
                SetAt(conditionLabels, 0, _gameTextResolver.Get(
                    m02 ? "mission.condition.risk.medium" : "mission.condition.risk.low",
                    m02 ? "MED" : "LOW").ToUpperInvariant());
                SetAt(conditionNameLabels, 1, _gameTextResolver.Get(
                    "mission.condition.intel_confidence", "INTEL CONFIDENCE").ToUpperInvariant());
                SetAt(conditionLabels, 1, _gameTextResolver.Get(
                    "mission.condition.confidence.high", "HIGH").ToUpperInvariant());
                SetAt(conditionNameLabels, 2, _gameTextResolver.Get(
                    "mission.condition.visibility", "VISIBILITY").ToUpperInvariant());
                SetAt(conditionLabels, 2, _gameTextResolver.Get(
                    "mission.condition.visibility.clear", "CLEAR").ToUpperInvariant());
                return;
            }
            if (m02)
            {
                SetAt(conditionNameLabels, 0, _gameTextResolver.Get(
                    "mission.m02.resources.label", "STARTING RESOURCES"));
                SetAt(conditionLabels, 0, _gameTextResolver.Get(
                    "mission.m02.resources.value",
                    $"{model.StartingCredits:N0} CR / {model.StartingMaterials:N0} MAT"));
                SetAt(conditionNameLabels, 1, _gameTextResolver.Get(
                    "mission.m02.restrictions.label", "MISSION ACCESS"));
                string build = string.IsNullOrWhiteSpace(model.AllowedBuildingConfigId)
                    ? "BUILD OFF"
                    : $"BARRACKS x{model.AllowedBuildingCount}";
                SetAt(conditionLabels, 1, _gameTextResolver.Get(
                    "mission.m02.restrictions.value",
                    $"{build} | TRANSPORT / AIR OFF"));
                return;
            }

            SetAt(conditionNameLabels, 0, "BUILDING / PRODUCTION");
            SetAt(conditionLabels, 0, Restriction(model.BuildingDisabled || model.ProductionDisabled));
            SetAt(conditionNameLabels, 1, "ECONOMY / TRANSPORT / AIR");
            SetAt(conditionLabels, 1,
                Restriction(model.EconomyDisabled || model.TransportDisabled || model.AirDisabled));
        }

        private static string SummaryFallback(string missionId) =>
            missionId == UiCampaignMissionProjectionIds.M02
                ? "Reopen an abandoned JRC forward post before a second hostile cell reaches it."
                : "Secure the Old Market corridor and protect the civilian route.";

        private static string LocationFallback(string missionId) =>
            missionId == UiCampaignMissionProjectionIds.M02
                ? "Abandoned JRC Forward Post, Sahrin  |  MAP: FORWARD POST 01"
                : "Old Market, Sahrin  |  MAP: DISTRICT EDGE 01";

        private static string Restriction(bool disabled) => disabled ? "DISABLED" : "ENABLED";
        private static void SetAt(TMP_Text[] targets, int index, string value)
        {
            if (targets != null && index >= 0 && index < targets.Length)
                Set(targets[index], value);
        }
        private static void Set(TMP_Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }
    }
}
