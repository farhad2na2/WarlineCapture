using System.Collections.Generic;
using System.Globalization;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishArmyGroupSystem))]
    [UpdateAfter(typeof(SkirmishNativeObjectiveTickSystem))]
    public partial struct SkirmishEnemyStrategySystem : ISystem
    {
        // A structure must be inside the defended base area and close enough to
        // an actual defender. This is a local response, not map-wide knowledge.
        private const float InfrastructureDefenseRadius = 90f;
        private const float InfrastructureResponseRadius = 100f;
        private EntityQuery ownedQuery;
        private EntityQuery sessions;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            ownedQuery = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>());
            sessions = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            // TryProduce creates entities. A foreach query makes that structural
            // change illegal, so the counter recruit threw before AttackBase.
            using NativeArray<Entity> entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0 || session.Phase != SkirmishSessionPhase.Playing)
                    continue;
                Evaluate(em, entity, ownedQuery, ResolveArmy(em, entity));
            }
        }

        public static SkirmishStrategyScore Evaluate(
            EntityManager em,
            Entity session,
            EntityQuery owned,
            SkirmishArmyProfileConfig army)
        {
            var score = default(SkirmishStrategyScore);
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session))
                return score;

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null)
                return score;
            army ??= SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault)
                return SkirmishEnemyObjectiveCommanderService.Evaluate(em,session,setup,army);
            if (setup.EnemyPolicy != null && setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault)
                return SkirmishEnemyCommanderService.Evaluate(em, session, setup, army);
            SkirmishPublicPerception perception = Perceive(em, session, owned);
            var current = em.HasComponent<SkirmishEnemyStrategyComponent>(session)
                ? em.GetComponentData<SkirmishEnemyStrategyComponent>(session).Priority
                : SkirmishStrategyPriority.None;
            var readiness = SkirmishResearchService.TryReadState(em, session, 2, out var research)
                ? research.Readiness : setup.Readiness;
            score = SkirmishStrategyScoring.ScoreBaseAssault(
                perception, army, readiness, setup.RoleOverlays, current);
            Execute(em, session, ref score, perception, army);
            return score;
        }

        public static SkirmishPublicPerception Perceive(EntityManager em, Entity session, EntityQuery owned)
        {
            var perception = new SkirmishPublicPerception
            {
                Playing = true,
                Finished = false,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true,
                KnowsHostileMaterials = false,
                HostileMaterialsIfKnown = 0
            };
            if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if (setup?.Forces != null)
                    foreach (var force in setup.Forces)
                        if (force.FactionId == setup.EnemyFaction)
                            perception.OwnStartingSupply += force.SupplyCost;
            }
            if (em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                perception.OwnMaterials = SkirmishMaterialsService.Read(em, session, 2);
                perception.OwnFuel = SkirmishStartingSupplyService.ReadFuel(em, session, 2);
            }

            if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var cap = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                perception.OwnInfantryLive = cap.InfantryLive;
                perception.OwnGroundLive = cap.GroundLive;
                perception.OwnAirLive = cap.AirLive;
                perception.AirCap = cap.AirCap;
                perception.OwnSupplyLive = cap.SupplyLive;
                perception.OwnSupplyCap = cap.SupplyCap;
            }

            perception.HelipadPresent = SkirmishProductionService.HasLivingProducer(
                em, session, SkirmishProducerKind.Helipad, 2);
            perception.AirportPresent = SkirmishProductionService.HasLivingProducer(
                em, session, SkirmishProducerKind.Airport, 2);
            perception.SmallAirportPresent = SkirmishProductionService.HasLivingProducer(
                em, session, SkirmishProducerKind.SmallAirport, 2);

            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                var facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                perception.PlayerDesignatedAlive = facts.PlayerDesignatedAlive != 0;
                perception.EnemyDesignatedAlive = facts.EnemyDesignatedAlive != 0;
            }

            if (em.HasComponent<SkirmishResultComponent>(session))
            {
                perception.Finished = true;
                perception.Playing = false;
            }

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using NativeArray<Entity> entities = owned.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var ownedUnit = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!ownedUnit.SessionId.Equals(sessionId) || ownedUnit.FactionId != 1 || ownedUnit.IsStructure != 0)
                    continue;
                if (!SkirmishFogService.IsVisibleToFaction(em, unit, 2) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                if (!em.HasComponent<SkirmishUnitRoleComponent>(unit))
                    continue;
                SkirmishUnitRoleComponent role = em.GetComponentData<SkirmishUnitRoleComponent>(unit);
                if (role.Category == SkirmishPopulationCategory.Infantry)
                    perception.VisibleHostileInfantry++;
                else if (role.Category == SkirmishPopulationCategory.Ground)
                {
                    perception.VisibleHostileGround++;
                    if (role.Role == SkirmishRoleKind.Tank)
                        perception.VisibleHostileTanks++;
                }
                else if (role.Category == SkirmishPopulationCategory.Air)
                    perception.VisibleHostileAir++;
            }

            return perception;
        }

        private static void Execute(
            EntityManager em,
            Entity session,
            ref SkirmishStrategyScore score,
            in SkirmishPublicPerception perception,
            SkirmishArmyProfileConfig army)
        {
            var state = em.HasComponent<SkirmishEnemyStrategyComponent>(session)
                ? em.GetComponentData<SkirmishEnemyStrategyComponent>(session)
                : new SkirmishEnemyStrategyComponent();
            if (score.Priority == SkirmishStrategyPriority.RecruitCounter && state.CounterCommitted != 0)
            {
                // One visible counter purchase, then the base assault. Evaluate runs
                // every tick; repeating TryProduce printed a rocketeer army in one second.
                score.Priority = SkirmishStrategyPriority.AttackBase;
                score.RecruitRole = SkirmishRoleKind.None;
                score.Field = "assault.after_counter";
            }

            state.Priority = score.Priority;
            state.LastScore = score.Total;
            state.RecruitRole = score.RecruitRole;

            if (score.Priority == SkirmishStrategyPriority.RecruitCounter &&
                army != null &&
                (score.RecruitRole == SkirmishRoleKind.Rocketeer ||
                 score.RecruitRole == SkirmishRoleKind.AntiAir))
            {
                string roleId = SkirmishRoleIds.ToId(score.RecruitRole);
                int result;
                if (SkirmishNativeProduction.TrySession(em, out _))
                    result = SkirmishNativeProduction.RequestEnemyCounter(em, session,
                        SkirmishRoleCatalogConfig.RuntimePrefabKey(score.RecruitRole));
                else
                    result = UnityEngine.Application.isPlaying ? 0 :
                        SkirmishProductionService.TryProduce(em, session, roleId, 1, army, 2, out _) ? 1 : -1;
                if (result < 0) state.FailedAttempts++;
                else if (result > 0)
                {
                    state.FailedAttempts = 0;
                    state.CounterCommitted = 1;
                }
            }
            else if (score.Priority == SkirmishStrategyPriority.AttackBase)
            {
                uint groupId = FirstEnemyAssaultGroup(em, session);
                Entity intrusion = FindDefendedHostileStructure(em, session, groupId);
                Entity target = intrusion != Entity.Null ? intrusion : FindVisiblePlayerBase(em, session);
                bool issued = target != Entity.Null && groupId != 0 &&
                    SkirmishArmyCommandService.TryIssueGroupOrder(
                        em, session, groupId, 2, SkirmishGroupOrderKind.Attack, target, out _);
                if (!issued && intrusion != Entity.Null)
                {
                    // An inaccessible intrusion must not strand the base assault.
                    target = FindVisiblePlayerBase(em, session);
                    issued = target != Entity.Null &&
                        SkirmishArmyCommandService.TryIssueGroupOrder(
                            em, session, groupId, 2, SkirmishGroupOrderKind.Attack, target, out _);
                }
                if (!issued && groupId != 0 && TryKnownPlayerBaseApproach(em, session, out var approach))
                    issued = SkirmishArmyCommandService.TryIssueGroupOrder(
                        em, session, groupId, 2, SkirmishGroupOrderKind.Move, Entity.Null, approach, out _);
                if (!issued)
                    state.FailedAttempts++;
                else
                    state.FailedAttempts = 0;
                state.LastGroupId = groupId;
            }
            else if (score.Priority == SkirmishStrategyPriority.Hold ||
                     score.Priority == SkirmishStrategyPriority.DefendHome ||
                     score.Priority == SkirmishStrategyPriority.Reserve)
            {
                uint reserve = FirstEnemyRifleGroup(em, session);
                if (reserve != 0)
                    SkirmishArmyCommandService.TryIssueGroupOrder(
                        em, session, reserve, 2, SkirmishGroupOrderKind.Hold, Entity.Null, out _);
                state.LastGroupId = reserve;
            }

            _ = perception;
            if (em.HasComponent<SkirmishEnemyStrategyComponent>(session))
                em.SetComponentData(session, state);
            else
                em.AddComponentData(session, state);
        }

        private static SkirmishArmyProfileConfig ResolveArmy(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session))
                return null;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            return setup == null ? null : SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
        }

        internal static bool TryKnownPlayerBaseApproach(EntityManager em, Entity session, out float3 destination)
        {
            destination = default;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session)) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null || !setup.MeasuredLayoutBound) return false;
            // Both fixed objective locations are authored public map information.
            // No hidden target entity, mobile position, health or order is consulted.
            destination = new float3(setup.PlayerBaseWorldX, 0, setup.PlayerBaseWorldZ);
            return math.all(math.isfinite(destination));
        }

        private static Entity FindVisiblePlayerBase(EntityManager em, Entity session)
        {
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(
                typeof(SkirmishObjectiveRoleComponent),
                typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 1)
                    continue;
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role !=
                    SkirmishObjectiveRoleKind.PlayerBase)
                    continue;
                if (SkirmishFogService.IsVisibleToFaction(em, entity, 2))
                    return entity;
            }

            return Entity.Null;
        }

        internal static Entity FindDefendedHostileStructure(EntityManager em, Entity session, uint groupId)
        {
            if (groupId == 0 || !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return Entity.Null;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            Entity ownBase = Entity.Null;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (owned.SessionId.Equals(sessionId) && owned.FactionId == 2 &&
                    em.HasComponent<SkirmishObjectiveRoleComponent>(entity) &&
                    em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role == SkirmishObjectiveRoleKind.EnemyBase)
                { ownBase = entity; break; }
            }
            if (ownBase == Entity.Null)
                return Entity.Null;
            float2 basePosition = em.GetComponentData<LocalTransform>(ownBase).Position.xz;
            float best = InfrastructureDefenseRadius * InfrastructureDefenseRadius;
            Entity chosen = Entity.Null;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(candidate);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 1 || owned.IsStructure == 0 ||
                    !em.HasComponent<UnitHealth>(candidate) ||
                    !SkirmishArmyGroupSystem.IsAlive(em, candidate) || !SkirmishFogService.IsVisibleToFaction(em, candidate, 2))
                    continue;
                float2 position = em.GetComponentData<LocalTransform>(candidate).Position.xz;
                float distance = math.distancesq(position, basePosition);
                if (distance >= best || !HasStructureCapableDefenderNear(em, entities, sessionId, groupId, position))
                    continue;
                best = distance;
                chosen = candidate;
            }
            return chosen;
        }

        private static bool HasStructureCapableDefenderNear(EntityManager em, NativeArray<Entity> entities,
            FixedString64Bytes sessionId, uint groupId, float2 position)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity member = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 2 || owned.IsStructure != 0 ||
                    !SkirmishArmyGroupSystem.IsAlive(em, member) ||
                    !em.HasComponent<SkirmishArmyGroupMembershipComponent>(member) ||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(member).GroupId != groupId ||
                    !em.HasComponent<SkirmishRoleOverlayComponent>(member))
                    continue;
                var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(member);
                if (overlay.Damage <= 0 ||
                    !SkirmishExpandedEngagementService.DomainAllows(overlay.TargetDomains, true, SkirmishPopulationCategory.None))
                    continue;
                if (math.distancesq(em.GetComponentData<LocalTransform>(member).Position.xz, position) <=
                    InfrastructureResponseRadius * InfrastructureResponseRadius)
                    return true;
            }
            return false;
        }

        private static uint FirstEnemyAssaultGroup(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            uint fallback = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId != 2 || buffer[i].AliveCount <= 0)
                    continue;
                // Rifles cannot damage a Barracks. Ordering them after the tank
                // died sent a rifle wave that killed the assault column before
                // either designated base fell. Field armies have no tank, so the
                // same public structure roles (rocketeer, breacher, siege) assault.
                if (buffer[i].Role == SkirmishRoleKind.Tank)
                    return buffer[i].GroupId;
                if (fallback == 0 && SkirmishExpandedPresentedOrders.IsStructureAssaultRole(buffer[i].Role))
                    fallback = buffer[i].GroupId;
            }

            return fallback;
        }

        private static uint FirstEnemyRifleGroup(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId == 2 && buffer[i].Role == SkirmishRoleKind.Rifle && buffer[i].AliveCount > 0)
                    return buffer[i].GroupId;
            }

            return 0;
        }
    }

    // Diagnostics only; keys include the match identity and do not affect orders or damage.
    internal static class SkirmishEnemyTrace
    {
        private static readonly Dictionary<string, string> Orders = new();
        private static readonly HashSet<string> FootprintSessions = new();

        internal static void InitialWorld(EntityManager em, Entity session)
        {
            var match = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (match.IsLegacy != 0) return;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var parts = new List<string>();
            for (byte faction = 1; faction <= 2; faction++)
            {
                Entity baseEntity = Entity.Null;
                var roles = new SortedDictionary<string, int>();
                foreach (var actor in entities)
                {
                    var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if (!own.SessionId.Equals(match.SessionId) || own.FactionId != faction) continue;
                    if (own.IsStructure == 0 && SkirmishArmyGroupSystem.IsAlive(em, actor))
                        Add(roles, em.HasComponent<SkirmishUnitRoleComponent>(actor) ? em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role.ToString() : "unknown");
                    if (em.HasComponent<SkirmishObjectiveRoleComponent>(actor))
                    {
                        var role = em.GetComponentData<SkirmishObjectiveRoleComponent>(actor).Role;
                        if ((faction == 1 && role == SkirmishObjectiveRoleKind.PlayerBase) ||
                            (faction == 2 && role == SkirmishObjectiveRoleKind.EnemyBase)) baseEntity = actor;
                    }
                }
                var defenses = new SortedDictionary<string, int>();
                var garrison = new SortedDictionary<string, int>();
                string position = "unknown", health = "unknown";
                if (baseEntity != Entity.Null)
                {
                    if (em.HasComponent<UnitHealth>(baseEntity)) { var hp = em.GetComponentData<UnitHealth>(baseEntity); health = hp.Current + "/" + hp.Max; }
                    if (em.HasComponent<LocalTransform>(baseEntity))
                    {
                        float3 origin = em.GetComponentData<LocalTransform>(baseEntity).Position;
                        position = Pos(origin);
                        foreach (var actor in entities)
                        {
                            var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                            if (!own.SessionId.Equals(match.SessionId) || own.FactionId != faction || actor == baseEntity ||
                                !em.HasComponent<LocalTransform>(actor) || !SkirmishArmyGroupSystem.IsAlive(em, actor) ||
                                math.distancesq(origin.xz, em.GetComponentData<LocalTransform>(actor).Position.xz) > 90f * 90f) continue;
                            if (own.IsStructure == 0)
                                Add(garrison, em.HasComponent<SkirmishUnitRoleComponent>(actor) ? em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role.ToString() : "unknown");
                            else if (em.HasComponent<BuildingDefenseWeapon>(actor) || em.HasComponent<UnitAttack>(actor))
                                Add(defenses, em.HasComponent<SkirmishStructureIdentityComponent>(actor) ? em.GetComponentData<SkirmishStructureIdentityComponent>(actor).StructureId.ToString() : "unknown");
                        }
                    }
                }
                parts.Add("faction" + faction + "_units=" + Counts(roles) + " faction" + faction + "_base_position=" + position + " faction" + faction + "_base_health=" + health + " faction" + faction + "_defenses=" + Counts(defenses) + " faction" + faction + "_garrison=" + Counts(garrison));
            }
            UnityEngine.Debug.Log("[SkirmishEnemyTrace] event=initial_world session=" + match.SessionId + " radius=90 " + string.Join(" ", parts));
            LogFootprints(em, match.SessionId.ToString());
        }
        private static void LogFootprints(EntityManager em, string sessionId)
        {
            if (FootprintSessions.Contains(sessionId)) return;
            using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingConfiguredSpawnableReadModel));
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (boundaries.CalculateEntityCount() != 1 || grids.CalculateEntityCount() != 1) return;
            float cellSize = grids.GetSingleton<GridConfig>().CellSize;
            if (!math.isfinite(cellSize) || cellSize <= 0f) return;
            var definitions = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaries.GetSingletonEntity(), true);
            if (definitions.Length == 0) return;
            var lines = new List<(string id, string line)>();
            foreach (var definition in definitions)
            {
                string key = definition.BuildingId.ToString();
                string id = FootprintStructureId(key);
                string width = (definition.FootprintCells.x * cellSize).ToString("F3", CultureInfo.InvariantCulture);
                string depth = (definition.FootprintCells.y * cellSize).ToString("F3", CultureInfo.InvariantCulture);
                // The placement lifecycle accepts rotation for every definition; walls use drag orientation.
                lines.Add((id, "[SkirmishFootprintTrace] structure=" + id + " width=" + width +
                    " depth=" + depth + " rotatable=1 source=BuildingConfiguredSpawnableReadModel"));
            }
            lines.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.id, b.id));
            FootprintSessions.Add(sessionId);
            foreach (var line in lines) UnityEngine.Debug.Log(line.line);
            UnityEngine.Debug.Log("[SkirmishFootprintTrace] result=Completed count=" + lines.Count);
        }
        private static string FootprintStructureId(string key)
        {
            // The read-model ID is a normalized prefab key, not a Skirmish structure ID.
            string[] ids = { SkirmishStructureIds.Barracks, SkirmishStructureIds.GroundStaging,
                SkirmishStructureIds.Helipad, SkirmishStructureIds.Airport, SkirmishStructureIds.AirportSmall, SkirmishStructureIds.OilPump,
                SkirmishStructureIds.Refinery, SkirmishStructureIds.FuelBladder, SkirmishStructureIds.FabricationDepot,
                SkirmishStructureIds.Watchtower, SkirmishStructureIds.SatelliteDish };
            foreach (string id in ids)
                if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(SkirmishStructureIds.VisualKey(id)) == key)
                    return id;
            return key;
        }
        private static void Add(SortedDictionary<string, int> counts, string key) { counts.TryGetValue(key, out int value); counts[key] = value + 1; }
        private static string Counts(SortedDictionary<string, int> counts) { var values = new List<string>(); foreach (var pair in counts) values.Add(pair.Key + ":" + pair.Value); return "[" + string.Join(",", values) + "]"; }
        private static string Pos(float3 p) => "(" + p.x.ToString("F1") + "," + p.y.ToString("F1") + "," + p.z.ToString("F1") + ")";

        internal static void Order(EntityManager em, Entity session, uint group, SkirmishGroupOrderKind order, Entity target, float3 destination, SkirmishArmyGroupRecord record)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session)) return;
            var match = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (match.IsLegacy != 0 || match.Phase != SkirmishSessionPhase.Playing) return;
            if (order == SkirmishGroupOrderKind.Attack && target != Entity.Null && em.Exists(target) && em.HasComponent<LocalTransform>(target)) destination = em.GetComponentData<LocalTransform>(target).Position;
            string key = match.SessionId + ":" + group;
            string signature = order + ":" + target.Index + ":" + target.Version + ":" + (order == SkirmishGroupOrderKind.Move ? Pos(destination) : "");
            if (Orders.TryGetValue(key, out var previous) && previous == signature) return;
            Orders[key] = signature;
            string stack = new System.Diagnostics.StackTrace().ToString();
            string source = stack.Contains("SkirmishEnemyCapabilityService") ? "capability" : stack.Contains("SkirmishEnemyVehicleRepairPolicy") ? "repair" : stack.Contains("SkirmishEnemyObjectiveCommanderService") ? "objective_commander" : stack.Contains("SkirmishEnemyCommanderService") ? "commander" : stack.Contains("SkirmishEnemyStrategySystem") ? "legacy_strategy" : "other";
            float now = em.HasComponent<SkirmishObjectiveClockComponent>(session) ? em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds : 0f;
            UnityEngine.Debug.Log("[SkirmishEnemyTrace] event=enemy_order session=" + match.SessionId + " source=" + source + " group=" + group + " roles=[" + record.Role + ":" + record.AliveCount + "] count=" + record.AliveCount + " order=" + order + " destination=" + Pos(destination) + " target=" + target + " time=" + now.ToString("F2"));
        }


    }

}
