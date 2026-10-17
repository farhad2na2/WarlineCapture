using System;
using System.Linq;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishBreakthroughReducerTests
    {
        private static long T(double seconds) => (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
        private static string[] Ids() => Enumerable.Range(1, 12).Select(i => "original.rifle." + i).ToArray();
        private static SkirmishBreakthroughState New(bool open = false)
        {
            var state = SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard, "corridor.a", "corridor.b", Ids());
            state.ExitOpen = open; return state;
        }
        private static SkirmishBreakthroughTickFacts Facts(double seconds, int atExit = 0) => new SkirmishBreakthroughTickFacts
        {
            Playing = true, SimulationDeltaTicks = T(seconds), OriginalSubjects = Ids().Select((id, i) =>
                new SkirmishBreakthroughSubjectFacts { StableOriginalId = id, AuthoritativeLifeKnown = true,
                    Alive = true, Dismounted = true, InsideExit = i < atExit }).ToArray()
        };
        private static int Count(SkirmishBreakthroughState state, SkirmishBreakthroughSubjectStatus status) =>
            state.OriginalSubjects.Count(subject => subject.Status == status);

        [Test]
        public void ExactlyTwelveOriginalIdsAndSizeDeadlinesAreRequired()
        {
            var state = New(); Assert.AreEqual(12, state.OriginalSubjects.Length); Assert.AreEqual(T(1080), state.DeadlineTicks);
            Assert.AreEqual(T(1500), SkirmishBreakthroughReducer.Create(SkirmishSizeId.War, "a", "b", Ids()).DeadlineTicks);
            Assert.AreEqual(T(1800), SkirmishBreakthroughReducer.Create(SkirmishSizeId.LargeWar, "a", "b", Ids()).DeadlineTicks);
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard, "a", "b", Ids().Take(11).ToArray()));
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard, "a", "b", Ids().Concat(new[] { "extra" }).ToArray()));
            var duplicate = Ids(); duplicate[11] = duplicate[0];
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard, "a", "b", duplicate));
        }

        [Test]
        public void EitherCorridorCanOpenWithOrdinarySupportInfantry()
        {
            foreach (bool useB in new[] { false, true })
            {
                var state = New(); var tick = Facts(20);
                if (useB) tick.CorridorB.PlayerEligibleInfantry = 1; else tick.CorridorA.PlayerEligibleInfantry = 1;
                Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, tick));
                Assert.IsTrue(state.ExitOpen); Assert.AreEqual(0, Count(state, SkirmishBreakthroughSubjectStatus.Evacuated));
            }
        }

        [Test]
        public void HoldingBothCorridorsDoesNotReduceEightRequired()
        {
            var state = New(); var open = Facts(20); open.CorridorA.PlayerEligibleInfantry = open.CorridorB.PlayerEligibleInfantry = 64;
            SkirmishBreakthroughReducer.Step(ref state, open);
            var seven = Facts(3, 7); Assert.AreEqual(7, SkirmishBreakthroughReducer.Step(ref state, seven).Length);
            Assert.IsFalse(state.Terminal);
            var eight = Facts(3, 8); Assert.AreEqual(1, SkirmishBreakthroughReducer.Step(ref state, eight).Length);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome);
        }

        [Test]
        public void ContestOrEmptyCorridorResetsEvenOnFinalHoldTick()
        {
            var state = New(); var almost = Facts(19.9); almost.CorridorA.PlayerEligibleInfantry = 1;
            SkirmishBreakthroughReducer.Step(ref state, almost);
            var contest = Facts(.1); contest.CorridorA.PlayerEligibleInfantry = contest.CorridorA.EnemyEligibleInfantry = 1;
            SkirmishBreakthroughReducer.Step(ref state, contest); Assert.AreEqual(0, state.CorridorA.HoldTicks); Assert.IsFalse(state.ExitOpen);
            SkirmishBreakthroughReducer.Step(ref state, almost);
            var empty = Facts(.1); SkirmishBreakthroughReducer.Step(ref state, empty);
            Assert.AreEqual(0, state.CorridorA.HoldTicks); Assert.IsFalse(state.ExitOpen);
        }

        [Test]
        public void ExitLatchSurvivesLosingBothCorridors()
        {
            var state = New(); var open = Facts(20); open.CorridorA.PlayerEligibleInfantry = 1;
            SkirmishBreakthroughReducer.Step(ref state, open);
            var lost = Facts(1); lost.CorridorA.EnemyEligibleInfantry = lost.CorridorB.EnemyEligibleInfantry = 1;
            SkirmishBreakthroughReducer.Step(ref state, lost);
            Assert.IsTrue(state.ExitOpen); Assert.AreEqual(0, state.CorridorA.HoldTicks); Assert.AreEqual(0, state.CorridorB.HoldTicks);
        }

        [Test]
        public void ExactUnlockTickNeverCreditsRetroactiveExitDwell()
        {
            var state = New(); var open = Facts(20, 8); open.CorridorA.PlayerEligibleInfantry = 1;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, open));
            Assert.IsTrue(state.ExitOpen); Assert.AreEqual(0, state.OriginalSubjects[0].ExitDwellTicks);
            var dwell = Facts(3, 8); Assert.AreEqual(8, SkirmishBreakthroughReducer.Step(ref state, dwell).Length);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome);
        }

        [Test]
        public void TransportedFlyoverCannotCountUntilDismountedThreeSeconds()
        {
            var state = New(true); var carried = Facts(5, 8);
            for (int i = 0; i < 8; i++) carried.OriginalSubjects[i].Dismounted = false;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, carried));
            var almost = Facts(2.9, 8); Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, almost));
            var final = Facts(.1, 8); Assert.AreEqual(8, SkirmishBreakthroughReducer.Step(ref state, final).Length);
        }

        [Test]
        public void LeavingOrBoardingResetsIndividualUninterruptedDwell()
        {
            var state = New(true); var almost = Facts(2.9, 2); SkirmishBreakthroughReducer.Step(ref state, almost);
            var interrupt = Facts(.1, 2); interrupt.OriginalSubjects[0].InsideExit = false; interrupt.OriginalSubjects[1].Dismounted = false;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, interrupt));
            Assert.AreEqual(0, state.OriginalSubjects[0].ExitDwellTicks); Assert.AreEqual(0, state.OriginalSubjects[1].ExitDwellTicks);
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, almost));
            var final = Facts(.1, 2); Assert.AreEqual(2, SkirmishBreakthroughReducer.Step(ref state, final).Length);
        }

        [Test]
        public void DeathOnThirdSecondPreventsCreditAndRepeatedCarrierDeathDoesNotDuplicate()
        {
            var state = New(true); var almost = Facts(2.9, 1); SkirmishBreakthroughReducer.Step(ref state, almost);
            var killed = Facts(.1, 1); killed.OriginalSubjects[0].Alive = false;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, killed));
            Assert.AreEqual(SkirmishBreakthroughSubjectStatus.Dead, state.OriginalSubjects[0].Status);
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, killed));
            Assert.AreEqual(1, Count(state, SkirmishBreakthroughSubjectStatus.Dead));
        }

        [Test]
        public void FourDeathsRemainWinnableFifthMakesVictoryImpossible()
        {
            var state = New(true); var four = Facts(0);
            for (int i = 0; i < 4; i++) four.OriginalSubjects[i].Alive = false;
            SkirmishBreakthroughReducer.Step(ref state, four); Assert.IsFalse(state.Terminal);
            var canWin = state; canWin.OriginalSubjects = (SkirmishBreakthroughSubjectState[])state.OriginalSubjects.Clone();
            var survivors = Facts(3, 12); for (int i = 0; i < 4; i++) survivors.OriginalSubjects[i].Alive = false;
            Assert.AreEqual(8, SkirmishBreakthroughReducer.Step(ref canWin, survivors).Length);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, canWin.Outcome);
            four.OriginalSubjects[4].Alive = false; SkirmishBreakthroughReducer.Step(ref state, four);
            Assert.AreEqual(SkirmishOutcomeKind.Defeat, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.BreakthroughImpossible, state.Reason);
        }

        [Test]
        public void ReplacementRecruitAndDuplicateFactsCannotReplaceDesignatedIdentity()
        {
            var state = New(true); string before = JsonUtility.ToJson(state); var replacement = Facts(3, 8);
            replacement.OriginalSubjects[0].StableOriginalId = "new.recruit";
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Step(ref state, replacement));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
            replacement = Facts(3, 8); replacement.OriginalSubjects[1].StableOriginalId = replacement.OriginalSubjects[0].StableOriginalId;
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Step(ref state, replacement));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        [Test]
        public void EvacuatedTombstonesSurviveCheckpointWithoutDuplicateTransition()
        {
            var state = New(true); var exit = Facts(3, 1);
            Assert.AreEqual(1, SkirmishBreakthroughReducer.Step(ref state, exit).Length);
            var restored = JsonUtility.FromJson<SkirmishBreakthroughState>(JsonUtility.ToJson(state));
            var removedActor = Facts(0); removedActor.OriginalSubjects[0].Alive = false;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref restored, removedActor));
            Assert.AreEqual(1, Count(restored, SkirmishBreakthroughSubjectStatus.Evacuated));
            Assert.AreEqual(0, Count(restored, SkirmishBreakthroughSubjectStatus.Dead));
            var replayed = Facts(3, 1); Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref restored, replayed));
        }

        [Test]
        public void CheckpointPreservesNineteenPointNineHoldAndTwoPointNineDwell()
        {
            var state = New(); var almost = Facts(19.9); almost.CorridorB.PlayerEligibleInfantry = 1;
            SkirmishBreakthroughReducer.Step(ref state, almost);
            state = JsonUtility.FromJson<SkirmishBreakthroughState>(JsonUtility.ToJson(state));
            var unlock = Facts(.1, 8); unlock.CorridorB.PlayerEligibleInfantry = 1;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, unlock)); Assert.IsTrue(state.ExitOpen);
            var dwell = Facts(2.9, 8); SkirmishBreakthroughReducer.Step(ref state, dwell);
            var restored = JsonUtility.FromJson<SkirmishBreakthroughState>(JsonUtility.ToJson(state));
            var final = Facts(.1, 8); SkirmishBreakthroughReducer.Step(ref state, final); SkirmishBreakthroughReducer.Step(ref restored, final);
            Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(restored)); Assert.AreEqual(SkirmishOutcomeKind.Victory, restored.Outcome);
        }

        [Test]
        public void EighthEvacuationOnDeadlineWinsOtherwiseDeadlineLoses()
        {
            var state = New(true); var almost = Facts(2.9, 8); SkirmishBreakthroughReducer.Step(ref state, almost);
            state.ElapsedTicks = state.DeadlineTicks - T(.1); var final = Facts(.1, 8);
            SkirmishBreakthroughReducer.Step(ref state, final);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.BreakthroughEvacuated, state.Reason);
            state = New(true); state.ElapsedTicks = state.DeadlineTicks - T(.1); final = Facts(.1, 7);
            SkirmishBreakthroughReducer.Step(ref state, final);
            Assert.AreEqual(SkirmishOutcomeKind.Defeat, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.ObjectiveDeadline, state.Reason);
        }

        [Test]
        public void PauseSurrenderInvalidFactsAndOverflowPreserveFrozenOrUnchangedState()
        {
            var state = New(); string before = JsonUtility.ToJson(state); var paused = Facts(100, 12); paused.Paused = true;
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, paused)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            var invalid = Facts(1); invalid.OriginalSubjects[0].AuthoritativeLifeKnown = false;
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            invalid = Facts(1); invalid.SimulationDeltaTicks = -1;
            Assert.Throws<ArgumentException>(() => SkirmishBreakthroughReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            state.ElapsedTicks = long.MaxValue; before = JsonUtility.ToJson(state); var overflow = Facts(1);
            Assert.Throws<OverflowException>(() => SkirmishBreakthroughReducer.Step(ref state, overflow)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            state = New(); Assert.IsFalse(SkirmishBreakthroughReducer.TrySurrender(ref state, false));
            Assert.IsTrue(SkirmishBreakthroughReducer.TrySurrender(ref state, true)); before = JsonUtility.ToJson(state);
            Assert.IsFalse(SkirmishBreakthroughReducer.TrySurrender(ref state, true));
            Assert.IsEmpty(SkirmishBreakthroughReducer.Step(ref state, overflow)); Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishBreakthroughReducerTests();
            Action[] tests = { suite.ExactlyTwelveOriginalIdsAndSizeDeadlinesAreRequired,
                suite.EitherCorridorCanOpenWithOrdinarySupportInfantry, suite.HoldingBothCorridorsDoesNotReduceEightRequired,
                suite.ContestOrEmptyCorridorResetsEvenOnFinalHoldTick, suite.ExitLatchSurvivesLosingBothCorridors,
                suite.ExactUnlockTickNeverCreditsRetroactiveExitDwell, suite.TransportedFlyoverCannotCountUntilDismountedThreeSeconds,
                suite.LeavingOrBoardingResetsIndividualUninterruptedDwell, suite.DeathOnThirdSecondPreventsCreditAndRepeatedCarrierDeathDoesNotDuplicate,
                suite.FourDeathsRemainWinnableFifthMakesVictoryImpossible, suite.ReplacementRecruitAndDuplicateFactsCannotReplaceDesignatedIdentity,
                suite.EvacuatedTombstonesSurviveCheckpointWithoutDuplicateTransition, suite.CheckpointPreservesNineteenPointNineHoldAndTwoPointNineDwell,
                suite.EighthEvacuationOnDeadlineWinsOtherwiseDeadlineLoses, suite.PauseSurrenderInvalidFactsAndOverflowPreserveFrozenOrUnchangedState };
            try { foreach (var test in tests) test(); Debug.Log("[SkirmishBreakthroughReducerTests] result=Passed tests=15"); }
            catch (Exception error) { Debug.LogError("[SkirmishBreakthroughReducerTests] result=Failed\n" + error); throw; }
        }
    }
}
