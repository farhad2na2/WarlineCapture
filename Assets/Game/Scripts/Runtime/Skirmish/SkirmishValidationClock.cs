using System;
using Game.Simulation;
namespace Game.Runtime
{
    // Shipping reads are unchanged. Only the owned Editor diagnostic can install a clock.
    public static class SkirmishValidationClock
    {
#if UNITY_EDITOR || WARLINE_VALIDATION_PLAYER
        private static SkirmishValidationStepClock clock;
        public static bool Active => clock != null;
        public static string Failure => clock?.Failure;
        public static int Samples => clock?.Samples ?? 0;
        public static void Begin(int frame, double scaled, float unscaled, double realtime, float fixedStep)
        {
            if (clock != null) throw new InvalidOperationException("Validation clock already owned");
            clock = new SkirmishValidationStepClock(frame, scaled, unscaled, realtime, fixedStep);
        }
        public static void Sample(int frame, double scaled, float scaledDelta, float unscaledDelta, bool paused)
            => clock?.Sample(frame, scaled, scaledDelta, unscaledDelta, paused);
        public static void End() { clock = null; }
#endif
        public static double RealtimeOrOriginal(double original)
        {
#if UNITY_EDITOR || WARLINE_VALIDATION_PLAYER
            if (clock != null) return clock.RealtimeNow;
#endif
            return original;
        }
        public static float UnscaledOrOriginal(float original)
        {
#if UNITY_EDITOR || WARLINE_VALIDATION_PLAYER
            if (clock != null) return clock.UnscaledNow;
#endif
            return original;
        }
        public static float DeltaOrOriginal(float original)
        {
#if UNITY_EDITOR || WARLINE_VALIDATION_PLAYER
            if (clock != null) return clock.Delta;
#endif
            return original;
        }
    }
}
