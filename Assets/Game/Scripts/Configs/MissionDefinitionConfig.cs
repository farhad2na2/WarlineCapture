using System;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionObjectiveDefinitionConfig
    {
        [SerializeField] private string objectiveId;
        [SerializeField] private string displayTextKey;
        [SerializeField] private MissionObjectiveRuleKind rule;
        [SerializeField] private string missionRoleId;
        [SerializeField] private string targetConfigId;
        [SerializeField, Min(1)] private int requiredCount;
        [SerializeField] private bool failureOnRuleBreak;

        public MissionObjectiveDefinitionConfig(
            string objectiveId,
            string displayTextKey,
            MissionObjectiveRuleKind rule,
            string missionRoleId,
            int requiredCount,
            bool failureOnRuleBreak = false)
        {
            this.objectiveId = objectiveId;
            this.displayTextKey = displayTextKey;
            this.rule = rule;
            this.missionRoleId = missionRoleId;
            this.targetConfigId = string.Empty;
            this.requiredCount = requiredCount;
            this.failureOnRuleBreak = failureOnRuleBreak;
        }

        public MissionObjectiveDefinitionConfig(
            string objectiveId,
            string displayTextKey,
            MissionObjectiveRuleKind rule,
            string missionRoleId,
            string targetConfigId,
            int requiredCount,
            bool failureOnRuleBreak = false)
        {
            this.objectiveId = objectiveId;
            this.displayTextKey = displayTextKey;
            this.rule = rule;
            this.missionRoleId = missionRoleId;
            this.targetConfigId = targetConfigId;
            this.requiredCount = requiredCount;
            this.failureOnRuleBreak = failureOnRuleBreak;
        }

        public string ObjectiveId => objectiveId;
        public string DisplayTextKey => displayTextKey;
        public MissionObjectiveRuleKind Rule => rule;
        public string MissionRoleId => missionRoleId;
        public string TargetConfigId => targetConfigId;
        public int RequiredCount => requiredCount;
        public bool FailureOnRuleBreak => failureOnRuleBreak;
    }

    [Serializable]
    public struct MissionStarDefinitionConfig
    {
        [SerializeField, Range(1, 3)] private byte starIndex;
        [SerializeField] private MissionStarRuleKind rule;
        [SerializeField] private string displayTextKey;
        [SerializeField, Min(0)] private int threshold;

        public MissionStarDefinitionConfig(byte starIndex, MissionStarRuleKind rule, int threshold = 0)
            : this(starIndex, rule, DefaultDisplayTextKey(rule), threshold)
        {
        }

        public MissionStarDefinitionConfig(
            byte starIndex,
            MissionStarRuleKind rule,
            string displayTextKey,
            int threshold = 0)
        {
            this.starIndex = starIndex;
            this.rule = rule;
            this.displayTextKey = displayTextKey;
            this.threshold = threshold;
        }

        public byte StarIndex => starIndex;
        public MissionStarRuleKind Rule => rule;
        public string DisplayTextKey => displayTextKey;
        public int Threshold => threshold;

        private static string DefaultDisplayTextKey(MissionStarRuleKind value) => value switch
        {
            MissionStarRuleKind.CompleteMission => "mission.star.complete",
            MissionStarRuleKind.NoSquadLoss => "mission.star.no_squad_loss",
            MissionStarRuleKind.CompleteUnderMilliseconds => "mission.star.under_time",
            MissionStarRuleKind.NoCivilianLoss => "mission.star.no_civilian_loss",
            _ => string.Empty
        };
    }

    [Serializable]
    public struct MissionRewardDefinitionConfig
    {
        [SerializeField] private MissionRewardKind kind;
        [SerializeField] private string rewardConfigId;
        [SerializeField] private string displayTextKey;
        [SerializeField, Min(1)] private int amount;

        public MissionRewardDefinitionConfig(MissionRewardKind kind, int amount)
            : this(kind, string.Empty, DefaultDisplayTextKey(kind), amount)
        {
        }

        public MissionRewardDefinitionConfig(string rewardConfigId, string displayTextKey, int amount)
            : this(MissionRewardKind.None, rewardConfigId, displayTextKey, amount)
        {
        }

        private MissionRewardDefinitionConfig(
            MissionRewardKind kind,
            string rewardConfigId,
            string displayTextKey,
            int amount)
        {
            this.kind = kind;
            this.rewardConfigId = rewardConfigId;
            this.displayTextKey = displayTextKey;
            this.amount = amount;
        }

        public MissionRewardKind Kind => kind;
        public string RewardConfigId => rewardConfigId;
        public string DisplayTextKey => displayTextKey;
        public int Amount => amount;

        private static string DefaultDisplayTextKey(MissionRewardKind value) => value switch
        {
            MissionRewardKind.Credits => "mission.reward.credits",
            MissionRewardKind.Materials => "mission.reward.materials",
            MissionRewardKind.Fuel => "mission.reward.fuel",
            MissionRewardKind.Intel => "mission.reward.intel",
            _ => string.Empty
        };
    }

    [Serializable]
    public struct MissionCommandPolicyConfig
    {
        [SerializeField] private TacticalCommandMode[] allowedCommands;

        public MissionCommandPolicyConfig(TacticalCommandMode[] allowedCommands)
        {
            this.allowedCommands = allowedCommands;
        }

        public ReadOnlySpan<TacticalCommandMode> AllowedCommands => allowedCommands;
    }

    [CreateAssetMenu(menuName = "Game/Missions/Mission Definition", fileName = "MissionDefinition")]
    public sealed class MissionDefinitionConfig : ScriptableObject
    {
        [SerializeField] private string missionId;
        [SerializeField, Min(1)] private int schemaVersion = 1;
        [Header("Display")]
        [SerializeField] private string displayNameKey;
        [SerializeField] private string displaySummaryKey;
        [SerializeField] private string locationNameKey;
        [Header("References")]
        [SerializeField] private string scenarioId;
        [SerializeField] private string operationMapId;
        [SerializeField] private string briefingSequenceId;
        [SerializeField] private string commsSequenceId;
        [SerializeField] private string debriefSequenceId;
        [Header("Rules")]
        [SerializeField] private MissionObjectiveDefinitionConfig[] objectives =
            Array.Empty<MissionObjectiveDefinitionConfig>();
        [SerializeField] private MissionStarDefinitionConfig[] stars =
            Array.Empty<MissionStarDefinitionConfig>();
        [SerializeField] private MissionRewardDefinitionConfig[] firstClearRewards =
            Array.Empty<MissionRewardDefinitionConfig>();
        [SerializeField] private MissionRewardDefinitionConfig[] replayRewards =
            Array.Empty<MissionRewardDefinitionConfig>();
        [SerializeField] private MissionCommandPolicyConfig commandPolicy;
        [Header("Replay")]
        [SerializeField] private bool replayAllowed = true;
        [SerializeField] private bool replayTutorialDefaultEnabled;
        [Header("Readiness")]
        [SerializeField] private bool requireOperationMapReady = true;
        [SerializeField] private bool requireGridReady = true;
        [SerializeField] private bool requireUnitCatalogReady = true;
        [SerializeField] private string[] requiredFeatureIds = Array.Empty<string>();

        public string MissionId => missionId;
        public int SchemaVersion => schemaVersion;
        public string DisplayNameKey => displayNameKey;
        public string DisplaySummaryKey => displaySummaryKey;
        public string LocationNameKey => locationNameKey;
        public string ScenarioId => scenarioId;
        public string OperationMapId => operationMapId;
        public string BriefingSequenceId => briefingSequenceId;
        public string CommsSequenceId => commsSequenceId;
        public string DebriefSequenceId => debriefSequenceId;
        public ReadOnlySpan<MissionObjectiveDefinitionConfig> Objectives => objectives;
        public ReadOnlySpan<MissionStarDefinitionConfig> Stars => stars;
        public ReadOnlySpan<MissionRewardDefinitionConfig> FirstClearRewards => firstClearRewards;
        public ReadOnlySpan<MissionRewardDefinitionConfig> ReplayRewards => replayRewards;
        public MissionCommandPolicyConfig CommandPolicy => commandPolicy;
        public bool ReplayAllowed => replayAllowed;
        public bool ReplayTutorialDefaultEnabled => replayTutorialDefaultEnabled;
        public bool RequireOperationMapReady => requireOperationMapReady;
        public bool RequireGridReady => requireGridReady;
        public bool RequireUnitCatalogReady => requireUnitCatalogReady;
        public ReadOnlySpan<string> RequiredFeatureIds => requiredFeatureIds;
    }
}
