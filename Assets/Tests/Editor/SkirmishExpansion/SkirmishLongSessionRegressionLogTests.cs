#if UNITY_EDITOR
using Game.Configs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Verdict and text of the long-session runtime regression marker (gate from 2026-11-17).</summary>
    public sealed class SkirmishLongSessionRegressionLogTests
    {
        private static SkirmishLongSessionRegressionLog.Evidence Clean() => new()
        {
            Catalog = "S026", Seed = 104729, ResultScreen = true, ReturnToMenu = true, Expiry = true,
            Outcome = "Defeat", EndReason = "TimeLimit", Elapsed = 1080.2f, Purchases = 20,
            Archetypes = 4100, QueryShapes = 1720, QueryBlocks = 301, QueryBudgetBlocks = 2048
        };

        [Test]
        public void CleanDeadlineRunPasses()
        {
            var e = Clean();
            Assert.AreEqual("Passed", SkirmishLongSessionRegressionLog.Verdict(e));
            Assert.AreEqual("[SkirmishLongSessionRegression] catalog=S026 seed=104729 result=Passed allocatorErrors=0 exceptions=0 errors=0 " +
                "resultScreen=1 returnToMenu=1 expiry=1 outcome=Defeat endReason=TimeLimit elapsed=1080.2 purchases=20 purchaseFailures=0 " +
                "archetypes=4100 queryShapes=1720 queryBlocks=301/2048 abort=none firstError=none", SkirmishLongSessionRegressionLog.Format(e));
        }

        [Test]
        public void AllocatorErrorExceptionOrMissingTerminalFails()
        {
            var e = Clean(); e.AllocatorErrors = 1;
            Assert.AreEqual("Failed", SkirmishLongSessionRegressionLog.Verdict(e));
            e = Clean(); e.Exceptions = 1;
            Assert.AreEqual("Failed", SkirmishLongSessionRegressionLog.Verdict(e));
            e = Clean(); e.ResultScreen = false;
            Assert.AreEqual("Failed", SkirmishLongSessionRegressionLog.Verdict(e));
            e = Clean(); e.ReturnToMenu = false;
            Assert.AreEqual("Failed", SkirmishLongSessionRegressionLog.Verdict(e));
        }

        [Test]
        public void EarlyEndIsInconclusiveOnlyAfterTenMinutes()
        {
            var e = Clean(); e.Expiry = false; e.EndReason = "MainBaseDestroyed";
            Assert.AreEqual("Inconclusive", SkirmishLongSessionRegressionLog.Verdict(e));
            e.Elapsed = 300f;
            Assert.AreEqual("Failed", SkirmishLongSessionRegressionLog.Verdict(e));
        }

        [Test]
        public void ExpiryReasonsAndAllocatorText()
        {
            Assert.IsTrue(SkirmishLongSessionRegressionLog.IsExpiry("TimeLimit"));
            Assert.IsTrue(SkirmishLongSessionRegressionLog.IsExpiry("ObjectiveDeadline"));
            Assert.IsFalse(SkirmishLongSessionRegressionLog.IsExpiry("MainBaseDestroyed"));
            Assert.IsTrue(SkirmishLongSessionRegressionLog.IsAllocatorError("ArgumentException: Cannot exceed budget of 16777216 in BlockAllocator."));
            Assert.IsFalse(SkirmishLongSessionRegressionLog.IsAllocatorError("NullReferenceException"));
            Assert.AreEqual("Exception:A_b", SkirmishLongSessionRegressionLog.Sanitize("Exception:A b\n  at X"));
        }
    }
}
#endif
