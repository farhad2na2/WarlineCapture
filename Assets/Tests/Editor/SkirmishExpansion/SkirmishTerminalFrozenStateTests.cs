#if UNITY_EDITOR
using Game.Configs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Pure tests for the terminal probe's frozen-state comparison (codex 030: S002 155923 0 -> -1 base HP).</summary>
    public sealed class SkirmishTerminalFrozenStateTests
    {
        private static SkirmishTerminalFrozenState.Sample Initial() => new()
        {
            Found = true, Session = "7:1", Phase = "Finished", Outcome = "Victory", Attempt = "attempt-1", Elapsed = 233.259f,
            PlayerBaseHp = 800, EnemyBaseHp = 0, SimulationActive = false, ClockPlaying = 0, ClockPaused = 1
        };

        private static string[] Run(SkirmishTerminalFrozenState.Sample delayed, bool attemptMatches = true) =>
            SkirmishTerminalFrozenState.Violations(Initial(), delayed, attemptMatches).ToArray();

        [Test] public void IdenticalSampleIsFrozen() => CollectionAssert.IsEmpty(Run(Initial()));

        [Test]
        public void DestroyedBaseRemovedByCleanupIsStillFrozen()
        {
            var delayed = Initial();
            delayed.EnemyBaseHp = -1;
            CollectionAssert.IsEmpty(Run(delayed));
        }

        [Test]
        public void BaseHpRules()
        {
            Assert.IsTrue(SkirmishTerminalFrozenState.BaseHpFrozen(0, -1));
            Assert.IsTrue(SkirmishTerminalFrozenState.BaseHpFrozen(-1, -1));
            Assert.IsTrue(SkirmishTerminalFrozenState.BaseHpFrozen(0, 0));
            Assert.IsTrue(SkirmishTerminalFrozenState.BaseHpFrozen(800, 800));
            Assert.IsFalse(SkirmishTerminalFrozenState.BaseHpFrozen(800, -1), "a live base that vanishes is a change");
            Assert.IsFalse(SkirmishTerminalFrozenState.BaseHpFrozen(800, 799));
            Assert.IsFalse(SkirmishTerminalFrozenState.BaseHpFrozen(0, 50), "a destroyed base must not come back");
            Assert.IsFalse(SkirmishTerminalFrozenState.BaseHpFrozen(-1, 50));
        }

        [Test]
        public void LivePlayerBaseDisappearingStillFails()
        {
            var delayed = Initial();
            delayed.PlayerBaseHp = -1;
            CollectionAssert.AreEqual(new[] { "playerBaseHp" }, Run(delayed));
        }

        [Test]
        public void EveryOtherPredicateIsUnchanged()
        {
            var d = Initial(); d.Found = false;
            CollectionAssert.AreEqual(new[] { "match" }, Run(d));
            d = Initial(); d.Session = "8:1";
            CollectionAssert.AreEqual(new[] { "session" }, Run(d));
            d = Initial(); d.Phase = "Playing";
            CollectionAssert.AreEqual(new[] { "phase" }, Run(d));
            d = Initial(); d.Outcome = "Defeat";
            CollectionAssert.AreEqual(new[] { "outcome" }, Run(d));
            CollectionAssert.AreEqual(new[] { "attempt" }, Run(Initial(), false));
            d = Initial(); d.Elapsed += .06f;
            CollectionAssert.AreEqual(new[] { "elapsedDrift" }, Run(d));
            d = Initial(); d.Elapsed += .04f;
            CollectionAssert.IsEmpty(Run(d));
            d = Initial(); d.SimulationActive = true;
            CollectionAssert.AreEqual(new[] { "simulationActive" }, Run(d));
        }

        [Test]
        public void ClockFlagsAreSeparateAndUnchanged()
        {
            var d = Initial();
            Assert.IsTrue(SkirmishTerminalFrozenState.ClockFrozen(d));
            d.ClockPlaying = 1;
            Assert.IsFalse(SkirmishTerminalFrozenState.ClockFrozen(d));
            d.ClockPlaying = 0; d.ClockPaused = 0;
            Assert.IsFalse(SkirmishTerminalFrozenState.ClockFrozen(d));
            d.ClockPlaying = -1;
            Assert.IsTrue(SkirmishTerminalFrozenState.ClockFrozen(d), "no clock component: nothing to check");
            CollectionAssert.IsEmpty(Run(Initial()), "clock flags never add a terminalNotFrozen violation");
        }

        [Test]
        public void LogLineShowsInitialAndDelayedForEveryPredicate()
        {
            var delayed = Initial();
            delayed.EnemyBaseHp = -1;
            var line = SkirmishTerminalFrozenState.Format(Initial(), delayed, Run(delayed), true);
            StringAssert.StartsWith("[SkirmishTerminalFrozen] result=Passed violations=none clockFrozen=1", line);
            foreach (var key in new[] { "session=", "phase=", "outcome=", "attempt=", "elapsed=", "playerBaseHp=", "enemyBaseHp=", "simActive=", "clockPlaying=", "clockPaused=" })
                Assert.AreEqual(2, System.Text.RegularExpressions.Regex.Matches(line, key).Count, key);
            StringAssert.Contains("enemyBaseHp=0", line);
            StringAssert.Contains("enemyBaseHp=-1", line);
            delayed.Phase = "Playing";
            var failed = SkirmishTerminalFrozenState.Format(Initial(), delayed, Run(delayed), true);
            StringAssert.StartsWith("[SkirmishTerminalFrozen] result=Failed violations=phase", failed);
        }
    }
}
#endif
