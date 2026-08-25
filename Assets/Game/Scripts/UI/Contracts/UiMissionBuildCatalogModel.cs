namespace Game.UI.Contracts
{
    public readonly struct UiMissionBuildCatalogModel
    {
        public static readonly UiMissionBuildCatalogModel Inactive = default;

        public UiMissionBuildCatalogModel(string missionId, int entryCount)
        {
            IsActive = true;
            MissionId = missionId ?? string.Empty;
            EntryCount = entryCount < 0 ? 0 : entryCount;
        }

        public bool IsActive { get; }
        public string MissionId { get; }
        public int EntryCount { get; }
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
