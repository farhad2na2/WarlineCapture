using System.Collections.Generic;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // FC/BT/CE objective skill (AriaSkirmishPlanSystem.ObjectiveSkill, AriaObjectiveSkillPolicy,
    // AriaObjectiveEscortPolicy, SkirmishPlayerObjectivePolicyUtilitySystemHelper BT corridor rule).
    // Lane 22: S007/S013/S019 stopped at 8.5 s with [AriaBlocked] reason=101 intent=Recruit because the first
    // observation had no rendered objective frame and the old path blocked immediately.
    public sealed class AriaObjectiveSkillTests
    {
        public static void RunFocusedValidation()
        {
            var t = new AriaObjectiveSkillTests();
            t.MissingFrameIsWaitedForThenBlocks();
            t.OwnPopupIsFinishedThenClosedThenBlocks();
            t.FailedOrdersAreRetriedBoundedAndEarnedBack();
            t.PublicProgressRevisionRenewsOnlyOnChange();
            t.TypesTargetsAndOriginalHoldBack();
            t.EscortPicksOwnGroundVehiclesOnly();
            t.EscortGoalFollowsCommittedZoneExitAndDestination();
            t.BreakthroughCorridorPrefersSupportInfantry();
            t.Lane22FirstObservationWithoutFrameDoesNotBlock();
            t.OwnDrawerPurchaseContinuesWithoutFrame();
            t.UnownedDrawerIsClosed();
            t.ObjectiveOrderTimeoutIsRetriedNotBlocked();
            t.ProgressRenewsInputWatchdogs();
            t.EscortRunsWhenNoObjectiveOrderIsWanted();
            t.FinishedObjectiveHandsBack();
            t.RejectedObjectivePressesAreRetriedBeforeTheInputLayerStops();
            t.RosterChangeBetweenSelectPressesKeepsTheInputBound();
            t.EscortGetsATurnAfterAnAcceptedObjectiveOrder();
            t.StuckSupportRecruitIsBoundedWhileTheDrawerIsOpen();
            t.FrontlineEscortsGarrisonTheThinnerOwnedZone();
            t.FrontlineGarrisonZoneStaysChosenUntilHeldOrLost();
            t.GarrisonOfferSkipsHoldersAndPrefersUncommittedGroups();
            UnityEngine.Debug.Log("[AriaObjectiveSkill] result=Passed controls=22");
        }

        // ---------- pure policy ----------

        [Test]
        public void MissingFrameIsWaitedForThenBlocks()
        {
            var s = default(AriaObjectiveFrameState);
            Assert.AreEqual(AriaObjectiveFrameAction.Proceed, AriaObjectiveSkillPolicy.Frame(true, false, 10, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Wait, AriaObjectiveSkillPolicy.Frame(false, false, 11, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Wait, AriaObjectiveSkillPolicy.Frame(false, false, 11 + AriaObjectiveSkillPolicy.FrameGraceSeconds, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Proceed, AriaObjectiveSkillPolicy.Frame(true, false, 30, ref s), "a returning frame resets the grace");
            Assert.AreEqual(AriaObjectiveFrameAction.Wait, AriaObjectiveSkillPolicy.Frame(false, false, 31, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Block, AriaObjectiveSkillPolicy.Frame(false, false, 31.1f + AriaObjectiveSkillPolicy.FrameGraceSeconds, ref s));
        }

        [Test]
        public void OwnPopupIsFinishedThenClosedThenBlocks()
        {
            var s = default(AriaObjectiveFrameState);
            Assert.AreEqual(AriaObjectiveFrameAction.Popup, AriaObjectiveSkillPolicy.Frame(false, true, 100, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Popup, AriaObjectiveSkillPolicy.Frame(false, true, 100 + AriaObjectiveSkillPolicy.PopupLimitSeconds, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.ClosePopup, AriaObjectiveSkillPolicy.Frame(false, true, 100.5f + AriaObjectiveSkillPolicy.PopupLimitSeconds, ref s));
            Assert.AreEqual(AriaObjectiveFrameAction.Block, AriaObjectiveSkillPolicy.Frame(false, true,
                100.5f + AriaObjectiveSkillPolicy.PopupLimitSeconds + AriaObjectiveSkillPolicy.PopupCloseSeconds, ref s));
            s = default;
            AriaObjectiveSkillPolicy.Frame(false, true, 0, ref s);
            Assert.AreEqual(AriaObjectiveFrameAction.Wait, AriaObjectiveSkillPolicy.Frame(false, false, 50, ref s), "a closed popup starts the ordinary grace");
        }

        [Test]
        public void FailedOrdersAreRetriedBoundedAndEarnedBack()
        {
            var s = default(AriaObjectiveOrderState);
            for (int i = 1; i < AriaObjectiveSkillPolicy.MaxOrderFailures; i++)
            {
                Assert.IsTrue(AriaObjectiveSkillPolicy.AbsorbFailure(ref s, 10 * i, AriaObjectiveSkillPolicy.MaxOrderFailures));
                Assert.IsFalse(AriaObjectiveSkillPolicy.Ready(s, 10 * i + 1));
                Assert.IsTrue(AriaObjectiveSkillPolicy.Ready(s, 10 * i + AriaObjectiveSkillPolicy.OrderRetrySeconds));
            }
            AriaObjectiveSkillPolicy.Accepted(ref s);
            Assert.AreEqual(AriaObjectiveSkillPolicy.MaxOrderFailures - 2, s.Failures);
            Assert.IsTrue(AriaObjectiveSkillPolicy.AbsorbFailure(ref s, 100, AriaObjectiveSkillPolicy.MaxOrderFailures));
            Assert.IsFalse(AriaObjectiveSkillPolicy.AbsorbFailure(ref s, 110, AriaObjectiveSkillPolicy.MaxOrderFailures));
        }

        [Test]
        public void PublicProgressRevisionRenewsOnlyOnChange()
        {
            var s = default(AriaObjectiveProgressState);
            Assert.IsFalse(AriaObjectiveSkillPolicy.Progress(false, 5, 1, ref s));
            Assert.IsTrue(AriaObjectiveSkillPolicy.Progress(true, 5, 2, ref s));
            Assert.IsFalse(AriaObjectiveSkillPolicy.Progress(true, 5, 3, ref s));
            Assert.IsTrue(AriaObjectiveSkillPolicy.Progress(true, 6, 4, ref s));
            Assert.AreEqual(4, s.LastAt);
        }

        [Test]
        public void TypesTargetsAndOriginalHoldBack()
        {
            Assert.AreEqual(AriaObjectiveType.FrontlineControl, AriaObjectiveSkillPolicy.TypeOf(SkirmishPublicGoalPerspective.Capture));
            Assert.AreEqual(AriaObjectiveType.Breakthrough, AriaObjectiveSkillPolicy.TypeOf(SkirmishPublicGoalPerspective.Evacuate));
            Assert.AreEqual(AriaObjectiveType.ConvoyEscort, AriaObjectiveSkillPolicy.TypeOf(SkirmishPublicGoalPerspective.Escort));
            Assert.AreEqual(AriaObjectiveType.None, AriaObjectiveSkillPolicy.TypeOf(SkirmishPublicGoalPerspective.BaseAssault));
            Assert.Greater(AriaObjectiveSkillPolicy.InfantryTarget(AriaObjectiveType.FrontlineControl), 12);
            Assert.Greater(AriaObjectiveSkillPolicy.InfantryTarget(AriaObjectiveType.Breakthrough), 12, "BT buys support beyond the 12 originals");
            Assert.AreEqual(0, AriaObjectiveSkillPolicy.InfantryTarget(AriaObjectiveType.None));
            var bt = AriaObjectiveType.Breakthrough;
            Assert.IsTrue(AriaObjectiveSkillPolicy.HoldBackOriginals(bt, false, 10, true, false));
            Assert.IsFalse(AriaObjectiveSkillPolicy.HoldBackOriginals(bt, true, 10, true, false), "an open exit needs the originals");
            Assert.IsFalse(AriaObjectiveSkillPolicy.HoldBackOriginals(bt, false, 10, true, true), "support infantry exists");
            Assert.IsFalse(AriaObjectiveSkillPolicy.HoldBackOriginals(bt, false, AriaObjectiveSkillPolicy.OriginalHoldSeconds, true, false), "bounded");
            Assert.IsFalse(AriaObjectiveSkillPolicy.HoldBackOriginals(bt, false, 10, false, false));
            Assert.IsFalse(AriaObjectiveSkillPolicy.HoldBackOriginals(AriaObjectiveType.FrontlineControl, false, 10, true, false));
        }

        [Test]
        public void EscortPicksOwnGroundVehiclesOnly()
        {
            AriaEscortCandidate C(uint id, SkirmishRoleKind role, SkirmishPopulationCategory domain) => new AriaEscortCandidate
            { GroupId = id, Role = role, Domain = domain, CanMove = true, Target = true };
            var list = new List<AriaEscortCandidate>
            {
                C(2, SkirmishRoleKind.Rifle, SkirmishPopulationCategory.Infantry),
                C(3, SkirmishRoleKind.Radar, SkirmishPopulationCategory.Ground),
                C(4, SkirmishRoleKind.LogisticsTruck, SkirmishPopulationCategory.LogisticsSupport),
                C(9, SkirmishRoleKind.Tank, SkirmishPopulationCategory.Ground),
                C(7, SkirmishRoleKind.ApcArmored, SkirmishPopulationCategory.Ground),
            };
            var originals = C(5, SkirmishRoleKind.ConvoyLogic, SkirmishPopulationCategory.Ground); originals.Originals = true; list.Add(originals);
            Assert.AreEqual(7u, list[AriaObjectiveEscortPolicy.Pick(list)].GroupId, "lowest eligible vehicle group");
            var done = list[4]; done.Done = true; list[4] = done;
            Assert.AreEqual(9u, list[AriaObjectiveEscortPolicy.Pick(list)].GroupId);
            var hidden = list[3]; hidden.Target = false; list[3] = hidden;
            Assert.AreEqual(-1, AriaObjectiveEscortPolicy.Pick(list), "no shown target, radar keeps its support order, originals are never re-tasked");
        }

        [Test]
        public void EscortGoalFollowsCommittedZoneExitAndDestination()
        {
            var fc = Frontline(0, 0, 0, contestedC: false);
            var groups = new List<SkirmishPublicPlayerGroup>
            { new SkirmishPublicPlayerGroup { GroupId = 1, Own = true, Living = true, CombatInfantry = true, Dismounted = true, AcceptedOrderActive = true, AcceptedGoalId = "zone.b" } };
            Assert.AreEqual("zone.b", AriaObjectiveEscortPolicy.Goal(fc, groups, "zone.a"));
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(fc, new List<SkirmishPublicPlayerGroup>(), "zone.a"));
            Assert.AreEqual("zone.c", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 0, contestedC: true), groups, null), "contested zone first");
            Assert.AreEqual("exit", AriaObjectiveEscortPolicy.Goal(Breakthrough(true, false), null, null));
            Assert.AreEqual("corridor.a", AriaObjectiveEscortPolicy.Goal(Breakthrough(false, false), null, null));
            Assert.AreEqual("destination", AriaObjectiveEscortPolicy.Goal(Convoy(), null, null));
        }

        [Test]
        public void FrontlineEscortsGarrisonTheThinnerOwnedZone()
        {
            SkirmishPublicPlayerGroup G(uint id, string goal, bool infantry) => new SkirmishPublicPlayerGroup
            { GroupId = id, Own = true, Living = true, CombatInfantry = infantry, Dismounted = infantry, AcceptedOrderActive = goal != null, AcceptedGoalId = goal };
            // W16 S010/S012: infantry holds zone.a; zone.b is also ours with nobody on it -> escorts go to zone.b.
            var holding = new List<SkirmishPublicPlayerGroup> { G(1, "zone.a", true), G(2, null, true), G(3, null, false) };
            Assert.AreEqual("zone.b", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, contestedC: false), holding, null), "empty owned zone is garrisoned");
            // Once zone.b has as many own groups (any domain) as zone.a, escorts return to the committed zone.
            var balanced = new List<SkirmishPublicPlayerGroup> { G(1, "zone.a", true), G(3, "zone.b", false) };
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, contestedC: false), balanced, null), "balanced: committed zone");
            // One owned zone: the committed (owned) zone is kept; escorts never go alone to unowned zones.
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 0, 2, contestedC: false), holding, null), "one owned zone unchanged");
            // A contested zone still comes first.
            Assert.AreEqual("zone.c", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 1, contestedC: true), holding, null), "contested zone first");
            // The committed zone is not ours (attack in progress): unchanged.
            var attacking = new List<SkirmishPublicPlayerGroup> { G(1, "zone.c", true) };
            Assert.AreEqual("zone.c", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 0, contestedC: false), attacking, null), "attack support unchanged");
            // Not FC: unchanged.
            Assert.AreEqual("corridor.a", AriaObjectiveEscortPolicy.Goal(Breakthrough(false, false), holding, null));
        }

        [Test]
        public void FrontlineGarrisonZoneStaysChosenUntilHeldOrLost()
        {
            SkirmishPublicPlayerGroup G(uint id, string goal, bool infantry) => new SkirmishPublicPlayerGroup
            { GroupId = id, Own = true, Living = true, CombatInfantry = infantry, Dismounted = infantry, AcceptedOrderActive = goal != null, AcceptedGoalId = goal };
            var holding = new List<SkirmishPublicPlayerGroup> { G(1, "zone.a", true), G(2, null, true), G(3, null, false) };
            var s = default(AriaGarrisonState);
            Assert.AreEqual("zone.b", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false), holding, null, ref s));
            Assert.AreEqual("zone.b", s.Zone); Assert.AreEqual("zone.a", s.Committed);
            // W16 proof (attempt 1): the wave contesting zone.a flipped every escort back to zone.a.
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false, contestedA: true), holding, null), "stateless: flips");
            Assert.AreEqual("zone.b", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false, contestedA: true), holding, null, ref s),
                "the chosen garrison zone is kept while it is ours and nobody holds it");
            // The garrisoning group's Move is accepted: other escorts return to the committed zone; the choice is kept.
            AriaObjectiveEscortPolicy.BindGarrison(ref s, 3); s.Ordered = true;
            AriaObjectiveEscortPolicy.BindGarrison(ref s, 2);
            Assert.AreEqual(3u, s.Group, "the first bound group keeps the garrison");
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false, contestedA: true), holding, null, ref s));
            Assert.AreEqual("zone.b", s.Zone);
            // It dies before arriving: zone.b is offered again.
            var died = new List<SkirmishPublicPlayerGroup> { G(1, "zone.a", true), G(2, null, true) };
            Assert.AreEqual("zone.b", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false, contestedA: true), died, null, ref s));
            Assert.AreEqual(0u, s.Group); Assert.IsFalse(s.Ordered);
            // A group arrives and holds zone.b: the choice ends (balanced, committed zone).
            var arrived = new List<SkirmishPublicPlayerGroup> { G(1, "zone.a", true), G(3, "zone.b", false) };
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false), arrived, null, ref s));
            Assert.IsNull(s.Zone);
            // zone.b is lost: the choice ends.
            s = default;
            AriaObjectiveEscortPolicy.Goal(Frontline(1, 1, 2, false), holding, null, ref s); Assert.AreEqual("zone.b", s.Zone);
            Assert.AreEqual("zone.a", AriaObjectiveEscortPolicy.Goal(Frontline(1, 2, 2, false), holding, null, ref s));
            Assert.IsNull(s.Zone);
            // Not FC: no garrison state.
            s = new AriaGarrisonState { Zone = "zone.b" };
            Assert.AreEqual("corridor.a", AriaObjectiveEscortPolicy.Goal(Breakthrough(false, false), holding, null, ref s));
            Assert.IsNull(s.Zone);
        }

        [Test]
        public void GarrisonOfferSkipsHoldersAndPrefersUncommittedGroups()
        {
            AriaEscortCandidate C(uint id) => new AriaEscortCandidate
            { GroupId = id, Role = SkirmishRoleKind.Tank, Domain = SkirmishPopulationCategory.Ground, CanMove = true, Target = true };
            var holder = C(1); holder.Holding = true;
            var committed = C(2); committed.Committed = true;
            var list = new List<AriaEscortCandidate> { holder, committed, C(4) };
            // W16 proof S060: the group holding zone A was sent to garrison zone B and the chooser then swapped holders.
            Assert.AreEqual(4u, list[AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl, true)].GroupId);
            Assert.AreEqual(1u, list[AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl)].GroupId, "other escorts unchanged");
            list.RemoveAt(2);
            Assert.AreEqual(2u, list[AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl, true)].GroupId,
                "a group sent to the committed zone only when no other is free");
            list.RemoveAt(1);
            Assert.AreEqual(-1, AriaObjectiveEscortPolicy.Pick(list, AriaObjectiveType.FrontlineControl, true), "never the holder");
        }

        [Test]
        public void BreakthroughCorridorPrefersSupportInfantry()
        {
            var goal = Breakthrough(false, true);
            var approaches = new List<SkirmishPublicPlayerApproach>();
            foreach (uint g in new uint[] { 1, 2 })
                foreach (var id in new[] { "corridor.a", "corridor.b" })
                    approaches.Add(new SkirmishPublicPlayerApproach { GroupId = g, GoalId = id, Inspected = true, GroundCompatible = true });
            var originals = new SkirmishPublicPlayerGroup { GroupId = 1, Own = true, Living = true, CombatInfantry = true, Dismounted = true,
                CanMove = true, CanHold = true, PositionKnown = true, DesignatedSubjects = new[] { "rifle.1" } };
            var support = new SkirmishPublicPlayerGroup { GroupId = 2, Own = true, Living = true, CombatInfantry = true, Dismounted = true,
                CanMove = true, CanHold = true, PositionKnown = true };
            var alone = SkirmishPlayerObjectivePolicyUtilitySystemHelper.Choose(goal, new[] { originals }, approaches, default);
            Assert.AreEqual(1u, alone.GroupId, "without support infantry the originals may open the corridor");
            var both = SkirmishPlayerObjectivePolicyUtilitySystemHelper.Choose(goal, new[] { originals, support }, approaches, default);
            Assert.AreEqual(2u, both.GroupId, "support infantry opens the corridor; originals are kept for the exit");
            Assert.AreEqual(SkirmishPublicPlayerAction.Move, both.Action);
        }

        // ---------- planner (AriaSkirmishPlanSystem.Step, pure) ----------

        static AriaTouchTarget T(int id) => new AriaTouchTarget { Available = true, Id = id };

        static AriaSkirmishObservation Missing(float time) => new AriaSkirmishObservation
        {
            Active = true, ExpandedSession = true, Frame = 647, Time = time, ObjectiveType = 1, ObjectiveFrameReason = 9,
            PublicObjective = new AriaObjectiveTouchObservation { Applicable = true }
        };

        static AriaSkirmishObservation Presented(float time) => new AriaSkirmishObservation
        {
            Active = true, ExpandedSession = true, Frame = 700, Time = time, ObjectiveType = 1,
            PublicObjective = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true, PresentationEpoch = 1,
                Intent = AriaSkirmishIntent.ObserveObjective, CameraSettled = true, ProgressKnown = true, PublicProgressRevision = 1 },
            ObjectiveSupport = new AriaObjectiveSupportObservation { Presented = true, SimulationSeconds = time - 50, DismountedCombatGroups = 3, CaptureMajority = true }
        };

        static void Step(in AriaSkirmishObservation v, ref AriaSkirmishPlanComponent p, ref AriaPlaySessionComponent t, out AriaPlayObservationComponent o)
        { o = default; AriaSkirmishPlanSystem.Step(v, ref p, ref t, ref o); }

        [Test]
        public void Lane22FirstObservationWithoutFrameDoesNotBlock()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            Step(Missing(8.5f), ref p, ref t, out var o);
            Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase, "lane 22: reason=101 intent=Recruit at 8.5 s");
            Assert.AreEqual(AriaPlayObservationKind.Waiting, o.Kind); Assert.AreEqual(0, o.TargetId);
            Assert.AreEqual(AriaSkirmishIntent.ObserveObjective, p.Intent);
            Step(Missing(8.5f + AriaObjectiveSkillPolicy.FrameGraceSeconds), ref p, ref t, out _);
            Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase);
            Step(Missing(9f + AriaObjectiveSkillPolicy.FrameGraceSeconds), ref p, ref t, out _);
            Assert.AreEqual(AriaPlayPhase.Blocked, t.Phase, "a frame that never returns still fails closed");
        }

        [Test]
        public void OwnDrawerPurchaseContinuesWithoutFrame()
        {
            var p = new AriaSkirmishPlanComponent { ObjectiveSupport = new AriaObjectiveSupportPlan { Epoch = 1, Action = AriaObjectiveSupportAction.Rifle, Deadline = 999, BaselineInfantry = 12 } };
            var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Missing(40); v.DrawerOpen = true; v.Recruit = T(91); v.CloseDrawer = T(71); v.Infantry = 12;
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(91, o.TargetId, "the rifle card in the open drawer");
            Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase);
            v.RifleRecruitPending = true; v.Frame++;
            Step(v, ref p, ref t, out o);
            Assert.AreEqual(71, o.TargetId, "accepted purchase closes the drawer");
            Assert.AreEqual(AriaObjectiveSupportAction.None, p.ObjectiveSupport.Action);
            Assert.AreEqual(40, t.LastObjectiveProgressAt);
        }

        [Test]
        public void UnownedDrawerIsClosed()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Missing(40); v.DrawerOpen = true; v.CloseDrawer = T(71);
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(71, o.TargetId);
            var placement = Missing(41); placement.PlacementOpen = true; placement.PlacementCancel = T(72);
            Step(placement, ref p, ref t, out o);
            Assert.AreEqual(72, o.TargetId, "an unconfirmed preview is cancelled through its own control");
        }

        // W27 cause 1 (S061 Blocked 115): the Move control was pressed three times without arming move mode; the
        // planner's own bound came one press too late, so the input layer stopped ARIA instead of the skill retrying.
        [Test]
        public void RejectedObjectivePressesAreRetriedBeforeTheInputLayerStops()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.Intent = AriaSkirmishIntent.OpenObjectiveCorridor; v.PublicObjective.ExclusiveGroupSelected = true;
            v.PublicObjective.GoalInView = true; v.PublicObjective.MoveMode = false; v.PublicObjective.Move = T(7);
            int presses = 0;
            for (int frame = 0; frame < 6 && p.ObjectiveFailures == 0; frame++)
            {
                v.Frame++; v.Time += .5f;
                Step(v, ref p, ref t, out var o);
                Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase, "frame " + frame);
                if (o.TargetId != 7) continue;
                // Mirror of AriaPlayDecisionSystem: a fourth consecutive press of one target is Blocked 115.
                if (t.TargetId != 7) { t.TargetId = 7; t.Attempts = 0; }
                Assert.Less(t.Attempts, 3, "the planner must end the attempt before the input layer's 115");
                t.Attempts++; t.Actions++; presses++;
            }
            Assert.AreEqual(3, presses, "three presses of the unresponsive Move control");
            Assert.AreEqual(1, p.ObjectiveFailures, "absorbed as a retry, not a stop");
            Assert.AreEqual(0, t.Attempts, "the retry starts a fresh input count");
            Assert.AreEqual(0, t.TargetId);
            Assert.AreEqual(2, AriaSkirmishPlanSystem.MaxRejectedObjectiveContacts);
        }

        [Test]
        public void RosterChangeBetweenSelectPressesKeepsTheInputBound()
        {
            // W27c (S038 lane 45: Blocked 115 intent=EvacuateOriginals control=Squad0 selectedGroups=0 after 4/8 evacuated):
            // an evacuation bumps the public roster revision, which resets the objective touch plan and its rejected
            // count, while the input layer keeps counting presses of the same squad card.
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.Intent = AriaSkirmishIntent.EvacuateOriginals; v.PublicObjective.SelectGroup = T(5);
            int presses = 0; bool bumped = false;
            for (int frame = 0; frame < 10 && p.ObjectiveFailures == 0; frame++)
            {
                v.Frame++; v.Time += .5f;
                if (presses == 2 && !bumped) { v.PublicObjective.RosterRevision++; bumped = true; }
                Step(v, ref p, ref t, out var o);
                Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase, "frame " + frame);
                if (o.TargetId != 5) continue;
                // Mirror of AriaPlayDecisionSystem: a fourth consecutive press of one target is Blocked 115.
                if (t.TargetId != 5) { t.TargetId = 5; t.Attempts = 0; }
                Assert.Less(t.Attempts, AriaSkirmishPlanSystem.InputPressLimit, "the planner must end the attempt before the input layer's 115");
                t.Attempts++; t.Actions++; presses++;
            }
            Assert.IsTrue(bumped);
            Assert.AreEqual(3, presses, "three presses of the card that does not select, across the roster change");
            Assert.AreEqual(1, p.ObjectiveFailures, "absorbed as a retry, not a stop");
            Assert.AreEqual(0, t.Attempts, "the retry starts a fresh input count");
        }

        [Test]
        public void ObjectiveOrderTimeoutIsRetriedNotBlocked()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.Intent = AriaSkirmishIntent.ControlZone; v.PublicObjective.SelectGroup = T(5);
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(5, o.TargetId, "select the chosen group's card first");
            Assert.AreEqual(AriaSkirmishIntent.ControlZone, p.Intent);
            for (int i = 1; i < AriaObjectiveSkillPolicy.MaxOrderFailures; i++)
            {
                v.Frame++; v.Time += 31 + AriaObjectiveSkillPolicy.OrderRetrySeconds; v.PublicObjective.SelectGroup = default;
                Step(v, ref p, ref t, out o);
                if (p.ObjectiveTouch.GroupId != 0) { v.Frame++; v.Time += 31; Step(v, ref p, ref t, out o); }
                Assert.AreNotEqual(AriaPlayPhase.Blocked, t.Phase, "failure " + i + " is retried after a pause");
                Assert.AreEqual(i, p.ObjectiveFailures);
            }
            for (int i = 0; i < 4 && t.Phase != AriaPlayPhase.Blocked; i++)
            { v.Frame++; v.Time += 31 + AriaObjectiveSkillPolicy.OrderRetrySeconds; Step(v, ref p, ref t, out o); }
            Assert.AreEqual(AriaPlayPhase.Blocked, t.Phase, "repeated failure without an accepted order stops");
        }

        [Test]
        public void ProgressRenewsInputWatchdogs()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            Step(v, ref p, ref t, out _);
            Assert.AreEqual(100, t.LastProgressAt);
            v.Time = 150; v.Frame++; Step(v, ref p, ref t, out _);
            Assert.AreEqual(100, t.LastProgressAt, "an unchanged scoreboard is not progress");
            v.Time = 160; v.Frame++; v.PublicObjective.PublicProgressRevision = 2; Step(v, ref p, ref t, out _);
            Assert.AreEqual(160, t.LastProgressAt); Assert.AreEqual(160, t.LastObjectiveProgressAt);
        }

        [Test]
        public void EscortRunsWhenNoObjectiveOrderIsWanted()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            v.ObjectiveEscort = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true, PresentationEpoch = 1,
                GroupId = 7, NavigationGroupId = 7, PublicKey = 33, Action = AriaObjectiveAction.Move, SelectGroup = T(8), CameraSettled = true };
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(8, o.TargetId, "the vehicle group's card");
            Assert.AreEqual(2, p.ObjectiveOwner);
            Assert.AreEqual(AriaSkirmishIntent.Advance, p.Intent);
            // A new objective choice waits until the in-flight escort finishes.
            v.Frame++; v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Move;
            v.PublicObjective.SelectGroup = T(5);
            Step(v, ref p, ref t, out o);
            Assert.AreEqual(8, o.TargetId);
        }

        // W32 (gate 3): FC re-issues Holds every ~15 s, so escorts used to run only on idle frames that never came.
        [Test]
        public void EscortGetsATurnAfterAnAcceptedObjectiveOrder()
        {
            var p = new AriaSkirmishPlanComponent { ObjectiveAccepted = 1, ObjectiveEscortTurn = 1 };
            var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(100);
            v.PublicObjective.GroupId = 11; v.PublicObjective.PublicKey = 22; v.PublicObjective.Action = AriaObjectiveAction.Hold;
            v.PublicObjective.Intent = AriaSkirmishIntent.ControlZone; v.PublicObjective.SelectGroup = T(5);
            v.ObjectiveEscort = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true, PresentationEpoch = 1,
                GroupId = 7, NavigationGroupId = 7, PublicKey = 33, Action = AriaObjectiveAction.Move, SelectGroup = T(8), CameraSettled = true };
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(8, o.TargetId, "the escort's vehicle card, although an objective order is wanted");
            Assert.AreEqual(2, p.ObjectiveOwner); Assert.AreEqual(0, p.ObjectiveEscortTurn, "one turn per accepted order");
        }

        // W32 (S032: an unaffordable AttackAircraft recruit held the Build drawer for the 60 s popup limit 5 times).
        [Test]
        public void StuckSupportRecruitIsBoundedWhileTheDrawerIsOpen()
        {
            var p = new AriaSkirmishPlanComponent { ObjectiveSupport = new AriaObjectiveSupportPlan { Epoch = 1, Action = AriaObjectiveSupportAction.Rifle, Deadline = 50, BaselineInfantry = 12 } };
            var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Missing(60); v.DrawerOpen = true; v.Recruit = T(91); v.CloseDrawer = T(71); v.Infantry = 12;
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(71, o.TargetId, "past its deadline the recruit closes the drawer instead of pressing again");
            Assert.AreEqual(AriaObjectiveSupportAction.None, p.ObjectiveSupport.Action);
            Assert.AreEqual(75f, p.ObjectiveSupport.NextDecisionAt, 1e-3f, "and waits before choosing a recruit again");
        }

        [Test]
        public void FinishedObjectiveHandsBack()
        {
            var p = new AriaSkirmishPlanComponent(); var t = new AriaPlaySessionComponent { Phase = AriaPlayPhase.Observing };
            var v = Presented(500); v.PublicObjective.Finished = true;
            Step(v, ref p, ref t, out var o);
            Assert.AreEqual(AriaPlayObservationKind.Finished, o.Kind);
            Assert.AreEqual(AriaSkirmishIntent.Handback, p.Intent);
        }

        // ---------- public goal snapshots ----------

        static SkirmishPublicGoalNode Zone(string id, float x, int owner, bool contested) => new SkirmishPublicGoalNode
        {
            PublicId = id, Kind = SkirmishPublicGoalNodeKind.CaptureZone, Provenance = SkirmishPublicGoalProvenance.PublicObjective,
            PositionKnown = true, WorldX = x, WorldZ = 10, OwnerKnown = true, OwnerFaction = owner, Contested = contested,
            CapturePhase = owner != 0 ? SkirmishPublicCapturePhase.Held : SkirmishPublicCapturePhase.None
        };

        static SkirmishPublicGoalNode Marker(string id, SkirmishPublicGoalNodeKind kind, float x) => new SkirmishPublicGoalNode
        { PublicId = id, Kind = kind, Provenance = SkirmishPublicGoalProvenance.PublicObjective, PositionKnown = true, WorldX = x, WorldZ = 20 };

        static SkirmishPublicGoalSnapshot Frontline(int a, int b, int c, bool contestedC, bool contestedA = false)
        {
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("fc-skill", 1, 1, SkirmishObjectiveKind.FrontlineControl, SkirmishPublicGoalPerspective.Capture,
                0, 100, SkirmishOutcomeKind.None, new SkirmishPublicGoalScore { TicketsKnown = true, OwnTickets = 500, OpposingTickets = 500 },
                new[] { Zone("zone.a", 10, a, contestedA), Zone("zone.b", 20, b, false), Zone("zone.c", 30, c, contestedC) }, out var goal, out var error), error);
            return goal;
        }

        static SkirmishPublicGoalSnapshot Breakthrough(bool exitOpen, bool originals)
        {
            var nodes = new List<SkirmishPublicGoalNode>
            {
                Marker("corridor.a", SkirmishPublicGoalNodeKind.ExitCorridor, 10), Marker("corridor.b", SkirmishPublicGoalNodeKind.ExitCorridor, 20),
                Marker("exit", SkirmishPublicGoalNodeKind.EvacuationExit, 30)
            };
            if (originals)
                nodes.Add(new SkirmishPublicGoalNode { PublicId = "rifle.1", Kind = SkirmishPublicGoalNodeKind.OwnDesignatedSubject,
                    Provenance = SkirmishPublicGoalProvenance.OwnRoster, PositionKnown = true, WorldX = 1, WorldZ = 1, OwnerKnown = true, OwnerFaction = 1,
                    RevealedSubjectKind = SkirmishPublicSubjectKind.DesignatedRifle, SubjectDisposition = SkirmishPublicSubjectDisposition.Live,
                    HealthKnown = true, Health = 100, MaximumHealth = 100, HealthProvenance = SkirmishPublicHealthProvenance.OwnRoster });
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("bt-skill", 1, 1, SkirmishObjectiveKind.Breakthrough, SkirmishPublicGoalPerspective.Evacuate,
                0, 100, SkirmishOutcomeKind.None, new SkirmishPublicGoalScore { ExitStateKnown = true, ExitOpen = exitOpen },
                nodes.ToArray(), out var goal, out var error), error);
            return goal;
        }

        static SkirmishPublicGoalSnapshot Convoy()
        {
            Assert.IsTrue(SkirmishPublicGoalSnapshot.TryCreate("ce-skill", 1, 1, SkirmishObjectiveKind.ConvoyEscort, SkirmishPublicGoalPerspective.Escort,
                0, 100, SkirmishOutcomeKind.None, default, new[] { Marker("destination", SkirmishPublicGoalNodeKind.DeliveryDestination, 40) },
                out var goal, out var error), error);
            return goal;
        }
    }
}
