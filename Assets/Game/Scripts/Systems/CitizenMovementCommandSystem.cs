using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal partial struct CitizenMovementCommandSystem : ISystem
{
    private EntityQuery _queueQuery;

    public void OnCreate(ref SystemState state)
    {
        _queueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<CitizenMovementCommandQueueComponent>(),
            ComponentType.ReadWrite<CitizenMoveCommandRequestElement>(),
            ComponentType.ReadWrite<CitizenMoveCommandResultElement>());
        EnsureCommandEntity(state.EntityManager, _queueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPendingRequests(state.EntityManager, _queueQuery);
    }

    public static bool TryEnqueueMoveCommand(EntityManager em, Entity entity, int2 goal)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        Entity queueEntity = EnsureCommandEntity(em);
        CitizenMovementCommandQueueComponent queue = em.GetComponentData<CitizenMovementCommandQueueComponent>(queueEntity);
        queue.LastRequestId++;
        em.SetComponentData(queueEntity, queue);

        em.GetBuffer<CitizenMoveCommandRequestElement>(queueEntity).Add(new CitizenMoveCommandRequestElement
        {
            RequestId = queue.LastRequestId,
            UnitEntity = entity,
            Goal = goal
        });
        return true;
    }

    public static void ProcessPendingRequests(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<CitizenMovementCommandQueueComponent>());
        ProcessPendingRequests(em, query);
    }

    private static void ProcessPendingRequests(EntityManager em, EntityQuery query)
    {
        Entity queueEntity = EnsureCommandEntity(em, query);
        DynamicBuffer<CitizenMoveCommandRequestElement> requests = em.GetBuffer<CitizenMoveCommandRequestElement>(queueEntity);
        if (requests.Length == 0)
            return;

        DynamicBuffer<CitizenMoveCommandResultElement> results = em.GetBuffer<CitizenMoveCommandResultElement>(queueEntity);
        results.Clear();
        for (int i = 0; i < requests.Length; i++)
        {
            CitizenMoveCommandRequestElement request = requests[i];
            bool accepted = TryApplyMoveCommand(em, request.UnitEntity, request.Goal);
            results.Add(new CitizenMoveCommandResultElement
            {
                RequestId = request.RequestId,
                UnitEntity = request.UnitEntity,
                Goal = request.Goal,
                Accepted = accepted ? (byte)1 : (byte)0
            });
        }

        requests.Clear();
    }

    private static Entity EnsureCommandEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<CitizenMovementCommandQueueComponent>());
        return EnsureCommandEntity(em, query);
    }

    private static Entity EnsureCommandEntity(EntityManager em, EntityQuery query)
    {
        Entity entity;
        if (!query.IsEmptyIgnoreFilter)
        {
            entity = query.GetSingletonEntity();
            EnsureBuffers(em, entity);
            return entity;
        }

        entity = em.CreateEntity(typeof(CitizenMovementCommandQueueComponent));
        em.SetName(entity, "CitizenMovementCommands");
        EnsureBuffers(em, entity);
        return entity;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<CitizenMoveCommandRequestElement>(entity))
            em.AddBuffer<CitizenMoveCommandRequestElement>(entity);
        if (!em.HasBuffer<CitizenMoveCommandResultElement>(entity))
            em.AddBuffer<CitizenMoveCommandResultElement>(entity);
    }

    private static bool TryApplyMoveCommand(EntityManager em, Entity entity, int2 goal)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        EntityCommandBuffer ecb = new(Allocator.Temp);

        if (em.HasComponent<EngageTarget>(entity))
            ecb.RemoveComponent<EngageTarget>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            ecb.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            ecb.RemoveComponent<UnitPathRange>(entity);
        if (em.HasComponent<AutoWanderMoveTag>(entity))
            ecb.RemoveComponent<AutoWanderMoveTag>(entity);

        if (em.HasComponent<UnitTarget>(entity))
            ecb.SetComponent(entity, new UnitTarget { Cell = goal });
        else
            ecb.AddComponent(entity, new UnitTarget { Cell = goal });

        if (!em.HasComponent<UnitAirMovement>(entity))
        {
            if (em.HasComponent<UnitPathRequest>(entity))
                ecb.SetComponent(entity, new UnitPathRequest { Goal = goal });
            else
                ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
        }
        else if (em.HasComponent<UnitPathRequest>(entity))
        {
            ecb.RemoveComponent<UnitPathRequest>(entity);
        }

        if (!em.HasComponent<ManualMoveOrderTag>(entity))
            ecb.AddComponent<ManualMoveOrderTag>(entity);

        ecb.Playback(em);
        ecb.Dispose();
        return true;
    }
}
