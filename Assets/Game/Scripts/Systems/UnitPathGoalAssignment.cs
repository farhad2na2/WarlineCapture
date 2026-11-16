using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Pathfinding;

namespace Game.Runtime
{
    internal struct UnitPathGoalAssignment
    {
        public const int InfantryGoalSearchRadius = PathGoalPolicyKernel.InfantryGoalSearchRadius;
        public const int VehicleGoalSearchRadius = PathGoalPolicyKernel.VehicleGoalSearchRadius;
        public const int InfantryAlternateGoalCandidates = PathGoalPolicyKernel.InfantryAlternateGoalCandidates;
        public const int VehicleAlternateGoalCandidates = PathGoalPolicyKernel.VehicleAlternateGoalCandidates;

        public int2 FindNearestFreeGoal(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<int> reservedGoalEpochs,
            int reservedGoalGeneration,
            Entity movingEntity,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            MapSurfacePathfindingSnapshot.Context surfaceContext,
            int2 desiredGoal,
            int2 startCell,
            int2 footprintSize,
            byte factionId,
            int startIndex)
        {
            var identityContext = new GoalIdentityContext
            {
                Grid=grid, Walkable=walkable, DynamicBlocked=dynamicBlocked, Friendly=friendlyPassFactionIds, Occupied=occupied,
                Entities=liveUnitEntities, Grids=liveUnitGrids, Footprints=liveUnitFootprints, Reserved=reservedGoalEpochs,
                Generation=reservedGoalGeneration, Moving=movingEntity, Ignored=ignoredOccupancyEntity,
                IgnoredCell=ignoredOccupancyCell, IgnoredSize=ignoredOccupancySize, Surface=surfaceContext,
                Footprint=footprintSize, Start=startCell, Faction=factionId
            };
            var goal = PathGoalAssignmentKernel.FindNearestFreeGoal(ref identityContext,
                new PlacementCell(desiredGoal.x,desiredGoal.y),new PlacementCell(startCell.x,startCell.y),
                new PlacementCell(footprintSize.x,footprintSize.y),startIndex);
            return new int2(goal.x,goal.y);
        }

        private struct GoalIdentityContext : IPathGoalIdentityContext, IPathGoalCandidateContext
        {
            public GridConfig Grid;
            public NativeArray<GridWalkable> Walkable;
            public NativeBitArray DynamicBlocked, Occupied;
            public NativeArray<byte> Friendly;
            public NativeArray<Entity> Entities;
            public NativeArray<UnitGrid> Grids;
            public NativeArray<UnitFootprint> Footprints;
            public NativeArray<int> Reserved;
            public int Generation;
            public Entity Moving, Ignored;
            public int2 IgnoredCell, IgnoredSize, Footprint, Start;
            public MapSurfacePathfindingSnapshot.Context Surface;
            public byte Faction;
            public int Width => Grid.Width;
            public int Height => Grid.Height;
            public bool CanUseGoal(PlacementCell cell) => PathGoalAssignmentKernel.CanUseGoal(ref this,cell,
                new PlacementCell(Footprint.x,Footprint.y),Generation);
            public bool CanPlaceGoal(PlacementCell cell) => UnitPathPlacementValidation.CanPlaceForPathing(
                Grid,Walkable,DynamicBlocked,Friendly,Occupied,Entities,Grids,Footprints,default,Moving,
                new int2(cell.x,cell.y),Footprint,Start,Footprint.x>1||Footprint.y>1,false,Faction,Ignored,IgnoredCell,IgnoredSize);
            public bool CanTraverseGoalSurface(PlacementCell cell) => new MapSurfaceTraversalValidation().CanTraverseFootprint(
                Surface.Surface,Surface.HasSurfaceData,Grid,new int2(cell.x,cell.y),Footprint,Footprint.x>1||Footprint.y>1);
            public int ReservedEpoch(int index) => Reserved[index];
            public void ReserveGoal(PlacementCell cell) => UnitPathReservedGoal.ReserveGoalFootprint(
                Grid,Reserved,Generation,new int2(cell.x,cell.y),Footprint);
        }

        public static bool CanUseGoalCell(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<int> reservedGoalEpochs,
            int reservedGoalGeneration,
            Entity movingEntity,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            MapSurfacePathfindingSnapshot.Context surfaceContext,
            int2 cell,
            int2 footprintSize,
            int2 startCell,
            byte factionId)
        {
            var context = new GoalIdentityContext
            {
                Grid=grid, Walkable=walkable, DynamicBlocked=dynamicBlocked, Friendly=friendlyPassFactionIds, Occupied=occupied,
                Entities=liveUnitEntities, Grids=liveUnitGrids, Footprints=liveUnitFootprints, Reserved=reservedGoalEpochs,
                Generation=reservedGoalGeneration, Moving=movingEntity, Ignored=ignoredOccupancyEntity,
                IgnoredCell=ignoredOccupancyCell, IgnoredSize=ignoredOccupancySize, Surface=surfaceContext,
                Footprint=footprintSize, Start=startCell, Faction=factionId
            };
            return PathGoalAssignmentKernel.CanUseGoal(ref context,new PlacementCell(cell.x,cell.y),
                new PlacementCell(footprintSize.x,footprintSize.y),reservedGoalGeneration);
        }

        public static bool IsFree(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            int cellIndex,
            byte factionId) =>
            (uint)cellIndex < (uint)(grid.Width * grid.Height) &&
            walkable[cellIndex].Value != 0 &&
            (!dynamicBlocked.IsSet(cellIndex) ||
             ((uint)cellIndex < (uint)friendlyPassFactionIds.Length && friendlyPassFactionIds[cellIndex] == factionId)) &&
            !occupied.IsSet(cellIndex);

        public static int2 SquareRingOffset(int r, int step)
        {
            var cell=PathGoalAssignmentKernel.SquareRingOffset(r,step);
            return new int2(cell.x,cell.y);
        }
    }
}
