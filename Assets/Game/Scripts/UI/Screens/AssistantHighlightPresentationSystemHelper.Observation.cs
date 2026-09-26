using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        // Observe only geometry already presented to the player, never mission entity targets.
        internal bool TryObserveVisibleGuide(out Vector2 position, out int identity, out bool world)
        {
            position = default; identity = 0; world = false;
            if (!HasVisibleDirectTutorialTarget) return false;
            if (_directTutorialTarget != null)
            {
                var button = _directTutorialTarget.GetComponent<Button>();
                if (button == null || !button.IsActive() || !button.IsInteractable()) return false;
                Canvas canvas = button.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var wedge = button.targetGraphic as V3RadialWedgeGraphic;
                Vector3 touchPoint = wedge != null ? wedge.GuidanceTouchPoint :
                    _directTutorialTarget.TransformPoint(_directTutorialTarget.rect.center);
                position = RectTransformUtility.WorldToScreenPoint(camera, touchPoint);
                identity = button.GetEntityId().GetHashCode();
                return true;
            }
            // A plain area ring is a wait/defend region, not a tap instruction.
            if (_worldCrosshairRenderers == null || _worldCrosshairRenderers.Length == 0 || !_worldCrosshairRenderers[0].enabled) return false;
            // LineRenderer bounds can lag one frame after a mission guidance step changes.
            // Observe the exact world point used to draw the currently visible direct cue so
            // ARIA never taps the previous objective while the ring is being rebuilt.
            Vector3 projected = _worldCamera.WorldToScreenPoint(_directTutorialWorldTarget);
            position = projected;
            identity = 1;
            world = true;
            return projected.z > 0;
        }
    }
}
