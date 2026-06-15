using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingMarkerVisualCompositionSystem : SystemBase
{
    private MaterialPropertyBlock _markerPropertyBlock;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static MaterialPropertyBlock GetMarkerPropertyBlock(BuildingMarkerVisualCompositionSystem system)
    {
        return system != null
            ? system.GetMarkerPropertyBlock()
            : new MaterialPropertyBlock();
    }

    public MaterialPropertyBlock GetMarkerPropertyBlock()
    {
        _markerPropertyBlock ??= new MaterialPropertyBlock();
        return _markerPropertyBlock;
    }
}
