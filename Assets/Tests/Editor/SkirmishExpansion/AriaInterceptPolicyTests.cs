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
            ThreatAvailable = true, ArmyMoving = true, AttackAvailable = true, ToggleAvailable = true,
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
            view.ThreatAvailable = false; view.Time = 60f + AriaInterceptPolicy.LostSeconds + .1f;
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
            view = View(55f); view.ThreatAvailable = false;
            Assert.IsFalse(AriaInterceptPolicy.Step(view, ref plan, out _), "a hidden or off-screen contact is never chased");
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

            Assert.IsTrue(AriaInterceptPolicy.Candidate(82f, 122f, 50f, 600f), "behind the column and in reach");
            Assert.IsTrue(AriaInterceptPolicy.Candidate(111f, 105f, 10f, 600f), "crossing the column");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(130f, 105f, 50f, 600f), "ahead of the column and not in contact");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(82f, 122f, AriaInterceptPolicy.Reach + 1f, 600f), "too far from any own unit");
            Assert.IsFalse(AriaInterceptPolicy.Candidate(600f, 580f, 10f, 30f), "defenders at the enemy base belong to the assault");
        }
    }
}
