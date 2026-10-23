using Unity.Mathematics;

namespace Game.Runtime
{
    internal struct UnitPathfindingBudget
    {
        public const int MaxRequestsPerFrame = 32;
        public const int MinRequestsPerFrame = 1;

        private const double FreezeLogThresholdSeconds = 0.05d;
        private const double TargetPathJobWallSeconds = 0.008d;
        private const double LowPathJobWallSeconds = 0.006d;
        private const double HighPathJobWallSeconds = 0.012d;
        private const int MaxManualInfantryRequestsPerFrame = 4;
        private const int StableManualInfantryBatchesBeforeIncrease = 2;
        private const int StableOneFrameBatchesBeforeIncrease = 3;

        private int _adaptiveRequestsPerFrame;
        private int _stableOneFrameBatchCount;
        private bool _pendingBudgetReduced;

        public int AdaptiveRequestsPerFrame => _adaptiveRequestsPerFrame;

        public void Initialize()
        {
            _adaptiveRequestsPerFrame = MinRequestsPerFrame;
            _stableOneFrameBatchCount = 0;
            _pendingBudgetReduced = false;
        }

        public int GetCurrentRequestBudget()
        {
            return math.clamp(_adaptiveRequestsPerFrame, MinRequestsPerFrame, MaxRequestsPerFrame);
        }

        public void ResetPendingJobReduction()
        {
            _pendingBudgetReduced = false;
        }

        public void ReduceForPendingJob(int frameCount, int pendingScheduleFrame, int pendingRequestBudget)
        {
            if (_pendingBudgetReduced || frameCount <= pendingScheduleFrame)
                return;

            _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
            _stableOneFrameBatchCount = 0;
            _pendingBudgetReduced = true;
        }

        public void ReportCompletedJob(
            int pendingFrames,
            double pendingWallTime,
            int requestCount,
            int manualRequestCount,
            int vehicleLikeCount,
            int pendingRequestBudget)
        {
            if (requestCount <= 0)
                return;

            bool allManualInfantry = manualRequestCount == requestCount && vehicleLikeCount == 0;
            if (pendingFrames > 1 || pendingWallTime >= FreezeLogThresholdSeconds)
            {
                _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
                _stableOneFrameBatchCount = 0;
                return;
            }

            if (allManualInfantry)
            {
                if (requestCount >= pendingRequestBudget)
                {
                    _stableOneFrameBatchCount++;
                    if (_stableOneFrameBatchCount >= StableManualInfantryBatchesBeforeIncrease)
                    {
                        _adaptiveRequestsPerFrame = math.min(MaxManualInfantryRequestsPerFrame, pendingRequestBudget + 1);
                        _stableOneFrameBatchCount = 0;
                    }
                    else
                    {
                        _adaptiveRequestsPerFrame = math.max(_adaptiveRequestsPerFrame, pendingRequestBudget);
                    }
                }
                else
                {
                    _stableOneFrameBatchCount = 0;
                    _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, math.min(_adaptiveRequestsPerFrame, pendingRequestBudget));
                }
                return;
            }

            if (pendingWallTime >= HighPathJobWallSeconds)
            {
                _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, pendingRequestBudget / 2);
                _stableOneFrameBatchCount = 0;
                return;
            }

            int targetBudget = math.clamp(
                (int)math.floor(TargetPathJobWallSeconds / math.max(1e-6d, pendingWallTime / requestCount)),
                MinRequestsPerFrame,
                MaxRequestsPerFrame);

            if (pendingWallTime <= LowPathJobWallSeconds && requestCount >= pendingRequestBudget)
            {
                _stableOneFrameBatchCount++;
                if (_stableOneFrameBatchCount >= StableOneFrameBatchesBeforeIncrease)
                {
                    _adaptiveRequestsPerFrame = math.min(targetBudget, pendingRequestBudget + 1);
                    _stableOneFrameBatchCount = 0;
                }
                else
                {
                    _adaptiveRequestsPerFrame = math.min(_adaptiveRequestsPerFrame, targetBudget);
                }
            }
            else
            {
                _stableOneFrameBatchCount = 0;
                _adaptiveRequestsPerFrame = math.min(pendingRequestBudget, targetBudget);
            }
        }
    }
}
