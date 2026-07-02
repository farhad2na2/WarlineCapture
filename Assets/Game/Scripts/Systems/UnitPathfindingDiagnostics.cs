using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal struct UnitPathfindingDiagnostics
    {
        private Entity _logQueueEntity;

        public void Initialize(ref SystemState state)
        {
            _logQueueEntity = GetOrCreateLogQueue(state.EntityManager);
        }

        public void LogHierarchicalValidation(
            EntityManager em,
            int frameCount,
            int requestCount,
            int manualRequestCount,
            int hierarchicalEligibleCount,
            int hierarchicalWaypointCount,
            int hierarchicalFallbackCount,
            int hierarchicalSectorSizeCells,
            float manualInfantryLongDistanceSegmentCells,
            float manualVehicleLongDistanceSegmentCells,
            int hierarchicalMaxExpandedSectors)
        {
            Enqueue(em, $"[HierPathValidate] frame={frameCount} requests={requestCount} manual={manualRequestCount} eligible={hierarchicalEligibleCount} hierarchical={hierarchicalWaypointCount} fallback={hierarchicalFallbackCount} sector={hierarchicalSectorSizeCells} infantryThreshold={manualInfantryLongDistanceSegmentCells} vehicleThreshold={manualVehicleLongDistanceSegmentCells} maxExpanded={hierarchicalMaxExpandedSectors}");
        }

        public void LogFrameFreeze(
            EntityManager em,
            int frameCount,
            double elapsed,
            double startTime,
            double afterGridTime,
            double afterScratchTime,
            double afterSnapshotTime,
            double afterRequestCollectTime,
            double afterGoalAssignTime,
            double afterScheduleTime,
            double afterCompleteTime,
            double afterApplyTime,
            bool scratchWasAllocated,
            int scratchCellsForLog,
            int scratchThreadSlotsForLog,
            int requestCountForLog,
            int requestBudgetForLog,
            int nextRequestBudget,
            int successCountForLog,
            int failedCountForLog,
            int segmentedCountForLog,
            int liveUnitCountForLog,
            int gridWidthForLog,
            int gridHeightForLog)
        {
            Enqueue(
                em,
                $"[FreezeDetect:ECS] UnitPathfindingSystem frame={frameCount} {(elapsed * 1000d):F1}ms " +
                $"requests={requestCountForLog} liveUnits={liveUnitCountForLog} grid={gridWidthForLog}x{gridHeightForLog}");
            Enqueue(
                em,
                $"[PathDiag] frame={frameCount} total={(elapsed * 1000d):F1}ms " +
                $"grid={(afterGridTime - startTime) * 1000d:F1}ms " +
                $"scratch={(afterScratchTime - afterGridTime) * 1000d:F1}ms allocated={(scratchWasAllocated ? 1 : 0)} scratchCells={scratchCellsForLog} scratchThreads={scratchThreadSlotsForLog} " +
                $"snapshot={(afterSnapshotTime - afterScratchTime) * 1000d:F1}ms " +
                $"collect={(afterRequestCollectTime - afterSnapshotTime) * 1000d:F1}ms " +
                $"goal={(afterGoalAssignTime - afterRequestCollectTime) * 1000d:F1}ms " +
                $"schedule={(afterScheduleTime - afterGoalAssignTime) * 1000d:F1}ms " +
                $"wait={(afterCompleteTime - afterScheduleTime) * 1000d:F1}ms " +
                $"apply={(afterApplyTime - afterCompleteTime) * 1000d:F1}ms " +
                $"requests={requestCountForLog} budget={requestBudgetForLog} nextBudget={nextRequestBudget} success={successCountForLog} failed={failedCountForLog} segmented={segmentedCountForLog} liveUnits={liveUnitCountForLog}");
        }

        public void LogValidationStart(
            EntityManager em,
            int frameCount,
            int manualPendingCount,
            int manualQueuedCount,
            int manualFollowingCount,
            int retryCooldownCount,
            int longDistanceCount,
            int scheduledBudget,
            int nextBudget)
        {
            Enqueue(
                em,
                $"[PathDiagValidate] START frame={frameCount} manualPending={manualPendingCount} manualQueued={manualQueuedCount} manualFollowing={manualFollowingCount} manualIdle={math.max(0, manualPendingCount - manualQueuedCount - manualFollowingCount)} cooldown={retryCooldownCount} longMove={longDistanceCount} scheduledBudget={scheduledBudget} nextBudget={nextBudget}");
        }

        public void LogValidationEnd(
            EntityManager em,
            int validationStartFrame,
            int frameCount,
            int validationPeakManualQueued,
            int validationPeakManualFollowing,
            int validationPeakLongMove,
            int validationPeakCooldown,
            int validationPeakScheduledBudget,
            int validationPeakNextBudget,
            int validationPeakPendingFrames,
            double validationPeakPendingWallMs,
            int validationPeakScheduledManual,
            int validationPeakScheduledVehicleLike,
            int validationPeakScheduledSegmented,
            int validationPeakScheduledContinuations,
            int validationPeakCheapSegments,
            int validationPeakAltReduced,
            int validationPeakAltAttempts,
            int validationCompletedTotal,
            int validationCompletedSegmentTotal,
            int validationManualCompletedTotal,
            int validationRetriedTotal,
            int validationRetriedSegmentTotal,
            int validationManualRetriedTotal,
            int validationAbandonedTotal)
        {
            Enqueue(
                em,
                $"[PathDiagValidate] END startFrame={validationStartFrame} endFrame={frameCount} peakManualQueued={validationPeakManualQueued} peakManualFollowing={validationPeakManualFollowing} peakLongMove={validationPeakLongMove} peakCooldown={validationPeakCooldown} peakScheduledBudget={validationPeakScheduledBudget} peakNextBudget={validationPeakNextBudget} peakPendingFrames={validationPeakPendingFrames} peakPendingWallMs={validationPeakPendingWallMs:F1} peakScheduledManual={validationPeakScheduledManual} peakScheduledVehicleLike={validationPeakScheduledVehicleLike} peakScheduledSegmented={validationPeakScheduledSegmented} peakScheduledContinuations={validationPeakScheduledContinuations} peakCheapSegments={validationPeakCheapSegments} peakAltReduced={validationPeakAltReduced} peakAltAttempts={validationPeakAltAttempts} totalCompleted={validationCompletedTotal} totalCompletedSegmented={validationCompletedSegmentTotal} totalManualCompleted={validationManualCompletedTotal} totalRetried={validationRetriedTotal} totalRetriedSegmented={validationRetriedSegmentTotal} totalManualRetried={validationManualRetriedTotal} totalAbandoned={validationAbandonedTotal}");
        }

        public void LogAsync(
            EntityManager em,
            int frameCount,
            double applyElapsed,
            double pendingWallTime,
            double completeElapsed,
            double applyOnlyElapsed,
            int pendingRequestCount,
            int pendingRequestBudget,
            int nextRequestBudget,
            int completedCount,
            int failedCount,
            int segmentedCount,
            int pendingLiveUnitCount,
            int pendingGridWidth,
            int pendingGridHeight)
        {
            Enqueue(
                em,
                $"[PathDiagAsync] frame={frameCount} applyTotal={(applyElapsed * 1000d):F1}ms " +
                $"pendingWall={(pendingWallTime * 1000d):F1}ms complete={(completeElapsed * 1000d):F1}ms apply={(applyOnlyElapsed * 1000d):F1}ms " +
                $"requests={pendingRequestCount} budget={pendingRequestBudget} nextBudget={nextRequestBudget} " +
                $"success={completedCount} failed={failedCount} segmented={segmentedCount} liveUnits={pendingLiveUnitCount} " +
                $"grid={pendingGridWidth}x{pendingGridHeight}");
        }

        public void LogValidationStuck(
            EntityManager em,
            int frameCount,
            int validationStartFrame,
            int manualPendingCount,
            int manualQueuedCount,
            int manualFollowingCount,
            int longDistanceCount,
            int retryCooldownCount,
            int queuedCount,
            int followingCount,
            bool hasPendingPathJob,
            int pendingRequestCount,
            int pendingRequestBudget,
            int nextRequestBudget,
            int completedCount,
            int manualCompletedCount,
            int retriedCount,
            int manualRetriedCount,
            int abandonedCount,
            int validationManualCompletedTotal,
            int validationManualRetriedTotal,
            int validationAbandonedTotal,
            string samples)
        {
            int manualIdleCount = math.max(0, manualPendingCount - manualQueuedCount - manualFollowingCount);
            Enqueue(
                em,
                $"[PathDiagStuck] frame={frameCount} ageFrames={frameCount - validationStartFrame} " +
                $"manualPending={manualPendingCount} manualQueued={manualQueuedCount} manualFollowing={manualFollowingCount} manualIdle={manualIdleCount} " +
                $"cooldown={retryCooldownCount} longMove={longDistanceCount} queued={queuedCount} following={followingCount} " +
                $"pendingJob={(hasPendingPathJob ? 1 : 0)} pendingRequests={pendingRequestCount} scheduledBudget={pendingRequestBudget} nextBudget={nextRequestBudget} " +
                $"lastCompleted={completedCount} lastManualCompleted={manualCompletedCount} lastRetried={retriedCount} lastManualRetried={manualRetriedCount} lastAbandoned={abandonedCount} " +
                $"totalManualCompleted={validationManualCompletedTotal} totalManualRetried={validationManualRetriedTotal} totalAbandoned={validationAbandonedTotal} " +
                $"samples={samples}");
        }

        public string BuildManualMoveSamples(EntityManager em, NativeArray<Entity> entities, int maxSamples)
        {
            if (entities.Length == 0)
                return "none";

            var builder = new StringBuilder(512);
            int written = 0;
            for (int i = 0; i < entities.Length && written < maxSamples; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                if (written > 0)
                    builder.Append(" | ");

                builder.Append(entity);
                AppendSampleComponentState(ref builder, em, entity);
                written++;
            }

            if (entities.Length > written)
                builder.Append($" | more={entities.Length - written}");

            return written == 0 ? "none" : builder.ToString();
        }

        private static void AppendSampleComponentState(ref StringBuilder builder, EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitGrid>(entity))
                builder.Append($" cell={em.GetComponentData<UnitGrid>(entity).Cell}");
            else
                builder.Append(" cell=none");

            if (em.HasComponent<UnitTarget>(entity))
                builder.Append($" target={em.GetComponentData<UnitTarget>(entity).Cell}");
            else
                builder.Append(" target=none");

            if (em.HasComponent<UnitPathRequest>(entity))
                builder.Append($" req={em.GetComponentData<UnitPathRequest>(entity).Goal}");
            else
                builder.Append(" req=none");

            if (em.HasComponent<UnitPathFollow>(entity) && em.HasComponent<UnitPathRange>(entity))
            {
                UnitPathFollow follow = em.GetComponentData<UnitPathFollow>(entity);
                UnitPathRange range = em.GetComponentData<UnitPathRange>(entity);
                builder.Append($" follow={follow.PathIndex}/{range.Length}");
            }
            else
            {
                builder.Append(" follow=none");
            }

            if (em.HasComponent<UnitLongDistanceMove>(entity))
                builder.Append($" long={em.GetComponentData<UnitLongDistanceMove>(entity).FinalGoal}");
            else
                builder.Append(" long=none");

            if (em.HasComponent<UnitPathRetryCooldown>(entity))
                builder.Append($" cooldownUntil={em.GetComponentData<UnitPathRetryCooldown>(entity).ResumeFrame}");
            else
                builder.Append(" cooldown=none");

            builder.Append($" group={(em.HasComponent<ManualMoveGroupMemberTag>(entity) ? 1 : 0)}");

            if (em.HasComponent<UnitFootprint>(entity) && em.HasComponent<UnitMovementBehavior>(entity))
            {
                UnitFootprint footprint = em.GetComponentData<UnitFootprint>(entity);
                UnitMovementBehavior movementBehavior = em.GetComponentData<UnitMovementBehavior>(entity);
                builder.Append($" footprint={footprint.Size} vehicle={(UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior) ? 1 : 0)}");
            }
        }

        private void Enqueue(EntityManager em, string message)
        {
            if (_logQueueEntity == Entity.Null || !em.Exists(_logQueueEntity))
                _logQueueEntity = GetOrCreateLogQueue(em);

            DynamicBuffer<UnitPathfindingDiagnosticLogComponent> logs =
                em.GetBuffer<UnitPathfindingDiagnosticLogComponent>(_logQueueEntity);
            logs.Add(new UnitPathfindingDiagnosticLogComponent
            {
                Message = CreateFixedMessage(message),
            });
        }

        private static Entity GetOrCreateLogQueue(EntityManager em)
        {
            EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPathfindingDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<UnitPathfindingDiagnosticLogComponent>());
            try
            {
                if (!query.IsEmptyIgnoreFilter)
                    return query.GetSingletonEntity();
            }
            finally
            {
                query.Dispose();
            }

            Entity queueEntity = em.CreateEntity(typeof(UnitPathfindingDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "UnitPathfindingDiagnosticLogQueue");
            em.AddBuffer<UnitPathfindingDiagnosticLogComponent>(queueEntity);
            return queueEntity;
        }

        private static FixedString4096Bytes CreateFixedMessage(string message)
        {
            var fixedMessage = new FixedString4096Bytes();
            fixedMessage.Append(message);
            return fixedMessage;
        }
    }
}
