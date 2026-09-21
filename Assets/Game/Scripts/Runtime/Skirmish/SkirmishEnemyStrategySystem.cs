using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishArmyGroupSystem))]
    public partial struct SkirmishEnemyStrategySystem : ISystem
    {
        private EntityQuery ownedQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            ownedQuery = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
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
            Execute(em, session, score, perception, army);
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
            if (em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                perception.OwnMaterials = stock.Materials;
                perception.OwnFuel = stock.Fuel;
            }

            if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var cap = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                perception.OwnInfantryLive = cap.InfantryLive;
                perception.OwnGroundLive = cap.GroundLive;
                perception.OwnSupplyLive = cap.SupplyLive;
                perception.OwnSupplyCap = cap.SupplyCap;
            }

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
            }

            return perception;
        }

        private static void Execute(
            EntityManager em,
            Entity session,
            SkirmishStrategyScore score,
            in SkirmishPublicPerception perception,
            SkirmishArmyProfileConfig army)
        {
            var state = em.HasComponent<SkirmishEnemyStrategyComponent>(session)
                ? em.GetComponentData<SkirmishEnemyStrategyComponent>(session)
                : new SkirmishEnemyStrategyComponent();
            state.Priority = score.Priority;
            state.LastScore = score.Total;
            state.RecruitRole = score.RecruitRole;

            if (score.Priority == SkirmishStrategyPriority.RecruitCounter &&
                score.RecruitRole == SkirmishRoleKind.Rocketeer &&
                army != null)
            {
                if (!SkirmishProductionService.TryProduce(
                    em, session, SkirmishRoleIds.Rocketeer, 1, army, 2, out _))
                    state.FailedAttempts++;
                else
                    state.FailedAttempts = 0;
            }
            else if (score.Priority == SkirmishStrategyPriority.AttackBase)
            {
                Entity target = FindVisiblePlayerBase(em, session);
                uint groupId = FirstEnemyAssaultGroup(em, session);
                if (target == Entity.Null || groupId == 0 ||
                    !SkirmishArmyCommandService.TryIssueGroupOrder(
                        em, session, groupId, 2, SkirmishGroupOrderKind.Attack, target, out _))
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

        private static uint FirstEnemyAssaultGroup(EntityManager em, Entity session)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(session))
                return 0;
            DynamicBuffer<SkirmishArmyGroupRecord> buffer = em.GetBuffer<SkirmishArmyGroupRecord>(session);
            uint rifle = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].FactionId != 2 || buffer[i].AliveCount <= 0)
                    continue;
                if (buffer[i].Role == SkirmishRoleKind.Tank)
                    return buffer[i].GroupId;
                if (rifle == 0 && buffer[i].Role == SkirmishRoleKind.Rifle)
                    rifle = buffer[i].GroupId;
            }

            return rifle;
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
