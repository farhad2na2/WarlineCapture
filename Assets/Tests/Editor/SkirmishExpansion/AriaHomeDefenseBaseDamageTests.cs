using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Pure policy checks: HUD base-health damage as a home threat and a
    // proportionate single defender issued through the normal roster controls.
    public sealed class AriaHomeDefenseBaseDamageTests
    {
        static AriaHomeDefenseView View(float time) => new AriaHomeDefenseView
        {
            PresentedHomeThreat = true, BaseDamageOnly = true, ProportionateDefense = true,
            FocusAvailable = true, AttackAvailable = true, ToggleAvailable = true, NextAvailable = true,
            HomeGroundAvailable = true, ThreatGroundAvailable = true,
            ExpandedPageIndex = 0, ExpandedAssaultMask = 0b11111, AvailableSquadMask = 0b11111, Time = time
        };

        static AriaHomeDefenseDecision Step(in AriaHomeDefenseView view, ref AriaHomeDefenseState plan, out bool handled)
        {
            handled = AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
            return decision;
        }

        [Test]
        public void UndamagedBaseIsNotAThreat()
        {
            var plan = default(AriaHomeDefenseState);
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(10f, 800f, ref plan));
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(11f, 800f, ref plan));
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(12f, 810f, ref plan), "repair is not damage");
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(13f, 0f, ref plan), "an unknown readout is not damage");
            var view = View(13f); view.PresentedHomeThreat = false; view.BaseDamageOnly = false;
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsFalse(handled);
            Assert.AreEqual(AriaHomeDefenseAction.None, decision.Action);
            Assert.AreEqual(0, plan.HomeDefenseStage);
        }

        [Test]
        public void BaseDamageWithoutContactTriggersDefence()
        {
            var plan = default(AriaHomeDefenseState);
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(60f, 800f, ref plan));
            Assert.IsTrue(AriaBaseDamagePolicy.Observe(61f, 772f, ref plan));
            Assert.IsTrue(AriaBaseDamagePolicy.Observe(65f, 772f, ref plan), "damage is remembered briefly");
            // The camera is with the column: no home ground is on screen yet, so Home is tapped first.
            var away = View(65f); away.HomeGroundAvailable = away.ThreatGroundAvailable = false;
            var decision = Step(away, ref plan, out bool handled);
            Assert.IsTrue(handled);
            Assert.IsTrue(decision.Defend);
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, decision.Action);
            Assert.AreEqual(1, plan.HomeDefenseStage);
            Assert.AreEqual(AriaHomeDefensePolicy.ReasonBaseDamage, plan.HomeDefenseReason);
            Assert.AreEqual(1, plan.HomeDefenseScope, "a raid gets one defender first");
        }

        [Test]
        public void ProportionateDefenderIsOneGroupOnHomeGround()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(70f); view.HomeGroundAvailable = view.ThreatGroundAvailable = false;
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 1; view.HomeGroundAvailable = view.ThreatGroundAvailable = true;
            var decision = Step(view, ref plan, out _);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(0, decision.Slot);
            Assert.AreEqual(1, plan.HomeDefenseSlot);
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.CompletedContacts = 2;
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, Step(view, ref plan, out _).Action);
            view.AttackMode = true; view.CompletedContacts = 3;
            decision = Step(view, ref plan, out _);
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, decision.Action, "no contact: attack-move onto home ground, not the army's threat");
            Assert.AreEqual(5, plan.HomeDefenseStage);
            view.AttackMode = false; view.CompletedContacts = 4;
            decision = Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled);
            Assert.AreNotEqual(AriaHomeDefenseAction.NextPage, decision.Action, "other pages are not swept for a single defender");
            Assert.AreEqual(8, plan.HomeDefenseStage);
            Assert.AreEqual(70f + AriaHomeDefensePolicy.SingleDefenderObserveSeconds, plan.HomeDefenseObserveUntil, 1e-4f);
        }

        [Test]
        public void FallingBaseEscalatesToTheWholeRoster()
        {
            var plan = new AriaHomeDefenseState { HomeDefenseStage = 8, HomeDefenseScope = 1, HomeDefenseRounds = 1,
                HomeDefenseReason = AriaHomeDefensePolicy.ReasonBaseDamage, HomeDefenseObserveUntil = 100f };
            var view = View(80f); view.BaseCritical = true; view.HomeGroundAvailable = view.ThreatGroundAvailable = false;
            Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, plan.HomeDefenseStage);
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, Step(view, ref plan, out _).Action);
            Assert.AreEqual(0, plan.HomeDefenseScope, "second round sends every assault group");
            Assert.AreEqual(2, plan.HomeDefenseRounds);
            Assert.IsTrue(AriaBaseDamagePolicy.Critical(800f, 320f));
            Assert.IsFalse(AriaBaseDamagePolicy.Critical(800f, 744f));

            var critical = default(AriaHomeDefenseState);
            Step(view, ref critical, out _);
            Assert.AreEqual(0, critical.HomeDefenseScope, "a base about to fall gets the whole roster at once");
        }

        [Test]
        public void ThreatClearsAndAssaultResumes()
        {
            var plan = default(AriaHomeDefenseState);
            AriaBaseDamagePolicy.Observe(60f, 800f, ref plan);
            Assert.IsTrue(AriaBaseDamagePolicy.Observe(61f, 772f, ref plan));
            Assert.IsFalse(AriaBaseDamagePolicy.Observe(61f + AriaBaseDamagePolicy.MemorySeconds + .1f, 772f, ref plan), "steady readout clears");
            plan.HomeDefenseStage = 8; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1; plan.HomeDefenseRounds = 1;
            plan.HomeDefenseReason = AriaHomeDefensePolicy.ReasonBaseDamage; plan.Column.AssaultIssued = 1; plan.Column.PagesOrdered = 2;
            var view = View(70f); view.PresentedHomeThreat = false; view.BaseDamageOnly = false;
            Step(view, ref plan, out bool handled);
            Assert.IsFalse(handled);
            Assert.AreEqual(0, plan.HomeDefenseStage);
            Assert.AreEqual(AriaHomeDefensePolicy.ReasonNone, plan.HomeDefenseReason);
            Assert.AreEqual(0, plan.HomeDefenseRounds);
            Assert.AreEqual(0, plan.Column.AssaultIssued, "the enemy-base sweep is reissued");
            Assert.AreEqual(0, plan.Column.PagesOrdered);
        }

        [Test]
        public void LegacyCallersKeepTheFullMinimapSweep()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(70f); view.ProportionateDefense = false; view.BaseDamageOnly = false;
            Step(view, ref plan, out _);
            Assert.AreEqual(0, plan.HomeDefenseScope);
            Assert.AreEqual(AriaHomeDefensePolicy.ReasonMinimap, plan.HomeDefenseReason);
            view.CompletedContacts = 1; view.ExpandedSelectedMask = 0b00001; view.ExpandedSelectedGroups = 0;
            var decision = Step(view, ref plan, out _);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(1, decision.Slot, "every unselected assault slot is selected");
        }
    
        // Windows lane, S002 130365 fa-IR (2026-11-17): base damage at t=71.8, one defender selected,
        // Attack armed, home ground never offered; Focus was pressed four times with Attack armed
        // and ARIA stopped Blocked (115). Simulated here: every press completes, the ground stays
        // unavailable. No control may be pressed three times in a row and Attack ends disarmed.
        [Test]
        public void HomeGroundNeverOfferedNeverRepeatsAControlAndDisarmsAttack()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(71.8f); view.HomeGroundAvailable = false; view.ThreatGroundAvailable = false; view.ThreatVisibleAvailable = false;
            var last = AriaHomeDefenseAction.None; int lastSlot = -1, streak = 0, longest = 0, presses = 0, focus = 0;
            for (int i = 0; i < 400; i++)
            {
                var decision = Step(view, ref plan, out bool handled);
                Assert.IsTrue(handled, "home defence keeps the controls while the base is attacked");
                view.Time += .3f;
                if (decision.Action <= AriaHomeDefenseAction.Unavailable) continue;
                // Each step that names a press is one completed touch.
                presses++; view.CompletedContacts++;
                // Same control = same action on the same slot (cards differ per slot).
                streak = decision.Action == last && decision.Slot == lastSlot ? streak + 1 : 1;
                last = decision.Action; lastSlot = decision.Slot; if (streak > longest) longest = streak;
                if (decision.Action == AriaHomeDefenseAction.InspectHome) focus++;
                if (decision.Action == AriaHomeDefenseAction.ArmAttack) view.AttackMode = !view.AttackMode;
                if (decision.Action == AriaHomeDefenseAction.SelectSlot) { view.ExpandedSelectedMask |= 1 << decision.Slot; view.ExpandedSelectedGroups++; }
                if (decision.Action == AriaHomeDefenseAction.ToggleSelected) { view.ExpandedSelectedMask = 0; view.ExpandedSelectedGroups = 0; }
            }
            Assert.Less(longest, 3, "never three presses of one control in a row (reason 115 needs a fourth)");
            Assert.Greater(presses, 0);
            Assert.Greater(focus, 1, "rounds keep re-checking the home view, spaced by other controls");
            Assert.IsFalse(view.AttackMode, "Attack is not left armed for the next owner");
        }

        [Test]
        public void AttackArmedWithoutGroundTogglesItOffBeforeOneRecentre()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(80f); view.HomeGroundAvailable = false; view.ThreatGroundAvailable = false;
            plan.HomeDefenseStage = 4; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.AttackMode = true; view.CompletedContacts = 31;
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, Step(view, ref plan, out _).Action, "disarm first: a focus with Attack armed does not move the camera");
            view.AttackMode = false; view.CompletedContacts = 32;
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, Step(view, ref plan, out _).Action, "one re-centre with Attack off");
            Assert.AreEqual(AriaHomeDefensePolicy.StageRecentre, plan.HomeDefenseStage);
            view.CompletedContacts = 33; view.HomeGroundAvailable = true;
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, Step(view, ref plan, out _).Action, "ground offered after the re-centre: arm and defend");
            view.AttackMode = true; view.CompletedContacts = 34;
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, Step(view, ref plan, out _).Action);
        }

        [Test]
        public void MissingHomeGroundFallsBackToAnOfferedThreat()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(80f); view.HomeGroundAvailable = false; view.ThreatGroundAvailable = false; view.ThreatVisibleAvailable = true;
            plan.HomeDefenseStage = 4; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.AttackMode = true;
            Assert.AreEqual(AriaHomeDefenseAction.AttackThreatVisible, Step(view, ref plan, out _).Action, "the raider on screen is attacked");
        }

        [Test]
        public void GroundTapLeavingAttackArmedIsRepeatedOnceThenGivenUp()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(80f);
            plan.HomeDefenseStage = 4; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.AttackMode = true; view.CompletedContacts = 10;
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 11; // tap completed, Attack still armed
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, Step(view, ref plan, out _).Action, "repeated once");
            view.CompletedContacts = 12;
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, Step(view, ref plan, out _).Action, "then Attack is toggled off, not a third ground tap");
            Assert.AreEqual(AriaHomeDefensePolicy.StageDisarm, plan.HomeDefenseStage);
            view.AttackMode = false; view.CompletedContacts = 13;
            Assert.IsTrue(AriaHomeDefensePolicy.Step(view, ref plan, out _));
            Assert.AreEqual(8, plan.HomeDefenseStage, "observe briefly, then a new round");
        }

        // Windows lane batch 4 (17 fps), S002 130365/155923: the re-centre's result was judged in the
        // frame the focus tap completed; the round gave up while the camera was still arriving.
        [Test]
        public void RecentreWaitsForTheCameraBeforeGivingUp()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(73f); view.HomeGroundAvailable = false; view.ThreatGroundAvailable = false;
            plan.HomeDefenseStage = 4; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.CompletedContacts = 35;
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, Step(view, ref plan, out _).Action);
            Assert.AreEqual(AriaHomeDefensePolicy.StageRecentre, plan.HomeDefenseStage);
            view.CompletedContacts = 36; view.Time = 74.2f; // focus tap completed, camera still moving
            Assert.AreEqual(AriaHomeDefenseAction.Waiting, Step(view, ref plan, out _).Action, "wait for the camera");
            view.Time = 75.5f; view.HomeGroundAvailable = true; // home ground offered once the view settles
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, Step(view, ref plan, out _).Action);
            view.AttackMode = true; view.CompletedContacts = 37;
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, Step(view, ref plan, out _).Action, "the defender attack-moves onto home ground");

            plan = default;
            view = View(73f); view.HomeGroundAvailable = false; view.ThreatGroundAvailable = false;
            plan.HomeDefenseStage = 4; plan.HomeDefenseScope = 1; plan.HomeDefenseSlot = 1;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.CompletedContacts = 35;
            Step(view, ref plan, out _);
            view.CompletedContacts = 36; view.Time = 73f + AriaHomeDefensePolicy.RecentreSeconds + .1f;
            Assert.IsTrue(AriaHomeDefensePolicy.Step(view, ref plan, out _));
            Assert.AreEqual(8, plan.HomeDefenseStage, "bounded: still nothing after the settle time, the round is given up");
        }
}
}
