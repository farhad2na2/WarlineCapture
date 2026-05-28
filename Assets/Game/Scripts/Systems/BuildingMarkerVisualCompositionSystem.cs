using UnityEngine;

internal sealed class BuildingMarkerVisualCompositionSystem
{
    private MaterialPropertyBlock _markerPropertyBlock;

    public MaterialPropertyBlock GetMarkerPropertyBlock()
    {
        _markerPropertyBlock ??= new MaterialPropertyBlock();
        return _markerPropertyBlock;
    }
}
