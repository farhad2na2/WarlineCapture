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

        public static bool TryResolveBoundUnitPrefab(EntityManager em, Entity unit, out GameObject prefab)
        {
            prefab = null;
            if (!em.Exists(unit) || !em.HasComponent<SkirmishAttemptOwnedComponent>(unit) ||
                !em.HasComponent<UnitSourcePrefabKey>(unit)) return false;
            var id = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishVisualPrefabCatalogRecord));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            foreach (var session in entities)
            {
                if (!em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.Equals(id)) continue;
                var catalog = em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session).Catalog;
                return catalog != null && catalog.BoundFromRegistry &&
                    catalog.TryGet(em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString(), out prefab);
            }
            return false;
        }

        public static SkirmishVisualPrefabCatalog EnsureCatalog(EntityManager em, Entity session)
        {
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
            {
                var existing = em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session);
                if (existing.Catalog != null)
                    return existing.Catalog;
            }

            SkirmishVisualPrefabCatalog catalog = BindSceneRegistry(em, session, null, false);
            if (catalog != null)
                return catalog;

            catalog = SkirmishVisualPrefabCatalog.CreatePresentationStandIns();
            BindCatalog(em, session, catalog);
            return catalog;
        }

        public static SkirmishVisualPrefabCatalog BindSceneRegistry(
            EntityManager em,
            Entity session,
            UnitPrefabRegistryAuthoringConfig explicitRegistry,
            bool createStandInsIfMissing = false)
        {
            if (em == default || session == Entity.Null)
                return null;

            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
            {
                var existing = em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session);
                if (existing.Catalog != null &&
                    (explicitRegistry == null || existing.Catalog.BoundFromRegistry))
                    return existing.Catalog;
            }

            UnitPrefabRegistryAuthoringConfig registry = explicitRegistry ?? FindSceneRegistry();
            var catalog = new SkirmishVisualPrefabCatalog();
            if (registry != null)
                catalog.BindRegistry(registry);
            BindPlacementSpawnables(catalog);
            EnsureAuthoredGroundStaging(catalog);
            EnsureAuthoredBarracks(catalog);

            if (catalog.Count == 0)
            {
                catalog.Dispose();
                if (!createStandInsIfMissing)
                    return null;
                catalog = SkirmishVisualPrefabCatalog.CreatePresentationStandIns();
            }

            BindCatalog(em, session, catalog);
            return catalog;
        }

        public static void BindCatalog(EntityManager em, Entity session, SkirmishVisualPrefabCatalog catalog)
        {
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
            {
                var existing = em.GetComponentObject<SkirmishVisualPrefabCatalogRecord>(session);
                if (existing.Catalog != null && existing.Catalog != catalog)
                    existing.Catalog.Dispose();
                existing.Catalog = catalog;
                return;
            }

            em.AddComponentObject(session, new SkirmishVisualPrefabCatalogRecord { Catalog = catalog });
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
                // The shared building runtime already owns this GameObject and its
                // transform. Attaching a skirmish visual here duplicates the model
                // and resets the committed pose to the deployment anchor.
                if (em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                {
                    memberIndex++;
                    continue;
                }
                Vector3 position = ResolvePosition(em, session, setup, entity, owned, memberIndex);
                position = GroundPosition(em, position);
                if (TryAttach(em, entity, catalog, position))
                    spawned++;
                memberIndex++;
            }

            // The live unit registry binds without Building_Barrack, so TryAttach
            // leaves the designated base with health and no transform. Attack then
            // stores the 40m stand-in and never enters range. Combat still needs
            // the measured pad when the prefab is missing.
            EnsureMissingCombatTransforms(em, session);
            return spawned;
        }

        public static int EnsureMissingCombatTransforms(EntityManager em, Entity session)
        {
            if (em == default || session == Entity.Null || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return 0;

            SkirmishResolvedSetup setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup
                : null;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int placed = 0;
            int memberIndex = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || !em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                    continue;
                SkirmishAttemptOwnedComponent owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId))
                    continue;
                int index = memberIndex;
                memberIndex++;
                if (em.HasComponent<LocalTransform>(entity))
                    continue;
                Vector3 position = ResolvePosition(em, session, setup, entity, owned, index);
                position = GroundPosition(em, position);
                em.AddComponentData(entity, LocalTransform.FromPosition(new float3(position.x, position.y, position.z)));
                placed++;
            }

            return placed;
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

        public static bool TryResolveUnitSpawnWorld(
            SkirmishResolvedSetup setup,
            byte factionId,
            SkirmishRoleKind role,
            int memberIndex,
            out Vector3 world)
        {
            return TryResolveMeasuredWorld(setup, factionId, false, string.Empty, role, memberIndex, out world);
        }

        /// <summary>
        /// No-layout fallback pad offset. Prefab instances must always receive a
        /// deliberate position; a baked prefab origin is never a deployment.
        /// </summary>
        public static Vector3 ResolveUnitFallbackWorld(byte factionId, int memberIndex)
        {
            Vector3 pad = StagingWorld(factionId, true);
            int column = memberIndex % 4;
            int row = memberIndex / 4;
            float side = factionId == 2 ? -1f : 1f;
            return pad + new Vector3(column * 2.2f * side, 0f, -row * 2.2f);
        }

        public static bool TryResolveMeasuredWorld(
            SkirmishResolvedSetup setup,
            byte factionId,
            bool isStructure,
            string structureId,
            SkirmishRoleKind role,
            int memberIndex,
            out Vector3 world)
        {
            world = default;
            if (setup == null || !setup.MeasuredLayoutBound)
                return false;

            if (isStructure)
            {
                if (TryFindStructure(setup, factionId, structureId, out SkirmishResolvedStructureEntry structure) &&
                    (structure.SpawnWorldX != 0f || structure.SpawnWorldZ != 0f || setup.PlayerStagingWorldX != 0f))
                {
                    world = new Vector3(structure.SpawnWorldX, 0f, structure.SpawnWorldZ);
                    return true;
                }

                if (structureId == SkirmishStructureIds.GroundStaging)
                {
                    world = factionId == 2
                        ? new Vector3(setup.EnemyStagingWorldX, 0f, setup.EnemyStagingWorldZ)
                        : new Vector3(setup.PlayerStagingWorldX, 0f, setup.PlayerStagingWorldZ);
                    return world.x != 0f || world.z != 0f;
                }

                world = factionId == 2
                    ? new Vector3(setup.EnemyBaseWorldX, 0f, setup.EnemyBaseWorldZ)
                    : new Vector3(setup.PlayerBaseWorldX, 0f, setup.PlayerBaseWorldZ);
                return world.x != 0f || world.z != 0f;
            }

            if (TryFindForce(setup, factionId, role, out SkirmishResolvedForceEntry force) &&
                (force.SpawnWorldX != 0f || force.SpawnWorldZ != 0f))
            {
                world = OffsetOnPad(force.SpawnWorldX, force.SpawnWorldZ, factionId, memberIndex);
                return true;
            }

            float padX = factionId == 2 ? setup.EnemySpawnPadX : setup.PlayerSpawnPadX;
            float padZ = factionId == 2 ? setup.EnemySpawnPadZ : setup.PlayerSpawnPadZ;
            if (padX == 0f && padZ == 0f)
                return false;
            world = OffsetOnPad(padX, padZ, factionId, memberIndex);
            return true;
        }

        private static Vector3 ResolvePosition(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            Entity entity,
            SkirmishAttemptOwnedComponent owned,
            int memberIndex)
        {
            _ = session;
            string structureId = owned.IsStructure != 0 && em.HasComponent<SkirmishStructureIdentityComponent>(entity)
                ? em.GetComponentData<SkirmishStructureIdentityComponent>(entity).StructureId.ToString()
                : string.Empty;
            SkirmishRoleKind role = em.HasComponent<SkirmishUnitRoleComponent>(entity)
                ? em.GetComponentData<SkirmishUnitRoleComponent>(entity).Role
                : SkirmishRoleKind.None;
            if (TryResolveMeasuredWorld(setup, owned.FactionId, owned.IsStructure != 0, structureId, role, memberIndex, out Vector3 measured))
            {
                if (owned.IsStructure != 0 && structureId == SkirmishStructureIds.GroundStaging)
                    ApplyMeasuredStagingPads(em, entity, owned.FactionId, setup);
                return measured;
            }

            if (owned.IsStructure != 0 && em.HasComponent<SkirmishStructureIdentityComponent>(entity))
            {
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

        private static void ApplyMeasuredStagingPads(
            EntityManager em,
            Entity entity,
            byte factionId,
            SkirmishResolvedSetup setup)
        {
            if (!em.HasComponent<SkirmishGroundStagingStateComponent>(entity))
                return;
            var state = em.GetComponentData<SkirmishGroundStagingStateComponent>(entity);
            state.SpawnPadX = factionId == 2 ? setup.EnemySpawnPadX : setup.PlayerSpawnPadX;
            state.SpawnPadZ = factionId == 2 ? setup.EnemySpawnPadZ : setup.PlayerSpawnPadZ;
            state.RallyPadX = factionId == 2 ? setup.EnemyRallyPadX : setup.PlayerRallyPadX;
            state.RallyPadZ = factionId == 2 ? setup.EnemyRallyPadZ : setup.PlayerRallyPadZ;
            em.SetComponentData(entity, state);
        }

        private static Vector3 OffsetOnPad(float centerX, float centerZ, byte factionId, int memberIndex)
        {
            int column = memberIndex % 4;
            int row = memberIndex / 4;
            float side = factionId == 2 ? -1f : 1f;
            return new Vector3(centerX + column * 2.2f * side, 0f, centerZ - row * 2.2f);
        }

        private static Vector3 GroundPosition(EntityManager em, Vector3 position)
        {
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1)
                return position;
            GridConfig grid = grids.GetSingleton<GridConfig>();
            float3 world = new float3(position.x, position.y, position.z);
            new MapSurfaceSpawnGrounding().TryGroundWorldPosition(em, grid, ref world, out _, out _);
            return new Vector3(world.x, world.y, world.z);
        }

        private static bool TryFindStructure(
            SkirmishResolvedSetup setup,
            byte factionId,
            string structureId,
            out SkirmishResolvedStructureEntry structure)
        {
            structure = default;
            if (setup.Structures == null)
                return false;
            for (int i = 0; i < setup.Structures.Length; i++)
            {
                if (setup.Structures[i].FactionId == factionId &&
                    setup.Structures[i].StructureId == structureId)
                {
                    structure = setup.Structures[i];
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindForce(
            SkirmishResolvedSetup setup,
            byte factionId,
            SkirmishRoleKind role,
            out SkirmishResolvedForceEntry force)
        {
            force = default;
            if (setup.Forces == null)
                return false;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].FactionId == factionId && setup.Forces[i].RoleKind == role)
                {
                    force = setup.Forces[i];
                    return true;
                }
            }

            return false;
        }

        public static UnitPrefabRegistryAuthoringConfig FindSceneRegistry()
        {
            BuildingPlacementSystemConfig[] placements =
                Resources.FindObjectsOfTypeAll<BuildingPlacementSystemConfig>();
            if (placements != null)
            {
                for (int i = 0; i < placements.Length; i++)
                {
                    UnitPrefabRegistryAuthoringConfig placed = placements[i] != null
                        ? placements[i].UnitPrefabRegistryConfig
                        : null;
                    if (placed != null && placed.UnitSpawnPrefabs != null && placed.UnitSpawnPrefabs.Count > 0)
                        return placed;
                }
            }

            UnitPrefabRegistryAuthoringConfig[] loaded =
                Resources.FindObjectsOfTypeAll<UnitPrefabRegistryAuthoringConfig>();
            return loaded != null && loaded.Length > 0 ? loaded[0] : null;
        }

        private static void BindPlacementSpawnables(SkirmishVisualPrefabCatalog catalog)
        {
            BuildingPlacementSystemConfig[] placements =
                Resources.FindObjectsOfTypeAll<BuildingPlacementSystemConfig>();
            if (placements == null)
                return;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i] == null || placements[i].Spawnables == null)
                    continue;
                for (int j = 0; j < placements[i].Spawnables.Count; j++)
                {
                    GameObject prefab = placements[i].Spawnables[j];
                    if (prefab == null || catalog.Contains(prefab.name))
                        continue;
                    catalog.Bind(prefab.name, prefab);
                }
            }
        }

        private static void EnsureAuthoredGroundStaging(SkirmishVisualPrefabCatalog catalog)
        {
            string key = SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging);
            if (catalog.Contains(key))
                return;
            if (!SkirmishGroundStagingPrefabAccess.TryLoadAuthored(out GameObject staging, out bool owned) ||
                staging == null)
                return;
            staging.SetActive(false);
            catalog.Bind(key, staging, owned);
        }

        private static void EnsureAuthoredBarracks(SkirmishVisualPrefabCatalog catalog)
        {
            string key = SkirmishStructureIds.VisualKey(SkirmishStructureIds.Barracks);
            if (catalog.Contains(key))
                return;
            GameObject barracks = Resources.Load<GameObject>("Skirmish/S002Barracks");
            if (barracks != null)
                catalog.Bind(key, barracks);
        }
    }
}
