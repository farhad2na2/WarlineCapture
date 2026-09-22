using System;

namespace Game.Configs
{
    public struct SkirmishS002AriaTerminalFacts
    {
        public string RunId;
        public int DefinitionVersion;
        public string CodeHash;
        public string ConfigHash;
        public int Seed;
        public string Locale;
        public string Device;
        public bool NormalSpeed;
        public string StartedAtUtc;
        public bool MatchFinished;
        public string MatchOutcome;
        public string EndReason;
        public float DurationSeconds;
        public int InputViolations;
        public int HumanInterventions;
        public string TracePath;
        public string LogPath;
        public bool ForcedVictory;
        public bool Aborted;
    }

    /// <summary>
    /// S002 runs.csv gate. Finished-match rows delegate to <see cref="SkirmishExpandedAriaRunLog"/>.
    /// The stuck-simulation abort delegates to <see cref="SkirmishExpandedAriaFailFast"/>, the same
    /// rule used by the S003, S004, and S005 watches. A caller-supplied Victory is ignored unless
    /// the finished match outcome is Victory.
    /// </summary>
    public static class SkirmishS002AriaRunLog
    {
        public const string ResultVictory = SkirmishExpandedAriaRunLog.ResultVictory;
        public const string ResultDefeat = SkirmishExpandedAriaRunLog.ResultDefeat;
        public const string ResultDraw = SkirmishExpandedAriaRunLog.ResultDraw;
        public const string ResultAbort = SkirmishExpandedAriaRunLog.ResultAbort;
        public const string AbortReasonSimulationNotAdvancing = SkirmishExpandedAriaFailFast.AbortReasonSimulationNotAdvancing;
        public const double SimulationStallGraceSeconds = SkirmishExpandedAriaFailFast.SimulationStallGraceSeconds;
        public const string InvalidSeedError = "First-visit ARIA seed must be 104731, 130365 or 155923.";

        public static bool IsSimulationNotAdvancing(
            bool playing,
            bool simulationActive,
            float matchElapsedSeconds,
            double wallSecondsSincePlaying,
            double graceSeconds = SimulationStallGraceSeconds)
        {
            return SkirmishExpandedAriaFailFast.IsSimulationNotAdvancing(
                playing,
                simulationActive,
                matchElapsedSeconds,
                wallSecondsSincePlaying,
                graceSeconds);
        }

        public static bool TryNextAriaRunId(string csvText, int seed, string locale, out string runId, out string error)
        {
            return SkirmishExpandedAriaRunLog.TryNextAriaRunId(
                SkirmishAcceptanceCensusCapture.CatalogId,
                SkirmishAcceptanceCensusCapture.IsRegularStandardAriaSeed,
                InvalidSeedError,
                csvText,
                seed,
                locale,
                out runId,
                out error);
        }

        public static bool TryAcceptTerminal(in SkirmishS002AriaTerminalFacts facts, out string result, out string error)
        {
            SkirmishExpandedAriaTerminalFacts expanded = AsExpanded(in facts);
            return SkirmishExpandedAriaRunLog.TryAcceptTerminal(in expanded, out result, out error);
        }

        public static bool IsCountedAriaWin(in SkirmishS002AriaTerminalFacts facts, string result)
        {
            SkirmishExpandedAriaTerminalFacts expanded = AsExpanded(in facts);
            return SkirmishExpandedAriaRunLog.IsCountedAriaWin(in expanded, result);
        }

        public static bool TryFormatRow(in SkirmishS002AriaTerminalFacts facts, out string row, out string error)
        {
            SkirmishExpandedAriaTerminalFacts expanded = AsExpanded(in facts);
            return SkirmishExpandedAriaRunLog.TryFormatRow(
                SkirmishAcceptanceCensusCapture.CatalogId,
                in expanded,
                out row,
                out error);
        }

        public static bool TryAppendTerminalRow(string runsCsvPath, in SkirmishS002AriaTerminalFacts facts, out string row, out string error)
        {
            SkirmishExpandedAriaTerminalFacts expanded = AsExpanded(in facts);
            return SkirmishExpandedAriaRunLog.TryAppendTerminalRow(
                SkirmishAcceptanceCensusCapture.CatalogId,
                runsCsvPath,
                in expanded,
                out row,
                out error);
        }

        private static SkirmishExpandedAriaTerminalFacts AsExpanded(in SkirmishS002AriaTerminalFacts facts)
        {
            return new SkirmishExpandedAriaTerminalFacts
            {
                RunId = facts.RunId,
                DefinitionVersion = facts.DefinitionVersion,
                CodeHash = facts.CodeHash,
                ConfigHash = facts.ConfigHash,
                Seed = facts.Seed,
                Locale = facts.Locale,
                Device = facts.Device,
                NormalSpeed = facts.NormalSpeed,
                StartedAtUtc = facts.StartedAtUtc,
                MatchFinished = facts.MatchFinished,
                MatchOutcome = facts.MatchOutcome,
                EndReason = facts.EndReason,
                DurationSeconds = facts.DurationSeconds,
                InputViolations = facts.InputViolations,
                HumanInterventions = facts.HumanInterventions,
                TracePath = facts.TracePath,
                LogPath = facts.LogPath,
                ForcedVictory = facts.ForcedVictory,
                Aborted = facts.Aborted
            };
        }
    }
}
