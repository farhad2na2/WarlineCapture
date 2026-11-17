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
            var offScreen = View(65f); offScreen.HomeGroundAvailable = false;
            var decision = Step(offScreen, ref plan, out bool handled);
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
            var view = View(70f); view.HomeGroundAvailable = false;
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, Step(view, ref plan, out _).Action);
            view.CompletedContacts = 1; view.HomeGroundAvailable = true;
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
            var view = View(80f); view.BaseCritical = true; view.HomeGroundAvailable = false;
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
        public void ShownDefenceTargetSkipsTheCameraStep()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(67f); view.CompletedContacts = 30;
            var decision = Step(view, ref plan, out bool handled);
            Assert.IsTrue(handled);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action, "home ground already on screen: select the defender at once");
            Assert.AreEqual(0, decision.Slot);
            Assert.AreEqual(1, plan.HomeDefenseRounds);
        }

        [Test]
        public void HomeGuardIsThePreferredSingleDefender()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(67f); view.PreferredSlotMask = 0b00010; view.CompletedContacts = 5;
            var decision = Step(view, ref plan, out _);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(1, decision.Slot, "the guard is already home");
            Assert.AreEqual(2, plan.HomeDefenseSlot);
        }

        static AriaHomeGuardView Roster() => new AriaHomeGuardView
        {
            // Rifle x12, Gunner x4, Rocketeer x4 (structure), Car x1, Apc x1.
            PageIndex = 0, TotalGroups = 8, AssaultMask = 0b11111, StructureMask = 0b00100, InfantryMask = 0b00111,
            Alive0 = 12, Alive1 = 4, Alive2 = 4, Alive3 = 1, Alive4 = 1,
            Group0 = 1, Group1 = 2, Group2 = 3, Group3 = 4, Group4 = 5
        };

        [Test]
        public void GuardIsTheSmallestInfantrySquadThatCannotHitTheBase()
        {
            var roster = Roster();
            Assert.AreEqual(2u, AriaHomeGuardPolicy.Choose(roster), "gunners stay; rifles and every base-damaging squad go");
            Assert.AreEqual(0b00010, AriaHomeGuardPolicy.SlotMask(2u, roster));
            roster.Group1 = 7;
            Assert.AreEqual(0, AriaHomeGuardPolicy.SlotMask(2u, roster), "guard card not on this page");
            Assert.AreEqual(0, AriaHomeGuardPolicy.SlotMask(0u, roster));
        }

        [Test]
        public void NoGuardWhenItWouldStripTheAssault()
        {
            var roster = Roster(); roster.InfantryMask = 0b00100;
            Assert.AreEqual(0u, AriaHomeGuardPolicy.Choose(roster), "structure squads always join the assault");
            roster = Roster(); roster.AirMask = 0b00011; roster.InfantryMask = 0b00011;
            Assert.AreEqual(0u, AriaHomeGuardPolicy.Choose(roster), "air squads are not guards");
            roster = Roster(); roster.AssaultMask = 0b00010;
            Assert.AreEqual(0u, AriaHomeGuardPolicy.Choose(roster), "a lone squad goes with the assault");
            roster = Roster(); roster.TotalGroups = 2;
            Assert.AreEqual(0u, AriaHomeGuardPolicy.Choose(roster));
            roster = Roster(); roster.PageIndex = 1;
            Assert.AreEqual(0u, AriaHomeGuardPolicy.Choose(roster), "chosen from the first roster page only");
        }

        static AriaHomeGuardPostView Post(float time) => new AriaHomeGuardPostView
        {
            Guard = 1, GuardMask = 0b00001, PageIndex = 0, Time = time, PreviousAvailable = true, ToggleAvailable = true,
            AttackAvailable = true, HomeGroundAvailable = true, FocusAvailable = true
        };

        [Test]
        public void GuardIsPostedOntoHomeGroundWithOrdinaryControls()
        {
            byte stage = 0; float deadline = 0f; int actions = 0;
            var view = Post(5f); view.SelectedGroups = 1; view.SelectedMask = 0b00100;
            Assert.AreEqual(AriaHomeGuardPostAction.ToggleSelected, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions), "clear another selection first");
            Assert.AreEqual(1, stage);
            Assert.AreEqual(5f + AriaHomeGuardPostPolicy.DeadlineSeconds, deadline, 1e-4f);
            view.SelectedGroups = 0; view.SelectedMask = 0; view.CompletedContacts = 1;
            Assert.AreEqual(AriaHomeGuardPostAction.SelectGuard, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            view.SelectedGroups = 1; view.SelectedMask = 0b00001; view.CompletedContacts = 2;
            Assert.AreEqual(AriaHomeGuardPostAction.ArmAttack, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            view.AttackMode = true; view.HomeGroundAvailable = false; view.CompletedContacts = 3;
            Assert.AreEqual(AriaHomeGuardPostAction.Waiting, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions), "no camera touch, no order elsewhere");
            view.HomeGroundAvailable = true;
            Assert.AreEqual(AriaHomeGuardPostAction.TargetHomeGround, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            Assert.AreEqual(3, stage);
            Assert.AreEqual(AriaHomeGuardPostAction.TargetHomeGround, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions), "same target until the touch completes");
            view.AttackMode = false; view.CompletedContacts = 4;
            Assert.AreEqual(AriaHomeGuardPostAction.None, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            Assert.AreEqual(2, stage, "posted once");
            Assert.AreEqual(AriaHomeGuardPostAction.None, AriaHomeGuardPostPolicy.Step(Post(6f), ref stage, ref deadline, ref actions));
        }

        [Test]
        public void GuardPostIsBoundedAndNeedsAGuard()
        {
            byte stage = 0; float deadline = 0f; int actions = 0;
            var view = Post(5f); view.Guard = 0;
            Assert.AreEqual(AriaHomeGuardPostAction.None, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            Assert.AreEqual(2, stage);
            stage = 0; view = Post(5f); view.GuardMask = 0; view.PageIndex = 1;
            Assert.AreEqual(AriaHomeGuardPostAction.PreviousPage, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions), "the guard card is on the first page");
            view = Post(5f + AriaHomeGuardPostPolicy.DeadlineSeconds + 1f);
            Assert.AreEqual(AriaHomeGuardPostAction.None, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions));
            Assert.AreEqual(2, stage, "gives up after the deadline");
            stage = 0; view = Post(5f); view.UiBusy = true;
            Assert.AreEqual(AriaHomeGuardPostAction.None, AriaHomeGuardPostPolicy.Step(view, ref stage, ref deadline, ref actions), "open drawers/maps are left to the normal flow");
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
    }
}
