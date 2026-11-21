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
                view.OwnDefenseTowers = CountPlayerDefenseTowers(em, session);
                view.AntiAirCommitted = HasPendingRecruit(em, session, SkirmishRoleKind.AntiAir) ||
                    HasLivingRole(em, session, SkirmishRoleKind.AntiAir);
                view.CanAffordLogisticsTruck = SkirmishProductionService.EvaluateQueue(em, session,
                    SkirmishRoleIds.LogisticsTruck, 1, army, 1).Accepted;
                view.CanAffordTanker = SkirmishProductionService.EvaluateQueue(em, session,
                    SkirmishRoleIds.Tanker, 1, army, 1).Accepted;
                view.TankersCommitted = CountCommittedPlayerRole(em, session, SkirmishRoleKind.Tanker);
                view.RifleRecruitPending = HasPendingRecruit(em, session, SkirmishRoleKind.Rifle);
                view.RocketeerRecruitPending = HasPendingRecruit(em, session, SkirmishRoleKind.Rocketeer);
                view.BreacherRecruitPending = HasPendingRecruit(em, session, SkirmishRoleKind.Breacher);
                view.BreacherFielded = HasLivingRole(em, session, SkirmishRoleKind.Breacher);
                view.CoveragePendingMask = PendingRoleMask(em, session, CoverageRoles);
                view.CoverageFieldedMask = LivingRoleMask(em, session, CoverageRoles);
                view.OwnStructureAttackUnits = CountPlayerStructureAttackUnits(em, session);
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
                // A depleted base-assault force can lose its producer too. Quote the actual
                // purchase before offering Build, or the unavailable card sends ARIA back to Close.
                bool replacingAssaultForce = setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
                    view.OwnStructureAttackUnits == 0;
                var readiness = LiveReadiness(em, session, setup.Readiness);
                bool CanRecruit(string id, SkirmishRoleKind role) => replacingAssaultForce
                    ? SkirmishProductionService.EvaluateQueue(em, session, id, 1, army, 1).Accepted
                    : SkirmishStrategyScoring.TryAfford(army, readiness, setup.RoleOverlays, perception, id, role);
                view.CanAffordRifle = CanRecruit(SkirmishRoleIds.Rifle, SkirmishRoleKind.Rifle);
                view.CanAffordRocketeer = CanRecruit(SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer);
                view.CanAffordBreacher = CanRecruit(SkirmishRoleIds.Breacher, SkirmishRoleKind.Breacher);
                // Coverage is affordable only above a materials reserve (lane 8 S050: five coverage
                // purchases took materials 900 -> 20 and the assault stalled for ~130 s).
                // The real production check (readiness, every producer incl. Intel Station and Helipad, caps,
                // stocks): lane 10 S026 never bought a drone because the scoring helper assumes only Barracks
                // and Ground Staging exist.
                // Quoted at most once per second (each quote checks every producer).
                double now = em.World.Time.ElapsedTime;
                if (s_coverageQuoteSession != session || now < s_coverageQuoteAt || now - s_coverageQuoteAt >= 1d)
                {
                    int mask = 0, research = 0, buildingNeed = 0, buildingPresent = 0, objectiveMask = 0;
                    for (int i = 0; i < CoverageRoles.Length; i++)
                    {
                        string id = SkirmishRoleIds.ToId(CoverageRoles[i]);
                        var quote = SkirmishProductionService.EvaluateQueue(em, session, id, 1, army, 1);
                        if (quote.Accepted && view.OwnMaterials - quote.MaterialsCost >= CoverageReserveMaterials)
                            mask |= 1 << i;
                        if (quote.Accepted && view.OwnMaterials - quote.MaterialsCost >= ObjectiveCoverageReserveMaterials)
                            objectiveMask |= 1 << i;
                        // What more readiness (up to Full Arsenal) would unlock, and which producer
                        // buildings the mission offers: the game's own production rules, no duplicate table.
                        var full = SkirmishProductionService.EvaluateQueueAtReadiness(em, session, id, 1, army, 1, SkirmishReadinessStage.FullArsenal);
                        if (quote.Reason == SkirmishReasonCode.MissingReadiness && full.Reason != SkirmishReasonCode.MissingReadiness &&
                            full.Reason != SkirmishReasonCode.UnsupportedRole) research |= 1 << i;
                        if (full.Reason == SkirmishReasonCode.UnsupportedRole) continue;
                        bool helipadRole = CoverageRoles[i] == SkirmishRoleKind.TransportHeli, dishRole = CoverageRoles[i] == SkirmishRoleKind.Drone;
                        bool missingProducer = full.Reason == SkirmishReasonCode.MissingProducer;
                        if (helipadRole) { if (missingProducer && full.Field == "producer.helipad") buildingNeed |= 1; else buildingPresent |= 1; }
                        if (dishRole) { if (missingProducer && full.Field == "producer.intel_station") buildingNeed |= 2; else buildingPresent |= 2; }
                    }
                    int buildingAfford = 0;
                    int buildMaterials = view.OwnMaterials - CoverageReserveMaterials;
                    if ((buildingNeed & 1) != 0 && CanAffordBuilding(em, SkirmishStructureIds.VisualKey(SkirmishStructureIds.Helipad), buildMaterials)) buildingAfford |= 1;
                    if ((buildingNeed & 2) != 0 && CanAffordBuilding(em, SkirmishStructureIds.VisualKey(SkirmishStructureIds.SatelliteDish), buildMaterials)) buildingAfford |= 2;
                    int objectiveBuildingAfford = 0, objectiveBuildMaterials = view.OwnMaterials - ObjectiveCoverageReserveMaterials;
                    if ((buildingNeed & 1) != 0 && CanAffordBuilding(em, SkirmishStructureIds.VisualKey(SkirmishStructureIds.Helipad), objectiveBuildMaterials)) objectiveBuildingAfford |= 1;
                    if ((buildingNeed & 2) != 0 && CanAffordBuilding(em, SkirmishStructureIds.VisualKey(SkirmishStructureIds.SatelliteDish), objectiveBuildMaterials)) objectiveBuildingAfford |= 2;
                    // Producer buildings below Established also need research before they can be placed.
                    if (buildingNeed != 0 && LiveReadiness(em, session, setup.Readiness) < SkirmishReadinessStage.Established) research |= buildingNeed << CoverageRoles.Length;
                    s_coverageQuoteMask = mask; s_coverageQuoteAt = now; s_coverageQuoteSession = session;
                    s_researchNeedMask = research; s_buildingNeedMask = buildingNeed; s_buildingPresentMask = buildingPresent; s_buildingAffordMask = buildingAfford;
                    s_objectiveCoverageMask = objectiveMask; s_objectiveBuildingAffordMask = objectiveBuildingAfford;
                }
                view.CoverageAffordMask = s_coverageQuoteMask;
                view.ResearchNeedMask = s_researchNeedMask;
                view.BuildingNeedMask = s_buildingNeedMask;
                view.BuildingPresentMask = s_buildingPresentMask;
                view.BuildingAffordMask = s_buildingAffordMask;
                view.CoverageObjectiveAffordMask = s_objectiveCoverageMask;
                view.BuildingObjectiveAffordMask = s_objectiveBuildingAffordMask;
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
                view.AirRecruitPending = HasPendingRecruit(em, session, SkirmishRoleKind.AttackHeliLight);
                CountPlayerAttackAircraft(em, session, ref view);
                view.AirQueueOffered = view.PadReady && !view.AirRecruitPending && view.OwnAttackAirLive == 0 &&
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
            ObserveSlippingHostile(em, session, ref view);
            view.RecruitControlAvailable = true;
            view.AttackControlAvailable = true;
            view.HoldControlAvailable = true;
            view.GroupControlAvailable = true;
            return view;
        }

        private static int CountPlayerDefenseTowers(EntityManager em, Entity session)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishStructureIdentityComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var building in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(id) || !SkirmishArmyGroupSystem.IsAlive(em, building)) continue;
                string structure = em.GetComponentData<SkirmishStructureIdentityComponent>(building).StructureId.ToString();
                if (structure == SkirmishStructureIds.Watchtower || structure == SkirmishStructureIds.WatchtowerApproach) count++;
            }
            return count;
        }

        private static void CountPlayerAttackAircraft(EntityManager em, Entity session, ref SkirmishAriaPublicView view)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            view.OwnAirFuel = SkirmishStartingSupplyService.ReadFuel(em, session, 1);
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitAirComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(id) ||
                    !SkirmishArmyGroupSystem.IsAlive(em, unit)) continue;
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role;
                if (SkirmishRoleIds.Category(role) != SkirmishPopulationCategory.Air ||
                    !SkirmishExpandedPresentedOrders.IsAssaultRole(role)) continue;
                view.OwnAttackAirLive++;
                var air = em.GetComponentData<UnitAirComponent>(unit);
                if (air.Airborne != 0 || air.TakeoffRolling != 0 || air.LandingRolling != 0 || air.ReturningHome != 0)
                    view.OwnAttackAirActive++;
                else if (air.HomeInitialized != 0)
                    view.OwnAttackAirLanded++;
            }
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

        // Read every frame: created once per World (world-owned, disposed with it), never per call.
        private static EntityQuery s_unitRoleQuery;
        private static World s_unitRoleWorld;
        private static EntityQuery UnitRoleQuery(EntityManager em)
        {
            if (s_unitRoleWorld != em.World || !em.IsQueryValid(s_unitRoleQuery))
            {
                s_unitRoleQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
                s_unitRoleWorld = em.World;
            }
            return s_unitRoleQuery;
        }

        public const int CoverageReserveMaterials = 250;
        // FC/BT/CE objective skill only: keep one rifle squad of materials (FieldBase starts at 450, cap 800,
        // and lane 25 S007 never rose above 470, so the 250 base-assault reserve blocked every coverage purchase).
        public const int ObjectiveCoverageReserveMaterials = 80;
        private static int s_objectiveCoverageMask, s_objectiveBuildingAffordMask;
        private static Entity s_coverageQuoteSession;
        private static double s_coverageQuoteAt;
        private static int s_coverageQuoteMask, s_researchNeedMask, s_buildingNeedMask, s_buildingPresentMask, s_buildingAffordMask;

        // Gate-3 coverage kinds; bit i of the coverage masks is CoverageRoles[i] (AriaCoverageKind i+1).
        // AA and attack heli (bits 9, 10) are air-capable army roles (lane 24 S004); ground-only armies reject
        // them as UnsupportedRole, so their bits stay 0 there and the earlier bits keep their meaning.
        private static readonly SkirmishRoleKind[] CoverageRoles =
            { SkirmishRoleKind.Marksman, SkirmishRoleKind.ApcFast, SkirmishRoleKind.Radar,
              SkirmishRoleKind.ApcHeavy, SkirmishRoleKind.Drone, SkirmishRoleKind.TransportHeli,
              SkirmishRoleKind.Gunner, SkirmishRoleKind.Tank, SkirmishRoleKind.Siege,
              SkirmishRoleKind.AntiAir, SkirmishRoleKind.AttackHeli };

        private static int PendingRoleMask(EntityManager em, Entity session, SkirmishRoleKind[] roles)
        {
            int mask = 0;
            for (int i = 0; i < roles.Length; i++)
                if (HasPendingRecruit(em, session, roles[i])) mask |= 1 << i;
            return mask;
        }

        // One pass over the player's units for several roles.
        private static int LivingRoleMask(EntityManager em, Entity session, SkirmishRoleKind[] roles)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var query = UnitRoleQuery(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            int mask = 0;
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(id)) continue;
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role;
                for (int i = 0; i < roles.Length; i++)
                    if (role == roles[i] && (mask & (1 << i)) == 0 && SkirmishArmyGroupSystem.IsAlive(em, unit)) mask |= 1 << i;
            }
            return mask;
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

        private static int CountCommittedPlayerRole(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            int count = 0;
            var query = UnitRoleQuery(em);
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owner.FactionId == 1 && owner.SessionId.Equals(id) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == role &&
                    SkirmishArmyGroupSystem.IsAlive(em, actor)) count++;
            }
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                    if (receipt.FactionId == 1 && receipt.Role == role &&
                        receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing)
                        count += System.Math.Max(0, receipt.MemberCount - receipt.DeliveredMembers);
            return count;
        }

        private static bool HasLivingRole(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            var query = UnitRoleQuery(em);
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

        private static int CountPlayerStructureAttackUnits(EntityManager em, Entity session)
        {
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishRoleOverlayComponent));
            using var units = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var unit in units)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                var overlay = em.GetComponentData<SkirmishRoleOverlayComponent>(unit);
                if (owner.FactionId == 1 && owner.IsStructure == 0 && owner.SessionId.Equals(id) &&
                    overlay.Damage > 0 && (overlay.TargetDomains & SkirmishTargetDomain.Structure) != 0 &&
                    SkirmishArmyGroupSystem.IsAlive(em, unit)) count++;
            }
            return count;
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
                if (!SkirmishFogService.IsVisibleToFaction(em, unit, 1) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
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

        // Public fog view only: a hostile vehicle the player can currently see that is
        // crossing the player's column or has slipped behind most of it toward the
        // player's base (AriaInterceptPolicy.Candidate). Hidden contacts are never considered.
        private static void ObserveSlippingHostile(EntityManager em, Entity session, ref SkirmishAriaPublicView view)
        {
            if (!view.Playing || !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishMatchState>(session))
                return;
            var match = em.GetComponentData<SkirmishMatchState>(session);
            Entity home = match.PlayerMainBase, enemyBase = match.EnemyMainBase;
            if (home == Entity.Null || !em.Exists(home) || !em.HasComponent<Unity.Transforms.LocalTransform>(home))
                return;
            Unity.Mathematics.float2 homeXZ = em.GetComponentData<Unity.Transforms.LocalTransform>(home).Position.xz;
            bool enemyBaseKnown = enemyBase != Entity.Null && em.Exists(enemyBase) && em.HasComponent<Unity.Transforms.LocalTransform>(enemyBase);
            Unity.Mathematics.float2 enemyXZ = enemyBaseKnown ? em.GetComponentData<Unity.Transforms.LocalTransform>(enemyBase).Position.xz : default;
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(Unity.Transforms.LocalTransform));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using var own = new NativeList<Unity.Mathematics.float2>(Allocator.Temp);
            using var ownHome = new NativeList<float>(Allocator.Temp);
            using var hostiles = new NativeList<Unity.Mathematics.float3>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (!owned.SessionId.Equals(sessionId) || owned.IsStructure != 0 ||
                    !em.HasComponent<SkirmishUnitRoleComponent>(unit) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
                    continue;
                SkirmishPopulationCategory category = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category;
                var position = em.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position;
                if (owned.FactionId == 1 &&
                    (category == SkirmishPopulationCategory.Infantry || category == SkirmishPopulationCategory.Ground))
                {
                    own.Add(position.xz);
                    ownHome.Add(Unity.Mathematics.math.distance(position.xz, homeXZ));
                }
                else if (owned.FactionId == 2 && category == SkirmishPopulationCategory.Ground &&
                    SkirmishFogService.IsVisibleToFaction(em, unit, 1))
                    hostiles.Add(position);
            }
            if (own.Length == 0 || hostiles.Length == 0)
                return;
            ownHome.Sort();
            float median = ownHome[ownHome.Length / 2];
            float best = float.MaxValue;
            for (int h = 0; h < hostiles.Length; h++)
            {
                var hostile = hostiles[h];
                float homeDistance = Unity.Mathematics.math.distance(hostile.xz, homeXZ);
                float nearest = float.MaxValue;
                for (int o = 0; o < own.Length; o++)
                    nearest = Unity.Mathematics.math.min(nearest, Unity.Mathematics.math.distance(own[o], hostile.xz));
                float enemyDistance = enemyBaseKnown ? Unity.Mathematics.math.distance(hostile.xz, enemyXZ) : float.MaxValue;
                if (!Game.Simulation.AriaInterceptPolicy.Candidate(homeDistance, median, nearest, enemyDistance) || homeDistance >= best)
                    continue;
                best = homeDistance;
                view.SlippingHostile = true;
                view.SlippingHostileX = hostile.x; view.SlippingHostileY = hostile.y; view.SlippingHostileZ = hostile.z;
            }
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
                if (!SkirmishFogService.IsVisibleToFaction(em, unit, 1) || !SkirmishArmyGroupSystem.IsAlive(em, unit))
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
