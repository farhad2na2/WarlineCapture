using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
internal struct PathfindBatchJob : IJobFor
{
    private const int FreeTraversalCost = 10;
    private const int FreeDiagonalTraversalCost = 14;
    private const int PreferredSurfaceTraversalCost = 6;
    private const int PreferredSurfaceDiagonalTraversalCost = 8;
    private const int AvoidedSurfaceTraversalCost = 18;
    private const int AvoidedSurfaceDiagonalTraversalCost = 25;
    private const int OccupiedTraversalPenalty = 50;
    private const int InfantryMaxAStarExpansions = 450;
    private const int InfantrySegmentedMaxAStarExpansions = 30000;
    private const int VehicleMaxAStarExpansions = 1600;
    private const int VehicleManualMaxAStarExpansions = 12000;
    private const int InfantrySearchBoundsPaddingCells = 8;
    private const int InfantrySegmentedSearchBoundsPaddingCells = 180;
    private const int VehicleSearchBoundsPaddingCells = 12;
    private static readonly int2[] SearchDirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    [ReadOnly] public GridConfig Grid;
    [ReadOnly] public NativeArray<GridWalkable> Walkable;
    [ReadOnly] public NativeArray<GridRoad> Roads;
    [ReadOnly] public NativeArray<GridRoadSidewalk> Sidewalks;
    [ReadOnly] public NativeArray<GridRoadDirt> DirtRoads;
    [ReadOnly] public MapSurfaceComponent MapSurface;
    [ReadOnly] public byte HasMapSurface;
    public MapSurfacePathingValidationSystem SurfaceValidation;
    [ReadOnly] public MapSurfacePathCostComponent MapSurfacePathCost;
    public MapSurfacePathCostSystem SurfacePathCost;
    public MapSurfaceRoadPrioritySystem SurfaceRoadPriority;
    [ReadOnly] public NativeBitArray DynamicBlocked;
    [ReadOnly] public NativeArray<byte> FriendlyPassFactionIds;
    [ReadOnly] public NativeBitArray Occupied;
    [ReadOnly] public NativeArray<Entity> LiveUnitEntities;
    [ReadOnly] public NativeArray<UnitGrid> LiveUnitGrids;
    [ReadOnly] public NativeArray<UnitFootprint> LiveUnitFootprints;
    [ReadOnly] public NativeArray<byte> LiveUnitManualGroupMembers;
    [ReadOnly] public NativeArray<Entity> Entities;
    [ReadOnly] public NativeArray<UnitGrid> UnitGrids;
    [ReadOnly] public NativeArray<UnitFootprint> Footprints;
    [ReadOnly] public NativeArray<UnitMovementBehavior> MovementBehaviors;
    [ReadOnly] public NativeArray<byte> Factions;
    [ReadOnly] public NativeArray<byte> ManualMoves;
    [ReadOnly] public NativeArray<Entity> IgnoredOccupancyEntities;
    [ReadOnly] public NativeArray<int2> IgnoredOccupancyCells;
    [ReadOnly] public NativeArray<int2> IgnoredOccupancySizes;
    [ReadOnly] public NativeArray<UnitPathRequest> RequestedGoals;
    public NativeArray<int2> Goals;
    [NativeDisableParallelForRestriction] public NativeArray<byte> Segmented;

    public NativeStream.Writer Output;
    public NativeArray<byte> Status; // 1 = found, 0 = none/invalid
    public NativeArray<int> FailureCodes;
    public NativeArray<int> ExpansionCounts;
    [ReadOnly] public NativeArray<byte> CheapSegmentModes;
    [ReadOnly] public NativeArray<byte> AlternateSearchSkipped;
    [NativeDisableParallelForRestriction] public NativeArray<int> AlternateAttempts;

    public int GridSize;

    [NativeDisableParallelForRestriction] public NativeArray<int> ScratchCameFrom;
    [NativeDisableParallelForRestriction] public NativeArray<int> ScratchGScore;
    [NativeDisableParallelForRestriction] public NativeArray<byte> ScratchClosed;
    [NativeDisableParallelForRestriction] public NativeArray<byte> ScratchInOpen;
    [NativeDisableParallelForRestriction] public NativeArray<int> ScratchEpoch;
    [NativeDisableParallelForRestriction] public NativeArray<long> ScratchOpen;
    [NativeDisableParallelForRestriction] public NativeArray<int> ScratchPath;
    public int SearchEpochBase;

    public void Execute(int index)
    {
        Output.BeginForEachIndex(index);

        int2 start = UnitGrids[index].Cell;
        UnitFootprint footprint = Footprints[index];
        int2 footprintSize = footprint.Size;
        int2 desiredGoal = RequestedGoals[index].Goal;
        int2 goal = Goals[index];
        byte factionId = Factions[index];
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, MovementBehaviors[index]);
        bool manualMove = ManualMoves[index] != 0;
        bool cheapSegmentMode = CheapSegmentModes[index] != 0;
        bool skipAlternateSearch = AlternateSearchSkipped[index] != 0;
        Entity movingEntity = Entities[index];
        Entity ignoredOccupancyEntity = IgnoredOccupancyEntities[index];
        int2 ignoredOccupancyCell = IgnoredOccupancyCells[index];
        int2 ignoredOccupancySize = IgnoredOccupancySizes[index];

        if (!GridUtils.InBounds(start, Grid.Width, Grid.Height))
        {
            Status[index] = 0;
            FailureCodes[index] = 1;
            Output.EndForEachIndex();
            return;
        }

        int startIndex = GridUtils.CellToIndex(start, Grid.Width);
        if (Walkable[startIndex].Value == 0)
        {
            Status[index] = 0;
            FailureCodes[index] = 2;
            Output.EndForEachIndex();
            return;
        }

        if (!IsSurfaceCellPathable(start, isVehicle))
        {
            Status[index] = 0;
            FailureCodes[index] = 3;
            Output.EndForEachIndex();
            return;
        }

        int searchEpoch = SearchEpochBase + (index * UnitPathScratchWorkspaceSystem.EpochsPerRequest);
        if (TryWritePath(index, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, goal, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch))
        {
            Status[index] = 1;
            Output.EndForEachIndex();
            return;
        }

        int2 searchCenter = goal;
        if (!GridUtils.InBounds(searchCenter, Grid.Width, Grid.Height))
        {
            searchCenter = new int2(
                math.clamp(searchCenter.x, 0, Grid.Width - 1),
                math.clamp(searchCenter.y, 0, Grid.Height - 1));
        }

        bool isFinalSegment = desiredGoal.Equals(goal);
        if (isFinalSegment && GridUtils.InBounds(desiredGoal, Grid.Width, Grid.Height))
            searchCenter = desiredGoal;

        int maxRadius = math.min(
            math.max(Grid.Width, Grid.Height),
            isVehicle ? UnitPathGoalAssignmentSystem.VehicleGoalSearchRadius : UnitPathGoalAssignmentSystem.InfantryGoalSearchRadius);
        int candidateAttempts = 0;
        if (!skipAlternateSearch)
        {
            int maxCandidateAttempts = isVehicle ? UnitPathGoalAssignmentSystem.VehicleAlternateGoalCandidates : UnitPathGoalAssignmentSystem.InfantryAlternateGoalCandidates;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                int ringLen = math.max(1, 8 * radius);
                for (int step = 0; step < ringLen; step++)
                {
                    if (candidateAttempts >= maxCandidateAttempts)
                    {
                        radius = maxRadius + 1;
                        break;
                    }

                    int2 candidate = searchCenter + SquareRingOffset(radius, step);
                    if (!GridUtils.InBounds(candidate, Grid.Width, Grid.Height))
                        continue;

                    if (candidate.Equals(goal) || candidate.Equals(start))
                        continue;

                    if (!CanReachGoalCell(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                        continue;

                    candidateAttempts++;
                    if (TryWritePath(index, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch + candidateAttempts + 1))
                    {
                        AlternateAttempts[index] = candidateAttempts;
                        Goals[index] = candidate;
                        Status[index] = 1;
                        Output.EndForEachIndex();
                        return;
                    }
                }
            }
        }

        AlternateAttempts[index] = candidateAttempts;

        if (manualMove &&
            TryWriteSegmentProgressFallback(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, desiredGoal, footprintSize, isVehicle, manualMove, factionId, out int2 fallbackGoal))
        {
            Goals[index] = fallbackGoal;
            Segmented[index] = 1;
            Status[index] = 1;
            Output.EndForEachIndex();
            return;
        }

        Status[index] = 0;
        if (FailureCodes[index] == 0)
            FailureCodes[index] = 12;
        Output.EndForEachIndex();
    }

    private bool TryWriteSegmentProgressFallback(
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        int2 start,
        int2 desiredGoal,
        int2 footprintSize,
        bool isVehicle,
        bool manualMove,
        byte factionId,
        out int2 fallbackGoal)
    {
        int bestDistanceSq = int.MaxValue;
        int2 bestCell = start;
        fallbackGoal = start;
        int maxRadius = math.min(16, math.max(Grid.Width, Grid.Height));

        for (int radius = maxRadius; radius >= 1; radius--)
        {
            int ringLen = math.max(1, 8 * radius);
            for (int step = 0; step < ringLen; step++)
            {
                int2 candidate = start + SquareRingOffset(radius, step);
                if (!GridUtils.InBounds(candidate, Grid.Width, Grid.Height))
                    continue;
                if (candidate.Equals(start))
                    continue;
                if (!CanReachGoalCell(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                    continue;
                if (!HasDirectPath(Grid, Walkable, DynamicBlocked, FriendlyPassFactionIds, Occupied, LiveUnitEntities, LiveUnitGrids, LiveUnitFootprints, LiveUnitManualGroupMembers, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                    continue;

                int dx = candidate.x - desiredGoal.x;
                int dy = candidate.y - desiredGoal.y;
                int distanceSq = (dx * dx) + (dy * dy);
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                bestCell = candidate;
            }

            if (!bestCell.Equals(start))
                break;
        }

        if (bestCell.Equals(start))
            return false;

        fallbackGoal = bestCell;
        Output.Write(start);
        Output.Write(bestCell);
        return true;
    }

    private bool CanReachGoalCell(Entity movingEntity, Entity ignoredOccupancyEntity, int2 ignoredOccupancyCell, int2 ignoredOccupancySize, int2 start, int2 goal, int2 footprintSize, bool isVehicle, bool manualMove, byte factionId)
    {
        int goalIndex = GridUtils.CellToIndex(goal, Grid.Width);
        if (Walkable[goalIndex].Value == 0)
            return false;

        if (!IsSurfaceCellPathable(goal, isVehicle))
            return false;

        return UnitPathPlacementValidationSystem.CanPlaceForPathing(
            Grid,
            Walkable,
            DynamicBlocked,
            FriendlyPassFactionIds,
            Occupied,
            LiveUnitEntities,
            LiveUnitGrids,
            LiveUnitFootprints,
            LiveUnitManualGroupMembers,
            movingEntity,
            goal,
            footprintSize,
            start,
            isVehicle,
            manualMove,
            factionId,
            ignoredOccupancyEntity,
            ignoredOccupancyCell,
            ignoredOccupancySize) &&
            IsSurfaceFootprintPathable(goal, footprintSize, isVehicle);
    }

    private bool TryWritePath(int index, Entity movingEntity, Entity ignoredOccupancyEntity, int2 ignoredOccupancyCell, int2 ignoredOccupancySize, int2 start, int2 goal, int2 footprintSize, bool isVehicle, bool manualMove, bool cheapSegmentMode, byte factionId, int searchEpoch)
    {
        if (!GridUtils.InBounds(goal, Grid.Width, Grid.Height))
        {
            FailureCodes[index] = 4;
            return false;
        }

        int startIndex = GridUtils.CellToIndex(start, Grid.Width);
        int goalIndex = GridUtils.CellToIndex(goal, Grid.Width);
        int searchBoundsPadding = isVehicle
            ? VehicleSearchBoundsPaddingCells
            : cheapSegmentMode ? InfantrySegmentedSearchBoundsPaddingCells : InfantrySearchBoundsPaddingCells;
        int minSearchX = math.max(0, math.min(start.x, goal.x) - searchBoundsPadding);
        int maxSearchX = math.min(Grid.Width - 1, math.max(start.x, goal.x) + searchBoundsPadding);
        int minSearchY = math.max(0, math.min(start.y, goal.y) - searchBoundsPadding);
        int maxSearchY = math.min(Grid.Height - 1, math.max(start.y, goal.y) + searchBoundsPadding);
        if (Walkable[goalIndex].Value == 0)
        {
            FailureCodes[index] = 5;
            return false;
        }

        if (!IsSurfaceCellPathable(goal, isVehicle))
        {
            FailureCodes[index] = 6;
            return false;
        }

        bool goalPlacementValid = UnitPathPlacementValidationSystem.CanPlaceForPathing(
            Grid,
            Walkable,
            DynamicBlocked,
            FriendlyPassFactionIds,
            Occupied,
            LiveUnitEntities,
            LiveUnitGrids,
            LiveUnitFootprints,
            LiveUnitManualGroupMembers,
            movingEntity,
            goal,
            footprintSize,
            start,
            isVehicle,
            manualMove,
            factionId,
            ignoredOccupancyEntity,
            ignoredOccupancyCell,
            ignoredOccupancySize);
        bool goalSurfaceFootprintValid = IsSurfaceFootprintPathable(goal, footprintSize, isVehicle);
        if (goalIndex != startIndex)
        {
            if (!goalPlacementValid)
            {
                FailureCodes[index] = 7;
                return false;
            }

            if (!goalSurfaceFootprintValid)
            {
                FailureCodes[index] = 8;
                return false;
            }
        }

        if (HasDirectPath(Grid, Walkable, DynamicBlocked, FriendlyPassFactionIds, Occupied, LiveUnitEntities, LiveUnitGrids, LiveUnitFootprints, LiveUnitManualGroupMembers, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, goal, footprintSize, isVehicle, manualMove, factionId))
        {
            Output.Write(start);
            if (!start.Equals(goal))
                Output.Write(goal);
            FailureCodes[index] = 0;
            ExpansionCounts[index] = 0;
            return true;
        }

        int threadOffset = 0;

        InitializeScratchNode(threadOffset, startIndex, searchEpoch);
        ScratchGScore[threadOffset + startIndex] = 0;
        int heapCount = 0;
        HeapPush(threadOffset, ref heapCount, PackHeapEntry(HeuristicOctile(start, goal), startIndex));
        int expansions = 0;
        int maxExpansions = isVehicle
            ? (manualMove ? VehicleManualMaxAStarExpansions : VehicleMaxAStarExpansions)
            : (cheapSegmentMode ? InfantrySegmentedMaxAStarExpansions : InfantryMaxAStarExpansions);

        bool found = false;

        while (heapCount > 0)
        {
            int current = UnpackHeapIndex(HeapPop(threadOffset, ref heapCount));
            if (ScratchClosed[threadOffset + current] != 0)
                continue; // stale duplicate heap entry (node already expanded with a better f)

            expansions++;
            if (expansions > maxExpansions)
            {
                FailureCodes[index] = 9;
                ExpansionCounts[index] = expansions;
                return false;
            }

            if (current == goalIndex)
            {
                found = true;
                break;
            }

            ScratchClosed[threadOffset + current] = 1;
            int2 currentCell = GridUtils.IndexToCell(current, Grid.Width);

            for (int d = 0; d < SearchDirs.Length; d++)
            {
                int2 nextCell = currentCell + SearchDirs[d];
                if (!GridUtils.InBounds(nextCell, Grid.Width, Grid.Height))
                    continue;
                if (nextCell.x < minSearchX || nextCell.x > maxSearchX || nextCell.y < minSearchY || nextCell.y > maxSearchY)
                    continue;

                int nextIndex = GridUtils.CellToIndex(nextCell, Grid.Width);
                InitializeScratchNode(threadOffset, nextIndex, searchEpoch);
                if (ScratchClosed[threadOffset + nextIndex] != 0)
                    continue;

                if (Walkable[nextIndex].Value == 0)
                    continue;

                if (!IsSurfaceCellPathable(nextCell, isVehicle))
                    continue;

                bool diagonalStep = nextCell.x != currentCell.x && nextCell.y != currentCell.y;
                if (diagonalStep)
                {
                    int2 horizontalCell = new int2(nextCell.x, currentCell.y);
                    int2 verticalCell = new int2(currentCell.x, nextCell.y);
                    bool canPlaceHorizontal = UnitPathPlacementValidationSystem.CanPlaceForPathing(
                        Grid,
                        Walkable,
                        DynamicBlocked,
                        FriendlyPassFactionIds,
                        Occupied,
                        LiveUnitEntities,
                        LiveUnitGrids,
                        LiveUnitFootprints,
                        LiveUnitManualGroupMembers,
                        movingEntity,
                        horizontalCell,
                        footprintSize,
                        currentCell,
                        isVehicle,
                        manualMove,
                        factionId,
                        ignoredOccupancyEntity,
                        ignoredOccupancyCell,
                        ignoredOccupancySize);
                    canPlaceHorizontal = canPlaceHorizontal && IsSurfaceFootprintPathable(horizontalCell, footprintSize, isVehicle);
                    bool canPlaceVertical = UnitPathPlacementValidationSystem.CanPlaceForPathing(
                        Grid,
                        Walkable,
                        DynamicBlocked,
                        FriendlyPassFactionIds,
                        Occupied,
                        LiveUnitEntities,
                        LiveUnitGrids,
                        LiveUnitFootprints,
                        LiveUnitManualGroupMembers,
                        movingEntity,
                        verticalCell,
                        footprintSize,
                        currentCell,
                        isVehicle,
                        manualMove,
                        factionId,
                        ignoredOccupancyEntity,
                        ignoredOccupancyCell,
                        ignoredOccupancySize);
                    canPlaceVertical = canPlaceVertical && IsSurfaceFootprintPathable(verticalCell, footprintSize, isVehicle);
                    if (!canPlaceHorizontal || !canPlaceVertical)
                        continue;
                }

                int addCost = GetTraversalCost(nextIndex, diagonalStep, isVehicle);
                bool canPlaceNext = UnitPathPlacementValidationSystem.CanPlaceForPathing(
                    Grid,
                    Walkable,
                    DynamicBlocked,
                    FriendlyPassFactionIds,
                    Occupied,
                    LiveUnitEntities,
                    LiveUnitGrids,
                    LiveUnitFootprints,
                    LiveUnitManualGroupMembers,
                    movingEntity,
                    nextCell,
                    footprintSize,
                    currentCell,
                    isVehicle,
                    manualMove,
                    factionId,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize);
                if (!canPlaceNext)
                    continue;
                if (!IsSurfaceFootprintPathable(nextCell, footprintSize, isVehicle))
                    continue;

                int currentG = ScratchGScore[threadOffset + current];
                int tentative = currentG + addCost;
                if (tentative >= ScratchGScore[threadOffset + nextIndex])
                    continue;

                ScratchCameFrom[threadOffset + nextIndex] = current;
                ScratchGScore[threadOffset + nextIndex] = tentative;

                if (heapCount < GridSize)
                    HeapPush(threadOffset, ref heapCount, PackHeapEntry(tentative + HeuristicOctile(nextCell, goal), nextIndex));
            }
        }

        if (!found)
        {
            FailureCodes[index] = 10;
            ExpansionCounts[index] = expansions;
            return false;
        }

        int pathLen = 0;
        int cur = goalIndex;

        while (cur >= 0 && pathLen < GridSize)
        {
            ScratchPath[threadOffset + pathLen] = cur;
            pathLen++;
            if (cur == startIndex)
                break;
            cur = ScratchCameFrom[threadOffset + cur];
        }

        if (pathLen == 0 || ScratchPath[threadOffset + (pathLen - 1)] != startIndex)
        {
            FailureCodes[index] = 11;
            ExpansionCounts[index] = expansions;
            return false;
        }

        bool keepFullPath = !isVehicle;
        for (int i = pathLen - 1; i >= 0; i--)
        {
            int cellIndex = ScratchPath[threadOffset + i];
            int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);

            bool isFirst = i == pathLen - 1;
            bool isLast = i == 0;
            if (keepFullPath || isFirst || isLast)
            {
                Output.Write(cell);
                continue;
            }

            int2 prevCell = GridUtils.IndexToCell(ScratchPath[threadOffset + i + 1], Grid.Width);
            int2 nextCell = GridUtils.IndexToCell(ScratchPath[threadOffset + i - 1], Grid.Width);
            int2 prevDir = cell - prevCell;
            int2 nextDir = nextCell - cell;

            if (!prevDir.Equals(nextDir))
                Output.Write(cell);
        }

        FailureCodes[index] = 0;
        ExpansionCounts[index] = expansions;
        return true;
    }

    private void InitializeScratchNode(int threadOffset, int cellIndex, int searchEpoch)
    {
        int offset = threadOffset + cellIndex;
        if (ScratchEpoch[offset] == searchEpoch)
            return;

        ScratchEpoch[offset] = searchEpoch;
        ScratchCameFrom[offset] = -1;
        ScratchGScore[offset] = int.MaxValue;
        ScratchClosed[offset] = 0;
        ScratchInOpen[offset] = 0;
    }

    private int GetTraversalCost(int cellIndex, bool diagonalStep, bool isVehicle)
    {
        bool isSidewalk = Sidewalks[cellIndex].Value != 0;
        bool isDirtRoad = DirtRoads[cellIndex].Value != 0;
        int slopeCost = GetSlopeTraversalCost(cellIndex);
        MapSurfaceRoadPriority roadPriority = SurfaceRoadPriority.ResolveGridRoadPriority(
            (byte)(isSidewalk ? 1 : 0),
            (byte)(isDirtRoad ? 1 : 0),
            isVehicle);

        if (roadPriority == MapSurfaceRoadPriority.Preferred)
            return (diagonalStep ? PreferredSurfaceDiagonalTraversalCost : PreferredSurfaceTraversalCost) + slopeCost;
        if (roadPriority == MapSurfaceRoadPriority.Avoided)
            return (diagonalStep ? AvoidedSurfaceDiagonalTraversalCost : AvoidedSurfaceTraversalCost) + slopeCost;

        return (diagonalStep ? FreeDiagonalTraversalCost : FreeTraversalCost) + slopeCost;
    }

    private int GetSlopeTraversalCost(int cellIndex)
    {
        if (HasMapSurface == 0 || MapSurfacePathCost.EnableSlopeCost == 0)
            return 0;

        int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);
        return SurfacePathCost.GetSlopeTraversalCost(MapSurface, HasMapSurface, MapSurfacePathCost, cell);
    }

    private bool HasDirectPath(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<byte> liveUnitManualGroupMembers,
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        int2 start,
        int2 goal,
        int2 footprintSize,
        bool isVehicle,
        bool manualMove,
        byte factionId)
    {
        int dx = math.abs(goal.x - start.x);
        int dy = math.abs(goal.y - start.y);
        int steps = math.max(dx, dy);
        if (steps <= 1)
            return true;

        int2 current = start;
        for (int step = 1; step <= steps; step++)
        {
            float t = (float)step / steps;
            int2 next = new int2(
                (int)math.round(math.lerp(start.x, goal.x, t)),
                (int)math.round(math.lerp(start.y, goal.y, t)));

            if (next.Equals(current))
                continue;

            if (!UnitPathPlacementValidationSystem.CanPlaceForPathing(grid, walkable, dynamicBlocked, friendlyPassFactionIds, occupied, liveUnitEntities, liveUnitGrids, liveUnitFootprints, liveUnitManualGroupMembers, movingEntity, next, footprintSize, current, isVehicle, manualMove, factionId, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize))
                return false;

            if (!IsSurfaceFootprintPathable(next, footprintSize, isVehicle))
                return false;

            current = next;
        }

        return true;
    }

    private bool IsSurfaceCellPathable(int2 cell, bool isVehicle)
    {
        MapSurfaceMovementMask movementMask = SurfaceValidation.ResolveMovementMask(isVehicle);
        return SurfaceValidation.CanTraverse(MapSurface, HasMapSurface, cell, movementMask);
    }

    private bool IsSurfaceFootprintPathable(int2 cell, int2 footprintSize, bool isVehicle)
    {
        return SurfaceValidation.CanTraverseFootprint(MapSurface, HasMapSurface, Grid, cell, footprintSize, isVehicle);
    }

    // Binary min-heap over packed (fScore << 32 | cellIndex) entries stored in ScratchOpen.
    // Relaxations push duplicate entries instead of decrease-key; stale entries are
    // skipped on pop via the Closed check. Packing keeps the comparison branch-free and
    // makes tie-breaking on cell index deterministic.
    private static long PackHeapEntry(int fScore, int cellIndex)
    {
        return ((long)fScore << 32) | (uint)cellIndex;
    }

    private static int UnpackHeapIndex(long entry)
    {
        return (int)(entry & 0xFFFFFFFF);
    }

    private void HeapPush(int threadOffset, ref int heapCount, long entry)
    {
        int child = heapCount;
        heapCount++;
        while (child > 0)
        {
            int parent = (child - 1) >> 1;
            long parentEntry = ScratchOpen[threadOffset + parent];
            if (parentEntry <= entry)
                break;
            ScratchOpen[threadOffset + child] = parentEntry;
            child = parent;
        }

        ScratchOpen[threadOffset + child] = entry;
    }

    private long HeapPop(int threadOffset, ref int heapCount)
    {
        long root = ScratchOpen[threadOffset];
        heapCount--;
        long last = ScratchOpen[threadOffset + heapCount];
        int parent = 0;
        while (true)
        {
            int left = (parent << 1) + 1;
            if (left >= heapCount)
                break;
            int right = left + 1;
            int smallest = right < heapCount && ScratchOpen[threadOffset + right] < ScratchOpen[threadOffset + left]
                ? right
                : left;
            if (ScratchOpen[threadOffset + smallest] >= last)
                break;
            ScratchOpen[threadOffset + parent] = ScratchOpen[threadOffset + smallest];
            parent = smallest;
        }

        ScratchOpen[threadOffset + parent] = last;
        return root;
    }

    private static int HeuristicOctile(int2 a, int2 b)
    {
        int dx = math.abs(a.x - b.x);
        int dy = math.abs(a.y - b.y);
        int diagonal = math.min(dx, dy);
        int straight = math.max(dx, dy) - diagonal;
        return (diagonal * FreeDiagonalTraversalCost) + (straight * FreeTraversalCost);
    }

    private static int2 SquareRingOffset(int r, int step)
    {
        int topLen = (2 * r) + 1;
        if (step < topLen)
            return new int2(-r + step, r);

        step -= topLen;
        int rightLen = 2 * r;
        if (step < rightLen)
            return new int2(r, (r - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * r;
        if (step < bottomLen)
            return new int2((r - 1) - step, -r);

        step -= bottomLen;
        return new int2(-r, (-r + 1) + step);
    }
}
