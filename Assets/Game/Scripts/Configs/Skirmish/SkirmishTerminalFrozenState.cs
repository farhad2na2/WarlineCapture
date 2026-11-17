using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Configs
{
    /// <summary>
    /// Pure comparison for the S002 ARIA terminal probe's "terminal state is frozen" check, plus the log text that
    /// shows every predicate's initial and delayed value. Harness evidence only: no gameplay reads this.
    /// </summary>
    public static class SkirmishTerminalFrozenState
    {
        public struct Sample
        {
            /// <summary>False when the match/session could not be read at all.</summary>
            public bool Found;
            public string Session, Phase, Outcome, Attempt;
            public float Elapsed;
            /// <summary>Designated base HP; -1 when the base entity no longer exists.</summary>
            public int PlayerBaseHp, EnemyBaseHp;
            public bool SimulationActive;
            /// <summary>Objective clock flags, -1 when the session has no clock component.</summary>
            public int ClockPlaying, ClockPaused;
        }

        public const float ElapsedTolerance = .05f;
        public const string FinishedPhase = "Finished";

        /// <summary>
        /// A destroyed base is one state. Once the base was at or below 0 HP, the building cleanup that removes its entity
        /// turns the reading into -1 ("not found"); that is not an HP change. Any other difference still is one,
        /// including a live (positive) base that disappears or one that reappears.
        /// </summary>
        public static bool BaseHpFrozen(int initial, int delayed) =>
            initial == delayed || initial <= 0 && delayed <= 0;

        public static bool ClockFrozen(in Sample sample) =>
            sample.ClockPlaying < 0 || sample.ClockPlaying == 0 && sample.ClockPaused != 0;

        /// <summary>Names of the failed predicates behind "terminalNotFrozen" (empty = frozen). Clock flags are separate.</summary>
        public static List<string> Violations(in Sample initial, in Sample delayed, bool attemptMatches)
        {
            var failed = new List<string>();
            if (!delayed.Found) { failed.Add("match"); return failed; }
            if (delayed.Session != initial.Session) failed.Add("session");
            if (delayed.Phase != FinishedPhase) failed.Add("phase");
            if (delayed.Outcome != initial.Outcome) failed.Add("outcome");
            if (!attemptMatches) failed.Add("attempt");
            if (System.Math.Abs(delayed.Elapsed - initial.Elapsed) > ElapsedTolerance) failed.Add("elapsedDrift");
            if (!BaseHpFrozen(initial.PlayerBaseHp, delayed.PlayerBaseHp)) failed.Add("playerBaseHp");
            if (!BaseHpFrozen(initial.EnemyBaseHp, delayed.EnemyBaseHp)) failed.Add("enemyBaseHp");
            if (delayed.SimulationActive) failed.Add("simulationActive");
            return failed;
        }

        public static string Format(in Sample initial, in Sample delayed, IReadOnlyList<string> violations, bool clockFrozen)
        {
            var text = new StringBuilder("[SkirmishTerminalFrozen] result=")
                .Append(violations.Count == 0 && clockFrozen ? "Passed" : "Failed")
                .Append(" violations=").Append(violations.Count == 0 ? "none" : string.Join(",", violations))
                .Append(" clockFrozen=").Append(clockFrozen ? 1 : 0);
            Append(text, "initial", initial);
            Append(text, "delayed", delayed);
            return text.ToString();
        }

        private static void Append(StringBuilder text, string label, in Sample s)
        {
            text.Append(' ').Append(label).Append("={found=").Append(s.Found ? 1 : 0)
                .Append(" session=").Append(s.Session ?? "-").Append(" phase=").Append(s.Phase ?? "-")
                .Append(" outcome=").Append(s.Outcome ?? "-").Append(" attempt=").Append(s.Attempt ?? "-")
                .Append(" elapsed=").Append(s.Elapsed.ToString("0.000", CultureInfo.InvariantCulture))
                .Append(" playerBaseHp=").Append(s.PlayerBaseHp).Append(" enemyBaseHp=").Append(s.EnemyBaseHp)
                .Append(" simActive=").Append(s.SimulationActive ? 1 : 0)
                .Append(" clockPlaying=").Append(s.ClockPlaying).Append(" clockPaused=").Append(s.ClockPaused).Append('}');
        }
    }
}
