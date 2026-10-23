using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Pathfinding;

namespace Game.Runtime
{
    [BurstCompile]
    internal struct PathfindBatchJob : IJobFor
    {
        private const int FreeTraversalCost = GridPathSearchKernel.FreeTraversalCost;
        private const int FreeDiagonalTraversalCost = GridPathSearchKernel.FreeDiagonalTraversalCost;
        private const int PreferredSurfaceTraversalCost = GridPathSearchKernel.PreferredSurfaceTraversalCost;
        private const int PreferredSurfaceDiagonalTraversalCost = GridPathSearchKernel.PreferredSurfaceDiagonalTraversalCost;
        private const int AvoidedSurfaceTraversalCost = GridPathSearchKernel.AvoidedSurfaceTraversalCost;
        private const int AvoidedSurfaceDiagonalTraversalCost = GridPathSearchKernel.AvoidedSurfaceDiagonalTraversalCost;
        private const int OccupiedTraversalPenalty = 50;
        private const int InfantryMaxAStarExpansions = GridPathSearchKernel.InfantryMaxAStarExpansions;
        private const int InfantrySegmentedMaxAStarExpansions = GridPathSearchKernel.InfantrySegmentedMaxAStarExpansions;
        private const int VehicleMaxAStarExpansions = GridPathSearchKernel.VehicleMaxAStarExpansions;
        private const int VehicleManualMaxAStarExpansions = GridPathSearchKernel.VehicleManualMaxAStarExpansions;
        private const int InfantrySearchBoundsPaddingCells = GridPathSearchKernel.InfantrySearchBoundsPaddingCells;
        private const int InfantrySegmentedSearchBoundsPaddingCells = GridPathSearchKernel.InfantrySegmentedSearchBoundsPaddingCells;
        private const int VehicleSearchBoundsPaddingCells = GridPathSearchKernel.VehicleSearchBoundsPaddingCells;
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
        public MapSurfaceTraversalValidation SurfaceValidation;
        [ReadOnly] public MapSurfacePathCostComponent MapSurfacePathCost;
        public MapSurfacePathCost SurfacePathCost;
        public MapSurfaceRoadPriorityPolicy SurfaceRoadPriority;
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

            int searchEpoch = SearchEpochBase + (index * UnitPathScratchWorkspace.EpochsPerRequest);
            if (TryWritePath(index, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, goal, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch))
            {
                Status[index] = 1;
                Output.EndForEachIndex();
                return;
            }

            // A nearby industrial bay can require a much longer detour through
            // the town. Repeating the same exhausted search leaves automatic
            // haulers stationary forever. Keep the normal route first, then
            // make one goal-directed retry within the same per-search limit.
            // This trades shortest-route optimality for reaching the real bay;
            // every footprint, surface and occupancy check still applies.
            bool detailedHaulerSearch = CheapSegmentModes.IsCreated && CheapSegmentModes[index] == 3;
            if (detailedHaulerSearch && FailureCodes[index] == 9 &&
                TryWritePath(index, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize,
                    start, goal, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch + 1, 2))
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
                isVehicle ? UnitPathGoalAssignment.VehicleGoalSearchRadius : UnitPathGoalAssignment.InfantryGoalSearchRadius);
            int candidateAttempts = 0;
            if (!skipAlternateSearch)
            {
                int maxCandidateAttempts = isVehicle ? UnitPathGoalAssignment.VehicleAlternateGoalCandidates : UnitPathGoalAssignment.InfantryAlternateGoalCandidates;
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

            return UnitPathPlacementValidation.CanPlaceForPathing(
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

        private unsafe bool TryWritePath(int index, Entity movingEntity, Entity ignoredOccupancyEntity, int2 ignoredOccupancyCell, int2 ignoredOccupancySize, int2 start, int2 goal, int2 footprintSize, bool isVehicle, bool manualMove, bool cheapSegmentMode, byte factionId, int searchEpoch, int heuristicWeight = 1)
        {
            bool hasModes = CheapSegmentModes.IsCreated;
            byte mode = hasModes ? CheapSegmentModes[index] : (byte)0;
            fixed (NativeStream.Writer* writer = &Output)
            {
                var context = new NativeSearchContext
                {
                    Job = this, Writer = writer, MovingEntity = movingEntity, IgnoredEntity = ignoredOccupancyEntity,
                    IgnoredCell = ignoredOccupancyCell, IgnoredSize = ignoredOccupancySize,
                    FootprintSize = footprintSize, IsVehicle = isVehicle, ManualMove = manualMove, FactionId = factionId
                };
                var request = new PathSearchRequest
                {
                    Width = Grid.Width, Height = Grid.Height, GridSize = GridSize,
                    StartX = start.x, StartZ = start.y, GoalX = goal.x, GoalZ = goal.y,
                    IsVehicle = isVehicle, SearchEpoch = searchEpoch, HeuristicWeight = heuristicWeight,
                    SearchBoundsPadding = GridPathSearchKernel.ResolveSearchPadding(isVehicle, cheapSegmentMode, hasModes, mode),
                    MaxExpansions = GridPathSearchKernel.ResolveExpansionLimit(isVehicle, manualMove, cheapSegmentMode, hasModes, mode)
                };
                var result = GridPathSearchKernel.TrySearch(ref context, request);
                FailureCodes[index] = result.FailureCode;
                if (result.SetExpansionCount) ExpansionCounts[index] = result.ExpansionCount;
                return result.Found;
            }
        }

        private unsafe struct NativeSearchContext : IPathSearchContext
        {
            public PathfindBatchJob Job;
            public NativeStream.Writer* Writer;
            public Entity MovingEntity, IgnoredEntity;
            public int2 IgnoredCell, IgnoredSize, FootprintSize;
            public bool IsVehicle, ManualMove;
            public byte FactionId;
            public bool IsWalkable(int index) => Job.Walkable[index].Value != 0;
            public bool IsSurfaceCellPathable(int x, int z) => Job.IsSurfaceCellPathable(new int2(x, z), IsVehicle);
            public bool IsSurfaceFootprintPathable(int x, int z) => Job.IsSurfaceFootprintPathable(new int2(x, z), FootprintSize, IsVehicle);
            public bool CanPlace(int x, int z, int cx, int cz) => UnitPathPlacementValidation.CanPlaceForPathing(
                Job.Grid, Job.Walkable, Job.DynamicBlocked, Job.FriendlyPassFactionIds, Job.Occupied,
                Job.LiveUnitEntities, Job.LiveUnitGrids, Job.LiveUnitFootprints, Job.LiveUnitManualGroupMembers,
                MovingEntity, new int2(x, z), FootprintSize, new int2(cx, cz), IsVehicle, ManualMove, FactionId,
                IgnoredEntity, IgnoredCell, IgnoredSize);
            public int TraversalCost(int index, bool diagonal) => Job.GetTraversalCost(index, diagonal, IsVehicle);
            public int Epoch(int index) => Job.ScratchEpoch[index];
            public void SetEpoch(int index, int value) => Job.ScratchEpoch[index] = value;
            public int CameFrom(int index) => Job.ScratchCameFrom[index];
            public void SetCameFrom(int index, int value) => Job.ScratchCameFrom[index] = value;
            public int GScore(int index) => Job.ScratchGScore[index];
            public void SetGScore(int index, int value) => Job.ScratchGScore[index] = value;
            public byte Closed(int index) => Job.ScratchClosed[index];
            public void SetClosed(int index, byte value) => Job.ScratchClosed[index] = value;
            public void SetInOpen(int index, byte value) => Job.ScratchInOpen[index] = value;
            public long Open(int index) => Job.ScratchOpen[index];
            public void SetOpen(int index, long value) => Job.ScratchOpen[index] = value;
            public int Path(int index) => Job.ScratchPath[index];
            public void SetPath(int index, int value) => Job.ScratchPath[index] = value;
            public void WriteCell(int x, int z) => Writer->Write(new int2(x, z));
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
            var context = new NativeSearchContext
            {
                Job = this, MovingEntity = movingEntity, IgnoredEntity = ignoredOccupancyEntity,
                IgnoredCell = ignoredOccupancyCell, IgnoredSize = ignoredOccupancySize,
                FootprintSize = footprintSize, IsVehicle = isVehicle, ManualMove = manualMove, FactionId = factionId
            };
            return GridPathSearchKernel.HasDirectPath(ref context, start.x, start.y, goal.x, goal.y);
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

        private static int HeuristicOctile(int2 a, int2 b) => GridPathSearchKernel.HeuristicOctile(a.x, a.y, b.x, b.y);

        private static int2 SquareRingOffset(int r, int step)
        {
            var offset = GridPathSearchKernel.SquareRingOffset(r, step);
            return new int2(offset.X, offset.Z);
        }
    }
}
