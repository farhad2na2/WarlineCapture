using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishResearchService
    {
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

            if (!em.HasBuffer<SkirmishResearchQueueItem>(session))
                em.AddBuffer<SkirmishResearchQueueItem>(session);
        }

        public static bool TryQueue(
            EntityManager em,
            Entity session,
            SkirmishResearchKind kind,
            byte factionId,
            out SkirmishResearchDecision decision)
        {
            decision = new SkirmishResearchDecision { Kind = kind, Field = "research" };
            if (!TryPrepare(em, session, kind, factionId, out decision, out SkirmishProducerKind producer, out int cost, out float seconds))
                return false;
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

            item.Phase = SkirmishResearchPhase.Researching;
            DynamicBuffer<SkirmishResearchQueueItem> started = em.GetBuffer<SkirmishResearchQueueItem>(session);
            started[index] = item;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Researching;
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

            item.Phase = SkirmishResearchPhase.Completed;
            item.RemainingSeconds = 0f;
            DynamicBuffer<SkirmishResearchQueueItem> completed = em.GetBuffer<SkirmishResearchQueueItem>(session);
            completed[index] = item;
            Grant(em, session, item);
            SkirmishResearchEffects.ApplyCompleted(em, session, item.FactionId);
            var state = em.GetComponentData<SkirmishResearchStateComponent>(session);
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Completed;
            decision.ReadinessAfter = state.Readiness;
            decision.LevelAfter = LevelOf(state, item.Kind);
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
            CreditMaterials(em, session, item.FactionId, refund);
            item.Phase = SkirmishResearchPhase.Cancelled;
            DynamicBuffer<SkirmishResearchQueueItem> cancelled = em.GetBuffer<SkirmishResearchQueueItem>(session);
            cancelled[index] = item;
            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Phase = SkirmishResearchPhase.Cancelled;
            decision.RefundedMaterials = refund;
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
                item.Phase = SkirmishResearchPhase.Lost;
                buffer[i] = item;
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
                if (item.Phase != SkirmishResearchPhase.Researching)
                    continue;
                item.RemainingSeconds -= deltaSeconds;
                buffer[i] = item;
                if (item.RemainingSeconds > 0f)
                    continue;
                if (TryComplete(em, session, item.ResearchId, out _))
                    completed++;
            }

            return completed;
        }

        public static bool HasCompleted(EntityManager em, Entity session, SkirmishResearchKind kind)
        {
            if (!em.HasComponent<SkirmishResearchStateComponent>(session))
                return false;
            var state = em.GetComponentData<SkirmishResearchStateComponent>(session);
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
            EnsureSession(em, session, em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup
                : null);
            if (kind == SkirmishResearchKind.None)
            {
                decision.Reason = SkirmishReasonCode.UnsupportedCapability;
                return false;
            }

            var state = em.GetComponentData<SkirmishResearchStateComponent>(session);
            if (kind != SkirmishResearchKind.Readiness && state.Readiness < SkirmishReadinessStage.Established)
            {
                decision.Reason = SkirmishReasonCode.MissingReadiness;
                decision.Field = "readiness";
                return false;
            }

            if (kind == SkirmishResearchKind.Readiness)
            {
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
            var state = em.GetComponentData<SkirmishResearchStateComponent>(session);
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
            em.SetComponentData(session, state);
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

        private static int ReadMaterials(EntityManager em, Entity session, byte factionId)
        {
            if (factionId == 2 && em.HasComponent<SkirmishEnemyStockComponent>(session))
                return em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials;
            return em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials
                : 0;
        }

        private static bool TryDebitMaterials(EntityManager em, Entity session, byte factionId, int cost)
        {
            if (factionId == 2 && em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                if (stock.Materials < cost)
                    return false;
                stock.Materials -= cost;
                em.SetComponentData(session, stock);
                return true;
            }

            if (!em.HasComponent<SkirmishEconomyStockComponent>(session))
                return false;
            var player = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            if (player.Materials < cost)
                return false;
            player.Materials -= cost;
            em.SetComponentData(session, player);
            return true;
        }

        private static void CreditMaterials(EntityManager em, Entity session, byte factionId, int amount)
        {
            if (amount <= 0)
                return;
            if (factionId == 2 && em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                stock.Materials += amount;
                em.SetComponentData(session, stock);
                return;
            }

            if (!em.HasComponent<SkirmishEconomyStockComponent>(session))
                return;
            var player = em.GetComponentData<SkirmishEconomyStockComponent>(session);
            player.Materials += amount;
            em.SetComponentData(session, player);
        }
    }
}
