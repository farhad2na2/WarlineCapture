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
            return Ensure(entityManager, width, height, cellSize, origin, null);
        }

        public bool Ensure(
            EntityManager entityManager,
            int width,
            int height,
            float cellSize,
            Vector3 origin,
            Vector2Int[] authoredBlockedCells)
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
            RuntimeGridStorageInitialization.EnsureDynamicStorage(entityManager, gridEntity, gridSize);
            RuntimeGridStorageInitialization.InitializeCells(
                walkable, roads, sidewalks, dirtRoads, width, height, authoredBlockedCells);

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

    }
}
