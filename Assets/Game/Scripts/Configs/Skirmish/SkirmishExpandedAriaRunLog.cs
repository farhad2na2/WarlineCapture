using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Game.Configs
{
    public struct SkirmishExpandedAriaTerminalFacts
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
    /// Shared runs.csv gate for expanded ARIA watches (S002, S003, S004, S005).
    /// A row is accepted only from a finished match outcome or an explicit abort.
    /// A caller-supplied Victory is ignored unless the finished match outcome is Victory.
    /// </summary>
    public static class SkirmishExpandedAriaRunLog
    {
        public const string ResultVictory = "Victory";
        public const string ResultDefeat = "Defeat";
        public const string ResultDraw = "Draw";
        public const string ResultAbort = "Abort";
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

        public static bool TryNextAriaRunId(
            string catalogId,
            Func<int, bool> isFirstVisitSeed,
            string invalidSeedError,
            string csvText,
            int seed,
            string locale,
            out string runId,
            out string error)
        {
            runId = null;
            if (isFirstVisitSeed == null || !isFirstVisitSeed(seed))
            {
                error = string.IsNullOrEmpty(invalidSeedError)
                    ? "Seed is not a first-visit Regular Standard ARIA seed for this catalog."
                    : invalidSeedError;
                return false;
            }

            if (!string.Equals(locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) &&
                !string.Equals(locale, GameLocalization.PersianLocaleCode, StringComparison.Ordinal))
            {
                error = "Use catalog locale en or fa-IR.";
                return false;
            }

            string token = string.Equals(locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ? "en" : "fa";
            string prefix = string.Format(
                CultureInfo.InvariantCulture,
                "{0}-aria-rs-{1}-{2}-",
                catalogId,
                seed,
                token);
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                string candidate = prefix + attempt.ToString(CultureInfo.InvariantCulture);
                if (!ContainsRunId(csvText, candidate))
                {
                    runId = candidate;
                    error = null;
                    return true;
                }
            }

            error = "All three ARIA slots for this seed and locale already have rows.";
            return false;
        }

        public static bool TryAcceptTerminal(in SkirmishExpandedAriaTerminalFacts facts, out string result, out string error)
        {
            result = null;
            if (facts.ForcedVictory)
            {
                error = "Forced Victory is refused.";
                return false;
            }

            if (facts.MatchFinished)
            {
                if (IsMatchOutcome(facts.MatchOutcome))
                {
                    result = facts.MatchOutcome;
                    error = null;
                    return true;
                }

                error = "A finished match must report Victory, Defeat, or Draw.";
                return false;
            }

            if (facts.Aborted)
            {
                result = ResultAbort;
                error = null;
                return true;
            }

            error = "The match has not finished.";
            return false;
        }

        public static bool IsCountedAriaWin(in SkirmishExpandedAriaTerminalFacts facts, string result)
        {
            return string.Equals(result, ResultVictory, StringComparison.Ordinal) &&
                   facts.MatchFinished &&
                   string.Equals(facts.MatchOutcome, ResultVictory, StringComparison.Ordinal) &&
                   !facts.ForcedVictory &&
                   facts.NormalSpeed &&
                   facts.InputViolations == 0 &&
                   facts.HumanInterventions == 0;
        }

        public static bool TryFormatRow(
            string catalogId,
            in SkirmishExpandedAriaTerminalFacts facts,
            out string row,
            out string error)
        {
            row = null;
            if (!TryAcceptTerminal(in facts, out string result, out error))
                return false;
            if (string.IsNullOrEmpty(facts.RunId))
            {
                error = "run_id is required.";
                return false;
            }

            string[] fields =
            {
                facts.RunId,
                catalogId ?? string.Empty,
                facts.DefinitionVersion.ToString(CultureInfo.InvariantCulture),
                facts.CodeHash ?? string.Empty,
                facts.ConfigHash ?? string.Empty,
                "Standard",
                "Regular",
                facts.Seed.ToString(CultureInfo.InvariantCulture),
                facts.Locale ?? string.Empty,
                facts.Device ?? string.Empty,
                "aria",
                facts.NormalSpeed ? "1" : "0",
                facts.StartedAtUtc ?? string.Empty,
                result,
                facts.EndReason ?? string.Empty,
                facts.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                facts.InputViolations.ToString(CultureInfo.InvariantCulture),
                facts.HumanInterventions.ToString(CultureInfo.InvariantCulture),
                facts.TracePath ?? string.Empty,
                facts.LogPath ?? string.Empty
            };
            row = string.Join(",", fields);
            error = null;
            return true;
        }

        public static bool TryAppendTerminalRow(
            string catalogId,
            string runsCsvPath,
            in SkirmishExpandedAriaTerminalFacts facts,
            out string row,
            out string error)
        {
            row = null;
            if (string.IsNullOrEmpty(runsCsvPath))
            {
                error = "runs.csv path is required.";
                return false;
            }

            if (!TryFormatRow(catalogId, in facts, out row, out error))
                return false;

            string existing = File.Exists(runsCsvPath) ? File.ReadAllText(runsCsvPath) : string.Empty;
            if (!string.IsNullOrEmpty(existing) &&
                !existing.StartsWith(SkirmishAcceptanceScaffold.RequiredHeader, StringComparison.Ordinal))
            {
                row = null;
                error = "runs.csv header does not match the mandated fields.";
                return false;
            }

            var builder = new StringBuilder();
            if (string.IsNullOrEmpty(existing))
                builder.Append(SkirmishAcceptanceScaffold.RequiredHeader).Append('\n');
            else
            {
                builder.Append(existing);
                if (!existing.EndsWith("\n", StringComparison.Ordinal))
                    builder.Append('\n');
            }

            builder.Append(row).Append('\n');
            string directory = Path.GetDirectoryName(runsCsvPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(runsCsvPath, builder.ToString());
            error = null;
            return true;
        }

        private static bool IsMatchOutcome(string outcome)
        {
            return string.Equals(outcome, ResultVictory, StringComparison.Ordinal) ||
                   string.Equals(outcome, ResultDefeat, StringComparison.Ordinal) ||
                   string.Equals(outcome, ResultDraw, StringComparison.Ordinal);
        }

        private static bool ContainsRunId(string csvText, string runId)
        {
            if (string.IsNullOrEmpty(csvText) || string.IsNullOrEmpty(runId))
                return false;
            string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(runId + ",", StringComparison.Ordinal) ||
                    string.Equals(line, runId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
