using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishAriaPublicProjection
    {
        public static SkirmishAriaPublicView FromSession(
            EntityManager em,
            Entity session,
            SkirmishArmyProfileConfig army)
        {
            var view = new SkirmishAriaPublicView
            {
                Playing = true,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true
            };
            if (em.HasComponent<SkirmishResultComponent>(session))
            {
                view.Finished = true;
                view.Playing = false;
            }

            if (em.HasComponent<SkirmishEconomyStockComponent>(session))
                view.OwnMaterials = em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials;
            if (em.HasComponent<SkirmishCapacityComponent>(session))
                view.OwnInfantry = em.GetComponentData<SkirmishCapacityComponent>(session).InfantryLive;
            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                var facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                view.PlayerDesignatedAlive = facts.PlayerDesignatedAlive != 0;
                view.EnemyDesignatedAlive = facts.EnemyDesignatedAlive != 0;
            }

            if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                var perception = new SkirmishPublicPerception
                {
                    OwnMaterials = view.OwnMaterials,
                    OwnFuel = em.HasComponent<SkirmishEconomyStockComponent>(session)
                        ? em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel
                        : 0,
                    OwnInfantryLive = view.OwnInfantry,
                    OwnGroundLive = em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).GroundLive
                        : 0,
                    OwnAirLive = em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).AirLive
                        : 0,
                    AirCap = em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).AirCap
                        : 0,
                    HelipadPresent = SkirmishProductionService.HasLivingProducer(
                        em, session, SkirmishProducerKind.Helipad, 1),
                    AirportPresent = SkirmishProductionService.HasLivingProducer(
                        em, session, SkirmishProducerKind.Airport, 1),
                    OwnSupplyLive = em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).SupplyLive
                        : 0,
                    OwnSupplyCap = em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).SupplyCap
                        : 128,
                    Playing = view.Playing
                };
                view.CanAffordRifle = SkirmishStrategyScoring.TryAfford(
                    army, setup.Readiness, setup.RoleOverlays, perception,
                    SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle);
                view.CanAffordRocketeer = SkirmishStrategyScoring.TryAfford(
                    army, setup.Readiness, setup.RoleOverlays, perception,
                    SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer);
                view.CanAffordTank = SkirmishStrategyScoring.TryAfford(
                    army, setup.Readiness, setup.RoleOverlays, perception,
                    SkirmishRoleIds.Tank, SkirmishRoleKind.Tank);
                view.CanAffordAntiAir = SkirmishStrategyScoring.TryAfford(
                    army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays, perception,
                    SkirmishRoleIds.AntiAir, SkirmishRoleKind.AntiAir);
                view.PadReady = perception.HelipadPresent &&
                    (int)LiveReadiness(em, session, setup.Readiness) >= (int)SkirmishReadinessStage.Established;
            }

            view.VisibleHostileCombat = CountVisibleHostileCombat(em, session);
            view.VisibleHostileAir = CountVisibleHostileAir(em, session);
            view.RecruitControlAvailable = true;
            view.AttackControlAvailable = true;
            view.HoldControlAvailable = true;
            view.GroupControlAvailable = true;
            return view;
        }

        private static int CountVisibleHostileCombat(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return 0;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int visible = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 2 || owned.IsStructure != 0)
                    continue;
                if (!SkirmishFogService.IsVisible(em, unit) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                if (!em.HasComponent<SkirmishUnitRoleComponent>(unit))
                    continue;
                SkirmishPopulationCategory category = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category;
                if (category == SkirmishPopulationCategory.Infantry ||
                    category == SkirmishPopulationCategory.Ground)
                    visible++;
            }

            return visible;
        }

        private static int CountVisibleHostileAir(EntityManager em, Entity session)
        {
            return CountVisibleHostile(em, session, SkirmishPopulationCategory.Air);
        }

        private static int CountVisibleHostile(EntityManager em, Entity session, SkirmishPopulationCategory category)
        {
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return 0;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int visible = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 2 || owned.IsStructure != 0)
                    continue;
                if (!SkirmishFogService.IsVisible(em, unit) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                if (!em.HasComponent<SkirmishUnitRoleComponent>(unit))
                    continue;
                if (em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category == category)
                    visible++;
            }

            return visible;
        }

        private static SkirmishReadinessStage LiveReadiness(
            EntityManager em,
            Entity session,
            SkirmishReadinessStage compiled)
        {
            if (!em.HasComponent<SkirmishResearchStateComponent>(session))
                return compiled;
            SkirmishReadinessStage live = em.GetComponentData<SkirmishResearchStateComponent>(session).Readiness;
            return live > compiled ? live : compiled;
        }
    }
}
