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

        private void TickWatch()
        {
            if (_embeddedTutorialView == null) return;
            var kind = AriaPlayObservationKind.Unavailable;
            int target = 0;
            Vector2 position = default;
            Vector2 dragEnd=default;bool drag=false;
            bool skirmish = UiShellRuntimeGateway.TryReadSkirmish(out var skirmishModel);
            bool supported = UiShellRuntimeGateway.ReadAriaPlayCapability() != AriaPlayCapability.None;
            bool available = supported && (skirmish ? !skirmishModel.Finished && !skirmishModel.StartupFailed : UsesNextTutorialAction && _lastPanelModel.HasRecommendation);
            if (available && !skirmish)
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
            UiShellRuntimeGateway.PublishAriaObservation(new AriaPlayObservation(kind, target,
                _lastPanelModel.TutorialStepCount * 100 + _lastPanelModel.TutorialStep, position, Time.frameCount, Time.unscaledTime,drag,dragEnd));
            var state = UiShellRuntimeGateway.ReadAriaPlay();
            _embeddedTutorialView.PresentWatch(state, available);
            _embeddedTutorialView.RefreshContentLayout();
            RenderWatchFinger(state);
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
            if (watchHits.Count == 0) return false;
            var button = watchHits[0].gameObject.GetComponentInParent<Button>();
            return button != null && button.GetEntityId().GetHashCode() == targetId;
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
