using Unity.Collections;
using Unity.Entities;

public struct UnitRenderBudgetDiagnosticLog
{
    private Entity _logQueueEntity;

    public void LogEmptyQueryState(
        EntityManager em,
        int frame,
        UnitRenderBudgetSources.Context queryContext,
        int unitCount,
        bool playRequested)
    {
        EnqueueLog(
            em,
            $"[UnitRenderBudgetEmptyDiag] frame={frame} queryUnits={unitCount} allUnitGrid={queryContext.AllUnitGridQuery.CalculateEntityCount()} spawnConfigs={queryContext.SpawnConfigQuery.CalculateEntityCount()} spawnProgress={queryContext.SpawnProgressQuery.CalculateEntityCount()} spawnInitialized={queryContext.SpawnInitializedQuery.CalculateEntityCount()} playRequested={(playRequested ? 1 : 0)}");
    }

    public void EnqueueLog(EntityManager em, string message)
    {
        Enqueue(em, message, UnitRenderBudgetDiagnosticLogComponent.LogSeverity);
    }

    public void EnqueueWarning(EntityManager em, string message)
    {
        Enqueue(em, message, UnitRenderBudgetDiagnosticLogComponent.WarningSeverity);
    }

    private void Enqueue(EntityManager em, string message, byte severity)
    {
        if (_logQueueEntity == Entity.Null || !em.Exists(_logQueueEntity))
            _logQueueEntity = GetOrCreateLogQueue(em);

        DynamicBuffer<UnitRenderBudgetDiagnosticLogComponent> logs =
            em.GetBuffer<UnitRenderBudgetDiagnosticLogComponent>(_logQueueEntity);
        logs.Add(new UnitRenderBudgetDiagnosticLogComponent
        {
            Message = CreateFixedMessage(message),
            Severity = severity
        });
    }

    private static Entity GetOrCreateLogQueue(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitRenderBudgetDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<UnitRenderBudgetDiagnosticLogComponent>());
        try
        {
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();
        }
        finally
        {
            query.Dispose();
        }

        Entity queueEntity = em.CreateEntity(typeof(UnitRenderBudgetDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "UnitRenderBudgetDiagnosticLogQueue");
        em.AddBuffer<UnitRenderBudgetDiagnosticLogComponent>(queueEntity);
        return queueEntity;
    }

    private static FixedString4096Bytes CreateFixedMessage(string message)
    {
        var fixedMessage = new FixedString4096Bytes();
        fixedMessage.Append(message);
        return fixedMessage;
    }
}
