using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialSpawnGridContextSystem
{
    public struct Context : IDisposable
    {
        public readonly Entity GridEntity;
        public readonly GridConfig Grid;
        public readonly NativeArray<GridWalkable> Walkable;
        public readonly NativeBitArray DynamicBlocked;
        public readonly NativeBitArray Occupied;
        public NativeBitArray Reserved;

        public Context(
            Entity gridEntity,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeBitArray occupied,
            NativeBitArray reserved)
        {
            GridEntity = gridEntity;
            Grid = grid;
            Walkable = walkable;
            DynamicBlocked = dynamicBlocked;
            Occupied = occupied;
            Reserved = reserved;
        }

        public void Dispose()
        {
            if (Reserved.IsCreated)
                Reserved.Dispose();
        }
    }

    public bool TryGetGridConfig(EntityManager em, EntityQuery gridContextQuery, out GridConfig grid)
    {
        if (!TryGetGridEntity(gridContextQuery, out Entity gridEntity))
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(gridEntity);
        return true;
    }

    public bool TryCreate(EntityManager em, EntityQuery gridContextQuery, Allocator allocator, out Context context)
    {
        context = default;
        if (!TryGetGridEntity(gridContextQuery, out Entity gridEntity))
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray dynamicBlocked = em.GetComponentData<DynamicBlockerData>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
        var reserved = new NativeBitArray(grid.Width * grid.Height, allocator);
        context = new Context(gridEntity, grid, walkable, dynamicBlocked, occupied, reserved);
        return true;
    }

    private static bool TryGetGridEntity(EntityQuery gridContextQuery, out Entity gridEntity)
    {
        gridEntity = Entity.Null;
        int entityCount = gridContextQuery.CalculateEntityCount();
        if (entityCount <= 0)
            return false;

        if (entityCount == 1)
        {
            gridEntity = gridContextQuery.GetSingletonEntity();
            return gridEntity != Entity.Null;
        }

        using NativeArray<Entity> gridEntities = gridContextQuery.ToEntityArray(Allocator.Temp);
        if (gridEntities.Length <= 0)
            return false;

        gridEntity = gridEntities[0];
        return gridEntity != Entity.Null;
    }
}
