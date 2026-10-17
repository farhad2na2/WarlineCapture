using System;
using System.Linq;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishConvoyReducerTests
    {
        private static long T(double seconds) => (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
        private static string[] Ids() => new[] { "original.convoy.1", "original.convoy.2", "original.convoy.3" };
        private static SkirmishConvoyState New() => SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "destination", Ids());
        private static SkirmishConvoyTickFacts Facts(double seconds, int atDestination = 0) => new SkirmishConvoyTickFacts
        {
            Playing = true, SimulationDeltaTicks = T(seconds), OriginalTrucks = Ids().Select((id, i) =>
                new SkirmishConvoySubjectFacts { StableOriginalId = id, AuthoritativeLifeKnown = true,
                    Alive = true, InsideDestination = i < atDestination }).ToArray()
        };
        private static int Count(SkirmishConvoyState state, SkirmishConvoySubjectStatus status) =>
            state.OriginalTrucks.Count(truck => truck.Status == status);

        [Test]
        public void ExactlyThreeDistinctOriginalIdsAndSizeDeadlinesAreRequired()
        {
            var state = New(); Assert.AreEqual(3, state.OriginalTrucks.Length); Assert.AreEqual(T(1080), state.DeadlineTicks);
            Assert.AreEqual(T(1500), SkirmishConvoyReducer.Create(SkirmishSizeId.War, "exit", Ids()).DeadlineTicks);
            Assert.AreEqual(T(1800), SkirmishConvoyReducer.Create(SkirmishSizeId.LargeWar, "exit", Ids()).DeadlineTicks);
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "exit", Ids().Take(2).ToArray()));
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "exit", Ids().Concat(new[] { "extra" }).ToArray()));
            var ids = Ids(); ids[2] = ids[0];
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "exit", ids));
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "", Ids()));
            Assert.Throws<ArgumentOutOfRangeException>(() => SkirmishConvoyReducer.Create((SkirmishSizeId)255, "exit", Ids()));
            ids = Ids(); state = SkirmishConvoyReducer.Create(SkirmishSizeId.Standard, "exit", ids);
            ids[0] = "mutated"; Assert.AreEqual("original.convoy.1", state.OriginalTrucks[0].StableOriginalId);
        }

        [Test]
        public void FirstDeliveredIsNonterminalSecondWinsAndThirdMayDeliverSameTick()
        {
            var state = New(); var first = Facts(5, 1);
            Assert.AreEqual(1, SkirmishConvoyReducer.Step(ref state, first).Length);
            Assert.AreEqual(1, Count(state, SkirmishConvoySubjectStatus.Delivered)); Assert.IsFalse(state.Terminal);
            var next = Facts(5, 3); Assert.AreEqual(2, SkirmishConvoyReducer.Step(ref state, next).Length);
            Assert.AreEqual(3, Count(state, SkirmishConvoySubjectStatus.Delivered));
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.ConvoyDelivered, state.Reason);
        }

        [Test]
        public void TwoDeathsBeforeDepartureMakeVictoryImpossible()
        {
            var state = New(); var killed = Facts(0); killed.OriginalTrucks[0].Alive = killed.OriginalTrucks[1].Alive = false;
            Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, killed));
            Assert.AreEqual(2, Count(state, SkirmishConvoySubjectStatus.Dead));
            Assert.AreEqual(SkirmishOutcomeKind.Defeat, state.Outcome); Assert.AreEqual(SkirmishEndReasonKind.ConvoyImpossible, state.Reason);
        }

        [Test]
        public void OneDeliveredOneLiveOneDeadRemainsWinnable()
        {
            var state = New(); var first = Facts(5, 1); first.OriginalTrucks[2].Alive = false;
            SkirmishConvoyReducer.Step(ref state, first); Assert.IsFalse(state.Terminal);
            Assert.AreEqual(1, Count(state, SkirmishConvoySubjectStatus.Alive));
            var second = Facts(5, 2); second.OriginalTrucks[2].Alive = false;
            Assert.AreEqual(1, SkirmishConvoyReducer.Step(ref state, second).Length); Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome);
        }

        [Test]
        public void DeathOnFifthSecondPreventsCredit()
        {
            var state = New(); var almost = Facts(4.9, 2); SkirmishConvoyReducer.Step(ref state, almost);
            var killed = Facts(.1, 2); killed.OriginalTrucks[1].Alive = false;
            var delivered = SkirmishConvoyReducer.Step(ref state, killed);
            CollectionAssert.AreEqual(new[] { Ids()[0] }, delivered);
            Assert.AreEqual(SkirmishConvoySubjectStatus.Dead, state.OriginalTrucks[1].Status);
            Assert.IsFalse(state.Terminal); Assert.AreEqual(0, state.OriginalTrucks[1].DestinationDwellTicks);
        }

        [Test]
        public void ExitOnFifthSecondResetsOnlyThatTruck()
        {
            var state = New(); var almost = Facts(4.9, 2); SkirmishConvoyReducer.Step(ref state, almost);
            var leave = Facts(.1, 1); Assert.AreEqual(1, SkirmishConvoyReducer.Step(ref state, leave).Length);
            Assert.AreEqual(0, state.OriginalTrucks[1].DestinationDwellTicks);
            var back = Facts(4.9, 2); Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, back));
            var final = Facts(.1, 2); Assert.AreEqual(1, SkirmishConvoyReducer.Step(ref state, final).Length);
            Assert.AreEqual(SkirmishOutcomeKind.Victory, state.Outcome);
        }

        [Test]
        public void LogisticsReplacementCannotSubstituteOriginalIdentity()
        {
            var state = New(); var replacement = Facts(5, 3); replacement.OriginalTrucks[0].StableOriginalId = "ordinary.logistics.replacement";
            string before = JsonUtility.ToJson(state);
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, replacement));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        [Test]
        public void DuplicateOrMissingObservationFailsWithoutMutation()
        {
            var state = New(); string before = JsonUtility.ToJson(state); var duplicate = Facts(5, 3);
            duplicate.OriginalTrucks[1].StableOriginalId = duplicate.OriginalTrucks[0].StableOriginalId;
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, duplicate)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            duplicate = Facts(5, 3); duplicate.OriginalTrucks = duplicate.OriginalTrucks.Take(2).ToArray();
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, duplicate)); Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        [Test]
        public void DeliveredTombstoneSurvivesRemovedActorAndRepeatedDelivery()
        {
            var state = New(); var first = Facts(5, 1); Assert.AreEqual(1, SkirmishConvoyReducer.Step(ref state, first).Length);
            state = JsonUtility.FromJson<SkirmishConvoyState>(JsonUtility.ToJson(state));
            var absentActor = Facts(0); absentActor.OriginalTrucks[0].Alive = false; absentActor.OriginalTrucks[0].AuthoritativeLifeKnown = false;
            Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, absentActor));
            Assert.AreEqual(1, Count(state, SkirmishConvoySubjectStatus.Delivered)); Assert.AreEqual(0, Count(state, SkirmishConvoySubjectStatus.Dead));
            Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, first)); Assert.IsFalse(state.Terminal);
        }

        [Test]
        public void DestroyedTombstoneCannotResurrectOrDeliver()
        {
            var state = New(); var death = Facts(0); death.OriginalTrucks[0].Alive = false;
            SkirmishConvoyReducer.Step(ref state, death); Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, death));
            var resurrect = Facts(5, 1); Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, resurrect));
            Assert.AreEqual(1, Count(state, SkirmishConvoySubjectStatus.Dead)); Assert.AreEqual(0, Count(state, SkirmishConvoySubjectStatus.Delivered));
        }

        [Test]
        public void FourPointNineSecondCheckpointPreservesFractionAndStableIds()
        {
            var state = New(); var almost = Facts(4.9, 2); SkirmishConvoyReducer.Step(ref state, almost);
            var restored = JsonUtility.FromJson<SkirmishConvoyState>(JsonUtility.ToJson(state));
            Assert.AreEqual(T(4.9), restored.OriginalTrucks[1].DestinationDwellTicks);
            var final = Facts(.1, 2); SkirmishConvoyReducer.Step(ref state, final); SkirmishConvoyReducer.Step(ref restored, final);
            Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(restored)); Assert.AreEqual(SkirmishOutcomeKind.Victory, restored.Outcome);
        }

        [Test]
        public void SecondDeliveryOnDeadlineWinsOtherwiseDeadlineLoses()
        {
            var state = New(); var almost = Facts(4.9, 2); SkirmishConvoyReducer.Step(ref state, almost);
            state.ElapsedTicks = state.DeadlineTicks - T(.1); var final = Facts(.1, 2);
            SkirmishConvoyReducer.Step(ref state, final); Assert.AreEqual(SkirmishEndReasonKind.ConvoyDelivered, state.Reason);
            state = New(); state.ElapsedTicks = state.DeadlineTicks - T(.1); final = Facts(.1, 1);
            SkirmishConvoyReducer.Step(ref state, final); Assert.AreEqual(SkirmishOutcomeKind.Defeat, state.Outcome);
            Assert.AreEqual(SkirmishEndReasonKind.ObjectiveDeadline, state.Reason);
        }

        [Test]
        public void PausedOrNotPlayingFreezesDwellAndDeadline()
        {
            var state = New(); string before = JsonUtility.ToJson(state); var tick = Facts(1800, 3); tick.Paused = true;
            Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, tick)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            tick.Paused = false; tick.Playing = false; Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, tick));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        [Test]
        public void InvalidLifeClockDwellAndOverflowDoNotPartiallyCommitAndSurrenderFreezes()
        {
            var state = New(); string before = JsonUtility.ToJson(state); var invalid = Facts(5, 3);
            invalid.OriginalTrucks[2].AuthoritativeLifeKnown = false;
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            invalid = Facts(1); invalid.SimulationDeltaTicks = -1;
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            state.OriginalTrucks[2].DestinationDwellTicks = T(5); before = JsonUtility.ToJson(state); invalid = Facts(5, 3);
            Assert.Throws<ArgumentException>(() => SkirmishConvoyReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            state = New(); state.ElapsedTicks = long.MaxValue; before = JsonUtility.ToJson(state); invalid = Facts(1);
            Assert.Throws<OverflowException>(() => SkirmishConvoyReducer.Step(ref state, invalid)); Assert.AreEqual(before, JsonUtility.ToJson(state));
            state = New(); Assert.IsFalse(SkirmishConvoyReducer.TrySurrender(ref state, false));
            Assert.IsTrue(SkirmishConvoyReducer.TrySurrender(ref state, true)); before = JsonUtility.ToJson(state);
            Assert.IsFalse(SkirmishConvoyReducer.TrySurrender(ref state, true)); Assert.IsEmpty(SkirmishConvoyReducer.Step(ref state, invalid));
            Assert.AreEqual(before, JsonUtility.ToJson(state)); Assert.AreEqual(SkirmishEndReasonKind.Surrender, state.Reason);
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishConvoyReducerTests();
            Action[] tests = { suite.ExactlyThreeDistinctOriginalIdsAndSizeDeadlinesAreRequired,
                suite.FirstDeliveredIsNonterminalSecondWinsAndThirdMayDeliverSameTick, suite.TwoDeathsBeforeDepartureMakeVictoryImpossible,
                suite.OneDeliveredOneLiveOneDeadRemainsWinnable, suite.DeathOnFifthSecondPreventsCredit, suite.ExitOnFifthSecondResetsOnlyThatTruck,
                suite.LogisticsReplacementCannotSubstituteOriginalIdentity, suite.DuplicateOrMissingObservationFailsWithoutMutation,
                suite.DeliveredTombstoneSurvivesRemovedActorAndRepeatedDelivery, suite.DestroyedTombstoneCannotResurrectOrDeliver,
                suite.FourPointNineSecondCheckpointPreservesFractionAndStableIds, suite.SecondDeliveryOnDeadlineWinsOtherwiseDeadlineLoses,
                suite.PausedOrNotPlayingFreezesDwellAndDeadline, suite.InvalidLifeClockDwellAndOverflowDoNotPartiallyCommitAndSurrenderFreezes };
            try { foreach (var test in tests) test(); Debug.Log("[SkirmishConvoyReducerTests] result=Passed tests=14"); }
            catch (Exception error) { Debug.LogError("[SkirmishConvoyReducerTests] result=Failed\n" + error); throw; }
        }
    }
}
