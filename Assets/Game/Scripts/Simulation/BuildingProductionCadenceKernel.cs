namespace Game.Simulation
{
    // Exact native managed-production scheduler rules; no wall clock or Unity types.
    public static class BuildingProductionCadenceKernel
    {
        public const double ProductionIntervalSeconds = 0.1d;
        public const double ActiveTransportIntervalSeconds = 0.08d;
        public const double IdleTransportProbeIntervalSeconds = 0.5d;

        public static bool BeginPending(double now, bool disabled, ref double nextProductionAt)
        {
            if (!disabled && now >= nextProductionAt)
            {
                nextProductionAt = now + ProductionIntervalSeconds;
                return true;
            }
            return false;
        }

        public static bool ShouldUpdateActive(double now, bool disabled, bool activeObserved,
            bool pendingObservedActive, double nextProbeAt) =>
            !disabled && ((activeObserved && now >= nextProbeAt) || pendingObservedActive || now >= nextProbeAt);

        public static void RecordActiveResult(double now, bool actualCallbackResult,
            ref bool activeObserved, ref double nextProbeAt)
        {
            activeObserved = actualCallbackResult;
            nextProbeAt = activeObserved ? now + ActiveTransportIntervalSeconds : now + IdleTransportProbeIntervalSeconds;
        }
    }
}
