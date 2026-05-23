using System;
using UnityEngine;

internal sealed class BuildingRuntimeCitySpawnSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnSystem.Context RuntimeSpawnContext;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Context(
            BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
            Func<int, bool> deleteBuildingById,
            Action beginDeferredRuntimeBuildingSideEffects,
            Action endDeferredRuntimeBuildingSideEffects)
        {
            RuntimeSpawnContext = runtimeSpawnContext;
            DeleteBuildingById = deleteBuildingById;
            BeginDeferredRuntimeBuildingSideEffects = beginDeferredRuntimeBuildingSideEffects;
            EndDeferredRuntimeBuildingSideEffects = endDeferredRuntimeBuildingSideEffects;
        }
    }

    private readonly BuildingRuntimeSpawnSystem _runtimeSpawnSystem = new();

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

        if (!_runtimeSpawnSystem.TrySpawnRuntimeBuilding(
                context.RuntimeSpawnContext,
                prefab,
                preferredOrigin,
                fallbackDisplayName,
                fallbackDescription,
                fallbackFootprint,
                fallbackMaxHealth,
                isCityGenerated: true,
                ownerFactionId: null,
                rotateVertical: false,
                out BuildingRuntimeSpawnSystem.SpawnRuntimeBuildingResult result))
        {
            return false;
        }

        buildingId = result.BuildingId;
        actualOrigin = result.ActualOrigin;
        actualFootprint = result.ActualFootprint;
        return true;
    }
}
