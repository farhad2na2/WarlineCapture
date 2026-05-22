using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitTransportBoardingSystem))]
public partial struct TransportBoardingDiagnosticLogFlushSystem : ISystem
{
    private EntityQuery _logQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _logQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
        state.RequireForUpdate(_logQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        bool shouldLog = ShouldFlushDiagnostics(ref state);
        using NativeArray<Entity> queueEntities = _logQueueQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < queueEntities.Length; i++)
        {
            DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs =
                state.EntityManager.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntities[i]);
            if (shouldLog)
            {
                for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                    Debug.Log(logs[logIndex].Message.ToString());
            }

            logs.Clear();
        }
    }

    private bool ShouldFlushDiagnostics(ref SystemState state)
    {
        if (Application.isBatchMode)
            return true;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().TransportBoardingDiagnostics != 0;
    }
}
