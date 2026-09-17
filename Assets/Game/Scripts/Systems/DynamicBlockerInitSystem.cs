using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RuntimeGridDeduplicationSystem))]
    [UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
    [UpdateBefore(typeof(DynamicOccupancyRebuildSystem))]
    public partial struct DynamicBlockerInitSystem : ISystem
    {
        private EntityQuery _gridQuery;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
        }

        public void OnDestroy(ref SystemState state)
        {
            state.EntityManager.CompleteAllTrackedJobs();
            using var query = state.EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<DynamicBlockerComponent>(),
                    ComponentType.ReadOnly<DynamicOccupancyComponent>(), ComponentType.ReadOnly<PathPoolComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            using var grids = query.ToEntityArray(Allocator.Temp);
            foreach (var grid in grids)
                RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(state.EntityManager, grid);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            int gridSize = grid.Width * grid.Height;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            bool addedMissingComponents = false;
            if (!state.EntityManager.HasComponent<DynamicBlockerComponent>(gridEntity))
            {
                ecb.AddComponent(gridEntity, default(DynamicBlockerComponent));
                addedMissingComponents = true;
            }

            if (!state.EntityManager.HasComponent<PathPoolComponent>(gridEntity))
            {
                ecb.AddComponent(gridEntity, default(PathPoolComponent));
                addedMissingComponents = true;
            }

            if (!state.EntityManager.HasComponent<DynamicOccupancyComponent>(gridEntity))
            {
                ecb.AddComponent(gridEntity, default(DynamicOccupancyComponent));
                addedMissingComponents = true;
            }

            if (addedMissingComponents)
                ecb.Playback(state.EntityManager);
            ecb.Dispose();

            if (RuntimeGridPersistentStorageUtilitySystemHelper.IsStorageValid(
                    state.EntityManager,
                    gridEntity,
                    gridSize))
                return;

            state.Dependency.Complete();
            RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(
                state.EntityManager,
                gridEntity,
                gridSize);
        }
    }
}
