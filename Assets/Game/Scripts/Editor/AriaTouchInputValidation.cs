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
                // UIBehaviour callbacks are not invoked automatically for Edit-mode fixtures.
                // Balance manual lifecycle calls explicitly during cleanup below.
                typeof(InputSystemUIInputModule).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(module, null);
                module.AssignDefaultActions();
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
                driver.Tick(10.6f); PumpInput(); module.Process();
                hand.Present(new AriaPlayModel(AriaPlayPhase.Touching, driver.ContactPosition, true, 0));
                InvokeFixtureLifecycle(hand, "LateUpdate");
                Require(Vector2.Distance(hand.rectTransform.position, driver.ContactPosition) < .01f, "hand fingertip tracks drag without lag");
                Require(driver.IsPressed && Vector2.Distance(driver.ContactPosition, point + Vector2.right * 40) < .1f, "drag contact follows accepted event");
                driver.Stop(); PumpInput(); module.Process();
                Require(clicks == 1, "cancelled drag does not click");
                string result = "[AriaTouchInputValidation] result=Passed cases=22";
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
                if (previousEvents != null) EventSystem.current = previousEvents;
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.ClosePreviewScene(scene);
                Require(ReferenceEquals(EventSystem.current, previousEvents), "fixture restores EventSystem registration");
            }
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
    public sealed class AriaInputFixtureClicks : MonoBehaviour, IPointerClickHandler
    {
        public string Trace;
        public void OnPointerClick(PointerEventData data)
        { Trace += data.pointerId + ":" + data.button + ";"; }
    }
}
#endif
