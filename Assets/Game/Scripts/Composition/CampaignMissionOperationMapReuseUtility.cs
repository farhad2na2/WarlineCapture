using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Composition
{
    internal static class CampaignMissionOperationMapReuseUtility
    {
        public static bool TryReuse(
            EntityManager entityManager,
            OperationMapDefinition definition,
            out Entity rootEntity,
            out string error)
        {
            rootEntity = Entity.Null;
            error = null;
            if (definition == null)
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
            CampaignMissionLaunchRequestElement request = requests[0];
            if (!metadata.Blob.IsCreated || active.Generation != metadata.Generation ||
                !active.OperationMapId.Equals(request.OperationMapId) ||
                !active.ScenarioId.Equals(request.ScenarioId) ||
                !active.MissionId.Equals(request.MissionId) ||
                !metadata.Blob.Value.OperationMapId.Equals(request.OperationMapId) ||
                active.SchemaVersion != metadata.Blob.Value.SchemaVersion ||
                active.ContentVersion != metadata.Blob.Value.ContentVersion)
                return false;

            bool isLogicalDefinition = request.OperationMapId.Equals(
                new Unity.Collections.FixedString64Bytes(definition.OperationMapId));
            bool exactIdentity = isLogicalDefinition
                ? metadata.Blob.Value.SourceOperationMapId.Equals(
                      new Unity.Collections.FixedString64Bytes(
                          definition.SourceBinding.SourceOperationMapId)) &&
                  metadata.Blob.Value.SourceIdentityHash.Equals(
                      new Unity.Collections.FixedString128Bytes(
                          definition.SourceBinding.SourceIdentityHash)) &&
                  metadata.Blob.Value.SourceContentHash.Equals(
                      new Unity.Collections.FixedString128Bytes(
                          definition.SourceBinding.SourceContentHash)) &&
                  metadata.Blob.Value.ContentHash.Equals(
                      new Unity.Collections.FixedString128Bytes(definition.ContentHash)) &&
                  metadata.Blob.Value.GeneratedMetadataHash.Equals(
                      new Unity.Collections.FixedString128Bytes(definition.GeneratedMetadataHash)) &&
                  metadata.Blob.Value.SchemaVersion == definition.SchemaVersion &&
                  metadata.Blob.Value.ContentVersion == definition.ContentVersion
                : !definition.SourceBinding.IsConfigured &&
                  metadata.Blob.Value.SourceOperationMapId.Equals(
                      new Unity.Collections.FixedString64Bytes(definition.OperationMapId)) &&
                  metadata.Blob.Value.SourceIdentityHash.Equals(
                      new Unity.Collections.FixedString128Bytes(definition.SourceIdentityHash)) &&
                  metadata.Blob.Value.SourceContentHash.Equals(
                      new Unity.Collections.FixedString128Bytes(definition.ContentHash)) &&
                  metadata.Blob.Value.SchemaVersion == definition.SchemaVersion;
            if (!exactIdentity)
                return false;

            if (!isLogicalDefinition && metadata.PhysicalSourceValidated == 0)
            {
                metadata.PhysicalSourceValidated = 1;
                entityManager.SetComponentData(mapRoot, metadata);
            }
            rootEntity = mapRoot;
            return true;
        }
    }
}
