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
        using var blockers = blockerQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < blockers.Length; i++)
        {
            Entity blocker = blockers[i];
            int2 origin = em.GetComponentData<UnitGrid>(blocker).Cell;
            int2 size = em.GetComponentData<GridBlockerSize>(blocker).Size;
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
        using var units = unitQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (!em.Exists(unit))
                continue;

            int2 center = em.GetComponentData<UnitGrid>(unit).Cell;
            int2 size = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(unit).Size);
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
