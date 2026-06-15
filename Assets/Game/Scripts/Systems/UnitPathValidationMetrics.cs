using Unity.Mathematics;

internal struct UnitPathValidationMetrics
{
    public const int StuckLogIntervalFrames = 180;
    public const int StuckLogFirstDelayFrames = 180;
    public const int StuckSampleCount = 6;

    private bool _validationLogActive;
    private int _validationStartFrame;
    private int _validationPeakManualQueued;
    private int _validationPeakManualFollowing;
    private int _validationPeakLongMove;
    private int _validationPeakCooldown;
    private int _validationPeakScheduledBudget;
    private int _validationPeakNextBudget;
    private int _validationPeakPendingFrames;
    private double _validationPeakPendingWallMs;
    private int _validationPeakScheduledManual;
    private int _validationPeakScheduledVehicleLike;
    private int _validationPeakScheduledSegmented;
    private int _validationPeakScheduledContinuations;
    private int _validationPeakCheapSegments;
    private int _validationPeakAltReduced;
    private int _validationPeakAltAttempts;
    private int _validationCompletedTotal;
    private int _validationCompletedSegmentTotal;
    private int _validationManualCompletedTotal;
    private int _validationRetriedTotal;
    private int _validationRetriedSegmentTotal;
    private int _validationManualRetriedTotal;
    private int _validationAbandonedTotal;
    private int _nextValidationStuckLogFrame;

    public int StartFrame => _validationStartFrame;
    public int ManualCompletedTotal => _validationManualCompletedTotal;
    public int ManualRetriedTotal => _validationManualRetriedTotal;
    public int AbandonedTotal => _validationAbandonedTotal;

    public void Initialize()
    {
        _validationLogActive = false;
        _validationStartFrame = 0;
        _nextValidationStuckLogFrame = 0;
    }

    public static bool IsManualValidationActive(
        bool diagnosticsEnabled,
        int manualPendingCount,
        int manualQueuedCount,
        int manualFollowingCount,
        int longDistanceCount,
        int retryCooldownCount)
    {
        return diagnosticsEnabled &&
               (manualPendingCount > 0 ||
                manualQueuedCount > 0 ||
                manualFollowingCount > 0 ||
                longDistanceCount > 0 ||
                retryCooldownCount > 0);
    }

    public bool BeginIfNeeded(int frame, in FrameInputs inputs)
    {
        if (_validationLogActive)
            return false;

        _validationLogActive = true;
        _validationStartFrame = frame;
        _validationPeakManualQueued = inputs.ManualQueuedCount;
        _validationPeakManualFollowing = inputs.ManualFollowingCount;
        _validationPeakLongMove = inputs.LongDistanceCount;
        _validationPeakCooldown = inputs.RetryCooldownCount;
        _validationPeakScheduledBudget = inputs.ScheduledBudget;
        _validationPeakNextBudget = inputs.NextBudget;
        _validationPeakPendingFrames = inputs.PendingFrames;
        _validationPeakPendingWallMs = inputs.PendingWallMs;
        _validationPeakScheduledManual = inputs.ScheduledManualCount;
        _validationPeakScheduledVehicleLike = inputs.ScheduledVehicleLikeCount;
        _validationPeakScheduledSegmented = inputs.ScheduledSegmentedCount;
        _validationPeakScheduledContinuations = inputs.ScheduledContinuationCount;
        _validationPeakCheapSegments = inputs.CheapSegmentCount;
        _validationPeakAltReduced = inputs.AlternateReducedCount;
        _validationPeakAltAttempts = inputs.AlternateAttemptTotal;
        _validationCompletedTotal = 0;
        _validationCompletedSegmentTotal = 0;
        _validationManualCompletedTotal = 0;
        _validationRetriedTotal = 0;
        _validationRetriedSegmentTotal = 0;
        _validationManualRetriedTotal = 0;
        _validationAbandonedTotal = 0;
        _nextValidationStuckLogFrame = frame + StuckLogFirstDelayFrames;
        return true;
    }

    public bool RecordActiveFrameAndShouldLogStuck(int frame, in FrameInputs inputs, in FrameResults results)
    {
        _validationPeakManualQueued = math.max(_validationPeakManualQueued, inputs.ManualQueuedCount);
        _validationPeakManualFollowing = math.max(_validationPeakManualFollowing, inputs.ManualFollowingCount);
        _validationPeakLongMove = math.max(_validationPeakLongMove, inputs.LongDistanceCount);
        _validationPeakCooldown = math.max(_validationPeakCooldown, inputs.RetryCooldownCount);
        _validationPeakScheduledBudget = math.max(_validationPeakScheduledBudget, inputs.ScheduledBudget);
        _validationPeakNextBudget = math.max(_validationPeakNextBudget, inputs.NextBudget);
        _validationPeakPendingFrames = math.max(_validationPeakPendingFrames, inputs.PendingFrames);
        _validationPeakPendingWallMs = math.max(_validationPeakPendingWallMs, inputs.PendingWallMs);
        _validationPeakScheduledManual = math.max(_validationPeakScheduledManual, inputs.ScheduledManualCount);
        _validationPeakScheduledVehicleLike = math.max(_validationPeakScheduledVehicleLike, inputs.ScheduledVehicleLikeCount);
        _validationPeakScheduledSegmented = math.max(_validationPeakScheduledSegmented, inputs.ScheduledSegmentedCount);
        _validationPeakScheduledContinuations = math.max(_validationPeakScheduledContinuations, inputs.ScheduledContinuationCount);
        _validationPeakCheapSegments = math.max(_validationPeakCheapSegments, inputs.CheapSegmentCount);
        _validationPeakAltReduced = math.max(_validationPeakAltReduced, inputs.AlternateReducedCount);
        _validationPeakAltAttempts = math.max(_validationPeakAltAttempts, inputs.AlternateAttemptTotal);
        _validationCompletedTotal += results.CompletedCount;
        _validationCompletedSegmentTotal += results.CompletedSegmentCount;
        _validationManualCompletedTotal += results.ManualCompletedCount;
        _validationRetriedTotal += results.RetriedCount;
        _validationRetriedSegmentTotal += results.RetriedSegmentCount;
        _validationManualRetriedTotal += results.ManualRetriedCount;
        _validationAbandonedTotal += results.AbandonedCount;

        if (!_validationLogActive || frame < _nextValidationStuckLogFrame)
            return false;

        _nextValidationStuckLogFrame = frame + StuckLogIntervalFrames;
        return true;
    }

    public bool TryEnd(bool manualValidationActive, int endFrame, out EndSnapshot snapshot)
    {
        snapshot = default;
        if (manualValidationActive || !_validationLogActive)
            return false;

        snapshot = new EndSnapshot
        {
            StartFrame = _validationStartFrame,
            EndFrame = endFrame,
            PeakManualQueued = _validationPeakManualQueued,
            PeakManualFollowing = _validationPeakManualFollowing,
            PeakLongMove = _validationPeakLongMove,
            PeakCooldown = _validationPeakCooldown,
            PeakScheduledBudget = _validationPeakScheduledBudget,
            PeakNextBudget = _validationPeakNextBudget,
            PeakPendingFrames = _validationPeakPendingFrames,
            PeakPendingWallMs = _validationPeakPendingWallMs,
            PeakScheduledManual = _validationPeakScheduledManual,
            PeakScheduledVehicleLike = _validationPeakScheduledVehicleLike,
            PeakScheduledSegmented = _validationPeakScheduledSegmented,
            PeakScheduledContinuations = _validationPeakScheduledContinuations,
            PeakCheapSegments = _validationPeakCheapSegments,
            PeakAltReduced = _validationPeakAltReduced,
            PeakAltAttempts = _validationPeakAltAttempts,
            CompletedTotal = _validationCompletedTotal,
            CompletedSegmentTotal = _validationCompletedSegmentTotal,
            ManualCompletedTotal = _validationManualCompletedTotal,
            RetriedTotal = _validationRetriedTotal,
            RetriedSegmentTotal = _validationRetriedSegmentTotal,
            ManualRetriedTotal = _validationManualRetriedTotal,
            AbandonedTotal = _validationAbandonedTotal,
        };

        _validationLogActive = false;
        _nextValidationStuckLogFrame = 0;
        return true;
    }

    public struct FrameInputs
    {
        public int ManualQueuedCount;
        public int ManualFollowingCount;
        public int LongDistanceCount;
        public int RetryCooldownCount;
        public int ScheduledBudget;
        public int NextBudget;
        public int PendingFrames;
        public double PendingWallMs;
        public int ScheduledManualCount;
        public int ScheduledVehicleLikeCount;
        public int ScheduledSegmentedCount;
        public int ScheduledContinuationCount;
        public int CheapSegmentCount;
        public int AlternateReducedCount;
        public int AlternateAttemptTotal;
    }

    public struct FrameResults
    {
        public int CompletedCount;
        public int CompletedSegmentCount;
        public int ManualCompletedCount;
        public int RetriedCount;
        public int RetriedSegmentCount;
        public int ManualRetriedCount;
        public int AbandonedCount;
    }

    public struct EndSnapshot
    {
        public int StartFrame;
        public int EndFrame;
        public int PeakManualQueued;
        public int PeakManualFollowing;
        public int PeakLongMove;
        public int PeakCooldown;
        public int PeakScheduledBudget;
        public int PeakNextBudget;
        public int PeakPendingFrames;
        public double PeakPendingWallMs;
        public int PeakScheduledManual;
        public int PeakScheduledVehicleLike;
        public int PeakScheduledSegmented;
        public int PeakScheduledContinuations;
        public int PeakCheapSegments;
        public int PeakAltReduced;
        public int PeakAltAttempts;
        public int CompletedTotal;
        public int CompletedSegmentTotal;
        public int ManualCompletedTotal;
        public int RetriedTotal;
        public int RetriedSegmentTotal;
        public int ManualRetriedTotal;
        public int AbandonedTotal;
    }
}
