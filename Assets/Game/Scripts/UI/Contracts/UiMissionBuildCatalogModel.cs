namespace Game.UI.Contracts
{
    public readonly struct UiMissionBuildCatalogModel
    {
        public static readonly UiMissionBuildCatalogModel Inactive = default;

        public UiMissionBuildCatalogModel(string missionId, int entryCount)
            : this(missionId, entryCount, string.Empty, false)
        {
        }

        public UiMissionBuildCatalogModel(
            string missionId,
            int entryCount,
            string requiredUnitConfigId,
            bool requiredProducerCompleted)
        {
            IsActive = true;
            MissionId = missionId ?? string.Empty;
            EntryCount = entryCount < 0 ? 0 : entryCount;
            RequiredUnitConfigId = requiredUnitConfigId ?? string.Empty;
            RequiredProducerCompleted = requiredProducerCompleted;
        }

        public bool IsActive { get; }
        public string MissionId { get; }
        public int EntryCount { get; }
        public string RequiredUnitConfigId { get; }
        public bool RequiredProducerCompleted { get; }
        public bool CanRequestRequiredUnit =>
            RequiredProducerCompleted && !string.IsNullOrWhiteSpace(RequiredUnitConfigId);
    }

    public readonly struct UiMissionBuildCatalogEntryModel
    {
        public UiMissionBuildCatalogEntryModel(string buildingConfigId, int maxCount)
        {
            BuildingConfigId = buildingConfigId ?? string.Empty;
            MaxCount = maxCount < 0 ? 0 : maxCount;
        }

        public string BuildingConfigId { get; }
        public int MaxCount { get; }
    }

    public interface IUiMissionBuildCatalogGateway
    {
        bool TryReadMissionBuildCatalog(out UiMissionBuildCatalogModel catalog);
        bool TryReadMissionBuildCatalogEntry(int index, out UiMissionBuildCatalogEntryModel entry);
    }
}
