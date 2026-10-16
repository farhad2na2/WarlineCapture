#if UNITY_EDITOR
using System;
using Game.UI.Runtime;
using Game.UI.Contracts;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class AriaTouchInputValidation
    {
        public static string Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run the isolated input fixture in Edit mode.");
            var previousScene = SceneManager.GetActiveScene();
            var previousEvents = EventSystem.current;
            // Preview scenes do not require saving an unrelated untitled Editor scene.
            var scene = EditorSceneManager.NewPreviewScene();
            AriaTouchInputUiSystemHelper driver = null;
            Mouse physical = null;
            Touchscreen physicalTouch = null;
            GameObject canvas = null, events = null;
            int clicks = 0;
            DefaultInputActions fixtureActions = null;
            var fixtureActionReferences = new System.Collections.Generic.List<InputActionReference>();
            var suspendedDevices = new System.Collections.Generic.List<InputDevice>();
            var oldSettings = InputSystem.settings;
            var fixtureSettings = UnityEngine.Object.Instantiate(oldSettings);
            try
            {
                // The isolated fixture owns its synthetic devices. Real Editor
                // input can arrive between Start and PumpInput and would correctly
                // interrupt the shipping driver, making this fixture nondeterministic.
                foreach (var device in InputSystem.devices)
                    if (device.enabled) { suspendedDevices.Add(device); InputSystem.DisableDevice(device); }
                InputSystem.settings = fixtureSettings;
                fixtureSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                // Keep this isolated Dynamic-update fixture independent of Editor
                // panel focus after returning from a real Play-mode session.
                fixtureSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                events = new GameObject("AriaInputFixtureEvents", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(events, scene);
                typeof(EventSystem).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(), null);
                EventSystem.current = events.GetComponent<EventSystem>();
                // Isolate pointer delivery from navigation actions cached during this Editor frame.
                events.GetComponent<EventSystem>().sendNavigationEvents = false;
                var module = events.GetComponent<InputSystemUIInputModule>();
                // Own the action asset/references instead of using the package's
                // shared default: its Dispose uses deferred Destroy in Edit mode.
                fixtureActions = new DefaultInputActions();
                InputActionReference Ref(InputAction action)
                {
                    var reference = InputActionReference.Create(action);
                    fixtureActionReferences.Add(reference);
                    return reference;
                }
                module.actionsAsset = fixtureActions.asset;
                module.cancel = Ref(fixtureActions.UI.Cancel);
                module.submit = Ref(fixtureActions.UI.Submit);
                module.move = Ref(fixtureActions.UI.Navigate);
                module.leftClick = Ref(fixtureActions.UI.Click);
                module.rightClick = Ref(fixtureActions.UI.RightClick);
                module.middleClick = Ref(fixtureActions.UI.MiddleClick);
                module.point = Ref(fixtureActions.UI.Point);
                module.scrollWheel = Ref(fixtureActions.UI.ScrollWheel);
                module.trackedDeviceOrientation = Ref(fixtureActions.UI.TrackedDeviceOrientation);
                module.trackedDevicePosition = Ref(fixtureActions.UI.TrackedDevicePosition);
                // UIBehaviour callbacks are not invoked automatically for Edit-mode fixtures.
                // Balance manual lifecycle calls explicitly during cleanup below.
                typeof(InputSystemUIInputModule).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(module, null);
                module.actionsAsset.Enable();
                canvas = new GameObject("AriaInputFixtureCanvas", typeof(Canvas), typeof(AriaInputFixtureRaycaster));
                SceneManager.MoveGameObjectToScene(canvas, scene);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var raycaster = canvas.GetComponent<AriaInputFixtureRaycaster>();
                InvokeFixtureLifecycle(raycaster, "OnEnable");
                var target = new GameObject("TouchTarget", typeof(RectTransform), typeof(Image), typeof(Button), typeof(AriaInputFixtureClicks));
                target.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)target.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                target.GetComponent<Button>().onClick.AddListener(() => clicks++);
                InvokeFixtureLifecycle(target.GetComponent<Image>(), "OnEnable");
                InvokeFixtureLifecycle(target.GetComponent<Button>(), "OnEnable");
                raycaster.Target = target;
                Canvas.ForceUpdateCanvases();
                Vector2 point = new(Screen.width * .5f, Screen.height * .5f);
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                Require(hits.Count > 0, "fixture must have a raycast target at " + point);
                driver = new AriaTouchInputUiSystemHelper();
                physical = InputSystem.AddDevice<Mouse>("AriaInputFixturePhysicalMouse");
                InputSystem.DisableDevice(physical);
                InputSystem.QueueStateEvent(physical, new MouseState { position = point }.WithButton(MouseButton.Left));
                Require(driver.Start(), "start");
                Require(driver.TryGesture(point, point, .15f, 0, 0), "tap enqueue");
                PumpInput(); module.Process();
                Require(clicks == 0 && driver.IsPressed, "press must not click; clicks=" + clicks + " pressed=" + driver.IsPressed + " running=" + driver.IsRunning + " samples=" + driver.AcceptedSamples + " interruption=" + driver.LastInterruption);
                Require(Vector2.Distance(driver.ContactPosition, point) < .01f, "contact alignment");
                driver.Tick(.2f); PumpInput(); module.Process();
                Require(clicks == 1 && !driver.IsBusy, "normal touch must click once; clicks=" + clicks + " samples=" + driver.AcceptedSamples + " busy=" + driver.IsBusy + " pointers=" + target.GetComponent<AriaInputFixtureClicks>().Trace);

                Require(driver.DispatchedGestures == 1 && driver.CompletedGestures == 1 && driver.AcceptedSamples == 2,
                    "completed tap has measured dispatch and two input samples");
                Require(driver.PhysicalInterventions == 0 && driver.UnexpectedSamples == 0, "clean tap counters");
                Require(driver.TryGesture(point, point, .15f, 0, 1), "cancel enqueue");
                PumpInput(); module.Process();
                driver.Stop(); PumpInput(); module.Process();
                Require(clicks == 1 && !driver.IsPressed && !driver.IsRunning, "cancel must not click");
                driver.Tick(5); PumpInput(); module.Process();
                Require(clicks == 1, "no delayed release after stop");
                Require(driver.CompletedGestures == 1, "cancel cannot count a completed gesture");

                Require(driver.Start(), "restart");
                Require(driver.TryGesture(point, point, .15f, 0, 6), "race enqueue");
                driver.Stop(); PumpInput(); module.Process();
                Require(clicks == 1, "queued press discarded on stop");

                InputSystem.EnableDevice(physical);
                Require(driver.Start(), "physical override start");
                InputSystem.QueueStateEvent(physical, new MouseState { position = point }.WithButton(MouseButton.Left));
                PumpInput(); module.Process();
                Require(driver.PlayerInterrupted && !driver.IsRunning, "physical press cancels");
                InputSystem.QueueStateEvent(physical, new MouseState { position = point });
                PumpInput(); module.Process();
                Require(clicks == 1, "handover gesture consumed");

                // A player can touch a different control during a queued ARIA press.
                // The first press only hands back; the next press must work normally.
                var second = new GameObject("ManualTarget", typeof(RectTransform), typeof(Image), typeof(Button));
                second.transform.SetParent(canvas.transform, false);
                InvokeFixtureLifecycle(second.GetComponent<Image>(), "OnEnable");
                InvokeFixtureLifecycle(second.GetComponent<Button>(), "OnEnable");
                int secondClicks = 0;
                second.GetComponent<Button>().onClick.AddListener(() => secondClicks++);
                Require(driver.Start(), "distinct-target handover start");
                Require(driver.TryGesture(point, point, .3f, 0, 7), "pending original control press");
                PumpInput(); module.Process();
                raycaster.Target = second;
                InputSystem.QueueStateEvent(physical, new MouseState { position = point }.WithButton(MouseButton.Left));
                PumpInput(); module.Process();
                InputSystem.QueueStateEvent(physical, new MouseState { position = point });
                PumpInput(); module.Process();
                Require(clicks == 1 && secondClicks == 0 && !driver.IsRunning, "handover activates neither control");
                InputSystem.QueueStateEvent(physical, new MouseState { position = point }.WithButton(MouseButton.Left));
                PumpInput(); module.Process();
                InputSystem.QueueStateEvent(physical, new MouseState { position = point });
                PumpInput(); module.Process();
                Require(secondClicks == 1, "next manual press works once");
                raycaster.Target = target;

                physicalTouch = InputSystem.AddDevice<Touchscreen>("AriaInputFixturePhysicalTouch");
                Require(driver.Start(), "touch override start");
                InputSystem.QueueStateEvent(physicalTouch, new TouchState { touchId = 91, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
                PumpInput(); module.Process();
                Require(driver.PlayerInterrupted && !driver.IsRunning, "physical finger cancels before delivery");
                InputSystem.QueueStateEvent(physicalTouch, new TouchState { touchId = 91, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
                PumpInput(); module.Process();
                Require(clicks == 1, "physical finger handover consumed");
                Require(driver.PhysicalInterventions == 3, "mouse and finger interventions persist across restarts");

                var handObject = new GameObject("AriaHandFixture", typeof(RectTransform), typeof(AriaHolographicFingerGraphic));
                handObject.transform.SetParent(canvas.transform, false);
                var hand = handObject.GetComponent<AriaHolographicFingerGraphic>();
                InvokeFixtureLifecycle(hand, "OnEnable");
                Require(hand.canvasRenderer != null, "hand must create its renderer");
                Require(!hand.raycastTarget, "hand must not intercept its own touches");
                Require(driver.Start(), "drag start");
                Require(driver.TryGesture(point, point + Vector2.right * 80, .2f, .8f, 10), "drag enqueue");
                Require(!driver.TryGesture(point, point, .1f, 0, 10), "overlapping gesture rejected");
                PumpInput(); module.Process();
                hand.Present(new AriaPlayModel(AriaPlayPhase.Touching, driver.ContactPosition, true, 0));
                InvokeFixtureLifecycle(hand, "LateUpdate");
                Require(Vector2.Distance(hand.rectTransform.position, driver.ContactPosition) < .01f, "hand fingertip matches press");
                PumpInput(); module.Process(); // Separate UI consumption opportunity before moving.
                driver.Tick(10.6f); PumpInput(); module.Process();
                PumpInput();module.Process();driver.Tick(10.6f);PumpInput();module.Process(); // Consume priming, then nominal midpoint.
                hand.Present(new AriaPlayModel(AriaPlayPhase.Touching, driver.ContactPosition, true, 0));
                InvokeFixtureLifecycle(hand, "LateUpdate");
                Require(Vector2.Distance(hand.rectTransform.position, driver.ContactPosition) < .01f, "hand fingertip tracks drag without lag");
                Require(driver.IsPressed && Vector2.Distance(driver.ContactPosition, point + Vector2.right * 40) < .1f, "drag contact follows accepted event");
                driver.Stop(); PumpInput(); module.Process();
                Require(clicks == 1, "cancelled drag does not click");
                RunCadenceCases(driver, module, target, physical, point, () => clicks);
                string result = "[AriaTouchInputValidation] result=Passed cases=28";
                Debug.Log(result);
                return result;
            }
            finally
            {
                driver?.Dispose();
                InputSystem.settings = oldSettings;
                foreach (var device in suspendedDevices)
                    if (device.added) InputSystem.EnableDevice(device);
                UnityEngine.Object.DestroyImmediate(fixtureSettings);
                if (physicalTouch != null && physicalTouch.added) InputSystem.RemoveDevice(physicalTouch);
                if (physical != null && physical.added) InputSystem.RemoveDevice(physical);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
                if (events != null)
                {
                    typeof(InputSystemUIInputModule).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<InputSystemUIInputModule>(), null);
                    typeof(EventSystem).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic).Invoke(events.GetComponent<EventSystem>(), null);
                    UnityEngine.Object.DestroyImmediate(events);
                }
                foreach (var reference in fixtureActionReferences)
                    if (reference != null) UnityEngine.Object.DestroyImmediate(reference);
                if (fixtureActions != null && fixtureActions.asset != null)
                {
                    fixtureActions.asset.Disable();
                    UnityEngine.Object.DestroyImmediate(fixtureActions.asset);
                }
                if (previousEvents != null) EventSystem.current = previousEvents;
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.ClosePreviewScene(scene);
                Require(ReferenceEquals(EventSystem.current, previousEvents), "fixture restores EventSystem registration");
            }
        }

        public static void RunTouchCadenceFocusedValidation()
        {
            Run();
            Debug.Log("[AriaTouchCadenceValidation] result=Passed cases=6 existingCases=22");
        }

        private static void RunCadenceCases(AriaTouchInputUiSystemHelper driver, InputSystemUIInputModule module,
            GameObject target, Mouse physical, Vector2 origin, Func<int> clicks)
        {
            // Match real parent drag / child control routing. Unity intentionally
            // permits a click when pointerPress and pointerDrag are the same object.
            var surface = new GameObject("TouchDragSurface", typeof(RectTransform), typeof(AriaInputFixtureDrag));
            surface.transform.SetParent(target.transform.parent, false);
            var surfaceRect = (RectTransform)surface.transform;
            surfaceRect.anchorMin = Vector2.zero; surfaceRect.anchorMax = Vector2.one;
            surfaceRect.offsetMin = surfaceRect.offsetMax = Vector2.zero;
            target.transform.SetParent(surface.transform, false);
            var drag = surface.GetComponent<AriaInputFixtureDrag>();
            var child = target.GetComponent<AriaInputFixtureClicks>();
            Vector2 destination = origin + Vector2.right * 80;
            // Each event is consumed by the real UI module. The explicit empty
            // Dynamic update corresponds to the next frame opportunity in Play.
            void Pump() { PumpInput(); module.Process(); }
            void Begin(float now, float travel)
            {
                if (!driver.IsRunning) Require(driver.Start(), "cadence start");
                Require(driver.TryGesture(origin, destination, .15f, travel, now), "cadence drag enqueue");
                Pump();
            }
            void FinalMove(float overdue)
            {
                uint accepted = driver.AcceptedSamples;
                driver.Tick(overdue); Pump();
                Require(driver.AcceptedSamples == accepted && driver.IsPressed, "same-consumption drag cannot advance");
                driver.Tick(overdue); Pump();
                if(Vector2.Distance(driver.ContactPosition,destination)>=.01f)
                {
                    Require(driver.IsPressed&&driver.IsBusy&&Vector2.Distance(driver.ContactPosition,origin)>.01f,
                        "overdue scroll must first expose a distinct intermediate move");
                    Pump();driver.Tick(overdue);Pump();
                }
                Require(driver.IsPressed && driver.IsBusy && Vector2.Distance(driver.ContactPosition, destination) < .01f,
                    "overdue drag must expose accepted final Moved before release");
            }
            for (int i = 0; i < 3; i++)
            {
                int beforeClicks = clicks(), began = drag.Began, ended = drag.Ended, up = child.Up;
                uint completed = driver.CompletedGestures;
                float now = 20 + i * 10;
                Begin(now, i == 1 ? 0 : .2f);
                if (i == 2)
                {
                    Pump();
                    driver.Tick(now + .25f); Pump();
                    Pump();driver.Tick(now + .25f);Pump();
                    Require(driver.IsPressed && Vector2.Distance(driver.ContactPosition, origin + Vector2.right * 40) < .1f,
                        "normal intermediate drag is retained");
                }
                FinalMove(now + 5);
                Require(drag.Began == began + 1 && drag.Dragged > 0 && drag.Ended == ended && child.Up == up + 1,
                    "real UI transfers child press to parent drag while touch remains held");
                uint accepted = driver.AcceptedSamples;
                driver.Tick(now + 5); Pump();
                Require(driver.IsPressed && driver.AcceptedSamples == accepted && drag.Ended == ended,
                    "final Moved survives its UI update without release");
                driver.Tick(now + 5); Pump();
                Require(!driver.IsBusy && driver.CompletedGestures == completed + 1 && drag.Ended == ended + 1 && child.Up == up + 1,
                    "next UI opportunity completes one real drag");
                Require(clicks() == beforeClicks, "drag cannot click its source control");
                Require(Vector2.Distance(drag.LastDragPosition, destination) < .01f, "UI received final drag destination");
            }

            int baselineClicks = clicks(); uint baselineCompleted = driver.CompletedGestures;
            Begin(60, .2f); FinalMove(65);
            driver.Stop(); Pump(); driver.Tick(66); Pump();
            Require(!driver.IsRunning && clicks() == baselineClicks && driver.CompletedGestures == baselineCompleted,
                "cancel after final Moved has no delayed release/click/completion");

            Begin(70, .2f); FinalMove(75);
            uint interventions = driver.PhysicalInterventions;
            InputSystem.QueueStateEvent(physical, new MouseState { position = origin }.WithButton(MouseButton.Left));
            Pump();
            InputSystem.QueueStateEvent(physical, new MouseState { position = origin }); Pump();
            driver.Tick(76); Pump();
            Require(driver.PlayerInterrupted && !driver.IsRunning && driver.PhysicalInterventions == interventions + 1 &&
                clicks() == baselineClicks && driver.CompletedGestures == baselineCompleted,
                "physical takeover after final Moved still cancels without click/completion");

            Require(driver.Start(), "sparse tap start");
            Require(driver.TryGesture(origin, origin, .15f, 0, 80), "sparse tap enqueue");
            Pump(); driver.Tick(85); Pump();
            Require(clicks() == baselineClicks + 1 && driver.CompletedGestures == baselineCompleted + 1 && !driver.IsBusy,
                "sparse stationary tap retains ordinary one-click timing");
            Require(driver.UnexpectedSamples == 0, "cadence changes cannot create unexpected touch samples");
            driver.Stop(); Pump();
            RunSparseScrollCases(driver,module,target,origin,Pump,clicks);
        }

        private static void RunSparseScrollCases(AriaTouchInputUiSystemHelper driver,InputSystemUIInputModule module,
            GameObject target,Vector2 origin,Action pump,Func<int> clicks)
        {
            var oldParent=target.transform.parent;
            var viewport=new GameObject("SparseScrollFixture",typeof(RectTransform),typeof(ScrollRect));
            viewport.transform.SetParent(oldParent,false);
            var rect=(RectTransform)viewport.transform;rect.sizeDelta=new Vector2(300,100);
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);
            var contentRect=(RectTransform)content.transform;contentRect.sizeDelta=new Vector2(900,100);
            var scroll=viewport.GetComponent<ScrollRect>();scroll.content=contentRect;scroll.viewport=rect;
            scroll.horizontal=true;scroll.vertical=false;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Unrestricted;
            target.transform.SetParent(content.transform,false);
            try
            {
                foreach(int sign in new[]{-1,1})
                {
                    int beforeClicks=clicks();Vector2 baseline=contentRect.anchoredPosition;
                    uint baselineUnexpected=driver.UnexpectedSamples,baselineInterventions=driver.PhysicalInterventions;
                    Require(driver.Start(),"sparse scroll driver start");
                    Require(driver.TryGesture(origin,origin+Vector2.right*(sign*160),.12f,.4f,100),"sparse scroll enqueue");
                    pump();
                    int steps=0;
                    while(driver.IsBusy&&steps++<10){pump();driver.Tick(105);pump();}
                    Require(!driver.IsBusy&&steps<=10,"sparse scroll contact bounded completion");
                    float actual=contentRect.anchoredPosition.x-baseline.x;
                    Require(actual*sign>100,"real ScrollRect must move under sparse native UI updates; delta="+actual);
                    Require(clicks()==beforeClicks,"sparse scroll cannot click its source child");
                    Require(driver.UnexpectedSamples==baselineUnexpected&&driver.PhysicalInterventions==baselineInterventions,"sparse scroll input ownership: unexpected="+baselineUnexpected+"->"+driver.UnexpectedSamples+" interventions="+baselineInterventions+"->"+driver.PhysicalInterventions);
                    driver.Stop();pump();
                }
                Debug.Log("[AriaSparseScroll] result=Passed controls=2 realScrollRect=1 directScrollWrites=0 missionCertified=0");
            }
            finally{driver.Stop();pump();target.transform.SetParent(oldParent,false);UnityEngine.Object.DestroyImmediate(viewport);}
        }

        private static void InvokeFixtureLifecycle(Component component, string method)
        {
            // These Edit-mode fixtures explicitly drive UI callbacks. SendMessage
            // goes through the native play-mode guard and logs ShouldRunBehaviour.
            component.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(component, null);
        }

        private static void PumpInput()
        {
            // Explicit player update in an Edit-mode fixture (package-internal test API).
            typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("ARIA input: " + message);
        }
    }

    // An Edit-mode canvas has no rendered graphic depths. This fixture supplies only
    // the raycast surface; the real InputSystemUIInputModule still owns all events.
    public sealed class AriaInputFixtureRaycaster : BaseRaycaster
    {
        public GameObject Target;
        public override Camera eventCamera => null;
        public override void Raycast(PointerEventData data, System.Collections.Generic.List<RaycastResult> results)
        {
            if (Target == null || !Target.activeInHierarchy) return;
            results.Add(new RaycastResult { gameObject = Target, module = this, distance = 0, index = results.Count, screenPosition = data.position });
        }
    }
    public sealed class AriaInputFixtureDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int Began, Dragged, Ended;
        public Vector2 LastDragPosition;
        public void OnBeginDrag(PointerEventData data) { Began++; }
        public void OnDrag(PointerEventData data) { Dragged++; LastDragPosition = data.position; }
        public void OnEndDrag(PointerEventData data) { Ended++; }
    }
    public sealed class AriaInputFixtureClicks : MonoBehaviour, IPointerClickHandler, IPointerUpHandler
    {
        public string Trace;
        public int Up;
        public void OnPointerUp(PointerEventData data) { Up++; }
        public void OnPointerClick(PointerEventData data)
        { Trace += data.pointerId + ":" + data.button + ";"; }
    }
}
#endif
