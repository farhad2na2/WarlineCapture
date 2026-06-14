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

        using NativeList<CitizenMoveCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
        for (int i = 0; i < requests.Length; i++)
            pendingRequests.Add(requests[i]);
        requests.Clear();

        DynamicBuffer<CitizenMoveCommandResultElement> results = em.GetBuffer<CitizenMoveCommandResultElement>(queueEntity);
        results.Clear();
        NativeArray<CitizenMoveCommandRequestElement> pendingRequestArray = pendingRequests.AsArray();
        for (int i = 0; i < pendingRequestArray.Length; i++)
        {
            CitizenMoveCommandRequestElement request = pendingRequestArray[i];
            bool accepted = TryApplyMoveCommand(em, request.UnitEntity, request.Goal);
            results = em.GetBuffer<CitizenMoveCommandResultElement>(queueEntity);
            results.Add(new CitizenMoveCommandResultElement
            {
                RequestId = request.RequestId,
                UnitEntity = request.UnitEntity,
                Goal = request.Goal,
                Accepted = accepted ? (byte)1 : (byte)0
            });
        }
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

        return UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, entity, goal);
    }
}
