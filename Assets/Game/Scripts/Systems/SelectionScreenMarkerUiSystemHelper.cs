using System;
using UnityEngine;

public sealed class SelectionScreenMarkerUiSystemHelper
{
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
