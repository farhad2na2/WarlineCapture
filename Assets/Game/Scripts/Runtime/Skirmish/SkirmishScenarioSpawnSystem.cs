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

        private static void TrySpawn(EntityManager em, Entity entity)
        {
            SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            if (session.IsLegacy != 0 || session.InitializationComplete == 0 || session.SpawnComplete != 0)
                return;
            if (session.Phase == SkirmishSessionPhase.Failed || session.Phase == SkirmishSessionPhase.Finished)
                return;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
            {
                session.FailureCode = SkirmishReasonCode.MissingResolvedSetup;
                session.Phase = SkirmishSessionPhase.Failed;
                em.SetComponentData(entity, session);
                return;
            }

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
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

            if (setup.Structures != null)
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
            return CreateForceMember(em, sessionId, force, forceIndex, member, perMemberSupply, prefabKey, null, null);
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
            Entity owned = fromPrefab
                ? em.Instantiate(prefabEntity)
                : em.CreateEntity();
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
                        : SkirmishVisualSpawnService.ResolveUnitFallbackWorld(force.FactionId, member);
                PlaceOnMap(em, owned, position);
                // Rendered by the shared impostor/model presentation, not a per-unit
                // GameObject; the visual attach pass must skip prefab instances.
                SetOrAdd(em, owned, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
                // The session keeps its fog-aware objective combat rules; shared
                // auto-engagement must not open a second combat path on these units.
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
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryTag), typeof(UnitPrefabRegistryEntry));
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

        private static void PlaceOnMap(EntityManager em, Entity entity, Vector3 position)
        {
            var pos = new float3(position.x, position.y, position.z);
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (!grids.IsEmptyIgnoreFilter)
            {
                GridConfig grid = grids.GetSingleton<GridConfig>();
                int2 cell = GridUtils.WorldToCell(grid, pos);
                if (cell.x >= 0 && cell.y >= 0 && cell.x < grid.Width && cell.y < grid.Height)
                {
                    default(MapSurfaceSpawnGrounding).TryGroundCellCenter(em, grid, cell, ref pos, out _);
                    SetOrAdd(em, entity, new UnitGrid { Cell = cell });
                }
            }

            SetOrAdd(em, entity, LocalTransform.FromPosition(pos));
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
            em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new FixedString64Bytes(structure.StructureId + "." + structure.FactionId),
                FactionId = structure.FactionId,
                IsStructure = 1
            });
            em.AddComponentData(owned, new SkirmishStructureIdentityComponent
            {
                StructureId = new FixedString64Bytes(structure.StructureId ?? string.Empty),
                Producer = SkirmishStructureIds.ProducerFor(structure.StructureId),
                DesignatedBase = (byte)(structure.DesignatedBase ? 1 : 0)
            });
            if (structure.DesignatedBase)
            {
                em.AddComponentData(owned, new SkirmishObjectiveRoleComponent
                {
                    Role = structure.FactionId == 1
                        ? SkirmishObjectiveRoleKind.PlayerBase
                        : SkirmishObjectiveRoleKind.EnemyBase,
                    StableObjectId = new FixedString64Bytes(structure.ObjectiveRoleId),
                    FactionId = structure.FactionId
                });
            }

            em.AddComponentData(owned, new Faction { Id = structure.FactionId });
            string visual = SkirmishStructureIds.VisualKey(structure.StructureId);
            if (!string.IsNullOrEmpty(visual))
                em.AddComponentData(owned, new UnitSourcePrefabKey { Value = new FixedString64Bytes(visual) });
            if (structure.StructureId == SkirmishStructureIds.GroundStaging)
            {
                em.AddComponentData(owned, new SkirmishGroundStagingStateComponent
                {
                    VehicleQueues = 1,
                    LogisticsQueues = 1
                });
            }

            return owned;
        }

        public static void DestroyAttemptOwned(EntityManager em, FixedString64Bytes sessionId)
        {
            using EntityQuery query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    continue;
                SkirmishVisualSpawnService.DestroyVisual(em, entities[i]);
                em.DestroyEntity(entities[i]);
            }
        }
    }
}
