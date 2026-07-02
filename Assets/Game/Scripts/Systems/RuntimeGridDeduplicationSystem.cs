using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(DynamicBlockerInitSystem))]
    [UpdateBefore(typeof(StaticGridBlockerUpdateSystem))]
    [UpdateBefore(typeof(DynamicOccupancyRebuildSystem))]
    [UpdateBefore(typeof(InitialUnitsBlockerChurnSystem))]
    [UpdateBefore(typeof(AIBuildPlannerSystem))]
    [UpdateBefore(typeof(AICombatOrderSystem))]
    [UpdateBefore(typeof(UnitGridSnapSystem))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(UnitAirMovementSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitRespawnSystem))]
    [UpdateBefore(typeof(PathPoolMaintenanceSystem))]
    [UpdateBefore(typeof(UnitIdleWanderSystem))]
    public partial struct RuntimeGridDeduplicationSystem : ISystem
    {
        private EntityQuery _gridQuery;
        private EntityTypeHandle _entityType;
        private ComponentLookup<RuntimeGridBootstrapGridTag> _runtimeGridLookup;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _entityType = state.GetEntityTypeHandle();
            _runtimeGridLookup = state.GetComponentLookup<RuntimeGridBootstrapGridTag>(true);
            state.RequireForUpdate<GridConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_gridQuery.CalculateEntityCount() <= 1)
                return;

            state.Dependency.Complete();
            EntityManager em = state.EntityManager;
            _entityType.Update(ref state);
            _runtimeGridLookup.Update(ref state);
            using NativeArray<ArchetypeChunk> chunks = _gridQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var runtimeGridEntities = new NativeList<Entity>(_gridQuery.CalculateEntityCount(), Allocator.Temp);
            bool hasAuthoredGrid = false;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> gridEntities = chunks[chunkIndex].GetNativeArray(_entityType);
                for (int i = 0; i < gridEntities.Length; i++)
                {
                    Entity entity = gridEntities[i];
                    if (_runtimeGridLookup.HasComponent(entity))
                        runtimeGridEntities.Add(entity);
                    else
                        hasAuthoredGrid = true;
                }
            }

            if (!hasAuthoredGrid)
                return;

            for (int i = 0; i < runtimeGridEntities.Length; i++)
            {
                Entity entity = runtimeGridEntities[i];
                DisposeNativeGridData(em, entity);
                em.DestroyEntity(entity);
            }
        }

        private static void DisposeNativeGridData(EntityManager em, Entity entity)
        {
            if (em.HasComponent<DynamicBlockerComponent>(entity))
            {
                DynamicBlockerComponent data = em.GetComponentData<DynamicBlockerComponent>(entity);
                if (data.Counts.IsCreated)
                    data.Counts.Dispose();
                if (data.Blocked.IsCreated)
                    data.Blocked.Dispose();
                if (data.FriendlyPassFactionIds.IsCreated)
                    data.FriendlyPassFactionIds.Dispose();
            }

            if (em.HasComponent<PathPoolComponent>(entity))
            {
                PathPoolComponent pool = em.GetComponentData<PathPoolComponent>(entity);
                if (pool.Cells.IsCreated)
                    pool.Cells.Dispose();
            }

            if (em.HasComponent<DynamicOccupancyComponent>(entity))
            {
                DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(entity);
                if (occupancy.Occupied.IsCreated)
                    occupancy.Occupied.Dispose();
            }
        }
    }
}
