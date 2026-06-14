using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeSpawnCommandSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnSystem RuntimeSpawnSystem;
        public readonly BuildingRuntimeSpawnSystem.Context SpawnContext;

        public Context(
            BuildingRuntimeSpawnSystem runtimeSpawnSystem,
            BuildingRuntimeSpawnSystem.Context spawnContext)
        {
            RuntimeSpawnSystem = runtimeSpawnSystem;
            SpawnContext = spawnContext;
        }
    }

    public bool TryEnqueueRuntimeBuildingSpawnRequest(
        EntityManager em,
        string buildingId,
        Vector2Int preferredOrigin,
        byte factionId,
        out int requestId,
        bool rotateVertical = false,
        bool hasOwnerFaction = true)
    {
        requestId = 0;
        string normalizedBuildingId = BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId);
        if (string.IsNullOrEmpty(normalizedBuildingId) ||
            !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
        {
            return false;
        }

        requestId = EnqueueRuntimeSpawnRequest(
            em,
            boundaryEntity,
            BuildingRuntimeSpawnRequest.KindBuilding,
            normalizedBuildingId,
            preferredOrigin,
            default,
            factionId,
            hasOwnerFaction,
            rotateVertical,
            allowExistingWallOverlap: false);
        return true;
    }

    public bool TryEnqueueRuntimeWallRunSpawnRequest(
        EntityManager em,
        string wallId,
        Vector2Int startOrigin,
        Vector2Int endOrigin,
        byte factionId,
        out int requestId)
    {
        requestId = 0;
        string normalizedWallId = BuildingDefinitionSystem.NormalizeSpawnableKey(wallId);
        if (string.IsNullOrEmpty(normalizedWallId) ||
            !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
        {
            return false;
        }

        requestId = EnqueueRuntimeSpawnRequest(
            em,
            boundaryEntity,
            BuildingRuntimeSpawnRequest.KindWallRun,
            normalizedWallId,
            startOrigin,
            endOrigin,
            factionId,
            hasOwnerFaction: true,
            rotateVertical: false,
            allowExistingWallOverlap: false);
        return true;
    }

    public bool TryEnqueueRuntimeWallSegmentSpawnRequest(
        EntityManager em,
        string wallId,
        Vector2Int origin,
        bool rotateVertical,
        byte factionId,
        bool allowExistingWallOverlap,
        out int requestId)
    {
        requestId = 0;
        string normalizedWallId = BuildingDefinitionSystem.NormalizeSpawnableKey(wallId);
        if (string.IsNullOrEmpty(normalizedWallId) ||
            !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
        {
            return false;
        }

        requestId = EnqueueRuntimeSpawnRequest(
            em,
            boundaryEntity,
            BuildingRuntimeSpawnRequest.KindWallSegment,
            normalizedWallId,
            origin,
            default,
            factionId,
            hasOwnerFaction: true,
            rotateVertical,
            allowExistingWallOverlap);
        return true;
    }

    public bool TryGetRuntimeSpawnRequestResult(
        EntityManager em,
        int requestId,
        out BuildingRuntimeSpawnRequest result)
    {
        result = default;
        if (requestId <= 0 || !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
            !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
        {
            return false;
        }

        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].RequestId != requestId)
                continue;

            result = requests[i];
            return true;
        }

        return false;
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        return TrySpawnRuntimeBuilding(
            context,
            prefab,
            preferredOrigin,
            out buildingId,
            out _,
            out _,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint,
            fallbackMaxHealth,
            isCityGenerated,
            ownerFactionId,
            rotateVertical);
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        if (context.RuntimeSpawnSystem == null ||
            !context.RuntimeSpawnSystem.TrySpawnRuntimeBuilding(
                context.SpawnContext,
                prefab,
                preferredOrigin,
                fallbackDisplayName,
                fallbackDescription,
                fallbackFootprint,
                fallbackMaxHealth,
                isCityGenerated,
                ownerFactionId,
                rotateVertical,
                out BuildingRuntimeSpawnSystem.SpawnRuntimeBuildingResult result))
        {
            return false;
        }

        buildingId = result.BuildingId;
        actualOrigin = result.ActualOrigin;
        actualFootprint = result.ActualFootprint;
        return true;
    }

    public int TrySpawnRuntimeWallRun(Context context, GameObject prefab, Vector2Int startOrigin, Vector2Int endOrigin, byte? ownerFactionId)
    {
        return context.RuntimeSpawnSystem != null
            ? context.RuntimeSpawnSystem.TrySpawnRuntimeWallRun(context.SpawnContext, prefab, startOrigin, endOrigin, ownerFactionId)
            : 0;
    }

    public bool TryGetRuntimeWallSegmentFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TryGetRuntimeWallSegmentFootprint(context.SpawnContext, prefab, rotateVertical, out footprint);
    }

    public bool TrySpawnRuntimeWallSegment(Context context, GameObject prefab, Vector2Int origin, bool rotateVertical, byte? ownerFactionId, bool allowExistingWallOverlap)
    {
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TrySpawnRuntimeWallSegment(context.SpawnContext, prefab, origin, rotateVertical, ownerFactionId, allowExistingWallOverlap);
    }

    public bool TryGetRuntimeBuildingPlacementFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(context.SpawnContext, prefab, rotateVertical, out footprint);
    }

    internal static bool TryGetRuntimeBoundaryEntity(EntityManager em, out Entity boundaryEntity)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>());
        if (!query.IsEmptyIgnoreFilter)
        {
            boundaryEntity = query.GetSingletonEntity();
            return em.Exists(boundaryEntity);
        }

        boundaryEntity = Entity.Null;
        return false;
    }

    private static DynamicBuffer<BuildingRuntimeSpawnRequest> EnsureRuntimeSpawnRequestBuffer(
        EntityManager em,
        Entity boundaryEntity)
    {
        if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            em.AddBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);

        return em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
    }

    private static int EnqueueRuntimeSpawnRequest(
        EntityManager em,
        Entity boundaryEntity,
        byte requestKind,
        string normalizedBuildingId,
        Vector2Int preferredOrigin,
        Vector2Int endOrigin,
        byte factionId,
        bool hasOwnerFaction,
        bool rotateVertical,
        bool allowExistingWallOverlap)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests = EnsureRuntimeSpawnRequestBuffer(em, boundaryEntity);
        int requestId = NextRequestId(requests);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requestId,
            RequestKind = requestKind,
            FactionId = factionId,
            HasOwnerFaction = hasOwnerFaction ? (byte)1 : (byte)0,
            BuildingId = new FixedString128Bytes(normalizedBuildingId),
            PreferredOrigin = new int2(preferredOrigin.x, preferredOrigin.y),
            EndOrigin = new int2(endOrigin.x, endOrigin.y),
            RotateVertical = rotateVertical ? (byte)1 : (byte)0,
            AllowExistingWallOverlap = allowExistingWallOverlap ? (byte)1 : (byte)0,
            Status = BuildingRuntimeSpawnRequest.Pending
        });
        return requestId;
    }

    private static int NextRequestId(DynamicBuffer<BuildingRuntimeSpawnRequest> requests)
    {
        int nextRequestId = 0;
        for (int i = 0; i < requests.Length; i++)
            nextRequestId = Mathf.Max(nextRequestId, requests[i].RequestId);

        return nextRequestId + 1;
    }
}
