using System.Collections.Generic;
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
            bool skirmish = UiShellRuntimeGateway.TryReadSkirmish(out var skirmishModel);
            bool supported = UiShellRuntimeGateway.ReadAriaPlayCapability() != AriaPlayCapability.None;
            bool available = supported && (skirmish ? !skirmishModel.Finished && !skirmishModel.StartupFailed : UsesNextTutorialAction && _lastPanelModel.HasRecommendation);
            bool supplyCanReserve=_lastPanelModel.TutorialStepCount==4 &&
                UiShellRuntimeGateway.TryReadSupplyLine(out _,out _,out bool canReserve) && canReserve;
            bool supplyWaiting=_lastPanelModel.TutorialStepCount==4 && UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var supplyTarget) &&
                (supplyTarget.BattleAction==UiTutorialBattleAction.Watch || supplyTarget.Moving) &&
                !supplyCanReserve;
            if (available && !skirmish && supplyWaiting)kind=AriaPlayObservationKind.Waiting;
            // Supply Line's final decision is an explicit HUD action. Observe the
            // visible control directly once it becomes legal instead of depending
            // on the preceding defend-area cue being replaced in the same frame.
            // Otherwise Watch can remain on the completed defense step until the
            // deadline even though the reserve button is enabled for the player.
            if(available && !skirmish && supplyCanReserve)
            {
                var hud=Object.FindAnyObjectByType<MissionDefenseHudView>();
                var reserve=ObserveWatchButton(hud?.SupplyReserveButton);
                if(reserve.Available){kind=AriaPlayObservationKind.Control;target=reserve.Id;position=reserve.Position;}
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
                if (!_tutorialCinematicSuspended && kind == AriaPlayObservationKind.Waiting &&
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
            if (!skirmish && _finalTutorialSuppressed) kind = AriaPlayObservationKind.Finished;
            int goalId=_lastPanelModel.TutorialStepCount * 100 + _lastPanelModel.TutorialStep;
            // A delivered load is visible reserve progress, even while the same
            // defend-storage instruction remains on screen.
            if(_lastPanelModel.TutorialStepCount==4 && UiShellRuntimeGateway.TryReadSupplyLine(out int reserveFuel,out _,out _))
                goalId=7600+_lastPanelModel.TutorialStep*8+Mathf.Clamp(reserveFuel/8,0,6);
            UiShellRuntimeGateway.PublishAriaObservation(new AriaPlayObservation(kind, target,
                goalId, position, Time.frameCount, Time.unscaledTime,drag,dragEnd));
            var state = UiShellRuntimeGateway.ReadAriaPlay();
            _embeddedTutorialView.PresentWatch(state, available);
            _embeddedTutorialView.RefreshContentLayout();
            RenderWatchFinger(state);
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

        private void RenderWatchFinger(AriaPlayModel state)
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
                var hand = new GameObject("HolographicFinger", typeof(RectTransform), typeof(CanvasRenderer), typeof(AriaHolographicFingerGraphic));
                hand.transform.SetParent(watchOverlay.transform, false);
                watchFinger = hand.GetComponent<AriaHolographicFingerGraphic>();
                watchFinger.raycastTarget = false;
            }
            if (watchOverlay == null) return;
            watchOverlay.SetActive(visible);
            if (!visible) return;
            // The panel already has Stop. Keep the floating copy only when a modal
            // covers it, so it cannot obscure mission counters during normal play.
            var panelStop = _embeddedTutorialView.WatchButton;
            var panelStopRect = (RectTransform)panelStop.transform;
            Vector2 panelStopPoint = RectTransformUtility.WorldToScreenPoint(ResolveEventCamera(panelStop),
                panelStopRect.TransformPoint(panelStopRect.rect.center));
            watchStop.gameObject.SetActive(!panelStop.IsActive() ||
                !WatchTargetIsReachable(panelStopPoint, panelStop.GetEntityId().GetHashCode(), false));
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
            stopRect.sizeDelta = new Vector2(Mathf.Max(180, Screen.width * .14f), Mathf.Max(60, Screen.height * .065f));
            stopRect.position = new Vector3(safe.center.x, safe.yMax - Mathf.Max(84, Screen.height * .13f));
            if (watchBuild != null && watchBuild.TryGetAriaStopBounds(out Rect headerStop))
            {
                stopRect.sizeDelta = headerStop.size;
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
