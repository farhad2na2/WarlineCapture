using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial struct UnitPathfindingSystem : ISystem
{
    private static readonly bool EnablePathDiagnostics = false;
    private static readonly bool EnablePathFreezeLogs = false;
    private static readonly bool EnableHierarchicalPathValidationLog = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private EntityQuery _pendingStateQuery;
    private UnitPathfindingQuerySystem _queries;
    private UnitPathScratchWorkspaceSystem _scratchWorkspace;
    private UnitPathReservedGoalSystem _reservedGoals;
    private UnitPathCoarseWorkspaceSystem _coarseWorkspace;
    private UnitHierarchicalPathSystem _hierarchicalPath;
    private UnitPathGoalAssignmentSystem _goalAssignment;
    private int _lastHierarchicalPathValidationFrame;
    private UnitPathRequestBufferSystem _requestBuffers;
    private UnitPathIgnoredOccupancySystem _ignoredOccupancy;
    private UnitPathRequestCollectionSystem _requestCollection;
    private UnitPathSegmentationSystem _segmentation;
    private UnitPathResultApplySystem _resultApply;
    private UnitPathValidationMetricsSystem _validationMetrics;
    private UnitPathfindingScheduleSystem _schedule;
    private UnitPathfindingApplySystem _apply;
    private UnitPathfindingBudgetSystem _budget;
    private JobHandle _pendingPathHandle;
    private NativeStream _pendingPathStream;
    private bool _hasPendingPathJob;
    private UnitPathLiveUnitSnapshotSystem _liveUnitSnapshot;
    private int _pendingRequestCount;
    private int _pendingRequestBudget;
    private int _pendingLiveUnitCount;
    private int _pendingGridWidth;
    private int _pendingGridHeight;
    private int _pendingScheduleFrame;
    private double _pendingScheduleTime;
    private UnitPathfindingDiagnosticSystem _diagnostics;
    private UnitPathfindingPendingStateSystem _pendingState;

    public void OnCreate(ref SystemState state)
    {
        _scratchWorkspace.Initialize();
        _budget.Initialize();
        _validationMetrics.Initialize();
        _diagnostics.Initialize(ref state);
        _pendingStateQuery = _pendingState.CreateQuery(ref state);
        _pendingState.EnsureSingleton(ref state, _pendingStateQuery);
        _queries.Initialize(ref state);

        _requestBuffers.Initialize();
        _requestCollection.Initialize(ref state);
    }

    public void OnDestroy(ref SystemState state)
    {
        DisposePendingPathJob(ref state);
        _scratchWorkspace.Dispose();
        _reservedGoals.Dispose();
        _coarseWorkspace.Dispose();
        _requestBuffers.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
        {
            if (_hasPendingPathJob)
                DisposePendingPathJob(ref state);
            PublishPendingState(ref state);
            return;
        }

        if (_hasPendingPathJob)
        {
            if (!_pendingPathHandle.IsCompleted)
            {
                _budget.ReduceForPendingJob(Time.frameCount, _pendingScheduleFrame, _pendingRequestBudget);
                state.Dependency = _pendingPathHandle;
                PublishPendingState(ref state);
                return;
            }

            _apply.Apply(
                ref state,
                ref _queries,
                ref _requestBuffers,
                ref _resultApply,
                ref _validationMetrics,
                ref _budget,
                ref _diagnostics,
                ref _liveUnitSnapshot,
                ref _pendingPathHandle,
                ref _pendingPathStream,
                ref _hasPendingPathJob,
                ref _pendingRequestCount,
                ref _pendingRequestBudget,
                ref _pendingLiveUnitCount,
                ref _pendingGridWidth,
                ref _pendingGridHeight,
                ref _pendingScheduleFrame,
                ref _pendingScheduleTime,
                EnablePathDiagnostics,
                FreezeLogThresholdSeconds);
            PublishPendingState(ref state);
        }

        int requestBudgetForLog = _budget.GetCurrentRequestBudget();
        UnitPathfindingScheduleSystem.Result scheduleResult = _schedule.Schedule(
            ref state,
            ref _queries,
            ref _scratchWorkspace,
            ref _liveUnitSnapshot,
            ref _requestBuffers,
            ref _ignoredOccupancy,
            ref _requestCollection,
            ref _reservedGoals,
            ref _segmentation,
            ref _coarseWorkspace,
            ref _hierarchicalPath,
            ref _goalAssignment,
            ref _diagnostics,
            ref _lastHierarchicalPathValidationFrame,
            requestBudgetForLog,
            _budget.AdaptiveRequestsPerFrame,
            EnablePathFreezeLogs,
            EnableHierarchicalPathValidationLog,
            FreezeLogThresholdSeconds);
        if (!scheduleResult.Scheduled)
            return;

        _pendingPathHandle = scheduleResult.PendingPathHandle;
        _pendingPathStream = scheduleResult.PendingPathStream;
        _hasPendingPathJob = true;
        _pendingRequestCount = scheduleResult.RequestCount;
        _pendingRequestBudget = scheduleResult.RequestBudget;
        _pendingLiveUnitCount = scheduleResult.LiveUnitCount;
        _pendingGridWidth = scheduleResult.GridWidth;
        _pendingGridHeight = scheduleResult.GridHeight;
        _pendingScheduleFrame = scheduleResult.ScheduleFrame;
        _pendingScheduleTime = scheduleResult.ScheduleTime;
        _budget.ResetPendingJobReduction();
        PublishPendingState(ref state);
    }

    private void DisposePendingPathJob(ref SystemState state)
    {
        _apply.DisposePending(
            ref state,
            ref _liveUnitSnapshot,
            ref _budget,
            ref _pendingPathHandle,
            ref _pendingPathStream,
            ref _hasPendingPathJob,
            ref _pendingRequestCount,
            ref _pendingRequestBudget,
            ref _pendingLiveUnitCount,
            ref _pendingGridWidth,
            ref _pendingGridHeight,
            ref _pendingScheduleFrame,
            ref _pendingScheduleTime);
        PublishPendingState(ref state);
    }

    private void PublishPendingState(ref SystemState state)
    {
        _pendingState.Publish(
            ref state,
            _pendingStateQuery,
            _hasPendingPathJob,
            _pendingRequestCount,
            _pendingRequestBudget,
            _pendingScheduleFrame);
    }


}
