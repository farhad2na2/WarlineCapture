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
        public UiCampaignMissionPrimaryActionKind PrimaryAction;
        public byte Available;
        public byte FirstClearCompleted;
        public byte PendingResume;
        public byte NextMissionRevealed;
    }

    [InternalBufferCapacity(2)]
    public struct UiCampaignMissionActionRequestElement : IBufferElementData
    {
        public UiCampaignMissionActionKind Action;
        public FixedString64Bytes MissionId;
    }
}
