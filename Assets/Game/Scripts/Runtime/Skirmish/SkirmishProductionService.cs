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

            bool barracks = HasProducer(em, session, SkirmishProducerKind.Barracks);
            bool staging = HasProducer(em, session, SkirmishProducerKind.GroundStaging);
            SkirmishCapacityComponent capacity = em.HasComponent<SkirmishCapacityComponent>(session)
                ? em.GetComponentData<SkirmishCapacityComponent>(session)
                : default;
            SkirmishEconomyStockComponent stock = em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session)
                : default;

            var request = new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = roleKind,
                SquadCount = squadCount,
                BarracksPresent = barracks,
                GroundStagingPresent = staging,
                EnforceStocks = true,
                MaterialsAvailable = stock.Materials,
                FuelAvailable = stock.Fuel,
                InfantryLive = capacity.InfantryLive,
                GroundLive = capacity.GroundLive,
                AirLive = capacity.AirLive,
                SupplyLive = capacity.SupplyLive,
                InfantryReserved = capacity.InfantryReserved,
                GroundReserved = capacity.GroundReserved,
                AirReserved = capacity.AirReserved,
                SupplyReserved = capacity.SupplyReserved,
                InfantryCap = capacity.InfantryCap,
                GroundCap = capacity.GroundCap,
                AirCap = capacity.AirCap,
                SupplyCap = capacity.SupplyCap
            };
            decision = SkirmishProductionEligibility.Evaluate(request, army, setup.Readiness, setup.RoleOverlays);
            if (!decision.Accepted)
                return false;

            SkirmishPopulationCategory category = SkirmishRoleIds.Category(roleKind);
            var snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
            if (!SkirmishCapacityLedger.TryReserve(ref snapshot, category, decision.MemberCount, decision.SupplyCost))
            {
                decision.Accepted = false;
                decision.Reason = SkirmishReasonCode.InsufficientCapacity;
                return false;
            }

            stock.Materials -= decision.MaterialsCost;
            stock.Fuel -= decision.FuelCost;
            capacity.NextReservationId++;
            uint reservationId = capacity.NextReservationId;
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
                FactionId = 1
            });

            SkirmishCapacityLedger.PromoteLive(ref snapshot, category, decision.MemberCount, decision.SupplyCost);
            em.SetComponentData(session, SkirmishCapacityLedger.FromSnapshot(capacity, snapshot));
            em.SetComponentData(session, stock);
            MarkReservation(em, session, reservationId, SkirmishReservationPhase.Live);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            string prefabKey = PrefabKey(setup, roleKind);
            int perMemberSupply = decision.MemberCount == 0 ? 0 : decision.SupplyCost / decision.MemberCount;
            var force = new SkirmishResolvedForceEntry
            {
                FactionId = 1,
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
            if (!em.HasComponent<SkirmishCapacityComponent>(session))
                return false;

            SkirmishCapacityComponent capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
            var snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
            if (!SkirmishCapacityLedger.TryReleaseDeath(ref snapshot, role.Category, role.SupplyCost))
                return false;

            owned.CapacityReleased = 1;
            em.SetComponentData(unit, owned);
            em.SetComponentData(session, SkirmishCapacityLedger.FromSnapshot(capacity, snapshot));
            DecrementReservation(em, session, owned.ReservationId);
            return true;
        }

        private static bool HasProducer(EntityManager em, Entity session, SkirmishProducerKind producer)
        {
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent), typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    continue;
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).Producer == producer)
                    return true;
            }

            return false;
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
