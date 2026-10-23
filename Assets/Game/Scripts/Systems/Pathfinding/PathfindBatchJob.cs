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

        public unsafe void Execute(int index)
        {
            // The stream writer must retain its exact address from Begin through End.
            // Pin only for this synchronous call; no context or pointer escapes.
            fixed (NativeStream.Writer* writer = &Output)
            {
                writer->BeginForEachIndex(index);
                int2 start = UnitGrids[index].Cell;
                UnitFootprint footprint = Footprints[index];
                int2 desired = RequestedGoals[index].Goal, goal = Goals[index];
                bool vehicle = UnitVehicleMovementUtility.IsVehicle(footprint, MovementBehaviors[index]);
                bool manual = ManualMoves[index] != 0, cheap = CheapSegmentModes[index] != 0;
                bool hasModes = CheapSegmentModes.IsCreated;
                byte mode = hasModes ? CheapSegmentModes[index] : (byte)0;
                var context = new NativeSearchContext
                {
                    Job = this, Writer = writer, MovingEntity = Entities[index], IgnoredEntity = IgnoredOccupancyEntities[index],
                    IgnoredCell = IgnoredOccupancyCells[index], IgnoredSize = IgnoredOccupancySizes[index],
                    FootprintSize = footprint.Size, IsVehicle = vehicle, ManualMove = manual, FactionId = Factions[index]
                };
                var request = new PathRoutingRequest
                {
                    Search = new PathSearchRequest
                    {
                        Width = Grid.Width, Height = Grid.Height, GridSize = GridSize,
                        StartX = start.x, StartZ = start.y, GoalX = goal.x, GoalZ = goal.y,
                        SearchEpoch = unchecked(SearchEpochBase + index * UnitPathScratchWorkspace.EpochsPerRequest),
                        IsVehicle = vehicle, HeuristicWeight = 1,
                        SearchBoundsPadding = GridPathSearchKernel.ResolveSearchPadding(vehicle, cheap, hasModes, mode),
                        MaxExpansions = GridPathSearchKernel.ResolveExpansionLimit(vehicle, manual, cheap, hasModes, mode)
                    },
                    DesiredGoalX = desired.x, DesiredGoalZ = desired.y,
                    CheapMode = mode, HasModes = hasModes, ManualMove = manual,
                    SkipAlternateSearch = AlternateSearchSkipped[index] != 0,
                    GoalSearchRadius = vehicle ? UnitPathGoalAssignment.VehicleGoalSearchRadius : UnitPathGoalAssignment.InfantryGoalSearchRadius,
                    MaxCandidateAttempts = vehicle ? UnitPathGoalAssignment.VehicleAlternateGoalCandidates : UnitPathGoalAssignment.InfantryAlternateGoalCandidates
                };
                var state = new PathRoutingState
                {
                    GoalX = goal.x, GoalZ = goal.y, Status = Status[index], FailureCode = FailureCodes[index],
                    ExpansionCount = ExpansionCounts[index], AlternateAttempts = AlternateAttempts[index], Segmented = Segmented[index]
                };
                GridPathRoutingKernel.Route(ref context, request, ref state);
                Goals[index] = new int2(state.GoalX, state.GoalZ); Status[index] = state.Status;
                FailureCodes[index] = state.FailureCode; ExpansionCounts[index] = state.ExpansionCount;
                AlternateAttempts[index] = state.AlternateAttempts; Segmented[index] = state.Segmented;
                writer->EndForEachIndex();
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

        private bool IsSurfaceCellPathable(int2 cell, bool isVehicle)
        {
            MapSurfaceMovementMask movementMask = SurfaceValidation.ResolveMovementMask(isVehicle);
            return SurfaceValidation.CanTraverse(MapSurface, HasMapSurface, cell, movementMask);
        }

        private bool IsSurfaceFootprintPathable(int2 cell, int2 footprintSize, bool isVehicle)
        {
            return SurfaceValidation.CanTraverseFootprint(MapSurface, HasMapSurface, Grid, cell, footprintSize, isVehicle);
        }

    }
}
