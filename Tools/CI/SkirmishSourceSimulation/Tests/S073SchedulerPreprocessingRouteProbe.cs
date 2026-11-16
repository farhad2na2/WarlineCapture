using System;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Read-only diagnostic over the retained S073 startup host. Route endpoints are
// actual spawned units and actual opposing registered faction buildings. The
// job runs synchronously; it does not publish a path request or move an entity.
internal static class S073SchedulerPreprocessingRouteProbe
{
    private static void Need(bool value, string detail)
    {
        if (!value) throw new InvalidOperationException("S073 read-only route probe: " + detail);
    }

    internal static void Capture(EntityManager em, Entity gridEntity, Entity surfaceEntity,
        Entity[] spawnedUnits, string outputPath)
    {
        Need(em != null && em.Exists(gridEntity) && em.Exists(surfaceEntity), "retained grid and surface required");
        Need(spawnedUnits != null && spawnedUnits.Length == 24, "actual 24-unit source roster required");
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        int gridSize = checked(grid.Width * grid.Height);
        Need(grid.Width == 2048 && grid.Height == 1024, "retained S073 grid dimensions required");
        DynamicBlockerComponent blocker = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        Need(RuntimeGridBlockerEpoch.IsCommitted(grid, blocker) && blocker.Blocked.IsCreated &&
             blocker.FriendlyPassFactionIds.IsCreated && blocker.GridSize == gridSize,
             "committed original static/dynamic blocker projection required");
        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        Need(occupancy.GridSize == gridSize && occupancy.Occupied.IsCreated,
             "original rebuilt live-unit occupancy required");

        using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        Need(surfaces.CalculateEntityCount() == 1 && surfaces.GetSingletonEntity() == surfaceEntity,
             "unique retained map-surface owner required");
        var surface = em.GetComponentData<MapSurfaceComponent>(surfaceEntity);
        Need(surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated,
             "actual retained S073 surface blob required");
        var pathCostUtility = new MapSurfacePathCost();
        var surfaceCost = em.HasComponent<MapSurfacePathCostComponent>(surfaceEntity)
            ? em.GetComponentData<MapSurfacePathCostComponent>(surfaceEntity)
            : pathCostUtility.CreateDisabledDefault();
        var surfaceSnapshot = new MapSurfacePathfindingSnapshot();
        Need(surfaceSnapshot.TryCreateContext(em, surfaces, out var surfaceContext),
             "original scheduler map-surface context required");

        DynamicBuffer<GridWalkable> walkableBuffer = em.GetBuffer<GridWalkable>(gridEntity, true);
        DynamicBuffer<GridRoad> roadsBuffer = em.GetBuffer<GridRoad>(gridEntity, true);
        DynamicBuffer<GridRoadSidewalk> sidewalkBuffer = em.GetBuffer<GridRoadSidewalk>(gridEntity, true);
        DynamicBuffer<GridRoadDirt> dirtBuffer = em.GetBuffer<GridRoadDirt>(gridEntity, true);
        Need(walkableBuffer.Length == gridSize && roadsBuffer.Length == gridSize &&
             sidewalkBuffer.Length == gridSize && dirtBuffer.Length == gridSize,
             "complete source grid and road buffers required");

        using var buildings = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>() },
            None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>() }
        });
        using var buildingEntities = buildings.ToEntityArray(Allocator.Temp);
        Need(buildingEntities.Length > 0, "actual registered faction buildings required");
        using var liveQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>() },
            None = new[] { ComponentType.ReadOnly<StaticGridBlocker>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>() }
        });
        using var allLiveUnits = liveQuery.ToEntityArray(Allocator.Temp);
        Need(allLiveUnits.Length >= spawnedUnits.Length, "complete original live-unit pathfinding domain required");

        var destinations = new Entity[spawnedUnits.Length];
        var starts = new NativeArray<UnitGrid>(spawnedUnits.Length, Allocator.Temp);
        var footprints = new NativeArray<UnitFootprint>(spawnedUnits.Length, Allocator.Temp);
        var movement = new NativeArray<UnitMovementBehavior>(spawnedUnits.Length, Allocator.Temp);
        var factions = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp);
        var manual = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var ignoredEntities = new NativeArray<Entity>(spawnedUnits.Length, Allocator.Temp);
        var ignoredCells = new NativeArray<int2>(spawnedUnits.Length, Allocator.Temp);
        var ignoredSizes = new NativeArray<int2>(spawnedUnits.Length, Allocator.Temp);
        var requestedGoals = new NativeArray<UnitPathRequest>(spawnedUnits.Length, Allocator.Temp);
        var goals = new NativeArray<int2>(spawnedUnits.Length, Allocator.Temp);
        var status = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var failures = new NativeArray<int>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var expansions = new NativeArray<int>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var segmented = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var cheapModes = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var alternateSkipped = new NativeArray<byte>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var alternateAttempts = new NativeArray<int>(spawnedUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var liveEntities = new NativeArray<Entity>(allLiveUnits.Length, Allocator.Temp);
        var liveGrids = new NativeArray<UnitGrid>(allLiveUnits.Length, Allocator.Temp);
        var liveFootprints = new NativeArray<UnitFootprint>(allLiveUnits.Length, Allocator.Temp);
        var liveManual = new NativeArray<byte>(allLiveUnits.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var requestEntities = new NativeArray<Entity>(spawnedUnits.Length, Allocator.Temp);
        var reservedGoalEpochs = new NativeArray<int>(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var cameFrom = new NativeArray<int>(gridSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var gScore = new NativeArray<int>(gridSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var closed = new NativeArray<byte>(gridSize, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        var inOpen = new NativeArray<byte>(gridSize, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        var epochs = new NativeArray<int>(gridSize, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        var open = new NativeArray<long>(gridSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var path = new NativeArray<int>(gridSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        using var output = new NativeStream(spawnedUnits.Length, Allocator.Temp);
        try
        {
            for (int i = 0; i < allLiveUnits.Length; i++)
            {
                Entity liveUnit = allLiveUnits[i];
                liveEntities[i] = liveUnit;
                liveGrids[i] = em.GetComponentData<UnitGrid>(liveUnit);
                liveFootprints[i] = em.GetComponentData<UnitFootprint>(liveUnit);
                liveManual[i] = (byte)(em.HasComponent<ManualMoveGroupMemberTag>(liveUnit) ? 1 : 0);
            }
            for (int i = 0; i < spawnedUnits.Length; i++)
            {
                Entity unit = spawnedUnits[i];
                Need(em.Exists(unit) && em.HasComponent<UnitGrid>(unit) && em.HasComponent<UnitFootprint>(unit) &&
                     em.HasComponent<UnitMovementBehavior>(unit) && em.HasComponent<Faction>(unit),
                     "spawned route unit lacks original pathfinding components");
                var start = em.GetComponentData<UnitGrid>(unit);
                var faction = em.GetComponentData<Faction>(unit).Id;
                byte targetFaction = FactionIdentity.ResolveDefaultTargetFaction(faction);
                Entity destination = Entity.Null;
                int bestDistance = int.MaxValue;
                int2 goal = default;
                foreach (Entity candidate in buildingEntities)
                {
                    if (em.GetComponentData<Faction>(candidate).Id != targetFaction ||
                        em.GetComponentData<UnitHealth>(candidate).Current <= 0) continue;
                    int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                    int distance = math.abs(cell.x - start.Cell.x) + math.abs(cell.y - start.Cell.y);
                    if (distance >= bestDistance) continue;
                    destination = candidate; goal = cell; bestDistance = distance;
                }
                Need(destination != Entity.Null, "opposing live registered building endpoint missing");
                destinations[i] = destination;
                requestEntities[i] = unit;
                starts[i] = start;
                footprints[i] = em.GetComponentData<UnitFootprint>(unit);
                movement[i] = em.GetComponentData<UnitMovementBehavior>(unit);
                factions[i] = faction;
                ignoredEntities[i] = unit;
                ignoredCells[i] = start.Cell;
                ignoredSizes[i] = footprints[i].Size;
                requestedGoals[i] = new UnitPathRequest { Goal = goal };
                // Controlled diagnostic request: the intent is injected above. Apply the
                // original scheduler's non-manual segmentation, detailed Skirmish override,
                // routing profile, and sequential reserved-goal assignment to that request.
                var footprintSize = footprints[i].Size;
                var behavior = movement[i];
                bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprints[i], behavior);
                bool isManualMove = false;
                bool requestedSurfaceValid = GridUtils.InBounds(goal, grid.Width, grid.Height) &&
                    new MapSurfaceTraversalValidation().CanTraverseFootprint(
                        surfaceContext.Surface, surfaceContext.HasSurfaceData, grid, goal, footprintSize, isVehicle);
                int2 pathGoal = requestedSurfaceValid
                    ? UnitPathSegmentationPolicy(start.Cell, goal, isManualMove, isVehicle)
                    : goal;
                bool detailedSkirmishPath = em.HasComponent<SkirmishCombatTuned>(unit) &&
                    math.distancesq(start.Cell, goal) <=
                    UnitPathSegmentation.ManualInfantryLongDistanceSegmentCells *
                    UnitPathSegmentation.ManualInfantryLongDistanceSegmentCells;
                if (detailedSkirmishPath) pathGoal = goal;
                segmented[i] = (byte)(pathGoal.Equals(goal) ? 0 : 1);
                cheapModes[i] = PathGoalPolicyKernel.GetRoutingMode(isManualMove, isVehicle, detailedSkirmishPath, false);
                alternateSkipped[i] = (byte)(PathGoalPolicyKernel.SkipAlternateSearch(isManualMove, isVehicle, detailedSkirmishPath, false) ? 1 : 0);
                int startIndex = GridUtils.InBounds(start.Cell, grid.Width, grid.Height)
                    ? GridUtils.CellToIndex(start.Cell, grid.Width) : -1;
                var assignment = new UnitPathGoalAssignment();
                int2 assignedGoal = assignment.FindNearestFreeGoal(
                    grid, walkableBuffer.AsNativeArray(), blocker.Blocked, blocker.FriendlyPassFactionIds,
                    occupancy.Occupied, liveEntities, liveGrids, liveFootprints, reservedGoalEpochs, 1,
                    unit, unit, start.Cell, footprintSize, surfaceContext, pathGoal, start.Cell,
                    footprintSize, faction, startIndex);
                if (PathGoalAssignmentKernel.ShouldRestoreDesiredGoal(
                    new PlacementCell(assignedGoal.x, assignedGoal.y), new PlacementCell(start.Cell.x, start.Cell.y),
                    new PlacementCell(pathGoal.x, pathGoal.y), grid.Width, grid.Height))
                {
                    bool validSurface = new MapSurfaceTraversalValidation().CanTraverseFootprint(
                        surfaceContext.Surface, surfaceContext.HasSurfaceData, grid, pathGoal, footprintSize, isVehicle);
                    var restored = PathGoalAssignmentKernel.RestoreDesiredGoal(
                        new PlacementCell(assignedGoal.x, assignedGoal.y), new PlacementCell(pathGoal.x, pathGoal.y), validSurface);
                    assignedGoal = new int2(restored.x, restored.y);
                }
                goals[i] = assignedGoal;
            }

            var job = new PathfindBatchJob
            {
                Grid = grid,
                Walkable = walkableBuffer.AsNativeArray(),
                Roads = roadsBuffer.AsNativeArray(),
                Sidewalks = sidewalkBuffer.AsNativeArray(),
                DirtRoads = dirtBuffer.AsNativeArray(),
                MapSurface = surface,
                HasMapSurface = 1,
                SurfaceValidation = new MapSurfaceTraversalValidation(),
                MapSurfacePathCost = surfaceCost,
                SurfacePathCost = pathCostUtility,
                SurfaceRoadPriority = new MapSurfaceRoadPriorityPolicy(),
                DynamicBlocked = blocker.Blocked,
                FriendlyPassFactionIds = blocker.FriendlyPassFactionIds,
                Occupied = occupancy.Occupied,
                LiveUnitEntities = liveEntities,
                LiveUnitGrids = liveGrids,
                LiveUnitFootprints = liveFootprints,
                LiveUnitManualGroupMembers = liveManual,
                Entities = requestEntities,
                UnitGrids = starts,
                Footprints = footprints,
                MovementBehaviors = movement,
                Factions = factions,
                ManualMoves = manual,
                IgnoredOccupancyEntities = ignoredEntities,
                IgnoredOccupancyCells = ignoredCells,
                IgnoredOccupancySizes = ignoredSizes,
                RequestedGoals = requestedGoals,
                Goals = goals,
                Segmented = segmented,
                Output = output.AsWriter(),
                Status = status,
                FailureCodes = failures,
                ExpansionCounts = expansions,
                CheapSegmentModes = cheapModes,
                AlternateSearchSkipped = alternateSkipped,
                AlternateAttempts = alternateAttempts,
                GridSize = gridSize,
                ScratchCameFrom = cameFrom,
                ScratchGScore = gScore,
                ScratchClosed = closed,
                ScratchInOpen = inOpen,
                ScratchEpoch = epochs,
                ScratchOpen = open,
                ScratchPath = path,
                SearchEpochBase = 1
            };
            for (int i = 0; i < spawnedUnits.Length; i++) job.Execute(i);

            var routes = new JArray();
            var reader = output.AsReader();
            for (int i = 0; i < spawnedUnits.Length; i++)
            {
                int routeCellCount = reader.BeginForEachIndex(i);
                var cells = new JArray();
                for (int cellIndex = 0; cellIndex < routeCellCount; cellIndex++)
                {
                    int2 cell = reader.Read<int2>();
                    cells.Add(new JArray(cell.x, cell.y));
                }
                reader.EndForEachIndex();
                routes.Add(new JObject
                {
                    ["unit_entity"] = spawnedUnits[i].ToString(),
                    ["faction_id"] = factions[i],
                    ["start_cell"] = new JArray(starts[i].Cell.x, starts[i].Cell.y),
                    ["endpoint_entity"] = destinations[i].ToString(),
                    ["endpoint_cell"] = new JArray(requestedGoals[i].Goal.x, requestedGoals[i].Goal.y),
                    ["resolved_path_goal_cell"] = new JArray(goals[i].x, goals[i].y),
                    ["route_found"] = status[i] != 0,
                    ["failure_code"] = failures[i],
                    ["expansions"] = expansions[i],
                    ["cells"] = cells
                });
            }
            var result = new JObject
            {
                ["kind"] = "S073SchedulerPreprocessingRouteProbe",
                ["schema_version"] = 1,
                ["scope"] = "ReconstructedSchedulerGlue_OriginalSegmentationGoalAssignmentAndPathfindJob_ControlledDiagnosticRequests",
                ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height, ["walkable_cells"] = walkableBuffer.Length },
                ["surface_blob_used"] = true,
                ["committed_blocker_storage_used"] = true,
                ["rebuilt_live_occupancy_used"] = true,
                ["diagnostic_goal_policy"] = "Nearest actual live registered opposing-faction building by Manhattan distance; diagnostic only",
                ["path_request_profile"] = "Injected diagnostic requests (not actual ARIA/player intent); original UnitPathSegmentation, Skirmish detailed-path override, PathGoalPolicyKernel routing mode, UnitPathGoalAssignment and sequential reserved-goal epochs applied before original PathfindBatchJob",
                ["coverage_optimization"] = "Disabled for this direct diagnostic; exact placement checks retain full live-unit arrays",
                ["surface_path_cost_source"] = em.HasComponent<MapSurfacePathCostComponent>(surfaceEntity)
                    ? "Actual map-surface path-cost component" : "Original disabled-default path-cost projection",
                ["movement_or_command_issued"] = false,
                ["scheduler_admitted"] = false,
                ["requests_injected"] = true,
                ["request_origin"] = "Probe-injected diagnostic nearest-opposing-building requests; not ARIA/player orders or actual UnitPathRequest components",
                ["original_scheduler_admission_and_system_schedule_invoked"] = false,
                ["mission_ready"] = false,
                ["routes"] = routes
            };
            System.IO.File.WriteAllText(outputPath, result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        }
        finally
        {
            starts.Dispose(); footprints.Dispose(); movement.Dispose(); factions.Dispose(); manual.Dispose();
            ignoredEntities.Dispose(); ignoredCells.Dispose(); ignoredSizes.Dispose(); requestedGoals.Dispose(); goals.Dispose();
            status.Dispose(); failures.Dispose(); expansions.Dispose(); segmented.Dispose(); cheapModes.Dispose(); alternateSkipped.Dispose(); alternateAttempts.Dispose();
            liveEntities.Dispose(); liveGrids.Dispose(); liveFootprints.Dispose(); liveManual.Dispose();
            requestEntities.Dispose();
            reservedGoalEpochs.Dispose();
            cameFrom.Dispose(); gScore.Dispose(); closed.Dispose(); inOpen.Dispose(); epochs.Dispose(); open.Dispose(); path.Dispose();
        }
    }

    private static int2 UnitPathSegmentationPolicy(int2 start, int2 goal, bool manualMove, bool isVehicle)
    {
        var segmentation = new UnitPathSegmentation();
        float maxSegmentCells = segmentation.GetMaxSegmentCells(manualMove, isVehicle);
        return segmentation.GetSegmentGoal(start, goal, maxSegmentCells);
    }
}
