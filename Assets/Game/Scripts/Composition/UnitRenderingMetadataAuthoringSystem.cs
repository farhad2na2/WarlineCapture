using Unity.Entities;
using UnityEngine;

internal sealed partial class UnitRenderingMetadataAuthoringSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
