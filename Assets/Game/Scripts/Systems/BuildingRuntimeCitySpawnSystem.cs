using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeCitySpawnSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnCommandSystem RuntimeSpawnCommandSystem;
        public readonly BuildingRuntimeSpawnCommandSystem.Context RuntimeSpawnCommandContext;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingRuntimeBoundarySystem RuntimeBoundarySystem;
        public readonly BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Context(
            BuildingRuntimeSpawnCommandSystem runtimeSpawnCommandSystem,
            BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext,
            BuildingDefinitionSystem definitionSystem,
            BuildingRuntimeBoundarySystem runtimeBoundarySystem,
            BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Func<int, bool> deleteBuildingById,
            Action beginDeferredRuntimeBuildingSideEffects,
            Action endDeferredRuntimeBuildingSideEffects)
        {
            RuntimeSpawnCommandSystem = runtimeSpawnCommandSystem;
            RuntimeSpawnCommandContext = runtimeSpawnCommandContext;
            DefinitionSystem = definitionSystem;
            RuntimeBoundarySystem = runtimeBoundarySystem;
            TryGetEntityManager = tryGetEntityManager;
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

        if (TrySpawnRuntimeBuildingViaRequest(
                context,
                prefab,
                preferredOrigin,
                out buildingId,
                out actualOrigin,
                out actualFootprint))
        {
            return true;
        }

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

    private static bool TrySpawnRuntimeBuildingViaRequest(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        if (prefab == null ||
            context.RuntimeSpawnCommandSystem == null ||
            context.RuntimeBoundarySystem == null ||
            context.DefinitionSystem == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out _) ||
            !BuildingRuntimeSpawnCommandSystem.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
        {
            return false;
        }

        string buildingIdKey = BuildingDefinitionSystem.GetSpawnableLookupKey(prefab);
        if (!context.RuntimeSpawnCommandSystem.TryEnqueueRuntimeBuildingSpawnRequest(
                em,
                buildingIdKey,
                preferredOrigin,
                FactionIdentitySystem.NeutralFactionId,
                out int requestId))
        {
            return false;
        }

        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingRuntimeSpawnRequest request = requests[i];
            if (request.RequestId != requestId)
                continue;

            request.HasOwnerFaction = 0;
            requests[i] = request;
            break;
        }

        context.RuntimeBoundarySystem.ProcessRuntimeSpawnRequestsForBoundary(
            context.DefinitionSystem,
            context.RuntimeSpawnCommandContext.RuntimeSpawnSystem,
            context.RuntimeSpawnCommandContext.SpawnContext,
            em,
            boundaryEntity);

        if (!context.RuntimeSpawnCommandSystem.TryGetRuntimeSpawnRequestResult(
                em,
                requestId,
                out BuildingRuntimeSpawnRequest result) ||
            result.Status != BuildingRuntimeSpawnRequest.Succeeded)
        {
            return false;
        }

        buildingId = result.BuildingRuntimeId;
        actualOrigin = new Vector2Int(result.ActualOrigin.x, result.ActualOrigin.y);
        actualFootprint = new Vector2Int(result.ActualFootprint.x, result.ActualFootprint.y);
        return buildingId != 0 && actualFootprint.x > 0 && actualFootprint.y > 0;
    }
}
