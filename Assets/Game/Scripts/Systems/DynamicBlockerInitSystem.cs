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
            if (_gridQuery.IsEmptyIgnoreFilter)
                return;

            state.Dependency.Complete();
            using NativeArray<Entity> gridEntities = _gridQuery.ToEntityArray(Allocator.Temp);
            for (int index = 0; index < gridEntities.Length; index++)
                RuntimeGridPersistentStorageUtility.DisposeStorage(state.EntityManager, gridEntities[index]);
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

            if (RuntimeGridPersistentStorageUtility.IsStorageValid(
                    state.EntityManager,
                    gridEntity,
                    gridSize))
                return;

            state.Dependency.Complete();
            RuntimeGridPersistentStorageUtility.EnsureStorage(
                state.EntityManager,
                gridEntity,
                gridSize);
        }
    }
}
