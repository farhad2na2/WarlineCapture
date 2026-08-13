using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static class CampaignMissionCatalogProjection
    {
        public static bool TryProject(
            EntityManager entityManager,
            MissionDefinitionConfig mission,
            ScenarioSetupConfig scenario,
            OperationMapCatalogConfig operationMaps,
            uint sourceVersion,
            out Entity root,
            out string error)
        {
            root = Entity.Null;
            error = null;
            if (sourceVersion == 0 || !MissionDefinitionContractValidation.TryValidateDefinition(mission, out error) ||
                scenario == null || !scenario.TryValidate(out error) || operationMaps == null ||
                !operationMaps.TryValidate(out error) ||
                !string.Equals(mission.ScenarioId, scenario.ScenarioId, StringComparison.Ordinal) ||
                !string.Equals(mission.OperationMapId, scenario.OperationMapId, StringComparison.Ordinal) ||
                !operationMaps.TryResolve(mission.OperationMapId, out _))
            {
                error ??= "Mission, scenario, and operation-map identities must resolve as one validated launch unit.";
                return false;
            }

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>());
            using NativeArray<Entity> roots = query.ToEntityArray(Allocator.Temp);
            if (roots.Length > 1)
            {
                error = "Exactly zero or one campaign-mission root is permitted.";
                return false;
            }

            root = roots.Length == 1 ? roots[0] : CreateRoot(entityManager);
            CampaignMissionCatalogComponent previous = entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (previous.SourceVersion == sourceVersion && previous.Blob.IsCreated &&
                previous.Blob.Value.Missions.Length == 1 &&
                previous.Blob.Value.Missions[0].MissionId.Equals(new FixedString64Bytes(mission.MissionId)))
            {
                error = null;
                return true;
            }

            BlobBuilder builder = new(Allocator.Temp);
            ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            catalog.SchemaVersion = mission.SchemaVersion;
            BlobBuilderArray<CampaignMissionDefinitionBlob> definitions = builder.Allocate(ref catalog.Missions, 1);
            definitions[0] = new CampaignMissionDefinitionBlob
            {
                MissionId = new FixedString64Bytes(mission.MissionId),
                ScenarioId = new FixedString64Bytes(scenario.ScenarioId),
                OperationMapId = new FixedString64Bytes(mission.OperationMapId),
                SchemaVersion = mission.SchemaVersion
            };
            BlobAssetReference<CampaignMissionCatalogBlob> projected =
                builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            CampaignMissionCatalogDisposalSystem.DisposeOwned(ref previous);
            entityManager.SetComponentData(root, new CampaignMissionCatalogComponent
            {
                Blob = projected,
                SourceVersion = sourceVersion,
                OwnsBlob = 1
            });
            error = null;
            return true;
        }

        private static Entity CreateRoot(EntityManager entityManager)
        {
            Entity root = entityManager.CreateEntity(
                typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
                typeof(CampaignMissionLaunchQueueComponent), typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent));
            entityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
            entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
            entityManager.SetName(root, "CampaignMissionRoot");
            return root;
        }
    }
}
