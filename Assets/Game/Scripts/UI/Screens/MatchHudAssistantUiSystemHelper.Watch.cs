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
            bool supplyWaiting=_lastPanelModel.TutorialStepCount==4 && UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var supplyTarget) &&
                (supplyTarget.BattleAction==UiTutorialBattleAction.Watch || supplyTarget.Moving) &&
                !(UiShellRuntimeGateway.TryReadSupplyLine(out _,out _,out bool canReserve) && canReserve);
            if (available && !skirmish && supplyWaiting)kind=AriaPlayObservationKind.Waiting;
            if (available && !skirmish && !supplyWaiting)
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
            int goal = 300;
            int site = -1;
            for (int i = 0; i < 3; i++)
                if (model.SiteCompleted == null || i >= model.SiteCompleted.Length || !model.SiteCompleted[i])
                { site = i; goal = 100 + i; break; }
            if (site < 0 && !model.EvidenceCarried) goal = 200;
            if (model.Finished) return new AriaPlayObservation(AriaPlayObservationKind.Finished, 0, goal,
                default, Time.frameCount, now);
            var state = UiShellRuntimeGateway.ReadAriaPlay();
            if (state.Actions != operationsWatchActions)
            {
                if (operationsLastWasTravel) operationsTravelUntil = now + 120f;
                operationsWatchActions = state.Actions;
            }
            if (!state.Active) operationsTravelUntil = 0;
            if (watchOperations == null || watchOperations.ScanButton(0) == null)
            {
                foreach (var candidate in Object.FindObjectsByType<OperationsMissionScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (candidate.ScanButton(0) != null) { watchOperations = candidate; break; }
            }
            if (watchOperations == null) return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal,
                default, Time.frameCount, now);
            Button target = null;
            bool travel = false;
            if (watchSquads == null) watchSquads = Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (watchSquads == null || (int)watchSquads.VisibleSelectedSlot <= 0)
                target = watchSquads?.VisibleCardButton(0);
            else if (site >= 0)
            {
                if (model.SiteProgress != null && site < model.SiteProgress.Length && model.SiteProgress[site] > 0)
                    return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
                if (model.CanScanSite != null && site < model.CanScanSite.Length && model.CanScanSite[site])
                    target = watchOperations.ScanButton(site);
                else if (now >= operationsTravelUntil)
                { target = watchOperations.AdvanceButton(site); travel = true; }
            }
            else if (!model.EvidenceCarried)
            {
                // Reissuing recovery resets its 15-second channel. Wait for the
                // visible progress before considering another tap.
                if (model.EvidenceProgress > 0)
                    return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal, default, Time.frameCount, now);
                if (model.CanRecoverHere) target = watchOperations.RecoverButton;
                else if (now >= operationsTravelUntil)
                { target = watchOperations.AdvanceButton(3); travel = true; }
            }
            else if (now >= operationsTravelUntil)
            { target = watchOperations.AdvanceButton(4); travel = true; }
            if (target == null) return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal,
                default, Time.frameCount, now);
            var observed = ObserveWatchButton(target);
            if (!observed.Available && travel)
            {
                target = watchOperations.FocusButton(site >= 0 ? site : model.EvidenceCarried ? 4 : 3);
                observed = ObserveWatchButton(target);
                travel = false;
            }
            if (!observed.Available && !watchOperations.GuideOpen && target != watchSquads?.VisibleCardButton(0))
            {
                target = watchOperations.GuideButton;
                observed = ObserveWatchButton(target);
                travel = false;
            }
            if (!observed.Available) return new AriaPlayObservation(AriaPlayObservationKind.Waiting, 0, goal,
                default, Time.frameCount, now);
            operationsLastWasTravel = travel;
            return new AriaPlayObservation(AriaPlayObservationKind.Control, observed.Id, goal,
                observed.Position, Time.frameCount, now);
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
            // A label or panel graphic must not hide an interactable control.
            // The control is reachable when no other interactable button is in front of it.
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
            watchFinger.Present(state);
        }

        private void DisposeWatch(bool stop = true)
        {
            if (stop) UiShellRuntimeGateway.StopAriaPlay();
            DestroyObject(watchOverlay); watchOverlay = null; watchFinger = null; watchStop = null; watchStopLocale = null;
        }
    }
}
