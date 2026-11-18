using System.Globalization;
using System.Text;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    /// <summary>
    /// Harness evidence for FC/BT/CE (Frontline Control, Breakthrough, Convoy Escort) terminals, next to the
    /// shared [SkirmishTerminalFrozen]/[SkirmishTerminalProbe] lines: the objective outcome, end reason, clock,
    /// deadline and the public scoreboard the player saw. Base Assault never uses it, so its markers stay
    /// unchanged. Pure; no gameplay reads this.
    /// </summary>
    public static class SkirmishTerminalObjectiveEvidence
    {
        public const string Marker = "[SkirmishTerminalObjective]";

        public struct Facts
        {
            public string Catalog, Attempt, Outcome, Reason;
            public SkirmishObjectiveKind Kind;
            public float Elapsed;
            /// <summary>Objective deadline in seconds from the public goal clock; negative when unread.</summary>
            public float Deadline;
            public bool ScoreKnown, TicketsKnown, ExitOpen;
            public int OwnZones, OpposingZones, OwnTickets, OpposingTickets, Completed, Required, Lost;
        }

        /// <summary>FC/BT/CE terminals get the objective line and gate-3 units on an ARIA stop.</summary>
        public static bool Applies(SkirmishObjectiveKind kind) =>
            kind == SkirmishObjectiveKind.FrontlineControl || kind == SkirmishObjectiveKind.Breakthrough ||
            kind == SkirmishObjectiveKind.ConvoyEscort;

        public static string Code(SkirmishObjectiveKind kind) => kind switch
        {
            SkirmishObjectiveKind.FrontlineControl => "FC",
            SkirmishObjectiveKind.Breakthrough => "BT",
            SkirmishObjectiveKind.ConvoyEscort => "CE",
            _ => "none"
        };

        public static void ReadScore(SkirmishPublicGoalSnapshot goal, ref Facts facts)
        {
            if (goal == null) return;
            facts.ScoreKnown = true;
            facts.TicketsKnown = goal.Score.TicketsKnown;
            facts.ExitOpen = goal.Score.ExitStateKnown && goal.Score.ExitOpen;
            facts.OwnTickets = goal.Score.OwnTickets; facts.OpposingTickets = goal.Score.OpposingTickets;
            facts.Completed = goal.Score.Completed; facts.Required = goal.Score.Required; facts.Lost = goal.Score.SubjectsLost;
            facts.Deadline = (goal.ElapsedTicks + goal.RemainingTicks) / (float)System.TimeSpan.TicksPerSecond;
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.Kind != SkirmishPublicGoalNodeKind.CaptureZone || !node.OwnerKnown || node.OwnerFaction == 0) continue;
                if (node.OwnerFaction == goal.ObserverFaction) facts.OwnZones++; else facts.OpposingZones++;
            }
        }

        public static string Format(in Facts f)
        {
            var c = CultureInfo.InvariantCulture;
            var text = new StringBuilder(Marker)
                .Append(" catalog=").Append(string.IsNullOrEmpty(f.Catalog) ? "-" : f.Catalog)
                .Append(" type=").Append(Code(f.Kind))
                .Append(" attempt=").Append(string.IsNullOrEmpty(f.Attempt) ? "-" : f.Attempt)
                .Append(" outcome=").Append(string.IsNullOrEmpty(f.Outcome) ? "-" : f.Outcome)
                .Append(" reason=").Append(string.IsNullOrEmpty(f.Reason) ? "-" : f.Reason)
                .Append(" elapsed=").Append(f.Elapsed.ToString("0.000", c))
                .Append(" deadline=").Append(f.Deadline >= 0 && f.ScoreKnown ? f.Deadline.ToString("0.0", c) : "unknown")
                .Append(" deadlinePct=").Append(f.Deadline > 0 && f.ScoreKnown ? (100f * f.Elapsed / f.Deadline).ToString("0.00", c) : "unknown");
            if (!f.ScoreKnown) return text.Append(" score=unknown").ToString();
            if (f.Kind == SkirmishObjectiveKind.FrontlineControl)
                text.Append(" ownZones=").Append(f.OwnZones).Append(" opposingZones=").Append(f.OpposingZones)
                    .Append(" tickets=").Append(f.OwnTickets).Append('/').Append(f.OpposingTickets);
            else
                text.Append(" completed=").Append(f.Completed).Append('/').Append(f.Required).Append(" lost=").Append(f.Lost);
            if (f.Kind == SkirmishObjectiveKind.Breakthrough) text.Append(" exitOpen=").Append(f.ExitOpen ? 1 : 0);
            return text.ToString();
        }
    }
}
