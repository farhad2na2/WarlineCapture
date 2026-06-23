using UnityEngine;

internal sealed class BuildingMarkerVisualPresentationSystemHelper
{
    private MaterialPropertyBlock _markerPropertyBlock;

    public static MaterialPropertyBlock GetMarkerPropertyBlock(BuildingMarkerVisualPresentationSystemHelper system)
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
