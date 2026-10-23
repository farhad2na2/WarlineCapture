namespace Game.Runtime.Pathfinding
{
    public struct PathRequestAdmissionState
    {
        public int RequestBudget, CollectedCount;
        public byte NextPass;
    }
    // Native query/chunk order is supplied by the caller; this owns pass order and admission.
    public static class PathRequestAdmissionKernel
    {
        public static PathRequestAdmissionState Initialize(int budget)=>new PathRequestAdmissionState {RequestBudget=budget};
        public static bool TryBeginPass(ref PathRequestAdmissionState state,out bool manualPass)
        {
            manualPass=state.NextPass==0;
            if(state.NextPass==0) {state.NextPass=1;return true;}
            if(state.NextPass==1) {state.NextPass=2;return state.CollectedCount<state.RequestBudget;}
            return false;
        }
        public static bool IncludeCandidate(bool manualPass,bool hasManualTag)=>manualPass||!hasManualTag;
        // Deliberately after append: zero/negative budgets still admit the first manual entity.
        public static bool RecordAppend(ref PathRequestAdmissionState state)
        {state.CollectedCount++;return state.CollectedCount>=state.RequestBudget;}
    }
}
