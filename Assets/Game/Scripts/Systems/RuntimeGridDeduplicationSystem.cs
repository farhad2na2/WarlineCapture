using Unity.Collections;
using Unity.Entities;

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

    public void OnCreate(ref SystemState state)
    {
        _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
        state.RequireForUpdate<GridConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_gridQuery.CalculateEntityCount() <= 1)
            return;

        state.Dependency.Complete();
        EntityManager em = state.EntityManager;
        using NativeArray<Entity> gridEntities = _gridQuery.ToEntityArray(Allocator.Temp);
        bool hasAuthoredGrid = false;
        for (int i = 0; i < gridEntities.Length; i++)
        {
            if (!em.HasComponent<RuntimeGridBootstrapGridTag>(gridEntities[i]))
            {
                hasAuthoredGrid = true;
                break;
            }
        }

        if (!hasAuthoredGrid)
            return;

        for (int i = 0; i < gridEntities.Length; i++)
        {
            Entity entity = gridEntities[i];
            if (!em.HasComponent<RuntimeGridBootstrapGridTag>(entity))
                continue;

            DisposeNativeGridData(em, entity);
            em.DestroyEntity(entity);
        }
    }

    private static void DisposeNativeGridData(EntityManager em, Entity entity)
    {
        if (em.HasComponent<DynamicBlockerData>(entity))
        {
            DynamicBlockerData data = em.GetComponentData<DynamicBlockerData>(entity);
            if (data.Counts.IsCreated)
                data.Counts.Dispose();
            if (data.Blocked.IsCreated)
                data.Blocked.Dispose();
            if (data.FriendlyPassFactionIds.IsCreated)
                data.FriendlyPassFactionIds.Dispose();
        }

        if (em.HasComponent<PathPoolData>(entity))
        {
            PathPoolData pool = em.GetComponentData<PathPoolData>(entity);
            if (pool.Cells.IsCreated)
                pool.Cells.Dispose();
        }

        if (em.HasComponent<DynamicOccupancyData>(entity))
        {
            DynamicOccupancyData occupancy = em.GetComponentData<DynamicOccupancyData>(entity);
            if (occupancy.Occupied.IsCreated)
                occupancy.Occupied.Dispose();
        }
    }
}
