using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishProductionService
    {
        public static bool TryProduce(
            EntityManager em,
            Entity session,
            string roleId,
            int squadCount,
            SkirmishArmyProfileConfig army,
            out SkirmishProductionDecision decision) =>
            TryProduce(em, session, roleId, squadCount, army, 1, out decision);

        public static bool TryProduce(
            EntityManager em,
            Entity session,
            string roleId,
            int squadCount,
            SkirmishArmyProfileConfig army,
            byte factionId,
            out SkirmishProductionDecision decision)
        {
            decision = default;
            if (em == default || !em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                decision.Reason = SkirmishReasonCode.MissingResolvedSetup;
                return false;
            }

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null || !SkirmishRoleIds.TryParse(roleId, out SkirmishRoleKind roleKind))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedRole;
                return false;
            }

            bool barracks = HasProducer(em, session, SkirmishProducerKind.Barracks, factionId);
            bool staging = HasProducer(em, session, SkirmishProducerKind.GroundStaging, factionId);
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
            var request = new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = roleKind,
                SquadCount = squadCount,
                BarracksPresent = barracks,
                GroundStagingPresent = staging,
                EnforceStocks = true,
                MaterialsAvailable = materials,
                FuelAvailable = fuel,
                InfantryLive = snapshot.InfantryLive,
                GroundLive = snapshot.GroundLive,
                AirLive = snapshot.AirLive,
                SupplyLive = snapshot.SupplyLive,
                InfantryReserved = snapshot.InfantryReserved,
                GroundReserved = snapshot.GroundReserved,
                AirReserved = snapshot.AirReserved,
                SupplyReserved = snapshot.SupplyReserved,
                InfantryCap = snapshot.InfantryCap,
                GroundCap = snapshot.GroundCap,
                AirCap = snapshot.AirCap,
                SupplyCap = snapshot.SupplyCap
            };
            decision = SkirmishProductionEligibility.Evaluate(request, army, setup.Readiness, setup.RoleOverlays);
            if (!decision.Accepted)
                return false;

            SkirmishPopulationCategory category = SkirmishRoleIds.Category(roleKind);
            if (!SkirmishCapacityLedger.TryReserve(ref snapshot, category, decision.MemberCount, decision.SupplyCost))
            {
                decision.Accepted = false;
                decision.Reason = SkirmishReasonCode.InsufficientCapacity;
                return false;
            }

            materials -= decision.MaterialsCost;
            fuel -= decision.FuelCost;
            nextReservation++;
            uint reservationId = nextReservation;
            decision.ReservationId = reservationId;
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);
            em.GetBuffer<SkirmishProductionReservation>(session).Add(new SkirmishProductionReservation
            {
                ReservationId = reservationId,
                Role = roleKind,
                Category = category,
                MemberCount = decision.MemberCount,
                RemainingMembers = decision.MemberCount,
                MaterialsPaid = decision.MaterialsCost,
                SupplyCost = decision.SupplyCost,
                Phase = SkirmishReservationPhase.Reserved,
                FactionId = factionId
            });

            SkirmishCapacityLedger.PromoteLive(ref snapshot, category, decision.MemberCount, decision.SupplyCost);
            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            MarkReservation(em, session, reservationId, SkirmishReservationPhase.Live);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            string prefabKey = PrefabKey(setup, roleKind);
            int perMemberSupply = decision.MemberCount == 0 ? 0 : decision.SupplyCost / decision.MemberCount;
            var force = new SkirmishResolvedForceEntry
            {
                FactionId = factionId,
                RoleId = roleId,
                RoleKind = roleKind,
                RuntimePrefabKey = prefabKey,
                Quantity = decision.MemberCount,
                SupplyCost = decision.SupplyCost
            };
            for (int i = 0; i < decision.MemberCount; i++)
            {
                Entity member = SkirmishScenarioSpawnSystem.CreateForceMember(
                    em, sessionId, force, (int)reservationId, i, perMemberSupply, prefabKey);
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                owned.ReservationId = reservationId;
                em.SetComponentData(member, owned);
            }

            using var ownedQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(em, ownedQuery, sessionId, setup);
            SkirmishArmyGroupSystem.AssignProduced(em, session, reservationId);
            SkirmishFogService.Project(em, session);
            return true;
        }

        public static bool TryReleaseDeath(EntityManager em, Entity session, Entity unit)
        {
            if (!em.HasComponent<SkirmishAttemptOwnedComponent>(unit) ||
                !em.HasComponent<SkirmishUnitRoleComponent>(unit))
                return false;

            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
            if (owned.CapacityReleased != 0 || owned.IsStructure != 0)
                return false;

            SkirmishUnitRoleComponent role = em.GetComponentData<SkirmishUnitRoleComponent>(unit);
            if (!em.HasComponent<SkirmishCapacityComponent>(session) &&
                !em.HasComponent<SkirmishEnemyCapacityComponent>(session))
                return false;

            byte factionId = owned.FactionId == 0 ? (byte)1 : owned.FactionId;
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
            if (!SkirmishCapacityLedger.TryReleaseDeath(ref snapshot, role.Category, role.SupplyCost))
                return false;

            owned.CapacityReleased = 1;
            em.SetComponentData(unit, owned);
            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            DecrementReservation(em, session, owned.ReservationId);
            using var ownedQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            SkirmishArmyGroupSystem.RefreshAlive(em, session, ownedQuery, sessionId);
            return true;
        }

        private static bool HasProducer(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId)
        {
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != factionId)
                    continue;
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).Producer == producer)
                    return true;
            }

            return false;
        }

        private static void ReadStocks(
            EntityManager em,
            Entity session,
            byte factionId,
            out int materials,
            out int fuel,
            out SkirmishCapacitySnapshot snapshot,
            out uint nextReservation)
        {
            if (factionId == 2 && em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {
                var enemyStock = em.HasComponent<SkirmishEnemyStockComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyStockComponent>(session)
                    : default;
                var enemyCap = em.GetComponentData<SkirmishEnemyCapacityComponent>(session);
                materials = enemyStock.Materials;
                fuel = enemyStock.Fuel;
                snapshot = ToEnemySnapshot(enemyCap);
                nextReservation = enemyCap.NextReservationId;
                return;
            }

            var stock = em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session)
                : default;
            var capacity = em.HasComponent<SkirmishCapacityComponent>(session)
                ? em.GetComponentData<SkirmishCapacityComponent>(session)
                : default;
            materials = stock.Materials;
            fuel = stock.Fuel;
            snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
            nextReservation = capacity.NextReservationId;
        }

        private static void WriteStocks(
            EntityManager em,
            Entity session,
            byte factionId,
            int materials,
            int fuel,
            SkirmishCapacitySnapshot snapshot,
            uint nextReservation)
        {
            if (factionId == 2)
            {
                var enemyStock = em.HasComponent<SkirmishEnemyStockComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyStockComponent>(session)
                    : new SkirmishEnemyStockComponent();
                enemyStock.Materials = materials;
                enemyStock.Fuel = fuel;
                if (em.HasComponent<SkirmishEnemyStockComponent>(session))
                    em.SetComponentData(session, enemyStock);
                else
                    em.AddComponentData(session, enemyStock);

                var enemyCap = em.HasComponent<SkirmishEnemyCapacityComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyCapacityComponent>(session)
                    : new SkirmishEnemyCapacityComponent();
                ApplyEnemySnapshot(ref enemyCap, snapshot);
                enemyCap.NextReservationId = nextReservation;
                if (em.HasComponent<SkirmishEnemyCapacityComponent>(session))
                    em.SetComponentData(session, enemyCap);
                else
                    em.AddComponentData(session, enemyCap);
                return;
            }

            var stock = em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session)
                : new SkirmishEconomyStockComponent();
            stock.Materials = materials;
            stock.Fuel = fuel;
            em.SetComponentData(session, stock);
            var capacity = em.HasComponent<SkirmishCapacityComponent>(session)
                ? em.GetComponentData<SkirmishCapacityComponent>(session)
                : new SkirmishCapacityComponent();
            capacity = SkirmishCapacityLedger.FromSnapshot(capacity, snapshot);
            capacity.NextReservationId = nextReservation;
            em.SetComponentData(session, capacity);
        }

        private static SkirmishCapacitySnapshot ToEnemySnapshot(SkirmishEnemyCapacityComponent capacity) =>
            new SkirmishCapacitySnapshot
            {
                InfantryCap = capacity.InfantryCap,
                GroundCap = capacity.GroundCap,
                AirCap = capacity.AirCap,
                SupplyCap = capacity.SupplyCap,
                InfantryLive = capacity.InfantryLive,
                GroundLive = capacity.GroundLive,
                AirLive = capacity.AirLive,
                SupplyLive = capacity.SupplyLive,
                InfantryReserved = capacity.InfantryReserved,
                GroundReserved = capacity.GroundReserved,
                AirReserved = capacity.AirReserved,
                SupplyReserved = capacity.SupplyReserved
            };

        private static void ApplyEnemySnapshot(
            ref SkirmishEnemyCapacityComponent capacity,
            SkirmishCapacitySnapshot snapshot)
        {
            capacity.InfantryLive = snapshot.InfantryLive;
            capacity.GroundLive = snapshot.GroundLive;
            capacity.AirLive = snapshot.AirLive;
            capacity.SupplyLive = snapshot.SupplyLive;
            capacity.InfantryReserved = snapshot.InfantryReserved;
            capacity.GroundReserved = snapshot.GroundReserved;
            capacity.AirReserved = snapshot.AirReserved;
            capacity.SupplyReserved = snapshot.SupplyReserved;
        }

        private static string PrefabKey(SkirmishResolvedSetup setup, SkirmishRoleKind role)
        {
            if (setup?.Forces == null)
                return string.Empty;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].RoleKind == role && !string.IsNullOrEmpty(setup.Forces[i].RuntimePrefabKey))
                    return setup.Forces[i].RuntimePrefabKey;
            }

            return string.Empty;
        }

        private static void MarkReservation(
            EntityManager em,
            Entity session,
            uint reservationId,
            SkirmishReservationPhase phase)
        {
            if (reservationId == 0 || !em.HasBuffer<SkirmishProductionReservation>(session))
                return;
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishProductionReservation reservation = buffer[i];
                if (reservation.ReservationId != reservationId)
                    continue;
                reservation.Phase = phase;
                buffer[i] = reservation;
                return;
            }
        }

        private static void DecrementReservation(EntityManager em, Entity session, uint reservationId)
        {
            if (reservationId == 0 || !em.HasBuffer<SkirmishProductionReservation>(session))
                return;
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishProductionReservation reservation = buffer[i];
                if (reservation.ReservationId != reservationId)
                    continue;
                reservation.RemainingMembers--;
                if (reservation.RemainingMembers <= 0)
                    reservation.Phase = SkirmishReservationPhase.Released;
                buffer[i] = reservation;
                return;
            }
        }
    }
}
