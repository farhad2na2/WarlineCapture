using UnityEngine;

public enum MapBakeGroupRole
{
    IgnoredDecoration = 0,
    Terrain = 1,
    Road = 2,
    Bridge = 3,
    Ramp = 4,
    Blocker = 5
}

[DisallowMultipleComponent]
public sealed class MapBakeGroupAuthoring : MonoBehaviour
{
    [SerializeField] private MapBakeGroupRole role = MapBakeGroupRole.IgnoredDecoration;
    [SerializeField, Min(0)] private int layerId;
    [SerializeField] private MapSurfaceMovementMask movementMask = MapSurfaceMovementMask.AllGroundUnits;
    [SerializeField] private bool includeInactiveChildren = true;

    public MapBakeGroupRole Role => role;
    public int LayerId => layerId;
    public MapSurfaceMovementMask MovementMask => movementMask;
    public bool IncludeInactiveChildren => includeInactiveChildren;
}
