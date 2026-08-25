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
            if (!TryResolveMissionBuildCatalog(out EntityManager entityManager, out Entity root,
                    out CampaignMissionRuntimeComponent runtime,
                    out CampaignMissionCatalogComponent source, out int definitionIndex))
                return false;

            ref CampaignMissionDefinitionBlob definition = ref source.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0)
                return false;

            string requiredUnitConfigId = string.Empty;
            int produceObjectiveCount = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.Rule != MissionObjectiveRuleKind.ProduceUnit)
                    continue;

                produceObjectiveCount++;
                requiredUnitConfigId = objective.TargetConfigId.ToString();
            }

            bool requiredProducerCompleted =
                entityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root) &&
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root)
                    .RequiredBuildingCompletedCount > 0;
            catalog = new UiMissionBuildCatalogModel(
                runtime.MissionId.ToString(),
                definition.BuildCatalog.Length,
                produceObjectiveCount == 1 ? requiredUnitConfigId : string.Empty,
                requiredProducerCompleted);
            return true;
        }

        public bool TryReadMissionBuildCatalogEntry(
            int index,
            out UiMissionBuildCatalogEntryModel entry)
        {
            entry = default;
            if (!TryResolveMissionBuildCatalog(out _, out _, out _,
                    out CampaignMissionCatalogComponent source,
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
            out EntityManager entityManager,
            out Entity root,
            out CampaignMissionRuntimeComponent runtime,
            out CampaignMissionCatalogComponent catalog,
            out int definitionIndex)
        {
            entityManager = default;
            root = Entity.Null;
            runtime = default;
            catalog = default;
            definitionIndex = -1;
            if (!TryGetMissionRoot(out entityManager, out root) ||
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
