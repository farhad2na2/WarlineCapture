using Game.Simulation;
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
            UnityEngine.Debug.Log("[AriaAirCoverage] result=Passed controls=9 normalMissionAcceptance=Pending");
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
        public void SupportFieldedMapsTheAaBit()
        {
            Assert.AreEqual(1 | 4 | 8, AriaSkirmishPlanSystem.SupportFielded(Radar | TransportHeli | AntiAir | Gunner));
            Assert.AreEqual(2, AriaSkirmishPlanSystem.SupportFielded(Drone | AttackHeli), "the attack heli is an assault role, ordered by the sweep");
        }
    }
}
