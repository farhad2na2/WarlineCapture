using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

internal struct UnitPathfindingScheduleSystem
{
    private MapSurfacePathfindingReadSystem _surfaceReadSystem;

    public struct Result
    {
        public bool Scheduled;
        public JobHandle PendingPathHandle;
        public NativeStream PendingPathStream;
        public int RequestCount;
        public int RequestBudget;
        public int LiveUnitCount;
        public int GridWidth;
        public int GridHeight;
        public int ScheduleFrame;
        public double ScheduleTime;
    }

    public Result Schedule(
        ref SystemState state,
        ref UnitPathfindingQuerySystem queries,
        ref UnitPathScratchWorkspaceSystem scratchWorkspace,
        ref UnitPathLiveUnitSnapshotSystem liveUnitSnapshot,
        ref UnitPathRequestBufferSystem requestBuffers,
        ref UnitPathIgnoredOccupancySystem ignoredOccupancy,
        ref UnitPathRequestCollectionSystem requestCollection,
        ref UnitPathReservedGoalSystem reservedGoals,
        ref UnitPathSegmentationSystem segmentation,
        ref UnitPathCoarseWorkspaceSystem coarseWorkspace,
        ref UnitHierarchicalPathSystem hierarchicalPath,
        ref UnitPathGoalAssignmentSystem goalAssignment,
        ref UnitPathfindingDiagnosticSystem diagnostics,
        ref int lastHierarchicalPathValidationFrame,
        int requestBudget,
        int adaptiveRequestBudget,
        bool enableFreezeLogs,
        bool enableHierarchicalPathValidationLog,
        double freezeLogThresholdSeconds)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        int requestCountForLog = 0;
        int liveUnitCountForLog = 0;
        int gridWidthForLog = 0;
        int gridHeightForLog = 0;
        double afterGridTime = startTime;
        double afterScratchTime = startTime;
        double afterSnapshotTime = startTime;
        double afterRequestCollectTime = startTime;
        double afterGoalAssignTime = startTime;
        double afterScheduleTime = startTime;
        double afterCompleteTime = startTime;
        double afterApplyTime = startTime;
        bool scratchWasAllocated = false;
        int scratchCellsForLog = 0;
        int scratchThreadSlotsForLog = 0;

        try
        {
            Entity gridEntity = queries.GridQuery.GetSingletonEntity();
            EntityManager em = state.EntityManager;
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            gridWidthForLog = grid.Width;
            gridHeightForLog = grid.Height;
            int gridSize = grid.Width * grid.Height;
            afterGridTime = Time.realtimeSinceStartupAsDouble;
            scratchWasAllocated = scratchWorkspace.Ensure(gridSize, out scratchCellsForLog, out scratchThreadSlotsForLog);
            afterScratchTime = Time.realtimeSinceStartupAsDouble;

            if (queries.RequestQuery.IsEmptyIgnoreFilter)
                return default;

            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            MapSurfacePathfindingReadSystem.Context surfaceContext = _surfaceReadSystem.TryCreateContext(em, queries.MapSurfaceQuery, out MapSurfacePathfindingReadSystem.Context resolvedSurfaceContext)
                ? resolvedSurfaceContext
                : _surfaceReadSystem.CreateFlatFallbackContext();
            DynamicBlockerComponent dynamicBlockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            DynamicOccupancyComponent dynamicOccupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            NativeBitArray dynamicBlockers = dynamicBlockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = dynamicBlockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = dynamicOccupancyData.Occupied;
            liveUnitSnapshot.Capture(ref state, queries.LiveUnitsQuery);
            NativeArray<Entity> liveUnitEntities = liveUnitSnapshot.Entities;
            NativeArray<UnitGrid> liveUnitGrids = liveUnitSnapshot.Grids;
            NativeArray<UnitFootprint> liveUnitFootprints = liveUnitSnapshot.Footprints;
            NativeArray<byte> liveUnitManualGroupMembers = liveUnitSnapshot.ManualGroupMembers;
            liveUnitCountForLog = liveUnitEntities.Length;
            afterSnapshotTime = Time.realtimeSinceStartupAsDouble;

            int requestCount = requestCollection.Collect(ref state, ref queries, ref requestBuffers, ref ignoredOccupancy, requestBudget);
            requestCountForLog = requestCount;
            afterRequestCollectTime = Time.realtimeSinceStartupAsDouble;
            if (requestCount == 0)
            {
                liveUnitSnapshot.Dispose();
                return default;
            }

            reservedGoals.Prepare(gridSize);

            requestBuffers.AssignedGoals.ResizeUninitialized(requestCount);
            requestBuffers.Status.ResizeUninitialized(requestCount);
            requestBuffers.Segmented.ResizeUninitialized(requestCount);
            for (int i = 0; i < requestCount; i++)
            {
                requestBuffers.Status[i] = 0;
                requestBuffers.Segmented[i] = 0;
            }

            NativeArray<Entity> requestEntities = requestBuffers.Entities.AsArray();
            NativeArray<UnitGrid> requestUnitGrids = requestBuffers.UnitGrids.AsArray();
            NativeArray<UnitPathRequest> requestGoals = requestBuffers.Goals.AsArray();
            NativeArray<UnitFootprint> requestFootprints = requestBuffers.Footprints.AsArray();
            NativeArray<UnitMovementBehavior> requestMovementBehaviors = requestBuffers.MovementBehaviors.AsArray();
            NativeArray<byte> requestFactions = requestBuffers.Factions.AsArray();
            NativeArray<byte> requestManualMoves = requestBuffers.ManualMoves.AsArray();
            NativeArray<Entity> requestIgnoredOccupancyEntities = requestBuffers.IgnoredOccupancyEntities.AsArray();
            NativeArray<int2> requestIgnoredOccupancyCells = requestBuffers.IgnoredOccupancyCells.AsArray();
            NativeArray<int2> requestIgnoredOccupancySizes = requestBuffers.IgnoredOccupancySizes.AsArray();
            NativeArray<int2> assignedGoals = requestBuffers.AssignedGoals.AsArray();
            NativeArray<byte> status = requestBuffers.Status.AsArray();
            NativeArray<byte> segmented = requestBuffers.Segmented.AsArray();
            requestBuffers.CheapSegmentModes.ResizeUninitialized(requestCount);
            requestBuffers.AlternateSearchSkipped.ResizeUninitialized(requestCount);
            requestBuffers.AlternateAttempts.ResizeUninitialized(requestCount);
            NativeArray<byte> cheapSegmentModes = requestBuffers.CheapSegmentModes.AsArray();
            NativeArray<byte> alternateSearchSkipped = requestBuffers.AlternateSearchSkipped.AsArray();
            NativeArray<int> alternateAttempts = requestBuffers.AlternateAttempts.AsArray();
            NativeArray<int> reservedGoalEpochs = reservedGoals.Epochs;
            int reservedGoalGeneration = reservedGoals.Generation;
            int scratchSearchEpochBase = scratchWorkspace.ReserveEpochs(requestCount);
            int manualRequestCount = 0;
            int hierarchicalEligibleCount = 0;
            int hierarchicalWaypointCount = 0;
            int hierarchicalFallbackCount = 0;
            for (int i = 0; i < requestCount; i++)
            {
                int2 start = requestUnitGrids[i].Cell;
                int2 footprintSize = requestFootprints[i].Size;
                int startIndex = GridUtils.InBounds(start, grid.Width, grid.Height) ? GridUtils.CellToIndex(start, grid.Width) : -1;
                int2 requestedGoal = requestGoals[i].Goal;
                bool isVehicle = UnitVehicleMovementUtility.IsVehicle(requestFootprints[i], requestMovementBehaviors[i]);
                bool isManualMove = requestManualMoves[i] != 0;
                if (isManualMove)
                {
                    manualRequestCount++;
                    if (segmentation.ExceedsMaxSegment(start, requestedGoal, true, isVehicle))
                        hierarchicalEligibleCount++;
                }

                int2 pathGoal = GetSegmentGoalHierarchical(
                    grid,
                    walkable.AsNativeArray(),
                    dynamicBlockers,
                    friendlyPassFactionIds,
                    ref segmentation,
                    ref coarseWorkspace,
                    ref hierarchicalPath,
                    start,
                    requestedGoal,
                    isManualMove,
                    isVehicle,
                    requestFactions[i],
                    out bool usedHierarchicalWaypoint,
                    out bool hierarchicalFallback);
                if (usedHierarchicalWaypoint)
                    hierarchicalWaypointCount++;
                if (hierarchicalFallback)
                    hierarchicalFallbackCount++;
                bool isSegmentedRequest = !pathGoal.Equals(requestedGoal);
                segmented[i] = (byte)(isSegmentedRequest ? 1 : 0);
                bool cheapSegmentMode = isManualMove && !isVehicle;
                cheapSegmentModes[i] = (byte)(cheapSegmentMode ? 1 : 0);
                bool skipAlternateSearch = isManualMove && !isVehicle;
                alternateSearchSkipped[i] = (byte)(skipAlternateSearch ? 1 : 0);
                alternateAttempts[i] = 0;
                int2 assignedGoal = goalAssignment.FindNearestFreeGoal(
                    grid,
                    walkable.AsNativeArray(),
                    dynamicBlockers,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    reservedGoalEpochs,
                    reservedGoalGeneration,
                    requestEntities[i],
                    requestIgnoredOccupancyEntities[i],
                    requestIgnoredOccupancyCells[i],
                    requestIgnoredOccupancySizes[i],
                    pathGoal,
                    start,
                    footprintSize,
                    requestFactions[i],
                    startIndex);
                if (assignedGoal.Equals(start) && !pathGoal.Equals(start))
                    assignedGoal = pathGoal;
                assignedGoals[i] = assignedGoal;
            }

            if (enableHierarchicalPathValidationLog &&
                manualRequestCount > 0 &&
                Time.frameCount - lastHierarchicalPathValidationFrame >= 60)
            {
                lastHierarchicalPathValidationFrame = Time.frameCount;
                diagnostics.LogHierarchicalValidation(state.EntityManager, Time.frameCount, requestCount, manualRequestCount, hierarchicalEligibleCount, hierarchicalWaypointCount, hierarchicalFallbackCount, UnitHierarchicalPathSystem.SectorSizeCells, UnitPathSegmentationSystem.ManualInfantryLongDistanceSegmentCells, UnitPathSegmentationSystem.ManualVehicleLongDistanceSegmentCells, UnitHierarchicalPathSystem.MaxExpandedSectors);
            }

            afterGoalAssignTime = Time.realtimeSinceStartupAsDouble;

            var pendingPathStream = new NativeStream(requestCount, Allocator.Persistent);

            var job = new PathfindBatchJob
            {
                Grid = grid,
                Walkable = walkable.AsNativeArray(),
                Roads = roads.AsNativeArray(),
                Sidewalks = sidewalks.AsNativeArray(),
                DirtRoads = dirtRoads.AsNativeArray(),
                MapSurface = surfaceContext.Surface,
                HasMapSurface = surfaceContext.HasSurfaceData,
                SurfaceValidation = new MapSurfacePathingValidationSystem(),
                MapSurfacePathCost = surfaceContext.PathCost,
                SurfacePathCost = new MapSurfacePathCostSystem(),
                SurfaceRoadPriority = new MapSurfaceRoadPrioritySystem(),
                DynamicBlocked = dynamicBlockers,
                FriendlyPassFactionIds = friendlyPassFactionIds,
                Occupied = occupied,
                LiveUnitEntities = liveUnitEntities,
                LiveUnitGrids = liveUnitGrids,
                LiveUnitFootprints = liveUnitFootprints,
                LiveUnitManualGroupMembers = liveUnitManualGroupMembers,
                Entities = requestEntities,
                UnitGrids = requestUnitGrids,
                Footprints = requestFootprints,
                MovementBehaviors = requestMovementBehaviors,
                Factions = requestFactions,
                ManualMoves = requestManualMoves,
                IgnoredOccupancyEntities = requestIgnoredOccupancyEntities,
                IgnoredOccupancyCells = requestIgnoredOccupancyCells,
                IgnoredOccupancySizes = requestIgnoredOccupancySizes,
                RequestedGoals = requestGoals,
                Goals = assignedGoals,
                Segmented = segmented,
                Output = pendingPathStream.AsWriter(),
                Status = status,
                CheapSegmentModes = cheapSegmentModes,
                AlternateSearchSkipped = alternateSearchSkipped,
                AlternateAttempts = alternateAttempts,
                GridSize = gridSize,
                ScratchCameFrom = scratchWorkspace.CameFrom,
                ScratchGScore = scratchWorkspace.GScore,
                ScratchClosed = scratchWorkspace.Closed,
                ScratchInOpen = scratchWorkspace.InOpen,
                ScratchEpoch = scratchWorkspace.Epoch,
                ScratchOpen = scratchWorkspace.Open,
                ScratchPath = scratchWorkspace.Path,
                SearchEpochBase = scratchSearchEpochBase,
            };

            JobHandle pendingPathHandle = job.Schedule(requestCount, state.Dependency);
            afterScheduleTime = Time.realtimeSinceStartupAsDouble;
            afterCompleteTime = afterScheduleTime;
            afterApplyTime = afterScheduleTime;
            state.Dependency = pendingPathHandle;

            return new Result
            {
                Scheduled = true,
                PendingPathHandle = pendingPathHandle,
                PendingPathStream = pendingPathStream,
                RequestCount = requestCount,
                RequestBudget = requestBudget,
                LiveUnitCount = liveUnitCountForLog,
                GridWidth = gridWidthForLog,
                GridHeight = gridHeightForLog,
                ScheduleFrame = Time.frameCount,
                ScheduleTime = afterScheduleTime,
            };
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;

            if (enableFreezeLogs && elapsed >= freezeLogThresholdSeconds)
            {
                if (afterSnapshotTime < afterScratchTime)
                    afterSnapshotTime = afterScratchTime;
                if (afterRequestCollectTime < afterSnapshotTime)
                    afterRequestCollectTime = afterSnapshotTime;
                if (afterGoalAssignTime < afterRequestCollectTime)
                    afterGoalAssignTime = afterRequestCollectTime;
                if (afterScheduleTime < afterGoalAssignTime)
                    afterScheduleTime = afterGoalAssignTime;
                if (afterCompleteTime < afterScheduleTime)
                    afterCompleteTime = afterScheduleTime;
                if (afterApplyTime < afterCompleteTime)
                    afterApplyTime = afterCompleteTime;

                diagnostics.LogFrameFreeze(
                    state.EntityManager,
                    Time.frameCount,
                    elapsed,
                    startTime,
                    afterGridTime,
                    afterScratchTime,
                    afterSnapshotTime,
                    afterRequestCollectTime,
                    afterGoalAssignTime,
                    afterScheduleTime,
                    afterCompleteTime,
                    afterApplyTime,
                    scratchWasAllocated,
                    scratchCellsForLog,
                    scratchThreadSlotsForLog,
                    requestCountForLog,
                    requestBudget,
                    adaptiveRequestBudget,
                    0,
                    0,
                    0,
                    liveUnitCountForLog,
                    gridWidthForLog,
                    gridHeightForLog);
            }
        }
    }

    private static int2 GetSegmentGoalHierarchical(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        ref UnitPathSegmentationSystem segmentation,
        ref UnitPathCoarseWorkspaceSystem coarseWorkspace,
        ref UnitHierarchicalPathSystem hierarchicalPath,
        int2 start,
        int2 requestedGoal,
        bool manualMove,
        bool isVehicle,
        byte factionId,
        out bool usedHierarchicalWaypoint,
        out bool hierarchicalFallback)
    {
        usedHierarchicalWaypoint = false;
        hierarchicalFallback = false;
        float maxSegmentCells = segmentation.GetMaxSegmentCells(manualMove, isVehicle);
        int2 segmentGoal = segmentation.GetSegmentGoal(start, requestedGoal, maxSegmentCells);
        if (segmentGoal.Equals(requestedGoal))
            return requestedGoal;

        if (manualMove &&
            coarseWorkspace.Ensure(grid.Width, grid.Height, UnitHierarchicalPathSystem.SectorSizeCells) &&
            hierarchicalPath.TryFindWaypoint(
                grid,
                walkable,
                dynamicBlocked,
                friendlyPassFactionIds,
                ref coarseWorkspace,
                start,
                requestedGoal,
                maxSegmentCells,
                factionId,
                out int2 waypoint))
        {
            usedHierarchicalWaypoint = true;
            return waypoint;
        }

        hierarchicalFallback = manualMove;
        return segmentGoal;
    }
}
