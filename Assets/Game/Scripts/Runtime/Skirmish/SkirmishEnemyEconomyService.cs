using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public enum SkirmishEnemyEconomyAction : byte
    { None, PaidIntentPending, PreserveDefense, LogisticsRequested, ResearchPending, ReadinessQueued }

    // No new bank, research state or production intent. Existing registered native
    // records are authoritative across Continue; this service only chooses a spend.
    public static class SkirmishEnemyEconomyService
    {
        public struct Decision
        {
            public SkirmishEnemyEconomyAction Action;
            public bool PermitCombatPurchase;
            public int ReserveMaterials;
            public uint ResearchId;
            public SkirmishRoleKind SuggestedCombatRole;
        }
        public struct Context
        {
            public int Materials, LogisticsMembers, LogisticsReplacementCost;
            public float Elapsed, Remaining;
            public SkirmishReadinessStage Readiness;
            public uint OpenResearchId;
        }

        public static bool TryReadContext(EntityManager em, Entity session, byte actingFaction,
            out Context context)
        {
            context = default;
            if (em == default || !em.Exists(session) || actingFaction != 2 ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session) ||
                !em.HasComponent<SkirmishEnemyPlannerV1Component>(session) ||
                !SkirmishEnemyPlannerService.TryReadActiveClock(em, session, out float now)) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var current = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (setup?.EnemyPolicy == null || setup.EnemyFaction != actingFaction ||
                (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault && !SkirmishEnemyObjectivePolicyScope.Supports(setup)) || current.SessionId.IsEmpty ||
                current.SpawnComplete == 0 || !em.HasBuffer<SkirmishResearchQueueItem>(session) ||
                !SkirmishResearchService.TryReadState(em,session,actingFaction,out var research)) return false;
            var planner = em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            var intent = em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
            if (planner.Initialized != 1 || planner.PolicyVersion != setup.EnemyPolicy.Version ||
                planner.PolicyHash != setup.EnemyPolicy.Hash ||
                planner.PolicyFingerprint.ToString() != setup.EnemyPolicy.Fingerprint || intent.Version != 1) return false;
            context.Elapsed = now;
            context.Remaining = setup.DeadlineSeconds - now;
            context.Materials = SkirmishMaterialsService.Read(em,session,actingFaction);
            context.Readiness = research.Readiness;
            context.LogisticsMembers = CountOwnedOrPending(em,session,actingFaction,SkirmishRoleKind.LogisticsTruck);
            var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            if (army != null && army.Allows(SkirmishRoleIds.LogisticsTruck) &&
                SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,SkirmishRoleKind.LogisticsTruck,out var truck) &&
                SkirmishProductionService.HasLivingProducer(em,session,truck.Producer,actingFaction))
                context.LogisticsReplacementCost = truck.MaterialsCost;
            foreach (var row in em.GetBuffer<SkirmishResearchQueueItem>(session))
                if (row.FactionId == actingFaction && (row.Phase == SkirmishResearchPhase.Queued ||
                    row.Phase == SkirmishResearchPhase.Researching))
                { context.OpenResearchId = row.ResearchId; break; }
            return true;
        }

        // The construction owner supplies this only from its persisted, settled
        // paid rebuild receipt. This value is not stored as a second research state.
        public readonly struct ResearchRecoveryEvidence
        {
            public readonly FixedString64Bytes Attempt, PreviousProducerStableId, RebuiltProducerStableId;
            public readonly uint FailedResearchId;
            public ResearchRecoveryEvidence(FixedString64Bytes attempt,uint failedResearchId,
                FixedString64Bytes previousProducerStableId,FixedString64Bytes rebuiltProducerStableId)
            { Attempt=attempt;FailedResearchId=failedResearchId;PreviousProducerStableId=previousProducerStableId;RebuiltProducerStableId=rebuiltProducerStableId; }
        }
        public static Decision Tick(EntityManager em, Entity session, byte actingFaction,
            SkirmishRoleKind combatRole, bool immediateDefense, SkirmishRoleKind readinessGoal, uint planRevision) =>
            Tick(em,session,actingFaction,combatRole,immediateDefense,readinessGoal,planRevision,default);

        public static Decision Tick(EntityManager em, Entity session, byte actingFaction,
            SkirmishRoleKind combatRole, bool immediateDefense, SkirmishRoleKind readinessGoal, uint planRevision,
            in ResearchRecoveryEvidence recovery)
        {
            var result = new Decision();
            if (!TryReadContext(em,session,actingFaction,out var context)) return result;
            // An accepted research purchase owns the sole pending economic action.
            // Do not admit urgent replacement or retry an unpaid unit intent until
            // native research reaches a terminal state. Settlement remains native.
            if (context.OpenResearchId != 0)
            { result.Action = SkirmishEnemyEconomyAction.ResearchPending; result.ResearchId = context.OpenResearchId; return result; }
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var army = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            SkirmishEnemyPaidProductionAdapter.Tick(em,session,army,context.Elapsed);
            var intent = em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
            if (SkirmishEnemyPaidProductionAdapter.IsOutstanding(intent))
            { result.Action = SkirmishEnemyEconomyAction.PaidIntentPending; return result; }
            bool combatFeasible = combatRole != SkirmishRoleKind.None &&
                SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,combatRole);
            // Emergency compatible defense can consume the reserve. Research never
            // wins over an actually feasible immediate defender/replacement.
            if (immediateDefense && combatFeasible)
            {
                result.Action = SkirmishEnemyEconomyAction.PreserveDefense;
                result.PermitCombatPurchase = combatFeasible;
                return result;
            }
            int combatReserve = combatFeasible ? SkirmishProductionService.EvaluateQueue(em,session,
                SkirmishRoleIds.ToId(combatRole),1,army,actingFaction).MaterialsCost : 0;
            result.ReserveMaterials = combatReserve + context.LogisticsReplacementCost;
            result.PermitCombatPurchase = combatFeasible && CanSpend(context.Materials,
                combatReserve,context.LogisticsReplacementCost);
            if (context.LogisticsMembers == 0 &&
                SkirmishEnemyCommanderService.MayStartProduction(intent,SkirmishRoleKind.LogisticsTruck) &&
                SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,SkirmishRoleKind.LogisticsTruck) &&
                CanSpend(context.Materials,context.LogisticsReplacementCost,combatReserve))
            {
                SkirmishEnemyPaidProductionAdapter.TryRequest(em,session,
                    new FixedString64Bytes("enemy.purchase." + intent.NextRequestId),SkirmishRoleKind.LogisticsTruck,
                    planRevision,army,context.Elapsed);
                if (SkirmishEnemyPaidProductionAdapter.IsOutstanding(
                    em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session)))
                { result.Action = SkirmishEnemyEconomyAction.LogisticsRequested; result.PermitCombatPurchase = false; }
                return result;
            }
            if (immediateDefense || readinessGoal == SkirmishRoleKind.None || army == null ||
                !army.Allows(SkirmishRoleIds.ToId(readinessGoal)) ||
                !SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,readinessGoal,out var goal) ||
                !SkirmishProductionService.HasLivingProducer(em,session,goal.Producer,actingFaction)) return result;
            if (context.Readiness >= SkirmishProductionEligibility.RequiredReadiness(readinessGoal))
            {
                if (SkirmishEnemyPaidProductionAdapter.CanDispatchNow(em,session,readinessGoal) &&
                    CanSpend(context.Materials,goal.MaterialsCost,context.LogisticsReplacementCost))
                { result.SuggestedCombatRole = readinessGoal; result.PermitCombatPurchase = true; }
                return result;
            }
            // Only the latest same-faction readiness operation matters. A later
            // real completion supersedes historical loss; an unresolved terminal
            // operation needs the construction owner's exact rebuild evidence.
            if (TryReadFailedReadiness(em,session,actingFaction,out uint failedResearchId) &&
                !ValidRecovery(em,session,actingFaction,failedResearchId,in recovery)) return result;
            var goalQuote = SkirmishProductionService.EvaluateQueueAtReadiness(em,session,
                SkirmishRoleIds.ToId(readinessGoal),1,army,actingFaction,
                SkirmishProductionEligibility.RequiredReadiness(readinessGoal));
            if (!em.HasComponent<SkirmishProductionCatalogRecord>(session)) return result;
            var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            if (catalog == null || !catalog.TryReadProductionDeliverySeconds(readinessGoal,out float productionDeliverySeconds)) return result;
            if (!goalQuote.Accepted ||
                SkirmishProductionService.IsProducerLocked(em,session,goal.Producer,actingFaction) ||
                SkirmishProductionService.IsProducerLocked(em,session,SkirmishProducerKind.Barracks,actingFaction) ||
                !SkirmishResearchService.CanQueue(em,session,SkirmishResearchKind.Readiness,actingFaction,
                    out var quote,out _) || context.Remaining <= ReadinessResearchSeconds(context.Readiness,
                    SkirmishProductionEligibility.RequiredReadiness(readinessGoal)) + productionDeliverySeconds ||
                !CanSpend(context.Materials,quote.MaterialsCost,
                    combatReserve + context.LogisticsReplacementCost)) return result;
            if (SkirmishResearchService.TryQueue(em,session,SkirmishResearchKind.Readiness,actingFaction,out var accepted))
            { result.Action = SkirmishEnemyEconomyAction.ReadinessQueued; result.ResearchId = accepted.ResearchId; result.PermitCombatPurchase = false; }
            return result;
        }

        // Complete native research path to the requested role, rather than only
        // the next queue step. Still queues/pays just one native step at a time.
        public static float ReadinessResearchSeconds(SkirmishReadinessStage current, SkirmishReadinessStage required)
        {
            float seconds = 0;
            if (current < SkirmishReadinessStage.Established && required >= SkirmishReadinessStage.Established)
                seconds += SkirmishResearchCosts.ReadinessEstablishedSeconds;
            if (current < SkirmishReadinessStage.FullArsenal && required >= SkirmishReadinessStage.FullArsenal)
                seconds += SkirmishResearchCosts.ReadinessFullArsenalSeconds;
            return seconds;
        }

        public static bool TryReadFailedReadiness(EntityManager em,Entity session,byte faction,out uint researchId)
        {
            researchId=0;if (!em.Exists(session)||!em.HasBuffer<SkirmishResearchQueueItem>(session)) return false;
            var latest=default(SkirmishResearchQueueItem);
            foreach(var row in em.GetBuffer<SkirmishResearchQueueItem>(session))
                if(row.FactionId==faction&&row.Kind==SkirmishResearchKind.Readiness&&row.ResearchId>latest.ResearchId) latest=row;
            if(latest.Phase!=SkirmishResearchPhase.Lost&&latest.Phase!=SkirmishResearchPhase.Cancelled)return false;
            researchId=latest.ResearchId;return researchId!=0;
        }
        static bool ValidRecovery(EntityManager em,Entity session,byte faction,uint failedResearchId,in ResearchRecoveryEvidence evidence)
        {
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if(evidence.FailedResearchId!=failedResearchId||!evidence.Attempt.Equals(attempt)||
                evidence.PreviousProducerStableId.IsEmpty||evidence.RebuiltProducerStableId.IsEmpty||
                evidence.PreviousProducerStableId.Equals(evidence.RebuiltProducerStableId))return false;
            int rebuilt=0;
            using var query=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth));
            using var buildings=query.ToEntityArray(Allocator.Temp);
            foreach(var building in buildings)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
                if(!owner.SessionId.Equals(attempt)||owner.FactionId!=faction||owner.IsStructure==0||
                    em.GetComponentData<SkirmishStructureIdentityComponent>(building).Producer!=SkirmishProducerKind.Barracks||
                    !SkirmishArmyGroupSystem.IsAlive(em,building))continue;
                if(owner.StableObjectId.Equals(evidence.PreviousProducerStableId))return false;
                if(owner.StableObjectId.Equals(evidence.RebuiltProducerStableId))rebuilt++;
            }
            return rebuilt==1;
        }

        // Overflow-safe planner ceiling; native owners still perform all debits.
        public static bool CanSpend(int materials, int cost, int reserve) =>
            materials >= 0 && cost >= 0 && reserve >= 0 && (long)cost + reserve <= materials;

        public static int CountOwnedOrPending(EntityManager em, Entity session, byte faction, SkirmishRoleKind role)
        {
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return 0;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            int count = 0;
            using var query = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<SkirmishUnitRoleComponent>(), ComponentType.ReadOnly<UnitHealth>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owned.SessionId.Equals(attempt) && owned.FactionId == faction && owned.IsStructure == 0 &&
                    owned.CapacityReleased == 0 && SkirmishArmyGroupSystem.IsAlive(em,actor) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == role) count++;
            }
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                    if (receipt.FactionId == faction && receipt.Role == role &&
                        (receipt.Phase == SkirmishReservationPhase.Reserved || receipt.Phase == SkirmishReservationPhase.Producing))
                        count += System.Math.Max(0,receipt.MemberCount - receipt.DeliveredMembers);
            return count;
        }
    }
}
