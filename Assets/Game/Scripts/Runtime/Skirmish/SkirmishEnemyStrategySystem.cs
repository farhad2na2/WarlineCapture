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
            SkirmishPublicPerception perception = Perceive(em, session, owned);
            var current = em.HasComponent<SkirmishEnemyStrategyComponent>(session)
                ? em.GetComponentData<SkirmishEnemyStrategyComponent>(session).Priority
                : SkirmishStrategyPriority.None;
            score = SkirmishStrategyScoring.ScoreBaseAssault(
                perception, army, setup.Readiness, setup.RoleOverlays, current);
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
                if (!SkirmishFogService.IsVisible(em, unit) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
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
                if (SkirmishFogService.IsVisible(em, entity))
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
                    !SkirmishArmyGroupSystem.IsAlive(em, candidate) || !SkirmishFogService.IsVisible(em, candidate))
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

}
