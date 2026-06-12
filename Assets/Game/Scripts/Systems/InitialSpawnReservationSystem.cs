using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialSpawnReservationSystem
{
    public void ReserveStaticBlockerFootprints(EntityManager em, ref NativeBitArray reserved, GridConfig grid)
    {
        using var blockerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<StaticGridBlocker>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<GridBlockerSize>());
        ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<GridBlockerSize> blockerSizeType = em.GetComponentTypeHandle<GridBlockerSize>(true);
        using NativeArray<ArchetypeChunk> chunks = blockerQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<GridBlockerSize> blockerSizes = chunk.GetNativeArray(ref blockerSizeType);
            for (int i = 0; i < unitGrids.Length; i++)
            {
                int2 origin = unitGrids[i].Cell;
                int2 size = blockerSizes[i].Size;
                for (int y = origin.y; y < origin.y + size.y; y++)
                {
                    if ((uint)y >= (uint)grid.Height)
                        continue;
                    int row = y * grid.Width;
                    for (int x = origin.x; x < origin.x + size.x; x++)
                    {
                        if ((uint)x >= (uint)grid.Width)
                            continue;
                        reserved.Set(row + x, true);
                    }
                }
            }
        }
    }

    public void ReserveExistingUnitFootprints(EntityManager em, ref NativeBitArray reserved, GridConfig grid)
    {
        using var unitQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
        using NativeArray<ArchetypeChunk> chunks = unitQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
            for (int i = 0; i < unitGrids.Length; i++)
            {
                int2 center = unitGrids[i].Cell;
                int2 size = UnitFootprintUtility.ClampSize(footprints[i].Size);
                int2 min = UnitFootprintUtility.GetMinCell(center, size);
                int2 max = min + size;
                for (int y = min.y; y < max.y; y++)
                {
                    if ((uint)y >= (uint)grid.Height)
                        continue;

                    int row = y * grid.Width;
                    for (int x = min.x; x < max.x; x++)
                    {
                        if ((uint)x >= (uint)grid.Width)
                            continue;

                        reserved.Set(row + x, true);
                    }
                }
            }
        }
    }
}
