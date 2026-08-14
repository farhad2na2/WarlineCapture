using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Contracts.Ecs
{
    public struct UiCampaignOperationsComponent : IComponentData
    {
        public uint Version;
        public uint CatalogSourceVersion;
        public uint ProgressSourceVersion;
        public uint ObservedSettlementSourceVersion;
        public FixedString64Bytes SelectedMissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes DisplayName;
        public FixedString64Bytes PrimaryActionLabel;
        public FixedString64Bytes NextMissionId;
        public int BestStars;
        public int BestCompletionMilliseconds;
        public int SuccessfulReplayCount;
        public int LastAttemptOrdinal;
        public UiCampaignMissionPrimaryActionKind PrimaryAction;
        public byte Available;
        public byte FirstClearCompleted;
        public byte PendingResume;
        public byte NextMissionRevealed;
    }

    public struct UiMissionObjectiveProjectionData
    {
        public FixedString64Bytes ObjectiveId;
        public FixedString64Bytes DisplayTextKey;
        public FixedString64Bytes MissionRoleId;
        public UiMissionObjectiveRuleKind Rule;
        public int RequiredCount;
        public byte FailureOnRuleBreak;
    }

    public struct UiMissionRewardProjectionData
    {
        public FixedString64Bytes RewardConfigId;
        public FixedString64Bytes DisplayTextKey;
        public UiMissionRewardKind Kind;
        public int Amount;
    }

    public struct UiMissionBriefingComponent : IComponentData
    {
        public uint Version;
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes DisplayNameKey;
        public FixedString64Bytes DisplaySummaryKey;
        public FixedString64Bytes LocationNameKey;
        public FixedList512Bytes<UiMissionObjectiveProjectionData> Objectives;
        public FixedList512Bytes<UiMissionRewardProjectionData> Rewards;
        public ulong DeployTransitionToken;
        public int HostileUnitCount;
        public byte BuildingDisabled;
        public byte ProductionDisabled;
        public byte EconomyDisabled;
        public byte TransportDisabled;
        public byte AirDisabled;
        public byte Replay;
        public byte ReplayAllowed;
        public byte ReplayTutorialEnabled;
        public byte ReplayTutorialToggleVisible;
        public byte DeployQueued;
    }

    [InternalBufferCapacity(2)]
    public struct UiCampaignMissionActionRequestElement : IBufferElementData
    {
        public UiCampaignMissionActionKind Action;
        public FixedString64Bytes MissionId;
        public byte Value;
    }
}
