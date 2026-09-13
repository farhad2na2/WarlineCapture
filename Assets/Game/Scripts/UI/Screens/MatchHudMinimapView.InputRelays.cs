using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapView
    {
        private void EnsureZoomRelay(Button button, int direction)
        {
            if (button == null)
                return;

            MatchHudMinimapZoomPressRelay relay = button.GetComponent<MatchHudMinimapZoomPressRelay>();
            if (relay == null)
                relay = button.gameObject.AddComponent<MatchHudMinimapZoomPressRelay>();

            relay.Configure(this, direction);
        }

        private void EnsureViewportDragRelay()
        {
            if (viewportRect == null)
                return;

            Graphic graphic = viewportRect.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = viewportRect.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                graphic = image;
            }

            graphic.raycastTarget = true;

            MatchHudMinimapViewportDragRelayView relay = viewportRect.GetComponent<MatchHudMinimapViewportDragRelayView>();
            if (relay == null)
                relay = viewportRect.gameObject.AddComponent<MatchHudMinimapViewportDragRelayView>();

            relay.Configure(this);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera();
            if (GetComponent<RectMask2D>() != null &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPosition, eventCamera)) return false;
            RectTransform rect = MapRect;
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
                return true;

            return _allowZoom &&
                   (ContainsButton(zoomInButton, screenPosition, eventCamera) ||
                    ContainsButton(zoomOutButton, screenPosition, eventCamera));
        }

    }
}
