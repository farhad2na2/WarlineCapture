using Unity.Collections;
using Unity.Entities;

internal struct UnitPathLiveUnitSnapshotSystem
{
    private NativeList<Entity> _entities;
    private NativeList<UnitGrid> _grids;
    private NativeList<UnitFootprint> _footprints;
    private NativeList<byte> _manualGroupMembers;
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

    public void Capture(ref SystemState state, EntityQuery liveUnitsQuery)
    {
        Dispose();

        int capacity = liveUnitsQuery.CalculateEntityCount();
        if (capacity < 1)
            capacity = 1;

        _entities = new NativeList<Entity>(capacity, Allocator.Persistent);
        _grids = new NativeList<UnitGrid>(capacity, Allocator.Persistent);
        _footprints = new NativeList<UnitFootprint>(capacity, Allocator.Persistent);
        _manualGroupMembers = new NativeList<byte>(capacity, Allocator.Persistent);

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
    }

    public void Dispose()
    {
        if (_entities.IsCreated)
            _entities.Dispose();
        if (_grids.IsCreated)
            _grids.Dispose();
        if (_footprints.IsCreated)
            _footprints.Dispose();
        if (_manualGroupMembers.IsCreated)
            _manualGroupMembers.Dispose();
    }
}
