using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3 (lead decision 2026-11-17): readiness research once per stage when it unlocks something the
    // mission offers and ARIA has not used (siege, S049's Established kinds), and producer buildings the
    // mission offers that the player lacks (S049: helipad, satellite dish), once each. Pure policy checks.
    public sealed class AriaResearchBuildingTests
    {
        static AriaResearchView Research(float time) => new AriaResearchView
        {
            AssaultSeen = true, EnemyAlive = true, CoverageIdle = true, CanUpgrade = true, UpgradeAvailable = true,
            Stage = 2, Cost = 480, Materials = 800, Reserve = AriaResearchPolicy.Reserve, NeedMask = 256, CompletedContacts = 50, Time = time
        };

        [Test]
        public void ResearchStartsOnlyWhenItUnlocksSomethingAndMaterialsAllow()
        {
            var plan = default(AriaResearchState);
            var view = Research(200f); view.NeedMask = 0;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "nothing to unlock");
            view = Research(200f); view.Materials = 480 + AriaResearchPolicy.Reserve - 1;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "keeps the reserve");
            view = Research(200f); view.CoverageIdle = false;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "after the coverage purchases");
            view = Research(200f); view.AssaultSeen = false;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "never before the first assault");
            view = Research(200f); view.Stage = AriaResearchPolicy.MaxStage;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "Full Arsenal is the last stage");

            view = Research(200f);
            Assert.IsTrue(AriaResearchPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaResearchAction.Upgrade, d.Action);
            Assert.AreEqual(1, d.Trace);
            view.Time = 202f; view.CompletedContacts = 51; // Build tapped; the readiness button is next
            Assert.AreEqual(AriaResearchAction.Upgrade, (AriaResearchPolicy.Step(view, ref plan, out d), d.Action).Item2);
            view.InProgress = true; view.Time = 204f;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "research queued");
            view.InProgress = false; view.Stage = 2; view.Time = 230f;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _), "once per stage");
        }

        [Test]
        public void ResearchYieldsAndIsBoundedPerStage()
        {
            var plan = default(AriaResearchState);
            var view = Research(200f);
            AriaResearchPolicy.Step(view, ref plan, out _);
            view.Busy = true; view.Time = 201f; // a home raid
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(3, d.Trace, "released for home defence");
            view.Busy = false; view.Time = 230f;
            Assert.IsTrue(AriaResearchPolicy.Step(view, ref plan, out _), "second attempt");
            view.Time = 230f + AriaResearchPolicy.TapSeconds + 1f;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(4, d.Trace, "given up after two attempts");
            view.Time = 300f;
            Assert.IsFalse(AriaResearchPolicy.Step(view, ref plan, out _));
        }

        static AriaBuildingView Building(float time) => new AriaBuildingView
        {
            AssaultSeen = true, EnemyAlive = true, CoverageIdle = true, ResearchIdle = true,
            NeedMask = 3, AffordMask = 3, CardMask = 3, Time = time
        };

        [Test]
        public void HelipadThenDishAreBuiltOnceEach()
        {
            var plan = default(AriaBuildingState);
            var view = Building(150f);
            Assert.IsTrue(AriaBuildingPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaBuildingAction.Start, d.Action);
            Assert.AreEqual(1, d.Kind, "helipad first");
            view.Time = 160f; view.PlacementOpen = true;
            Assert.AreEqual(AriaBuildingAction.Continue, (AriaBuildingPolicy.Step(view, ref plan, out d), d.Action).Item2);
            view.PlacementOpen = false; view.PresentMask = 1; view.Time = 165f;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "built");
            view.Time = 166f;
            Assert.IsTrue(AriaBuildingPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Kind, "then the satellite dish");
            view.PresentMask = 3; view.Time = 180f;
            AriaBuildingPolicy.Step(view, ref plan, out _);
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out _), "each once");
        }

        [Test]
        public void NoLegalSiteGivesUpCleanlyAndIsBounded()
        {
            var plan = default(AriaBuildingState);
            var view = Building(150f); view.NeedMask = 2; view.AffordMask = 2; view.CardMask = 2;
            AriaBuildingPolicy.Step(view, ref plan, out _);
            view.PlacementOpen = true; view.PlacementGaveUp = true; view.Time = 170f; // no legal pad site offered
            Assert.IsTrue(AriaBuildingPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaBuildingAction.Cancel, d.Action, "the preview is cancelled, never left open");
            view.PlacementOpen = false; view.Time = 171f;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(3, d.Trace, "released");
            view.PlacementGaveUp = false; view.Time = 172f;
            Assert.IsTrue(AriaBuildingPolicy.Step(view, ref plan, out _), "second attempt");
            view.PlacementGaveUp = true; view.Time = 180f;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(4, d.Trace, "given up");
            view.PlacementGaveUp = false; view.Time = 200f;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out _));

            plan = default;
            view = Building(150f); view.Busy = true;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out _), "yields to home defence and other owners");
            view = Building(150f); view.CoverageIdle = false;
            Assert.IsFalse(AriaBuildingPolicy.Step(view, ref plan, out _), "one purchase at a time");
        }

        [Test]
        public void CoverageIsIdleOnlyWhenNothingPossibleIsLeft()
        {
            Assert.IsTrue(AriaCoverageProgress.Idle(done: 7, current: 0, affordMask: 7, pendingMask: 0));
            Assert.IsFalse(AriaCoverageProgress.Idle(done: 3, current: 0, affordMask: 7, pendingMask: 0), "radar still affordable");
            Assert.IsFalse(AriaCoverageProgress.Idle(done: 7, current: 3, affordMask: 7, pendingMask: 0));
            Assert.IsFalse(AriaCoverageProgress.Idle(done: 7, current: 0, affordMask: 7, pendingMask: 4));
            Assert.IsTrue(AriaCoverageProgress.Idle(done: 7, current: 0, affordMask: 0, pendingMask: 0), "unaffordable kinds do not hold research back");
        }
    }
}
