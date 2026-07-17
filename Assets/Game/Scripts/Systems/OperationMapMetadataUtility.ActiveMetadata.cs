using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public static partial class OperationMapMetadataUtility
    {
        internal static bool TryResolveActiveMetadata(
            EntityManager entityManager,
            out BlobAssetReference<OperationMapBlob> metadataBlob,
            out bool hasActiveMap,
            out string error)
        {
            return TryResolveActiveMetadata(
                entityManager,
                out metadataBlob,
                out _,
                out hasActiveMap,
                out error);
        }

        internal static bool TryResolveActiveMetadata(
            EntityManager entityManager,
            out BlobAssetReference<OperationMapBlob> metadataBlob,
            out int generation,
            out bool hasActiveMap,
            out string error)
        {
            metadataBlob = default;
            generation = 0;
            hasActiveMap = false;

            using EntityQuery rootQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>());
            int rootCount = rootQuery.CalculateEntityCount();
            if (rootCount == 0)
            {
                error = null;
                return false;
            }

            hasActiveMap = true;
            if (rootCount != 1)
            {
                error = $"Expected exactly one operation-map root, found {rootCount}.";
                return false;
            }

            Entity rootEntity = rootQuery.GetSingletonEntity();
            if (!entityManager.HasComponent<ActiveOperationMapComponent>(rootEntity) ||
                !entityManager.HasComponent<OperationMapMetadataComponent>(rootEntity))
            {
                error = "The operation-map root is missing active identity or metadata.";
                return false;
            }

            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(rootEntity);
            OperationMapMetadataComponent metadata =
                entityManager.GetComponentData<OperationMapMetadataComponent>(rootEntity);
            if (!metadata.Blob.IsCreated || metadata.Generation != active.Generation)
            {
                error = "Active operation-map metadata is missing or belongs to a different generation.";
                return false;
            }

            if (!metadata.Blob.Value.OperationMapId.Equals(active.OperationMapId))
            {
                error = "Active operation-map identity does not match its metadata blob.";
                return false;
            }

            metadataBlob = metadata.Blob;
            generation = active.Generation;
            error = null;
            return true;
        }
    }
}
