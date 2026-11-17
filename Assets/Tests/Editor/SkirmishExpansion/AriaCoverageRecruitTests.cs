using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3: after the breacher, one marksman squad, one apc_fast and one radar are bought
    // one at a time through the visible drawer; the radar then gets one ordinary order.
    public sealed class AriaCoverageRecruitTests
    {
        static AriaCoverageView Ready(float time) => new AriaCoverageView
        {
            AssaultIssued = true, BreacherSettled = true, EnemyAlive = true,
            AffordMask = AriaCoverageRecruitPolicy.All, CardMask = AriaCoverageRecruitPolicy.All, Time = time
        };

        [Test]
        public void BuysMarksmanThenApcFastThenRadarOneAtATime()
        {
            var plan = default(AriaCoverageState);
            var view = Ready(80f);
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind);
            view.CardMask = 0; view.Time = 81f; // drawer opening: the card is not offered yet
            Assert.AreEqual(AriaCoverageAction.Waiting, AriaCoverageRecruitPolicy.Step(view, ref plan, out _));

            view.CardMask = AriaCoverageRecruitPolicy.All; view.PendingMask = 1; view.Time = 83f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "the marksman purchase landed");
            Assert.AreEqual(0, plan.Current);
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "one recruit at a time");

            view.PendingMask = 0; view.FieldedMask = 1; view.Time = 90f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.ApcFast, kind);
            view.PendingMask = 2; view.Time = 92f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            view.PendingMask = 0; view.FieldedMask = 3; view.Time = 100f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Radar, kind);
            view.FieldedMask = 7; view.Time = 110f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(AriaCoverageRecruitPolicy.All, plan.Done);
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "each kind once per match");
        }

        [Test]
        public void WaitsForTheAssaultTheBreacherAndOtherPurchases()
        {
            var plan = default(AriaCoverageState);
            var view = Ready(40f); view.AssaultIssued = false;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "never before the first assault");
            view = Ready(60f); view.BreacherSettled = false;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "the breacher goes first");
            view = Ready(70f); view.OtherRecruitPending = true;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "a queued rifle/rocketeer/breacher goes first");
            view = Ready(70f); view.Busy = true;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "home defence and other owners first");
            Assert.AreEqual(0, plan.Current);
            Assert.AreEqual(0, plan.Done);
        }

        [Test]
        public void UnaffordableKindWaitsAndTimeoutGivesUp()
        {
            var plan = default(AriaCoverageState);
            var view = Ready(80f); view.AffordMask = 2 | 4; // no materials for the marksman yet
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.ApcFast, kind);
            view.CardMask = 0; view.Time = 80f + AriaCoverageRecruitPolicy.BuySeconds + 1f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "bounded: no endless drawer taps");
            Assert.AreEqual(2, plan.Done, "apc_fast is not retried this match");
            view.CardMask = AriaCoverageRecruitPolicy.All; view.AffordMask = AriaCoverageRecruitPolicy.All; view.Time = 95f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind, "the marksman is bought once affordable");
            view.Busy = true; view.Time = 96f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "a home threat abandons the purchase");
            Assert.AreEqual(3, plan.Done);
        }

        static AriaRadarView Radar(float time) => new AriaRadarView
        {
            RadarFielded = true, ArmyMoving = true, EnemyAlive = true, ToggleAvailable = true, AttackAvailable = true,
            RearAvailable = true, RadarMask = 0b00100, AvailableSquadMask = 0b11111, Time = time
        };

        [Test]
        public void RadarCardAttackThenRearGroundOnce()
        {
            var plan = default(AriaRadarState);
            var view = Radar(120f);
            Assert.IsTrue(AriaRadarOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(1, d.Trace);
            Assert.AreEqual(AriaRadarAction.SelectSlot, d.Action);
            Assert.AreEqual(2, d.Slot);
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 122f;
            Assert.IsTrue(AriaRadarOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaRadarAction.ArmAttack, d.Action);
            view.AttackMode = true; view.CompletedContacts = 2; view.Time = 124f;
            Assert.IsTrue(AriaRadarOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaRadarAction.TargetRear, d.Action);
            view.RearAvailable = false; view.Time = 125f;
            Assert.AreEqual(AriaRadarAction.Waiting, (AriaRadarOrderPolicy.Step(view, ref plan, out d), d.Action).Item2, "off-screen rear ground: wait, never tap blind");
            view.RearAvailable = true; view.AttackMode = false; view.CompletedContacts = 3; view.Time = 127f;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "ordered");
            Assert.AreEqual(4, plan.Stage);
            view.Time = 200f; view.SelectedGroups = 0; view.SelectedMask = 0;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "one order per match");
        }

        [Test]
        public void RadarYieldsToSelectionsOwnersAndPagingAndIsBounded()
        {
            var plan = default(AriaRadarState);
            var view = Radar(120f); view.SelectedGroups = 2; view.SelectedMask = 3;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "never cuts into a roster sweep selection");
            view = Radar(120f); view.Busy = true;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "home defence and other owners first");
            view = Radar(120f); view.RadarMask = 0;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "the radar card is on another page");
            view = Radar(120f); view.ArmyMoving = false;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "only after the column has its order");
            Assert.AreEqual(0, plan.Stage);

            view = Radar(120f);
            Assert.IsTrue(AriaRadarOrderPolicy.Step(view, ref plan, out _));
            view.RadarMask = 0; view.Time = 121f; // the sweep paged away before the card was tapped
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(3, d.Trace);
            Assert.AreEqual(0, plan.Stage);
            view = Radar(121.5f);
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out _), "cooldown after a release");
            view.Time = plan.ReadyAt + .1f;
            Assert.IsTrue(AriaRadarOrderPolicy.Step(view, ref plan, out _));
            view.Time += AriaRadarOrderPolicy.OrderSeconds + 1f;
            Assert.IsFalse(AriaRadarOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(4, d.Trace, "given up after MaxAttempts");
            Assert.AreEqual(4, plan.Stage);
        }
    }
}
