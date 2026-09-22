namespace Game.Configs
{
    /// <summary>
    /// Shared stuck-simulation fail-fast for the S002, S003, S004, and S005 ARIA watches.
    /// After Playing, the watch aborts with <see cref="AbortReasonSimulationNotAdvancing"/>
    /// once <see cref="SimulationStallGraceSeconds"/> have passed while simulation is inactive
    /// or match elapsed is still at 0. The watches do not invent a later catalog.
    /// </summary>
    public static class SkirmishExpandedAriaFailFast
    {
        public const string AbortReasonSimulationNotAdvancing = "simulationNotAdvancing";
        public const double SimulationStallGraceSeconds = 45d;

        /// <summary>
        /// After Playing, match elapsed must leave 0 once simulation is armed.
        /// A stuck-zero clock for the grace window means the watch should abort
        /// instead of burning the full wall-clock budget.
        /// </summary>
        public static bool IsSimulationNotAdvancing(
            bool playing,
            bool simulationActive,
            float matchElapsedSeconds,
            double wallSecondsSincePlaying,
            double graceSeconds = SimulationStallGraceSeconds)
        {
            if (!playing || wallSecondsSincePlaying < graceSeconds)
                return false;
            if (!simulationActive)
                return true;
            return matchElapsedSeconds <= 0f;
        }

        /// <summary>
        /// Wall seconds since the watch first observed Playing.
        /// A non-positive stamp means Playing has not been recorded, so the stall window has not started.
        /// </summary>
        public static double WallSecondsSincePlaying(double editorTimeSinceStartup, double playingSince)
        {
            if (playingSince <= 0d)
                return 0d;
            return editorTimeSinceStartup - playingSince;
        }

        /// <summary>
        /// Simulation is armed only when exactly one gameplay state exists and its flag is set.
        /// </summary>
        public static bool IsSimulationActive(int gameplayStateCount, byte simulationActive)
        {
            return gameplayStateCount == 1 && simulationActive != 0;
        }

        /// <summary>
        /// Expanded matches report elapsed from the objective clock when that component is present.
        /// Otherwise the match-state elapsed is used.
        /// </summary>
        public static float ReadMatchElapsedSeconds(
            bool hasObjectiveClock,
            float clockElapsedSeconds,
            float matchElapsedSeconds)
        {
            return hasObjectiveClock ? clockElapsedSeconds : matchElapsedSeconds;
        }
    }
}
