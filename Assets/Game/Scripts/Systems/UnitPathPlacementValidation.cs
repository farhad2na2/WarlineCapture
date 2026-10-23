using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Pathfinding;

namespace Game.Runtime
{
    internal struct UnitPathPlacementValidation
    {
        public const int VehicleOccupancyPaddingCells = PathPlacementKernel.VehicleOccupancyPaddingCells;
        public static bool CanPlaceForPathing(
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
            int2 cell,
            int2 footprintSize,
            int2 currentCell,
            bool isVehicle,
            bool manualMove,
            byte factionId,
            Entity ignoredOccupancyEntity = default,
            int2 ignoredOccupancyCell = default,
            int2 ignoredOccupancySize = default)
        {
            var context = new NativeContext { Grid=grid, WalkableCells=walkable,BlockedCells=dynamicBlocked,FriendlyIds=friendlyPassFactionIds,OccupiedCells=occupied,Entities=liveUnitEntities,Grids=liveUnitGrids,Footprints=liveUnitFootprints,Groups=liveUnitManualGroupMembers };
            return PathPlacementKernel.CanPlaceForPathing(ref context,Identity(movingEntity),Cell(cell),Cell(footprintSize),Cell(currentCell),isVehicle,manualMove,factionId,Identity(ignoredOccupancyEntity),Cell(ignoredOccupancyCell),Cell(ignoredOccupancySize));
        }
        private struct NativeContext : IPathPlacementContext
        {
            public GridConfig Grid;
            public NativeArray<GridWalkable> WalkableCells;
            public NativeBitArray BlockedCells, OccupiedCells;
            public NativeArray<byte> FriendlyIds, Groups;
            public NativeArray<Entity> Entities;
            public NativeArray<UnitGrid> Grids;
            public NativeArray<UnitFootprint> Footprints;
            public int Width=>Grid.Width; public int Height=>Grid.Height;
            public bool HasDynamicBlocked=>BlockedCells.IsCreated; public bool HasOccupied=>OccupiedCells.IsCreated; public bool HasFriendlyPass=>FriendlyIds.IsCreated;
            public int FriendlyPassLength=>FriendlyIds.Length; public int UnitCount=>Entities.Length; public int ManualGroupCount=>Groups.Length;
            public byte Walkable(int i)=>WalkableCells[i].Value;
            public bool DynamicBlocked(int i)=>BlockedCells.IsSet(i); public bool Occupied(int i)=>OccupiedCells.IsSet(i); public byte FriendlyPass(int i)=>FriendlyIds[i];
            public PlacementIdentity UnitEntity(int i)=>Identity(Entities[i]);
            public PlacementCell UnitCell(int i)=>Cell(Grids[i].Cell); public PlacementCell UnitSize(int i)=>Cell(Footprints[i].Size); public byte ManualGroup(int i)=>Groups[i];
        }
        private static PlacementIdentity Identity(Entity e)=>new PlacementIdentity(e.Index,e.Version);
        private static PlacementCell Cell(int2 c)=>new PlacementCell(c.x,c.y);
    }
}
