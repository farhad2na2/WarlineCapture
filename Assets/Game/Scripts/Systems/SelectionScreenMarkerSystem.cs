using System;
using Unity.Entities;
using UnityEngine;

public sealed partial class SelectionScreenMarkerSystem : SystemBase
{
    public event Action<Vector2> MoveOrderScreenMarkerRequested;
    public event Action<Vector2> AttackOrderScreenMarkerRequested;
    public event Action OrderScreenMarkersHideRequested;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
