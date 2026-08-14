using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Composition
{
    internal static class CampaignMissionOperationMapReuseUtility
    {
        public static bool TryReuse(
            EntityManager entityManager,
            OperationMapDefinition physicalSource,
            out Entity rootEntity,
            out string error)
        {
            rootEntity = Entity.Null;
            error = null;
            if (physicalSource == null)
                return false;

            using EntityQuery mapQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>(),
                ComponentType.ReadOnly<ActiveOperationMapComponent>(),
                ComponentType.ReadOnly<OperationMapMetadataComponent>());
            using EntityQuery missionQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionLaunchRequestElement>());
            if (mapQuery.CalculateEntityCount() != 1 || missionQuery.CalculateEntityCount() != 1)
                return false;

            Entity mapRoot = mapQuery.GetSingletonEntity();
            Entity missionRoot = missionQuery.GetSingletonEntity();
            DynamicBuffer<CampaignMissionLaunchRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
            if (requests.Length == 0)
                return false;

            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            OperationMapMetadataComponent metadata =
                entityManager.GetComponentData<OperationMapMetadataComponent>(mapRoot);
            metadata.PhysicalSourceValidated = 0;
            entityManager.SetComponentData(mapRoot, metadata);
            CampaignMissionLaunchRequestElement request = requests[0];
            if (!metadata.Blob.IsCreated || active.Generation != metadata.Generation ||
                !active.OperationMapId.Equals(request.OperationMapId) ||
                !active.ScenarioId.Equals(request.ScenarioId) ||
                !active.MissionId.Equals(request.MissionId) ||
                !metadata.Blob.Value.SourceOperationMapId.Equals(
                    new Unity.Collections.FixedString64Bytes(physicalSource.OperationMapId)) ||
                !metadata.Blob.Value.SourceIdentityHash.Equals(
                    new Unity.Collections.FixedString128Bytes(physicalSource.SourceIdentityHash)) ||
                !metadata.Blob.Value.SourceContentHash.Equals(
                    new Unity.Collections.FixedString128Bytes(physicalSource.ContentHash)) ||
                metadata.Blob.Value.SchemaVersion != physicalSource.SchemaVersion)
                return false;

            metadata.PhysicalSourceValidated = 1;
            entityManager.SetComponentData(mapRoot, metadata);
            rootEntity = mapRoot;
            return true;
        }
    }
}
