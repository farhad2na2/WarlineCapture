using Unity.Collections;
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

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity entity = ecb.CreateEntity();
        ecb.AddComponent<UnitPathfindingPendingStateComponent>(entity);
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public static UnitPathfindingPendingStateComponent CreateState(
        bool hasPendingPathJob,
        int requestCount,
        int requestBudget,
        int scheduledFrame)
    {
        return new UnitPathfindingPendingStateComponent
        {
            HasPendingPathJob = hasPendingPathJob ? (byte)1 : (byte)0,
            RequestCount = requestCount,
            RequestBudget = requestBudget,
            ScheduledFrame = scheduledFrame
        };
    }
}

internal sealed class UnitPathfindingPendingStateReadSystem
{
    private EntityQuery _query;
    private World _world;
    private bool _hasQuery;

    public bool HasPendingPathJob()
    {
        if (!TryEnsureQuery())
            return false;

        try
        {
            if (_query.IsEmptyIgnoreFilter)
                return false;

            return _query.GetSingleton<UnitPathfindingPendingStateComponent>().HasPendingPathJob != 0;
        }
        catch (System.NullReferenceException)
        {
            ClearQueryState();
            return false;
        }
        catch (System.InvalidOperationException)
        {
            ClearQueryState();
            return false;
        }
    }

    public void Dispose()
    {
        if (!_hasQuery)
            return;

        try
        {
            if (IsQueryWorldAlive())
                _query.Dispose();
        }
        catch (System.NullReferenceException)
        {
        }
        catch (System.InvalidOperationException)
        {
        }
        finally
        {
            ClearQueryState();
        }
    }

    private bool TryEnsureQuery()
    {
        if (_hasQuery && IsQueryWorldAlive())
            return true;

        ClearQueryState();

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        _query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitPathfindingPendingStateComponent>());
        _world = world;
        _hasQuery = true;
        return true;
    }

    private bool IsQueryWorldAlive()
    {
        return _world != null && _world.IsCreated;
    }

    private void ClearQueryState()
    {
        _query = default;
        _world = null;
        _hasQuery = false;
    }
}
