using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitPathfindingDiagnosticLogFlushSystem : ISystem
{
    private EntityQuery _logQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _logQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UnitPathfindingDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<UnitPathfindingDiagnosticLogComponent>());
        state.RequireForUpdate(_logQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        using NativeArray<Entity> queueEntities = _logQueueQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < queueEntities.Length; i++)
        {
            DynamicBuffer<UnitPathfindingDiagnosticLogComponent> logs =
                state.EntityManager.GetBuffer<UnitPathfindingDiagnosticLogComponent>(queueEntities[i]);
            for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                Debug.Log(logs[logIndex].Message.ToString());

            logs.Clear();
        }
    }
}
