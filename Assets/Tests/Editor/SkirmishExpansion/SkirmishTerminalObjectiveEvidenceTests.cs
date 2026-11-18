using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // FC/BT/CE terminal evidence ([SkirmishTerminalObjective]); Base Assault never emits it.
    public sealed class SkirmishTerminalObjectiveEvidenceTests
    {
        public static void RunFocusedValidation()
        {
            var t = new SkirmishTerminalObjectiveEvidenceTests();
            t.OnlyObjectiveKindsApply();
            t.FrontlineLineCarriesClockDeadlineAndZones();
            t.BreakthroughAndConvoyCarryCompletionLossesAndExit();
            t.UnreadScoreboardIsExplicit();
            t.ScoreIsReadFromThePublicSnapshot();
            UnityEngine.Debug.Log("[SkirmishTerminalObjectiveEvidence] result=Passed controls=5");
        }

        [Test]
        public void OnlyObjectiveKindsApply()
        {
            Assert.IsTrue(SkirmishTerminalObjectiveEvidence.Applies(SkirmishObjectiveKind.FrontlineControl));
            Assert.IsTrue(SkirmishTerminalObjectiveEvidence.Applies(SkirmishObjectiveKind.Breakthrough));
            Assert.IsTrue(SkirmishTerminalObjectiveEvidence.Applies(SkirmishObjectiveKind.ConvoyEscort));
            Assert.IsFalse(SkirmishTerminalObjectiveEvidence.Applies(SkirmishObjectiveKind.BaseAssault), "S002/S026 output stays unchanged");
            Assert.IsFalse(SkirmishTerminalObjectiveEvidence.Applies(SkirmishObjectiveKind.None));
        }

        [Test]
        public void FrontlineLineCarriesClockDeadlineAndZones()
        {
            var f = new SkirmishTerminalObjectiveEvidence.Facts { Catalog = "S007", Kind = SkirmishObjectiveKind.FrontlineControl,
                Attempt = "expanded-s007-130365-a", Outcome = "Victory", Reason = "TicketsDepleted", Elapsed = 612.5f, Deadline = 1200f,
                ScoreKnown = true, TicketsKnown = true, OwnZones = 2, OpposingZones = 1, OwnTickets = 88, OpposingTickets = 0 };
            Assert.AreEqual("[SkirmishTerminalObjective] catalog=S007 type=FC attempt=expanded-s007-130365-a outcome=Victory reason=TicketsDepleted " +
                "elapsed=612.500 deadline=1200.0 deadlinePct=51.04 ownZones=2 opposingZones=1 tickets=88/0",
                SkirmishTerminalObjectiveEvidence.Format(f));
        }

        [Test]
        public void BreakthroughAndConvoyCarryCompletionLossesAndExit()
        {
            var bt = new SkirmishTerminalObjectiveEvidence.Facts { Catalog = "S013", Kind = SkirmishObjectiveKind.Breakthrough, Attempt = "a",
                Outcome = "Victory", Reason = "BreakthroughEvacuated", Elapsed = 540f, Deadline = 1080f, ScoreKnown = true, Completed = 8, Required = 8, Lost = 2, ExitOpen = true };
            StringAssert.EndsWith("deadlinePct=50.00 completed=8/8 lost=2 exitOpen=1", SkirmishTerminalObjectiveEvidence.Format(bt));
            var ce = bt; ce.Catalog = "S019"; ce.Kind = SkirmishObjectiveKind.ConvoyEscort; ce.Completed = 2; ce.Required = 2; ce.Lost = 1;
            string line = SkirmishTerminalObjectiveEvidence.Format(ce);
            StringAssert.Contains(" type=CE ", line);
            StringAssert.EndsWith("completed=2/2 lost=1", line);
        }

        [Test]
        public void UnreadScoreboardIsExplicit()
        {
            var f = new SkirmishTerminalObjectiveEvidence.Facts { Kind = SkirmishObjectiveKind.ConvoyEscort, Elapsed = 9f, Deadline = -1f };
            StringAssert.EndsWith("deadline=unknown deadlinePct=unknown score=unknown", SkirmishTerminalObjectiveEvidence.Format(f));
            StringAssert.Contains("catalog=- type=CE attempt=- outcome=- reason=-", SkirmishTerminalObjectiveEvidence.Format(f));
        }

        [Test]
        public void ScoreIsReadFromThePublicSnapshot()
        {
            SkirmishPublicGoalNode Zone(string id, int owner) => new SkirmishPublicGoalNode { PublicId = id, Kind = SkirmishPublicGoalNodeKind.CaptureZone,
                Provenance = SkirmishPublicGoalProvenance.PublicObjective, PositionKnown = true, WorldX = 1, WorldZ = 1, OwnerKnown = true, OwnerFaction = owner,
                CapturePhase = owner != 0 ? SkirmishPublicCapturePhase.Held : SkirmishPublicCapturePhase.None };
            long tick = System.TimeSpan.TicksPerSecond;
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("fc-terminal", 1, 1, SkirmishObjectiveKind.FrontlineControl, SkirmishPublicGoalPerspective.Capture,
                600 * tick, 600 * tick, SkirmishOutcomeKind.Victory, new SkirmishPublicGoalScore { TicketsKnown = true, OwnTickets = 120, OpposingTickets = 0 },
                new[] { Zone("a", 1), Zone("b", 1), Zone("c", 2) }, out var goal, out var error), error);
            var f = new SkirmishTerminalObjectiveEvidence.Facts { Kind = SkirmishObjectiveKind.FrontlineControl, Deadline = -1f };
            SkirmishTerminalObjectiveEvidence.ReadScore(goal, ref f);
            Assert.IsTrue(f.ScoreKnown);
            Assert.AreEqual((2, 1, 120, 0), (f.OwnZones, f.OpposingZones, f.OwnTickets, f.OpposingTickets));
            Assert.AreEqual(1200f, f.Deadline, .001f);
        }
    }
}
