using Game.Simulation;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3 for air-capable Base Assault armies (lane 24 S004, army A, established start): every seed left
    // breacher, marksman, apc_fast, radar, AA and transport heli unused because the generic coverage was gated
    // off for AirProfile, and the starting AA (not an assault role) had no order at all. Pure policy checks;
    // ground armies (S002, S026) never offer the new kinds, so their masks and order are unchanged.
    public sealed class AriaAirCoverageTests
    {
        const int Marksman = 1, ApcFast = 2, Radar = 4, Drone = 16, TransportHeli = 32, Gunner = 64, AntiAir = 512, AttackHeli = 1024;

        public static void RunFocusedValidation()
        {
            var t = new AriaAirCoverageTests();
            t.AirCapableBaseAssaultIsCoverageEligible();
            t.NewKindBitsFollowTheProjectionRoleOrder();
            t.S004KindsAfterTheGroundKindsWithStartingAaAndHeliCovered();
            t.AttackHeliIsBoughtOnlyWhenTheAirCapHasRoom();
            t.AirOpeningKeepsItsSlotUntilQueuedOnce();
            t.GroundArmyMasksAreUnchanged();
            t.SupportSlotsCarryTheAaKindInThreeBits();
            t.StartingAaMovesBehindTheColumnOnce();
            t.SupportFieldedMapsTheAaBit();
            t.InterruptedPurchaseIsRetriedOnceOnlyForAirAssault();
            t.InterruptedBreacherIsRetriedOnce();
            t.TidyAaFocusesHomeThenMovesToHomeGroundAndDeselects();
            t.TidyPrefersTheUntriedTransportHeliOverTheReleasedAa();
            t.TidyDisarmTogglesUntilTheModeIsOff();
            t.GroundArmySupportOrderIsUnchanged();
            t.TidyTransportFocusSurvivesAimAndRequiresAcceptedGroundOrder();
            t.AirOpeningPrioritizesRadarAndKeepsFreedAirSlotsForCoverage();
            t.AirOpeningPagesToTransportAndCoversTheStartingArmoredApc();
            t.LostSupportSelectionIsReselectedAndCardPressesAreBounded();
            UnityEngine.Debug.Log("[AriaAirCoverage] result=Passed controls=19 normalMissionAcceptance=Pending");
        }

        static AriaCoverageView Ready(float time, int afford, int fielded) => new AriaCoverageView
        {
            AssaultIssued = true, BreacherSettled = true, EnemyAlive = true,
            AffordMask = afford, FieldedMask = fielded, CardMask = AriaCoverageRecruitPolicy.All, Time = time
        };

        [Test]
        public void AirCapableBaseAssaultIsCoverageEligible()
        {
            // The rule no longer looks at AirProfile: S004 (army A) and S026 (army G) both qualify.
            Assert.IsTrue(AriaCoverageEligibility.BaseAssault(true, true, false, false), "air-capable or ground base assault");
            Assert.IsFalse(AriaCoverageEligibility.BaseAssault(true, true, true, false), "Combined Arms keeps its own structure-force path");
            Assert.IsFalse(AriaCoverageEligibility.BaseAssault(true, true, false, true), "objective missions use the objective rule");
            Assert.IsFalse(AriaCoverageEligibility.BaseAssault(true, false, false, false), "not a base assault");
            Assert.IsFalse(AriaCoverageEligibility.BaseAssault(false, true, false, false), "legacy sessions");
        }

        [Test]
        public void NewKindBitsFollowTheProjectionRoleOrder()
        {
            // SkirmishAriaPublicProjection.CoverageRoles: ..., Siege (bit 256), AntiAir (512), AttackHeli (1024).
            Assert.AreEqual(AntiAir, AriaCoverageRecruitPolicy.Bit(AriaCoverageKind.AntiAir));
            Assert.AreEqual(AttackHeli, AriaCoverageRecruitPolicy.Bit(AriaCoverageKind.AttackHeli));
            Assert.AreEqual(2047, AriaCoverageRecruitPolicy.All);
            Assert.AreEqual(Drone | TransportHeli | AttackHeli, AriaCoverageRecruitPolicy.AirKinds);
        }

        [Test]
        public void S004KindsAfterTheGroundKindsWithStartingAaAndHeliCovered()
        {
            // S004 army A at ~35 s (lane 24 trace): affordMask=119, fieldedMask=96 (transport heli, gunner); the starting
            // AA is fielded too. Marksman, apc_fast and radar are bought one per visit kind; AA and the heli never are.
            var plan = default(AriaCoverageState);
            int fielded = TransportHeli | Gunner | AntiAir;
            var view = Ready(60f, Marksman | ApcFast | Radar | TransportHeli | Gunner | AntiAir, fielded);
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind);
            Assert.AreNotEqual(0, plan.Done & AntiAir, "the starting AA is covered at once (it gets a support order, not a purchase)");
            Assert.AreNotEqual(0, plan.Done & TransportHeli, "the starting transport heli is covered at once");
            view.PendingMask = Marksman; view.DrawerOpen = true; view.Time = 62f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind), "same drawer visit");
            Assert.AreEqual(AriaCoverageKind.ApcFast, kind);
            view.PendingMask = Marksman | ApcFast; view.Time = 64f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Radar, kind);
            view.PendingMask = Marksman | ApcFast | Radar; view.Time = 66f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "three kinds per visit");
            Assert.IsTrue(AriaCoverageProgress.Idle(plan.Done, plan.Current, view.AffordMask, 0), "nothing affordable left uncovered");
        }

        [Test]
        public void AttackHeliIsBoughtOnlyWhenTheAirCapHasRoom()
        {
            // The quote (SkirmishProductionService.EvaluateQueue) includes the tactical air cap: a full cap leaves the bit clear.
            var plan = new AriaCoverageState { Done = AriaCoverageRecruitPolicy.All & ~AttackHeli };
            var view = Ready(150f, 0, 0);
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _), "air cap full: waits");
            Assert.IsTrue(AriaCoverageProgress.Idle(plan.Done, plan.Current, view.AffordMask, 0), "an unaffordable kind does not hold research back");
            view.AffordMask = AttackHeli;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.AttackHeli, kind, "a freed slot buys the attack heli through its Helipad card");
        }

        [Test]
        public void AirOpeningKeepsItsSlotUntilQueuedOnce()
        {
            int afford = Marksman | Drone | AttackHeli;
            Assert.AreEqual(Marksman, AriaCoverageRecruitPolicy.AirOpeningAfford(afford, true, false, true),
                "the light attack heli is still to be queued: no drone or attack heli takes its slot");
            Assert.AreEqual(afford, AriaCoverageRecruitPolicy.AirOpeningAfford(afford, true, true, true),
                "after the opening, a slot freed by a loss goes to an unused air kind");
            Assert.AreEqual(afford, AriaCoverageRecruitPolicy.AirOpeningAfford(afford, true, false, false),
                "no opening on offer (no pad, unaffordable): coverage may use the slot");
        }

        [Test]
        public void GroundArmyMasksAreUnchanged()
        {
            // S002/S026 (army G) reject AA and attack heli as UnsupportedRole: bits 512/1024 stay 0, so the
            // purchase order and the air-opening rule are identities there.
            int afford = Marksman | ApcFast | Radar | 8 | Drone | TransportHeli;
            Assert.AreEqual(afford, AriaCoverageRecruitPolicy.AirOpeningAfford(afford, false, false, true));
            var plan = new AriaCoverageState { Done = Marksman | ApcFast | Radar | 8 | Drone | Gunner | 128 };
            var view = Ready(120f, afford, 0);
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.TransportHeli, kind, "the S026 order is unchanged");
        }

        [Test]
        public void SupportSlotsCarryTheAaKindInThreeBits()
        {
            int slots = AriaSupportOrderPolicy.Slots(AriaSupportKind.Radar, AriaSupportKind.Drone, AriaSupportKind.TransportHeli,
                AriaSupportKind.None, AriaSupportKind.AntiAir);
            Assert.AreEqual(AriaSupportKind.Radar, AriaSupportOrderPolicy.SlotKind(slots, 0));
            Assert.AreEqual(AriaSupportKind.Drone, AriaSupportOrderPolicy.SlotKind(slots, 1));
            Assert.AreEqual(AriaSupportKind.TransportHeli, AriaSupportOrderPolicy.SlotKind(slots, 2));
            Assert.AreEqual(AriaSupportKind.None, AriaSupportOrderPolicy.SlotKind(slots, 3));
            Assert.AreEqual(AriaSupportKind.AntiAir, AriaSupportOrderPolicy.SlotKind(slots, 4));
            Assert.AreEqual(8, AriaSupportOrderPolicy.KindBit(AriaSupportKind.AntiAir));
        }

        [Test]
        public void StartingAaMovesBehindTheColumnOnce()
        {
            var plan = default(AriaSupportState);
            var view = new AriaSupportView
            {
                FieldedMask = AriaSupportOrderPolicy.KindBit(AriaSupportKind.AntiAir), ArmyMoving = true, EnemyAlive = true,
                ToggleAvailable = true, MoveAvailable = true, AttackAvailable = true, ScanAvailable = true, RearAvailable = true, FrontAvailable = true,
                SupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.None, AriaSupportKind.AntiAir, AriaSupportKind.None, AriaSupportKind.None, AriaSupportKind.None),
                AvailableSquadMask = 0b11111, Time = 90f
            };
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action);
            Assert.AreEqual(1, d.Slot);
            Assert.AreEqual(AriaSupportKind.AntiAir, d.Kind);
            view.SelectedMask = 0b00010; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 91f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "the AA gets an ordinary Move, not a Scan");
            view.Mode = AriaSupportMode.Move; view.CompletedContacts = 2; view.Time = 92f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.TargetRear, d.Action, "clear ground behind the presented column");
            view.Mode = AriaSupportMode.None; view.CompletedContacts = 3; view.Time = 93f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "ordered");
            Assert.AreEqual(8, plan.Done);
            view.SelectedGroups = 0; view.SelectedMask = 0; view.Time = 200f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _), "one order per kind per match");
        }

        [Test]
        public void InterruptedPurchaseIsRetriedOnceOnlyForAirAssault()
        {
            // Lane 26 S004: the base alert's home defence (Busy) interrupted purchases, which were then given up for the match.
            var plan = default(AriaCoverageState);
            var view = Ready(90f, Marksman | ApcFast, 0); view.RetryInterrupted = true;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind);
            view.Busy = true; view.Time = 92f;
            Assert.AreEqual(AriaCoverageAction.None, AriaCoverageRecruitPolicy.Step(view, ref plan, out _));
            Assert.AreEqual(0, plan.Done & Marksman, "released, not given up");
            Assert.AreEqual(Marksman, plan.Interrupted & Marksman);
            view.Busy = false; view.Time = 92f + AriaCoverageRecruitPolicy.GapSeconds + .1f;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Marksman, kind, "retried on the next visit");
            view.Busy = true; view.Time += 1f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(Marksman, plan.Done & Marksman, "a second interruption gives the kind up (bounded)");

            // Ground armies (S002, S026): unchanged, the first interruption gives up.
            plan = default; view = Ready(90f, Marksman, 0);
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            view.Busy = true; view.Time = 92f;
            AriaCoverageRecruitPolicy.Step(view, ref plan, out _);
            Assert.AreEqual(Marksman, plan.Done & Marksman);
        }

        [Test]
        public void InterruptedBreacherIsRetriedOnce()
        {
            var plan = default(AriaBreacherState);
            var view = new AriaBreacherView { AssaultIssued = true, EnemyAlive = true, CanAffordBreacher = true, RecruitAvailable = true,
                RetryInterrupted = true, Time = 80f };
            Assert.AreEqual(AriaBreacherAction.Recruit, AriaBreacherPolicy.Step(view, ref plan));
            view.Busy = true; view.Time = 82f;
            Assert.AreEqual(AriaBreacherAction.None, AriaBreacherPolicy.Step(view, ref plan));
            Assert.AreEqual(0, plan.Stage, "released for one retry");
            view.Busy = false; view.Time = 120f;
            Assert.AreEqual(AriaBreacherAction.Recruit, AriaBreacherPolicy.Step(view, ref plan));
            view.Busy = true; view.Time = 121f;
            AriaBreacherPolicy.Step(view, ref plan);
            Assert.AreEqual(2, plan.Stage, "second interruption: done for the match");
        }

        static AriaSupportView Tidy(float time, AriaSupportKind slot1, AriaSupportKind slot2) => new AriaSupportView
        {
            FieldedMask = AriaSupportOrderPolicy.KindBit(slot1) | AriaSupportOrderPolicy.KindBit(slot2), ArmyMoving = true, EnemyAlive = true,
            ToggleAvailable = true, MoveAvailable = true, AttackAvailable = true, ScanAvailable = true, FocusAvailable = true, Tidy = true,
            SupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.None, slot1, slot2, AriaSupportKind.None, AriaSupportKind.None),
            AvailableSquadMask = 0b11111, Time = time
        };

        [Test]
        public void TidyAaFocusesHomeThenMovesToHomeGroundAndDeselects()
        {
            // Lane 26: rear=False after the AA card was tapped in every job, so the Move tap never happened.
            var plan = default(AriaSupportState);
            var view = Tidy(90f, AriaSupportKind.AntiAir, AriaSupportKind.None);
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action);
            view.SelectedMask = 0b10; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 91f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Focus, d.Action, "no Move ground on screen: one Home focus before arming");
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Focus, d.Action, "through the focus tap's aim delay");
            view.CompletedContacts = 2; view.Time = 92f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "never a second focus");
            view.Mode = AriaSupportMode.Move; view.CompletedContacts = 3; view.HomeAvailable = true; view.RearAvailable = true; view.Time = 93f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.TargetHome, d.Action, "the AA covers home even when the column rear is shown");
            view.Mode = AriaSupportMode.None; view.CompletedContacts = 4; view.Time = 94f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace, "ordered");
            Assert.AreEqual(8, plan.Done);
            Assert.AreEqual(AriaSupportOrderPolicy.StageDeselect, plan.Stage);
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ToggleSelected, d.Action, "the AA is deselected for the next owner");
            view.CompletedContacts = 5; view.SelectedGroups = 0; view.SelectedMask = 0; view.Time = 95f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out _));
            Assert.AreEqual(0, plan.Stage);
        }

        [Test]
        public void TidyPrefersTheUntriedTransportHeliOverTheReleasedAa()
        {
            var plan = new AriaSupportState { Tried = 8 }; // the AA was released once (lane 26 jobs 1-3)
            var view = Tidy(120f, AriaSupportKind.AntiAir, AriaSupportKind.TransportHeli);
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportKind.TransportHeli, d.Kind);
            Assert.AreEqual(2, d.Slot);
            plan = new AriaSupportState { Tried = 8 }; view.Tidy = false;
            AriaSupportOrderPolicy.Step(view, ref plan, out d);
            Assert.AreEqual(AriaSupportKind.AntiAir, d.Kind, "ground armies keep the card order");
        }

        [Test]
        public void TidyDisarmTogglesUntilTheModeIsOff()
        {
            // Lane 26: the single disarm press completed but Move stayed armed for the home defence.
            var plan = new AriaSupportState { Stage = 3, Kind = (byte)AriaSupportKind.AntiAir, Slot = 1, Actions = 10, Deadline = 102f };
            var view = Tidy(100f, AriaSupportKind.AntiAir, AriaSupportKind.None);
            view.Mode = AriaSupportMode.Move; view.SelectedGroups = 1; view.SelectedMask = 0b10; view.CompletedContacts = 10; view.Busy = true;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "toggle the armed Move off");
            view.CompletedContacts = 11; view.Time = 100.2f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Waiting, d.Action, "settle before judging the press");
            view.Time = 100.6f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action, "still armed: a second press");
            view.Mode = AriaSupportMode.None; view.Time = 100.8f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(3, d.Trace, "released");
            Assert.AreEqual(0, plan.Stage, "busy: the owner that took over handles the selection");
        }

        [Test]
        public void GroundArmySupportOrderIsUnchanged()
        {
            var plan = new AriaSupportState { Stage = 3, Kind = (byte)AriaSupportKind.Radar, Slot = 1, Actions = 10, Deadline = 102f };
            var view = Tidy(100f, AriaSupportKind.Radar, AriaSupportKind.None); view.Tidy = false; view.HomeAvailable = true;
            view.Mode = AriaSupportMode.Move; view.SelectedGroups = 1; view.SelectedMask = 0b10; view.CompletedContacts = 10;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.Waiting, d.Action, "the radar waits for the column rear; home ground is air-assault only");
            view.Busy = true;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action);
            view.CompletedContacts = 11; view.Time = 100.1f;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d), "one disarm press, then released");
            Assert.AreEqual(3, d.Trace);
            Assert.AreEqual(0, plan.Stage);
        }

        [Test]
        public void LostSupportSelectionIsReselectedAndCardPressesAreBounded()
        {
            // W27c (W16 proof S010 130365: Blocked 115 on Move, intent Advance, supportStage=2, selectedGroups=0).
            var plan = default(AriaSupportState);
            var view = new AriaSupportView
            {
                FieldedMask = AriaSupportOrderPolicy.KindBit(AriaSupportKind.AntiAir), ArmyMoving = true, EnemyAlive = true,
                ToggleAvailable = true, MoveAvailable = true, AttackAvailable = true, ScanAvailable = true, RearAvailable = true, FrontAvailable = true,
                SupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.None, AriaSupportKind.AntiAir, AriaSupportKind.None, AriaSupportKind.None, AriaSupportKind.None),
                AvailableSquadMask = 0b11111, Time = 90f
            };
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action);
            view.SelectedMask = 0b00010; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 91f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action);
            Assert.AreEqual(2, plan.Stage);
            // The selection is lost before Move arms: select the unit again instead of pressing Move with nothing selected.
            view.SelectedMask = 0; view.SelectedGroups = 0; view.CompletedContacts = 2; view.Time = 92f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action, "re-select, never Move with selectedGroups=0");
            Assert.AreEqual(1, d.Slot);
            // Card presses that never select the unit end the order before the input layer's fourth press.
            int selects = 1;
            for (int contacts = 3; contacts < 10; contacts++)
            {
                view.CompletedContacts = contacts; view.Time += 1f;
                bool handled = AriaSupportOrderPolicy.Step(view, ref plan, out d);
                if (!handled || d.Action != AriaSupportAction.SelectSlot) break;
                selects++;
            }
            Assert.LessOrEqual(selects, 3, "at most three consecutive presses of the card");
            Assert.AreNotEqual(AriaSupportAction.ArmMove, d.Action);
            Assert.AreEqual(0, plan.Stage, "released for the ordinary cooldown");
        }

        [Test]
        public void SupportFieldedMapsTheAaBit()
        {
            Assert.AreEqual(1 | 4 | 8, AriaSkirmishPlanSystem.SupportFielded(Radar | TransportHeli | AntiAir | Gunner));
            Assert.AreEqual(2, AriaSkirmishPlanSystem.SupportFielded(Drone | AttackHeli), "the attack heli is an assault role, ordered by the sweep");
        }

        [Test]
        public void TidyTransportFocusSurvivesAimAndRequiresAcceptedGroundOrder()
        {
            var plan = default(AriaSupportState);
            var view = Tidy(80f, AriaSupportKind.AntiAir, AriaSupportKind.TransportHeli);
            view.RequireOrderResult = true;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out var d));
            Assert.AreEqual(AriaSupportKind.TransportHeli, d.Kind);
            Assert.AreEqual(AriaSupportAction.SelectSlot, d.Action);
            view.SelectedMask = 4; view.SelectedGroups = 1; view.CompletedContacts = 1; view.Time = 81f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Focus, d.Action);
            view.Time = 81.9f;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Focus, d.Action, "the finger still needs the same control after aiming");
            view.CompletedContacts = 2; view.Time = 83f; view.HomeAvailable = true;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.ArmMove, d.Action);
            view.CompletedContacts = 3; view.Time = 85f; view.Mode = AriaSupportMode.Move;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.TargetHome, d.Action);
            view.CompletedContacts = 4; view.Time = 87f; view.Mode = AriaSupportMode.None;
            Assert.IsTrue(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(AriaSupportAction.Waiting, d.Action);
            Assert.AreEqual(0, plan.Done);
            var rejected = plan;
            view.OrderResultAvailable = true;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref rejected, out d));
            Assert.AreEqual(3, d.Trace, "a rejected ground tap releases for a retry");
            Assert.AreEqual(0, rejected.Done);
            view.OrderAccepted = true;
            Assert.IsFalse(AriaSupportOrderPolicy.Step(view, ref plan, out d));
            Assert.AreEqual(2, d.Trace);
            Assert.AreEqual(4, plan.Done);
        }

        [Test]
        public void AirOpeningPrioritizesRadarAndKeepsFreedAirSlotsForCoverage()
        {
            var plan = default(AriaCoverageState);
            var view = Ready(55f, Radar | Marksman | ApcFast, TransportHeli);
            view.PrioritizeAirCoverage = true;
            Assert.IsFalse(AriaCoverageRecruitPolicy.CanBuyOpeningTanker(true, plan.Done));
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out var kind));
            Assert.AreEqual(AriaCoverageKind.Radar, kind, "buy radar before cheaper kinds consume the budget");
            Assert.IsTrue(AriaCoverageRecruitPolicy.CanBuyOpeningTanker(true, Radar | Marksman | ApcFast));

            Assert.IsTrue(AriaCoverageRecruitPolicy.CanBuyLightAttackHeli(true, false, 0), "keep the first light heli");
            Assert.IsFalse(AriaCoverageRecruitPolicy.CanBuyLightAttackHeli(true, true, AttackHeli));
            Assert.IsTrue(AriaCoverageRecruitPolicy.CanBuyLightAttackHeli(true, true, AttackHeli | Drone));
            plan = default;
            view.AffordMask = Radar | AttackHeli | Drone;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.AttackHeli, kind, "use a newly free and affordable slot");
            view.AffordMask = Radar | Drone; plan = default;
            Assert.AreEqual(AriaCoverageAction.Recruit, AriaCoverageRecruitPolicy.Step(view, ref plan, out kind));
            Assert.AreEqual(AriaCoverageKind.Drone, kind, "the heavy gunship quote may still be too expensive");
        }

        [Test]
        public void AirOpeningPagesToTransportAndCoversTheStartingArmoredApc()
        {
            var plan = new AriaSkirmishPlanComponent { CoverageAirOpened = 1 };
            var touch = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var output = default(AriaPlayObservationComponent);
            var view = new AriaSkirmishObservation
            {
                Active = true, ExpandedSession = true, ExpandedBaseAssault = true, AirProfile = true,
                EnemyDesignatedAlive = true, CoverageFieldedMask = TransportHeli, Time = 40f,
                ExpandedPageCount = 3, NextSquadPage = new AriaTouchTarget { Id = 10, Available = true }
            };
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(10, output.TargetId, "use the visible page control to reach the starting transport");
            view.ExpandedPageIndex = 1;
            view.ExpandedSupportSlots = AriaSupportOrderPolicy.Slots(AriaSupportKind.ApcArmored, AriaSupportKind.TransportHeli,
                AriaSupportKind.None, AriaSupportKind.None, AriaSupportKind.None);
            view.Squad0 = new AriaTouchTarget { Id = 11, Available = true };
            view.Squad1 = new AriaTouchTarget { Id = 12, Available = true };
            view.Time = 42f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(12, output.TargetId);
            plan.SupportStage = 0; plan.SupportDone = 4;
            view.Time = 49f;
            AriaSkirmishPlanSystem.Step(view, ref plan, ref touch, ref output);
            Assert.AreEqual(11, output.TargetId, "the starting APC has a normal roster order path");
            Assert.AreEqual((byte)AriaSupportKind.ApcArmored, plan.SupportKind);
        }
    }
}
