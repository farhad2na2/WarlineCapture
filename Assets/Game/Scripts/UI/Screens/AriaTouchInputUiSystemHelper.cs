using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Touch-only actuator. Has no gameplay, entity, camera or Button dependencies.
    /// The owner supplies one bounded gesture at a time and may cancel at any point.
    /// </summary>
    public sealed class AriaTouchInputUiSystemHelper : IDisposable
    {
        private Touchscreen screen;
        private InputDevice consumedDevice;
        private bool disposed, queued, activeGesture, pressed;
        private Vector2 origin, destination;
        private float startedAt, holdSeconds, travelSeconds;
        private int nextTouchId, touchId;
        private TouchPhase queuedPhase;

        public bool IsRunning => screen != null && screen.added;
        public bool IsBusy => activeGesture || queued;
        public bool IsPressed => pressed;
        public Vector2 ContactPosition { get; private set; }
        public uint Generation { get; private set; }
        public uint AcceptedSamples { get; private set; }
        public bool PlayerInterrupted { get; private set; }
        public byte LastInterruption { get; private set; }
        public int DeviceId => screen != null ? screen.deviceId : 0;

        public AriaTouchInputUiSystemHelper()
        {
            InputSystem.onEvent += OnInputEvent;
            Application.focusChanged += OnFocusChanged;
        }

        public bool Start()
        {
            if (disposed || IsRunning || AnyPhysicalButtonPressed()) return false;
            Generation++;
            PlayerInterrupted = false;
            LastInterruption = 0;
            consumedDevice = null;
            screen = InputSystem.AddDevice<Touchscreen>("AriaDemonstrationTouch");
            return true;
        }

        public bool TryGesture(Vector2 from, Vector2 to, float hold, float travel, float now)
        {
            if (!IsRunning || IsBusy || !Finite(from) || !Finite(to) ||
                !float.IsFinite(hold) || !float.IsFinite(travel) || hold < .06f || travel < 0 || hold + travel > 8f)
                return false;
            origin = from; destination = to;
            holdSeconds = hold; travelSeconds = travel; startedAt = now;
            touchId = ++nextTouchId;
            if (touchId <= 0) touchId = nextTouchId = 1;
            activeGesture = true;
            Queue(from, TouchPhase.Began);
            return true;
        }

        public void Tick(float now)
        {
            if (!IsRunning || !activeGesture || queued) return;
            float elapsed = now - startedAt;
            if (elapsed < holdSeconds) return;
            if (travelSeconds > 0 && elapsed < holdSeconds + travelSeconds)
            {
                float t = Mathf.Clamp01((elapsed - holdSeconds) / travelSeconds);
                Queue(Vector2.Lerp(origin, destination, t), TouchPhase.Moved);
            }
            else Queue(destination, TouchPhase.Ended);
        }

        public void Stop()
        {
            Generation++;
            activeGesture = queued = pressed = false;
            // A Canceled TouchState is interpreted as a release by some UI handlers.
            // Device removal uses InputSystemUIInputModule's stale-pointer purge, not Click.
            var retiring = screen;
            screen = null;
            if (retiring != null && retiring.added) InputSystem.RemoveDevice(retiring);
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
            InputSystem.onEvent -= OnInputEvent;
            Application.focusChanged -= OnFocusChanged;
            consumedDevice = null;
        }

        private void OnFocusChanged(bool focused)
        {
            if (!focused) { LastInterruption = 2; Stop(); }
        }

        private void Queue(Vector2 position, TouchPhase phase)
        {
            queued = true; queuedPhase = phase; pendingPosition = position;
            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = touchId, phase = phase, position = position,
                pressure = phase == TouchPhase.Ended ? 0 : 1
            });
        }

        private void OnInputEvent(InputEventPtr input, InputDevice device)
        {
            if (!input.IsA<StateEvent>() && !input.IsA<DeltaStateEvent>()) return;
            if (device == screen)
            {
                if (!IsRunning || !queued) { input.handled = true; return; }
                // The trajectory supplies the same point to the touch device and visual.
                ContactPosition = pendingPosition;
                AcceptedSamples++;
                pressed = queuedPhase != TouchPhase.Ended;
                if (!pressed) activeGesture = false;
                queued = false;
                return;
            }
            bool down = HasPressedButton(device, input);
            if (consumedDevice == device)
            {
                input.handled = true;
                if (!down) consumedDevice = null;
                return;
            }
            if (!IsRunning || !down) return;
            // Mouse movement alone is not intervention. A real button/contact is.
            PlayerInterrupted = true;
            LastInterruption = 7;
            consumedDevice = device;
            input.handled = true;
            Stop();
        }

        private Vector2 pendingPosition;

        private static bool HasPressedButton(InputDevice device, InputEventPtr input)
        {
            if (device is Touchscreen touch)
            {
                // Touchscreen maps individual TouchState events only through primaryTouch.
                if (touch.primaryTouch.press.ReadValueFromEvent(input, out float primary) && primary > .5f) return true;
                foreach (var contact in touch.touches)
                    if (contact.press.ReadValueFromEvent(input, out float value) && value > .5f) return true;
                return false;
            }
            foreach (var control in input.EnumerateChangedControls(device, 0))
                if (control is ButtonControl button && button.ReadValueFromEvent(input) > .5f) return true;
            return false;
        }

        private static bool AnyPhysicalButtonPressed()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is Touchscreen touch)
                {
                    foreach (var contact in touch.touches) if (contact.press.isPressed) return true;
                }
                else if (device is Mouse mouse && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) return true;
                else if (device is Keyboard keyboard && keyboard.anyKey.isPressed) return true;
            }
            return false;
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
