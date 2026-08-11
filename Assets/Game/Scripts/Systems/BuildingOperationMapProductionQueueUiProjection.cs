using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingOperationMapProductionQueueUiProjection
    {
        internal static void Append(
            BuildingUiQueryUiSystemHelper.Context context,
            float now,
            List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> entries)
        {
            if (entries == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager entityManager) ||
                entityManager.World == null ||
                !entityManager.World.IsCreated)
            {
                return;
            }

            BuildingProductionRequestSystemHelper.Context productionContext =
                context.CreateProductionRequestContext != null
                    ? context.CreateProductionRequestContext()
                    : default;

            using EntityQuery producerQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingProductionQueueComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingUnitProductionRequest>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>());
            using NativeArray<Entity> producers = producerQuery.ToEntityArray(Allocator.Temp);
            for (int producerIndex = 0; producerIndex < producers.Length; producerIndex++)
            {
                Entity producer = producers[producerIndex];
                if (!IsFriendlyLiveProducer(entityManager, producer))
                    continue;

                AppendPendingRequests(
                    context,
                    productionContext,
                    entityManager,
                    producer,
                    now,
                    entries);
            }
        }

        private static bool IsFriendlyLiveProducer(EntityManager entityManager, Entity producer) =>
            entityManager.GetComponentData<Faction>(producer).Id == FactionIdentity.PlayerFactionId &&
            entityManager.GetComponentData<UnitHealth>(producer).Current > 0 &&
            (!entityManager.HasComponent<OperationMapBuildingDestroyedComponent>(producer) ||
             !entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(producer));

        private static void AppendPendingRequests(
            BuildingUiQueryUiSystemHelper.Context context,
            BuildingProductionRequestSystemHelper.Context productionContext,
            EntityManager entityManager,
            Entity producer,
            float now,
            List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> entries)
        {
            OperationMapBuildingComponent building =
                entityManager.GetComponentData<OperationMapBuildingComponent>(producer);
            string producerDisplayName = entityManager.HasComponent<UnitDisplayInfo>(producer)
                ? entityManager.GetComponentData<UnitDisplayInfo>(producer).Name.ToString()
                : building.StableId.ToString();
            DynamicBuffer<OperationMapBuildingUnitProductionRequest> queue =
                entityManager.GetBuffer<OperationMapBuildingUnitProductionRequest>(producer, true);
            for (int requestIndex = 0; requestIndex < queue.Length; requestIndex++)
            {
                OperationMapBuildingUnitProductionRequest request = queue[requestIndex];
                if (request.Status != OperationMapBuildingUnitProductionRequest.Pending ||
                    !TryResolvePrefab(context, productionContext, request, out GameObject prefab))
                {
                    continue;
                }

                float duration = Mathf.Max(0.01f, request.ReadyAt - request.QueuedAt);
                entries.Add(new BuildingUiQueryUiSystemHelper.PendingProductionUiEntry(
                    building.PlacementIndex,
                    -1,
                    prefab,
                    Mathf.Max(0f, request.ReadyAt - now),
                    duration,
                    Mathf.Clamp01((now - request.QueuedAt) / duration),
                    request.QueuedAt,
                    request.ReadyAt,
                    producerDisplayName));
            }
        }

        private static bool TryResolvePrefab(
            BuildingUiQueryUiSystemHelper.Context context,
            BuildingProductionRequestSystemHelper.Context productionContext,
            OperationMapBuildingUnitProductionRequest request,
            out GameObject prefab)
        {
            prefab = null;
            context.TryResolveLiveUnitPreviewPrefab?.Invoke(request.UnitPrefab, out prefab);
            if (prefab != null)
                return true;

            string normalizedSourceKey = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                request.UnitSourceKey.ToString());
            return !string.IsNullOrEmpty(normalizedSourceKey) &&
                   productionContext.UnitSpawnPrefabsByKey != null &&
                   productionContext.UnitSpawnPrefabsByKey.TryGetValue(normalizedSourceKey, out prefab) &&
                   prefab != null;
        }
    }
}
