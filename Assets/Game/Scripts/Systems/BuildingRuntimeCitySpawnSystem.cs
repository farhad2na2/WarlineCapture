using System;
using UnityEngine;

internal sealed class BuildingRuntimeCitySpawnSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnCommandSystem RuntimeSpawnCommandSystem;
        public readonly BuildingRuntimeSpawnCommandSystem.Context RuntimeSpawnCommandContext;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Context(
            BuildingRuntimeSpawnCommandSystem runtimeSpawnCommandSystem,
            BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext,
            Func<int, bool> deleteBuildingById,
            Action beginDeferredRuntimeBuildingSideEffects,
            Action endDeferredRuntimeBuildingSideEffects)
        {
            RuntimeSpawnCommandSystem = runtimeSpawnCommandSystem;
            RuntimeSpawnCommandContext = runtimeSpawnCommandContext;
            DeleteBuildingById = deleteBuildingById;
            BeginDeferredRuntimeBuildingSideEffects = beginDeferredRuntimeBuildingSideEffects;
            EndDeferredRuntimeBuildingSideEffects = endDeferredRuntimeBuildingSideEffects;
        }
    }

    public void BeginDeferredSideEffects(Context context)
    {
        context.BeginDeferredRuntimeBuildingSideEffects?.Invoke();
    }

    public void EndDeferredSideEffects(Context context)
    {
        context.EndDeferredRuntimeBuildingSideEffects?.Invoke();
    }

    public bool DeleteBuildingById(Context context, int buildingId)
    {
        return context.DeleteBuildingById?.Invoke(buildingId) == true;
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        string fallbackDisplayName,
        string fallbackDescription,
        Vector2Int? fallbackFootprint,
        int fallbackMaxHealth)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;

        if (context.RuntimeSpawnCommandSystem == null ||
            !context.RuntimeSpawnCommandSystem.TrySpawnRuntimeBuilding(
                context.RuntimeSpawnCommandContext,
                prefab,
                preferredOrigin,
                out buildingId,
                out actualOrigin,
                out actualFootprint,
                fallbackDisplayName,
                fallbackDescription,
                fallbackFootprint,
                fallbackMaxHealth,
                isCityGenerated: true,
                ownerFactionId: null,
                rotateVertical: false))
        {
            return false;
        }

        return true;
    }
}
