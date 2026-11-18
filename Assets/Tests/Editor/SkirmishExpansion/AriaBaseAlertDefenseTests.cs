using Game.Simulation;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Orchestrator decision 2026-11-17 (S002 lone tank): legibility first. The HUD warns the player that
    // the base is under attack (perimeter entry or first damage), gives base vision through fog and
    // highlights the nearest own squad card. ARIA reacts to that same warning, through the same controls,
    // and reads nothing the player is not shown. These checks cover the trigger, the nearest suitable
    // defender and the touch budget from warning to order (~5 s at the lane's ~1.75 s per touch).
    public sealed class AriaBaseAlertDefenseTests
    {
        const int MaxTouchesToOrder = 3;

        static AriaHomeDefenseView Alert(float time) => new AriaHomeDefenseView
        {
            PresentedHomeThreat = true, BaseDamageOnly = true, BaseAlert = true, ProportionateDefense = true,
            FocusAvailable = true, AttackAvailable = true, HomeGroundAvailable = true,
            ExpandedPageIndex = 1, ExpandedAssaultMask = 0b11111, AvailableSquadMask = 0b11111,
            NextAvailable = true, PreviousAvailable = true, Time = time
        };

        static bool IsOrder(AriaHomeDefenseAction action) =>
            action == AriaHomeDefenseAction.AttackThreatTerrain || action == AriaHomeDefenseAction.AttackThreatVisible ||
            action == AriaHomeDefenseAction.AttackHomeTerrain || action == AriaHomeDefenseAction.AttackAttackerVisible ||
            action == AriaHomeDefenseAction.AttackAttackerTerrain || action == AriaHomeDefenseAction.AttackHomeFarTerrain;

        static int Bits(int mask) { int n = 0; while (mask != 0) { n += mask & 1; mask >>= 1; } return n; }

        // Plays decisions as completed touches until the first order. nearestPage/nearestSlot: where the
        // highlighted card lives; the warning's control pages the roster there.
        static System.Collections.Generic.List<AriaHomeDefenseAction> PlayToOrder(ref AriaHomeDefenseView view,
            ref AriaHomeDefenseState plan, int nearestPage, int nearestSlot, int maxSteps = 20)
        {
            var actions = new System.Collections.Generic.List<AriaHomeDefenseAction>();
            int idle = 0;
            for (int i = 0; i < maxSteps; i++)
            {
                view.HighlightedDefender = nearestSlot >= 0 && view.ExpandedPageIndex == nearestPage ? nearestSlot + 1 : 0;
                view.DefenderKnown = nearestSlot >= 0;
                view.DefenderPageAvailable = view.DefenderKnown && view.HighlightedDefender == 0;
                AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
                if (decision.Action == AriaHomeDefenseAction.Waiting || decision.Action == AriaHomeDefenseAction.None ||
                    decision.Action == AriaHomeDefenseAction.Unavailable)
                { if (++idle >= 2) break; continue; }
                idle = 0;
                actions.Add(decision.Action);
                switch (decision.Action)
                {
                    case AriaHomeDefenseAction.SelectSlot: view.ExpandedSelectedMask |= 1 << decision.Slot; break;
                    case AriaHomeDefenseAction.ToggleSelected: view.ExpandedSelectedMask &= view.ExpandedSelectedMask - 1; break;
                    case AriaHomeDefenseAction.ArmAttack: view.AttackMode = !view.AttackMode; break;
                    case AriaHomeDefenseAction.PageToDefender: view.ExpandedPageIndex = nearestPage; view.ExpandedSelectedMask = 0; break;
                    case AriaHomeDefenseAction.PreviousPage: view.ExpandedPageIndex--; view.ExpandedSelectedMask = 0; break;
                    case AriaHomeDefenseAction.NextPage: view.ExpandedPageIndex++; view.ExpandedSelectedMask = 0; break;
                }
                view.ExpandedSelectedGroups = Bits(view.ExpandedSelectedMask);
                view.ToggleAvailable = view.ExpandedSelectedGroups > 0;
                view.CompletedContacts++;
                view.Time += 1.75f;
                if (IsOrder(decision.Action)) break;
            }
            return actions;
        }

        [Test]
        public void DefenderMaskIsTheNearestCardPlusOneAntiArmourCardAtMost()
        {
            var view = Alert(0f); view.AntiArmorMask = 0b01000;
            view.HighlightedDefender = 4; // slot 3, anti-armour
            Assert.AreEqual(0b01000, AriaHomeDefensePolicy.DefenderMask(view), "the nearest card already fights armour");
            view.HighlightedDefender = 1; // slot 0, a rifle squad
            Assert.AreEqual(0b01001, AriaHomeDefensePolicy.DefenderMask(view), "nearest plus one anti-armour card");
            view.AntiArmorMask = 0;
            Assert.AreEqual(0b00001, AriaHomeDefensePolicy.DefenderMask(view), "no anti-armour on the page: nearest only");
            view.HighlightedDefender = 0; view.AntiArmorMask = 0b10100;
            Assert.AreEqual(0b00100, AriaHomeDefensePolicy.DefenderMask(view), "no highlight: the first anti-armour card");
            view.HighlightedDefender = 2; view.AvailableSquadMask = 0b11101;
            Assert.AreEqual(0b00100, AriaHomeDefensePolicy.DefenderMask(view), "a highlighted card that is not offered is ignored");
            view.AvailableSquadMask = 0b11111; view.ExpandedAssaultMask = 0b11101;
            Assert.AreEqual(0b00100, AriaHomeDefensePolicy.DefenderMask(view), "a highlighted support card is never sent");
            Assert.IsTrue(Bits(AriaHomeDefensePolicy.DefenderMask(view)) <= 2);
        }

        [Test]
        public void WarningWithTheAttackerOnScreenOrdersWithinThreeTouches()
        {
            // Base vision shows the tank next to the HQ; the nearest card (anti-armour) is on this page.
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f); view.AttackerVisibleAvailable = true; view.AntiArmorMask = 0b00100;
            var actions = PlayToOrder(ref view, ref plan, nearestPage: 1, nearestSlot: 2);
            Assert.AreEqual(AriaHomeDefensePolicy.ReasonBaseAlert, plan.HomeDefenseReason);
            Assert.IsTrue(actions.Count <= MaxTouchesToOrder, "touches to the order: " + string.Join(",", actions));
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, actions[0]);
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, actions[1]);
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerVisible, actions[2], "the tank itself, not home ground");
            Assert.AreEqual(0b00100, view.ExpandedSelectedMask, "only the nearest anti-armour card");
            Assert.IsFalse(actions.Contains(AriaHomeDefenseAction.InspectHome), "the target was already on screen");
            Assert.IsFalse(actions.Contains(AriaHomeDefenseAction.PreviousPage), "the nearest card's page is used as it is");
            Assert.IsTrue(view.Time - 67f <= 5.25f + .01f);
        }

        [Test]
        public void NearestCardOnAnotherPageIsReachedWithTheWarningsOwnControl()
        {
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f); view.ExpandedPageIndex = 2; view.AttackerGroundAvailable = true; view.AntiArmorMask = 0b00010;
            var actions = PlayToOrder(ref view, ref plan, nearestPage: 0, nearestSlot: 1);
            Assert.AreEqual(AriaHomeDefenseAction.PageToDefender, actions[0]);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, actions[1]);
            Assert.AreEqual(1, actions.FindAll(a => a == AriaHomeDefenseAction.PageToDefender).Count, "one paging press");
            Assert.IsFalse(actions.Contains(AriaHomeDefenseAction.PreviousPage), "no page-by-page walk");
            Assert.AreEqual(AriaHomeDefenseAction.AttackAttackerTerrain, actions[actions.Count - 1]);
            Assert.AreEqual(0b00010, view.ExpandedSelectedMask);
            Assert.IsTrue(actions.Count <= MaxTouchesToOrder + 1);
        }

        [Test]
        public void ShowHomeGuardWaitsForTheHighlightedCardThenSelectsItBeforeAttack()
        {
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f);
            view.DefenderKnown = view.DefenderPageAvailable = true;
            AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
            Assert.AreEqual(AriaHomeDefenseAction.PageToDefender, decision.Action);

            // SHOW moves the tray without selecting. Its highlighted slot can appear before the card offers a tap.
            view.CompletedContacts++;
            view.ExpandedPageIndex = 0;
            view.DefenderPageAvailable = false;
            view.HighlightedDefender = 3;
            view.AvailableSquadMask = 0;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.Waiting, decision.Action);

            view.AntiArmorMask = 0b00001;
            view.AvailableSquadMask = 0b00101;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(2, decision.Slot, "the next touch is the highlighted card even with a lower anti-armour slot");

            view.CompletedContacts++;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action, "a completed tap alone does not confirm selection");
            Assert.AreEqual(2, decision.Slot);

            view.ExpandedSelectedMask = 0b00100;
            view.ExpandedSelectedGroups = 1;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(0, decision.Slot);
            view.ExpandedSelectedMask |= 0b00001;
            view.ExpandedSelectedGroups++;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, decision.Action);
        }

        [Test]
        public void SelectionLostWhileAimingAttackReturnsToCardSelection()
        {
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f);
            view.DefenderKnown = true;
            view.HighlightedDefender = 2;
            view.ExpandedSelectedMask = 0b00010;
            view.ExpandedSelectedGroups = 1;
            AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
            Assert.AreEqual(AriaHomeDefenseAction.ArmAttack, decision.Action);

            view.ExpandedSelectedMask = view.ExpandedSelectedGroups = 0;
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.Waiting, decision.Action);
            AriaHomeDefensePolicy.Step(view, ref plan, out decision);
            Assert.AreEqual(AriaHomeDefenseAction.SelectSlot, decision.Action);
            Assert.AreEqual(1, decision.Slot);
        }

        [Test]
        public void ANearestRifleSquadIsJoinedByOneAntiArmourCardAndTheChoiceIsLatched()
        {
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f); view.AttackerVisibleAvailable = true; view.AntiArmorMask = 0b10000;
            var actions = new System.Collections.Generic.List<AriaHomeDefenseAction>();
            int highlight = 0; // slot 0 rifle; the highlight then moves to slot 1 mid-round
            for (int i = 0; i < 12 && (actions.Count == 0 || !IsOrder(actions[actions.Count - 1])); i++)
            {
                view.HighlightedDefender = highlight + 1; view.DefenderKnown = true;
                AriaHomeDefensePolicy.Step(view, ref plan, out var decision);
                if (decision.Action <= AriaHomeDefenseAction.Unavailable) continue;
                actions.Add(decision.Action);
                if (decision.Action == AriaHomeDefenseAction.SelectSlot) { view.ExpandedSelectedMask |= 1 << decision.Slot; highlight = 1; }
                if (decision.Action == AriaHomeDefenseAction.ArmAttack) view.AttackMode = true;
                view.ExpandedSelectedGroups = Bits(view.ExpandedSelectedMask);
                view.CompletedContacts++; view.Time += 1.75f;
            }
            Assert.AreEqual(0b10001, view.ExpandedSelectedMask, "rifle squad (nearest) + rocket squad; a moved highlight adds nothing");
            Assert.AreEqual(0b10001, plan.HomeDefenseMask);
            Assert.AreEqual(4, actions.Count, "two cards, Attack, the tank");
        }

        [Test]
        public void WithoutTheWarningTheOldTriggerAndPathRemain()
        {
            // Pre-HUD (no ISkirmishBaseAlertPresentation): the base-health drop keeps its reason, and with
            // no anti-armour card on this page the round still walks back to the first page.
            var plan = default(AriaHomeDefenseState);
            var view = Alert(67f); view.BaseAlert = false; view.HomeGroundAvailable = true;
            var actions = PlayToOrder(ref view, ref plan, nearestPage: 0, nearestSlot: -1);
            Assert.AreEqual(AriaHomeDefensePolicy.ReasonBaseDamage, plan.HomeDefenseReason);
            Assert.AreEqual(AriaHomeDefenseAction.PreviousPage, actions[0]);
            Assert.IsTrue(IsOrder(actions[actions.Count - 1]));
        }

        [Test]
        public void ARIAReadsOnlyThePresentedWarningAndYieldsTheNextTouch()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(ThisFile(), "..", "..", "..", ".."));
            string Read(string relative) => System.IO.File.ReadAllText(System.IO.Path.Combine(root, relative));
            string s002 = Read("Game/Scripts/Simulation/AriaGroundBaseAssaultPolicy.cs");
            Assert.IsTrue(s002.Contains("bool alerted=!view.Finished&&view.BaseAlertPresented;"), "the warning is a home-threat trigger");
            Assert.IsTrue(s002.Contains("if (view.PublicHomeThreat && plan.HomeDefenseStage == 0) YieldToHomeDefense(view, ref plan);"), "the defence takes the next touch");
            string watch = Read("Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.SkirmishWatch.cs");
            int start = watch.IndexOf("private void ObserveBaseAlert(", System.StringComparison.Ordinal);
            int end = watch.IndexOf("private void ObserveGroupRectangle(", start, System.StringComparison.Ordinal);
            Assert.IsTrue(start > 0 && end > start);
            string alert = watch.Substring(start, end - start);
            foreach (var hidden in new[] { "EntityManager", "World.", "GetComponentData", "IsVisibleToFaction", "CombatDamage" })
                Assert.IsFalse(alert.Contains(hidden), "the warning read touches no simulation state: " + hidden);
            Assert.IsTrue(alert.Contains("GetComponentInChildren<ISkirmishBaseAlertPresentation>"));
            // Every hostile the public projection reports is fog-visible to the player (faction 1).
            string projection = Read("Game/Scripts/Runtime/Skirmish/SkirmishAriaPublicProjection.cs");
            int hostileReads = Count(projection, "FactionId != 2") + Count(projection, "FactionId == 2");
            Assert.IsTrue(hostileReads >= 3);
            Assert.IsTrue(Count(projection, "SkirmishFogService.IsVisibleToFaction(em, unit, 1)") >= hostileReads,
                "each hostile read is fog-filtered");
            // ARIA's threat messages and target lock: a shooter hidden by fog is neither named, located nor
            // offered as an attack target (it used to be named in the voice line and lockable).
            string threats = Read("Game/Scripts/UI/Shell/Ecs/AssistantThreatReadModelSystem.cs");
            Assert.IsTrue(threats.Contains("!Game.Runtime.SkirmishFogService.IsVisibleToFaction(em, observation.SourceEntity, 1)"));
            Assert.IsTrue(threats.Contains("HostileSource = sourceHidden ? Entity.Null : observation.SourceEntity,"));
            Assert.IsTrue(threats.Contains("HostileWorldPosition = sourceHidden ? observation.TargetWorldPosition : observation.SourceWorldPosition,"));
        }

        static int Count(string text, string token)
        {
            int n = 0;
            for (int i = text.IndexOf(token, System.StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, System.StringComparison.Ordinal)) n++;
            return n;
        }

        static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
    }
}
