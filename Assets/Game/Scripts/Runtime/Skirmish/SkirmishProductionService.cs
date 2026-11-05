using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static partial class SkirmishProductionService
    {

        // Optional observation only; no bank/decision/component is written here.
        // The owned Editor context installs one sink. Ordinary capture failures
        // invalidate its ledger, but cannot change the transaction's result.
        public sealed class ProductionLedgerPoint
        {
            public int Faction, BankIndex, BankVersion, Materials, Fuel;
            public FactionTacticalMaterialsComponent Bank;
            public bool HasReservation;
            public SkirmishProductionReservation Reservation;
        }
        public sealed class ProductionLedgerObservation
        {
            public EntityManager Manager;
            public Entity Session, Actor;
            public string Operation;
            public int MemberOrdinal;
            public ProductionLedgerPoint Before, After;
            public bool HasDecision;
            public SkirmishProductionDecision Decision;
            public SkirmishAttemptOwnedComponent ActorOwner;
            public bool HasActorOwner;
        }
        // One optional immutable observation at the authoritative pre-Playing boundary.
        // No gameplay state or transaction result is changed if the observer fails.
        public static System.Action<EntityManager, Entity, byte> InitialPackageObserver { get; private set; }
        public static long InitialPackageObservationFaults { get; private set; }
        public static void OpenInitialPackageObserverOwned(System.Action<EntityManager, Entity, byte> sink)
        {
            if (sink == null) throw new System.ArgumentNullException(nameof(sink));
            if (InitialPackageObserver != null) throw new System.InvalidOperationException("Initial package already observed");
            InitialPackageObserver = sink;
        }
        public static bool CloseInitialPackageObserverOwned(System.Action<EntityManager, Entity, byte> sink)
        {
            if (sink == null || InitialPackageObserver != sink) return false;
            InitialPackageObserver = null; return true;
        }
        internal static void ObserveInitialPackage(EntityManager em, Entity session, byte visualPending)
        {
            var sink = InitialPackageObserver; if (sink == null) return;
            try { sink(em, session, visualPending); }
            catch (System.Exception error) when (!(error is System.OutOfMemoryException))
            { InitialPackageObservationFaults++; }
        }
        public static System.Action<ProductionLedgerObservation> ProductionLedgerObserver;
        public static long ProductionLedgerObservationFaults { get; private set; }
        private static ProductionLedgerPoint LedgerPoint(EntityManager em, Entity session, byte faction, uint id)
        {
            if (!em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
                throw new System.InvalidOperationException("Shared materials ledger required");
            Entity bank = Entity.Null;
            using (var query = em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var candidate in entities)
                    if (em.GetComponentData<FactionEconomy>(candidate).FactionId == faction)
                    {
                        if (bank != Entity.Null) throw new System.InvalidOperationException("Duplicate materials bank");
                        bank = candidate;
                    }
            if (bank == Entity.Null) throw new System.InvalidOperationException("Missing materials bank");
            var result = new ProductionLedgerPoint { Faction=faction, BankIndex=bank.Index, BankVersion=bank.Version,
                Bank=em.GetComponentData<FactionTacticalMaterialsComponent>(bank),
                Materials=SkirmishMaterialsService.Read(em,session,faction),
                Fuel=SkirmishStartingSupplyService.ReadFuel(em,session,faction) };
            if (id!=0 && TryGetReservation(em,session,id,out _,out var reservation))
            { result.HasReservation=true; result.Reservation=reservation; }
            return result;
        }
        private static ProductionLedgerPoint BeginLedger(EntityManager em,Entity session,byte faction,uint id)
        {
            if (ProductionLedgerObserver==null) return null;
            try { return LedgerPoint(em,session,faction,id); }
            catch (System.Exception error) when (!(error is System.OutOfMemoryException))
            { ProductionLedgerObservationFaults++; return null; }
        }
        private static void EndLedger(EntityManager em,Entity session,ProductionLedgerPoint before,string operation,
            uint id,SkirmishProductionDecision? decision=null,Entity actor=default,int memberOrdinal=-1)
        {
            if (before==null || ProductionLedgerObserver==null) return;
            try
            {
                var row=new ProductionLedgerObservation { Manager=em,Session=session,Actor=actor,Operation=operation,
                    Before=before,After=LedgerPoint(em,session,(byte)before.Faction,id),MemberOrdinal=memberOrdinal,
                    HasDecision=decision.HasValue,Decision=decision.GetValueOrDefault() };
                if (actor!=Entity.Null && em.Exists(actor) && em.HasComponent<SkirmishAttemptOwnedComponent>(actor))
                { row.HasActorOwner=true;row.ActorOwner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor); }
                ProductionLedgerObserver(row);
            }
            catch (System.Exception error) when (!(error is System.OutOfMemoryException))
            { ProductionLedgerObservationFaults++; }
        }

        private static void EndDirectLedgerMembers(EntityManager em,Entity session,byte faction,uint id,
            System.Collections.Generic.List<Entity> members)
        {
            if(members==null)return;
            for(int i=0;i<members.Count;i++)
                EndLedger(em,session,BeginLedger(em,session,faction,id),"DirectMember",id,null,members[i],i);
        }

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

            decision = EvaluateQueue(em, session, roleId, squadCount, army, factionId);
            if (!decision.Accepted) return false;
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
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

            var directLedgerBefore=BeginLedger(em,session,factionId,0);
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

            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            EndLedger(em,session,directLedgerBefore,"DirectDebit",reservationId,decision);
            var directDispatchBefore=BeginLedger(em,session,factionId,reservationId);
            var directMembers=directDispatchBefore==null?null:new System.Collections.Generic.List<Entity>();

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
                if (member == Entity.Null)
                {
                    DestroyUndispatchedMembers(em, sessionId, reservationId);
                    TryRefundFailedDispatch(em, session, reservationId, out decision);
                    decision.Accepted = false;
                    decision.Reason = SkirmishReasonCode.BlockedSpawn;
                    return false;
                }
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                owned.ReservationId = reservationId;
                em.SetComponentData(member, owned);
                if(directMembers!=null)directMembers.Add(member);
            }

            SkirmishCapacityLedger.PromoteLive(ref snapshot, category, decision.MemberCount, decision.SupplyCost);
            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            MarkReservation(em, session, reservationId, SkirmishReservationPhase.Live);
            EndLedger(em,session,directDispatchBefore,"DirectDispatch",reservationId,decision);
            EndDirectLedgerMembers(em,session,factionId,reservationId,directMembers);

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
            decision = EvaluateQueue(em, session, roleId, squadCount, army, factionId);
            if (!decision.Accepted) return false;
            SkirmishRoleIds.TryParse(roleId, out SkirmishRoleKind roleKind);
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);

            SkirmishPopulationCategory category = SkirmishRoleIds.Category(roleKind);
            if (!SkirmishCapacityLedger.TryReserve(ref snapshot, category, decision.MemberCount, decision.SupplyCost))
            {
                decision.Accepted = false;
                decision.Reason = SkirmishReasonCode.InsufficientCapacity;
                return false;
            }

            var ledgerBefore=BeginLedger(em,session,factionId,0);
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
            AriaCommandEvidence.Accepted("Production", factionId);
            EndLedger(em,session,ledgerBefore,"Queue",decision.ReservationId,decision);
            return true;
        }

        public static SkirmishProductionDecision EvaluateQueue(EntityManager em, Entity session,
            string roleId, int squadCount, SkirmishArmyProfileConfig army, byte factionId) =>
            EvaluateQueueInternal(em,session,roleId,squadCount,army,factionId,null);

        // Pure investment quote only. A projected unlock never authorizes a native
        // payment/queue: all acting APIs retain their current-readiness evaluation.
        public static SkirmishProductionDecision EvaluateQueueAtReadiness(EntityManager em, Entity session,
            string roleId, int squadCount, SkirmishArmyProfileConfig army, byte factionId,
            SkirmishReadinessStage projectedReadiness) =>
            EvaluateQueueInternal(em,session,roleId,squadCount,army,factionId,projectedReadiness);

        private static SkirmishProductionDecision EvaluateQueueInternal(EntityManager em, Entity session,
            string roleId, int squadCount, SkirmishArmyProfileConfig army, byte factionId,
            SkirmishReadinessStage? projectedReadiness)
        {
            SkirmishProductionDecision decision = default;
            if (em == default || !em.HasComponent<SkirmishResolvedSetupRecord>(session))
            {
                decision.Reason = SkirmishReasonCode.MissingResolvedSetup;
                return decision;
            }

            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null || !SkirmishRoleIds.TryParse(roleId, out SkirmishRoleKind roleKind))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedRole;
                return decision;
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
                request, army, projectedReadiness ?? LiveReadiness(em, session, setup.Readiness, factionId), setup.RoleOverlays);
            if (decision.Accepted && SkirmishRoleIds.Category(roleKind) == SkirmishPopulationCategory.LogisticsSupport &&
                CountLogisticsSupport(em, session, factionId) + decision.MemberCount > setup.LogisticsSupportCapEach)
            {
                decision.Accepted = false;
                decision.Reason = SkirmishReasonCode.InsufficientCapacity;
                decision.Field = "capacity.logistics";
            }
            return decision;
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

            var queuedStartBefore=BeginLedger(em,session,reservation.FactionId,reservationId);
            reservation.Phase = SkirmishReservationPhase.Producing;
            DynamicBuffer<SkirmishProductionReservation> started = em.GetBuffer<SkirmishProductionReservation>(session);
            started[index] = reservation;
            EndLedger(em,session,queuedStartBefore,"StartQueued",reservationId);
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
            var queuedMembers=ProductionLedgerObserver==null?null:new System.Collections.Generic.List<Entity>();
            System.Collections.Generic.Dictionary<string, Entity> prefabLookup =
                SkirmishScenarioSpawnSystem.BuildPrefabEntityLookup(em);
            for (int i = 0; i < reservation.MemberCount; i++)
            {
                Entity member = SkirmishScenarioSpawnSystem.CreateForceMember(
                    em, sessionId, force, (int)reservationId, i, perMemberSupply, prefabKey, prefabLookup, setup);
                if (member == Entity.Null)
                {
                    DestroyUndispatchedMembers(em, sessionId, reservationId);
                    TryRefundFailedDispatch(em, session, reservationId, out decision);
                    decision.Accepted = false;
                    decision.Reason = SkirmishReasonCode.BlockedSpawn;
                    return false;
                }
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(member);
                owned.ReservationId = reservationId;
                em.SetComponentData(member, owned);
                if(queuedMembers!=null)queuedMembers.Add(member);
            }

            var dispatchBefore=BeginLedger(em,session,reservation.FactionId,reservationId);
            SkirmishCapacityLedger.PromoteLive(ref snapshot, reservation.Category, UndeliveredMembers(reservation), UndeliveredSupply(reservation));
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            MarkReservation(em, session, reservationId, SkirmishReservationPhase.Live);
            EndLedger(em,session,dispatchBefore,"QueuedDirectDispatch",reservationId);
            EndDirectLedgerMembers(em,session,reservation.FactionId,reservationId,queuedMembers);

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

            int refund = SkirmishResearchCosts.RefundProduce(UndeliveredPayment(reservation), reservation.Phase);
            ReadStocks(em, session, reservation.FactionId, out int materials, out int fuel, out var snapshot, out uint next);
            if (!SkirmishCapacityLedger.TryReleaseReserved(
                    ref snapshot, reservation.Category, UndeliveredMembers(reservation), UndeliveredSupply(reservation)))
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                return false;
            }

            var ledgerBefore=BeginLedger(em,session,reservation.FactionId,reservationId);
            materials += refund;
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            reservation.Phase = SkirmishReservationPhase.Cancelled;
            reservation.RemainingMembers = System.Math.Max(0, reservation.RemainingMembers - UndeliveredMembers(reservation));
            DynamicBuffer<SkirmishProductionReservation> cancelled = em.GetBuffer<SkirmishProductionReservation>(session);
            cancelled[index] = reservation;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.RefundedMaterials = refund;
            EndLedger(em,session,ledgerBefore,"Cancel",reservationId,decision);
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
                    ref snapshot, reservation.Category, UndeliveredMembers(reservation), UndeliveredSupply(reservation)))
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                return false;
            }

            var ledgerBefore=BeginLedger(em,session,reservation.FactionId,reservationId);
            materials += UndeliveredPayment(reservation);
            WriteStocks(em, session, reservation.FactionId, materials, fuel, snapshot, next);
            reservation.Phase = SkirmishReservationPhase.Cancelled;
            reservation.RemainingMembers = System.Math.Max(0, reservation.RemainingMembers - UndeliveredMembers(reservation));
            DynamicBuffer<SkirmishProductionReservation> failed = em.GetBuffer<SkirmishProductionReservation>(session);
            failed[index] = reservation;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.ReservationId = reservationId;
            decision.MaterialsCost = reservation.MaterialsPaid;
            decision.RefundedMaterials = UndeliveredPayment(reservation);
            EndLedger(em,session,ledgerBefore,"FailedDispatchRefund",reservationId,decision);
            return true;
        }

        public static int NotifyProducerDestroyed(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId,
            int runtimeProducerId = 0)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                return 0;
            DynamicBuffer<SkirmishProductionReservation> buffer = em.GetBuffer<SkirmishProductionReservation>(session);
            int changed = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishProductionReservation reservation = buffer[i];
                if (reservation.FactionId != factionId || reservation.Producer != producer ||
                    reservation.ProducerRuntimeId != runtimeProducerId)
                    continue;
                if (reservation.Phase == SkirmishReservationPhase.Reserved)
                {
                    TryRefundFailedDispatch(em, session, reservation.ReservationId, out _);
                    changed++;
                    continue;
                }

                if (reservation.Phase != SkirmishReservationPhase.Producing)
                    continue;
                var ledgerBefore=BeginLedger(em,session,factionId,reservation.ReservationId);
                ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint next);
                SkirmishCapacityLedger.TryReleaseReserved(
                    ref snapshot, reservation.Category, UndeliveredMembers(reservation), UndeliveredSupply(reservation));
                WriteStocks(em, session, factionId, materials, fuel, snapshot, next);
                reservation.Phase = SkirmishReservationPhase.Lost;
                reservation.RemainingMembers = System.Math.Max(0, reservation.RemainingMembers - UndeliveredMembers(reservation));
                buffer = em.GetBuffer<SkirmishProductionReservation>(session);
                buffer[i] = reservation;
                EndLedger(em,session,ledgerBefore,"ProducerLost",reservation.ReservationId);
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
            if (owned.CapacityReleased != 0 || owned.IsStructure != 0 ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !owned.SessionId.Equals(em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId))
                return false;

            SkirmishUnitRoleComponent role = em.GetComponentData<SkirmishUnitRoleComponent>(unit);
            if (!em.HasComponent<SkirmishCapacityComponent>(session) &&
                !em.HasComponent<SkirmishEnemyCapacityComponent>(session))
                return false;

            byte factionId = owned.FactionId == 0 ? (byte)1 : owned.FactionId;
            var deathBefore=BeginLedger(em,session,factionId,owned.ReservationId);
            ReadStocks(em, session, factionId, out int materials, out int fuel, out var snapshot, out uint nextReservation);
            if (!SkirmishCapacityLedger.TryReleaseDeath(ref snapshot, role.Category, role.SupplyCost))
                return false;

            if(em.HasComponent<SkirmishSupportPassengerCapacity>(unit))
            {var cleanup=em.GetComponentData<SkirmishSupportPassengerCapacity>(unit);cleanup.Released=1;em.SetComponentData(unit,cleanup);}
            owned.CapacityReleased = 1;
            em.SetComponentData(unit, owned);
            WriteStocks(em, session, factionId, materials, fuel, snapshot, nextReservation);
            DecrementReservation(em, session, owned.ReservationId);
            EndLedger(em,session,deathBefore,"DeathRelease",owned.ReservationId,null,unit);
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

        private static void DestroyUndispatchedMembers(EntityManager em, FixedString64Bytes sessionId, uint reservationId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]);
                if (owned.SessionId.Equals(sessionId) && owned.ReservationId == reservationId)
                    em.DestroyEntity(entities[i]);
            }
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
                materials = SkirmishMaterialsService.Read(em, session, factionId);
                fuel = SkirmishStartingSupplyService.ReadFuel(em, session, factionId);
                snapshot = ToEnemySnapshot(enemyCap);
                nextReservation = System.Math.Max(enemyCap.NextReservationId,
                    em.HasComponent<SkirmishCapacityComponent>(session)
                        ? em.GetComponentData<SkirmishCapacityComponent>(session).NextReservationId : 0u);
                return;
            }

            var stock = em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session)
                : default;
            var capacity = em.HasComponent<SkirmishCapacityComponent>(session)
                ? em.GetComponentData<SkirmishCapacityComponent>(session)
                : default;
            materials = SkirmishMaterialsService.Read(em, session, factionId);
            fuel = SkirmishStartingSupplyService.ReadFuel(em, session, factionId);
            snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
            // Both factions share the reservation buffer and lookup key.
            // Allocate from one monotonic sequence even though capacity is stored per faction.
            nextReservation = System.Math.Max(capacity.NextReservationId,
                em.HasComponent<SkirmishEnemyCapacityComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyCapacityComponent>(session).NextReservationId : 0u);
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
            if (em.HasComponent<SkirmishSharedSupplyInitialized>(session) &&
                !SkirmishStartingSupplyService.TryWriteFuel(em, session, factionId, fuel))
                throw new System.InvalidOperationException("Accepted production transaction cannot update physical fuel stores.");
            SkirmishMaterialsService.WriteTransaction(em, session, factionId, materials, FactionTacticalMaterialsSpendKind.Production);
            if (factionId == 2)
            {
                var enemyStock = em.HasComponent<SkirmishEnemyStockComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyStockComponent>(session)
                    : new SkirmishEnemyStockComponent();
                if (!em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
                    enemyStock.Materials = materials;
                if (!em.HasComponent<SkirmishSharedSupplyInitialized>(session)) enemyStock.Fuel = fuel;
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
            if (!em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
                stock.Materials = materials;
            if (!em.HasComponent<SkirmishSharedSupplyInitialized>(session)) stock.Fuel = fuel;
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
            SkirmishReadinessStage compiled, byte factionId = 1)
        {
            if (!em.HasComponent<SkirmishResearchStateComponent>(session))
                return compiled;
            if (!SkirmishResearchService.TryReadState(em, session, factionId, out var research)) return compiled;
            SkirmishReadinessStage live = research.Readiness;
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

            if ((int)LiveReadiness(em, session, compiled, reservation.FactionId) < (int)required)
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
