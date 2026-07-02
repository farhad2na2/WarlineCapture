using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
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
            foreach (DynamicBuffer<UnitPathfindingDiagnosticLogComponent> logs in SystemAPI
                         .Query<DynamicBuffer<UnitPathfindingDiagnosticLogComponent>>()
                         .WithAll<UnitPathfindingDiagnosticLogQueueComponent>())
            {
                for (int logIndex = 0; logIndex < logs.Length; logIndex++)
                    Debug.Log(logs[logIndex].Message.ToString());

                logs.Clear();
            }
        }
    }
}
