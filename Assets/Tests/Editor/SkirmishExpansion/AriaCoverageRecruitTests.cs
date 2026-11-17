using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3: after the breacher, one marksman squad, one apc_fast and one radar are bought
    // one at a time through the visible drawer; the radar then gets one ordinary order.
    public sealed class AriaCoverageRecruitTests
    {
        // S002's catalog offers marksman, apc_fast and radar (bits 1/2/4); apc_heavy, drone and transport heli
        // are not in it, so they are never affordable there.
        static AriaCoverageView Ready(float time) => new AriaCoverageView
        {
            AssaultIssued = true, BreacherSettled = true, EnemyAlive = true,
            AffordMask = 7, CardMask = AriaCoverageRecruitPolicy.All, Time = time
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
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "purchases are spaced (GapSeconds)");
            view.Time = 83f + AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.ApcFast, kind);
            view.PendingMask = 2; view.Time += 2f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            view.PendingMask = 0; view.FieldedMask = 3; view.Time += AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Radar, kind);
            view.FieldedMask = 7; view.Time += 10f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(7, plan.Done);
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
            view.CardMask = AriaCoverageRecruitPolicy.All; view.AffordMask = 7; view.Time = 95f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "spaced after the given-up purchase");
            view.Time = 91f + AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind, "the marksman is bought once affordable");
            view.Busy = true; view.Time += 1f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "a home threat abandons the purchase");
            Assert.AreEqual(3, plan.Done);
        }

        [Test]
        public void S026KindsFollowAfterTheS002KindsOneAtATime()
        {
            var plan = new AriaCoverageState { Done = 7 }; // marksman, apc_fast, radar covered
            // S026 starts with a gunner and a tank (bits 64, 128).
            var view = Ready(120f); view.AffordMask = AriaCoverageRecruitPolicy.All; view.FieldedMask = 7 | 64 | 128;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.ApcHeavy, kind);
            view.PendingMask = 8; view.Time = 122f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "one recruit at a time");
            view.PendingMask = 0; view.FieldedMask = 15 | 64 | 128; view.Time = 122f + AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Drone, kind);
            view.FieldedMask = 31 | 64 | 128; view.Time += 10f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            view.Time += AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.TransportHeli, kind);
        }

        static AriaSupportView Support(float time, AriaSupportKind kind = AriaSupportKind.Radar) => new AriaSupportView
        {
            FieldedMask = AriaSupportOrderPolicy.KindBit(kind), ArmyMoving = true, EnemyAlive = true, ToggleAvailable = true,
            MoveAvailable = true, AttackAvailable = true, ScanAvailable = true, RearAvailable = true, FrontAvailable = true,
            SupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.None, AriaSupportKind.None, kind, AriaSupportKind.None, AriaSupportKind.None),
            AvailableSquadMask = 0b11111, Time = time
        };

        [Test]
        public void RadarCardMoveThenRearGroundOnce()
        {
            var plan = default(AriaSupportState);
            var view = Support(120f);
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(1, d.Trace);
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action);
            Assert.AreEqual(2, d.Slot);
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 122f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "an unarmed radar gets an ordinary Move");
            view.Mode = AriaSupportMode.Move; view.CompletedContacts = 2; view.Time = 124f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.TargetRear, d.Action);
            view.RearAvailable = false; view.Time = 125f;
            AriaSupportOrderPolicy.Step(view, ref plan, out d);
            Assert.AreEqual(AriaSupportAction.Waiting, d.Action, "off-screen rear ground: wait, never tap blind");
            view.RearAvailable = true; view.Mode = AriaSupportMode.None; view.CompletedContacts = 3; view.Time = 127f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "ordered");
            Assert.AreEqual(1, plan.Done);
            view.Time = 200f; view.SelectedGroups = 0; view.SelectedMask = 0;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "one order per kind per match");
        }

        [Test]
        public void MoveFallsBackToAttackAndDroneScansAhead()
        {
            var plan = default(AriaSupportState);
            var view = Support(120f, AriaSupportKind.TransportHeli); view.MoveAvailable = false;
            view.SelectedMask = 0b00100; view.SelectedGroups = 1;
            // A selection is visible: the step never starts on top of it.
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _));
            view.SelectedMask = 0; view.SelectedGroups = 0;
            AriaSupportOrderPolicy.Step(view, ref plan, out _);
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 1;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.ArmAttack, d.Action, "no Move button presented: Attack Move");

            plan = default;
            view = Support(150f, AriaSupportKind.Drone);
            AriaSupportOrderPolicy.Step(view, ref plan, out d);
            Assert.AreEqual(AriaSupportKind.Drone, d.Kind);
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 1;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmScan, d.Action);
            view.Mode = AriaSupportMode.Scan; view.CompletedContacts = 2;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.TargetFront, d.Action, "the drone scans ahead of the column");
        }

        [Test]
        public void SupportYieldsToOwnersAndPagingAndIsBoundedPerKind()
        {
            var plan = default(AriaSupportState);
            var view = Support(120f); view.Busy = true;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "home defence and other owners first");
            view = Support(120f); view.SupportSlots = 0;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "the card is on another page");
            view = Support(120f); view.ArmyMoving = false;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "only after the column has its order");
            view = Support(120f); view.Mode = AriaSupportMode.Attack;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "never while a command mode is armed");
            Assert.AreEqual(0, plan.Stage);

            view = Support(120f);
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out _));
            view.SupportSlots = 0; view.Time = 121f; // the sweep paged away before the card was tapped
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(3, d.Trace);
            view = Support(121.5f);
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "cooldown after a release");
            view.Time = plan.ReadyAt + .1f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out _));
            view.Time += AriaSupportOrderPolicy.OrderSeconds + 1f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(4, d.Trace, "given up after the second release of this kind");
            Assert.AreEqual(1, plan.Done);
            view.Time = plan.ReadyAt + 1f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "a given-up kind is not retried");
        }
    }
}
