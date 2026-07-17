using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal static class RuntimeGridPersistentStorageUtilitySystemHelper
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

        public static bool IsStorageValid(EntityManager entityManager, Entity entity, int gridSize)
        {
            if (gridSize <= 0 ||
                !entityManager.HasComponent<DynamicBlockerComponent>(entity) ||
                !entityManager.HasComponent<DynamicOccupancyComponent>(entity) ||
                !entityManager.HasComponent<PathPoolComponent>(entity))
            {
                return false;
            }

            DynamicBlockerComponent blockerData = entityManager.GetComponentData<DynamicBlockerComponent>(entity);
            DynamicOccupancyComponent occupancyData = entityManager.GetComponentData<DynamicOccupancyComponent>(entity);
            PathPoolComponent pathPool = entityManager.GetComponentData<PathPoolComponent>(entity);
            return blockerData.GridSize == gridSize &&
                   blockerData.Counts.IsCreated &&
                   blockerData.Blocked.IsCreated &&
                   blockerData.FriendlyPassFactionIds.IsCreated &&
                   occupancyData.GridSize == gridSize &&
                   occupancyData.Occupied.IsCreated &&
                   pathPool.Cells.IsCreated;
        }

        public static void EnsureStorage(EntityManager entityManager, Entity entity, int gridSize)
        {
            if (gridSize <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(gridSize));
            RequireStorageComponents(entityManager, entity);

            bool storageResized = false;
            DynamicBlockerComponent blockerData = entityManager.GetComponentData<DynamicBlockerComponent>(entity);
            if (blockerData.GridSize != gridSize ||
                !blockerData.Counts.IsCreated ||
                !blockerData.Blocked.IsCreated ||
                !blockerData.FriendlyPassFactionIds.IsCreated)
            {
                DynamicBlockerComponent replacement = CreateBlockerStorage(gridSize);
                DisposeBlocker(ref blockerData);
                blockerData = replacement;
                entityManager.SetComponentData(entity, blockerData);
                storageResized = true;
            }

            DynamicOccupancyComponent occupancyData = entityManager.GetComponentData<DynamicOccupancyComponent>(entity);
            if (occupancyData.GridSize != gridSize || !occupancyData.Occupied.IsCreated)
            {
                DynamicOccupancyComponent replacement = new()
                {
                    GridSize = gridSize,
                    Occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory)
                };
                DisposeOccupancy(ref occupancyData);
                occupancyData = replacement;
                entityManager.SetComponentData(entity, occupancyData);
                storageResized = true;
            }

            PathPoolComponent pathPool = entityManager.GetComponentData<PathPoolComponent>(entity);
            if (!pathPool.Cells.IsCreated)
            {
                pathPool.Cells = new NativeList<Unity.Mathematics.int2>(1024, Allocator.Persistent);
                entityManager.SetComponentData(entity, pathPool);
            }
            else if (storageResized && pathPool.Cells.Length != 0)
            {
                pathPool.Cells.Clear();
                entityManager.SetComponentData(entity, pathPool);
            }
        }

        public static void DisposeStorage(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.Exists(entity))
                return;

            if (entityManager.HasComponent<DynamicBlockerComponent>(entity))
            {
                DynamicBlockerComponent blockerData = entityManager.GetComponentData<DynamicBlockerComponent>(entity);
                DisposeBlocker(ref blockerData);
                entityManager.SetComponentData(entity, blockerData);
            }

            if (entityManager.HasComponent<DynamicOccupancyComponent>(entity))
            {
                DynamicOccupancyComponent occupancyData = entityManager.GetComponentData<DynamicOccupancyComponent>(entity);
                DisposeOccupancy(ref occupancyData);
                entityManager.SetComponentData(entity, occupancyData);
            }

            if (entityManager.HasComponent<PathPoolComponent>(entity))
            {
                PathPoolComponent pathPool = entityManager.GetComponentData<PathPoolComponent>(entity);
                if (pathPool.Cells.IsCreated)
                    pathPool.Cells.Dispose();
                pathPool = default;
                entityManager.SetComponentData(entity, pathPool);
            }
        }

        private static DynamicBlockerComponent CreateBlockerStorage(int gridSize)
        {
            NativeArray<int> counts = default;
            NativeBitArray blocked = default;
            NativeArray<byte> friendlyPassFactionIds = default;
            try
            {
                counts = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                for (int index = 0; index < friendlyPassFactionIds.Length; index++)
                    friendlyPassFactionIds[index] = byte.MaxValue;
                return new DynamicBlockerComponent
                {
                    GridSize = gridSize,
                    Counts = counts,
                    Blocked = blocked,
                    FriendlyPassFactionIds = friendlyPassFactionIds
                };
            }
            catch
            {
                if (counts.IsCreated)
                    counts.Dispose();
                if (blocked.IsCreated)
                    blocked.Dispose();
                if (friendlyPassFactionIds.IsCreated)
                    friendlyPassFactionIds.Dispose();
                throw;
            }
        }

        private static void RequireStorageComponents(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<DynamicBlockerComponent>(entity) ||
                !entityManager.HasComponent<DynamicOccupancyComponent>(entity) ||
                !entityManager.HasComponent<PathPoolComponent>(entity))
            {
                throw new System.InvalidOperationException(
                    "Runtime grid persistent storage components must exist before allocation.");
            }
        }

        private static void DisposeBlocker(ref DynamicBlockerComponent blockerData)
        {
            if (blockerData.Counts.IsCreated)
                blockerData.Counts.Dispose();
            if (blockerData.Blocked.IsCreated)
                blockerData.Blocked.Dispose();
            if (blockerData.FriendlyPassFactionIds.IsCreated)
                blockerData.FriendlyPassFactionIds.Dispose();
            blockerData = default;
        }

        private static void DisposeOccupancy(ref DynamicOccupancyComponent occupancyData)
        {
            if (occupancyData.Occupied.IsCreated)
                occupancyData.Occupied.Dispose();
            occupancyData = default;
        }
    }
}
