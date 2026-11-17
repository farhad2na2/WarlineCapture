using Game.Components;
using Game.Runtime;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    /// <summary>Managed InputSystem edge only. Decisions belong to AriaPlayDecisionSystem.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AriaPlayInputSystem : SystemBase
    {
        private AriaTouchInputUiSystemHelper touch;
        private Game.Skirmish.Contracts.SkirmishPublicArmyOrderGoal gestureGoal;
        private string goalSession; private ulong goalWorld;
        private bool reportedBlocked;
        private void TraceS002(string eventName, in AriaPlaySessionComponent session, in AriaPlayObservationComponent observation)
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) return;
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (v.SourceScenarioIndex != 4 || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            Debug.Log($"[AriaS002Trace] event={eventName} frame={v.Frame} time={v.Time} phase={session.Phase} reason={session.StopReason} intent={p.Intent} kind={observation.Kind} target={session.TargetId} offered={observation.TargetId} goal={observation.GoalId} attempts={session.Attempts} actions={session.Actions} screen={session.Target} drag={session.Drag} dragEnd={session.DragEnd} lastProgress={session.LastProgressAt} objectiveProgress={session.LastObjectiveProgressAt} planProgress={p.LastProgressAt} infantry={v.Infantry} playerHealth={v.PlayerHealth} enemyHealth={v.EnemyHealth} distanceKnown={v.AssaultDistanceVisible} distance={v.NearestFriendlyObjectiveDistance} selected={v.SelectedCount} selectedMask={v.ExpandedSelectedMask} assaultMask={v.ExpandedAssaultMask} attackMask={v.ExpandedAttackOrderMask} page={v.ExpandedPageIndex} groups={v.ExpandedTotalGroups} group0={v.ExpandedGroup0} group1={v.ExpandedGroup1} group2={v.ExpandedGroup2} group3={v.ExpandedGroup3} group4={v.ExpandedGroup4} attackMode={v.AttackMode} homeThreat={v.PresentedHomeThreat} inspectStage={p.AssaultInspectStage} mapOpen={v.MapOpen} mapOwned={v.AssaultMap.ExpectedIdentity} mapId={v.AssaultMap.Identity} expectedMapId={p.AssaultInspectMapIdentity} closeAvailable={v.CloseMap.Available} precisionStage={p.PrecisionStage} precisionGroup={p.PrecisionGroup} precisionAttempts={p.PrecisionAttempts} receiptPresented={v.PrecisionAttack.Presented} receiptGroup={v.PrecisionAttack.GroupId} receiptAccepted={v.PrecisionAttack.AcceptedMembers} receiptSource={v.PrecisionAttack.SourceMembers} goalKind={gestureGoal.Kind} goalKnown={gestureGoal.Known} goalX={gestureGoal.ObjectiveX} goalZ={gestureGoal.ObjectiveZ}");
        }
        private int tracedHomeDefenseStage, tracedHomeDefenseReason;
        // Evidence only: reports home-defence transitions of the planner state.
        private void TraceHomeDefense()
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) { tracedHomeDefenseStage = tracedHomeDefenseReason = 0; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            int priorStage = tracedHomeDefenseStage, priorReason = tracedHomeDefenseReason;
            tracedHomeDefenseStage = p.HomeDefenseStage; tracedHomeDefenseReason = p.HomeDefenseReason;
            string state = priorStage == 0 && p.HomeDefenseStage != 0 ? "triggered"
                : priorStage != 5 && p.HomeDefenseStage == 5 ? "ordered"
                : priorStage != Game.Simulation.AriaHomeDefensePolicy.StageRecentre && p.HomeDefenseStage == Game.Simulation.AriaHomeDefensePolicy.StageRecentre ? "recentre"
                : priorStage != Game.Simulation.AriaHomeDefensePolicy.StageDisarm && p.HomeDefenseStage == Game.Simulation.AriaHomeDefensePolicy.StageDisarm ? "gave_up_disarm"
                : (priorStage == 4 || priorStage == Game.Simulation.AriaHomeDefensePolicy.StageRecentre) && p.HomeDefenseStage == 8 ? "gave_up"
                : priorReason != 0 && p.HomeDefenseReason == 0 ? "cleared" : null;
            if (state == null) return;
            int reasonCode = p.HomeDefenseReason != 0 ? p.HomeDefenseReason : priorReason;
            string reason = reasonCode == 2 ? "baseDamage" : reasonCode == 1 ? "minimap" : "none";
            string group = p.HomeDefenseReason == 0 ? "none" : p.HomeDefenseScope == 1 ? (p.HomeDefenseSlot - 1).ToString() : "all";
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            Debug.Log($"[AriaS002Trace] event=home_defense state={state} baseHealth={v.PlayerHealth} group={group} reason={reason} round={p.HomeDefenseRounds} stage={p.HomeDefenseStage} recovery={p.HomeDefenseRecovery} attackMode={v.AttackMode} homeGround={v.HomeGround.Available} homeGroundRefusal={v.HomeGroundRefusal} threatGround={v.ThreatGround.Available} threat={v.Threat.Available} focus={v.FocusPlayer.Available} t={matchSeconds:F2}");
        }
        private int tracedInterceptStage; private bool tracedInterceptKnown, tracedInterceptOffered;
        // Evidence only: reports intercept transitions ("finish what slips past") of the
        // planner state, and when a public slipping vehicle appears, is offered on screen, or leaves.
        private void TraceIntercept()
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) { tracedInterceptStage = 0; tracedInterceptKnown = tracedInterceptOffered = false; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (v.SourceScenarioIndex != 4 || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            int prior = tracedInterceptStage;
            bool known = v.InterceptKnown, offered = v.InterceptThreat.Available;
            string state = prior == 0 && p.InterceptStage != 0 ? "triggered"
                : prior != 4 && p.InterceptStage == 4 ? "ordered"
                : prior != 0 && p.InterceptStage == 0 ? "released"
                : known != tracedInterceptKnown || offered != tracedInterceptOffered ? "contact" : null;
            tracedInterceptStage = p.InterceptStage; tracedInterceptKnown = known; tracedInterceptOffered = offered;
            if (state == null) return;
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            string slot = p.InterceptSlot == Game.Simulation.AriaInterceptPolicy.KeepSelection ? "selection" : p.InterceptSlot.ToString();
            Debug.Log($"[AriaS002Trace] event=intercept state={state} known={known} offered={offered} stage={p.InterceptStage} count={p.InterceptCount} slot={slot} selectedMask={v.ExpandedSelectedMask} assaultMask={v.ExpandedAssaultMask} attackMask={v.ExpandedAttackOrderMask} page={v.ExpandedPageIndex} attackMode={v.AttackMode} homeThreat={v.PresentedHomeThreat} atBase={v.AssaultAtBase} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
        }
        private int tracedTrayPage = -1, tracedTrayRoster = -1, lastGestureTarget; private bool lastGestureDrag;
        // Evidence only: every visible roster page change with the gesture that preceded it
        // (tap = page chevron, swipe = tray drag). A skipped page shows as |to-from|>1.
        private void TraceTrayPage(in AriaPlaySessionComponent session)
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaObservationQuery;
            if (query.CalculateEntityCount() != 1) { tracedTrayPage = tracedTrayRoster = -1; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (v.SourceScenarioIndex != 4 || !v.ExpandedBaseAssault || v.ExpandedPageCount <= 0) return;
            int page = v.ExpandedPageIndex; int roster = (int)v.ExpandedRosterRevision;
            if (tracedTrayPage < 0 || roster != tracedTrayRoster) { tracedTrayPage = page; tracedTrayRoster = roster; return; }
            if (page == tracedTrayPage) return;
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            Debug.Log($"[AriaS002Trace] event=tray_page from={tracedTrayPage} to={page} gesture={(lastGestureDrag ? "swipe" : "tap")} target={lastGestureTarget} skipped={(System.Math.Abs(page - tracedTrayPage) > 1 ? 1 : 0)} pages={v.ExpandedPageCount} assaultMask={v.ExpandedAssaultMask} attackMask={v.ExpandedAttackOrderMask} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
            tracedTrayPage = page;
        }
        private int tracedBreacherStage; private bool tracedBreacherPending, tracedBreacherFielded;
        // Evidence only: the breacher coverage purchase (state=buying|done) and every breacher
        // order reaching the queue (state=pending, reason=coverage|replacement) or the field.
        private void TraceBreacher()
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) { tracedBreacherStage = 0; tracedBreacherPending = tracedBreacherFielded = false; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.ExpandedSession || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            string state = tracedBreacherStage == 0 && p.BreacherStage == 1 ? "buying"
                : tracedBreacherStage == 1 && p.BreacherStage == 2 ? "done"
                : !tracedBreacherPending && v.BreacherRecruitPending ? "pending"
                : !tracedBreacherFielded && v.BreacherFielded ? "fielded" : null;
            tracedBreacherStage = p.BreacherStage; tracedBreacherPending = v.BreacherRecruitPending; tracedBreacherFielded = v.BreacherFielded;
            if (state == null) return;
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            string reason = v.OwnStructureAttackUnits == 0 ? "replacement" : "coverage";
            Debug.Log($"[AriaS002Trace] event=breacher state={state} reason={reason} stage={p.BreacherStage} pending={v.BreacherRecruitPending} fielded={v.BreacherFielded} canAfford={v.CanAffordBreacher} cardOffered={v.RecruitBreacher.Available} drawer={v.DrawerOpen} structureUnits={v.OwnStructureAttackUnits} materials={v.OwnMaterials} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
        }
        private byte tracedCoverageCurrent; private int tracedCoveragePending, tracedCoverageFielded;
        private byte tracedSupportStage, tracedSupportDone;
        private static readonly string[] CoverageKindNames = { "none", "marksman", "apc_fast", "radar", "apc_heavy", "drone", "transport_heli" };
        private static readonly string[] SupportKindNames = { "none", "radar", "drone", "transport_heli" };
        // Evidence only, every expanded base assault (S002 native and generic planners):
        // coverage purchases (event=coverage kind=... state=buying|done|gave_up|pending|fielded) and each
        // support unit's one order (event=support_order kind=radar|drone|transport_heli
        // state=selecting|arming|tapping|ordered|released|gave_up).
        private void TraceCoverage()
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1)
            { tracedCoverageCurrent = tracedSupportStage = tracedSupportDone = 0; tracedCoveragePending = tracedCoverageFielded = 0; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.ExpandedSession || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            for (int k = 1; k < CoverageKindNames.Length; k++)
            {
                int bit = 1 << (k - 1);
                string state = tracedCoverageCurrent != k && p.CoverageCurrent == k ? "buying"
                    : tracedCoverageCurrent == k && p.CoverageCurrent != k ? ((v.CoveragePendingMask | v.CoverageFieldedMask) & bit) != 0 ? "done" : "gave_up"
                    : (tracedCoveragePending & bit) == 0 && (v.CoveragePendingMask & bit) != 0 ? "pending"
                    : (tracedCoverageFielded & bit) == 0 && (v.CoverageFieldedMask & bit) != 0 ? "fielded" : null;
                if (state == null) continue;
                Debug.Log($"[AriaS002Trace] event=coverage kind={CoverageKindNames[k]} state={state} scenario={v.SourceScenarioIndex} done={p.CoverageDone} affordMask={v.CoverageAffordMask} pendingMask={v.CoveragePendingMask} fieldedMask={v.CoverageFieldedMask} drawer={v.DrawerOpen} materials={v.OwnMaterials} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
            }
            tracedCoverageCurrent = p.CoverageCurrent;
            tracedCoveragePending = v.CoveragePendingMask; tracedCoverageFielded = v.CoverageFieldedMask;
            if (p.SupportStage != tracedSupportStage || p.SupportDone != tracedSupportDone)
            {
                bool newlyDone = (p.SupportDone & ~tracedSupportDone) != 0;
                string state = newlyDone ? (tracedSupportStage == 3 ? "ordered" : "gave_up")
                    : p.SupportStage switch { 1 => "selecting", 2 => "arming", 3 => "tapping", 5 => "disarming", _ => "released" };
                int kind = p.SupportKind < SupportKindNames.Length ? p.SupportKind : 0;
                Debug.Log($"[AriaS002Trace] event=support_order kind={SupportKindNames[kind]} state={state} scenario={v.SourceScenarioIndex} done={p.SupportDone} tried={p.SupportTried} slot={p.SupportSlot} page={v.ExpandedPageIndex} supportSlots={v.ExpandedSupportSlots} selectedMask={v.ExpandedSelectedMask} move={v.MoveMode} attack={v.AttackMode} scan={v.ScanMode} rear={v.RearGround.Available} front={v.FrontGround.Available} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
                tracedSupportStage = p.SupportStage; tracedSupportDone = p.SupportDone;
            }
        }
        private int tracedColumnSkips, tracedFindBaseGiveUps;
        // Evidence only: a roster page given up by the column stall guard (card never offered, or an
        // order that never shows) and a bounded base search that disarmed Attack, any expanded base assault.
        private void TraceColumnRecovery()
        {
            if (!AriaCommandEvidence.Active) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) { tracedColumnSkips = tracedFindBaseGiveUps = 0; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.ExpandedSession || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            if (p.ColumnSkips == tracedColumnSkips && p.FindBaseGiveUps == tracedFindBaseGiveUps) return;
            string state = p.FindBaseGiveUps != tracedFindBaseGiveUps ? "find_base_given_up" : "page_skipped";
            int cards = 0;
            for (int i = 0; i < 5; i++) if (v.Squad(i).Available) cards |= 1 << i;
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            Debug.Log($"[AriaS002Trace] event=column_recovery state={state} scenario={v.SourceScenarioIndex} skips={p.ColumnSkips} findBaseGiveUps={p.FindBaseGiveUps} page={v.ExpandedPageIndex} pages={v.ExpandedPageCount} assaultMask={v.ExpandedAssaultMask} attackMask={v.ExpandedAttackOrderMask} selectedMask={v.ExpandedSelectedMask} structureMask={v.ExpandedStructureMask} squadTargets={cards} attackMode={v.AttackMode} enemyBase={v.EnemyBase.Available} advanceGround={v.AdvanceGround.Available} frame={v.Frame} time={v.Time} t={matchSeconds:F2}");
            tracedColumnSkips = p.ColumnSkips; tracedFindBaseGiveUps = p.FindBaseGiveUps;
        }
        private void ClearGoalContext(){if(goalSession!=null)AriaCommandEvidence.RetirePublicGoals(goalSession,goalWorld);goalSession=null;goalWorld=0;gestureGoal=default;}
        private void CaptureGoal(int targetId)
        {
            gestureGoal=default;
            var query=ariaStateQuery;
            if(query.CalculateEntityCount()!=1)return;
            var view=query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            var plan=query.GetSingleton<AriaSkirmishPlanComponent>();
            // Native ground advance and home-threat controls share -20006.
            // Bind the original decision intent, never the control ID alone.
            if(plan.Intent==AriaSkirmishIntent.DefendBase||plan.HomeDefenseStage!=0)return;
            if(view.AdvanceGround.Available&&targetId==view.AdvanceGround.Id)gestureGoal=view.AdvanceOrderGoal;
            else if(view.EnemyBase.Available&&targetId==view.EnemyBase.Id)gestureGoal=view.VisibleObjectiveOrderGoal;
            if(gestureGoal.Known){goalSession=gestureGoal.SessionId;goalWorld=gestureGoal.WorldSequenceNumber;}
        }
        private EntityQuery missionQuery, skirmishQuery, operationsQuery;
        // Created once in OnCreate (system-owned): traces and goal capture read these every frame.
        private EntityQuery ariaStateQuery, ariaObservationQuery;
        protected override void OnCreate()
        {
            touch = new AriaTouchInputUiSystemHelper();
            AriaTouchInputUiSystemHelper.Evidence += ObserveTouchEvidence;
            skirmishQuery = GetEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            ariaStateQuery = GetEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationComponent>(), ComponentType.ReadOnly<AriaSkirmishPlanComponent>());
            ariaObservationQuery = GetEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationComponent>());
            missionQuery = GetEntityQuery(ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            operationsQuery = GetEntityQuery(ComponentType.ReadOnly<OperationsReconMissionComponent>());
        }
        protected override void OnDestroy()
        {
            AriaTouchInputUiSystemHelper.Evidence -= ObserveTouchEvidence;
            touch?.Dispose();ClearGoalContext();
        }
        private void ObserveTouchEvidence(AriaTouchInputUiSystemHelper source, string kind, Vector2 position)
        {
            if (ReferenceEquals(source, touch) && kind == "Ended")
                { AriaCommandEvidence.ObserveRelease(UnityEngine.Time.frameCount, position,gestureGoal);gestureGoal=default; }
        }
        public int ExplicitStops { get; private set; }
        public int MeasuredHumanInterventions => touch == null || touch.StartedRuns == 0 ? -1 :
            (int)touch.PhysicalInterventions + ExplicitStops;
        public uint DispatchedGestures => touch?.DispatchedGestures ?? 0;
        public uint CompletedGestures => touch?.CompletedGestures ?? 0;
        public uint AcceptedSamples => touch?.AcceptedSamples ?? 0;
        public uint UnexpectedSamples => touch?.UnexpectedSamples ?? 0;
        public void Cancel()
        {
            bool finished = skirmishQuery.CalculateEntityCount() == 1 &&
                skirmishQuery.GetSingleton<SkirmishMatchState>().Phase == SkirmishPhase.Finished;
            if (touch != null && touch.IsRunning && !finished) ExplicitStops++;
            touch?.Stop();ClearGoalContext();
        }

        // A completed campaign component can remain when the player starts Skirmish.
        // The current Skirmish owns its own terminal state.
        public static bool IsCurrentMatchFinished(bool hasSkirmish, SkirmishPhase skirmishPhase,
            bool hasCampaign, MissionOutcomeKind campaignOutcome) => hasSkirmish
                ? skirmishPhase == SkirmishPhase.Finished
                : hasCampaign && campaignOutcome != MissionOutcomeKind.None;

        protected override void OnUpdate()
        {
            bool found = false;
            TraceHomeDefense();
            TraceIntercept();
            TraceBreacher();
            TraceCoverage();
            TraceColumnRecovery();
            // Terminal outcome is a public lifecycle signal, never a targeting/planning input.
            bool hasSkirmish = skirmishQuery.CalculateEntityCount() == 1;
            bool hasCampaign = missionQuery.CalculateEntityCount() == 1;
            bool hasOperations = operationsQuery.CalculateEntityCount() == 1;
            bool ended = IsCurrentMatchFinished(hasSkirmish,
                hasSkirmish ? skirmishQuery.GetSingleton<SkirmishMatchState>().Phase : default,
                hasCampaign, hasCampaign ? missionQuery.GetSingleton<CampaignMissionRuntimeComponent>().Outcome : default) ||
                hasOperations && operationsQuery.GetSingleton<OperationsReconMissionComponent>().Phase == OperationsReconPhase.Terminal;
            foreach (var (observation, session, shell) in SystemAPI.Query<RefRO<AriaPlayObservationComponent>, RefRW<AriaPlaySessionComponent>, RefRO<UiShellStateComponent>>())
            {
                found = true;
                ref var current = ref session.ValueRW;
                TraceTrayPage(current);
                if (current.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked)
                { if (current.Phase == AriaPlayPhase.Blocked && !reportedBlocked) { TraceS002("blocked", current, observation.ValueRO); reportedBlocked = true; }
                  if (touch.IsRunning) touch.Stop();ClearGoalContext(); current.Pressed = 0; continue; }
                reportedBlocked = false;
                if (ended || shell.ValueRO.ActiveRoute != UIRoute.Match ||
                    (!Application.isFocused && !AriaTouchInputUiSystemHelper.AllowBackgroundValidation) || UnityEngine.Time.timeScale <= 0 ||
                    (current.Phase != AriaPlayPhase.Starting && UnityEngine.Time.frameCount - observation.ValueRO.Frame > 5) ||
                    observation.ValueRO.Kind == AriaPlayObservationKind.Finished)
                {
                    Debug.LogWarning("[AriaInputLifecycleStop] ended=" + ended + " route=" + shell.ValueRO.ActiveRoute +
                        " timeScale=" + UnityEngine.Time.timeScale + " kind=" + observation.ValueRO.Kind +
                        " observationFrame=" + observation.ValueRO.Frame + " frame=" + UnityEngine.Time.frameCount);
                    touch.Stop();ClearGoalContext(); current.Phase = AriaPlayPhase.Manual; current.Pressed = current.GestureRequested = 0;
                    current.StopReason = (byte)(ended ? 5 :
                        !Application.isFocused && !AriaTouchInputUiSystemHelper.AllowBackgroundValidation ? 2 :
                        UnityEngine.Time.timeScale <= 0 ? 3 : shell.ValueRO.ActiveRoute != UIRoute.Match ? 1 : 4);
                    continue;
                }
                if (current.Phase == AriaPlayPhase.Starting)
                {
                    if (Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime) < current.DueAt) continue;
                    if (UnityEngine.Time.frameCount - observation.ValueRO.Frame > 5)
                    {
                        if (Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime) - current.DueAt > 3)
                        { current.Phase = AriaPlayPhase.Blocked; current.StopReason = 4; }
                        continue;
                    }
                    if (touch.Start()) current.Phase = AriaPlayPhase.Observing;
                    else if (Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime) - current.DueAt > 3) current.Phase = AriaPlayPhase.Blocked;
                    continue;
                }
                if (!touch.IsRunning) { ClearGoalContext(); current.Phase = AriaPlayPhase.Manual; current.Pressed = current.GestureRequested = 0; current.StopReason = touch.LastInterruption != 0 ? touch.LastInterruption : (byte)6; continue; }
                if (current.GestureRequested != 0)
                {
                    current.GestureRequested = 0;
                    CaptureGoal(current.TargetId);
                    lastGestureDrag = current.Drag != 0; lastGestureTarget = current.TargetId;
                    TraceS002("gesture", current, observation.ValueRO);
                    if (!touch.TryGesture(current.Target, current.Drag != 0 ? current.DragEnd : current.Target,
                        current.Drag != 0 ? .5f : hasSkirmish ? .15f : .08f, current.Drag != 0 ? .9f : 0, Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime)))
                    { current.Phase = AriaPlayPhase.Blocked; current.StopReason = 116; TraceS002("blocked", current, observation.ValueRO); reportedBlocked = true; touch.Stop();ClearGoalContext(); continue; }
                }
                touch.Tick(Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime));
                current.Contact = touch.ContactPosition;
                current.Pressed = (byte)(touch.IsPressed ? 1 : 0);
                if (current.Phase == AriaPlayPhase.Touching && !touch.IsBusy)
                {
                    current.Actions++;
                    current.LastCompletedTargetId=current.TargetId;
                    current.LastCompletedFrame=UnityEngine.Time.frameCount;
                    current.LastCompletedPosition=current.Contact;
                    current.Phase = AriaPlayPhase.Verifying;
                    current.DueAt = Game.Runtime.SkirmishValidationClock.UnscaledOrOriginal(UnityEngine.Time.unscaledTime) + (hasSkirmish ? .65f : 1.5f);
                }
            }
            if (!found && touch.IsRunning) {touch.Stop();ClearGoalContext();}
        }
    }
}
