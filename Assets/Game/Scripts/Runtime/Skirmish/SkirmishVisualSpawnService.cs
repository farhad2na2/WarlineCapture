using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishVisualSpawnService
    {
        public const float WorldExtent = 40f;

        public static SkirmishVisualPrefabCatalog EnsureCatalog(EntityManager em, Entity session)
        {
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
            {
                var existing = em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session);
                if (existing.Catalog != null)
                    return existing.Catalog;
            }

            SkirmishVisualPrefabCatalog catalog = BindLoadedRegistry() ??
                                                 SkirmishVisualPrefabCatalog.CreatePresentationStandIns();
            BindCatalog(em, session, catalog);
            return catalog;
        }

        public static void BindCatalog(EntityManager em, Entity session, SkirmishVisualPrefabCatalog catalog)
        {
            var record = new SkirmishVisualPrefabCatalogRecord { Catalog = catalog };
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
                em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog = catalog;
            else
                em.AddComponentObject(session, record);
        }

        public static int AttachMissing(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            SkirmishVisualPrefabCatalog catalog = EnsureCatalog(em, session);
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int spawned = 0;
            int memberIndex = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId))
                    continue;
                if (em.HasComponent<SkirmishVisualSpawnedComponent>(entity) &&
                    em.GetComponentData<SkirmishVisualSpawnedComponent>(entity).Spawned != 0)
                    continue;
                Vector3 position = ResolvePosition(em, session, setup, entity, owned, memberIndex);
                if (TryAttach(em, entity, catalog, position))
                    spawned++;
                memberIndex++;
            }

            return spawned;
        }

        public static bool TryAttach(
            EntityManager em,
            Entity entity,
            SkirmishVisualPrefabCatalog catalog,
            Vector3 position)
        {
            if (catalog == null || !em.HasComponent<UnitSourcePrefabKey>(entity))
                return false;

            string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!catalog.TryGet(key, out GameObject prefab))
                return false;

            GameObject instance = Object.Instantiate(prefab);
            instance.name = key;
            instance.hideFlags = HideFlags.None;
            instance.SetActive(true);
            instance.transform.position = position;
            bool fromRegistry = catalog.Contains(key);
            if (key == SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging) &&
                !SkirmishGroundStagingBuilder.HasRequiredSurfaces(instance))
            {
                SkirmishVisualLifecycle.DestroyOwned(instance);
                instance = Object.Instantiate(SkirmishGroundStagingBuilder.BuildHierarchy());
                instance.name = key;
                instance.SetActive(true);
                instance.transform.position = position;
                fromRegistry = false;
            }

            if (em.HasComponent<SkirmishVisualInstanceRecord>(entity))
            {
                var existing = em.GetComponentObject<SkirmishVisualInstanceRecord>(entity);
                if (existing.Instance != null)
                    SkirmishVisualLifecycle.DestroyOwned(existing.Instance);
                existing.Instance = instance;
                existing.PrefabKey = key;
            }
            else
            {
                em.AddComponentObject(entity, new SkirmishVisualInstanceRecord
                {
                    Instance = instance,
                    PrefabKey = key
                });
            }

            var spawned = new SkirmishVisualSpawnedComponent
            {
                Spawned = 1,
                FromRegistry = (byte)(fromRegistry ? 1 : 0)
            };
            if (em.HasComponent<SkirmishVisualSpawnedComponent>(entity))
                em.SetComponentData(entity, spawned);
            else
                em.AddComponentData(entity, spawned);

            var transform = LocalTransform.FromPosition(new float3(position.x, position.y, position.z));
            if (em.HasComponent<LocalTransform>(entity))
                em.SetComponentData(entity, transform);
            else
                em.AddComponentData(entity, transform);
            return true;
        }

        public static void DestroyVisual(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<SkirmishVisualInstanceRecord>(entity))
                return;
            var record = em.GetComponentObject<SkirmishVisualInstanceRecord>(entity);
            if (record.Instance != null)
                SkirmishVisualLifecycle.DestroyOwned(record.Instance);
            record.Instance = null;
        }

        public static Vector3 WorldFromNormalized(float u, float v)
        {
            return new Vector3((u - 0.5f) * 2f * WorldExtent, 0f, (v - 0.5f) * 2f * WorldExtent);
        }

        public static Vector3 StagingWorld(byte factionId, bool spawnPad)
        {
            float u = factionId == 2 ? 0.82f : 0.18f;
            float v = 0.50f;
            Vector3 origin = WorldFromNormalized(u, v);
            return origin + (spawnPad ? new Vector3(4.5f, 0f, 0f) : Vector3.zero);
        }

        private static Vector3 ResolvePosition(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            Entity entity,
            SkirmishAttemptOwnedComponent owned,
            int memberIndex)
        {
            _ = setup;
            if (owned.IsStructure != 0 && em.HasComponent<SkirmishStructureIdentityComponent>(entity))
            {
                string structureId = em.GetComponentData<SkirmishStructureIdentityComponent>(entity).StructureId.ToString();
                if (structureId == SkirmishStructureIds.GroundStaging)
                {
                    Vector3 staging = StagingWorld(owned.FactionId, false);
                    if (em.HasComponent<SkirmishGroundStagingStateComponent>(entity))
                    {
                        var state = em.GetComponentData<SkirmishGroundStagingStateComponent>(entity);
                        state.SpawnPadX = staging.x + 4.5f;
                        state.SpawnPadZ = staging.z;
                        state.RallyPadX = staging.x + 8.5f;
                        state.RallyPadZ = staging.z;
                        em.SetComponentData(entity, state);
                    }

                    return staging;
                }

                return StagingWorld(owned.FactionId, false) + new Vector3(owned.FactionId == 2 ? 6f : -6f, 0f, 8f);
            }

            Vector3 pad = StagingWorld(owned.FactionId, true);
            int column = memberIndex % 4;
            int row = memberIndex / 4;
            float side = owned.FactionId == 2 ? -1f : 1f;
            return pad + new Vector3(column * 2.2f * side, 0f, -row * 2.2f);
        }

        private static SkirmishVisualPrefabCatalog BindLoadedRegistry()
        {
            UnitPrefabRegistryAuthoringConfig[] loaded =
                Resources.FindObjectsOfTypeAll<UnitPrefabRegistryAuthoringConfig>();
            if (loaded == null || loaded.Length == 0)
                return null;
            var catalog = new SkirmishVisualPrefabCatalog();
            catalog.BindRegistry(loaded[0]);
            if (!catalog.Contains(SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging)))
            {
                GameObject staging = SkirmishGroundStagingBuilder.BuildHierarchy();
                staging.SetActive(false);
                catalog.Bind(SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging), staging, true);
            }
            return catalog.Count == 0 ? null : catalog;
        }
    }
}
