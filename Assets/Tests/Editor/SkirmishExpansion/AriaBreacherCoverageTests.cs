using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3 coverage: one breacher squad after the first assault order, and breachers as the
    // structure-force replacement when every rocketeer/tank is gone (155923 fa-IR q1 stall).
    public sealed class AriaBreacherCoverageTests
    {
        static AriaBreacherView View(float time) => new AriaBreacherView
        {
            AssaultIssued = true, EnemyAlive = true, CanAffordBreacher = true, RecruitAvailable = true, Time = time
        };

        [Test]
        public void OneBreacherSquadAfterTheFirstAssaultOrder()
        {
            var plan = default(AriaBreacherState);
            var view = View(40f); view.AssaultIssued = false;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan), "never before the first assault");
            view.AssaultIssued = true;
            Assert.AreEqual(AriaBreacherAction.Recruit, AriaBreacherPolicy.Step(view, ref plan), "Build button");
            Assert.AreEqual(1, plan.Stage);
            view.Time = 42f;
            Assert.AreEqual(AriaBreacherAction.Recruit, AriaBreacherPolicy.Step(view, ref plan), "catalog card inside the drawer");
            view.BreacherPending = true; view.Time = 43f;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan));
            Assert.AreEqual(2, plan.Stage);
            view.BreacherPending = false; view.Time = 200f;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan), "once per match");
        }

        [Test]
        public void BreacherYieldsWaitsForOtherRecruitsAndIsBounded()
        {
            var plan = default(AriaBreacherState);
            var view = View(50f); view.Busy = true;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan));
            view.Busy = false; view.RifleRecruitPending = true;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan), "no second recruit in flight");
            view.RifleRecruitPending = false; view.CanAffordBreacher = false;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan), "only when affordable");
            Assert.AreEqual(0, plan.Stage);

            view.CanAffordBreacher = true;
            AriaBreacherPolicy.Step(view, ref plan);
            view.RecruitAvailable = false; view.Time = 51f;
            Assert.AreEqual(AriaBreacherAction.Waiting, AriaBreacherPolicy.Step(view, ref plan), "drawer card not yet presented");
            view.Time = 50f + AriaBreacherPolicy.BuySeconds + .1f;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan));
            Assert.AreEqual(2, plan.Stage, "a purchase that never appears is not retried");

            var fielded = default(AriaBreacherState);
            var already = View(60f); already.BreacherFielded = true;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(already, ref fielded));
            Assert.AreEqual(2, fielded.Stage, "a fielded breacher already covers the type");
        }

        [Test]
        public void LostStructureForceIsRebuiltWithBreachersFirst()
        {
            Assert.AreEqual(AriaStructureReplacement.RecruitBreacher,
                AriaStructureReplacementPolicy.Choose(false, false, true, true, true, true));
            Assert.AreEqual(AriaStructureReplacement.RecruitRocketeer,
                AriaStructureReplacementPolicy.Choose(false, false, false, true, true, true), "rocketeer fallback");
            Assert.AreEqual(AriaStructureReplacement.Wait,
                AriaStructureReplacementPolicy.Choose(true, false, true, true, true, true));
            Assert.AreEqual(AriaStructureReplacement.Wait,
                AriaStructureReplacementPolicy.Choose(false, true, true, true, true, true));
            Assert.AreEqual(AriaStructureReplacement.None,
                AriaStructureReplacementPolicy.Choose(false, false, false, false, false, true));
        }
    }
}
