using Unity.Collections;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Pathfinding;

namespace Game.Runtime
{
    internal struct UnitPathReservedGoal
    {
        public NativeArray<int> Epochs;
        public int Generation;

        private int _gridSize;

        public void Prepare(int gridSize)
        {
            var decision=PathGoalReservationKernel.DecidePrepare(Epochs.IsCreated,_gridSize,Generation,gridSize);
            if (decision.Reallocate)
            {
                Dispose();
                Epochs = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            }
            else if(decision.Clear) Epochs.AsSpan().Clear();
            Generation=decision.Generation;
            _gridSize=decision.RememberedGridSize;
        }

        public void Dispose()
        {
            if (Epochs.IsCreated)
                Epochs.Dispose();
            _gridSize = 0;
            Generation = 0;
        }

        public static void ReserveGoalFootprint(
            GridConfig grid,
            NativeArray<int> reservedGoalEpochs,
            int reservedGoalGeneration,
            int2 cell,
            int2 footprintSize)
        {
            var storage=new ReservationStorage { Epochs=reservedGoalEpochs };
            PathGoalReservationKernel.ReserveFootprint(ref storage,grid.Width,reservedGoalGeneration,
                new PlacementCell(cell.x,cell.y),new PlacementCell(footprintSize.x,footprintSize.y));
        }
        private struct ReservationStorage : IPathGoalReservationStorage
        {
            public NativeArray<int> Epochs;
            public void SetEpoch(int index,int generation) => Epochs[index]=generation;
        }
    }
}
