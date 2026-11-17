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
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationComponent>(), ComponentType.ReadOnly<AriaSkirmishPlanComponent>());
            if (query.CalculateEntityCount() != 1) return;
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (v.SourceScenarioIndex != 4 || !v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            Debug.Log($"[AriaS002Trace] event={eventName} frame={v.Frame} time={v.Time} phase={session.Phase} reason={session.StopReason} intent={p.Intent} kind={observation.Kind} target={session.TargetId} offered={observation.TargetId} goal={observation.GoalId} attempts={session.Attempts} actions={session.Actions} screen={session.Target} drag={session.Drag} dragEnd={session.DragEnd} lastProgress={session.LastProgressAt} objectiveProgress={session.LastObjectiveProgressAt} planProgress={p.LastProgressAt} infantry={v.Infantry} playerHealth={v.PlayerHealth} enemyHealth={v.EnemyHealth} distanceKnown={v.AssaultDistanceVisible} distance={v.NearestFriendlyObjectiveDistance} selected={v.SelectedCount} selectedMask={v.ExpandedSelectedMask} assaultMask={v.ExpandedAssaultMask} attackMask={v.ExpandedAttackOrderMask} page={v.ExpandedPageIndex} groups={v.ExpandedTotalGroups} group0={v.ExpandedGroup0} group1={v.ExpandedGroup1} group2={v.ExpandedGroup2} group3={v.ExpandedGroup3} group4={v.ExpandedGroup4} attackMode={v.AttackMode} homeThreat={v.PresentedHomeThreat} inspectStage={p.AssaultInspectStage} mapOpen={v.MapOpen} mapOwned={v.AssaultMap.ExpectedIdentity} mapId={v.AssaultMap.Identity} expectedMapId={p.AssaultInspectMapIdentity} closeAvailable={v.CloseMap.Available} precisionStage={p.PrecisionStage} precisionGroup={p.PrecisionGroup} precisionAttempts={p.PrecisionAttempts} receiptPresented={v.PrecisionAttack.Presented} receiptGroup={v.PrecisionAttack.GroupId} receiptAccepted={v.PrecisionAttack.AcceptedMembers} receiptSource={v.PrecisionAttack.SourceMembers} goalKind={gestureGoal.Kind} goalKnown={gestureGoal.Known} goalX={gestureGoal.ObjectiveX} goalZ={gestureGoal.ObjectiveZ}");
        }
        private int tracedHomeDefenseStage, tracedHomeDefenseReason, tracedHomeGuardChosen;
        // Evidence only: reports home-defence transitions of the planner state.
        private void TraceHomeDefense()
        {
            if (!AriaCommandEvidence.Active) return;
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationComponent>(), ComponentType.ReadOnly<AriaSkirmishPlanComponent>());
            if (query.CalculateEntityCount() != 1) { tracedHomeDefenseStage = tracedHomeDefenseReason = tracedHomeGuardChosen = 0; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.ExpandedBaseAssault) return;
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            if (p.HomeGuardChosen != tracedHomeGuardChosen)
            {
                tracedHomeGuardChosen = p.HomeGuardChosen;
                if (p.HomeGuardChosen != 0)
                {
                    float guardSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
                    Debug.Log($"[AriaS002Trace] event=home_guard group={p.HomeGuardGroup} page={v.ExpandedPageIndex} assaultMask={v.ExpandedAssaultMask} structureMask={v.ExpandedStructureMask} infantryMask={v.ExpandedInfantryMask} alive={v.ExpandedAlive0},{v.ExpandedAlive1},{v.ExpandedAlive2},{v.ExpandedAlive3},{v.ExpandedAlive4} t={guardSeconds:F2}");
                }
            }
            int priorStage = tracedHomeDefenseStage, priorReason = tracedHomeDefenseReason;
            tracedHomeDefenseStage = p.HomeDefenseStage; tracedHomeDefenseReason = p.HomeDefenseReason;
            string state = priorStage == 0 && p.HomeDefenseStage != 0 ? "triggered"
                : priorStage != 5 && p.HomeDefenseStage == 5 ? "ordered"
                : priorReason != 0 && p.HomeDefenseReason == 0 ? "cleared" : null;
            if (state == null) return;
            int reasonCode = p.HomeDefenseReason != 0 ? p.HomeDefenseReason : priorReason;
            string reason = reasonCode == 2 ? "baseDamage" : reasonCode == 1 ? "minimap" : "none";
            string group = p.HomeDefenseReason == 0 ? "none" : p.HomeDefenseScope == 1 ? (p.HomeDefenseSlot - 1).ToString() : "all";
            float matchSeconds = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            Debug.Log($"[AriaS002Trace] event=home_defense state={state} baseHealth={v.PlayerHealth} group={group} reason={reason} round={p.HomeDefenseRounds} stage={p.HomeDefenseStage} t={matchSeconds:F2}");
        }
        private void ClearGoalContext(){if(goalSession!=null)AriaCommandEvidence.RetirePublicGoals(goalSession,goalWorld);goalSession=null;goalWorld=0;gestureGoal=default;}
        private void CaptureGoal(int targetId)
        {
            gestureGoal=default;
            using var query=EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AriaSkirmishObservationComponent>(),ComponentType.ReadOnly<AriaSkirmishPlanComponent>());
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
        protected override void OnCreate()
        {
            touch = new AriaTouchInputUiSystemHelper();
            AriaTouchInputUiSystemHelper.Evidence += ObserveTouchEvidence;
            skirmishQuery = GetEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
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
