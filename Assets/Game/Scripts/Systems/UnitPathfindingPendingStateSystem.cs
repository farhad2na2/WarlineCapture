using Unity.Entities;

public struct UnitPathfindingPendingStateComponent : IComponentData
{
    public byte HasPendingPathJob;
    public int RequestCount;
    public int RequestBudget;
    public int ScheduledFrame;
}

internal struct UnitPathfindingPendingStateSystem
{
    public EntityQuery CreateQuery(ref SystemState state)
    {
        return state.GetEntityQuery(ComponentType.ReadWrite<UnitPathfindingPendingStateComponent>());
    }

    public void EnsureSingleton(ref SystemState state, EntityQuery query)
    {
        if (!query.IsEmptyIgnoreFilter)
            return;

        state.EntityManager.CreateEntity(typeof(UnitPathfindingPendingStateComponent));
    }

    public void Publish(
        ref SystemState state,
        EntityQuery query,
        bool hasPendingPathJob,
        int requestCount,
        int requestBudget,
        int scheduledFrame)
    {
        EnsureSingleton(ref state, query);
        Entity entity = query.GetSingletonEntity();
        state.EntityManager.SetComponentData(
            entity,
            new UnitPathfindingPendingStateComponent
            {
                HasPendingPathJob = hasPendingPathJob ? (byte)1 : (byte)0,
                RequestCount = requestCount,
                RequestBudget = requestBudget,
                ScheduledFrame = scheduledFrame
            });
    }
}

internal sealed class UnitPathfindingPendingStateReadSystem
{
    private EntityQuery _query;
    private bool _hasQuery;

    public bool HasPendingPathJob()
    {
        if (!TryEnsureQuery())
            return false;
        if (_query.IsEmptyIgnoreFilter)
            return false;

        return _query.GetSingleton<UnitPathfindingPendingStateComponent>().HasPendingPathJob != 0;
    }

    public void Dispose()
    {
        if (_hasQuery)
            _query.Dispose();
        _query = default;
        _hasQuery = false;
    }

    private bool TryEnsureQuery()
    {
        if (_hasQuery)
            return true;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        _query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitPathfindingPendingStateComponent>());
        _hasQuery = true;
        return true;
    }
}
