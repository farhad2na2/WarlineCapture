using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime
{
    internal readonly struct GamePointerState
    {
        public GamePointerState(Vector2 position, bool isPressed, bool wasPressedThisFrame, bool wasReleasedThisFrame, bool wasCanceled = false)
        {
            Position = position;
            IsPressed = isPressed;
            WasPressedThisFrame = wasPressedThisFrame;
            WasReleasedThisFrame = wasReleasedThisFrame;
            WasCanceled = wasCanceled;
        }

        public Vector2 Position { get; }
        public bool IsPressed { get; }
        public bool WasPressedThisFrame { get; }
        public bool WasReleasedThisFrame { get; }
        public bool WasCanceled { get; }
    }

    internal static class GamePointerInput
    {
        private static Touchscreen lastPressedScreen;
        private static int cancellationFrame = -1;
        private static Vector2 lastTouchPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTouchTracking()
        {
            lastPressedScreen = null;
            cancellationFrame = -1;
            lastTouchPosition = default;
        }

        public static bool TryGetPrimaryPointer(out GamePointerState pointer)
        {
            pointer = default;

            // Device removal is cancellation, never an implied world click/drag release.
            // All pointer consumers see the cancellation for the same frame.
            if (lastPressedScreen != null && !lastPressedScreen.added)
            {
                lastPressedScreen = null;
                cancellationFrame = Time.frameCount;
            }
            if (cancellationFrame == Time.frameCount)
            {
                pointer = new GamePointerState(lastTouchPosition, false, false, false, true);
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    lastPressedScreen = null;
                    cancellationFrame = Time.frameCount;
                    lastTouchPosition = touch.position.ReadValue();
                    pointer = new GamePointerState(lastTouchPosition, false, false, false, true);
                    return true;
                }
                bool isPressed = touch.press.isPressed;
                bool wasPressedThisFrame = touch.press.wasPressedThisFrame;
                bool wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
                if (isPressed || wasPressedThisFrame || wasReleasedThisFrame)
                {
                    lastPressedScreen = isPressed ? touchscreen : null;
                    lastTouchPosition = touch.position.ReadValue();
                    pointer = new GamePointerState(
                        touch.position.ReadValue(),
                        isPressed,
                        wasPressedThisFrame,
                        wasReleasedThisFrame);
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;

            pointer = new GamePointerState(
                mouse.position.ReadValue(),
                mouse.leftButton.isPressed,
                mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.wasReleasedThisFrame);
            return true;
        }

        public static bool TryGetPointerPosition(out Vector2 screenPosition)
        {
            if (TryGetPrimaryPointer(out GamePointerState pointer))
            {
                screenPosition = pointer.Position;
                return true;
            }

            screenPosition = default;
            return false;
        }

        public static bool IsPrimaryPointerPressed()
        {
            return TryGetPrimaryPointer(out GamePointerState pointer) && pointer.IsPressed;
        }
    }
}
