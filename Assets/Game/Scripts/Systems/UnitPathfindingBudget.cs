using Game.Runtime.Pathfinding;
namespace Game.Runtime
{
    internal struct UnitPathfindingBudget
    {
        public const int MaxRequestsPerFrame=PathRequestBudgetKernel.MaxRequestsPerFrame;
        public const int MinRequestsPerFrame=PathRequestBudgetKernel.MinRequestsPerFrame;
        private PathRequestBudgetState _state;
#if UNITY_EDITOR
        // Passive validation receipt; absent from player builds and normally unset.
        public static System.Action<PathRequestBudgetCompletionObservation> CompletionObserved;
        [Unity.Burst.BurstDiscard]
        private static void ObserveCompletion(PathRequestBudgetCompletionObservation observation)=>CompletionObserved?.Invoke(observation);
#endif
        public int AdaptiveRequestsPerFrame=>_state.AdaptiveRequestsPerFrame;
        public void Initialize()=>PathRequestBudgetKernel.Initialize(ref _state);
        public int GetCurrentRequestBudget()=>PathRequestBudgetKernel.GetCurrentRequestBudget(_state);
        public void ResetPendingJobReduction()=>PathRequestBudgetKernel.ResetPendingJobReduction(ref _state);
        public void ReduceForPendingJob(int frameCount,int pendingScheduleFrame,int pendingRequestBudget)=>
            PathRequestBudgetKernel.ReduceForPendingJob(ref _state,frameCount,pendingScheduleFrame,pendingRequestBudget);
        public void ReportCompletedJob(int pendingFrames,double pendingWallTime,int requestCount,int manualRequestCount,int vehicleLikeCount,int pendingRequestBudget)
        {
#if UNITY_EDITOR
            var before=_state;
#endif
            PathRequestBudgetKernel.ReportCompletedJob(ref _state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);
#if UNITY_EDITOR
            ObserveCompletion(new PathRequestBudgetCompletionObservation
            {
                Before=before,After=_state,PendingFrames=pendingFrames,PendingWallTime=pendingWallTime,
                RequestCount=requestCount,ManualRequestCount=manualRequestCount,VehicleLikeCount=vehicleLikeCount,
                PendingRequestBudget=pendingRequestBudget
            });
#endif
        }
    }
}
