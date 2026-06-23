using UnityEngine;

internal static class BuildingRuntimeFocusPositionPresentationSystemHelper
{
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
