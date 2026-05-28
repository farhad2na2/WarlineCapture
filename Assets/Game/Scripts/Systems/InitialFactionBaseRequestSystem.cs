using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public readonly struct InitialFactionBaseRequestSystem
{
    public bool Enqueue(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        InitialUnitsSpawnConfig config,
        GridConfig grid,
        NativeArray<InitialUnitsFactionSpawnEntry> factionSpawns,
        int initialBaseCoreRequestEntryIndex,
        out int requestCount)
    {
        requestCount = 0;
        if (boundaryEntity == Entity.Null)
            return false;

        var spawnableSystem = new InitialBuildingSpawnableSystem();
        if (!spawnableSystem.TryResolveSpawnableId(em, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Dirt_Straight", out string wallId, out BuildingConfiguredSpawnableReadModel wallModel) &&
            !spawnableSystem.TryResolveSpawnableId(em, boundaryEntity, config.BaseWallPrefabLookupKey, "Wall_Fence_Straight", out wallId, out wallModel))
            return false;
        if (!spawnableSystem.TryResolveSpawnableId(em, boundaryEntity, config.BaseGatePrefabLookupKey, "Building_Road_Barrier", out string gateId, out BuildingConfiguredSpawnableReadModel gateModel))
            return false;
        if (!spawnableSystem.TryResolveSpawnableId(em, boundaryEntity, config.BaseCoreBuildingPrefabLookupKey, "Building_Ammunition_Depot", out _, out _))
            return false;

        var placements = new List<InitialFactionBasePlacement>();
        InitialFactionBaseLayoutPlanner.BuildPlacements(
            config.BaseHalfWidthCells,
            config.BaseHalfHeightCells,
            placements);
        var placementIds = new Dictionary<string, string>();
        var placementModels = new Dictionary<string, BuildingConfiguredSpawnableReadModel>();
        for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
        {
            InitialFactionBasePlacement placement = placements[placementIndex];
            if (placement.Kind == InitialFactionBasePlacementKind.Gate ||
                placementIds.ContainsKey(placement.PrefabKey))
            {
                continue;
            }

            if (!spawnableSystem.TryResolveSpawnableId(em, boundaryEntity, new FixedString128Bytes(placement.PrefabKey), placement.PrefabKey, out string resolvedId, out BuildingConfiguredSpawnableReadModel resolvedModel))
            {
                if (placement.Kind == InitialFactionBasePlacementKind.CoreBuilding)
                    return false;

                continue;
            }

            placementIds.Add(placement.PrefabKey, resolvedId);
            placementModels.Add(placement.PrefabKey, resolvedModel);
        }

        Vector2Int bottomGateFootprint = ToFootprint(gateModel.FootprintCells, false);
        Vector2Int sideGateFootprint = ToFootprint(gateModel.FootprintCells, true);
        Vector2Int bottomWallFootprint = ToFootprint(wallModel.FootprintCells, false);
        Vector2Int sideWallFootprint = ToFootprint(wallModel.FootprintCells, true);
        int gateHalfGap = InitialFactionBaseLayoutPlanner.CalculateGateHalfGap(bottomGateFootprint, sideGateFootprint, bottomWallFootprint, sideWallFootprint);
        var wallRuns = new List<InitialFactionBaseWallRun>();
        InitialFactionBaseLayoutPlanner.BuildWallRuns(config.BaseHalfWidthCells, config.BaseHalfHeightCells, gateHalfGap, wallRuns);
        var gateFlankWalls = new List<InitialFactionBaseGateFlankWall>();
        InitialFactionBaseLayoutPlanner.BuildGateFlankWalls(
            config.BaseHalfWidthCells,
            config.BaseHalfHeightCells,
            bottomGateFootprint,
            sideGateFootprint,
            bottomWallFootprint,
            sideWallFootprint,
            gateFlankWalls);

        var wallRunRequestSystem = new InitialBaseWallRunRequestSystem();
        for (int factionIndex = 0; factionIndex < factionSpawns.Length; factionIndex++)
        {
            InitialUnitsFactionSpawnEntry factionSpawn = factionSpawns[factionIndex];
            Vector2Int anchor = new(factionSpawn.SpawnCell.x, factionSpawn.SpawnCell.y);
            for (int wallRunIndex = 0; wallRunIndex < wallRuns.Count; wallRunIndex++)
            {
                InitialFactionBaseWallRun run = wallRuns[wallRunIndex];
                requestCount += wallRunRequestSystem.Enqueue(
                    em,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    wallId,
                    new int2(anchor.x + run.StartOffset.x, anchor.y + run.StartOffset.y),
                    new int2(anchor.x + run.EndOffset.x, anchor.y + run.EndOffset.y),
                    bottomWallFootprint,
                    sideWallFootprint);
            }

            for (int flankIndex = 0; flankIndex < gateFlankWalls.Count; flankIndex++)
            {
                InitialFactionBaseGateFlankWall flank = gateFlankWalls[flankIndex];
                EnqueueInitialBuildingSpawnRequest(
                    em,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    wallId,
                    new int2(anchor.x + flank.OriginOffset.x, anchor.y + flank.OriginOffset.y),
                    flank.RotateVertical,
                    BuildingRuntimeSpawnRequest.KindWallSegment,
                    default,
                    allowExistingWallOverlap: true);
                requestCount++;
            }

            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                InitialFactionBasePlacement placement = placements[placementIndex];
                string buildingId;
                BuildingConfiguredSpawnableReadModel model;
                if (placement.Kind == InitialFactionBasePlacementKind.Gate)
                {
                    buildingId = gateId;
                    model = gateModel;
                }
                else
                {
                    if (!placementIds.TryGetValue(placement.PrefabKey, out buildingId) ||
                        !placementModels.TryGetValue(placement.PrefabKey, out model))
                    {
                        continue;
                    }
                }

                Vector2Int footprint = ToFootprint(model.FootprintCells, placement.RotateVertical);
                Vector2Int origin = InitialFactionBaseLayoutPlanner.ResolvePlacementOrigin(anchor, placement, footprint);
                EnqueueInitialBuildingSpawnRequest(
                    em,
                    boundaryEntity,
                    configEntity,
                    factionSpawn.FactionId,
                    buildingId,
                    new int2(origin.x, origin.y),
                    placement.RotateVertical,
                    BuildingRuntimeSpawnRequest.KindBuilding,
                    default,
                    false,
                    factionSpawn.FactionId == 0 && placement.Kind == InitialFactionBasePlacementKind.CoreBuilding
                        ? initialBaseCoreRequestEntryIndex
                        : 0);
                requestCount++;
            }
        }

        return true;
    }

    private static void EnqueueInitialBuildingSpawnRequest(
        EntityManager em,
        Entity boundaryEntity,
        Entity configEntity,
        byte factionId,
        string buildingId,
        int2 origin,
        bool rotateVertical,
        byte requestKind = BuildingRuntimeSpawnRequest.KindBuilding,
        int2 endOrigin = default,
        bool allowExistingWallOverlap = false,
        int entryIndex = 0)
    {
        DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
            new InitialBuildingBoundarySystem().GetRuntimeSpawnRequests(em, boundaryEntity);
        requests.Add(new BuildingRuntimeSpawnRequest
        {
            RequestId = requests.Length + 1,
            RequestKind = requestKind,
            FactionId = factionId,
            BuildingId = new FixedString128Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey(buildingId)),
            PreferredOrigin = origin,
            EndOrigin = endOrigin,
            RotateVertical = rotateVertical ? (byte)1 : (byte)0,
            AllowExistingWallOverlap = allowExistingWallOverlap ? (byte)1 : (byte)0,
            Status = BuildingRuntimeSpawnRequest.Pending,
            PlanEntity = configEntity,
            EntryIndex = entryIndex
        });
    }

    private static Vector2Int ToFootprint(int2 footprint, bool rotateVertical)
    {
        int x = math.max(1, footprint.x);
        int y = math.max(1, footprint.y);
        return rotateVertical ? new Vector2Int(y, x) : new Vector2Int(x, y);
    }
}
