using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;

namespace Game.UI.Shell.Ecs
{
    // FC (Frontline Control), BT (Breakthrough) and CE (Convoy Escort) skill for expanded Skirmish sessions.
    // It plays only through the controls the player is shown: the goal HUD's rendered objective frame
    // (zones/corridors/exit/destination, accepted order receipts), the Build drawer, squad cards and the
    // ordinary Move/Hold buttons. Decisions come from SkirmishPlayerObjectivePolicyUtilitySystemHelper (which
    // group to which public goal) and Game.Simulation.AriaObjectiveSkillPolicy (frame grace, retries).
    //
    // Lane 22 (S007/S013/S019): the previous path stopped on the first frame without a rendered objective
    // frame ([AriaBlocked] reason=101 intent=Recruit at 8.5 s). The goal HUD withdraws its frame whenever it is
    // hidden (Build drawer open, shell settling) and for one frame when the HUD updates after ARIA's observer.
    // Missing frames are now waited for (bounded), ARIA's own purchase drawer is finished without the frame,
    // a failed order sequence is retried after a pause, and public objective progress renews the watchdogs.
    public partial struct AriaSkirmishPlanSystem
    {
        static bool ObjectivePresented(in AriaObjectiveTouchObservation o) =>
            o.Applicable && o.Supported && o.Presented && o.PresentationEpoch != 0;

        public static void StepObjectiveSkill(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            var objective = view.PublicObjective;
            output = new AriaPlayObservationComponent { Kind = AriaPlayObservationKind.Waiting, Time = view.Time, Frame = view.Frame,
                GoalId = unchecked((int)(objective.PublicKey ^ objective.GroupId)) };
            if (view.Finished || objective.Finished)
            {
                plan.Intent = AriaSkirmishIntent.Handback; touch.GestureRequested = 0;
                output.Kind = AriaPlayObservationKind.Finished; return;
            }
            if (touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting) return;
            bool presented = ObjectivePresented(objective);
            var progress = new Game.Simulation.AriaObjectiveProgressState { Known = plan.ObjectiveProgressKnown,
                Revision = plan.ObjectiveProgressRevision, LastAt = plan.ObjectiveProgressAt };
            if (presented && Game.Simulation.AriaObjectiveSkillPolicy.Progress(objective.ProgressKnown, objective.PublicProgressRevision, view.Time, ref progress))
            { touch.LastProgressAt = view.Time; touch.LastObjectiveProgressAt = view.Time; }
            plan.ObjectiveProgressKnown = progress.Known; plan.ObjectiveProgressRevision = progress.Revision; plan.ObjectiveProgressAt = progress.LastAt;
            if (presented && view.ObjectiveSupport.Presented && !float.IsNaN(view.ObjectiveSupport.SimulationSeconds) && !float.IsInfinity(view.ObjectiveSupport.SimulationSeconds))
                plan.ObjectiveClockOffset = view.ObjectiveSupport.SimulationSeconds - view.Time;
            if (touch.Phase == AriaPlayPhase.Touching) return;
            bool ownPopup = view.DrawerOpen || view.PlacementOpen;
            var frame = new Game.Simulation.AriaObjectiveFrameState { Missing = plan.ObjectiveMissing, MissingSince = plan.ObjectiveMissingSince,
                PopupOpen = plan.ObjectivePopupOpen, PopupSince = plan.ObjectivePopupSince };
            var frameAction = Game.Simulation.AriaObjectiveSkillPolicy.Frame(presented, ownPopup, view.Time, ref frame);
            plan.ObjectiveMissing = frame.Missing; plan.ObjectiveMissingSince = frame.MissingSince;
            plan.ObjectivePopupOpen = frame.PopupOpen; plan.ObjectivePopupSince = frame.PopupSince;
            plan.ObjectiveFrameAction = (byte)frameAction;
            switch (frameAction)
            {
                case Game.Simulation.AriaObjectiveFrameAction.Block:
                    touch.Phase = AriaPlayPhase.Blocked; touch.GestureRequested = 0; return;
                case Game.Simulation.AriaObjectiveFrameAction.Wait:
                    // Never act on a remembered objective. Keep any open map closable.
                    plan.Intent = AriaSkirmishIntent.ObserveObjective;
                    if (view.MapOpen && view.CloseMap.Available) Target(view.CloseMap, false, ref output);
                    return;
                case Game.Simulation.AriaObjectiveFrameAction.Popup:
                    if (StepObjectivePopup(view, ref plan, ref touch, ref output, false)) return;
                    return;
                case Game.Simulation.AriaObjectiveFrameAction.ClosePopup:
                    StepObjectivePopup(view, ref plan, ref touch, ref output, true); return;
            }
            if (objective.Paused) { plan.Intent = AriaSkirmishIntent.ObserveObjective; return; }
            if (ConvoyEscortCadence(view)) TrackTruckWait(view, ref plan);
            // W37d (lead): the damaged truck's service Move/Hold, its emergency Hold and the Repair are never put behind
            // escorts, and while the truck is held or repaired no escort, support or coverage turn re-selects another group
            // (W37c: 5 of 5 service Holds expired with escort Moves or a transport-heli support order taking the turns).
            bool service = ConvoyServiceOrder(view), serviceHold = ConvoyServiceHolding(view);
            if (plan.ObjectiveOwner != 2) plan.EscortTurnGuard = 0;
            if (ConvoyEscortCadence(view) && ReleaseFinishedObjective(view, plan.ObjectiveTouch)) { plan.ObjectiveTouch = default; plan.ObjectiveLatchReleases++; }
            // An escort order already in flight finishes (or fails) before anything else re-selects squads.
            if (plan.ObjectiveOwner == 2 && StepObjectiveEscort(view, ref plan, ref touch, ref output)) return;
            if (StepPublicBoarding(view, ref plan, ref touch, ref output)) return;
            if (StepOffensiveAir(view, ref plan, ref touch, ref output)) return;
            if (StepPublicScan(view, ref plan, ref touch, ref output)) return;
            var current = plan.ObjectiveTouch;
            bool pending = current.CommandPending || current.Stage == 3;
            bool sameGoal = current.GroupId == objective.GroupId && current.PublicKey == objective.PublicKey &&
                current.Action == objective.Action && current.PresentationEpoch == objective.PresentationEpoch;
            bool wants = objective.GroupId != 0 && objective.PublicKey != 0 && objective.Action != AriaObjectiveAction.None &&
                !(sameGoal && current.Stage == 4);
            bool ready = view.Time >= plan.ObjectiveRetryAt;
            // Coverage of every offered unit type, readiness research and producer buildings after the first
            // accepted objective order (the same pure policies as the base-assault planners).
            if (plan.ObjectiveAccepted > 0) plan.CoverageAssaultSeen = 1;
            // Gate 3 (lane 25: 12-15 offered types unused on S007/S013/S019): a purchase, research, placement or
            // support order already started finishes before objective orders re-select squads. Their Busy rules
            // otherwise abandoned it (and marked the kind done) whenever an objective order armed Move.
            if (!pending && !serviceHold && ObjectiveCoverageInFlight(plan) && StepObjectiveCoverage(view, ref plan, ref touch, ref output)) return;
            // Ordinary rifles refill to the type's strength until coverage starts, then only to the floor:
            // FieldBase materials go to the other offered types (gunner/marksman/breacher add infantry too).
            // W32 (gate 3): one escort turn after each accepted objective order, before support and coverage. Otherwise
            // FC's Hold re-issues left starting vehicle/infantry groups no idle frame and every offer expired unused.
            if (!pending && !service && plan.ObjectiveEscortTurn != 0)
            {
                plan.ObjectiveEscortTurn = 0;
                if (StepObjectiveEscort(view, ref plan, ref touch, ref output))
                {
                    // W37e (lead, gate 3): CE's escort turn after an accepted truck order is not interrupted by a later
                    // truck order (only service orders and a truck Hold pre-empt it); it ends on its own receipt or limits.
                    if (plan.ObjectiveOwner == 2 && ConvoyEscortCadence(view)) plan.EscortTurnGuard = 1;
                    return;
                }
            }
            var supportView = view;
            supportView.ObjectiveSupport.InfantryTarget = Game.Simulation.AriaObjectiveSkillPolicy.RifleTarget(
                (Game.Simulation.AriaObjectiveType)view.ObjectiveType, plan.CoverageAssaultSeen != 0);
            if (!pending && !serviceHold && StepPublicObjectiveSupport(supportView, ref plan, ref touch, ref output)) return;
            // One coverage turn after each accepted objective order: FC re-issues Holds every ~15 s, which
            // otherwise left coverage no idle frame to start in.
            if (!pending && !serviceHold && plan.ObjectiveCoverageTurn != 0)
            {
                plan.ObjectiveCoverageTurn = 0;
                if (StepObjectiveCoverage(view, ref plan, ref touch, ref output)) return;
            }
            // W37c (lead, option a): CE escorts first. While a truck order waits, every offered escort goes first until each
            // escort-capable group has an accepted Move, or the truck order has waited ConvoyTruckReleaseSeconds.
            // W37d (lead): only for the mission's FIRST truck order (initial cover, every starting group used for gate 3).
            // Later truck orders go first (W37c: S046 waited 215 s over 6 orders); idle escorts follow on the next turns.
            if (!pending && wants && ready && ConvoyEscortCadence(view) && !service)
            {
                bool first = plan.TruckReleases == 0, bound = TruckBoundExpired(view, plan);
                // W37e: a truck Hold (dwell at the destination) is never put behind escorts (it also ends an escort in flight).
                if (first && !bound && objective.Action != AriaObjectiveAction.Hold && EscortOffered(view, plan)) { StepObjectiveEscort(view, ref plan, ref touch, ref output); return; }
                ReleaseTruck(view, ref plan, !first ? (byte)4 : bound ? (byte)2 : view.EscortAccepted >= view.EscortCapable ? (byte)1 : (byte)3);
            }
            if (pending || wants && ready)
            {
                StepObjectiveOrder(view, objective, ref plan, ref touch, ref output);
                return;
            }
            if (!serviceHold && StepObjectiveCoverage(view, ref plan, ref touch, ref output)) return;
            if (!service && StepObjectiveEscort(view, ref plan, ref touch, ref output)) return;
            plan.Intent = AriaSkirmishIntent.ObserveObjective;
            if (view.MapOpen && view.CloseMap.Available) Target(view.CloseMap, false, ref output);
        }

        // One public objective order through StepPublicObjective. Its own fail-closed limits (30 s sequence,
        // three rejected contacts, 8 s receipt) end this attempt; the skill pauses and retries instead of
        // stopping, up to MaxOrderFailures without an accepted order in between.
        static void StepObjectiveOrder(in AriaSkirmishObservation view, in AriaObjectiveTouchObservation objective,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            var prior = touch.Phase;
            int stage = plan.ObjectiveTouch.Stage;
            plan.Intent = objective.Intent;
            StepPublicObjective(view, objective, ref plan.ObjectiveTouch, ref touch, ref output);
            if (plan.ObjectiveTouch.Stage == 4)
            {
                if (stage != 4)
                {
                    var order = new Game.Simulation.AriaObjectiveOrderState { Failures = plan.ObjectiveFailures, Accepted = plan.ObjectiveAccepted, RetryAt = plan.ObjectiveRetryAt };
                    Game.Simulation.AriaObjectiveSkillPolicy.Accepted(ref order);
                    plan.ObjectiveFailures = order.Failures; plan.ObjectiveAccepted = order.Accepted;
                    plan.ObjectiveCoverageTurn = 1; plan.ObjectiveEscortTurn = 1;
                    touch.LastProgressAt = view.Time;
                }
                plan.Intent = AriaSkirmishIntent.ObserveObjective;
            }
            if (prior == AriaPlayPhase.Blocked || touch.Phase != AriaPlayPhase.Blocked) return;
            var state = new Game.Simulation.AriaObjectiveOrderState { Failures = plan.ObjectiveFailures, Accepted = plan.ObjectiveAccepted, RetryAt = plan.ObjectiveRetryAt };
            bool retry = Game.Simulation.AriaObjectiveSkillPolicy.AbsorbFailure(ref state, view.Time, Game.Simulation.AriaObjectiveSkillPolicy.MaxOrderFailures);
            plan.ObjectiveFailures = state.Failures; plan.ObjectiveRetryAt = state.RetryAt;
            if (!retry) return;
            touch.Phase = AriaPlayPhase.Observing; touch.GestureRequested = 0;
            // W27 cause 1: the retry starts a fresh input count; a stale count of 3 would stop the next press (115).
            touch.Attempts = 0; touch.TargetId = 0;
            plan.ObjectiveTouch = default;
            output.Kind = AriaPlayObservationKind.Waiting; output.TargetId = 0;
        }

        // One own vehicle group's ordinary Move to the current objective (view.ObjectiveEscort), through the
        // same select/focus/Move/ground-tap sequence and accepted-order receipt as objective orders.
        public static bool ConvoyEscortCadence(in AriaSkirmishObservation view) =>
            view.ObjectiveType == (byte)Game.Simulation.AriaObjectiveType.ConvoyEscort;

        public const float ConvoyTruckReleaseSeconds = 45f;

        // W37e (lead; W37d: 4 CE deadline losses ended on a truck's identical Move re-offered after its accepted Move and never
        // re-tapped). A finished objective (accepted, Stage 4) stays latched only while the observer keeps choosing it: once
        // the choice moves away (None, Hold, another goal or group) or the group shows another accepted order, the latch is
        // released and the same Move offered again is a new order. CE only (FC/BT/BA keep the cf5f53bd1 latch).
        public static bool ReleaseFinishedObjective(in AriaSkirmishObservation view, in AriaObjectiveTouchPlan finished)
        {
            if (finished.Stage != 4 || finished.CommandPending) return false;
            var o = view.PublicObjective;
            if (o.GroupId != finished.GroupId || o.PublicKey != finished.PublicKey || o.Action != finished.Action) return true;
            return o.AcceptedGroupId == finished.GroupId && o.AcceptedOrderRevision > finished.OrderRevisionAtIssue &&
                (o.AcceptedPublicKey != finished.PublicKey || o.AcceptedAction != finished.Action);
        }

        // The damaged truck's service journey order (Move/Hold to the pad, emergency Hold, Repair): never behind escorts.
        public static bool ConvoyServiceOrder(in AriaSkirmishObservation view)
        {
            var intent = view.PublicObjective.Intent;
            return ConvoyEscortCadence(view) && (intent == AriaSkirmishIntent.ServiceOriginalTruck ||
                intent == AriaSkirmishIntent.StopDamagedTruck || intent == AriaSkirmishIntent.RepairVehicle);
        }

        // The truck is being held or repaired: it stays the selected group through the Hold budget.
        public static bool ConvoyServiceHolding(in AriaSkirmishObservation view) =>
            ConvoyServiceOrder(view) && (view.PublicObjective.Action == AriaObjectiveAction.Hold ||
                view.PublicObjective.Action == AriaObjectiveAction.Repair || view.PublicObjective.Intent == AriaSkirmishIntent.RepairVehicle);

        // W37d: an escort in flight hands the turn back now: for any service order, for a truck order once one truck order
        // was released, and for the first truck order exactly at its 45 s bound (even with the same offer still in flight).
        // W37e: the escort turn after an accepted truck order (EscortTurnGuard) is interrupted only by a service order or a
        // truck Hold (a truck in danger or dwelling at the destination comes first), not by a later truck Move.
        public static bool ConvoyTruckFirst(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan) =>
            ConvoyEscortCadence(view) && (ConvoyServiceOrder(view) ||
                ObjectiveOrderWaiting(view, plan) && (view.PublicObjective.Action == AriaObjectiveAction.Hold ||
                    plan.EscortTurnGuard == 0 && (plan.TruckReleases > 0 || TruckBoundExpired(view, plan))));

        static void TrackTruckWait(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan)
        {
            if (ConvoyServiceOrder(view) || !ObjectiveOrderWaiting(view, plan)) { plan.TruckWaitSince = 0; plan.TruckReleased = 0; return; }
            if (plan.TruckWaitSince <= 0) { plan.TruckWaitSince = view.Time > 0.01f ? view.Time : 0.01f; plan.TruckReleased = 0; }
        }

        public static bool TruckBoundExpired(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan) =>
            plan.TruckWaitSince > 0 && view.Time - plan.TruckWaitSince >= ConvoyTruckReleaseSeconds;

        // An escort offer ARIA can act on now (the same presence/limit tests StepObjectiveEscort applies).
        public static bool EscortOffered(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan)
        {
            var escort = view.ObjectiveEscort;
            return ObjectivePresented(escort) && escort.GroupId != 0 && escort.PublicKey != 0 && escort.Action == AriaObjectiveAction.Move &&
                plan.EscortFailures < Game.Simulation.AriaObjectiveSkillPolicy.MaxEscortFailures &&
                (plan.ObjectiveOwner == 2 || view.Time >= plan.EscortRetryAt);
        }

        static void ReleaseTruck(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan, byte reason)
        {
            if (plan.TruckReleased != 0 || plan.TruckWaitSince <= 0) return;
            plan.TruckReleased = 1; plan.TruckReleases++; plan.TruckReleaseReason = reason;
            plan.TruckReleaseEscorts = view.EscortAccepted; plan.TruckReleaseCapable = view.EscortCapable;
            plan.TruckReleaseEscortsOld = view.EscortAcceptedOld;
            plan.TruckReleaseWaited = view.Time - plan.TruckWaitSince;
        }

        // The objective order would act this frame (the same wants/ready test StepObjectiveSkill uses).
        public static bool ObjectiveOrderWaiting(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan)
        {
            var objective = view.PublicObjective; var current = plan.ObjectiveTouch;
            if (current.CommandPending || current.Stage == 3) return true;
            bool sameGoal = current.GroupId == objective.GroupId && current.PublicKey == objective.PublicKey &&
                current.Action == objective.Action && current.PresentationEpoch == objective.PresentationEpoch;
            return objective.GroupId != 0 && objective.PublicKey != 0 && objective.Action != AriaObjectiveAction.None &&
                !(sameGoal && current.Stage == 4) && view.Time >= plan.ObjectiveRetryAt;
        }

        static bool StepObjectiveEscort(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            var escort = view.ObjectiveEscort;
            bool inFlight = plan.ObjectiveOwner == 2;
            if (!ObjectivePresented(escort) || escort.GroupId == 0 || escort.PublicKey == 0 || escort.Action != AriaObjectiveAction.Move ||
                plan.EscortFailures >= Game.Simulation.AriaObjectiveSkillPolicy.MaxEscortFailures)
            { plan.ObjectiveOwner = 0; return false; }
            bool same = plan.EscortTouch.GroupId == escort.GroupId && plan.EscortTouch.PublicKey == escort.PublicKey &&
                plan.EscortTouch.PresentationEpoch == escort.PresentationEpoch;
            // W37 (S046: a truck's chosen Move waited 144 s; escort_accepted=0 on every run): the observer marks an escort
            // done on the frame its accepted Move is displayed and offers the next group at once, so the in-flight touch
            // never saw its own receipt and ObjectiveOwner stayed 2 while offers lasted; escorts ran back to back ahead of
            // the objective order. A changed offer ends the in-flight escort (its displayed receipt counts as accepted) and
            // hands the turn back; the next escort starts from the escort turn or an idle frame.
            // W37b (lead): CE convoys only. FC/BT/BA keep the cf5f53bd1 path (S010 lost once with this cadence on FC).
            // W37c (option a): escorts first; the in-flight escort hands back only once the waiting truck order reached
            // its 45 s release bound (otherwise escorts keep running back to back, as gate 3 needs).
            // W37d (lead): the bound is exact (W37c released 47-71 s after the wait began while an unchanged offer finished),
            // later truck orders and service orders take the turn at once; the escort is offered again on a later turn.
            if (inFlight && plan.EscortTouch.GroupId != 0 && ConvoyTruckFirst(view, plan))
            {
                var last = plan.EscortTouch;
                if ((last.CommandPending || last.Stage == 3) && escort.AcceptedOrderRevision > last.OrderRevisionAtIssue &&
                    escort.AcceptedGroupId == last.GroupId && escort.AcceptedPublicKey == last.PublicKey && escort.AcceptedAction == AriaObjectiveAction.Move)
                {
                    plan.EscortAccepted++;
                    if (plan.EscortFailures > 0) plan.EscortFailures--;
                    touch.LastProgressAt = view.Time;
                }
                plan.ObjectiveOwner = 0; plan.EscortTouch = default; plan.EscortTurnGuard = 0;
                if (!ConvoyServiceOrder(view)) ReleaseTruck(view, ref plan, plan.TruckReleases > 0 ? (byte)4 : (byte)2);
                return false;
            }
            if (same && plan.EscortTouch.Stage == 4) { plan.ObjectiveOwner = 0; return false; }
            if (!inFlight && view.Time < plan.EscortRetryAt) return false;
            var prior = touch.Phase;
            plan.Intent = AriaSkirmishIntent.Advance;
            plan.ObjectiveOwner = 2;
            StepPublicObjective(view, escort, ref plan.EscortTouch, ref touch, ref output);
            if (plan.EscortTouch.Stage == 4)
            {
                plan.ObjectiveOwner = 0; plan.EscortAccepted++;
                if (plan.EscortFailures > 0) plan.EscortFailures--;
                touch.LastProgressAt = view.Time;
                return true;
            }
            if (prior != AriaPlayPhase.Blocked && touch.Phase == AriaPlayPhase.Blocked)
            {
                // An escort is optional: its failure never stops ARIA. The observer offers the next group.
                plan.EscortFailures++; plan.EscortRetryAt = view.Time + Game.Simulation.AriaObjectiveSkillPolicy.OrderRetrySeconds;
                plan.ObjectiveOwner = 0; plan.EscortTouch = default;
                touch.Phase = AriaPlayPhase.Observing; touch.GestureRequested = 0;
                touch.Attempts = 0; touch.TargetId = 0;
                output.Kind = AriaPlayObservationKind.Waiting; output.TargetId = 0;
            }
            return true;
        }

        static bool StepObjectiveCoverage(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            if (plan.PadStage != 0 && plan.BuildCurrent == 0 && plan.ObjectiveSupport.Action != AriaObjectiveSupportAction.Helipad)
                plan.PadStage = 0;
            if (StepGenericSupportOrder(view, ref plan, ref touch, ref output)) return true;
            if (StepObjectiveRocketeer(view, ref plan, ref touch, ref output)) return true;
            if (StepGenericBreacherCoverage(view, ref plan, ref touch, ref output)) return true;
            if (StepGenericCoverageRecruit(view, ref plan, ref touch, ref output)) return true;
            if (StepGenericResearch(view, ref plan, ref touch, ref output)) return true;
            if (StepGenericBuilding(view, ref plan, ref touch, ref output)) return true;
            return false;
        }

        // A gate-3 action this skill started and has not finished (each owner bounds itself).
        public static bool ObjectiveCoverageInFlight(in AriaSkirmishPlanComponent p) =>
            p.CoverageCurrent != 0 || p.BreacherStage == 1 || p.ResearchCurrent != 0 || p.BuildCurrent != 0 ||
            p.SupportStage != 0 || p.ObjectiveRocketeerStage == 1;

        // The rocketeer is offered but not in the coverage list (S013/S019 start without one): one purchase.
        static bool StepObjectiveRocketeer(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            if (!ObjectiveCoverageEligible(view)) return false;
            bool busy = CoverageOtherOwner(view, plan, touch) || plan.CoverageCurrent != 0 || plan.BreacherStage == 1 ||
                plan.ObjectiveRocketeerStage == 0 && view.DrawerOpen;
            byte stage = plan.ObjectiveRocketeerStage; float deadline = plan.ObjectiveRocketeerDeadline;
            var action = Game.Simulation.AriaObjectiveSkillPolicy.OneTimeBuy(ref stage, ref deadline, plan.CoverageAssaultSeen != 0, busy,
                view.CanAffordRocketeer && view.OwnMaterials >= Game.Simulation.AriaObjectiveSkillPolicy.CoverageReserve,
                view.RocketeerRecruitPending, view.RecruitRocketeer.Available, view.Time);
            plan.ObjectiveRocketeerStage = stage; plan.ObjectiveRocketeerDeadline = deadline;
            if (action == Game.Simulation.AriaObjectiveBuy.Recruit)
            { plan.Intent = AriaSkirmishIntent.Recruit; Target(view.RecruitRocketeer, false, ref output); return true; }
            if (action == Game.Simulation.AriaObjectiveBuy.Waiting)
            { plan.Intent = AriaSkirmishIntent.Recruit; output.Kind = AriaPlayObservationKind.Waiting; return true; }
            return false;
        }

        // The Build drawer or a placement preview hides the goal HUD. Finish the purchase/placement this skill
        // started (all of these read only the drawer, catalog cards, placement bar and own roster counts),
        // otherwise close the popup through its own visible control.
        static bool StepObjectivePopup(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output, bool forceClose)
        {
            ref var support = ref plan.ObjectiveSupport;
            if (!forceClose)
            {
                if (ObjectiveCoverageInFlight(plan) && StepObjectiveCoverage(view, ref plan, ref touch, ref output)) return true;
                if (support.Action == AriaObjectiveSupportAction.Helipad && plan.PadStage != 0)
                {
                    if (view.PadPresent) { support.Action = AriaObjectiveSupportAction.None; plan.PadStage = 0; }
                    else
                    {
                        // The helipad deadline is on the objective clock (ExecuteSupport).
                        var simulationView = view; simulationView.Time = view.Time + plan.ObjectiveClockOffset;
                        StepHomePadPlacement(simulationView, ref plan, ref touch, ref output); return true;
                    }
                }
                if (support.Action != AriaObjectiveSupportAction.None && support.Action != AriaObjectiveSupportAction.Helipad &&
                    support.Action != AriaObjectiveSupportAction.ReturnAircraft && view.DrawerOpen)
                {
                    if (SupportAccepted(view, support))
                    {
                        support.ObservedAcceptance = true; support.Action = AriaObjectiveSupportAction.None; support.Closing = true;
                        touch.LastObjectiveProgressAt = view.Time; touch.LastProgressAt = view.Time;
                        plan.Intent = AriaSkirmishIntent.Inspect; Target(view.CloseDrawer, false, ref output); return true;
                    }
                    // W32: the support deadline also bounds the recruit while its drawer is open (S032: an unaffordable
                    // AttackAircraft held the drawer for the 60 s popup limit five times).
                    if (support.RepeatedContacts < 3 && view.Time + plan.ObjectiveClockOffset < support.Deadline)
                        return ExecuteSupport(view, ref plan, ref touch, ref output);
                }
            }
            // Nothing of ours is pending in this popup (or it overstayed): close it, cancelling an unconfirmed preview.
            if (support.Action != AriaObjectiveSupportAction.ReturnAircraft)
            {
                // W32: a dropped recruit waits like an expired one, so the same recruit is not chosen again at once.
                if (support.Action != AriaObjectiveSupportAction.None) support.NextDecisionAt = view.Time + plan.ObjectiveClockOffset + 15;
                support.Action = AriaObjectiveSupportAction.None;
            }
            support.Closing = false;
            if (forceClose) { plan.CoverageCurrent = 0; plan.ResearchCurrent = 0; plan.BuildCurrent = 0; plan.PadStage = 0; if (plan.BreacherStage == 1) plan.BreacherStage = 2; if (plan.ObjectiveRocketeerStage == 1) plan.ObjectiveRocketeerStage = 2; }
            plan.Intent = AriaSkirmishIntent.Inspect;
            if (view.PlacementOpen && view.PlacementCancel.Available) { Target(view.PlacementCancel, false, ref output); return true; }
            if (view.DrawerOpen && view.CloseDrawer.Available) { Target(view.CloseDrawer, false, ref output); return true; }
            output.Kind = AriaPlayObservationKind.Waiting;
            return true;
        }
    }
}
