using System;
namespace Game.Simulation {
// Shared policy body. Runtime/source admission remains an adapter responsibility.
public static class AriaGroundBaseAssaultPolicy {
public static void Step(in GroundMissionContext context,in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output){
 context.RequireCompatibleObservation(view);
 StepCore(view,ref plan,ref touch,out output);
}
internal static void StepLegacyS002Observation(in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output)=>StepCore(view,ref plan,ref touch,out output);
private static void StepCore(in AriaS002Observation view,ref AriaS002PlanState plan,ref AriaS002ContactState touch,out AriaS002Decision output){
 output=new AriaS002Decision {Kind=view.Finished?AriaS002DecisionKind.Finished:AriaS002DecisionKind.Waiting,Time=view.Time,Frame=view.Frame,GoalId=10000+plan.Cycle*10+plan.Slot};
 if(!view.Finished&&touch.Phase is AriaS002ContactPhase.Blocked or AriaS002ContactPhase.Starting){output.Intent=plan.Intent;return;}
 if(view.ExpandedRosterRevision!=0&&touch.Phase!=AriaS002ContactPhase.Touching){
  if(plan.SweepRosterRevision!=0&&plan.SweepRosterRevision!=view.ExpandedRosterRevision){plan.PagedToAssault=0;plan.AssaultSelecting=0;plan.AssaultIssued=0;plan.StructureOrdered=0;plan.AssaultPageSeen=0;plan.PagesOrdered=0;}
  plan.SweepRosterRevision=view.ExpandedRosterRevision;
 }
 StepExpandedBaseAssault(view,ref plan,ref touch,ref output);output.Intent=plan.Intent;
}

        private static void StepExpandedBaseAssault(in AriaS002Observation view,
            ref AriaS002PlanState plan, ref AriaS002ContactState touch,
            ref AriaS002Decision output)
        {
            // Public visible controls only. No gameplay Entity mutation from this planner.
            if (view.Finished || touch.Phase == AriaS002ContactPhase.Manual ||
                (plan.AssaultInspectStage == 0 &&
                 (plan.AssaultIssued == 0 || plan.StructureOrdered == 0) &&
                 !AcceptedAssaultInspectionPage(view, plan)))
                ClearAssaultMapInspection(ref plan);
            if (view.Finished)
            {
                plan.Intent = AriaS002Intent.Handback;
                output.Kind = AriaS002DecisionKind.Finished;
                return;
            }
            if (touch.Phase == AriaS002ContactPhase.Touching) return;
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
            if (touch.Phase != AriaS002ContactPhase.Manual &&
                (plan.AssaultIssued != 0 || acceptedAssaultOnPage) && CurrentAssaultDistance(view, plan) &&
                view.NearestFriendlyObjectiveDistance >= 0f &&
                !float.IsNaN(view.NearestFriendlyObjectiveDistance) &&
                !float.IsInfinity(view.NearestFriendlyObjectiveDistance))
            {
                if (plan.AssaultObjectiveHudIdentity != view.PublicSnapshot.ObjectiveHudIdentity ||
                    AriaGroundApproachPolicy.SquaredDistance(plan.AssaultObjectiveWorld,view.PublicSnapshot.ObjectiveWorld) > .0001f)
                {
                    plan.AssaultDistanceObserved = 0;
                    plan.AssaultObjectiveHudIdentity = view.PublicSnapshot.ObjectiveHudIdentity;
                    plan.AssaultObjectiveWorld = view.PublicSnapshot.ObjectiveWorld;
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
            if(blocked){touch.Phase=AriaS002ContactPhase.Blocked;touch.GestureRequested=0;return;}
            if (plan.PrecisionStage != 0)
            {
                // Actual home defence may interrupt; accepted group history is retained.
                if ((view.PublicHomeThreat || plan.HomeDefenseStage != 0) &&
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
                plan.Intent = AriaS002Intent.Inspect;
                Target(view.PlacementCancel, false, ref output);
                return;
            }
            // Preserve a viable base-assault force, then establish physical
            // Oil and Fuel delivery before research or repeated aircraft sorties.
            // These are visible paid catalog controls, with queue/capacity eligibility.
            var preparationView=new Game.Simulation.AriaPreparationView {
                AirProfile=view.AirProfile,CombinedArmsField=view.CombinedArmsField,Infantry=view.Infantry,Time=view.Time,OwnStructureAttackUnits=view.OwnStructureAttackUnits,
                LogisticsTruckCommitted=view.LogisticsTruckCommitted,CanAffordLogisticsTruck=view.CanAffordLogisticsTruck,LogisticsTargetAvailable=view.RecruitLogisticsTruck.Admitted,
                TankersCommitted=view.TankersCommitted,CanAffordTanker=view.CanAffordTanker,TankerTargetAvailable=view.RecruitTanker.Admitted };
            var preparationState=new Game.Simulation.AriaPreparationState{AssaultStarted=plan.AssaultStarted,OpeningUntil=plan.OpeningUntil};
            var preparationAction=Game.Simulation.AriaExpandedPreparationPolicy.Supply(preparationView,ref preparationState);
            plan.AssaultStarted=preparationState.AssaultStarted;
            if(preparationAction==Game.Simulation.AriaPreparationAction.RecruitLogisticsTruck){plan.Intent=AriaS002Intent.Recruit;Target(view.RecruitLogisticsTruck,false,ref output);return;}
            if(preparationAction==Game.Simulation.AriaPreparationAction.RecruitTanker){plan.Intent=AriaS002Intent.Recruit;Target(view.RecruitTanker,false,ref output);return;}
            if (StepAircraftCycle(view, ref plan, ref touch, ref output)) return;
            if (StepAssaultObjectiveFocus(view, ref plan, ref touch, ref output)) return;
            if (StepPrecisionAttack(view, ref plan, ref touch, ref output)) return;
            if (view.AttackMode && view.SelectionVisible)
            {
                var objectiveContact=Game.Simulation.AriaGroundObjectivePolicy.Contact(view.EnemyBase.Admitted,view.AdvanceGround.Admitted);
                if (objectiveContact==Game.Simulation.AriaObjectiveContact.VisibleEnemyTarget)
                {
                    plan.Intent = AriaS002Intent.TargetBase;
                    Target(view.EnemyBase, true, ref output);
                }
                else if (objectiveContact==Game.Simulation.AriaObjectiveContact.PublicTerrainApproach)
                {
                    // The labelled objective is public; a hidden base is not a precision target.
                    // Offer the already presented terrain approach for a separate normal Attack Move touch.
                    // Camera focus alone cannot reveal a contact and must never issue this order.
                    plan.Intent = AriaS002Intent.Advance;
                    Target(view.AdvanceGround, true, ref output);
                }
                else
                {
                    plan.Intent = AriaS002Intent.FindBase;
                    if (view.FocusAdvance.Admitted &&
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
                if (view.Hold.Admitted && plan.Intent == AriaS002Intent.Attack)
                {
                    plan.Intent = AriaS002Intent.Hold;
                    Target(view.Hold, false, ref output);
                    return;
                }
                plan.Intent = AriaS002Intent.Handback;
                output.Kind = AriaS002DecisionKind.Waiting;
                return;
            }
            if (view.VisibleHostileAir > 0 && !view.AntiAirCommitted && view.CanAffordAntiAir && view.RecruitAntiAir.Admitted)
            {
                plan.Intent = AriaS002Intent.Recruit;
                Target(view.RecruitAntiAir, false, ref output);
                return;
            }
            // Combined Arms needs a viable structure force before spending
            // its remaining materials on rifles that cannot hurt the base.
            if (view.CombinedArmsField && view.OwnStructureAttackUnits < 8 && view.Infantry < 24)
            {
                if (view.RocketeerRecruitPending)
                {
                    plan.Intent = AriaS002Intent.ObserveBattle;
                    if (view.CatalogOpen) Target(view.CloseDrawer, false, ref output);
                    return;
                }
                if (view.CanAffordRocketeer && view.RecruitRocketeer.Admitted)
                {
                    plan.Intent = AriaS002Intent.Recruit;
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
                        plan.Intent = AriaS002Intent.ObserveBattle;
                        if (view.CatalogOpen) Target(view.CloseDrawer, false, ref output);
                        else output.Kind = AriaS002DecisionKind.Waiting;
                        return;
                    }
                    if (view.CanAffordRifle)
                    {
                        plan.Intent = AriaS002Intent.Recruit;
                        // Drawer/category transitions can briefly hide their next
                        // control. Keep the bounded opening instead of treating
                        // one unavailable frame as an unaffordable reinforcement.
                        if (view.Recruit.Admitted) Target(view.Recruit, false, ref output);
                        else output.Kind = AriaS002DecisionKind.Waiting;
                        return;
                    }
                }
                plan.AssaultStarted = 1;
            }
            if (!view.ReadinessEligible && view.CanUpgradeReadiness && view.UpgradeReadiness.Admitted &&
                plan.AssaultIssued != 0)
            {
                plan.Intent = AriaS002Intent.Inspect;
                Target(view.UpgradeReadiness, false, ref output);
                return;
            }
            if (view.ReadinessEligible && !view.PadPresent && view.CanBuildAirPad && view.AirPad.Admitted)
            {
                plan.Intent = AriaS002Intent.Inspect;
                plan.PadStage = 1;
                plan.PadDeadline = view.Time + 45f;
                StepHomePadPlacement(view, ref plan, ref touch, ref output);
                return;
            }
            if (view.AirQueueOffered && !view.AirRecruitPending && view.OwnAttackAirLive == 0 && view.RecruitAir.Admitted)
            {
                plan.Intent = AriaS002Intent.Recruit;
                Target(view.RecruitAir, false, ref output);
                return;
            }
            if (view.CatalogOpen)
            {
                if (!view.AirProfile && !view.RifleRecruitPending && plan.Intent == AriaS002Intent.Recruit && view.Infantry < 16 &&
                    view.CanAffordRifle && view.Recruit.Admitted &&
                    view.VisibleHostileAir == 0 && !view.AirQueueOffered)
                {
                    Target(view.Recruit, false, ref output);
                    return;
                }
                // Once a purchase/research action is accepted, return to the
                // visible army controls before interpreting their page state.
                plan.Intent = AriaS002Intent.Inspect;
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
                if (view.NextSquadPage.Admitted &&
                    (plan.MapNavigationStage == 1 || view.Time >= plan.ObserveUntil))
                {
                    if (plan.MapNavigationStage == 0)
                    { plan.MapNavigationStage = 1; plan.AssaultPageSeen = view.ExpandedPageIndex; }
                    plan.Intent = AriaS002Intent.Inspect;
                    Target(view.NextSquadPage, false, ref output);
                    return;
                }
                plan.Intent = AriaS002Intent.ObserveBattle;
                output.Kind = AriaS002DecisionKind.Waiting;
                return;
            }
            // Rifles cannot hurt a Barracks. Do not spend the attack button on them
            // while a later page still holds the tank and rocketeers.
            if (view.EnemyDesignatedAlive && plan.AssaultIssued == 0 &&
                view.ExpandedAssaultMask == 0 && view.NextSquadPage.Admitted)
            {
                plan.Intent = AriaS002Intent.Inspect;
                output.Kind = AriaS002DecisionKind.Waiting;
                return;
            }
            if (Game.Simulation.AriaExpandedPreparationPolicy.FallbackGroundRecruit(new Game.Simulation.AriaPreparationView {
                AirProfile=view.AirProfile,RifleRecruitPending=view.RifleRecruitPending,Infantry=view.Infantry,CanAffordRifle=view.CanAffordRifle,RifleTargetAvailable=view.Recruit.Admitted }) == Game.Simulation.AriaPreparationAction.RecruitRifle)
            {
                plan.Intent = AriaS002Intent.Recruit;
                Target(view.Recruit, false, ref output);
                return;
            }
            if (!view.SelectionVisible && view.Squad0.Admitted &&
                SelectionCanDamageDesignatedBase(view, squad0WithoutSelection: true))
            {
                plan.Intent = AriaS002Intent.SelectSquad;
                Target(view.Squad0, false, ref output);
                return;
            }
            // Rifles and cars cannot damage a Barracks. Attack only when the
            // visible selection includes a structure card (tank, rocketeer,
            // breacher, siege), or when this observation has no page model.
            if (view.EnemyDesignatedAlive && view.Attack.Admitted && view.SelectionVisible &&
                SelectionCanDamageDesignatedBase(view, squad0WithoutSelection: false))
            {
                plan.Intent = AriaS002Intent.Attack;
                Target(view.Attack, false, ref output);
                return;
            }
            // A visible Hold button is not an actionable order without a living
            // selection. Let an already-issued assault continue toward the base
            // instead of spending three touch retries on an unselected command.
            if (view.Hold.Admitted && view.SelectionVisible && view.ExpandedSelectedGroups > 0)
            {
                plan.Intent = AriaS002Intent.Hold;
                Target(view.Hold, false, ref output);
                return;
            }
            plan.Intent = AriaS002Intent.ObserveBattle;
            output.Kind = AriaS002DecisionKind.Waiting;
        }

        private static bool StepPresentedHomeDefense(in AriaS002Observation view,
            ref AriaS002PlanState plan, ref AriaS002ContactState touch,
            ref AriaS002Decision output)
        {
            var facts=new Game.Simulation.AriaHomeDefenseView {
                PresentedHomeThreat=view.PublicHomeThreat,PlacementOpen=view.PlacementOpen,DrawerOpen=view.CatalogOpen,MapOpen=view.InspectionOpen,AttackMode=view.AttackMode,
                CancelAvailable=view.PlacementCancel.Admitted,CloseDrawerAvailable=view.CloseDrawer.Admitted,CloseMapAvailable=view.CloseMap.Admitted,
                FocusAvailable=view.FocusPlayer.Admitted,PreviousAvailable=view.PreviousSquadPage.Admitted,NextAvailable=view.NextSquadPage.Admitted,
                ToggleAvailable=view.SelectedSquadToToggle.Admitted,AttackAvailable=view.Attack.Admitted,
                ThreatGroundAvailable=view.ThreatGround.Admitted,ThreatVisibleAvailable=view.Threat.Admitted,
                ExpandedPageIndex=view.ExpandedPageIndex,ExpandedSelectedGroups=view.ExpandedSelectedGroups,ExpandedAssaultMask=view.ExpandedAssaultMask,ExpandedSelectedMask=view.ExpandedSelectedMask,
                CompletedContacts=touch.Actions,Time=view.Time };
            for(int slot=0;slot<5;slot++)if(view.Squad(slot).Admitted)facts.AvailableSquadMask|=1<<slot;
            var state=new Game.Simulation.AriaHomeDefenseState {HomeDefenseStage=plan.HomeDefenseStage,HomeDefenseAction=plan.HomeDefenseAction,HomeDefensePage=plan.HomeDefensePage,DefenseStage=plan.DefenseStage,HomeDefenseObserveUntil=plan.HomeDefenseObserveUntil,
                Column=new Game.Simulation.AriaGroundColumnState {AssaultIssued=plan.AssaultIssued,PagedToAssault=plan.PagedToAssault,AssaultSelecting=plan.AssaultSelecting,StructureOrdered=plan.StructureOrdered,PagesOrdered=plan.PagesOrdered,AssaultPageSeen=plan.AssaultPageSeen,SelectionResetPage=plan.SelectionResetPage}};
            bool handled=Game.Simulation.AriaHomeDefensePolicy.Step(facts,ref state,out var decision);
            plan.HomeDefenseStage=state.HomeDefenseStage;plan.HomeDefenseAction=state.HomeDefenseAction;plan.HomeDefensePage=state.HomeDefensePage;plan.DefenseStage=state.DefenseStage;plan.HomeDefenseObserveUntil=state.HomeDefenseObserveUntil;
            plan.AssaultIssued=state.Column.AssaultIssued;plan.PagedToAssault=state.Column.PagedToAssault;plan.AssaultSelecting=state.Column.AssaultSelecting;plan.StructureOrdered=state.Column.StructureOrdered;plan.PagesOrdered=state.Column.PagesOrdered;plan.AssaultPageSeen=state.Column.AssaultPageSeen;plan.SelectionResetPage=state.Column.SelectionResetPage;
            if(decision.Defend)plan.Intent=AriaS002Intent.DefendBase;
            switch(decision.Action){
                case Game.Simulation.AriaHomeDefenseAction.Waiting:output.Kind=AriaS002DecisionKind.Waiting;break;
                case Game.Simulation.AriaHomeDefenseAction.Unavailable:output.Kind=AriaS002DecisionKind.Unavailable;break;
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

        private static bool StepExpandedAdvanceNavigation(in AriaS002Observation view,
            ref AriaS002PlanState plan, ref AriaS002ContactState touch,
            ref AriaS002Decision output)
        {
            if (plan.AdvanceNavigation == 0)
                return view.Time < plan.ObserveUntil && view.Time < plan.MapNavigationReadyAt;
            plan.Intent = AriaS002Intent.FindBase;
            if (!view.InspectionOpen)
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
                if (!view.FocusAdvance.Admitted && view.Time < plan.MapOpenedAt + 5) return true;
                if (view.FocusAdvance.Admitted)
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

        static void ClearAssaultMapInspection(ref AriaS002PlanState plan)
        {
            plan.AssaultInspectStage = 0; plan.AssaultInspectActions = 0; plan.AssaultInspectOpenTarget = 0;
            plan.AssaultInspectMapIdentity = 0; plan.AssaultInspectOpenedFrame = 0;
            plan.AssaultInspectRevision = 0; plan.AssaultInspectDeadline = 0; plan.AssaultInspectReadyAt = 0;
            plan.AssaultInspectObjectiveHudIdentity = 0; plan.AssaultInspectObjectiveWorld = default;
        }
        static bool AcceptedAssaultInspectionPage(in AriaS002Observation view,
            in AriaS002PlanState plan)
            => plan.AssaultIssued == 0 && plan.PagesOrdered > 0 &&
               plan.AssaultPageSeen == view.ExpandedPageIndex + 1 &&
               plan.SweepRosterRevision == view.ExpandedRosterRevision &&
               view.ExpandedAssaultMask != 0 &&
               (view.ExpandedAssaultMask & ~view.ExpandedAttackOrderMask) == 0;

        static bool AssaultInspectionUiBusy(in AriaS002Observation view, in AriaS002PlanState plan)
            => view.CatalogOpen || view.PlacementOpen || view.AttackMode ||
               (plan.AssaultSelecting != 0 && !AcceptedAssaultInspectionPage(view, plan)) ||
               plan.TargetPending != 0 || plan.AdvanceNavigation != 0 || plan.MapNavigationStage != 0 ||
               (plan.PadStage != 0 && plan.PadStage != 8) ||
               (view.AirProfile && plan.AirCycleCount == 0 && plan.AirCycleStage >= 1 && plan.AirCycleStage <= 4 &&
                view.OwnAttackAirLive > 0 && view.PadPresent && view.Time < plan.AirCycleDeadline);

        static bool CurrentAssaultDistance(in AriaS002Observation view, in AriaS002PlanState plan)
        {
            if (!view.AssaultDistanceKnown || view.PublicSnapshot.InspectionScopeOpen != view.InspectionOpen ||
                !view.PublicSnapshot.IsCurrent(view.Frame, view.Time)) return false;
            if (plan.AssaultInspectStage == 0 || !view.InspectionOpen) return true;
            return plan.AssaultInspectStage == 2 && view.Time <= plan.AssaultInspectDeadline &&
                view.PublicSnapshot.InspectionScopeOpen && view.PublicSnapshot.Identity == plan.AssaultInspectMapIdentity &&
                view.PublicSnapshot.MarkerRevision != plan.AssaultInspectRevision &&
                view.PublicSnapshot.MarkerFrame > plan.AssaultInspectOpenedFrame &&
                view.PublicSnapshot.ObjectiveHudIdentity == plan.AssaultInspectObjectiveHudIdentity &&
                AriaGroundApproachPolicy.SquaredDistance(view.PublicSnapshot.ObjectiveWorld,plan.AssaultInspectObjectiveWorld) <= .0001f;
        }
        static bool StepAssaultMapInspection(in AriaS002Observation view,
            ref AriaS002PlanState plan, ref AriaS002ContactState touch,
            ref AriaS002Decision output)
        {
            if (plan.AssaultInspectStage == 0)
            {
                if (((plan.AssaultIssued == 0 || plan.StructureOrdered == 0) &&
                     !AcceptedAssaultInspectionPage(view, plan)) ||
                    (touch.Phase != AriaS002ContactPhase.Observing && touch.Phase != AriaS002ContactPhase.Waiting &&
                     !(touch.Phase == AriaS002ContactPhase.Verifying && touch.GestureRequested == 0 &&
                       touch.Pressed == 0 && touch.Actions > 0 &&
                       touch.LastCompletedTargetId == touch.TargetId && touch.LastCompletedFrame > 0 &&
                       view.Frame > touch.LastCompletedFrame && view.Time >= touch.DueAt)) ||
                    view.AssaultDistanceKnown || view.InspectionOpen || AssaultInspectionUiBusy(view,plan) ||
                    view.PublicHomeThreat ||
                    view.Time < plan.AssaultInspectReadyAt || !view.PublicSnapshot.OpenMap.Admitted) return false;
                plan.AssaultInspectStage = 1;
                plan.AssaultInspectActions = touch.Actions;
                plan.AssaultInspectOpenTarget = view.PublicSnapshot.OpenMap.Id;
                plan.AssaultInspectDeadline = view.Time + 8f;
                plan.AssaultInspectObjectiveHudIdentity = view.PublicSnapshot.ObjectiveHudIdentity;
                plan.AssaultInspectObjectiveWorld = view.PublicSnapshot.ObjectiveWorld;
            }
            if (AssaultInspectionUiBusy(view,plan))
            {
                if (!view.InspectionOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return false;
                }
                if (plan.AssaultInspectStage != 3 && plan.AssaultInspectStage != 4)
                { plan.AssaultInspectStage = 3; plan.AssaultInspectDeadline = view.Time + 3f; }
            }
            plan.Intent = AriaS002Intent.Inspect;
            if (plan.AssaultInspectStage == 1)
            {
                if (view.InspectionOpen)
                {
                    // A different control/user opening is not this requested operation.
                    if (touch.Actions <= plan.AssaultInspectActions ||
                        touch.TargetId != plan.AssaultInspectOpenTarget ||
                        !view.PublicSnapshot.OwnedIdentity || !view.PublicSnapshot.InspectionScopeOpen ||
                        view.PublicSnapshot.Identity == 0)
                    { plan.AssaultInspectStage = 4; return true; }
                    plan.AssaultInspectMapIdentity = view.PublicSnapshot.Identity;
                    plan.AssaultInspectOpenedFrame = view.Frame;
                    plan.AssaultInspectRevision = view.PublicSnapshot.MarkerRevision;
                    plan.AssaultInspectStage = view.Time <= plan.AssaultInspectDeadline &&
                        view.PublicSnapshot.ObjectiveHudIdentity == plan.AssaultInspectObjectiveHudIdentity &&
                        AriaGroundApproachPolicy.SquaredDistance(view.PublicSnapshot.ObjectiveWorld,plan.AssaultInspectObjectiveWorld) <= .0001f ? (byte)2 : (byte)3;
                    plan.AssaultInspectDeadline = view.Time + 3f;
                    return true; // Require a subsequent completed rendered marker update.
                }
                if (view.Time > plan.AssaultInspectDeadline || view.PublicHomeThreat)
                { plan.AssaultInspectStage = 4; return true; }
                if (view.PublicSnapshot.OpenMap.Admitted &&
                    view.PublicSnapshot.OpenMap.Id == plan.AssaultInspectOpenTarget)
                    Target(view.PublicSnapshot.OpenMap, false, ref output);
                return true;
            }
            if (plan.AssaultInspectStage == 2)
            {
                if (!view.InspectionOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return true;
                }
                if (view.PublicHomeThreat || view.Time > plan.AssaultInspectDeadline ||
                    view.PublicSnapshot.ObjectiveHudIdentity != plan.AssaultInspectObjectiveHudIdentity ||
                    AriaGroundApproachPolicy.SquaredDistance(view.PublicSnapshot.ObjectiveWorld,plan.AssaultInspectObjectiveWorld) > .0001f ||
                    CurrentAssaultDistance(view, plan))
                { plan.AssaultInspectStage = 3; plan.AssaultInspectDeadline = view.Time + 3f; }
                else return true;
            }
            if (plan.AssaultInspectStage == 3)
            {
                if (!view.InspectionOpen)
                {
                    ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                    return true;
                }
                // Keep a reachable owned close target stable through normal hand aiming.
                // The existing input owner bounds attempts; the battle watchdog is unchanged.
                if (view.PublicSnapshot.OwnedIdentity && view.PublicSnapshot.InspectionScopeOpen &&
                    view.PublicSnapshot.Identity == plan.AssaultInspectMapIdentity &&
                    view.CloseMap.Admitted) Target(view.CloseMap, false, ref output);
                else if (view.Time > plan.AssaultInspectDeadline) plan.AssaultInspectStage = 4;
                return true;
            }
            // Closed/failed open cannot starve ordinary production/navigation.
            // An unclosable/foreign open popup still refuses all inspection actions.
            if (!view.InspectionOpen)
            {
                ClearAssaultMapInspection(ref plan); plan.AssaultInspectReadyAt = view.Time + 12f;
                return false;
            }
            // A previously timed-out presentation may become closable again. Reuse only
            // the same owned popup, without resetting attempts, deadline or progress.
            if (view.PublicSnapshot.OwnedIdentity && view.PublicSnapshot.InspectionScopeOpen &&
                view.PublicSnapshot.Identity == plan.AssaultInspectMapIdentity &&
                plan.AssaultInspectMapIdentity != 0 && view.CloseMap.Admitted)
            {
                plan.AssaultInspectStage = 3;
                Target(view.CloseMap, false, ref output);
                return true;
            }
            // Never dismiss a foreign/unreachable popup or wait here indefinitely.
            touch.Phase = AriaS002ContactPhase.Blocked;
            touch.GestureRequested = 0;
            return true;
        }

        private static bool SelectionCanDamageDesignatedBase(
            in AriaS002Observation view,
            bool squad0WithoutSelection)
        {
            bool pageModel = view.ExpandedTotalGroups > 0 || view.ExpandedStructureMask != 0 || view.ExpandedAssaultMask != 0;
            if (!pageModel)
                return true;
            if (squad0WithoutSelection)
                return (view.ExpandedStructureMask & 1) != 0;
            return (view.ExpandedStructureMask & view.ExpandedSelectedMask) != 0;
        }

        private static bool PrecisionContains(in AriaS002GroupHistory ids,uint group)
        {
            for(int i=0;i<ids.Length;i++)if(ids[i]==group)return true;
            return false;
        }
        private static bool StepAssaultObjectiveFocus(in AriaS002Observation v,
            ref AriaS002PlanState p, ref AriaS002ContactState t,
            ref AriaS002Decision o)
        {
            if(p.AssaultFocusStage==2)return false; // One bounded attempt per plan/session, including roster churn.
            if(!v.ExpandedBaseAssault||v.Finished||t.Phase is AriaS002ContactPhase.Manual or AriaS002ContactPhase.Blocked or AriaS002ContactPhase.Starting)
                return false;
            if(p.AssaultFocusStage==0)
            {
                if(p.AssaultIssued==0||p.StructureOrdered==0||!v.EnemyDesignatedAlive||!v.AssaultAtBase||
                    !CurrentAssaultDistance(v,p)||v.EnemyBase.Admitted||
                    (t.Phase!=AriaS002ContactPhase.Observing&&t.Phase!=AriaS002ContactPhase.Waiting)||
                    v.CatalogOpen||v.InspectionOpen||v.PlacementOpen||v.AttackMode||v.PublicHomeThreat||
                    p.HomeDefenseStage!=0||p.PrecisionStage!=0||p.AssaultInspectStage!=0||
                    AssaultInspectionUiBusy(v,p)||!v.FocusEnemy.Admitted||v.FocusEnemy.Id==0||
                    v.FocusEnemy.Id==-20001||v.FocusEnemy.Drag)return false;
                p.AssaultFocusStage=1;p.AssaultFocusActions=t.Actions;
                p.AssaultFocusTarget=v.FocusEnemy.Id;p.AssaultFocusFrame=v.Frame;
                p.AssaultFocusDeadline=v.Time+8f;
            }
            if(t.Actions>p.AssaultFocusActions&&t.LastCompletedTargetId==p.AssaultFocusTarget&&
                t.LastCompletedFrame>=p.AssaultFocusFrame)
            {p.AssaultFocusStage=2;return true;}
            if(v.Time>=p.AssaultFocusDeadline||p.AssaultIssued==0||p.StructureOrdered==0||
                v.CatalogOpen||v.InspectionOpen||v.PlacementOpen||v.AttackMode||v.PublicHomeThreat||
                p.HomeDefenseStage!=0||!v.FocusEnemy.Admitted||v.FocusEnemy.Id!=p.AssaultFocusTarget)
            {p.AssaultFocusStage=2;return false;}
            p.Intent=AriaS002Intent.FindBase;
            Target(v.FocusEnemy,false,ref o);
            return true; // No selection, Attack toggle, terrain/target order, accepted ack or progress-clock write.
        }

        private static bool StepPrecisionAttack(in AriaS002Observation v,ref AriaS002PlanState p,
            ref AriaS002ContactState touch,ref AriaS002Decision output)
        {
            if(v.Finished||touch.Phase==AriaS002ContactPhase.Manual)
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
                    ack.ResultKnown&&ack.Revision>p.PrecisionBaseline&&ack.GroupId==p.PrecisionGroup&&
                    ack.PublicTargetId==-20001&&ack.RequestId>0&&ack.Frame>=p.PrecisionIssueFrame&&
                    ack.AcceptedMembers>0&&ack.AcceptedMembers<=ack.SourceMembers&&
                    touch.Actions>p.PrecisionActions&&touch.LastCompletedTargetId==-20001&&
                    touch.LastCompletedFrame>=p.PrecisionIssueFrame&&
                    ack.Scope==v.Scope&&ack.ScopeReleaseVerified;
                if(exact)
                {
                    if(!PrecisionContains(p.PrecisionAcceptedGroups,p.PrecisionGroup)&&
                        p.PrecisionAcceptedGroups.Length<p.PrecisionAcceptedGroups.Capacity)
                        p.PrecisionAcceptedGroups.Add(p.PrecisionGroup);
                    p.PrecisionStage=0;p.PrecisionAttempts=0;
                    p.Intent=AriaS002Intent.ObserveBattle;
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
                        v.EnemyBase.Admitted&&v.EnemyBase.Id==-20001&&!v.EnemyBase.Drag&&
                        p.AssaultIssued!=0&&p.StructureOrdered!=0&&!v.CatalogOpen&&!v.InspectionOpen&&!v.PlacementOpen&&
                        !v.PublicHomeThreat&&p.HomeDefenseStage==0;
                    if(current&&touch.Actions==p.PrecisionActions)
                        Target(v.EnemyBase,true,ref output);
                    return true;
                }
                if(p.PrecisionAttempts>=2||!v.EnemyBase.Admitted||v.AttackMode)
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
                    !v.EnemyBase.Admitted||v.EnemyBase.Id!=-20001||v.AttackMode||v.CatalogOpen||v.PlacementOpen||v.InspectionOpen||
                    p.AssaultInspectStage!=0||p.AdvanceNavigation!=0||p.MapNavigationStage!=0||
                    p.HomeDefenseStage!=0||v.PublicHomeThreat)return false;
                int eligible=v.ExpandedStructureMask&v.ExpandedAssaultMask&~v.ExpandedAirMask;
                int slot=-1;
                for(int i=0;i<5;i++)
                {
                    uint group=v.ExpandedGroup(i);
                    if((eligible&(1<<i))!=0&&group!=0&&v.Squad(i).Admitted&&
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
            if(!sameGroup||!v.EnemyBase.Admitted||v.EnemyBase.Id!=-20001||p.AssaultIssued==0||p.StructureOrdered==0||
                v.CatalogOpen||v.InspectionOpen||v.PlacementOpen||v.Time>=p.PrecisionDeadline)
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
                {p.Intent=AriaS002Intent.SelectSquad;Target(v.Select,false,ref output);return true;}
                p.Intent=AriaS002Intent.SelectSquad;Target(v.Squad(p.PrecisionSlot),false,ref output);
                return true;
            }
            if(!v.AttackMode)
            {
                p.Intent=AriaS002Intent.Attack;Target(v.Attack,false,ref output);
                return true;
            }
            if(!v.SelectionVisible)return true;
            // Exactly one target gesture per attempt; subsequent frames wait for native feedback.
            p.PrecisionBaseline=v.PrecisionAttack.Revision;p.PrecisionIssueFrame=v.Frame;
            p.PrecisionActions=touch.Actions;p.PrecisionAttempts++;p.PrecisionStage=4;
            p.PrecisionDeadline=v.Time+8f;
            p.Intent=AriaS002Intent.TargetBase;Target(v.EnemyBase,true,ref output);
            return true;
        }

        private static bool TryExpandedGroundAssault(
            in AriaS002Observation view,
            ref AriaS002PlanState plan,
            ref AriaS002Decision output)
        {
            var facts = new Game.Simulation.AriaGroundColumnView {
                ExpandedPageCount=view.ExpandedPageCount,ExpandedPageIndex=view.ExpandedPageIndex,
                ExpandedTotalGroups=view.ExpandedTotalGroups,EnemyDesignatedAlive=view.EnemyDesignatedAlive,
                ExpandedCanPreviousPage=view.ExpandedCanPreviousPage,ExpandedCanNextPage=view.ExpandedCanNextPage,
                ExpandedSelectedGroups=view.ExpandedSelectedGroups,ExpandedAssaultMask=view.ExpandedAssaultMask,
                ExpandedAttackOrderMask=view.ExpandedAttackOrderMask,ExpandedSelectedMask=view.ExpandedSelectedMask,
                ExpandedStructureMask=view.ExpandedStructureMask,
                PreviousAvailable=view.PreviousSquadPage.Admitted,NextAvailable=view.NextSquadPage.Admitted,
                ToggleAvailable=view.SelectedSquadToToggle.Admitted,AttackAvailable=view.Attack.Admitted };
            for(int slot=0;slot<5;slot++)if(view.Squad(slot).Admitted)facts.AvailableSquadMask|=1<<slot;
            var state = new Game.Simulation.AriaGroundColumnState {
                PagesOrdered=plan.PagesOrdered,SelectionResetPage=plan.SelectionResetPage,AssaultPageSeen=plan.AssaultPageSeen,
                AssaultSelecting=plan.AssaultSelecting,StructureOrdered=plan.StructureOrdered,
                PagedToAssault=plan.PagedToAssault,AssaultIssued=plan.AssaultIssued };
            bool handled=Game.Simulation.AriaGroundColumnPolicy.StepGroundAssault(facts,ref state,out var decision);
            plan.PagesOrdered=state.PagesOrdered;plan.SelectionResetPage=state.SelectionResetPage;plan.AssaultPageSeen=state.AssaultPageSeen;
            plan.AssaultSelecting=state.AssaultSelecting;plan.StructureOrdered=state.StructureOrdered;
            plan.PagedToAssault=state.PagedToAssault;plan.AssaultIssued=state.AssaultIssued;
            switch(decision.Intent) {
                case Game.Simulation.AriaGroundIntent.Inspect:plan.Intent=AriaS002Intent.Inspect;break;
                case Game.Simulation.AriaGroundIntent.SelectSquad:plan.Intent=AriaS002Intent.SelectSquad;break;
                case Game.Simulation.AriaGroundIntent.Attack:plan.Intent=AriaS002Intent.Attack;break;
            }
            switch(decision.Action) {
                case Game.Simulation.AriaGroundAction.Wait:output.Kind=AriaS002DecisionKind.Waiting;break;
                case Game.Simulation.AriaGroundAction.Unavailable:output.Kind=AriaS002DecisionKind.Unavailable;break;
                case Game.Simulation.AriaGroundAction.PreviousPage:Target(view.PreviousSquadPage,false,ref output);break;
                case Game.Simulation.AriaGroundAction.NextPage:Target(view.NextSquadPage,false,ref output);break;
                case Game.Simulation.AriaGroundAction.ToggleSelected:Target(view.SelectedSquadToToggle,false,ref output);break;
                case Game.Simulation.AriaGroundAction.SelectSlot:Target(view.Squad(decision.Slot),false,ref output);break;
                case Game.Simulation.AriaGroundAction.ArmAttack:Target(view.Attack,false,ref output);break;
            }
            return handled;
        }





        private static bool TryDeselectSquads(in AriaS002Observation view,
            ref AriaS002Decision output)
        {
            if (view.ExpandedSelectedGroups <= 0) return false;
            if (view.SelectedSquadToToggle.Admitted)
                Target(view.SelectedSquadToToggle, false, ref output);
            else output.Kind = AriaS002DecisionKind.Waiting;
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

        private static void Target(AriaS002ActionTarget target, bool world, ref AriaS002Decision output)
        {
            if (!target.Admitted) { output.Kind = AriaS002DecisionKind.Unavailable; return; }
            output.Kind = world ? AriaS002DecisionKind.WorldTarget : AriaS002DecisionKind.Control;
            output.Action=target.Action;output.Slot=target.Slot;output.TargetId = target.Id; output.Position = target.Position;
            output.Drag = target.Drag ? (byte)1 : (byte)0;
            output.DragEnd = target.DragEnd;
            output.ScrollTargetId = target.ScrollTargetId;
            output.ScrollDistanceViewports = target.ScrollDistanceViewports;
        }static bool OpeningDefense(in AriaS002Observation v,ref AriaS002PlanState p,ref AriaS002ContactState t,ref AriaS002Decision o)=>throw new NotSupportedException("Active air-opening construction not admitted");
static void StepHomePadPlacement(in AriaS002Observation v,ref AriaS002PlanState p,ref AriaS002ContactState t,ref AriaS002Decision o)=>throw new NotSupportedException("Active air-pad placement not admitted");
static bool StepAircraftCycle(in AriaS002Observation v,ref AriaS002PlanState p,ref AriaS002ContactState t,ref AriaS002Decision o){if(!v.AirProfile||p.AirCycleCount!=0)return false;throw new NotSupportedException("Active aircraft service not admitted");}

}
}
