using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(InitialUnitsSpawnSystem))]
public partial struct InitialSpawnDiagnosticLogFlushSystem : ISystem
{
    private EntityQuery _logQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _logQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<InitialSpawnDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<InitialSpawnDiagnosticLogComponent>());
        state.RequireForUpdate(_logQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (DynamicBuffer<InitialSpawnDiagnosticLogComponent> logs in
                 SystemAPI.Query<DynamicBuffer<InitialSpawnDiagnosticLogComponent>>()
                     .WithAll<InitialSpawnDiagnosticLogQueueComponent>())
        {
            for (int logIndex = 0; logIndex < logs.Length; logIndex++)
            {
                InitialSpawnDiagnosticLogComponent log = logs[logIndex];
                if (log.Severity == InitialSpawnDiagnosticLogComponent.WarningSeverity)
                    Debug.LogWarning(log.Message.ToString());
                else
                    Debug.Log(log.Message.ToString());
            }

            logs.Clear();
        }
    }
}
