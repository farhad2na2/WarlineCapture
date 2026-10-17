using System;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishFrontlineReducerTests
    {
        private static long T(double seconds) => (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
        private static SkirmishFrontlineState New() => SkirmishFrontlineReducer.Create(SkirmishSizeId.Standard, "zone.a", "zone.b", "zone.c");
        private static SkirmishFrontlineTickFacts Facts(double seconds, int player = 0, int enemy = 0) => new SkirmishFrontlineTickFacts
        {
            Playing = true, OriginalBaseFactsPresent = true, PlayerOriginalBaseAlive = true, EnemyOriginalBaseAlive = true,
            SimulationDeltaTicks = T(seconds), ZoneA = new SkirmishFrontlineZoneFacts { PlayerEligibleInfantry = player, EnemyEligibleInfantry = enemy }
        };

        [Test]
        public void InitialTicketsAndAllSizeDeadlinesMatchContract()
        {
            var state = New();
            Assert.AreEqual(500, state.PlayerTickets); Assert.AreEqual(500, state.EnemyTickets);
            Assert.AreEqual(T(1200), state.DeadlineTicks);
            Assert.AreEqual(T(1500), SkirmishFrontlineReducer.Create(SkirmishSizeId.War, "a", "b", "c").DeadlineTicks);
            Assert.AreEqual(T(1800), SkirmishFrontlineReducer.Create(SkirmishSizeId.LargeWar, "a", "b", "c").DeadlineTicks);
            Assert.AreEqual(SkirmishFrontlineSide.Neutral, state.ZoneA.Owner);
        }

        [Test]
        public void NeutralTakesEightHostileOwnedTakesEightPlusEight()
        {
            var neutral = New(); var owned = New(); owned.ZoneA.Owner = SkirmishFrontlineSide.Enemy;
            var eight = Facts(8, 4);
            SkirmishFrontlineReducer.Step(ref neutral, eight); SkirmishFrontlineReducer.Step(ref owned, eight);
            Assert.AreEqual(SkirmishFrontlineSide.Player, neutral.ZoneA.Owner);
            Assert.AreEqual(SkirmishFrontlineSide.Neutral, owned.ZoneA.Owner);
            Assert.AreEqual(SkirmishFrontlineCapturePhase.Capturing, owned.ZoneA.Phase);
            Assert.AreEqual(0, owned.ZoneA.ProgressTicks);
            var almost = Facts(7.9, 4); SkirmishFrontlineReducer.Step(ref owned, almost);
            Assert.AreEqual(SkirmishFrontlineSide.Neutral, owned.ZoneA.Owner);
            var final = Facts(.1, 4); SkirmishFrontlineReducer.Step(ref owned, final);
            Assert.AreEqual(SkirmishFrontlineSide.Player, owned.ZoneA.Owner);
            var combined = New(); combined.ZoneA.Owner = SkirmishFrontlineSide.Enemy;
            var sixteen = Facts(16, 4); SkirmishFrontlineReducer.Step(ref combined, sixteen);
            Assert.AreEqual(owned.ZoneA.Owner, combined.ZoneA.Owner, "Capture overshoot must not discard the second phase.");
        }

        [Test]
        public void MoreInfantryDoesNotAccelerateAndFinalTickContestFreezes()
        {
            var one = New(); var many = New();
            var slow = Facts(7.9, 1); var crowded = Facts(7.9, 64);
            SkirmishFrontlineReducer.Step(ref one, slow); SkirmishFrontlineReducer.Step(ref many, crowded);
            Assert.AreEqual(one.ZoneA.ProgressTicks, many.ZoneA.ProgressTicks);
            var contest = Facts(.1, 64, 1); SkirmishFrontlineReducer.Step(ref many, contest);
            Assert.IsTrue(many.ZoneA.Contested); Assert.AreEqual(T(7.9), many.ZoneA.ProgressTicks);
            Assert.AreEqual(SkirmishFrontlineSide.Neutral, many.ZoneA.Owner);
            var resume = Facts(.1, 64); SkirmishFrontlineReducer.Step(ref many, resume);
            Assert.AreEqual(SkirmishFrontlineSide.Player, many.ZoneA.Owner);
        }

        [Test]
        public void DefendingOwnerAndChangedCapturingFactionDiscardOpposingWork()
        {
            var owned = New(); owned.ZoneA.Owner = SkirmishFrontlineSide.Enemy;
            var attacker = Facts(3, 1); SkirmishFrontlineReducer.Step(ref owned, attacker);
            var defender = Facts(1, 0, 1); SkirmishFrontlineReducer.Step(ref owned, defender);
            Assert.AreEqual(0, owned.ZoneA.ProgressTicks); Assert.AreEqual(SkirmishFrontlineCapturePhase.None, owned.ZoneA.Phase);
            var neutral = New(); SkirmishFrontlineReducer.Step(ref neutral, attacker); SkirmishFrontlineReducer.Step(ref neutral, defender);
            Assert.AreEqual(T(1), neutral.ZoneA.ProgressTicks);
            Assert.AreEqual(SkirmishFrontlineSide.Enemy, neutral.ZoneA.CapturingSide);
        }

        [Test]
        public void EmptyGraceIsExactlyTenThenOneForOneDecayAndOwnershipRetained()
        {
            var state = New(); state.ZoneA.Owner = SkirmishFrontlineSide.Enemy;
            var attack = Facts(6, 1); SkirmishFrontlineReducer.Step(ref state, attack);
            var ten = Facts(10); SkirmishFrontlineReducer.Step(ref state, ten);
            Assert.AreEqual(T(6), state.ZoneA.ProgressTicks); Assert.AreEqual(T(10), state.ZoneA.EmptyGraceTicks);
            var one = Facts(1); SkirmishFrontlineReducer.Step(ref state, one);
            Assert.AreEqual(T(5), state.ZoneA.ProgressTicks);
            var five = Facts(5); SkirmishFrontlineReducer.Step(ref state, five);
            Assert.AreEqual(0, state.ZoneA.ProgressTicks); Assert.AreEqual(SkirmishFrontlineSide.Enemy, state.ZoneA.Owner);
        }

        [Test]
        public void ContestResetsEmptyGraceButOwnedContestedZonesStillDrain()
        {
            var state = New(); state.ZoneA.Owner = state.ZoneB.Owner = SkirmishFrontlineSide.Player;
            state.ZoneA.EmptyGraceTicks = T(9);
            var contest = Facts(2, 1, 1); SkirmishFrontlineReducer.Step(ref state, contest);
            Assert.AreEqual(0, state.ZoneA.EmptyGraceTicks); Assert.IsTrue(state.ZoneA.Contested);
            Assert.AreEqual(498, state.EnemyTickets);
        }

        [Test]
        public void ThreeOwnedZonesStillDrainOnlyOnePerSecondAndClampAtZero()
        {
            var two = New(); two.ZoneA.Owner = two.ZoneB.Owner = SkirmishFrontlineSide.Player;
            var three = two; three.ZoneC.Owner = SkirmishFrontlineSide.Player;
            var tick = Facts(1); SkirmishFrontlineReducer.Step(ref two, tick); SkirmishFrontlineReducer.Step(ref three, tick);
            Assert.AreEqual(499, two.EnemyTickets); Assert.AreEqual(two.EnemyTickets, three.EnemyTickets);
            three.EnemyTickets = 1; tick = Facts(10); SkirmishFrontlineReducer.Step(ref three, tick);
            Assert.AreEqual(0, three.EnemyTickets); Assert.AreEqual(SkirmishOutcomeKind.Victory, three.Outcome);
        }

        [Test]
        public void JsonRoundTripPreservesSevenPointNineCaptureAndFractionalTicketRemainder()
        {
            var state = New(); state.ZoneB.Owner = state.ZoneC.Owner = SkirmishFrontlineSide.Player;
            var almost = Facts(7.9, 1); SkirmishFrontlineReducer.Step(ref state, almost);
            string checkpoint = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<SkirmishFrontlineState>(checkpoint);
            var final = Facts(.1, 1); SkirmishFrontlineReducer.Step(ref state, final); SkirmishFrontlineReducer.Step(ref restored, final);
            Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(restored));
            Assert.AreEqual(SkirmishFrontlineSide.Player, restored.ZoneA.Owner);
            Assert.AreEqual(492, restored.EnemyTickets); Assert.AreEqual(0, restored.EnemyDrainRemainderTicks);
        }

        [Test]
        public void PostCaptureTicketZeroAndReciprocalBaseDeathDrawOnSameTick()
        {
            var state = New(); state.ZoneB.Owner = SkirmishFrontlineSide.Player;
            state.ZoneA.CapturingSide = SkirmishFrontlineSide.Player; state.ZoneA.Phase = SkirmishFrontlineCapturePhase.Capturing;
            state.ZoneA.ProgressTicks = T(7.9); state.EnemyTickets = 1; state.EnemyDrainRemainderTicks = T(.9);
            var final = Facts(.1, 1); final.PlayerOriginalBaseAlive = false;
            SkirmishFrontlineReducer.Step(ref state, final);
            Assert.AreEqual(SkirmishFrontlineSide.Player, state.ZoneA.Owner);
            Assert.AreEqual(0, state.EnemyTickets); Assert.AreEqual(SkirmishOutcomeKind.Draw, state.Outcome);
        }

        [Test]
        public void BothBasesDeadAndBaseDeathAtDeadlinePrecedeTicketComparison()
        {
            var state = New(); state.ElapsedTicks = state.DeadlineTicks - T(1); state.PlayerTickets = 100;
            var final = Facts(1); final.EnemyOriginalBaseAlive = false;
            SkirmishFrontlineReducer.Step(ref state, final);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.MainBaseDestroyed, state.Reason);
            state = New(); final.PlayerOriginalBaseAlive = false;
            SkirmishFrontlineReducer.Step(ref state, final);
            Assert.AreEqual(SkirmishOutcomeKind.Draw, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.BothBasesDestroyed, state.Reason);
        }

        [Test]
        public void DeadlineUsesGreaterOrEqualTicketsAndPauseFreezesEverything()
        {
            foreach (int playerTickets in new[] { 499, 500 })
            {
                var state = New(); state.PlayerTickets = playerTickets; state.ElapsedTicks = state.DeadlineTicks - T(1);
                var final = Facts(1); SkirmishFrontlineReducer.Step(ref state, final);
                Assert.AreEqual(playerTickets == 500 ? SkirmishOutcomeKind.Draw : SkirmishOutcomeKind.Defeat, state.Outcome);
                Assert.AreEqual(SkirmishEndReasonKind.TicketsAtDeadline, state.Reason);
            }
            var ahead = New(); ahead.EnemyTickets = 499; ahead.ElapsedTicks = ahead.DeadlineTicks - T(1);
            var deadline = Facts(1); SkirmishFrontlineReducer.Step(ref ahead, deadline);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, ahead.Outcome);
            var paused = New(); string before = JsonUtility.ToJson(paused);
            var facts = Facts(100, 1); facts.Paused = true; SkirmishFrontlineReducer.Step(ref paused, facts);
            Assert.AreEqual(before, JsonUtility.ToJson(paused));
        }

        [Test]
        public void AcceptedSurrenderAndTerminalResultCannotBeOverwritten()
        {
            var state = New(); Assert.IsTrue(SkirmishFrontlineReducer.TrySurrender(ref state, true));
            Assert.AreEqual(SkirmishEndReasonKind.Surrender, state.Reason);
            string frozen = JsonUtility.ToJson(state); var tick = Facts(100, 100);
            SkirmishFrontlineReducer.Step(ref state, tick);
            Assert.IsFalse(SkirmishFrontlineReducer.TrySurrender(ref state, true));
            Assert.AreEqual(frozen, JsonUtility.ToJson(state));
            state = New(); Assert.IsFalse(SkirmishFrontlineReducer.TrySurrender(ref state, false));
        }

        [Test]
        public void InvalidFactsAndOverflowDoNotPartlyMutateState()
        {
            var state = New(); string before = JsonUtility.ToJson(state);
            var invalid = Facts(1, -1);
            Assert.Throws<ArgumentException>(() => SkirmishFrontlineReducer.Step(ref state, invalid));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
            invalid = Facts(1); invalid.OriginalBaseFactsPresent = false;
            Assert.Throws<ArgumentException>(() => SkirmishFrontlineReducer.Step(ref state, invalid));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
            state.ElapsedTicks = long.MaxValue; before = JsonUtility.ToJson(state);
            var overflow = Facts(1); Assert.Throws<OverflowException>(() => SkirmishFrontlineReducer.Step(ref state, overflow));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishFrontlineReducerTests();
            Action[] tests = { suite.InitialTicketsAndAllSizeDeadlinesMatchContract,
                suite.NeutralTakesEightHostileOwnedTakesEightPlusEight,
                suite.MoreInfantryDoesNotAccelerateAndFinalTickContestFreezes,
                suite.DefendingOwnerAndChangedCapturingFactionDiscardOpposingWork,
                suite.EmptyGraceIsExactlyTenThenOneForOneDecayAndOwnershipRetained,
                suite.ContestResetsEmptyGraceButOwnedContestedZonesStillDrain,
                suite.ThreeOwnedZonesStillDrainOnlyOnePerSecondAndClampAtZero,
                suite.JsonRoundTripPreservesSevenPointNineCaptureAndFractionalTicketRemainder,
                suite.PostCaptureTicketZeroAndReciprocalBaseDeathDrawOnSameTick,
                suite.BothBasesDeadAndBaseDeathAtDeadlinePrecedeTicketComparison,
                suite.DeadlineUsesGreaterOrEqualTicketsAndPauseFreezesEverything,
                suite.AcceptedSurrenderAndTerminalResultCannotBeOverwritten,
                suite.InvalidFactsAndOverflowDoNotPartlyMutateState };
            try { foreach (var test in tests) test(); Debug.Log("[SkirmishFrontlineReducerTests] result=Passed tests=13"); }
            catch (Exception error) { Debug.LogError("[SkirmishFrontlineReducerTests] result=Failed\n" + error); throw; }
        }
    }
}
