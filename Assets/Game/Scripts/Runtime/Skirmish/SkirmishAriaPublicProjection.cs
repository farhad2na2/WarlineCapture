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
                view.OwnMaterials = SkirmishMaterialsService.Read(em, session, 1);
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
                view.AirProfile = army != null && army.AllowsOffensiveAir;
                view.CanAffordLogisticsTruck = SkirmishProductionService.EvaluateQueue(em, session,
                    SkirmishRoleIds.LogisticsTruck, 1, army, 1).Accepted;
                view.RifleRecruitPending = HasPendingRecruit(em, session, SkirmishRoleKind.Rifle);
                view.LogisticsTruckCommitted = HasPendingRecruit(em, session, SkirmishRoleKind.LogisticsTruck) ||
                    HasLivingRole(em, session, SkirmishRoleKind.LogisticsTruck);
                var perception = new SkirmishPublicPerception
                {
                    OwnMaterials = view.OwnMaterials,
                    OwnFuel = SkirmishStartingSupplyService.ReadFuel(em, session, 1),
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
                    army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays, perception,
                    SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle);
                view.CanAffordRocketeer = SkirmishStrategyScoring.TryAfford(
                    army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays, perception,
                    SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer);
                view.CanAffordTank = SkirmishStrategyScoring.TryAfford(
                    army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays, perception,
                    SkirmishRoleIds.Tank, SkirmishRoleKind.Tank);
                view.CanAffordAntiAir = SkirmishStrategyScoring.TryAfford(
                    army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays, perception,
                    SkirmishRoleIds.AntiAir, SkirmishRoleKind.AntiAir);
                view.PadPresent = perception.HelipadPresent;
                view.ReadinessEligible = LiveReadiness(em, session, setup.Readiness) >= SkirmishReadinessStage.Established;
                view.CanBuildAirPad = view.Playing && view.AirProfile && view.ReadinessEligible &&
                    CanAffordBuilding(em, SkirmishStructureIds.VisualKey(SkirmishStructureIds.Helipad), view.OwnMaterials);
                view.PadReady = view.PadPresent && view.ReadinessEligible;
                view.AirQueueOffered = view.PadReady &&
                    SkirmishStrategyScoring.TryAfford(
                        army,
                        LiveReadiness(em, session, setup.Readiness),
                        setup.RoleOverlays,
                        perception,
                        SkirmishRoleIds.AttackHeliLight,
                        SkirmishRoleKind.AttackHeliLight);
            }

            view.VisibleHostileCombat = CountVisibleHostileCombat(em, session);
            view.VisibleHostileAir = CountVisibleHostileAir(em, session);
            view.RecruitControlAvailable = true;
            view.AttackControlAvailable = true;
            view.HoldControlAvailable = true;
            view.GroupControlAvailable = true;
            return view;
        }

        private static bool CanAffordBuilding(EntityManager em, string prefabKey, int materials)
        {
            // The shared catalog read model survives the drawer's destruction.
            // Use its authored price; never duplicate a price in the planner.
            using var query = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingConfiguredSpawnableReadModel));
            if (query.CalculateEntityCount() != 1) return false;
            string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefabKey);
            foreach (var item in em.GetBuffer<BuildingConfiguredSpawnableReadModel>(query.GetSingletonEntity(), true))
                if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(item.BuildingId.ToString()) == key)
                    return item.CanRequest != 0 && materials >= item.MaterialsCost;
            return false;
        }

        private static bool HasPendingRecruit(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(session)) return false;
            foreach (var item in em.GetBuffer<SkirmishProductionReservation>(session))
                if (item.FactionId == 1 && item.Role == role &&
                    item.DeliveredMembers < item.MemberCount &&
                    item.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing)
                    return true;
            return false;
        }

        private static bool HasLivingRole(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId == 1 && owned.SessionId.Equals(id) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role == role &&
                    SkirmishArmyGroupSystem.IsAlive(em, unit)) return true;
            }
            return false;
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
