using Unity.Collections;
using Unity.Entities;

public struct InitialSpawnDiagnosticLogSystem
{
    private Entity _logQueueEntity;

    public void EnsureQueue(EntityManager em)
    {
        if (_logQueueEntity == Entity.Null || !em.Exists(_logQueueEntity))
            _logQueueEntity = GetOrCreateLogQueue(em);
    }

    public void EnqueueLog(EntityManager em, string message)
    {
        Enqueue(em, message, InitialSpawnDiagnosticLogComponent.LogSeverity);
    }

    public void EnqueueWarning(EntityManager em, string message)
    {
        Enqueue(em, message, InitialSpawnDiagnosticLogComponent.WarningSeverity);
    }

    private void Enqueue(EntityManager em, string message, byte severity)
    {
        EnsureQueue(em);
        DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs =
            em.GetBuffer<InitialSpawnDiagnosticLogComponent>(_logQueueEntity);
        logs.Add(new InitialSpawnDiagnosticLogComponent
        {
            Message = CreateFixedMessage(message),
            Severity = severity
        });
    }

    private static Entity GetOrCreateLogQueue(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialSpawnDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<InitialSpawnDiagnosticLogComponent>());
        try
        {
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();
        }
        finally
        {
            query.Dispose();
        }

        Entity queueEntity = em.CreateEntity(typeof(InitialSpawnDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "InitialSpawnDiagnosticLogQueue");
        em.AddBuffer<InitialSpawnDiagnosticLogComponent>(queueEntity);
        return queueEntity;
    }

    private static FixedString4096Bytes CreateFixedMessage(string message)
    {
        var fixedMessage = new FixedString4096Bytes();
        fixedMessage.Append(message);
        return fixedMessage;
    }
}
