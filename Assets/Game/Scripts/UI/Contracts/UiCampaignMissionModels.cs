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
        OpenBriefing = 3
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
}
