using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RuntimeGridStorageCleanupSystem : ISystem
    {
        private EntityQuery _retired;

        public void OnCreate(ref SystemState state)
        {
            _retired = state.GetEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<DynamicBlockerComponent>(),
                    ComponentType.ReadOnly<DynamicOccupancyComponent>(), ComponentType.ReadOnly<PathPoolComponent>() },
                None = new[] { ComponentType.ReadOnly<GridConfig>() },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            state.RequireForUpdate(_retired);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Pathfinding jobs can retain native-array aliases from the previous
            // frame. Retirement is a scene boundary, so finish those readers first.
            state.EntityManager.CompleteAllTrackedJobs();
            using var retired = _retired.ToEntityArray(Allocator.Temp);
            foreach (var entity in retired)
            {
                RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(state.EntityManager, entity);
                state.EntityManager.RemoveComponent<DynamicBlockerComponent>(entity);
                state.EntityManager.RemoveComponent<DynamicOccupancyComponent>(entity);
                state.EntityManager.RemoveComponent<PathPoolComponent>(entity);
            }
        }
    }
}
