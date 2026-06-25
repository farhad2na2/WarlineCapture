using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeCitySpawnSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnCommandBoundary RuntimeSpawnCommandBoundary;
        public readonly BuildingRuntimeSpawnCommandBoundary.Context RuntimeSpawnCommandContext;
        public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
        public readonly BuildingRuntimeBoundaryProcessingCompositionSystemHelper RuntimeBoundarySystem;
        public readonly BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Context(
            BuildingRuntimeSpawnCommandBoundary runtimeSpawnCommandBoundary,
            BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeBoundaryProcessingCompositionSystemHelper runtimeBoundarySystem,
            BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            Func<int, bool> deleteBuildingById,
            Action beginDeferredRuntimeBuildingSideEffects,
            Action endDeferredRuntimeBuildingSideEffects)
        {
            RuntimeSpawnCommandBoundary = runtimeSpawnCommandBoundary;
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
                out actualFootprint,
                out bool attemptedRequest))
        {
            return true;
        }
        if (attemptedRequest)
            return false;

        BuildingRuntimeSpawnSystem runtimeSpawnSystem = context.RuntimeSpawnCommandContext.RuntimeSpawnSystem;
        if (runtimeSpawnSystem == null ||
            !runtimeSpawnSystem.TrySpawnRuntimeBuilding(
                context.RuntimeSpawnCommandContext.SpawnContext,
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

    private static bool TrySpawnRuntimeBuildingViaRequest(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        out bool attemptedRequest)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        attemptedRequest = false;
        if (prefab == null ||
            context.RuntimeSpawnCommandBoundary == null ||
            context.RuntimeBoundarySystem == null ||
            context.DefinitionSystem == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out _) ||
            !BuildingRuntimeSpawnCommandBoundary.TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
        {
            return false;
        }

        string buildingIdKey = BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(prefab);
        if (!context.RuntimeSpawnCommandBoundary.TryEnqueueRuntimeBuildingSpawnRequest(
                em,
                buildingIdKey,
                preferredOrigin,
                FactionIdentity.NeutralFactionId,
                out int requestId,
                rotateVertical: false,
                hasOwnerFaction: false))
        {
            return false;
        }

        attemptedRequest = true;
        context.RuntimeBoundarySystem.ProcessRuntimeSpawnRequestsForBoundary(
            context.DefinitionSystem,
            context.RuntimeSpawnCommandContext.RuntimeSpawnSystem,
            context.RuntimeSpawnCommandContext.SpawnContext,
            em,
            boundaryEntity);

        if (!context.RuntimeSpawnCommandBoundary.TryGetRuntimeSpawnRequestResult(
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
