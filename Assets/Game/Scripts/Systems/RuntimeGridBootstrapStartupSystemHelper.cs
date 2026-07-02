using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class RuntimeGridBootstrapStartupSystemHelper
    {
        private const string FixMarker = "RuntimeGridBootstrap_NoTacticalMapRuntimeLoader_2026-05-26";

        public bool Ensure(EntityManager entityManager, int width, int height, float cellSize, Vector3 origin)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.01f, cellSize);

            Entity gridEntity = ResolveGridEntity(entityManager);
            entityManager.SetComponentData(gridEntity, new GridConfig
            {
                Width = width,
                Height = height,
                CellSize = cellSize,
                Origin = (float3)origin
            });

            int gridSize = width * height;
            EnsureBufferExists<GridWalkable>(entityManager, gridEntity);
            EnsureBufferExists<GridRoad>(entityManager, gridEntity);
            EnsureBufferExists<GridRoadSidewalk>(entityManager, gridEntity);
            EnsureBufferExists<GridRoadDirt>(entityManager, gridEntity);
            if (!entityManager.HasComponent<DynamicBlockerComponent>(gridEntity))
                entityManager.AddComponentData(gridEntity, default(DynamicBlockerComponent));
            if (!entityManager.HasComponent<PathPoolComponent>(gridEntity))
                entityManager.AddComponentData(gridEntity, new PathPoolComponent { Cells = new NativeList<int2>(1024, Allocator.Persistent) });
            if (!entityManager.HasComponent<DynamicOccupancyComponent>(gridEntity))
                entityManager.AddComponentData(gridEntity, default(DynamicOccupancyComponent));

            DynamicBuffer<GridWalkable> walkable = ResizeBuffer<GridWalkable>(entityManager, gridEntity, gridSize);
            DynamicBuffer<GridRoad> roads = ResizeBuffer<GridRoad>(entityManager, gridEntity, gridSize);
            DynamicBuffer<GridRoadSidewalk> sidewalks = ResizeBuffer<GridRoadSidewalk>(entityManager, gridEntity, gridSize);
            DynamicBuffer<GridRoadDirt> dirtRoads = ResizeBuffer<GridRoadDirt>(entityManager, gridEntity, gridSize);
            EnsureDynamicGridStorage(entityManager, gridEntity, gridSize);

            for (int i = 0; i < gridSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            Debug.Log($"[RuntimeGridBootstrap] {FixMarker} ready entity={gridEntity.Index} size={width}x{height} cellSize={cellSize:0.###}");
            return true;
        }

        private static Entity ResolveGridEntity(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Entity firstGridEntity = Entity.Null;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> gridEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < gridEntities.Length; i++)
                {
                    Entity candidate = gridEntities[i];
                    if (firstGridEntity == Entity.Null)
                        firstGridEntity = candidate;
                    if (!entityManager.HasComponent<RuntimeGridBootstrapGridTag>(candidate))
                        return candidate;
                }
            }

            if (firstGridEntity != Entity.Null)
                return firstGridEntity;

            Entity entity = entityManager.CreateEntity(typeof(GridConfig), typeof(RuntimeGridBootstrapGridTag));
            entityManager.SetName(entity, "RuntimeGameplayGrid");
            return entity;
        }

        private static void EnsureBufferExists<T>(EntityManager entityManager, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(entity))
                entityManager.AddBuffer<T>(entity);
        }

        private static DynamicBuffer<T> ResizeBuffer<T>(EntityManager entityManager, Entity entity, int size)
            where T : unmanaged, IBufferElementData
        {
            DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(entity);
            buffer.ResizeUninitialized(size);
            return buffer;
        }

        private static void EnsureDynamicGridStorage(EntityManager entityManager, Entity entity, int gridSize)
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
                for (int i = 0; i < blockerData.FriendlyPassFactionIds.Length; i++)
                    blockerData.FriendlyPassFactionIds[i] = byte.MaxValue;

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
