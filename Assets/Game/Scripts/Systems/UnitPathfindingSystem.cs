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
    private UnitPathfindingEntitySets _queries;
    private UnitPathScratchWorkspace _scratchWorkspace;
    private UnitPathGridSnapshot _gridSnapshot;
    private UnitPathReservedGoal _reservedGoals;
    private UnitPathCoarseWorkspace _coarseWorkspace;
    private UnitHierarchicalPathPlanner _hierarchicalPath;
    private UnitPathGoalAssignment _goalAssignment;
    private int _lastHierarchicalPathValidationFrame;
    private UnitPathRequestBuffer _requestBuffers;
    private UnitPathIgnoredOccupancy _ignoredOccupancy;
    private UnitPathRequestCollection _requestCollection;
    private UnitPathSegmentation _segmentation;
    private UnitPathResultApply _resultApply;
    private UnitPathValidationMetrics _validationMetrics;
    private UnitPathfindingScheduler _schedule;
    private UnitPathfindingApply _apply;
    private UnitPathfindingBudget _budget;
    private JobHandle _pendingPathHandle;
    private NativeStream _pendingPathStream;
    private bool _hasPendingPathJob;
    private UnitPathLiveUnitSnapshot _liveUnitSnapshot;
    private int _pendingRequestCount;
    private int _pendingRequestBudget;
    private int _pendingLiveUnitCount;
    private int _pendingGridWidth;
    private int _pendingGridHeight;
    private int _pendingScheduleFrame;
    private double _pendingScheduleTime;
    private UnitPathfindingDiagnostics _diagnostics;
    private UnitPathfindingPendingStateStore _pendingState;

    public void OnCreate(ref SystemState state)
    {
        _scratchWorkspace.Initialize();
        _budget.Initialize();
        _validationMetrics.Initialize();
        _diagnostics.Initialize(ref state);
        _pendingStateQuery = _pendingState.CreateQuery(ref state);
        _pendingState.EnsureSingleton(ref state, _pendingStateQuery);
        _queries.Initialize(ref state);
        _apply.Initialize(ref state);
        _liveUnitSnapshot.Initialize(ref state);

        _requestBuffers.Initialize();
        _requestCollection.Initialize(ref state);
    }

    public void OnDestroy(ref SystemState state)
    {
        DisposePendingPathJob(ref state);
        _scratchWorkspace.Dispose();
        _gridSnapshot.Dispose();
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
                // Intentionally NOT chained into state.Dependency: the job only reads
                // system-owned snapshots (see UnitPathGridSnapshot), so nothing on
                // the main thread should ever be forced to wait for it.
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
        UnitPathfindingScheduler.Result scheduleResult = _schedule.Schedule(
            ref state,
            ref _queries,
            ref _scratchWorkspace,
            ref _gridSnapshot,
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
        _pendingState.EnsureSingleton(ref state, _pendingStateQuery);
        RefRW<UnitPathfindingPendingStateComponent> pendingState =
            SystemAPI.GetSingletonRW<UnitPathfindingPendingStateComponent>();
        pendingState.ValueRW = UnitPathfindingPendingStateStore.CreateState(
            _hasPendingPathJob,
            _pendingRequestCount,
            _pendingRequestBudget,
            _pendingScheduleFrame);
    }


}
