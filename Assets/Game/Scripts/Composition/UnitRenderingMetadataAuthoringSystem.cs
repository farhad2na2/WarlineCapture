using UnityEngine;

internal static class UnitRenderingMetadataAuthoringSystem
{
    public static bool TryGetUnitRenderingMetadata(GameObject prefab, out UnitRenderingMetadata metadata)
    {
        metadata = default;
        if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
            return false;

        metadata = new UnitRenderingMetadata(
            authoring.IsAirUnit,
            authoring.GetConfiguredFootprintCells(),
            authoring.AnimationOrder);
        return true;
    }
}
