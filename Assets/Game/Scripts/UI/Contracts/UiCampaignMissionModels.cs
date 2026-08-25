using System;

namespace Game.UI.Contracts
{
    public enum UiCampaignMissionPrimaryActionKind : byte
    {
        Locked = 0,
        Start = 1,
        Continue = 2,
        Replay = 3
    }

    public enum UiCampaignMissionActionKind : byte
    {
        None = 0,
        Refresh = 1,
        Select = 2,
        OpenBriefing = 3,
        SetReplayTutorial = 4,
        Deploy = 5
    }

    public enum UiMissionObjectiveRuleKind : byte
    {
        None = 0,
        DestroyMissionRole = 1,
        ProtectMissionRole = 2,
        BuildStructure = 3,
        ProduceUnit = 4,
        DefendMissionRole = 5
    }

    public enum UiMissionRewardKind : byte
    {
        None = 0,
        Credits = 1,
        Materials = 2,
        Fuel = 3,
        Intel = 4
    }

    public readonly struct UiCampaignMissionModel : IEquatable<UiCampaignMissionModel>
    {
        public UiCampaignMissionModel(
            string missionId, string scenarioId, string operationMapId, string displayName,
            bool available, bool firstClearCompleted, bool pendingResume,
            int bestStars, int bestCompletionMilliseconds, int successfulReplayCount,
            UiCampaignMissionPrimaryActionKind primaryAction, string primaryActionLabel)
        {
            MissionId = missionId ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
            OperationMapId = operationMapId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Available = available;
            FirstClearCompleted = firstClearCompleted;
            PendingResume = pendingResume;
            BestStars = Math.Clamp(bestStars, 0, 3);
            BestCompletionMilliseconds = Math.Max(0, bestCompletionMilliseconds);
            SuccessfulReplayCount = Math.Max(0, successfulReplayCount);
            PrimaryAction = primaryAction;
            PrimaryActionLabel = primaryActionLabel ?? string.Empty;
        }

        public string MissionId { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public string DisplayName { get; }
        public bool Available { get; }
        public bool FirstClearCompleted { get; }
        public bool PendingResume { get; }
        public int BestStars { get; }
        public int BestCompletionMilliseconds { get; }
        public int SuccessfulReplayCount { get; }
        public UiCampaignMissionPrimaryActionKind PrimaryAction { get; }
        public string PrimaryActionLabel { get; }

        public bool Equals(UiCampaignMissionModel other) =>
            MissionId == other.MissionId && ScenarioId == other.ScenarioId &&
            OperationMapId == other.OperationMapId && DisplayName == other.DisplayName &&
            Available == other.Available && FirstClearCompleted == other.FirstClearCompleted &&
            PendingResume == other.PendingResume && BestStars == other.BestStars &&
            BestCompletionMilliseconds == other.BestCompletionMilliseconds &&
            SuccessfulReplayCount == other.SuccessfulReplayCount && PrimaryAction == other.PrimaryAction &&
            PrimaryActionLabel == other.PrimaryActionLabel;

        public override bool Equals(object obj) => obj is UiCampaignMissionModel other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            MissionId, ScenarioId, OperationMapId, DisplayName, Available, FirstClearCompleted,
            HashCode.Combine(PendingResume, BestStars, BestCompletionMilliseconds,
                SuccessfulReplayCount, PrimaryAction, PrimaryActionLabel));
    }

    public readonly struct UiCampaignOperationsModel
    {
        public UiCampaignOperationsModel(
            uint version, uint catalogSourceVersion, uint progressSourceVersion,
            UiCampaignMissionModel selectedMission, string nextMissionId, bool nextMissionRevealed)
        {
            Version = version;
            CatalogSourceVersion = catalogSourceVersion;
            ProgressSourceVersion = progressSourceVersion;
            SelectedMission = selectedMission;
            NextMissionId = nextMissionId ?? string.Empty;
            NextMissionRevealed = nextMissionRevealed;
        }

        public uint Version { get; }
        public uint CatalogSourceVersion { get; }
        public uint ProgressSourceVersion { get; }
        public UiCampaignMissionModel SelectedMission { get; }
        public string NextMissionId { get; }
        public bool NextMissionRevealed { get; }
        public bool IsValid => Version != 0 && !string.IsNullOrWhiteSpace(SelectedMission.MissionId);
    }

    public readonly struct UiMissionObjectiveModel
    {
        public UiMissionObjectiveModel(
            string objectiveId, string displayTextKey, string missionRoleId,
            UiMissionObjectiveRuleKind rule, int requiredCount, bool failureOnRuleBreak)
            : this(objectiveId, displayTextKey, missionRoleId, string.Empty,
                rule, requiredCount, failureOnRuleBreak)
        {
        }

        public UiMissionObjectiveModel(
            string objectiveId, string displayTextKey, string missionRoleId, string targetConfigId,
            UiMissionObjectiveRuleKind rule, int requiredCount, bool failureOnRuleBreak)
        {
            ObjectiveId = objectiveId ?? string.Empty;
            DisplayTextKey = displayTextKey ?? string.Empty;
            MissionRoleId = missionRoleId ?? string.Empty;
            TargetConfigId = targetConfigId ?? string.Empty;
            Rule = rule;
            RequiredCount = Math.Max(1, requiredCount);
            FailureOnRuleBreak = failureOnRuleBreak;
        }

        public string ObjectiveId { get; }
        public string DisplayTextKey { get; }
        public string MissionRoleId { get; }
        public string TargetConfigId { get; }
        public UiMissionObjectiveRuleKind Rule { get; }
        public int RequiredCount { get; }
        public bool FailureOnRuleBreak { get; }
    }

    public readonly struct UiMissionRewardModel
    {
        public UiMissionRewardModel(
            UiMissionRewardKind kind, string rewardConfigId, string displayTextKey, int amount)
        {
            Kind = kind;
            RewardConfigId = rewardConfigId ?? string.Empty;
            DisplayTextKey = displayTextKey ?? string.Empty;
            Amount = Math.Max(0, amount);
        }

        public UiMissionRewardKind Kind { get; }
        public string RewardConfigId { get; }
        public string DisplayTextKey { get; }
        public int Amount { get; }
    }

    public readonly struct UiMissionBriefingModel
    {
        public UiMissionBriefingModel(
            uint version, string missionId, string scenarioId, string operationMapId,
            string displayNameKey, string displaySummaryKey, string locationNameKey,
            UiMissionObjectiveModel[] objectives, UiMissionRewardModel[] rewards,
            int hostileUnitCount, bool buildingDisabled, bool productionDisabled,
            bool economyDisabled, bool transportDisabled, bool airDisabled,
            bool replay, bool replayAllowed, bool replayTutorialEnabled,
            bool replayTutorialToggleVisible, bool deployQueued)
            : this(version, missionId, scenarioId, operationMapId,
                displayNameKey, displaySummaryKey, locationNameKey,
                objectives, rewards, hostileUnitCount, 0, 0, string.Empty, 0,
                buildingDisabled, productionDisabled, economyDisabled, transportDisabled, airDisabled,
                replay, replayAllowed, replayTutorialEnabled, replayTutorialToggleVisible, deployQueued)
        {
        }

        public UiMissionBriefingModel(
            uint version, string missionId, string scenarioId, string operationMapId,
            string displayNameKey, string displaySummaryKey, string locationNameKey,
            UiMissionObjectiveModel[] objectives, UiMissionRewardModel[] rewards,
            int hostileUnitCount, int startingCredits, int startingMaterials,
            string allowedBuildingConfigId, int allowedBuildingCount,
            bool buildingDisabled, bool productionDisabled,
            bool economyDisabled, bool transportDisabled, bool airDisabled,
            bool replay, bool replayAllowed, bool replayTutorialEnabled,
            bool replayTutorialToggleVisible, bool deployQueued)
        {
            Version = version;
            MissionId = missionId ?? string.Empty;
            ScenarioId = scenarioId ?? string.Empty;
            OperationMapId = operationMapId ?? string.Empty;
            DisplayNameKey = displayNameKey ?? string.Empty;
            DisplaySummaryKey = displaySummaryKey ?? string.Empty;
            LocationNameKey = locationNameKey ?? string.Empty;
            Objectives = objectives ?? Array.Empty<UiMissionObjectiveModel>();
            Rewards = rewards ?? Array.Empty<UiMissionRewardModel>();
            HostileUnitCount = Math.Max(0, hostileUnitCount);
            StartingCredits = Math.Max(0, startingCredits);
            StartingMaterials = Math.Max(0, startingMaterials);
            AllowedBuildingConfigId = allowedBuildingConfigId ?? string.Empty;
            AllowedBuildingCount = Math.Max(0, allowedBuildingCount);
            BuildingDisabled = buildingDisabled;
            ProductionDisabled = productionDisabled;
            EconomyDisabled = economyDisabled;
            TransportDisabled = transportDisabled;
            AirDisabled = airDisabled;
            Replay = replay;
            ReplayAllowed = replayAllowed;
            ReplayTutorialEnabled = replayTutorialEnabled;
            ReplayTutorialToggleVisible = replayTutorialToggleVisible;
            DeployQueued = deployQueued;
        }

        public uint Version { get; }
        public string MissionId { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public string DisplayNameKey { get; }
        public string DisplaySummaryKey { get; }
        public string LocationNameKey { get; }
        public UiMissionObjectiveModel[] Objectives { get; }
        public UiMissionRewardModel[] Rewards { get; }
        public int HostileUnitCount { get; }
        public int StartingCredits { get; }
        public int StartingMaterials { get; }
        public string AllowedBuildingConfigId { get; }
        public int AllowedBuildingCount { get; }
        public bool BuildingDisabled { get; }
        public bool ProductionDisabled { get; }
        public bool EconomyDisabled { get; }
        public bool TransportDisabled { get; }
        public bool AirDisabled { get; }
        public bool Replay { get; }
        public bool ReplayAllowed { get; }
        public bool ReplayTutorialEnabled { get; }
        public bool ReplayTutorialToggleVisible { get; }
        public bool DeployQueued { get; }
        public bool IsValid => Version != 0 && !string.IsNullOrWhiteSpace(MissionId) && Objectives.Length > 0;
    }
}
