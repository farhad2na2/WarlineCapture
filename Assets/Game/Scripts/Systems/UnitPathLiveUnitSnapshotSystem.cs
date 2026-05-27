using Unity.Collections;
using Unity.Entities;

internal struct UnitPathLiveUnitSnapshotSystem
{
    public NativeArray<Entity> Entities;
    public NativeArray<UnitGrid> Grids;
    public NativeArray<UnitFootprint> Footprints;
    public NativeArray<byte> ManualGroupMembers;

    public int Count => Entities.IsCreated ? Entities.Length : 0;

    public void Capture(ref SystemState state, EntityQuery liveUnitsQuery)
    {
        Dispose();

        Entities = liveUnitsQuery.ToEntityArray(Allocator.Persistent);
        Grids = liveUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Persistent);
        Footprints = liveUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Persistent);
        ManualGroupMembers = new NativeArray<byte>(Entities.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < Entities.Length; i++)
            ManualGroupMembers[i] = (byte)(state.EntityManager.HasComponent<ManualMoveGroupMemberTag>(Entities[i]) ? 1 : 0);
    }

    public void Dispose()
    {
        if (Entities.IsCreated)
            Entities.Dispose();
        if (Grids.IsCreated)
            Grids.Dispose();
        if (Footprints.IsCreated)
            Footprints.Dispose();
        if (ManualGroupMembers.IsCreated)
            ManualGroupMembers.Dispose();
    }
}
