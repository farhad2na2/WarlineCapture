using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal struct UnitPathfindingApplySystem
{
    private MapSurfacePathfindingReadSystem _surfaceReadSystem;

    public void Apply(
        ref SystemState state,
        ref UnitPathfindingQuerySystem queries,
        ref UnitPathRequestBufferSystem requestBuffers,
        ref UnitPathResultApplySystem resultApply,
        ref UnitPathValidationMetricsSystem validationMetrics,
        ref UnitPathfindingBudgetSystem budget,
        ref UnitPathfindingDiagnosticSystem diagnostics,
        ref UnitPathLiveUnitSnapshotSystem liveUnitSnapshot,
        ref JobHandle pendingPathHandle,
        ref NativeStream pendingPathStream,
        ref bool hasPendingPathJob,
        ref int pendingRequestCount,
        ref int pendingRequestBudget,
        ref int pendingLiveUnitCount,
        ref int pendingGridWidth,
        ref int pendingGridHeight,
        ref int pendingScheduleFrame,
        ref double pendingScheduleTime,
        bool enablePathDiagnostics,
        double freezeLogThresholdSeconds)
    {
        double applyStart = Time.realtimeSinceStartupAsDouble;
        pendingPathHandle.Complete();
        double afterComplete = Time.realtimeSinceStartupAsDouble;
        double pendingWallTime = afterComplete - pendingScheduleTime;
        int pendingFrames = math.max(1, Time.frameCount - pendingScheduleFrame);
        NativeArray<byte> manualMoves = requestBuffers.ManualMoves.AsArray();
        int scheduledManualCountForBudget = 0;
        int scheduledVehicleLikeCountForBudget = 0;
        for (int i = 0; i < pendingRequestCount; i++)
        {
            if (manualMoves[i] != 0)
                scheduledManualCountForBudget++;
            if (UnitVehicleMovementUtility.IsVehicle(requestBuffers.Footprints[i], requestBuffers.MovementBehaviors[i]))
                scheduledVehicleLikeCountForBudget++;
        }

        budget.ReportCompletedJob(
            pendingFrames,
            pendingWallTime,
            pendingRequestCount,
            scheduledManualCountForBudget,
            scheduledVehicleLikeCountForBudget,
            pendingRequestBudget);

        Entity gridEntity = queries.GridQuery.GetSingletonEntity();
        PathPoolData pool = state.EntityManager.GetComponentData<PathPoolData>(gridEntity);
        MapSurfacePathfindingReadSystem.Context surfaceContext = _surfaceReadSystem.TryCreateContext(state.EntityManager, queries.MapSurfaceQuery, out MapSurfacePathfindingReadSystem.Context resolvedSurfaceContext)
            ? resolvedSurfaceContext
            : _surfaceReadSystem.CreateFlatFallbackContext();
        NativeArray<Entity> requestEntities = requestBuffers.Entities.AsArray();
        NativeArray<UnitPathRequest> requestGoals = requestBuffers.Goals.AsArray();
        NativeArray<int2> assignedGoals = requestBuffers.AssignedGoals.AsArray();
        NativeArray<byte> status = requestBuffers.Status.AsArray();
        NativeArray<byte> segmented = requestBuffers.Segmented.AsArray();
        NativeArray<byte> requestContinuationMoves = requestBuffers.ContinuationMoves.AsArray();
        NativeArray<byte> cheapSegmentModes = requestBuffers.CheapSegmentModes.AsArray();
        NativeArray<byte> alternateSearchSkipped = requestBuffers.AlternateSearchSkipped.AsArray();
        NativeArray<int> alternateAttempts = requestBuffers.AlternateAttempts.AsArray();

        resultApply.Apply(
            ref state,
            gridEntity,
            ref pool,
            requestEntities,
            requestGoals,
            assignedGoals,
            segmented,
            manualMoves,
            surfaceContext,
            pendingPathStream,
            status,
            out int completedCount,
            out int completedSegmentCount,
            out int manualCompletedCount,
            out int retriedCount,
            out int retriedSegmentCount,
            out int manualRetriedCount,
            out int abandonedCount);
        state.EntityManager.SetComponentData(gridEntity, pool);

        int queuedCount = queries.RequestQuery.CalculateEntityCount();
        int followingCount = queries.PathFollowQuery.CalculateEntityCount();
        int manualPendingCount = queries.PendingManualMoveQuery.CalculateEntityCount();
        int manualQueuedCount = queries.ManualRequestQuery.CalculateEntityCount();
        int manualFollowingCount = queries.ManualPathFollowQuery.CalculateEntityCount();
        int longDistanceCount = queries.LongDistanceMoveQuery.CalculateEntityCount();
        int retryCooldownCount = queries.RetryCooldownQuery.CalculateEntityCount();
        int segmentedCount = 0;
        int scheduledManualCount = 0;
        int scheduledVehicleLikeCount = 0;
        int scheduledSegmentedCount = 0;
        int scheduledContinuationCount = 0;
        int cheapSegmentCount = 0;
        int alternateReducedCount = 0;
        int alternateAttemptTotal = 0;
        for (int i = 0; i < pendingRequestCount; i++)
        {
            if (UnitVehicleMovementUtility.IsVehicle(requestBuffers.Footprints[i], requestBuffers.MovementBehaviors[i]))
                scheduledVehicleLikeCount++;
            if (segmented[i] != 0)
            {
                segmentedCount++;
                scheduledSegmentedCount++;
            }
            if (manualMoves[i] != 0)
                scheduledManualCount++;
            if (requestContinuationMoves[i] != 0)
                scheduledContinuationCount++;
            if (cheapSegmentModes[i] != 0)
                cheapSegmentCount++;
            if (alternateSearchSkipped[i] != 0)
                alternateReducedCount++;
            alternateAttemptTotal += alternateAttempts[i];
        }

        double afterApply = Time.realtimeSinceStartupAsDouble;
        double applyElapsed = afterApply - applyStart;
        bool manualValidationActive = UnitPathValidationMetricsSystem.IsManualValidationActive(
            enablePathDiagnostics,
            manualPendingCount,
            manualQueuedCount,
            manualFollowingCount,
            longDistanceCount,
            retryCooldownCount);
        var validationInputs = new UnitPathValidationMetricsSystem.FrameInputs
        {
            ManualQueuedCount = manualQueuedCount,
            ManualFollowingCount = manualFollowingCount,
            LongDistanceCount = longDistanceCount,
            RetryCooldownCount = retryCooldownCount,
            ScheduledBudget = pendingRequestBudget,
            NextBudget = budget.AdaptiveRequestsPerFrame,
            PendingFrames = pendingFrames,
            PendingWallMs = pendingWallTime * 1000d,
            ScheduledManualCount = scheduledManualCount,
            ScheduledVehicleLikeCount = scheduledVehicleLikeCount,
            ScheduledSegmentedCount = scheduledSegmentedCount,
            ScheduledContinuationCount = scheduledContinuationCount,
            CheapSegmentCount = cheapSegmentCount,
            AlternateReducedCount = alternateReducedCount,
            AlternateAttemptTotal = alternateAttemptTotal,
        };
        var validationResults = new UnitPathValidationMetricsSystem.FrameResults
        {
            CompletedCount = completedCount,
            CompletedSegmentCount = completedSegmentCount,
            ManualCompletedCount = manualCompletedCount,
            RetriedCount = retriedCount,
            RetriedSegmentCount = retriedSegmentCount,
            ManualRetriedCount = manualRetriedCount,
            AbandonedCount = abandonedCount,
        };

        if (manualValidationActive && validationMetrics.BeginIfNeeded(Time.frameCount, validationInputs))
            diagnostics.LogValidationStart(state.EntityManager, Time.frameCount, manualPendingCount, manualQueuedCount, manualFollowingCount, retryCooldownCount, longDistanceCount, pendingRequestBudget, budget.AdaptiveRequestsPerFrame);

        if (manualValidationActive)
        {
            if (validationMetrics.RecordActiveFrameAndShouldLogStuck(Time.frameCount, validationInputs, validationResults))
                LogValidationStuck(
                    ref state,
                    ref queries,
                    ref validationMetrics,
                    ref diagnostics,
                    hasPendingPathJob,
                    pendingRequestCount,
                    pendingRequestBudget,
                    budget.AdaptiveRequestsPerFrame,
                    manualPendingCount,
                    manualQueuedCount,
                    manualFollowingCount,
                    longDistanceCount,
                    retryCooldownCount,
                    queuedCount,
                    followingCount,
                    completedCount,
                    manualCompletedCount,
                    retriedCount,
                    manualRetriedCount,
                    abandonedCount);
        }
        else if (validationMetrics.TryEnd(manualValidationActive, Time.frameCount, out UnitPathValidationMetricsSystem.EndSnapshot validationEnd))
        {
            diagnostics.LogValidationEnd(
                state.EntityManager,
                validationEnd.StartFrame,
                validationEnd.EndFrame,
                validationEnd.PeakManualQueued,
                validationEnd.PeakManualFollowing,
                validationEnd.PeakLongMove,
                validationEnd.PeakCooldown,
                validationEnd.PeakScheduledBudget,
                validationEnd.PeakNextBudget,
                validationEnd.PeakPendingFrames,
                validationEnd.PeakPendingWallMs,
                validationEnd.PeakScheduledManual,
                validationEnd.PeakScheduledVehicleLike,
                validationEnd.PeakScheduledSegmented,
                validationEnd.PeakScheduledContinuations,
                validationEnd.PeakCheapSegments,
                validationEnd.PeakAltReduced,
                validationEnd.PeakAltAttempts,
                validationEnd.CompletedTotal,
                validationEnd.CompletedSegmentTotal,
                validationEnd.ManualCompletedTotal,
                validationEnd.RetriedTotal,
                validationEnd.RetriedSegmentTotal,
                validationEnd.ManualRetriedTotal,
                validationEnd.AbandonedTotal);
        }

        if (enablePathDiagnostics && (applyElapsed >= freezeLogThresholdSeconds || pendingWallTime >= freezeLogThresholdSeconds))
        {
            diagnostics.LogAsync(
                state.EntityManager,
                Time.frameCount,
                applyElapsed,
                pendingWallTime,
                afterComplete - applyStart,
                afterApply - afterComplete,
                pendingRequestCount,
                pendingRequestBudget,
                budget.AdaptiveRequestsPerFrame,
                completedCount,
                retriedCount + abandonedCount,
                segmentedCount,
                pendingLiveUnitCount,
                pendingGridWidth,
                pendingGridHeight);
        }

        DisposeCompleted(
            ref liveUnitSnapshot,
            ref pendingPathStream,
            ref hasPendingPathJob,
            ref pendingRequestCount,
            ref pendingRequestBudget,
            ref pendingLiveUnitCount,
            ref pendingGridWidth,
            ref pendingGridHeight,
            ref pendingScheduleFrame,
            ref pendingScheduleTime,
            ref budget);
    }

    public void DisposePending(
        ref SystemState state,
        ref UnitPathLiveUnitSnapshotSystem liveUnitSnapshot,
        ref UnitPathfindingBudgetSystem budget,
        ref JobHandle pendingPathHandle,
        ref NativeStream pendingPathStream,
        ref bool hasPendingPathJob,
        ref int pendingRequestCount,
        ref int pendingRequestBudget,
        ref int pendingLiveUnitCount,
        ref int pendingGridWidth,
        ref int pendingGridHeight,
        ref int pendingScheduleFrame,
        ref double pendingScheduleTime)
    {
        if (!hasPendingPathJob)
            return;

        pendingPathHandle.Complete();
        state.Dependency = default;
        DisposeCompleted(
            ref liveUnitSnapshot,
            ref pendingPathStream,
            ref hasPendingPathJob,
            ref pendingRequestCount,
            ref pendingRequestBudget,
            ref pendingLiveUnitCount,
            ref pendingGridWidth,
            ref pendingGridHeight,
            ref pendingScheduleFrame,
            ref pendingScheduleTime,
            ref budget);
    }

    private void LogValidationStuck(
        ref SystemState state,
        ref UnitPathfindingQuerySystem queries,
        ref UnitPathValidationMetricsSystem validationMetrics,
        ref UnitPathfindingDiagnosticSystem diagnostics,
        bool hasPendingPathJob,
        int pendingRequestCount,
        int pendingRequestBudget,
        int adaptiveRequestBudget,
        int manualPendingCount,
        int manualQueuedCount,
        int manualFollowingCount,
        int longDistanceCount,
        int retryCooldownCount,
        int queuedCount,
        int followingCount,
        int completedCount,
        int manualCompletedCount,
        int retriedCount,
        int manualRetriedCount,
        int abandonedCount)
    {
        string samples = BuildManualMoveSamples(ref state, ref queries, ref diagnostics, UnitPathValidationMetricsSystem.StuckSampleCount);
        diagnostics.LogValidationStuck(
            state.EntityManager,
            Time.frameCount,
            validationMetrics.StartFrame,
            manualPendingCount,
            manualQueuedCount,
            manualFollowingCount,
            longDistanceCount,
            retryCooldownCount,
            queuedCount,
            followingCount,
            hasPendingPathJob,
            pendingRequestCount,
            pendingRequestBudget,
            adaptiveRequestBudget,
            completedCount,
            manualCompletedCount,
            retriedCount,
            manualRetriedCount,
            abandonedCount,
            validationMetrics.ManualCompletedTotal,
            validationMetrics.ManualRetriedTotal,
            validationMetrics.AbandonedTotal,
            samples);
    }

    private static string BuildManualMoveSamples(
        ref SystemState state,
        ref UnitPathfindingQuerySystem queries,
        ref UnitPathfindingDiagnosticSystem diagnostics,
        int maxSamples)
    {
        EntityManager em = state.EntityManager;
        using NativeArray<Entity> entities = queries.PendingManualMoveQuery.ToEntityArray(Allocator.Temp);
        return diagnostics.BuildManualMoveSamples(em, entities, maxSamples);
    }

    private static void DisposeCompleted(
        ref UnitPathLiveUnitSnapshotSystem liveUnitSnapshot,
        ref NativeStream pendingPathStream,
        ref bool hasPendingPathJob,
        ref int pendingRequestCount,
        ref int pendingRequestBudget,
        ref int pendingLiveUnitCount,
        ref int pendingGridWidth,
        ref int pendingGridHeight,
        ref int pendingScheduleFrame,
        ref double pendingScheduleTime,
        ref UnitPathfindingBudgetSystem budget)
    {
        if (pendingPathStream.IsCreated)
            pendingPathStream.Dispose();
        liveUnitSnapshot.Dispose();
        hasPendingPathJob = false;
        pendingRequestCount = 0;
        pendingRequestBudget = 0;
        pendingLiveUnitCount = 0;
        pendingGridWidth = 0;
        pendingGridHeight = 0;
        pendingScheduleFrame = 0;
        pendingScheduleTime = 0d;
        budget.ResetPendingJobReduction();
    }
}
