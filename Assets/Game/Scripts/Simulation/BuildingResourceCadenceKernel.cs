namespace Game.Simulation
{
    // Numeric scheduling shared by the native managed owner and the CLI host.
    // Callers keep their own clocks, callbacks and profiling boundaries.
    public static class BuildingResourceCadenceKernel
    {
        public const double ProductionIntervalSeconds = 1d;
        public const double HaulerIntervalSeconds = 0.25d;

        public static float ClampDelta(float delta) => 0f > delta ? 0f : delta;

        public static void AccumulateDelta(float clampedDelta, ref float elapsed)
        {
            elapsed += clampedDelta;
        }

        public static bool BeginProduction(double now, bool disabled, ref double nextAt)
        {
            if (!disabled && now >= nextAt)
            {
                nextAt = now + ProductionIntervalSeconds;
                return true;
            }
            return false;
        }

        public static float ConsumeProductionDelta(float clampedDelta, ref float elapsed)
        {
            float result = clampedDelta > elapsed ? clampedDelta : elapsed;
            elapsed = 0f;
            return result;
        }

        public static bool BeginHaulers(double now, bool disabled, ref double nextAt)
        {
            if (!disabled && now >= nextAt)
            {
                nextAt = now + HaulerIntervalSeconds;
                return true;
            }
            return false;
        }
    }
}
