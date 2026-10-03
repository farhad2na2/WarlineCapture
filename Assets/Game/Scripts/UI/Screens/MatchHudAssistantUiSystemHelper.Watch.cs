using System.Collections.Generic;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private GameObject watchOverlay;
        private AriaHolographicFingerGraphic watchFinger;
        private Button watchStop;
        private string watchStopLocale;
        private PointerEventData watchRaycast;
        private EventSystem watchEventSystem;
        private readonly List<RaycastResult> watchHits = new(16);
        private OperationsMissionScreenView watchOperations;
        private int operationsWatchActions;
        private bool operationsLastWasTravel;
        private float operationsTravelUntil;
        private bool supplyWatchFocusSet;
        private Vector3 supplyWatchFocusTarget;
        private float supplyWatchFocusAt;
        private bool extractionWatchPointSet;
        private Vector2 extractionWatchPoint;
        private Vector2 extractionWatchDragEnd;
        private float extractionWatchPointStableAt;

        private void TickWatch()
        {
            if (_embeddedTutorialView == null) return;
            if (UiShellRuntimeGateway.TryReadOperationsMission(out var operation) && operation.InMission)
            {
                UiShellRuntimeGateway.PublishAriaSkirmishObservation(default);
                UiShellRuntimeGateway.PublishAriaObservation(ObserveOperationsWatch(operation));
                var operationState = UiShellRuntimeGateway.ReadAriaPlay();
                _embeddedTutorialView.PresentWatch(operationState, false);
                RenderWatchFinger(operationState);
                return;
            }
            var kind = AriaPlayObservationKind.Unavailable;
            int target = 0;
            Vector2 position = default;
            Vector2 dragEnd=default;bool drag=false;
            bool supplyFocusPending=false;
            bool skirmish = UiShellRuntimeGateway.TryReadSkirmish(out var skirmishModel);
            bool narrativeBlocking = false;
            if (!skirmish)
                foreach (var narrative in Object.FindObjectsByType<NarrativeSequenceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (narrative.IsVisible) { narrativeBlocking = true; break; }
            bool supported = UiShellRuntimeGateway.ReadAriaPlayCapability() != AriaPlayCapability.None;
            bool available = supported && (skirmish ? !skirmishModel.Finished && !skirmishModel.StartupFailed : UsesNextTutorialAction && _lastPanelModel.HasRecommendation);
            bool supplyWaiting=_lastPanelModel.TutorialStepCount==4 && UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var supplyTarget) &&
                (supplyTarget.BattleAction==UiTutorialBattleAction.Watch || supplyTarget.Moving);
            if (available && !skirmish && supplyWaiting)kind=AriaPlayObservationKind.Waiting;
            // Observe the real tactical command control directly after selection. The
            // decorative tutorial frame can trail the selection projection by a frame,
            // but Watch must still perform the same visible Move/Attack button press as
            // the player before it is allowed to touch the world destination.
            if(available && !skirmish && kind!=AriaPlayObservationKind.Control &&
               UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var tutorialTarget) &&
               !tutorialTarget.NeedsSelection && !tutorialTarget.Moving && !tutorialTarget.ExecutingAttack)
            {
                if(tutorialTarget.BattleAction==UiTutorialBattleAction.Hold&&UiShellRuntimeGateway.IsDefensePreparationGuideContext())
                {
                    var command=ObserveWatchButton(_commandControlsView?.HoldButton);
                    if(command.Available){kind=AriaPlayObservationKind.Control;target=command.Id;position=command.Position;}
                }
                TacticalCommandMode required=tutorialTarget.BattleAction switch
                {
                    UiTutorialBattleAction.Move=>TacticalCommandMode.Move,
                    UiTutorialBattleAction.Attack=>TacticalCommandMode.Attack,
                    _=>TacticalCommandMode.None
                };
                if(UiShellRuntimeGateway.IsLastCorridorGuideContext()&&UiShellRuntimeGateway.TryReadLastCorridor(out var corridorBoarding)&&corridorBoarding.Stage==7&&!corridorBoarding.EngineerAboard)required=TacticalCommandMode.Board;
                if(UiShellRuntimeGateway.IsNetworkCollapseGuideContext()&&UiShellRuntimeGateway.TryReadNetworkCollapse(out var boarding)&&boarding.Stage==8&&!boarding.EngineerAboard)required=TacticalCommandMode.Board;
                if(required!=TacticalCommandMode.None && _activeCommandMode!=required)
                {
                    var command=ObserveWatchButton(required==TacticalCommandMode.Board ? _commandControlsView?.CommandWheelPanel?.NextBoardButton : required==TacticalCommandMode.Move
                        ?_commandControlsView?.MoveButton:_commandControlsView?.AttackButton);
                    if(command.Available){kind=AriaPlayObservationKind.Control;target=command.Id;position=command.Position;}
                }
                else if(required!=TacticalCommandMode.None)
                {
                    // Materialize the same public world cue that the manual tutorial uses.
                    // Watch may observe it only after the required command mode is visibly
                    // active; the subsequent tap remains a normal world-input gesture.
                    _highlightPresentationSystem.ShowTutorialWorld(tutorialTarget.Destination);
                    if(!_highlightPresentationSystem.HasVisibleDirectTutorialTarget &&
                       !UiShellRuntimeGateway.IsEvidenceChainGuideContext() &&
                       !UiShellRuntimeGateway.IsSupplyLineGuideContext() &&
                       !UiShellRuntimeGateway.IsPowerRelayGuideContext() &&
                       !UiShellRuntimeGateway.IsRouteReopenedGuideContext() &&
                       !UiShellRuntimeGateway.IsExtractionGuideContext() &&
                       !UiShellRuntimeGateway.IsGroundedSignalGuideContext() &&
                       !UiShellRuntimeGateway.IsArmorBreakGuideContext() &&
                       !UiShellRuntimeGateway.IsCitywideAlertGuideContext() &&
                       !UiShellRuntimeGateway.IsNetworkCollapseGuideContext() &&
                       !UiShellRuntimeGateway.IsCommandNodeGuideContext() && !UiShellRuntimeGateway.IsLastCorridorGuideContext() &&
                       !UiShellRuntimeGateway.IsTrustUnderFireGuideContext() &&
                       // Air Corridor observes and taps the existing Show Me control.
                       // Do not give Watch a hidden camera shortcut around that input.
                       !UiShellRuntimeGateway.IsDefensePreparationGuideContext())
                        UiShellRuntimeGateway.TryFocusMissionTutorialTarget(false);
                }
            }
            if (available && !skirmish && !supplyWaiting && kind!=AriaPlayObservationKind.Control)
            {
                kind = _tutorialCinematicSuspended ? AriaPlayObservationKind.Cinematic : AriaPlayObservationKind.Waiting;
                if(!_tutorialCinematicSuspended && _highlightPresentationSystem.TryObserveVisibleSelectionDrag(out position,out dragEnd))
                {
                    target=-30001;
                    if(WatchTargetIsReachable(position,target,true) && WatchTargetIsReachable(dragEnd,target,true) && WatchTargetIsReachable((position+dragEnd)*.5f,target,true))
                    {kind=AriaPlayObservationKind.WorldTarget;drag=true;}
                }
                else if (!_tutorialCinematicSuspended && _highlightPresentationSystem.TryObserveVisibleGuide(out position, out target, out bool world))
                {
                    if (WatchTargetIsReachable(position, target, world))
                        kind = world ? AriaPlayObservationKind.WorldTarget : AriaPlayObservationKind.Control;
                }
                // The large refinery map can put a unit or road destination behind
                // a side panel while Show Me is still panning. Wait for a usable
                // center-screen cue instead of spending Watch retries on that panel.
                if (!_tutorialCinematicSuspended &&
                    (!UiShellRuntimeGateway.IsRouteReopenedGuideContext() || UiShellRuntimeGateway.ReadAriaPlay().Active) &&
                    (UiShellRuntimeGateway.IsSupplyLineGuideContext() || UiShellRuntimeGateway.IsPowerRelayGuideContext() || UiShellRuntimeGateway.IsRouteReopenedGuideContext()) &&
                    UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var supplyFocus) && Camera.main != null)
                {
                    Vector3 worldPoint=supplyFocus.NeedsSelection?supplyFocus.Selection:supplyFocus.Destination;
                    Vector3 screenPoint=Camera.main.WorldToScreenPoint(worldPoint);
                    if(screenPoint.z<=0 || screenPoint.x<Screen.width*.26f || screenPoint.x>Screen.width*.74f ||
                       screenPoint.y<Screen.height*.20f || screenPoint.y>Screen.height*.78f)
                    {
                        // Reissuing a smooth focus every frame restarts the pan. Let
                        // each camera request settle before following a moving hauler.
                        float sinceFocus=Time.unscaledTime-supplyWatchFocusAt;
                        if(!supplyWatchFocusSet || sinceFocus>3f &&
                           (Vector3.Distance(worldPoint,supplyWatchFocusTarget)>35f || sinceFocus>8f))
                        {
                            if(UiShellRuntimeGateway.TryFocusMissionTutorialTarget(supplyFocus.NeedsSelection))
                            {supplyWatchFocusSet=true;supplyWatchFocusTarget=worldPoint;supplyWatchFocusAt=Time.unscaledTime;}
                        }
                        supplyFocusPending=true;
                        kind=AriaPlayObservationKind.Waiting;target=0;drag=false;dragEnd=default;
                    }
                    else supplyWatchFocusSet=false;
                }
                if (!_tutorialCinematicSuspended && kind == AriaPlayObservationKind.Waiting &&
                    !supplyFocusPending &&
                    _embeddedTutorialView.ShowMeButton is Button show && show.IsActive() && show.IsInteractable())
                {
                    RectTransform rect = (RectTransform)show.transform;
                    position = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(show), rect.TransformPoint(rect.rect.center));
                    target = show.GetEntityId().GetHashCode();
                    if (WatchTargetIsReachable(position, target, false)) kind = AriaPlayObservationKind.Control;
                }
            }
            if (skirmish && supported) { kind = skirmishModel.Finished ? AriaPlayObservationKind.Finished : AriaPlayObservationKind.Waiting; ObserveSkirmishWatch(skirmishModel); }
            else UiShellRuntimeGateway.PublishAriaSkirmishObservation(default);
            // A comms comic can interrupt live play. Keep Watch active, but never
            // touch tactical controls underneath its raycast-blocking canvas.
            if (narrativeBlocking)
            {
                kind = AriaPlayObservationKind.Cinematic;
                target = 0;
                position = default;
                drag = false;
                dragEnd = default;
            }
            // Show Me pans the extraction camera. A touch held at yesterday's
            // screen position becomes a different ground order while that camera
            // moves. Observe the presented cue until it settles before touching it.
            if ((UiShellRuntimeGateway.IsExtractionGuideContext() || UiShellRuntimeGateway.IsRouteReopenedGuideContext() || UiShellRuntimeGateway.IsGroundedSignalGuideContext() || UiShellRuntimeGateway.IsArmorBreakGuideContext() || UiShellRuntimeGateway.IsCitywideAlertGuideContext() || UiShellRuntimeGateway.IsTrustUnderFireGuideContext() || UiShellRuntimeGateway.IsNetworkCollapseGuideContext() || UiShellRuntimeGateway.IsCommandNodeGuideContext() || UiShellRuntimeGateway.IsLastCorridorGuideContext()) && kind == AriaPlayObservationKind.WorldTarget)
            {
                if (!extractionWatchPointSet || Vector2.Distance(position, extractionWatchPoint) > 1f ||
                    drag && Vector2.Distance(dragEnd, extractionWatchDragEnd) > 1f)
                {
                    extractionWatchPointStableAt = Time.unscaledTime;
                    extractionWatchPoint = position;
                    extractionWatchDragEnd = dragEnd;
                }
                extractionWatchPointSet = true;
                if (Time.unscaledTime - extractionWatchPointStableAt < .5f)
                { kind = AriaPlayObservationKind.Waiting; target = 0; drag = false; dragEnd = default; }
            }
            else extractionWatchPointSet = false;
            if (!skirmish && _finalTutorialSuppressed) kind = AriaPlayObservationKind.Finished;
            int goalId=_lastPanelModel.TutorialStepCount * 100 + _lastPanelModel.TutorialStep;
            // Grounded Signal's public relay step includes reaching the service
            // gate and then disabling the relay. Reaching attack range is real
            // progress; give that visible sub-objective its own watchdog goal.
            if(UiShellRuntimeGateway.IsGroundedSignalGuideContext())
            {
                int stage=_lastPanelModel.TutorialStep;
                goalId=6600+stage+(stage>=3?1:0);
                if(stage==2 && UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var groundedTarget) &&
                    groundedTarget.BattleAction==UiTutorialBattleAction.Attack)goalId=6603;
            }
            // A reached attack approach is real progress in the public aircraft/armor step.
            if(UiShellRuntimeGateway.IsArmorBreakGuideContext())
            {
                int stage=_lastPanelModel.TutorialStep;
                goalId=6700+stage*8;
                if(stage==4 && UiShellRuntimeGateway.TryReadArmorBreak(out var armorProgress))
                    goalId+=Mathf.Clamp(armorProgress.ArmoredDefendersCleared,0,3)*2;
                if(UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var armorTarget) &&
                    armorTarget.BattleAction==UiTutorialBattleAction.Attack)goalId++;
            }
            if(UiShellRuntimeGateway.IsCommandNodeGuideContext()&&UiShellRuntimeGateway.TryReadCommandNode(out var commandProgress))
                goalId=UiCommandNodeProgress.WatchGoal(commandProgress.Stage,commandProgress.HostilesDefeated,commandProgress.IsolationStep,commandProgress.ReleaseOrdered,commandProgress.CoverReady);
            if(UiShellRuntimeGateway.IsLastCorridorGuideContext()&&UiShellRuntimeGateway.TryReadLastCorridor(out var corridorProgress))
                goalId=UiLastCorridorProgress.WatchGoal(corridorProgress.Stage,corridorProgress.HostilesDefeated,corridorProgress.RouteStep,corridorProgress.EngineerAboard);
            if(UiShellRuntimeGateway.IsNetworkCollapseGuideContext()&&UiShellRuntimeGateway.TryReadNetworkCollapse(out var networkProgress))goalId=7000+networkProgress.Stage*8+Mathf.Clamp(networkProgress.HostilesDefeated,0,9)*2+(networkProgress.EngineerAboard?1:0);
            if(UiShellRuntimeGateway.IsTrustUnderFireGuideContext() && UiShellRuntimeGateway.TryReadTrustUnderFire(out var trustProgress)) goalId=6900+trustProgress.Stage*32+Mathf.Clamp(trustProgress.HostilesDefeated,0,9)*2+(trustProgress.NorthCrossed?1:0)+(trustProgress.SouthCrossed?2:0);
            if(UiShellRuntimeGateway.IsCitywideAlertGuideContext() && UiShellRuntimeGateway.TryReadCitywideAlert(out var citywideProgress)) goalId=6800+citywideProgress.Stage*16+Mathf.Clamp(citywideProgress.HostilesCleared,0,11);
            // A delivered load is visible reserve progress, even while the same
            // defend-storage instruction remains on screen.
            if(_lastPanelModel.TutorialStepCount==4 && UiShellRuntimeGateway.TryReadSupplyLineDelivery(out int reserveFuel))
                goalId=7600+_lastPanelModel.TutorialStep*8+Mathf.Clamp(reserveFuel/8,0,6);
            UiShellRuntimeGateway.PublishAriaObservation(new AriaPlayObservation(kind, target,
                goalId, position, Time.frameCount, Time.unscaledTime,drag,dragEnd));
            var state = UiShellRuntimeGateway.ReadAriaPlay();
            _embeddedTutorialView.PresentWatch(state, available);
            _embeddedTutorialView.RefreshContentLayout();
            RenderWatchFinger(state, narrativeBlocking);
        }

        private AriaPlayObservation ObserveOperationsWatch(UiOperationsMissionModel model)
        {
            float now = Time.unscaledTime;
            int goal = model.NextSite >= 0 ? 100 + model.NextSite : model.EvidenceCarried ? 300 : 200;
            if (model.Finished) return new AriaPlayObservation(AriaPlayObservationKind.Finished, 0, goal, default, Time.frameCount, now);
            if (watchOperations == null)
                foreach (var candidate in Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (candidate.IsHud) { watchOperations = candidate; break; }
            if (watchOperations == null) return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
            var state = UiShellRuntimeGateway.ReadAriaPlay();
            if (state.Actions != operationsWatchActions)
            {
                if (operationsLastWasTravel) operationsTravelUntil = now + 12f;
                operationsLastWasTravel = false;
                operationsWatchActions = state.Actions;
            }
            if (!state.Active) operationsTravelUntil = 0;
            Button target = null;
            if (model.Introduction)
            {
                if (!model.Touring) target = watchOperations.IntroductionButton;
                else return new AriaPlayObservation(AriaPlayObservationKind.Cinematic, 0, goal, default, Time.frameCount, now);
            }
            else if (model.Paused) return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
            else
            {
                if (watchSquads == null) watchSquads = Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                if (watchSquads == null || !UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected) || !selected.Visible ||
                    (int)watchSquads.VisibleSelectedSlot <= 0) target = watchSquads?.VisibleCardButton(0);
                else
                {
                    bool scanning = false, inRange = false;
                    for (int i = 0; model.SiteProgress != null && i < model.SiteProgress.Length; i++)
                    {
                        scanning |= !model.SiteCompleted[i] && model.SiteProgress[i] > 0;
                        inRange |= model.CanScanSite[i];
                    }
                    if (scanning || !model.EvidenceCarried && model.EvidenceProgress > 0)
                        return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
                    if (inRange) target = watchOperations.ScanButton(model.NextSite);
                    else if (model.CanRecoverHere && !model.EvidenceCarried) target = watchOperations.RecoverButton;
                    else if (now >= operationsTravelUntil)
                    {
                        if (!watchOperations.TryObserveObjective(out var point)) target = watchOperations.ObjectiveButton;
                        else if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) || command.ActiveCommandMode != Game.Tactical.Contracts.TacticalCommandMode.Attack)
                            target = _commandControlsView?.AttackButton;
                        else if (WatchTargetIsReachable(point, -31000 - goal, true))
                        {
                            operationsLastWasTravel = true;
                            return new AriaPlayObservation(AriaPlayObservationKind.WorldTarget, -31000 - goal, goal, point, Time.frameCount, now);
                        }
                    }
                }
            }
            var observed = ObserveWatchButton(target);
            return observed.Available
                ? new AriaPlayObservation(AriaPlayObservationKind.Control, observed.Id, goal, observed.Position, Time.frameCount, now)
                : new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
        }

        private bool WatchTargetIsReachable(Vector2 point, int targetId, bool world)
        {
            if (!Screen.safeArea.Contains(point) || EventSystem.current == null) return false;
            if (watchRaycast == null || watchEventSystem != EventSystem.current)
            {
                watchEventSystem = EventSystem.current;
                watchRaycast = new PointerEventData(EventSystem.current);
            }
            watchRaycast.position = point;
            watchHits.Clear(); EventSystem.current.RaycastAll(watchRaycast, watchHits);
            if (world) return watchHits.Count == 0;
            // Labels and decorative panel graphics commonly sit above a button.
            // Unity still dispatches the click to the first interactable button in
            // the raycast chain, so ignore non-button graphics while preserving a
            // real interactable control that is layered in front of the target.
            for (int i = 0; i < watchHits.Count; i++)
            {
                Button button = watchHits[i].gameObject.GetComponentInParent<Button>();
                if (button == null || !button.IsActive() || !button.IsInteractable())
                    continue;
                return button.GetEntityId().GetHashCode() == targetId;
            }
            return false;
        }

        private void RenderWatchFinger(AriaPlayModel state, bool suppressFinger = false)
        {
            bool visible = state.Active;
            if (watchOverlay == null && visible)
            {
                watchOverlay = new GameObject("AriaTouchPresentation", typeof(RectTransform), typeof(Canvas));
                var canvas = watchOverlay.GetComponent<Canvas>();
                // Above the Skirmish objective HUD (32700) and tutorial cues (32750).
                // Keep within Unity's signed 16-bit sorting-order range.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
                watchOverlay.AddComponent<GraphicRaycaster>();
                watchStop = Object.Instantiate(_embeddedTutorialView.WatchButton, watchOverlay.transform);
                watchStop.name = "AriaInstantStop";
                watchStop.onClick = new Button.ButtonClickedEvent();
                watchStop.onClick.AddListener(UiShellRuntimeGateway.StopAriaPlay);
                watchStop.gameObject.SetActive(true);
                watchStop.interactable = true;
                HudIconButtonView.Apply(watchStop,HudButtonIcon.Stop,HudButtonRole.Stop,"STOP ARIA","توقف آریا");
                var hand = new GameObject("HolographicFinger", typeof(RectTransform), typeof(CanvasRenderer), typeof(AriaHolographicFingerGraphic));
                hand.transform.SetParent(watchOverlay.transform, false);
                watchFinger = hand.GetComponent<AriaHolographicFingerGraphic>();
                watchFinger.raycastTarget = false;
            }
            if (watchOverlay == null) return;
            watchOverlay.SetActive(visible);
            if (!visible) return;
            watchFinger.gameObject.SetActive(!suppressFinger);
            // The panel already has Stop. Keep the floating copy only when a modal
            // covers it, so it cannot obscure mission counters during normal play.
            var panelStop = _embeddedTutorialView.WatchButton;
            var panelStopRect = (RectTransform)panelStop.transform;
            Vector2 panelStopPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(panelStop),
                panelStopRect.TransformPoint(panelStopRect.rect.center));
            watchStop.gameObject.SetActive(!panelStop.IsActive() ||
                !WatchTargetIsReachable(panelStopPoint, panelStop.GetEntityId().GetHashCode(), false));
            // Support supplies the same modal stop affordance as Build. Do not
            // cover its catalog with a second floating Stop while that button is reachable.
            var supportStop=SupportPopupView.Active?.AriaStopButton;
            if(supportStop!=null && supportStop.IsActive() && supportStop.IsInteractable())
            {
                var supportRect=(RectTransform)supportStop.transform;
                var supportPoint=RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(supportStop),supportRect.TransformPoint(supportRect.rect.center));
                if(WatchTargetIsReachable(supportPoint,supportStop.GetEntityId().GetHashCode(),false))watchStop.gameObject.SetActive(false);
            }
            if (watchOperations != null && watchOperations.AriaButton is Button operationStop &&
                operationStop.IsActive() && operationStop.IsInteractable())
            {
                var operationRect = (RectTransform)operationStop.transform;
                var operationPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(operationStop),
                    operationRect.TransformPoint(operationRect.rect.center));
                if (WatchTargetIsReachable(operationPoint, operationStop.GetEntityId().GetHashCode(), false))
                    watchStop.gameObject.SetActive(false);
            }
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (watchStopLocale != locale)
            {
                watchStopLocale = locale;
                UiLocalizedText.Set(watchStop.GetComponentInChildren<TMPro.TMP_Text>(true), locale.StartsWith("fa") ? "توقف آریا" : "Stop ARIA");
            }
            var safe = Screen.safeArea;
            var stopRect = (RectTransform)watchStop.transform;
            stopRect.anchorMin = stopRect.anchorMax = stopRect.pivot = new Vector2(.5f, 1f);
            stopRect.sizeDelta = new Vector2(Mathf.Max(240, Screen.width * .14f), Mathf.Max(72, Screen.height * .065f));
            stopRect.position = new Vector3(safe.center.x, safe.yMax - Mathf.Max(84, Screen.height * .13f));
            if (watchBuild != null && watchBuild.TryGetAriaStopBounds(out Rect headerStop))
            {
                stopRect.sizeDelta = new Vector2(Mathf.Max(240,headerStop.width),Mathf.Max(72,headerStop.height));
                stopRect.position = new Vector3(headerStop.center.x, headerStop.yMax);
            }
            watchFinger.Present(state);
        }

        private void DisposeWatch(bool stop = true)
        {
            if (stop) UiShellRuntimeGateway.StopAriaPlay();
            DestroyObject(watchOverlay); watchOverlay = null; watchFinger = null; watchStop = null; watchStopLocale = null;
        }
    }
}
