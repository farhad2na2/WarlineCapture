using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Game.Runtime.Pathfinding;
using Game.Components;

namespace Game.Runtime
{
    internal struct UnitPathLiveUnitSnapshot
    {
        private NativeList<Entity> _entities;
        private NativeList<UnitGrid> _grids;
        private NativeList<UnitFootprint> _footprints;
        private NativeList<byte> _manualGroupMembers;
        private NativeBitArray _coverageCells;
        private PathUnitCoverageGrid _coverageGrid;
        private bool _coverageComplete;
        public UnitPathPlacementValidation.UnitCoverage Coverage => new UnitPathPlacementValidation.UnitCoverage(_coverageCells,_coverageGrid,_coverageComplete);
        private struct CoverageWriter : IPathUnitCoverageStorage
        {
            public NativeBitArray Cells;
            public bool IsMarked(int i)=>Cells.IsSet(i);
            public void Mark(int i)=>Cells.Set(i,true);
        }
        private void BuildCoverage(in GridConfig grid)
        {
            _coverageComplete=false;
            if(!PathUnitCoverageKernel.TryCreateGrid(grid.Width,grid.Height,out _coverageGrid))return;
            if(!_coverageCells.IsCreated||_coverageCells.Length!=_coverageGrid.TileCount)
            {
                if(_coverageCells.IsCreated)_coverageCells.Dispose();
                _coverageCells=new NativeBitArray(_coverageGrid.TileCount,Allocator.Persistent,NativeArrayOptions.ClearMemory);
            }
            else _coverageCells.Clear();
            var writer=new CoverageWriter{Cells=_coverageCells};int writesRemaining=_coverageGrid.TileCount;
            for(int i=0;i<_entities.Length;i++)
            {
                int2 cell=_grids[i].Cell,size=_footprints[i].Size;
                if(!PathUnitCoverageKernel.TryAppend(in _coverageGrid,ref writer,cell.x,cell.y,size.x,size.y,ref writesRemaining))return;
            }
            _coverageComplete=true;
        }
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<UnitGrid> _gridType;
        private ComponentTypeHandle<UnitFootprint> _footprintType;
        private ComponentLookup<ManualMoveGroupMemberTag> _manualGroupLookup;

        public NativeArray<Entity> Entities => _entities.IsCreated ? _entities.AsArray() : default;
        public NativeArray<UnitGrid> Grids => _grids.IsCreated ? _grids.AsArray() : default;
        public NativeArray<UnitFootprint> Footprints => _footprints.IsCreated ? _footprints.AsArray() : default;
        public NativeArray<byte> ManualGroupMembers => _manualGroupMembers.IsCreated ? _manualGroupMembers.AsArray() : default;

        public int Count => _entities.IsCreated ? _entities.Length : 0;

        public void Initialize(ref SystemState state)
        {
            _entityType = state.GetEntityTypeHandle();
            _gridType = state.GetComponentTypeHandle<UnitGrid>(true);
            _footprintType = state.GetComponentTypeHandle<UnitFootprint>(true);
            _manualGroupLookup = state.GetComponentLookup<ManualMoveGroupMemberTag>(true);
        }

        public void Capture(ref SystemState state, EntityQuery liveUnitsQuery, in GridConfig grid)
        {
            _coverageComplete=false;
            int capacity = liveUnitsQuery.CalculateEntityCount();
            if (capacity < 1)
                capacity = 1;

            EnsureList(ref _entities, capacity);
            EnsureList(ref _grids, capacity);
            EnsureList(ref _footprints, capacity);
            EnsureList(ref _manualGroupMembers, capacity);

            state.EntityManager.CompleteDependencyBeforeRO<UnitGrid>();
            state.EntityManager.CompleteDependencyBeforeRO<UnitFootprint>();
            state.EntityManager.CompleteDependencyBeforeRO<ManualMoveGroupMemberTag>();
            _entityType.Update(ref state);
            _gridType.Update(ref state);
            _footprintType.Update(ref state);
            _manualGroupLookup.Update(ref state);

            using NativeArray<ArchetypeChunk> chunks = liveUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(_entityType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref _gridType);
                NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref _footprintType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    _entities.Add(entity);
                    _grids.Add(grids[i]);
                    _footprints.Add(footprints[i]);
                    _manualGroupMembers.Add((byte)(_manualGroupLookup.HasComponent(entity) ? 1 : 0));
                }
            }
            BuildCoverage(grid);
        }

        public void Dispose()
        {
            _coverageComplete=false;
            if(_coverageCells.IsCreated)_coverageCells.Dispose();
            _coverageCells=default;
            _coverageGrid=default;
            if (_entities.IsCreated)
                _entities.Dispose();
            if (_grids.IsCreated)
                _grids.Dispose();
            if (_footprints.IsCreated)
                _footprints.Dispose();
            if (_manualGroupMembers.IsCreated)
                _manualGroupMembers.Dispose();
        }

        private static void EnsureList<T>(ref NativeList<T> list, int capacity) where T : unmanaged
        {
            if (!list.IsCreated)
            {
                list = new NativeList<T>(capacity, Allocator.Persistent);
                return;
            }

            list.Clear();
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }
    }
}
