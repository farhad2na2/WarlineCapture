using Unity.Entities;

public sealed class MatchStartRequestSystem
{
    private World _world;
    private Entity _matchStartEntity;

    public bool QueueStartAfterMatchLoaded(EntityManager em)
    {
        Entity entity = EnsureMatchStartEntity(em);
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(entity);
        DynamicBuffer<MatchStartRequestElement> requests = em.GetBuffer<MatchStartRequestElement>(entity);
        if (queue.IsStartPending != 0 || requests.Length > 0)
            return true;

        queue.LastRequestId++;
        requests.Add(new MatchStartRequestElement
        {
            RequestId = queue.LastRequestId,
            RequireMatchLoaded = 1
        });
        em.SetComponentData(entity, queue);
        return true;
    }

    private Entity EnsureMatchStartEntity(EntityManager em)
    {
        World world = em.World;
        if (_world == world &&
            _matchStartEntity != Entity.Null &&
            em.Exists(_matchStartEntity) &&
            em.HasComponent<MatchStartBoundaryComponent>(_matchStartEntity))
        {
            EnsureBuffers(em, _matchStartEntity);
            return _matchStartEntity;
        }

        _world = world;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MatchStartBoundaryComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _matchStartEntity = query.GetSingletonEntity();
            EnsureBuffers(em, _matchStartEntity);
            return _matchStartEntity;
        }

        _matchStartEntity = em.CreateEntity(typeof(MatchStartBoundaryComponent), typeof(MatchStartQueueComponent));
        em.SetName(_matchStartEntity, "MatchStartBoundary");
        EnsureBuffers(em, _matchStartEntity);
        return _matchStartEntity;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<MatchStartRequestElement>(entity))
            em.AddBuffer<MatchStartRequestElement>(entity);
        if (!em.HasBuffer<MatchStartResultElement>(entity))
            em.AddBuffer<MatchStartResultElement>(entity);
        if (!em.HasComponent<MatchStartProgressComponent>(entity))
            em.AddComponentData(entity, new MatchStartProgressComponent());
    }
}
