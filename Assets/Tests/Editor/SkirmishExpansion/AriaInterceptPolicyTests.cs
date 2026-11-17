using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Pure policy checks: a visible vehicle slipping behind the advancing column is
    // attacked by one group through the normal card / Attack / tap controls, then the
    // ordinary assault resumes once it is dead or out of sight.
    public sealed class AriaInterceptPolicyTests
    {
        static AriaInterceptView View(float time) => new AriaInterceptView
        {
            ThreatKnown = true, ThreatAvailable = true, ArmyMoving = true, AttackAvailable = true, ToggleAvailable = true,
            AssaultMask = 0b00111, StructureMask = 0b00111, AvailableSquadMask = 0b00111, Time = time
        };

        static AriaInterceptDecision Step(in AriaInterceptView view, ref AriaInterceptState plan, out bool handled)
        {
            handled = AriaInterceptPolicy.Step(view, ref plan, out var decision);
            return decision;
        }

        [Test]
        public void SelectedRocketeersAttackTheSlippingTankThenResume()
        {
            var plan = default(AriaInterceptState);
            var view = View(55f);
            view.SelectedMask = 0b00111; view.SelectedGroups = 3; // just-ordered rocketeer page
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled);
            Assert.AreEqual(1, decision.Trace);
            Assert.AreEqual(AriaInterceptPolicy.KeepSelection, plan.Slot, "the visible anti-armour selection is reused");
            Assert.AreEqual(AriaInterceptAction.ArmAttack, decision.Action);

            view.AttackMode = true; view.CompletedContacts = 1; view.Time = 56.5f;
            decision = Step(view, ref plan, out handled);
            Assert.AreEqual(AriaInterceptAction.TargetThreat, decision.Action);
            Assert.AreEqual(3, plan.Stage);
            view.Time = 57f;
            Assert.AreEqual(AriaInterceptAction.TargetThreat, Step(view, ref plan, out _).Action, "keep the same tap until it completes");

            view.AttackMode = false; view.CompletedContacts = 2; view.Time = 58f;
            decision = Step(view, ref plan, out handled);
            Assert.IsTrue(handled);
            Assert.AreEqual(2, decision.Trace);
            Assert.AreEqual(4, plan.Stage);
            Assert.AreEqual(AriaInterceptAction.Waiting, decision.Action, "the group fights; no base order replaces it yet");

            view.Time = 60f;
            Assert.IsTrue(AriaInterceptPolicy.Step(view, ref plan, out _));
            view.ThreatKnown = false; view.ThreatAvailable = false; view.Time = 60f + AriaInterceptPolicy.LostSeconds + .1f;
            Assert.IsTrue(AriaInterceptPolicy.Step(view, ref plan, out _), "a chased contact may leave sight briefly");
            view.Time = 60f + AriaInterceptPolicy.HoldLostSeconds + .1f;
            decision = Step(view, ref plan, out handled);
            Assert.IsFalse(handled, "dead or hidden: the ordinary assault resumes this frame");
            Assert.AreEqual(3, decision.Trace);
            Assert.AreEqual(0, plan.Stage);
            Assert.AreEqual(1, plan.Count);
        }

        [Test]
        public void WithoutAntiArmourSelectionOneCardIsSelectedFirst()
        {
            var plan = default(AriaInterceptState);
            var view = View(55f);
            view.AssaultMask = 0b11111; view.StructureMask = 0b00100; view.AvailableSquadMask = 0b11111;
            view.SelectedMask = 0b00001; view.SelectedGroups = 1; // a rifle card is selected
            var decision = Step(view, ref plan, out _);
            Assert.AreEqual(2, plan.Slot, "the anti-armour card is preferred");
            Assert.AreEqual(AriaInterceptAction.ToggleSelected, decision.Action);
            view.SelectedMask = 0; view.SelectedGroups = 0; view.CompletedContacts = 1;
            decision = Step(view, ref plan, out _);
            Assert.AreEqual(AriaInterceptAction.SelectSlot, decision.Action);
            Assert.AreEqual(2, decision.Slot);
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 2;
            Assert.AreEqual(AriaInterceptAction.ArmAttack, Step(view, ref plan, out _).Action);
        }

        [Test]
        public void NoInterceptWhileBusyStillOrWithoutAVisibleContact()
        {
            var plan = default(AriaInterceptState);
            var view = View(55f); view.Busy = true;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "home defence, open map or precision own the controls");
            view = View(55f); view.ArmyMoving = false;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "no column has been ordered yet");
            view = View(55f); view.ThreatKnown = false;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "a hidden contact is never chased");
            view = View(55f); view.ThreatAvailable = false; view.PanAvailable = false;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "no tap and no clear camera drag");
            Assert.AreEqual(0, plan.Stage);
            Assert.AreEqual(0, plan.Count);
        }

        [Test]
        public void HomeThreatOrTimeoutReleasesAndInterceptsAreBounded()
        {
            var plan = default(AriaInterceptState);
            var view = View(55f); view.SelectedMask = 1; view.SelectedGroups = 1;
            Step(view, ref plan, out _);
            view.Busy = true; view.Time = 56f;
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsFalse(handled, "home defence takes over in the same frame");
            Assert.AreEqual(3, decision.Trace);
            view.Busy = false; view.Time = 57f;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "cooldown after a release");
            for (int round = 1; round < AriaInterceptPolicy.MaxIntercepts; round++)
            {
                view.Time = plan.ReadyAt + .1f;
                Assert.IsTrue(AriaInterceptPolicy.Step(view, ref plan, out _));
                view.Time += AriaInterceptPolicy.OrderSeconds + 1f;
                Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "an unfinished order times out");
            }
            view.Time = plan.ReadyAt + .1f;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "at most MaxIntercepts per match");
            Assert.AreEqual(AriaInterceptPolicy.MaxIntercepts, plan.Count);
        }

        [Test]
        public void SlotChoiceNeverSendsAircraftAndSlippingNeedsReach()
        {
            var view = View(0f);
            view.AssaultMask = 0b00011; view.StructureMask = 0b00001; view.AirMask = 0b00001; view.AvailableSquadMask = 0b00011;
            Assert.AreEqual(1, AriaInterceptPolicy.ChooseSlot(view), "aircraft are skipped");
            view.AssaultMask = 0b00001;
            Assert.AreEqual(-1, AriaInterceptPolicy.ChooseSlot(view));

            // (home distance, nearest own unit distance, that unit's home distance, enemy-base distance)
            Assert.IsTrue(AriaInterceptPolicy.Candidate(111f, 10f, 105f, 600f), "crossing the column");
            Assert.IsTrue(AriaInterceptPolicy.Candidate(82f, 50f, 100f, 600f), "passed its nearest own unit toward home");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(130f, 50f, 105f, 600f), "ahead of the column and not in contact");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(82f, AriaInterceptPolicy.Reach + 1f, 150f, 600f), "too far from any own unit");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(600f, 10f, 580f, 30f), "defenders at the enemy base belong to the assault");
        }

        // Seed 155923, gate run lane-s002gate: at t=53.97 the tank crossing the column is
        // known (fog-visible) while the rocketeer page is selected with Attack armed, but
        // it is outside the camera view and the same tank is also a presented home threat.
        [Test]
        public void OffScreenCrossingTankIsPannedIntoViewThenTapped()
        {
            var plan = default(AriaInterceptState);
            var view = View(53.97f);
            view.ThreatAvailable = false; view.PanAvailable = true; view.AttackMode = true;
            view.SelectedMask = 0b00111; view.SelectedGroups = 3; view.CompletedContacts = 22;
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled, "known but off screen still starts the intercept");
            Assert.AreEqual(1, decision.Trace);
            Assert.AreEqual(AriaInterceptAction.PanToThreat, decision.Action, "Attack is already armed: drag the view onto the tank");
            Assert.AreEqual(AriaInterceptPolicy.StagePan, plan.Stage);

            view.Time = 55f;
            Assert.AreEqual(AriaInterceptAction.PanToThreat, Step(view, ref plan, out _).Action, "same drag until it completes");
            view.CompletedContacts = 23; view.Time = 56f; // drag finished; tank now in view
            view.ThreatAvailable = true; view.PanAvailable = false;
            decision = Step(view, ref plan, out handled);
            Assert.AreEqual(AriaInterceptAction.TargetThreat, decision.Action, "a completed drag is not the attack tap");
            Assert.AreEqual(AriaInterceptPolicy.StageTap, plan.Stage);
            view.CompletedContacts = 24; view.AttackMode = false; view.Time = 57.8f;
            decision = Step(view, ref plan, out handled);
            Assert.IsTrue(handled);
            Assert.AreEqual(2, decision.Trace);
            Assert.AreEqual(AriaInterceptPolicy.StageFight, plan.Stage);
        }

        [Test]
        public void DragThatDropsAttackModeReArmsBeforeTheTap()
        {
            var plan = default(AriaInterceptState);
            var view = View(54f);
            view.ThreatAvailable = false; view.PanAvailable = true; view.AttackMode = true;
            view.SelectedMask = 0b00100; view.SelectedGroups = 1; view.CompletedContacts = 5;
            Assert.AreEqual(AriaInterceptAction.PanToThreat, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 6; view.AttackMode = false; view.Time = 55.5f;
            Assert.AreEqual(AriaInterceptAction.ArmAttack, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 7; view.AttackMode = true; view.ThreatAvailable = true; view.Time = 57f;
            Assert.AreEqual(AriaInterceptAction.TargetThreat, Step(view, ref plan, out _).Action);
        }

        [Test]
        public void PanDeltaMovesTheContactTowardTheCentreWithinLimits()
        {
            // Contact below-left of the view (Unity screen y grows upward): drag right and up.
            Assert.IsTrue(AriaInterceptPolicy.PanDelta(-400f, -300f, 960f, 594f, 730f, 302f, out float dx, out float dy, out bool single));
            Assert.IsFalse(single, "farther than one drag");
            Assert.IsTrue(dx > 0f && dy > 0f);
            Assert.IsTrue(dx <= 730f + .01f && dy <= 302f + .01f, "clamped to the clear drag area");
            Assert.AreEqual(894.0 / 1360.0, dy / dx, 1e-3, "direction is preserved");
            Assert.IsFalse(AriaInterceptPolicy.PanDelta(965f, 590f, 960f, 594f, 730f, 302f, out _, out _, out _), "already centred");
            Assert.IsTrue(AriaInterceptPolicy.PanDelta(1500f, 1200f, 960f, 594f, 730f, 302f, out _, out _, out single));
            Assert.IsFalse(single, "y 1200 needs 606 px > 302 px");
            Assert.IsTrue(AriaInterceptPolicy.PanDelta(1500f, 800f, 960f, 594f, 730f, 302f, out _, out _, out single));
            Assert.IsTrue(single, "just off the right edge: one drag");
            // aria-intercept-q1, 155923 fa-IR t=54.05: tank projected at (-311, 3032), 2.8 screens away.
            Assert.IsTrue(AriaInterceptPolicy.PanDelta(-311f, 3032f, 960f, 594f, 730f, 302f, out _, out _, out single));
            Assert.IsFalse(single);
        }

        // aria-intercept-q1: the tank crossing the column at t=54-56 projected about 2.8
        // screens off view. It is fog-visible (known) but neither tappable nor one drag
        // away, so no intercept starts and the column is not disturbed.
        [Test]
        public void KnownButFarOffScreenContactStartsNothing()
        {
            var plan = default(AriaInterceptState);
            var view = View(54.05f);
            view.ThreatAvailable = false; view.PanAvailable = false;
            view.SelectedMask = 0b00111; view.SelectedGroups = 3;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _));
            Assert.AreEqual(AriaInterceptPolicy.StageIdle, plan.Stage);
            Assert.AreEqual(0, plan.Count);
        }

        // aria-intercept-q1, 130365 en: an intercept released at t=180.42 with Attack still
        // armed; the next owner (FindBase) aimed it at a focus control three times and ARIA
        // stopped Blocked (115). Attack is now toggled off before the controls are handed back.
        [Test]
        public void LostContactWithAttackArmedTogglesAttackOffBeforeRelease()
        {
            var plan = default(AriaInterceptState);
            var view = View(170.51f);
            view.SelectedMask = 0b00001; view.SelectedGroups = 1; view.CompletedContacts = 40;
            Assert.AreEqual(AriaInterceptAction.ArmAttack, Step(view, ref plan, out _).Action);
            view.AttackMode = true; view.CompletedContacts = 41; view.Time = 172f;
            Assert.AreEqual(AriaInterceptAction.TargetThreat, Step(view, ref plan, out _).Action);
            view.ThreatKnown = false; view.ThreatAvailable = false; view.Time = 172f + AriaInterceptPolicy.LostSeconds + .1f;
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled, "not handed back while Attack is armed");
            Assert.AreEqual(AriaInterceptAction.ArmAttack, decision.Action, "the Attack toggle disarms it");
            Assert.AreEqual(AriaInterceptPolicy.StageDisarm, plan.Stage);
            view.Time += .5f;
            Assert.AreEqual(AriaInterceptAction.ArmAttack, Step(view, ref plan, out _).Action, "same toggle until it completes");
            view.AttackMode = false; view.CompletedContacts = 42; view.Time += 1f;
            decision = Step(view, ref plan, out handled);
            Assert.IsFalse(handled);
            Assert.AreEqual(3, decision.Trace);
            Assert.AreEqual(AriaInterceptPolicy.StageIdle, plan.Stage);

            // A disarm that cannot complete is still bounded.
            var stuck = new AriaInterceptState { Stage = AriaInterceptPolicy.StageDisarm, Actions = 50, Deadline = 10f };
            var armed = View(10.1f); armed.AttackMode = true; armed.CompletedContacts = 50;
            Assert.IsFalse(AriaInterceptPolicy.Step(armed, ref stuck, out _));
            Assert.AreEqual(AriaInterceptPolicy.StageIdle, stuck.Stage);
        }

        // aria-intercept-q1, 155923 fa-IR t=122-131: three drags on the same control counted
        // as touch retries (attempts 1-3). One drag per intercept; then wait or release.
        [Test]
        public void AtMostOneDragPerIntercept()
        {
            var plan = default(AriaInterceptState);
            var view = View(120f);
            view.ThreatAvailable = false; view.PanAvailable = true; view.AttackMode = true;
            view.SelectedMask = 0b00001; view.SelectedGroups = 1; view.CompletedContacts = 10;
            Assert.AreEqual(AriaInterceptAction.PanToThreat, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 11; view.Time = 121.5f; // drag done, still not in view
            Assert.AreEqual(AriaInterceptAction.Waiting, Step(view, ref plan, out _).Action, "no second drag");
            Assert.AreEqual(1, plan.Pans);
            view.ThreatAvailable = true; view.Time = 122f;
            Assert.AreEqual(AriaInterceptAction.TargetThreat, Step(view, ref plan, out _).Action);
        }

        static AriaColumnCameraView March(float time) => new AriaColumnCameraView
        {
            Marching = true, EnemyAlive = true, PageOrdered = true, ColumnKnown = true,
            HomeAvailable = true, PanAvailable = true, Time = time
        };

        // The Advance point sits 25 units in front of the enemy base, about 600 world units
        // from the column; one Home tap brings the camera back, then drags keep it framed.
        [Test]
        public void HomeOnceThenPacedAlternatingRecentringDrags()
        {
            var plan = default(AriaColumnCameraState);
            var view = March(50f); view.PageOrdered = false;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "the page still needs its Advance order");
            view.PageOrdered = true; view.CompletedContacts = 20;
            Assert.IsTrue(AriaColumnCameraPolicy.Step(view, ref plan, out var decision));
            Assert.AreEqual(AriaColumnCameraAction.Home, decision.Action);
            Assert.AreEqual(1, decision.Trace);
            view.Time = 50.5f;
            Assert.AreEqual(AriaColumnCameraAction.Home, Step(view, ref plan).Action, "same Home tap until it completes");
            view.CompletedContacts = 21; view.Time = 51f;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _));
            Assert.AreEqual(51f + AriaColumnCameraPolicy.IntervalSeconds, plan.ReadyAt, 1e-4);
            view.Time = 52f;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "human pace between camera actions");
            view.Time = 53.6f;
            Assert.IsTrue(AriaColumnCameraPolicy.Step(view, ref plan, out decision));
            Assert.AreEqual(AriaColumnCameraAction.Recentre, decision.Action);
            int first = decision.PanId;
            view.CompletedContacts = 22; view.Time = 54f;
            AriaColumnCameraPolicy.Step(view, ref plan, out _);
            view.Time = 57f;
            Assert.IsTrue(AriaColumnCameraPolicy.Step(view, ref plan, out decision));
            Assert.AreNotEqual(first, decision.PanId, "consecutive drags use alternate controls, never touch retries");
            Assert.AreEqual(1, plan.HomeDone);
        }

        static AriaColumnCameraDecision Step(in AriaColumnCameraView view, ref AriaColumnCameraState plan)
        { AriaColumnCameraPolicy.Step(view, ref plan, out var decision); return decision; }

        [Test]
        public void ColumnCameraYieldsAndStaysQuietWhenFramed()
        {
            var plan = default(AriaColumnCameraState);
            var view = March(50f); view.Busy = true;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "another owner holds the controls");
            view.Busy = false; view.ColumnCentred = true;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "already watching the column: no Home tap");
            Assert.AreEqual(1, plan.HomeDone);
            view.Time = 60f;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "inside the middle third: no drag");

            view.ColumnCentred = false; view.CompletedContacts = 5;
            Assert.IsTrue(AriaColumnCameraPolicy.Step(view, ref plan, out _));
            view.Busy = true; view.Time = 60.2f; // e.g. the intercept or home defence starts
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "a pending drag yields at once");
            Assert.AreEqual(0, plan.Stage);
        }

        [Test]
        public void ColumnCameraStopsAtTheBaseAfterRefindsAndWithinItsBudget()
        {
            var plan = default(AriaColumnCameraState);
            var view = March(50f); view.AtBase = true;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out var decision));
            Assert.AreEqual(3, decision.Trace);
            view.AtBase = false; view.Time = 99f;
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "done for this match");

            // Each Advance-point re-find after the Home tap counts once; two end the rule.
            plan = new AriaColumnCameraState { HomeDone = 1 };
            view = March(60f); view.Refinding = true; view.Busy = true;
            AriaColumnCameraPolicy.Step(view, ref plan, out _);
            AriaColumnCameraPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(1, plan.Refinds, "one navigation counts once");
            view.Refinding = false; view.Busy = false; view.Time = 61f;
            AriaColumnCameraPolicy.Step(view, ref plan, out _);
            view.Refinding = true; view.Busy = true;
            AriaColumnCameraPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(1, plan.Done, "falls back to today's behaviour");

            plan = new AriaColumnCameraState { HomeDone = 1, Pans = (byte)AriaColumnCameraPolicy.MaxPans };
            view = March(70f);
            Assert.IsFalse(AriaColumnCameraPolicy.Step(view, ref plan, out _), "drag budget per match");

            Assert.IsTrue(AriaColumnCameraPolicy.Centred(960f, 540f, 0f, 0f, 1920f, 1080f));
            Assert.IsFalse(AriaColumnCameraPolicy.Centred(500f, 540f, 0f, 0f, 1920f, 1080f));
            Assert.IsFalse(AriaColumnCameraPolicy.Centred(960f, 3037f, 0f, 0f, 1920f, 1080f));
        }
    }
}
