using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime
{
    internal readonly struct GamePointerState
    {
        public GamePointerState(Vector2 position, bool isPressed, bool wasPressedThisFrame, bool wasReleasedThisFrame)
        {
            Position = position;
            IsPressed = isPressed;
            WasPressedThisFrame = wasPressedThisFrame;
            WasReleasedThisFrame = wasReleasedThisFrame;
        }

        public Vector2 Position { get; }
        public bool IsPressed { get; }
        public bool WasPressedThisFrame { get; }
        public bool WasReleasedThisFrame { get; }
    }

    internal static class GamePointerInput
    {
        public static bool TryGetPrimaryPointer(out GamePointerState pointer)
        {
            pointer = default;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                bool isPressed = touch.press.isPressed;
                bool wasPressedThisFrame = touch.press.wasPressedThisFrame;
                bool wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
                if (isPressed || wasPressedThisFrame || wasReleasedThisFrame)
                {
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
