namespace Game.Runtime
{
    // Admission only: these are the existing individual system rules, not a global pause
    // policy or a replacement for Unity's sorted group and EndSimulation playback.
    public static class WorldSchedulingKernel
    {
        public const float AiTargetRefreshSeconds = 0.5f;
        public const double UnitTargetAcquisitionSeconds = 0.12d;

        public static bool TryAdvanceAiTargetRefresh(bool simulationActive, double elapsed,
            ref float next, out float now)
        {
            now = 0f;
            if (!simulationActive) return false;
            now = elapsed > float.MaxValue ? float.MaxValue : (float)elapsed;
            if (now < next) return false;
            next = now + AiTargetRefreshSeconds;
            return true;
        }

        public static bool TryAdvanceUnitAcquisition(double now, ref double next)
        {
            if (now < next) return false;
            next = now + UnitTargetAcquisitionSeconds;
            return true;
        }

        public static bool ShouldRunGridMovement(float deltaTime, bool hasGameplay, byte simulationActive) =>
            !(deltaTime <= 0f || (hasGameplay && simulationActive == 0));

        public static bool ShouldRunAttack(bool hasSkirmish, bool finished) => !(hasSkirmish && finished);
    }
}
