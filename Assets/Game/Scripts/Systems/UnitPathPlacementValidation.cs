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
        internal readonly struct UnitCoverage
        {
            private readonly NativeBitArray cells;
            private readonly PathUnitCoverageGrid grid;
            private readonly byte complete;
            internal UnitCoverage(NativeBitArray cells,PathUnitCoverageGrid grid,bool complete)
            { this.cells=cells;this.grid=grid;this.complete=(byte)(complete?1:0); }
            private struct Reader : IPathUnitCoverageStorage
            {
                public NativeBitArray Cells;
                public bool IsMarked(int i)=>Cells.IsSet(i);
                public void Mark(int i)=>Cells.Set(i,true);
            }
            internal bool CanSkip(in GridConfig actual,int2 cell,int2 footprint)
            {
                if(complete==0||!cells.IsCreated||cells.Length!=grid.TileCount||grid.Width!=actual.Width||grid.Height!=actual.Height)return false;
                var reader=new Reader{Cells=cells};
                return !PathUnitCoverageKernel.MayOverlap(in grid,ref reader,cell.x,cell.y,footprint.x,footprint.y);
            }
        }
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
            int2 ignoredOccupancySize = default,
            UnitCoverage unitCoverage = default)
        {
            var context = new NativeContext { Grid=grid, WalkableCells=walkable,BlockedCells=dynamicBlocked,FriendlyIds=friendlyPassFactionIds,OccupiedCells=occupied,Entities=liveUnitEntities,Grids=liveUnitGrids,Footprints=liveUnitFootprints,Groups=liveUnitManualGroupMembers };
            return PathPlacementKernel.CanPlaceForPathingWithCoverage(ref context,Identity(movingEntity),Cell(cell),Cell(footprintSize),Cell(currentCell),isVehicle,manualMove,factionId,Identity(ignoredOccupancyEntity),Cell(ignoredOccupancyCell),Cell(ignoredOccupancySize),unitCoverage.CanSkip(grid,cell,footprintSize));
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
