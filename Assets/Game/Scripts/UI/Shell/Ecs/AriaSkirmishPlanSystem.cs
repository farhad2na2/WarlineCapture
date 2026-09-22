using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Burst;
using Unity.Entities;
namespace Game.UI.Shell.Ecs
{
    /// <summary>Base Assault skill. Inputs contain only currently presented player information.</summary>
    [BurstCompile, UpdateInGroup(typeof(PresentationSystemGroup)), UpdateBefore(typeof(AriaPlayDecisionSystem))]
    public partial struct AriaSkirmishPlanSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (view, plan, touch, output) in SystemAPI.Query<RefRO<AriaSkirmishObservationComponent>, RefRW<AriaSkirmishPlanComponent>, RefRW<AriaPlaySessionComponent>, RefRW<AriaPlayObservationComponent>>())
                if (view.ValueRO.Value.Active) Step(view.ValueRO.Value, ref plan.ValueRW, ref touch.ValueRW, ref output.ValueRW);
        }
        public static void Step(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            output = new AriaPlayObservationComponent { Kind = view.Finished ? AriaPlayObservationKind.Finished : AriaPlayObservationKind.Waiting, Time = view.Time, Frame = view.Frame, GoalId = 10000 + plan.Cycle * 10 + plan.Slot };
            if (view.ExpandedSession)
            {
                // Manual is idle / pre-consent: still publish the presented control so
                // the cyan hand and DecisionSystem can see TargetId. Blocked/Starting
                // are takeover or not-ready. Touching is held inside StepExpanded.
                if (!view.Finished && touch.Phase is AriaPlayPhase.Blocked or AriaPlayPhase.Starting)
                    return;
                StepExpandedBaseAssault(view, ref plan, ref touch, ref output);
                return;
            }
            if (view.Finished || touch.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting) return;
            if (plan.OpeningUntil == 0) { plan.OpeningUntil = view.Time + 180; plan.OpeningSquads = view.AvailableSquads; }
            // Assemble a force and finish the nearby defensive fight before advancing.
            // A deadline prevents waiting forever when production is unaffordable.
            bool awaitingAssault = plan.AssaultStarted == 0;
            // After the opening window, depart with a viable force even if a contact is still nearby.
            // Dense maps otherwise keep ThreatNearForce true until OpeningUntil+60 while the clock burns.
            if (!view.ThreatNearForce && view.Infantry >= 24 ||
                view.Infantry >= 16 && view.Time >= plan.OpeningUntil ||
                view.Time >= plan.OpeningUntil + 60) plan.AssaultStarted = 1;
            if (plan.LastProgressAt == 0 || plan.EnemyHealth != view.EnemyHealth || plan.PlayerHealth != view.PlayerHealth || plan.ForceHealth != view.ForceHealth || plan.Infantry != view.Infantry)
            { plan.LastProgressAt = view.Time; plan.EnemyHealth = view.EnemyHealth; plan.PlayerHealth = view.PlayerHealth; plan.ForceHealth = view.ForceHealth; plan.Infantry = view.Infantry; }
            // Opening assembly is deliberate waiting for the assault deadline, not a stuck tap loop.
            // Dense maps can hold a nearby contact without health changes until OpeningUntil+60.
            // Refresh on the transition frame too so the deadline itself is not treated as stall.
            if (awaitingAssault) plan.LastProgressAt = view.Time;
            // A loop of successful taps is not evidence that the match is progressing.
            if (view.Time - plan.LastProgressAt > 150) { touch.Phase = AriaPlayPhase.Blocked; return; }
            if (touch.Phase == AriaPlayPhase.Touching) return;
            if (OpeningDefense(view, ref plan, ref touch, ref output)) return;
            if (plan.TargetPending != 0 && (touch.TargetId == -20001 || touch.TargetId == -20002 || touch.TargetId == -20006) && touch.Actions > plan.ActionsAtTarget)
            {
                plan.TargetPending = 0;
                // Preserve a rectangle-selected fighting group between engagements.
                // Replacing it with individual cards splits an otherwise coordinated army.
                if (plan.GroupStage == 3 && view.SelectionVisible)
                {
                    plan.Slot = 0; plan.Cycle++; plan.ObserveUntil = view.Time + (touch.TargetId == -20006 ? 8 : touch.TargetId == -20001 ? 12 : 8);
                    plan.Intent = AriaSkirmishIntent.ObserveBattle;
                    return;
                }
                plan.Slot++;
                while (plan.Slot < 5 && (view.AvailableSquads & (1 << plan.Slot)) == 0) plan.Slot++;
                plan.ObserveUntil = view.Time + 1;
                if (plan.Slot >= 5) { plan.Slot = 0; plan.Cycle++; plan.ObserveUntil = view.Time + (touch.TargetId == -20006 ? 8 : touch.TargetId == -20001 ? 12 : 8); }
                // Finish issuing the army's orders before returning to production.
                // Only near-total losses justify interrupting the coordinated advance.
                if (plan.Slot != 0 && view.Infantry < 4 && view.Time >= plan.NextRecruitAt)
                { plan.Intent = AriaSkirmishIntent.Recruit; plan.RecruitBurstRemaining = 2; plan.ObserveUntil = 0; }
            }
            if (!view.MapOpen && plan.MapNavigationStage == 1)
            {
                // Keep the opening gesture stable while the hand moves to the map.
                // Advancing the plan here would cancel that touch before it lands.
                plan.Intent = plan.GroupStage == 7 ? AriaSkirmishIntent.GroupForce : AriaSkirmishIntent.FindThreat;
                Target(plan.AdvanceNavigation != 0 ? view.FocusAdvance : plan.GroupStage == 7 ? view.FocusGroup : view.FocusThreat, false, ref output);
                return;
            }
            if (view.MapOpen)
            {
                var focus = plan.AdvanceNavigation != 0 ? view.FocusAdvance : plan.GroupStage == 7 ? view.FocusGroup : view.FocusThreat;
                bool drag = plan.AdvanceNavigation != 0 ? view.FocusAdvanceDrag : plan.GroupStage == 7 ? view.FocusGroupDrag : view.FocusThreatDrag;
                var dragEnd = plan.AdvanceNavigation != 0 ? view.FocusAdvanceDragEnd : plan.GroupStage == 7 ? view.FocusGroupDragEnd : view.FocusThreatDragEnd;
                if (plan.MapOpenedAt == 0) plan.MapOpenedAt = view.Time;
                if (plan.MapNavigationStage == 1 && !focus.Available && view.Time < plan.MapOpenedAt + 5)
                    return; // The full map needs a presentation frame before its markers are clickable.
                if (plan.MapNavigationStage == 1 && focus.Available)
                {
                    plan.MapNavigationStage = 2; plan.MapFocusActions = touch.Actions;
                }
                if (plan.MapNavigationStage == 2 &&
                    !((touch.TargetId == -20004 || touch.TargetId == -20008 || touch.TargetId == -20011) && touch.Actions > plan.MapFocusActions))
                {
                    plan.Intent = plan.GroupStage == 7 ? AriaSkirmishIntent.GroupForce : AriaSkirmishIntent.FindThreat;
                    Target(focus, false, ref output);
                    if (drag) { output.Drag = 1; output.DragEnd = dragEnd; }
                    return;
                }
                // Exactly one focus gesture per opening, then close regardless of marker motion.
                plan.MapNavigationStage = 3;
                plan.Intent = plan.GroupStage == 7 ? AriaSkirmishIntent.GroupForce : AriaSkirmishIntent.FindThreat;
                Target(view.CloseMap, false, ref output); return;
            }
            if (plan.MapNavigationStage == 3)
            {
                if (plan.GroupStage == 7) { plan.GroupStage = 6; plan.GroupReadyAt = view.Time + 2.5f; }
                plan.MapNavigationStage = 0; plan.MapOpenedAt = 0; plan.AdvanceNavigation = 0;
                plan.MapNavigationReadyAt = view.Time + 6;
                plan.ObserveUntil = view.Time + 1;
            }
            if (view.Time < plan.ObserveUntil)
            {
                // Camera settlement is part of the current attack sequence. Do not
                // turn it into a recruitment window that pulls the camera away again.
                plan.Intent = view.Time < plan.MapNavigationReadyAt
                    ? AriaSkirmishIntent.FindThreat : plan.Slot != 0
                        ? AriaSkirmishIntent.SelectSquad : AriaSkirmishIntent.ObserveBattle;
                return;
            }
            bool finishingBase = view.SelectedCount > 0 && view.AssaultAtBase &&
                view.EnemyHealth > 0 && view.EnemyHealth <= 400;
            if (plan.AssaultStarted != 0 && plan.GroupStage >= 3 && view.Infantry < 8 && !finishingBase)
            {
                // Zero survivors is also a depleted assault. Rebuild before selecting
                // replacement cards, otherwise each delivery is sent out on its own.
                plan.GroupStage = 0; plan.AssaultStarted = 0; plan.OpeningUntil = view.Time + 45;
                plan.NextRecruitAt = view.Time;
                plan.Intent = AriaSkirmishIntent.Recruit;
            }
            else if (plan.GroupStage == 3 && view.SelectedCount < 8 && !finishingBase)
            { plan.GroupStage = 4; plan.Intent = AriaSkirmishIntent.ObserveBattle; }
            if (plan.RecruitDrawerSeen != 0 && !view.DrawerOpen)
            {
                bool unavailable = plan.NextRecruitAt > view.Time;
                plan.RecruitDrawerSeen = 0;
                plan.NextRecruitAt = UnityEngine.Mathf.Max(plan.NextRecruitAt, view.Time + 3);
                plan.RecruitBurstRemaining = unavailable ? 0 : UnityEngine.Mathf.Max(0, plan.RecruitBurstRemaining - 1);
                plan.Intent = plan.RecruitBurstRemaining > 0 && view.Infantry < 24
                    ? AriaSkirmishIntent.Recruit : AriaSkirmishIntent.SelectSquad;
                // Squad cards include replacements. Keep the current camera and
                // continue the rotation instead of regrouping at home after every delivery.
            }
            if (!view.DrawerOpen && plan.RecruitBurstRemaining > 0 && plan.Intent == AriaSkirmishIntent.Recruit &&
                view.Time < plan.NextRecruitAt) return;
            if (view.AvailableSquads == 0) { plan.Intent = AriaSkirmishIntent.Recruit; plan.GroupStage = 0; }
            bool groupStillFighting = plan.GroupStage == 3 && view.SelectionVisible && (view.SelectedCount >= 8 || finishingBase);
            if (groupStillFighting && !finishingBase && view.Infantry < 20 &&
                view.Time >= plan.NextRecruitAt && plan.Intent == AriaSkirmishIntent.ObserveBattle)
            {
                // Keep production working while the current army fights. Squad cards
                // include the arrivals; do not wait for the selected force to be wiped out.
                plan.GroupStage = 4; plan.RecruitBurstRemaining = 1;
                plan.Intent = AriaSkirmishIntent.Recruit; groupStillFighting = false;
            }
            if (!groupStillFighting && view.Infantry < 24 && view.Time >= plan.NextRecruitAt &&
                plan.Intent is AriaSkirmishIntent.Recruit or AriaSkirmishIntent.ObserveBattle)
            {
                if (plan.Intent == AriaSkirmishIntent.ObserveBattle && view.Infantry < 16)
                    plan.RecruitBurstRemaining = 2;
                plan.Intent = AriaSkirmishIntent.Recruit;
                if (view.DrawerOpen) plan.RecruitDrawerSeen = 1;
                if (view.Recruit.Available) { plan.RecruitUnavailableAt = 0; Target(view.Recruit, false, ref output); return; }
                if (plan.RecruitUnavailableAt == 0) plan.RecruitUnavailableAt = view.Time;
                if (view.Time - plan.RecruitUnavailableAt < 3) return;
                // An unavailable Recruit control is visible affordability/capacity feedback.
                // Close the drawer and fight with existing units; never force an order.
                plan.NextRecruitAt = view.Time + 12;
            }
            if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return; }
            if (plan.AssaultStarted == 0)
            {
                // Contest nearby contacts during assembly. Returning without a touch left
                // the opening army idle under fire on dense maps until OpeningUntil+60.
                // Fight campers even while rebuilding; map threat navigation needs a force.
                plan.Intent = AriaSkirmishIntent.ObserveBattle;
                bool openingThreat = view.Threat.Available;
                if (openingThreat)
                {
                    if (!view.AttackMode)
                    { plan.Intent = AriaSkirmishIntent.Attack; Target(view.Attack, false, ref output); return; }
                    plan.Intent = AriaSkirmishIntent.TargetThreat;
                    Target(view.ThreatGround.Available ? view.ThreatGround : view.Threat, true, ref output);
                    plan.ActionsAtTarget = touch.Actions; plan.TargetPending = 1;
                    return;
                }
                if (view.Infantry < 8 || view.AvailableSquads == 0)
                {
                    plan.Intent = AriaSkirmishIntent.Recruit;
                    return;
                }
                if (view.FocusThreat.Available && view.Time >= plan.MapNavigationReadyAt)
                {
                    plan.Intent = AriaSkirmishIntent.FindThreat;
                    plan.MapNavigationStage = 1;
                    Target(view.FocusThreat, false, ref output);
                    return;
                }
                return;
            }
            if (view.AvailableSquads == 0) return;
            if (view.FocusPlayer.Available)
            {
                // A selection disappearing (for example when its last unit dies)
                // needs a surviving squad card, not another camera trip and drag.
                if (plan.GroupStage == 3 && !view.SelectionVisible) plan.GroupStage = 4;
                if (plan.GroupStage < 3 || plan.GroupStage == 6)
                {
                    plan.Intent = AriaSkirmishIntent.GroupForce;
                    if (plan.GroupStage == 0)
                    {
                        if (view.FocusGroup.Available)
                        {
                            plan.GroupStage = 7; plan.MapNavigationStage = 1;
                            Target(view.FocusGroup, false, ref output); return;
                        }
                        plan.GroupAction = touch.Actions; plan.GroupStage = 1; plan.GroupReadyAt = 0;
                        Target(view.FocusPlayer, false, ref output); return;
                    }
                    if (plan.GroupStage == 1 && touch.Actions == plan.GroupAction)
                    { Target(view.FocusPlayer, false, ref output); return; }
                    if (plan.GroupStage == 1)
                    {
                        if (plan.GroupReadyAt == 0) plan.GroupReadyAt = view.Time + 2.5f;
                        if (view.Time < plan.GroupReadyAt) return;
                    }
                    if (plan.GroupStage == 6 && view.Time < plan.GroupReadyAt) return;
                    if (plan.GroupStage == 2 && touch.TargetId == -20005 && touch.Actions > plan.GroupAction && view.SelectionVisible)
                    {
                        // A clipped rectangle can select only the visible half of an army.
                        // In that case order every squad card, including the off-screen troops,
                        // instead of treating a small selection as the whole assault.
                        plan.GroupStage = view.SelectedCount >= UnityEngine.Mathf.Max(8, UnityEngine.Mathf.CeilToInt(view.Infantry * .75f)) ? 3 : 4;
                        plan.RegroupAt = view.Time + 40;
                    }
                    else if (HasSelectionRectangle(view))
                    {
                        // Select only arms rectangle selection; it never selects a squad.
                        // Require both clear corners before entering that mode.
                        if (!view.SelectionMode) { Target(view.Select, false, ref output); return; }
                        plan.GroupStage = 2; plan.GroupAction = touch.Actions;
                        Target(view.GroupStart, true, ref output); output.Drag = 1; output.DragEnd = view.GroupEnd.Position; return;
                    }
                    else
                    {
                        if (view.Time < plan.GroupReadyAt + 5) return;
                        // A crowded/occluded view cannot safely fit a box; use a visible squad card.
                        if (!view.SelectionVisible) { Target(view.Squad(plan.Slot), false, ref output); return; }
                        // A card selects only one squad: retain normal squad rotation.
                        plan.GroupStage = 4; plan.RegroupAt = view.Time + 40;
                    }
                }
            }
            for (int i = 0; i < 5 && (view.AvailableSquads & (1 << plan.Slot)) == 0; i++) plan.Slot = (plan.Slot + 1) % 5;
            if (view.AvailableSquads == 0) { plan.Intent = AriaSkirmishIntent.ObserveBattle; return; }
            if (plan.GroupStage != 3 && (!view.SelectionVisible || view.SelectedSlot != plan.Slot))
            { plan.Intent = AriaSkirmishIntent.SelectSquad; Target(view.Squad(plan.Slot), false, ref output); return; }
            if (view.AssaultAtBase && view.EnemyBase.Available)
            {
                // The base is the win objective. A gathered force must not chase
                // every new recruit or logistics contact away from that objective.
                if (!view.AttackMode)
                { plan.Intent = AriaSkirmishIntent.Attack; Target(view.Attack, false, ref output); return; }
                plan.Intent = AriaSkirmishIntent.TargetBase;
                Target(view.EnemyBase, true, ref output);
                plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
                return;
            }
            // Attack Move handles enemies along the route. Retain the approach goal
            // instead of repeatedly pulling the army toward incidental structures.
            if (view.AdvancePreferred &&
                (view.AdvanceGround.Available || view.FocusAdvance.Available || !view.EnemyBase.Available))
            {
                if (!view.AdvanceGround.Available)
                {
                    plan.Intent = AriaSkirmishIntent.FindBase;
                    if (view.FocusAdvance.Available)
                    {
                        // Match FindThreat: one map trip per settle window. Re-opening
                        // immediately after Close Map is a no-progress gesture loop.
                        if (view.FocusAdvance.Id == -20010)
                        {
                            if (view.Time < plan.MapNavigationReadyAt)
                            {
                                if (view.FocusEnemy.Available)
                                    Target(view.FocusEnemy, false, ref output);
                                return;
                            }
                            plan.AdvanceNavigation = 1; plan.MapNavigationStage = 1;
                        }
                        Target(view.FocusAdvance, false, ref output);
                    }
                    else Target(view.FocusEnemy, false, ref output);
                    return;
                }
                if (!view.AttackMode)
                { plan.Intent = AriaSkirmishIntent.Attack; Target(view.Attack, false, ref output); return; }
                plan.Intent = AriaSkirmishIntent.Advance;
                Target(view.AdvanceGround, true, ref output);
                plan.TargetPending = 1; plan.ActionsAtTarget = touch.Actions;
                return;
            }
            bool threat = view.Threat.Available;
            if (!threat && view.FocusThreat.Available)
            {
                plan.Intent = AriaSkirmishIntent.FindThreat;
                if (view.Time >= plan.MapNavigationReadyAt)
                {
                    plan.MapNavigationStage = 1;
                    Target(view.FocusThreat, false, ref output);
                }
                return;
            }
            if (!threat && !view.EnemyBase.Available)
            { plan.Intent = AriaSkirmishIntent.FindBase; Target(view.FocusEnemy, false, ref output); return; }
            if (!view.AttackMode)
            { plan.Intent = AriaSkirmishIntent.Attack; Target(view.Attack, false, ref output); return; }
            plan.Intent = threat ? AriaSkirmishIntent.TargetThreat : AriaSkirmishIntent.TargetBase;
            Target(threat ? (view.ThreatGround.Available ? view.ThreatGround : view.Threat) : view.EnemyBase, true, ref output);
            plan.ActionsAtTarget = touch.Actions; plan.TargetPending = 1;
        }
        private static bool OpeningDefense(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            if (plan.DefenseStage == 4) return false;
            // Recruit before opening construction: the initial force must survive
            // the first raid while the player is occupied with placement.
            if (plan.DefenseStage == 0 && view.Infantry < 16) return false;
            if (plan.DefenseStage == 0)
            {
                // Only attempt construction when its normal visible controls exist.
                if (!view.DefenseBuild.Available || !view.FocusPlayer.Available)
                { plan.DefenseStage = 4; return false; }
                plan.DefenseStage = 1; plan.DefenseActions = touch.Actions;
                plan.DefenseDeadline = view.Time + 75;
            }
            plan.Intent = AriaSkirmishIntent.BuildDefense;
            if (view.Time > plan.DefenseDeadline)
            {
                if (view.PlacementOpen) { Target(view.PlacementCancel, false, ref output); return true; }
                if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return true; }
                plan.DefenseStage = 4; plan.Intent = AriaSkirmishIntent.Recruit; return false;
            }
            if (plan.DefenseStage == 1)
            {
                if (touch.Actions == plan.DefenseActions) { Target(view.FocusPlayer, false, ref output); return true; }
                if (plan.DefenseReadyAt == 0) plan.DefenseReadyAt = view.Time + 2;
                if (view.Time < plan.DefenseReadyAt) return true;
                plan.DefenseStage = 2;
            }
            if (plan.DefenseStage == 3)
            {
                if (view.PlacementOpen) { Target(view.PlacementConfirm, false, ref output); return true; }
                if (touch.Actions <= plan.DefenseActions) return true;
                plan.DefensesPlaced++;
                plan.DefensePositioned = 0; plan.DefenseSite++;
                if (plan.DefensesPlaced < 2) { plan.DefenseStage = 2; plan.DefenseReadyAt = view.Time + 1; }
                else { plan.DefenseStage = 4; plan.OpeningUntil = view.Time + 180; plan.Intent = AriaSkirmishIntent.Recruit; return false; }
            }
            if (view.Time < plan.DefenseReadyAt) return true;
            if (view.PlacementOpen)
            {
                if (plan.DefenseSitePending != 0)
                {
                    if (touch.Actions <= plan.DefenseSiteAction && view.Site(plan.DefenseSite).Available)
                    { Target(view.Site(plan.DefenseSite), true, ref output); return true; }
                    plan.DefenseSitePending = 0;
                    if (touch.Actions > plan.DefenseSiteAction)
                    {
                        plan.DefensePositioned = 1;
                        plan.DefenseReadyAt = view.Time + .5f; return true;
                    }
                    // Placement can recenter the camera while the hand approaches.
                    // A site hidden by the HUD is no longer a touchable destination.
                    // Try another visible site, then the normal valid preview/cancel controls.
                    plan.DefenseSite++; plan.DefensePositioned = 0;
                }
                // Reposition through an ordinary visible ground touch; the placement UI
                // remains the authority for terrain, footprint, roads and affordability.
                if (plan.DefensePositioned == 0 || !view.PlacementConfirm.Available)
                {
                    if (plan.DefensePositioned != 0) { plan.DefenseSite++; plan.DefensePositioned = 0; }
                    while (plan.DefenseSite < 6 && !view.Site(plan.DefenseSite).Available) plan.DefenseSite++;
                    if (plan.DefenseSite < 6)
                    {
                        plan.DefenseSitePending = 1; plan.DefenseSiteAction = touch.Actions;
                        Target(view.Site(plan.DefenseSite), true, ref output); return true;
                    }
                }
                if (view.PlacementConfirm.Available)
                {
                    plan.DefenseStage = 3; plan.DefenseActions = touch.Actions;
                    Target(view.PlacementConfirm, false, ref output);
                }
                // Never force an invalid site or bypass normal affordability checks.
                else { plan.DefenseDeadline = view.Time; Target(view.PlacementCancel, false, ref output); }
                return true;
            }
            Target(view.DefenseBuild, false, ref output); return true;
        }
        private static bool HasSelectionRectangle(in AriaSkirmishObservation view)
        {
            var size = view.GroupEnd.Position - view.GroupStart.Position;
            return view.GroupStart.Available && view.GroupEnd.Available &&
                UnityEngine.Mathf.Abs(size.x) >= 20 && UnityEngine.Mathf.Abs(size.y) >= 20;
        }
        private static void StepExpandedBaseAssault(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            // Public visible controls only. No gameplay Entity mutation from this planner.
            if (view.Finished)
            {
                plan.Intent = AriaSkirmishIntent.Handback;
                output.Kind = AriaPlayObservationKind.Finished;
                return;
            }
            if (touch.Phase == AriaPlayPhase.Touching) return;
            if (view.ExpandedRetries >= 3)
            {
                if (view.Hold.Available && plan.Intent == AriaSkirmishIntent.Attack)
                {
                    plan.Intent = AriaSkirmishIntent.Hold;
                    Target(view.Hold, false, ref output);
                    return;
                }
                plan.Intent = AriaSkirmishIntent.Handback;
                output.Kind = AriaPlayObservationKind.Waiting;
                return;
            }
            if (view.VisibleHostileAir > 0 && view.CanAffordAntiAir && view.RecruitAntiAir.Available)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                Target(view.RecruitAntiAir, false, ref output);
                return;
            }
            if (view.AirQueueOffered && !view.PadReady && view.AirPad.Available)
            {
                plan.Intent = AriaSkirmishIntent.Inspect;
                Target(view.AirPad, false, ref output);
                return;
            }
            if (view.Infantry < 16 && view.CanAffordRifle && view.Recruit.Available)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                Target(view.Recruit, false, ref output);
                return;
            }
            if (!view.SelectionVisible && view.Squad0.Available)
            {
                plan.Intent = AriaSkirmishIntent.SelectSquad;
                Target(view.Squad0, false, ref output);
                return;
            }
            if (view.EnemyDesignatedAlive && view.Attack.Available && view.SelectionVisible)
            {
                plan.Intent = AriaSkirmishIntent.Attack;
                Target(view.Attack, false, ref output);
                return;
            }
            if (view.Hold.Available)
            {
                plan.Intent = AriaSkirmishIntent.Hold;
                Target(view.Hold, false, ref output);
                return;
            }
            plan.Intent = AriaSkirmishIntent.Inspect;
            output.Kind = AriaPlayObservationKind.Waiting;
        }

        private static void Target(AriaTouchTarget target, bool world, ref AriaPlayObservationComponent output)
        {
            if (!target.Available) { output.Kind = AriaPlayObservationKind.Unavailable; return; }
            output.Kind = world ? AriaPlayObservationKind.WorldTarget : AriaPlayObservationKind.Control;
            output.TargetId = target.Id; output.Position = target.Position;
        }
    }
}
