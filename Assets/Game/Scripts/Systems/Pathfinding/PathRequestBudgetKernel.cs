using System;

namespace Game.Runtime.Pathfinding
{
    public struct PathRequestBudgetState
    {
        public int AdaptiveRequestsPerFrame, StableOneFrameBatchCount;
        public bool PendingBudgetReduced;
    }

    public struct PathRequestBudgetCompletionObservation
    {
        public PathRequestBudgetState Before, After;
        public int PendingFrames, RequestCount, ManualRequestCount, VehicleLikeCount, PendingRequestBudget;
        public double PendingWallTime;
    }

    public static class PathRequestBudgetKernel
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

        private static int Max(int a,int b) => a > b ? a : b;
        private static int Min(int a,int b) => a < b ? a : b;
        private static double Max(double a,double b) => double.IsNaN(b) || a > b ? a : b;
        private static int Clamp(int value,int min,int max) => Max(min,Min(max,value));

        public static void Initialize(ref PathRequestBudgetState state)
        {
            state.AdaptiveRequestsPerFrame = MinRequestsPerFrame;
            state.StableOneFrameBatchCount = 0;
            state.PendingBudgetReduced = false;
        }

        public static int GetCurrentRequestBudget(in PathRequestBudgetState state)
        {
            return Clamp(state.AdaptiveRequestsPerFrame, MinRequestsPerFrame, MaxRequestsPerFrame);
        }

        public static void ResetPendingJobReduction(ref PathRequestBudgetState state)
        {
            state.PendingBudgetReduced = false;
        }

        public static void ReduceForPendingJob(ref PathRequestBudgetState state, int frameCount, int pendingScheduleFrame, int pendingRequestBudget)
        {
            if (state.PendingBudgetReduced || frameCount <= pendingScheduleFrame)
                return;

            state.AdaptiveRequestsPerFrame = Max(MinRequestsPerFrame, pendingRequestBudget / 2);
            state.StableOneFrameBatchCount = 0;
            state.PendingBudgetReduced = true;
        }

        public static void ReportCompletedJob(
            ref PathRequestBudgetState state,
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
                state.AdaptiveRequestsPerFrame = Max(MinRequestsPerFrame, pendingRequestBudget / 2);
                state.StableOneFrameBatchCount = 0;
                return;
            }

            if (allManualInfantry)
            {
                if (requestCount >= pendingRequestBudget)
                {
                    state.StableOneFrameBatchCount++;
                    if (state.StableOneFrameBatchCount >= StableManualInfantryBatchesBeforeIncrease)
                    {
                        state.AdaptiveRequestsPerFrame = Min(MaxManualInfantryRequestsPerFrame, pendingRequestBudget + 1);
                        state.StableOneFrameBatchCount = 0;
                    }
                    else
                    {
                        state.AdaptiveRequestsPerFrame = Max(state.AdaptiveRequestsPerFrame, pendingRequestBudget);
                    }
                }
                else
                {
                    state.StableOneFrameBatchCount = 0;
                    state.AdaptiveRequestsPerFrame = Max(MinRequestsPerFrame, Min(state.AdaptiveRequestsPerFrame, pendingRequestBudget));
                }
                return;
            }

            if (pendingWallTime >= HighPathJobWallSeconds)
            {
                state.AdaptiveRequestsPerFrame = Max(MinRequestsPerFrame, pendingRequestBudget / 2);
                state.StableOneFrameBatchCount = 0;
                return;
            }

            int targetBudget = Clamp(
                (int)Math.Floor(TargetPathJobWallSeconds / Max(1e-6d, pendingWallTime / requestCount)),
                MinRequestsPerFrame,
                MaxRequestsPerFrame);

            if (pendingWallTime <= LowPathJobWallSeconds && requestCount >= pendingRequestBudget)
            {
                state.StableOneFrameBatchCount++;
                if (state.StableOneFrameBatchCount >= StableOneFrameBatchesBeforeIncrease)
                {
                    state.AdaptiveRequestsPerFrame = Min(targetBudget, pendingRequestBudget + 1);
                    state.StableOneFrameBatchCount = 0;
                }
                else
                {
                    state.AdaptiveRequestsPerFrame = Min(state.AdaptiveRequestsPerFrame, targetBudget);
                }
            }
            else
            {
                state.StableOneFrameBatchCount = 0;
                state.AdaptiveRequestsPerFrame = Min(pendingRequestBudget, targetBudget);
            }
        }
    }
}
