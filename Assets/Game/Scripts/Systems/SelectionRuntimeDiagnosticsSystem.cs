using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class SelectionRuntimeDiagnosticsSystem
{
    public static readonly bool EnableSelectionClickDiagnostics = false;
    public static readonly bool EnableMoveCommandTrace = true;

    private const string SelectionClickPrefix = "[SelectionClick]";
    private const string MoveCommandTracePrefix = "[MoveCommandTrace]";

    public void EnqueueSelectionDiagnostic(string message)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        if (ShouldQueueTransportBoardingDiagnostics(em))
            EnqueueTransportBoardingDiagnostic(em, $"[Selection] {message}");
    }

    public void LogSelectionClickDiagnostic(string message)
    {
        if (!EnableSelectionClickDiagnostics)
            return;

        Debug.Log($"{SelectionClickPrefix} {message}");
        EnqueueSelectionDiagnostic(message);
    }

    [System.Diagnostics.Conditional("WARLINE_SELECTION_CLICK_DIAGNOSTICS")]
    public static void LogSelectionClickDebug(string message)
    {
        Debug.Log(message);
    }

    public static void LogMoveCommandTrace(string message)
    {
        if (!EnableMoveCommandTrace)
            return;

        Debug.Log($"{MoveCommandTracePrefix} {message}");
    }

    private static bool ShouldQueueTransportBoardingDiagnostics(EntityManager em)
    {
        if (Application.isBatchMode)
            return true;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        return !query.IsEmptyIgnoreFilter &&
            em.GetComponentData<RuntimeDiagnosticsStateComponent>(query.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
    }

    private static Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
        em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        return queueEntity;
    }

    private static void EnqueueTransportBoardingDiagnostic(EntityManager em, FixedString512Bytes message)
    {
        Entity queueEntity = EnsureTransportBoardingDiagnosticQueue(em);
        DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs = em.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
    }
}
