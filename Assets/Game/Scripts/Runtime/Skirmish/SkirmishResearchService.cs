using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishResearchService
    {
        public sealed class ResearchLedgerPoint
        {
            public Entity BankEntity;
            public FactionTacticalMaterialsComponent Bank;
            public int Materials,Fuel;
            public uint NextResearchId;
            public SkirmishResearchStateComponent State;
            public bool HasItem;
            public SkirmishResearchQueueItem Item;
        }
        public sealed class ResearchLedgerObservation
        {
            public EntityManager Manager;
            public Entity Session;
            public string Operation;
            public ResearchLedgerPoint Before,After;
            public bool HasDecision;
            public SkirmishResearchDecision Decision;
        }
        public static System.Action<ResearchLedgerObservation> ResearchLedgerObserver;
        public static long ResearchLedgerObservationFaults {get;private set;}
        private static ResearchLedgerPoint ResearchPoint(EntityManager em,Entity session,byte faction,uint id)
        {
            if(!em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
                throw new System.InvalidOperationException("Research ledger requires actual shared materials");
            Entity bank=Entity.Null;
            using(var query=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))
            using(var entities=query.ToEntityArray(Allocator.Temp))
                foreach(var candidate in entities)
                    if(em.GetComponentData<FactionEconomy>(candidate).FactionId==faction)
                    {
                        if(bank!=Entity.Null)throw new System.InvalidOperationException("Duplicate research materials bank");
                        bank=candidate;
                    }
            if(bank==Entity.Null || !TryReadState(em,session,faction,out var state))
                throw new System.InvalidOperationException("Missing actual research bank/state");
            var point=new ResearchLedgerPoint {BankEntity=bank,Bank=em.GetComponentData<FactionTacticalMaterialsComponent>(bank),
                Materials=SkirmishMaterialsService.Read(em,session,faction),Fuel=SkirmishStartingSupplyService.ReadFuel(em,session,faction),
                State=state,NextResearchId=em.GetComponentData<SkirmishResearchStateComponent>(session).NextResearchId};
            if(id!=0 && TryGet(em,session,id,out _,out var item)){point.HasItem=true;point.Item=item;}
            return point;
        }
        private static ResearchLedgerPoint BeginResearchLedger(EntityManager em,Entity session,byte faction,uint id)
        {
            if(ResearchLedgerObserver==null)return null;
            try{return ResearchPoint(em,session,faction,id);}
            catch(System.Exception error)when(!(error is System.OutOfMemoryException)){ResearchLedgerObservationFaults++;return null;}
        }
        private static void EndResearchLedger(EntityManager em,Entity session,ResearchLedgerPoint before,string operation,uint id,
            SkirmishResearchDecision? decision=null)
        {
            if(before==null || ResearchLedgerObserver==null)return;
            try
            {
                ResearchLedgerObserver(new ResearchLedgerObservation {Manager=em,Session=session,Operation=operation,
                    Before=before,After=ResearchPoint(em,session,before.State.FactionId,id),HasDecision=decision.HasValue,
                    Decision=decision.GetValueOrDefault()});
            }
            catch(System.Exception error)when(!(error is System.OutOfMemoryException)){ResearchLedgerObservationFaults++;}
        }

        public static void EnsureSession(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (!em.HasComponent<SkirmishResearchStateComponent>(session))
            {
                em.AddComponentData(session, new SkirmishResearchStateComponent
                {
                    FactionId = 1,
                    Readiness = setup != null ? setup.Readiness : SkirmishReadinessStage.Field
                });
            }

            if (!em.HasComponent<SkirmishEnemyResearchStateComponent>(session))
                em.AddComponentData(session, new SkirmishEnemyResearchStateComponent { State =
                    new SkirmishResearchStateComponent { FactionId = 2,
                        Readiness = setup != null ? setup.Readiness : SkirmishReadinessStage.Field } });

            if (!em.HasBuffer<SkirmishResearchQueueItem>(session))
                em.AddBuffer<SkirmishResearchQueueItem>(session);
        }

        // Faction two owns a separate state. Missing legacy enemy state means the
        // compiled opening stage, never the player’s paid progress. This read is pure.
        public static bool TryReadState(EntityManager em, Entity session, byte factionId,
            out SkirmishResearchStateComponent state)
        {
            state = default;
            if (factionId != 1 && factionId != 2 || !em.Exists(session) ||
                !em.HasComponent<SkirmishResearchStateComponent>(session)) return false;
            if (factionId == 1)
            {
                state = em.GetComponentData<SkirmishResearchStateComponent>(session);
                return state.FactionId == 1;
            }
            if (em.HasComponent<SkirmishEnemyResearchStateComponent>(session))
            {
                state = em.GetComponentData<SkirmishEnemyResearchStateComponent>(session).State;
                return state.FactionId == 2;
            }
            var setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup : null;
            state = new SkirmishResearchStateComponent { FactionId = 2,
                Readiness = setup != null ? setup.Readiness : SkirmishReadinessStage.Field };
            return true;
        }

        private static void WriteState(EntityManager em, Entity session, SkirmishResearchStateComponent state)
        {
            if (state.FactionId == 1) em.SetComponentData(session, state);
            else
            {
                var enemy = new SkirmishEnemyResearchStateComponent { State = state };
                if (em.HasComponent<SkirmishEnemyResearchStateComponent>(session)) em.SetComponentData(session, enemy);
                else em.AddComponentData(session, enemy);
            }
        }

        // Read-only eligibility used by the visible readiness control and ARIA.
        public static bool CanQueue(EntityManager em, Entity session, SkirmishResearchKind kind,
            byte factionId, out SkirmishResearchDecision decision, out float seconds)
        {
            seconds = 0;
            decision = new SkirmishResearchDecision { Reason = SkirmishReasonCode.MissingReference };
            if (!em.HasComponent<SkirmishResearchStateComponent>(session) ||
                !em.HasBuffer<SkirmishResearchQueueItem>(session)) return false;
            return TryPrepare(em, session, kind, factionId, out decision, out _, out _, out seconds);
        }

        public static bool TryQueue(
            EntityManager em,
            Entity session,
            SkirmishResearchKind kind,
            byte factionId,
            out SkirmishResearchDecision decision)
        {
            EnsureSession(em, session, em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup : null);
            decision = new SkirmishResearchDecision { Kind = kind, Field = "research" };
            if (!TryPrepare(em, session, kind, factionId, out decision, out SkirmishProducerKind producer, out int cost, out float seconds))
                return false;
            var ledgerBefore=BeginResearchLedger(em,session,factionId,0);
            if (!TryDebitMaterials(em, session, factionId, cost))
            {
                decision.Reason = SkirmishReasonCode.InsufficientMaterials;
                decision.Field = "materials";
                decision.MaterialsCost = cost;
                return false;
            }

            var state = em.GetComponentData<SkirmishResearchStateComponent>(session);
            state.NextResearchId++;
            uint id = state.NextResearchId;
            em.SetComponentData(session, state);
            em.GetBuffer<SkirmishResearchQueueItem>(session).Add(new SkirmishResearchQueueItem
            {
                ResearchId = id,
                Kind = kind,
                Phase = SkirmishResearchPhase.Queued,
                MaterialsPaid = cost,
                RemainingSeconds = seconds,
                FactionId = factionId,
                Producer = producer
            });
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.MaterialsCost = cost;
            decision.ResearchId = id;
            decision.Phase = SkirmishResearchPhase.Queued;
            AriaCommandEvidence.Accepted("Research", factionId);
            EndResearchLedger(em,session,ledgerBefore,"Queue",id,decision);
            return true;
        }

        public static bool TryStart(
            EntityManager em,
            Entity session,
            uint researchId,
            out SkirmishResearchDecision decision)
        {
            decision = default;
            if (!TryGet(em, session, researchId, out int index, out SkirmishResearchQueueItem item))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            decision.Kind = item.Kind;
            decision.ResearchId = researchId;
            decision.MaterialsCost = item.MaterialsPaid;
            if (item.Phase != SkirmishResearchPhase.Queued)
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                decision.Phase = item.Phase;
                return false;
            }

            if (SkirmishProductionService.IsProducerLocked(em, session, item.Producer, item.FactionId))
            {
                decision.Reason = SkirmishReasonCode.QueueLocked;
                decision.Field = "queue";
                return false;
            }

            var ledgerBefore=BeginResearchLedger(em,session,item.FactionId,researchId);
            item.Phase = SkirmishResearchPhase.Researching;
            DynamicBuffer<SkirmishResearchQueueItem> started = em.GetBuffer<SkirmishResearchQueueItem>(session);
            started[index] = item;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Researching;
            EndResearchLedger(em,session,ledgerBefore,"Start",researchId,decision);
            return true;
        }

        public static bool TryComplete(
            EntityManager em,
            Entity session,
            uint researchId,
            out SkirmishResearchDecision decision)
        {
            decision = default;
            if (!TryGet(em, session, researchId, out int index, out SkirmishResearchQueueItem item))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            decision.Kind = item.Kind;
            decision.ResearchId = researchId;
            decision.MaterialsCost = item.MaterialsPaid;
            if (item.Phase != SkirmishResearchPhase.Researching && item.Phase != SkirmishResearchPhase.Queued)
            {
                decision.Reason = SkirmishReasonCode.StaleCommand;
                decision.Phase = item.Phase;
                return false;
            }

            if (!TryReadState(em, session, item.FactionId, out _))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedCapability;
                decision.Field = "faction";
                return false;
            }

            var ledgerBefore=BeginResearchLedger(em,session,item.FactionId,researchId);
            item.Phase = SkirmishResearchPhase.Completed;
            item.RemainingSeconds = 0f;
            DynamicBuffer<SkirmishResearchQueueItem> completed = em.GetBuffer<SkirmishResearchQueueItem>(session);
            completed[index] = item;
            Grant(em, session, item);
            SkirmishResearchEffects.ApplyCompleted(em, session, item.FactionId);
            TryReadState(em, session, item.FactionId, out var state);
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Completed;
            decision.ReadinessAfter = state.Readiness;
            decision.LevelAfter = LevelOf(state, item.Kind);
            EndResearchLedger(em,session,ledgerBefore,"Complete",researchId,decision);
            return true;
        }

        public static bool TryCancel(
            EntityManager em,
            Entity session,
            uint researchId,
            out SkirmishResearchDecision decision)
        {
            decision = default;
            if (!TryGet(em, session, researchId, out int index, out SkirmishResearchQueueItem item))
            {
                decision.Reason = SkirmishReasonCode.MissingReference;
                return false;
            }

            decision.Kind = item.Kind;
            decision.ResearchId = researchId;
            decision.MaterialsCost = item.MaterialsPaid;
            if (item.Phase != SkirmishResearchPhase.Queued && item.Phase != SkirmishResearchPhase.Researching)
            {
                decision.Reason = SkirmishReasonCode.NotCancellable;
                decision.Phase = item.Phase;
                return false;
            }

            int refund = SkirmishResearchCosts.RefundMaterials(item.MaterialsPaid, item.Phase);
            var ledgerBefore=BeginResearchLedger(em,session,item.FactionId,researchId);
            CreditMaterials(em, session, item.FactionId, refund);
            item.Phase = SkirmishResearchPhase.Cancelled;
            DynamicBuffer<SkirmishResearchQueueItem> cancelled = em.GetBuffer<SkirmishResearchQueueItem>(session);
            cancelled[index] = item;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Cancelled;
            decision.RefundedMaterials = refund;
            EndResearchLedger(em,session,ledgerBefore,"Cancel",researchId,decision);
            return true;
        }

        public static int NotifyProducerDestroyed(
            EntityManager em,
            Entity session,
            SkirmishProducerKind producer,
            byte factionId)
        {
            if (!em.HasBuffer<SkirmishResearchQueueItem>(session))
                return 0;
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            int lost = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishResearchQueueItem item = buffer[i];
                if (item.FactionId != factionId || item.Producer != producer)
                    continue;
                if (item.Phase != SkirmishResearchPhase.Queued && item.Phase != SkirmishResearchPhase.Researching)
                    continue;
                var ledgerBefore=BeginResearchLedger(em,session,item.FactionId,item.ResearchId);
                item.Phase = SkirmishResearchPhase.Lost;
                buffer[i] = item;
                EndResearchLedger(em,session,ledgerBefore,"Lost",item.ResearchId);
                lost++;
            }

            return lost;
        }

        public static int Tick(EntityManager em, Entity session, float deltaSeconds, bool paused)
        {
            if (paused || deltaSeconds <= 0f || !em.HasBuffer<SkirmishResearchQueueItem>(session))
                return 0;
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            int completed = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                SkirmishResearchQueueItem item = buffer[i];
                if (item.Phase == SkirmishResearchPhase.Queued)
                {
                    if (!SkirmishProductionService.HasLivingProducer(em, session, item.Producer, item.FactionId) ||
                        !TryStart(em, session, item.ResearchId, out _)) continue;
                    item = buffer[i];
                }
                if (item.Phase != SkirmishResearchPhase.Researching)
                    continue;
                item.RemainingSeconds -= deltaSeconds;
                buffer[i] = item;
                if (item.RemainingSeconds > 0f)
                    continue;
                if (TryComplete(em, session, item.ResearchId, out _))
                    completed++;
                // Completion can add upgrade stamps to actors and invalidate
                // every cached DynamicBuffer handle in the world.
                buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            }

            return completed;
        }

        public static bool HasCompleted(EntityManager em, Entity session, SkirmishResearchKind kind, byte factionId = 1)
        {
            if (!TryReadState(em, session, factionId, out var state)) return false;
            return LevelOf(state, kind) != 0;
        }

        private static bool TryPrepare(
            EntityManager em,
            Entity session,
            SkirmishResearchKind kind,
            byte factionId,
            out SkirmishResearchDecision decision,
            out SkirmishProducerKind producer,
            out int cost,
            out float seconds)
        {
            decision = new SkirmishResearchDecision { Kind = kind, Field = "research" };
            producer = SkirmishProducerKind.None;
            cost = 0;
            seconds = 0f;
            if (kind == SkirmishResearchKind.None)
            {
                decision.Reason = SkirmishReasonCode.UnsupportedCapability;
                return false;
            }

            if (!TryReadState(em, session, factionId, out var state))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedCapability;
                decision.Field = "faction";
                return false;
            }
            if (kind != SkirmishResearchKind.Readiness && state.Readiness < SkirmishReadinessStage.Established)
            {
                decision.Reason = SkirmishReasonCode.MissingReadiness;
                decision.Field = "readiness";
                return false;
            }

            if (kind == SkirmishResearchKind.Readiness)
            {
                if (HasOpen(em, session, kind, factionId))
                {
                    decision.Reason = SkirmishReasonCode.QueueLocked;
                    decision.Field = "queue";
                    return false;
                }
                if (state.Readiness >= SkirmishReadinessStage.FullArsenal)
                {
                    decision.Reason = SkirmishReasonCode.AlreadyCompleted;
                    return false;
                }

                bool toFull = state.Readiness >= SkirmishReadinessStage.Established;
                cost = toFull
                    ? SkirmishResearchCosts.ReadinessFullArsenalMaterials
                    : SkirmishResearchCosts.ReadinessEstablishedMaterials;
                seconds = toFull
                    ? SkirmishResearchCosts.ReadinessFullArsenalSeconds
                    : SkirmishResearchCosts.ReadinessEstablishedSeconds;
                producer = SkirmishProducerKind.Barracks;
            }
            else
            {
                if (LevelOf(state, kind) != 0 || HasOpen(em, session, kind, factionId))
                {
                    decision.Reason = SkirmishReasonCode.AlreadyCompleted;
                    return false;
                }

                cost = SkirmishResearchCosts.CategoryMaterials;
                seconds = SkirmishResearchCosts.CategorySeconds;
                producer = kind == SkirmishResearchKind.VehicleProtection
                    ? SkirmishProducerKind.GroundStaging
                    : kind == SkirmishResearchKind.AircraftEfficiency
                        ? SkirmishProducerKind.Helipad
                        : SkirmishProducerKind.Barracks;
            }

            decision.MaterialsCost = cost;
            if (kind == SkirmishResearchKind.AircraftEfficiency &&
                !SkirmishProductionService.HasLivingProducer(em, session, producer, factionId) &&
                SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Airport, factionId))
                producer = SkirmishProducerKind.Airport;
            if (!SkirmishProductionService.HasLivingProducer(em, session, producer, factionId) &&
                !(kind == SkirmishResearchKind.AircraftEfficiency &&
                  SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Airport, factionId)))
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = producer == SkirmishProducerKind.GroundStaging
                    ? "producer.ground_staging"
                    : producer == SkirmishProducerKind.Helipad
                        ? "producer.air"
                        : "producer.barracks";
                return false;
            }

            if (kind == SkirmishResearchKind.Readiness &&
                state.Readiness >= SkirmishReadinessStage.Established &&
                !HasFunctionalProduction(em, session, factionId))
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "production";
                return false;
            }

            int materials = ReadMaterials(em, session, factionId);
            decision.MaterialsCost = cost;
            if (materials < cost)
            {
                decision.Reason = SkirmishReasonCode.InsufficientMaterials;
                decision.Field = "materials";
                return false;
            }

            return true;
        }

        private static bool HasFunctionalProduction(EntityManager em, Entity session, byte factionId)
        {
            return SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Barracks, factionId) ||
                   SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.GroundStaging, factionId);
        }

        private static void Grant(EntityManager em, Entity session, SkirmishResearchQueueItem item)
        {
            TryReadState(em, session, item.FactionId, out var state);
            if (item.Kind == SkirmishResearchKind.Readiness)
            {
                if (state.Readiness < SkirmishReadinessStage.Established)
                    state.Readiness = SkirmishReadinessStage.Established;
                else if (state.Readiness < SkirmishReadinessStage.FullArsenal)
                    state.Readiness = SkirmishReadinessStage.FullArsenal;
            }
            else if (item.Kind == SkirmishResearchKind.InfantryWeapons)
                state.InfantryWeapons = 1;
            else if (item.Kind == SkirmishResearchKind.VehicleProtection)
                state.VehicleProtection = 1;
            else if (item.Kind == SkirmishResearchKind.AircraftEfficiency)
                state.AircraftEfficiency = 1;
            WriteState(em, session, state);
        }

        private static byte LevelOf(SkirmishResearchStateComponent state, SkirmishResearchKind kind)
        {
            if (kind == SkirmishResearchKind.InfantryWeapons)
                return state.InfantryWeapons;
            if (kind == SkirmishResearchKind.VehicleProtection)
                return state.VehicleProtection;
            if (kind == SkirmishResearchKind.AircraftEfficiency)
                return state.AircraftEfficiency;
            if (kind == SkirmishResearchKind.Readiness)
                return (byte)state.Readiness;
            return 0;
        }

        private static bool HasOpen(EntityManager em, Entity session, SkirmishResearchKind kind, byte factionId)
        {
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Kind == kind &&
                    buffer[i].FactionId == factionId &&
                    (buffer[i].Phase == SkirmishResearchPhase.Queued ||
                     buffer[i].Phase == SkirmishResearchPhase.Researching))
                    return true;
            }

            return false;
        }

        private static bool TryGet(
            EntityManager em,
            Entity session,
            uint researchId,
            out int index,
            out SkirmishResearchQueueItem item)
        {
            index = -1;
            item = default;
            if (researchId == 0 || !em.HasBuffer<SkirmishResearchQueueItem>(session))
                return false;
            DynamicBuffer<SkirmishResearchQueueItem> buffer = em.GetBuffer<SkirmishResearchQueueItem>(session);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].ResearchId != researchId)
                    continue;
                index = i;
                item = buffer[i];
                return true;
            }

            return false;
        }

        private static int ReadMaterials(EntityManager em, Entity session, byte factionId) =>
            SkirmishMaterialsService.Read(em, session, factionId);

        private static bool TryDebitMaterials(EntityManager em, Entity session, byte factionId, int cost)
        {
            int available = ReadMaterials(em, session, factionId);
            if (cost < 0 || available < cost) return false;
            SkirmishMaterialsService.WriteTransaction(em, session, factionId, available - cost, FactionTacticalMaterialsSpendKind.Upgrade);
            return true;
        }

        private static void CreditMaterials(EntityManager em, Entity session, byte factionId, int amount)
        {
            if (amount > 0)
                SkirmishMaterialsService.WriteTransaction(em, session, factionId, ReadMaterials(em, session, factionId) + amount, FactionTacticalMaterialsSpendKind.Upgrade);
        }
    }
}
