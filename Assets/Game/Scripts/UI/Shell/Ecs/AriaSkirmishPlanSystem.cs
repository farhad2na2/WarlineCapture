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
            var em=state.EntityManager;
            using var cleanup=new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            foreach (var (view, plan, owner, storage) in SystemAPI.Query<RefRW<AriaSkirmishObservationComponent>, RefRW<AriaSkirmishPlanComponent>, RefRO<AriaSkirmishObservationOwner>>().WithEntityAccess())
            {
                var boundary=owner.ValueRO.Boundary;
                if(!em.Exists(boundary)||!em.HasComponent<UiShellRootComponent>(boundary))
                {cleanup.DestroyEntity(storage);continue;}
                if(!em.HasComponent<AriaPlaySessionComponent>(boundary)||!em.HasComponent<AriaPlayObservationComponent>(boundary)||
                    !em.HasComponent<UiShellStateComponent>(boundary)||em.GetComponentData<UiShellStateComponent>(boundary).ActiveRoute!=UIRoute.Match||
                    owner.ValueRO.TransitionSequence!=em.GetComponentData<UiShellStateComponent>(boundary).TransitionSequenceId)
                {
                    plan.ValueRW=default;
                    if(view.ValueRO.Value.Active)
                    {
                        view.ValueRW=default;
                        if(em.HasComponent<AriaPlaySessionComponent>(boundary))em.SetComponentData(boundary,default(AriaPlaySessionComponent));
                        if(em.HasComponent<AriaPlayObservationComponent>(boundary))em.SetComponentData(boundary,default(AriaPlayObservationComponent));
                    }
                    continue;
                }
                if(!view.ValueRO.Value.Active){plan.ValueRW=default;continue;}
                var touch=em.GetComponentData<AriaPlaySessionComponent>(boundary);
                var output=em.GetComponentData<AriaPlayObservationComponent>(boundary);
                Step(view.ValueRO.Value,ref plan.ValueRW,ref touch,ref output);
                em.SetComponentData(boundary,touch);em.SetComponentData(boundary,output);
            }
            cleanup.Playback(em);
        }
        public static void Step(in AriaSkirmishObservation view, ref AriaSkirmishPlanComponent plan,
            ref AriaPlaySessionComponent touch, ref AriaPlayObservationComponent output)
        {
            if(AriaS002NativeAdapter.CanHandle(view)){AriaS002NativeAdapter.Step(view,ref plan,ref touch,ref output);return;}
            output = new AriaPlayObservationComponent { Kind = view.Finished ? AriaPlayObservationKind.Finished : AriaPlayObservationKind.Waiting, Time = view.Time, Frame = view.Frame, GoalId = 10000 + plan.Cycle * 10 + plan.Slot };
            if (view.PublicObjective.Applicable)
            {
                if (StepPublicBoarding(view,ref plan,ref touch,ref output)) return;
                if (StepOffensiveAir(view,ref plan,ref touch,ref output)) return;
                if (StepPublicScan(view,ref plan,ref touch,ref output)) return;
                if (StepPublicObjectiveSupport(view,ref plan,ref touch,ref output)) return;
                plan.Intent = view.PublicObjective.Intent;
                StepPublicObjective(view, view.PublicObjective, ref plan.ObjectiveTouch, ref touch, ref output);
                if (plan.ObjectiveTouch.Stage == 4) plan.Intent = AriaSkirmishIntent.ObserveObjective;
                return;
            }
            if (view.ExpandedSession)
            {
                // Blocked remains stopped until the player explicitly retries ARIA.
                // Repeated failed taps must not erase the watchdog's evidence.
                // Manual is idle / pre-consent: still publish the presented control so
                // the cyan hand and DecisionSystem can see TargetId. Starting waits
                // for the touch driver. Touching is held inside StepExpanded.
                if (!view.Finished && touch.Phase is AriaPlayPhase.Blocked or AriaPlayPhase.Starting)
                    return;
                if (view.ExpandedRosterRevision != 0 && touch.Phase != AriaPlayPhase.Touching)
                {
                    if (plan.SweepRosterRevision != 0 && plan.SweepRosterRevision != view.ExpandedRosterRevision)
                    {
                        plan.PagedToAssault = 0;
                        plan.AssaultSelecting = 0;
                        plan.AssaultIssued = 0;
                        plan.StructureOrdered = 0;
                        plan.AssaultPageSeen = 0;
                        plan.PagesOrdered = 0;
                    }
                    plan.SweepRosterRevision = view.ExpandedRosterRevision;
                }
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
            if (view.OwnDefenseTowers >= 2 && plan.DefenseStage == 0)
                plan.DefenseStage = 4;
            if (plan.DefenseStage == 4) return false;
            // Recruit before opening construction: the initial force must survive
            // the first raid while the player is occupied with placement.
            if (plan.DefenseStage == 0 && view.Infantry < 16) return false;
            if (plan.DefenseStage == 0)
            {
                // Only attempt construction when its normal visible controls exist.
                if (!view.DefenseBuild.Available || !view.FocusPlayer.Available)
                { plan.DefenseStage = 4; return false; }
                plan.DefensesPlaced = view.OwnDefenseTowers;
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
                // A completed tap alone is not a purchased firing tower. Wait for
                // the public own-building census before advancing construction.
                if (view.ExpandedSession)
                {
                    if (view.OwnDefenseTowers <= plan.DefensesPlaced) return true;
                    plan.DefensesPlaced = view.OwnDefenseTowers;
                }
                else plan.DefensesPlaced++; // Legacy observations have no building census.
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
        private static void StepHomePadPlacement(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            plan.Intent = plan.PadStage == 8 ? AriaSkirmishIntent.WaitForAirPadSite : AriaSkirmishIntent.BuildAirPad;
            if (plan.PadStage == 8)
            {
                // No legal visible site was found in the bounded search. Wait for
                // the player or a fresh ARIA start rather than using camera position.
                if (view.PlacementOpen) Target(view.PlacementCancel, false, ref output);
                else output.Kind = AriaPlayObservationKind.Waiting;
                return;
            }
            if (view.Time > plan.PadDeadline)
            {
                plan.PadStage = 8;
                plan.Intent = AriaSkirmishIntent.WaitForAirPadSite;
                if (view.PlacementOpen) Target(view.PlacementCancel, false, ref output);
                return;
            }
            if (plan.PadStage == 1)
            {
                if (view.PlacementOpen) { plan.PadStage = 8; Target(view.PlacementCancel, false, ref output); return; }
                if (!view.FocusPlayer.Available) { plan.PadStage = 8; return; }
                plan.PadAction = touch.Actions;
                plan.PadStage = 2;
                Target(view.FocusPlayer, false, ref output);
                return;
            }
            if (plan.PadStage == 2)
            {
                if (view.PlacementOpen) { plan.PadStage = 8; Target(view.PlacementCancel, false, ref output); return; }
                if (touch.Actions <= plan.PadAction) { Target(view.FocusPlayer, false, ref output); return; }
                plan.PadStage = 3;
                plan.PadReadyAt = view.Time + .5f;
            }
            if (plan.PadStage == 3)
            {
                if (view.Time < plan.PadReadyAt) return;
                if (view.PlacementOpen) { plan.PadStage = 8; Target(view.PlacementCancel, false, ref output); return; }
                if (!view.AirPad.Available) return;
                plan.PadAction = touch.Actions;
                plan.PadTargetId = view.AirPad.Id;
                plan.PadStage = 10;
                Target(view.AirPad, false, ref output);
                return;
            }
            if (plan.PadStage == 10)
            {
                if (touch.Actions <= plan.PadAction)
                {
                    if (view.AirPad.Id == plan.PadTargetId) Target(view.AirPad, false, ref output);
                    return;
                }
                if (touch.TargetId != plan.PadTargetId) { plan.PadStage = 8; return; }
                if (!view.PlacementOpen)
                { plan.PadStage = 3; plan.PadReadyAt = view.Time + .2f; return; }
                plan.PadStage = 4;
                plan.PadSiteIndex = 0;
            }
            if (plan.PadStage == 4)
            {
                if (!view.PlacementOpen) { plan.PadStage = 8; return; }
                while (plan.PadSiteIndex < 4 && !view.PadSite(plan.PadSiteIndex).Available)
                    plan.PadSiteIndex++;
                if (plan.PadSiteIndex == 4)
                {
                    // Refocus once after construction changes the camera. The
                    // remaining deadline bounds the retry and eventual cancel.
                    if (plan.PadReadyAt >= 0)
                    {
                        plan.PadReadyAt = -1;
                        plan.PadSiteIndex = 0;
                        if (!view.FocusPlayer.Available) { plan.PadStage = 8; return; }
                        plan.PadAction = touch.Actions;
                        plan.PadStage = 9;
                        Target(view.FocusPlayer, false, ref output);
                        return;
                    }
                    plan.PadStage = 8;
                    plan.Intent = AriaSkirmishIntent.WaitForAirPadSite;
                    Target(view.PlacementCancel, false, ref output);
                    return;
                }
                plan.PadAction = touch.Actions;
                plan.PadStage = 5;
                Target(view.PadSite(plan.PadSiteIndex), true, ref output);
                return;
            }
            if (plan.PadStage == 5)
            {
                if (!view.PlacementOpen) { plan.PadStage = 8; return; }
                if (touch.Actions <= plan.PadAction)
                { Target(view.PadSite(plan.PadSiteIndex), true, ref output); return; }
                if (touch.TargetId != view.PadSite(plan.PadSiteIndex).Id)
                { plan.PadSiteIndex++; plan.PadStage = 4; return; }
                plan.PadFeedbackFrame = view.Frame;
                plan.PadStage = 6;
                return;
            }
            if (plan.PadStage == 6)
            {
                if (!view.PlacementOpen) { plan.PadStage = 8; return; }
                if (view.Frame <= plan.PadFeedbackFrame) return;
                if (!view.PlacementConfirm.Available)
                { plan.PadSiteIndex++; plan.PadStage = 4; return; }
                plan.PadAction = touch.Actions;
                plan.PadStage = 7;
                Target(view.PlacementConfirm, false, ref output);
                return;
            }
            if (plan.PadStage == 7)
            {
                if (touch.Actions <= plan.PadAction)
                { Target(view.PlacementConfirm, false, ref output); return; }
                if (!view.PlacementOpen && !view.PadPresent) plan.PadStage = 8;
            }
            if (plan.PadStage == 9)
            {
                if (touch.Actions <= plan.PadAction)
                { Target(view.FocusPlayer, false, ref output); return; }
                plan.PadStage = 4;
            }
        }

        private static bool StepExpandedAdvanceNavigation(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            if (plan.AdvanceNavigation == 0)
                return view.Time < plan.ObserveUntil && view.Time < plan.MapNavigationReadyAt;
            plan.Intent = AriaSkirmishIntent.FindBase;
            if (!view.MapOpen)
            {
                if (plan.MapNavigationStage == 1)
                {
                    Target(view.FocusAdvance, false, ref output);
                    return true;
                }
                plan.AdvanceNavigation = 0; plan.MapNavigationStage = 0; plan.MapOpenedAt = 0;
                plan.MapNavigationReadyAt = view.Time + 6;
                plan.ObserveUntil = view.Time + 1;
                return true;
            }
            if (plan.MapOpenedAt == 0) plan.MapOpenedAt = view.Time;
            if (plan.MapNavigationStage == 1)
            {
                if (!view.FocusAdvance.Available && view.Time < plan.MapOpenedAt + 5) return true;
                if (view.FocusAdvance.Available)
                {
                    plan.MapNavigationStage = 2;
                    plan.MapFocusActions = touch.Actions;
                }
            }
            if (plan.MapNavigationStage == 2 &&
                !(touch.TargetId == -20011 && touch.Actions > plan.MapFocusActions))
            {
                Target(view.FocusAdvance, false, ref output);
                if (view.FocusAdvanceDrag) { output.Drag = 1; output.DragEnd = view.FocusAdvanceDragEnd; }
                return true;
            }
            // One camera gesture, then close and settle before any separate order.
            plan.MapNavigationStage = 3;
            Target(view.CloseMap, false, ref output);
            return true;
        }

        static void ClearAssaultMapInspection(ref AriaSkirmishPlanComponent plan)
        {
            plan.AssaultInspectStage = 0; plan.AssaultInspectActions = 0; plan.AssaultInspectOpenTarget = 0;
            plan.AssaultInspectMapIdentity = 0; plan.AssaultInspectOpenedFrame = 0;
            plan.AssaultInspectRevision = 0; plan.AssaultInspectDeadline = 0; plan.AssaultInspectReadyAt = 0;
            plan.AssaultInspectObjectiveHudIdentity = 0; plan.AssaultInspectObjectiveWorld = default;
        }
        // A completed visible page is enough to inspect the moving cohort on the
        // normal map. Paging itself never renews the objective watchdog.
        static bool AcceptedAssaultInspectionPage(in AriaSkirmishObservation view,
            in AriaSkirmishPlanComponent plan)
            => plan.AssaultIssued == 0 && plan.PagesOrdered > 0 &&
               plan.AssaultPageSeen == view.ExpandedPageIndex + 1 &&
               plan.SweepRosterRevision == view.ExpandedRosterRevision &&
               view.ExpandedAssaultMask != 0 &&
               (view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask) == 0;

        static bool AssaultInspectionUiBusy(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan)
            => view.DrawerOpen || view.PlacementOpen || view.AttackMode ||
               (plan.AssaultSelecting != 0 && !AcceptedAssaultInspectionPage(view, plan)) ||
               plan.TargetPending != 0 || plan.AdvanceNavigation != 0 || plan.MapNavigationStage != 0 ||
               (plan.PadStage != 0 && plan.PadStage != 8) ||
               (view.AirProfile && plan.AirCycleCount == 0 && plan.AirCycleStage >= 1 && plan.AirCycleStage <= 4 &&
                view.OwnAttackAirLive > 0 && view.PadPresent && view.Time < plan.AirCycleDeadline);

        static bool CurrentAssaultDistance(in AriaSkirmishObservation view, in AriaSkirmishPlanComponent plan)
        {
            if (!view.AssaultDistanceVisible || view.AssaultMap.FullMap != view.MapOpen ||
                !view.AssaultMap.IsCurrent(view.Frame, view.Time)) return false;
            if (plan.AssaultInspectStage == 0 || !view.MapOpen) return true;
            return plan.AssaultInspectStage == 2 && view.Time <= plan.AssaultInspectDeadline &&
                view.AssaultMap.FullMap && view.AssaultMap.Identity == plan.AssaultInspectMapIdentity &&
                view.AssaultMap.MarkerRevision != plan.AssaultInspectRevision &&
                view.AssaultMap.MarkerFrame > plan.AssaultInspectOpenedFrame &&
                view.AssaultMap.ObjectiveHudIdentity == plan.AssaultInspectObjectiveHudIdentity &&
                (view.AssaultMap.ObjectiveWorld - plan.AssaultInspectObjectiveWorld).sqrMagnitude <= .0001f;
        }

        // Only normal map open/close touches. UI activity never renews progress.
        static bool StepAssaultMapInspection(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            if (plan.AssaultInspectStage == 0)
            {
                if (((plan.AssaultIssued == 0 || plan.StructureOrdered == 0) &&
                     !AcceptedAssaultInspectionPage(view, plan)) ||
                    (touch.Phase != AriaPlayPhase.Observing && touch.Phase != AriaPlayPhase.Waiting &&
                     !(touch.Phase == AriaPlayPhase.Verifying && touch.GestureRequested == 0 &&
                       touch.Pressed == 0 && touch.Actions > 0 &&
                       touch.LastCompletedTargetId == touch.TargetId && touch.LastCompletedFrame > 0 &&
                       view.Frame > touch.LastCompletedFrame && view.Time >= touch.DueAt)) ||
                    view.AssaultDistanceVisible || view.MapOpen || AssaultInspectionUiBusy(view,plan) ||
                    view.PresentedHomeThreat ||
                    view.Time < plan.AssaultInspectReadyAt || !view.AssaultMap.OpenMap.Available) return false;
                plan.AssaultInspectStage = 1;
                plan.AssaultInspectActions = touch.Actions;
                plan.AssaultInspectOpenTarget = view.AssaultMap.OpenMap.Id;
                plan.AssaultInspectDeadline = view.Time + 8f;
                plan.AssaultInspectObjectiveHudIdentity = view.AssaultMap.ObjectiveHudIdentity;
                plan.AssaultInspectObjectiveWorld = view.AssaultMap.ObjectiveWorld;
            }
            if (AssaultInspectionUiBusy(view,plan))
            {
                if (!view.MapOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return false;
                }
                if (plan.AssaultInspectStage != 3 && plan.AssaultInspectStage != 4)
                { plan.AssaultInspectStage = 3; plan.AssaultInspectDeadline = view.Time + 3f; }
            }
            plan.Intent = AriaSkirmishIntent.Inspect;
            if (plan.AssaultInspectStage == 1)
            {
                if (view.MapOpen)
                {
                    // A different control/user opening is not this requested operation.
                    if (touch.Actions <= plan.AssaultInspectActions ||
                        touch.TargetId != plan.AssaultInspectOpenTarget ||
                        !view.AssaultMap.ExpectedIdentity || !view.AssaultMap.FullMap ||
                        view.AssaultMap.Identity == 0)
                    { plan.AssaultInspectStage = 4; return true; }
                    plan.AssaultInspectMapIdentity = view.AssaultMap.Identity;
                    plan.AssaultInspectOpenedFrame = view.Frame;
                    plan.AssaultInspectRevision = view.AssaultMap.MarkerRevision;
                    plan.AssaultInspectStage = view.Time <= plan.AssaultInspectDeadline &&
                        view.AssaultMap.ObjectiveHudIdentity == plan.AssaultInspectObjectiveHudIdentity &&
                        (view.AssaultMap.ObjectiveWorld - plan.AssaultInspectObjectiveWorld).sqrMagnitude <= .0001f ? (byte)2 : (byte)3;
                    plan.AssaultInspectDeadline = view.Time + 3f;
                    return true; // Require a subsequent completed rendered marker update.
                }
                if (view.Time > plan.AssaultInspectDeadline || view.PresentedHomeThreat)
                { plan.AssaultInspectStage = 4; return true; }
                if (view.AssaultMap.OpenMap.Available &&
                    view.AssaultMap.OpenMap.Id == plan.AssaultInspectOpenTarget)
                    Target(view.AssaultMap.OpenMap, false, ref output);
                return true;
            }
            if (plan.AssaultInspectStage == 2)
            {
                if (!view.MapOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return true;
                }
                if (view.PresentedHomeThreat || view.Time > plan.AssaultInspectDeadline ||
                    view.AssaultMap.ObjectiveHudIdentity != plan.AssaultInspectObjectiveHudIdentity ||
                    (view.AssaultMap.ObjectiveWorld - plan.AssaultInspectObjectiveWorld).sqrMagnitude > .0001f ||
                    CurrentAssaultDistance(view, plan))
                { plan.AssaultInspectStage = 3; plan.AssaultInspectDeadline = view.Time + 3f; }
                else return true;
            }
            if (plan.AssaultInspectStage == 3)
            {
                if (!view.MapOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return true;
                }
                // Keep a reachable owned close target stable through normal hand aiming.
                // The existing input owner bounds attempts; the battle watchdog is unchanged.
                if (view.AssaultMap.ExpectedIdentity && view.AssaultMap.FullMap &&
                    view.AssaultMap.Identity == plan.AssaultInspectMapIdentity &&
                    view.CloseMap.Available) Target(view.CloseMap, false, ref output);
                else if (view.Time > plan.AssaultInspectDeadline) plan.AssaultInspectStage = 4;
                return true;
            }
            // Closed/failed open cannot starve ordinary production/navigation.
            // An unclosable/foreign open popup still refuses all inspection actions.
            if (!view.MapOpen)
            {
                ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                return false;
            }
            // A previously timed-out presentation may become closable again. Reuse only
            // the same owned popup, without resetting attempts, deadline or progress.
            if (view.AssaultMap.ExpectedIdentity && view.AssaultMap.FullMap &&
                view.AssaultMap.Identity == plan.AssaultInspectMapIdentity &&
                plan.AssaultInspectMapIdentity != 0 && view.CloseMap.Available)
            {
                plan.AssaultInspectStage = 3;
                Target(view.CloseMap, false, ref output);
                return true;
            }
            // Never dismiss a foreign/unreachable popup or wait here indefinitely.
            touch.Phase = AriaPlayPhase.Blocked;
            touch.GestureRequested = 0;
            return true;
        }

        private static void StepExpandedBaseAssault(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            // Public visible controls only. No gameplay Entity mutation from this planner.
            if (view.Finished || touch.Phase == AriaPlayPhase.Manual ||
                (plan.AssaultInspectStage == 0 &&
                 (plan.AssaultIssued == 0 || plan.StructureOrdered == 0) &&
                 !AcceptedAssaultInspectionPage(view, plan)))
                ClearAssaultMapInspection(ref plan);
            if (view.Finished)
            {
                plan.Intent = AriaSkirmishIntent.Handback;
                output.Kind = AriaPlayObservationKind.Finished;
                return;
            }
            if (touch.Phase == AriaPlayPhase.Touching) return;
            // NEXT reuses its button identity across pages. A changed visible
            // page proves the tap succeeded; do not count later inspections as
            // retries of that completed action. Paging is not battle progress.
            if (plan.ObservedPageIndex != view.ExpandedPageIndex)
            {
                plan.ObservedPageIndex = view.ExpandedPageIndex;
                touch.Attempts = 0;
            }
            // Supply and construction are public progress, but each milestone
            // counts once. Repeated taps and spending/earning loops cannot
            // indefinitely refresh the watchdog.
            var economicState=new Game.Simulation.AriaEconomicProgressState{EconomicMilestones=plan.EconomicMilestones,MaterialsHighWater=plan.MaterialsHighWater};
            var economic=Game.Simulation.AriaEconomicProgressPolicy.Observe(new Game.Simulation.AriaEconomicProgressView {
                AirProfile=view.AirProfile,LogisticsTruckCommitted=view.LogisticsTruckCommitted,ReadinessEligible=view.ReadinessEligible,PadPresent=view.PadPresent,AirQueueOffered=view.AirQueueOffered,OwnMaterials=view.OwnMaterials },ref economicState);
            plan.EconomicMilestones=economicState.EconomicMilestones;plan.MaterialsHighWater=economicState.MaterialsHighWater;
            bool newMilestone=economic.NewMilestone,supplyProgress=economic.SupplyProgress;
            // Only a new closest approach on the player's presented minimap
            // counts. Paging, jitter and retreat/re-advance cannot mask a stall.
            bool assaultAdvance = false;
            // Public accepted assault intent may precede completion of the full roster sweep.
            bool acceptedAssaultOnPage = (view.ExpandedAssaultMask & view.ExpandedAttackOrderMask) != 0;
            if (touch.Phase != AriaPlayPhase.Manual &&
                (plan.AssaultIssued != 0 || acceptedAssaultOnPage) && CurrentAssaultDistance(view, plan) &&
                view.NearestFriendlyObjectiveDistance >= 0f &&
                !float.IsNaN(view.NearestFriendlyObjectiveDistance) &&
                !float.IsInfinity(view.NearestFriendlyObjectiveDistance))
            {
                if (plan.AssaultObjectiveHudIdentity != view.AssaultMap.ObjectiveHudIdentity ||
                    (plan.AssaultObjectiveWorld - view.AssaultMap.ObjectiveWorld).sqrMagnitude > .0001f)
                {
                    plan.AssaultDistanceObserved = 0;
                    plan.AssaultObjectiveHudIdentity = view.AssaultMap.ObjectiveHudIdentity;
                    plan.AssaultObjectiveWorld = view.AssaultMap.ObjectiveWorld;
                }
                if (plan.AssaultDistanceObserved == 0)
                {
                    plan.AssaultDistanceObserved = 1;
                    plan.BestAssaultDistance = view.NearestFriendlyObjectiveDistance;
                }
                else if (view.NearestFriendlyObjectiveDistance <= plan.BestAssaultDistance - 2f)
                {
                    plan.BestAssaultDistance = view.NearestFriendlyObjectiveDistance;
                    assaultAdvance = true;
                }
            }
            var progress=new Game.Simulation.AriaGroundProgressState{LastProgressAt=plan.LastProgressAt,EnemyHealth=plan.EnemyHealth,PlayerHealth=plan.PlayerHealth,Infantry=plan.Infantry};
            if(Game.Simulation.AriaGroundProgressPolicy.Observe(view.Time,view.EnemyHealth,view.PlayerHealth,view.Infantry,newMilestone,supplyProgress,assaultAdvance,ref progress,out bool blocked))
            {touch.LastProgressAt=view.Time;touch.LastObjectiveProgressAt=view.Time;}
            plan.LastProgressAt=progress.LastProgressAt;plan.EnemyHealth=progress.EnemyHealth;plan.PlayerHealth=progress.PlayerHealth;plan.Infantry=progress.Infantry;
            if(blocked){touch.Phase=AriaPlayPhase.Blocked;touch.GestureRequested=0;return;}
            if (plan.PrecisionStage != 0)
            {
                // Actual home defence may interrupt; accepted group history is retained.
                if ((view.PresentedHomeThreat || plan.HomeDefenseStage != 0) &&
                    StepPresentedHomeDefense(view, ref plan, ref touch, ref output))
                {
                    if (plan.PrecisionGroup != 0 &&
                        !PrecisionContains(plan.PrecisionExhaustedGroups, plan.PrecisionGroup) &&
                        plan.PrecisionExhaustedGroups.Length < plan.PrecisionExhaustedGroups.Capacity)
                        plan.PrecisionExhaustedGroups.Add(plan.PrecisionGroup);
                    plan.PrecisionStage = view.AttackMode ? (byte)5 : (byte)0; return;
                }
                if (StepPrecisionAttack(view, ref plan, ref touch, ref output)) return;
            }
            if (StepAssaultMapInspection(view, ref plan, ref touch, ref output)) return;
            if (StepExpandedAdvanceNavigation(view, ref plan, ref touch, ref output)) return;
            if (StepPresentedHomeDefense(view, ref plan, ref touch, ref output)) return;
            // An air-capable starting force cannot leave its base undefended.
            // Reuse the visible, bounded construction sequence used by legacy
            // skirmishes after the first reinforcement squad arrives.
            if (view.AirProfile && plan.AssaultStarted == 0 &&
                OpeningDefense(view, ref plan, ref touch, ref output)) return;
            if (view.PadPresent) plan.PadStage = 0;
            if (plan.PadStage != 0)
            {
                StepHomePadPlacement(view, ref plan, ref touch, ref output);
                return;
            }
            if (view.PlacementOpen)
            {
                // A placement opened outside this plan has no proven home site.
                plan.Intent = AriaSkirmishIntent.Inspect;
                Target(view.PlacementCancel, false, ref output);
                return;
            }
            // Preserve a viable base-assault force, then establish physical
            // Oil and Fuel delivery before research or repeated aircraft sorties.
            // These are visible paid catalog controls, with queue/capacity eligibility.
            var preparationView=new Game.Simulation.AriaPreparationView {
                AirProfile=view.AirProfile,CombinedArmsField=view.CombinedArmsField,Infantry=view.Infantry,Time=view.Time,OwnStructureAttackUnits=view.OwnStructureAttackUnits,
                LogisticsTruckCommitted=view.LogisticsTruckCommitted,CanAffordLogisticsTruck=view.CanAffordLogisticsTruck,LogisticsTargetAvailable=view.RecruitLogisticsTruck.Available,
                TankersCommitted=view.TankersCommitted,CanAffordTanker=view.CanAffordTanker,TankerTargetAvailable=view.RecruitTanker.Available };
            var preparationState=new Game.Simulation.AriaPreparationState{AssaultStarted=plan.AssaultStarted,OpeningUntil=plan.OpeningUntil};
            var preparationAction=Game.Simulation.AriaExpandedPreparationPolicy.Supply(preparationView,ref preparationState);
            plan.AssaultStarted=preparationState.AssaultStarted;
            if(preparationAction==Game.Simulation.AriaPreparationAction.RecruitLogisticsTruck){plan.Intent=AriaSkirmishIntent.Recruit;Target(view.RecruitLogisticsTruck,false,ref output);return;}
            if(preparationAction==Game.Simulation.AriaPreparationAction.RecruitTanker){plan.Intent=AriaSkirmishIntent.Recruit;Target(view.RecruitTanker,false,ref output);return;}
            if (StepAircraftCycle(view, ref plan, ref touch, ref output)) return;
            if (StepAssaultObjectiveFocus(view, ref plan, ref touch, ref output)) return;
            if (StepPrecisionAttack(view, ref plan, ref touch, ref output)) return;
            if (view.AttackMode && view.SelectionVisible)
            {
                var objectiveContact=Game.Simulation.AriaGroundObjectivePolicy.Contact(view.EnemyBase.Available,view.AdvanceGround.Available);
                if (objectiveContact==Game.Simulation.AriaObjectiveContact.VisibleEnemyTarget)
                {
                    plan.Intent = AriaSkirmishIntent.TargetBase;
                    Target(view.EnemyBase, true, ref output);
                }
                else if (objectiveContact==Game.Simulation.AriaObjectiveContact.PublicTerrainApproach)
                {
                    // The labelled objective is public; a hidden base is not a precision target.
                    // Offer the already presented terrain approach for a separate normal Attack Move touch.
                    // Camera focus alone cannot reveal a contact and must never issue this order.
                    plan.Intent = AriaSkirmishIntent.Advance;
                    Target(view.AdvanceGround, true, ref output);
                }
                else
                {
                    plan.Intent = AriaSkirmishIntent.FindBase;
                    if (view.FocusAdvance.Available &&
                        (view.FocusAdvance.Id == -20010 || view.FocusAdvance.Id == -20011))
                    {
                        if (view.Time < plan.MapNavigationReadyAt) return;
                        plan.AdvanceNavigation = 1; plan.MapNavigationStage = 1;
                        StepExpandedAdvanceNavigation(view, ref plan, ref touch, ref output);
                    }
                    else Target(view.FocusEnemy, false, ref output);
                }
                return;
            }
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
            if (view.VisibleHostileAir > 0 && !view.AntiAirCommitted && view.CanAffordAntiAir && view.RecruitAntiAir.Available)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                Target(view.RecruitAntiAir, false, ref output);
                return;
            }
            // Combined Arms needs a viable structure force before spending
            // its remaining materials on rifles that cannot hurt the base.
            if (view.CombinedArmsField && view.OwnStructureAttackUnits < 8 && view.Infantry < 24)
            {
                if (view.RocketeerRecruitPending)
                {
                    plan.Intent = AriaSkirmishIntent.ObserveBattle;
                    if (view.DrawerOpen) Target(view.CloseDrawer, false, ref output);
                    return;
                }
                if (view.CanAffordRocketeer && view.RecruitRocketeer.Available)
                {
                    plan.Intent = AriaSkirmishIntent.Recruit;
                    Target(view.RecruitRocketeer, false, ref output);
                    return;
                }
            }
            if (view.AirProfile && plan.AssaultStarted == 0)
            {
                if (plan.OpeningUntil == 0) plan.OpeningUntil = view.Time + 75f;
                if (view.Infantry < 20 && view.Time < plan.OpeningUntil)
                {
                    if (view.RifleRecruitPending)
                    {
                        plan.Intent = AriaSkirmishIntent.ObserveBattle;
                        if (view.DrawerOpen) Target(view.CloseDrawer, false, ref output);
                        else output.Kind = AriaPlayObservationKind.Waiting;
                        return;
                    }
                    if (view.CanAffordRifle)
                    {
                        plan.Intent = AriaSkirmishIntent.Recruit;
                        // Drawer/category transitions can briefly hide their next
                        // control. Keep the bounded opening instead of treating
                        // one unavailable frame as an unaffordable reinforcement.
                        if (view.Recruit.Available) Target(view.Recruit, false, ref output);
                        else output.Kind = AriaPlayObservationKind.Waiting;
                        return;
                    }
                }
                plan.AssaultStarted = 1;
            }
            if (!view.ReadinessEligible && view.CanUpgradeReadiness && view.UpgradeReadiness.Available &&
                plan.AssaultIssued != 0)
            {
                plan.Intent = AriaSkirmishIntent.Inspect;
                Target(view.UpgradeReadiness, false, ref output);
                return;
            }
            if (view.ReadinessEligible && !view.PadPresent && view.CanBuildAirPad && view.AirPad.Available)
            {
                plan.Intent = AriaSkirmishIntent.Inspect;
                plan.PadStage = 1;
                plan.PadDeadline = view.Time + 45f;
                StepHomePadPlacement(view, ref plan, ref touch, ref output);
                return;
            }
            if (view.AirQueueOffered && !view.AirRecruitPending && view.OwnAttackAirLive == 0 && view.RecruitAir.Available)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                Target(view.RecruitAir, false, ref output);
                return;
            }
            if (view.DrawerOpen)
            {
                if (!view.AirProfile && !view.RifleRecruitPending && plan.Intent == AriaSkirmishIntent.Recruit && view.Infantry < 16 &&
                    view.CanAffordRifle && view.Recruit.Available &&
                    view.VisibleHostileAir == 0 && !view.AirQueueOffered)
                {
                    Target(view.Recruit, false, ref output);
                    return;
                }
                // Once a purchase/research action is accepted, return to the
                // visible army controls before interpreting their page state.
                plan.Intent = AriaSkirmishIntent.Inspect;
                Target(view.CloseDrawer, false, ref output);
                return;
            }
            if (plan.AssaultIssued != 0 &&
                (view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask) != 0)
            {
                plan.AssaultIssued = 0;
                plan.PagesOrdered = 0;
                plan.PagedToAssault = 0;
            }
            if (TryExpandedGroundAssault(view, ref plan, ref output))
                return;
            // The tank and rocketeers are already ordered. Do not replace that
            // column with the rifle card on page 0.
            if (plan.AssaultIssued != 0 && plan.StructureOrdered != 0)
            {
                // New reinforcements can arrive on another page. Inspect the
                // public tray periodically without replacing existing orders.
                if (plan.MapNavigationStage == 1 && view.ExpandedPageIndex != plan.AssaultPageSeen)
                {
                    plan.MapNavigationStage = 0;
                    plan.ObserveUntil = view.Time + 12f;
                }
                if (view.NextSquadPage.Available &&
                    (plan.MapNavigationStage == 1 || view.Time >= plan.ObserveUntil))
                {
                    if (plan.MapNavigationStage == 0)
                    { plan.MapNavigationStage = 1; plan.AssaultPageSeen = view.ExpandedPageIndex; }
                    plan.Intent = AriaSkirmishIntent.Inspect;
                    Target(view.NextSquadPage, false, ref output);
                    return;
                }
                plan.Intent = AriaSkirmishIntent.ObserveBattle;
                output.Kind = AriaPlayObservationKind.Waiting;
                return;
            }
            // Rifles cannot hurt a Barracks. Do not spend the attack button on them
            // while a later page still holds the tank and rocketeers.
            if (view.EnemyDesignatedAlive && plan.AssaultIssued == 0 &&
                view.ExpandedAssaultMask == 0 && view.NextSquadPage.Available)
            {
                plan.Intent = AriaSkirmishIntent.Inspect;
                output.Kind = AriaPlayObservationKind.Waiting;
                return;
            }
            if (Game.Simulation.AriaExpandedPreparationPolicy.FallbackGroundRecruit(new Game.Simulation.AriaPreparationView {
                AirProfile=view.AirProfile,RifleRecruitPending=view.RifleRecruitPending,Infantry=view.Infantry,CanAffordRifle=view.CanAffordRifle,RifleTargetAvailable=view.Recruit.Available }) == Game.Simulation.AriaPreparationAction.RecruitRifle)
            {
                plan.Intent = AriaSkirmishIntent.Recruit;
                Target(view.Recruit, false, ref output);
                return;
            }
            if (!view.SelectionVisible && view.Squad0.Available &&
                SelectionCanDamageDesignatedBase(view, squad0WithoutSelection: true))
            {
                plan.Intent = AriaSkirmishIntent.SelectSquad;
                Target(view.Squad0, false, ref output);
                return;
            }
            // Rifles and cars cannot damage a Barracks. Attack only when the
            // visible selection includes a structure card (tank, rocketeer,
            // breacher, siege), or when this observation has no page model.
            if (view.EnemyDesignatedAlive && view.Attack.Available && view.SelectionVisible &&
                SelectionCanDamageDesignatedBase(view, squad0WithoutSelection: false))
            {
                plan.Intent = AriaSkirmishIntent.Attack;
                Target(view.Attack, false, ref output);
                return;
            }
            // A visible Hold button is not an actionable order without a living
            // selection. Let an already-issued assault continue toward the base
            // instead of spending three touch retries on an unselected command.
            if (view.Hold.Available && view.SelectionVisible && view.ExpandedSelectedGroups > 0)
            {
                plan.Intent = AriaSkirmishIntent.Hold;
                Target(view.Hold, false, ref output);
                return;
            }
            plan.Intent = AriaSkirmishIntent.ObserveBattle;
            output.Kind = AriaPlayObservationKind.Waiting;
        }

        private static bool StepPresentedHomeDefense(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            var facts=new Game.Simulation.AriaHomeDefenseView {
                PresentedHomeThreat=view.PresentedHomeThreat,PlacementOpen=view.PlacementOpen,DrawerOpen=view.DrawerOpen,MapOpen=view.MapOpen,AttackMode=view.AttackMode,
                CancelAvailable=view.PlacementCancel.Available,CloseDrawerAvailable=view.CloseDrawer.Available,CloseMapAvailable=view.CloseMap.Available,
                FocusAvailable=view.FocusPlayer.Available,PreviousAvailable=view.PreviousSquadPage.Available,NextAvailable=view.NextSquadPage.Available,
                ToggleAvailable=view.SelectedSquadToToggle.Available,AttackAvailable=view.Attack.Available,
                ThreatGroundAvailable=view.ThreatGround.Available,ThreatVisibleAvailable=view.Threat.Available,
                ExpandedPageIndex=view.ExpandedPageIndex,ExpandedSelectedGroups=view.ExpandedSelectedGroups,ExpandedAssaultMask=view.ExpandedAssaultMask,ExpandedSelectedMask=view.ExpandedSelectedMask,
                CompletedContacts=touch.Actions,Time=view.Time };
            for(int slot=0;slot<5;slot++)if(view.Squad(slot).Available)facts.AvailableSquadMask|=1<<slot;
            var state=new Game.Simulation.AriaHomeDefenseState {HomeDefenseStage=plan.HomeDefenseStage,HomeDefenseAction=plan.HomeDefenseAction,HomeDefensePage=plan.HomeDefensePage,DefenseStage=plan.DefenseStage,HomeDefenseObserveUntil=plan.HomeDefenseObserveUntil,
                Column=new Game.Simulation.AriaGroundColumnState {AssaultIssued=plan.AssaultIssued,PagedToAssault=plan.PagedToAssault,AssaultSelecting=plan.AssaultSelecting,StructureOrdered=plan.StructureOrdered,PagesOrdered=plan.PagesOrdered,AssaultPageSeen=plan.AssaultPageSeen,SelectionResetPage=plan.SelectionResetPage}};
            bool handled=Game.Simulation.AriaHomeDefensePolicy.Step(facts,ref state,out var decision);
            plan.HomeDefenseStage=state.HomeDefenseStage;plan.HomeDefenseAction=state.HomeDefenseAction;plan.HomeDefensePage=state.HomeDefensePage;plan.DefenseStage=state.DefenseStage;plan.HomeDefenseObserveUntil=state.HomeDefenseObserveUntil;
            plan.AssaultIssued=state.Column.AssaultIssued;plan.PagedToAssault=state.Column.PagedToAssault;plan.AssaultSelecting=state.Column.AssaultSelecting;plan.StructureOrdered=state.Column.StructureOrdered;plan.PagesOrdered=state.Column.PagesOrdered;plan.AssaultPageSeen=state.Column.AssaultPageSeen;plan.SelectionResetPage=state.Column.SelectionResetPage;
            if(decision.Defend)plan.Intent=AriaSkirmishIntent.DefendBase;
            switch(decision.Action){
                case Game.Simulation.AriaHomeDefenseAction.Waiting:output.Kind=AriaPlayObservationKind.Waiting;break;
                case Game.Simulation.AriaHomeDefenseAction.Unavailable:output.Kind=AriaPlayObservationKind.Unavailable;break;
                case Game.Simulation.AriaHomeDefenseAction.CancelPlacement:Target(view.PlacementCancel,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.CloseDrawer:Target(view.CloseDrawer,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.CloseMap:Target(view.CloseMap,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.InspectHome:Target(view.FocusPlayer,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.PreviousPage:Target(view.PreviousSquadPage,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.NextPage:Target(view.NextSquadPage,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.ToggleSelected:Target(view.SelectedSquadToToggle,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.SelectSlot:Target(view.Squad(decision.Slot),false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.ArmAttack:Target(view.Attack,false,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.AttackThreatTerrain:Target(view.ThreatGround,true,ref output);break;
                case Game.Simulation.AriaHomeDefenseAction.AttackThreatVisible:Target(view.Threat,true,ref output);break;
            }
            return handled;
        }

        private static bool StepAircraftCycle(in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan, ref AriaPlaySessionComponent touch,
            ref AriaPlayObservationComponent output)
        {
            if (!view.AirProfile || plan.AirCycleCount != 0) return false;
            if (plan.AirCycleStage == 0)
            {
                if (view.OwnAttackAirActive == 0 || plan.AssaultIssued == 0)
                { plan.AirSortieObservedAt = 0; return false; }
                if (plan.AirSortieObservedAt == 0) plan.AirSortieObservedAt = view.Time;
                if (view.Time - plan.AirSortieObservedAt < 45f) return false;
                plan.AirCycleStage = 1;
                plan.AirCycleDeadline = view.Time + 120f;
            }
            if (view.OwnAttackAirLive == 0 || !view.PadPresent || view.Time >= plan.AirCycleDeadline)
            {
                plan.AirCycleStage = 0;
                plan.AirCycleCount = 2; // Bounded recovery; normal assault remains available.
                return false;
            }
            if (plan.AirCycleStage == 1)
            {
                plan.Intent = AriaSkirmishIntent.ReturnAircraft;
                if (view.DrawerOpen) { Target(view.CloseDrawer, false, ref output); return true; }
                if (view.SelectedAircraft && view.ExpandedSelectedGroups <= 1) { plan.AirCycleStage = 2; }
                else
                {
                    if (TryDeselectSquads(view, ref output)) return true;
                    for (int slot = 0; slot < 5; slot++)
                        if ((view.ExpandedAirMask & (1 << slot)) != 0 && view.Squad(slot).Available)
                        { Target(view.Squad(slot), false, ref output); return true; }
                    if (TryDeselectSquads(view, ref output)) return true;
                    if (view.NextSquadPage.Available)
                        Target(view.NextSquadPage, false, ref output);
                    else output.Kind = AriaPlayObservationKind.Waiting;
                    return true;
                }
            }
            if (plan.AirCycleStage == 2)
            {
                plan.Intent = AriaSkirmishIntent.ReturnAircraft;
                if (!view.SelectedAircraft) { plan.AirCycleStage = 1; return true; }
                if (!view.ReturnAircraft.Available)
                {
                    if (view.OpenAircraftActions.Available) Target(view.OpenAircraftActions, false, ref output);
                    else output.Kind = AriaPlayObservationKind.Waiting;
                    return true;
                }
                plan.AirCycleAction = touch.Actions;
                plan.AirCycleStage = 3;
                Target(view.ReturnAircraft, false, ref output);
                return true;
            }
            if (plan.AirCycleStage == 3)
            {
                plan.Intent = AriaSkirmishIntent.ReturnAircraft;
                if (touch.Actions <= plan.AirCycleAction)
                {
                    if (view.ReturnAircraft.Available) Target(view.ReturnAircraft, false, ref output);
                    else output.Kind = AriaPlayObservationKind.Waiting;
                    return true;
                }
                if (view.OwnAttackAirActive == 0 && view.OwnAttackAirLanded > 0)
                {
                    plan.AirCycleStage = 4;
                    plan.AirServiceReadyAt = view.Time + 5f;
                }
                else { output.Kind = AriaPlayObservationKind.Waiting; return true; }
            }
            plan.Intent = AriaSkirmishIntent.ServiceAircraft;
            if (view.Time < plan.AirServiceReadyAt || view.OwnAirFuel <= 0)
            { output.Kind = AriaPlayObservationKind.Waiting; return true; }
            // The landed aircraft is serviced by shared home/fuel owners. Allow
            // the visible squad-card attack sequence to issue a second sortie.
            plan.AirCycleStage = 0;
            plan.AirCycleCount = 1;
            plan.AssaultIssued = 0;
            plan.PagesOrdered = 0;
            plan.StructureOrdered = 0;
            plan.ObserveUntil = view.Time + 1f;
            output.Kind = AriaPlayObservationKind.Waiting;
            return true;
        }

        private static bool SelectionCanDamageDesignatedBase(
            in AriaSkirmishObservation view,
            bool squad0WithoutSelection)
        {
            bool pageModel = view.ExpandedTotalGroups > 0 || view.ExpandedStructureMask != 0 || view.ExpandedAssaultMask != 0;
            if (!pageModel)
                return true;
            if (squad0WithoutSelection)
                return (view.ExpandedStructureMask & 1) != 0;
            return (view.ExpandedStructureMask & view.ExpandedSelectedMask) != 0;
        }

        private static bool PrecisionContains(in Unity.Collections.FixedList512Bytes<uint> ids,uint group)
        {
            for(int i=0;i<ids.Length;i++)if(ids[i]==group)return true;
            return false;
        }
        // A public camera control only. The rendered/fog/reachability target gates remain authoritative.
        private static bool StepAssaultObjectiveFocus(in AriaSkirmishObservation v,
            ref AriaSkirmishPlanComponent p, ref AriaPlaySessionComponent t,
            ref AriaPlayObservationComponent o)
        {
            if(p.AssaultFocusStage==2)return false; // One bounded attempt per plan/session, including roster churn.
            if(!v.ExpandedBaseAssault||v.Finished||t.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked or AriaPlayPhase.Starting)
                return false;
            if(p.AssaultFocusStage==0)
            {
                if(p.AssaultIssued==0||p.StructureOrdered==0||!v.EnemyDesignatedAlive||!v.AssaultAtBase||
                    !CurrentAssaultDistance(v,p)||v.EnemyBase.Available||
                    (t.Phase!=AriaPlayPhase.Observing&&t.Phase!=AriaPlayPhase.Waiting)||
                    v.DrawerOpen||v.MapOpen||v.PlacementOpen||v.AttackMode||v.PresentedHomeThreat||
                    p.HomeDefenseStage!=0||p.PrecisionStage!=0||p.AssaultInspectStage!=0||
                    AssaultInspectionUiBusy(v,p)||!v.FocusEnemy.Available||v.FocusEnemy.Id==0||
                    v.FocusEnemy.Id==-20001||v.FocusEnemy.Drag)return false;
                p.AssaultFocusStage=1;p.AssaultFocusActions=t.Actions;
                p.AssaultFocusTarget=v.FocusEnemy.Id;p.AssaultFocusFrame=v.Frame;
                p.AssaultFocusDeadline=v.Time+8f;
            }
            if(t.Actions>p.AssaultFocusActions&&t.LastCompletedTargetId==p.AssaultFocusTarget&&
                t.LastCompletedFrame>=p.AssaultFocusFrame)
            {p.AssaultFocusStage=2;return true;}
            if(v.Time>=p.AssaultFocusDeadline||p.AssaultIssued==0||p.StructureOrdered==0||
                v.DrawerOpen||v.MapOpen||v.PlacementOpen||v.AttackMode||v.PresentedHomeThreat||
                p.HomeDefenseStage!=0||!v.FocusEnemy.Available||v.FocusEnemy.Id!=p.AssaultFocusTarget)
            {p.AssaultFocusStage=2;return false;}
            p.Intent=AriaSkirmishIntent.FindBase;
            Target(v.FocusEnemy,false,ref o);
            return true; // No selection, Attack toggle, terrain/target order, accepted ack or progress-clock write.
        }

        private static bool StepPrecisionAttack(in AriaSkirmishObservation v,ref AriaSkirmishPlanComponent p,
            ref AriaPlaySessionComponent touch,ref AriaPlayObservationComponent output)
        {
            if(v.Finished||touch.Phase==AriaPlayPhase.Manual)
            {p.PrecisionStage=0;return false;}
            // Once precision owns Attack mode, a missing target never becomes a terrain order.
            if(p.PrecisionStage==5)
            {
                if(v.AttackMode)return true;
                p.PrecisionStage=0;return false;
            }
            if(p.PrecisionStage==4)
            {
                var ack=v.PrecisionAttack;
                bool exact=v.ExpandedBaseAssault&&v.ExpandedRosterRevision==p.PrecisionRoster&&
                    v.ExpandedPageIndex==p.PrecisionPage&&v.ExpandedGroup(p.PrecisionSlot)==p.PrecisionGroup&&
                    ack.Presented&&ack.Revision>p.PrecisionBaseline&&ack.GroupId==p.PrecisionGroup&&
                    ack.PublicTargetId==-20001&&ack.RequestId>0&&ack.Frame>=p.PrecisionIssueFrame&&
                    ack.AcceptedMembers>0&&ack.AcceptedMembers<=ack.SourceMembers&&
                    touch.Actions>p.PrecisionActions&&touch.LastCompletedTargetId==-20001&&
                    touch.LastCompletedFrame>=p.PrecisionIssueFrame&&
                    UnityEngine.Mathf.Abs(touch.LastCompletedFrame-ack.Frame)<=2&&
                    (touch.LastCompletedPosition-ack.ScreenPosition).sqrMagnitude<=4f;
                if(exact)
                {
                    if(!PrecisionContains(p.PrecisionAcceptedGroups,p.PrecisionGroup)&&
                        p.PrecisionAcceptedGroups.Length<p.PrecisionAcceptedGroups.Capacity)
                        p.PrecisionAcceptedGroups.Add(p.PrecisionGroup);
                    p.PrecisionStage=0;p.PrecisionAttempts=0;
                    p.Intent=AriaSkirmishIntent.ObserveBattle;
                    return true; // Never issue another group's order in the acknowledgement frame.
                }
                if(v.Time<p.PrecisionDeadline)
                {
                    // Publishing a target starts normal DecisionSystem aiming; it is not a tap.
                    // Keep only the SAME current public target available until the owned gesture completes.
                    bool current=v.Active&&v.ExpandedSession&&v.ExpandedBaseAssault&&v.EnemyDesignatedAlive&&
                        v.Frame>=p.PrecisionIssueFrame&&v.ExpandedRosterRevision==p.PrecisionRoster&&
                        v.ExpandedPageIndex==p.PrecisionPage&&v.ExpandedGroup(p.PrecisionSlot)==p.PrecisionGroup&&
                        (v.ExpandedStructureMask&v.ExpandedAssaultMask&~v.ExpandedAirMask&(1<<p.PrecisionSlot))!=0&&
                        v.ExpandedSelectedGroups==1&&v.ExpandedSelectedOffPage==0&&
                        v.ExpandedSelectedMask==(1<<p.PrecisionSlot)&&v.SelectionVisible&&v.AttackMode&&
                        v.EnemyBase.Available&&v.EnemyBase.Id==-20001&&!v.EnemyBase.Drag&&
                        p.AssaultIssued!=0&&p.StructureOrdered!=0&&!v.DrawerOpen&&!v.MapOpen&&!v.PlacementOpen&&
                        !v.PresentedHomeThreat&&p.HomeDefenseStage==0;
                    if(current&&touch.Actions==p.PrecisionActions)
                        Target(v.EnemyBase,true,ref output);
                    return true;
                }
                if(p.PrecisionAttempts>=2||!v.EnemyBase.Available||v.AttackMode)
                {
                    if(p.PrecisionExhaustedGroups.Length<p.PrecisionExhaustedGroups.Capacity)
                        p.PrecisionExhaustedGroups.Add(p.PrecisionGroup);
                    p.PrecisionStage=v.AttackMode?(byte)5:(byte)0;
                    return true;
                }
                p.PrecisionStage=1;p.PrecisionReadyAt=v.Time+3f;p.PrecisionDeadline=v.Time+18f;
                return true;
            }
            if(p.PrecisionStage==0)
            {
                if(!v.ExpandedBaseAssault||p.AssaultIssued==0||p.StructureOrdered==0||!v.EnemyDesignatedAlive||
                    !v.EnemyBase.Available||v.EnemyBase.Id!=-20001||v.AttackMode||v.DrawerOpen||v.PlacementOpen||v.MapOpen||
                    p.AssaultInspectStage!=0||p.AdvanceNavigation!=0||p.MapNavigationStage!=0||
                    p.HomeDefenseStage!=0||v.PresentedHomeThreat)return false;
                int eligible=v.ExpandedStructureMask&v.ExpandedAssaultMask&~v.ExpandedAirMask;
                int slot=-1;
                for(int i=0;i<5;i++)
                {
                    uint group=v.ExpandedGroup(i);
                    if((eligible&(1<<i))!=0&&group!=0&&v.Squad(i).Available&&
                        !PrecisionContains(p.PrecisionAcceptedGroups,group)&&
                        !PrecisionContains(p.PrecisionExhaustedGroups,group)){slot=i;break;}
                }
                if(slot<0||p.PrecisionAcceptedGroups.Length==p.PrecisionAcceptedGroups.Capacity||
                    p.PrecisionExhaustedGroups.Length==p.PrecisionExhaustedGroups.Capacity)return false;
                p.PrecisionGroup=v.ExpandedGroup(slot);p.PrecisionSlot=slot;p.PrecisionPage=v.ExpandedPageIndex;
                p.PrecisionRoster=v.ExpandedRosterRevision;p.PrecisionStage=1;p.PrecisionAttempts=0;
                p.PrecisionReadyAt=v.Time;p.PrecisionDeadline=v.Time+18f;
            }
            bool sameGroup=v.ExpandedPageIndex==p.PrecisionPage&&v.ExpandedRosterRevision==p.PrecisionRoster&&
                v.ExpandedGroup(p.PrecisionSlot)==p.PrecisionGroup&&
                (v.ExpandedStructureMask&v.ExpandedAssaultMask&~v.ExpandedAirMask&(1<<p.PrecisionSlot))!=0;
            if(!sameGroup||!v.EnemyBase.Available||v.EnemyBase.Id!=-20001||p.AssaultIssued==0||p.StructureOrdered==0||
                v.DrawerOpen||v.MapOpen||v.PlacementOpen||v.Time>=p.PrecisionDeadline)
            {
                if(!PrecisionContains(p.PrecisionExhaustedGroups,p.PrecisionGroup)&&
                    p.PrecisionExhaustedGroups.Length<p.PrecisionExhaustedGroups.Capacity)
                    p.PrecisionExhaustedGroups.Add(p.PrecisionGroup);
                p.PrecisionStage=v.AttackMode?(byte)5:(byte)0;
                return true;
            }
            if(v.Time<p.PrecisionReadyAt)return true;
            bool selected=v.ExpandedSelectedGroups==1&&v.ExpandedSelectedOffPage==0&&
                v.ExpandedSelectedMask==(1<<p.PrecisionSlot);
            if(!selected)
            {
                if(v.ExpandedSelectedGroups>0)
                {TryDeselectSquads(v,ref output);return true;}
                if(!v.SelectionMode)
                {p.Intent=AriaSkirmishIntent.SelectSquad;Target(v.Select,false,ref output);return true;}
                p.Intent=AriaSkirmishIntent.SelectSquad;Target(v.Squad(p.PrecisionSlot),false,ref output);
                return true;
            }
            if(!v.AttackMode)
            {
                p.Intent=AriaSkirmishIntent.Attack;Target(v.Attack,false,ref output);
                return true;
            }
            if(!v.SelectionVisible)return true;
            // Exactly one target gesture per attempt; subsequent frames wait for native feedback.
            p.PrecisionBaseline=v.PrecisionAttack.Revision;p.PrecisionIssueFrame=v.Frame;
            p.PrecisionActions=touch.Actions;p.PrecisionAttempts++;p.PrecisionStage=4;
            p.PrecisionDeadline=v.Time+8f;
            p.Intent=AriaSkirmishIntent.TargetBase;Target(v.EnemyBase,true,ref output);
            return true;
        }

        private static bool TryExpandedGroundAssault(
            in AriaSkirmishObservation view,
            ref AriaSkirmishPlanComponent plan,
            ref AriaPlayObservationComponent output)
        {
            var facts = new Game.Simulation.AriaGroundColumnView {
                ExpandedPageCount=view.ExpandedPageCount,ExpandedPageIndex=view.ExpandedPageIndex,
                ExpandedTotalGroups=view.ExpandedTotalGroups,EnemyDesignatedAlive=view.EnemyDesignatedAlive,
                ExpandedCanPreviousPage=view.ExpandedCanPreviousPage,ExpandedCanNextPage=view.ExpandedCanNextPage,
                ExpandedSelectedGroups=view.ExpandedSelectedGroups,ExpandedAssaultMask=view.ExpandedAssaultMask,
                ExpandedAttackOrderMask=view.ExpandedAttackOrderMask,ExpandedSelectedMask=view.ExpandedSelectedMask,
                ExpandedStructureMask=view.ExpandedStructureMask,
                PreviousAvailable=view.PreviousSquadPage.Available,NextAvailable=view.NextSquadPage.Available,
                ToggleAvailable=view.SelectedSquadToToggle.Available,AttackAvailable=view.Attack.Available };
            for(int slot=0;slot<5;slot++)if(view.Squad(slot).Available)facts.AvailableSquadMask|=1<<slot;
            var state = new Game.Simulation.AriaGroundColumnState {
                PagesOrdered=plan.PagesOrdered,SelectionResetPage=plan.SelectionResetPage,AssaultPageSeen=plan.AssaultPageSeen,
                AssaultSelecting=plan.AssaultSelecting,StructureOrdered=plan.StructureOrdered,
                PagedToAssault=plan.PagedToAssault,AssaultIssued=plan.AssaultIssued };
            bool handled=Game.Simulation.AriaGroundColumnPolicy.StepGroundAssault(facts,ref state,out var decision);
            plan.PagesOrdered=state.PagesOrdered;plan.SelectionResetPage=state.SelectionResetPage;plan.AssaultPageSeen=state.AssaultPageSeen;
            plan.AssaultSelecting=state.AssaultSelecting;plan.StructureOrdered=state.StructureOrdered;
            plan.PagedToAssault=state.PagedToAssault;plan.AssaultIssued=state.AssaultIssued;
            switch(decision.Intent) {
                case Game.Simulation.AriaGroundIntent.Inspect:plan.Intent=AriaSkirmishIntent.Inspect;break;
                case Game.Simulation.AriaGroundIntent.SelectSquad:plan.Intent=AriaSkirmishIntent.SelectSquad;break;
                case Game.Simulation.AriaGroundIntent.Attack:plan.Intent=AriaSkirmishIntent.Attack;break;
            }
            switch(decision.Action) {
                case Game.Simulation.AriaGroundAction.Wait:output.Kind=AriaPlayObservationKind.Waiting;break;
                case Game.Simulation.AriaGroundAction.Unavailable:output.Kind=AriaPlayObservationKind.Unavailable;break;
                case Game.Simulation.AriaGroundAction.PreviousPage:Target(view.PreviousSquadPage,false,ref output);break;
                case Game.Simulation.AriaGroundAction.NextPage:Target(view.NextSquadPage,false,ref output);break;
                case Game.Simulation.AriaGroundAction.ToggleSelected:Target(view.SelectedSquadToToggle,false,ref output);break;
                case Game.Simulation.AriaGroundAction.SelectSlot:Target(view.Squad(decision.Slot),false,ref output);break;
                case Game.Simulation.AriaGroundAction.ArmAttack:Target(view.Attack,false,ref output);break;
            }
            return handled;
        }





        private static bool TryDeselectSquads(in AriaSkirmishObservation view,
            ref AriaPlayObservationComponent output)
        {
            if (view.ExpandedSelectedGroups <= 0) return false;
            if (view.SelectedSquadToToggle.Available)
                Target(view.SelectedSquadToToggle, false, ref output);
            else output.Kind = AriaPlayObservationKind.Waiting;
            return true;
        }

        private static int LowestSetBit(int mask)
        {
            int bit = 0;
            int value = mask;
            while ((value & 1) == 0)
            {
                value >>= 1;
                bit++;
            }

            return bit;
        }

        private static void Target(AriaTouchTarget target, bool world, ref AriaPlayObservationComponent output)
        {
            if (!target.Available) { output.Kind = AriaPlayObservationKind.Unavailable; return; }
            output.Kind = world ? AriaPlayObservationKind.WorldTarget : AriaPlayObservationKind.Control;
            output.TargetId = target.Id; output.Position = target.Position;
            output.Drag = target.Drag ? (byte)1 : (byte)0;
            output.DragEnd = target.DragEnd;
            output.ScrollTargetId = target.ScrollTargetId;
            output.ScrollDistanceViewports = target.ScrollDistanceViewports;
        }
    }
}
