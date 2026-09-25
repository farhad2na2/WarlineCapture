using System.Collections.Generic;
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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishSessionInitializationSystem))]
    public partial struct SkirmishScenarioSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // Streamed map scenery must not join either authored force. In a player
            // session, wait until the loaded map and baked prefab registry exist so
            // the ledger is not marked Playing in the menu scene.
            SkirmishWorldSetup.NormalizeScenery(em);
            if (Application.isPlaying && !MapAndRegistryReady(em))
                return;
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<SkirmishExpandedSessionComponent>());
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                    TrySpawn(em, entities[i]);
            }
            finally
            {
                entities.Dispose();
            }
        }

        private static bool MapAndRegistryReady(EntityManager em)
        {
            using var map = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            using var surface = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            // CustomGameStartup strips UnitPrefabRegistryTag after rebuilding the
            // buffer. The buffer is the registry; requiring the tag deadlocks spawn.
            using var prefabs = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            return !map.IsEmptyIgnoreFilter && !surface.IsEmptyIgnoreFilter && !prefabs.IsEmptyIgnoreFilter;
        }

        private static void TrySpawn(EntityManager em, Entity entity)
        {
            SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            if (session.IsLegacy != 0 || session.InitializationComplete == 0 || session.SpawnComplete != 0)
                return;
            if (session.Phase == SkirmishSessionPhase.Failed || session.Phase == SkirmishSessionPhase.Finished ||
                session.Phase == SkirmishSessionPhase.Cleaning)
                return;
            // In a player session, wait for the loaded map and its baked prefab
            // registry. The ledger must not become Playing in the menu scene.
            if (Application.isPlaying)
            {
                using var map = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                using var surface = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                using var prefabs = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
                if (map.IsEmptyIgnoreFilter || surface.IsEmptyIgnoreFilter || prefabs.IsEmptyIgnoreFilter)
                    return;
            }
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
            {
                session.FailureCode = SkirmishReasonCode.MissingResolvedSetup;
                session.Phase = SkirmishSessionPhase.Failed;
                em.SetComponentData(entity, session);
                return;
            }

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
            if (Application.isPlaying && !SkirmishStartingBuildingsService.Step(em, entity, setup, out var buildingReason))
            {
                if (buildingReason != SkirmishReasonCode.None)
                {
                    SkirmishStartingBuildingsService.Cancel(em, entity);
                    DestroyAttemptOwned(em, session.SessionId);
                    session.FailureCode = buildingReason;
                    session.Phase = SkirmishSessionPhase.Failed;
                    em.SetComponentData(entity, session);
                }
                return;
            }
            if (!TrySpawnLedgers(em, entity, setup, out SkirmishReasonCode reason, out byte visualPending))
            {
                DestroyAttemptOwned(em, session.SessionId);
                session.FailureCode = reason;
                session.Phase = SkirmishSessionPhase.Failed;
                em.SetComponentData(entity, session);
                return;
            }

            session.SpawnComplete = 1;
            session.SpawnVisualPending = visualPending;
            session.Phase = SkirmishSessionPhase.Playing;
            if (em.HasComponent<SkirmishObjectiveStateComponent>(entity))
            {
                var objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                objective.State = SkirmishObjectiveStateKind.Active;
                em.SetComponentData(entity, objective);
            }

            if (!em.HasComponent<SkirmishObjectiveClockComponent>(entity))
            {
                em.AddComponentData(entity, new SkirmishObjectiveClockComponent
                {
                    ElapsedSeconds = 0f,
                    DeadlineSeconds = setup.DeadlineSeconds,
                    Paused = 0,
                    Playing = 1
                });
            }

            em.SetComponentData(entity, session);
            SkirmishExpandedSessionControlService.ProjectMatchPhase(em, entity);
        }

        public static bool TrySpawnLedgers(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            out SkirmishReasonCode reason,
            out byte visualPending)
        {
            reason = SkirmishReasonCode.None;
            visualPending = 0;
            if (setup?.Forces == null)
            {
                reason = SkirmishReasonCode.MissingResolvedSetup;
                return false;
            }

            // Wait until the loaded map has published its faction entities. Seeding
            // in the menu creates a duplicate bank when the baked economy streams in.
            if (Application.isPlaying)
                SkirmishMaterialsService.Initialize(em, session, setup);

            if (!em.HasComponent<SkirmishCapacityComponent>(session))
            {
                em.AddComponentData(session, new SkirmishCapacityComponent
                {
                    FactionId = 1,
                    InfantryCap = setup.InfantryCapEach,
                    GroundCap = setup.GroundCapEach,
                    AirCap = setup.TacticalAirCapEach,
                    SupplyCap = setup.SupplyCapEach
                });
            }

            if (!em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEconomyStockComponent
                {
                    FactionId = 1,
                    Materials = setup.MaterialsEach,
                    Oil = setup.OilEach,
                    Fuel = setup.UsableFuelEach,
                    MaterialsCapacity = setup.MaterialsCapacityEach,
                    OilCapacity = setup.OilCapacityEach,
                    FuelCapacity = setup.FuelCapacityEach
                });
            }

            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);

            SkirmishArmyGroupSystem.EnsureSession(em, session, setup);
            SkirmishResearchService.EnsureSession(em, session, setup);
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            // The shared baked prefab registry is the compliant spawn source. Units
            // instantiated from it render and move through the same entity pipeline as
            // every other mode; the bare-entity fallback only covers Editor fixtures
            // without a baked registry and keeps SpawnVisualPending set.
            Dictionary<string, Entity> prefabLookup = BuildPrefabEntityLookup(em);
            bool startingSetBound = true;
            int infantryLive = 0;
            int groundLive = 0;
            int airLive = 0;
            int supplyLive = 0;
            int enemyInfantryLive = 0;
            int enemyGroundLive = 0;
            int enemyAirLive = 0;
            int enemySupplyLive = 0;
            uint openGroupId = 0;
            byte openFaction = 0;
            SkirmishRoleKind openRole = SkirmishRoleKind.None;
            int openFill = 0;

            for (int i = 0; i < setup.Forces.Length; i++)
            {
                SkirmishResolvedForceEntry force = setup.Forces[i];
                int quantity = force.Quantity < 1 ? 1 : force.Quantity;
                int perMemberSupply = quantity == 0 ? 0 : force.SupplyCost / quantity;
                string prefabKey = force.RuntimePrefabKey ?? string.Empty;
                if (string.IsNullOrEmpty(prefabKey))
                    startingSetBound = false;
                SkirmishPopulationCategory category = SkirmishRoleIds.Category(force.RoleKind);
                for (int member = 0; member < quantity; member++)
                {
                    Entity spawned = CreateForceMember(
                        em,
                        sessionId,
                        force,
                        i,
                        member,
                        perMemberSupply,
                        prefabKey,
                        prefabLookup,
                        setup);
                    if (spawned == Entity.Null)
                    {
                        DestroyAttemptOwned(em, sessionId);
                        reason = SkirmishReasonCode.BlockedSpawn;
                        return false;
                    }
                    bool infantry = category == SkirmishPopulationCategory.Infantry;
                    bool needNew = openGroupId == 0 ||
                                   force.FactionId != openFaction ||
                                   force.RoleKind != openRole ||
                                   !infantry ||
                                   openFill >= SkirmishRoleIds.InfantrySquadMembers;
                    if (needNew)
                    {
                        openGroupId = SkirmishArmyGroupSystem.OpenGroup(
                            em, session, force.FactionId, force.RoleKind, category);
                        openFaction = force.FactionId;
                        openRole = force.RoleKind;
                        openFill = 0;
                    }

                    SkirmishArmyGroupSystem.BindMember(em, session, spawned, openGroupId);
                    openFill++;
                    if (category == SkirmishPopulationCategory.Infantry ||
                        category == SkirmishPopulationCategory.Ground ||
                        category == SkirmishPopulationCategory.Air)
                    {
                        if (force.FactionId == 1)
                        {
                            if (category == SkirmishPopulationCategory.Infantry)
                                infantryLive++;
                            else if (category == SkirmishPopulationCategory.Ground)
                                groundLive++;
                            else
                                airLive++;
                            supplyLive += perMemberSupply;
                        }
                        else if (force.FactionId == 2)
                        {
                            if (category == SkirmishPopulationCategory.Infantry)
                                enemyInfantryLive++;
                            else if (category == SkirmishPopulationCategory.Ground)
                                enemyGroundLive++;
                            else
                                enemyAirLive++;
                            enemySupplyLive += perMemberSupply;
                        }
                    }
                }
            }

            if (setup.Structures != null && !em.HasComponent<SkirmishSharedBuildingsReady>(session))
            {
                for (int i = 0; i < setup.Structures.Length; i++)
                {
                    if (string.IsNullOrEmpty(SkirmishStructureIds.VisualKey(setup.Structures[i].StructureId)))
                        startingSetBound = false;
                    CreateStructure(em, sessionId, setup.Structures[i]);
                }
            }

            visualPending = (byte)(startingSetBound ? 0 : 1);
            if (em.HasComponent<SkirmishCapacityComponent>(session))
            {
                SkirmishCapacityComponent capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
                var snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
                SkirmishCapacityLedger.SeedLive(ref snapshot, infantryLive, groundLive, airLive, supplyLive);
                em.SetComponentData(session, SkirmishCapacityLedger.FromSnapshot(capacity, snapshot));
            }

            if (!em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEnemyStockComponent
                {
                    Materials = setup.MaterialsEach,
                    Oil = setup.OilEach,
                    Fuel = setup.UsableFuelEach,
                    MaterialsCapacity = setup.MaterialsCapacityEach,
                    OilCapacity = setup.OilCapacityEach,
                    FuelCapacity = setup.FuelCapacityEach
                });
            }

            if (!em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEnemyCapacityComponent
                {
                    InfantryCap = setup.InfantryCapEach,
                    GroundCap = setup.GroundCapEach,
                    AirCap = setup.TacticalAirCapEach,
                    SupplyCap = setup.SupplyCapEach,
                    NextReservationId = 1000
                });
            }

            if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var enemyCap = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                enemyCap.InfantryLive = enemyInfantryLive;
                enemyCap.GroundLive = enemyGroundLive;
                enemyCap.AirLive = enemyAirLive;
                enemyCap.SupplyLive = enemySupplyLive;
                em.SetComponentData(session, enemyCap);
            }

            if (!em.HasComponent<SkirmishEnemyStrategyComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEnemyStrategyComponent
                {
                    Priority = SkirmishStrategyPriority.Reserve,
                    Personality = (byte)(setup.Seed % 3)
                });
            }

            if (Application.isPlaying && !SkirmishStartingSupplyService.Initialize(em, session, setup))
            {
                reason = SkirmishReasonCode.MissingReference;
                return false;
            }
            SkirmishFogService.Project(em, session);
            return true;
        }

        public static Entity CreateForceMember(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishResolvedForceEntry force,
            int forceIndex,
            int member,
            int perMemberSupply,
            string prefabKey)
        {
            return CreateForceMember(em, sessionId, force, forceIndex, member, perMemberSupply, prefabKey, BuildPrefabEntityLookup(em), null);
        }

        internal static Entity CreateForceMember(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishResolvedForceEntry force,
            int forceIndex,
            int member,
            int perMemberSupply,
            string prefabKey,
            Dictionary<string, Entity> prefabLookup,
            SkirmishResolvedSetup setup)
        {
            bool fromPrefab = TryResolvePrefabEntity(prefabLookup, prefabKey, out Entity prefabEntity);
            if (!fromPrefab && Application.isPlaying)
                return Entity.Null;
            Entity owned = fromPrefab
                ? em.Instantiate(prefabEntity)
                : em.CreateEntity();
            if (fromPrefab) SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, owned);
            SetOrAdd(em, owned, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new FixedString64Bytes(force.RoleId + "." + force.FactionId + "." + forceIndex + "." + member),
                FactionId = force.FactionId,
                IsStructure = 0
            });
            SetOrAdd(em, owned, new SkirmishUnitRoleComponent
            {
                Role = force.RoleKind,
                Category = SkirmishRoleIds.Category(force.RoleKind),
                SupplyCost = perMemberSupply
            });
            SetOrAdd(em, owned, new Faction { Id = force.FactionId });
            if (!string.IsNullOrEmpty(prefabKey))
                SetOrAdd(em, owned, new UnitSourcePrefabKey { Value = new FixedString64Bytes(prefabKey) });
            if (fromPrefab)
            {
                Vector3 position = setup != null &&
                    SkirmishVisualSpawnService.TryResolveUnitSpawnWorld(setup, force.FactionId, force.RoleKind, member, out Vector3 measured)
                        ? measured
                        : force.SpawnWorldX != 0f || force.SpawnWorldZ != 0f
                            ? new Vector3(force.SpawnWorldX, 0f, force.SpawnWorldZ)
                            : SkirmishVisualSpawnService.ResolveUnitFallbackWorld(force.FactionId, member);
                if (!TryPlaceOnMap(em, owned, position))
                {
                    em.DestroyEntity(owned);
                    return Entity.Null;
                }
                // Rendered by the shared impostor/model presentation, not a per-unit
                // GameObject; the visual attach pass must skip prefab instances.
                SetOrAdd(em, owned, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
                SetOrAdd(em, owned, new SkirmishSharedActorTag());
                // Keep the unit inert until roster projection has applied its shared
                // weapon stats and target policy; that owner then removes this tag.
                if (!em.HasComponent<CampaignMissionCombatSuppressedTag>(owned))
                    em.AddComponentData(owned, new CampaignMissionCombatSuppressedTag());
            }

            return owned;
        }

        private static bool TryResolvePrefabEntity(
            Dictionary<string, Entity> prefabLookup,
            string prefabKey,
            out Entity prefabEntity)
        {
            prefabEntity = Entity.Null;
            return prefabLookup != null &&
                   !string.IsNullOrEmpty(prefabKey) &&
                   prefabLookup.TryGetValue(prefabKey, out prefabEntity) &&
                   prefabEntity != Entity.Null;
        }

        internal static Dictionary<string, Entity> BuildPrefabEntityLookup(EntityManager em)
        {
            var lookup = new Dictionary<string, Entity>(System.StringComparer.OrdinalIgnoreCase);
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            using NativeArray<Entity> registries = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < registries.Length; i++)
            {
                if (!em.HasBuffer<UnitPrefabRegistryEntry>(registries[i]))
                    continue;
                DynamicBuffer<UnitPrefabRegistryEntry> entries = em.GetBuffer<UnitPrefabRegistryEntry>(registries[i]);
                for (int j = 0; j < entries.Length; j++)
                {
                    Entity prefab = entries[j].Prefab;
                    if (prefab == Entity.Null || !em.Exists(prefab) || !em.HasComponent<UnitSourcePrefabKey>(prefab))
                        continue;
                    string key = em.GetComponentData<UnitSourcePrefabKey>(prefab).Value.ToString();
                    if (key.Length == 0)
                        continue;
                    lookup[key] = prefab;
                }
            }

            return lookup;
        }

        private static bool TryPlaceOnMap(EntityManager em, Entity entity, Vector3 position)
        {
            var pos = new float3(position.x, position.y, position.z);
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (!grids.IsEmptyIgnoreFilter)
            {
                Entity gridEntity = grids.GetSingletonEntity();
                GridConfig grid = grids.GetSingleton<GridConfig>();
                int2 cell = GridUtils.WorldToCell(grid, pos);
                if (em.HasBuffer<GridWalkable>(gridEntity) &&
                    em.HasComponent<DynamicBlockerComponent>(gridEntity) &&
                    em.HasComponent<DynamicOccupancyComponent>(gridEntity))
                {
                    var blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
                    var occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
                    var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
                    var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp, NativeArrayOptions.ClearMemory);
                    try
                    {
                        using var units = em.CreateEntityQuery(typeof(UnitGrid), typeof(UnitFootprint));
                        using var existing = units.ToEntityArray(Allocator.Temp);
                        for (int i = 0; i < existing.Length; i++)
                        {
                            if (existing[i] == entity) continue;
                            int2 size = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(existing[i]).Size);
                            int2 min = UnitFootprintUtility.GetMinCell(em.GetComponentData<UnitGrid>(existing[i]).Cell, size);
                            for (int y = min.y; y < min.y + size.y; y++)
                                for (int x = min.x; x < min.x + size.x; x++)
                                    if (GridUtils.InBounds(new int2(x, y), grid.Width, grid.Height))
                                        reserved.Set(y * grid.Width + x, true);
                        }
                        var rng = new Unity.Mathematics.Random(math.max(1u, math.hash(new int3(cell, entity.Index))));
                        int2 footprint = em.HasComponent<UnitFootprint>(entity)
                            ? em.GetComponentData<UnitFootprint>(entity).Size : new int2(1, 1);
                        if (!InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref rng, grid, walkable,
                            blocked, occupied, ref reserved, cell, 12, footprint,
                            em.HasComponent<UnitAirMovement>(entity), out cell)) return false;
                        pos = GridUtils.CellToWorldCenter(grid, cell);
                    }
                    finally { reserved.Dispose(); }
                }
                if (GridUtils.InBounds(cell, grid.Width, grid.Height))
                {
                    default(MapSurfaceSpawnGrounding).TryGroundCellCenter(em, grid, cell, ref pos, out _);
                    SetOrAdd(em, entity, new UnitGrid { Cell = cell });
                }
            }

            SetOrAdd(em, entity, LocalTransform.FromPosition(pos));
            return true;
        }

        private static void SetOrAdd<T>(EntityManager em, Entity entity, T component) where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity))
                em.SetComponentData(entity, component);
            else
                em.AddComponentData(entity, component);
        }

        public static Entity CreateStructure(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishResolvedStructureEntry structure)
        {
            var owned = em.CreateEntity();
            BindStructure(em, owned, sessionId, structure);
            return owned;
        }

        public static void BindStructure(EntityManager em, Entity owned, FixedString64Bytes sessionId,
            SkirmishResolvedStructureEntry structure)
        {
            SetOrAdd(em, owned, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new FixedString64Bytes(structure.StructureId + "." + structure.FactionId),
                FactionId = structure.FactionId,
                IsStructure = 1
            });
            SetOrAdd(em, owned, new SkirmishStructureIdentityComponent
            {
                StructureId = new FixedString64Bytes(structure.StructureId ?? string.Empty),
                Producer = SkirmishStructureIds.ProducerFor(structure.StructureId),
                DesignatedBase = (byte)(structure.DesignatedBase ? 1 : 0)
            });
            if (structure.DesignatedBase)
            {
                SetOrAdd(em, owned, new SkirmishObjectiveRoleComponent
                {
                    Role = structure.FactionId == 1
                        ? SkirmishObjectiveRoleKind.PlayerBase
                        : SkirmishObjectiveRoleKind.EnemyBase,
                    StableObjectId = new FixedString64Bytes(structure.ObjectiveRoleId),
                    FactionId = structure.FactionId
                });
            }

            SetOrAdd(em, owned, new Faction { Id = structure.FactionId });
            SetOrAdd(em, owned, new CombatTargetPolicy
            { Domain = CombatTargetDomain.Structure, AllowedTargets = CombatTargetDomain.None, Visible = 0 });
            string visual = SkirmishStructureIds.VisualKey(structure.StructureId);
            if (!string.IsNullOrEmpty(visual))
                SetOrAdd(em, owned, new UnitSourcePrefabKey { Value = new FixedString64Bytes(visual) });
            if (structure.StructureId == SkirmishStructureIds.GroundStaging)
            {
                SetOrAdd(em, owned, new SkirmishGroundStagingStateComponent
                {
                    VehicleQueues = 1,
                    LogisticsQueues = 1
                });
            }

        }

        // Called only by the player construction commit, never by a census of
        // existing scenery. Replacements belong to the attempt but never inherit
        // the designated objective identity of the original Barracks.
        internal static bool AdoptConstructedBuilding(EntityManager em, Entity building, string prefabKey)
        {
            if (building == Entity.Null || !em.Exists(building) ||
                !em.HasComponent<RuntimeBuildingCombatInfo>(building) ||
                em.HasComponent<OperationMapBuildingComponent>(building) ||
                em.HasComponent<SkirmishAttemptOwnedComponent>(building)) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) return false;
            var state = sessions.GetSingleton<SkirmishExpandedSessionComponent>();
            if (state.IsLegacy != 0 || state.Phase != SkirmishSessionPhase.Playing) return false;
            var info = em.GetComponentData<RuntimeBuildingCombatInfo>(building);
            if (info.OwnerFactionId != 1) return false;
            BindStructure(em, building, state.SessionId, new SkirmishResolvedStructureEntry
            {
                FactionId = info.OwnerFactionId,
                StructureId = SkirmishStructureIds.FromVisualKey(prefabKey),
                DesignatedBase = false
            });
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
            owner.StableObjectId = new FixedString64Bytes("constructed." + info.RuntimeBuildingId);
            em.SetComponentData(building, owner);
            SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, building);
            return true;
        }

        public static void DestroyAttemptOwned(EntityManager em, FixedString64Bytes sessionId)
        {
            using EntityQuery query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    continue;
                if (em.HasComponent<RuntimeBuildingCombatInfo>(entities[i]) && em.HasComponent<UnitHealth>(entities[i]))
                {
                    var health = em.GetComponentData<UnitHealth>(entities[i]);
                    health.Current = 0;
                    em.SetComponentData(entities[i], health);
                    // The shared building owner removes its visual, blocker and storage.
                    continue;
                }
                SkirmishVisualSpawnService.DestroyVisual(em, entities[i]);
                em.DestroyEntity(entities[i]);
            }
        }
    }
}
