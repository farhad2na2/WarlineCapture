namespace Game.Runtime.Pathfinding
{
    public enum PathJobAdmission : byte { Inactive, CancelPending, WaitPending, ApplyThenSchedule, Schedule }
    // Completion is an external worker fact; this kernel never invents job latency.
    public static class PathJobSchedulingKernel
    {
        public static PathJobAdmission Decide(bool playRequested, bool hasPending, bool pendingCompleted)
        {
            if (!playRequested) return hasPending ? PathJobAdmission.CancelPending : PathJobAdmission.Inactive;
            if (!hasPending) return PathJobAdmission.Schedule;
            return pendingCompleted ? PathJobAdmission.ApplyThenSchedule : PathJobAdmission.WaitPending;
        }
    }
}
