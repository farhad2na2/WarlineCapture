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

            bool barracks = HasLivingProducer(em, session, SkirmishProducerKind.Barracks, factionId);
            bool staging = HasLivingProducer(em, session, SkirmishProducerKind.GroundStaging, factionId);
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
            var request = new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = roleKind,
                SquadCount = squadCount,
                BarracksPresent = barracks,
                GroundStagingPresent = staging,
                HelipadPresent = HasLivingProducer(em, session, SkirmishProducerKind.Helipad, factionId),
                AirportPresent = HasLivingProducer(em, session, SkirmishProducerKind.Airport, factionId),
                IntelStationPresent = HasLivingProducer(em, session, SkirmishProducerKind.IntelStation, factionId),
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
            decision = SkirmishProductionEligibility.Evaluate(
                request, army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays);
            if (!decision.Accepted)
                return false;
            if (IsProducerLocked(em, session, decision.Producer, factionId))
            {
                decision.Accepted = false;
                decision.Reason = SkirmishReasonCode.QueueLocked;
                decision.Field = "queue";
                return false;
            }

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
                FactionId = factionId,
                Producer = decision.Producer
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
            System.Collections.Generic.Dictionary<string, Entity> prefabLookup =
                SkirmishScenarioSpawnSystem.BuildPrefabEntityLookup(em);
            for (int i = 0; i < decision.MemberCount; i++)
            {
                Entity member = SkirmishScenarioSpawnSystem.CreateForceMember(
                    em, sessionId, force, (int)reservationId, i, perMemberSupply, prefabKey, prefabLookup, setup);
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                owned.ReservationId = reservationId;
                em.SetComponentData(member, owned);
            }

            using var ownedQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(em, ownedQuery, sessionId, setup);
            SkirmishArmyGroupSystem.AssignProduced(em, session, reservationId);
            SkirmishFogService.Project(em, session);
            if (em.HasComponent<SkirmishVisualPrefabCatalogRecord>(session))
                SkirmishVisualSpawnService.AttachMissing(em, session, setup);
            if (em.HasComponent<SkirmishResearchStateComponent>(session))
                SkirmishResearchEffects.ApplyCompleted(em, session, factionId);
            return true;
        }

        public static bool TryQueue(
            EntityManager em,
            Entity session,
            string roleId,
            int squadCount,
            SkirmishArmyProfileConfig army,
            out SkirmishProductionDecision decision) =>
            TryQueue(em, session, roleId, squadCount, army, 1, out decision);

        public static bool TryQueue(
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

            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
            var request = new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = roleKind,
                SquadCount = squadCount,
                BarracksPresent = HasLivingProducer(em, session, SkirmishProducerKind.Barracks, factionId),
                GroundStagingPresent = HasLivingProducer(em, session, SkirmishProducerKind.GroundStaging, factionId),
                HelipadPresent = HasLivingProducer(em, session, SkirmishProducerKind.Helipad, factionId),
                AirportPresent = HasLivingProducer(em, session, SkirmishProducerKind.Airport, factionId),
                IntelStationPresent = HasLivingProducer(em, session, SkirmishProducerKind.IntelStation, factionId),
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
            decision = SkirmishProductionEligibility.Evaluate(
                request, army, LiveReadiness(em, session, setup.Readiness), setup.RoleOverlays);
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
            decision.ReservationId = nextReservation;
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);
            em.GetBuffer<SkirmishProductionReservation>(session).Add(new SkirmishProductionReservation
            {
                ReservationId = nextReservation,
                Role = roleKind,
                Category = category,
                MemberCount = decision.MemberCount,
                RemainingMembers = decision.MemberCount,
                MaterialsPaid = decision.MaterialsCost,
                SupplyCost = decision.SupplyCost,
                Phase = SkirmishReservationPhase.Reserved,
                FactionId = factionId,
                Producer = decision.Producer
            });
            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            return true;
        }

        public static bool TryStartQueued(
            EntityManager em,
            Entity session,
            uint reservationId,
            out SkirmishProductionDecision decision)
        {
            decision = default;
            if (!TryGetReservation(em, session, reservationId, out int index, out SkirmishProductionReservation reservation))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            decision.ReservationId = reservationId;
            decision.MaterialsCost = reservation.MaterialsPaid;
            decision.SupplyCost = reservation.SupplyCost;
            decision.MemberCount = reservation.MemberCount;
            decision.Producer = reservation.Producer;
            if (reservation.Phase != SkirmishReservationPhase.Reserved)
            {
                decision.Reason = reservation.Phase == SkirmishReservationPhase.Live
                    ? SkirmishReasonCode.NotCancellable
                    : SkirmishReasonCode.StaleCommand;
                return false;
            }

            if (TryRejectMissingAirPad(em, session, reservation, ref decision))
                return false;

            if (IsProducerLocked(em, session, reservation.Producer, reservation.FactionId))
            {
                decision.Reason = SkirmishReasonCode.QueueLocked;
                decision.Field = "queue";
                return false;
            }

            reservation.Phase = SkirmishReservationPhase.Producing;
            DynamicBuffer<SkirmishProductionReservation> started = em.GetBuffer<SkirmishProductionReservation>(session);
            started[index] = reservation;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            return true;
        }

        public static bool TryDispatchQueued(
            EntityManager em,
            Entity session,
            uint reservationId,
            SkirmishArmyProfileConfig army,
            out SkirmishProductionDecision decision)
        {
            decision = default;
            if (!TryGetReservation(em, session, reservationId, out int index, out SkirmishProductionReservation reservation))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            if (reservation.Phase != SkirmishReservationPhase.Producing)
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                decision.ReservationId = reservationId;
                return false;
            }

            if (TryRejectMissingAirPad(em, session, reservation, ref decision))
                return false;

            _ = army;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup == null)
            {
                return TryRefundFailedDispatch(em, session, reservationId, out decision);
            }

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            ReadStocks(em, session, reservation.FactionId, out int materials, out int fuel, out var snapshot, out uint next);
            SkirmishCapacityLedger.PromoteLive(
                ref snapshot, reservation.Category, reservation.MemberCount, reservation.SupplyCost);
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            reservation.Phase = SkirmishReservationPhase.Live;
            DynamicBuffer<SkirmishProductionReservation> dispatched = em.GetBuffer<SkirmishProductionReservation>(session);
            dispatched[index] = reservation;

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            string roleId = RoleIdOf(reservation.Role);
            string prefabKey = PrefabKey(setup, reservation.Role);
            int perMemberSupply = reservation.MemberCount == 0 ? 0 : reservation.SupplyCost / reservation.MemberCount;
            var force = new SkirmishResolvedForceEntry
            {
                FactionId = reservation.FactionId,
                RoleId = roleId,
                RoleKind = reservation.Role,
                RuntimePrefabKey = prefabKey,
                Quantity = reservation.MemberCount,
                SupplyCost = reservation.SupplyCost
            };
            System.Collections.Generic.Dictionary<string, Entity> prefabLookup =
                SkirmishScenarioSpawnSystem.BuildPrefabEntityLookup(em);
            for (int i = 0; i < reservation.MemberCount; i++)
            {
                Entity member = SkirmishScenarioSpawnSystem.CreateForceMember(
                    em, sessionId, force, (int)reservationId, i, perMemberSupply, prefabKey, prefabLookup, setup);
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                owned.ReservationId = reservationId;
                em.SetComponentData(member, owned);
            }

            using var ownedQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            SkirmishRosterProjectionSystem.Apply(em, ownedQuery, sessionId, setup);
            SkirmishArmyGroupSystem.AssignProduced(em, session, reservationId);
            if (em.HasComponent<SkirmishResearchStateComponent>(session))
                SkirmishResearchEffects.ApplyCompleted(em, session, reservation.FactionId);
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.ReservationId = reservationId;
            decision.MemberCount = reservation.MemberCount;
            decision.MaterialsCost = reservation.MaterialsPaid;
            decision.SupplyCost = reservation.SupplyCost;
            decision.Producer = reservation.Producer;
            return true;
        }

        public static bool TryCancel(
            EntityManager em,
            Entity session,
            uint reservationId,
            out SkirmishProductionDecision decision)
        {
            decision = default;
            if (!TryGetReservation(em, session, reservationId, out int index, out SkirmishProductionReservation reservation))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            decision.ReservationId = reservationId;
            decision.MaterialsCost = reservation.MaterialsPaid;
            decision.Producer = reservation.Producer;
            if (reservation.Phase == SkirmishReservationPhase.Live)
            {
                decision.Reason = SkirmishReasonCode.NotCancellable;
                return false;
            }

            if (reservation.Phase != SkirmishReservationPhase.Reserved &&
                reservation.Phase != SkirmishReservationPhase.Producing)
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                return false;
            }

            int refund = SkirmishResearchCosts.RefundProduce(reservation.MaterialsPaid, reservation.Phase);
            ReadStocks(em, session, reservation.FactionId, out int materials, out int fuel, out var snapshot, out uint next);
            if (!SkirmishCapacityLedger.TryReleaseReserved(
                    ref snapshot, reservation.Category, reservation.MemberCount, reservation.SupplyCost))
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                return false;
            }

            materials += refund;
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            reservation.Phase = SkirmishReservationPhase.Cancelled;
            DynamicBuffer<SkirmishProductionReservation> cancelled = em.GetBuffer<SkirmishProductionReservation>(session);
            cancelled[index] = reservation;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.RefundedMaterials = refund;
            return true;
        }

        public static bool TryRefundFailedDispatch(
            EntityManager em,
            Entity session,
            uint reservationId,
            out SkirmishProductionDecision decision)
        {
            decision = default;
            if (!TryGetReservation(em, session, reservationId, out int index, out SkirmishProductionReservation reservation))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            if (reservation.Phase != SkirmishReservationPhase.Reserved &&
                reservation.Phase != SkirmishReservationPhase.Producing)
            {
                decision.Reason = SkirmishReasonCode.NotCancellable;
                return false;
            }

            ReadStocks(em, session, reservation.FactionId, out int materials, out int fuel, out var snapshot, out uint next);
            if (!SkirmishCapacityLedger.TryReleaseReserved(
                    ref snapshot, reservation.Category, reservation.MemberCount, reservation.SupplyCost))
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                return false;
            }

            materials += reservation.MaterialsPaid;
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            reservation.Phase = SkirmishReservationPhase.Cancelled;
            DynamicBuffer<SkirmishProductionReservation> failed = em.GetBuffer<SkirmishProductionReservation>(session);
            failed[index] = reservation;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.ReservationId = reservationId;
            decision.MaterialsCost = reservation.MaterialsPaid;
            decision.RefundedMaterials = reservation.MaterialsPaid;
            return true;
        }

        public static int NotifyProducerDestroyed(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                return 0;
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            int changed = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishProductionReservation reservation = buffer[i];
                if (reservation.FactionId != factionId || reservation.Producer != producer)
                    continue;
                if (reservation.Phase == SkirmishReservationPhase.Reserved)
                {
                    TryRefundFailedDispatch(em, session, reservation.ReservationId, out _);
                    changed++;
                    continue;
                }

                if (reservation.Phase != SkirmishReservationPhase.Producing)
                    continue;
                ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint next);
                SkirmishCapacityLedger.TryReleaseReserved(
                    ref snapshot, reservation.Category, reservation.MemberCount, reservation.SupplyCost);
                WriteStocks(em, session, factionId, materials, fuel, snapshot, next);
                reservation.Phase = SkirmishReservationPhase.Lost;
                buffer = em.GetBuffer<SkirmishProductionReservation>(session);
                buffer[i] = reservation;
                changed++;
            }

            return changed;
        }

        public static bool IsProducerLocked(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId)
        {
            if (producer == SkirmishProducerKind.None)
                return false;
            if (em.HasBuffer<SkirmishProductionReservation>(session))
            {
                DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].Producer == producer &&
                        buffer[i].FactionId == factionId &&
                        buffer[i].Phase == SkirmishReservationPhase.Producing)
                        return true;
                }
            }

            if (!em.HasBuffer<SkirmishResearchQueueItem>(session))
                return false;
            DynamicBuffer<SkirmishResearchQueueItem> research = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < research.Length; i++)
            {
                if (research[i].Producer == producer &&
                    research[i].FactionId == factionId &&
                    research[i].Phase == SkirmishResearchPhase.Researching)
                    return true;
            }

            return false;
        }

        public static bool HasLivingProducer(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId)
        {
            return HasProducer(em, session, producer, factionId);
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
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).Producer != producer)
                    continue;
                if (SkirmishArmyGroupSystem.IsAlive(em, entities[i]))
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

        private static bool TryGetReservation(
            EntityManager em,
            Entity session,
            uint reservationId,
            out int index,
            out SkirmishProductionReservation reservation)
        {
            index = -1;
            reservation = default;
            if (reservationId == 0 || !em.HasBuffer<SkirmishProductionReservation>(session))
                return false;
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ReservationId != reservationId)
                    continue;
                index = i;
                reservation = buffer[i];
                return true;
            }

            return false;
        }

        private static bool TryRejectMissingAirPad(
            EntityManager em,
            Entity session,
            SkirmishProductionReservation reservation,
            ref SkirmishProductionDecision decision)
        {
            if (AirPadReady(em, session, reservation, out SkirmishReasonCode reason, out string field))
                return false;

            int paid = reservation.MaterialsPaid;
            if (reservation.Phase == SkirmishReservationPhase.Reserved ||
                reservation.Phase == SkirmishReservationPhase.Producing)
                TryRefundFailedDispatch(em, session, reservation.ReservationId, out decision);

            decision.Accepted = false;
            decision.Reason = reason;
            decision.Field = field;
            decision.ReservationId = reservation.ReservationId;
            decision.MaterialsCost = paid;
            decision.RefundedMaterials = paid;
            decision.Producer = reservation.Producer;
            return true;
        }

        private static bool AirPadReady(
            EntityManager em,
            Entity session,
            SkirmishProductionReservation reservation,
            out SkirmishReasonCode reason,
            out string field)
        {
            reason = SkirmishReasonCode.None;
            field = string.Empty;
            if (reservation.Producer != SkirmishProducerKind.Helipad &&
                reservation.Producer != SkirmishProducerKind.Airport)
                return true;

            SkirmishReadinessStage required = reservation.Producer == SkirmishProducerKind.Airport
                ? SkirmishReadinessStage.FullArsenal
                : SkirmishReadinessStage.Established;
            SkirmishReadinessStage compiled = SkirmishReadinessStage.Field;
            if (em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if (setup != null)
                    compiled = setup.Readiness;
            }

            if ((int)LiveReadiness(em, session, compiled) < (int)required)
            {
                reason = SkirmishReasonCode.MissingReadiness;
                field = "readiness";
                return false;
            }

            if (!HasLivingProducer(em, session, reservation.Producer, reservation.FactionId))
            {
                reason = SkirmishReasonCode.MissingProducer;
                field = reservation.Producer == SkirmishProducerKind.Airport
                    ? "producer.airport"
                    : "producer.helipad";
                return false;
            }

            return true;
        }

        private static string RoleIdOf(SkirmishRoleKind kind) => SkirmishRoleIds.ToId(kind);

        private static string PrefabKey(SkirmishResolvedSetup setup, SkirmishRoleKind role)
        {
            if (setup?.Forces != null)
            {
                for (int i = 0; i < setup.Forces.Length; i++)
                {
                    if (setup.Forces[i].RoleKind == role && !string.IsNullOrEmpty(setup.Forces[i].RuntimePrefabKey))
                        return setup.Forces[i].RuntimePrefabKey;
                }
            }

            return SkirmishRoleCatalogConfig.RuntimePrefabKey(role);
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
