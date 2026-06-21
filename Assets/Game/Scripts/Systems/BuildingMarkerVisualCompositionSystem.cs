using UnityEngine;

internal sealed class BuildingMarkerVisualCompositionSystem
{
    private MaterialPropertyBlock _markerPropertyBlock;

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
