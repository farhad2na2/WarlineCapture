using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingRuntimeFocusPositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static Vector3 Resolve(
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource,
        RuntimeBuildingEntity building)
    {
        if (runtimeSource.TryResolveBuildingFocusWorldPosition != null &&
            runtimeSource.TryResolveBuildingFocusWorldPosition(building, out Vector3 worldPosition))
            return worldPosition;

        return building != null && building.Instance != null
            ? building.Instance.transform.position
            : Vector3.zero;
    }
}
