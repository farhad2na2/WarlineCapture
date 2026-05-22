using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AICombatOrderSystem))]
public partial struct AIDiagnosticLogFlushSystem : ISystem
{
    private EntityQuery _logQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _logQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        state.RequireForUpdate(_logQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        bool shouldLog = ShouldFlushDiagnostics(ref state);
        using Unity.Collections.NativeArray<Entity> queueEntities = _logQueueQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < queueEntities.Length; i++)
        {
            DynamicBuffer<AIDiagnosticLogComponent> logs = state.EntityManager.GetBuffer<AIDiagnosticLogComponent>(queueEntities[i]);
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
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
    }
}
