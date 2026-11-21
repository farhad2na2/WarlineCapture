using System;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class SelectionScreenMarkerUiSystemHelper
    {
        // The HUD's static marker preview is editor artwork. Screen notifications
        // keep their input positions while live markers render at world positions.
        internal static bool ShowHudMarkerPreview(bool worldMarkersVisible) => false;

        public event Action<Vector2> MoveOrderScreenMarkerRequested;
        public event Action<Vector2> AttackOrderScreenMarkerRequested;
        public event Action OrderScreenMarkersHideRequested;

        public void RequestMoveOrderMarker(Vector2 screenPosition)
        {
            MoveOrderScreenMarkerRequested?.Invoke(screenPosition);
        }

        public void RequestAttackOrderMarker(Vector2 screenPosition)
        {
            AttackOrderScreenMarkerRequested?.Invoke(screenPosition);
        }

        public void RequestHideOrderMarkers()
        {
            OrderScreenMarkersHideRequested?.Invoke();
        }
    }
}
