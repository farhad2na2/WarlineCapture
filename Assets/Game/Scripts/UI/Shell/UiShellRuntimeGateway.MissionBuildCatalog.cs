using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadMissionBuildCatalog(out UiMissionBuildCatalogModel catalog)
        {
            catalog = UiMissionBuildCatalogModel.Inactive;
            return current is IUiMissionBuildCatalogGateway missionGateway &&
                   missionGateway.TryReadMissionBuildCatalog(out catalog) &&
                   catalog.IsActive;
        }

        public static bool TryReadMissionBuildCatalogEntry(
            int index,
            out UiMissionBuildCatalogEntryModel entry)
        {
            entry = default;
            return current is IUiMissionBuildCatalogGateway missionGateway &&
                   missionGateway.TryReadMissionBuildCatalogEntry(index, out entry);
        }
    }
}
