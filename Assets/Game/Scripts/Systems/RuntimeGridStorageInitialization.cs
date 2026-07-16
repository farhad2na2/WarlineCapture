using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class RuntimeGridStorageInitialization
    {
        public static void InitializeCells(
            DynamicBuffer<GridWalkable> walkable,
            DynamicBuffer<GridRoad> roads,
            DynamicBuffer<GridRoadSidewalk> sidewalks,
            DynamicBuffer<GridRoadDirt> dirtRoads,
            int width,
            int height,
            Vector2Int[] authoredBlockedCells)
        {
            int gridSize = width * height;
            for (int index = 0; index < gridSize; index++)
            {
                walkable[index] = new GridWalkable { Value = 1 };
                roads[index] = new GridRoad { Value = 0 };
                sidewalks[index] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[index] = new GridRoadDirt { Value = 0 };
            }

            if (authoredBlockedCells == null)
                return;

            for (int index = 0; index < authoredBlockedCells.Length; index++)
            {
                Vector2Int cell = authoredBlockedCells[index];
                if ((uint)cell.x >= (uint)width || (uint)cell.y >= (uint)height)
                    continue;
                walkable[cell.y * width + cell.x] = new GridWalkable { Value = 0 };
            }
        }

        public static void EnsureDynamicStorage(EntityManager entityManager, Entity entity, int gridSize)
        {
            DynamicBlockerComponent blockerData = entityManager.GetComponentData<DynamicBlockerComponent>(entity);
            if (blockerData.GridSize != gridSize ||
                !blockerData.Counts.IsCreated ||
                !blockerData.Blocked.IsCreated ||
                !blockerData.FriendlyPassFactionIds.IsCreated)
            {
                if (blockerData.Counts.IsCreated)
                    blockerData.Counts.Dispose();
                if (blockerData.Blocked.IsCreated)
                    blockerData.Blocked.Dispose();
                if (blockerData.FriendlyPassFactionIds.IsCreated)
                    blockerData.FriendlyPassFactionIds.Dispose();

                blockerData.GridSize = gridSize;
                blockerData.Counts = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                blockerData.Blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                blockerData.FriendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (int index = 0; index < blockerData.FriendlyPassFactionIds.Length; index++)
                    blockerData.FriendlyPassFactionIds[index] = byte.MaxValue;
                entityManager.SetComponentData(entity, blockerData);
            }

            DynamicOccupancyComponent occupancyData = entityManager.GetComponentData<DynamicOccupancyComponent>(entity);
            if (occupancyData.GridSize == gridSize && occupancyData.Occupied.IsCreated)
                return;

            if (occupancyData.Occupied.IsCreated)
                occupancyData.Occupied.Dispose();
            occupancyData.GridSize = gridSize;
            occupancyData.Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            entityManager.SetComponentData(entity, occupancyData);
        }
    }
}
