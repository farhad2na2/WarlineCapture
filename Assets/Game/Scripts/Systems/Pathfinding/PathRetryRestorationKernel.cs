namespace Game.Runtime.Pathfinding
{
    public enum RetryTargetAction : byte { None, Add, Set }
    public enum RetryManualAction : byte { None, Add, Remove }
    public struct PathRetryRestorationFacts
    {
        public bool HasCooldown, HasManual, HasGroupMember, HasLongDistance, HasTarget;
        public bool HasRequest, HasFollow, HasEngage, HasAir;
        public int ResumeFrame;
        public byte LongDistanceManual;
        public PlacementCell Target, FinalGoal;
    }
    public struct PathRetryRestorationPlan
    {
        public bool ClearCooldown, RemoveStaleGroupMember, RestoreRequest;
        public RetryTargetAction TargetAction;
        public RetryManualAction ManualAction;
        public PlacementCell Goal;
    }
    // All four native jobs read the same pre-playback component membership.
    // In particular, clearing an expired cooldown cannot restore a request until
    // the next update. This system has no gameplay-active, pause or death gate.
    public static class PathRetryRestorationKernel
    {
        public static bool ShouldClearCooldown(int resumeFrame, int currentFrame) => resumeFrame <= currentFrame;
        public static PathRetryRestorationPlan Plan(in PathRetryRestorationFacts facts, int currentFrame)
        {
            var result = new PathRetryRestorationPlan
            {
                ClearCooldown = facts.HasCooldown && ShouldClearCooldown(facts.ResumeFrame, currentFrame),
                RemoveStaleGroupMember = facts.HasGroupMember && !facts.HasManual
            };
            if (facts.HasCooldown || facts.HasRequest || facts.HasFollow || facts.HasEngage || facts.HasAir)
                return result;
            if (facts.HasLongDistance)
            {
                result.TargetAction = facts.HasTarget ? RetryTargetAction.Set : RetryTargetAction.Add;
                if (facts.LongDistanceManual != 0 && !facts.HasManual) result.ManualAction = RetryManualAction.Add;
                else if (facts.LongDistanceManual == 0 && facts.HasManual) result.ManualAction = RetryManualAction.Remove;
                result.RestoreRequest = true;
                result.Goal = facts.FinalGoal;
            }
            else if (facts.HasManual && facts.HasTarget)
            {
                result.RestoreRequest = true;
                result.Goal = facts.Target;
            }
            return result;
        }
    }
}
