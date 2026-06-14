using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly struct InitialBaseWallRunRequestSystem
{
    public int Enqueue(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        byte factionId,
        string wallId,
        int2 startOrigin,
        int2 endOrigin,
        Vector2Int bottomWallFootprint,
        Vector2Int sideWallFootprint)
    {
        Vector2Int start = new(startOrigin.x, startOrigin.y);
        Vector2Int end = new(endOrigin.x, endOrigin.y);
        bool vertical = Mathf.Abs(end.y - start.y) > Mathf.Abs(end.x - start.x);
        if (vertical)
            end.x = start.x;
        else
            end.y = start.y;

        Vector2Int footprint = vertical ? sideWallFootprint : bottomWallFootprint;
        List<Vector2Int> origins = BuildingPlacementCommitSystem.BuildWallRunOrigins(start, end, footprint, vertical);
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            EnqueueInitialBuildingSpawnRequest(
                em,
                boundaryEntity,
                configEntity,
                factionId,
                wallId,
                new int2(origin.x, origin.y),
                vertical,
                BuildingRuntimeSpawnRequest.KindWallSegment);
        }

        return origins.Count;
    }

    private static void EnqueueInitialBuildingSpawnRequest(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        byte factionId,
        string buildingId,
        int2 origin,
        bool rotateVertical,
        byte requestKind)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            new InitialBuildingBoundarySystem().GetRuntimeSpawnRequests(em, boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requests.Length + 1,
            RequestKind = requestKind,
            FactionId = factionId,
            HasOwnerFaction = 1,
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = origin,
            EndOrigin = default,
            RotateVertical = rotateVertical ? (byte)1 : (byte)0,
            AllowExistingWallOverlap = 0,
            Status = BuildingRuntimeSpawnRequest.Pending,
            PlanEntity = configEntity,
            EntryIndex = 0
        });
    }
}
