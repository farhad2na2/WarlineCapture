using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal static class BuildingProductionEntriesUiSystemHelper
    {
        internal static void GetProducedUnits(
            List<Entity> producedUnits,
            EntityManager entityManager,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            List<Entity> results)
        {
            results?.Clear();
            if (producedUnits == null || productionSystem == null || results == null)
                return;

            productionSystem.PruneProducedUnits(producedUnits, null, null, entityManager);
            for (int i = 0; i < producedUnits.Count; i++)
                results.Add(producedUnits[i]);
        }

        internal static void GetSelectedBuildingProducedUnits(
            BuildingUiQueryUiSystemHelper.Context context,
            List<Entity> results)
        {
            results?.Clear();
            if (results == null ||
                context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em))
            {
                return;
            }

            int? buildingId = context.GetActiveBuildingId();
            if (!buildingId.HasValue ||
                !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
                building == null)
            {
                return;
            }

            if (TryAddProducedUnitsFromReadModel(building, em, results))
                return;

            building.ProducedUnits ??= new List<Entity>();
            GetProducedUnits(building.ProducedUnits, em, context.ProductionSystem, results);
        }

        internal static void AddProducedUnitEntries(
            List<Entity> producedUnits,
            Dictionary<Entity, GameObject> producedUnitPrefabs,
            Dictionary<Entity, FixedString64Bytes> producedUnitSourceKeys,
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            EntityManager entityManager,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry> entries,
            BuildingUiQueryUiSystemHelper.TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab)
        {
            if (entries == null)
                return;

            if (producedUnits != null)
            {
                productionSystem?.PruneProducedUnits(
                    producedUnits,
                    null,
                    producedUnitPrefabs,
                    entityManager,
                    producedUnitSourceKeys);
                for (int i = 0; i < producedUnits.Count; i++)
                {
                    Entity unit = producedUnits[i];
                    GameObject prefab = null;
                    producedUnitPrefabs?.TryGetValue(unit, out prefab);
                    if (prefab == null && tryResolveLiveUnitPreviewPrefab != null)
                        tryResolveLiveUnitPreviewPrefab(unit, out prefab);
                    entries.Add(new BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry(unit, prefab, true, 1f));
                }
            }

            AddPendingProducedUnitEntries(pendingProductions, productionSystem, now, entries);
        }

        internal static void AddPendingProducedUnitEntries(
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry> entries)
        {
            if (pendingProductions == null || productionSystem == null || entries == null)
                return;

            foreach (BuildingProductionQueueCompositionSystemHelper.IPendingProduction pending in pendingProductions)
            {
                if (pending == null || pending.Prefab == null)
                    continue;

                BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress =
                    productionSystem.GetProgress(pending, now, true);
                entries.Add(new BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry(
                    Entity.Null,
                    pending.Prefab,
                    false,
                    progress.Progress01));
            }
        }

        internal static void GetSelectedBuildingProducedUnitEntries(
            BuildingUiQueryUiSystemHelper.Context context,
            List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry> entries)
        {
            entries?.Clear();
            if (entries == null ||
                context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em))
            {
                return;
            }

            int? buildingId = context.GetActiveBuildingId();
            if (!buildingId.HasValue ||
                !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
                building == null)
            {
                return;
            }

            float now = context.GetNow != null ? context.GetNow() : Time.time;
            if (TryAddProducedUnitEntriesFromReadModel(context, building, em, entries))
            {
                AddPendingProducedUnitEntries(building.PendingProductions, context.ProductionSystem, now, entries);
                return;
            }

            building.ProducedUnits ??= new List<Entity>();
            AddProducedUnitEntries(
                building.ProducedUnits,
                building.ProducedUnitPrefabs,
                building.ProducedUnitSourceKeys,
                building.PendingProductions,
                em,
                context.ProductionSystem,
                now,
                entries,
                context.TryResolveLiveUnitPreviewPrefab);
        }

        internal static void AddPendingProductionUiEntries(
            int buildingId,
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> entries,
            string producerDisplayName)
        {
            if (pendingProductions == null || productionSystem == null || entries == null)
                return;

            int pendingIndex = 0;
            foreach (BuildingProductionQueueCompositionSystemHelper.IPendingProduction pending in pendingProductions)
            {
                if (pending == null || pending.Prefab == null)
                {
                    pendingIndex++;
                    continue;
                }

                BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress =
                    productionSystem.GetProgress(pending, now, false);
                entries.Add(new BuildingUiQueryUiSystemHelper.PendingProductionUiEntry(
                    buildingId,
                    pendingIndex,
                    pending.Prefab,
                    progress.RemainingSeconds,
                    progress.DurationSeconds,
                    progress.Progress01,
                    pending.StartedAt,
                    pending.ReadyAt,
                    producerDisplayName));
                pendingIndex++;
            }
        }

        internal static void GetFriendlyPendingProductionUiEntries(
            BuildingUiQueryUiSystemHelper.Context context,
            List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> entries)
        {
            if (entries == null)
                return;

            entries.Clear();
            float now = context.GetNow != null ? context.GetNow() : Time.time;
            if (context.RuntimeBuildings != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                {
                    RuntimeBuildingEntity building = pair.Value;
                    if (building == null ||
                        building.IsDestroyed ||
                        building.PendingProductions == null ||
                        building.PendingProductions.Count == 0)
                    {
                        continue;
                    }

                    if (building.IsCityGenerated)
                        continue;
                    if (!IsFriendlyProductionBuilding(building))
                        continue;

                    AddPendingProductionUiEntries(
                        pair.Key,
                        building.PendingProductions,
                        context.ProductionSystem,
                        now,
                        entries,
                        ResolveProducerDisplayName(pair.Key, building));
                }
            }

            AddOperationMapPendingProductionUiEntries(context, now, entries);
        }

        private static void AddOperationMapPendingProductionUiEntries(
            BuildingUiQueryUiSystemHelper.Context context,
            float now,
            List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> entries)
        {
            if (context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                em.World == null ||
                !em.World.IsCreated)
            {
                return;
            }

            BuildingProductionRequestSystemHelper.Context productionContext =
                context.CreateProductionRequestContext != null
                    ? context.CreateProductionRequestContext()
                    : default;

            using EntityQuery producerQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingProductionQueueComponent>(),
                ComponentType.ReadOnly<OperationMapBuildingUnitProductionRequest>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>());
            using NativeArray<Entity> producers = producerQuery.ToEntityArray(Allocator.Temp);
            for (int producerIndex = 0; producerIndex < producers.Length; producerIndex++)
            {
                Entity producer = producers[producerIndex];
                if (em.GetComponentData<Faction>(producer).Id != FactionIdentity.PlayerFactionId ||
                    em.GetComponentData<UnitHealth>(producer).Current <= 0 ||
                    (em.HasComponent<OperationMapBuildingDestroyedComponent>(producer) &&
                     em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(producer)))
                {
                    continue;
                }

                OperationMapBuildingComponent building =
                    em.GetComponentData<OperationMapBuildingComponent>(producer);
                string producerDisplayName = em.HasComponent<UnitDisplayInfo>(producer)
                    ? em.GetComponentData<UnitDisplayInfo>(producer).Name.ToString()
                    : building.StableId.ToString();
                DynamicBuffer<OperationMapBuildingUnitProductionRequest> queue =
                    em.GetBuffer<OperationMapBuildingUnitProductionRequest>(producer, true);
                for (int requestIndex = 0; requestIndex < queue.Length; requestIndex++)
                {
                    OperationMapBuildingUnitProductionRequest request = queue[requestIndex];
                    if (request.Status != OperationMapBuildingUnitProductionRequest.Pending)
                        continue;

                    GameObject prefab = null;
                    context.TryResolveLiveUnitPreviewPrefab?.Invoke(request.UnitPrefab, out prefab);
                    if (prefab == null)
                    {
                        string normalizedSourceKey = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                            request.UnitSourceKey.ToString());
                        if (!string.IsNullOrEmpty(normalizedSourceKey) &&
                            productionContext.UnitSpawnPrefabsByKey != null)
                        {
                            productionContext.UnitSpawnPrefabsByKey.TryGetValue(normalizedSourceKey, out prefab);
                        }
                    }

                    if (prefab == null)
                    {
                        continue;
                    }

                    float duration = Mathf.Max(0.01f, request.ReadyAt - request.QueuedAt);
                    float remaining = Mathf.Max(0f, request.ReadyAt - now);
                    float progress = Mathf.Clamp01((now - request.QueuedAt) / duration);
                    entries.Add(new BuildingUiQueryUiSystemHelper.PendingProductionUiEntry(
                        building.PlacementIndex,
                        -1,
                        prefab,
                        remaining,
                        duration,
                        progress,
                        request.QueuedAt,
                        request.ReadyAt,
                        producerDisplayName));
                }
            }
        }

        private static bool TryAddProducedUnitsFromReadModel(
            RuntimeBuildingEntity building,
            EntityManager em,
            List<Entity> results)
        {
            if (!TryGetProducedUnitReadModelRows(em, out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits))
                return false;

            bool matchedBuilding = false;
            for (int i = 0; i < producedUnits.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnits[i];
                if (producedUnit.BuildingRuntimeId != building.Id)
                    continue;

                matchedBuilding = true;
                if (IsProducedUnitAlive(producedUnit.Unit, em))
                    results.Add(producedUnit.Unit);
            }

            return matchedBuilding;
        }

        private static bool TryAddProducedUnitEntriesFromReadModel(
            BuildingUiQueryUiSystemHelper.Context context,
            RuntimeBuildingEntity building,
            EntityManager em,
            List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry> entries)
        {
            if (!TryGetProducedUnitReadModelRows(em, out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits))
                return false;

            bool matchedBuilding = false;
            for (int i = 0; i < producedUnits.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnits[i];
                if (producedUnit.BuildingRuntimeId != building.Id)
                    continue;

                matchedBuilding = true;
                Entity unit = producedUnit.Unit;
                if (!IsProducedUnitAlive(unit, em))
                    continue;

                GameObject prefab = null;
                context.TryResolveLiveUnitPreviewPrefab?.Invoke(unit, out prefab);
                entries.Add(new BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry(unit, prefab, true, 1f));
            }

            return matchedBuilding;
        }

        private static bool TryGetProducedUnitReadModelRows(
            EntityManager em,
            out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits)
        {
            producedUnits = default;
            if (em.World == null || !em.World.IsCreated)
                return false;

            using EntityQuery boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            if (boundaryQuery.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<ArchetypeChunk> boundaryChunks =
                boundaryQuery.ToArchetypeChunkArray(Allocator.Temp);
            if (boundaryChunks.Length == 0)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            NativeArray<Entity> firstChunkEntities = boundaryChunks[0].GetNativeArray(entityType);
            if (firstChunkEntities.Length == 0)
                return false;

            Entity boundaryEntity = firstChunkEntities[0];
            if (boundaryEntity == Entity.Null ||
                !em.Exists(boundaryEntity) ||
                !em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
            {
                return false;
            }

            producedUnits = em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            return true;
        }

        private static bool IsProducedUnitAlive(Entity unit, EntityManager em)
        {
            if (unit == Entity.Null ||
                em.World == null ||
                !em.World.IsCreated ||
                !em.Exists(unit))
            {
                return false;
            }

            return !em.HasComponent<UnitHealth>(unit) ||
                   em.GetComponentData<UnitHealth>(unit).Current > 0;
        }

        private static bool IsFriendlyProductionBuilding(RuntimeBuildingEntity building)
        {
            if (building == null)
                return false;

            return !building.HasOwnerFaction ||
                   building.OwnerFactionId == FactionIdentity.NeutralFactionId ||
                   building.OwnerFactionId == FactionIdentity.PlayerFactionId;
        }

        private static string ResolveProducerDisplayName(int buildingId, RuntimeBuildingEntity building)
        {
            string displayName = building != null && building.Definition != null
                ? building.Definition.DisplayName
                : string.Empty;
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Building {buildingId}"
                : displayName;
        }
    }
}
