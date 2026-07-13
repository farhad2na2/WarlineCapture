using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class ResourceExchangePhysicalStorageCandidateQuerySystemHelper
    {
        private const float Epsilon = 0.001f;

        internal struct StorageCandidate : IComparable<StorageCandidate>
        {
            public Entity Entity;
            public int RuntimeBuildingId;
            public float Available;

            public int CompareTo(StorageCandidate other)
            {
                int runtimeIdComparison = RuntimeBuildingId.CompareTo(other.RuntimeBuildingId);
                if (runtimeIdComparison != 0)
                    return runtimeIdComparison;

                int indexComparison = Entity.Index.CompareTo(other.Entity.Index);
                return indexComparison != 0
                    ? indexComparison
                    : Entity.Version.CompareTo(other.Entity.Version);
            }
        }

        internal static float CollectCandidates(
            EntityManager entityManager,
            EntityQuery storageQuery,
            byte factionId,
            byte resourceKind,
            ResourceExchangePhysicalReservationKind reservationKind,
            NativeList<StorageCandidate> candidates)
        {
            float total = 0f;
            using NativeArray<ArchetypeChunk> chunks = storageQuery.ToArchetypeChunkArray(Allocator.Temp);
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            ComponentTypeHandle<BuildingResourceStorageComponent> storageType =
                entityManager.GetComponentTypeHandle<BuildingResourceStorageComponent>(true);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<BuildingResourceStorageComponent> storages = chunk.GetNativeArray(ref storageType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    BuildingResourceStorageComponent storage = storages[i];
                    if (storage.OwnerFactionId != factionId)
                        continue;

                    float available = reservationKind == ResourceExchangePhysicalReservationKind.Input
                        ? BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(storage, resourceKind)
                        : resourceKind == BuildingResourceStorageTransferSystemHelper.FuelResourceKind
                            ? BuildingResourceStorageTransferSystemHelper.GetFuelReceivingFreeCapacity(storage)
                            : BuildingResourceStorageTransferSystemHelper.GetOilReceivingFreeCapacity(storage);
                    if (available <= Epsilon)
                        continue;

                    candidates.Add(new StorageCandidate
                    {
                        Entity = entities[i],
                        RuntimeBuildingId = storage.RuntimeBuildingId,
                        Available = available
                    });
                    total += available;
                }
            }

            return total;
        }
    }
}
