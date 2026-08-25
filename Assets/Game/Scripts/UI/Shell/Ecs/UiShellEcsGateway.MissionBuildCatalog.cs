using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionBuildCatalogGateway
    {
        public bool TryReadMissionBuildCatalog(out UiMissionBuildCatalogModel catalog)
        {
            catalog = UiMissionBuildCatalogModel.Inactive;
            if (!TryResolveMissionBuildCatalog(out CampaignMissionRuntimeComponent runtime,
                    out CampaignMissionCatalogComponent source, out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref source.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0)
                return false;

            catalog = new UiMissionBuildCatalogModel(
                runtime.MissionId.ToString(), definition.BuildCatalog.Length);
            return true;
        }

        public bool TryReadMissionBuildCatalogEntry(
            int index,
            out UiMissionBuildCatalogEntryModel entry)
        {
            entry = default;
            if (!TryResolveMissionBuildCatalog(out _, out CampaignMissionCatalogComponent source,
                    out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref source.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0 || index < 0 || index >= definition.BuildCatalog.Length)
                return false;

            ref CampaignMissionBuildEntryBlob sourceEntry = ref definition.BuildCatalog[index];
            entry = new UiMissionBuildCatalogEntryModel(
                sourceEntry.BuildingConfigId.ToString(), sourceEntry.MaxCount);
            return true;
        }

        private static bool TryResolveMissionBuildCatalog(
            out CampaignMissionRuntimeComponent runtime,
            out CampaignMissionCatalogComponent catalog,
            out int definitionIndex)
        {
            runtime = default;
            catalog = default;
            definitionIndex = -1;
            if (!TryGetMissionRoot(out EntityManager entityManager, out Entity root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionCatalogComponent>(root))
                return false;

            runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            catalog = entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.Phase == MissionPhaseKind.None || runtime.MissionId.Length == 0 ||
                !catalog.Blob.IsCreated)
                return false;

            ref CampaignMissionCatalogBlob blob = ref catalog.Blob.Value;
            for (int index = 0; index < blob.Missions.Length; index++)
            {
                if (!blob.Missions[index].MissionId.Equals(runtime.MissionId))
                    continue;

                definitionIndex = index;
                return true;
            }

            return false;
        }
    }
}
