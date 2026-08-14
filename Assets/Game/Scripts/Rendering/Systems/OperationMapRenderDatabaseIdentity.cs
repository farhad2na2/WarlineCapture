using System;
using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Rendering
{
    internal static class OperationMapRenderDatabaseIdentity
    {
        internal static OperationMapMetadataComponent ResolveMetadata(
            EntityManager entityManager, Entity activeMapEntity) =>
            entityManager.HasComponent<OperationMapMetadataComponent>(activeMapEntity)
                ? entityManager.GetComponentData<OperationMapMetadataComponent>(activeMapEntity)
                : default;

        internal static bool IsCompatible(
            OperationMapRenderDatabaseComponent database,
            ActiveOperationMapComponent activeMap,
            OperationMapMetadataComponent metadata)
        {
            if (!database.Blob.IsCreated)
                return false;
            ref OperationMapRenderDatabaseBlob databaseBlob = ref database.Blob.Value;
            if (databaseBlob.OperationMapId.Equals(activeMap.OperationMapId))
                return true;
            if (!metadata.Blob.IsCreated || metadata.Generation != activeMap.Generation ||
                metadata.PhysicalSourceValidated != 1)
                return false;

            ref OperationMapBlob logicalBlob = ref metadata.Blob.Value;
            return logicalBlob.OperationMapId.Equals(activeMap.OperationMapId) &&
                   logicalBlob.SchemaVersion == activeMap.SchemaVersion &&
                   logicalBlob.SchemaVersion == databaseBlob.SchemaVersion &&
                   !logicalBlob.SourceIdentityHash.IsEmpty &&
                   !logicalBlob.SourceContentHash.IsEmpty &&
                   logicalBlob.SourceOperationMapId.Equals(databaseBlob.OperationMapId);
        }

        internal static void ValidateForStateSync(
            OperationMapRenderDatabaseComponent database,
            ActiveOperationMapComponent activeMap,
            OperationMapMetadataComponent metadata)
        {
            if (!database.Blob.IsCreated || activeMap.Generation <= 0 ||
                !IsCompatible(database, activeMap, metadata))
            {
                throw new InvalidOperationException(
                    "Render state synchronization received an invalid map database.");
            }
        }

        internal static void ValidateForVirtualization(
            OperationMapRenderDatabaseComponent database,
            OperationMapRenderPackedReadinessComponent readiness,
            ActiveOperationMapComponent activeMap,
            OperationMapMetadataComponent metadata)
        {
            if (!database.Blob.IsCreated)
                throw new InvalidOperationException(
                    "Render virtualization database blob is not created.");
            ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
            if (database.SchemaVersion <= 0 ||
                database.SchemaVersion != blob.SchemaVersion ||
                database.ContentHash.Length == 0 ||
                !database.ContentHash.Equals(blob.ContentHash) ||
                !IsCompatible(database, activeMap, metadata))
            {
                throw new InvalidOperationException(
                    "Render virtualization database map, schema, or content identity is invalid.");
            }
            if (readiness.ResidencyMode !=
                (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool)
            {
                throw new InvalidOperationException(
                    "Render virtualization initialization requires VirtualizedProxyPool residency.");
            }
        }
    }
}
