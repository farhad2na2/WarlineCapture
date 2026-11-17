using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Lane 14 S002 155923 en: an enemy tank shelled the HQ from ~66 units while no contact was presented
    // inside the home radius. Round 1 sent one squad onto column-side home ground, away from the tank. Round 2
    // spent 11 s on slot-by-slot selection and the base fell. These checks cover the public ranged-attacker
    // target, the anti-armour defender and the fast escalation, all issued through ordinary roster controls.
    public sealed class AriaHomeDefenseRangedTests
    {
        static AriaHomeDefenseView View(float time) => new AriaHomeDefenseView
        {
            PresentedHomeThreat = true, BaseDamageOnly = true, ProportionateDefense = true,
            FocusAvailable = true, AttackAvailable = true, ToggleAvailable = true,
            HomeGroundAvailable = true, ExpandedPageIndex = 0, ExpandedAssaultMask = 0b11111,
            AvailableSquadMask = 0b11111, Time = time
        };

        static bool IsOrder(AriaHomeDefenseAction action) =>
            action == AriaHomeDefenseAction.AttackThreatTerrain || action == AriaHomeDefenseAction.AttackThreatVisible ||
            action == AriaHomeDefenseAction.AttackHomeTerrain || action == AriaHomeDefenseAction.AttackAttackerVisible ||
            action == AriaHomeDefenseAction.AttackAttackerTerrain || action == AriaHomeDefenseAction.AttackHomeFarTerrain;

        static int Bits(int mask) { int n = 0; while (mask != 0) { n += mask & 1; mask >>= 1; } return n; }

        // Plays the decisions as touches on one roster page. Returns the touch counts at which orders went out.
        static System.Collections.Generic.List<int> Play(ref AriaHomeDefenseView view, ref AriaHomeDefenseState plan,
            out System.Collections.Generic.List<AriaHomeDefenseAction> actions, int maxSteps = 40)
        {
            var orders = new System.Collections.Generic.List<int>();
            actions = new System.Collections.Generic.List<AriaHomeDefenseAction>();
            int idle = 0;
            for (int i = 0; i < maxSteps; i++)
            {
                AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
                // A step may only change stage (no touch); two in a row means the round is waiting.
                if (decision.Action == AriaHomeDefenseAction.Waiting || decision.Action == AriaHomeDefenseAction.None ||
                    decision.Action == AriaHomeDefenseAction.Unavailable)
                { if (++idle >= 2) break; continue; }
                idle = 0;
                actions.Add(decision.Action);
                switch (decision.Action)
                {
                    case AriaHomeDefenseAction.SelectSlot: view.ExpandedSelectedMask |= 1 << decision.Slot; break;
                    case AriaHomeDefenseAction.ToggleSelected:
                        view.ExpandedSelectedMask &= view.ExpandedSelectedMask - 1; break;
                    case AriaHomeDefenseAction.ArmAttack: view.AttackMode = !view.AttackMode; break;
                }
                if (IsOrder(decision.Action)) { view.AttackMode = false; orders.Add(view.CompletedContacts + 1); }
                view.ExpandedSelectedGroups = Bits(view.ExpandedSelectedMask);
                view.ToggleAvailable = view.ExpandedSelectedGroups > 0;
                view.CompletedContacts++;
                view.Time += 2f;
            }
            return orders;
        }

        [Test]
        public void PresentedAttackerNearHomeIsPreferredVisibleBeforeGhost()
        {
            float near = 66f * 66f, far = 100f * 100f, outside = 140f * 140f;
            Assert.IsTrue(AriaPublicHomeThreatPolicy.PreferAttacker(2, near, 0, AriaPublicHomeThreatPolicy.RangedRadiusSquared), "a last-seen ghost counts");
            Assert.IsFalse(AriaPublicHomeThreatPolicy.PreferAttacker(1, outside, 0, AriaPublicHomeThreatPolicy.RangedRadiusSquared), "beyond weapon reach of the HQ");
            Assert.IsTrue(AriaPublicHomeThreatPolicy.PreferAttacker(1, far, 2, near), "a visible unit beats a nearer ghost");
            Assert.IsFalse(AriaPublicHomeThreatPolicy.PreferAttacker(2, near, 1, far), "a ghost never replaces a visible unit");
            Assert.IsTrue(AriaPublicHomeThreatPolicy.PreferAttacker(1, near, 1, far), "then the nearer to home");
            Assert.IsFalse(AriaPublicHomeThreatPolicy.PreferAttacker(0, near, 0, AriaPublicHomeThreatPolicy.RangedRadiusSquared), "no sight, no candidate");
        }

        [Test]
        public void BaseDamageTargetsTheRangedAttackerBeforeHomeGround()
        {
            var view = View(67f);
            view.AttackerVisibleAvailable = true; view.AttackerGroundAvailable = true;
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerVisible, AriaHomeDefensePolicy.DefenseTarget(view, 1).Action);
            view.AttackerVisibleAvailable = false;
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerTerrain, AriaHomeDefensePolicy.DefenseTarget(view, 1).Action, "a ghost marker: ground beside it");
            view.AttackerGroundAvailable = false; view.HomeGroundFarAvailable = true;
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeTerrain, AriaHomeDefensePolicy.DefenseTarget(view, 1).Action, "no attacker: column-side ground first");
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeFarTerrain, AriaHomeDefensePolicy.DefenseTarget(view, 2).Action, "the second round tries the far side");
            view.HomeGroundAvailable = false;
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeFarTerrain, AriaHomeDefensePolicy.DefenseTarget(view, 1).Action);
            // A presented home contact (minimap reason) keeps its original targets.
            view.BaseDamageOnly = false; view.AttackerVisibleAvailable = true; view.ThreatGroundAvailable = true;
            Assert.AreEqual(AriaHomeDefenseAction.AttackThreatTerrain, AriaHomeDefensePolicy.DefenseTarget(view, 1).Action);
        }

        [Test]
        public void FirstRoundSendsTheAntiArmourSquadAtTheAttacker()
        {
            var plan = default(AriaHomeDefenseState);
            var view = View(67f); view.HomeGroundAvailable = false; view.AntiArmorMask = 0b00100;
            var first = AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
            Assert.IsTrue(first);
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, decision.Action, "camera away from home: Home first");
            view.CompletedContacts = 1; view.AttackerGroundAvailable = true;
            var orders = Play(ref view, ref plan, out var actions);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, actions[0]);
            Assert.AreEqual(0b00100, view.ExpandedSelectedMask, "the rocket/tank card, not the first card");
            Assert.AreEqual(1, orders.Count);
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerTerrain, actions[actions.Count - 1]);
            Assert.AreEqual(3, plan.HomeDefenseSlot);
            Assert.AreEqual(1, plan.HomeDefenseScope);
        }

        [Test]
        public void EscalationOrdersWithinTwoTouchesAndNeverClearsItsOwnSelection()
        {
            // Round 1 left squad 0 selected; the base is now critical and the far-side ground is on screen.
            var plan = new AriaHomeDefenseState { HomeDefenseStage = 8, HomeDefenseScope = 1, HomeDefenseRounds = 1, HomeDefenseSlot = 1,
                HomeDefenseReason = AriaHomeDefensePolicy.ReasonBaseDamage, HomeDefenseObserveUntil = 200f };
            var view = View(84f); view.BaseCritical = true; view.HomeGroundFarAvailable = true;
            view.ExpandedSelectedMask = 1; view.ExpandedSelectedGroups = 1; view.CompletedContacts = 10;
            int start = view.CompletedContacts;
            var orders = Play(ref view, ref plan, out var actions);
            Assert.AreEqual(2, plan.HomeDefenseRounds);
            Assert.AreEqual(0, plan.HomeDefenseScope, "escalation is the whole roster");
            Assert.IsTrue(orders.Count >= 2, "first order, then the rest of the page");
            Assert.IsTrue(orders[0] - start <= 2, "Attack + ground: the first order needs no Home tap and no card taps");
            Assert.IsFalse(actions.Contains(AriaHomeDefenseAction.ToggleSelected), "a sweep never deselects squads it is about to order");
            Assert.IsFalse(actions.Contains(AriaHomeDefenseAction.InspectHome), "the ground was already on screen");
            Assert.AreEqual(AriaHomeDefenseAction.AttackHomeFarTerrain, actions[orders[0] - start - 1], "round 2 without an attacker: far side");
            Assert.AreEqual(0b11111, view.ExpandedSelectedMask, "every assault squad was ordered");
            Assert.IsTrue(orders[orders.Count - 1] - start <= 8, "whole page within 8 touches");
        }

        [Test]
        public void EscalationWithNothingSelectedPicksOneAntiArmourCardThenOrders()
        {
            var plan = new AriaHomeDefenseState { HomeDefenseStage = 8, HomeDefenseScope = 1, HomeDefenseRounds = 1,
                HomeDefenseReason = AriaHomeDefensePolicy.ReasonBaseDamage, HomeDefenseObserveUntil = 200f };
            var view = View(84f); view.BaseCritical = true; view.AttackerGroundAvailable = true; view.AntiArmorMask = 0b01000;
            var orders = Play(ref view, ref plan, out var actions);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, actions[0]);
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, actions[1]);
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerTerrain, actions[2]);
            Assert.AreEqual(3, orders[0], "card, Attack, ground");
            Assert.AreEqual(0b11111, view.ExpandedSelectedMask);
        }

        [Test]
        public void GenericPlannerKeepsItsOriginalSweep()
        {
            // ProportionateDefense false (generic planner): focus first and the selection is cleared before selecting.
            var plan = default(AriaHomeDefenseState);
            var view = View(50f); view.ProportionateDefense = false; view.BaseDamageOnly = false; view.ThreatGroundAvailable = true;
            view.ExpandedSelectedMask = 0b00001; view.ExpandedSelectedGroups = 1;
            AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
            Assert.AreEqual(AriaHomeDefenseAction.InspectHome, decision.Action);
            view.CompletedContacts = 1;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.ToggleSelected, decision.Action);
        }
    }
}
